# ============================================================================
# Std/IO/StdStream.sl — 标准输入/输出流（STREAM_DESIGN.md §4.5）
#
# 语义要点：
#   - 复用现有 SystemPrint / SystemReadLine syscall，统一到 ByteStream
#     抽象后，「Json.encode(obj, StdOutStream.shared())」这类把序列化目标
#     从文件泛化为任意流的写法即可成立。
#   - StdInStream 行缓冲：一次 read 返回一行（含行尾 '\n'）。
#     vm_sys_readline 在 EOF 返回空串，无法区分「空行」与「EOF」——
#     已知偏差：空行会被当作 EOF（详见设计文档 §18 偏差清单）。
#   - StdErrStream 复用 SystemPrint 输出到 stdout：当前无 stderr syscall
#     （已知偏差，见 §18）；接入 stderr 句柄后仅需替换 write 内一处调用。
#   - 三个流均 canSeek = false（基类默认），单向（另一方向抛 NotSupported）。
# ============================================================================

public class StdInStream extends ByteStream
{
    static StdInStream _instance = null

    # 进程级标准输入（单例）
    static StdInStream shared()
    {
        if StdInStream._instance == null
        {
            StdInStream._instance = StdInStream()
        }
        ret StdInStream._instance
    }

    override _init_()
    {
        this._canWrite = false
    }

    # 行缓冲读取：一次 read 返回一行（含 '\n'）；0 = EOF
    override public Int32 read( ByteBuffer dst ) throws
    {
        this._ensureReadable()
        if this._eof
        {
            ret 0
        }
        string line = SystemReadLine()
        if line == ""
        {
            this._eof = true
            ret 0
        }
        Int32 before = dst.writerIndex
        dst.writeString( line + "\n" )
        ret dst.writerIndex - before
    }

    override public void write( ByteBuffer src ) throws
    {
        this._ensureWritable()
    }

    override public void flush() throws
    {
        # stdin 无用户态缓冲，flush 为空操作
    }
}

public class StdOutStream extends ByteStream
{
    static StdOutStream _instance = null

    # 进程级标准输出（单例）
    static StdOutStream shared()
    {
        if StdOutStream._instance == null
        {
            StdOutStream._instance = StdOutStream()
        }
        ret StdOutStream._instance
    }

    override _init_()
    {
        this._canRead = false
    }

    override public Int32 read( ByteBuffer dst ) throws
    {
        this._ensureReadable()
        ret 0
    }

    # 整段写出：按 UTF-8 解码 src 可读区后打印（不追加换行）
    override public void write( ByteBuffer src ) throws
    {
        this._ensureWritable()
        Int32 n = src.readableBytes
        if n <= 0
        {
            ret
        }
        string text = src.readString( n )
        SystemPrint( text, null )
    }

    override public void flush() throws
    {
        # SystemPrint 即时输出（无用户态缓冲），flush 为空操作
    }
}

public class StdErrStream extends ByteStream
{
    static StdErrStream _instance = null

    # 进程级标准错误（单例）
    static StdErrStream shared()
    {
        if StdErrStream._instance == null
        {
            StdErrStream._instance = StdErrStream()
        }
        ret StdErrStream._instance
    }

    override _init_()
    {
        this._canRead = false
    }

    override public Int32 read( ByteBuffer dst ) throws
    {
        this._ensureReadable()
        ret 0
    }

    # 偏差：当前无 stderr syscall，复用 stdout（见文件头注记）
    override public void write( ByteBuffer src ) throws
    {
        this._ensureWritable()
        Int32 n = src.readableBytes
        if n <= 0
        {
            ret
        }
        string text = src.readString( n )
        SystemPrint( text, null )
    }

    override public void flush() throws
    {
        # 同 StdOutStream：即时输出，flush 为空操作
    }
}
