# Stream 设计文档（simple_language）· v2 总纲

> 版本：**v2**（在 v1 基础上重构为「字节流 / 元素流 / 编解码」三层模型）
> 状态：规划（未落地）
> 触发需求（`test/未解决问题.txt` L19-20、L37）：
> - 「提供一个 Stream 的 abstract 类，用来存放缓存数据，未来 ProtoBuf、序列化，可能都要用这个类，**尤其是一些内置的 system_method_call 一定要提供成共用的**」
> - 「增加 protobuf、字节码序列化相互转化过程，stream 相关的类」
> - 「再增加一些 ErrorShow, stream, **netStream**, buffer, builder」
>
> 依赖现状（已落地，可直接复用）：
> - `source/Front/Lib/Core/Container/Channel.sl` — CSP 通道，`SystemChannel*` 6 个 syscall（容量 / 背压 / 协程挂起）
> - `source/Front/Lib/Core/Coroutine.sl` — `Coroutine`(别名 `coro`) / `Task`，30 个 syscall；`CoroutineBlockReason.IO = 5` **已预留**
> - `source/Front/Lib/Core/IteratorInterface.sl` — `IIterable<T>` / `IIterator<T>`
> - `source/Front/Lib/Std/Isolate/{Isolate,SendPort,ReceivePort,TransferableData}.sl` — 30 个 syscall，含 `SystemTransfer*` 零拷贝
> - `csimple_lang/src/base/bytearray.{c,h}` — **C 层 ByteArray 已实现**（275 行，含 LE/BE 读取）
>
> 风格约定：**入口 / 回调一律使用函数值**（函数变量 / `Func<>` / 匿名闭包），与 `ISOLATE_DESIGN.md` §5.2.1 一致；不使用方法名字符串。
> 硬规则：新增系统方法遵循 `AGENT.md` R7 **四处同步**（`.sl` 声明 + `<Lib>.jsonc` 的 `systemCalls[].cvmFunction` + `src/vm/system_method_call/*.c` + `Front/Define.cs` 的 `ESystemMethodCall`）；新 `.c` 文件必须登记 `CMakeLists.txt` 的 `CSIMPLE_LIB_SOURCES`（R6）。

---

## 0. v1 → v2 变更摘要

| 维度 | v1 | v2（本文） |
|------|----|-----------|
| 模型 | 单层 `Stream<T>`（元素流） | **三层**：L0 字节流 / L1 元素流 / L2 编解码 |
| 缓存数据载体 | 无（直接 `Channel<T>`） | ★ `ByteBuf`（Netty 风格双索引缓冲）作为**唯一共用缓存载体** |
| 字节语义 | 只出现在 `TcpStream extends Stream<ByteArray>` | ★ `ByteStream` 抽象类 + 能力位（Read/Write/Seek/Timeout/Duplex/Message） |
| 序列化 / ProtoBuf | 未涉及 | ★ §8 完整设计：`Codec` 体系 + ProtoBuf wire format + varint/zigzag **共用原语** |
| 网络 | Tcp/Udp 两类 | ★ `NetStream` 统一基类 + Tcp / Udp / **Tls** / **WebSocket** / HttpBody |
| WebSocket | 未涉及 | ★ §10 专项：握手 / 帧 / **消息流与字节流双视图** / ping-pong / close 协商 |
| 共用契约 | 散落 | ★ §15「共用 system_method_call 契约」：命名规范 + 原语清单 + 复用矩阵 |
| 文件 / 压缩 / 加密 | 未涉及 | ★ `FileStream` / `TransformStream`（GZip、Lz4、AES）/ `HashStream` |
| 系统性缺口 | 未识别 | ★ §2.3 指出 `ByteArray` 在 SL 层**完全缺失**，是 P0 前置项 |

> v1 的有效内容（协程驱动、容量背压、跨 isolate 桥接、Tcp/Udp 双模式、A~J 测试用例）**全部保留**并按新模型重编章节。

---

## 1. 目标、范围与设计原则

### 1.1 一句话目标

提供一个 **abstract 的流体系**，使其同时胜任四件事：

1. **异步元素序列**（Dart `Stream<T>` 风格）：协程驱动的惰性流水线。
2. **共用缓存载体**：ProtoBuf、二进制序列化、Json/Xml 文本序列化共用同一套字节缓冲与读写原语。
3. **统一 I/O 抽象**（C# `Stream` 风格）：文件 / 网络 / 内存 / 压缩 / 加密全部是「流」，可装饰器叠加。
4. **网络流家族**（`NetStream`）：Tcp / Udp / Tls / WebSocket / HttpBody 共用一套句柄模型、事件循环、背压与错误语义。

### 1.2 四条设计原则（按优先级）

| # | 原则 | 含义 |
|---|------|------|
| **P1** | **共用优先于专用** | 凡是「读写字节 / 缓冲 / 编解码」的能力，一律下沉为**共用 syscall 原语**，不允许 ProtoBuf、WebSocket、文件各自造一套 |
| **P2** | **组合优于继承** | 用装饰器（`FileStream → GZipDecode → ProtoDecode → Stream<T>`）而非庞大的基类 |
| **P3** | **协程优先，root 可退化** | 所有阻塞点默认挂起协程（`CORO_BLOCK_IO`）；root 上下文退化为真实阻塞，与 `Channel.recv` 行为一致 |
| **P4** | **零 C 改动优先** | v1 结论延续：能用 SL 层组合实现的，不新增 syscall；只有**热路径原语**（字节读写、varint、CRC、压缩）才下沉 C |

### 1.3 范围边界

**纳入**：`ByteBuf`、`ByteStream` 及子类、`Stream<T>` 及子类、`Codec` 体系、ProtoBuf、WebSocket、`NetStream` 家族、文件流、压缩/加密变换流、共用 syscall 契约。

**不纳入**：HTTP 服务端路由（`Std/Net/Route.sl` 已有）、Sqlite 结果集流式化（P4 可选）、音视频媒体流（依赖 ffmpeg，见 `未解决问题.txt` L24）。

---

## 2. 现状盘点与缺口

### 2.1 已有机制（可复用）

| 能力 | 现状 | Stream 体系如何复用 |
|------|------|---------------------|
| 缓冲 / 容量 / 背压 | `Channel<T>` + `SystemChannel{Create,Send,Recv,Close,Count,IsClosed}` | L1 元素流的背压底座 |
| 协程挂起 / 唤醒 | `Coroutine` 30 个 syscall；`CoroutineBlockReason.IO=5`；`reexecute` 约定 | 所有阻塞点统一挂起 |
| 迭代器契约 | `IIterable<T>` / `IIterator<T>`（List/Set/Map/Queue/Array/Range/Tree 已实现） | `Stream<T>` 实现 `IIterable<T>` |
| 跨 isolate | `SendPort.send` / `ReceivePort.recv` / `TransferableData` | 元素与 `ByteBuf` 的跨岛桥接、零拷贝 |
| C 层字节容器 | `src/base/bytearray.c`（`ByteArray*`，2 倍扩容，LE/BE 读取） | `ByteBuf` 的底层存储 |
| 错误类型 | `Core/Error.sl`、`Core/Result.sl` | 统一流错误类型树（§14） |

### 2.2 各语言 Stream 定位（对齐表）

| 语言 | 本质 | 本设计对应 |
|------|------|-----------|
| Dart | `Stream<T>` 异步事件序列 + `StreamController` + `dart:convert` 的 `Codec` | **L1 `Stream<T>`** + **L2 `Codec<S,T>`** |
| C# | `abstract Stream` 字节流（CanRead/CanWrite/CanSeek）+ 装饰器 | **L0 `ByteStream`** + 能力位 + 变换流 |
| Java | `Stream<T>` 惰性函数式流水线 | L1 的 `map/where/take/reduce` |
| Netty | `ByteBuf` 双索引 + 零拷贝 slice | **L0 `ByteBuf`** |
| Go | `io.Reader/Writer` 极小接口 + 组合 | L0 接口切分（Source/Sink） |
| Swift | `AsyncStream` 闭包 `yield` | `Stream.generate(closure)` |
| gRPC / ProtoBuf | 字节流上的 wire format 编解码 | L2 `ProtoCodec` + 共用 varint 原语 |

### 2.3 缺口清单（★ = 阻塞项）

| 缺口 | 现状 | 影响 | 优先级 |
|------|------|------|--------|
| ★ **SL 层无 `ByteArray` / `ByteBuf`** | C 层 `bytearray.c` 已就绪，SL 层**零绑定、零 syscall** | 整个 L0 层无法落地；v1 里 `Stream<ByteArray>` 是空中楼阁 | **P0** |
| ★ 无字节读写共用原语 | 只有零散 `SystemPtrReadByte` / `SystemMemoryNativeReadU8` | ProtoBuf / 序列化无处复用 | **P0** |
| 无流式文件 API | `File` 只有 `readAllText/writeAllText/appendText` | `FileStream` 需新建 `SystemFileOpen/Read/...` | P1 |
| 网络全是空壳 | `Std/Net/{TcpSocket,UdpSocket}.sl` 各 ~21 B（`UdpSocket.sl` 内部 namespace 还写成 `Http`，笔误）；`HttpClient.sl` 是手写 stub；C 层 **libuv 只声明依赖、`src/` 零接入** | `NetStream` 家族需从零 | P1 |
| 无序列化 / ProtoBuf | 全仓仅 2 处规划文本 | §8 | P2 |
| 无 WebSocket | 仅 `扩展库.md:81` 一句规划 | §10 | P2 |
| 压缩 / 编码空壳 | `Core/Text/{Encoding,Lz4}.sl`、`Std/Zip/{GZip,Zip}.sl` 均 25~30 B 空壳 | 变换流与 `Codec` 无实现 | P3 |
| 无 `md/syntax/` 篇章 | 53 篇中无 stream / byte / io / net / serialize | 落地后补 + 登记 `md/INDEX.md`（R13） | P3 |

---

## 3. 三层模型总览

```
┌──────────────────────────────────────────────────────────────────────┐
│ L2  编解码层  Codec<S,T> / Encoder / Decoder                           │
│     ProtoBuf · Binary · Json · Xml · Base64 · Encoding · GZip · Aes    │
│     （纯函数式：ByteBuf ⇄ 对象；不感知数据来源）                        │
└───────────────▲──────────────────────────────┬───────────────────────┘
                │ decode(ByteBuf)              │ encode(obj)→ByteBuf
┌───────────────┴──────────────────────────────▼───────────────────────┐
│ L1  元素层   abstract Stream<T>  （Dart 风格异步序列）                 │
│     StreamController · StreamSubscription · StreamTransformer          │
│     map/where/take/skip/flatMap/reduce · listen/forEach/toList         │
│     背压 = Channel<T> 容量                                             │
└───────────────▲──────────────────────────────┬───────────────────────┘
                │ Stream.fromByteStream(...)   │ pipeTo / fromReceivePort
┌───────────────┴──────────────────────────────▼───────────────────────┐
│ L0  字节层   ByteBuf（缓存载体）+ abstract ByteStream（I/O 抽象）       │
│     MemoryStream · FileStream · NetStream(Tcp/Udp/Tls/WebSocket/Http) │
│     TransformStream(GZip/Lz4/Aes) · HashStream · StdIn/StdOut          │
│     能力位：Read/Write/Seek/Timeout/Duplex/MessageBoundary             │
│     背压 = ByteBuf 高/低水位 + Channel 等待队列                         │
└───────────────▲──────────────────────────────────────────────────────┘
                │ 共用 syscall 原语（§15）
┌───────────────┴──────────────────────────────────────────────────────┐
│ C VM  vm_bytebuf_* · vm_sys_bytestream_* · vm_sys_file_* · vm_sys_net_*│
│       vm_sys_ws_* · vm_sys_codec_*（varint/zigzag）· vm_sys_hash_*     │
│       vm_sys_compress_*      ← 全部围绕 ByteBuf，跨领域共用            │
└──────────────────────────────────────────────────────────────────────┘
```

### 3.1 为什么必须分三层（关键论证）

