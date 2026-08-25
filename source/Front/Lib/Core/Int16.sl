public class Int16 extends Num
{
    public const static Int16 MaxValue = 0x7fff;
    public const static Int16 MinValue = -32768;

    Int16 _value = 0;

    _init_( Int16 _val )
    {
        this._value = _val
    }
    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }

    public static Int16 parse( string s )
    {
        ret SystemConvertInt16( s )
    }
    public override Num abs()
    {
        ret SystemConvertInt16(SystemNumAbs(this))
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
        Int16 ov = SystemConvertInt16(other)
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
        ret this._value
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
