# Net.Tcp — TCP 客户端 / 服务端

- 源码：`source/Front/Lib/Std/Net/TcpSocket.sl`（namespace `Net`，含 `Tcp` / `TcpServer` / `TcpSocket` / `NetError` / `SocketShutdown`）
- 底层：C VM 系统调用 `SystemTcp*`（`csimple_lang/src/vm/system_method_call/net_system_method.c`）；非阻塞 socket + 协程化挂起（Windows IOCP / Linux epoll）
- 相关：总览见 [net.md](net.md)；流形态 [stream.md](stream.md)；UDP 见 [udp.md](udp.md)；TLS 见 [stream.md](stream.md) §6；协程机制见 [../../coroutine.md](../../coroutine.md)
- 测试：`test/Other/NetTest/`（TcpBasicTest / NetFrameTest / NetConcurrentTest / NetCloseTest / NetIsolateTest / NetTimeoutTest / NetSuspendTest）

---

## 1. 快速上手

echo 服务器 + 客户端（全部 IO 挂起当前协程，不阻塞 VM 线程）：

```sl
import Std;
import Core;

NetDemo
{
    static fun()
    {
        # 服务端：listen + accept（无连接时挂起）
        Net.TcpServer srv = Net.Tcp.listen( 9000 )

        # 每连接一条协程
        function echo = function()
        {
            Net.TcpStream c = srv.accept()
            ByteBuffer buf = ByteBuffer( 4096 )
            while true
            {
                Int32 n = c.read( buf )        # 挂起直到对端数据；0 = EOF
                if n == 0 { break }
                c.write( buf )                 # 全量写出（写背压由 C 层透明挂起）
                buf.clear()                    # 双索引复位，循环复用
            }
            c.close()
        }
        spawn echo()

        # 客户端：连接 + 发送 + 读回（默认 10s 连接超时）
        Net.TcpStream conn = Net.Tcp.connect( "127.0.0.1", 9000 )
        conn.write( Utf8.encode( "hello" ) )
        ByteBuffer rx = ByteBuffer( 64 )
        conn.read( rx )
        Console.println( "echo = " + Utf8.decode( rx ) )
        conn.close()
        srv.close()
    }
}
```

回调形态一行接线（onConnection 内部即「accept 循环 + 每连接 spawn」）：

```sl
srv.onConnection( void( Net.TcpStream c )
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
} )
```

---

## 2. Tcp — 外观类（静态工厂）

SL 命名空间不能放裸函数，连接 / 监听入口统一挂在 `Net.Tcp` 上：

| 方法 | 语义 |
|------|------|
| `static TcpStream connect( string host, Int32 port ) throws` | 连接（内部 `connectTimeout(host, port, 10000)`，默认 10s） |
| `static TcpStream connectTimeout( string host, Int32 port, Int64 timeoutMs ) throws` | 带超时连接；未就绪时挂起；错误细分见下 |
| `static TcpServer listen( Int32 port ) throws` | 监听 `0.0.0.0:port`（backlog 128），失败抛 `NetError.ListenFailed` |
| `static TcpServer listenAddress( string host, Int32 port ) throws` | 监听指定地址 |
| `static TcpStream streamOf( TcpSocket socket ) throws` | 裸 socket 升级为流 |

`connectTimeout` 抛错细分：`HostNotFound`（域名解析失败）/ `Timeout`（超时窗口内未就绪）/ `ConnectFailed`（连接被拒 / 中途断开）/ `SocketCreate`（socket 创建失败）。

> 连接内部拆两步：`BeginConnect` 发起非阻塞 connect → `ConnectWait` 挂起等待可写 / 超时。两步都在 `connectTimeout` 内完成，SL 侧无感。

---

## 3. TcpServer — 服务端

| 方法 | 语义 |
|------|------|
| `TcpStream accept() throws` | 挂起直到新连接；接入连接默认 `setNoDelay(true)` |
| `TcpSocket acceptSocket() throws` | 同上但返回裸 socket（自控选项时用）；失败抛 `NetError.AcceptFailed` |
| `void setAcceptTimeout( Int32 timeoutMs )` | accept 挂起等待超时窗口；`<= 0` 清除（默认无限等待）；超时抛 `NetError.Timeout` |
| `void onConnection( Function fn )` | 回调形态：内部 accept 循环，每个连接 `Coroutine.spawnClosure1( fn, client )`。**该方法自身不返回**（无限循环），一般放进独立协程 |
| `get Int32 port()` | 实际监听端口（句柄失效后 0） |
| `void close()` | 关闭监听句柄（幂等） |

`onConnection` 的回调签名 = `void( Net.TcpStream client )`（单参闭包，每连接一条协程）。想在接入时自设 socket 选项，用 `acceptSocket()` + `Tcp.streamOf()`：

```sl
Net.TcpSocket sock = srv.acceptSocket()
sock.setNoDelay( false )                       # 例：显式关 NoDelay
Net.TcpStream s = Net.Tcp.streamOf( sock )
```

---

## 4. TcpSocket — 裸操作

一般经 `TcpServer.acceptSocket()` 获取（`Tcp.connect` 直接返回流，无需裸操作）：

