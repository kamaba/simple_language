# =========================================================================
# Core/Text/ProtocalBuffers.sl —— Protocol Buffers (protobuf) 线格式编解码
#
# 设计定位：
#   - 这是「线格式（wire format）」编解码器，对应 protobuf 的二进制层；
#     .proto schema 与 message 类由业务侧自己定义（本项目不引入代码生成）。
#   - 底座是 IO/ByteBuf（L0 字节层），复杂计算全部走 C 层 system method：
#       * varint        → ByteBuf.writeVarUint / readVarUint（无符号 LEB128）
#       * zigzag varint → ByteBuf.writeVarInt  / readVarInt（zigzag + LEB128 一体）
#       * 定长小端      → ByteBuf.writeI32Le / writeI64Le / readI32Le / readI64Le
#       * 浮点位打包    → ByteBuf.writeF32Le / writeF64Le / readF32Le / readF64Le
#                         （IEEE754 位重释在 C 层完成，不再需要逃逸口）
#       * UTF-8 编解码  → ByteBuf.writeString（无长度前缀）/ readString(len)
#       * UInt64→Int64 位重释 → SystemConvertInt64FromUInt64（readVarUint
#                         返回 UInt64，protobuf 原始 varint 值需按位重释为
#                         Int64；SystemConvertInt64 会截断到低 32 位，不可用）
#   - 原实现中的 SystemCallExternalFunction 逃逸口全部移除。
#
# 关键约定（与 protobuf 官方语义一致）：
#   - tag = (fieldNumber << 3) | wireType
#   - int32 以「64 位符号扩展」的 varint 写出（负数 10 字节）；uint32 取低 32 位后 varint
#   - sint32/sint64 走 zigzag 后再 varint（sint32 借道 64 位 zigzag，数学等价）
#   - 未知字段用 skipField 跳过（保持前向/后向兼容）
#   - C 层 varint 截断时「告警 + 默认值 + 不推进 readerIndex」，
#     readVarintRaw 以 readerIndex 是否前移判失败，统一抛 BufferError.Underflow
#
# 用法：
#   PbWriter w = ProtocalBuffers.newWriter()
#   w.writeInt32( 1, 42 )
#   w.writeString( 2, "hello" )
#   w.writeMessage( 3, subWriter )
#   Array<UInt8> blob = w.toBytes()
#
#   PbReader r = ProtocalBuffers.newReader( blob )
#   while !r.atEnd
#   {
#       PbTag t = r.readTag()
#       if t.fieldNumber == 1 { int v = r.readInt32() }
#       elif t.fieldNumber == 2 { string s = r.readString() }
#       else { r.skipField( t.wireType ) }
#   }
# =========================================================================

# 线格式类型（protobuf wire type）
# StartGroup/EndGroup 已废弃（groups），仅读取端兼容跳过
public enum EPbWireType
{
    Varint = 0
    Fixed64 = 1
    LengthDelimited = 2
    StartGroup = 3
    EndGroup = 4
    Fixed32 = 5
}

# 字段 tag：fieldNumber(1..2^29) + wireType(0..5)
public class PbTag extends Object
{
    public Int32 fieldNumber = 0
    public Int32 wireType = 0

    override _init_()
    {
    }

    static PbTag make( Int32 field, Int32 wire )
    {
        PbTag t = new()
        t.fieldNumber = field
        t.wireType = wire & 7
        ret t
    }

    # tag 原始整数 = (field << 3) | wire
    static Int32 encode( Int32 field, Int32 wire )
    {
        Int64 raw = ( SystemConvertInt64( field ) << 3 ) | SystemConvertInt64( wire & 7 )
        ret SystemConvertInt32( raw )
    }

    static PbTag decode( Int32 raw )
    {
        PbTag t = new()
        t.fieldNumber = SystemConvertInt32( SystemConvertInt64( raw ) >> 3 )
        t.wireType = raw & 7
        ret t
    }

    override string toString()
    {
        ret "PbTag(#" + this.fieldNumber.toString() + ", wire=" + this.wireType.toString() + ")"
    }
}

