namespace Net
{
    # ============================================================================
    # Net/NetStream.sl — 网络流（L0 字节层 ByteStream 的网络子类）
    # 设计契约：md/design/STREAM_DESIGN.md §4.5 / csimple_lang/md/design/NETSTREAM_DESIGN.md §6.5
    #
    # 语义要点：
    #   - NetStream 抽象基类：socket 句柄 + 连接态 + 对端地址查询 / NoDelay 直通
    #   - TcpStream 双向流：read 返回 0 = EOF（对端关闭）置位 _eof；
    #     closeRead / closeWrite 映射 shutdown 半关闭；
    #     close 完全覆写（直接释放句柄，不走基类 closeWrite -> flush 链）
    #   - UdpStream 报文流：_canWrite = false（无固定对端，定向发送走 sendTo）、
    #     _isMessageOriented = true；datagrams() 引流协程把数据报灌进 Stream
    #
    # 已知偏差（详见 NETSTREAM_DESIGN.md §6.8 差异清单）：
    #   - Phase 1 无 connect(对端) 语义：UdpStream.write 仅守卫（抛 NotSupported）
    #   - 空数据报使 read 返回 0（与 ByteStream 的 EOF 语义共用 0 值）
    #   - SL 子类构造不强制调父类 _init_：能力位在子类构造体内显式置齐
    #     （FileStream / MemoryStream 同款），NetStream 自身不定义构造
    # ============================================================================

    # ============================================================================
    # NetStream — 网络流抽象基类
    # ============================================================================

    public abstract class NetStream extends ByteStream
    {
        Int64 _sid = 0
        bool _connected = false

        public get bool isConnected()
        {
            ret this._connected
        }

        # 对端地址（UDP 无 connect 对端时 C 侧返回空串 / 0）
        public get string remoteAddress()
        {
            if this._sid == 0
            {
                ret ""
            }
            ret SystemTcpRemoteAddress( this._sid )
        }

        public get Int32 remotePort()
        {
            if this._sid == 0
            {
                ret 0
            }
            ret SystemTcpRemotePort( this._sid )
        }

        # socket 选项直通（UDP 套接字上无效但无害）
        public void setNoDelay( bool enabled )
        {
            if this._sid != 0
            {
                SystemTcpSetNoDelay( this._sid, enabled )
            }
        }

        # 读超时毫秒透传（<= 0 = 取消超时，无限等待；超时窗口到期后
        # recv / recvFrom 抛 NetError.Timeout，连接保持可用）
        public void setReadTimeout( Int32 timeoutMs )
        {
            if this._sid != 0
            {
                SystemNetSetReadTimeout( this._sid, timeoutMs )
            }
        }

        # 写超时毫秒透传（<= 0 = 取消超时，无限等待；仅 TCP send 挂起路径生效，
        # UDP sendTo 不挂起不受影响）
        public void setWriteTimeout( Int32 timeoutMs )
        {
            if this._sid != 0
            {
                SystemNetSetWriteTimeout( this._sid, timeoutMs )
            }
        }
    }

    # ============================================================================
    # TcpStream — TCP 双向流
    # ============================================================================

    public class TcpStream extends NetStream
    {
        TcpSocket _socket = null

        _init_( TcpSocket socket )
        {
            this._socket = socket
            this._sid = socket._sid
            this._connected = true
            this._canSeek = false
            this._canTimeout = false
            this._isDuplex = true
        }

        # 非阻塞读：写入 dst 可写区；返回 0 = EOF（对端关闭）置位 _eof
        override public Int32 read( ByteBuffer dst ) throws
        {
            this._ensureReadable()
            if !this._connected || this._eof
            {
                ret 0
            }
            Int32 n = this._socket.recv( dst )
            if n == 0
            {
                this._eof = true
            }
            ret n
        }

        # 全量写出（写背压由 C 层挂起透明处理）
        override public void write( ByteBuffer src ) throws
        {
            this._ensureWritable()
            this._socket.send( src )
        }

        override public void flush() throws
        {
            # TCP 无用户态缓冲，flush 为空操作
        }

