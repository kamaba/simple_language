import Std;
import Core;

# Log.fatal 硬停（fatal_halt）验证用例：
#   Log.fatal / Logger.fatal 触发后 VM 立即停机 —— 后续语句不执行、
#   label/catch 无法截获（硬停不走异常路径）、进程退出码非 0（-91 透传）。
# 本用例必须放在 _main_ 调用链的最末尾（fatal 会终止整个进程）。
# 预期输出（控制台 / Result.txt）：
#   1) "before fatal" 一行 info 正常出现
#   2) "[FATAL] ..." 一行出现（fatal 始终输出，无视级别开关）
#   3) 之后不应有任何输出（catch 块与末尾 println 均不执行）
#   4) 进程退出码 = 1

LogFatalTest
{
    static fun()
    {
        Console.println("---- LogFatalTest start ----")
        SLang.Log.info("before fatal: this must appear")

        label fatalBlock
        {
            # try 标记也无法截获：FATAL 是硬停（fatal_halt），不走异常路径
            try SLang.Log.fatal("fatal halt: VM must stop here (not catchable)")
        }
        catch
        {
            Console.println("[FAIL] fatal must NOT be catchable")
        }

        Console.println("[FAIL] this line must NOT appear (VM halted)")
    }
}
