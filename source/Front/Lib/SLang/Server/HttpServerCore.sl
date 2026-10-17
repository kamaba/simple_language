import Std;
import Core;

namespace SLang
{
    # ============================================================================
    # SLang/Server/HttpServerCore.sl — HTTP/1.1 连接引擎（P0 → P1）
    # 设计契约：SERVER_DESIGN.md §6.1 / §6.2 / §6.8
    #
    # 范围（P1）：
    #   - _HttpParser：请求行 / 头（行数与总字节双上限）/ body（Content-Length
    #     精确读 | chunked 聚合 | 无则空）解析，预读缓冲跨 read 边界安全；
    #   - 响应编码：状态行 + Content-Type/Length/Connection + 用户头 + body；
    #   - keep-alive 循环（每连接一协程；Connection: close / HTTP/1.0 即断，
    #     残留预读自然衔接下一条请求）；
    #   - handler 协程派发：spawnClosure2 独立协程 + waitTimeout 超时守护
    #     （超时 cancel + 504 + 断连）+ awaitTask 等待（异常传播走 500 兜底）；
    #   - 路由：Router（{param} 段参数提取 / 404·405+Allow / ANY /
    #     HEAD 视同 GET / DuplicateRoute fail-fast）；
    #   - WS Upgrade 分流：Upgrade: websocket 命中 WS 路由 → 三参 accept
    #     （预读残留直传）→ WsSession 会话；未命中 / 握手失败 → 400。
    #
    # 随后阶段：P2 中间件管道 + 流式响应（stream/sse/sendFile）。
    #
    # 复用（不改动，G8 边界内）：Net.TcpServer / Net.TcpStream / Net.HttpHeaders /
    #   Net.WebSocketStream / Coroutine / ByteBuffer。语言纪律：get/set/next/
    #   data/as/is/out/return 为关键字不作方法名与标识符；Function 字段先拷
    #   局部再传参（HttpHeaders 头注记先例）；子类实参经局部赋型上行
    #   （HttpTest 头注记先例）。
    # ============================================================================

    # 内部：请求解析结果（errorCode 0 = OK；400 / 413 / 431）
    class _HttpParseResult
    {
        Int32 _errorCode = 0
        string _method = ""
        string _path = ""
        string _rawQuery = ""
        bool _http10 = false
        Net.HttpHeaders _headers = null
        ByteBuffer _body = null
    }

    public class HttpServerCore
    {
        # P0 形态：模块级单例（全静态）——SERVER_DESIGN.md §1.2「单 VM 协程
        # 调度即全部并发」的 v1 服务器语义。实测跨模块类多实例化在 C VM
        # 存在实例态串扰（S2 连接读到 S1 的路由表，闭包零捕获 + 实参传值
        # 亦无法规避），静态化是最小可靠路径；P1 门面（SLang.Server）落地
        # 时按单实例语义收口。
        static ServerConfig _cfg = null
        static Net.TcpServer _server = null
        static bool _running = false
        static Task _acceptTask = null
        # 跨模块点分类型不能作泛型参数（Front：模板中不允许 . 连接符号）——
        # 连接表用 List<object> + as Net.TcpStream 转型
        static List<object> _conns = null
        # 监听参数构造期提取为单层静态字段（this._cfg.host 双层链求值坑规避）
        static string _listenHost = ""
        static Int32 _listenPort = 0

        # 便捷覆盖监听端口（Server.start(port)/run(port) 用；start 前生效）
        public static void setListenPort( Int32 port )
        {
            _listenPort = port
            if _cfg != null
            {
                _cfg.port = port
            }
        }

        # 初始化配置（构造替代；重复调用覆盖配置，start 前生效）
        public static void configure( ServerConfig cfg )
        {
            _cfg = cfg
            _listenHost = cfg.host
            _listenPort = cfg.port
            if _conns == null
            {
                _conns = new()
            }
        }

        # 清空路由表（stop 后重新配置用；连接表由 stop 清理）
        public static void reset()
        {
            Router.clear()
        }

        # ── 路由注册（P1 起经 Router；推荐用 SLang.Server 门面——返回 RouteInfo 可写文档元数据）──

