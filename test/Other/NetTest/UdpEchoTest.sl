import Std;
import Core;

# ============================================================================
# UdpEchoTest.sl -- NET Phase 1 D 组：UDP 收发验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md §9（测试计划 D 组）。
#   D1  双端收发：sendTo 定向投递 + 对端 read 读取（双向往返 n == 8）
#   D2  报文边界保持：两次独立 sendTo 各 3 字节 -> 两次 read 各得 3 字节
#   D3  端口占用重复 bind：NetError.BindFailed(code=5)
#   D4  close 幂等：UdpStream 双 close 不抛异常
#
# 编写约定同 TcpBasicTest.sl。
# ============================================================================

UdpEchoTest
{
    # ---------- 统一断言辅助 ----------
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[UdpEchoTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[UdpEchoTest] " + name + " : FAIL" )
        }
    }

    # D4 辅助：三个 UdpStream 各连续 close 两次（幂等则全程无异常）
    static Int32 sfD4CloseAll( Net.UdpStream a, Net.UdpStream b, Net.UdpStream c ) throws
    {
        a.close()
        a.close()
        b.close()
        b.close()
        c.close()
        c.close()
        ret 1
    }

    static testGroupD()
    {
        Console.println( "========== D: UDP 收发 ==========" )

        # ---- D1: 双端收发（19311 <-> 19312） ----
        Net.UdpStream ua = Net.Udp.open( 19311 )
        Net.UdpStream ub = Net.Udp.open( 19312 )
        ByteBuffer w1 = ByteBuffer( 64 )
        w1.writeString( "udp-ping" )
        ua.sendTo( w1, "127.0.0.1", 19312 )
        ByteBuffer r1 = ByteBuffer( 64 )
        Int32 n1 = ub.read( r1 )
        ByteBuffer w2 = ByteBuffer( 64 )
        w2.writeString( "udp-pong" )
        ub.sendTo( w2, "127.0.0.1", 19311 )
        ByteBuffer r2 = ByteBuffer( 64 )
        Int32 n2 = ua.read( r2 )
        string s1 = r1.readString( r1.readableBytes )
        string s2 = r2.readString( r2.readableBytes )
        check( "D1 udp round-trip", n1 == 8 && n2 == 8 && s1 == "udp-ping" && s2 == "udp-pong" )

        # ---- D2: 报文边界保持（两次独立 3 字节报文，不得粘包合并） ----
        ByteBuffer p1 = ByteBuffer( 16 )
        p1.writeString( "abc" )
        ua.sendTo( p1, "127.0.0.1", 19312 )
        ByteBuffer p2 = ByteBuffer( 16 )
        p2.writeString( "xyz" )
        ua.sendTo( p2, "127.0.0.1", 19312 )
        ByteBuffer q1 = ByteBuffer( 16 )
        Int32 u1 = ub.read( q1 )
        ByteBuffer q2 = ByteBuffer( 16 )
        Int32 u2 = ub.read( q2 )
        string t1 = q1.readString( q1.readableBytes )
        string t2 = q2.readString( q2.readableBytes )
        check( "D2 datagram boundaries kept", u1 == 3 && u2 == 3 && t1 == "abc" && t2 == "xyz" )

        # ---- D3: 端口占用重复 bind（19313） ----
        Net.UdpStream uc = Net.Udp.open( 19313 )
        bool d3 = false
        label labD3
        {
            try Net.Udp.open( 19313 )
        }
        catch e
        {
            Error ne = e as Error
            d3 = ne != null && ne.code == 5
        }
        check( "D3 double bind -> BindFailed", d3 )

        # ---- D4: close 幂等（双 close 不抛异常） ----
        bool d4 = true
        label labD4
        {
            try UdpEchoTest.sfD4CloseAll( ua, ub, uc )
        }
        catch e
        {
            d4 = false
        }
        check( "D4 close idempotent", d4 )
    }

    static fun()
    {
        Console.println( "===== UdpEchoTest start =====" )
        testGroupD()
        Console.println( "===== UdpEchoTest end =====" )
    }
}
