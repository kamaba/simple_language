
Core.Int32
{
    private Int32 _value = 0i;

    _init_(Int32 _val )
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
        if( t == Int16.type )
        {
            ret Convert.Int32ConvertToInt16( m_Value )
        }
    }
}