# ============================================================================
# Core/IO/ByteBuf.sl — L0 字节层：双索引字节缓冲
# 设计契约：md/design/STREAM_DESIGN.md §4.1 / §15.2
#
# 本体存放在 C VM 端注册表（csimple_lang/src/base/vm_bytebuf.c），
# SL 对象仅持 Int64 句柄（与 Channel<T>._chid 同一模式），句柄 0 恒为无效。
# 守卫约定：读越界抛 BufferError.Underflow，索引越界抛 IndexOutOfRange，
# 释放后读写抛 Released，扩容超上限抛 CapacityExceeded；
# C 层失败只告警并压类型默认值（与 string/coroutine 类别同一约定）。
# 端序：默认小端；2/4/8 字节读写各带 Be 孪生方法。
#
# 已知偏差（详见设计文档 §4.1 实现注记）：
#   - wrap() 在 P0 阶段为深拷贝（零拷贝待 isolate/root-pin 机制后启用）
#   - readVarUint() 返回 UInt64（无符号语义完整）；截断时返回 0 且不推进
#     readerIndex，调用方以 readerIndex 是否前移判断成功
#   - readFixedVarUint / readString(Encoding) / writeString(Encoding) 重载、
#     toTransferable/fromTransferable 未实现（Encoding 为空壳、isolate 传输
#     机制未落地，待后续版本）
# ============================================================================

public enum BufferError extends Error
{
    Underflow        = { code = 1 }
    IndexOutOfRange  = { code = 2 }
    Released         = { code = 3 }
    CapacityExceeded = { code = 4 }
}

public class ByteBuf extends Object
{
    # C VM 端注册表句柄：0 = 未持有 / 已释放
    Int64 _bid = 0

    # ── 构造 ──

    override _init_()
    {
        this._bid = SystemByteBufCreate( 0 )
    }

    _init_( Int32 initialCapacity )
    {
        this._bid = SystemByteBufCreate( initialCapacity )
    }

    # ── 静态工厂（destroy-then-replace，防注册表句柄泄漏） ──

    public static ByteBuf fromBytes( UInt8Array bytes )
    {
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = SystemByteBufFromArray( bytes )
        ret b
    }

    # P0：与 fromBytes 等价的深拷贝语义，零拷贝待 root-pin 机制后启用
    public static ByteBuf wrap( UInt8Array bytes )
    {
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = SystemByteBufWrapArray( bytes )
        ret b
    }

    public static ByteBuf fromString( string text )
    {
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = SystemByteBufFromString( text )
        ret b
    }

    public static ByteBuf fromHex( string hex )
    {
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = SystemByteBufFromHex( hex )
        ret b
    }

    # 供 Std 等跨模块将系统调用返回的注册表 id 包装成 ByteBuf 对象
    #（Lz4/Zlib 压缩等返回新缓冲的场景；接管 id，禁止与现有句柄重复持有）
    public static ByteBuf fromHandle( Int64 handle )
    {
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = handle
        ret b
    }

    # ── 私有守卫 ──

    _ensureLive() throws
    {
        if SystemByteBufIsReleased( this._bid )
        {
            throw BufferError.Released
        }
    }

    _ensureReadable( Int32 len ) throws
    {
        this._ensureLive()
        if SystemByteBufReadableBytes( this._bid ) < len
        {
            throw BufferError.Underflow
        }
    }

    # ── 索引与容量属性 ──

    get Int32 readerIndex()
    {
        ret SystemByteBufGetReaderIndex( this._bid )
    }

    set void readerIndex( Int32 index ) throws
    {
        this._ensureLive()
        if index < 0 || index > this.capacity
        {
            throw BufferError.IndexOutOfRange
        }
        SystemByteBufSetReaderIndex( this._bid, index )
    }

    get Int32 writerIndex()
    {
        ret SystemByteBufGetWriterIndex( this._bid )
    }

    set void writerIndex( Int32 index ) throws
    {
        this._ensureLive()
        if index < 0 || index > this.capacity
        {
            throw BufferError.IndexOutOfRange
        }
        SystemByteBufSetWriterIndex( this._bid, index )
    }

    get Int32 capacity()
    {
        ret SystemByteBufCapacity( this._bid )
    }

    get Int32 maxCapacity()
    {
        ret SystemByteBufMaxCapacity( this._bid )
    }