        # 原始形式（动态注册 / 方法变量）
        public static RouteInfo on( string method, string path, Function handler )
        {
            ret Router.add( method, path, handler )
        }

        public static RouteInfo onGet( string path, Function handler )
        {
            ret Router.add( "GET", path, handler )
        }

        public static RouteInfo onPost( string path, Function handler )
        {
            ret Router.add( "POST", path, handler )
        }

        public static RouteInfo onAny( string path, Function handler )
        {
            ret Router.add( "ANY", path, handler )
        }

        # WS 升级路由（SLang.Server.websocket 同源）
        public static RouteInfo websocket( string path, Function handler )
        {
            ret Router.addWs( path, handler )
        }

        # ── 生命周期 ──

        public static get bool isRunning()
        {
            ret _running
        }

        # 启动监听（立即返回；accept 循环进独立协程）
        public static void start() throws
        {
            if _running
            {
                throw ServerError.AlreadyRunning
            }

            if _listenPort <= 0 || _listenPort > 65535
            {
                throw ServerError.InvalidConfig
            }
            # 路由冲突检测（fail-fast，防静默覆盖；SERVER_DESIGN.md §6.2）
            string dup = Router.checkDuplicate()
            if dup != null
            {
                throw ServerError.DuplicateRoute
            }
            string host = _listenHost
            Int32 lport = _listenPort
            # 跨模块 string 字段默认值初始化不可靠（实测消费侧读出空串/
            # null，Int32 正常）——空值兜底 "0.0.0.0"（本模块内字符串常量
            # 可靠，HttpClient 大量先例）
            if host == null
            {
                host = "0.0.0.0"
            }
            if SystemStringLength( host ) == 0
            {
                host = "0.0.0.0"
            }
            _server = Net.Tcp.listenAddress( host, lport )
            _running = true
            # accept 循环协程：闭包零捕获（NetConcurrentTest.ef 先例），
            # 状态全走静态字段
            Net.TcpServer srv = _server
            function f = function( object a )
            {
                HttpServerCore._acceptLoop( a as Net.TcpServer )
            }
            _acceptTask = Coroutine.spawnClosure1( f, srv )
        }

        # 停止：关监听 + 关全部在途连接（各连接协程随读失败/EOF 退出）
        public static void stop() throws
        {
            if !_running
            {
                throw ServerError.NotRunning
            }
            _running = false
            if _server != null
            {
                _server.close()
                _server = null
            }
            Int32 n = _conns.length
            for Int32 i = 0, i < n, i = i + 1
            {
                Net.TcpStream c = _conns[i] as Net.TcpStream
                label closeOne
                {
                    c.close()
                }
                catch { }
            }
            _conns.clear()
        }

        # 挂起直至停止（等待 accept 循环协程收尾）
        public static void awaitStop() throws
        {
            if _acceptTask == null
            {
                throw ServerError.NotRunning
            }
            Task t = _acceptTask
            Coroutine.awaitTask( t )
        }

        # ── 内部：accept 循环（独立协程；每连接再派发一协程）──

        static void _acceptLoop( Net.TcpServer srv )
        {
            # label 只包单轮 accept（label 块内挂起 = HttpClient parseBlock 先例）
            bool run = true
            while run
            {
                label accOne
                {
                    Net.TcpStream client = srv.accept()
                    # 连接处理闭包零捕获（NetConcurrentTest.ef 先例）：
                    # client 经实参传值，状态全走静态字段
                    function f = function( object a )
                    {
                        HttpServerCore._runConn( a as Net.TcpStream )
                    }
                    Coroutine.spawnClosure1( f, client )
                }
                catch
                {
                    # 监听已关闭（stop）或 accept 失败：退出循环
                    run = false
                }
            }
        }

        # 连接收尾：从连接表移除 + 尽力关闭（幂等）
        static void _cleanupConn( Net.TcpStream c )
        {
            Int32 n = _conns.length
            for Int32 i = 0, i < n, i = i + 1
            {
                if _conns[i] == c
                {
                    _conns.removeRange( i, 1 )
                    break
                }
            }
            label closeConn
            {
                c.close()
            }
            catch { }
        }

