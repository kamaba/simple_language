# inline 内联体（`=>` 内联表达式 + `inline` 方法修饰符）

## 概述

内联体提供一种**轻量、零分配的局部计算**机制，两种等价形态：

1. **`=>` 内联表达式**：局部别名，`var add = (a, b) => a + b`——定义点只记录「参数名 + 表达式体」，调用点把表达式**就地展开**（形参替换为实参），**不创建闭包对象、不进入调用函数、无函数帧**。
2. **`inline` 方法修饰符**：方法/函数声明上加 `inline` 关键字。**语义层完全按普通函数规则解析**（形参可传 `object`、遵守函数体的全部检查规则、走正常调用解析），仅在 IR 生成阶段把函数体**替换到调用点局部**，不发射 `Call` 指令。

```sl
var add = (a, b) => a + b;         # 形态一：内联别名
var r1 = add(1, 2);                # 编译期就地展开为 1 + 2，无函数帧

static inline Int32 add2( Int32 a, Int32 b )    # 形态二：inline 方法
{
    ret a + b
}
var r2 = add2(1, 2);               # IR 生成时体替换到调用点局部，无 Call 指令
```

两者与 `function` 闭包是**互补**而非替代：闭包用于回调 / 异步 / 状态持有；内联体用于"一个计算、就地用一下"。全部机制在**编译期**完成，运行时无新 opcode。

设计文档（含完整推导与跨语言调研）：`csimple_lang/md/design/INLINE_LAMBDA_DESIGN.md`。

---

## 一、`=>` 内联表达式

### 语法

```sl
var <name> = ( <param> [, <param> ...] ) => <expression> ;

# <param> 两种形态 (逐参可选, 可混用):
<name>               # 裸名: 类型由调用点实参推导
<Type> <name>        # 类型标注: 定义点解析, 调用点校验实参类型
```

- 右侧**必须是单个表达式**，不能是 `{ }` 语句块（语句块属于 `function` 闭包）。
- 参数可为裸名或**带可选类型标注**，逐参独立、可混用：`(Int32 a, b) => a * b`——`a` 带 `Int32` 标注、`b` 裸名由实参推导。单参也必须带括号：`(x) => x * x`。
- 参数名与当前作用域局部变量重名时，以参数优先（展开时做 alpha 替换）。
- 参数**不支持默认值表达式与 `params` 可变参数**（展开时实参必须全部提供，见下「参数可选类型标注」）。

### 语义（不进入调用函数）

1. **定义即记录，不建对象**：`MetaCore` 层创建一个内联别名变量（伪类型 `InlineLambda`），仅记录「参数名列表 + 表达式体 AST」；不生成合成函数、不分配 `NewClosure`、不创建 `Function` 类型对象。
2. **调用即内联替换**：遇到 `f(args)` 且 `f` 是内联别名时，不插入任何调用，而是把表达式体**就地展开**到调用点：
   - 形参被替换为对应实参表达式（多次使用时经临时局部避免重复求值）；
   - 体内引用的**外层变量原样保留**——内联发生在调用点，外层变量在该点本就可见，直接 `LoadLocal`。
3. **无独立帧**：`=>` 没有自己的调用帧 / context，因此承载不了需要真实帧的 `spawn` / `isolate` / `await` / `yield`（见禁止项）。
4. **类型标注即契约**（M3）：带标注形参在调用点做实参类型校验——实参须是标注类型的**子类或相等**（与赋值兼容规则一致）；校验在体展开前独立进行，未标注槽跳过。

展开后 IR 与手写表达式完全一致（`LoadConst` / `Add` / 普通调用等），零运行时开销。

### 合法示例

