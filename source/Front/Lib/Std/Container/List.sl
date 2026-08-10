
public class List<T> interface Core.IIterable<T>, Core.IIterator<T>
{
    int _length = 0
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
        SystemListInit(this, 0)
    }
    _init_( int capacity )
    {
        if capacity < 0
        {
            capacity = 0
        }
        SystemListInit(this, capacity)
    }
    get int length(){ ret this._length }

    #容量（内部存储长度）
    get int capacity()
    {
        ret SystemListGetCapacity(this)
    }
    set void capacity( int value )
    {
        if value < this._length
        {
            ret
        }
        if value != SystemListGetCapacity(this)
        {
            SystemListSetCapacity(this, value)
        }
    }

    #容量扩展：0->4，之后倍增 4->8->16...（与 C# List<T> 一致）
    void grow()
    {
        int newCapacity = 4
        int curCap = SystemListGetCapacity(this)
        if curCap > 0
        {
            newCapacity = curCap * 2
        }
        this.capacity = newCapacity
    }
    void ensureCapacity( int min )
    {
        int curCap = SystemListGetCapacity(this)
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
            this.capacity = newCapacity
        }
    }

    public void add( T item )
    {
        if this._length == SystemListGetCapacity(this)
        {
            this.grow()
        }
        SystemListSetValueThis(this, this._length, item)
        this._length++
    }
    public void insert( int index, T item )
    {
        if index < 0 || index > this._length
        {
            ret
        }
        if this._length == SystemListGetCapacity(this)
        {
            this.grow()
        }
        int i = this._length
        while i > index
        {
            SystemListSetValueThis(this, i, SystemListGetValueThis(this, i - 1))
            i = i - 1
        }
        SystemListSetValueThis(this, index, item)
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
            SystemListSetValueThis(this, i, SystemListGetValueThis(this, i + 1))
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
            SystemListSetValueThis(this, i, value)
        }
    }
    Array<T> toArray()
    {
        Array<T> arr = Array<T>(this._length)
        for i = 0, i < this._length, i++
        {
            SystemArraySetValueThis(arr, i, SystemListGetValueThis(this, i))
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
            this._current = SystemListGetValueThis(this, this._index) as T
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
        SystemListSetValueThis(this, this._index, val)
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
        this._current = SystemListGetValueThis(this, ind) as T
    }
    set setValue( int __index, T val )
    {
        SystemListSetValueThis(this, __index, val)
    }
    get T getValue( int __index )
    {
        ret SystemListGetValueThis(this, __index) as T
    }
    override string toString()
    {
        string showstr = "["
        for i = 0, i < this._length, i++
        {
            var cur = SystemListGetValueThis(this, i)
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
