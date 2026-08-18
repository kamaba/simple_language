
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
    override _init_()
    {
        this._list = Array<T>(4)
    }
    #从数组构造：SystemArrayCopy 系统级拷贝全部元素（生成新数组，不与源数组共享存储）
    void _init_( Array<T> list )
    {
        if list == null
        {
            this._list = Array<T>(0)
            ret
        }
        int count = list.length
        if count <= 0
        {
            this._list = Array<T>(0)
            ret
        }
        this._list = SystemArrayCopy(list, count)
        this._length = count
        this._capacity = count
    }
    #从数组区间构造：拷贝 [startIndex, startIndex+length) 的元素，越界自动截断
    void _init_( Array<T> list, int startIndex, int length )
    {
        if list == null
        {
            this._list = Array<T>(0)
            ret
        }
        if startIndex < 0
        {
            startIndex = 0
        }
        if length < 0
        {
            length = 0
        }
        int count = list.length
        if startIndex + length > count
        {
            length = count - startIndex
        }
        if length <= 0
        {
            this._list = Array<T>(0)
            ret
        }
        this._list = Array<T>(length)
        for i = 0, i < length, i++
        {
            SystemArraySetValueThis(this._list, i, SystemArrayGetValueThis(list, startIndex + i))
        }
        this._length = length
        this._capacity = length
    }
    #从 Range 构造：Range.toArray() 系统级物化区间为数组，再 SystemArrayCopy 拷贝持有
    #（不直接持有 Range 内部缓存数组，避免后续 add/insert 污染 Range）
    void _init_( Range<T> range )
    {
        if range == null
        {
            this._list = Array<T>(0)
            ret
        }
        Array<T> arr = range.toArray()
        int count = 0
        if arr != null
        {
            count = arr.length
        }
        if count <= 0
        {
            this._list = Array<T>(0)
            ret
        }
        this._list = SystemArrayCopy(arr, count)
        this._length = count
        this._capacity = count
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

    #是否为空 / 是否非空
    get bool isEmpty()
    {
        if this._length <= 0
        {
            ret true
        }
        ret false
    }
    get bool isNotEmpty()
    {
        if this._length > 0
        {
            ret true
        }
        ret false
    }

    #首元素 / 末元素（空列表返回 null）
    get T first()
    {
        if this._length <= 0
        {
            ret null
        }
        ret SystemArrayGetValueThis(this._list, 0) as T
    }
    get T last()
    {
        if this._length <= 0
        {
            ret null
        }
        ret SystemArrayGetValueThis(this._list, this._length - 1) as T
    }

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
        #仅当已满（_length >= _capacity）时才扩容；
        #此前写成 <= 导致每次 add 都 grow：List(5) 三次 add 容量翻到 40。
        if this._length >= this._capacity
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

    #查找元素首次出现的位置，未找到返回 -1（== 为值比较：数值/字符串/布尔按值，类按 equals 语义）
    public int indexOf( T item )
    {
        for i = 0, i < this._length, i++
        {
            if SystemArrayGetValueThis(this._list, i) == item
            {
                ret i
            }
        }
        ret -1
    }
    #查找元素最后一次出现的位置，未找到返回 -1
    public int lastIndexOf( T item )
    {
        for i = this._length - 1, i >= 0, i--
        {
            if SystemArrayGetValueThis(this._list, i) == item
            {
                ret i
            }
        }
        ret -1
    }
    #是否包含指定元素
    public bool contains( T item )
    {
        if this.indexOf(item) >= 0
        {
            ret true
        }
        ret false
    }
    #count 默认值取 0（类型零值）：跨模块调用时省略参数由 VM 做零值填充（导入函数无默认表达式），
    #因此默认值必须与零值一致，否则 ListTest 等外部模块省参调用会得到 0 而非 -1。
    public void fill( T value, int startIndex = 0, int count = 0 )
    {
        if startIndex < 0 || startIndex >= this._capacity
        {
            ret
        }
        #count==0：默认从 startIndex 填到 _length 末尾；
        #count>0：精确填 count 个（超过 capacity 剩余槽位则截断），
        #此前 elif 把 count 覆盖成 capacity-startIndex，导致 fill(33,2,3) 填到 capacity 末尾。
        if( count == 0 )
        {
            count = this._length - startIndex
        }
        elif count > 0
        {
            if count > this._capacity - startIndex
            {
                count = this._capacity - startIndex
            }
        }
        else
        {
            SystemPrint("List.fill: index out of range")
            ret
        }
        SystemArrayFillValue(this._list, startIndex, count, value)
        #填充区间可以超出当前 _length（最多到 _capacity）：以 end 扩展 _length，
        #使 fill(33,2,3) 这类越界填充后 length 自动增长。
        int end = startIndex + count
        if end > this._length
        {
            this._length = end
        }
    }

    #批量追加另一个列表的全部元素（other 为 null 时忽略）
    public void addRange( List<T> other )
    {
        if other == null
        {
            ret
        }
        for i = 0, i < other.length, i++
        {
            this.add(other._getItem_(i))
        }
    }
    #在 index 处批量插入另一个列表的全部元素
    public void insertRange( int index, List<T> other )
    {
        if other == null
        {
            ret
        }
        if index < 0 || index > this._length
        {
            ret
        }
        for i = 0, i < other.length, i++
        {
            this.insert(index + i, other._getItem_(i))
        }
    }
    #移除 [index, index+count) 区间的元素，越界部分自动截断
    public void removeRange( int index, int count )
    {
        if index < 0 || index >= this._length
        {
            ret
        }
        if count <= 0
        {
            ret
        }
        if index + count > this._length
        {
            count = this._length - index
        }
        int newLength = this._length - count
        for i = index, i < newLength, i++
        {
            SystemArraySetValueThis(this._list, i, SystemArrayGetValueThis(this._list, i + count))
        }
        this._length = newLength
    }
    #原地反转元素顺序
    public void reverse()
    {
        int left = 0
        int right = this._length - 1
        while left < right
        {
            var tmp = SystemArrayGetValueThis(this._list, left)
            SystemArraySetValueThis(this._list, left, SystemArrayGetValueThis(this._list, right))
            SystemArraySetValueThis(this._list, right, tmp)
            left++
            right--
        }
    }
    #拷贝 [index, index+count) 区间为新列表（越界自动截断；index 非法返回 null）
    public List<T> getRange( int index, int count )
    {
        if index < 0 || index >= this._length
        {
            ret null
        }
        if count <= 0
        {
            ret null
        }
        if index + count > this._length
        {
            count = this._length - index
        }
        List<T> result = new()
        for i = 0, i < count, i++
        {
            result.add(SystemArrayGetValueThis(this._list, index + i) as T)
        }
        ret result
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
