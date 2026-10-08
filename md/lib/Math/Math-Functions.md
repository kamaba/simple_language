# Math-Functions（Mathd / Mathf / Mathh 数学函数）

`Math` 命名空间下的三个静态数学函数类，同一 API 面按浮点精度平行提供：

| 类 | 精度 | 实现策略 |
|------|------|------|
| `Math.Mathd` | Float64 | FFI 直调 `math_lib.dll` 的 `mathd_*`；DLL 不可用回退纯 SL 实现（泰勒级数 / 牛顿迭代），是三精度的基准实现 |
| `Math.Mathf` | Float32 | FFI 直调 `mathf_*`；回退实现委托 `Mathd` 以 Float64 中转 |
| `Math.Mathh` | Float16 | 无 FFI、无系统调用，全部委托 `Mathf` 以 Float32 中转 |

精度中转链：`Mathh → Mathf → Mathd`。半精度标量不存在硬件超越函数时这是标准做法；Float16 的有效位约 3 位十进制，仅用于存储 / 压缩场景。

---

## 1. 常量

| 常量 | Mathd | Mathf | Mathh | 说明 |
|------|------|------|------|------|
| `Pi` | Float64 | Float32 | Float16 | 圆周率（按各精度舍入） |
| `E` | Float64 | Float32 | Float16 | 自然对数的底 |

---

## 2. 三角与反三角

均接收弧度。

| 函数签名（Mathd 版） | Mathf | Mathh | 说明 |
|------|------|------|------|
| `sin( Float64 x ) -> Float64` | ✔ | ✔ | 正弦 |
| `cos( Float64 x ) -> Float64` | ✔ | ✔ | 余弦 |
| `tan( Float64 x ) -> Float64` | ✔ | ✔ | 正切 |
| `asin( Float64 x ) -> Float64` | ✔ | ✔ | 反正弦，x ∈ [-1, 1] |
| `acos( Float64 x ) -> Float64` | ✔ | ✔ | 反余弦 |
| `atan( Float64 x ) -> Float64` | ✔ | ✔ | 反正切 |
| `atan2( Float64 y, Float64 x ) -> Float64` | ✔ | ✔ | 二参反正切，按象限取值 |

## 3. 双曲函数

| 函数签名（Mathd 版） | Mathf | Mathh | 说明 |
|------|------|------|------|
| `sinh( Float64 x ) -> Float64` | ✔ | ✔ | 双曲正弦 |
| `cosh( Float64 x ) -> Float64` | ✔ | ✔ | 双曲余弦 |
| `tanh( Float64 x ) -> Float64` | ✔ | ✔ | 双曲正切 |

## 4. 幂 / 指数 / 对数 / 平方根

| 函数签名（Mathd 版） | Mathf | Mathh | 说明 |
|------|------|------|------|
| `sqrt( Float64 x ) -> Float64` | ✔ | ✔ | 平方根，x < 0 返回 0 |
| `pow( Float64 x, Float64 y ) -> Float64` | ✔ | ✔ | 幂 |
| `exp( Float64 x ) -> Float64` | ✔ | ✔ | e 的幂 |
| `log( Float64 x ) -> Float64` | ✔ | ✔ | 自然对数 |
| `log10( Float64 x ) -> Float64` | ✔ | ✔ | 常用对数 |
| `dot3( Float64 x1, Float64 y1, Float64 z1, Float64 x2, Float64 y2, Float64 z2 ) -> Float64` | ✔ | ✘ | 三维点积（标量展开形式，供矩阵元素级运算复用） |
| `mod( Int32 a, Int32 b ) -> Int32` | ✘ | ✘ | 整数取模（仅 Mathd） |
| `powInt( Int32 base, Int32 exponent ) -> Int32` | ✘ | ✘ | 整数快速幂（仅 Mathd；等价物见 `Factor.powInt`） |

## 5. 取整

| 函数签名（Mathd 版） | Mathf | Mathh | 说明 |
|------|------|------|------|
| `ceil( Float64 x ) -> Float64` | ✔ | ✔ | 向上取整 |
| `floor( Float64 x ) -> Float64` | ✔ | ✔ | 向下取整 |
| `round( Float64 x ) -> Float64` | ✔ | ✔ | 四舍五入 |
| `truncate( Float64 x ) -> Int32` | ✔ | ✔ | 向零截断，返回 Int32 |

## 6. 数值工具（整数 / 浮点双套重载）

