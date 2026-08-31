# Mathf —— Float32（single）精度数学库。
#
# 底层能力：由 MathNativeImpl.dll 通过 SystemCallExternalFunction("Math.xxx", ...)
# 注册提供（单精度版本，见 MathExternalModule.cs）。
# 纯算术能力（abs / min / max / clamp / sign ...）在 SL 层实现。
#
# 精度分组约定：
#   Mathd -> Float64（见 Mathd.sl）
#   Mathf -> Float32（本文件）
#   Mathh -> Float16（见 Mathh.sl）
public class Mathf
{
    public const static Float32 Pi = 3.141592653589793f
    public const static Float32 E = 2.718281828459045f

    # ── 三角函数 ─────────────────────────────────────────
    public static Float32 sin( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.sin", value ) as Float32
    }

    public static Float32 cos( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.cos", value ) as Float32
    }

    public static Float32 tan( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.tan", value ) as Float32
    }

    public static Float32 asin( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.asin", value ) as Float32
    }

    public static Float32 acos( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.acos", value ) as Float32
    }

    public static Float32 atan( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.atan", value ) as Float32
    }

    public static Float32 atan2( Float32 y, Float32 x )
    {
        ret SystemCallExternalFunction( "Math.atan2", y, x ) as Float32
    }

    # ── 双曲函数 ─────────────────────────────────────────
    public static Float32 sinh( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.sinh", value ) as Float32
    }

    public static Float32 cosh( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.cosh", value ) as Float32
    }

    public static Float32 tanh( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.tanh", value ) as Float32
    }

    # ── 幂与对数 ─────────────────────────────────────────
    public static Float32 pow( Float32 baseValue, Float32 exponent )
    {
        ret SystemCallExternalFunction( "Math.pow", baseValue, exponent ) as Float32
    }

    public static Float32 sqrt( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.sqrt", value ) as Float32
    }

    public static Float32 exp( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.exp", value ) as Float32
    }

    public static Float32 log( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.log", value ) as Float32
    }

    public static Float32 log10( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.log10", value ) as Float32
    }

    # ── 取整 ─────────────────────────────────────────────
    public static Float32 ceil( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.ceil", value ) as Float32
    }

    public static Float32 floor( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.floor", value ) as Float32
    }

    public static Float32 round( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.round", value ) as Float32
    }

    public static Int32 truncate( Float32 value )
    {
        ret SystemCallExternalFunction( "Math.truncate", value ) as Int32
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

    public static Float32 abs( Float32 value )
    {
        if value < 0.0f
        {
            ret 0.0f - value
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

    public static Float32 min( Float32 a, Float32 b )
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

    public static Float32 max( Float32 a, Float32 b )
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

    public static Float32 clamp( Float32 value, Float32 minValue, Float32 maxValue )
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

    public static Int32 sign( Float32 value )
    {
        if value > 0.0f
        {
            ret 1
        }
        if value < 0.0f
        {
            ret 0 - 1
        }
        ret 0
    }

    # ── 距离 ─────────────────────────────────────────────
    public static Float32 distance( Float32 x1, Float32 y1, Float32 x2, Float32 y2 )
    {
        Float32 dx = x2 - x1
        Float32 dy = y2 - y1
        ret Mathf.sqrt( dx * dx + dy * dy )
    }

    public static Float32 distance3D( Float32 x1, Float32 y1, Float32 z1, Float32 x2, Float32 y2, Float32 z2 )
    {
        Float32 dx = x2 - x1
        Float32 dy = y2 - y1
        Float32 dz = z2 - z1
        ret Mathf.sqrt( dx * dx + dy * dy + dz * dz )
    }

    # ── 插值 ─────────────────────────────────────────────
    public static Float32 lerp( Float32 a, Float32 b, Float32 t )
    {
        ret a + ( b - a ) * t
    }

    public static Float32 lerpClamped( Float32 a, Float32 b, Float32 t )
    {
        ret a + ( b - a ) * Mathf.clamp( t, 0.0f, 1.0f )
    }

    # ── 角度转换 ─────────────────────────────────────────
    public static Float32 degrees( Float32 radians )
    {
        ret radians * 180.0f / Mathf.Pi
    }

    public static Float32 radians( Float32 degrees )
    {
        ret degrees * Mathf.Pi / 180.0f
    }

    # ── 扩展工具 ─────────────────────────────────────────
    public static bool approximately( Float32 a, Float32 b, Float32 epsilon = 0.000001f )
    {
        ret Mathf.abs( a - b ) <= epsilon
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
