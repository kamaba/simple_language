# 低精度浮点类型（Float8 / Float16）

本文说明四种**低精度浮点系统类型**：`Float8`（e4m3）、`Float8_E5M2`（e5m2）、`Float16`（IEEE half）、`Float16_Brain`（bfloat16）。覆盖**字面量后缀**、**底层存储约定**、**强制转换**、**算术/比较语义**以及 **Front / C VM 实现要点**。实现细节以 `source/Front` 与 `csimple_lang` 代码为准。

---

## 1. 类型总览

| 类型名           | 别名（Nickname） | 位格式        | bias | 最大有限值   | 最小次正规数        | 底层存储 | 字面量后缀 |
|------------------|------------------|---------------|------|--------------|---------------------|----------|------------|
| `Float8`         | `Float8_E4M3`    | 1s + 4e + 3m  | 7    | ±448         | 2^-9 ≈ 0.001953125  | `uint8`  | `fe4`      |
| `Float8_E5M2`    | -                | 1s + 5e + 2m  | 15   | ±57344       | 2^-16 ≈ 1.53e-5     | `uint8`  | `fe5`      |
| `Float16`        | -                | 1s + 5e + 10m | 15   | ±65504       | 2^-24 ≈ 5.96e-8     | `uint16` | `h`        |
| `Float16_Brain`  | -                | 1s + 8e + 7m  | 127  | ≈ ±3.39e38   | 2^-133 ≈ 9.18e-41   | `uint16` | `hb`       |

要点：

- 四个类型均定义于 Core 库（`source/Front/Lib/Core/Float8.sl`、`Float16.sl`），`extends Num`。
- **e4m3**（OCP FP8）：无 `Infinity`，`S.1111.111` 编码为 NaN，指数全 1 且尾数非全 1 时仍表示有限值（因此最大值为 448 而非 240 量级）。
- **e5m2**（OCP FP8）：保留 `Infinity`/`NaN` 编码。
- **Float16** 即 IEEE 754 binary16（half precision）。
- **Float16_Brain** 即 bfloat16：指数域与 `Float32` 完全相同，尾数仅 7 位——**动态范围大、精度低**。

---

## 2. 字面量后缀

低精度浮点字面量由**数字后缀**决定类型，编译期即完成十进制到目标位格式的**舍入编码**：

| 字面量        | 类型        | 说明                              |
|---------------|-------------|-----------------------------------|
| `1.5fe4`      | `Float8`    | e4m3 编码                          |
| `2.0fe5`      | `Float8_E5M2` | e5m2 编码                        |
| `0.3h`        | `Float16`   | IEEE half 编码                     |
| `0.3hb`       | `Float16_Brain` | bfloat16 编码                  |

```slang
Float8 a = 1.5fe4
Float8_E5M2 b = 2.0fe5
Float16 c = 0.3h
Float16_Brain d = 0.3hb
```

**舍入示例**（round-to-nearest-even，十进制字面量编码到目标格式）：

| 字面量        | 实际存储值     | 原因                                          |
|---------------|----------------|-----------------------------------------------|
| `0.3fe4`      | 0.3125         | e4m3 尾数 3 位，最近可表示值为 0.25/0.3125/0.375 |
| `0.3fe5`      | 0.3125         | e5m2 尾数 2 位，同为 0.3125 最近                |
| `0.3h`        | 0.300048828125 | half 尾数 10 位                                 |
| `0.3hb`       | 0.30078125     | bf16 尾数 7 位                                  |
| `5.9604644775390625e-8h` | 5.9604644775390625e-8 | half 最小次正规数 2^-24         |

---

## 3. 底层存储约定

**低精度浮点在运行时以"位模式 + 类型标记"方式存储**：

- `Float8` / `Float8_E5M2`：真实存储类型为 **`uint8`**（1 字节位模式）。
- `Float16` / `Float16_Brain`：真实存储类型为 **`uint16`**（2 字节位模式）。
- 栈槽、局部变量、成员变量均保存**原始位模式**，类型标记（`EVMType.Float8_E4M3` 等）决定如何解码。

```
1.5fe4 的 e4m3 位模式 = 0x3C（0 0111 100）：1.5 * 2^0
2.0fe5 的 e5m2 位模式 = 0x40（0 10000 00）：1.0 * 2^1
-2.5h 的 f16 位模式  = 0xC040（1 10000 0001000000）
```

这一约定意味着：**位模式在压栈/弹栈/存取全程不解码**，仅在计算、比较、转换、格式化输出时按类型标记解码为 `double`。

