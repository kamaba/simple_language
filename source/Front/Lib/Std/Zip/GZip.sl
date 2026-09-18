# ============================================================================
# Std/Zip/GZip.sl — 压缩层：gzip RFC 1952 压缩 / 解压
#
# 纯静态工具类：本体在 C VM 端（csimple_lang/src/core/sl_core_gzip.c，
# 转发 third_party/zlib 1.3.2 deflate/inflate 的 windowBits=15+16 gzip
# wrapper），SL 层无状态。
# 容器格式：纯 RFC 1952 gzip 流（10 字节头 + DEFLATE 数据 + 8 字节
# CRC32/ISIZE 尾），与 gzip 命令行工具字节互通——产物可直接用
# gzip -d 解开，gzip 产物也可直接解压。
# 跨模块访问：经 Core.ByteBuffer 的 handle / fromHandle 桥接注册表 id。
# 守卫约定：入参缓冲已释放（Released）或编解码失败时抛错；
# 压缩上界超出 Int32 范围时返回 0，由调用方自行判断。
# 便捷重载：compress(string)/compress(UInt8Array)/compress(ByteStream)
# 输入适配、decompressString/decompressBytes 输出适配（中间缓冲即时释放）。
# Stream 桥接：compressTo/decompressTo 把结果写入目标字节流
# （中间缓冲即时释放，数据归属目标流），供文件流 / 管道等场景衔接。
# ============================================================================

public enum GZipError extends Error
{
    CompressFailed   = { code = 1 }
    DecompressFailed = { code = 2 }
}

public class GZip extends Object
{
    # ── 尺寸预查 ──

    # 压缩 inputSize 字节最坏情况下的 gzip 流尺寸（RFC 1952 头尾 + DEFLATE 上界）；
    # inputSize 为负或超出 Int32 容器上限时返回 0
    public static Int32 compressBound( Int32 inputSize )
    {
        ret SystemGzipCompressBound( inputSize )
    }

    # ── 压缩 / 解压 ──

    # 压缩 src 的可读区，返回新缓冲；src 索引不变
    public static ByteBuffer compress( ByteBuffer src ) throws
    {
        if SystemByteBufferIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemGzipCompress( src.handle )
        if id == 0
        {
            throw GZipError.CompressFailed
        }
        ret ByteBuffer.fromHandle( id )
    }

    # 解压 gzip 流（本类或 gzip 工具产物均可），返回新缓冲；src 索引不变。
    # 数据损坏 / 截断 / 尾部 ISIZE 与实际不符时抛 DecompressFailed
    public static ByteBuffer decompress( ByteBuffer src ) throws
    {
        if SystemByteBufferIsReleased( src.handle )
        {
            throw BufferError.Released
        }
        var id = SystemGzipDecompress( src.handle )
        if id == 0
        {
            throw GZipError.DecompressFailed
        }
        ret ByteBuffer.fromHandle( id )
    }

    # ── string / UInt8Array 入口（经 Core.ByteBuffer 适配，本体同上）──

    # 压缩 UTF-8 文本字节，返回新缓冲（中间缓冲即时释放，不依赖注册表 GC）
    public static ByteBuffer compress( string src ) throws
    {
        var b = ByteBuffer.fromString( src )
        var packed = GZip.compress( b )
        b.release()
        ret packed
    }

    # 压缩整个字节数组，返回新缓冲
    public static ByteBuffer compress( UInt8Array src ) throws
    {
        var b = ByteBuffer.fromBytes( src )
        var packed = GZip.compress( b )
        b.release()
        ret packed
    }

    # 解压并按 UTF-8 解码为字符串（内嵌 NUL 截断语义同 ByteBuffer.toString）
    public static string decompressString( ByteBuffer src ) throws
    {
        var b = GZip.decompress( src )
        var text = b.toString()
        b.release()
        ret text
    }

    # 解压并导出为字节数组
    public static UInt8Array decompressBytes( ByteBuffer src ) throws
    {
        var b = GZip.decompress( src )
        var bytes = b.toArray()
        b.release()
        ret bytes
    }

    # ── Stream 桥接（L0 字节流，见 Core/IO/ByteStream.sl）──

    # 读源流至 EOF 并压缩（源流读位置推进到末尾；中间缓冲即时释放）。
    # 慎用于无限流——readAll(0) 会一直读到 EOF
    public static ByteBuffer compress( ByteStream src ) throws
    {
        var all = src.readAll()
        var packed = GZip.compress( all )
        all.release()
        ret packed
    }

    # 压缩 src 并写入目标流（中间缓冲即时释放，数据归属 dst）
    public static void compressTo( ByteStream dst, ByteBuffer src ) throws
    {
        var packed = GZip.compress( src )
        dst.write( packed )
        packed.release()
    }

    # 解压 src 并写入目标流（中间缓冲即时释放，数据归属 dst）
    public static void decompressTo( ByteStream dst, ByteBuffer src ) throws
    {
        var plain = GZip.decompress( src )
        dst.write( plain )
        plain.release()
    }
}