- **ProtoBuf / 序列化关心的是 L0 + L2**：它们要的是「往一个缓冲区里按 wire format 写 varint」，既可能写到内存（`MemoryStream`），也可能写到 socket（`NetStream`）。若只有 L1 的 `Stream<T>`（元素流），序列化就必须为「内存 / 文件 / 网络」各写一份 → 违背 P1 共用原则。
- **网络 / 文件关心的是 L0**：它们只认识字节，不认识 `T`。
- **业务流水线关心的是 L1**：`where/map/take`。
- **L2 是纯函数**：`Codec` 不持有 I/O 句柄 → 可跨 isolate 发送、可单测、可组合（`fuse`）。

> 结论：**`Stream<T>` 不是被 ProtoBuf「直接使用」，而是 ProtoBuf 通过「L0 字节流 + L2 Codec」与 L1 桥接**。这三层的接缝就是本文档的核心。

### 3.2 全景类图

```
                              Object
                                 │
    ┌────────────────────────────┼─────────────────────────────┐
    │                            │                             │
 ByteBuf                  abstract ByteStream          abstract Stream<T>
 (缓存载体)                (字节 I/O 抽象)              (元素流, : IIterable<T>)
    │                            │                             │
    │            ┌───────────────┼───────────────┐             ├─ _BaseStream<T>
    │            │               │               │             │    ├─ ControllerStream<T>
    │      MemoryStream   TransformStream     NetStream         │    ├─ BroadcastStream<T>
    │                       (GZip/Lz4/Aes)       │              │    └─ GeneratedStream<T>
    │                            │               │              ├─ FromIterableStream<T>
    │                       HashStream     ┌─────┼─────┐        ├─ FromChannelStream<T>
    │                       (CRC/SHA)      │     │     │        ├─ FromByteStream<T> ★桥接
    │                                  TcpStream │     │        ├─ FromReceivePortStream<T>
    │                                  UdpStream │     │        └─ MapStream/WhereStream/...
    │                                  TlsStream │     │                  (惰性转换)
    │                              WebSocketStream     │
    │                                  HttpBodyStream  │
    │                                                  └ FileStream / StdInStream / StdOutStream
    │
    └─ 被 L2 Codec 消费：Codec<ByteBuf,T> / Codec<Array<UInt8>,T>
         ProtoCodec · BinaryCodec · JsonCodec · XmlCodec · Base64Codec · EncodingCodec
```

---

## 4. L0 字节层

### 4.1 `ByteBuf` — 共用缓存载体（★ P0）

> **这是用户所述「用来存放缓存数据」的核心类型。** 设计目标：ProtoBuf、序列化、网络收发、文件读写、压缩解压**全部只认这一个类型**，从而让 C 侧的读写原语可以 100% 共用。

采用 **Netty 风格双索引**（`readerIndex` / `writerIndex`），比单纯的 `ByteArray` 更适配流式场景：

```
 0                  readerIndex      writerIndex            capacity
 ├── 已读区(discard) ──┼── 可读区(readable) ──┼── 可写区(writable) ──┤
```

**为什么双索引而不是单个 `ByteArray` + offset 参数**：
1. 半包场景（TCP）：多次 `read` 累积写入，一次消费 → 无需调用方自己维护 offset。
2. 零拷贝：`slice()` / `duplicate()` 共享底层存储，压缩/加密变换流可零拷贝串联。
3. `discardReadBytes()` 提供滑动窗口压缩，长连接不会无限膨胀。

```sl
#!
 * Core/IO/ByteBuf.sl
 * 共用字节缓冲区：ProtoBuf / 序列化 / 网络 / 文件 / 压缩 的唯一字节载体。
 * 本体在 C VM 端（VMByteBuf 注册表），SL 对象仅持有 Int64 句柄 _bid。
 !#
public class ByteBuf extends Object
{
    Int64 _bid = 0

    # ── 构造 ──
    _init_()                                        # 默认初始容量 512
    _init_( Int32 initialCapacity )
    static ByteBuf fromBytes( Array<UInt8> bytes )  # 深拷贝
    static ByteBuf wrap( Array<UInt8> bytes )       # 零拷贝包装（不复制，容量=长度）
    static ByteBuf fromString( string s )           # UTF-8
    static ByteBuf fromHex( string hex )

    # ── 三区索引 ──
    get Int32 readerIndex()
    set Int32 readerIndex( Int32 idx )              # 越界抛 IndexOutOfRange
    get Int32 writerIndex()
    set Int32 writerIndex( Int32 idx )
    get Int32 capacity()
    get Int32 readableBytes()                       # writerIndex - readerIndex
    get Int32 writableBytes()                       # capacity - writerIndex
    get Int32 maxCapacity()                         # 默认 Int32.Max，可在构造时限制

    # ── 容量管理 ──
    void ensureWritable( Int32 minWritableBytes )   # 不足则 2 倍扩容（可超 maxCapacity 抛异常）
    void discardReadBytes()                         # 丢弃已读区，前移剩余（滑动窗口）
    void clear()                                    # readerIndex=writerIndex=0（不释放内存）
    void shrink()                                   # 缩容到 readableBytes

    # ── 读原语（推进 readerIndex；可读不足抛 BufferUnderflow）──
    UInt8 readU8()
    Int16 readI16Le();  Int16 readI16Be()
    UInt16 readU16Le(); UInt16 readU16Be()
    Int32 readI32Le();  Int32 readI32Be()
    UInt32 readU32Le(); UInt32 readU32Be()
    Int64 readI64Le();  Int64 readI64Be()
    UInt64 readU64Le(); UInt64 readU64Be()
    Float32 readF32Le(); Float32 readF32Be()
    Float64 readF64Le(); Float64 readF64Be()
    Bool readBool()

    # ── ★ 变长整数原语（ProtoBuf / 二进制序列化共用，见 §8.2）──
    Int64 readVarUint()                             # LEB128 无符号
    Int64 readVarInt()                              # LEB128 + ZigZag 有符号
    Int32 readFixedVarUint()                        # 定长变体（可选优化）

    # ── 批量读 ──
    void readBytes( ByteBuf dst, Int32 len )
    Array<UInt8> readByteArray( Int32 len )
    string readString( Int32 len )                  # UTF-8
    string readString( Int32 len, Encoding enc )
    string readLine()                               # 含 '\n'，无行则返回 null
    void skipBytes( Int32 len )

    # ── 写原语（推进 writerIndex；可写不足自动扩容）──
    void writeU8( UInt8 v )
    void writeI16Le( Int16 v );  void writeI16Be( Int16 v )
    void writeU16Le( UInt16 v ); void writeU16Be( UInt16 v )
    void writeI32Le( Int32 v );  void writeI32Be( Int32 v )
    void writeU32Le( UInt32 v ); void writeU32Be( UInt32 v )
    void writeI64Le( Int64 v );  void writeI64Be( Int64 v )
    void writeU64Le( UInt64 v ); void writeU64Be( UInt64 v )
    void writeF32Le( Float32 v );void writeF32Be( Float32 v )
    void writeF64Le( Float64 v );void writeF64Be( Float64 v )
    void writeBool( Bool v )

    # ── ★ 变长整数写入 ──
    void writeVarUint( Int64 v )
    void writeVarInt( Int64 v )                     # ZigZag 后 LEB128

    # ── 批量写 ──
    void writeBytes( ByteBuf src )                  # 写入 src 全部可读区
    void writeBytes( ByteBuf src, Int32 len )
    void writeByteArray( Array<UInt8> bytes )
    void writeString( string s )                    # UTF-8
    void writeString( string s, Encoding enc )

    # ── 零拷贝视图 ──
    ByteBuf slice()                                 # 当前可读区视图，共享底层，独立索引
    ByteBuf slice( Int32 index, Int32 length )
    ByteBuf duplicate()                             # 共享底层，独立双索引
    ByteBuf copy()                                  # 深拷贝

    # ── 查找 / 比较 ──
    Int32 indexOf( UInt8 b )                        # 从 readerIndex 起，未找到返回 -1
    Int32 indexOf( ByteBuf pattern )
    Bool startsWith( ByteBuf prefix )
    Bool equals( ByteBuf other )                    # 可读区逐字节比较

    # ── 与 isolate 互操作（★ 零拷贝，见 §12）──
    TransferableData toTransferable()               # 转移所有权，本 buf 失效（isReleased=true）
    static ByteBuf fromTransferable( TransferableData td )
    get bool isReleased()

    # ── 转换 ──
    Array<UInt8> toArray()                          # 可读区拷贝
    string toHex()
    override string toString()                      # 可读区按 UTF-8 解码
    Int32 hashCode()
}
```

**关键语义约定**：

| 约定 | 说明 |
|------|------|
| 越界 | 读越界抛 `BufferUnderflow`；写自动扩容（受 `maxCapacity` 限制） |
| 索引独立 | `slice()` / `duplicate()` 共享底层数组 → **禁止跨 isolate 发送**（见 §12.2） |
| 释放 | `toTransferable()` 后 `isReleased=true`，任何读写抛 `BufferReleased` |
| 字节序 | 默认 **小端（LE）**；网络协议用 `Be` 后缀（ProtoBuf 用 varint，与字节序无关） |
| 线程 | 与 `Channel` 一致：**仅 VM 线程内使用**，isolate 间靠 `toTransferable` 转移 |

### 4.2 `abstract ByteStream` — 统一 I/O 抽象（C# `Stream` 风格）

```sl
#!
 * Core/IO/ByteStream.sl
 * 字节流统一抽象（能力位 + 协程挂起）。
 * 所有阻塞方法：协程上下文挂起（CORO_BLOCK_IO）；root 上下文退化为真实阻塞。
 !#
public enum SeekOrigin extends Int32 { Begin = 0, Current = 1, End = 2 }

public abstract class ByteStream extends Object
{
    # ── 能力协商（C# CanRead/CanWrite/CanSeek 的扩展）──
    get bool canRead()              # 可读（单向流可为 false）
    get bool canWrite()             # 可写
    get bool canSeek()              # 可随机访问（socket / pipe 为 false）
    get bool canTimeout()           # 支持读写超时
    get bool isDuplex()             # 读写独立，支持半关闭（socket）
    get bool isMessageOriented()    # ★ 保留消息/帧边界（UDP 数据报、WebSocket 消息）

    # ── 读 ──
    # 返回实际读入字节数；0 = EOF；负值 = 错误（协程内改为抛异常，见 §14）
    Int32 read( ByteBuf dst )                       # 尽量读满 dst 可写区（至少 1 字节，除非 EOF）
    Int32 readAtLeast( ByteBuf dst, Int32 n )       # 挂起直到 ≥ n 字节或 EOF
    ByteBuf readExactly( Int32 n )                  # 恰好 n 字节；不足抛 UnexpectedEof
    Int32 skip( Int32 n )                           # 丢弃 n 字节，返回实际
    ByteBuf readAll( Int32 maxBytes = 0 )           # 读到 EOF（0 = 不限），慎用于无限流

    # ── 写 ──
    void write( ByteBuf src )                       # 挂起直到全部写出（写背压）
    void writeByte( UInt8 b )
    void flush()                                    # 冲刷用户态缓冲（不保证落盘）

    # ── 位置（canSeek == false 时抛 NotSupported）──
    get Int64 position()
    set Int64 position( Int64 pos )
    get Int64 length()
    Int64 seek( Int64 offset, SeekOrigin origin )

    # ── 生命周期 ──
    void closeRead()                                # 半关闭读侧（isDuplex 才有意义）
    void closeWrite()                               # 半关闭写侧 + flush（TCP FIN）
    void close()                                    # 全关闭
    get bool isClosed()
    get bool isEof()                                # 读侧已到 EOF

    # ── 超时（canTimeout）──
    set Int64 readTimeoutMs( Int64 ms )             # 0 = 无限；超时抛 TimeoutError
    set Int64 writeTimeoutMs( Int64 ms )

    # ── 背压 / 水位（见 §13）──
    set Int32 readHighWaterMark( Int32 bytes )      # 读缓冲达到此值暂停上游（TCP: uv_read_stop）
    set Int32 readLowWaterMark( Int32 bytes )       # 降到此值恢复
    set Int32 writeHighWaterMark( Int32 bytes )     # 待写队列达到此值 write() 挂起
    get Int32 pendingWriteBytes()

    # ── ★ 与 L1 元素层的桥接（见 §6.5）──
    Stream<T> asStream<T>( Codec<ByteBuf,T> codec, Int32 bufferSize = 8192 )
    Stream<ByteBuf> asChunkStream( Int32 chunkSize = 8192 )

    # ── 工厂 ──
    static ByteStream memory()                      # → MemoryStream
    static ByteStream memory( ByteBuf initial )
    static ByteStream wrapReadOnly( ByteBuf buf )   # 只读包装（序列化读取场景）
}
```