---

## 4. 声明与初始化

```slang
# 声明 + 字面量初始化（无转换指令，直接编码入栈）
Float8 a = 1.5fe4
Float8_E5M2 b = 0.25fe5
Float16 c = 65504.0h
Float16_Brain d = 3.3895314e38hb

# 负数字面量（一元负号编译期折叠，或翻转符号位）
Float8 neg = -2.5fe4

# 直接打印（println 走 toString 路径）
global.println(1.5fe4)     # 输出 1.5
global.println(a.toString())
```

---

## 5. 强制转换

低精度类型与普通类型（`Float32` / `Float64` / `Num` / 整数）之间**赋值即触发强制转换**，编译器插入 `Convert` 系列指令；**同类型赋值不产生转换指令**：

| 转换方向                        | IR 指令          |
|---------------------------------|------------------|
| `Float32` / `Float64` → `Float8` | `Convert_F8E4M3` |
| `Float32` / `Float64` → `Float8_E5M2` | `Convert_F8E5M2` |
| `Float32` / `Float64` → `Float16` | `Convert_F16`    |
| `Float32` / `Float64` → `Float16_Brain` | `Convert_F16B` |
| 低精度 → `Float32`               | `Convert_R4`     |
| 低精度 → `Float64`               | `Convert_R8`     |

```slang
# Float32 -> Float8（赋值即显式转换）
Float32 f = 9.5f
Float8 a = f                 # 9.5 在 e4m3 无法精确表示 -> 10

# Float8 -> Float32（解码为 Float32）
Float8 b = 0.3fe4            # 实际值 0.3125
Float32 g = b                # 0.3125

# Float64 <-> Float16
Float64 d = 12.0
Float16 c = d
Float64 e = c

# e4m3 与 e5m2 互转（经 double 中转重新编码）
Float8 m = 3.5fe4
Float8_E5M2 n = m            # 3.5 两种格式均可精确表示

# 赋值语句中的转换（非初始化位置同样生效）
Float8 p = 0.0fe4
p = 6.25f                    # 6.25 在 e4m3 与 6.0/6.5 等距 -> ties-to-even 取 6
```

**舍入规则**：所有进入低精度格式的转换均为 **round-to-nearest-even**（银行家舍入）：

- `9.5f -> Float8` = **10**（9.5 在 9 与 10 正中间，取尾数为偶数的 10）
- `6.25f -> Float8` = **6**（6.25 在 6.0 与 6.5 正中间，取偶）
- `6.25f -> Float16` = **6.25**（half 尾数 10 位可精确表示）

---

## 6. 算术运算

低精度运算采用**「解码 → double 计算 → 重新编码」**语义（`runtime_value_compute`）：

1. 两侧操作数按各自类型标记解码为 `double`。
2. 以 `double` 完成加/减/乘/除。
3. 结果重新编码回**运算结果类型**（低精度参与的混合运算按类型提升规则确定）。
4. 一元负号 `-x` 直接**翻转符号位**（位模式异或符号位，不解码）。

```slang
Float8 a = 1.5fe4
Float8 b = 0.5fe4
a + b        # 2
a - b        # 1
a * b        # 0.75
a / b        # 3

# 舍入：81 = 1.265625 * 2^6
Float8 x = 9.0fe4
x * x        # 80   （e4m3 尾数 3 位，81 舍入到 80）
Float16 y = 9.0h
y * y        # 81   （half 尾数 10 位，精确）
Float16_Brain z = 9.0hb
z * z        # 81   （bf16 尾数 7 位：1.265625 -> 0100010 精确）

# 负数参与运算
Float8 m = -1.5fe4
Float8 n = 2.5fe4
m + n        # 1
```

---

## 7. 比较运算

`== != > >= < <=` 均按**解码后的数值**比较（`runtime_value_compare`），支持**跨类型比较**（任一侧为低精度/浮点则按 `double` 比较）：

```slang
Float8 a = 2.0fe4
Float8 b = 3.0fe4
a > b        # False
a < b        # True
a == b       # False

Float8 n = -3.0fe4
n < a        # True  （位模式 0xC4 > 0x40，但按值比较正确）

# 跨类型比较
Float32 f = 2.0f
a == f       # True  （Float8 与 Float32 混合比较）

Float16_Brain p = 128.0hb
Float16 q = 2.0h
p > q        # True  （bf16 与 f16 混合比较）
```

---

## 8. 真值判断与 toString

