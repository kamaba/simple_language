# 协程（Coroutine）与通道（Channel）

本节描述 S 语言的协程并发模型、`Coroutine` 静态工具类与 `Channel<T>` 通道的使用方式。

设计规格见 `md/design/COROUTINE_DESIGN.md`；SL 层实现位于 `source/Front/Lib/Core/Coroutine.sl` 与 `source/Front/Lib/Core/Container/Channel.sl`；VM 侧实现位于 `src/vm/vm_coroutine.c` 与 `src/vm/system_method_call/coroutine_system_method.c`。

---

## 1. 模型概述

- **协作式单线程调度**：所有协程运行在同一个 VM 线程上，协程只在明确的调度点（`yield` / `await` / `sleep` / `Channel` 阻塞等）让出执行权。协程之间不存在数据竞争，共享静态字段无需加锁。
- **栈式协程**：每个协程持有独立的"帧链 + 求值栈"，对象堆（对象池、LOS、弱引用表）在 VM 内共享。协程挂起时其求值栈原位保留，禁止搬迁（实现红线，见第 9 节）。
- **Int64 句柄**：协程在 SL 层以 `Int64` 句柄表示（VM 内部为协程注册表 id），不是对象引用。
- **root 协程**：CLI 加载的主入口（`static fun()`）被 VM 包装为 root 协程运行，因此从主入口即可直接调用 `await` / `yield` / `sleep` / `current`，`Coroutine.current()` 在主入口返回 root 的非零句柄。

### 1.1 语言限制（重要）

本实现**没有**协程关键字与前端语法支持，以下设计档中的语法糖**均不可用**：

| 不可用写法 | 替代方式 |
|------------|----------|
| `spawn Func(args)` 关键字 | `Coroutine.spawn0..3("methodName", args...)` |
| `await t` 表达式 | `Coroutine.await(handle)` |
| `yield;` 语句 | `Coroutine.yield()` |
| `spawn function(){...}` 函数字面量 | 无——只能 spawn **具名静态方法** |
| `await [t1, t2, ...]` 数组语法糖 | `waitAll2/3`、`waitAny2/3` 固定参数重载 |
| `cor.All(...)` / `cor.Any(...)` 变参 | 同上，固定 2/3 参版本 |
| `cor<T>` 泛型句柄 | 统一 `Int64` |

其它约束：

1. **被 spawn 的方法必须是静态方法**，按**简单名 + 参数个数**在整个汇编内全局解析（`vm_find_method_entry_by_name`），**不区分类名**。因此被 spawn 的方法名必须全工程唯一（测试工程把几十个 `.sl` 编译在一起，同名同参数个数的方法会相互冲突）。
2. **参数为 `object` 形参**（装箱传递）：`int` / `string` 等值可直接传入（自动装箱），协程侧按声明的参数类型绑定，round-trip 无损。最多 3 个参数（`spawn0` ~ `spawn3`）。
3. **前端不发射 `SCHED_CHECK` 公平性指令**：纯计算循环不会自动让出。需要公平交替时必须在循环体内显式调用 `Coroutine.yield()`，否则长循环会独占调度器。
4. **native 函数体内禁止挂起**：挂起只发生在解释循环的指令边界（系统方法入口登记挂起原因后返回，由调度器切换）。
5. **子 VM（静态初始化器）内禁止**使用协程 API。

---

## 2. Coroutine API

### 2.1 生成

| 方法 | 说明 |
|------|------|
| `Coroutine.spawn0(string name) -> Int64` | 以无参静态方法创建并启动协程，返回句柄 |
| `Coroutine.spawn1(string name, object a0) -> Int64` | 1 参版本 |
| `Coroutine.spawn2(string name, object a0, object a1) -> Int64` | 2 参版本 |
| `Coroutine.spawn3(string name, object a0, object a1, object a2) -> Int64` | 3 参版本 |

