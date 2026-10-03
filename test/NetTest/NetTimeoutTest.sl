import Std;
import Core;

# ============================================================================
# NetTimeoutTest.sl -- NET Phase 2 T 组：读写超时验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md Phase 2（读写超时）。
#   T1  recv 读超时：静默对端 + setReadTimeout(200) -> NetError.Timeout(code=4)，
#       且等待时长落在窗口附近（不立即返回、不无限挂起）
#   T2  超时后连接仍可用 + 重设窗口生效：setReadTimeout(1000) 后迟到数据可收
#   T3  accept 超时：无连接接入 + setAcceptTimeout(200) -> Timeout；
#       之后新连接仍可正常接入（超时不杀伤监听器）
#   T4  recvFrom 超时：UDP 无报文 + setReadTimeout(200) -> Timeout；
#       之后报文到达仍可正常收取
#
# send 写超时需对端持续不读 + 填满发送缓冲，本地回环难以稳定构造，不设用例
# （C 侧 vm_net_io 挂起点改造与 recv 同构，路径覆盖由代码审查保证）。
#
# 编写约定同 NetCloseTest.sl。
# ============================================================================

NetTimeoutTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[NetTimeoutTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[NetTimeoutTest] " + name + " : FAIL" )
        }
    }

    # T1/T2 server：accept -> 协程级非阻塞延时 500ms（制造读超时窗口）
    # -> 迟到发数据 -> 等 client 确认收到后收尾
    # 注意：此处必须用 Coroutine.delay（挂起协程、调度器继续跑其它协程）。
    # SystemSleep 是阻塞式真睡眠：单线程 VM 下会冻住整个调度线程，导致
    # 主协程的 200ms 读超时无法按时重执行、迟到数据先落内核缓冲，T1 失效。
    static Int32 sfT12Server( Net.TcpServer srv, Channel<object> doneCh ) throws
    {
        Net.TcpStream c = srv.accept()
        Coroutine.delay( 500 )
        ByteBuffer w = ByteBuffer( 64 )
        w.writeString( "late-data" )
        c.write( w )
        object msg = doneCh.recv()
        c.close()
        ret 1
    }

    static testGroupT()
    {
        Console.println( "========== T: 读写超时 ==========" )

        # ---- T1 + T2: recv 读超时 + 超时后连接仍可用（端口 19341） ----
        Net.TcpServer srv1 = Net.Tcp.listen( 19341 )
        Channel<object> doneCh = Channel<object>.create( 4 )
        function fT12 = function()
        {
            object r = try? NetTimeoutTest.sfT12Server( srv1, doneCh )
        }
        spawn fT12()
        Net.TcpStream c1 = Net.Tcp.connect( "127.0.0.1", 19341 )
        c1.setReadTimeout( 200 )
        Int64 t1 = SystemDateTimeNowMillis()
        bool f1 = false
        Int64 e1 = 0
        ByteBuffer b1 = ByteBuffer( 64 )
        label labT1
        {
            try c1.read( b1 )
        }
        catch e
        {
            Error ne = e as Error
            f1 = ne != null && ne.code == 4
        }
        e1 = SystemDateTimeNowMillis() - t1
        check( "T1 recv read-timeout -> Timeout", f1 )
        check( "T1 timeout window honored", e1 >= 180 && e1 < 3000 )

        # T2: 重设窗口 1000ms（窗口自本次 recv 挂起重锚定），500ms 迟到数据可收
        c1.setReadTimeout( 1000 )
        ByteBuffer b2 = ByteBuffer( 64 )
        Int32 n2 = c1.read( b2 )
        string s2 = b2.readString( n2 )
        check( "T2 conn alive after timeout", n2 > 0 && s2 == "late-data" )
        doneCh.send( "ok" )
        c1.close()
        srv1.close()

        # ---- T3: accept 超时 + 之后接入仍正常（端口 19342） ----
        Net.TcpServer srv3 = Net.Tcp.listen( 19342 )
        srv3.setAcceptTimeout( 200 )
        Int64 t3 = SystemDateTimeNowMillis()
        bool f3 = false
        Int64 e3 = 0
        label labT3
        {
            try srv3.acceptSocket()
        }
        catch e
        {
            Error ne3 = e as Error
            f3 = ne3 != null && ne3.code == 4
        }
        e3 = SystemDateTimeNowMillis() - t3
        check( "T3 accept timeout -> Timeout", f3 )
        check( "T3 timeout window honored", e3 >= 180 && e3 < 3000 )

        # 超时后监听器仍可用：真连接进来，accept 正常接入
        function fT3c = function()
        {
            object r = try? Net.Tcp.connect( "127.0.0.1", 19342 )
        }
        spawn fT3c()
        Net.TcpSocket sk3 = srv3.acceptSocket()
        check( "T3 accept alive after timeout", sk3 != null && sk3.isOpen )
        sk3.close()
        srv3.close()

        # ---- T4: recvFrom 超时 + 之后收报仍正常（端口 19343/19344） ----
        Net.UdpStream u4 = Net.Udp.open( 19343 )
        u4.setReadTimeout( 200 )
        Int64 t4 = SystemDateTimeNowMillis()
        bool f4 = false
        Int64 e4 = 0
        ByteBuffer b4 = ByteBuffer( 64 )
        label labT4
        {
            try u4.read( b4 )
        }
        catch e
        {
            Error ne4 = e as Error
            f4 = ne4 != null && ne4.code == 4
        }
        e4 = SystemDateTimeNowMillis() - t4
        check( "T4 recvFrom timeout -> Timeout", f4 )
        check( "T4 timeout window honored", e4 >= 180 && e4 < 3000 )

        # 超时后 UDP 仍可用：对端发一报，本端正常收取
        Net.UdpStream u4b = Net.Udp.open( 19344 )
        ByteBuffer w4 = ByteBuffer( 64 )
        w4.writeString( "ping" )
        u4b.sendTo( w4, "127.0.0.1", 19343 )
        ByteBuffer b4b = ByteBuffer( 64 )
        Int32 n4 = u4.read( b4b )
        string s4 = b4b.readString( n4 )
        check( "T4 udp alive after timeout", n4 > 0 && s4 == "ping" )
        u4.close()
        u4b.close()
    }

    static fun()
    {
        Console.println( "===== NetTimeoutTest start =====" )
        testGroupT()
        Console.println( "===== NetTimeoutTest end =====" )
    }
}
