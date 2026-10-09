# WebSocket — Net.WebSocketStream / Net.WebSocketClient

> 位置：`source/Front/Lib/Std/Net/Websocket.sl`（namespace `Net`，RFC 6455 纯 SL 实现）
> 关系：握手复用 `HttpHeaders`/`Uri`（[http.md](http.md)）；承载于 `TcpStream` / `TlsStream`（[stream.md](stream.md)）；wss 走 `TlsStream.wrap`
> 总览：[net.md](net.md) §11；测试：`test/Other/WebsocketTest/WebsocketTest.sl`（协议层 A~H 组全量）

---

## 1. 快速上手

```sl
using Net

# ---------- (1) 客户端：同步 echo 往返 ----------
Net.WebSocketStream ws = Net.WebSocketStream.connect( "ws://127.0.0.1:19370/chat" )
ws.sendText( "hello" )
Net.WsMessage msg = ws.receive()          # null = 连接关闭
Console.println( msg.text() )            # "hello"
msg.release()                            # 载荷归还
ws.sendClose()                           # 优雅关闭（1000）

# ---------- (2) 服务端：在已 accept 的 TcpStream 上应答握手 ----------
Net.TcpServer server = Net.Tcp.bind( "127.0.0.1", 19370, void( Net.TcpStream c )
{
    Net.WebSocketStream conn = Net.WebSocketStream.accept( c )
    Net.WsMessage m = conn.receive()
    conn.sendText( m.text() )            # echo
    m.release()
    conn.close()
} )

# ---------- (3) 回调式客户端（WebSocketClient 门面） ----------
var c = Net.WebSocketClient()
c.onOpen = function()
{
    c.sendText( "hi" )
}
c.onMessage = function( Net.WsMessage m )
{
    Console.println( "recv: " + m.text() )
    m.release()
}
c.onClose = function( Net.WsCloseInfo info )
{
    Console.println( "closed clean=" + info.wasClean )
}
c.connect( "ws://127.0.0.1:19370/chat" ) # 握手失败原地抛
c.waitClosed()                           # 等 onClose 送达

# ---------- (4) 协程异步连接 ----------
Task t1 = Net.WebSocketStream.connectAsync( "ws://127.0.0.1:19378/async1" )
Task t2 = Net.WebSocketStream.connectAsync( "ws://127.0.0.1:19378/async2" )
object r1 = Coroutine.awaitTask( t1 )
Net.WebSocketStream a = r1 as Net.WebSocketStream
```

---

## 2. Net.WebSocketStream

`extends NetStream`，消息粒度流（`_isMessageOriented = true`，裸 `write` 不支持）。

### 2.1 连接 / 接入（静态）

| 成员 | 签名 | 说明 |
|------|------|------|
| connect | `static WebSocketStream connect( string url ) throws` | 连 ws/wss 端点（默认选项） |
| connect | `static WebSocketStream connect( string url, WsOptions opt ) throws` | 建连（TCP/TLS）→ 发 Upgrade 握手 → 校验 101/Accept |
| connectAsync | `static Task connectAsync( string url )` | 协程异步连接，`awaitTask` 取回（`object as WebSocketStream`） |
| connectAsync | `static Task connectAsync( string url, WsOptions opt )` | 带选项异步连接 |
| accept | `static WebSocketStream accept( TcpStream raw ) throws` | 服务端：在已 accept 的 TCP 连接上应答握手 |
| accept | `static WebSocketStream accept( TcpStream raw, WsOptions opt ) throws` | 同上，带选项 |

- 握手内容：`Sec-WebSocket-Key`（随机 16 字节 Base64）+ `Sec-WebSocket-Accept` = Base64(SHA-1(key + `258EAFA5-E914-47DA-95CA-C5AB0DC85B11`))，**比对大小写敏感**。
- 非 ws/wss scheme 抛 `HttpError.UnsupportedScheme`；握手失败自动关连接后抛 `WsError.Handshake`。
- 子协议协商：客户端校验服务端回显是否在请求列表内；服务端取「服务端声明 ∩ 客户端请求」交集，无交集抛 `WsError.Handshake`。

### 2.2 状态查询

| 成员 | 签名 | 说明 |
|------|------|------|
| state | `get WsState state()` | 连接状态机（见 §5） |
| closeInfo | `get WsCloseInfo closeInfo()` | 对端 Close 帧解析结果，未收到为 `null` |
| protocol | `get string protocol()` | 协商成功的子协议，未协商为空串 |
| isClient | `get bool isClient()` | 是否客户端角色（决定掩码方向） |

### 2.3 收发

