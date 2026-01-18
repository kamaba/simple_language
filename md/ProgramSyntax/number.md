# 数值类型

S 语言支持多种整数与浮点类型：`byte`, `sbyte`, `char`, `short`, `ushort`, `int` (Int32), `uint`, `long` (Int64), `ulong`，以及 `float`/`double` 等浮点类型。

字面量后缀约定：
- `i` 用于标注 Int32，例如 `10i`。
- `u` / `ui` 用于无符号整数后缀（需结合位宽，如 `100uL` 表示 unsigned long）。
- `s` / `us` 表示短整型（Int16/UInt16）。
- `L` 表示 long（Int64），`uL` 表示 UInt64。
- 二进制或下划线分组可写：`0b0011_1100`。

示例：

```s
var a1 = 10i;
Int32 a2 = 10;
var a3 = 10ui;
var a4 = 20s; // short
var a5 = 20us; // unsigned short
var a6 = 100000000L; // long
var a7 = 10000000000uL; // unsigned long
var b1 = 0b0011_1100;
```

比较与等价：
- 对于基本数值类型，`==` 表示值相等（等同于 `===` 对值类型）。
- 对于对象类型，`==` 通常表示引用相等（object identity）；使用 `===` 可比较值等价（若对象支持）。

