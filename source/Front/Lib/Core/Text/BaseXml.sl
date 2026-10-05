#XML 文本门面：基于 Tree<string> 树容器实现的 XML 文本解析与序列化工具。
#只处理文本，不做文件读写；文件相关功能见 Std/Text/Xml.sl。
#解析与序列化由 VM 层 system_method_call 完成，本类只做树容器门面。
#
#弱类型映射约定（TreeNode<string>）：
#  元素节点：_name = 标签名，_value = 元素直接文本（无文本为 null；
#            多段文本拼接，CDATA 并入，已反转义）。
#  属性节点：元素的孩子，_name = "@" + 属性名，_value = 属性值文本；
#            解析时排在子元素之前（构建时顺序不限，序列化统一先输出属性）。
#  声明/DOCTYPE/注释/处理指令：解析时跳过，序列化不输出。
#
#路径语法沿用 Tree.select：按 _name 定位孩子，纯数字段视为孩子下标，
#  如 "user/name"、"items/0/id"、"user/@id"（属性定位）。
#
#典型用法：BaseXml x = BaseXml("<user id=\"1\"><name>alice</name></user>")
#          x.getText("user/name", "") / x.getAttr("user", "id", "") / x.toString()。
public class BaseXml extends Object
{
    Tree<string> _tree = new()

    # ---- 构造 ----

    override _init_()
    {
    }

    void _init_( string xmlText )
    {
        if xmlText == null { ret }
        this.parseText(xmlText)
    }

    void _init_( Tree<string> tree )
    {
        if tree != null
        {
            this._tree = tree
        }
    }

    # ---- 静态工厂 ----

    public static BaseXml parse( string xmlText )
    {
        ret BaseXml(xmlText)
    }

    public static BaseXml fromTree( Tree<string> tree )
    {
        ret BaseXml(tree)
    }

    # ---- 解析 ----

    #解析 XML 文本并替换当前树；文本为空或非法时返回 false 且不清空原内容。
    public bool parseText( string xmlText )
    {
        TreeNode<string> proto = TreeNode<string>()
        TreeNode<string> root = SystemXmlParse(proto, xmlText) as TreeNode<string>
        if root == null { ret false }
        this._tree.setRoot(root)
        ret true
    }

    # ---- 基础属性 ----

    #内部树容器（活引用，可用于深度遍历与结构操作）。
    get Tree<string> tree()
    {
        ret this._tree
    }

    #根节点（空文档时为 null）。
    get TreeNode<string> root()
    {
        ret this._tree.root
    }

    #根标签名（空文档时为 null）。
    get string rootName()
    {
        ret this._tree.rootName
    }

    get bool isEmpty()
    {
        ret this._tree.isEmpty
    }

    get bool isNotEmpty()
    {
        ret this._tree.isNotEmpty
    }

    #根元素直接孩子数（属性孩子 + 子元素）。
    get int memberCount()
    {
        TreeNode<string> root = this._tree.root
        if root == null { ret 0 }
        ret root.childCount
    }

    #全树节点总数。
    get int nodeCount()
    {
        ret this._tree.length
    }

    get int height()
    {
        ret this._tree.height
    }

    # ---- 树操作透传 ----

    #设置根节点（注意：节点会先脱离原父节点，即从原树中摘除）。
    public void setRoot( TreeNode<string> node )
    {
        if node == null { ret }
        this._tree.setRoot(node)
    }

    #按路径选取节点，如 "user/name"、"items/0/id"、"user/@id"；未命中返回 null。
    public TreeNode<string> select( string path )
    {
        ret this._tree.select(path)
    }

    #按名称在全树中查找第一个匹配节点；未命中返回 null。
    public TreeNode<string> findByName( string name )
    {
        ret this._tree.findByName(name)
    }

    public bool has( string path )
    {
        ret this.select(path) != null
    }

    public void clear()
    {
        this._tree.clear()
    }

    # ---- 节点类型判定 ----

    #属性孩子名（"@" + 属性名），孩子定位与属性构建共用。
    static string attrName( string name )
    {
        ret "@" + name
    }

    #是否元素节点（_name 非空且不以 "@" 开头）。
    public static bool isElement( TreeNode<string> node )
    {
        if node == null { ret false }
        if node._name == null || node._name.length == 0 { ret false }
        ret SystemStringCharCodeAt( node._name, 0 ) != 64
    }

