public class SByte extends Num
{    
    const SByte MaxValue = 0b1111111;
    const SByte MinValue = 0b0000000;
    SByte _value = 0; 

    
    override get int size() { ret 8 }
    override get int byteLength() { ret 1 }   
    
    public override Num abs()
    {
        ret SystemConvertSInt8(SystemNumAbs(this))
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
        SByte ov = SystemConvertSInt8(other)
        if (this._value == ov) { ret 0 }
        ret this._value > ov ? 1 : -1
    }

    static String SByteToString( SByte value )
    {
        #ret SimpleLanguage.Lib.SByteClass.SByteToString( value )
    }
    public static SByte parseString( string s )
    {
        ret System.Convert.ToSByte(s)
    }
    _init_( SByte _val )
    {
        this._value = _val
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