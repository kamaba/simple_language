public class Int64 extends Num
{
    public const int MaxValue = 0x7fffffff;
    public const int MinValue = unchecked((int)0x80000000);

    private Int32 _value = 0i;

    _init_( Int32 _val )
    {
        this._value = _val        
    }
    
    override get int size() { ret 32 }
    override get int byteLength() { ret 4 }

    String toString()
    {
        return String.ParseString( m_Value );
    }
    static String Int32ToString( Int32 value )
    {
        return String.ParseString( value );
    }
    public int compareTo(Int64 value)
    {
        if (value == null)
        {
            ret 1;
        }

        ret 0
    }
    public static Int64 parse( string s )
    {
        ret 0
    }
}