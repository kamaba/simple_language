import CSharp.System;

public class Float32 extends Num
{
    # IEEE 754 binary32 (single): 1位符号 + 8位指数(bias=127) + 23位尾数
    public const static Float32 Epsilon = 1.1920928955078125e-7f;    # 机器精度 2^-23
    public const static Float32 MaxValue = 3.4028234663852886e38f;  # 最大有限值
    public const static Float32 MinValue = -3.4028234663852886e38f; # 最小有限值
    public const static Float32 MinPositive = 1.401298464324817e-45f; # 最小正次正规数 2^-149

    Float32 _value = 0.0f

    public void _init_( Float32 f )
    {
        this._value = f
    }

    override get int size() { ret 32 }
    override get int byteLength() { ret 4 }

    public static bool isNaN( Float32 f )
    {
        ret !(f == f)
    }
    public static bool isInfinite( Float32 f )
    {
        ret f > Float32.MaxValue || f < Float32.MinValue
    }
    public static bool isFinite( Float32 f )
    {
        ret f == f && f >= Float32.MinValue && f <= Float32.MaxValue
    }
    public override Int32 toInt32()
    {
        ret SystemConvertInt32(this)
    }
    public override Float32 toFloat32()
    {
        ret this._value
    }
    public override Float64 toFloat64()
    {
        ret SystemConvertFloat64(this)
    }
    public override Num abs()
    {
        ret SystemNumAbs(this)
    }
    public override Num floor()
    {
        ret SystemNumFloor(this)
    }
    public override Num ceil()
    {
        Num fl = SystemNumFloor(this)
        if (this._value > fl)
        {
            ret fl + 1.0d
        }
        ret this
    }
    public override Int8 compareTo( Num other )
    {
        if (other == null){ret 1}
        Float64 ov = other.toFloat64()
        if (this._value == ov){ ret 0 }
        ret this._value > ov ? 1 : 0-1
    }

    override String toString()
    {
        ret SystemConvertString(this)
    }
    public String toString( string format )
    {
        ret String.toFormat( format, this._value );
    }
}
