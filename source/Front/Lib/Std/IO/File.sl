
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
}
