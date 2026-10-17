import Std;
#Xml 测试：BaseXml（Core）纯文本解析/属性与文本取值/实体与 CDATA/构建与序列化，
#Text.Xml（Std）文件加载/保存往返，非法输入失败路径。

XmlTest
{
    static fun()
    {
        global.println("===== XmlTest start =====")
        testBaseXmlText()
        testEntities()
        testWhitespace()
        testBuild()
        testFailures()
        testXmlFile()
        global.println("===== XmlTest end =====")
    }

    # 断言辅助：OK/FAIL 单行输出
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[Xml] " + name + " : OK" )
        }
        else
        {
            global.println( "[Xml] " + name + " : FAIL" )
        }
    }

    # BaseXml：文本解析 -> 属性/文本/类型化取值 -> 树透传 -> 往返序列化
    static testBaseXmlText()
    {
        global.println( "----- testBaseXmlText -----" )
        # 声明/注释跳过；属性双挂接；空元素；嵌套子元素
        string text = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><!-- 注释 --><user id=\"1\" vip=\"true\"><name>alice</name><age>30</age><score>95.5</score><tags><t>a</t><t>b</t></tags><note/></user>"

        BaseXml x = BaseXml( text )
        check( "解析成功非空", x.isNotEmpty )
        check( "根标签名", x.rootName == "user" )
        check( "根直接孩子数 7", x.memberCount == 7 )      # @id @vip name age score tags note
        check( "全树节点数 10", x.nodeCount == 10 )

        # 属性取值
        check( "getAttr id", x.getAttr( "user", "id" ) == "1" )
        check( "getAttr vip", x.getAttr( "user", "vip" ) == "true" )
        check( "hasAttr 命中", x.hasAttr( "user", "vip" ) )
        check( "hasAttr 未命中", !x.hasAttr( "user", "none" ) )
        check( "getAttr 默认值(缺属性)", x.getAttr( "user", "none", "d" ) == "d" )
        check( "getAttr 默认值(缺元素)", x.getAttr( "nobody", "id", "d" ) == "d" )

        # 文本与类型化取值
        check( "getText", x.getText( "user/name" ) == "alice" )
        check( "getInt", x.getInt( "user/age" ) == 30 )
        check( "getFloat", x.getFloat( "user/score" ) == 95.5d )
        check( "下标取子元素 0", x.getText( "user/tags/0" ) == "a" )
        check( "下标取子元素 1", x.getText( "user/tags/1" ) == "b" )
        check( "空元素文本默认", x.getText( "user/note", "def" ) == "def" )
        check( "缺失路径文本默认", x.getText( "user/zzz", "def" ) == "def" )

        # 树透传
        check( "has 命中", x.has( "user/name" ) )
        check( "has 未命中", !x.has( "user/zzz" ) )
        check( "findByName", x.findByName( "t" )._value == "a" )

        # 节点类型判定
        TreeNode<string> attrNode = x.select( "user/@id" )
        check( "属性路径定位", attrNode != null )
        check( "isAttr 判定", BaseXml.isAttr( attrNode ) )
        check( "isElement 判定", !BaseXml.isElement( attrNode ) )
        check( "元素 isElement", BaseXml.isElement( x.select( "user/name" ) ) )

        # 往返序列化
        string compact = x.toXml()
        check( "toString == toXml", x.toString() == compact )
        check( "往返文本一致", compact == "<user id=\"1\" vip=\"true\"><name>alice</name><age>30</age><score>95.5</score><tags><t>a</t><t>b</t></tags><note/></user>" )
        BaseXml again = BaseXml( compact )
        check( "二次解析往返一致", again.toXml() == compact )
        check( "pretty 更长", x.toXmlPretty().length > compact.length )
        BaseXml prettyBack = BaseXml( x.toXmlPretty() )
        check( "pretty 再解析等价", prettyBack.toXml() == compact )

        # 同名属性：后者覆盖前者，不重复挂接
        BaseXml dup = BaseXml( "<r a=\"1\" a=\"2\"/>" )
        check( "同名属性后者覆盖", dup.getAttr( "r", "a" ) == "2" )
        check( "同名属性不重复挂接", dup.memberCount == 1 )

        # 空实例与静态工厂
        BaseXml empty = BaseXml()
        check( "空实例 isEmpty", empty.isEmpty )
        check( "静态 parse", BaseXml.parse( "<r/>" ).rootName == "r" )
    }

    # 实体/CDATA/字符引用/DOCTYPE 跳过/属性引号转义
    static testEntities()
    {
        global.println( "----- testEntities -----" )
        BaseXml x = BaseXml( "<?xml version=\"1.0\"?><!DOCTYPE root [<!ENTITY e \"y\">]><root><a>&lt;hi&gt;&amp;</a><b><![CDATA[x < y]]></b><c>&#65;&#x42;</c><d q=\"a&quot;b&quot;c\"/></root>" )
        check( "DOCTYPE 后解析成功", x.isNotEmpty )
        check( "命名实体反转义", x.getText( "root/a" ) == "<hi>&" )
        check( "CDATA 原文并入", x.getText( "root/b" ) == "x < y" )
        check( "字符引用(十进制/十六进制)", x.getText( "root/c" ) == "AB" )
        check( "属性引号转义", x.getAttr( "root/d", "q" ) == "a\"b\"c" )

        # 序列化再转义 + 再解析还原
        string round = x.toXml()
        check( "文本再转义往返", BaseXml( round ).getText( "root/a" ) == "<hi>&" )
        check( "CDATA 转文本往返", BaseXml( round ).getText( "root/b" ) == "x < y" )
        check( "属性再转义往返", BaseXml( round ).getAttr( "root/d", "q" ) == "a\"b\"c" )

        # 未声明实体原样保留
        BaseXml u = BaseXml( "<r>&foo;</r>" )
        check( "未声明实体保留", u.getText( "r" ) == "&foo;" )
    }

    # 空白语义：首尾裁剪 / 纯空白视作无文本 / 混合内容文本先出
    static testWhitespace()
    {
        global.println( "----- testWhitespace -----" )
        BaseXml t = BaseXml( "<r>  hello  </r>" )
        check( "文本首尾裁剪", t.getText( "r" ) == "hello" )

        BaseXml w = BaseXml( "<r>\n  <a>1</a>\n</r>" )
        check( "纯空白文本为 null", w.getText( "r", "none" ) == "none" )
        check( "子元素文本", w.getText( "r/a" ) == "1" )

        BaseXml m = BaseXml( "<r>text<c/></r>" )
        check( "混合内容文本", m.getText( "r" ) == "text" )
        check( "混合内容序列化(文本先出)", m.toXml() == "<r>text<c/></r>" )

        BaseXml s = BaseXml( "<r/>" )
        check( "自闭合无文本", s.getText( "r", "none" ) == "none" )
        check( "自闭合序列化", s.toXml() == "<r/>" )
    }

    # 构建：addRootElement/addElement/setAttr -> 序列化与取值
    static testBuild()
    {
        global.println( "----- testBuild -----" )
        BaseXml b = BaseXml()
        check( "addRootElement", b.addRootElement( "config" ) != null )
        check( "setAttr 新建属性", b.setAttr( "", "version", "1.0" ) )
        b.addElement( "", "host", "127.0.0.1" )
        b.addElement( "", "port", "8080" )
        check( "setAttr 子元素属性", b.setAttr( "config/host", "if", "eth0" ) )
        b.addElement( "", "esc", "a<b>&c" )
        check( "构建序列化", b.toXml() == "<config version=\"1.0\"><host if=\"eth0\">127.0.0.1</host><port>8080</port><esc>a&lt;b&gt;&amp;c</esc></config>" )
        check( "构建取值", b.getText( "config/host" ) == "127.0.0.1" )
        check( "构建属性", b.getAttr( "config/host", "if" ) == "eth0" )
        check( "setAttr 改值", b.setAttr( "config", "version", "2.0" ) )
        check( "改值生效", b.getAttr( "config", "version" ) == "2.0" )
        check( "addElement 失败路径", b.addElement( "nope", "x" ) == null )
        check( "setAttr 失败路径", !b.setAttr( "nope", "x", "y" ) )

        # fromTree 门面
        Tree<string> t = Tree<string>()
        t.addRoot( "wrap" )
        BaseXml f = BaseXml.fromTree( t )
        check( "fromTree 根名", f.rootName == "wrap" )
        t.root.addChild( "inner", "v" )
        check( "fromTree 序列化", f.toXml() == "<wrap><inner>v</inner></wrap>" )

        # clear
        BaseXml c = BaseXml( "<r/>" )
        c.clear()
        check( "clear 后 isEmpty", c.isEmpty )
    }

    # 非法输入：解析失败返回 null 且 parseText 为 false
    static testFailures()
    {
        global.println( "----- testFailures -----" )
        BaseXml e1 = BaseXml( "" )
        check( "空文本", e1.isEmpty )
        BaseXml e2 = BaseXml( null )
        check( "null 文本", e2.isEmpty )

        BaseXml f1 = BaseXml( "<a><b></a>" )
        check( "闭标签失配", f1.isEmpty )
        BaseXml f2 = BaseXml( "<a></b>" )
        check( "标签不匹配", f2.isEmpty )
        BaseXml f3 = BaseXml( "<a>" )
        check( "未闭合元素", f3.isEmpty )
        BaseXml f4 = BaseXml( "plain text" )
        check( "无根元素", f4.isEmpty )
        BaseXml f5 = BaseXml( "<a/>tail" )
        check( "尾随内容", f5.isEmpty )
        BaseXml f6 = BaseXml( "<!-- x<a/>" )
        check( "未闭合注释", f6.isEmpty )

        # parseText 失败不清空原内容
        BaseXml keep = BaseXml( "<k>v</k>" )
        bool kept = !keep.parseText( "<bad>" ) && keep.getText( "k" ) == "v"
        check( "parseText 失败保持原树", kept )
    }

    # Text.Xml（Std）：saveAs -> load 往返 -> 关联文件信息
    static testXmlFile()
    {
        global.println( "----- testXmlFile -----" )
        string path = "xml_test_out.xml"

        var xf = Text.Xml( "<root a=\"1\"><x>10</x><y>hello</y></root>" )
        check( "saveAs", xf.saveAs( path ) )
        check( "path 记录", xf.path == path )
        check( "fileExists", xf.fileExists )
        check( "fileSize > 0", xf.fileSize() > 0 )

        var loaded = Text.Xml.load( path )
        check( "load 非空", loaded.isNotEmpty )
        check( "load 属性", loaded.getAttr( "root", "a" ) == "1" )
        check( "load 文本", loaded.getText( "root/y" ) == "hello" )
        check( "load 类型化取值", loaded.getInt( "root/x" ) == 10 )

        check( "reload", loaded.reload() )

        check( "savePretty", loaded.savePretty() )
        var pretty = Text.Xml.load( path )
        check( "pretty 再加载", pretty.getText( "root/y" ) == "hello" )

        # 失败路径
        var miss = Text.Xml.load( "no_such_xml_file.xml" )
        check( "缺失文件空实例", miss.isEmpty )
        check( "readFrom 缺失文件", !loaded.readFrom( "no_such_xml_file.xml" ) )
        check( "save 未关联路径", !miss.save() )
    }
}
