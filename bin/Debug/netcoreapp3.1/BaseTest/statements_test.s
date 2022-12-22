import System;
import Application;

Application.C1
{
    a = 10;
    b = 20;

    C1( int x1, int x2 )
    {
        a = x1;
        b = x2;
    }
}

C2
{
    Print()
    {
        C1 c112;
        static Application.C1 c1 = { a = 15 };
        int x = 10;
        ++x;
        if( x % 2 == 0 )
        {
            Console.Write("is %2 " );
        }
        else
            x = 20;

        x = 30;
        x -= 10;

        C1 c12 = ( 1, 2 );
        c1112  = C1( 12, 33 );
        C1 c113 = null;

        c1.b = 20;
        c112.a = 200;

        System.Console.Write(" OK   $c1.b " );
    }
    static SPint()
    {
        
    }
}