# 线格式原语薄封装：zigzag 走 C 层 SystemZigZagEncode / SystemZigZagDecode
public class PbCoding extends Object
{
    override _init_()
    {
    }

    # 低 32 位的 64 位掩码（避免写 2^32 字面量）：0x10000<<16 - 1
    static Int64 mask32()
    {
        ret ( SystemConvertInt64( 0x10000 ) << 16 ) - 1
    }

    # ── zigzag：有符号 <-> 无符号 varint 的映射（C 层实现）──────
    # 32 位值经 64 位 zigzag 编码数学等价（结果恒在 [0, 2^32) 内），
    # 解码同理，故 sint32 直接借道 64 位原语
    static Int64 zigzagEncode32( Int32 n )
    {
        ret SystemZigZagEncode( SystemConvertInt64( n ) )
    }

    static Int32 zigzagDecode32( Int64 value )
    {
        ret SystemConvertInt32( SystemZigZagDecode( value ) )
    }

    static Int64 zigzagEncode64( Int64 n )
    {
        ret SystemZigZagEncode( n )
    }

    static Int64 zigzagDecode64( Int64 value )
    {
        ret SystemZigZagDecode( value )
    }
}

# ── 写入器 ─────────────────────────────────────────────────
public class PbWriter extends Object
{
    ByteBuf _buf = null

    override _init_()
    {
        this._buf = ByteBuf()
    }

    get Int32 length()
    {
        ret this._buf.writerIndex
    }

    # 追加一个原始字节
    void writeRawByte( Int32 b )
    {
        this._buf.writeU8( SystemConvertUInt8( b ) )
    }

    # 原始 varint（无 zigzag）；负数按 64 位位模式写出（10 字节），
    # 正是 protobuf int32/int64 负值的线格式
    void writeVarint( Int64 v )
    {
        this._buf.writeVarUint( v )
    }

    void writeTag( Int32 field, Int32 wire )
    {
        Int64 raw = ( SystemConvertInt64( field ) << 3 ) | SystemConvertInt64( wire & 7 )
        this._buf.writeVarUint( raw )
    }

    # ── 整数类型 ───────────────────────────────────────────
    void writeInt32( Int32 field, Int32 v )
    {
        this.writeTag( field, EPbWireType.Varint )
        this._buf.writeVarUint( SystemConvertInt64( v ) )
    }

    void writeInt64( Int32 field, Int64 v )
    {
        this.writeTag( field, EPbWireType.Varint )
        this._buf.writeVarUint( v )
    }

    void writeUInt32( Int32 field, Int32 v )
    {
        this.writeTag( field, EPbWireType.Varint )
        Int64 uv = SystemConvertInt64( v ) & PbCoding.mask32()
        this._buf.writeVarUint( uv )
    }

    void writeUInt64( Int32 field, Int64 v )
    {
        this.writeTag( field, EPbWireType.Varint )
        this._buf.writeVarUint( v )
    }

    void writeSInt32( Int32 field, Int32 v )
    {
        this.writeTag( field, EPbWireType.Varint )
        this._buf.writeVarInt( SystemConvertInt64( v ) )
    }

    void writeSInt64( Int32 field, Int64 v )
    {
        this.writeTag( field, EPbWireType.Varint )
        this._buf.writeVarInt( v )
    }

    void writeBool( Int32 field, bool v )
    {
        this.writeTag( field, EPbWireType.Varint )
        if v
        {
            this._buf.writeVarUint( SystemConvertInt64( 1 ) )
        }
        else
        {
            this._buf.writeVarUint( SystemConvertInt64( 0 ) )
        }
    }

    void writeEnum( Int32 field, Int32 v )
    {
        this.writeInt32( field, v )
    }

    # ── 定长小端（浮点位打包在 C 层 writeF32Le / writeF64Le 完成）──
    void writeFixed32( Int32 field, Int32 v )
    {
        this.writeTag( field, EPbWireType.Fixed32 )
        this._buf.writeI32Le( v )
    }

