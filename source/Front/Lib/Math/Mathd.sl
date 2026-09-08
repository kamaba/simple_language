import Std;

# Math —— Float64（double）精度数学库，默认通用入口。
#
# 底层能力：由 math_lib.dll（C ABI，见 source\Front\Lib\Math\cvm_math_lib\math_lib.c）
# 经 FFI @DllImport 三参形式（库别名, 符号名, 参数签名）绑定调用；
# DLL 加载失败 / 符号缺失时自动执行函数本体（SL 纯实现 fallback，
# 见本文件后半的 xxxFallback 私有静态实现）。
# 纯算术能力（abs / min / max / clamp / sign ...）在 SL 层实现。
#
# 精度分组约定：
#   Math  -> Float64（本文件）
#   Mathf -> Float32（见 Mathf.sl）
#   Mathh -> Float16（见 Mathh.sl）
public class Mathd
{
    public const static Float64 Pi = 3.141592653589793d
    public const static Float64 E = 2.718281828459045d

    # ── 三角函数（FFI mathd_* + SL fallback） ────────────
    @DllImport( "math_lib", "mathd_sin", "Float64->Float64" )
    public static Float64 sin( Float64 value )
    {
        ret Mathd.sinFallback( value )
    }

    @DllImport( "math_lib", "mathd_cos", "Float64->Float64" )
    public static Float64 cos( Float64 value )
    {
        ret Mathd.cosFallback( value )
    }

    @DllImport( "math_lib", "mathd_tan", "Float64->Float64" )
    public static Float64 tan( Float64 value )
    {
        ret Mathd.tanFallback( value )
    }

    @DllImport( "math_lib", "mathd_asin", "Float64->Float64" )
    public static Float64 asin( Float64 value )
    {
        ret Mathd.asinFallback( value )
    }

    @DllImport( "math_lib", "mathd_acos", "Float64->Float64" )
    public static Float64 acos( Float64 value )
    {
        ret Mathd.acosFallback( value )
    }

    @DllImport( "math_lib", "mathd_atan", "Float64->Float64" )
    public static Float64 atan( Float64 value )
    {
        ret Mathd.atanFallback( value )
    }

    @DllImport( "math_lib", "mathd_atan2", "Float64,Float64->Float64" )
    public static Float64 atan2( Float64 y, Float64 x )
    {
        ret Mathd.atan2Fallback( y, x )
    }

    # ── 双曲函数 ─────────────────────────────────────────
    @DllImport( "math_lib", "mathd_sinh", "Float64->Float64" )
    public static Float64 sinh( Float64 value )
    {
        ret Mathd.sinhFallback( value )
    }

    @DllImport( "math_lib", "mathd_cosh", "Float64->Float64" )
    public static Float64 cosh( Float64 value )
    {
        ret Mathd.coshFallback( value )
    }

    @DllImport( "math_lib", "mathd_tanh", "Float64->Float64" )
    public static Float64 tanh( Float64 value )
    {
        ret Mathd.tanhFallback( value )
    }

    # ── 幂与对数 ─────────────────────────────────────────
    @DllImport( "math_lib", "mathd_pow", "Float64,Float64->Float64" )
    public static Float64 pow( Float64 baseValue, Float64 exponent )
    {
        ret Mathd.powFallback( baseValue, exponent )
    }

    @DllImport( "math_lib", "mathd_sqrt", "Float64->Float64" )
    public static Float64 sqrt( Float64 value )
    {
        ret Mathd.sqrtFallback( value )
    }

    @DllImport( "math_lib", "mathd_exp", "Float64->Float64" )
    public static Float64 exp( Float64 value )
    {
        ret Mathd.expFallback( value )
    }

    @DllImport( "math_lib", "mathd_log", "Float64->Float64" )
    public static Float64 log( Float64 value )
    {
        ret Mathd.logFallback( value )
    }

    @DllImport( "math_lib", "mathd_log10", "Float64->Float64" )
    public static Float64 log10( Float64 value )
    {
        ret Mathd.logFallback( value ) / 2.302585092994046d
    }

    # ── 取整 ─────────────────────────────────────────────
    @DllImport( "math_lib", "mathd_ceil", "Float64->Float64" )
    public static Float64 ceil( Float64 value )
    {
        ret 0.0d - Mathd.floorFallback( 0.0d - value )
    }

    @DllImport( "math_lib", "mathd_floor", "Float64->Float64" )
    public static Float64 floor( Float64 value )
    {
        ret Mathd.floorFallback( value )
    }

    @DllImport( "math_lib", "mathd_round", "Float64->Float64" )
    public static Float64 round( Float64 value )
    {
        ret Mathd.roundFallback( value )
    }

