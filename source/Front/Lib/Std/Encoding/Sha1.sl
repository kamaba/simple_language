# ============================================================================
# Std/Encoding/Sha1.sl — 编码层：SHA-1 报文摘要（FIPS 180-1）
#
# 纯静态工具类：本体在 C VM 端（csimple_lang/src/core/sl_core_sha1.c，
# .NET 权威实现交叉验证通过），SL 层无状态。
# 摘要语义：任意长度输入 -> 20 字节（160 位）大端摘要；
# 跨模块访问：经 Core.ByteBuffer 的 handle / fromHandle 桥接注册表 id。
# 守卫约定：入参缓冲已释放（Released）或计算失败时抛错。
# 便捷重载：digest(string) 文本输入适配、digestHex 输出
# 40 字符小写十六进制串（中间缓冲即时释放，不依赖注册表 GC）。
# 主要消费方：Net/Websocket.sl RFC 6455 握手 Sec-WebSocket-Accept 计算。
# ============================================================================

public enum Sha1Error extends Error
{
    DigestFailed = { code = 1 }
}

public class Sha1 extends Object
{
    # ── 摘要 ──

    # 计算 src 可读区的 SHA-1 摘要，返回新 20 字节缓冲；src 索引不变
    public static ByteBuffer digest( ByteBuffer src ) throws
    {
        if SystemByteBufferIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemSha1( src.handle )
        if id == 0
        {
            throw Sha1Error.DigestFailed
        }
        ret ByteBuffer.fromHandle( id )
    }

    # 计算 UTF-8 文本字节的 SHA-1 摘要，返回新缓冲（中间缓冲即时释放）
    public static ByteBuffer digest( string src ) throws
    {
        var b = ByteBuffer.fromString( src )
        var d = Sha1.digest( b )
        b.release()
        ret d
    }

    # ── 输出适配（中间缓冲即时释放）──

    # 摘要并输出为 40 字符小写十六进制串
    public static string digestHex( ByteBuffer src ) throws
    {
        var d = Sha1.digest( src )
        var text = d.toHex()
        d.release()
        ret text
    }

    # 文本到文本高频快捷入口：UTF-8 文本 -> 小写十六进制摘要
    public static string digestHex( string src ) throws
    {
        var b = ByteBuffer.fromString( src )
        var text = Sha1.digestHex( b )
        b.release()
        ret text
    }
}
