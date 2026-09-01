#!
 * Core/Coroutine.sl
 * 协程对象（Coroutine）与协程管理器（CoroutineManager）。
 *
 * 说明：
 *  - Coroutine：协程对象，由 CoroutineManager 的 spawn/spawnClosure 系列生成，
 *    是 VM 内部协程（Int64 句柄）的强类型包装；提供 await/cancel 实例方法与
 *    status/blockedReason/isDead/handle 查询属性。
 *  - CoroutineManager：静态管理器，负责生成、查询与等待协程；所有操作接口
 *    均收发 Coroutine 对象（取代裸 Int64 句柄，从类型层面杜绝错误传值）。
 *  - 同一句柄始终对应同一 Coroutine 实例（内部注册表保证），可用 == 判等。
 *  - yield/await/spawn 为前端关键字（语法糖），分别展开为本管理器
 *    yieldNow/awaitFunction 与闭包 spawn 调用；显式调用本类方法亦合法。
 *  - 所有方法最终转发到 C VM 系统调用（见 coroutine_system_method.c）。
 !#

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

#! ==================== 协程对象 ==================== !#

#!
 * 协程对象：CoroutineManager.spawn/spawnClosure 系列的返回类型。
 * 包装 VM 内部协程句柄，禁止裸句柄误用。
!#
public class Coroutine extends Object
{
    #! VM 内部协程句柄（注册表 id，由 VM 单调递增分配）。 !#
    Int64 _handle = 0

    #! 内部构造：包装一个已有句柄。仅由 CoroutineManager 登记时调用。 !#
    _init_( Int64 handle )
    {
        this._handle = handle
    }

    #!
     * 等待本协程结束并取回其返回值（等价 CoroutineManager.awaitFunction(this)）。
     * 若已结束则立即返回结果；若以异常结束，异常向等待者传播。
    !#
    public object awaitHandle()
    {
        ret SystemCoroutineAwait( this._handle )
    }

    #!
     * 请求取消本协程（等价 CoroutineManager.cancel(this)）。
     * 取消是协作式的：本协程在下一个调度点抛出取消异常并结束。
     * 对已结束协程返回 false；返回 true 表示已成功登记取消请求。
    !#
    public bool cancel()
    {
        ret SystemCoroutineCancel( this._handle )
    }

    #! 当前状态，取 CoroutineStatus 常量值。 !#
    get Int32 status()
    {
        ret SystemCoroutineStatus( this._handle )
    }

    #! 当前挂起原因，取 CoroutineBlockReason 常量值（诊断用途）。 !#
    get Int32 blockedReason()
    {
        ret SystemCoroutineBlockedReason( this._handle )
    }

    #! 是否已结束（status == CoroutineStatus.Dead）。 !#
    get bool isDead()
    {
        ret SystemCoroutineStatus( this._handle ) == CoroutineStatus.Dead
    }

    #! 原始句柄（诊断用途）。 !#
    get Int64 handle()
    {
        ret this._handle
    }
}

#! ==================== 协程管理器 ==================== !#

#!
 * 协程管理器：生成、查询、等待与取消协程的静态工具类。
 * 所有操作接口收发 Coroutine 对象。
!#
public class CoroutineManager extends Object
{
    #!
     * 句柄 -> 协程对象注册表（惰性初始化的数组 + 已用计数）。
     * Core 库不含 Map 容器，故采用数组线性管理；
     * VM 句柄单调递增不复用，登记一次即终身有效，仅追加不删除，
     * 由此保证同一句柄总是返回同一 Coroutine 实例（可用 == 判等）。
    !#
    static Array<Coroutine> s_regs = null
    static Int32 s_count = 0

