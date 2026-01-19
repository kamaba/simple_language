import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage
import CSharp.System

public class Int32 extends Num
{
    const int MaxValue = 0x7fffffff;
    const int MinValue = 0x80000000;

    Int32 _value = 0i;
    
    static String Int32ToString( Int32 value )
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( value )
    }
    public static Int32 parseString( string s )
    {
        # simple parse
        #try
        #{
            var v = System.Convert.ToInt32(s);
            ret v
        #}
        #catch
        #{
        #    ret 0
        #}
    }
    _init_( Int32 _val )
    {
        this._value = _val
    }
    #!
    _init_( Float32 f )
    {
        this._value = f.toInt32()
    }
    _init_( Int8 _val )
    {
        this._value = _val.toInt32()
    }
    _init_( Int64 _val )
    {
        this._value = _val.toInt32()
    }
    !#
    public int compareTo(Int32 value)
    {
        if (value == null)
        {
            ret 1;
        }
        if (this._value == value ){ ret 0; }
        ret this._value > value._value ? 1 : -1
    }
    #!
    Int8 toInt8()
    {
        ret (Int8)this._value
    }
    SInt8 toSInt8()
    {
        ret (SInt8)this._value
    }
    Int16 toSInt16()
    {
        ret (Int16)this._value
    }
    UInt16 toUInt16()
    {
        ret (UInt16)this._value
    }
    UInt32 toUInt32()
    {
        ret (UInt32)this._value
    }
    Float32 toFloat32()
    {
        ret (Float32)this._value
    }
    Float64 toFloat64()
    {
        #convert to double
        ret this._value
    }
    !#
    override String toString()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( this )
    }

    public override Int32 toInt32()
    {
        ret this
    }
    public override Float64 toFloat64()
    {
        ret this._value
    }
    public override Num abs()
    {
        ret Int32( System.Math.Abs(this) )
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