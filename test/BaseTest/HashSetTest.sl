import Std;
import Core;

HashSetTest
{
    # 测试基本 add + 重复去重 + length / isEmpty
    static testBasicAdd()
    {
        global.println("===== testBasicAdd =====")
        Core.HashSet<int> s = new()
        global.println("isEmpty = " + s.isEmpty)
        global.println("add(1) = " + s.add(1))
        global.println("add(2) = " + s.add(2))
        global.println("add(2) dup = " + s.add(2))   # 重复返回 false
        global.println("add(3) = " + s.add(3))
        global.println("length = " + s.length)
        global.println("isEmpty = " + s.isEmpty)
        global.println("toString = " + s.toString())
    }

    # 测试指定容量构造 + 从数组构造
    static testConstructors()
    {
        global.println("===== testConstructors =====")
        Core.HashSet<int> s1 = HashSet<int>(8)
        global.println("capacity(s1) = " + s1.capacity)

        Array<int> arr = Array<int>(3)
        arr[0] = 10
        arr[1] = 20
        arr[2] = 20        # 重复，构造时去重
        Core.HashSet<int> s2 = HashSet<int>(arr)
        global.println("s2.length = " + s2.length)        # 2
        global.println("s2.toString = " + s2.toString())
    }

    # 测试 contains / addRange
    static testContainsAndAddRange()
    {
        global.println("===== testContainsAndAddRange =====")
        Core.HashSet<int> s = new()
        s.add(1)
        s.add(2)
        global.println("contains(1) = " + s.contains(1))
        global.println("contains(9) = " + s.contains(9))

        Array<int> more = Array<int>(3)
        more[0] = 3
        more[1] = 4
        more[2] = 1        # 已存在，addRange 会跳过
        s.addRange(more)
        global.println("after addRange, length = " + s.length)  # 4
        global.println("s.toString = " + s.toString())
    }

    # 测试 remove / clear
    static testRemoveAndClear()
    {
        global.println("===== testRemoveAndClear =====")
        Core.HashSet<int> s = new()
        s.add(1)
        s.add(2)
        s.add(3)
        global.println("remove(2) = " + s.remove(2))   # true
        global.println("remove(2) again = " + s.remove(2))  # false
        global.println("after removes, length = " + s.length)  # 2
        global.println("contains(2) = " + s.contains(2))
        s.clear()
        global.println("after clear, length = " + s.length)
        global.println("after clear, isEmpty = " + s.isEmpty)
    }

    # 测试容量增长（自动扩容）
    static testGrow()
    {
        global.println("===== testGrow =====")
        Core.HashSet<int> s = new()
        global.println("initial capacity = " + s.capacity)   # 0
        s.add(1)
        global.println("after 1 add, capacity = " + s.capacity)  # 4
        for i = 2, i <= 8, i++
        {
            s.add(i)
        }
        global.println("after 8 adds, capacity = " + s.capacity)  # 8
        for i = 9, i <= 16, i++
        {
            s.add(i)
        }
        global.println("after 16 adds, capacity = " + s.capacity) # 16
        global.println("length = " + s.length)
        global.println("setEquals(toArray-based) check toString = " + s.toString())
    }

    # 测试 ensureCapacity
    static testEnsureCapacity()
    {
        global.println("===== testEnsureCapacity =====")
        Core.HashSet<int> s = new()
        s.ensureCapacity(100)
        global.println("after ensureCapacity(100), capacity = " + s.capacity)  # 100
        s.add(1)
        global.println("after 1 add, capacity = " + s.capacity)  # 仍为 100（不缩容）
    }

    # 测试修改型集合运算 unionWith / intersectWith / exceptWith / symmetricExceptWith
    static testModifyOps()
    {
        global.println("===== testModifyOps =====")
        Core.HashSet<int> a = HashSet<int>([1, 2, 3])
        Core.HashSet<int> b = HashSet<int>([2, 3, 4])

        Core.HashSet<int> u = HashSet<int>([1, 2, 3])
        u.unionWith(b)
        global.println("unionWith = " + u.toString())       # {1,2,3,4}

        Core.HashSet<int> i = HashSet<int>([1, 2, 3])
        i.intersectWith(b)
        global.println("intersectWith = " + i.toString())   # {2,3}

        Core.HashSet<int> e = HashSet<int>([1, 2, 3])
        e.exceptWith(b)
        global.println("exceptWith = " + e.toString())      # {1}

        Core.HashSet<int> se = HashSet<int>([1, 2, 3])
        se.symmetricExceptWith(b)
        global.println("symmetricExceptWith = " + se.toString())  # {1,4}
    }

    # 测试非修改型集合运算 union / intersection / difference / symmetricDifference / copy
    static testQueryOps()
    {
        global.println("===== testQueryOps =====")
        Core.HashSet<int> a = HashSet<int>([1, 2, 3])
        Core.HashSet<int> b = HashSet<int>([2, 3, 4])
        global.println("union = " + a.union(b).toString())              # {1,2,3,4}
        global.println("intersection = " + a.intersection(b).toString()) # {2,3}
        global.println("difference = " + a.difference(b).toString())     # {1}
        global.println("symmetricDifference = " + a.symmetricDifference(b).toString()) # {1,4}
        Core.HashSet<int> c = a.copy()
        global.println("copy = " + c.toString())
        global.println("a unchanged after ops = " + a.toString())       # {1,2,3}
    }

    # 测试判断型集合运算
    static testPredicateOps()
    {
        global.println("===== testPredicateOps =====")
        Core.HashSet<int> empty = new()
        Core.HashSet<int> a = HashSet<int>([1, 2, 3])
        Core.HashSet<int> b = HashSet<int>([2, 3])
        Core.HashSet<int> c = HashSet<int>([1, 2, 3])
        Core.HashSet<int> d = HashSet<int>([3, 4, 5])

        global.println("b.isSubsetOf(a) = " + b.isSubsetOf(a))          # true
        global.println("a.isSupersetOf(b) = " + a.isSupersetOf(b))      # true
        global.println("empty.isSubsetOf(a) = " + empty.isSubsetOf(a)) # true
        global.println("a.isSupersetOf(empty) = " + a.isSupersetOf(empty)) # true
        global.println("b.isProperSubsetOf(a) = " + b.isProperSubsetOf(a))  # true
        global.println("a.isProperSubsetOf(c) = " + a.isProperSubsetOf(c))  # false（长度相等）
        global.println("a.isProperSupersetOf(b) = " + a.isProperSupersetOf(b)) # true
        global.println("a.overlaps(d) = " + a.overlaps(d))              # true（共有 3）
        global.println("empty.overlaps(a) = " + empty.overlaps(a))     # false
        global.println("a.setEquals(c) = " + a.setEquals(c))           # true
        global.println("a.setEquals(b) = " + a.setEquals(b))           # false
    }

    # 测试 toArray / toList / first / last
    static testConvert()
    {
        global.println("===== testConvert =====")
        Core.HashSet<int> s = HashSet<int>([10, 20, 30])
        Array<int> arr = s.toArray()
        global.println("toArray.length = " + arr.length)
        for i = 0, i < arr.length, i++
        {
            global.println("arr[" + i + "] = " + arr[i])
        }
        Core.List<int> list = s.toList()
        global.println("toList.length = " + list.length)
        global.println("first = " + s.first)
        global.println("last = " + s.last)
    }

    # 测试迭代器 / foreach
    static testIterator()
    {
        global.println("===== testIterator =====")
        Core.HashSet<string> s = HashSet<string>(["a", "b", "c"])
        int sum = 0
        for item in s
        {
            global.println("foreach item = " + item)
            sum++
        }
        global.println("iterated count = " + sum)
    }

    # 测试字符串元素集合
    static testStringElements()
    {
        global.println("===== testStringElements =====")
        Core.HashSet<string> s = new()
        s.add("apple")
        s.add("banana")
        s.add("apple")     # 去重
        global.println("length = " + s.length)
        global.println("contains(banana) = " + s.contains("banana"))
        global.println("add(null) = " + s.add(null))              # false（null 不存储）
        global.println("contains(null) = " + s.contains(null))    # false
        global.println("remove(null) = " + s.remove(null))        # false
        global.println("toString = " + s.toString())
    }

    # 测试空集合边界行为
    static testEmptyEdge()
    {
        global.println("===== testEmptyEdge =====")
        Core.HashSet<int> s = new()
        global.println("toArray.length = " + s.toArray().length)
        global.println("first = " + s.first)
        global.println("last = " + s.last)
        global.println("toString = " + s.toString())
        Core.HashSet<int> empty = new()
        global.println("setEquals(empty) = " + s.setEquals(empty))
    }

    static fun()
    {
        global.println("===== HashSetTest =====")
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
