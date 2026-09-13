
#键值对实体：hashId 缓存 key.hashCode（供哈希类容器扩展使用），key/value 公开可读写。
#link 字段用于哈希桶链表（同 bucket 冲突时的链式地址）与删除后的空闲链表复用。
#独立顶级类：外部模块通过 Map 迭代器 current / entryAt 访问 key、value。
public class MapEntity<TKey,TValue>
{
    public int hashId = 0
    public TKey key = null
    public TValue value = null
    public int link = -1

    public override string toString()
    {
        ret "MapEntity{hashId:" + this.hashId.toString() + ",key:" + this.key.toString() + ",value:" + this.value.toString() + "}"
    }
}

#字典容器：仿 C# Dictionary<TKey,TValue> 的哈希表实现。
#核心数据结构：
#   _buckets  -- 桶数组，存储 entries 中的索引+1（0 表示空桶）
#   _entries  -- 实际存放 key-value 的实体数组
#   _count    -- 已使用的槽位数（含已删除的空闲槽）
#   _freeList -- 被删除元素的空闲链表头（-1 表示无空闲槽，复用空位是 .NET 核心优化）
#   _freeCount-- 空闲槽数量
# 有效元素个数 = _count - _freeCount
# key 匹配用 hashCode + == 值比较（数值/字符串/布尔按值，类按 equals 语义）
public class Map<TKey,TValue> extends Object interface IMap<TKey,TValue>, Core.IIterable<MapEntity<TKey,TValue>>, Core.IIterator<MapEntity<TKey,TValue>>
{
    # --- C# Dictionary 核心字段 ---
    Array<int> _buckets = null              # 桶数组，存Entry的索引+1（0=空桶）
    Array<MapEntity<TKey,TValue>> _entries = null  # 实际存放 key-value 的实体数组
    int _count = 0                         # 已使用的槽位数（含空闲槽）
    int _freeList = -1                     # 被删除元素的链表头（-1=无空闲）
    int _freeCount = 0                     # 删除空槽数量

    # --- 迭代器字段 ---
    int _index = -1
    MapEntity<TKey,TValue> _current = null

    #默认构造，容量为0，首次添加时扩容为4（与 C# Dictionary 一致）
    override  _init_()
    {
    }
    #指定初始容量构造（负数按 0 处理）
    override void _init_( int capacity )
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

    #有效元素个数
    get int length(){ ret this._count - this._freeCount }

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
    override get int capacity()
    {
        if this._entries == null
        {
            ret 0
        }
        ret this._entries.length
    }
    override set void capacity( int value )
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
    override void ensureCapacity( int min )
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

    #重新分配 _buckets 和 _entries，并将已有有效元素重新哈希
    void resize( int newSize )
    {
        #创建新桶（全部为0=空桶）
        Array<int> newBuckets = Array<int>(newSize)
        #扩容 entries 数组（SystemArrayResize 保留已有元素）
        Array<MapEntity<TKey,TValue>> newEntries = null
        if this._entries == null
        {
            newEntries = Array<MapEntity<TKey,TValue>>(newSize)
        }
        else
        {
            newEntries = SystemArrayResize(this._entries, newSize)
        }

        #重新哈希：遍历所有已使用槽位，重建桶链
        for i = 0, i < this._count, i++
        {
            var ent = SystemArrayGetValueThis(newEntries, i) as MapEntity<TKey,TValue>
            if ent != null && ent.hashId >= 0
            {
                int bucket = ent.hashId % newSize
                int bucketValue = SystemArrayGetValueThis(newBuckets, bucket) as int
                ent.link = bucketValue - 1
                SystemArraySetValueThis(newBuckets, bucket, i + 1)
            }
        }

        this._buckets = newBuckets
        this._entries = newEntries
    }
    #计算 key 的哈希值
    int getHash( TKey key )
    {
        ret key.hashCode()
    }

