import Application.Core;

#namespace Application.MFC;


Class1
{
    interface string interfaceFun1()  #必须定义返回值
}
Class2
{
    interface int interfaceFun2()
}
Class3  #如果被接口了，发现没有接口，需要报错
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

    _init_(int z1 )
    {
        _init_( 1, 2 );
        base._init_(z1+10);
    }

    # 必须实现 Fun函数  自动变成public
    override string interfaceFun1()    
    {  
        return "a";
    }
    override int interfaceFun2(){
        return 2;
    }

    interface Fun2()
}
Class2_1 extends Class1_1
{
    Fun2(){
        return "fun2";
    }
}


InterfaceTest
{
    static Fun()
    {
        Class1 c1 = Class1_1();
        v1 = c1.Fun()
        CSharp.Console.Write("-------------" + v1 );

        Class1 c2 = Class2_1();
        v2 = c2.Fun2();
        CSharp.Console.Write("-------------" + v2 );

    }
}


