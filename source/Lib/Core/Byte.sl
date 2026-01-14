import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

public class Byte extends Object
{    
    const Byte MaxValue = 0b1111111;
    const Byte MinValue = 0b0000000;
    Byte _value = 0;    

    static String ByteToString( Byte value )
    {
        ret SimpleLanguage.Lib.ByteClass.ByteToString( value )
    }
    public static Byte parseString( string s )
    {
        ret 0
    }
    _init_( Byte _val )
    {
        this._value = _val
    }
    public int compareTo(Int32 value)
    {
        if (value == null)
        {
            ret 1;
        }
        ret 0
    }
    #!
    Byte toByte()
    {
        ret 0
    }
    SByte toSByte()
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
        ret SimpleLanguage.Lib.ByteClass.ByteToString( this )
    }
}