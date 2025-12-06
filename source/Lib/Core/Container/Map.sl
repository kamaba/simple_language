
public class Core.Map<TKey,TValue> extends Object interface Core.IIterable<T>, Core.IIterator<T>
{
    private class MapEntity<T,V>
    {
        public int hashId = 0
        public T key = null
        public V value = null
    }

    List<MapEntity<TKey,TVvalue> > m_MapContent = List<<TKey,TVvalue> >()


    void add( TKey key, TValue value )
    {
        MapEntity<TKey,TValue> me = new()
        me.key = key;
        me.value = value

        
    }
    get TValue getValue( TKey key )
    {
        return TValue.default;
    }
    public bool containByKey( TKey key )
    {
        return false;
    }    
}

MapTest
{
    static fun()
    {
        Map map = Map<stringt, string>(20);
        map.add( "xx", "20" );

        xx = "xx"

        Map map2 = Map<Class1, int>();
        Class1 c1 = Class1(20);
        map2.add(c1, 20);

        var mapv = map.$xx;  #这样是读取上边的变量  $变量  $"xx"  $c1 $0 
    }
}