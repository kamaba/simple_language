# 网络编程（Net）：Tcp / Udp / NetStream

> **本文档以「当前实现」为准**，对应 `source/Front/Lib/Std/Net/`（`TcpSocket.sl` / `UdpSocket.sl` / `NetStream.sl`）
> 与 `test/NetTest/`（A~H 组验收用例，全部通过）。
> 设计规格见 `csimple_lang/md/design/NET_DESIGN.md`（唯一真源）；差异与偏差见本文 §13。
> 相关文档：`md/syntax/coroutine.md`（协程——网络挂起的载体）、`md/syntax/core/Stream.md`（Stream 元素流）、
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
9. [错误处理（NetError）](#9-错误处理neterror)
10. [组合范式](#10-组合范式)
11. [与协程 / isolate 的关系](#11-与协程--isolate-的关系)
12. [API 速查总表](#12-api-速查总表)
13. [限制与已知偏差（必读）](#13-限制与已知偏差必读)

---

## 1. 模型概述

网络层提供 **TCP / UDP 裸 socket**（不含 HTTP / TLS / DNS 解析服务），全部 IO 走**协程化挂起**——
未就绪时挂起当前协程、让出调度，fd 就绪后由 IO 线程唤醒续跑；**不阻塞 VM 线程**。

| 特性 | 说明 |
|---|---|
| **同步写法，异步执行** | `read` / `write` / `accept` / `recvFrom` 写法与阻塞式 API 一致，但未就绪时挂起当前协程（`CORO_BLOCK_IO`），其它协程照常推进 |
| **挂起协议 Option A** | 系统调用（`SystemTcpRecv` 等）返回「未就绪」哨兵 → C VM 挂起当前协程 → fd 就绪（IO 线程 poll）→ 协程恢复后**指令重执行**自行完成读写 |
| **root 可挂起** | `fun()` 入口被包装为 root 协程，`read` / `accept` / `Coroutine.delay` 等可直接写在最外层，无需先 spawn |
| **ByteStream 子类** | `NetStream` extends `ByteStream`：`read(ByteBuffer)` / `write(ByteBuffer)` / `closeRead` / `closeWrite` / `close` 与文件流同构，可直连 Stream 体系（`LengthPrefix` 等） |
| **失败 = 异常** | SL 层方法 `throws` 抛 `NetError`（extends Error）；C 层用哨兵值 + `SystemNetLastError`，SL 负责归类抛出 |
| **关闭即取消** | `close()` 释放句柄的同时，C 侧取消挂在该 socket 上的全部在途等待（挂起协程以错误返回唤醒） |

### 1.1 一次 `client.read(buf)` 挂起的完整时序

```
协程 A: client.read(buf)
  └─ SystemTcpRecv(sid) ──► WOULD_BLOCK（未就绪哨兵）
        └─ C VM 挂起协程 A（CORO_BLOCK_IO），登记 fd 等待表
调度器: 运行其它就绪协程 / 空闲 park
IO 线程: poll 发现 fd 可读 ──► 唤醒协程 A 入就绪队列
调度器: 恢复协程 A ──► 指令重执行 SystemTcpRecv ──► 本次读到数据，返回字节数
```

### 1.2 源码位置

| 层 | 文件 |
|---|---|
| SL API | `source/Front/Lib/Std/Net/`（`TcpSocket.sl`、`UdpSocket.sl`、`NetStream.sl`） |
| 系统调用注册 | `source/Front/Lib/Std/Std.jsonc` → `files[]` + `systemCalls[]`（SystemTcp* / SystemUdp* 共 20 项） |
| C VM 原语层 | `csimple_lang/src/lib/os/sys_net.c`（非阻塞 socket / poll 包装） |
| C VM 等待层 | `csimple_lang/src/vm/runtime/net/vm_net_io.c`（fd 等待表 + IO 线程） |
| C VM 系统调用 | `csimple_lang/src/vm/system_method_call/net_system_method.c` |
| 验收用例 | `test/NetTest/`（A 基础收发 / B 挂起唤醒 / C 分帧 / D UDP / E 并发 / F 关闭 / G Stream 组合 / H isolate） |

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
| `Net.NetError` | 网络错误枚举（extends Error，code 1~9，见 §9） |
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

## 9. 错误处理（NetError）

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

## 10. 组合范式

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

## 11. 与协程 / isolate 的关系

- **与协程**：网络挂起与 `Channel` / `delay` 走同一个 `CORO_BLOCK_IO` / timer 通道与唤醒路径，天然正交组合。root 协程（`fun()` 主体）可直接在网络调用上挂起——`Coroutine.delay` 期间网络协程照常被唤醒推进（NetTest B1 验证）。
- **与 isolate**：`TcpStream` / `UdpStream` / `Channel` / `Task` **均不可 Sendable**——网络流不能跨 isolate 传递。范式：网络 IO 协程留在主 VM，把「不可信 / 易崩」的协议解析（string / TransferableData 等可发送数据）丢进 isolate，解析崩了也不影响主 VM 的其它连接（NetTest H 组验证）。

---

## 12. API 速查总表

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

---

## 13. 限制与已知偏差（必读）

| 事项 | 说明 |
|---|---|
| **读写超时默认关闭** | recv / send / accept / recvFrom 默认无限等待（挂到 fd 就绪或关闭）；`setReadTimeout` / `setWriteTimeout` / `setAcceptTimeout` 配置窗口后，等待超过窗口抛 `NetError.Timeout` 且连接保持可用（窗口自首次挂起锚定，跨挂起重执行不重置；Phase 2 已落地，设计档 §17 / ADR-6） |
| **UdpStream.write 不支持** | Phase 1 无 `connect(对端)` 语义，固定对端写入走守卫（抛 NotSupported）；定向发送一律 `sendTo` |
| **空 UDP 数据报返回 0** | 与 ByteStream 的 EOF 语义共用 0 值（UDP 空报是合法报文，非错误） |
| **UdpDatagram 属性名是 `payload`** | 设计稿早期记 `data`，落地为 `payload` |
| **UDP 工厂名是 `open` / `openAddress`** | 设计稿早期记 `bind` / `bindAddress` |
| **UdpStream read 截断** | 超出 dst 容量的报文尾部由 C 侧丢弃（无粘连，也不报错） |
| **HTTP / TLS / WebSocket / Uri 不在 Phase 1** | `HttpClient.sl` / `Websocket.sl` / `Uri.sl` / `Route.sl` / `IpProtocal.sl` 维持原状（Phase 3） |
| **网络流不可跨 isolate** | `TcpStream` / `UdpStream` / `Channel` / `Task` 均不可 Sendable（§11） |
| **M:1 协作调度** | 单 VM 内协程为协作式（让出点推进）；网络 IO 挂起不占线程，但 CPU 密集协程需手动让出（`Coroutine.yieldNow`） |
| **root 挂起** | root 协程可挂起（delay / 网络 IO 均安全）；但 root 返回后若仍有挂起协程，仅网络等待类由守卫保活到就绪（NetTest B3） |
| **onConnection 不返回** | 内部无限 accept 循环，须放进独立协程调用 |
