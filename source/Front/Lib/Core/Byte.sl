import CSharpLang.SimpleLanguage

public class Byte extends Num
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

    override get int size() { ret 32 }
    override get int byteLength() { ret 4 }

    public override Byte abs()
    {
        ret 0
    } 
    public override Byte floor()
    {
        ret this
    }
    public override Byte ceil()
    {
        ret this
    }
    public override int compareTo(Byte value)
    {
        if (value == null)
        {
            ret 1;
        }
        if (this._value == value ){ ret 0; }
        ret this._value > value._value ? 1 : -1
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