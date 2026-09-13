import Std;
import Core;

LinkedListTest
{
    # 测试基本 add + length
    static testBasicAdd()
    {
        Console.println("===== testBasicAdd =====")
        Core.LinkedList<int> list = new()
        list.add(10)
        list.add(20)
        list.add(30)
        Console.println("length = " + list.length)
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list._getItem_(i))
        }
    }

    # 测试 addFirst / addLast
    static testAddFirstLast()
    {
        Console.println("===== testAddFirstLast =====")
        Core.LinkedList<int> list = new()
        list.addLast(2)
        list.addLast(3)
        list.addFirst(1)
        list.addFirst(0)
        Console.println("length = " + list.length)
        Console.println("toString = " + list.toString())
        Console.println("first = " + list.first)
        Console.println("last = " + list.last)
    }

    # 测试 addBefore / addAfter（通过索引操作）
    static testAddBeforeAfter()
    {
        Console.println("===== testAddBeforeAfter =====")
        Core.LinkedList<int> list = new()
        list.add(1)
        list.add(3)
        # 在索引1(值为3)之前插入2
        list.addBefore(1, 2)
        # 在索引0(值为1)之后插入15
        list.addAfter(0, 15)
        Console.println("after addBefore/addAfter: " + list.toString())
        Console.println("length = " + list.length)
    }

    # 测试 insert at index
    static testInsert()
    {
        Console.println("===== testInsert =====")
        Core.LinkedList<int> list = new()
        list.add(1)
        list.add(3)
        list.add(4)
        list.insert(1, 2)
        Console.println("after insert(1,2): " + list.toString())
        list.insert(0, 0)
        Console.println("after insert(0,0): " + list.toString())
        list.insert(list.length, 99)
        Console.println("after insert(end,99): " + list.toString())
        list.insert(-1, 100)
        Console.println("after insert(-1,100) noop: " + list.toString())
        list.insert(list.length + 1, 200)
        Console.println("after insert(out,200) noop: " + list.toString())
    }

    # 测试 remove by value
    static testRemove()
    {
        Console.println("===== testRemove =====")
        Core.LinkedList<int> list = new()
        list.add(10)
        list.add(20)
        list.add(30)
        list.add(20)
        Console.println("before remove: " + list.toString())
        list.remove(20)
        Console.println("after remove(20): " + list.toString())
        list.remove(99)
        Console.println("after remove(99) noop: " + list.toString())
    }

    # 测试 removeFirst / removeLast
    static testRemoveFirstLast()
    {
        Console.println("===== testRemoveFirstLast =====")
        Core.LinkedList<int> list = new()
        list.add(1)
        list.add(2)
        list.add(3)
        list.add(4)
        Console.println("before: " + list.toString())
        list.removeFirst()
        Console.println("after removeFirst: " + list.toString())
        list.removeLast()
        Console.println("after removeLast: " + list.toString())
        # 删到空
        list.removeFirst()
        list.removeFirst()
        Console.println("after remove all, length = " + list.length)
        list.removeFirst()
        Console.println("removeFirst on empty, length = " + list.length)
        list.removeLast()
        Console.println("removeLast on empty, length = " + list.length)
    }

    # 测试 removeAt
    static testRemoveAt()
    {
        Console.println("===== testRemoveAt =====")
        Core.LinkedList<int> list = new()
        list.add(10)
        list.add(20)
        list.add(30)
        list.add(40)
        list.add(50)
        Console.println("before: " + list.toString())
        list.removeAt(2)
        Console.println("after removeAt(2): " + list.toString())
        list.removeAt(0)
        Console.println("after removeAt(0): " + list.toString())
        list.removeAt(list.length - 1)
        Console.println("after removeAt(last): " + list.toString())
        list.removeAt(-1)
        Console.println("after removeAt(-1) noop: " + list.toString())
        list.removeAt(99)
        Console.println("after removeAt(99) noop: " + list.toString())
    }

    # 测试 clear
    static testClear()
    {
        Console.println("===== testClear =====")
        Core.LinkedList<int> list = new()
        list.add(1)
        list.add(2)
        list.add(3)
        Console.println("before clear, length = " + list.length)
        Console.println("before clear: " + list.toString())
        list.clear()
        Console.println("after clear, length = " + list.length)
        Console.println("after clear, isEmpty = " + list.isEmpty)
        Console.println("after clear, first = " + list.first)
        Console.println("after clear, last = " + list.last)
    }

    # 测试 indexOf / lastIndexOf / contains
    static testIndexOfContains()
    {
        Console.println("===== testIndexOfContains =====")
        Core.LinkedList<int> list = new()
        list.add(10)
        list.add(20)
        list.add(30)
        list.add(20)
        Console.println("indexOf(20) = " + list.indexOf(20))
        Console.println("lastIndexOf(20) = " + list.lastIndexOf(20))
        Console.println("indexOf(99) = " + list.indexOf(99))
        Console.println("lastIndexOf(99) = " + list.lastIndexOf(99))
        Console.println("contains(30) = " + list.contains(30))
        Console.println("contains(99) = " + list.contains(99))

        Core.LinkedList<string> slist = new()
        slist.add("aa")
        slist.add("bb")
        Console.println("slist.indexOf(\"bb\") = " + slist.indexOf("bb"))
        Console.println("slist.contains(\"cc\") = " + slist.contains("cc"))
    }

    # 测试 isEmpty / isNotEmpty
    static testIsEmpty()
    {
        Console.println("===== testIsEmpty =====")
        Core.LinkedList<int> list = new()
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
        Core.LinkedList<int> list = new()
        Console.println("empty: first = " + list.first + ", last = " + list.last)
        list.add(10)
        Console.println("single: first = " + list.first + ", last = " + list.last)
        list.add(20)
        list.add(30)
        Console.println("multi: first = " + list.first + ", last = " + list.last)
        list.removeLast()
        Console.println("after removeLast: first = " + list.first + ", last = " + list.last)
    }

    # 测试索引器 get/set
    static testIndexer()
    {
        Console.println("===== testIndexer =====")
        Core.LinkedList<int> list = new()
        list.add(100)
        list.add(200)
        list.add(300)
        Console.println("list[0] = " + list._getItem_(0))
        Console.println("list[1] = " + list._getItem_(1))
        Console.println("list[2] = " + list._getItem_(2))
        list._setItem_(1, 999)
        Console.println("after setItem(1,999): list[1] = " + list._getItem_(1))
        Console.println("toString = " + list.toString())
    }

    # 测试 toString
    static testToString()
    {
        Console.println("===== testToString =====")
        Core.LinkedList<int> list = new()
        Console.println("empty toString: " + list.toString())
        list.add(1)
        Console.println("single toString: " + list.toString())
        list.add(2)
        list.add(3)
        Console.println("multi toString: " + list.toString())
    }

    # 测试 string 类型 LinkedList
    static testStringList()
    {
        Console.println("===== testStringList =====")
        Core.LinkedList<string> list = new()
        list.add("hello")
        list.add("world")
        list.add("!")
        Console.println("length = " + list.length)
        Console.println("toString = " + list.toString())
        list.remove("world")
        Console.println("after remove(\"world\"): " + list.toString())
        Console.println("contains(\"hello\") = " + list.contains("hello"))
    }

    # 测试 toArray
    static testToArray()
    {
        Console.println("===== testToArray =====")
        Core.LinkedList<int> list = new()
        list.add(10)
        list.add(20)
        list.add(30)
        Array<int> arr = list.toArray()
        Console.println("arr.length = " + arr.length)
        for i = 0, i < arr.length, i++
        {
            Console.println("arr[" + i + "] = " + arr[i])
        }
        # 空列表 toArray
        Core.LinkedList<int> empty = new()
        Array<int> emptyArr = empty.toArray()
        Console.println("empty arr.length = " + emptyArr.length)
    }

    # 测试迭代器
    static testIterator()
    {
        Console.println("===== testIterator =====")
        Core.LinkedList<int> list = new()
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
        # 再次迭代
        list.reset()
        Console.println("second iteration:")
        while list.moveNext()
        {
            Console.println("iter: " + list.current)
        }
    }

    # 测试单元素列表各种操作
    static testSingleElement()
    {
        Console.println("===== testSingleElement =====")
        Core.LinkedList<int> list = new()
        list.add(42)
        Console.println("single: first = " + list.first + ", last = " + list.last)
        list.removeFirst()
        Console.println("after removeFirst: length = " + list.length + ", isEmpty = " + list.isEmpty)

        list.add(99)
        list.removeLast()
        Console.println("after add+removeLast: length = " + list.length + ", isEmpty = " + list.isEmpty)

        list.add(77)
        list.remove(77)
        Console.println("after add+remove(77): length = " + list.length + ", isEmpty = " + list.isEmpty)

        list.add(55)
        list.removeAt(0)
        Console.println("after add+removeAt(0): length = " + list.length + ", isEmpty = " + list.isEmpty)
    }

    # 测试 object 类型 LinkedList
    static testObjectList()
    {
        Console.println("===== testObjectList =====")
        Core.LinkedList<object> list = new()
        list.add(10)
        list.add("hello")
        list.add(3.14)
        Console.println("toString = " + list.toString())
        Console.println("length = " + list.length)
        for i = 0, i < list.length, i++
        {
            Console.println("list[" + i + "] = " + list._getItem_(i).toString())
        }
    }

    # 测试连续添加和删除的交替操作
    static testMixedOperations()
    {
        Console.println("===== testMixedOperations =====")
        Core.LinkedList<int> list = new()
        list.add(1)
        list.add(2)
        list.add(3)
        list.addFirst(0)
        Console.println("after adds: " + list.toString())
        list.removeAt(1)
        Console.println("after removeAt(1): " + list.toString())
        list.insert(2, 99)
        Console.println("after insert(2,99): " + list.toString())
        list.removeLast()
        Console.println("after removeLast: " + list.toString())
        list.addLast(88)
        Console.println("after addLast(88): " + list.toString())
        list.removeFirst()
        Console.println("after removeFirst: " + list.toString())
        Console.println("final length = " + list.length)
        Console.println("final first = " + list.first + ", last = " + list.last)
    }

    # 测试空列表的各种边界操作
    static testEmptyListEdgeCases()
    {
        Console.println("===== testEmptyListEdgeCases =====")
        Core.LinkedList<int> list = new()
        Console.println("isEmpty = " + list.isEmpty)
        Console.println("length = " + list.length)
        Console.println("first = " + list.first)
        Console.println("last = " + list.last)
        list.remove(10)
        Console.println("after remove on empty: length = " + list.length)
        list.removeAt(0)
        Console.println("after removeAt on empty: length = " + list.length)
        list.removeFirst()
        Console.println("after removeFirst on empty: length = " + list.length)
        list.removeLast()
        Console.println("after removeLast on empty: length = " + list.length)
        Console.println("indexOf(10) = " + list.indexOf(10))
        Console.println("contains(10) = " + list.contains(10))
        Console.println("toString = " + list.toString())
    }

    # 测试重复元素
    static testDuplicateElements()
    {
        Console.println("===== testDuplicateElements =====")
        Core.LinkedList<int> list = new()
        list.add(5)
        list.add(5)
        list.add(5)
        Console.println("after 3x add(5): " + list.toString())
        Console.println("indexOf(5) = " + list.indexOf(5))
        Console.println("lastIndexOf(5) = " + list.lastIndexOf(5))
        list.remove(5)
        Console.println("after remove(5): " + list.toString() + ", length = " + list.length)
        list.remove(5)
        list.remove(5)
        Console.println("after 3x remove(5): length = " + list.length)
    }

    # 测试大批量添加
    static testBulkAdd()
    {
        Console.println("===== testBulkAdd =====")
        Core.LinkedList<int> list = new()
        for i = 0, i < 100, i++
        {
            list.add(i)
        }
        Console.println("after 100 adds, length = " + list.length)
        Console.println("first = " + list.first + ", last = " + list.last)
        # 验证几个关键点
        Console.println("list[0] = " + list._getItem_(0))
        Console.println("list[50] = " + list._getItem_(50))
        Console.println("list[99] = " + list._getItem_(99))
        # 删一半
        for i = 0, i < 50, i++
        {
            list.removeFirst()
        }
        Console.println("after 50x removeFirst, length = " + list.length)
        Console.println("first = " + list.first + ", last = " + list.last)
    }

    # 测试 addBefore/addAfter 在头尾的边界
    static testAddBeforeAfterEdge()
    {
        Console.println("===== testAddBeforeAfterEdge =====")
        Core.LinkedList<int> list = new()
        list.add(2)
        # 在索引0之前插入 -> 新元素变成头
        list.addBefore(0, 1)
        Console.println("after addBefore(0,1): " + list.toString())
        Console.println("first = " + list.first)
        # 在最后一个索引之后插入 -> 新元素变成尾
        list.addAfter(list.length - 1, 3)
        Console.println("after addAfter(last,3): " + list.toString())
        Console.println("last = " + list.last)
        # addBefore 越界应该无操作
        list.addBefore(-1, 99)
        Console.println("after addBefore(-1,99) noop: " + list.toString())
        list.addBefore(99, 99)
        Console.println("after addBefore(99,99) noop: " + list.toString())
        # addAfter 越界应该无操作
        list.addAfter(-1, 99)
        Console.println("after addAfter(-1,99) noop: " + list.toString())
        list.addAfter(99, 99)
        Console.println("after addAfter(99,99) noop: " + list.toString())
    }

    # 入口函数
    static fun()
    {
        testBasicAdd()
        testAddFirstLast()
        testAddBeforeAfter()
        testInsert()
        testRemove()
        testRemoveFirstLast()
        testRemoveAt()
        testClear()
        testIndexOfContains()
        testIsEmpty()
        testFirstLast()
        testIndexer()
        testToString()
        testStringList()
        testToArray()
        testIterator()
        testSingleElement()
        testObjectList()
        testMixedOperations()
        testEmptyListEdgeCases()
        testDuplicateElements()
        testBulkAdd()
        testAddBeforeAfterEdge()
    }
}
