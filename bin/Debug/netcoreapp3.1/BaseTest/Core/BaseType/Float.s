
Float
{
    Float value;
    String ToString()
    {
        return this;
    }
    Cast<T>()
    {
        if T == Int32 
        {
            return Int32.ParseFloat( value );
        }
        elif T == Long
        {
            return Int64.ParseFloat( value );
        }
        elif T == String
        {
            return String.ParseFloat( value );
        }
        return 0.0f;
    }
    static Int32 ToInt32( Float _value )
    {
        return Int32.Parse( _value );
    }
}