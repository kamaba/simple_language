# ProtocalBuffers（Protocol Buffers 线格式编解码）

`ProtocalBuffers` 是 Protocol Buffers（protobuf）**线格式（wire format）**编解码器，位于 `Core` 标准库的 `Text` 命名空间下（`Core/Text/ProtocalBuffers.sl`），proto3 兼容子集，底座是 `ByteBuf`（L0 字节层）。

定位：**只做二进制层的编解码**——`.proto` schema 与 message 类由业务侧自己定义，本项目不引入代码生成。复杂计算（varint / zigzag / IEEE754 位打包 / UTF-8）全部走 C 层共用原语：

| 原语 | 承载 |
|------|------|
| varint（无符号 LEB128） | `ByteBuf.writeVarUint` / `readVarUint` |
| zigzag varint | `ByteBuf.writeVarInt` / `readVarInt` |
| 定长小端整数 | `ByteBuf.writeI32Le` / `writeI64Le` / `readI32Le` / `readI64Le` |
| 浮点位打包 | `ByteBuf.writeF32Le` / `writeF64Le` / `readF32Le` / `readF64Le` |
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
var h1 = ByteBuf.fromBytes( w1.toBytes() )
global.println( h1.toHex() )     # 089601
h1.release()

# string field2 = "testing"  →  hex "120774657374696e67"
PbWriter w2 = ProtocalBuffers.newWriter()
w2.writeString( 2, "testing" )
var h2 = ByteBuf.fromBytes( w2.toBytes() )
global.println( h2.toHex() )    # 120774657374696e67
h2.release()
```

## 7. 与设计文档的关系

- 当前交付的是**手写 wire format 编解码层**；schema 声明（`ProtoSchema`）与 `@ProtoField` 注解驱动的**代码生成**、`ProtoCodec<T>`（依赖 Stream 体系与长度前缀分帧）仍为 STREAM_DESIGN.md §8.3 / §8.4 的 P2 规划。
- varint / zigzag 原语来自 §15.3 的共用 syscall 组（`vm_sys_codec_*`），与自研 BinaryCodec、RPC 分帧共用，**不在此模块私有实现**。

## 8. 测试

`test/BaseTest/ProtocalBuffersTest.sl`（85 项断言，A~H 组）：tag 编解码、zigzag32/64、写入黄金 hex 向量（含 varint(-1) 十字节、中文 UTF-8、null bytes/string）、13 类字段往返、官方三组示例向量、嵌套消息（剥壳 + 子消息内读取）、skipField（含 group）、异常注入（截断 varint / 空输入 / 谎报长度 / 负长度）。
