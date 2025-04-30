import CSharp.System;
import Tkinter;

PTS
{

    x1 = [[12,7,3],[4,5,6],[7,8,9]];        # array<int>(9,3);  
    x = 0
    y = 0

    List<PTS> points;

    static Fun()
    {        
        canvas = Canvas( width=800, height=600, bg="white" )
        canvas.pack( )
        
        k = 1
        j = 1
        for i in range(0,26)
        {
            canvas.createOval( 310- k, 250 - k, 310 + k, 250 + k, width = 1 )
            k += j
            j += 0.3
        }
    }
}