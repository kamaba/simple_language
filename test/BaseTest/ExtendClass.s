import Application.Core;

#namespace Application.MFC;


Class1{
    int a = 20;

    _init_( int _a )
    {
        this.a = 20;
    }
}

Class1_1 extends Class1
{
    x1 = 0;
    y1 = 0;
    z1 = 0;

    _init_( int _x1, int _y1 )
    {
        base._init_(_x1+1);
    }

    _init_(int z1 )
    {
        _init_( 1, 2 );
        base._init_(z1+10);
    }
}


ExtendsClass
{
    static Fun()
    {
        c11  = Class1_1( 20 );
        c12 = Class1_1( 20, 30 );
        c13 = Class1_1();
        c14 = Class1();
        c15 = Class1(20);
    }
}


