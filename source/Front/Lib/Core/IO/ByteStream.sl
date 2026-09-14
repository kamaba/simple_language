# ============================================================================
# Core/IO/ByteStream.sl — L0 字节层：统一字节流抽象
# 设计契约：md/design/STREAM_DESIGN.md §4.2 / §4.3 / §5 / §11.1
#
# P1 范围：abstract ByteStream + MemoryStream（TransformStream / HashingStream
# 为 §5 表格中的 P3，不落本文件；FileStream / StdStream 为 P2；NetStream 不做）。
#
# 语义要点：
#   - read 系返回实际读入字节数；0 = EOF；不存在负值（设计稿中的负值
#     错误码已统一并入异常模型，见 §14）。
#   - write 挂起直到全部写出（写背压）。P1 内 MemoryStream 即时完成，
#     真挂起点待 P2 网络 / 文件流接入 CORO_BLOCK_IO。
#   - canSeek == false 的流，seek() 抛 StreamIOError.NotSupported（getter
#     语法上不支持 throws，position / length 返 0，见位置节注记）。
#   - 能力位用「字段 + getter」而非抽象 getter：子类在构造期置位即可，
#     避免六个能力位全部 override。
#   - 超时：canTimeout == false 时设置读写超时抛 NotSupported（P1 没有
#     支持超时的具体流）。
#   - 水位：P1 仅保存设定值，不参与调度（MemoryStream 无背压需求），
#     P2 网络流接入 uv_read_stop 时启用。
#
# 已知偏差（详见设计文档 §4.2 实现注记）：
#   - asStream<T> / asChunkStream 实例桥接方法未做：改为 Stream.fromByteStream /
#     Stream.fromChunkStream 静态工厂（避免 L0 → L1 的跨文件实例方法依赖），
#     功能等价，见 §6.5。
# ============================================================================

public enum StreamIOError extends Error
{
    NotSupported     = { code = 1 }
    UnexpectedEof    = { code = 2 }
    Closed           = { code = 3 }
    Timeout          = { code = 4 }
    InvalidPosition  = { code = 5 }
    # 长度前缀分帧：payload 长度超过 maxFrame 上限（STREAM_DESIGN.md §8.1）
    FrameTooLarge    = { code = 6 }
    # 文件/管道打开失败（STREAM_DESIGN.md §4.4 FileStream）
    OpenFailed       = { code = 7 }
    # 底层 IO 读写/冲刷失败（FileStream 传递 C 侧负值错误码）
    IoError          = { code = 8 }
}

public enum SeekOrigin extends Int32
{
    Begin = 0
    Current = 1
    End = 2
}

public abstract class ByteStream extends Object
{
    # ── 能力位（子类构造期置位；getter 供外部查询）──

    bool _canRead = true
    bool _canWrite = true
    bool _canSeek = false
    bool _canTimeout = false
    bool _isDuplex = false
    bool _isMessageOriented = false

    # ── 生命周期状态 ──

    bool _closedRead = false
    bool _closedWrite = false
    bool _eof = false

    # ── 水位设定（P1 仅存值；见文件头注记）──

    Int32 _readHighWaterMark = 0
    Int32 _readLowWaterMark = 0
    Int32 _writeHighWaterMark = 0

    get bool canRead()
    {
        ret this._canRead
    }

    get bool canWrite()
    {
        ret this._canWrite
    }

    get bool canSeek()
    {
        ret this._canSeek
    }

    get bool canTimeout()
    {
        ret this._canTimeout
    }

    get bool isDuplex()
    {
        ret this._isDuplex
    }

    get bool isMessageOriented()
    {
        ret this._isMessageOriented
    }

    # ── 核心抽象（子类实现）──

    # 尽量读满 dst 可写区；返回实际读入字节数，0 = EOF。
    public abstract Int32 read( ByteBuf dst ) throws;

