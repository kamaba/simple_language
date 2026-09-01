# SimpleLanguage 隔离岛（Isolate）程序设计文档

- 版本：v0.1（设计草案，待评审）
- 适用范围：C VM（`csimple_lang/src/vm`）、C# VM（`src/csharp`）、前端（`simple_language/source/Front`）、Core 库（`source/Front/Lib/Core`）
- 目标读者：负责实现本特性的开发 AI
- 关联文档：`md/design/COROUTINE_DESIGN.md`、`md/GC_DESIGN.md`、`md/syntax/coroutine.md`、`md/RUNTIME_LAYOUT_GUIDE.md`
- 对标基线：Dart `dart:isolate`（含 Dart 2.15+ 的 Isolate Group 机制）

***

## 0. 结论先行

| 问题 | 结论 |
|------|------|
| 现有 `Channel<T>` 能不能直接用？ | **不能**。语义冲突（阻塞 vs 异步、共享引用 vs 深拷贝），且 C 层实现是进程全局 + 单 VM 协程等待队列，跨 VM 会直接产生悬垂指针 |
| 能不能用**继承**扩展 `Channel<T>`？ | **不能**。会违反里氏替换（`SendPort` 只能发白名单类型 = 收窄前置条件；`send` 从阻塞变为异步 = 改变可观测行为）。**并列新类型**，不继承 |
| 那复用什么？ | **C 层抽取公共的端口骨架 `VMPort`（`vm/port/vm_port.h/.c`）**，把 FIFO 缓冲、关闭语义、唤醒规则、句柄注册表全部下沉；`Channel<T>` 与 `SendPort/ReceivePort` 各自作为 `VMPort` 的两种**模式**使用 |
| SL 层新增什么类型？ | `Isolate`、`IsolateGroup`、`SendPort`、`ReceivePort`、`RawReceivePort`、`Capability`、`TransferableData`，放在 `Core/Isolate/` 下 |
| 需不需要新 opcode？ | **不需要**。全部走系统方法（与 `Coroutine`/`Channel` 现有惯例一致）。仅需改造 `LoadStaticField/StoreStaticField/LoadGlobal/StoreGlobal` 的**访问路径**（opcode 编号不变） |
| 隔离边界靠什么保证？ | **运行时强制的深拷贝**（与 Dart 一致），不靠线程。线程只是可选的性能手段 |
| 分几期？ | 三期。**P1 单线程协程式隔离**（无 OS 线程、无锁，隔离语义完整）→ **P2 真并行**（每 isolate 一 OS 线程）→ **P3 传输优化**（零拷贝转移、跨 group spawn） |
| 最大的改造量在哪？ | **静态字段 per-isolate 化**（"静态影子表"）。当前静态字段与静态初始化器是**进程全局**的，这是隔离的头号障碍 |

***

## 1. 需求与目标

### 1.1 目标

1. **内存隔离**：两个 isolate 之间**不共享任何可变内存**。一个 isolate 的 GC、崩溃、无限循环，不影响另一个。
2. **唯一通信方式 = 消息 + 克隆**：跨 isolate 传值一律**深拷贝**；除了显式使用 `TransferableData` 的零拷贝转移，任何引用都不得跨 isolate 流动。
3. **Dart 风格 API**：`Isolate.spawn` / `SendPort` / `ReceivePort` / `Capability` / `Isolate.exit` / `Isolate.run`，降低用户心智负担。
4. **Group 机制**：组内 isolate **共享代码与元数据**（spawn 成本极低、类型身份一致），**隔离可变状态**（静态字段、堆、GC、调度器）。
5. **与协程正交**：协程是**单 isolate 内的并发**（共享堆）；isolate 是**跨堆的并行**（不共享）。二者可组合：一个 isolate 内可以有成百上千个协程。
6. **渐进交付**：P1 阶段不加一个锁、不加一个线程，就能交付完整的隔离语义。

### 1.2 非目标

- 不做共享内存多线程（`SharedArrayBuffer` 风格）——与隔离模型冲突。
- 不做分布式/跨进程 isolate（仅进程内）。
- 不做 isolate 内的抢占式协程调度（沿用 `COROUTINE_DESIGN` 的协作式模型）。
- 一期不做跨 isolate 的调试器/热重载。

### 1.3 与协程的定位对比

| 维度 | 协程（Coroutine） | 隔离岛（Isolate） |
|------|------------------|------------------|
| 堆 | **共享**同一个 VM 的 `obj_pool` / LOS | **独立**（各自 `obj_pool` / LOS） |
| 静态字段 | 共享（单线程下无撕裂） | **独立**（静态影子表） |
| GC | 同一个 GC，per-VM 统一标记 | **各自独立 GC** |
| 通信 | `Channel<T>`（传引用，零拷贝） | `SendPort`（**深拷贝**） |
| 调度 | 同一个协作式调度器 | 各自调度器；P2 起各占一个 OS 线程 |
| 切换成本 | 极低（帧链切换） | 高（进程内建 VM + 消息序列化） |
| 典型用途 | 高并发 IO、pipeline、状态机 | CPU 密集计算、故障隔离、插件沙箱 |

***

## 2. Dart 机制梳理（对标基线）

本章先把 Dart 的机制讲清楚，作为后续设计的对照表。引用以 Dart 官方 `dart:isolate` 文档为准。

### 2.1 Isolate = 不共享内存的独立执行上下文

> "All Dart code runs in an isolate, and code can access classes and values only from the same isolate. Different isolates can communicate by sending values through ports."

- 每个 isolate 有**自己的堆**、**自己的 GC**、**自己的事件循环**（event loop + microtask queue）、**自己的 mutator thread**。
- isolate 之间"看不见"彼此的内存——这正是 "isolate" 名字的由来。
- 主 isolate 由 `main()` 启动。

### 2.2 Port 模型

| 概念 | 语义 |
|------|------|
| `ReceivePort` | 消息接收端，实现 `Stream`，可 `listen` |
| `SendPort` | 消息发送端；**一个 `SendPort` 只对应一个 `ReceivePort`，但一个 `ReceivePort` 可以有多个 `SendPort`** |
| `RawReceivePort` | 底层接收端，用 `handler` 回调代替 Stream；便于把"启动逻辑"与"消息处理逻辑"分离 |
| `SendPort.send(msg)` | **异步且立即返回**，不阻塞发送方 |
| 发送 `SendPort` | `SendPort` 本身可以被发送，且**跨 isolate 后保持 `==` 相等** |

### 2.3 可发送消息白名单（关键约束）

`SendPort.send` 的内容受严格限制。允许的类型（递归适用）：

- `null`、`bool`、`int`、`double`、`String`
- `List` / `Map`（元素必须递归可发送）
- `SendPort`
- `Capability`
- `TransferableTypedData`

**不允许**：任意类实例、闭包、Future、Stream、`ReceivePort`（只能发 `SendPort`）、native 句柄等。发送非法对象时抛出 "Illegal argument in isolate message"。

这条约束是**整个隔离模型的基石**：只要消息图里没有共享引用，就不需要任何锁。

### 2.4 创建与退出

```dart
// 长短两类用法
final json = await Isolate.run(_readAndParseJson);              // 一次性：spawn→跑→返回值→销毁
final iso  = await Isolate.spawn(_entry, msg,                   // 长生命周期
    paused: false, errorsAreFatal: true,
    onExit: exitPort, onError: errorPort, debugName: 'worker');
Isolate.exit(finalMessagePort, result);                          // 同步终止当前 isolate
```

- `Isolate.spawn`：**与当前 isolate 共享代码**（同一个 isolate group）。
- `Isolate.spawnUri`：**从头加载指定 URI 的代码**，创建**新的 isolate group**。
- `Isolate.run`：spawn → 执行 → 捕获结果 → 关闭 isolate → 把异常抛回主 isolate。
- `Isolate.exit` 的返回值通过 `finalMessagePort` 送回；Dart 会做一次**验证扫描**确保对象可传输。

### 2.5 生命周期控制：Capability 能力令牌

```dart
final iso = await Isolate.spawn(entry, msg);
final c   = iso.pause();          // 返回 resumeCapability
iso.resume(c);
iso.kill(priority: Isolate.immediate);   // 或 Isolate.beforeNextEvent
iso.ping(responsePort, response: 'pong', priority: Isolate.immediate);
iso.setErrorsFatal(false);
iso.addOnExitListener(exitPort, response: 'done');
iso.addErrorListener(errorPort);
```

- **`Capability`**：不可伪造的权限令牌，**穿过 isolate 后 `==` 依然成立**。
- `Isolate` 对象本身**不能被发送**，但 `controlPort` / `pauseCapability` / `terminateCapability` **可以**，接收方可用 `Isolate(controlPort, pauseCapability: ..., terminateCapability: ...)` 重建一个等价句柄。
- 没有对应 capability 而调用 `pause()` / `kill()` 时**静默无效**（不是报错）——这是能力安全模型的要求。
- 常量：`Isolate.immediate`（立即）、`Isolate.beforeNextEvent（下一个事件前）。

### 2.6 错误模型

- 未捕获错误 → 发到 `onError` 端口 / `errors` 广播流，载荷为 `[error, stackTrace]`。
- `RemoteError` 用于在 isolate 间传递错误描述。
- `errorsAreFatal = true`（默认）时，未捕获错误**终止**该 isolate。
- `IsolateSpawnException`：spawn 失败时抛出。

### 2.7 Isolate Group 机制（重点）

Dart 2.15 引入 isolate groups，核心动机是**降低 spawn 开销**：原先每次 `spawn` 都要复制整份程序快照，组内共享后几乎免费。

**组内共享（group-wide，只读 / 不可变为主）：**

| 共享内容 | 说明 |
|---------|------|
| 可执行代码 | JIT/AOT 产物、函数对象、字节码 |
| 类型信息 | `Type` 元对象、类型提要（type feedback）、`RuntimeType` 元数据 |
| 程序结构 | 类定义、方法表、字段布局 |
| 不可变数据 | 字符串驻留表、常量实例、部分不可变字符串 |

**组内隔离（per-isolate）：**

| 隔离内容 | 说明 |
|---------|------|
| 可变堆对象 | 所有普通实例、数组、`Map` |
| **静态字段 / 顶层变量** | 每个 isolate 一份 |
| GC | 各自独立的堆与 GC 周期 |
| 求值栈 / 帧链 | 各自独立 |
| 事件循环 / mutator thread | 各自独立 |
| 端口表（port table） | 各自独立（但 port id 全局唯一以便路由） |

**要点：**

1. `Isolate.spawn` → **同组**；`Isolate.spawnUri` → **新建组**。
2. 同组 isolate 共享代码 ⇒ 类型身份一致 ⇒ 消息反序列化后 `is` 判断、`Type` 比较依旧正确。这是 group 存在的关键理由之一。
3. `IsolateGroup.exit()` 可终止整组。
4. 组内仍**不共享可变状态**，仍然只能靠消息通信。

### 2.8 我们抄什么、不抄什么

| Dart 机制 | 本设计 | 说明 |
|-----------|--------|------|
| 不共享内存 + 消息通信 | ✅ 全盘采用 | 核心 |
| `SendPort` / `ReceivePort` / `RawReceivePort` | ✅ 采用 | 按 SL 命名习惯调整 |
| 可发送消息白名单 | ✅ 采用并扩展 | 增加 `TransferableData`、字节数组 |
| `Isolate.spawn` / `exit` / `run` | ✅ 采用 | API 名保持一致 |
| `Capability` 能力模型 | ✅ 采用 | 含"无能力时静默无效"语义 |
| errorsAreFatal / onExit / onError | ✅ 采用 | 错误改为 SL 的 `Error` 值 |
| **Isolate Group（代码共享 / 静态隔离）** | ✅ 采用，且**天然映射现有架构** | 见 4.3 |
| `spawnUri`（运行时加载新代码） | ⚠️ 三期 | 需要动态装载 SLIR，成本高 |
| 每 isolate 一个**真 OS 线程** | ⚠️ 二期 | 一期用单线程协作式，语义等价 |
| `Isolate.run` 的零拷贝结果转移 | ⚠️ 三期 | 需 `TransferableData` |

***

## 3. 现有实现盘点

> 所有路径以仓库根 `f:/project/lang` 为基准。行号为当前 HEAD 状态，实现时以符号名为准。

### 3.1 `VM` 结构：哪些状态是 per-VM 的

文件：`csimple_lang/src/vm/runtime/vm_runtime.h`

已 per-VM（好消息，隔离的天然基础）：

| 字段 | 说明 |
|------|------|
| `obj_pool` / `obj_pool_capacity` / `los_head` | **对象堆**（小对象池 + 大对象空间） |
| `mem_manager` | 内存管理器（manual / GC 模式） |
| `stack` / `sp` / `stack_slot_kind` | 求值栈（协程化后指向当前协程的栈块） |
| `try_stack` / `pending_exception` | 异常处理状态 |
| `current_frame` / `frame_stop` | 帧链 |
| `all_coroutines` / `current_coroutine` / `root_coroutine` / `scheduler` | 协程与调度器 |
| `native_depth` / `checked_depth` | 重入与检查算术上下文 |
| `name` / `level` | 调试名与层级（子 VM 用） |

**结论：`VM` 实例本身就是"堆 + 执行上下文"的天然容器，isolate ≈ `VM` 实例。**

### 3.2 进程级全局状态清单（隔离的障碍）—— 头号问题

这些是**文件级 `static`**，被进程内所有 `VM` 共享。isolate 化必须逐个处理：

| 变量 | 文件 | 用途 | isolate 化策略 |
|------|------|------|---------------|
| `s_runtime_load_model` | `vm/runtime/vm_runtime.c`（`vm_set_runtime_load_model`） | 已加载的 SLIR 模块模型 | **Group 共享（只读）** ✅ |
| `s_runtime_assembly` | 同上 | 程序集：类表 / 方法表 | **Group 共享（只读）** ✅ |
| `s_runtime_class_list` | `vm/runtime/runtime_class_manager.c` | 全进程 `RuntimeClass` 列表 | **Group 共享（只读）** ✅ |
| 内置 `s_*_runtime_type` | `vm/runtime/runtime_type_manager.c` | `Object/String/Int32/...` 的 `RuntimeType` 单例 | **Group 共享（只读）** ✅ |
| `s_method_entries` / `s_class_entries` | `vm/parse/sl_runtime_module_registry.c` | 方法 / 类元数据注册表 | **Group 共享（只读）** ✅ |
| `s_pool` | `vm/parse/slir_string_pool.c` | irStringDict 字符串常量池 | **Group 共享（只读）** ✅ |
| `s_runtime_class_map` | `vm/assembly/sl_runtime_assembly.c` | 类包 → 运行时类映射 | **Group 共享（只读）** ✅ |
| `s_method_code_cache` | `vm/runtime/method/runtime_method.c` | 方法字节码缓存 | **Group 共享（只读，需加只读锁或构建期冻结）** ✅ |
| ⚠️ `static_member_runtime_object_array`<br>⚠️ `static_member_data_buffer` | `vm/runtime/vm_runtime_type.h`（`RuntimeType` 字段） | **类的静态字段值存储** | 🔴 **必须 per-isolate**（静态影子表，见 4.4） |
| ⚠️ `s_applied_keys[256]` / `s_applying_keys[64]` | `vm/runtime/vm_runtime.c`（`vm_runtime_type_ensure_static_expr_initialized`） | **静态初始化器去重表** | 🔴 **必须 per-isolate**（否则第二个 isolate 不跑静态初始化器） |
| ⚠️ `s_global_entries[256]` / `s_global_count` | `vm/runtime/vm_runtime_manager.c` | **`global` 全局变量映射表** | 🔴 **必须 per-isolate** |
| ⚠️ `s_global_init_instructions` / `s_is_global_init_applied` | 同上 | **全局初始化指令与已应用标志** | 🔴 **必须 per-isolate** |
| ⚠️ `s_channel_head` / `s_channel_next_id` | `vm/system_method_call/coroutine_system_method.c` | **Channel 注册表** | 🔴 必须 per-isolate（或加互斥锁，见 6.3） |
| 系统方法表 | `vm/system_method_call/system_method_registry.c` | name/id → C 函数 | **进程共享（只读函数指针）** ✅ |
| 日志系统 `sl_log_*` | `src/log` | 日志 | 进程共享（**输出需加锁或行缓冲**） |

**一句话总结**：**代码/元数据天然可共享（→ Group），可变数据（静态字段、全局、Channel 表）必须 per-isolate。**

这正好与 Dart 的 group 划分完全吻合，是本设计最关键的洞察。

### 3.3 `Channel<T>` 现状

**SL 层**：`simple_language/source/Front/Lib/Core/Container/Channel.sl`

```sl
public class Channel<T> extends Object
{
    Int64 _chid = 0                       # SL 对象只持有句柄
    _init_() { this._chid = SystemChannelCreate(0) }
    public void send( T value )  { SystemChannelSend(this._chid, value) }
    public T recv()              { ret SystemChannelRecv(this._chid) as T }
    public void close()          { SystemChannelClose(this._chid) }
    get int count()              { ret SystemChannelCount(this._chid) }
    get bool isClosed()          { ret SystemChannelIsClosed(this._chid) }
}
```

**C 层**：`csimple_lang/src/vm/system_method_call/coroutine_system_method.c`

```c
typedef struct VMChannelWaiter { VMCoroutine* coro; struct VMChannelWaiter* next; } VMChannelWaiter;

