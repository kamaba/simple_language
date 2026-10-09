
public enum FileError extends Error
{
    NotFound = { code = 1 }
}

public class File
{
    # 判断文件是否存在
    # 类似 C# File.Exists(string path)
    static bool exists(string path)
    {
        ret SystemFileExists(path)
    }

    # 删除文件
    # 类似 C# File.Delete(string path)
    static bool delete(string path)
    {
        ret SystemFileDelete(path)
    }

    # 复制文件
    # 类似 C# File.Copy(string src, string dst)
    static bool copy(string src, string dst)
    {
        ret SystemFileCopy(src, dst)
    }

    # 移动/重命名文件
    # 类似 C# File.Move(string src, string dst)
    static bool move(string src, string dst)
    {
        ret SystemFileMove(src, dst)
    }

    # 获取文件大小（字节）
    # 类似 C# new FileInfo(path).Length
    static Int64 getSize(string path)
    {
        ret SystemFileGetSize(path)
    }

    # 读取文件全部文本
    # 类似 C# File.ReadAllText(string path)
    static string readAllText(string path)
    {
        ret SystemFileReadAllText(path)
    }

    # 写入文件全部文本（覆盖）
    # 类似 C# File.WriteAllText(string path, string content)
    static bool writeAllText(string path, string content)
    {
        ret SystemFileWriteAllText(path, content)
    }

    # 追加文本到文件末尾
    # 类似 C# File.AppendAllText(string path, string content)
    static bool appendText(string path, string content)
    {
        ret SystemFileAppendText(path, content)
    }

    # ── 流式接口（STREAM_DESIGN.md §4.4）──

    # 打开只读文件流
    # 类似 C# File.OpenRead(string path)
    static FileStream openRead( string path ) throws
    {
        var fs = FileStream( path, FileMode.Read, FileAccess.Read )
        ret fs
    }

    # 打开写文件流（存在则截断）
    # 类似 C# File.OpenWrite(string path)
    static FileStream openWrite( string path ) throws
    {
        var fs = FileStream( path, FileMode.Write )
        ret fs
    }

    # 读取文件全部字节
    # 类似 C# File.ReadAllBytes(string path)
    static UInt8Array readAllBytes( string path ) throws
    {
        var fs = FileStream( path, FileMode.Read, FileAccess.Read )
        var buf = fs.readAll( 0 )
        fs.close()
        ret buf.toArray()
    }

    # 写入字节到文件（覆盖）
    # 类似 C# File.WriteAllBytes(string path, byte[] bytes)
    static bool writeAllBytes( string path, UInt8Array bytes ) throws
    {
        var src = ByteBuffer.fromBytes( bytes )
        var fs = FileStream( path, FileMode.Write )
        fs.write( src )
        fs.close()
        ret true
    }
}
