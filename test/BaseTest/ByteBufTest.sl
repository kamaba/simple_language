# ============================================================================
# test/BaseTest/ByteBufTest.sl — ByteBuf L0 字节层冒烟测试
# 设计契约：md/design/STREAM_DESIGN.md §4.1 / §15.2 / §15.3
#
# 覆盖分组：
#   A factory       构造/静态工厂/fromHex 大写输入/深拷贝 wrap
#   B fixedWidth    定宽读写链 + 端序十六进制 + 浮点往返
#   C varint        varUint/varInt 编码长度、十六进制、正负往返、截断语义
#   D bulk          readBytes/writeBytes/readByteArray/readString/readLine
#   E view          slice/duplicate/copy 的索引与存储语义
#   F search        indexOf（相对 readerIndex 偏移）/startsWith/equals/hashCode
#   G capacity      writableBytes/ensureWritable/discardReadBytes/clear/shrink
#   H exceptions    Underflow/IndexOutOfRange/Released（幂等）
#   I lz4           compressBound/可压缩与不可压缩往返/空容器/损坏数据/释放源
#
# 已知不可测项（详见设计文档 §4.1 实现注记）：
#   - CapacityExceeded：maxCapacity == Int32.MaxValue，SL 层 Int32 算术
#     无法触达，且不可真调 ensureWritable(大数)（会走 C 层 2GB realloc）
#   - readString 超长钳制：SL 守卫 _ensureReadable 先抛 Underflow
# ============================================================================

ByteBufTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[ByteBufTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[ByteBufTest] " + name + " : FAIL" )
        }
    }

    # ── 异常注入辅助（throws 单动作，供 label/catch 捕获） ──

    static bbReadU8( ByteBuf buf ) throws
    {
        var v = buf.readU8()
    }

    static bbSetBadReaderIndex( ByteBuf buf ) throws
    {
        buf.readerIndex = 10
    }

    static bbBadSlice( ByteBuf buf ) throws
    {
        var v = buf.slice( 3, 5 )
    }

    # ── A：构造与静态工厂 ──

    static testFactory()
    {
        var b = ByteBuf()
        check( "default capacity 512", b.capacity == 512 )
        check( "default maxCapacity", b.maxCapacity == 2147483647 )
        check( "default indexes zero", b.readerIndex == 0 && b.writerIndex == 0 )
        b.release()

        var b64 = ByteBuf( 64 )
        check( "ByteBuf(64) capacity", b64.capacity == 64 )
        b64.release()

        var hb = ByteBuf.fromHex( "DeadBeef" )
        check( "fromHex readable", hb.readableBytes == 4 )
        check( "fromHex toHex lowercase", hb.toHex() == "deadbeef" )
        check( "fromHex toArray length", hb.toArray().length == 4 )
        hb.release()

        var sb = ByteBuf.fromString( "ABCD" )
        check( "fromString readable", sb.readableBytes == 4 )
        check( "fromString toString", sb.toString() == "ABCD" )
        sb.release()

        UInt8[] four = Array<UInt8>.create( 4 )
        four[0] = 1
        four[1] = 2
        four[2] = 3
        four[3] = 4
        var fb = ByteBuf.fromBytes( four )
        check( "fromBytes roundtrip", fb.toHex() == "01020304" )

        var wb = ByteBuf.wrap( four )
        check( "wrap roundtrip", wb.toHex() == "01020304" )
        four[0] = 99
        check( "wrap deep copy in P0", wb.toHex() == "01020304" )
    }

    # ── B：定宽读写 ──

    static testFixedWidth() throws
    {
        var b = ByteBuf( 16 )
        b.writeU8( 255 )
        b.writeBool( true )
        b.writeI16Le( 4660 )
        b.writeI16Be( 4660 )
        b.writeI32Le( 305419896 )
        b.writeI32Be( 305419896 )
        b.writeI64Le( 1 )
        b.writeF64Be( 3.14 )
        check( "fixed width writerIndex 30", b.writerIndex == 30 )

        check( "readU8", b.readU8() == 255 )
        check( "readBool", b.readBool() == true )
        check( "readI16Le", b.readI16Le() == 4660 )
        check( "readI16Be", b.readI16Be() == 4660 )
        check( "readI32Le", b.readI32Le() == 305419896 )
        check( "readI32Be", b.readI32Be() == 305419896 )
        check( "readI64Le", b.readI64Le() == 1 )
        var f = b.readF64Be()
        check( "readF64Be roundtrip", f == 3.14 )
        check( "readerIndex drained", b.readerIndex == 30 )

        var e = ByteBuf( 16 )
        e.writeI16Be( 4660 )
        check( "I16Be hex", e.toHex() == "1234" )
        e.clear()
        e.writeI32Be( 305419896 )
        check( "I32Be hex", e.toHex() == "12345678" )
        e.clear()
        e.writeI32Le( 305419896 )
        check( "I32Le hex", e.toHex() == "78563412" )
        e.clear()
        e.writeU64Le( 1 )
        check( "U64Le hex", e.toHex() == "0100000000000000" )
    }

    # ── C：变长读写（varint/zigzag） ──

    static testVarint() throws
    {
        check( "varUintEncodedSize(0)", ByteBuf.varUintEncodedSize( 0 ) == 1 )
        check( "varUintEncodedSize(127)", ByteBuf.varUintEncodedSize( 127 ) == 1 )
        check( "varUintEncodedSize(128)", ByteBuf.varUintEncodedSize( 128 ) == 2 )
        check( "varUintEncodedSize(16383)", ByteBuf.varUintEncodedSize( 16383 ) == 2 )
        check( "varUintEncodedSize(16384)", ByteBuf.varUintEncodedSize( 16384 ) == 3 )

        var b = ByteBuf( 16 )
        b.writeVarUint( 0 )
        check( "varUint(0) hex", b.toHex() == "00" )
        b.clear()
        b.writeVarUint( 300 )
        check( "varUint(300) hex", b.toHex() == "ac02" )
        b.clear()
        b.writeVarInt( -1 )
        check( "varInt(-1) zigzag hex", b.toHex() == "01" )
        b.clear()

        b.writeVarInt( 123456789 )
        b.writeVarInt( -123456789 )
        b.writeVarUint( 123456789 )
        check( "varInt positive roundtrip", b.readVarInt() == 123456789 )
        check( "varInt negative roundtrip", b.readVarInt() == -123456789 )
        check( "varUint roundtrip", b.readVarUint() == 123456789 )

        var t = ByteBuf.fromHex( "80" )
        var v = t.readVarUint()
        check( "truncated varUint returns 0", v == 0 )
        check( "truncated varUint no advance", t.readerIndex == 0 )
    }

    # ── D：批量搬运 ──

    static testBulk() throws
    {
        var src = ByteBuf.fromHex( "0102030405" )
        var dst = ByteBuf( 8 )
        src.readBytes( dst, 3 )
        check( "readBytes dst hex", dst.toHex() == "010203" )
        check( "readBytes src advanced", src.readerIndex == 3 )

        var arr = src.readByteArray( 2 )
        check( "readByteArray length", arr.length == 2 )
        check( "readByteArray values", arr[0] == 4 && arr[1] == 5 )

        UInt8[] two = Array<UInt8>.create( 2 )
        two[0] = 4
        two[1] = 5
        var w = ByteBuf( 8 )
        w.writeByteArray( two )
        check( "writeByteArray", w.toHex() == "0405" )

        var all = ByteBuf( 8 )
        var chunk = ByteBuf.fromHex( "aabb" )
        all.writeBytes( chunk )
        check( "writeBytes full", all.toHex() == "aabb" )

        var s = ByteBuf.fromString( "helloworld" )
        check( "readString first half", s.readString( 5 ) == "hello" )
        check( "readString second half", s.readString( 5 ) == "world" )

        var lines = ByteBuf.fromString( "hello\nworld" )
        var l1 = lines.readLine()
        check( "readLine keeps newline", l1 == "hello\n" )
        var l2 = lines.readLine()
        check( "readLine tail without newline is null", l2 == null )
    }

    # ── E：视图 ──

    static testView() throws
    {
        var b = ByteBuf.fromHex( "0001020304" )

        var s1 = b.slice( 1, 3 )
        check( "slice(1,3) readable", s1.toHex() == "010203" )
        check( "slice(1,3) capacity", s1.capacity == 3 )

        b.readerIndex = 2
        var s2 = b.slice()
        check( "slice() from readerIndex", s2.toHex() == "020304" )

        var d = b.duplicate()
        check( "duplicate capacity", d.capacity == 5 )
        check( "duplicate copies readerIndex", d.readerIndex == 2 )
        d.readerIndex = 0
        check( "duplicate index independence", b.readerIndex == 2 )
        check( "duplicate readable", d.toHex() == "0001020304" )

        var c = b.copy()
        check( "copy capacity is readable size", c.capacity == 3 )
        check( "copy readable", c.toHex() == "020304" )
        c.writeU8( 9 )
        check( "copy independent storage", b.toHex() == "020304" )
    }

    # ── F：查找与比较 ──

    static testSearch() throws
    {
        var f = ByteBuf.fromHex( "01020304" )
        check( "indexOf byte offset from 0", f.indexOf( 3 ) == 2 )
        f.readerIndex = 1
        check( "indexOf offset relative to readerIndex", f.indexOf( 3 ) == 1 )

        var pat = ByteBuf.fromHex( "0203" )
        check( "indexOf pattern", f.indexOf( pat ) == 0 )
        check( "indexOf miss", f.indexOf( 255 ) == -1 )

        var pre = ByteBuf.fromHex( "0203" )
        check( "startsWith true", f.startsWith( pre ) )
        var pre2 = ByteBuf.fromHex( "0304" )
        check( "startsWith false", f.startsWith( pre2 ) == false )

        var same = ByteBuf.fromHex( "020304" )
        check( "equals true", f.equals( same ) )
        var diff = ByteBuf.fromHex( "0203" )
        check( "equals false", f.equals( diff ) == false )
        check( "hashCode value semantics", f.hashCode == same.hashCode )
    }

    # ── G：容量管理 ──

    static testCapacity() throws
    {
        var b = ByteBuf( 4 )
        check( "writable equals capacity initially", b.writableBytes == 4 )
        b.writeU8( 1 )
        b.writeU8( 2 )
        check( "writable after writes", b.writableBytes == 2 )

        b.ensureWritable( 8 )
        check( "ensureWritable grows", b.writableBytes >= 8 )
        check( "ensureWritable keeps content", b.toHex() == "0102" )

        var g = ByteBuf.fromHex( "ff010203" )
        g.readU8()
        g.discardReadBytes()
        check( "discardReadBytes readerIndex", g.readerIndex == 0 )
        check( "discardReadBytes content", g.toHex() == "010203" )

        g.clear()
        check( "clear indexes", g.readerIndex == 0 && g.writerIndex == 0 )
        check( "clear readable", g.readableBytes == 0 )

        var s = ByteBuf( 64 )
        s.writeU8( 9 )
        s.shrink()
        check( "shrink capacity", s.capacity == 1 )
        check( "shrink content", s.toHex() == "09" )
    }

    # ── H：异常路径 ──
    # CapacityExceeded 不可测：maxCapacity == Int32.MaxValue，
    # SL 层 Int32 算术无法触达（见设计文档 §4.1）。

    static testExceptions()
    {
        Int32 caught = 0

        var empty = ByteBuf( 4 )
        caught = 0
        label underflowBlock
        {
            try bbReadU8( empty )
        }
        catch
        {
            caught = 1
        }
        check( "Underflow on empty read", caught == 1 )

        var small = ByteBuf( 4 )
        caught = 0
        label readerIndexBlock
        {
            try bbSetBadReaderIndex( small )
        }
        catch
        {
            caught = 1
        }
        check( "IndexOutOfRange on readerIndex set", caught == 1 )

        caught = 0
        label sliceBlock
        {
            try bbBadSlice( small )
        }
        catch
        {
            caught = 1
        }
        check( "IndexOutOfRange on slice", caught == 1 )

        var gone = ByteBuf( 4 )
        gone.writeU8( 1 )
        gone.release()
        gone.release()
        caught = 0
        label releasedBlock
        {
            try bbReadU8( gone )
        }
        catch
        {
            caught = 1
        }
        check( "Released after idempotent release", caught == 1 )
    }

    # ── I：LZ4 块压缩 ──

    static lz4Compress( ByteBuf src ) throws
    {
        var packed = Lz4.compress( src )
        packed.release()
    }

    static lz4Decompress( ByteBuf src ) throws
    {
        var back = Lz4.decompress( src )
        back.release()
    }

    static testLz4() throws
    {
        # bound 数值（isize + isize/255 + 16，再加 4 字节容器头）
        check( "lz4 bound empty", Lz4.compressBound( 0 ) == 20 )
        check( "lz4 bound 255", Lz4.compressBound( 255 ) == 276 )
        check( "lz4 bound negative", Lz4.compressBound( -1 ) == 0 )

        # 可压缩往返：512 字节重复文本
        var text = ""
        for Int32 i = 0, i < 64, i = i + 1
        {
            text = text + "abcdefgh"
        }
        var src = ByteBuf.fromString( text )
        var packed = Lz4.compress( src )
        check( "lz4 compressible shrinks", packed.readableBytes < 512 )
        var back = Lz4.decompress( packed )
        check( "lz4 compressible roundtrip", back.toString() == text )
        check( "lz4 src index untouched", src.readerIndex == 0 )
        src.release()
        packed.release()
        back.release()

        # 不可压缩往返：22 字节高熵数据（只保证往返，不保证变小）
        var noiseHex = "0ff1ceab2d9e37415566788a9bbccddeef0a12345678"
        var noise = ByteBuf.fromHex( noiseHex )
        var npacked = Lz4.compress( noise )
        check( "lz4 incompressible no shrink", npacked.readableBytes >= 26 )
        var nback = Lz4.decompress( npacked )
        check( "lz4 incompressible roundtrip", nback.toHex() == noiseHex )
        noise.release()
        npacked.release()
        nback.release()

        # 空容器：4 字节零长度头 + 0x00 token
        var empty = ByteBuf()
        var epacked = Lz4.compress( empty )
        check( "lz4 empty container hex", epacked.toHex() == "0000000000" )
        check( "lz4 empty container size", epacked.readableBytes == 5 )
        var eback = Lz4.decompress( epacked )
        check( "lz4 empty roundtrip", eback.readableBytes == 0 )
        empty.release()
        epacked.release()
        eback.release()

        # 损坏数据：短于 4 字节头
        var shortBuf = ByteBuf.fromHex( "0102" )
        var caught = 0
        label lz4ShortBlock
        {
            try lz4Decompress( shortBuf )
        }
        catch
        {
            caught = 1
        }
        check( "lz4 short header fails", caught == 1 )
        shortBuf.release()

        # 损坏数据：头声明 100 字节但块只解出 0 字节
        var lieBuf = ByteBuf.fromHex( "6400000000" )
        caught = 0
        label lz4LieBlock
        {
            try lz4Decompress( lieBuf )
        }
        catch
        {
            caught = 1
        }
        check( "lz4 lying length fails", caught == 1 )
        lieBuf.release()

        # 释放后的源 → 异常
        var gone = ByteBuf( 4 )
        gone.writeU8( 1 )
        gone.release()
        caught = 0
        label lz4GoneBlock
        {
            try lz4Compress( gone )
        }
        catch
        {
            caught = 1
        }
        check( "lz4 released source fails", caught == 1 )
    }

    static fun()
    {
        testFactory()
        testFixedWidth()
        testVarint()
        testBulk()
        testView()
        testSearch()
        testCapacity()
        testExceptions()
        testLz4()
        global.println( "[ByteBufTest] all groups done" )
    }
}
