import Std;

FileTest
{
    # 测试文件写入和读取
    static testWriteRead()
    {
        Console.println("===== FileTest testWriteRead =====")
        string path = "test_file_io.txt"
        string content = "Hello, File I/O!\nSecond line.\nThird line."

        # 写入文件
        bool ok = File.writeAllText(path, content)
        Console.println("writeAllText ret = " + ok.toString())

        # 验证文件存在
        bool ex = File.exists(path)
        Console.println("exists = " + ex.toString())

        # 读取文件
        string read = File.readAllText(path)
        Console.println("readAllText = " + read)

        # 获取文件大小
        Int64 size = File.getSize(path)
        Console.println("getSize = " + size.toString())

        # 清理
        bool del = File.delete(path)
        Console.println("delete = " + del.toString())
        Console.println("exists after delete = " + File.exists(path).toString())
    }

    # 测试文件追加
    static testAppend()
    {
        Console.println("===== FileTest testAppend =====")
        string path = "test_append.txt"

        # 初始写入
        File.writeAllText(path, "Line 1\n")
        Console.println("after write: " + File.readAllText(path))

        # 追加
        File.appendText(path, "Line 2\n")
        Console.println("after append: " + File.readAllText(path))

        # 再追加
        File.appendText(path, "Line 3\n")
        Console.println("after append2: " + File.readAllText(path))

        # 清理
        File.delete(path)
    }

    # 测试文件复制和移动
    static testCopyMove()
    {
        Console.println("===== FileTest testCopyMove =====")
        string src = "test_copy_src.txt"
        string dst = "test_copy_dst.txt"
        string mv = "test_moved.txt"

        File.writeAllText(src, "copy content")
        Console.println("src exists = " + File.exists(src).toString())

        # 复制
        bool cpOk = File.copy(src, dst)
        Console.println("copy ret = " + cpOk.toString())
        Console.println("dst exists = " + File.exists(dst).toString())
        Console.println("dst content = " + File.readAllText(dst))

        # 移动
        bool mvOk = File.move(src, mv)
        Console.println("move ret = " + mvOk.toString())
        Console.println("src exists after move = " + File.exists(src).toString())
        Console.println("mv exists = " + File.exists(mv).toString())

        # 清理
        File.delete(dst)
        File.delete(mv)
    }

    static fun()
    {
        Console.println("===== FileTest =====")
        testWriteRead()
        testAppend()
        testCopyMove()
    }
}
