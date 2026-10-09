namespace Net
{
    # ============================================================================
    # Net/Uri.sl — URI 解析（HTTP 客户端配套）
    # 设计契约：csimple_lang/md/design/NET_DESIGN.md L1118（Phase 3: TLS ->
    #   HttpClient / WebSocket / HttpServer）；R-8 namespace 统一 Std.Net
    #
    # 用法:
    #   import Std;
    #   var u = Net.Uri( "https://example.com:8443/a/b?q=1" )
    #   u.scheme        # "https"
    #   u.host          # "example.com"
    #   u.port          # 8443（未显式给出为 -1）
    #   u.effectivePort # 8443（缺省按 scheme 补 80/443）
    #   u.pathAndQuery  # "/a/b?q=1"
    #   u.hostHeader    # "example.com:8443"（非默认端口拼 host:port）
    #
    # 语义要点:
    #   - 构造即解析，非法输入抛 HttpError.UriFormat（label{}catch{} 捕获）
    #   - 全部字节级操作：SystemStringCharCodeAt/Range/Length 按 UTF-8 字节
    #     索引；scheme/host/port/path/query 均为 ASCII 安全区
    #   - scheme 原样保留，isHttp/isHttps 比较忽略大小写
    #   - Stage B 限制（详见 md/syntax/net.md 限制表）: 不支持 userinfo
    #     （user@host）、IPv6 字面量、percent-encoding、fragment
    # ============================================================================

    # HTTP 家族错误码（Uri 解析 / 协议处理共用）
    public enum HttpError extends Error
    {
        # 1 URL 格式非法（缺 scheme/host、端口非数字或超范围等）
        UriFormat = { code = 1 }
        # 2 scheme 不支持（非 http / https）
        UnsupportedScheme = { code = 2 }
        # 3 响应不符合 HTTP/1.1 报文格式（截断 / 非法状态行 / 非法块长度等）
        Protocol = { code = 3 }
        # 4 底层 IO 失败（连接 / 读写出错，Net 层异常原样透传）
        IoError = { code = 4 }
    }

    public class Uri
    {
        string _text = ""
        string _scheme = ""
        string _host = ""
        Int32 _port = -1
        string _path = ""
        string _query = ""

        # 构造即解析；非法 URL 抛 HttpError.UriFormat
        _init_( string text ) throws
        {
            if text == null
            {
                throw HttpError.UriFormat
            }
            Int32 len = SystemStringLength( text )
            if len == 0
            {
                throw HttpError.UriFormat
            }

            # ---- scheme：[A-Za-z][A-Za-z0-9+.-]* 直到 ':' ----
            if !Uri._isAlpha( SystemStringCharCodeAt( text, 0 ) )
            {
                throw HttpError.UriFormat
            }
            Int32 i = 1
            while i < len
            {
                Int32 c = SystemStringCharCodeAt( text, i )
                if c == 58
                {
                    break
                }
                if !Uri._isSchemeByte( c )
                {
                    throw HttpError.UriFormat
                }
                i = i + 1
            }
            if i >= len
            {
                throw HttpError.UriFormat
            }
            this._scheme = SystemStringRange( text, 0, i )

            # ---- "://" ----
            if i + 2 >= len
            {
                throw HttpError.UriFormat
            }
            if SystemStringCharCodeAt( text, i + 1 ) != 47 || SystemStringCharCodeAt( text, i + 2 ) != 47
            {
                throw HttpError.UriFormat
            }
            i = i + 3
            if i >= len
            {
                throw HttpError.UriFormat
            }

            # ---- authority：host[:port]，遇 '/' 或 '?' 结束 ----
            Int32 hostStart = i
            Int32 hostEnd = -1
            Int32 portStart = -1
            while i < len
            {
                Int32 c = SystemStringCharCodeAt( text, i )
                if c == 47 || c == 63
                {
                    break
                }
                # userinfo '@' 与 IPv6 '[]' 不支持（Stage B 限制）
                if c == 64 || c == 91 || c == 93
                {
                    throw HttpError.UriFormat
                }
                if c == 58
                {
                    if hostEnd >= 0
                    {
                        throw HttpError.UriFormat
                    }
                    hostEnd = i
                    portStart = i + 1
                }
                i = i + 1
            }
            if hostEnd < 0
            {
                hostEnd = i
            }
            this._host = SystemStringRange( text, hostStart, hostEnd )
            if SystemStringLength( this._host ) == 0
            {
                throw HttpError.UriFormat
            }

            # ---- 端口（可选）----
            if portStart >= 0
            {
                if portStart == i
                {
                    # "host:" 后无数字
                    throw HttpError.UriFormat
                }
                Int32 port = Uri._parsePort( text, portStart, i )
                if port < 0
                {
                    throw HttpError.UriFormat
                }
                this._port = port
            }

            # ---- path / query（path 空补 "/"；query 不含 '?'）----
            Int32 pathStart = i
            Int32 q = -1
            while i < len
            {
                if SystemStringCharCodeAt( text, i ) == 63
                {
                    q = i
                    break
                }
                i = i + 1
            }
            Int32 pathEnd = q < 0 ? len : q
            if pathEnd > pathStart
            {
                this._path = SystemStringRange( text, pathStart, pathEnd )
            }
            else
            {
                this._path = "/"
            }
            if q >= 0
            {
                this._query = SystemStringRange( text, q + 1, len )
            }
            this._text = text
        }

        # ── 解析结果 ──

        public get string scheme()
        {
            ret this._scheme
        }

        public get string host()
        {
            ret this._host
        }

        # 显式端口；URL 未给出为 -1
        public get Int32 port()
        {
            ret this._port
        }

        public get string path()
        {
            ret this._path
        }

        # query 原样（不含 '?'）
        public get string query()
        {
            ret this._query
        }

        override string toString()
        {
            ret this._text
        }

        public get bool isHttp()
        {
            ret Uri.eqFold( this._scheme, "http" )
        }

        public get bool isHttps()
        {
            ret Uri.eqFold( this._scheme, "https" )
        }

        public get bool isWs()
        {
            ret Uri.eqFold( this._scheme, "ws" )
        }

        public get bool isWss()
        {
            ret Uri.eqFold( this._scheme, "wss" )
        }

        # 端口未显式给出时按 scheme 补默认（http/ws=80 / https/wss=443）
        public get Int32 effectivePort()
        {
            if this._port >= 0
            {
                ret this._port
            }
            if this.isHttps || this.isWss
            {
                ret 443
            }
            ret 80
        }

        # Host 请求头：显式非默认端口时拼 host:port
        public get string hostHeader()
        {
            if this._port >= 0 && this._port != 80 && this._port != 443
            {
                ret this._host + ":" + this._port.toString()
            }
            ret this._host
        }

        # 请求行路径（path 空时为 "/"；query 非空时拼 "?query"）
        public get string pathAndQuery()
        {
            if SystemStringLength( this._query ) > 0
            {
                ret this._path + "?" + this._query
            }
            ret this._path
        }

        # ── 字符辅助（SystemString* 直调，Path/Png 同款先例）──

        static bool _isAlpha( Int32 c )
        {
            ret ( c >= 65 && c <= 90 ) || ( c >= 97 && c <= 122 )
        }

        static bool _isSchemeByte( Int32 c )
        {
            if Uri._isAlpha( c )
            {
                ret true
            }
            if c >= 48 && c <= 57
            {
                ret true
            }
            # '+' '-' '.'
            ret c == 43 || c == 45 || c == 46
        }

        # [start, end) 数字端口；非数字 / 超出 1-65535 返回 -1
        static Int32 _parsePort( string text, Int32 start, Int32 end )
        {
            Int32 v = 0
            for Int32 i = start, i < end, i = i + 1
            {
                Int32 c = SystemStringCharCodeAt( text, i )
                if c < 48 || c > 57
                {
                    ret -1
                }
                v = v * 10 + ( c - 48 )
                if v > 65535
                {
                    ret -1
                }
            }
            ret v
        }

        # 忽略大小写全串比较（ASCII 折叠）
        # 注意：跨类静态调用时实参不能用 this.字段[索引器] 复合表达式（会导致
        #   参数收集失败、方法匹配 NotFound），调用方需先提取局部变量——
        #   故与 Std 惯例的 _ 前缀不同、用公开名，便于 HttpHeaders 等复用
        public static bool eqFold( string a, string b )
        {
            Int32 al = SystemStringLength( a )
            Int32 bl = SystemStringLength( b )
            if al != bl
            {
                ret false
            }
            for Int32 i = 0, i < al, i = i + 1
            {
                if Uri.fold( SystemStringCharCodeAt( a, i ) ) != Uri.fold( SystemStringCharCodeAt( b, i ) )
                {
                    ret false
                }
            }
            ret true
        }

        # ASCII 小写折叠（'A'-'Z' -> 'a'-'z'）
        public static Int32 fold( Int32 c )
        {
            if c >= 65 && c <= 90
            {
                ret c + 32
            }
            ret c
        }
    }
}
