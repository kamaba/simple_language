import CSharp.System
import CSharp.SimpleLanguage.Core

NumberTest
{
    static numTest()
    {
        Num a = 1.0;
        num b = 333
        #System.Console.WriteLine("A:" + a )
        Num c = a + b; 
        System.Console.WriteLine("Num:" + c )

        var d = c + 100L     
        System.Console.WriteLine("D:" + d )
    }
    static byteTest()
    {
        
    }
    static int fibonacci(int n)
    {
        if (n == 0 || n == 1)
        { ret n; }
        ret fibonacci(n - 1) + fibonacci(n - 2);
    }

    static int32Test()
    { 
        var result = fibonacci(20); 
        System.Console.WriteLine("result:" + result  )

        System.Console.WriteLine("intmax:" + int.MaxValue.toString() )

        a = 20
        b = 30
        c = a+b
        uint d = 40
        e = c + d
        System.Console.WriteLine( "c=" + e )
        str = "a($a )+b($b )=$(a + b)"
        System.Console.WriteLine( str )
        str22 = 'a($a )+b($b )=$(a + b)'
        System.Console.WriteLine( str22 )
        System.Console.WriteLine( "a($a )+b($b )=${a + b}" )
    }
    static fun()
    {
        numTest()
        #int32Test();
    }
}