spawn 立即把协程置为就绪（Ready）并返回句柄；协程体在下一次调度时开始执行。方法名找不到时记录错误日志并返回 0（无效句柄）。

### 2.2 调度控制

| 方法 | 说明 |
|------|------|
| `Coroutine.yield() -> void` | 让出当前协程，允许调度器运行其它就绪协程；不在协程上下文时为空操作 |
| `Coroutine.sleep(Int64 millis) -> void` | 休眠当前协程指定毫秒，期间调度器可运行其它协程；不在协程上下文时退化为阻塞 sleep |

### 2.3 查询

| 方法 | 说明 |
|------|------|
| `Coroutine.current() -> Int64` | 当前协程句柄；root 直接执行上下文返回 0 |
| `Coroutine.status(Int64 h) -> Int32` | 协程状态（`CoroutineStatus` 常量）；无效句柄返回 -1 |
| `Coroutine.blockedReason(Int64 h) -> Int32` | 挂起原因（`CoroutineBlockReason` 常量），用于诊断 |

### 2.4 等待与聚合

| 方法 | 说明 |
|------|------|
| `Coroutine.await(Int64 h) -> object` | 等待目标结束并取回返回值；目标已结束则立即同步返回；void 协程返回 `null`；目标以异常结束则异常向等待者传播。`await` 自己是运行期错误 |
| `Coroutine.waitAll2(h0, h1) -> void` | 等待两个协程全部结束；结果在返回后用 `await` 逐个取回 |
| `Coroutine.waitAll3(h0, h1, h2) -> void` | 三元版本 |
| `Coroutine.waitAny2(h0, h1) -> Int64` | 等任意一个结束，返回先结束者句柄 |
| `Coroutine.waitAny3(h0, h1, h2) -> Int64` | 三元版本 |
| `Coroutine.nextCompleted2(h0, h1) -> Int64` | **非阻塞**取回一个已完成且结果未被消费的句柄，没有则返回 0；会消费该协程的结果（再次查询不再返回） |
| `Coroutine.nextCompleted3(h0, h1, h2) -> Int64` | 三元版本 |
| `Coroutine.waitTimeout(Int64 h, Int64 millis) -> bool` | 限时等待；`true` = 目标已结束（可用 `await` 取回），`false` = 超时（等待关系已解除，目标继续运行不受影响） |

**聚合的错误语义**：`waitAll` / `waitAny` 中任何一个协程以异常结束，会立即取消其余协程并向调用者抛出该异常。

**为什么是固定参数重载**：本语言数组不支持协变（`int[]` 不能赋 `object[]`），且 `Int64` 句柄无法直接装入 `object[]` 元素槽，故聚合 API 采用 2/3 参固定重载而非数组/变参形式。

### 2.5 取消

| 方法 | 说明 |
|------|------|
| `Coroutine.cancel(Int64 h) -> bool` | 请求取消目标协程。取消是**协作式**的：目标在下一个调度点（yield/await/sleep 重入等）抛出取消异常并结束。返回 `true` 表示已登记取消请求；对已结束（Dead）或无效句柄返回 `false` |

---

## 3. 状态常量

```sl
# CoroutineStatus：与 VM 的 VMCoroutineState 一一对应
public class CoroutineStatus extends Object
{
    public static const Int32 Created   = 0    # 已创建未入队
    public static const Int32 Ready     = 1    # 就绪，等待调度
    public static const Int32 Running   = 2    # 正在执行
    public static const Int32 Suspended = 3    # 挂起（yield/await/sleep/channel 阻塞）
    public static const Int32 Dead      = 4    # 已结束（正常返回、异常或取消）
}

# CoroutineBlockReason：挂起原因（诊断用）
public class CoroutineBlockReason extends Object
{
    public static const Int32 None  = 0
    public static const Int32 Yield = 1
    public static const Int32 Sched = 2
    public static const Int32 Await = 3
    public static const Int32 Sleep = 4
    public static const Int32 IO    = 5
}
```

---

## 4. 基本用法

