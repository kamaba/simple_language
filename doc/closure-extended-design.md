# 闭包(Closure)扩展实现 -- FrontEnd 层 (第二版)

> 前置文档: [closure-design.md](closure-design.md) (框架版第一版)
>
> 本文档在第一版基础上扩展:引入 `Function` 类型、间接闭包调用、
> 闭包内访问类成员、`Array.forEach` 闭包回调。
> 范围:仅 FrontEnd(Lexer -> MetaCore -> IR),不涉及 C# VM 侧。
> 如果需要 VM 执行,由 csimple_lang(C VM)另行实现。
>
> **2026-08-26 更新**: C VM 闭包执行已实现(共享捕获上下文方案),
> 详见第 8 章。

---

## 1. 第一版回顾与扩展目标

### 1.1 第一版已实现

| 能力 | 状态 |
|---|---|
| 具名闭包 `function f(a){...}` | ✅ |
| 匿名闭包 `var f = function(a){...}` | ✅ |
| 按引用捕获宿主局部变量 | ✅ |
| 闭包体内修改捕获变量 | ✅ |
| 直接调用 `f(args)` (f 是闭包变量) | ✅ |
| NewClosure / CallClosure IR 指令 | ✅ (FrontEnd 侧) |

### 1.2 第二版扩展项

| 扩展项 | 说明 |
|---|---|
| **Function 类型** | 新增 `Function` 内建类型,闭包变量从 `object` 改为 `Function` |
| **间接闭包调用** | 闭包作为返回值/参数/类成员变量后,通过 `Function` 类型变量调用 |
| **闭包内访问类成员** | 闭包体内可读写宿主类的静态成员变量、调用静态方法 |
| **Array.forEach** | `Array<T>.forEach(Function callback)` 方法,闭包作为回调 |
| **makeCounter 模式** | 函数返回闭包,闭包捕获的局部变量在函数返回后依然存活 |

---

## 2. Function 类型

### 2.1 为什么需要 Function 类型

第一版中闭包变量的类型为 `object`,导致以下场景无法工作:

```
static makeCounter()
{
    int count = 0;
    function counter() { count = count + 1; ret count; }
    ret counter;     # counter 类型为 object, 返回后调用方拿到 object
}

static test()
{
    var c = makeCounter();  # c 的类型为 object
    c();                     # ❌ object 不可调用, MetaCallNode 不识别
}
```

引入 `Function` 类型后:
- 闭包变量的静态类型为 `Function` (而非 `object`)
- `MetaCallNode` 检测到 `Function` 类型变量被调用时,生成 `ClosureCall`
- `Function` 可作为返回类型、参数类型、成员变量类型

### 2.2 类型系统改动

#### 2.2.1 EType 枚举 (`Front/Define.cs`)

```csharp
public enum EType : byte
{
    // ... 现有成员 ...
    Result, ResultT,
    Function,    // ← 新增
}
```

#### 2.2.2 DefaultObject 枚举 (`Front/Core/BaseMetaClass/CoreMetaClassManager.cs`)

```csharp
public enum DefaultObject
{
    // ... 现有成员 ...
    Result, ResultT,
    Function,    // ← 新增
}
```

#### 2.2.3 FunctionMetaClass (`Front/Core/BaseMetaClass/FunctionMetaClass.cs` 新文件)

```csharp
public class FunctionMetaClass : MetaClass
{
    public FunctionMetaClass() : base( DefaultObject.Function.ToString() )
    {
        SetExtendClass( CoreMetaClassManager.objectMetaClass );
        m_Type = EType.Function;
        m_InnderDefine = true;
    }

    public static MetaClass CreateMetaClass()
    {
    return new FunctionMetaClass();
    }
}
```

#### 2.2.4 CoreMetaClassManager 注册

在静态构造函数中创建并注册 `Function` 类型:

```csharp
public static MetaClass functionMetaClass;
// ...
functionMetaClass = FunctionMetaClass.CreateMetaClass();
m_DefaultMetaClassDict[DefaultObject.Function] = functionMetaClass;
```

