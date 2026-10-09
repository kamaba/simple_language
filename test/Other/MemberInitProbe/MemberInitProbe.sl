# 探针：类成员变量声明不带初始化（如 int v;）的报错形态
# 预期（修复后）：明确报"配置成员变量时，必须需要有等号及后续的表达式"（LID 21305）
# 历史（修复前）：报含混的 MetaCoreExpressIsNull（11009 没有生成成功表达式）
# 本工程编译报错即通过；正常初始化成员作对照不报错
import Std;

class MemberInitProbe
{
    # 无初始化成员声明（问题场景）
    int v;

    # 正常初始化成员（对照组，不应报错）
    int ok = 1;

    public static void run()
    {
        Console.println( "MemberInitProbe done" )
    }
}
