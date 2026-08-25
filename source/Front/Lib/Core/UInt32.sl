public class UInt32 extends Num
{
    const static uint MaxValue = 0xffffffff;
    const static uint MinValue = 0;

    UInt32 _value = 0ui;

    override get int size() { ret 32 }
    override get int byteLength() { ret 4 }

    _init_( UInt32 _val )
    {
        this._value = _val
    }
    public override Int8 compareTo( Num other )
    {
        if (other == null) { ret 1 }
        UInt32 ov = SystemConvertUInt32(other)
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : 0-1
    }

    override String toString()
    {
        ret SystemConvertString( this )
    }

    public override Int32 toInt32()
    {
        ret this._value
    }
    public override Float64 toFloat64()
    {
        ret  this._value
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
}