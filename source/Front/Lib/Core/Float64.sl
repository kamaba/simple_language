import CSharp.System;

public class Float64 extends Num
{
    # IEEE 754 binary64 (double): 1位符号 + 11位指数(bias=1023) + 52位尾数
    public const static Float64 Epsilon = 2.220446049250313e-16d;       # 机器精度 2^-52
    public const static Float64 MaxValue = 1.7976931348623157e308d;     # 最大有限值
    public const static Float64 MinValue = -1.7976931348623157e308d;    # 最小有限值
    public const static Float64 MinPositive = 4.9406564584124654e-324d; # 最小正次正规数 2^-1074

    Float64 _value = 0.0d

    public void _init_( Float64 f )
    {
        this._value = f
    }

    override get int size() { ret 64 }
    override get int byteLength() { ret 8 }

    public static bool isNaN( Float64 f )
    {
        ret !(f == f)
    }
    public static bool isInfinite( Float64 f )
    {
        ret f > Float64.MaxValue || f < Float64.MinValue
    }
    public static bool isFinite( Float64 f )
    {
        ret f == f && f >= Float64.MinValue && f <= Float64.MaxValue
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
    public override Int8 compareTo(Num other)
    {
        if (other == null) { ret 1 }
        Float64 ov = other.toFloat64()
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : 0-1
    }
    public override Int32 toInt32()
    {
        ret SystemConvertInt32(this)
    }
    public override Float32 toFloat32()
    {
        ret SystemConvertFloat32(this)
    }
    public override Float64 toFloat64()
    {
        ret this._value
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
