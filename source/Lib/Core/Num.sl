import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

#Abstract numeric base class (Dart-like)
public abstract class Num extends Object
{
    #convert to 32-bit integer
    public abstract Int32 toInt32();
    #public abstract Byte toByte()

    #convert to 64-bit float
    public abstract Float64 toFloat64();

    #absolute value
    public abstract Num abs();

    #floor / ceil
    public abstract Num floor();
    public abstract Num ceil();

    #compare to another numeric value: -1,0,1
    public abstract Int32 compareTo( Num other );

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
