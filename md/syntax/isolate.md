# 隔离岛（Isolate）与端口（Port）

> **本文档以「当前实现」为准**，对应 `source/Front/Lib/Std/Isolate/*.sl`
> 与 `test/ExpendTest/IsolateTest.sl`（A–I 共 9 组验收用例）。
> 设计规格见 `md/design/ISOLATE_DESIGN.md`——**该设计档部分内容尚未实现**，差异清单见 §14。
> 相关文档：`md/syntax/coroutine.md`（协程）、`md/design/COROUTINE_DESIGN.md`。

---

## 目录

1. [模型概述](#1-模型概述)
2. [快速开始](#2-快速开始)
3. [类型体系](#3-类型体系)
4. [可发送消息（Sendable）白名单](#4-可发送消息sendable白名单)
5. [创建与运行（spawn / run / exit）](#5-创建与运行spawn--run--exit)
6. [端口通信（SendPort / ReceivePort）](#6-端口通信sendport--receiveport)
7. [生命周期控制（pause / resume / kill / ping / 监听）](#7-生命周期控制pause--resume--kill--ping--监听)
8. [TransferableData 零拷贝转移](#8-transferabledata-零拷贝转移)
9. [IsolateGroup（隔离组）](#9-isolategroup隔离组)
10. [状态与错误常量](#10-状态与错误常量)
11. [数据隔离语义（深拷贝 / 静态字段 / global）](#11-数据隔离语义深拷贝--静态字段--global)
12. [与协程的组合](#12-与协程的组合)
13. [API 速查总表](#13-api-速查总表)
14. [限制与实现偏差（必读）](#14-限制与实现偏差必读)
15. [实战用例集](#15-实战用例集)

---

## 1. 模型概述

每个 isolate 拥有**独立的 VM 实例**：独立堆、独立调度器、独立静态字段、独立 GC。
isolate 相互之间**只能通过消息传递（深拷贝）或 TransferableData（所有权转移）通信**；
同组 isolate 共享代码与类型（跨 isolate 类型身份一致）。

| 特性 | 说明 |
|---|---|
| **内存隔离** | 一个 isolate 的 GC、崩溃、无限循环不影响另一个；不共享任何可变内存 |
| **消息即克隆** | 跨 isolate 传值一律深拷贝；接收方拿到的是自己堆里的副本 |
| **M:1 单线程调度（当前 P1）** | 所有 isolate 挂在同一个协作式调度循环上，**主 isolate 协程阻塞（recv/sleep/waitAll）时调度器才推进 worker**——语义与真并行一致，但无实际并行 |
| **与协程正交** | 协程是单 isolate 内的并发（共享堆）；isolate 是跨堆的并行（不共享）。一个 isolate 内可跑任意多协程 |
| **句柄稳定** | 同一 isolate id 恒对应同一 wrapper 实例，`==` 判等可靠 |
| **组共享代码** | 同组 isolate 共享类表 / 方法表 / 字节码 → spawn 近乎免费、闭包可按 `method_id` 跨 isolate 解析、类型身份一致 |

### 1.1 与协程（Coroutine）/ 通道（Channel）的对比

| 维度 | 协程（Coroutine） | 通道（Channel\<T\>） | 隔离岛（Isolate） |
|---|---|---|---|
| 堆 | 共享同一 VM | 共享同一 VM | **独立**（各自 obj_pool / LOS） |
| 静态字段 | 共享 | 共享 | **独立**（静态影子表） |
| GC | 同一个 GC | 同一个 GC | **各自独立 GC** |
| 通信 | — | `Channel<T>`（传引用、有背压、可阻塞） | `SendPort`（**深拷贝、永不阻塞**） |
| 典型用途 | 高并发 IO、pipeline | 同 VM 内协程间数据流 | CPU 密集计算、故障隔离、状态隔离 |

> `Channel<T>` 与 `SendPort/ReceivePort` 是**并列关系，互不为子类**：语义冲突（共享引用 vs 深拷贝、阻塞 vs 异步）导致无法用继承统一，详见 `ISOLATE_DESIGN.md` §7。

### 1.2 生命周期（状态机）

```
        spawn
Created ──► Ready ──► Running ──┬──► Paused ──resume──► Running
                               │
                               ├──► Exiting ──► Dead
                               │
                               └── kill(0/1) / 入口返回 / Isolate.exit / 未捕获异常(fatal)
```

### 1.3 源码位置

| 层 | 文件 |
|---|---|
| SL API | `source/Front/Lib/Std/Isolate/`（`Isolate.sl`、`SendPort.sl`、`ReceivePort.sl`、`RawReceivePort.sl`、`Capability.sl`、`TransferableData.sl`、`IsolateGroup.sl`、`IsolateStatus.sl`、`IsolateError.sl`） |
| 系统调用注册 | `source/Front/Lib/Std/Std.jsonc` → `files[]` + `systemCalls[]` |
| C VM isolate 核心 | `csimple_lang/src/vm/runtime/isolate/`（`vm_isolate`、`vm_message`、`vm_static_shadow`、`vm_transfer`） |
| C VM 系统调用 | `csimple_lang/src/vm/system_method_call/isolate_system_method.c/.h` |
| 验收用例 | `test/ExpendTest/IsolateTest.sl` |

---

## 2. 快速开始

```sl
import Std;
import Core;

IsolateDemo
{
    static fun()
    {
        # 入口是函数值：匿名闭包字面量不能直接作实参，
        # 一律先赋给 function 变量再传（见 §14 语言限制 1）
        function add2 = function( int a, int b ) { ret a + b }

        # run2：spawn -> 执行 -> 取回返回值 -> 销毁（一次性计算）
        Int32 r = Isolate.run2( add2, 3, 4 ) as int       # 7

        Console.println( "result = " + r.toString() )
    }
}
```

> `run*` 会**挂起当前协程**直至 worker 结束并取回返回值；`void` 入口返回 `null`。
> 传参按 `object` 装箱，返回值需 `as` 转回。

---

## 3. 类型体系

```
Isolate               isolate 句柄（管理器）：spawn / run / 生命周期控制
IsolateGroup          组句柄：current / exit / isolateCount
SendPort              端口发送端（可跨 isolate 发送）
ReceivePort           端口接收端：recv / tryRecv / listen / close
RawReceivePort        底层接收端（与 ReceivePort 同 API，无缓冲语义糖）
Capability            能力令牌（pause/resume 授权，可发送）
TransferableData       零拷贝转移块（可发送，转移后源失效）
IsolateStatus         状态常量类
IsolateError          错误码常量类
```

全部为**库类型 + 系统方法**，不引入任何新关键字（与 `Coroutine`/`Channel` 惯例一致）。

### 3.1 `Isolate`（句柄）

由 `spawn*` 创建或由 port + capability 重建。**同一 isolate id 恒对应同一 wrapper 实例，`==` 判等可靠**。`Isolate` 本身**不可跨 isolate 发送**——请拆成 `controlPort` + `pauseCapability` + `terminateCapability` 传递。

| 成员 | 签名 | 说明 |
|---|---|---|
| `Isolate.current()` | `static Isolate` | 当前 isolate 句柄 |
| `Isolate.spawn0..3(entry[, a0..a2])` | `static Isolate` | 以函数值为入口创建并启动（同组），返回句柄 |
| `Isolate.run0..3(entry[, a0..a2])` | `static object` | 一次性计算：spawn → 执行 → 取回返回值 → 销毁，**挂起当前协程**直至结束 |
| `Isolate.exit(port, msg)` | `static void` | **同步终止当前 isolate**，并向 `port` 发送一条终止消息 |
| `iso.pause()` | `Capability` | 请求暂停本 isolate，返回 resumeCapability；capId 为 0 表示不可暂停（已退出等） |
| `iso.resume(cap)` | `void` | 恢复被暂停的 isolate；capability 不匹配**静默无效** |
| `iso.kill(priority)` | `void` | 请求终止：`0 = immediate`（立即）/ `1 = beforeNextEvent`（下一事件前） |
| `iso.ping(port, response, priority)` | `void` | 存活探测：isolate 存活时向 `port` 发送 `response` |
| `iso.setErrorsFatal(fatal)` | `void` | 未捕获异常是否终止 isolate（`fatal=true` 终止） |
| `iso.addOnExitListener(port, response)` | `void` | 注册退出监听：isolate 退出时向 `port` 发送 `response` |
| `iso.addErrorListener(port)` | `void` | 注册错误监听：isolate 未捕获异常时向 `port` 发送错误描述 |
| `iso.status` | `Int32` | 当前状态（`IsolateStatus` 常量）；**句柄无效返回 -1** |
| `iso.debugName` | `string` | 调试名（`"Isolate-" + id`） |
| `iso.handle` | `Int64` | 原始 isolate id，诊断用 |
| `iso.controlPort` | `SendPort` | 控制端口（**可发送**） |
| `iso.pauseCapability` | `Capability` | 恢复能力（**可发送**） |
| `iso.terminateCapability` | `Capability` | 终止能力（**可发送**） |
| `Isolate(ctrlPort, pauseCap, termCap)` | 构造 | 由 port + capability **重建句柄**（C VM 按 control port 反查 owner）；未知 port 得全 0 句柄，`status` 返回 -1 |

### 3.2 `ReceivePort` / `RawReceivePort`（接收端）

端口**属本 isolate 所有，本身不可跨 isolate 发送**；跨 isolate 传递请使用 `sendPort`。

| 成员 | 签名 | 说明 |
|---|---|---|
| `ReceivePort()` | 构造 | 创建接收端并自动分配对应端口 |
| `rp.sendPort` | `SendPort` | 对应的发送端；同一端口重复读取返回**同一实例**，`==` 可靠 |
| `rp.listen(handler)` | `void` | 注册消息处理器：内部起一个分发协程阻塞收消息并回调（收一条调一次）；端口关闭且消息耗尽后分发协程自动退出 |
| `rp.recv()` | `object` | **阻塞当前协程**直到收到一条消息；端口关闭且无消息后返回 `null` |
| `rp.tryRecv()` | `object` | 非阻塞取一条；无消息返回 `null` |
| `rp.close()` | `void` | 关闭端口；关闭后 `send` 不再可达本端口 |
| `rp.count` | `Int32` | 当前队列中的消息数（诊断用） |
| `rp.isClosed` | `bool` | 端口是否已关闭 |

`RawReceivePort`（对标 Dart 的 `RawReceivePort`）与 `ReceivePort` **API 完全相同**，共用同一套端口机制；区别仅在定位——不提供缓冲语义糖，适合需要完全控制取消息时机的底层代码。

### 3.3 `SendPort`（发送端）

可跨 isolate 发送（序列化只携带 `port_id`，接收端重建同一 wrapper）。**同一 `port_id` 在同一 VM 内恒对应同一 wrapper 实例**，因此收到同一端口的两次消息后用 `==` 比较为 `true`。

| 成员 | 签名 | 说明 |
|---|---|---|
| `sp.send(message)` | `void` | **异步发送**（深拷贝语义，**永不阻塞当前协程**）；消息须可发送（见 §4） |
| `sp.portId` | `Int64` | 端口 id，诊断用 |

### 3.4 `Capability`（能力令牌）

不可伪造语义的 `Int64` id 包装，用于 `pause` / `resume` / `kill` 等敏感操作的授权校验（C VM 侧比对 id，不匹配**静默无效**）。可跨 isolate 发送（按值深拷贝，用于把控制权转交给其它 isolate）。

| 成员 | 签名 | 说明 |
|---|---|---|
| `Capability(capId)` | 构造 | 一般用于重建（从 `pause()` 返回值或消息中获得） |
| `cap.capId` | `Int64` | 能力 id，诊断用 |

### 3.5 `TransferableData`（零拷贝转移块）

一段字节数据的**独占所有权句柄**。跨 isolate 发送时不深拷贝内容，只转移所有权（发送后本句柄失效）；接收方 `materialize` 取回字节内容（一次性，取出后句柄同样失效）。

| 成员 | 签名 | 说明 |
|---|---|---|
| `TransferableData.fromBytes(bytes)` | `static TransferableData` | 从 `Array<UInt8>` 创建转移块（拷贝一次进 C 侧 blob） |
| `td.materialize()` | `Array<UInt8>` | 取回字节内容（一次性）；**句柄无效（已转移/已取出）时返回 `null`** |
| `td.size` | `Int32` | 转移块字节数（句柄失效后为 0） |
| `td.isValid` | `bool` | 句柄是否仍然有效（未转移且未取出） |

### 3.6 `IsolateGroup`（组句柄）

同组 isolate 共享代码与类型（闭包可按 `method_id` 解析），**组是闭包跨 isolate 传递的前提**。

| 成员 | 签名 | 说明 |
|---|---|---|
| `IsolateGroup.current()` | `static IsolateGroup` | 当前 isolate 所属组 |
| `grp.exit()` | `void` | 请求组内全部 isolate 退出（**含当前 isolate**） |
| `grp.isolateCount` | `Int32` | 组内存活 isolate 数（诊断用） |
| `grp.id` | `Int64` | 组 id，诊断用 |

---

## 4. 可发送消息（Sendable）白名单

`SendPort.send` 的内容受严格限制。这条约束是**整个隔离模型的基石**：只要消息图里没有共享引用，就不需要任何锁。

### 4.1 允许（递归适用）

| 类型 | 说明 |
|---|---|
| `null` | 直接编码 |
| `bool` / 所有整数 / 所有浮点（含 `Float8` / `Float16`） | 直接编码 |
| `string` | UTF-8 编码 |
| `List` / `Map` / `Set` | **元素递归可发送** |
| `SendPort` | 编码为 `port_id`，目标端重建同一 wrapper（**保持 `==`**） |
| `Capability` | 编码为 `cap_id`（**保持 `==`**） |
| `TransferableData` | 零拷贝所有权转移（见 §8） |
| **闭包（函数值）** | **捕获环境全部可发送时**可发送：编码为 `method_id` + 深拷贝的 context。**组内有效**（共享代码）——这也是 `spawn*` / `run*` 能直接以闭包为入口的原理 |

### 4.2 禁止（发送方抛出 `IsolateError.NotSendable`）

| 类型 | 原因 |
|---|---|
| 任意普通 `class` 实例 | 含方法表 / 可变状态，跨堆即破坏隔离 |
| `ReceivePort` / `RawReceivePort` | 端口属本 isolate 所有；只能发 `sendPort` |
| `Isolate` 句柄 | 生命周期管理会失控；请发 `controlPort` + capabilities |
| `Channel<T>` | 绑定单 VM 的协程等待队列 |
| 协程句柄（`Task`） | 绑定单 VM 调度器 |
| 捕获了上述任一值的闭包 | context 整体拒绝 |
| 含上述任一元素的容器 | 递归失败 |

### 4.3 示例

```sl
# B2 标量回显：int / string / float / null 都可发送
function echo = function( object v ) { ret v }
Int32  i = Isolate.run1( echo, 42 ) as int        # 42
string s = Isolate.run1( echo, "hi" ) as string   # "hi"
double f = Isolate.run1( echo, 3.14 ) as double   # 3.14
object n = Isolate.run1( echo, null )             # null

# B5 普通类实例不可发送 → 在发送方抛出
PlainBox { int v }
ReceivePort rp = ReceivePort()
bool threw = false
label guard
{
    try rp.sendPort.send( PlainBox() )
}
catch
{
    threw = true      # true
}

# G4 ReceivePort 本身不可发送
label guard2
{
    try rp.sendPort.send( rp )
}
catch
{
    # 同样在发送方抛出
}
```

---

## 5. 创建与运行（spawn / run / exit）

### 5.1 入口：函数值的三种等价形态

`spawn0..3` / `run0..3` 的第一个参数是**函数值**（不是方法名字符串）。三种形态等价：

```sl
# 形态一：宽松 function 变量（不做签名检查）
function add = function( int a, int b ) { ret a + b }
Int32 r1 = Isolate.run2( add, 3, 4 ) as int        # 7

# 形态二：Func<签名> 类型（第 1 个模板实参是返回类型，其后为参数类型）
Func<int, int, int> typed = function( int a, int b ) { ret a - b }
Int32 r2 = Isolate.run2( typed, 10, 3 ) as int     # 7

# 形态三：匿名闭包（当前语言限制：须先赋给变量再传参，不能内联写实参）
function fn = function() { ret 42 }
object r3 = Isolate.run0( fn )                     # 42
```

**数字后缀 = 参数个数**（`spawn0..3` / `run0..3`），上限 3 个；更多参数请打包成一个可发送容器（如 `List<object>`）传入。

**闭包可发送 ⟺ 其捕获环境中的每一个值都可发送**：

| 捕获内容 | 行为 |
|---|---|
| 无捕获（context 为空） | 始终可发送 |
| 标量 / string / 可发送容器 | 可发送，按深拷贝传递 |
| 普通类实例 / `Channel` / 协程句柄 / `ReceivePort` | 报 `IsolateError.NotSendable` |

> **关键坑（宿主方法粒度共享）**：闭包捕获上下文按「宿主方法」粒度共享——同一方法内任一闭包捕获了不可发送值（如 `Channel`），**整个共享上下文即不可发送**，同方法后续闭包全部发不出去。捕获不可发送值的用例必须放在独立宿主方法里（见 §14 限制 2）。

### 5.2 run：一次性计算

spawn → 执行 → 取回返回值 → 销毁。会**挂起当前协程**直至 worker 结束。

```sl
# A4 void 入口：run 返回 null
function noop = function() { Int32 x = 0 }
object r = Isolate.run0( noop )     # null
```

### 5.3 spawn：长生命周期 worker

```sl
# A2 端口双向 echo：worker 建自己的 ReceivePort 回传 sendPort
ReceivePort rp = ReceivePort()
function echoWorker = function( object arg )
{
    SendPort sp = arg as SendPort          # 主 isolate 的发送端（捕获/传参均可）
    ReceivePort wrp = ReceivePort()        # worker 自己的接收端
    sp.send( wrp.sendPort )                # 回传 worker 侧端口（SendPort 可发送）
    object msg = wrp.recv()                # 阻塞当前协程等消息
    sp.send( msg )                         # 原样回显
}
Isolate.spawn1( echoWorker, rp.sendPort )

SendPort wport = rp.recv() as SendPort     # 第一条消息：worker 回传的 SendPort
wport.send( "ping" )
string echo = rp.recv() as string          # "ping"
```

### 5.4 非函数入口 → SpawnFailed

```sl
static object g_badEntry = null

g_badEntry = 42                            # 非函数值
bool threw = false
label guard
{
    try Isolate.spawn0( g_badEntry )
}
catch
{
    threw = true                           # true：入口非法（非函数值 / method_id 解析失败）
}
g_badEntry = null
```

### 5.5 Isolate.exit：同步终止并携带最终消息

```sl
# E3 定向退出消息
ReceivePort rp = ReceivePort()
SendPort sp = rp.sendPort                  # 捕获可发送的 SendPort（不是 ReceivePort）
function exitWithMsg = function() { Isolate.exit( sp, 12345 ) }
Isolate.spawn0( exitWithMsg )

Int32 v = rp.recv() as int                 # 12345
```

> `Isolate.exit` **不再执行后续代码**；入口正常 return / `exit` / `kill` / 未捕获异常（fatal）都会使 isolate 进入 Dead。

### 5.6 重建句柄（跨 isolate 传递控制权）

```sl
Isolate iso = Isolate.spawn0( someEntry )

# 三者均可发送；发往其它 isolate 后，对端可重建等价句柄
SendPort ctrl = iso.controlPort
Capability pc = iso.pauseCapability
Capability tc = iso.terminateCapability

# 接收方：
Isolate restored = Isolate( ctrl, pc, tc )     # 同一 id，== 判等成立
# 未知 port（isolate 不存在 / 已销毁）时得到全 0 句柄，status 查询返回 -1
```

---

## 6. 端口通信（SendPort / ReceivePort）

### 6.1 基本收发

```sl
ReceivePort rp = ReceivePort()

rp.sendPort.send( 42 )          # 异步、永不阻塞
rp.sendPort.send( "hello" )

Int32 a = rp.recv() as int      # 阻塞当前协程：42
string b = rp.recv() as string  # "hello"

object c = rp.tryRecv()         # 非阻塞：null（队列已空）
```

- **同一对 (sender, port) 的消息保证 FIFO**；不同 sender 之间不保证全局顺序。
- `recv()` 阻塞的是**当前协程**（不是线程），其它协程照常调度。
- 一个 `ReceivePort` 可以有多个 `SendPort`；一个 `SendPort` 只对应一个 `ReceivePort`。

### 6.2 关闭语义

```sl
# B6 向已关闭端口发送 → 报错
ReceivePort rp6 = ReceivePort()
SendPort sp6 = rp6.sendPort
rp6.close()
label guard
{
    try sp6.send( 1 )
}
catch
{
    # 在发送方抛出
}

# B7 关闭后残留消息仍可取，取尽返回 null
ReceivePort rp7 = ReceivePort()
rp7.sendPort.send( 42 )
rp7.close()
Int32 x = rp7.recv() as int     # 42（残留消息）
object y = rp7.recv()           # null（取尽）
```

### 6.3 listen：Stream 风格消息处理器

```sl
ReceivePort rp = ReceivePort()

function handler = function( object msg )
{
    Console.println( "recv: " + ( msg as string ) )
}
rp.listen( handler )            # 内部起分发协程阻塞收消息并回调

rp.sendPort.send( "hello" )
Coroutine.sleep( 20 )           # M:1 调度：主协程让出后分发协程才跑
```

> `listen` 后端口关闭且消息耗尽时，分发协程自动退出。
> `RawReceivePort` 的 `listen` 用法相同——适合把「启动逻辑」与「消息处理逻辑」分离的底层代码。

### 6.4 SendPort 的 `==` 跨 isolate 稳定

```sl
# B4 worker 内对收到的 SendPort 做自反判等并回传
ReceivePort rp = ReceivePort()
function check = function( object arg )
{
    SendPort p = arg as SendPort
    p.send( p == p )            # 恒 true
}
Isolate.spawn1( check, rp.sendPort )
bool ok = rp.recv() as bool      # true
```

---

## 7. 生命周期控制（pause / resume / kill / ping / 监听）

### 7.1 pause / resume（Capability 授权）

```sl
ReceivePort rp = ReceivePort()
function entry = function( object arg )
{
    SendPort sp = arg as SendPort
    sp.send( "ready" )
    Coroutine.sleep( 5000 )      # 长睡眠 worker
}
Isolate iso = Isolate.spawn1( entry, rp.sendPort )
Coroutine.sleep( 50 )            # M:1 调度：等 worker 起来（制造让出点）

Capability cap = iso.pause()
# iso.status == IsolateStatus.Paused (3)
iso.resume( cap )               # 匹配的 capability 恢复

string msg = rp.recv() as string    # "ready"
iso.kill( 0 )
```

### 7.2 伪造 capability → 静默无效（对齐 Dart 能力安全模型）

```sl
Isolate iso = Isolate.spawn0( sleepEntry )
Coroutine.sleep( 30 )
Capability cap = iso.pause()            # 真正的 resumeCapability

Capability fake = Capability( 0 )       # 未授权的 capability
iso.resume( fake )                       # 不报错，也不生效
bool stillPaused = iso.status == 3       # true

iso.resume( cap )                       # 真 capability 恢复
iso.kill( 0 )
```

### 7.3 kill：两种优先级

```sl
# kill(0) = immediate：立即终止（当前实现注册表不摘除，status 仍可查 Dead(5)）
Isolate iso = Isolate.spawn0( sleepEntry )
Coroutine.sleep( 30 )
iso.kill( 0 )
Coroutine.sleep( 30 )
# iso.status == IsolateStatus.Dead (5)

# kill(1) = beforeNextEvent：下一个事件边界终止
```

### 7.4 ping：存活探测

```sl
ReceivePort rp = ReceivePort()
Isolate iso = Isolate.spawn0( sleepEntry )
Coroutine.sleep( 30 )

iso.ping( rp.sendPort, "pong", 0 )
string pong = rp.recv() as string    # "pong"
iso.kill( 0 )
```

### 7.5 退出 / 错误监听

```sl
ReceivePort exitRp = ReceivePort()
ReceivePort errRp = ReceivePort()

Isolate iso = Isolate.spawn0( sleepEntry )
iso.addOnExitListener( exitRp.sendPort, null )   # 退出时向该端口发 response（此处 null）
iso.addErrorListener( errRp.sendPort )          # 未捕获异常时发错误描述
iso.setErrorsFatal( true )                      # 未捕获异常终止 isolate

iso.kill( 0 )

# 轮询等待通知到达（M:1 调度：需让出点）
Int32 spins = 0
while ( exitRp.count < 1 && spins < 200 )
{
    Coroutine.sleep( 5 )
    spins = spins + 1
}
object done = exitRp.recv()          # null（当前实现 onExit 载荷为 null，见 §14）
```

---

## 8. TransferableData 零拷贝转移

大块字节数据跨 isolate 时避免两次深拷贝：创建时拷贝一次进 C 侧 blob，发送时**只转移所有权**，接收方 `materialize` 一次性取出。

```sl
# F1 1000 字节转移往返
Array<UInt8> bytes = Array<UInt8>( 1000 )
for Int32 i = 0, i < bytes.length, i = i + 1
{
    bytes._setItem_( i, 7 )
}

TransferableData td = TransferableData.fromBytes( bytes )
# td.isValid == true；td.size == 1000

function consume = function( object arg )
{
    TransferableData t = arg as TransferableData
    Array<UInt8> b = t.materialize()    # 在 worker 堆中物化（一次性）
    ret b.length
}
Int32 n = Isolate.run1( consume, td ) as int     # 1000

# F3 转移后源句柄失效
# td.isValid == false

# F2 失效句柄 materialize 返回 null（不抛异常）
Array<UInt8> gone = td.materialize()    # null
```

**所有权不变式**：任何时刻裸字节块只被一个 isolate 的句柄引用——发送后源端立即失效（`isValid == false`），杜绝共享。

---

## 9. IsolateGroup（隔离组）

```sl
# I2 当前组非空且至少含自己
IsolateGroup grp = IsolateGroup.current()
# grp != null && grp.isolateCount >= 1
# grp.id 为组 id（诊断用）

# I3 spawn 进入同组，kill 后组计数回落
function sleepEntry = function() { Coroutine.sleep( 5000 ) }
Isolate iso = Isolate.spawn0( sleepEntry )
Int32 before = IsolateGroup.current().isolateCount    # >= 2（含新 worker）
iso.kill( 0 )
Coroutine.sleep( 30 )
Int32 after = IsolateGroup.current().isolateCount      # before - 1

# 批量一次性 run 后全部回收，组计数回落到 1（仅主 isolate）
function noop = function() { Int32 x = 0 }
for Int32 i = 0, i < 20, i = i + 1
{
    Isolate.run0( noop )
}
Memory.collect()
# IsolateGroup.current().isolateCount == 1
```

**组的意义**：

1. **spawn 近乎免费**：组内共享类表 / 方法表 / 字节码，spawn 只需建一个 VM 结构 + 静态影子表。
2. **类型身份一致**（决定性理由）：`RuntimeType` 被组内共享，深拷贝过去的 `List<int>` 在 worker 里**仍是** `List<int>`——`as` / `is` 判断天然正确：

```sl
# I1 跨 isolate 类型身份
List<int> list = new()
list.add( 42 )
function first = function( object arg )
{
    List<int> v = arg as List<int>
    ret v._getItem_( 0 )
}
Int32 v = Isolate.run1( first, list ) as int    # 42
```

3. **闭包跨 isolate 的前提**：闭包编码为 `method_id` + context，目标 isolate 靠组内共享代码用同一个 `method_id` 解析到同一方法。

`grp.exit()` 请求**组内全部** isolate 退出（含当前 isolate）。

---

## 10. 状态与错误常量

### 10.1 `IsolateStatus`

```sl
public class IsolateStatus extends Object
{
    public static const Int32 Created  = 0
    public static const Int32 Ready    = 1
    public static const Int32 Running  = 2
    public static const Int32 Paused   = 3
    public static const Int32 Exiting  = 4
    public static const Int32 Dead     = 5
}
```

| 状态 | 值 | 含义 |
|---|:---:|---|
| `Created` | 0 | 结构已建，尚未入调度 |
| `Ready` | 1 | 在调度器待运行集合里 |
| `Running` | 2 | 正在执行 |
| `Paused` | 3 | 被 `pause()` 挂起 |
| `Exiting` | 4 | 收到退出请求，正在清理 |
| `Dead` | 5 | 已终止 |

### 10.2 `IsolateError`

```sl
public class IsolateError extends Object
{
    public static const Int32 None              = 0
    public static const Int32 SpawnFailed       = 1   # 入口非法（非函数值 / method_id 解析失败）/ 资源不足
    public static const Int32 NotSendable      = 2   # 消息含不可发送对象
    public static const Int32 CyclicMessage    = 3   # 消息图含环（一期不支持）
    public static const Int32 TransferInvalid  = 4   # 已转移的 TransferableData 被再次使用
    public static const Int32 InvalidHandle    = 5   # port / capability / isolate 句柄无效
    public static const Int32 PortClosed       = 6   # 向已关闭的 port 发送
    public static const Int32 IsolateDead      = 7   # 目标 isolate 已终止
    public static const Int32 PermissionDenied = 8   # capability 不匹配
}
```

C 侧错误码 = `-(70 + 序号)`，如 `SpawnFailed -> -70`、`NotSendable -> -71`。

---

## 11. 数据隔离语义（深拷贝 / 静态字段 / global）

### 11.1 消息与捕获环境一律深拷贝

```sl
# B1 消息深拷贝：worker add 后长度 3，源仍 2
List<int> src = new()
src.add( 1 )
src.add( 2 )
function mutate = function( object arg )
{
    List<int> s = arg as List<int>
    s.add( 999 )                 # 只改 worker 里的副本
    ret s.length
}
Int32 n = Isolate.run1( mutate, src ) as int    # 3
# src.length == 2（源不受影响）

# A6 闭包捕获环境同样深拷贝：worker 改副本，源不变
Int32 v = 10
List<int> lst = new()
lst.add( 1 )
function bump = function()
{
    v = v + 100                  # 只改 worker 侧副本
    lst.add( 100 )
    ret lst.length
}
Int32 r = Isolate.run0( bump ) as int    # 2
# v == 10 && lst.length == 1（源不变）
```

### 11.2 类静态字段：per-isolate（静态影子表）

worker VM 拥有**独立的静态字段副本**，初始为声明处的初始化表达式值：

```sl
Counter
{
    static Int32 g_value = 0

    static fun()
    {
        g_value = 7
        function bump = function()
        {
            g_value = g_value + 100     # 只影响 worker 自己的副本
            ret g_value
        }
        Int32 w = Isolate.run0( bump ) as int    # 100：worker 从初始值 0 起步
        Console.println( w.toString() )          # 100
        Console.println( g_value.toString() )    # 7：主 isolate 不受影响
    }
}
```

**静态初始化器在每个 isolate 各跑一次**：worker VM 首次触碰类时**重跑静态初始化表达式**：

```sl
InitProbe
{
    static Int32 g_init = 41

    static fun()
    {
        g_init = 0                                    # 主 isolate 手动清零
        function readInit = function() { ret g_init }
        Int32 w = Isolate.run0( readInit ) as int    # 41：worker 重跑初始化器
        Console.println( w.toString() )              # 41
        Console.println( g_init.toString() )         # 0：主 isolate 保持
    }
}
```

### 11.3 `global` 全局数据变量同样隔离

```sl
global.var1 = 99
function bumpGlobal = function()
{
    global.var1 = global.var1 + 1    # 只改 worker 的 shadow 副本
    ret global.var1
}
Int32 w = Isolate.run0( bumpGlobal ) as int
# w != 99（worker 读到 shadow 初始值）；global.var1 == 99（主端不受影响）
```

### 11.4 GC 与引用

- 各 isolate **独立 GC**，只扫自己的堆（`Memory.collect()` 只作用于当前 isolate）。
- **静态字段是 GC 根**：静态字段引用的对象在强制 GC 后仍可达。
- `Channel<T>` 缓冲同样是 GC 根（缓冲直存引用、不做深拷贝——Channel 只在**同一** isolate 内使用）。
- 一次性 `run*` 的 worker 结束后即销毁，堆随之回收（组计数回落）。

---

## 12. 与协程的组合

isolate（跨堆并行）与协程（单 isolate 内并发）可自由组合：worker 内可再起任意多协程。

### 12.1 worker 内起协程（按名 spawn + waitAll + await）

```sl
# 按 Coroutine.spawn 名字调用的方法须全工程唯一（沿用协程惯例）
static Int32 isoAdd2( Int32 a, Int32 b )
{
    ret a + b
}

# H1 worker 内协程并发求和
function entry = function()
{
    Task t1 = Coroutine.spawn2( "isoAdd2", 1, 1 )
    Task t2 = Coroutine.spawn2( "isoAdd2", 4, 2 )
    Coroutine.waitAll2( t1, t2 )
    Int32 v1 = Coroutine.awaitHandle( t1 ) as int
    Int32 v2 = Coroutine.awaitHandle( t2 ) as int
    ret v1 + v2
}
Int32 r = Isolate.run0( entry ) as int    # 8
```

### 12.2 主 isolate 协程间端口通信

```sl
# H2 延迟发送协程 + 阻塞接收协程
static bool g_recvFlag = false

static coroSendLater( object arg )
{
    SendPort sp = arg as SendPort
    Coroutine.sleep( 20 )
    sp.send( "late" )
}
static coroRecvAndFlag( object arg )
{
    ReceivePort rp = arg as ReceivePort
    string msg = rp.recv() as string     # 阻塞本协程，不阻塞他人
    if ( msg == "late" )
    {
        g_recvFlag = true
    }
}

ReceivePort rp = ReceivePort()
Task tSend = Coroutine.spawn1( "coroSendLater", rp.sendPort )
Task tRecv = Coroutine.spawn1( "coroRecvAndFlag", rp )
Coroutine.waitAll2( tSend, tRecv )
# g_recvFlag == true
```

### 12.3 Channel 与 Port 并存（各自独立存取）

```sl
Channel<object> ch = Channel<object>.create( 4 )    # 同 isolate 内：传引用
ReceivePort rp = ReceivePort()                      # 跨 isolate：深拷贝

ch.send( "a" )
rp.sendPort.send( "b" )

string a = ch.recv() as string      # "a"
string b = rp.recv() as string      # "b"
```

---

## 13. API 速查总表

### Isolate

| 调用 | 返回 | 说明 |
|---|---|---|
| `Isolate.current()` | `Isolate` | 当前 isolate 句柄 |
| `Isolate.spawn0( entry )` | `Isolate` | 无参入口，同组创建并启动 |
| `Isolate.spawn1( entry, a0 )` | `Isolate` | 1 参入口 |
| `Isolate.spawn2( entry, a0, a1 )` | `Isolate` | 2 参入口 |
| `Isolate.spawn3( entry, a0, a1, a2 )` | `Isolate` | 3 参入口 |
| `Isolate.run0( entry )` | `object` | 一次性计算（0 参），挂起当前协程 |
| `Isolate.run1( entry, a0 )` | `object` | 一次性计算（1 参） |
| `Isolate.run2( entry, a0, a1 )` | `object` | 一次性计算（2 参） |
| `Isolate.run3( entry, a0, a1, a2 )` | `object` | 一次性计算（3 参） |
| `Isolate.exit( port, message )` | `void` | 同步终止当前 isolate 并发终止消息 |
| `iso.pause()` | `Capability` | 请求暂停，返回 resumeCapability |
| `iso.resume( cap )` | `void` | 恢复；cap 不匹配静默无效 |
| `iso.kill( priority )` | `void` | 终止（0=immediate / 1=beforeNextEvent） |
| `iso.ping( port, response, priority )` | `void` | 存活探测 |
| `iso.setErrorsFatal( fatal )` | `void` | 未捕获异常是否终止 isolate |
| `iso.addOnExitListener( port, response )` | `void` | 退出监听 |
| `iso.addErrorListener( port )` | `void` | 错误监听 |
| `iso.status` | `Int32` | `IsolateStatus` 常量；无效句柄 -1 |
| `iso.debugName` | `string` | `"Isolate-" + id` |
| `iso.handle` | `Int64` | 原始 id |
| `iso.controlPort` | `SendPort` | 控制端口（可发送） |
| `iso.pauseCapability` | `Capability` | 恢复能力（可发送） |
| `iso.terminateCapability` | `Capability` | 终止能力（可发送） |
| `Isolate( ctrlPort, pauseCap, termCap )` | 构造 | 重建句柄 |

### ReceivePort / RawReceivePort

| 调用 | 返回 | 说明 |
|---|---|---|
| `ReceivePort()` | 构造 | 创建接收端 |
| `rp.sendPort` | `SendPort` | 对应发送端（同端口同实例） |
| `rp.listen( handler )` | `void` | Stream 风格回调（内部起分发协程） |
| `rp.recv()` | `object` | 阻塞收一条；关闭耗尽后 `null` |
| `rp.tryRecv()` | `object` | 非阻塞收一条；无消息 `null` |
| `rp.close()` | `void` | 关闭端口 |
| `rp.count` | `Int32` | 队列消息数 |
| `rp.isClosed` | `bool` | 是否已关闭 |

### SendPort / Capability / TransferableData / IsolateGroup

| 调用 | 返回 | 说明 |
|---|---|---|
| `sp.send( message )` | `void` | 异步发送（深拷贝，永不阻塞） |
| `sp.portId` | `Int64` | 端口 id |
| `cap.capId` | `Int64` | 能力 id |
| `TransferableData.fromBytes( bytes )` | `TransferableData` | 创建转移块（拷贝一次） |
| `td.materialize()` | `Array<UInt8>` | 一次性取回；失效句柄返 `null` |
| `td.size` | `Int32` | 字节数 |
| `td.isValid` | `bool` | 句柄是否有效 |
| `IsolateGroup.current()` | `IsolateGroup` | 当前组 |
| `grp.exit()` | `void` | 请求组内全部退出 |
| `grp.isolateCount` | `Int32` | 组内存活数 |
| `grp.id` | `Int64` | 组 id |

---

## 14. 限制与实现偏差（必读）

### 14.1 语言限制

1. **匿名闭包字面量不能直接作为调用实参**——一律先赋给 `function` 变量再传参（`spawn*` / `run*` / `listen` 同此）。
2. **闭包捕获上下文按「宿主方法」粒度共享**：同一方法内任一闭包捕获了不可发送值（如 `Channel`），整个共享上下文即不可发送，同方法后续闭包全部发不出去——捕获不可发送值的闭包必须放在**独立宿主方法**里。
3. `spawn*` / `run*` 入口参数上限 **3 个**；更多参数打包成 `List` 等可发送容器传入。
4. worker 内按名 `Coroutine.spawn*("name", ...)` 的目标方法须**全工程唯一**（沿用协程惯例，建议加 `iso` / `coro` 前缀）。
5. 消息图**不支持环**（一期）：检测到循环引用报 `IsolateError.CyclicMessage`。
6. `throw` 只能抛 `enum extends Error`；**Error 枚举值不可序列化**（见 14.2 第 1 条的根源）。

### 14.2 实现偏差（相对 ISOLATE_DESIGN.md）

1. **异常 worker 的错误传播不完整**：Error 枚举不可序列化 → 异常 worker 的 exit_blob 为 NULL →
   - `addErrorListener` 注册的端口**收不到**消息；
   - `addOnExitListener` 端口收到 `null`；
   - `Isolate.run*` 对异常 worker **返回 `null` 且不向调用者重抛**（设计文档称会传播）。
2. **`kill(0)` 立即终止后 isolate 注册表不摘除**，`status` 仍可查 `Dead(5)`。
3. **`TransferableData.materialize` 对无效句柄返回 `null` 而非抛 `IsolateError.TransferInvalid`**。
4. **当前为 P1（M:1 单线程协程式隔离）**：所有 isolate 跑在同一调度循环上，主 isolate 的协程阻塞（`recv` / `sleep` / `waitAll`）时调度器才推进 worker——因此测试/业务代码中用 `sleep` / `recv` 天然制造让出点；**无真并行**（P2 每 isolate 一 OS 线程，语义一致）。
5. `SendPort` 的 `closed` 状态**不可跨 isolate 实时同步**：关闭后已入队的消息仍会被投递，发送方可能拿到 `PortClosed` 也可能在关闭前成功入队——固有竞态。

---

## 15. 实战用例集

### 15.1 一次性 CPU 密集计算（最常用）

```sl
import Std;
import Core;

Heavy
{
    static fun()
    {
        # 闭包可捕获局部变量：捕获环境随闭包深拷贝进 worker，
        # worker 内修改不影响源（见 §11.1）
        Int32 base = 100
        function heavy = function( int a, int b )
        {
            Int32 acc = base            # 读到的是副本
            for Int32 i = 0, i < 1000000, i = i + 1
            {
                acc = acc + 1
            }
            ret acc + a + b
        }

        Int32 r = Isolate.run2( heavy, 3, 4 ) as int
        Console.println( "r = " + r.toString() )
        # base 仍是 100：源不受影响
    }
}
```

### 15.2 长生命周期 worker 服务（请求-响应）

```sl
import Std;
import Core;

# 顶层辅助类：注意普通类实例不可作为消息发送（白名单外）
# —— 请用 List / Map / 标量组装协议
WorkerSvc
{
    static fun()
    {
        # worker 入口：回传自己的 SendPort，然后循环收命令
        function workerMain = function( object arg )
        {
            SendPort mainPort = arg as SendPort
            ReceivePort cmd = ReceivePort()
            mainPort.send( cmd.sendPort )           # 握手：回传命令端口

            while ( true )
            {
                object msg = cmd.recv()              # 阻塞等命令
                if ( msg == null )
                {
                    break                           # 端口已关闭
                }
                string text = msg as string
                if ( text == "shutdown" )
                {
                    cmd.close()
                    break
                }
                mainPort.send( text + "!" )          # 回响应
            }
        }

        # 主 isolate 侧
        ReceivePort rp = ReceivePort()
        Isolate iso = Isolate.spawn1( workerMain, rp.sendPort )

        SendPort worker = rp.recv() as SendPort     # 握手完成

        worker.send( "hello" )
        Console.println( rp.recv() as string )      # "hello!"

        worker.send( "shutdown" )                   # 优雅关停
        Coroutine.sleep( 50 )                        # 让出点：等 worker 退出
        Console.println( "worker status = " + iso.status.toString() )
    }
}
```

### 15.3 静态计数器隔离（故障隔离 / 状态隔离）

```sl
import Std;
import Core;

Counter
{
    static Int32 g_hits = 0

    static fun()
    {
        g_hits = 5

        # worker 从自己的初始值 0 起步（静态影子表）
        function worker = function()
        {
            Int32 i = 0
            for Int32 k = 0, k < 3, k = k + 1
            {
                g_hits = g_hits + 1                  # 只改 worker 副本
            }
            ret g_hits
        }

        Int32 w = Isolate.run0( worker ) as int      # 3
        Console.println( "worker sees " + w.toString() )        # 3
        Console.println( "main sees " + g_hits.toString() )     # 5：不受影响
    }
}
```

### 15.4 大块数据零拷贝转移

```sl
import Std;
import Core;

BigData
{
    static fun()
    {
        # 1MB 数据：fromBytes 只拷贝一次，发送时零拷贝转移所有权
        Array<UInt8> big = Array<UInt8>( 1024 * 1024 )
        TransferableData td = TransferableData.fromBytes( big )

        function digest = function( object arg )
        {
            TransferableData t = arg as TransferableData
            Array<UInt8> b = t.materialize()          # 一次性物化到 worker 堆
            Int32 sum = 0
            for Int32 i = 0, i < b.length, i = i + 1
            {
                sum = sum + b._getItem_( i )
            }
            ret sum
        }

        Int32 s = Isolate.run1( digest, td ) as int
        Console.println( "sum = " + s.toString() )
        # td.isValid == false：源句柄已失效，不可再用
    }
}
```

### 15.5 带生命周期控制的 worker

```sl
import Std;
import Core;

Lifecycle
{
    static fun()
    {
        ReceivePort rp = ReceivePort()
        function sleeper = function( object arg )
        {
            SendPort sp = arg as SendPort
            sp.send( "ready" )
            Coroutine.sleep( 60000 )                 # 长期驻留
        }

        Isolate iso = Isolate.spawn1( sleeper, rp.sendPort )
        Coroutine.sleep( 50 )                        # M:1 调度：让 worker 起来

        # 暂停 / 恢复
        Capability cap = iso.pause()
        Console.println( "paused = " + ( iso.status == IsolateStatus.Paused ).toString() )
        iso.resume( cap )

        # 存活探测
        iso.ping( rp.sendPort, "pong", 0 )
        Console.println( rp.recv() as string )       # "pong"

        # 退出监听 + 终止
        ReceivePort exitRp = ReceivePort()
        iso.addOnExitListener( exitRp.sendPort, null )
        iso.kill( 0 )
        while ( exitRp.count < 1 )
        {
            Coroutine.sleep( 5 )
        }
        Console.println( "dead = " + ( iso.status == IsolateStatus.Dead ).toString() )
        Console.println( "onExit payload = " + ( exitRp.recv() == null ).toString() )   # true
    }
}
```

---

**相关文档**：[coroutine.md](./coroutine.md)（协程）、[system_method.md](./system_method.md)（系统方法）、`md/design/ISOLATE_DESIGN.md`（设计规格）
