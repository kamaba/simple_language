import Std;
import Core;

# ============================================================================
# NetSuspendTest.sl -- NET Phase 1 B 组：挂起与唤醒验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md §9（测试计划 B 组）与 ADR-7。
#   B1  accept / recv 挂起态：Task.status == Suspended 且 blockedReason == IO
#   B2  挂起服务器被客户端唤醒后 echo 恢复
#   B3  守卫回归（ADR-7）：root 返回后仍有网络等待者时 VM 不退出，
#       延时 isolate 连接晚到，服务器协程在 root 返回后完成投递
#
# 本文件必须最后执行（ProjectTest.sp 顺序约束）：B3 依赖 root 返回后
# vm_net_wait_count() > 0 维持 VM 存活，此前各组协程须已全部结束。
# B3 断言在 "===== NetSuspendTest end =====" 横幅之后打印属预期。
#
# 编写约定同 TcpBasicTest.sl。
# ============================================================================

NetSuspendTest
{
    static string g_b3Data = ""

    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[NetSuspendTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[NetSuspendTest] " + name + " : FAIL" )
        }
    }

    # ---------- sf 前缀 throws 辅助 ----------

    # B1：accept 一条连接 -> 读一条 4 字节消息 -> 送通道 -> 关闭
    static Int32 sfB1Accept( Net.TcpServer srv, Channel<object> ch ) throws
    {
        Net.TcpStream c = srv.accept()
        ByteBuffer acc = ByteBuffer( 64 )
        while acc.readableBytes < 4
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
        ch.send( got )
        c.close()
        ret 1
    }

    # B2：accept -> 通知已接受 -> echo 至 EOF -> 关闭
    static Int32 sfB2Echo( Net.TcpServer srv, Channel<object> notifyCh ) throws
    {
        Net.TcpStream c = srv.accept()
        notifyCh.send( "accepted" )
        while true
        {
            ByteBuffer b = ByteBuffer( 64 )
            Int32 n = c.read( b )
            if n <= 0
            {
                break
            }
            c.write( b )
        }
        c.close()
        ret 1
    }

    # B3：accept（root 返回后才被延时 worker 唤醒）-> 累积 5 字节 -> 记录 -> 关闭
    static Int32 sfB3Accept( Net.TcpServer srv ) throws
    {
        Net.TcpStream c = srv.accept()
        ByteBuffer acc = ByteBuffer( 64 )
        while acc.readableBytes < 5
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
        NetSuspendTest.g_b3Data = got
        c.close()
        srv.close()
        NetSuspendTest.check( "B3 guard kept VM alive for late delivery", NetSuspendTest.g_b3Data == "b3-ok" )
        ret 1
    }

    # B3 worker 连接体：connect + 写 5 字节 + close
    static Int32 sfB3Connect( Int32 port ) throws
    {
        Net.TcpStream c = Net.Tcp.connect( "127.0.0.1", port )
        ByteBuffer w = ByteBuffer( 64 )
        w.writeString( "b3-ok" )
        c.write( w )
        c.close()
        ret 1
    }

    # B3 worker 入口工厂：延时 400ms 再连接（确保 root 已先行返回）
    static Func<int, int> isoB3WorkerFn()
    {
        Func<int, int> f = function( int port )
        {
            Coroutine.delay( 400 )
            Int32 ok = 0
            object o = try? NetSuspendTest.sfB3Connect( port )
            if o != null
            {
                ok = 1
            }
            ret ok
        }
        ret f
    }

    static testGroupB()
    {
        Console.println( "========== B: 挂起与唤醒 ==========" )

        # ---- B1: accept / recv 挂起态断言（端口 19303） ----
        Net.TcpServer srv1 = Net.Tcp.listen( 19303 )
        Channel<object> ch1 = Channel<object>.create( 4 )
        function b1Fn = function()
        {
            object r = try? NetSuspendTest.sfB1Accept( srv1, ch1 )
        }
        Task st1 = spawn b1Fn()
        Coroutine.delay( 100 )
        bool b1a = st1.status == CoroutineStatus.Suspended && st1.blockedReason == CoroutineBlockReason.IO
        check( "B1 accept suspended", b1a )
        Net.TcpStream c1 = Net.Tcp.connect( "127.0.0.1", 19303 )
        Coroutine.delay( 100 )
        bool b1b = st1.status == CoroutineStatus.Suspended && st1.blockedReason == CoroutineBlockReason.IO
        check( "B1 recv suspended", b1b )
        ByteBuffer w1 = ByteBuffer( 64 )
        w1.writeString( "wake" )
        c1.write( w1 )
        object o1 = ch1.recv()
        string got1 = o1 as string
        check( "B1 wakeup round-trip", got1 == "wake" )
        Coroutine.awaitTask( st1 )
        c1.close()
        srv1.close()

        # ---- B2: 挂起服务器被唤醒（端口 19304） ----
        Net.TcpServer srv2 = Net.Tcp.listen( 19304 )
        Channel<object> notifyCh = Channel<object>.create( 4 )
        function b2Fn = function()
        {
            object r = try? NetSuspendTest.sfB2Echo( srv2, notifyCh )
        }
        Task st2 = spawn b2Fn()
        Net.TcpStream c2 = Net.Tcp.connect( "127.0.0.1", 19304 )
        object nmsg = notifyCh.recv()
        string ns = nmsg as string
        Coroutine.delay( 150 )
        bool b2a = ns == "accepted" && st2.status == CoroutineStatus.Suspended && st2.blockedReason == CoroutineBlockReason.IO
        check( "B2 server suspended on recv", b2a )
        ByteBuffer w2 = ByteBuffer( 64 )
        w2.writeString( "b2-ping" )
        c2.write( w2 )
        ByteBuffer acc2 = ByteBuffer( 64 )
        while acc2.readableBytes < 7
        {
            ByteBuffer rb2 = ByteBuffer( 64 )
            Int32 n2 = c2.read( rb2 )
            if n2 <= 0
            {
                break
            }
            acc2.writeBytes( rb2 )
        }
        string got2 = acc2.readString( acc2.readableBytes )
        check( "B2 echo after wakeup", got2 == "b2-ping" )
        c2.closeWrite()
        Coroutine.awaitTask( st2 )
        c2.close()
        srv2.close()

        # ---- B3: 守卫回归——root 返回后 VM 不退出（端口 19305） ----
        Net.TcpServer srv3 = Net.Tcp.listen( 19305 )
        function b3Fn = function()
        {
            object r = try? NetSuspendTest.sfB3Accept( srv3 )
        }
        Task st3 = spawn b3Fn()
        Coroutine.delay( 100 )
        bool b3a = st3.status == CoroutineStatus.Suspended && st3.blockedReason == CoroutineBlockReason.IO
        check( "B3 server suspended on accept before root returns", b3a )
        Func<int, int> b3Entry = NetSuspendTest.isoB3WorkerFn()
        Isolate.spawn( b3Entry, 19305 )
        Console.println( "[NetSuspendTest] B3 root returning (server suspended on accept)" )
    }

    static fun()
    {
        Console.println( "===== NetSuspendTest start =====" )
        testGroupB()
        Console.println( "===== NetSuspendTest end =====" )
    }
}
