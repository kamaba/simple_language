#JSON 文本门面：基于 Tree<string> 树容器实现的 JSON 文本解析与序列化工具。
#只处理文本，不做文件读写；文件相关功能见 Std/Text/Json.sl。
#解析与序列化由 VM 层 system_method_call（yyjson）完成，本类只做树容器门面。
#
#弱类型映射约定（TreeNode<string>）：
#  object 节点：_value = "{}"，每个成员为一个孩子节点，孩子 _name = 成员键名。
#  array  节点：_value = "[]"，每个元素为一个孩子节点，孩子 _name = ""（空串）。
#  string 节点：_value = 原文（已反转义）。
#  number 节点：_value = 最短往返十进制文本。
#  bool   节点：_value = "true" / "false"。
#  null   节点：_value = null。
#
#路径语法沿用 Tree.select：按 _name 定位孩子，纯数字段视为孩子下标，
#  如 "user/name"、"list/0/id"、"data/items/2"。
#
#典型用法：BaseJson j = BaseJson("{\"name\":\"alice\",\"age\":30}")
#          j.getStr("name", "") / j.getInt("age", 0) / j.toString()。
public class BaseJson extends Object
{
    Tree<string> _tree = new()

    # ---- 构造 ----

    override _init_()
    {
    }

    void _init_( string jsonText )
    {
        if jsonText == null { ret }
        this.parseText(jsonText)
    }

    void _init_( Tree<string> tree )
    {
        if tree != null
        {
            this._tree = tree
        }
    }

    # ---- 静态工厂 ----

    public static BaseJson parse( string jsonText )
    {
        ret BaseJson(jsonText)
    }

    public static BaseJson fromTree( Tree<string> tree )
    {
        ret BaseJson(tree)
    }

    # ---- 解析 ----

    #解析 JSON 文本并替换当前树；文本为空或非法时返回 false 且不清空原内容。
    public bool parseText( string jsonText )
    {
        TreeNode<string> proto = TreeNode<string>()
        TreeNode<string> root = SystemJsonParse(proto, jsonText) as TreeNode<string>
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

    #顶层成员数（顶层为 object 时是成员数，为 array 时是元素数）。
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

    public string toJson()
    {
        ret SystemJsonToString(this._tree.root, false)
    }

    public string toJsonPretty()
    {
        ret SystemJsonToString(this._tree.root, true)
    }

    override string toString()
    {
        ret this.toJson()
    }
}
