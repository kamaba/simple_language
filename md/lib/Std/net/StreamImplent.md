# Net 流体系 — ByteStream / NetStream / TlsStream / Stream&lt;T&gt;

- 源码：`source/Front/Lib/Core/IO/ByteStream.sl`（L0 字节流）+ `Std/Net/NetStream.sl`（网络流基类与 TcpStream / UdpStream）+ `Std/Net/TlsStream.sl`（TLS）+ `Core/IO/Stream.sl`（L1 元素流）+ `Core/IO/Codec.sl`（L2 转换）
- 定位：网络 / 文件 / 内存 IO 的**统一流抽象**——网络流是 ByteStream 子类，可直接接入 Stream 体系（`LengthPrefix` / `ProtoCodec` 等）
- 相关：总览见 [Net.md](Net.md)；TCP 见 [Tcp.md](Tcp.md)；UDP 见 [Udp.md](Udp.md)；WebSocket 消息流见 [Websocket.md](Websocket.md)；流式编解码见 [../../Core/ProtocalBuffers.md](../../Core/ProtocalBuffers.md)；Stream 专题见 [../../Core/Stream.md](../../Core/Stream.md)
- 测试：`test/Other/NetTest/`（NetStreamComposeTest / TlsTest 等）+ `test/BaseTest/StreamTest.sl`

---

## 1. 体系分层

```
L0  ByteStream（Core/IO · 字节流抽象 + 能力位）
     ├── MemoryStream                     内存流（可 seek）
     └── NetStream（Std/Net · 网络流基类）
          ├── TcpStream                   TCP 字节流（双工）
          ├── UdpStream                   UDP 报文流（只读 + sendTo）
          ├── TlsStream                   TLS 装饰（包 TcpStream，mbedTLS）
          └── WebSocketStream             WebSocket 消息流（见 websocket.md）

L1  Stream<T> / StreamController<T>（Core/IO · 元素流 + 引流协程模式）
L2  Converter / Codec（Core/IO · 同步转换，永不挂起）
```

| 层 | 关注 | 挂起行为 |
|----|------|------|
| L0 ByteStream | 字节收发（`read(ByteBuffer)` / `write(ByteBuffer)`） | 网络 / 文件实现可挂起（协程化）；内存实现不挂起 |
| L1 Stream&lt;T&gt; | 元素逐个到达（`listen` / `forEach` / 变换） | 靠引流协程把 L0 字节泵成元素 |
| L2 Converter / Codec | 同步值转换（`convert` / `encode` / `decode`） | **永不挂起，全部方法无 throws** |

桥接入口：`Stream.fromByteStream( src, codec )`（L0+L2 → L1）。

---

## 2. ByteStream — L0 契约（Core/IO）

### 2.1 能力位

实现类以 `public bool` 字段声明能力，同名 getter 透出（`_canWrite`、`_isMessageOriented` 等内部置位）：

| 能力位 | 含义 | MemoryStream | TcpStream | UdpStream | TlsStream | WebSocketStream |
|--------|------|--------------|-----------|-----------|-----------|-----------------|
| canRead | 可读 | ✓ | ✓ | ✓ | ✓ | ✓ |
| canWrite | 可写 | ✓ | ✓ | ✗（走 sendTo） | ✓ | ✗（走 sendText/sendBinary） |
| canSeek | 可寻位 | ✓ | ✗ | ✗ | ✗ | ✗ |
| canTimeout | 支持读写超时 | ✗ | ✓ | 读侧 | ✓（随底层） | ✓（随底层） |
| isDuplex | 双工 | — | ✓ | ✗ | ✓ | ✗ |
| isMessageOriented | 报文粒度 | ✗ | ✗（字节流） | ✓ | ✗ | ✓（read 按消息） |

### 2.2 抽象方法（子类必须实现）

| 方法 | 语义 |
|------|------|
| `abstract Int32 read( ByteBuffer dst ) throws` | 写入 dst 可写区，返回读入字节数；**0 = EOF，无负值**（负值由实现转 `StreamIOError.IoError`）；未就绪时网络实现挂起 |
| `abstract void write( ByteBuffer src ) throws` | 全量写出：推进 src.readerIndex 直到 readableBytes == 0（写背压由实现挂起透明处理） |
| `abstract void flush() throws` | 冲刷用户态缓冲（无缓冲的实现为空操作） |

