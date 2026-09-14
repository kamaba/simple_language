# ============================================================================
# Core/IO/LengthPrefix.sl — 长度前缀分帧机制（STREAM_DESIGN.md §8.1）
#
# P2 范围：分帧机制本体（静态工具 + 三个内部支撑类）。帧格式
# `varuint(len) + payload`（无符号 LEB128 长度前缀 + 裸 payload），
# 供 ProtoCodec 流式编解码（§8.4）复用；设计稿中的 LengthPrefixCodec
# StreamTransformer（§6.3）为 P3，不落本文件。
#
# 语义要点：
#   - 帧边界：varuint 长度前缀声明 payload 字节数，接收端按长度切帧。
#   - 半包契约：长度前缀或 payload 不完整时 tryReadFrame 返回 null，
#     且不消费 readerIndex（varint 读走的索引已回退），调用方可继续
#     累积后重试（半包续接）。
#   - 超限契约：payload 长度超过 maxFrame 抛 StreamIOError.FrameTooLarge
#     （防恶意长度前缀撑爆内存；readFrameLength 以 -2 报告，drainFrames
#     转 throw）。
#   - varint 截断判定沿用 ProtocalBuffers.sl 先例：C 层截断返回 0 且
#     不推进 readerIndex，以 readerIndex 是否前移判成功。
#
# 已知偏差（详见设计文档 §17 实现注记）：
#   - 设计稿 bind() 因 `bind` 为 SL 保留 token（类头语法位）改名
#     bindStream，语义不变。
#   - 设计稿 LengthPrefixCodec（StreamTransformer 形态）在 P3 行；P2
#     先以静态工具 + ProtoCodec 薄壳落地分帧机制。
# ============================================================================

# ============================================================================
# LengthPrefix — 长度前缀分帧静态工具（无实例）
# ============================================================================

public class LengthPrefix extends Object
{
    # 默认单帧上限 64 KiB（设计稿 §12.3 WebSocket maxFrameSize 同值）
    public static Int32 defaultMaxFrame()
    {
        ret 65536
    }

    # ── 写侧：帧 = varuint(payload 可读长度) + payload 可读区 ──

    # 把 payload 的可读区打包成帧写入 dst（不检查上限，写入方自决）
    public static void writeFrame( ByteBuf dst, ByteBuf payload )
    {
        Int32 len = payload.readableBytes
        dst.writeVarUint( SystemConvertInt64( len ) )
        dst.writeBytes( payload )
    }

    # 数组形式（payload 整体为一帧）
    public static void writeFrameBytes( ByteBuf dst, UInt8Array payload )
    {
        Int32 len = payload.length
        dst.writeVarUint( SystemConvertInt64( len ) )
        dst.writeByteArray( payload )
    }

    # ── 读侧：单帧尝试解析 ──

    # 读取长度前缀。返回值：
    #   >= 0  完整帧的 payload 长度（varint 已被消费，payload 未动）
    #   -1    半包（varint 或 payload 不完整；readerIndex 已回退）
    #   -2    超限（payload 长度 > maxFrame；readerIndex 已回退）
    public static Int32 readFrameLength( ByteBuf src, Int32 maxFrame ) throws
    {
        Int32 before = src.readerIndex
        var u = src.readVarUint()
        if src.readerIndex == before
        {
            ret -1
        }
        Int64 len64 = SystemConvertInt64FromUInt64( u )
        if len64 > SystemConvertInt64( maxFrame )
        {
            src.readerIndex = before
            ret -2
        }
        Int32 len = SystemConvertInt32( len64 )
        if src.readableBytes < len
        {
            src.readerIndex = before
            ret -1
        }
        ret len
    }

    # 尝试从 src 读出一帧（半包续接语义）。
    #   成功 → 独立 ByteBuf（消费 src 对应区间）
    #   半包 → null（不消费 readerIndex）
    #   超限 → throw StreamIOError.FrameTooLarge
    public static ByteBuf tryReadFrame( ByteBuf src, Int32 maxFrame ) throws
    {
        Int32 len = LengthPrefix.readFrameLength( src, maxFrame )
        if len == -2
        {
            throw StreamIOError.FrameTooLarge
        }
        if len < 0
        {
            ret null
        }
        var frame = ByteBuf( len )
        if len > 0
        {
            src.readBytes( frame, len )
        }
        ret frame
    }