### 4.3 `MemoryStream` / 变换流 / 哈希流

```sl
# 内存流：序列化最常用的目标（ProtoBuf encode → MemoryStream → toArray）
public class MemoryStream extends ByteStream
{
    _init_()
    _init_( Int32 initialCapacity )
    _init_( ByteBuf backing )                       # 复用外部 ByteBuf（避免二次拷贝）
    ByteBuf toByteBuf()                             # 返回内部缓冲（不拷贝）
    Array<UInt8> toArray()
    void reset()                                    # 清空并复位索引
}

# 变换流：装饰器基类 —— 输入字节流经 transform 后输出（压缩 / 解压 / 加密 / 解密）
public abstract class TransformStream extends ByteStream
{
    _init_( ByteStream downstream, Int32 bufferSize = 8192 )
    protected abstract void transform( ByteBuf src, ByteBuf dst )   # 子类实现
    protected ByteStream downstream()               # 下游（可为另一个 TransformStream → 链式）
    Task flushAndFinish()                           # 冲刷变换器剩余输出（GZip finish / AES final）
}

public class GZipEncodeStream extends TransformStream    # 写侧压缩
public class GZipDecodeStream extends TransformStream    # 读侧解压
public class Lz4EncodeStream  extends TransformStream
public class Lz4DecodeStream  extends TransformStream
public class AesEncodeStream  extends TransformStream    # 需 key/iv/mode
public class AesDecodeStream  extends TransformStream

# 旁路哈希流：只透传数据，同时累计摘要（校验场景，不改变数据）
public class HashingStream extends ByteStream
{
    _init_( ByteStream downstream, HashAlgo algo )  # CRC32 / Adler32 / MD5 / SHA1 / SHA256
    ByteBuf digest()                                # 结束时取摘要
    get Int64 bytesHashed()
}
```

### 4.4 `FileStream`

现状 `Std/IO/File.sl` 只有整文件文本 API；新增流式：

```sl
public enum FileMode  extends Int32 { Read=1, Write=2, Append=4, ReadWrite=3, Create=8, Truncate=16 }
public enum FileAccess extends Int32 { Read=1, Write=2, ReadWrite=3 }

public class FileStream extends ByteStream
{
    _init_( string path, FileMode mode )
    _init_( string path, FileMode mode, FileAccess access )
    _init_( string path, FileMode mode, FileAccess access, Int32 bufferSize )

    void flushToDisk()                              # fsync（区别于 flush）
    void lock( Int64 offset, Int64 length )         # 可选（P3）
    void unlock( Int64 offset, Int64 length )
    get string path()
    get bool canSeek()                              # override: true
}

# File 类扩展（向后兼容，保留原 8 个静态方法）
public class File extends Object
{
    static FileStream openRead( string path )
    static FileStream openWrite( string path )
    static FileStream open( string path, FileMode mode )
    static Array<UInt8> readAllBytes( string path )          # 内部走 FileStream
    static bool writeAllBytes( string path, Array<UInt8> bytes )
    # ... 原有 exists/delete/copy/move/getSize/readAllText/... 不变
}
```

### 4.5 标准输入 / 输出流

```sl
public class StdInStream  extends ByteStream     # Console/stdin，canWrite=false, canSeek=false
public class StdOutStream extends ByteStream     # Console/stdout，canRead=false, canSeek=false
public class StdErrStream extends ByteStream
```
> 复用现有 `SystemPrint` / `SystemPrintln` / `SystemReadLine`，但统一到 `ByteStream` 后，`Json.encode(obj, stdout)` 这类写法即可成立（序列化目标从「文件」泛化为「任意流」）。

---

## 5. L0 具体子类清单（速查）

| 类 | 模块 | canRead | canWrite | canSeek | isDuplex | isMessage | 背压 | 分期 |
|----|------|---------|----------|---------|----------|-----------|------|------|
| `MemoryStream` | Core/IO | ✔ | ✔ | ✔ | ✘ | ✘ | 无（内存） | P1 |
| `FileStream` | Std/IO | 视 mode | 视 mode | ✔ | ✘ | ✘ | 写队列水位 | P2 |
| `StdIn/StdOut/StdErrStream` | Std/IO | 单向 | 单向 | ✘ | ✘ | ✘ | 无 | P2 |
| `TcpStream` | Std/Net | ✔ | ✔ | ✘ | ✔ | ✘ | **真背压**（暂停读） | P2 |
| `UdpStream` | Std/Net | ✔ | ✔ | ✘ | ✘ | **✔** | 无（丢包策略） | P2 |
| `TlsStream` | Std/Net | ✔ | ✔ | ✘ | ✔ | ✘ | 同 Tcp | P3 |
| `WebSocketStream` | Std/Net | ✔ | ✔ | ✘ | ✔ | **✔** | 帧级 + 消息级 | P3 |
| `HttpBodyStream` | Std/Net | ✔ | ✔(请求) | 通常 ✘ | ✘ | ✘ | chunked 流控 | P3 |
| `TransformStream` | Std/Zip,Core/Crypto | 视下游 | 视下游 | ✘ | ✘ | ✘ | 继承下游 | P3 |
| `HashingStream` | Core/Crypto | ✔ | ✔ | 继承 | 继承 | 继承 | 继承 | P3 |

---

## 6. L1 元素层：`abstract Stream<T>`（Dart 风格）

> v1 §5 的 API 基本保留，此处补充 **StreamTransformer**、**StreamSink**、**与 L0 的桥接**、**asBroadcast**。

### 6.1 抽象接口

```sl
public abstract class Stream<T> extends Object interface Core.IIterable<T>
{
    # ── 终态查询 ──
    get bool isDone()          # 已正常完成（close）
    get bool isErrored()       # 以错误结束
    get bool isClosed()        # 进入终态（Done 或 Errored）
    get bool isBroadcast()     # 是否多订阅流
    get bool isPaused()        # 订阅端是否暂停（背压软控制）

    # ── 订阅（推模式）──
    StreamSubscription<T> listen(
        Func<void,T> onData,
        Func<void,Error> onError = null,
        Func<void> onDone = null,
        Bool cancelOnError = false )

    # ── 拉模式（协程内）──
    override Core.IIterator<T> iterator()          # 返回 StreamIterator

    # ── 终端操作（内部 spawn 协程，返回 Task 可 await）──
    Task forEach( Func<void,T> action )
    Task toList( List<T> out )
    Task reduce( object seed, Func<object,object,T> combine )
    Task<Int64> count()
    Task<T> first()                                # 空流抛 StateError
    Task<T> last()
    Task<Bool> any( Func<bool,T> pred )
    Task<Bool> every( Func<bool,T> pred )

    # ── 惰性转换（返回新 Stream，不立即执行）──
    Stream<U> map<U>( Func<U,T> fn )
    Stream<T> where( Func<bool,T> pred )
    Stream<T> take( Int32 n )
    Stream<T> skip( Int32 n )
    Stream<U> flatMap<U>( Func<Stream<U>,T> fn )
    Stream<U> expand<U>( Func<IIterable<U>,T> fn )         # flatMap 的同步版
    Stream<T> distinct()                                    # P3
    Stream<U> scan<U>( U seed, Func<U,U,T> combine )        # P3（流式 fold）
    Stream<T> transform<U>( StreamTransformer<T,U> tf )     # ★ Dart 风格通用转换
    Stream<T> timeout( Int64 millis, Func<T> onTimeout )    # P3

    # ── 多订阅 ──
    Stream<T> asBroadcast()                                 # 单订阅 → 广播（P3）

    # ── 跨 isolate 桥接（源端）──
    Task pipeTo( SendPort port )

    # ── 工厂 ──
    static Stream<T> fromIterable( Core.IIterable<T> src )
    static Stream<T> fromChannel( Channel<T> ch )
    static Stream<T> fromReceivePort( ReceivePort rp )
    static Stream<T> generate( Func<void,StreamController<T>> producer )
    static Stream<T> periodic( Int64 millis, Func<T> tick )        # P2
    static Stream<T> fromByteStream<T>( ByteStream src, Codec<ByteBuf,T> codec, Int32 bufferSize = 8192 )  # ★ L0→L1 桥接
    static Stream<T> empty()
    static Stream<T> error( Error e )
}
```

### 6.2 `StreamController<T>` / `StreamSink<T>` / `StreamSubscription<T>`

```sl
public class StreamController<T> extends Object
{
    _init_()                        # 默认无界
    _init_( int capacity )          # 有界（背压）；建议 64

    Stream<T> get stream            # 单订阅；重复取抛 StreamAlreadyListened
    StreamSink<T> get sink          # ★ Dart 风格写入侧视图
    get bool isClosed()
    get bool isPaused()
    get bool hasListener()
    get Task done()                 # 流完成/出错的等待句柄

    void add( T value )             # 缓冲满则挂起当前协程；终态后抛 StreamClosed
    void addError( Error e )
    void addStream( Stream<T> src, Bool cancelOnError = true )   # 合流
    void close()
}

public class StreamSink<T> extends Object       # 写入侧抽象（ProtoBuf 编码后可 sink.add）
{
    void add( T value )
    void addError( Error e )
    void addStream( Stream<T> src )
    Task close()
    Task get done()
}

public class StreamSubscription<T> extends Object
{
    Task get consumer               # 消费协程；await 可等流耗尽/出错
    void cancel()                   # 协作取消：取消消费协程 + 关闭流
    void pause()                    # 暂停投递（背压软控制，缓冲继续累积）
    void resume()
    get bool isCancelled()
    get bool isPaused()
    void onData( Func<void,T> h )   # 可后续替换处理器
    void onError( Func<void,Error> h )
    void onDone( Func<void> h )
    Future asFuture<T>( T defaultValue )   # P3：若语言后续引入 Future；当前用 Task 替代
}
```

### 6.3 `StreamTransformer<S,T>`（★ Dart 风格的通用转换点）

> 这是「共用」的另一个关键接缝：**分包、解压、解码、协议解析全部实现成 Transformer**，可以任意串接，无需为每个协议新增 Stream 子类。

```sl
public abstract class StreamTransformer<S,T> extends Object
{
    # 把输入流 src 转换为输出流；cancelOnError 决定错误是否终止
    abstract Stream<T> bind( Stream<S> src )

    # 工厂：常见 transformer 由 Codec / 闭包构造
    static StreamTransformer<S,T> fromHandlers(
        Func<void,T, StreamSink<T>, S> handleData,
        Func<void, Error, StreamSink<T>> handleError = null,
        Func<void, StreamSink<T>> handleDone = null )

    static StreamTransformer<S,T> fromCodec( Codec<S,T> codec )
    static StreamTransformer<ByteBuf,T> fromDecoder( Decoder<ByteBuf,T> dec )
}

# 内置 transformer（P3，协议层共用）
public class ChunkTransformer  extends StreamTransformer<ByteBuf,ByteBuf>   # 定长切块
public class SplitTransformer  extends StreamTransformer<ByteBuf,ByteBuf>   # 按分隔符切分（解决 TCP 半包/粘包）
public class LengthPrefixCodec extends StreamTransformer<ByteBuf,ByteBuf>  # 长度前缀分帧（★ ProtoBuf/RPC 常用）
public class GZipTransformer   extends StreamTransformer<ByteBuf,ByteBuf>
public class Base64Transformer extends StreamTransformer<ByteBuf,ByteBuf>
```

### 6.4 惰性转换语义

- `map` / `where` / `take` / `skip` / `flatMap` **惰性**：只构造包装 `Stream`，不启动源。
- 只有**终端操作**（`listen` / `forEach` / `toList` / `reduce` / `iterator.moveNext`）才 `spawnClosure` 启动上游生产协程。
- 元素在协程里逐个流过 → 内存恒定，与 Java Stream 的惰性一致。

### 6.5 ★ L0 → L1 桥接（`fromByteStream`）

