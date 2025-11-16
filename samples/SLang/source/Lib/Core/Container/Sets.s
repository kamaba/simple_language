
String
{
    String value;
    String ToString()
    {
        return this;
    }
    static Int32 ToInt32( String value )
    {
        return Int32.Parse( value );
    }
}