    @DllImport( "math_lib", "mathd_truncate", "Float64->Int32" )
    public static Int32 truncate( Float64 value )
    {
        ret value.toInt32()
    }

    # ── 三维点积（矩阵 / 向量元素运算用，恰好 6 参 FFI 上限） ──
    @DllImport( "math_lib", "mathd_dot3", "Float64,Float64,Float64,Float64,Float64,Float64->Float64" )
    public static Float64 dot3( Float64 a0, Float64 a1, Float64 a2, Float64 b0, Float64 b1, Float64 b2 )
    {
        ret a0 * b0 + a1 * b1 + a2 * b2
    }

    # ── FFI 不可用时的 SL 纯实现（private fallback） ─────

    # 归约：x mod 2π 折到 [-π, π]（截断式整除，避免 Float64 取模）
    private static Float64 reducePi( Float64 value )
    {
        Float64 twoPi = 6.283185307179586d
        Float64 q = value / twoPi
        Int32 k = q.toInt32()
        ret value - twoPi * k.toFloat64()
    }

    private static Float64 sinFallback( Float64 value )
    {
        Float64 x = Mathd.reducePi( value )
        # 折到 [-π/2, π/2]：sin(x) = sin(π - x)
        if x > 1.5707963267948966d
        {
            x = Mathd.Pi - x
        }
        if x < -1.5707963267948966d
        {
            x = 0.0d - Mathd.Pi - x
        }
        # 泰勒级数至 x^25（|x| ≤ π/2 时误差 ~1e-22）
        Float64 x2 = x * x
        Float64 term = x
        Float64 sum = x
        Int32 i = 1
        while i <= 12
        {
            Int32 n = i + i
            term = term * x2 / ( n.toFloat64() * ( n + 1 ).toFloat64() )
            if i % 2 == 1
            {
                sum = sum - term
            }
            else
            {
                sum = sum + term
            }
            i = i + 1
        }
        ret sum
    }

    private static Float64 cosFallback( Float64 value )
    {
        Float64 x = Mathd.reducePi( value )
        # 偶函数
        if x < 0.0d
        {
            x = 0.0d - x
        }
        # x ∈ [0, π]，若 x > π/2 用 cos(x) = -cos(π - x)
        Float64 sgn = 1.0d
        if x > 1.5707963267948966d
        {
            x = Mathd.Pi - x
            sgn = 0.0d - 1.0d
        }
        # 泰勒级数至 x^24
        Float64 x2 = x * x
        Float64 term = 1.0d
        Float64 sum = 1.0d
        Int32 i = 1
        while i <= 12
        {
            Int32 n = i + i - 1
            term = term * x2 / ( n.toFloat64() * ( n + 1 ).toFloat64() )
            if i % 2 == 1
            {
                sum = sum - term
            }
            else
            {
                sum = sum + term
            }
            i = i + 1
        }
        ret sgn * sum
    }

    private static Float64 tanFallback( Float64 value )
    {
        ret Mathd.sinFallback( value ) / Mathd.cosFallback( value )
    }

    private static Float64 asinFallback( Float64 value )
    {
        if value >= 1.0d
        {
            ret Mathd.Pi * 0.5d
        }
        if value <= -1.0d
        {
            ret 0.0d - Mathd.Pi * 0.5d
        }
        ret Mathd.atanFallback( value / Mathd.sqrtFallback( 1.0d - value * value ) )
    }

    private static Float64 acosFallback( Float64 value )
    {
        ret Mathd.Pi * 0.5d - Mathd.asinFallback( value )
    }

    private static Float64 atanFallback( Float64 value )
    {
        # 象限归约：|x| > 1 用 atan(x) = π/2 - atan(1/x)
        if value > 1.0d
        {
            ret Mathd.Pi * 0.5d - Mathd.atanFallback( 1.0d / value )
        }
        if value < -1.0d
        {
            ret 0.0d - Mathd.Pi * 0.5d - Mathd.atanFallback( 1.0d / value )
        }
        # 两次半角收缩：v = v / (1 + sqrt(1 + v²))，结果 ×4
        Float64 v = value
        Int32 k = 0
        while k < 2
        {
            v = v / ( 1.0d + Mathd.sqrtFallback( 1.0d + v * v ) )
            k = k + 1
        }
        # 泰勒级数至 v^23（|v| ≤ 0.2 时误差 ~1e-17）
        Float64 v2 = v * v
        Float64 term = v
        Float64 sum = v
        Int32 i = 1
        while i <= 11
        {
            term = term * v2
            Int32 d = i + i + 1
            if i % 2 == 1
            {
                sum = sum - term / d.toFloat64()
            }
            else
            {
                sum = sum + term / d.toFloat64()
            }
            i = i + 1
        }
        ret sum * 4.0d
    }

