import CSharp.System;

public class Float32 extends Num
{
    public const static Float32 Epsilen = 4.9123213f;
    public const static Float32 MaxValue = 20.0f;
    public const static Float32 MinValue = -1.0f;

    Float32 _value = 0.0f

    public void _init_( Float32 f )
    {
        this._value = f
    }
    
    override get int size() { ret 32 }
    override get int byteLength() { ret 4 }
    
    public static bool isFinite( Float32 f )
    {
        #delegate to CLR Math if available
        ret false;
    }
    #!
    public override String toString( string format )
    {
        return string.format( format, this._value );
    }
    !#
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
    public override Int32 compareTo( Num other )
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
}