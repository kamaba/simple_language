
String
{
    private String m_Value = null;
    String ToString()
    {
        return this;
    }
    static Int32 ToInt32( String value )
    {
        return Int32.Parse( value );
    }
}