    private static Float64 atan2Fallback( Float64 y, Float64 x )
    {
        if x > 0.0d
        {
            ret Mathd.atanFallback( y / x )
        }
        if x < 0.0d
        {
            if y >= 0.0d
            {
                ret Mathd.atanFallback( y / x ) + Mathd.Pi
            }
            ret Mathd.atanFallback( y / x ) - Mathd.Pi
        }
        # x == 0
        if y > 0.0d
        {
            ret Mathd.Pi * 0.5d
        }
        if y < 0.0d
        {
            ret 0.0d - Mathd.Pi * 0.5d
        }
        ret 0.0d
    }

    private static Float64 sinhFallback( Float64 value )
    {
        Float64 e = Mathd.expFallback( value )
        ret ( e - 1.0d / e ) * 0.5d
    }

    private static Float64 coshFallback( Float64 value )
    {
        Float64 e = Mathd.expFallback( value )
        ret ( e + 1.0d / e ) * 0.5d
    }

    private static Float64 tanhFallback( Float64 value )
    {
        if value > 20.0d
        {
            ret 1.0d
        }
        if value < -20.0d
        {
            ret 0.0d - 1.0d
        }
        Float64 e = Mathd.expFallback( value + value )
        ret ( e - 1.0d ) / ( e + 1.0d )
    }

    private static Float64 powFallback( Float64 baseValue, Float64 exponent )
    {
        # 正底：exp(e · ln(b))
        if baseValue > 0.0d
        {
            ret Mathd.expFallback( exponent * Mathd.logFallback( baseValue ) )
        }
        if baseValue == 0.0d
        {
            if exponent > 0.0d
            {
                ret 0.0d
            }
            ret 1.0e308d
        }
        # 负底：仅整数指数（平方求幂）
        Int32 n = exponent.toInt32()
        if n.toFloat64() == exponent
        {
            Float64 r = 1.0d
            Float64 b = baseValue
            Int32 e = n
            if e < 0
            {
                e = 0 - e
                b = 1.0d / b
            }
            while e > 0
            {
                if e % 2 == 1
                {
                    r = r * b
                }
                b = b * b
                e = e / 2
            }
            ret r
        }
        # 负底非整数指数：NaN 近似
        ret 0.0d - 1.0e308d
    }

    private static Float64 sqrtFallback( Float64 value )
    {
        if value < 0.0d
        {
            ret 0.0d - 1.0e308d
        }
        if value == 0.0d
        {
            ret 0.0d
        }
        # 缩放到 [1, 4) 再牛顿迭代 8 次
        Float64 x = value
        Int32 e = 0
        while x >= 4.0d
        {
            x = x * 0.25d
            e = e + 1
        }
        while x < 1.0d
        {
            x = x * 4.0d
            e = e - 1
        }
        Float64 r = x * 0.5d + 0.5d
        Int32 i = 0
        while i < 8
        {
            r = ( r + x / r ) * 0.5d
            i = i + 1
        }
        while e > 0
        {
            r = r * 2.0d
            e = e - 1
        }
        while e < 0
        {
            r = r * 0.5d
            e = e + 1
        }
        ret r
    }

    private static Float64 expFallback( Float64 value )
    {
        if value > 709.0d
        {
            ret 1.0e308d
        }
        if value < -745.0d
        {
            ret 0.0d
        }
        # x = k·ln2 + r（r ∈ [-ln2/2, ln2/2]），e^r 泰勒后乘 2^k
        Float64 ln2 = 0.6931471805599453d
        Float64 q = value / ln2 + 0.5d
        Int32 k = q.toInt32()
        Float64 r = value - k.toFloat64() * ln2
        Float64 sum = 1.0d
        Float64 term = 1.0d
        Int32 i = 1
        while i <= 15
        {
            term = term * r / i.toFloat64()
            sum = sum + term
            i = i + 1
        }
        while k > 0
        {
            sum = sum * 2.0d
            k = k - 1
        }
        while k < 0
        {
            sum = sum * 0.5d
            k = k + 1
        }
        ret sum
    }

    private static Float64 logFallback( Float64 value )
    {
        if value <= 0.0d
        {
            ret 0.0d - 1.0e308d
        }
        if value == 1.0d
        {
            ret 0.0d
        }
        # 归一化 value = m · 2^e（m ∈ [1, 2)）
        Float64 m = value
        Int32 e = 0
        while m >= 2.0d
        {
            m = m * 0.5d
            e = e + 1
        }
        while m < 1.0d
        {
            m = m * 2.0d
            e = e - 1
        }
        # atanh 级数：ln(m) = 2·Σ u^(2i+1)/(2i+1)，u = (m-1)/(m+1) ∈ [0, 1/3)
        Float64 u = ( m - 1.0d ) / ( m + 1.0d )
        Float64 u2 = u * u
        Float64 sum = 0.0d
        Float64 term = u
        Int32 i = 1
        while i <= 33
        {
            sum = sum + term / i.toFloat64()
            term = term * u2
            i = i + 2
        }
        ret sum * 2.0d + e.toFloat64() * 0.6931471805599453d
    }