> ByteBuffer 约定：`read(buf)` 写入 writable 区、`write(buf)` 消费 readable 区、`buf.clear()` 双索引复位循环复用（详见 `Core/IO/ByteBuffer.sl`）。

### 2.3 组合方法（基类基于 read/write 实现）

| 方法 | 语义 |
|------|------|
| `Int32 readAtLeast( ByteBuffer dst, Int32 n ) throws` | 读到至少 n 字节或 EOF 为止（返回实际读入；EOF 时返回本次读到的 < n） |
| `ByteBuffer readExactly( Int32 n ) throws` | 精确读 n 字节返回新 ByteBuffer；流提前结束抛 `StreamIOError.UnexpectedEof` |
| `Int32 skip( Int32 n ) throws` | 丢弃 n 字节（返回实际跳过数） |
| `ByteBuffer readAll( Int32 maxBytes = 0 ) throws` | 读到 EOF 全量返回；maxBytes > 0 为上限（超限抛 `FrameTooLarge`） |
| `void writeByte( UInt8 b ) throws` | 写单字节 |

### 2.4 生命周期

| 方法 | 语义 |
|------|------|
| `void closeRead() throws` | 关读侧（TcpStream 映射 `shutdown(Read)`） |
| `void closeWrite() throws` | 关写侧（TcpStream：先 flush 再 `shutdown(Write)`，对端 read 见 EOF） |
| `void close() throws` | 完全关闭（幂等；网络实现同时取消在途等待） |
| `get bool isClosed()` | 已关闭 |
| `get bool isEof()` | 已读到 EOF |

### 2.5 超时与位置

| 成员 | 语义 |
|------|------|
| `set Int32 readTimeoutMs` / `set Int32 writeTimeoutMs` | 挂起等待超时窗口；`canTimeout == false` 的实现抛 `StreamIOError.NotSupported`（网络子类经 socket 生效，超时抛 `NetError` / `TlsError.Timeout`） |
| `get Int64 position()` / `get Int64 length()` | 位置 / 长度（不可 seek 的实现返回 0——getter 不支持 throws 的语言限制） |
| `Int64 seek( Int64 offset, SeekOrigin origin ) throws` | 寻位；`canSeek == false` 抛 `NotSupported` |

**SeekOrigin extends Int32**：`Begin=0` / `Current=1` / `End=2`。

### 2.6 静态工厂

| 工厂 | 说明 |
|------|------|
| `static ByteStream memory()` | 空内存流 |
| `static ByteStream memory( ByteBuffer initial )` | 带初始内容的内存流 |
| `static ByteStream wrapReadOnly( ByteBuffer buf )` | 只读包装（零拷贝视图） |

---

## 3. MemoryStream — 内存流

工厂 `ByteStream.memory(...)` 的返回形态（可 seek、readerIndex 即 position）：

```sl
ByteStream s = ByteStream.memory()
s.write( Utf8.encode( "hello" ) )
s.seek( 0, SeekOrigin.Begin )            # 回到开头再读
ByteBuffer one = s.readExactly( 5 )

s.toByteBuffer()                          # 剩余内容视图
UInt8Array all = s.toArray()              # 拷贝为数组
s.reset()                                 # 双索引复位
```

---

## 4. StreamIOError — L0 错误码

**StreamIOError extends Error**（ByteStream 体系，与网络层 NetError 相互独立）：

| 枚举 | code | 含义 |
|------|------|------|
| NotSupported | 1 | 能力位不允许（不可 seek 却 seek、不可超时却设超时、UdpStream.write 等） |
| UnexpectedEof | 2 | `readExactly` 未能读满 |
| Closed | 3 | 已关闭的流上操作 |
| Timeout | 4 | 超时（预留） |
| InvalidPosition | 5 | seek 位置非法 |
| FrameTooLarge | 6 | `readAll` 超 maxBytes 上限 |
| OpenFailed | 7 | 打开失败（文件流） |
| IoError | 8 | 底层读写失败 |

---

## 5. NetStream — 网络流基类（Std/Net）

承载 socket 句柄 + 连接态 + 地址查询 + NoDelay 直通；四个子类的获取方式与语义对比：

