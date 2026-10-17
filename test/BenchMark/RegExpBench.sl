import Std

# ============================================================================
# test/BenchMark/RegExpBench.sl — Std.Text.RegExp 性能基准
# 目的：度量 _run 回溯引擎下沉 C VM（SystemRegExpRun）前后的耗时差异
# 用例：编译 / 早命中 search / 全扫不命中 / matchAll / replace / 分组捕获 /
#       backref / UTF-8 中文
# ============================================================================

RegExpBench
{
    static fun()
    {
        Console.println( "========== RegExpBench (start) ==========" )

        # 主文本："abc123 def456 ghi789 " x 4000 = 80000 字节
        string chunk = "abc123 def456 ghi789 "
        string text = ""
        for i = 0, i < 4000, i++
        {
            text = text + chunk
        }
        Console.println( "text bytes = " + text.length.toString() )

        # 0. 编译开销 x2000（模式解析在 SL，两版相同）
        nowMs = Environment.sys.nowMillis()
        for i = 0, i < 2000, i++
        {
            var re = Text.RegExp( "([a-z]+)([0-9]+)" )
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println( "compile x2000  [" + nowMs.toString() + " ms]" )

        # 1. search 早命中 "def456" x500（第 8 字节命中）
        var re1 = Text.RegExp( "def456" )
        nowMs = Environment.sys.nowMillis()
        string hit = ""
        for i = 0, i < 500, i++
        {
            hit = re1.search( text ).value()
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println( "search hit x500  [" + nowMs.toString() + " ms]  value=" + hit )

        # 2. search 全扫不命中 "zzz999" x20（80000 个起点全部失败）
        var re2 = Text.RegExp( "zzz999" )
        nowMs = Environment.sys.nowMillis()
        bool miss = true
        for i = 0, i < 20, i++
        {
            miss = re2.search( text ) == null
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println( "search miss x20  [" + nowMs.toString() + " ms]  miss=" + miss.toString() )

        # 3. matchAll "[a-z]+" x5（12000 个单词匹配）
        var re3 = Text.RegExp( "[a-z]+" )
        nowMs = Environment.sys.nowMillis()
        Int32 words = 0
        for i = 0, i < 5, i++
        {
            words = re3.matchAll( text ).length
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println( "matchAll x5  [" + nowMs.toString() + " ms]  count=" + words.toString() )

        # 4. replace "[0-9]+" x5（12000 处替换）
        var re4 = Text.RegExp( "[0-9]+" )
        nowMs = Environment.sys.nowMillis()
        string rep = ""
        for i = 0, i < 5, i++
        {
            rep = re4.replace( text, "#" )
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println( "replace x5  [" + nowMs.toString() + " ms]  len=" + rep.length.toString() )

        # 5. 分组捕获 search + group 取值 x200
        var re5 = Text.RegExp( "([a-z]+)([0-9]+)" )
        nowMs = Environment.sys.nowMillis()
        string g1 = ""
        string g2 = ""
        for i = 0, i < 200, i++
        {
            var m = re5.search( text )
            g1 = m.group( 1 )
            g2 = m.group( 2 )
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println( "group search x200  [" + nowMs.toString() + " ms]  g1=" + g1 + " g2=" + g2 )

        # 6. backref 文本 "abc abc def def " x 2000 = 32000 字节
        string chunk2 = "abc abc def def "
        string text2 = ""
        for i = 0, i < 2000, i++
        {
            text2 = text2 + chunk2
        }
        var re6 = Text.RegExp( "(\\w+) \\1" )
        nowMs = Environment.sys.nowMillis()
        Int32 pairs = 0
        for i = 0, i < 10, i++
        {
            pairs = re6.matchAll( text2 ).length
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println( "backref matchAll x10  [" + nowMs.toString() + " ms]  count=" + pairs.toString() )

        # 7. UTF-8 中文："中文测试123 " x 2000 = 44000 字节
        string chunk3 = "中文测试123 "
        string text3 = ""
        for i = 0, i < 2000, i++
        {
            text3 = text3 + chunk3
        }
        var re7 = Text.RegExp( "测试[0-9]+" )
        nowMs = Environment.sys.nowMillis()
        string zh = ""
        for i = 0, i < 100, i++
        {
            zh = re7.search( text3 ).value()
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println( "utf8 search x100  [" + nowMs.toString() + " ms]  value=" + zh )

        Console.println( "========== RegExpBench (end) ==========" )
    }
}
