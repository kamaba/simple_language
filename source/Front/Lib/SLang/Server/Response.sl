import Std;
import Core;

namespace SLang
{
    # ============================================================================
    # SLang/Server/Response.sl — 响应输出（写侧 API，P1 全表）
    # 设计契约：SERVER_DESIGN.md §4.3 / §6.3
    #
    # 语义：终端方法把响应暂存到本对象，handler 协程结束后由引擎统一编码
    #   写出（普通响应恒精确 Content-Length）；终端方法第二次调用抛
    #   ServerError.AlreadyResponded（开发期防重复写出）。
    # 链式：status / header / contentType 返回 this（须在终端方法之前调用）。
    # 注意：Content-Type / Content-Length / Connection 三头由引擎管理，
    #   自定义 Content-Type 走 contentType(mime)，经 header() 设置会被跳过。
    # P2 增补：stream()/sse()/sendFile() 流式终端 + file()。
    # ============================================================================
    public class Response
    {
        Int32 _status = 200
        string _contentType = "text/plain; charset=utf-8"
        Net.HttpHeaders _headers = null
        ByteBuffer _body = null
        bool _responded = false

        override _init_()
        {
            this._headers = Net.HttpHeaders()
        }

        # 是否已有终端写出（handler 忘记响应时引擎兜底 500）
        public get bool isResponded()
        {
            ret this._responded
        }

        # ── 链式（终端方法之前调用）──

        # 置状态码
        public Response status( Int32 code )
        {
            this._status = code
            ret this
        }

        # 追加响应头（覆盖同名，大小写不敏感）
        public Response header( string name, string value )
        {
            this._headers.setHeader( name, value )
            ret this
        }

        # 覆盖 Content-Type
        public Response contentType( string mime )
        {
            this._contentType = mime
            ret this
        }

        # ── 终端方法（二次调用抛 ServerError.AlreadyResponded）──

        # 文本响应（text/plain）
        public void text( string s )
        {
            this._terminal()
            this._contentType = "text/plain; charset=utf-8"
            this._body = ByteBuffer.fromString( s )
        }

        # HTML 响应（text/html）
        public void html( string s )
        {
            this._terminal()
            this._contentType = "text/html; charset=utf-8"
            this._body = ByteBuffer.fromString( s )
        }

        # 任意对象 → JSON（BaseJson.fromData；null → "null"）
        public void json( object obj )
        {
            this._terminal()
            this._contentType = "application/json; charset=utf-8"
            string text = "null"
            if obj != null
            {
                BaseJson j = BaseJson.fromData( obj )
                string s = j.toJson()
                if SystemStringLength( s ) > 0
                {
                    text = s
                }
            }
            this._body = ByteBuffer.fromString( text )
        }

        # 预构建 JSON 串直写（application/json；openapi.json 等内部使用）
        public void rawJson( string jsonText )
        {
            this._terminal()
            this._contentType = "application/json; charset=utf-8"
            this._body = ByteBuffer.fromString( jsonText )
        }

        # 统一成功封装 = json( ApiResult.ok( payload ) )
        #（输出 {"code":0,"message":"ok","data":...}；payload null → data:null）
        public void jsonOk( object payload )
        {
            this._terminal()
            this._contentType = "application/json; charset=utf-8"
            this._body = ByteBuffer.fromString( ApiResult.ok( payload ).toJson() )
        }

        # 统一失败封装（HTTP 仍 200：业务码与 HTTP 码分离）
        public void jsonFail( Int32 code, string message )
        {
            this._terminal()
            this._contentType = "application/json; charset=utf-8"
            this._body = ByteBuffer.fromString( ApiResult.fail( code, message ).toJson() )
        }

        # 三参重载：同时改 HTTP 状态码
        public void jsonFail( Int32 code, string message, Int32 httpStatus )
        {
            this._terminal()
            this._status = httpStatus
            this._contentType = "application/json; charset=utf-8"
            this._body = ByteBuffer.fromString( ApiResult.fail( code, message ).toJson() )
        }

        # 二进制响应（application/octet-stream；缓冲归属本响应，写出后引擎回收）
        public void bytes( ByteBuffer buf )
        {
            this._terminal()
            this._contentType = "application/octet-stream"
            this._body = buf
        }

        # 重定向（302 + Location；空 body）
        public void redirect( string url )
        {
            this._terminal()
            this._status = 302
            this._contentType = "text/plain; charset=utf-8"
            this._headers.setHeader( "Location", url )
            this._body = null
        }

        # 终端守卫
        void _terminal() throws
        {
            if this._responded
            {
                throw ServerError.AlreadyResponded
            }
            this._responded = true
        }
    }
}