    get Int32 readableBytes()
    {
        ret SystemByteBufReadableBytes( this._bid )
    }

    get Int32 writableBytes()
    {
        ret SystemByteBufWritableBytes( this._bid )
    }

    # 原生句柄（FileStream 等原生流直读写；仅供标准库内部使用）
    public get Int64 handle()
    {
        ret this._bid
    }

    # ── 容量管理 ──

    # 超出 maxCapacity 时抛 CapacityExceeded
    public void ensureWritable( Int32 minWritableBytes ) throws
    {
        this._ensureLive()
        var need = minWritableBytes - this.writableBytes
        if need > 0 && this.capacity + need > this.maxCapacity
        {
            throw BufferError.CapacityExceeded
        }
        SystemByteBufEnsureWritable( this._bid, minWritableBytes )
    }

    # 将已读区滑动归零（压缩碎片，代价是 memmove）
    public void discardReadBytes()
    {
        SystemByteBufDiscardReadBytes( this._bid )
    }

    # readerIndex = writerIndex = 0（不清底层数组）
    public void clear()
    {
        SystemByteBufClear( this._bid )
    }

    # capacity 收缩到 readableBytes
    public void shrink()
    {
        SystemByteBufShrink( this._bid )
    }

    # ── 定宽读（推进 readerIndex，先做可读性守卫） ──

    public UInt8 readU8() throws
    {
        this._ensureReadable( 1 )
        ret SystemByteBufReadU8( this._bid )
    }

    public bool readBool() throws
    {
        this._ensureReadable( 1 )
        ret SystemByteBufReadBool( this._bid )
    }

    public Int16 readI16Le() throws
    {
        this._ensureReadable( 2 )
        ret SystemByteBufReadI16Le( this._bid )
    }

    public Int16 readI16Be() throws
    {
        this._ensureReadable( 2 )
        ret SystemByteBufReadI16Be( this._bid )
    }

    public UInt16 readU16Le() throws
    {
        this._ensureReadable( 2 )
        ret SystemByteBufReadU16Le( this._bid )
    }

    public UInt16 readU16Be() throws
    {
        this._ensureReadable( 2 )
        ret SystemByteBufReadU16Be( this._bid )
    }

    public Int32 readI32Le() throws
    {
        this._ensureReadable( 4 )
        ret SystemByteBufReadI32Le( this._bid )
    }

    public Int32 readI32Be() throws
    {
        this._ensureReadable( 4 )
        ret SystemByteBufReadI32Be( this._bid )
    }

    public UInt32 readU32Le() throws
    {
        this._ensureReadable( 4 )
        ret SystemByteBufReadU32Le( this._bid )
    }

    public UInt32 readU32Be() throws
    {
        this._ensureReadable( 4 )
        ret SystemByteBufReadU32Be( this._bid )
    }

    public Int64 readI64Le() throws
    {
        this._ensureReadable( 8 )
        ret SystemByteBufReadI64Le( this._bid )
    }

    public Int64 readI64Be() throws
    {
        this._ensureReadable( 8 )
        ret SystemByteBufReadI64Be( this._bid )
    }

    public UInt64 readU64Le() throws
    {
        this._ensureReadable( 8 )
        ret SystemByteBufReadU64Le( this._bid )
    }

    public UInt64 readU64Be() throws
    {
        this._ensureReadable( 8 )
        ret SystemByteBufReadU64Be( this._bid )
    }

    public Float32 readF32Le() throws
    {
        this._ensureReadable( 4 )
        ret SystemByteBufReadF32Le( this._bid )
    }

    public Float32 readF32Be() throws
    {
        this._ensureReadable( 4 )
        ret SystemByteBufReadF32Be( this._bid )
    }

    public Float64 readF64Le() throws
    {
        this._ensureReadable( 8 )
        ret SystemByteBufReadF64Le( this._bid )
    }

    public Float64 readF64Be() throws
    {
        this._ensureReadable( 8 )
        ret SystemByteBufReadF64Be( this._bid )
    }

    # ── 变长读写（varint/zigzag，STREAM_DESIGN.md §15.3） ──

