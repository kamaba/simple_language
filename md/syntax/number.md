# 数值类型

本文说明：**内置数值类型与词法**、**字面量后缀**、以及 **`Num` 通用数值类型** 在语言与运行时的语义。实现细节以 `source/Front` 与 VM 代码为准。

---

## 1. 内置类型与词法（Lexer）

扫描参考：`source/Front/Compile/Parse/LexerParse.cs`

### 1.1 词法中的类型关键字（小写）

词法直接识别的小写关键字包括：

`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `bool`, `string`

与内部 `EType` 映射示例：

| 关键字   | EType   |
|----------|---------|
| `short`  | Int16   |
| `ushort` | UInt16  |
| `int`    | Int32   |
| `uint`   | UInt32  |
| `long`   | Int64   |
| `ulong`  | UInt64  |
| `byte`   | Byte    |
| `sbyte`  | SByte   |

### 1.2 数值字面量后缀（词法）

| 写法   | 类型     |
|--------|----------|
| `1s`   | Int16    |
| `1us`  | UInt16   |
| `1i`   | Int32    |
| `1ui`  | UInt32   |
| `1L`   | Int64    |
| `1uL`  | UInt64   |
| `1.0f` | Float32  |
| `1.0d` | Float64  |

### 1.3 FileMeta / 类型系统（Meta 层）

扫描参考：`FileMetatUtil.cs`、`TypeManager.cs`、`CoreMetaClassManager.cs`、`Define.cs` 等。

- 核心运行时类型枚举为规范 **PascalCase**：`Byte`, `SByte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Float32`, `Float64`, `Boolean`, `String`, `Num`。
- `FileMeta` 阶段通过 meta/class 解析类型引用，不仅限于小写别名。
- 源码中可使用 **PascalCase 核心类型名**（如 `Int16`、`Byte`）作为类风格类型名。

### 1.4 推荐书写风格（声明）

为减少歧义、与工程一致：

- **声明优先**使用核心类型名：`Byte`, `SByte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Float32`, `Float64`, `Boolean`, `String`, `Num`。
- 与小写关键字对应关系（语义等价）：`short == Int16`，`byte == Byte`，`int == Int32`，`long == Int64` 等。
- 测试与文档中尽量**统一一种风格**（推荐 PascalCase），避免同一文件混用小写别名与核心名。

`test/BaseTest/NumberTest.sl` 已按上述风格调整示例（如 `Int32`、`Num`、`String`、`as Int32` 等）。

---

## 2. 字面量与示例

语言支持多种整数与浮点类型；字面量可使用后缀与二进制写法。

后缀约定（与词法一致，可与上表对照）：

- `i` — Int32，例如 `10i`。
- `ui` — UInt32（无符号整型后缀需结合位宽）。
- `s` / `us` — Int16 / UInt16。
- `L` / `uL` — Int64 / UInt64。
- `f` / `d` — 浮点（见词法 `1.0f` / `1.0d`）。
- 二进制与下划线分组：`0b0011_1100`。

```s
var a1 = 10i;
Int32 a2 = 10;
var a3 = 10ui;
var a4 = 20s;   // short / Int16
var a5 = 20us;  // unsigned short
var a6 = 100000000L;
var a7 = 10000000000uL;
var b1 = 0b0011_1100;
```

**比较**：对基本数值类型，`==` 表示值相等；对对象类型，`==` 多为引用相等，值语义比较需看类型是否提供 `===` 等约定。

---

## 3. `Num` — 通用数值类型

### 3.1 概述

- `Num` 表示任意数值（整数或浮点场景的通用容器）。
- 运行时原始 `Num` 在 `SValue` 中常以 **`double`（Float64）** 存储；需要对象语义时使用 **`NumObject`**（方法、虚调用等）。
- 内部枚举对应 **`EVMType.Num`**。

### 3.2 运行时表示

- **原始 `Num`**：存于 `SValue.doubleValue`，`SValue.eType == EVMType.Num`；快速计算路径中按浮点参与运算；在 `RawSValue` 中与 `Float64` 分支对应。
- **`NumObject`**：字段、模板实例或显式对象包装时使用；若运算一侧为 `NumObject`，优先走 **`NumObject.Operate(...)`**。

### 3.3 赋值与类型兼容

- 赋给声明为 `Num` 的变量时，来源须为数值类型（`Byte` … `Float64` 及 `Num`）；`String`、`Boolean`、自定义类等不可隐式赋给 `Num`（实现中常用 `Debug.Assert` 提示不兼容）。
- `RuntimeVM` 的 `SetObjectByValue` / `SetValue` 等路径会检查 `SValue` 与目标类型兼容性。

### 3.4 计算与提升

1. 任一侧为 **`NumObject`**：优先 `Operate`；另一侧为原始值时可转为临时 `NumObject`。
2. 否则走原始路径：`RawSValue` / `ComputeValueInlineRaw` 等；**`Num` 按 Float64 处理**。
3. **提升**：任一侧为 `Float32`/`Float64` 或 `Num` 时走双精度浮点路径；否则整数路径（有符号 `long` 语义 / 无符号 `ulong` 语义由标志决定）。
4. **`Num` 不支持位运算**（`& | ^ << >>`），需在整数类型上运算或先显式转换为整数类型。

### 3.5 比较（==, !=, >, …）

- `Num` 按 **double** 比较；与运算的提升规则一致：一侧为浮点或 `Num` 则按浮点比较。

### 3.6 示例

```slang
Num a = 3
Num b = 2.5
Int32 i = 2

a = a + b
a = a + i

Int32 x = 1
Num y = x + 1

Num no = 10
var res = no + 5   // 可能走 NumObject.Operate

# Num n = "abc"   # 错误：不可隐式转换
```

### 3.7 实现注意与边界

- `SValueCompute`、`RawSValue`、`SValueCompare` 等将 **`Num` 按浮点** 纳入路径。
- **精度**：`double` 无法精确表示所有极大无符号整数。
- **除零**：当前浮点除零可能返回 `0`（避免抛异常），可按产品需求改为错误或 `Infinity/NaN`。

### 3.8 何时用 `Num` 与何时用具体类型

- 需要**位运算**或**严格整型范围**：用 `Int32`、`UInt64` 等具体类型。
- 需要**通用数值**、默认以小数为主：用 **`Num`**。
