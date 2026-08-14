# 这是一行注释


#! 

中间注释 

!#

##!
注释 一层 

    #!
        注释二层
    !#

    #! 注释二层 2

    !#
!##

#!md
md类型注释
!#

CommitTest   #注释类
{
    test( int a = 20 #! 这里是注释 !# )
    {
        global.println("Commit.test() a = " + a)
    }

    static fun()
    {
        if true  #兼容语句后边的注释
        {

        }
        global.println("========== CommitTest (start) ==========")
        global.println("Commit.test 默认参数演示（空体，仅编译/链接 smoke）")
        CommitTest c = CommitTest()
        c.test()
        global.println("========== CommitTest (end) ==========")
    }
}

# 测试说明：验证块注释 #! !#、#!md !#md、##! 嵌套及参数行内 #! !# 注释不影响解析。
# 预期：可编译；static fun 输出三段横幅；Commit.test() 无输出但调用成功。
