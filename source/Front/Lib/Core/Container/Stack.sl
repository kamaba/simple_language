
#后进先出（LIFO）栈：仿 C# Stack<T>，API 参考 Python list.append/pop / Dart List。
#核心数据结构：
#   _items    -- 底层数组（栈底在下标 0，栈顶在下标 _count-1）
#   _count    -- 有效元素个数
#   _capacity -- 当前容量（恒等于 _items.length）
#设计（与 LinkedList/Set 一致的分层）：
#   - SL 层：泛型数组实例化（Array<T> 须由前端实例化）、容量决策与新数组分配；
#   - C 层：入栈/出栈置值、扩容拷贝、查找比较、遍历物化、迭代推进等
#     全部耗时操作通过 SystemStack* 系统调用映射到 CVM 原生实现
#     （stack_system_method.c）。
#空栈 pop/peek 返回 null（与本库其他容器越界返回 null 的语义一致）。
public class Stack<T> extends Object interface Core.IIterable<T>, Core.IIterator<T>
{
    # --- 核心字段 ---
    Array<T> _items = null                         # 底层数组（栈底在 0，栈顶在 _count-1）
    int _count = 0                                 # 有效元素个数
    int _capacity = 0                              # 当前容量（== _items.length）

    # --- 迭代器字段（LIFO 迭代：从栈顶向栈底）---
    int _index = -1
    T _current = null

    #默认构造，容量为0，首次 push 时扩容为4（与 C# Stack<T> 一致）
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
    #从数组构造（元素按下标顺序入栈，数组首元素成为栈底）：遍历在 VM 层完成（SystemStackPushRange）
    void _init_( Array<T> items )
    {
        if items == null || items.length <= 0
        {
            ret
        }
        this.ensureCapacity( items.length )
        SystemStackPushRange( this, items )
    }

    #有效元素个数（Python len(stack) / C# Count / Dart length）
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
    #预分配容量，避免多次扩容拷贝（C# Stack.EnsureCapacity）
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

    #内部方法：换用新数组（新数组须由 SL 层分配，元素拷贝在 VM 层完成）
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
        SystemStackGrow( this, newArr )
        this._capacity = newCapacity
    }

    # ── 栈操作 ──

    #入栈（C# Push / Python append）：容量已满时先扩容，置值与 _count++ 在 VM 层完成
    public void push( T item )
    {
        if this._items == null || this._count >= this._capacity
        {
            this.grow()
        }
        SystemStackPush( this, item )
    }
    #批量入栈（Python extend）：遍历与批量置值在 VM 层完成
    public void pushRange( Array<T> items )
    {
        if items == null || items.length <= 0
        {
            ret
        }
        this.ensureCapacity( this._count + items.length )
        SystemStackPushRange( this, items )
    }
    #出栈（C# Pop / Python pop）：弹出并移除栈顶；空栈返回 null
    public T pop()
    {
        ret SystemStackPop( this ) as T
    }
    #查看栈顶但不移除（C# Peek / Python stack[-1]）；空栈返回 null
    get T peek()
    {
        ret SystemStackPeek( this ) as T
    }
    #栈底元素（首个入栈元素）：读取在 VM 层完成
    get T bottom()
    {
        ret SystemStackBottom( this ) as T
    }

    # ── 查找（VM 层完成）──

    #是否包含指定元素（Python in / C# Contains / Dart contains）
    public bool contains( T item )
    {
        ret SystemStackContains( this, item )
    }
    #查找元素首次出现的位置（按栈底->栈顶方向，即底层数组下标），未找到返回 -1
    public int indexOf( T item )
    {
        ret SystemStackIndexOf( this, item )
    }
    #查找元素最后一次出现的位置（按栈顶->栈底方向查找，返回底层数组下标），未找到返回 -1
    public int lastIndexOf( T item )
    {
        ret SystemStackLastIndexOf( this, item )
    }

    # ── 删除 ──

    #清空栈（Python clear / C# Clear）：槽位置空、_count/_capacity/_items 与迭代器复位全部在 VM 层完成
    public void clear()
    {
        SystemStackClear( this )
    }

    # ── 转换 ──

    #拷贝出元素数组（栈底->栈顶顺序，长度精确为当前元素数）：遍历在 VM 层完成
    Array<T> toArray()
    {
        Array<T> arr = Array<T>( this._count )
        if this._count <= 0
        {
            ret arr
        }
        SystemStackToArray( this, arr )
        ret arr
    }
    #转换为列表（SystemArrayCopy 系统级拷贝）
    List<T> toList()
    {
        ret List<T>( this.toArray() )
    }
    #浅拷贝（Python stack.copy() / Dart List.of）：元素按下标顺序重新入栈，栈序不变
    Stack<T> copy()
    {
        ret Stack<T>( this.toArray() )
    }

    # ── 迭代器（foreach 热路径，SystemStackMoveNext 在 VM 层按 LIFO 顺序推进）──

    override void reset()
    {
        this._index = -1
        this._current = null
    }
    override bool moveNext()
    {
        ret SystemStackMoveNext( this )
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

    #输出 [a,b,c] 格式（栈底->栈顶，栈顶为最右侧元素）：结构遍历在 VM 层完成（toArray）
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
