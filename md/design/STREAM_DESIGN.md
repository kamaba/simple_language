# Stream 设计文档（simple_language）

> 版本：v1（设计稿）
> 依赖：
> - `source/Front/Lib/Core/Container/Channel.sl`（CSP 通道，提供容量 / 协程背压）
> - `source/Front/Lib/Core/Coroutine.sl`（协程与 `Task`）
> - `source/Front/Lib/Core/IteratorInterface.sl`（`IIterable<T>` / `IIterator<T>`）
> - `md/design/ISOLATE_DESIGN.md`（隔离岛、`SendPort` / `ReceivePort` / `Sendable`）
> 风格约定：**入口 / 回调一律使用函数值**（函数变量 / `Func<>` / 匿名闭包），与 `ISOLATE_DESIGN.md` §5.2.1 一致；不使用方法名字符串。

---

## 1. 目标与范围

为 simple_language 增加 `Stream<T>`：**异步、可能无限、一次性（单订阅）的元素序列抽象**，融合三类语言的优点：

- **Dart `Stream`**：异步事件序列、三态（数据 / 错误 / 完成）、`listen` / `StreamController`、跨 isolate 用 `ReceivePort` 桥接。
- **Java `Stream<T>`**：函数式**惰性**链式转换（`map` / `filter` / `take` / `reduce` / `collect`），只有终端操作才真正跑。
- **C# `Stream` / `IAsyncEnumerable<T>`**：统一"连续产出"的抽象、装饰器叠加（`BufferedStream` 思路 → 我们的容量背压）。
- **Swift `AsyncStream`**：用 `yield` 在闭包里产出值。

本设计**不重新发明**现有机制，而是**组合复用**：
- 用 `Channel<T>` 做底层缓冲与协程背压（容量、发送者挂起 / 接收者唤醒免费获得）；
- 用 `Coroutine` / `Task` 驱动生产者与消费者；
- 用 `SendPort` / `ReceivePort` 做跨 isolate 桥接；
- 用 `IIterable<T>` / `IIterator<T>` 让 `Stream` 也能被 `for` / `forEach` 遍历。

三大约束点（用户要求）：**协程**、**容量（背压）**、**isolate**。
延伸约束：**网络流**——`Stream` 需承载 TCP（有序字节流）与 UDP（数据报）数据，且 Tcp/Udp 要同时支持「无协程回调模式」与「协程等待模式」，见 §16 / §17。

---

## 2. 现状与启发

### 2.1 本语言已有机制（要点）

- `Channel<T>`（`Channel.sl`）：`create()` / `create(capacity)` / `send` / `recv` / `close` / `count` / `isClosed`。
  - `capacity <= 0` 为**无界**；有界时缓冲满则 `send` **挂起当前协程**，取走后唤醒发送者。
  - 本体在 **C VM 端静态注册表**，SL 仅持 `Int64 _chid`；**不可跨 isolate 发送**（不在 `Sendable` 白名单）。
- `Coroutine` / `Task`（`Coroutine.sl`）：
  - `spawnClosure0..3(closure, args…) -> Task`、`spawn0..3`、`awaitHandle`、`waitAll`、`waitAny`、`cancel`、`yieldNow`、`sleep`、`waitUntil`。
  - `Task` 包装 `Int64` 句柄，`awaitHandle()` 取返回值，协作式 `cancel()`。
  - `await expr` 是 `Coroutine.awaitHandle(expr)` 语法糖，`yield` 是 `Coroutine.yieldNow()` 语法糖。
- `IIterable<T>` / `IIterator<T>`（`IteratorInterface.sl`）：
  - `IIterator<T>`: `reset()` / `moveNext() -> bool` / `get T current`。
  - `IIterable<T>`: `iterator() -> IIterator<T>`。`List` / `Set` / `Map` / `Queue` / `Array` / `Range` / `Tree` 等均已实现。
- **没有** `Future` / `async` 关键字；`Stream` / `Iterator`（拉取数据）之外也没有现成的"流式序列"类型。

### 2.2 与四语言 Stream 的对齐

| 语言 | Stream 本质 | 本设计对应 |
|------|-------------|------------|
| Dart | 异步事件序列 + `StreamController` | `Stream<T>` + `StreamController<T>` + 协程驱动 |
| Java | 函数式惰性流水线 | `map` / `where` / `take` / `reduce` 链式返回新 `Stream` |
| C# | 连续产出 + 装饰器 | 容量背压（复用 `Channel`）作为内置装饰能力 |
| Swift | `AsyncStream` 闭包 `yield` | `Stream.generate(closure)`，闭包内 `ctrl.add(x)` |

---

## 3. 设计决策

1. **`Stream<T>` 是 `abstract class`**，统一抽象；具体子类持有底层 `Channel<T>`（或组合其它源）。
2. **单订阅优先**：默认 `Stream` 只能被消费一次（与 `Channel` 一次性消费语义一致）。`BroadcastStream<T>` 作为可选子类支持多订阅。
3. **三态语义**：数据（`add`）/ 错误（`addError`）/ 完成（`close`）。单订阅流在错误 / 完成后进入终态，后续 `add` 抛 `StreamClosed`。
4. **容量即背压**：构造 / `StreamController` 接受 `capacity`，默认 `0`（无界，兼容 `Channel` 语义）。缓冲满 → 生产者 `add` 内部 `channel.send` **挂起生产者协程**，消费者取走后唤醒。
5. **协程驱动，零阻塞**：生产者在协程里 `add`；消费者通过 `listen` / `forEach` / `IIterator` 在协程里消费。终端操作内部 `spawnClosure` 跑消费协程，返回 `Task`，**root 上下文调用也安全**。
6. **跨 isolate 用端口桥接**：`Stream` 本体不可跨 isolate（与 `Channel` 同理）；提供 `pipeTo(SendPort)` / `Stream.fromReceivePort(ReceivePort)` 在两端各建本地 `Stream`，元素按 `Sendable` 规则深拷贝 / 转移。
7. **实现在 SL 层**：完全由 `Channel` + `Coroutine` + `SendPort` 组合而成，**不新增 C 系统调用**（必要时才加性能辅助函数）。

