local
{
    #!
    sqlite = Std.Sql.Sqlite3("d:/file.db")

    if( sqlite == null )
    {
        Console.print("初始化的时间发生了错误" )
        return
    }
    sqlite.flush()

    test( sqlstr )
    {
        sqlite.lock()
        arr = sqlite.exec(sqlstr)
        sqlite.unlock()
    }
    !#
    loc = LocalC(){ a =100 }


    testloc()
    {
        global.println( "localc=$loc.a " )
    }
}

LocalC
{
    a = 20
}

LocalTest
{
    static fun()
    {
        global.println("========== LocalTest (start) ==========")
        local.testloc()
        #!
        if local.sqlite
        {
            arr = local.test("select * from mysql")
            global.println("LocalTest: sqlite path ok, exec returned (check arr)")
        }
        else
        {
            global.println("LocalTest: sqlite is null (path d:/file.db 不可用或未实现)")
        }
        !#
        global.println("========== LocalTest (end) ==========")
    }
}

# 25.1.1 使用local 关键字，只能用来放到所以类的前边使用，即文件的最上边位置 
# 25.1.2 local 关键字，主要是为了解决本页的全局化的一些语句， 这样的话，可以在下边类里边，直接通过 local.的方式调用
#
# 测试说明：local 块在文件顶层初始化 Sqlite3；static fun 通过 local.sqlite / local.test 做集成 smoke。
# 预期：本机存在 d:/file.db 且驱动可用时进入查询分支；否则打印 null 提示，不视为失败。
