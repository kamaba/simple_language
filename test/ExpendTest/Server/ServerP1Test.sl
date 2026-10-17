import Std;
import Core;
import SLangServer;

# ============================================================================
# ServerP1Test.sl -- SLangServer P1 验收（ExpendTest 宿主）
# 设计契约：SERVER_DESIGN.md §8 P1 验收（§4.1-§4.4 / §4.6 示例全可运行）
#
#   T1  SLang.Server 门面：动词家族注册（onGet/onPost/onPut/onDelete/onPatch/
#       onOptions/onAny）+ 原始客户端 PUT/DELETE/PATCH/OPTIONS + onAny 双动词
#       + 405 + Allow + keep-alive 同连接双请求
#   T2  路由匹配：{param} 路径参数（pathParam/pathInt/pathInt64）+ 精确段优先
#       （/user/profile 压过 /user/{id}）+ RouteGroup 前缀拼接
#   T3  查询参数：percent-decode（%20）+ '+'→空格 + queryInt/queryBool/
#       hasQuery/queryKeys
#   T4  body 三形态：json（body<DTO> toData + bodyJson 回显）+ form
#       （urlencoded '+' 解码 / formInt / formBool / formKeys）+ bin
#       （octet-stream 字节校验）+ bodyFormat 判定
#   T5  Response 家族：链式 status/header/contentType + jsonOk/jsonFail(2/3参)
#       + html + redirect(302+Location) + 终端方法二次调用 500
#   T6  WS 形态 A（HTTP 同端口升级）：/ws/{room} 路径参数 + query + 会话注册表
#       sessions/broadcast + 未注册路径升级 400 + 关闭注销
#   T7  WS 形态 B（WebSocketServer 独立端口）：onMessage 回显 + sessions()
#       + broadcast + stop 清空会话
#   T8  DuplicateRoute fail-fast（checkDuplicate 探测 + start 抛错不监听）
#
# 端口：19401-19408（避开 NetTest S 组 19371-19374）。
# 编写约定同 ServerCoreTest.sl（原始 HTTP / 手搓 WS 客户端助手同款实现）。
# ============================================================================

# JSON DTO（body<P1User>() / jsonOk 序列化用；字段名与 json 键一一对应）。
# @Serializable 必备：SystemJsonFromData/toData 对未标注 class 走 toString
# 嫁接（SerializeTest 用例类型 5），不参与成员展开。
@Serializable()
class P1User
{
    public string name = ""
    public Int32 age = 0

    _init_()
    {
        this.name = ""
        this.age = 0
    }
}

