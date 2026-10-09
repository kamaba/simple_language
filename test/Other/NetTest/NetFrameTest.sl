import Std;
import Core;

# ============================================================================
# NetFrameTest.sl -- NET Phase 1 C 组：LengthPrefix 分帧验收测试
#
# 参考设计档：csimple_lang/md/design/NET_DESIGN.md §9（测试计划 C 组）。
#   C1  粘包：两帧拼一个缓冲一次写出，client 侧 tryReadFrame 逐帧还原
#   C2  半包：一帧分两段写出（writeBytes 消费语义切分），client 还原完整帧
#   C3  decodeStream<T> + BinaryCodec：流式解码（生产协程 + StreamIterator）
#
# 编写约定同 TcpBasicTest.sl（Net. 前缀 / sf 辅助 try? 中转 / 组方法裸写）。
# ============================================================================

NetFrameTest
{
    # ---------- 统一断言辅助 ----------
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[NetFrameTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[NetFrameTest] " + name + " : FAIL" )
        }
    }

    # ---------- sf 前缀 throws 辅助 ----------

    # raw echo：读到 EOF 把累积字节一次回写（C1/C2/C3 共用）
    static Int32 sfRawEcho( Net.TcpServer srv ) throws
    {
        Net.TcpStream c = srv.accept()
        while true
        {
            ByteBuffer buf = ByteBuffer( 256 )
            Int32 n = c.read( buf )
            if n <= 0
            {
                break
            }
            c.write( buf )
        }
        c.close()
        ret 1
    }

    # C3 编码：string -> 长度前缀帧
    static ByteBuffer sfEncFrame( object v ) throws
    {
        string s = v as string
        ByteBuffer payload = ByteBuffer( 64 )
        payload.writeString( s )
        ByteBuffer frame = ByteBuffer( 256 )
        LengthPrefix.writeFrame( frame, payload )
        ret frame
    }

    # C3 解码：帧 -> string
    static object sfDecFrame( ByteBuffer b ) throws
    {
        string s = b.readString( b.readableBytes )
        ret s
    }

    # C3 codec 工厂（闭包不能声明 throws -> try? 中转，返回值装箱语义）
    static BinaryCodec sfMakeCodec()
    {
        function enc = function( object v )
        {
            object r = try? NetFrameTest.sfEncFrame( v )
            ret r
        }
        function dec = function( object v )
        {
            ByteBuffer b = v as ByteBuffer
            object m = try? NetFrameTest.sfDecFrame( b )
            ret m
        }
        BinaryCodec k = BinaryCodec.of( enc, dec )
        ret k
    }

    # ---------- C 组用例 ----------

    static testGroupC()
    {
        Console.println( "========== C: LengthPrefix 分帧 ==========" )

        # ---- C1: 粘包（端口 19306） ----
        Net.TcpServer srv1 = Net.Tcp.listen( 19306 )
        function rawEchoFn1 = function()
        {
            object r = try? NetFrameTest.sfRawEcho( srv1 )
        }
        spawn rawEchoFn1()
        Net.TcpStream c1 = Net.Tcp.connect( "127.0.0.1", 19306 )
        # 两帧拼在一个 wire 缓冲，一次 write 全部发出
        ByteBuffer wire = ByteBuffer( 256 )
        ByteBuffer p1 = ByteBuffer( 64 )
        p1.writeString( "hello-1" )
        LengthPrefix.writeFrame( wire, p1 )
        ByteBuffer p2 = ByteBuffer( 64 )
        p2.writeString( "hello-2" )
        LengthPrefix.writeFrame( wire, p2 )
        c1.write( wire )
        ByteBuffer acc1 = ByteBuffer( 256 )
        Int32 frames = 0
        bool ok1 = false
        bool ok2 = false
        while frames < 2
        {
            ByteBuffer f = LengthPrefix.tryReadFrame( acc1, 1024 )
            if f != null
            {
                string s = f.readString( f.readableBytes )
                if frames == 0
                {
                    ok1 = s == "hello-1"
                }
                else
                {
                    ok2 = s == "hello-2"
                }
                frames = frames + 1
            }
            else
            {
                ByteBuffer b1 = ByteBuffer( 128 )
                Int32 n1 = c1.read( b1 )
                if n1 <= 0
                {
                    break
                }
                acc1.writeBytes( b1 )
            }
        }
        check( "C1 two frames from one write", frames == 2 && ok1 && ok2 )
        c1.close()
        srv1.close()

        # ---- C2: 半包（端口 19307） ----
        Net.TcpServer srv2 = Net.Tcp.listen( 19307 )
        function rawEchoFn2 = function()
        {
            object r = try? NetFrameTest.sfRawEcho( srv2 )
        }
        spawn rawEchoFn2()
        Net.TcpStream c2 = Net.Tcp.connect( "127.0.0.1", 19307 )
        string msg = "half-packet-frame"
        ByteBuffer payload = ByteBuffer( 64 )
        payload.writeString( msg )
        ByteBuffer frame = ByteBuffer( 256 )
        LengthPrefix.writeFrame( frame, payload )
        # 分两段：先 5 字节，间隔后剩余（writeBytes(src,len) 推进 src.readerIndex）
        ByteBuffer part1 = ByteBuffer( 256 )
        part1.writeBytes( frame, 5 )
        c2.write( part1 )
        Coroutine.delay( 100 )
        ByteBuffer part2 = ByteBuffer( 256 )
        part2.writeBytes( frame, frame.readableBytes )
        c2.write( part2 )
        # client 侧循环累积直到凑出完整帧
        ByteBuffer acc2 = ByteBuffer( 256 )
        ByteBuffer got2 = null
        while got2 == null
        {
            got2 = LengthPrefix.tryReadFrame( acc2, 1024 )
            if got2 == null
            {
                ByteBuffer b2 = ByteBuffer( 128 )
                Int32 n2 = c2.read( b2 )
                if n2 <= 0
                {
                    break
                }
                acc2.writeBytes( b2 )
            }
        }
        bool c2ok = false
        if got2 != null
        {
            string s2 = got2.readString( got2.readableBytes )
            c2ok = s2 == msg
        }
        check( "C2 frame split across two writes", c2ok )
        c2.close()
        srv2.close()

        # ---- C3: decodeStream + BinaryCodec（端口 19308） ----
        Net.TcpServer srv3 = Net.Tcp.listen( 19308 )
        function rawEchoFn3 = function()
        {
            object r = try? NetFrameTest.sfRawEcho( srv3 )
        }
        spawn rawEchoFn3()
        Net.TcpStream c3 = Net.Tcp.connect( "127.0.0.1", 19308 )
        BinaryCodec k = NetFrameTest.sfMakeCodec()
        Stream<object> ds = LengthPrefix.decodeStream<object>( c3, k )
        StreamIterator<object> it = StreamIterator<object>( ds )
        ByteBuffer f1 = NetFrameTest.sfEncFrame( "alpha" )
        c3.write( f1 )
        ByteBuffer f2 = NetFrameTest.sfEncFrame( "beta" )
        c3.write( f2 )
        bool m1 = it.moveNext()
        string s1 = ""
        if m1
        {
            s1 = it.current as string
        }
        bool m2 = it.moveNext()
        string s2 = ""
        if m2
        {
            s2 = it.current as string
        }
        c3.close()
        bool m3 = it.moveNext()
        check( "C3 decodeStream two frames", m1 && s1 == "alpha" && m2 && s2 == "beta" )
        check( "C3 decodeStream ends on close", m3 == false )
        srv3.close()
    }

    static fun()
    {
        Console.println( "===== NetFrameTest start =====" )
        testGroupC()
        Console.println( "===== NetFrameTest end =====" )
    }
}
