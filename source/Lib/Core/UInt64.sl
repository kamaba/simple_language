import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

public class UInt64 extends Num
{
    const UInt64 MaxValue = 0x7fffffff;
    const UInt64 MinValue = 0;

    UInt32 _value = 0iu;
    
    static String UInt64ToString( UInt64 value )
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( value )
    }
    public static UInt64 parseString( string s )
    {
        try { ret (UInt64)System.Convert.ToUInt64(s) } catch { ret 0 }
    }
    _init_( UInt64 _val )
    {
        this._value = _val
    }
    override String toString()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( this )
    }

    public override Int32 toInt32()
    {
        ret (Int32)this._value
    }
    public override Float64 toFloat64()
    {
        ret (Float64)this._value
    }
    public override Num abs()
    {
        ret this
    }
    public override Num floor()
    {
        ret this
    }
    public override Num ceil()
    {
        ret this
    }
    public override Int32 compareTo( Num other )
    {
        if (other == null) { ret 1 }
        Float64 ov = other.toFloat64()
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : -1
    }
}