ServerP1Test
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[ServerP1Test] " + name + " : OK" )
        }
        else
        {
            Console.println( "[ServerP1Test] " + name + " : FAIL" )
        }
    }

    # ── 通用助手（ServerCoreTest 同款）──

    # 朴素子串查找（返回首匹配下标；未找到 -1；sub 空返回 0）
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

    # 读满一条响应：头到空行 + Content-Length body
    static string sfReadResp( Net.NetStream s, bool headOnly ) throws
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
                Int32 he = ServerP1Test.sfFind( text, "\r\n\r\n" )
                if he >= 0
                {
                    headerEnd = he + 4
                    # 本组用例 body 均为文本，长度即字节数，直接按串长判满
                    bodyNeed = 0
                }
            }
            if headerEnd >= 0
            {
                if headOnly
                {
                    done = true
                }
                elif ServerP1Test.respBodyDone( text, headerEnd )
                {
                    done = true
                }
            }
        }
        ret text
    }

    # Content-Length 判满（从头部文本提取；无 CL 视为 0）
    static bool respBodyDone( string text, Int32 headerEnd )
    {
        string pat = "\r\nContent-Length:"
        Int32 pos = ServerP1Test.sfFind( text, pat )
        if pos < 0 || pos > headerEnd
        {
            ret SystemStringLength( text ) >= headerEnd
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
        if any == false
        {
            ret SystemStringLength( text ) >= headerEnd
        }
        ret SystemStringLength( text ) >= headerEnd + v
    }

    # 单连接单请求（原始客户端）
    static string rawReq( Int32 port, string reqText ) throws
    {
        Net.TcpStream c = Net.Tcp.connect( "127.0.0.1", port )
        Net.NetStream ns = c
        c.write( ByteBuffer.fromString( reqText ) )
        string r = ServerP1Test.sfReadResp( ns, false )
        c.close()
        ret r
    }

    # 条件轮询（异步注册/注销时序；每轮 50ms）
    static bool waitCond( Function cond, Int32 loops )
    {
        for Int32 i = 0, i < loops, i = i + 1
        {
            bool b = cond()
            if b
            {
                ret true
            }
            Coroutine.delay( 50 )
        }
        ret false
    }

    # ── T1 门面 + 动词家族 ──

    static void t1Verbs()
    {
        Console.println( "[ServerP1Test] T1 begin" )
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19401
        SLang.Server.configure( cfg )
        SLang.Server.reset()

        function hG = function( SLang.Request req, SLang.Response res )
        {
            res.text( "g-ok" )
        }
        SLang.Server.onGet( "/get1", hG )
        function hP = function( SLang.Request req, SLang.Response res )
        {
            res.text( "p:" + req.bodyText() )
        }
        SLang.Server.onPost( "/post1", hP )
        function hPut = function( SLang.Request req, SLang.Response res )
        {
            res.text( "put-ok" )
        }
        SLang.Server.onPut( "/put1", hPut )
        function hDel = function( SLang.Request req, SLang.Response res )
        {
            res.text( "del-ok" )
        }
        SLang.Server.onDelete( "/del1", hDel )
        function hPatch = function( SLang.Request req, SLang.Response res )
        {
            res.text( "patch-ok" )
        }
        SLang.Server.onPatch( "/patch1", hPatch )
        function hOpt = function( SLang.Request req, SLang.Response res )
        {
            res.text( "opt-ok" )
        }
        SLang.Server.onOptions( "/opt1", hOpt )
        function hAny = function( SLang.Request req, SLang.Response res )
        {
            res.text( "any-ok" )
        }
        SLang.Server.onAny( "/any1", hAny )
        SLang.Server.start( 19401 )

        # T1a/T1b HttpClient 走门面路由
        Net.HttpResponse r1 = Net.HttpClient.httpGet( "http://127.0.0.1:19401/get1" )
        ServerP1Test.check( "T1a GET via facade", r1.statusCode == 200 && r1.body == "g-ok" )
        Net.HttpResponse r2 = Net.HttpClient.httpPost( "http://127.0.0.1:19401/post1", "data1", "text/plain" )
        ServerP1Test.check( "T1b POST via facade", r2.statusCode == 200 && r2.body == "p:data1" )

        # T1c-f 原始客户端动词家族
        string r3 = ServerP1Test.rawReq( 19401, "PUT /put1 HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 0\r\n\r\n" )
        ServerP1Test.check( "T1c PUT", sfFind( r3, "200" ) >= 0 && sfFind( r3, "put-ok" ) >= 0 )
        string r4 = ServerP1Test.rawReq( 19401, "DELETE /del1 HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" )
        ServerP1Test.check( "T1d DELETE", sfFind( r4, "200" ) >= 0 && sfFind( r4, "del-ok" ) >= 0 )
        string r5 = ServerP1Test.rawReq( 19401, "PATCH /patch1 HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" )
        ServerP1Test.check( "T1e PATCH", sfFind( r5, "200" ) >= 0 && sfFind( r5, "patch-ok" ) >= 0 )
        string r6 = ServerP1Test.rawReq( 19401, "OPTIONS /opt1 HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" )
        ServerP1Test.check( "T1f OPTIONS", sfFind( r6, "200" ) >= 0 && sfFind( r6, "opt-ok" ) >= 0 )

        # T1g onAny 双动词命中
        string r7 = ServerP1Test.rawReq( 19401, "POST /any1 HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 0\r\n\r\n" )
        string r8 = ServerP1Test.rawReq( 19401, "GET /any1 HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" )
        ServerP1Test.check( "T1g onAny two verbs", sfFind( r7, "any-ok" ) >= 0 && sfFind( r8, "any-ok" ) >= 0 )

        # T1h 405 + Allow（门面注册路径）
        Net.HttpResponse r9 = Net.HttpClient.httpPost( "http://127.0.0.1:19401/get1", "", "text/plain" )
        ServerP1Test.check( "T1h 405 + Allow", r9.statusCode == 405 && r9.headers.getHeader( "Allow" ) == "GET" )

        # T1i keep-alive 同连接双请求
        Net.TcpStream c = Net.Tcp.connect( "127.0.0.1", 19401 )
        Net.NetStream ns = c
        c.write( ByteBuffer.fromString( "GET /get1 HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" ) )
        string k1 = ServerP1Test.sfReadResp( ns, false )
        c.write( ByteBuffer.fromString( "GET /get1 HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" ) )
        string k2 = ServerP1Test.sfReadResp( ns, false )
        c.close()
        ServerP1Test.check( "T1i keep-alive two requests", sfFind( k1, "g-ok" ) >= 0 && sfFind( k2, "g-ok" ) >= 0 )

        SLang.Server.stop()
    }

    # ── T2 路由匹配 ──

    static void t2Routes()
    {
        Console.println( "[ServerP1Test] T2 begin" )
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19402
        SLang.Server.configure( cfg )
        SLang.Server.reset()

        function hUser = function( SLang.Request req, SLang.Response res )
        {
            res.text( "id=" + req.pathParam( "id" ) + ",n=" + req.pathInt( "id", -1 ).toString() )
        }
        SLang.Server.onGet( "/user/{id}", hUser )
        function hProfile = function( SLang.Request req, SLang.Response res )
        {
            res.text( "profile" )
        }
        SLang.Server.onGet( "/user/profile", hProfile )
        function hBig = function( SLang.Request req, SLang.Response res )
        {
            Int64 bv = req.pathInt64( "v", 0 )
            res.text( "v=" + bv.toString() )
        }
        SLang.Server.onGet( "/big/{v}", hBig )
        function hList = function( SLang.Request req, SLang.Response res )
        {
            res.text( "users-list" )
        }
        function hCreate = function( SLang.Request req, SLang.Response res )
        {
            res.text( "users-create" )
        }
        SLang.RouteGroup g = SLang.Server.group( "/api/v1" )
        g.onGet( "/users", hList )
        g.onPost( "/users", hCreate )
        SLang.Server.start( 19402 )

        Net.HttpResponse r1 = Net.HttpClient.httpGet( "http://127.0.0.1:19402/user/42" )
        ServerP1Test.check( "T2a pathParam + pathInt", r1.statusCode == 200 && r1.body == "id=42,n=42" )
        Net.HttpResponse r2 = Net.HttpClient.httpGet( "http://127.0.0.1:19402/user/profile" )
        ServerP1Test.check( "T2b static beats param", r2.statusCode == 200 && r2.body == "profile" )
        Net.HttpResponse r3 = Net.HttpClient.httpGet( "http://127.0.0.1:19402/user/abc" )
        ServerP1Test.check( "T2c pathInt bad -> defVal", r3.statusCode == 200 && r3.body == "id=abc,n=-1" )
        Net.HttpResponse r4 = Net.HttpClient.httpGet( "http://127.0.0.1:19402/big/1234567890123" )
        ServerP1Test.check( "T2d pathInt64", r4.statusCode == 200 && r4.body == "v=1234567890123" )
        Net.HttpResponse r5 = Net.HttpClient.httpGet( "http://127.0.0.1:19402/api/v1/users" )
        ServerP1Test.check( "T2e group GET", r5.statusCode == 200 && r5.body == "users-list" )
        Net.HttpResponse r6 = Net.HttpClient.httpPost( "http://127.0.0.1:19402/api/v1/users", "", "text/plain" )
        ServerP1Test.check( "T2f group POST", r6.statusCode == 200 && r6.body == "users-create" )

        SLang.Server.stop()
    }

    # ── T3 查询参数 ──

    static void t3Query()
    {
        Console.println( "[ServerP1Test] T3 begin" )
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19403
        SLang.Server.configure( cfg )
        SLang.Server.reset()
        function h = function( SLang.Request req, SLang.Response res )
        {
            string fpart = "f0"
            if req.queryBool( "f", false )
            {
                fpart = "f1"
            }
            string zpart = "zz0"
            if req.hasQuery( "zz" )
            {
                zpart = "zz1"
            }
            res.text( "a=" + req.query( "a" ) + "|b=" + req.query( "b" ) + "|c=" + req.query( "c" ) + "|" + fpart + "|" + zpart + "|ai=" + req.queryInt( "a", 0 ).toString() + "|kn=" + req.queryKeys().length.toString() )
        }
        SLang.Server.onGet( "/q", h )
        SLang.Server.start( 19403 )

        string r = ServerP1Test.rawReq( 19403, "GET /q?a=1&b=hello%20world&c=x+y&f=on HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" )
        ServerP1Test.check( "T3a query decode (+/%20/int/bool/keys)",
            sfFind( r, "a=1" ) >= 0 && sfFind( r, "b=hello world" ) >= 0 && sfFind( r, "c=x y" ) >= 0
            && sfFind( r, "|f1|" ) >= 0 && sfFind( r, "|zz0|" ) >= 0 && sfFind( r, "ai=1" ) >= 0 && sfFind( r, "kn=4" ) >= 0 )

        SLang.Server.stop()
    }

    # ── T4 body 三形态 ──

    static void t4Body()
    {
        Console.println( "[ServerP1Test] T4 begin" )
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19404
        SLang.Server.configure( cfg )
        SLang.Server.reset()

        function hJson = function( SLang.Request req, SLang.Response res )
        {
            string fmt = "unk"
            if req.bodyFormat == SLang.EBodyFormat.Json
            {
                fmt = "json"
            }
            BaseJson jp = req.bodyJson()
            P1User u0 = req.body<P1User>()
            string jflag = "jok"
            if jp == null
            {
                jflag = "jnull"
            }
            string uflag = "uok"
            if u0 == null
            {
                uflag = "unull"
            }
            elif SystemStringLength( u0.name ) == 0
            {
                uflag = "uname0"
            }
            P1User u = u0
            BaseJson j = jp
            if u != null && j != null
            {
                res.text( fmt + "|u=" + u.name + "," + u.age.toString() + "|j=" + j.toJson() )
            }
            else
            {
                res.text( fmt + "|miss|" + jflag + "|" + uflag )
            }
        }
        SLang.Server.onPost( "/json1", hJson )

        function hForm = function( SLang.Request req, SLang.Response res )
        {
            string fmt = "unk"
            if req.bodyFormat == SLang.EBodyFormat.Form
            {
                fmt = "form"
            }
            string bpart = "b=null"
            if req.hasForm( "b" )
            {
                bpart = "b=" + req.form( "b" )
            }
            string fl = "flag0"
            if req.formBool( "flag", false )
            {
                fl = "flag1"
            }
            res.text( fmt + "|" + bpart + "|ai=" + req.formInt( "a", -1 ).toString() + "|" + fl + "|kn=" + req.formKeys().length.toString() )
        }
        SLang.Server.onPost( "/form1", hForm )

        function hBin = function( SLang.Request req, SLang.Response res )
        {
            string fmt = "unk"
            if req.bodyFormat == SLang.EBodyFormat.Binary
            {
                fmt = "bin"
            }
            ByteBuffer bb = req.bodyBytes()
            Int32 blen = 0
            Int32 v0 = 0 - 1
            Int32 v4 = 0 - 1
            if bb != null
            {
                blen = bb.readableBytes
                if blen >= 5
                {
                    UInt8 u0 = bb.readU8()
                    v0 = u0
                    bb.skipBytes( 3 )
                    UInt8 u4 = bb.readU8()
                    v4 = u4
                }
            }
            res.text( fmt + "|len=" + blen.toString() + "|v0=" + v0.toString() + "|v4=" + v4.toString() )
        }
        SLang.Server.onPost( "/bin1", hBin )
        SLang.Server.start( 19404 )

        # T4a json：DTO toData + bodyJson 回显
        Net.HttpResponse r1 = Net.HttpClient.httpPost( "http://127.0.0.1:19404/json1", "{\"name\":\"tom\",\"age\":18}", "application/json" )
        ServerP1Test.check( "T4a json DTO + bodyJson",
            r1.statusCode == 200 && sfFind( r1.body, "json|u=tom,18" ) == 0
            && sfFind( r1.body, "\"name\":\"tom\"" ) >= 0 && sfFind( r1.body, "\"age\":18" ) >= 0 )

        # T4b form：urlencoded 解码（'+'→空格）
        Net.HttpResponse r2 = Net.HttpClient.httpPost( "http://127.0.0.1:19404/form1", "a=1&b=hello+world&flag=on", "application/x-www-form-urlencoded" )
        ServerP1Test.check( "T4b form decode",
            r2.statusCode == 200 && sfFind( r2.body, "form|" ) == 0 && sfFind( r2.body, "b=hello world" ) >= 0
            && sfFind( r2.body, "ai=1" ) >= 0 && sfFind( r2.body, "|flag1|" ) >= 0 && sfFind( r2.body, "kn=3" ) >= 0 )

        # T4c bin：octet-stream 字节校验（原始客户端）
        Net.TcpStream c3 = Net.Tcp.connect( "127.0.0.1", 19404 )
        ByteBuffer body3 = ByteBuffer( 8 )
        body3.writeU8( SystemConvertUInt8( 0 ) )
        body3.writeU8( SystemConvertUInt8( 1 ) )
        body3.writeU8( SystemConvertUInt8( 2 ) )
        body3.writeU8( SystemConvertUInt8( 3 ) )
        body3.writeU8( SystemConvertUInt8( 251 ) )
        string head3 = "POST /bin1 HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Type: application/octet-stream\r\nContent-Length: 5\r\n\r\n"
        ByteBuffer req3 = ByteBuffer( 64 )
        ByteBuffer hb3 = ByteBuffer.fromString( head3 )
        req3.writeBytes( hb3 )
        hb3.release()
        req3.writeBytes( body3 )
        body3.release()
        c3.write( req3 )
        req3.release()
        Net.NetStream ns3 = c3
        string r3 = ServerP1Test.sfReadResp( ns3, false )
        c3.close()
        ServerP1Test.check( "T4c bin bytes + format",
            sfFind( r3, "bin|len=5" ) >= 0 && sfFind( r3, "v0=0" ) >= 0 && sfFind( r3, "v4=251" ) >= 0 )

        SLang.Server.stop()
    }

    # ── T5 Response 家族 ──

    static void t5Response()
    {
        Console.println( "[ServerP1Test] T5 begin" )
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19405
        SLang.Server.configure( cfg )
        SLang.Server.reset()

        function hCreated = function( SLang.Request req, SLang.Response res )
        {
            res.status( 201 ).header( "X-Custom", "abc" ).contentType( "application/json; charset=utf-8" ).rawJson( "{\"k\":1}" )
        }
        SLang.Server.onGet( "/created", hCreated )
        function hOk = function( SLang.Request req, SLang.Response res )
        {
            P1User u = P1User()
            u.name = "tom"
            u.age = 18
            res.jsonOk( u )
        }
        SLang.Server.onGet( "/ok", hOk )
        function hFail = function( SLang.Request req, SLang.Response res )
        {
            res.jsonFail( 1001, "bad" )
        }
        SLang.Server.onGet( "/fail", hFail )
        function hFail2 = function( SLang.Request req, SLang.Response res )
        {
            res.jsonFail( 1002, "bad2", 422 )
        }
        SLang.Server.onGet( "/fail2", hFail2 )
        function hHtml = function( SLang.Request req, SLang.Response res )
        {
            res.html( "<p>hi</p>" )
        }
        SLang.Server.onGet( "/html", hHtml )
        function hRedir = function( SLang.Request req, SLang.Response res )
        {
            res.redirect( "http://example.com/x" )
        }
        SLang.Server.onGet( "/redir", hRedir )
        function hTwice = function( SLang.Request req, SLang.Response res )
        {
            res.text( "first" )
            res.text( "second" )
        }
        SLang.Server.onGet( "/twice", hTwice )
        SLang.Server.start( 19405 )

        # T5a 链式 status/header/contentType + rawJson
        Net.HttpResponse r1 = Net.HttpClient.httpGet( "http://127.0.0.1:19405/created" )
        ServerP1Test.check( "T5a chain status/header/contentType",
            r1.statusCode == 201 && r1.headers.getHeader( "X-Custom" ) == "abc"
            && r1.headers.getHeader( "Content-Type" ) == "application/json; charset=utf-8"
            && sfFind( r1.body, "\"k\":1" ) >= 0 )

        # T5b jsonOk（ApiResult 封装）
        Net.HttpResponse r2 = Net.HttpClient.httpGet( "http://127.0.0.1:19405/ok" )
        ServerP1Test.check( "T5b jsonOk",
            r2.statusCode == 200 && sfFind( r2.body, "\"code\":0" ) >= 0
            && sfFind( r2.body, "\"message\":\"ok\"" ) >= 0 && sfFind( r2.body, "\"name\":\"tom\"" ) >= 0 )

        # T5c jsonFail 二参（HTTP 仍 200）
        Net.HttpResponse r3 = Net.HttpClient.httpGet( "http://127.0.0.1:19405/fail" )
        ServerP1Test.check( "T5c jsonFail 2-arg", r3.statusCode == 200 && sfFind( r3.body, "\"code\":1001" ) >= 0 && sfFind( r3.body, "bad" ) >= 0 )

        # T5d jsonFail 三参（HTTP 422）
        Net.HttpResponse r4 = Net.HttpClient.httpGet( "http://127.0.0.1:19405/fail2" )
        ServerP1Test.check( "T5d jsonFail 3-arg", r4.statusCode == 422 && sfFind( r4.body, "\"code\":1002" ) >= 0 )

        # T5e redirect 302 + Location（原始客户端；HttpClient 会自动跟随）
        string r5 = ServerP1Test.rawReq( 19405, "GET /redir HTTP/1.1\r\nHost: 127.0.0.1\r\n\r\n" )
        ServerP1Test.check( "T5e redirect 302 + Location", sfFind( r5, "302" ) >= 0 && sfFind( r5, "Location: http://example.com/x" ) >= 0 )

        # T5f html Content-Type
        Net.HttpResponse r6 = Net.HttpClient.httpGet( "http://127.0.0.1:19405/html" )
        ServerP1Test.check( "T5f html", r6.statusCode == 200 && r6.body == "<p>hi</p>" && sfFind( r6.headers.getHeader( "Content-Type" ), "text/html" ) == 0 )

        # T5g 终端方法二次调用：首个响应已暂存（isResponded=true）→ handler
        # 抛 AlreadyResponded → 引擎按"已响应照常写出"契约输出暂存响应
        #（200 "first"）；未响应才 500（§6.1 响应写出分支）
        Net.HttpResponse r7 = Net.HttpClient.httpGet( "http://127.0.0.1:19405/twice" )
        ServerP1Test.check( "T5g double terminal -> staged 200", r7.statusCode == 200 && r7.body == "first" )

        SLang.Server.stop()
    }

    # ── T6 WS 形态 A（HTTP 同端口升级 + 会话注册表）──

    static void t6WsApp()
    {
        Console.println( "[ServerP1Test] T6 begin" )
        SLang.ServerConfig cfg = SLang.ServerConfig()
        cfg.port = 19406
        SLang.Server.configure( cfg )
        SLang.Server.reset()
        function hWs = function( SLang.WsSession s )
        {
            bool looping = true
            while looping
            {
                Net.WsMessage m = s.receive()
                if m == null
                {
                    looping = false
                }
                else
                {
                    string t = m.text()
                    if t == "path"
                    {
                        s.sendText( s.pathParam( "room" ) + ":" + s.query( "name" ) )
                    }
                    elif t == "id"
                    {
                        s.sendText( "id:" + s.id )
                    }
                    elif t == "bcast"
                    {
                        Int32 sent = SLang.Server.broadcast( "/ws/{room}", "room-msg" )
                        s.sendText( "sent:" + sent.toString() )
                    }
                    else
                    {
                        s.sendText( "echo:" + t )
                    }
                    m.release()
                }
            }
        }
        SLang.Server.websocket( "/ws/{room}", hWs )
        SLang.Server.start( 19406 )

        # T6a 未注册路径升级 → 400（注意 /ws/none 会命中 {room} 通配，
        # 须用 /ws 前缀之外的真实未注册路径）
        P1WsClient bad = P1WsClient.connect( 19406, "/other" )
        ServerP1Test.check( "T6a ws unknown path 400", sfFind( bad.head, "400" ) >= 0 )
        bad.close()

        P1WsClient a = P1WsClient.connect( 19406, "/ws/alpha?name=alpha" )
        ServerP1Test.check( "T6b upgrade 101", sfFind( a.head, "101" ) >= 0 )
        P1WsClient b = P1WsClient.connect( 19406, "/ws/beta?name=beta" )

        # T6c 会话注册（异步 accept 协程注册有时序，轮询）
        function cond2 = function()
        {
            ret SLang.Server.sessions( "/ws/{room}" ).length == 2
        }
        ServerP1Test.check( "T6c sessions == 2", ServerP1Test.waitCond( cond2, 20 ) )

        a.sendText( "path" )
        ServerP1Test.check( "T6d ws pathParam+query", a.readText() == "alpha:alpha" )
        b.sendText( "path" )
        ServerP1Test.check( "T6e ws pathParam 2nd", b.readText() == "beta:beta" )
        a.sendText( "id" )
        string idr = a.readText()
        # 会话 id = GUID（36 字符）；空 id（3 字符）必不过
        ServerP1Test.check( "T6f session id", sfFind( idr, "id:" ) == 0 && SystemStringLength( idr ) > 36 )
        a.sendText( "bcast" )
        ServerP1Test.check( "T6g broadcast both", a.readText() == "room-msg" && a.readText() == "sent:2" && b.readText() == "room-msg" )
        a.sendText( "hi" )
        ServerP1Test.check( "T6h ws echo", a.readText() == "echo:hi" )

        a.close()
        function cond1 = function()
        {
            ret SLang.Server.sessions( "/ws/{room}" ).length == 1
        }
        ServerP1Test.check( "T6i unregister on close", ServerP1Test.waitCond( cond1, 20 ) )
        b.close()
        SLang.Server.stop()
    }

    # ── T7 WS 形态 B（独立端口 WebSocketServer）──

    static void t7Standalone()
    {
        Console.println( "[ServerP1Test] T7 begin" )
        SLang.WebSocketServer.configure( 19407 )
        function hMsg = function( SLang.WsSession s, Net.WsMessage m )
        {
            string t = m.text()
            if t == "who"
            {
                s.sendText( "n=" + SLang.WebSocketServer.sessions().length.toString() )
            }
            else
            {
                s.sendText( "ind:" + t )
            }
            m.release()
        }
        SLang.WebSocketServer.onMessage( hMsg )
        function hClose = function( SLang.WsSession s )
        {
            Console.println( "[ServerP1Test] T7 session closed" )
        }
        SLang.WebSocketServer.onClose( hClose )
        SLang.WebSocketServer.start( 19407 )

        P1WsClient w = P1WsClient.connect( 19407, "/any" )
        ServerP1Test.check( "T7a standalone upgrade", sfFind( w.head, "101" ) >= 0 )
        w.sendText( "hi" )
        ServerP1Test.check( "T7b standalone echo", w.readText() == "ind:hi" )
        w.sendText( "who" )
        ServerP1Test.check( "T7c sessions()==1", w.readText() == "n=1" )
        Int32 sent = SLang.WebSocketServer.broadcast( "bcast-all" )
        ServerP1Test.check( "T7d standalone broadcast", sent == 1 && w.readText() == "bcast-all" )

        SLang.WebSocketServer.stop()
        function cond0 = function()
        {
            ret SLang.WebSocketServer.sessions().length == 0
        }
        ServerP1Test.check( "T7e stop clears sessions", SLang.WebSocketServer.isRunning == false && ServerP1Test.waitCond( cond0, 20 ) )
        w.close()
    }

    # ── T8 DuplicateRoute fail-fast ──

    static void t8Duplicate()
    {
        Console.println( "[ServerP1Test] T8 begin" )
        SLang.Server.reset()
        function h1 = function( SLang.Request req, SLang.Response res )
        {
            res.text( "1" )
        }
        function h2 = function( SLang.Request req, SLang.Response res )
        {
            res.text( "2" )
        }
        function hw = function( SLang.WsSession s )
        {
        }
        SLang.Server.onGet( "/dup", h1 )
        SLang.Server.onGet( "/dup", h2 )
        string dup = SLang.Router.checkDuplicate()
        ServerP1Test.check( "T8a checkDuplicate http", dup == "GET /dup" )

        SLang.Server.reset()
        SLang.Server.websocket( "/dupws", hw )
        SLang.Server.websocket( "/dupws", hw )
        string dupWs = SLang.Router.checkDuplicate()
        ServerP1Test.check( "T8b checkDuplicate ws", dupWs == "WS /dupws" )

        # start() fail-fast：抛错且不监听
        SLang.Server.reset()
        SLang.Server.onGet( "/dup", h1 )
        SLang.Server.onGet( "/dup", h2 )
        bool threw = false
        label startDup
        {
            SLang.Server.start( 19408 )
        }
        catch
        {
            threw = true
        }
        ServerP1Test.check( "T8c start fail-fast", threw && SLang.Server.isRunning == false )
    }

    static fun()
    {
        ServerP1Test.t1Verbs()
        ServerP1Test.t2Routes()
        ServerP1Test.t3Query()
        ServerP1Test.t4Body()
        ServerP1Test.t5Response()
        ServerP1Test.t6WsApp()
        ServerP1Test.t7Standalone()
        ServerP1Test.t8Duplicate()
        Console.println( "[ServerP1Test] all done" )
    }
}

