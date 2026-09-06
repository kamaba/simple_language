public class Int64 extends Num
{
    public const static  Int64 MaxValue = 0x7fffffff
    public const static Int64 MinValue = 0x80000000
   
    Int64 _value = 0L;  
    _init_( Int64 _val )
    {
        this._value = _val
    }
    #size helpers
    override get int size() { ret 64 }
    override get int byteLength() { ret 8 }
    
    public override Num abs()
    {
        ret SystemConvertInt64(SystemNumAbs(this))
    } 
    public override Num floor()
    {
        ret SystemNumFloor(this)
    }
    public override Num ceil()
    {
        ret this
    }
    public override Int8 compareTo(Num other)
    {
        if (other == null) { ret 1 }
        Int64 ov = SystemConvertInt64(other)
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : 0-1
    }
    override bool toBool()
    {
        ret SystemConvertBool(this) 
    }
    override Int8 toInt8( UInt8 index = 0 )
    {
        ret SystemConvertInt8(this, index)
    }
    override UInt8 toUInt8( UInt8 index = 0 )
    {
        ret SystemConvertUInt8(this, index)
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
        ret SystemConvertInt32(this)
    }
    override UInt32 toUInt32()
    {
        ret SystemConvertUInt32(this)
    }
    override Int64 toInt64()
    {
        ret this._value
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
        ret this._value > 0 ? 1 : 0-1
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