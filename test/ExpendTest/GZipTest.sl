# ============================================================================
# test/ExpendTest/GZipTest.sl — Std.Zip.GZip RFC 1952 压缩冒烟测试
# 实现：csimple_lang/src/core/sl_core_gzip.c（third_party/zlib 1.3.2）
#
# 覆盖分组：
#   A bound         compressBound 数值（RFC 1952 头尾 + DEFLATE 上界）
#   B roundtrip     可压缩文本 / 不可压缩高熵 / 空输入的压缩解压往返 + magic
#   C corrupt       短于最小流 / 坏头部 / 尾部 ISIZE 不符 → 异常
#   D released      释放后的源 → 异常
#   E overloads     string / UInt8Array 入口
#   F stream        ByteStream 桥接（compress(ByteStream)/compressTo/decompressTo）
# ============================================================================

import Std;

GZipTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[GZipTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[GZipTest] " + name + " : FAIL" )
        }
    }

    # ── 异常注入辅助（throws 单动作，供 label/catch 捕获） ──

    static gzipCompress( ByteBuf src ) throws
    {
        var packed = GZip.compress( src )
        packed.release()
    }

    static gzipDecompress( ByteBuf src ) throws
    {
        var back = GZip.decompress( src )
        back.release()
    }

    # ── A：compressBound ──

    static testBound()
    {
        # gzip bound = n + (n>>12) + (n>>14) + (n>>25) + 25
        check( "gzip bound empty", GZip.compressBound( 0 ) == 25 )
        check( "gzip bound 255", GZip.compressBound( 255 ) == 280 )
        check( "gzip bound 4096", GZip.compressBound( 4096 ) == 4122 )
        check( "gzip bound negative", GZip.compressBound( -1 ) == 0 )
    }

    # ── B：往返 ──

    static testRoundtrip() throws
    {
        # 可压缩往返：512 字节重复文本
        var text = ""
        for Int32 i = 0, i < 64, i = i + 1
        {
            text = text + "abcdefgh"
        }
        var src = ByteBuf.fromString( text )
        var packed = GZip.compress( src )
        check( "gzip compressible shrinks", packed.readableBytes < 512 )
        var back = GZip.decompress( packed )
        check( "gzip compressible roundtrip", back.toString() == text )
        check( "gzip src index untouched", src.readerIndex == 0 )
        # RFC 1952 magic：1f 8b（放最后读，避免推进 readerIndex 影响解压）
        var m0 = packed.readU8()
        var m1 = packed.readU8()
        check( "gzip rfc1952 magic", m0 == 31 && m1 == 139 )
        src.release()
        packed.release()
        back.release()

        # 不可压缩往返：22 字节高熵数据（只保证往返，不保证变小）
        var noiseHex = "0ff1ceab2d9e37415566788a9bbccddeef0a12345678"
        var noise = ByteBuf.fromHex( noiseHex )
        var npacked = GZip.compress( noise )
        check( "gzip incompressible no shrink", npacked.readableBytes >= 22 )
        var nback = GZip.decompress( npacked )
        check( "gzip incompressible roundtrip", nback.toHex() == noiseHex )
        noise.release()
        npacked.release()
        nback.release()

        # 空输入：仍是完整 gzip 流（10 字节头 + 8 字节尾 ≥ 18）
        var empty = ByteBuf()
        var epacked = GZip.compress( empty )
        check( "gzip empty stream size", epacked.readableBytes >= 18 )
        var eback = GZip.decompress( epacked )
        check( "gzip empty roundtrip", eback.readableBytes == 0 )
        empty.release()
        epacked.release()
        eback.release()
    }

    # ── C / D：损坏数据与释放源 ──

    static testCorruptAndReleased() throws
    {
        # 损坏数据：短于最小 gzip 流（18 字节头尾）
        var shortBuf = ByteBuf.fromHex( "1f8b08000000000000" )
        var caught = 0
        label gzipShortBlock
        {
            try gzipDecompress( shortBuf )
        }
        catch
        {
            caught = 1
        }
        check( "gzip short stream fails", caught == 1 )
        shortBuf.release()

        # 损坏数据：坏头部（magic 不是 1f 8b）
        var junk = ByteBuf.fromHex( "0000000000000000000000000000000000000000" )
        caught = 0
        label gzipJunkBlock
        {
            try gzipDecompress( junk )
        }
        catch
        {
            caught = 1
        }
        check( "gzip junk header fails", caught == 1 )
        junk.release()

        # 损坏数据：尾部 ISIZE 篡改（解压长度与 trailer 声明不符）
        var src = ByteBuf.fromString( "gzip isize mismatch probe" )
        var packed = GZip.compress( src )
        var raw = packed.toArray()
        var last = raw.length - 1
        if raw[last] == 7
        {
            raw[last] = 8
        }
        else
        {
            raw[last] = 7
        }
        var lie = ByteBuf.fromBytes( raw )
        caught = 0
        label gzipLieBlock
        {
            try gzipDecompress( lie )
        }
        catch
        {
            caught = 1
        }
        check( "gzip isize mismatch fails", caught == 1 )
        src.release()
        packed.release()
        lie.release()

        # 释放后的源 → 异常
        var gone = ByteBuf( 4 )
        gone.writeU8( 1 )
        gone.release()
        caught = 0
        label gzipGoneBlock
        {
            try gzipCompress( gone )
        }
        catch
        {
            caught = 1
        }
        check( "gzip released source fails", caught == 1 )
    }

    # ── E：string / UInt8Array 重载 ──

    static testOverloads() throws
    {
        # string 入口 + decompressString 出口
        # gzip 有 18 字节固定头尾（RFC 1952），22 字节文本产物约 38~40
        var text = "hello gzip, Hello world"
        var packed = GZip.compress( text )
        check( "gzip string compress shrinks", packed.readableBytes < 48 )
        var back = GZip.decompressString( packed )
        check( "gzip string roundtrip", back == text )
        packed.release()

        # UInt8Array 入口 + decompressBytes 出口
        UInt8Array arr = [0, 1, 2, 3, 250, 251, 252]
        var apacked = GZip.compress( arr )
        var abytes = GZip.decompressBytes( apacked )
        check( "gzip bytes roundtrip length", abytes.length == 7 )
        check( "gzip bytes roundtrip first", abytes[0] == 0 )
        check( "gzip bytes roundtrip last", abytes[6] == 252 )
        apacked.release()
    }

    # ── F：ByteStream 桥接 ──

    static testStream() throws
    {
        var text = "gzip stream bridge sample data 0123456789"

        # compress(ByteStream)：源流读到 EOF 再压缩
        var srcBuf = ByteBuf.fromString( text )
        var mem = ByteStream.wrapReadOnly( srcBuf )
        var packed = GZip.compress( mem )
        var back = GZip.decompressString( packed )
        check( "gzip stream compress roundtrip", back == text )
        srcBuf.release()
        packed.release()
        mem.close()

        # compressTo / decompressTo：结果写入目标流（数据归属目标流）
        var src = ByteBuf.fromString( text )
        var dst = ByteStream.memory()
        GZip.compressTo( dst, src )
        var gz = dst.readAll()
        var plain = ByteStream.memory()
        GZip.decompressTo( plain, gz )
        var back2 = plain.readAll()
        check( "gzip stream pipe roundtrip", back2.toString() == text )
        src.release()
        gz.release()
        back2.release()
        dst.close()
        plain.close()
    }

    static fun()
    {
        Console.println( "===== GZipTest =====" )
        testBound()
        testRoundtrip()
        testCorruptAndReleased()
        testOverloads()
        testStream()
        Console.println( "[GZipTest] all groups done" )
    }
}