# ============================================================================
# P1WsClient -- 手搓 WS 测试客户端（ServerCoreTest.s4 客户端同款实现）：
# 升级握手 + 掩码文本帧发送 + 服务端文本帧读取
# ============================================================================
class P1WsClient
{
    Net.TcpStream c = null
    ByteBuffer rbuf = null
    string head = ""

    _init_()
    {
        this.c = null
        this.rbuf = null
        this.head = ""
    }

    # 握手：Upgrade 请求 → 读响应头到空行（101 / 400 判定用 head；残留进 rbuf）
    static P1WsClient connect( Int32 port, string path ) throws
    {
        P1WsClient w = P1WsClient()
        w.c = Net.Tcp.connect( "127.0.0.1", port )
        Int64 keyId = SystemRandomFill( 16 )
        ByteBuffer kb = ByteBuffer.fromHandle( keyId )
        string key = Base64.encodeToString( kb )
        kb.release()
        string reqText = "GET " + path + " HTTP/1.1\r\nHost: 127.0.0.1\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Key: " + key + "\r\nSec-WebSocket-Version: 13\r\n\r\n"
        w.rbuf = ByteBuffer()
        ByteBuffer rb = ByteBuffer.fromString( reqText )
        w.c.write( rb )
        rb.release()
        string resp = ""
        while ServerP1Test.sfFind( resp, "\r\n\r\n" ) < 0
        {
            ByteBuffer b = ByteBuffer( 256 )
            Int32 n = w.c.read( b )
            if n == 0
            {
                break
            }
            w.rbuf.writeBytes( b, n )
            resp = w.rbuf.toString()
        }
        w.head = resp
        Int32 hdrEnd = ServerP1Test.sfFind( resp, "\r\n\r\n" )
        if hdrEnd >= 0
        {
            # 丢弃已消费头区（readerIndex 推进到帧数据起点）
            w.rbuf.skipBytes( hdrEnd + 4 )
        }
        ret w
    }

