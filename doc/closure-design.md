# 闭包(Closure)机制设计 —— FrontEnd 层

> 状态:框架版(第一版)。只实现 FrontEnd(Lexer→TokenParse→StructParse→FileMeta→MetaCore→IR),
> 虚拟机(CVM)侧待本设计确认后另行出方案。
> 本文先整理 Dart 的闭包机制,再给出本语言(SimpleLanguage)的语法/语义/降级方案与 IR 指令设计。

---

## 1. Dart 闭包机制整理

### 1.1 基本概念

Dart 中**函数是一等公民**(first-class objects)。任何函数字面量(function literal)都是一个对象,
它"捕获"自己词法作用域(lexical scope)中引用到的外层变量,即使外层函数已经返回,
这些变量依然存活——这就是闭包。

```dart
Function makeAdder(int addBy) {      // 外层函数
  return (int i) => addBy + i;       // 函数字面量,捕获 addBy
}

void main() {
  var add2 = makeAdder(2);           // add2 持有 addBy=2 的环境
  print(add2(3));                    // 5 —— 外层变量 addBy 仍然存活
}
```

### 1.2 Dart 闭包的关键语义

| 语义点 | Dart 行为 |
|---|---|
| **捕获方式** | **按引用捕获**(capture by reference)。闭包内对外层变量的读写,作用到外层变量本身(同一存储位置) |
| **共享性** | 同一作用域派生的多个闭包共享同一批被捕获变量(一个改,另一个能看到) |
| **可变性** | 闭包内可以修改被捕获变量,外层也能看到修改 |
| **声明位置** | 函数字面量可出现在任何表达式位置:赋值右侧、实参、返回值、集合元素…… |
| **延迟绑定** | 捕获以**变量**为单位,不是以"值快照"为单位 |
| **this 捕获** | 方法内定义的闭包可访问 `this` 与实例成员 |

### 1.3 Dart 的实现降级(Dart Kernel/VM 视角)

Dart 编译器把闭包降级为:

1. **Context(上下文对象)**:被捕获变量的存放地。一个堆分配的"槽位数组"。
   - 未被捕获的局部变量留在栈帧;
   - 一旦某变量被内层闭包引用,该变量就从**栈槽移入 Context 槽**;
   - Context 由**最内层捕获它的作用域**创建,作为隐藏参数在闭包链上传递。
2. **闭包对象**:结构上是 `(函数入口, context 引用)` 的组合体(Dart VM 中为 `Closure` 对象,
   含 `function` 与 `context` 两个核心字段)。
3. **调用约定**:调用闭包时,VM 用闭包对象里保存的函数入口,并把闭包对象里的 context
   作为该函数的隐藏首参传入。函数体访问被捕获变量 = `context[slot]`。

本语言的实现完全采用这一思路:**合成静态函数 + context 隐藏参数**。

---

## 2. 本语言的闭包语法设计

### 2.1 两种定义形式

**形式一:具名闭包(`function` 关键字)**

```
function 闭包名( 参数列表 ) {
    ... 闭包体 ...
}
```

**形式二:匿名闭包(`var` + 函数字面量)**

```
var 闭包名 = ( 参数列表 ) {
    ... 闭包体 ...
}
```

### 2.2 语法约束(第一版)

1. **`function` 关键字出现即闭包**。源码中遇到 `function` 关键字,一律按闭包处理。
2. **闭包只能出现在方法体内**。类成员位置 / 顶层出现 `function` 直接报编译错误。
3. **先定义后使用**。闭包变量只在定义点之后可见(与 `var` 局部变量一致,沿用
   `AddOnlyNameMetaVariable` 防前向引用机制)。
4. **返回值**:闭包体用 `ret 表达式;` 返回。第一版**返回类型不做静态推断**,合成函数
   返回类型默认 `void`;`ret expr;` 的值按表达式求值后返回(类型层面不校验,留待 VM 方案)。
5. **捕获范围(第一版)**:仅支持捕获**外层方法的局部变量与参数**(不含 `this`/实例成员/
   静态成员——它们本就可通过其它路径访问,不进 context)。
6. **按引用捕获、共享**:捕获的是变量的"槽位",读写直达原变量。

### 2.3 示例

```
class Main {
    void Test() {
        var count = 0;

        // 形式一:具名闭包,捕获 count
        function addCount( int step ) {
            count = count + step;      // 写捕获变量
            ret count;
        }

        var r = addCount( 3 );          // 调用闭包 -> 3

        // 形式二:匿名闭包
        var add = ( int a, int b ) {
            ret a + b;                  // 捕获参数 a,b(闭包自身参数,不进 context)
        }
        var s = add( 1, 2 );            // 调用闭包 -> 3
    }
}
```

---

## 3. 编译流水线各层的设计

### 3.0 总览:降级(Desugar)策略

```
用户写的闭包                     编译器降级为
──────────────────────────────────────────────────────────────────
function f(a){ body }      →   (1) 宿主类上合成静态函数  <宿主类名>_closure_<N>_f(a, ctx)
var add = (a,b){ body }        (2) context 数组:捕获变量按序装箱
                               (3) 调用点:压捕获值 → NewClosure → StoreLocal
                               (4) 闭包体访问捕获变量: ctx[slot]
                               (5) f(x) 调用点:CallClosure
```

