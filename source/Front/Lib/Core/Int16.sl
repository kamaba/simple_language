public class Int16 extends Num
{
    const Int16 MaxValue = 0x7fff;
    const Int16 MinValue = 0x8000;

    Int16 _value = 0;
    

    public static Int16 parse( string s )
    {
        ret SystemConvertInt16(s)
    }


    _init_( Int16 _val )
    {
        this._value = _val
    }
    override get int size() { ret 16 }
    override get int byteLength() { ret 2 }

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
    public override Int32 compareTo(Num other)
    {
        if (other == null) { ret 1 }
        Int16 ov = SystemConvertInt16(other)
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
        ret SystemConvertString( this )
    }
}