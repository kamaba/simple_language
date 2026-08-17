import Std;

MapTest
{
    # 测试基本 add + _getItem_ + length
    static testBasicAdd()
    {
        Console.println("===== testBasicAdd =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        map.add(3, "three")
        Console.println("length = " + map.length)
        Console.println("map[1] = " + map._getItem_(1))
        Console.println("map[2] = " + map._getItem_(2))
        Console.println("map[3] = " + map._getItem_(3))
        Console.println("map[99] = " + map._getItem_(99))
    }

    # 测试字面量初始化 {key:value}
    static testInitializer()
    {
        Console.println("===== testInitializer =====")
        Map<int,string> map = Map<int,string>(4){100:"aaa", 200:"bbb"}
        Console.println("length = " + map.length)
        Console.println("map[100] = " + map._getItem_(100))
        Console.println("map[200] = " + map._getItem_(200))
    }

    # 测试重复 key：add 已存在返回 false 且不覆盖，m[key]=value 才覆盖
    static testDuplicateKey()
    {
        Console.println("===== testDuplicateKey =====")
        Std.Map<int,string> map = new()
        Console.println("add(1, first) ret = " + map.add(1, "first"))
        Console.println("add(1, second) ret = " + map.add(1, "second"))
        Console.println("add(1, third) ret = " + map.add(1, "third"))
        Console.println("length = " + map.length)
        Console.println("map[1] = " + map._getItem_(1))
        map._setItem_(1, "third")
        Console.println("after setItem(1, third), map[1] = " + map._getItem_(1))
    }

    # 测试 _setItem_ 写入（存在更新 / 不存在添加）
    static testSetGetValue()
    {
        Console.println("===== testSetGetValue =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map._setItem_(1, "ONE")
        Console.println("after set existing, map[1] = " + map._getItem_(1))
        map._setItem_(9, "nine")
        Console.println("after set new, length = " + map.length)
        Console.println("map[9] = " + map._getItem_(9))
    }

    # 测试下标读写 map[key] / map[key] = value（常量 / 变量 / 字符串 key）
    static testSubscript()
    {
        Console.println("===== testSubscript =====")
        Std.Map<int,string> map = new()
        map[10] = "aa"
        map[20] = "bb"
        Console.println("map[10] = " + map[10])
        Console.println("map[20] = " + map[20])
        Console.println("map[99] = " + map[99])
        int k = 20
        map[k] = "BB"
        Console.println("after map[k=20] = BB, map[20] = " + map[20])
        int k2 = 30
        map[k2] = "cc"
        Console.println("after map[k2=30] = cc, length = " + map.length)
        Console.println("map[k2=30] = " + map[k2])
        Std.Map<string,int> smap = new()
        smap["one"] = 1
        smap["two"] = 2
        string sk = "one"
        Console.println("smap[sk=one] = " + smap[sk])
        Console.println("smap[two] = " + smap["two"])
        Console.println("smap[three] = " + smap["three"])
    }

    # 测试 containsKey / containsValue
    static testContains()
    {
        Console.println("===== testContains =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        Console.println("containsKey(1) = " + map.containsKey(1))
        Console.println("containsKey(5) = " + map.containsKey(5))
        Console.println("containsValue(two) = " + map.containsValue("two"))
        Console.println("containsValue(five) = " + map.containsValue("five"))
    }

    # 测试带初始容量的构造
    static testCapacityConstructor()
    {
        Console.println("===== testCapacityConstructor =====")
        Std.Map<int,int> map = Std.Map<int,int>(8)
        Console.println("capacity = " + map.capacity)
        map.add(1, 10)
        map.add(2, 20)
        Console.println("length = " + map.length)
        Console.println("capacity = " + map.capacity)
    }

    # 测试扩容 (0->4->8->16)
    static testGrow()
    {
        Console.println("===== testGrow =====")
        Std.Map<int,int> map = new()
        Console.println("init capacity = " + map.capacity)
        for i = 0, i < 10, i++
        {
            map.add(i, i * 100)
        }
        Console.println("after 10 adds, length = " + map.length)
        Console.println("after 10 adds, capacity = " + map.capacity)
        Console.println("map[9] = " + map._getItem_(9))
    }

    # 测试 remove（返回旧值，不存在返回 null）
    static testRemove()
    {
        Console.println("===== testRemove =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        map.add(3, "three")
        Console.println("remove(2) = " + map.remove(2))
        Console.println("after remove, length = " + map.length)
        Console.println("containsKey(2) = " + map.containsKey(2))
        Console.println("remove(99) = " + map.remove(99))
        Console.println("after remove missing, length = " + map.length)
    }

    # 测试 removeAt
    static testRemoveAt()
    {
        Console.println("===== testRemoveAt =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        map.add(3, "three")
        map.removeAt(0)
        Console.println("after removeAt(0), length = " + map.length)
        Console.println("containsKey(1) = " + map.containsKey(1))
        map.removeAt(99)
        Console.println("after removeAt(99) invalid, length = " + map.length)
    }

    # 测试 clear + isEmpty / isNotEmpty
    static testClear()
    {
        Console.println("===== testClear =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        Console.println("before clear: isEmpty = " + map.isEmpty + ", isNotEmpty = " + map.isNotEmpty)
        map.clear()
        Console.println("after clear: length = " + map.length)
        Console.println("after clear: isEmpty = " + map.isEmpty)
        Console.println("after clear: map[1] = " + map._getItem_(1))
    }

    # 测试 keys / values
    static testKeysValues()
    {
        Console.println("===== testKeysValues =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        map.add(3, "three")
        List<int> ks = map.keys
        List<string> vs = map.values
        Console.println("keys = " + ks.toString())
        Console.println("values = " + vs.toString())
    }

    # 测试迭代器 reset / moveNext / current
    static testIterator()
    {
        Console.println("===== testIterator =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        map.add(3, "three")
        map.reset()
        while map.moveNext()
        {
            var ent = map.current
            Console.println("entry: " + ent.key + " = " + ent.value)
        }
        # 再迭代一次验证 reset 生效
        map.reset()
        int count = 0
        while map.moveNext()
        {
            count++
        }
        Console.println("second pass count = " + count)
    }

    # 测试 entryAt / indexOfKey / hashId
    static testEntry()
    {
        Console.println("===== testEntry =====")
        Std.Map<int,string> map = new()
        map.add(10, "ten")
        map.add(20, "twenty")
        Console.println("indexOfKey(10) = " + map.indexOfKey(10))
        Console.println("indexOfKey(30) = " + map.indexOfKey(30))
        var ent = map.entryAt(0)
        Console.println("entryAt(0): key = " + ent.key + ", value = " + ent.value + ", hashId = " + ent.hashId)
        Console.println("entryAt(99) = " + map.entryAt(99))
    }

    # 测试 getOrDefault / putIfAbsent
    static testDefaultAndAbsent()
    {
        Console.println("===== testDefaultAndAbsent =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        Console.println("getOrDefault(1, xxx) = " + map.getOrDefault(1, "xxx"))
        Console.println("getOrDefault(9, xxx) = " + map.getOrDefault(9, "xxx"))
        Console.println("putIfAbsent(1, new1) = " + map.putIfAbsent(1, "new1"))
        Console.println("putIfAbsent(9, nine) = " + map.putIfAbsent(9, "nine"))
        Console.println("after putIfAbsent, length = " + map.length)
        Console.println("map[1] = " + map._getItem_(1))
        Console.println("map[9] = " + map._getItem_(9))
    }

    # 测试字符串 key
    static testStringKeys()
    {
        Console.println("===== testStringKeys =====")
        Std.Map<string,int> map = new()
        map.add("apple", 1)
        map.add("banana", 2)
        map.add("cherry", 3)
        Console.println("length = " + map.length)
        Console.println("map[banana] = " + map["banana"].toString() )
        map["banana"] = 20
        Console.println("after overwrite, map[banana] = " + map["banana"])
        Console.println("containsKey(cherry) = " + map.containsKey("cherry"))
    }

    # 测试自定义类作 key（引用 equals 语义）
    static testObjectKeys()
    {
        Console.println("===== testObjectKeys =====")
        KeyClass k1 = new()
        k1.id = 1
        KeyClass k2 = new()
        k2.id = 2
        Map<KeyClass,string> map = new()
        map.add(k1, "first")
        map.add(k2, "second")
        Console.println("length = " + map.length)
        Console.println("map[k1] = " + map._getItem_(k1))
        Console.println("map[k2] = " + map._getItem_(k2))
        Console.println("containsKey(k2) = " + map.containsKey(k2))
        Console.println("remove(k1) = " + map.remove(k1))
        Console.println("after remove, length = " + map.length)
    }

    # 测试 toString（{key=value} Java 风格）
    static testToString()
    {
        Console.println("===== testToString =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        Console.println("map = " + map.toString())
        Std.Map<int,string> empty = new()
        Console.println("empty = " + empty.toString())
    }

    # 测试 toArray / toList
    static testToArrayToList()
    {
        Console.println("===== testToArrayToList =====")
        Std.Map<int,string> map = new()
        map.add(1, "one")
        map.add(2, "two")
        Array<MapEntity<int,string>> arr = map.toArray()
        Console.println("toArray length = " + arr.length)
        for i = 0, i < arr.length, i++
        {
            var ent = arr._getItem_(i)
            Console.println("arr[" + i + "]: " + ent.key + " = " + ent.value)
        }
        List<MapEntity<int,string>> list = map.toList()
        Console.println("toList length = " + list.length)
    }

    # 测试大量数据下的覆盖与查找
    static testManyEntries()
    {
        Console.println("===== testManyEntries =====")
        Std.Map<int,int> map = new()
        for i = 0, i < 100, i++
        {
            map.add(i, i * i)
        }
        Console.println("length = " + map.length)
        Console.println("map[50] = " + map._getItem_(50))
        Console.println("map[99] = " + map._getItem_(99))
        # 覆盖一半（put 语义用下标写入）
        for i = 0, i < 50, i++
        {
            map[i] = -1
        }
        Console.println("after overwrite, length = " + map.length)
        Console.println("map[10] = " + map[10].toString() )
        Console.println("map[60] = " + map._getItem_(60))
        # 删一半
        for i = 50, i < 100, i++
        {
            map.remove(i)
        }
        Console.println("after removes, length = " + map.length)
        Console.println("containsKey(60) = " + map.containsKey(60))
        Console.println("containsKey(10) = " + map.containsKey(10))
    }

    KeyClass
    {
        int id = 0
    }

    static fun()
    {
        Console.println("===== MapTest =====")
        testBasicAdd()
        testInitializer()
        testDuplicateKey()
        testSetGetValue()
        testSubscript()
        testContains()
        testCapacityConstructor()
        testGrow()
        testRemove()
        testRemoveAt()
        testClear()
        testKeysValues()
        testIterator()
        testEntry()
        testDefaultAndAbsent()
        testStringKeys()
        testObjectKeys()
        testToString()
        testToArrayToList()
        testManyEntries()
    }
}
