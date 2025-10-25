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