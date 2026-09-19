# ============================================================================
# Core/IO/JsonCodec.sl — JSON 编解码器（STREAM_DESIGN.md §7.1）
#
# P2 范围：JsonCodec<T> 类型化编解码，包装 BaseJson（Core/Text/BaseJson.sl）
# 的 parse / fromData / toData<T> / toJson 四原语为 Codec<T, string>。
#
# 已知偏差（详见设计文档 §17 实现注记）：
#   - 设计稿 §7.1 表格写 S=object T=string（非泛型），§7.2 用法示例又为
#     JsonCodec.of<Config>()（泛型用法）——两处自相矛盾。取泛型
#     JsonCodec<T>：编码侧 S=T 经 BaseJson.fromData(object) 中转，
#     解码侧 toData<T>() 还原；与 L828 用法兼容。
#   - 设计稿直接 extends Codec<T, string>；Front 泛型 extends 混合实参
#     （模板 + 具体类）不物化 gen 实体，故经参数到参数中间基类
#     _JsonCodecBase<S,T> 继承（_ProtoCodecBase 同模式先例），语义不变。
#   - 设计稿落位 Std/Text/JsonCodec.sl；因 Front 跨模块 extends 模板
#     类不支持（引用模块类装载时不重建模板映射字典，
#     HandleExtendClassTemplateMapRelation 查 Core.Converter<S,T> 失败），
#     P2 落位 Core/IO（Codec 机制家族同址）；待 Front 支持跨模块模板
#     继承后迁移。
# ============================================================================

# ============================================================================
# _JsonCodecBase<S,T> — JsonCodec 的参数化基类
#
# 承载 Converter.startChunkedConversion 的通用覆写（逐块独立编码后转发
# 下游 sink），使覆写签名保持模板形态（S/T 形参）与原始模板类
# Codec<S,T> 的抽象方法按 metaClass 引用相等匹配。encoder/decoder 留给
# 具象子类实现。
# ============================================================================

public abstract class _JsonCodecBase<S,T> extends Codec<S,T>
{
    override public ChunkedConversionSink<S> startChunkedConversion( ChunkedConversionSink<T> sink )
    {
        Converter<S,T> enc = this.encoder
        ret enc.startChunkedConversion( sink )
    }
}

# ============================================================================
# JsonCodec<T> — T ↔ string（对象 ↔ JSON 文本）
# S = T（业务对象），T = string（文本，分块单位）
# ============================================================================

public class JsonCodec<T> extends _JsonCodecBase<T, string>
{
    public static JsonCodec<T> of<T>()
    {
        var c = JsonCodec<T>()
        ret c
    }

    # ── Codec 契约：encoder / decoder 为 BaseJson 适配的 Converter ──

    override get Converter<T, string> encoder()
    {
        function fn = function( object v )
        {
            BaseJson j = BaseJson.fromData( v )
            ret j.toJson()
        }
        var c = _FnConverter<T, string>( fn )
        ret c
    }

    override get Converter<string, T> decoder()
    {
        function fn = function( string s )
        {
            BaseJson j = BaseJson.parse( s )
            ret j.toData<T>()
        }
        var c = _FnConverter<string, T>( fn )
        ret c
    }
}
