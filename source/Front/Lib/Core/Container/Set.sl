
#集合元素实体：hashId 缓存 value.hashCode（桶链快速过滤用），value 公开可读写。
#link 字段用于哈希桶冲突链（同 bucket 的链式地址），删除后复用作空闲链表节点（.NET 空间复用优化）。
#独立顶级类：与 Map.MapEntity 对称，便于调试观察。
public class SetEntity<T>
{
    public int hashId = 0
    public T value = null
    public int link = -1

    public override string toString()
    {
        string vstr = "null"
        if this.value != null
        {
            vstr = this.value.toString()
        }
        ret "SetEntity{hashId:" + this.hashId.toString() + ",value:" + vstr + "}"
    }
}

#无序不重复集合：仿 C# HashSet<T>（即只存 key 的 Dictionary），API 参考 Python set / Dart Set。
#核心数据结构（与 Core.Map 一致）：
#   _buckets  -- 桶数组，存储 entries 中的索引+1（0 表示空桶）
#   _entries  -- 实际存放元素的实体数组（SetEntity，hashId 缓存哈希，-1 表示空闲槽）
#   _count    -- 已使用的槽位数（含已删除的空闲槽）
#   _freeList -- 被删除元素的空闲链表头（-1 表示无空闲槽）
#   _freeCount-- 空闲槽数量
# 有效元素个数 = _count - _freeCount
# 元素匹配用 hashCode + == 值比较（与 Map 的 key 匹配语义一致）
#性能设计：桶链查找/插入/删除、扩容重哈希、集合运算、迭代推进等查找比较与遍历的耗时操作
#全部通过 SystemSet* 系统调用映射到 CVM 原生实现（set_system_method.c），
#SL 层仅负责 hashCode 虚调用（用户类可重写）、SetEntity 泛型实例化与初始数组分配。
public class Set<T> extends Object interface Core.IIterable<T>, Core.IIterator<T>
{
    # --- C# HashSet 核心字段 ---
    Array<int> _buckets = null                    # 桶数组，存Entry的索引+1（0=空桶）
    Array<SetEntity<T>> _entries = null          # 实际存放元素的实体数组
    int _count = 0                               # 已使用的槽位数（含空闲槽）
    int _freeList = -1                           # 被删除元素的空闲链表头（-1=无空闲）
    int _freeCount = 0                           # 空闲槽数量

    # --- 迭代器字段 ---
    int _index = -1
    T _current = null