    # 掩码文本帧（len < 126 假设；掩码键固定 1/2/3/4 同 ServerCoreTest.s4）
    void sendText( string text ) throws
    {
        Int32 m0 = 1
        Int32 m1 = 2
        Int32 m2 = 3
        Int32 m3 = 4
        Int32 plen = SystemStringLength( text )
        ByteBuffer frame = ByteBuffer( plen + 16 )
        frame.writeU8( SystemConvertUInt8( 0x81 ) )
        frame.writeU8( SystemConvertUInt8( 0x80 | plen ) )
        frame.writeU8( SystemConvertUInt8( m0 ) )
        frame.writeU8( SystemConvertUInt8( m1 ) )
        frame.writeU8( SystemConvertUInt8( m2 ) )
        frame.writeU8( SystemConvertUInt8( m3 ) )
        for Int32 i = 0, i < plen, i = i + 1
        {
            Int32 ch = SystemStringCharCodeAt( text, i )
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
        this.c.write( frame )
        frame.release()
    }

    # 读一条服务端文本帧（无掩码；EOF/Close/空载荷帧返回 null）
    string readText() throws
    {
        bool skip = true
        while skip
        {
            while this.rbuf.readableBytes < 2
            {
                ByteBuffer rb1 = this.rbuf
                Int32 n = this.c.read( rb1 )
                if n == 0
                {
                    ret null
                }
            }
            UInt8 b0 = this.rbuf.readU8()
            Int32 h0 = b0
            UInt8 b1 = this.rbuf.readU8()
            Int32 h1 = b1
            Int32 opcode = h0 & 0x0F
            Int32 flen = h1 & 0x7F
            if opcode == 8
            {
                ret null
            }
            if flen > 0
            {
                skip = false
                while this.rbuf.readableBytes < flen
                {
                    ByteBuffer rb2 = this.rbuf
                    Int32 n2 = this.c.read( rb2 )
                    if n2 == 0
                    {
                        ret null
                    }
                }
                ret this.rbuf.readString( flen )
            }
            # 空载荷帧（Ping/Pong）：继续读下一条
        }
        ret null
    }

    void close()
    {
        if this.c != null
        {
            this.c.close()
        }
    }
}
