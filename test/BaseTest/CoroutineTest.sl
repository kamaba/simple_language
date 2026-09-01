# ============================================================================
# CoroutineTest.sl -- 协程（Coroutine / CoroutineManager）功能全量测试
#
# 参考设计档：md/design/COROUTINE_DESIGN.md 第 8 章（A-J 组验收用例）。
# 类型体系（见 Core/Coroutine.sl）：
#   CoroutineManager -- 静态管理器：spawn0..3 / spawnClosure0..3 / yieldNow /
#     sleep / current / status / blockedReason / awaitFunction / waitAll2/3 /
#     waitAny2/3 / nextCompleted2/3 / waitTimeout / cancel（均收发 Coroutine 对象）
#   Coroutine -- 协程对象（spawn 系列的返回类型）：await()/cancel() 实例方法，
#     status/blockedReason/isDead/handle 查询属性
#   关键字 yield/await/spawn 为语法糖（Node 层展开），分别对应
#     CoroutineManager.yieldNow / awaitFunction 与 spawnClosureN。
#   另含 Channel<T>（send / recv / close / count / isClosed）。
#
# 编写约定（重要）：
#  1. 被 spawn 的方法按"简单名 + 参数个数"在整个汇编内全局解析
#     （vm_find_method_entry_by_name），故本文件所有被 spawn 方法均以
#     coro / coroKw 前缀命名，保证全工程唯一（本测试工程编译几十个 .sl 在一起）。
#  2. C VM 侧抛出的异常（取消 -63 / 非法操作 -64）其异常值为 null，
#     捕获时必须用裸 catch{}（不绑定变量）；SL 层 throw 的枚举异常
#     才可用 catch XxxError ex 绑定。
#  3. 前端不发射 SCHED_CHECK（回边公平性指令），调度公平性用例以
#     显式 CoroutineManager.yieldNow() 保证交替。
#  4. 主入口 static fun() 被包装为 root 协程（vm_scheduler_enter），
#     因此 await/yield/sleep/current 从主入口调用全部有效。
#  5. 设计档中的语法糖用例（spawn 函数字面量 / await 数组语法糖 /
#     cor.All 变参）在本实现中不存在，以 spawn0..3 + waitAll2/3 固定
#     参数重载等价替代。
#  6. 本文件已合并原 CoroutineKeywordTest.sl（关键字 / 函数类型用例并入
#     K 组）并删除重复测试点：原"yield 让出顺序 1B2"（= D2）、"await
#     表达式/语句取值"（= A1 / K9 语句形态）、"协程内 ret await"
#     （= B5）、"I2 方法名全局解析"（= H7）、"K8 直接调用"（= K3）。
#     L 组为 Coroutine 对象 API 专项。
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
    static int g_sum = 0

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
        CoroutineManager.sleep( 50 )
        CoroutineTest.g_order = CoroutineTest.g_order + "e"
        ret 1
    }

    # B5：协程内 return await
    static int coroIndirect()
    {
        Coroutine h = CoroutineManager.spawn2( "coroAdd2", 5, 6 )
        ret CoroutineManager.awaitFunction( h ) as int
    }

    # 通用：睡眠指定毫秒（void 协程）
    static coroSleepMs( int ms )
    {
        CoroutineManager.sleep( ms )
    }

    # D1：公平性 A 方（10 次追加 + 显式让出）
    static coroFairA()
    {
        for Int32 i = 0, i < 10, i = i + 1
        {
            CoroutineTest.g_order = CoroutineTest.g_order + "A"
            CoroutineManager.yieldNow()
        }
    }

    # D1：公平性 B 方
    static coroFairB()
    {
        for Int32 i = 0, i < 10, i = i + 1
        {
            CoroutineTest.g_order = CoroutineTest.g_order + "B"
            CoroutineManager.yieldNow()
        }
    }

    # D2：yield 让出方（记 1 -> 让出 -> 记 2）
    static coroYieldA()
    {
        CoroutineTest.g_order = CoroutineTest.g_order + "1"
        CoroutineManager.yieldNow()
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
        CoroutineManager.sleep( ms )
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
        Coroutine h = CoroutineManager.spawn0( "coroThrowErr" )
        int caught = 0
        label innerBlock
        {
            try CoroutineManager.awaitFunction( h )
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
            CoroutineManager.sleep( 10 )
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
                CoroutineManager.yieldNow()
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
        Coroutine h = CoroutineManager.spawn1( "coroDeep", n - 1 )
        ret ( CoroutineManager.awaitFunction( h ) as int ) + 1
    }

    # H5：静态计数 100 次
    static coroInc100()
    {
        for Int32 j = 0, j < 100, j = j + 1
        {
            CoroutineTest.g_counter = CoroutineTest.g_counter + 1
        }
    }

    # H7：协程内再 spawn
    static int coroNested()
    {
        Coroutine h = CoroutineManager.spawn2( "coroAdd2", 40, 2 )
        ret CoroutineManager.awaitFunction( h ) as int
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
        CoroutineManager.sleep( 10 )
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
        CoroutineManager.sleep( 2 )
        ret i + 1
    }

    # ======================================================================
    # K 组被 spawn 的静态方法（coroKw 前缀全工程唯一）
    # ======================================================================

    # K6：协程体内组合 function 声明 + spawn + await + yield（帧保持）
    static int coroKwPipeline()
    {
        function f = function( int a, int b )
        {
            ret a + b;
        }
        Coroutine h = spawn f( 10, 20 )
        int r = await h as int
        yield
        ret r
    }

    # K7：参数类型为 Func<签名>：函数体内直接调用
    static int coroKwApplyFunc( Func<int,int,int> fn, int a, int b )
    {
        ret fn( a, b )
    }

    # K7：参数类型为 Func<签名>：函数体内 spawn 后 await 取回
    static int coroKwSpawnFunc( Func<int,int,int> fn, int a, int b )
    {
        Coroutine h = spawn fn( a, b )
        ret await h as int
    }

    # K7：参数类型为 Function 内置类型（宽松）：返回 object
    static object coroKwApplyLoose( Function fn, int a, int b )
    {
        ret fn( a, b )
    }

    # K7：参数类型为 Func<void,参数...>：闭包作返回值传出
    static Func<void,int,int> coroKwMakeAccum()
    {
        Func<void,int,int> fn = function( int a, int b )
        {
            g_sum = g_sum + a * b;
        }
        ret fn
    }

    # ======================================================================
    # A 组：spawn 基础
    # ======================================================================
    static testGroupA()
    {
        global.println( "========== A: spawn 基础 ==========" )

        # A1 基本 spawn + await 取回返回值
        Coroutine h1 = CoroutineManager.spawn2( "coroAdd2", 3, 4 )
        int r1 = CoroutineManager.awaitFunction( h1 ) as int
        check( "A1 spawn+await 返回值", r1 == 7 )

        # A2 fire-and-forget：不 await 也要跑完（副作用可见）
        CoroutineTest.g_done = false
        CoroutineManager.spawn0( "coroSetFlag" )
        for Int32 i = 0, i < 1000, i = i + 1
        {
            if CoroutineTest.g_done
            {
                break
            }
            CoroutineManager.sleep( 1 )
        }
        check( "A2 不 await 也执行完", CoroutineTest.g_done )

        # A3 多参数
        Coroutine h3 = CoroutineManager.spawn3( "coroSum3", 1, 2, 3 )
        int r3 = CoroutineManager.awaitFunction( h3 ) as int
        check( "A3 三参数", r3 == 6 )

        # A4 句柄与状态
        Coroutine h4 = CoroutineManager.spawn3( "coroSum3", 1, 2, 3 )
        Int32 st1 = CoroutineManager.status( h4 )
        bool fresh = st1 == CoroutineStatus.Created || st1 == CoroutineStatus.Ready || st1 == CoroutineStatus.Running
        CoroutineManager.awaitFunction( h4 )
        check( "A4 新建状态为创建/就绪/运行", fresh )
        check( "A4 结束后状态 Dead", CoroutineManager.status( h4 ) == CoroutineStatus.Dead )

        # A5 void 协程：await 得 null
        Coroutine h5 = CoroutineManager.spawn0( "coroVoid" )
        object r5 = CoroutineManager.awaitFunction( h5 )
        check( "A5 void 协程 await 得 null", r5 == null )

        # A6 返回值是数组
        Coroutine h6 = CoroutineManager.spawn0( "coroMakeArr" )
        Array<Int32> arr = CoroutineManager.awaitFunction( h6 ) as Array<Int32>
        check( "A6 返回数组", arr != null && arr.length == 3 && arr._getItem_( 2 ) == 3 )

        # A7（设计档 spawn 函数字面量）：由 K5 匿名闭包形态等价覆盖
    }

    # ======================================================================
    # B 组：await 基础与串行/并行
    # ======================================================================
    static testGroupB() throws
    {
        global.println( "========== B: await 基础 ==========" )

        # B1 await 已完成协程 = 同步返回不挂起
        Coroutine b1a = CoroutineManager.spawn2( "coroAdd2", 1, 2 )
        CoroutineManager.awaitFunction( b1a )
        Coroutine b1b = CoroutineManager.spawn2( "coroAdd2", 2, 3 )
        int rb1 = CoroutineManager.awaitFunction( b1b ) as int
        check( "B1 已完成协程直接取值", rb1 == 5 )

        # B2 串行执行：第二个等第一个完成后才启动
        CoroutineTest.g_order = ""
        Coroutine b2a = CoroutineManager.spawn0( "coroTrack" )
        CoroutineManager.awaitFunction( b2a )
        Coroutine b2b = CoroutineManager.spawn0( "coroTrack" )
        CoroutineManager.awaitFunction( b2b )
        check( "B2 串行执行顺序", CoroutineTest.g_order == "sese" )

        # B3 并行执行 + 串行消费
        CoroutineTest.g_order = ""
        Coroutine b3a = CoroutineManager.spawn0( "coroTrack" )
        Coroutine b3b = CoroutineManager.spawn0( "coroTrack" )
        CoroutineManager.awaitFunction( b3a )
        CoroutineManager.awaitFunction( b3b )
        check( "B3 并行执行", CoroutineTest.g_order == "ssee" )

        # B4 await 嵌套表达式
        Coroutine b4 = CoroutineManager.spawn2( "coroAdd2", 10, 20 )
        check( "B4 await 嵌套表达式", 1 + ( CoroutineManager.awaitFunction( b4 ) as int ) == 31 )

        # B5 协程内 return await
        Coroutine b5 = CoroutineManager.spawn0( "coroIndirect" )
        int rb5 = CoroutineManager.awaitFunction( b5 ) as int
        check( "B5 协程内 return await", rb5 == 11 )

        # B6 await 自己 -> 运行期错误（C 侧抛非法操作，异常值为 null，须裸 catch）
        Coroutine self = CoroutineManager.current()
        bool threw6 = false
        label b6block
        {
            try CoroutineManager.awaitFunction( self )
        }
        catch
        {
            threw6 = true
        }
        check( "B6 await 自己报错", threw6 && self != null )
    }

    # ======================================================================
    # C 组：批量等待
    # ======================================================================
    static testGroupC() throws
    {
        global.println( "========== C: 批量等待 ==========" )

        # C1 waitAll2 + await 逐个取回
        Coroutine c1a = CoroutineManager.spawn2( "coroAdd2", 1, 1 )
        Coroutine c1b = CoroutineManager.spawn2( "coroAdd2", 2, 2 )
        CoroutineManager.waitAll2( c1a, c1b )
        int rc1a = CoroutineManager.awaitFunction( c1a ) as int
        int rc1b = CoroutineManager.awaitFunction( c1b ) as int
        check( "C1 waitAll2 + await 取回", rc1a == 2 && rc1b == 4 )

        # C2 waitAll3 + await 逐个取回
        Coroutine c2a = CoroutineManager.spawn3( "coroSum3", 1, 1, 1 )
        Coroutine c2b = CoroutineManager.spawn2( "coroAdd2", 2, 2 )
        Coroutine c2c = CoroutineManager.spawn2( "coroAdd2", 3, 3 )
        CoroutineManager.waitAll3( c2a, c2b, c2c )
        int rc2a = CoroutineManager.awaitFunction( c2a ) as int
        int rc2b = CoroutineManager.awaitFunction( c2b ) as int
        int rc2c = CoroutineManager.awaitFunction( c2c ) as int
        check( "C2 waitAll3 + await 取回", rc2a == 3 && rc2b == 4 && rc2c == 6 )

        # C3 waitAny2：先完成者胜出（注册表保证同一句柄同一实例，== 即引用判等）
        Coroutine c3slow = CoroutineManager.spawn1( "coroSleepMs", 100 )
        Coroutine c3fast = CoroutineManager.spawn1( "coroSleepMs", 10 )
        Coroutine winner = CoroutineManager.waitAny2( c3slow, c3fast )
        check( "C3 waitAny2 快者胜出", winner == c3fast )
        CoroutineManager.awaitFunction( c3slow )    # 清理：等慢者也结束

        # C4 waitAny3：三个不同时长，最快者胜
        Coroutine c4a = CoroutineManager.spawn1( "coroSleepMs", 60 )
        Coroutine c4b = CoroutineManager.spawn1( "coroSleepMs", 10 )
        Coroutine c4c = CoroutineManager.spawn1( "coroSleepMs", 30 )
        Coroutine winner3 = CoroutineManager.waitAny3( c4a, c4b, c4c )
        check( "C4 waitAny3 最快者胜出", winner3 == c4b )
        CoroutineManager.waitAll3( c4a, c4b, c4c )    # 清理

        # C5 nextCompleted：非阻塞消费（按参数顺序返回第一个 Dead 且未消费者）
        Coroutine c5a = CoroutineManager.spawn1( "coroSleepMs", 50 )
        Coroutine c5b = CoroutineManager.spawn1( "coroSleepMs", 10 )
        CoroutineManager.sleep( 20 )    # 此刻 c5b 已完成、c5a 未完成
        Coroutine got1 = CoroutineManager.nextCompleted2( c5a, c5b )
        Coroutine got2 = CoroutineManager.nextCompleted2( c5a, c5b )
        check( "C5 nextCompleted 部分完成", got1 == c5b && got2 == null )
        CoroutineManager.awaitFunction( c5a )    # c5a 完成后仍可消费
        Coroutine got3 = CoroutineManager.nextCompleted2( c5a, c5b )
        Coroutine got4 = CoroutineManager.nextCompleted2( c5a, c5b )
        check( "C5 nextCompleted 消费与耗尽", got3 == c5a && got4 == null )

        # C6 waitAll2 中某协程出错 -> 立即失败并取消其余
        Coroutine c6a = CoroutineManager.spawn0( "coroThrowErr" )
        Coroutine c6b = CoroutineManager.spawn1( "coroSleepMs", 1000 )
        int caughtCode = 0
        label c6block
        {
            try CoroutineManager.waitAll2( c6a, c6b )
        }
        catch CoroTestError ex
        {
            # catch 绑定变量静态类型为 object（无字段访问），与枚举成员比较
            if ex == CoroTestError.BoomError
            {
                caughtCode = 201
            }
        }
        label c6cleanup
        {
            try CoroutineManager.awaitFunction( c6b )    # 等被取消者也真正结束（裸 catch）
        }
        catch
        {
        }
        check( "C6 waitAll 错误传播并取消其余", caughtCode == 201 && CoroutineManager.status( c6b ) == CoroutineStatus.Dead )
    }

    # ======================================================================
    # D 组：让出与调度公平性
    # ======================================================================
    static testGroupD()
    {
        global.println( "========== D: 让出与调度公平性 ==========" )

        # D1 两个协程显式让出 -> 严格交替（前端无 SCHED_CHECK，用显式 yield）
        CoroutineTest.g_order = ""
        Coroutine d1a = CoroutineManager.spawn0( "coroFairA" )
        Coroutine d1b = CoroutineManager.spawn0( "coroFairB" )
        CoroutineManager.waitAll2( d1a, d1b )
        check( "D1 显式 yield 交替执行", CoroutineTest.g_order == "ABABABABABABABABABAB" )

        # D2 yield 让出后，其它就绪协程先跑
        CoroutineTest.g_order = ""
        Coroutine d2a = CoroutineManager.spawn0( "coroYieldA" )
        Coroutine d2b = CoroutineManager.spawn0( "coroPlainB" )
        CoroutineManager.waitAll2( d2a, d2b )
        check( "D2 yield 让出后 B 先完成", CoroutineTest.g_order == "1B2" )

        # D3 1000 个协程：无栈溢出、无死锁
        int sum = 0
        List<Coroutine> tasks = List<Coroutine>()
        for Int32 i = 0, i < 10, i = i + 1
        {
            tasks.add( CoroutineManager.spawn2( "coroAdd2", i, 1 ) )
        }
        for v in tasks
        {
            sum = sum + ( CoroutineManager.awaitFunction( v ) as int )
        }
        check( "D3 1000 协程全部完成", sum == 500500 )

        # D4 后台协程睡醒后自然结束（调度器可正常收敛退出）
        Coroutine d4 = CoroutineManager.spawn1( "coroSleepMs", 5 )
        object rd4 = CoroutineManager.awaitFunction( d4 )
        check( "D4 无就绪协程时正常收敛", CoroutineManager.status( d4 ) == CoroutineStatus.Dead && rd4 == null )
    }

    # ======================================================================
    # E 组：定时器与 Sleep
    # ======================================================================
    static testGroupE()
    {
        global.println( "========== E: 定时器与 Sleep ==========" )

        # E1 并行 Sleep：总耗时 ≈ max，不是 sum
        Int64 t0 = Environment.nowMillis()
        Coroutine e1a = CoroutineManager.spawn1( "coroSleepMs", 100 )
        Coroutine e1b = CoroutineManager.spawn1( "coroSleepMs", 100 )
        CoroutineManager.waitAll2( e1a, e1b )
        Int64 dt = Environment.nowMillis() - t0
        check( "E1 Sleep 并行总时长≈max", dt >= 100 && dt < 190 )

        # E2 Sleep(0) 只让出不阻塞
        Coroutine e2 = CoroutineManager.spawn1( "coroSleepMs", 0 )
        object re2 = CoroutineManager.awaitFunction( e2 )
        check( "E2 Sleep(0)", CoroutineManager.status( e2 ) == CoroutineStatus.Dead && re2 == null )

        # E3 定时器唤醒顺序（10 -> 20 -> 30）
        CoroutineTest.g_order = ""
        Coroutine e3a = CoroutineManager.spawn2( "coroTimerMark", 30, "30" )
        Coroutine e3b = CoroutineManager.spawn2( "coroTimerMark", 10, "10" )
        Coroutine e3c = CoroutineManager.spawn2( "coroTimerMark", 20, "20" )
        CoroutineManager.waitAll3( e3a, e3b, e3c )
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
        Coroutine f1p = CoroutineManager.spawn1( "coroF1Produce", ch1 )
        Coroutine f1c = CoroutineManager.spawn1( "coroF1Consume", ch1 )
        CoroutineManager.waitAll2( f1p, f1c )
        check( "F1 基本生产消费", CoroutineTest.g_f1sum == 10 )
        check( "F1 关闭后 isClosed", ch1.isClosed == true )

        # F2 有界通道满时 Send 挂起让出（不忙等）
        CoroutineTest.g_f2sum = 0
        Channel<object> ch2 = Channel<object>.create( 2 )
        Coroutine f2p = CoroutineManager.spawn1( "coroF2Produce", ch2 )
        Coroutine f2c = CoroutineManager.spawn1( "coroF2Consume", ch2 )
        CoroutineManager.waitAll2( f2p, f2c )
        check( "F2 满时 Send 挂起让出", CoroutineTest.g_f2sum == 6 )

        # F3 多生产者单消费者：不丢不重
        CoroutineTest.g_f3count = 0
        Channel<object> ch3 = Channel<object>.create( 8 )
        Coroutine f3c = CoroutineManager.spawn1( "coroF3Consume", ch3 )
        Coroutine f3p0 = CoroutineManager.spawn1( "coroF3Produce", ch3 )
        Coroutine f3p1 = CoroutineManager.spawn1( "coroF3Produce", ch3 )
        Coroutine f3p2 = CoroutineManager.spawn1( "coroF3Produce", ch3 )
        Coroutine f3p3 = CoroutineManager.spawn1( "coroF3Produce", ch3 )
        CoroutineManager.waitAll2( f3p0, f3p1 )
        CoroutineManager.waitAll2( f3p2, f3p3 )
        CoroutineManager.awaitFunction( f3c )
        check( "F3 多生产者不丢不重", CoroutineTest.g_f3count == 40 )

        # F4 单生产者多消费者：100 个值全被消费
        CoroutineTest.g_f4count = 0
        Channel<object> ch4 = Channel<object>.create( 8 )
        Coroutine f4p = CoroutineManager.spawn1( "coroF4Produce", ch4 )
        Coroutine f4c0 = CoroutineManager.spawn1( "coroF4Consume", ch4 )
        Coroutine f4c1 = CoroutineManager.spawn1( "coroF4Consume", ch4 )
        Coroutine f4c2 = CoroutineManager.spawn1( "coroF4Consume", ch4 )
        Coroutine f4c3 = CoroutineManager.spawn1( "coroF4Consume", ch4 )
        CoroutineManager.waitAll2( f4c0, f4c1 )
        CoroutineManager.waitAll2( f4c2, f4c3 )
        CoroutineManager.awaitFunction( f4p )
        check( "F4 单生产者多消费者", CoroutineTest.g_f4count == 100 )
    }

    # ======================================================================
    # G 组：错误与取消
    # ======================================================================
    static testGroupG() throws
    {
        global.println( "========== G: 错误与取消 ==========" )

        # G1 协程内出错 -> await 处报错，错误值不变（SL 层异常可绑定 catch）
        Coroutine g1 = CoroutineManager.spawn0( "coroThrowErr" )
        int code1 = 0
        label g1block
        {
            try CoroutineManager.awaitFunction( g1 )
        }
        catch CoroTestError ex
        {
            # catch 绑定变量静态类型为 object（无字段访问），与枚举成员比较
            if ex == CoroTestError.BoomError
            {
                code1 = 201
            }
        }
        check( "G1 错误跨协程传播", code1 == 201 )

        # G2 未捕获错误的协程：状态 Dead（取消类 C 侧异常值为 null，须裸 catch）
        Coroutine g2 = CoroutineManager.spawn0( "coroThrowErr" )
        label g2block
        {
            try CoroutineManager.awaitFunction( g2 )
        }
        catch
        {
        }
        check( "G2 出错协程状态 Dead", CoroutineManager.status( g2 ) == CoroutineStatus.Dead )

        # G3 嵌套：子协程抛 -> 父协程捕获
        Coroutine g3 = CoroutineManager.spawn0( "coroCatchInner" )
        int rg3 = CoroutineManager.awaitFunction( g3 ) as int
        check( "G3 嵌套错误捕获", rg3 == 1 )

        # G4 协程内 finally 在挂起后正常执行
        CoroutineTest.g_done = false
        Coroutine g4 = CoroutineManager.spawn0( "coroFinallySleep" )
        CoroutineManager.awaitFunction( g4 )
        check( "G4 挂起后 finally 执行", CoroutineTest.g_done )

        # G5 cancel：下个调度点以取消异常终止，finally 必须执行
        CoroutineTest.g_done = false
        Coroutine g5 = CoroutineManager.spawn0( "coroCancelTarget" )
        CoroutineManager.sleep( 10 )    # 让目标先跑起来（进入 yield 循环）
        bool cancelled = CoroutineManager.cancel( g5 )
        bool threw5 = false
        label g5block
        {
            try CoroutineManager.awaitFunction( g5 )
        }
        catch
        {
            threw5 = true
        }
        check( "G5 取消时 finally 执行", cancelled && threw5 && CoroutineTest.g_done )

        # G6 cancel 对已结束协程返回 false
        Coroutine g6 = CoroutineManager.spawn2( "coroAdd2", 1, 1 )
        int rg6 = CoroutineManager.awaitFunction( g6 ) as int
        check( "G6 取消已结束协程无影响", rg6 == 2 && CoroutineManager.cancel( g6 ) == false )
    }

    # ======================================================================
    # H 组：资源与边界
    # ======================================================================
    static testGroupH()
    {
        global.println( "========== H: 资源与边界 ==========" )

        # H1 深递归 200 层（协程帧链化解除旧 64 层限制）
        Coroutine h1 = CoroutineManager.spawn1( "coroDeep", 200 )
        int rh1 = CoroutineManager.awaitFunction( h1 ) as int
        check( "H1 深递归 200 层", rh1 == 200 )

        # H5 协程间共享静态字段（协作式单线程下无撕裂）
        CoroutineTest.g_counter = 0
        List<Coroutine> incs = List<Coroutine>()
        for Int32 i = 0, i < 10, i = i + 1
        {
            incs.add( CoroutineManager.spawn0( "coroInc100" ) )
        }
        for v in incs
        {
            CoroutineManager.awaitFunction( v )
        }
        check( "H5 共享静态字段无丢失", CoroutineTest.g_counter == 1000 )

        # H7 协程内再 spawn（树状并发）
        Coroutine h7 = CoroutineManager.spawn0( "coroNested" )
        int rh7 = CoroutineManager.awaitFunction( h7 ) as int
        check( "H7 协程内再 spawn", rh7 == 42 )

        # H2/H3/H4/H6 说明：
        #  - H2 栈溢出保护依赖 VM 栈上限策略，为避免整个测试进程崩溃，
        #    不在语言层触发，由 C 侧测试覆盖。
        #  - H3/H4 依赖 Gc.Collect / CoroutineCount API，本版本未开放，跳过。
        #  - H6 捕获局部变量的闭包协程（帧保持）由 C 侧测试覆盖。
        # 原 I 组说明：I1 静态初始化器禁 spawn/await、I3 动态类型 await、
        #  - I4 泛型句柄均无对应实现；I2 方法名全局解析与 H7 重复，已删除。
        global.println( "[CoroutineTest] H2/H3/H4/H6 由 C 侧测试或后续版本覆盖" )
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
        Coroutine jp = CoroutineManager.spawn1( "coroJ1Produce", raw )
        Coroutine jpr = CoroutineManager.spawn2( "coroJ1Process", raw, proc )
        Coroutine ja = CoroutineManager.spawn1( "coroJ1Aggregate", proc )
        CoroutineManager.waitAll3( jp, jpr, ja )
        check( "J1 pipeline 聚合", CoroutineTest.g_j1sum == 9900 )

        # J2 并发扇出-扇入（50 个并行工作单元）
        List<Coroutine> works = List<Coroutine>()
        for Int32 i = 0, i < 50, i = i + 1
        {
            works.add( CoroutineManager.spawn1( "coroJ2Work", i ) )
        }
        int sum = 0
        for v in works
        {
            sum = sum + ( CoroutineManager.awaitFunction( v ) as int )
        }
        check( "J2 扇出-扇入", sum == 1275 )

        # J3 公平性 + 超时混合
        Coroutine j3 = CoroutineManager.spawn1( "coroSleepMs", 500 )
        bool ok1 = CoroutineManager.waitTimeout( j3, 100 )
        Int32 st3 = CoroutineManager.status( j3 )
        bool ok2 = CoroutineManager.waitTimeout( j3, 1000 )
        check( "J3 超时未完成后再等到完成", ok1 == false && st3 == CoroutineStatus.Suspended && ok2 == true )
    }

    # ======================================================================
    # K 组：关键字（spawn / await / yield）与函数类型
    #（自 CoroutineKeywordTest.sl 合并，重复测试点已删除，见文件头第 6 条）
    # ======================================================================
    static testGroupK()
    {
        global.println( "========== K: 关键字与函数类型 ==========" )

        # K1 yield 关键字：root 协程内直接 yield（无可切换者，应空操作不崩溃）
        #（原"1B2 让出顺序"用例与 D2 重复，已删除）
        yield
        check( "K1 root 协程 yield 空操作", true )

        # K3 function 宽松函数类型声明 + spawn 函数变量
        function adder = function( int a, int b )
        {
            ret a + b;
        }
        int direct = adder( 10, 20 )
        check( "K3 function 变量直接调用", direct == 30 )

        # spawn 函数变量（2 参）: spawn adder(1,2) -> CoroutineManager.spawnClosure2(adder,1,2)
        Coroutine h = spawn adder( 1, 2 )
        int r = await h as int
        check( "K3 spawn function 变量(2参)", r == 3 )

        # 1 参 / 0 参形态
        CoroutineTest.g_sum = 0
        function acc1 = function( int v )
        {
            g_sum = g_sum + v;
        }
        Coroutine h1 = spawn acc1( 100 )
        await h1
        function acc0 = function()
        {
            g_sum = g_sum + 5;
        }
        Coroutine h0 = spawn acc0()
        await h0
        check( "K3 spawn function 变量(0/1参)", CoroutineTest.g_sum == 105 )

        # function 变量再赋值后调用（同为宽松 function 类型）
        function alias = adder
        object ra = alias( 3, 4 )
        check( "K3 function 变量再赋值调用", ( ra as int ) == 7 )

        # K4 Func<返回类型,参数类型...> 签名类型 + spawn
        function mul = function( int a, int b )
        {
            ret a * b;
        }
        Func<int,int,int> mulf = mul
        int direct4 = mulf( 6, 7 )
        check( "K4 Func<> 变量直接调用", direct4 == 42 )

        Coroutine hm = spawn mulf( 2, 5 )
        int rm = await hm as int
        check( "K4 spawn Func<> 变量", rm == 10 )

        # Func<void,参数类型...> 声明 + 匿名闭包直赋（void 仅允许返回类型位置）
        CoroutineTest.g_sum = 0
        Func<void,int,int> accum = function( int a, int b )
        {
            g_sum = g_sum + a + b;
        }
        accum( 100, 200 )
        Coroutine h2 = spawn accum( 1, 2 )
        await h2
        check( "K4 Func<> 匿名闭包直赋 + spawn", CoroutineTest.g_sum == 303 )

        # K5 spawn 匿名闭包（提升为具名闭包后 spawnClosure0），体内可用 yield 挂起
        CoroutineTest.g_sum = 0
        CoroutineTest.g_order = ""
        Coroutine ha = spawn function()
        {
            g_sum = g_sum + 55;
            g_order = g_order + "x";
            yield
            g_order = g_order + "y";
        }
        await ha
        check( "K5 spawn 匿名闭包(含 yield)", CoroutineTest.g_sum == 55 && CoroutineTest.g_order == "xy" )

        # 语句形态（fire-and-forget，用库 API await 收尾）
        Coroutine hb = spawn function()
        {
            g_sum = g_sum + 1;
        }
        CoroutineManager.awaitFunction( hb )
        check( "K5 spawn 匿名闭包(语句形态)", CoroutineTest.g_sum == 56 )

        # K6 协程体内组合（函数变量 + spawn + await + yield 帧保持）
        Coroutine hp = CoroutineManager.spawn0( "coroKwPipeline" )
        int pr = await hp as int
        check( "K6 协程体内 spawn/await/yield 组合", pr == 30 )

        # K7 函数值作为参数传递（Func<> 签名 / Function 宽松 / 闭包返回值）
        int d7 = CoroutineTest.coroKwApplyFunc( adder, 3, 4 )
        check( "K7 Func<> 参数直接调用", d7 == 7 )

        int r7 = CoroutineTest.coroKwSpawnFunc( adder, 10, 20 )
        check( "K7 Func<> 参数体内 spawn", r7 == 30 )

        object ro = CoroutineTest.coroKwApplyLoose( adder, 5, 6 )
        check( "K7 Function 参数宽松调用", ( ro as int ) == 11 )

        CoroutineTest.g_sum = 0
        Func<void,int,int> acc = CoroutineTest.coroKwMakeAccum()
        acc( 3, 4 )
        check( "K7 闭包作返回值传出调用", CoroutineTest.g_sum == 12 )

        # K8 spawn 3 参函数变量（直接调用用例与 K3 重复，已删除）
        function calc3 = function( int a, int b, int c )
        {
            ret a * 100 + b * 10 + c;
        }
        Coroutine h3k = spawn calc3( 4, 5, 6 )
        int r8 = await h3k as int
        check( "K8 spawn 3 参函数变量", r8 == 456 )

        # K9 Func<返回类型> 无参签名 + spawn
        Func<int> mk = function()
        {
            ret 88;
        }
        int d9 = mk()
        check( "K9 Func<int> 无参直接调用", d9 == 88 )

        Coroutine h9 = spawn mk()
        int r9 = await h9 as int
        check( "K9 spawn Func<int> 无参", r9 == 88 )

        # Func<void> 无参无返回（直接调用 + spawn 各执行一次: 9 + 9 = 18）
        CoroutineTest.g_sum = 0
        Func<void> eff = function()
        {
            g_sum = g_sum + 9;
        }
        eff()
        Coroutine h9b = spawn eff()
        await h9b
        check( "K9 Func<void> 无参 + spawn", CoroutineTest.g_sum == 18 )
    }

    # ======================================================================
    # L 组：Coroutine 对象 API（实例方法与查询属性）
    # ======================================================================
    static testGroupL()
    {
        global.println( "========== L: Coroutine 对象 API ==========" )

        # L1 实例方法 await 取回返回值
        Coroutine l1 = CoroutineManager.spawn2( "coroAdd2", 7, 8 )
        int rl1 = l1.awaitHandle() as int
        check( "L1 实例 await 取回返回值", rl1 == 15 )

        # L2 查询属性：status / blockedReason / isDead / handle
        Coroutine l2 = CoroutineManager.spawn1( "coroSleepMs", 50 )
        CoroutineManager.sleep( 10 )    # 此刻 l2 已进入休眠挂起
        bool suspended = l2.status == CoroutineStatus.Suspended
        bool sleeping = l2.blockedReason == CoroutineBlockReason.Sleep
        bool alive = ( l2.isDead == false ) && ( l2.handle != 0 )
        CoroutineManager.awaitFunction( l2 )
        check( "L2 挂起中状态/原因", suspended && sleeping )
        check( "L2 属性 isDead/handle", alive )
        check( "L2 结束后 isDead", l2.isDead && l2.status == CoroutineStatus.Dead )

        # L3 注册表同一性：同一句柄总是同一实例（== 引用判等）
        Coroutine cur1 = CoroutineManager.current()
        Coroutine cur2 = CoroutineManager.current()
        check( "L3 current 同一实例", cur1 != null && cur1 == cur2 )

        # L4 实例方法 cancel：对已结束协程返回 false
        Coroutine l4 = CoroutineManager.spawn2( "coroAdd2", 1, 1 )
        int rl4 = CoroutineManager.awaitFunction( l4 ) as int
        check( "L4 实例 cancel 已结束返回 false", rl4 == 2 && l4.cancel() == false )
    }

    static testGroupM()
    {
        var cor1 = spawn function(){
            global.println( "CoroutineTest: 协程测试开始" )
            yield;
            CoroutineManager.sleep( 1000 )
            global.println( "CoroutineTest: 协程测试开始2" )
            yield;
            CoroutineManager.sleep( 1000 )
            global.println( "CoroutineTest: 协程测试开始3" )
        };

    }

    # ======================================================================
    # 入口
    # ======================================================================
    static fun()
    {
        global.println( "========== CoroutineTest (start) ==========" )
\
        #!
        CoroutineTest.testGroupA()
        CoroutineTest.testGroupB()
        CoroutineTest.testGroupC()
        CoroutineTest.testGroupD()
        CoroutineTest.testGroupE()
        CoroutineTest.testGroupF()
        CoroutineTest.testGroupG()
        CoroutineTest.testGroupH()
        CoroutineTest.testGroupJ()
        CoroutineTest.testGroupK()
        CoroutineTest.testGroupL()
        !#
        CoroutineTest.testGroupM()

        global.println( "========== CoroutineTest (end) ==========" )
    }
}