    private static Float64 floorFallback( Float64 value )
    {
        Int32 i = value.toInt32()
        Float64 fi = i.toFloat64()
        if value < fi
        {
            fi = fi - 1.0d
        }
        ret fi
    }

    private static Float64 roundFallback( Float64 value )
    {
        # 半值远离零（与 C round 一致）
        if value >= 0.0d
        {
            ret Mathd.floorFallback( value + 0.5d )
        }
        ret 0.0d - Mathd.floorFallback( 0.0d - value + 0.5d )
    }

    # ── 绝对值 ───────────────────────────────────────────
    public static Int32 abs( Int32 value )
    {
        if value < 0
        {
            ret 0 - value
        }
        ret value
    }

    public static Float64 abs( Float64 value )
    {
        if value < 0.0d
        {
            ret 0.0d - value
        }
        ret value
    }

    # ── 最小 / 最大 ──────────────────────────────────────
    public static Int32 min( Int32 a, Int32 b )
    {
        if a < b
        {
            ret a
        }
        ret b
    }

    public static Float64 min( Float64 a, Float64 b )
    {
        if a < b
        {
            ret a
        }
        ret b
    }

    public static Int32 max( Int32 a, Int32 b )
    {
        if a > b
        {
            ret a
        }
        ret b
    }

    public static Float64 max( Float64 a, Float64 b )
    {
        if a > b
        {
            ret a
        }
        ret b
    }

    # ── 区间限定 ─────────────────────────────────────────
    public static Int32 clamp( Int32 value, Int32 minValue, Int32 maxValue )
    {
        if value < minValue
        {
            ret minValue
        }
        if value > maxValue
        {
            ret maxValue
        }
        ret value
    }

    public static Float64 clamp( Float64 value, Float64 minValue, Float64 maxValue )
    {
        if value < minValue
        {
            ret minValue
        }
        if value > maxValue
        {
            ret maxValue
        }
        ret value
    }

    # ── 符号 ─────────────────────────────────────────────
    public static Int32 sign( Int32 value )
    {
        if value > 0
        {
            ret 1
        }
        if value < 0
        {
            ret 0 - 1
        }
        ret 0
    }

    public static Int32 sign( Float64 value )
    {
        if value > 0.0d
        {
            ret 1
        }
        if value < 0.0d
        {
            ret 0 - 1
        }
        ret 0
    }

    # ── 距离 ─────────────────────────────────────────────
    public static Float64 distance( Float64 x1, Float64 y1, Float64 x2, Float64 y2 )
    {
        Float64 dx = x2 - x1
        Float64 dy = y2 - y1
        ret Mathd.sqrt( dx * dx + dy * dy )
    }

    public static Float64 distance3D( Float64 x1, Float64 y1, Float64 z1, Float64 x2, Float64 y2, Float64 z2 )
    {
        Float64 dx = x2 - x1
        Float64 dy = y2 - y1
        Float64 dz = z2 - z1
        ret Mathd.sqrt( dx * dx + dy * dy + dz * dz )
    }

    # ── 插值 ─────────────────────────────────────────────
    public static Float64 lerp( Float64 a, Float64 b, Float64 t )
    {
        ret a + ( b - a ) * t
    }

    public static Float64 lerpClamped( Float64 a, Float64 b, Float64 t )
    {
        ret a + ( b - a ) * Mathd.clamp( t, 0.0d, 1.0d )
    }

    # ── 角度转换 ─────────────────────────────────────────
    public static Float64 degrees( Float64 radians )
    {
        ret radians * 180.0d / Mathd.Pi
    }

    public static Float64 radians( Float64 degrees )
    {
        ret degrees * Mathd.Pi / 180.0d
    }

    # ── 扩展工具 ─────────────────────────────────────────
    public static bool approximately( Float64 a, Float64 b, Float64 epsilon = 0.0000000001d )
    {
        ret Mathd.abs( a - b ) <= epsilon
    }

    # 取模（结果恒为非负）
    public static Int32 mod( Int32 value, Int32 m )
    {
        Int32 r = value % m
        if r < 0
        {
            r = r + m
        }
        ret r
    }

    # 整数快速幂
    public static Int32 powInt( Int32 baseValue, Int32 exponent )
    {
        Int32 result = 1
        Int32 b = baseValue
        Int32 e = exponent
        while e > 0
        {
            if e % 2 == 1
            {
                result = result * b
            }
            b = b * b
            e = e / 2
        }
        ret result
    }
}
