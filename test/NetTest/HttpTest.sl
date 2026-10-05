import Std;
import Core;

# ============================================================================
# HttpTest.sl -- STREAM Phase 3 Stage B L 组：HTTP/1.1 客户端验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md（TLS -> HttpClient /
#   WebSocket / HttpServer 路线）、csimple_lang/md/design/STREAM_DESIGN.md
#   §9.6（HTTP body；本阶段先行实现完整缓冲 Response）。
#   L1  Uri 解析（纯解析无网络）：scheme/host/port/path/query/
#       pathAndQuery/isHttp/isHttps/effectivePort/hostHeader；
#       "http://" 空主机 -> UriFormat(code=1)；
#       "ftp://..." -> UnsupportedScheme(code=2，发生在建连前)
#   L2  GET 明文回环：Content-Length 精确读响应；服务端校验请求行 /
#       Host / Connection: close；客户端校验状态行 / body /
#       头大小写不敏感 getHeader / contentLength / isOk
#   L3  POST + chunked 响应：两块聚合 body；服务端校验 POST 行 /
#       Content-Type / Content-Length: 10 / body 紧跟空行；
#       客户端 contentLength == -1（头缺失）
#   L4  https 完整验证：caPem + hostname(SAN) 复用 TlsTest 证书；
#       send() 自定义头 setHeader 大小写不敏感覆盖（值覆盖、无残留条目）
#
# 端口：L=19361-19363（与既有组端口不冲突）。
# 证书：Resources/cert.pem / key.pem（复用 TlsTest 的自签证书，SAN
#       DNS:localhost + IP:127.0.0.1，客户端 caPem 直接用同一张证书）。
#
# 服务端读取说明：sfReadAll 按"头到空行 + Content-Length body"读满
# 整条请求。注意：形参用 Net.NetStream 且调用侧先局部赋型再传参——
# 子类实参直传基类形参在 SL 已编译代码中无先例（只有赋值上行转型，
# HttpClient send 的 NetStream stream = raw），规避参数收集风险；
# 运行期多态分发由 send 内 stream.write 同款模式佐证。
#
# 编写约定同 TlsTest.sl / NetCloseTest.sl / NetTimeoutTest.sl。
# ============================================================================

HttpTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[HttpTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[HttpTest] " + name + " : FAIL" )
        }
    }

    # 朴素子串查找（返回首匹配下标；未找到返回 -1；sub 空返回 0）
    # ProcessTest.strContains 同款逐字节比对（matched 标志 + break）
    static Int32 sfFind( string text, string sub )
    {
        Int32 tlen = SystemStringLength( text )
        Int32 slen = SystemStringLength( sub )
        if slen == 0
        {
            ret 0
        }
        if tlen < slen
        {
            ret -1
        }
        Int32 last = tlen - slen
        for Int32 i = 0, i <= last, i = i + 1
        {
            bool matched = true
            for Int32 j = 0, j < slen, j = j + 1
            {
                if SystemStringCharCodeAt( text, i + j ) != SystemStringCharCodeAt( sub, j )
                {
                    matched = false
                    break
                }
            }
            if matched
            {
                ret i
            }
        }
        ret -1
    }

    # 从请求/响应文本取 "<name>: <十进制值>" 头（未找到 / 无数字返回 -1）
    # 客户端写头大小写固定（"name: value"），请求行必在最前，
    # 故精确匹配 "\r\n<name>:" 可靠；冒号后的空格必须跳过
    static Int32 sfHeaderInt( string text, string name )
    {
        string pat = "\r\n" + name + ":"
        Int32 pos = sfFind( text, pat )
        if pos < 0
        {
            ret -1
        }
        Int32 i = pos + SystemStringLength( pat )
        Int32 tlen = SystemStringLength( text )
        while i < tlen && SystemStringCharCodeAt( text, i ) == 32
        {
            i = i + 1
        }
        Int32 v = 0
        bool any = false
        while i < tlen
        {
            Int32 ch = SystemStringCharCodeAt( text, i )
            if ch < 48 || ch > 57
            {
                break
            }
            v = v * 10 + ( ch - 48 )
            any = true
            i = i + 1
        }
        if any
        {
            ret v
        }
        ret -1
    }

    # 读满一条 HTTP 请求：头到空行 + Content-Length body（无该头 = 0）
    # 连接对端关闭（n == 0）时返回已读文本
    static string sfReadAll( Net.NetStream s ) throws
    {
        string text = ""
        Int32 headerEnd = -1
        Int32 bodyNeed = 0
        bool done = false
        while done == false
        {
            ByteBuffer b = ByteBuffer( 512 )
            Int32 n = s.read( b )
            if n == 0
            {
                ret text
            }
            text = text + b.readString( n )
            if headerEnd < 0
            {
                Int32 he = sfFind( text, "\r\n\r\n" )
                if he >= 0
                {
                    headerEnd = he + 4
                    Int32 bn = sfHeaderInt( text, "Content-Length" )
                    if bn < 0
                    {
                        bn = 0
                    }
                    bodyNeed = bn
                }
            }
            if headerEnd >= 0
            {
                if SystemStringLength( text ) >= headerEnd + bodyNeed
                {
                    done = true
                }
            }
        }
        ret text
    }

    # L2 server：accept -> 读满 GET 请求 -> 校验请求行/Host/Connection
    # -> Content-Length 响应 -> 等客户端确认 -> close
    static Int32 sfL2Server( Net.TcpServer srv, Channel<object> doneCh ) throws
    {
        Net.TcpStream c = srv.accept()
        Net.NetStream s = c
        string req = sfReadAll( s )
        Int32 p1 = sfFind( req, "GET /a/b?q=1&r=2 HTTP/1.1\r\n" )
        Int32 p2 = sfFind( req, "\r\nHost: 127.0.0.1:19361\r\n" )
        Int32 p3 = sfFind( req, "\r\nConnection: close\r\n" )
        check( "L2 server request line", p1 == 0 )
        check( "L2 server Host header", p2 >= 0 )
        check( "L2 server Connection: close", p3 >= 0 )
        string body = "hello-http-1"
        Int32 blen = SystemStringLength( body )
        string resp = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n"
        resp = resp + "Content-Length: " + blen.toString() + "\r\n"
        resp = resp + "X-Http-Test: l2\r\nConnection: close\r\n\r\n" + body
        ByteBuffer w = ByteBuffer( 512 )
        w.writeString( resp )
        s.write( w )
        object msg = doneCh.recv()
        s.close()
        ret 1
    }

    # L3 server：accept -> 读满 POST 请求 -> 校验请求行/头/body
    # -> chunked 响应（两块聚合为 "hello-http-3"）-> 等确认 -> close
    static Int32 sfL3Server( Net.TcpServer srv, Channel<object> doneCh ) throws
    {
        Net.TcpStream c = srv.accept()
        Net.NetStream s = c
        string req = sfReadAll( s )
        Int32 p1 = sfFind( req, "POST /echo HTTP/1.1\r\n" )
        Int32 p2 = sfFind( req, "\r\nContent-Type: text/plain\r\n" )
        Int32 p3 = sfFind( req, "\r\nContent-Length: 10\r\n" )
        Int32 p4 = sfFind( req, "\r\n\r\nhello=http" )
        check( "L3 server request line", p1 == 0 )
        check( "L3 server Content-Type header", p2 >= 0 )
        check( "L3 server Content-Length: 10", p3 >= 0 )
        check( "L3 server body after blank line", p4 >= 0 )
        string resp = "HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\nConnection: close\r\n\r\n"
        resp = resp + "b\r\nhello-http-\r\n"
        resp = resp + "1\r\n3\r\n"
        resp = resp + "0\r\n\r\n"
        ByteBuffer w = ByteBuffer( 512 )
        w.writeString( resp )
        s.write( w )
        object msg = doneCh.recv()
        s.close()
        ret 1
    }

    # L4 server：accept -> TLS accept（SAN 证书）-> 读满 GET 请求
    # -> 校验请求行/Host/setHeader 覆盖 -> Content-Length 响应
    # -> 等确认 -> close（发 close_notify）
    static Int32 sfL4Server( Net.TcpServer srv, string certPem, string keyPem, Channel<object> doneCh ) throws
    {
        Net.TcpStream c = srv.accept()
        Console.println( "L4 dbg: server accepted" )
        Net.TlsOptions sopt = Net.TlsOptions()
        sopt.certPem = certPem
        sopt.keyPem = keyPem
        Net.TlsStream tls = Net.TlsStream.accept( c, sopt )
        Console.println( "L4 dbg: server tls accepted" )
        Net.NetStream s = tls
        string req = sfReadAll( s )
        Console.println( "L4 dbg: server req read" )
        Int32 p1 = sfFind( req, "GET /secure HTTP/1.1\r\n" )
        Int32 p2 = sfFind( req, "\r\nHost: localhost:19363\r\n" )
        Int32 p3 = sfFind( req, "\r\nX-Test: xyz\r\n" )
        Int32 p4 = sfFind( req, "\r\nX-Test: abc\r\n" )
        check( "L4 server https request line", p1 == 0 )
        check( "L4 server Host header", p2 >= 0 )
        check( "L4 server setHeader override value", p3 >= 0 )
        check( "L4 server setHeader no stale copy", p4 < 0 )
        string body = "secure-http-4"
        Int32 blen = SystemStringLength( body )
        string resp = "HTTP/1.1 200 OK\r\nContent-Length: " + blen.toString() + "\r\n\r\n" + body
        ByteBuffer w = ByteBuffer( 512 )
        w.writeString( resp )
        s.write( w )
        object msg = doneCh.recv()
        tls.close()
        ret 1
    }

    static testGroupL()
    {
        Console.println( "========== L: HTTP 客户端 ==========" )

        # ---- L1: Uri 解析 + scheme 校验（纯解析，无网络） ----
        Net.Uri u1 = Net.Uri( "http://example.com:8080/a/b?q=1&r=2" )
        bool f1a = u1.scheme == "http" && u1.host == "example.com" && u1.port == 8080
        bool f1b = u1.path == "/a/b" && u1.query == "q=1&r=2" && u1.pathAndQuery == "/a/b?q=1&r=2"
        bool f1c = u1.isHttp == true && u1.isHttps == false && u1.effectivePort == 8080
        bool f1d = u1.hostHeader == "example.com:8080"
        check( "L1 uri scheme/host/port", f1a )
        check( "L1 uri path/query/pathAndQuery", f1b )
        check( "L1 uri flags + effectivePort", f1c )
        check( "L1 uri hostHeader (explicit 8080)", f1d )

        Net.Uri u2 = Net.Uri( "https://example.com/x" )
        bool f1e = u2.scheme == "https" && u2.isHttps == true && u2.port < 0
        bool f1f = u2.effectivePort == 443 && u2.hostHeader == "example.com"
        check( "L1 uri https defaults", f1e )
        check( "L1 uri https effectivePort/hostHeader", f1f )

        Net.Uri u3 = Net.Uri( "http://example.com" )
        bool f1g = u3.path == "/" && u3.pathAndQuery == "/" && u3.query == ""
        check( "L1 uri empty path -> /", f1g )

        # "http://" 空 host -> UriFormat(code=1)
        # label 块本身受 catch 保护，块内直接赋值（HttpClient send 先例）
        Net.Uri bad = null
        bool fBad = false
        label labL1a
        {
            bad = Net.Uri( "http://" )
        }
        catch e
        {
            Error te = e as Error
            fBad = te != null && te.code == 1
        }
        check( "L1 uri empty host -> UriFormat(1)", fBad )

        # ftp:// scheme -> UnsupportedScheme(code=2，建连前)
        Net.HttpResponse fr = null
        bool fFtp = false
        label labL1b
        {
            fr = Net.HttpClient.httpGet( "ftp://127.0.0.1/x" )
        }
        catch e
        {
            Error te2 = e as Error
            fFtp = te2 != null && te2.code == 2
        }
        check( "L1 ftp scheme -> UnsupportedScheme(2)", fFtp )

        # ---- L2: GET 回环（明文，Content-Length 响应，端口 19361） ----
        Net.TcpServer srv2 = Net.Tcp.listen( 19361 )
        Channel<object> doneCh2 = Channel<object>.create( 4 )
        function fL2 = function()
        {
            object r = try? HttpTest.sfL2Server( srv2, doneCh2 )
        }
        spawn fL2()
        Net.HttpResponse r2 = Net.HttpClient.httpGet( "http://127.0.0.1:19361/a/b?q=1&r=2" )
        bool f2a = r2.statusCode == 200 && r2.reasonPhrase == "OK" && r2.protocol == "HTTP/1.1"
        bool f2b = r2.body == "hello-http-1"
        string ct2 = r2.headers.getHeader( "Content-Type" )
        string xt2 = r2.headers.getHeader( "x-http-test" )
        Int64 cl2 = r2.contentLength
        bool f2c = ct2 == "text/plain" && xt2 == "l2"
        bool f2d1 = cl2 == 12
        bool f2d2 = r2.isOk
        bool f2d3 = r2.headers.count >= 2
        check( "L2 GET status line", f2a )
        check( "L2 GET body via content-length", f2b )
        check( "L2 GET response headers (case-insensitive get)", f2c )
        check( "L2 GET contentLength == 12", f2d1 )
        check( "L2 GET isOk", f2d2 )
        check( "L2 GET headers.count >= 2", f2d3 )
        doneCh2.send( "ok" )
        srv2.close()

        # ---- L3: POST + chunked 响应（两块聚合，端口 19362） ----
        Net.TcpServer srv3 = Net.Tcp.listen( 19362 )
        Channel<object> doneCh3 = Channel<object>.create( 4 )
        function fL3 = function()
        {
            object r = try? HttpTest.sfL3Server( srv3, doneCh3 )
        }
        spawn fL3()
        Net.HttpResponse r3 = Net.HttpClient.httpPost( "http://127.0.0.1:19362/echo", "hello=http", "text/plain" )
        bool f3a = r3.statusCode == 200 && r3.isOk
        bool f3b = r3.body == "hello-http-3"
        Int64 cl3 = r3.contentLength
        string te3 = r3.headers.getHeader( "Transfer-Encoding" )
        bool f3c = cl3 < 0 && te3 == "chunked"
        check( "L3 POST status + isOk", f3a )
        check( "L3 chunked body aggregation (2 chunks)", f3b )
        check( "L3 contentLength -1 + chunked header", f3c )
        doneCh3.send( "ok" )
        srv3.close()

        # ---- L4: https 完整验证（CA + SNI + SAN，端口 19363） ----
        # localhost 主机名连接安全：sys_net getaddrinfo 遍历候选地址，
        # ::1 立即失败时回落 127.0.0.1；TLS hostname=localhost 与证书
        # SAN DNS:localhost 匹配（TlsTest K2 同款证书）
        string certPem = File.readAllText( "Resources/cert.pem" )
        string keyPem = File.readAllText( "Resources/key.pem" )
        Console.println( "L4 dbg: client certs loaded" )
        Net.TcpServer srv4 = Net.Tcp.listen( 19363 )
        Channel<object> doneCh4 = Channel<object>.create( 4 )
        function fL4 = function()
        {
            object r = try? HttpTest.sfL4Server( srv4, certPem, keyPem, doneCh4 )
        }
        spawn fL4()
        Net.HttpClient c4 = Net.HttpClient()
        c4.caPem = certPem
        Net.HttpRequest req4 = Net.HttpRequest()
        req4.method = 0   # 0 = GET
        req4.url = "https://localhost:19363/secure"
        req4.headers.setHeader( "X-Test", "abc" )
        req4.headers.setHeader( "x-TEST", "xyz" )
        Console.println( "L4 dbg: client connecting" )
        Net.HttpResponse r4 = c4.send( req4 )
        Console.println( "L4 dbg: client response got" )
        bool f4a = r4.statusCode == 200 && r4.isOk
        bool f4b = r4.body == "secure-http-4"
        check( "L4 https GET status", f4a )
        check( "L4 https GET body", f4b )
        doneCh4.send( "ok" )
        srv4.close()
    }

    static fun()
    {
        Console.println( "===== HttpTest start =====" )
        testGroupL()
        Console.println( "===== HttpTest end =====" )
    }
}