```sl
static Stream<T> fromByteStream<T>( ByteStream src, Codec<ByteBuf,T> codec, Int32 bufferSize = 8192 )
{
    # 内部：
    #   1. 建 StreamController<T>(capacity)
    #   2. spawn 协程：
    #        ByteBuf buf = ByteBuf(bufferSize)
    #        loop {
    #            Int32 n = src.read(buf)          # 协程挂起点（CORO_BLOCK_IO）
    #            if n <= 0 { ctrl.close(); break }        # EOF
    #            var decoder = codec.decoder.startChunkedConversion(ctrl.sink)
    #            decoder.add(buf.slice()); decoder.close()      # 逐帧解码，处理半包
    #        }
    #   3. 返回 ctrl.stream
}
```

> **半包处理**：解码器必须是**有状态的 chunked decoder**（Dart `ChunkedConversionSink` 思路），未消费完的尾部字节留在解码器内部缓冲，下一块到来时续接。这样 TCP 拆包、ProtoBuf 多消息连读、GZip 分块解压**共用同一套机制**。

### 6.6 类层级（更新版）

```
abstract Stream<T> : IIterable<T>
│
├─ abstract _BaseStream<T>                  # 持有 Channel<T> + 状态机
│     ├─ ControllerStream<T>                # StreamController.stream
│     ├─ BroadcastStream<T>                 # 多订阅（P3）
│     └─ GeneratedStream<T>                 # Stream.generate
│
├─ FromIterableStream<T>                    # List / IIterable（无需协程）
├─ FromChannelStream<T>                     # CSP ↔ Stream 桥接
├─ FromReceivePortStream<T>                 # 跨 isolate 重建
├─ FromByteStreamStream<T>                  # ★ L0 → L1（Codec 驱动）
│
├─ FromTcpStream          : Stream<ByteBuf>     # 由 TcpStream.asChunkStream() 产生
├─ FromWebSocketStream    : Stream<WsMessage>   # 由 WebSocketStream.messages() 产生（保留消息边界）
├─ FromProtobufStream<T>  : Stream<T>           # ★ 由 ProtoCodec 驱动（见 §8.5）
│
└─ 惰性转换包装：MapStream / WhereStream / TakeStream / SkipStream
                 / FlatMapStream / DistinctStream / ScanStream / TransformedStream
```

---

## 7. L2 编解码层：`Codec<S,T>`

> 直接对标 Dart `dart:convert`：**不持有 I/O 句柄的纯函数对象** → 可跨 isolate 发送、可 `fuse` 组合、易单测。

```sl
public abstract class Converter<S,T> extends Object
{
    abstract T convert( S input )
    abstract ChunkedConversionSink<S> startChunkedConversion( ChunkedConversionSink<T> sink )
    Converter<S,M> fuse( Converter<T,M> other )      # 串联：json.fuse(utf8)
}

public abstract class Codec<S,T> extends Converter<S,T>
{
    abstract Converter<S,T> get encoder()
    abstract Converter<T,S> get decoder()
    T encode( S input )                              # = encoder.convert
    S decode( T output )                             # = decoder.convert
    Codec<S,M> fuseCodec( Codec<T,M> other )
}

public abstract class ChunkedConversionSink<T> extends Object
{
    abstract void add( T chunk )
    abstract void close()
}
```

### 7.1 内置 Codec 一览

| Codec | S | T | 说明 | 分期 |
|-------|---|---|------|------|
| `Utf8Codec` | `string` | `Array<UInt8>` | 复用 `Core/Text/Encoding.sl`（当前空壳，需实现） | P2 |
| `Utf16Codec` / `Gb18030Codec` | `string` | `Array<UInt8>` | P3 |
| `Base64Codec` | `Array<UInt8>` | `string` | P3 |
| `HexCodec` | `Array<UInt8>` | `string` | P3 |
| `JsonCodec` | `object` | `string` | 包装现有 `Std/Text/Json.sl` + `Core/Text/BaseJson.sl` | P2 |
| `XmlCodec` | `object` | `string` | `Std/Text/Xml.sl`（空壳）需实现 | P3 |
| `CsvCodec` | `Table` | `string` | 复用 `Core/DataStruct/BaseCsv.sl` | P3 |
| `BinaryCodec` | `object` | `ByteBuf` | ★ 通用二进制序列化（§8.6） | P2 |
| `ProtoCodec<T>` | `T` | `ByteBuf` | ★ ProtoBuf（§8） | P2 |
| `GZipCodec` | `ByteBuf` | `ByteBuf` | 复用 zlib | P3 |
| `Lz4Codec` | `ByteBuf` | `ByteBuf` | 复用 `third_party/lz4` | P3 |

### 7.2 Codec 组合示例（体现"共用"）

```sl
# 内存：对象 → ProtoBuf 字节 → GZip → Base64 文本
string s = ProtoCodec.of( User.schema )
             .fuseCodec( GZipCodec() )
             .fuseCodec( Base64Codec() )
             .encode( user )

# 网络：WebSocket 二进制消息 → 解压 → ProtoBuf 解码 → 对象流
Stream<User> users = ws.messages()
                       .where( function(WsMessage m){ ret m.isBinary } )
                       .map( function(WsMessage m){ ret m.binary } )
                       .transform( GZipTransformer() )            # ByteBuf → ByteBuf
                       .transform( StreamTransformer.fromCodec( ProtoCodec.of(User.schema) ) )

# 文件：磁盘 → 解压 → 按行切分 → Json 解析
Stream<Config> cfgs = FileStream.openRead("a.jsonl.gz")
                        .asChunkStream( 65536 )
                        .transform( GZipTransformer() )
                        .transform( SplitTransformer( ByteArray("\n") ) )
                        .transform( StreamTransformer.fromCodec( JsonCodec.of<Config>() ) )
```

> 同一个 `ProtoCodec` 在**内存、文件、TCP、WebSocket、跨 isolate** 五处完全一致地复用 —— 这就是三层模型要达到的**共用目标**。

---

## 8. ProtoBuf 与序列化（★ 用户重点需求）

### 8.1 定位

ProtoBuf = **L2 的一个 `Codec`**，其下层只依赖 `ByteBuf`（L0），不依赖任何 I/O。因此：

```
encode: obj ──ProtoCodec.encode──▶ ByteBuf ──▶ MemoryStream / FileStream / TcpStream / WsStream
decode: ByteBuf ◀────────────────────────── MemoryStream / FileStream / TcpStream / WsStream
        └─ProtoCodec.decode─▶ obj   （或 chunked decoder → Stream<T>）
```

**被共用的部分**（写入 §15 的 syscall 原语）：
- varint / zigzag 编解码（`vm_sys_codec_varint_*`）—— ProtoBuf、自研 BinaryCodec、RPC 分帧**共用**
- 字节序读写（`vm_sys_bytebuf_*`）—— 所有二进制协议共用
- CRC32 / Adler（`vm_sys_hash_*`）—— 校验和、Zip、压缩共用
- 长度前缀分帧（`LengthPrefixCodec`）—— ProtoBuf 流式、WebSocket、RPC 共用

### 8.2 Wire Format（Proto3 子集，v1 范围）

| type | 含义 | 用于 |
|------|------|------|
| 0 | Varint | int32/int64/uint32/uint64/sint32/sint64/bool/enum |
| 1 | 64-bit | fixed64/sfixed64/double |
| 2 | Length-delimited | string/bytes/embedded messages/packed repeated |
| 5 | 32-bit | fixed32/sfixed32/float |

tag = `(field_number << 3) | wire_type`（varint 编码）

`sint32/sint64` 用 **ZigZag** 后再 varint：`(n << 1) ^ (n >> 31/63)`

### 8.3 Schema 描述与代码生成

```sl
# 方式 A：声明式 schema（推荐，可被反射/工具消费）
public class ProtoSchema extends Object
{
    static ProtoSchema of( string messageName )
    ProtoSchema field( Int32 number, string name, ProtoType type, Bool repeated = false, Bool packed = false )
    get ProtoField fieldByName( string name )
    get ProtoField fieldByNumber( Int32 number )
    get Array<ProtoField> fields()
}
public enum ProtoType extends Int32
{
    Double=1, Float=2, Int32=3, Int64=4, UInt32=5, UInt64=6,
    SInt32=7, SInt64=8, Fixed32=9, Fixed64=10, SFixed32=11, SFixed64=12,
    Bool=13, String=14, Bytes=15, Message=16, Enum=17
}

# 方式 B：注解驱动（对齐 md/syntax/attribute.md 的 @Serializable() 构想）
@Serializable( format = "proto" )
public class User extends Object
{
    @ProtoField(1) Int32 id = 0
    @ProtoField(2) string name = ""
    @ProtoField(3, packed = true) Array<Int32> tags = null
}
```

**代码生成**：由 Front 在编译期（Meta 层）读取 `@ProtoField` → 生成 `writeTo(ByteBuf)` / `readFrom(ByteBuf)` 方法体；**不生成运行时反射**，符合语言静态性。P2 提供生成器，P3 提供 `.proto` 导入。

### 8.4 `ProtoCodec<T>`

```sl
public class ProtoCodec<T> extends Codec<ByteBuf,T>
{
    static ProtoCodec<T> of( ProtoSchema schema )
    static ProtoCodec<T> of()                     # 走 @Serializable 生成的编解码器

    override T decode( ByteBuf bytes )            # 单条消息
    override ByteBuf encode( T value )

    # ★ 流式：长度前缀分帧的多消息流（RPC / 文件批量存储的标准做法）
    Stream<T> decodeStream( ByteStream src )      # = LengthPrefix + decode
    StreamSink<T> encodeSink( ByteStream dst )

    # 与 L1 直接桥接
    Stream<T> bind( Stream<ByteBuf> chunks )      # chunked decoder，处理半包
}
```

### 8.5 端到端示例

```sl
# ① 内存序列化（最常用）
MemoryStream ms = MemoryStream()
ProtoCodec<User>.of( User.schema ).encodeSink( ms ).add( user )
Array<UInt8> wire = ms.toArray()

# ② TCP 上收发 ProtoBuf 消息流（长度前缀分帧 + 半包续接）
TcpStream conn = Tcp.connectCoroutine( "127.0.0.1", 9000 )
Stream<User> incoming = ProtoCodec<User>.of( User.schema ).decodeStream( conn )
incoming.forEach( function( User u ) { handle(u) } ).awaitHandle()

# ③ 文件批量落盘（ProtoBuf 日志文件：varint 长度 + payload）
FileStream out = FileStream.openWrite( "users.pb" )
ProtoCodec<User>.of( User.schema ).encodeSink( out ).add( user )
out.close()

# ④ 跨 isolate 批量传输（零拷贝转移整个 ByteBuf）
MemoryStream ms2 = MemoryStream()
for User u in batch { ProtoCodec<User>.of(User.schema).encodeSink(ms2).add(u) }
port.send( ms2.toByteBuf().toTransferable() )        # 无深拷贝
# 接收端
ByteBuf b = ByteBuf.fromTransferable( msg as TransferableData )
Stream<User> users = ProtoCodec<User>.of(User.schema).bind( MemoryStream(b).asChunkStream() )

# ⑤ WebSocket 二进制通道
Stream<User> users = ProtoCodec<User>.of( User.schema ).bind(
    ws.messages().where( function(WsMessage m){ ret m.isBinary } ).map( function(WsMessage m){ ret m.binary } ) )
```

### 8.6 自研 `BinaryCodec`（非 ProtoBuf 场景）

> 与 ProtoBuf **共用 varint 与字节序原语**，但格式更简单（无 field number，按声明顺序紧凑编码），用于内部高频场景（如 VM 内的 IR 缓存、组件状态快照）。

```sl
public class BinaryCodec extends Codec<ByteBuf,object>
{
    static BinaryCodec of( Type t )            # 按字段声明顺序
    # 编码：string → varint(len) + utf8；Array → varint(n) + 元素；数值 → varint 或定长（可配）
}
```

### 8.7 序列化统一入口（★ 共用的最终形态）

```sl
public class Serialize extends Object        # Std/Text/Serialize.sl
{
    static ByteBuf   toBytes<T>( T obj, Codec<ByteBuf,T> codec )
    static T         fromBytes<T>( ByteBuf b, Codec<ByteBuf,T> codec )
    static string    toText<T>( T obj, Codec<string,T> codec )          # Json / Xml / Csv
    static T         fromText<T>( string s, Codec<string,T> codec )
    static Task      toStream<T>( T obj, ByteStream dst, Codec<ByteBuf,T> codec )
    static Task<T>   fromStream<T>( ByteStream src, Codec<ByteBuf,T> codec )
    static Stream<T> toElementStream<T>( ByteStream src, Codec<ByteBuf,T> codec )
}
```

