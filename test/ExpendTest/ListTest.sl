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
        var a2 = List<int>( range( 1, 100 ) )
        var a3 = List<int>( [1,2,3,4,100,32] );
        List<List<int>> b = List<List<int>>();

        
        # alist = List(2){ intvalue, 1 }

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
        b.add( a2 )
        b.add( a3 )
        b.add( a );
        av = a.@10; #相当于 a._value_( 10 );
        a.@2 = va;  #相当于 a._value_( 20, va );
        a[2] = 100

        for it in a
        {
            a.value = 20;    #相当于 it = 20;
        }
        for it2 in b
        {
            for it3 in it2
            {
                var it3val = it3.value
                Console.WriteLine("-----------" + it3val )
            }
        }

        b.value.add( 1000 )
        b.value.value = 20
        b.index = 1
        aavalue = b.value

        b.value.@2
    }
}