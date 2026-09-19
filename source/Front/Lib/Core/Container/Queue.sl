
#先进先出（FIFO）队列：仿 C# Queue<T>，API 参考 Python collections.deque / Dart Queue。
#核心数据结构（环形缓冲）：
#   _items    -- 底层数组（容量 _capacity，逻辑首元素在 _head，下一个入队槽位在 _tail）
#   _head     -- 队首下标（下一个出队位置）
#   _tail     -- 队尾下标（下一个入队位置，环绕递增）
#   _count    -- 有效元素个数
#   _capacity -- 当前容量（恒等于 _items.length）
#设计（与 Stack/LinkedList/Set 一致的分层）：
#   - SL 层：泛型数组实例化（Array<T> 须由前端实例化）、容量决策与新数组分配；
#   - C 层：入队/出队置值、环绕游标推进、扩容拷贝、查找比较、遍历物化、迭代推进等
#     全部耗时操作通过 SystemQueue* 系统调用映射到 CVM 原生实现
#     （queue_system_method.c）。
#空队 dequeue/peek 返回 null（与本库其他容器越界返回 null 的语义一致）。
public class Queue<T> extends Object interface Core.IIterable<T>, Core.IIterator<T>
{
    # --- 核心字段 ---
    Array<T> _items = null                         # 底层数组（环形使用）
    int _head = 0                                  # 队首下标（下一个出队位置）
    int _tail = 0                                  # 队尾下标（下一个入队位置）
    int _count = 0                                 # 有效元素个数
    int _capacity = 0                              # 当前容量（== _items.length）

    # --- 迭代器字段（FIFO 迭代：从队首向队尾）---
    int _index = -1
    T _current = null

    #默认构造，容量为0，首次 enqueue 时扩容为4（与 C# Queue<T> 一致）
    override _init_()
    {
    }
    #指定初始容量构造（负数按 0 处理）
    void _init_( int capacity )
    {
        if capacity < 0
        {
            capacity = 0
        }
        if capacity > 0
        {
            this._items = Array<T>( capacity )
            this._capacity = capacity
        }
    }
    #从数组构造（元素按下标顺序入队，数组首元素成为队首）：遍历在 VM 层完成（SystemQueueEnqueueRange）
    void _init_( Array<T> items )
    {
        if items == null || items.length <= 0
        {
            ret
        }
        this.ensureCapacity( items.length )
        SystemQueueEnqueueRange( this, items )
    }

    #有效元素个数（Python len(queue) / C# Count / Dart length）
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

    #容量（内部数组长度）
    get int capacity()
    {
        ret this._capacity
    }
    set void capacity( int value )
    {
        if value < this._count
        {
            ret
        }
        if this._items == null
        {
            if value > 0
            {
                this.resizeArray( value )
            }
            ret
        }
        if value != this._items.length
        {
            this.resizeArray( value )
        }
    }
    #预分配容量，避免多次扩容拷贝（C# Queue.EnsureCapacity）
    void ensureCapacity( int min )
    {
        if this._capacity < min
        {
            int newCapacity = 4
            if this._capacity > 0
            {
                newCapacity = this._capacity * 2
            }
            if newCapacity < min
            {
                newCapacity = min
            }
            this.resizeArray( newCapacity )
        }
    }
    #容量扩展：0->4，之后倍增 4->8->16...（与 C# 容器一致）
    void grow()
    {
        int newCapacity = 4
        if this._capacity > 0
        {
            newCapacity = this._capacity * 2
        }
        this.resizeArray( newCapacity )
    }

    #内部方法：换用新数组（新数组须由 SL 层分配，元素按队首->队尾顺序拷贝并归位 _head/_tail，在 VM 层完成）
    void resizeArray( int newCapacity )
    {
        if newCapacity < this._count
        {
            ret
        }
        if this._items == null
        {
            this._items = Array<T>( newCapacity )
            this._capacity = newCapacity
            ret
        }
        if newCapacity == this._items.length
        {
            ret
        }
        Array<T> newArr = Array<T>( newCapacity )
        SystemQueueGrow( this, newArr )
        this._capacity = newCapacity
    }

