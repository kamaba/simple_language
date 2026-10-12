# typealias (类型别名)

## 概述

`typealias` 用于为一个类型声明一个新的别名名称（类似 Swift 的 `typealias`）。别名只发生在 **编译期**，不会生成新的运行时类型。

该特性用于简化长泛型类型、容器类型或嵌套类型的书写，并提升可读性。

完整可运行示例见探针工程：`test/Other/AliasProbe/`（AliasProbe.sp + AliasProbeLib.sl）。

---

## 语法

两种形式：

```sl
# 1. 普通类型别名
typealias AliasName = TypeExpression

# 2. 函数类型别名
typealias AliasName = ReturnType Function( ParamType, ParamType, ... )
```

普通类型别名示例：

```sl
typealias ArrayByte = Array<Byte>
typealias Map2String = Map<string, string>
typealias IntList = List<int>
```

函数类型别名示例：

```sl
# 声明一个"接收两个 int、返回 int 的函数"的别名
typealias LibFuncTA = int Function( int, int )
```

---

## 允许的书写位置

`typealias` 允许出现在两类位置（均为"声明区"，不允许出现在函数体/方法体内）：

### 1. 文件级（`.sl` / `.sp` 均可）

写在文件**顶级**，且只能位于 `import` / `local` 声明之后、首个 `namespace` / `class` / `data` / `enum` 定义之前：

```sl
file:MyLib.sl
import Std
import AliasNS.Sub as SUB

typealias LibIntList = List<int>     # ← import 之后、类定义之前
typealias LibAliasAlpha = SUB.Alpha

public class MyLib
{
    ...
}
```

文件级别名仅在本文件内可见（解析优先级最高）。

### 2. 工程级（仅 `.sp` 的 `Project` 类体内）

写在 `.sp` 工程文件的 **`Project` 类体** 中（注意：是类体直接成员，不是 `Global(){} `函数体内），对工程内所有源文件可见：

```sl
file:My.sp
typealias SpIntList = List<int>      # ← .sp 顶级（属于 Project 声明区）
typealias SpAliasAlpha = AliasNS.Sub.Alpha

Project
{
    typealias SpKV = Map<string,string>   # ← Project 类体内
}
```

工程级别名在所有源文件完成类结构整合后统一解析，且按**依赖顺序多轮解析**——别名可以引用其它别名（包括定义在别处的别名）。

### 禁止的位置

- 类的方法体 / `Global()` 等函数体内部
- `.sl` 文件的 `namespace` / `class` / `data` / `enum` 之后（文件级声明区结束）
- 非 `.sp` 工程文件的类体内（报错：仅允许出现在 .sp 工程的 Project 类中）

---

## 解析与查找链

编译器解析类型名时按以下顺序查找 `typealias` 别名（命中即停止）：

```
当前文件局部别名 → 工程级别名(.sp Project) → 内置全局别名
```

- 别名是**编译期符号替换**：`AliasName` 等价于直接书写目标类型，不产生新的 `MetaClass` / `RuntimeClass`。
- 工程级与文件级重名时，文件级优先；同名工程级别名只有首个生效（后续静默忽略）；内置全局别名不可被同名遮蔽。

---

## 目标类型的支持形态

`TypeExpression` 支持：

| 形态 | 示例 |
|------|------|
| 基本类型 | `int`、`string` |
| 容器泛型（**实参必须写全**） | `List<int>`、`Map<string,string>` |
| 多层嵌套泛型 | `List<Map<string,string>>` |
| 用户类 / enum | `Color3`、`FileMode` |
| 其它 typealias（链式别名） | `typealias B = A`（A 也是别名） |
| 经 `import as` 别名前缀限定 | 见下 |

### 经 import 别名前缀限定（三种形态）

`typealias` 的目标类型可以以 import 别名作为路径前缀，三种别名形态均可：

