# Mathh —— Float16（half）精度数学库。
#
# 实现说明：
# 运行时目前未注册 half 版外部函数，也没有 SystemConvertFloat16 转换调用，
# 因此本类以 Float32 作为中间精度计算（half 仅 10 位尾数，Float32 中转不会引入额外误差），
# 结果再隐式收敛回 Float16。
#
# 若后续在 MathNativeImpl 中注册 "Mathh.xxx"（System.Half 版本），
# 可把每个方法替换为：ret SystemCallExternalFunction( "Mathh.sin", value ) as Float16
#
# 精度分组约定：
#   Mathd -> Float64（见 Mathd.sl）
#   Mathf -> Float32（见 Mathf.sl）
#   Mathh -> Float16（本文件）
public class Mathh
{
    # half 表示下的常量（写入时按 Float16 位宽截断）
    public const static Float16 Pi = 3.141592653589793h
    public const static Float16 E = 2.718281828459045h

    # ── 三角函数 ─────────────────────────────────────────
    public static Float16 sin( Float16 value )
    {
        ret Mathf.sin( value.toFloat32() )
    }

    public static Float16 cos( Float16 value )
    {
        ret Mathf.cos( value.toFloat32() )
    }

    public static Float16 tan( Float16 value )
    {
        ret Mathf.tan( value.toFloat32() )
    }

    public static Float16 asin( Float16 value )
    {
        ret Mathf.asin( value.toFloat32() )
    }

    public static Float16 acos( Float16 value )
    {
        ret Mathf.acos( value.toFloat32() )
    }

    public static Float16 atan( Float16 value )
    {
        ret Mathf.atan( value.toFloat32() )
    }

    public static Float16 atan2( Float16 y, Float16 x )
    {
        ret Mathf.atan2( y.toFloat32(), x.toFloat32() )
    }

    # ── 双曲函数 ─────────────────────────────────────────
    public static Float16 sinh( Float16 value )
    {
        ret Mathf.sinh( value.toFloat32() )
    }

    public static Float16 cosh( Float16 value )
    {
        ret Mathf.cosh( value.toFloat32() )
    }

    public static Float16 tanh( Float16 value )
    {
        ret Mathf.tanh( value.toFloat32() )
    }

    # ── 幂与对数 ─────────────────────────────────────────
    public static Float16 pow( Float16 baseValue, Float16 exponent )
    {
        ret Mathf.pow( baseValue.toFloat32(), exponent.toFloat32() )
    }

    public static Float16 sqrt( Float16 value )
    {
        ret Mathf.sqrt( value.toFloat32() )
    }

    public static Float16 exp( Float16 value )
    {
        ret Mathf.exp( value.toFloat32() )
    }

    public static Float16 log( Float16 value )
    {
        ret Mathf.log( value.toFloat32() )
    }

    public static Float16 log10( Float16 value )
    {
        ret Mathf.log10( value.toFloat32() )
    }

    # ── 取整 ─────────────────────────────────────────────
    public static Float16 ceil( Float16 value )
    {
        ret Mathf.ceil( value.toFloat32() )
    }

    public static Float16 floor( Float16 value )
    {
        ret Mathf.floor( value.toFloat32() )
    }

    public static Float16 round( Float16 value )
    {
        ret Mathf.round( value.toFloat32() )
    }

    public static Int32 truncate( Float16 value )
    {
        ret Mathf.truncate( value.toFloat32() )
    }

    # ── 绝对值 ───────────────────────────────────────────
    public static Float16 abs( Float16 value )
    {
        if value < 0.0h
        {
            ret 0.0h - value
        }
        ret value
    }

    # ── 最小 / 最大 ──────────────────────────────────────
    public static Float16 min( Float16 a, Float16 b )
    {
        if a < b
        {
            ret a
        }
        ret b
    }

    public static Float16 max( Float16 a, Float16 b )
    {
        if a > b
        {
            ret a
        }
        ret b
    }

    # ── 区间限定 ─────────────────────────────────────────
    public static Float16 clamp( Float16 value, Float16 minValue, Float16 maxValue )
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
    public static Int32 sign( Float16 value )
    {
        if value > 0.0h
        {
            ret 1
        }
        if value < 0.0h
        {
            ret 0 - 1
        }
        ret 0
    }

    # ── 距离 ─────────────────────────────────────────────
    public static Float16 distance( Float16 x1, Float16 y1, Float16 x2, Float16 y2 )
    {
        Float16 dx = x2 - x1
        Float16 dy = y2 - y1
        ret Mathh.sqrt( dx * dx + dy * dy )
    }

    public static Float16 distance3D( Float16 x1, Float16 y1, Float16 z1, Float16 x2, Float16 y2, Float16 z2 )
    {
        Float16 dx = x2 - x1
        Float16 dy = y2 - y1
        Float16 dz = z2 - z1
        ret Mathh.sqrt( dx * dx + dy * dy + dz * dz )
    }

    # ── 插值 ─────────────────────────────────────────────
    public static Float16 lerp( Float16 a, Float16 b, Float16 t )
    {
        ret Mathf.lerp( a.toFloat32(), b.toFloat32(), t.toFloat32() )
    }

    public static Float16 lerpClamped( Float16 a, Float16 b, Float16 t )
    {
        ret Mathf.lerpClamped( a.toFloat32(), b.toFloat32(), t.toFloat32() )
    }

    # ── 角度转换 ─────────────────────────────────────────
    public static Float16 degrees( Float16 radians )
    {
        ret Mathf.degrees( radians.toFloat32() )
    }

    public static Float16 radians( Float16 degrees )
    {
        ret Mathf.radians( degrees.toFloat32() )
    }

    # ── 扩展工具 ─────────────────────────────────────────
    # half 的机器精度为 2^-10，epsilon 取 0.0009765625
    public static bool approximately( Float16 a, Float16 b, Float16 epsilon = 0.0009765625h )
    {
        ret Mathh.abs( a - b ) <= epsilon
    }
}