---

## 4. 类层级（父类 / 子类）

```
abstract class Stream<T> extends Object
        interface Core.IIterable<T>          # iterator() 返回 StreamIterator，可在协程内 for 遍历
│
├─ abstract class _BaseStream<T> extends Stream<T>     # 内部基类：持有 Channel<T> + 状态机
│     ├─ class ControllerStream<T>          # StreamController.stream 持有；单订阅
│     ├─ class BroadcastStream<T>           # 多订阅：内部持有多路 Channel（P3）
│     └─ class GeneratedStream<T>           # Stream.generate(closure) 的闭包生成源
│
├─ class FromIterableStream<T>              # 从 List / IIterable 一次性放出（不需协程）
├─ class FromChannelStream<T>               # 包装已有 Channel<T>（CSP ↔ Stream 桥接）
├─ class FromReceivePortStream<T>           # 从 ReceivePort 重建（跨 isolate）
│
├─ class TcpStream extends Stream<ByteArray>        # TCP 连接（协程模式；有序字节流，有背压）
└─ class UdpStream extends Stream<UdpDatagram>      # UDP（协程模式；数据报序列，无背压→丢弃策略）
│
└─ 惰性转换包装（链式，返回新 Stream；终端操作才启动源协程）：
      class MapStream<U,T>       # map
      class WhereStream<T>       # where / filter
      class TakeStream<T>        # take(n)
      class SkipStream<T>        # skip(n)
      class FlatMapStream<U,T>   # flatMap / expand
      class DistinctStream<T>    # distinct（P3）
      class ScanStream<U,T>      # scan / fold 流式版（P3）
```

### 4.1 各类型职责

- **`Stream<T>`（abstract）**：声明全部抽象 / 默认 API（§5.1），定义终态机与错误类型。
- **`_BaseStream<T>`（abstract）**：聚合 `Channel<T> _ch`、状态（`Running` / `Done` / `Errored`）、订阅计数；提供 `add` / `addError` / `close` 等共享实现给子类复用。
- **`ControllerStream<T>`**：由 `StreamController.stream` 暴露；`_BaseStream` 的单订阅特化。
- **`GeneratedStream<T>`**：在 `Stream.generate` 时创建，内部 `Coroutine.spawnClosure0(producer)`，producer 通过 `ctrl.add/addError/close` 产出。
- **`FromIterableStream<T>`**：构造时把集合元素预推入 `Channel`（无界或给定容量），无需常驻协程。
- **`FromChannelStream<T>`**：直接把 `Channel<T>` 套上 `Stream` 三态语义（源 `close` 时流完成）。
- **`FromReceivePortStream<T>`**：内部协程 `recv` 端口消息，转为 `add`，遇到终止哨兵则 `close`。
- **惰性转换类**：持"上游 `Stream` + 转换闭包"，自身仍是 `Stream`；`listen` / `forEach` 时把转换串到消费链上，在协程里逐个处理。

---

## 5. API 设计

### 5.1 `Stream<T>` 抽象接口

```sl
public abstract class Stream<T> extends Object interface Core.IIterable<T>
{
    # ── 终态查询 ──
    get bool isDone()             # 是否已正常完成（close）
    get bool isErrored()          # 是否以错误结束
    get bool isClosed()           # 是否进入终态（Done 或 Errored）

    # ── 订阅（推模式）。返回订阅句柄，可 cancel。
    # onData / onError / onDone 均为函数值（Func<>）。
    StreamSubscription listen(
        Func<void,T> onData,
        Func<void,Error> onError = null,
        Func<void> onDone = null )

    # ── 拉模式（协程内）。IIterable 契约：返回 StreamIterator。
    override Core.IIterator<T> iterator()

    # ── 终端操作（内部 spawn 协程消费，返回 Task 可 await）。
    Task forEach( Func<void,T> action )        # 每个元素执行 action，完成返回
    Task toList( List<T> out )                 # 收集到 List（out 由调用方持有）
    Task reduce( object seed, Func<object,object,T> combine )  # 折叠，结果在 Task 中

    # ── 惰性转换（链式，返回新 Stream；不立即执行）。
    Stream<U> map( Func<U,T> fn )
    Stream<T> where( Func<bool,T> pred )
    Stream<T> take( Int32 n )
    Stream<T> skip( Int32 n )
    Stream<U> flatMap( Func<Stream<U>,T> fn )
    Stream<T> transform( StreamTransformer<T> tf )   # 高级：自定义转换（P3）

    # ── 跨 isolate 桥接（源端）──
    Task pipeTo( SendPort port )               # 把元素逐个 port.send；终态发哨兵

    # ── 工厂 ──
    static Stream<T> fromIterable( Core.IIterable<T> src )
    static Stream<T> fromChannel( Channel<T> ch )
    static Stream<T> fromReceivePort( ReceivePort rp )
    static Stream<T> generate( Func<void,StreamController<T>> producer )  # 闭包产出
    static Stream<T> periodic( Int64 millis, Func<T> tick )               # 周期产出（P2）
}
```

### 5.2 `StreamController<T>`（手动造流）

```sl
public class StreamController<T> extends Object
{
    _init_()                    # 默认无界
    _init_( int capacity )      # 有界（背压）

    Stream<T> get stream        # 返回 ControllerStream（单订阅；重复取抛 StreamAlreadyListened）
    void add( T value )         # 缓冲满则挂起当前协程（背压）；终态后抛 StreamClosed
    void addError( Error e )    # 下发错误；单订阅流随后进入 Errored 终态
    void close()                # 正常完成；唤醒消费者 -> onDone
    void cancel()               # 取消：关闭流并通知未消费协程
}
```

> 注意：`add` / `addError` / `close` 必须在协程上下文才有"挂起"语义；在 root 上下文调用 `add` 时若缓冲满，退化为阻塞（由 `Channel.send` 现有行为决定）。推荐一律在 `generate` 闭包或 `Coroutine` 内使用。

### 5.3 `StreamSubscription`（订阅句柄）

```sl
public class StreamSubscription extends Object
{
    Task get consumer           # 消费协程；await 可等流耗尽/出错
    void cancel()               # 协作取消：取消消费协程 + 关闭流
    bool get isCancelled()
}
```

