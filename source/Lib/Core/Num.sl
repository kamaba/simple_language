import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

#Abstract numeric base class (Dart-like)
public abstract class Num extends Object
{
    
    abstract get int size();
    abstract get int byteLength();

    #convert to 32-bit integer
    public abstract Int32 toInt32();

    #convert to 64-bit float
    public abstract Float64 toFloat64();

    #absolute value
    public abstract Num abs();

    #floor / ceil
    public abstract Num floor();
    public abstract Num ceil();

    #compare to another numeric value: -1,0,1
    public abstract Int32 compareTo( Num other );

    
    bool toBool()
    {
        ret SimpleLanguage.Lib.NumClass.NumToBool(this )
    }
    Byte toByte( byte index = 0 )
    {
        ret SimpleLanguage.Lib.NumClass.NumToByte(this, index )
    }
    Byte toSByte( byte index = 0 )
    {
        ret SimpleLanguage.Lib.NumClass.NumToSByte(this, index )
    }
    Int16 toInt16()
    {
        ret SimpleLanguage.Lib.NumClass.NumToInt16(this )
    }
    UInt16 toUInt16()
    {
        ret SimpleLanguage.Lib.NumClass.NumToUInt16(this )
    }
    Int32 toInt32()
    {
        ret SimpleLanguage.Lib.NumClass.NumToInt32(this )
    }
    UInt32 toUInt32()
    {
        ret SimpleLanguage.Lib.NumClass.NumToUInt32(this )
    }
    Int64 toInt64()
    {
        ret SimpleLanguage.Lib.NumClass.NumToInt64(this )
    }
    Int64 toUInt64()
    {
        ret SimpleLanguage.Lib.NumClass.NumToUInt64(this )
    }
    Float32 toFloat32()
    {
        ret SimpleLanguage.Lib.NumClass.NumToFloat32(this)
    }
    Float64 toFloat64()
    {
        ret SimpleLanguage.Lib.NumClass.NumToFloat64(this)
    }    

    #common helpers (non-abstract) can be added if needed
    # default operator-style methods following Python-like naming (_add_, _sub_, ...)
    # these are used by the runtime when looking up operator implementations on class objects
    public Num _add_( Num other )
    {
        ret this.toFloat64() + other.toFloat64();
    }
    public Num _sub_( Num other )
    {
        ret this.toFloat64() - other.toFloat64();
    }
    public Num _mul_( Num other )
    {
        ret this.toFloat64() * other.toFloat64();
    }
    public Num _div_( Num other )
    {
        ret this.toFloat64() / other.toFloat64();
    }
    public Num _neg_()
    {
        ret -this.toFloat64();
    }
}
