public class Core.Int64 extends Num
{
    public const int MaxValue = 0x7fffffff;
    public const int MinValue = unchecked((int)0x80000000);

    private Int32 _value = 0i;

    _init_( Int32 _val )
    {
        this._value = _val        
    }
    String toString()
    {
        return String.ParseString( m_Value );
    }
    static String Int32ToString( Int32 value )
    {
        return String.ParseString( value );
    }
    cast( Type t )
    {
        if t == Int16.type 
        {
            ret Convert.Int32ConvertToInt16( m_Value )
        }
    }

    public int compareTo(object value)
    {
        if (value == null)
        {
            ret 1;
        }

        ret 0
    }

    public static int parse( string s )
    {
        ret 0
    }
}