    #是否属性节点（_name 以 "@" 开头）。
    public static bool isAttr( TreeNode<string> node )
    {
        if node == null { ret false }
        if node._name == null || node._name.length == 0 { ret false }
        ret SystemStringCharCodeAt( node._name, 0 ) == 64
    }

    # ---- 元素文本（路径未命中或无文本时返回默认值）----

    public string getText( string path )
    {
        ret this.getText(path, "")
    }

    #取 path 元素的直接文本；未命中或无文本返回默认值。
    public string getText( string path, string defaultValue )
    {
        TreeNode<string> node = this.select(path)
        if node == null { ret defaultValue }
        if node._value == null { ret defaultValue }
        ret node._value
    }

    # ---- 属性操作 ----

    public string getAttr( string path, string name )
    {
        ret this.getAttr(path, name, "")
    }

    #取 path 元素的 name 属性值；元素缺失/无该属性时返回默认值。
    public string getAttr( string path, string name, string defaultValue )
    {
        TreeNode<string> node = this.select(path)
        if node == null { ret defaultValue }
        if name == null || name.length == 0 { ret defaultValue }
        TreeNode<string> attr = node.childByName( BaseXml.attrName(name) )
        if attr == null { ret defaultValue }
        if attr._value == null { ret defaultValue }
        ret attr._value
    }

    #path 元素是否含有 name 属性。
    public bool hasAttr( string path, string name )
    {
        TreeNode<string> node = this.select(path)
        if node == null { ret false }
        if name == null || name.length == 0 { ret false }
        ret node.childByName( BaseXml.attrName(name) ) != null
    }

    #设置 path 元素的 name 属性（已有属性改值，没有则新建属性孩子）。
    public bool setAttr( string path, string name, string value )
    {
        TreeNode<string> node = this.select(path)
        if node == null { ret false }
        if name == null || name.length == 0 { ret false }
        TreeNode<string> attr = node.childByName( BaseXml.attrName(name) )
        if attr != null
        {
            attr._value = value
            ret true
        }
        ret node.addChild( BaseXml.attrName(name), value ) != null
    }

    # ---- 元素构建 ----

    #创建并设置根元素，返回新根节点。
    public TreeNode<string> addRootElement( string name )
    {
        if name == null || name.length == 0 { ret null }
        ret this._tree.addRoot(name)
    }

    #在 path 元素下创建并挂接无文本子元素。
    public TreeNode<string> addElement( string path, string name )
    {
        TreeNode<string> parent = this.select(path)
        if parent == null { ret null }
        ret parent.addChild(name)
    }

    #在 path 元素下创建并挂接带文本子元素。
    public TreeNode<string> addElement( string path, string name, string text )
    {
        TreeNode<string> parent = this.select(path)
        if parent == null { ret null }
        ret parent.addChild(name, text)
    }

    # ---- 类型化取值（元素文本；路径未命中或值为 null 时返回默认值）----

    public bool getBool( string path )
    {
        ret this.getBool(path, false)
    }

    public bool getBool( string path, bool defaultValue )
    {
        TreeNode<string> node = this.select(path)
        if node == null { ret defaultValue }
        if node._value == null { ret defaultValue }
        ret node._value == "true"
    }

    #整数取值：解析失败（如非数字文本）按 0 处理。
    public int getInt( string path )
    {
        ret this.getInt(path, 0)
    }

    public int getInt( string path, int defaultValue )
    {
        TreeNode<string> node = this.select(path)
        if node == null { ret defaultValue }
        if node._value == null { ret defaultValue }
        ret SystemInt32Parse(node._value)
    }

    public Float64 getFloat( string path )
    {
        ret this.getFloat(path, 0.0d)
    }

    public Float64 getFloat( string path, Float64 defaultValue )
    {
        TreeNode<string> node = this.select(path)
        if node == null { ret defaultValue }
        if node._value == null { ret defaultValue }
        ret SystemConvertFloat64(node._value)
    }

    # ---- 序列化 ----

    public string toXml()
    {
        ret SystemXmlToString(this._tree.root, false)
    }

    public string toXmlPretty()
    {
        ret SystemXmlToString(this._tree.root, true)
    }

    override string toString()
    {
        ret this.toXml()
    }
}
