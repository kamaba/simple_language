# ============================================================================
# Core/IO/Serialize.sl — 序列化门面（STREAM_DESIGN.md §8.7）
#
# T 按 Codec 分两个方向编排：
#   - 字节向：Codec<T, ByteBuf>（BinaryCodec / ProtoCodec / Utf8Codec）
#   - 文本向：Codec<T, string>（JsonCodec 等）
# 七方法：toBytes / fromBytes / toText / fromText 同步；
#         toStream / fromStream 返回 Task（协程异步）；
#         toElementStream 返回 Stream<T>（varint 分帧多元素流）。
#
# 已知偏差（详见设计文档 §17 实现注记）：
#   - fromStream 读循环 read 异常（try? 吞 null 后 ret null）；decode
#     无 throws 声明（Codec 契约），无 SL 级可捕获异常，P4 若契约补
#     throws 再对齐错误透传（与 LengthPrefix 解码流 addError 路径一致）。
#   - fromStream 读循环套 ByteStream.readAll 语义（read 返回 0 = EOF）
#     与 LengthPrefix 解码泵同款 try? 模式；不用 readAll() 是因默认
#     参数无参调用暂无库内先例。
#   - 闭包函数不能声明 throws；闭包体内也不能以方法级泛型 T 作声明
#     类型（DefineStatements 解析不到，LengthPrefix / Stream 先例均为
#     具体类型声明 + T 仅现于泛型实参 / 返回表达式），故 decode 结果
#     直接 ret 不落局部变量。
#   - 设计稿落位 Std/Text/Serialize.sl；因 Front 跨模块 extends 模板
#     类不支持（引用模块类装载时不重建模板映射字典，
#     HandleExtendClassTemplateMapRelation 查 Core.Converter<S,T> 失败），
#     P2 落位 Core/IO（Codec 机制家族同址）；待 Front 支持跨模块模板
#     继承后迁移。
# ============================================================================

public class Serialize extends Object
{
    # ── 同步四件 ──

    public static ByteBuf toBytes<T>( T obj, Codec<T, ByteBuf> codec )
    {
        ByteBuf b = codec.encode( obj )
        ret b
    }

    public static T fromBytes<T>( ByteBuf b, Codec<T, ByteBuf> codec )
    {
        T obj = codec.decode( b )
        ret obj
    }

    public static string toText<T>( T obj, Codec<T, string> codec )
    {
        string s = codec.encode( obj )
        ret s
    }

    public static T fromText<T>( string s, Codec<T, string> codec )
    {
        T obj = codec.decode( s )
        ret obj
    }

    # ── 异步两件（协程 Task；错误吞 null，见文件头偏差注记）──

    # 对象编码后整体写出（编码同步、写盘异步）
    public static Task toStream<T>( T obj, ByteStream dst, Codec<T, ByteBuf> codec )
    {
        ByteBuf payload = codec.encode( obj )
        function f = function()
        {
            label io
            {
                try dst.write( payload )
                try dst.flush()
            }
            catch
            {
            }
        }
        Task t = Coroutine.spawnClosure0( f )
        ret t
    }

    # 整流读入后解码（读尽 EOF 收尾；读/解码异常吞 null）
    public static Task fromStream<T>( ByteStream src, Codec<T, ByteBuf> codec )
    {
        function f = function()
        {
            ByteBuf acc = ByteBuf( 4096 )
            while true
            {
                Int32 got = try? src.read( acc )
                if got == null
                {
                    ret null
                }
                if got <= 0
                {
                    break
                }
            }
            ret codec.decode( acc )
        }
        Task t = Coroutine.spawnClosure0( f )
        ret t
    }

    # ── 流式一件：字节流 → 分帧多元素流（varint 长度前缀，机制在
    #    LengthPrefix，见 §8.1；RPC / 文件批量存储的标准做法）──

    public static Stream<T> toElementStream<T>( ByteStream src, Codec<T, ByteBuf> codec )
    {
        ret LengthPrefix.decodeStream<T>( src, codec )
    }
}
