import Std;

DirectoryTest
{
    # 测试目录创建、判断、删除
    static testCreateExistsDelete()
    {
        Console.println("===== DirectoryTest testCreateExistsDelete =====")
        string path = "test_dir_sl"

        # 初始不存在
        Console.println("exists before = " + IO.Directory.exists(path).toString())

        # 创建
        bool ok = IO.Directory.createDirectory(path)
        Console.println("createDirectory ret = " + ok.toString())
        Console.println("exists after create = " + IO.Directory.exists(path).toString())

        # 删除
        bool del = IO.Directory.delete(path)
        Console.println("delete ret = " + del.toString())
        Console.println("exists after delete = " + IO.Directory.exists(path).toString())
    }

    # 测试获取和设置当前工作目录
    static testCurrentDirectory()
    {
        Console.println("===== DirectoryTest testCurrentDirectory =====")
        string cwd = IO.Directory.getCurrentDirectory()
        Console.println("current dir = " + cwd)

        # 设置当前目录（切到父目录再切回来）
        # 不实际切换以避免影响其他测试，仅打印
        Console.println("getCurrentDirectory ok")
    }

    # 测试列出目录内容
    static testListFiles()
    {
        Console.println("===== DirectoryTest testListFiles =====")
        string path = "test_list_dir"

        # 创建测试目录
        IO.Directory.createDirectory(path)

        # 在目录中创建几个文件
        File.writeAllText(path + "/a.txt", "aaa")
        File.writeAllText(path + "/b.txt", "bbb")
        File.writeAllText(path + "/c.txt", "ccc")

        # 列出目录内容
        string files = IO.Directory.getFiles(path)
        Console.println("getFiles = " + files)

        # 清理
        File.delete(path + "/a.txt")
        File.delete(path + "/b.txt")
        File.delete(path + "/c.txt")
        IO.Directory.delete(path)
    }

    static fun()
    {
        Console.println("===== DirectoryTest =====")
        testCreateExistsDelete()
        testCurrentDirectory()
        testListFiles()
    }
}
