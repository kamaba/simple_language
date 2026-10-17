import Std;

# ============================================================
# final 关键字正向用例 + 跨模块导出库：编译须 0 错误
# ============================================================

# final 类：不可被继承，但可正常实例化使用
final class FinalBox
{
    int v = 0;
    _init_( int v )
    {
        this.v = v
    }
    int getV()
    {
        ret this.v
    }
    # final 方法：子孙类可调用但不可 override
    final int sealedGet()
    {
        ret this.v * 10
    }
}

# 普通类继承链（对照组：无 final，继承 + override 均合法）
class OpenBase
{
    int calc()
    {
        ret 1
    }
}

class OpenChild extends OpenBase
{
    override int calc()
    {
        ret base.calc() + 1
    }
    # 调用（而非 override）final 类实例的方法：合法
    int useFinalBox()
    {
        FinalBox fb = FinalBox( 7 );
        ret fb.sealedGet()
    }
}

class FinalLib
{
    public static void run()
    {
        FinalBox fb = FinalBox( 5 );
        OpenChild oc = OpenChild();
        int r = fb.getV() + fb.sealedGet() + oc.calc() + oc.useFinalBox();
        Console.println( "FinalLib done " + r.toString() )
    }
}
