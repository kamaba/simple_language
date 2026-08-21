public class Float16 extends Num
{
    public const static Float16 Epsilen = 4.9123213f;
    public const static Float16 MaxValue = 20.0f;
    public const static Float16 MinValue = -1.0f;

    Float16 _value = 0.0f

    public void _init_( Float16 f )
    {
        this._value = f
    }
    
    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }
    
    public static bool isFinite( Float16 f )
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

public class Float16Brain extends Num
{
    public const static Float16Brain Epsilen = 4.9123213f;
    public const static Float16Brain MaxValue = 20.0f;
    public const static Float16Brain MinValue = -1.0f;

    Float16Brain _value = 0.0f

    public void _init_( Float16Brain f )
    {
        this._value = f
    }
    
    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }
    
    public static bool isFinite( Float16Brain f )
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