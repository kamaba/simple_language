
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
    _val1 = null
    _val2 = null
    _val3 = null
    _val4 = null
    _val5 = null
    _val6 = null

    _length = 0
    _init_( val1 )
    {
        this._val1 = val1
        this._length = 1
    }
    _init_( val1, val2 )
    {
        this._val1 = val1
        this._val2 = val2
        this._length = 2
    }
}