```sl
var square = (x) => x * x;
var r1 = square(5);                 # 展开为 5 * 5

var g = (a, b) => a + b * 2;
var r2 = g(3, 4);                   # 展开为 3 + 4 * 2

# 体内可引用外层局部变量：内联在调用点，外层变量天然可见
var base = 10;
var shift = (n) => n + base;
var r3 = shift(5);                  # 展开为 5 + base

# 三元、括号优先级、实参为表达式
var maxOf = (a, b) => a > b ? a : b;
var cal = (a, b, c) => (a + b) * c;
var r4 = cal(1 + 2, 3 * 4, 2);

# 体内可调用普通函数/方法，也可调用另一个内联 lambda（无环即可）
var dbl = (x) => g(x, x);
var r5 = dbl(7);

# 同一别名多处调用
var r6 = g(10, 20) + g(100, 200);

# M3 类型标注：双参 Int32 标注，调用点校验实参类型
var typedAdd = (Int32 a, Int32 b) => a + b;
var r7 = typedAdd(3, 4);            # 展开为 3 + 4

# 标注可逐参混用：a 标注 + b 裸名（裸名槽跳过校验）
var mixed = (Int32 a, b) => a * b;
var r8 = mixed(6, 7);               # 展开为 6 * 7

# object 标注：放行一切子类实参（赋值兼容规则）
var asObj = (object o) => o;
var o1 = asObj(42);                 # Int32 → object 兼容
var o2 = asObj("str");              # String → object 兼容
```

### 参数可选类型标注（M3）

```sl
var typedAdd = (Int32 a, Int32 b) => a + b;    # 双参标注
var typedDbl = (Int32 x) => typedAdd(x, x);     # 体内嵌套调用另一带标注的内联 lambda
var mixed    = (Int32 a, b)      => a * b;     # 混用：一参标注 + 一参裸名
var greet    = (String name)     => "hello " + name;
var asObj    = (object o)        => o;          # object 标注放行一切子类
```

- **逐参可选**：每个参数独立决定是否标注，可混用 `(Int32 a, b)`；未标注槽由实参推导（同 M1 行为），校验时跳过。
- **调用点校验**：标注槽在调用点比对实参类型——实参须是标注类型的**子类或相等**（与赋值兼容规则一致，`TypeManager.CompareMetaType` 默认 Out 协变）。`(object o) => o` 放行 `Int32` / `String` 等一切子类实参。不匹配即编译错误（LID 21456）。
- **校验独立性**：实参类型经独立表达式节点解析取得，不影响体展开本身（实参随体展开后还会正常解析一次）。
- **类型解析路径**：与闭包参数同一路径（`MetaDefineParam.ParseMetaDefineType`），支持模板函数体内 `(T x) => ...` 查找函数模板形参；解析失败的标注槽宽容置空（体内使用处连锁报错，调用点校验跳过该槽）。
- **返回类型 = 体类型**：`f(args)` 的类型即表达式体类型（M1 既有行为，标注不影响）。
- **不支持**（StructParse 层拦截，编译错误）：
  - **默认值表达式**：`(Int32 a = 10) => ...`——展开时实参必须全部提供，默认值无从生效；
  - **`params` 可变参数**：`(params Int32[] a) => ...`——变长实参与形参替换机制冲突。

---

## 二、`inline` 方法修饰符

### 语法

```sl
# 静态成员方法 + inline：无 this，调用点就地展开为纯计算
static inline Int32 add( Int32 a, Int32 b )
{
    ret a + b
}

# 实例成员方法 + inline：体内可引用 this / 实例字段
inline Int32 dot( Vec2 o )
{
    ret this.x * o.x + this.y * o.y
}
```

- `inline` 是**方法修饰符**，与 `public` / `static` / `final` 同级、顺序无关，位于返回类型之前。
- 适用于**静态成员方法 / 实例成员方法 / 顶层模块函数**，调用规则与普通方法完全一致（`obj.m(...)` / `m(...)`）。
- 方法体**顶层语句 ≤ 10 条**（防代码膨胀；嵌套子语句不单独计数，21453）。
- **不允许 `throws` 标签**（21457）：inline 是调用点体替换，`throws` 声明的异常传播依赖被调方法的调用帧边界，两者语义冲突，组合禁止。
- **不允许在变量初始化器处直接调用**（21458，字段 / enum 成员 / data 成员初始化，`EParseFrom.MemberVariableExpress` 上下文）：IR 层体展开依赖宿主函数语句流，初始化器不在函数体内；只允许在其它函数体内调用。

