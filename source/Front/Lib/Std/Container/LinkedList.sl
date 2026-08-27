
public class LinkedList<T> interface Core.IIterable<T>, Core.IIterator<T>
{
    # ── Node 内部类：双向链表节点（参照 C# LinkedListNode<T>）──
    public class Node
    {
        Object _value = null
        Node _prev = null
        Node _next = null

        void _init_( Object value )
        {
            this._value = value
        }

        public get Object value()
        {
            ret this._value
        }
        public set void value( Object val )
        {
            this._value = val
        }
    }

    # ── 字段 ──
    Node _head = null
    Node _tail = null
    int _count = 0

    # 迭代器状态
    int _index = -1
    Node _iterNode = null

    # ── 构造函数 ──
    override _init_()
    {
    }

    # ── 属性 ──
    get int length()
    {
        ret this._count
    }

    get bool isEmpty()
    {
        if this._count <= 0
        {
            ret true
        }
        ret false
    }

    get bool isNotEmpty()
    {
        if this._count > 0
        {
            ret true
        }
        ret false
    }

    # 首元素 / 末元素（空列表返回 null）
    get T first()
    {
        if this._head == null
        {
            ret null
        }
        ret this._head._value as T
    }
    get T last()
    {
        if this._tail == null
        {
            ret null
        }
        ret this._tail._value as T
    }

    # ── 添加操作 ──

    # 在末尾添加元素（与 List<T>.add 行为一致）
    public void add( T item )
    {
        this.addLast( item )
    }

    # 在末尾添加元素
    public void addLast( T item )
    {
        Node node = Node( item )
        if this._tail == null
        {
            this._head = node
            this._tail = node
        }
        else
        {
            node._prev = this._tail
            this._tail._next = node
            this._tail = node
        }
        this._count++
    }

    # 在头部添加元素
    public void addFirst( T item )
    {
        Node node = Node( item )
        if this._head == null
        {
            this._head = node
            this._tail = node
        }
        else
        {
            node._next = this._head
            this._head._prev = node
            this._head = node
        }
        this._count++
    }

    # 在指定索引处之前插入新元素
    public void addBefore( int index, T item )
    {
        if index < 0 || index >= this._count
        {
            ret
        }
        if index == 0
        {
            this.addFirst( item )
            ret
        }
        Node node = this._nodeAt( index )
        Node newNode = Node( item )
        newNode._next = node
        newNode._prev = node._prev
        if node._prev != null
        {
            node._prev._next = newNode
        }
        else
        {
            this._head = newNode
        }
        node._prev = newNode
        this._count++
    }

    # 在指定索引处之后插入新元素
    public void addAfter( int index, T item )
    {
        if index < 0 || index >= this._count
        {
            ret
        }
        if index == this._count - 1
        {
            this.addLast( item )
            ret
        }
        Node node = this._nodeAt( index )
        Node newNode = Node( item )
        newNode._prev = node
        newNode._next = node._next
        if node._next != null
        {
            node._next._prev = newNode
        }
        else
        {
            this._tail = newNode
        }
        node._next = newNode
        this._count++
    }

    # 在指定索引处插入元素
    public void insert( int index, T item )
    {
        if index < 0 || index > this._count
        {
            ret
        }
        if index == 0
        {
            this.addFirst( item )
            ret
        }
        if index == this._count
        {
            this.addLast( item )
            ret
        }
        this.addBefore( index, item )
    }

    # ── 删除操作 ──

    # 删除首个匹配元素
    public void remove( T item )
    {
        Node node = this._findNode( item )
        if node != null
        {
            this._removeNode( node )
        }
    }

    # 删除首元素
    public void removeFirst()
    {
        if this._head == null
        {
            ret
        }
        this._removeNode( this._head )
    }

    # 删除末元素
    public void removeLast()
    {
        if this._tail == null
        {
            ret
        }
        this._removeNode( this._tail )
    }

