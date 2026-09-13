# ============================================================================
# test/BaseTest/ProtocalBuffersTest.sl — ProtocalBuffers 线格式编解码冒烟测试
# 设计契约：md/design/STREAM_DESIGN.md §15.3（codec 层）
#
# 覆盖分组：
#   A tag            PbTag encode/decode/make/toString
#   B zigzag         PbCoding zigzag32/64 数学与往返、mask32
#   C writerHex      写侧黄金向量（对齐 protobuf 官方线格式向量）
#   D roundtrip      全类型写→读往返（13 字段）
#   E official       官方文档示例报文解码
#   F nested         嵌套消息（writeMessage/readMessage/readMessageBytes/null）
#   G skip           未知字段跳过（各 wireType + StartGroup 兼容）
#   H exceptions     截断 varint / 空输入 / 撒谎长度 / 负长度
#
# 已知不可测项：
#   - fieldNumber >= 2^28：tag = field<<3 超出 Int32 正域（SL 层无 UInt32 算术）
#   - readUInt32() 读 0xFFFFFFFF 以 Int32 位模式承载，断言 == -1
#     （SystemConvertInt32 的 C 层是回绕截断语义）
# ============================================================================

ProtocalBuffersTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[ProtocalBuffersTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[ProtocalBuffersTest] " + name + " : FAIL" )
        }
    }

    # 黄金向量比对用：writer 导出 → ByteBuf → 小写十六进制
    static string hexOf( PbWriter w )
    {
        var b = ByteBuf.fromBytes( w.toBytes() )
        string h = b.toHex()
        b.release()
        ret h
    }

    # ── A：tag 编解码 ──

    static testTag()
    {
        check( "tag encode(1,0) == 8", PbTag.encode( 1, 0 ) == 8 )
        check( "tag encode(2,2) == 18", PbTag.encode( 2, 2 ) == 18 )
        check( "tag encode(15,5) == 125", PbTag.encode( 15, 5 ) == 125 )

        PbTag t = PbTag.decode( 18 )
        check( "tag decode(18) field", t.fieldNumber == 2 )
        check( "tag decode(18) wire", t.wireType == 2 )

        PbTag m = PbTag.make( 7, 5 )
        check( "tag make field", m.fieldNumber == 7 )
        check( "tag make wire", m.wireType == 5 )
        check( "tag make wire masked", PbTag.make( 7, 9 ).wireType == 1 )
        check( "tag toString", m.toString() == "PbTag(#7, wire=5)" )
    }

    # ── B：zigzag 数学 ──

    static testZigzag()
    {
        check( "zigzag32(0) == 0", PbCoding.zigzagEncode32( 0 ) == 0 )
        check( "zigzag32(-1) == 1", PbCoding.zigzagEncode32( -1 ) == 1 )
        check( "zigzag32(1) == 2", PbCoding.zigzagEncode32( 1 ) == 2 )
        check( "zigzag32(-2) == 3", PbCoding.zigzagEncode32( -2 ) == 3 )
        check( "mask32", PbCoding.mask32() == 4294967295 )

        check( "zigzag32 roundtrip neg", PbCoding.zigzagDecode32( PbCoding.zigzagEncode32( -123456789 ) ) == -123456789 )
        check( "zigzag32 roundtrip max", PbCoding.zigzagDecode32( PbCoding.zigzagEncode32( 2147483647 ) ) == 2147483647 )

        check( "zigzag64(1) == 2", PbCoding.zigzagEncode64( 1 ) == 2 )
        check( "zigzag64(-1) == 1", PbCoding.zigzagEncode64( -1 ) == 1 )
        check( "zigzag64 roundtrip", PbCoding.zigzagDecode64( PbCoding.zigzagEncode64( -9876543210 ) ) == -9876543210 )
    }

    # ── C：写侧黄金向量 ──

    static testWriterHex()
    {
        var w = ProtocalBuffers.newWriter()
        w.writeInt32( 1, 150 )
        check( "hex int32(1,150)", hexOf( w ) == "089601" )
        check( "len int32(1,150)", w.length == 3 )

        var w2 = ProtocalBuffers.newWriter()
        w2.writeInt32( 1, -1 )
        check( "hex int32(1,-1) ten bytes", hexOf( w2 ) == "08ffffffffffffffffff01" )

        var w3 = ProtocalBuffers.newWriter()
        w3.writeInt32( 1, 0 )
        check( "hex int32(1,0)", hexOf( w3 ) == "0800" )

        var w4 = ProtocalBuffers.newWriter()
        w4.writeSInt32( 1, -1 )
        w4.writeSInt32( 2, 1 )
        w4.writeSInt32( 3, -2 )
        check( "hex sint32 zigzag trio", hexOf( w4 ) == "080110021803" )

        var w5 = ProtocalBuffers.newWriter()
        w5.writeUInt32( 1, -1 )
        check( "hex uint32(1,-1)", hexOf( w5 ) == "08ffffffff0f" )

        var w6 = ProtocalBuffers.newWriter()
        w6.writeBool( 1, true )
        w6.writeBool( 2, false )
        check( "hex bool pair", hexOf( w6 ) == "08011000" )

        var w7 = ProtocalBuffers.newWriter()
        w7.writeFixed32( 1, 1 )
        check( "hex fixed32(1,1)", hexOf( w7 ) == "0d01000000" )

        var w8 = ProtocalBuffers.newWriter()
        w8.writeFloat( 1, 3.5f )
        check( "hex float(1,3.5f)", hexOf( w8 ) == "0d00006040" )

        var w9 = ProtocalBuffers.newWriter()
        w9.writeFixed64( 1, 1 )
        check( "hex fixed64(1,1)", hexOf( w9 ) == "090100000000000000" )

        var w10 = ProtocalBuffers.newWriter()
        w10.writeDouble( 1, 1.0 )
        check( "hex double(1,1.0)", hexOf( w10 ) == "09000000000000f03f" )

        var w11 = ProtocalBuffers.newWriter()
        w11.writeString( 2, "testing" )
        check( "hex string(2,testing)", hexOf( w11 ) == "120774657374696e67" )

        var w12 = ProtocalBuffers.newWriter()
        w12.writeString( 1, "你好" )
        check( "hex string(1,你好) utf8", hexOf( w12 ) == "0a06e4bda0e5a5bd" )

        UInt8[] four = Array<UInt8>.create( 4 )
        four[0] = 222
        four[1] = 173
        four[2] = 190
        four[3] = 239
        var w13 = ProtocalBuffers.newWriter()
        w13.writeBytes( 1, four )
        check( "hex bytes(1,deadbeef)", hexOf( w13 ) == "0a04deadbeef" )

        # null 实参：bytes/string 均按长度 0 编码（字段仍在场）
        var wB = ProtocalBuffers.newWriter()
        wB.writeBytes( 2, null )
        check( "hex bytes(2,null)", hexOf( wB ) == "1200" )

        var wS = ProtocalBuffers.newWriter()
        wS.writeString( 3, null )
        check( "hex string(3,null)", hexOf( wS ) == "1a00" )
    }

    # ── D：全类型往返 ──

    static testRoundtrip() throws
    {
        var w = ProtocalBuffers.newWriter()
        w.writeInt32( 1, -42 )
        w.writeInt64( 2, 10000000000 )
        w.writeInt64( 3, -9876543210 )
        w.writeSInt32( 4, -123456789 )
        w.writeSInt64( 5, -9876543210 )
        w.writeUInt32( 6, -1 )
        w.writeUInt64( 7, 10000000000 )
        w.writeBool( 8, true )
        w.writeEnum( 9, 3 )
        w.writeFixed32( 10, -1 )
        w.writeSFixed64( 11, -9876543210 )
        w.writeFloat( 12, 2.5f )
        w.writeDouble( 13, -0.5 )
        Int32 total = w.length

        var r = ProtocalBuffers.newReader( w.toBytes() )

        PbTag t1 = r.readTag()
        check( "rt field1 tag", t1.fieldNumber == 1 && t1.wireType == 0 )
        check( "rt int32 -42", r.readInt32() == -42 )

        PbTag t2 = r.readTag()
        check( "rt field2 tag", t2.fieldNumber == 2 )
        check( "rt int64 10^10", r.readInt64() == 10000000000 )

        PbTag t3 = r.readTag()
        check( "rt int64 negative", r.readInt64() == -9876543210 )

        PbTag t4 = r.readTag()
        check( "rt field4 tag", t4.fieldNumber == 4 )
        check( "rt sint32", r.readSInt32() == -123456789 )

        PbTag t5 = r.readTag()
        check( "rt sint64", r.readSInt64() == -9876543210 )

        PbTag t6 = r.readTag()
        check( "rt field6 tag", t6.fieldNumber == 6 )
        check( "rt uint32 bit pattern -1", r.readUInt32() == -1 )

        PbTag t7 = r.readTag()
        check( "rt uint64", r.readUInt64() == 10000000000 )

        PbTag t8 = r.readTag()
        check( "rt bool true", r.readBool() == true )

        PbTag t9 = r.readTag()
        check( "rt enum 3", r.readEnum() == 3 )

        PbTag t10 = r.readTag()
        check( "rt field10 tag fixed32", t10.fieldNumber == 10 && t10.wireType == 5 )
        check( "rt fixed32 -1", r.readFixed32() == -1 )

        PbTag t11 = r.readTag()
        check( "rt sfixed64", r.readSFixed64() == -9876543210 )

        PbTag t12 = r.readTag()
        check( "rt field12 tag fixed32", t12.fieldNumber == 12 && t12.wireType == 5 )
        var f = r.readFloat()
        check( "rt float 2.5", f == 2.5f )

        PbTag t13 = r.readTag()
        check( "rt double -0.5", r.readDouble() == -0.5 )

        check( "rt atEnd", r.atEnd )
        check( "rt position equals length", r.position == total )
    }

    # ── E：官方文档示例报文 ──

    static testOfficialVectors() throws
    {
        # 官方示例 1：field 1 varint 150
        var b1 = ByteBuf.fromHex( "089601" )
        var r1 = ProtocalBuffers.newReader( b1.toArray() )
        b1.release()
        PbTag t1 = r1.readTag()
        check( "official vec1 tag", t1.fieldNumber == 1 && t1.wireType == 0 )
        check( "official vec1 value", r1.readInt32() == 150 )
        check( "official vec1 atEnd", r1.atEnd )

        # 官方示例 2：field 2 length-delimited "testing"
        var b2 = ByteBuf.fromHex( "120774657374696e67" )
        var r2 = ProtocalBuffers.newReader( b2.toArray() )
        b2.release()
        PbTag t2 = r2.readTag()
        check( "official vec2 tag", t2.fieldNumber == 2 && t2.wireType == 2 )
        check( "official vec2 string", r2.readString() == "testing" )
        check( "official vec2 atEnd", r2.atEnd )

        # 官方示例 3：嵌套子消息 1a 03 08 96 01
        var b3 = ByteBuf.fromHex( "1a03089601" )
        var r3 = ProtocalBuffers.newReader( b3.toArray() )
        b3.release()
        PbTag t3 = r3.readTag()
        check( "official vec3 tag", t3.fieldNumber == 3 && t3.wireType == 2 )
        var inner = r3.readMessage()
        PbTag it = inner.readTag()
        check( "official vec3 inner tag", it.fieldNumber == 1 )
        check( "official vec3 inner value", inner.readInt32() == 150 )
        check( "official vec3 inner atEnd", inner.atEnd )
        check( "official vec3 outer atEnd", r3.atEnd )
    }

    # ── F：嵌套消息 ──

    static testNested() throws
    {
        var sub = ProtocalBuffers.newWriter()
        sub.writeInt32( 1, 42 )
        sub.writeString( 2, "hi" )
        check( "nested sub length 6", sub.length == 6 )

        var w = ProtocalBuffers.newWriter()
        w.writeInt32( 1, 7 )
        w.writeMessage( 2, sub )
        check( "nested outer hex", hexOf( w ) == "08071206082a12026869" )

        var r = ProtocalBuffers.newReader( w.toBytes() )
        PbTag t1 = r.readTag()
        check( "nested outer field1", t1.fieldNumber == 1 && r.readInt32() == 7 )
        PbTag t2 = r.readTag()
        check( "nested outer field2", t2.fieldNumber == 2 && t2.wireType == 2 )
        var inner = r.readMessage()
        PbTag it1 = inner.readTag()
        check( "nested inner int", it1.fieldNumber == 1 && inner.readInt32() == 42 )
        PbTag it2 = inner.readTag()
        check( "nested inner string", it2.fieldNumber == 2 && inner.readString() == "hi" )
        check( "nested inner atEnd", inner.atEnd )
        check( "nested outer atEnd", r.atEnd )

        # readMessageBytes 剥出子消息原始字节
        var r2 = ProtocalBuffers.newReader( w.toBytes() )
        r2.readTag()
        r2.readInt32()
        r2.readTag()
        var raw = r2.readMessageBytes()
        check( "nested raw bytes length", raw.length == 6 )

        # writeMessage null 为 no-op
        var w2 = ProtocalBuffers.newWriter()
        w2.writeInt32( 1, 7 )
        w2.writeMessage( 2, null )
        check( "nested writeMessage null noop", w2.length == 2 )
    }

    # ── G：未知字段跳过 ──

    static testSkip() throws
    {
        var w = ProtocalBuffers.newWriter()
        w.writeInt32( 1, 3 )
        w.writeInt32( 3, 300 )
        w.writeString( 4, "xy" )
        w.writeFixed32( 5, -1 )
        w.writeFixed64( 6, -1 )
        w.writeInt32( 2, 9 )

        var r = ProtocalBuffers.newReader( w.toBytes() )
        PbTag s1 = r.readTag()
        r.skipField( s1.wireType )
        PbTag s2 = r.readTag()
        r.skipField( s2.wireType )
        PbTag s3 = r.readTag()
        r.skipField( s3.wireType )
        PbTag s4 = r.readTag()
        r.skipField( s4.wireType )
        PbTag s5 = r.readTag()
        r.skipField( s5.wireType )
        PbTag t = r.readTag()
        check( "skip reaches field2", t.fieldNumber == 2 && r.readInt32() == 9 )
        check( "skip drains input", r.atEnd )

        # groups 兼容（已废弃的 wire 3/4）：13 08 2a 14
        var gb = ByteBuf.fromHex( "13082a14" )
        var gr = ProtocalBuffers.newReader( gb.toArray() )
        gb.release()
        PbTag gt = gr.readTag()
        check( "group start tag", gt.fieldNumber == 2 && gt.wireType == 3 )
        gr.skipField( gt.wireType )
        check( "group skip atEnd", gr.atEnd )
    }

    # ── H：异常路径 ──

    static pbReadTagOn( Array<UInt8> bytes ) throws
    {
        var r = ProtocalBuffers.newReader( bytes )
        var t = r.readTag()
    }

    static pbReadBytesOn( Array<UInt8> bytes ) throws
    {
        var r = ProtocalBuffers.newReader( bytes )
        r.readTag()
        var blob = r.readBytes()
    }

    static pbSkipOn( Array<UInt8> bytes ) throws
    {
        var r = ProtocalBuffers.newReader( bytes )
        PbTag t = r.readTag()
        r.skipField( t.wireType )
    }

    static testExceptions()
    {
        Int32 caught = 0

        # 截断 varint：单字节 0x80 无终止
        UInt8[] one = Array<UInt8>.create( 1 )
        one[0] = 128
        caught = 0
        label pbTruncVarintBlock
        {
            try pbReadTagOn( one )
        }
        catch
        {
            caught = 1
        }
        check( "truncated varint throws", caught == 1 )

        # 空输入（null → 空 reader）：readTag 抛
        caught = 0
        label pbEmptyBlock
        {
            try pbReadTagOn( null )
        }
        catch
        {
            caught = 1
        }
        check( "empty input throws", caught == 1 )

        # 撒谎长度：声明 5 字节只有 2 字节
        var lb = ByteBuf.fromHex( "0a050102" )
        var larr = lb.toArray()
        lb.release()
        caught = 0
        label pbLieLenBlock
        {
            try pbReadBytesOn( larr )
        }
        catch
        {
            caught = 1
        }
        check( "lying length throws", caught == 1 )

        # skipField 撒谎长度
        var sb = ByteBuf.fromHex( "0a05" )
        var sarr = sb.toArray()
        sb.release()
        caught = 0
        label pbSkipShortBlock
        {
            try pbSkipOn( sarr )
        }
        catch
        {
            caught = 1
        }
        check( "skip short length throws", caught == 1 )

        # 负长度：varint(-300) 作长度前缀 → _readLength 抛
        var w3 = ProtocalBuffers.newWriter()
        w3.writeTag( 1, 2 )
        w3.writeVarint( -300 )
        var narr = w3.toBytes()
        caught = 0
        label pbNegLenBlock
        {
            try pbReadBytesOn( narr )
        }
        catch
        {
            caught = 1
        }
        check( "negative length throws", caught == 1 )
    }

    static fun()
    {
        testTag()
        testZigzag()
        testWriterHex()
        testRoundtrip()
        testOfficialVectors()
        testNested()
        testSkip()
        testExceptions()
        global.println( "[ProtocalBuffersTest] all groups done" )
    }
}
