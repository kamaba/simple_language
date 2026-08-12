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
        list.add(0)
        list.add(0)
        list.add(0)
        list.fill(99)
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list.$i.toString() )
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
        Console.println("list.getValue(2) = " + list.[2]?.toString() )
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
    }
}
