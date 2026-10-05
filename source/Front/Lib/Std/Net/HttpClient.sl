namespace Net
{
    # ============================================================================
    # Net/HttpClient.sl — HTTP/1.1 客户端（TcpStream / TlsStream 之上）
    # 设计契约：csimple_lang/md/design/NET_DESIGN.md L1118（Phase 3: TLS ->
    #   HttpClient）；STREAM_DESIGN.md §9.6（body 三态读取语义）
    #
    # 用法:
    #   import Std;
    #   var resp = Net.HttpClient.httpGet( "http://127.0.0.1:8080/hello" )
    #   resp.statusCode       # 200
    #   resp.body             # 响应文本
    #   resp.headers.getHeader( "Content-Type" )
    #
    #   var req = Net.HttpRequest()
    #   req.method = 1            # 1 = POST（0=GET 1=POST 2=PUT 3=DELETE 4=HEAD）
    #   req.url = "https://example.com/api"
    #   req.body = "ping=1"
    #   req.headers.add( "Content-Type", "application/x-www-form-urlencoded" )
    #   var c = Net.HttpClient()
    #   c.caPem = caText          # https CA 验证（空 = 跳过验证）
    #   var r2 = c.send( req )
    #
    # 语义要点:
    #   - 每请求一连接：自动发 Connection: close，响应读毕即关连接
    #     （无连接复用，Stage B 限制，详见 md/syntax/net.md 限制表）
    #   - body 三态读取：Content-Length 精确读 / Transfer-Encoding: chunked
    #     逐块聚合 / 两者皆无读到 EOF
    #   - HEAD 与 204/205/304 状态不读 body
    #   - https 经 TlsStream.wrap（caPem 空 = 跳过验证，与 TlsStream 一致）
    #   - 响应解析经预读缓冲（ByteBuffer），跨 read 边界的安全行解析
    #   - 非法 URL 抛 HttpError.UriFormat；非 http/https 抛
    #     HttpError.UnsupportedScheme；响应报文损坏抛 HttpError.Protocol；
    #     底层网络异常（NetError / TlsError）原样透传
    # ============================================================================

    # 请求方法约定（HttpRequest.method 为 Int32 字段）:
    #   0 = GET   1 = POST   2 = PUT   3 = DELETE   4 = HEAD
    # 注意：不设 HttpMethod 枚举——SL 枚举不能作字段类型（ParseMemberExpress
    #   不支持，全库先例 SocketShutdown / FileMode / SeekOrigin 均只作方法
    #   参数类型），枚举值赋值亦无先例，故直接用数字约定

    # ============================================================================
    # HttpHeaders — 请求 / 响应头集合（名字大小写不敏感）
    # ============================================================================

    public class HttpHeaders
    {
        List<string> _names = null
        List<string> _values = null

        _init_()
        {
            this._names = List<string>()
            this._values = List<string>()
        }

        public get int count()
        {
            ret this._names.length
        }

        # 追加（允许同名头共存）
        public void add( string name, string value )
        {
            this._names.add( name )
            this._values.add( value )
        }

        # 覆盖同名头（大小写不敏感；不存在则追加）
        # 注意：方法名不能叫 set——set 是 SL 属性 setter 关键字，会触发解析歧义
        # 注意：跨类静态调用的实参先提取局部变量——直接传 this._names[i]
        #   （this.字段[索引器] 复合表达式）会导致参数收集失败、方法匹配 NotFound
        public void setHeader( string name, string value )
        {
            for Int32 i = 0, i < this._names.length, i = i + 1
            {
                string cur = this._names[i]
                if Uri.eqFold( cur, name )
                {
                    this._values[i] = value
                    ret
                }
            }
            this.add( name, value )
        }

        # 取值（大小写不敏感；不存在返回 null）
        # 注意：方法名不能叫 get——get 是 SL 属性 getter 关键字，会触发解析歧义
        public string getHeader( string name )
        {
            for Int32 i = 0, i < this._names.length, i = i + 1
            {
                string cur = this._names[i]
                if Uri.eqFold( cur, name )
                {
                    ret this._values[i]
                }
            }
            ret null
        }

        public bool contains( string name )
        {
            ret this.getHeader( name ) != null
        }

        public string nameAt( int index )
        {
            ret this._names[index]
        }

        public string valueAt( int index )
        {
            ret this._values[index]
        }
    }

    # ============================================================================
    # HttpRequest — 请求描述
    # ============================================================================

    public class HttpRequest
    {
        # 请求方法（0=GET 1=POST 2=PUT 3=DELETE 4=HEAD；默认零值 = GET。
        # 不能用 HttpMethod 枚举类型——SL 枚举不能作字段类型，见文件头注释）
        public Int32 method = 0
        public string url = ""
        public HttpHeaders headers = null
        # 文本 body（UTF-8；空串 = 无 body，内嵌 NUL 会被截断）
        public string body = ""

        _init_()
        {
            this.headers = HttpHeaders()
        }
    }

    # ============================================================================
    # HttpResponse — 响应
    # ============================================================================

    public class HttpResponse
    {
        public Int32 statusCode = 0
        public string reasonPhrase = ""
        public string protocol = ""
        public HttpHeaders headers = null
        public string body = ""

        _init_()
        {
            this.headers = HttpHeaders()
        }

        public get bool isOk()
        {
            ret this.statusCode >= 200 && this.statusCode < 300
        }

        # Content-Length 头（Int64；头缺失 / 格式非法返回 -1）
        public get Int64 contentLength()
        {
            string v = this.headers.getHeader( "Content-Length" )
            if v == null
            {
                ret -1
            }
            ret HttpClient._parseDec64( v )
        }
    }

    # ============================================================================
    # HttpClient — 客户端（每请求一连接）
    # ============================================================================

    public class HttpClient
    {
        # 连接超时毫秒（Tcp.connectTimeout 透传）
        public Int32 connectTimeoutMs = 10000
        # 读超时毫秒（<= 0 = 无限等待；超时抛 NetError.Timeout）
        public Int32 readTimeoutMs = 0
        # https CA 证书链 PEM 文本（空 = 跳过验证，与 TlsOptions.caPem 一致）
        public string caPem = ""

        # 便捷 GET
        # 注意：方法名不能叫 get——get 是 SL 属性 getter 关键字，会触发解析歧义
        public static HttpResponse httpGet( string url ) throws
        {
            HttpRequest req = HttpRequest()
            req.method = 0   # 0 = GET
            req.url = url
            ret HttpClient().send( req )
        }

        # 便捷 POST（文本 body + Content-Type）
        public static HttpResponse httpPost( string url, string body, string contentType ) throws
        {
            HttpRequest req = HttpRequest()
            req.method = 1   # 1 = POST
            req.url = url
            req.body = body
            req.headers.add( "Content-Type", contentType )
            ret HttpClient().send( req )
        }

        # 发送请求并读取完整响应（body 全量缓冲为文本）
        public HttpResponse send( HttpRequest req ) throws
        {
            # 解析 URL（非法抛 UriFormat，发生在建连前）
            Uri uri = Uri( req.url )
            if !uri.isHttp && !uri.isHttps
            {
                throw HttpError.UnsupportedScheme
            }

            # ---- 建连（明文 TCP / TLS）----
            # 注意：Tcp.connectTimeout 已直接返回 TcpStream（TcpStream._init_
            #   只接受 TcpSocket，再包一层 TcpStream(...) 会参数类型不匹配）；
            #   同 namespace 内类名不加前缀（TlsStream._init_ 先例）
            TcpStream raw = Tcp.connectTimeout( uri.host, uri.effectivePort, this.connectTimeoutMs )
            raw.setNoDelay( true )
            if this.readTimeoutMs > 0
            {
                raw.setReadTimeout( this.readTimeoutMs )
            }
            NetStream stream = raw
            if uri.isHttps
            {
                TlsOptions opt = TlsOptions()
                opt.caPem = this.caPem
                opt.hostname = uri.host
                stream = TlsStream.wrap( raw, opt )
            }

            # ---- 请求行 + 头（Host 自动管理；Connection: close 单次连接）----
            string reqText = HttpClient._methodName( req.method ) + " " + uri.pathAndQuery + " HTTP/1.1\r\n"
            reqText = reqText + "Host: " + uri.hostHeader + "\r\n"
            Int32 bodyLen = SystemStringLength( req.body )
            if bodyLen > 0 && !req.headers.contains( "Content-Length" )
            {
                reqText = reqText + "Content-Length: " + bodyLen.toString() + "\r\n"
            }
            for Int32 i = 0, i < req.headers.count, i = i + 1
            {
                reqText = reqText + req.headers.nameAt( i ) + ": " + req.headers.valueAt( i ) + "\r\n"
            }
            if !req.headers.contains( "Connection" )
            {
                reqText = reqText + "Connection: close\r\n"
            }
            reqText = reqText + "\r\n"
            stream.write( ByteBuffer.fromString( reqText + req.body ) )

            # ---- 响应解析（异常路径也尽力关连接后原样上抛）----
            ByteBuffer recvBuf = ByteBuffer()
            HttpResponse resp = null
            # 注意：label{}...catch{} 中 label 块本身就是受 catch 保护的语句块
            #   （TryTest rethrowOuter 先例），块内直接写赋值即可；try 语句
            #   （"try <函数调用>"）后跟赋值会报"没有适合的符号 Assign"
            label parseBlock
            {
                resp = HttpClient._readResponse( stream, recvBuf, req )
            }
            catch
            {
                try stream.close()
                throw
            }
            stream.close()
            ret resp
        }

        # ── 响应解析（一切读经 recvBuf 预读缓冲中转）──

        static HttpResponse _readResponse( NetStream s, ByteBuffer buf, HttpRequest req ) throws
        {
            # ---- 状态行："<protocol> SP <3 位码> [SP reason]" ----
            string statusLine = HttpClient._readLine( s, buf )
            if statusLine == null
            {
                throw HttpError.Protocol
            }
            statusLine = HttpClient._stripEol( statusLine )
            Int32 slen = SystemStringLength( statusLine )
            Int32 sp1 = -1
            for Int32 i = 0, i < slen, i = i + 1
            {
                if SystemStringCharCodeAt( statusLine, i ) == 32
                {
                    sp1 = i
                    break
                }
            }
            if sp1 <= 0 || sp1 + 4 > slen
            {
                throw HttpError.Protocol
            }
            Int32 code = 0
            for Int32 k = 0, k < 3, k = k + 1
            {
                Int32 c = SystemStringCharCodeAt( statusLine, sp1 + 1 + k )
                if c < 48 || c > 57
                {
                    throw HttpError.Protocol
                }
                code = code * 10 + ( c - 48 )
            }
            # code 后必须空格（或恰为行尾）
            Int32 reasonStart = sp1 + 4
            if reasonStart < slen
            {
                if SystemStringCharCodeAt( statusLine, reasonStart ) != 32
                {
                    throw HttpError.Protocol
                }
                reasonStart = reasonStart + 1
            }

            HttpResponse resp = HttpResponse()
            resp.protocol = SystemStringRange( statusLine, 0, sp1 )
            resp.statusCode = code
            resp.reasonPhrase = SystemStringRange( statusLine, reasonStart, slen )

            # ---- 头：到空行结束 ----
            while true
            {
                string line = HttpClient._readLine( s, buf )
                if line == null
                {
                    throw HttpError.Protocol
                }
                line = HttpClient._stripEol( line )
                Int32 llen = SystemStringLength( line )
                if llen == 0
                {
                    break
                }
                Int32 ci = -1
                for Int32 k = 0, k < llen, k = k + 1
                {
                    if SystemStringCharCodeAt( line, k ) == 58
                    {
                        ci = k
                        break
                    }
                }
                if ci <= 0
                {
                    throw HttpError.Protocol
                }
                # 值去前导空格
                Int32 vs = ci + 1
                while vs < llen && SystemStringCharCodeAt( line, vs ) == 32
                {
                    vs = vs + 1
                }
                resp.headers.add( SystemStringRange( line, 0, ci ), SystemStringRange( line, vs, llen ) )
            }

            # ---- body 三态 ----
            bool noBody = req.method == 4   # 4 = HEAD
            if code == 204 || code == 205 || code == 304
            {
                noBody = true
            }
            if !noBody
            {
                ByteBuffer bodyBuf = ByteBuffer()
                string te = resp.headers.getHeader( "Transfer-Encoding" )
                bool chunked = te != null && HttpClient._containsFold( te, "chunked" )
                if chunked
                {
                    HttpClient._readChunked( s, buf, bodyBuf )
                }
                else
                {
                    string cls = resp.headers.getHeader( "Content-Length" )
                    if cls != null
                    {
                        Int64 cl = HttpClient._parseDec64( cls )
                        if cl < 0 || cl > 2000000000
                        {
                            throw HttpError.Protocol
                        }
                        if cl > 0
                        {
                            Int32 need = cl.toInt32()
                            bodyBuf.ensureWritable( need )
                            HttpClient._readBytes( s, buf, bodyBuf, need )
                        }
                    }
                    else
                    {
                        HttpClient._readToEof( s, buf, bodyBuf )
                    }
                }
                resp.body = bodyBuf.readString( bodyBuf.readableBytes )
            }
            ret resp
        }

        # chunked：hex size 行 -> 块数据 -> CRLF -> size 0 -> trailer 空行
        static void _readChunked( NetStream s, ByteBuffer buf, ByteBuffer dst ) throws
        {
            while true
            {
                string line = HttpClient._readLine( s, buf )
                if line == null
                {
                    throw HttpError.Protocol
                }
                line = HttpClient._stripEol( line )
                Int32 size = HttpClient._chunkSize( line )
                if size < 0
                {
                    throw HttpError.Protocol
                }
                if size == 0
                {
                    # trailer 头直到空行
                    while true
                    {
                        string t = HttpClient._readLine( s, buf )
                        if t == null
                        {
                            throw HttpError.Protocol
                        }
                        if SystemStringLength( HttpClient._stripEol( t ) ) == 0
                        {
                            break
                        }
                    }
                    ret
                }
                dst.ensureWritable( size )
                HttpClient._readBytes( s, buf, dst, size )
                # 块尾 CRLF
                ByteBuffer crlf = ByteBuffer()
                HttpClient._readBytes( s, buf, crlf, 2 )
            }
        }

        # 精确读 need 字节入 dst：先消费 buf 预读，不足再从流拉
        static void _readBytes( NetStream s, ByteBuffer buf, ByteBuffer dst, Int32 need ) throws
        {
            while need > 0
            {
                if buf.readableBytes > 0
                {
                    Int32 take = buf.readableBytes
                    if take > need
                    {
                        take = need
                    }
                    buf.readBytes( dst, take )
                    need = need - take
                }
                else
                {
                    if buf.writableBytes < 4096
                    {
                        buf.discardReadBytes()
                        buf.ensureWritable( 4096 )
                    }
                    Int32 n = s.read( buf )
                    if n == 0
                    {
                        # EOF 截断
                        throw HttpError.Protocol
                    }
                }
            }
        }

        # 读到 EOF（无 Content-Length / chunked 时的 body）
        static void _readToEof( NetStream s, ByteBuffer buf, ByteBuffer dst ) throws
        {
            while true
            {
                if buf.readableBytes > 0
                {
                    dst.writeBytes( buf )
                }
                if buf.writableBytes < 4096
                {
                    buf.discardReadBytes()
                    buf.ensureWritable( 4096 )
                }
                Int32 n = s.read( buf )
                if n == 0
                {
                    ret
                }
            }
        }

        # 读一行（含 '\n'）：buf 有行直接取，否则从流拉；EOF 无行返回 null
        static string _readLine( NetStream s, ByteBuffer buf ) throws
        {
            string line = buf.readLine()
            while line == null
            {
                if buf.writableBytes < 4096
                {
                    buf.discardReadBytes()
                    buf.ensureWritable( 4096 )
                }
                Int32 n = s.read( buf )
                if n == 0
                {
                    ret null
                }
                line = buf.readLine()
            }
            ret line
        }

        # ── 文本辅助 ──

        # 去行尾 CRLF / LF
        static string _stripEol( string line )
        {
            Int32 len = SystemStringLength( line )
            if len > 0 && SystemStringCharCodeAt( line, len - 1 ) == 10
            {
                len = len - 1
            }
            if len > 0 && SystemStringCharCodeAt( line, len - 1 ) == 13
            {
                len = len - 1
            }
            ret SystemStringRange( line, 0, len )
        }

        # 方法名（Int32 -> 请求行文本；0=GET 1=POST 2=PUT 3=DELETE 4=HEAD）
        static string _methodName( Int32 m )
        {
            if m == 0
            {
                ret "GET"
            }
            if m == 1
            {
                ret "POST"
            }
            if m == 2
            {
                ret "PUT"
            }
            if m == 3
            {
                ret "DELETE"
            }
            if m == 4
            {
                ret "HEAD"
            }
            ret "GET"
        }

        # chunk size 行解析：hex 段到 ';' 前截止（忽略 chunk 扩展）
        static Int32 _chunkSize( string line )
        {
            Int32 len = SystemStringLength( line )
            Int32 hexEnd = len
            for Int32 i = 0, i < len, i = i + 1
            {
                if SystemStringCharCodeAt( line, i ) == 59
                {
                    hexEnd = i
                    break
                }
            }
            ret HttpClient._parseHex( SystemStringRange( line, 0, hexEnd ) )
        }

        # 十六进制解析（Int32 中转，> 2e9 返回 -1）
        static Int32 _parseHex( string s )
        {
            Int32 len = SystemStringLength( s )
            if len == 0
            {
                ret -1
            }
            Int64 v = 0
            for Int32 i = 0, i < len, i = i + 1
            {
                Int32 c = SystemStringCharCodeAt( s, i )
                Int32 d = -1
                if c >= 48 && c <= 57
                {
                    d = c - 48
                }
                elif c >= 65 && c <= 70
                {
                    d = c - 55
                }
                elif c >= 97 && c <= 102
                {
                    d = c - 87
                }
                else
                {
                    ret -1
                }
                v = v * 16 + d
                if v > 2000000000
                {
                    ret -1
                }
            }
            ret v.toInt32()
        }

        # 十进制解析（Content-Length 用；非法返回 -1）
        static Int64 _parseDec64( string s )
        {
            Int32 len = SystemStringLength( s )
            if len == 0
            {
                ret -1
            }
            Int64 v = 0
            for Int32 i = 0, i < len, i = i + 1
            {
                Int32 c = SystemStringCharCodeAt( s, i )
                if c < 48 || c > 57
                {
                    ret -1
                }
                v = v * 10 + ( c - 48 )
                if v > 2000000000
                {
                    ret -1
                }
            }
            ret v
        }

        # 忽略大小写子串查找（Transfer-Encoding 值判 chunked 用）
        static bool _containsFold( string hay, string needle )
        {
            Int32 hl = SystemStringLength( hay )
            Int32 nl = SystemStringLength( needle )
            if nl == 0
            {
                ret true
            }
            if nl > hl
            {
                ret false
            }
            for Int32 i = 0, i <= hl - nl, i = i + 1
            {
                bool match = true
                for Int32 j = 0, j < nl, j = j + 1
                {
                    Int32 a = Uri.fold( SystemStringCharCodeAt( hay, i + j ) )
                    Int32 b = Uri.fold( SystemStringCharCodeAt( needle, j ) )
                    if a != b
                    {
                        match = false
                        break
                    }
                }
                if match
                {
                    ret true
                }
            }
            ret false
        }
    }
}
