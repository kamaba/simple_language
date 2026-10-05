import Std;
import Core;

# ============================================================================
# Crawler.sl -- 简单 HTTPS 爬虫
#
# 功能：抓取一组公开测试站点的 HTTPS 页面 -> 朴素解析 <title> 与 <a href>
#   链接 -> 汇总为结构化 JSON -> 写入 crawl_result.json。
#
# 技术要点（全部为库既有能力，无新增系统调用）：
#   - Net.HttpClient（HTTP/1.1 over TLS，md/syntax/net.md §10）
#       * caPem 空 = 跳过证书验证（简单爬虫取舍；生产请提供 CA PEM 文本）
#       * 每请求一连接（Connection: close）
#       * 不跟随重定向 / 不解压 —— 请求头显式 Accept-Encoding: identity
#   - data CrawlPage + toJsonPretty()（Core/Data.sl，md 见 JsonTest）
#       * List<string> 成员序列化为 JSON 数组（json_system_method.c 正向规则）
#   - 顶层汇总对象由页面 toJsonPretty() 文本拼接（顶层字段均为本脚本
#     生成的安全标量，页面文本转义由 toJson 后端保证）
#   - File.writeAllText（Std.IO.File）
#
# HTML 解析为朴素字符串扫描（SystemStringLength / SystemStringCharCodeAt /
#   SystemStringRange，HttpTest.sfFind 同款模式），不做完整 DOM。
# ============================================================================

# 每页一条抓取记录（data -> JSON 对象）
data CrawlPage
{
    url = ""
    ok = false
    status = 0
    title = ""
    contentType = ""
    bodyLength = 0
    linkCount = 0
    links = List<string>()
    fetchedAt = ""
    durationMs = 0
    error = ""
}

