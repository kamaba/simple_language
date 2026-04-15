
public class UInt8 extends Num
{
    const UInt8 MaxValue = 0b11111111;
    const UInt8 MinValue = 0b00000000;
    UInt8 _value = 0;

    _init_(UInt8 _val)
    {
        this._value = _val
    }

    override get int size() { ret 8 }
    override get int byteLength() { ret 1 }

    public static UInt8 parse(string s)
    {
        ret SystemConvertUInt8(s, 0-1)
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
    public override Num compareTo(Num other)
    {
        if (other == null) { ret 1 }
        UInt8 ov = SystemConvertInt8(other, 0-1)
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : 0-1
    }
    override Int32 toInt32()
    {
        ret SystemConvertInt32(this)
    }
    override Float64 toFloat64()
    {
        ret SystemConvertFloat64(this)
    }
    override String toString()
    {
        ret SystemConvertString(this)
    }
}
