import Std;
import Core;

# ============================================================================
# StreamFileTest.sl -- P2 文件流 / stdout / 同岛桥接验收测试
#
# 参考设计档：md/design/STREAM_DESIGN.md §18（M / F 组 + StreamBridge G 组）。
# 测试对象（Std/IO + Std/Isolate，按测试路由落 ExpendTest/CSimpleVMStdTest）：
#   A  FileStream 读写往返（Create 写 → Read 读 / EOF）
#   B  FileStream seek 三 origin（Begin / Current / End）
#   C  FileMode 语义（Write 截断 / Append 追加 / Create 重建 / Read 失败）
#   D  File 流式与整文件 API 一致（writeAllBytes / readAll / readAllBytes）
#   E  ProtoCodec Memory/File 双载体一致（§18 H2）
#   F  StdOutStream（stdout 写 / 读拒绝 / 能力位）
#   G  StreamBridge 同岛桥接（fromIterable → pipeTo → fromReceivePort）
#
# 编写约定（沿用 IsolateTest.sl / FileTest 先例）：
#  1. 相对路径临时文件，测完 File.delete 清理。
#  2. 创建新文件一律 FileMode.Create（ReadWrite 映射 "r+b" 要求已存在）。
#  3. 抛错构造场景经静态方法（File.openRead）+ label{} try 语句 catch{}。
#  4. throws 辅助 sf 前缀静态方法，闭包经 try? 中转（闭包不能声明
#     throws）；SL 异常非受检，fun() 不声明 throws。
# ============================================================================

StreamFileMsg
{
    int id = 0
    string name = ""

    void _init_( int i, string n )
    {
        this.id = i
        this.name = n
    }
}

