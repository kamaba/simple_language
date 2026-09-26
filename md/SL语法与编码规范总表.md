# SL 语言语法与编码规范总表

> **用途**：AI / 开发者编码时的单点引用，免去每次全量扫描 `md/syntax/`。
> **来源**：`md/code.md` + `md/syntax/` 全部文档（约 48 篇）+ `test/BaseTest` 示例代码，已提炼去重。
> **效力**：语法声明格式与示例以本文为准（均与源码/用例核对过）；更深的语义细节见文末指向的各单篇文档。

***

## 一、编码规范（写代码的硬规则）

### 1.1 命名规范

| 规则  | 内容                                     |
| --- | -------------------------------------- |
| 1-1 | 英文单词命名，禁止拼音或无意义字母                      |
| 1-2 | 直观易懂，能描述功能或有意义                         |
| 1-3 | 禁止下划线命名法（`car_type` 错误）                |
| 1-4 | 常量、静态字段、类、结构体、非私有字段、方法 → **大驼峰**（C# 侧） |
| 1-5 | 私有字段、方法形参、局部变量 → **小驼峰**；私有字段加 `m_` 前缀 |
| 1-6 | 接口以大写字母 **I** 开头                       |
| 1-7 | 枚举以大写字母 **E** 开头                       |
| 1-8 | 函数类命名必须以大写字母                         |
| 1-9 | 函数命名小驼峰命名，参数命名尽量小驼峰命名               |
| 1-10 | 变量命名开头以小写字母，后续以小驼峰命名               |

### 1.2 SL 侧代码风格（依标准库与测试用例提炼）

| 对象                     | 风格               | 示例                                        |
| ---------------------- | ---------------- | ----------------------------------------- |
| 类 / 接口 / data / enum 名 | PascalCase       | `ClassDefineBase`、`CalcPrice`、`BookData`  |
| 方法 / 成员函数              | 小驼峰              | `virtualName()`、`riskyFunc()`、`addBase()` |
| 字段 / 局部变量              | 小驼峰              | `baseValue`、`childValue`、`firstName`      |
| 枚举成员                   | PascalCase       | `TestError1`、`Red`                        |
| 命名空间                   | PascalCase 可点号分段 | `N1.N2.N3`                                |

### 1.3 对齐与编码

- **Tab 键对齐**（VS 可设置）。
- 一行只声明一个变量（规则 2-1）。
- 类的字段声明统一放类的最前端（规则 2-2）。
- 一行代码不超屏幕宽度，超过则换行（规则 2-3）。
- 行尾分号**可选**（默认不强制；jsonc `compile.isUseForceSemiColonInLineEnd` = true 时强制）。

### 1.4 注释规范

| 规则  | 内容                                                |
| --- | ------------------------------------------------- |
| 3-1 | 公共方法用 `///` XML 注释（方法介绍、参数、返回值）；私有方法可不注           |
| 3-2 | 公共字段用 `///` XML 注释                                |
| 3-3 | 私有字段注释放代码行尾，Space 隔开：`int baseValue = 10; // 基础值` |
| 3-4 | 方法内代码块用 `//` 分段注释                                 |

SL 源码内注释（见 §三 3.6）：`#` 行注释、`#! ... !#` 块注释。

### 1.5 提交规范

- 每个 PR 只针对一项内容的改进或修复，勿合并提交。
- PR 标题尽量英文，备注内容可选中文。

### 1.6 关键字使用硬约束

1. 禁止使用关键字作任何标识符（变量/方法/类/参数名），完整清单见 §2.1。
2. `get` / `set` 是属性访问器关键字，禁作成员名和方法名（词法阶段即报错）。
3. 小写容器糖名（`map` `list` `stack` `hashset` `queue` `tuple` `array` `range`）与 Result 保留名（`error` `errmsg`）：**可作局部变量/参数名，禁止作类成员声明名**（字段/方法/getter/setter，LID 11042）。`async` 不是保留字，按普通标识符处理。
4. 循环跳出统一用 `continue` / `break`；`next` 只在 switch case 体内表示显式贯穿，勿在循环内用 `next` 代替 `continue`。
5. 优先用语法糖保持简洁：`ret`、`a ?? b`、`a?.member`、`$var` / `${expr}` 插值；同一文件内风格一致。
6. 协程中用 `await` / `spawn` / `yield` 语法糖，不要手写等价的 `Coroutine.awaitTask(...)` / `Coroutine.spawnClosureN(...)` / `Coroutine.yieldNow()`。

