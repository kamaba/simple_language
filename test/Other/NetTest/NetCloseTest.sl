import Std;
import Core;

# ============================================================================
# NetCloseTest.sl -- NET Phase 1 F 组：关闭语义验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md §9（测试计划 F 组）。
#   F1  closeWrite 半关闭：数据不丢 + 对端读到 EOF
#   F2  closeRead 后 read：StreamIOError.Closed(code=3)
#   F3  socket close 后 recv：NetError.Closed(code=8)
#   F4  close 幂等：TcpStream/TcpServer/UdpStream 双 close 不抛异常
#
# 编写约定同 TcpBasicTest.sl。
# ============================================================================

NetCloseTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[NetCloseTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[NetCloseTest] " + name + " : FAIL" )
        }
    }

    # F1 server：accept -> 累积读至 EOF -> 比对 -> doneCh 汇合 -> close
    static Int32 sfF1Server( Net.TcpServer srv, Channel<object> doneCh ) throws
    {
        Net.TcpStream c = srv.accept()
        ByteBuffer acc = ByteBuffer( 64 )
        while true
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
        if got == "half-close"
        {
            doneCh.send( "ok" )
        }
        else
        {
            doneCh.send( "bad" )
        }
        c.close()
        ret 1
    }

    # F4 辅助：三类对象各连续 close 两次（幂等则全程无异常）
    static Int32 sfF4CloseAll( Net.TcpStream c, Net.TcpServer srv, Net.UdpStream u ) throws
    {
        c.close()
        c.close()
        srv.close()
        srv.close()
        u.close()
        u.close()
        ret 1
    }

    static testGroupF()
    {
        Console.println( "========== F: 关闭语义 ==========" )

        # ---- F1: closeWrite 半关闭（端口 19322） ----
        Net.TcpServer srv1 = Net.Tcp.listen( 19322 )
        Channel<object> doneCh = Channel<object>.create( 4 )
        function f1Fn = function()
        {
            object r = try? NetCloseTest.sfF1Server( srv1, doneCh )
        }
        spawn f1Fn()
        Net.TcpStream c1 = Net.Tcp.connect( "127.0.0.1", 19322 )
        ByteBuffer w1 = ByteBuffer( 64 )
        w1.writeString( "half-close" )
        c1.write( w1 )
        c1.closeWrite()
        object doneMsg = doneCh.recv()
        string ds = doneMsg as string
        check( "F1 half-close keeps data then EOF", ds == "ok" )
        c1.close()
        srv1.close()

        # ---- F2: closeRead 后 read -> StreamIOError.Closed（端口 19323） ----
        Net.TcpServer srv2 = Net.Tcp.listen( 19323 )
        Net.TcpStream c2 = Net.Tcp.connect( "127.0.0.1", 19323 )
        c2.closeRead()
        ByteBuffer b2 = ByteBuffer( 16 )
        bool f2 = false
        label labF2
        {
            try c2.read( b2 )
        }
        catch e
        {
            Error se = e as Error
            f2 = se != null && se.code == 3
        }
        check( "F2 read after closeRead -> Closed", f2 )
        c2.close()
        srv2.close()

        # ---- F3: socket close 后 recv -> NetError.Closed（端口 19324） ----
        Net.TcpServer srv3 = Net.Tcp.listen( 19324 )
        Net.TcpStream c3 = Net.Tcp.connect( "127.0.0.1", 19324 )
        Net.TcpSocket sk = srv3.acceptSocket()
        sk.close()
        ByteBuffer b3 = ByteBuffer( 16 )
        bool f3 = false
        label labF3
        {
            try sk.recv( b3 )
        }
        catch e
        {
            Error ne = e as Error
            f3 = ne != null && ne.code == 8
        }
        check( "F3 recv after socket close -> Closed", f3 )

        # ---- F4: close 幂等（TcpStream/TcpServer/UdpStream 各双 close） ----
        Net.UdpStream u4 = Net.Udp.open( 19324 )
        bool f4 = true
        label labF4
        {
            try NetCloseTest.sfF4CloseAll( c3, srv3, u4 )
        }
        catch e
        {
            f4 = false
        }
        check( "F4 close idempotent", f4 )
    }

    static fun()
    {
        Console.println( "===== NetCloseTest start =====" )
        testGroupF()
        Console.println( "===== NetCloseTest end =====" )
    }
}