        # 连接处理入口（连接协程体）：异常兜底静默（对端异常断开 / 写失败等）
        static void _runConn( Net.TcpStream c )
        {
            _conns.add( c )
            label connBlock
            {
                HttpServerCore._handleConnection( c )
            }
            catch
            {
            }
            HttpServerCore._cleanupConn( c )
        }

        # ── 内部：连接处理（每连接一协程；keep-alive 循环）──

        static void _handleConnection( Net.TcpStream client )
        {
            ServerConfig cfg = _cfg
            client.setReadTimeout( cfg.keepAliveTimeoutMs )
            ByteBuffer buf = ByteBuffer()
            while true
            {
                # ---- 1) 解析请求（EOF / 空闲超时 / IO 异常 → 静默收连接）----
                _HttpParseResult r = null
                bool failed = false
                label parseBlock
                {
                    Net.NetStream ns = client
                    r = HttpServerCore._parse( ns, buf, cfg )
                }
                catch
                {
                    failed = true
                }
                if failed
                {
                    break
                }
                if r == null
                {
                    break
                }
                if r._errorCode != 0
                {
                    # 协议层错误：应答后优雅断连（431/413/400）。
                    # 直接 close 在未读完入站 body 时会触发 RST，客户端读不到
                    # 响应——先停发（FIN）→ 短暂让出（客户端收响应）→ 丢弃式
                    # 收尾读 → close（_gracefulClose）
                    HttpServerCore._writeSimple( client, r._errorCode, HttpServerCore._errorBody( r._errorCode ) )
                    if r._body != null
                    {
                        r._body.release()
                    }
                    HttpServerCore._gracefulClose( client )
                    break
                }

                # ---- 2) WS Upgrade 分流（§6.1：升级在路由/中间件管道之前）----
                # Upgrade: websocket + Connection 含 upgrade 且命中 WS 路由 →
                # 三参 accept（buf 预读残留直传，残留帧不丢失）→ 会话协程；
                # 未命中 WS 路由 → 400；握手失败 → 400。该连接退出 keep-alive 循环
                if HttpServerCore._isWsUpgrade( r )
                {
                    RouteInfo wsRoute = Router.findWs( r._path )
                    if wsRoute == null
                    {
                        HttpServerCore._writeSimple( client, 400, HttpServerCore._errorBody( 400 ) )
                        if r._body != null
                        {
                            r._body.release()
                        }
                        HttpServerCore._gracefulClose( client )
                        break
                    }
                    HttpServerCore._runWsSession( client, wsRoute, r, buf )
                    # 会话结束 = 连接终结
                    break
                }

                # ---- 3) 路由匹配（HEAD 视同 GET；§6.2）----
                bool isHead = Net.Uri.eqFold( r._method, "HEAD" )
                string lookupMethod = r._method
                if isHead
                {
                    lookupMethod = "GET"
                }
                bool keepAlive = HttpServerCore._wantKeepAlive( r )
                RouteInfo route = Router.match( r._method, r._path )
                if route == null
                {
                    # 404 / 405 判定：路径有无注册（findBest 忽略动词）
                    RouteInfo pathHit = Router.findBest( r._path )
                    if pathHit == null
                    {
                        # 404：正常响应（按 keep-alive 语义继续连接）
                        Response r404 = Response()
                        r404.status( 404 ).rawJson( HttpServerCore._errorBody( 404 ) )
                        HttpServerCore._writeResponse( client, r404, keepAlive, isHead )
                        if r._body != null
                        {
                            r._body.release()
                        }
                        continue
                    }
                    # 路径命中但动词不符：405 + Allow（正常响应，连接可继续）
                    string allow = Router.allowMethods( r._path )
                    Response r405 = Response()
                    r405.status( 405 ).header( "Allow", allow ).rawJson( HttpServerCore._errorBody( 405 ) )
                    HttpServerCore._writeResponse( client, r405, keepAlive, isHead )
                    if r._body != null
                    {
                        r._body.release()
                    }
                    continue
                }

                # ---- 4) 构造上下文 + handler 协程派发（SERVER_DESIGN.md §6.1）----
                Map<string,string> pathParams = Router.extractParams( route, r._path )
                Request req = Request( r._method, r._path, r._rawQuery, r._headers, r._body, client.remoteAddress, client.remotePort, pathParams )
                r._headers = null
                r._body = null
                Response res = Response()

                # Function 字段先拷局部再传参（HttpHeaders 头注记纪律）
                function h = route.handler
                Task task = Coroutine.spawnClosure2( h, req, res )

                bool timedOut = false
                if cfg.handlerTimeoutMs > 0
                {
                    timedOut = !Coroutine.waitTimeout( task, cfg.handlerTimeoutMs )
                }
                if timedOut
                {
                    # 超时：协作式取消 + 优雅断连（杜绝未死协程的后续读写竞态；
                    # 直接 close 会 RST 致客户端读不到 504 —— _gracefulClose）
                    task.cancel()
                    if res.isResponded
                    {
                        # 已缓冲完成的响应照常写出（非流式单缓冲无半截风险）
                        HttpServerCore._writeResponse( client, res, false, isHead )
                    }
                    else
                    {
                        HttpServerCore._writeSimple( client, 504, HttpServerCore._errorBody( 504 ) )
                    }
                    if req._body != null
                    {
                        req._body.release()
                    }
                    if res._body != null
                    {
                        res._body.release()
                    }
                    HttpServerCore._gracefulClose( client )
                    break
                }

                bool handlerError = false
                label awaitBlock
                {
                    Coroutine.awaitTask( task )
                }
                catch
                {
                    handlerError = true
                }

                # ---- 4) 响应写出 ----
                bool closeConn = false
                if handlerError
                {
                    if res.isResponded
                    {
                        # 响应已缓冲完成：照常写出（首个响应胜出）
                        HttpServerCore._writeResponse( client, res, keepAlive, isHead )
                    }
                    else
                    {
                        # handler 异常未响应：500 + 断连（连接状态不可信）
                        HttpServerCore._writeSimple( client, 500, HttpServerCore._errorBody( 500 ) )
                        closeConn = true
                    }
                }
                elif !res.isResponded
                {
                    # handler 正常返回但未写任何响应（契约违约）：500 + 断连
                    HttpServerCore._writeSimple( client, 500, HttpServerCore._errorBody( 500 ) )
                    closeConn = true
                }
                else
                {
                    HttpServerCore._writeResponse( client, res, keepAlive, isHead )
                }

                if req._body != null
                {
                    req._body.release()
                }
                if res._body != null
                {
                    res._body.release()
                }

                if closeConn || !keepAlive
                {
                    if closeConn
                    {
                        # 500 断连路径同走优雅收尾（防 RST）
                        HttpServerCore._gracefulClose( client )
                    }
                    break
                }
            }
            label bufRelease
            {
                buf.release()
            }
            catch { }
        }

