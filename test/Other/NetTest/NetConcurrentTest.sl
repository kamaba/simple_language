import Std;
import Core;

# ============================================================================
# NetConcurrentTest.sl -- NET Phase 1 E 组：并发连接验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md §9（测试计划 E 组）。
#   E1  10 条并发连接：每条独立 echo 往返，服务端 accept 循环逐条分发
#
# 要点：C VM 闭包为「宿主方法级共享上下文」——同一方法内所有闭包共用
#       一个捕获数组，循环内闭包捕获循环变量会被后续迭代覆写。故循环内
#       spawn 一律用「循环外定义无捕获闭包 + spawn 实参传值」模式
#       （spawn 点求值实参，对齐 IsolateTest H2 先例）。
# ============================================================================

NetConcurrentTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[NetConcurrentTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[NetConcurrentTest] " + name + " : FAIL" )
        }
    }

    # 单连接 echo：EOF 驱动累积回写（收完再回，连接关闭即 EOF）
    static Int32 sfEchoConn( Net.TcpStream conn ) throws
    {
        ByteBuffer acc = ByteBuffer( 128 )
        while true
        {
            ByteBuffer buf = ByteBuffer( 128 )
            Int32 n = conn.read( buf )
            if n <= 0
            {
                break
            }
            acc.writeBytes( buf )
        }
        conn.write( acc )
        conn.close()
        ret 1
    }

    # accept 循环：10 条连接逐条 spawn echo（无捕获闭包 + 实参传值）
    static Int32 sfAccept10( Net.TcpServer srv ) throws
    {
        function ef = function( object arg )
        {
            object r = try? NetConcurrentTest.sfEchoConn( arg as Net.TcpStream )
        }
        Int32 i = 0
        while i < 10
        {
            Net.TcpStream conn = srv.accept()
            spawn ef( conn )
            i = i + 1
        }
        ret 1
    }

    # 单客户端：写 6 字节 + closeWrite（发 EOF）+ 累积收 6 字节比对
    static Int32 sfCliOnce( Int32 idx, Channel<object> results ) throws
    {
        Net.TcpStream c = Net.Tcp.connectTimeout( "127.0.0.1", 19321, 2000 )
        ByteBuffer w = ByteBuffer( 64 )
        w.writeString( "ping-" + idx.toString() )
        c.write( w )
        c.closeWrite()
        ByteBuffer acc = ByteBuffer( 64 )
        while acc.readableBytes < 6
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
        if got == "ping-" + idx.toString()
        {
            results.send( "ok" )
        }
        else
        {
            results.send( "bad" )
        }
        ret 1
    }

    static testGroupE()
    {
        Console.println( "========== E: 并发连接 ==========" )

        Net.TcpServer srv = Net.Tcp.listen( 19321 )
        function af = function()
        {
            object r = try? NetConcurrentTest.sfAccept10( srv )
        }
        Task at = spawn af()

        Channel<object> results = Channel<object>.create( 16 )
        function cf = function( object a, object b )
        {
            object r = try? NetConcurrentTest.sfCliOnce( a as int, b as Channel<object> )
        }
        Int32 i = 0
        while i < 10
        {
            spawn cf( i, results )
            i = i + 1
        }

        Int32 okCount = 0
        i = 0
        while i < 10
        {
            object msg = results.recv()
            string ms = msg as string
            if ms == "ok"
            {
                okCount = okCount + 1
            }
            i = i + 1
        }
        check( "E1 10 concurrent echo round-trips", okCount == 10 )

        object ar = Coroutine.awaitTask( at )
        srv.close()
    }

    static fun()
    {
        Console.println( "===== NetConcurrentTest start =====" )
        testGroupE()
        Console.println( "===== NetConcurrentTest end =====" )
    }
}
