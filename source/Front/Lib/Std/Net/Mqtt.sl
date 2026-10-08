namespace Net
{
    # ============================================================================
    # Net/Mqtt.sl — MQTT 3.1.1 客户端（纯 SL 协议实现）
    #
    # 语义要点：
    #   - connect：TCP 建连 -> 发 CONNECT -> 等 CONNACK（returnCode != 0 抛
    #     ConnackRejected）；握手期间设读超时，握手成功后恢复无限期阻塞
    #   - 剩余长度 varint：每字节低 7 位有效、最高位延续、最多 4 字节、
    #     上限 268435455（MQTT 规范 §2.2.3）
    #   - publish / subscribe / unsubscribe / ping：QoS 1 场景等对应 ACK
    #     （PUBACK / SUBACK / UNSUBACK / PINGRESP）
    #   - receive：同步收一条 PUBLISH（返回 null = 对端 DISCONNECT 或断开）；
    #     QoS 1 消息自动回 PUBACK；等待类 ACK 途中收到的 PUBLISH 入队缓存
    #   - 仅支持 QoS 0 / 1（QoS 2 抛 Protocol）；disconnect：尽力发送
    #     DISCONNECT 后关流，任一步失败不阻断收尾
    #
    # 实现注记：
    #   - SL 层纯协议实现（Websocket.sl 同款模式），TCP 走 Tcp.connectTimeout，
    #     载荷缓冲走 ByteBuffer（release 归还注册表）
    #   - UInt8 上下文一律 SystemConvertUInt8( Int32 表达式 )；
    #     UInt16 上下文先 Int32 -> UInt16 局部变量（Websocket 811 行先例）
    #   - parsePacket：返回 null = 数据不足且不推进 readerIndex（回滚），
    #     readPacket 循环 _fill 补数据（Websocket._fill 同款）
    #   - _MqttPacket 为内部解析中间产物，不对外注册（_WsFrame 同款）
    # ============================================================================

    # ============================================================================
    # 错误 / 枚举
    # ============================================================================

    public enum MqttError extends Error
    {
        # 协议违例（剩余长度非法 / 包类型错 / CONNACK 载荷不完整等）
        Protocol = { code = 1 }
        # 服务端 CONNACK 拒绝（returnCode != 0）
        ConnackRejected = { code = 2 }
        # 已关闭的连接上收发 / 握手中对端断开
        Closed = { code = 3 }
    }

    # 服务质量等级（底层 Int32；本实现仅支持 0 / 1）
    public enum MqttQoS extends Int32
    {
        AtMostOnce = 0
        AtLeastOnce = 1
        ExactlyOnce = 2
    }

    # 包类型（固定头第 1 字节高 4 位）
    public enum MqttPacketType extends Int32
    {
        Connect = 1
        Connack = 2
        Publish = 3
        PubAck = 4
        Subscribe = 8
        Suback = 9
        Unsubscribe = 10
        Unsuback = 11
        PingReq = 12
        PingResp = 13
        Disconnect = 14
    }

    # ============================================================================
    # MqttMessage — 一条收到的 PUBLISH 消息
    # ============================================================================

    public class MqttMessage
    {
        # 主题
        public string topic = ""
        # 载荷（归属本消息；消费完调 release 归还，勿外部持有引用跨越 release）
        public ByteBuffer payload = null
        # 服务端授予的 QoS（0 / 1）
        public Int32 qos = 0
        # QoS 1 时的报文标识符
        public Int32 packetId = 0
        # retain 标志
        public bool retain = false

        _init_( string topic, ByteBuffer payload, Int32 qos, Int32 packetId, bool retain )
        {
            this.topic = topic
            this.payload = payload
            this.qos = qos
            this.packetId = packetId
            this.retain = retain
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
    # MqttConnectOptions — CONNECT 报文参数
    # ============================================================================

    public class MqttConnectOptions
    {
        # 客户端标识（UTF-8 编码后 <= 65535 字节由 _writeMqttString 自然截断/校验）
        public string clientId = "slmqtt"
        # 清除会话标志（CONNECT flags bit1）
        public bool cleanSession = true
        # 保活间隔（秒，0 = 不保活）
        public Int32 keepAliveSeconds = 60
        # 用户名（空串 = 不携带）
        public string username = ""
        # 密码（空串 = 不携带）
        public string password = ""
        # TCP 建连 + 握手（CONNACK 等待）超时毫秒
        public Int32 connectTimeoutMs = 10000
    }

    # ============================================================================
    # _MqttPacket — 内部已解析包（固定头 + 载荷，不对外注册）
    # ============================================================================

    public class _MqttPacket
    {
        public Int32 type = 0
        public Int32 flags = 0
        public ByteBuffer payload = null

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
    # MqttCodec — 编解码（全静态）
    # ============================================================================

    public class MqttCodec
    {
        # ---------------------------------------------------------------
        # 剩余长度 varint
        # ---------------------------------------------------------------

        # 剩余长度编码后占几字节（1..4）；非法值（<0 或 >268435455）抛 Protocol
        public static Int32 remainingLengthSize( Int32 value ) throws
        {
            if value < 0 || value > 268435455
            {
                throw MqttError.Protocol
            }
            if value < 128
            {
                ret 1
            }
            if value < 16384
            {
                ret 2
            }
            if value < 2097152
            {
                ret 3
            }
            ret 4
        }

        # 剩余长度编码（每字节低 7 位、最高位延续、最多 4 字节）
        public static ByteBuffer encodeRemainingLength( Int32 value ) throws
        {
            if value < 0 || value > 268435455
            {
                throw MqttError.Protocol
            }
            ByteBuffer outBuf = ByteBuffer( 4 )
            Int32 v = value
            while true
            {
                Int32 b = v % 128
                v = v / 128
                if v > 0
                {
                    b = b + 128
                }
                outBuf.writeU8( SystemConvertUInt8( b ) )
                if v == 0
                {
                    break
                }
            }
            ret outBuf
        }

        # 剩余长度解码（从 buf 当前读位起）：
        #   >=0 成功（已消费 varint 字节）；-1 数据不足（回滚读位）；
        #   -2 非法（超过 4 字节上限，回滚读位）
        public static Int32 decodeRemainingLength( ByteBuffer buf ) throws
        {
            Int32 startPos = buf.readerIndex
            Int32 value = 0
            Int32 mul = 1
            Int32 count = 0
            while true
            {
                if buf.readableBytes < 1
                {
                    buf.readerIndex = startPos
                    ret -1
                }
                Int32 b = buf.readU8()
                count = count + 1
                value = value + ( b % 128 ) * mul
                if ( b & 128 ) == 0
                {
                    break
                }
                if count == 4
                {
                    # 第 4 字节仍有延续位 -> 超过 4 字节上限
                    buf.readerIndex = startPos
                    ret -2
                }
                mul = mul * 128
            }
            ret value
        }

        # ---------------------------------------------------------------
        # MQTT 字符串（U16 长度前缀 + UTF-8）
        # ---------------------------------------------------------------

        # 写入 U16 前缀 + UTF-8 字节（长度天然按字节计）
        static void _writeMqttString( ByteBuffer outBuf, string text )
        {
            ByteBuffer tmp = ByteBuffer.fromString( text )
            UInt16 v = tmp.readableBytes
            outBuf.writeU16Be( v )
            outBuf.writeBytes( tmp )
            tmp.release()
        }

        # 读取 U16 前缀 + UTF-8 字节；长度前缀或字节数不足抛 Protocol
        public static string readMqttString( ByteBuffer buf ) throws
        {
            if buf.readableBytes < 2
            {
                throw MqttError.Protocol
            }
            UInt16 len16 = buf.readU16Be()
            Int32 len = len16
            if buf.readableBytes < len
            {
                throw MqttError.Protocol
            }
            ret buf.readString( len )
        }

        # ---------------------------------------------------------------
        # 组帧：固定头第 1 字节 + 剩余长度 + body（body 归属本方法，消费后释放）
        # ---------------------------------------------------------------

        static ByteBuffer _frame( Int32 ptype, Int32 flags, ByteBuffer body ) throws
        {
            Int32 bodyLen = 0
            if body != null
            {
                bodyLen = body.readableBytes
            }
            ByteBuffer outBuf = ByteBuffer( 5 + bodyLen )
            outBuf.writeU8( SystemConvertUInt8( ptype * 16 + flags ) )
            ByteBuffer rl = MqttCodec.encodeRemainingLength( bodyLen )
            outBuf.writeBytes( rl )
            rl.release()
            if bodyLen > 0
            {
                outBuf.writeBytes( body )
            }
            body.release()
            ret outBuf
        }

        # ---------------------------------------------------------------
        # 各控制包编码
        # ---------------------------------------------------------------

        # CONNECT（MQTT 3.1.1：协议名 MQTT / level 4）
        public static ByteBuffer encodeConnect( MqttConnectOptions opt ) throws
        {
            ByteBuffer body = ByteBuffer( 64 )
            MqttCodec._writeMqttString( body, "MQTT" )
            body.writeU8( SystemConvertUInt8( 4 ) )
            Int32 flags = 0
            if opt.cleanSession
            {
                flags = flags | 2
            }
            if SystemStringLength( opt.username ) > 0
            {
                flags = flags | 128
            }
            if SystemStringLength( opt.password ) > 0
            {
                flags = flags | 64
            }
            body.writeU8( SystemConvertUInt8( flags ) )
            UInt16 ka = opt.keepAliveSeconds
            body.writeU16Be( ka )
            MqttCodec._writeMqttString( body, opt.clientId )
            if SystemStringLength( opt.username ) > 0
            {
                MqttCodec._writeMqttString( body, opt.username )
            }
            if SystemStringLength( opt.password ) > 0
            {
                MqttCodec._writeMqttString( body, opt.password )
            }
            ret MqttCodec._frame( MqttPacketType.Connect, 0, body )
        }

        # PUBLISH：flags = qos*2 + retain（DUP 恒 0）；payload 经 slice() 视图
        # 零拷贝写入（视图索引独立，不推进源）
        public static ByteBuffer encodePublish( string topic, ByteBuffer payload, Int32 qos, Int32 packetId, bool retain ) throws
        {
            if qos < 0 || qos > 2
            {
                throw MqttError.Protocol
            }
            Int32 payloadLen = 0
            if payload != null
            {
                payloadLen = payload.readableBytes
            }
            ByteBuffer body = ByteBuffer( 6 + SystemStringLength( topic ) + payloadLen )
            MqttCodec._writeMqttString( body, topic )
            if qos > 0
            {
                UInt16 pid = packetId
                body.writeU16Be( pid )
            }
            if payloadLen > 0
            {
                body.writeBytes( payload.slice() )
            }
            Int32 flags = qos * 2
            if retain
            {
                flags = flags | 1
            }
            ret MqttCodec._frame( MqttPacketType.Publish, flags, body )
        }

        # SUBSCRIBE（固定头 flags 固定 0b0010）；body = packetId + 主题过滤器 + QoS
        public static ByteBuffer encodeSubscribe( Int32 packetId, string topic, Int32 qos ) throws
        {
            ByteBuffer body = ByteBuffer( 8 + SystemStringLength( topic ) )
            UInt16 pid = packetId
            body.writeU16Be( pid )
            MqttCodec._writeMqttString( body, topic )
            body.writeU8( SystemConvertUInt8( qos ) )
            ret MqttCodec._frame( MqttPacketType.Subscribe, 2, body )
        }

        # UNSUBSCRIBE（固定头 flags 固定 0b0010）；body = packetId + 主题过滤器
        public static ByteBuffer encodeUnsubscribe( Int32 packetId, string topic ) throws
        {
            ByteBuffer body = ByteBuffer( 6 + SystemStringLength( topic ) )
            UInt16 pid = packetId
            body.writeU16Be( pid )
            MqttCodec._writeMqttString( body, topic )
            ret MqttCodec._frame( MqttPacketType.Unsubscribe, 2, body )
        }

        # PINGREQ（无载荷，固定 2 字节 c000）
        public static ByteBuffer encodePingReq()
        {
            ByteBuffer outBuf = ByteBuffer( 2 )
            outBuf.writeU8( SystemConvertUInt8( 12 * 16 ) )
            outBuf.writeU8( SystemConvertUInt8( 0 ) )
            ret outBuf
        }

        # PINGRESP（无载荷，固定 2 字节 d000）
        public static ByteBuffer encodePingResp()
        {
            ByteBuffer outBuf = ByteBuffer( 2 )
            outBuf.writeU8( SystemConvertUInt8( 13 * 16 ) )
            outBuf.writeU8( SystemConvertUInt8( 0 ) )
            ret outBuf
        }

        # DISCONNECT（无载荷，固定 2 字节 e000）
        public static ByteBuffer encodeDisconnect()
        {
            ByteBuffer outBuf = ByteBuffer( 2 )
            outBuf.writeU8( SystemConvertUInt8( 14 * 16 ) )
            outBuf.writeU8( SystemConvertUInt8( 0 ) )
            ret outBuf
        }

        # PUBACK（type=4）/ UNSUBACK（type=11）通用应答：body = packetId
        public static ByteBuffer encodeAck( Int32 ptype, Int32 packetId ) throws
        {
            ByteBuffer body = ByteBuffer( 2 )
            UInt16 pid = packetId
            body.writeU16Be( pid )
            ret MqttCodec._frame( ptype, 0, body )
        }

        # CONNACK（服务端用）：sessionPresent + returnCode
        public static ByteBuffer encodeConnack( bool sessionPresent, Int32 returnCode ) throws
        {
            ByteBuffer body = ByteBuffer( 2 )
            Int32 sp = 0
            if sessionPresent
            {
                sp = 1
            }
            body.writeU8( SystemConvertUInt8( sp ) )
            body.writeU8( SystemConvertUInt8( returnCode ) )
            ret MqttCodec._frame( MqttPacketType.Connack, 0, body )
        }

        # SUBACK（服务端用）：packetId + 授予 QoS（单主题）
        public static ByteBuffer encodeSuback( Int32 packetId, Int32 grantedQos ) throws
        {
            ByteBuffer body = ByteBuffer( 3 )
            UInt16 pid = packetId
            body.writeU16Be( pid )
            body.writeU8( SystemConvertUInt8( grantedQos ) )
            ret MqttCodec._frame( MqttPacketType.Suback, 0, body )
        }

        # ---------------------------------------------------------------
        # 解析
        # ---------------------------------------------------------------

        # 从 buf 当前读位起解析一个完整包：
        #   非 null 成功（已消费对应字节，payload 为独立拷贝）；
        #   null 数据不足（读位回滚，等更多数据）；
        #   非法（type==0 / 剩余长度超限）抛 Protocol 并回滚
        public static _MqttPacket parsePacket( ByteBuffer buf ) throws
        {
            if buf.readableBytes < 2
            {
                ret null
            }
            Int32 startPos = buf.readerIndex
            Int32 h0 = buf.readU8()
            Int32 ptype = h0 / 16
            Int32 flags = h0 % 16
            if ptype == 0
            {
                buf.readerIndex = startPos
                throw MqttError.Protocol
            }
            Int32 rl = MqttCodec.decodeRemainingLength( buf )
            if rl == -1
            {
                buf.readerIndex = startPos
                ret null
            }
            if rl == -2
            {
                buf.readerIndex = startPos
                throw MqttError.Protocol
            }
            if buf.readableBytes < rl
            {
                buf.readerIndex = startPos
                ret null
            }
            _MqttPacket pkt = _MqttPacket()
            pkt.type = ptype
            pkt.flags = flags
            if rl > 0
            {
                ByteBuffer payload = ByteBuffer( rl )
                buf.readBytes( payload, rl )
                pkt.payload = payload
            }
            else
            {
                pkt.payload = ByteBuffer( 0 )
            }
            ret pkt
        }

        # 从流读一个完整包（buf 为残留缓冲，_fill 补数据）；
        # 返回 null = 对端关闭（EOF）
        public static _MqttPacket readPacket( NetStream s, ByteBuffer buf ) throws
        {
            while true
            {
                _MqttPacket pkt = MqttCodec.parsePacket( buf )
                if pkt != null
                {
                    ret pkt
                }
                # 至少再补 1 字节（_fill 内部按需压缩/扩容）
                bool ok = MqttCodec._fill( s, buf, buf.readableBytes + 1 )
                if !ok
                {
                    ret null
                }
            }
        }

        # 解析 PUBLISH 载荷为 MqttMessage（载荷为独立拷贝，
        # 调用方随后应对原包调 release）
        public static MqttMessage parsePublish( _MqttPacket pkt ) throws
        {
            if pkt.type != MqttPacketType.Publish
            {
                throw MqttError.Protocol
            }
            Int32 qos = ( pkt.flags / 2 ) % 4
            if qos == 3
            {
                throw MqttError.Protocol
            }
            bool retain = false
            if ( pkt.flags & 1 ) == 1
            {
                retain = true
            }
            if pkt.payload == null
            {
                throw MqttError.Protocol
            }
            string topic = MqttCodec.readMqttString( pkt.payload )
            Int32 packetId = 0
            if qos > 0
            {
                if pkt.payload.readableBytes < 2
                {
                    throw MqttError.Protocol
                }
                UInt16 pid = pkt.payload.readU16Be()
                packetId = pid
            }
            Int32 restLen = pkt.payload.readableBytes
            ByteBuffer payload = ByteBuffer( restLen )
            if restLen > 0
            {
                pkt.payload.readBytes( payload, restLen )
            }
            ret MqttMessage( topic, payload, qos, packetId, retain )
        }

        # 补数据直到 buf 可读字节 >= need；返回 false = 对端关闭（EOF）。
        # 满则先压缩再按需扩容（Websocket._fill 同款）
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
    # MqttClient — MQTT 3.1.1 客户端（QoS 0 / 1）
    # ============================================================================

    public class MqttClient
    {
        NetStream _stream = null
        ByteBuffer _recvBuf = null
        List<MqttMessage> _pending = List<MqttMessage>( 4 )
        Int32 _nextPacketId = 1
        bool _connected = false

        public get bool isConnected()
        {
            ret this._connected
        }

        # ---------------------------------------------------------------
        # 接入
        # ---------------------------------------------------------------

        public static MqttClient connect( string host, Int32 port ) throws
        {
            MqttConnectOptions opt = MqttConnectOptions()
            ret MqttClient.connect( host, port, opt )
        }

        public static MqttClient connect( string host, Int32 port, MqttConnectOptions opt ) throws
        {
            TcpStream raw = Tcp.connectTimeout( host, port, opt.connectTimeoutMs )
            raw.setReadTimeout( opt.connectTimeoutMs )
            MqttClient client = MqttClient()
            client._stream = raw
            client._recvBuf = ByteBuffer()
            # 注意：label{}...catch{} 中 label 块本身就是受 catch 保护的语句块
            #   （Websocket / HttpClient 先例），块内直接写调用即可
            label connBlock
            {
                client._connectHandshake( opt )
            }
            catch
            {
                try raw.close()
                client._stream = null
                client._recvBuf = null
                throw
            }
            client._connected = true
            # 握手完成，恢复无限期阻塞读
            raw.setReadTimeout( 0 )
            ret client
        }

        # 握手：发 CONNECT -> 等 CONNACK；returnCode != 0 抛 ConnackRejected
        void _connectHandshake( MqttConnectOptions opt ) throws
        {
            ByteBuffer frame = MqttCodec.encodeConnect( opt )
            this._stream.write( frame )
            frame.release()
            _MqttPacket pkt = MqttCodec.readPacket( this._stream, this._recvBuf )
            if pkt == null
            {
                throw MqttError.Closed
            }
            if pkt.type != MqttPacketType.Connack
            {
                pkt.release()
                throw MqttError.Protocol
            }
            if pkt.payload == null || pkt.payload.readableBytes < 2
            {
                pkt.release()
                throw MqttError.Protocol
            }
            pkt.payload.readU8()
            Int32 returnCode = pkt.payload.readU8()
            pkt.release()
            if returnCode != 0
            {
                throw MqttError.ConnackRejected
            }
        }

        # ---------------------------------------------------------------
        # 收发
        # ---------------------------------------------------------------

        # 收一条 PUBLISH（null = 对端 DISCONNECT 或断开）；
        # QoS 1 自动回 PUBACK；先弹 _waitPacket 期间缓存的消息
        public MqttMessage receive() throws
        {
            if this._pending.length > 0
            {
                MqttMessage head = this._pending._getItem_( 0 )
                this._pending.removeAt( 0 )
                ret head
            }
            if !this._connected
            {
                throw MqttError.Closed
            }
            while true
            {
                _MqttPacket pkt = MqttCodec.readPacket( this._stream, this._recvBuf )
                if pkt == null
                {
                    ret null
                }
                if pkt.type == MqttPacketType.Publish
                {
                    MqttMessage msg = MqttCodec.parsePublish( pkt )
                    pkt.release()
                    this._ackPublish( msg )
                    ret msg
                }
                if pkt.type == MqttPacketType.Disconnect
                {
                    pkt.release()
                    this._connected = false
                    ret null
                }
                # 其余（PUBACK / PINGRESP 等）释放忽略
                pkt.release()
            }
        }

        # 发布文本（QoS 0）
        public void publish( string topic, string text ) throws
        {
            this.publish( topic, text, 0 )
        }

        # 发布文本（qos 仅支持 0 / 1；QoS 1 同步等 PUBACK）
        public void publish( string topic, string text, Int32 qos ) throws
        {
            if qos < 0 || qos > 1
            {
                throw MqttError.Protocol
            }
            if !this._connected
            {
                throw MqttError.Closed
            }
            ByteBuffer payload = ByteBuffer.fromString( text )
            Int32 packetId = 0
            if qos > 0
            {
                packetId = this._allocPacketId()
            }
            ByteBuffer frame = MqttCodec.encodePublish( topic, payload, qos, packetId, false )
            payload.release()
            this._stream.write( frame )
            frame.release()
            if qos > 0
            {
                _MqttPacket ack = this._waitPacket( MqttPacketType.PubAck )
                if ack == null
                {
                    throw MqttError.Closed
                }
                ack.release()
            }
        }

        # 订阅主题过滤器，返回服务端授予的 QoS
        public Int32 subscribe( string topic, Int32 qos ) throws
        {
            if !this._connected
            {
                throw MqttError.Closed
            }
            Int32 packetId = this._allocPacketId()
            ByteBuffer frame = MqttCodec.encodeSubscribe( packetId, topic, qos )
            this._stream.write( frame )
            frame.release()
            _MqttPacket ack = this._waitPacket( MqttPacketType.Suback )
            if ack == null
            {
                throw MqttError.Closed
            }
            if ack.payload == null || ack.payload.readableBytes < 3
            {
                ack.release()
                throw MqttError.Protocol
            }
            ack.payload.readU16Be()
            Int32 granted = ack.payload.readU8()
            ack.release()
            ret granted
        }

        # 退订主题过滤器（同步等 UNSUBACK）
        public void unsubscribe( string topic ) throws
        {
            if !this._connected
            {
                throw MqttError.Closed
            }
            Int32 packetId = this._allocPacketId()
            ByteBuffer frame = MqttCodec.encodeUnsubscribe( packetId, topic )
            this._stream.write( frame )
            frame.release()
            _MqttPacket ack = this._waitPacket( MqttPacketType.Unsuback )
            if ack == null
            {
                throw MqttError.Closed
            }
            ack.release()
        }

        # 心跳（同步等 PINGRESP）
        public void ping() throws
        {
            if !this._connected
            {
                throw MqttError.Closed
            }
            ByteBuffer frame = MqttCodec.encodePingReq()
            this._stream.write( frame )
            frame.release()
            _MqttPacket ack = this._waitPacket( MqttPacketType.PingResp )
            if ack == null
            {
                throw MqttError.Closed
            }
            ack.release()
        }

        # 主动断开：尽力发 DISCONNECT -> 关流 -> 复位；任一步失败不抛
        public void disconnect()
        {
            if this._stream != null
            {
                if this._connected
                {
                    label sendBlock
                    {
                        ByteBuffer frame = MqttCodec.encodeDisconnect()
                        this._stream.write( frame )
                        frame.release()
                    }
                    catch
                    {
                        # 尽力发送，失败忽略
                    }
                }
                try this._stream.close()
            }
            this._stream = null
            this._recvBuf = null
            this._connected = false
            while this._pending.length > 0
            {
                MqttMessage m = this._pending._getItem_( 0 )
                this._pending.removeAt( 0 )
                if m != null
                {
                    m.release()
                }
            }
        }

        # ---------------------------------------------------------------
        # 内部
        # ---------------------------------------------------------------

        # 等待指定类型包；途中收到的 PUBLISH 解析入队（稍后 receive 弹出），
        # 其余包释放忽略；对端断开 / DISCONNECT 返回 null
        _MqttPacket _waitPacket( Int32 wantType ) throws
        {
            while true
            {
                _MqttPacket pkt = MqttCodec.readPacket( this._stream, this._recvBuf )
                if pkt == null
                {
                    ret null
                }
                if pkt.type == wantType
                {
                    ret pkt
                }
                if pkt.type == MqttPacketType.Publish
                {
                    MqttMessage msg = MqttCodec.parsePublish( pkt )
                    pkt.release()
                    this._ackPublish( msg )
                    this._pending.add( msg )
                }
                elif pkt.type == MqttPacketType.Disconnect
                {
                    pkt.release()
                    this._connected = false
                    ret null
                }
                else
                {
                    pkt.release()
                }
            }
        }

        # QoS 1 消息自动回 PUBACK
        void _ackPublish( MqttMessage msg ) throws
        {
            if msg.qos == 1
            {
                ByteBuffer ack = MqttCodec.encodeAck( MqttPacketType.PubAck, msg.packetId )
                this._stream.write( ack )
                ack.release()
            }
        }

        # 报文标识符分配（1..65535 循环）
        Int32 _allocPacketId()
        {
            Int32 id = this._nextPacketId
            this._nextPacketId = this._nextPacketId + 1
            if this._nextPacketId > 65535
            {
                this._nextPacketId = 1
            }
            ret id
        }
    }
}