        # 优雅断连：停发（closeWrite = shutdown Write，NetCloseTest F1 先例）
        # → 让出 50ms（客户端收响应）→ 丢弃式收尾读（消费对端残余 body，
        # 防未读缓冲触发 RST）→ close。协程内 delay 挂起不阻塞调度；
        # 收尾读窗口 500ms，超时静默放弃
        static void _gracefulClose( Net.TcpStream client )
        {
            label shut
            {
                client.closeWrite()
            }
            catch { }
            Coroutine.delay( 50 )
            client.setReadTimeout( 500 )
            ByteBuffer drain = ByteBuffer( 4096 )
            while true
            {
                Int32 n = 0
                label drainRead
                {
                    n = client.read( drain )
                }
                catch
                {
                    break
                }
                if n <= 0
                {
                    break
                }
                drain.clear()
            }
            drain.release()
            client.setReadTimeout( 0 )
        }

        # ── WebSocket Upgrade 分流（§6.1 / §6.8）──

        # 升级请求判定：Upgrade 头含 websocket（大小写不敏感）
        static bool _isWsUpgrade( _HttpParseResult r )
        {
            string up = r._headers.getHeader( "Upgrade" )
            if up == null
            {
                ret false
            }
            ret HttpServerCore._containsFold( up, "websocket" )
        }