### 5.4 惰性转换语义（Java Stream 风格）

- 所有 `map` / `where` / `take` / `skip` / `flatMap` **惰性**：调用时只构造包装 `Stream`，不启动源。
- 只有**终端操作**（`listen` / `forEach` / `toList` / `reduce` / 拉取 `iterator`）才 `spawnClosure` 启动上游生产协程。
- 链式 `s.map(f).where(p).take(n)` 形成转换链，元素在协程里逐个流过（lazy，内存恒定）。
- `flatMap` 把每个元素展开成 `Stream<U>`，由 `BroadcastStream` 之外的专用 `FlatMapStream` 顺序合并。

### 5.5 跨 isolate 桥接

```sl
# 源端：把本 isolate 的流桥接到目标 isolate 的 SendPort
Task pipeTo( SendPort port )
{
    # 内部 spawn 协程：
    #   forEach( x => port.send(x) )   # 元素按 Sendable 规则深拷贝/Transferable
    #   终态：port.send( StreamDoneSentinel )  或 port.close()
}

# 目标端：从 ReceivePort 重建本地 Stream
static Stream<T> fromReceivePort( ReceivePort rp )
{
    # 内部建 StreamController + spawn 协程：
    #   loop { obj = rp.recv(); if obj is StreamDoneSentinel { ctrl.close(); break } else ctrl.add(obj) }
}
```

跨 isolate 注意：
- **本体不可发**：`Stream` / `Channel` 句柄不在 `Sendable` 白名单；只能发元素。
- **无背压**：`SendPort.send` 立即深拷贝，不阻塞源协程；如需跨 isolate 背压，用 `Channel<T>` + 双 `SendPort` 反向流控（超出本设计，注明）。
- **类型身份**：跨 isolate 重建的元素是深拷贝；同 `IsolateGroup` 内因共享代码，`is` 判断成立（见 `ISOLATE_DESIGN.md` §4.3 / §9 I 组）。

---

## 6. 协程集成

### 6.1 生产者

```sl
# 方式 A：StreamController 手动（在协程内）
static void producer()
{
    StreamController<int> ctrl = StreamController<int>( 4 )   # 容量 4 = 背压上限
    Stream<int> s = ctrl.stream
    Coroutine.spawnClosure0( function() {
        for Int32 i = 0, i < 1000, i = i + 1 {
            ctrl.add( i )      # 缓冲满则此处挂起，消费者取走后自动唤醒
        }
        ctrl.close()
    })
    # ... s 可被监听
}

# 方式 B：Stream.generate（推荐，内部自动 spawn 协程）
Stream<int> s = Stream<int>.generate( function( StreamController<int> ctrl ) {
    for Int32 i = 0, i < 1000, i = i + 1 {
        ctrl.add( i )
    }
    ctrl.close()
})
```

### 6.2 消费者

```sl
# 推模式
StreamSubscription sub = s.listen(
    function( int x ) { print( x ) },
    function( Error e ) { print( "err:", e ) },
    function() { print( "done" ) } )

# 拉模式（协程内 for 遍历，复用 IIterable）
Coroutine.spawnClosure0( function() {
    Core.IIterator<int> it = s.iterator()
    while ( it.moveNext() ) { print( it.current ) }     # moveNext 内部 recv 挂起
})
```

### 6.3 取消

- `sub.cancel()` / `StreamController.cancel()` → 调用 `Coroutine.cancel(consumerTask)`（协作式：消费协程在下一调度点抛取消异常并结束），并 `close` 流唤醒生产者。
- 生产者协程在 `add` 阻塞时，流关闭会经 `Channel` 唤醒并使其收到 `StreamClosed`。

---

## 7. 容量与背压

- 构造 / `StreamController` 的 `capacity` 透传给 `Channel`（`create(capacity)`）：
  - `capacity <= 0`：**无界**，生产者永不阻塞（可能 OOM，文档提示慎用）。
  - `capacity > 0`：有界，缓冲满时 `add` → `Channel.send` **挂起生产者协程**，消费者每取一个唤醒一个生产者（复用 `vm_channel_wake_one`）。
- 背压对**同 isolate** 内部有效；**跨 isolate** 桥接（`pipeTo`）因 `SendPort.send` 不阻塞而无背压（见 §5.5）。
- 建议默认容量：`StreamController(64)` 级别的合理有界值，避免无界风险（与 `Channel` 默认无界不同，此处建议有界优先）。

---

## 8. isolate 集成（小结）

- `Stream` 与 `Channel` 同为 VM 注册表句柄，**不可跨 isolate**。
- 跨 isolate 唯一路径：源端 `pipeTo(SendPort)` + 目标端 `Stream.fromReceivePort(ReceivePort)`。
- 元素可发送性遵循 `ISOLATE_DESIGN.md` §4.5.3 白名单；不可发送值（如 `Channel` / `ReceivePort` / 捕获了它们的闭包）在 `pipeTo` 时抛 `NotSendable`。
- 在 isolate 内 `Stream.generate` 产出的流只能在**本 isolate** 消费；要交给父 isolate，调用方需自己持有 `SendPort` 并在 producer 闭包里 `port.send`。

---

## 9. 复用与实现路径（零 / 极少 C 改动）

全部逻辑可在 **SL 层** 组合现有系统调用实现：

| Stream 能力 | 复用 |
|-------------|------|
| 缓冲 / 容量 / 背压 | `SystemChannelCreate / Send / Recv / Close / Count` |
| 生产者 / 消费者协程 | `SystemCoroutineSpawnClosure0..3` / `Await` / `Cancel` / `Yield` |
| 跨 isolate | `SendPort.send` / `ReceivePort.recv`（见 ISOLATE_DESIGN） |
| `for` 遍历 | `IIterable<T>` / `IIterator<T>`（已实现） |

**结论**：v1 **不需要任何新 C 系统调用**。仅当性能剖析显示 `Stream` 热路径成为瓶颈时，才考虑新增 `vm_sys_stream_*` 辅助（如批量桥接 `SendPort`），届时另立任务。

---