    # 挂起直到 src 可读区全部写出（写背压）。
    public abstract void write( ByteBuf src ) throws;

    # 冲刷用户态缓冲（不保证落盘）。
    public abstract void flush() throws;

    # ── 读组合方法（基类实现）──

    public Int32 readAtLeast( ByteBuf dst, Int32 n ) throws
    {
        this._ensureReadable()
        if n <= 0
        {
            ret 0
        }
        Int32 before = dst.writerIndex
        while dst.writerIndex - before < n
        {
            Int32 got = this.read( dst )
            if got <= 0
            {
                break
            }
        }
        ret dst.writerIndex - before
    }

    public ByteBuf readExactly( Int32 n ) throws
    {
        this._ensureReadable()
        if n < 0
        {
            throw StreamIOError.NotSupported
        }
        var dst = ByteBuf( n )
        if n > 0
        {
            Int32 got = this.readAtLeast( dst, n )
            if got < n
            {
                throw StreamIOError.UnexpectedEof
            }
        }
        ret dst
    }

    public Int32 skip( Int32 n ) throws
    {
        this._ensureReadable()
        if n <= 0
        {
            ret 0
        }
        var scratch = ByteBuf( 4096 )
        Int32 remaining = n
        Int32 total = 0
        while remaining > 0
        {
            if scratch.readableBytes > 0
            {
                scratch.clear()
            }
            Int32 got = this.read( scratch )
            if got <= 0
            {
                break
            }
            total = total + got
            remaining = remaining - got
        }
        ret total
    }

    # 读到 EOF；maxBytes = 0 表示不限。慎用于无限流。
    public ByteBuf readAll( Int32 maxBytes = 0 ) throws
    {
        this._ensureReadable()
        var buf = ByteBuf()
        while true
        {
            Int32 got = this.read( buf )
            if got <= 0
            {
                break
            }
            if maxBytes > 0 && buf.readableBytes >= maxBytes
            {
                break
            }
        }
        ret buf
    }

    # ── 写组合方法 ──

    public void writeByte( UInt8 b ) throws
    {
        this._ensureWritable()
        var one = ByteBuf( 1 )
        one.writeU8( b )
        this.write( one )
    }

    # ── 位置 ──
    # 语言限制：getter 不支持 throws（全库无先例），NotSupported 检查集中在
    # seek() 方法；不可 seek 流的 position / length getter 返回 0（P1 偏差，
    # 调用方需要严格检查时应走 seek / canSeek）。

    public get Int64 position()
    {
        ret 0
    }

    public set void position( Int64 pos ) throws
    {
        this._ensureSeekable()
    }

    public get Int64 length()
    {
        ret 0
    }

    public Int64 seek( Int64 offset, SeekOrigin origin ) throws
    {
        this._ensureSeekable()
        ret 0
    }

    # ── 生命周期 ──

    public void closeRead() throws
    {
        this._closedRead = true
    }

    public void closeWrite() throws
    {
        this.flush()
        this._closedWrite = true
    }

    public void close() throws
    {
        this.closeRead()
        this.closeWrite()
    }

    public get bool isClosed()
    {
        ret this._closedRead || this._closedWrite
    }

    public get bool isEof()
    {
        ret this._eof
    }

    # ── 超时（canTimeout == false 时抛 NotSupported）──

    public set void readTimeoutMs( Int64 ms ) throws
    {
        if this._canTimeout == false
        {
            throw StreamIOError.NotSupported
        }
    }

    public set void writeTimeoutMs( Int64 ms ) throws
    {
        if this._canTimeout == false
        {
            throw StreamIOError.NotSupported
        }
    }

    # ── 水位（P1 仅存值，见文件头注记）──

    public set void readHighWaterMark( Int32 bytes )
    {
        this._readHighWaterMark = bytes
    }

    public set void readLowWaterMark( Int32 bytes )
    {
        this._readLowWaterMark = bytes
    }

