import CSharp.SimpleLanguage.Core
import CSharp.SimpleLanguage

public class UInt64 extends Num
{
    const UInt64 MaxValue = 0x7fffffff;
    const UInt64 MinValue = 0;

    UInt32 _value = 0iu;
    
    static String UInt64ToString( UInt64 value )
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( value )
    }
    public static UInt64 parseString( string s )
    {
        ret 0
    }
    _init_( UInt64 _val )
    {
        this._value = _val
    }
    override String toString()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( this )
    }
}