    # 删除指定索引处的元素
    public void removeAt( int index )
    {
        if index < 0 || index >= this._count
        {
            ret
        }
        Node node = this._nodeAt( index )
        this._removeNode( node )
    }

    # 内部：摘除一个节点并修复前后指针
    void _removeNode( Node node )
    {
        if node == null
        {
            ret
        }
        if node._prev != null
        {
            node._prev._next = node._next
        }
        else
        {
            this._head = node._next
        }
        if node._next != null
        {
            node._next._prev = node._prev
        }
        else
        {
            this._tail = node._prev
        }
        node._prev = null
        node._next = null
        this._count--
    }

    # 清空列表
    public void clear()
    {
        this._head = null
        this._tail = null
        this._count = 0
        this._index = -1
        this._iterNode = null
    }

    # ── 查找操作 ──

    # 内部：正向查找首个匹配节点
    Node _findNode( T item )
    {
        Node node = this._head
        while node != null
        {
            if node._value == item
            {
                ret node
            }
            node = node._next
        }
        ret null
    }

    # 查找元素首次出现的索引，未找到返回 -1
    public int indexOf( T item )
    {
        int i = 0
        Node node = this._head
        while node != null
        {
            if node._value == item
            {
                ret i
            }
            node = node._next
            i++
        }
        ret -1
    }

    # 查找元素最后一次出现的索引，未找到返回 -1
    public int lastIndexOf( T item )
    {
        int i = this._count - 1
        Node node = this._tail
        while node != null
        {
            if node._value == item
            {
                ret i
            }
            node = node._prev
            i--
        }
        ret -1
    }

    # 是否包含指定元素
    public bool contains( T item )
    {
        if this._findNode( item ) != null
        {
            ret true
        }
        ret false
    }

    # ── 索引器 / 随机访问 ──

    # 内部：按索引获取节点（从头部开始遍历）
    Node _nodeAt( int index )
    {
        if index < 0 || index >= this._count
        {
            ret null
        }
        Node node = this._head
        int i = 0
        while i < index
        {
            node = node._next
            i++
        }
        ret node
    }

    # 索引器：获取指定索引处的值
    T _getItem_( int index )
    {
        Node node = this._nodeAt( index )
        if node == null
        {
            ret null
        }
        ret node._value as T
    }

    # 索引器：设置指定索引处的值
    void _setItem_( int index, T value )
    {
        Node node = this._nodeAt( index )
        if node != null
        {
            node._value = value
        }
    }

    # ── 转换 ──

    # 转换为数组
    Array<T> toArray()
    {
        Array<T> arr = Array<T>( this._count )
        Node node = this._head
        int i = 0
        while node != null
        {
            SystemArraySetValueThis( arr, i, node._value )
            node = node._next
            i++
        }
        ret arr
    }

    # 字符串表示
    override string toString()
    {
        string showstr = "["
        Node node = this._head
        bool isFirst = true
        while node != null
        {
            if !isFirst
            {
                showstr = showstr + ","
            }
            isFirst = false
            if node._value == null
            {
                showstr = showstr + "null"
            }
            else
            {
                showstr = showstr + node._value.toString()
            }
            node = node._next
        }
        ret showstr + "]"
    }

    # ── IIterator / IIterable 接口实现 ──

    override void reset()
    {
        this._index = -1
        this._iterNode = null
    }

    override bool moveNext()
    {
        this._index++
        if this._index < this._count
        {
            if this._index == 0
            {
                this._iterNode = this._head
            }
            else
            {
                if this._iterNode != null
                {
                    this._iterNode = this._iterNode._next
                }
            }
            ret true
        }
        this._iterNode = null
        ret false
    }

    override get T current()
    {
        if this._iterNode == null
        {
            ret null
        }
        ret this._iterNode._value as T
    }

    override get Core.IIterator<T> iterator()
    {
        this.reset()
        ret this
    }
}
