import Std;

class ConstLib
{
    # const 形参：函数体内只读（合法），供跨模块调用验证 isConst 导出
    public static Int32 addOne( const Int32 a )
    {
        ret a + 1;
    }
    # 非 const 形参：接收 const 实参应报错
    public static Int32 addTwo( Int32 a )
    {
        ret a + 2;
    }
    # 正向验证：const 传 const 形参、字面量传 const 形参均合法
    public static void run()
    {
        const Int32 la = 10;
        Int32 r1 = ConstLib.addOne( la );
        Int32 r2 = ConstLib.addOne( 5 );
        Console.println( "ConstLib done " + ( r1 + r2 ).toString() )
    }
}
