import CSharp.System;

class XC
{
    a = 10;
    b = 10;
}
enum Book
{
    B1 = { int i2 = 20, string url = "http://www.baidu.com", XC xc1= XC(), anonC = { string name = "xx", age = 20 } }
    C1 = 1
    string Str
    C2
    C4 = 10
    C5
}
enum Book2
{
    Int32 Id = 1;
    String Name = "";
}
enum Book3
{
    A1 = 10;
}

MixColor
{
    Red = 0.0f;
    Green = 0.0f;
    Blue = 0.0f;
}
const enum ConstColor
{
    Red = 0xff0000;
    Green = 0x00ff00;
    Blue = 0x0000ff;

    MixColor1 = MixColor() {Red=0.9f, Green = 0.1f, Blue = 0.01f } ;
    MixColor2 = MixColor() {Red=0.4f, Green = 0.22f, Blue = 0.7f } ;

    Book b1 = Book.C1( 2 );
}
enum GameState
{
    Init = 1;
    Begin = 2;
    End = 3;
}
OK
{
    code = 0;
}
Error
{
    code = 0;
}
enum Res
{
    OK ok;
    Error error;
}

EnumTest
{
    foo()
    {
        Res res = Res.ok({code=0});
        ret res;
    }
    static Func()
    {
        anonObj = { name = "ok", age = 20, sex = 0 };

        anon2 = {name="aa", age = 15, sex = 1 };
        
        for b3 in GameState
        {
            Console.Write("Book3: " + b3 );
        }
        

        
        Book3 b3 = Book3.A1;
        Book3 b3_3 = Book3.A1( 20 );
        

        book = Book.B1({ i2 = 20, url = "mas" });
        switch book
        {
            case Book.B1 b1
            {
                Console.Write( "bi2:" + b1.i2 );
            }
        }

        Book eb = Book.C1;
        if( eb == Book.C1 )
        {
            Console.Write( eb );
        }

        #! 暂不实现
        Option op = Option.Some<String>("hello");
        switch op{
            case Option.Some sot:
            {
                Console.Write("something=" + sot.ToString() );
            }
        }
        !#
    }
}

#! 进度
1. 上边除了T的enum没有实现，其实的都验证通过
2. 没有在vm中没有实现enum相关的内容
3. enum的new没有具体的分配相关的逻辑
4. enum的.ToString() 没有实现。
!#