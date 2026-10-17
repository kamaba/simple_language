import Std;
import Core;

# ============================================================================
# HttpWebTest.sl -- HTTP/HTTPS 公网站点集成测试（独立工程）
#
# 目标站点（市面流行的公开测试站，2026-10-06 实测可达）：
#   - example.com                   IANA 保留示例域名（明文 HTTP + HTTPS 双通道）
#   - www.httpbin.org               经典 HTTP 请求/响应测试服务（全套回显端点）
#   - jsonplaceholder.typicode.com  流行假 REST API（posts 资源 CRUD）
#   - quotes.toscrape.com           知名爬虫练习站（HTTPS 第二证书域）
#
# 覆盖功能点（G 组，依赖 Net.HttpClient / Net.HttpRequest / Text.Json /
#   Base64 / 协程（httpGetAsync / sendAsync + await + waitAll2）既有能力，
#   无新增系统调用）：
#   G1  明文 HTTP GET        状态行/协议版本/响应头/body；example.com chunked
#                            （Cloudflare 前置，无 Content-Length 头）+
#                            httpbin /get 明文 Content-Length 精确读一致性
#   G2  HTTPS GET            同一 httpGet 入口按 scheme 自动走 TLS
#                            （caPem 空 = 跳过证书验证，双站点证书域）
#   G3  GET + query 回显     httpbin /get -> Text.Json args/origin/url 校验
#   G4  自定义请求头回显     httpbin /headers -> X-Sl-Test / User-Agent 回显
#   G5  POST 表单回显        httpbin /post -> form 字段解析回显
#   G6  PUT 回显             httpbin /put -> data 原文回显
#   G7  DELETE               httpbin /delete -> 200 + url 回显
#   G8  HEAD 无 body         Content-Length 头存在但 body 为空
#   G9  状态码语义           /status/200|404|500 -> statusCode / isOk 映射
#   G10 重定向不跟随         /redirect/2 -> 302 + Location 头（不自动跳转）
#   G11 Basic Auth           无凭证 401 / Authorization: Basic base64 -> 200
#   G12 chunked 流式 body    /stream/5 -> 无 Content-Length + 5 行聚合
#   G13 REST CRUD            jsonplaceholder /posts GET/POST(201)/PUT/DELETE
#   G14 服务端延迟 + 耗时    /delay/2 -> 客户端实测时长 >= 1500ms
#   G15 UTF-8 多语言 body    /encoding/utf8 -> 非 ASCII 文本解码
#   G16 错误路径             ftp 协议(2) / 连接被拒(2) / 域名解析失败(3) /
#                            空 host(1) / https 读超时(Timeout 4|9)
#   G17 协程等待 HttpClient  httpGetAsync / sendAsync 原生返回 Task：
#                            await 取回响应 / waitAll2 并发聚合；协程内
#                            网络 IO 挂起当前协程（Option A），不阻塞调度器
#
# 网络健壮性：每个网络组独立 label{}...catch{}——公网站点偶发抖动只记
#   一条 FAIL 后继续，不阻断其余组（Crawler.sfFetch 同款取舍）。
#   除入口覆盖组（G1 httpGet / G5 httpPost / G16 错误路径）外，全部公网
#   请求走 sfGet/sfPost/sfClient（连接 15s / 读 25s）：便捷入口默认
#   readTimeoutMs=0 无限阻塞，站点挂起不回包会卡死整个套件。
# 错误码取值依据：HttpError.UriFormat=1 / UnsupportedScheme=2；
#   NetError.ConnectFailed=2 / HostNotFound=3 / Timeout=4；TlsError.Timeout=9。
# 编写约定同 HttpTest.sl / Crawler.sl / YamlTest.sl。
# ============================================================================

