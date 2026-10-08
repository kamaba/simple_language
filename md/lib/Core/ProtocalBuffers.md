# ProtocalBuffers / ProtoCodec（Protocol Buffers 编解码）

`ProtocalBuffers` 是 Protocol Buffers（protobuf）**线格式（wire format）**编解码器，位于 `Core` 标准库的 `Text` 命名空间下（`Core/Text/ProtocalBuffers.sl`），proto3 兼容子集，底座是 `ByteBuffer`（L0 字节层）；`ProtoCodec<T>`（`Core/IO/ProtoCodec.sl`，见 §7）在其上提供消息对象层编解码 + LengthPrefix 分帧流式三件套。

定位：**只做二进制层的编解码**——`.proto` schema 与 message 类由业务侧自己定义，本项目不引入代码生成。复杂计算（varint / zigzag / IEEE754 位打包 / UTF-8）全部走 C 层共用原语：

| 原语 | 承载 |
|------|------|
| varint（无符号 LEB128） | `ByteBuffer.writeVarUint` / `readVarUint` |
| zigzag varint | `ByteBuffer.writeVarInt` / `readVarInt` |
| 定长小端整数 | `ByteBuffer.writeI32Le` / `writeI64Le` / `readI32Le` / `readI64Le` |
| 浮点位打包 | `ByteBuffer.writeF32Le` / `writeF64Le` / `readF32Le` / `readF64Le` |
| UInt64→Int64 位重释 | `SystemConvertInt64FromUInt64`（`readVarUint` 返回 UInt64，原始 varint 值需按位重释；`SystemConvertInt64` 会截断到低 32 位，不可用） |

设计契约：`md/design/STREAM_DESIGN.md` §8。

---

## 1. 线格式速查

### 1.1 wire type

| `EPbWireType` | 值 | 含义 | 用于 |
|------|---|------|------|
| `Varint` | 0 | 变长整数 | int32/int64/uint32/uint64/sint32/sint64/bool/enum |
| `Fixed64` | 1 | 定长 8 字节 | fixed64/sfixed64/double |
| `LengthDelimited` | 2 | 长度前缀 | string/bytes/嵌套 message/packed repeated |
| `StartGroup` | 3 | group 起（已废弃） | 仅读取端兼容跳过 |
| `EndGroup` | 4 | group 止（已废弃） | 同上 |
| `Fixed32` | 5 | 定长 4 字节 | fixed32/sfixed32/float |

### 1.2 编码规则（与 protobuf 官方语义一致）

- tag = `(fieldNumber << 3) | wireType`，varint 编码；字段号有效范围 1..2^29
- **int32 / int64**：负数按 64 位符号扩展 varint 写出（**10 字节**）
- **uint32 / uint64**：位模式编码（`writeUInt32` 形参是 Int32，取低 32 位按无符号位模式 varint）
- **sint32 / sint64**：先 zigzag（`(n << 1) ^ (n >> 31/63)`）再 varint；sint32 借道 64 位 zigzag，数学等价
- **定长**：小端
- **未知字段**：用 `skipField` 跳过（前向 / 后向兼容）

## 2. 类结构

| 类 | 说明 |
|----|------|
| `ProtocalBuffers` | 门面：`newWriter()` / `newReader(bytes)` |
| `ProtoCodec<T>` | 消息对象 ↔ wire format（Codec 层，见 §7） |
| `EPbWireType` | wire type 枚举 |
| `PbTag` | 字段 tag：`fieldNumber` + `wireType` |
| `PbCoding` | 线格式原语：zigzag 编解码、`mask32()` |
| `PbWriter` | 写入器 |
| `PbReader` | 读取器 |

### 2.1 PbTag / PbCoding

