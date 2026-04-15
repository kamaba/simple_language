
public class Byte extends Num
{
    const Byte MaxValue = 0b11111111;
    const Byte MinValue = 0b00000000;
    Byte _value = 0;

    _init_(Byte _val)
    {
        this._value = _val
    }

    override get int size() { ret 8 }
    override get int byteLength() { ret 1 }

    public static Byte parse(string s)
    {
        ret SystemConvertInt8(s, -1)
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
    public override Int32 compareTo(Num other)
    {
        if (other == null) { ret 1 }
        Byte ov = SystemConvertInt8(other, -1)
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : -1
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