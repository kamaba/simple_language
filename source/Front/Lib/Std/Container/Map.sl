
#键值对实体：hashId 缓存 key.hashCode（供哈希类容器扩展使用），key/value 公开可读写。
#独立顶级类：外部模块通过 Map 迭代器 current / entryAt 访问 key、value。
public class MapEntity<TKey,TValue>
{
    public int hashId = 0
    public TKey key = null
    public TValue value = null
}

#字典容器：仿 Java HashMap / CLR Dictionary / Dart Map。
#底层与 List 同构：Array<MapEntity<TKey,TValue>> 顺序存储，
#增删改查全部通过 SystemArray* 系统函数操作底层数组。
#key 匹配用 == 值比较（数值/字符串/布尔按值，类按 equals 语义），重复 key 覆盖 value（put 语义）。
public class Map<TKey,TValue> extends Object interface IMap<TKey,TValue>, Core.IIterable<MapEntity<TKey,TValue>>, Core.IIterator<MapEntity<TKey,TValue>>
{
    Array<MapEntity<TKey,TValue>> _list = null
    int _length = 0
    int _capacity = 0
    int _index = 0;
    MapEntity<TKey,TValue> _current = null

    #默认构造，容量为0，首次添加时扩容为4（与 C# Dictionary 一致）
    override  _init_()
    {
        this._list = Array<MapEntity<TKey,TValue>>(4)
    }
    #指定初始容量构造（负数按 0 处理）
    override void _init_( int capacity )
    {
        if capacity < 0
        {
            capacity = 0
        }
        this._list = Array<MapEntity<TKey,TValue>>(capacity)
        this._capacity = capacity
    }
    get int length(){ ret this._length }
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

    #容量扩展：0->4，之后倍增 4->8->16...（与 C# 容器一致）
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

    #内部方法：SystemArrayResize 系统级重分配底层数组并保留已有元素
    void resizeArray( int newCapacity )
    {
        this._list = SystemArrayResize(this._list, newCapacity)
    }