### 1.7 Core 库使用规范

1. Core 类型直接用短名，不写 `Core.` 前缀，不重复 import（`List<Int32>` 正确；`Core.List<Int32>` 冗余）。
2. 类型声明优先用语言类型词（`int`、`float`、`string`…），与 Core 类型类等价；精度有明确要求时用类名（`Int32`、`Float64`…）。
3. `Core.Environment` 下的类型需限定名：`Environment.env`（配 `import Core;`）或全限定 `Core.Environment.env`，不能裸用 `env`。
4. 非 Core 模块（Std / Math / Tensora 等）短名必须先 `import`，或用限定名（`Std.Console.X`）。
5. 禁止使用 Core 内部私有类（`_` 开头实现类如 `_MapStream<T>`）。
6. 使用 Core 注册的别名保持原样：`coro`（= `Coroutine`）、`Float8_E4M3`（= `Float8`），勿自行再造别名。

***

## 二、关键字总表（词法权威，来自 LexerParseToToken.cs）

### 2.1 词法保留关键字（禁作任何标识符）

**声明与结构**：`typealias`、`import`、`as`、`is`、`isnot`、`namespace`、`class`、`extends`、`interface`、`abstract`、`enum`、`data`、`dynamic`、`void`、`extern`、`bind`、`label`

**修饰符**：`public`、`protected`、`private`、`const`、`mut`、`final`、`static`、`partial`、`override`、`operator`、`params`、`tr`（Transience 保留字，当前无实际用例）

**控制流**：`if`、`elif`、`else`、`while`、`dowhile`、`for`、`in`、`out`、`switch`、`case`、`default`、`next`、`continue`、`break`、`goto`、`ret`

**异常**：`try`、`catch`、`finally`、`throw`、`throws`、`checked`、`unchecked`

**对象 / 值 / 协程**：`new`、`var`、`this`、`base`、`null`、`true`、`false`、`get`、`set`、`function`、`await`、`spawn`、`yield`

**类型词（保留，映射类型类）**：`object` `byte` `sbyte` `short` `ushort` `int` `uint` `long` `ulong` `bool` `half` `float` `double` `string`

### 2.2 非词法保留（可作局部变量名，禁作类成员名）

| 词                                                              | 说明                                                                   |
| -------------------------------------------------------------- | -------------------------------------------------------------------- |
| `map` `list` `stack` `hashset` `queue` `tuple` `array` `range` | 小写容器构造糖。糖只劫持**链首裸调用**，不影响 `obj.map(...)` 与变量引用；但作类成员名声明即报错 LID 11042 |
| `error` `errmsg`                                               | Result 机制保留名，同上禁作类成员名                                                |
| `async`                                                        | 已禁用，按普通标识符处理                                                         |

### 2.3 类型词 → 实际类型映射

| 类型词                 | 类型       | 类型词      | 类型        |
| ------------------- | -------- | -------- | --------- |
| `object` / `Object` | `Object` | `long`   | `Int64`   |
| `byte`              | `UInt8`  | `ulong`  | `UInt64`  |
| `sbyte`             | `Int8`   | `bool`   | `Boolean` |
| `short`             | `Int16`  | `half`   | `Float16` |
| `ushort`            | `UInt16` | `float`  | `Float32` |
| `int`               | `Int32`  | `double` | `Float64` |
| `uint`              | `UInt32` | `string` | `String`  |

### 2.4 运算符与特殊符号

| 符号                | 用途                              |
| ----------------- | ------------------------------- |
| `??`              | 空合并：左值为 null 取右值                |
| `?.`              | 空条件访问：对象为 null 时短路返回 null       |
| `..`              | 区间糖，构造 `Range`                  |
| `=>`              | lambda 表达式                      |
| `===` / `!==`     | 值等 / 值不等（内容比较，区别于引用等的 `==`）     |
| `? :`             | 三元条件                            |
| `@`               | 属性前缀：`@Nickname`、`@DllImport` 等 |
| `$`               | 字符串插值前缀                         |
| `#` / `#! ... !#` | 行注释 / 块注释                       |

常规算术 / 比较 / 逻辑运算符（含 `++`、`+=` 等复合赋值）同 C 系语言；`i++` / `+=` 需 jsonc `compile.isSupportDoublePlus` 开启（BaseTest 已开启）。

***

## 三、语法糖总表

