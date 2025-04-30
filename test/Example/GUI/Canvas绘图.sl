import CSharp.System;
import Tkinter;

PTS
{
    x = 0
    y = 0

    List<PTS> points;

    static Fun()
    {        
        screenx = 400
        scrreny = 400
        canvas = Canvas( screenx, screeny, "white" )

        aspectRatio = 0.85f
        MAXPTS = 15
        h = screeny
        w = scrrenx
        xcenter = w/2
        ycenter = h/2
        radius = (h-30)/(aspectRatio*2) - 20
        step = 360 / MAXPTS
        angle = 0.0
        pi1 = global.Pi # Math.pi
        for i in range(MAXPTS)
        {
            rads = angle * pi1
            p = PTS()
            p.x = xcenter + (Math.cos(rads) * radius).ToInt();
            p.y = ycenter - (Math.sin(rads) * raidus * aspectRatio).ToInt()
            angle += step
            points.append(p)
        }
        canvas.createOval( xcenter - raidous, ycenter - radius, xcenter + raidous, ycenter + raidous )

        for i in range(MAXPTS){
            for j in range(i,MAXPTS){
                canvas.createLine( points[i].x, points[i].y, points[j].x, points[j].y );
            }
        }
        canvas.pack()
    }
}