| 维度 | TcpStream | UdpStream | TlsStream | WebSocketStream |
|------|-----------|-----------|-----------|-----------------|
| 获取 | `Tcp.connect` / `accept()` / `Tcp.streamOf()` | `Udp.open` / `openAddress()` | `TlsStream.wrap` / `accept` | `WebSocketStream.connect` / `accept` |
| read 语义 | 字节流，0 = EOF | 一次一报，0 = 空报 / EOF | 解密读，0 = EOF | 按消息，0 = 关闭 |
| write 语义 | 全量写出 | 不支持（`sendTo`） | 加密全量写 | 不支持（`sendText`/`sendBinary`） |
| 半关闭 | ✓ closeRead / closeWrite | ✗ | ✗（无半关闭） | closeWrite ≈ 尽力 sendClose |
| 基类扩展 | — | `sendTo` + `datagrams()` + `lastFrom*` | 超时复用底层 TCP | `receive` / `messages` + 状态查询 |

NetStream 自身公共成员：

| 成员 | 语义 |
|------|------|
| `get bool isConnected()` | 连接态 |
| `get string remoteAddress()` / `get Int32 remotePort()` | 对端（Udp 无 connect 对端时空 / 0） |
| `void setNoDelay( bool enabled )` | TCP_NODELAY 直通 |
| `void setReadTimeout( Int32 timeoutMs )` | 读挂起超时窗口（`<= 0` 清除；超时抛 `NetError.Timeout`，连接保持可用） |
| `void setWriteTimeout( Int32 timeoutMs )` | 写挂起超时窗口（UDP sendTo 不挂起，不受影响） |

TcpStream / UdpStream 细节见 [tcp.md](tcp.md) / [udp.md](udp.md)。

---

## 6. TlsStream — TLS 加密流（mbedTLS 装饰器）

在已建立的明文 TcpStream 上完成 TLS 握手，之后 read / write 自动加解密；握手与收发的挂起全部透明（未就绪协程挂起，不阻塞 VM 线程）。mbedTLS 3.6.7 源码直编，支持 TLS 1.2 / 1.3（1.3 仅 ephemeral 密钥交换）。

### 6.1 客户端（wrap）

```sl
import Std;
import Core;

TlsDemo
{
    static fun()
    {
        # 1. 先建明文 TCP
        Net.TcpStream raw = Net.Tcp.connect( "127.0.0.1", 9000 )

        # 2. 配置 TLS 参数（字段直接赋值）
        Net.TlsOptions opt = Net.TlsOptions()
        opt.caPem = File.readAllText( "ca.pem" )    # 显式 CA；空串 = 跳过验证（仅测试场景）
        opt.hostname = "example.com"                # SNI + 主机名校验

        # 3. 客户端握手（成功后 raw 所有权转移，勿再操作 raw）
        Net.TlsStream tls = Net.TlsStream.wrap( raw, opt )

        tls.write( Utf8.encode( "hello" ) )
        ByteBuffer buf = ByteBuffer( 4096 )
        Int32 n = tls.read( buf )                   # 解密读；0 = EOF
        tls.close()                                 # 先 close_notify 再关 TCP
    }
}
```

### 6.2 服务端（accept）

```sl
Net.TcpServer srv = Net.Tcp.listen( 9443 )
Net.TcpStream client = srv.accept()                # 先 accept 明文连接

Net.TlsOptions opt = Net.TlsOptions()
opt.certPem = File.readAllText( "server.pem" )     # 服务端必填：本端证书 + 私钥
opt.keyPem  = File.readAllText( "server.key" )

Net.TlsStream tls = Net.TlsStream.accept( client, opt )   # 服务端握手（挂起透明）
```

### 6.3 TlsOptions 字段

| 字段 | 说明 |
|------|------|
| `string caPem` | CA 证书链 PEM 文本（验证对端）。**空串 = 跳过验证**（信任所有对端，仅自签 / 内网测试） |
| `string certPem` | 本端证书 PEM（服务端必填；客户端双向认证时提供） |
| `string keyPem` | 本端私钥 PEM（与 certPem 配对） |
| `string hostname` | 客户端：SNI 发送 + 证书主机名校验；空串 = 不发送也不校验 |

### 6.4 TlsStream 方法