StreamFileTest
{
    # ---------- 统一断言辅助 ----------
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[StreamFileTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[StreamFileTest] " + name + " : FAIL" )
        }
    }

    # ---------- sf 前缀 throws 辅助 ----------

    static ByteBuf sfEncodeMsg( object v ) throws
    {
        StreamFileMsg m = v as StreamFileMsg
        var w = ProtocalBuffers.newWriter()
        w.writeInt32( 1, m.id )
        w.writeString( 2, m.name )
        ret ByteBuf.fromBytes( w.toBytes() )
    }

    static StreamFileMsg sfDecodeMsg( ByteBuf b ) throws
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
        ret StreamFileMsg( id, name )
    }

    static ProtoCodec<StreamFileMsg> sfMakeCodec()
    {
        function enc = function( object v )
        {
            object b = try? StreamFileTest.sfEncodeMsg( v )
            ret b
        }
        function dec = function( object v )
        {
            ByteBuf b = v as ByteBuf
            object m = try? StreamFileTest.sfDecodeMsg( b )
            ret m
        }
        var pc = ProtoCodec<StreamFileMsg>( enc, dec )
        ret pc
    }

    # ---------- 全局状态 ----------
    static int g_bridgeSum = 0
    static bool g_bridgeDone = false

    # ======================================================================
    # A 组：FileStream 读写往返
    # ======================================================================
    static testGroupA()
    {
        Console.println( "========== A: FileStream 读写往返 ==========" )

        string path = "stream_file_test_a.txt"
        var fw = FileStream( path, FileMode.Create )
        fw.write( ByteBuf.fromString( "hello file stream" ) )
        fw.flush()
        fw.flushToDisk()
        fw.close()

        FileStream fr = FileStream( path, FileMode.Read )
        check( "A1 length", fr.length == 17 )
        check( "A2 position", fr.position == 0 )
        var got = ByteBuf( 32 )
        Int32 n = fr.read( got )
        check( "A3 read", n == 17 && got.readString( got.readableBytes ) == "hello file stream" )
        var got2 = ByteBuf( 32 )
        Int32 n2 = fr.read( got2 )
        check( "A4 eof", n2 == 0 )
        fr.close()

        File.delete( path )
    }

    # ======================================================================
    # B 组：FileStream seek 三 origin
    # ======================================================================
    static testGroupB()
    {
        Console.println( "========== B: FileStream seek ==========" )

        string path = "stream_file_test_b.txt"
        File.writeAllText( path, "0123456789" )
        FileStream fs = FileStream( path, FileMode.Read )

        # Begin 4 → '4'
        Int64 p1 = fs.seek( 4, SeekOrigin.Begin )
        var b1 = fs.readExactly( 1 )
        check( "B1 seek begin", p1 == 4 && b1.readString( b1.readableBytes ) == "4" )

        # 读 1 字节后 position=5，Current +2 → 7 → '7'
        Int64 p2 = fs.seek( 2, SeekOrigin.Current )
        var b2 = fs.readExactly( 1 )
        check( "B2 seek current", p2 == 7 && b2.readString( b2.readableBytes ) == "7" )

        # End -3 → 10-3=7 → '7'
        Int64 p3 = fs.seek( -3, SeekOrigin.End )
        var b3 = fs.readExactly( 1 )
        check( "B3 seek end", p3 == 7 && b3.readString( b3.readableBytes ) == "7" )

        fs.close()
        File.delete( path )
    }

    # ======================================================================
    # C 组：FileMode 语义
    # ======================================================================
    static testGroupC()
    {
        Console.println( "========== C: FileMode 语义 ==========" )

        string path = "stream_file_test_c.txt"
        File.writeAllText( path, "abcdef" )

        # Write 截断
        var f1 = FileStream( path, FileMode.Write )
        f1.write( ByteBuf.fromString( "ab" ) )
        f1.close()
        check( "C1 write truncate", File.readAllText( path ) == "ab" )

        # Append 追加
        var f2 = FileStream( path, FileMode.Append )
        f2.write( ByteBuf.fromString( "cd" ) )
        f2.close()
        check( "C2 append", File.readAllText( path ) == "abcd" )

        # Create 截断重建
        var f3 = FileStream( path, FileMode.Create )
        f3.write( ByteBuf.fromString( "XY" ) )
        f3.close()
        check( "C3 create recreate", File.readAllText( path ) == "XY" )

        # Read 打开不存在文件 → OpenFailed
        bool c4 = false
        label labC4
        {
            try File.openRead( "stream_file_test_no_such.txt" )
        }
        catch
        {
            c4 = true
        }
        check( "C4 open failed", c4 )

        File.delete( path )
    }

    # ======================================================================
    # D 组：File 流式与整文件 API 一致
    # ======================================================================
    static testGroupD()
    {
        Console.println( "========== D: File 整文件 API ==========" )

        string path = "stream_file_test_d.txt"
        UInt8[] dead = Array<UInt8>.create( 4 )
        dead[0] = 222
        dead[1] = 173
        dead[2] = 190
        dead[3] = 239
        File.writeAllBytes( path, dead )
        check( "D1 size", File.getSize( path ) == 4 )

        FileStream fr = File.openRead( path )
        var all = fr.readAll( 0 )
        check( "D2 readAll hex", all.toHex() == "deadbeef" )
        fr.close()

        UInt8Array back = File.readAllBytes( path )
        check( "D3 readAllBytes", back != null && back.length == 4 && back._getItem_( 0 ) == 222 && back._getItem_( 3 ) == 239 )

        File.delete( path )
    }

    # ======================================================================
    # E 组：ProtoCodec Memory/File 双载体一致（§18 H2）
    # ======================================================================
    static testGroupE()
    {
        Console.println( "========== E: ProtoCodec 双载体 ==========" )

        string path = "stream_file_test_e.txt"
        ProtoCodec<StreamFileMsg> codec = sfMakeCodec()

        # Memory 载体
        var ms = MemoryStream()
        StreamSink<StreamFileMsg> km = LengthPrefix.encodeSink<StreamFileMsg>( ms, codec )
        km.add( StreamFileMsg( 9, "file-codec" ) )
        km.close()
        string memHex = ms.toByteBuf().toHex()

        # File 载体
        var fw = FileStream( path, FileMode.Create )
        StreamSink<StreamFileMsg> kf = LengthPrefix.encodeSink<StreamFileMsg>( fw, codec )
        kf.add( StreamFileMsg( 9, "file-codec" ) )
        kf.close()
        fw.close()
        string fileHex = ByteBuf.fromBytes( File.readAllBytes( path ) ).toHex()
        check( "E1 dual hex", memHex == fileHex )

        # File 读回 decodeStream
        FileStream fr = FileStream( path, FileMode.Read )
        Stream<StreamFileMsg> sr = LengthPrefix.decodeStream<StreamFileMsg>( fr, codec )
        Task t = sr.toListThenTask()
        object r = Coroutine.awaitTask( t )
        List<StreamFileMsg> lst = r as List<StreamFileMsg>
        int ecnt = 0
        int eid = 0
        string ename = ""
        if lst != null
        {
            for v in lst
            {
                StreamFileMsg m = v as StreamFileMsg
                if m != null
                {
                    ecnt = ecnt + 1
                    eid = m.id
                    ename = m.name
                }
            }
        }
        check( "E2 file decode", ecnt == 1 && eid == 9 && ename == "file-codec" )
        fr.close()

        File.delete( path )
    }

    # ======================================================================
    # F 组：StdOutStream（stdout 写 / 读拒绝 / 能力位）
    # ======================================================================
    static testGroupF()
    {
        Console.println( "========== F: StdOutStream ==========" )

        StdOutStream so = StdOutStream.shared()
        so.write( ByteBuf.fromString( "[StreamFileTest] F1 stdout write" ) )
        so.write( ByteBuf.fromString( "\n" ) )

        bool f2 = false
        label labF2
        {
            try so.read( ByteBuf( 16 ) )
        }
        catch
        {
            f2 = true
        }
        check( "F2 read reject", f2 )

        check( "F3 capability", so.canWrite && so.canRead == false && so.canSeek == false )
    }

    # ======================================================================
    # G 组：StreamBridge 同岛桥接
    # ======================================================================
    static testGroupG()
    {
        Console.println( "========== G: StreamBridge 同岛 ==========" )

        StreamFileTest.g_bridgeSum = 0
        StreamFileTest.g_bridgeDone = false

        Array<Int32> arr = Array<Int32>.create( 4 )
        arr._setItem_( 0, 10 )
        arr._setItem_( 1, 20 )
        arr._setItem_( 2, 30 )
        arr._setItem_( 3, 40 )
        Stream<Int32> src = Stream<Int32>.fromIterable( arr )

        ReceivePort rp = ReceivePort()
        Task pump = StreamBridge.pipeTo<Int32>( src, rp.sendPort )
        Stream<object> bs = StreamBridge.fromReceivePort( rp )
        function onB = function( object v )
        {
            int x = v as int
            StreamFileTest.g_bridgeSum = StreamFileTest.g_bridgeSum + x
        }
        function onBd = function()
        {
            StreamFileTest.g_bridgeDone = true
        }
        bs.listen( onB, null, onBd, false )
        object pr = Coroutine.awaitTask( pump )
        Coroutine.delay( 50 )
        check( "G bridge roundtrip", g_bridgeSum == 100 && g_bridgeDone )
    }

    static fun()
    {
        Console.println( "===== StreamFileTest start =====" )
        testGroupA()
        testGroupB()
        testGroupC()
        testGroupD()
        testGroupE()
        testGroupF()
        testGroupG()
        Console.println( "===== StreamFileTest end =====" )
    }
}
