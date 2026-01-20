import CSharp.System
import CSharp.SimpleLanguage.Core

NumberTest
{
    static numTest()
    {
        #floating point / numeric mix tests
        Num a = 1.0;
        num b = 333
        # basic ops
        Num na = -a
        System.Console.WriteLine("Num na: " + na )

        Num nanb = b - -a;
        System.Console.WriteLine("Num nanb: " + nanb )

        Num nanb2 = b + -a;
        System.Console.WriteLine("Num nanb2: " + nanb2 )
        
        Num nanb3 = b + (+a);
        System.Console.WriteLine("Num nanb3: " + nanb3 )

        Num c = a + b; 
        System.Console.WriteLine("Num add: " + c )
        Num s = c - 12.5
        System.Console.WriteLine("Num sub: " + s )
        Num m = c * 2
        System.Console.WriteLine("Num mul: " + m )
        Num dv = c / 2
        System.Console.WriteLine("Num div: " + dv )
        # modulo with integers
        var modv = 100 % 3
        System.Console.WriteLine("mod 100%3=" + modv )

        # mixing long/int with Num
        var d = c + 100L     
        System.Console.WriteLine("Num mix long: " + d )

        # comparisons
        System.Console.WriteLine("c > b: " + (c > b) )
        System.Console.WriteLine("c == (a+b): " + (c == (a + b)) )
        # negative and unary
        Num neg = -c
        System.Console.WriteLine("neg: " + neg )
    }
    static byteTest()
    {
        # byte wrap and conversions
        byte x = 250
        System.Console.WriteLine("byte x=" + x)
        var y = x + 10
        System.Console.WriteLine("byte+int =" + y)
        #cast down
        byte z = y as byte
        System.Console.WriteLine("casted back byte=" + z)
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
        System.Console.WriteLine( "c+uint=" + e )
        # bit operations
        System.Console.WriteLine("(a<<2)=" + (a << 2))
        System.Console.WriteLine("(b>>1)=" + (b >> 1))
        System.Console.WriteLine("(a&b)=" + (a & b))
        System.Console.WriteLine("(a|b)=" + (a | b))
        str = "a($a )+b($b )=$(a + b)"
        System.Console.WriteLine( str )
        str22 = 'a($a )+b($b )=$(a + b)'
        System.Console.WriteLine( str22 )
        System.Console.WriteLine( "a($a )+b($b )=${a + b}" )
    }
    static fun()
    {
        numTest();
        #byteTest();
        #int32Test();
    }
}