## 10. 错误处理

| 错误 | 触发 | 处理 |
|------|------|------|
| `StreamClosed` | 向已 `close` / `addError` 的流 `add` | 抛异常，阻止污染终态流 |
| `StreamAlreadyListened` | 单订阅流重复 `listen` / 重复取 `controller.stream` | 抛异常（提示用 `BroadcastStream`） |
| `NotSendable` | `pipeTo` 发送不可发送元素 | 抛异常（见 ISOLATE_DESIGN §4.5.3） |
| 协程取消异常 | `sub.cancel()` / `controller.cancel()` | 协作式取消，向上传播 |

错误沿流**向下游传播**：单订阅流 `addError(e)` → 当前 / 后续消费者收到 `onError(e)` 且流进入 `Errored` 终态。

---

## 11. 端到端示例

```sl
# 生成 0..9，过滤偶数，×10，收集到 List
static void demo()
{
    Stream<int> src = Stream<int>.generate( function( StreamController<int> c ) {
        for Int32 i = 0, i < 10, i = i + 1 { c.add( i ) }
        c.close()
    })

    List<int> out = List<int>()
    Task t = src.where( function( int x ) { ret x % 2 == 0 } )
                .map( function( int x ) { ret x * 10 } )
                .forEach( function( int x ) { out.add( x ) } )
    t.awaitHandle()              # 等流耗尽（协程语法糖：await t）
    print( out )                 # [0, 20, 40, 60, 80]
}

# 跨 isolate：worker 产流，主 isolate 收
static void main()
{
    ReceivePort rp = ReceivePort()
    # 在 worker 里 generate 并 pipe 到 rp.sendPort
    Isolate iso = Isolate.spawn0( function() {
        Stream<int> s = Stream<int>.generate( function( StreamController<int> c ) {
            for Int32 i = 0, i < 5, i = i + 1 { c.add( i ) }
            c.close()
        })
        s.pipeTo( rp.sendPort ).awaitHandle()
    })
    Stream<int> remote = Stream<int>.fromReceivePort( rp )
    remote.forEach( function( int x ) { print( "got", x ) } ).awaitHandle()
    rp.close(); iso.cancel()
}
```

---

## 12. 测试用例（分组）

```sl
# A 组：基础生成 / 完成
static void test_generate_close()
{
    Stream<int> s = Stream<int>.generate( function( StreamController<int> c ) {
        c.add(1); c.add(2); c.close()
    })
    List<int> out = List<int>()
    s.toList(out).awaitHandle()
    require( out.size() == 2 && out[0] == 1, "A 生成+完成" )
}

# B 组：容量 / 背压（协程挂起）
static void test_backpressure()
{
    StreamController<int> ctrl = StreamController<int>( 1 )   # 容量 1
    Stream<int> s = ctrl.stream
    bool producerBlocked = false
    Task prod = Coroutine.spawnClosure0( function() {
        ctrl.add(1); ctrl.add(2)   # 第二个 add 应挂起（容量满）
        ctrl.close()
    })
    Coroutine.sleep( 20 )
    require( !prod.isDead, "B 容量满时生产者协程挂起（背压生效）" )
    s.forEach( function( int x ) { } ).awaitHandle()
}

# C 组：惰性转换链
static void test_lazy_chain()
{
    Stream<int> s = Stream<int>.fromIterable( List<int>(1,2,3,4,5) )
    List<int> out = List<int>()
    s.where( function(int x){ ret x>2 } )
     .map( function(int x){ ret x*x } )
     .toList(out).awaitHandle()
    require( out == List<int>(9,16,25), "C map+where 惰性" )
}

# D 组：错误传播
static void test_error()
{
    bool got = false
    Stream<int> s = Stream<int>.generate( function( StreamController<int> c ) {
        c.add(1); c.addError( Error("boom") )
    })
    s.listen( function(int x){}, function(Error e){ got = true }, function(){} )
    Coroutine.sleep( 30 )
    require( got, "D 错误向下游传播" )
}

# E 组：IIterable 拉取（协程内）
static void test_iterator()
{
    Stream<int> s = Stream<int>.fromIterable( List<int>(10,20) )
    Int32 sum = 0
    Coroutine.spawnClosure0( function() {
        Core.IIterator<int> it = s.iterator()
        while ( it.moveNext() ) { sum = sum + it.current }
    }).awaitHandle()
    require( sum == 30, "E 拉取遍历" )
}

# F 组：跨 isolate 桥接
static void test_cross_isolate()
{
    ReceivePort rp = ReceivePort()
    Isolate iso = Isolate.spawn0( function() {
        Stream<int> s = Stream<int>.generate( function( StreamController<int> c ) {
            c.add(7); c.add(8); c.close()
        })
        s.pipeTo( rp.sendPort ).awaitHandle()
    })
    List<int> out = List<int>()
    Stream<int>.fromReceivePort( rp ).toList(out).awaitHandle()
    require( out == List<int>(7,8), "F 跨 isolate 重组" )
    rp.close(); iso.cancel()
}

# G 组：取消
static void test_cancel()
{
    StreamController<int> ctrl = StreamController<int>( 2 )
    StreamSubscription sub = ctrl.stream.listen( function(int x){} )
    sub.cancel()
    require( ctrl.stream.isClosed(), "G 取消关闭流" )
}
```

---

## 13. 与四语言差异

| 维度 | Dart | Java | C# | Swift | **simple_language** |
|------|------|------|----|-------|---------------------|
| 基类形态 | `abstract Stream<T>` | `interface Stream<T>` | `abstract Stream` | `struct AsyncStream` | **`abstract Stream<T>`** |
| 背压 | 由 `Channel`/同步 | 无（拉惰性） | `Buffer` 装饰 | 无界缓冲默认 | **复用 `Channel` 容量** |
| 协程驱动 | `async*`/`await for` | 无 | `await foreach` | `for await` | **`Coroutine` + 闭包，无新语法** |
| 跨 isolate | `ReceivePort` | 无 | `Channel`(TPL) | `Actor` | **`SendPort`/`ReceivePort` 桥接** |
| 函数式转换 | 少 | 丰富 | LINQ | 少 | **`map`/`where`/`take`/`flatMap`** |

