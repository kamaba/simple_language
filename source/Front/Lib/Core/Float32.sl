import CSharpLang.SimpleLanguage
import CSharp.System;

public class Float32 extends Num
{
    public const Float32 Epsilen = 4.9123213f;
    public const Float32 MaxValue = 20.0f;
    public const Float32 MinValue = -1.0f;

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
        ret SimpleLanguage.Lib.Float32Class.ToInt32(this)
    }
    public override Float64 toFloat64()
    {
        #convert to double
        ret this._value
    }
    public override Num abs()
    {
        ret SimpleLanguage.Lib.Float32Class.Abs(this)
    }
    public override Num floor()
    {
        ret SimpleLanguage.Lib.Float32Class.Floor(this)
    }
    public override Num ceil()
    {
        ret SimpleLanguage.Lib.Float32Class.Ceil(this)
    }
    public override Int32 compareTo( Num other )
    {
        if (other == null){ret 1} 
        Float64 ov = other.toFloat64()
        if (this._value == ov){ ret 0 }
        ret this._value > ov ? 1 : -1
    }
}