import Application.Core;

#namespace Application.MFC;


Class1{
    interface Fun()
}

Class1_1 extends Class1
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

    # 必须实现 Fun函数
    Fun(){
        return "a";
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