    void writeSFixed32( Int32 field, Int32 v )
    {
        this.writeTag( field, EPbWireType.Fixed32 )
        this._buf.writeI32Le( v )
    }

    void writeFixed64( Int32 field, Int64 v )
    {
        this.writeTag( field, EPbWireType.Fixed64 )
        this._buf.writeI64Le( v )
    }

    void writeSFixed64( Int32 field, Int64 v )
    {
        this.writeTag( field, EPbWireType.Fixed64 )
        this._buf.writeI64Le( v )
    }

    void writeFloat( Int32 field, Float32 v )
    {
        this.writeTag( field, EPbWireType.Fixed32 )
        this._buf.writeF32Le( v )
    }

    void writeDouble( Int32 field, Float64 v )
    {
        this.writeTag( field, EPbWireType.Fixed64 )
        this._buf.writeF64Le( v )
    }

    # ── 长度前缀类型 ───────────────────────────────────────
    void writeString( Int32 field, string s )
    {
        this.writeTag( field, EPbWireType.LengthDelimited )
        if s == null
        {
            this._buf.writeVarUint( 0 )
            ret
        }
        # 临时缓冲做 UTF-8 编码（writeString 不带长度前缀），量完长度再搬运
        ByteBuf tmp = ByteBuf()
        tmp.writeString( s )
        this._buf.writeVarUint( SystemConvertInt64( tmp.readableBytes ) )
        this._buf.writeBytes( tmp )
        tmp.release()
    }

    void writeBytes( Int32 field, Array<UInt8> bytes )
    {
        this.writeTag( field, EPbWireType.LengthDelimited )
        Int32 n = 0
        if bytes != null
        {
            n = bytes.length
        }
        this._buf.writeVarUint( SystemConvertInt64( n ) )
        if n > 0
        {
            this._buf.writeByteArray( bytes )
        }
    }

    void writeMessage( Int32 field, PbWriter sub )
    {
        if sub == null
        {
            ret
        }
        this.writeTag( field, EPbWireType.LengthDelimited )
        # slice 视图只读消费：writeBytes 会推进视图自己的 readerIndex，
        # 不动 sub 的索引；用完释放视图句柄
        ByteBuf view = sub._buf.slice()
        this._buf.writeVarUint( SystemConvertInt64( view.readableBytes ) )
        this._buf.writeBytes( view )
        view.release()
    }

    # 导出最终字节数组
    Array<UInt8> toBytes()
    {
        ret this._buf.toArray()
    }

    override string toString()
    {
        ret "PbWriter(" + this._buf.writerIndex.toString() + " bytes)"
    }
}

# ── 读取器 ─────────────────────────────────────────────────
public class PbReader extends Object
{
    ByteBuf _buf = null

    override _init_()
    {
        this._buf = ByteBuf()
    }

    # fromBytes 为深拷贝语义（P0 约定），读取器独立持有数据
    _init_( Array<UInt8> bytes )
    {
        if bytes == null
        {
            this._buf = ByteBuf()
        }
        else
        {
            this._buf = ByteBuf.fromBytes( bytes )
        }
    }

    get bool atEnd()
    {
        ret this._buf.readableBytes == 0
    }

    get Int32 position()
    {
        ret this._buf.readerIndex
    }

    # ── varint / tag ────────────────────────────────────────
    # 原始 varint（无 zigzag）。C 层 readVarUint 截断时返回 0 且不推进
    # readerIndex，此处以 readerIndex 是否前移判失败，统一抛 Underflow
    Int64 readVarintRaw() throws
    {
        Int32 before = this._buf.readerIndex
        var u = this._buf.readVarUint()
        if this._buf.readerIndex == before
        {
            throw BufferError.Underflow
        }
        ret SystemConvertInt64FromUInt64( u )
    }

    PbTag readTag() throws
    {
        ret PbTag.decode( SystemConvertInt32( this.readVarintRaw() ) )
    }

