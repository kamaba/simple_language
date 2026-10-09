# 网络编程（Net）：Tcp / Udp / Tls / Http / WebSocket / NetStream

> **本文档以「当前实现」为准**，对应 `source/Front/Lib/Std/Net/`（`TcpSocket.sl` / `UdpSocket.sl` / `NetStream.sl` / `TlsStream.sl` / `Uri.sl` / `HttpClient.sl` / `Websocket.sl`）
> 与 `test/NetTest/`（A~L 组验收用例——含 T 超时 / K TLS / L HTTP——全部通过）、`test/Other/WebsocketTest/`（WebSocket 验收用例）。
> 设计规格见 `csimple_lang/md/design/NET_DESIGN.md`（唯一真源）；差异与偏差见本文 §16。
> 相关文档：`md/syntax/coroutine.md`（协程——网络挂起的载体）、`md/lib/Core/Stream.md`（Stream 元素流）、
> `md/syntax/string.md` / ByteBuffer 用法见 `Core/IO/ByteBuffer.sl`。

---

## 目录

1. [模型概述](#1-模型概述)
2. [快速开始](#2-快速开始)
3. [类型总览](#3-类型总览)
4. [TCP 客户端（Tcp）](#4-tcp-客户端tcp)
5. [TCP 服务端（TcpServer）](#5-tcp-服务端tcpserver)
6. [TcpSocket 裸操作](#6-tcpsocket-裸操作)
7. [网络流（NetStream / TcpStream / UdpStream）](#7-网络流netstream--tcpstream--udpstream)
8. [UDP（Udp / UdpSocket / UdpDatagram）](#8-udpudp--udpsocket--udpdatagram)
9. [TLS 加密流（TlsStream / TlsOptions）](#9-tls-加密流tlsstream--tlsoptions)
10. [HTTP 客户端（Uri / HttpClient）](#10-http-客户端uri--httpclient)
11. [WebSocket（WebSocketClient / WebSocketStream）](#11-websocketwebsocketclient--websocketstream)
12. [错误处理（NetError / TlsError / HttpError / WsError）](#12-错误处理neterror--tlserror--httperror--wserror)
13. [组合范式](#13-组合范式)
14. [与协程 / isolate 的关系](#14-与协程--isolate-的关系)
15. [API 速查总表](#15-api-速查总表)
16. [限制与已知偏差（必读）](#16-限制与已知偏差必读)

---

## 1. 模型概述

网络层提供 **TCP / UDP 裸 socket**、**TLS 加密流** 与 **HTTP/1.1 客户端**（`Uri` / `HttpClient`；不含 DNS 解析服务与 HTTP 服务端），全部 IO 走**协程化挂起**——
未就绪时挂起当前协程、让出调度，fd 就绪后由 IO 线程唤醒续跑；**不阻塞 VM 线程**。

| 特性 | 说明 |
|---|---|
| **同步写法，异步执行** | `read` / `write` / `accept` / `recvFrom` 写法与阻塞式 API 一致，但未就绪时挂起当前协程（`CORO_BLOCK_IO`），其它协程照常推进 |
| **挂起协议 Option A** | 系统调用（`SystemTcpRecv` 等）返回「未就绪」哨兵 → C VM 挂起当前协程 → fd 就绪（IO 线程就绪后端：Windows IOCP / Linux epoll / 其余 Unix poll）→ 协程恢复后**指令重执行**自行完成读写 |
| **root 可挂起** | `fun()` 入口被包装为 root 协程，`read` / `accept` / `Coroutine.delay` 等可直接写在最外层，无需先 spawn |
| **ByteStream 子类** | `NetStream` extends `ByteStream`：`read(ByteBuffer)` / `write(ByteBuffer)` / `closeRead` / `closeWrite` / `close` 与文件流同构，可直连 Stream 体系（`LengthPrefix` 等） |
| **TLS 装饰器** | `TlsStream.wrap` / `accept` 在 `TcpStream` 上叠加 mbedTLS 加密（握手挂起透明，读写超时继承底层 TCP，见 §9） |
| **HTTP 客户端** | `HttpClient` 在 `TcpStream` / `TlsStream` 之上实现 HTTP/1.1 客户端（纯 SL，无新增系统调用）：GET / POST / PUT / DELETE / HEAD，http / https，body 三态读取（见 §10） |
| **WebSocket** | `WebSocketStream` 在 `TcpStream` / `TlsStream` 之上实现 RFC 6455（纯 SL 协议层 + 两个系统调用 `SystemSha1` / `SystemRandomFill`）：ws / wss，握手校验，帧编解码（分片 / 掩码 / 控制帧）；`WebSocketClient` 高层门面提供 onOpen / onMessage / onClose / onError 事件回调模型（见 §11） |
| **失败 = 异常** | SL 层方法 `throws` 抛 `NetError` / `TlsError` / `HttpError`（均 extends Error；HttpError 为纯 SL 错误族）；C 层用哨兵值 + `SystemNetLastError` / `SystemTlsLastError`，SL 负责归类抛出 |
| **关闭即取消** | `close()` 释放句柄的同时，C 侧取消挂在该 socket 上的全部在途等待（挂起协程以错误返回唤醒） |

### 1.1 一次 `client.read(buf)` 挂起的完整时序

```
协程 A: client.read(buf)
  └─ SystemTcpRecv(sid) ──► WOULD_BLOCK（未就绪哨兵）
        └─ C VM 挂起协程 A（CORO_BLOCK_IO），登记 fd 等待表
调度器: 运行其它就绪协程 / 空闲 park
IO 线程: 就绪后端（IOCP / epoll / poll）发现 fd 可读 ──► 唤醒协程 A 入就绪队列
调度器: 恢复协程 A ──► 指令重执行 SystemTcpRecv ──► 本次读到数据，返回字节数
```

### 1.2 源码位置

| 层 | 文件 |
|---|---|
| SL API | `source/Front/Lib/Std/Net/`（`TcpSocket.sl`、`UdpSocket.sl`、`NetStream.sl`、`TlsStream.sl`、`Uri.sl`、`HttpClient.sl`、`Websocket.sl`）+ `source/Front/Lib/Std/Encoding/Sha1.sl` |
| 系统调用注册 | `source/Front/Lib/Std/Std.jsonc` → `files[]` + `systemCalls[]`（SystemTcp* / SystemUdp* / SystemTls* / SystemSha1 共 27 项；HTTP 客户端为纯 SL 实现，复用既有系统调用）+ `Core.jsonc`（SystemRandomFill） |
| C VM 原语层 | `csimple_lang/src/lib/os/sys_net.c`（非阻塞 socket；就绪后端：Windows IOCP / Linux epoll / 其余 Unix poll）、`sys_tls.c`（mbedTLS 会话桥，vendored `third_party/mbedtls/`）、`sys_random.c`（密码学随机字节） |
| C VM 等待层 | `csimple_lang/src/vm/runtime/net/vm_net_io.c`（fd 等待表 + IO 线程） |
| C VM 系统调用 | `csimple_lang/src/vm/system_method_call/net_system_method.c`（Tcp/Udp/Tls）、`byte_buffer_system_method.c`（SystemSha1，core `sl_core_sha1.c`）、`random_system_method.c`（SystemRandomFill） |
| 验收用例 | `test/NetTest/`（A 基础收发 / B 挂起唤醒 / C 分帧 / D UDP / E 并发 / F 关闭 / G Stream 组合 / H isolate / T 超时 / K TLS / L HTTP）、`test/Other/WebsocketTest/`（握手 / echo / 分片 / ping-pong / close 协商 / Stream 消费 / 协程 / isolate） |

---

## 2. 快速开始

### TCP echo 服务器 + 客户端

```sl
import Std;
import Core;

NetDemo
{
    static fun()
    {
        # 服务端：listen + accept（无连接时挂起，不阻塞）
        Net.TcpServer srv = Net.Tcp.listen( 9000 )

        # 每连接一条协程（onConnection 内部即此模式）
        function echo = function()
        {
            Net.TcpStream c = srv.accept()
            ByteBuffer buf = ByteBuffer( 4096 )
            while true
            {
                Int32 n = c.read( buf )        # 挂起直到对端数据；0 = EOF
                if n == 0
                {
                    break
                }
                c.write( buf )                  # 全量写出（写背压由 C 层透明挂起）
                buf.clear()                     # 双索引复位，循环复用
            }
            c.close()
        }
        spawn echo()

        # 客户端：连接 + 发送 + 读回（默认 10s 连接超时）
        Net.TcpStream conn = Net.Tcp.connect( "127.0.0.1", 9000 )
        conn.write( Utf8.encode( "hello" ) )    # write 消费 readable 区
        ByteBuffer rx = ByteBuffer( 64 )
        conn.read( rx )                          # read 写入 writable 区
        Console.println( "echo = " + Utf8.decode( rx ) )
        conn.close()
        srv.close()
    }
}
```

### UDP 收发

```sl
import Std;
import Core;

NetUdpDemo
{
    static fun()
    {
        Net.UdpStream a = Net.Udp.open( 9001 )          # 绑定端口得报文流
        Net.UdpStream b = Net.Udp.open( 9002 )

        b.sendTo( Utf8.encode( "ping" ), "127.0.0.1", 9001 )   # 定向发送

        ByteBuffer dst = ByteBuffer( 64 )
        Int32 n = a.read( dst )                          # 挂起收一报写入 dst
        Console.println( Utf8.decode( dst ) )            # ping

        a.close()
        b.close()
    }
}
```

---

## 3. 类型总览

统一 `namespace Net`（`import Std;` 后以 `Net.` 前缀引用）：

| 类型 | 角色 |
|---|---|
| `Net.Tcp` | 外观类：`connect` / `listen` 静态工厂（SL 命名空间不能放裸函数） |
| `Net.TcpServer` | 监听器：`accept` 挂起等连接 |
| `Net.TcpSocket` | 裸 socket 包装（recv / send / shutdown / 地址查询），一般经 `TcpServer.acceptSocket()` 或 `Tcp.streamOf()` 接入 |
| `Net.TcpStream` | TCP 双向流（extends NetStream extends ByteStream），`Tcp.connect` / `accept()` 的返回形态 |
| `Net.Udp` | 外观类：`open` 静态工厂 |
| `Net.UdpStream` | UDP 报文流（extends NetStream），`Udp.open` 的返回形态 |
| `Net.UdpSocket` | 裸 socket 包装（recvFrom / sendTo / lastFrom 查询） |
| `Net.UdpDatagram` | 数据报值对象（payload + address + port），`datagrams()` 流的元素 |
| `Net.TlsStream` | TLS 加密流（extends NetStream），`TlsStream.wrap` / `accept` 的返回形态，装饰 `TcpStream`（见 §9） |
| `Net.TlsOptions` | TLS 会话参数（caPem / certPem / keyPem / hostname，可变字段直接赋值） |
| `Net.Uri` | URL 解析值对象：构造即解析，`scheme` / `host` / `port` / `path` / `query` / `effectivePort` / `hostHeader` / `pathAndQuery`（非法抛 `HttpError.UriFormat`，见 §10.1） |
| `Net.WebSocketClient` | WebSocket 高层客户端门面（HttpClient 风格 + Python WebSocketApp 事件模型）：`onOpen` / `onMessage` / `onClose` / `onError` 回调 + `connect` / `sendText` / `sendBinary` / `close` / `waitClosed`（见 §11.1） |
| `Net.WebSocketStream` | WebSocket 双向消息流（extends NetStream），`connect` / `accept` 的返回形态；`receive()` / `messages()` 消费、`sendText` / `sendBinary` 发送（见 §11） |
| `Net.WsMessage` | 收到的消息值对象：`op` / `payload` + `isText` / `isBinary` / `size` / `text()` / `binary()` / `release()` |
| `Net.WsCloseInfo` | 关闭信息：`code` / `reason` / `wasClean` |
| `Net.WsOptions` | 连接参数（protocols / maxMessageSize / handshakeTimeoutMs / autoPong / caPem，可变字段直接赋值） |
| `Net.WsOpCode` / `Net.WsCloseCode` / `Net.WsState` | 操作码（Text=1 / Binary=2 / Close=8 / Ping=9 / Pong=10…）/ 关闭码（Normal=1000…）/ 连接态（Connecting / Open / Closing / Closed）枚举 |
| `Net.WsError` | WebSocket 家族错误枚举（extends Error，code 1~5，见 §12） |
| `Net.HttpClient` | HTTP/1.1 客户端：`send` 完整收发（每请求一连接，自动 `Connection: close`）；静态捷径 `httpGet` / `httpPost`；协程异步形态 `httpGetAsync` / `httpPostAsync` / `sendAsync`（直接返回 Task，见 §10.4） |
| `Net.HttpRequest` | 请求描述：`method`（`Net.HttpMethod` 枚举：Get/Post/Put/Delete/Head）/ `url` / `headers` / `body` 可变字段 |
| `Net.HttpResponse` | 响应：`statusCode` / `reasonPhrase` / `protocol` / `headers` / `body` + `isOk` / `contentLength` |
| `Net.HttpHeaders` | 请求 / 响应头集合（名字大小写不敏感：add / setHeader / getHeader / contains） |
| `Net.HttpError` | HTTP 家族错误枚举（extends Error，code 1~4，Uri 解析与协议处理共用，见 §12） |
| `Net.TlsError` | TLS 错误枚举（extends Error，code 1~9，与 NetError 相互独立） |
| `Net.NetError` | 网络错误枚举（extends Error，code 1~9，见 §12） |
| `Net.SocketShutdown` | 关闭方向（Read=0 / Write=1 / Both=2） |

---

## 4. TCP 客户端（Tcp）

```sl
# 默认 10 秒连接超时
Net.TcpStream conn = Net.Tcp.connect( "127.0.0.1", 9000 )

# 自定义超时（毫秒）；主机名解析失败与连接失败分开抛
Net.TcpStream c2 = Net.Tcp.connectTimeout( "example.com", 443, 3000 )

# 裸 socket 手动精细控制后升级为流
Net.TcpSocket sock = srv.acceptSocket()
sock.setNoDelay( true )
Net.TcpStream s = Net.Tcp.streamOf( sock )
```

| 方法 | 语义 |
|---|---|
| `static TcpStream connect( string host, Int32 port ) throws` | 连接（内部 `connectTimeout(host, port, 10000)`） |
| `static TcpStream connectTimeout( string host, Int32 port, Int64 timeoutMs ) throws` | 带超时连接；未就绪时挂起；`HostNotFound`（域名解析失败）/ `Timeout` / `ConnectFailed`（握手被拒、立即拒绝等）/ `SocketCreate` 分开抛 |
| `static TcpServer listen( Int32 port ) throws` | 监听 `0.0.0.0:port`（backlog 128），失败抛 `ListenFailed` |
| `static TcpServer listenAddress( string host, Int32 port ) throws` | 监听指定地址 |
| `static TcpStream streamOf( TcpSocket socket ) throws` | 裸 socket 升级为流 |

> 连接是「拆两步」的：`BeginConnect` 发起非阻塞 connect → `ConnectWait` 挂起等待可写 / 超时。两步都在 `connectTimeout` 内部完成，SL 侧无感。

---

## 5. TCP 服务端（TcpServer）

```sl
Net.TcpServer srv = Net.Tcp.listen( 9000 )

# 形态一：accept 循环（挂起直到新连接）
Net.TcpStream client = srv.accept()          # 接入连接默认开 NoDelay
srv.setAcceptTimeout( 2000 )                 # accept 挂起最多等 2s（<= 0 = 清除，默认无限）

# 形态二：回调形态（内部 = accept 循环 + 每连接 spawnClosure1）
srv.onConnection( void( Net.TcpStream c )
{
    # 每个连接一条协程，互不阻塞
    ByteBuffer buf = ByteBuffer( 4096 )
    while true
    {
        Int32 n = c.read( buf )
        if n == 0 { break }
        c.write( buf )
        buf.clear()
    }
    c.close()
} )

Console.println( srv.port.toString() )        # 实际监听端口
srv.close()
```

| 方法 | 语义 |
|---|---|
| `TcpStream accept() throws` | 挂起直到新连接；接入连接默认 `setNoDelay(true)` |
| `TcpSocket acceptSocket() throws` | 同上但返回裸 socket（自控选项时用）；失败抛 `AcceptFailed` |
| `void setAcceptTimeout( Int32 timeoutMs )` | accept 挂起等待超时窗口；`<= 0` 清除（默认无限等待）；窗口自 accept 首次挂起锚定，超时抛 `NetError.Timeout` |
| `void onConnection( Function fn )` | 回调形态：内部 `accept` 循环，每个连接 `Coroutine.spawnClosure1( fn, client )`。**注意：该方法自身不返回**（无限循环），一般放进独立协程 |
| `get Int32 port()` | 实际监听端口（句柄失效后 0） |
| `void close()` | 关闭监听句柄（幂等） |

---

## 6. TcpSocket 裸操作

一般经 `TcpServer.acceptSocket()` 获取（`Tcp.connect` 直接返回流，无需裸操作）：

```sl
Net.TcpSocket sock = srv.acceptSocket()

# 地址信息（IPv4 点分串；句柄失效返回 ""/0）
string ra = sock.remoteAddress()
Int32  rp = sock.remotePort()
string la = sock.localAddress()
Int32  lp = sock.localPort()

# 收发（ByteBuffer 零拷贝桥接）
Int32 n = sock.recv( dst )        # 写入 dst 可写区，返回本次读入字节数；0 = EOF
sock.send( src )                  # 全量写出：推进 src.readerIndex 直到 readableBytes == 0

# 选项与关闭
sock.setNoDelay( true )
sock.shutdown( Net.SocketShutdown.Write )   # 半关闭（停发；对端 read 将见 EOF）
sock.close()                                 # 关闭并取消该句柄上的全部在途等待（幂等）

# 读写超时（作用于挂起等待；超时抛 NetError.Timeout，连接保持可用）
sock.setReadTimeout( 3000 )                  # recv 挂起最多等 3s（<= 0 = 清除，默认无限等待）
sock.setWriteTimeout( 5000 )                 # send 挂起等待窗口（内核缓冲满才挂起，罕见）
```

| 方法 | 语义 |
|---|---|
| `get bool isOpen()` | 句柄有效 |
| `get string remoteAddress()` / `get Int32 remotePort()` | 对端地址 / 端口 |
| `get string localAddress()` / `get Int32 localPort()` | 本端地址 / 端口 |
| `Int32 recv( ByteBuffer dst ) throws` | 非阻塞读，未就绪挂起；**返回 0 = EOF（对端关闭）**；负值在 SL 层转 `IoError`，句柄失效抛 `Closed` |
| `void send( ByteBuffer src ) throws` | 全量发送（写背压由 C 层挂起透明处理） |
| `void setNoDelay( bool enabled )` | TCP_NODELAY 直通 |
| `void setReadTimeout( Int32 timeoutMs )` | recv 挂起等待超时窗口；`<= 0` 清除（默认无限等待）；窗口自 recv 首次挂起锚定（跨挂起重执行不重置），超时抛 `NetError.Timeout`，连接保持可用 |
| `void setWriteTimeout( Int32 timeoutMs )` | send 挂起等待超时窗口；语义同上（UDP `sendTo` 不挂起，不受影响） |
| `void shutdown( SocketShutdown how )` | 半关闭：`Read` 停收 / `Write` 停发 / `Both` 双向 |
| `void close()` | 关闭并取消在途等待（幂等） |

---

## 7. 网络流（NetStream / TcpStream / UdpStream）

`NetStream` 是抽象基类（extends `ByteStream`），承载 socket 句柄 + 连接态 + 地址查询 + NoDelay 直通；两个子类形态由工厂决定：

| 维度 | TcpStream | UdpStream |
|---|---|---|
| 获取方式 | `Tcp.connect` / `server.accept()` / `Tcp.streamOf()` | `Udp.open` / `Udp.openAddress()` |
| `read(dst)` 语义 | 字节流：写入 dst 可写区，**0 = EOF** | 报文流：**一次 read 收一报**，超 dst 容量的尾部丢弃；0 = 空数据报（合法）或 EOF |
| `write(src)` 语义 | 全量写出 | **不支持**（无固定对端），定向发送走 `sendTo` |
| 半关闭 | `closeRead` / `closeWrite` 映射 socket shutdown | 无（UDP 无连接） |
| 定向发送 | —（有固定对端） | `sendTo( src, address, port )` |
| 报文来源 | `remoteAddress` / `remotePort` | `lastFromAddress` / `lastFromPort`（最近一次收报的来源，经 C 侧槽位查询） |
| 报文流 | — | `datagrams()` 得 `Stream<UdpDatagram>` |

```sl
# TCP 流（与文件流同构的用法）
Net.TcpStream conn = Net.Tcp.connect( "127.0.0.1", 9000 )
conn.setReadTimeout( 3000 )       # 读挂起最多等 3s（超时抛 NetError.Timeout，连接可用；<= 0 = 清除）
conn.setWriteTimeout( 5000 )      # 写挂起最多等 5s
ByteBuffer buf = ByteBuffer( 4096 )
Int32 n = conn.read( buf )       # 挂起直到有数据
conn.write( buf )                # 全量写出
conn.closeWrite()                # 半关闭：停发（先 flush 再 shutdown），对端 read 见 EOF
conn.closeRead()                 # 半关闭：停收
conn.close()                     # 完全关闭（幂等，封口两方向）

# UDP 流
Net.UdpStream u = Net.Udp.open( 9001 )
u.setReadTimeout( 2000 )                      # 收报等待超时（超时抛 NetError.Timeout）
u.sendTo( Utf8.encode( "hi" ), "127.0.0.1", 9002 )
Int32 n2 = u.read( dst )                     # 收一报
string from = u.lastFromAddress()             # 本报来源
Int32  fromPort = u.lastFromPort()
```

> **ByteBuffer 约定**（无 `of()` 工厂、无 `flip()`）：`ByteBuffer( n )` 构造分配；`read(buf)` 写入 writable 区；`write(buf)` 消费 readable 区；`buf.clear()` 双索引复位循环复用。详见 `Core/IO/ByteBuffer.sl`。

---

## 8. UDP（Udp / UdpSocket / UdpDatagram）

```sl
# 工厂：绑定端口（open = 0.0.0.0；openAddress 指定地址）
Net.UdpStream u = Net.Udp.open( 9001 )

# 定向发送 + 收报（详见 §7 表）
u.sendTo( Utf8.encode( "ping" ), "127.0.0.1", 9002 )
Int32 n = u.read( dst )

# 报文流：引流协程循环收报灌进 StreamController，消费方逐报读取
Net.UdpStream ua = Net.Udp.open( 9003 )
Stream<Net.UdpDatagram> dgs = ua.datagrams()
dgs.listen( void( Net.UdpDatagram dg )
{
    Console.println( dg.address + ":" + dg.port.toString() + " -> " + Utf8.decode( dg.payload ) )
} )
```

`UdpDatagram` 值对象：

| 属性 | 说明 |
|---|---|
| `get ByteBuffer payload()` | 报文负载（readerIndex 从 0 起，直接按 ByteBuffer 读取） |
| `get string address()` | 来源地址 |
| `get Int32 port()` | 来源端口 |

`datagrams()` 实现要点：内部 `spawnClosure0` 启动引流协程，循环 `SystemUdpRecvFrom` 收报 → `ctrl.add( UdpDatagram( buf, lastFromAddress, lastFromPort ) )`；`close()` 或底层出错时 `ctrl.close()` 收尾（close 会取消在途等待，挂起的 recvFrom 以错误返回自然退出）。

---

## 9. TLS 加密流（TlsStream / TlsOptions）

`TlsStream` 是 `TcpStream` + mbedTLS 的**装饰器**（extends NetStream，`canSeek=false` / `isDuplex=true`，背压继承底层 TCP）：在已建立的明文 TCP 连接上完成 TLS 握手，之后 `read` / `write` 自动加解密。握手与收发的挂起全部由 C VM 侧透明处理（方向交替等待可读 / 可写），SL 侧写法与普通流一致。

### 9.1 客户端（wrap）

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

        # 3. 客户端握手（挂起透明；成功后 raw 所有权转移，勿再操作 raw）
        Net.TlsStream tls = Net.TlsStream.wrap( raw, opt )

        tls.write( Utf8.encode( "hello" ) )
        ByteBuffer buf = ByteBuffer( 4096 )
        Int32 n = tls.read( buf )                   # 解密读；0 = EOF
        tls.close()                                 # 先 close_notify 再关 TCP
    }
}
```

### 9.2 服务端（accept）

```sl
Net.TcpServer srv = Net.Tcp.listen( 9443 )
Net.TcpStream client = srv.accept()                # 先 accept 明文连接

Net.TlsOptions opt = Net.TlsOptions()
opt.certPem = File.readAllText( "server.pem" )     # 服务端必填：本端证书 + 私钥
opt.keyPem  = File.readAllText( "server.key" )

Net.TlsStream tls = Net.TlsStream.accept( client, opt )   # 服务端握手（挂起透明）
```

### 9.3 TlsOptions 字段

| 字段 | 说明 |
|---|---|
| `string caPem` | CA 证书链 PEM 文本（验证对端）。**空串 = 跳过验证**（信任所有对端，仅建议自签 / 内网测试） |
| `string certPem` | 本端证书 PEM（服务端必填；客户端双向认证时提供） |
| `string keyPem` | 本端私钥 PEM（与 certPem 配对） |
| `string hostname` | 客户端：SNI 发送 + 证书主机名校验；空串 = 不发送也不校验 |

### 9.4 TlsStream 方法

| 方法 | 语义 |
|---|---|
| `static TlsStream wrap( TcpStream raw, TlsOptions opt ) throws` | 客户端握手；成功后 **raw 所有权转移**（勿再操作 raw），失败抛 `TlsError` 并关闭底层连接（半程握手后连接状态已污染，不可复用明文重试） |
| `static TlsStream accept( TcpStream raw, TlsOptions opt ) throws` | 服务端握手（语义同上） |
| `override Int32 read( ByteBuffer dst ) throws` | 解密读：写入 dst 可写区，**0 = EOF（对端关闭）** |
| `override void write( ByteBuffer src ) throws` | 加密写：全量写出（写背压由 C 层挂起透明处理） |
| `override void flush() throws` | 空操作（mbedTLS 记录直写内核，无用户态缓冲） |
| `override void close() throws` | **先销毁 TLS 会话（向对端发 close_notify）再关底层 TCP**（幂等，封口两方向） |
| （继承 NetStream） | `isConnected` / `remoteAddress` / `remotePort` / `setNoDelay` / `setReadTimeout` / `setWriteTimeout` 均经底层 TCP 透传 |

### 9.5 超时语义（随装饰继承）

TLS **没有独立的超时配置**——挂起等待（握手 / 收 / 发）复用底层 TCP 的读 / 写超时锚点（挂起时经 `libsys_tls_get_net_sid` 反查底层 sid 提交等待，锚点即挂在该 sid 上）：

```sl
Net.TlsStream tls = Net.TlsStream.wrap( raw, opt )
tls.setReadTimeout( 3000 )     # read / 握手读阶段挂起最多等 3s（<= 0 = 清除，默认无限）
tls.setWriteTimeout( 5000 )    # write / 握手写阶段挂起窗口
```

窗口自首次挂起锚定（跨挂起重执行不重置）；超时抛 `TlsError.Timeout`（注意：是 `TlsError`，非 `NetError.Timeout`），会话保持可用。也可在 wrap 之前对 raw 设置（`raw.setReadTimeout(...)`）——锚点挂在同一底层 sid 上，效果等同。

### 9.6 与设计稿的差异

- 设计稿（`csimple_lang/md/design/STREAM_DESIGN.md` §9.5）记 `static Task wrap/accept`（await 后 as TlsStream）；落地为**直接返回 TlsStream**——挂起透明（root 可挂起使 Task 包装无必要），对齐 `Tcp.connect` 风格。
- TLS **无半关闭**：`closeRead` / `closeWrite` 未覆写（基类默认仅置位封口，不动底层会话）；close_notify 是双向收尾，统一走 `close()`。

---

## 10. HTTP 客户端（Uri / HttpClient）

`HttpClient` 是 `TcpStream` / `TlsStream` 之上的 **HTTP/1.1 客户端**（纯 SL 实现，复用既有 Tcp / Tls 系统调用，无新增）：`Uri` 解析 URL → `Tcp.connectTimeout` 建连（https 再 `TlsStream.wrap`）→ 写请求 → 读完整响应。连接与收发的挂起语义与底层一致（未就绪协程挂起，不阻塞 VM 线程）。

### 10.1 Uri 解析

```sl
import Std;

UriDemo
{
    static fun()
    {
        # 构造即解析（非法输入抛 HttpError.UriFormat）
        Net.Uri u = Net.Uri( "https://example.com:8443/a/b?q=1" )
        u.scheme          # "https"（原样保留；isHttp / isHttps 比较忽略大小写）
        u.host            # "example.com"
        u.port            # 8443（URL 未显式给出为 -1）
        u.effectivePort   # 8443（缺省按 scheme 补默认：http/ws=80 / https/wss=443）
        u.path            # "/a/b"（空路径补 "/"）
        u.query           # "q=1"（不含 '?'）
        u.pathAndQuery    # "/a/b?q=1"（请求行用）
        u.hostHeader      # "example.com:8443"（Host 请求头；非默认端口拼 host:port）
        u.isHttp          # false
        u.isHttps          # true
        u.toString()      # 原始 URL 文本
    }
}
```

| 方法 / 属性 | 语义 |
|---|---|
| `_init_( string text ) throws` | **构造即解析**；null / 空串 / 缺 scheme / 缺 host / 端口非数字或超 1~65535 / 含 `@` `[` `]` 抛 `HttpError.UriFormat`。scheme 本身不限类型（`isHttp` / `isHttps` 判别；`HttpClient.send` 拒绝非 http/https） |
| `get string scheme()` | scheme（原样保留） |
| `get string host()` | 主机名 |
| `get Int32 port()` | 显式端口；URL 未给出为 -1 |
| `get string path()` | 路径（空补 `/`） |
| `get string query()` | query 原样（不含 `?`） |
| `get string pathAndQuery()` | 请求行路径（query 非空拼 `?query`） |
| `get Int32 effectivePort()` | 显式端口或 scheme 默认（http/ws=80 / https/wss=443） |
| `get string hostHeader()` | Host 请求头（非默认端口拼 `host:port`） |
| `get bool isHttp()` / `get bool isHttps()` / `get bool isWs()` / `get bool isWss()` | scheme 判别（忽略大小写；ws/wss 供 `WebSocketStream.connect` 用） |
| `override string toString()` | 原始 URL 文本 |
| `static bool eqFold( string a, string b )` | ASCII 忽略大小写全串比较（`HttpHeaders` 复用） |
| `static Int32 fold( Int32 c )` | ASCII 小写折叠（`'A'~'Z'` → `'a'~'z'`） |

> 不支持 userinfo（`user@host`）、IPv6 字面量、percent-encoding、fragment——见 §16 限制表。

### 10.2 便捷方法（httpGet / httpPost）

```sl
# GET（默认 10s 连接超时；响应读毕连接自动关闭）
Net.HttpResponse resp = Net.HttpClient.httpGet( "http://127.0.0.1:19361/a/b?q=1&r=2" )
resp.statusCode                                  # 200
resp.reasonPhrase                                # "OK"
resp.protocol                                    # "HTTP/1.1"
resp.body                                        # 响应文本（UTF-8）
resp.headers.getHeader( "Content-Type" )         # 取头（名字大小写不敏感）
resp.contentLength                               # Content-Length 头（Int64；缺失 / 非法 -1）
resp.isOk                                        # 200~299

# POST（文本 body + Content-Type；非空 body 自动带 Content-Length）
Net.HttpResponse r = Net.HttpClient.httpPost( "http://127.0.0.1:19362/echo",
    "hello=http", "text/plain" )
```

### 10.3 完整形态（HttpRequest + HttpClient）

```sl
Net.HttpRequest req = Net.HttpRequest()
req.method = Net.HttpMethod.Post                     # 请求方法枚举（Get / Post / Put / Delete / Head，默认 Get）
req.url    = "https://localhost:19363/secure"          # send 时解析；非法抛 UriFormat，非 http/https 抛 UnsupportedScheme
req.body   = "ping=1"                                 # 文本 body（UTF-8）
req.headers.setHeader( "X-Test", "abc" )              # 覆盖式（同名大小写不敏感覆盖，无重复条目）
req.headers.add( "X-Extra", "x" )                     # 追加式（允许同名头共存）

Net.HttpClient c = Net.HttpClient()
c.connectTimeoutMs = 10000                            # 连接超时（Tcp.connectTimeout 透传，默认 10000）
c.readTimeoutMs    = 5000                             # 响应读挂起超时（<= 0 = 无限等待，默认 0；超时抛 NetError.Timeout）
c.caPem            = File.readAllText( "ca.pem" )     # https CA 验证（空串 = 跳过验证，与 TlsOptions 一致）

Net.HttpResponse resp = c.send( req )                 # 发送 + 读完整响应（body 全量缓冲为文本）
resp.isOk                                             # 200~299
```

**HttpRequest 字段**（可变字段直接赋值）：

| 字段 | 说明 |
|---|---|
| `HttpMethod method` | 请求方法：`Net.HttpMethod` 枚举（`extends Int32`：Get=0 Post=1 Put=2 Delete=3 Head=4，默认 Get）。枚举作类字段类型的先例：Render/Pipeline/GpuTexture.sl 的 `EFilterMode` |
| `string url` | 目标 URL（`send` 时解析） |
| `HttpHeaders headers` | 请求头（构造自带空集合） |
| `string body` | 文本 body（UTF-8；空串 = 无 body，内嵌 NUL 会被截断） |

**HttpClient 字段**：

| 字段 | 说明 |
|---|---|
| `Int32 connectTimeoutMs` | 连接超时毫秒（`Tcp.connectTimeout` 透传，默认 10000） |
| `Int32 readTimeoutMs` | 响应读挂起超时毫秒（`<= 0` 无限等待，默认 0；超时抛 `NetError.Timeout`，对齐 §6 语义） |
| `string caPem` | https CA 证书链 PEM 文本（**空串 = 跳过验证**，与 `TlsOptions.caPem` 一致，仅自签 / 测试场景） |

**`send` 内部自动管理**（无需手工拼）：

- `Host` 头自动填 `uri.hostHeader`（显式非默认端口拼 `host:port`）
- 请求行 = `"<METHOD> <pathAndQuery> HTTP/1.1"`（method 未知值按 GET）
- body 非空且用户未带 `Content-Length` → 自动补
- 用户未带 `Connection` → 自动补 `Connection: close`（每请求一连接，见 §16 限制表）
- **不读 body 的两种情况**：`method == HttpMethod.Head`，或状态码 204 / 205 / 304

### 10.4 协程异步形态（httpGetAsync / httpPostAsync / sendAsync）

三个异步入口**直接返回 `Task`**（内部以协程执行对应同步方法），调用方无需自己包装闭包再 spawn：

```sl
# 单请求：静态异步入口，await 取回 HttpResponse
Task t1 = Net.HttpClient.httpGetAsync( "https://example.com/a" )
Net.HttpResponse r1 = await t1 as Net.HttpResponse
r1.isOk                                             # 200~299

# 并发：实例 sendAsync（connectTimeoutMs / readTimeoutMs / caPem 生效）+ waitAll2
Net.HttpClient c = Net.HttpClient()
c.readTimeoutMs = 25000
Task t2 = c.sendAsync( req2 )
Task t3 = c.sendAsync( req3 )
Coroutine.waitAll2( t2, t3 )                        # 两请求 IO 同时挂起等就绪
Net.HttpResponse r2 = await t2 as Net.HttpResponse
Net.HttpResponse r3 = await t3 as Net.HttpResponse
```

| 方法 | 语义 |
|---|---|
| `static Task httpGetAsync( string url )` | 异步便捷 GET（协程执行 `httpGet`） |
| `static Task httpPostAsync( string url, string body, string contentType )` | 异步便捷 POST |
| `Task sendAsync( HttpRequest req )` | 异步 `send`（实例配置生效） |

- 协程内全部网络 IO（connect / send / recv / TLS 握手）挂起当前协程而不阻塞 VM 线程（挂起协议 Option A，见 `md/syntax/coroutine.md` §16.2）
- 协程以异常结束 → 异常向 await 该 Task 的等待者重抛，故三个方法**无需 `throws` 声明**（调用方 try / catch 即可）

### 10.5 body 三态读取（响应侧）

| 响应形态 | 读取语义 |
|---|---|
| `Transfer-Encoding` 含 `chunked` | 逐块聚合（hex 长度行支持 `;` 后 chunk 扩展；消费 trailer 行到空行）；`resp.contentLength` 为 -1（无 Content-Length 头） |
| 有 `Content-Length` | 精确读 N 字节（上限 2e9；连接提前关闭 / 截断抛 `HttpError.Protocol`） |
| 两者皆无 | 读到 EOF（依赖 `Connection: close`：对端关连接即 body 结束） |

读取全部经预读缓冲（`ByteBuffer`）中转，跨 `read` 边界安全行解析；body 全量缓冲为文本后返回。

### 10.6 HttpHeaders（请求 / 响应共用）

| 方法 | 语义 |
|---|---|
| `void add( string name, string value )` | 追加（**允许同名头共存**，保持插入序） |
| `void setHeader( string name, string value )` | 覆盖同名头（大小写不敏感；不存在则追加）。方法名不能叫 `set`——`set` 是 SL 属性 setter 关键字 |
| `string getHeader( string name )` | 取值（大小写不敏感；不存在返回 null）。方法名不能叫 `get`——`get` 是属性 getter 关键字 |
| `bool contains( string name )` | 存在性（大小写不敏感） |
| `get int count()` | 头数量 |
| `string nameAt( int index )` / `string valueAt( int index )` | 按序取（遍历用） |

### 10.7 错误路径

- 非法 URL → `HttpError.UriFormat`（建连前抛）
- 非 http / https scheme → `HttpError.UnsupportedScheme`（建连前抛）
- 响应报文损坏（连接截断 / 非法状态行 / 非法块长度）→ `HttpError.Protocol`
- 底层网络异常（`NetError` / `TlsError`）**原样透传**（不吞不改）；异常路径尽力关连接后上抛，连接不复用

> 验收用例：`test/NetTest/HttpTest.sl`（L 组 27 项断言——L1 Uri 解析、L2 GET 明文回环、L3 POST + chunked 聚合、L4 https + setHeader 覆盖，全部通过）。

---

## 11. WebSocket（WebSocketClient / WebSocketStream）

`WebSocketStream` 是 `TcpStream` / `TlsStream` 之上的 **RFC 6455 消息流**（纯 SL 协议层，仅新增 `SystemSha1` / `SystemRandomFill` 两个系统调用）：`Uri` 解析 ws/wss URL → `Tcp.connectTimeout` 建连（wss 再 `TlsStream.wrap`）→ HTTP Upgrade 握手（`Sec-WebSocket-Key` / `Accept` 校验）→ 之后 `receive` / `sendText` / `sendBinary` 走帧协议。挂起语义与底层一致（未就绪协程挂起，不阻塞 VM 线程）。

在此之上提供两层消费形态：

| 形态 | 类型 | 适用 |
|---|---|---|
| **高层门面（推荐）** | `WebSocketClient` | Python `WebSocketApp` 风格事件回调：`onOpen` / `onMessage` / `onClose` / `onError`，connect 后自动跑事件循环，收发即调（见 §11.1） |
| 底层流 | `WebSocketStream` | 自管收发循环 / 服务端接入 / Stream 体系消费（`messages()`）/ 精细状态控制（见 §11.2~11.9） |

### 11.1 高层客户端（WebSocketClient 事件模型）

`WebSocketClient` 是 HttpClient 风格门面 + Python `websocket-client`（`WebSocketApp`）事件模型：**先赋回调与配置字段，`connect` 建连握手并启动事件循环协程，之后收到的消息自动推给 `onMessage`，连接收尾回调 `onClose`**。

```sl
import Std;
import Core;

WsClientDemo
{
    static fun()
    {
        var c = Net.WebSocketClient()

        # ---- 事件回调（连接前赋值；null = 不通知）----
        c.onOpen = function()
        {
            Console.println( "opened" )
        }
        c.onMessage = function( Net.WsMessage m )
        {
            Console.println( "recv: " + m.text() )
            m.release()                         # 载荷归回调所有，用完释放
        }
        c.onClose = function( Net.WsCloseInfo info )
        {
            # info 可能为 null（异常断开，无协商信息）
            Console.println( "closed" )
        }
        c.onError = function( Error e )
        {
            Console.println( "error: " + e.toString() )
        }

        # ---- 配置字段（连接前赋值，语义同 WsOptions）----
        c.protocols = "chat,superchat"          # 子协议协商（可选）
        c.handshakeTimeoutMs = 10000            # 握手超时（默认 10s）
        c.maxMessageSize = 16777216             # 单条消息上限（默认 16 MiB）
        c.closeTimeoutMs = 5000                 # 优雅关闭等回显超时（默认 5s）

        # ---- 连接 + 启动事件循环（握手失败原地抛，调用方直接 catch）----
        c.connect( "ws://127.0.0.1:19371/echo" )

        c.sendText( "hi" )                      # 发送直调（转发底层连接）
        c.sendBinary( Utf8.encode( "bytes" ) )
        c.sendPing()

        c.close()                               # 优雅关闭（发 Close 帧等回显；不阻塞）
        c.waitClosed()                          # 等事件循环收尾（onClose 已送达）
    }
}
```

**事件顺序契约**：`onOpen` 恰一次 → `onMessage` 0..N 次 → `[onError` 0..1 次`]` → `onClose` 恰一次。

| 回调 / 字段 | 签名 / 类型 | 语义 |
|---|---|---|
| `Function onOpen` | `function()` | 握手完成时调用一次 |
| `Function onMessage` | `function( WsMessage msg )` | 每条 Text / Binary 消息（**msg 归回调所有，消费完调 `msg.release()`**） |
| `Function onClose` | `function( WsCloseInfo info )` | 连接收尾时调用一次（info 可能为 null——异常断开无协商信息） |
| `Function onError` | `function( Error err )` | 事件循环异常（协议违例 / onMessage 内抛错）；未设置时异常吞掉 |
| `string protocols` / `string caPem` | 配置 | 子协议清单（逗号分隔）/ wss CA 链 PEM（空 = 跳过验证） |
| `Int32 handshakeTimeoutMs` / `Int32 maxMessageSize` / `bool autoPong` | 配置 | 语义同 `WsOptions` 同名字段（默认 10000 / 16777216 / true） |
| `Int32 closeTimeoutMs` | 配置 | 优雅关闭等对端回显 Close 的超时（默认 5000；超时强制收尾防事件循环挂起） |

**方法**：

| 方法 | 语义 |
|---|---|
| `WebSocketClient connect( string url ) throws` | 连接 + 握手 + 启动事件循环协程（返回 this 便于链式）；握手失败原地抛（`WsError.Handshake` 等，异常路径连接已清理）；**重复 connect 拒绝**（抛 `WsError.Closed`，重连语义 = 新建 WebSocketClient） |
| `void sendText( string text ) throws` / `void sendBinary( ByteBuffer / UInt8Array ) throws` / `void sendPing() throws` | 发送族（未连接抛 `WsError.Closed`；转发底层 `WebSocketStream` 同名方法） |
| `void close()` / `void close( Int32 code, string reason )` | 优雅关闭：发 Close 帧（失败不阻断）+ 超时看门狗协程（`closeTimeoutMs` 后对端未回显则强制关闭）；事件循环随后收尾触发 `onClose`（携带协商 `closeInfo`）。**不阻塞、不等待** |
| `void abort()` | 立即强制关闭：不等回显，事件循环立即结束（`onClose` 的 info 可能为 null） |
| `void waitClosed()` | 阻塞等事件循环结束（`onClose` 已送达后返回） |
| `get WsState state()` / `get bool isOpen()` | 连接态（未连接 = Closed）/ 是否 Open |
| `get WsCloseInfo closeInfo()` / `get string protocol()` | 关闭协商信息 / 协商出的子协议（未连接 null / ""） |

实现要点：事件循环为 `Coroutine.spawnClosure0` 启动的独立协程（`_runLoop` 静态方法），内部 `receive()` 循环把消息推给 `onMessage`；循环体受 label-catch 保护——协议违例或回调抛错先送 `onError`，随后 `onClose` 恰一次收尾并兜底释放底层连接（对端发起关闭时 receive 返回 null 但底层流未关）。回调与 this 在闭包捕获前先拷局部（`messages()` 同款约定）。

> 验收：`test/Other/WebsocketTest/`（门面事件顺序 / 回调收发 / close 协商 / abort 强断）。

### 11.2 底层客户端（WebSocketStream.connect / connectAsync）

```sl
import Std;
import Core;

WsDemo
{
    static fun()
    {
        # 最简形态（默认 10s 握手超时；wss 时 caPem 为空 = 跳过验证，同 TlsOptions）
        Net.WebSocketStream ws = Net.WebSocketStream.connect( "ws://127.0.0.1:19371/echo" )

        ws.sendText( "hello" )                      # 发文本帧（客户端自动掩码）
        Net.WsMessage m = ws.receive()              # 收一条消息（挂起直到有数据）
        if m != null && m.isText
        {
            Console.println( "echo = " + m.text() ) # "echo = hello"
            m.release()                             # 释放消息载荷（幂等）
        }

        ws.sendClose( Net.WsCloseCode.Normal, "bye" )   # 主动关闭（回显等待见 11.6）
        ws.close()
    }
}
```

| 方法 | 语义 |
|---|---|
| `static WebSocketStream connect( string url ) throws` | 连接 + 握手（默认 `WsOptions`）；非 ws / wss scheme 抛 `HttpError.UnsupportedScheme`，握手失败抛 `WsError.Handshake`（状态码非 101 / Upgrade 头不符 / Accept 不匹配等） |
| `static WebSocketStream connect( string url, WsOptions opt ) throws` | 带参数连接（opt 判 null 自动取默认值） |
| `static Task connectAsync( string url )` / `static Task connectAsync( string url, WsOptions opt )` | 协程异步形态（直接返回 Task，`await t as Net.WebSocketStream` 取回；语义同 HttpClient §10.4） |

握手细节：`Sec-WebSocket-Key = Base64(16 随机字节)`（`SystemRandomFill`）；响应 `Sec-WebSocket-Accept` 必须**精确匹配** `Base64(SHA-1(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))`（大小写敏感）；`Upgrade: websocket` 与 `Connection: upgrade` 头忽略大小写校验；子协议回显校验（客户端请求的协议之一必须出现在响应 `Sec-WebSocket-Protocol` 中）。

### 11.3 服务端（accept）

```sl
# 在已 accept 的明文 TcpStream 上完成 Upgrade 握手（wss 则先 TlsStream.accept 再传）
Net.TcpServer srv = Net.Tcp.listen( 19371 )
Net.TcpStream raw = srv.accept()
Net.WebSocketStream ws = Net.WebSocketStream.accept( raw )     # 读 Upgrade 请求 → 回 101

Net.WsMessage m = ws.receive()
ws.sendText( "pong" )
ws.close()
```

| 方法 | 语义 |
|---|---|
| `static WebSocketStream accept( TcpStream raw ) throws` | 服务端握手：读 GET 请求行 + 头（Key 非空、Upgrade / Connection / Version 校验同客户端）→ 计算 Accept 回 101 响应（含协商出的 `Sec-WebSocket-Protocol`）；成功后 **raw 所有权转移**（勿再操作 raw），失败抛 `WsError.Handshake` 并关闭底层连接 |
| `static WebSocketStream accept( TcpStream raw, WsOptions opt ) throws` | 带参数（opt.protocols 为服务端支持的子协议清单，与客户端请求交集协商） |

### 11.4 收消息（receive / messages）

```sl
# 形态一：同步循环（null = 连接关闭）
Net.WsMessage m = ws.receive()
while m != null
{
    if m.isText { Console.println( m.text() ) }
    else if m.isBinary { ByteBuffer b = m.binary() }
    m.release()
    m = ws.receive()
}

# 形态二：Stream 消费（内部引流协程泵进 StreamController，收完 / 出错自动 close）
Stream<Net.WsMessage> msgs = ws.messages()
msgs.listen( void( Net.WsMessage msg )
{
    if msg.isText { Console.println( "recv: " + msg.text() ) }
    msg.release()
} )
```

| 方法 | 语义 |
|---|---|
| `WsMessage receive() throws` | 挂起收一条**完整消息**（分片自动重组）；**null = 对端关闭 / 连接断开**（`closeInfo` 可查详情）；消息超 `maxMessageSize` 抛 `WsError.MessageTooBig` |
| `Stream<WsMessage> messages()` | 消息流：内部 `spawnClosure0` 引流协程循环 `receive` 灌进 `StreamController`（缓冲 64）；`receive` 返回 null 后 `ctrl.close()` 收尾，未正常关闭则先 `addError(WsError.Closed)` |

**协议自动处理**（对 `receive` 透明）：收到 Ping 且 `autoPong` 开启 → 自动回 Pong（尽力发送）；收到 Pong → 跳过；收到 Close → 解析关闭码后**自动回显 Close**（遵守 RFC 6455 关闭握手）再以 null 返回；Continuation 分片按 FIN 重组（乱序 / 无首发分片抛 `WsError.Protocol`）。

### 11.5 发送（sendText / sendBinary / sendPing / sendPong）

```sl
ws.sendText( "hello" )                        # 文本帧（UTF-8）
ws.sendBinary( Utf8.encode( "bytes" ) )       # 二进制帧（ByteBuffer；内部 slice 零拷贝视图）
UInt8Array arr = UInt8Array( 4 )              # 或 UInt8Array（内部拷贝为 ByteBuffer）
ws.sendBinary( arr )
ws.sendPing()                                 # 心跳探测（空载荷）
ws.sendPong()                                 # 手动回 Pong（autoPong 关闭时用）
```

| 方法 | 语义 |
|---|---|
| `void sendText( string text ) throws` | 发 Text 帧（单帧整发，不分片） |
| `void sendBinary( ByteBuffer data ) throws` / `void sendBinary( UInt8Array data ) throws` | 发 Binary 帧（前者 `slice()` 零拷贝视图，不推进原缓冲索引；后者拷贝） |
| `void sendPing() throws` / `void sendPong() throws` | 发空载荷控制帧（≤125 字节合规） |
| 发送约束 | 已 Closed 或 Closing（非 Close 帧）状态发送抛 `WsError.Closed`；**客户端帧自动掩码、服务端帧禁止掩码**（方向校验，违反对端帧掩码方向抛 `WsError.Protocol`）；RSV 位非 0 抛 `Protocol` |

### 11.6 关闭协商（sendClose / close / closeInfo）

```sl
ws.sendClose( Net.WsCloseCode.Normal, "bye" )    # 发 Close 帧（code + reason，reason 截断 123 字节）
Net.WsMessage m = ws.receive()                   # 对端回显 Close 后 receive 返回 null
Console.println( ws.closeInfo.code.toString() )  # 对端关闭码（1000 = Normal）
ws.close()                                       # 关底层连接（幂等；内部尽力 sendClose）
```

| 方法 / 属性 | 语义 |
|---|---|
| `void sendClose() throws` | 发 Normal(1000) 空原因 Close 帧（幂等：已发送过则忽略），置 `Closing` 态 |
| `void sendClose( Int32 code, string reason ) throws` | 发指定关闭码 + 原因（见 `WsCloseCode` 枚举） |
| `override void close() throws` | 尽力发 Close（失败吞掉）→ 关底层流 → 封口全部状态（幂等；分片累积缓冲一并回收） |
| `override void closeWrite() throws` | `flush` + 尽力 `sendClose`（ByteStream 半关闭语义映射） |
| `get WsCloseInfo closeInfo()` | 关闭信息：`code`（载荷 <2 字节为 1005 NoStatus）/ `reason` / `wasClean`（收到 Close 帧关闭 = true，连接直接断 = false） |
| `get WsState state()` | 连接态：`Connecting(0)` / `Open(1)` / `Closing(2)` / `Closed(3)` |
| `get string protocol()` | 协商出的子协议（未协商为 ""） |
| `get bool isClient()` | 客户端 / 服务端侧（决定掩码方向） |

### 11.7 WsOptions 字段

| 字段 | 说明 |
|---|---|
| `string protocols` | 请求 / 支持的子协议清单（逗号分隔，如 `"chat,superchat"`；协商结果回填 `ws.protocol`） |
| `Int32 maxMessageSize` | 单条消息上限字节（默认 16777216 = 16 MiB；超限抛 `WsError.MessageTooBig`） |
| `Int32 handshakeTimeoutMs` | TCP 连接超时（透传 `Tcp.connectTimeout`，默认 10000） |
| `bool autoPong` | 收 Ping 自动回 Pong（默认 true） |
| `string caPem` | wss CA 证书链 PEM（空串 = 跳过验证，同 `TlsOptions`） |

### 11.8 ByteStream 接入（read / write）

`WebSocketStream` extends `NetStream`（`isMessageOriented=true` / `canWrite=false`），可作 `ByteStream` 消费（如 `LengthPrefix` 无关场景的逐消息读取）：

| 方法 | 语义 |
|---|---|
| `override Int32 read( ByteBuffer dst ) throws` | 按消息读：`receive` 一条 Text/Binary，截取 `writableBytes` 拷入 dst 后释放；null（关闭）置 EOF 返回 0 |
| `override void write( ByteBuffer src ) throws` | 守卫拒绝（WebSocket 消息边界语义，不提供裸字节写——发送一律 `sendText` / `sendBinary`） |
| `override void flush() throws` | 空操作（发送同步完成，无用户态缓冲） |

### 11.9 协议实现要点

- **帧头 2~14 字节大端**：b0 = FIN(0x80) | opcode；b1 = MASK(0x80) | len7（126 → 2 字节 U16 扩展，127 → 8 字节 U64 扩展）；掩码 key 4 字节（有 MASK 时）；载荷 XOR 掩码逐字节
- **控制帧约束**：Close / Ping / Pong 必须 FIN=1 且载荷 ≤125 字节；opcode 3~7 / 11~15 未知值抛 `WsError.Protocol`
- **SHA-1**：`Sha1.digest / digestHex`（`Encoding/Sha1.sl`，core `sl_core_sha1.c`，FIPS 180-1），握手 Accept 计算 = `Base64.encode( Sha1.digest( key + GUID ) )`
- **随机源**：`SystemRandomFill(n)` 返回 n 字节密码学随机 `ByteBuffer`（失败返回 0 → 抛 `WsError.RandomSource`）
- 收发路径全部经 `_recvBuf`（握手预读缓冲复用）与 `ByteBuffer` 视图（`slice`），无多余拷贝

> 验收用例：`test/Other/WebsocketTest/`（握手 / echo 文本+二进制 / 分片重组 / ping-pong / close 协商 / messages() Stream 消费 / connectAsync 协程 / isolate worker 融合）。

---

## 12. 错误处理（NetError / TlsError / HttpError / WsError）

`NetError extends Error`，code 位镜像 C VM `sys_net.h` 的错误段：

| 枚举 | code | 触发场景 |
|---|---|---|
| `SocketCreate` | 1 | socket 创建失败 |
| `ConnectFailed` | 2 | 连接失败（握手被拒 / 中途断开 / 本机端口立即拒绝） |
| `HostNotFound` | 3 | 主机名解析失败（`Tcp.connectTimeout` 据此细分） |
| `Timeout` | 4 | 超时：`connectTimeout` 连接超时；recv / send / accept / recvFrom 挂起等待超时——需先经 `setReadTimeout` / `setWriteTimeout` / `setAcceptTimeout` 配置窗口（默认无限等待，见 §5/§6/§7） |
| `BindFailed` | 5 | UDP 绑定失败（端口占用等） |
| `ListenFailed` | 6 | 监听失败 |
| `AcceptFailed` | 7 | 接受连接失败 |
| `Closed` | 8 | 句柄已关闭（`recv` / `send` / `recvFrom` / `sendTo` 前置检查） |
| `IoError` | 9 | 底层读写失败（含 close 后再读） |

`TlsError extends Error`（TLS 层独立错误，code 位镜像 C VM `sys_tls.h` 的 `LIBSYS_TLS_ERR_*` 1-9 段）：

| 枚举 | code | 触发场景 |
|---|---|---|
| `TlsError.Init` | 1 | TLS 库初始化失败 |
| `TlsError.BadParam` | 2 | 参数无效（句柄失效 / 证书组合不完整等） |
| `TlsError.Alloc` | 3 | 内存不足 |
| `TlsError.Parse` | 4 | 证书 / 私钥 PEM 解析失败 |
| `TlsError.Verify` | 5 | 证书链验证失败 / 主机名不匹配 |
| `TlsError.Handshake` | 6 | TLS 握手失败（协议错误 / 对端拒绝） |
| `TlsError.Closed` | 7 | 会话已关闭 |
| `TlsError.IoError` | 8 | 底层读写失败 |
| `TlsError.Timeout` | 9 | TLS 挂起等待超时（复用底层 TCP 读 / 写超时窗口，见 §9.5） |

`HttpError extends Error`（HTTP 家族，Uri 解析与协议处理共用，纯 SL 层无 C 侧镜像）：

| 枚举 | code | 触发场景 |
|---|---|---|
| `HttpError.UriFormat` | 1 | URL 格式非法（缺 scheme / host、端口非数字或超 1~65535、含 `@` / `[` / `]` 等） |
| `HttpError.UnsupportedScheme` | 2 | scheme 不支持（`HttpClient.send` 遇非 http / https，建连前抛） |
| `HttpError.Protocol` | 3 | 响应不符合 HTTP/1.1 报文格式（连接截断 / 非法状态行 / 非法块长度等） |
| `HttpError.IoError` | 4 | 预留；当前底层 IO 异常按原样透传 `NetError` / `TlsError`，不转换为本枚举 |

`WsError extends Error`（WebSocket 层，纯 SL 协议层无 C 侧镜像）：

| 枚举 | code | 触发场景 |
|---|---|---|
| `WsError.Handshake` | 1 | 握手失败：响应状态码非 101 / `Upgrade` / `Connection` / `Sec-WebSocket-Accept` 头不符（Accept 必须与本地计算值精确比对，大小写敏感） |
| `WsError.Protocol` | 2 | 帧违规：RSV 位非 0 / 掩码方向错误（客户端帧必掩码、服务端帧禁掩码）/ 未知 opcode / 分片乱序（控制帧出现于分片中间、首帧非首） |
| `WsError.MessageTooBig` | 3 | 重组消息超过 `maxMessageSize`（默认 16 MiB）或 64 位长度字段溢出 |
| `WsError.Closed` | 4 | 已关闭（或握手未完成）后发送 |
| `WsError.RandomSource` | 5 | 握手 Key / 帧掩码随机源失败（`SystemRandomFill` 返回 0） |

> 四个枚举相互独立：TCP 层错误（连接 / DNS / 裸 socket 收发）抛 `NetError`，TLS 层错误（握手 / 证书 / 加密收发）抛 `TlsError`，HTTP 层错误（URL 解析 / 协议报文）抛 `HttpError`，WebSocket 层错误（握手校验 / 帧协议）抛 `WsError`（底层网络异常原样透传前三者）。判别写法同下（`e as Net.TlsError` / `e as Net.HttpError` / `e as Net.WsError`）。

```sl
# 预期异常的标准写法（组方法不声明 throws，正常路径裸写）
Net.TcpStream c = null
bool refused = false
refusedLabel:
{
    try c = Net.Tcp.connect( "127.0.0.1", 19999 )   # 无监听端口
    catch e
    {
        refused = ( e as Net.NetError ).code == 2    # ConnectFailed
    }
}
```

> SL 异常为非受检（unchecked）；协程闭包内不能声明 `throws`，需经 `try?` 中转到 sf 前缀静态方法（见 `test/NetTest/TcpBasicTest.sl` 编写约定）。

---

## 13. 组合范式

### LengthPrefix 分帧（Stream 体系直连）

```sl
Net.TcpStream conn = Net.Tcp.connect( "127.0.0.1", 9000 )

# 组帧：组好 ByteBuffer 再 write
ByteBuffer frame = ByteBuffer( 64 )
LengthPrefix.writeFrameBytes( frame, Utf8.encode( "hello" ) )
conn.write( frame )

# 半包解析：tryReadFrame 需显式 maxFrame，null = 半包（继续收）
ByteBuffer rx = ByteBuffer( 4096 )
conn.read( rx )
ByteBuffer one = LengthPrefix.tryReadFrame( rx, 65536 )

# 流式解码：decodeStream<T> 直接吃 ByteStream
Stream<Message> messages = LengthPrefix.decodeStream<Message>( conn, ProtoCodec<Message>() )
# 每次 moveNext 都在底层 read 处协程化挂起，直到解出一帧
```

### 并发连接（每连接一协程）

```sl
function worker = function( Net.TcpStream c )
{
    ByteBuffer buf = ByteBuffer( 4096 )
    while true
    {
        Int32 n = c.read( buf )
        if n == 0 { break }
        c.write( buf )
        buf.clear()
    }
    c.close()
}
# N 个客户端并发 echo，互不阻塞（NetTest E1：10 并发）
```

### Channel 解耦（IO 协程与业务协程）

```sl
Channel<ByteBuffer> ch = Channel<ByteBuffer>( 8 )
function ioReader = function( Net.TcpStream c )
{
    ByteBuffer buf = ByteBuffer( 4096 )
    while true
    {
        Int32 n = c.read( buf )
        if n == 0 { break }
        ch.send( buf )              # 满时挂起（天然背压）
        buf.clear()
    }
    ch.close()
}
```

更多组合（isolate 每连接一隔离、JSON 编码进流、心跳等）见 `NET_DESIGN.md` §19。

---

## 14. 与协程 / isolate 的关系

- **与协程**：网络挂起与 `Channel` / `delay` 走同一个 `CORO_BLOCK_IO` / timer 通道与唤醒路径，天然正交组合。root 协程（`fun()` 主体）可直接在网络调用上挂起——`Coroutine.delay` 期间网络协程照常被唤醒推进（NetTest B1 验证）。`WebSocketClient` 事件循环 / `WebSocketStream.connectAsync` / `messages()` 均基于该机制（消息流每条消息一唤醒）。
- **与 isolate**：`TcpStream` / `UdpStream` / `TlsStream` / `WebSocketStream` / `Channel` / `Task` **均不可 Sendable**——网络流（含 WebSocket 流）不能跨 isolate 传递。范式：网络 IO 协程留在主 VM，把「不可信 / 易崩」的协议解析（string / TransferableData 等可发送数据）丢进 isolate，解析崩了也不影响主 VM 的其它连接（NetTest H 组验证）。WebSocket 同理：`receive()` 拿到 `WsMessage` 后把 `op == WsOpCode.Text` 的消息文本丢进 isolate worker 解析（WebsocketTest G 组验证）。

---

## 15. API 速查总表

### Net.Tcp（外观）

| 方法 | 说明 |
|---|---|
| `static TcpStream connect( host, port ) throws` | 连接（10s 超时） |
| `static TcpStream connectTimeout( host, port, timeoutMs ) throws` | 带超时连接 |
| `static TcpServer listen( port ) throws` | 监听 0.0.0.0 |
| `static TcpServer listenAddress( host, port ) throws` | 监听指定地址 |
| `static TcpStream streamOf( TcpSocket ) throws` | 裸 socket 升级为流 |

### Net.TcpServer

| 方法 | 说明 |
|---|---|
| `TcpStream accept() throws` | 挂起等连接（默认 NoDelay） |
| `TcpSocket acceptSocket() throws` | 挂起等连接（裸 socket） |
| `void setAcceptTimeout( Int32 timeoutMs )` | accept 挂起超时（`<= 0` 清除；超时抛 `Timeout`） |
| `void onConnection( Function fn )` | 回调形态（不返回，每连接一协程） |
| `get Int32 port()` | 实际监听端口 |
| `void close()` | 关闭（幂等） |

### Net.TcpSocket

| 方法 | 说明 |
|---|---|
| `get bool isOpen()` | 句柄有效 |
| `get string remoteAddress()` / `get Int32 remotePort()` | 对端 |
| `get string localAddress()` / `get Int32 localPort()` | 本端 |
| `Int32 recv( ByteBuffer dst ) throws` | 读（0 = EOF） |
| `void send( ByteBuffer src ) throws` | 全量写 |
| `void setNoDelay( bool )` | TCP_NODELAY |
| `void setReadTimeout( Int32 timeoutMs )` | recv 挂起超时（`<= 0` 清除；超时抛 `Timeout`，连接可用） |
| `void setWriteTimeout( Int32 timeoutMs )` | send 挂起超时（`<= 0` 清除） |
| `void shutdown( SocketShutdown how )` | 半关闭 |
| `void close()` | 关闭（幂等，取消在途等待） |

### Net.TcpStream / Net.UdpStream（extends NetStream extends ByteStream）

| 方法 | 说明 |
|---|---|
| `override Int32 read( ByteBuffer dst ) throws` | Tcp：字节流读（0 = EOF）；Udp：收一报（0 = 空报 / EOF） |
| `override void write( ByteBuffer src ) throws` | Tcp：全量写；Udp：不支持（走 sendTo） |
| `override void flush() throws` | 空操作（无用户态缓冲） |
| `override void closeRead() throws` | Tcp：停收（shutdown Read） |
| `override void closeWrite() throws` | Tcp：停发（shutdown Write） |
| `override void close() throws` | 完全关闭（幂等） |
| `get bool isConnected()` | 连接态 |
| `get string remoteAddress()` / `get Int32 remotePort()` | 对端（Udp 无 connect 对端时空 / 0） |
| `void setNoDelay( bool )` | 选项直通 |
| `void setReadTimeout( Int32 timeoutMs )` | read / recvFrom 挂起超时（`<= 0` 清除；超时抛 `Timeout`，连接可用） |
| `void setWriteTimeout( Int32 timeoutMs )` | write（Tcp）挂起超时（`<= 0` 清除） |
| `void sendTo( ByteBuffer src, string addr, Int32 port ) throws` | **Udp 专属**：定向发送 |
| `Stream<UdpDatagram> datagrams()` | **Udp 专属**：报文流 |

### Net.Udp（外观）/ Net.UdpSocket / Net.UdpDatagram

| 方法 | 说明 |
|---|---|
| `static UdpStream open( port ) throws` | 绑定 0.0.0.0 |
| `static UdpStream openAddress( host, port ) throws` | 绑定指定地址 |
| `Int32 recvFrom( ByteBuffer dst ) throws` | 挂起收一报 |
| `void setReadTimeout( Int32 timeoutMs )` | 收报挂起超时（`<= 0` 清除；超时抛 `Timeout`，见 §7 代码示例） |
| `void sendTo( ByteBuffer src, addr, port ) throws` | 全量定向发送 |
| `get string lastFromAddress()` / `get Int32 lastFromPort()` | 最近收报来源 |
| `void close()` | 关闭（幂等） |
| `get ByteBuffer payload()` / `get string address()` / `get Int32 port()` | UdpDatagram 三属性 |

### Net.TlsStream / Net.TlsOptions（详见 §9）

| 方法 | 说明 |
|---|---|
| `static TlsStream wrap( TcpStream raw, TlsOptions opt ) throws` | 客户端握手（挂起透明；raw 所有权转移） |
| `static TlsStream accept( TcpStream raw, TlsOptions opt ) throws` | 服务端握手（同上） |
| `override Int32 read( ByteBuffer dst ) throws` | 解密读（0 = EOF） |
| `override void write( ByteBuffer src ) throws` | 加密全量写 |
| `override void close() throws` | 先 close_notify 再关底层 TCP（幂等） |
| `string caPem / certPem / keyPem / hostname` | TlsOptions 四字段（空 caPem = 跳过验证；空 hostname = 无 SNI） |
| 超时 | 无独立配置，`setReadTimeout` / `setWriteTimeout` 随装饰继承底层 TCP（超时抛 `TlsError.Timeout`） |

### Net.Uri（详见 §10.1）

| 方法 | 说明 |
|---|---|
| `_init_( string text ) throws` | 构造即解析（非法抛 `HttpError.UriFormat`） |
| `get string scheme()` / `get string host()` / `get Int32 port()` | 解析结果（port 未显式给出 -1） |
| `get string path()` / `get string query()` / `get string pathAndQuery()` | 路径 / 查询串 / 请求行路径（空 path 补 `/`） |
| `get Int32 effectivePort()` / `get string hostHeader()` | scheme 默认端口（http / ws=80，https / wss=443）/ Host 请求头（非默认端口拼 host:port） |
| `get bool isHttp()` / `get bool isHttps()` / `get bool isWs()` / `get bool isWss()` | scheme 判别（忽略大小写） |
| `static bool eqFold( string a, string b )` / `static Int32 fold( Int32 c )` | ASCII 忽略大小写比较 / 小写折叠 |

### Net.WebSocketClient（高层门面，详见 §11.1）

| 方法 / 字段 | 说明 |
|---|---|
| `WebSocketClient connect( string url ) throws` | 建连 + 握手 + 启动事件循环协程（返回 this）；握手失败原地抛；重复 connect 拒绝（重连 = 新建实例） |
| `Function onOpen` / `Function onMessage` / `Function onClose` / `Function onError` | 四事件回调（连接前赋值）：握手完成 / 每条消息（用完 `release()`）/ 收尾（info 可 null）/ 事件循环异常 |
| `void sendText( string ) throws` / `void sendBinary( ByteBuffer / UInt8Array ) throws` / `void sendPing() throws` | 发送族（未连接抛 `WsError.Closed`） |
| `void close()` / `void close( Int32 code, string reason )` | 优雅关闭（发 Close + 超时看门狗；不阻塞，收尾触发 `onClose`） |
| `void abort()` | 强制立即关闭（不等回显） |
| `void waitClosed()` | 等事件循环结束（onClose 已送达） |
| `get WsState state()` / `get bool isOpen()` | 连接态（未连接 = Closed）/ 是否 Open |
| `get WsCloseInfo closeInfo()` / `get string protocol()` | 关闭协商信息 / 选定子协议（未连接 null / ""） |
| `protocols / caPem / handshakeTimeoutMs / maxMessageSize / autoPong / closeTimeoutMs` | 配置字段（语义同 `WsOptions` + 优雅关闭超时默认 5000ms） |

### Net.WebSocketStream / WsMessage / WsCloseInfo / WsOptions（详见 §11.2~11.9）

| 方法 / 字段 | 说明 |
|---|---|
| `static WebSocketStream connect( string url ) throws` | 客户端握手（10s 超时；ws=TCP / wss=TLS） |
| `static WebSocketStream connect( string url, WsOptions opt ) throws` | 带配置握手 |
| `static Task connectAsync( string url )` / `static Task connectAsync( string url, WsOptions opt )` | 异步握手（Task 结果为 WebSocketStream） |
| `static WebSocketStream accept( TcpStream raw ) throws` | 服务端握手（raw 已完成 TCP accept） |
| `static WebSocketStream accept( TcpStream raw, WsOptions opt ) throws` | 带配置服务端握手 |
| `WsMessage receive() throws` | 挂起收一条完整消息（自动应答 Ping；连接关闭返回 null） |
| `Stream<WsMessage> messages()` | 消息流形态（`each` 消费；关闭后自然结束） |
| `void sendText( string text ) throws` / `void sendBinary( ByteBuffer ) throws` / `void sendBinary( UInt8Array ) throws` | 发文本 / 二进制消息（自动分片按 maxMessageSize） |
| `void sendPing() throws` / `void sendPong() throws` | 心跳控制帧（负载 0 字节） |
| `void sendClose() throws` / `void sendClose( Int32 code, string reason ) throws` | 主动关闭（发 Close 帧） |
| `override void close() throws` | 发 Close 并关底层（幂等；closeInfo 置 wasClean） |
| `get WsState state()` / `get WsCloseInfo closeInfo()` / `get string protocol()` / `get bool isClient()` | 连接态（Connecting/Open/Closed）/ 关闭协商结果 / 选定子协议 / 角色 |
| `WsOpCode op` / `ByteBuffer payload` / `get Int32 size()` | WsMessage 三属性（op：Text/Binary/Close/Ping/Pong） |
| `Int32 code` / `string reason` / `bool wasClean` | WsCloseInfo 三属性（code：1000 正常关闭；1005 = 无码） |
| `string protocols` / `Int32 maxMessageSize` / `Int32 handshakeTimeoutMs` / `bool autoPong` / `string caPem` | WsOptions 五字段（子协议列表逗号分隔 / 重组上限默认 16 MiB / 握手超时默认 10s / 自动应答 Ping 默认开 / wss CA 空为跳过验证） |

### Net.HttpClient / HttpRequest / HttpResponse / HttpHeaders（详见 §10）

| 方法 | 说明 |
|---|---|
| `static HttpResponse httpGet( string url ) throws` | 便捷 GET |
| `static HttpResponse httpPost( string url, string body, string contentType ) throws` | 便捷 POST（文本 body + Content-Type） |
| `static Task httpGetAsync( string url )` / `static Task httpPostAsync( string url, string body, string contentType )` | 异步便捷 GET / POST（直接返回 Task，见 §10.4） |
| `HttpResponse send( HttpRequest req ) throws` | 发送并读完整响应（每请求一连接，自动 `Connection: close`；body 三态读取见 §10.5） |
| `Task sendAsync( HttpRequest req )` | 异步 `send`（实例配置生效，直接返回 Task，见 §10.4） |
| `Int32 connectTimeoutMs / Int32 readTimeoutMs / string caPem` | HttpClient 字段：连接超时（默认 10s）/ 响应读超时（默认无限，超时抛 `NetError.Timeout`）/ https CA（空 = 跳过验证） |
| `HttpMethod method / string url / HttpHeaders headers / string body` | HttpRequest 字段（method：`Net.HttpMethod` 枚举 Get/Post/Put/Delete/Head） |
| `get bool isOk()` / `get Int64 contentLength()` | 响应 200~299 / Content-Length 头（缺失 / 非法 -1） |
| `void add( name, value )` / `void setHeader( name, value )` | HttpHeaders：追加同名头 / 覆盖同名头（大小写不敏感） |
| `string getHeader( string name )` / `bool contains( string name )` | 取值（不存在 null）/ 存在性（大小写不敏感） |
| `get int count()` / `string nameAt( int )` / `string valueAt( int )` | 头数量 / 按序取名取值 |

---

## 16. 限制与已知偏差（必读）

| 事项 | 说明 |
|---|---|
| **读写超时默认关闭** | recv / send / accept / recvFrom 默认无限等待（挂到 fd 就绪或关闭）；`setReadTimeout` / `setWriteTimeout` / `setAcceptTimeout` 配置窗口后，等待超过窗口抛 `NetError.Timeout` 且连接保持可用（窗口自首次挂起锚定，跨挂起重执行不重置；Phase 2 已落地，设计档 §17 / ADR-6） |
| **UdpStream.write 不支持** | Phase 1 无 `connect(对端)` 语义，固定对端写入走守卫（抛 NotSupported）；定向发送一律 `sendTo` |
| **空 UDP 数据报返回 0** | 与 ByteStream 的 EOF 语义共用 0 值（UDP 空报是合法报文，非错误） |
| **UdpDatagram 属性名是 `payload`** | 设计稿早期记 `data`，落地为 `payload` |
| **UDP 工厂名是 `open` / `openAddress`** | 设计稿早期记 `bind` / `bindAddress` |
| **UdpStream read 截断** | 超出 dst 容量的报文尾部由 C 侧丢弃（无粘连，也不报错） |
| **TLS 已落地（Phase 3 Stage A）** | `TlsStream.sl`（§9）：mbedTLS 3.6.7 源码直编；支持 TLS 1.2 / 1.3（1.3 仅 ephemeral 密钥交换模式）；`caPem` 为空 = 跳过证书链验证（仅自签 / 测试环境使用，生产必须显式 CA）；证书 / 私钥仅支持 PEM 格式；无 ALPN；服务端不开会话票据（客户端可恢复） |
| **HTTP 客户端已落地（Phase 3 Stage B）** | `Uri.sl` / `HttpClient.sl`（§10）：HTTP/1.1 客户端（GET / POST / PUT / DELETE / HEAD，http / https，body 三态：Content-Length / chunked / EOF），纯 SL 实现。**每请求一连接**（自动 `Connection: close`，无连接复用 / keep-alive / pipeline）；无重定向跟随、无 cookie / proxy / gzip 解压；`HttpRequest.method` 为 `Net.HttpMethod` 枚举（Get/Post/Put/Delete/Head）；Uri 不支持 userinfo / IPv6 字面量 / percent-encoding / fragment；域名解析仍为同步 getaddrinfo（R-3，Phase 2 异步化） |
| **WebSocket 已落地（Phase 3 Stage C）** | `Websocket.sl`（§11）：RFC 6455 客户端 + 服务端（握手 / 帧编解码 / 分片重组 / 掩码 / 控制帧 / 关闭协商），ws / wss，纯 SL 协议层 + 两个系统调用（`SystemSha1` / `SystemRandomFill`）；消费三形态——`WebSocketClient` 事件门面（onOpen / onMessage / onClose / onError，§11.1）/ `receive`（单条挂起）/ `messages()`（Stream 消费）；事件循环协程内不自动重连（重连 = 新建 WebSocketClient）；**无 permessage-deflate 压缩扩展**；wss 依赖 `TlsStream`（caPem 空 = 跳过验证）；`Sha1.sl` 为根级类（同 Base64）。HTTP 服务端（`Route.sl` / `IpProtocal.sl`）仍未落地 |
| **网络流不可跨 isolate** | `TcpStream` / `TlsStream` / `UdpStream` / `WebSocketStream` / `Channel` / `Task` 均不可 Sendable（§14） |
| **M:1 协作调度** | 单 VM 内协程为协作式（让出点推进）；网络 IO 挂起不占线程，但 CPU 密集协程需手动让出（`Coroutine.yieldNow`） |
| **root 挂起** | root 协程可挂起（delay / 网络 IO 均安全）；但 root 返回后若仍有挂起协程，仅网络等待类由守卫保活到就绪（NetTest B3） |
| **onConnection 不返回** | 内部无限 accept 循环，须放进独立协程调用 |