---

## 9. `NetStream` 家族

> 用户点名「NetStream 一类」。定位：**`NetStream` = `ByteStream` 的网络特化抽象**，统一句柄模型、事件循环、背压、错误、超时、半关闭。所有具体协议（Tcp/Udp/Tls/WebSocket）继承它。

### 9.1 基类

```sl
#!
 * Std/Net/NetStream.sl
 * 网络字节流抽象：在 ByteStream 之上补充网络专有语义。
 * 句柄在 C VM 端（VMSocket 注册表），SL 仅持 Int64 _sid；不可跨 isolate（§12）。
 !#
public abstract class NetStream extends ByteStream
{
    Int64 _sid = 0

    # ── 地址信息 ──
    get string localAddress()
    get Int32  localPort()
    get string remoteAddress()
    get Int32  remotePort()

    # ── 连接状态 ──
    get bool isConnected()
    get bool isClosing()
    get Int64 bytesRead()
    get Int64 bytesWritten()
    get NetError lastError()

    # ── socket 选项（各协议按需支持，不支持抛 NotSupported）──
    void setNoDelay( Bool on )            # TCP Nagle
    void setKeepAlive( Bool on, Int64 idleSec )
    void setReuseAddr( Bool on )
    void setRecvBufferSize( Int32 bytes )
    void setSendBufferSize( Int32 bytes )

    # ── 两种编程模式（见 §9.2）──
    get NetMode mode()                    # Callback / Coroutine
}
public enum NetMode extends Int32 { Callback = 0, Coroutine = 1 }
```

### 9.2 模式 A（回调/无协程）与模式 B（协程等待）

| 维度 | **A：回调** | **B：协程** |
|------|------------|------------|
| 风格 | 事件驱动 | 顺序阻塞风格 `read()` |
| 挂起协程 | 否（回调在 VM 线程执行） | 是（`CORO_BLOCK_IO`，`reexecute=TRUE`） |
| 错误处理 | `onError(cb)` | `try/catch` 包裹 `read()` |
| 适用 | 高并发服务、事件驱动 | 顺序业务、客户端、协议解析 |
| 代表类 | `TcpSocket` / `TcpServer` / `UdpSocket` / `RawWebSocket` | `TcpStream` / `UdpStream` / `WebSocketStream` |
| root 上下文 | 正常 | `read()` 退化为真实阻塞（与 `Channel.recv` 一致） |

**模式互斥**（创建时确定，防止回调与 `read` 抢同一份数据）：
- `Callback` 模式调 `read()` → 抛 `InvalidMode`
- `Coroutine` 模式调 `onData(cb)` → 抛 `InvalidMode`
- 回调模式可通过 `socket.stream(capacity)` 转成 `Stream<ByteBuf>` 供协程侧消费（内部桥接 `Channel`）。

### 9.3 TCP

| 设计因素 | TCP → `Stream<ByteBuf>` |
|----------|------------------------|
| 有序性 | 保证 |
| 消息边界 | **无**（字节流）→ 需 `SplitTransformer` / `LengthPrefixCodec` 分包（§6.3） |
| 背压 | **有**：`Channel`/水位满 → `uv_read_stop`；消费者取走 → `uv_read_start` |
| 终态 | 对端 FIN → EOF → `close()`；RST / 超时 → `addError(NetworkError)` |
| 半关闭 | ✔ `closeWrite()` 发 FIN，仍可读 |

```sl
public class TcpStream extends NetStream
{
    static TcpStream connect( string host, Int32 port )                 # 协程模式，root 下阻塞
    static Task<TcpStream> connectAsync( string host, Int32 port )      # 协程内挂起
    static TcpSocket connectCallback( string host, Int32 port, Func<void,TcpSocket> onConnect = null )
    get bool canSeek()      # override: false
    get bool isDuplex()     # override: true
}
public class TcpServer extends Object
{
    static TcpServer listen( string host, Int32 port )
    void onConnection( Func<void,TcpSocket> cb )        # 模式 A
    TcpStream accept()                                  # 模式 B：挂起等连接
    void close()
}
```

### 9.4 UDP

| 设计因素 | UDP → `Stream<UdpDatagram>` |
|----------|------------------------------|
| 有序性 | 不保证 |
| 消息边界 | **有**（一次 `recvFrom` = 一个数据报）→ `isMessageOriented = true` |
| 背压 | **无**（UDP 不反压）→ 缓冲满按策略丢弃 |
| 容量满策略 | `DropNewest`（默认）/ `DropOldest` 可配；`dropped` 计数 |
| 终态 | 无 EOF；只有本端 `close()` |

```sl
public class UdpDatagram extends Object
{
    ByteBuf data = null
    string address = ""
    Int32  port = -1
}
public class UdpStream extends NetStream
{
    static UdpStream bind( string host, Int32 port )
    UdpDatagram recvFrom()                  # 挂起等待一个数据报
    void sendTo( UdpDatagram dg )           # 不挂起（UDP 无写背压）
    get UdpOverflowPolicy overflowPolicy()
    get Int64 droppedCount()
    Stream<UdpDatagram> datagrams( Int32 capacity = 64 )   # 元素视图
}
public enum UdpOverflowPolicy extends Int32 { DropNewest = 0, DropOldest = 1 }
```

### 9.5 TLS（P3）

```sl
public class TlsStream extends NetStream
{
    static Task<TlsStream> wrap( TcpStream raw, TlsOptions opt )   # 客户端
    static Task<TlsStream> accept( TcpStream raw, TlsOptions opt ) # 服务端
    # 复用 OpenSSL / mbedTLS；作为 TransformStream 语义叠加在 TcpStream 上
}
```
> 设计上 `TlsStream` 是「TcpStream + 加密变换」的装饰器，`canSeek=false`、`isDuplex=true`，背压继承 TCP。

### 9.6 HTTP body（P3）

```sl
public class HttpBodyStream extends NetStream
{
    # 请求/响应体：chunked 或 content-length 已知
    get bool isChunked()
    get Int64 contentLength()      # 未知返回 -1
    # 由 HttpClient 复用；与 Route.sl 服务端配合
}
```

---

## 10. WebSocket（★ 用户点名）

### 10.1 语义定位：消息流，不是纯字节流

WebSocket 是**面向消息**的：帧（frame）会被分帧/重组，一个逻辑消息可能由多个帧组成。因此提供**双视图**：

| 视图 | 类型 | 用途 |
|------|------|------|
| **消息视图** | `Stream<WsMessage>`（元素流） | 业务主用：`sendText` / `sendBinary`，保留消息边界 |
| **字节视图** | `ByteStream`（底层帧载荷流） | 流式传输大文件/媒体，配合 `ProtoCodec` / `GZipTransformer` |

```sl
public enum WsOpCode extends Int32
{ Continuation = 0x0, Text = 0x1, Binary = 0x2, Close = 0x8, Ping = 0x9, Pong = 0xA }

public enum WsCloseCode extends Int32
{
    Normal = 1000, GoingAway = 1001, ProtocolError = 1002, UnsupportedData = 1003,
    InvalidPayload = 1007, PolicyViolation = 1008, MessageTooBig = 1009,
    InternalError = 1011, TlsHandshake = 1015
}

public class WsMessage extends Object
{
    ByteBuf data = null
    get bool isText()
    get bool isBinary()
    get string text()                    # isText 时有效；UTF-8 解码，非法抛 EncodingError
    get ByteBuf binary()
    get Int64 size()
}
```

### 10.2 API

```sl
public class WsOptions extends Object
{
    Array<string> protocols = null        # Sec-WebSocket-Protocol
    Int32 maxMessageSize = 16 * 1024 * 1024
    Int32 maxFrameSize   = 64 * 1024
    Int64 pingIntervalMs = 30000          # ≤0 关闭自动 ping
    Int64 handshakeTimeoutMs = 10000
    Bool  autoPong = true                 # 收到 ping 自动回 pong
    Bool  compress = false                # permessage-deflate（P3）
    Int32 messageQueueCapacity = 16       # ★ 消息级背压
    string userAgent = ""
    Map<string,string> headers = null
}

public class WebSocketStream extends NetStream
{
    # ── 连接 ──
    static Task<WebSocketStream> connect( string url )                  # ws:// wss://
    static Task<WebSocketStream> connect( string url, WsOptions opt )
    static void connectCallback( string url, WsOptions opt,
                                 Func<void,WebSocketStream> onOpen,
                                 Func<void,Error> onError = null )      # 模式 A
    static WebSocketStream wrap( HttpUpgradeResponse resp )             # 服务端：HTTP 升级后接管

    # ── 消息视图（★ 主用）──
    Stream<WsMessage> messages()                        # 单订阅；重复调用抛 StreamAlreadyListened
    Task send( WsMessage msg )
    Task sendText( string s )
    Task sendBinary( ByteBuf b )

    # ── 字节视图（流式大块传输）──
    ByteBuf openBinaryMessage()                         # 开启一条流式消息，返回可写 ByteBuf
    Task endBinaryMessage()                             # 结束并提交
    # 说明：字节视图下 ByteStream.write() 会把数据作为 Binary 帧连续发送，
    #       read() 会把多个消息的载荷拼接为字节流（不含帧头）

    # ── 控制帧 ──
    Task ping( ByteBuf payload = null )
    Task pong( ByteBuf payload = null )                 # 通常 autoPong 已处理
    Task close( WsCloseCode code = WsCloseCode.Normal, string reason = "" )

    # ── 状态 ──
    get WsState state()                                 # Connecting/Open/Closing/Closed
    get WsCloseInfo closeInfo()                         # code + reason + wasClean
    get string protocol()                               # 协商出的子协议
    get bool canSeek()                                  # override: false
    get bool isDuplex()                                 # override: true
    get bool isMessageOriented()                        # override: true
}
public enum WsState extends Int32 { Connecting=0, Open=1, Closing=2, Closed=3 }
public class WsCloseInfo extends Object { Int32 code=0; string reason=""; Bool wasClean=false }
```

### 10.3 设计因素清单

| 因素 | 处理 | 与 Stream 体系的关系 |
|------|------|---------------------|
| **握手** | HTTP Upgrade（GET + `Sec-WebSocket-Key` → `101` + `Sec-WebSocket-Accept`） | 复用 `HttpBodyStream` 前的 HTTP 层；握手失败 → `addError(WsHandshakeError)` |
| **帧分/重组** | 控制帧（Ping/Pong/Close）可在消息中间穿插；数据帧按 FIN 重组 | 重组缓冲 = `ByteBuf`（共用载体） |
| **消息边界** | 保留 → `isMessageOriented=true`，`Stream<WsMessage>` 一元素一消息 | 与 UDP 的 `isMessageOriented` 同构 |
| **背压（两级）** | ① 帧级：`maxMessageSize` 超限 → `addError(MessageTooBig)` 并 `close(1009)`；② 消息级：`WsOptions.messageQueueCapacity`，队列满 → 暂停读帧（`uv_read_stop`），队列降到低水位恢复 | 与 TCP 背压**同一套水位机制**（§13） |
| **控制帧优先** | Ping/Pong/Close 不受消息队列背压影响，立即处理 | 需在 C 层与数据帧分流 |
| **关闭协商** | 收到 Close → 回 Close → `close()`；`closeInfo` 记录 code/reason/wasClean | 终态：Done（正常）或 Errored（异常） |
| **Ping/Pong 心跳** | `pingIntervalMs` 自动 ping；超时未 pong → `close(InternalError)` | 用 `Timer` / 协程 sleep 实现，不新增 syscall |
| **掩码** | 客户端→服务端必须掩码；服务端→客户端禁止 | C 层 `vm_sys_ws_*` 处理，对 SL 透明 |
| **permessage-deflate** | P3，走 `GZipTransformStream` 复用 | 复用 L0 变换流 |
| **TLS** | `wss://` = `TlsStream` + WebSocket | 装饰器叠加 |
| **跨 isolate** | 句柄不可发；消息体用 `ByteBuf.toTransferable()` 转移 | §12 |
| **半关闭** | WebSocket 无半关闭语义；`closeWrite()` 映射为发送 Close 帧 | 与 TCP FIN 语义不同，需在适配层说明 |