        # WS 会话：三参 accept（预读残留直传，残留帧不丢失）→ WsSession →
        # 会话回调（单参 Function(session)）→ 收尾注销注册表。
        # 握手失败 → 400；本方法返回 = 连接终结（由调用方 break）
        static void _runWsSession( Net.TcpStream client, RouteInfo wsRoute, _HttpParseResult r, ByteBuffer buf )
        {
            Net.WebSocketStream ws = null
            label hsBlock
            {
                # HTTP 解析已消费请求行+头（readerIndex 前进，原始字节不再
                # 可读）——accept 的握手重读必须喂一份重建的最小请求。用已
                # 解析值重组：请求行 + 四个 WS 关键头 + 空行，灌入 preRead
                # 缓冲（_acceptHandshake 从预读区取行，与 socket 残留同路）。
                string hs = "GET " + r._path
                if SystemStringLength( r._rawQuery ) > 0
                {
                    hs = hs + "?" + r._rawQuery
                }
                hs = hs + " HTTP/1.1\r\n"
                string wk = r._headers.getHeader( "Sec-WebSocket-Key" )
                string wv = r._headers.getHeader( "Sec-WebSocket-Version" )
                if wk == null || wv == null
                {
                    throw ServerError.BadRequest
                }
                hs = hs + "Upgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Version: " + wv + "\r\nSec-WebSocket-Key: " + wk + "\r\n\r\n"
                ByteBuffer pre = ByteBuffer.fromString( hs )
                ws = Net.WebSocketStream.accept( client, pre, null )
                pre.release()
            }
            catch
            {
                # 握手失败（非升级请求形态 / 头缺失）：400 后断连
                HttpServerCore._writeSimple( client, 400, HttpServerCore._errorBody( 400 ) )
                HttpServerCore._gracefulClose( client )
                ret
            }
            Map<string,string> pathParams = Router.extractParams( wsRoute, r._path )
            WsSession s = WsSession( ws, wsRoute.pattern, pathParams, r._rawQuery )
            _WsSessions.register( wsRoute.pattern, s )
            # Function 字段先拷局部（HttpHeaders 头注记纪律）
            function h = wsRoute.handler
            label sessBlock
            {
                # 会话回调（单参；handler 内循环 s.receive()，null = 关闭）
                h( s )
            }
            catch
            {
                # 会话异常：静默收尾（不影响其他会话，§6.11）
            }
            _WsSessions.unregister( wsRoute.pattern, s )
            ws.close()
        }

        # keep-alive 判定：Connection 头显式优先，否则 HTTP/1.1 默认开
        static bool _wantKeepAlive( _HttpParseResult r )
        {
            string conn = r._headers.getHeader( "Connection" )
            if conn != null
            {
                if HttpServerCore._containsFold( conn, "close" )
                {
                    ret false
                }
                if HttpServerCore._containsFold( conn, "keep-alive" )
                {
                    ret true
                }
            }
            ret r._http10 == false
        }

        # ── 响应编码 ──

        # 响应写出（Content-Length 恒精确；HEAD 不写 body；写毕由调用方回收 body）
        static void _writeResponse( Net.TcpStream client, Response res, bool keepAlive, bool isHead ) throws
        {
            Int32 status = res._status
            string head = "HTTP/1.1 " + status.toString() + " " + HttpServerCore._reason( status ) + "\r\n"
            head = head + "Content-Type: " + res._contentType + "\r\n"
            Int32 bodyLen = 0
            if res._body != null
            {
                bodyLen = res._body.readableBytes
            }
            head = head + "Content-Length: " + bodyLen.toString() + "\r\n"
            if keepAlive
            {
                head = head + "Connection: keep-alive\r\n"
            }
            else
            {
                head = head + "Connection: close\r\n"
            }
            Int32 n = res._headers.count
            for Int32 i = 0, i < n, i = i + 1
            {
                string hn = res._headers.nameAt( i )
                # 引擎管理头跳过（Content-Type/Length/Connection 由上方生成；
                # 自定义 Content-Type 请走 Response.contentType）
                if Net.Uri.eqFold( hn, "Content-Type" ) || Net.Uri.eqFold( hn, "Content-Length" ) || Net.Uri.eqFold( hn, "Connection" )
                {
                    continue
                }
                head = head + hn + ": " + res._headers.valueAt( i ) + "\r\n"
            }
            head = head + "\r\n"
            client.write( ByteBuffer.fromString( head ) )
            if !isHead && bodyLen > 0
            {
                client.write( res._body )
            }
        }