    #查找 key 在 entries 中的下标，未找到返回 -1
    #通过 SystemMapFindEntry 系统调用在 VM 层完成桶链遍历（while 循环），
    #避免 SL 层多次 SystemArrayGetValueThis + 成员访问的开销
    int indexOfKey( TKey key )
    {
        if this._entries == null || this._count == 0
        {
            ret -1
        }
        if key == null
        {
            ret -1
        }
        int hash = this.getHash(key)
        int bucketSize = this._entries.length
        int bucket = hash % bucketSize
        ret SystemMapFindEntry(this._entries, this._buckets, key, hash, bucket)
    }

    
    public bool TryAdd(TKey key, TValue value)
    {
        ret true;
    }
    public bool TryGetValue(TKey key, TValue value)
    {
        ret true;
    }

    #插入新 entry（不复用空闲槽时在 _count 位置追加，必要时扩容）
    void insertEntry( TKey key, TValue value )
    {
        int hash = this.getHash(key)

        if this._entries == null
        {
            this.grow()
        }

        int slotIndex = -1
        if this._freeList >= 0
        {
            #复用空闲槽
            slotIndex = this._freeList
            var oldEnt = SystemArrayGetValueThis(this._entries, slotIndex) as MapEntity<TKey,TValue>
            this._freeList = oldEnt.link
            this._freeCount--
        }
        else
        {
            #无空闲槽，检查是否需要扩容
            if this._count >= this._entries.length
            {
                this.grow()
            }
            slotIndex = this._count
            this._count++
        }

        #创建新 entry 并存储
        MapEntity<TKey,TValue> ent = new()
        ent.key = key
        ent.value = value
        ent.hashId = hash
        SystemArraySetValueThis(this._entries, slotIndex, ent)

        #插入桶链头部
        int bucketSize = this._entries.length
        int bucket = hash % bucketSize
        int bucketValue = SystemArrayGetValueThis(this._buckets, bucket) as int
        ent.link = bucketValue - 1
        SystemArraySetValueThis(this._buckets, bucket, slotIndex + 1)
    }

    #add 语义（同 C# Dictionary.Add 的无异常版 / TryAdd）：key 已存在时不修改原值并返回 false，新插入返回 true
    #需要覆盖旧值请用 m[key] = value（put 语义）
    public override bool add( TKey key, TValue value )
    {
        if key == null
        {
            ret false
        }
        int idx = this.indexOfKey(key)
        if idx >= 0
        {
            ret false
        }
        this.insertEntry(key, value)
        ret true
    }
    #m[key] = value 写入语义（put，同 Java HashMap.put / Dart m[k]=v）：key 已存在则更新 value，不存在则插入
    public override void _setItem_( TKey key, TValue value )
    {
        if key == null
        {
            ret
        }
        int idx = this.indexOfKey(key)
        if idx >= 0
        {
            var ent = SystemArrayGetValueThis(this._entries, idx) as MapEntity<TKey,TValue>
            ent.value = value
            ret
        }
        this.insertEntry(key, value)
    }
    #m[key] 读取语义：key 不存在返回 null（Dart Map 语义）
    public override TValue _getItem_( TKey key )
    {
        int idx = this.indexOfKey(key)
        if idx < 0
        {
            ret null
        }
        var ent = SystemArrayGetValueThis(this._entries, idx) as MapEntity<TKey,TValue>
        ret ent.value
    }

    #是否包含指定 key
    public bool containsKey( TKey key )
    {
        if this.indexOfKey(key) >= 0
        {
            ret true
        }
        ret false
    }
    #是否包含指定 value
    public bool containsValue( TValue value )
    {
        if this._entries == null
        {
            ret false
        }
        for i = 0, i < this._count, i++
        {
            var ent = SystemArrayGetValueThis(this._entries, i) as MapEntity<TKey,TValue>
            if ent != null && ent.hashId >= 0 && ent.value == value
            {
                ret true
            }
        }
        ret false
    }
    #读取指定 key 的值，不存在返回 defaultValue（Java 8 getOrDefault）
    public TValue getOrDefault( TKey key, TValue defaultValue )
    {
        int idx = this.indexOfKey(key)
        if idx < 0
        {
            ret defaultValue
        }
        var ent = SystemArrayGetValueThis(this._entries, idx) as MapEntity<TKey,TValue>
        ret ent.value
    }
    #key 不存在时才插入（Java 8 putIfAbsent）：返回已存在的值，原本不存在则插入并返回 null
    public TValue putIfAbsent( TKey key, TValue value )
    {
        int idx = this.indexOfKey(key)
        if idx >= 0
        {
            var ent = SystemArrayGetValueThis(this._entries, idx) as MapEntity<TKey,TValue>
            ret ent.value
        }
        this.insertEntry(key, value)
        ret null
    }