| 糖                                                                                                      | 展开为 / 语义                                                                      |
| ------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------- |
| `ret`                                                                                                  | `return`                                                                      |
| `next`（switch case 体内）                                                                                 | 显式贯穿到下一个 case（不写则隐式 break）                                                    |
| `yield;`                                                                                               | `Coroutine.yieldNow()`                                                        |
| `await expr`                                                                                           | `Coroutine.awaitTask(expr)`                                                   |
| `spawn f(实参...)`                                                                                       | `Coroutine.spawnClosureN(...)`（挂起当前协程，异步执行 f）                                 |
| `static if / static elif / static else`                                                                | 编译期条件（MacroManager 求值，数据源 jsonc `global.macro` 与环境变量 `SL_MACRO_*`，未选中分支不参与编译） |
| `try? expr` / `try! expr`                                                                              | try 的表达式前缀形式                                                                  |
| `a ?? b` / `a?.member`                                                                                 | 判空合并 / 判空访问                                                                   |
| `a..b`                                                                                                 | 构造 `Range`                                                                    |
| `x => expr`                                                                                            | lambda                                                                        |
| `a === b` / `a !== b`                                                                                  | 值等 / 值不等                                                                      |
| `$var` / `$obj.member` / `${expr}`                                                                     | 字符串插值；三引号 `f"""x=${expr}"""` 仅支持 `${expr}`                                    |
| `@Nickname`                                                                                            | Core 内注册短别名：`coro` = `Coroutine`、`Float8_E4M3` = `Float8`                     |
| `map(...)` `list(...)` `stack(...)` `hashset(...)` `queue(...)` `tuple(...)` `array(...)` `range(...)` | 链首裸调用时展开为对应容器构造（`Map(...)` / `List(...)`…）                                    |

> 注意：`!if` / `!else` / `!endif`（MacroIf token）在词法层存在但**主语法未启用**，编译期条件用 `static if`。

***

## 四、语法速查（声明与语句格式，均核对过示例）

### 4.1 工程结构与入口

工程三件套：**`.sp`（入口）+** **`.jsonc`（同名同目录配置）+** **`.sl`（源码）**。

`.sp` 两种块：

```sl
import ETC1
import MFT

Project                      # 工程块：静态成员 + 函数
{
    println( txt )           # 成员函数（工程级，通过 global.println 调用）
    {
        SystemPrintln( txt )
    }

    _main_()                 # 主入口
    {
        GlobalTest.fun()
    }

    _test_()                 # 测试入口（run project.sp -test 运行）
    {
        ...
    }

    CompileBefore() { }      # 编译钩子
    CompileAfter()  { }      # 编译钩子
}

ProjectEnter                 # 工程级全局函数入口区
{
    MyFunc( int[] arr )
    {
        ...
    }
}
```

- `Project{}` 成员通过 **`global.xxx`** 访问（`global.println(...)`、`global.Pi`）。
- jsonc `global.data` 注入数据：`global.var1`、`global.arrvar1[0]`、`global.vardata2.a`。

### 4.2 作用域体系（四层）

| 层级   | 声明方式                                      | 访问方式            |
| ---- | ----------------------------------------- | --------------- |
| 函数内块 | `{ ... }`                                 | 直接访问            |
| 文件级  | `local { ... }`                           | `local.xxx`     |
| 类成员  | 类体内实例 / 静态成员                              | `this.xxx` / 直接 |
| 工程级  | `.sp` 的 `Project{}` + jsonc `global.data` | `global.xxx`    |

`local{}` 规则：

- 只能放在任何类（class/data/interface/enum）**前边**；其前只允许 `import`、`typealias`、注释。
- 编译器生成 `<FileName>_Local` 类，变量提升为实例成员，函数成为实例成员函数，初始化语句进 `__local_init__()`。
- 执行时机：类静态成员与 const 初始化之后、`_main_()` 前部，按 jsonc `compileFiles.files.priority` 顺序（数值小先执行）。
- 多文件可各自写 `local{}` 且可重名，互不冲突。

### 4.3 namespace

```sl
namespace N1
{
    namespace N2.N3          # 点号分段
    {
        Class1_2_3_1 { }
    }
}

partial N1.N2.N3.ClassN1_2_3_1 { X1 = 100; }   # partial + 限定名
```

### 4.4 class