同时更新 `GetETypeByMetaClass`、`GetMetaClassByEType`、`GetSelfMetaName` 三个方法,
添加 `Function` / `EType.Function` 分支。

### 2.3 闭包变量类型从 object 改为 Function

| 文件 | 改动点 |
|---|---|
| `MetaClosureDefineStatements.cs` | `MetaClosureVariable` 构造函数: `objectMetaClass` -> `functionMetaClass` (仅闭包变量本身) |
| `MetaClosureDefineStatements.cs` | 闭包函数返回类型: 保持 `object` 不变 (是闭包体 ret 返回值类型, 非闭包变量类型) |
| `MetaCallNode.cs` | 闭包调用返回类型: 保持 `objectMetaClass` (闭包调用返回值类型, 非闭包对象类型) |
| `MetaVisitCall.cs` | `MetaClosureCall` 返回类型: 保持 `objectMetaClass`, 未修改 |

---

## 3. 间接闭包调用

### 3.1 直接调用 vs 间接调用

| 调用方式 | 示例 | 当前状态 |
|---|---|---|
| **直接调用** | `addBase(1, 2)` — addBase 是 `MetaClosureVariable` | ✅ 第一版已实现 |
| **间接调用** | `c()` — c 是 `Function` 类型的普通变量(返回值/参数/成员) | 第二版实现 |

### 3.2 MetaCallNode 检测逻辑扩展

`MetaCallNode` 的调用类型检测(`TryResolveCall` 方法)中,第一版只检测 `MetaClosureVariable`:

```csharp
// 第一版: 只检测闭包变量
else if ( MetaClosureVariable.ResolveClosureVariable( m_MetaVariable ) != null )
{
    m_CallNodeType = ECallNodeType.ClosureCall;
    return true;
}
```

第二版增加 `Function` 类型检测:

```csharp
// 第二版: 增加 Function 类型变量的间接闭包调用检测
else if ( MetaClosureVariable.ResolveClosureVariable( m_MetaVariable ) != null )
{
    m_CallNodeType = ECallNodeType.ClosureCall;
    return true;
}
else if ( IsFunctionType( m_MetaVariable ) )
{
    // Function 类型变量被调用 -> 间接闭包调用
    m_CallNodeType = ECallNodeType.ClosureCall;
    return true;
}
```

`IsFunctionType` 判定: 变量的最终类型(`GetFinalMetaType()`)的 MetaClass 是 `functionMetaClass`。

### 3.3 MetaCallLink 生成间接闭包调用节点

`MetaCallLink` 中 `ClosureCall` 分支需要处理两种情况:

1. **直接调用**: `mcc.closureVariable` 不为 null — 第一版逻辑
2. **间接调用**: `mcc.closureVariable` 为 null — 只压变量,`CallClosure` 不携带 IRMethod

```csharp
if ( mcn.callNodeType == ECallNodeType.ClosureCall )
{
    var closureVar = MetaClosureVariable.ResolveClosureVariable( mcn.metaVariable );
    var mcc = new MetaClosureCall(
        mcn.metaVariable,
        closureVar,           // 直接调用时非 null, 间接调用时为 null
        mcn.metaInputParamCollection
    );
    // ...
}
```

### 3.4 IRMetaCallLink 间接 CallClosure IR 生成

`IRMetaCallLink` 中 `ClosureCall` 分支扩展:

```csharp
if ( cnode.visitType == MetaVisitNode.EVisitType.ClosureCall )
{
    var mcc = cnode.closureCall;

    // 1. 压闭包变量 (直接和间接都需压)
    var loadMv = mcc.loadMetaVariable ?? (MetaVariable)mcc.closureVariable;
    // -> IRLoadVariable

    // 2. 压实参表达式
    // -> IRExpressManager.CreateExpress

    // 3. CallClosure
    var closureIRM = ( mcc.closureFunction != null )
        ? IRClosureDefineStatements.ResolveClosureIRMethod( mcc.closureFunction, cnode.token )
        : null;   // 间接调用: 无编译期 IRMethod 引用

    var imc = new IRMethodCall( null, null, closureIRM, plist.Count );
    IRData dataCall = new IRData();
    dataCall.opCode = EIROpCode.CallClosure;
    dataCall.SetOpValue( imc );
    dataCall.index = plist.Count;
    irList.Add( new IRBase( dataCall ) );
}
```