### 4.1 spawn + await 取回返回值

```sl
Worker
{
    # 被 spawn 的方法：静态、名字全工程唯一
    static int coroAdd2( int a, int b )
    {
        ret a + b
    }

    static fun()
    {
        Int64 h = Coroutine.spawn2( "coroAdd2", 3, 4 )
        int result = Coroutine.await( h ) as int    # 7
        global.println( "result = " + result.toString() )
    }
}
```

`await` 返回 `object`，用 `as` 转回原类型；void 协程 `await` 得 `null`：

```sl
static coroSetFlag()
{
    Worker.g_done = true
}

static fun()
{
    Int64 h = Coroutine.spawn0( "coroSetFlag" )
    object r = Coroutine.await( h )    # null（void 协程）
}
```

### 4.2 fire-and-forget（不 await 也要跑完）

后台协程的副作用在其完成后可见。主协程不挂起时后台协程没有执行机会，需轮询让出：

```sl
static fun()
{
    Worker.g_done = false
    Coroutine.spawn0( "coroSetFlag" )
    for Int32 i = 0, i < 1000, i = i + 1
    {
        if Worker.g_done
        {
            break
        }
        Coroutine.sleep( 1 )    # 挂起主协程，给后台协程执行机会
    }
}
```

### 4.3 并行等待与限时等待

```sl
static fun()
{
    Int64 a = Coroutine.spawn2( "coroAdd2", 1, 1 )
    Int64 b = Coroutine.spawn2( "coroAdd2", 2, 2 )
    Coroutine.waitAll2( a, b )                 # 等两个都结束
    int ra = Coroutine.await( a ) as int       # 2
    int rb = Coroutine.await( b ) as int       # 4

    Int64 slow = Coroutine.spawn0( "coroSlow" )
    if ( Coroutine.waitTimeout( slow, 100 ) )
    {
        int rs = Coroutine.await( slow ) as int   # 已结束，取回结果
    }
    else
    {
        # 超时：slow 仍在运行，不受影响
    }
}
```

### 4.4 显式让出（公平性）

纯计算循环不会自动让出（前端不发射 `SCHED_CHECK`），需要交替执行时显式 `yield`：

```sl
static coroFairA()
{
    for Int32 i = 0, i < 10, i = i + 1
    {
        Worker.g_order = Worker.g_order + "A"
        Coroutine.yield()    # 让出，另一个协程得以执行
    }
}
```

### 4.5 协程内再 spawn（树状并发）与深递归

```sl
static int coroNested()
{
    Int64 h = Coroutine.spawn2( "coroAdd2", 40, 2 )
    ret Coroutine.await( h ) as int    # 42
}

# 深递归：协程帧链化，不受旧 64 层栈帧限制
static int coroDeep( int n )
{
    if ( n <= 0 )
    {
        ret 0
    }
    Int64 h = Coroutine.spawn1( "coroDeep", n - 1 )
    ret ( Coroutine.await( h ) as int ) + 1
}
```

---

## 5. Channel\<T\> 通道

`Channel<T>` 是 CSP 风格的协程间通信通道。通道本体在 VM 端（文件级静态注册表），SL 对象仅持有 `Int64` 句柄。

| 成员 | 说明 |
|------|------|
| `Channel<T>.create() -> Channel<T>` | 创建无缓冲上限（unbounded）通道 |
| `Channel<T>.create( int capacity ) -> Channel<T>` | 创建指定容量通道；`capacity <= 0` 视为无上限 |
| `send( T value ) -> void` | 发送：缓冲未满则入队并唤醒一个等待的接收者；缓冲满则挂起发送者直至有空位；**对已关闭通道 send 抛出异常** |
| `recv() -> T` | 接收：缓冲非空则取队头并唤醒一个等待的发送者；缓冲空且未关闭则挂起接收者；**通道已关闭且缓冲空时返回 `null`** |
| `close() -> void` | 关闭通道，唤醒全部等待的发送者与接收者 |
| `get int count` | 缓冲内当前元素个数 |
| `get bool isClosed` | 通道是否已关闭 |

