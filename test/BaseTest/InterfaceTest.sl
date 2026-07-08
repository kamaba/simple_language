import Application.Core;

#namespace Application.MFC;


interface Class1
{
    string interfaceFun1()  #必须定义返回值
}
interface Class2
{
    int interfaceFun2()
}
interface Class3  #如果被接口了，发现没有接口，需要报错
{
    interface3(){};
}

Class1_1 interface Class1,Class2,Class3
{
    x1 = 0;
    y1 = 0;
    z1 = 0;

    _init_( int _x1, int _y1 )
    {
        base._init_(_x1+1);
    }

    _init_(int _z1 )
    {
        z1 = _z1
    }

    # 必须实现 Fun函数  自动变成public
    override string interfaceFun1()    
    {  
        ret (x1 + y1 + z1).toString()
    }
    override int interfaceFun2(){
        ret (x1*y1 + z1);
    }

    interface Fun2()
}
Class2_1 extends Class1_1
{
    override Fun2(){
        ret "fun2";
    }
}


InterfaceTest
{
    static Fun()
    {
        Class1 c1 = Class1_1(1,2);
        v1 = c1.Fun()
        CSharp.Debug.Write("-------------" + v1 );

        Class1 c2 = Class1_1(100);
        v2 = c2.Fun()
        CSharp.Debug.Write("-------------" + v2 );

        Class1 c2 = Class2_1();
        v2 = c2.Fun2();
        CSharp.Debug.Write("-------------" + v2 );

        List<Class1> listc1 = new()
        listc1.add(v1)
        listc1.add(c2)

        for cc in listc1
        {
            System.Console.WriteLine( "if1" + cc.interfaceFun1() )
        }

    }
}


# 接口，必须以inetreface 定义结构，后， 在使用接口时使用interface 接口调用
# 如果有extends 类，则要看父类中是否有 interface 的方法， 如果有，则必须实现父类的方式，否则，子类不允许使用，父类如果是实例时，则不允许调用该方法  在实现的时候，必须使用override字段，标名，是个要实现的方法。