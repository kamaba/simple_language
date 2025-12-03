import Std
import CSharp.SimpleLanguage
import CSharp.System


local
{
    sqlite = Sqlite3("d:/file.db")

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
}

LocalTest
{
    static fun()
    { 
        if local.sqlite 
        {
            arr = local.test("select * from mysql")
        }
    }
}
# 25.1.1 使用local 关键字，只能用来放到所以类的前边使用，即文件的最上边位置 
# 25.1.2 local 关键字，主要是为了解决本页的全局化的一些语句， 这样的话，可以在下边类里边，直接通过 local.的方式调用