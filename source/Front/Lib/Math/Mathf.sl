# Mathf —— Float32（single）精度数学库。
#
# 底层能力：由 math_lib.dll（C ABI，见 source\Front\Lib\Math\cvm_math_lib\math_lib.c）
# 经 FFI @DllStaticImport 二参形式（静态注册名, 符号名）静态绑定：cvm 加载期按
# Math.jsonc dllImports 的 "static" 字段预载注册，静态调用点由编译期发射
# opcode 118 直调绑定表（sig 从函数签名自动推导），无 wrapper 委托转发开销；
# 绑定失败（库未注册 / 符号缺失 / sig 非法）时回退执行函数本体（fallback 委托
# Mathd 双精度实现后回转 Float32，链路同 Mathh 的中转模式）。
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

    # ── 三角函数（FFI mathf_* + 委托 Mathd fallback） ─────
    @DllStaticImport( "math_lib", "mathf_sin" )
    public static Float32 sin( Float32 value )
    {
        ret Mathd.sin( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_cos" )
    public static Float32 cos( Float32 value )
    {
        ret Mathd.cos( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_tan" )
    public static Float32 tan( Float32 value )
    {
        ret Mathd.tan( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_asin" )
    public static Float32 asin( Float32 value )
    {
        ret Mathd.asin( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_acos" )
    public static Float32 acos( Float32 value )
    {
        ret Mathd.acos( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_atan" )
    public static Float32 atan( Float32 value )
    {
        ret Mathd.atan( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_atan2" )
    public static Float32 atan2( Float32 y, Float32 x )
    {
        ret Mathd.atan2( y.toFloat64(), x.toFloat64() ).toFloat32()
    }

    # ── 双曲函数 ─────────────────────────────────────────
    @DllStaticImport( "math_lib", "mathf_sinh" )
    public static Float32 sinh( Float32 value )
    {
        ret Mathd.sinh( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_cosh" )
    public static Float32 cosh( Float32 value )
    {
        ret Mathd.cosh( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_tanh" )
    public static Float32 tanh( Float32 value )
    {
        ret Mathd.tanh( value.toFloat64() ).toFloat32()
    }

    # ── 幂与对数 ─────────────────────────────────────────
    @DllStaticImport( "math_lib", "mathf_pow" )
    public static Float32 pow( Float32 baseValue, Float32 exponent )
    {
        ret Mathd.pow( baseValue.toFloat64(), exponent.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_sqrt" )
    public static Float32 sqrt( Float32 value )
    {
        ret Mathd.sqrt( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_exp" )
    public static Float32 exp( Float32 value )
    {
        ret Mathd.exp( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_log" )
    public static Float32 log( Float32 value )
    {
        ret Mathd.log( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_log10" )
    public static Float32 log10( Float32 value )
    {
        ret Mathd.log10( value.toFloat64() ).toFloat32()
    }

    # ── 取整 ─────────────────────────────────────────────
    @DllStaticImport( "math_lib", "mathf_ceil" )
    public static Float32 ceil( Float32 value )
    {
        ret Mathd.ceil( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_floor" )
    public static Float32 floor( Float32 value )
    {
        ret Mathd.floor( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_round" )
    public static Float32 round( Float32 value )
    {
        ret Mathd.round( value.toFloat64() ).toFloat32()
    }

    @DllStaticImport( "math_lib", "mathf_truncate" )
    public static Int32 truncate( Float32 value )
    {
        ret value.toInt32()
    }

    # ── 三维点积（矩阵 / 向量元素运算用，恰好 6 参 FFI 上限） ──
    @DllStaticImport( "math_lib", "mathf_dot3" )
    public static Float32 dot3( Float32 a0, Float32 a1, Float32 a2, Float32 b0, Float32 b1, Float32 b2 )
    {
        ret a0 * b0 + a1 * b1 + a2 * b2
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

    # 整数快速幂（result 是 Result 返回函数注入变量的上下文名，避免遮蔽歧义，此处改名 r）
    public static Int32 powInt( Int32 baseValue, Int32 exponent )
    {
        Int32 r = 1
        Int32 b = baseValue
        Int32 e = exponent
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
}