HttpWebTest
{
    static Int32 s_pass = 0
    static Int32 s_fail = 0

    # 断言辅助：OK/FAIL 单行输出 + 计数（CSharpTest.passed/failed 同款）
    static check( string name, bool cond )
    {
        if cond
        {
            s_pass = s_pass + 1
            Console.println( "[HttpWebTest] " + name + " : OK" )
        }
        else
        {
            s_fail = s_fail + 1
            Console.println( "[HttpWebTest] " + name + " : FAIL" )
        }
    }

    # 网络组异常兜底：整组记一条 FAIL 后继续（站点抖动不阻断套件）
    static groupError( string group, Int32 code, string message )
    {
        s_fail = s_fail + 1
        Console.println( "[HttpWebTest] " + group + " : FAIL (network error code=" + code.toString() + " " + message + ")" )
    }

    # 朴素子串查找（返回首匹配下标；未找到返回 -1；sub 空返回 0）
    # HttpTest.sfFind / ProcessTest.strContains 同款逐字节比对
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

    # 统计字符出现次数（ch 为字符码；chunked 行计数用）
    static Int32 sfCountChar( string text, Int32 ch )
    {
        Int32 n = 0
        Int32 len = SystemStringLength( text )
        for Int32 i = 0, i < len, i = i + 1
        {
            if SystemStringCharCodeAt( text, i ) == ch
            {
                n = n + 1
            }
        }
        ret n
    }

    # 带超时的 client（公网站点抖动兜底：连接 15s / 读 25s）。
    # 便捷 httpGet/httpPost 默认 readTimeoutMs=0 = 无限阻塞，
    # 站点挂起不回包会卡死整个套件，故除入口覆盖组外统一走本辅助。
    static Net.HttpClient sfClient()
    {
        Net.HttpClient c = Net.HttpClient()
        c.connectTimeoutMs = 15000
        c.readTimeoutMs = 25000
        ret c
    }

    # 带超时的 GET（等价 Net.HttpClient.httpGet；公网偶发 TLS 握手/
    #   连接抖动自动重试一次，两次都失败才重抛给组级 catch）
    static Net.HttpResponse sfGet( string url )
    {
        Net.HttpResponse r = null
        label labTry1
        {
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Get
            req.url = url
            r = HttpWebTest.sfClient().send( req )
        }
        catch
        {
            label labTry2
            {
                Net.HttpRequest req2 = Net.HttpRequest()
                req2.method = Net.HttpMethod.Get
                req2.url = url
                r = HttpWebTest.sfClient().send( req2 )
            }
            catch
            {
                throw
            }
        }
        ret r
    }

    # 带超时的 POST（等价 Net.HttpClient.httpPost；重试语义同 sfGet）
    static Net.HttpResponse sfPost( string url, string body, string contentType )
    {
        Net.HttpResponse r = null
        label labTry1
        {
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Post
            req.url = url
            req.body = body
            req.headers.setHeader( "Content-Type", contentType )
            r = HttpWebTest.sfClient().send( req )
        }
        catch
        {
            label labTry2
            {
                Net.HttpRequest req2 = Net.HttpRequest()
                req2.method = Net.HttpMethod.Post
                req2.url = url
                req2.body = body
                req2.headers.setHeader( "Content-Type", contentType )
                r = HttpWebTest.sfClient().send( req2 )
            }
            catch
            {
                throw
            }
        }
        ret r
    }

    # ---- G1: 明文 HTTP GET（example.com + httpbin /get） ----
    static testPlainHttp()
    {
        Console.println( "----- G1: 明文 HTTP GET ( example.com / httpbin ) -----" )
        label labG1
        {
            # 便捷静态入口 httpGet 走明文 http（入口覆盖保留）
            Net.HttpResponse r = Net.HttpClient.httpGet( "http://example.com/" )
            check( "G1 status 200 + isOk", r.statusCode == 200 && r.isOk )
            check( "G1 protocol HTTP/1.1", r.protocol == "HTTP/1.1" )
            check( "G1 reason phrase OK", r.reasonPhrase == "OK" )
            string ct = r.headers.getHeader( "Content-Type" )
            check( "G1 Content-Type text/html", ct != null && sfFind( ct, "text/html" ) == 0 )
            check( "G1 body contains Example Domain", sfFind( r.body, "Example Domain" ) >= 0 )
            # example.com 明文由 Cloudflare 前置：chunked 响应无 Content-Length 头
            check( "G1 chunked: no Content-Length", r.contentLength < 0 )
            check( "G1 headers count >= 3", r.headers.count >= 3 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G1 plain http", te.code, te.message )
            }
            else
            {
                groupError( "G1 plain http", -1, "unknown error" )
            }
        }

        # 明文 + Content-Length 精确读一致性（httpbin 固定回 Content-Length）
        Net.HttpResponse rb = null
        label labG1b
        {
            rb = HttpWebTest.sfGet( "http://www.httpbin.org/get" )
            Int64 blen = SystemStringLength( rb.body )
            check( "G1b status 200", rb.statusCode == 200 && rb.isOk )
            check( "G1b Content-Length == body length", rb.contentLength == blen )
        }
        catch e
        {
            Error te2 = e as Error
            if te2 != null
            {
                groupError( "G1b httpbin plain", te2.code, te2.message )
            }
            else
            {
                groupError( "G1b httpbin plain", -1, "unknown error" )
            }
        }
    }

    # ---- G2: HTTPS GET（同一入口自动 TLS；双站点证书域） ----
    static testHttpsGet()
    {
        Console.println( "----- G2: HTTPS GET ( example.com / quotes.toscrape.com ) -----" )

        Net.HttpResponse r1 = null
        label labG2a
        {
            r1 = HttpWebTest.sfGet( "https://example.com/" )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G2 https example.com", te.code, te.message )
            }
            else
            {
                groupError( "G2 https example.com", -1, "unknown error" )
            }
        }
        if r1 != null
        {
            check( "G2 example.com status 200", r1.statusCode == 200 && r1.isOk )
            check( "G2 example.com body", sfFind( r1.body, "Example Domain" ) >= 0 )
        }

        Net.HttpResponse r2 = null
        label labG2b
        {
            r2 = HttpWebTest.sfGet( "https://quotes.toscrape.com/" )
        }
        catch e
        {
            Error te2 = e as Error
            if te2 != null
            {
                groupError( "G2 https quotes.toscrape.com", te2.code, te2.message )
            }
            else
            {
                groupError( "G2 https quotes.toscrape.com", -1, "unknown error" )
            }
        }
        if r2 != null
        {
            check( "G2 quotes status 200", r2.statusCode == 200 && r2.isOk )
            check( "G2 quotes body title", sfFind( r2.body, "Quotes to Scrape" ) >= 0 )
        }
    }

    # ---- G3: GET + query 回显（httpbin /get） ----
    static testGetQuery()
    {
        Console.println( "----- G3: GET + query 回显 ( httpbin /get ) -----" )
        label labG3
        {
            Net.HttpResponse r = HttpWebTest.sfGet( "https://www.httpbin.org/get?slk=hello&num=42" )
            check( "G3 status 200", r.statusCode == 200 && r.isOk )
            string ct = r.headers.getHeader( "Content-Type" )
            check( "G3 Content-Type json", ct != null && sfFind( ct, "application/json" ) >= 0 )
            Text.Json j = Text.Json( r.body )
            check( "G3 parse non-empty", j.isNotEmpty )
            check( "G3 args/slk echo", j.getStr( "args/slk" ) == "hello" )
            check( "G3 args/num echo", j.getStr( "args/num" ) == "42" )
            check( "G3 url field", sfFind( j.getStr( "url" ), "httpbin.org" ) >= 0 )
            check( "G3 origin non-empty", SystemStringLength( j.getStr( "origin" ) ) > 0 )
            check( "G3 Host header echo", j.getStr( "headers/Host" ) == "www.httpbin.org" )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G3 httpbin /get", te.code, te.message )
            }
            else
            {
                groupError( "G3 httpbin /get", -1, "unknown error" )
            }
        }
    }

    # ---- G4: 自定义请求头回显（httpbin /headers） ----
    static testHeaderEcho()
    {
        Console.println( "----- G4: 自定义请求头回显 ( httpbin /headers ) -----" )
        label labG4
        {
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Get
            req.url = "https://www.httpbin.org/headers"
            req.headers.setHeader( "X-Sl-Test", "sl-web" )
            req.headers.setHeader( "User-Agent", "SL-HttpWebTest/1.0" )
            Net.HttpResponse r = HttpWebTest.sfClient().send( req )
            check( "G4 status 200", r.statusCode == 200 && r.isOk )
            Text.Json j = Text.Json( r.body )
            check( "G4 custom header echo", j.getStr( "headers/X-Sl-Test" ) == "sl-web" )
            check( "G4 User-Agent echo", j.getStr( "headers/User-Agent" ) == "SL-HttpWebTest/1.0" )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G4 httpbin /headers", te.code, te.message )
            }
            else
            {
                groupError( "G4 httpbin /headers", -1, "unknown error" )
            }
        }
    }

    # ---- G5: POST 表单回显（httpbin /post） ----
    static testPostForm()
    {
        Console.println( "----- G5: POST 表单回显 ( httpbin /post ) -----" )
        label labG5
        {
            Net.HttpResponse r = Net.HttpClient.httpPost( "https://www.httpbin.org/post", "name=sl&ver=1", "application/x-www-form-urlencoded" )
            check( "G5 status 200 + isOk", r.statusCode == 200 && r.isOk )
            Text.Json j = Text.Json( r.body )
            check( "G5 form name echo", j.getStr( "form/name" ) == "sl" )
            check( "G5 form ver echo", j.getStr( "form/ver" ) == "1" )
            check( "G5 Content-Type echo", sfFind( j.getStr( "headers/Content-Type" ), "application/x-www-form-urlencoded" ) >= 0 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G5 httpbin /post", te.code, te.message )
            }
            else
            {
                groupError( "G5 httpbin /post", -1, "unknown error" )
            }
        }
    }

    # ---- G6: PUT 回显（httpbin /put） ----
    static testPut()
    {
        Console.println( "----- G6: PUT 回显 ( httpbin /put ) -----" )
        label labG6
        {
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Put
            req.url = "https://www.httpbin.org/put"
            req.body = "put-payload"
            req.headers.setHeader( "Content-Type", "text/plain" )
            Net.HttpResponse r = HttpWebTest.sfClient().send( req )
            check( "G6 status 200 + isOk", r.statusCode == 200 && r.isOk )
            Text.Json j = Text.Json( r.body )
            check( "G6 data echo", j.getStr( "data" ) == "put-payload" )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G6 httpbin /put", te.code, te.message )
            }
            else
            {
                groupError( "G6 httpbin /put", -1, "unknown error" )
            }
        }
    }

    # ---- G7: DELETE（httpbin /delete） ----
    static testDelete()
    {
        Console.println( "----- G7: DELETE ( httpbin /delete ) -----" )
        label labG7
        {
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Delete
            req.url = "https://www.httpbin.org/delete"
            Net.HttpResponse r = HttpWebTest.sfClient().send( req )
            check( "G7 status 200 + isOk", r.statusCode == 200 && r.isOk )
            Text.Json j = Text.Json( r.body )
            check( "G7 url echo", sfFind( j.getStr( "url" ), "/delete" ) >= 0 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G7 httpbin /delete", te.code, te.message )
            }
            else
            {
                groupError( "G7 httpbin /delete", -1, "unknown error" )
            }
        }
    }

    # ---- G8: HEAD 无 body（httpbin /get） ----
    static testHead()
    {
        Console.println( "----- G8: HEAD 无 body ( httpbin /get ) -----" )
        label labG8
        {
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Head
            req.url = "https://www.httpbin.org/get"
            Net.HttpResponse r = HttpWebTest.sfClient().send( req )
            check( "G8 status 200 + isOk", r.statusCode == 200 && r.isOk )
            check( "G8 body empty", SystemStringLength( r.body ) == 0 )
            check( "G8 Content-Length > 0", r.contentLength > 0 )
            string ct = r.headers.getHeader( "Content-Type" )
            check( "G8 Content-Type json", ct != null && sfFind( ct, "application/json" ) >= 0 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G8 httpbin HEAD", te.code, te.message )
            }
            else
            {
                groupError( "G8 httpbin HEAD", -1, "unknown error" )
            }
        }
    }

    # ---- G9: 状态码语义（/status/200 | /status/404 | /status/500） ----
    static testStatusCodes()
    {
        Console.println( "----- G9: 状态码语义 ( httpbin /status ) -----" )

        Net.HttpResponse r200 = null
        label labG9a
        {
            r200 = HttpWebTest.sfGet( "https://www.httpbin.org/status/200" )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G9 status/200", te.code, te.message )
            }
            else
            {
                groupError( "G9 status/200", -1, "unknown error" )
            }
        }
        if r200 != null
        {
            check( "G9 200 -> isOk", r200.statusCode == 200 && r200.isOk )
        }

        Net.HttpResponse r404 = null
        label labG9b
        {
            r404 = HttpWebTest.sfGet( "https://www.httpbin.org/status/404" )
        }
        catch e
        {
            Error te2 = e as Error
            if te2 != null
            {
                groupError( "G9 status/404", te2.code, te2.message )
            }
            else
            {
                groupError( "G9 status/404", -1, "unknown error" )
            }
        }
        if r404 != null
        {
            check( "G9 404 -> statusCode", r404.statusCode == 404 && !r404.isOk )
        }

        Net.HttpResponse r500 = null
        label labG9c
        {
            r500 = HttpWebTest.sfGet( "https://www.httpbin.org/status/500" )
        }
        catch e
        {
            Error te3 = e as Error
            if te3 != null
            {
                groupError( "G9 status/500", te3.code, te3.message )
            }
            else
            {
                groupError( "G9 status/500", -1, "unknown error" )
            }
        }
        if r500 != null
        {
            check( "G9 500 -> statusCode", r500.statusCode == 500 && !r500.isOk )
        }
    }

    # ---- G10: 重定向不跟随（/redirect/2 -> 302 + Location） ----
    static testRedirect()
    {
        Console.println( "----- G10: 重定向不跟随 ( httpbin /redirect/2 ) -----" )
        label labG10
        {
            Net.HttpResponse r = HttpWebTest.sfGet( "https://www.httpbin.org/redirect/2" )
            check( "G10 status 302", r.statusCode == 302 )
            check( "G10 not isOk", !r.isOk )
            string loc = r.headers.getHeader( "Location" )
            check( "G10 Location present", loc != null && SystemStringLength( loc ) > 0 )
            check( "G10 Location points redirect", loc != null && sfFind( loc, "redirect" ) >= 0 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G10 httpbin /redirect/2", te.code, te.message )
            }
            else
            {
                groupError( "G10 httpbin /redirect/2", -1, "unknown error" )
            }
        }
    }

    # ---- G11: Basic Auth（无凭证 401 / 凭证 200） ----
    static testBasicAuth()
    {
        Console.println( "----- G11: Basic Auth ( httpbin /basic-auth ) -----" )

        Net.HttpResponse r401 = null
        label labG11a
        {
            r401 = HttpWebTest.sfGet( "https://www.httpbin.org/basic-auth/sluser/slpass" )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G11 no credentials", te.code, te.message )
            }
            else
            {
                groupError( "G11 no credentials", -1, "unknown error" )
            }
        }
        if r401 != null
        {
            check( "G11 no credentials -> 401", r401.statusCode == 401 && !r401.isOk )
        }

        Net.HttpResponse r200 = null
        label labG11b
        {
            string cred = Base64.encodeToString( "sluser:slpass" )
            check( "G11 base64 credential vector", cred == "c2x1c2VyOnNscGFzcw==" )
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Get
            req.url = "https://www.httpbin.org/basic-auth/sluser/slpass"
            req.headers.setHeader( "Authorization", "Basic " + cred )
            r200 = HttpWebTest.sfClient().send( req )
        }
        catch e
        {
            Error te2 = e as Error
            if te2 != null
            {
                groupError( "G11 with credentials", te2.code, te2.message )
            }
            else
            {
                groupError( "G11 with credentials", -1, "unknown error" )
            }
        }
        if r200 != null
        {
            check( "G11 with credentials -> 200", r200.statusCode == 200 && r200.isOk )
            Text.Json j = Text.Json( r200.body )
            check( "G11 authenticated true", j.getBool( "authenticated" ) )
            check( "G11 user echo", j.getStr( "user" ) == "sluser" )
        }
    }

    # ---- G12: chunked 流式 body（/stream/5 -> 5 行聚合） ----
    static testChunked()
    {
        Console.println( "----- G12: chunked 流式 body ( httpbin /stream/5 ) -----" )
        label labG12
        {
            Net.HttpResponse r = HttpWebTest.sfGet( "https://www.httpbin.org/stream/5" )
            check( "G12 status 200 + isOk", r.statusCode == 200 && r.isOk )
            check( "G12 no Content-Length", r.contentLength < 0 )
            string enc = r.headers.getHeader( "Transfer-Encoding" )
            check( "G12 Transfer-Encoding chunked", enc != null && sfFind( enc, "chunked" ) >= 0 )
            check( "G12 5 lines aggregated", sfCountChar( r.body, 10 ) == 5 )
            check( "G12 line content json", sfFind( r.body, "\"url\"" ) >= 0 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G12 httpbin /stream/5", te.code, te.message )
            }
            else
            {
                groupError( "G12 httpbin /stream/5", -1, "unknown error" )
            }
        }
    }

    # ---- G13: REST CRUD（jsonplaceholder.typicode.com /posts） ----
    static testRestApi()
    {
        Console.println( "----- G13: REST CRUD ( jsonplaceholder /posts ) -----" )

        # GET /posts/1
        Net.HttpResponse rg = null
        label labG13a
        {
            rg = HttpWebTest.sfGet( "https://jsonplaceholder.typicode.com/posts/1" )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G13 GET /posts/1", te.code, te.message )
            }
            else
            {
                groupError( "G13 GET /posts/1", -1, "unknown error" )
            }
        }
        if rg != null
        {
            check( "G13 GET status 200", rg.statusCode == 200 && rg.isOk )
            Text.Json jg = Text.Json( rg.body )
            check( "G13 GET userId == 1", jg.getInt( "userId" ) == 1 )
            check( "G13 GET id == 1", jg.getInt( "id" ) == 1 )
            check( "G13 GET title non-empty", SystemStringLength( jg.getStr( "title" ) ) > 0 )
        }

        # POST /posts -> 201
        Net.HttpResponse rp = null
        label labG13b
        {
            rp = HttpWebTest.sfPost( "https://jsonplaceholder.typicode.com/posts", "{\"title\":\"sl-title\",\"body\":\"sl-content\",\"userId\":1}", "application/json" )
        }
        catch e
        {
            Error te2 = e as Error
            if te2 != null
            {
                groupError( "G13 POST /posts", te2.code, te2.message )
            }
            else
            {
                groupError( "G13 POST /posts", -1, "unknown error" )
            }
        }
        if rp != null
        {
            check( "G13 POST status 201", rp.statusCode == 201 && rp.isOk )
            Text.Json jp = Text.Json( rp.body )
            check( "G13 POST echo title", jp.getStr( "title" ) == "sl-title" )
            check( "G13 POST assigned id 101", jp.getInt( "id" ) == 101 )
        }

        # PUT /posts/1 -> 200
        Net.HttpResponse ru = null
        label labG13c
        {
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Put
            req.url = "https://jsonplaceholder.typicode.com/posts/1"
            req.body = "{\"id\":1,\"title\":\"sl-put\",\"body\":\"sl-put-body\",\"userId\":1}"
            req.headers.setHeader( "Content-Type", "application/json" )
            ru = HttpWebTest.sfClient().send( req )
        }
        catch e
        {
            Error te3 = e as Error
            if te3 != null
            {
                groupError( "G13 PUT /posts/1", te3.code, te3.message )
            }
            else
            {
                groupError( "G13 PUT /posts/1", -1, "unknown error" )
            }
        }
        if ru != null
        {
            check( "G13 PUT status 200", ru.statusCode == 200 && ru.isOk )
            Text.Json ju = Text.Json( ru.body )
            check( "G13 PUT echo title", ju.getStr( "title" ) == "sl-put" )
            check( "G13 PUT echo id", ju.getInt( "id" ) == 1 )
        }

        # DELETE /posts/1 -> 200
        Net.HttpResponse rd = null
        label labG13d
        {
            Net.HttpRequest req2 = Net.HttpRequest()
            req2.method = Net.HttpMethod.Delete
            req2.url = "https://jsonplaceholder.typicode.com/posts/1"
            rd = HttpWebTest.sfClient().send( req2 )
        }
        catch e
        {
            Error te4 = e as Error
            if te4 != null
            {
                groupError( "G13 DELETE /posts/1", te4.code, te4.message )
            }
            else
            {
                groupError( "G13 DELETE /posts/1", -1, "unknown error" )
            }
        }
        if rd != null
        {
            check( "G13 DELETE status 200", rd.statusCode == 200 && rd.isOk )
        }
    }

    # ---- G14: 服务端延迟 + 客户端耗时（/delay/2） ----
    static testDelay()
    {
        Console.println( "----- G14: 服务端延迟 + 耗时 ( httpbin /delay/2 ) -----" )
        label labG14
        {
            OS.DateTime t0 = OS.DateTime.now()
            Int64 t0ms = t0.unixTimeMillis
            Net.HttpResponse r = HttpWebTest.sfGet( "https://www.httpbin.org/delay/2" )
            Int64 dt = OS.DateTime.now().unixTimeMillis - t0ms
            check( "G14 status 200 + isOk", r.statusCode == 200 && r.isOk )
            check( "G14 elapsed >= 1500ms", dt >= 1500 )
            Console.println( "  G14 elapsedMs=" + dt.toString() )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G14 httpbin /delay/2", te.code, te.message )
            }
            else
            {
                groupError( "G14 httpbin /delay/2", -1, "unknown error" )
            }
        }
    }

    # ---- G15: UTF-8 多语言 body（/encoding/utf8） ----
    static testUtf8()
    {
        Console.println( "----- G15: UTF-8 多语言 body ( httpbin /encoding/utf8 ) -----" )
        label labG15
        {
            Net.HttpResponse r = HttpWebTest.sfGet( "https://www.httpbin.org/encoding/utf8" )
            check( "G15 status 200 + isOk", r.statusCode == 200 && r.isOk )
            check( "G15 body contains Unicode Demo", sfFind( r.body, "Unicode Demo" ) >= 0 )
            check( "G15 multilingual text", sfFind( r.body, "Kuhn" ) >= 0 )
            check( "G15 body length > 1000", SystemStringLength( r.body ) > 1000 )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G15 httpbin /encoding/utf8", te.code, te.message )
            }
            else
            {
                groupError( "G15 httpbin /encoding/utf8", -1, "unknown error" )
            }
        }
    }

    # ---- G16: 错误路径（协议/建连/超时，全部确定性触发） ----
    static testErrors()
    {
        Console.println( "----- G16: 错误路径 -----" )

        # ftp:// -> UnsupportedScheme(2)
        bool f1 = false
        label labG16a
        {
            try Net.HttpClient.httpGet( "ftp://www.httpbin.org/x" )
        }
        catch e
        {
            Error te = e as Error
            f1 = te != null && te.code == 2
        }
        check( "G16 ftp scheme -> UnsupportedScheme(2)", f1 )

        # 本机无监听端口 -> NetError.ConnectFailed(2)
        bool f2 = false
        label labG16b
        {
            try Net.HttpClient.httpGet( "http://127.0.0.1:19371/" )
        }
        catch e
        {
            Error te2 = e as Error
            f2 = te2 != null && te2.code == 2
        }
        check( "G16 connection refused -> ConnectFailed(2)", f2 )

        # 域名解析失败 -> NetError.HostNotFound(3)
        bool f3 = false
        label labG16c
        {
            try Net.HttpClient.httpGet( "http://no-such-host-xyz.invalid/" )
        }
        catch e
        {
            Error te3 = e as Error
            f3 = te3 != null && te3.code == 3
        }
        check( "G16 host not found -> HostNotFound(3)", f3 )

        # 空 host -> HttpError.UriFormat(1)
        bool f4 = false
        label labG16d
        {
            try Net.HttpClient.httpGet( "http://" )
        }
        catch e
        {
            Error te4 = e as Error
            f4 = te4 != null && te4.code == 1
        }
        check( "G16 empty host -> UriFormat(1)", f4 )

        # https 读超时：/delay/10 服务端睡 10s，读窗口 3s 先到
        # TLS 层超时错误码可能映射为 NetError.Timeout(4) 或 TlsError.Timeout(9)
        bool f5 = false
        label labG16e
        {
            Net.HttpClient c = Net.HttpClient()
            c.connectTimeoutMs = 15000
            c.readTimeoutMs = 3000
            Net.HttpRequest req = Net.HttpRequest()
            req.method = Net.HttpMethod.Get
            req.url = "https://www.httpbin.org/delay/10"
            Net.HttpResponse r = c.send( req )
        }
        catch e
        {
            Error te5 = e as Error
            f5 = te5 != null && ( te5.code == 4 || te5.code == 9 )
        }
        check( "G16 https read timeout -> Timeout(4|9)", f5 )
    }

    # ---- G17: 协程等待 HttpClient（原生异步方法 httpGetAsync / sendAsync） ----
    static testCoroutine()
    {
        Console.println( "----- G17: 协程异步 HttpClient ( httpGetAsync / sendAsync + await / waitAll2 ) -----" )
        label labG17
        {
            # 单请求：静态异步入口直接返回 Task，await 取回 HttpResponse
            # 协程内网络 IO（connect / send / recv / TLS 握手）挂起当前协程
            # 而不阻塞调度器（Option A，md/syntax/coroutine.md §16.2）
            Task t1 = Net.HttpClient.httpGetAsync( "https://www.httpbin.org/get" )
            Net.HttpResponse r1 = await t1 as Net.HttpResponse
            check( "G17 await single status 200", r1 != null && r1.statusCode == 200 && r1.isOk )
            Text.Json j1 = Text.Json( r1.body )
            check( "G17 await single body json", j1.getStr( "url" ) == "https://www.httpbin.org/get" )

            # 并发聚合：实例 sendAsync（配置生效：连接 15s / 读 25s）+ waitAll2
            Net.HttpClient c = Net.HttpClient()
            c.connectTimeoutMs = 15000
            c.readTimeoutMs = 25000
            Net.HttpRequest req2 = Net.HttpRequest()
            req2.method = Net.HttpMethod.Get
            req2.url = "https://www.httpbin.org/get?slk=coro"
            Net.HttpRequest req3 = Net.HttpRequest()
            req3.method = Net.HttpMethod.Get
            req3.url = "https://example.com/"
            Task t2 = c.sendAsync( req2 )
            Task t3 = c.sendAsync( req3 )
            Coroutine.waitAll2( t2, t3 )
            Net.HttpResponse r2 = await t2 as Net.HttpResponse
            Net.HttpResponse r3 = await t3 as Net.HttpResponse
            check( "G17 waitAll2 first 200", r2 != null && r2.statusCode == 200 && r2.isOk )
            Text.Json j2 = Text.Json( r2.body )
            check( "G17 waitAll2 args echo", j2.getStr( "args/slk" ) == "coro" )
            check( "G17 waitAll2 second 200", r3 != null && r3.statusCode == 200 && r3.isOk )
            check( "G17 waitAll2 tasks dead", t2.isDead && t3.isDead )
        }
        catch e
        {
            Error te = e as Error
            if te != null
            {
                groupError( "G17 coroutine await", te.code, te.message )
            }
            else
            {
                groupError( "G17 coroutine await", -1, "unknown error" )
            }
        }
    }

    static fun()
    {
        Console.println( "===== HttpWebTest start =====" )
        Console.println( "目标站点: example.com / www.httpbin.org / jsonplaceholder.typicode.com / quotes.toscrape.com" )
        testPlainHttp()
        testHttpsGet()
        testGetQuery()
        testHeaderEcho()
        testPostForm()
        testPut()
        testDelete()
        testHead()
        testStatusCodes()
        testRedirect()
        testBasicAuth()
        testChunked()
        testRestApi()
        testDelay()
        testUtf8()
        testErrors()
        testCoroutine()
        Console.println( "===== HttpWebTest end : pass=" + s_pass.toString() + " fail=" + s_fail.toString() + " =====" )
    }
}
