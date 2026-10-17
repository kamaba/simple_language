namespace Net
{
    # ============================================================================
    # Net/TlsStream.sl — TLS 加密流（TcpStream + mbedTLS 装饰器）
    # 设计契约：csimple_lang/md/design/STREAM_DESIGN.md §9.5 / §15.8 P3
    #
    # 用法:
    #   import Std;
    #   var opt  = Net.TlsOptions()                       # 默认：无 CA = 跳过验证
    #   opt.caPem = caText                                # 显式 CA（PEM 文本）
    #   opt.hostname = "example.com"                      # SNI + 主机名校验
    #   var raw  = Net.Tcp.connect( "example.com", 443 )  # 先建明文 TCP
    #   var tls  = Net.TlsStream.wrap( raw, opt )         # 客户端握手（挂起透明）
    #   # 服务端侧：var tls = Net.TlsStream.accept( client, serverOpt )
    #
    # 语义要点:
    #   - 装饰器语义：canSeek=false / isDuplex=true，背压继承底层 TCP
    #   - 握手挂起透明：SystemTlsHandshake 方向交替（待可读 / 待可写），
    #     C VM 侧挂起当前协程（Option A 指令重执行），SL 侧只见终局 0/-1
    #   - 读写超时复用底层 TCP 锚点：setReadTimeout / setWriteTimeout 经
    #     this._sid（即底层 TCP sid）作用，与 TLS 挂起等待提交的 sid 一致
    #   - wrap / accept 成功后底层 TcpStream 所有权转移，勿再直接操作
    #   - 失败路径：销毁 TLS 会话（先）+ 关闭底层连接（后），半程握手后
    #     连接状态已污染，不可复用明文重试
    #   - 设计稿 §9.5 记 static Task wrap/accept，落地为直接返回 TlsStream
    #     （挂起透明，对齐 Tcp.connect 风格，无需 Task 包装）
    # ============================================================================

    # TLS 错误码（code 位镜像 C VM sys_tls.h 的 LIBSYS_TLS_ERR_* 1-9 段；
    # 与 NetError 相互独立：TCP 层错误抛 NetError，TLS 层错误抛 TlsError）
    public enum TlsError extends Error
    {
        # 1 TLS 库初始化失败
        Init = { code = 1 }
        # 2 参数无效（句柄失效 / 证书组合不完整等）
        BadParam = { code = 2 }
        # 3 内存不足
        Alloc = { code = 3 }
        # 4 证书 / 私钥 PEM 解析失败
        Parse = { code = 4 }
        # 5 证书链验证失败 / 主机名不匹配
        Verify = { code = 5 }
        # 6 TLS 握手失败（协议错误 / 对端拒绝）
        Handshake = { code = 6 }
        # 7 会话已关闭
        Closed = { code = 7 }
        # 8 底层读写失败
        IoError = { code = 8 }
        # 9 挂起等待超时（复用底层 TCP 读 / 写超时窗口）
        Timeout = { code = 9 }
    }

    # ============================================================================
    # TlsOptions — TLS 会话参数（可变配置对象，字段直接赋值）
    # ============================================================================

    public class TlsOptions
    {
        # CA 证书链 PEM 文本（用于验证对端）。空串 = 跳过验证
        # （信任所有对端，仅建议自签 / 内网测试场景使用）
        string caPem = ""
        # 本端证书 PEM 文本（服务端必填；客户端双向认证时提供）
        string certPem = ""
        # 本端私钥 PEM 文本（与 certPem 配对）
        string keyPem = ""
        # 主机名（客户端：SNI 发送 + 证书主机名校验；空串 = 不发送也不校验）
        string hostname = ""
    }

    # ============================================================================
    # TlsStream — TLS 双向流（一般经 wrap / accept 获取，勿直接构造）
    # ============================================================================

    public class TlsStream extends NetStream
    {
        TcpSocket _socket = null
        Int64 _tlsSid = 0

        # raw 的所有权转移到本流（_socket / _sid 均从 raw 接管）
        _init_( TcpStream raw, Int64 tlsSid )
        {
            this._socket = raw._socket
            this._tlsSid = tlsSid
            this._sid = raw._sid
            this._connected = true
            this._canSeek = false
            this._canTimeout = false
            this._isDuplex = true
        }

        # 客户端握手：在已连接的 TcpStream 上发起 TLS（证书 / SNI / 主机名校验）
        public static TlsStream wrap( TcpStream raw, TlsOptions opt ) throws
        {
            ret TlsStream._handshake( raw, opt, false )
        }

        # 服务端握手：在已 accept 的 TcpStream 上等待并完成 TLS
        # （opt.certPem / opt.keyPem 提供本端证书）
        public static TlsStream accept( TcpStream raw, TlsOptions opt ) throws
        {
            ret TlsStream._handshake( raw, opt, true )
        }

        # 会话创建 + 握手（挂起由 C VM 侧透明处理，此处只见终局）
        static TlsStream _handshake( TcpStream raw, TlsOptions opt, bool isServer ) throws
        {
            if raw == null || raw._sid == 0
            {
                throw TlsError.Closed
            }
            Int64 tlsSid = SystemTlsSessionNew( raw._sid, isServer, opt.caPem, opt.certPem, opt.keyPem, opt.hostname )
            if tlsSid == 0
            {
                # 创建失败（证书解析 / 参数 / 内存）：底层连接一并关闭
                raw.close()
                Int32 e = SystemTlsLastError()
                if e == 4
                {
                    throw TlsError.Parse
                }
                if e == 5
                {
                    throw TlsError.Verify
                }
                if e == 2
                {
                    throw TlsError.BadParam
                }
                throw TlsError.Init
            }
            Int32 r = SystemTlsHandshake( tlsSid )
            if r != 0
            {
                # 握手失败 / 超时：先销毁 TLS 会话（发 close_notify）再关底层连接
                SystemTlsClose( tlsSid )
                raw.close()
                Int32 e = SystemTlsLastError()
                if e == 9
                {
                    throw TlsError.Timeout
                }
                if e == 5
                {
                    throw TlsError.Verify
                }
                if e == 7
                {
                    throw TlsError.Closed
                }
                throw TlsError.Handshake
            }
            ret TlsStream( raw, tlsSid )
        }

        # 解密读：写入 dst 可写区；返回 0 = EOF（对端关闭）置位 _eof
        override public Int32 read( ByteBuffer dst ) throws
        {
            this._ensureReadable()
            if !this._connected || this._eof
            {
                ret 0
            }
            if dst.writableBytes <= 0
            {
                ret 0
            }
            Int32 n = SystemTlsRecv( this._tlsSid, dst.handle )
            if n < 0
            {
                Int32 e = SystemTlsLastError()
                if e == 9
                {
                    throw TlsError.Timeout
                }
                if e == 7
                {
                    throw TlsError.Closed
                }
                throw TlsError.IoError
            }
            if n == 0
            {
                this._eof = true
            }
            ret n
        }

        # 加密写：全量写出（写背压由 C 层挂起透明处理）
        override public void write( ByteBuffer src ) throws
        {
            this._ensureWritable()
            while src.readableBytes > 0
            {
                Int32 n = SystemTlsSend( this._tlsSid, src.handle )
                if n < 0
                {
                    Int32 e = SystemTlsLastError()
                    if e == 9
                    {
                        throw TlsError.Timeout
                    }
                    if e == 7
                    {
                        throw TlsError.Closed
                    }
                    throw TlsError.IoError
                }
                if n == 0
                {
                    ret
                }
            }
        }

        override public void flush() throws
        {
            # mbedTLS 记录直写内核，无用户态缓冲，flush 为空操作
        }

        # 不覆写 closeRead / closeWrite：TLS 的 close_notify 是双向收尾，
        # 无半关闭 syscall（基类默认仅置位封口，不动底层会话）

        # 完全覆写：先销毁 TLS 会话（向对端发 close_notify），再关底层 TCP
        override public void close() throws
        {
            if this._tlsSid != 0
            {
                SystemTlsClose( this._tlsSid )
                this._tlsSid = 0
            }
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
