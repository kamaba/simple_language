import CSharpLang.SimpleLanguage

public class Int16 extends Num
{
    const int MaxValue = 0x7fffffff;
    const int MinValue = 0x80000000;

    Int16 _value = 0i;
    
    static String Int16ToString( Int16 value )
    {
        ret SimpleLanguage.Lib.Int32Class.Int16ToString( value )
    }
    public static Int16 parseString( string s )
    {
        ret 0s
    }
    _init_( Int16 _val )
    {
        this._value = _val
    }
    public int compareTo(Int16 value)
    {
        if (value == null)
        {
            ret 1;
        }
        ret 0
    }
    override String toString()
    {
        ret SimpleLanguage.Lib.Int32Class.Int32ToString( this )
    }
}