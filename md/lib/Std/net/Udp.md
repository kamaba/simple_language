# Net.Udp — UDP 报文收发

- 源码：`source/Front/Lib/Std/Net/UdpSocket.sl`（namespace `Net`，含 `Udp` / `UdpSocket` / `UdpDatagram`）+ `NetStream.sl` 中的 `UdpStream`
- 底层：C VM 系统调用 `SystemUdp*`（`csimple_lang/src/vm/system_method_call/net_system_method.c`）；非阻塞 socket + 协程化挂起
- 相关：总览见 [net.md](net.md)；TCP 见 [tcp.md](tcp.md)；流形态与 ByteStream 契约见 [stream.md](stream.md)
- 测试：`test/Other/NetTest/UdpEchoTest.sl`（D 组，端口 19311~19313）

---

## 1. 快速上手

两个端口互发（open 绑定即得报文流）：

```sl
import Std;
import Core;

NetUdpDemo
{
    static fun()
    {
        Net.UdpStream a = Net.Udp.open( 19311 )     # 绑定 0.0.0.0:19311
        Net.UdpStream b = Net.Udp.open( 19312 )

        b.sendTo( Utf8.encode( "ping" ), "127.0.0.1", 19311 )   # 定向发送

        ByteBuffer dst = ByteBuffer( 64 )
        Int32 n = a.read( dst )                     # 挂起收一报写入 dst
        Console.println( Utf8.decode( dst ) )       # ping

        a.close()
        b.close()
    }
}
```

报文流形态（`datagrams()` 引流协程 + Stream 消费，逐报带来源地址）：

```sl
Net.UdpStream ua = Net.Udp.open( 19313 )
Stream<Net.UdpDatagram> dgs = ua.datagrams()
dgs.listen( void( Net.UdpDatagram dg )
{
    Console.println( dg.address + ":" + dg.port.toString() + " -> " + Utf8.decode( dg.payload ) )
} )
```

---

## 2. Udp — 外观类（静态工厂）

| 方法 | 语义 |
|------|------|
| `static UdpStream open( Int32 port ) throws` | 绑定 `0.0.0.0:port` 得报文流；端口占用等失败抛 `NetError.BindFailed` |
| `static UdpStream openAddress( string host, Int32 port ) throws` | 绑定指定地址 |

> 工厂名落地为 `open` / `openAddress`（设计稿早期记 `bind` / `bindAddress`，以源码为准）。

---

## 3. UdpStream — 报文流（extends NetStream）

| 方法 | 语义 |
|------|------|
| `override Int32 read( ByteBuffer dst ) throws` | **一次 read 收一报**（挂起直到有报）；写入 dst 可写区；**0 = 空数据报（合法）或 EOF** |
| `override void write( ByteBuffer src ) throws` | **不支持**（Phase 1 无 connect(对端) 语义，抛 NotSupported）；定向发送一律 `sendTo` |
| `void sendTo( ByteBuffer src, string address, Int32 port ) throws` | 定向发送（全量；UDP sendTo 不挂起，不受写超时影响） |
| `get string lastFromAddress()` / `get Int32 lastFromPort()` | 最近一次收报的来源（经 C 侧槽位查询） |
| `Stream<UdpDatagram> datagrams()` | 报文流：内部 `spawnClosure0` 引流协程循环收报灌进 `StreamController` |
| `void setReadTimeout( Int32 timeoutMs )` | 收报挂起等待超时窗口；`<= 0` 清除（默认无限等待）；超时抛 `NetError.Timeout`，socket 保持可用 |
| `void close()` | 关闭（幂等；取消在途等待，引流协程以错误返回自然退出，`ctrl.close()` 收尾） |

`datagrams()` 实现要点：每轮新 `ByteBuffer(65536)` 直调 `SystemUdpRecvFrom` 收报 → `ctrl.add( UdpDatagram( buf, lastFromAddress, lastFromPort ) )`。

```sl
Net.UdpStream u = Net.Udp.open( 19311 )
u.setReadTimeout( 2000 )                       # 收报最多等 2s（超时抛 NetError.Timeout）
u.sendTo( Utf8.encode( "hi" ), "127.0.0.1", 19312 )
Int32 n = u.read( dst )                        # 收一报
string from = u.lastFromAddress()              # 本报来源
Int32  fromPort = u.lastFromPort()
```

---

## 4. UdpSocket — 裸操作

`UdpStream` 内部持有 `UdpSocket`；需要裸 socket 语义时直接用：

| 方法 | 语义 |
|------|------|
| `Int32 recvFrom( ByteBuffer dst ) throws` | 挂起收一报写入 dst 可写区，返回读入字节数；**0 = 空数据报（合法）** |
| `void sendTo( ByteBuffer src, string address, Int32 port ) throws` | 定向发送（全量） |
| `void setReadTimeout( Int32 timeoutMs )` | recvFrom 挂起等待超时窗口；`<= 0` 清除；超时抛 `NetError.Timeout` |
| `get string lastFromAddress()` / `get Int32 lastFromPort()` | 最近一次收报来源 |
| `void close()` | 关闭（幂等，取消在途等待） |

---

## 5. UdpDatagram — 数据报值对象

`datagrams()` 流的元素（payload + 来源地址三元组）：

| 属性 | 说明 |
|------|------|
| `get ByteBuffer payload()` | 报文负载（readerIndex 从 0 起，直接按 ByteBuffer 读取） |
| `get string address()` | 来源地址 |
| `get Int32 port()` | 来源端口 |

---

## 6. 语义要点（与 TCP 的差异）

| 要点 | 说明 |
|------|------|
| 无连接、无固定对端 | UDP 无 accept / connect(对端)；`write` 守卫拒绝，发送一律 `sendTo(src, addr, port)` 指定目标 |
| 0 是合法返回值 | 空数据报 read / recvFrom 返回 0（合法报文，非错误）；与 ByteStream 的 EOF 语义共用 0 值——需区分时用 `lastFromAddress` 或上下文判断 |
| 报文截断不报错 | 超出 dst 容量的报文尾部由 C 侧丢弃（无粘连，也不报错）；收大报请给足缓冲（惯例 65536） |
| 无半关闭 | UDP 无连接语义，`closeRead` / `closeWrite` 不适用；统一 `close()` |
| 属性名 | `UdpDatagram` 用 `payload`（设计稿早期记 `data`，以源码为准） |
| sendTo 不挂起 | UDP 发送不等待对端；写超时（`setWriteTimeout`）对 UDP 无效 |
| 不可跨 isolate | `UdpStream` / `UdpSocket` / `UdpDatagram` 中的流对象不可 Sendable；解析业务可丢进 isolate（payload 先取出为可发送数据） |

错误码复用 [tcp.md](tcp.md) §6 的 `NetError`（UDP 相关：`BindFailed` / `Timeout` / `Closed` / `IoError`）。

---

## 7. 测试

- 用例：`test/Other/NetTest/UdpEchoTest.sl`（D 组：echo 回环 + 来源地址校验 + 空报边界；端口 19311~19313）
- 运行：

```powershell
cd d:\project\lang\simple_language
dotnet run --project project\CSimpleVMStdTest -- test\Other\NetTest\ProjectTest.sp
```

（整组入口在 `ProjectTest.sp` 的 `_main_`；D 组端口 19311~19313。）
