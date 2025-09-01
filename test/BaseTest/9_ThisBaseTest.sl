

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

class Base3 extends
{
    v3 = 1

    override test1()
    {

    }

    final fun1()
    {
        base.fun1()  #调用父类中的函数
        this.test1()

        ret 3
    }

    int get geta()
    {
        ret 20
    }

    print()
    {
        this.v2 = 20;   #不允许 应该使用base.v2;
    }
}

class Base4 extneds Base3
{
    test()
    {

        this.fun1()         #可以使用 不像成员变量 必须是this只能指定使用本类的成员 
        this.test1();   
        this.v3 = 20        #错误只能用base

        a = this.geta;      #这个可以调用，因为这个是个函数
    }
}
