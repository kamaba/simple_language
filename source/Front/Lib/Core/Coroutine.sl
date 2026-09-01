#!
 * Core/Coroutine.sl
 * 协程（Coroutine）静态工具类与状态常量。
 *
 * 说明：
 *  - 协程在 SL 层以 Int64 句柄表示（C VM 内部为 VMCoroutine* 的注册表 id）。
 *  - yield/await/spawn 为前端关键字（语法糖），分别展开为本类 yieldNow/awaitFunction
 *    与闭包 spawn 调用；显式调用本类方法亦合法。
 *  - 所有方法直接转发到 C VM 系统调用（见 coroutine_system_method.c）。
 !#

public class Coroutine extends Object
{
    #! ---------- 生成 ---------- !#

    #!
     * 以无参静态方法创建并启动协程。
     * 返回协程句柄。
    !#
    public static Int64 spawn0(string methodName)
    {
        ret SystemCoroutineSpawn0(methodName)
    }

    #!
     * 以 1 参静态方法创建并启动协程（参数为 object）。
     * 返回协程句柄。
    !#
    public static Int64 spawn1(string methodName, object arg0)
    {
        ret SystemCoroutineSpawn1(methodName, arg0)
    }

    #!
     * 以 2 参静态方法创建并启动协程（参数为 object）。
     * 返回协程句柄。
    !#
    public static Int64 spawn2(string methodName, object arg0, object arg1)
    {
        ret SystemCoroutineSpawn2(methodName, arg0, arg1)
    }

    #!
     * 以 3 参静态方法创建并启动协程（参数为 object）。
     * 返回协程句柄。
    !#
    public static Int64 spawn3(string methodName, object arg0, object arg1, object arg2)
    {
        ret SystemCoroutineSpawn3(methodName, arg0, arg1, arg2)
    }

    #!
     * 以无参闭包创建并启动协程。
     * 闭包可为匿名闭包、function 声明变量或 Func&lt;&gt; 类型变量。
     * 返回协程句柄。spawn 关键字即本组方法的语法糖。
    !#
    public static Int64 spawnClosure0(object closure)
    {
        ret SystemCoroutineSpawnClosure0(closure)
    }

    #!
     * 以 1 参闭包创建并启动协程（参数为 object）。
    !#
    public static Int64 spawnClosure1(object closure, object arg0)
    {
        ret SystemCoroutineSpawnClosure1(closure, arg0)
    }

    #!
     * 以 2 参闭包创建并启动协程（参数为 object）。
    !#
    public static Int64 spawnClosure2(object closure, object arg0, object arg1)
    {
        ret SystemCoroutineSpawnClosure2(closure, arg0, arg1)
    }

    #!
     * 以 3 参闭包创建并启动协程（参数为 object）。
    !#
    public static Int64 spawnClosure3(object closure, object arg0, object arg1, object arg2)
    {
        ret SystemCoroutineSpawnClosure3(closure, arg0, arg1, arg2)
    }

    #! ---------- 调度控制 ---------- !#

    #!
     * 让出当前协程，允许调度器运行其它就绪协程。
     * 若当前不在协程上下文（root 直接执行），则空操作。
     * yield 语句关键字即本方法的语法糖。
    !#
    public static void yieldNow()
    {
        SystemCoroutineYield()
    }

    #!
     * 休眠当前协程指定毫秒数。休眠期间调度器可运行其它协程。
     * 若当前不在协程上下文，则退化为阻塞 sleep。
    !#
    public static void sleep(Int64 millis)
    {
        SystemCoroutineSleep(millis)
    }

    #! ---------- 查询 ---------- !#

    #!
     * 获取当前协程句柄。若在 root 直接执行上下文，返回 0。
    !#
    public static Int64 current()
    {
        ret SystemCoroutineCurrent()
    }

    #!
     * 查询协程状态。返回 CoroutineStatus 常量值：
     *  0=Created, 1=Ready, 2=Running, 3=Suspended, 4=Dead。
    !#
    public static Int32 status(Int64 handle)
    {
        ret SystemCoroutineStatus(handle)
    }

    #!
     * 查询协程挂起原因（用于诊断）。返回 CoroutineBlockReason 常量值：
     *  0=None, 1=Yield, 2=Sched, 3=Await, 4=Sleep, 5=IO。
    !#
    public static Int32 blockedReason(Int64 handle)
    {
        ret SystemCoroutineBlockedReason(handle)
    }