间接调用时 `closureIRM` 为 null,VM 侧执行 `CallClosure` 时从栈顶闭包对象中
取出函数入口(运行时绑定),而非编译期 IRMethod 引用。

### 3.5 makeCounter 模式

```
static Function makeCounter()
{
    int count = 0;
    function counter() { count++; ret count; }
    ret counter;          # counter 是 MetaClosureVariable, 类型 Function
}

static test()
{
    var c = makeCounter();  # c 类型 = makeCounter 返回类型 = Function
    c();                    # 间接闭包调用 -> CallClosure
    c();                    # 每次调用 count 自增 (context 数组中 count 仍存活)
}
```

IR 序列:

```
; makeCounter 体内
int count = 0;
; function counter(){...}
LoadLocal count           ; 捕获值
NewClosure  <__closure_counter_N>   ; count 进 context[0]
StoreLocal counter
; ret counter
LoadLocal counter
IRReturn

; test 体内
; var c = makeCounter()
Call        <makeCounter>
StoreLocal  c             ; c 是 Function 类型
; c()
LoadLocal   c             ; 闭包对象 (间接: 无编译期 IRMethod)
CallClosure               ; index = 1 (closure + 0 实参)
; c()
LoadLocal   c
CallClosure
```

---

## 4. 闭包内访问类成员

### 4.1 问题

第一版中 `MetaClosureBlockStatements.GetMetaVariableByName` 的变量查找链:

1. 闭包体局部变量
2. 已捕获变量 (context 代理)
3. 闭包函数参数
4. 宿主函数作用域 (沿 block 链向上)

当变量不在以上 4 级中时,返回 null — **类静态成员变量无法访问**。

### 4.2 解决方案

在 `CaptureFromHost` 返回 null 后,回退到类成员变量查找:

```csharp
private MetaVariable CaptureFromHost( string name )
{
    // ... 沿宿主块链查找 (第一版逻辑) ...

    // 未在宿主作用域找到 -> 尝试类成员变量
    // 静态成员有全局生命期, 无需捕获进 context 数组, 直接返回成员变量
    var ownerClass = m_OwnerClosureDefineStatements?.closureFunction?.ownerMetaClass;
    if ( ownerClass != null )
    {
        var memberMv = ownerClass.GetMetaMemberVariableByName( name );
        if ( memberMv != null )
            return memberMv;   // 类成员, 不进 context, 直接访问
    }

    return null;
}
```

类静态成员**不进入 context 数组**,因为它们在类级别有固定存储位置,
闭包(合成静态函数)可以直接通过 `LoadStatic` / `StoreStatic` IR 访问。

### 4.3 闭包内调用类静态方法

闭包函数是宿主类的合成静态成员,调用同类中的其他静态方法时,
`MetaCallNode` 的方法解析会搜索当前类的方法列表,自然命中。
无需额外改动。

---

## 5. Array.forEach

### 5.1 新增方法

在 `Front/Lib/Core/Array.sl` 的 `Array<T>` 类中新增:

```sl
public void forEach( Function callback )
{
    for i = 0, i < this._length, i++
    {
        var item = SystemArrayGetValueThis( this, i ) as T
        callback( item )
    }
}
```

### 5.2 调用流程

```
arr.forEach( function( int i ) { ... } )
```

1. 闭包字面量 `function(int i){...}` -> NewClosure -> 压栈
2. `forEach` 方法参数类型为 `Function`,接收闭包对象
3. `forEach` 体内 `callback( item )` -> 间接闭包调用 -> CallClosure

### 5.3 IR 序列

