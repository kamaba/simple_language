import Std;
import Core;

HashSetTest
{
    # 测试基本 add + 重复去重 + length / isEmpty
    static testBasicAdd()
    {
        Console.println("===== testBasicAdd =====")
        Core.Set<int> s = new()
        Console.println("isEmpty = " + s.isEmpty)
        Console.println("add(1) = " + s.add(1))
        Console.println("add(2) = " + s.add(2))
        Console.println("add(2) dup = " + s.add(2))   # 重复返回 false
        Console.println("add(3) = " + s.add(3))
        Console.println("length = " + s.length)
        Console.println("isEmpty = " + s.isEmpty)
        Console.println("toString = " + s.toString())
    }

    # 测试指定容量构造 + 从数组构造
    static testConstructors()
    {
        Console.println("===== testConstructors =====")
        Core.Set<int> s1 = Set<int>(8)
        Console.println("capacity(s1) = " + s1.capacity)

        Array<int> arr = Array<int>(3)
        arr[0] = 10
        arr[1] = 20
        arr[2] = 20        # 重复，构造时去重
        Core.Set<int> s2 = Set<int>(arr)
        Console.println("s2.length = " + s2.length)        # 2
        Console.println("s2.toString = " + s2.toString())
    }

    # 测试 contains / addRange
    static testContainsAndAddRange()
    {
        Console.println("===== testContainsAndAddRange =====")
        Core.Set<int> s = new()
        s.add(1)
        s.add(2)
        Console.println("contains(1) = " + s.contains(1))
        Console.println("contains(9) = " + s.contains(9))
        Console.println("contains(null) = " + s.contains(null))
        Console.println("add(null) = " + s.add(null))      # null 不存储

        Array<int> more = Array<int>(3)
        more[0] = 3
        more[1] = 4
        more[2] = 1        # 已存在，addRange 会跳过
        s.addRange(more)
        Console.println("after addRange, length = " + s.length)  # 4
        Console.println("s.toString = " + s.toString())
    }

    # 测试 remove / clear
    static testRemoveAndClear()
    {
        Console.println("===== testRemoveAndClear =====")
        Core.Set<int> s = new()
        s.add(1)
        s.add(2)
        s.add(3)
        Console.println("remove(2) = " + s.remove(2))   # true
        Console.println("remove(2) again = " + s.remove(2))  # false
        Console.println("remove(null) = " + s.remove(null))  # false
        Console.println("after removes, length = " + s.length)  # 2
        Console.println("contains(2) = " + s.contains(2))
        s.clear()
        Console.println("after clear, length = " + s.length)
        Console.println("after clear, isEmpty = " + s.isEmpty)
    }

    # 测试容量增长（自动扩容）
    static testGrow()
    {
        Console.println("===== testGrow =====")
        Core.Set<int> s = new()
        Console.println("initial capacity = " + s.capacity)   # 0
        s.add(1)
        Console.println("after 1 add, capacity = " + s.capacity)  # 4
        for i = 2, i <= 8, i++
        {
            s.add(i)
        }
        Console.println("after 8 adds, capacity = " + s.capacity)  # 8
        for i = 9, i <= 16, i++
        {
            s.add(i)
        }
        Console.println("after 16 adds, capacity = " + s.capacity) # 16
        Console.println("length = " + s.length)
        Console.println("setEquals(toArray-based) check toString = " + s.toString())
    }

    # 测试 ensureCapacity
    static testEnsureCapacity()
    {
        Console.println("===== testEnsureCapacity =====")
        Core.Set<int> s = new()
        s.ensureCapacity(100)
        Console.println("after ensureCapacity(100), capacity = " + s.capacity)  # 100
        s.add(1)
        Console.println("after 1 add, capacity = " + s.capacity)  # 仍为 100（不缩容）
    }

    # 测试修改型集合运算 unionWith / intersectWith / exceptWith / symmetricExceptWith
    static testModifyOps()
    {
        Console.println("===== testModifyOps =====")
        Core.Set<int> a = Set<int>({1, 2, 3})
        Core.Set<int> b = Set<int>({2, 3, 4})

        Core.Set<int> u = Set<int>({1, 2, 3})
        u.unionWith(b)
        Console.println("unionWith = " + u.toString())       # {1,2,3,4}

        Core.Set<int> i = Set<int>({1, 2, 3})
        i.intersectWith(b)
        Console.println("intersectWith = " + i.toString())   # {2,3}

        Core.Set<int> e = Set<int>({1, 2, 3})
        e.exceptWith(b)
        Console.println("exceptWith = " + e.toString())      # {1}

        Core.Set<int> se = Set<int>({1, 2, 3})
        se.symmetricExceptWith(b)
        Console.println("symmetricExceptWith = " + se.toString())  # {1,4}
    }

    # 测试非修改型集合运算 union / intersection / difference / symmetricDifference / copy
    static testQueryOps()
    {
        Console.println("===== testQueryOps =====")
        Core.Set<int> a = Set<int>({1, 2, 3})
        Core.Set<int> b = Set<int>({2, 3, 4})
        Console.println("union = " + a.union(b).toString())              # {1,2,3,4}
        Console.println("intersection = " + a.intersection(b).toString()) # {2,3}
        Console.println("difference = " + a.difference(b).toString())     # {1}
        Console.println("symmetricDifference = " + a.symmetricDifference(b).toString()) # {1,4}
        Core.Set<int> c = a.copy()
        Console.println("copy = " + c.toString())
        Console.println("a unchanged after ops = " + a.toString())       # {1,2,3}
    }

    # 测试判断型集合运算
    static testPredicateOps()
    {
        Console.println("===== testPredicateOps =====")
        Core.Set<int> empty = new()
        Core.Set<int> a = Set<int>({1, 2, 3})
        Core.Set<int> b = Set<int>({2, 3})
        Core.Set<int> c = Set<int>({1, 2, 3})
        Core.Set<int> d = Set<int>({3, 4, 5})

        Console.println("b.isSubsetOf(a) = " + b.isSubsetOf(a))          # true
        Console.println("a.isSupersetOf(b) = " + a.isSupersetOf(b))      # true
        Console.println("empty.isSubsetOf(a) = " + empty.isSubsetOf(a)) # true
        Console.println("a.isSupersetOf(empty) = " + a.isSupersetOf(empty)) # true
        Console.println("b.isProperSubsetOf(a) = " + b.isProperSubsetOf(a))  # true
        Console.println("a.isProperSubsetOf(c) = " + a.isProperSubsetOf(c))  # false（长度相等）
        Console.println("a.isProperSupersetOf(b) = " + a.isProperSupersetOf(b)) # true
        Console.println("a.overlaps(d) = " + a.overlaps(d))              # true（共有 3）
        Console.println("empty.overlaps(a) = " + empty.overlaps(a))     # false
        Console.println("a.setEquals(c) = " + a.setEquals(c))           # true
        Console.println("a.setEquals(b) = " + a.setEquals(b))           # false
    }

    # 测试 toArray / toList / first / last
    static testConvert()
    {
        Console.println("===== testConvert =====")
        Core.Set<int> s = Set<int>({10, 20, 30})
        Array<int> arr = s.toArray()
        Console.println("toArray.length = " + arr.length)
        for i = 0, i < arr.length, i++
        {
            Console.println("arr[" + i + "] = " + arr[i])
        }
        Core.List<int> list = s.toList()
        Console.println("toList.length = " + list.length)
        Console.println("first = " + s.first)
        Console.println("last = " + s.last)
    }

    # 测试迭代器 / foreach
    static testIterator()
    {
        Console.println("===== testIterator =====")
        Core.Set<string> s = Set<string>({"a", "b", "c"})
        int sum = 0
        for item in s
        {
            Console.println("foreach item = " + item)
            sum++
        }
        Console.println("iterated count = " + sum)
    }

    # 测试字符串元素集合
    static testStringElements()
    {
        Console.println("===== testStringElements =====")
        Core.Set<string> s = new()
        s.add("apple")
        s.add("banana")
        s.add("apple")     # 去重
        Console.println("length = " + s.length)
        Console.println("contains(banana) = " + s.contains("banana"))
        Console.println("toString = " + s.toString())
    }

    # 测试空集合边界行为
    static testEmptyEdge()
    {
        Console.println("===== testEmptyEdge =====")
        Core.Set<int> s = new()
        Console.println("toArray.length = " + s.toArray().length)
        Console.println("first = " + s.first)
        Console.println("last = " + s.last)
        Console.println("toString = " + s.toString())
        Console.println("setEquals(empty) = " + s.setEquals(new()))
    }

    static fun()
    {
        Console.println("===== HashSetTest =====")
        testBasicAdd()
        testConstructors()
        testContainsAndAddRange()
        testRemoveAndClear()
        testGrow()
        testEnsureCapacity()
        testModifyOps()
        testQueryOps()
        testPredicateOps()
        testConvert()
        testIterator()
        testStringElements()
        testEmptyEdge()
    }
}
