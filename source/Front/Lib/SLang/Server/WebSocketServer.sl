import Std;
import Core;

namespace SLang
{
    # ============================================================================
    # SLang/Server/WebSocketServer.sl — WsSession / 会话注册表 / 独立端口 WS 服务（P1）
    # 设计契约：SERVER_DESIGN.md §4.6 / §6.8
    #
    # WsSession：包装 Net.WebSocketStream + 会话标识 + 路由信息（路径参数 /
    #   查询参数与 HTTP 路由同一套 {param} 语法）；模块内构造（引擎升级分流 /
    #   独立服务 accept 后创建），跨模块字段/方法消费（HttpResponse 安全模式）。
    # 注册表：按 pattern 分组（app.sessions/broadcast 的底座）。
    # 独立形态：SLang.WebSocketServer 静态门面（跨模块多实例化隔离不可靠，
    #   [2026-10-17] 复盘 ①；v1 单 VM 单服务，多实例场景 C VM 修复前一律静态）。
    # ============================================================================

    # ── 会话注册表（内部静态；pattern → List<WsSession>）──
    class _WsSessions
    {
        static Map<string, object> _byPattern = null

        static void _ensure()
        {
            if _byPattern == null
            {
                _byPattern = new()
            }
        }

        static void register( string pattern, WsSession s )
        {
            _WsSessions._ensure()
            if pattern == null
            {
                pattern = ""
            }
            List<WsSession> list = null
            object cur = _byPattern[pattern]
            if cur == null
            {
                list = new()
                _byPattern[pattern] = list
            }
            else
            {
                list = cur as List<WsSession>
            }
            list.add( s )
        }

        static void unregister( string pattern, WsSession s )
        {
            if _byPattern == null
            {
                ret
            }
            if pattern == null
            {
                pattern = ""
            }
            object cur = _byPattern[pattern]
            if cur == null
            {
                ret
            }
            List<WsSession> list = cur as List<WsSession>
            Int32 n = list.length
            for Int32 i = 0, i < n, i = i + 1
            {
                if list[i] == s
                {
                    list.removeRange( i, 1 )
                    ret
                }
            }
        }

        # 在线会话快照（该 pattern 全部会话）。
        # 注意：struct 登记类的方法体内 List.add 链解析失败（Front Meta 层，
        # _SrvUtil 头注记同款）——拷贝循环收进本内部类（sessions 是内部类方法）
        static List<WsSession> sessions( string pattern )
        {
            List<WsSession> outList = new()
            if _byPattern == null
            {
                ret outList
            }
            if pattern == null
            {
                pattern = ""
            }
            object cur = _byPattern[pattern]
            if cur == null
            {
                ret outList
            }
            List<WsSession> list = cur as List<WsSession>
            _WsSessions._copySessions( list, outList )
            ret outList
        }

        # 会话拷贝（内部类方法：List.add 在内部类内可解析）
        static void _copySessions( List<WsSession> src, List<WsSession> dst )
        {
            Int32 n = src.length
            for Int32 i = 0, i < n, i = i + 1
            {
                dst.add( src[i] )
            }
        }

        # 组播文本（已断会话 try? 吞错跳过）
        static Int32 broadcast( string pattern, string text )
        {
            Int32 sent = 0
            List<WsSession> list = _WsSessions.sessions( pattern )
            Int32 n = list.length
            for Int32 i = 0, i < n, i = i + 1
            {
                WsSession s = list[i]
                label sendOne
                {
                    s.sendText( text )
                    sent = sent + 1
                }
                catch { }
            }
            ret sent
        }
    }

    # ============================================================================
    # WsSession — WebSocket 会话（模块内构造；用户侧只消费）
    # ============================================================================
    public class WsSession
    {
        # 底层连接（握手完成后移交）
        Net.WebSocketStream _ws = null
        # 会话标识（GUID 字符串；注册表键）
        string _id = ""
        # 对端地址（"ip:port"）
        string _remote = ""
        # 命中的路由 pattern（注册表分组键）
        string _pattern = ""
        # 升级 URL 的路径参数
        Map<string,string> _pathParams = null
        # 升级 URL 原始 query（惰性解码）
        string _rawQuery = ""
        Map<string,string> _queryParsed = null

