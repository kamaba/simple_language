import Std;
import Core;

# ============================================================================
# TlsTest.sl -- STREAM Phase 3 Stage A K 组：TLS 加密流验收测试
#
# 参考设计档：csimple_lang/md/design/STREAM_DESIGN.md §9.5 / §18（K 组）。
#   K1  跳过验证回环 echo：caPem 空（VERIFY_NONE）-> 握手 + 双向加解密
#       数据一致；服务端 close（close_notify）-> 客户端 read 干净 EOF(0)
#   K2  完整验证回环 echo：caPem = 自签证书（信任锚）+ hostname = "localhost"
#       （SNI 发送 + SAN 校验）-> 握手 + 数据一致
#   K3  读超时：setReadTimeout(200) + 服务端 delay(500) -> TlsError.Timeout
#       (code=9)；重设 1000ms 后迟到数据可收（超时后连接仍可用）
#
# 证书：Resources/cert.pem / key.pem（openssl 自签 RSA-2048，CN=localhost，
#       SAN DNS:localhost + IP:127.0.0.1，CA:TRUE，有效期 10 年）。
#       服务端 accept 必须提供 certPem/keyPem；客户端验证用 caPem 直接
#       复用同一张自签证书（自签即自身信任锚）。
#
# 编写约定同 NetCloseTest.sl / NetTimeoutTest.sl。
# ============================================================================

TlsTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[TlsTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[TlsTest] " + name + " : FAIL" )
        }
    }

    # K1/K2 server：accept -> TLS accept -> echo 一条消息
    # -> 等客户端确认收妥 -> close（发 close_notify）
    static Int32 sfKEchoServer( Net.TcpServer srv, string certPem, string keyPem, Channel<object> doneCh ) throws
    {
        Net.TcpStream c = srv.accept()
        Net.TlsOptions sopt = Net.TlsOptions()
        sopt.certPem = certPem
        sopt.keyPem = keyPem
        Net.TlsStream tls = Net.TlsStream.accept( c, sopt )
        ByteBuffer r = ByteBuffer( 64 )
        Int32 n = tls.read( r )
        string got = r.readString( n )
        ByteBuffer w = ByteBuffer( 64 )
        w.writeString( got )
        tls.write( w )
        object msg = doneCh.recv()
        tls.close()
        ret 1
    }

    # K3 server：accept -> TLS accept -> delay(500) 制造读超时窗口
    # -> 迟到发数据 -> 等客户端确认 -> close
    # 注意：必须用 Coroutine.delay（协程级非阻塞），同 NetTimeoutTest T1/T2
    static Int32 sfK3Server( Net.TcpServer srv, string certPem, string keyPem, Channel<object> doneCh ) throws
    {
        Net.TcpStream c = srv.accept()
        Net.TlsOptions sopt = Net.TlsOptions()
        sopt.certPem = certPem
        sopt.keyPem = keyPem
        Net.TlsStream tls = Net.TlsStream.accept( c, sopt )
        Coroutine.delay( 500 )
        ByteBuffer w = ByteBuffer( 64 )
        w.writeString( "late-tls" )
        tls.write( w )
        object msg = doneCh.recv()
        tls.close()
        ret 1
    }

    static testGroupK()
    {
        Console.println( "========== K: TLS 加密流 ==========" )

        string certPem = File.readAllText( "Resources/cert.pem" )
        string keyPem = File.readAllText( "Resources/key.pem" )

        # ---- K1: 跳过验证回环 echo + 干净 EOF（端口 19351） ----
        Net.TcpServer srv1 = Net.Tcp.listen( 19351 )
        Channel<object> doneCh1 = Channel<object>.create( 4 )
        function fK1 = function()
        {
            object r = try? TlsTest.sfKEchoServer( srv1, certPem, keyPem, doneCh1 )
        }
        spawn fK1()
        Net.TcpStream raw1 = Net.Tcp.connect( "127.0.0.1", 19351 )
        Net.TlsOptions copt1 = Net.TlsOptions()
        Net.TlsStream tls1 = Net.TlsStream.wrap( raw1, copt1 )
        ByteBuffer w1 = ByteBuffer( 64 )
        w1.writeString( "ping-tls" )
        tls1.write( w1 )
        ByteBuffer r1 = ByteBuffer( 64 )
        Int32 n1 = tls1.read( r1 )
        string s1 = r1.readString( n1 )
        check( "K1 insecure wrap + echo", n1 > 0 && s1 == "ping-tls" )
        doneCh1.send( "ok" )
        ByteBuffer r1b = ByteBuffer( 64 )
        Int32 n1b = tls1.read( r1b )
        check( "K1 peer close_notify -> clean EOF", n1b == 0 )
        tls1.close()
        srv1.close()

        # ---- K2: 完整验证回环 echo（端口 19352） ----
        Net.TcpServer srv2 = Net.Tcp.listen( 19352 )
        Channel<object> doneCh2 = Channel<object>.create( 4 )
        function fK2 = function()
        {
            object r = try? TlsTest.sfKEchoServer( srv2, certPem, keyPem, doneCh2 )
        }
        spawn fK2()
        Net.TcpStream raw2 = Net.Tcp.connect( "127.0.0.1", 19352 )
        Net.TlsOptions copt2 = Net.TlsOptions()
        copt2.caPem = certPem
        copt2.hostname = "localhost"
        Net.TlsStream tls2 = Net.TlsStream.wrap( raw2, copt2 )
        ByteBuffer w2 = ByteBuffer( 64 )
        w2.writeString( "secure-tls" )
        tls2.write( w2 )
        ByteBuffer r2 = ByteBuffer( 64 )
        Int32 n2 = tls2.read( r2 )
        string s2 = r2.readString( n2 )
        check( "K2 verified wrap (CA + SNI + SAN) + echo", n2 > 0 && s2 == "secure-tls" )
        doneCh2.send( "ok" )
        ByteBuffer r2b = ByteBuffer( 64 )
        Int32 n2b = tls2.read( r2b )
        check( "K2 peer close_notify -> clean EOF", n2b == 0 )
        tls2.close()
        srv2.close()

        # ---- K3: 读超时 + 超时后连接仍可用（端口 19353） ----
        Net.TcpServer srv3 = Net.Tcp.listen( 19353 )
        Channel<object> doneCh3 = Channel<object>.create( 4 )
        function fK3 = function()
        {
            object r = try? TlsTest.sfK3Server( srv3, certPem, keyPem, doneCh3 )
        }
        spawn fK3()
        Net.TcpStream raw3 = Net.Tcp.connect( "127.0.0.1", 19353 )
        Net.TlsOptions copt3 = Net.TlsOptions()
        Net.TlsStream tls3 = Net.TlsStream.wrap( raw3, copt3 )
        tls3.setReadTimeout( 200 )
        Int64 t3 = SystemDateTimeNowMillis()
        bool f3 = false
        Int64 e3 = 0
        ByteBuffer b3 = ByteBuffer( 64 )
        label labK3
        {
            try tls3.read( b3 )
        }
        catch e
        {
            Error te = e as Error
            f3 = te != null && te.code == 9
        }
        e3 = SystemDateTimeNowMillis() - t3
        check( "K3 tls read-timeout -> Timeout", f3 )
        check( "K3 timeout window honored", e3 >= 180 && e3 < 3000 )
        tls3.setReadTimeout( 1000 )
        ByteBuffer b3b = ByteBuffer( 64 )
        Int32 n3b = tls3.read( b3b )
        string s3b = b3b.readString( n3b )
        check( "K3 conn alive after timeout", n3b > 0 && s3b == "late-tls" )
        doneCh3.send( "ok" )
        tls3.close()
        srv3.close()
    }

    static fun()
    {
        Console.println( "===== TlsTest start =====" )
        testGroupK()
        Console.println( "===== TlsTest end =====" )
    }
}