| 成员 | 签名 | 说明 |
|------|------|------|
| receive | `WsMessage receive() throws` | 同步收**一条完整消息**（分片已重组、控制帧已消化）；**返回 null = 连接关闭** |
| messages | `Stream<WsMessage> messages()` | 引流协程模式：后台循环 receive 灌进 `Stream<WsMessage>`（容量 64），关闭/出错自动 close 流 |
| sendText | `void sendText( string text ) throws` | 发文本消息（UTF-8，opcode=Text） |
| sendBinary | `void sendBinary( ByteBuffer payload ) throws` | 发二进制消息（内部 slice 视图，src 不被读空） |
| sendBinary | `void sendBinary( UInt8Array payload ) throws` | 发二进制消息（字节数组重载） |
| sendPing | `void sendPing() throws` | 心跳探测（空载荷 Ping） |
| sendPong | `void sendPong() throws` | 心跳应答（autoPong 已自动回，通常无需手调） |
| sendClose | `void sendClose() throws` | 优雅关闭，默认 `1000 Normal` |
| sendClose | `void sendClose( Int32 code, string reason ) throws` | 指定状态码/原因；**幂等**（已发过直接返回）；reason 截断到 123 字节 |
| close | `override void close() throws` | 尽力发 Close → 关底层流 → 封口（任一步失败不阻断收尾） |

**receive 内部行为**：收到 Ping 自动回 Pong（`autoPong`）；收到 Close 解析并回显协商后返回 `null`。心跳与关闭协商对调用方透明。

**ByteStream 契约适配**：`read(dst)` 一次收一条消息写入 dst（超容量部分丢弃，0 = EOF）；`closeWrite()` = 尽力发 Close。

---

## 3. Net.WsMessage

一条完整消息（分片已重组）。

| 成员 | 类型/签名 | 说明 |
|------|-----------|------|
| op | `Int32` 字段 | `WsOpCode.Text` / `WsOpCode.Binary` |
| payload | `ByteBuffer` 字段 | 载荷，**归属本消息** |
| isText / isBinary | `get bool` | 操作码判定 |
| size | `get Int32` | 载荷字节数 |
| text | `string text()` | 载荷按 UTF-8 解码（内嵌 NUL 截断） |
| binary | `ByteBuffer binary()` | 载荷原样返回（release 前有效） |
| release | `void release()` | **归还载荷（幂等）**；消费完必须调用，勿跨 release 持有引用 |

---

## 4. Net.WsOptions

| 字段 | 类型 | 默认 | 说明 |
|------|------|------|------|
| protocols | `string` | "" | 子协议（逗号分隔，如 `"chat,superchat"`；空 = 不协商） |
| maxMessageSize | `Int32` | 16777216 | 单条消息上限字节（含分片重组后），超出抛 `WsError.MessageTooBig` |
| handshakeTimeoutMs | `Int32` | 10000 | 握手建连超时（透传 `Tcp.connectTimeout`） |
| autoPong | `bool` | true | 收到 Ping 自动回 Pong（receive 循环内尽力发送） |
| caPem | `string` | "" | wss CA 证书链 PEM；**空 = 跳过验证**（与 `TlsOptions.caPem` 一致）⚠ |

---

## 5. 枚举

**WsOpCode extends Int32**（帧操作码）：

| 值 | 名称 | | 值 | 名称 |
|----|------|-|----|------|
| 0 | Continuation | | 8 | Close |
| 1 | Text | | 9 | Ping |
| 2 | Binary | | 10 | Pong |

**WsCloseCode extends Int32**（Close 状态码，RFC 6455 §7.4.1 常用子集）：

| 值 | 名称 | | 值 | 名称 |
|----|------|-|----|------|
| 1000 | Normal | | 1008 | PolicyViolation |
| 1001 | GoingAway | | 1009 | MessageTooBig |
| 1002 | ProtocolError | | 1011 | InternalError |
| 1003 | UnsupportedData | | | |

（1005 = 无状态码，仅解析语义，不在枚举内；1007 InvalidPayload 亦在枚举中。）

**WsState extends Int32**（连接状态机）：

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Connecting | 握手中（仅 connect 流程内部短暂出现） |
| 1 | Open | 已建立，可收发 |
| 2 | Closing | 已发 Close，等待对端回显 |
| 3 | Closed | 已关闭（收到回显或底层断开） |

---

## 6. Net.WebSocketClient — 回调式门面

HttpClient 风格门面 + 事件模型（onOpen / onMessage / onClose / onError）。连接前赋值回调与配置，`connect` 后启动内部分发协程。

### 6.1 回调（`Function` 字段，连接前赋值，null = 不通知）

| 回调 | 形参 | 时机 |
|------|------|------|
| onOpen | 无 | 握手完成，**恰一次** |
| onMessage | `WsMessage msg` | 每条 Text/Binary；**msg 归回调所有，消费完 `m.release()`** |
| onClose | `WsCloseInfo info` | 连接收尾恰一次（**info 可能为 null** = 异常断开） |
| onError | `Error err` | 事件循环异常（协议违例 / onMessage 内抛错），0..1 次；未设则吞 |

事件顺序：`onOpen 恰一次 → onMessage 0..N → [onError 0..1] → onClose 恰一次`。

### 6.2 配置字段

`protocols` / `caPem` / `handshakeTimeoutMs` / `maxMessageSize` / `autoPong`（语义同 WsOptions 同名字段）+ `closeTimeoutMs`（`Int32`，默认 5000：优雅关闭等对端回显的超时，超时强制收尾防事件循环挂起）。