    #删除指定 key 并返回其旧值（Java remove / Dart remove 语义），key 不存在返回 null
    public override TValue remove( TKey key )
    {
        if this._entries == null || this._count == 0
        {
            ret null
        }
        if key == null
        {
            ret null
        }
        int hash = this.getHash(key)
        int bucketSize = this._entries.length
        int bucket = hash % bucketSize
        int bucketValue = SystemArrayGetValueThis(this._buckets, bucket) as int
        int entryIndex = bucketValue - 1
        int prev = -1

        while entryIndex >= 0
        {
            var ent = SystemArrayGetValueThis(this._entries, entryIndex) as MapEntity<TKey,TValue>
            if ent.hashId == hash && ent.key == key
            {
                #找到--从链中摘除
                if prev >= 0
                {
                    var prevEnt = SystemArrayGetValueThis(this._entries, prev) as MapEntity<TKey,TValue>
                    prevEnt.link = ent.link
                }
                else
                {
                    #原为链头
                    SystemArraySetValueThis(this._buckets, bucket, ent.link + 1)
                }

                TValue oldValue = ent.value

                #加入空闲链表
                ent.link = this._freeList
                ent.hashId = -1
                this._freeList = entryIndex
                this._freeCount++

                ret oldValue
            }
            prev = entryIndex
            entryIndex = ent.link
        }
        ret null
    }

    #按下标删除实体（下标非法忽略）
    public override void removeAt( int index )
    {
        if index < 0 || index >= this._count
        {
            ret
        }
        if this._entries == null
        {
            ret
        }
        var ent = SystemArrayGetValueThis(this._entries, index) as MapEntity<TKey,TValue>
        if ent == null || ent.hashId < 0
        {
            ret
        }

        int hash = ent.hashId
        int bucketSize = this._entries.length
        int bucket = hash % bucketSize

        #在桶链中查找并摘除该 entry
        int bucketValue = SystemArrayGetValueThis(this._buckets, bucket) as int
        int entryIndex = bucketValue - 1
        int prev = -1
        while entryIndex >= 0
        {
            var curEnt = SystemArrayGetValueThis(this._entries, entryIndex) as MapEntity<TKey,TValue>
            if entryIndex == index
            {
                #找到--从链中摘除
                if prev >= 0
                {
                    var prevEnt = SystemArrayGetValueThis(this._entries, prev) as MapEntity<TKey,TValue>
                    prevEnt.link = curEnt.link
                }
                else
                {
                    SystemArraySetValueThis(this._buckets, bucket, curEnt.link + 1)
                }

                #加入空闲链表
                curEnt.link = this._freeList
                curEnt.hashId = -1
                this._freeList = entryIndex
                this._freeCount++
                ret
            }
            prev = entryIndex
            entryIndex = curEnt.link
        }
    }

    #按下标取实体（下标非法或已删除返回 null）
    public MapEntity<TKey,TValue> entryAt( int index )
    {
        if index < 0 || index >= this._count
        {
            ret null
        }
        if this._entries == null
        {
            ret null
        }
        var ent = SystemArrayGetValueThis(this._entries, index) as MapEntity<TKey,TValue>
        if ent == null || ent.hashId < 0
        {
            ret null
        }
        ret ent
    }

    public override void clear()
    {
        this._buckets = null
        this._entries = null
        this._count = 0
        this._freeList = -1
        this._freeCount = 0
        this._index = -1
        this._current = null
    }

