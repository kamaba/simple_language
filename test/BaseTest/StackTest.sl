#Stack 容器功能测试：构造/入栈/出栈/查找/扩容/清空/转换/迭代器（LIFO）
StackTest
{
    # 测试基本 push / pop / peek / length / isEmpty
    static testBasicPushPop()
    {
        global.println("===== testBasicPushPop =====")
        Stack<int> s = new()
        global.println("isEmpty = " + s.isEmpty.toString())
        s.push(1)
        s.push(2)
        s.push(3)
        global.println("length = " + s.length.toString())
        global.println("peek = " + s.peek.toString())
        global.println("bottom = " + s.bottom.toString())
        global.println("pop = " + s.pop().toString())
        global.println("pop = " + s.pop().toString())
        global.println("length = " + s.length.toString())
        global.println("peek = " + s.peek.toString())
        global.println("isEmpty = " + s.isEmpty.toString())
        global.println("pop = " + s.pop().toString())
        global.println("isEmpty = " + s.isEmpty.toString())
        global.println("pop on empty is null = " + (s.pop() == null).toString())
        global.println("peek on empty is null = " + (s.peek == null).toString())
    }

    # 测试指定容量构造 + 从数组构造
    static testConstructors()
    {
        global.println("===== testConstructors =====")
        Stack<int> s1 = Stack<int>(8)
        global.println("capacity(s1) = " + s1.capacity.toString())

        Array<int> arr = Array<int>(3)
        arr[0] = 10
        arr[1] = 20
        arr[2] = 30
        Stack<int> s2 = Stack<int>(arr)    # 数组首元素为栈底
        global.println("s2.length = " + s2.length.toString())
        global.println("s2.bottom = " + s2.bottom.toString())
        global.println("s2.peek = " + s2.peek.toString())
        global.println("s2.toString = " + s2.toString())
    }

    # 测试 contains / indexOf / lastIndexOf / pushRange
    static testSearchAndPushRange()
    {
        global.println("===== testSearchAndPushRange =====")
        Stack<int> s = new()
        s.push(1)
        s.push(2)
        s.push(3)
        s.push(2)
        global.println("contains(2) = " + s.contains(2).toString())
        global.println("contains(9) = " + s.contains(9).toString())
        global.println("indexOf(2) = " + s.indexOf(2).toString())          # 底->顶首个 = 1
        global.println("lastIndexOf(2) = " + s.lastIndexOf(2).toString())  # 顶->底首个 = 3
        global.println("indexOf(9) = " + s.indexOf(9).toString())          # -1

        Array<int> more = Array<int>(3)
        more[0] = 4
        more[1] = 5
        more[2] = 6
        s.pushRange(more)
        global.println("after pushRange, length = " + s.length.toString())  # 7
        global.println("s.toString = " + s.toString())
    }

    # 测试容量增长（自动扩容）
    static testGrow()
    {
        global.println("===== testGrow =====")
        Stack<int> s = new()
        global.println("initial capacity = " + s.capacity.toString())   # 0
        s.push(1)
        global.println("after 1 push, capacity = " + s.capacity.toString())  # 4
        for i = 2, i <= 4, i++
        {
            s.push(i)
        }
        global.println("after 4 pushes, capacity = " + s.capacity.toString())  # 4
        s.push(5)
        global.println("after 5 pushes, capacity = " + s.capacity.toString())  # 8
        for i = 6, i <= 16, i++
        {
            s.push(i)
        }
        global.println("after 16 pushes, capacity = " + s.capacity.toString()) # 16
        global.println("length = " + s.length.toString())
    }

    # 测试 ensureCapacity / capacity setter
    static testEnsureCapacity()
    {
        global.println("===== testEnsureCapacity =====")
        Stack<int> s = new()
        s.ensureCapacity(100)
        global.println("after ensureCapacity(100), capacity = " + s.capacity.toString())  # 100
        s.push(1)
        global.println("after 1 push, capacity = " + s.capacity.toString())  # 仍为 100（不缩容）
    }

    # 测试 clear
    static testClear()
    {
        global.println("===== testClear =====")
        Stack<int> s = new()
        s.push(1)
        s.push(2)
        s.push(3)
        s.clear()
        global.println("after clear, length = " + s.length.toString())
        global.println("after clear, isEmpty = " + s.isEmpty.toString())
        global.println("after clear, capacity = " + s.capacity.toString())  # 0
        s.push(9)
        global.println("push after clear works, length = " + s.length.toString())
    }

    # 测试 toArray / toList / copy
    static testConvert()
    {
        global.println("===== testConvert =====")
        Array<int> arr = Array<int>(3)
        arr[0] = 10
        arr[1] = 20
        arr[2] = 30
        Stack<int> s = Stack<int>(arr)
        Array<int> outArr = s.toArray()
        global.println("toArray.length = " + outArr.length.toString())
        for i = 0, i < outArr.length, i++
        {
            global.println("outArr[" + i.toString() + "] = " + outArr[i].toString())
        }
        List<int> list = s.toList()
        global.println("toList.length = " + list.length.toString())
        Stack<int> c = s.copy()
        global.println("copy.length = " + c.length.toString())
        global.println("copy.peek = " + c.peek.toString())
        global.println("copy.toString = " + c.toString())
    }

    # 测试迭代器 / foreach（LIFO：栈顶 -> 栈底）
    static testIterator()
    {
        global.println("===== testIterator =====")
        Stack<string> s = new()
        s.push("a")
        s.push("b")
        s.push("c")
        int count = 0
        for item in s
        {
            global.println("foreach item = " + item)
            count++
        }
        global.println("iterated count = " + count.toString())
        # 迭代后再迭代（reset 语义）
        for item in s
        {
            global.println("foreach again item = " + item)
        }
    }

    # 测试字符串元素栈 + toString
    static testStringElements()
    {
        global.println("===== testStringElements =====")
        Stack<string> s = new()
        s.push("apple")
        s.push("banana")
        s.push("cherry")
        global.println("length = " + s.length.toString())
        global.println("contains(banana) = " + s.contains("banana").toString())
        global.println("toString = " + s.toString())   # [apple,banana,cherry]
    }

    # 测试空栈边界行为
    static testEmptyEdge()
    {
        global.println("===== testEmptyEdge =====")
        Stack<int> s = new()
        global.println("toArray.length = " + s.toArray().length.toString())
        global.println("bottom is null = " + (s.bottom == null).toString())
        global.println("toString = " + s.toString())
        global.println("contains(1) = " + s.contains(1).toString())
        global.println("indexOf(1) = " + s.indexOf(1).toString())
        for item in s
        {
            global.println("should not reach here")
        }
        global.println("empty foreach done")
    }

    static fun()
    {
        global.println("===== StackTest =====")
        testBasicPushPop()
        testConstructors()
        testSearchAndPushRange()
        testGrow()
        testEnsureCapacity()
        testClear()
        testConvert()
        testIterator()
        testStringElements()
        testEmptyEdge()
    }
}