typedef struct VMChannel {
    int64                 id;
    int32                 capacity;      /* <=0 = unbounded */
    uint8                 closed;
    VMRuntimeValue*       buf;           /* ← 直接存 VMRuntimeValue！ */
    int32                 buf_count, buf_capacity;
    VMChannelWaiter*      send_q;        /* 缓冲满的发送者 */
    VMChannelWaiter*      recv_q;        /* 缓冲空的接收者 */
    struct VMChannel*     next;
} VMChannel;

static VMChannel* s_channel_head = NULL;     /* ← 进程全局，无锁 */
static int64      s_channel_next_id = 0;
```

**语义**：`Send` 满则挂起发送者（背压）；`Recv` 空则挂起接收者；`Close` 唤醒全部，`Recv` 在关闭且缓冲空时返回 `null`；对已关闭通道 `Send` 抛异常。

### 3.4 协程与调度器现状

- `VMCoroutine` / `VMScheduler`：`csimple_lang/src/vm/runtime/coroutine/vm_coroutine.h`
- 协作式 FIFO 就绪队列 + 定时器链表；`vm_scheduler_enter(vm)` 为入口（`vm_coroutine.c`）
- 挂起原语：`vm_coroutine_suspend_current(vm, reason, reexecute, requeue)`
- 阻塞系统调用采用 **Option A 协议**：peek 参数（不 pop）→ 挂起且 `reexecute=TRUE` → 恢复后重新检查等待条件
- 挂起原因已有 `CORO_BLOCK_NONE/YIELD/SCHED/AWAIT/SLEEP/IO`

### 3.5 GC 现状

- 文件：`csimple_lang/src/vm/memory/vm_gc.c`，设计基线 `md/GC_DESIGN.md` §3.3–3.6
- **per-VM**：根集合 = 手动管理对象 + 求值栈 PTR/STRING 槽 + 当前帧的 arg/local/ret slots + 各协程帧链与私有栈
- 三色标记 + 小对象池压缩式 sweep + LOS 链表式 sweep
- **`gc_state->collecting` 防重入；无任何线程安全假设**

### 3.6 子 VM（child VM）与对象转移模式

文件：`csimple_lang/src/vm/runtime/call/runtime_call.c`（`vm_execute_instruction_buffer_with_child_vm`）

现状：执行静态字段初始化器 / `NewObject` 时，临时 `vm_create_with_id` 一个子 VM，跑完内联指令列表后，**把子 VM 对象池中的对象逐个 `vm_pool_add_object` 转交给父 VM**，再销毁子 VM。

**意义**：项目里**已有"对象所有权在 VM 之间转移"的成熟模式**，isolate 的消息深拷贝 / 零拷贝转移可以直接站在它肩上。

### 3.7 线程现状

- `src/base` 下**没有任何线程/互斥/条件变量/原子操作封装**（只有 `mem/chars/list/array/queue/string/unicode`）
- `src/platform/{windows,unix}` 只有 date / dir / file / misc / module / timer / unicode
- 全仓库几乎无 OS 线程使用

**结论：线程是净新增工作量**，必须新建 `src/base/thread/` 抽象层。这也是把"真并行"放到二期的原因。

### 3.8 结论：现有 `Channel<T>` 能不能用？

| 检查项 | 现状 | 能否用于 isolate |
|--------|------|-----------------|
| 句柄模型（`Int64` + C 侧注册表） | ✅ 已有 | 可直接沿用 |
| FIFO 缓冲 + 容量 + 关闭语义 | ✅ 已有 | 可复用 |
| 等待者唤醒规则（send 唤醒 recv / recv 唤醒 send） | ✅ 已有 | 部分复用（isolate 侧不需要 send_q） |
| **缓冲元素存 `VMRuntimeValue`（含裸 `VMObject*`）** | ❌ | **致命**：跨 VM 即悬垂 / 别名，直接违反隔离 |
| **等待者是 `VMCoroutine*`（单 VM 内）** | ❌ | **致命**：跨 VM 唤醒会无锁触碰另一个调度器 |
| **注册表是进程全局 `static`，无锁** | ❌ | 二期多线程下必须加锁 |
| `Send` 满时**阻塞发送协程**（背压） | ❌ | **语义冲突**：Dart 的 `SendPort.send` 必须永不阻塞 |
| `Recv` 空时阻塞接收协程 | ✅ | 与 `ReceivePort` 一致，可复用 |

**最终判定**：

- **不能继承扩展**（LSP 违规，见第 7 章论证）。
- **不能原样复用**（三条致命项）。
- **推荐方案：C 层抽取公共端口骨架 `VMPort`，两种模式并存**；SL 层 `Channel<T>` 与 `SendPort/ReceivePort` **并列，不继承**。

***

## 4. 总体设计

### 4.1 术语

| 术语 | 含义 |
|------|------|
| **Isolate（隔离岛）** | 一个独立的执行上下文 ≈ 一个 `VM` 实例：独立堆、独立静态状态、独立调度器、独立 GC |
| **Isolate Group（隔离组）** | 共享代码与元数据的一组 isolate；组内 isolate 的**类型身份一致** |
| **Port（端口）** | 消息端点。`SendPort`（发送端）/ `ReceivePort`（接收端） |
| **Message（消息）** | 跨 isolate 传递的值；**传递即克隆** |
| **Capability（能力）** | 不可伪造的权限令牌，跨 isolate 传递后保持 `==` |
| **静态影子表（Static Shadow Table）** | per-isolate 的静态字段存储副本，屏蔽全局 `RuntimeType` 上的静态数据 |
| **可发送对象（Sendable）** | 允许出现在消息图中的类型集合（白名单） |
| **TransferableData** | 显式选择零拷贝转移的字节块（转移后源端失效） |

### 4.2 隔离边界定义

```
┌─────────────────────── Isolate Group ───────────────────────┐
│                                                             │
│  【共享：只读 / 不可变】                                       │
│    RuntimeAssembly · RuntimeClass · RuntimeType(元数据部分)   │
│    方法字节码缓存 · irStringDict 字符串池 · 类型表 · 方法表     │
│                                                             │
│  ┌──────── Isolate A ────────┐   ┌──────── Isolate B ────────┐│
│  │ VM 实例                    │   │ VM 实例                    ││
│  │  ├ obj_pool / LOS  (独立堆) │   │  ├ obj_pool / LOS  (独立堆) ││
│  │  ├ GC             (独立)   │   │  ├ GC             (独立)   ││
│  │  ├ 静态字段影子表   (独立)   │   │  ├ 静态字段影子表   (独立)   ││
│  │  ├ 调度器 + 协程    (独立)   │   │  ├ 调度器 + 协程    (独立)   ││
│  │  ├ 求值栈 / 帧链    (独立)   │   │  ├ 求值栈 / 帧链    (独立)   ││
│  │  └ 端口表           (独立)   │   │  └ 端口表           (独立)   ││
│  │  [P2] OS 线程        (独立)  │   │  [P2] OS 线程        (独立)  ││
│  └──────────┬────────────────┘   └──────────┬────────────────┘│
│             │   SendPort ──深拷贝──▶ ReceivePort              │
│             └──────────── 消息队列（加锁） ◀─────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

**不变式（必须被运行时强制，而非靠用户自觉）：**

> **I1**：任何 `VMObject*`（含 `VMArray*`、`VMObject` 字符串）**不得**出现在另一个 isolate 的对象池、求值栈、帧 slots、静态影子表中。
> **I2**：跨 isolate 的值传递**必经** `vm_isolate_serialize` → 字节 blob → `vm_isolate_deserialize`，或显式的 `TransferableData` 所有权转移。
> **I3**：`TransferableData` 转移后，源 isolate 的句柄立即失效，任何后续访问报 `Error.IsolateTransferInvalid`。

### 4.3 Isolate Group 设计

**为什么要有 Group（三个硬理由）：**

1. **spawn 成本**：重建 `RuntimeAssembly`（类表 + 方法表 + 字节码）开销巨大。组内共享后，spawn 只需建一个 `VM` 结构 + 静态影子表，接近免费。
2. **类型身份一致**（决定性理由）：`RuntimeType*` 指针被编码在对象头与 `is` 判断里。若两个 isolate 各建一套 `RuntimeType`，深拷贝过去的 `List<int>` 在新 isolate 里就**不是** `List<int>` 了。组内共享 ⇒ `RuntimeType*` 相同 ⇒ 反序列化后类型判断天然正确。
3. **内存**：类元数据、字符串驻留表、字节码各 isolate 一份会成倍浪费。

**Group 的 C 结构：**

```c
typedef struct _VMIsolateGroup
{
    int64                    id;
    /* ── 共享的只读程序结构（指向进程级全局，group 只持有引用与生命周期） ── */
    const SLIRRuntimeLoadModel* load_model;
    SLRuntimeAssembly*          assembly;         /* 类表 / 方法表 */
    /* ── 组内成员 ── */
    struct _VMIsolate**         isolates;
    int32                       isolate_count;
    int32                       isolate_capacity;
    /* ── 生命周期 ── */
    int32                       ref_count;        /* isolate 数 + 外部引用 */
    uint8                       exiting;          /* IsolateGroup.exit 已请求 */
    /* 组内共享的不可变对象（P3：常量字符串驻留 / 共享常量实例） */
    void*                       shared_heap;
    /* 线程安全（P2 起） */
    void*                       lock;             /* 成员表互斥 */
} VMIsolateGroup;
```

**映射关系：**

| Dart Isolate Group | SimpleLanguage 对应 | 现状 |
|--------------------|--------------------|------|
| 共享 JIT/AOT 代码 | 共享 `s_method_code_cache`、方法表 | 已是进程全局 ✅ |
| 共享类型信息 | 共享 `RuntimeType` / `RuntimeClass` / `s_runtime_class_list` | 已是进程全局 ✅ |
| 共享程序结构 | 共享 `s_runtime_assembly`、`s_runtime_load_model` | 已是进程全局 ✅ |
| 共享不可变数据 / 字符串驻留 | `s_pool`（irStringDict） | 已是进程全局 ✅ |
| **per-isolate 可变堆** | `VM.obj_pool` / `VM.los_head` | **已 per-VM** ✅ |
| **per-isolate 静态字段** | `RuntimeType.static_member_*` | 🔴 **现为全局，需影子表** |
| **per-isolate GC** | `vm_gc_collect(vm)` | **已 per-VM** ✅ |
| **per-isolate 事件循环** | `VMScheduler` | **已 per-VM** ✅ |

**结论：只需把"静态字段 + 全局表"这一项 per-isolate 化，Group 模型即完整成立。**

**Group 生命周期：**

- 进程启动 → 创建**默认 group**（`SL_ISOLATE_GROUP_DEFAULT`），主 isolate 加入
- `Isolate.spawn` → 在**当前 isolate 所属的 group** 内创建
- `Isolate.spawnUri` / `Isolate.spawnInNewGroup`（P3）→ 新建 group 并独立加载 SLIR
- `IsolateGroup.exit()` → 置 `exiting`，请求组内所有 isolate 退出
- 最后一个 isolate 退出且 `ref_count == 0` → 销毁 group（**不销毁共享的程序结构**，它由进程/加载器持有）

### 4.4 静态字段影子表（Static Shadow Table）—— 本设计的核心改造