### 10.4 用法示例

```sl
# 协程风格：聊天客户端
WebSocketStream ws = await WebSocketStream.connect( "ws://127.0.0.1:8080/chat" )
Coroutine.spawnClosure0( function() {
    Stream<WsMessage> msgs = ws.messages()
    Core.IIterator<WsMessage> it = msgs.iterator()
    while ( it.moveNext() ) { onChat( it.current.text() ) }
})
ws.sendText( "hello" )
...
await ws.close( WsCloseCode.Normal, "bye" )

# 流式上传大文件（字节视图）
WebSocketStream up = await WebSocketStream.connect( "ws://host/upload" )
ByteBuf out = up.openBinaryMessage()
FileStream f = FileStream.openRead( "big.bin" )
loop { Int32 n = f.read( out ); if n <= 0 { break } up.write( out.slice() ) }
await up.endBinaryMessage()

# 与 ProtoBuf / 压缩组合（完全复用 L2）
Stream<User> users = ProtoCodec<User>.of( User.schema ).bind(
    ws.messages().where( function(WsMessage m){ ret m.isBinary } )
                 .map( function(WsMessage m){ ret m.binary } ) )
```

---

## 11. 协程集成

### 11.1 挂起点矩阵（★ 统一约定）

| 操作 | 协程上下文 | root 上下文 | 阻塞原因码 | 唤醒方 |
|------|-----------|------------|-----------|--------|
| `ByteStream.read/readAtLeast/readExactly` | 挂起 | 真实阻塞 | `IO(5)` | 数据到达（libuv / 文件完成） |
| `ByteStream.write`（待写超水位） | 挂起 | 真实阻塞 | `IO(5)` | `uv_write_cb` / 文件写完 |
| `flush` / `closeWrite` | 挂起 | 真实阻塞 | `IO(5)` | 写完 |
| `Channel.send`（缓冲满） | 挂起 | 阻塞 | `IO(5)` | 消费者取走 |
| `Channel.recv`（缓冲空） | 挂起 | 阻塞 | `IO(5)` | 生产者放入 |
| `StreamController.add`（满） | 挂起 | 阻塞 | `IO(5)` | 消费者取走 |
| `StreamSubscription.consumer` / `forEach` | 挂起 | 阻塞（不推荐） | `IO(5)` | 流终态 |
| `TcpServer.accept` | 挂起 | 阻塞 | `IO(5)` | 新连接 |
| `WebSocketStream.connect` / `close` | 挂起 | 阻塞 | `IO(5)` | 握手完成 / Close 回执 |
| `Codec.encode/decode` | **不挂起**（纯 CPU） | 同 | — | — |
| `ByteBuf` 全部方法 | **不挂起**（纯内存） | 同 | — | — |

> **重要**：`ByteBuf` 与 `Codec` 是**同步纯函数**，永不挂起。这样序列化逻辑可以在任何上下文（含 root、含回调内）安全调用 —— 这是"共用"能成立的前提。

### 11.2 挂起 / 唤醒时序（与 Channel 完全同构）

```
协程调用 TcpStream.read(dst)
  └─> vm_sys_tcp_recv(sid, dstBid)
        ├─ 接收缓冲非空 → 拷入 dst，返回字节数（不挂起）
        └─ 缓冲为空 → vm_coroutine_suspend_current(vm, CORO_BLOCK_IO, reexecute=TRUE)
             （参数留栈，resume 后重跑该系统调用重新检查）

libuv 事件循环（vm_isolate_pump 内 uv_run(loop, UV_RUN_NOWAIT)）
  └─> uv_read_cb 收到字节 → 写入 ByteBuf
        ├─ 达到 highWaterMark → uv_read_stop(handle)        # 背压
        └─ vm_channel_wake_one(vm, &sock->recv_q)           # 唤醒挂起协程
调度器恢复 → 重跑系统调用 → 返回数据
```

### 11.3 取消与超时

| 机制 | 行为 |
|------|------|
| `sub.cancel()` | 协作式：取消消费协程（下一调度点抛 `CancelledError`）+ `close()` 流唤醒生产者 |
| `ctrl.cancel()` / `close()` | 关闭流；阻塞在 `add` 的生产者被唤醒并收到 `StreamClosed` |
| `NetStream.close()` | 关闭 socket，所有挂起的读写协程抛 `NetworkError(Closed)` |
| `readTimeoutMs` / `writeTimeoutMs` | 超时抛 `TimeoutError`；由 `Timer` 队列实现，不新增 syscall |
| `Task.cancel()` | 复用现有 `Coroutine.cancel`（协作式） |

---

## 12. isolate 集成

### 12.1 可发送性矩阵（★ 关键约束）

| 类型 | 可跨 isolate | 方式 | 依据 |
|------|-------------|------|------|
| `ByteBuf` | **仅通过 `toTransferable()`** | 零拷贝转移，原 buf 失效 | 复用 `SystemTransfer*` |
| `ByteBuf.slice()` / `duplicate()` 派生的 | **✘** | 共享底层数组，发送抛 `NotSendable` | §4.1 约定 |
| `ByteStream` 及其子类（`FileStream`/`TcpStream`/`WebSocketStream`…） | **✘** | VM 注册表句柄，不在 `Sendable` 白名单 | 与 `Channel` 同 |
| `Stream<T>` / `StreamController` / `StreamSubscription` | **✘** | 同上 | v1 §8 |
| `Codec` / `Converter` / `ProtoSchema` | **✔** | 纯数据对象，深拷贝 | 无句柄、无闭包捕获 → 可发 |
| `Stream` 的**元素 `T`** | 视 `T` | 遵循现有白名单（null/数值/bool/string/SendPort/Capability/TransferableData/List/Map/Set/闭包） | `ISOLATE_DESIGN.md` §4.5.3 |
| 捕获了 stream/socket 的闭包 | **✘** | 抛 `NotSendable` | 同上 |

### 12.2 跨岛桥接模式

```sl
# 模式 1：元素级（小数据，深拷贝）
Task pipeTo( SendPort port )        # 逐元素 send，终态发哨兵 StreamDoneSentinel
static Stream<T> fromReceivePort( ReceivePort rp )

# 模式 2：字节块级（推荐，大数据）
# 源端：按块读 → 零拷贝转移
Coroutine.spawnClosure0( function() {
    ByteBuf buf = ByteBuf( 64 * 1024 )
    loop {
        Int32 n = src.read( buf )
        if n <= 0 { port.send( StreamDoneSentinel() ); break }
        port.send( buf.slice(0, n).copy().toTransferable() )   # slice 需 copy 才可发
        buf.discardReadBytes()
    }
})
# 目标端：重组为本地 ByteStream → 再转 Stream<T>
static ByteStream fromReceivePortBytes( ReceivePort rp )
```

> **注意**：`slice()` 是共享底层视图，**不可跨岛**；跨岛前必须 `.copy()`。这条规则要写进 `ByteBuf` 的文档注释与运行期错误提示。

### 12.3 推荐架构：IO isolate

```
┌─────────────┐   TransferableData(ByteBuf)   ┌──────────────┐
│  IO isolate │ ───────────────────────────▶  │ Worker isolate│
│ TcpStream   │   SendPort(元素/字节块)        │ ProtoCodec    │
│ WebSocket   │ ◀───────────────────────────  │ 业务处理      │
│ FileStream  │   SendPort(回压/控制)          │ Stream<T>     │
└─────────────┘                                └──────────────┘
```
- 网络/文件句柄**只存在于 IO isolate**，永不跨岛。
- 传输单位是大块 `ByteBuf`（零拷贝），而非逐元素 → 跨岛开销最小。
- **跨岛背压**：`SendPort.send` 不阻塞 → 需显式反向流控（worker 处理完一批后 `ackPort.send(n)`；IO 侧未收到 ack 前暂停读）。P3 提供 `BackpressureChannel` 封装。

---

## 13. 容量与背压（三层统一）

| 层 | 背压载体 | 参数 | 满时行为 | 恢复 |
|----|---------|------|---------|------|
| **L0 字节** | `ByteBuf` 水位 + 等待队列 | `readHighWaterMark` / `readLowWaterMark` / `writeHighWaterMark` | 读：暂停上游（`uv_read_stop`）；写：`write()` 挂起 | 降到低水位 / `uv_write_cb` |
| **L0 元素（块流）** | `Channel<ByteBuf>` 容量 | `capacity`（块数） | `send` 挂起生产者协程 | 消费者取走唤醒 |
| **L1 元素** | `Channel<T>` 容量 | `StreamController(capacity)` | `add` 挂起生产者协程 | 消费者取走唤醒 |
| **UDP** | 无背压 | `overflowPolicy` | 丢弃（Newest/Oldest）+ `dropped++` | — |
| **WebSocket 消息** | 消息队列容量 | `messageQueueCapacity` | 暂停读帧 | 队列降到低水位 |
| **跨 isolate** | `SendPort` 无背压 | — | 需显式 ack 反向流控（§12.3） | ack 到达 |

**默认取值建议**：

| 参数 | 默认 | 理由 |
|------|------|------|
| `StreamController(capacity)` | 64 | 有界优先，避免无界 OOM（v1 §7 结论） |
| `ByteBuf` 初始容量 | 512（小）/ 8 KB（IO 缓冲） | 减少扩容 |
| `readHighWaterMark` | 64 KB | 与 socket 接收缓冲同量级 |
| `readLowWaterMark` | 16 KB | 留 4 倍滞后，避免抖动 |
| `writeHighWaterMark` | 256 KB | 允许批量写，超过则挂起 |
| WebSocket `messageQueueCapacity` | 16 条消息 | 消息级背压 |

> **端到端背压链**（TCP → ProtoBuf → 业务）：业务消费慢 → `Channel<T>` 满 → 解码协程挂起 → `ByteBuf` 缓冲达高水位 → `uv_read_stop` → 内核接收窗口满 → 对端发送变慢。**整条链无需业务代码参与**。

---

## 14. 错误处理

### 14.1 错误类型树

```
Error（现有 Core/Error.sl）
└── StreamError
    ├── StreamClosed              # 向终态流 add / 已关闭流读写
    ├── StreamAlreadyListened     # 单订阅流重复 listen / 重复取 controller.stream
    ├── BufferUnderflow           # ByteBuf 读越界（可读不足）
    ├── BufferReleased            # 已 toTransferable 的 ByteBuf 被访问
    ├── UnexpectedEof             # readExactly / 解码到半条消息遇 EOF
    ├── NotSendable               # 跨 isolate 发送非法值
    ├── CancelledError            # 协程协作取消
    ├── TimeoutError              # read/write 超时
    └── NotSupported              # 能力位不支持（如对 socket 调 seek）
└── CodecError
    ├── EncodingError             # 非法 UTF-8 / 字符编码错误
    ├── ProtoFormatError          # wire format 非法（tag/长度/嵌套过深）
    └── SchemaMismatch            # schema 与数据不匹配
└── NetError（netStream）
    ├── ConnectionRefused / ConnectionReset / ConnectionAborted
    ├── DnsError
    ├── HandshakeError
    ├── WsHandshakeError          # 101 未返回 / Key 校验失败
    ├── WsProtocolError           # 帧格式非法 / 掩码错误（close 1002）
    └── WsMessageTooBig           # 超 maxMessageSize（close 1009）
└── FileError（现有 Std/IO/File.sl，扩展）
    └── 复用现有 FileError.NotFound，新增 AccessDenied / SharingViolation
```

### 14.2 传播规则

| 场景 | 行为 |
|------|------|
| `ByteStream` 读写出错 | 协程内**抛异常**（不用负值返回码）；`read` 的 `Int32` 返回值仅表示字节数，`0` = EOF |
| `addError(e)`（单订阅） | 当前/后续消费者收到 `onError(e)`，流进入 `Errored` 终态 |
| `cancelOnError=true` | 首个错误即自动取消订阅 |
| 网络错误 | 统一转 `NetError` 下行；`lastError` 可查询 |
| WebSocket 协议错误 | 自动发 Close 帧（对应 code）后转 `WsProtocolError` |
| 解码错误 | `CodecError` 沿流传播；`ProtoCodec` 支持 `skipUnknownFields` 容错（Proto 语义） |

---

## 15. ★ 共用 `system_method_call` 契约（用户重点）