```sl
partial ClassDefineBase
{
    public static int staticSeed = 7     # 静态字段
    int baseValue = 10                    # 默认私有

    _init_(int value)                     # 构造器（可重载）
    {
        this.baseValue = value
    }

    int get baseProp()                    # getter：返回类型在前
    {
        ret this.baseValue
    }

    string virtualName()                  # 方法（基类无需 virtual 声明）
    {
        ret "base:" + this.baseValue.toString()
    }

    ClassDefineNested { nestedValue = 31 }   # 嵌套类
}

ClassDefineChild extends ClassDefineBase          # 继承
{
    childValue = 30

    _init_(int value, int child)
    {
        base._init_(value)                        # 基类构造
        this.childValue = child
    }

    override string virtualName() { ret "child" }  # 覆写
}
```

要点：

- 类声明可省略 `class` 关键字（`ForTest { ... }` 即类）；jsonc `compile.isForceUseClassKey` = true 时强制写。
- 访问修饰符：`public` / `private` / `internal` / `projected`；其他修饰符 `static` `final` `partial` `abstract` `override`。
- setter 手写格式：`set a( int v ) { this.DA2.a = v }`（bind 冲突场景形参可无类型）。
- 实例化：`ClassDefineChild(11, 22)` 直接构造调用，或 `new()`（带泛型推断，如 `List<InterfaceClass1> listc1 = new()`）。
- 继承用 `extends`；**实现接口用** **`interface`** **关键字**（不是 implements）：

```sl
class MultiImpl extends Base interface IA, IB { }
```

### 4.5 interface

```sl
interface InterfaceClass1
{
    string interfaceFun1()               # 接口方法声明
}

ImmplementClass1_1 interface InterfaceClass1,InterfaceClass2,InterfaceClass3
{
    override string interfaceFun1() { ... }   # 实现带 override
}

public interface CalcPrice bind BookData      # 接口可 bind 数据 + 带默认实现
{
    float calc() { ret ... }
}

InterfaceClass1 c1 = ImmplementClass1_1(1,2)  # 接口类型接收实例
List<InterfaceClass1> listc1 = new()           # 接口作泛型参数
```

### 4.6 enum

```sl
enum SwitchColor extends int { Red = 1, Green = 2, Blue = 3 }     # 值枚举

enum TestError extends Error                                     # 错误枚举（用于 throw）
{
    TestError1 = { code = 1, message = "test-error" },
    TestError2 = { code = 2, message = "test-error2" }
}

for v in BridgeKind { }        # 枚举遍历；v.index / v.name / v.value 访问成员
for b3 in ESeason.values { }   # 值表遍历
```

### 4.7 data

```sl
data ScoreRule { passLine = 60, excellentLine = 90 }

const data BookData { name = "ABC", price = 10 }        # const data 禁改

data StudentRecord
{
    sid = 7, name = "..."                               # 逗号分隔字段
    anondatax = { a = 111i, b = 222i }                   # 匿名嵌套 data
    scores = [95, 88, 91]                               # 数组字面量
    profile = { grade = 3, address = { city = "..." } } # 深层嵌套
    items = [1, [2,3], { code = 7 }, ClassHolder() { ... }, DataKind.Base]  # 混合元素
}

StudentRecord b = StudentRecord(){ sid = 211i, name = "n211" }   # 具名构造
StudentRecord c = { sid = 339, name = "n339" }                    # 匿名构造
```

### 4.8 bind（数据绑定）

```sl
data BP { width = 30 }

BookC bind BookData,BP { }                # 类 bind 多个 data

bc.name = "hahah"                        # get/set 代理：直接访问被绑数据字段
bc.BookData.name = "bind-name-access"     # 限定名访问（多 bind 消歧）
```

- bind 相当于自动注入 `_+数据名` 成员（`BookC bind BookData` ≈ `BookData _BookData = new()`），并自动生成 `get name()` / `set name(...)` 等代理方法。
- 同名字段冲突时需手写 get/set：

```sl
get int a() { ret this.DA2.a }
set a( int v ) { this.DA2.a = v }
```

- `_init_` 可重载（BindExpandManager 处理）；interface 也可 bind。

### 4.9 变量与字面量

```sl
Int32 a = 20            # 类名
int b = 20              # 类型词（等价 Int32）
var x = expr            # 类型推断
Num n1 = 1.5             # 通用数值（双精度存储）
Byte b8 = 250
Int64 i64 = 900000000000
Float32 f = 1.0f
Float64 d = 1.0d
```

