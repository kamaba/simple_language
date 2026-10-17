import Std;

# ============================================================
# final 关键字拦截用例（Meta 层）：本工程编译报错即通过（Front.txt 检查 LID）
# 注：FileMeta 层校验（final enum/data/interface、abstract final）
#     已在先前版本验证命中 21470/21471，此处只留 MetaCore 层拦截场景。
# ============================================================

# ---- 场景 1：final 类被继承（应报 21469 MetaCoreFinalClassCannotExtend）----
final class SealedBase
{
    int v = 1;
    int getV()
    {
        ret this.v
    }
}

class SealedChild extends SealedBase
{
    int doubleV()
    {
        ret this.getV() * 2
    }
}

# ---- 场景 2：final 方法被 override（应报 12274 存量 MetaCoreFinalFunctionCannotOverride）----
class MethodBase
{
    final int locked()
    {
        ret 100
    }
}

class MethodChild extends MethodBase
{
    override int locked()
    {
        ret 200
    }
}

class FinalProbe
{
    public static void run()
    {
        Console.println( "done" )
    }
}