```
; arr.forEach( function(int i){...} )
; 1. 创建闭包
NewClosure  <__closure_anon_N>   ; 无捕获值
; 2. 压实参 (arr + 闭包)
LoadLocal   arr
; 3. 调用 forEach (普通 CallStatic)
CallStatic  <Array.forEach>
; ---
; forEach 体内:
; callback( item )
LoadArgument 1            ; callback (Function 类型参数)
LoadLocal   item          ; 实参
CallClosure               ; index = 2 (closure + 1 实参)
```

---

## 6. 测试用例说明 (ClosureTest.sl)

| 案例 | 方法名 | 验证点 |
|---|---|---|
| 1 | `captureCase` | 具名闭包 + 捕获宿主局部变量(只读) |
| 2 | `anonymousCase` | 匿名闭包语法 `var f = function(a,b){...}` |
| 3 | `writeCaptureCase` | 闭包内修改捕获变量 + 闭包返回数组(`getCounts`) |
| 4 | `memberFunctionRelation` | 闭包内调用宿主类静态方法 `helperAdd` |
| 5 | `memberVariableRelation` | 闭包内读写宿主类静态成员变量 `s_counter` |
| 6 | `variableAbout` | 闭包作为变量,在循环中多次调用 |
| 7 | `returnClosureFunction` | makeCounter 模式: 函数返回闭包,间接调用 |
| 8 | `forEachCase` | `Array.forEach` + 闭包回调 |
| 9 | `returnTypeInferenceCase` | 闭包返回类型推断: 无显式类型默认 Void, 有 ret 则推断 |
| 10 | `typealiasFuncCase` | typealias 函数类型 `int Function(int,int)` + 间接调用返回类型推断 |
| 11 | `thisInClosureCase` | 实例方法闭包中使用 `this` 访问实例成员 |

---

## 7. 文件改动清单

| 层 | 文件 | 改动 |
|---|---|---|
| Enum | `Front/Define.cs` | `EType` 加 `Function` |
| MetaClass | `Front/Core/BaseMetaClass/FunctionMetaClass.cs` (新) | `FunctionMetaClass` 类 |
| MetaClass | `Front/Core/BaseMetaClass/CoreMetaClassManager.cs` | `DefaultObject` 加 `Function`; 静态构造注册; 3 个查找方法加分支 |
| MetaCore | `Front/Core/Statements/MetaClosureDefineStatements.cs` | 闭包变量类型 `object` -> `Function`; `CaptureFromHost` 加类成员回退; 返回类型默认 Void + 推断; 捕获 `this` |
| MetaCore | `Front/Core/Statements/MetaReturnStatements.cs` | 闭包 ret 语句回填返回类型 (Void -> 实际类型) |
| MetaCore | `Front/Core/MetaMemberFunction.cs` | 新增 `capturedThis` / `SetCapturedThis`; 新增闭包捕获注册表 + `GetOrAddClosureCapture` |
| MetaCore | `Front/Core/MetaCallNode.cs` | `ParseNode` 加 `Function` 类型变量检测; `this` 闭包处理; 闭包调用返回类型推断 |
| MetaCore | `Front/Core/MetaVisitCall.cs` | 未修改 (现有 `MetaClosureCall` 已支持 `closureVariable` 为 null) |
| MetaCore | `Front/Core/MetaCallLink.cs` | 未修改 (现有 `ClosureCall` 分支已支持间接调用) |
| IR | `Front/IROpEnum.cs` | 新增 `AllocClosureContext` (106) |
| IR | `Front/IR/Core/IRMethod.cs` | 宿主方法 prologue: `AllocClosureContext N` + `StoreLocal __closure_ctx__` |
| IR | `Front/IR/Core/IRVariable.cs` | Load/Store 拦截被捕获变量, 路由到共享数组槽 |
| IR | `Front/IR/IRStatements/IRClosureDefineStatements.cs` | `NewClosure` 新协议: 先压共享 ctx 数组再创建闭包; 闭包参数表首位 `__closure_context__` |
| IR | `Front/IR/Core/IRMetaCallLink.cs` | `ClosureCall` 分支: 间接调用时 `closureIRM` 为 null |
| IR | `Front/IRData.cs` | `UsesIndex` 白名单纳入数组槽访问指令 |
| Lib | `Front/Lib/Core/Array.sl` | 新增 `forEach( Function callback )` 方法 |
| Parse | `Front/Compile/Parse/StructParseToSyntax.cs` | GetOneSyntax: `function` 在语句中间不抢占主关键字; CrateFileMetaSyntaxNoKey: 识别 `[Key,Par,Brace]` 匿名闭包新模式, 旧模式报错 |
| Parse | `Front/Compile/Parse/StructParseFrame.cs` | ConsumeTypeAliasAt: 识别 `ReturnType Function(ParamType,...)` 函数类型模式; 新增 `FindFirstIdentifierLink` |
| File | `Front/Compile/FileMeta/FileMetaTypeAliasDecl.cs` | 新增 `IsFunctionType`/`FunctionReturnTypeDefine`/`FunctionParamTypeDefineList` 字段及构造函数 |
| File | `Front/Compile/FileMeta/FileMetaSyntax.cs` | `ToFormatString` 匿名闭包输出改为 `var name = function( ` |
| MetaClass | `Front/Core/BaseMetaClass/FunctionSignatureMetaClass.cs` (新) | 继承 `FunctionMetaClass`, 携带 `returnMetaType` 与 `paramMetaTypeList` |
| MetaClass | `Front/Core/BaseMetaClass/CoreMetaClassManager.cs` | `GetETypeByMetaClass`: `mc == functionMetaClass` 改为 `mc is FunctionMetaClass` |
| MetaCore | `Front/Core/MetaCallNode.cs` | `IsFunctionTypeVariable` 改为 `is FunctionMetaClass`; 间接闭包调用返回类型从签名取 |
| MetaCore | `Front/Core/TypeManager.cs` | `ResolveAllDeclaredTypeAliases` 加 `ResolveFunctionTypeAlias` 方法 |
| IR | `Front/IR/IRManager.cs` | `GetIRMetaClassByMetaType`: `FunctionSignatureMetaClass` 映射回 `functionMetaClass` 的 `IRMetaClass` |