    #!
     * 句柄 -> 协程对象。句柄无效（0）返回 null；
     * 未登记则创建 Coroutine、登记并返回。
    !#
    static Coroutine _ensure( Int64 handle )
    {
        if ( handle == 0 )
        {
            ret null
        }
        if ( s_regs == null )
        {
            s_regs = Array<Coroutine>( 16 )
        }
        Int32 i = 0
        while ( i < s_count )
        {
            Coroutine cor = s_regs._getItem_( i )
            if ( cor.handle == handle )
            {
                ret cor
            }
            i = i + 1
        }
        if ( s_count >= s_regs.length )
        {
            # 容量不足则倍增扩容（拷贝旧表）
            Array<Coroutine> bigger = Array<Coroutine>( s_regs.length * 2 )
            Int32 j = 0
            while ( j < s_regs.length )
            {
                bigger._setItem_( j, s_regs._getItem_( j ) )
                j = j + 1
            }
            s_regs = bigger
        }
        Coroutine ncor = Coroutine( handle )
        s_regs._setItem_( s_count, ncor )
        s_count = s_count + 1
        ret ncor
    }

    #! ---------- 生成 ---------- !#

    #! 以无参静态方法创建并启动协程，返回协程对象。 !#
    public static Coroutine spawn0(string methodName)
    {
        ret _ensure( SystemCoroutineSpawn0(methodName) )
    }

    #! 以 1 参静态方法创建并启动协程（参数为 object），返回协程对象。 !#
    public static Coroutine spawn1(string methodName, object arg0)
    {
        ret _ensure( SystemCoroutineSpawn1(methodName, arg0) )
    }

    #! 以 2 参静态方法创建并启动协程（参数为 object），返回协程对象。 !#
    public static Coroutine spawn2(string methodName, object arg0, object arg1)
    {
        ret _ensure( SystemCoroutineSpawn2(methodName, arg0, arg1) )
    }

    #! 以 3 参静态方法创建并启动协程（参数为 object），返回协程对象。 !#
    public static Coroutine spawn3(string methodName, object arg0, object arg1, object arg2)
    {
        ret _ensure( SystemCoroutineSpawn3(methodName, arg0, arg1, arg2) )
    }

    #!
     * 以无参闭包创建并启动协程，返回协程对象。
     * 闭包可为匿名闭包、function 声明变量或 Func&lt;&gt; 类型变量。
     * spawn 关键字即本组方法的语法糖。
    !#
    public static Coroutine spawnClosure0(object closure)
    {
        ret _ensure( SystemCoroutineSpawnClosure0(closure) )
    }

    #! 以 1 参闭包创建并启动协程（参数为 object），返回协程对象。 !#
    public static Coroutine spawnClosure1(object closure, object arg0)
    {
        ret _ensure( SystemCoroutineSpawnClosure1(closure, arg0) )
    }

    #! 以 2 参闭包创建并启动协程（参数为 object），返回协程对象。 !#
    public static Coroutine spawnClosure2(object closure, object arg0, object arg1)
    {
        ret _ensure( SystemCoroutineSpawnClosure2(closure, arg0, arg1) )
    }

