#Queue 容器功能测试：构造/入队/出队/查找/扩容/回绕/清空/转换/迭代器（FIFO）
QueueTest
{
    # 测试基本 enqueue / dequeue / peek / rear / length / isEmpty（FIFO 顺序）
    static testBasicEnqueueDequeue()
    {
        global.println("===== testBasicEnqueueDequeue =====")
        Queue<int> q = new()
        global.println("isEmpty = " + q.isEmpty.toString())
        q.enqueue(1)
        q.enqueue(2)
        q.enqueue(3)
        global.println("length = " + q.length.toString())
        global.println("peek = " + q.peek.toString())     # 1（队首）
        global.println("rear = " + q.rear.toString())     # 3（队尾）
        global.println("dequeue = " + q.dequeue().toString())  # 1
        global.println("dequeue = " + q.dequeue().toString())  # 2
        global.println("length = " + q.length.toString())      # 1
        global.println("peek = " + q.peek.toString())          # 3
        global.println("isEmpty = " + q.isEmpty.toString())
        global.println("dequeue = " + q.dequeue().toString())  # 3
        global.println("isEmpty = " + q.isEmpty.toString())
        global.println("dequeue on empty is null = " + (q.dequeue() == null).toString())
        global.println("peek on empty is null = " + (q.peek == null).toString())
        global.println("rear on empty is null = " + (q.rear == null).toString())
    }

    # 测试指定容量构造 + 从数组构造（数组首元素成为队首）
    static testConstructors()
    {
        global.println("===== testConstructors =====")
        Queue<int> q1 = Queue<int>(8)
        global.println("capacity(q1) = " + q1.capacity.toString())

        Array<int> arr = Array<int>(3)
        arr[0] = 10
        arr[1] = 20
        arr[2] = 30
        Queue<int> q2 = Queue<int>(arr)    # 数组首元素为队首
        global.println("q2.length = " + q2.length.toString())
        global.println("q2.peek = " + q2.peek.toString())     # 10
        global.println("q2.rear = " + q2.rear.toString())     # 30
        global.println("q2.toString = " + q2.toString())      # [10,20,30]
        global.println("dequeue order = " + q2.dequeue().toString() + "," + q2.dequeue().toString() + "," + q2.dequeue().toString())  # 10,20,30

        Queue<int> q3 = Queue<int>(-5)     # 负容量按 0 处理
        global.println("capacity(q3) = " + q3.capacity.toString())
    }

    # 测试 contains / indexOf / lastIndexOf / enqueueRange
    static testSearchAndEnqueueRange()
    {
        global.println("===== testSearchAndEnqueueRange =====")
        Queue<int> q = new()
        q.enqueue(1)
        q.enqueue(2)
        q.enqueue(3)
        q.enqueue(2)
        global.println("contains(2) = " + q.contains(2).toString())
        global.println("contains(9) = " + q.contains(9).toString())
        global.println("indexOf(2) = " + q.indexOf(2).toString())          # 队首起首个 = 1
        global.println("lastIndexOf(2) = " + q.lastIndexOf(2).toString())  # 队尾起最后匹配 = 3
        global.println("indexOf(9) = " + q.indexOf(9).toString())          # -1

        Array<int> more = Array<int>(3)
        more[0] = 4
        more[1] = 5
        more[2] = 6
        q.enqueueRange(more)
        global.println("after enqueueRange, length = " + q.length.toString())  # 7
        global.println("q.toString = " + q.toString())                         # [1,2,3,2,4,5,6]
        global.println("rear after enqueueRange = " + q.rear.toString())       # 6
    }

    # 测试容量增长（自动扩容 0->4->8->16）
    static testGrow()
    {
        global.println("===== testGrow =====")
        Queue<int> q = new()
        global.println("initial capacity = " + q.capacity.toString())   # 0
        q.enqueue(1)
        global.println("after 1 enqueue, capacity = " + q.capacity.toString())  # 4
        for i = 2, i <= 4, i++
        {
            q.enqueue(i)
        }
        global.println("after 4 enqueues, capacity = " + q.capacity.toString())  # 4
        q.enqueue(5)
        global.println("after 5 enqueues, capacity = " + q.capacity.toString())  # 8
        for i = 6, i <= 16, i++
        {
            q.enqueue(i)
        }
        global.println("after 16 enqueues, capacity = " + q.capacity.toString()) # 16
        global.println("length = " + q.length.toString())                         # 16
        global.println("peek still first = " + q.peek.toString())                 # 1（扩容不破坏 FIFO 序）
    }

    # 测试 ensureCapacity / capacity setter
    static testEnsureCapacity()
    {
        global.println("===== testEnsureCapacity =====")
        Queue<int> q = new()
        q.ensureCapacity(100)
        global.println("after ensureCapacity(100), capacity = " + q.capacity.toString())  # 100
        q.enqueue(1)
        global.println("after 1 enqueue, capacity = " + q.capacity.toString())  # 仍为 100（不缩容）
        q.enqueue(2)
        global.println("peek = " + q.peek.toString())   # 1
        q.capacity = 2
        global.println("after set capacity=2, capacity = " + q.capacity.toString())  # 2（不小于 count 才生效）
    }

    # 环形缓冲回绕（Queue 特有核心场景）：_head/_tail 越过数组末尾后环绕，逻辑顺序不乱
    static testWraparound()
    {
        global.println("===== testWraparound =====")
        Queue<int> q = Queue<int>(4)
        q.enqueue(1)
        q.enqueue(2)
        q.enqueue(3)
        q.enqueue(4)                       # 满：head=0 tail=0
        global.println("dequeue = " + q.dequeue().toString())  # 1（head=1）
        global.println("dequeue = " + q.dequeue().toString())  # 2（head=2）
        q.enqueue(5)                       # 写回槽位0（tail 环绕）
        q.enqueue(6)                       # 写回槽位1
        global.println("after wrap, length = " + q.length.toString())   # 4
        global.println("after wrap, peek = " + q.peek.toString())       # 3
        global.println("after wrap, rear = " + q.rear.toString())       # 6
        global.println("after wrap, toString = " + q.toString())        # [3,4,5,6]
        global.println("after wrap, indexOf(5) = " + q.indexOf(5).toString())   # 2
        global.println("dequeue order = " + q.dequeue().toString() + "," + q.dequeue().toString() + "," + q.dequeue().toString() + "," + q.dequeue().toString())  # 3,4,5,6
        global.println("isEmpty = " + q.isEmpty.toString())

        # 回绕期间扩容：环绕态下再入队触发 grow，元素须按逻辑序展开
        Queue<int> q2 = Queue<int>(4)
        q2.enqueue(1)
        q2.enqueue(2)
        q2.enqueue(3)
        q2.enqueue(4)
        q2.dequeue()                       # head=1
        q2.enqueue(5)                      # tail 环绕到 0
        q2.enqueue(6)                      # 触发扩容 4->8，逻辑序 [2,3,4,5,6]
        global.println("q2.length = " + q2.length.toString())       # 5
        global.println("q2.capacity = " + q2.capacity.toString())   # 8
        global.println("q2.toString = " + q2.toString())            # [2,3,4,5,6]
    }

    # 交错入队/出队压测：多次环绕 + 扩容，验证 FIFO 序与游标一致性
    static testInterleavedStress()
    {
        global.println("===== testInterleavedStress =====")
        Queue<int> q = new()
        int sum = 0
        for i = 1, i <= 200, i++
        {
            q.enqueue(i)
            if i > 100
            {
                sum += q.dequeue()
            }
        }
        global.println("length = " + q.length.toString())       # 100（1..100 已出队）
        global.println("peek = " + q.peek.toString())           # 101
        global.println("rear = " + q.rear.toString())           # 200
        global.println("dequeue sum(1..100) = " + sum.toString())  # 5050

        # 队列只剩 101..200，顺序出队求和验证
        int remainSum = 0
        while q.isNotEmpty
        {
            remainSum += q.dequeue()
        }
        global.println("dequeue sum(101..200) = " + remainSum.toString())  # 15050
        global.println("isEmpty = " + q.isEmpty.toString())
    }

    # 测试 clear
    static testClear()
    {
        global.println("===== testClear =====")
        Queue<int> q = new()
        q.enqueue(1)
        q.enqueue(2)
        q.enqueue(3)
        q.clear()
        global.println("after clear, length = " + q.length.toString())
        global.println("after clear, isEmpty = " + q.isEmpty.toString())
        global.println("after clear, capacity = " + q.capacity.toString())  # 0
        q.enqueue(9)
        global.println("enqueue after clear works, length = " + q.length.toString())
        global.println("enqueue after clear, peek = " + q.peek.toString())  # 9
    }

    # 测试 toArray / toList / copy
    static testConvert()
    {
        global.println("===== testConvert =====")
        Array<int> arr = Array<int>(3)
        arr[0] = 10
        arr[1] = 20
        arr[2] = 30
        Queue<int> q = Queue<int>(arr)
        Array<int> outArr = q.toArray()
        global.println("toArray.length = " + outArr.length.toString())
        for i = 0, i < outArr.length, i++
        {
            global.println("outArr[" + i.toString() + "] = " + outArr[i].toString())  # 10,20,30
        }
        List<int> list = q.toList()
        global.println("toList.length = " + list.length.toString())
        Queue<int> c = q.copy()
        global.println("copy.length = " + c.length.toString())
        global.println("copy.peek = " + c.peek.toString())
        global.println("copy.rear = " + c.rear.toString())
        global.println("copy.toString = " + c.toString())
        # 拷贝独立：改原队列不影响副本
        q.dequeue()
        global.println("after orig dequeue, copy.length = " + c.length.toString())  # 仍 3
    }

    # 测试迭代器 / foreach（FIFO：队首 -> 队尾），含 reset 复用与手动游标
    static testIterator()
    {
        global.println("===== testIterator =====")
        Queue<string> q = new()
        q.enqueue("a")
        q.enqueue("b")
        q.enqueue("c")
        int count = 0
        for item in q
        {
            global.println("foreach item = " + item)
            count++
        }
        global.println("iterated count = " + count.toString())

        # 迭代后再迭代（reset 语义）
        for item in q
        {
            global.println("foreach again item = " + item)
        }

        # 手动迭代器：moveNext 推进，current/index 读取（验证 VM 层游标赋值）
        q.reset()
        while q.moveNext()
        {
            global.println("manual current = " + q.current + ", index = " + q.index.toString())
        }
        global.println("after exhaust, index = " + q.index.toString())      # -1
        global.println("after exhaust, current is null = " + (q.current == null).toString())

        # 出队两个后再迭代：只遍历剩余元素
        q.dequeue()
        q.dequeue()
        for item in q
        {
            global.println("foreach after dequeue item = " + item)   # 只剩 c
        }
    }

    # 测试字符串元素队列 + toString + 字符串查找（覆盖值比较的字符串归一化）
    static testStringElements()
    {
        global.println("===== testStringElements =====")
        Queue<string> q = new()
        q.enqueue("apple")
        q.enqueue("banana")
        q.enqueue("cherry")
        global.println("length = " + q.length.toString())
        global.println("contains(banana) = " + q.contains("banana").toString())
        global.println("contains(durian) = " + q.contains("durian").toString())
        global.println("indexOf(cherry) = " + q.indexOf("cherry").toString())   # 2
        global.println("lastIndexOf(banana) = " + q.lastIndexOf("banana").toString())  # 1
        global.println("toString = " + q.toString())   # [apple,banana,cherry]
        global.println("dequeue = " + q.dequeue())     # apple
        q.enqueue("banana")
        global.println("lastIndexOf(banana) = " + q.lastIndexOf("banana").toString())  # 2（队尾新入的）
        global.println("toString = " + q.toString())   # [banana,cherry,banana]
    }

    # 测试空队边界行为
    static testEmptyEdge()
    {
        global.println("===== testEmptyEdge =====")
        Queue<int> q = new()
        global.println("toArray.length = " + q.toArray().length.toString())
        global.println("toString = " + q.toString())          # []
        global.println("contains(1) = " + q.contains(1).toString())
        global.println("indexOf(1) = " + q.indexOf(1).toString())
        global.println("lastIndexOf(1) = " + q.lastIndexOf(1).toString())
        Array<int> empty = Array<int>(0)
        q.enqueueRange(empty)                                 # 空数组批量入队 no-op
        global.println("after empty enqueueRange, length = " + q.length.toString())
        for item in q
        {
            global.println("should not reach here")
        }
        global.println("empty foreach done")
        q.clear()                                             # 空队 clear 不崩溃
        global.println("empty clear ok, isEmpty = " + q.isEmpty.toString())
    }

    static fun()
    {
        global.println("===== QueueTest =====")
        testBasicEnqueueDequeue()
        testConstructors()
        testSearchAndEnqueueRange()
        testGrow()
        testEnsureCapacity()
        testWraparound()
        testInterleavedStress()
        testClear()
        testConvert()
        testIterator()
        testStringElements()
        testEmptyEdge()
    }
}
