
Int32
{
    Int32 value;
    String ToString()
    {
        return String.ParseString( value );
    }
    static String ToString( Int32 value )
    {
        return String.ParseString( value );
    }
    AssignFloat( Float t )
    {
        value = t.ToInt();
    }
}