        # 简单 JSON 错误响应（尽力写出，失败静默；恒 Connection: close）
        static void _writeSimple( Net.TcpStream client, Int32 status, string jsonBody )
        {
            HttpServerCore._writeSimple( client, status, jsonBody, "", "" )
        }

        # 带单附加头变体（如 405 的 Allow）
        static void _writeSimple( Net.TcpStream client, Int32 status, string jsonBody, string extraName, string extraValue )
        {
            label wBlock
            {
                string head = "HTTP/1.1 " + status.toString() + " " + HttpServerCore._reason( status ) + "\r\n"
                head = head + "Content-Type: application/json; charset=utf-8\r\n"
                Int32 bodyLen = SystemStringLength( jsonBody )
                head = head + "Content-Length: " + bodyLen.toString() + "\r\n"
                head = head + "Connection: close\r\n"
                if SystemStringLength( extraName ) > 0
                {
                    head = head + extraName + ": " + extraValue + "\r\n"
                }
                head = head + "\r\n"
                client.write( ByteBuffer.fromString( head + jsonBody ) )
            }
            catch { }
        }

        # ApiResult 形态错误体（键序 code → message → data；data 是关键字，
        # 框架手工组包，SERVER_DESIGN.md §6.7；ApiResult 类随 P1 落地）
        static string _errorBody( Int32 code )
        {
            string msg = "bad request"
            if code == 404
            {
                msg = "not found"
            }
            elif code == 405
            {
                msg = "method not allowed"
            }
            elif code == 413
            {
                msg = "payload too large"
            }
            elif code == 431
            {
                msg = "request header fields too large"
            }
            elif code == 500
            {
                msg = "internal error"
            }
            elif code == 504
            {
                msg = "handler timeout"
            }
            ret "{\"code\":" + code.toString() + ",\"message\":\"" + msg + "\",\"data\":null}"
        }

        # 状态码 reason（SERVER_DESIGN.md 附录 A 常用子集；未知码空串）
        static string _reason( Int32 code )
        {
            if code == 200
            {
                ret "OK"
            }
            if code == 201
            {
                ret "Created"
            }
            if code == 204
            {
                ret "No Content"
            }
            if code == 301
            {
                ret "Moved Permanently"
            }
            if code == 302
            {
                ret "Found"
            }
            if code == 304
            {
                ret "Not Modified"
            }
            if code == 400
            {
                ret "Bad Request"
            }
            if code == 401
            {
                ret "Unauthorized"
            }
            if code == 403
            {
                ret "Forbidden"
            }
            if code == 404
            {
                ret "Not Found"
            }
            if code == 405
            {
                ret "Method Not Allowed"
            }
            if code == 409
            {
                ret "Conflict"
            }
            if code == 413
            {
                ret "Payload Too Large"
            }
            if code == 415
            {
                ret "Unsupported Media Type"
            }
            if code == 431
            {
                ret "Request Header Fields Too Large"
            }
            if code == 500
            {
                ret "Internal Server Error"
            }
            if code == 501
            {
                ret "Not Implemented"
            }
            if code == 503
            {
                ret "Service Unavailable"
            }
            if code == 504
            {
                ret "Gateway Timeout"
            }
            ret ""
        }

        # ── 请求解析（预读缓冲模式；HttpClient 响应解析的对称实现）──