```sl
Net.TcpSocket sock = srv.acceptSocket()

# 地址信息（IPv4 点分串；句柄失效返回 ""/0）
string ra = sock.remoteAddress()
Int32  rp = sock.remotePort()
string la = sock.localAddress()
Int32  lp = sock.localPort()

# 收发（ByteBuffer 零拷贝桥接）
Int32 n = sock.recv( dst )        # 写入 dst 可写区；0 = EOF
sock.send( src )                  # 全量写出：推进 src.readerIndex 直到 readableBytes == 0

# 选项与关闭
sock.setNoDelay( true )
sock.shutdown( Net.SocketShutdown.Write )   # 半关闭：停发，对端 read 将见 EOF
sock.close()                                # 关闭并取消该句柄上的全部在途等待（幂等）
```

| 方法 | 语义 |
|------|------|
| `get bool isOpen()` | 句柄有效 |
| `get string remoteAddress()` / `get Int32 remotePort()` | 对端地址 / 端口 |
| `get string localAddress()` / `get Int32 localPort()` | 本端地址 / 端口 |
| `Int32 recv( ByteBuffer dst ) throws` | 非阻塞读，未就绪挂起；**返回 0 = EOF（对端关闭）**；负值在 SL 层转 `IoError`，句柄失效抛 `Closed` |
| `void send( ByteBuffer src ) throws` | 全量发送（写背压由 C 层挂起透明处理） |
| `void setNoDelay( bool enabled )` | TCP_NODELAY 直通 |
| `void setReadTimeout( Int32 timeoutMs )` | recv 挂起等待超时窗口；`<= 0` 清除（默认无限等待）；超时抛 `NetError.Timeout`，**连接保持可用** |
| `void setWriteTimeout( Int32 timeoutMs )` | send 挂起等待超时窗口（内核缓冲满才挂起，罕见）；语义同上 |
| `void shutdown( SocketShutdown how )` | 半关闭：`Read` 停收 / `Write` 停发 / `Both` 双向 |
| `void close()` | 关闭并取消在途等待（幂等） |

> 一般业务直接用 `TcpStream`（[stream.md](stream.md)），`TcpSocket` 用于「接入时自设选项」「半关闭精细控制」等场景。

---

## 5. SocketShutdown — 半关闭方向

**SocketShutdown extends Int32**：

| 枚举 | 值 | 含义 |
|------|-----|------|
| Read | 0 | 停收（本端 read 侧关闭） |
| Write | 1 | 停发（对端将读到 EOF） |
| Both | 2 | 双向 |

典型用法（请求-响应后主动告知写完）：

```sl
conn.write( Utf8.encode( "request" ) )
conn.closeWrite()          # TcpStream 的半关闭：先 flush 再 shutdown(Write)
```

---

## 6. NetError — 错误码

**NetError extends Error**（`catch e` 后 `e as Net.NetError` 查 `code`）：

| 枚举 | code | 触发场景 |
|------|------|------|
| SocketCreate | 1 | socket 创建失败 |
| ConnectFailed | 2 | 连接失败（握手被拒 / 中途断开 / 立即拒绝） |
| HostNotFound | 3 | 主机名解析失败 |
| Timeout | 4 | 连接 / recv / send / accept 挂起等待超时（需先配置超时窗口） |
| BindFailed | 5 | UDP 绑定失败（端口占用等） |
| ListenFailed | 6 | 监听失败 |
| AcceptFailed | 7 | 接受连接失败 |
| Closed | 8 | 句柄已关闭（收发前置检查） |
| IoError | 9 | 底层读写失败（含 close 后再读） |

预期异常的标准写法（SL 异常为非受检，正常路径裸写）：

```sl
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

---

## 7. 语义要点

| 要点 | 说明 |
|------|------|
| 同步写法，异步执行 | `connect` / `accept` / `recv` / `send` 写法与阻塞式 API 一致；未就绪时挂起当前协程（Option A），fd 就绪后指令重执行自行完成 |
| root 可挂起 | `fun()` 入口被包装为 root 协程，网络调用可直接写在最外层，无需先 spawn |
| 超时不毁连接 | 超时窗口自首次挂起锚定（跨挂起重执行不重置）；超时抛 `NetError.Timeout` 后连接 / 监听**保持可用**，可继续收发 |
| close 即取消 | `close()` 释放句柄的同时，C 侧取消挂在该 socket 上的全部在途等待（挂起协程以错误返回唤醒） |
| recv 0 = EOF | 返回值无负值语义：0 = 对端关闭；负值已在 SL 层转 `IoError` 异常 |
| 并发范式 | 每连接一条协程互不阻塞（`onConnection` / `spawnClosure1`）；IO 协程与业务协程可用 `Channel` 解耦（满时挂起 = 天然背压） |
| 不可跨 isolate | `TcpStream` / `TcpServer` / `TcpSocket` 均不可 Sendable；范式：网络 IO 留在主 VM，把可发送数据（string / TransferableData）丢进 isolate 解析 |

---

## 8. 测试

- 用例：`test/Other/NetTest/`（工程入口 `ProjectTest.sp` 的 `_main_` 串跑 11 组，TCP 相关：A TcpBasicTest 基础收发 / B NetSuspendTest 挂起唤醒 / C NetFrameTest 分帧 / E NetConcurrentTest 并发 / F NetCloseTest 关闭语义 / H NetIsolateTest 隔离融合 / T NetTimeoutTest 超时锚定）
- 端口约定：19301~19399 段独立分配，避免组间干扰与 TIME_WAIT 残留
- 运行：

```powershell
cd d:\project\lang\simple_language
dotnet run --project project\CSimpleVMStdTest -- test\Other\NetTest\ProjectTest.sp
```

（宿主自动完成 Front 编译 + C VM 运行；只跑单个 .sl 组可参考 [test-guide](../../../md/project/test-guide.md)。）
