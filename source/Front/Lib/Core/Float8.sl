@Nickname("Float8_E4M3")
public class Float8 extends Num
{
    # e4m3: 1位符号 + 4位指数(bias=7) + 3位尾数, 无穷大编码, 仅有NaN
    public const static Float8 Epsilon = 0.125fe4;              # 机器精度: 1与下一个可表示数的差 2^-3
    public const static Float8 MaxValue = 448.0fe4;             # 最大有限值 1.75 * 2^8
    public const static Float8 MinValue = -448.0fe4;            # 最小有限值
    public const static Float8 MinPositive = 0.001953125fe4;    # 最小正次正规数 2^-9

    Float8 _value = 0.0fe4

    public void _init_( Float8 f )
    {
        this._value = f
    }

    override get int size() { ret 8 }
    override get int byteLength() { ret 1 }

    public static bool isNaN( Float8 f )
    {
        ret !(f == f)
    }
    public static bool isInfinite( Float8 f )
    {
        # e4m3 没有无穷大编码, 恒为 false
        ret f > Float8.MaxValue || f < Float8.MinValue
    }
    public static bool isFinite( Float8 f )
    {
        ret f == f && f >= Float8.MinValue && f <= Float8.MaxValue
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

public class Float8_E5M2 extends Num
{
    # e5m2: 1位符号 + 5位指数(bias=15) + 2位尾数, 保留Inf/NaN编码
    public const static Float8_E5M2 Epsilon = 0.25fe5;                  # 机器精度 2^-2
    public const static Float8_E5M2 MaxValue = 57344.0fe5;              # 最大有限值 1.75 * 2^15
    public const static Float8_E5M2 MinValue = -57344.0fe5;             # 最小有限值
    public const static Float8_E5M2 MinPositive = 1.52587890625e-5fe5;  # 最小正次正规数 2^-16

    Float8_E5M2 _value = 0.0fe5

    public void _init_( Float8_E5M2 f )
    {
        this._value = f
    }

    override get int size() { ret 8 }
    override get int byteLength() { ret 1 }

    public static bool isNaN( Float8_E5M2 f )
    {
        ret !(f == f)
    }
    public static bool isInfinite( Float8_E5M2 f )
    {
        ret f > Float8_E5M2.MaxValue || f < Float8_E5M2.MinValue
    }
    public static bool isFinite( Float8_E5M2 f )
    {
        ret f == f && f >= Float8_E5M2.MinValue && f <= Float8_E5M2.MaxValue
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
