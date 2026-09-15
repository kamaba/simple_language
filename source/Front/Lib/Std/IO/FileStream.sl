# ============================================================================
# Std/IO/FileStream.sl — L0 文件流（STREAM_DESIGN.md §4.4）
#
# 语义要点：
#   - 原生句柄经 ByteBuf 承载：SystemFileOpen 返回 Int64 句柄（0 = 失败），
#     读写通过 dst.handle / src.handle 传 ByteBuf 注册表 id 零拷贝桥接。
#   - 同步阻塞 IO：P2 无 CORO_BLOCK_IO，大文件读写会阻塞整条协程
#     （已知偏差，见设计文档 §4.4 注记）。
#   - flush 只对可写句柄生效（fflush 输入流是 C 未定义行为）；
#     flushToDisk 对应 fsync / _commit，保证落盘。
#   - close 完全覆写：直接释放句柄并封口两个方向，不走基类
#     closeWrite -> flush 链（只读流在基类链上会误触 flush）。
# ============================================================================

# 文件打开方式（位组合）
# Read/Write/ReadWrite 决定 fopen 模式；Append 追加写；
# Create / Truncate 为「不存在则建、存在则清空」的重建语义
#（C# FileMode 对齐：Read=1 Write=2 ReadWrite=3 Append=4 Create=8 Truncate=16）。
public enum FileMode extends Int32
{
    Read = 1
    Write = 2
    ReadWrite = 3
    Append = 4
    Create = 8
    Truncate = 16
}

# 文件访问能力（只影响 SL 侧能力位 canRead / canWrite；
# 底层打开模式由 FileMode 决定）
public enum FileAccess extends Int32
{
    Read = 1
    Write = 2
    ReadWrite = 3
}

public class FileStream extends ByteStream
{
    Int64 _handle = 0
    string _path = null

    # ── 构造 ──

    _init_( string path, FileMode mode ) throws
    {
        this._init_( path, mode, FileAccess.ReadWrite )
    }

    _init_( string path, FileMode mode, FileAccess access ) throws
    {
        # enum -> Int32 不能用 as 转换（Front 不支持），显式映射为 C 侧位值；
        # Create/Truncate 的重建语义按 access 修正：需要读 -> 9（"w+b"），否则 8（"wb"）
        Int32 flags = 0
        if mode == FileMode.Read
        {
            flags = 1
        }
        elif mode == FileMode.Write
        {
            flags = 2
        }
        elif mode == FileMode.ReadWrite
        {
            flags = 3
        }
        elif mode == FileMode.Append
        {
            flags = 4
        }
        elif mode == FileMode.Create || mode == FileMode.Truncate
        {
            flags = 8
            if access == FileAccess.Read || access == FileAccess.ReadWrite
            {
                flags = 9
            }
        }
        else
        {
            flags = 2
        }
        Int64 h = 0
        this._path = path
        h = SystemFileOpen( path, flags )
        if h == 0
        {
            throw StreamIOError.OpenFailed
        }
        this._handle = h
        this._canRead = access == FileAccess.Read || access == FileAccess.ReadWrite
        this._canWrite = access == FileAccess.Write || access == FileAccess.ReadWrite
        this._canSeek = true
    }

    # ── 路径 ──

    public get string path()
    {
        ret this._path
    }

    # ── 位置 ──

    override get Int64 position()
    {
        if this._handle == 0
        {
            ret 0
        }
        ret SystemFileTell( this._handle )
    }

    override set void position( Int64 pos ) throws
    {
        this._ensureSeekable()
        Int64 r = SystemFileSeek( this._handle, pos, SeekOrigin.Begin )
        if r < 0
        {
            throw StreamIOError.InvalidPosition
        }
    }

    override get Int64 length()
    {
        if this._handle == 0
        {
            ret 0
        }
        ret SystemFileLength( this._handle )
    }

    override public Int64 seek( Int64 offset, SeekOrigin origin ) throws
    {
        this._ensureSeekable()
        # enum -> Int32 显式映射（与 C 侧 SEEK_SET/CUR/END 对齐：0/1/2）
        Int32 o = 0
        if origin == SeekOrigin.Begin
        {
            o = 0
        }
        elif origin == SeekOrigin.Current
        {
            o = 1
        }
        else
        {
            o = 2
        }
        Int64 pos = SystemFileSeek( this._handle, offset, o )
        if pos < 0
        {
            throw StreamIOError.InvalidPosition
        }
        ret pos
    }

    # ── 核心读写 ──

    override public Int32 read( ByteBuf dst ) throws
    {
        this._ensureReadable()
        if this._handle == 0 || dst.writableBytes <= 0
        {
            ret 0
        }
        Int32 n = SystemFileRead( this._handle, dst.handle, dst.writableBytes )
        if n < 0
        {
            throw StreamIOError.IoError
        }
        if n == 0
        {
            this._eof = true
        }
        ret n
    }

    override public void write( ByteBuf src ) throws
    {
        this._ensureWritable()
        Int32 want = src.readableBytes
        if want <= 0
        {
            ret
        }
        # 写背压：全部写出才返回；部分写视为 IO 错误
        Int32 n = SystemFileWrite( this._handle, src.handle )
        if n < 0 || n < want
        {
            throw StreamIOError.IoError
        }
    }

    # ── 冲刷 ──

    override public void flush() throws
    {
        # 只读句柄 fflush 是 C 未定义行为，直接跳过
        if this._handle == 0 || this._canWrite == false
        {
            ret
        }
        if SystemFileFlush( this._handle ) == false
        {
            throw StreamIOError.IoError
        }
    }

    # 强制落盘（fsync / _commit）
    public void flushToDisk() throws
    {
        if this._handle == 0
        {
            ret
        }
        if SystemFileSyncDisk( this._handle ) == false
        {
            throw StreamIOError.IoError
        }
    }

    # ── 生命周期 ──

    # 完全覆写：直接释放句柄并封口两个方向
    #（不走基类 close -> closeWrite -> flush 链，只读流会误触 flush）
    override public void close() throws
    {
        if this._handle != 0
        {
            SystemFileClose( this._handle )
            this._handle = 0
        }
        this._closedRead = true
        this._closedWrite = true
    }
}
