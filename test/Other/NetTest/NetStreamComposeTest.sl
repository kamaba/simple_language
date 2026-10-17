import Std;
import Core;

# ============================================================================
# NetStreamComposeTest.sl -- NET Phase 1 G 组：Stream 组合验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md §9（测试计划 G 组）。
#   G1  UdpStream.datagrams()：数据报流（迭代器逐报消费 + 地址校验）
#   G2  StreamController 背压：容量 1，add 满则生产协程挂起，消费后推进
#
# 编写约定同 TcpBasicTest.sl。
# ============================================================================

NetStreamComposeTest
{
    static Int32 g_g2Stage = 0

    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[NetStreamComposeTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[NetStreamComposeTest] " + name + " : FAIL" )
        }
    }

    # G2 生产体：add(1) -> stage=1 -> add(2)（容量 1 满则挂起）-> stage=2 -> close
    static Int32 sfG2Produce( StreamController<object> ctrl ) throws
    {
        ctrl.add( 1 )
        NetStreamComposeTest.g_g2Stage = 1
        ctrl.add( 2 )
        NetStreamComposeTest.g_g2Stage = 2
        ctrl.close()
        ret 1
    }

    static testGroupG()
    {
        Console.println( "========== G: Stream 组合 ==========" )

        # ---- G1: datagrams() 数据报流（19314 -> 19315） ----
        # 泛型实参带命名空间前缀（Stream<Net.UdpDatagram>）无 SL 先例，
        # 迭代器经 var + iterator getter 取得（Stream.sl 同款），元素强类型接收
        Net.UdpStream ua = Net.Udp.open( 19314 )
        Net.UdpStream ub = Net.Udp.open( 19315 )
        var dgs = ub.datagrams()
        var it = dgs.iterator
        ByteBuffer w1 = ByteBuffer( 64 )
        w1.writeString( "dg-1" )
        ua.sendTo( w1, "127.0.0.1", 19315 )
        ByteBuffer w2 = ByteBuffer( 64 )
        w2.writeString( "dg-2" )
        ua.sendTo( w2, "127.0.0.1", 19315 )
        bool g1m1 = it.moveNext()
        Net.UdpDatagram d1 = null
        if g1m1
        {
            d1 = it.current
        }
        bool g1m2 = it.moveNext()
        Net.UdpDatagram d2 = null
        if g1m2
        {
            d2 = it.current
        }
        bool g1ok = false
        bool g1addr = false
        if d1 != null && d2 != null
        {
            ByteBuffer p1 = d1.payload
            string s1 = p1.readString( p1.readableBytes )
            ByteBuffer p2 = d2.payload
            string s2 = p2.readString( p2.readableBytes )
            g1ok = ( s1 == "dg-1" && s2 == "dg-2" ) || ( s1 == "dg-2" && s2 == "dg-1" )
            g1addr = d1.address == "127.0.0.1" && d1.port == 19314
        }
        check( "G1 datagram stream payloads", g1m1 && g1m2 && g1ok )
        check( "G1 datagram source address", g1addr )
        ua.close()
        ub.close()

        # ---- G2: StreamController 背压（本地，无网络） ----
        NetStreamComposeTest.g_g2Stage = 0
        StreamController<object> ctrl = StreamController<object>( 1 )
        function pf = function()
        {
            object r = try? NetStreamComposeTest.sfG2Produce( ctrl )
        }
        spawn pf()
        Coroutine.delay( 100 )
        bool g2s1 = NetStreamComposeTest.g_g2Stage == 1
        StreamIterator<object> it2 = StreamIterator<object>( ctrl.stream )
        bool g2m1 = it2.moveNext()
        Int32 v1 = 0
        if g2m1
        {
            v1 = it2.current as int
        }
        bool g2m2 = it2.moveNext()
        Int32 v2 = 0
        if g2m2
        {
            v2 = it2.current as int
        }
        Coroutine.delay( 50 )
        bool g2s2 = NetStreamComposeTest.g_g2Stage == 2
        bool g2m3 = it2.moveNext()
        check( "G2 add suspends on full buffer", g2s1 )
        check( "G2 values delivered in order", g2m1 && g2m2 && v1 == 1 && v2 == 2 )
        check( "G2 producer resumed then closed", g2s2 && g2m3 == false )
    }

    static fun()
    {
        Console.println( "===== NetStreamComposeTest start =====" )
        testGroupG()
        Console.println( "===== NetStreamComposeTest end =====" )
    }
}