数值字面量后缀：

| 后缀    | 类型     | 后缀     | 类型      |
| ----- | ------ | ------ | ------- |
| `1s`  | Int16  | `1L`   | Int64   |
| `1us` | UInt16 | `1uL`  | UInt64  |
| `1i`  | Int32  | `1.0f` | Float32 |
| `1ui` | UInt32 | `1.0d` | Float64 |

- 二进制 `0b1100`（可 `_` 分隔：`0b0011_1100`）；十六进制 `0xef`。
- `Num` 不支持位运算。
- 转换方法：`a.toHexString()` / `toBinaryString()` / `toOctalString()` / `toRadixString(10)`。

### 4.10 函数、闭包与 lambda

```sl
typealias CalcFunc = int Function( int, int )      # 函数类型别名

function addBase( int a, int b )                   # 具名闭包：可捕获局部变量
{
    ret a + b + baseVal;
}

var mul = function( int a, int b ) { ret a * b; };   # 匿名闭包

function Array<int> getCounts( Array<int> arr )     # 带返回类型的具名闭包
{
    ...
    ret result;
}

var counter = makeCounter()                        # 函数可返回闭包
arr.forEach( printer )                             # 闭包作参数

var lam = x => x + 1                              # lambda
```

类内静态方法：`static string riskyFunc(bool shouldFail) throws { ... }`（见 §4.12）。

### 4.11 控制流

**if / elif / else**（条件括号可选）：

```sl
if flag { }
elif b is string str2 { }     # elif 不是 else if；支持 is 模式绑定
else { }

if (flag) { ... }            # 带括号也合法
```

**switch**（case 无穿透，`next` 显式贯穿）：

```sl
switch i
{
    case 1 { ... }
    case 2 { ... }
    default { ... }
}

case 1|2|3|4 { ... }          # 多值：| 分隔
case 10,11,12 { ... }         # 多值：, 分隔
case is ClassName { }        # 类型模式
case is ClassName obj { }     # 类型模式 + 绑定变量
case SwitchColor.Red { }      # 枚举成员
case 5 { next }               # 显式贯穿到下一 case
```

**for**（两种形态）：

```sl
# ① C 风格逗号分段（三段可省略后两段）
for i = 0, i < 10, i++ { }
for i = 0, i < 10 { }         # 两段
for i = 0 { if i > 22 { break } i++ }   # 一段（内部自控）

# ② for-in 遍历（数组 / Range / 枚举 / 字面量）
for v in arr { }
for v in [1..4] { }
for v in range(1, 10, 2) { }
for v in BridgeKind { }
```

**while / dowhile**：

```sl
while w < 50 { }

dowhile i < 3        # 先执行后判断，条件为假也至少执行一次
{
    i++
}
```

**label / goto**：

```sl
goto targetBlock          # 跳转到 label 命名块

label targetBlock
{
    ...
}

label loopStart { ... goto loopStart; }   # 后向跳转构成循环；可从 if/while 内跳出
```

循环控制：`continue` / `break`。

### 4.12 try 异常体系

```sl
enum TestError extends Error { TestError1 = { code = 1, message = "test-error" } }

static string riskyFunc(bool shouldFail) throws    # throws 声明后才能 throw
{
    if shouldFail
    {
        throw TestError.TestError1                 # throw 只能跟 enum extends Error
    }
    ret "ok"
}

label basicBlock                       # try 捕获的载体是 label 命名块
{
    try riskyFunc(true)                # try 前缀调用
}
catch
{
    ...
}
catch TestError ex                    # 带类型捕获
{
    captured = ex.toString()
}

label block { ... } catch { ... } finally { ... }   # finally 兜底
checked { ... } / unchecked { ... }    # 溢出检测上下文

var s = try? riskyFunc(true)           # try? / try! 表达式形式
```

### 4.13 range

```sl
r1 = 1..100                          # 快捷 int range ≈ range(1, 100, 1)
r2 = range( 1.0f, 200.0f, 1.0f )      # 三参含步长
Range<double> r3 = new(3.2d, 54.3d, 0.22d)   # 泛型 + new()
r4 = Range<short>( 1s, 100s, 2s )

r1.step( 1 )                          # 步长写法一：方法调用
r4.step = 2                           # 步长写法二：属性赋值
```

### 4.14 Result 机制

