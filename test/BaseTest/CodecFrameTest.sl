# ============================================================================
# CodecFrameTest.sl -- P2 Codec/分帧/序列化验收测试
#
# 参考设计档：md/design/STREAM_DESIGN.md §18（codec 层 / periodic 组）。
# 测试对象（全部 Core/IO，按测试路由落 BaseTest/CSimpleVMCoreTest）：
#   A  LengthPrefix 帧原语（writeFrame / writeFrameBytes / readFrameLength /
#      tryReadFrame：半包 / 超限 / 截断 varint / 粘包 / 续接）
#   B  encodeSink + decodeStream 流式往返（含超限拒绝）
#   C  ProtoCodec 往返（黄金向量 hex 断言）
#   D  Utf8Codec（string ↔ UTF-8 字节数组）
#   E  BinaryCodec（varuint 自定义二进制编码）
#   F  JsonCodec（data 类型 JSON 文本往返）
#   G  Serialize（toStream/fromStream 整存整取 + toElementStream 分帧流）
#   H  bindStream chunked 解码（跨 chunk 重组 / 截断流报错）
#   I  Stream.periodic 定时流（周期生产 + 取消冻结）
#
# 编写约定（沿用 StreamTest.sl / ProtocalBuffersTest.sl 先例）：
#  1. throws 辅助一律 cf 前缀静态方法，闭包经 try? 中转（闭包不能声明
#     throws；try? 吞异常为 null，正常路径不受影响）。
#  2. 静态字段在闭包内直接访问（带类名前缀，StreamTest 先例）。
#  3. 匿名闭包字面量不能直接作调用实参，先赋 function 变量再传参。
#  4. SL 异常非受检：fun() 不声明 throws 直接调组方法（ProtocalBuffersTest
#     先例）；预期抛错处用 label{} try 语句 catch{} 捕获。
#  5. 黄金向量：msg(1,"a") 帧 = 050801120161（payload 08 01 12 01 61）；
#     msg(42,"rt") payload = 082a12027274；(1,"one") payload 7 字节、
#     (3,"three") payload 9 字节（maxFrame=8 边界）。
# ============================================================================

CodecFrameMsg
{
    int id = 0
    string name = ""

    void _init_( int i, string n )
    {
        this.id = i
        this.name = n
    }
}

data CodecUserData
{
    uid = 0
    name = ""
    score = 0.0
}

