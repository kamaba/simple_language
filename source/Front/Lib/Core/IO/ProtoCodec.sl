# ============================================================================
# Core/IO/ProtoCodec.sl — ProtoBuf 消息编解码器（STREAM_DESIGN.md §8.4）
#
# P2 范围：ProtoCodec<T> 闭包注入版 + 分帧流式三件套（decodeStream /
# encodeSink / bindStream，机制本体在 LengthPrefix.sl）。底层 wire
# format 读写用 Core.Text.ProtocalBuffers（PbWriter / PbReader / varint
# 全家走 ByteBuf system method）。
#
# 已知偏差（详见设计文档 §17 实现注记）：
#   - 设计稿 of(ProtoSchema) / of()（schema 或 @Serializable 注解驱动）
#     依赖编译期代码生成与运行时反射，SL 无运行时反射；P2 改为
#     of(Function encoder, Function decoder) 闭包注入 —— encode/decode
#     逻辑由调用方用 PbWriter/PbReader 手写（方式 0，§8.2 手写 wire
#     format 层已交付）。schema 驱动的生成器仍为 P3（codegen）。
#   - 设计稿 bind() 因 `bind` 为 SL 保留 token（类头语法位）改名
#     bindStream，语义不变。
#   - 设计稿直接 `extends Codec<T, ByteBuf>`；Front 泛型 extends 混合实参
#     （模板 + 具体类）不物化 gen 实体（AddMetaTemplateClassByMetaClassAndMetaTemplateMetaTypeList
#     要求全部实参为具体类形态），父类落回原始模板类后覆写签名无法匹配
#     具体类型参数。故经参数到参数的中间基类 _ProtoCodecBase<S,T> 继承
#     （_FusedCodec 同模式），语义不变。
# ============================================================================

# ============================================================================
# _ProtoCodecBase<S,T> — ProtoCodec 的参数化基类
#
# 承载 Converter.startChunkedConversion 的通用覆写（逐消息委托 encoder），
# 使覆写签名保持模板形态（S/T 形参），与原始模板类 Codec<S,T> 的抽象
# 方法按 metaClass 引用相等匹配。encoder/decoder 留给具象子类实现。
# ============================================================================

public abstract class _ProtoCodecBase<S,T> extends Codec<S,T>
{
    # Converter 契约：分块编码方向（每个消息块独立 encode 后转发下游
    # sink）。委托 encoder 落地（_FnConverter 的逐项 chunked 实现）。
    override public ChunkedConversionSink<S> startChunkedConversion( ChunkedConversionSink<T> sink )
    {
        Converter<S,T> enc = this.encoder
        ret enc.startChunkedConversion( sink )
    }
}

# ============================================================================
# ProtoCodec<T> — T ↔ ByteBuf（消息对象 ↔ ProtoBuf wire format）
# S = T（消息对象），T = ByteBuf（字节，分帧单位）
# ============================================================================

public class ProtoCodec<T> extends _ProtoCodecBase<T, ByteBuf>
{
    # encode 闭包：T → ByteBuf（内部用 PbWriter 编码）
    Function _encoderFn = null
    # decode 闭包：ByteBuf → T（内部用 PbReader 解码）
    Function _decoderFn = null

    _init_( Function encoderFn, Function decoderFn )
    {
        this._encoderFn = encoderFn
        this._decoderFn = decoderFn
    }

    # 闭包注入工厂：encoder/decoder 由调用方手写（例见 BaseTest
    # CodecFrameTest）。codegen 生成器为 P3。
    public static ProtoCodec<T> of<T>( Function encoderFn, Function decoderFn )
    {
        var c = ProtoCodec<T>( encoderFn, decoderFn )
        ret c
    }

    # ── Codec 契约：encoder / decoder 为闭包适配的 Converter ──

    override get Converter<T, ByteBuf> encoder()
    {
        Function fn = this._encoderFn
        var c = _FnConverter<T, ByteBuf>( fn )
        ret c
    }

    override get Converter<ByteBuf, T> decoder()
    {
        Function fn = this._decoderFn
        var c = _FnConverter<ByteBuf, T>( fn )
        ret c
    }

    # Converter 契约：分块编码方向（startChunkedConversion 的通用覆写）
    # 由基类 _ProtoCodecBase<S,T> 提供，逐消息委托 encoder 落地。

    # ── 分帧流式三件套（机制在 LengthPrefix，见 §8.1）──

    # 字节流 → 分帧多消息流（varint 长度前缀 + 半包续接，
    # RPC / 文件批量存储的标准做法）
    public Stream<T> decodeStream( ByteStream src )
    {
        ret LengthPrefix.decodeStream<T>( src, this )
    }

    public Stream<T> decodeStream( ByteStream src, Int32 maxFrame )
    {
        ret LengthPrefix.decodeStream<T>( src, this, maxFrame )
    }

    # 消息写入端：add(msg) → encode → 帧直写
    public StreamSink<T> encodeSink( ByteStream dst )
    {
        ret LengthPrefix.encodeSink<T>( dst, this )
    }

    public StreamSink<T> encodeSink( ByteStream dst, Int32 maxFrame )
    {
        ret LengthPrefix.encodeSink<T>( dst, this, maxFrame )
    }

    # chunked decoder：Stream<ByteBuf> → Stream<T>（处理半包）
    public Stream<T> bindStream( Stream<ByteBuf> chunks )
    {
        ret LengthPrefix.bindStream<T>( chunks, this )
    }

    public Stream<T> bindStream( Stream<ByteBuf> chunks, Int32 maxFrame )
    {
        ret LengthPrefix.bindStream<T>( chunks, this, maxFrame )
    }
}

# ============================================================================
# _FnConverter<S,T> — Function → Converter 适配器（闭包注入的落地实现）
# ============================================================================

public class _FnConverter<S,T> extends Converter<S,T>
{
    Function _fn = null

    _init_( Function fn )
    {
        this._fn = fn
    }

    override public T convert( S input )
    {
        Function fn = this._fn
        ret fn( input ) as T
    }

    # 无状态逐项分块转换：每个分块独立 convert 转发（适合逐消息
    # codec；有跨块状态的 codec 应提供自己的 chunked 实现）
    override public ChunkedConversionSink<S> startChunkedConversion( ChunkedConversionSink<T> sink )
    {
        Function fn = this._fn
        var k = _FnChunkSink<S,T>( fn, sink )
        ret k
    }
}

# ============================================================================
# _FnChunkSink<S,T> — 闭包逐项分块 sink（_FnConverter 的 chunked 支撑）
# ============================================================================

public class _FnChunkSink<S,T> extends ChunkedConversionSink<S>
{
    Function _fn = null
    ChunkedConversionSink<T> _sink = null

    _init_( Function fn, ChunkedConversionSink<T> sink )
    {
        this._fn = fn
        this._sink = sink
    }

    override public void add( S chunk )
    {
        Function fn = this._fn
        ChunkedConversionSink<T> sink = this._sink
        sink.add( fn( chunk ) as T )
    }

    override public void close()
    {
        this._sink.close()
    }
}
