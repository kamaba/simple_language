#JSON 层次化节点视图：包装 TreeNode<string> 的弱类型 JSON 节点，
#提供多层嵌套导航与类型化取值，弥补 BaseJson 路径取值（select）之外的
#层次化访问能力（BaseJson 的弱类型映射约定见其文件头注释）。
#
#设计要点：
#   - JsonValue 是树节点的活引用视图（非拷贝），树结构变化实时可见；
#   - 缺失访问安全：_getItem_ 未命中不返回 null 引用，而是返回一个
#     包装 null 节点的 JsonValue（isNull == true），链式导航不中断：
#         jv["user"]["name"].asInt32()   # 缺 "user" 时得默认值 0
#   - 存在性判断用 has(name) / isNull，不要对 _getItem_ 结果判 null；
#   - foreach 直接遍历本节点的直接孩子（数组元素或 object 成员）。
#
#典型用法：
#   JsonValue jv = JsonValue.parse("{\"user\":{\"name\":\"alice\",\"age\":30}}")
#   jv["user"]["name"].asString()      # "alice"
#   jv["user"]["age"].asInt32()        # 30
#   for item in jv["user"]             # 遍历 user 的成员
#   jv["list"][0].asFloat64()
#   jv["obj"].asMap() / jv["list"].asArray() / jv["list"].asList()
#   jv["data"].asData<MyData>()
#   jv["obj"].toJsonPretty()
#
#复用既有系统调用（data<->json 与 BaseJson 已验证的路径）：
#   SystemTreeChildByName / SystemTreeChildAt  子元素定位
#   SystemJsonIsNumberText                      数字文本判定（本批新增）
#   SystemJsonParseInt64                        Int64 文本解析（本批新增）
#   SystemInt32Parse / SystemConvertFloat64     标量解析
#   SystemJsonToData / SystemJsonToString       data 转换与序列化
public class JsonValue extends Object interface Core.IIterable<JsonValue>, Core.IIterator<JsonValue>
{
    # --- 核心字段 ---
    TreeNode<string> _node = null                     # 被包装的树节点（null 包装 = 缺失/null）

    # --- 迭代器字段（直接孩子序）---
    TreeNode<string> _iterNode = null
    int _index = -1
    JsonValue _current = null

    # ---- 构造 ----

    override _init_()
    {
    }

    #以树节点构造（node 可为 null，构造缺失视图）
    void _init_( TreeNode<string> node )
    {
        this._node = node
    }

    # ---- 静态工厂 ----

    #包装树节点（node 为 null 时返回 isNull 的缺失视图，不返回 null 引用）
    public static JsonValue wrap( TreeNode<string> node )
    {
        JsonValue jv = new()
        jv._node = node
        ret jv
    }

    #解析 JSON 文本为根节点视图；文本为空或非法时返回缺失视图
    public static JsonValue parse( string jsonText )
    {
        TreeNode<string> proto = TreeNode<string>()
        TreeNode<string> root = SystemJsonParse( proto, jsonText ) as TreeNode<string>
        ret JsonValue.wrap( root )
    }

    # ---- 基础属性 ----

    #被包装的原始树节点（缺失视图为 null；树结构操作的逃生口）
    get TreeNode<string> node()
    {
        ret this._node
    }

    #节点名（object 成员的键名；数组元素为空串，根/缺失为 null）
    get string name()
    {
        if this._node == null
        {
            ret null
        }
        ret this._node._name
    }

    #直接孩子数（object 的成员数或 array 的元素数）
    get int length()
    {
        if this._node == null
        {
            ret 0
        }
        ret this._node._childCount
    }

    #是否为存在的节点（缺失视图为 false）
    get bool exists()
    {
        ret this._node != null
    }

    # ---- 节点类型判定（弱类型标记约定与 BaseJson 一致）----

    #JSON null（或缺失视图）
    get bool isNull()
    {
        if this._node == null
        {
            ret true
        }
        ret this._node._value == null
    }

    get bool isObject()
    {
        if this._node == null
        {
            ret false
        }
        ret this._node._value == "{}"
    }

    get bool isArray()
    {
        if this._node == null
        {
            ret false
        }
        ret this._node._value == "[]"
    }

    get bool isBool()
    {
        if this._node == null
        {
            ret false
        }
        string v = this._node._value
        if v == "true"
        {
            ret true
        }
        ret v == "false"
    }