---

## 8. C VM(csimple_lang)闭包实现 -- 共享捕获上下文方案(2026-08-26)

FrontEnd 产出的 IR 中 `NewClosure(104)` / `CallClosure(105)` / `AllocClosureContext(106)`
指令在 csimple_lang(C VM)中已全部实现。方案核心是「**捕获即共享**」:
宿主函数与所有闭包全程通过同一个 `Object[]` 上下文数组读写捕获槽位,
闭包对捕获变量的修改对宿主立即可见(反之亦然)。

### 8.1 IR 协议(完整链路)

| 位置 | IR 序列 | 说明 |
|---|---|---|
| 宿主 prologue | `AllocClosureContext N` + `StoreLocal __closure_ctx__` | 分配 N 槽 Object[] 存入隐藏局部变量 `__closure_ctx__` |
| 宿主初始化捕获变量 | `LoadConstXxx v` + `LoadLocal __closure_ctx__` + `StoreArrayIndex slot` | 声明语句的值写入共享数组 |
| 闭包定义处 | `LoadLocal __closure_ctx__` + `NewClosure funcId` + `StoreLocal 闭包变量` | 创建闭包对象并绑定共享数组 |
| 宿主体内读写捕获变量 | `LoadLocal __closure_ctx__` + `LoadArrayIndex/StoreArrayIndex slot` | `IRVariable` 拦截被捕获变量,路由到数组槽 |
| 闭包体内读写捕获变量 | `LoadArgument 0` + `LoadArrayIndex/StoreArrayIndex slot` | arg0 即共享数组(闭包参数表首位为 `__closure_context__`) |
| 闭包调用 | `LoadLocal 闭包变量` + 实参 + `CallClosure n` | 运行时绑定,间接调用无编译期 IRMethod 引用 |

### 8.2 C VM 侧实现

