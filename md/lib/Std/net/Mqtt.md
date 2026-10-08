# Net.Mqtt — MQTT 3.1.1 客户端（纯 SL 实现）

- 源码：`source/Front/Lib/Std/Net/Mqtt.sl`（namespace `Net`）
- 依赖：`TcpSocket` / `TcpServer` / `NetStream` / `ByteBuffer`（均在 `Net`，无 C VM 依赖、无新增系统方法）
- 支持范围：MQTT 3.1.1 核心子集——CONNECT / CONNACK / PUBLISH / PUBACK / SUBSCRIBE / SUBACK / UNSUBSCRIBE / UNSUBACK / PINGREQ / PINGRESP / DISCONNECT；QoS 0 与 QoS 1；不支持 QoS 2、TLS、遗嘱（Will）、保留消息标记下发（retain 仅编码透传）
- 测试：`test/ExpendTest/MqttTest.sl`（8 组 63 断言，含 SL 协程 mock echo broker 全流程）

---

## 1. 快速上手

```sl
import Std;
import Core;

class Demo
{
    static void run()
    {
        Net.MqttClient cli = Net.MqttClient.connect( "127.0.0.1", 1883 )
        Int32 granted = cli.subscribe( "a/b", 1 )
        cli.publish( "a/b", "hello" )
        # 订阅者视角：receive() 阻塞等一条 PUBLISH；返回 null = 对端 DISCONNECT / 断开
        Net.MqttMessage m = cli.receive()
        if m != null
        {
            Console.println( m.topic + " = " + m.text() )
            m.release()
        }
        cli.disconnect()
    }
}
```

带连接参数：

```sl
Net.MqttConnectOptions opt = Net.MqttConnectOptions()
opt.clientId = "mydev-001"
opt.username = "user"
opt.password = "pass"
opt.keepAliveSeconds = 30
Net.MqttClient cli = Net.MqttClient.connect( "broker.example.com", 1883, opt )
```

---

## 2. MqttClient — 客户端

| 成员 | 签名 | 说明 |
|------|------|------|
| connect | `static MqttClient connect( string host, Int32 port ) throws` | 默认参数连接 |
| connect | `static MqttClient connect( string host, Int32 port, MqttConnectOptions opt ) throws` | 自定义参数；CONNACK `returnCode != 0` 抛 `MqttError.ConnackRejected` |
| isConnected | `public get bool isConnected` | 连接状态 |
| subscribe | `Int32 subscribe( string topic, Int32 qos ) throws` | 返回 SUBACK 授权的 QoS（0/1） |
| unsubscribe | `void unsubscribe( string topic ) throws` | |
| publish | `void publish( string topic, string text ) throws` | QoS 0 |
| publish | `void publish( string topic, string text, Int32 qos ) throws` | `qos > 1` 抛 `MqttError.Protocol`；QoS 1 内部等 PUBACK |
| receive | `MqttMessage receive() throws` | 阻塞收一条 PUBLISH；`null` = 对端 DISCONNECT / EOF；非 null 时**用完必须 `m.release()`** |
| ping | `void ping() throws` | PINGREQ → PINGRESP 往返 |
| disconnect | `void disconnect()` | 发 DISCONNECT 并关流，尽力而为不抛 |

异常：连接/握手失败、读超时（`NetError.Timeout`）、协议违例均通过 `throws` 抛出，调用方用 `label`/`try` + `catch e` 捕获（`e as Error` 取 `code`）。

---

## 3. MqttConnectOptions — 连接参数

| 字段 | 类型 | 默认值 |
|------|------|--------|
| clientId | string | `"slmqtt"` |
| cleanSession | bool | `true` |
| keepAliveSeconds | Int32 | `60` |
| username | string | `""`（空则不发用户名标志） |
| password | string | `""`（有 username 才生效） |
| connectTimeoutMs | Int32 | `10000` |

---

## 4. MqttMessage — 收到的 PUBLISH

| 成员 | 说明 |
|------|------|
| `public string topic` | 主题 |
| `public ByteBuffer payload` | 原始载荷 |
| `public Int32 qos` | 0 / 1 |
| `public Int32 packetId` | QoS 1 时的报文标识 |
| `public bool retain` | retain 标志 |
| `public get Int32 size` | 载荷字节数 |
| `public string text()` | 载荷按 UTF-8 解码为字符串 |
| `public void release()` | 释放载荷（**每条消息必须调用一次**） |

---

