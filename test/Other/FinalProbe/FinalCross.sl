import Std;
import FinalLib;

# ============================================================
# final 跨模块约束：本地类 extends 导入的 final 类（应报 21469）
# ============================================================

# 场景 1：继承 FinalLib 导出的 final 类 FinalBox（应报 21469 MetaCoreFinalClassCannotExtend）
class CrossSealedChild extends FinalBox
{
    int extra()
    {
        ret this.sealedGet() + 1
    }
}

class FinalCross
{
    public static void run()
    {
        # 场景 2（合法对照）：跨模块实例化 final 类并调用其方法
        FinalBox fb = FinalBox( 3 );
        Console.println( "FinalCross done " + fb.sealedGet().toString() )
    }
}