### 语义（语义层普通函数 + IR 层体替换）

**语义层（MetaCore）不做任何特殊处理**，`inline` 调用按普通函数规则解析：

- 形参类型检查照常，**可以传 `object`**；
- 函数体的全部检查规则照常执行（含定义点违禁扫描，见下「禁止项」）；
- 参与正常调用解析与类型检查，`inline` 方法可再调用普通方法 / 另一个 `inline` 方法（无环即可，嵌套 inline 递归展开）。

唯一的特殊处理发生在 **IR 生成阶段**（`IRCallFunction.Parse`，实参求值之后、`Call` 指令发射之前拦截 `isInline` 标记）：把 inline 函数体的内容**替换到调用点局部**，不生成 `Call` 指令：

- **形参绑定**：为每个形参在调用方 `IRMethod` 注册局部槽，已求值的实参**逆序 `StoreLocal`** 绑定（实参只求值一次）；
- **接收者绑定**（实例方法）：receiver 存入专用槽，体内 `this` / 实例成员引用经该槽解析——内联后接收者在调用点作用域内天然可见；
- **体内局部 var**：预注册进调用方局部表，按 `MetaVariable` 哈希绑定（不同变量对象天然无名字冲突，无需 alpha 重命名）；
- **`ret expr` 改写**：求值 `expr` → `StoreLocal` 结果槽 → 跳展开段段尾（**不跳函数末尾**）；多条 `ret` 均改写为同一模式；函数无显式 `ret` 的自然结束路径由**兜底 epilogue**（`[Load result][StoreLocal 结果槽]`）补齐——`Result` 返回类型的 `result` 变量语义完整保留；
- **段尾**：非 `void` 方法统一 `LoadLocal` 结果槽，展开段最终栈效果与 `Call` 指令严格等价（弹 N 实参压 1 返回值；`void` 不压值）。

返回值类型支持：**任意类型**——含 `void`（不压值）、基础类型（`Int32`/`double`…）、`Result` / `Result<T>`（自动注入的 `result` 变量、`ret 值` 改写、无显式 `ret` 兜底路径全部生效）、实例方法（`this` 槽绑定）、链式调用（`v1.len2().toString()`）。

### 实例 inline 隐含 final

`inline` 展开按**静态绑定**（调用点直接把声明处的体替换进来）。若允许子类 override，调用点展开的永远是声明处版本，会破坏多态语义。因此**实例方法加 `inline` 即隐含 `final`**：不可被子类 override（复用现有 final 检查，override 时编译错误）。

### 示例

```sl
InlineMath
{
    static Int32 c1 = 110
    static Int32 c2 = 220

    static inline Int32 add( Int32 a, Int32 b )
    {
        ret a + b
    }

    static inline double avg( double a, double b )
    {
        ret (a + b) / 2.0d
    }

    static inline Result calcResultMix( bool flag )
    {
        if flag
        {
            result.code = 1
            ret result                    # 显式 ret：跳段尾
        }
        result.code = 2                    # 无 ret 兜底路径：段尾 epilogue 补结果
    }
}

Vec2
{
    Int32 x = 0
    Int32 y = 0

    inline Int32 dot( Vec2 o )             # 实例 inline：隐含 final
    {
        ret this.x * o.x + this.y * o.y    # this → 调用点 receiver 槽
    }
}

# 调用点
Int32 s = InlineMath.add(1, 2)             # 展开为 1 + 2（无 Call）
double m = InlineMath.avg(1.0d, 2.0d)      # 展开为 (1.0 + 2.0) / 2.0d
Result r = InlineMath.calcResultMix(true)   # 体内分支/ret/result 兜底照常
Vec2 v1 = new(3, 4)
Int32 d = v1.dot(new(1, 2))                # receiver v1 存槽，this → v1
global.println(v1.dot(new(1, 2)).toString())   # 链式调用天然支持
```

