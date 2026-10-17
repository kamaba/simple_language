public class Float16 extends Num
{
    # IEEE 754 binary16 (half): 1位符号 + 5位指数(bias=15) + 10位尾数
    public const static Float16 Epsilon = 0.0009765625h;                 # 机器精度 2^-10
    public const static Float16 MaxValue = 65504.0h;                     # 最大有限值 1.1111111111 * 2^15
    public const static Float16 MinValue = -65504.0h;                    # 最小有限值
    public const static Float16 MinPositive = 5.9604644775390625e-8h;    # 最小正次正规数 2^-24

    Float16 _value = 0.0h

    public void _init_( Float16 f )
    {
        this._value = f
    }

    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }

    public static bool isNaN( Float16 f )
    {
        ret !(f == f)
    }
    public static bool isInfinite( Float16 f )
    {
        ret f > Float16.MaxValue || f < Float16.MinValue
    }
    public static bool isFinite( Float16 f )
    {
        ret f == f && f >= Float16.MinValue && f <= Float16.MaxValue
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

public class Float16_Brain extends Num
{
    # bfloat16: 1位符号 + 8位指数(bias=127, 与Float32相同) + 7位尾数, 动态范围大精度低
    public const static Float16_Brain Epsilon = 0.0078125hb;                 # 机器精度 2^-7
    public const static Float16_Brain MaxValue = 3.3895313892515355e38hb;    # 最大有限值 1.1111111 * 2^127
    public const static Float16_Brain MinValue = -3.3895313892515355e38hb;   # 最小有限值
    public const static Float16_Brain MinPositive = 9.183549615799121e-41hb; # 最小正次正规数 2^-133

    Float16_Brain _value = 0.0hb

    public void _init_( Float16_Brain f )
    {
        this._value = f
    }

    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }

    public static bool isNaN( Float16_Brain f )
    {
        ret !(f == f)
    }
    public static bool isInfinite( Float16_Brain f )
    {
        ret f > Float16_Brain.MaxValue || f < Float16_Brain.MinValue
    }
    public static bool isFinite( Float16_Brain f )
    {
        ret f == f && f >= Float16_Brain.MinValue && f <= Float16_Brain.MaxValue
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