| 成员 | 签名 | 说明 |
|------|------|------|
| `PbTag.make` | `(Int32 field, Int32 wire) -> PbTag` | wire 自动 `& 7` |
| `PbTag.encode` | `(Int32 field, Int32 wire) -> Int32` | tag 原始整数 |
| `PbTag.decode` | `(Int32 raw) -> PbTag` | 反解 |
| `PbCoding.zigzagEncode32 / zigzagDecode32` | `(Int32)/(Int64) -> ...` | 32 位 zigzag（借道 64 位原语） |
| `PbCoding.zigzagEncode64 / zigzagDecode64` | `(Int64) -> Int64` | 64 位 zigzag |
| `PbCoding.mask32` | `() -> Int64` | 低 32 位掩码 |

## 3. 写入（PbWriter）

| 方法 | wire | 说明 |
|------|------|------|
| `writeInt32( field, Int32 )` | 0 | 负数 10 字节 |
| `writeInt64( field, Int64 )` | 0 | |
| `writeUInt32( field, Int32 )` | 0 | 取低 32 位位模式 |
| `writeUInt64( field, Int64 )` | 0 | |
| `writeSInt32( field, Int32 )` | 0 | zigzag |
| `writeSInt64( field, Int64 )` | 0 | zigzag |
| `writeBool( field, bool )` | 0 | |
| `writeEnum( field, Int32 )` | 0 | 同 writeInt32 |
| `writeFixed32 / writeSFixed32( field, Int32 )` | 5 | 4 字节小端 |
| `writeFixed64 / writeSFixed64( field, Int64 )` | 1 | 8 字节小端 |
| `writeFloat( field, Float32 )` | 5 | IEEE754 位打包在 C 层 |
| `writeDouble( field, Float64 )` | 1 | 同上 |
| `writeString( field, string )` | 2 | UTF-8 + varint 长度前缀；**null 按长度 0 编码（字段在场）** |
| `writeBytes( field, Array<UInt8> )` | 2 | varint 长度前缀；**null 同上** |
| `writeMessage( field, PbWriter sub )` | 2 | 嵌套子消息；**null 为 no-op（字段不写）** |
| `writeTag( field, wire )` | — | 裸 tag（高级用法） |
| `writeVarint( Int64 )` | — | 裸原始 varint（高级用法） |
| `writeRawByte( Int32 )` | — | 裸字节（高级用法） |
| `length` get | — | 已写字节数 |
| `toBytes()` | — | 导出 `Array<UInt8>` |

字段写出顺序即调用顺序，不做字段号排序。

## 4. 读取（PbReader）

| 方法 | 说明 |
|------|------|
| `atEnd` get | 可读区耗尽 |
| `position` get | 当前 readerIndex |
| `readTag()` | 读 tag，返回 `PbTag` |
| `readInt32()` | 取 varint 低 32 位按有符号解释 |
| `readInt64()` | 原始 varint |
| `readUInt32()` | 取低 32 位位模式（返回 Int32，如写 -1 读回 -1，位模式承载） |
| `readUInt64()` | 原始 varint |
| `readSInt32() / readSInt64()` | zigzag 解码 |
| `readBool()` | `!= 0` |
| `readEnum()` | 同 readInt32 |
| `readFixed32 / readSFixed32()` | 4 字节小端 |
| `readFixed64 / readSFixed64()` | 8 字节小端 |
| `readFloat() / readDouble()` | IEEE754 位重释在 C 层 |
| `readBytes()` | 长度前缀 + `Array<UInt8>` |
| `readString()` | 长度前缀 + UTF-8 解码 |
| `readMessage()` | 剥出子消息字节后建**独立**读取器（深拷贝方案） |
| `readMessageBytes()` | 只剥出子消息原始字节（交给上层 / 其他解码器） |
| `skipField( wireType )` | 跳过未知字段；group（wire 3）递归跳到 EndGroup(4) |

读取器由 `ProtocalBuffers.newReader( bytes )` 构造，**深拷贝语义**（P0 约定），独立持有数据；`newReader( null )` 得到空缓冲（首个 `readTag()` 即抛 `BufferError.Underflow`）。

## 5. 异常语义

读取端统一抛 `BufferError.Underflow`：