```sl
static Result useAutoResult()
{
    result.code = 100                  # 函数体内注入 result 变量
    result.message = "ok"
    ret result
}

static Result retValue()
{
    ret 100                            # 自动改写：result.value = 100; ret result
}

static Result<int> retValueT() { ret 200 }    # 泛型 Result

Result r = retValueT()                 # 协变赋值：Result<T> 可赋给 Result
```

- `ret 表达式` 在 Result 函数内自动改写为 `result.value = 表达式; ret result`。
- 无 ret 路径时 epilogue 兜底返回 result。
- `error` / `errmsg` 是 Result 机制保留名（禁作类成员名）。

### 4.15 运算符重载（约定方法名）

| 方法名            | 对应运算              |
| -------------- | ----------------- |
| `_add_`        | `+`               |
| `_eq_`         | `==`              |
| `_getItem_`    | 下标读 `a[i]`        |
| `_setItem_`    | 下标写 `a[i] = v`    |
| `_reloadsign_` | 正负号转换（带第三参 `"+"`） |

```sl
nums._setItem_(2, 99)     # 显式调用
m._getItem_( 1 )
```

### 4.16 容器与语义差异

**构造（含小写糖）**：

```sl
Array<Int32> arr = Array<Int32>.create(10)    # 或 Int32[] 声明
m = map()                # Map<Object,Object>；m8 = map( 8 )；mi = map<int,string>()
l = list()               # li = list<int>()
s = stack()               # st = hashset( 10 )
q = queue()
a = array( 10 )
r = range(1, 10)
```

**下标**：`arr[i]` ≡ `arr.$i`（`$` 下标糖）；`m[4] = "four"` 写、`m[2]` 读。

**语义差异表（勿混淆）**：

| 容器              | 底层 / 语义要点                                                                                                 |
| --------------- | --------------------------------------------------------------------------------------------------------- |
| `Array<T>`      | 连续存储；`fill(v)`、`current`；**不协变**（int\[] 不能赋 object\[]）                                                    |
| `List<T>`       | 底层与 Array 同构 **O(n) 非哈希**；add/insert/remove/indexOf/contains/getRange/toArray/capacity；非法下标**静默忽略**；非线程安全 |
| `Map<T1,T2>`    | 底层 O(n) 线性；**add 是 TryAdd 语义**（key 已存在不覆盖），`map[k]=v` 是 put 语义；getOrDefault/putIfAbsent；key 不存在返回 null    |
| `HashSet<T>`    | **哈希桶**；union/intersection/difference/symmetricDifference/setEquals；不存 null；顺序不保证                         |
| `Queue<T>`      | FIFO 尾进头出；enqueue/dequeue/peek/isEmpty/length                                                             |
| `Stack<T>`      | LIFO；push/pop/peek/isEmpty/length                                                                         |
| `LinkedList<T>` | **Std 模块**（`Std.LinkedList<T>`，需 import）；C 层指针操作；addFirst/addBefore/addAfter/removeAt；越界静默忽略              |
| `Tuple`         | 模板 `Tuple<T1..T8>` 与无参 Tuple 共 9 变体；`t.$N` 下标访问；**不支持匿名** **`()`** **字面量**；SQL 参数绑定容器                     |

协变例外：`IIterator<Num>` 等只读遍历接口可协变（泛型 `out` 标注）。

### 4.17 协程（协作式单线程，栈私有）

```sl
yield;                        # 让出
await taskHandle              # 等待 Task
spawn setFlagFn()             # 异步执行（挂起当前协程）

# Task 句柄：handle / awaitTask / cancel / status / isDead
# Channel<T>：协程间通道
```

- 三件套由 `StructParseToSyntax.cs` 的 `TransformCoroutineKeywordNodes` 展开为 `Coroutine.*` 调用。
- 管理器类 `Coroutine`（别名 `coro`），协程对象类 `Task`。
- **聚合 API 参数形式不一致（源码即如此，勿"统一"）**：`waitAll(params Array<Task>)` vs `waitAny(params Task[])`。
- 按**函数名** spawn 的 `spawn0..3` 已移除，现行是 `spawnClosure0..3`（糖形式 `spawn`）。

### 4.18 isolate（独立 VM 实例）

- 每个 isolate 是独立 VM，消息**深拷贝**传递；`TransferableData` 零拷贝转移。
- `Sendable` 白名单类型才能发送；闭包可发送（method\_id + context）。
- `ReceivePort` / `SendPort` 通信。
- 实参上限 255。
- 与协程差异：协程是单 VM 内协作调度，isolate 是多实例隔离。

