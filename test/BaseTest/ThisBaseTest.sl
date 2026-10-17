
class Base1
{
    v1 = 1

    fun1()
    {
        ret 1
    }
}

class Base2 extends Base1
{
    v2 = 2

    override fun1()
    {
        ret 2
    }

    test1()
    {
    }
}

class Base3 extends Base2
{
    v3 = 1

    override test1()
    {
    }

    final fun1()
    {
        base.fun1()
        this.test1()
        ret 3
    }

    int get geta()
    {
        ret 20
    }

    print()
    {
        # this.v2 = 20  # 设计约束：多态下对父类布局字段的写入方式见文档
    }
}

class Base4 extends Base3
{
    test()
    {
        this.fun1()
        this.test1()
        a = this.geta
        global.println("ThisBaseTest Base4.geta -> " + a.toString())
    }
}

ThisBaseTest
{
    static fun()
    {
        global.println("========== ThisBaseTest (start) ==========")
        b4 = Base4()
        b4.test()
        global.println("========== ThisBaseTest (end) ==========")
    }
}

# 测试面向：继承链上的 this、base.fun1()、final 方法、get 访问器；子类中通过 this 调用父类实现与属性式 getter。
# 预期：Base4.test 调用 final fun1 返回 3 的路径由运行时绑定；geta 打印 20；无 extneds 拼写错误。
