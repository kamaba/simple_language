# ============================================================================
# CoroutineTest.sl -- 协程（Coroutine）功能全量测试
#
# 参考设计档：md/design/COROUTINE_DESIGN.md 第 8 章（A-J 组验收用例）。
# 本语言无 spawn/await/yield 关键字（前端零支持），全部用例以库 API 形式编写：
#   Coroutine.spawn0..3 / yield / sleep / current / status / blockedReason /
#   await / waitAll2/3 / waitAny2/3 / nextCompleted2/3 / waitTimeout / cancel
#   以及 Channel<T>（send / recv / close / count / isClosed）。
#
# 编写约定（重要）：
#  1. 被 spawn 的方法按"简单名 + 参数个数"在整个汇编内全局解析
#     （vm_find_method_entry_by_name），故本文件所有被 spawn 方法均以
#     coro 前缀命名，保证全工程唯一（本测试工程编译几十个 .sl 在一起）。
#  2. C VM 侧抛出的异常（取消 -63 / 非法操作 -64）其异常值为 null，
#     捕获时必须用裸 catch{}（不绑定变量）；SL 层 throw 的枚举异常
#     才可用 catch XxxError ex 绑定。
#  3. 前端不发射 SCHED_CHECK（回边公平性指令），调度公平性用例以
#     显式 Coroutine.yield() 保证交替。
#  4. 主入口 static fun() 被包装为 root 协程（vm_scheduler_enter），
#     因此 await/yield/sleep/current 从主入口调用全部有效。
#  5. 设计档中的语法糖用例（spawn 函数字面量 / await 数组语法糖 /
#     cor.All 变参）在本实现中不存在，以 spawn0..3 + waitAll2/3 固定
#     参数重载等价替代。
# ============================================================================

# 测试用错误枚举（名字避开 TryTest 的 TestError）
enum CoroTestError extends Error
{
    BoomError = { code = 201, message = "coro-boom" }
}

