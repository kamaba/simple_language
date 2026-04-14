import Application.Core;

#namespace Application.MFC;


Class1{
    int a = 20;

    _init_( int _a )
    {
        this.a = 20;
    }
}

Int32 extends Object
{
    Int32 _value = 0

    bool _eq_( obj )
    {
        if obj == null 
        {
            ret false
        }
        if obj.type == Int32.type
        {
            ret obj == this._value
        }
        elif obj.type == String.type
        {
            ret obj.toInt32() == this._value
        }
        ret false
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

    bool compare2( _val )
    {
        if _val == null 
        {
            ret false
        }
        if( _val.type == int.type )
        {
            ret this._value.toInt32()  == _val
        }
        ret false
    }
}


ValueCompareTest
{
    static Fun()
    {
        c11  = Class1_1( 20 );
        c11.compare2( 2 )

        if c11 === 2
        {

        }
    }
}


