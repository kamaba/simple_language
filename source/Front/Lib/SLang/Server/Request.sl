import Std;
import Core;

namespace SLang
{
    # ============================================================================
    # SLang/Server/Request.sl — 请求上下文（读侧 API，P1 全表）
    # 设计契约：SERVER_DESIGN.md §6.3 / §4.4
    #
    # P1 全表：方法/路径/原始查询/请求头/body 三视图（text/json/bytes/<T>）/
    #   bodyFormat 分流/form 族（urlencoded；multipart 字段随 P1.5）/
    #   路径参数族（路由 {param} 提取）/查询参数族（percent + '+'→空格解码）/
    #   对端地址/传值槽（userData）/身份槽（auth，P3 auth 插件写入）。
    # 实例由引擎构造（构造入参即全部字段，无公开无参构造语义）；
    # bodyBytes() 返回 slice 视图（不推进源索引），勿跨请求持有。
    # P1.5 增补：文件（file/files/fileCount，multipart）。
    # ============================================================================
    public class Request
    {
        string _method = ""
        string _path = ""
        string _rawQuery = ""
        Net.HttpHeaders _headers = null
        # body 缓冲（无 body 为 null；归属本请求，响应写出后引擎统一回收）
        ByteBuffer _body = null
        string _ip = ""
        Int32 _port = 0
        # 路由 {param} 提取结果（无参数路由为空 Map）
        Map<string,string> _pathParams = null
        # 查询参数惰性解析缓存
        Map<string,string> _queryParsed = null
        # form 字段惰性解析缓存（urlencoded）
        Map<string,string> _formParsed = null
        bool _formDone = false

        # ── 传值槽（中间件 → 处理器；引擎不触碰）──
        # 自由传值槽（任意语义）
        public object userData = null
        # 约定身份槽（auth 插件校验通过后写入身份、handler 只读使用；
        # 多为 SLang.JwtClaims；与 userData 分工见 §4.11，P3 auth 落地）
        public object auth = null

        # 引擎内部构造（用户勿直接 new）
        _init_( string method, string path, string rawQuery, Net.HttpHeaders headers, ByteBuffer body, string ip, Int32 port, Map<string,string> pathParams )
        {
            this._method = method
            this._path = path
            this._rawQuery = rawQuery
            this._headers = headers
            this._body = body
            this._ip = ip
            this._port = port
            this._pathParams = pathParams
        }

        # ── 请求行 ──

        # 请求方法（原始大写，如 "GET" / "POST"）
        public get string method()
        {
            ret this._method
        }

        # 路径（不含 query）
        public get string path()
        {
            ret this._path
        }

        # 原始 query 串（可为空串）
        public get string rawQuery()
        {
            ret this._rawQuery
        }

        # 对端地址 / 端口
        public get string ip()
        {
            ret this._ip
        }

        public get Int32 port()
        {
            ret this._port
        }

        # ── 请求头 ──

        # 原始头集合（大小写不敏感取值经 header(name)）
        public get Net.HttpHeaders headers()
        {
            ret this._headers
        }

        # 取请求头（大小写不敏感；不存在返回 null）
        public string header( string name )
        {
            ret this._headers.getHeader( name )
        }

        # 原始 Content-Type（含参数，如 boundary；无则空串）
        public get string contentType()
        {
            string v = this._headers.getHeader( "Content-Type" )
            if v == null
            {
                ret ""
            }
            ret v
        }

        # 请求声明长度（Content-Length；无头或格式非法为 0）
        public get Int64 contentLength()
        {
            string v = this._headers.getHeader( "Content-Length" )
            if v == null
            {
                ret 0
            }
            Int64 n = HttpServerCore._parseDec64( v )
            if n < 0
            {
                ret 0
            }
            ret n
        }

        # Upgrade 头探测（Upgrade: websocket 升级请求为 true）
        public get bool isUpgrade()
        {
            string v = this._headers.getHeader( "Upgrade" )
            if v == null
            {
                ret false
            }
            ret HttpServerCore._containsFold( v, "websocket" )
        }

        # ── body 三视图 ──

        # body 文本（UTF-8；无 body 空串；可重复调用，不消耗索引）
        public string bodyText()
        {
            if this._body == null
            {
                ret ""
            }
            ret this._body.toString()
        }

        # body 二进制视图（slice 视图；无 body 返回 null）
        public ByteBuffer bodyBytes()
        {
            if this._body == null
            {
                ret null
            }
            ret this._body.slice()
        }

        # body 作 JSON 树解析；空 body / 解析失败返回 null
        public BaseJson bodyJson()
        {
            string text = this.bodyText()
            if SystemStringLength( text ) == 0
            {
                ret null
            }
            BaseJson j = BaseJson.parse( text )
            if j.isEmpty
            {
                ret null
            }
            ret j
        }

        # body JSON → DTO（BaseJson.toData<T> 直通；失败/空 body 返回 null，不抛）
        public T body<T>()
        {
            BaseJson j = BaseJson.parse( this.bodyText() )
            ret j.toData<T>()
        }

        # body 格式判定（Content-Type 分流，§4.4 表）
        public get EBodyFormat bodyFormat()
        {
            Int32 bodyLen = 0
            if this._body != null
            {
                bodyLen = this._body.readableBytes
            }
            ret BodyParser.detect( this.contentType(), bodyLen )
        }

        # ── form 表单（application/x-www-form-urlencoded；§4.4）──
        # multipart 普通字段随 P1.5；当前 Multipart 格式下 form 族返回空。

        # form 字段值（已解码：percent + '+'→空格）；不存在返回 null
        public string form( string name )
        {
            if this._ensureForm() == false
            {
                ret null
            }
            if this._formParsed.containsKey( name ) == false
            {
                ret null
            }
            ret this._formParsed[name]
        }