    #默认构造，容量为0，首次添加时分配为4（与 C# HashSet 一致）
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
            this.resize(capacity)
        }
    }
    #从数组构造（Python set(iterable) / Dart Set.from）：哈希须在 SL 层计算故逐个 add
    void _init_( Array<T> items )
    {
        if items == null || items.length <= 0
        {
            ret
        }
        this.ensureCapacity(items.length)
        for i = 0, i < items.length, i++
        {
            this.add(items[i])
        }
    }

    #有效元素个数（Python len(set) / Dart set.length）
    get int length()
    {
        ret this._count - this._freeCount
    }
    get bool isEmpty()
    {
        if this._count - this._freeCount <= 0
        {
            ret true
        }
        ret false
    }
    get bool isNotEmpty()
    {
        if this._count - this._freeCount > 0
        {
            ret true
        }
        ret false
    }

    #容量（内部数组长度）
    get int capacity()
    {
        if this._entries == null
        {
            ret 0
        }
        ret this._entries.length
    }
    set void capacity( int value )
    {
        if value < this._count - this._freeCount
        {
            ret
        }
        if this._entries == null
        {
            if value > 0
            {
                this.resize(value)
            }
            ret
        }
        if value != this._entries.length
        {
            this.resize(value)
        }
    }

    #容量扩展：0->4，之后倍增 4->8->16...（与 C# 容器一致）
    void grow()
    {
        int newCapacity = 4
        if this._entries != null && this._entries.length > 0
        {
            newCapacity = this._entries.length * 2
        }
        this.resize(newCapacity)
    }
    #预分配容量，避免多次扩容重哈希（C# HashSet.EnsureCapacity）
    void ensureCapacity( int min )
    {
        int curCap = 0
        if this._entries != null
        {
            curCap = this._entries.length
        }
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
            this.resize(newCapacity)
        }
    }

    #重哈希：新数组分配、空闲槽压缩、桶链重建全部在 VM 层完成（SystemSetRehash）
    void resize( int newSize )
    {
        if newSize < 4
        {
            newSize = 4
        }
        if this._entries == null
        {
            #首次分配：泛型数组（Array<SetEntity<T>>）须由前端实例化
            this._buckets = Array<int>(newSize)
            this._entries = Array<SetEntity<T>>(newSize)
            ret
        }
        if newSize == this._entries.length
        {
            ret
        }
        SystemSetRehash(this, newSize)
    }

    #计算元素的哈希值（虚调用，用户类可重写 hashCode）
    int getHash( T item )
    {
        ret item.hashCode()
    }

    #添加元素（C# HashSet.Add / Python set.add / Dart Set.add）
    #已存在返回 false，新增返回 true；桶链查重、空闲槽复用、扩容重哈希均在 VM 层完成
    public bool add( T item )
    {
        if item == null
        {
            ret false
        }
        if this._entries == null
        {
            this.grow()
        }
        SetEntity<T> ent = new()
        ent.hashId = this.getHash(item)
        ent.value = item
        ret SystemSetAddEntry(this, ent)
    }
    #从数组批量添加（Python set.update(iterable)）：哈希须在 SL 层计算故逐个 add
    public void addRange( Array<T> items )
    {
        if items == null || items.length <= 0
        {
            ret
        }
        this.ensureCapacity(this._count - this._freeCount + items.length)
        for i = 0, i < items.length, i++
        {
            this.add(items[i])
        }
    }

    #是否包含指定元素（Python in / C# Contains / Dart contains）：VM 层桶链查找
    public bool contains( T item )
    {
        if item == null || this._entries == null
        {
            ret false
        }
        ret SystemSetContains(this, item, this.getHash(item))
    }
    #移除指定元素（C# HashSet.Remove / Dart Set.remove）：VM 层桶链查找并摘除，成功返回 true
    public bool remove( T item )
    {
        if item == null || this._entries == null
        {
            ret false
        }
        ret SystemSetRemoveEntry(this, item, this.getHash(item))
    }
    #清空集合（Python set.clear / C# Clear）：VM 层置空内部数组并复位迭代器
    public void clear()
    {
        SystemSetClear(this)
    }

    # ---- 修改型集合运算（VM 层完成）----
    #并集并入（Python |= / set.update；C# UnionWith）
    public void unionWith( Set<T> other )
    {
        SystemSetUnionWith(this, other)
    }
    #交集保留（Python &=；C# IntersectWith）：删除不在 other 中的元素
    public void intersectWith( Set<T> other )
    {
        SystemSetIntersectWith(this, other)
    }
    #差集移除（Python -=；C# ExceptWith）：删除同时在 other 中的元素
    public void exceptWith( Set<T> other )
    {
        SystemSetExceptWith(this, other)
    }
    #对称差（Python ^=；C# SymmetricExceptWith）：删除交集并并入双方独有元素
    public void symmetricExceptWith( Set<T> other )
    {
        SystemSetSymmetricExceptWith(this, other)
    }

    # ---- 非修改型集合运算（返回新 Set，底层为 VM 调用组合）----
    #并集（Python | / set.union）
    public Set<T> union( Set<T> other )
    {
        Set<T> result = Set<T>()
        result.unionWith(this)
        result.unionWith(other)
        ret result
    }
    #交集（Python & / set.intersection）
    public Set<T> intersection( Set<T> other )
    {
        Set<T> result = Set<T>()
        result.unionWith(this)
        result.intersectWith(other)
        ret result
    }
    #差集（Python - / set.difference）
    public Set<T> difference( Set<T> other )
    {
        Set<T> result = Set<T>()
        result.unionWith(this)
        result.exceptWith(other)
        ret result
    }
    #对称差（Python ^ / set.symmetric_difference）
    public Set<T> symmetricDifference( Set<T> other )
    {
        Set<T> result = Set<T>()
        result.unionWith(this)
        result.symmetricExceptWith(other)
        ret result
    }
    #浅拷贝（Python set.copy / Dart Set.toSet）：VM 层单次调用完成
    public Set<T> copy()
    {
        Set<T> result = Set<T>()
        result.unionWith(this)
        ret result
    }

    # ---- 判断型集合运算（VM 层完成）----
    #子集判断（Python issubset；C# IsSubsetOf）：空集是任何集合的子集
    public bool isSubsetOf( Set<T> other )
    {
        ret SystemSetIsSubsetOf(this, other)
    }
    #超集判断（Python issuperset；C# IsSupersetOf）：任何集合是空集的超集
    public bool isSupersetOf( Set<T> other )
    {
        ret SystemSetIsSupersetOf(this, other)
    }
    #真子集判断（Python set < set）
    public bool isProperSubsetOf( Set<T> other )
    {
        if this.isSubsetOf(other)
        {
            if this.length < other.length
            {
                ret true
            }
        }
        ret false
    }
    #真超集判断（Python set > set）
    public bool isProperSupersetOf( Set<T> other )
    {
        if this.isSupersetOf(other)
        {
            if this.length > other.length
            {
                ret true
            }
        }
        ret false
    }
    #交集非空判断（C# Overlaps）：任一公共元素即 true
    public bool overlaps( Set<T> other )
    {
        ret SystemSetOverlaps(this, other)
    }
    #集合相等判断（C# SetEquals）：元素完全相同（与顺序无关）
    public bool setEquals( Set<T> other )
    {
        ret SystemSetSetEquals(this, other)
    }

    # ---- 转换 ----
    #拷贝出元素数组（长度精确为当前有效元素数）：VM 层遍历填充
    Array<T> toArray()
    {
        int len = this._count - this._freeCount
        Array<T> arr = Array<T>(len)
        if this._entries == null
        {
            ret arr
        }
        SystemSetToArray(this, arr)
        ret arr
    }
    #转换为列表（SystemArrayCopy 系统级拷贝）
    List<T> toList()
    {
        ret List<T>(this.toArray())
    }
    #首元素（Dart Set.first）：遍历在 VM 层完成
    get T first()
    {
        if this._count - this._freeCount <= 0
        {
            ret null
        }
        Array<T> arr = this.toArray()
        ret SystemArrayGetValueThis(arr, 0) as T
    }
    #末元素（Dart Set.last）：遍历在 VM 层完成
    get T last()
    {
        if this._count - this._freeCount <= 0
        {
            ret null
        }
        Array<T> arr = this.toArray()
        ret SystemArrayGetValueThis(arr, arr.length - 1) as T
    }

    # ---- 迭代器（foreach 热路径，SystemSetMoveNext 在 VM 层推进并跳过空闲槽）----
    override void reset()
    {
        this._index = -1
        this._current = null
    }
    override bool moveNext()
    {
        ret SystemSetMoveNext(this)
    }
    override get T current()
    {
        ret this._current
    }
    override get Core.IIterator<T> iterator()
    {
        ret this
    }
    get int index()
    {
        ret this._index
    }

    #输出 {a,b,c} 格式（同 Python/Dart 集合字面量）：桶结构遍历在 VM 层完成（toArray）
    override string toString()
    {
        Array<T> arr = this.toArray()
        string showstr = "{"
        int first_ = 1
        for i = 0, i < arr.length, i++
        {
            if first_ == 1
            {
                first_ = 0
            }
            else
            {
                showstr += ","
            }
            var v = SystemArrayGetValueThis(arr, i) as T
            if v != null
            {
                showstr += v.toString()
            }
            else
            {
                showstr += "null"
            }
        }
        ret showstr + "}"
    }
}
