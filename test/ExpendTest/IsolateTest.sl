import Std;
import Core;

# ============================================================
# IsolateTest —— Std/Isolate 机制验收测试（ISOLATE_DESIGN.md §9 A~I 组）
#
# 约定：
#  - worker 入口全部使用闭包：捕获环境随闭包深拷贝进 worker VM，
#    worker 内修改不影响源（A6/B1/C 组验证）。
#  - 语言限制：匿名闭包字面量不能直接作为调用实参，
#    一律先赋给 function 变量再传参（同 Std/Isolate/ReceivePort.listen）。
#  - 语言限制：闭包捕获上下文按「宿主方法」粒度共享——同一方法内任一
#    闭包捕获了不可发送值（如 Channel），整个共享上下文即不可发送，
#    故捕获 Channel 的用例（A3b）必须放在独立宿主方法里。
#  - 按 C VM 实际行为断言（实现偏差说明）：
#      * throw 只能抛 enum extends Error，枚举值不可序列化 →
#        异常 worker 的 exit_blob 为 NULL → onError 收不到消息、
#        onExit 收 null、Isolate.run* 返 null 且不向调用者重抛（E 组）；
#      * kill(0) 立即终止，isolate 注册表不摘除，status 仍可查 Dead(5)；
#      * TransferableData.materialize 对无效句柄返回 null 而非抛异常（F2）。
#  - isolate 状态数值（vm_isolate.h VMIsolateStatus）：
#      Created=0 Ready=1 Running=2 Paused=3 Exiting=4 Dead=5
#  - M:1 单线程调度：主 isolate 的协程阻塞（recv/sleep/waitAll）时
#    调度器才推进 worker，故测试中用 sleep/recv 天然制造让出点。
#  - 被 Coroutine.spawn 按名调用的方法用 iso/coro 前缀保证全工程唯一。
# ============================================================

enum IsoTestError extends Error
{
    BoomError = { code = 301, message = "iso-boom" }
}

# 顶层辅助类：不可序列化（不在 SendPort.send 白名单内），用于 B5/G1/G2
IsoPlainBox
{
    int v = 0

    void _init_( int val )
    {
        this.v = val
    }
}

