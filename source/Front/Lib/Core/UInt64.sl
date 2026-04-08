public class UInt64 extends Num
{
    const UInt64 MaxValue = 0xffffffffffffffff;
    const UInt64 MinValue = 0;

    UInt64 _value = 0iu;

    
    override get int size() { ret 64 }
    override get int byteLength() { ret 8 }

    public static UInt64 parseString( string s )
    {
        ret System.Convert.ToUInt64(s)
    }
    _init_( UInt64 _val )
    {
        this._value = _val
    }
    override String toString()
    {
        ret SystemConvertString(this)
    }

    public override Int32 toInt32()
    {
        ret (Int32)this._value
    }
    public override Float64 toFloat64()
    {
        ret (Float64)this._value
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
        UInt64 ov = SystemConvertUInt64(other)
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : -1
    }
}