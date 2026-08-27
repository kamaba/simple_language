
public class Tuple<T1>
{
    T1 _value1 = null
}

public class Tuple<T1,T2>
{
    T1 _value1 = null
    T2 _value2 = null
}

public class Tuple<T1,T2,T3>
{
    T1 _value1 = null
    T2 _value2 = null
    T3 _value3 = null
}

public class Tuple<T1,T2,T3,T4>
{
    T1 _value1 = null
    T2 _value2 = null
    T3 _value3 = null
    T4 _value4 = null
}

public class Tuple
{
    Array<object> _values = null
    _init_( val1 )
    {
    }
    _init_( val1, val2 )
    {
    }
    _init_( val1, val2, val2 )
    {
    }

    override string toString()
    {
        if( this._values == null ){
            ret "Tuple()"
        }
        else{
            ret "Tuple( " + this._values + " )"
        }
    }
}