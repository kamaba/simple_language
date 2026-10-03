import Std;

CsvTest
{
    # 测试继承 BaseCsv 的构造与基础能力透传
    static testInherit()
    {
        Console.println("===== CsvTest testInherit =====")
        # 继承 BaseCsv 的文本构造（首行为表头）
        var c = Text.Csv("name,age\nalice,30\nbob,25")
        Console.println("rowCount = " + c.rowCount.toString())                      # 2
        Console.println("columnCount = " + c.columnCount.toString())                # 2
        Console.println("getIntByName(0,age,0) = " + c.getIntByName(0, "age", 0))   # 30
        Console.println("getStrByName(1,name) = " + c.getStrByName(1, "name", ""))  # bob

        # 继承的查询/统计/修改/序列化
        c.sortBy("age", false)
        Console.println("sorted first name = " + c.getStrByName(0, "name", ""))     # alice
        Console.println("sum(age) = " + c.sum("age").toString())                    # 55
        c.setValueByName(0, "age", 31)
        Console.println("toCsv = " + c.toCsv())
    }

    # 测试 load/save/saveAs/reload 与关联文件信息
    static testLoadSave()
    {
        Console.println("===== CsvTest testLoadSave =====")
        string path = "test_csv_io.csv"
        File.writeAllText(path, "name,age\nalice,30\nbob,25")

        # 静态工厂加载
        var c = Text.Csv.load(path)
        Console.println("load rowCount = " + c.rowCount.toString())                 # 2
        Console.println("path = " + c.path)                                         # test_csv_io.csv
        Console.println("fileExists = " + c.fileExists.toString())                  # true
        Console.println("fileSize = " + c.fileSize().toString())                    # 24

        # 修改后写回关联路径
        c.setValueByName(0, "age", 99)
        Console.println("save ret = " + c.save().toString())                        # true

        # 重新加载验证写回生效
        var c2 = Text.Csv.load(path)
        Console.println("age(0) after save = " + c2.getIntByName(0, "age", 0))      # 99

        # saveAs 换路径并记录关联路径
        string path2 = "test_csv_io_2.csv"
        Console.println("saveAs ret = " + c2.saveAs(path2).toString())              # true
        Console.println("path after saveAs = " + c2.path)                           # test_csv_io_2.csv

        # reload 按关联路径重新加载
        Console.println("reload ret = " + c2.reload().toString())                   # true
        Console.println("reload rowCount = " + c2.rowCount.toString())              # 2

        # 清理
        File.delete(path)
        File.delete(path2)
    }

    # 测试 readFrom/loadNoHeader/loadDelimited/appendTo
    static testReadAppend()
    {
        Console.println("===== CsvTest testReadAppend =====")
        string path = "test_csv_read.csv"
        File.writeAllText(path, "name,age\nalice,30\nbob,25")

        # readFrom 整体替换当前表（旧列 x,y 被替换为文件的 name,age）
        var c = Text.Csv("x,y\n9,8")
        Console.println("before readFrom rowCount = " + c.rowCount.toString())      # 1
        Console.println("readFrom ret = " + c.readFrom(path).toString())            # true
        Console.println("after readFrom rowCount = " + c.rowCount.toString())       # 2
        Console.println("after readFrom age(0) = " + c.getIntByName(0, "age", 0))   # 30

        # 无表头加载（列名自动补列号）
        string pathNoH = "test_csv_noh.csv"
        File.writeAllText(pathNoH, "alice,30\nbob,25")
        var nh = Text.Csv.loadNoHeader(pathNoH)
        Console.println("loadNoHeader getColumnName(0) = " + nh.getColumnName(0))   # col0
        Console.println("loadNoHeader col0(0) = " + nh.getStrByName(0, "col0", "")) # alice

        # 自定义分隔符加载
        string pathSemi = "test_csv_semi.csv"
        File.writeAllText(pathSemi, "x;y\n1;2")
        var semi = Text.Csv.loadDelimited(pathSemi, true, ";")
        Console.println("loadDelimited x(0) = " + semi.getStrByName(0, "x", ""))    # 1
        Console.println("loadDelimited y(0) = " + semi.getIntByName(0, "y", 0))     # 2

        # appendTo：文件不存在时整表写入（含表头）
        string path3 = "test_csv_append.csv"
        Console.println("appendTo ret = " + c.appendTo(path3).toString())           # true
        var c3 = Text.Csv.load(path3)
        Console.println("after append rowCount = " + c3.rowCount.toString())        # 2

        # appendTo：文件已存在时只追加数据行（不含表头）
        Console.println("appendTo again ret = " + c.appendTo(path3).toString())     # true
        var c4 = Text.Csv.load(path3)
        Console.println("after append2 rowCount = " + c4.rowCount.toString())       # 4

        # 失败路径：文件不存在
        Console.println("readFrom missing = " + c.readFrom("no_such_csv_file.csv").toString())  # false

        # 清理
        File.delete(path)
        File.delete(pathNoH)
        File.delete(pathSemi)
        File.delete(path3)
    }

    static fun()
    {
        Console.println("===== CsvTest =====")
        testInherit()
        testLoadSave()
        testReadAppend()
    }
}