---

## 14. 分期

- **P1（一期）**：`Stream<T>` + `_BaseStream` + `ControllerStream` + `GeneratedStream` + `FromIterableStream` + `FromChannelStream`；`listen` / `forEach` / `toList` / `reduce`；`map` / `where` / `take` / `skip`；`StreamIterator`；`StreamController`；`IIterable` 适配。**纯 SL 层，零 C 改动**。
- **P2（二期）**：跨 isolate 桥接 `pipeTo` / `fromReceivePort`；`periodic`；`fromChannel` 双向。
- **P3（三期）**：`BroadcastStream`（多订阅）；`flatMap` / `distinct` / `scan`；可选语法糖 `async*` 生成器与 `for await` 循环（前端降级为 `StreamController` + 协程，与 `ISOLATE_DESIGN.md` §6.5 闭包路径同构）。
- **网络分期**：见 §17.7（N1 回调模式 → N2 协程模式 + `TcpStream`/`UdpStream` 桥接 → N3 背压 / 零拷贝 / 服务器）。

---

## 15. 附录：与现有类型的关系图

```
                 Object
                   │
            abstract Stream<T>  ──implements──▶ IIterable<T>
               │        │
   _BaseStream<T>   FromIterableStream / FromChannelStream / FromReceivePortStream
        │
 ControllerStream / GeneratedStream
        │
   StreamController<T>  ─持有─▶ Channel<T>（VM 注册表，容量/背压）
        │
   生产者: Coroutine.spawnClosure0(producer)  ──add──▶ Channel.send（满则挂起）
   消费者: listen/forEach/iterator ──recv──▶ Channel.recv（空则挂起）
   跨 isolate: Stream ─pipeTo─▶ SendPort.send ─▶ ReceivePort.recv ─▶ 新 Stream
```

设计要点复盘：**一个 abstract `Stream<T>` 居中，向下复用 `Channel<T>` 拿到容量与协程背压，向外用 `Coroutine` 驱动、用 `SendPort`/`ReceivePort` 跨越 isolate，并借用 `IIterable`/`IIterator` 提供拉取遍历**——在不动 C VM 的前提下，把四语言的 Stream 优点收敛进本语言既有机制。

---

## 16. 网络流：Tcp/Udp 与 `Stream` 的关联（设计因素）

### 16.1 为什么网络要接进 `Stream`

1. **统一消费方式**：网络数据本质就是"陆续到达的字节/报文序列"，正是 `Stream` 的语义，接进来后 `map` / `where` / `take` / `forEach` 可直接用于协议解析。
2. **背压复用**：`Stream` 已有容量机制，正好承载 TCP 的接收缓冲与流控，无需另造一套。
3. **错误语义统一**：网络错误（连接重置、超时）统一走 `addError` → 流进入 `Errored`，业务代码只处理一种错误通道。
4. **跨 isolate 复用**：`pipeTo(SendPort)` 可直接把网络流分发到其它 isolate，与 §5.5 完全一致。

### 16.2 TCP vs UDP 在 `Stream` 语义下的差异（关键设计因素）

| 设计因素 | TCP → `Stream<ByteArray>` | UDP → `Stream<UdpDatagram>` |
|----------|---------------------------|------------------------------|
| 有序性 | 保证顺序 | 不保证（可能乱序） |
| 消息边界 | **无**（字节流，`read` 返回任意长度块） | **有**（一次 `recvFrom` = 一个数据报） |
| 背压 | **有**：缓冲满 → 暂停读（反压到对端窗口） | **无**（UDP 不反压）；缓冲满 → 丢弃 |
| 容量满策略 | `uv_read_stop` / `uv_read_start` | `DropNewest`（默认）/ `DropOldest`（保留最新）可配 |
| 终态（Done） | 对端关闭 → EOF → `close()` | 无 EOF；只有本端 `close()` 才算 Done |
| 可靠性 | 可靠、自动重传 | 不可靠，可能丢包/重复 |
| 元素类型 | `ByteArray`（字节块） | `UdpDatagram`（数据 + 源地址 + 源端口） |

> **设计结论**：TCP 与 UDP **不能共用同一个 `Stream` 子类**——字节流要处理"半包/粘包"，数据报要保留"边界与来源"。因此分别用 `TcpStream` 与 `UdpStream` 两个子类，类型层面就把差异固定下来，避免用户误用。

### 16.3 类型映射

```sl
# TCP：有序字节流
public class TcpStream extends Stream<ByteArray>
{
    # 协程模式：挂起等待数据（见 §17.4）
    ByteArray read()                    # 有数据立即返回；无数据挂起当前协程
    ByteArray readExactly( Int32 n )    # 挂起直到凑够 n 字节；对端关闭且不足则返回已读部分
    void send( ByteArray data )         # 写；待写队列超阈值则挂起（写背压）
    void close()
    get bool isConnected()
}

# UDP：数据报序列
public class UdpDatagram extends Object
{
    ByteArray data = null;
    string address = ""          # 源/目标地址
    Int32 port = -1
}

public class UdpStream extends Stream<UdpDatagram>
{
    UdpDatagram recvFrom()              # 挂起等待一个数据报
    void sendTo( UdpDatagram dg )       # 发送（不挂起；UDP 无写背压）
    void close()
    get UdpOverflowPolicy overflowPolicy()   # DropNewest / DropOldest
}
```

### 16.4 背压策略（复用 `Stream` 容量）

- **TCP（真背压）**：`TcpStream` 底层 `Channel<ByteArray>` 缓冲达容量上限 → `uv_read_stop(handle)` 停止读；消费者取走（`recv` / `moveNext`）→ `uv_read_start(handle)` 恢复读。
  - 效果：消费者慢 → 内核接收窗口填满 → 对端发送变慢 → **背压一路传导到发送方**，与 `Channel` 的协程背压完全同构。
  - 容量单位建议按**块数**（如 64 块）或**字节数**（如 64KB）二选一，默认按块数（与 `Channel` 一致，实现简单）。