- varint 截断（C 层返回 0 且不推进 readerIndex，以 readerIndex 是否前移判失败）
- 空输入读 tag
- 长度前缀为负（损坏数据）
- 长度前缀超出可读区（谎报长度）

## 6. 用法

### 6.1 编码 + 解码往返

```sl
# 写
PbWriter w = ProtocalBuffers.newWriter()
w.writeInt32( 1, -42 )
w.writeString( 2, "hello" )
w.writeDouble( 3, -0.5 )
Array<UInt8> blob = w.toBytes()

# 读
PbReader r = ProtocalBuffers.newReader( blob )
while !r.atEnd
{
    PbTag t = r.readTag()
    if t.fieldNumber == 1      { global.println( r.readInt32().toString() ) }
    elif t.fieldNumber == 2    { global.println( r.readString() ) }
    elif t.fieldNumber == 3    { global.println( r.readDouble().toString() ) }
    else                      { r.skipField( t.wireType ) }
}
```

### 6.2 嵌套子消息

```sl
# 内层
PbWriter inner = ProtocalBuffers.newWriter()
inner.writeInt32( 1, 7 )
inner.writeString( 2, "hi" )

# 外层包一条嵌套 message（field 2）
PbWriter outer = ProtocalBuffers.newWriter()
outer.writeInt32( 1, 150 )
outer.writeMessage( 2, inner )
Array<UInt8> wire = outer.toBytes()

# 剥壳
PbReader r = ProtocalBuffers.newReader( wire )
r.readTag()
r.readInt32()                    # 150
PbTag t2 = r.readTag()          # field 2, wire 2
PbReader sub = r.readMessage()  # 独立读取器，读子消息体
```

### 6.3 官方示例向量（自校验）

```sl
# int32 field1 = 150  →  hex "089601"
PbWriter w1 = ProtocalBuffers.newWriter()
w1.writeInt32( 1, 150 )
var h1 = ByteBuffer.fromBytes( w1.toBytes() )
global.println( h1.toHex() )     # 089601
h1.release()

# string field2 = "testing"  →  hex "120774657374696e67"
PbWriter w2 = ProtocalBuffers.newWriter()
w2.writeString( 2, "testing" )
var h2 = ByteBuffer.fromBytes( w2.toBytes() )
global.println( h2.toHex() )    # 120774657374696e67
h2.release()
```

## 7. ProtoCodec&lt;T&gt; — 消息对象 ↔ wire format（Codec 层）

`Core/IO/ProtoCodec.sl`，`extends Codec<T, ByteBuffer>`（经 `_ProtoCodecBase<S,T>` 参数化基类）。把 PbWriter/PbReader 手写的 encode/decode 闭包注入为可组合的 `Codec`，并接入 LengthPrefix 分帧体系（L2 层）。

### 7.1 创建（闭包注入）

```sl
# encode：T → ByteBuffer（内部用 PbWriter）
function enc = function( object v )
{
    object b = try? MyMsg.pbEncode( v )     # 返回 ByteBuffer
    ret b
}
# decode：ByteBuffer → T（内部用 PbReader）
function dec = function( object v )
{
    ByteBuffer b = v as ByteBuffer
    object m = try? MyMsg.pbDecode( b )
    ret m
}
ProtoCodec<CodecFrameMsg> pc = ProtoCodec.of<CodecFrameMsg>( enc, dec )
```

| 成员 | 签名 | 说明 |
|------|------|------|
| of | `static ProtoCodec<T> of<T>( Function encoderFn, Function decoderFn )` | 闭包注入工厂 |
| encoder | `get Converter<T, ByteBuffer> encoder()` | 编码方向 Converter |
| decoder | `get Converter<ByteBuffer, T> decoder()` | 解码方向 Converter |

### 7.2 分帧流式三件套（机制在 LengthPrefix）

