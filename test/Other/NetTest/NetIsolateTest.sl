import Std;
import Core;

# ============================================================================
# NetIsolateTest.sl -- NET Phase 1 H 组：isolate 组合验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md §9（测试计划 H 组）。
#   H1  每 worker isolate 独立 connect/echo 往返（3 个 worker）
#   H2  一 worker 连接失败不影响其余（fail-fast 返回，server 继续服务）
#
# 要点：TcpStream/Channel/Task 不可 Sendable，worker 内自行 connect，
#       只把结果 int 传回（Isolate.run 返回值）。
# ============================================================================

NetIsolateTest
{
    static Int32 g_hServed = 0

    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[NetIsolateTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[NetIsolateTest] " + name + " : FAIL" )
        }
    }

    # server：依次服务 4 条连接（每条收 8 字节回显后关闭）
    static Int32 sfHServer( Net.TcpServer srv ) throws
    {
        Int32 i = 0
        while i < 4
        {
            Net.TcpStream c = srv.accept()
            ByteBuffer acc = ByteBuffer( 64 )
            while acc.readableBytes < 8
            {
                ByteBuffer buf = ByteBuffer( 64 )
                Int32 n = c.read( buf )
                if n <= 0
                {
                    break
                }
                acc.writeBytes( buf )
            }
            c.write( acc )
            c.close()
            i = i + 1
        }
        NetIsolateTest.g_hServed = 4
        srv.close()
        ret 1
    }

    # worker：connect + 8 字节 echo 往返，成功 ret 1 否则 ret 0
    static Int32 sfWorkerEcho( Int32 port ) throws
    {
        Net.TcpStream c = Net.Tcp.connectTimeout( "127.0.0.1", port, 2000 )
        ByteBuffer w = ByteBuffer( 64 )
        w.writeString( "iso-ping" )
        c.write( w )
        ByteBuffer acc = ByteBuffer( 64 )
        while acc.readableBytes < 8
        {
            ByteBuffer b = ByteBuffer( 64 )
            Int32 n = c.read( b )
            if n <= 0
            {
                break
            }
            acc.writeBytes( b )
        }
        string got = acc.readString( acc.readableBytes )
        c.close()
        if got == "iso-ping"
        {
            ret 1
        }
        ret 0
    }

    # worker：连接必然失败（连接被拒），close 兜底后 ret 1
    static Int32 sfFailConn( Int32 port ) throws
    {
        Net.TcpStream c = Net.Tcp.connectTimeout( "127.0.0.1", port, 2000 )
        c.close()
        ret 1
    }

    # isolate 入口工厂（闭包工厂：每次调用独立闭包实例，避免共享捕获）
    static Func<int, int> isoEchoFn()
    {
        Func<int, int> f = function( int port )
        {
            Int32 ok = 0
            object r = try? NetIsolateTest.sfWorkerEcho( port )
            if r != null
            {
                Int32 v = r as int
                if v == 1
                {
                    ok = 1
                }
            }
            ret ok
        }
        ret f
    }

    static Func<int, int> isoFailFn()
    {
        Func<int, int> f = function( int port )
        {
            Int32 r = 0
            object o = try? NetIsolateTest.sfFailConn( port )
            if o == null
            {
                r = 1
            }
            ret r
        }
        ret f
    }

    static testGroupH()
    {
        Console.println( "========== H: isolate 组合 ==========" )

        Net.TcpServer srv = Net.Tcp.listen( 19325 )
        function hf = function()
        {
            object r = try? NetIsolateTest.sfHServer( srv )
        }
        Task st = spawn hf()
        Func<int, int> entry = NetIsolateTest.isoEchoFn()
        Int32 w1 = Isolate.run( entry, 19325 ) as int
        Int32 w2 = Isolate.run( entry, 19325 ) as int
        Int32 w3 = Isolate.run( entry, 19325 ) as int
        check( "H1 isolate workers echo", w1 == 1 && w2 == 1 && w3 == 1 )
        Func<int, int> failEntry = NetIsolateTest.isoFailFn()
        Int32 f = Isolate.run( failEntry, 1 ) as int
        Int32 w4 = Isolate.run( entry, 19325 ) as int
        object sr = Coroutine.awaitTask( st )
        check( "H2 failed worker returns null", f == 1 )
        check( "H2 server unaffected by failed worker", w4 == 1 && NetIsolateTest.g_hServed == 4 )
    }

    static fun()
    {
        Console.println( "===== NetIsolateTest start =====" )
        testGroupH()
        Console.println( "===== NetIsolateTest end =====" )
    }
}
