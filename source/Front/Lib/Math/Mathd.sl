# Math —— Float64（double）精度数学库，默认通用入口。
#
# 底层能力：由 MathNativeImpl.dll 通过 SystemCallExternalFunction("Mathd.xxx", ...)
# 注册提供（双精度版本）。纯算术能力（abs / min / max / clamp / sign ...）在 SL 层实现。
#
# 精度分组约定：
#   Math  -> Float64（本文件）
#   Mathf -> Float32（见 Mathf.sl）
#   Mathh -> Float16（见 Mathh.sl）
public class Mathd
{
    public const static Float64 Pi = 3.141592653589793d
    public const static Float64 E = 2.718281828459045d

    # ── 三角函数 ─────────────────────────────────────────
    public static Float64 sin( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.sin", value ) as Float64
    }

    public static Float64 cos( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.cos", value ) as Float64
    }

    public static Float64 tan( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.tan", value ) as Float64
    }

    public static Float64 asin( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.asin", value ) as Float64
    }

    public static Float64 acos( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.acos", value ) as Float64
    }

    public static Float64 atan( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.atan", value ) as Float64
    }

    public static Float64 atan2( Float64 y, Float64 x )
    {
        ret SystemCallExternalFunction( "Mathd.atan2", y, x ) as Float64
    }

    # ── 双曲函数 ─────────────────────────────────────────
    public static Float64 sinh( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.sinh", value ) as Float64
    }

    public static Float64 cosh( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.cosh", value ) as Float64
    }

    public static Float64 tanh( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.tanh", value ) as Float64
    }

    # ── 幂与对数 ─────────────────────────────────────────
    public static Float64 pow( Float64 baseValue, Float64 exponent )
    {
        ret SystemCallExternalFunction( "Mathd.pow", baseValue, exponent ) as Float64
    }

    public static Float64 sqrt( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.sqrt", value ) as Float64
    }

    public static Float64 exp( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.exp", value ) as Float64
    }

    public static Float64 log( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.log", value ) as Float64
    }

    public static Float64 log10( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.log10", value ) as Float64
    }

    # ── 取整 ─────────────────────────────────────────────
    public static Float64 ceil( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.ceil", value ) as Float64
    }

    public static Float64 floor( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.floor", value ) as Float64
    }

    public static Float64 round( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.round", value ) as Float64
    }

    public static Int32 truncate( Float64 value )
    {
        ret SystemCallExternalFunction( "Mathd.truncate", value ) as Int32
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