> **核心要求**：「一些内置的 system_method_call 一定要提供成共用的」。
> 契约：**所有 syscall 只依赖 `ByteBuf` 句柄（`Int64 _bid`）与基础标量，不依赖任何具体来源（文件/网络/内存）**。这样才能被 ProtoBuf、序列化、压缩、网络、文件共同复用。

### 15.1 命名规范

```
vm_sys_<domain>_<object>_<action>
domain:  bytebuf | bytestream | file | net | ws | codec | hash | compress | transfer
```
- 返回句柄的用 `create`；销毁用 `destroy`/`close`。
- 全部遵循 R3：`src/vm/system_method_call/*.c` **只做栈 ↔ 库转发**，平台逻辑放 `src/lib/os/sys_*.c`。
- 全部遵循 R7：四处同步（`.sl` + `<Lib>.jsonc` 的 `cvmFunction` + C 实现 + `Define.cs`）。

### 15.2 P0：ByteBuf 原语（★ 最优先，一切共用的地基）

登记于 `Core.jsonc`（新增 `bytebuf` 组）。C 实现建议 `src/base/vm_bytebuf.c`（**在 base 层，非 vm 层**，与 `bytearray.c` 同层，便于 lib 直接调用）+ `src/vm/system_method_call/bytebuf_system_method.c` 做栈转发。

| SL 调用 | C 函数 | 说明 | 被复用方 |
|---------|--------|------|---------|
| `SystemByteBufCreate` | `vm_sys_bytebuf_create` | `(Int32 cap) -> Int64 bid` | 全部 |
| `SystemByteBufDestroy` | `vm_sys_bytebuf_destroy` | `(Int64 bid) -> void` | 全部 |
| `SystemByteBufFromArray` / `WrapArray` | `vm_sys_bytebuf_from_array` / `_wrap_array` | 深拷贝 / 零拷贝包装 | 序列化、网络 |
| `SystemByteBufToArray` | `vm_sys_bytebuf_to_array` | 可读区 → `Array<UInt8>` | 序列化 |
| `SystemByteBufReaderIndex` (get/set) | `vm_sys_bytebuf_get/set_reader_index` | | 全部 |
| `SystemByteBufWriterIndex` (get/set) | `vm_sys_bytebuf_get/set_writer_index` | | 全部 |
| `SystemByteBufCapacity` / `ReadableBytes` / `WritableBytes` | `vm_sys_bytebuf_*` | | 背压 |
| `SystemByteBufEnsureWritable` / `DiscardReadBytes` / `Clear` / `Shrink` | `vm_sys_bytebuf_*` | | 长连接 |
| `SystemByteBufSlice` / `Duplicate` / `Copy` | `vm_sys_bytebuf_slice` / `_duplicate` / `_copy` | 返回新 bid；`slice`/`duplicate` 共享底层 | 零拷贝管线 |
| `SystemByteBufReadU8` … `ReadF64` (含 Le/Be) | `vm_sys_bytebuf_read_u8` … | 一组 ~20 个 | **ProtoBuf / Binary / 所有二进制协议** |
| `SystemByteBufWriteU8` … `WriteF64` (含 Le/Be) | `vm_sys_bytebuf_write_*` | 一组 ~20 个 | 同上 |
| `SystemByteBufReadBytes` / `WriteBytes` | `vm_sys_bytebuf_read/write_bytes` | `(Int64 dst, Int64 src, Int32 len)` | 管线串联 |
| `SystemByteBufIndexOf` / `StartsWith` / `Equals` | `vm_sys_bytebuf_*` | | 分包（分隔符查找） |
| `SystemByteBufIsReleased` | `vm_sys_bytebuf_is_released` | | 转移后保护 |

### 15.3 P0：变长整数 / 编解码原语（★ ProtoBuf 与序列化共用）

登记于 `Core.jsonc`（`codec` 组）。**注意：这一组是 ProtoBuf 与自研 `BinaryCodec` 唯一的共用点，绝不能在 ProtoBuf 模块里私有实现。**

| SL 调用 | C 函数 | 说明 | 复用方 |
|---------|--------|------|--------|
| `SystemVarUintEncode` | `vm_sys_codec_varuint_encode` | `(Int64 bid, Int64 v) -> Int32 写入字节数` | ProtoBuf、BinaryCodec、RPC 分帧 |
| `SystemVarUintDecode` | `vm_sys_codec_varuint_decode` | `(Int64 bid) -> Int64` | 同上 |
| `SystemVarIntEncode` | `vm_sys_codec_varint_encode` | ZigZag + LEB128 | 同上 |
| `SystemVarIntDecode` | `vm_sys_codec_varint_decode` | | 同上 |
| `SystemZigZagEncode` / `Decode` | `vm_sys_codec_zigzag_*` | `(Int64) -> Int64` | 同上 |
| `SystemVarUintEncodedSize` | `vm_sys_codec_varuint_size` | 预计算长度（用于先写长度前缀） | **长度前缀分帧** |

### 15.4 P1：字节流 / 文件

登记于 `Std.jsonc`（`io` 组）。

| SL 调用 | C 函数 | 说明 |
|---------|--------|------|
| `SystemFileOpen` | `vm_sys_file_open` | `(string path, Int32 mode, Int32 access) -> Int64 fid` |
| `SystemFileRead` | `vm_sys_file_read` | `(Int64 fid, Int64 bid, Int32 maxBytes) -> Int32`（协程挂起） |
| `SystemFileWrite` | `vm_sys_file_write` | `(Int64 fid, Int64 bid) -> Int32` |
| `SystemFileSeek` | `vm_sys_file_seek` | `(Int64 fid, Int64 off, Int32 origin) -> Int64` |
| `SystemFileFlush` / `Sync` | `vm_sys_file_flush` / `_sync` | |
| `SystemFileTell` / `Length` | `vm_sys_file_tell` / `_length` | |
| `SystemFileClose` | `vm_sys_file_close` | |
| `SystemFileIsEof` | `vm_sys_file_is_eof` | |

> 平台实现复用现有 `src/platform/{windows,unix}/sl_file-*.c` 与 `src/lib/os/sys_fs.c`，仅新增「句柄 + 流式」语义。

### 15.5 P1：网络（`net` / `ws` 组）

沿用 v1 §17.5 的 11 个 syscall，并补充 WebSocket：

| SL 调用 | C 函数 | 说明 |
|---------|--------|------|
| `SystemTcpConnect` / `Listen` / `Accept` / `Send` / `Recv` / `Close` | `vm_sys_tcp_*` | `Send/Recv` 直接收 `Int64 bid`（★ 复用 ByteBuf，不再单独传 `ByteArray`） |
| `SystemUdpBind` / `SendTo` / `RecvFrom` / `Close` | `vm_sys_udp_*` | |
| `SystemDnsResolve` | `vm_sys_dns_resolve` | |
| `SystemNetSetOption` | `vm_sys_net_set_option` | `(sid, optId, intVal)`：NoDelay/KeepAlive/ReuseAddr/BufSize |
| `SystemNetLocalAddress` / `RemoteAddress` / `IsConnected` | `vm_sys_net_*` | |
| **WebSocket** | | |
| `SystemWsConnect` | `vm_sys_ws_connect` | `(string url, Int64 optBid) -> Int64 sid`（协程挂起至握手完成） |
| `SystemWsSend` | `vm_sys_ws_send` | `(sid, Int64 bid, Int32 opcode) -> void` |
| `SystemWsRecvMessage` | `vm_sys_ws_recv_message` | `(sid) -> WsMessage(ByteBuf)`（协程挂起；保留消息边界） |
| `SystemWsPing` / `Pong` | `vm_sys_ws_ping` / `_pong` | |
| `SystemWsClose` | `vm_sys_ws_close` | `(sid, Int32 code, string reason)` |
| `SystemWsState` / `CloseInfo` | `vm_sys_ws_state` / `_close_info` | |

### 15.6 P2/P3：哈希与压缩

| SL 调用 | C 函数 | 复用方 |
|---------|--------|--------|
| `SystemHashCreate` / `Update` / `Digest` | `vm_sys_hash_*` | 校验和、Zip、去重、断点续传 |
| `SystemCompressBound` / `Compress` / `Decompress` | `vm_sys_compress_*` | GZip（zlib）、Lz4（third_party/lz4） |

### 15.7 复用矩阵（★ 证明"共用"成立）

| syscall 组 | ProtoBuf | Binary 序列化 | Json/Xml | 文件 | TCP | UDP | WebSocket | 压缩 | 跨 isolate |
|-----------|----------|--------------|----------|------|-----|-----|-----------|------|-----------|
| `bytebuf_*`（读写原语） | ★ | ★ | ○ | ★ | ★ | ★ | ★ | ★ | ★ |
| `codec_varint_*` | ★ | ★ | — | — | — | — | — | — | — |
| `hash_*` | ○ | ○ | — | ○ | ○ | — | ○ | ★ | — |
| `compress_*` | ○ | ○ | — | ○ | ○ | — | ○ | ★ | — |
| `file_*` | ○ | ○ | ○ | ★ | — | — | — | — | — |
| `tcp_*` / `udp_*` | — | — | — | — | ★ | ★ | — | — | — |
| `ws_*` | — | — | — | — | — | — | ★ | — | — |
| `transfer_*`（现有） | ★ | ★ | — | ★ | ★ | ★ | ★ | ★ | ★ |
| `channel_*`（现有） | ○ | ○ | — | — | ★ | ★ | ★ | — | — |
| `coroutine_*`（现有） | ○ | ○ | — | ★ | ★ | ★ | ★ | — | — |

★ = 强依赖（核心路径） ○ = 可选使用 — = 不需要

### 15.8 新增 syscall 总清单（按分期）

| 分期 | 组 | 数量 | 说明 |
|------|-----|------|------|
| **P0** | `bytebuf_*` | ~45 | 含 Le/Be 读写、slice、索引管理 |
| **P0** | `codec_varint_*` / `zigzag_*` | 6 | ProtoBuf 与 Binary 共用 |
| P1 | `file_*` | 9 | 流式文件 |
| P1 | `net_*` / `tcp_*` / `udp_*` / `dns_*` | ~15 | libuv 接入 |
| P2 | `ws_*` | 8 | WebSocket |
| P3 | `hash_*` / `compress_*` | ~8 | 校验与压缩 |
| P3 | `tls_*` | ~6 | TLS |
| — | **合计新增** | **~97** | 分 4 期落地，每期独立可测 |

---

## 16. 文件落位与模块划分

| 文件 | 模块 | 内容 | 分期 |
|------|------|------|------|
| `Core/IO/ByteBuf.sl` | Core | `ByteBuf` | P0 |
| `Core/IO/ByteStream.sl` | Core | `abstract ByteStream`、`SeekOrigin`、`MemoryStream`、`TransformStream`、`HashingStream` | P1 |
| `Core/IO/Codec.sl` | Core | `Codec` / `Converter` / `ChunkedConversionSink` | P1 |
| `Core/IO/Encoding.sl` | Core | 替换现有空壳 `Core/Text/Encoding.sl`：`Utf8Codec` 等 | P2 |
| `Core/Container/Stream.sl` | Core | `abstract Stream<T>`、`_BaseStream`、`StreamController`、`StreamSink`、`StreamSubscription`、`StreamIterator` | P1 |
| `Core/Container/StreamTransformer.sl` | Core | `StreamTransformer` + 内置 transformer | P1 |
| `Core/Container/Channel.sl` | Core | 不变（背压底座） | — |
| `Std/IO/FileStream.sl` | Std | `FileStream`、`FileMode`、`FileAccess`；`File` 扩展 | P2 |
| `Std/IO/StdStream.sl` | Std | `StdIn/Out/ErrStream` | P2 |
| `Std/Net/NetStream.sl` | Std | `abstract NetStream`、`NetMode`、`NetworkError` | P1 |
| `Std/Net/TcpSocket.sl` | Std | 替换空壳：`TcpSocket` / `TcpServer` / `TcpStream` | P1 |
| `Std/Net/UdpSocket.sl` | Std | 替换空壳（**顺带修正 namespace 写成 `Http` 的笔误**）：`UdpSocket` / `UdpStream` / `UdpDatagram` | P1 |
| `Std/Net/WebSocket.sl` | Std | `WebSocketStream`、`WsMessage`、`WsOptions`、`WsCloseCode` | P2 |
| `Std/Net/HttpBody.sl` | Std | `HttpBodyStream`；`HttpClient.sl` 改造为流式 | P3 |
| `Std/Text/ProtoBuf.sl` | Std | `ProtoCodec`、`ProtoSchema`、`ProtoType`、`ProtoField` | P2 |
| `Std/Text/BinaryCodec.sl` | Std | `BinaryCodec` | P2 |
| `Std/Text/Serialize.sl` | Std | `Serialize` 统一入口（§8.7） | P2 |
| `Std/Zip/GZipStream.sl` | Std | 替换空壳：`GZipEncode/DecodeStream` | P3 |
| `Std/Zip/Lz4Stream.sl` | Std | 替换空壳 | P3 |