    #查找 key 首次出现的实体下标，未找到返回 -1
    #（== 为值比较：数值/字符串/布尔按值，类按 equals 语义，与 List.indexOf 一致）
    public int indexOfKey( TKey key )
    {
        for i = 0, i < this._length, i++
        {
            var ent = SystemArrayGetValueThis(this._list, i) as MapEntity<TKey,TValue>
            if ent != null && ent.key == key
            {
                ret i
            }
        }
        ret -1
    }
    #add 语义（同 C# Dictionary.Add 的无异常版 / TryAdd）：key 已存在时不修改原值并返回 false，新插入返回 true
    #需要覆盖旧值请用 m[key] = value（put 语义）
    public override bool add( TKey key, TValue value )
    {
        int idx = this.indexOfKey(key)
        if idx >= 0
        {
            ret false
        }
        if this._length >= this._capacity
        {
            this.grow()
        }
        MapEntity<TKey,TValue> me = new()
        me.key = key
        me.value = value
        if key != null
        {
            me.hashId = key.hashCode
        }
        SystemArraySetValueThis(this._list, this._length, me)
        this._length++
        ret true
    }
    #m[key] = value 写入语义（put，同 Java HashMap.put / Dart m[k]=v）：key 已存在则更新 value，不存在则插入
    public override void _setItem_( TKey key, TValue value )
    {
        int idx = this.indexOfKey(key)
        if idx >= 0
        {
            MapEntity<TKey,TValue> ent = SystemArrayGetValueThis(this._list, idx) as MapEntity<TKey,TValue>
            ent.value = value
            ret
        }
        this.add(key, value)
    }
    #m[key] 读取语义：key 不存在返回 null（Dart Map 语义）
    public override TValue _getItem_( TKey key )
    {
        int idx = this.indexOfKey(key)
        if idx < 0
        {
            ret null
        }
        MapEntity<TKey,TValue> ent = SystemArrayGetValueThis(this._list, idx) as MapEntity<TKey,TValue>
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
        for i = 0, i < this._length, i++
        {
            var ent = SystemArrayGetValueThis(this._list, i) as MapEntity<TKey,TValue>
            if ent != null && ent.value == value
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
        MapEntity<TKey,TValue> ent = SystemArrayGetValueThis(this._list, idx) as MapEntity<TKey,TValue>
        ret ent.value
    }
    #key 不存在时才插入（Java 8 putIfAbsent）：返回已存在的值，原本不存在则插入并返回 null
    public TValue putIfAbsent( TKey key, TValue value )
    {
        int idx = this.indexOfKey(key)
        if idx >= 0
        {
            MapEntity<TKey,TValue> ent = SystemArrayGetValueThis(this._list, idx) as MapEntity<TKey,TValue>
            ret ent.value
        }
        this.add(key, value)
        ret null
    }
    #删除指定 key 并返回其旧值（Java remove / Dart remove 语义），key 不存在返回 null
    public override TValue remove( TKey key )
    {
        int idx = this.indexOfKey(key)
        if idx < 0
        {
            ret null
        }
        MapEntity<TKey,TValue> ent = SystemArrayGetValueThis(this._list, idx) as MapEntity<TKey,TValue>
        TValue oldValue = ent.value
        SystemArrayRemoveAtValue(this._list, idx, this._length)
        this._length--
        ret oldValue
    }
    #按下标删除实体（下标非法忽略）
    public override void removeAt( int index )
    {
        if index < 0 || index >= this._length
        {
            ret
        }
        SystemArrayRemoveAtValue(this._list, index, this._length)
        this._length--
    }
    #按下标取实体（下标非法返回 null）
    public MapEntity<TKey,TValue> entryAt( int index )
    {
        if index < 0 || index >= this._length
        {
            ret null
        }
        ret SystemArrayGetValueThis(this._list, index) as MapEntity<TKey,TValue>
    }
    public override void clear()
    {
        this._length = 0
        this._capacity = 0
        this._list = Array<MapEntity<TKey,TValue>>(0)
        this._index = -1
        this._current = null
    }

    #全部 key 组成的列表（Dart Map.keys）
    public get List<TKey> keys()
    {
        List<TKey> keyList = List<TKey>(this._length)
        for i = 0, i < this._length, i++
        {
            var ent = SystemArrayGetValueThis(this._list, i) as MapEntity<TKey,TValue>
            if ent != null
            {
                keyList.add(ent.key)
            }
        }
        ret keyList
    }
    #全部 value 组成的列表（Dart Map.values）
    public get List<TValue> values()
    {
        List<TValue> valueList = List<TValue>(this._length)
        for i = 0, i < this._length, i++
        {
            var ent = SystemArrayGetValueThis(this._list, i) as MapEntity<TKey,TValue>
            if ent != null
            {
                valueList.add(ent.value)
            }
        }
        ret valueList
    }
    #拷贝出实体数组（长度精确为当前元素数；SystemArrayCopy 返回 Array<Object>，
    #无法直接绑定 Array<MapEntity<TKey,TValue>>，故按下标逐个系统级拷贝）
    Array<MapEntity<TKey,TValue>> toArray()
    {
        Array<MapEntity<TKey,TValue>> arr = Array<MapEntity<TKey,TValue>>(this._length)
        for i = 0, i < this._length, i++
        {
            SystemArraySetValueThis(arr, i, SystemArrayGetValueThis(this._list, i))
        }
        ret arr
    }
    List<MapEntity<TKey,TValue>> toList()
    {
        List<MapEntity<TKey,TValue>> list = List<MapEntity<TKey,TValue>>(this._length)
        for i = 0, i < this._length, i++
        {
            list.add(SystemArrayGetValueThis(this._list, i) as MapEntity<TKey,TValue>)
        }
        ret list
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
            this._current = SystemArrayGetValueThis(this._list, this._index) as MapEntity<TKey,TValue>
        }
        else
        {
            this._current = null
        }
        ret hasNext_var
    }
    override get MapEntity<TKey,TValue> current()
    {
        ret this._current;
    }
    #迭代位置写入：替换当前实体的 value（key 不可变）
    set void current( TValue val )
    {
        var ent = SystemArrayGetValueThis(this._list, this._index) as MapEntity<TKey,TValue>
        if ent != null
        {
            ent.value = val
        }
        this._current = ent
    }
    override get Core.IIterator<MapEntity<TKey,TValue>> iterator()
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
        this._current = SystemArrayGetValueThis(this._list, ind) as MapEntity<TKey,TValue>
    }
    #输出 {key=value,key=value} 格式（同 Java HashMap.toString）
    override string toString()
    {
        string showstr = "{"
        for i = 0, i < this._length, i++
        {
            var ent = SystemArrayGetValueThis(this._list, i) as MapEntity<TKey,TValue>
            if ent == null
            {
                showstr = showstr + "null"
            }
            else
            {
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
            if( i < this._length - 1 )
            {
                showstr += ","
            }
        }
        ret showstr + "}"
    }
}
