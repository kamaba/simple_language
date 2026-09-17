# ============================================================================
# test/ExpendTest/ZlibTest.sl — Std.Zip.Zlib DEFLATE 压缩冒烟测试
# 实现：csimple_lang/src/core/sl_core_zlib.c（third_party/zlib 1.3.2）
#
# 覆盖分组：
#   A bound         compressBound 数值（4 字节头 + zlib 上界）
#   B roundtrip     可压缩文本 / 不可压缩高熵 / 空容器的压缩解压往返
#   C corrupt       短于 4 字节头 / 头声明长度与实际流不符 → 异常
#   D released      释放后的源 → 异常
# ============================================================================

import Std;

ZlibTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[ZlibTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[ZlibTest] " + name + " : FAIL" )
        }
    }

    # ── 异常注入辅助（throws 单动作，供 label/catch 捕获） ──

    static zlibCompress( ByteBuf src ) throws
    {
        var packed = Zlib.compress( src )
        packed.release()
    }

    static zlibDecompress( ByteBuf src ) throws
    {
        var back = Zlib.decompress( src )
        back.release()
    }

    # ── A：compressBound ──

    static testBound()
    {
        # zlib bound = n + n>>12 + n>>14 + n>>25 + 13，再加 4 字节容器头
        check( "zlib bound empty", Zlib.compressBound( 0 ) == 17 )
        check( "zlib bound 255", Zlib.compressBound( 255 ) == 272 )
        check( "zlib bound 4096", Zlib.compressBound( 4096 ) == 4114 )
        check( "zlib bound negative", Zlib.compressBound( -1 ) == 0 )
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
        var packed = Zlib.compress( src )
        check( "zlib compressible shrinks", packed.readableBytes < 512 )
        var back = Zlib.decompress( packed )
        check( "zlib compressible roundtrip", back.toString() == text )
        check( "zlib src index untouched", src.readerIndex == 0 )
        src.release()
        packed.release()
        back.release()

        # 不可压缩往返：22 字节高熵数据（只保证往返，不保证变小）
        var noiseHex = "0ff1ceab2d9e37415566788a9bbccddeef0a12345678"
        var noise = ByteBuf.fromHex( noiseHex )
        var npacked = Zlib.compress( noise )
        check( "zlib incompressible no shrink", npacked.readableBytes >= 26 )
        var nback = Zlib.decompress( npacked )
        check( "zlib incompressible roundtrip", nback.toHex() == noiseHex )
        noise.release()
        npacked.release()
        nback.release()

        # 空容器：4 字节零长度头 + zlib 空流（8 字节）
        var empty = ByteBuf()
        var epacked = Zlib.compress( empty )
        check( "zlib empty container size", epacked.readableBytes == 12 )
        var eback = Zlib.decompress( epacked )
        check( "zlib empty roundtrip", eback.readableBytes == 0 )
        empty.release()
        epacked.release()
        eback.release()
    }

    # ── C / D：损坏数据与释放源 ──

    static testCorruptAndReleased() throws
    {
        # 损坏数据：短于 4 字节头
        var shortBuf = ByteBuf.fromHex( "0102" )
        var caught = 0
        label zlibShortBlock
        {
            try zlibDecompress( shortBuf )
        }
        catch
        {
            caught = 1
        }
        check( "zlib short header fails", caught == 1 )
        shortBuf.release()

        # 损坏数据：头声明 100 字节但流是坏的
        var lieBuf = ByteBuf.fromHex( "6400000000" )
        caught = 0
        label zlibLieBlock
        {
            try zlibDecompress( lieBuf )
        }
        catch
        {
            caught = 1
        }
        check( "zlib lying length fails", caught == 1 )
        lieBuf.release()

        # 释放后的源 → 异常
        var gone = ByteBuf( 4 )
        gone.writeU8( 1 )
        gone.release()
        caught = 0
        label zlibGoneBlock
        {
            try zlibCompress( gone )
        }
        catch
        {
            caught = 1
        }
        check( "zlib released source fails", caught == 1 )
    }

    # ── E：string / UInt8Array 重载 ──

    static testOverloads() throws
    {
        # string 入口 + decompressString 出口
        var text = "hello zlib, Hello world"
        var packed = Zlib.compress( text )
        check( "zlib string compress shrinks", packed.readableBytes < 32 )
        var back = Zlib.decompressString( packed )
        check( "zlib string roundtrip", back == text )
        packed.release()

        # UInt8Array 入口 + decompressBytes 出口
        UInt8Array arr = [0, 1, 2, 3, 250, 251, 252]
        var apacked = Zlib.compress( arr )
        var abytes = Zlib.decompressBytes( apacked )
        check( "zlib bytes roundtrip length", abytes.length == 7 )
        check( "zlib bytes roundtrip first", abytes[0] == 0 )
        check( "zlib bytes roundtrip last", abytes[6] == 252 )
        apacked.release()
    }

    static fun()
    {
        Console.println( "===== ZlibTest =====" )
        testBound()
        testRoundtrip()
        testCorruptAndReleased()
        testOverloads()
        Console.println( "[ZlibTest] all groups done" )
    }
}