    # ── 队列操作 ──

    #入队（C# Enqueue / Python append / Dart add）：容量已满时先扩容，置值与 _tail/_count 推进在 VM 层完成
    public void enqueue( T item )
    {
        if this._items == null || this._count >= this._capacity
        {
            this.grow()
        }
        SystemQueueEnqueue( this, item )
    }
    #批量入队（Python extend）：遍历与批量置值在 VM 层完成
    public void enqueueRange( Array<T> items )
    {
        if items == null || items.length <= 0
        {
            ret
        }
        this.ensureCapacity( this._count + items.length )
        SystemQueueEnqueueRange( this, items )
    }
    #出队（C# Dequeue / Python popleft）：移除并返回队首；空队返回 null
    public T dequeue()
    {
        ret SystemQueueDequeue( this ) as T
    }
    #查看队首但不移除（C# Peek / Python queue[0]）；空队返回 null
    get T peek()
    {
        ret SystemQueuePeek( this ) as T
    }
    #队尾元素（最后入队元素）：读取在 VM 层完成
    get T rear()
    {
        ret SystemQueueRear( this ) as T
    }

    # ── 查找（VM 层完成）──

    #是否包含指定元素（Python in / C# Contains / Dart contains）
    public bool contains( T item )
    {
        ret SystemQueueContains( this, item )
    }
    #查找元素首次出现的位置（按队首->队尾方向，返回逻辑下标），未找到返回 -1
    public int indexOf( T item )
    {
        ret SystemQueueIndexOf( this, item )
    }
    #查找元素最后一次出现的位置（按队首->队尾方向查找最后匹配，返回逻辑下标），未找到返回 -1
    public int lastIndexOf( T item )
    {
        ret SystemQueueLastIndexOf( this, item )
    }

    # ── 删除 ──

    #清空队列（Python clear / C# Clear）：槽位置空、_count/_capacity/_items/_head/_tail 与迭代器复位全部在 VM 层完成
    public void clear()
    {
        SystemQueueClear( this )
    }

    # ── 转换 ──

    #拷贝出元素数组（队首->队尾顺序，长度精确为当前元素数）：遍历在 VM 层完成
    Array<T> toArray()
    {
        Array<T> arr = Array<T>( this._count )
        if this._count <= 0
        {
            ret arr
        }
        SystemQueueToArray( this, arr )
        ret arr
    }
    #转换为列表
    List<T> toList()
    {
        ret List<T>( this.toArray() )
    }
    #浅拷贝（Python queue.copy() / Dart Queue.of）：元素按队首->队尾顺序重新入队，队列序不变
    Queue<T> copy()
    {
        ret Queue<T>( this.toArray() )
    }

    # ── 迭代器（foreach 热路径，SystemQueueMoveNext 在 VM 层按 FIFO 顺序推进）──

    override void reset()
    {
        this._index = -1
        this._current = null
    }
    override bool moveNext()
    {
        ret SystemQueueMoveNext( this )
    }
    override get T current()
    {
        ret this._current
    }
    override get Core.IIterator<T> iterator()
    {
        this.reset()
        ret this
    }
    get int index()
    {
        ret this._index
    }

    #输出 [a,b,c] 格式（队首->队尾）：结构遍历在 VM 层完成（toArray）
    override string toString()
    {
        Array<T> arr = this.toArray()
        string showstr = "["
        for i = 0, i < arr.length, i++
        {
            var v = SystemArrayGetValueThis( arr, i )
            if v == null
            {
                showstr += "null"
            }
            else
            {
                showstr += v.toString()
            }
            if i < arr.length - 1
            {
                showstr += ","
            }
        }
        ret showstr + "]"
    }
}
