import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage
import CSharp.System

public class Int32 extends Num
{
    const int MaxValue = 0x7fffffff;
    const int MinValue = 0x80000000;

    Int32 _value = 0i;
    
    
    _init_( Int32 _val )
    {
        this._value = _val
    }

    #size helpers
    override get int size() { ret 32 }
    override get int byteLength() { ret 4 }
    
    public Int32? parse( string s )
    {
        ret SimpleLanguage.Lib.Int32Class.Parse( s )
    }    
    public static Int32 parseInt(string s)
    {
        ret System.Convert.ToInt32(s);
    }
    public override Int32 abs()
    {
        ret SimpleLanguage.Lib.Int32Class.Abs(this._value)
    } 
    public override Num floor()
    {
        ret this
    }
    public override Num ceil()
    {
        ret this
    }
    public override int compareTo(Int32 value)
    {
        if (value == null)
        {
            ret 1;
        }
        if (this._value == value ){ ret 0; }
        ret this._value > value._value ? 1 : -1
    }
    override bool toBool()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToBool(this._value )
    }
    override Byte toByte( byte index = 0 )
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToByte(this._value, index )
    }
    override Byte toSByte( byte index = 0 )
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToSByte(this._value, index )
    }
    override Int16 toInt16()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToInt16(this._value )
    }
    override UInt16 toUInt16()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToUInt16(this._value )
    }
    override Int32 toInt32()
    {
        ret this._value
    }
    override UInt32 toUInt32()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToUInt32(this._value )
    }
    override Int64 toInt64()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToInt64(this._value )
    }
    override Int64 toUInt64()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToUInt64(this._value )
    }
    override Float32 toFloat32()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToFloat32(this._value)
    }
    override Float64 toFloat64()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToFloat64(this._value)
    }   
    
    public Int32 sign()
    {
        ret this._value > 0 ? 1 : -1
    }
    public bool isEven()
    {
        ret (this._value & 1) == 0
    }
    public bool isOdd()
    {
        ret (this._value & 1) != 0
    }
    public String toRadixString(int radix)
    {
        ret "";
    }
    public String toBinaryString()
    {
        ret this.toRadixString(2)
    }
    public String toHexString()
    {
        ret this.toRadixString(16)
    }
    public String toOctalString()
    {
        ret this.toRadixString(8)
    }
    override String toString()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( this )
    }
}