### 自动 inline（`-O` 优化选项触发）

除显式 `inline` 声明外，编译器还会按 **IR 优化等级**（CLI `-O1` / `-O2` / `-O3`，默认 `-O1`）**自动**给满足条件的小函数打 inline 标记，此后走与显式 `inline` 完全相同的 IR 体替换路径：

| 优化等级 | 自动标记条件（体内顶层语句数，嵌套子语句不计数） |
|----------|---------------------------------------------|
| `-O1`（默认） | ≤ 2 条 |
| `-O2` | ≤ 4 条 |
| `-O3` | ≤ 7 条 |

- **前提**：函数非 `throws`（同 21457 的语义约束）且有非空方法体。
- **排除集**（命中即不自动标记，静默跳过、**不产生编译错误**）：构造 `_init_` / abstract / 接口方法 / override 链上的实例方法（自身 override 或被子类 override）/ operator 运算符方法 / get·set 访问器 / 闭包合成函数 / 模板函数 / ref 导入函数 / 体内含违禁构造（21449-21452 同款扫描口径的静默预检不过）。完整清单与理由见 `md/project/optimization.md`。
- **实例方法自动标记同样隐含 `final`**（静态绑定语义，与显式 `inline` 一致）；override 链上的方法已在排除集中。
- **在变量初始化器处调用的差异**：显式 `inline` 命中 21458 编译错误；自动 inline 的函数在该场景**自动回退为正常 `Call` 指令**（初始化器无宿主函数语句流，无法展开——优化降级，语义不变），其余函数体内的调用点照常展开。
- 与显式标记的关系：显式 `inline` 的定义点检查（21453 规模 / 21457 throws / 违禁扫描）命中即报错；自动标记则是**优化机会**——不满足条件就不标记，绝不为用户制造新的编译错误。

```sl
# -O2 下无需任何声明，以下两个顶层小函数在调用点自动展开（无 Call 指令）：
static Int32 sum( Int32 a, Int32 b )          # 1 条 ≤ 4
{
    ret a + b
}
static Int32 sum3( Int32 a, Int32 b, Int32 c )  # 2 条 ≤ 4
{
    var t = a + b
    ret t + c
}
Int32 r = sum(1, 2) + sum3(3, 4, 5)           # 两处调用点均展开为体内语句
```

---

## 禁止项与错误码（两种形态通用）

内联体不是真实函数帧，凡"依赖调用帧 / 控制流逃逸 / 资源与异常帧 / 无限展开"的构造都不允许。**`=>` 形态在展开点检查，`inline` 方法形态在定义点静态扫描（`ScanInlineMethodBody`）**；命中即**编译错误**（MetaCore 层，不进运行时）：

