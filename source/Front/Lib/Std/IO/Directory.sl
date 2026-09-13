
enum IO.SearchOption
{
    TopDirectoryOnly = 0
}

public class IO.Directory
{
    # 判断目录是否存在
    # 类似 C# Directory.Exists(string path)
    static bool exists(string path)
    {
        ret SystemDirectoryExists(path)
    }

    # 创建目录（含父目录）
    # 类似 C# Directory.CreateDirectory(string path)
    static bool createDirectory(string path)
    {
        ret SystemDirectoryCreate(path)
    }

    # 删除空目录
    # 类似 C# Directory.Delete(string path)
    static bool delete(string path)
    {
        ret SystemDirectoryDelete(path)
    }

    # 获取当前工作目录
    # 类似 C# Directory.GetCurrentDirectory()
    static string getCurrentDirectory()
    {
        ret SystemDirectoryGetCurrent()
    }

    # 设置当前工作目录
    # 类似 C# Directory.SetCurrentDirectory(string path)
    static bool setCurrentDirectory(string path)
    {
        ret SystemDirectorySetCurrent(path)
    }

    # 列出目录下的文件和子目录名（换行分隔的字符串）
    # 类似 C# Directory.GetFileSystemEntries(string path)
    static string getFiles(string path)
    {
        ret SystemDirectoryGetFiles(path)
    }

    # 获取当前目录
    get string current()
    {
        ret SystemDirectoryGetCurrent()
    }
}