    #数字叶子（严格 JSON 数字语法，复用 C 层序列化方向的判定）
    get bool isNumber()
    {
        if this._node == null
        {
            ret false
        }
        string v = this._node._value
        if v == null || v == "{}" || v == "[]"
        {
            ret false
        }
        ret SystemJsonIsNumberText( v )
    }

    #字符串叶子（排除 null/object/array/bool/number 之后的剩余情形）
    get bool isString()
    {
        if this._node == null
        {
            ret false
        }
        string v = this._node._value
        if v == null || v == "{}" || v == "[]"
        {
            ret false
        }
        if v == "true" || v == "false"
        {
            ret false
        }
        if SystemJsonIsNumberText( v )
        {
            ret false
        }
        ret true
    }

    #叶子原始文本（null/object/array/缺失返回 null，供 asXxx 内部使用）
    string _leafText()
    {
        if this._node == null
        {
            ret null
        }
        string v = this._node._value
        if v == null || v == "{}" || v == "[]"
        {
            ret null
        }
        ret v
    }

    # ---- 子元素访问（多层嵌套导航核心）----

    #孩子节点包装（实例内构造，避免实例方法走类名静态解析）
    JsonValue _wrapChild( TreeNode<string> child )
    {
        JsonValue jv = new()
        jv._node = child
        ret jv
    }

    #按键名取 object 成员：jv["name"] / jv.$"name"。
    #未命中返回 isNull 缺失视图（链式导航安全，存在性判断用 has）
    public override JsonValue _getItem_( string name )
    {
        if this._node != null && name != null
        {
            ret this._wrapChild( this._node.childByName( name ) )
        }
        ret this._wrapChild( null )
    }

    #按下标取 array 元素：jv[0] / jv.$0。越界返回 isNull 缺失视图
    public override JsonValue _getItem_( int index )
    {
        if this._node != null && index >= 0
        {
            ret this._wrapChild( this._node.childAt( index ) )
        }
        ret this._wrapChild( null )
    }

    #是否含有指定键名的直接孩子
    public bool has( string name )
    {
        if this._node == null || name == null
        {
            ret false
        }
        ret this._node.childByName( name ) != null
    }

    #下标是否在直接孩子范围内
    public bool has( int index )
    {
        if this._node == null
        {
            ret false
        }
        ret index >= 0 && index < this._node._childCount
    }

    #路径导航（沿用 Tree.select 语法："user/address"、"list/0/id"），
    #未命中返回 isNull 缺失视图
    public JsonValue select( string path )
    {
        if this._node == null || path == null
        {
            ret this._wrapChild( null )
        }
        ret this._wrapChild( this._node.select( path ) )
    }

    #直接孩子视图数组（object 成员与 array 元素通用）
    public Array<JsonValue> children()
    {
        if this._node == null
        {
            ret Array<JsonValue>( 0 )
        }
        int n = this._node._childCount
        Array<JsonValue> arr = Array<JsonValue>( n )
        TreeNode<string> c = this._node._firstChild
        int i = 0
        while c != null
        {
            JsonValue jv = new()
            jv._node = c
            arr._setItem_( i, jv )
            i = i + 1
            c = c._nextSibling
        }
        ret arr
    }

    #object 成员的键名数组（数组元素场景为空串，一般配合 asMap 使用）
    public Array<string> keys()
    {
        if this._node == null
        {
            ret Array<string>( 0 )
        }
        int n = this._node._childCount
        Array<string> arr = Array<string>( n )
        TreeNode<string> c = this._node._firstChild
        int i = 0
        while c != null
        {
            if c._name == null
            {
                arr._setItem_( i, "" )
            }
            else
            {
                arr._setItem_( i, c._name )
            }
            i = i + 1
            c = c._nextSibling
        }
        ret arr
    }

    # ---- 标量转换（值缺失 / 非数字文本时返回默认值）----

    public bool asBool()
    {
        ret this.asBool( false )
    }

    public bool asBool( bool defaultValue )
    {
        string text = this._leafText()
        if text == null
        {
            ret defaultValue
        }
        if text == "true"
        {
            ret true
        }
        if text == "false"
        {
            ret false
        }
        ret defaultValue
    }

    #叶子原文（object/array/null 返回默认值，不做序列化）
    public string asString()
    {
        ret this.asString( "" )
    }

    public string asString( string defaultValue )
    {
        string text = this._leafText()
        if text == null
        {
            ret defaultValue
        }
        ret text
    }

