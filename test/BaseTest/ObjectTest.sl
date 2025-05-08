import Application.Core;

namespace Application;
namespace Application.Core;


ClassT
{
    ClassT2
    {
        int t2 = 100;
    }
    ClassT2 t3     ;
    #array<int> arr3
    static t2 = 112;
    t = 0;

    _init_( int _t123123 )
    {
        this.t = _t123123;
    }
    ClassT2 GetT()
    {
        ret this.t3
    }
}
QS.ClassT2
{
    t = 113;
}
Class1
{
    _init_( int _x1, int _y1 )
    {
        #this.x1 = _x1;
        #this.y1 = _y1;
        #this.ct1.t = 20;   #[error] 不允许这样的赋值，也不允许静态的赋值
        #this.ct1 = { t = 20 }; #[error] 暂时有报错, 不支持该方法使用 只支持 Class1()方式 这种方法，只适合于 临时语句 或者是定义成员变量时 Class1 a = {t1 = 1, t2 = 2};的方式
        #this.ct2 = ClassT( 114 );
        #this.ct3 = ClassT(){ t = 115 };
        #this.ct4 = QS.ClassT2();
        #this.ct5 = QS.ClassT2(){t = 116; };
    }
    _init_( ClassT _t, ClassT _t2 )
    {
        this.ct3 = _t;
        this.ct3 = _t2;
    }
    x1 = 117;
    y1 = 118;
    #ClassT ct1 = null;               #测试对象赋值为空
    #ClassT ct2 = ClassT( 119 );        # 测试对象创建  _init_方式
    #ClassT ct2_2 = ();
    ClassT ct3 = ();
    #QS.ClassT2 ct4 = QS.ClassT2();
    #QS.ClassT2 ct5;
    #ct6 = ClassT( 120 );
    #ClassT ct7;
    #arr = [{name = "ed"; my = 22;},{name="wy"; my = 23; }]  #[error]  必须申请为List<object> 类型后 可以使用 List<object> arr = new List<object>(){ {}, {} }; 的方式进行初始化赋值，如果是数组形式，不支持 动态数据
    #dynclass = { q1 = 23; a2 = 25; aws = "xgla"; q4 = ClassT(); }  #[error] 不允许，因为在前期赋值的时候，必须知道该类型  后期如果做的话，也是先提取格式，然后在运行类的时候进行赋值
}
Class2 extends Class1
{
    public class Class2_2 extends Class2 interface Class1,QS.ClassT2
    {
        int e2c;
    }
    _init_( int _x1, int _y1, int _x2, int _y2 )
    {
        #!
        base._init_( _x1, _y2 );
        _init_( _x1, _y1, _x2 );   
        !#     
        this.x2++;
        this.x2 = this.x1 + _x2;
        this.y2 = _y2;
    }
    _init_( int _x1, int _y1, int _x2 )
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


data QS.Data1
{
    ct = ClassT(20) 
    x1 = 20
    x2 = [-11.3f,2,3]
    x3 = [{a = 10}, {a = 20} ]
    x4 = {
         a = 30
    }
    x41 = 
    {
        
    }
}
#!
data Data2
{
    d11 = Data1()
}
data Data11 binds Data1,Data2
{
    x5 = [1,2,3]
}
!#

ObjectTest
{
    static Fun()
    {
        Data1 d1 = Data1(){ ct = ClassT(30) }

        #dynamic d1 = {t1 = 2, t2 = 3 }
        #!
        a20 = 10000 + 20;
        a21 = ClassT(10){t=1}
        d2 = { ct = ClassT(20), childd1 = { a3 = { qx = 1024 } }, ch2 = [1,2,3,4],  ha=a21, ch3 = [ {a=20}, {a = {ax = 10241 } } ], y2 = "sss" }  # 相当于data d2 = {}
        md2str = d2.y2;
        r20 = d2.childd1.a3.qx;
        r21 = d2.ch2.$2;
        r22 = d2.ha.t
        r23 = d2.ch3.$2.a.ax
        !#
        #ct111 = ClassT(100){ t = 1 }        
        #vv1 = ClassT().t;      #不允许 newclass只允许 使用创建新的变量方式
        #c1 = Class1(ClassT(), ct111 );    #传参的时候，如果是 ClassName(){} 不支持后边的{} 只支持ClassName(1,2,3) 的形式使用 
        #c2 = Class2( 121,122,123,124 );
        #c3 = Class1( 125, 126 ){ x1 = 127 };
        #Class1 c4 = { x1 = 128, y1 = 129 };
        #Class2 c5 = { x1 = 130, y1 = 131, x2 = 132, y2 = 133 };
        #Class2 c6 = { ct1 = ClassT() };     # { ct1.t = 20; } 是不允许的

        #c1.x1 = 250                                                  #测试调用对象+赋值对象

        

        #t2 = c1.ct3.GetT().t2;                                       #测试调用对象链
        #t2  = c1.ct3.t;
        
        #CSharp.System.Console.Write("Class1 Value: " + d1.t1 );     

        #CSharp.System.Console.Write("Class1 Value: " + c1.ct3.t );     #测试调用对象链

        #aynn = {name = "mypc", wodm = Class1(), womd2 = Class2() };    #测试匿名对象
        #if aynn.name == "mypc"
        #{
             #CSharp.System.Console.Write("aynn is mypc" );
        #}        
    }
}

#!
对象的创建使用
1. Class()的方式，就等于 new Class1()
2. Class(1,2) 使用函数的重载，传入参数的方式 其实调用的是已配置 __Init( int a, int b )的函数，
3. 每个函数，如果不在函数上边设置 [@NoDefaultConstruction]的话，函数是都可以通过 Class()方式创建的
4. 在构造体中，不允许有返回类型，所以构造函数不能return 变量 的使用，在new Class()的时候，首先生成一个对象后，然后再进行构造处理，相当于构造只是一个处理函数，和普通的函数没有什么区别
相然，构造非静态的才能重构，静态的只能无参数类型
5. 在函数构造体中，只使用 this.当前类与继承类中变量的赋值，不能 this.其它类对象 如this.ct.t = 20; 不允许这种方式，只允许 this.ct = ClassT(2);的方式
所以，正常来说，类的构造，只允许当前变量的赋值，不允许操作其它类的值。
6. 继承使用 base._init_( _x1, _y1 ); 可以调用父中的构造类的方式
7. 对象可以使用 Class c = {}的方式，创建，但，与类构造一样，只允许this.当前变量法制，否则语法不通过。
8. 同样的，不管是在构造体中，还是在{}方式中，使用都不允许有表达式的使用， 如果使用了[NoDefaultConstruction]标记的类，则不能直接使用{}方式 其它{}的方
式相当于 Class a = {var1=10;var2 = 20; } a = Class(); a.var1 = 10; a.var2 = 20; 
9. 不允许 Class1().Fun() 这种的调用，必须 是1-7的情况，其它一律不行
    1. Class1 varname = Class1();   Class1 varname = Class2()
    2. Class1 varname;
    3. Class1 varname = (1,2)   在初始化后调用_init_(1,2)函数 
    4. Class1 varname = {var1 = 20; var2 = 30; }  在初始化时直接给某个成员赋值
    5. varname = Class1(1,2){var1 = 20; var2 = 30; }   即调用_init_(1,2)  先给变量赋值，再进行初始化调用
!#

#!
111
!#