        # 引擎/独立服务内部构造（用户勿直接 new）
        _init_( Net.WebSocketStream ws, string pattern, Map<string,string> pathParams, string rawQuery )
        {
            this._ws = ws
            this._id = Guid.newGuid()
            this._pattern = pattern
            this._pathParams = pathParams
            this._rawQuery = rawQuery
            string ra = ws.remoteAddress
            Int32 rp = ws.remotePort
            this._remote = ra + ":" + rp.toString()
        }

        # ── 会话标识 ──

        public get string id()
        {
            ret this._id
        }

        public get string remoteAddress()
        {
            ret this._remote
        }

        # 命中的路由 pattern（注册/broadcast 分组键）
        public get string routePattern()
        {
            ret this._pattern
        }

        # ── 升级 URL 参数（与 HTTP 路由同一套 {param} 语法）──

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

        # 路径参数 Int32 化；坏格式用 defVal
        public Int32 pathInt( string name, Int32 defVal )
        {
            string v = this.pathParam( name )
            if v == null
            {
                ret defVal
            }
            Int64 parsed = HttpServerCore._parseDec64( v )
            if parsed < 0
            {
                ret defVal
            }
            ret parsed.toInt32()
        }

        # 查询参数（已解码：percent-decode + '+'→空格）；不存在返回 null
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

        # ── 收发（透传底层连接）──

        # 收一条完整消息（null = 连接关闭；载荷归属返回值，消费完 release）
        public Net.WsMessage receive() throws
        {
            ret this._ws.receive()
        }

        public void sendText( string text ) throws
        {
            this._ws.sendText( text )
        }

        # 二进制（底层 slice 视图，src 不被读空）
        public void sendBinary( ByteBuffer payload ) throws
        {
            this._ws.sendBinary( payload )
        }

        public void sendPing() throws
        {
            this._ws.sendPing()
        }

        # 优雅关闭（尽力发 Close 帧后关底层流）
        public void close()
        {
            label closeWs
            {
                this._ws.close()
            }
            catch { }
        }

        public get Net.WsCloseInfo closeInfo()
        {
            ret this._ws.closeInfo
        }

        public get bool isOpen()
        {
            ret this._ws.state == Net.WsState.Open
        }
    }

    # ============================================================================
    # WebSocketServer — 独立端口 WS 服务（无 HTTP 层，裸 TCP 直握手；§4.6 形态 B）
    # 静态门面：configure(port) → onMessage(fn)/onClose(fn) → start/stop。
    # 与设计实例形态的偏差见 SERVER_DESIGN.md §12（跨模块多实例化隔离不可靠）。
    # ============================================================================
    public class WebSocketServer
    {
        static Int32 _port = 0
        static Net.TcpServer _server = null
        static bool _running = false
        static Task _acceptTask = null
        # function( WsSession s, Net.WsMessage m )；消息回调（msg 归回调所有，
        # 消费完调 m.release()；WebSocketClient.onMessage 先例）
        static Function _onMessage = null
        # function( WsSession s )；会话收尾回调（可选）
        static Function _onClose = null
        # 本服务在线会话注册键（sessions/broadcast 底座）。
        # 静态 string 字面量默认值初始化不可靠（[2026-10-17] 复盘 ② 同族），
        # 经方法返回字面量（模块内字符串常量可靠）
        static string _key()
        {
            ret "*standalone*"
        }

        # ── 配置（start 前生效）──

        public static void configure( Int32 port )
        {
            _port = port
        }

        # 消息回调（2 参：会话 + 消息；msg 归回调所有）
        public static void onMessage( Function fn )
        {
            _onMessage = fn
        }

        # 会话收尾回调（1 参：会话）
        public static void onClose( Function fn )
        {
            _onClose = fn
        }

        # ── 生命周期 ──

        public static get bool isRunning()
        {
            ret _running
        }

        # 启动（便捷覆盖端口；立即返回，accept 循环独立协程）
        public static void start( Int32 port ) throws
        {
            _port = port
            WebSocketServer.start()
        }