    # ── 整数类型 ───────────────────────────────────────────
    # protobuf 语义：int32 解码取 varint 低 32 位再按有符号解释
    Int32 readInt32() throws
    {
        ret SystemConvertInt32( this.readVarintRaw() )
    }

    Int64 readInt64() throws
    {
        ret this.readVarintRaw()
    }

    Int32 readUInt32() throws
    {
        ret SystemConvertInt32( this.readVarintRaw() & PbCoding.mask32() )
    }

    Int64 readUInt64() throws
    {
        ret this.readVarintRaw()
    }

    Int32 readSInt32() throws
    {
        ret SystemConvertInt32( this._buf.readVarInt() )
    }

    Int64 readSInt64() throws
    {
        ret this._buf.readVarInt()
    }

    bool readBool() throws
    {
        ret this.readVarintRaw() != 0
    }

    Int32 readEnum() throws
    {
        ret SystemConvertInt32( this.readVarintRaw() )
    }

    # ── 定长（浮点位重释在 C 层 readF32Le / readF64Le 完成）──
    Int32 readFixed32() throws
    {
        ret this._buf.readI32Le()
    }

    Int64 readFixed64() throws
    {
        ret this._buf.readI64Le()
    }

    Int32 readSFixed32() throws
    {
        ret this._buf.readI32Le()
    }

    Int64 readSFixed64() throws
    {
        ret this._buf.readI64Le()
    }

    Float32 readFloat() throws
    {
        ret this._buf.readF32Le()
    }

    Float64 readDouble() throws
    {
        ret this._buf.readF64Le()
    }

    # ── 长度前缀 ───────────────────────────────────────────
    # 长度前缀统一入口：损坏数据（负长度）按 Underflow 处理
    Int32 _readLength() throws
    {
        Int32 n = SystemConvertInt32( this.readVarintRaw() )
        if n < 0
        {
            throw BufferError.Underflow
        }
        ret n
    }

    Array<UInt8> readBytes() throws
    {
        ret this._buf.readByteArray( this._readLength() )
    }

    string readString() throws
    {
        ret this._buf.readString( this._readLength() )
    }

    # 拷贝方案：剥出子消息原始字节再建独立读取器，
    # 避免视图句柄在注册表里悬挂（P0 无 root-pin 机制）
    PbReader readMessage() throws
    {
        ret ProtocalBuffers.newReader( this.readMessageBytes() )
    }

    # 把整个子消息原始字节剥出来（交给上层 / 其他解码器）
    Array<UInt8> readMessageBytes() throws
    {
        ret this._buf.readByteArray( this._readLength() )
    }

    # ── 跳过未知字段（前向/后向兼容）────────────────────────
    void skipField( Int32 wireType ) throws
    {
        if wireType == EPbWireType.Varint
        {
            this.readVarintRaw()
        }
        elif wireType == EPbWireType.Fixed64
        {
            this._buf.skipBytes( 8 )
        }
        elif wireType == EPbWireType.Fixed32
        {
            this._buf.skipBytes( 4 )
        }
        elif wireType == EPbWireType.LengthDelimited
        {
            this._buf.skipBytes( this._readLength() )
        }
        elif wireType == EPbWireType.StartGroup
        {
            # 递归跳过直到 EndGroup(4)
            while !this.atEnd
            {
                PbTag t = this.readTag()
                if t.wireType == EPbWireType.EndGroup
                {
                    break
                }
                this.skipField( t.wireType )
            }
        }
        # EndGroup(4)：无内容
    }

    override string toString()
    {
        ret "PbReader(pos=" + this._buf.readerIndex.toString() + ", readable=" + this._buf.readableBytes.toString() + ")"
    }
}

# ── 门面（保留原 ProtocalBuffers 类名与 extends Object 约定）──
public class ProtocalBuffers extends Object
{
    override _init_()
    {
    }

    static PbWriter newWriter()
    {
        ret PbWriter()
    }

    static PbReader newReader( Array<UInt8> bytes )
    {
        ret PbReader( bytes )
    }

    override string toString()
    {
        ret "ProtocalBuffers(wire-format codec, protobuf3-compatible)"
    }
}
