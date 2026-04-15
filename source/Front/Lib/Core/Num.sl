
#Abstract numeric base class (Dart-like)
public abstract class Num extends Object
{
    
    abstract get int size();
    abstract get int byteLength();

    #absolute value
    public abstract Num abs();

    #floor / ceil
    public abstract Num floor();
    public abstract Num ceil();

    #compare to another numeric value: -1,0,1
    public abstract Byte compareTo( Num other );

    
    bool toBool()
    {
        ret SystemConvertBool(this)
    }
    Int8 toSByte( byte index = 0 )
    {
        ret SystemConvertInt8(this, index)
    }
    UInt8 toByte( byte index = 0 )
    {
        ret SystemConvertUInt8(this, index)
    }
    Int16 toInt16()
    {
        ret SystemConvertInt16(this)
    }
    UInt16 toUInt16()
    {
        ret SystemConvertUInt16(this)
    }
    Int32 toInt32()
    {
        ret SystemConvertInt32(this)
    }
    UInt32 toUInt32()
    {
        ret SystemConvertUInt32(this)
    }
    Int64 toInt64()
    {
        ret SystemConvertInt64(this)
    }
    Int64 toUInt64()
    {
        ret SystemConvertUInt64(this)
    }
    Float32 toFloat32()
    {
        ret SystemConvertFloat32(this)
    }
    Float64 toFloat64()
    {
        ret SystemConvertFloat64(this)
    }
}
