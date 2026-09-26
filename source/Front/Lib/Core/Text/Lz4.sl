# ============================================================================
# Core/Text/Lz4.sl — L0 压缩层：LZ4 块压缩 / 解压
# 设计契约：md/design/STREAM_DESIGN.md §15.6
#
# 纯静态工具类：本体在 C VM 端（csimple_lang/src/core/sl_core_lz4.c，
# 转发 third_party/lz4），SL 层无状态。
# 容器格式：[4 字节小端原始长度][LZ4 块]，自包含——解压无需外部尺寸提示。
# 守卫约定：入参缓冲已释放（Released）或编解码失败时抛 Lz4Error；
# 压缩上界（单块约 2GB 之外）返回 0，由调用方自行判断。
#
# 命名偏差（详见设计文档 §15.6 实现注记）：
#   - 设计文档原计划通用 Compress/Decompress/CompressBound（GZip + Lz4
#     共用 algo 参数）；实现按"机制优先、不做推测性参数"采用 LZ4 专用
#     命名 SystemLz4*，GZip 待落地时再扩展
# ============================================================================

public enum Lz4Error extends Error
{
    CompressFailed   = { code = 1 }
    DecompressFailed = { code = 2 }
}

public class Lz4 extends Object
{
    # ── 尺寸预查 ──

    # 压缩 inputSize 字节最坏情况下的容器尺寸（4 字节头 + LZ4 上界）；
    # inputSize 为负或超出 LZ4 单块上限（约 2GB）时返回 0
    public static Int32 compressBound( Int32 inputSize )
    {
        ret SystemLz4CompressBound( inputSize )
    }

    # ── 压缩 / 解压 ──

    # 压缩 src 的可读区，返回新缓冲；src 索引不变
    public static ByteBuffer compress( ByteBuffer src ) throws
    {
        src._ensureLive()
        var id = SystemLz4Compress( src._bid )
        if id == 0
        {
            throw Lz4Error.CompressFailed
        }
        var b = ByteBuffer()
        SystemByteBufferDestroy( b._bid )
        b._bid = id
        ret b
    }

    # 解压 SystemLz4Compress 产物，返回新缓冲；src 索引不变。
    # 数据损坏 / 截断 / 头部非法时抛 DecompressFailed
    public static ByteBuffer decompress( ByteBuffer src ) throws
    {
        src._ensureLive()
        var id = SystemLz4Decompress( src._bid )
        if id == 0
        {
            throw Lz4Error.DecompressFailed
        }
        var b = ByteBuffer()
        SystemByteBufferDestroy( b._bid )
        b._bid = id
        ret b
    }
}
