import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

public class Float32 extends Num
{
    public const Float32 Epsilen = 4.9123213f;
    public const Float32 MaxValue = 20f;
    public const Float32 MinValue = -1f;

    Float32 _value = 0.0f

    public void _init_( Float32 f )
    {
        this._value = f
    }
    public static bool isFinite( Float32 f )
    {
        ret false;
    }
    #!
    public override String toString( string format )
    {
        return string.format( format, this._value );
    }
    !#
}