| 文件 | 改动 |
|---|---|
| `src/vm/vm.h` | opcode 106 `AllocClosureContext` + 名称表项 |
| `src/vm/runtime/vm_runtime.c` | `NewClosure`(104) 重写为「弹 1 个数组」协议(创建闭包对象时绑定共享上下文);106 handler 分配 Object[] 并入常量池;`CallClosure`(105) 弹参数 -> 取闭包对象 -> 重压 ctx 为 arg0 -> 执行 |
| `src/vm/vm_array.c` | Object 元素数组的装箱/拆箱:存标量经 `vm_object_new_scalar_wrapper` 装箱为包装对象指针;读出返回包装对象(etype=Object) |
| `src/vm/runtime/value_ops/runtime_value_ops.c` | `vm_pop_stack_into_vmvalue_for_runtime_object`(StoreReturn 路径)支持装箱标量拆箱 |

**装箱/拆箱约定**: 闭包捕获的值统一存于 Object[] 数组。Int32 等标量存入时装箱为包装对象;
消费端(算术运算、赋值、return)在读取时拆箱还原为原始标量类型。

### 8.3 调试记录: `Add(装箱值, 标量)` 结果丢失问题

**症状**: 10 案例中 8 通过;案例 3(`count` 恒为 0)与案例 7(`makeCounter` 的 `c()` 恒为 0)
失败 -- 闭包内 `count = count + step` 的写回不生效。

**定位手段**: 静态排查(IR 序列、StoreArrayIndex 栈序、装箱路径、CallClosure 传参)均正确后,
用 Debug 构建 + `csimple_lang.exe run --debug <module.json>` 跑逐指令追踪,
日志位于 `<csimple_lang>/build/logs/VM.txt`(注意: Release 构建下 `#if !RELEASE`
会把追踪日志编译掉,必须用 Debug 构建)。

**追踪证据**:

```
案例 1(正常): Add(i32:3, wrapper(100)) -> push_i32 103      # 右操作数是装箱值
案例 7(异常): Add(wrapper(0), i32:1)   -> push_ptr 原wrapper  # 左操作数是装箱值
```

**根因**: `runtime_value_compute.c` 中 `result` 初始化为 normalize **之前**的 `*a` 拷贝
(etype=Object 的包装对象)。数值路径 `runtime_value_assign_long_to_type(result, lr)` 按
`result` 的 etype 分发赋值 -- etype=Object 命中 `default: break` 什么也不写,
`result` 保持原 wrapper 指针不变。左操作数为装箱标量时算术结果整体丢失。
C# 原实现 `Compute(ref left, ...)` 的 by-ref 语义使 normalize 后的 left 本身就是结果基底,
C 移植时因拷贝而丢失了这一语义。

**修复**: normalize 之后补一行 `*result = left;` 恢复 by-ref 语义,
数值赋值路径看到拆箱后的标量 etype(Int32 等),结果正确写回。

### 8.4 验证结果

- `ClosureTest.sl` 11 案例全部通过(含共享可变捕获 `count = 12`、
  `makeCounter` 独立计数 `c() = 1/2/3`、`this` 捕获、返回类型推断、
  typealias 函数类型 `calc(3,7) = 21`)。
- `CSimpleVMTest1` 工程(进程内 Front 编译 -> 调 `csimple_lang.exe run`)
  Release/Debug 双配置验证通过。
- 全量回归: ProjectTest 其余测试套件无回归;唯一 Error 日志来自 DataTest
  故意的 `staticDataMisuseErrorDemo` 错误演示用例(与闭包无关)。

---

## 9. 闭包返回类型推断

### 9.1 设计

闭包函数没有显式声明的返回类型。处理策略:

1. **默认 Void**: 闭包定义时,返回类型初始化为 `Void`
2. **ret 推断**: 解析闭包体遇到 `ret expr;` 时,取 `expr` 的返回类型,
   若当前闭包返回类型仍为 `Void`,则替换 `DefineMetaType` 为实际类型
3. **同步 RealMetaType**: 闭包体解析完毕后,若返回类型已被推断,
   同步更新 `RealMetaType`
4. **闭包调用返回类型**: `MetaClosureCall` 和 `MetaCallNode` 优先使用
   闭包函数推断出的返回类型,而非固定 `object`