### 4.19 类型系统

```sl
typealias CalcFunc = int Function( int, int )    # 类型别名

Int32 i = f as Int32                            # as 显式转换
String s = f?.toString()                        # 判空调用
if t is string str2 { }                         # is / isnot 类型判断（可绑定变量）
dynamic d = ...                                 # 动态类型
```

- 浮点家族：`Float8`（别名 `Float8_E4M3`）、`Float8_E5M2`、`Float16`、`Float16_Brain`、`Float32`、`Float64`。
- `Num`：通用数值抽象基类，双精度存储，不支持位运算。

### 4.20 系统方法（systemCalls）

`println` / `print` 等系统方法由 `Core.jsonc` 的 `systemCalls[]` 注册（`{name, returnType, params, isVariadic, cvmFunction}`），实现下沉在 C VM 的 `system_method_call/` 适配器。

**新增系统方法四处同步**：`.sl` 声明 + `Core.jsonc` 的 `systemCalls[].cvmFunction` + `src/vm/system_method_call/*.c` 实现 + C# `Front/Define.cs` 的 `ESystemMethodCall`。

### 4.21 性能下沉四层级

| 层级                     | 方式                             |
| ---------------------- | ------------------------------ |
| ① 纯 SL                 | 全部逻辑用 SL 写                     |
| ② FFI                  | `@DllImport` 声明 + native 库符号直调 |
| ③ systemCalls → 扩展 DLL | 注册进扩展库模块                       |
| ④ systemCalls → 主工程符号  | 注册进主工程（C VM 内直连）               |

***

## 五、Core 库类型速查（引用后直接可用）

### 5.1 可见性规则

1. `.jsonc` `references` 引用 Core 后，**Core 模块根下类型裸短名直接使用，无需 import**（`List<Int32>`、`Object`、`Stream<T>`）。
2. `Core.` 前缀限定名总是可用（`Core.List<T>`、`Core.IIterable<T>`）。
3. 嵌套命名空间（Environment）需限定名：`Core.Environment.env` 或 `import Core;` 后 `Environment.env`（裸 `env` 不可用）。
4. 非 Core 模块（Std / Math / Tensora 等）短名必须 `import`，限定名（`Std.Console.X`）可直接用。

### 5.2 类型清单

| 分类     | 类型                                                                                                                                               |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------ |
| 根      | `Object` `Void` `Boolean` `Type` `MetaClass` `Member` `Data`(abstract) `Num`(abstract)                                                           |
| 整数     | `Int8` `Int16` `Int32` `Int64` `UInt8` `UInt16` `UInt32` `UInt64`                                                                                |
| 浮点     | `Float8`(别名 `Float8_E4M3`) `Float8_E5M2` `Float16` `Float16_Brain` `Float32` `Float64`                                                           |
| 其他值    | `String` `Range<T:Num>` `Guid` `Random` `Ptr` `Ptr<T>` `Memory` `Result` `Result<T>` `Tuple`(9 变体)                                               |
| 容器     | `Array<T>` `List<T>` `Map<T1,T2>` `HashSet<T>` `Queue<T>` `Stack<T>` `LinkedList<T>` `StringBuilder`                                             |
| 树/表    | `Tree<T>` `TreeNode<T>` `BinaryTree<T>` `BinarySearchTree<T>` `BinaryNode<T>` `Table`                                                            |
| 文本/序列化 | `BaseJson` `JsonValue` `Json` `BaseCsv` `BaseToml` `BaseYaml` `Serialize` `Utf8Codec` `JsonCodec<T>` `BinaryCodec` `ProtocalBuffers`(Pb 系) `Lz4` |
| 接口     | `IClone` `IClone<T>` `IIterator` `IIterable` `IIterator<out T>` `IIterable<T>` `IList<T>` `IMap<T1,T2>` `Iterater` `Iterater<T>`                 |
| 协程并发   | `Coroutine`(别名 `coro`) `Task` `CoroutineStatus` `CoroutineBlockReason` `Channel` `Channel<T>`                                                    |
| 字节     | `ByteBuffer` `ByteStream`(abstract) `MemoryStream` `LengthPrefix`                                                                                |
| Stream | `Stream`(abstract) `Stream<T>`(abstract) `StreamIterator<T>` `StreamSubscription` `StreamController<T>` `StreamSink<T>` `StreamTransformer<S,T>` |
| Codec  | `ChunkedConversionSink` `Converter<S,T>` `Codec<S,T>` `ProtoCodec` `ProtoCodec<T>`                                                               |
| 其他     | `Attribute` `Nickname` `AOT` `NativeBridge` `Error` 及 `CoreError` `MathOpError` `BufferError` `StreamIOError` `SeekOrigin` `Lz4Error` 等错误枚举      |

