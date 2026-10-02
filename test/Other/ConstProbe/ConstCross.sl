import Std;
import ConstLib;

class ConstCross
{
    public static void run()
    {
        const int la = 10;
        # 1. const 实参 -> 跨模块 const 形参（合法）
        int r1 = ConstLib.addOne( la );
        # 2. 字面量 -> 跨模块 const 形参（合法）
        int r2 = ConstLib.addOne( 5 );
        # 3. const 实参 -> 跨模块非 const 形参（应报错 21468）
        int r3 = ConstLib.addTwo( la );
        Console.println( "ConstCross done " + ( r1 + r2 + r3 ) )
    }
}
