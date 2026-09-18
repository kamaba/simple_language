# ============================================================================
# Std/Encoding/Base64.sl — 编码层：Base64 RFC 4648 编码 / 解码
#
# 纯静态工具类：本体在 C VM 端（csimple_lang/src/core/sl_core_base64.c，
# 表驱动标准字母表编解码），SL 层无状态。
# 文本格式：RFC 4648 §4 标准字母表（A-Z a-z 0-9 + /），尾部 '=' 填充；
# 解码严格校验——长度非 4 倍数、字符集外字符、填充位置不合法
# （'=' 后出现数据字符 / 填充多于 2 个）均抛 DecodeFailed。
# 跨模块访问：经 Core.ByteBuf 的 handle / fromHandle 桥接注册表 id。
# 守卫约定：入参缓冲已释放（Released）或编解码失败时抛错；
# 编码上界超出 Int32 范围时返回 0，由调用方自行判断。
# 便捷重载：encode(string)/encode(UInt8Array)/encode(ByteStream)
# 输入适配、encodeToString/decodeString/decodeBytes 输出适配
# （中间缓冲即时释放）；encodeToString(string)/decodeString(string)
# 为文本到文本的高频快捷入口。
# Stream 桥接：encodeTo/decodeTo 把结果写入目标字节流
# （中间缓冲即时释放，数据归属目标流），供文件流 / 管道等场景衔接。
# ============================================================================

public enum Base64Error extends Error
{
    EncodeFailed = { code = 1 }
    DecodeFailed = { code = 2 }
}

public class Base64 extends Object
{
    # ── 尺寸预查 ──

    # 编码 inputSize 字节所需的 Base64 文本尺寸（((n+2)/3)*4 向上取整）；
    # inputSize 为负或超出 Int32 容器上限时返回 0
    public static Int32 encodeBound( Int32 inputSize )
    {
        ret SystemBase64EncodeBound( inputSize )
    }

    # ── 编码 / 解码 ──

    # 把 src 的可读区编码为 Base64 文本，返回新缓冲；src 索引不变
    public static ByteBuf encode( ByteBuf src ) throws
    {
        if SystemByteBufIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemBase64Encode( src.handle )
        if id == 0
        {
            throw Base64Error.EncodeFailed
        }
        ret ByteBuf.fromHandle( id )
    }

    # 解码 Base64 文本（本类或任何 RFC 4648 标准产物均可），返回新缓冲；
    # src 索引不变。文本非法（长度非 4 倍数 / 非法字符 / 填充位置不合法）
    # 时抛 DecodeFailed
    public static ByteBuf decode( ByteBuf src ) throws
    {
        if SystemByteBufIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemBase64Decode( src.handle )
        if id == 0
        {
            throw Base64Error.DecodeFailed
        }
        ret ByteBuf.fromHandle( id )
    }

    # ── string / UInt8Array 入口（经 Core.ByteBuf 适配，本体同上）──

    # 编码 UTF-8 文本字节，返回新缓冲（中间缓冲即时释放，不依赖注册表 GC）
    public static ByteBuf encode( string src ) throws
    {
        var b = ByteBuf.fromString( src )
        var encoded = Base64.encode( b )
        b.release()
        ret encoded
    }

    # 编码整个字节数组，返回新缓冲
    public static ByteBuf encode( UInt8Array src ) throws
    {
        var b = ByteBuf.fromBytes( src )
        var encoded = Base64.encode( b )
        b.release()
        ret encoded
    }

    # ── 输出适配（中间缓冲即时释放）──

    # 编码并按 UTF-8 解码为 Base64 文本字符串
    public static string encodeToString( ByteBuf src ) throws
    {
        var b = Base64.encode( src )
        var text = b.toString()
        b.release()
        ret text
    }

    # 文本到文本高频快捷入口：UTF-8 文本 → Base64 文本
    public static string encodeToString( string src ) throws
    {
        var b = ByteBuf.fromString( src )
        var encoded = Base64.encode( b )
        b.release()
        var text = encoded.toString()
        encoded.release()
        ret text
    }

    # 解码并按 UTF-8 还原为原始文本（内嵌 NUL 截断语义同 ByteBuf.toString）
    public static string decodeString( ByteBuf src ) throws
    {
        var b = Base64.decode( src )
        var text = b.toString()
        b.release()
        ret text
    }

    # 文本到文本高频快捷入口：Base64 文本 → 原始 UTF-8 文本
    public static string decodeString( string src ) throws
    {
        var b = ByteBuf.fromString( src )
        var decoded = Base64.decode( b )
        b.release()
        var text = decoded.toString()
        decoded.release()
        ret text
    }

    # 解码并导出为字节数组
    public static UInt8Array decodeBytes( ByteBuf src ) throws
    {
        var b = Base64.decode( src )
        var bytes = b.toArray()
        b.release()
        ret bytes
    }

    # ── Stream 桥接（L0 字节流，见 Core/IO/ByteStream.sl）──

    # 读源流至 EOF 并编码（源流读位置推进到末尾；中间缓冲即时释放）。
    # 慎用于无限流——readAll(0) 会一直读到 EOF
    public static ByteBuf encode( ByteStream src ) throws
    {
        var all = src.readAll()
        var encoded = Base64.encode( all )
        all.release()
        ret encoded
    }

    # 编码 src 并写入目标流（中间缓冲即时释放，数据归属 dst）
    public static void encodeTo( ByteStream dst, ByteBuf src ) throws
    {
        var encoded = Base64.encode( src )
        dst.write( encoded )
        encoded.release()
    }

    # 解码 src 并写入目标流（中间缓冲即时释放，数据归属 dst）
    public static void decodeTo( ByteStream dst, ByteBuf src ) throws
    {
        var decoded = Base64.decode( src )
        dst.write( decoded )
        decoded.release()
    }
}
