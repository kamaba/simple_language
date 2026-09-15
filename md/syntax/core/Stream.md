# Stream（元素流）

`Stream<T>` 是 Dart 风格的异步元素流抽象（L1 元素层），与 `ByteStream`（L0 字节层）、`Codec<S,T>`（L2 编解码层）共同构成三层流模型。位于 `Core` 标准库 `IO` 目录（`Core/IO/Stream.sl`，无独立命名空间）。

P1 实现全部由 SL 层组合完成，**零新增 syscall**（设计原则 P4「零 C 改动优先」）：

```
Stream<T> / StreamController<T>       （SL，Lib/Core/IO/Stream.sl）
  ├─ Channel<object>                   事件通道：容量即背压，空则 recv 挂起
  ├─ Coroutine.spawnClosure0           分发协程（listen 后启动）与生产协程
  ├─ StreamIterator<T>                拉模式：listen + Channel 桥接 moveNext
  └─ ByteStream × Codec<S,T>           fromByteStream：L0×L2×L1 三层桥接（decode 方向）
```

配套：`syntax/core/Lz4.md`（L0 压缩原语）、`Core/IO/ByteStream.sl` 与 `Core/IO/Codec.sl`（同批 P1 交付，见 §2.4）。设计文档 `md/design/STREAM_DESIGN.md` 已随 `md/design/` 目录移除（2026-09-13），语义以本文与源码为准。

---

## 1. 组成

`Core/IO/Stream.sl` 共 26 类，分四组：

| 组 | 类 | 职责 |
|----|-----|------|
| 事件与订阅 | `_StreamEvent`、`_CancelFlag`、`StreamSubscription` | 事件信封（data/error/done）、闭包内可写的取消标记、订阅句柄 |
| 流抽象与拉模式 | `abstract Stream<T>`（interface `Core.IIterable<T>`）、`StreamIterator<T>` | 推模式入口 + iterator 拉模式桥 |
| 控制器 | `StreamController<T>`、`_ControllerStream<T>`、`StreamSink<T>` | 写入端（add/addError/close/sink）与读取端（分发协程） |
| 惰性变换与生产流 | `_MapStream`、`_WhereStream`、`_TakeStream`、`_SkipStream`、`abstract StreamTransformer<S,T>`、`_FromIterableStream`、`_GenerateStream`、`_EmptyStream`、`_ErrorStream`、`_FromByteStreamStream` | map/where/take/skip/transform + 五种工厂实现 |

## 2. API

### 2.1 Stream\<T\>（abstract，interface Core.IIterable\<T\>）

| 成员 | 说明 |
|------|------|
| `abstract StreamSubscription listen( Function onData, Function onError, Function onDone, bool cancelOnError )` | 订阅（单次）；另有单参重载 `listen( onData )` |
| `override get Core.IIIterator<T> iterator` | 拉模式迭代器（`StreamIterator` 适配） |
| `Stream<U> map<U>( Function mapper )` | 惰性映射：`fn(v)` 结果转发给下游 onData |
| `Stream<T> where( Function predicate )` | 惰性过滤：`fn(v) as bool` 为 true 才放行 |
| `Stream<T> take( int count )` | 惰性截断：取满 N 后 cancel 上游 + 触发 onDone |
| `Stream<T> skip( int count )` | 惰性跳过前 N 个后透传 |
| `Stream<U> transform<U>( StreamTransformer<T,U> transformer )` | 变换器桥接（内部调 `transformer.apply( this )`） |
| `Task forEach( Function action )` | 终端：逐元素执行 action，完成时 Task 结束 |
| `Task toList()` | 终端：收集为 `List<T>`；结果经 `Coroutine.awaitTask( task )` 取回后 `as List<T>` |

静态工厂：

| 工厂 | 说明 |
|------|------|
| `static Stream<T> fromIterable( Array<T> items )` | 数组生产流：生产协程逐个 add 后 close |
| `static Stream<T> generate( int count, Function gen )` | 生成生产流：`gen(i)` 产出 i ∈ [0, count) |
| `static Stream<T> fromByteStream( ByteStream src, Codec<T, ByteBuf> codec )` | 字节流解码生产流（三层桥接） |
| `static Stream<T> empty()` | 空流：订阅即 done |
| `static Stream<T> error( object error )` | 错误流：订阅即 error + done |
| `static Stream<T> periodic( Int64 millis, Function tick )` | 周期生产流：每 millis 毫秒以 `tick()` 返回值投递一个元素 |