**问题**：`RuntimeType` 携带 `static_member_runtime_object_array` / `static_member_data_buffer` / `static_member_initialized` / `static_member_expr_applied`。类静态字段（`OpCode_LoadStaticField` / `OpCode_StoreStaticField`）与 `global` 全局变量（`OpCode_LoadGlobal` / `OpCode_StoreGlobal`）都落在这些全局结构上。`vm_runtime.c` 里还有进程全局的静态初始化器去重表 `s_applied_keys[256]`。

**方案**：保留 `RuntimeType` 的**元数据**全局共享，把**可变数据**外置到 per-isolate 的影子表。

```c
/* 一个 RuntimeType 在某个 isolate 中的静态数据副本 */
typedef struct VMStaticShadowEntry
{
    RuntimeType*        rt;                    /* key（指针相等即命中，O(1) 哈希） */
    VMRuntimeObject**   member_runtime_objects;/* 静态字段的 VMRuntimeObject 数组 */
    int32               member_count;
    uint8*              member_data_buffer;    /* 扁平字节存储（值类型字段） */
    int32               member_data_size;
    uint8               initialized;
} VMStaticShadowEntry;

typedef struct VMStaticShadow
{
    VMStaticShadowEntry* entries;      /* 开放寻址哈希表，key = RuntimeType* */
    int32                capacity;     /* 2 的幂 */
    int32                count;
} VMStaticShadow;
```

挂在 `VM` 上：

```c
typedef struct _VM {
    /* ... 现有字段 ... */
    struct _VMIsolate*  isolate;        /* 反向指针；NULL = 非 isolate 上下文（子 VM / 编译期 VM） */
    VMStaticShadow*     static_shadow;  /* ★ 新增：per-isolate 静态字段存储 */
} VM;
```

**改造点：**

| 位置 | 现状 | 改造后 |
|------|------|--------|
| `OpCode_StoreStaticField`（`vm_runtime.c`） | 直接写 `rt->static_member_*` | `vm_static_shadow_get_or_create(vm, rt)` 后写入影子条目 |
| `OpCode_LoadStaticField` | 直接读 `rt->static_member_*` | 读影子条目 |
| `vm_runtime_manager_store_global` / `load_global` | 写 `rt->static_member_*` | 同上（global 的 `field_index` 走影子表） |
| `vm_runtime_type_ensure_static_expr_initialized` 的 `s_applied_keys` / `s_applying_keys` | 进程全局 static 数组 | **移入 `VM`**：`vm->static_applied_keys` / `vm->static_applying_keys` |
| `vm_runtime_manager` 的 `s_global_entries` / `s_is_global_init_applied` | 进程全局 | **移入 `VM`**（或移入 isolate 结构） |

**兼容策略**：`vm->isolate == NULL`（子 VM、编译期 VM、`vm_execute_instruction_buffer_with_child_vm`）时，影子表为 `NULL`，**退化到直接读写 `RuntimeType` 全局存储**，行为与今天完全一致。这保证改造是纯增量的，不打破任何现有路径。

**代价**：每次静态字段访问多一次哈希查找。优化：影子条目缓存在 `RuntimeType` 上做"上一次命中"快速路径（`rt->last_shadow_vm == vm`），命中率接近 100%。

### 4.5 消息模型

#### 4.5.1 Port 表与消息队列

```c
typedef struct _VMIsolate
{
    int64                 id;
    char*                 debug_name;
    VM*                   vm;                  /* 拥有独立堆与调度器的 VM 实例 */
    VMIsolateGroup*       group;
    /* 生命周期 */
    int32                 state;               /* Created/Running/Paused/Exiting/Dead */
    uint8                 errors_are_fatal;
    /* 控制面 */
    int64                 control_port_id;
    int64                 pause_capability_id;
    int64                 terminate_capability_id;
    /* 监听者 */
    int64*                on_exit_ports;  int32 on_exit_count;
    int64*                on_error_ports; int32 on_error_count;
    /* 线程（P2） */
    void*                 thread;
    /* 消息泵 */
    void*                 msg_queue;           /* 加锁 FIFO */
    void*                 msg_lock;
    void*                 msg_cond;            /* P2：无消息时阻塞等待 */
    /* GC / 诊断 */
    struct _VMIsolate*    next;                /* 进程内 isolate 链表 */
} VMIsolate;
```

- **Port id 全局唯一**（进程级单调计数器），保证跨 isolate 路由无歧义。
- 每个 isolate 持有自己的**端口表**（`port_id → VMPort*`），但 `port_id → 归属 isolate` 的路由表是**进程级**的（加锁）。
- `SendPort.send(msg)`：由**发送方 isolate** 完成序列化 → 得到纯字节 blob → 找到目标 isolate → 入队 → 唤醒目标（P1 入就绪队列 / P2 `cond_signal`）。**发送方永不阻塞、永不触碰目标堆**。

#### 4.5.2 消息序列化格式

采用自描述的 TLV 流（SimpleLanguage Isolate Message, `SLM1`）：

```
Header: 'S','L','M','1'  (4 bytes)  +  u8 version  +  u8 flags  +  u16 reserved
Body  : TLV 序列（前序遍历，深度优先）

Tag 表：
  0x00  NULL
  0x01  BOOL        u8
  0x02  INT32       i32LE
  0x03  INT64       i64LE
  0x04  UINT64      u64LE
  0x05  FLOAT32     f32LE
  0x06  FLOAT64     f64LE
  0x07  FLOAT8_E4M3 u8
  0x08  FLOAT8_E5M2 u8
  0x09  FLOAT16     u16LE
  0x0A  FLOAT16_BRAIN u16LE
  0x10  STRING      u32 byteLen + UTF-8 bytes
  0x11  BYTES       u32 len + raw bytes
  0x20  LIST        u32 count + count × TLV      （元素类型必须可发送）
  0x21  MAP         u32 count + count × (TLV key, TLV value)
  0x22  SET         u32 count + count × TLV
  0x30  SENDPORT    u64 port_id
  0x31  CAPABILITY  u64 cap_id
  0x32  TRANSFERABLE u64 xfer_id + u32 byteLen + raw bytes
  0x33  CLOSURE     STRING method_id + ARRAY context    （函数值入口，见 5.2.1）
  0x40  REF_BACKREF u32 blob_offset               （P3：支持共享子图与有向环）
```

设计取舍：

- **不用长度前缀递归嵌套**，用**流式 TLV**，便于一次遍历完成校验（先校验再构造，避免构造到一半失败留下垃圾）。
- 一期**不支持环**（检测到重复引用的非 `SendPort`/`Capability` 对象 → 报 `Error.IsolateCyclicMessage`）。三期再引入 `REF_BACKREF`。
- 数值统一按 tag 编码，**不保留源端的声明类型**（`int` 8/16/32 统一为 `INT32` 或按实际位宽 tag 保留，建议保留 tag 以维持 `Float16` 等低精度语义）。

#### 4.5.3 可发送消息白名单

| 允许 | 说明 |
|------|------|
| `null`、所有 `bool` / 整数 / 浮点（含 `Float8`/`Float16`） | 直接编码 |
| `string` | UTF-8 编码 |
| `List<T>` / `Array<T>` / `Set<T>` / `Map<K,V>` | **元素递归可发送** |
| `SendPort` | 编码为 `port_id`，目标端重建为指向同一接收端的 `SendPort`（**保持 `==`**） |
| `Capability` | 编码为 `cap_id`，**保持 `==`** |
| `TransferableData` | 零拷贝转移（见 4.7） |
| **函数值（闭包）** | **捕获环境全部可发送时**可发送：编码为 `method_id` + 深拷贝的 context 数组（见 5.2.1）。**组内**有效（共享代码） |
| 标注 `@sendable` 的 `data`/不可变类 | P3：字段全可发送时按字段序列展开 |

| 禁止（报 `Error.IsolateNotSendable`） | 原因 |
|------|------|
| 任意普通 `class` 实例（未标注 `@sendable`） | 含方法表 / 可变状态，跨堆即破坏隔离 |
| **捕获了不可发送值的闭包** | context 中任一槽位不可发送即整体拒绝；编译期能判定的提前报错（5.7） |
| `ReceivePort` | 只能发 `SendPort` |
| `Channel<T>` | 绑定单 VM 的协程等待队列（但句柄被克隆后指向已失效的注册表，见 4.5.4） |
| `cor` 协程句柄 | 绑定单 VM 调度器 |
| `Type` / 反射对象 | 由 group 共享，无需发送；发送则报不可发送（防止假隔离） |
| native 句柄 / 指针（`IntPtr` 包装的原生资源） | 跨堆语义未定义 |
| 含上述任一元素的容器 | 递归失败 |

#### 4.5.4 一个必须显式处理的坑：`Channel` / 协程句柄跨 isolate

`Channel<T>` 的句柄是 `Int64`，而 Channel 注册表是 per-isolate 的。因此：

- **消息中不允许出现 `Channel` / `cor` 句柄**（已在白名单中禁止）。
- 即便用户绕过（`SystemChannelSend` 直接传 int），目标端也查不到该 id → 统一报 `Error.IsolateInvalidHandle`。
- **P1 阶段把 Channel 注册表 per-isolate 化**（从 `static` 移入 `VM`），从根本上杜绝跨 isolate 误命中。

### 4.6 深拷贝（克隆）实现

"深拷贝"在实现上等价于 **序列化 → 传输 blob → 反序列化**。

```
发送方 isolate（自己的线程 / 时间片内，自己的堆）
    │
    ├─ 1. vm_isolate_validate_sendable(vm, value)     ① 校验（失败 → Error.IsolateNotSendable）
    ├─ 2. vm_isolate_serialize(vm, value, &blob)      ② 拍成字节（只读遍历，不改源）
    └─ 3. vm_port_post(target_isolate, port_id, blob) ③ 入队（跨线程：blob 是纯字节，安全）

目标 isolate（自己的线程 / 时间片内，自己的堆）
    ├─ 4. 事件循环取出 blob
    └─ 5. vm_isolate_deserialize(vm, blob, &value)    ④ 在目标堆里重建对象图
```

**要点：**

- 第 ② 步**不分配目标堆内存**，全部在源堆内完成，最后产出一个 `uint8*` blob（用 `base_malloc` 分配，不属于任何 VM 堆）。
- 第 ④ 步**只分配目标堆内存**，不触碰源堆。
- blob 的所有权在入队时移交目标 isolate，目标端反序列化后 `base_free`。
- 反序列化时**复用 group 共享的 `RuntimeType*`**（`vm_array_get_or_create_array_type`、`get_runtime_type_by_runtime_class_and_runtime_type_list` 等现有接口），保证类型身份一致。

**字符串**：目标端重新分配 `VMObject` 字符串对象（不共享）。P3 可优化为组内共享不可变字符串（对应 Dart 的组内共享常量）。

### 4.7 零拷贝转移：`TransferableData`

对应 Dart 的 `TransferableTypedData`。

```c
typedef struct VMTransferable
{
    int64   id;             /* 全局唯一 */
    uint8*  data;           /* 不属于任何 VM 堆的裸字节（base_malloc） */
    int32   size;
    uint8*  owner_blob;     /* 若来自消息，指向所属 blob；否则 NULL */
    uint8   transferred;    /* 已转移标记 */
} VMTransferable;
```

**流程：**

1. 源 isolate：`TransferableData.fromBytes(b)` → 分配裸字节块（**不属于源堆**），返回句柄。
2. 发送：编码为 `TRANSFERABLE(u64 id, u32 len, bytes)`。
3. 目标 isolate 反序列化：在本 isolate 堆中新建 `TransferableData` 对象，内部持有裸字节块。
4. **转移后源端句柄立即失效**（`transferred = 1`），再访问报 `Error.IsolateTransferInvalid`。
5. `TransferableData.materialize()` → 在**当前 isolate 堆**中构造真正的 `ByteArray` / `List<int>`，并可释放裸块。

**不变式 I3 由此保证**：任何时刻裸字节块只被一个 isolate 的句柄引用。

### 4.8 线程模型

#### P1（一期）：M:1 协程式隔离 —— **推荐首个里程碑**

- **不引入任何 OS 线程**。
- 所有 isolate 的调度器挂在**同一个**协作式调度循环上：进程级 `vm_isolate_scheduler_run_all()` 轮转各 isolate 的就绪队列。
- 一个 isolate 内阻塞在 `ReceivePort.recv()` 的协程，被挂起（`CORO_BLOCK_ISOLATE_MSG`），让出给其它 isolate 的协程。
- **隔离性完全由深拷贝保证，与线程无关**——因此 P1 与 P2 的用户可见语义**完全一致**。
- 收益：不加锁、不改 GC、不动线程；能立刻交付完整语义并跑通全部用例。
- 代价：无真正并行，CPU 密集任务仍会串行（但**故障隔离、状态隔离、消息语义**全部成立）。

#### P2（二期）：1:1 线程式隔离 —— 真并行

- 每个 isolate 一个 OS 线程（新建 `src/base/thread/`：`sl_thread` / `sl_mutex` / `sl_cond` / `sl_atomic` / `sl_tls`）。
- 各 isolate 独立 `vm_scheduler_enter`；消息队列加 `sl_mutex` + `sl_cond`。
- 共享只读结构（字节码缓存等）在**加载期冻结**，之后只读，无需锁。
- **GC**：各 isolate 独立 GC，天然无需全局 STW（这是 isolate 相对"多线程共享堆"的最大优势）。
- 需要加锁的位置：**端口路由表、Channel 注册表、日志输出、全局 id 分配器（原子自增）**。
- 主线程（主 isolate）需要能等待其它 isolate 完成 → `vm_isolate_join(id)`。

#### P3（三期）：传输与加载优化

- `spawnUri` / 跨 group spawn（运行时加载新 SLIR 模块）
- `TransferableData` 深度优化（大块数据零拷贝、共享内存页）
- 组内共享不可变字符串驻留
- 消息图中的 `REF_BACKREF`（支持环与共享子图）

### 4.9 生命周期与错误

#### 4.9.1 Isolate 状态机

```
        spawn
Created ──────▶ Ready ──────▶ Running ──┬──▶ Paused ──resume──▶ Running
                  ▲                     │
                  └─────────────────────┤
                                        ├──▶ Exiting ──▶ Dead
                   (pause/resume)       │        │
                                        └────────┴── kill / exit / 未捕获错误(errorsAreFatal)
```

| 状态 | 含义 |
|------|------|
| `Created` | 结构已建，尚未入调度 |
| `Ready` | 在调度器的待运行集合里 |
| `Running` | 正在执行 |
| `Paused` | 被 `pause()` 挂起，不处理事件（`ping` 也不响应） |
| `Exiting` | 收到退出请求，正在执行 `finally` / 清理 |
| `Dead` | 已终止，资源待回收 |