    public int asInt32()
    {
        ret this.asInt32( 0 )
    }

    public int asInt32( int defaultValue )
    {
        string text = this._leafText()
        if text == null || !SystemJsonIsNumberText( text )
        {
            ret defaultValue
        }
        ret SystemInt32Parse( text )
    }

    public Int64 asInt64()
    {
        ret this.asInt64( 0 )
    }

    #SystemConvertInt64 不解析字符串，走本批新增的 SystemJsonParseInt64
    public Int64 asInt64( Int64 defaultValue )
    {
        string text = this._leafText()
        if text == null || !SystemJsonIsNumberText( text )
        {
            ret defaultValue
        }
        ret SystemJsonParseInt64( text )
    }

    public Float32 asFloat32()
    {
        ret this.asFloat32( 0.0f )
    }

    #先按 Float64 解析字符串再压窄到 Float32（Float32 直转不解析字符串）
    public Float32 asFloat32( Float32 defaultValue )
    {
        string text = this._leafText()
        if text == null || !SystemJsonIsNumberText( text )
        {
            ret defaultValue
        }
        Float64 d = SystemConvertFloat64( text )
        ret SystemConvertFloat32( d )
    }

    public Float64 asFloat64()
    {
        ret this.asFloat64( 0.0d )
    }

    public Float64 asFloat64( Float64 defaultValue )
    {
        string text = this._leafText()
        if text == null || !SystemJsonIsNumberText( text )
        {
            ret defaultValue
        }
        ret SystemConvertFloat64( text )
    }

    # ---- 容器转换 ----

    #转为数组视图（直接孩子逐个包装；空数组场景含 isNull 视图）
    public Array<JsonValue> asArray()
    {
        ret this.children()
    }

    #转为列表（直接孩子逐个包装，动态扩容）
    public List<JsonValue> asList()
    {
        List<JsonValue> list = new()
        if this._node != null
        {
            TreeNode<string> c = this._node._firstChild
            while c != null
            {
                JsonValue jv = new()
                jv._node = c
                list.add( jv )
                c = c._nextSibling
            }
        }
        ret list
    }

    #转为键值映射（object 成员的键名 -> JsonValue；跳过无键名的孩子）
    public Map<string, JsonValue> asMap()
    {
        Map<string, JsonValue> map = new()
        if this._node != null
        {
            TreeNode<string> c = this._node._firstChild
            while c != null
            {
                if c._name != null && c._name.length > 0
                {
                    JsonValue jv = new()
                    jv._node = c
                    map.add( c._name, jv )
                }
                c = c._nextSibling
            }
        }
        ret map
    }

    # ---- data 转换（复用 BaseJson.toData 的 C 层后端）----

    #按 typeName（data 类型全名或短名）构建 data 实例；非 data/根非 object 返回 null
    public object asData( string typeName )
    {
        ret SystemJsonToData( this._node, typeName )
    }

    #模板版本：JsonValue jv; MyData d = jv.asData<MyData>()
    public T asData<T>()
    {
        ret SystemJsonToData( this._node, T.type ) as T
    }

    # ---- 序列化（复用 BaseJson 的 C 层后端）----

    #以本节点为根序列化为紧凑 JSON 文本（缺失视图输出 "null"）
    public string toJson()
    {
        ret SystemJsonToString( this._node, false )
    }

    #以本节点为根序列化为 2 空格缩进的格式化文本
    public string toJsonPretty()
    {
        ret SystemJsonToString( this._node, true )
    }

    override string toString()
    {
        ret this.toJson()
    }

    # ---- 迭代器（foreach 遍历直接孩子，沿兄弟链 O(n) 推进）----

    override void reset()
    {
        this._index = -1
        this._current = null
        this._iterNode = null
    }

    override bool moveNext()
    {
        if this._node == null
        {
            ret false
        }
        if this._iterNode == null
        {
            this._iterNode = this._node._firstChild
        }
        else
        {
            this._iterNode = this._iterNode._nextSibling
        }
        if this._iterNode == null
        {
            this._current = null
            ret false
        }
        if this._current == null
        {
            this._current = new()
        }
        this._current._node = this._iterNode
        this._index = this._index + 1
        ret true
    }

    override get JsonValue current()
    {
        ret this._current
    }

    override get Core.IIterator<JsonValue> iterator()
    {
        this.reset()
        ret this
    }

    get int index()
    {
        ret this._index
    }
}