### 9.2 关键代码

`MetaReturnStatements.cs` -- ret 语句解析时回填:

```csharp
if( ownerFunc != null && ownerFunc.isClosureFunction )
{
    if( m_ReturnMetaDefineType != null
        && m_ReturnMetaDefineType.metaClass != CoreMetaClassManager.voidMetaClass )
    {
        var curType = ownerFunc.returnMetaVariable.defineMetaType;
        if( curType != null && curType.metaClass == CoreMetaClassManager.voidMetaClass )
        {
            ownerFunc.returnMetaVariable.SetMetaDefineType( m_ReturnMetaDefineType );
        }
    }
}
```

---

## 10. this 在闭包中的支持

### 10.1 设计

闭包函数本身是合成的 static 函数,没有天然的 `this`。要支持 `this`:

1. **判断宿主**: 闭包定义时检查宿主方法 (`m_OwnerMetaBlockStatements.ownerMetaFunction`)
   是否为实例方法 (`!isStatic && thisMetaVariable != null`)
2. **捕获 this**: 若宿主是实例方法,将宿主的 `thisMetaVariable` 作为
   context 数组的一个槽位捕获,通过 `SetCapturedThis` 存储到闭包函数上
3. **MetaCallNode 解析 this**: 当 `this` 关键字出现在闭包体内时,
   `MetaCallNode` 检测到 owner function 是闭包函数,使用 `capturedThis`
   而非 `thisMetaVariable`;若 `capturedThis` 为 null (静态方法中定义),
   报错 "闭包在静态方法中定义, 不能使用 this!"

### 10.2 关键代码

`MetaClosureDefineStatements.cs` -- 捕获 this:

```csharp
var hostFunc = m_OwnerMetaBlockStatements.ownerMetaFunction as MetaMemberFunction;
if ( hostFunc != null && !hostFunc.isStatic && hostFunc.thisMetaVariable != null )
{
    var thisProxy = new MetaClosureContextVariable( hostFunc.thisMetaVariable, m_CaptureList.Count );
    m_CaptureList.Add( thisProxy );
    m_ClosureFunction.SetCapturedThis( thisProxy );
}
```

`MetaCallNode.cs` -- this 关键字解析:

```csharp
if (mmf != null && mmf.isClosureFunction)
{
    m_MetaVariable = mmf.capturedThis;
    if (m_MetaVariable == null)
    {
        Log.AddMetaCoreLog(LID.MetaCoreAssertShowMessage, m_Token,
            "Error 闭包在静态方法中定义, 不能使用 this!");
        return false;
    }
}
```

---

## 11. 匿名闭包新语法与 typealias 函数类型 (2026-08-26 更新)

### 11.1 匿名闭包语法变更

**旧语法** (已废弃): `var f = ( int a, int b ) { ... }`

**新语法**: `var f = function( int a, int b ) { ... }`

在括号前增加 `function` 关键字,使匿名闭包与具名闭包语法统一,
避免与表达式分组括号产生歧义。

#### 解析改动

| 位置 | 改动 |
|---|---|
| `StructParseToSyntax.cs` GetOneSyntax | `function` 关键字出现在语句中间(`commonContent.Count > 0`)时不抢占主关键字,而是作为普通内容节点继续收集 |
| `StructParseToSyntax.cs` CrateFileMetaSyntaxNoKey | 识别新模式 `[Key(function), Par, Brace]`,创建 `FileMetaDefineClosureSyntax`(匿名);旧模式 `[Par, Brace]` 报错提示使用 `function` 关键字 |
| `StructParseToSyntax.cs` IsAnonymousClosurePending | 无需修改 -- `function` Key 节点和 Par 节点均进入 `commonContent`,`hasAssign` 和 `hasPar` 检查仍有效 |
| `FileMetaSyntax.cs` ToFormatString | 匿名闭包输出从 `var name = ( ` 改为 `var name = function( ` |

### 11.2 typealias 函数类型

**语法**: `typealias DefineName = ReturnType Function( ParamType, ... )`