> 同时修正的既有 bug：`Std/Net/UdpSocket.sl` 内 `namespace Http` → `namespace Udp`（v1 §17.1 已记录）。

---

## 17. 分期路线

| 期 | 目标 | 交付物 | 依赖 |
|----|------|--------|------|
| **P0 地基** | `ByteBuf` + varint 原语 | `Core/IO/ByteBuf.sl`；`vm_bytebuf.c` + `vm_sys_codec_varint_*`；~51 个 syscall；单测覆盖读写/边界/slice/扩容 | 无 |
| **P1 流骨架** | L0 + L1 成型 | `ByteStream`/`MemoryStream`/`TransformStream`/`Codec`；`Stream<T>` 全套 + `StreamTransformer` + `FromByteStream` 桥接；`IIterable` 适配；`NetStream` 抽象 + Tcp/Udp（含回调/协程双模式）；libuv 接入 | P0 |
| **P2 序列化与文件** | 用户核心诉求落地 | `ProtoCodec`（含 chunked decoder、长度前缀分帧）、`BinaryCodec`、`Serialize` 入口、`JsonCodec`；`FileStream`；`WebSocketStream`；`periodic`；跨 isolate `pipeTo`/`fromReceivePort` | P1 |
| **P3 生态** | 完整管线 | Broadcast 多订阅；`flatMap`/`distinct`/`scan`；GZip/Lz4 变换流；TLS；`HttpBodyStream`；`SplitTransformer`/`LengthPrefixCodec`；Hash；跨岛背压 `BackpressureChannel`；`TransferableData` 批量零拷贝 | P2 |
| **P4 体验** | 语法与文档 | 可选语法糖 `async*` 生成器 + `for await`（前端降级为 `StreamController` + 协程，与 `ISOLATE_DESIGN.md` §6.5 同构）；补 `md/syntax/{stream,bytebuf,io,net,serialize}.md` 并登记 `md/INDEX.md`（R13） | P3 |

> **与 `未解决问题.txt` 的对应**：L19/L20（Stream abstract + protobuf/序列化）→ P0~P2；L37（netStream、buffer、builder）→ `NetStream`(P1) + `ByteBuf`(P0) + `ByteBufferBuilder`(P3)。

---

## 18. 测试用例分组

| 组 | 覆盖 | 关键断言 |
|----|------|---------|
| **A** | `ByteBuf` 基础 | 读写各类标量、LE/BE 一致、索引推进、`clear`/`discardReadBytes`、扩容 |
| **A2** | `ByteBuf` 边界 | 读越界抛 `BufferUnderflow`；`wrap` 后写抛只读错；`slice` 共享；`toTransferable` 后访问抛 `BufferReleased` |
| **A3** | varint / zigzag | 与 ProtoBuf 官方测试向量比对（0/1/127/128/300/-1/Int64.Max） |
| **B** | `MemoryStream` | 写后读、`seek`、`position`、`toArray` 往返 |
| **C** | `Stream` 基础 | `generate`+`close`；`fromIterable`；`toList`；`forEach` 顺序 |
| **C2** | 惰性转换链 | `where`+`map`+`take` 内存恒定（用周期性流验证未提前求值） |
| **D** | 错误传播 | `addError` → `onError` + `Errored` 终态；`cancelOnError` 自动取消 |
| **E** | `IIterable` 拉取 | 协程内 `iterator().moveNext()` 挂起与恢复 |
| **F** | 背压 | 容量 1 的 `StreamController`，第二个 `add` 挂起（`!task.isDead`），消费后恢复 |
| **F2** | 字节流背压 | TCP 高水位触发 `uv_read_stop`，消费后恢复读取 |
| **G** | 跨 isolate | `pipeTo` + `fromReceivePort`；`ByteBuf.toTransferable` 零拷贝；`slice` 发送抛 `NotSendable` |
| **H** | ProtoBuf | 单消息编解码往返；嵌套 message；packed repeated；未知字段跳过；**流式多消息（含拆包/粘包）** |
| **H2** | ProtoBuf × 多载体 | 同一 schema 在 Memory/File/Tcp/WebSocket 四种载体上编解码结果一致（★ 共用性验证） |
| **I** | TCP 回调模式 | `onConnection` / `onData` / `send` |
| **I2** | TCP 协程模式 | `accept`/`read` 挂起不阻塞其它协程；EOF；RST → `NetworkError` |
| **J** | UDP | 数据报边界保留（不粘包）；丢弃策略与 `droppedCount` |
| **K** | WebSocket | 握手（含 `wss`）；文本/二进制消息；消息边界；ping/pong；close 协商与 `closeInfo`；超 `maxMessageSize` → 1009 |
| **K2** | WebSocket × ProtoBuf | 二进制消息流经 `ProtoCodec` 解码为 `Stream<T>` |
| **L** | 变换流 | GZip 编解码往返；`FileStream → GZipDecode → Split → Json` 全链路 |
| **M** | 文件流 | `openRead`/`openWrite`/`seek`/`flushToDisk`；与整文件 API 结果一致 |
| **N** | 取消 / 超时 | `sub.cancel()`；`readTimeoutMs` 触发 `TimeoutError`；`close` 唤醒挂起协程 |

> 用例落位：`simple_language/test/ExpendTest/`（标准库宿主 `CSimpleVMStdTest`），新增 `StreamTest/`、`ProtoBufTest/`、`NetTest/` 三个子目录；新增用例三步见 `md/project/test-guide.md`。

---

## 19. 与五语言对比

| 维度 | Dart | Java | C# | Swift | Netty/Go | **simple_language** |
|------|------|------|----|-------|----------|---------------------|
| 元素流 | `Stream<T>` | `Stream<T>` | `IAsyncEnumerable<T>` | `AsyncStream` | — | **`abstract Stream<T>`** |
| 字节流 | — | `InputStream` | `abstract Stream` | — | `ByteBuf` / `io.Reader` | **`abstract ByteStream` + `ByteBuf`** |
| 能力协商 | — | — | `CanRead/Write/Seek` | — | — | **能力位（6 位）** |
| 缓冲载体 | `Uint8List` | `ByteBuffer` | `Span<byte>` | — | `ByteBuf` 双索引 | **`ByteBuf` 双索引 + slice 零拷贝** |
| 编解码 | `dart:convert` Codec | — | `System.Text.Json` | `Codable` | `ByteToMessageDecoder` | **`Codec<S,T>` + `StreamTransformer`** |
| 背压 | 订阅 pause | 无（拉惰性） | 无 | 无 | 水位 | **Channel 容量 + ByteBuf 双水位** |
| 协程驱动 | `async*`/`await for` | 无 | `await foreach` | `for await` | — | **`Coroutine` + 闭包，无新语法** |
| 跨 isolate | `ReceivePort` | 无 | `Channel`(TPL) | `Actor` | — | **`SendPort`/`ReceivePort` + `TransferableData` 零拷贝** |
| 函数式转换 | 少 | 丰富 | LINQ | 少 | — | **`map`/`where`/`take`/`flatMap`/`transform`** |
| 消息边界 | — | — | — | — | `MessageToByteEncoder` | **`isMessageOriented`（UDP/WebSocket）** |

---

## 20. 附录

### 20.1 全景关系图

```
                        ┌───────────────────────────────┐
   业务代码 ────────────▶│  L1  Stream<T>                │
                        │  map/where/take/flatMap        │
                        │  listen/forEach/toList         │
                        └───┬───────────────────▲────────┘
                            │ fromByteStream     │ asStream
                            │ (Codec 驱动)       │
                        ┌───▼───────────────────┴────────┐
                        │  L2  Codec<S,T>                │
                        │  ProtoBuf/Binary/Json/Base64   │
                        │  GZip/Lz4/Aes/Encoding         │
                        └───┬───────────────────▲────────┘
                            │ encode→ByteBuf     │ decode←ByteBuf
                        ┌───▼───────────────────┴────────┐
                        │  L0  ByteBuf（缓存载体）        │
                        │      abstract ByteStream        │
                        └───┬────────────────────────────┘
        ┌───────────┬───────┼────────┬──────────┬────────────┐
    MemoryStream FileStream NetStream  Transform  StdIn/Out
                              │        (GZip/Aes)
                    ┌─────────┼─────────┐
                TcpStream  UdpStream  WebSocketStream
                TlsStream  HttpBodyStream
                    │
              libuv 事件循环（vm_isolate_pump: uv_run NOWAIT）

   挂起：CORO_BLOCK_IO(5) → vm_coroutine_suspend_current(reexecute=TRUE)
   唤醒：uv_*_cb → 写入 ByteBuf → wake_one → enqueue_ready → 重跑系统调用
   背压：ByteBuf 高/低水位 + Channel 容量（端到端传导）
   跨岛：句柄不可发；ByteBuf.copy().toTransferable() 零拷贝转移
   共用：vm_sys_bytebuf_* / vm_sys_codec_varint_* / vm_sys_hash_* / vm_sys_compress_*
```

### 20.2 术语表

| 术语 | 含义 |
|------|------|
| L0 / L1 / L2 | 字节层 / 元素层 / 编解码层 |
| `ByteBuf` | 双索引字节缓冲，全体系唯一缓存载体 |
| `ByteStream` | 字节 I/O 抽象（能力位 + 协程挂起） |
| `Stream<T>` | 元素流（Dart 风格异步序列） |
| `Codec<S,T>` | 无状态编解码器，可跨岛、可 fuse |
| `StreamTransformer<S,T>` | 流到流的转换（含分包、解压、解码） |
| 能力位 | `canRead/canWrite/canSeek/canTimeout/isDuplex/isMessageOriented` |
| 水位 | 读/写高、低水位，用于背压 |
| `CORO_BLOCK_IO` | `CoroutineBlockReason.IO = 5`，IO 挂起原因码 |
| `reexecute` | 挂起后重跑系统调用的约定（与 Channel 同构） |
| `isMessageOriented` | 保留消息/帧边界（UDP 数据报、WebSocket 消息） |

### 20.3 相关文档

| 文档 | 关联 |
|------|------|
| `md/design/ISOLATE_DESIGN.md` | 跨岛规则、`TransferableData` 零拷贝、`Sendable` 白名单 |
| `md/syntax/coroutine.md` | 协程语法权威（实现版） |
| `md/syntax/isolate.md` | isolate 语法与端口 |
| `md/syntax/attribute.md` | `@Serializable()` 构想（§8.3 方式 B 的依据） |
| `md/syntax/system_method.md` | 系统方法声明语法 |
| `md/project/project-config-jsonc-guide.md` | `.jsonc` 的 `systemCalls[]` 配置 |
| `csimple_lang/md/aiprompt.txt` + `vm-mapping.md` | 改 C VM 前必读（R1） |
| `csimple_lang/md/RUNTIME_LAYOUT_GUIDE.md` | 目录放置与平台隔离（R3） |
| `AGENT.md` | R2 opcode 对齐、R6 源文件登记、R7 系统方法四处同步 |

### 20.4 设计要点复盘

> **一个 `ByteBuf` 打底，两个 abstract 分居两侧（`ByteStream` 管字节、`Stream<T>` 管元素），一个 `Codec` 在中间做翻译。**
> 字节读写与 varint 原语下沉为**共用 syscall**，使 ProtoBuf、二进制序列化、Json、文件、TCP、UDP、WebSocket、压缩、跨岛传输**共用同一套字节操作**，而不是各自为政。
> 协程提供挂起语义、`Channel` 提供背压、`SendPort`/`TransferableData` 提供跨岛与零拷贝，`IIterable` 提供同步遍历 —— 全部复用既有机制，新增的只是**原语与组合方式**。