CoroutineTest
{
    # ---------- 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL ----------
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[CoroutineTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[CoroutineTest] " + name + " : FAIL" )
        }
    }

    # ---------- 全局辅助状态（协程间共享静态字段） ----------
    static bool g_done = false
    static string g_order = ""
    static int g_counter = 0
    static int g_f1sum = 0
    static int g_f2sum = 0
    static int g_f3count = 0
    static int g_f4count = 0
    static int g_j1sum = 0

    # ======================================================================
    # 被 spawn 的静态方法（名字全工程唯一，coro 前缀）
    # ======================================================================

    # A1/B1/B4/D3：两参求和
    static int coroAdd2( int a, int b )
    {
        ret a + b
    }

    # A3/A4/C2：三参求和
    static int coroSum3( int a, int b, int c )
    {
        ret a + b + c
    }

    # A2：置标志（fire-and-forget 目标）
    static coroSetFlag()
    {
        CoroutineTest.g_done = true
    }

    # A5：void 协程（await 应得 null）
    static coroVoid()
    {
    }

    # A6：返回数组
    static Array<Int32> coroMakeArr()
    {
        Array<Int32> a = Array<Int32>.create( 3 )
        a._setItem_( 0, 1 )
        a._setItem_( 1, 2 )
        a._setItem_( 2, 3 )
        ret a
    }

    # B2/B3：顺序追踪（记 s -> 睡 50ms -> 记 e）
    static int coroTrack()
    {
        CoroutineTest.g_order = CoroutineTest.g_order + "s"
        Coroutine.sleep( 50 )
        CoroutineTest.g_order = CoroutineTest.g_order + "e"
        ret 1
    }

    # B5：协程内 return await
    static int coroIndirect()
    {
        Int64 h = Coroutine.spawn2( "coroAdd2", 5, 6 )
        ret Coroutine.awaitFunction( h ) as int
    }

    # 通用：睡眠指定毫秒（void 协程）
    static coroSleepMs( int ms )
    {
        Coroutine.sleep( ms )
    }

    # D1：公平性 A 方（10 次追加 + 显式让出）
    static coroFairA()
    {
        for Int32 i = 0, i < 10, i = i + 1
        {
            CoroutineTest.g_order = CoroutineTest.g_order + "A"
            Coroutine.yield()
        }
    }

    # D1：公平性 B 方
    static coroFairB()
    {
        for Int32 i = 0, i < 10, i = i + 1
        {
            CoroutineTest.g_order = CoroutineTest.g_order + "B"
            Coroutine.yield()
        }
    }

    # D2：yield 让出方（记 1 -> 让出 -> 记 2）
    static coroYieldA()
    {
        CoroutineTest.g_order = CoroutineTest.g_order + "1"
        Coroutine.yield()
        CoroutineTest.g_order = CoroutineTest.g_order + "2"
    }

    # D2：无让出方
    static coroPlainB()
    {
        CoroutineTest.g_order = CoroutineTest.g_order + "B"
    }

    # E3：睡眠指定毫秒后按完成顺序追加标记
    static coroTimerMark( int ms, string mark )
    {
        Coroutine.sleep( ms )
        CoroutineTest.g_order = CoroutineTest.g_order + mark
    }

    # G1/G2/C6：抛枚举异常
    static coroThrowErr() throws
    {
        throw CoroTestError.BoomError
    }

    # G3：协程内捕获子协程异常
    static int coroCatchInner() throws
    {
        Int64 h = Coroutine.spawn0( "coroThrowErr" )
        int caught = 0
        label innerBlock
        {
            try Coroutine.awaitFunction( h )
        }
        catch
        {
            caught = 1
        }
        ret caught
    }

    # G4：挂起后 finally 执行
    static coroFinallySleep()
    {
        label finBlock
        {
            Coroutine.sleep( 10 )
        }
        finally
        {
            CoroutineTest.g_done = true
        }
    }

    # G5：无限循环 + finally（取消目标）
    static coroCancelTarget()
    {
        label cancelBlock
        {
            while ( true )
            {
                Coroutine.yield()
            }
        }
        finally
        {
            CoroutineTest.g_done = true
        }
    }

    # H1：深递归（协程内 spawn + await 递归）
    static int coroDeep( int n )
    {
        if ( n <= 0 )
        {
            ret 0
        }
        Int64 h = Coroutine.spawn1( "coroDeep", n - 1 )
        ret ( Coroutine.awaitFunction( h ) as int ) + 1
    }

    # H5：静态计数 100 次
    static coroInc100()
    {
        for Int32 j = 0, j < 100, j = j + 1
        {
            CoroutineTest.g_counter = CoroutineTest.g_counter + 1
        }
    }

    # H7/I2：协程内再 spawn
    static int coroNested()
    {
        Int64 h = Coroutine.spawn2( "coroAdd2", 40, 2 )
        ret Coroutine.awaitFunction( h ) as int
    }

    # F1：生产 0..4 后关闭
    static coroF1Produce( Channel<object> ch )
    {
        for Int32 i = 0, i < 5, i = i + 1
        {
            ch.send( i )
        }
        ch.close()
    }

    # F1：消费至 null 终止
    static coroF1Consume( Channel<object> ch )
    {
        while ( true )
        {
            object v = ch.recv()
            if ( v == null )
            {
                break
            }
            CoroutineTest.g_f1sum = CoroutineTest.g_f1sum + ( v as int )
        }
    }

    # F2：容量 2 上连发 3 个（第 3 个必然满而挂起让出）
    static coroF2Produce( Channel<object> ch )
    {
        ch.send( 1 )
        ch.send( 2 )
        ch.send( 3 )
    }

    # F2：延时后消费 3 个
    static coroF2Consume( Channel<object> ch )
    {
        Coroutine.sleep( 10 )
        CoroutineTest.g_f2sum = CoroutineTest.g_f2sum + ( ch.recv() as int )
        CoroutineTest.g_f2sum = CoroutineTest.g_f2sum + ( ch.recv() as int )
        CoroutineTest.g_f2sum = CoroutineTest.g_f2sum + ( ch.recv() as int )
    }

    # F3：生产 10 个 1
    static coroF3Produce( Channel<object> ch )
    {
        for Int32 i = 0, i < 10, i = i + 1
        {
            ch.send( 1 )
        }
    }

    # F3：消费固定 40 个
    static coroF3Consume( Channel<object> ch )
    {
        for Int32 i = 0, i < 40, i = i + 1
        {
            object v = ch.recv()
            if ( v != null )
            {
                CoroutineTest.g_f3count = CoroutineTest.g_f3count + 1
            }
        }
    }

    # F4：生产 0..99 后关闭
    static coroF4Produce( Channel<object> ch )
    {
        for Int32 i = 0, i < 100, i = i + 1
        {
            ch.send( i )
        }
        ch.close()
    }

    # F4：消费至 null 终止（多消费者分摊）
    static coroF4Consume( Channel<object> ch )
    {
        while ( true )
        {
            object v = ch.recv()
            if ( v == null )
            {
                break
            }
            CoroutineTest.g_f4count = CoroutineTest.g_f4count + 1
        }
    }

    # J1：pipeline 生产者
    static coroJ1Produce( Channel<object> raw )
    {
        for Int32 i = 0, i < 100, i = i + 1
        {
            raw.send( i )
        }
        raw.close()
    }

    # J1：pipeline 处理者（×2 转发）
    static coroJ1Process( Channel<object> raw, Channel<object> proc )
    {
        while ( true )
        {
            object v = raw.recv()
            if ( v == null )
            {
                break
            }
            proc.send( ( v as int ) * 2 )
        }
        proc.close()
    }

    # J1：pipeline 聚合者
    static coroJ1Aggregate( Channel<object> proc )
    {
        while ( true )
        {
            object v = proc.recv()
            if ( v == null )
            {
                break
            }
            CoroutineTest.g_j1sum = CoroutineTest.g_j1sum + ( v as int )
        }
    }

    # J2：扇出工作单元
    static int coroJ2Work( int i )
    {
        Coroutine.sleep( 2 )
        ret i + 1
    }

    # ======================================================================
    # A 组：spawn 基础
    # ======================================================================
    static testGroupA()
    {
        global.println( "========== A: spawn 基础 ==========" )

        # A1 基本 spawn + await 取回返回值
        Int64 h1 = Coroutine.spawn2( "coroAdd2", 3, 4 )
        int r1 = Coroutine.awaitFunction( h1 ) as int
        check( "A1 spawn+await 返回值", r1 == 7 )

        # A2 fire-and-forget：不 await 也要跑完（副作用可见）
        CoroutineTest.g_done = false
        Coroutine.spawn0( "coroSetFlag" )
        for Int32 i = 0, i < 1000, i = i + 1
        {
            if CoroutineTest.g_done
            {
                break
            }
            Coroutine.sleep( 1 )
        }
        check( "A2 不 await 也执行完", CoroutineTest.g_done )

        # A3 多参数
        Int64 h3 = Coroutine.spawn3( "coroSum3", 1, 2, 3 )
        int r3 = Coroutine.awaitFunction( h3 ) as int
        check( "A3 三参数", r3 == 6 )

        # A4 句柄与状态
        Int64 h4 = Coroutine.spawn3( "coroSum3", 1, 2, 3 )
        Int32 st1 = Coroutine.status( h4 )
        bool fresh = st1 == CoroutineStatus.Created || st1 == CoroutineStatus.Ready || st1 == CoroutineStatus.Running
        Coroutine.awaitFunction( h4 )
        check( "A4 新建状态为创建/就绪/运行", fresh )
        check( "A4 结束后状态 Dead", Coroutine.status( h4 ) == CoroutineStatus.Dead )

        # A5 void 协程：await 得 null
        Int64 h5 = Coroutine.spawn0( "coroVoid" )
        object r5 = Coroutine.awaitFunction( h5 )
        check( "A5 void 协程 await 得 null", r5 == null )

        # A6 返回值是数组
        Int64 h6 = Coroutine.spawn0( "coroMakeArr" )
        Array<Int32> arr = Coroutine.awaitFunction( h6 ) as Array<Int32>
        check( "A6 返回数组", arr != null && arr.length == 3 && arr._getItem_( 2 ) == 3 )

        # A7（设计档 spawn 函数字面量）：前端不支持函数字面量，无对应用例
    }

    # ======================================================================
    # B 组：await 基础与串行/并行
    # ======================================================================
    static testGroupB() throws
    {
        global.println( "========== B: await 基础 ==========" )

        # B1 await 已完成协程 = 同步返回不挂起
        Int64 b1a = Coroutine.spawn2( "coroAdd2", 1, 2 )
        Coroutine.awaitFunction( b1a )
        Int64 b1b = Coroutine.spawn2( "coroAdd2", 2, 3 )
        int rb1 = Coroutine.awaitFunction( b1b ) as int
        check( "B1 已完成协程直接取值", rb1 == 5 )

        # B2 串行执行：第二个等第一个完成后才启动
        CoroutineTest.g_order = ""
        Int64 b2a = Coroutine.spawn0( "coroTrack" )
        Coroutine.awaitFunction( b2a )
        Int64 b2b = Coroutine.spawn0( "coroTrack" )
        Coroutine.awaitFunction( b2b )
        check( "B2 串行执行顺序", CoroutineTest.g_order == "sese" )

        # B3 并行执行 + 串行消费
        CoroutineTest.g_order = ""
        Int64 b3a = Coroutine.spawn0( "coroTrack" )
        Int64 b3b = Coroutine.spawn0( "coroTrack" )
        Coroutine.awaitFunction( b3a )
        Coroutine.awaitFunction( b3b )
        check( "B3 并行执行", CoroutineTest.g_order == "ssee" )

        # B4 await 嵌套表达式
        Int64 b4 = Coroutine.spawn2( "coroAdd2", 10, 20 )
        check( "B4 await 嵌套表达式", 1 + ( Coroutine.awaitFunction( b4 ) as int ) == 31 )

        # B5 return await t
        Int64 b5 = Coroutine.spawn0( "coroIndirect" )
        int rb5 = Coroutine.awaitFunction( b5 ) as int
        check( "B5 协程内 return await", rb5 == 11 )

        # B6 await 自己 -> 运行期错误（C 侧抛非法操作，异常值为 null，须裸 catch）
        Int64 self = Coroutine.current()
        bool threw6 = false
        label b6block
        {
            try Coroutine.awaitFunction( self )
        }
        catch
        {
            threw6 = true
        }
        check( "B6 await 自己报错", threw6 && self != 0 )
    }

    # ======================================================================
    # C 组：批量等待
    # ======================================================================
    static testGroupC() throws
    {
        global.println( "========== C: 批量等待 ==========" )

        # C1 waitAll2 + await 逐个取回
        Int64 c1a = Coroutine.spawn2( "coroAdd2", 1, 1 )
        Int64 c1b = Coroutine.spawn2( "coroAdd2", 2, 2 )
        Coroutine.waitAll2( c1a, c1b )
        int rc1a = Coroutine.awaitFunction( c1a ) as int
        int rc1b = Coroutine.awaitFunction( c1b ) as int
        check( "C1 waitAll2 + await 取回", rc1a == 2 && rc1b == 4 )

        # C2 waitAll3 + await 逐个取回
        Int64 c2a = Coroutine.spawn3( "coroSum3", 1, 1, 1 )
        Int64 c2b = Coroutine.spawn2( "coroAdd2", 2, 2 )
        Int64 c2c = Coroutine.spawn2( "coroAdd2", 3, 3 )
        Coroutine.waitAll3( c2a, c2b, c2c )
        int rc2a = Coroutine.awaitFunction( c2a ) as int
        int rc2b = Coroutine.awaitFunction( c2b ) as int
        int rc2c = Coroutine.awaitFunction( c2c ) as int
        check( "C2 waitAll3 + await 取回", rc2a == 3 && rc2b == 4 && rc2c == 6 )

        # C3 waitAny2：先完成者胜出
        Int64 c3slow = Coroutine.spawn1( "coroSleepMs", 100 )
        Int64 c3fast = Coroutine.spawn1( "coroSleepMs", 10 )
        Int64 winner = Coroutine.waitAny2( c3slow, c3fast )
        check( "C3 waitAny2 快者胜出", winner == c3fast )
        Coroutine.awaitFunction( c3slow )    # 清理：等慢者也结束

        # C4 waitAny3：三个不同时长，最快者胜
        Int64 c4a = Coroutine.spawn1( "coroSleepMs", 60 )
        Int64 c4b = Coroutine.spawn1( "coroSleepMs", 10 )
        Int64 c4c = Coroutine.spawn1( "coroSleepMs", 30 )
        Int64 winner3 = Coroutine.waitAny3( c4a, c4b, c4c )
        check( "C4 waitAny3 最快者胜出", winner3 == c4b )
        Coroutine.waitAll3( c4a, c4b, c4c )    # 清理

        # C5 nextCompleted：非阻塞消费（按参数顺序返回第一个 Dead 且未消费者）
        Int64 c5a = Coroutine.spawn1( "coroSleepMs", 50 )
        Int64 c5b = Coroutine.spawn1( "coroSleepMs", 10 )
        Coroutine.sleep( 20 )    # 此刻 c5b 已完成、c5a 未完成
        Int64 got1 = Coroutine.nextCompleted2( c5a, c5b )
        Int64 got2 = Coroutine.nextCompleted2( c5a, c5b )
        check( "C5 nextCompleted 部分完成", got1 == c5b && got2 == 0 )
        Coroutine.awaitFunction( c5a )    # c5a 完成后仍可消费
        Int64 got3 = Coroutine.nextCompleted2( c5a, c5b )
        Int64 got4 = Coroutine.nextCompleted2( c5a, c5b )
        check( "C5 nextCompleted 消费与耗尽", got3 == c5a && got4 == 0 )

        # C6 waitAll2 中某协程出错 -> 立即失败并取消其余
        Int64 c6a = Coroutine.spawn0( "coroThrowErr" )
        Int64 c6b = Coroutine.spawn1( "coroSleepMs", 1000 )
        int caughtCode = 0
        label c6block
        {
            try Coroutine.waitAll2( c6a, c6b )
        }
        catch CoroTestError ex
        {
            #caughtCode = ex.code
        }
        label c6cleanup
        {
            try Coroutine.awaitFunction( c6b )    # 等被取消者也真正结束（裸 catch）
        }
        catch
        {
        }
        check( "C6 waitAll 错误传播并取消其余", caughtCode == 201 && Coroutine.status( c6b ) == CoroutineStatus.Dead )
    }

    # ======================================================================
    # D 组：让出与调度公平性
    # ======================================================================
    static testGroupD()
    {
        global.println( "========== D: 让出与调度公平性 ==========" )

        # D1 两个协程显式让出 -> 严格交替（前端无 SCHED_CHECK，用显式 yield）
        CoroutineTest.g_order = ""
        Int64 d1a = Coroutine.spawn0( "coroFairA" )
        Int64 d1b = Coroutine.spawn0( "coroFairB" )
        Coroutine.waitAll2( d1a, d1b )
        check( "D1 显式 yield 交替执行", CoroutineTest.g_order == "ABABABABABABABABABAB" )

        # D2 yield 让出后，其它就绪协程先跑
        CoroutineTest.g_order = ""
        Int64 d2a = Coroutine.spawn0( "coroYieldA" )
        Int64 d2b = Coroutine.spawn0( "coroPlainB" )
        Coroutine.waitAll2( d2a, d2b )
        check( "D2 yield 让出后 B 先完成", CoroutineTest.g_order == "1B2" )

        # D3 1000 个协程：无栈溢出、无死锁
        int sum = 0
        List<Int64> tasks = List<Int64>()
        for Int32 i = 0, i < 1000, i = i + 1
        {
            tasks.add( Coroutine.spawn2( "coroAdd2", i, 1 ) )
        }
        for v in tasks
        {
            sum = sum + ( Coroutine.awaitFunction( v ) as int )
        }
        check( "D3 1000 协程全部完成", sum == 500500 )

        # D4 后台协程睡醒后自然结束（调度器可正常收敛退出）
        Int64 d4 = Coroutine.spawn1( "coroSleepMs", 5 )
        object rd4 = Coroutine.awaitFunction( d4 )
        check( "D4 无就绪协程时正常收敛", Coroutine.status( d4 ) == CoroutineStatus.Dead && rd4 == null )
    }

    # ======================================================================
    # E 组：定时器与 Sleep
    # ======================================================================
    static testGroupE()
    {
        global.println( "========== E: 定时器与 Sleep ==========" )

        # E1 并行 Sleep：总耗时 ≈ max，不是 sum
        Int64 t0 = Environment.nowMillis()
        Int64 e1a = Coroutine.spawn1( "coroSleepMs", 100 )
        Int64 e1b = Coroutine.spawn1( "coroSleepMs", 100 )
        Coroutine.waitAll2( e1a, e1b )
        Int64 dt = Environment.nowMillis() - t0
        check( "E1 Sleep 并行总时长≈max", dt >= 100 && dt < 190 )

        # E2 Sleep(0) 只让出不阻塞
        Int64 e2 = Coroutine.spawn1( "coroSleepMs", 0 )
        object re2 = Coroutine.awaitFunction( e2 )
        check( "E2 Sleep(0)", Coroutine.status( e2 ) == CoroutineStatus.Dead && re2 == null )

        # E3 定时器唤醒顺序（10 -> 20 -> 30）
        CoroutineTest.g_order = ""
        Int64 e3a = Coroutine.spawn2( "coroTimerMark", 30, "30" )
        Int64 e3b = Coroutine.spawn2( "coroTimerMark", 10, "10" )
        Int64 e3c = Coroutine.spawn2( "coroTimerMark", 20, "20" )
        Coroutine.waitAll3( e3a, e3b, e3c )
        check( "E3 定时器顺序", CoroutineTest.g_order == "102030" )
    }

    # ======================================================================
    # F 组：Channel 通信
    # ======================================================================
    static testGroupF()
    {
        global.println( "========== F: Channel 通信 ==========" )

        # F1 基本生产者-消费者（close 后 recv 得 null 终止）
        CoroutineTest.g_f1sum = 0
        Channel<object> ch1 = Channel<object>.create( 4 )
        Int64 f1p = Coroutine.spawn1( "coroF1Produce", ch1 )
        Int64 f1c = Coroutine.spawn1( "coroF1Consume", ch1 )
        Coroutine.waitAll2( f1p, f1c )
        check( "F1 基本生产消费", CoroutineTest.g_f1sum == 10 )
        check( "F1 关闭后 isClosed", ch1.isClosed == true )

        # F2 有界通道满时 Send 挂起让出（不忙等）
        CoroutineTest.g_f2sum = 0
        Channel<object> ch2 = Channel<object>.create( 2 )
        Int64 f2p = Coroutine.spawn1( "coroF2Produce", ch2 )
        Int64 f2c = Coroutine.spawn1( "coroF2Consume", ch2 )
        Coroutine.waitAll2( f2p, f2c )
        check( "F2 满时 Send 挂起让出", CoroutineTest.g_f2sum == 6 )

        # F3 多生产者单消费者：不丢不重
        CoroutineTest.g_f3count = 0
        Channel<object> ch3 = Channel<object>.create( 8 )
        Int64 f3c = Coroutine.spawn1( "coroF3Consume", ch3 )
        Int64 f3p0 = Coroutine.spawn1( "coroF3Produce", ch3 )
        Int64 f3p1 = Coroutine.spawn1( "coroF3Produce", ch3 )
        Int64 f3p2 = Coroutine.spawn1( "coroF3Produce", ch3 )
        Int64 f3p3 = Coroutine.spawn1( "coroF3Produce", ch3 )
        Coroutine.waitAll2( f3p0, f3p1 )
        Coroutine.waitAll2( f3p2, f3p3 )
        Coroutine.awaitFunction( f3c )
        check( "F3 多生产者不丢不重", CoroutineTest.g_f3count == 40 )

        # F4 单生产者多消费者：100 个值全被消费
        CoroutineTest.g_f4count = 0
        Channel<object> ch4 = Channel<object>.create( 8 )
        Int64 f4p = Coroutine.spawn1( "coroF4Produce", ch4 )
        Int64 f4c0 = Coroutine.spawn1( "coroF4Consume", ch4 )
        Int64 f4c1 = Coroutine.spawn1( "coroF4Consume", ch4 )
        Int64 f4c2 = Coroutine.spawn1( "coroF4Consume", ch4 )
        Int64 f4c3 = Coroutine.spawn1( "coroF4Consume", ch4 )
        Coroutine.waitAll2( f4c0, f4c1 )
        Coroutine.waitAll2( f4c2, f4c3 )
        Coroutine.awaitFunction( f4p )
        check( "F4 单生产者多消费者", CoroutineTest.g_f4count == 100 )
    }

    # ======================================================================
    # G 组：错误与取消
    # ======================================================================
    static testGroupG() throws
    {
        global.println( "========== G: 错误与取消 ==========" )

        # G1 协程内出错 -> await 处报错，错误值不变（SL 层异常可绑定 catch）
        Int64 g1 = Coroutine.spawn0( "coroThrowErr" )
        int code1 = 0
        label g1block
        {
            try Coroutine.awaitFunction( g1 )
        }
        catch CoroTestError ex
        {
            #code1 = ex.code
        }
        check( "G1 错误跨协程传播", code1 == 201 )

        # G2 未捕获错误的协程：状态 Dead（取消类 C 侧异常值为 null，须裸 catch）
        Int64 g2 = Coroutine.spawn0( "coroThrowErr" )
        label g2block
        {
            try Coroutine.awaitFunction( g2 )
        }
        catch
        {
        }
        check( "G2 出错协程状态 Dead", Coroutine.status( g2 ) == CoroutineStatus.Dead )

        # G3 嵌套：子协程抛 -> 父协程捕获
        Int64 g3 = Coroutine.spawn0( "coroCatchInner" )
        int rg3 = Coroutine.awaitFunction( g3 ) as int
        check( "G3 嵌套错误捕获", rg3 == 1 )

        # G4 协程内 finally 在挂起后正常执行
        CoroutineTest.g_done = false
        Int64 g4 = Coroutine.spawn0( "coroFinallySleep" )
        Coroutine.awaitFunction( g4 )
        check( "G4 挂起后 finally 执行", CoroutineTest.g_done )

        # G5 cancel：下个调度点以取消异常终止，finally 必须执行
        CoroutineTest.g_done = false
        Int64 g5 = Coroutine.spawn0( "coroCancelTarget" )
        Coroutine.sleep( 10 )    # 让目标先跑起来（进入 yield 循环）
        bool cancelled = Coroutine.cancel( g5 )
        bool threw5 = false
        label g5block
        {
            try Coroutine.awaitFunction( g5 )
        }
        catch
        {
            threw5 = true
        }
        check( "G5 取消时 finally 执行", cancelled && threw5 && CoroutineTest.g_done )

        # G6 cancel 对已结束协程返回 false
        Int64 g6 = Coroutine.spawn2( "coroAdd2", 1, 1 )
        int rg6 = Coroutine.awaitFunction( g6 ) as int
        check( "G6 取消已结束协程无影响", rg6 == 2 && Coroutine.cancel( g6 ) == false )
    }

    # ======================================================================
    # H 组：资源与边界
    # ======================================================================
    static testGroupH()
    {
        global.println( "========== H: 资源与边界 ==========" )

        # H1 深递归 200 层（协程帧链化解除旧 64 层限制）
        Int64 h1 = Coroutine.spawn1( "coroDeep", 200 )
        int rh1 = Coroutine.awaitFunction( h1 ) as int
        check( "H1 深递归 200 层", rh1 == 200 )

        # H5 协程间共享静态字段（协作式单线程下无撕裂）
        CoroutineTest.g_counter = 0
        List<Int64> incs = List<Int64>()
        for Int32 i = 0, i < 10, i = i + 1
        {
            incs.add( Coroutine.spawn0( "coroInc100" ) )
        }
        for v in incs
        {
            Coroutine.awaitFunction( v )
        }
        check( "H5 共享静态字段无丢失", CoroutineTest.g_counter == 1000 )

        # H7 协程内再 spawn（树状并发）
        Int64 h7 = Coroutine.spawn0( "coroNested" )
        int rh7 = Coroutine.awaitFunction( h7 ) as int
        check( "H7 协程内再 spawn", rh7 == 42 )

        # H2/H3/H4/H6 说明：
        #  - H2 栈溢出保护依赖 VM 栈上限策略，为避免整个测试进程崩溃，
        #    不在语言层触发，由 C 侧测试覆盖。
        #  - H3/H4 依赖 Gc.Collect / CoroutineCount API，本版本未开放，跳过。
        #  - H6 闭包捕获需要函数字面量（spawn 函数字面量前端不支持），跳过。
        global.println( "[CoroutineTest] H2/H3/H4/H6 由 C 侧测试或后续版本覆盖" )
    }

    # ======================================================================
    # I 组：与现有特性交互 / 限制
    # ======================================================================
    static testGroupI()
    {
        global.println( "========== I: 交互与限制 ==========" )

        # I1 子 VM（静态初始化器）禁止 spawn/await：本实现无关键字支持，
        #    库 API 形式下静态初始化器同样不应调用 Coroutine（由前端约束）。
        # I3 动态类型 await：句柄为 Int64 值类型，无 dynamic 关键字支持。
        # I4 泛型 cor<T>：句柄统一为 Int64，无泛型句柄形态。

        # I2 spawn 方法名按"全汇编简单名 + 参数个数"解析（无类名限定），
        #    故被 spawn 方法名必须全工程唯一；此处验证方法名解析正常。
        Int64 i2 = Coroutine.spawn0( "coroNested" )
        int ri2 = Coroutine.awaitFunction( i2 ) as int
        check( "I2 方法名全局解析（唯一名）", ri2 == 42 )
    }

    # ======================================================================
    # J 组：组合场景（冒烟）
    # ======================================================================
    static testGroupJ()
    {
        global.println( "========== J: 组合场景 ==========" )

        # J1 经典 pipeline：生产者 -> 处理(×2) -> 聚合
        CoroutineTest.g_j1sum = 0
        Channel<object> raw = Channel<object>.create( 4 )
        Channel<object> proc = Channel<object>.create( 4 )
        Int64 jp = Coroutine.spawn1( "coroJ1Produce", raw )
        Int64 jpr = Coroutine.spawn2( "coroJ1Process", raw, proc )
        Int64 ja = Coroutine.spawn1( "coroJ1Aggregate", proc )
        Coroutine.waitAll3( jp, jpr, ja )
        check( "J1 pipeline 聚合", CoroutineTest.g_j1sum == 9900 )

        # J2 并发扇出-扇入（50 个并行工作单元）
        List<Int64> works = List<Int64>()
        for Int32 i = 0, i < 50, i = i + 1
        {
            works.add( Coroutine.spawn1( "coroJ2Work", i ) )
        }
        int sum = 0
        for v in works
        {
            sum = sum + ( Coroutine.awaitFunction( v ) as int )
        }
        check( "J2 扇出-扇入", sum == 1275 )

        # J3 公平性 + 超时混合
        Int64 j3 = Coroutine.spawn1( "coroSleepMs", 500 )
        bool ok1 = Coroutine.waitTimeout( j3, 100 )
        Int32 st3 = Coroutine.status( j3 )
        bool ok2 = Coroutine.waitTimeout( j3, 1000 )
        check( "J3 超时未完成后再等到完成", ok1 == false && st3 == CoroutineStatus.Suspended && ok2 == true )
    }

    # ======================================================================
    # 入口
    # ======================================================================
    static fun()
    {
        global.println( "========== CoroutineTest (start) ==========" )

        CoroutineTest.testGroupA()
        CoroutineTest.testGroupB()
        CoroutineTest.testGroupC()
        CoroutineTest.testGroupD()
        CoroutineTest.testGroupE()
        CoroutineTest.testGroupF()
        CoroutineTest.testGroupG()
        CoroutineTest.testGroupH()
        CoroutineTest.testGroupI()
        CoroutineTest.testGroupJ()

        global.println( "========== CoroutineTest (end) ==========" )
    }
}