示例:
```
typealias CalcFunc = int Function( int, int )
```

定义一个名为 `CalcFunc` 的函数类型,签名为「接受两个 `int` 参数,返回 `int`」。
所有 typealias 统一使用 `typealias DefineName = Ori Struct` 形式。

#### 解析链路

| 层 | 文件 | 改动 |
|---|---|---|
| File | `StructParseFrame.cs` ConsumeTypeAliasAt | 检测 `[IdentifierLink(returnType), IdentifierLink("Function")(parNode)]` 模式;按 Comma 切分 parNode 子节点创建参数 `FileMetaClassDefine` 列表;调用 `FileMetaTypeAliasDecl` 函数类型构造函数 |
| File | `FileMetaTypeAliasDecl.cs` | 新增 `IsFunctionType`、`FunctionReturnTypeDefine`、`FunctionParamTypeDefineList` 字段及构造函数 |
| MetaCore | `TypeManager.cs` ResolveAllDeclaredTypeAliases | 对 `decl.IsFunctionType` 分支调用 `ResolveFunctionTypeAlias`:解析返回类型与参数类型 `MetaType`,创建 `FunctionSignatureMetaClass` 并返回指向它的 `MetaType` |
| MetaClass | `FunctionSignatureMetaClass.cs` (新) | 继承 `FunctionMetaClass`,携带 `returnMetaType` 与 `paramMetaTypeList`;allName 设为 `"FunctionSig_" + aliasName` 保证 classId 唯一 |

### 11.3 FunctionSignatureMetaClass

```csharp
public class FunctionSignatureMetaClass : FunctionMetaClass
{
    public MetaType returnMetaType => m_ReturnMetaType;
    public List<MetaType> paramMetaTypeList => m_ParamMetaTypeList;
    // ...
}
```

- 继承 `FunctionMetaClass`,因此 `is FunctionMetaClass` 检查命中
- `CompareMetaClass` 对非数字非 object 类全部返回 `true`,使 `FunctionSignatureMetaClass` 变量可接受 `functionMetaClass` 闭包赋值(类型兼容兜底)
- IR 序列化时映射回 `functionMetaClass` 的 `IRMetaClass`,避免空壳类

### 11.4 类型系统适配

| 文件 | 改动 |
|---|---|
| `MetaCallNode.cs` IsFunctionTypeVariable | `mt.metaClass == functionMetaClass` 改为 `mt.metaClass is FunctionMetaClass`(兼容子类) |
| `MetaCallNode.cs` 间接闭包调用返回类型 | 从固定 `objectMetaClass` 改为:若变量类型为 `FunctionSignatureMetaClass` 则取 `returnMetaType`,否则 fallback 到 `object` |
| `CoreMetaClassManager.cs` GetETypeByMetaClass | `mc == functionMetaClass` 改为 `mc is FunctionMetaClass` |
| `IRManager.cs` GetIRMetaClassByMetaType | `tmc is FunctionSignatureMetaClass` 时返回 `functionMetaClass` 的 `IRMetaClass` |

### 11.5 测试用例

`ClosureTest.sl` 新增案例 10 (`typealiasFuncCase`):

```
typealias CalcFunc = int Function( int, int )   # 文件级

# 类内:
static CalcFunc makeCalc()
{
    var f = function( int a, int b ) { ret a * b; };
    ret f;
}

static typealiasFuncCase()
{
    var adder = function( int a, int b ) { ret a + b; };
    global.println( "adder(5,6) = " + adder(5, 6).toString() );   # -> 11

    var calc = makeCalc();
    global.println( "calc(3,7) = " + calc(3, 7).toString() );     # -> 21
}
```

- `makeCalc` 返回类型为 `CalcFunc`(`FunctionSignatureMetaClass`),闭包(`functionMetaClass`)通过 `CompareMetaClass` 兜底兼容赋值
- `calc(3,7)` 走间接闭包调用(`IsFunctionTypeVariable` -> `is FunctionMetaClass`),返回类型从签名的 `returnMetaType`(`int`)取,结果 `21` 正确