| 函数（Mathd 版） | Mathf | Mathh | 说明 |
|------|------|------|------|
| `abs( Int32 v ) -> Int32` / `abs( Float64 v ) -> Float64` | ✔ | ✔ | 绝对值 |
| `min( Int32 a, Int32 b ) -> Int32` / `min( Float64 a, Float64 b ) -> Float64` | ✔ | ✔ | 较小值 |
| `max( Int32 a, Int32 b ) -> Int32` / `max( Float64 a, Float64 b ) -> Float64` | ✔ | ✔ | 较大值 |
| `clamp( Int32 v, Int32 lo, Int32 hi ) -> Int32` / `clamp( Float64 v, Float64 lo, Float64 hi ) -> Float64` | ✔ | ✔ | 区间钳制 |
| `sign( Int32 v ) -> Int32` / `sign( Float64 v ) -> Int32` | ✔ | ✔ | 符号（-1 / 0 / 1） |

## 7. 插值 / 距离 / 角度转换 / 近似比较

| 函数签名（Mathd 版） | Mathf | Mathh | 说明 |
|------|------|------|------|
| `lerp( Float64 a, Float64 b, Float64 t ) -> Float64` | ✔ | ✔ | 线性插值 |
| `lerpClamped( Float64 a, Float64 b, Float64 t ) -> Float64` | ✔ | ✔ | 线性插值，t 钳制到 [0, 1] |
| `distance( Float64 x1, Float64 y1, Float64 x2, Float64 y2 ) -> Float64` | ✔ | ✔ | 2D 两点距离 |
| `distance3D( Float64 x1, Float64 y1, Float64 z1, Float64 x2, Float64 y2, Float64 z2 ) -> Float64` | ✔ | ✔ | 3D 两点距离 |
| `degrees( Float64 radians ) -> Float64` | ✔ | ✔ | 弧度 → 角度 |
| `radians( Float64 degrees ) -> Float64` | ✔ | ✔ | 角度 → 弧度 |
| `approximately( Float64 a, Float64 b ) -> bool` | ✔ | ✔ | 近似相等（容差见下表） |

`approximately` 的内置容差：

| 类 | 容差 |
|------|------|
| `Mathd` | `0.0000000001`（1e-10） |
| `Mathf` | `0.000001f`（1e-6） |
| `Mathh` | `0.0009765625h`（2^-10，半精度最小正规数） |

---

## 8. FFI 绑定与 fallback 机制

`Mathd` / `Mathf` 的标量函数通过 `@DllImport` 三参形式绑定到 `math_lib.dll`（普通 C ABI，不经过 VM 栈）：

```sl
# Mathf.sl 片段
@DllImport( "math_lib", "mathf_sin", "Float32->Float32" )
public static Float32 sin( Float32 value )
{
    # 方法体即 fallback 本体：DLL 加载失败 / 符号缺失时执行
    ret Mathd.sin( value.toFloat64() ).toFloat32()
}
```

- 首参 `"math_lib"` 是别名，编译期经 `ResolveDllImportPath` 按 `Math.jsonc` 的 `dllImports` 别名表替换为实际 DLL 路径
- 第三参 `"Float32->Float32"` 声明 C 符号签名
- C 侧实现位于 `source/Front/Lib/Math/cvm_math_lib/math_lib.c`（`mathf_*` / `mathd_*` 导出符号）
- DLL 不可用时自动执行 SL 方法体，调用方无感知

`Mathh` 完全不含 `@DllImport`，全部方法形如：

```sl
public static Float16 sin( Float16 value )
{
    ret Mathf.sin( value.toFloat32() ).toFloat16()
}
```

---

## 9. 使用示例

```sl
# 三精度同名调用
Float64 a = Mathd.sin( 0.5 )          # Float64
Float32 b = Mathf.sin( 0.5f )         # Float32
Float16 c = Mathh.sin( 0.5h )         # Float16

# 向量长度（向量类内部即用 Mathf.sqrt）
Float32 len = Mathf.sqrt( 3.0f * 3.0f + 4.0f * 4.0f )   # 5.0

# 角度与插值
Float32 rad = Mathf.radians( 90.0f )
Float32 v   = Mathf.lerpClamped( 0.0f, 10.0f, 1.5f )    # 10.0

# 近似比较
if Mathf.approximately( 0.1f + 0.2f, 0.3f )
{
    Console.println( "equal" )
}
```