Crawler
{
    # ---- 基础文本工具（HttpTest.sfFind 同款逐字节比对） ----

    # 子串查找（从 from 起，返回首匹配下标；未找到 -1；sub 空 = from）
    static Int32 sfFind( string text, string sub, Int32 from )
    {
        Int32 tlen = SystemStringLength( text )
        Int32 slen = SystemStringLength( sub )
        if slen == 0
        {
            ret from
        }
        if from < 0
        {
            from = 0
        }
        Int32 last = tlen - slen
        Int32 i = from
        while i <= last
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
            i = i + 1
        }
        ret -1
    }

    # 大小写不敏感子串查找（Uri.fold 逐字符折叠，HttpClient._containsFold 同款）
    static Int32 sfFindFold( string text, string sub, Int32 from )
    {
        Int32 tlen = SystemStringLength( text )
        Int32 slen = SystemStringLength( sub )
        if slen == 0
        {
            ret from
        }
        if from < 0
        {
            from = 0
        }
        Int32 last = tlen - slen
        Int32 i = from
        while i <= last
        {
            bool matched = true
            for Int32 j = 0, j < slen, j = j + 1
            {
                Int32 a = Net.Uri.fold( SystemStringCharCodeAt( text, i + j ) )
                Int32 b = Net.Uri.fold( SystemStringCharCodeAt( sub, j ) )
                if a != b
                {
                    matched = false
                    break
                }
            }
            if matched
            {
                ret i
            }
            i = i + 1
        }
        ret -1
    }

    # 单字符查找（ch 为字符码；未找到 -1）
    static Int32 sfFindChar( string text, Int32 ch, Int32 from )
    {
        Int32 tlen = SystemStringLength( text )
        if from < 0
        {
            from = 0
        }
        Int32 i = from
        while i < tlen
        {
            if SystemStringCharCodeAt( text, i ) == ch
            {
                ret i
            }
            i = i + 1
        }
        ret -1
    }

    # 去首尾空白（空格 32 / \t 9 / \n 10 / \r 13）
    static string sfTrim( string s )
    {
        Int32 len = SystemStringLength( s )
        Int32 st = 0
        while st < len
        {
            Int32 c = SystemStringCharCodeAt( s, st )
            if c == 32 || c == 9 || c == 10 || c == 13
            {
                st = st + 1
            }
            else
            {
                break
            }
        }
        Int32 en = len
        while en > st
        {
            Int32 c2 = SystemStringCharCodeAt( s, en - 1 )
            if c2 == 32 || c2 == 9 || c2 == 10 || c2 == 13
            {
                en = en - 1
            }
            else
            {
                break
            }
        }
        ret SystemStringRange( s, st, en )
    }

    # ---- 朴素 HTML 解析 ----

    # 提取 <title> 文本（大小写不敏感；未找到返回空串）
    static string sfExtractTitle( string html )
    {
        Int32 t = Crawler.sfFindFold( html, "<title", 0 )
        if t < 0
        {
            ret ""
        }
        Int32 gt = Crawler.sfFind( html, ">", t )
        if gt < 0
        {
            ret ""
        }
        Int32 endTag = Crawler.sfFindFold( html, "</title", gt )
        if endTag < 0
        {
            ret ""
        }
        ret Crawler.sfTrim( SystemStringRange( html, gt + 1, endTag ) )
    }

    # 在单个标签文本（如 "<a href=\"x\">") 中取属性值（大小写不敏感；
    # 支持双引号/单引号/裸值；未找到返回空串）
    static string sfExtractAttr( string tag, string name )
    {
        Int32 tlen = SystemStringLength( tag )
        Int32 nlen = SystemStringLength( name )
        Int32 pos = 0
        while pos < tlen
        {
            Int32 at = Crawler.sfFindFold( tag, name, pos )
            if at < 0
            {
                ret ""
            }
            Int32 eq = at + nlen
            if eq >= tlen || SystemStringCharCodeAt( tag, eq ) != 61   # 61 = '='
            {
                pos = at + 1
                continue
            }
            Int32 vs = eq + 1
            # 跳过 '=' 后的空白
            while vs < tlen
            {
                Int32 vc = SystemStringCharCodeAt( tag, vs )
                if vc == 32 || vc == 9
                {
                    vs = vs + 1
                }
                else
                {
                    break
                }
            }
            if vs >= tlen
            {
                ret ""
            }
            Int32 q = SystemStringCharCodeAt( tag, vs )
            if q == 34 || q == 39   # 34 = " / 39 = '
            {
                Int32 end = Crawler.sfFindChar( tag, q, vs + 1 )
                if end < 0
                {
                    ret ""
                }
                ret SystemStringRange( tag, vs + 1, end )
            }
            # 裸值：到空白或 '>'
            Int32 ve = vs
            while ve < tlen
            {
                Int32 ch = SystemStringCharCodeAt( tag, ve )
                if ch == 32 || ch == 9 || ch == 62
                {
                    break
                }
                ve = ve + 1
            }
            ret SystemStringRange( tag, vs, ve )
        }
        ret ""
    }

    # 提取所有 <a href="..."> 链接（最多 maxLinks 条）
    # "<a" 后必须跟空白或 '>'（排除 <abbr> 等）；HTML 实体不解码（原样保留）
    static List<string> sfExtractLinks( string html, Int32 maxLinks )
    {
        List<string> links = List<string>()
        Int32 tlen = SystemStringLength( html )
        Int32 pos = 0
        while pos < tlen
        {
            Int32 lt = Crawler.sfFindFold( html, "<a", pos )
            if lt < 0
            {
                break
            }
            Int32 after = lt + 2
            if after >= tlen
            {
                break
            }
            Int32 c = SystemStringCharCodeAt( html, after )
            if c != 32 && c != 9 && c != 10 && c != 13 && c != 62   # 空白 或 '>'
            {
                pos = lt + 2
                continue
            }
            Int32 gt = Crawler.sfFind( html, ">", after )
            if gt < 0
            {
                break
            }
            string tag = SystemStringRange( html, lt, gt + 1 )
            string href = Crawler.sfExtractAttr( tag, "href" )
            if href != ""
            {
                links.add( href )
                if links.length >= maxLinks
                {
                    break
                }
            }
            pos = gt + 1
        }
        ret links
    }

    # ---- 抓取单页（异常 -> 记录错误字段，不中断整体） ----
    static CrawlPage sfFetch( Net.HttpClient client, string url )
    {
        CrawlPage page = new()
        page.url = url
        page.links = List<string>()
        OS.DateTime t0 = OS.DateTime.now()
        page.fetchedAt = t0.toString( "yyyy-MM-dd HH:mm:ss" )
        label fetchBlock
        {
            Net.HttpRequest req = Net.HttpRequest()
            req.method = 0   # 0 = GET
            req.url = url
            # identity：HttpClient 不解压，显式禁用压缩避免拿到 gzip 乱码
            req.headers.setHeader( "Accept-Encoding", "identity" )
            req.headers.setHeader( "User-Agent", "SL-Crawler/0.1" )
            req.headers.setHeader( "Accept", "text/html,application/xhtml+xml" )
            Net.HttpResponse resp = client.send( req )
            page.status = resp.statusCode
            page.ok = resp.isOk
            string ct = resp.headers.getHeader( "Content-Type" )
            if ct == null
            {
                ct = ""
            }
            page.contentType = ct
            page.bodyLength = SystemStringLength( resp.body )
            if resp.isOk
            {
                page.title = Crawler.sfExtractTitle( resp.body )
                List<string> links = Crawler.sfExtractLinks( resp.body, 50 )
                page.links = links
                page.linkCount = links.length
            }
        }
        catch e
        {
            page.ok = false
            Error te = e as Error
            if te != null
            {
                page.error = "code " + te.code.toString() + " " + te.message
            }
            else
            {
                page.error = "unknown error"
            }
        }
        Int64 t1ms = OS.DateTime.now().unixTimeMillis
        page.durationMs = ( t1ms - t0.unixTimeMillis ).toInt32()
        ret page
    }

    # ---- 汇总 JSON（顶层标量手拼，页面对象已在抓取时用 data.toJsonPretty 预序列化） ----
    # 注：List<data> 的 _getItem_ 返回类型擦除为 Core.Object 且 as 不能转 data，
    #     故容器只存 string（页面 JSON 片段），data 实例不进容器
    static string sfBuildResultJson( List<string> pageJsons, string startedAt, Int32 durationMs, Int32 successCount )
    {
        Int32 total = pageJsons.length
        string json = "{\n"
        json = json + "  \"startedAt\": \"" + startedAt + "\",\n"
        json = json + "  \"durationMs\": " + durationMs.toString() + ",\n"
        json = json + "  \"totalUrls\": " + total.toString() + ",\n"
        json = json + "  \"successCount\": " + successCount.toString() + ",\n"
        json = json + "  \"pages\": [\n"
        for Int32 i = 0, i < total, i = i + 1
        {
            string pj = pageJsons._getItem_( i ) as string
            json = json + Crawler.sfIndent( pj, "    " )
            if i + 1 < total
            {
                json = json + ","
            }
            json = json + "\n"
        }
        json = json + "  ]\n"
        json = json + "}"
        ret json
    }

    # 逐行加缩进（把 toJsonPretty 的 2 空格层级嵌进 pages 数组）
    static string sfIndent( string text, string pad )
    {
        string res = ""
        Int32 len = SystemStringLength( text )
        Int32 i = 0
        while i < len
        {
            Int32 c = SystemStringCharCodeAt( text, i )
            res = res + SystemStringRange( text, i, i + 1 )
            if c == 10 && i + 1 < len   # '\n' 且非末行
            {
                res = res + pad
            }
            i = i + 1
        }
        ret res
    }

    # ---- 入口 ----
    static fun()
    {
        Console.println( "===== Crawler start =====" )

        # 目标：公开测试站点（可按需增删；单页失败不影响其余）
        List<string> urls = List<string>()
        urls.add( "https://example.com/" )
        urls.add( "https://www.httpbin.org/html" )
        urls.add( "https://quotes.toscrape.com/" )

        # 客户端：caPem 空 = 跳过证书验证（简单爬虫取舍）
        Net.HttpClient client = Net.HttpClient()
        client.connectTimeoutMs = 10000
        client.readTimeoutMs = 15000

        OS.DateTime start = OS.DateTime.now()
        string startedAt = start.toString( "yyyy-MM-dd HH:mm:ss" )
        Int64 startMs = start.unixTimeMillis

        List<string> pageJsons = List<string>()
        Int32 okCount = 0
        for Int32 i = 0, i < urls.length, i = i + 1
        {
            string url = urls._getItem_( i ) as string
            Console.println( "fetching: " + url )
            CrawlPage page = Crawler.sfFetch( client, url )
            pageJsons.add( page.toJsonPretty() )
            if page.ok
            {
                okCount = okCount + 1
                Console.println( "  -> status=" + page.status.toString() + " title=\"" + page.title + "\" links=" + page.linkCount.toString() )
            }
            else
            {
                Console.println( "  -> failed (" + page.error + ")" )
            }
        }

        Int64 endMs = OS.DateTime.now().unixTimeMillis
        Int32 durationMs = ( endMs - startMs ).toInt32()
        string json = Crawler.sfBuildResultJson( pageJsons, startedAt, durationMs, okCount )

        string outPath = "crawl_result.json"
        bool saved = File.writeAllText( outPath, json )
        Console.println( "saved: " + outPath + " = " + saved.toString() )
        Console.println( "pages=" + pageJsons.length.toString() + " durationMs=" + durationMs.toString() )
        Console.println( "===== Crawler end =====" )
    }
}
