import Std;

ListTest
{
    # 测试基本 add + length
    static testBasicAdd()
    {
        Console.println("===== testBasicAdd =====")
        Std.List<int> list = new()
        list.add(10)
        list.add(20)
        list.add(30)
        Console.println("length = " + list.length)
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list._getItem_(i))
        }
    }
    static testBasicInitializer()
    { 
         Console.println("===== testBasicInitializer =====")        
        list = List<int>(4){123,456,789}
         for i = 0, i < list.length, i++
         {
            Console.println("list[" + i + "] = " + list._getItem_(i))
         }     
    }

    # 测试带初始容量的构造
    static testCapacityConstructor()
    {
        Console.println("===== testCapacityConstructor =====")
        Std.List<int> list = Std.List<int>(8)
        Console.println("capacity = " + list.capacity)
        list.add(1)
        list.add(2)
        Console.println("length = " + list.length)
        Console.println("capacity = " + list.capacity)
    }

    # 测试扩容 (0->4->8->16)
    static testGrow()
    {
        Console.println("===== testGrow =====")
        Std.List<int> list = new()
        Console.println("init capacity = " + list.capacity)
        for i = 0, i < 10, i++
        {
            list.add(i * 5)
        }
        Console.println("after 10 adds, length = " + list.length)
        Console.println("after 10 adds, capacity = " + list.capacity)
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list._getItem_(i))
        }
    }

    # 测试 insert
    static testInsert()
    {
        Console.println("===== testInsert =====")
        List<int> list = new()
        list.add(1)
        list.add(3)
        list.add(4)
        list.insert(1, 2)
        Console.println("after insert at 1, length = " + list.length)
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list[i].toString() )
        }
    }

    # 测试 removeAt
    static testRemoveAt()
    {
        Console.println("===== testRemoveAt =====")
        Std.List<int> list = new(6)
        list.add(10)
        list.add(20)
        list.add(30)
        list.add(40)
        list.removeAt(1)
        list.remove(40 );
        Console.println("after removeAt(1), length = " + list.length)
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list._getItem_(i))
        }
    }

    # 测试 clear
    static testClear()
    {
        Console.println("===== testClear =====")
        List<int> list = Std.List<int>()
        list.add(1)
        list.add(2)
        list.add(3)
        Console.println("before clear, length = " + list.length + "capacity = " + list.capacity )
        list.clear()
        Console.println("after clear, length = " + list.length + "capacity = " + list.capacity)
    }

    # 测试 fill
    static testFill()
    {
        Console.println("===== testFill =====")
        List<int> list = Std.List<int>(5)
        Console.println("===== testFill capacity=====" + list.capacity )
        list.add(0)
        list.add(0)
        list.add(0)
        
        list.fill(99)
        for i = 0, i < list.length, i++
        {
            Console.println("list1[" + i + "] = " + list._getItem_(i) )
        }


        list.fill(66,1)
        for i = 0, i < list.length, i++
        {
            Console.println("list2[" + i + "] = " + list._getItem_(i) )
        }
        


        list.fill(33,2,3)
        for i = 0, i < list.length, i++
        {
            Console.println("list3[" + i + "] = " + list._getItem_(i) )
        }
    }

    # 测试 ensureCapacity
    static testEnsureCapacity()
    {
        Console.println("===== testEnsureCapacity =====")
        Std.List<int> list = new()
        Console.println("init capacity = " + list.capacity)
        list.ensureCapacity(20)
        Console.println("after ensureCapacity(20), capacity = " + list.capacity)
    }

    # 测试 setValue + getValue
    static testSetGetValue()
    {
        Console.println("===== testSetGetValue =====")
        List<int> list = Std.List<int>(10)
        list.add(100)
        list.add(200)
        list._setItem_(0, 999)
        list[4] = 888
        list.$3 = 777
        Console.println("list.getValue(0) = " + list._getItem_(0)?.toString() )
        Console.println("list.getValue(1) = " + list._getItem_(1)?.toString() )
        Console.println("list.getValue(2) = " + list._getItem_(2)?.toString() )
        Console.println("list.getValue(3) = " + list[3].toString() )
        Console.println("list.getValue(4) = " + list.$4.toString() )
    }

    # 测试迭代器 (IIterable/IIterator)
    static testIterator()
    {
        Console.println("===== testIterator =====")
        Std.List<int> list = new()
        list.add(1)
        list.add(2)
        list.add(3)
        list.add(4)
        list.add(5)
        list.reset()
        while list.moveNext()
        {
            Console.println("iter: " + list.current)
        }
    }

    # 测试 toString
    static testToString()
    {
        Console.println("===== testToString =====")
        Std.List<int> list = new()
        list.add(1)
        list.add(2)
        list.add(3)
        Console.println("toString: " + list.toString())
    }

    # 测试 string 类型 List
    static testStringList()
    {
        Console.println("===== testStringList =====")
        Std.List<string> list = new()
        list.add("hello")
        list.add("world")
        list.add("!")
        Console.println("length = " + list.length)
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list._getItem_(i))
        }
    }

    # 测试 toArray
    static testToArray()
    {
        Console.println("===== testToArray =====")
        Std.List<int> list = new()
        list.add(10)
        list.add(20)
        list.add(30)
        Array<int> arr = list.toArray()
        Console.println("arr.length = " + arr.length)
        for i = 0, i < arr.length, i++
        {
            Console.println("arr[" + i + "] = " + arr[i] )
        }
    }
    # 测试 object
    static testForObject()
    {
        Console.println("===== testToArray =====")
        Std.List<object> list = new()
        list.add(10)
        list.add("aaa")
        list.add([1,2,3])
       
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list[i].toString() )
        }
    }

    # 测试 isEmpty / isNotEmpty
    static testIsEmpty()
    {
        Console.println("===== testIsEmpty =====")
        Std.List<int> list = new()
        Console.println("empty: isEmpty = " + list.isEmpty + ", isNotEmpty = " + list.isNotEmpty)
        list.add(1)
        Console.println("after add: isEmpty = " + list.isEmpty + ", isNotEmpty = " + list.isNotEmpty)
        list.clear()
        Console.println("after clear: isEmpty = " + list.isEmpty + ", isNotEmpty = " + list.isNotEmpty)
    }

    # 测试 first / last
    static testFirstLast()
    {
        Console.println("===== testFirstLast =====")
        Std.List<int> list = new()
        Console.println("empty: first = " + list.first + ", last = " + list.last)
        list.add(10)
        list.add(20)
        list.add(30)
        Console.println("first = " + list.first)
        Console.println("last = " + list.last)
        list.removeAt(list.length - 1)
        Console.println("after remove last: first = " + list.first + ", last = " + list.last)
    }

    # 测试 indexOf / lastIndexOf / contains
    static testIndexOfContains()
    {
        Console.println("===== testIndexOfContains =====")
        Std.List<int> list = new()
        list.add(10)
        list.add(20)
        list.add(30)
        list.add(20)
        Console.println("indexOf(20) = " + list.indexOf(20))
        Console.println("lastIndexOf(20) = " + list.lastIndexOf(20))
        Console.println("indexOf(99) = " + list.indexOf(99))
        Console.println("contains(30) = " + list.contains(30))
        Console.println("contains(99) = " + list.contains(99))

        Std.List<string> slist = new()
        slist.add("aa")
        slist.add("bb")
        Console.println("slist.indexOf(\"bb\") = " + slist.indexOf("bb"))
        Console.println("slist.contains(\"cc\") = " + slist.contains("cc"))
    }

    # 测试 addRange
    static testAddRange()
    {
        Console.println("===== testAddRange =====")
        Std.List<int> list = new()
        list.add(1)
        list.add(2)
        Std.List<int> other = new()
        other.add(3)
        other.add(4)
        other.add(5)
        list.addRange(other)
        Console.println("after addRange, length = " + list.length)
        Console.println("list = " + list.toString())
        Std.List<int> nullList = null
        list.addRange(nullList)
        Console.println("after addRange(null), length = " + list.length)
    }

    # 测试 insertRange
    static testInsertRange()
    {
        Console.println("===== testInsertRange =====")
        Std.List<int> list = new()
        list.add(1)
        list.add(5)
        Std.List<int> other = new()
        other.add(2)
        other.add(3)
        other.add(4)
        list.insertRange(1, other)
        Console.println("after insertRange(1), length = " + list.length)
        Console.println("list = " + list.toString())
        list.insertRange(-1, other)
        Console.println("after insertRange(-1), length = " + list.length)
    }

    # 测试 removeRange
    static testRemoveRange()
    {
        Console.println("===== testRemoveRange =====")
        Std.List<int> list = new()
        for i = 0, i < 6, i++
        {
            list.add(i)
        }
        Console.println("before removeRange: " + list.toString())
        list.removeRange(2, 2)
        Console.println("after removeRange(2,2): " + list.toString() + ", length = " + list.length)
        list.removeRange(1, 100)
        Console.println("after removeRange(1,100) clamp: " + list.toString() + ", length = " + list.length)
        list.removeRange(0, 0)
        Console.println("after removeRange(0,0) noop: " + list.toString() + ", length = " + list.length)
    }

    # 测试 reverse
    static testReverse()
    {
        Console.println("===== testReverse =====")
        Std.List<int> list = new()
        for i = 0, i < 5, i++
        {
            list.add(i)
        }
        Console.println("before reverse: " + list.toString())
        list.reverse()
        Console.println("after reverse: " + list.toString())
        Std.List<int> empty = new()
        empty.reverse()
        Console.println("empty reverse ok, length = " + empty.length)
    }

    # 测试 getRange
    static testGetRange()
    {
        Console.println("===== testGetRange =====")
        Std.List<int> list = new()
        for i = 0, i < 6, i++
        {
            list.add(i * 10)
        }
        Std.List<int> sub = list.getRange(2, 2)
        Console.println("getRange(2,2) = " + sub.toString())
        Std.List<int> clamped = list.getRange(4, 100)
        Console.println("getRange(4,100) clamp = " + clamped.toString())
        Std.List<int> bad = list.getRange(99, 2)
        if bad == null
        {
            Console.println("getRange(99,2) = null")
        }
        else
        {
            Console.println("getRange(99,2) = " + bad.toString())
        }
    }

    # 测试三个新构造函数：从数组 / 从数组区间 / 从 Range
    static testConstructors()
    {
        Console.println("===== testConstructors =====")
        # 1. 从数组构造
        Array<int> arr = Array<int>(3)
        arr._setItem_(0, 1)
        arr._setItem_(1, 2)
        arr._setItem_(2, 3)
        Std.List<int> fromArray = Std.List<int>(arr)
        Console.println("fromArray: " + fromArray.toString() + ", length = " + fromArray.length)
        # 源数组后续修改不影响已拷贝的 list
        arr._setItem_(0, 99)
        Console.println("after src change: " + fromArray.toString())
        # 2. 从数组区间构造
        Std.List<int> slice = Std.List<int>(arr, 1, 1)
        Console.println("slice(1,1): " + slice.toString() + ", length = " + slice.length)
        Std.List<int> sliceClamp = Std.List<int>(arr, 2, 100)
        Console.println("slice(2,100) clamp: " + sliceClamp.toString() + ", length = " + sliceClamp.length)
        Std.List<int> sliceEmpty = Std.List<int>(arr, 5, 2)
        Console.println("slice(5,2) empty: length = " + sliceEmpty.length)
        # 3. 从 Range 构造
        Std.List<int> fromRange = Std.List<int>(Range<int>(0, 5))
        Console.println("fromRange(0,5): " + fromRange.toString() + ", length = " + fromRange.length)
        Std.List<int> fromRangeStep = Std.List<int>(Range<int>(10, 0, -2))
        Console.println("fromRange(10,0,-2): " + fromRangeStep.toString() + ", length = " + fromRangeStep.length)
        # 构造后可继续 add（验证容量状态正确）
        fromArray.add(4)
        Console.println("after add: " + fromArray.toString() + ", capacity = " + fromArray.capacity)
    }
    public void testInitListByOtherClass()
    {
        Console.println("===== testInitListByOtherClass =====")
    }

    static fun()
    {
        testBasicAdd()
        testBasicInitializer();
        testCapacityConstructor()
        testGrow()
        testInsert()
        testRemoveAt()
        testClear()
        testFill()
        testEnsureCapacity()
        testSetGetValue()
        testIterator()
        testToString()
        testStringList()
        testToArray()
        testForObject();
        testIsEmpty()
        testFirstLast()
        testIndexOfContains()
        testAddRange()
        testInsertRange()
        testRemoveRange()
        testReverse()
        testGetRange()
        testConstructors()
    }
}
