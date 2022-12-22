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

List<T> :: Object interface ICollection<T>, IIterator<T>
{
    T[] m_Value;
    List()
    {

    }
    T Get(int index)
    {
        return m_Value[index];
    }
    void Set( int index, T value )
    {
        m_Value[index] = value;
    }
    int get Count()
    {
        return m_Value.Count;
    }
    set Count( int count )
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

        for i = 0, i < a.Count
        {
            i++;
        }
        for it in a
        {
            indexa = it.index + 1;
        }
        a.Add( 10 );
        a.Remove( 20 );
        b.Add( List<int>() );
        av = a.@10; #相当于 a.Get( 10 );
        a.@20 = va;  #相当于 a.Set( va );

        for it in a
        {
            a.value = 20;    #相当于 it = 20;
        }
    }
}