# ============================================================================
# Core/IO/BinaryCodec.sl — 二进制编解码器（STREAM_DESIGN.md §8.6）
#
# P2 范围：闭包注入版 Codec<object, ByteBuf>。encode/decode 逻辑由调用方
# 提供（System.Buffer 风格的字段序读写由调用方手写，与 ProtoBuf 共用
# varint 原语走 ByteBuf system method）。
#
# 已知偏差（详见设计文档 §17 实现注记）：
#   - 设计稿 of(Type t)（按字段声明顺序反射读写）依赖运行时反射，
#     SL 无运行时反射；P2 改为 of(Function encoder, Function decoder)
#     闭包注入（与 ProtoCodec 同一降级方式）。反射式 of(Type) 待 P3
#     反射 syscall 落地后补。
#   - 设计稿 extends Codec<object, ByteBuf>：object/ByteBuf 均为具体类
#     形态（全具体实参），直接 extends 可物化（Utf8Codec 先例），
#     无需中间基类。
#   - 设计稿落位 Std/Text/BinaryCodec.sl；因 Front 跨模块 extends 模板
#     类不支持（引用模块类装载时不重建模板映射字典，
#     HandleExtendClassTemplateMapRelation 查 Core.Converter<S,T> 失败），
#     P2 落位 Core/IO（Codec 机制家族同址）；待 Front 支持跨模块模板
#     继承后迁移。
# ============================================================================

public class BinaryCodec extends Codec<object, ByteBuf>
{
    # encode 闭包：object → ByteBuf（字段序字节布局由调用方手写）
    Function _encoderFn = null
    # decode 闭包：ByteBuf → object
    Function _decoderFn = null

    _init_( Function encoderFn, Function decoderFn )
    {
        this._encoderFn = encoderFn
        this._decoderFn = decoderFn
    }

    # 闭包注入工厂（反射式 of(Type) 见文件头偏差注记）
    public static BinaryCodec of( Function encoderFn, Function decoderFn )
    {
        var c = BinaryCodec( encoderFn, decoderFn )
        ret c
    }

    # ── Codec 契约 ──

    # Converter 契约：分块编码方向（逐块独立 encode 后转发下游 sink）
    override public ChunkedConversionSink<object> startChunkedConversion( ChunkedConversionSink<ByteBuf> sink )
    {
        Converter<object, ByteBuf> enc = this.encoder
        ret enc.startChunkedConversion( sink )
    }

    override get Converter<object, ByteBuf> encoder()
    {
        Function fn = this._encoderFn
        var c = _FnConverter<object, ByteBuf>( fn )
        ret c
    }

    override get Converter<ByteBuf, object> decoder()
    {
        Function fn = this._decoderFn
        var c = _FnConverter<ByteBuf, object>( fn )
        ret c
    }
}