- **UDP（无背压，只能丢弃）**：容量满时的策略在创建时指定：
  - `DropNewest`（默认）：新到的包直接丢弃（适合实时音视频，宁可丢新包）。
  - `DropOldest`：挤掉最老的包（适合"只关心最新状态"的场景，如遥测）。
  - 无论哪种，**都不挂起发送方**（UDP 语义如此），仅记录丢包计数供诊断。

### 16.5 终态与错误

- **TCP**：对端 `FIN` → EOF → 流 `close()` → `Done`；`RST` / 超时 → `addError(NetworkError)` → `Errored`。
- **UDP**：无连接，无 EOF；只有本端 `close()` 才 `Done`。ICMP 不可达等错误（平台相关）可选择性 `addError`，默认**不上报**（避免不可靠错误打断流）。
- 网络错误统一转成 `Stream` 的错误通道，业务侧 `onError` 一处处理。

### 16.6 与转换链协作（协议解析示例）

```sl
# TCP 字节流 -> 按行切分 -> 过滤 -> 处理（惰性，内存恒定）
TcpStream conn = Tcp.connectCoroutine( "127.0.0.1", 9000 )
conn.map( function( ByteArray b ) { ret decodeLines(b) } )   # 半包/粘包在 map 里处理
    .where( function( string line ) { ret line.length() > 0 } )
    .take( 100 )
    .forEach( function( string line ) { handle( line ) } )
    .awaitHandle()
```

> **半包/粘包**是 TCP 字节流的固有问题：设计上**不内置**分包逻辑（协议千差万别），而是提供 P3 的 `Stream<ByteArray>.splitBy(delimiter)` / `chunk(n)` 辅助算子，由用户组合。

### 16.7 设计因素清单（Checklist）

- [x] 顺序 / 边界：TCP 无边界、UDP 有边界 → 分两个子类
- [x] 背压：TCP 暂停读（真背压）；UDP 丢弃策略
- [x] 容量：复用 `Channel` 容量，默认有界
- [x] 错误：统一 `addError` → `Errored`
- [x] 终态：TCP 有 EOF；UDP 无 EOF
- [x] 跨 isolate：句柄不可发，只发数据（`pipeTo` + `TransferableData` 零拷贝）
- [x] 协程：模式 B 用 `CORO_BLOCK_IO` 挂起，事件循环唤醒
- [x] 零拷贝：大块 `ByteArray` 用 `TransferableData` 转移（见 `ISOLATE_DESIGN.md` §5.6.4）

---

## 17. Tcp/Udp 两种模式设计

### 17.1 现状（调研结论）

- `source/Front/Lib/Std/Net/Tcp.sl` 与 `Udp.sl` 目前是**空命名空间占位**，已在 `Std.jsonc` 注册（tag `net`）。
  - `Tcp.sl`：`namespace Tcp { }`
  - `Udp.sl`：`namespace Http { }` —— **命名空间名是拷贝残留的 bug**，实现时应改为 `namespace Udp`。
- `csimple_lang` 已**依赖 libuv**（`README.md`、`project/cmake/Makefile`、`project/build.sh` 的 `pkg-config --exists libuv`），但 `src/` 中**尚未接入**（无 `uv_loop` / `uv_tcp` 等使用代码）→ 网络是全新实现，libuv 可直接用。
- 协程 IO 挂起常量 **`CORO_BLOCK_IO = 5` 已存在**（当前用于 `Channel` 的 send/recv 挂起），等待队列 + `reexecute` 约定可直接套用到网络。

### 17.2 两种模式定位对比

| 维度 | **模式 A：无协程（回调 / 非阻塞）** | **模式 B：协程等待（同步风格）** |
|------|--------------------------------------|-----------------------------------|
| 编程风格 | 事件驱动，注册回调（Node.js 风格） | 顺序阻塞风格 `read()`（Go / Dart 风格） |
| 是否挂起协程 | **否**，回调在 VM 线程执行 | **是**，`CORO_BLOCK_IO` 挂起，数据到达唤醒 |
| 线程占用 | 不占线程 | 不占线程（挂起期间线程跑其它协程） |
| 错误处理 | `onError(cb)` | `try/catch` 包裹 `read()` |
| 适用场景 | 高并发服务、事件驱动、与现有回调代码共存 | 顺序业务逻辑、客户端、脚本式代码、协议解析 |
| 代表类 | `TcpSocket` / `TcpServer` / `UdpSocket` | `TcpStream` / `UdpStream`（即 `Stream` 子类） |
| root 上下文 | 正常工作 | `read()` 退化为真实阻塞（与 `Channel.recv` / `sleep` 一致） |

**关键决策**：两种模式**共用同一套底层句柄与 libuv 事件循环**，区别仅在 API 层是否挂起协程。连接创建时确定模式，模式互斥（防止回调与 `read` 抢同一份数据）：
- `Callback` 模式调 `read()` → 抛 `InvalidMode`
- `Coroutine` 模式调 `onData(cb)` → 抛 `InvalidMode`

### 17.3 模式 A：无协程（回调 / 非阻塞）

```sl
public class TcpSocket extends Object
{
    Int64 _sid = 0

    static TcpSocket connect( string host, Int32 port )      # 非阻塞连接
    static TcpSocket connect( string host, Int32 port, Func<void> onConnect )

    void onData( Func<void,ByteArray> cb )     # 数据到达（VM 线程内回调）
    void onClose( Func<void> cb )
    void onError( Func<void,Error> cb )

    void send( ByteArray data )                # 立即返回，libuv 排队写出
    void close()
    get bool isConnected()

    # 转 Stream：把回调数据推入 Channel，供需要流的消费者使用
    Stream<ByteArray> stream( int capacity = 64 )
}

public class TcpServer extends Object
{
    static TcpServer listen( string host, Int32 port )
    void onConnection( Func<void,TcpSocket> cb )   # 每来一个连接回调一次
    void close()
}

public class UdpSocket extends Object
{
    static UdpSocket bind( string host, Int32 port )
    void onMessage( Func<void,UdpDatagram> cb )
    void sendTo( UdpDatagram dg )
    void close()
    Stream<UdpDatagram> stream( int capacity = 64 )
}
```