闭包对象(CVM 侧将来实现)= `{ 方法引用, context引用 }`。
本版 FrontEnd 只负责产出正确的 IR 序列,不生成 Closure 元类(暂用 `object` 作为闭包变量的静态类型)。

### 3.1 Lexer 层

`ReadIdentifier` 关键字表新增 `"function"` → `ETokenType.Function`
(`ETokenType.Function` 枚举值已存在,一直未接入 Lexer)。

### 3.2 TokenParse 层(Node 树)

不需要专门改动。两种写法经现有规则自然形成:

| 写法 | childList 形态 |
|---|---|
| 具名:`function f(a,b){...}` | `[Key(function), IdentifierLink(f, f.parNode=(a,b)), Brace(body)]` —— 参数 Par 挂在 IdentifierLink 上 |
| 匿名:`var f = (a,b){...}` | `[Key(var), IdentifierLink(f), Assign, Par(a,b), Brace(body)]` —— `=` 会把 identifierNode 置空,故 Par/Brace 都是直接 child |

### 3.3 StructParse 层(→FileMeta)

改动点:

1. `function` 关键字加入 6 处 token 判定列表,使其成为一个"Key 语句":
   - `SyntaxNodeStruct.AddContent`:路由到 keyContent(而不是 commonContent)
   - `SyntaxNodeStruct.SetBraceNode`:允许 `{}` 挂接
   - `SyntaxNodeStruct.IsLineEndBreak`:遇行尾不提前断句(继续找 `{`)
   - `NeedAttachTrailingBrace`
   - `GetOneSyntax`:进入 `SetMainKeyNode` 的关键字列表
   - `GetOneSyntax` Brace 分支:`isMustContactBrace` 列表
2. `HandleCreateFileMetaSyntaxByPNode` 新增 `ETokenType.Function` 分支:
   解析具名闭包 → 生成 `FileMetaDefineClosureSyntax`。闭包体走
   `ParseCurrentNodeInfo push → ParseSyntax(blockNode) → pop` 递归(与 if 分支同模式)。
3. `CrateFileMetaSyntaxNoKey` 匿名闭包拦截(在 hasSameLineExpression 检查之后、
   CreateFileMetaExpress 之前):
   判定 `varToken != null && afterNodeList[0] 是 Par && afterNodeList[1] 是 Brace`
   → 生成 `FileMetaDefineClosureSyntax(匿名)`。
4. **闭包只能在方法体内**:StructParse 创建闭包语法时,检查当前
   `ParseCurrentNodeInfo.parseType == Function/Statements`,否则报错。

### 3.4 FileMeta 层

新增语法类 `FileMetaDefineClosureSyntax : FileMetaSyntax`:

- `name` / `nameToken`:闭包变量名
- `List<FileMetaParamterDefine> paramList`:参数(复用现有参数解析)
- `FileMetaBlockSyntax blockSyntax`:闭包体
- `bool isAnonymous`:匿名标记(仅影响报错信息/格式化输出)

### 3.5 MetaCore 层

新增两块:

**(a) `MetaClosureDefineStatements : MetaBaseStatements`**(闭包定义语句)

职责(仿 `MetaDefineVarStatements`):

1. `AddOnlyNameMetaVariable(name)` 防前向引用;
2. 在当前块建 `MetaVariable(name, EVariableFrom.LocalStatement, ...)`,类型暂定 object;
3. **合成闭包函数**:
   - `MetaMemberFunction(ownerClass, "<宿主>_<外层函数>_closure_<序号>_<名>")`
   - **isStatic = true**(不占用 this 槽)
   - 参数 = `[context(隐藏, Argument 0, 类型 object)] + 用户参数`
   - `MethodManager.instance.AddDynamicMemeberFunction(...)` 注册,使其进入 IR 翻译列表;
4. **捕获分析**:解析闭包体块(用 `MetaClosureBlockStatements`,见下),
   收集"闭包体引用但属于外层作用域"的变量 → 分配 context 槽位(0..N-1);
5. 闭包体内对这些变量的访问改写为 context 槽位访问;
6. 生成"创建闭包"的元表达式(供 IR 层消费)。

**(b) `MetaClosureBlockStatements : MetaBlockStatements`**(闭包函数体块)

- 重写变量查找:先查自己块内(含闭包参数),未命中则向**闭包宿主作用域**请求外层变量;
- 命中外层变量时:
  - 已分配过槽 → 复用槽号;
  - 未分配 → 分配新槽,登记到 `MetaClosureFunction.captureList`;
- 返回一个**捕获代理 MetaVariable**(`EVariableFrom.ClosureContext` 新枚举,
  携带 `slotIndex` + context 来源),闭包体内读写它 → IR 生成 ctx[slot] 序列。

**(c) `MetaVariable.EVariableFrom` 新增 `ClosureContext`**

