@Nickname("Float8_E4M3")
public class Float8 extends Num
{
    public const static Float8 Epsilen = 4.9128e-4;
    public const static Float8 MaxValue = 20.0f;
    public const static Float8 MinValue = -1.0f;

    Float8 _value = 0.0f

    public void _init_( Float8 f )
    {
        this._value = f
    }
    
    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }
    
    public static bool isFinite( Float8 f )
    {
        #delegate to CLR Math if available
        ret false;
    }
    public override Int32 toInt32()
    {
        ret SystemConvertInt32(this)
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
        ret string.toFormat( format, this._value );
    }
}

public class Float8_E5M2 extends Num
{
    public const static Float8_E5M2 Epsilen = 4.9128e-4;
    public const static Float8_E5M2 MaxValue = 20.0f;
    public const static Float8_E5M2 MinValue = -1.0f;

    Float8_E5M2 _value = 0.0f

    public void _init_( Float8_E5M2 f )
    {
        this._value = f
    }
    
    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }
    
    public static bool isFinite( Float8 f )
    {
        #delegate to CLR Math if available
        ret false;
    }
    public override Int32 toInt32()
    {
        ret SystemConvertInt32(this)
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
        ret string.toFormat( format, this._value );
    }
}