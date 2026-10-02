import Std;

class ConstField
{
    const int CF = 10;
    static const int SCF = 20;
    int normal = 0;
}

class ConstProbe
{
    public static void run()
    {
        # 1. const 成员变量赋值（应报错）
        ConstField cf = ConstField();
        cf.CF = 100;

        # 2. const 局部变量（语句中）
        const int la = 10;
        la = 20;

        # 3. const 无类型局部变量
        const lb = 30;
        lb = 40;

        # 4. const 实参传给非 const 形参（应报错）
        ConstProbe.takeNormal( la );

        # 5. const 参数在函数体内赋值（应报错）
        ConstProbe.takeConst( 5 );

        Console.println( "done" )
    }

    public static void takeConst( const int a )
    {
        a = 100;
    }
    public static void takeNormal( int a )
    {
        a = 100;
    }
}
