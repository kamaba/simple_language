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
        # 4. 跨模块只读访问 const 静态成员（合法）
        int r4 = ConstLib.SCF;
        # 3. const 实参 -> 跨模块非 const 形参（应报错 21468）
        # int r3 = ConstLib.addTwo( la );
        # 5. 跨模块 const 成员赋值（应报错 21372）
        # ConstLib.SCF = 5;
        Console.println( "ConstCross done " + ( r1 + r2 + r4 ) )
    }
}
