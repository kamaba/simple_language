# `Num` — 通用数值类型

本文件说明 `Num` 类型在语言层面与运行时的结构、赋值与计算语义，以及常见用法示例。

## 概述

- `Num` 是语言提供的通用数值类型，用于表示任意数值（整数或浮点）。
- 在运行时，`Num` 在 `SValue` 中通常以 `double`（`Float64`）存储；同时存在一个面向对象的包装类型 `NumObject`（类对象），用于在需要对象语义（方法、引用计数、虚函数等）时表示数值。
- 在内部枚举中对应为 `EVMType.Num`。

## 运行时表示

- 原始（primitive）`Num` 值：
  - 存储在 `SValue.doubleValue` 并且 `SValue.eType == EVMType.Num`。
  - 在原始（快速）计算路径中，`Num` 被视为浮点（`Float64`）参与计算。
  - 在原始栈（`RawSValue`）中，`Num` 对应到 `Float64` 的分支（读取/写回为 `Float64`）。

- 对象包装 `NumObject`：
  - 当数值需要作为对象时（例如类字段、模板类中的实例或显式使用对象包装），使用 `NumObject`。
  - 运算时若任一操作数是 `NumObject`，优先调用 `NumObject.Operate(...)`（对象自带的运算方法），以保持对象语义（可能有重载或特殊行为）。

## 类型兼容与赋值规则

- 将值赋给声明为 `Num` 的变量时，只接受兼容的数值类型（例如 `Byte`, `SByte`, `Int16`, `UInt16`, `Int32`, `UInt32`, `Int64`, `UInt64`, `Float32`, `Float64` 以及 `Num` 本身）。
- 非数值类型（例如 `String`、`Boolean`、自定义类）不可隐式赋值给 `Num`（运行时会触发断言或报错，当前实现使用 `Debug.Assert` 来提示不兼容赋值）。
- 在 `RuntimeVM` 的赋值路径中（`SetObjectByValue` / `SetValue`），已有检查以确保源 `SValue` 的 `eType` 与目标 `obj.eType` 的兼容性。

## 计算语义（运算与提升规则）

计算流程的大致规则：

1. 如果任一操作数是类包装的 `NumObject`：
   - 优先使用 `NumObject` 的运算方法（`Operate`），保持对象语义。
   - 如果另一侧是原始数值，会把它转换为临时 `NumObject` 后再运算。

2. 否则，进入原始/快速数值路径：
   - 当两侧都被视为数值时，会尝试使用 `RawSValue` 的快速路径来完成操作（`ComputeValueInlineRaw`）。
   - 在快速路径中，`Num` 被当作 `Float64` 处理。

3. 提升与结果类型：
   - 如果任一操作数是浮点（`Float32`/`Float64`）或 `Num`，计算使用浮点（双精度 `double`）路径，结果为浮点（若左操作数原来是 `Num` 或 `Float64`，结果写回为 `Float64`，否则写回为 `Float32`）。
   - 否则使用整数路径：
     - 若存在无符号参与者或 `isUnSign` 标志，则走无符号整型运算（使用 `ulong` 语义）。
     - 否则走有符号整型运算（使用 `long` 语义）。

4. 不支持对 `Num`（浮点语义）执行位运算（如 `& | ^ << >>`）——会在浮点路径时报错。

## 与比较 (==, !=, >, >=, <, <=)

- `Num` 参与比较时作为浮点进行比较（使用 `double` 值）。
- 比较逻辑与运算逻辑的提升规则一致：任何一边为浮点或 `Num`，按浮点比较；仅整数类型时按整数比较（并考虑无符号/有符号）。

## 例子（伪语言/示例）

```slang
// 声明与赋值
Num a = 3          # a 存储为 3.0 (double)
Num b = 2.5        # b 存储为 2.5
int i = 2

// 算术
a = a + b              #浮点相加 => 5.5
a = a + i              #混合：i 提升为浮点 => 5.5 + 2 => 7.5

// 与整型混合
Int32 x = 1
Num y = x + 1      # x 与整数相加后结果转换为 Num（取决于上下文，当前规则为写入 Num 时转换为(double）

// 对象包装
Num no = 10     #类包装的数值
var res = no + 5           # 会调用 NumObject.Operate

// 非法赋值（运行时会断言）
Num n = "abc"    # 错误：字符串不能隐式转换为 Num
```

## 开发者注意事项（运行时与实现）

- 当前实现要点（已在代码中体现）：
  - `EVMType.Num` 被加入到数值检测与提升路径中（`SValueCompute`、`RawSValue`、`SValueCompare` 中均把 `Num` 当作浮点处理）。
  - `RawSValue.FromSValue` 将 `EVMType.Num` 放入 `Float64` 分支，`ApplyToSValue` 会把 `Num` 写回到 `SValue.doubleValue`。
  - `RuntimeVM.SetObjectByValue` 中对目标为 `Num` 的赋值路径会拒绝非数值来源（以 `Debug.Assert` 表示）。

- 需要注意的边缘情况：
  - 精度与整数保留：目前 `Num` 使用 `double` 表示，会丢失某些极大无符号整数的精度。如果需要保留整数精度（例如 `1 + 1` 保持为整型），需要引入更复杂的晋升与保留规则。
  - 位运算：`Num`（浮点语义）不适合位运算，使用时请先显式转换为整数类型（例如 `Int32`）。
  - 除零：浮点除以零在当前实现中返回 `0`（为避免抛异常），可根据需求改为抛出错误或生成 `Infinity/NaN`。

## 推荐风格

- 如果你需要明确整数语义（位运算或严格的整型范围），请使用具体整数类型（例如 `Int32`、`UInt64`）。
- 需要通用数值与小数精度时使用 `Num`，它在多数算术场景下行为符合工程默认策略（以 double 为主）。

---

文件位置：`md/ProgramSyntax/Num.md`。若需我把示例替换为你项目的真实语法示例（若默认伪语法与项目语法有差异），把期望的语言片段贴上我可以调整。