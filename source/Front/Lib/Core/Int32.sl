public class Int32 extends Num
{
    const int MaxValue = 0x7fff;
    const int MinValue = 0x8000;

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
        ret SystemInt32Parse(s)
    }    
    public override Int32 abs()
    {
        ret SystemNumAbs(this)
    } 
    public override Num floor()
    {
        ret SystemNumFloor(this)
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
        ret SystemConvertFloat64(this) != 0
    }
    override Byte toByte( byte index = 0 )
    {
        ret SystemConvertInt8(this)
    }
    override Byte toSByte( byte index = 0 )
    {
        ret SystemConvertSInt8(this)
    }
    override Int16 toInt16()
    {
        ret SystemConvertInt16(this)
    }
    override UInt16 toUInt16()
    {
        ret SystemConvertUInt16(this)
    }
    override Int32 toInt32()
    {
        ret this._value
    }
    override UInt32 toUInt32()
    {
        ret SystemConvertUInt32(this)
    }
    override Int64 toInt64()
    {
        ret SystemConvertInt64(this)
    }
    override Int64 toUInt64()
    {
        ret SystemConvertUInt64(this)
    }
    override Float32 toFloat32()
    {
        ret SystemConvertFloat32(this)
    }
    override Float64 toFloat64()
    {
        ret SystemConvertFloat64(this)
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
}