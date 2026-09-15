# ============================================================================
# test/BaseTest/StringBuilderTest.sl — StringBuilder 冒烟测试
#
# 覆盖分组：
#   A construct     三构造（默认/string/Int32 容量）+ fromString 工厂
#   B append        string/bool/Int32/Int64/Float64 重载、链式调用、数值兜底路径
#   C lines/format  appendLine 两种形态、appendFormat 索引式 {} 与自增式、write 别名
#   D utf8/bytes    多字节 UTF-8 length（字节数）、toUInt8Array 字节序列
#   E clear/release clear 复用、追加复写、幂等双释放
#
# 已知不可测项：
#   - 追加类实例：C 层 SystemConvertString 无对象文本化协议（见
#     StringBuilder.sl 兜底重载注释），自定义对象应先显式 .toString()，
#     此处不作为契约断言
#   - insert/replace/delete/reverse：Java 对照的已知未实现偏差
# ============================================================================

StringBuilderTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[StringBuilderTest] " + name + " : OK" )
        }
        else
        {
            global.println( "[StringBuilderTest] " + name + " : FAIL" )
        }
    }

    # ── A：构造与工厂 ──

    static testConstructors()
    {
        var b = StringBuilder()
        check( "default empty", b.isEmpty )
        check( "default capacity 512", b.capacity == 512 )
        check( "default length 0", b.length == 0 )

        var s = StringBuilder( "abc" )
        check( "init(string) content", s.toString() == "abc" )
        check( "init(string) length", s.length == 3 )
        check( "init(string) isNotEmpty", s.isNotEmpty )

        var c = StringBuilder( 64 )
        check( "init(Int32) capacity", c.capacity == 64 )
        check( "init(Int32) empty", c.isEmpty )

        var f = StringBuilder.fromString( "fac" )
        check( "fromString factory", f.toString() == "fac" )
        check( "fromString length", f.length == 3 )
    }

    # ── B：append 重载与链式 ──

    static testAppend() throws
    {
        var b = StringBuilder()
        b.append( "n=" ).append( 42 ).append( "," ).append( true )
        check( "append chain string/int/bool", b.toString() == "n=42,true" )

        Int64 big = 9000000000
        b.clear()
        b.append( big )
        check( "append Int64", b.toString() == "9000000000" )

        # 注意：裸小数字面量默认 Float32 精度（md/syntax/number.md），
        # 精确 Float64 需 d 后缀，否则 3.14 → 3.140000104904175
        Float64 f = 3.14d
        b.clear()
        b.append( f )
        check( "append Float64", b.toString() == "3.14" )

        # UInt8 无专属重载 → 数值兜底路径（SystemConvertString）
        var src = ByteBuf.fromHex( "c8" )
        var u = src.readU8()
        src.release()
        b.clear()
        b.append( u )
        check( "append UInt8 numeric fallback", b.toString() == "200" )
    }

    # ── C：appendLine / appendFormat / write ──

    static testLinesAndFormat()
    {
        var b = StringBuilder()
        b.appendLine( "first" )
        b.appendLine()
        b.write( "tail" )
        check( "appendLine/write content", b.toString() == "first\n\ntail" )
        check( "appendLine length", b.length == 11 )

        var f = StringBuilder()
        f.appendFormat( "{0} + {1} = {2}", "a", "b", "ab" )
        check( "appendFormat indexed", f.toString() == "a + b = ab" )

        var g = StringBuilder()
        g.appendFormat( "x={} y={}", 7, "z" )
        check( "appendFormat autoincrement", g.toString() == "x=7 y=z" )
    }

    # ── D：UTF-8 与字节序列 ──

    static testUtf8AndBytes()
    {
        var b = StringBuilder( "中文" )
        check( "utf8 length is bytes", b.length == 6 )
        check( "utf8 toString roundtrip", b.toString() == "中文" )

        var arr = b.toUInt8Array()
        check( "toUInt8Array length", arr.length == 6 )
        check( "toUInt8Array first byte", arr[0] == 228 )
    }

    # ── E：clear 与 release ──

    static testClearAndRelease()
    {
        var b = StringBuilder( "hello" )
        check( "isNotEmpty before clear", b.isNotEmpty )
        b.clear()
        check( "clear empties", b.isEmpty && b.length == 0 )
        check( "clear keeps capacity", b.capacity >= 5 )
        b.append( "again" )
        check( "append after clear", b.toString() == "again" )
        b.release()
        b.release()
        check( "double release idempotent", true )
    }

    static fun()
    {
        global.println( "========== StringBuilderTest (start) ==========" )
        testConstructors()
        testAppend()
        testLinesAndFormat()
        testUtf8AndBytes()
        testClearAndRelease()
        global.println( "========== StringBuilderTest (end) ==========" )
    }
}

# 测试面向：StringBuilder 三构造与工厂、append 六重载链式、appendLine/appendFormat/write、
# UTF-8 字节语义（length=字节数、toUInt8Array）、clear 复用与幂等 release。
# 预期：全部输出 OK，无 FAIL；双释放后不再触碰实例。