### 2.2 StreamController\<T\>（写入端）

| 成员 | 说明 |
|------|------|
| `_init_()` / `_init_( int capacity )` | 默认通道无缓冲上限（add 永不挂起）；指定容量后 add 满时挂起 |
| `get Stream<T> stream` | 读取端（惰性创建，重复读取返回同一实例） |
| `get StreamSink<T> sink` | 分块输出端（衔接 Codec 的 chunked API） |
| `get Channel<object> events` | 原始事件通道（`_ControllerStream` 分发协程的数据源） |
| `get bool isClosed` | 是否已 close |
| `void add( T value )` / `void addError( object error )` | 投递数据/错误事件；通道满时挂起；已 close 时静默忽略 |
| `void close()` | 投递 done 事件并关闭通道（已缓冲事件仍可被取尽） |
| `get Task done` | 写入端收尾等待（P1 轮询实现，P4 换事件通知） |

### 2.3 其余配套类

| 类 | 说明 |
|----|------|
| `StreamSink<T> extends ChunkedConversionSink<T>` | controller 的 sink 视图：`add`/`close` 直通 controller |
| `StreamSubscription` | 订阅句柄：`cancel()` 关闭通道 + 置标记；`get bool isCanceled` |
| `StreamIterator<T>` | 拉模式：`moveNext()` 首调才 listen；事件写 `Channel<T>`（无上限）；`get T current`；`reset()` 空实现（流不可重放） |
| `abstract StreamTransformer<S,T>` | 流到流变换器：`abstract Stream<T> apply( Stream<S> source )`（`bind` 为 SL 保留 token，方法名改用 apply） |

### 2.4 同批交付的 L0 / L2

`Core/IO/ByteStream.sl`：`abstract ByteStream`（`read(ByteBuf)`/`write(ByteBuf)`/`flush`/`seek`/能力位与水位 setter，`static memory()` 工厂）+ `MemoryStream`（`toByteBuf`/`toArray`/`reset` 往返）。`TransformStream`/`HashingStream` 装饰器未落地（P3）。

`Core/IO/Codec.sl`：`abstract Converter<S,T>`（`convert` + `startChunkedConversion` + `fuse<M>` 组合）+ `abstract Codec<S,T>`（`encoder`/`decoder` + `encode`/`decode` + `fuseCodec<M>`）+ `abstract ChunkedConversionSink<T>`（`add`/`close`）+ `_FusedConverter`/`_FusedCodec` 组合实现。

P2 交付的具体 codec 与配套工具（同目录，见 §5 CodecFrameTest）：

| 文件 | 类 | 职责 |
|------|-----|------|
| `Core/IO/Encoding.sl` | `Utf8Codec extends Codec<string, Array<UInt8>>` | string ↔ UTF-8 字节 |
| `Core/IO/JsonCodec.sl` | `JsonCodec<T> extends _JsonCodecBase<T, string>` | T ↔ JSON 文本 |
| `Core/IO/BinaryCodec.sl` | `BinaryCodec extends Codec<object, ByteBuf>` | object ↔ 二进制帧 |
| `Core/IO/ProtoCodec.sl` | `ProtoCodec<T> extends _ProtoCodecBase<T, ByteBuf>` | T ↔ protobuf 线格式（复用 LengthPrefix 分帧） |
| `Core/IO/Serialize.sl` | `Serialize extends Object` | 序列化门面：toBytes/fromBytes/toText/fromText 同步；toStream/fromStream 返回 Task；toElementStream 返回 Stream\<T\>（varint 分帧多元素流） |
| `Core/IO/LengthPrefix.sl` | `LengthPrefix extends Object` | 长度前缀分帧静态工具：帧 = `varuint(len)+payload`；半包时 tryReadFrame 返 null 且不消费 readerIndex（续接重试）；超限抛 `StreamIOError.FrameTooLarge` |

## 3. 用法

