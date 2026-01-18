import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

public class UInt32 extends Num
{
    const uint MaxValue = 0x7fffffff;
    const uint MinValue = 0;

    UInt32 _value = 0iu;
    
    static String UInt32ToString( UInt32 value )
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( value )
    }
    public static UInt32 parseString( string s )
    {
        ret 0
    }
    _init_( UInt32 _val )
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
    T cast<T>()
    {
        ret null
    }
    !#
    public int compareTo(Int32 value)
    {
        if (value == null)
        {
            ret 1;
        }
        ret 0
    }
    #!
    Int8 toInt8()
    {
        ret 0
    }
    SInt8 toSInt8()
    {
        ret 0
    }
    Int16 toSInt16()
    {
        ret 0
    }
    UInt16 toUInt16()
    {
        ret 0
    }
    UInt32 toUInt32()
    {
        ret 0
    }
    Float32 toFloat32()
    {
        ret 0
    }
    Float64 toFloat64()
    {
        ret 0
    }
    !#
    override String toString()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( this )
    }
}