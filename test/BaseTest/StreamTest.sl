# ============================================================================
# StreamTest.sl -- Stream<T> L1 元素流功能测试（P1 可测子集 C/C2/D/E/F）
#
# 参考设计档：md/design/STREAM_DESIGN.md §18（C / C2 / D / E / F 组断言）。
# 测试对象：Core/IO/Stream.sl（推/拉双模事件流）。
# 落位说明：设计文档 §18 建议 ExpendTest/CSimpleVMStdTest，但 Stream 属
#   Core 库，按测试路由（md/project/test-guide.md §1）落 BaseTest/
#   CSimpleVMCoreTest，与 CoroutineTest.sl / ByteBufTest.sl 同列。
#
# 编写约定（沿用 CoroutineTest.sl）：
#  1. 被 spawn 的方法按"简单名+参数个数"在整个汇编内全局解析，故本文件
#     被 spawn 的方法均以 coroStream 前缀命名（全工程唯一）。
#  2. 闭包仅捕获外层方法局部变量/参数；静态字段在闭包内直接访问
#     （先例 CoroutineTest K3/K5）。
#  3. 错误走 onError 回调不走异常通道（设计 §14）；背压级联场景下生产
#     协程对已关闭通道 send 抛异常终止（无人 await，仅状态 Dead）。
#  4. 顺序断言不依赖字符串拼接：记 first/last/相邻增量标记。
# ============================================================================

