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
        System.Console.Write(" OK   $this.c1.b " );
    }
    static SPrint()
    {
        Console.Write(" SPRint" );
        CSharp.System.Console.Write("aaa");
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