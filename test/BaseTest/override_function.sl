class Core.Object
{
    string toString()
    {
        ret "Object.toString()"
    }
    get int  hashCode()
    {
        ret 0
    }
}

VClass
{
    _VClassa = 0;
    _init_( int a )
    {

    }
    fun()
    {
        System.Console.WriteLine("vclass fun" );
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
    _init_(int a )
    {
        base._init_(a)
    }
    override fun()
    {
        System.Console.WriteLine("oclass fun" )
    }
    override get int hashCode()
    {
        ret 1001
    }
    override string toString()
    {
        ret "OClass.toString()"
    }
    fun2(){

    }
}

OL2Class extends OClass
{
    _init_(int a )
    {

    }
    _init_(string a)
    {

    }
}

OverrideFunction
{
    static fun()
    {
        global.println("========== override_function (start) ==========")
        VClass oclass = OClass(0)
        oclass.fun()
        oc2 = OClass(0)
        global.println("oc2.toString -> " + oc2.toString())
        global.println("oc2.hashCode -> " + oc2.hashCode.toString())
        global.println("========== override_function (end) ==========")
    }
}

# 与 MemberStaticFunction.sl 场景相同：子类重写实例方法与 Object 相关成员。
# 预期：fun 输出 oclass 分支；toString 为 OClass.toString()；hashCode 为 1001。
