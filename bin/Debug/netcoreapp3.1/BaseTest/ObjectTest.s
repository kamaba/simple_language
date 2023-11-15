import Application.Core;

#namespace Application.MFC;
#namespace QS;

ClassT
{
    t = 111;
    static t2 = 112;

    __Init__( int _t )
    {
        this.t = _t;
    }
}
QS.ClassT2
{
    t = 113;
}
Class1
{
    __Init__( int _x1, int _y1 )
    {
        this.x1 = _x1;
        #this.y1 = _y1;
        #ct.t = 20;   #[error] 不允许这样的赋值，也不允许静态的赋值
        #this.ct1 = { t = 20 }; #[warning] 暂时有报错, 未确定是否支持这种方式
        #this.ct2 = ClassT( 114 );
        #this.ct3 = ClassT(){ t = 115 };
        #this.ct4 = QS.ClassT2();
        #this.ct5 = QS.ClassT2(){t = 116; };
    }
    x1 = 117;
    #y1 = 118;
    #ClassT ct1 = null;
    #ClassT ct2 = ClassT( 119 );
    #ClassT ct2_2 = ();
    #ClassT ct3 = ();
    #QS.ClassT2 ct4 = QS.ClassT2();
    #QS.ClassT2 ct5;
    #ct6 = ClassT( 120 );
    #ClassT ct7;
}
Class2 :: Class1
{
    __Init__( int _x1, int _y1, int _x2, int _y2 )
    {
        #!
        base.__Init__( _x1, _y2 );
        __Init__( _x1, _y1, _x2 );        
        this.x2++;
        this.x2 = this.x1 + _x2;
        this.y2 = _y2;
        !#
    }
    __Init__( int _x1, int _y1, int _x2 )
    {
        this.x2 = _x1;
    }
    #!
    GetX()
    {
        ret this.x2;
    }
    !#
    x2 = 0;
    y2 = 0;
}

ObjectTest
{
    static Fun()
    {
        c1 = Class1();        
        #c2 = Class2( 121,122,123,124 );
        #c3 = Class1( 125, 126 ){ x1 = 127 };
        #Class1 c4 = { x1 = 128, y1 = 129 };
        #Class2 c5 = { x1 = 130, y1 = 131, x2 = 132, y2 = 133 };
        #Class2 c6 = { ct1 = ClassT() };     # { ct1.t = 20; } 是不允许的

        CSharp.System.Console.Write("Class1 Value: " + c1.x1 );

        #aynn = {name = "mypc", wodm = Class1(), womd2 = Class2() };
        #if aynn.name == "mypc"
        #{
        #}
    }
}
#!
对象的创建使用
1. Class()的方式，就等于 new Class1()
2. Class(1,2) 使用函数的重载，传入参数的方式，
3. 每个函数，如果不在函数上边设置 [NoDefaultConstruction]的话，函数是都可以通过 Class()方式创建的
4. 在构造体中，不允许有返回类型，所以构造函数不能return 变量 的使用，在new Class()的时候，首先生成一个对象后，然后再进行构造处理，相当于构造只是一个处理函数，和普通的函数没有什么区别
相然，构造非静态的才能重构，静态的只能无参数类型
5. 在函数构造体中，只使用 this.当前类与继承类中变量的赋值，不能 this.其它类对象 如this.ct.t = 20; 不允许这种方式，只允许 this.ct = ClassT(2);的方式
所以，正常来说，类的构造，只允许当前变量的赋值，不允许操作其它类的值。
6. 继承使用 Class1( _x1, _y1 ); 可以调用父中的构造类的方式
7. 对象可以使用 Class c = {}的方式，创建，但，与类构造一样，只允许this.当前变量法制，否则语法不通过。
8. 同样的，不管是在构造体中，还是在{}方式中，使用都不允许有表达式的使用， 如果使用了[NoDefaultConstruction]标记的类，则不能直接使用{}方式 其它{}的方
式相当于 Class a = {var1=10;var2 = 20; } a = Class(); a.var1 = 10; a.var2 = 20; 
!#