    # ── 内部：从累积缓冲抽取完整帧并解码投递（drain 循环）──
    # 半包自然退出；超限 throw FrameTooLarge；decode 抛出的异常自然
    # 传播（由调用方统一转投 onError，见 §8.4）。返回无意义，仅保持
    # void 契约。

    static void drainFrames<T>( StreamController<T> ctrl, Codec<T, ByteBuf> codec, ByteBuf acc, Int32 maxFrame ) throws
    {
        StreamController<T> c = ctrl
        Codec<T, ByteBuf> cd = codec
        while true
        {
            Int32 len = LengthPrefix.readFrameLength( acc, maxFrame )
            if len == -2
            {
                throw StreamIOError.FrameTooLarge
            }
            if len < 0
            {
                break
            }
            var frame = ByteBuf( len )
            if len > 0
            {
                acc.readBytes( frame, len )
            }
            T msg = cd.decode( frame )
            c.add( msg )
        }
    }

    # ── 流式三件套（拉 / 写 / 推）──

    # 字节流 → 分帧解码消息流：read 循环 + 半包续接；EOF 时残留
    # 半包帧抛 UnexpectedEof，read/超限/解码异常统一转投 onError。
    public static Stream<T> decodeStream<T>( ByteStream src, Codec<T, ByteBuf> codec )
    {
        ret LengthPrefix.decodeStream<T>( src, codec, LengthPrefix.defaultMaxFrame() )
    }

    public static Stream<T> decodeStream<T>( ByteStream src, Codec<T, ByteBuf> codec, Int32 maxFrame )
    {
        var s = _FramedDecodeStream<T>( src, codec, maxFrame )
        ret s
    }

    # 消息写入端：add(msg) → encode → 帧直写 dst（varint 头 + payload
    # 两步写出）；close 时 flush。payload 超 maxFrame 抛 FrameTooLarge。
    public static StreamSink<T> encodeSink<T>( ByteStream dst, Codec<T, ByteBuf> codec )
    {
        ret LengthPrefix.encodeSink<T>( dst, codec, LengthPrefix.defaultMaxFrame() )
    }

    public static StreamSink<T> encodeSink<T>( ByteStream dst, Codec<T, ByteBuf> codec, Int32 maxFrame )
    {
        var k = _FrameEncodeSink<T>( dst, codec, maxFrame )
        ret k
    }

    # chunk 流（Stream<ByteBuf>）→ 分帧解码消息流：上游每 chunk 累积
    # 后抽帧（chunked decoder，处理半包，§8.4 bind 语义）。
    public static Stream<T> bindStream<T>( Stream<ByteBuf> chunks, Codec<T, ByteBuf> codec )
    {
        ret LengthPrefix.bindStream<T>( chunks, codec, LengthPrefix.defaultMaxFrame() )
    }

    public static Stream<T> bindStream<T>( Stream<ByteBuf> chunks, Codec<T, ByteBuf> codec, Int32 maxFrame )
    {
        var s = _FramedBindStream<T>( chunks, codec, maxFrame )
        ret s
    }
}

# ============================================================================
# _FramedDecodeStream<T> — 拉模式分帧解码流
# 生产协程循环 read → acc 累积 → drainFrames；异常（含 EOF 残留半包的
# UnexpectedEof）经 label{}catch{} 统一转投 addError 后 close。
# ============================================================================

public class _FramedDecodeStream<T> extends Stream<T>
{
    ByteStream _src = null
    Codec<T, ByteBuf> _codec = null
    Int32 _maxFrame = 0

