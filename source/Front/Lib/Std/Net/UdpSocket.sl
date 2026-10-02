namespace Net
{
    # ============================================================================
    # Net/UdpSocket.sl — UDP 套接字
    # 设计契约：csimple_lang/md/design/NETSTREAM_DESIGN.md §6.6
    #
    # 用法:
    #   import Std;
    #   var s = Net.Udp.bind( 9000 )                    # 绑定端口得 UdpStream
    #   s.sendTo( buf, "127.0.0.1", 9001 )              # 定向发送
    #   var n = s.read( dst )                           # 挂起收一报写入 dst
    #   var dgs = s.datagrams()                         # 报文流（Stream 体系直连）
    #
    # 语义要点（与设计文档对应）:
    #   - recvFrom 挂起直到有数据报（Option A 指令重执行），一次收一报写入
    #     dst 可写区，超出 dst 容量的报文尾部由 C 侧丢弃
    #   - 来源地址不随返回值带出：经 C 侧 last-from 槽位查询
    #     （lastFromAddress / lastFromPort）
    #   - sendTo 全量写出：写出多少推进 src.readerIndex 多少
    #   - 失败 = 哨兵值 + last_error 可查，SL 层负责 throw
    # ============================================================================

    # ============================================================================
    # UdpSocket — 裸 socket 包装（一般经 Udp.bind 获取，勿直接构造）
    # ============================================================================

    public class UdpSocket
    {
        Int64 _sid = 0

        _init_( Int64 sid )
        {
            this._sid = sid
        }

        # 挂起直到有数据报：一次收一报写入 dst 可写区，返回本报字节数；
        # 空数据报返回 0（UDP 合法报文，非错误）
        public Int32 recvFrom( ByteBuffer dst ) throws
        {
            if this._sid == 0
            {
                throw NetError.Closed
            }
            Int32 n = SystemUdpRecvFrom( this._sid, dst.handle )
            if n < 0
            {
                throw NetError.IoError
            }
            ret n
        }

        # 全量发送：写出多少推进 src.readerIndex 多少（重挂起由 C 层透明处理）
        public void sendTo( ByteBuffer src, string address, Int32 port ) throws
        {
            if this._sid == 0
            {
                throw NetError.Closed
            }
            while src.readableBytes > 0
            {
                Int32 n = SystemUdpSendTo( this._sid, src.handle, address, port )
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

        # 最近一次 recvFrom 的来源地址（C 侧 last-from 槽位）
        public get string lastFromAddress()
        {
            if this._sid == 0
            {
                ret ""
            }
            ret SystemUdpGetLastFromAddress( this._sid )
        }

        # 最近一次 recvFrom 的来源端口
        public get Int32 lastFromPort()
        {
            if this._sid == 0
            {
                ret 0
            }
            ret SystemUdpGetLastFromPort( this._sid )
        }

        # 关闭并释放句柄；C 侧会取消挂在该 sid 上的全部在途等待
        public void close()
        {
            if this._sid != 0
            {
                SystemUdpClose( this._sid )
                this._sid = 0
            }
        }
    }

    # ============================================================================
    # Udp — 外观类（SL 命名空间不能放裸函数 -> 静态工厂收口）
    # ============================================================================

    public class Udp
    {
        public static UdpStream bind( Int32 port ) throws
        {
            ret Udp.bindAddress( "0.0.0.0", port )
        }

        public static UdpStream bindAddress( string host, Int32 port ) throws
        {
            Int64 sid = SystemUdpBind( host, port )
            if sid == 0
            {
                throw NetError.BindFailed
            }
            ret UdpStream( UdpSocket( sid ) )
        }
    }

    # ============================================================================
    # UdpDatagram — 数据报值对象（datagrams() 流的元素）
    # ============================================================================

    public class UdpDatagram
    {
        ByteBuffer _data = null
        string _address = ""
        Int32 _port = 0

        _init_( ByteBuffer data, string address, Int32 port )
        {
            this._data = data
            this._address = address
            this._port = port
        }

        # 报文负载（收报后 readerIndex 从 0 起，直接按 ByteBuffer 读取）
        public get ByteBuffer data()
        {
            ret this._data
        }

        public get string address()
        {
            ret this._address
        }

        public get Int32 port()
        {
            ret this._port
        }
    }
}
