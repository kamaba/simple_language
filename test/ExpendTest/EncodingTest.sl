# ============================================================================
# test/ExpendTest/EncodingTest.sl — Std.Encoding 字符编码转换冒烟测试
# 实现：csimple_lang/src/core/sl_core_encoding.c（GB2312 查表
# sl_core_gb2312_table.c）；SL 层 Lib/Std/Encoding/Encoding.sl
#
# 覆盖分组：
#   A bound         maxByteCount / maxCharCount 数值（最坏情况 + 负数）
#   B code          codeFromName / nameFromCode / hasBom（12 编码往返）
#   C vectors       "中"(U+4E2D) 六种表示 + 代理对往返 + 空输入
#   D bom           encodeBom 前缀 / decode 剥离 / 反向 BOM 翻转端序 /
#                   preamble 字节
#   E corrupt       非法 UTF-8 / 未配对代理 / UTF-32 越界 / GB2312 空位与
#                   越界 trail / ASCII 高位字节 / 表外字符编码失败
#   F released      释放后的源 → 异常
#   G convenience   Utf8 / Unicode / BigEndianUnicode / Gb2312 / Ascii 便捷类
#   H convert       跨编码转换（GB2312 ↔ UTF-16LE / ASCII → UTF-32BE）
# ============================================================================

import Std;

EncodingTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[EncodingTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[EncodingTest] " + name + " : FAIL" )
        }
    }

    # ── 异常注入辅助（throws 单动作，供 label/catch 捕获） ──

    static encEncodeStr( EncodingCode code, string text ) throws
    {
        var out = Encoding.encode( code, text )
        out.release()
    }

    static encEncodeBuf( EncodingCode code, ByteBuffer src ) throws
    {
        var out = Encoding.encode( code, src )
        out.release()
    }

    static encDecode( EncodingCode code, ByteBuffer src ) throws
    {
        var text = Encoding.decode( code, src )
    }

    static encCodeFromName( string name ) throws
    {
        var code = Encoding.codeFromName( name )
    }

    # 通用断言辅助：hex 输入 decode 应抛异常
    static expectDecodeFail( string name, EncodingCode code, string hex ) throws
    {
        var src = ByteBuffer.fromHex( hex )
        var caught = 0
        label encDecodeFailBlock
        {
            try encDecode( code, src )
        }
        catch
        {
            caught = 1
        }
        check( name, caught == 1 )
        src.release()
    }

    # 通用断言辅助：string 输入 encode 应抛异常（目标字符集无法表示）
    static expectEncodeStrFail( string name, EncodingCode code, string text ) throws
    {
        var caught = 0
        label encEncodeStrFailBlock
        {
            try encEncodeStr( code, text )
        }
        catch
        {
            caught = 1
        }
        check( name, caught == 1 )
    }

    # 通用断言辅助：hex 输入 encode 应抛异常（源字节非法 UTF-8）
    static expectEncodeBufFail( string name, EncodingCode code, string hex ) throws
    {
        var src = ByteBuffer.fromHex( hex )
        var caught = 0
        label encEncodeBufFailBlock
        {
            try encEncodeBuf( code, src )
        }
        catch
        {
            caught = 1
        }
        check( name, caught == 1 )
        src.release()
    }

    # ── A：maxByteCount / maxCharCount ──

    static testBound()
    {
        # encode bound（UTF-8 输入最坏情况 + BOM 前缀；负数 → 0）
        check( "enc mbc utf8 10", Encoding.maxByteCount( EncodingCode.Utf8, 10 ) == 10 )
        check( "enc mbc utf8bom 10", Encoding.maxByteCount( EncodingCode.Utf8Bom, 10 ) == 13 )
        check( "enc mbc utf16le 10", Encoding.maxByteCount( EncodingCode.Utf16LE, 10 ) == 20 )
        check( "enc mbc utf16lebom 10", Encoding.maxByteCount( EncodingCode.Utf16LEBom, 10 ) == 22 )
        check( "enc mbc utf16be 4", Encoding.maxByteCount( EncodingCode.Utf16BE, 4 ) == 8 )
        check( "enc mbc utf32le 10", Encoding.maxByteCount( EncodingCode.Utf32LE, 10 ) == 40 )
        check( "enc mbc utf32lebom 10", Encoding.maxByteCount( EncodingCode.Utf32LEBom, 10 ) == 44 )
        check( "enc mbc gb2312 10", Encoding.maxByteCount( EncodingCode.Gb2312, 10 ) == 10 )
        check( "enc mbc ascii 10", Encoding.maxByteCount( EncodingCode.Ascii, 10 ) == 10 )
        check( "enc mbc negative", Encoding.maxByteCount( EncodingCode.Utf8, -1 ) == 0 )

        # decode bound（目标编码输入最坏情况；BOM 不产生输出）
        check( "enc mcc utf8 10", Encoding.maxCharCount( EncodingCode.Utf8, 10 ) == 10 )
        check( "enc mcc utf16le 10", Encoding.maxCharCount( EncodingCode.Utf16LE, 10 ) == 15 )
        check( "enc mcc utf16le 11", Encoding.maxCharCount( EncodingCode.Utf16LE, 11 ) == 18 )
        check( "enc mcc gb2312 10", Encoding.maxCharCount( EncodingCode.Gb2312, 10 ) == 15 )
        check( "enc mcc utf32le 8", Encoding.maxCharCount( EncodingCode.Utf32LE, 8 ) == 8 )
        check( "enc mcc ascii 10", Encoding.maxCharCount( EncodingCode.Ascii, 10 ) == 10 )
        check( "enc mcc negative", Encoding.maxCharCount( EncodingCode.Utf8, -1 ) == 0 )
    }

    # ── B：编码名 / BOM 标志 ──

    static testCode() throws
    {
        # 12 个编码名与代码往返（nameFromCode ∘ codeFromName = 恒等）
        check( "enc name utf8", Encoding.nameFromCode( Encoding.codeFromName( "utf-8" ) ) == "utf-8" )
        check( "enc name utf8bom", Encoding.nameFromCode( Encoding.codeFromName( "utf-8-bom" ) ) == "utf-8-bom" )
        check( "enc name utf16le", Encoding.nameFromCode( Encoding.codeFromName( "utf-16le" ) ) == "utf-16le" )
        check( "enc name utf16lebom", Encoding.nameFromCode( Encoding.codeFromName( "utf-16le-bom" ) ) == "utf-16le-bom" )
        check( "enc name utf16be", Encoding.nameFromCode( Encoding.codeFromName( "utf-16be" ) ) == "utf-16be" )
        check( "enc name utf16bebom", Encoding.nameFromCode( Encoding.codeFromName( "utf-16be-bom" ) ) == "utf-16be-bom" )
        check( "enc name utf32le", Encoding.nameFromCode( Encoding.codeFromName( "utf-32le" ) ) == "utf-32le" )
        check( "enc name utf32lebom", Encoding.nameFromCode( Encoding.codeFromName( "utf-32le-bom" ) ) == "utf-32le-bom" )
        check( "enc name utf32be", Encoding.nameFromCode( Encoding.codeFromName( "utf-32be" ) ) == "utf-32be" )
        check( "enc name utf32bebom", Encoding.nameFromCode( Encoding.codeFromName( "utf-32be-bom" ) ) == "utf-32be-bom" )
        check( "enc name gb2312", Encoding.nameFromCode( Encoding.codeFromName( "gb2312" ) ) == "gb2312" )
        check( "enc name ascii", Encoding.nameFromCode( Encoding.codeFromName( "ascii" ) ) == "ascii" )

        # hasBom：仅 *_BOM 系为 true
        check( "enc hasbom utf8", Encoding.hasBom( EncodingCode.Utf8 ) == false )
        check( "enc hasbom utf8bom", Encoding.hasBom( EncodingCode.Utf8Bom ) == true )
        check( "enc hasbom utf16le", Encoding.hasBom( EncodingCode.Utf16LE ) == false )
        check( "enc hasbom utf16lebom", Encoding.hasBom( EncodingCode.Utf16LEBom ) == true )
        check( "enc hasbom utf16bebom", Encoding.hasBom( EncodingCode.Utf16BEBom ) == true )
        check( "enc hasbom utf32lebom", Encoding.hasBom( EncodingCode.Utf32LEBom ) == true )
        check( "enc hasbom utf32bebom", Encoding.hasBom( EncodingCode.Utf32BEBom ) == true )
        check( "enc hasbom gb2312", Encoding.hasBom( EncodingCode.Gb2312 ) == false )
        check( "enc hasbom ascii", Encoding.hasBom( EncodingCode.Ascii ) == false )

        # 未知名抛 UnknownEncoding：精确匹配小写规范名（大小写敏感）
        var caught = 0
        label encUpperNameBlock
        {
            try encCodeFromName( "UTF-8" )
        }
        catch
        {
            caught = 1
        }
        check( "enc name uppercase fails", caught == 1 )

        caught = 0
        label encAliasNameBlock
        {
            try encCodeFromName( "utf8" )
        }
        catch
        {
            caught = 1
        }
        check( "enc name alias fails", caught == 1 )
    }

    # ── C：各编码官方向量与往返 ──

    static testVectors() throws
    {
        # "中" U+4E2D 的六种编码表示
        var u8 = Encoding.encode( EncodingCode.Utf8, "中" )
        check( "enc vec utf8", u8.toHex() == "e4b8ad" )
        check( "enc vec utf8 size", u8.readableBytes == 3 )
        u8.release()

        var u16le = Encoding.encode( EncodingCode.Utf16LE, "中" )
        check( "enc vec utf16le", u16le.toHex() == "2d4e" )
        u16le.release()

        var u16be = Encoding.encode( EncodingCode.Utf16BE, "中" )
        check( "enc vec utf16be", u16be.toHex() == "4e2d" )
        u16be.release()

        var u32le = Encoding.encode( EncodingCode.Utf32LE, "中" )
        check( "enc vec utf32le", u32le.toHex() == "2d4e0000" )
        u32le.release()

        var u32be = Encoding.encode( EncodingCode.Utf32BE, "中" )
        check( "enc vec utf32be", u32be.toHex() == "00004e2d" )
        u32be.release()

        var gb = Encoding.encode( EncodingCode.Gb2312, "中文" )
        check( "enc vec gb2312", gb.toHex() == "d6d0cec4" )
        gb.release()

        # 补充平面往返：U+1F600 的 UTF-16 代理对 D83D DE00（LE: 3d d8 00 de）
        var sur = ByteBuffer.fromHex( "3dd800de" )
        var surText = Encoding.decode( EncodingCode.Utf16LE, sur )
        sur.release()
        var surBack = Encoding.encode( EncodingCode.Utf16LE, surText )
        check( "enc vec surrogate roundtrip", surBack.toHex() == "3dd800de" )
        surBack.release()

        # 反向解码：各编码字节 → 文本（源索引不变）
        var src8 = ByteBuffer.fromHex( "e4b8ad" )
        check( "enc dec utf8", Encoding.decode( EncodingCode.Utf8, src8 ) == "中" )
        check( "enc dec src untouched", src8.readerIndex == 0 )
        src8.release()

        var src16be = ByteBuffer.fromHex( "4e2d" )
        check( "enc dec utf16be", Encoding.decode( EncodingCode.Utf16BE, src16be ) == "中" )
        src16be.release()

        var src32le = ByteBuffer.fromHex( "2d4e0000" )
        check( "enc dec utf32le", Encoding.decode( EncodingCode.Utf32LE, src32le ) == "中" )
        src32le.release()

        var srcGb = ByteBuffer.fromHex( "c4e3bac3" )
        check( "enc dec gb2312", Encoding.decode( EncodingCode.Gb2312, srcGb ) == "你好" )
        srcGb.release()

        var srcAsc = ByteBuffer.fromHex( "416239" )
        check( "enc dec ascii", Encoding.decode( EncodingCode.Ascii, srcAsc ) == "Ab9" )
        srcAsc.release()

        # 空输入往返
        var empty = Encoding.encode( EncodingCode.Utf16LE, "" )
        check( "enc empty encode", empty.readableBytes == 0 )
        check( "enc empty decode", Encoding.decode( EncodingCode.Utf16LE, empty ) == "" )
        empty.release()
    }

    # ── D：BOM 前缀 / 剥离 / 端序翻转 / preamble ──

    static testBom() throws
    {
        # *_BOM 变体编码产物 = BOM + 编码体
        var b8 = Encoding.encode( EncodingCode.Utf8Bom, "A" )
        check( "enc bom utf8", b8.toHex() == "efbbbf41" )
        b8.release()

        var b16le = Encoding.encode( EncodingCode.Utf16LEBom, "中" )
        check( "enc bom utf16le", b16le.toHex() == "fffe2d4e" )
        b16le.release()

        var b16be = Encoding.encode( EncodingCode.Utf16BEBom, "中" )
        check( "enc bom utf16be", b16be.toHex() == "feff4e2d" )
        b16be.release()

        var b32le = Encoding.encode( EncodingCode.Utf32LEBom, "A" )
        check( "enc bom utf32le", b32le.toHex() == "fffe000041000000" )
        b32le.release()

        var b32be = Encoding.encode( EncodingCode.Utf32BEBom, "A" )
        check( "enc bom utf32be", b32be.toHex() == "0000feff00000041" )
        b32be.release()

        # decode 自动剥离开头 BOM（非 BOM 代码声明 + 带 BOM 输入）
        var bom8 = ByteBuffer.fromHex( "efbbbf" + "e4b8ad" )
        check( "enc bom stripped utf8", Encoding.decode( EncodingCode.Utf8, bom8 ) == "中" )
        bom8.release()

        var bom16 = ByteBuffer.fromHex( "fffe" + "2d4e" )
        check( "enc bom stripped utf16le", Encoding.decode( EncodingCode.Utf16LE, bom16 ) == "中" )
        bom16.release()

        var bom32 = ByteBuffer.fromHex( "0000feff" + "00004e2d" )
        check( "enc bom stripped utf32be", Encoding.decode( EncodingCode.Utf32BE, bom32 ) == "中" )
        bom32.release()

        # 反向 BOM 翻转端序：声明端序与 BOM 相反时按 BOM 端序解释
        var flipToBe = Encoding.encode( EncodingCode.Utf16BEBom, "中" )
        check( "enc bom flip to be", Encoding.decode( EncodingCode.Utf16LE, flipToBe ) == "中" )
        flipToBe.release()

        var flipToLe = Encoding.encode( EncodingCode.Utf16LEBom, "中" )
        check( "enc bom flip to le", Encoding.decode( EncodingCode.Utf16BE, flipToLe ) == "中" )
        flipToLe.release()

        var flip32 = Encoding.encode( EncodingCode.Utf32BEBom, "中" )
        check( "enc bom flip utf32", Encoding.decode( EncodingCode.Utf32LE, flip32 ) == "中" )
        flip32.release()

        # preamble：*_BOM 系返回 BOM 字节，其余为空缓冲
        var p8 = Encoding.preamble( EncodingCode.Utf8Bom )
        check( "enc preamble utf8bom", p8.toHex() == "efbbbf" )
        p8.release()

        var p16 = Encoding.preamble( EncodingCode.Utf16LEBom )
        check( "enc preamble utf16lebom", p16.toHex() == "fffe" )
        p16.release()

        var p32be = Encoding.preamble( EncodingCode.Utf32BEBom )
        check( "enc preamble utf32bebom", p32be.toHex() == "0000feff" )
        p32be.release()

        var pgb = Encoding.preamble( EncodingCode.Gb2312 )
        check( "enc preamble gb2312 empty", pgb.readableBytes == 0 )
        pgb.release()

        var p8plain = Encoding.preamble( EncodingCode.Utf8 )
        check( "enc preamble utf8 empty", p8plain.readableBytes == 0 )
        p8plain.release()
    }

    # ── E：损坏输入 ──

    static testCorrupt() throws
    {
        # UTF-8 非法：裸 continuation 字节开头
        expectDecodeFail( "enc bad utf8 continuation", EncodingCode.Utf8, "80" )
        # UTF-8 非法：overlong 形式 C0 80
        expectDecodeFail( "enc bad utf8 overlong", EncodingCode.Utf8, "c080" )
        # UTF-8 非法：截断的多字节序列（E4 后缺续字节）
        expectDecodeFail( "enc bad utf8 truncated", EncodingCode.Utf8, "e4b8" )

        # UTF-16 未配对高代理（LE: D800 0041，0041 非低代理）
        expectDecodeFail( "enc bad utf16 lone surrogate", EncodingCode.Utf16LE, "00d84100" )
        # UTF-16 奇数长度
        expectDecodeFail( "enc bad utf16 odd length", EncodingCode.Utf16LE, "2d" )

        # UTF-32 值越界（LE 字节 00001100 = 0x110000 > U+10FFFF）
        expectDecodeFail( "enc bad utf32 out of range", EncodingCode.Utf32LE, "00001100" )
        # UTF-32 长度非 4 倍数
        expectDecodeFail( "enc bad utf32 bad length", EncodingCode.Utf32LE, "2d4e00" )

        # GB2312 trail 越界（A1 30：trail < 0xA1）
        expectDecodeFail( "enc bad gb2312 trail range", EncodingCode.Gb2312, "a130" )
        # GB2312 未赋值位（A2A1 = 02 区 01 位，表值为 0）
        expectDecodeFail( "enc bad gb2312 unassigned", EncodingCode.Gb2312, "a2a1" )
        # GB2312 lead 后截断
        expectDecodeFail( "enc bad gb2312 truncated", EncodingCode.Gb2312, "d6" )

        # ASCII 解码遇高位字节（不做 latin-1 宽松映射）
        expectDecodeFail( "enc bad ascii high byte", EncodingCode.Ascii, "c3a9" )

        # 编码方向：目标字符集无法表示（U+00E9 / U+20AC 均表外）
        expectEncodeStrFail( "enc bad ascii unmappable", EncodingCode.Ascii, "é" )
        expectEncodeStrFail( "enc bad gb2312 unmappable", EncodingCode.Gb2312, "€" )

        # 编码方向：ByteBuffer 源本身非法 UTF-8
        expectEncodeBufFail( "enc bad encode not utf8", EncodingCode.Utf16LE, "80" )
    }

    # ── F：释放后的源 ──

    static testReleased() throws
    {
        var gone = ByteBuffer( 4 )
        gone.writeU8( 1 )
        gone.release()

        var caught = 0
        label encGoneEncodeBlock
        {
            try encEncodeBuf( EncodingCode.Utf8, gone )
        }
        catch
        {
            caught = 1
        }
        check( "enc released encode fails", caught == 1 )

        caught = 0
        label encGoneDecodeBlock
        {
            try encDecode( EncodingCode.Utf16LE, gone )
        }
        catch
        {
            caught = 1
        }
        check( "enc released decode fails", caught == 1 )
    }

    # ── G：便捷类（Utf8 / Unicode / BigEndianUnicode / Gb2312 / Ascii） ──

    static testConvenience() throws
    {
        # Utf8
        var u8 = Utf8.encode( "中" )
        check( "utf8 encode", u8.toHex() == "e4b8ad" )
        check( "utf8 decode", Utf8.decode( u8 ) == "中" )
        u8.release()
        var u8b = Utf8.encodeBom( "A" )
        check( "utf8 encodeBom", u8b.toHex() == "efbbbf41" )
        u8b.release()
        check( "utf8 maxByteCount", Utf8.maxByteCount( 10 ) == 10 )
        check( "utf8 maxCharCount", Utf8.maxCharCount( 10 ) == 10 )

        # Unicode（UTF-16LE，C# Encoding.Unicode 对位）
        var un = Unicode.encode( "A" )
        check( "unicode encode", un.toHex() == "4100" )
        check( "unicode decode", Unicode.decode( un ) == "A" )
        un.release()
        var unb = Unicode.encodeBom( "中" )
        check( "unicode encodeBom", unb.toHex() == "fffe2d4e" )
        unb.release()
        check( "unicode maxByteCount", Unicode.maxByteCount( 10 ) == 20 )

        # BigEndianUnicode（UTF-16BE）
        var be = BigEndianUnicode.encode( "A" )
        check( "beunicode encode", be.toHex() == "0041" )
        check( "beunicode decode", BigEndianUnicode.decode( be ) == "A" )
        be.release()
        var beb = BigEndianUnicode.encodeBom( "中" )
        check( "beunicode encodeBom", beb.toHex() == "feff4e2d" )
        beb.release()
        check( "beunicode maxByteCount", BigEndianUnicode.maxByteCount( 4 ) == 8 )

        # Gb2312
        var gb = Gb2312.encode( "你好" )
        check( "gb2312 encode", gb.toHex() == "c4e3bac3" )
        check( "gb2312 decode", Gb2312.decode( gb ) == "你好" )
        gb.release()
        check( "gb2312 maxCharCount", Gb2312.maxCharCount( 10 ) == 15 )

        # Ascii
        var asc = Ascii.encode( "Ab9" )
        check( "ascii encode", asc.toHex() == "416239" )
        check( "ascii decode", Ascii.decode( asc ) == "Ab9" )
        asc.release()
        check( "ascii maxByteCount", Ascii.maxByteCount( 10 ) == 10 )
    }

    # ── H：跨编码转换 ──

    static testConvert() throws
    {
        # GB2312 -> UTF-16LE -> GB2312 往返（"中文"："文" U+6587 LE = 87 65）
        var gb = Encoding.encode( EncodingCode.Gb2312, "中文" )
        var wide = Encoding.convert( EncodingCode.Gb2312, EncodingCode.Utf16LE, gb )
        check( "enc convert gb to u16le", wide.toHex() == "2d4e8765" )
        var back = Encoding.convert( EncodingCode.Utf16LE, EncodingCode.Gb2312, wide )
        check( "enc convert u16le to gb", back.toHex() == "d6d0cec4" )
        check( "enc convert src untouched", gb.readerIndex == 0 )
        gb.release()
        wide.release()
        back.release()

        # ASCII -> UTF-32BE（无 BOM）
        var asc = ByteBuffer.fromHex( "4142" )
        var wide32 = Encoding.convert( EncodingCode.Ascii, EncodingCode.Utf32BE, asc )
        check( "enc convert ascii to u32be", wide32.toHex() == "0000004100000042" )
        asc.release()
        wide32.release()
    }

    static fun()
    {
        Console.println( "===== EncodingTest =====" )
        testBound()
        testCode()
        testVectors()
        testBom()
        testCorrupt()
        testReleased()
        testConvenience()
        testConvert()
        Console.println( "[EncodingTest] all groups done" )
    }
}
