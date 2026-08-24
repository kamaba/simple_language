public class Int8 extends Num
{
    const static Int8 MaxValue = 127;    # 0b0111_1111
    const static Int8 MinValue = -128;   # 0b1000_0000
    Int8 _value = 0;

    override get int size() { ret 8 }
    override get int byteLength() { ret 1 }


    _init_( Int8 _val )
    {
        this._value = _val
    }

    public static Int8 parse( string s )
    {
        ret SystemConvertInt8( s )
    }
    public override Num abs()
    {
        ret SystemConvertInt8(SystemNumAbs(this))
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
        Int8 ov = SystemConvertInt8(other)
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : 0-1
    }
    override bool toBool()
    {
        ret SystemConvertBool(this)
    }
    override Int8 toInt8( UInt8 index = 0 )
    {
        ret this._value
    }
    override UInt8 toUInt8( UInt8 index = 0 )
    {
        ret SystemConvertUInt8(this)
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
        if (this._value == 0) { ret 0 }
        ret this._value > 0 ? 1 : 0-1
    }
    public bool isEven()
    {
        ret (this.toInt32() & 1) == 0
    }
    public bool isOdd()
    {
        ret (this.toInt32() & 1) != 0
    }
    public String toRadixString(int radix)
    {
        ret SystemConvertInt32ToRadixString(this.toInt32(), radix)
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