#### 4.9.2 退出与错误传播

| 场景 | 行为 |
|------|------|
| 入口方法正常返回 | → `Exiting` → 向所有 `onExit` 端口发 `null`（或 `response`）→ `Dead` |
| `Isolate.exit(port, msg)` | 同步终止；向 `port` 发 `msg`；**不再执行后续代码**（但已注册的清理回调执行） |
| 未捕获 `Error` 且 `errorsAreFatal = true` | 向所有 `onError` 端口发 `[errorValue, null]`（`Error` 值 + 占位栈轨迹）→ 终止 |
| 未捕获 `Error` 且 `errorsAreFatal = false` | 仅向 `onError` 端口发送，**继续运行** |
| `kill(priority: immediate)` | 立即终止，不执行 `finally` |
| `kill(priority: beforeNextEvent)` | 在下一个事件边界终止，**执行** `finally`（推荐默认） |
| `ping(port, response)` | 目标 isolate 收到后立即回发 `response`，用于存活探测 |
| `IsolateGroup.exit()` | 请求组内所有 isolate 退出 |

**约定**：跨 isolate 传递的错误用 SL 既有的 `Error` 值（`Int32 code` + `string message`）承载，不引入异常对象（与 `COROUTINE_DESIGN` §4.9 一致）。栈轨迹一期传 `null`。

#### 4.9.3 Capability

- 结构：进程级全局表 `cap_id → { 计数器/标志 }`，`cap_id` 为**随机基 + 单调自增**的 `u64`，不可预测（不可伪造）。
- 跨 isolate 传递时编码为 `CAPABILITY(u64)`，目标端重建后 `==` 成立（因为 `cap_id` 相同且类型相同）。
- **无能力时静默无效**（对齐 Dart）：`pause()` 时若句柄不含 `pauseCapability`，直接返回，不报错。
- `kill()` 需要 `terminateCapability`；`pause()/resume()` 需要 `pauseCapability`。

***

## 5. 语言层设计（SL API）

### 5.1 新增类型

| 类型 | 位置 | 说明 |
|------|------|------|
| `Isolate` | `Core/Isolate/Isolate.sl` | isolate 句柄（**不可发送**，需拆成 port + capability 传递） |
| `IsolateGroup` | `Core/Isolate/IsolateGroup.sl` | 组句柄 |
| `SendPort` | `Core/Isolate/SendPort.sl` | 发送端 |
| `ReceivePort` | `Core/Isolate/ReceivePort.sl` | 接收端（Stream 风格 `listen`） |
| `RawReceivePort` | `Core/Isolate/RawReceivePort.sl` | 底层接收端（`handler` 回调） |
| `Capability` | `Core/Isolate/Capability.sl` | 能力令牌 |
| `TransferableData` | `Core/Isolate/TransferableData.sl` | 零拷贝转移块 |
| `IsolateStatus` | `Core/Isolate/IsolateStatus.sl` | 状态常量 |
| `IsolateError` | `Core/Isolate/IsolateError.sl` | 错误码常量 |

> 全部为**库类型 + 系统方法**，不引入任何新关键字（与 `Coroutine`/`Channel` 惯例一致）。

### 5.2 `Isolate` API

| 成员 | 签名 | 语义 |
|------|------|------|
| `Isolate.current` | 静态属性 → `Isolate` | 当前 isolate 句柄 |
| `Isolate.spawn0/1/2/3` | `static Isolate spawnN(object entry, object a0, ...)` | 同 group 内创建并启动；入口为**函数变量**（见 5.2.1） |
| `Isolate.run0/1/2/3` | `static object runN(object entry, object a0, ...)` | spawn → 执行 → 取回返回值 → 销毁；异常向调用者传播（**会挂起当前协程**） |
| `Isolate.exit` | `static void exit(SendPort port, object msg)` | 同步终止当前 isolate |
| `iso.pause()` | `Capability pause()` | 请求暂停，返回 `resumeCapability` |
| `iso.resume(cap)` | `void resume(Capability)` | 恢复；capability 不匹配则**静默无效** |
| `iso.kill(int priority)` | `void kill(int)` | `immediate=0` / `beforeNextEvent=1` |
| `iso.ping(port, obj, int priority)` | `void ping(SendPort, object, int)` | 存活探测 |
| `iso.setErrorsFatal(bool)` | `void setErrorsFatal(bool)` |  |
| `iso.addOnExitListener(port, obj)` | `void addOnExitListener(SendPort, object)` |  |
| `iso.addErrorListener(port)` | `void addErrorListener(SendPort)` |  |
| `iso.debugName` | `string` | 调试名 |
| `iso.status` | `IsolateStatus` | 状态 |
| `iso.controlPort` / `pauseCapability` / `terminateCapability` |  | **可发送**，用于重建句柄 |
| `Isolate(controlPort, pauseCap, termCap)` | 构造 | 由 port + capability 重建句柄 |

### 5.2.1 入口：函数变量（三种等价形态）

`spawnN` / `runN` 的第一个参数是**函数值**，不是方法名字符串。语言已支持函数类型（见 `doc/closure-design.md`、`test/BaseTest/CoroutineKeywordTest.sl` K3/K4 组），以下三种写法**等价且都合法**：

| 形态 | 声明写法 | 说明 |
|------|---------|------|
| **宽松函数变量** | `function add = function( int a, int b ) { ret a + b; }` | 类似 `var`，**不做签名检查** |
| **签名函数类型** | `Func<int,int,int> add = add_fn` | C# 风格：**第 1 个模板实参是返回类型**，其后为参数类型；返回类型位置允许 `void` |
| **匿名闭包（直接内联）** | `Isolate.run0( function() { ret 42; } )` | 闭包体就地写在调用点 |

```sl
# 形态一：宽松函数变量
function add = function( int a, int b ) { ret a + b; }
object r1 = Isolate.run2( add, 3, 4 )          # 7

# 形态二：Func<> 签名类型
Func<int,int,int> addTyped = add
object r2 = Isolate.run2( addTyped, 3, 4 )     # 7

# 形态三：匿名闭包内联
object r3 = Isolate.run0( function() { ret 42; } )   # 42

# void 入口
Func<void,int> sink = function( int v ) { g_sum = g_sum + v; }
Isolate.run1( sink, 5 )
```

**保留数字后缀的原因**：语言无函数重载机制，参数个数靠方法名区分（与 `Coroutine.spawn0..3` / `Coroutine.spawnClosure0..3` 同一惯例）。参数仍按 `object` 装箱传递，上限 3 个（`run0..3` / `spawn0..3`）——需要更多参数时请打包成一个可发送容器（如 `List<object>` 或 `data` 类实例）传入。

#### 闭包跨 isolate：捕获环境随闭包一起深拷贝

这是本设计与 Dart 的**关键差异**（Dart 的 `Isolate.spawn` 不接受闭包，只有 `Isolate.run` 接受）。本语言**两者都接受闭包**，规则统一为一条：

> **闭包可发送 ⟺ 其捕获环境（context）中的每一个值都可发送。**

依据现有闭包实现（`csimple_lang/src/vm/runtime/method/runtime_closure_method.h`、`doc/closure-design.md` §3.0）：

- 闭包对象在 C VM 侧是 `VMClosureData { magic, method_id, context }`
  - `method_id`：**合成静态方法的 id 字符串**（`<宿主>_<外层函数>_closure_<序号>_<名>`）
  - `context`：`VMArray*`，元素为 `Object`（标量装箱），按槽位存放被捕获变量
- 前端把闭包降级为"合成静态方法 + context 隐藏首参"，因此**闭包天然可序列化**：

```
发送端：vm_closure_object_try_get_data(obj, &method_id, &ctx)
        → 序列化 method_id（STRING tag）
        → 递归序列化 ctx 的每个槽位（按 4.5.3 白名单）
        → 新 tag：0x33 CLOSURE = STRING method_id + ARRAY context

接收端：反序列化 method_id + 在每个槽位重建对象
        → vm_closure_object_new(method_id, ctx) 重建闭包对象
```

**关键前提**：`method_id` 指向的是**代码**，而组内 isolate **共享代码**（4.3）。因此目标 isolate 用同一个 `method_id` 能解析到同一个合成静态方法——**这是 Isolate Group 存在的又一硬理由**。跨 group 传递闭包需要连同代码一起加载（P3）。

**约束：**

| 场景 | 行为 |
|------|------|
| 闭包未捕获任何变量（context 为空） | 始终可发送 |
| 捕获了标量 / 字符串 / 可发送容器 | 可发送，按深拷贝传递 |
| 捕获了普通类实例 / `Channel` / 协程句柄 / `ReceivePort` | 报 `Error.IsolateNotSendable` |
| 闭包捕获了变量 `x`，worker 里修改 `x` | **只改自己 isolate 的副本**，源 isolate 的 `x` 不变（与 4.5.3 深拷贝语义一致） |
| 闭包捕获了 `this` / 实例成员 / 静态成员 | 第一版闭包机制本就不捕获它们（`closure-design.md` §3.5），故不受影响；这些成员在 worker isolate 中**按 worker 自己的静态影子表解析** |

**编译期检查**：前端在 `spawnN` / `runN` 调用点做**捕获分析**——若闭包捕获列表中存在编译期可判定为不可发送的类型（如 `Channel<T>`、`ReceivePort`、`cor`），直接编译报错，不必等到运行期。

### 5.3 Port API

| 成员 | 签名 | 语义 |
|------|------|------|
| `ReceivePort()` | 构造 | 创建接收端，自动生成 `SendPort` |
| `rp.sendPort` | `SendPort` | 对应的发送端 |
| `rp.listen(handler)` | `void` | 注册消息处理器（沿用 SL 既有回调约定） |
| `rp.recv()` | `object` | **阻塞当前协程**直到收到一条消息（可在协程内使用） |
| `rp.tryRecv()` | `object` | 非阻塞；无消息返回 `null` |
| `rp.close()` | `void` | 关闭；已入队消息仍可取出，之后 `recv` 返回 `null` |
| `sp.send(obj)` | `void` | **异步、永不阻塞**；对象必须可发送 |
| `RawReceivePort()` / `handler` / `sendPort` / `close()` | | 底层版本 |
| `SendPort` 的 `==` | | 跨 isolate 保持相等 |

### 5.4 `IsolateGroup` API

| 成员 | 签名 | 语义 |
|------|------|------|
| `IsolateGroup.current` | 静态属性 | 当前 isolate 所属组 |
| `isoGroup.exit()` | `void` | 请求组内全部 isolate 退出 |
| `isoGroup.isolateCount` | `int` | 组内 isolate 数（诊断用） |
| `isoGroup.id` | `Int64` | 组 id |

### 5.5 状态与错误常量

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

