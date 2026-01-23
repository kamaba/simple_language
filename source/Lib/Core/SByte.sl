import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

public class SByte extends Num
{    
    const SByte MaxValue = 0b1111111;
    const SByte MinValue = 0b0000000;
    SByte _value = 0; 

    
    override get int size() { ret 32 }
    override get int byteLength() { ret 4 }   
    
    public override SByte abs()
    {
        ret 0
    } 
    public override SByte floor()
    {
        ret this
    }
    public override SByte ceil()
    {
        ret this
    }
    public override int compareTo(SByte value)
    {
        if (value == null)
        {
            ret 1;
        }
        if (this._value == value ){ ret 0; }
        ret this._value > value._value ? 1 : -1
    }

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