- **真值判断**：低精度值**解码后判零**——`0.0` 为 falsy，任何非零（含次正规数）为 truthy。
- **toString**：解码为 double 后以 `%g` 格式输出，委托 `Core.Float8.toString()` → `SystemConvertString(this)`。

```slang
Float8 z = 0.0fe4
if (z != 0.0fe4)
    # 不进入：0.0fe4 为 falsy
    ...

Float8 tiny = 0.001953125fe4
if (tiny != 0.0fe4)
    # 进入：次正规数非零，truthy
    ...

global.println(1.5fe4.toString())     # 1.5
global.println(0.3fe4.toString())     # 0.3125
global.println(57344.0fe5.toString()) # 57344
global.println(5.9604644775390625e-8h.toString())  # 5.96046e-08
```

---

## 9. 实现要点

### 9.1 Front（编译器）

| 环节         | 位置 / 说明 |
|--------------|-------------|
| 词法后缀     | `LexerParse.cs`：识别 `fe4` / `fe5` / `h` / `hb` 数字后缀 |
| EType        | `EType.Float8_E4M3` / `Float8_E5M2` / `Float16` / `Float16_Brain` |
| 类型提升     | `MetaTypeFactory.CalcETypeByLeftAndRight`：低精度参与的二元运算类型分支 |
| 常量编码     | 字面量编译期完成十进制 → 位模式的 RNE 编码 |
| IR 常量指令  | `LoadConstFloat8_E4M3` / `LoadConstFloat8_E5M2` / `LoadConstFloat16` / `LoadConstFloat16_Brain`（操作数为原始位模式） |
| IR 转换指令  | `Convert_F8E4M3` / `Convert_F8E5M2` / `Convert_F16` / `Convert_F16B` / `Convert_R4` / `Convert_R8` |
| Core 类      | `Lib/Core/Float8.sl`（`Float8` + `Float8_E5M2`）、`Lib/Core/Float16.sl`（`Float16` + `Float16_Brain`） |

### 9.2 C VM（csimple_lang）

| 环节             | 位置 / 说明 |
|------------------|-------------|
| EVMType          | `EVMType_Float8_E4M3` / `Float8_E5M2` / `Float16` / `Float16_Brain` |
| 栈槽 kind        | `VM_STACK_SLOT_FLOAT8_E4M3` / `FLOAT8_E5M2` / `FLOAT16` / `FLOAT16_BRAIN`（byte 长度 1/1/2/2） |
| 位级编解码       | `runtime_value_convert.c`：`runtime_value_f32_to_f8e4m3_bits` / `f8e5m2_bits` / `f16_bits` / `bf16_bits` 及对应反向解码 |
| 计算             | `runtime_value_compute.c`：低精度解码 → double 运算 → 重编码；取反翻符号位 |
| 比较             | `runtime_value_compare.c`：按 `runtime_value_get_as_double` 解码比较（含跨类型） |
| Convert 指令     | `vm_runtime.c` Convert opcode 分发：`Convert_F8E4M3` 等六种指令的 etype 映射 |
| toString         | `convert_system_method.c` `vm_sys_convert_string`：按栈槽 kind 解码后 `%g` 输出 |
| 局部/成员存储    | `runtime_object.c`：按 `runtime_type` 位宽读写 data slice（float8=1 字节、float16=2 字节位模式） |
| 类型标记         | `runtime_class_manager.c` `class_name_hint_e_type`：`Core.Float8*` / `Core.Float16*` 类名 → EVMType 映射 |

**存储不变式**：从 `LoadConst` 压栈、`StoreLocal/LoadLocal`、方法传参到成员读写，float8/16 全程以**原始位模式**流动；任何需要数值语义的出口（计算/比较/转换/格式化）都基于栈槽 kind 或 etype 解码。栈上 pop/peek 辅助函数（`vm_try_pop_i32` 等）对低精度槽按位模式镜像 `UInt8`/`UInt16` 处理。

### 9.3 测试

- `test/BaseTest/Float8Test.sl`：字面量 / 算术 / 转换 / 比较 / 存储五组用例（e4m3 + e5m2）。
- `test/BaseTest/Float16Test.sl`：同结构五组用例（f16 + bf16）。
- 两组用例在 `test/BaseTest/ProjectTest.sp` 的 `_main_()` 中调用 `Float8Test.fun()` / `Float16Test.fun()` 触发编译与导出。
- 验证方式：Front 编译导出（`-e ir`）后用 C VM 运行 `ProjectTest.module.json`，确认无 `(ERROR)` 输出且数值符合本文各表格预期。
