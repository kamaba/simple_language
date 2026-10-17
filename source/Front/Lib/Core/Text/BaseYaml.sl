#YAML 文本门面：基于 Tree<string> 树容器实现的 YAML 文本解析与序列化工具。
#只处理文本，不做文件读写；文件相关功能见 Std/Text/Yaml.sl。
#解析与序列化由 VM 层 system_method_call（自研行驱动解析器）完成，
#本类只做树容器门面。
#
#弱类型映射约定（TreeNode<string>，与 BaseJson 一致，YAML 是 JSON 超集）：
#  mapping  节点：_value = "{}"，每个成员为一个孩子节点，孩子 _name = 成员键名。
#  sequence 节点：_value = "[]"，每个元素为一个孩子节点，孩子 _name = ""（空串）。
#  string   节点：_value = 原文（引号已剥、转义已反转义）。
#  number   节点：_value = 原文（不做数值转换，弱类型无损保留）。
#  bool     节点：_value = "true" / "false"（小写规范化）。
#  null     节点：_value = null。
#
#路径语法沿用 Tree.select：按 _name 定位孩子，纯数字段视为孩子下标，
#  如 "user/name"、"list/0/id"、"data/items/2"。
#
#序列化：toYaml() 为 flow 单行风格（与 JSON 文本互通可再解析），
#  toYamlPretty() 为块风格 2 空格缩进多行。
#
#典型用法：BaseYaml y = BaseYaml("name: alice\nage: 30")
#          y.getStr("name", "") / y.getInt("age", 0) / y.toYamlPretty()。
public class BaseYaml extends Object
{
    Tree<string> _tree = new()

    # ---- 构造 ----

    override _init_()
    {
    }

    void _init_( string yamlText )
    {
        if yamlText == null { ret }
        this.parseText(yamlText)
    }

    void _init_( Tree<string> tree )
    {
        if tree != null
        {
            this._tree = tree
        }
    }

    # ---- 静态工厂 ----

    public static BaseYaml parse( string yamlText )
    {
        ret BaseYaml(yamlText)
    }

    public static BaseYaml fromTree( Tree<string> tree )
    {
        ret BaseYaml(tree)
    }

    # ---- 解析 ----

    #解析 YAML 文本并替换当前树；文本为空或非法时返回 false 且不清空原内容。
    public bool parseText( string yamlText )
    {
        TreeNode<string> proto = TreeNode<string>()
        TreeNode<string> root = SystemYamlParse(proto, yamlText) as TreeNode<string>
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

    get bool isEmpty()
    {
        ret this._tree.isEmpty
    }

    get bool isNotEmpty()
    {
        ret this._tree.isNotEmpty
    }

    #顶层成员数（顶层为 mapping 时是成员数，为 sequence 时是元素数）。
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

    #按路径选取节点，如 "user/name"、"list/0/id"；未命中返回 null。
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

    public static bool isObject( TreeNode<string> node )
    {
        if node == null { ret false }
        ret node._value == "{}"
    }

    public static bool isArray( TreeNode<string> node )
    {
        if node == null { ret false }
        ret node._value == "[]"
    }

    public static bool isNull( TreeNode<string> node )
    {
        if node == null { ret false }
        ret node._value == null
    }

    # ---- 类型化取值（路径未命中或值为 null 时返回默认值）----

    public string getStr( string path )
    {
        ret this.getStr(path, "")
    }

    public string getStr( string path, string defaultValue )
    {
        TreeNode<string> node = this.select(path)
        if node == null { ret defaultValue }
        if node._value == null { ret defaultValue }
        ret node._value
    }

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

    #整数取值：解析失败（如非数字文本）按默认值处理。
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

    #flow 单行风格（{k: v, ...} / [a, b]，与 JSON 文本互通可再解析）。
    public string toYaml()
    {
        ret SystemYamlToString(this._tree.root, false)
    }

    #块风格 2 空格缩进多行（文档尾随一个换行）。
    public string toYamlPretty()
    {
        ret SystemYamlToString(this._tree.root, true)
    }

    override string toString()
    {
        ret this.toYaml()
    }
}