    public set void writeHighWaterMark( Int32 bytes )
    {
        this._writeHighWaterMark = bytes
    }

    public get Int32 pendingWriteBytes()
    {
        ret 0
    }

    # ── 工厂 ──

    public static ByteStream memory()
    {
        var s = MemoryStream()
        ret s
    }

    public static ByteStream memory( ByteBuf initial )
    {
        var s = MemoryStream( initial )
        ret s
    }

    # 只读包装（序列化读取场景）：复用外部 ByteBuf，禁止写入。
    public static ByteStream wrapReadOnly( ByteBuf buf )
    {
        var s = MemoryStream( buf )
        s._canWrite = false
        ret s
    }

    # ── 私有守卫 ──

    _ensureReadable() throws
    {
        if this._closedRead
        {
            throw StreamIOError.Closed
        }
        if this._canRead == false
        {
            throw StreamIOError.NotSupported
        }
    }

    _ensureWritable() throws
    {
        if this._closedWrite
        {
            throw StreamIOError.Closed
        }
        if this._canWrite == false
        {
            throw StreamIOError.NotSupported
        }
    }

    _ensureSeekable() throws
    {
        if this._canSeek == false
        {
            throw StreamIOError.NotSupported
        }
    }
}

# ============================================================================
# MemoryStream — 内存流（P1 唯一 L0 具体子类）
# 读写共用一个 ByteBuf：readerIndex 即 position，writerIndex 即数据尾。
# 序列化最常用的目标（ProtoBuf encode → MemoryStream → toArray）。
# ============================================================================

public class MemoryStream extends ByteStream
{
    ByteBuf _buf = null

    # ── 构造 ──

    override _init_()
    {
        this._init_( 512 )
    }

    _init_( Int32 initialCapacity )
    {
        this._buf = ByteBuf( initialCapacity )
        this._canSeek = true
    }

    # 复用外部 ByteBuf（避免二次拷贝）
    _init_( ByteBuf backing )
    {
        this._buf = backing
        this._canSeek = true
    }

    # ── 能力/状态 ──

    override get Int64 length()
    {
        ret this._buf.writerIndex
    }

    override get Int64 position()
    {
        ret this._buf.readerIndex
    }

    override set void position( Int64 pos ) throws
    {
        Int32 p = pos as Int32
        if p < 0 || p > this._buf.writerIndex
        {
            throw StreamIOError.InvalidPosition
        }
        this._buf.readerIndex = p
    }

    override public Int64 seek( Int64 offset, SeekOrigin origin ) throws
    {
        Int64 target = 0
        if origin == SeekOrigin.Begin
        {
            target = offset
        }
        else if origin == SeekOrigin.Current
        {
            target = this.position + offset
        }
        else
        {
            target = this.length + offset
        }
        this.position = target
        ret target
    }

    # ── 核心读写 ──

    override public Int32 read( ByteBuf dst ) throws
    {
        this._ensureReadable()
        Int32 avail = this._buf.readableBytes
        Int32 room = dst.writableBytes
        Int32 n = avail
        if room < n
        {
            n = room
        }
        if n <= 0
        {
            this._eof = true
            ret 0
        }
        this._buf.readBytes( dst, n )
        ret n
    }

    override public void write( ByteBuf src ) throws
    {
        this._ensureWritable()
        Int32 n = src.readableBytes
        if n > 0
        {
            this._buf.writeBytes( src )
        }
    }

    override public void flush() throws
    {
        # 内存流无用户态缓冲，flush 为空操作
    }

    # ── 访问内部缓冲 ──

    # 返回内部缓冲（不拷贝）；调用方继续读即从当前 readerIndex 开始
    public ByteBuf toByteBuf()
    {
        ret this._buf
    }

    public UInt8Array toArray()
    {
        ret this._buf.toArray()
    }

    # 清空并复位索引
    public void reset()
    {
        this._buf.clear()
        this._eof = false
    }
}
