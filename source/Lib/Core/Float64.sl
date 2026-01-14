

public class Float64 extends Object
{
    public const Epsilen = 4.9123123213d;
    public const MaxValue = 20d;
    public const MinValue = -1d;

    Float64 _value = 0.0d

    public void _init_( Float64 f )
    {
        this._value = f
    }
    public static bool IsFinite( Float64 f )
    {
        ret false;
    }
    #!
    public override String toString( string format )
    {
        ret string.format( this._value );
    }
    !#
}