- 回调在 `vm_isolate_pump` 之后的 VM 线程内同步执行 → **无锁、无需跨线程投递**。
- 回调内若抛异常，按 isolate 的未捕获异常处理（不崩溃进程）。

### 17.4 模式 B：协程等待（同步风格）

`TcpStream` / `UdpStream` 即 `Stream` 子类（§16.3），其 `read()` / `recvFrom()` 在协程内挂起。

```sl
# 服务端：accept 挂起
TcpServer svr = TcpServer.listenCoroutine( "0.0.0.0", 9000 )
Coroutine.spawnClosure0( function() {
    while ( true )
    {
        TcpStream conn = svr.accept()          # 无连接则挂起协程（不阻塞线程）
        ByteArray head = conn.readExactly( 4 ) # 挂起直到凑够 4 字节
        conn.send( ByteArray("ok") )
        conn.close()
    }
})

# 客户端
TcpStream c = Tcp.connectCoroutine( "127.0.0.1", 9000 )
ByteArray resp = c.read()        # 挂起等待
```

**挂起 / 唤醒时序（核心）**：

```
协程调用 TcpStream.read()
  └─> vm_sys_tcp_recv(sid)
        ├─ VMSocket.ch 缓冲非空 → pop 出 ByteArray，直接返回（不挂起）
        └─ 缓冲为空 → vm_coroutine_suspend_current(vm, CORO_BLOCK_IO,
                          reexecute = TRUE, requeue = FALSE)
             （sid 留在栈上不 pop；resume 后重跑该系统调用重新检查条件 —— 与 Channel 完全同构）

libuv 事件循环（在 vm_isolate_pump 内 uv_run(loop, UV_RUN_NOWAIT) 驱动）
  └─> uv_read_cb 收到字节 → 构造 ByteArray
        ├─ vm_channel_buf_push(sock->ch, bytes)
        │     └─ 若达到容量上限 → uv_read_stop(handle)      # TCP 背压
        └─ vm_channel_wake_one(vm, &sock->ch->recv_q)        # 唤醒挂起协程
              └─ vm_coroutine_enqueue_ready(vm, waiter)

调度器恢复该协程 → 重跑 vm_sys_tcp_recv → 取到 bytes → 返回给 SL
```

### 17.5 C 层设计

**句柄结构**（新增，per-isolate 注册表，与 `VMChannel` 同构）：

```c
typedef struct VMSocket {
    Int64             id;
    int32             kind;          /* TCP_CLIENT / TCP_LISTENER / UDP */
    int32             mode;          /* MODE_CALLBACK / MODE_COROUTINE */
    uv_tcp_t          tcp;           /* 或 uv_udp_t udp（union） */
    VMIsolate*        owner;         /* 归属 isolate（句柄不可跨 isolate） */

    /* 模式 A：回调（函数值闭包对象） */
    VMObject*         cb_on_data;
    VMObject*         cb_on_close;
    VMObject*         cb_on_error;
    VMObject*         cb_on_conn;    /* listener */

    /* 模式 B：复用 VMChannel 做缓冲 + 等待队列 ★ */
    VMChannel*        ch;            /* 容量/背压/挂起唤醒全部免费复用 */
    int32             eof;           /* TCP 对端已关闭 */
    int32             overflow;      /* UDP 丢弃策略 */
    Int64             dropped;       /* UDP 丢包计数 */
} VMSocket;
```

> **★ 关键复用点**：模式 B 直接把 socket 的接收缓冲建模成 `VMChannel`——`read()` 就是 `vm_sys_channel_recv` 的同一套逻辑（容量、发送/接收等待队列、`wake_one`、`reexecute` 约定）。这样网络协程 IO **几乎零新代码**，且行为与 `Channel` 完全一致（背压、唤醒、取消语义统一）。

**事件循环集成**：

```c
void vm_isolate_pump(VMIsolate* iso)     /* 在 ISOLATE_DESIGN §6.7 基础上增加两行 */
{
    uv_run(iso->loop, UV_RUN_NOWAIT);    /* ① 先跑一轮网络事件（非阻塞） */
    /* ...原有端口消息分发... */
}
```
- P1（M:1 协程式）：libuv loop 跑在**主 VM 线程**，每轮调度 `uv_run(UV_RUN_NOWAIT)`；回调与协程恢复都在 VM 线程 → 无锁安全。
- P2（1:1 线程式）：每个 isolate 独立 loop + 独立线程，socket 归该 isolate 私有。

**新增系统调用**（注册到 `Std.jsonc` 的 `net` 组）：

| SL 调用 | C 函数 | 签名 / 说明 |
|---------|--------|-------------|
| `SystemTcpConnect` | `vm_sys_tcp_connect` | `(string host, Int32 port, Int32 mode) -> Int64 sid` |
| `SystemTcpListen` | `vm_sys_tcp_listen` | `(string host, Int32 port, Int32 mode) -> Int64 sid` |
| `SystemTcpAccept` | `vm_sys_tcp_accept` | `(Int64 sid) -> Int64`（协程模式挂起） |
| `SystemTcpSend` | `vm_sys_tcp_send` | `(Int64 sid, ByteArray) -> void`（写队列满则挂起） |
| `SystemTcpRecv` | `vm_sys_tcp_recv` | `(Int64 sid) -> ByteArray`（协程模式挂起） |
| `SystemTcpClose` | `vm_sys_tcp_close` | `(Int64 sid) -> void` |
| `SystemUdpBind` | `vm_sys_udp_bind` | `(string host, Int32 port, Int32 mode) -> Int64 sid` |
| `SystemUdpSendTo` | `vm_sys_udp_sendto` | `(Int64 sid, UdpDatagram) -> void`（不挂起） |
| `SystemUdpRecvFrom` | `vm_sys_udp_recvfrom` | `(Int64 sid) -> UdpDatagram`（协程模式挂起） |
| `SystemUdpClose` | `vm_sys_udp_close` | `(Int64 sid) -> void` |
| `SystemDnsResolve` | `vm_sys_dns_resolve` | `(string host) -> string`（协程模式挂起） |

**背压实现**：