| 成员 | 签名 | 说明 |
|------|------|------|
| decodeStream | `Stream<T> decodeStream( ByteStream src )` | 字节流 → 分帧多消息流（**varint 长度前缀 + 半包续接**，RPC/批量存储标准做法） |
| decodeStream | `Stream<T> decodeStream( ByteStream src, Int32 maxFrame )` | 同上，帧上限字节（超出拒绝） |
| encodeSink | `StreamSink<T> encodeSink( ByteStream dst )` | 写入端：`add(msg)` → encode → 帧直写 |
| encodeSink | `StreamSink<T> encodeSink( ByteStream dst, Int32 maxFrame )` | 同上，带帧上限 |
| bindStream | `Stream<T> bindStream( Stream<ByteBuffer> chunks )` | chunked 解码：`Stream<ByteBuffer>` → `Stream<T>`（跨 chunk 重组半包；截断流报错） |
| bindStream | `Stream<T> bindStream( Stream<ByteBuffer> chunks, Int32 maxFrame )` | 同上 |

语义要点：

- **黄金向量**（CodecFrameTest C 组断言基准）：msg(1,"a") 帧 = `050801120161`（payload `08 01 12 01 61`）；msg(42,"rt") payload = `082a12027274`。
- **已知偏差**（设计稿 → 实现）：`of(ProtoSchema)` → `of(Function, Function)`（SL 无运行时反射）；`bind()` → `bindStream()`（`bind` 是 SL 保留 token）；`extends Codec<T, ByteBuffer>` → 经 `_ProtoCodecBase<S,T>` 中间基类（Front 泛型物化限制，语义不变）。
- **网络组合范式**：`TcpStream` + `ProtoCodec.decodeStream` = 长度前缀分帧 RPC 流（半包自动续接），见 `md/lib/Std/net/` 各分篇。

### 7.3 使用示例（提炼自 `test/BaseTest/CodecFrameTest.sl`）

编写约定（CodecFrameTest 先例）：**闭包不能声明 throws**，编解码逻辑落 `cf` 前缀静态辅助方法，闭包经 `try?` 中转调用（`try?` 吞异常为 `null`，正常路径不受影响）；**匿名闭包字面量不能直接作调用实参**，先赋 `function` 变量再传参。

```sl
# ---------- 1. 消息类 + 编解码辅助（pbEncode / pbDecode） ----------
class CodecFrameMsg
{
    int id = 0
    string name = ""

    void _init_( int i, string n )
    {
        this.id = i
        this.name = n
    }
}

# 编码：field 1 = varint(id)，field 2 = 字符串(name)
static ByteBuffer cfEncodeMsg( object v ) throws
{
    CodecFrameMsg m = v as CodecFrameMsg
    var w = ProtocalBuffers.newWriter()
    w.writeInt32( 1, m.id )
    w.writeString( 2, m.name )
    ret ByteBuffer.fromBytes( w.toBytes() )
}

# 解码：按 tag 循环读 1/2 号字段
static CodecFrameMsg cfDecodeMsg( ByteBuffer b ) throws
{
    var r = ProtocalBuffers.newReader( b.toArray() )
    Int32 id = 0
    string name = ""
    while !r.atEnd
    {
        PbTag t = r.readTag()
        if t.fieldNumber == 1      { id = r.readInt32() }
        elif t.fieldNumber == 2    { name = r.readString() }
        else                       { r.skipField( t.wireType ) }
    }
    ret CodecFrameMsg( id, name )
}

# ---------- 2. 构造 codec（of 工厂；与构造器 ProtoCodec<T>(enc, dec) 等价） ----------
function enc = function( object v )
{
    object b = try? CodecFrameTest.cfEncodeMsg( v )
    ret b
}
function dec = function( object v )
{
    ByteBuffer b = v as ByteBuffer
    object m = try? CodecFrameTest.cfDecodeMsg( b )
    ret m
}
ProtoCodec<CodecFrameMsg> codec = ProtoCodec.of<CodecFrameMsg>( enc, dec )
```

单条消息整存整取（无帧头，黄金向量断言基准）：