public class IsolateError extends Object
{
    public static const Int32 None                 = 0
    public static const Int32 SpawnFailed          = 1   # 入口非法（非函数值 / method_id 解析失败）/ 资源不足
    public static const Int32 NotSendable          = 2   # 消息含不可发送对象
    public static const Int32 CyclicMessage        = 3   # 消息图含环（一期不支持）
    public static const Int32 TransferInvalid      = 4   # 已转移的 TransferableData 被再次使用
    public static const Int32 InvalidHandle        = 5   # port / capability / isolate 句柄无效
    public static const Int32 PortClosed           = 6   # 向已关闭的 port 发送
    public static const Int32 IsolateDead          = 7   # 目标 isolate 已终止
    public static const Int32 PermissionDenied     = 8   # capability 不匹配
}
```

**C 侧错误码**（沿用 `coroutine_system_method.c` 的负数约定：`-62` spawn 失败、`-63` cancelled、`-64` InvalidOperation）：

| 错误 | C 码 |
|------|------|
| `IsolateError.SpawnFailed` | `-70` |
| `IsolateError.NotSendable` | `-71` |
| `IsolateError.CyclicMessage` | `-72` |
| `IsolateError.TransferInvalid` | `-73` |
| `IsolateError.InvalidHandle` | `-74` |
| `IsolateError.PortClosed` | `-75` |
| `IsolateError.IsolateDead` | `-76` |

### 5.6 示例

#### 5.6.1 一次性计算（`Isolate.run`）

```sl
Worker
{
    static fun()
    {
        # 函数变量作为入口（宽松类型）
        function isoHeavyAdd2 = function( int a, int b )
        {
            # 在 worker isolate 中执行；此处的静态字段与主 isolate 相互独立
            ret a + b
        }

        object r = Isolate.run2( isoHeavyAdd2, 3, 4 )
        global.println( "r = " + ( r as int ).toString() )    # 7

        # 等价写法：Func<> 签名类型
        Func<int,int,int> typed = isoHeavyAdd2
        object r2 = Isolate.run2( typed, 3, 4 )               # 7

        # 等价写法：匿名闭包内联
        object r3 = Isolate.run0( function() { ret 42; } )    # 42
    }
}
```

#### 5.6.2 长生命周期 worker + 双向端口

```sl
IsoWorker
{
    static SendPort g_cmd      # worker isolate 自己的静态字段（与主 isolate 不共享）
    static ReceivePort g_rp

    static fun()
    {
        # ── worker 侧入口：函数变量 ──
        Func<void,SendPort> isoMain = function( SendPort mainPort )
        {
            IsoWorker.g_rp = ReceivePort()
            mainPort.send( IsoWorker.g_rp.sendPort )     # 回传 sendPort，建立反向通道

            while ( true )
            {
                object msg = IsoWorker.g_rp.recv()       # 阻塞当前协程（不是阻塞线程）
                if ( msg == null ) { break }             # port 已关闭

                string text = msg as string
                if ( text == "shutdown" )
                {
                    IsoWorker.g_rp.close()
                    break
                }
                mainPort.send( text + "!" )
            }
        }

        # ── 主 isolate 侧 ──
        ReceivePort rp = ReceivePort()
        Isolate iso = Isolate.spawn1( isoMain, rp.sendPort )

        # 第一条消息：worker 回传的 SendPort
        SendPort worker = rp.recv() as SendPort

        worker.send( "hello" )
        global.println( rp.recv() as string )        # "hello!"

        worker.send( "shutdown" )
        rp.close()
    }
}
```

> 注意：`isoMain` 闭包**没有捕获任何变量**（context 为空），因此无条件可发送。若它捕获了主 isolate 的局部变量，那些值会被深拷贝进 worker（见 5.2.1）。

#### 5.6.3 静态字段隔离

```sl
Counter
{
    static int g_value = 0

    static fun()
    {
        Func<int> isoBump = function()
        {
            Counter.g_value = Counter.g_value + 100  # 只影响本 isolate
            ret Counter.g_value
        }

        object a = Isolate.run0( isoBump )           # worker isolate: 0 + 100 = 100
        global.println( ( a as int ).toString() )    # 100
        global.println( Counter.g_value.toString() ) # 0  ← 主 isolate 不受影响
    }
}
```

#### 5.6.4 零拷贝转移大块数据

```sl
static fun()
{
    Func<int,TransferableData> isoProcessBytes = function( TransferableData td )
    {
        ByteArray bytes = td.materialize()      # 在 worker 堆中物化
        ret bytes.length()
    }

    ByteArray big = ByteArray( 1024 * 1024 )
    TransferableData td = TransferableData.fromBytes( big )
    object n = Isolate.run1( isoProcessBytes, td )
    global.println( ( n as int ).toString() )    # 1048576
    # td 已转移，此处再使用 td 会抛 IsolateError.TransferInvalid
}
```

### 5.7 编译期 / 运行期限制

**编译期**：

1. `Isolate.spawnN` / `runN` 的第一个参数必须是**函数值**——即函数变量（`function` 声明）、函数类型变量（`Func<>` 声明）或匿名闭包。传入字符串字面量或其它非函数值一律**编译期报错**。
2. **不再需要**"方法名全工程唯一"约束：闭包入口由 `method_id` 精确定位到前端合成出的静态方法，不存在按名解析的歧义（这消除了 `md/syntax/coroutine.md` §1.1 第 1 条所描述的 `Coroutine.spawnN` 历史包袱）。
3. **捕获分析**：闭包入口若捕获了编译期可判定的不可发送类型（`Channel<T>`、`ReceivePort`、`cor` 句柄、普通类实例），编译期直接报错。运行期才能确定的（`object` 动态类型、容器元素）留到发送时校验。
4. 实参个数必须与 `runN` / `spawnN` 的 `N` 一致，且能隐式装箱为 `object`（沿用现有调用参数检查）。

**运行期**：

1. **静态初始化器（子 VM）内禁止**调用任何 isolate API —— 与现有"子 VM 禁止协程 API"一致。
2. 向已关闭的 port 发送 → `Error.IsolatePortClosed`。
3. 发送不可发送对象（含**捕获了不可发送值的闭包**）→ `Error.IsolateNotSendable`（**在发送方抛出，不污染目标 isolate**）。
4. `Isolate` 句柄本身不可发送 → `Error.IsolateNotSendable`；请发送 `controlPort` + capabilities。
5. 入口值不是合法闭包对象（无 `VM_CLOSURE_MAGIC` 标记）或 `method_id` 在目标 group 中解析不到 → `Error.IsolateSpawnFailed`。

***

## 6. VM 运行时设计（C VM）

### 6.1 新增源文件

```
csimple_lang/src/vm/
├── runtime/
│   └── isolate/
│       ├── vm_isolate.h / .c          isolate 生命周期 / 句柄表 / 状态机
│       ├── vm_isolate_group.h / .c    group 生命周期 / 成员表
│       ├── vm_static_shadow.h / .c    ★ 静态字段影子表
│       ├── vm_message.h / .c          序列化 / 反序列化 / 可发送性校验（含 CLOSURE tag）
│       └── vm_transfer.h / .c         TransferableData 所有权转移
├── port/
│   ├── vm_port.h / .c                 ★ 公共端口骨架（Channel 与 Port 共用）
│   └── vm_port_table.h / .c           进程级 port_id → isolate 路由表
└── system_method_call/
    ├── isolate_system_method.h / .c   SystemIsolate*/SystemPort*/SystemTransfer* 系统调用
```

`src/base/thread/`（P2）：

```
sl_thread.h/.c    sl_mutex.h/.c    sl_cond.h/.c    sl_atomic.h/.c    sl_tls.h/.c
```

### 6.2 `VMPort`：公共端口骨架

文件：`csimple_lang/src/vm/port/vm_port.h`

```c
typedef enum
{
    VM_PORT_MODE_LOCAL   = 0,   /* Channel<T>：同 VM，传引用，有背压 */
    VM_PORT_MODE_ISOLATE = 1    /* SendPort/ReceivePort：跨 VM，深拷贝，无背压 */
} VMPortMode;

typedef struct VMPortMessage            /* isolate 模式：纯字节消息 */
{
    uint8*  data;
    int32   size;
    struct VMPortMessage* next;
} VMPortMessage;

typedef struct VMPortWaiter             /* local 模式：挂起的协程 */
{
    VMCoroutine* coro;
    struct VMPortWaiter* next;
} VMPortWaiter;

