# ============================================================================
# test/ExpendTest/Base64Test.sl — Std.Encoding.Base64 RFC 4648 编码冒烟测试
# 实现：csimple_lang/src/core/sl_core_base64.c（表驱动标准字母表）
#
# 覆盖分组：
#   A bound         encodeBound 数值（((n+2)/3)*4 向上取整）
#   B vectors       RFC 4648 §10 官方测试向量 + 二进制/空输入往返
#   C corrupt       长度非 4 倍数 / 非法字符 / 填充位置不合法 → 异常
#   D released      释放后的源 → 异常
#   E overloads     string / UInt8Array 入口 + encodeToString / decodeString /
#                   decodeBytes 出口
#   F stream        ByteStream 桥接（encode(ByteStream)/encodeTo/decodeTo）
# ============================================================================

import Std;

Base64Test
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[Base64Test] " + name + " : OK" )
        }
        else
        {
            Console.println( "[Base64Test] " + name + " : FAIL" )
        }
    }

    # ── 异常注入辅助（throws 单动作，供 label/catch 捕获） ──

    static base64Encode( ByteBuffer src ) throws
    {
        var encoded = Base64.encode( src )
        encoded.release()
    }

    static base64Decode( ByteBuffer src ) throws
    {
        var back = Base64.decode( src )
        back.release()
    }

    # ── A：encodeBound ──

    static testBound()
    {
        # base64 bound = ((n + 2) / 3) * 4
        check( "b64 bound empty", Base64.encodeBound( 0 ) == 0 )
        check( "b64 bound 1", Base64.encodeBound( 1 ) == 4 )
        check( "b64 bound 2", Base64.encodeBound( 2 ) == 4 )
        check( "b64 bound 3", Base64.encodeBound( 3 ) == 4 )
        check( "b64 bound 4", Base64.encodeBound( 4 ) == 8 )
        check( "b64 bound 255", Base64.encodeBound( 255 ) == 340 )
        check( "b64 bound negative", Base64.encodeBound( -1 ) == 0 )
    }

    # ── B：RFC 4648 官方向量 + 往返 ──

    static testVectors() throws
    {
        # RFC 4648 §10 官方测试向量（文本到文本）
        check( "b64 vector empty", Base64.encodeToString( "" ) == "" )
        check( "b64 vector f", Base64.encodeToString( "f" ) == "Zg==" )
        check( "b64 vector fo", Base64.encodeToString( "fo" ) == "Zm8=" )
        check( "b64 vector foo", Base64.encodeToString( "foo" ) == "Zm9v" )
        check( "b64 vector foob", Base64.encodeToString( "foob" ) == "Zm9vYg==" )
        check( "b64 vector fooba", Base64.encodeToString( "fooba" ) == "Zm9vYmE=" )
        check( "b64 vector foobar", Base64.encodeToString( "foobar" ) == "Zm9vYmFy" )

        # 官方向量反向解码（含 padding 与无 padding）
        check( "b64 decode vector", Base64.decodeString( "Zm9vYmFy" ) == "foobar" )
        check( "b64 decode vector padded", Base64.decodeString( "Zm9vYg==" ) == "foob" )
        check( "b64 decode vector double pad", Base64.decodeString( "Zg==" ) == "f" )
        check( "b64 decode vector empty", Base64.decodeString( "" ) == "" )

        # 二进制往返：22 字节高熵数据（不依赖文本编码语义）
        var noiseHex = "0ff1ceab2d9e37415566788a9bbccddeef0a12345678"
        var noise = ByteBuffer.fromHex( noiseHex )
        var encoded = Base64.encode( noise )
        check( "b64 binary encoded length", encoded.readableBytes == 32 )
        var back = Base64.decode( encoded )
        check( "b64 binary roundtrip", back.toHex() == noiseHex )
        check( "b64 src index untouched", noise.readerIndex == 0 )
        noise.release()
        encoded.release()
        back.release()

        # 空输入：encode 产物为空缓冲（合法），decode 还原为空
        var empty = ByteBuffer()
        var eencoded = Base64.encode( empty )
        check( "b64 empty encoded size", eencoded.readableBytes == 0 )
        var eback = Base64.decode( eencoded )
        check( "b64 empty roundtrip", eback.readableBytes == 0 )
        empty.release()
        eencoded.release()
        eback.release()
    }

    # ── C / D：损坏文本与释放源 ──

    static testCorruptAndReleased() throws
    {
        # 损坏文本：长度非 4 倍数
        var odd = ByteBuffer.fromString( "Zg=" )
        var caught = 0
        label b64OddBlock
        {
            try base64Decode( odd )
        }
        catch
        {
            caught = 1
        }
        check( "b64 odd length fails", caught == 1 )
        odd.release()

        # 损坏文本：字符集外字符（'*' 不在标准字母表）
        var bad = ByteBuffer.fromString( "Zm*v" )
        caught = 0
        label b64BadCharBlock
        {
            try base64Decode( bad )
        }
        catch
        {
            caught = 1
        }
        check( "b64 bad char fails", caught == 1 )
        bad.release()

        # 损坏文本：'=' 填充后又出现数据字符（非规范文本）
        var padMid = ByteBuffer.fromString( "Zg==Zg==" )
        caught = 0
        label b64PadMidBlock
        {
            try base64Decode( padMid )
        }
        catch
        {
            caught = 1
        }
        check( "b64 pad mid data fails", caught == 1 )
        padMid.release()

        # 损坏文本：整 quad 全是 '='（填充多于 2 个）
        var fourPads = ByteBuffer.fromString( "====" )
        caught = 0
        label b64FourPadsBlock
        {
            try base64Decode( fourPads )
        }
        catch
        {
            caught = 1
        }
        check( "b64 four pads fails", caught == 1 )
        fourPads.release()

        # 释放后的源 → 异常（编码侧）
        var gone = ByteBuffer( 4 )
        gone.writeU8( 1 )
        gone.release()
        caught = 0
        label b64GoneBlock
        {
            try base64Encode( gone )
        }
        catch
        {
            caught = 1
        }
        check( "b64 released source fails", caught == 1 )
    }

    # ── E：string / UInt8Array 重载与输出适配 ──

    static testOverloads() throws
    {
        # string 入口 + decodeString(ByteBuffer) 出口（25 字节文本 → 36 字符）
        var text = "hello base64, Hello world"
        var packed = Base64.encode( text )
        check( "b64 string encode length", packed.readableBytes == 36 )
        var back = Base64.decodeString( packed )
        check( "b64 string roundtrip", back == text )
        packed.release()

        # UInt8Array 入口 + decodeBytes 出口
        UInt8Array arr = [0, 1, 2, 3, 250, 251, 252]
        var apacked = Base64.encode( arr )
        var abytes = Base64.decodeBytes( apacked )
        check( "b64 bytes roundtrip length", abytes.length == 7 )
        check( "b64 bytes roundtrip first", abytes[0] == 0 )
        check( "b64 bytes roundtrip last", abytes[6] == 252 )
        apacked.release()

        # encodeToString(ByteBuffer) 与 decodeString(string) 快捷入口
        var b = ByteBuffer.fromString( "Man" )
        check( "b64 buf encodeToString", Base64.encodeToString( b ) == "TWFu" )
        b.release()
        check( "b64 string decodeString", Base64.decodeString( "TWFu" ) == "Man" )
    }

    # ── F：ByteStream 桥接 ──

    static testStream() throws
    {
        var text = "base64 stream bridge sample 0123456789"

        # encode(ByteStream)：源流读到 EOF 再编码
        var srcBuf = ByteBuffer.fromString( text )
        var mem = ByteStream.wrapReadOnly( srcBuf )
        var encoded = Base64.encode( mem )
        var back = Base64.decodeString( encoded )
        check( "b64 stream encode roundtrip", back == text )
        srcBuf.release()
        encoded.release()
        mem.close()

        # encodeTo / decodeTo：结果写入目标流（数据归属目标流）
        var src = ByteBuffer.fromString( text )
        var dst = ByteStream.memory()
        Base64.encodeTo( dst, src )
        var b64 = dst.readAll()
        var plain = ByteStream.memory()
        Base64.decodeTo( plain, b64 )
        var back2 = plain.readAll()
        check( "b64 stream pipe roundtrip", back2.toString() == text )
        src.release()
        b64.release()
        back2.release()
        dst.close()
        plain.close()
    }

    static fun()
    {
        Console.println( "===== Base64Test =====" )
        testBound()
        testVectors()
        testCorruptAndReleased()
        testOverloads()
        testStream()
        Console.println( "[Base64Test] all groups done" )
    }
}