CodecFrameTest
{
    # ---------- 统一断言辅助 ----------
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[CodecFrameTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[CodecFrameTest] " + name + " : FAIL" )
        }
    }

    # ---------- cf 前缀 throws 辅助（闭包经 try? 中转调用） ----------

    # ProtoBuf 编码：field 1 = varint(id)，field 2 = 字符串(name)
    static ByteBuf cfEncodeMsg( object v ) throws
    {
        CodecFrameMsg m = v as CodecFrameMsg
        var w = ProtocalBuffers.newWriter()
        w.writeInt32( 1, m.id )
        w.writeString( 2, m.name )
        ret ByteBuf.fromBytes( w.toBytes() )
    }

    # ProtoBuf 解码：按 tag 循环读 1/2 号字段
    static CodecFrameMsg cfDecodeMsg( ByteBuf b ) throws
    {
        var r = ProtocalBuffers.newReader( b.toArray() )
        Int32 id = 0
        string name = ""
        while !r.atEnd
        {
            PbTag t = r.readTag()
            if t.fieldNumber == 1
            {
                id = r.readInt32()
            }
            else
            {
                if t.fieldNumber == 2
                {
                    name = r.readString()
                }
            }
        }
        ret CodecFrameMsg( id, name )
    }

    # varuint 自定义编码：varuint(id) + name 裸字节（无 field 标签）
    static ByteBuf cfBinEncode( object v ) throws
    {
        CodecFrameMsg m = v as CodecFrameMsg
        var b = ByteBuf( 64 )
        b.writeVarUint( SystemConvertInt64( m.id ) )
        b.writeString( m.name )
        ret b
    }

    static CodecFrameMsg cfBinDecode( ByteBuf b ) throws
    {
        var u = b.readVarUint()
        Int64 id64 = SystemConvertInt64FromUInt64( u )
        Int32 id = SystemConvertInt32( id64 )
        string name = b.readString( b.readableBytes )
        ret CodecFrameMsg( id, name )
    }

    # ProtoCodec 工厂：encoder/decoder 闭包经 try? 调 throws 辅助
    static ProtoCodec<CodecFrameMsg> cfMakeCodec()
    {
        function enc = function( object v )
        {
            object b = try? CodecFrameTest.cfEncodeMsg( v )
            ret b
        }
        function dec = function( object v )
        {
            ByteBuf b = v as ByteBuf
            object m = try? CodecFrameTest.cfDecodeMsg( b )
            ret m
        }
        var pc = ProtoCodec<CodecFrameMsg>( enc, dec )
        ret pc
    }

    # ---------- 全局状态（闭包/协程间共享静态字段） ----------
    static int g_hCount = 0
    static string g_hNames = ""
    static bool g_hDone = false
    static bool g_hErr = false
    static int g_hCount2 = 0
    static bool g_hErr2 = false
    static int g_tickCount = 0
    static int g_tickLast = 0

    # ======================================================================
    # A 组：LengthPrefix 帧原语（半包 / 超限 / 粘包 / 续接）
    # ======================================================================
    static testGroupA()
    {
        global.println( "========== A: LengthPrefix 帧原语 ==========" )

        # A1 writeFrame + tryReadFrame 往返
        var payload1 = ByteBuf.fromString( "ABCD" )
        var dst1 = ByteBuf()
        LengthPrefix.writeFrame( dst1, payload1 )
        check( "A1 writeFrame bytes", dst1.readableBytes == 5 && dst1.toHex() == "0441424344" )
        var frame1 = LengthPrefix.tryReadFrame( dst1, LengthPrefix.defaultMaxFrame() )
        check( "A1 tryReadFrame roundtrip", frame1 != null && frame1.readableBytes == 4 && frame1.toString() == "ABCD" )

        # A2 writeFrameBytes 数组形式
        UInt8[] four = Array<UInt8>.create( 4 )
        four[0] = 222
        four[1] = 173
        four[2] = 190
        four[3] = 239
        var dst2 = ByteBuf()
        LengthPrefix.writeFrameBytes( dst2, four )
        check( "A2 writeFrameBytes", dst2.readableBytes == 5 && dst2.toHex() == "04deadbeef" )

        # A3 readFrameLength：半包 / 超限 / 截断 varint / 空
        var src3a = ByteBuf.fromHex( "05" )
        Int32 r3a = LengthPrefix.readFrameLength( src3a, 65536 )
        check( "A3 half frame", r3a == -1 && src3a.readerIndex == 0 )

        var src3b = ByteBuf.fromHex( "2a" )
        Int32 r3b = LengthPrefix.readFrameLength( src3b, 8 )
        check( "A3 over max", r3b == -2 && src3b.readerIndex == 0 )

        var src3c = ByteBuf.fromHex( "80" )
        Int32 r3c = LengthPrefix.readFrameLength( src3c, 65536 )
        check( "A3 truncated varint", r3c == -1 && src3c.readerIndex == 0 )

        var src3d = ByteBuf()
        Int32 r3d = LengthPrefix.readFrameLength( src3d, 65536 )
        var f3d = LengthPrefix.tryReadFrame( src3d, 65536 )
        check( "A3 empty", r3d == -1 && f3d == null )

        # A4 粘包：两帧连写，逐帧读出后第三帧 null
        var src4 = ByteBuf.fromHex( "024142024344" )
        var f4a = LengthPrefix.tryReadFrame( src4, 65536 )
        var f4b = LengthPrefix.tryReadFrame( src4, 65536 )
        var f4c = LengthPrefix.tryReadFrame( src4, 65536 )
        check( "A4 sticky frames", f4a != null && f4a.toString() == "AB" && f4b != null && f4b.toString() == "CD" && f4c == null )

        # A5 半包续接：先到 1/3 字节读不出，补齐后整帧读出
        var acc5 = ByteBuf( 64 )
        acc5.writeBytes( ByteBuf.fromHex( "0361" ) )
        var f5a = LengthPrefix.tryReadFrame( acc5, 65536 )
        acc5.writeBytes( ByteBuf.fromHex( "6263" ) )
        var f5b = LengthPrefix.tryReadFrame( acc5, 65536 )
        check( "A5 partial resume", f5a == null && f5b != null && f5b.toString() == "abc" )

        # A6 超限抛 FrameTooLarge（payload 10 > maxFrame 8）
        var src6 = ByteBuf.fromHex( "0a41414141414141414141" )
        bool a6 = false
        label labA6
        {
            try LengthPrefix.tryReadFrame( src6, 8 )
        }
        catch
        {
            a6 = true
        }
        check( "A6 frame too large", a6 )
    }

    # ======================================================================
    # B 组：encodeSink + decodeStream 流式往返
    # ======================================================================
    static testGroupB()
    {
        global.println( "========== B: encodeSink + decodeStream ==========" )

        # B1 三条消息经 MemoryStream 写读往返（同流先写后读）
        var ms1 = MemoryStream()
        StreamSink<CodecFrameMsg> k1 = LengthPrefix.encodeSink<CodecFrameMsg>( ms1, cfMakeCodec() )
        k1.add( CodecFrameMsg( 1, "one" ) )
        k1.add( CodecFrameMsg( 2, "two" ) )
        k1.add( CodecFrameMsg( 3, "three" ) )
        k1.close()
        Stream<CodecFrameMsg> s1 = LengthPrefix.decodeStream<CodecFrameMsg>( ms1, cfMakeCodec() )
        Task t1 = s1.toList()
        object r1 = Coroutine.awaitHandle( t1 )
        List<CodecFrameMsg> lst1 = r1 as List<CodecFrameMsg>
        int b1cnt = 0
        int b1sum = 0
        string b1names = ""
        if lst1 != null
        {
            for v in lst1
            {
                CodecFrameMsg m = v as CodecFrameMsg
                if m != null
                {
                    b1cnt = b1cnt + 1
                    b1sum = b1sum + m.id
                    b1names = b1names + m.name
                }
            }
        }
        check( "B1 stream roundtrip", b1cnt == 3 && b1sum == 6 && b1names == "onetwothree" )

        # B2 超限拒绝：maxFrame=8，(1,"one") payload 7 字节过，
        #    (3,"three") payload 9 字节抛 FrameTooLarge（具体类型持引用
        #    才能以 try 前缀调带 throws 的 add）
        var ms2 = MemoryStream()
        var k2 = _FrameEncodeSink<CodecFrameMsg>( ms2, cfMakeCodec(), 8 )
        k2.add( CodecFrameMsg( 1, "one" ) )
        bool b2over = false
        label labB2
        {
            try k2.add( CodecFrameMsg( 3, "three" ) )
        }
        catch
        {
            b2over = true
        }
        string b2hex = ms2.toByteBuf().toHex()
        check( "B2 over limit", b2over && b2hex == "07080112036f6e65" )
    }

    # ======================================================================
    # C 组：ProtoCodec 往返（黄金向量）
    # ======================================================================
    static testGroupC()
    {
        global.println( "========== C: ProtoCodec 往返 ==========" )

        ProtoCodec<CodecFrameMsg> codec = cfMakeCodec()
        CodecFrameMsg msg = CodecFrameMsg( 42, "rt" )
        ByteBuf b = Serialize.toBytes<CodecFrameMsg>( msg, codec )
        check( "C1 proto encode hex", b.toHex() == "082a12027274" )
        CodecFrameMsg back = Serialize.fromBytes<CodecFrameMsg>( b, codec )
        check( "C2 proto decode", back != null && back.id == 42 && back.name == "rt" )
    }

    # ======================================================================
    # D 组：Utf8Codec
    # ======================================================================
    static testGroupD()
    {
        global.println( "========== D: Utf8Codec ==========" )

        Utf8Codec u8 = Utf8Codec()
        Array<UInt8> enc = u8.encode( "你好SL" )
        check( "D1 utf8 encode", enc != null && enc.length == 8 )
        string dec = u8.decode( enc )
        check( "D2 utf8 decode", dec == "你好SL" )
    }

    # ======================================================================
    # E 组：BinaryCodec（varuint 自定义二进制编码）
    # ======================================================================
    static testGroupE()
    {
        global.println( "========== E: BinaryCodec ==========" )

        function benc = function( object v )
        {
            object b = try? CodecFrameTest.cfBinEncode( v )
            ret b
        }
        function bdec = function( object v )
        {
            ByteBuf b = v as ByteBuf
            object m = try? CodecFrameTest.cfBinDecode( b )
            ret m
        }
        BinaryCodec bc = BinaryCodec.of( benc, bdec )
        CodecFrameMsg em = CodecFrameMsg( 7, "bc" )
        ByteBuf eb = bc.encode( em )
        check( "E1 binary encode hex", eb.toHex() == "076263" )
        object edo = bc.decode( eb )
        CodecFrameMsg ed = edo as CodecFrameMsg
        check( "E2 binary decode", ed != null && ed.id == 7 && ed.name == "bc" )
    }

    # ======================================================================
    # F 组：JsonCodec（data 类型 JSON 文本往返）
    # ======================================================================
    static testGroupF()
    {
        global.println( "========== F: JsonCodec ==========" )

        JsonCodec<CodecUserData> jc = JsonCodec<CodecUserData>()
        CodecUserData ud = CodecUserData(){ uid = 7, name = "json", score = 1.5 }
        string txt = Serialize.toText<CodecUserData>( ud, jc )
        CodecUserData back = Serialize.fromText<CodecUserData>( txt, jc )

        check( "F1 json roundtrip", back != null && back.uid == 7 && back.name == "json" && back.score > 1.4 && back.score < 1.6 )
    }

    # ======================================================================
    # G 组：Serialize（toStream/fromStream + toElementStream）
    # ======================================================================
    static testGroupG()
    {
        global.println( "========== G: Serialize ==========" )

        ProtoCodec<CodecFrameMsg> codec = cfMakeCodec()
        CodecFrameMsg msg = CodecFrameMsg( 42, "rt" )

        # G1 toStream + fromStream（无帧头整存整取）
        var ms1 = MemoryStream()
        Task t1 = Serialize.toStream<CodecFrameMsg>( msg, ms1, codec )
        object r1 = Coroutine.awaitHandle( t1 )
        Task t2 = Serialize.fromStream<CodecFrameMsg>( ms1, codec )
        object r2 = Coroutine.awaitHandle( t2 )
        CodecFrameMsg g1back = r2 as CodecFrameMsg
        check( "G1 toStream/fromStream", g1back != null && g1back.id == 42 && g1back.name == "rt" )

        # G2 toElementStream（varint 分帧多元素流）
        var ms2 = MemoryStream()
        StreamSink<CodecFrameMsg> k2 = LengthPrefix.encodeSink<CodecFrameMsg>( ms2, codec )
        k2.add( CodecFrameMsg( 1, "e1" ) )
        k2.add( CodecFrameMsg( 2, "e2" ) )
        k2.close()
        Stream<CodecFrameMsg> s2 = Serialize.toElementStream<CodecFrameMsg>( ms2, codec )
        Task t3 = s2.toList()
        object r3 = Coroutine.awaitHandle( t3 )
        List<CodecFrameMsg> lst2 = r3 as List<CodecFrameMsg>
        int g2cnt = 0
        string g2names = ""
        if lst2 != null
        {
            for v in lst2
            {
                CodecFrameMsg m = v as CodecFrameMsg
                if m != null
                {
                    g2cnt = g2cnt + 1
                    g2names = g2names + m.name
                }
            }
        }
        check( "G2 toElementStream", g2cnt == 2 && g2names == "e1e2" )
    }

    # ======================================================================
    # H 组：bindStream chunked 解码（跨 chunk 重组 / 截断流报错）
    # ======================================================================
    static testGroupH()
    {
        global.println( "========== H: bindStream chunked ==========" )

        # H1 两帧 12 字节按 4 chunk 投递（边界切在帧中间），
        #    跨 chunk 重组出 msg(1,"a") + msg(1,"b")
        CodecFrameTest.g_hCount = 0
        CodecFrameTest.g_hNames = ""
        CodecFrameTest.g_hDone = false
        CodecFrameTest.g_hErr = false
        StreamController<ByteBuf> ctrl1 = StreamController<ByteBuf>()
        Stream<ByteBuf> cs1 = ctrl1.stream
        Stream<CodecFrameMsg> s1 = LengthPrefix.bindStream<CodecFrameMsg>( cs1, cfMakeCodec() )
        function onData1 = function( object v )
        {
            CodecFrameMsg m = v as CodecFrameMsg
            if m != null
            {
                CodecFrameTest.g_hCount = CodecFrameTest.g_hCount + 1
                CodecFrameTest.g_hNames = CodecFrameTest.g_hNames + m.name
            }
        }
        function onError1 = function( object e )
        {
            CodecFrameTest.g_hErr = true
        }
        function onDone1 = function()
        {
            CodecFrameTest.g_hDone = true
        }
        s1.listen( onData1, onError1, onDone1, false )
        ctrl1.add( ByteBuf.fromHex( "05" ) )
        ctrl1.add( ByteBuf.fromHex( "080112" ) )
        ctrl1.add( ByteBuf.fromHex( "01610508" ) )
        ctrl1.add( ByteBuf.fromHex( "01120162" ) )
        ctrl1.close()
        Coroutine.sleep( 50 )
        check( "H1 chunked decode", g_hCount == 2 && g_hNames == "ab" && g_hDone && !g_hErr )

        # H2 截断流：只喂半帧后上游 close → UnexpectedEof 走 onError
        CodecFrameTest.g_hCount2 = 0
        CodecFrameTest.g_hErr2 = false
        StreamController<ByteBuf> ctrl2 = StreamController<ByteBuf>()
        Stream<ByteBuf> cs2 = ctrl2.stream
        Stream<CodecFrameMsg> s2 = LengthPrefix.bindStream<CodecFrameMsg>( cs2, cfMakeCodec() )
        function onData2 = function( object v )
        {
            CodecFrameTest.g_hCount2 = CodecFrameTest.g_hCount2 + 1
        }
        function onError2 = function( object e )
        {
            CodecFrameTest.g_hErr2 = true
        }
        s2.listen( onData2, onError2, null, false )
        ctrl2.add( ByteBuf.fromHex( "05080112" ) )
        ctrl2.close()
        Coroutine.sleep( 50 )
        check( "H2 truncated eof", g_hErr2 && g_hCount2 == 0 )
    }

    # ======================================================================
    # I 组：Stream.periodic 定时流（周期生产 + 取消冻结）
    # ======================================================================
    static testGroupI()
    {
        global.println( "========== I: Stream.periodic ==========" )

        CodecFrameTest.g_tickCount = 0
        CodecFrameTest.g_tickLast = 0
        function tick = function()
        {
            CodecFrameTest.g_tickCount = CodecFrameTest.g_tickCount + 1
            ret CodecFrameTest.g_tickCount
        }
        function onTick = function( object v )
        {
            int x = v as int
            CodecFrameTest.g_tickLast = x
        }
        Stream<Int32> ps = Stream<Int32>.periodic( 10, tick )
        StreamSubscription sub = ps.listen( onTick )
        int spin = 0
        while CodecFrameTest.g_tickCount < 3 && spin < 200
        {
            Coroutine.sleep( 5 )
            spin = spin + 1
        }
        int snap = CodecFrameTest.g_tickCount
        sub.cancel()
        Coroutine.sleep( 60 )
        check( "I periodic cancel", snap >= 3 && g_tickCount == snap && g_tickLast >= 3 )
    }

    static fun()
    {
        global.println( "===== CodecFrameTest start =====" )
        testGroupA()
        testGroupB()
        testGroupC()
        testGroupD()
        testGroupE()
        testGroupF()
        testGroupG()
        testGroupH()
        testGroupI()
        global.println( "===== CodecFrameTest end =====" )
    }
}