> `_` 下划线开头的实现类（如 `_MapStream<T>`）是 Core 内部私有类，禁用。

### 5.3 命名空间

- **`Core`（模块根）**：`Object`、`List<T>` 等直接挂根下，引用后裸短名可用。
- **`Core.Environment`**：`current`、`env` / `custom` / `sys` / `legacy`、`OSVersionNumber`、`probe` / `Probe*` 系列、`Cpu` / `Ai` / `Render` / `Network` / `Script` / `Embedded` / `Device` / `DeviceInfo` / `Override`。
- **`Core.Environment.Platform`**：编译期平台定义枚举与常量：`os` `osVersion` `form` `arch` `cpu` `isa` `device` `ai` `render` `shaderModel` `network` `link` `embedded` `rtos` `runtime` `build` `endian`，及 `DefItem` / `DefKind` / `defs`。

***

## 六、冲突、限制与已知坑（编码前扫一眼）

| #  | 事项                                                                                                                  |
| -- | ------------------------------------------------------------------------------------------------------------------- |
| 1  | **实现接口用** **`interface`** **关键字**（`class C extends Base interface IA, IB`），不是 implements；个别旧文档写 implements，以本文与用例为准 |
| 2  | `virtual` 关键字跨文档冲突（class.md 有用例 vs object.md 禁用）；实测基类方法**不写 virtual 也可直接 override**，建议不写                            |
| 3  | `Map.add` 是 **TryAdd 语义**（key 已存在不覆盖），覆盖用 `map[k] = v`                                                              |
| 4  | `List` / `Map` 底层**非哈希**（Array 同构 O(n)）；只有 `HashSet` 是哈希桶                                                           |
| 5  | 容器**不协变**：`int[]` 不能赋 `object[]`；只读 `IIterator<out T>` 遍历可协变                                                        |
| 6  | `waitAll(params Array<Task>)` 与 `waitAny(params Task[])` 参数形式不一致是**历史现状，勿统一**（会破坏 SL 侧调用点）                          |
| 7  | 容器糖名 / `error` / `errmsg` 禁作类成员声明名（LID 11042）；局部变量与参数不受限                                                            |
| 8  | `next` 只用于 switch 贯穿；循环内用 `continue` / `break`                                                                      |
| 9  | `!if` / `!else` / `!endif` 词法存在但未启用，编译期条件用 `static if`；`async` 已禁用                                                  |
| 10 | `LinkedList<T>` 在 **Std 模块**，用前 `import Std;`                                                                       |
| 11 | `throw` 只能抛 `enum extends Error`，且函数需 `throws` 声明；try 捕获载体是 `label{} catch{}`                                       |
| 12 | `Tuple` 不支持匿名 `()` 字面量，须用构造                                                                                         |
| 13 | `Num` 不支持位运算                                                                                                        |
| 14 | `Map` key 不存在返回 `null`（非报错）；`List` 非法下标**静默忽略**                                                                     |
| 15 | `tr` 是保留字（当前无实际用例），禁作标识符                                                                                            |
| 16 | 行尾分号默认可选；`i++` / `+=` 需 jsonc `compile.isSupportDoublePlus` 开启                                                      |

***

## 附：本文未覆盖、需查单篇文档的主题

| 主题                       | 文档                                         |
| ------------------------ | ------------------------------------------ |
| 深入排障 / 产物结构              | `md/ai/故障排查流程.md`、`md/ai/EXPORT_PATHS.md`  |
| jsonc 工程配置全量关键字          | `md/project/project-config-jsonc-guide.md` |
| `.sp` 入口与 global.data 细节 | `md/project/project_sp-guide.md`           |
| Environment 平台环境全量       | `md/project/environment-guide.md`          |
| FFI 落地用法                 | `md/project/ffi.md`                        |
| 协程权威（实现版）                | `md/syntax/coroutine.md`                   |
| 各语法单篇语义细节                | `md/syntax/<特性>.md`                        |
| 测试集 ↔ 宿主工程对照             | `md/project/test-guide.md`                 |

