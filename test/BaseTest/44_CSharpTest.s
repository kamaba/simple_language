import CSharp.System;
import CSharp.System.Text;

C1
{
    b = 20;
}
C2
{
    C1 c1;
    Print()
    {
        this.c1.b = 20;
        System.Debug.Write(" OK   $this.c1.b " );
    }
    static SPrint()
    {
        Debug.Write(" SPRint" );
        Debug.Write("aaa");
    }
}
CSharpTest
{
    static Fun()
    {
        C2 c2 = { c1 = C1() };
        c2.Print();
        C2.SPrint();
    }
}

# 对CSharp或者其它虚机调用方式，直接使用传统的值类型，直接输入调用  像    c = add( int a, int b )  这种的如果直接传，add(2，3) 会把内部转化为csharp类的int型
# 还有一种调用，是通过 SObject类型，通过虚拟传入值，然后根据功能， 直接取SObject里边的值 ，比如通过SObject先分析出类型，然后通过其它参数获得 处理，比如  Class1 { float x = 0, float y = 0.0f float z = 0.0f } 这时，如果
# 使用 Class1 a = (10,20,30)  Class1 b = (100,200,300) 进行求差运行， 先要解出    length( SObject a, SObject b )    传入后，在length 通过a 获取 类型， 然后识别A*B的运算  float a = a.GetMD(0) float b = a.GetMD(0) 
# 依此类推，最后，再进行float运行后，通过返回值，或者是输入类，改变某些值 
# 如果使用c编译，则传入SObject后，进行指针与类的拿取后，通过位置指针取值，还有值类型
