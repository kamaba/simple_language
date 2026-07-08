public class UInt64 extends Num
{
    const static UInt64 MaxValue = 0xffffffffffffffff;
    const static UInt64 MinValue = 0;

    UInt64 _value = 0ui;

    
    override get int size() { ret 64 }
    override get int byteLength() { ret 8 }

    public static UInt64 parse( string s )
    {
        ret SystemConvertUInt64(s)
    }
    _init_( UInt64 _val )
    {
        this._value = _val
    }

    public override Int32 toInt32()
    {
        ret this._value
    }
    public override Float64 toFloat64()
    {
        ret this._value
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
        ret this._value > ov ? 1 : 0-1
    }
    override String toString()
    {
        ret SystemConvertString(this)
    }
}