| 方法 | 语义 |
|------|------|
| `static TlsStream wrap( TcpStream raw, TlsOptions opt ) throws` | 客户端握手；成功后 **raw 所有权转移**，失败抛 `TlsError` 并关闭底层连接（半程握手后连接状态已污染，不可复用明文重试） |
| `static TlsStream accept( TcpStream raw, TlsOptions opt ) throws` | 服务端握手（语义同上） |
| `override Int32 read( ByteBuffer dst ) throws` | 解密读：0 = EOF（对端关闭） |
| `override void write( ByteBuffer src ) throws` | 加密写：全量写出 |
| `override void flush() throws` | 空操作（mbedTLS 记录直写内核，无用户态缓冲） |
| `override void close() throws` | **先销毁 TLS 会话（发 close_notify）再关底层 TCP**（幂等） |
| （继承 NetStream） | `isConnected` / `remoteAddress` / `remotePort` / `setNoDelay` / `setReadTimeout` / `setWriteTimeout` 均经底层 TCP 透传 |

### 6.5 超时语义（随装饰继承）

TLS **无独立超时配置**——挂起等待（握手 / 收 / 发）复用底层 TCP 的读 / 写超时锚点：

```sl
tls.setReadTimeout( 3000 )      # read / 握手读阶段最多等 3s（<= 0 = 清除，默认无限）
tls.setWriteTimeout( 5000 )     # write / 握手写阶段挂起窗口
```

窗口自首次挂起锚定（跨挂起重执行不重置）；超时抛 **`TlsError.Timeout`**（注意是 TlsError，非 `NetError.Timeout`），会话保持可用。也可在 wrap 前对 raw 设置——锚点挂同一底层 sid，效果等同。

**TLS 无半关闭**：`closeRead` / `closeWrite` 未覆写（基类默认仅置位封口，不动底层会话）；close_notify 是双向收尾，统一走 `close()`。

### 6.6 TlsError

**TlsError extends Error**（与 NetError 相互独立，code 镜像 C VM `sys_tls.h`）：

| 枚举 | code | 触发场景 |
|------|------|------|
| Init | 1 | TLS 库初始化失败 |
| BadParam | 2 | 参数无效（句柄失效 / 证书组合不完整等） |
| Alloc | 3 | 内存不足 |
| Parse | 4 | 证书 / 私钥 PEM 解析失败 |
| Verify | 5 | 证书链验证失败 / 主机名不匹配 |
| Handshake | 6 | TLS 握手失败（协议错误 / 对端拒绝） |
| Closed | 7 | 会话已关闭 |
| IoError | 8 | 底层读写失败 |
| Timeout | 9 | TLS 挂起等待超时（复用底层 TCP 超时窗口） |

---

## 7. Stream&lt;T&gt; — L1 元素流（Core/IO）

网络场景的标准范式是**引流协程**：一条协程循环从 L0 流读数据 → `StreamController.add` 灌入 → 消费方 `listen` 逐元素处理（`datagrams()` / `messages()` / `Stream.fromByteStream` 内部均为此模式）。

### 7.1 消费（listen）

```sl
Stream<Net.UdpDatagram> dgs = udpStream.datagrams()
dgs.listen( void( Net.UdpDatagram dg )
{
    Console.println( dg.address + " -> " + Utf8.decode( dg.payload ) )
} )                                  # 返回 StreamSubscription，可 cancel()
```

### 7.2 变换与聚合

| 方法 | 语义 |
|------|------|
| `StreamSubscription listen( Function onData )` | 订阅（返回值可 `cancel()` 取消） |
| `Stream<T> where( Function predicate )` | 过滤 |
| `Stream<T> take( int count )` / `Stream<T> skip( int count )` | 取前 N / 跳过 N |
| `Task forEach( Function action )` | 逐元素执行（Task 完成即流结束） |
| `Task toListThenTask()` | 收集为列表（`await` 后 `as Array<T>`） |

### 7.3 静态工厂

| 工厂 | 说明 |
|------|------|
| `static Stream<T> fromByteStream( ByteStream src, Codec<T, ByteBuffer> codec )` | **L0→L1 桥接**：引流协程循环读 src，经 codec 解码逐元素 add |
| `static Stream<T> fromIterable( Array<T> items )` | 数组转流 |
| `static Stream<T> generate( int count, Function gen )` | 生成 count 个 |
| `static Stream<T> periodic( Int64 millis, Function tick )` | 定时滴答流 |
| `static Stream<T> empty()` / `static Stream<T> failed( object errInfo )` | 空流 / 立即出错流 |

