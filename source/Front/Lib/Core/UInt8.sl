
public class UInt8 extends Num
{
    public const static UInt8 MaxValue = 0b11111111;
    public const static UInt8 MinValue = 0b00000000;
    UInt8 _value = 0;

    _init_(UInt8 _val)
    {
        this._value = _val
    }

    override get int size() { ret 8 }
    override get int byteLength() { ret 1 }

    public static UInt8 parse(string s)
    {
        ret SystemConvertUInt8(s)
    }

    public override Num abs()
    {
        ret this
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
        UInt8 ov = SystemConvertUInt8(other)
        if this._value == ov { ret 0 }
        ret this._value > ov ? 1 : 0-1
    }
    override bool toBool()
    {
        ret SystemConvertBool(this)
    }
    override Int8 toInt8( UInt8 index = 0 )
    {
        ret SystemConvertInt8(this)
    }
    override UInt8 toUInt8( UInt8 index = 0 )
    {
        ret this._value
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
        ret SystemConvertString(this)
    }
}
