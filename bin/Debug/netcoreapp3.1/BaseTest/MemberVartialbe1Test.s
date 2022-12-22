
C1
{
    C2 c3 = ();
    X = 1;
    Y = 2;

    static public int a1 = 100;
    private a2 = 20;
    private long a3;
    C2 a4;
    static C2 a5;
    a6 = C2();
    a7 = 10 / 5 + 2 * 10 - C2.x2;
    C2 a8 = ();
    
    a = 10;
    b = 20;
}

partial C2
{
    x = 1;
    static int x2 = 10;

    C2( int a, float b )
    {
        this.x = 10;
        this.x1 = C2.x2;
    }
}

C3 :: C1
{
    x = 1;
    z = 2;
}

Class2
{
    static int m2 = 20;
}

C4 :: C3
{
    bool ab = 10 <= 10 / (20+1 * 35 - (32/15) );
    bx = -20;
    int a = 10 + (1 - ( 3 * ( 3 / ( -Class2.m2 - 100 ) )  * 30 ) ) / 10 * (15 - (22/2 + 1 ) ) - 20 / -10;
    int x1 = 10 + (1-20) * 33/20 + 10/20 - 22 * 2/1;
    C2 c2 = C2( 20 );
    C2 c3 = ();
}