```sl
# ---------- 3. Serialize.toBytes / fromBytes ----------
CodecFrameMsg msg = CodecFrameMsg( 42, "rt" )
ByteBuffer b = Serialize.toBytes<CodecFrameMsg>( msg, codec )
global.println( b.toHex() )                          # 082a12027274
CodecFrameMsg back = Serialize.fromBytes<CodecFrameMsg>( b, codec )
```

encodeSink + decodeStream 流式往返（varint 帧头；实例方法与 `LengthPrefix.encodeSink<T>(stream, codec)` 静态形式等价）：

```sl
# ---------- 4. 分帧流式往返（MemoryStream 同流先写后读） ----------
var ms = MemoryStream()
StreamSink<CodecFrameMsg> k = codec.encodeSink( ms )
k.add( CodecFrameMsg( 1, "one" ) )
k.add( CodecFrameMsg( 2, "two" ) )
k.close()

Stream<CodecFrameMsg> s = codec.decodeStream( ms )
Task t = s.toListThenTask()
object r = Coroutine.awaitTask( t )
List<CodecFrameMsg> lst = r as List<CodecFrameMsg>
# 超限拒绝：codec.encodeSink( ms, 8 ) 后 add 超 8 字节帧抛 FrameTooLarge
```

bindStream chunked 解码（`Stream<ByteBuffer>` → `Stream<T>`，半包跨 chunk 自动重组；上游截断 close 走 onError 报 UnexpectedEof）：

```sl
# ---------- 5. bindStream：按任意边界切块投递 ----------
StreamController<ByteBuffer> ctrl = StreamController<ByteBuffer>()
Stream<CodecFrameMsg> s2 = codec.bindStream( ctrl.stream )
function onData = function( object v )
{
    CodecFrameMsg m = v as CodecFrameMsg
    global.println( "recv " + m.name )
}
function onError = function( object e )
{
    global.println( "stream error" )
}
function onDone = function()
{
    global.println( "done" )
}
s2.listen( onData, onError, onDone, false )
ctrl.add( ByteBuffer.fromHex( "05" ) )        # 帧长前缀先到（半包）
ctrl.add( ByteBuffer.fromHex( "080112" ) )    # 帧体续到：msg(1,"a")
ctrl.add( ByteBuffer.fromHex( "0161" ) )
ctrl.close()                                  # recv a → done
Coroutine.delay( 50 )                         # 协程调度让监听跑完
```

网络组合（`TcpStream` 即 `ByteStream` 子类，直接接入分帧；详见 `md/lib/Std/net/StreamImplent.md`）：

```sl
# ---------- 6. 长度前缀分帧 RPC 流 ----------
Net.TcpStream conn = Net.Tcp.connect( "127.0.0.1", 9000 )
Stream<CodecFrameMsg> rpc = codec.decodeStream( conn )      # 半包自动续接
StreamSink<CodecFrameMsg> out = codec.encodeSink( conn )    # add(msg) → 帧直写
```

## 8. 与设计文档的关系

- wire format 编解码层与 `ProtoCodec<T>`（§7）均已落地；schema 声明（`ProtoSchema`）与 `@ProtoField` 注解驱动的**代码生成**仍为 STREAM_DESIGN.md §8.3 / §8.4 的后续规划。
- varint / zigzag 原语来自 §15.3 的共用 syscall 组（`vm_sys_codec_*`），与自研 BinaryCodec、RPC 分帧共用，**不在此模块私有实现**。

## 9. 测试

- `test/BaseTest/ProtocalBuffersTest.sl`（85 项断言，A~H 组）：tag 编解码、zigzag32/64、写入黄金 hex 向量（含 varint(-1) 十字节、中文 UTF-8、null bytes/string）、13 类字段往返、官方三组示例向量、嵌套消息（剥壳 + 子消息内读取）、skipField（含 group）、异常注入（截断 varint / 空输入 / 谎报长度 / 负长度）。
- `test/BaseTest/CodecFrameTest.sl`（A~I 组）：A LengthPrefix 帧原语（半包/超限/粘包/续接）、B encodeSink+decodeStream 往返、C ProtoCodec 黄金向量、H bindStream chunked 解码、I periodic 定时流。