    _init_( ByteStream src, Codec<T, ByteBuf> codec, Int32 maxFrame )
    {
        this._src = src
        this._codec = codec
        this._maxFrame = maxFrame
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        StreamController<T> ctrl = StreamController<T>( 64 )
        Stream<T> s = ctrl.stream
        StreamSubscription sub = s.listen( onData, onError, onDone, cancelOnError )
        ByteStream src = this._src
        Codec<T, ByteBuf> codec = this._codec
        Int32 maxFrame = this._maxFrame
        function f = function()
        {
            ByteBuf acc = ByteBuf( 4096 )
            bool truncated = false
            label pumpBlock
            {
                while true
                {
                    Int32 got = try? src.read( acc )
                    if got == null
                    {
                        # read 异常（try? 吞为 null）：粗粒度转投（P4 透传原始错误）
                        ctrl.addError( StreamIOError.Timeout )
                        break
                    }
                    if got <= 0
                    {
                        # EOF：残留半包帧属截断流。闭包函数不能声明 throws，
                        # 以标志带出 label 块，在 catch 外统一转投（异常路径
                        # truncated 保持 false，两条路径互斥不重复投递）
                        if acc.readableBytes > 0
                        {
                            truncated = true
                        }
                        break
                    }
                    try LengthPrefix.drainFrames<T>( ctrl, codec, acc, maxFrame )
                    acc.discardReadBytes()
                }
            }
            catch e
            {
                ctrl.addError( e )
            }
            if truncated
            {
                ctrl.addError( StreamIOError.UnexpectedEof )
            }
            ctrl.close()
        }
        Coroutine.spawnClosure0( f )
        ret sub
    }
}

# ============================================================================
# _FrameEncodeSink<T> — 分帧编码写入端
# ============================================================================

public class _FrameEncodeSink<T> extends StreamSink<T>
{
    ByteStream _dst = null
    Codec<T, ByteBuf> _codec = null
    Int32 _maxFrame = 0

    _init_( ByteStream dst, Codec<T, ByteBuf> codec, Int32 maxFrame )
    {
        this._dst = dst
        this._codec = codec
        this._maxFrame = maxFrame
    }

    # 覆写自 StreamSink.add（父链无 throws）；throws 为本实现新增——
    # 超限/底层写失败以异常上抛，由调用方决定降级策略。
    override public void add( T chunk ) throws
    {
        Codec<T, ByteBuf> codec = this._codec
        ByteStream dst = this._dst
        ByteBuf payload = codec.encode( chunk )
        Int32 len = payload.readableBytes
        if len > this._maxFrame
        {
            throw StreamIOError.FrameTooLarge
        }
        # 帧头 + payload 打包为独立 ByteBuf 后整体写出：varint/字节级
        # 写入是 ByteBuf 的方法，ByteStream 只有 write(ByteBuf)。
        var frame = ByteBuf( ByteBuf.varUintEncodedSize( SystemConvertInt64( len ) ) + len )
        frame.writeVarUint( SystemConvertInt64( len ) )
        frame.writeBytes( payload )
        dst.write( frame )
    }

    override public void close()
    {
        this._dst.flush()
    }
}

# ============================================================================
# _FramedBindStream<T> — 推模式分帧解码流（chunked decoder）
# 订阅上游 ByteBuf chunk 流：onData 累积 + 抽帧转发；上游错误透传；
# 上游 done 时残留半包转 UnexpectedEof。中转 controller 无缓冲上限
# （转发不丢事件）。
# ============================================================================

public class _FramedBindStream<T> extends Stream<T>
{
    Stream<ByteBuf> _source = null
    Codec<T, ByteBuf> _codec = null
    Int32 _maxFrame = 0

    _init_( Stream<ByteBuf> source, Codec<T, ByteBuf> codec, Int32 maxFrame )
    {
        this._source = source
        this._codec = codec
        this._maxFrame = maxFrame
    }

    override public StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )
    {
        Stream<ByteBuf> src = this._source
        Codec<T, ByteBuf> codec = this._codec
        Int32 maxFrame = this._maxFrame
        StreamController<T> ctrl = StreamController<T>()
        Stream<T> s = ctrl.stream
        StreamSubscription sub = s.listen( onData, onError, onDone, cancelOnError )
        ByteBuf acc = ByteBuf( 4096 )
        function fwd = function( object v )
        {
            ByteBuf chunk = v as ByteBuf
            label chunkBlock
            {
                acc.writeBytes( chunk )
                try LengthPrefix.drainFrames<T>( ctrl, codec, acc, maxFrame )
                acc.discardReadBytes()
            }
            catch e
            {
                ctrl.addError( e )
            }
        }
        function err = function( object e )
        {
            ctrl.addError( e )
        }
        function dn = function()
        {
            if acc.readableBytes > 0
            {
                # 上游 done 但残留半包帧：截断流
                ctrl.addError( StreamIOError.UnexpectedEof )
            }
            ctrl.close()
        }
        ret src.listen( fwd, err, dn, cancelOnError )
    }
}
