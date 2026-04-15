public class Int8 extends Num
{    
    const Int8 MaxValue = 0b1111111;
    const Int8 MinValue = 0b0000000;
    Int8 _value = 0;
    
    override get int size() { ret 8 }
    override get int byteLength() { ret 1 }   
    
    
    _init_( Int8 _val )
    {
        this._value = _val
    }
    
    public static Int8 parse( string s )
    {
        ret SystemConvertSInt8(s, 0-1)
    }    

    public override Num abs()
    {
        ret SystemConvertSInt8(SystemNumAbs(this), 0-1)
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
        Int8 ov = SystemConvertSInt8(other, 0-1)
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
        ret SystemConvertString( this )
    }
}
