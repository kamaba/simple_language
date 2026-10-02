namespace Net
{
    # ============================================================================
    # Net/TcpSocket.sl — TCP 客户端 / 服务端
    # 设计契约：csimple_lang/md/design/NETSTREAM_DESIGN.md §6.2-§6.4
    #
    # 用法:
    #   import Std;
    #   var conn = Net.Tcp.connect( "127.0.0.1", 9000 )     # 客户端连接（默认 10s 超时）
    #   var server = Net.Tcp.listen( 9000 )                   # 服务端监听
    #   var client = server.accept()                           # 挂起直到新连接
    #   server.onConnection( function( c ) { ... } )          # 回调形态（协程组合模拟）
    #
    # 语义要点（与设计文档对应）:
    #   - 挂起协议 Option A：ConnectWait / Accept / Recv / Send 在 C VM 侧
    #     挂起当前协程，fd 就绪后指令重执行自行完成数据读写
    #   - 失败 = 哨兵值 + SystemNetLastError 可查，SL 层负责 throw
    #   - recv 返回 0 = EOF（对端关闭）；send 全量写出才返回
    # ============================================================================

    # 网络错误码（code 位镜像 C VM sys_net.h 的 LIBSYS_NET_ERR_* 1-9 段；
    # 码位 3 = HOST_NOT_FOUND，Tcp.connectTimeout 据此细分 HostNotFound）
    public enum NetError extends Error
    {
        # 1 socket 创建失败
        SocketCreate = { code = 1 }
        # 2 连接失败（握手被拒 / 中途断开）
        ConnectFailed = { code = 2 }
        # 3 主机名解析失败（对应 C 侧 HOST_NOT_FOUND）
        HostNotFound = { code = 3 }
        # 4 连接超时
        Timeout = { code = 4 }
        # 5 UDP 绑定失败（端口占用等）
        BindFailed = { code = 5 }
        # 6 监听失败
        ListenFailed = { code = 6 }
        # 7 接受连接失败
        AcceptFailed = { code = 7 }
        # 8 句柄已关闭
        Closed = { code = 8 }
        # 9 底层读写失败
        IoError = { code = 9 }
    }

    # shutdown 方向（裸值对齐 C 侧 shutdown how：0=停收 1=停发 2=双向）
    public enum SocketShutdown extends Int32
    {
        Read = 0
        Write = 1
        Both = 2
    }

    # ============================================================================
    # TcpSocket — 裸 socket 包装（一般经 Tcp / TcpServer 获取，勿直接构造）
    # ============================================================================

    public class TcpSocket
    {
        Int64 _sid = 0
        bool _open = false

        _init_( Int64 sid )
        {
            this._sid = sid
            this._open = sid != 0
        }

        public get bool isOpen()
        {
            ret this._open && this._sid != 0
        }

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

        public get string localAddress()
        {
            if this._sid == 0
            {
                ret ""
            }
            ret SystemTcpLocalAddress( this._sid )
        }

        public get Int32 localPort()
        {
            if this._sid == 0
            {
                ret 0
            }
            ret SystemTcpLocalPort( this._sid )
        }

        # 非阻塞 recv：写入 dst 可写区，返回本次读入字节数；0 = EOF（对端关闭）
        public Int32 recv( ByteBuffer dst ) throws
        {
            if !this.isOpen
            {
                throw NetError.Closed
            }
            if dst.writableBytes <= 0
            {
                ret 0
            }
            Int32 n = SystemTcpRecv( this._sid, dst.handle )
            if n < 0
            {
                throw NetError.IoError
            }
            ret n
        }

        # 全量发送：写出多少推进 src.readerIndex 多少（重挂起由 C 层透明处理）
        public void send( ByteBuffer src ) throws
        {
            if !this.isOpen
            {
                throw NetError.Closed
            }
            while src.readableBytes > 0
            {
                Int32 n = SystemTcpSend( this._sid, src.handle )
                if n < 0
                {
                    throw NetError.IoError
                }
                if n == 0
                {
                    ret
                }
            }
        }

        public void setNoDelay( bool enabled )
        {
            if this._sid != 0
            {
                SystemTcpSetNoDelay( this._sid, enabled )
            }
        }

        public void shutdown( SocketShutdown how )
        {
            # enum -> Int32 显式映射（SL 不支持 as，SeekOrigin 同款）
            Int32 h = 0
            if how == SocketShutdown.Write
            {
                h = 1
            }
            elif how == SocketShutdown.Both
            {
                h = 2
            }
            if this._sid != 0
            {
                SystemTcpShutdown( this._sid, h )
            }
        }

        # 关闭并释放句柄；C 侧会取消挂在该 sid 上的全部在途等待
        public void close()
        {
            if this._sid != 0
            {
                SystemTcpClose( this._sid )
                this._sid = 0
                this._open = false
            }
        }
    }

    # ============================================================================
    # Tcp — 外观类（SL 命名空间不能放裸函数 -> 静态工厂收口）
    # ============================================================================

    public class Tcp
    {
        public static TcpStream connect( string host, Int32 port ) throws
        {
            ret Tcp.connectTimeout( host, port, 10000 )
        }

        # 连接超时毫秒；失败码位细分：3 = 域名解析失败，其余按阶段归类
        public static TcpStream connectTimeout( string host, Int32 port, Int64 timeoutMs ) throws
        {
            Int64 sid = SystemTcpBeginConnect( host, port )
            if sid == 0
            {
                Int32 e = SystemNetLastError()
                if e == 3
                {
                    throw NetError.HostNotFound
                }
                throw NetError.SocketCreate
            }
            Int32 r = SystemTcpConnectWait( sid, timeoutMs )
            if r == 2
            {
                SystemTcpClose( sid )
                throw NetError.Timeout
            }
            if r != 0
            {
                SystemTcpClose( sid )
                throw NetError.ConnectFailed
            }
            ret TcpStream( TcpSocket( sid ) )
        }

        public static TcpServer listen( Int32 port ) throws
        {
            ret Tcp.listenAddress( "0.0.0.0", port )
        }

        public static TcpServer listenAddress( string host, Int32 port ) throws
        {
            Int64 sid = SystemTcpListen( host, port, 128 )
            if sid == 0
            {
                throw NetError.ListenFailed
            }
            ret TcpServer( sid )
        }

        # 裸 socket 升级为流（acceptSocket / 手动精细控制后接入 Stream 体系）
        public static TcpStream streamOf( TcpSocket socket ) throws
        {
            ret TcpStream( socket )
        }
    }

    # ============================================================================
    # TcpServer — 服务端监听器
    # ============================================================================

    public class TcpServer
    {
        Int64 _listenSid = 0

        _init_( Int64 listenSid )
        {
            this._listenSid = listenSid
        }

        # 挂起直到新连接（无连接时协程挂起，不阻塞 VM 线程）；
        # 接入连接默认开 NoDelay
        public TcpStream accept() throws
        {
            TcpSocket sock = this.acceptSocket()
            sock.setNoDelay( true )
            ret TcpStream( sock )
        }

        public TcpSocket acceptSocket() throws
        {
            Int64 sid = SystemTcpAccept( this._listenSid )
            if sid == 0
            {
                throw NetError.AcceptFailed
            }
            ret TcpSocket( sid )
        }

        # 回调形态（Phase 1 由协程组合模拟，设计 §6.7）：
        # fn 为单参闭包，参数为接入的 TcpStream；每个连接一条协程
        public void onConnection( Function fn )
        {
            while true
            {
                TcpStream client = this.accept()
                Coroutine.spawnClosure1( fn, client )
            }
        }

        public get Int32 port()
        {
            if this._listenSid == 0
            {
                ret 0
            }
            ret SystemTcpLocalPort( this._listenSid )
        }

        public void close()
        {
            if this._listenSid != 0
            {
                SystemTcpClose( this._listenSid )
                this._listenSid = 0
            }
        }
    }
}
