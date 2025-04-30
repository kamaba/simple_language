import CSharp.System;

Class1
{
    static st = 300;
    a = 10;
    b = 20;
    string str = "xx";
    Class1 c1 = null;

    ToString()
    {
        ret this.a;
    }
    Print( aaa )
    {
        Console.Write( aaa );
    }
}

TempTest
{    
    static Print( aaa )
    {
        Console.Write( aaa );
    }
    static GetA()
    {
        ret "asdfasdfa";
    }
    static Fun()
    {
        #Print( GetA() );
        byte a = 15;
        int b = 220;
        c = a+b;
        Print( c );
        #!
        int i = 0;
        dowhile i < 1
        {
            Print( "Index: " + i + Environment.NewLine );
            i++;
        }
        !#
        #!
        for i = 0, i < 100,i++
        {
            if i > 25
            {
                if i == 20 || i == 30
                {
                    continue;
                }
                Print( "index:" + i + Environment.NewLine );
            }
            i++;
        }
        !#
        #!
        int i = 10;
        while{
            
        }
        !#
        #!
        Class1 c1;
        if c1.a <= 4 {

            c1.Print( Class1.st );
        }
        elif c1.a <= 11
        {
            Print("mmm");
        }
        else
        {
            c1.Print("xxx");
        }
        !#
        #int b = 10;
        #int a = 10/b + 20/b;
        #Console.WriteLine( a );
        #Console.WriteLine( "hello world!!!" );        
        #int b1 = 10;
        #int c1 = a+b1;
        #string cs = c.ToString();
        #Console.Write( cs );
        #string aaa = "mmm";
        #Console.Write( aaa );
    }
}