### 5.1 生产者-消费者

`recv` 在通道关闭且缓冲耗尽后返回 `null`，这是标准的消费终止信号：

```sl
Producer
{
    static int g_sum = 0

    static coroProduce( Channel<object> ch )
    {
        for Int32 i = 0, i < 5, i = i + 1
        {
            ch.send( i )
        }
        ch.close()    # 发完关闭
    }

    static coroConsume( Channel<object> ch )
    {
        while ( true )
        {
            object v = ch.recv()
            if ( v == null )
            {
                break            # 通道已关闭且缓冲空
            }
            Producer.g_sum = Producer.g_sum + ( v as int )
        }
    }

    static fun()
    {
        Channel<object> ch = Channel<object>.create( 4 )
        Int64 p = Coroutine.spawn1( "coroProduce", ch )
        Int64 c = Coroutine.spawn1( "coroConsume", ch )
        Coroutine.waitAll2( p, c )
        global.println( "sum = " + Producer.g_sum.toString() )    # 10
    }
}
```

> 提示：通道元素类型建议用 `Channel<object>`（send 时装箱、recv 后 `as` 拆箱）。值类型 `T`（如 `Channel<int>`）的 `recv` 在关闭返回 `null` 时拆箱行为不保证，判断终止请使用 `object` 元素类型。

### 5.2 有界通道与背压

容量满时 `send` 自动挂起让出（不忙等），形成天然背压：

```sl
static fun()
{
    # 容量 2：生产者第 3 个 send 必然挂起，直至消费者腾出空位
    Channel<object> ch = Channel<object>.create( 2 )
    Int64 p = Coroutine.spawn1( "coroBoundedProduce", ch )
    Int64 c = Coroutine.spawn1( "coroDelayedConsume", ch )
    Coroutine.waitAll2( p, c )
}
```

### 5.3 多生产者 / 多消费者

同一个静态方法可以多次 spawn（每次一个新协程实例），配合共享静态字段统计：

```sl
static fun()
{
    Channel<object> ch = Channel<object>.create( 8 )
    # 4 个生产者（同一方法 spawn 4 次）
    Int64 p0 = Coroutine.spawn1( "coroProduce10", ch )
    Int64 p1 = Coroutine.spawn1( "coroProduce10", ch )
    Int64 p2 = Coroutine.spawn1( "coroProduce10", ch )
    Int64 p3 = Coroutine.spawn1( "coroProduce10", ch )
    Int64 c  = Coroutine.spawn1( "coroConsume40", ch )
    Coroutine.waitAll2( p0, p1 )
    Coroutine.waitAll2( p2, p3 )
    Coroutine.await( c )
}
```

---

## 6. 错误与取消

### 6.1 异常跨协程传播

协程内未捕获的异常使协程进入 Dead 状态并记录；`await` / `waitAll` / `waitAny` 处会向等待者重新抛出：

```sl
enum WorkError extends Error
{
    Boom = { code = 201, message = "boom" }
}

static coroBoom() throws
{
    throw WorkError.Boom
}

static fun()
{
    Int64 h = Coroutine.spawn0( "coroBoom" )
    label waitBlock
    {
        try Coroutine.await( h )    # 异常在此重抛
    }
    catch WorkError ex
    {
        global.println( "code = " + ex.code.toString() )    # 201
    }
    # 出错协程状态为 Dead
    global.println( Coroutine.status( h ) == CoroutineStatus.Dead )
}
```

### 6.2 裸 catch 约定（重要）

VM 侧抛出的**取消异常（-63）**与**非法操作异常（-64，如 await 自己）**其异常值为 `null`。捕获这两类异常必须用**裸 `catch{}`**（不绑定变量）：