        public static void start() throws
        {
            if _running
            {
                throw ServerError.AlreadyRunning
            }
            if _port <= 0 || _port > 65535
            {
                throw ServerError.InvalidConfig
            }
            _server = Net.Tcp.listen( _port )
            _running = true
            # accept 循环协程：闭包零捕获，实参传值（NetConcurrentTest.ef 先例）
            Net.TcpServer srv = _server
            function f = function( object a )
            {
                WebSocketServer._acceptLoop( a as Net.TcpServer )
            }
            _acceptTask = Coroutine.spawnClosure1( f, srv )
        }

        # 停止：关监听 + 关全部会话（会话协程随 receive null 自然收尾注销）
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
            string key = WebSocketServer._key()
            List<WsSession> list = _WsSessions.sessions( key )
            Int32 n = list.length
            for Int32 i = 0, i < n, i = i + 1
            {
                WsSession s = list[i]
                s.close()
            }
        }

        # 挂起直至停止
        public static void awaitStop() throws
        {
            if _acceptTask == null
            {
                throw ServerError.NotRunning
            }
            Task t = _acceptTask
            Coroutine.awaitTask( t )
        }

        # run = start + awaitStop（阻塞当前协程直至 stop）
        public static void run( Int32 port ) throws
        {
            WebSocketServer.start( port )
            WebSocketServer.awaitStop()
        }

        # ── 会话查询 / 组播 ──

        public static List<WsSession> sessions()
        {
            ret _WsSessions.sessions( WebSocketServer._key() )
        }

        public static Int32 broadcast( string text )
        {
            ret _WsSessions.broadcast( WebSocketServer._key(), text )
        }

        # ── 内部：accept 循环（每连接一协程：握手 → 会话循环 → 收尾）──

        static void _acceptLoop( Net.TcpServer srv )
        {
            bool run = true
            while run
            {
                label accOne
                {
                    Net.TcpStream client = srv.accept()
                    # 连接协程：零捕获 + 实参传值
                    function f = function( object a )
                    {
                        WebSocketServer._runSession( a as Net.TcpStream )
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

        # 单连接会话：握手 → WsSession → 消息循环（onMessage）→ 收尾（onClose）
        static void _runSession( Net.TcpStream client )
        {
            string key = WebSocketServer._key()
            # ws/s 提到 label 块外声明：catch 收尾路径须可见（stop() 强制
            # 关闭会令 receive 抛异常走 catch，漏注销则 sessions 不清零）
            Net.WebSocketStream ws = null
            WsSession s = null
            label sessBlock
            {
                # 独立形态无 HTTP 预读 → 两参 accept（无 preRead）
                ws = Net.WebSocketStream.accept( client, null )
                s = WsSession( ws, key, null, "" )
                _WsSessions.register( key, s )
                # Function 字段先拷局部再传参/调用（HttpHeaders 头注记纪律）
                function hMsg = _onMessage
                bool looping = true
                while looping
                {
                    Net.WsMessage m = ws.receive()
                    if m == null
                    {
                        looping = false
                    }
                    elif hMsg != null
                    {
                        # 回调异常吞掉（会话不因单条消息处理失败而中断；
                        # WebSocketClient 事件循环同款取舍）
                        label msgBlock
                        {
                            hMsg( s, m )
                        }
                        catch { }
                    }
                }
                _WsSessions.unregister( key, s )
                function hClose = _onClose
                if hClose != null
                {
                    label closeCb
                    {
                        hClose( s )
                    }
                    catch { }
                }
                ws.close()
            }
            catch
            {
                # 握手失败 / IO 异常（含 stop() 强制关闭）：已注册会话
                # 仍须注销 + 通知 onClose，再静默收尾
                if s != null
                {
                    _WsSessions.unregister( key, s )
                    function hClose2 = _onClose
                    if hClose2 != null
                    {
                        label closeCb2
                        {
                            hClose2( s )
                        }
                        catch { }
                    }
                }
                label rawClose
                {
                    if ws != null
                    {
                        ws.close()
                    }
                    client.close()
                }
                catch { }
                ret
            }
            # 正常路径：ws.close() 已关底层流，client.close 幂等无害
            label cleanup
            {
                client.close()
            }
            catch { }
        }
    }
}
