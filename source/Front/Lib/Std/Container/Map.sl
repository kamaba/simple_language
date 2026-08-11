
public class Map<TKey,TValue> extends Object interface Core.IIterable<T>, Core.IIterator<T>, IMap
{
    private class MapEntity<T,V>
    {
        public int hashId = 0
        public T key = null
        public V value = null
    }

    List<MapEntity<TKey,TValue> > m_MapContent = List<MapEntity<TKey,TValue> >()


    void add( TKey key, TValue value )
    {
        MapEntity<TKey,TValue> me = new()
        me.key = key;
        me.value = value
        me.hashId = key.hashCode
        m_MapContent.add(me)
    }
    get TValue getValue( TKey key )
    {
        for i = 0, i < m_MapContent.length, i++
        {
            var ent = m_MapContent.getValue(i)
            if ent != null && ent.key.equals(key)
            {
                ret ent.value
            }
        }
        ret TValue.default
    }
    public bool containByKey( TKey key )
    {
        for i = 0, i < m_MapContent.length, i++
        {
            var ent = m_MapContent.getValue(i)
            if ent != null && ent.key.equals(key)
            {
                ret true
            }
        }
        ret false
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