        # 半关闭：停收（对端再写会收到连接重置）
        override public void closeRead() throws
        {
            if !this._closedRead
            {
                this._socket.shutdown( SocketShutdown.Read )
                this._closedRead = true
            }
        }

        # 半关闭：停发（先冲刷再 shutdown，对端 read 将见 EOF）
        override public void closeWrite() throws
        {
            if !this._closedWrite
            {
                this.flush()
                this._socket.shutdown( SocketShutdown.Write )
                this._closedWrite = true
            }
        }

        # 完全覆写：直接释放句柄并封口两个方向
        #（不走基类 close -> closeWrite -> flush 链，FileStream 同款）
        override public void close() throws
        {
            if this._socket != null
            {
                this._socket.close()
                this._socket = null
            }
            this._sid = 0
            this._connected = false
            this._closedRead = true
            this._closedWrite = true
        }
    }

    # ============================================================================
    # UdpStream — UDP 报文流
    # ============================================================================

    public class UdpStream extends NetStream
    {
        UdpSocket _socket = null

        # 报文语义：无固定对端（_canWrite = false，定向发送走 sendTo）
        _init_( UdpSocket socket )
        {
            this._socket = socket
            this._sid = socket._sid
            this._connected = true
            this._canSeek = false
            this._canTimeout = false
            this._isDuplex = true
            this._canWrite = false
            this._isMessageOriented = true
        }

        # 数据报读取：一次 read 收一报写入 dst（超出 dst 容量的尾部由 C 侧丢弃）
        override public Int32 read( ByteBuffer dst ) throws
        {
            this._ensureReadable()
            ret this._socket.recvFrom( dst )
        }

        # Phase 1 无 connect(对端) 语义：固定对端写入不支持（守卫抛 NotSupported）
        override public void write( ByteBuffer src ) throws
        {
            this._ensureWritable()
        }

        override public void flush() throws
        {
            # UDP 无用户态缓冲，flush 为空操作
        }

        # 定向发送（UDP 专属，不经 write 的固定对端守卫）
        public void sendTo( ByteBuffer src, string address, Int32 port ) throws
        {
            if this._socket == null
            {
                throw NetError.Closed
            }
            this._socket.sendTo( src, address, port )
        }

        # 数据报流（Stream 体系直连）：引流协程循环收报灌进 controller，
        # 消费方经 Stream<UdpDatagram> 逐报读取；close / 底层出错时 ctrl.close() 收尾。
        # 闭包内直接走系统调用（系统调用不抛异常，哨兵值判定），
        # 不经 this.read（throws 语义在闭包内不可用）。
        public Stream<UdpDatagram> datagrams()
        {
            StreamController<UdpDatagram> ctrl = StreamController<UdpDatagram>()
            # 闭包捕获限制：this._x 一律先拷局部再进闭包（Stream.sl done 同款模式）
            UdpSocket sock = this._socket
            Int64 sid = this._sid
            function f = function()
            {
                # 退出机制：close 时 C 侧取消在途等待 -> 重执行的 recv 走错误返回
                # -> n < 0 自然退出（vm_net_cancel_wait 设计路径）
                while true
                {
                    # 每轮新缓冲：UdpDatagram 持引用，复用同一 buf 会被后续写入污染
                    ByteBuffer buf = ByteBuffer( 65536 )
                    Int32 n = SystemUdpRecvFrom( sid, buf.handle )
                    if n < 0
                    {
                        break
                    }
                    if n > 0
                    {
                        ctrl.add( UdpDatagram( buf, sock.lastFromAddress, sock.lastFromPort ) )
                    }
                }
                ctrl.close()
            }
            Task task = Coroutine.spawnClosure0( f )
            ret ctrl.stream
        }

        # 完全覆写：直接释放句柄并封口两个方向（TcpStream 同款）
        override public void close() throws
        {
            if this._socket != null
            {
                this._socket.close()
                this._socket = null
            }
            this._sid = 0
            this._connected = false
            this._closedRead = true
            this._closedWrite = true
        }
    }
}
