public class Int64 extends Num
{
    public const int MaxValue = 0x7fffffff;
    public const int MinValue = 0L
    #----------------    
    Int64 _value = 0L;  
    _init_( Int64 _val )
    {
        this._value = _val
    }
    #size helpers
    override get int size() { ret 64 }
    override get int byteLength() { ret 8 }
    
    public Int64? parse( string s )
    {
        #ret SimpleLanguage.Lib.Int32Class.Parse( s )
        ret 0
    }    
    public static Int64 parseInt64(string s)
    {
        #ret System.Convert.ToInt32(s);
        ret 0L
    }
    public override Int64 abs()
    {
        #ret SimpleLanguage.Lib.Int32Class.Abs(this._value)
        ret 0;
    } 
    public override Num floor()
    {
        ret this
    }
    public override Num ceil()
    {
        ret this
    }
    public override int compareTo(Int64 value)
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
        #ret SimpleLanguage.Lib.Int32Class.Int32ToBool(this._value )
    }
    override Byte toByte( byte index = 0 )
    {
        #ret SimpleLanguage.Lib.Int32Class.Int32ToByte(this._value, index )
    }
    override Byte toSByte( byte index = 0 )
    {
        #ret SimpleLanguage.Lib.Int32Class.Int32ToSByte(this._value, index )
    }
    override Int16 toInt16()
    {
        #ret SimpleLanguage.Lib.Int32Class.Int32ToInt16(this._value )
    }
    override UInt16 toUInt16()
    {
        #ret SimpleLanguage.Lib.Int32Class.Int32ToUInt16(this._value )
    }
    override Int32 toInt32()
    {
        ret this._value
    }
    override UInt32 toUInt32()
    {
        #ret SimpleLanguage.Lib.Int32Class.Int32ToUInt32(this._value )
    }
    override Int64 toInt64()
    {
        ret this._value
    }
    override Int64 toUInt64()
    {
        #ret SimpleLanguage.Lib.Int32Class.Int32ToUInt64(this._value )
    }
    override Float32 toFloat32()
    {
        #ret SimpleLanguage.Lib.Int32Class.Int32ToFloat32(this._value)
    }
    override Float64 toFloat64()
    {
        #ret SimpleLanguage.Lib.Int32Class.Int32ToFloat64(this._value)
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
        ret SystemConvertString( this )
    }
    #--------------------------
}