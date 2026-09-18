# ============================================================================
# Std/Zip/Zip.sl — 压缩层：zlib DEFLATE 压缩 / 解压
#
# 纯静态工具类：本体在 C VM 端（csimple_lang/src/core/sl_core_zlib.c，
# 转发 third_party/zlib 1.3.2，仅内存 compress/uncompress），SL 层无状态。
# 容器格式：[4 字节小端原始长度][zlib 流]，自包含——解压无需外部尺寸提示。
# 跨模块访问：经 Core.ByteBuffer 的 handle / fromHandle 桥接注册表 id。
# 守卫约定：入参缓冲已释放（Released）或编解码失败时抛错；
# 压缩上界超出 Int32 范围时返回 0，由调用方自行判断。
# 便捷重载：compress(string)/compress(UInt8Array) 输入适配、
# decompressString/decompressBytes 输出适配（中间缓冲即时释放）。
# ============================================================================

public enum ZlibError extends Error
{
    CompressFailed   = { code = 1 }
    DecompressFailed = { code = 2 }
}

public class Zlib extends Object
{
    # ── 尺寸预查 ──

    # 压缩 inputSize 字节最坏情况下的容器尺寸（4 字节头 + zlib 上界）；
    # inputSize 为负或超出 Int32 容器上限时返回 0
    public static Int32 compressBound( Int32 inputSize )
    {
        ret SystemZlibCompressBound( inputSize )
    }

    # ── 压缩 / 解压 ──

    # 压缩 src 的可读区，返回新缓冲；src 索引不变
    public static ByteBuffer compress( ByteBuffer src ) throws
    {
        if SystemByteBufferIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemZlibCompress( src.handle )
        if id == 0
        {
            throw ZlibError.CompressFailed
        }
        ret ByteBuffer.fromHandle( id )
    }

    # 解压 SystemZlibCompress 产物，返回新缓冲；src 索引不变。
    # 数据损坏 / 截断 / 头部非法时抛 DecompressFailed
    public static ByteBuffer decompress( ByteBuffer src ) throws
    {
        if SystemByteBufferIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemZlibDecompress( src.handle )
        if id == 0
        {
            throw ZlibError.DecompressFailed
        }
        ret ByteBuffer.fromHandle( id )
    }

    # ── string / UInt8Array 入口（经 Core.ByteBuffer 适配，本体同上）──

    # 压缩 UTF-8 文本字节，返回新缓冲（中间缓冲即时释放，不依赖注册表 GC）
    public static ByteBuffer compress( string src ) throws
    {
        var b = ByteBuffer.fromString( src )
        var packed = Zlib.compress( b )
        b.release()
        ret packed
    }

    # 压缩整个字节数组，返回新缓冲
    public static ByteBuffer compress( UInt8Array src ) throws
    {
        var b = ByteBuffer.fromBytes( src )
        var packed = Zlib.compress( b )
        b.release()
        ret packed
    }

    # 解压并按 UTF-8 解码为字符串（内嵌 NUL 截断语义同 ByteBuffer.toString）
    public static string decompressString( ByteBuffer src ) throws
    {
        var b = Zlib.decompress( src )
        var text = b.toString()
        b.release()
        ret text
    }

    # 解压并导出为字节数组
    public static UInt8Array decompressBytes( ByteBuffer src ) throws
    {
        var b = Zlib.decompress( src )
        var bytes = b.toArray()
        b.release()
        ret bytes
    }
}
