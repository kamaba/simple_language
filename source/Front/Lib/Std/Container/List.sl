
public class List<T> interface Core.IIterable<T>, Core.IIterator<T>, IList<T>
{
    int _length = 0
    int _capacity = 0;
    int _index = 0;
    T _current = null
    Array<T> _list = null

    public static List<T> create( int capacity )
    {
        var list = List<T>(capacity)
        ret list
    }

    #默认构造，容量为0，首次添加时扩容为4（与 C# List<T> 一致）
    _init_()
    {
        this._list = Array<T>(4)
    }
    override void _init_( int capacity )
    {
        if capacity < 0
        {
            capacity = 0
        }
        this._list = Array<T>(capacity)
        this._capacity = capacity
    }
    get int length(){ ret this._length }

    #容量（内部存储长度）
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
        this._list = SystemArrayResize(this._list, newCapacity)
    }

    public override void add( T item )
    {
        if this._length <= this._capacity
        {
            this.grow()
        }
        SystemArraySetValueThis(this._list, this._length, item)
        this._length++
    }
    public void insert( int index, T item )
    {
        if index < 0 || index > this._length
        {
            ret
        }
        if this._length == this._capacity
        {
            this.grow()
        }
        SystemArrayInsertValue(this._list, index, this._length, item)
        this._length++
    }
    public override void remove( T item )
    {
        if SystemArrayRemoveValue(this._list, item, this._length) >= 0
        {
            this._length--
        }
    }
    public override void removeAt( int index )
    {
        if index < 0 || index >= this._length
        {
            ret
        }
        SystemArrayRemoveAtValue(this._list, index, this._length)
        this._length--
    }
    public override void clear()
    {
        this._length = 0
        this._capacity = 0
        this._list = Array<T>(0)
        this._index = -1
        this._current = null
    }
    public void fill( T value )
    {
        SystemArrayFillValue(this._list, 0, this._length, value)
    }
    override Array<T> toArray()
    {
        ret SystemArrayCopy(this._list, this._length)
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
    void _setItem_( int _index, T _value )
    {
        SystemArraySetValueThis(this._list, _index, _value)
    }
    T _getItem_( int _index )
    {
        ret SystemArrayGetValueThis(this._list, _index) as T
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
