#import Application.Core;
import CSharp.System



namespace Core
{
    class Object
    {
        public void _init_()
        {

        }

        public string toString()
        {
            ret ""
        }
    }
    class Byte
    {
        
    }
    class Boolean
    {

    }
    class SByte
    {
        
    }
    class Int16
    {
        
    }
    class UInt16
    {
        
    }
    class Int32
    {
        
    }
    class UInt32
    {
        
    }
    class Int64
    {
        
    }
    class UInt64
    {
        
    }
    class Float32
    {
        
    }
    class Float64
    {
        _init_(Float64 f)
        {

        }
    }
    class String
    {
        _init_( String str )
        {

        }
    }

}

Class1
{
    c1_1 = 10
}
Class2 extends Class1
{
    c2_1 = 20
    Class1 c2_2 = new()
}
Class3
{
    c3_1 = Class2()
    Class2 GetClass2()
    {
        ret this.c3_1
    }
}
Class4 extends Class3
{
    Class3 c4_1 = null
    int c4_2 = 0
    Class3 c4_3 = new()
}

CallLinkTest
{
    static fun()
    {
        Class4 c4 = Class4()
        c4.c4_1 = Class3()
        c4.c4_3.GetClass2().c2_2.c1_1 = 40
        c4.GetClass2().c2_1 = c4.c4_3.GetClass2().c2_2.c1_1;
        newc1 = c4.GetClass2().c2_1;
        #c4.c41 = newc1.$2;
        #c4.c41 = newc1.index;
        #newcx1 = newc1.value;
        #result1 = newc1;
        System.Console.WriteLine("Class1 Value: " + newc1 )

        
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
        
        CSharp.System.Debug.Write("Class1 Value: " + vx );     

        #CSharp.System.Debug.Write("Class1 Value: " + c1.ct3.t );     #测试调用对象链

        #aynn = {name = "mypc", wodm = Class1(), womd2 = Class2() };    #测试匿名对象
        #if aynn.name == "mypc"
        #{
             #CSharp.System.Debug.Write("aynn is mypc" );
        #}        
    }
}
#!
调用链的说明
1. 使用. 调用 进行调用
2. 如果静态函数调用 使用 Class1.StaticFun()的方式
3. 如果有namespace的调用 则使用 NamespaceName.ClassName. 的方式
4. 新建对象使用 ClassName()
5. 变量名.变量名 c4.c3;
6. 变量名.函数名  c3.GetClass2();
6. 
!#