        # form 字段 Int32 化；不存在/坏格式用 defVal
        public Int32 formInt( string name, Int32 defVal )
        {
            string v = this.form( name )
            if v == null
            {
                ret defVal
            }
            Int64 n = HttpServerCore._parseDec64( v )
            if n < 0
            {
                ret defVal
            }
            ret n.toInt32()
        }

        # form 字段布尔化（1/true/on 为真，其余假）；不存在用 defVal
        public bool formBool( string name, bool defVal )
        {
            string v = this.form( name )
            if v == null
            {
                ret defVal
            }
            if v == "1" || v == "true" || v == "on"
            {
                ret true
            }
            ret false
        }

        public bool hasForm( string name )
        {
            if this._ensureForm() == false
            {
                ret false
            }
            ret this._formParsed.containsKey( name )
        }

        # 全部字段名（无 form 为空表）。
        # 注意：struct 登记类的泛型返回方法体内，局部/形参 List 的 .add 链
        # 解析失败（Front Meta 层，「节点[add] 没有在类[Core.List<Core.String>]」；
        # 内部类不受影响）——拷贝循环收进内部类 _SrvUtil（queryKeys 同款）
        public List<string> formKeys()
        {
            List<string> outList = new()
            if this._ensureForm()
            {
                _SrvUtil.copyKeys( this._formParsed, outList )
            }
            ret outList
        }

        # 惰性解析 form（仅 urlencoded；成功置缓存并 ret true）
        bool _ensureForm()
        {
            if this._formDone
            {
                ret this._formParsed != null
            }
            this._formDone = true
            if this.bodyFormat != EBodyFormat.Form
            {
                ret false
            }
            this._formParsed = BodyParser.parseForm( this.bodyText() )
            ret true
        }

        # ── 路径参数（路由 {param} 提取；§4.2）──

        # 路径段参数；不存在返回 null
        public string pathParam( string name )
        {
            if this._pathParams == null
            {
                ret null
            }
            if this._pathParams.containsKey( name ) == false
            {
                ret null
            }
            ret this._pathParams[name]
        }

        # 路径参数 Int32 化；不存在/坏格式用 defVal（单参版 defVal=0）
        public Int32 pathInt( string name, Int32 defVal )
        {
            string v = this.pathParam( name )
            if v == null
            {
                ret defVal
            }
            Int64 n = HttpServerCore._parseDec64( v )
            if n < 0
            {
                ret defVal
            }
            ret n.toInt32()
        }

        public Int32 pathInt( string name )
        {
            ret this.pathInt( name, 0 )
        }

        # 路径参数 Int64 化；不存在/坏格式/溢出用 defVal
        public Int64 pathInt64( string name, Int64 defVal )
        {
            string v = this.pathParam( name )
            if v == null
            {
                ret defVal
            }
            Int64 n = HttpServerCore._parseDec64Full( v )
            if n < 0
            {
                ret defVal
            }
            ret n
        }

        # 路径参数 Float64 化；不存在/坏格式（非数字串）用 defVal
        public Float64 pathFloat( string name, Float64 defVal )
        {
            string v = this.pathParam( name )
            if v == null
            {
                ret defVal
            }
            if Request._isNumeric( v ) == false
            {
                ret defVal
            }
            ret SystemConvertFloat64( v )
        }

        # ── 查询参数（?a=1&b=x；percent + '+'→空格解码）──

        # 查询参数值；不存在返回 null
        public string query( string name )
        {
            if this._queryParsed == null
            {
                this._queryParsed = BodyParser.parseForm( this._rawQuery )
            }
            if this._queryParsed.containsKey( name ) == false
            {
                ret null
            }
            ret this._queryParsed[name]
        }

        # 查询参数 Int32 化；不存在/坏格式用 defVal
        public Int32 queryInt( string name, Int32 defVal )
        {
            string v = this.query( name )
            if v == null
            {
                ret defVal
            }
            Int64 n = HttpServerCore._parseDec64( v )
            if n < 0
            {
                ret defVal
            }
            ret n.toInt32()
        }

        # 查询参数布尔化（1/true/on 为真）；不存在用 defVal
        public bool queryBool( string name, bool defVal )
        {
            string v = this.query( name )
            if v == null
            {
                ret defVal
            }
            if v == "1" || v == "true" || v == "on"
            {
                ret true
            }
            ret false
        }

        public bool hasQuery( string name )
        {
            if this._queryParsed == null
            {
                this._queryParsed = BodyParser.parseForm( this._rawQuery )
            }
            ret this._queryParsed.containsKey( name )
        }

        # 全部查询参数名（无查询为空表；拷贝经 _SrvUtil——见 formKeys 注记）
        public List<string> queryKeys()
        {
            List<string> outList = new()
            if this._queryParsed == null
            {
                this._queryParsed = BodyParser.parseForm( this._rawQuery )
            }
            _SrvUtil.copyKeys( this._queryParsed, outList )
            ret outList
        }

        # 数字串校验（可选一元 '-' + 数字 + 至多一个 '.'；空串 false）
        static bool _isNumeric( string s )
        {
            Int32 len = SystemStringLength( s )
            if len == 0
            {
                ret false
            }
            Int32 start = 0
            if SystemStringCharCodeAt( s, 0 ) == 45
            {
                start = 1
                if len == 1
                {
                    ret false
                }
            }
            Int32 dotCount = 0
            for Int32 i = start, i < len, i = i + 1
            {
                Int32 c = SystemStringCharCodeAt( s, i )
                if c == 46
                {
                    dotCount = dotCount + 1
                    if dotCount > 1
                    {
                        ret false
                    }
                }
                elif c < 48 || c > 57
                {
                    ret false
                }
            }
            ret true
        }
    }
}