```sl
import AliasNS.Sub as SUB          # 命名空间别名：import NS.a as NSA
import Std as S                    # 模块别名：import <模块> as 别名
import "Std:IO" as IOx             # 字符串 import 别名：import "模块:命名空间" as 别名

typealias LibAliasAlpha = SUB.Alpha        # → AliasNS.Sub.Alpha
typealias LibSFileMode = S.FileMode        # → Std 模块根下的 FileMode
typealias LibIOxSearch = IOx.SearchOption  # → Std 模块 IO 命名空间下的 SearchOption
```

注意：别名前缀续解的是**目标模块/命名空间下的真实声明成员**。如 `Std` 模块下 `Console` / `FileMode` / `FileStream` 注册在模块根（应为 `S.FileMode`），而 `Directory` / `Path` / `SearchOption` 是 `.sl` 显式命名空间限定声明（应为 `S.IO.Directory`）。

---

## 函数类型别名

`typealias Name = ReturnType Function( ParamType, ... )` 声明函数类型别名，可用作：

- 变量/字段类型
- 方法返回类型
- 实参类型

```sl
typealias LibFuncTA = int Function( int, int )

static LibFuncTA makeAdd()
{
    return ( a, b ) => a + b
}

static funcAliasTest()
{
    LibFuncTA f = makeAdd()
    Console.println( f( 3, 4 ).toString() )   # 输出 7
}
```

---

## 使用点限制：不允许追加模板实参（LID 23016）

**不支持泛型 typealias**（别名本身不能带模板参数声明）。目标类型是模板类时，**必须在声明处写全实参**；使用点不允许再给别名追加模板实参：

```sl
typealias LibBoxInt = Box<int>

LibBoxInt a = Box<int>()      # ✓ 正确
LibBoxInt<string> b = null    # ✗ 报错（LID 23016）
```

报错文案（LID 23016 · MetaCoreTypeAliasTemplateArgsNotAllowed）：

> typealias 别名[LibBoxInt]的目标类型已实例化，使用点不支持追加模板实参
> Tips：typealias 目标是模板类时应在声明处写全实参（如 typealias IntList = List\<int\>），使用点 AliasName\<...\> 不允许再携带实参

---

## 冲突与循环引用行为

- **循环引用**（`typealias A = B; typealias B = A`）：别名解析最多进行 64 轮收敛，循环引用无法解析出目标类型，最终在使用点按"类型不存在"报错。
- **重名**：文件级优先于工程级；同级重名时首个生效，后续声明静默忽略；内置全局别名不可遮蔽。
- 别名不与 `class` / `data` / `enum` 产生新的运行时冲突（别名不生成类型），但使用时优先命中别名解析链。

---

## 错误提示（真实 LID）

| LID | 错误码 | 场景与文案 |
|-----|--------|-----------|
| NodeStructFrameTypealiasImportLocal | 20204 | 文件级位置错误：`typealias 只能写在 import/local 后、namespace/class/data/enum 前` |
| NodeStructFrameTypealiasSpProject | 20223 | 类体内位置错误：`类体内的 typealias 仅允许出现在 .sp 工程的 Project 类中` |
| NodeStructFrameTypealias | 20209 | `typealias 后缺少别名与类型` |
| NodeStructFrameTypealias2 | 20210 | `typealias 后应为单个标识符别名` |
| NodeStructFrameTypealias3 | 20211 | `typealias 缺少 = 与目标类型` |
| NodeStructFrameTypealias4 | 20212 | `typealias 目标类型无法解析` |
| MetaCoreTypeAliasTemplateArgsNotAllowed | 23016 | 使用点给已实例化别名追加模板实参（见上节文案） |

---

## 实现参考

- 解析入口：`source/Front/Parse/StructParseFrame.cs`（`ConsumeTypeAliasAt`，文件级/工程级两个消费点）
- 声明结构：`source/Front/FileMeta/FileMetaTypeAliasDecl.cs`（普通/函数类型两种构造）
- 别名解析链与多轮收敛：`source/Front/Core/TypeManager.cs`（`TryResolveTypeAlias` / `ResolveAllDeclaredTypeAliases`）
- 使用点实参检查（LID 23016）：`TypeManager.cs` 两处检查点
