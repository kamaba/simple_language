# ============================================================================
# test/ExpendTest/RegExpTest.sl — Std.Text.RegExp 回溯引擎冒烟测试
# 实现：source/Front/Lib/Std/Text/RegExp.sl（纯 .sl，UTF-8 字节级）
#
# 覆盖分组：
#   A basic      match/search 锚定语义、group 捕获、静态便捷入口
#   B quant      * + ? {n} {n,} {n,m} 与懒惰后缀 ?
#   C class      [...] [^...] 区间、\d\D \w\W \s\S、字面 \.
#   D anchor     ^ $ \b \B + multiline 构造
#   E group      (?:...) 非捕获、| 选择、嵌套编号、可选组 groupOk
#   F lookahead  (?=) (?!)
#   G backref    \1-\9
#   H replace    replace $& $1..$9 $$、matchAll、split、escape
#   I utf8       中文多字节、. 整字符消费、字节级 index
#   J error      非法模式 → RegExpError（label/catch 捕获）
# ============================================================================

import Std;

RegExpTest
{
    static check( string name, bool cond )
    {
        if cond
        {
            Console.println( "[RegExpTest] " + name + " : OK" )
        }
        else
        {
            Console.println( "[RegExpTest] " + name + " : FAIL" )
        }
    }

    # ── 异常注入辅助（throws 单动作，供 label/catch 捕获） ──

    static makeUnclosed() throws
    {
        var re = Text.RegExp( "(" )
        Console.println( re.groupCount.toString() )
    }

    static makeBadRepeat() throws
    {
        var re = Text.RegExp( "a{2,1}" )
        Console.println( re.groupCount.toString() )
    }

    static makeStarStart() throws
    {
        var re = Text.RegExp( "*a" )
        Console.println( re.groupCount.toString() )
    }

    # ── A：match / search / group / 静态便捷 ──

    static testBasic() throws
    {
        var re = Text.RegExp( "(\\w+)@(\\w+)\\.com" )

        var m = re.search( "mail: bob@exa.com end" )
        check( "search found", m != null )
        if m != null
        {
            check( "group0 value", m.value() == "bob@exa.com" )
            check( "group1", m.group( 1 ) == "bob" )
            check( "group2", m.group( 2 ) == "exa" )
            check( "length bytes", m.length == 11 )
            check( "end = index+length", m.end == m.index + m.length )
        }

        check( "match anchored at head", re.match( "mail: bob@exa.com" ) == null )
        var m2 = re.match( "bob@exa.com tail" )
        check( "match from head ok", m2 != null && m2.value() == "bob@exa.com" )

        check( "isMatch true", re.isMatch( "x@y.com" ) )
        check( "isMatch false", re.isMatch( "no at sign" ) == false )

        check( "static isMatch", Text.RegExp.isMatch( "a1b", "\\d" ) )
        var sm = Text.RegExp.search( "hi bob@exa.com", "(\\w+)@" )
        check( "static search group", sm != null && sm.group( 1 ) == "bob" )
    }

    # ── B：量词与懒惰 ──

    static testQuantifier() throws
    {
        check( "star all", Text.RegExp( "ab*" ).match( "abbb" ).value() == "abbb" )
        check( "star zero", Text.RegExp( "ab*" ).match( "a" ).value() == "a" )
        check( "plus need one", Text.RegExp( "ab+" ).match( "a" ) == null )
        check( "plus ok", Text.RegExp( "ab+" ).match( "abb" ).value() == "abb" )
        check( "opt yes", Text.RegExp( "ab?c" ).match( "abc" ).value() == "abc" )
        check( "opt no", Text.RegExp( "ab?c" ).match( "ac" ).value() == "ac" )

        check( "rep n fail", Text.RegExp( "a{3}" ).match( "aa" ) == null )
        check( "rep n ok", Text.RegExp( "a{3}" ).match( "aaa" ).value() == "aaa" )
        check( "rep n of longer", Text.RegExp( "a{3}" ).match( "aaaa" ).value() == "aaa" )
        check( "rep n comma", Text.RegExp( "a{2,}" ).match( "aaaa" ).value() == "aaaa" )
        check( "rep n m", Text.RegExp( "a{2,3}" ).match( "aaaa" ).value() == "aaa" )

        check( "lazy star zero", Text.RegExp( "a*?" ).match( "aaa" ).value() == "" )
        check( "lazy plus stop", Text.RegExp( "<.+?>" ).match( "<a><b>" ).value() == "<a>" )
        check( "greedy plus eat", Text.RegExp( "<.+>" ).match( "<a><b>" ).value() == "<a><b>" )
        check( "lazy rep range", Text.RegExp( "a{1,3}?" ).match( "aaa" ).value() == "a" )
    }

    # ── C：字符类 ──

    static testCharClass() throws
    {
        check( "class set", Text.RegExp( "[abc]+" ).match( "cab" ).value() == "cab" )
        check( "class negate", Text.RegExp( "[^abc]+" ).search( "xyzabc" ).value() == "xyz" )
        check( "class range", Text.RegExp( "[a-cx-z]+" ).match( "zyxab" ).value() == "zyxab" )
        check( "class dash literal", Text.RegExp( "[-a]+" ).match( "-a-" ).value() == "-a-" )
        check( "digit class", Text.RegExp( "\\d+" ).search( "abc123def456" ).value() == "123" )
        check( "non digit", Text.RegExp( "\\D+" ).search( "123abc456" ).value() == "abc" )
        check( "word class", Text.RegExp( "\\w+" ).match( "hello world" ).value() == "hello" )
        check( "non word", Text.RegExp( "\\W+" ).search( "ab, cd" ).value() == ", " )
        check( "space class", Text.RegExp( "\\s+" ).search( "a  b" ).value() == "  " )
        check( "non space", Text.RegExp( "\\S+" ).search( " ab" ).value() == "ab" )
        check( "dot in class", Text.RegExp( "[.]" ).match( "." ) != null )
        check( "dot in class no digit", Text.RegExp( "[.]" ).match( "5" ) == null )
        check( "escaped dot", Text.RegExp( "3\\.14" ).match( "3.14" ).value() == "3.14" )
        check( "escaped dot no comma", Text.RegExp( "3\\.14" ).match( "3x14" ) == null )
    }

    # ── D：锚点与 multiline ──

    static testAnchor() throws
    {
        check( "caret head only", Text.RegExp( "^abc" ).search( "xabc" ) == null )
        check( "caret ok", Text.RegExp( "^abc" ).search( "abcd" ).value() == "abc" )

        check( "dollar tail only", Text.RegExp( "abc\$" ).search( "abcx" ) == null )
        check( "dollar ok", Text.RegExp( "abc\$" ).search( "xxabc" ).value() == "abc" )

        var words = Text.RegExp( "\\bfoo\\b" ).matchAll( "foo bar foo baz foofoo" )
        check( "word boundary count", words.length == 2 )
        var wb = Text.RegExp( "\\bfoo" ).search( "a foo b" )
        check( "boundary find", wb != null && wb.value() == "foo" )
        var nb = Text.RegExp( "\\Boo" ).search( "foo boo" )
        check( "non boundary inside", nb != null && nb.index == 1 )

        var ml = Text.RegExp( "^b", false, true )
        var mlHits = ml.matchAll( "a\nb" )
        check( "multiline caret", mlHits.length == 1 )
        var nm = Text.RegExp( "^b" )
        var nmHits = nm.matchAll( "a\nb" )
        check( "no multiline caret", nmHits.length == 0 )
    }

    # ── E：分组与选择 ──

    static testGroup() throws
    {
        check( "non capture", Text.RegExp( "(?:ab|cd)+" ).match( "abcd" ).value() == "abcd" )
        var alt = Text.RegExp( "(a|b)+" ).match( "ab" )
        check( "alt value", alt != null && alt.value() == "ab" )
        check( "alt last group", alt.group( 1 ) == "b" )

        var nest = Text.RegExp( "((a)(b))" ).match( "ab" )
        check( "nest g1", nest != null && nest.group( 1 ) == "ab" )
        check( "nest g2", nest.group( 2 ) == "a" )
        check( "nest g3", nest.group( 3 ) == "b" )
        check( "groupCount", nest._groupCount == 3 )

        var opt = Text.RegExp( "(a)?b" )
        var om = opt.match( "b" )
        check( "optional absent", om != null && om.groupOk( 1 ) == false )
        check( "optional empty text", om.group( 1 ) == "" )
        var on = opt.match( "ab" )
        check( "optional present", on != null && on.groupOk( 1 ) && on.group( 1 ) == "a" )
    }

    # ── F：前瞻 ──

    static testLookahead() throws
    {
        var la = Text.RegExp( "\\w+(?=@)" ).search( "bob@exa" )
        check( "positive lookahead", la != null && la.value() == "bob" )
        var nl = Text.RegExp( "foo(?!bar)" ).search( "foobar foobaz" )
        check( "negative lookahead skip", nl != null && nl.index == 7 )
        check( "lookahead not consume", Text.RegExp( "a(?=b)" ).match( "ab" ).value() == "a" )
    }

    # ── G：反向引用 ──

    static testBackref() throws
    {
        var br = Text.RegExp( "(\\w+) \\1" ).search( "hey hey you" )
        check( "backref found", br != null )
        check( "backref value", br.value() == "hey hey" )
        check( "backref group", br.group( 1 ) == "hey" )
        check( "backref no false hit", Text.RegExp( "(\\w+) \\1" ).search( "ab cd" ) == null )
    }

    # ── H：replace / matchAll / split / escape ──

    static testReplace() throws
    {
        var re = Text.RegExp( "(\\w+)@(\\w+)" )
        var rep = re.replace( "a@b c@d", "[\$1:\$2:\$&:\$\$]" )
        check( "replace all with groups", rep == "[a:b:a@b:$] [c:d:c@d:$]" )

        var nums = Text.RegExp( "(\\d+)" ).matchAll( "a1b22c333" )
        check( "matchAll count", nums.length == 3 )
        Text.RegExpMatch n0 = nums[0]
        Text.RegExpMatch n1 = nums[1]
        Text.RegExpMatch n2 = nums[2]
        check( "matchAll item0", n0.group( 1 ) == "1" )
        check( "matchAll item1", n1.group( 1 ) == "22" )
        check( "matchAll item2", n2.group( 1 ) == "333" )

        var parts = Text.RegExp( ",\\s*" ).split( "a, b ,c" )
        check( "split count", parts.length == 3 )
        string p0 = parts[0]
        string p2 = parts[2]
        check( "split head", p0 == "a" )
        check( "split tail", p2 == "c" )

        var esc = Text.RegExp.escape( "1+1=2" )
        var er = Text.RegExp( esc )
        check( "escape roundtrip", er.search( "zz1+1=2zz" ) != null )
        var escLit = Text.RegExp.escape( "1+1" )
        var elr = Text.RegExp( escLit )
        check( "escape plus literal only", elr.isMatch( "211" ) == false )
        check( "escape plus match", elr.isMatch( "x1+1y" ) )
    }

    # ── I：UTF-8 多字节 ──

    static testUtf8() throws
    {
        var zh = Text.RegExp( "中+" ).match( "中中中" )
        check( "utf8 repeat", zh != null && zh.value() == "中中中" )
        var guo = Text.RegExp( "国" ).search( "中国" )
        check( "utf8 byte index", guo != null && guo.index == 3 )
        var dots = Text.RegExp( "." ).matchAll( "a中b" )
        check( "dot eats whole char", dots.length == 3 )
        Text.RegExpMatch d1 = dots[1]
        check( "dot zh value", d1.value() == "中" )
        var cls = Text.RegExp( "[中日]" ).search( "日本語" )
        check( "class zh literal", cls != null && cls.value() == "日" )
        var zhGroup = Text.RegExp( "(\\w+)年(\\d+)月" ).search( "今年是2026年10月" )
        #\w 含非 ASCII（同 .NET/Python），故 group1 是"今年是2026"而非"2026"
        check( "zh mixed capture", zhGroup != null && zhGroup.group( 1 ) == "今年是2026" )
        check( "zh mixed digit group", zhGroup != null && zhGroup.group( 2 ) == "10" )
    }

    # ── J：非法模式 → RegExpError ──

    static testError() throws
    {
        var caught = 0
        label regUnclosedBlock
        {
            try makeUnclosed()
        }
        catch
        {
            caught = 1
        }
        check( "unclosed paren throws", caught == 1 )

        caught = 0
        label regBadRepeatBlock
        {
            try makeBadRepeat()
        }
        catch
        {
            caught = 1
        }
        check( "bad repeat throws", caught == 1 )

        caught = 0
        label regStarStartBlock
        {
            try makeStarStart()
        }
        catch
        {
            caught = 1
        }
        check( "star at start throws", caught == 1 )

        var ok = Text.RegExp( "a(b)c" )
        check( "valid pattern still ok", ok.groupCount == 1 )
    }

    static fun()
    {
        Console.println( "===== RegExpTest =====" )
        testBasic()
        testQuantifier()
        testCharClass()
        testAnchor()
        testGroup()
        testLookahead()
        testBackref()
        testReplace()
        testUtf8()
        testError()
        Console.println( "[RegExpTest] all groups done" )
    }
}