StreamTest
{
    # ---------- 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL ----------
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[StreamTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[StreamTest] " + name + " : FAIL" )
        }
    }

    # ---------- 全局辅助状态（协程/闭包间共享静态字段） ----------
    static int g_count = 0
    static int g_sum = 0
    static int g_first = -1
    static int g_last = -100
    static int g_stepOK = 1
    static string g_err = ""
    static bool g_done = false
    static int g_mapCount = 0
    static int g_genCount = 0
    static int g_f1sent = 0
    static int g_f2sent = 0

    # ======================================================================
    # 被 spawn 的静态方法（名字全工程唯一，coroStream 前缀）
    # ======================================================================

    # E1：拉取协程——逐个 moveNext 累加（通道空时挂起在 recv）
    static coroStreamE1Pull( StreamController<Int32> ctrl )
    {
        Stream<Int32> s = ctrl.stream
        var it = s.iterator
        while it.moveNext()
        {
            StreamTest.g_sum = StreamTest.g_sum + ( it.current as int )
            StreamTest.g_count = StreamTest.g_count + 1
        }
    }

    # F1：背压生产协程——容量 1 通道，第二个 add 挂起直至被消费
    static coroStreamF1Produce( StreamController<Int32> ctrl )
    {
        ctrl.add( 10 )
        StreamTest.g_f1sent = 1
        ctrl.add( 20 )
        StreamTest.g_f1sent = 2
        ctrl.add( 30 )
        StreamTest.g_f1sent = 3
        ctrl.close()
    }

    # F2：无上限通道连发 3 个不挂起
    static coroStreamF2Burst( StreamController<Int32> ctrl )
    {
        ctrl.add( 1 )
        ctrl.add( 2 )
        ctrl.add( 3 )
        StreamTest.g_f2sent = 3
        ctrl.close()
    }

    # ======================================================================
    # C 组：Stream 基础（generate+close / fromIterable / toList / forEach 顺序）
    # ======================================================================
    static testGroupC()
    {
        global.println( "========== C: Stream 基础 ==========" )

        # C1 fromIterable + toList（Task 取回 List<T> 后遍历校验）
        Array<Int32> arr1 = Array<Int32>.create( 5 )
        arr1._setItem_( 0, 1 )
        arr1._setItem_( 1, 2 )
        arr1._setItem_( 2, 3 )
        arr1._setItem_( 3, 4 )
        arr1._setItem_( 4, 5 )
        Stream<Int32> s1 = Stream<Int32>.fromIterable( arr1 )
        Task t1 = s1.toListThenTask()
        object r1 = Coroutine.awaitHandle( t1 )
        List<Int32> lst1 = r1 as List<Int32>
        int c1cnt = 0
        int c1sum = 0
        int c1first = -1
        int c1last = -100
        if lst1 != null
        {
            for v in lst1
            {
                int x = v as int
                if c1cnt == 0
                {
                    c1first = x
                }
                c1last = x
                c1sum = c1sum + x
                c1cnt = c1cnt + 1
            }
        }
        check( "C1 fromIterable+toList", c1cnt == 5 && c1sum == 15 && c1first == 1 && c1last == 5 )

        # C2 generate + forEach：产出顺序 1..5（first/增量/last 三重校验）
        StreamTest.g_count = 0
        StreamTest.g_sum = 0
        StreamTest.g_first = -1
        StreamTest.g_last = -100
        StreamTest.g_stepOK = 1
        function gen = function( int i )
        {
            ret i + 1;
        }
        function act = function( object v )
        {
            int x = v as int
            if StreamTest.g_count == 0
            {
                StreamTest.g_first = x
            }
            else
            {
                if x != StreamTest.g_last + 1
                {
                    StreamTest.g_stepOK = 0
                }
            }
            StreamTest.g_last = x
            StreamTest.g_sum = StreamTest.g_sum + x
            StreamTest.g_count = StreamTest.g_count + 1
        }
        Stream<Int32> s2 = Stream<Int32>.generate( 5, gen )
        Task t2 = s2.forEach( act )
        Coroutine.awaitHandle( t2 )
        check( "C2 generate+forEach 顺序", StreamTest.g_count == 5 && StreamTest.g_sum == 15 && StreamTest.g_first == 1 && StreamTest.g_last == 5 && StreamTest.g_stepOK == 1 )

        # C3 empty 流：订阅即 done，无数据
        StreamTest.g_count = 0
        StreamTest.g_done = false
        Stream<Int32> s3 = Stream<Int32>.empty()
        function onData3 = function( object v )
        {
            StreamTest.g_count = StreamTest.g_count + 1
        }
        function onDone3 = function()
        {
            StreamTest.g_done = true
        }
        s3.listen( onData3, null, onDone3, false )
        Coroutine.sleep( 10 )
        check( "C3 empty 流订阅即 done", StreamTest.g_done && StreamTest.g_count == 0 )
    }

    # ======================================================================
    # C2 组：惰性转换链（where+map+take，周期性流验证未提前求值）
    # ======================================================================
    static testGroupC2()
    {
        global.println( "========== C2: 惰性转换链 ==========" )

        # C2-1 链构建期间零求值（gen/mapper 均未被调用）
        StreamTest.g_genCount = 0
        StreamTest.g_mapCount = 0
        function gen = function( int i )
        {
            StreamTest.g_genCount = StreamTest.g_genCount + 1
            ret i;
        }
        function pred = function( object v )
        {
            int x = v as int
            bool even = x % 2 == 0
            ret even;
        }
        function mapper = function( object v )
        {
            StreamTest.g_mapCount = StreamTest.g_mapCount + 1
            int x = v as int
            ret x * 10;
        }
        Stream<Int32> s = Stream<Int32>.generate( 10, gen )
        Stream<Int32> w = s.where( pred )
        Stream<Int32> m = w.mapEach<Int32>( mapper )
        Stream<Int32> tk = m.take( 3 )
        check( "C2-1 链构建期间零求值", StreamTest.g_genCount == 0 && StreamTest.g_mapCount == 0 )

        # C2-2 where+map+take 消费结果：0..9 偶数 ×10 取 3 → 0, 20, 40
        Task t = tk.toListThenTask()
        object r = Coroutine.awaitHandle( t )
        List<Int32> lst = r as List<Int32>
        int cnt = 0
        int sum = 0
        int first = -1
        int last = -100
        if lst != null
        {
            for v in lst
            {
                int x = v as int
                if cnt == 0
                {
                    first = x
                }
                last = x
                sum = sum + x
                cnt = cnt + 1
            }
        }
        check( "C2-2 where+map+take 结果", cnt == 3 && sum == 60 && first == 0 && last == 40 )

        # C2-3 周期性流（controller 手动驱动）：未投递零变换，投一个变换一次
        StreamTest.g_mapCount = 0
        StreamController<Int32> ctrl = StreamController<Int32>()
        Stream<Int32> src = ctrl.stream
        function mapper2 = function( object v )
        {
            StreamTest.g_mapCount = StreamTest.g_mapCount + 1
            ret v;
        }
        Stream<Int32> mm = src.mapEach<Int32>( mapper2 )
        function onData = function( object v )
        {
        }
        mm.listen( onData )
        Coroutine.sleep( 10 )
        bool beforeOk = ( StreamTest.g_mapCount == 0 )
        ctrl.add( 1 )
        Coroutine.sleep( 10 )
        bool afterOneOk = ( StreamTest.g_mapCount == 1 )
        ctrl.close()
        Coroutine.sleep( 10 )
        bool afterCloseOk = ( StreamTest.g_mapCount == 1 )
        check( "C2-3 周期性流未提前求值", beforeOk && afterOneOk && afterCloseOk )
    }

    # ======================================================================
    # D 组：错误传播（addError → onError；cancelOnError 自动取消）
    # ======================================================================
    static testGroupD()
    {
        global.println( "========== D: 错误传播 ==========" )

        # D1 addError 传播且不中断：错误后数据继续、done 正常触发
        StreamController<Int32> ctrl1 = StreamController<Int32>()
        Stream<Int32> s1 = ctrl1.stream
        StreamTest.g_err = ""
        StreamTest.g_count = 0
        StreamTest.g_done = false
        function onData1 = function( object v )
        {
            StreamTest.g_count = StreamTest.g_count + 1
        }
        function onError1 = function( object e )
        {
            StreamTest.g_err = e as string
        }
        function onDone1 = function()
        {
            StreamTest.g_done = true
        }
        s1.listen( onData1, onError1, onDone1, false )
        ctrl1.add( 1 )
        ctrl1.addError( "boom1" )
        ctrl1.add( 2 )
        ctrl1.close()
        Coroutine.sleep( 30 )
        check( "D1 addError 传播且不中断", StreamTest.g_err == "boom1" && StreamTest.g_count == 2 && StreamTest.g_done )

        # D2 cancelOnError=true：错误后自动取消，后续事件不再分发
        StreamController<Int32> ctrl2 = StreamController<Int32>()
        Stream<Int32> s2 = ctrl2.stream
        StreamTest.g_err = ""
        StreamTest.g_count = 0
        StreamTest.g_done = false
        function onData2 = function( object v )
        {
            StreamTest.g_count = StreamTest.g_count + 1
        }
        function onError2 = function( object e )
        {
            StreamTest.g_err = e as string
        }
        function onDone2 = function()
        {
            StreamTest.g_done = true
        }
        s2.listen( onData2, onError2, onDone2, true )
        ctrl2.add( 1 )
        ctrl2.addError( "boom2" )
        ctrl2.add( 2 )
        ctrl2.close()
        Coroutine.sleep( 30 )
        check( "D2 cancelOnError 停止分发", StreamTest.g_err == "boom2" && StreamTest.g_count == 1 && StreamTest.g_done == false )

        # D3 Stream.error 工厂：订阅即 error + done
        StreamTest.g_err = ""
        StreamTest.g_count = 0
        StreamTest.g_done = false
        Stream<Int32> s3 = Stream<Int32>.failed( "errFactory" )
        function onData3 = function( object v )
        {
            StreamTest.g_count = StreamTest.g_count + 1
        }
        function onError3 = function( object e )
        {
            StreamTest.g_err = e as string
        }
        function onDone3 = function()
        {
            StreamTest.g_done = true
        }
        s3.listen( onData3, onError3, onDone3, false )
        Coroutine.sleep( 20 )
        check( "D3 Stream.error 订阅即错误", StreamTest.g_err == "errFactory" && StreamTest.g_count == 0 && StreamTest.g_done )

        # D4 sub.cancel：取消后无分发（生产协程级联终止）
        Array<Int32> arr4 = Array<Int32>.create( 3 )
        arr4._setItem_( 0, 7 )
        arr4._setItem_( 1, 8 )
        arr4._setItem_( 2, 9 )
        Stream<Int32> s4 = Stream<Int32>.fromIterable( arr4 )
        StreamTest.g_count = 0
        function onData4 = function( object v )
        {
            StreamTest.g_count = StreamTest.g_count + 1
        }
        StreamSubscription sub4 = s4.listen( onData4 )
        sub4.cancel()
        Coroutine.sleep( 30 )
        check( "D4 cancel 后无分发", StreamTest.g_count == 0 && sub4.isCanceled )
    }

    # ======================================================================
    # E 组：IIterator 拉取（协程内 moveNext 挂起与恢复）
    # ======================================================================
    static testGroupE()
    {
        global.println( "========== E: 拉取模式 ==========" )

        # 本组包装闭包（函数值形式，见 CoroutineTest 约定）
        function e1PullFn = function( StreamController<Int32> c )
        {
            coroStreamE1Pull( c )
        }

        # E1 拉取协程挂起：spawn 后通道无数据 → 挂起；add 后恢复；close 后结束
        StreamTest.g_count = 0
        StreamTest.g_sum = 0
        StreamController<Int32> ctrl = StreamController<Int32>()
        Task pull = spawn e1PullFn( ctrl )
        Coroutine.sleep( 30 )
        bool suspendedOk = ( StreamTest.g_count == 0 && pull.isDead == false )
        ctrl.add( 1 )
        ctrl.add( 2 )
        ctrl.close()
        Coroutine.awaitHandle( pull )
        check( "E1 拉取协程挂起与恢复", suspendedOk && StreamTest.g_count == 2 && StreamTest.g_sum == 3 )

        # E2 root 协程直接拉取生产流（fromIterable）
        Array<Int32> arr2 = Array<Int32>.create( 4 )
        arr2._setItem_( 0, 1 )
        arr2._setItem_( 1, 2 )
        arr2._setItem_( 2, 3 )
        arr2._setItem_( 3, 4 )
        Stream<Int32> s2 = Stream<Int32>.fromIterable( arr2 )
        var it = s2.iterator
        int e2sum = 0
        int e2cnt = 0
        while it.moveNext()
        {
            e2sum = e2sum + ( it.current as int )
            e2cnt = e2cnt + 1
        }
        check( "E2 拉取生产流全量", e2sum == 10 && e2cnt == 4 )
    }

    # ======================================================================
    # F 组：背压（容量 1 controller，第二个 add 挂起，消费后恢复）
    # ======================================================================
    static testGroupF()
    {
        global.println( "========== F: 背压 ==========" )

        # 本组包装闭包（函数值形式，见 CoroutineTest 约定）
        function f1ProduceFn = function( StreamController<Int32> c )
        {
            coroStreamF1Produce( c )
        }
        function f2BurstFn = function( StreamController<Int32> c )
        {
            coroStreamF2Burst( c )
        }

        # F1 容量 1：第一个 add 后第二个挂起（生产协程活着但不再推进）
        StreamTest.g_f1sent = 0
        StreamController<Int32> ctrl = StreamController<Int32>( 1 )
        Task p = spawn f1ProduceFn( ctrl )
        Coroutine.sleep( 30 )
        bool blockedOk = ( StreamTest.g_f1sent == 1 && p.isDead == false )
        # 消费驱动生产：iterator 拉取（触发 listen + 分发协程 recv）
        Stream<Int32> s = ctrl.stream
        var it = s.iterator
        int f1sum = 0
        int f1cnt = 0
        while it.moveNext()
        {
            f1sum = f1sum + ( it.current as int )
            f1cnt = f1cnt + 1
        }
        Coroutine.awaitHandle( p )
        check( "F1 容量满 add 挂起", blockedOk )
        check( "F1 消费驱动生产完成", f1sum == 60 && f1cnt == 3 && StreamTest.g_f1sent == 3 )

        # F2 默认无上限：连发 3 个不挂起（无消费者时也全部入队）
        StreamTest.g_f2sent = 0
        StreamController<Int32> ctrl2 = StreamController<Int32>()
        Task p2 = spawn f2BurstFn( ctrl2 )
        Coroutine.sleep( 30 )
        bool burstOk = ( StreamTest.g_f2sent == 3 )
        # 清理：消费全量
        Stream<Int32> s2 = ctrl2.stream
        var it2 = s2.iterator
        int f2sum = 0
        while it2.moveNext()
        {
            f2sum = f2sum + ( it2.current as int )
        }
        Coroutine.awaitHandle( p2 )
        check( "F2 无上限 add 不挂起", burstOk && f2sum == 6 )
    }

    # ======================================================================
    # 入口
    # ======================================================================
    static fun()
    {
        global.println( "========== StreamTest (start) ==========" )

        StreamTest.testGroupC()
        StreamTest.testGroupC2()
        StreamTest.testGroupD()
        StreamTest.testGroupE()
        StreamTest.testGroupF()

        global.println( "========== StreamTest (end) ==========" )
    }
}
