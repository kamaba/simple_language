namespace Net
{
    # ============================================================================
    # Net/Websocket.sl — WebSocket（RFC 6455）客户端 / 服务端
    #
    # 语义要点：
    #   - connect：ws / wss URL -> TCP / TLS 建连 -> HTTP Upgrade 握手
    #     （Sec-WebSocket-Key 随机 16 字节 Base64；Sec-WebSocket-Accept =
    #     Base64(SHA-1(key + 固定 GUID))，比对大小写敏感）
    #   - accept：服务端在已 accept 的 TcpStream 上解析请求并回 101
    #   - 帧层：客户端发帧强制掩码、收帧禁止掩码（方向校验，服务端反之）；
    #     RSV 必须为 0；控制帧（Close / Ping / Pong）FIN=1 且载荷 <= 125 字节；
    #     分片消息按 FIN + Continuation 重组
    #   - receive：同步收一条完整消息（返回 null = 连接关闭）；
    #     Ping 自动回 Pong（autoPong）；收到 Close 解析并回显协商
    #   - messages()：引流协程把消息灌进 Stream<WsMessage>（Stream 体系融合，
    #     UdpStream.datagrams 同款模式）；connectAsync：协程异步连接
    #   - close：尽力发 Close 帧 -> 关底层流，任一步失败不阻断收尾
    #
    # 实现注记：
    #   - SL 层纯协议实现：SHA-1 / 随机字节走系统方法（SystemSha1 /
    #     SystemRandomFill），帧编解码为逐字节位运算
    #     （UInt8 上下文一律 SystemConvertUInt8( Int32 表达式 )，Wav.sl 先例）
    #   - Tcp.connectTimeout 已直接返回 TcpStream（HttpClient 同款）；
    #     wss 经 TlsStream.wrap 包装
    #   - 掩码路径发送前拷贝载荷（TcpSocket.send 会推进 src.readerIndex），
    #     服务端路径以 slice() 视图零拷贝（视图索引独立，不推进源）
    # ============================================================================

    # ============================================================================
    # 错误 / 枚举
    # ============================================================================

    public enum WsError extends Error
    {
        # 握手失败（非 101 / 头缺失或非法 / Accept 不匹配）
        Handshake = { code = 1 }
        # 帧协议违例（RSV 非零 / 掩码方向错 / 控制帧约束 / 未知 opcode 等）
        Protocol = { code = 2 }
        # 消息超出 maxMessageSize
        MessageTooBig = { code = 3 }
        # 已关闭的连接上收发
        Closed = { code = 4 }
        # 随机源不可用（Sec-WebSocket-Key / 掩码生成失败）
        RandomSource = { code = 5 }
    }

    # 帧操作码（底层 Int32）
    public enum WsOpCode extends Int32
    {
        Continuation = 0
        Text = 1
        Binary = 2
        Close = 8
        Ping = 9
        Pong = 10
    }

    # Close 帧状态码（RFC 6455 §7.4.1 常用子集）
    public enum WsCloseCode extends Int32
    {
        Normal = 1000
        GoingAway = 1001
        ProtocolError = 1002
        UnsupportedData = 1003
        InvalidPayload = 1007
        PolicyViolation = 1008
        MessageTooBig = 1009
        InternalError = 1011
    }

    # 连接状态机
    public enum WsState extends Int32
    {
        # 握手中（仅 connect 流程内部短暂出现）
        Connecting = 0
        # 已建立（握手完成，可收发）
        Open = 1
        # 已发出 Close，等待对端回显
        Closing = 2
        # 已关闭（收到 Close 回显或底层断开）
        Closed = 3
    }

    # ============================================================================
    # WsMessage — 一条完整消息（分片已重组）
    # ============================================================================

    public class WsMessage
    {
        # 操作码（WsOpCode.Text / WsOpCode.Binary）
        public Int32 op = 0
        # 载荷（归属本消息；消费完调 release 归还，勿外部持有引用跨越 release）
        public ByteBuffer payload = null

        _init_( Int32 opCode, ByteBuffer buf )
        {
            this.op = opCode
            this.payload = buf
        }

        public get bool isText()
        {
            ret this.op == WsOpCode.Text
        }

        public get bool isBinary()
        {
            ret this.op == WsOpCode.Binary
        }

        public get Int32 size()
        {
            if this.payload == null
            {
                ret 0
            }
            ret this.payload.readableBytes
        }

        # 载荷按 UTF-8 解码为文本（内嵌 NUL 截断语义同 ByteBuffer.toString）
        public string text()
        {
            if this.payload == null
            {
                ret ""
            }
            ret this.payload.toString()
        }

        # 载荷原样返回（归属仍在本消息，release 前有效）
        public ByteBuffer binary()
        {
            ret this.payload
        }

        # 释放载荷（幂等：释放后置 null，重复调用无副作用）
        public void release()
        {
            if this.payload != null
            {
                this.payload.release()
                this.payload = null
            }
        }
    }

    # ============================================================================
    # WsCloseInfo — 关闭原因（对端 Close 帧解析结果）
    # ============================================================================

    public class WsCloseInfo
    {
        # 状态码（载荷 < 2 字节非法时按 1005 无状态码处理）
        public Int32 code = 0
        # 关闭原因文本（可为空串）
        public string reason = ""
        # 是否完成 Close 协商（收到对端 Close 即视为 clean）
        public bool wasClean = false
    }

    # ============================================================================
    # WsOptions — 连接 / 接入选项
    # ============================================================================

    public class WsOptions
    {
        # 子协议（逗号分隔列表，如 "chat,superchat"；空 = 不协商）
        public string protocols = ""
        # 单条消息上限字节（含分片重组后；超出抛 WsError.MessageTooBig）
        public Int32 maxMessageSize = 16777216
        # 握手连接超时毫秒（Tcp.connectTimeout 透传）
        public Int32 handshakeTimeoutMs = 10000
        # 收到 Ping 自动回 Pong（receive 循环内尽力发送）
        public bool autoPong = true
        # wss CA 证书链 PEM 文本（空 = 跳过验证，与 TlsOptions.caPem 一致）
        public string caPem = ""
    }

    # ============================================================================
    # _WsFrame — 内部帧结构（解析中间产物，不对外）
    # ============================================================================

    public class _WsFrame
    {
        bool fin = false
        Int32 opcode = 0
        ByteBuffer payload = null
    }

    # ============================================================================
    # WebSocketStream — WebSocket 连接（NetStream 消息子类）
    # ============================================================================

    public class WebSocketStream extends NetStream
    {
        # 底层字节流（TcpStream / TlsStream，握手期由静态方法持有后移交）
        NetStream _raw = null
        # 帧解析累积缓冲（握手期复用，握手后继续盛帧数据）
        ByteBuffer _recvBuf = null
        # 客户端角色（发帧掩码 / 收帧禁掩码；服务端反之）
        bool _isClient = false
        # 已发出 Close（幂等守卫）
        bool _sentClose = false
        # 连接状态机（enum 字段默认值先例：HttpClient.method）
        WsState _state = WsState.Connecting
        # 对端 Close 解析结果（未收到为 null）
        WsCloseInfo _closeInfo = null
        # 协商成功的子协议（未协商为空串）
        string _protocol = ""
        Int32 _maxMessageSize = 16777216
        bool _autoPong = true
        # 分片重组状态（_inFrag = true 期间 _fragAcc 累积 / _fragOpcode 记首帧）
        bool _inFrag = false
        ByteBuffer _fragAcc = null
        Int32 _fragOpcode = 0

        _init_( NetStream raw, ByteBuffer recvBuf, bool isClient, string protocol, WsOptions opt )
        {
            this._raw = raw
            this._recvBuf = recvBuf
            this._isClient = isClient
            this._protocol = protocol
            if opt != null
            {
                this._maxMessageSize = opt.maxMessageSize
                this._autoPong = opt.autoPong
            }
            # TcpStream 构造能力位同款（NetStream 子类显式置齐，不强制调父类 _init_）
            this._sid = raw._sid
            this._connected = true
            this._canSeek = false
            this._canTimeout = false
            this._isDuplex = true
            # 消息语义：裸字节写不支持（发消息走 sendText / sendBinary）
            this._canWrite = false
            this._isMessageOriented = true
            this._state = WsState.Open
        }

        # ── 状态查询 ──

        public get WsState state()
        {
            ret this._state
        }

        public get WsCloseInfo closeInfo()
        {
            ret this._closeInfo
        }

        public get string protocol()
        {
            ret this._protocol
        }

        public get bool isClient()
        {
            ret this._isClient
        }

        # ============================================================================
        # 连接（客户端）
        # ============================================================================

        # 连接 ws / wss 端点（默认选项）
        public static WebSocketStream connect( string url ) throws
        {
            ret WebSocketStream.connect( url, null )
        }

        # 连接 ws / wss 端点：建连 -> 发起 Upgrade 握手 -> 校验响应
        public static WebSocketStream connect( string url, WsOptions opt ) throws
        {
            if opt == null
            {
                opt = WsOptions()
            }
            Uri uri = Uri( url )
            if !uri.isWs && !uri.isWss
            {
                throw HttpError.UnsupportedScheme
            }

            # ---- 建连（明文 TCP / TLS）----
            # 注意：Tcp.connectTimeout 已直接返回 TcpStream（HttpClient 同款）；
            #   connectTimeoutMs 为 Int32，经宽松数值互转匹配 Int64 参数
            TcpStream raw = Tcp.connectTimeout( uri.host, uri.effectivePort, opt.handshakeTimeoutMs )
            raw.setNoDelay( true )
            NetStream stream = raw
            if uri.isWss
            {
                TlsOptions topt = TlsOptions()
                topt.caPem = opt.caPem
                topt.hostname = uri.host
                stream = TlsStream.wrap( raw, topt )
            }

            # ---- 随机 Key（16 字节 -> Base64 文本）----
            var keyId = SystemRandomFill( 16 )
            if keyId == 0
            {
                try stream.close()
                throw WsError.RandomSource
            }
            ByteBuffer keyBytes = ByteBuffer.fromHandle( keyId )
            string key = Base64.encodeToString( keyBytes )
            keyBytes.release()

            # ---- 握手请求（Host 自动管理；GET + Upgrade 头组）----
            string reqText = "GET " + uri.pathAndQuery + " HTTP/1.1\r\n"
            reqText = reqText + "Host: " + uri.hostHeader + "\r\n"
            reqText = reqText + "Upgrade: websocket\r\n"
            reqText = reqText + "Connection: Upgrade\r\n"
            reqText = reqText + "Sec-WebSocket-Key: " + key + "\r\n"
            reqText = reqText + "Sec-WebSocket-Version: 13\r\n"
            if SystemStringLength( opt.protocols ) > 0
            {
                reqText = reqText + "Sec-WebSocket-Protocol: " + opt.protocols + "\r\n"
            }
            reqText = reqText + "\r\n"
            stream.write( ByteBuffer.fromString( reqText ) )

            # ---- 响应校验（异常路径也尽力关连接后原样上抛）----
            ByteBuffer recvBuf = ByteBuffer()
            WebSocketStream ws = null
            # 注意：label{}...catch{} 中 label 块本身就是受 catch 保护的语句块
            #   （HttpClient 先例），块内直接写赋值即可
            label hsBlock
            {
                ws = WebSocketStream._connectHandshake( stream, recvBuf, key, opt )
            }
            catch
            {
                try stream.close()
                throw
            }
            ret ws
        }

        # 协程异步连接（结果经 Task.awaitTask() 取回，object as WebSocketStream）
        public static Task connectAsync( string url )
        {
            function fn = function( string u )
            {
                ret WebSocketStream.connect( u )
            }
            ret Coroutine.spawnClosure1( fn, url )
        }

        # 协程异步连接（带选项）
        public static Task connectAsync( string url, WsOptions opt )
        {
            function fn = function( string u, WsOptions o )
            {
                ret WebSocketStream.connect( u, o )
            }
            ret Coroutine.spawnClosure2( fn, url, opt )
        }

        # 客户端握手响应校验：101 + Upgrade 头组 + Accept 比对
        static WebSocketStream _connectHandshake( NetStream s, ByteBuffer buf, string key, WsOptions opt ) throws
        {
            string statusLine = WebSocketStream._readLine( s, buf )
            if statusLine == null
            {
                throw WsError.Handshake
            }
            statusLine = WebSocketStream._stripEol( statusLine )
            Int32 code = WebSocketStream._statusCode( statusLine )
            if code != 101
            {
                throw WsError.Handshake
            }
            HttpHeaders headers = WebSocketStream._readHeaders( s, buf )

            string upgrade = headers.getHeader( "Upgrade" )
            if upgrade == null || !Uri.eqFold( upgrade, "websocket" )
            {
                throw WsError.Handshake
            }
            string connection = headers.getHeader( "Connection" )
            if connection == null || !WebSocketStream._containsFold( connection, "upgrade" )
            {
                throw WsError.Handshake
            }
            # RFC 6455 §4.2.2：101 响应只含 Upgrade/Connection/Sec-WebSocket-Accept，
            # Sec-WebSocket-Version 仅在 426 拒绝响应中出现，此处不校验
            # Accept 比对大小写敏感（Base64 字母表本身区分大小写）
            string accept = headers.getHeader( "Sec-WebSocket-Accept" )
            string expect = WebSocketStream._acceptToken( key )
            if accept == null || !WebSocketStream._eqExact( accept, expect )
            {
                throw WsError.Handshake
            }

            # 子协议回显校验（服务端只允许回显客户端请求过的协议）
            string protocol = ""
            string proto = headers.getHeader( "Sec-WebSocket-Protocol" )
            if proto != null && SystemStringLength( proto ) > 0
            {
                if !WebSocketStream._listContainsFold( opt.protocols, proto )
                {
                    throw WsError.Handshake
                }
                protocol = proto
            }
            ret WebSocketStream( s, buf, true, protocol, opt )
        }

        # ============================================================================
        # 接入（服务端）
        # ============================================================================

        # 在已 accept 的连接上应答握手（默认选项）
        public static WebSocketStream accept( TcpStream raw ) throws
        {
            ret WebSocketStream.accept( raw, null )
        }

        # 在已 accept 的连接上应答握手：解析请求 -> 回 101 -> 移交消息层
        public static WebSocketStream accept( TcpStream raw, WsOptions opt ) throws
        {
            if opt == null
            {
                opt = WsOptions()
            }
            # 子类型先赋局部再传参（保守：避免类型推断歧义）
            NetStream s = raw
            ByteBuffer recvBuf = ByteBuffer()
            string protocol = WebSocketStream._acceptHandshake( s, recvBuf, opt )
            ret WebSocketStream( s, recvBuf, false, protocol, opt )
        }

        # 服务端握手应答：请求行 + 头校验 -> 101 + Accept 回填
        static string _acceptHandshake( NetStream s, ByteBuffer buf, WsOptions opt ) throws
        {
            string reqLine = WebSocketStream._readLine( s, buf )
            if reqLine == null
            {
                throw WsError.Handshake
            }
            if !WebSocketStream._startsWithFold( WebSocketStream._stripEol( reqLine ), "GET " )
            {
                throw WsError.Handshake
            }
            HttpHeaders headers = WebSocketStream._readHeaders( s, buf )
            string upgrade = headers.getHeader( "Upgrade" )
            if upgrade == null || !Uri.eqFold( upgrade, "websocket" )
            {
                throw WsError.Handshake
            }
            string connection = headers.getHeader( "Connection" )
            if connection == null || !WebSocketStream._containsFold( connection, "upgrade" )
            {
                throw WsError.Handshake
            }
            string version = headers.getHeader( "Sec-WebSocket-Version" )
            if version == null || !Uri.eqFold( version, "13" )
            {
                throw WsError.Handshake
            }
            string key = headers.getHeader( "Sec-WebSocket-Key" )
            if key == null || SystemStringLength( key ) == 0
            {
                throw WsError.Handshake
            }

            # 子协议协商：客户端请求列表与服务端支持列表取交集（服务端声明优先）
            string protocol = ""
            string reqProto = headers.getHeader( "Sec-WebSocket-Protocol" )
            if reqProto != null && SystemStringLength( reqProto ) > 0
            {
                if SystemStringLength( opt.protocols ) == 0
                {
                    throw WsError.Handshake
                }
                protocol = WebSocketStream._pickProtocol( opt.protocols, reqProto )
                if SystemStringLength( protocol ) == 0
                {
                    throw WsError.Handshake
                }
            }

            string respText = "HTTP/1.1 101 Switching Protocols\r\n"
            respText = respText + "Upgrade: websocket\r\n"
            respText = respText + "Connection: Upgrade\r\n"
            respText = respText + "Sec-WebSocket-Accept: " + WebSocketStream._acceptToken( key ) + "\r\n"
            if SystemStringLength( protocol ) > 0
            {
                respText = respText + "Sec-WebSocket-Protocol: " + protocol + "\r\n"
            }
            respText = respText + "\r\n"
            s.write( ByteBuffer.fromString( respText ) )
            ret protocol
        }

        # Accept 令牌：Base64( SHA-1( key + RFC 6455 固定 GUID ) )
        static string _acceptToken( string key ) throws
        {
            ByteBuffer b = ByteBuffer.fromString( key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" )
            ByteBuffer d = Sha1.digest( b )
            b.release()
            string t = Base64.encodeToString( d )
            d.release()
            ret t
        }

        # ============================================================================
        # 收消息（同步拉模式）
        # ============================================================================

        # 收一条完整消息（分片已重组 / 控制帧已处理）；
        # 返回 null = 连接关闭（Close 协商完成或底层 EOF）。
        # 消息载荷归属返回值，消费完调 msg.release() 归还。
        public WsMessage receive() throws
        {
            while true
            {
                _WsFrame f = this._readFrame()
                if f == null
                {
                    this._state = WsState.Closed
                    this._connected = false
                    ret null
                }

                # Close：解析 + 回显协商 + 收尾
                if f.opcode == WsOpCode.Close
                {
                    # 先切片后解析：_parseClose 会推进 readerIndex，
                    # 先取视图才能拿到完整回显载荷（slice 视图索引独立于源）
                    ByteBuffer echoPayload = f.payload.slice()
                    this._closeInfo = WebSocketStream._parseClose( f.payload )
                    # 先回显再置 Closed：_writeFrame 对 Closed 状态抛 WsError.Closed，
                    # 若先置状态，回显帧永远发不出去（对端只能读到 EOF，closeInfo 丢失）
                    if !this._sentClose
                    {
                        this._sentClose = true
                        this._trySend( WsOpCode.Close, echoPayload )
                    }
                    this._state = WsState.Closed
                    this._connected = false
                    f.payload.release()
                    ret null
                }

                # Ping：自动回 Pong（尽力发送）
                if f.opcode == WsOpCode.Ping
                {
                    if this._autoPong && !this._sentClose
                    {
                        this._trySend( WsOpCode.Pong, f.payload.slice() )
                    }
                    f.payload.release()
                    continue
                }

                # Pong：心跳应答，无用户可见语义
                if f.opcode == WsOpCode.Pong
                {
                    f.payload.release()
                    continue
                }

                # Continuation：分片续帧
                if f.opcode == WsOpCode.Continuation
                {
                    if !this._inFrag
                    {
                        f.payload.release()
                        throw WsError.Protocol
                    }
                    if this._fragAcc.readableBytes + f.payload.readableBytes > this._maxMessageSize
                    {
                        f.payload.release()
                        throw WsError.MessageTooBig
                    }
                    this._fragAcc.writeBytes( f.payload )
                    f.payload.release()
                    if f.fin
                    {
                        ByteBuffer payload = this._fragAcc
                        this._fragAcc = null
                        this._inFrag = false
                        Int32 op = this._fragOpcode
                        this._fragOpcode = 0
                        ret WsMessage( op, payload )
                    }
                    continue
                }

                # Text / Binary：首帧（fin = 单帧完整消息；否则开启分片累积）
                if f.opcode == WsOpCode.Text || f.opcode == WsOpCode.Binary
                {
                    if this._inFrag
                    {
                        f.payload.release()
                        throw WsError.Protocol
                    }
                    if f.fin
                    {
                        ret WsMessage( f.opcode, f.payload )
                    }
                    this._inFrag = true
                    this._fragOpcode = f.opcode
                    this._fragAcc = f.payload
                    continue
                }

                # 未知 opcode
                f.payload.release()
                throw WsError.Protocol
            }
            ret null
        }

        # 消息流（Stream 体系直连）：引流协程循环收消息灌进 controller，
        # 消费方经 Stream<WsMessage> 逐条读取；连接关闭 / 出错时 ctrl.close() 收尾。
        # try? 异常语义：失败返回默认值（类类型 = null），与关闭返回 null 合流
        public Stream<WsMessage> messages()
        {
            StreamController<WsMessage> ctrl = StreamController<WsMessage>( 64 )
            # 闭包捕获限制：this 一律先拷局部再进闭包（NetStream.datagrams 同款）
            WebSocketStream ws = this
            function f = function()
            {
                while true
                {
                    WsMessage msg = try? ws.receive()
                    if msg == null
                    {
                        break
                    }
                    ctrl.add( msg )
                }
                # 非 Closed 退出 = 异常路径（对端正常关闭时 state 已置 Closed）
                if ws.state != WsState.Closed
                {
                    ctrl.addError( WsError.Closed )
                }
                ctrl.close()
            }
            Task task = Coroutine.spawnClosure0( f )
            ret ctrl.stream
        }

        # ============================================================================
        # 帧层（内部）
        # ============================================================================

        # 读一帧（null = EOF）；帧头 / 掩码方向 / 控制帧约束 / 尺寸全面校验
        _WsFrame _readFrame() throws
        {
            if !WebSocketStream._fill( this._raw, this._recvBuf, 2 )
            {
                ret null
            }
            UInt8 b0 = this._recvBuf.readU8()
            UInt8 b1 = this._recvBuf.readU8()
            Int32 h0 = b0
            Int32 h1 = b1
            bool fin = ( h0 & 0x80 ) != 0
            Int32 opcode = h0 & 0x0F
            bool masked = ( h1 & 0x80 ) != 0
            Int32 len = h1 & 0x7F

            # RSV 位必须为 0（扩展协商未实现）
            if ( h0 & 0x70 ) != 0
            {
                throw WsError.Protocol
            }
            # 掩码方向：客户端收到掩码帧 / 服务端收到未掩码帧均违例
            if ( this._isClient && masked ) || ( !this._isClient && !masked )
            {
                throw WsError.Protocol
            }

            # 扩展长度（大端：126 -> U16，127 -> U64）
            if len == 126
            {
                if !WebSocketStream._fill( this._raw, this._recvBuf, 2 )
                {
                    ret null
                }
                UInt16 ext = this._recvBuf.readU16Be()
                len = ext
            }
            elif len == 127
            {
                if !WebSocketStream._fill( this._raw, this._recvBuf, 8 )
                {
                    ret null
                }
                UInt32 hi = this._recvBuf.readU32Be()
                UInt32 lo = this._recvBuf.readU32Be()
                Int32 hii = hi
                Int32 loi = lo
                if hii != 0 || loi < 0
                {
                    throw WsError.MessageTooBig
                }
                len = loi
            }

            # 控制帧：FIN 必须为 1 且载荷 <= 125 字节
            if opcode >= 8 && ( !fin || len > 125 )
            {
                throw WsError.Protocol
            }
            if len > this._maxMessageSize
            {
                throw WsError.MessageTooBig
            }

            # 载荷读取（掩码帧逐字节解掩；明文帧整块搬移）
            ByteBuffer payload = null
            if len == 0
            {
                payload = ByteBuffer()
            }
            elif masked
            {
                if !WebSocketStream._fill( this._raw, this._recvBuf, 4 )
                {
                    ret null
                }
                Int32 m0 = this._recvBuf.readU8()
                Int32 m1 = this._recvBuf.readU8()
                Int32 m2 = this._recvBuf.readU8()
                Int32 m3 = this._recvBuf.readU8()
                if !WebSocketStream._fill( this._raw, this._recvBuf, len )
                {
                    ret null
                }
                payload = ByteBuffer( len )
                for Int32 i = 0, i < len, i = i + 1
                {
                    Int32 mod = i & 3
                    Int32 m = m0
                    if mod == 1
                    {
                        m = m1
                    }
                    elif mod == 2
                    {
                        m = m2
                    }
                    elif mod == 3
                    {
                        m = m3
                    }
                    UInt8 b = this._recvBuf.readU8()
                    Int32 bi = b
                    payload.writeU8( SystemConvertUInt8( bi ^ m ) )
                }
            }
            else
            {
                if !WebSocketStream._fill( this._raw, this._recvBuf, len )
                {
                    ret null
                }
                payload = ByteBuffer( len )
                this._recvBuf.readBytes( payload, len )
            }

            var f = _WsFrame()
            f.fin = fin
            f.opcode = opcode
            f.payload = payload
            ret f
        }

        # 发一帧（掩码按角色自动施加；payload 会被读空——调用方传拷贝 / 视图）
        void _writeFrame( Int32 opcode, ByteBuffer payload ) throws
        {
            if this._state == WsState.Closed
            {
                throw WsError.Closed
            }
            if this._state == WsState.Closing && opcode != WsOpCode.Close
            {
                throw WsError.Closed
            }
            Int32 len = 0
            if payload != null
            {
                len = payload.readableBytes
            }

            # 帧头：FIN=1 + opcode；掩码位按角色；长度三段编码（大端）
            ByteBuffer hdr = ByteBuffer( 14 )
            UInt8 b0 = SystemConvertUInt8( 0x80 | opcode )
            hdr.writeU8( b0 )
            Int32 maskBit = 0
            if this._isClient
            {
                maskBit = 0x80
            }
            if len < 126
            {
                UInt8 b1 = SystemConvertUInt8( maskBit | len )
                hdr.writeU8( b1 )
            }
            elif len < 65536
            {
                UInt8 b1 = SystemConvertUInt8( maskBit | 126 )
                hdr.writeU8( b1 )
                UInt16 v = len
                hdr.writeU16Be( v )
            }
            else
            {
                UInt8 b1 = SystemConvertUInt8( maskBit | 127 )
                hdr.writeU8( b1 )
                UInt32 z = 0
                hdr.writeU32Be( z )
                UInt32 v = len
                hdr.writeU32Be( v )
            }
            # this.字段直接作 if 条件在 C VM 有求值异常（服务端 else 分支被跳过），
            # 先拷局部变量再判断（Uri.sl 跨类调用同款规避）
            bool isCli = this._isClient

            # 客户端：随机 4 字节掩码 + 逐字节 XOR 拷贝
            #（TcpSocket.send 会推进 src.readerIndex，掩码路径天然拷贝）
            if isCli
            {
                var maskId = SystemRandomFill( 4 )
                if maskId == 0
                {
                    hdr.release()
                    throw WsError.RandomSource
                }
                ByteBuffer maskBytes = ByteBuffer.fromHandle( maskId )
                Int32 m0 = maskBytes.readU8()
                Int32 m1 = maskBytes.readU8()
                Int32 m2 = maskBytes.readU8()
                Int32 m3 = maskBytes.readU8()
                maskBytes.release()
                ByteBuffer masked = ByteBuffer( len )
                for Int32 i = 0, i < len, i = i + 1
                {
                    UInt8 b = payload.readU8()
                    Int32 bi = b
                    Int32 mod = i & 3
                    Int32 m = m0
                    if mod == 1
                    {
                        m = m1
                    }
                    elif mod == 2
                    {
                        m = m2
                    }
                    elif mod == 3
                    {
                        m = m3
                    }
                    masked.writeU8( SystemConvertUInt8( bi ^ m ) )
                }
                # RFC 6455 §5.3：掩码键（4 字节）紧跟帧头、先于载荷写入
                hdr.writeU8( SystemConvertUInt8( m0 ) )
                hdr.writeU8( SystemConvertUInt8( m1 ) )
                hdr.writeU8( SystemConvertUInt8( m2 ) )
                hdr.writeU8( SystemConvertUInt8( m3 ) )
                this._raw.write( hdr )
                if len > 0
                {
                    this._raw.write( masked )
                }
                masked.release()
            }
            # 服务端：明文帧，slice() 视图零拷贝（视图索引独立，不推进源）
            else
            {
                this._raw.write( hdr )
                if len > 0
                {
                    ByteBuffer sv = payload.slice()
                    this._raw.write( sv )
                }
            }
            hdr.release()
        }

        # 尽力发送：连接已断 / 写失败时静默放弃，不打断收包主流程
        #（Pong 回显 / Close 协商容错路径）
        void _trySend( Int32 opcode, ByteBuffer payload )
        {
            label sendBlock
            {
                this._writeFrame( opcode, payload )
            }
            catch
            {
            }
        }

        # ============================================================================
        # 发消息
        # ============================================================================

        # 发文本消息（UTF-8）
        public void sendText( string text ) throws
        {
            ByteBuffer b = ByteBuffer.fromString( text )
            this._writeFrame( WsOpCode.Text, b )
            b.release()
        }

        # 发二进制消息（内部走 slice 视图，src 不被读空）
        public void sendBinary( ByteBuffer payload ) throws
        {
            this._writeFrame( WsOpCode.Binary, payload.slice() )
        }

        # 发二进制消息（字节数组）
        public void sendBinary( UInt8Array payload ) throws
        {
            ByteBuffer b = ByteBuffer.fromBytes( payload )
            this._writeFrame( WsOpCode.Binary, b )
            b.release()
        }

        # 发心跳探测（空载荷 Ping）
        public void sendPing() throws
        {
            this._writeFrame( WsOpCode.Ping, ByteBuffer() )
        }

        # 发心跳应答（空载荷 Pong；对端主动 Ping 时通常无需手调，autoPong 已回）
        public void sendPong() throws
        {
            this._writeFrame( WsOpCode.Pong, ByteBuffer() )
        }

        # 发 Close 并进入 Closing（默认 1000 正常关闭）
        public void sendClose() throws
        {
            this.sendClose( WsCloseCode.Normal, "" )
        }

        # 发 Close 并进入 Closing（幂等：已发过直接返回）
        public void sendClose( Int32 code, string reason ) throws
        {
            if this._sentClose
            {
                ret
            }
            this._sentClose = true
            this._state = WsState.Closing
            ByteBuffer b = WebSocketStream._closePayload( code, reason )
            this._writeFrame( WsOpCode.Close, b )
            b.release()
        }

        # Close 载荷：U16 状态码 + 可选 UTF-8 原因（截断到 123 字节满足 <= 125）
        static ByteBuffer _closePayload( Int32 code, string reason )
        {
            ByteBuffer b = ByteBuffer( 128 )
            UInt16 c = code
            b.writeU16Be( c )
            if SystemStringLength( reason ) > 0
            {
                ByteBuffer r = ByteBuffer.fromString( reason )
                Int32 rl = r.readableBytes
                if rl > 123
                {
                    rl = 123
                }
                b.writeBytes( r, rl )
                r.release()
            }
            ret b
        }

        # ============================================================================
        # ByteStream 契约（消息流适配）
        # ============================================================================

        # 消息粒度读：一次 read 收一条消息写入 dst（超出 dst 容量部分丢弃）；
        # 返回 0 = 关闭（置位 _eof）
        override public Int32 read( ByteBuffer dst ) throws
        {
            this._ensureReadable()
            WsMessage msg = this.receive()
            if msg == null
            {
                this._eof = true
                ret 0
            }
            Int32 n = 0
            if msg.payload != null
            {
                n = msg.payload.readableBytes
            }
            Int32 room = dst.writableBytes
            if n > room
            {
                n = room
            }
            if n > 0
            {
                dst.writeBytes( msg.payload, n )
            }
            msg.release()
            ret n
        }

        # 消息语义：裸字节写不支持（发消息走 sendText / sendBinary）
        override public void write( ByteBuffer src ) throws
        {
            this._ensureWritable()
        }

        override public void flush() throws
        {
            # 帧即时下发，无用户态缓冲，flush 为空操作
        }

        # 半关闭：停发（尽力发 Close 通知对端，对端 receive 将见 null）
        override public void closeWrite() throws
        {
            if !this._closedWrite
            {
                this.flush()
                label closeFrame
                {
                    this.sendClose( WsCloseCode.Normal, "" )
                }
                catch { }
                this._closedWrite = true
            }
        }

        # 完全覆写：尽力发 Close 帧 -> 关底层流 -> 封口（TcpStream 同款）
        override public void close() throws
        {
            if this._raw != null
            {
                label closeFrame
                {
                    this.sendClose( WsCloseCode.Normal, "" )
                }
                catch { }
                label rawClose
                {
                    this._raw.close()
                }
                catch { }
                this._raw = null
            }
            this._sid = 0
            this._connected = false
            this._state = WsState.Closed
            this._closedRead = true
            this._closedWrite = true
            this._eof = true
            this._releaseFrag()
        }

        # 分片累积缓冲回收（close / 出错路径共用）
        void _releaseFrag()
        {
            if this._fragAcc != null
            {
                this._fragAcc.release()
                this._fragAcc = null
            }
            this._inFrag = false
            this._fragOpcode = 0
        }

        # ============================================================================
        # 静态辅助（HttpClient 同款文本 / 解析工具 + WS 专属）
        # ============================================================================

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

        # 忽略大小写子串查找（Connection 头值判 upgrade 用）
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

        # 头域循环读取（空行截止；名字后冒号 + 可选前导空格）
        static HttpHeaders _readHeaders( NetStream s, ByteBuffer buf ) throws
        {
            HttpHeaders headers = HttpHeaders()
            while true
            {
                string line = WebSocketStream._readLine( s, buf )
                if line == null
                {
                    throw WsError.Handshake
                }
                line = WebSocketStream._stripEol( line )
                if SystemStringLength( line ) == 0
                {
                    break
                }
                Int32 len = SystemStringLength( line )
                Int32 colon = -1
                for Int32 i = 0, i < len, i = i + 1
                {
                    if SystemStringCharCodeAt( line, i ) == 58
                    {
                        colon = i
                        break
                    }
                }
                if colon <= 0
                {
                    throw WsError.Handshake
                }
                string name = SystemStringRange( line, 0, colon )
                Int32 vstart = colon + 1
                while vstart < len && SystemStringCharCodeAt( line, vstart ) == 32
                {
                    vstart = vstart + 1
                }
                # SystemStringRange 语义是半开区间 [start, end)：第三个参数是结束下标而非长度
                string value = SystemStringRange( line, vstart, len )
                headers.add( name, value )
            }
            ret headers
        }

        # 状态行状态码解析（"HTTP/1.1 101 ..." -> 101；非法返回 -1）
        static Int32 _statusCode( string line )
        {
            Int32 len = SystemStringLength( line )
            Int32 sp = -1
            for Int32 i = 0, i < len, i = i + 1
            {
                if SystemStringCharCodeAt( line, i ) == 32
                {
                    sp = i
                    break
                }
            }
            if sp <= 0 || sp + 4 > len
            {
                ret -1
            }
            Int32 v = 0
            for Int32 i = 0, i < 3, i = i + 1
            {
                Int32 c = SystemStringCharCodeAt( line, sp + 1 + i )
                if c < 48 || c > 57
                {
                    ret -1
                }
                v = v * 10 + ( c - 48 )
            }
            ret v
        }

        # 精确比对（不折叠大小写；Sec-WebSocket-Accept 用）
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

        # 忽略大小写前缀匹配（服务端请求行判 "GET " 用）
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
                if Uri.fold( SystemStringCharCodeAt( s, i ) ) != Uri.fold( SystemStringCharCodeAt( prefix, i ) )
                {
                    ret false
                }
            }
            ret true
        }

        # 子协议协商：遍历 supported 的每个 token，
        # 命中 requested 列表（大小写不敏感）即返回；无交集返回空串
        static string _pickProtocol( string supported, string requested )
        {
            Int32 slen = SystemStringLength( supported )
            Int32 i = 0
            while i < slen
            {
                # 跳过前导分隔（空格 / 制表 / 逗号）
                while i < slen
                {
                    Int32 c = SystemStringCharCodeAt( supported, i )
                    if c != 32 && c != 9 && c != 44
                    {
                        break
                    }
                    i = i + 1
                }
                if i >= slen
                {
                    break
                }
                Int32 start = i
                while i < slen
                {
                    Int32 c = SystemStringCharCodeAt( supported, i )
                    if c == 32 || c == 9 || c == 44
                    {
                        break
                    }
                    i = i + 1
                }
                string token = SystemStringRange( supported, start, i )
                if WebSocketStream._listContainsFold( requested, token )
                {
                    ret token
                }
            }
            ret ""
        }

        # 逗号 / 空白分隔列表包含判定（token 粒度，大小写不敏感）
        static bool _listContainsFold( string list, string token )
        {
            Int32 llen = SystemStringLength( list )
            Int32 tlen = SystemStringLength( token )
            if tlen == 0
            {
                ret false
            }
            Int32 i = 0
            while i < llen
            {
                while i < llen
                {
                    Int32 c = SystemStringCharCodeAt( list, i )
                    if c != 32 && c != 9 && c != 44
                    {
                        break
                    }
                    i = i + 1
                }
                if i >= llen
                {
                    break
                }
                Int32 start = i
                while i < llen
                {
                    Int32 c = SystemStringCharCodeAt( list, i )
                    if c == 32 || c == 9 || c == 44
                    {
                        break
                    }
                    i = i + 1
                }
                string cur = SystemStringRange( list, start, i )
                if Uri.eqFold( cur, token )
                {
                    ret true
                }
            }
            ret false
        }

        # Close 帧载荷解析：U16 状态码 + 可选 UTF-8 原因；
        # 载荷 < 2 字节非法按 1005（无状态码）处理；调用会推进 payload.readerIndex
        static WsCloseInfo _parseClose( ByteBuffer payload ) throws
        {
            WsCloseInfo info = WsCloseInfo()
            info.wasClean = true
            if payload == null
            {
                ret info
            }
            Int32 len = payload.readableBytes
            if len >= 2
            {
                UInt16 c = payload.readU16Be()
                info.code = c
            }
            else
            {
                info.code = 1005
            }
            if len > 2
            {
                info.reason = payload.readString( len - 2 )
            }
            ret info
        }

        # 累积读取：循环从流拉直到 buf 可读 >= need；
        # EOF（n == 0）返回 false；容量不足先压缩再扩容（4096 起步）
        static bool _fill( NetStream s, ByteBuffer buf, Int32 need ) throws
        {
            while buf.readableBytes < need
            {
                Int32 want = need - buf.readableBytes
                if buf.writableBytes < want
                {
                    buf.discardReadBytes()
                    if buf.writableBytes < want
                    {
                        Int32 ensure = want
                        if ensure < 4096
                        {
                            ensure = 4096
                        }
                        buf.ensureWritable( ensure )
                    }
                }
                Int32 n = s.read( buf )
                if n == 0
                {
                    ret false
                }
            }
            ret true
        }
    }

    # ============================================================================
    # WebSocketClient — 高层回调式客户端（HttpClient 风格门面 + Python
    # WebSocketApp 事件模型：onOpen / onMessage / onClose / onError）
    #
    # 用法:
    #   var c = Net.WebSocketClient()
    #   c.onOpen = function()
    #   {
    #       Console.println( "opened" )
    #   }
    #   c.onMessage = function( Net.WsMessage m )
    #   {
    #       Console.println( "recv: " + m.text() )
    #       m.release()
    #   }
    #   c.onClose = function( Net.WsCloseInfo info ) { }
    #   c.onError = function( Error e ) { }
    #   c.connect( "ws://127.0.0.1:9001/chat" )   # 握手 + 启动事件循环
    #   c.sendText( "hi" )
    #   c.waitClosed()                            # 等事件循环收尾
    #
    # 语义要点:
    #   - connect：建连握手原地抛错（失败调用方直接 catch）；
    #     成功后启动分发协程——循环 receive() 把消息推给 onMessage
    #   - 事件顺序：onOpen 恰一次 -> onMessage 0..N 次 ->
    #     [onError 0..1 次] -> onClose 恰一次
    #   - 协议违例 / onMessage 内抛错 -> onError（一次），随后 onClose 收尾；
    #     未设 onError 时异常吞掉（_ControllerStream.listen 同款取舍）
    #   - onClose 参数 WsCloseInfo 可能为 null（异常断开，无协商信息）
    #   - close()：尽力发 Close 帧后关底层流，事件循环随后自然收尾触发 onClose
    #   - 配置字段连接前赋值（protocols / caPem / 握手超时 / 消息上限 / autoPong）
    # ============================================================================

    public class WebSocketClient
    {
        # ---- 回调（连接前赋值；null = 不通知）----

        # function() — 握手完成时调用一次
        public Function onOpen = null
        # function( WsMessage msg ) — 每条 Text / Binary 消息
        #   （msg 归回调所有，消费完调 m.release() 归还）
        public Function onMessage = null
        # function( WsCloseInfo info ) — 连接收尾时调用一次（info 可能为 null）
        public Function onClose = null
        # function( Error err ) — 事件循环异常（协议违例 / 回调抛错）
        public Function onError = null

        # ---- 配置（连接前赋值；语义同 WsOptions 同名字段）----
        public string protocols = ""
        public string caPem = ""
        public Int32 handshakeTimeoutMs = 10000
        public Int32 maxMessageSize = 16777216
        public bool autoPong = true
        # 优雅关闭等待对端回显 Close 的超时（毫秒）；超时强制收尾防事件循环挂起
        public Int32 closeTimeoutMs = 5000

        # 底层连接（connect 后非空）
        WebSocketStream _ws = null
        # 事件循环协程（connect 后非空）
        Task _task = null

        # 连接并启动事件循环；握手失败原地抛（重连语义：新建 WebSocketClient）
        public WebSocketClient connect( string url ) throws
        {
            if this._ws != null
            {
                # 已连接（重复 connect）：拒绝，防事件循环双开
                throw WsError.Closed
            }
            WsOptions opt = WsOptions()
            opt.protocols = this.protocols
            opt.caPem = this.caPem
            opt.handshakeTimeoutMs = this.handshakeTimeoutMs
            opt.maxMessageSize = this.maxMessageSize
            opt.autoPong = this.autoPong
            WebSocketStream ws = WebSocketStream.connect( url, opt )
            this._ws = ws

            # 闭包捕获限制：this / 回调字段先拷局部（messages() 同款）
            var hOpen = this.onOpen
            var hMsg = this.onMessage
            var hClose = this.onClose
            var hErr = this.onError
            function f = function()
            {
                WebSocketClient._runLoop( ws, hOpen, hMsg, hClose, hErr )
            }
            this._task = Coroutine.spawnClosure0( f )
            ret this
        }

        # 事件循环体（独立静态方法：label-catch 保护 + 收尾回调顺序保证）
        static void _runLoop( WebSocketStream ws, Function hOpen, Function hMsg, Function hClose, Function hErr )
        {
            label evLoop
            {
                if hOpen != null
                {
                    hOpen()
                }
                bool running = true
                while running
                {
                    WsMessage msg = ws.receive()
                    if msg == null
                    {
                        running = false
                    }
                    elif hMsg != null
                    {
                        hMsg( msg )
                    }
                }
            }
            catch e
            {
                # 协议违例 / onMessage 抛错：有 onError 则通知，无则吞
                if hErr != null
                {
                    hErr( e )
                }
            }
            # onClose 恰一次收尾（正常关闭 / 异常断开都到达；info 可能为 null）
            if hClose != null
            {
                hClose( ws.closeInfo )
            }
            # 兜底释放底层连接（对端发起关闭时 receive 返回 null 但底层流未关）
            label cleanup
            {
                ws.close()
            }
            catch { }
        }

        # 等待事件循环结束（onClose 已送达；返回后连接终结）
        public void waitClosed()
        {
            if this._task != null
            {
                Coroutine.awaitTask( this._task )
            }
        }

        # 未连接守卫（发送族前置检查）
        void _ensureConnected() throws
        {
            if this._ws == null
            {
                throw WsError.Closed
            }
        }

        # ── 发送（转发底层连接）──

        public void sendText( string text ) throws
        {
            this._ensureConnected()
            this._ws.sendText( text )
        }

        public void sendBinary( ByteBuffer payload ) throws
        {
            this._ensureConnected()
            this._ws.sendBinary( payload )
        }

        public void sendBinary( UInt8Array payload ) throws
        {
            this._ensureConnected()
            this._ws.sendBinary( payload )
        }

        public void sendPing() throws
        {
            this._ensureConnected()
            this._ws.sendPing()
        }

        # ── 关闭 ──

        # 优雅关闭：只发 Close 帧并等待对端回显，事件循环收尾触发 onClose
        # （携带协商 closeInfo）；对端不回显时超时强制收尾。失败不阻断
        public void close()
        {
            this._beginClose( WsCloseCode.Normal, "" )
        }

        # 带状态码优雅关闭（语义同 close()，指定 code / reason）
        public void close( Int32 code, string reason )
        {
            this._beginClose( code, reason )
        }

        # 立即强制关闭：不等对端回显，事件循环立即结束（onClose 的 info 可能为 null）
        public void abort()
        {
            if this._ws != null
            {
                label rawClose
                {
                    this._ws.close()
                }
                catch { }
            }
        }

        void _beginClose( Int32 code, string reason )
        {
            if this._ws == null
            {
                ret
            }
            WebSocketStream ws = this._ws
            Int32 tmo = this.closeTimeoutMs
            label sndClose
            {
                ws.sendClose( code, reason )
            }
            catch { }
            # 对端可能不回显 Close：超时强制关闭读端，防事件循环永久阻塞
            function f = function()
            {
                Coroutine.delay( tmo )
                label forced
                {
                    ws.close()
                }
                catch { }
            }
            Coroutine.spawnClosure0( f )
        }

        # ── 状态查询（未连接时 state = Closed）──

        public get WsState state()
        {
            if this._ws == null
            {
                ret WsState.Closed
            }
            ret this._ws.state
        }

        public get WsCloseInfo closeInfo()
        {
            if this._ws == null
            {
                ret null
            }
            ret this._ws.closeInfo
        }

        public get string protocol()
        {
            if this._ws == null
            {
                ret ""
            }
            ret this._ws.protocol
        }

        public get bool isOpen()
        {
            ret this.state == WsState.Open
        }
    }
}