### 7.4 StreamController&lt;T&gt; — 生产侧

```sl
StreamController<ByteBuffer> ctrl = StreamController<ByteBuffer>()
function pump = function()
{
    ByteBuffer buf = ByteBuffer( 4096 )
    while true
    {
        Int32 n = conn.read( buf )          # 挂起读底层
        if n == 0 { break }
        ctrl.add( buf )                     # 灌给订阅者
        buf.clear()
    }
    ctrl.close()                            # 收尾（订阅方 onDone）
}
spawn pump()
Stream<ByteBuffer> items = ctrl.stream      # .stream getter 取消费端
```

| 方法 | 语义 |
|------|------|
| `void add( T value )` | 推一个元素 |
| `void addError( object errInfo )` | 推一个错误（订阅方 onError） |
| `void close()` | 结束流 |
| `get Stream<T> stream()` | 消费端 |

### 7.5 典型组合（L0 + L2 → L1）

```sl
# LengthPrefix varint 分帧 + ProtoCodec 解码 = 消息元素流
Net.TcpStream conn = Net.Tcp.connect( "127.0.0.1", 9000 )
Stream<Message> messages = LengthPrefix.decodeStream<Message>( conn, ProtoCodec<Message>() )

# 或经 Stream.fromByteStream 组装
Stream<ByteBuffer> chunks = Stream.fromByteStream( conn, RawCodec() )
```

详见 [../../Core/ProtocalBuffers.md](../../Core/ProtocalBuffers.md) §7 与 [../../Core/Stream.md](../../Core/Stream.md)。

---

## 8. L2：Converter / Codec（一览）

`Core/IO/Codec.sl` 的同步转换层（**永不挂起、无 throws**），为 L1 桥接提供协议无关的编解码插槽：

| 类型 | 关键成员 |
|------|------|
| `Converter<S,T>` | `convert(S)→T`、`startChunkedConversion(sink)`、`fuse` 级联 |
| `Codec<S,T> extends Converter` | abstract `get encoder` / `get decoder`；`encode` / `decode` / `fuseCodec` |
| `ChunkedConversionSink<T>` | `add(T)` / `close()`（流式转换的出口） |

`ProtoCodec<T>` 即 Codec 体系的流式落地（LengthPrefix 分帧），见 [../../Core/ProtocalBuffers.md](../../Core/ProtocalBuffers.md) §7。

---

## 9. 已知偏差 / 限制

| 事项 | 说明 |
|------|------|
| position / length 返回 0 | getter 不支持 throws，不可 seek 的实现无法报错，只能返回 0 |
| UdpStream.write 守卫 | Phase 1 无 connect(对端) 语义，固定对端写入抛 NotSupported |
| 空报与 EOF 共用 0 值 | UDP 空报是合法报文（非错误），需结合上下文区分 |
| TLS 超时抛 TlsError.Timeout | 而非 NetError.Timeout（错误族独立，catch 时注意判别） |
| 超时窗口锚定语义 | 自首次挂起锚定，跨挂起重执行不重置；超时后连接保持可用 |
| 超时水位仅存值 | 高低水位标记当前仅存储不生效（Phase 1） |
| 网络流不可跨 isolate | 四个网络流类均不可 Sendable；解析业务丢 isolate 时先取出可发送数据 |

---

## 10. 测试

- `test/Other/NetTest/NetStreamComposeTest.sl`（G 组：Stream 组合 / Channel 解耦）
- `test/Other/NetTest/TlsTest.sl`（K 组：TLS 握手 / 收发 / 超时，端口 19351~19353，证书在 `Resources/cert.pem` / `key.pem`）
- `test/Other/NetTest/NetTimeoutTest.sl`（T 组：读写超时锚定）
- `test/BaseTest/StreamTest.sl`（L1 元素流本体）
- 运行：

```powershell
cd d:\project\lang\simple_language
dotnet run --project project\CSimpleVMStdTest -- test\Other\NetTest\ProjectTest.sp
```