    # 无符号 LEB128；截断时返回 0 且不推进 readerIndex（以 readerIndex
    # 是否前移判断成功）。设计偏差：返回 UInt64 而非 Int64（无符号语义完整）
    public UInt64 readVarUint() throws
    {
        this._ensureLive()
        ret SystemVarUintDecode( this._bid )
    }

    # 有符号：LEB128 + ZigZag
    public Int64 readVarInt() throws
    {
        this._ensureLive()
        ret SystemVarIntDecode( this._bid )
    }

    # 无符号 LEB128 编码，返回写入字节数（1..10）
    public void writeVarUint( Int64 value )
    {
        SystemVarUintEncode( this._bid, value )
    }

    # ZigZag + LEB128 编码
    public void writeVarInt( Int64 value )
    {
        SystemVarIntEncode( this._bid, value )
    }

    # 编码长度预查（长度前缀分帧用），不写缓冲
    public static Int32 varUintEncodedSize( Int64 value )
    {
        ret SystemVarUintEncodedSize( value )
    }

    # ── 批量搬运 ──

    # 把 this 的 len 字节搬运到 dst（双方索引各自推进；经临时堆缓冲中转，
    # 切片/副本视图重叠也不会互相污染）
    public void readBytes( ByteBuf dst, Int32 len ) throws
    {
        this._ensureReadable( len )
        dst._ensureLive()
        SystemByteBufReadBytes( dst._bid, this._bid, len )
    }

    # 搬入 src 的全部可读字节
    public void writeBytes( ByteBuf src )
    {
        SystemByteBufWriteBytes( this._bid, src._bid, src.readableBytes )
    }

    public void writeBytes( ByteBuf src, Int32 len )
    {
        SystemByteBufWriteBytes( this._bid, src._bid, len )
    }

    # 读出 len 字节为新数组（len 钳制到 readableBytes）
    public UInt8Array readByteArray( Int32 len ) throws
    {
        this._ensureReadable( len )
        ret SystemByteBufReadByteArray( this._bid, len )
    }

    # 读出 len 字节并按 UTF-8 解码为字符串（SL 字符串为 C 风格 \0 结尾，内嵌 NUL 会截断；二进制安全用 readByteArray）
    public string readString( Int32 len ) throws
    {
        this._ensureReadable( len )
        ret SystemByteBufReadString( this._bid, len )
    }

    # 读到行尾（含 '\n'）；无剩余行时返回 null，不抛异常
    public string readLine()
    {
        ret SystemByteBufReadLine( this._bid )
    }

    # 跳过 len 字节（只推进 readerIndex）
    public void skipBytes( Int32 len ) throws
    {
        this._ensureReadable( len )
        SystemByteBufSkip( this._bid, len )
    }

    public void writeByteArray( UInt8Array bytes )
    {
        SystemByteBufWriteByteArray( this._bid, bytes )
    }

    # 按 UTF-8 追加文本字节
    public void writeString( string text )
    {
        SystemByteBufWriteString( this._bid, text )
    }

    # ── 定宽写（推进 writerIndex，容量不足自动扩容） ──

    public void writeU8( UInt8 value )
    {
        this._ensureLive()
        SystemByteBufWriteU8( this._bid, value )
    }

    public void writeBool( bool value )
    {
        this._ensureLive()
        SystemByteBufWriteBool( this._bid, value )
    }

    public void writeI16Le( Int16 value )
    {
        this._ensureLive()
        SystemByteBufWriteI16Le( this._bid, value )
    }

    public void writeI16Be( Int16 value )
    {
        this._ensureLive()
        SystemByteBufWriteI16Be( this._bid, value )
    }

    public void writeU16Le( UInt16 value )
    {
        this._ensureLive()
        SystemByteBufWriteU16Le( this._bid, value )
    }

    public void writeU16Be( UInt16 value )
    {
        this._ensureLive()
        SystemByteBufWriteU16Be( this._bid, value )
    }

    public void writeI32Le( Int32 value )
    {
        this._ensureLive()
        SystemByteBufWriteI32Le( this._bid, value )
    }

    public void writeI32Be( Int32 value )
    {
        this._ensureLive()
        SystemByteBufWriteI32Be( this._bid, value )
    }

    public void writeU32Le( UInt32 value )
    {
        this._ensureLive()
        SystemByteBufWriteU32Le( this._bid, value )
    }

