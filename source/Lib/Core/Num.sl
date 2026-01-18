import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

#Abstract numeric base class (Dart-like)
public abstract class Num extends Object
{
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

    #common helpers (non-abstract) can be added if needed
}
