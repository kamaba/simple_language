import Std;
import Core;

# ============================================================
# IsolateTest —— Std/Isolate 机制验收测试（ISOLATE_DESIGN.md §9 A~I 组
#               + T 组 pthread 真并行 + P 组多线程并发打印
#               + IO 组隔离岛非阻塞文件 IO）
#
# 约定：
#  - 三方法统一入口（编译期脱糖为变长系统调用 SystemIsolateRun/Spawn，
#    Dart Isolate.run 语义；转发实参个数不限）：
#      Isolate.run( entry, arg1, ..., argN )      // 一次性计算，返回 object
#      Isolate.spawn( entry, arg1, ..., argN )    // 返回 Isolate 句柄
#      Isolate.spawnInstance( entry, arg... )     // spawn 的别名形态
#    四种写法（A 组逐一验证）：
#      直通形态:      Isolate.run( fn, a, b )             // fn 为函数值变量
#      调用糖单标识符: Isolate.run( f( a, b ) )           // 拆参直传 f, a, b
#      匿名闭包:      Isolate.run( function(a){...}, x )  // 提升具名闭包
#      调用糖成员链:   Isolate.run( X.staticFn( a, b ) )   // 无参包装闭包
#    实参原样转发（零装箱）；实参求值位置按写法区分——直通/单标识符
#    糖/匿名闭包在调用点求值（实参即消息，深拷贝传 worker）；仅成员链
#    糖的实参表达式在 worker 内求值（被无参包装闭包捕获、随闭包 context
#    深拷贝，worker 内修改不影响源，A6/B1/C 组验证）。
#  - 语言限制：静态方法名裸引用不是函数值（语言无方法组转换），静态
#    成员入口用调用糖 Isolate.run( X.staticFn( a, b ) ) 或包装闭包工厂
#    （H1）；this 非静态成员函数编译报错；匿名闭包/成员链糖形式不支持
#    在闭包体内使用（闭包不能嵌套定义），改用直通形态 Isolate.run( fn,
#    a, b )（T6）；其余调用位置的匿名闭包字面量仍须先赋 function 变量
#    再传（同 Std/Isolate/ReceivePort.listen）。
#  - 语言限制：仅成员链糖（X.staticFn( a, b ) 形态）实参在 worker 内
#    求值——实参表达式被无参包装闭包捕获、随闭包消息序列化深拷贝，故
#    捕获值须可发送；直通/单标识符糖/匿名闭包实参在调用点求值、走消息
#    路径，rp.sendPort 内联写法合法（A2/B4/D1/T 组均按此模式；捕获不可
#    发送值会在发送方抛 NotSendable，Dart Isolate.run 同语义）。
#  - 语言限制：闭包捕获上下文按「宿主方法」粒度共享——同一方法内任一
#    闭包捕获了不可发送值（如 Channel），整个共享上下文即不可发送，
#    故捕获 Channel 的用例（A3b）必须放在独立宿主方法里。
#  - 按 C VM 实际行为断言（实现偏差说明）：
#      * throw 只能抛 enum extends Error，枚举值不可序列化 →
#        异常 worker 的 exit_blob 为 NULL → onError 收不到消息、
#        onExit 收 null、Isolate.run(...) 返 null 且不向调用者重抛（E 组）；
#      * kill(0) 立即终止，isolate 注册表不摘除，status 仍可查 Dead(5)；
#      * TransferableData.materialize 对无效句柄返回 null 而非抛异常（F2）。
#  - isolate 状态数值（vm_isolate.h VMIsolateStatus）：
#      Created=0 Ready=1 Running=2 Paused=3 Exiting=4 Dead=5
#  - P2 (1:1) 线程模型：每个 worker isolate 独占一条 OS 线程
#    （pthread/Win32 线程）真并行执行，主 isolate 跑在 CLI 线程上；
#    isolate 注册表/端口队列由单把递归锁保护，执行（解释器/GC）不持锁。
#    T 组专测该线程模型（真并行加速、无让出点推进、线程级阻塞隔离、
#    并发消息风暴、跨线程 kill 等），多数用例在旧 M:1 下 FAIL（回归哨兵）。
#  - 被 spawn 包装闭包转发的目标方法保留 iso/coro 前缀（可读性约定；
#    原按名 spawn 时代要求全工程唯一，现为普通静态调用无此约束）。
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

    # ---- worker / 协程辅助（spawn 改函数值形式，iso/coro 前缀保留为可读性约定）----

    # E 组 trampoline：闭包体内直接 throw 不可靠，经静态 throws 方法中转
    static isoThrowErr() throws
    {
        throw IsoTestError.BoomError
    }

    # H1：worker 内 spawn 的目标方法
    static Int32 isoH1Add2( Int32 a, Int32 b )
    {
        ret a + b
    }

    # H1：包装闭包工厂（闭包体内不能嵌套定义 function，经工厂取函数值）
    static Func<int,int,int> isoH1Add2Fn()
    {
        Func<int,int,int> fn = function( Int32 a, Int32 b )
        {
            ret isoH1Add2( a, b )
        }
        ret fn
    }

    # H2：延迟发送协程
    static coroH2Send( object arg )
    {
        SendPort sp = arg as SendPort
        Coroutine.delay( 20 )
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

        # A1 匿名闭包（先赋 function 变量）两参求和（脱糖形式）
        function fnA1 = function( int a, int b ) { ret a + b }
        Int32 a1 = Isolate.run( fnA1( 3, 4 ) ) as int
        isoCheck( "A1 run 调用表达式 = 7", a1 == 7 )

        # A1b 入口两形态（function 变量调用糖 / Func<签名>直通形态）
        function fvar = function( int a, int b ) { ret a * 10 + b }
        Int32 a1b1 = Isolate.run( fvar( 1, 2 ) ) as int
        isoCheck( "A1b function变量调用糖 = 12", a1b1 == 12 )

        Func<int, int, int> typed = function( int a, int b ) { ret a - b }
        Int32 a1b2 = Isolate.run( typed, 10, 3 ) as int
        isoCheck( "A1b Func<签名>直通 = 7", a1b2 == 7 )

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
        SendPort spA2 = rpA2.sendPort
        Isolate.spawn( fnA2( spA2 ) )
        SendPort wport = rpA2.recv() as SendPort
        wport.send( "ping" )
        string a2 = rpA2.recv() as string
        isoCheck( "A2 端口双向echo", a2 == "ping" )

        # A3 非函数入口 → SpawnFailed 异常（直通形态传非函数值，
        #    C VM 运行时入口校验抛错）
        g_badEntry = 42
        bool a3 = false
        label labA3
        {
            try Isolate.spawn( g_badEntry )
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
        object a4 = Isolate.run( fnA4() )
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
        Int32 a6r = Isolate.run( fnA6() ) as int
        isoCheck( "A6 捕获深拷贝(worker改副本源不变)", a6r == 2 && a6v == 10 && a6list.length == 1 )

        # A7 直通形态变长 5 参（超旧 0..3 上限，isVariadic 零装箱转发）
        function fnA7 = function( int a, int b, int c, int d, int e )
        {
            ret a + b + c + d + e
        }
        Int32 a7 = Isolate.run( fnA7, 1, 2, 3, 4, 5 ) as int
        isoCheck( "A7 直通变长5参求和=15", a7 == 15 )

        # A8 静态成员入口（调用糖成员链 → 无参包装闭包，实参在 worker 内
        #    求值；静态方法裸引用不是函数值，须经此形式或工厂包装）
        Int32 a8 = Isolate.run( IsolateTest.isoH1Add2( 3, 4 ) ) as int
        isoCheck( "A8 静态成员入口调用糖=7", a8 == 7 )

        # A9 spawnInstance 别名形态（返回 Isolate 句柄 + 调用糖拆参直传）
        #    recv 唤醒时 worker 可能已正常返回置 Dead(5), 与主端查询存在竞争,
        #    故只断言消息到达 + wrapper 非 null + status 在合法枚举区间 [0,5]
        ReceivePort rpA9 = ReceivePort()
        function fnA9 = function( object arg )
        {
            SendPort sp = arg as SendPort
            sp.send( "inst" )
        }
        SendPort spA9 = rpA9.sendPort
        Isolate isoA9 = Isolate.spawnInstance( fnA9( spA9 ) )
        string a9 = rpA9.recv() as string
        isoCheck( "A9 spawnInstance回传+句柄有效", a9 == "inst" && isoA9 != null && isoA9.status >= 0 && isoA9.status <= 5 )

        # A10 匿名闭包直传（脱糖提升具名闭包 + 转发实参；仅方法体支持）
        Int32 a10 = Isolate.run( function( int x ) { ret x * 2 }, 21 ) as int
        isoCheck( "A10 匿名闭包直传=42", a10 == 42 )
    }

    # A3b 独立宿主方法：闭包捕获 Channel → 不可发送（见 testGroupA 内注释）
    static testA3b()
    {
        Channel<object> chA3b = Channel<object>.create( 4 )
        function fnA3b = function() { chA3b.send( 1 ) }
        bool a3b = false
        label labA3b
        {
            try Isolate.run( fnA3b() )
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
        Int32 b1 = Isolate.run( fnB1( b1src ) ) as int
        isoCheck( "B1 消息深拷贝", b1 == 3 && b1src.length == 2 )

        # B2 标量回显：int / string / float / null
        function fnB2 = function( object v ) { ret v }
        Int32 b2i = Isolate.run( fnB2( 42 ) ) as int
        string b2s = Isolate.run( fnB2( "hi" ) ) as string
        double b2f = Isolate.run( fnB2( 3.14 ) ) as double
        object b2n = Isolate.run( fnB2( null ) )
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
        Int32 b3 = Isolate.run( fnB3() ) as int
        isoCheck( "B3 嵌套List求和=6", b3 == 6 && b3n.length == 2 && b3a.length == 2 )

        # B4 SendPort 可发送且自反相等（worker 内 == 判等回传）
        ReceivePort rpB4 = ReceivePort()
        function fnB4 = function( object arg )
        {
            SendPort p = arg as SendPort
            p.send( p == p )
        }
        SendPort spB4 = rpB4.sendPort
        Isolate.spawn( fnB4( spB4 ) )
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
        Int32 c1 = Isolate.run( fnC1() ) as int
        isoCheck( "C1 静态字段隔离(worker=100主=7)", c1 == 100 && g_counter == 7 )

        # C2 worker VM 首次触碰时重跑静态初始化表达式 → 读到 41
        g_init = 0
        function fnC2 = function() { ret g_init }
        Int32 c2 = Isolate.run( fnC2() ) as int
        isoCheck( "C2 初始化器重跑(worker读到41)", c2 == 41 && g_init == 0 )

        # C3 全局数据变量（Project data）隔离：worker 修改不影响主端
        global.var1 = 99
        function fnC3 = function()
        {
            global.var1 = global.var1 + 1
            ret global.var1
        }
        Int32 c3 = Isolate.run( fnC3() ) as int
        isoCheck( "C3 global数据变量隔离", c3 != 99 && global.var1 == 99 )
        Console.println( "  C3 worker读到=" + c3.toString() + " (shadow初始值,与主端99隔离)" )
    }

    # ================= D. 生命周期控制（§9 D 组）=================
    static testGroupD()
    {
        Console.println( "---------- D. 生命周期控制 ----------" )

        # 长睡眠 worker 入口（D2/D3/D4/D5/I3 复用形态）
        function fnSlp = function() { Coroutine.delay( 5000 ) }

        # D1 pause → Paused(3) → resume → 收到 ready
        ReceivePort rpD1 = ReceivePort()
        function fnD1 = function( object arg )
        {
            SendPort sp = arg as SendPort
            sp.send( "ready" )
            Coroutine.delay( 5000 )
        }
        SendPort spD1 = rpD1.sendPort
        Isolate isoD1 = Isolate.spawn( fnD1( spD1 ) )
        Coroutine.delay( 50 )
        Capability capD1 = isoD1.pause()
        isoCheck( "D1 pause后状态为Paused(3)", isoD1.status == 3 )
        isoD1.resume( capD1 )
        string d1 = rpD1.recv() as string
        isoCheck( "D1 resume后收到ready", d1 == "ready" )
        isoD1.kill( 0 )

        # D2 伪造 capability resume 静默无效
        Isolate isoD2 = Isolate.spawn( fnSlp() )
        Coroutine.delay( 30 )
        Capability capD2 = isoD2.pause()
        Capability fakeD2 = Capability( 0 )
        isoD2.resume( fakeD2 )
        bool d2still = isoD2.status == 3
        isoD2.resume( capD2 )
        isoCheck( "D2 伪cap静默+真cap恢复", d2still && isoD2.status != 3 )
        isoD2.kill( 0 )

        # D3 kill(0) 立即终止 → Dead(5)
        Isolate isoD3 = Isolate.spawn( fnSlp() )
        Coroutine.delay( 30 )
        isoD3.kill( 0 )
        Coroutine.delay( 30 )
        isoCheck( "D3 kill(0)立即死", isoD3.status == 5 )

        # D4 ping 存活探测
        ReceivePort rpD4 = ReceivePort()
        Isolate isoD4 = Isolate.spawn( fnSlp() )
        Coroutine.delay( 30 )
        isoD4.ping( rpD4.sendPort, "pong", 0 )
        string d4 = rpD4.recv() as string
        isoCheck( "D4 ping存活探测", d4 == "pong" )
        isoD4.kill( 0 )

        # D5 onExit 监听：退出时收到通知（载荷为 null，Dart 语义）
        ReceivePort exitRpD5 = ReceivePort()
        Isolate isoD5 = Isolate.spawn( fnSlp() )
        Coroutine.delay( 30 )
        isoD5.addOnExitListener( exitRpD5.sendPort, null )
        isoD5.kill( 0 )
        Int32 spinsD5 = 0
        while ( exitRpD5.count < 1 && spinsD5 < 200 )
        {
            Coroutine.delay( 5 )
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
        Isolate isoE1 = Isolate.spawn( boomFn() )
        isoE1.addErrorListener( errRpE1.sendPort )
        isoE1.addOnExitListener( exitRpE1.sendPort, null )
        Int32 spinsE1 = 0
        while ( exitRpE1.count < 1 && spinsE1 < 200 )
        {
            Coroutine.delay( 5 )
            spinsE1 = spinsE1 + 1
        }
        bool e1exit = exitRpE1.count == 1
        object e1msg = exitRpE1.recv()
        isoCheck( "E1 异常worker死亡+onExit(null)", isoE1.status == 5 && e1exit && e1msg == null )
        Console.println( "  E1 onError消息数=" + errRpE1.count.toString() + " (Error枚举不可序列化,偏差:收不到)" )

        # E2 run 对异常 worker 返回 null（不向调用者重抛）
        object e2 = Isolate.run( boomFn() )
        isoCheck( "E2 run异常返null不重抛", e2 == null )

        # E3 Isolate.exit 定向退出消息
        ReceivePort rpE3 = ReceivePort()
        SendPort spE3 = rpE3.sendPort
        function fnE3 = function() { Isolate.exit( spE3, 12345 ) }
        Isolate.spawn( fnE3() )
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
        Int32 f1 = Isolate.run( fnF1, tdF1 ) as int
        isoCheck( "F1 转移往返1000字节(直通,消息传递路径)", f1 == 1000 )

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
        Isolate.run( fnNoop() )
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

        # G3 批量 run 后全部回收：组计数回落到 1（仅主 isolate）
        for Int32 i = 0, i < 20, i = i + 1
        {
            function fnG3 = function() { Int32 n = i }
            Isolate.run( fnG3() )
        }
        Int32 freedG3 = Memory.collect()
        isoCheck( "G3 20次run后组计数回落", IsolateGroup.current().isolateCount == 1 )

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

        # H1 worker 内起协程：经工厂方法取包装闭包（闭包体内不能嵌套定义
        # function）+ spawn 函数值 ×2 + waitAll2 + await 求和
        function fnH1 = function()
        {
            Func<int,int,int> add2 = IsolateTest.isoH1Add2Fn()
            Task t1 = spawn add2( 1, 1 )
            Task t2 = spawn add2( 4, 2 )
            Coroutine.waitAll2( t1, t2 )
            Int32 v1 = Coroutine.awaitTask( t1 ) as int
            Int32 v2 = Coroutine.awaitTask( t2 ) as int
            ret v1 + v2
        }
        Int32 h1 = Isolate.run( fnH1() ) as int
        isoCheck( "H1 worker内协程并发求和=8", h1 == 8 )

        # H2 主协程阻塞期间协程间端口通信不受影响（包装闭包函数值形式）
        g_h2Flag = false
        ReceivePort rpH2 = ReceivePort()
        function h2SendFn = function( object arg )
        {
            coroH2Send( arg )
        }
        function h2RecvFn = function( object arg )
        {
            coroH2Recv( arg )
        }
        Task tSend = spawn h2SendFn( rpH2.sendPort )
        Task tRecv = spawn h2RecvFn( rpH2 )
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
        Int32 i1 = Isolate.run( fnI1( listI1 ) ) as int
        isoCheck( "I1 跨isolate类型身份", i1 == 42 )

        # I2 当前组非空且至少含自己
        IsolateGroup grpI2 = IsolateGroup.current()
        isoCheck( "I2 当前组非空且含自己", grpI2 != null && grpI2.isolateCount >= 1 )

        # I3 spawn 进入同组，kill 后组计数回落
        function fnI3 = function() { Coroutine.delay( 5000 ) }
        Isolate isoI3 = Isolate.spawn( fnI3() )
        Int32 cntBefore = IsolateGroup.current().isolateCount
        bool i3spawn = cntBefore >= 2
        isoI3.kill( 0 )
        Coroutine.delay( 30 )
        Int32 cntAfter = IsolateGroup.current().isolateCount
        isoCheck( "I3 spawn同组+kill后计数回落", i3spawn && cntAfter == cntBefore - 1 )
    }

    # ================= T. pthread 真并行（P2 1:1 线程模型）=================
    # 每个非主 isolate 独占一条 OS 线程真并行执行（vm_isolate.c P2），
    # 主 isolate 在 CLI 线程。以下用例验证线程模型语义。
    static testGroupT()
    {
        Console.println( "---------- T. pthread真并行 ----------" )

        # 忙循环 worker：纯算术无分配、无让出点（T1/T5 复用）
        # 3,000,000 次迭代，acc 周期归零防溢出，结果恒为 0
        function fnT1 = function( object arg )
        {
            SendPort sp = arg as SendPort
            Int32 acc = 0
            Int32 i = 0
            while ( i < 3000000 )
            {
                acc = acc + 1
                if ( acc >= 1000 )
                {
                    acc = 0
                }
                i = i + 1
            }
            sp.send( acc )
        }

        # T1 真并行加速：两 worker 并行耗时 < 1.5 × 单 worker 耗时
        #    （若串行执行约为 2×；需 ≥2 核，耗时打印供人工核对）
        ReceivePort rpT1a = ReceivePort()
        Int64 t1s0 = OS.Timer.clock()
        SendPort spT1a = rpT1a.sendPort
        Isolate.spawn( fnT1( spT1a ) )
        Int32 t1solo = rpT1a.recv() as int
        Int64 t1soloMs = OS.Timer.clock() - t1s0

        ReceivePort rpT1b = ReceivePort()
        ReceivePort rpT1c = ReceivePort()
        Int64 t1p0 = OS.Timer.clock()
        SendPort spT1b = rpT1b.sendPort
        SendPort spT1c = rpT1c.sendPort
        Isolate.spawn( fnT1( spT1b ) )
        Isolate.spawn( fnT1( spT1c ) )
        Int32 t1x = rpT1b.recv() as int
        Int32 t1y = rpT1c.recv() as int
        Int64 t1parMs = OS.Timer.clock() - t1p0
        isoCheck( "T1 真并行(2任务并行<1.5×单任务)", t1parMs * 2 < t1soloMs * 3 && t1x == t1solo && t1y == t1solo )
        Console.println( "  T1 单任务=" + t1soloMs.toString() + "ms  两任务并行=" + t1parMs.toString() + "ms (需≥2核)" )

        # T2 主 isolate 忙等（无 delay/recv/yield 让出点）时 worker 仍完成
        #    M:1 下 worker 永不被调度 → 自旋耗尽 FAIL；P2 下各自线程推进
        ReceivePort rpT2 = ReceivePort()
        function fnT2 = function( object arg )
        {
            SendPort sp = arg as SendPort
            sp.send( "thread" )
        }
        SendPort spT2 = rpT2.sendPort
        Isolate.spawn( fnT2( spT2 ) )
        Int32 spinsT2 = 0
        while ( rpT2.count < 1 && spinsT2 < 200000 )
        {
            spinsT2 = spinsT2 + 1
        }
        string t2msg = ""
        if ( rpT2.count == 1 )
        {
            t2msg = rpT2.recv() as string
        }
        isoCheck( "T2 主isolate无让出点worker仍完成", t2msg == "thread" )

        # T3 多 worker 同时 Running：各占一条 OS 线程并发驻留
        #    （M:1 下同一时刻至多 1 个 Running，其余 Ready → FAIL）
        function fnT3 = function() { Coroutine.delay( 5000 ) }
        Array<Isolate> isosT3 = Array<Isolate>( 4 )
        for Int32 i = 0, i < 4, i = i + 1
        {
            isosT3._setItem_( i, Isolate.spawn( fnT3() ) )
        }
        Int32 spinsT3 = 0
        Int32 runningT3 = 0
        while ( runningT3 < 4 && spinsT3 < 100 )
        {
            Coroutine.delay( 5 )
            spinsT3 = spinsT3 + 1
            runningT3 = 0
            for Int32 i = 0, i < 4, i = i + 1
            {
                if ( isosT3._getItem_( i ).status == 2 )
                {
                    runningT3 = runningT3 + 1
                }
            }
        }
        isoCheck( "T3 4个worker同时Running(2)", runningT3 == 4 )
        for Int32 i = 0, i < 4, i = i + 1
        {
            isosT3._getItem_( i ).kill( 0 )
        }

        # T4 worker 阻塞在 recv（线程级阻塞）不影响其它 isolate
        function fnT4a = function()
        {
            ReceivePort never = ReceivePort()
            object msg = never.recv()
        }
        Isolate isoT4a = Isolate.spawn( fnT4a() )
        Coroutine.delay( 50 )
        function fnT4b = function() { Coroutine.delay( 5000 ) }
        ReceivePort pingRpT4 = ReceivePort()
        Isolate isoT4b = Isolate.spawn( fnT4b() )
        Coroutine.delay( 50 )
        isoT4b.ping( pingRpT4.sendPort, "pong", 0 )
        string t4 = pingRpT4.recv() as string
        isoCheck( "T4 阻塞recv的worker不影响其它isolate", t4 == "pong" && isoT4a.status != 5 )
        isoT4a.kill( 0 )
        isoT4b.kill( 0 )

        # T5 run 挂起主协程期间，另一 worker 在别的线程照常完成回发
        #    （M:1 下忙 worker 无让出点独占调度，快 worker 无从推进）
        ReceivePort rpT5busy = ReceivePort()
        ReceivePort rpT5quick = ReceivePort()
        function fnT5quick = function( object arg )
        {
            SendPort sp = arg as SendPort
            Coroutine.delay( 20 )
            sp.send( "during" )
        }
        SendPort spT5quick = rpT5quick.sendPort
        SendPort spT5busy = rpT5busy.sendPort
        Isolate.spawn( fnT5quick( spT5quick ) )
        Isolate.run( fnT1( spT5busy ) )
        isoCheck( "T5 run挂起期间其它线程worker完成", rpT5quick.count == 1 && rpT5busy.count == 1 )

        # T6 独立宿主方法（fnT6 捕获函数值 inner，避免污染本组共享捕获上下文）
        testT6()

        # T7 并发首触静态初始化：6 worker 同时读 g_init，
        #    各自影子表独立重跑初始化（首触并发需线程安全）
        g_init = 0
        ReceivePort rpT7 = ReceivePort()
        function fnT7 = function( object arg )
        {
            SendPort sp = arg as SendPort
            sp.send( g_init )
        }
        SendPort spT7 = rpT7.sendPort
        for Int32 i = 0, i < 6, i = i + 1
        {
            Isolate.spawn( fnT7( spT7 ) )
        }
        Int32 okT7 = 0
        for Int32 i = 0, i < 6, i = i + 1
        {
            Int32 vT7 = rpT7.recv() as int
            if ( vT7 == 41 )
            {
                okT7 = okT7 + 1
            }
        }
        isoCheck( "T7 并发首触静态初始化均读到41", okT7 == 6 && g_init == 0 )

        # T8 并发消息风暴：8 worker × 50 条 = 400 条无丢失
        #    （多线程并发 send 打同一端口，验证全局锁无竞争丢失）
        ReceivePort rpT8 = ReceivePort()
        function fnT8 = function( object arg )
        {
            SendPort sp = arg as SendPort
            for Int32 k = 0, k < 50, k = k + 1
            {
                sp.send( 1 )
            }
        }
        SendPort spT8 = rpT8.sendPort
        for Int32 i = 0, i < 8, i = i + 1
        {
            Isolate.spawn( fnT8( spT8 ) )
        }
        Int32 spinsT8 = 0
        while ( rpT8.count < 400 && spinsT8 < 400 )
        {
            Coroutine.delay( 5 )
            spinsT8 = spinsT8 + 1
        }
        Int32 sumT8 = 0
        while ( rpT8.count > 0 )
        {
            Int32 vT8 = rpT8.recv() as int
            sumT8 = sumT8 + vT8
        }
        isoCheck( "T8 8worker并发400消息无丢失", sumT8 == 400 )

        # T9 跨线程 kill 运行中的忙 worker：周期 delay 制造退出检查点
        #    （终止为协作式 exit_requested，线程主循环在调度间隙检查）
        function fnT9 = function( object arg )
        {
            Int32 n = arg as int
            Int32 acc = 0
            Int32 i = 0
            while ( i < n )
            {
                acc = acc + 1
                if ( acc >= 200000 )
                {
                    acc = 0
                    Coroutine.delay( 1 )
                }
                i = i + 1
            }
            ret acc
        }
        Isolate isoT9 = Isolate.spawn( fnT9( 20000000 ) )
        Coroutine.delay( 100 )
        isoT9.kill( 0 )
        Int32 spinsT9 = 0
        while ( isoT9.status != 5 && spinsT9 < 200 )
        {
            Coroutine.delay( 5 )
            spinsT9 = spinsT9 + 1
        }
        isoCheck( "T9 kill运行中worker→Dead(5)", isoT9.status == 5 )
    }

    # T6 独立宿主方法：inner 函数值随闭包捕获上下文深拷贝进 worker，
    # worker OS 线程内再嵌套 run 孙 isolate（验证非主线程创建 isolate），
    # 并验证 worker 线程上 Isolate.current() 返回自身（TLS 正确）。
    static testT6()
    {
        ReceivePort rpT6 = ReceivePort()
        function inner = function() { ret 21 }
        function fnT6 = function( object arg )
        {
            SendPort sp = arg as SendPort
            # 闭包体内用直通形态（匿名闭包/成员链糖形式不支持在闭包体内）
            Int32 v = Isolate.run( inner ) as int
            Isolate cur = Isolate.current()
            sp.send( v * 2 )
            sp.send( cur != null && cur.status == 2 )
        }
        SendPort spT6 = rpT6.sendPort
        Isolate.spawn( fnT6( spT6 ) )
        Int32 t6v = rpT6.recv() as int
        bool t6cur = rpT6.recv() as bool
        isoCheck( "T6 worker线程内嵌套孙isolate=42", t6v == 42 )
        isoCheck( "T6 worker线程current()有效", t6cur )
    }

    # ================= P. 多线程并发打印（worker OS 线程 Console 输出）=================
    # P2 下每个 worker 独占一条 OS 线程,Console.println 经单次 _write
    # 输出,行内不交错;跨线程输出行序不确定,故不断言顺序,只断言
    # 全部完成、打印内容无丢失无重复、与主线程打印互不阻塞。
    static testGroupP()
    {
        Console.println( "---------- P. 多线程并发打印 ----------" )

        # P1 4 worker 各打印 3 行后回发 done:并发打印全部完成
        ReceivePort rpP1 = ReceivePort()
        function fnP1 = function( object a, object b )
        {
            SendPort sp = a as SendPort
            Int32 id = b as int
            for Int32 k = 0, k < 3, k = k + 1
            {
                Console.println( "[P1] worker-" + id.toString() + " line-" + k.toString() )
            }
            sp.send( "done" )
        }
        SendPort spP1 = rpP1.sendPort
        for Int32 i = 0, i < 4, i = i + 1
        {
            Isolate.spawn( fnP1( spP1, i ) )
        }
        Int32 spinsP1 = 0
        while ( rpP1.count < 4 && spinsP1 < 400 )
        {
            Coroutine.delay( 5 )
            spinsP1 = spinsP1 + 1
        }
        Int32 doneP1 = 0
        while ( rpP1.count > 0 )
        {
            rpP1.recv()
            doneP1 = doneP1 + 1
        }
        isoCheck( "P1 4worker并发打印均完成", doneP1 == 4 )

        # P2 worker 打印字符串并回传同一字符串:12 条乱序到达,
        #    逐条比对期望集合（顺序无关）,验证打印内容无丢失无重复
        ReceivePort rpP2 = ReceivePort()
        function fnP2 = function( object a, object b )
        {
            SendPort sp = a as SendPort
            Int32 id = b as int
            for Int32 k = 0, k < 3, k = k + 1
            {
                string line = "P2-w" + id.toString() + "-l" + k.toString()
                Console.println( line )
                sp.send( line )
            }
        }
        SendPort spP2 = rpP2.sendPort
        for Int32 i = 0, i < 4, i = i + 1
        {
            Isolate.spawn( fnP2( spP2, i ) )
        }
        Int32 spinsP2 = 0
        while ( rpP2.count < 12 && spinsP2 < 400 )
        {
            Coroutine.delay( 5 )
            spinsP2 = spinsP2 + 1
        }
        List<string> gotP2 = new()
        while ( rpP2.count > 0 )
        {
            gotP2.add( rpP2.recv() as string )
        }
        Int32 hitsP2 = 0
        for Int32 w = 0, w < 4, w = w + 1
        {
            for Int32 k = 0, k < 3, k = k + 1
            {
                string wantP2 = "P2-w" + w.toString() + "-l" + k.toString()
                Int32 foundP2 = 0
                for Int32 j = 0, j < gotP2.length, j = j + 1
                {
                    if ( gotP2._getItem_( j ) == wantP2 )
                    {
                        foundP2 = foundP2 + 1
                    }
                }
                if ( foundP2 == 1 )
                {
                    hitsP2 = hitsP2 + 1
                }
            }
        }
        isoCheck( "P2 并发打印内容无丢失无重复", hitsP2 == 12 )

        # P3 主线程打印期间 4 worker 并发打印:互不阻塞,各自完成
        ReceivePort rpP3 = ReceivePort()
        function fnP3 = function( object a, object b )
        {
            SendPort sp = a as SendPort
            Int32 id = b as int
            for Int32 k = 0, k < 3, k = k + 1
            {
                Console.println( "[P3] worker-" + id.toString() + " line-" + k.toString() )
            }
            sp.send( "done" )
        }
        SendPort spP3 = rpP3.sendPort
        for Int32 i = 0, i < 4, i = i + 1
        {
            Isolate.spawn( fnP3( spP3, i ) )
        }
        Int32 mainPrintsP3 = 0
        for Int32 i = 0, i < 6, i = i + 1
        {
            Console.println( "[P3] main line-" + i.toString() )
            mainPrintsP3 = mainPrintsP3 + 1
        }
        Int32 spinsP3 = 0
        while ( rpP3.count < 4 && spinsP3 < 400 )
        {
            Coroutine.delay( 5 )
            spinsP3 = spinsP3 + 1
        }
        isoCheck( "P3 主线程与worker打印互不阻塞", mainPrintsP3 == 6 && rpP3.count == 4 )
    }

    # ================= IO. 隔离岛非阻塞文件 IO =================
    # 文件 IO 放进 worker isolate（独立 OS 线程）执行:主线程 spawn 后
    # 即刻返回继续自己的代码,经端口收结果——非阻塞 IO 模式。
    # 文件按相对路径创建于进程 CWD,用例结束即删（同 FileTest 约定）。
    # worker 闭包仅捕获 string 路径（可发送）,端口一律走实参消息路径,
    # 不捕获 ReceivePort/Channel（污染宿主方法级共享捕获上下文）。
    static testGroupIO()
    {
        Console.println( "---------- IO. 隔离岛非阻塞文件IO ----------" )

        # IO1 异步读文本:spawn 后主线程继续本地计算,收内容回传
        string pathIO1 = "iso_io_basic.txt"
        File.writeAllText( pathIO1, "iso-io-basic-content" )
        ReceivePort rpIO1 = ReceivePort()
        function fnIO1 = function( object arg )
        {
            SendPort sp = arg as SendPort
            Console.println( "[IO1w] path=[" + pathIO1 + "]" )
            string text = File.readAllText( pathIO1 )
            if ( text == null )
            {
                Console.println( "[IO1w] read=NULL" )
            }
            else
            {
                Console.println( "[IO1w] read=[" + text + "]" )
            }
            sp.send( text )
        }
        SendPort spIO1 = rpIO1.sendPort
        Isolate.spawn( fnIO1( spIO1 ) )
        Int32 io1Sum = 0
        for Int32 i = 0, i < 1000, i = i + 1
        {
            io1Sum = io1Sum + i + 1
        }
        string io1Text = rpIO1.recv() as string
        isoCheck( "IO1 异步读文件内容回传", io1Text == "iso-io-basic-content" && io1Sum == 500500 )
        File.delete( pathIO1 )

        # IO2 读文件不阻塞主线程:worker 读+延迟(模拟慢IO)后回发,
        #    主线程立即投入无让出点忙算,IO 由另一条 OS 线程完成
        #    （M:1 下主线程忙算饿死 worker → FAIL,回归哨兵,同 T2）
        string pathIO2 = "iso_io_slow.txt"
        File.writeAllText( pathIO2, "iso-io-slow-data" )
        ReceivePort rpIO2 = ReceivePort()
        function fnIO2 = function( object arg )
        {
            SendPort sp = arg as SendPort
            string text = File.readAllText( pathIO2 )
            Coroutine.delay( 50 )
            sp.send( text )
        }
        SendPort spIO2 = rpIO2.sendPort
        Isolate.spawn( fnIO2( spIO2 ) )
        Int32 io2Work = 0
        Int32 spinsIO2 = 0
        while ( rpIO2.count < 1 && spinsIO2 < 200000 )
        {
            io2Work = io2Work + 1
            spinsIO2 = spinsIO2 + 1
        }
        string io2Text = ""
        if ( rpIO2.count == 1 )
        {
            io2Text = rpIO2.recv() as string
        }
        isoCheck( "IO2 IO期间主线程忙算不被阻塞", io2Text == "iso-io-slow-data" && io2Work > 0 )
        Console.println( "  IO2 主线程忙算迭代=" + io2Work.toString() + " 次(期间worker完成读文件)" )
        File.delete( pathIO2 )

        # IO3 4 文件并发读:每 worker 独立端口回传,并行耗时 < 串行/2
        #    （delay 模拟 IO 延迟;单核下并行 sleep 依然重叠,较 T1 温和）
        for Int32 i = 0, i < 4, i = i + 1
        {
            File.writeAllText( "iso_io_par_" + i.toString() + ".txt", "par-" + i.toString() + "-data" )
        }
        Int64 io3Ser0 = OS.Timer.clock()
        for Int32 i = 0, i < 4, i = i + 1
        {
            Coroutine.delay( 40 )
            string tIO3 = File.readAllText( "iso_io_par_" + i.toString() + ".txt" )
        }
        Int64 io3SerMs = OS.Timer.clock() - io3Ser0

        Array<ReceivePort> rpsIO3 = Array<ReceivePort>( 4 )
        for Int32 i = 0, i < 4, i = i + 1
        {
            rpsIO3._setItem_( i, ReceivePort() )
        }
        function fnIO3 = function( object a, object b )
        {
            SendPort sp = a as SendPort
            Int32 id = b as int
            string text = File.readAllText( "iso_io_par_" + id.toString() + ".txt" )
            Coroutine.delay( 40 )
            sp.send( text )
        }
        Int64 io3Par0 = OS.Timer.clock()
        for Int32 i = 0, i < 4, i = i + 1
        {
            Isolate.spawn( fnIO3( rpsIO3._getItem_( i ).sendPort, i ) )
        }
        Int32 spinsIO3 = 0
        Int32 arrivedIO3 = 0
        while ( arrivedIO3 < 4 && spinsIO3 < 400 )
        {
            arrivedIO3 = 0
            for Int32 i = 0, i < 4, i = i + 1
            {
                arrivedIO3 = arrivedIO3 + rpsIO3._getItem_( i ).count
            }
            Coroutine.delay( 5 )
            spinsIO3 = spinsIO3 + 1
        }
        Int64 io3ParMs = OS.Timer.clock() - io3Par0
        Int32 okIO3 = 0
        for Int32 i = 0, i < 4, i = i + 1
        {
            if ( rpsIO3._getItem_( i ).count == 1 )
            {
                string tIO3 = rpsIO3._getItem_( i ).recv() as string
                if ( tIO3 == "par-" + i.toString() + "-data" )
                {
                    okIO3 = okIO3 + 1
                }
            }
        }
        isoCheck( "IO3 4文件并发读内容全对", okIO3 == 4 )
        isoCheck( "IO3 并行读快于串行一半", io3ParMs * 2 < io3SerMs )
        Console.println( "  IO3 串行=" + io3SerMs.toString() + "ms  4文件并行=" + io3ParMs.toString() + "ms" )
        for Int32 i = 0, i < 4, i = i + 1
        {
            File.delete( "iso_io_par_" + i.toString() + ".txt" )
        }

        # IO4 worker 内读字节文件:字节数组不可直接发送,worker 内
        #    取长度回传,与主线程 File.getSize 交叉验证（内容 23 字节）
        string pathIO4 = "iso_io_bytes.txt"
        File.writeAllText( pathIO4, "ISO-IO-BYTES-0123456789" )
        ReceivePort rpIO4 = ReceivePort()
        function fnIO4 = function( object arg )
        {
            SendPort sp = arg as SendPort
            Console.println( "[IO4w] path=[" + pathIO4 + "]" )
            UInt8Array bytes = File.readAllBytes( pathIO4 )
            sp.send( bytes.length )
        }
        SendPort spIO4 = rpIO4.sendPort
        Isolate.spawn( fnIO4( spIO4 ) )
        Int32 io4Len = rpIO4.recv() as int
        Int64 io4Size = File.getSize( pathIO4 )
        isoCheck( "IO4 worker内读字节文件(23字节)", io4Len == 23 && io4Size == 23 )
        File.delete( pathIO4 )

        # IO5 worker 写文件回执,主线程读回验证（写路径在 worker 线程）
        string pathIO5 = "iso_io_write.txt"
        ReceivePort rpIO5 = ReceivePort()
        function fnIO5 = function( object arg )
        {
            SendPort sp = arg as SendPort
            bool ok = File.writeAllText( pathIO5, "iso-io-worker-written" )
            sp.send( ok )
        }
        SendPort spIO5 = rpIO5.sendPort
        Isolate.spawn( fnIO5( spIO5 ) )
        bool io5Ok = rpIO5.recv() as bool
        string io5Text = File.readAllText( pathIO5 )
        isoCheck( "IO5 worker写文件主线程读回", io5Ok && io5Text == "iso-io-worker-written" )
        File.delete( pathIO5 )
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
        testGroupT()
        testGroupP()
        testGroupIO()
        Console.println( "========== IsolateTest done ==========" )
    }
}
