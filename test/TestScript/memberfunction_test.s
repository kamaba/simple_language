import Application.Core;

namespace Application.MFC;

namespace Application.T1
{
    namespace T1_1.WW
    {
        partial class Core1
        {
            X = 100;
            Core1_1
            {
                Core1_1_1
                {
                    XX = 2;
                }
                XX = 100;
            }
        }
    }
    namespace T1_2.WW2
    {
        Core1 extends T1_1.WW.Core1
        {
            X = 200;
        }
    }
    C
    {
        x = 20;
    }
}

CI3
{
    interface object C3();
}
Application.C1
{
    X = 1;
    Y = 2;
}
Application.CI2
{
    interface int C2();
}
C22 extends Application.CI2
{
    virtual int getback(){ return 100; }

    Y = 10;
    interface int C2(){ return Y; }
}
C23 extends C22
{
    M = 100;
    interface int C2(){ return M; }
}

Applicaction.C3 extends C22 interface Application.CI2, CI3
{
    interface int C2(){ return 100; }
    Object C3(){ return Object.New(); }
}
Application.C34 extends C22 interface CI3
{
}

# -###![md]
Application.MFC.Core
{
    #int Y;
    #float Y2;
    #Y = 0.0;
    Y3 = 0.0;
    #X1 = 1;   #1.int(); 
    #x2 = 3;   #short  x22 = 3.short();
    #x3 = 0xffff; #char          3.char()   a.char()   ff.char();
    #x4 = 0b1010;   //byte  1011.bin();
    #x5 = 5i;   int
    #xs = 3123123.ToString();    
    #x6 = 1231231231233123.ToLong();
    Class2
    {
        Class2( x )
        {
            m = x;
        }
        m = 10;
        m2 = "xx";
        static Print()
        {
            m2 = "mmm";
        }
        PrintX()
        {
            m = 20;
        }
    }
    bbb = Class2.New(20);
    Class2 ccc = { m = 30, m2 = "qq" };
    Class2 ddd = Claaa2.New(){ m = 20 };
    Class2 eee;
    nm = { s3 = 10, wm = {x3=1, x4= 2 } };
    #Y;
    #X;
    #M = null;  #这是一个空对象 相当于object M = null;
    #Count = 20;
    #private f = 12.33;
    #private f2 = 12.35;
   
    
    Print2( m = 22 )
    {
        if( Add( 2.ToString().ToFloat().ToInt(), 4 ) + 30 <= GetA(30).c % 10 - m )
        {
            Y33 = Y3 * Y3;
            {
                obj2 = {t1="mm", t2="mm2" };
                cl2 = Class2.New(){ m = 10 };
                ob3 = { tm = cl2, tm3 = obj2 };
                #obj2.Cast<Class3>().mut.Set( 20 );
                Console.Write("temp: = @obj2.t1 qx2 @msg ");    #@变量空格 可以直接访问变量 并且变量直接加.ToString()  
                Print( "xxx" ); 
                Class2.Print();
                bbb.PrintX();
            }
            Console.Write( msg.ToString() + "mqqqq" );
        }
        else
        {
            {
                {
                    m = 100;
                }
            }
        }
    }
    
    static Print( msg, int v1 = 10, int v2 = (-25 + -33 % 3) / 3 + -(32+22*(1+3)) )
    { 
        obj = { firstname = "john", lastname = "doe", int age = 50, color1 = { a = 1, r = 0.5, g = 0.1, b = 0.2 }, color2 = Color(){ a = 0.5, r = 1.0 } };    
        
        na = "have";

        #obj.color1.Cast<Color>().t.a = 1.0;

        tip = "Show: {0}   OK! @na";
        for( int i = 0; i < 10; i++ )
        {
            Console.Write( tip.Format( i.ToString() ) );
            Console.Write( tip, i );
        }
#label( dingo );
        bool flag = false;
        dowhile( flag )
        {
            flag = false;
            Console.Write( "Flag: " + flag );
        }

        flag = true;
        while( flag )
        {
            x2 = "MM";
            Console.Write( "OK" + x2 );
        }
        arr = [1,2,3,4];
        for( a in arr )
        {
            if( a == 2 )
            {
                continue;
            }
            else if( a == 4 )
                break;
            else if( a == 3 )
                break dingo;
            else
            {
                Console.Write(" mtk:  " + a.ToString() );
            }
        }
        x = 2;
        x2 = 3;
        switch()
        {
        #    case x > 10 && x < 20: break;
        #    case x2 == 100: m = 100; break;            
        #    case x2 == 20: x = 10; break;
        #    case x < 20:{ }; break;
        #    case x > 40: { m = 200; break; }
        #    case x > 80: m = 30;
        #    case x > 100: m = 40;
        #    default:{ m = 0; } 
        }
        switch( t.Type() )
        {
        #    case int:{ Print("this is internal type!"); }break;
        #    case float:{}break;
        #    default:break;
        }
        obj = Class1();
        switch( obj.Type() )
        {
            
        }
        mxx = 30;
        switch( mxx )
        {
        #    case 20:{ mxx = 100; }break;
        #    case 100:mxx = 200; break;
        }
        arr = [1,2,3][22,33,44];
       
        Count = Add( 1, Add( 1, GetA().a ) ) - X * 2 - Add( 1, (-25+ (32 - (2*2) ) ) ) + GetA().a;

        Console = GetHandle( "System.Console", 1 );   #变成表达示去看待

        Console.Write( msg );

        Set( Count, GetA().a + 30 / 1.3 + 100%10 );          

        ret { int a = 20, string b = "100", x = Count };
    }
    
}

MainClass
{
    static Main()
    {
        X = 10 + 21;

        #Y = Tuple( 'a', "cb", 232, 1.1f, 1.2d, 0xff );  #默认tuple类,用来存储临时数据

        #map = Dictionary.New();    map["one"] = 1; map["two"] = 2       map2 = Map(sme=123,sme2=1000,ses2="emedk"}

        #ar = Array();   ar[0] = 1; ar[2] = 1; ar[1] = 3;  Array(1,2,3)  [1,2,3];   [[1,2,3],[4,5,6]]

        #list = List("1",2, 323, "dmekms");

        #Core.Print( "10+21=$X" );
    }
}