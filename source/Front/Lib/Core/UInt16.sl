public class UInt16 extends Num
{
    const UInt16 MaxValue = 0xffff;
    const UInt16 MinValue = 0;
    UInt16 _value = 0iu;

    _init_(UInt16 _val)
    {
        this._value = _val
    }

    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }

    public static UInt16 parseString(string s)
    {
        ret System.Convert.ToUInt16(s)
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
        UInt16 ov = SystemConvertUInt16(other)
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