    #!---------- 等待与聚合 ---------- !#

    #!
     * 等待目标协程结束并取回其返回值。
     * 若目标已结束，立即返回其结果；否则当前协程挂起直至目标结束。
     * 若目标以异常结束，异常向等待者传播。
    !#
    public static object awaitFunction(Int64 handle)
    {
        ret SystemCoroutineAwait(handle)
    }

    #!
     * 等待两个协程全部结束（无返回值；结果在返回后用 await 逐个取回）。
     * 任何一个协程以异常结束：立即取消其余协程并向调用者抛出该异常。
     * 说明：本语言数组不支持协变（int[] 不能赋 object[]），且 Int64 句柄
     * 无法直接装入 object[] 元素槽，故聚合 API 采用固定参数重载形式。
    !#
    public static void waitAll2(Int64 h0, Int64 h1)
    {
        SystemCoroutineWaitAll2(h0, h1)
    }

    #!
     * 等待三个协程全部结束（无返回值；结果在返回后用 await 逐个取回）。
     * 任何一个协程以异常结束：立即取消其余协程并向调用者抛出该异常。
    !#
    public static void waitAll3(Int64 h0, Int64 h1, Int64 h2)
    {
        SystemCoroutineWaitAll3(h0, h1, h2)
    }

    #!
     * 等待两个协程中任意一个结束，返回先结束者的句柄。
     * 某协程以异常结束：立即取消其余协程并向调用者抛出该异常。
    !#
    public static Int64 waitAny2(Int64 h0, Int64 h1)
    {
        ret SystemCoroutineWaitAny2(h0, h1)
    }

    #!
     * 等待三个协程中任意一个结束，返回先结束者的句柄。
     * 某协程以异常结束：立即取消其余协程并向调用者抛出该异常。
    !#
    public static Int64 waitAny3(Int64 h0, Int64 h1, Int64 h2)
    {
        ret SystemCoroutineWaitAny3(h0, h1, h2)
    }

    #!
     * 非阻塞地取回一个已完成且结果未被消费的协程句柄，没有则返回 0。
     * NextCompleted 会消费该协程的结果（再次查询同一协程不再返回）。
    !#
    public static Int64 nextCompleted2(Int64 h0, Int64 h1)
    {
        ret SystemCoroutineNextCompleted2(h0, h1)
    }

    #!
     * 非阻塞地取回一个已完成且结果未被消费的协程句柄（三元版本）。
    !#
    public static Int64 nextCompleted3(Int64 h0, Int64 h1, Int64 h2)
    {
        ret SystemCoroutineNextCompleted3(h0, h1, h2)
    }

    #!
     * 限时等待目标协程结束。
     * 返回 true 表示目标已结束（结果已被消费，可用 await 取回，await 对已 Dead 目标立即返回）；
     * 返回 false 表示超时（等待关系已解除，目标继续运行不受影响）。
    !#
    public static bool waitTimeout(Int64 handle, Int64 millis)
    {
        ret SystemCoroutineWaitTimeout(handle, millis)
    }

    #! ---------- 取消 ---------- !#

    #!
     * 请求取消目标协程。取消是协作式的：目标在下一个调度点（Yield/Await/Sleep 等）抛出
     * 取消异常并结束。对已结束协程调用返回 false。
     * 返回 true 表示已成功登记取消请求。
    !#
    public static bool cancel(Int64 handle)
    {
        ret SystemCoroutineCancel(handle)
    }
}

#!
 * 协程状态常量类。与 C VM 的 VMCoroutineState 枚举一一对应。
!#
public class CoroutineStatus extends Object
{
    public static const Int32 Created   = 0
    public static const Int32 Ready     = 1
    public static const Int32 Running   = 2
    public static const Int32 Suspended = 3
    public static const Int32 Dead      = 4
}

#!
 * 协程挂起原因常量类。与 C VM 的 CORO_BLOCK_* 常量一一对应。
!#
public class CoroutineBlockReason extends Object
{
    public static const Int32 None  = 0
    public static const Int32 Yield = 1
    public static const Int32 Sched = 2
    public static const Int32 Await = 3
    public static const Int32 Sleep = 4
    public static const Int32 IO    = 5
}
