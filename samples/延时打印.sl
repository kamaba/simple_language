import CSharp.System;
import Core;

JJ
{
    static Fun()
    {
        start = Core.Time.clock();

        await WaitTime( 20 )

        Console.print( "Execlute Time: " + Time.clock() - start )
    }
    async WaitTime( int dtime )
    {
        await Time.delay( dtime )

        yield 0;
    }
}