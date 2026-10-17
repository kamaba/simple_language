import Std;
#Yaml 测试：BaseYaml（Core）块映射/序列/flow/块标量/引号/注释/文档边界解析与序列化往返，
#Text.Yaml（Std）文件加载/保存往返，非法输入失败路径，与 Json 的 flow 互通。

YamlTest
{
    static fun()
    {
        global.println("===== YamlTest start =====")
        testBlockMap()
        testSequences()
        testFlow()
        testBlockScalars()
        testDocBoundary()
        testRoundTrip()
        testYamlFile()
        testFailures()
        global.println("===== YamlTest end =====")
    }

    # 断言辅助：OK/FAIL 单行输出
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[Yaml] " + name + " : OK" )
        }
        else
        {
            global.println( "[Yaml] " + name + " : FAIL" )
        }
    }

    # 基础块映射：标量/数字/bool/null/注释/嵌套映射/树透传
    static testBlockMap()
    {
        global.println( "----- testBlockMap -----" )
        string text = "# top comment\nname: alice\nage: 30\nscore: 95.5\nvip: true\nnote: null\nempty:\ndisabled: false\nport: 8080  # trailing comment\n"

        BaseYaml y = BaseYaml( text )
        check( "解析成功非空", y.isNotEmpty )
        check( "成员数 8", y.memberCount == 8 )
        check( "全树节点数 9", y.nodeCount == 9 )
        check( "getStr", y.getStr( "name" ) == "alice" )
        check( "getInt", y.getInt( "age" ) == 30 )
        check( "getFloat", y.getFloat( "score" ) == 95.5d )
        check( "getBool true", y.getBool( "vip" ) )
        check( "getBool false", !y.getBool( "disabled" ) )
        check( "行尾注释剥除", y.getInt( "port" ) == 8080 )
        check( "isNull note", BaseYaml.isNull( y.select( "note" ) ) )
        check( "isNull empty", BaseYaml.isNull( y.select( "empty" ) ) )
        check( "缺失路径默认", y.getStr( "nope", "d" ) == "d" )
        check( "null 取默认", y.getStr( "note", "d" ) == "d" )
        check( "null getInt 默认", y.getInt( "empty", -1 ) == -1 )
        check( "isObject root", BaseYaml.isObject( y.root ) )
        check( "has 命中", y.has( "name" ) )
        check( "has 未命中", !y.has( "zzz" ) )
        check( "findByName", y.findByName( "name" )._value == "alice" )

        # 嵌套块映射 + 注释行跳过
        BaseYaml n = BaseYaml( "server:\n  host: 127.0.0.1\n  # 中间注释\n  port: 8080\n" )
        check( "嵌套取值", n.getStr( "server/host" ) == "127.0.0.1" )
        check( "嵌套类型化取值", n.getInt( "server/port" ) == 8080 )
        check( "嵌套 isObject", BaseYaml.isObject( n.select( "server" ) ) )

        # 大小写 bool 变体
        BaseYaml b = BaseYaml( "a: True\nb: FALSE\n" )
        check( "True 规范化", b.getBool( "a" ) )
        check( "FALSE 规范化", !b.getBool( "b" ) )

        # 空实例/静态工厂/clear
        BaseYaml empty = BaseYaml()
        check( "空实例 isEmpty", empty.isEmpty )
        check( "静态 parse", BaseYaml.parse( "a: 1" ).getStr( "a" ) == "1" )
        BaseYaml c = BaseYaml( "a: 1" )
        c.clear()
        check( "clear 后 isEmpty", c.isEmpty )
    }

    # 序列：顶层/内联映射/嵌套序列/同缩进特例/独行 dash
    static testSequences()
    {
        global.println( "----- testSequences -----" )

        # 顶层序列
        BaseYaml top = BaseYaml( "- 1\n- 2\n- 3\n" )
        check( "顶层序列 isArray", BaseYaml.isArray( top.root ) )
        check( "顶层元素数", top.memberCount == 3 )
        check( "下标取值", top.getStr( "2" ) == "3" )

        # 序列项内联映射 - k: v
        BaseYaml im = BaseYaml( "items:\n  - id: 1\n    name: first\n  - id: 2\n    name: second\n" )
        check( "序列 isArray", BaseYaml.isArray( im.select( "items" ) ) )
        check( "序列长度", im.select( "items" ).childCount == 2 )
        check( "内联映射 isObject", BaseYaml.isObject( im.select( "items/0" ) ) )
        check( "内联映射取值", im.getStr( "items/0/name" ) == "first" )
        check( "内联映射类型化", im.getInt( "items/1/id" ) == 2 )

        # 嵌套序列 - - x
        BaseYaml ns = BaseYaml( "matrix:\n  - - a\n    - b\n  - - c\n    - d\n" )
        check( "嵌套序列取值0", ns.getStr( "matrix/0/1" ) == "b" )
        check( "嵌套序列取值1", ns.getStr( "matrix/1/0" ) == "c" )
        check( "嵌套 isArray", BaseYaml.isArray( ns.select( "matrix/0" ) ) )

        # 同缩进序列特例（序列项与键同缩进）
        BaseYaml sm = BaseYaml( "same:\n- 1\n- 2\n" )
        check( "同缩进 isArray", BaseYaml.isArray( sm.select( "same" ) ) )
        check( "同缩进取值", sm.getStr( "same/1" ) == "2" )

        # 独行 dash + 缩进子块 / dash 后标量
        BaseYaml dk = BaseYaml( "wrap:\n  -\n    k: v\n  - plain\n" )
        check( "独行 dash 子块映射", dk.getStr( "wrap/0/k" ) == "v" )
        check( "dash 后标量", dk.getStr( "wrap/1" ) == "plain" )
    }

    # flow 风格：单行/嵌套/跨行/空集合/flow 内引号
    static testFlow()
    {
        global.println( "----- testFlow -----" )
        BaseYaml f = BaseYaml( "a: [1, 2, 3]\nb: {x: 1, y: two}\nc: [1,\n     2,\n     3]\nempty: []\nemap: {}\n" )
        check( "flow seq isArray", BaseYaml.isArray( f.select( "a" ) ) )
        check( "flow seq 取值", f.getStr( "a/2" ) == "3" )
        check( "flow map isObject", BaseYaml.isObject( f.select( "b" ) ) )
        check( "flow map 取值", f.getStr( "b/y" ) == "two" )
        check( "跨行 flow", f.getStr( "c/1" ) == "2" )
        check( "空 seq", BaseYaml.isArray( f.select( "empty" ) ) && f.select( "empty" ).childCount == 0 )
        check( "空 map", BaseYaml.isObject( f.select( "emap" ) ) && f.select( "emap" ).childCount == 0 )

        # flow 内行内注释：截断到行尾，闭括号跨行续接；同行吞掉闭括号按失败处理
        BaseYaml fq = BaseYaml( "q: [\"a, b\", 'c']\n" )
        check( "flow 双引号元素", fq.getStr( "q/0" ) == "a, b" )
        check( "flow 单引号元素", fq.getStr( "q/1" ) == "c" )
        BaseYaml fc = BaseYaml( "h: [x # c\n]\n" )
        check( "flow 注释跨行闭合", fc.select( "h" ).childCount == 1 )
        check( "flow 注释后取值", fc.getStr( "h/0" ) == "x" )
        BaseYaml fb = BaseYaml( "h: [x # c]\n" )
        check( "flow 注释吞闭括号失败", fb.isEmpty )

        # 嵌套 flow
        BaseYaml fn = BaseYaml( "n: {list: [1, {k: v}]}\n" )
        check( "嵌套 flow 取值", fn.getStr( "n/list/1/k" ) == "v" )

        # flow 值为 null
        BaseYaml fz = BaseYaml( "z: {a: , b: null}\n" )
        check( "flow 空值 null", BaseYaml.isNull( fz.select( "z/a" ) ) )
        check( "flow null 字面量", BaseYaml.isNull( fz.select( "z/b" ) ) )
    }

    # 块标量：literal/folded/chomping + 引号转义
    static testBlockScalars()
    {
        global.println( "----- testBlockScalars -----" )
        string text = "lit: |\n  line1\n  line2\nfold: >\n  folded\n  text\nstrip: |-\n  a\nkeep: |+\n  b\n\nsq: 'it''s'\ndq: \"say \\\"hi\\\"\"\nesc: \"tab\\there\\nnl\"\n"
        BaseYaml y = BaseYaml( text )
        check( "literal 块", y.getStr( "lit" ) == "line1\nline2\n" )
        check( "folded 块", y.getStr( "fold" ) == "folded text\n" )
        check( "strip chomp", y.getStr( "strip" ) == "a" )
        check( "keep chomp", y.getStr( "keep" ) == "b\n\n" )
        check( "单引号转义", y.getStr( "sq" ) == "it's" )
        check( "双引号转义", y.getStr( "dq" ) == "say \"hi\"" )
        check( "控制字符转义", y.getStr( "esc" ) == "tab\there\nnl" )

        # 数字形文本保持原文（弱类型）
        BaseYaml numKeep = BaseYaml( "v: 0123\nw: 1e3\n" )
        check( "前导零原文", numKeep.getStr( "v" ) == "0123" )
        check( "科学计数原文", numKeep.getStr( "w" ) == "1e3" )
    }

    # 文档边界/顶层标量/引号内 # 
    static testDocBoundary()
    {
        global.println( "----- testDocBoundary -----" )
        BaseYaml d1 = BaseYaml( "---\na: 1\n" )
        check( "前导 --- 跳过", d1.getStr( "a" ) == "1" )

        BaseYaml d2 = BaseYaml( "a: 1\n...\nb: 2\n" )
        check( "... 文档边界停止", d2.getStr( "a" ) == "1" && !d2.has( "b" ) )

        BaseYaml d3 = BaseYaml( "just plain\n" )
        check( "顶层标量文档", d3.root._value == "just plain" )
        check( "顶层标量无孩子", d3.memberCount == 0 )

        # 引号内 # 非注释；plain 尾 # 注释截断
        BaseYaml d4 = BaseYaml( "q: \"a # b\"\nplain: x # c\n" )
        check( "引号内 # 保留", d4.getStr( "q" ) == "a # b" )
        check( "plain 尾注释剥除", d4.getStr( "plain" ) == "x" )

        # fromTree 门面
        Tree<string> t = Tree<string>()
        t.addRoot( "wrap" )
        BaseYaml f = BaseYaml.fromTree( t )
        check( "fromTree 根", f.root != null )
        t.root.addChild( "k", "v" )
        check( "fromTree 序列化", f.toYaml() == "{\"k\":\"v\"}" )
    }

    # 序列化往返：flow 精确文本/再解析等价/pretty 等价/与 Json 互通
    static testRoundTrip()
    {
        global.println( "----- testRoundTrip -----" )
        string text = "name: alice\nage: 30\nvip: true\nnote: null\ntags: [a, b]\ninner:\n  k: v\n  n: 1.5\n"
        BaseYaml y = BaseYaml( text )
        string flow = y.toYaml()
        check( "flow 精确文本", flow == "{\"name\":\"alice\",\"age\":30,\"vip\":true,\"note\":null,\"tags\":[\"a\",\"b\"],\"inner\":{\"k\":\"v\",\"n\":1.5}}" )
        check( "toString == toYaml", y.toString() == flow )

        BaseYaml y2 = BaseYaml( flow )
        check( "flow 再解析往返一致", y2.toYaml() == flow )
        check( "flow 再解析取值", y2.getStr( "inner/k" ) == "v" )

        string pretty = y.toYamlPretty()
        check( "pretty 块风格非 flow", pretty != flow )
        BaseYaml y3 = BaseYaml( pretty )
        check( "pretty 再解析等价", y3.toYaml() == flow )

        # 需要引号的 plain 序列化往返
        BaseYaml q = BaseYaml( "k: has space\nh: \"yes\" # c\n" )
        check( "含空格值加引号", q.toYaml() == "{\"k\":\"has space\",\"h\":\"yes\"}" )

        # 与 Json 互通（flow 输出即 JSON 文本）
        BaseJson j = BaseJson( flow )
        check( "Json 互通解析", j.getStr( "name" ) == "alice" )
        check( "Json 互通嵌套", j.getStr( "inner/k" ) == "v" )
        check( "Json 互通类型化", j.getInt( "age" ) == 30 )
    }

    # Text.Yaml（Std）：saveAs -> load 往返 -> 关联文件信息
    static testYamlFile()
    {
        global.println( "----- testYamlFile -----" )
        string path = "yaml_test_out.yaml"

        var yf = Text.Yaml( "name: yaml\nport: 9999\nlist:\n  - a\n  - b\n" )
        check( "saveAs", yf.saveAs( path ) )
        check( "path 记录", yf.path == path )
        check( "fileExists", yf.fileExists )
        check( "fileSize > 0", yf.fileSize() > 0 )

        var loaded = Text.Yaml.load( path )
        check( "load 非空", loaded.isNotEmpty )
        check( "load 取值", loaded.getStr( "name" ) == "yaml" )
        check( "load 类型化取值", loaded.getInt( "port" ) == 9999 )
        check( "load 序列", loaded.getStr( "list/1" ) == "b" )

        check( "reload", loaded.reload() )

        check( "savePretty", loaded.savePretty() )
        var pretty = Text.Yaml.load( path )
        check( "pretty 再加载", pretty.getStr( "list/0" ) == "a" )
        check( "saveAsPretty", pretty.saveAsPretty( path ) )
        check( "save", pretty.save() )

        # 失败路径
        var miss = Text.Yaml.load( "no_such_yaml_file.yaml" )
        check( "缺失文件空实例", miss.isEmpty )
        check( "readFrom 缺失文件", !loaded.readFrom( "no_such_yaml_file.yaml" ) )
        check( "save 未关联路径", !miss.save() )
    }

    # 非法输入：解析失败返回空实例且 parseText 保持原树
    static testFailures()
    {
        global.println( "----- testFailures -----" )
        BaseYaml e1 = BaseYaml( "" )
        check( "空文本", e1.isEmpty )
        BaseYaml e2 = BaseYaml( null )
        check( "null 文本", e2.isEmpty )

        BaseYaml f1 = BaseYaml( "a:\n\tb: 1\n" )
        check( "tab 缩进非法", f1.isEmpty )
        BaseYaml f2 = BaseYaml( "a: 1\n  b: 2\n" )
        check( "跳级缩进非法", f2.isEmpty )
        BaseYaml f3 = BaseYaml( "a: [1, 2\n" )
        check( "未闭合 flow", f3.isEmpty )
        BaseYaml f4 = BaseYaml( "a: \"abc\n" )
        check( "未闭合引号", f4.isEmpty )
        BaseYaml f5 = BaseYaml( "a: 1\nb: [x\n" )
        check( "跨行未闭合 flow", f5.isEmpty )
        BaseYaml f6 = BaseYaml( "just plain\nmore\n" )
        check( "顶层标量多行非法", f6.isEmpty )

        # parseText 失败不清空原内容
        BaseYaml keep = BaseYaml( "k: v\n" )
        bool kept = !keep.parseText( "a: [" ) && keep.getStr( "k" ) == "v"
        check( "parseText 失败保持原树", kept )
    }
}
