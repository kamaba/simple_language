import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

public class SByte extends Object
{    
    const SByte MaxValue = 0b1111111;
    const SByte MinValue = 0b0000000;
    SByte _value = 0;    

    static String SByteToString( SByte value )
    {
        ret SimpleLanguage.Lib.SByteClass.SByteToString( value )
    }
    public static SByte parseString( string s )
    {
        ret 0
    }
    _init_( SByte _val )
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
        ret SimpleLanguage.Lib.SByteClass.SByteToString( this )
    }
}