IsolateTest
{
    # ---- 静态字段（C 组隔离断言对象；G1 的 GC 根）----
    static object g_badEntry = null
    static Int32 g_counter = 0
    static Int32 g_init = 41
    static object g_hold = null
    static bool g_h2Flag = false

    # ---- 断言辅助 ----
    static isoCheck( string name, bool cond )
    {
        if ( cond )
        {
            Console.println( "[PASS] " + name )
        }
        else
        {
            Console.println( "[FAIL] " + name )
        }
    }

    # ---- worker / 协程辅助（按名 spawn 要求全工程唯一）----

    # E 组 trampoline：闭包体内直接 throw 不可靠，经静态 throws 方法中转
    static isoThrowErr() throws
    {
        throw IsoTestError.BoomError
    }

    # H1：worker 内按名 spawn 的目标方法
    static Int32 isoH1Add2( Int32 a, Int32 b )
    {
        ret a + b
    }

    # H2：延迟发送协程
    static coroH2Send( object arg )
    {
        SendPort sp = arg as SendPort
        Coroutine.sleep( 20 )
        sp.send( "late" )
    }

    # H2：阻塞接收协程
    static coroH2Recv( object arg )
    {
        ReceivePort rp = arg as ReceivePort
        string msg = rp.recv() as string
        if ( msg == "late" )
        {
            g_h2Flag = true
        }
    }

    # ================= A. spawn / run 基础（§9 A 组）=================
    static testGroupA()
    {
        Console.println( "---------- A. spawn/run 基础 ----------" )

        # A1 匿名闭包 run2 两参求和
        function fnA1 = function( int a, int b ) { ret a + b }
        Int32 a1 = Isolate.run2( fnA1, 3, 4 ) as int
        isoCheck( "A1 run2 匿名闭包 = 7", a1 == 7 )

        # A1b 入口三形态中的两种（function 变量 / Func<签名>）
        function fvar = function( int a, int b ) { ret a * 10 + b }
        Int32 a1b1 = Isolate.run2( fvar, 1, 2 ) as int
        isoCheck( "A1b function变量入口 = 12", a1b1 == 12 )

        Func<int, int, int> typed = function( int a, int b ) { ret a - b }
        Int32 a1b2 = Isolate.run2( typed, 10, 3 ) as int
        isoCheck( "A1b Func<签名>入口 = 7", a1b2 == 7 )

        # A2 端口双向 echo：worker 建自己的 ReceivePort 回传 sendPort
        ReceivePort rpA2 = ReceivePort()
        function fnA2 = function( object arg )
        {
            SendPort sp = arg as SendPort
            ReceivePort wrp = ReceivePort()
            sp.send( wrp.sendPort )
            object msg = wrp.recv()
            sp.send( msg )
        }
        Isolate.spawn1( fnA2, rpA2.sendPort )
        SendPort wport = rpA2.recv() as SendPort
        wport.send( "ping" )
        string a2 = rpA2.recv() as string
        isoCheck( "A2 端口双向echo", a2 == "ping" )

        # A3 非函数入口 → SpawnFailed 异常
        g_badEntry = 42
        bool a3 = false
        label labA3
        {
            try Isolate.spawn0( g_badEntry )
        }
        catch
        {
            a3 = true
        }
        isoCheck( "A3 非函数入口报错", a3 )
        g_badEntry = null

        # A3b 闭包捕获 Channel → 不可发送
        # 注意：C VM 闭包是「宿主方法级共享上下文」——同一方法内所有闭包
        # 共用一个捕获数组。捕获了不可发送值的闭包必须放在独立宿主方法里，
        # 否则会污染整个共享上下文，使同方法后续闭包全部不可发送
        # （这也是设计文档 §9 A 组每用例一方法的原因）。
        testA3b()

        # A4 void 入口 run 返回 null
        function fnA4 = function() { Int32 noop = 0 }
        object a4 = Isolate.run0( fnA4 )
        isoCheck( "A4 void入口返null", a4 == null )

        # A5 当前 isolate 句柄有效
        Isolate curA5 = Isolate.current()
        isoCheck( "A5 current非空且非Dead", curA5 != null && curA5.status != 5 )

        # A6 捕获环境深拷贝：worker 改副本，源不变
        Int32 a6v = 10
        List<int> a6list = new()
        a6list.add( 1 )
        function fnA6 = function()
        {
            a6v = a6v + 100
            a6list.add( 100 )
            ret a6list.length
        }
        Int32 a6r = Isolate.run0( fnA6 ) as int
        isoCheck( "A6 捕获深拷贝(worker改副本源不变)", a6r == 2 && a6v == 10 && a6list.length == 1 )
    }

    # A3b 独立宿主方法：闭包捕获 Channel → 不可发送（见 testGroupA 内注释）
    static testA3b()
    {
        Channel<object> chA3b = Channel<object>.create( 4 )
        function fnA3b = function() { chA3b.send( 1 ) }
        bool a3b = false
        label labA3b
        {
            try Isolate.run0( fnA3b )
        }
        catch
        {
            a3b = true
        }
        isoCheck( "A3b 闭包捕获Channel不可发送", a3b )
    }

    # ================= B. 消息传递（§9 B 组）=================
    static testGroupB()
    {
        Console.println( "---------- B. 消息传递 ----------" )

        # B1 消息深拷贝：worker add 后长度 3，源仍 2
        List<int> b1src = new()
        b1src.add( 1 )
        b1src.add( 2 )
        function fnB1 = function( object arg )
        {
            List<int> src = arg as List<int>
            src.add( 999 )
            ret src.length
        }
        Int32 b1 = Isolate.run1( fnB1, b1src ) as int
        isoCheck( "B1 消息深拷贝", b1 == 3 && b1src.length == 2 )

        # B2 标量回显：int / string / float / null
        function fnB2 = function( object v ) { ret v }
        Int32 b2i = Isolate.run1( fnB2, 42 ) as int
        string b2s = Isolate.run1( fnB2, "hi" ) as string
        double b2f = Isolate.run1( fnB2, 3.14 ) as double
        object b2n = Isolate.run1( fnB2, null )
        isoCheck( "B2 标量回显", b2i == 42 && b2s == "hi" && b2f > 3.13 && b2f < 3.15 && b2n == null )

        # B3 嵌套 List 求和（捕获局部变量，类型随环境深拷贝保留）
        List<int> b3a = new()
        b3a.add( 1 )
        b3a.add( 2 )
        List<int> b3b = new()
        b3b.add( 3 )
        List<List<int>> b3n = new()
        b3n.add( b3a )
        b3n.add( b3b )
        function fnB3 = function()
        {
            Int32 sum = 0
            for Int32 i = 0, i < b3n.length, i = i + 1
            {
                List<int> inner = b3n._getItem_( i )
                for Int32 j = 0, j < inner.length, j = j + 1
                {
                    sum = sum + inner._getItem_( j )
                }
            }
            ret sum
        }
        Int32 b3 = Isolate.run0( fnB3 ) as int
        isoCheck( "B3 嵌套List求和=6", b3 == 6 && b3n.length == 2 && b3a.length == 2 )

        # B4 SendPort 可发送且自反相等（worker 内 == 判等回传）
        ReceivePort rpB4 = ReceivePort()
        function fnB4 = function( object arg )
        {
            SendPort p = arg as SendPort
            p.send( p == p )
        }
        Isolate.spawn1( fnB4, rpB4.sendPort )
        bool b4 = rpB4.recv() as bool
        isoCheck( "B4 SendPort自反相等", b4 )

        # B5 不可发送类型（自定义类）→ 报错
        ReceivePort rpB5 = ReceivePort()
        SendPort spB5 = rpB5.sendPort
        bool b5 = false
        label labB5
        {
            try spB5.send( IsoPlainBox( 0 ) )
        }
        catch
        {
            b5 = true
        }
        isoCheck( "B5 不可发送类型报错", b5 )

        # B6 关闭后 send → 报错
        ReceivePort rpB6 = ReceivePort()
        SendPort spB6 = rpB6.sendPort
        rpB6.close()
        bool b6 = false
        label labB6
        {
            try spB6.send( 1 )
        }
        catch
        {
            b6 = true
        }
        isoCheck( "B6 关闭后send报错", b6 )

        # B7 关闭后残留消息可取，取尽返回 null
        ReceivePort rpB7 = ReceivePort()
        rpB7.sendPort.send( 42 )
        rpB7.close()
        Int32 b7a = rpB7.recv() as int
        object b7b = rpB7.recv()
        isoCheck( "B7 残留消息取出后返null", b7a == 42 && b7b == null )
    }

    # ================= C. 静态字段 / 全局数据隔离（§9 C 组）=================
    static testGroupC()
    {
        Console.println( "---------- C. 静态字段/全局数据隔离 ----------" )

        # C1 类静态字段隔离：worker VM 有独立静态副本（初始 0）
        g_counter = 7
        function fnC1 = function()
        {
            g_counter = g_counter + 100
            ret g_counter
        }
        Int32 c1 = Isolate.run0( fnC1 ) as int
        isoCheck( "C1 静态字段隔离(worker=100主=7)", c1 == 100 && g_counter == 7 )

        # C2 worker VM 首次触碰时重跑静态初始化表达式 → 读到 41
        g_init = 0
        function fnC2 = function() { ret g_init }
        Int32 c2 = Isolate.run0( fnC2 ) as int
        isoCheck( "C2 初始化器重跑(worker读到41)", c2 == 41 && g_init == 0 )

        # C3 全局数据变量（Project data）隔离：worker 修改不影响主端
        global.var1 = 99
        function fnC3 = function()
        {
            global.var1 = global.var1 + 1
            ret global.var1
        }
        Int32 c3 = Isolate.run0( fnC3 ) as int
        isoCheck( "C3 global数据变量隔离", c3 != 99 && global.var1 == 99 )
        Console.println( "  C3 worker读到=" + c3.toString() + " (shadow初始值,与主端99隔离)" )
    }

    # ================= D. 生命周期控制（§9 D 组）=================
    static testGroupD()
    {
        Console.println( "---------- D. 生命周期控制 ----------" )

        # 长睡眠 worker 入口（D2/D3/D4/D5/I3 复用形态）
        function fnSlp = function() { Coroutine.sleep( 5000 ) }

        # D1 pause → Paused(3) → resume → 收到 ready
        ReceivePort rpD1 = ReceivePort()
        function fnD1 = function( object arg )
        {
            SendPort sp = arg as SendPort
            sp.send( "ready" )
            Coroutine.sleep( 5000 )
        }
        Isolate isoD1 = Isolate.spawn1( fnD1, rpD1.sendPort )
        Coroutine.sleep( 50 )
        Capability capD1 = isoD1.pause()
        isoCheck( "D1 pause后状态为Paused(3)", isoD1.status == 3 )
        isoD1.resume( capD1 )
        string d1 = rpD1.recv() as string
        isoCheck( "D1 resume后收到ready", d1 == "ready" )
        isoD1.kill( 0 )

        # D2 伪造 capability resume 静默无效
        Isolate isoD2 = Isolate.spawn0( fnSlp )
        Coroutine.sleep( 30 )
        Capability capD2 = isoD2.pause()
        Capability fakeD2 = Capability( 0 )
        isoD2.resume( fakeD2 )
        bool d2still = isoD2.status == 3
        isoD2.resume( capD2 )
        isoCheck( "D2 伪cap静默+真cap恢复", d2still && isoD2.status != 3 )
        isoD2.kill( 0 )

        # D3 kill(0) 立即终止 → Dead(5)
        Isolate isoD3 = Isolate.spawn0( fnSlp )
        Coroutine.sleep( 30 )
        isoD3.kill( 0 )
        Coroutine.sleep( 30 )
        isoCheck( "D3 kill(0)立即死", isoD3.status == 5 )

        # D4 ping 存活探测
        ReceivePort rpD4 = ReceivePort()
        Isolate isoD4 = Isolate.spawn0( fnSlp )
        Coroutine.sleep( 30 )
        isoD4.ping( rpD4.sendPort, "pong", 0 )
        string d4 = rpD4.recv() as string
        isoCheck( "D4 ping存活探测", d4 == "pong" )
        isoD4.kill( 0 )

        # D5 onExit 监听：退出时收到通知（载荷为 null，Dart 语义）
        ReceivePort exitRpD5 = ReceivePort()
        Isolate isoD5 = Isolate.spawn0( fnSlp )
        Coroutine.sleep( 30 )
        isoD5.addOnExitListener( exitRpD5.sendPort, null )
        isoD5.kill( 0 )
        Int32 spinsD5 = 0
        while ( exitRpD5.count < 1 && spinsD5 < 200 )
        {
            Coroutine.sleep( 5 )
            spinsD5 = spinsD5 + 1
        }
        bool d5arrived = exitRpD5.count == 1
        object d5msg = exitRpD5.recv()
        isoCheck( "D5 onExit通知到达且载荷为null", d5arrived && d5msg == null )
    }

    # ================= E. 错误传播（§9 E 组）=================
    static testGroupE()
    {
        Console.println( "---------- E. 错误传播 ----------" )

        # E1 异常 worker：isolate 死亡 + onExit 通知
        #    （Error 枚举不可序列化 → onError 收不到消息，实现偏差）
        ReceivePort errRpE1 = ReceivePort()
        ReceivePort exitRpE1 = ReceivePort()
        function boomFn = function() { isoThrowErr() }
        Isolate isoE1 = Isolate.spawn0( boomFn )
        isoE1.addErrorListener( errRpE1.sendPort )
        isoE1.addOnExitListener( exitRpE1.sendPort, null )
        Int32 spinsE1 = 0
        while ( exitRpE1.count < 1 && spinsE1 < 200 )
        {
            Coroutine.sleep( 5 )
            spinsE1 = spinsE1 + 1
        }
        bool e1exit = exitRpE1.count == 1
        object e1msg = exitRpE1.recv()
        isoCheck( "E1 异常worker死亡+onExit(null)", isoE1.status == 5 && e1exit && e1msg == null )
        Console.println( "  E1 onError消息数=" + errRpE1.count.toString() + " (Error枚举不可序列化,偏差:收不到)" )

        # E2 run0 对异常 worker 返回 null（不向调用者重抛）
        object e2 = Isolate.run0( boomFn )
        isoCheck( "E2 run0异常返null不重抛", e2 == null )

        # E3 Isolate.exit 定向退出消息
        ReceivePort rpE3 = ReceivePort()
        SendPort spE3 = rpE3.sendPort
        function fnE3 = function() { Isolate.exit( spE3, 12345 ) }
        Isolate.spawn0( fnE3 )
        Int32 e3 = rpE3.recv() as int
        isoCheck( "E3 exit定向消息", e3 == 12345 )
    }

    # ================= F. TransferableData 零拷贝（§9 F 组）=================
    static testGroupF()
    {
        Console.println( "---------- F. TransferableData 零拷贝 ----------" )

        # F1 1000 字节转移往返
        Array<UInt8> bytesF1 = Array<UInt8>( 1000 )
        for Int32 i = 0, i < bytesF1.length, i = i + 1
        {
            bytesF1._setItem_( i, 7 )
        }
        TransferableData tdF1 = TransferableData.fromBytes( bytesF1 )
        isoCheck( "F1 创建转移块(1000字节有效)", tdF1.isValid && tdF1.size == 1000 )
        function fnF1 = function( object arg )
        {
            TransferableData td = arg as TransferableData
            Array<UInt8> b = td.materialize()
            ret b.length
        }
        Int32 f1 = Isolate.run1( fnF1, tdF1 ) as int
        isoCheck( "F1 转移往返1000字节", f1 == 1000 )

        # F3 转移后本句柄失效（isValid 前 true 后 false）
        isoCheck( "F3 转移后isValid变false", tdF1.isValid == false )

        # F2 失效句柄 materialize 返回 null（不抛异常）
        Array<UInt8> f2 = tdF1.materialize()
        isoCheck( "F2 失效句柄materialize返null", f2 == null )
    }

    # ================= G. GC 与引用（§9 G 组）=================
    static testGroupG()
    {
        Console.println( "---------- G. GC与引用 ----------" )

        # G1 静态字段是 GC 根：强制 GC 后仍可达
        g_hold = IsoPlainBox( 42 )
        function fnNoop = function() { Int32 noop = 0 }
        Isolate.run0( fnNoop )
        Int32 freedG1 = Memory.collect()
        IsoPlainBox backG1 = g_hold as IsoPlainBox
        isoCheck( "G1 静态字段是GC根", backG1 != null && backG1.v == 42 )
        g_hold = null

        # G2 Channel 缓冲是 GC 根（缓冲直存引用，不做深拷贝）
        Channel<object> chG2 = Channel<object>.create( 4 )
        IsoPlainBox boxG2 = IsoPlainBox( 7 )
        chG2.send( boxG2 )
        boxG2 = null
        Int32 freedG2 = Memory.collect()
        IsoPlainBox backG2 = chG2.recv() as IsoPlainBox
        isoCheck( "G2 Channel缓冲是GC根", backG2 != null && backG2.v == 7 )

        # G3 批量 run0 后全部回收：组计数回落到 1（仅主 isolate）
        for Int32 i = 0, i < 20, i = i + 1
        {
            function fnG3 = function() { Int32 n = i }
            Isolate.run0( fnG3 )
        }
        Int32 freedG3 = Memory.collect()
        isoCheck( "G3 20次run0后组计数回落", IsolateGroup.current().isolateCount == 1 )

        # G4 ReceivePort 不可发送
        ReceivePort rpG4 = ReceivePort()
        SendPort spG4 = rpG4.sendPort
        bool g4 = false
        label labG4
        {
            try spG4.send( rpG4 )
        }
        catch
        {
            g4 = true
        }
        isoCheck( "G4 ReceivePort不可发送", g4 )
    }

    # ================= H. 协程互操作（§9 H 组）=================
    static testGroupH()
    {
        Console.println( "---------- H. 协程互操作 ----------" )

        # H1 worker 内起协程：按名 spawn ×2 + waitAll2 + await 求和
        function fnH1 = function()
        {
            Task t1 = Coroutine.spawn2( "isoH1Add2", 1, 1 )
            Task t2 = Coroutine.spawn2( "isoH1Add2", 4, 2 )
            Coroutine.waitAll2( t1, t2 )
            Int32 v1 = Coroutine.awaitHandle( t1 ) as int
            Int32 v2 = Coroutine.awaitHandle( t2 ) as int
            ret v1 + v2
        }
        Int32 h1 = Isolate.run0( fnH1 ) as int
        isoCheck( "H1 worker内协程并发求和=8", h1 == 8 )

        # H2 主协程阻塞期间协程间端口通信不受影响
        g_h2Flag = false
        ReceivePort rpH2 = ReceivePort()
        Task tSend = Coroutine.spawn1( "coroH2Send", rpH2.sendPort )
        Task tRecv = Coroutine.spawn1( "coroH2Recv", rpH2 )
        Coroutine.waitAll2( tSend, tRecv )
        isoCheck( "H2 协程间端口通信", g_h2Flag )

        # H3 Channel 与 Port 并存，各自独立存取
        Channel<object> chH3 = Channel<object>.create( 4 )
        ReceivePort rpH3 = ReceivePort()
        chH3.send( "a" )
        rpH3.sendPort.send( "b" )
        string h3a = chH3.recv() as string
        string h3b = rpH3.recv() as string
        isoCheck( "H3 Channel与Port并存", h3a == "a" && h3b == "b" )
    }

    # ================= I. IsolateGroup（§9 I 组）=================
    static testGroupI()
    {
        Console.println( "---------- I. IsolateGroup ----------" )

        # I1 跨 isolate 类型身份：List<int> 传入 worker 取回元素
        List<int> listI1 = new()
        listI1.add( 42 )
        function fnI1 = function( object arg )
        {
            List<int> v = arg as List<int>
            ret v._getItem_( 0 )
        }
        Int32 i1 = Isolate.run1( fnI1, listI1 ) as int
        isoCheck( "I1 跨isolate类型身份", i1 == 42 )

        # I2 当前组非空且至少含自己
        IsolateGroup grpI2 = IsolateGroup.current()
        isoCheck( "I2 当前组非空且含自己", grpI2 != null && grpI2.isolateCount >= 1 )

        # I3 spawn 进入同组，kill 后组计数回落
        function fnI3 = function() { Coroutine.sleep( 5000 ) }
        Isolate isoI3 = Isolate.spawn0( fnI3 )
        Int32 cntBefore = IsolateGroup.current().isolateCount
        bool i3spawn = cntBefore >= 2
        isoI3.kill( 0 )
        Coroutine.sleep( 30 )
        Int32 cntAfter = IsolateGroup.current().isolateCount
        isoCheck( "I3 spawn同组+kill后计数回落", i3spawn && cntAfter == cntBefore - 1 )
    }

    # ---- 主入口 ----
    static fun()
    {
        Console.println( "========== IsolateTest (ISOLATE_DESIGN §9) ==========" )
        testGroupA()
        testGroupB()
        testGroupC()
        testGroupD()
        testGroupE()
        testGroupF()
        testGroupG()
        testGroupH()
        testGroupI()
        Console.println( "========== IsolateTest done ==========" )
    }
}
