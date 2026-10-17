import Std;
import Core;
import SLangServer;

# ============================================================================
# ServerCoreTest.sl -- SLangServer P0 引擎旁路验收（NetTest 宿主）
# 设计契约：SERVER_DESIGN.md §8 P0 验收
#
#   S1  基础往返：HttpClient 同进程回环（GET text / POST body echo / 404 /
#       异步 handler）；原始客户端 keep-alive 同连接双请求；HEAD（有
#       Content-Length 无 body）；405 + Allow
#   S2  上限：body 超限 413 / 头超限 431
#   S3  handler 超时守护：waitTimeout 超时 -> cancel + 504 + 断连
#   S4  WebSocketStream.accept(raw, preRead, opt) 预读残留探针（风险 #7）：
#       手搓掩码帧客户端把 Upgrade 请求 + 提前首帧一次写出，服务端只消费
#       到请求头空行后走新重载接入，残留帧不丢失（welcome / echo:hello）
#
# 端口：S=19371-19374（与既有组 A..L 19301-19363 不冲突）。
# 编写约定同 HttpTest.sl（sfFind / sfHeaderInt 同款实现）。
# ============================================================================

ServerCoreTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[ServerCoreTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[ServerCoreTest] " + name + " : FAIL" )
        }
    }

    # 朴素子串查找（返回首匹配下标；未找到返回 -1；sub 空返回 0）
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

    # 从响应文本取 "<name>: <十进制值>" 头（未找到 / 无数字返回 -1）
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

    # 读满一条响应：头到空行 + Content-Length body；headOnly 只到头（HEAD 用）
    static string sfReadResponse( Net.NetStream s, bool headOnly ) throws
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
                if headOnly
                {
                    done = true
                }
                elif SystemStringLength( text ) >= headerEnd + bodyNeed
                {
                    done = true
                }
            }
        }
        ret text
    }

    # ── S1 基础往返 ──

    static s1Basic()
    {
        Console.println( "[ServerCoreTest] S1 begin" )
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19371
        SLang.HttpServerCore.configure( cfg )
        SLang.HttpServerCore.reset()

        function hHello = function( SLang.Request req, SLang.Response res )
        {
            res.text( "hello world" )
        }
        SLang.HttpServerCore.onGet( "/hello", hHello )

        function hEcho = function( SLang.Request req, SLang.Response res )
        {
            res.text( "echo:" + req.bodyText() )
        }
        SLang.HttpServerCore.onPost( "/echo", hEcho )

        function hSlow = function( SLang.Request req, SLang.Response res )
        {
            # 异步 handler：派发协程内挂起 150ms 再响应（§4.2 执行模型）
            Coroutine.delay( 150 )
            res.text( "slow done" )
        }
        SLang.HttpServerCore.onGet( "/slow", hSlow )

        SLang.HttpServerCore.start()
        Console.println( "[ServerCoreTest] S1 server started" )

        # S1a GET 文本往返
        Console.println( "[ServerCoreTest] S1a requesting" )
        Net.HttpResponse r1 = Net.HttpClient.httpGet( "http://127.0.0.1:19371/hello" )
        Console.println( "[ServerCoreTest] S1a got response" )
        ServerCoreTest.check( "S1a HttpClient GET 200", r1.statusCode == 200 && r1.body == "hello world" )

        # S1b POST body echo
        Net.HttpResponse r2 = Net.HttpClient.httpPost( "http://127.0.0.1:19371/echo", "ping body", "text/plain" )
        ServerCoreTest.check( "S1b POST body echo", r2.statusCode == 200 && r2.body == "echo:ping body" )

        # S1c 404 + ApiResult 形态 body
        Net.HttpResponse r3 = Net.HttpClient.httpGet( "http://127.0.0.1:19371/nope" )
        ServerCoreTest.check( "S1c 404 + ApiResult body", r3.statusCode == 404 && sfFind( r3.body, "\"code\":404" ) >= 0 )

        # S1d 异步 handler（delay + 响应）
        Net.HttpResponse r4 = Net.HttpClient.httpGet( "http://127.0.0.1:19371/slow" )
        ServerCoreTest.check( "S1d async handler (delay+respond)", r4.statusCode == 200 && r4.body == "slow done" )

        # S1e keep-alive：同一连接顺序双请求（原始客户端）
        Net.TcpStream c = Net.Tcp.connect( "127.0.0.1", 19371 )
        Net.NetStream ns = c
        c.write( ByteBuffer.fromString( "GET /hello HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" ) )
        string k1 = ServerCoreTest.sfReadResponse( ns, false )
        c.write( ByteBuffer.fromString( "GET /hello HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" ) )
        string k2 = ServerCoreTest.sfReadResponse( ns, false )
        c.close()
        ServerCoreTest.check( "S1e keep-alive two requests one conn", sfFind( k1, "200 OK" ) >= 0 && sfFind( k2, "200 OK" ) >= 0 && sfFind( k2, "hello world" ) >= 0 )

        # S1f HEAD：有 Content-Length 无 body
        Net.TcpStream c2 = Net.Tcp.connect( "127.0.0.1", 19371 )
        Net.NetStream ns2 = c2
        c2.write( ByteBuffer.fromString( "HEAD /hello HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" ) )
        string k3 = ServerCoreTest.sfReadResponse( ns2, true )
        c2.close()
        ServerCoreTest.check( "S1f HEAD no body", sfFind( k3, "200 OK" ) >= 0 && sfHeaderInt( k3, "Content-Length" ) == 11 && sfFind( k3, "hello world" ) < 0 )

        # S1g 405 + Allow
        Net.HttpResponse r5 = Net.HttpClient.httpPost( "http://127.0.0.1:19371/hello", "", "text/plain" )
        ServerCoreTest.check( "S1g 405 + Allow", r5.statusCode == 405 && r5.headers.getHeader( "Allow" ) == "GET" )

        SLang.HttpServerCore.stop()
    }

    # ── S2 上限（413 / 431）──

    static s2Limits()
    {
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19372
        cfg.maxBodyBytes = 100
        cfg.maxHeaderBytes = 300
        SLang.HttpServerCore.configure( cfg )
        SLang.HttpServerCore.reset()
        function h = function( SLang.Request req, SLang.Response res )
        {
            res.text( "ok" )
        }
        SLang.HttpServerCore.onPost( "/x", h )
        SLang.HttpServerCore.start()

        # S2a body 超限 413（400 字节 > 100）
        string big = ""
        for Int32 i = 0, i < 40, i = i + 1
        {
            big = big + "0123456789"
        }
        Net.HttpResponse r1 = Net.HttpClient.httpPost( "http://127.0.0.1:19372/x", big, "text/plain" )
        ServerCoreTest.check( "S2a body over limit 413", r1.statusCode == 413 )

        # S2b 头超限 431（约 610 字节 > 300）
        string bigHeader = "0123456789"
        for Int32 i = 0, i < 60, i = i + 1
        {
            bigHeader = bigHeader + "abcdefghij"
        }
        Net.HttpRequest req = Net.HttpRequest()
        req.method = Net.HttpMethod.Post
        req.url = "http://127.0.0.1:19372/x"
        req.headers.add( "X-Big", bigHeader )
        Net.HttpClient hc = Net.HttpClient()
        Net.HttpResponse r2 = hc.send( req )
        ServerCoreTest.check( "S2b header over limit 431", r2.statusCode == 431 )

        SLang.HttpServerCore.stop()
    }

    # ── S3 handler 超时守护（504）──

    static s3Timeout()
    {
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19373
        cfg.handlerTimeoutMs = 150
        SLang.HttpServerCore.configure( cfg )
        SLang.HttpServerCore.reset()
        function h = function( SLang.Request req, SLang.Response res )
        {
            Coroutine.delay( 500 )
            res.text( "late" )
        }
        SLang.HttpServerCore.onGet( "/hang", h )
        SLang.HttpServerCore.start()

        Net.HttpResponse r1 = Net.HttpClient.httpGet( "http://127.0.0.1:19373/hang" )
        ServerCoreTest.check( "S3a handler timeout 504", r1.statusCode == 504 && sfFind( r1.body, "\"code\":504" ) >= 0 )

        SLang.HttpServerCore.stop()
    }

    # ── S4 accept 预读残留探针（风险 #7）──

    # 读一条服务端文本帧（无掩码；载荷 < 126；EOF/Close 返回 null）。
    # pending 为 101 头读取循环的遗留缓冲——帧与 101 同批到达时先消费它。
    # 帧头两字节：[FIN|opcode][len]——len 在第二个字节
    static string readFrame( Net.TcpStream c, ByteBuffer pending ) throws
    {
        bool skip = true
        while skip
        {
            while pending.readableBytes < 2
            {
                Int32 n = c.read( pending )
                if n == 0
                {
                    ret null
                }
            }
            UInt8 b0 = pending.readU8()
            Int32 h0 = b0
            UInt8 b1 = pending.readU8()
            Int32 h1 = b1
            Int32 opcode = h0 & 0x0F
            Int32 len = h1 & 0x7F
            if opcode == 8
            {
                # Close 帧：对端关闭协商
                ret null
            }
            if len > 0
            {
                skip = false
                while pending.readableBytes < len
                {
                    Int32 n = c.read( pending )
                    if n == 0
                    {
                        ret null
                    }
                }
                string s = pending.readString( len )
                ret s
            }
            # 空载荷帧（Ping/Pong）：继续循环读下一条
        }
        ret null
    }

    static s4WsAcceptBuffered()
    {
        Int32 port = 19374

        # 服务端协程：裸 accept -> 短暂等待客户端"Upgrade+提前帧"整体到达 ->
        # 一次性 read 进 buf（= HTTP 层预读残留：含请求字节 + 残留帧）->
        # 新重载接入（accept 内部自行解析请求行+头，残留帧保留进消息层）
        function srv = function()
        {
            Net.TcpServer server = Net.Tcp.listen( port )
            Net.TcpStream c = server.accept()
            Coroutine.delay( 200 )
            ByteBuffer buf = ByteBuffer( 1024 )
            Net.NetStream ns = c
            while buf.readableBytes == 0
            {
                Int32 n = ns.read( buf )
                if n == 0
                {
                    break
                }
            }
            Net.WebSocketStream ws = Net.WebSocketStream.accept( c, buf, null )
            ws.sendText( "welcome" )
            Net.WsMessage m = ws.receive()
            if m != null
            {
                ws.sendText( "echo:" + m.text() )
                m.release()
            }
            # 等对端关闭（客户端 c.close() 后 receive 返回 null）
            while true
            {
                Net.WsMessage e = ws.receive()
                if e == null
                {
                    break
                }
                e.release()
            }
            ws.close()
            server.close()
        }
        Task st = Coroutine.spawnClosure0( srv )
        Coroutine.delay( 300 )

        # 手搓客户端：Upgrade 请求 + 提前掩码帧（"hello"）一次写出
        Net.TcpStream c = Net.Tcp.connect( "127.0.0.1", port )
        Int64 keyId = SystemRandomFill( 16 )
        ByteBuffer kb = ByteBuffer.fromHandle( keyId )
        string key = Base64.encodeToString( kb )
        kb.release()
        string req = "GET /ws HTTP/1.1\r\nHost: 127.0.0.1\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: " + key + "\r\nSec-WebSocket-Version: 13\r\n\r\n"

        Int32 m0 = 1
        Int32 m1 = 2
        Int32 m2 = 3
        Int32 m3 = 4
        string payload = "hello"
        ByteBuffer frame = ByteBuffer( 16 )
        frame.writeU8( SystemConvertUInt8( 0x81 ) )
        frame.writeU8( SystemConvertUInt8( 0x85 ) )
        frame.writeU8( SystemConvertUInt8( m0 ) )
        frame.writeU8( SystemConvertUInt8( m1 ) )
        frame.writeU8( SystemConvertUInt8( m2 ) )
        frame.writeU8( SystemConvertUInt8( m3 ) )
        for Int32 i = 0, i < 5, i = i + 1
        {
            Int32 ch = SystemStringCharCodeAt( payload, i )
            Int32 mod = i & 3
            Int32 mk = m0
            if mod == 1
            {
                mk = m1
            }
            elif mod == 2
            {
                mk = m2
            }
            elif mod == 3
            {
                mk = m3
            }
            frame.writeU8( SystemConvertUInt8( ch ^ mk ) )
        }
        # out 是关键字（ETokenType.Out），缓冲名用 outBuf
        ByteBuffer outBuf = ByteBuffer( 512 )
        ByteBuffer rb = ByteBuffer.fromString( req )
        outBuf.writeBytes( rb )
        rb.release()
        outBuf.writeBytes( frame )
        frame.release()
        c.write( outBuf )

        # 读 101 响应头（到空行；rbuf 保留遗留字节供 readFrame 消费）
        ByteBuffer rbuf = ByteBuffer()
        string resp = ""
        while sfFind( resp, "\r\n\r\n" ) < 0
        {
            ByteBuffer b = ByteBuffer( 256 )
            Int32 n = c.read( b )
            if n == 0
            {
                break
            }
            rbuf.writeBytes( b, n )
            resp = rbuf.toString()
        }
        ServerCoreTest.check( "S4a upgrade 101", sfFind( resp, "101" ) >= 0 )
        # 丢弃 rbuf 中已消费的头区（readerIndex 推进到帧数据起点）
        Int32 hdrEnd = sfFind( resp, "\r\n\r\n" ) + 4
        rbuf.skipBytes( hdrEnd )

        # 读两条服务端消息：welcome + echo:hello（hello 来自预读残留帧）
        string msg1 = ServerCoreTest.readFrame( c, rbuf )
        string msg2 = ServerCoreTest.readFrame( c, rbuf )
        ServerCoreTest.check( "S4b preRead frame not lost (echo:hello)", msg1 == "welcome" && msg2 == "echo:hello" )

        c.close()
        Coroutine.awaitTask( st )
    }

    static fun()
    {
        ServerCoreTest.s1Basic()
        ServerCoreTest.s2Limits()
        ServerCoreTest.s3Timeout()
        ServerCoreTest.s4WsAcceptBuffered()
    }
}