    #! 以 3 参闭包创建并启动协程（参数为 object），返回协程对象。 !#
    public static Coroutine spawnClosure3(object closure, object arg0, object arg1, object arg2)
    {
        ret _ensure( SystemCoroutineSpawnClosure3(closure, arg0, arg1, arg2) )
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
     * 获取当前协程对象。若在 root 直接执行上下文，返回 null。
    !#
    public static Coroutine current()
    {
        ret _ensure( SystemCoroutineCurrent() )
    }

    #!
     * 查询协程状态。返回 CoroutineStatus 常量值：
     *  0=Created, 1=Ready, 2=Running, 3=Suspended, 4=Dead。
    !#
    public static Int32 status( Coroutine cor )
    {
        ret SystemCoroutineStatus( cor.handle )
    }

    #!
     * 查询协程挂起原因（用于诊断）。返回 CoroutineBlockReason 常量值：
     *  0=None, 1=Yield, 2=Sched, 3=Await, 4=Sleep, 5=IO。
    !#
    public static Int32 blockedReason( Coroutine cor )
    {
        ret SystemCoroutineBlockedReason( cor.handle )
    }

    #! ---------- 等待与聚合 ---------- !#

    #!
     * 等待目标协程结束并取回其返回值。
     * 若目标已结束，立即返回其结果；否则当前协程挂起直至目标结束。
     * 若目标以异常结束，异常向等待者传播。
    !#
    public static object awaitFunction( Coroutine cor )
    {
        ret SystemCoroutineAwait( cor.handle )
    }

    #!
     * 等待两个协程全部结束（无返回值；结果在返回后用 await 逐个取回）。
     * 任何一个协程以异常结束：立即取消其余协程并向调用者抛出该异常。
     * 说明：本语言数组不支持协变（int[] 不能赋 object[]），且协程对象
     * 无法直接装入 object[] 元素槽，故聚合 API 采用固定参数重载形式。
    !#
    public static void waitAll2( Coroutine c0, Coroutine c1 )
    {
        SystemCoroutineWaitAll2( c0.handle, c1.handle )
    }

    #!
     * 等待三个协程全部结束（无返回值；结果在返回后用 await 逐个取回）。
     * 任何一个协程以异常结束：立即取消其余协程并向调用者抛出该异常。
    !#
    public static void waitAll3( Coroutine c0, Coroutine c1, Coroutine c2 )
    {
        SystemCoroutineWaitAll3( c0.handle, c1.handle, c2.handle )
    }

    #!
     * 等待两个协程中任意一个结束，返回先结束者的协程对象。
     * 某协程以异常结束：立即取消其余协程并向调用者抛出该异常。
    !#
    public static Coroutine waitAny2( Coroutine c0, Coroutine c1 )
    {
        ret _ensure( SystemCoroutineWaitAny2( c0.handle, c1.handle ) )
    }

    #!
     * 等待三个协程中任意一个结束，返回先结束者的协程对象。
     * 某协程以异常结束：立即取消其余协程并向调用者抛出该异常。
    !#
    public static Coroutine waitAny3( Coroutine c0, Coroutine c1, Coroutine c2 )
    {
        ret _ensure( SystemCoroutineWaitAny3( c0.handle, c1.handle, c2.handle ) )
    }

    #!
     * 非阻塞地取回一个已完成且结果未被消费的协程对象，没有则返回 null。
     * NextCompleted 会消费该协程的结果（再次查询同一协程不再返回）。
    !#
    public static Coroutine nextCompleted2( Coroutine c0, Coroutine c1 )
    {
        ret _ensure( SystemCoroutineNextCompleted2( c0.handle, c1.handle ) )
    }

    #!
     * 非阻塞地取回一个已完成且结果未被消费的协程对象（三元版本），没有则返回 null。
    !#
    public static Coroutine nextCompleted3( Coroutine c0, Coroutine c1, Coroutine c2 )
    {
        ret _ensure( SystemCoroutineNextCompleted3( c0.handle, c1.handle, c2.handle ) )
    }

    #!
     * 限时等待目标协程结束。
     * 返回 true 表示目标已结束（结果已被消费，可用 await 取回，await 对已 Dead 目标立即返回）；
     * 返回 false 表示超时（等待关系已解除，目标继续运行不受影响）。
    !#
    public static bool waitTimeout( Coroutine cor, Int64 millis )
    {
        ret SystemCoroutineWaitTimeout( cor.handle, millis )
    }

    #! ---------- 取消 ---------- !#

    #!
     * 请求取消目标协程。取消是协作式的：目标在下一个调度点（Yield/Await/Sleep 等）抛出
     * 取消异常并结束。对已结束协程调用返回 false。
     * 返回 true 表示已成功登记取消请求。
    !#
    public static bool cancel( Coroutine cor )
    {
        ret SystemCoroutineCancel( cor.handle )
    }
}
