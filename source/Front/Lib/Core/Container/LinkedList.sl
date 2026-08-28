
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
    # 说明：SL 层只创建 Node（泛型实例化须在前端完成），
    #       遍历与 _prev/_next/_head/_tail/_count 指针手术全部在 C 系统调用中完成。

    # 在末尾添加元素（与 List<T>.add 行为一致）
    public void add( T item )
    {
        this.addLast( item )
    }

    # 在末尾添加元素
    public void addLast( T item )
    {
        Node node = Node( item )
        SystemLinkedListAddLast( this, node )
    }

    # 在头部添加元素
    public void addFirst( T item )
    {
        Node node = Node( item )
        SystemLinkedListAddFirst( this, node )
    }

    # 在指定索引处之前插入新元素（越界 no-op；index==0 走头插）
    public void addBefore( int index, T item )
    {
        Node node = Node( item )
        SystemLinkedListInsertBefore( this, index, node )
    }

    # 在指定索引处之后插入新元素（越界 no-op；index==末位走尾插）
    public void addAfter( int index, T item )
    {
        Node node = Node( item )
        SystemLinkedListInsertAfter( this, index, node )
    }

    # 在指定索引处插入元素
    public void insert( int index, T item )
    {
        if index < 0 || index > this._count
        {
            ret
        }
        if index == this._count
        {
            this.addLast( item )
            ret
        }
        Node node = Node( item )
        SystemLinkedListInsertBefore( this, index, node )
    }

    # ── 删除操作 ──

    # 删除首个匹配元素
    public void remove( T item )
    {
        SystemLinkedListRemoveValue( this, item )
    }

    # 删除首元素
    public void removeFirst()
    {
        SystemLinkedListRemoveFirst( this )
    }

    # 删除末元素
    public void removeLast()
    {
        SystemLinkedListRemoveLast( this )
    }

    # 删除指定索引处的元素（就近端遍历定位，越界 no-op）
    public void removeAt( int index )
    {
        SystemLinkedListRemoveAt( this, index )
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

    # 查找元素首次出现的索引，未找到返回 -1
    public int indexOf( T item )
    {
        ret SystemLinkedListIndexOf( this, item )
    }

    # 查找元素最后一次出现的索引，未找到返回 -1
    public int lastIndexOf( T item )
    {
        ret SystemLinkedListLastIndexOf( this, item )
    }

    # 是否包含指定元素
    public bool contains( T item )
    {
        ret SystemLinkedListIndexOf( this, item ) >= 0
    }

    # ── 索引器 / 随机访问 ──

    # 索引器：获取指定索引处的值（就近端遍历，越界返回 null）
    T _getItem_( int index )
    {
        ret SystemLinkedListGetValueAt( this, index ) as T
    }

    # 索引器：设置指定索引处的值（就近端遍历，越界 no-op）
    void _setItem_( int index, T value )
    {
        SystemLinkedListSetValueAt( this, index, value )
    }

    # ── 转换 ──

    # 转换为数组（SL 层分配，C 端遍历填充）
    Array<T> toArray()
    {
        Array<T> arr = Array<T>( this._count )
        SystemLinkedListToArray( this, arr )
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
        ret SystemLinkedListMoveNext( this )
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