| 类别 | 禁止项 | LID 错误码 | 理由 |
|------|--------|-----------|------|
| 帧依赖 | `spawn` / `await` / `yield`（含脱糖产物 `Coroutine.spawnClosure*` / `Coroutine.awaitTask`） | 21449 | 需真实协程帧；内联无帧 |
| 帧依赖 | `isolate`（含 `Isolate.*` 脱糖产物 `SystemIsolateRun`） | 21449 | 跨隔离区需真实调用边界 |
| 控制流逃逸 | `break` / `continue` / `goto` / `ret` / `out`（目标在体内自身控制流之外） | 21450 | 内联后无方法边界，跳转/返回目标失效 |
| 异常帧 | `try` / `try?` / `try!` / `catch` / `finally` | 21451 | 异常帧与内联拼接不兼容 |
| 自引用 | 递归 / 自调用 / 互递归 | 21452 | 内联无限展开 |
| 规模 | `inline` 方法体顶层语句 > 10 条 | 21453 | 防代码膨胀（仅 `inline` 方法形态） |
| 声明互斥 | `inline` + `throws` 标签组合 | 21457 | 体替换与异常帧语义冲突（仅 `inline` 方法形态） |
| 调用位置 | 在变量初始化器（字段 / enum 成员 / data 成员）处直接调用 inline 方法 | 21458 | IR 体展开依赖宿主函数语句流，初始化器不在函数体内；只允许在其它函数体内调用（仅 `inline` 方法形态） |
| 类型校验 | 调用点实参类型与形参类型标注不匹配（实参非标注类型的子类且不相等） | 21456 | 类型安全；与赋值兼容规则一致（仅 `=>` 形态带标注时，标注槽） |
| 形参语法 | 参数默认值表达式 / `params` 可变参数 | —（StructParse 层拦截） | 展开时实参必须全部提供；变长与形参替换机制冲突（仅 `=>` 形态） |

另有一类**跨边界**限制由类型系统拦截：内联别名的类型是编译器内部伪类型 `InlineLambda`，与 `Function` **不兼容**，因此以下写法均为编译错误（类型不匹配）：

```sl
var fn : Function = (x) => x + 1;    # 赋给 Function 类型
ret (x) => x + 1;                    # 作为返回值
arr.forEach((x) => x * 2);           # 作为实参跨函数边界
```

> `=>` 仅支持**同作用域直接调用**；需要跨函数边界传递时请改用 `function` 闭包。（`inline` 方法本身是具名声明，不存在此问题。）

**允许**的项（普通函数/表达式范畴）：普通函数/方法调用（含实参传 `object`）、运算符、字面量、外层变量、三元表达式、数组/容器访问、`if`/`for`/`while`/`switch` 等纯控制流（体内自身循环的 `break`/`continue` 允许）、局部 `var` 声明、多条 `ret`、调用另一个内联体（无环）。

---

## 与 function 闭包的边界

| 维度 | `function` 闭包 | `=>` 内联表达式 | `inline` 方法修饰符 |
|------|------|------|------|
| 写法 | `var f = function(int a){ ... }` | `var f = (a) => expr` | `static inline Int32 add(...)` |
| 右侧/体形态 | `{ }` 语句块 | 单表达式（无 `{ }`） | 多语句体（≤ 10 条顶层语句） |
| 语义层 | 生成合成静态函数 + context | 内联别名变量（伪类型） | **普通函数规则**（形参可传 object、体检查照常） |
| 调用机制 | 合成静态函数 + context + `CallClosure` | 调用点表达式替换，无函数帧 | IR 生成时体替换到调用点局部，**无 `Call` 指令**（栈效果与 `Call` 严格等价） |
| 运行时分配 | `NewClosure` 对象 + 共享数组 | 无 | 无 |
| 捕获 | 支持（按引用，跨返回存活） | 无捕获语义；外层变量在调用点作用域内自然可见 | 无捕获语义；形参/this/体内 var 经调用方局部槽绑定 |
| `ret` | 返回闭包自身函数 | 无（表达式即值） | 改写为结果槽存储 + 段尾跳转；多 `ret` / 无 `ret` 兜底均支持 |
| `spawn` / `isolate` / `await` | 支持 | 禁止（21449） | 禁止（21449） |
| 递归 | 支持 | 禁止（21452） | 禁止（21452） |
| override（实例） | —（闭包无此概念） | — | **隐含 final，不可 override** |
| 跨边界传递 | 支持（Function 类型） | 不支持（伪类型拦截） | 支持（普通方法引用，内联只发生在直接调用点） |
| 适用场景 | 回调、异步、持有状态 | 轻量局部计算别名、DSL 式表达式 | 性能敏感的小型方法（getter/数学运算/Result 构造等） |

