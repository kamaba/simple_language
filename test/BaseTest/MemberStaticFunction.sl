VClass
{
    fun()
    {
        System.Console.Write("vclass fun" );
    }
    override get int hashCode()
    {
        ret 100
    }
    override string toString()
    {
        ret "VClass.toString()"
    }
    static staticFun()
    {

    }
    final finalFun()
    {

    }
}

OClass extends VClass
{
    override fun()
    {
        System.Console.Write("oclass fun" )
    }
    override get int hashCode()
    {
        ret 1001
    }
    override string toString()
    {
        ret "OClass.toString()"
    }
}

OverrideFunction
{
    static fun()
    {
        global.println("========== OverrideFunction (start) ==========")
        VClass oclass = OClass()
        oclass.fun()
        oc2 = OClass()
        global.println("oc2.toString -> " + oc2.toString())
        global.println("oc2.hashCode -> " + oc2.hashCode.toString())
        global.println("========== OverrideFunction (end) ==========")
    }
}

# 测试说明：验证子类 OClass 对 fun、toString、hashCode 的 override 是否生效。
# 预期：打印 "oclass fun"（经 fun）、toString 含 OClass、hashCode 为 1001。