typedef struct VMPort
{
    int64           id;                 /* 进程唯一 */
    VMPortMode      mode;
    int32           capacity;           /* local: <=0 无上限；isolate: 忽略（无上限） */
    uint8           closed;

    /* ── 缓冲（二选一，按 mode） ── */
    VMRuntimeValue* value_buf;          /* LOCAL：直接存 VMRuntimeValue（引用语义） */
    int32           value_count, value_cap;
    VMPortMessage*  msg_head, * msg_tail;   /* ISOLATE：字节消息 FIFO */
    int32           msg_count;

    /* ── 等待者 ── */
    VMPortWaiter*   send_q;             /* 仅 LOCAL（背压） */
    VMPortWaiter*   recv_q;             /* LOCAL + ISOLATE（P1 同线程时） */

    /* ── 归属 ── */
    struct _VMIsolate* owner;           /* ISOLATE 模式：拥有该 port 的 isolate */

    struct VMPort*  next;               /* 注册表链表（per-VM 或 per-isolate） */
} VMPort;
```

**统一操作（两种模式共用）：**

```c
VMPort*  vm_port_create(VM* vm, VMPortMode mode, int32 capacity);
VMPort*  vm_port_find(VM* vm, int64 id);
void     vm_port_close(VMPort* p);                 /* 唤醒全部等待者 */
int32    vm_port_count(const VMPort* p);
int32    vm_port_is_closed(const VMPort* p);
void     vm_port_wake_one_recv(VM* vm, VMPort* p); /* 唤醒规则：入队后唤醒一个接收者 */
void     vm_port_wake_one_send(VM* vm, VMPort* p); /* 出队后唤醒一个发送者（仅 LOCAL） */
void     vm_port_wake_all(VM* vm, VMPort* p);
void     vm_port_registry_clear(VM* vm);           /* VM 销毁时 */
```

**模式差异（唯一需要分叉的地方）：**

| 操作 | `VM_PORT_MODE_LOCAL`（`Channel<T>`） | `VM_PORT_MODE_ISOLATE`（Port） |
|------|--------------------------------------|-------------------------------|
| 入队 | 直接拷 `VMRuntimeValue`（浅拷贝，共享引用） | 先序列化成字节 blob 再入队 |
| 出队 | 直接取 `VMRuntimeValue` | 取 blob → 反序列化 → 目标堆 |
| 满时 | 挂起发送协程（背压） | **永不阻塞**：无上限队列 |
| 空时 | 挂起接收协程 | 挂起接收协程（P1）/ 阻塞事件循环（P2） |
| 唤醒 | 操作同 VM 协程队列 | 入队后通知目标 isolate 的消息泵 |
| 关闭后 `send` | 抛异常 | 抛 `Error.IsolatePortClosed` |
| 关闭后 `recv` | 返回 `null` | 取完残留后返回 `null` |

### 6.3 `Channel<T>` 的迁移

迁移后 `coroutine_system_method.c` 里的 `VMChannel` / `s_channel_head` **整体删除**，改为：

```c
int32 vm_sys_channel_create(VM* vm, int32 param_count)
{
    int32 capacity = /* pop */;
    VMPort* p = vm_port_create(vm, VM_PORT_MODE_LOCAL, capacity);
    push_i64(p->id);
    return TRUE;
}
```

行为**逐字节不变**（同 VM、传引用、有背压、关闭后 recv 返回 null）。这样：

- 现有 `Channel<T>` 的 SL 代码与 `CoroutineTest.sl` 全部用例**不需要任何修改**。
- 注册表从"进程全局 static"变为 **per-VM**（顺手修掉一个跨 VM 隐患）。
- `Channel` 与 `Port` 共享同一套 FIFO / 关闭 / 唤醒代码，只维护一份。

> **注意**：迁移必须保证 `vm_sys_channel_registry_clear()` 的调用点（`vm_runtime.c` 的 `vm_destroy` 路径）改为 `vm_port_registry_clear(vm)`。

### 6.4 系统方法清单

文件：`csimple_lang/src/vm/system_method_call/isolate_system_method.h/.c`

| 系统方法 | 签名（SL 侧） | C 实现 |
|---------|--------------|--------|
| `SystemIsolateCurrent` | `Isolate current()` | `vm_sys_isolate_current` |
| `SystemIsolateSpawn0..3` | `Isolate spawnN(object entry, object...)` | `vm_sys_isolate_spawn0..3` |
| `SystemIsolateRun0..3` | `object runN(object entry, object...)` | `vm_sys_isolate_run0..3` |
| `SystemIsolateExit` | `void exit(SendPort, object)` | `vm_sys_isolate_exit` |
| `SystemIsolatePause` | `Capability pause(Int64)` | `vm_sys_isolate_pause` |
| `SystemIsolateResume` | `void resume(Int64, Int64)` | `vm_sys_isolate_resume` |
| `SystemIsolateKill` | `void kill(Int64, Int32)` | `vm_sys_isolate_kill` |
| `SystemIsolatePing` | `void ping(Int64, Int64, object, Int32)` | `vm_sys_isolate_ping` |
| `SystemIsolateSetErrorsFatal` | `void setErrorsFatal(Int64, bool)` | `vm_sys_isolate_set_errors_fatal` |
| `SystemIsolateAddOnExitListener` | `void addOnExitListener(Int64, Int64, object)` | `vm_sys_isolate_add_on_exit_listener` |
| `SystemIsolateAddErrorListener` | `void addErrorListener(Int64, Int64)` | `vm_sys_isolate_add_error_listener` |
| `SystemIsolateStatus` | `Int32 status(Int64)` | `vm_sys_isolate_status` |
| `SystemIsolateNew` | `Isolate new(Int64, Int64, Int64)` | `vm_sys_isolate_new` |
| `SystemIsolateGroupCurrent` | `IsolateGroup current()` | `vm_sys_isolate_group_current` |
| `SystemIsolateGroupExit` | `void exit(Int64)` | `vm_sys_isolate_group_exit` |
| `SystemIsolateGroupCount` | `Int32 isolateCount(Int64)` | `vm_sys_isolate_group_count` |
| `SystemReceivePortCreate` | `Int64 create()` | `vm_sys_receive_port_create` |
| `SystemPortSendPort` | `Int64 sendPort(Int64)` | `vm_sys_port_send_port` |
| `SystemPortSend` | `void send(Int64, object)` | `vm_sys_port_send` |
| `SystemPortRecv` | `object recv(Int64)` | `vm_sys_port_recv` |
| `SystemPortTryRecv` | `object tryRecv(Int64)` | `vm_sys_port_try_recv` |
| `SystemPortClose` | `void close(Int64)` | `vm_sys_port_close` |
| `SystemPortCount` | `Int32 count(Int64)` | `vm_sys_port_count` |
| `SystemPortIsClosed` | `bool isClosed(Int64)` | `vm_sys_port_is_closed` |
| `SystemTransferFromBytes` | `Int64 fromBytes(object)` | `vm_sys_transfer_from_bytes` |
| `SystemTransferMaterialize` | `object materialize(Int64)` | `vm_sys_transfer_materialize` |
| `SystemTransferSize` | `Int32 size(Int64)` | `vm_sys_transfer_size` |
| `SystemTransferIsValid` | `bool isValid(Int64)` | `vm_sys_transfer_is_valid` |

注册方式（与现有完全一致，见 `source/Front/Lib/Core/Core.jsonc`）：

```jsonc
// ---- Isolate native calls (Isolate/*.sl) ----
{ "name": "SystemIsolateCurrent", "returnType": "Int64", "params": [], "isVariadic": false, "cvmFunction": "vm_sys_isolate_current" },
{ "name": "SystemPortSend",       "returnType": "void",  "params": ["Int64", "object"], "isVariadic": false, "cvmFunction": "vm_sys_port_send" },
// ...
```

同时在 `Core.jsonc` 的 `files` 列表加入 `Isolate/*.sl`，在 `struct` 的 `children` 中注册 `Isolate`、`IsolateGroup`、`SendPort`、`ReceivePort`、`RawReceivePort`、`Capability`、`TransferableData`、`IsolateStatus`、`IsolateError`。

### 6.5 入口函数值的执行路径（`spawnN` / `runN`）

**与 `Coroutine.spawnN(string)` 的根本区别**：入口不再是"按名字在方法表里查"，而是**一个闭包对象**，由 `vm_closure_object_try_get_data` 取出 `{ method_id, context }` 后直接定位合成静态方法——**没有名字解析，没有歧义，不需要全工程唯一**。

实现直接复用现有闭包协程的成熟路径 `vm_sys_coroutine_spawn_closure_impl`（`coroutine_system_method.c`）：它已经能从闭包对象取出 `method_id` + `context`，并把 `context` 作为被调方的**隐藏 Argument 0**（与 `OpCode_CallClosure` 完全一致）。isolate 版本只需在中间插入"序列化 / 反序列化"。

```c
/* isolate_system_method.c —— spawnN / runN 共用 */
static int32 vm_sys_isolate_launch_impl(VM* vm, int32 param_count, int32 wait_for_result)
{
    VMRuntimeValue entry_val;
    const char*    method_id = NULL;
    VMArray*       ctx       = NULL;

    /* 1. 取入口值并校验是闭包对象 */
    if (!vm_try_pop_runtime_value(vm, &entry_val))            { spawn_failed(); return TRUE; }
    if (!vm_closure_object_try_get_data(entry_val.vm_object, &method_id, &ctx))
    {
        vm_isolate_throw(vm, ISO_ERR_SPAWN_FAILED, "Isolate entry is not a function value");
        return TRUE;
    }

    /* 2. 序列化入口闭包（CLOSURE tag：method_id + context 深拷贝校验）
          —— 校验失败在此处抛 NotSendable，不会污染新 isolate */
    uint8* entry_blob; int32 entry_size;
    if (!vm_isolate_serialize_entry(vm, method_id, ctx, &entry_blob, &entry_size))
    {
        vm_isolate_throw(vm, ISO_ERR_NOT_SENDABLE, "Isolate entry captures non-sendable value");
        return TRUE;
    }

    /* 3. 逐个序列化实参（0..3 个），同样走 4.5.3 白名单 */
    /*    ... arg_blob[] ... */

    /* 4. 创建 isolate：新 VM + 静态影子表，加入当前 group（共享代码） */
    VMIsolate* iso = vm_isolate_create(vm->isolate->group, debug_name);
    if (iso == NULL) { spawn_failed(); return TRUE; }

    /* 5. 在目标 isolate 的堆里重建闭包与实参，装配成"入口根协程" */
    VMObject* entry2 = vm_isolate_deserialize_entry(iso->vm, entry_blob, entry_size);
    vm_isolate_prepare_entry_coroutine(iso->vm, entry2, arg_blob, param_count - 1);

    /* 6. runN：挂起当前协程等待完成，结果回来后 push 返回值；
          spawnN：直接入就绪队列，返回 isolate 句柄 */
    if (wait_for_result) { ... vm_coroutine_suspend_current(vm, CORO_BLOCK_ISOLATE_RUN, TRUE, FALSE); }
    else                 { vm_isolate_enqueue_ready(iso); vm_eval_push_i64(vm, iso->id); }

    return TRUE;
}
```

**要点：**

| # | 说明 |
|---|------|
| 1 | 入口校验失败 → `Error.IsolateSpawnFailed`；**捕获环境不可发送** → `Error.IsolateNotSendable`，**在发送方抛出**，新 isolate 不会被创建 |
| 2 | 目标 isolate 里 `method_id` 必须能解析到方法——这依赖 **group 内共享代码**（4.3）。跨 group 传闭包会在这一步失败（P3 才支持） |
| 3 | `context` 数组按元素逐个深拷贝；标量槽位按 tag 还原其原始窄类型（`Int8`/`Float16` 等） |
| 4 | 新 isolate 的入口根协程由 `vm_coroutine_create` 建立，`context` 作为隐藏 Argument 0，与 `OpCode_CallClosure` 调用约定一致 |
| 5 | `runN` 的"取回返回值 + 传播异常"复用 `COROUTINE_DESIGN` §4.11 的 waiter 完成值机制，只是等待对象从 `VMCoroutine` 换成 `VMIsolate` |

### 6.6 `ReceivePort.recv()` 与调度器集成

沿用 `COROUTINE_DESIGN` §4.6.4 的 **Option A 挂起协议**（peek 参数 → 挂起 + `reexecute=TRUE` → 恢复后重查）：

```c
int32 vm_sys_port_recv(VM* vm, int32 param_count)
{
    VMCoroutine* cur = vm_coro_cur(vm);
    int64 pid; VMPort* p;

    if (cur != NULL && !vm_coro_check_cancel(vm)) { return TRUE; }

    if (!vm_coro_peek_i64_at(vm, 0, &pid)) { invalid_op("PortRecv: missing port"); return TRUE; }
    p = vm_port_find(vm, pid);
    if (p == NULL || p->owner != vm->isolate) { invalid_op("PortRecv: invalid port"); return TRUE; }

    if (p->msg_count > 0)
    {
        /* 缓冲区有消息：反序列化到当前 isolate 的堆 */
        VMPortMessage* m = vm_port_dequeue_msg(p);
        vm_wake_one_send_if_local(vm, p);
        VMRuntimeValue v;
        if (!vm_isolate_deserialize(vm, m->data, m->size, &v)) { /* 报 Error */ }
        vm_port_msg_free(m);
        vm_coro_pop_i64(vm, &pid);
        push_value(vm, &v);
        return TRUE;
    }

    if (p->closed)
    {
        vm_coro_pop_i64(vm, &pid);
        push_null(vm);                       /* 关闭且缓冲空 → null（同 Channel 语义） */
        return TRUE;
    }

    if (cur == NULL) { invalid_op("PortRecv: no coroutine context"); return TRUE; }

    vm_port_waiter_push(&p->recv_q, cur);
    vm_coroutine_suspend_current(vm, CORO_BLOCK_ISOLATE_MSG, TRUE, FALSE);
    return TRUE;
}
```

- 新增挂起原因 `CORO_BLOCK_ISOLATE_MSG = 6`（追加到 `vm_coroutine.h` 的宏定义与 `CoroutineBlockReason` 枚举）。
- **P1**：消息入队后，直接把目标协程放回当前（唯一的）调度器就绪队列。
- **P2**：消息入队后 `sl_cond_signal(target->msg_cond)`，由目标 isolate 自己的线程唤醒其协程。

### 6.7 消息泵（事件循环集成）

```c
/* 每个 isolate 的每轮调度都要跑一次消息泵 */
void vm_isolate_pump(VMIsolate* iso)
{
    VM* vm = iso->vm;
    for (;;)
    {
        VMPortMessage* m = vm_isolate_dequeue(iso);   /* 加锁 */
        if (m == NULL) { break; }

        VMPort* p = vm_port_lookup(iso, m->port_id);
        if (p == NULL || p->closed) { vm_port_msg_free(m); continue; }

        vm_port_enqueue_msg(p, m);
        /* 唤醒一个阻塞在 recv 上的协程（P1 同线程；P2 为本线程安全） */
        vm_port_wake_one_recv(vm, p);
        /* 触发已注册的 Stream handler（RawReceivePort.handler / listen 回调） */
        vm_port_dispatch_handler(vm, p);
    }
}
```

**插入位置**：

- P1：进程级调度循环 `vm_isolate_scheduler_run_all()` 每轮先对所有活着的 isolate 跑一次 pump，再调度各自的就绪协程。
- P2：各 isolate 自己的 `vm_scheduler_enter` 循环内，在"取就绪协程"之前跑 pump。

### 6.8 GC 交互

- **不变**：GC 依然 per-VM（`vm_gc_collect(vm)`），只扫自己 isolate 的堆。isolate 越多，GC 越独立——这正是隔离模型的优势。
- **新增根**：
  - 静态影子表 `vm->static_shadow` 中所有 `member_runtime_objects` 的 `object_ref`（**必须加入根集合，否则静态字段引用的对象会被误回收**）
  - `VMPort` 的 `value_buf`（LOCAL 模式）中的 PTR/STRING 槽（对应今天的 `VMChannel.buf`，今天**未被 GC 扫描**——这是一个**现存 bug**，迁移时一并修掉）
  - 反序列化中途构造的对象（在 `vm_isolate_deserialize` 期间，临时对象必须注册为手动对象或加入根）
- **禁止**：GC 不得扫描其它 isolate 的堆（不变式 I1）。
- **P2**：各 isolate 独立 GC，**无需全局 STW**。

### 6.9 静态字段访问路径改造（代码级）

| 文件 | 位置 | 改动 |
|------|------|------|
| `vm/runtime/vm_runtime.c` | `OpCode_LoadStaticField`（约 3405 行） | `rt` 解析后改为 `VMStaticShadowEntry* e = vm_static_shadow_get_or_create(vm, rt);`，从 `e` 读 |
| 同上 | `OpCode_StoreStaticField`（约 3236 行） | 同上，写入 `e` |
| 同上 | `OpCode_LoadGlobal` / `OpCode_StoreGlobal`（约 1393 / 1424 行） | 走 `vm_runtime_manager_load_global` / `store_global`，内部改为影子表 |
| 同上 | `vm_runtime_type_ensure_static_expr_initialized` | `s_applied_keys` / `s_applying_keys` 移入 `VM` |
| `vm/runtime/vm_runtime_manager.c` | `s_global_entries` / `s_global_init_instructions` / `s_is_global_init_applied` | **过程级 → per-VM**（移入 `VM` 或 isolate 结构） |
| `vm/runtime/vm_runtime.h` | `VM` 结构 | 新增 `isolate` / `static_shadow` / `static_applied_keys` / `static_applying_keys` |
| 新增 | `vm/runtime/isolate/vm_static_shadow.h/.c` | 影子表实现 |

**兼容保证**：`vm->static_shadow == NULL`（子 VM / 编译期 VM / 未启用 isolate）时，所有访问**退化为直接读写 `RuntimeType`**，行为与今天完全一致。

***

## 7. `Channel<T>` 的处置方案（决策论证）

用户提问：*"能不能用继承扩展用，不能用，就新建一个数据传输类"*。

### 7.1 结论

**不继承，新建并列类型；但 C 层抽取公共骨架复用。**

### 7.2 为什么不能继承

`Channel<T>` 与 `SendPort` 看似都是"一端进一端出的管道"，但语义差异是**本质性**的：

| 维度 | `Channel<T>` | `SendPort` | 继承是否可行 |
|------|-------------|-----------|-------------|
| 传递语义 | **共享引用**（零拷贝） | **深拷贝** | ❌ 子类**改变**了可观测行为 |
| `send` 阻塞性 | 缓冲满时**阻塞**发送协程（背压） | **永不阻塞**（异步） | ❌ 行为不同 |
| 元素类型约束 | **任意 `T`** | **仅白名单类型** | ❌ 子类**收窄**了前置条件 → **违反 LSP** |
| 作用域 | 同 VM 内 | 跨 VM | ❌ 前置条件不同 |
| 关闭后 `send` | 抛异常 | 抛 `PortClosed` | ✅ 可对齐 |
| 关闭后 `recv` | 返回 `null` | 返回 `null` | ✅ 可对齐 |
| 空时 `recv` | 挂起协程 | 挂起协程 | ✅ 可对齐 |

**里氏替换的致命一击**：`Channel<T>` 的契约是"**任意 `T` 都能收发**"。若 `SendPort extends Channel<object>`，则任何接受 `Channel<object>` 的代码传入 `SendPort` 后，发送一个普通类实例就会抛 `NotSendable`——**子类拒绝了父类接受的输入**，这是教科书级的 LSP 违规（前置条件不能被子类强化）。

同时，`send` 从"可能阻塞"变成"永不阻塞"是**放宽**后置条件（技术上合法），但"传引用"变成"传拷贝"是**强化**后置条件（调用方若依赖"我改了对象对方能看到"就会静默出错）——这属于最危险的一类继承：**能编译、能运行、但结果悄悄变错**。

### 7.3 采用方案：抽取公共骨架 + 两种模式

```
                        ┌─────────────────────────┐
                        │  VMPort  (C 层公共骨架)  │
                        │  · id / closed / count   │
                        │  · FIFO 缓冲             │
                        │  · 等待者队列 + 唤醒规则  │
                        │  · 注册表（per-VM）      │
                        └───────┬─────────┬───────┘
                                │         │
              VM_PORT_MODE_LOCAL│         │VM_PORT_MODE_ISOLATE
                                ▼         ▼
                    ┌───────────────┐  ┌────────────────────┐
                    │ SL: Channel<T>│  │ SL: SendPort       │
                    │    （不变）    │  │      ReceivePort   │
                    │               │  │      RawReceivePort│
                    │ 传引用 · 有背压│  │ 深拷贝 · 无背压    │
                    └───────────────┘  └────────────────────┘
                          ▲                     ▲
                          │                     │
                     并列关系，互不为子类（SL 层）
