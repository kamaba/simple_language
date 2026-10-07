import Std;
import Core;

# ============================================================================
# TcpBasicTest.sl -- NET Phase 1 A 组：Tcp 基础收发验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md §9（测试计划 A 组）。
#   A1  connect/listen/accept/send/recv 往返 + 地址端口查询
#   A2  对端立即关闭：client read 得到 EOF（n == 0）
#   A4  连接被拒（无监听端口）：NetError.ConnectFailed(code=2)
#   A5  主机名解析失败：NetError.HostNotFound(code=3)
#
# 编写约定（沿用 StreamFileTest.sl / Sqlite3Test.sl 先例）：
#  1. Net 命名空间类型一律 Net. 前缀（Net.Tcp / Net.TcpServer / Net.NetError），
#     Core 模块类型无 namespace，无前缀直接引用。
#  2. throws 辅助 sf 前缀静态方法（统一返回 Int32 1），协程闭包经 try? 中转
#     （闭包不能声明 throws）；SL 异常非受检，组方法不声明 throws，正常路径裸写。
#  3. 预期异常用例：bool 块外声明 + label{ try 语句 } catch e 单 try 形式。
#  4. 显式类型不用 var；无三元运算符；object 比较先 as 转型。
# ============================================================================

TcpBasicTest
{
    # ---------- 统一断言辅助 ----------
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[TcpBasicTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[TcpBasicTest] " + name + " : FAIL" )
        }
    }

    # ---------- sf 前缀 throws 辅助（协程体经 try? 中转） ----------

    # echo 一条 4 字节消息后关闭连接
    static Int32 sfEchoOnce( Net.TcpServer srv ) throws
    {
        Net.TcpStream c = srv.accept()
        ByteBuffer acc = ByteBuffer( 64 )
        while acc.readableBytes < 4
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
        ret 1
    }

    # accept 后立即关闭（client 侧 EOF 源）
    static Int32 sfEofOnce( Net.TcpServer srv ) throws
    {
        Net.TcpStream c = srv.accept()
        c.close()
        ret 1
    }

    # ---------- A 组用例 ----------

    static testGroupA()
    {
        Console.println( "========== A: Tcp 基础收发 ==========" )

        # ---- A1: echo 往返 + 地址端口查询（端口 19301） ----
        Net.TcpServer srv = Net.Tcp.listen( 19301 )
        function echoFn = function()
        {
            object r = try? TcpBasicTest.sfEchoOnce( srv )
        }
        spawn echoFn()
        Net.TcpStream c1 = Net.Tcp.connect( "127.0.0.1", 19301 )
        c1.setNoDelay( true )
        ByteBuffer w1 = ByteBuffer( 64 )
        w1.writeString( "ping" )
        c1.write( w1 )
        ByteBuffer acc1 = ByteBuffer( 64 )
        while acc1.readableBytes < 4
        {
            ByteBuffer b1 = ByteBuffer( 64 )
            Int32 n1 = c1.read( b1 )
            if n1 <= 0
            {
                break
            }
            acc1.writeBytes( b1 )
        }
        string got1 = acc1.readString( acc1.readableBytes )
        check( "A1 echo round-trip", got1 == "ping" )
        check( "A1 remote address", c1.remoteAddress == "127.0.0.1" )
        check( "A1 remote port and server port", c1.remotePort == 19301 && srv.port == 19301 )
        c1.close()
        srv.close()

        # ---- A2: 对端立即关闭 -> client read 得到 EOF（端口 19302） ----
        Net.TcpServer srv2 = Net.Tcp.listen( 19302 )
        function eofFn = function()
        {
            object r = try? TcpBasicTest.sfEofOnce( srv2 )
        }
        spawn eofFn()
        Net.TcpStream c2 = Net.Tcp.connect( "127.0.0.1", 19302 )
        ByteBuffer b2 = ByteBuffer( 16 )
        Int32 n2 = c2.read( b2 )
        check( "A2 peer close yields EOF", n2 == 0 )
        c2.close()
        srv2.close()

        # ---- A4: 连接被拒（无监听端口 19399） ----
        bool a4 = false
        label labA4
        {
            # 5000ms 窗口：本机 loopback refused 确认实测约 2s（云环境
            # SYN 重传后 RST），2000ms 会先超时拿到 Timeout(4) 而非
            # ConnectFailed(2)，测的是映射不是超时边界故放宽。
            try Net.Tcp.connectTimeout( "127.0.0.1", 19399, 5000 )
        }
        catch e
        {
            Error ne = e as Error
            a4 = ne != null && ne.code == 2
        }
        check( "A4 connection refused -> ConnectFailed", a4 )

        # ---- A5: 主机名解析失败 ----
        bool a5 = false
        label labA5
        {
            try Net.Tcp.connectTimeout( "no-such-host-xyz.invalid", 80, 2000 )
        }
        catch e
        {
            Error ne = e as Error
            a5 = ne != null && ne.code == 3
        }
        check( "A5 host not found", a5 )
    }

    static fun()
    {
        Console.println( "===== TcpBasicTest start =====" )
        testGroupA()
        Console.println( "===== TcpBasicTest end =====" )
    }
}