## 5. MqttCodec — 编解码工具（面向自定义 broker / 测试）

所有编码函数返回新 `ByteBuffer`（用完 `release()`）；`decode` 类基于读游标推进。

| 函数 | 说明 |
|------|------|
| `Int32 remainingLengthSize( Int32 value ) throws` | 剩余长度编码占几字节（1~4；超 268435455 抛 Protocol） |
| `ByteBuffer encodeRemainingLength( Int32 value ) throws` | 剩余长度 varint 编码 |
| `Int32 decodeRemainingLength( ByteBuffer buf ) throws` | 解码剩余长度：`>= 0` 成功；`-1` 数据不足（游标回滚）；`-2` 超 4 字节上限（游标回滚） |
| `string readMqttString( ByteBuffer buf ) throws` | 读 U16 长度前缀字符串（MQTT String） |
| `ByteBuffer encodeConnect( MqttConnectOptions opt ) throws` | CONNECT |
| `ByteBuffer encodePublish( string topic, ByteBuffer payload, Int32 qos, Int32 packetId, bool retain ) throws` | PUBLISH；payload 内部按 `slice()` 视图写入，**不消费源 buf**；`qos > 1` 抛 Protocol |
| `ByteBuffer encodeSubscribe( Int32 packetId, string topic, Int32 qos )` | SUBSCRIBE |
| `ByteBuffer encodeUnsubscribe( Int32 packetId, string topic )` | UNSUBSCRIBE |
| `ByteBuffer encodePingReq()` / `encodePingResp()` / `encodeDisconnect()` | 定长 2 字节包 |
| `ByteBuffer encodeAck( Int32 ptype, Int32 packetId ) throws` | PUBACK / UNSUBACK 等 ACK 型 |
| `ByteBuffer encodeConnack( bool sessionPresent, Int32 returnCode ) throws` | CONNACK（服务器侧） |
| `ByteBuffer encodeSuback( Int32 packetId, Int32 grantedQos ) throws` | SUBACK（服务器侧） |
| `_MqttPacket parsePacket( ByteBuffer buf ) throws` | 从 buf 解一个完整包；`null` = 数据不足（游标回滚）；type=0 / 剩余长度超限抛 Protocol 并回滚 |
| `_MqttPacket readPacket( NetStream s, ByteBuffer buf ) throws` | 从流持续读并拼包；`null` = EOF |
| `MqttMessage parsePublish( _MqttPacket pkt ) throws` | 把 PUBLISH 包解为 `MqttMessage` |

`_MqttPacket`：`public Int32 type / Int32 flags / ByteBuffer payload` + `release()`。

---

## 6. 错误码 / 常量

**MqttError extends Error**（`catch e` 后 `e as MqttError` 或查 `code`）：

| 枚举 | code | 含义 |
|------|------|------|
| Protocol | 1 | 协议违例（剩余长度非法 / 包类型错 / qos>1 / CONNACK 载荷不完整） |
| ConnackRejected | 2 | 服务端 CONNACK 拒绝（returnCode != 0） |
| Closed | 3 | 已关闭的连接上收发 / 握手中对端断开 |

**MqttPacketType extends Int32**：Connect=1, Connack=2, Publish=3, PubAck=4, Subscribe=8, Suback=9, Unsubscribe=10, Unsuback=11, PingReq=12, PingResp=13, Disconnect=14。

**MqttQoS extends Int32**：AtMostOnce=0, AtLeastOnce=1, ExactlyOnce=2（本实现仅支持 0/1）。

---

## 7. 测试

- 用例：`test/ExpendTest/MqttTest.sl`，入口 `MqttTest.fun()`，已登记进 `ProjectTest.sp` / `ProjectTest.jsonc`
- 分组：A/B 剩余长度编解码边界；C/D/E 十六进制包向量；F parsePacket/parsePublish 与错误路径；G mock echo broker（SL 协程 `TcpServer`，端口 18901）订阅/发布/回显/ping/断连全流程；H CONNACK 拒绝（端口 18902）
- 运行：

```powershell
cd d:\project\lang\simple_language
dotnet run --project source\CompileStd\SimpleLanguageCompileStd.csproj          # 重编 Std（改了 Mqtt.sl 才需要）
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\ExpendTest\ProjectTest.sp -e ir
dotnet run --project project\CSimpleVMStdTest
```

预期输出 `===== MqttTest start =====` … `MqttTest end : pass=63 fail=0`。