        # 解析一条完整请求；对端干净关闭（EOF）返回 null；协议错误经
        # _errorCode 带出；读超时 / IO 异常原样上抛（连接层收尾）
        static _HttpParseResult _parse( Net.NetStream s, ByteBuffer buf, ServerConfig cfg ) throws
        {
            _HttpParseResult result = _HttpParseResult()

            # ---- 请求行：METHOD SP target SP version ----
            string line = HttpServerCore._readLine( s, buf )
            if line == null
            {
                ret null
            }
            line = HttpServerCore._stripEol( line )
            Int32 llen = SystemStringLength( line )
            if llen == 0
            {
                result._errorCode = 400
                ret result
            }
            Int32 sp1 = -1
            for Int32 i = 0, i < llen, i = i + 1
            {
                if SystemStringCharCodeAt( line, i ) == 32
                {
                    sp1 = i
                    break
                }
            }
            if sp1 <= 0
            {
                result._errorCode = 400
                ret result
            }
            Int32 sp2 = -1
            for Int32 i = sp1 + 1, i < llen, i = i + 1
            {
                if SystemStringCharCodeAt( line, i ) == 32
                {
                    sp2 = i
                    break
                }
            }
            if sp2 < 0 || sp2 == sp1 + 1
            {
                result._errorCode = 400
                ret result
            }
            result._method = SystemStringRange( line, 0, sp1 )
            string target = SystemStringRange( line, sp1 + 1, sp2 )
            string version = SystemStringRange( line, sp2 + 1, llen )
            if !HttpServerCore._startsWithFold( version, "HTTP/" )
            {
                result._errorCode = 400
                ret result
            }
            result._http10 = HttpServerCore._eqExact( version, "HTTP/1.0" )

            # ---- target 拆 path ? query ----
            Int32 tlen = SystemStringLength( target )
            if tlen == 0 || SystemStringCharCodeAt( target, 0 ) != 47
            {
                result._errorCode = 400
                ret result
            }
            Int32 qpos = -1
            for Int32 i = 0, i < tlen, i = i + 1
            {
                if SystemStringCharCodeAt( target, i ) == 63
                {
                    qpos = i
                    break
                }
            }
            if qpos >= 0
            {
                result._path = SystemStringRange( target, 0, qpos )
                result._rawQuery = SystemStringRange( target, qpos + 1, tlen )
            }
            else
            {
                result._path = target
                result._rawQuery = ""
            }

            # ---- 头循环（空行截止；行数 / 总字节双上限）----
            Net.HttpHeaders headers = Net.HttpHeaders()
            result._headers = headers
            Int32 headerBytes = llen
            Int32 headerCount = 0
            while true
            {
                string h = HttpServerCore._readLine( s, buf )
                if h == null
                {
                    result._errorCode = 400
                    ret result
                }
                h = HttpServerCore._stripEol( h )
                Int32 hl = SystemStringLength( h )
                if hl == 0
                {
                    break
                }
                headerCount = headerCount + 1
                headerBytes = headerBytes + hl
                if headerCount > 100 || headerBytes > cfg.maxHeaderBytes
                {
                    result._errorCode = 431
                    ret result
                }
                Int32 ci = -1
                for Int32 k = 0, k < hl, k = k + 1
                {
                    if SystemStringCharCodeAt( h, k ) == 58
                    {
                        ci = k
                        break
                    }
                }
                if ci <= 0
                {
                    result._errorCode = 400
                    ret result
                }
                Int32 vs = ci + 1
                while vs < hl && SystemStringCharCodeAt( h, vs ) == 32
                {
                    vs = vs + 1
                }
                headers.add( SystemStringRange( h, 0, ci ), SystemStringRange( h, vs, hl ) )
            }

            # ---- body 三态（chunked / Content-Length / 无）----
            string te = headers.getHeader( "Transfer-Encoding" )
            bool chunked = false
            if te != null
            {
                chunked = HttpServerCore._containsFold( te, "chunked" )
            }
            if chunked
            {
                ByteBuffer body = ByteBuffer()
                Int32 ec = HttpServerCore._readChunked( s, buf, body, cfg.maxBodyBytes )
                if ec != 0
                {
                    body.release()
                    result._errorCode = ec
                    ret result
                }
                result._body = body
            }
            else
            {
                string cls = headers.getHeader( "Content-Length" )
                if cls != null
                {
                    Int64 cl = HttpServerCore._parseDec64( cls )
                    if cl < 0 || cl > 2000000000
                    {
                        result._errorCode = 400
                        ret result
                    }
                    if cl > cfg.maxBodyBytes
                    {
                        result._errorCode = 413
                        ret result
                    }
                    if cl > 0
                    {
                        Int32 need = cl.toInt32()
                        ByteBuffer body = ByteBuffer()
                        body.ensureWritable( need )
                        Int32 ec = HttpServerCore._readBytesChecked( s, buf, body, need )
                        if ec != 0
                        {
                            body.release()
                            result._errorCode = ec
                            ret result
                        }
                        result._body = body
                    }
                }
                # 无 CL 无 chunked：body 为空（keep-alive 下不可读 EOF）
            }
            ret result
        }