语义分流点：`=>` 形态在 StructParse 层（`=` 后遇 `(` 时——紧跟 `=>` + 表达式走内联路径；`function` 关键字 + `{ }` 走闭包路径）；`inline` 修饰符在方法声明解析中与 `static` 等 token 并列识别（`ETokenType.Inline`），仅打 `isInline` 标记，语义层不动。

---

## 测试

| 工程 | 内容 | 预期 |
|------|------|------|
| `test/Other/InlineLambdaTest/` | `=>` 13 个正例：双参/单参/表达式实参/嵌套调用/三元/var 接收/括号优先级/多处调用 + 类型标注 5 项（双参 `Int32` / 标注 lambda 嵌套调用 / 混用裸名 / `String` 标注 / `object` 标注放行） | 编译通过，C VM 运行输出全对 |
| `test/Other/InlineLambdaForbiddenTest/` | `=>` 8 个负例：spawn / await / Isolate.run / 自引用 / 互递归 / try? / 实参类型与标注不匹配（21456）/ 参数默认值表达式（StructParse 层拦截）；`inline` 方法 3 个负例：体 > 10 条语句（21453）/ `inline` + `throws` 组合（21457）/ 字段初始化器调用 inline 方法（21458） | 编译失败，命中 21449 / 21451 / 21452 / 21453 / 21456 / 21457 / 21458 / 默认值拦截 |
| `test/Other/InlineMethodTest/` | `inline` 方法 16 项用例：静态多参/表达式实参/嵌套 inline/maxOf/void/double/Result 四场景（显式 `ret result`/`ret 值`改写/无 `ret` 兜底/混合路径）/`Result<int>` 泛型/Vec2 实例 inline（带参 dot/无参 len2）/链式调用 | 编译通过，C VM 16 项输出全对；IR 确认真实展开（无 `Call` 指令） |
| `test/Other/AutoInlineTest/` | 自动 inline（`-O` 触发）：O1≤2 / O2≤4 / O3≤7 各档位小函数展开验证；排除集（throws / override 链 / 初始化器调用回退 Call） | 各档编译通过，运行输出与 `-O0` 完全一致；命中档位的调用点 IR 无 `Call` 指令 |

---

## 实现范围备注

当前已落地：

- `=>` 局部形态：单表达式体、就地展开、全部禁止项拦截（M1 + M2）。
- `inline` 方法修饰符（M1b）：静态/实例方法（实例隐含 final）、多语句体、多 `ret`、任意返回类型（`void` / 基础类型 / `Result` / `Result<T>`——`result` 变量、`ret 值` 改写、无显式 `ret` 兜底 epilogue 全部生效）、嵌套 inline、链式调用、语义层普通函数规则（形参可传 `object`）；三条形态限制（M2+）：体 ≤ 10 条顶层语句（21453）、不允许 `throws` 标签（21457）、不允许在变量初始化器处调用（21458）。
- 参数可选类型标注（M3）：逐参可选（可混用 `(Int32 a, b)`）、调用点实参类型校验（实参须为标注类型的子类或相等，`object` 标注放行一切子类，LID 21456）、未标注槽由实参推导；类型解析与闭包参数同路径（支持模板函数形参）；默认值表达式与 `params` 在解析层拦截；返回类型 = 体类型。
- 自动 inline（M4，`-O` 优化选项）：`-O1` ≤2 / `-O2` ≤4 / `-O3` ≤7 条顶层语句自动标记；前提非 throws、有体；排除集（构造 / abstract / 接口 / override 链 / operator / get·set / 闭包 / 模板 / ref 导入 / 违禁构造）静默跳过不报错；实例方法同样隐含 final；变量初始化器处调用自动回退正常 `Call`。详见 `md/project/optimization.md`。

设计文档 §8 全部里程碑（M1 / M1b / M2 / M3 / M4）已落地，无后续规划项。