```

**收益：**

1. **C 层去重**：FIFO 缓冲、关闭语义、唤醒规则、句柄注册表——全部只维护一份（`vm_port.c`）。
2. **现有 `Channel<T>` 零改动**：语义逐字节不变，`CoroutineTest.sl` 的 F 组用例全绿通过。
3. **语义清晰**：SL 层两个类型各自表达准确的契约，没有"继承来的诡异行为"。
4. **顺手修两个现存隐患**：Channel 注册表从进程全局变为 per-VM；Channel 的 `value_buf` 纳入 GC 根集合。
5. **未来可扩展**：`VM_PORT_MODE_*` 天然支持第三种模式（如"组内共享端口"P3）。

### 7.4 被否决的备选方案

| 方案 | 否决理由 |
|------|---------|
| A. `SendPort extends Channel<object>` | LSP 违规（见 7.2）；且 `Channel<T>` 的 `T` 与白名单无法统一 |
| B. 直接改造 `Channel<T>` 使其支持跨 VM | 会破坏现有 F 组全部用例（背压语义改变）；共享引用与深拷贝无法在同一 API 下共存 |
| C. 完全另起炉灶，不复用任何 Channel 代码 | FIFO / 关闭 / 唤醒逻辑要重复实现一遍，且要维护两套注册表 |

***

## 8. 分阶段实施计划

每个阶段必须通过**现有全部测试 + 新增验收用例**再进入下一阶段。

### P1：单线程协程式隔离（隔离语义完整，无线程、无锁）

| # | 任务 | 改动 | 验收 |
|---|------|------|------|
| 1 | `VMPort` 公共骨架落地 | 新增 `vm/port/vm_port.h/.c` | 编译通过 |
| 2 | `Channel<T>` 迁移到 `VMPort`（LOCAL 模式），删除旧 `VMChannel` | `coroutine_system_method.c`、`vm_runtime.c` 的 `vm_sys_channel_registry_clear` 调用点 | **F 组用例全绿，行为逐字节不变** |
| 3 | **静态影子表** | 新增 `vm/runtime/isolate/vm_static_shadow.h/.c`；改造 4 个 opcode + `vm_runtime_manager` | 现有全部测试通过（影子表为 NULL 时退化路径） |
| 4 | `VMIsolate` / `VMIsolateGroup` 骨架 + 句柄表 | 新增 `vm/runtime/isolate/vm_isolate.h/.c`、`vm_isolate_group.h/.c` | C 级单测 |
| 5 | 消息序列化 / 反序列化 + 可发送性校验 | 新增 `vm/runtime/isolate/vm_message.h/.c` | C 级单测（各类 tag round-trip、白名单拒绝） |
| 6 | `SendPort` / `ReceivePort` 系统方法 | 新增 `isolate_system_method.h/.c` | A、B 组用例 |
| 7 | 消息泵与协程集成（`CORO_BLOCK_ISOLATE_MSG`） | `vm_coroutine.h` 追加挂起原因、`vm_isolate_pump` | B、C 组用例 |
| 8 | `Isolate.spawn0..3` / `run0..3` / `exit` | `vm_isolate.c` | A、D 组用例 |
| 9 | 生命周期控制：pause / resume / kill / ping / onExit / onError / Capability | `vm_isolate.c` | D、E 组用例 |
| 10 | `TransferableData` | `vm/runtime/isolate/vm_transfer.h/.c` | F 组用例 |
| 11 | GC 根集合扩展（影子表 + port 的 value_buf） | `vm/memory/vm_gc.c` | G 组用例 |
| 12 | SL 层类型与 `Core.jsonc` 注册 | `source/Front/Lib/Core/Isolate/*.sl`、`Core.jsonc` | 全部用例 |

### P2：真并行（1:1 线程）

| # | 任务 |
|---|------|
| 1 | 新增 `src/base/thread/`（thread / mutex / cond / atomic / TLS），Windows + POSIX 双实现 |
| 2 | 共享只读结构**加载期冻结**；`s_method_code_cache` 改为只读 + 构建期锁 |
| 3 | 端口路由表、Channel 注册表、全局 id 分配器加锁；日志输出加锁 |
| 4 | 各 isolate 独立线程 + 独立 `vm_scheduler_enter`；`vm_isolate_join` |
| 5 | 消息队列改用 `sl_mutex` + `sl_cond` |
| 6 | P1 全部用例在多线程下重跑（语义必须一致） |

### P3：传输与加载优化

| # | 任务 |
|---|------|
| 1 | `Isolate.spawnUri` / 跨 group spawn（运行时加载新 SLIR） |
| 2 | 组内共享不可变字符串驻留 |
| 3 | 消息图 `REF_BACKREF`（支持环与共享子图） |
| 4 | `TransferableData` 大块零拷贝（共享内存页） |
| 5 | `@sendable` 标注与编译期校验 |

***

## 9. 测试用例（验收标准）

沿用 `COROUTINE_DESIGN` §8 的测试框架约定：

```sl
void require( bool cond, string name ) throws
{
    if ( !cond ) { throw Error.AssertFailed }
}
```

### A 组：spawn 与 run

```sl
# A1 Isolate.run 取回返回值（函数变量入口）
void test_isolate_run()
{
    function isoAdd2 = function( int a, int b ) { ret a + b; }
    object r = Isolate.run2( isoAdd2, 3, 4 )
    require( r as int == 7, "A1 Isolate.run 返回值" )
}

# A1b 三种入口形态等价
void test_isolate_entry_forms()
{
    function add = function( int a, int b ) { ret a + b; }

    require( Isolate.run2( add, 1, 1 ) as int == 2, "A1b function 变量" )

    Func<int,int,int> typed = add
    require( Isolate.run2( typed, 1, 1 ) as int == 2, "A1b Func<> 类型变量" )

    require( Isolate.run0( function() { ret 42; } ) as int == 42, "A1b 匿名闭包内联" )
}

# A2 spawn + 端口双向通信
static void test_isolate_spawn_ports()
{
    Func<void,SendPort> workerEntry = function( SendPort mainPort )
    {
        ReceivePort wrp = ReceivePort()
        mainPort.send( wrp.sendPort )
        while ( true )
        {
            object msg = wrp.recv()
            if ( msg == null ) { break }
            mainPort.send( ( msg as string ) + "!" )
        }
        wrp.close()
    }

    ReceivePort rp = ReceivePort()
    Isolate iso = Isolate.spawn1( workerEntry, rp.sendPort )
    SendPort worker = rp.recv() as SendPort
    worker.send( "hello" )
    require( rp.recv() as string == "hello!", "A2 双向端口" )
    rp.close()
}

# A3 入口不是函数值 → SpawnFailed（编译期即可拦截；此处验证运行期兜底）
object g_badEntry = null
static void test_isolate_spawn_not_function()
{
    g_badEntry = 42                 # 非函数值
    bool threw = false
    label b { try Isolate.spawn0( g_badEntry ) } catch { threw = true }
    require( threw, "A3 非函数值入口报 SpawnFailed" )
}

# A3b 闭包捕获了不可发送值 → NotSendable
static void test_isolate_entry_bad_capture()
{
    Channel<object> ch = Channel<object>.create( 4 )
    function bad = function() { ch.send( 1 ) ; ret 0; }   # 捕获了 Channel
    bool threw = false
    label b { try Isolate.run0( bad ) } catch { threw = true }
    require( threw, "A3b 捕获不可发送值报错" )
    ch.close()
}

# A4 void 入口：run 返回 null
static void test_isolate_void()
{
    Func<void> isoVoid = function() { }
    require( Isolate.run0( isoVoid ) == null, "A4 void isolate" )
}

# A5 Isolate.current 与 status
static void test_isolate_current()
{
    Isolate self = Isolate.current
    require( self != null && self.status != IsolateStatus.Dead, "A5 current" )
}

# A6 闭包捕获标量：worker 改的是自己的副本，源不变
static void test_isolate_capture_copy()
{
    int base = 10
    function bump = function() { base = base + 100 ; ret base; }
    require( Isolate.run0( bump ) as int == 110, "A6 worker 侧结果" )
    require( base == 10, "A6 源 isolate 的捕获变量未被修改" )
}
```

### B 组：消息传递与克隆语义

```sl
# B1 深拷贝：修改源不影响目标（核心不变式 I1）
static int g_src = 0
static void test_message_deep_copy()
{
    Func<int,List<int>> isoMutateList = function( List<int> src )
    {
        src.add( 999 )          # 只改自己 isolate 里的副本
        ret src.count()
    }

    List<int> a = List<int>()
    a.add( 1 ); a.add( 2 )
    object n = Isolate.run1( isoMutateList, a )
    require( n as int == 3, "B1 目标侧看到 3 个元素" )
    require( a.count() == 2, "B1 源侧仍是 2 个元素（深拷贝）" )
}

# B2 可发送类型：标量 / string / List / Map / SendPort
static void test_sendable_scalars()
{
    Func<object,object> isoEcho = function( object v ) { ret v }

    require( Isolate.run1( isoEcho, 42 ) as int == 42, "B2 int" )
    require( Isolate.run1( isoEcho, "hi" ) as string == "hi", "B2 string" )
    require( Isolate.run1( isoEcho, 3.14 ) as float == 3.14, "B2 float" )
    require( Isolate.run1( isoEcho, null ) == null, "B2 null" )
}

# B3 嵌套容器递归克隆
static void test_sendable_nested()
{
    Func<int,List<List<int>>> isoSumList = function( List<List<int>> nested )
    {
        int s = 0
        for Int32 i = 0, i < nested.count(), i = i + 1
        {
            for Int32 j = 0, j < nested.get(i).count(), j = j + 1
            { s = s + nested.get(i).get(j) }
        }
        ret s
    }

    List<List<int>> n = List<List<int>>()
    List<int> r1 = List<int>(); r1.add(1); r1.add(2)
    List<int> r2 = List<int>(); r2.add(3)
    n.add(r1); n.add(r2)
    require( Isolate.run1( isoSumList, n ) as int == 6, "B3 嵌套容器" )
}

# B4 SendPort 跨 isolate 保持 ==
static void test_sendport_equality()
{
    Func<int,SendPort> isoPortIdentity = function( SendPort p )
    {
        p.send( p == p )
        ret 0
    }

    ReceivePort rp = ReceivePort()
    Isolate.spawn1( isoPortIdentity, rp.sendPort )
    require( rp.recv() as bool, "B4 SendPort 自反相等" )
}

# B5 不可发送对象 → NotSendable（在发送方抛出）
PlainBox { }                       # 普通类，未标注 @sendable
static void test_not_sendable()
{
    ReceivePort rp = ReceivePort()
    bool threw = false
    label b { try rp.sendPort.send( PlainBox() ) } catch { threw = true }
    require( threw, "B5 普通类实例不可发送" )
    rp.close()
}

# B6 向已关闭 port 发送 → PortClosed
static void test_send_closed()
{
    ReceivePort rp = ReceivePort()
    SendPort sp = rp.sendPort
    rp.close()
    bool threw = false
    label b { try sp.send( 1 ) } catch { threw = true }
    require( threw, "B6 向已关闭 port 发送报错" )
}

# B7 recv 在关闭且缓冲耗尽后返回 null
static void test_recv_closed_null()
{
    ReceivePort rp = ReceivePort()
    rp.sendPort.send( 1 )
    rp.close()
    require( rp.recv() as int == 1, "B7 残留消息仍可取出" )
    require( rp.recv() == null, "B7 耗尽后返回 null" )
}
```

### C 组：静态字段隔离（本设计的核心）

```sl
Counter { static int g_value = 0 }

# C1 静态字段 per-isolate
static void test_static_isolation()
{
    Func<int> isoBump = function()
    {
        Counter.g_value = Counter.g_value + 100
        ret Counter.g_value
    }

    Counter.g_value = 7
    object a = Isolate.run0( isoBump )
    require( a as int == 100, "C1 worker 从自己的 0 开始" )     # 静态初始化器在新 isolate 重跑
    require( Counter.g_value == 7, "C1 主 isolate 不受影响" )
}

# C2 静态初始化器在每个 isolate 各跑一次
InitProbe { static int g_init = InitProbe.makeSeed() ; static int makeSeed() { ret 41 } }
static void test_static_init_per_isolate()
{
    Func<int> isoReadInit = function() { ret InitProbe.g_init }

    InitProbe.g_init = 0                       # 主 isolate 里手动清零
    require( Isolate.run0( isoReadInit ) as int == 41, "C2 worker 独立跑初始化器" )
    require( InitProbe.g_init == 0, "C2 主 isolate 保持 0" )
}

# C3 global 全局变量同样隔离
static void test_global_isolation()
{
    Func<void> isoBumpGlobal = function() { global.g_isoCounter = global.g_isoCounter + 1 }

    global.g_isoCounter = 5
    Isolate.run0( isoBumpGlobal )
    require( global.g_isoCounter == 5, "C3 global 隔离" )
}
```

### D 组：生命周期控制

```sl
# D 组共用入口：无捕获的 echo worker（context 为空，无条件可发送）
#   每个用例内部重新声明一份，因为函数变量是方法体局部的。

# D1 pause / resume（Capability）
static void test_pause_resume()
{
    Func<void,SendPort> isoMain = function( SendPort mainPort )
    {
        mainPort.send( "ready" )
        while ( true ) { Coroutine.sleep( 10 ) }
    }

    ReceivePort rp = ReceivePort()
    Isolate iso = Isolate.spawn1( isoMain, rp.sendPort )
    Capability cap = iso.pause()
    require( iso.status == IsolateStatus.Paused, "D1 已暂停" )
    iso.resume( cap )
    require( rp.recv() as string == "ready", "D1 恢复后继续运行" )
    rp.close()
}

# D2 无 capability 的 resume 静默无效（对齐 Dart）
static void test_pause_without_capability()
{
    Func<void,SendPort> isoMain = function( SendPort mainPort )
    {
        mainPort.send( "ready" )
        while ( true ) { Coroutine.sleep( 10 ) }
    }

    ReceivePort rp = ReceivePort()
    Isolate iso = Isolate.spawn1( isoMain, rp.sendPort )
    Capability fake = Capability()              # 未授权的 capability
    iso.resume( fake )                          # 不报错，也不生效
    require( iso.status != IsolateStatus.Dead, "D2 无效 capability 静默" )
    rp.close()
}

# D3 kill(beforeNextEvent) 执行 finally
static int g_cleaned = 0
static void test_kill_finally()
{
    Func<void> isoWithFinally = function()
    {
        label guard
        {
            try { while ( true ) { Coroutine.sleep( 10 ) } }
            finally { g_cleaned = 1 }
        }
    }

    Isolate iso = Isolate.spawn0( isoWithFinally )
    Coroutine.sleep( 30 )
    iso.kill( 1 )                               # beforeNextEvent
    require( g_cleaned == 1, "D3 kill 时 finally 执行" )
}

# D4 ping 存活探测
static void test_ping()
{
    Func<void,SendPort> isoMain = function( SendPort mainPort )
    {
        while ( true ) { Coroutine.sleep( 10 ) }
    }

    ReceivePort rp = ReceivePort()
    Isolate iso = Isolate.spawn1( isoMain, rp.sendPort )
    iso.ping( rp.sendPort, "pong", 0 )
    require( rp.recv() as string == "pong", "D4 ping 回包" )
    iso.kill( 0 )
    rp.close()
}

# D5 onExit 监听
static void test_on_exit()
{
    Func<void,SendPort> isoMain = function( SendPort mainPort )
    {
        while ( true ) { Coroutine.sleep( 10 ) }
    }

    ReceivePort exitRp = ReceivePort()
    ReceivePort rp = ReceivePort()
    Isolate iso = Isolate.spawn1( isoMain, rp.sendPort )
    iso.addOnExitListener( exitRp.sendPort, "done" )
    iso.kill( 0 )
    require( exitRp.recv() as string == "done", "D5 onExit 通知" )
    rp.close(); exitRp.close()
}
```

### E 组：错误传播

```sl
# E1 未捕获 Error → onError 端口 + errorsAreFatal 终止
static void test_error_propagation()
{
    Func<void> isoBoom = function() throws { throw Error.Runtime }

    ReceivePort errRp = ReceivePort()
    ReceivePort exitRp = ReceivePort()
    Isolate iso = Isolate.spawn0( isoBoom )
    iso.addErrorListener( errRp.sendPort )
    iso.addOnExitListener( exitRp.sendPort, null )

    require( errRp.recv() != null, "E1 收到错误通知" )
    require( exitRp.recv() == null, "E1 isolate 已终止" )
    errRp.close(); exitRp.close()
}

# E2 Isolate.run 把异常抛回调用方
static void test_run_error()
{
    Func<void> isoBoom = function() throws { throw Error.Runtime }

    bool threw = false
    label b { try Isolate.run0( isoBoom ) } catch { threw = true }
    require( threw, "E2 run 传播异常" )
}

# E3 Isolate.exit 携带最终消息
#    注意：捕获的是 SendPort（可发送），而不是 ReceivePort（不可发送）
static void test_isolate_exit()
{
    ReceivePort rp = ReceivePort()
    SendPort sp = rp.sendPort                  # ← 捕获可发送的一端

    Func<void> isoExitWithMsg = function() { Isolate.exit( sp, 12345 ) }

    Isolate.spawn0( isoExitWithMsg )
    require( rp.recv() as int == 12345, "E3 exit 最终消息" )
    rp.close()
}
```

### F 组：TransferableData

```sl
# F1 零拷贝转移
static void test_transferable()
{
    Func<int,TransferableData> isoProcessBytes = function( TransferableData td )
    {
        ByteArray b = td.materialize()
        ret b.length()
    }

    ByteArray big = ByteArray( 1000 )
    TransferableData td = TransferableData.fromBytes( big )
    require( Isolate.run1( isoProcessBytes, td ) as int == 1000, "F1 转移成功" )
}

# F2 转移后源句柄失效（不变式 I3）
static void test_transfer_invalid()
{
    Func<int,TransferableData> isoProcessBytes = function( TransferableData td )
    {
        ByteArray b = td.materialize()
        ret b.length()
    }

    ByteArray big = ByteArray( 100 )
    TransferableData td = TransferableData.fromBytes( big )
    Isolate.run1( isoProcessBytes, td )
    bool threw = false
    label b { try td.materialize() } catch { threw = true }
    require( threw, "F2 已转移的句柄失效" )
}

# F3 isValid 查询
static void test_transfer_isvalid()
{
    Func<int,TransferableData> isoProcessBytes = function( TransferableData td )
    {
        ByteArray b = td.materialize()
        ret b.length()
    }

    TransferableData td = TransferableData.fromBytes( ByteArray( 8 ) )
    require( td.isValid(), "F3 转移前有效" )
    Isolate.run1( isoProcessBytes, td )
    require( !td.isValid(), "F3 转移后无效" )
}
```

### G 组：GC 与资源

```sl
# G1 静态影子表引用的对象不被回收
Holder { static object g_obj = null }
static void test_gc_static_shadow()
{
    Func<void> isoNoop = function() { }

    Holder.g_obj = List<int>()
    Isolate.run0( isoNoop )          # 触发其它 isolate 的 GC
    Gc.collect()
    require( Holder.g_obj != null, "G1 静态字段引用的对象存活" )
}

# G2 Channel 缓冲中的对象不被回收（顺手修现存 bug）
static void test_gc_channel_buffer()
{
    Channel<object> ch = Channel<object>.create( 4 )
    ch.send( List<int>() )
    Gc.collect()
    require( ch.recv() != null, "G2 Channel 缓冲对象存活" )
    ch.close()
}

# G3 isolate 退出后堆被回收
static void test_gc_isolate_heap()
{
    Func<void> isoNoop = function() { }

    for Int32 i = 0, i < 20, i = i + 1 { Isolate.run0( isoNoop ) }
    Gc.collect()
    require( true, "G3 无泄漏（配合内存统计断言）" )
}

# G4 跨 isolate 的对象引用不可能存在（不变式 I1 的负面测试）
static void test_no_cross_isolate_ref()
{
    ReceivePort rp = ReceivePort()
    bool threw = false
    label b { try rp.sendPort.send( rp ) } catch { threw = true }   # ReceivePort 不可发送
    require( threw, "G4 ReceivePort 不可发送" )
    rp.close()
}
```

### H 组：与协程的组合

```sl
# H1 isolate 内可以跑多个协程（isolate 入口与内部协程均用函数值）
Func<int> isoMultiCoro = function()
{
    Func<int,int,int> coroAdd2 = function( int a, int b ) { ret a + b }
    Int64 h1 = Coroutine.spawnClosure2( coroAdd2, 1, 1 )
    Int64 h2 = Coroutine.spawnClosure2( coroAdd2, 2, 2 )
    Coroutine.waitAll2( h1, h2 )
    ret ( Coroutine.await(h1) as int ) + ( Coroutine.await(h2) as int )
}
static void test_isolate_with_coroutines()
{
    require( Isolate.run0( isoMultiCoro ) as int == 6, "H1 isolate 内跑协程" )
}

# H2 主 isolate 在协程里 recv 不阻塞其它协程
static void test_recv_in_coroutine()
{
    ReceivePort rp = ReceivePort()
    Int64 p = Coroutine.spawn1( "coroSendLater", rp.sendPort )
    Int64 c = Coroutine.spawn1( "coroRecvAndFlag", rp )
    Coroutine.waitAll2( p, c )
    require( g_recvFlag, "H2 协程内 recv 不阻塞他人" )
}

# H3 Channel 与 Port 共存
static void test_channel_and_port()
{
    Channel<object> ch = Channel<object>.create( 4 )
    ReceivePort rp = ReceivePort()
    ch.send( 1 )
    rp.sendPort.send( 2 )
    require( ch.recv() as int == 1 && rp.recv() as int == 2, "H3 两者并存" )
    ch.close(); rp.close()
}
```

### I 组：Isolate Group

```sl
# I1 同 group：类型身份一致（深拷贝后 is 判断仍成立）
static void test_group_type_identity()
{
    Func<bool,object> isoTypeCheck = function( object v ) { ret v is List<int> }

    List<int> a = List<int>()
    require( Isolate.run1( isoTypeCheck, a ) as bool, "I1 组内类型身份一致" )
}

# I2 IsolateGroup.current 与组内计数
static void test_group_current()
{
    IsolateGroup g = IsolateGroup.current
    require( g != null, "I2 当前组非空" )
}

# I3 spawn 落在同一个 group（共享代码 → 低成本）
static void test_group_same()
{
    Func<void,SendPort> isoMain = function( SendPort mainPort )
    {
        while ( true ) { Coroutine.sleep( 10 ) }
    }

    Int64 g1 = IsolateGroup.current.id
    ReceivePort rp = ReceivePort()
    Isolate iso = Isolate.spawn1( isoMain, rp.sendPort )
    require( iso != null, "I3 spawn 成功" )
    rp.close()
}
```

### 9.1 测试覆盖矩阵

| 组 | 覆盖点 | 对应章节 |
|----|--------|---------|
| A | run / spawn / 端口双向 / 失败 / void / current | 5.2、5.6.1 |
| B | **深拷贝不变式** / 白名单 / 嵌套容器 / SendPort 相等性 / 不可发送拒绝 / 关闭语义 | 4.5.3、4.6 |
| C | **静态字段隔离** / **静态初始化器 per-isolate** / global 隔离 | 4.4 |
| D | pause / resume / Capability / kill + finally / ping / onExit | 4.9 |
| E | 错误传播 / run 抛回 / exit 最终消息 | 4.9.2 |
| F | TransferableData / 不变式 I3 | 4.7 |
| G | 影子表 GC 根 / Channel 缓冲 GC 根 / 无跨 isolate 引用 | 6.7 |
| H | isolate × 协程组合 / Channel 与 Port 并存 | 4.8、7.3 |
| I | group 类型身份 / group API | 4.3 |

***

## 10. 风险与实现红线

1. **【最高】静态字段全局性是头号障碍**。在影子表完成（P1-3）之前，`Isolate` 的一切隔离承诺都不成立。**P1-3 未通过前不得合入任何 isolate 用户可见 API**。
2. **【最高】不变式 I1 必须由运行时强制**，不能靠用户自觉。任何绕过序列化直接把 `VMObject*` 塞进另一个 `VM` 的代码路径都是 bug——包括"看起来无害"的共享字符串。
3. **求值栈禁止搬迁**（沿用 `COROUTINE_DESIGN` 红线 1）：栈槽可存 `sizeof(void*)` 原生指针。
4. **native 函数体内禁止挂起**（沿用红线 2）：`PortRecv` 必须在入口走 `vm_coroutine_suspend_current`。
5. **只在安全点暂停/取消 isolate**：`pause()` / `kill()` 只置标志，等目标 isolate 到达下一个安全点（调度检查点 / 挂起点）再生效，保证 `finally` 可执行。`kill(immediate)` 是唯一例外。
6. **P1 与 P2 的用户可见语义必须完全一致**。禁止让用户写出"在 P1 下能过、在 P2 下过不了"的代码（例如依赖发送后立即可见）。跨 isolate 的可见性只有一条保证：**消息最终会到达，且按发送顺序到达同一 port**。
7. **消息顺序**：同一对 (sender, port) 的消息保证 FIFO；不同 sender 之间**不保证**全局顺序（与 Dart 一致）。
8. **`SendPort` 的 `closed` 状态不可跨 isolate 实时同步**：关闭后已入队的消息仍会被投递，发送方可能拿到 `PortClosed` 也可能在关闭前成功入队——这是分布式系统的固有竞态，文档必须明确。
9. **Channel 迁移是行为敏感的**：P1-2 完成后必须完整跑一遍 `CoroutineTest.sl` 的 F 组与 J 组，任何行为差异都视为回归。
10. **共享只读结构必须真正只读**：P2 多线程下，若 `s_method_code_cache` 等在加载后还会被写（懒编译 / 懒绑定），必须加锁或改为构建期全量构建。**这是 P2 最容易翻车的地方**。
11. **日志系统需要加锁**（P2）：`sl_log_add_runtime_log` 目前是进程级，多线程并发写会交错。
12. **反序列化中途失败不能留下半截对象**：必须先做一次完整的 `validate_sendable` 校验通过后再构造；构造阶段若 OOM，必须清理干净并报错。
13. **禁止在消息中发送 `Isolate` 句柄**（只能发 `controlPort` + capabilities），否则生命周期管理会失控。
14. **全局 id 分配器**（port id / isolate id / capability id / transfer id）在 P2 下必须用原子自增。
15. **C# VM 行为一致性**：所有 isolate 系统方法的语义必须与 C VM 一致，以本文档为唯一规格。C# VM 源码当前在 `source/VM.zip`（归档状态），对齐工作在 P1 收尾阶段进行。

***

## 附录 A：改动文件清单（C VM）

| 文件 | 阶段 | 改动 |
|------|------|------|
| **新增** `src/vm/port/vm_port.h/.c` | P1-1 | 公共端口骨架（FIFO / 关闭 / 唤醒 / 注册表） |
| **新增** `src/vm/port/vm_port_table.h/.c` | P1-4 | 进程级 port_id → isolate 路由表 |
| **新增** `src/vm/runtime/isolate/vm_isolate.h/.c` | P1-4 | isolate 生命周期 / 句柄表 / 状态机 / 消息泵 |
| **新增** `src/vm/runtime/isolate/vm_isolate_group.h/.c` | P1-4 | group 生命周期 / 成员表 |
| **新增** `src/vm/runtime/isolate/vm_static_shadow.h/.c` | P1-3 | ★ 静态字段影子表 |
| **新增** `src/vm/runtime/isolate/vm_message.h/.c` | P1-5 | 序列化 / 反序列化 / 可发送性校验 |
| **新增** `src/vm/runtime/isolate/vm_transfer.h/.c` | P1-10 | TransferableData 所有权转移 |
| **新增** `src/vm/system_method_call/isolate_system_method.h/.c` | P1-6 | `SystemIsolate*` / `SystemPort*` / `SystemTransfer*` |
| `src/vm/system_method_call/coroutine_system_method.c` | P1-2 | 删除 `VMChannel` / `s_channel_head`，改用 `VMPort`（LOCAL 模式） |
| `src/vm/runtime/vm_runtime.h` | P1-3 | `VM` 新增 `isolate` / `static_shadow` / `static_applied_keys` / `static_applying_keys` |
| `src/vm/runtime/vm_runtime.c` | P1-3 | `LoadStaticField` / `StoreStaticField` / `LoadGlobal` / `StoreGlobal` 走影子表；静态初始化器去重表 per-VM 化 |
| `src/vm/runtime/vm_runtime_manager.c` | P1-3 | `s_global_entries` / `s_global_init_*` per-VM 化 |
| `src/vm/runtime/vm_runtime.c`（`vm_destroy` 路径） | P1-2 | `vm_sys_channel_registry_clear()` → `vm_port_registry_clear(vm)` |
| `src/vm/runtime/coroutine/vm_coroutine.h` | P1-7 | 追加 `CORO_BLOCK_ISOLATE_MSG = 6` |
| `src/vm/memory/vm_gc.c` | P1-11 | 根集合扩展：静态影子表 + port 的 `value_buf` |
| **新增** `src/base/thread/sl_thread.h/.c` 等 | P2-1 | 线程 / 互斥 / 条件变量 / 原子 / TLS |
| `src/vm/runtime/method/runtime_method.c` | P2-2 | 字节码缓存加载期冻结 |
| `src/log/` | P2-3 | 日志输出加锁 |
| `source/Front/Lib/Core/Isolate/*.sl` | P1-12 | SL 层类型 |
| `source/Front/Lib/Core/Core.jsonc` | P1-12 | 文件列表 + 系统方法注册 + 类型注册 |
| `test/BaseTest/IsolateTest.sl` | P1 | A~I 组验收用例 |

**不改动**：`src/vm/vm.h`（**不新增 opcode**）、前端 lexer / parser / IR（**不新增关键字**）。

## 附录 B：与 Dart 的差异说明

| 项 | Dart | SimpleLanguage | 原因 |
|----|------|---------------|------|
| 入口函数 | 顶层函数或静态方法的引用 | **函数值**（函数变量 / `Func<>` 类型变量 / 匿名闭包），非名称字符串 | 语言已支持函数类型；`spawnN`/`runN` 第一个参数是函数值，`method_id` 精确到位，无需名字解析（5.2.1） |
| 异步原语 | `Future` / `await` / `Stream` | **协程 + `spawn`/`await`**（本项目）或 `Coroutine.await` 库调用 | 沿用 `COROUTINE_DESIGN` |
| `ReceivePort` | 实现 `Stream`，`listen` 返回 `StreamSubscription` | `listen(handler)` + `recv()` / `tryRecv()` | SL 的 Stream 机制尚未与 isolate 对齐 |
| 泛型 isolate | `Isolate.spawn<T>(entryPoint(T), T)` | `spawn0..3` 固定重载 | 数组不支持协变，`Int64` 句柄无法装入 `object[]`（见 `md/syntax/coroutine.md` §2.4 注） |
| 错误类型 | 任意对象 + `StackTrace` | `Error` 值（`Int32 code` + `string`） | 语言既有错误模型 |
| `TransferableTypedData` | 必须 `materialize()` 才能用 | `TransferableData` + `materialize()` | 对齐 |
| 每 isolate OS 线程 | 是（默认真并行） | **P1 否 / P2 是** | 分阶段交付 |
| `Isolate.spawnUri` | 支持 | P3 | 需要运行时 SLIR 装载 |
| 组内共享不可变对象 | 是 | P3 | 先保证正确性，后做优化 |
| 消息支持环 / 共享子图 | 是 | P3（`REF_BACKREF`） | 一期简化 |
| `Isolate` 对象可发送 | **否** | **否** | 对齐（发 port + capability） |