### 6.3 方法

| 成员 | 签名 | 说明 |
|------|------|------|
| connect | `WebSocketClient connect( string url ) throws` | 握手失败**原地抛**；成功启动事件循环；重复 connect 抛 `WsError.Closed`；重连 = 新建实例 |
| sendText / sendBinary / sendPing | 同 WebSocketStream | 转发底层连接（未连接抛 `WsError.Closed`） |
| close | `void close()` | 优雅关闭：发 Close 等回显，事件循环收尾触发 onClose |
| close | `void close( Int32 code, string reason )` | 指定状态码优雅关闭 |
| abort | `void abort()` | 立即强制关闭，不等回显（onClose 的 info 可能为 null） |
| waitClosed | `void waitClosed()` | 等事件循环结束（onClose 已送达） |
| state / closeInfo / protocol | `get` | 查询底层连接（未连接时 state=Closed、closeInfo=null、protocol=""） |
| isOpen | `get bool isOpen()` | state == Open |

---

## 7. Net.WsCloseInfo

| 字段 | 类型 | 说明 |
|------|------|------|
| code | `Int32` | 状态码；载荷 < 2 字节非法按 **1005** 处理 |
| reason | `string` | 关闭原因文本（可为空串） |
| wasClean | `bool` | 收到对端 Close 即视为 clean |

---

## 8. Net.WsError 错误码

`WsError extends Error`：

| code | 名称 | 触发场景 |
|------|------|----------|
| 1 | Handshake | 非 101 / 头缺失或非法 / Accept 不匹配 / 子协议无交集 |
| 2 | Protocol | 帧违例：RSV 非零 / 掩码方向错 / 控制帧约束 / 未知 opcode / 分片错序 |
| 3 | MessageTooBig | 消息超出 maxMessageSize（含分片重组后、127 长度段超 Int32） |
| 4 | Closed | 已关闭连接上收发 |
| 5 | RandomSource | 随机源不可用（Key / 掩码生成失败） |

底层 I/O 失败（TCP/TLS）以 **NetError / TlsError 原样透传**。异常捕获写法：

```sl
wsBlock: {
    Net.WebSocketStream w = Net.WebSocketStream.connect( url )
    # ...
} catch e {
    ( e as Net.WsError ).code == Net.WsError.Handshake
}
```

---

## 9. 语义要点

1. **掩码方向**（RFC 6455 §5.1）：客户端**发帧强制掩码**、收帧禁止掩码；服务端反之，双向校验，违例抛 `WsError.Protocol`。
2. **分片重组**：receive 自动按 FIN + Continuation 重组分片消息；首帧开分片、Continuation 续、FIN 终结；错序（裸 Continuation / 分片中插新首帧）抛 Protocol。
3. **控制帧约束**：Close/Ping/Pong 必须 FIN=1 且载荷 ≤ 125 字节；RSV 必须为 0（**无扩展协商：permessage-deflate 未实现**，见 net.md §16）。
4. **消息粒度流**：WebSocketStream 是 NetStream 的消息子类，`write()` 不可用（发消息走 sendText/sendBinary）；`read(dst)` 一次 = 一条消息（超 dst 容量部分**丢弃**）。
5. **载荷所有权**：WsMessage.payload 归消息所有，消费完必须 `release()` 归还（幂等）；`messages()` 流与 `onMessage` 回调中的消息同样如此。
6. **关闭协商**：receive 到 Close 会解析 closeInfo 并**先回显再置 Closed**；close() 尽力发 Close → 关底层流，任一步失败不阻断收尾；对端不回显时 WebSocketClient 靠 closeTimeoutMs 强制收尾。
7. **Option A 挂起协议**：`root fun()` 入口可直接 connect/receive；协程内未就绪时挂起（详见 [net.md](net.md)）。
8. **messages() 引流协程**：`Stream<WsMessage>` 容量 64，连接正常关闭时流正常 close；异常路径（state != Closed 退出）先 `addError(WsError.Closed)` 再 close——`try?` 语义把异常关闭与正常关闭合流为循环结束。
9. **服务端组合**：无独立 WebSocketServer——`Net.Tcp.bind`/`TcpServer` accept 出 `TcpStream` 后用 `WebSocketStream.accept` 升级（§1 示例）；wss 服务端同理可先 `TlsStream.accept`。

---

## 10. 测试与运行

| 用例 | 位置 | 覆盖 |
|------|------|------|
| 协议层 A~H 组 | `test/Other/WebsocketTest/WebsocketTest.sl` | 握手/帧编解码/掩码方向/分片重组/控制帧/Close 协商/错误码；messages() Stream 消费；H 组 connectAsync 两路并发 + waitAll2 + echo 往返（端口 19378） |

```powershell
cd d:\project\lang\simple_language
dotnet run --project project\CSimpleVMStdTest -- test\Other\WebsocketTest\WebsocketTest.sp
```
