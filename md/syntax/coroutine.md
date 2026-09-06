# 协程（Coroutine）与通道（Channel）

> **本文档以「当前实现」为准**，对应 `source/Front/Lib/Core/Coroutine.sl`、`source/Front/Lib/Core/Container/Channel.sl`
> 与 `test/BaseTest/CoroutineTest.sl`（1249 行，A–N + W 共 14 组验收用例）。
> 设计规格见 `md/design/COROUTINE_DESIGN.md`——**该设计档部分内容尚未实现**，差异清单见 §13。

---

## 目录

1. [模型概述](#1-模型概述)
2. [快速开始](#2-快速开始)
3. [类型体系](#3-类型体系)
4. [关键字：spawn / await / yield](#4-关键字spawn--await--yield)
5. [生成协程（spawn 全家族）](#5-生成协程spawn-全家族)
6. [调度控制](#6-调度控制)
7. [查询](#7-查询)
8. [等待与聚合](#8-等待与聚合)
9. [取消](#9-取消)
10. [错误传播](#10-错误传播)
11. [Channel\<T\> 通道](#11-channelt-通道)
12. [API 速查总表](#12-api-速查总表)
13. [与 COROUTINE_DESIGN.md 的差异](#13-与-coroutine_designmd-的差异设计档中未实现的部分)
14. [限制与约定（必读）](#14-限制与约定必读)
15. [实战用例集](#15-实战用例集)
16. [实现要点与排障](#16-实现要点与排障)

---

## 1. 模型概述

| 特性 | 说明 |
|---|---|
| **协作式单线程调度** | 所有协程跑在同一个 VM 线程上，只在明确调度点（`yield` / `await` / `sleep` / `waitUntil` / Channel 阻塞）让出。**协程之间不存在数据竞争，共享静态字段无需加锁** |
| **有栈协程（stackful）** | 每个协程持有独立的**帧链 + 私有求值栈**；可在任意调用深度挂起 |
| **求值栈禁止搬迁** | 栈槽可存原生指针，挂起时原位保留，绝不 realloc |
| **对象堆共享** | 对象池、LOS、弱引用表全部 per-VM 共享；协程只私有"帧链 + 求值栈" |
| **根协程（root）** | 主入口 `static fun()` 被 VM 包装为 root 协程，因此**从主入口即可直接用 `await` / `yield` / `sleep` / `current`** |
| **`Task` 即句柄** | 协程在 SL 层是 `Task` 对象（包装 C VM 的 `Int64` 注册表 id）。**同一句柄恒对应同一 `Task` 实例**，可用 `==` 判等 |

### 1.1 生命周期

```
spawn ──► Created ──► Ready ──► Running ──► Suspended ──► Running ──► Dead
                        ▲          │            ▲                       │
                        └──────────┘            │                       │
                         (yield/sleep 到期)      (await/waitUntil/Channel) │
                                                              return 或异常或取消
```

| 状态 | 值 | 含义 |
|---|:---:|---|
| `Created` | 0 | 已创建未入队 |
| `Ready` | 1 | 在就绪队列，等待调度 |
| `Running` | 2 | 正在执行 |
| `Suspended` | 3 | 挂起中（yield/await/sleep/Channel 阻塞） |
| `Dead` | 4 | 已结束（正常返回 / 异常 / 取消） |

| 挂起原因（`blockedReason`） | 值 | 触发 |
|---|:---:|---|
| `None` | 0 | 未挂起 |
| `Yield` | 1 | `yieldNow()` |
| `Sched` | 2 | 调度切片（预留） |
| `Await` | 3 | `awaitHandle` / `waitAll` / `waitAny` |
| `Sleep` | 4 | `sleep` / `waitUntil` / `waitTimeout` |
| `IO` | 5 | 阻塞 IO（**预留未使用**） |

### 1.2 源码位置

| 层 | 文件 |
|---|---|
| SL API | `source/Front/Lib/Core/Coroutine.sl`、`source/Front/Lib/Core/Container/Channel.sl` |
| 系统调用注册 | `source/Front/Lib/Core/Core.jsonc` → `systemCalls[]`（`cvmFunction` 为 C 侧函数名） |
| 关键字展开 | `source/Front/Compile/Parse/StructParseToSyntax.cs` |
| C VM 协程核心 | `csimple_lang/src/vm/runtime/coroutine/vm_coroutine.c/.h` |
| C VM 系统调用 | `csimple_lang/src/vm/system_method_call/coroutine_system_method.c/.h` |
| 验收用例 | `test/BaseTest/CoroutineTest.sl` |

---

## 2. 快速开始

```sl
import CSharp.System;

Worker
{
    # 被 spawn 的方法：必须是静态方法，且名字全工程唯一
    static int coroAdd2( int a, int b )
    {
        ret a + b
    }

    static fun()
    {
        # 1) 创建并启动
        Task h = Coroutine.spawn2( "coroAdd2", 3, 4 )

        # 2) 等待并取回返回值（await 关键字）
        int r = await h as int          # 7

        global.println( "result = " + r.toString() )
    }
}
```

> `spawn` 立即把协程置为 **Ready** 并返回句柄，**不阻塞当前协程**；协程体在下一次调度才开始执行。

---

## 3. 类型体系

```
Coroutine              静态管理器（别名 coro），所有操作收发 Task 对象
Task                   协程对象，spawn 系列的返回类型
CoroutineStatus        状态常量类
CoroutineBlockReason   挂起原因常量类
Channel<T>             CSP 通道
```

### 3.1 `Task`（协程对象）

由 C VM 侧注册表创建，**SL 侧无公开构造**。同一句柄恒为同一实例。

| 成员 | 类型 | 说明 |
|---|---|---|
| `awaitHandle()` | `object` | 等待本协程结束并取回返回值（等价 `Coroutine.awaitHandle(this)`） |
| `cancel()` | `bool` | 请求取消；已结束返回 `false` |
| `status` | `Int32` | 当前状态（`CoroutineStatus` 常量） |
| `blockedReason` | `Int32` | 挂起原因（`CoroutineBlockReason` 常量），诊断用 |
| `isDead` | `bool` | `status == Dead` |
| `handle` | `Int64` | 原始句柄，诊断用 |

### 3.2 `Coroutine`（管理器，`@Nickname("coro")`）

`Coroutine.xxx(...)` 与 `coro.xxx(...)` 完全等价。

### 3.3 常量类

```sl
public class CoroutineStatus extends Object
{
    public static const Int32 Created   = 0
    public static const Int32 Ready     = 1
    public static const Int32 Running   = 2
    public static const Int32 Suspended = 3
    public static const Int32 Dead      = 4
}

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

## 4. 关键字：spawn / await / yield

三个关键字都在 **Node 层展开**为库调用（`StructParseToSyntax.cs`），因此显式调用库方法同样合法。

| 关键字 | 语法身份 | 展开为 |
|---|---|---|
| `spawn E` | 一元前缀**表达式** | `Coroutine.spawnClosure0..3(...)` / `Coroutine.spawnInstance0..3(...)` |
| `await e` | 一元前缀**表达式** | `Coroutine.awaitHandle( e )` |
| `yield` | **语句**（无参） | `Coroutine.yieldNow()` |

```ebnf
spawn_expr := 'spawn' ( call_expr | function_expr )
await_expr := 'await' unary_expr
await_stmt := await_expr ';'
yield_stmt := 'yield' ';'
```

### 4.1 `spawn`

`spawn` 后必须跟**调用表达式**或**函数字面量**：

```sl
Task h1 = spawn coroAdd2( 1, 2 )                 # 具名静态方法
Task h2 = spawn adder( 1, 2 )                    # function 变量
Task h3 = spawn function() { ... }               # 匿名闭包
Task h4 = spawn c1.coroInstAdd2( 3, 4 )          # 实例方法
Task h5 = spawn this.coroInstAdd2( 1, 2 )        # 实例方法内 this 链
Task h6 = spawn mk()                             # 无参
```

⚠️ **`spawn x + 1` 这类任意表达式非法。**

### 4.2 `await`

`await` 是**表达式**，可嵌套在任意表达式位置：

```sl
var v  = await t
ret await t
int x  = 1 + ( await t as int )
await h                              # 语句形态：忽略返回值
```

### 4.3 `yield`

⚠️ **`yield` 只支持裸语句，不支持带参**：

```sl
yield                                # ✅ 合法
yield return new WaitHpLess(player)  # ❌ 编译期报错
```

带参时编译器报：

```
Error yield 不支持带表达式参数, 等待条件请使用 Coroutine.waitUntil( 谓词闭包 )
```

需要"等待某条件"时用 [`waitUntil`](#63-waituntil--条件等待类似-unity-的-waituntil)。

---

## 5. 生成协程（spawn 全家族）

### 5.1 具名静态方法

按**简单名 + 参数个数**在整个汇编内全局解析（C 侧 `vm_find_method_entry_by_name`），**不区分类名**。

| 方法 | 说明 |
|---|---|
| `spawn0( string methodName )` | 无参 |
| `spawn1( string methodName, object arg0 )` | 1 参 |
| `spawn2( string methodName, object arg0, object arg1 )` | 2 参 |
| `spawn3( string methodName, object arg0, object arg1, object arg2 )` | 3 参 |
| `spawnByName( string methodName, params Array<object> objs )` | 数组形参通用形式 |

```sl
static int coroAdd2( int a, int b ) { ret a + b }
static int coroSum3( int a, int b, int c ) { ret a + b + c }

Task h1 = Coroutine.spawn2( "coroAdd2", 3, 4 )        # 7
Task h2 = Coroutine.spawn3( "coroSum3", 1, 2, 3 )     # 6
Task h3 = Coroutine.spawn0( "coroSetFlag" )           # void 协程
```

⚠️ **被 spawn 的方法名必须全工程唯一**。测试工程把几十个 `.sl` 编译在一起，同名同参数个数的方法会相互冲突——`CoroutineTest.sl` 因此统一用 `coro` / `coroKw` 前缀命名。

### 5.2 闭包 / 函数变量

| 方法 | 说明 |
|---|---|
| `spawnClosure0( object closure )` | 无参闭包 |
| `spawnClosure1( object closure, object arg0 )` | 1 参 |
| `spawnClosure2( object closure, object arg0, object arg1 )` | 2 参 |
| `spawnClosure3( object closure, object arg0, object arg1, object arg2 )` | 3 参 |
| `spawnClosure( object closure, Array<object> objs )` | 数组形参通用形式 |

闭包可为**匿名闭包**、`function` 声明变量、或 `Func<>` 类型变量。

```sl
# function 声明变量
function adder = function( int a, int b ) { ret a + b; }
Task h = spawn adder( 1, 2 )
int r = await h as int                    # 3

# Func<> 签名类型
Func<int,int,int> mulf = function( int a, int b ) { ret a * b; }
Task hm = spawn mulf( 2, 5 )
int rm = await hm as int                  # 10

# 匿名闭包（体内可 yield）
Task ha = spawn function()
{
    g_sum = g_sum + 55;
    g_order = g_order + "x";
    yield
    g_order = g_order + "y";
}
await ha
```

`Func<>` 签名规则：`Func<返回类型, 参数类型...>`；`void` 仅允许出现在返回类型位置。

```sl
Func<int,int,int>   f2     # (int,int) -> int
Func<void,int,int>  fv     # (int,int) -> void
Func<int>           f0     # () -> int
Func<void>          fv0    # () -> void
Function            loose  # 宽松类型，返回 object
```

### 5.3 实例方法

| 方法 | 说明 |
|---|---|
| `spawnInstance0( object receiver, string methodName )` | 无参 |
| `spawnInstance1( object receiver, string methodName, object arg0 )` | 1 参 |
| `spawnInstance2( object receiver, string methodName, object arg0, object arg1 )` | 2 参 |
| `spawnInstance3( object receiver, string methodName, object arg0, object arg1, object arg2 )` | 3 参 |

`receiver` 绑定到被调方法的隐式 `this`，其实例字段在协程内**跨 yield 保持**，多实例互不干扰。

```sl
CoroInstTarget
{
    int instVal = 0

    int coroInstDouble( int v )
    {
        this.instVal = this.instVal + v
        yield;
        Coroutine.sleep( 1000 )
        this.instVal = this.instVal + v
        ret this.instVal
    }

    int coroInstSpawnThis()
    {
        Task h = spawn this.coroInstAdd2( 1, 2 )     # 实例方法内 spawn this.方法
        ret await h as int
    }
}

var cA = CoroInstTarget()
var cB = CoroInstTarget()
Task hA = spawn cA.coroInstDouble( 5 )    # 10
Task hB = spawn cB.coroInstDouble( 7 )    # 14
```

---

## 6. 调度控制

### 6.1 `yieldNow()` — 让出一次

```sl
public static void yieldNow()      # yield; 关键字即其语法糖
```

把自己排到**就绪队列队尾**并让出，调度器取队首的下一个协程运行。**不在协程上下文时为空操作。**

```sl
static coroFairA()
{
    for Int32 i = 0, i < 10, i = i + 1
    {
        g_order = g_order + "A"
        Coroutine.yieldNow()
    }
}
# 与 coroFairB 并行 -> "ABABABABABABABABABAB"
```

⚠️ **前端不发射 `SCHED_CHECK` 公平性指令**——纯计算循环不会自动让出。需要公平交替时**必须显式调用**，否则长循环会独占调度器。

### 6.2 `sleep( millis )` — 休眠

```sl
public static void sleep( Int64 millis )
```

挂起指定毫秒，**期间调度器可运行其它协程**。不在协程上下文时**退化为真阻塞 sleep**（会卡住整个线程！）。

```sl
# 并行：总耗时 ≈ max(100,100)，不是 200
Task e1a = Coroutine.spawn1( "coroSleepMs", 100 )
Task e1b = Coroutine.spawn1( "coroSleepMs", 100 )
Coroutine.waitAll2( e1a, e1b )          # 约 100ms

Task e2 = Coroutine.spawn1( "coroSleepMs", 0 )   # Sleep(0)：只让出，不阻塞
```

### 6.3 `waitUntil()` — 条件等待（类似 Unity 的 WaitUntil）

```sl
public static void waitUntil( Function predicate )
```

挂起当前协程直至谓词闭包返回 `true`。**以 1ms 间隔轮询**（内部 `sleep(1)`）。

```sl
CoroutineTest.g_wdone = false
Task setter = Coroutine.spawn0( "coroWSetFlag" )    # 50ms 后置位 g_wdone

function pred = function()
{
    ret CoroutineTest.g_wdone
}
Coroutine.waitUntil( pred )               # 挂起，直到 g_wdone 为 true
global.println( "条件满足" )
```

| 约束 | 说明 |
|---|---|
| **谓词必须是无副作用的纯查询** | 源码注释明写"不得再挂起" |
| **谓词内禁止 `await` / `sleep` / `yield`** | 会破坏栈 |
| **语义方向** | `waitUntil` = 等到谓词为 **true** 才继续（Unity 的 `keepWaiting` 方向相反） |
| **谓词立即为 true** | 不挂起，直接通过 |
| **root 直接执行** | 退化为阻塞轮询 |

#### 模拟 Unity 的 `CustomYieldInstruction`

Unity 的 `yield return new WaitHpLess(player)` 在 SL 里没有对应语法，但可以用抽象类复刻其语义：

```sl
abstract class CustomYieldInstruction extends Object
{
    abstract get bool keepWaiting()
}

class WaitHpLess extends CustomYieldInstruction
{
    Player player
    _init_( Player p ) { this.player = p }

    override get bool keepWaiting()
    {
        ret this.player.hp >= 20        # true = 继续等（与 Unity 语义一致）
    }
}

# 驱动器（建议加进 Coroutine.sl，与 waitUntil 并列）
public static void waitFor( CustomYieldInstruction inst )
{
    while ( inst.keepWaiting )
    {
        Coroutine.sleep( 1 )
    }
}

# 使用
Coroutine.waitFor( WaitHpLess(player) )
```

⚠️ SL 没有 `=>` 表达式体，必须写完整方法体；类继承用 `extends`，接口用 `implements`；语言**去掉了 `virtual` 关键字**，重写父类方法必须显式 `override`。

---

## 7. 查询

| 方法 | 返回 | 说明 |
|---|---|---|
| `current()` | `Task` | 当前协程对象；root 直接执行上下文返回 `null` |
| `status( Task )` | `Int32` | 状态常量；无效句柄返回 `-1` |
| `blockedReason( Task )` | `Int32` | 挂起原因，诊断用 |

```sl
Task l2 = Coroutine.spawn1( "coroSleepMs", 50 )
Coroutine.sleep( 10 )

bool suspended = l2.status == CoroutineStatus.Suspended
bool sleeping  = l2.blockedReason == CoroutineBlockReason.Sleep
bool alive     = ( l2.isDead == false ) && ( l2.handle != 0 )

Coroutine.awaitHandle( l2 )
bool dead = l2.isDead && l2.status == CoroutineStatus.Dead
```

**注册表同一性**：同一句柄恒为同一实例，`==` 即引用判等。

```sl
Task cur1 = Coroutine.current()
Task cur2 = Coroutine.current()
check( cur1 == cur2 )            # true
```

---

## 8. 等待与聚合

### 8.1 `awaitHandle()` — 等待单个

```sl
public static object awaitHandle( Task cor )     # await 关键字即其语法糖
Task.awaitHandle()                               # 实例方法等价形式
```

| 情形 | 行为 |
|---|---|
| 目标已 `Dead` | **不挂起**，立即同步返回结果 |
| 目标活跃 | 挂起当前协程，注册到目标的 waiter 列表 |
| 目标 `void` 返回 | 得到 `null` |
| 目标以异常结束 | **异常向等待者传播** |
| `await` 自己 | 运行期错误（C 侧非法操作 `-64`） |

```sl
Task h1 = Coroutine.spawn2( "coroAdd2", 7, 8 )
int r  = Coroutine.awaitHandle( h1 ) as int      # 15
int r2 = h1.awaitHandle() as int                 # 实例方法，等价
```

### 8.2 `waitAll` — 等全部

```sl
public static void waitAll2( Task c0, Task c1 )
public static void waitAll3( Task c0, Task c1, Task c2 )
public static void waitAll( params Array<Task> cors )
```

无返回值——**结果在返回后用 `await` 逐个取回**。数组为 `null` 或空时平凡完成。

**错误语义**：任一协程以异常结束 → **立即取消其余协程**并向调用者抛出该异常。

```sl
Task a = Coroutine.spawn2( "coroAdd2", 1, 1 )
Task b = Coroutine.spawn2( "coroAdd2", 2, 2 )
Coroutine.waitAll2( a, b )
int ra = Coroutine.awaitHandle( a ) as int       # 2
int rb = Coroutine.awaitHandle( b ) as int       # 4
```

⚠️ 因为数组不支持协变（`int[]` 不能赋 `object[]`）且 `Int64` 句柄无法直接装入 `object[]`，聚合 API 采用 **2/3 参固定重载**为主——`waitAll(Array<Task>)` / `waitAny(Task[])` 是后来的补充形式。

### 8.3 `waitAny` — 等任意一个

```sl
public static Task waitAny2( Task c0, Task c1 )
public static Task waitAny3( Task c0, Task c1, Task c2 )
public static Task waitAny( params Task[] cors )
```

返回**先结束者**的协程对象；数组为 `null` 或空时立即返回 `null`。

```sl
Task slow = Coroutine.spawn1( "coroSleepMs", 100 )
Task fast = Coroutine.spawn1( "coroSleepMs", 10 )
Task winner = Coroutine.waitAny2( slow, fast )
check( winner == fast )                  # == 引用判等成立
Coroutine.awaitHandle( slow )            # 清理：等慢者也结束
```

⚠️ 失败者**不会被取消**，会继续运行到结束，需要自行 `await` 收尾。

### 8.4 `nextCompleted` — 非阻塞取回

```sl
public static Task nextCompleted2( Task c0, Task c1 )
public static Task nextCompleted3( Task c0, Task c1, Task c2 )
```

**非阻塞**：按参数顺序扫描，返回第一个 **`Dead` 且结果未被消费** 的协程；没有则返回 `null`。
命中后标记 `consumed`，**再次查询同一协程不再返回**（但 `await` 仍然可用）。

```sl
Task a = Coroutine.spawn1( "coroSleepMs", 50 )
Task b = Coroutine.spawn1( "coroSleepMs", 10 )
Coroutine.sleep( 20 )                    # 此刻 b 已完成、a 未完成

Task got1 = Coroutine.nextCompleted2( a, b )     # == b
Task got2 = Coroutine.nextCompleted2( a, b )     # null（b 已消费）

Coroutine.awaitHandle( a )               # a 完成后仍可消费
Task got3 = Coroutine.nextCompleted2( a, b )     # == a
Task got4 = Coroutine.nextCompleted2( a, b )     # null
```

### 8.5 `waitTimeout` — 限时等待

```sl
public static bool waitTimeout( Task cor, Int64 millis )
```

| 返回 | 含义 |
|---|---|
| `true` | 目标已结束，可用 `await` 取回结果 |
| `false` | **超时**：等待关系已解除，**目标继续运行不受影响** |

```sl
Task j3 = Coroutine.spawn1( "coroSleepMs", 500 )

bool ok1 = Coroutine.waitTimeout( j3, 100 )      # false（超时）
Int32 st = Coroutine.status( j3 )                # Suspended（仍在跑）

bool ok2 = Coroutine.waitTimeout( j3, 1000 )     # true（这次等到了）
```

---

## 9. 取消

取消是**协作式**的：`cancel()` 只登记请求，目标在**下一个调度点**（`yield` / `await` / `sleep` / `waitUntil` 的重入入口）抛出取消异常并结束。

```sl
public static bool cancel( Task cor )
Task.cancel()                            # 实例方法等价形式
```

| 返回 | 含义 |
|---|---|
| `true` | 已登记取消请求 |
| `false` | 目标已 `Dead` 或句柄无效 |

```sl
static coroCancelTarget()
{
    label cancelBlock
    {
        while ( true )
        {
            Coroutine.yieldNow()         # 取消请求在此处生效
        }
    }
    finally
    {
        CoroutineTest.g_done = true      # 取消时 finally 保证执行
    }
}

Task g5 = Coroutine.spawn0( "coroCancelTarget" )
Coroutine.sleep( 10 )                    # 让目标先跑起来
bool cancelled = Coroutine.cancel( g5 )  # true
label g5block { try Coroutine.awaitHandle( g5 ) }
catch { }                                # 取消异常在此被捕获
check( CoroutineTest.g_done )            # finally 已执行
```

⚠️ **取消异常（`error_code = -63`）与非法操作异常（`-64`）的异常值为 `null`**，捕获时**必须用裸 `catch{}`**（不绑定变量）；绑定变量会绑到 `null`。只有 SL 层 `throw` 的枚举异常才能用 `catch XxxError ex` 绑定。

**`waitAll` / `waitAny` 中某协程异常结束时，其余协程会被自动取消。**

---

## 10. 错误传播

协程内未捕获的异常使其进入 `Dead` 并记录；`await` / `waitAll` / `waitAny` 处向等待者**重新抛出**。

```sl
enum CoroTestError extends Error
{
    BoomError = { code = 201, message = "coro-boom" }
}

static coroThrowErr() throws
{
    throw CoroTestError.BoomError
}

static fun()
{
    Task g1 = Coroutine.spawn0( "coroThrowErr" )
    int code1 = 0
    label g1block
    {
        try Coroutine.awaitHandle( g1 )
    }
    catch CoroTestError ex
    {
        # catch 绑定变量静态类型为 object（无字段访问），与枚举成员比较
        if ex == CoroTestError.BoomError
        {
            code1 = 201
        }
    }
    check( code1 == 201 )
    check( Coroutine.status( g1 ) == CoroutineStatus.Dead )
}
```

### 10.1 嵌套捕获

```sl
static int coroCatchInner() throws
{
    Task h = Coroutine.spawn0( "coroThrowErr" )
    int caught = 0
    label innerBlock
    {
        try Coroutine.awaitHandle( h )
    }
    catch
    {
        caught = 1                      # 子协程异常在父协程内被接住
    }
    ret caught
}
```

### 10.2 `waitAll` 的失败传播

```sl
Task c6a = Coroutine.spawn0( "coroThrowErr" )
Task c6b = Coroutine.spawn1( "coroSleepMs", 1000 )

label c6block
{
    try Coroutine.waitAll2( c6a, c6b )   # 立即失败
}
catch CoroTestError ex { ... }

# c6b 被自动取消，需裸 catch 收尾
label c6cleanup { try Coroutine.awaitHandle( c6b ) }
catch { }
check( Coroutine.status( c6b ) == CoroutineStatus.Dead )
```

### 10.3 挂起后 `finally` 仍执行

```sl
static coroFinallySleep()
{
    label finBlock
    {
        Coroutine.sleep( 10 )            # 挂起
    }
    finally
    {
        CoroutineTest.g_done = true      # 无论正常/异常/取消都会执行
    }
}
```

---

## 11. `Channel<T>` 通道

CSP 风格，通道本体在 C VM 端（文件级静态注册表），SL 对象仅持有 `Int64` 句柄。

| 成员 | 说明 |
|---|---|
| `_init_()` / `create()` | 无缓冲上限（unbounded，`send` 永不阻塞） |
| `_init_( int capacity )` / `create( int capacity )` | 指定容量，`capacity <= 0` 视为 unbounded |
| `send( T value )` | 缓冲未满则入队并唤醒一个接收者；**满则挂起**；已关闭则抛异常 |
| `recv()` | 缓冲非空则取队头并唤醒一个发送者；**空且未关闭则挂起**；**已关闭且空则返回 `null`** |
| `close()` | 关闭通道，唤醒**全部**等待的发送者与接收者 |
| `count` | 缓冲内元素个数 |
| `isClosed` | 是否已关闭 |

```sl
Channel<object> ch = Channel<object>.create( 4 )
```

### 11.1 基本生产-消费（`close` 后 `recv` 得 `null` 终止）

```sl
static coroF1Produce( Channel<object> ch )
{
    for Int32 i = 0, i < 5, i = i + 1 { ch.send( i ) }
    ch.close()                            # 关闭 -> 消费者 recv 得 null
}

static coroF1Consume( Channel<object> ch )
{
    while ( true )
    {
        object v = ch.recv()
        if ( v == null ) { break }        # 通道已关闭且缓冲空
        g_f1sum = g_f1sum + ( v as int )
    }
}
```

### 11.2 有界通道满时 `send` 挂起（不忙等）

```sl
Channel<object> ch2 = Channel<object>.create( 2 )
Task p = Coroutine.spawn1( "coroF2Produce", ch2 )   # 连发 1,2,3
Task c = Coroutine.spawn1( "coroF2Consume", ch2 )   # sleep(10) 后消费 3 个
Coroutine.waitAll2( p, c )                          # 第 3 个 send 挂起让出
```

### 11.3 多生产者 / 多消费者

```sl
# 4 个生产者各发 10 个 -> 单消费者收 40 个，不丢不重
Channel<object> ch3 = Channel<object>.create( 8 )
Task f3c  = Coroutine.spawn1( "coroF3Consume", ch3 )
Task f3p0 = Coroutine.spawn1( "coroF3Produce", ch3 )
Task f3p1 = Coroutine.spawn1( "coroF3Produce", ch3 )
Task f3p2 = Coroutine.spawn1( "coroF3Produce", ch3 )
Task f3p3 = Coroutine.spawn1( "coroF3Produce", ch3 )
Coroutine.waitAll2( f3p0, f3p1 )
Coroutine.waitAll2( f3p2, f3p3 )
Coroutine.awaitHandle( f3c )
check( g_f3count == 40 )

# 1 个生产者发 100 个 -> 4 个消费者分摊收完 100 个
```

---

## 12. API 速查总表

### `Coroutine`（别名 `coro`）

| 分类 | 方法 |
|---|---|
| **生成·静态方法** | `spawn0` `spawn1` `spawn2` `spawn3` `spawnByName` |
| **生成·实例方法** | `spawnInstance0` `spawnInstance1` `spawnInstance2` `spawnInstance3` |
| **生成·闭包** | `spawnClosure0` `spawnClosure1` `spawnClosure2` `spawnClosure3` `spawnClosure` |
| **调度控制** | `yieldNow` `sleep` `waitUntil` |
| **查询** | `current` `status` `blockedReason` |
| **等待聚合** | `awaitHandle` `waitAll2` `waitAll3` `waitAll` `waitAny2` `waitAny3` `waitAny` `nextCompleted2` `nextCompleted3` `waitTimeout` |
| **取消** | `cancel` |

### `Task`

| 成员 | 类型 |
|---|---|
| `awaitHandle()` | `object` |
| `cancel()` | `bool` |
| `status` | `Int32` |
| `blockedReason` | `Int32` |
| `isDead` | `bool` |
| `handle` | `Int64` |

### `Channel<T>`

| 成员 | 类型 |
|---|---|
| `send( T )` | `void` |
| `recv()` | `T` |
| `close()` | `void` |
| `count` | `int` |
| `isClosed` | `bool` |

---

## 13. 与 COROUTINE_DESIGN.md 的差异（设计档中**未实现**的部分）

> 设计档写于实现之前，以下条目**尚未落地**。照设计档写会编译失败。

| 设计档描述 | 实际 |
|---|---|
| 协程句柄类型 `Coroutine`，短别名 `cor` | 管理器类 `Coroutine`（别名 `coro`），协程对象类 **`Task`** |
| `cor.Current` 静态**属性** | `Coroutine.current()` 静态**方法** |
| `cor.All(c1, c2, ...)` / `cor.Any(...)` **可变参数** | `waitAll2/3` / `waitAny2/3` **固定 2/3 参重载**（+ 较新的 `waitAll(Array<Task>)` / `waitAny(Task[])`） |
| `await [t1, t2, t3]` **数组语法糖** | ❌ 不存在，用 `waitAll2/3` 等价替代 |
| `cor.NextCompleted(tasks)` 返回 `(cor, value)` 元组 | `nextCompleted2/3` 返回 `Task`，结果另用 `await` 取 |
| `c.Error` 实例属性 | ❌ 不存在 |
| `spawn function(){...}` 函数字面量 | ✅ 已支持（闭包提升 + `spawnClosure0`） |
| `Coroutine<T>` / `cor<T>` 泛型句柄 | ❌ 统一为非泛型 `Task` |
| **循环回边自动插入 `OpCode_SchedCheck`** | ❌ **前端不发射 `SCHED_CHECK`**，需显式 `Coroutine.yieldNow()` |
| 错误统一用 `Error` 枚举 int32 码承载 | SL 层用 `enum extends Error` 异常；C VM 侧抛出的取消/非法操作异常**值为 `null`**（`error_code = -63` / `-64`） |
| `Error.Cancelled` / `Error.StackOverflow` 等常量 | ❌ 无对应常量；用裸 `catch{}` 捕获 |
| 设计档未列出的**新增能力** | ✅ `spawnClosure*` / `spawnInstance*` / `spawnByName` / `waitUntil` |

---

## 14. 限制与约定（必读）

| # | 限制 | 说明 |
|:-:|---|---|
| 1 | **被 spawn 的方法必须全工程唯一** | 按"简单名 + 参数个数"全局解析（`vm_find_method_entry_by_name`），**不区分类名**。测试工程编译几十个 `.sl`，务必加前缀 |
| 2 | **参数按 `object` 装箱传递** | `int` / `string` 等值可直接传入并自动装箱，round-trip 无损；**最多 3 个参数**（`spawn0..3`） |
| 3 | **无自动公平性** | 前端不发射 `SCHED_CHECK`；纯计算循环必须显式 `Coroutine.yieldNow()`，否则独占调度器、饿死其它协程 |
| 4 | **`native` 函数体内禁止挂起** | 挂起只发生在解释循环的指令边界（安全点） |
| 5 | **子 VM（静态初始化器）内禁止用协程 API** | 编译期拦截 `spawn` / `await` / `yield` 三关键字 |
| 6 | **`yield` 不支持带参** | 条件等待用 `Coroutine.waitUntil( 谓词闭包 )` |
| 7 | **取消/非法操作异常值为 `null`** | 必须**裸 `catch{}`**，不能用 `catch X ex` 绑定 |
| 8 | **`sleep` 在非协程上下文退化为真阻塞** | 会卡住整个线程 |
| 9 | **`waitUntil` 谓词内禁止挂起** | 必须是无副作用的纯查询 |
| 10 | **`await` 自己是运行期错误** | C 侧抛非法操作（`-64`） |
| 11 | **求值栈禁止搬迁** | 栈槽可存原生指针，实现红线 |

### 编程建议

- 长计算循环内周期性 `Coroutine.yieldNow()`，避免饿死其它协程。
- fire-and-forget 协程也建议最终 `await`（或 `waitTimeout`）一次，确保异常被观测、资源被回收。
- 用 `Channel` + `close` 表达"生产结束"，用 `waitTimeout` 表达"限时等待"，**避免手写轮询**。
- 被 spawn 的方法统一加前缀（如 `coro`），规避全工程唯一约束。

---

## 15. 实战用例集

### 15.1 fire-and-forget（不 await 也要跑完）

```sl
static coroSetFlag() { CoroutineTest.g_done = true }

CoroutineTest.g_done = false
Coroutine.spawn0( "coroSetFlag" )
for Int32 i = 0, i < 1000, i = i + 1
{
    if CoroutineTest.g_done { break }
    Coroutine.sleep( 1 )                 # 让出，给后台协程执行机会
}
check( CoroutineTest.g_done )
```

### 15.2 串行 vs 并行

```sl
static int coroTrack()
{
    g_order = g_order + "s"
    Coroutine.sleep( 50 )
    g_order = g_order + "e"
    ret 1
}

# 串行：第二个等第一个完成才启动 -> "sese"
g_order = ""
Coroutine.awaitHandle( Coroutine.spawn0( "coroTrack" ) )
Coroutine.awaitHandle( Coroutine.spawn0( "coroTrack" ) )

# 并行：同时启动后串行消费 -> "ssee"
g_order = ""
Task a = Coroutine.spawn0( "coroTrack" )
Task b = Coroutine.spawn0( "coroTrack" )
Coroutine.awaitHandle( a )
Coroutine.awaitHandle( b )
```

### 15.3 定时器唤醒顺序

```sl
static coroTimerMark( int ms, string mark )
{
    Coroutine.sleep( ms )
    g_order = g_order + mark
}

g_order = ""
Task a = Coroutine.spawn2( "coroTimerMark", 30, "30" )
Task b = Coroutine.spawn2( "coroTimerMark", 10, "10" )
Task c = Coroutine.spawn2( "coroTimerMark", 20, "20" )
Coroutine.waitAll3( a, b, c )
check( g_order == "102030" )             # 按到期时间唤醒
```

### 15.4 深递归（帧链化，突破旧 64 层限制）

```sl
static int coroDeep( int n )
{
    if ( n <= 0 ) { ret 0 }
    Task h = Coroutine.spawn1( "coroDeep", n - 1 )
    ret ( Coroutine.awaitHandle( h ) as int ) + 1
}

Task h = Coroutine.spawn1( "coroDeep", 200 )
check( Coroutine.awaitHandle( h ) as int == 200 )
```

### 15.5 1000 个协程批量并发

```sl
int sum = 0
List<Task> tasks = List<Task>()
for Int32 i = 0, i < 1000, i = i + 1
{
    tasks.add( Coroutine.spawn2( "coroAdd2", i, 1 ) )
}
for v in tasks
{
    sum = sum + ( Coroutine.awaitHandle( v ) as int )
}
check( sum == 500500 )
```

### 15.6 协程间共享静态字段（无撕裂）

协作式单线程下不存在数据竞争，共享静态字段**无需加锁**：

```sl
static coroInc100()
{
    for Int32 j = 0, j < 100, j = j + 1
    {
        CoroutineTest.g_counter = CoroutineTest.g_counter + 1
    }
}

g_counter = 0
List<Task> incs = List<Task>()
for Int32 i = 0, i < 10, i = i + 1 { incs.add( Coroutine.spawn0( "coroInc100" ) ) }
for v in incs { Coroutine.awaitHandle( v ) }
check( g_counter == 1000 )
```

### 15.7 Pipeline：生产 → 处理 → 聚合

```sl
static coroJ1Produce( Channel<object> raw )
{
    for Int32 i = 0, i < 100, i = i + 1 { raw.send( i ) }
    raw.close()
}

static coroJ1Process( Channel<object> raw, Channel<object> proc )
{
    while ( true )
    {
        object v = raw.recv()
        if ( v == null ) { break }
        proc.send( ( v as int ) * 2 )
    }
    proc.close()
}

static coroJ1Aggregate( Channel<object> proc )
{
    while ( true )
    {
        object v = proc.recv()
        if ( v == null ) { break }
        g_j1sum = g_j1sum + ( v as int )
    }
}

Channel<object> raw  = Channel<object>.create( 4 )
Channel<object> proc = Channel<object>.create( 4 )
Task jp  = Coroutine.spawn1( "coroJ1Produce", raw )
Task jpr = Coroutine.spawn2( "coroJ1Process", raw, proc )
Task ja  = Coroutine.spawn1( "coroJ1Aggregate", proc )
Coroutine.waitAll3( jp, jpr, ja )
check( g_j1sum == 9900 )                 # sum(0..99) * 2
```

### 15.8 扇出-扇入（50 个并行工作单元）

```sl
static int coroJ2Work( int i )
{
    Coroutine.sleep( 2 )
    ret i + 1
}

List<Task> works = List<Task>()
for Int32 i = 0, i < 50, i = i + 1
{
    works.add( Coroutine.spawn1( "coroJ2Work", i ) )
}
int sum = 0
for v in works { sum = sum + ( Coroutine.awaitHandle( v ) as int ) }
check( sum == 1275 )                     # sum(1..50)
```

### 15.9 超时降级

```sl
Task rpc = Coroutine.spawn0( "coroSlowRpc" )

if ( Coroutine.waitTimeout( rpc, 200 ) )
{
    object result = Coroutine.awaitHandle( rpc )     # 正常取回
    global.println( "RPC 成功" )
}
else
{
    global.println( "RPC 超时，走降级；目标仍在后台运行" )
    rpc.cancel()                                     # 如需中止则显式取消
}
```

### 15.10 竞速：谁先完成用谁

```sl
Task fast = Coroutine.spawn0( "coroFastPath" )
Task slow = Coroutine.spawn0( "coroSlowPath" )

Task winner = Coroutine.waitAny2( fast, slow )
object res  = winner.awaitHandle()

# 收尾：让失败者也结束，避免悬挂
label cleanup { try Coroutine.awaitHandle( slow ) }
catch { }
```

### 15.11 渐进式结果消费（`nextCompleted` 轮询）

```sl
Task jobA = Coroutine.spawn0( "coroJobA" )
Task jobB = Coroutine.spawn0( "coroJobB" )

while ( true )
{
    Task done = Coroutine.nextCompleted2( jobA, jobB )
    if ( done == null )
    {
        Coroutine.sleep( 10 )            # 还没完成的，稍后再查
        continue
    }
    global.println( "完成一个: " + ( done.awaitHandle() as string ) )
    break
}
```

### 15.12 游戏：等待血量跌破阈值（Unity `WaitUntil` 风格）

```sl
# 方式 1：谓词闭包
function hpLow = function() { ret player.hp < 20 }
Coroutine.waitUntil( hpLow )
CastSkill()

# 方式 2：CustomYieldInstruction 对象（见 §6.3）
Coroutine.waitFor( WaitHpLess(player) )
CastSkill()

# 方式 3：事件驱动（零轮询，推荐高频场景）
player.hpLowSignal.recv()                # 在 set hp 里 send 通知
CastSkill()
```

事件驱动的 setter 写法：

```sl
class Player extends Object
{
    Int32 _hp = 100
    Channel<Int32> hpLowSignal

    get Int32 hp() { ret this._hp }
    set Int32 hp( Int32 v )
    {
        this._hp = v
        if ( v < 20 ) { this.hpLowSignal.send( v ) }
    }
}
```

### 15.13 游戏：技能序列与打断

```sl
static coroSkillSequence( object ctx )
{
    label skill
    {
        PlayCastAnim()
        Coroutine.sleep( 300 )           # 前摇
        ApplyDamage()
        Coroutine.sleep( 500 )           # 后摇
        EndSkill()
    }
    finally
    {
        CleanupCastFx()                  # 被打断也会执行
    }
}

Task skill = Coroutine.spawnClosure1( coroSkillSequence, target )
if ( player.isStunned )
{
    skill.cancel()                       # 协作式取消 -> finally 保证清理
}
```

### 15.14 工作队列（多 worker + 优雅关闭）

```sl
static coroWorker( Channel<object> jobs, int id )
{
    while ( true )
    {
        object job = jobs.recv()
        if ( job == null ) { break }     # close 后 recv 得 null -> 退出
        ProcessJob( job as string )
    }
}

Channel<object> jobs = Channel<object>.create( 16 )
List<Task> workers = List<Task>()
for Int32 i = 0, i < 4, i = i + 1
{
    workers.add( Coroutine.spawn2( "coroWorker", jobs, i ) )
}

for j in jobList { jobs.send( j ) }
jobs.close()                             # 广播关闭，4 个 worker 依次退出
for w in workers { Coroutine.awaitHandle( w ) }
```

### 15.15 带取消的批量任务（任一失败全停）

```sl
label batch
{
    Coroutine.waitAll3( t0, t1, t2 )     # 任一异常 -> 自动取消其余并抛出
}
catch WorkError ex
{
    global.println( "批次失败，其余已被自动取消" )
}
catch                                     # 裸 catch：接 C 侧取消异常
{
    global.println( "批次被取消" )
}

# 收尾：确认被取消者也真正结束
label cleanup
{
    try { Coroutine.awaitHandle( t0 ) } catch { }
    try { Coroutine.awaitHandle( t1 ) } catch { }
    try { Coroutine.awaitHandle( t2 ) } catch { }
}
```

### 15.16 `finally` 保证清理（正常 / 异常 / 取消三态）

```sl
static coroWithResource()
{
    Resource r = Resource.acquire()
    label work
    {
        Coroutine.sleep( 100 )           # 可能被取消
        r.use()
    }
    finally
    {
        r.release()                      # 三种结束方式都会执行
    }
}
```

### 15.17 协程内组合（函数变量 + spawn + await + yield 帧保持）

```sl
static int coroKwPipeline()
{
    function f = function( int a, int b ) { ret a + b; }
    Task h = spawn f( 10, 20 )
    int r = await h as int
    yield                                # 挂起后 r 与帧状态保持
    ret r
}

Task hp = Coroutine.spawn0( "coroKwPipeline" )
check( await hp as int == 30 )
```

---

## 16. 实现要点与排障

### 16.1 调度器要点

| 行为 | 说明 |
|---|---|
| 主入口即 root 协程 | `vm_scheduler_enter` 把 `static fun()` 包装为 root 协程，故主入口可直接 `await`/`yield`/`sleep`/`current` |
| 就绪队列 + 定时器链表 | **就绪队列先于定时器处理**；被目标死亡唤醒的等待者先于其残余定时器运行 |
| 定时器链表升序 | 按 `wake_at_ms` 升序插入，`sleep` 到期即唤醒 |
| 重复唤醒安全 | 多次 `enqueue_ready` 同一协程有守卫，不会重复入队 |
| 全部结束则收敛 | `vm_scheduler_enter` 的 drain 循环退出 |
| `sleep(0)` 必须 requeue | `ms <= 0` 时若只挂起不入队，协程会既不在就绪队列也不在定时器上，**永久丢失（E2 死锁）** |

### 16.2 挂起系统调用的通用协议（Option A）

会挂起的系统调用遵循统一约定（见 `coroutine_system_method.h`）：

1. **peek 而不 pop** 参数（挂起期间参数留在栈上）
2. 以 `reexecute = TRUE` 挂起 → 恢复后**同一条指令重跑一遍**
3. 重跑时重新检查等待条件，条件满足才 pop 参数并推进

> `sleep` 是唯一例外：它先 pop 参数（恢复后继续下一条指令）。

### 16.3 常见症状对照

| 症状 | 原因 | 处理 |
|---|---|---|
| 后台协程"没执行" | 主协程没让出 | 主协程加 `sleep` / `yieldNow` / `await` |
| 长循环卡住其它协程 | 无 `SCHED_CHECK` | 循环内显式 `Coroutine.yieldNow()` |
| `spawn` 报方法找不到 | 方法名不唯一 / 参数个数不符 | 检查全工程同名方法，加前缀；错误码 `-61` |
| `spawn` 创建失败 | 资源不足等 | 错误码 `-62` |
| 取消后 `catch X ex` 拿到 `null` | C 侧异常值为 `null`（`-63`） | 改用裸 `catch{}` |
| `await` 自己报错 | C 侧非法操作（`-64`） | 裸 `catch{}`；检查是否误用 `Coroutine.current()` |
| `yield return X` 编译失败 | `yield` 不支持带参 | 改用 `Coroutine.waitUntil( 谓词闭包 )` |
| 协程永久挂住 | 条件永不成立 / Channel 未 `close` | 用 `waitTimeout` 兜底；生产者务必 `close()` |
| CPU 100% | 忙等循环 | 循环内 `sleep(1)` 或 `yieldNow()`；改用 Channel 事件驱动 |
| 静态初始化器里协程 API 报错 | 子 VM 不支持挂起 | 把逻辑移到普通静态方法 |

### 16.4 错误码（C VM 侧）

| 码 | 含义 |
|---|---|
| `-61` | spawn：按名字找不到方法 |
| `-62` | spawn：协程创建失败 |
| `-63` | cancelled：取消异常（**值为 `null`**） |
| `-64` | `Error.InvalidOperation`：非法句柄 / 无协程上下文 / 向已关闭通道 send（**值为 `null`**） |

---

## 附：与本文档相关的文件

| 文件 | 关系 |
|---|---|
| `md/design/COROUTINE_DESIGN.md` | 设计规格（部分未实现，见 §13） |
| `md/ai/故障排查流程.md` | 项目级排查主文档 |
| `md/ai/DEBUG_WORKFLOW.md` | `Logs/` 与 `DebugCode/` 产物阅读顺序 |
| `test/BaseTest/CoroutineTest.sl` | A–N + W 组全量验收用例（本文档示例的可运行版本） |
| `csimple_lang/src/vm/runtime/coroutine/vm_coroutine.c` | 协程核心：创建 / 调度器 / 帧链 / 唤醒 / 取消 |
| `csimple_lang/src/vm/system_method_call/coroutine_system_method.c` | `SystemCoroutine*` 与 `SystemChannel*` 实现 |

