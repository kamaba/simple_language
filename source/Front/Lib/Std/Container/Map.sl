
public class Map<TKey,TValue> extends Object interface IMap,Core.IIterable<T>, Core.IIterator<T>
{
    private class MapEntity<T,V>
    {
        public int hashId = 0
        public T key = null
        public V value = null
    }

    private Array<MapEntity<TKey,TValue>> m_MapContent = new()
    private int _length = 0
    private int _capacity = 0
    private int _index = 0;
    private MapEntity<TKey,TValue> _current = null

    void _init_()
    {
        this._list = Array<MapEntity<TKey,TValue>>(0)
    }
    override void _init_( int capacity )
    {
        this._list = Array<MapEntity<TKey,TValue>>(capacity)
    }    
    override get int capacity()
    {
        ret this._capacity
    }
    override set void capacity( int value )
    {
        if value < this._length
        {
            ret
        }
        if value != this._list.length
        {
            this.resizeArray(value)
            this._capacity = value
        }
    }

    #容量扩展：0->4，之后倍增 4->8->16...（与 C# List<T> 一致）
    void grow()
    {
        int newCapacity = 4
        if this._capacity > 0
        {
            newCapacity = this._capacity * 2
        }
        this.resizeArray(newCapacity)
        this._capacity = newCapacity
    }
    override void ensureCapacity( int min )
    {
        int curCap = this._list.length
        if curCap < min
        {
            int newCapacity = 4
            if curCap > 0
            {
                newCapacity = curCap * 2
            }
            if newCapacity < min
            {
                newCapacity = min
            }
            this.resizeArray(newCapacity)
            this._capacity = newCapacity
        }
    }

    #内部方法：重新分配 _list Array 并拷贝已有元素
    void resizeArray( int newCapacity )
    {
        Array<T> newList = Array<T>(newCapacity)
        for i = 0, i < this._length, i++
        {
            SystemArraySetValueThis(newList, i, SystemArrayGetValueThis(this._list, i))
        }
        this._list = newList
    }

    void add( TKey key, TValue value )
    {
        MapEntity<TKey,TValue> me = new()
        me.key = key;
        me.value = value
        me.hashId = key.hashCode
        m_MapContent.add(me)
    }
    TValue _getItem_( TKey key )
    {
        for i = 0, i < m_MapContent.length, i++
        {
            var ent = m_MapContent._getItem_(i)
            if ent != null && ent.key.equals(key)
            {
                ret ent.value
            }
        }
        ret TValue.default
    }
    void _setItem_( TKey key, TValue value )
    {
        for i = 0, i < m_MapContent.length, i++
        {
            var ent = m_MapContent._getItem_(i)
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
    public override void clear()
    {
        this._length = 0
        this._capacity = 0
        this._list = Array<T>(0)
        this._index = -1
        this._current = null
    }
    public void fill( T value, int startIndex = 0, int count = -1 )
    {
        for i = 0, i < this._length, i++
        {
            SystemArraySetValueThis(this._list, i, value)
        }
    }
    override Array<T> toArray()
    {
        Array<T> arr = Array<T>(this._length)
        for i = 0, i < this._length, i++
        {
            SystemArraySetValueThis(arr, i, SystemArrayGetValueThis(this._list, i))
        }
        ret arr
    }

    #接口层
    override void reset()
    {
        this._index = -1;
        this._current = null
    }
    override bool moveNext()
    {
        this._index++;
        bool hasNext_var = this._index < this._length
        if hasNext_var
        {
            this._current = SystemArrayGetValueThis(this._list, this._index) as T
        }
        else
        {
            this._current = null
        }
        ret hasNext_var
    }
    override get T current()
    {
        ret this._current;
    }
    override set void current( T val )
    {
        SystemArraySetValueThis(this._list, this._index, val)
        this._current = val
    }
    override get Core.IIterator<T> iterator()
    {
        ret this
    }
    get int index()
    {
        ret this._index;
    }
    set void index( int ind )
    {
        if( ind < 0 )
        {
            ret
        }
        if( ind >= this._length )
        {
            ret
        }
        this._index = ind;
        this._current = SystemArrayGetValueThis(this._list, ind) as T
    }
    override string toString()
    {
        string showstr = "["
        for i = 0, i < this._length, i++
        {
            var cur = SystemArrayGetValueThis(this._list, i)
            if cur == null
            {
                showstr = showstr + "null"
            }
            else
            {
                showstr = showstr + cur.toString()
            }
            if( i < this._length - 1 )
            {
                showstr += ","
            }
        }
        ret showstr + "]"
    }
}