```sl
# 推模式：controller + listen（错误走 onError 不中断，close 触发 onDone）
var ctrl = StreamController<Int32>()
var s = ctrl.stream
s.listen( onData, onError, onDone, false )
ctrl.add( 1 )
ctrl.addError( "boom" )
ctrl.add( 2 )
ctrl.close()

# 惰性转换链：构建期间零求值，终端才驱动
Stream<Int32> src = Stream<Int32>.generate( 10, gen )
Stream<Int32> w = src.where( pred )            # 过滤偶数
Stream<Int32> m = w.map<Int32>( mapper )       # ×10
Stream<Int32> tk = m.take( 3 )                 # 取 3 个
Task t = tk.toList()
object r = Coroutine.awaitTask( t )
List<Int32> lst = r as List<Int32>             # 0, 20, 40

# 拉模式：iterator 挂起与恢复（协程内空通道 recv 挂起，add 后恢复）
var it = ctrl.stream.iterator
while it.moveNext()
{
    int x = it.current as int
}

# 背压：容量 1 的 controller，第二个 add 挂起直至被消费
var bounded = StreamController<Int32>( 1 )
```

## 4. 语义要点

- **单订阅**：一个流实例只允许 listen 一次；读取端经 `controller.stream` 取得，重复取同一实例。
- **惰性**：`map/where/take/skip` 只包装上游、不订阅；链构建期间回调零调用，终端（`listen`/`forEach`/`toList`/`iterator`）才驱动全链。
- **背压**：`StreamController` 默认无缓冲上限（add 永不挂起）；`_init_(capacity)` 指定容量后 add 满时挂起生产协程，消费后恢复——容量语义与 `Channel` 一致。
- **错误不中断**：`addError` 走 `onError` 回调（不走异常通道），数据流继续；`cancelOnError=true` 时错误后自动取消，后续事件不再分发。
- **cancel 级联**：`sub.cancel()` 粗暴关闭通道并置标记；生产协程对已关闭通道 `send` 抛异常终止（状态 Dead）。缓冲中的错误事件会随通道关闭丢弃（P4 改进点）。
- **take 语义**：取满 N 后 cancel 上游 + 调 onDone；`sub` 经闭包捕获延迟绑定（listen 返回后再赋值）。
- **拉模式短路**：`StreamIterator` 不透传错误事件，以 `moveNext()` 返回 false（current 为 null）短路。
- **语言设施适配**（契约偏差，见文件头注记）：回调类型统一用 `Function`；`StreamSubscription` 非泛型；事件信封 payload 收敛为 `object`（嵌套泛型无先例，且 `data` 是 SL 关键字不可作标识符，getter 名用 `payload`）；`done` 为轮询哨兵实现；闭包内访问 `this._x` 一律先拷局部变量再进闭包。
- **fromByteStream 桥接**：生产协程循环 `read(4096)` → `decoder.startChunkedConversion(sink)` 分块转换 → 写入 controller；`read` 异常（`try?` 返 null）转投 `StreamIOError.Timeout` 错误事件；`ByteBuf` 复用要求 decoder 实现在 `add` 内拷贝（分块所有权约定）。

## 5. 测试

`test/BaseTest/StreamTest.sl`（Core 宿主 `CSimpleVMCoreTest`，与 `CoroutineTest.sl`/`ByteBufTest.sl` 同列），15 项断言全绿：

| 组 | 覆盖 |
|----|------|
| C（3） | fromIterable+toList 取回、generate+forEach 顺序（first/增量/last）、empty 订阅即 done |
| C2（3） | 链构建零求值、where+map+take 结果（0/20/40）、周期性流未提前求值 |
| D（4） | addError 传播且不中断、cancelOnError 停止分发、Stream.error 工厂、cancel 后无分发 |
| E（2） | 拉取协程空通道挂起与恢复、root 协程拉取生产流全量 |
| F（2） | 容量 1 背压（第二个 add 挂起、消费驱动生产完成）、无上限连发不挂起 |

`test/BaseTest/CodecFrameTest.sl`（同宿主），325 项断言全绿，A~I 九组（载荷 `data CodecUserData{uid,name,score}`，作 `Codec<T, String/ByteBuf>` 泛型实参）：

| 组 | 覆盖 |
|----|------|
| A | LengthPrefix 帧原语 |
| B | encodeSink + decodeStream |
| C | ProtoCodec |
| D | Utf8Codec roundtrip |
| E | BinaryCodec |
| F | JsonCodec roundtrip |
| G | Serialize 门面 |
| H | bindStream |
| I | Stream.periodic |