表示"闭包 context 槽位变量",用于 IR 层识别并生成
`LoadArgument 0; LoadConstInt32 slot; LoadArrayIndex` / `... StoreArrayIndex` 序列。

### 3.6 IR 层

**(a) 新增操作码**(`EIROpCode` 末尾追加):

| 指令 | 栈行为 | payload | 语义 |
|---|---|---|---|
| `NewClosure` | `... v0 v1 .. vN-1 → closure` | IRMethod 引用(闭包合成函数) | 从栈弹出 N 个捕获值(N=captureCount,payload 方法元数据里携带),装箱进 context 数组,与函数入口组装成闭包对象压栈 |
| `CallClosure` | `... closure a1 a2 .. aM → ret` | 无(或 paramCount) | 弹出实参;经闭包对象取出函数入口与 context,以 `(context, a1..aM)` 调用 |

**(b) 闭包定义语句 IR**(`IRClosureDefineStatements`):

```
; function f(a){...} 捕获 [x, y]
LoadLocal x            ; 或 LoadArgument(参数捕获)
LoadLocal y
NewClosure  <closure_method>   ; payload=IRMethod
StoreLocal f
```

**(c) 闭包调用 IR**:

调用识别:调用链首个节点是**闭包变量**(其类型为闭包合成函数关联)时走 CallClosure:

```
; f(1, 2)
LoadLocal f            ; 闭包对象(先压)
LoadConstInt32 1
LoadConstInt32 2
CallClosure            ; index = 3(closure + 2实参)
```

**(d) 闭包体内捕获变量读写 IR**(IRVariable 对 `EVariableFrom.ClosureContext` 分派):

```
; 读 ctx 槽 k
LoadArgument 0         ; context(隐藏首参)
LoadConstInt32 k
LoadArrayIndex

; 写 ctx 槽 k
LoadArgument 0
<value 计算序列>
LoadConstInt32 k
StoreArrayIndex
```

**(e) 闭包函数体 IR**

合成函数正常走 `IRMethod` 翻译(参数含隐藏 context),`ret expr;` 正常生成返回序列。

---

## 4. 框架版范围与限制(明确不做)

| 项 | 状态 |
|---|---|
| `this` / 实例成员 / 静态成员捕获 | ❌ 不支持(第一版只捕获局部变量与参数) |
| 闭包嵌套(闭包里再定义闭包,多级 context) | ❌ 暂不(结构上已预留:context 也是闭包体内可见变量,后续可扩展) |
| 闭包作为实参/返回值传递 | ⚠️ 类型层面是 object,值可以传(框架版不校验) |
| 返回类型静态推断 | ❌ 合成函数返回 void,`ret` 不做类型检查 |
| 闭包赋值给已有变量 / 二次赋值 | ❌ 只支持定义即创建 |
| `function` 出现在方法体外 | ❌ 编译错误 |
| VM(CVM)侧 NewClosure/CallClosure 执行 | ❌ 等设计确认后另出方案 |

---

## 5. 验证方式

框架版不做运行时验证(CVM 未实现),验证手段为:

1. 编译含闭包用例的 `.sp` 源码,FrontEnd 全流程无报错;
2. 检查 `IR.txt` 调试输出中的指令序列:
   - 定义点出现 `LoadLocal/LoadArgument(捕获值) → NewClosure → StoreLocal`;
   - 调用点出现 `LoadLocal f → 实参 → CallClosure`;
   - 闭包函数体内出现 `LoadArgument 0 → LoadConstInt32 k → Load/StoreArrayIndex`;
   - 合成函数出现在动态函数列表并有正确的参数表(context 为 Argument 0)。

---

## 6. 文件改动清单

| 层 | 文件 | 改动 |
|---|---|---|
| Lexer | `Front\Compile\Parse\LexerParseToToken.cs` | `ReadIdentifier` 加 `case "function"` |
| Enum | `Front\Define.cs` | (已有 `ETokenType.Function`,无改动) |
| StructParse | `Front\Compile\Parse\StructParseToSyntax.cs` | 6 处 token 列表 + Function 分支 + 匿名闭包拦截 |
| FileMeta | `Front\Compile\FileMeta\FileMetaSyntax.cs`(或新文件) | 新增 `FileMetaDefineClosureSyntax` |
| MetaCore | `Front\Core\Statements\`(新文件) | `MetaClosureDefineStatements`、`MetaClosureBlockStatements`、`MetaClosureFunction` |
| MetaCore | `Front\Core\MetaVariable.cs` | `EVariableFrom` 加 `ClosureContext` |
| MetaCore | `Front\Core\MetaMemberFunction.cs` | `HandleMetaSyntax` 加闭包 case |
| IR | `Front\IROpEnum.cs` | 加 `NewClosure` / `CallClosure` |
| IR | `Front\IR\IRStatements\`(新文件) | `IRClosureDefineStatements` |
| IR | `Front\IR\IRStatements\IRBlockStatements.cs` | `ParseAnyIRStatements` 加闭包 case |
| IR | `Front\IR\IRVariable.cs` | `ClosureContext` 的 Load/Store 分派 |
| IR | `Front\IR\IRCall.cs` | 调用链闭包识别 → CallClosure |
