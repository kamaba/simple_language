#import Application.Core;
import CSharp.System


Class1
{
    a = 10
}
Class2 extends Class1
{
    a2 = 10
    Class1 c1 = new()
}
Class3
{
    c1 = Class2()
    Class2 GetClass2()
    {
        ret this.c1
    }    
}
Class4 extends Class3
{
    Class3 c3 = null;
    int c41 = 0;
}

CallLinkTest
{
    static fun()
    {
        Class4 c4 = Class4()
        c4.c3 = Class3()
        c4.GetClass2().a2 = c4.c3.GetClass2().c1.a;
        newc1 = c4.GetClass().a2;
        #c4.c41 = newc1.$2;
        #c4.c41 = newc1.index;
        #newcx1 = newc1.value;
        #result1 = newc1;
        System.Console.Writeline("Class1 Value: " + c4.GetClass2().a2 );
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