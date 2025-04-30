import Application.Core;


Class1
{
    a = 10
}
QS.Class2
{
    a2 = 10

    Class1 c1;
}
Class3
{
    Class2 c1;

    Class2 GetClass2()
    {
        ret this.c1;
    }    
}
Class4
{
    Class3 c3 = null;
    int c41 = 0;
    Array<int> arr1 = {1,2,3,4,5};
}

CallLinkTest
{
    static Fun()
    {
        Class4 c4 = Class4()
        c4.c3 = Class3()
        c4.GetClass2().a2 = c4.c3.GetClass2().c1.a;
        newc1 = c4.GetClass().a2;
        c4.c41 = newc1.$2;
        c4.c41 = newc1.index;
        newcx1 = newc1.value;
        result1 = newcx1;
        CSharp.System.Console.Write("Class1 Value: " + result1 );
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