```sl
static fun()
{
    # await 自己 -> C 侧非法操作（null 异常值）
    Int64 self = Coroutine.current()
    label selfBlock
    {
        try Coroutine.await( self )
    }
    catch      # 必须裸 catch：绑定变量会绑到 null
    {
        global.println( "cannot await self" )
    }
}
```

只有 SL 层 `throw` 的枚举异常才可用 `catch XxxError ex` 绑定（异常值非 null）。

### 6.3 协作式取消

`cancel` 只是登记请求；目标在下一个调度点以取消异常终止，其 `finally` 块保证执行：

```sl
static coroCancelTarget()
{
    label guard
    {
        while ( true )
        {
            Coroutine.yield()    # 取消请求在此处生效
        }
    }
    finally
    {
        App.g_cleaned = true     # 取消时也会执行
    }
}

static fun()
{
    App.g_cleaned = false
    Int64 h = Coroutine.spawn0( "coroCancelTarget" )
    Coroutine.sleep( 10 )
    bool ok = Coroutine.cancel( h )       # true：已登记
    label waitBlock
    {
        try Coroutine.await( h )          # 取消异常传播到等待者
    }
    catch
    {
    }
    global.println( ok && App.g_cleaned )    # true true
}
```

对已结束（Dead）的协程调用 `cancel` 返回 `false`。`waitAll` / `waitAny` 中某协程异常结束时，其余协程会被自动取消。

---

## 7. 调度行为速查

| 场景 | 行为 |
|------|------|
| 主入口（root）调用 `await` | 合法：root 也是协程，等待时挂起，由调度器驱动目标 |
| 主入口调用 `yield` / `sleep` | 合法：挂起主协程，其它就绪协程运行 |
| 无就绪协程时的 timer | 到期即唤醒对应协程 |
| 全部协程结束 | 调度器收敛退出（`vm_scheduler_enter` 的 drain 循环） |
| 就绪队列与定时器 | 就绪队列先于定时器处理；被目标死亡唤醒的等待者先于其残余定时器运行 |
| 多次唤醒同一协程 | 有守卫，重复入队安全 |

**编程建议**：

- 长计算循环内周期性 `Coroutine.yield()`，避免饿死其它协程。
- fire-and-forget 协程也建议最终 `await`（或 `waitTimeout`）一次，确保异常被观测、资源被回收。
- 用 `Channel` + `close` 表达"生产结束"，用 `waitTimeout` 表达"限时等待"，避免手写轮询。

---

## 8. 实现红线（摘要）

完整版见 `COROUTINE_DESIGN.md` 第 9 章：

1. **求值栈禁止搬迁**：栈槽可存原生指针，分段栈只追加不 realloc；任何"栈复制/搬迁"都会造成悬垂指针。
2. **native 函数体内禁止挂起**：挂起只发生在解释循环的安全点（指令边界）。系统方法要么瞬时返回，要么入口登记挂起原因。
3. **协程不持有独立堆**：对象池、LOS、弱引用表全部 per-VM 共享；协程只私有"帧链 + 求值栈"。
4. **子 VM 禁止挂起**：静态初始化器等子 VM 上下文没有调度器，不得使用协程挂起类 API。

---

## 9. 相关文件

| 文件 | 内容 |
|------|------|
| `source/Front/Lib/Core/Coroutine.sl` | `Coroutine` / `CoroutineStatus` / `CoroutineBlockReason` |
| `source/Front/Lib/Core/Container/Channel.sl` | `Channel<T>` |
| `source/Front/Lib/Core.jsonc` | 系统调用与类注册 |
| `csimple_lang/src/vm/vm_coroutine.c` | 协程核心：创建/调度器/帧链/唤醒/取消 |
| `csimple_lang/src/vm/system_method_call/coroutine_system_method.c` | `SystemCoroutine*` 与 `SystemChannel*` 系统调用实现 |
| `test/BaseTest/CoroutineTest.sl` | A-J 组全量验收用例（本节示例的可运行版本） |
| `md/design/COROUTINE_DESIGN.md` | 设计规格（含测试覆盖矩阵） |
