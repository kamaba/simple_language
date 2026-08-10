
public class List<T> interface Core.IIterable<T>, Core.IIterator<T>
{
    int _length = 0
    Array<T> _items = null
    Type _type = null;
    int _index = 0;
    T _current = null

    public static List<T> create( int capacity )
    {
        var list = List<T>(capacity)
        ret list
    }

    #默认构造，容量为0，首次添加时扩容为4（与 C# List<T> 一致）
    _init_()
    {
        this._items = Array<T>(0)
    }
    _init_( int capacity )
    {
        if capacity < 0
        {
            capacity = 0
        }
        this._items = Array<T>(capacity)
    }
    get int length(){ ret this._length }

    #容量（内部数组长度）
    get int capacity()
    {
        ret this._items.length
    }
    set void capacity( int value )
    {
        if value < this._length
        {
            ret
        }
        if value != this._items.length
        {
            Array<T> newItems = Array<T>(value)
            for i = 0, i < this._length, i++
            {
                newItems.setValue(i, this._items.getValue(i))
            }
            this._items = newItems
        }
    }

    #容量扩展：0->4，之后倍增 4->8->16...（与 C# List<T> 一致）
    void grow()
    {
        int newCapacity = 4
        if this._items.length > 0
        {
            newCapacity = this._items.length * 2
        }
        this.capacity = newCapacity
    }
    void ensureCapacity( int min )
    {
        if this._items.length < min
        {
            int newCapacity = 4
            if this._items.length > 0
            {
                newCapacity = this._items.length * 2
            }
            if newCapacity < min
            {
                newCapacity = min
            }
            this.capacity = newCapacity
        }
    }

    public void add( T item )
    {
        if this._length == this._items.length
        {
            this.grow()
        }
        this._items.setValue(this._length, item)
        this._length++
    }
    public void insert( int index, T item )
    {
        if index < 0 || index > this._length
        {
            ret
        }
        if this._length == this._items.length
        {
            this.grow()
        }
        int i = this._length
        while i > index
        {
            this._items.setValue(i, this._items.getValue(i - 1))
            i = i - 1
        }
        this._items.setValue(index, item)
        this._length++
    }
    public void removeAt( int index )
    {
        if index < 0 || index >= this._length
        {
            ret
        }
        for i = index, i < this._length - 1, i++
        {
            this._items.setValue(i, this._items.getValue(i + 1))
        }
        this._length = this._length - 1
    }
    public void clear()
    {
        this._length = 0
        this.reset()
    }
    public void fill( T value )
    {
        for i = 0, i < this._length, i++
        {
            this._items.setValue(i, value)
        }
    }
    Array<T> toArray()
    {
        Array<T> arr = Array<T>(this._length)
        for i = 0, i < this._length, i++
        {
            arr.setValue(i, this._items.getValue(i))
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
            this._current = this._items.getValue(this._index)
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
        this._items.setValue(this._index, val)
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
            #throw error("");
            ret
        }
        if( ind >= this._length )
        {
            #throw error("超出了范围")
            ret
        }
        this._index = ind;
        this._current = this._items.getValue(ind)
    }
    set setValue( int __index, T val )
    {
        this._items.setValue(__index, val)
    }
    get T getValue( int __index )
    {
        ret this._items.getValue(__index)
    }
    override string toString()
    {
        string showstr = "["
        for i = 0, i < this._length, i++
        {
            var cur = this._items.getValue(i)
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
