ICollection<T>
{
    interface get int Count()
    {
        return 0;
    }
    interface void Add( T ){}
    interface void Clear(){}
    interface void Contains( T ){ }
    interface void Remove( T ){ }
}
IEnumerable<T>
{
    interface T NextIterator(){ ret T; }
}

List<T> extends Object interface ICollection<T>, IIterator<T>
{
    Array _value = null;
    List()
    {
        this._Value = new( T.type, 4 )
    }
    get T _value_(int index)
    {
        return m_Value[index];
    }
    set _value_( int index, T value )
    {
        this._Value[index] = value;
    }
    get T value()
    {
        ret this._value.value();
    }
    set void value( T t )
    {
        
    }
    int get count()
    {
        return m_Value.Count;
    }
    set capity( int count )
    {
        m_Value.SetCount( count );
    }
}

ListTest
{
    static Fun()
    {
        List<int> a = List<int>();
        List<List<int>> b = List<List<int>>();

        for i = 0, i < a.count
        {
            i++;
        }
        for it in a
        {
            indexa = it.index + 1;
            val v1 = it.value           #读取当前遍历的value
        }
        a.add( 10 );
        a.remove( 20 );
        b.add( List<int>() );
        av = a.@10; #相当于 a._value_( 10 );
        a.@20 = va;  #相当于 a._value_( 20, va );

        for it in a
        {
            a.value = 20;    #相当于 it = 20;
        }
    }
}