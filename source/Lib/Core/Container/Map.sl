
public class Map<TKey,TValue> extends Object
{
    private class MapEntity
    {
        public int hashId = 0
        public TKey key = TKey.default
        public TValue value = TValue.default
    }

    List<MapEntity> m_MapContent = List<MapEntity>()


    void add( TKey key, TValue value )
    {

    }
    TValue get( TKey key )
    {
        return TValue.default;
    }
    public bool Include( TKey key )
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