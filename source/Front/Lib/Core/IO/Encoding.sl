# ============================================================================
# Core/IO/Encoding.sl — UTF-8 编解码器（STREAM_DESIGN.md §7.1）
#
# P2 范围：Utf8Codec（string ↔ Array<UInt8>，即全局别名 UInt8Array）。
# 取代 Core/Text/Encoding.sl 空壳（拼写错误类 Encdoing，未参与编译、
# 无代码引用，已删除）。
#
# 语义要点：
#   - SL 字符串即 UTF-8 C 字符串：C VM vm_sys_string_to_uint8_array 按
#     strlen 原样拷贝字节，encode 直接 SystemStringToUInt8Array；无
#     反向 syscall（UInt8Array → string），decode 经 ByteBuf 中转：
#     fromBytes(bytes).toString()（可读区 UTF-8 解码）。
#   - extends Codec<string, Array<UInt8>>：全部实参为具体类形态
#     （string 具体类；Array<UInt8> 中 UInt8 为具体基类，gen 实体
#     物化），泛型物化成立——区别于混合实参需中间基类的
#     ProtoCodec/JsonCodec，覆写签名直接写具体形态即可匹配。
#
# 已知偏差（详见设计文档 §17 实现注记）：
#   - 设计稿 Encoding 定位为通用字符集编码抽象（多字符集），P2 仅
#     落地 UTF-8 单 codec；ByteBuf.readString(Encoding) /
#     writeString(Encoding) 重载待后续多字符集需求再扩展。
# ============================================================================

public class Utf8Codec extends Codec<string, Array<UInt8>>
{
    # Codec 契约：分块编码方向（Converter.startChunkedConversion 通用
    # 覆写，委托 encoder 的逐项实现，同 _ProtoCodecBase 模式；本类
    # 实参全具体，直接以具体形态覆写）
    override public ChunkedConversionSink<string> startChunkedConversion( ChunkedConversionSink<Array<UInt8>> sink )
    {
        Converter<string, Array<UInt8>> enc = this.encoder
        ret enc.startChunkedConversion( sink )
    }

    # 编码器：string → UTF-8 字节数组（String.toUInt8Array 系统方法）
    override get Converter<string, Array<UInt8>> encoder()
    {
        function fn = function( string s )
        {
            ret s.toUInt8Array()
        }
        var c = _FnConverter<string, Array<UInt8>>( fn )
        ret c
    }

    # 解码器：UTF-8 字节数组 → string（经 ByteBuf 可读区解码中转）
    override get Converter<Array<UInt8>, string> decoder()
    {
        function fn = function( object v )
        {
            var arr = v as Array<UInt8>
            var buf = ByteBuf.fromBytes( arr )
            ret buf.toString()
        }
        var c = _FnConverter<Array<UInt8>, string>( fn )
        ret c
    }
}
