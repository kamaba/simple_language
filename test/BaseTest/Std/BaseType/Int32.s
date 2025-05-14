
Core.Int32
{
    private Int32 m_Value = 0i;
    String ToString()
    {
        return String.ParseString( m_Value );
    }
    static String Int32ToString( Int32 value )
    {
        return String.ParseString( value );
    }
    Cast( Type t )
    {
        if( t == Int16.type )
        {
            ret Convert.Int32ConvertToInt16( m_Value )
        }
    }
}