    public void writeU32Be( UInt32 value )
    {
        this._ensureLive()
        SystemByteBufWriteU32Be( this._bid, value )
    }

    public void writeI64Le( Int64 value )
    {
        this._ensureLive()
        SystemByteBufWriteI64Le( this._bid, value )
    }

    public void writeI64Be( Int64 value )
    {
        this._ensureLive()
        SystemByteBufWriteI64Be( this._bid, value )
    }

    public void writeU64Le( UInt64 value )
    {
        this._ensureLive()
        SystemByteBufWriteU64Le( this._bid, value )
    }

    public void writeU64Be( UInt64 value )
    {
        this._ensureLive()
        SystemByteBufWriteU64Be( this._bid, value )
    }

    public void writeF32Le( Float32 value )
    {
        this._ensureLive()
        SystemByteBufWriteF32Le( this._bid, value )
    }

    public void writeF32Be( Float32 value )
    {
        this._ensureLive()
        SystemByteBufWriteF32Be( this._bid, value )
    }

    public void writeF64Le( Float64 value )
    {
        this._ensureLive()
        SystemByteBufWriteF64Le( this._bid, value )
    }

    public void writeF64Be( Float64 value )
    {
        this._ensureLive()
        SystemByteBufWriteF64Be( this._bid, value )
    }

    # ── 视图（共享底层存储；经注册表句柄引用同一 root） ──

    # 可读区视图（从 readerIndex 起，长 readableBytes）
    public ByteBuf slice()
    {
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = SystemByteBufSlice( this._bid, this.readerIndex, this.readableBytes )
        ret b
    }

    # [index, index+length) 视图，越界抛 IndexOutOfRange
    public ByteBuf slice( Int32 index, Int32 length ) throws
    {
        this._ensureLive()
        if index < 0 || length < 0 || index + length > this.capacity
        {
            throw BufferError.IndexOutOfRange
        }
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = SystemByteBufSlice( this._bid, index, length )
        ret b
    }

    # 共享全部存储的副本视图（独立双索引）
    public ByteBuf duplicate()
    {
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = SystemByteBufDuplicate( this._bid )
        ret b
    }

    # 深拷贝（独立存储）
    public ByteBuf copy()
    {
        var b = ByteBuf()
        SystemByteBufDestroy( b._bid )
        b._bid = SystemByteBufCopy( this._bid )
        ret b
    }

    # ── 查找与比较（可读区语义；失败返回默认值，不抛异常） ──

    # 从 readerIndex 起找单个字节，返回相对 readerIndex 的偏移，未找到返回 -1
    public Int32 indexOf( UInt8 value )
    {
        ret SystemByteBufIndexOfByte( this._bid, value )
    }

    # 从 readerIndex 起找 pattern，返回相对 readerIndex 的偏移，未找到返回 -1
    public Int32 indexOf( ByteBuf pattern )
    {
        ret SystemByteBufIndexOf( this._bid, pattern._bid )
    }

    # 可读区是否以 prefix 开头（从 readerIndex 起比较）
    public bool startsWith( ByteBuf prefix )
    {
        ret SystemByteBufStartsWith( this._bid, prefix._bid )
    }

    # 可读区逐字节相等（对 Object.equals(object) 的类型重载）
    public bool equals( ByteBuf other )
    {
        ret SystemByteBufEquals( this._bid, other._bid )
    }

    # ── 转换 ──

    # 可读区导出为数组拷贝（不改变索引）
    public UInt8Array toArray()
    {
        ret SystemByteBufToArray( this._bid )
    }

    # 可读区导出为小写十六进制串（不改变索引）
    public string toHex()
    {
        ret SystemByteBufToHex( this._bid )
    }

    # 可读区按 UTF-8 解码（不改变索引）
    override string toString()
    {
        ret SystemByteBufToString( this._bid )
    }

    # 可读区 FNV-1a 哈希（值语义；与 Object.hashCode 的 getter 形式一致）
    override get Int32 hashCode()
    {
        ret SystemByteBufHashCode( this._bid )
    }

    # ── 生命周期（设计文档 §4.1 补充项：注册表手动释放入口） ──

    # 幂等：释放后句柄归零，后续读写由守卫抛 BufferError.Released
    public void release()
    {
        SystemByteBufDestroy( this._bid )
        this._bid = 0
    }
}