| 方向 | 机制 |
|------|------|
| TCP 读背压 | `Channel` 满 → `uv_read_stop`；消费者取走 → `uv_read_start` |
| TCP 写背压 | `uv_write` 待写队列超阈值 → 挂起协程（`CORO_BLOCK_IO`），`uv_write_cb` 后唤醒 |
| UDP | 不背压；缓冲满按 `DropNewest` / `DropOldest` 丢弃，`dropped` 计数 +1 |

### 17.6 跨 isolate

- `VMSocket` / `TcpStream` / `UdpStream` 句柄**不可跨 isolate**（与 `Channel` / `Stream` 同：VM 注册表句柄，不在 `Sendable` 白名单）。
- 跨 isolate 只传数据：
  - 小数据：`port.send(bytes)` 深拷贝。
  - **大数据零拷贝**：`TransferableData.fromBytes(big)` → `port.send(td)`（见 `ISOLATE_DESIGN.md` §5.6.4），目标端 `materialize()` 还原。
  - 推荐模式：**网络 IO 隔离在一个 isolate**（IO isolate），收包后 `pipeTo(sendPort)` 分发给工作 isolate，工作 isolate 用 `Stream.fromReceivePort` 重建流处理——CPU 密集处理与 IO 解耦，正是 isolate 的价值所在。

### 17.7 网络分期

- **N1（回调模式）**：libuv 接入 + `VMSocket` 注册表 + `TcpSocket` / `TcpServer` / `UdpSocket` 回调 API + `stream()` 转 `Stream`；`vm_isolate_pump` 增加 `uv_run(NOWAIT)`。
- **N2（协程模式 + Stream 桥接）**：`TcpStream extends Stream<ByteArray>` / `UdpStream extends Stream<UdpDatagram>`；`read` / `readExactly` / `accept` / `recvFrom` 协程挂起；`connectCoroutine` / `listenCoroutine`；`Dns.resolve`。
- **N3（进阶）**：TCP 读写背压调优、UDP 丢弃策略、`TransferableData` 零拷贝分发、`splitBy` / `chunk` 协议解析算子、`Tls`（可选）。

---

## 18. 网络测试用例（H / I / J 组）

```sl
# H 组：TCP 回调模式（无协程）
static void test_tcp_callback()
{
    TcpServer svr = TcpServer.listen( "127.0.0.1", 19090 )
    List<ByteArray> got = List<ByteArray>()
    svr.onConnection( function( TcpSocket s ) {
        s.onData( function( ByteArray b ) { got.add(b) } )
        s.send( ByteArray("hi") )
    })
    TcpSocket cli = TcpSocket.connect( "127.0.0.1", 19090 )
    cli.onData( function( ByteArray b ) { require( b.length() == 2, "H 收到 hi" ) } )
    cli.send( ByteArray("ping") )
    Coroutine.sleep( 100 )
    require( got.size() >= 1, "H 回调模式收发" )
    svr.close(); cli.close()
}

# I 组：TCP 协程模式（挂起 + 背压 + EOF）
static void test_tcp_coroutine()
{
    TcpServer svr = TcpServer.listenCoroutine( "127.0.0.1", 19091 )
    Coroutine.spawnClosure0( function() {
        TcpStream s = svr.accept()          # 挂起等连接
        s.send( ByteArray("hello") )
        s.close()
    })
    TcpStream c = Tcp.connectCoroutine( "127.0.0.1", 19091 )
    ByteArray b = c.read()                  # 挂起等数据（不阻塞线程）
    require( b.length() == 5, "I 协程 read 拿到数据" )
    ByteArray eof = c.read()                # 对端关闭 -> EOF
    require( eof == null, "I EOF 返回 null（流 Done）" )
}

# I2：协程 read 挂起期间不阻塞其它协程
static void test_tcp_read_not_blocking()
{
    # 两个协程并发：一个卡在 read()，另一个应能正常推进（用 flag 验证）
    ...
}

# J 组：UDP 两种模式
static void test_udp_datagram_boundary()
{
    UdpSocket srv = UdpSocket.bind( "127.0.0.1", 19092 )
    List<UdpDatagram> got = List<UdpDatagram>()
    srv.onMessage( function( UdpDatagram d ) { got.add(d) } )
    UdpSocket cli = UdpSocket.bind( "127.0.0.1", 0 )
    cli.sendTo( UdpDatagram( ByteArray("a"), "127.0.0.1", 19092 ) )
    cli.sendTo( UdpDatagram( ByteArray("bb"), "127.0.0.1", 19092 ) )
    Coroutine.sleep( 100 )
    require( got.size() == 2 && got[0].data.length() == 1, "J 数据报边界保留（不粘包）" )
    srv.close(); cli.close()
}
```

---

## 19. 附录：网络 + Stream + 协程 关系图

```
        Stream<ByteArray>  ◀──extends── TcpStream  ──持有──▶ VMSocket(uv_tcp_t)
        Stream<UdpDatagram> ◀──extends── UdpStream ──持有──▶ VMSocket(uv_udp_t)
                │                                                  │
        容量/背压（Channel）                              libuv 事件循环
                │                                                  │
   协程挂起 CORO_BLOCK_IO ◀────── vm_coroutine_suspend_current ◀───┘
                │                        （数据到达 uv_read_cb）
                ▼                                    │
   vm_coroutine_enqueue_ready ◀── vm_channel_wake_one ┘
                │
        调度器恢复 → 重跑系统调用（reexecute=TRUE）→ 拿到数据

  模式 A（回调）：uv_read_cb → 直接在 VM 线程调用 cb_on_data(bytes)（不挂起协程）
  模式 B（协程）：uv_read_cb → push 到 Channel → 唤醒挂起协程（挂起/恢复）

  跨 isolate：句柄不可发；只发数据（port.send / TransferableData 零拷贝）
```

**一句话总结网络部分**：`TcpStream` / `UdpStream` 作为 `Stream` 的子类，**复用 `Channel` 做接收缓冲**（容量 + 背压 + 协程等待队列全部免费），上层用 libuv 提供事件；对外同时暴露**回调（无协程）**与**协程等待（同步风格）**两种 API，让使用者按场景选择，而底层句柄、事件循环、错误语义完全统一。