        # chunked 请求 body 聚合（hex size 行 -> 块 -> CRLF；0 终止 + trailer 空行）
        # 返回 0 = OK；400 = 协议错误 / EOF 截断；413 = 累计超限
        static Int32 _readChunked( Net.NetStream s, ByteBuffer buf, ByteBuffer dst, Int32 maxBytes ) throws
        {
            Int32 total = 0
            while true
            {
                string line = HttpServerCore._readLine( s, buf )
                if line == null
                {
                    ret 400
                }
                line = HttpServerCore._stripEol( line )
                Int32 size = HttpServerCore._chunkSize( line )
                if size < 0
                {
                    ret 400
                }
                if size == 0
                {
                    # trailer 头直到空行
                    while true
                    {
                        string t = HttpServerCore._readLine( s, buf )
                        if t == null
                        {
                            ret 400
                        }
                        if SystemStringLength( HttpServerCore._stripEol( t ) ) == 0
                        {
                            break
                        }
                    }
                    ret 0
                }
                total = total + size
                if total > maxBytes
                {
                    ret 413
                }
                dst.ensureWritable( size )
                Int32 ec = HttpServerCore._readBytesChecked( s, buf, dst, size )
                if ec != 0
                {
                    ret ec
                }
                # 块尾 CRLF
                ByteBuffer crlf = ByteBuffer()
                Int32 ec2 = HttpServerCore._readBytesChecked( s, buf, crlf, 2 )
                crlf.release()
                if ec2 != 0
                {
                    ret ec2
                }
            }
            ret 0
        }

        # 精确读 need 字节入 dst（先消费 buf 预读，不足再从流拉）；
        # EOF 截断返回 400（读超时 / IO 异常原样上抛）
        static Int32 _readBytesChecked( Net.NetStream s, ByteBuffer buf, ByteBuffer dst, Int32 need ) throws
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
                        ret 400
                    }
                }
            }
            ret 0
        }

        # ── 文本 / 解析辅助（HttpClient 同款；Std 侧为模块内私有不便跨模块复用）──

        # 读一行（含 '\n'）：buf 有行直接取，否则从流拉；EOF 无行返回 null。
        # 单行上限 64KB（防无换行恶意行撑爆内存；头区另有 maxHeaderBytes 判定）
        static string _readLine( Net.NetStream s, ByteBuffer buf ) throws
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
                if buf.readableBytes > 65536
                {
                    throw Net.HttpError.Protocol
                }
                line = buf.readLine()
            }
            ret line
        }

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
            ret HttpServerCore._parseHex( SystemStringRange( line, 0, hexEnd ) )
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

        # 忽略大小写子串查找
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
                    Int32 a = Net.Uri.fold( SystemStringCharCodeAt( hay, i + j ) )
                    Int32 b = Net.Uri.fold( SystemStringCharCodeAt( needle, j ) )
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

        # 忽略大小写前缀匹配
        static bool _startsWithFold( string s, string prefix )
        {
            Int32 sl = SystemStringLength( s )
            Int32 pl = SystemStringLength( prefix )
            if pl == 0
            {
                ret true
            }
            if sl < pl
            {
                ret false
            }
            for Int32 i = 0, i < pl, i = i + 1
            {
                if Net.Uri.fold( SystemStringCharCodeAt( s, i ) ) != Net.Uri.fold( SystemStringCharCodeAt( prefix, i ) )
                {
                    ret false
                }
            }
            ret true
        }

        # 精确比对（不折叠大小写）
        static bool _eqExact( string a, string b )
        {
            Int32 la = SystemStringLength( a )
            Int32 lb = SystemStringLength( b )
            if la != lb
            {
                ret false
            }
            for Int32 i = 0, i < la, i = i + 1
            {
                if SystemStringCharCodeAt( a, i ) != SystemStringCharCodeAt( b, i )
                {
                    ret false
                }
            }
            ret true
        }
    }
}