    #全部 key 组成的列表（Dart Map.keys）
    public get List<TKey> keys()
    {
        int len = this._count - this._freeCount
        List<TKey> keyList = List<TKey>(len)
        if this._entries == null
        {
            ret keyList
        }
        for i = 0, i < this._count, i++
        {
            var ent = SystemArrayGetValueThis(this._entries, i) as MapEntity<TKey,TValue>
            if ent != null && ent.hashId >= 0
            {
                keyList.add(ent.key)
            }
        }
        ret keyList
    }
    #全部 value 组成的列表（Dart Map.values）
    public get List<TValue> values()
    {
        int len = this._count - this._freeCount
        List<TValue> valueList = List<TValue>(len)
        if this._entries == null
        {
            ret valueList
        }
        for i = 0, i < this._count, i++
        {
            var ent = SystemArrayGetValueThis(this._entries, i) as MapEntity<TKey,TValue>
            if ent != null && ent.hashId >= 0
            {
                valueList.add(ent.value)
            }
        }
        ret valueList
    }

    #拷贝出实体数组（长度精确为当前有效元素数）
    Array<MapEntity<TKey,TValue>> toArray()
    {
        int len = this._count - this._freeCount
        Array<MapEntity<TKey,TValue>> arr = Array<MapEntity<TKey,TValue>>(len)
        if this._entries == null
        {
            ret arr
        }
        int j = 0
        for i = 0, i < this._count, i++
        {
            var ent = SystemArrayGetValueThis(this._entries, i) as MapEntity<TKey,TValue>
            if ent != null && ent.hashId >= 0
            {
                SystemArraySetValueThis(arr, j, ent)
                j++
            }
        }
        ret arr
    }
    List<MapEntity<TKey,TValue>> toList()
    {
        int len = this._count - this._freeCount
        List<MapEntity<TKey,TValue>> list = List<MapEntity<TKey,TValue>>(len)
        if this._entries == null
        {
            ret list
        }
        for i = 0, i < this._count, i++
        {
            var ent = SystemArrayGetValueThis(this._entries, i) as MapEntity<TKey,TValue>
            if ent != null && ent.hashId >= 0
            {
                list.add(ent)
            }
        }
        ret list
    }

    #接口层：迭代器
    override void reset()
    {
        this._index = -1
        this._current = null
    }
    override bool moveNext()
    {
        if this._entries == null
        {
            ret false
        }
        this._index++
        while this._index < this._count
        {
            var ent = SystemArrayGetValueThis(this._entries, this._index) as MapEntity<TKey,TValue>
            if ent != null && ent.hashId >= 0
            {
                this._current = ent
                ret true
            }
            this._index++
        }
        this._current = null
        ret false
    }
    override get MapEntity<TKey,TValue> current()
    {
        ret this._current
    }
    #迭代位置写入：替换当前实体的 value（key 不可变）
    set void current( TValue val )
    {
        if this._current != null
        {
            this._current.value = val
        }
    }
    override get Core.IIterator<MapEntity<TKey,TValue>> iterator()
    {
        ret this
    }
    get int index()
    {
        ret this._index
    }
    set void index( int ind )
    {
        if ind < 0 || ind >= this._count
        {
            ret
        }
        this._index = ind
        if this._entries != null
        {
            this._current = SystemArrayGetValueThis(this._entries, ind) as MapEntity<TKey,TValue>
        }
    }

    #输出 {key=value,key=value} 格式（同 Java HashMap.toString）
    override string toString()
    {
        string showstr = "{"
        int first = 1
        if this._entries != null
        {
            for i = 0, i < this._count, i++
            {
                var ent = SystemArrayGetValueThis(this._entries, i) as MapEntity<TKey,TValue>
                if ent != null && ent.hashId >= 0
                {
                    if first == 1
                    {
                        first = 0
                    }
                    else
                    {
                        showstr += ","
                    }
                    string kstr = "null"
                    string vstr = "null"
                    if ent.key != null
                    {
                        kstr = ent.key.toString()
                    }
                    if ent.value != null
                    {
                        vstr = ent.value.toString()
                    }
                    showstr = showstr + kstr + "=" + vstr
                }
            }
        }
        ret showstr + "}"
    }
}
