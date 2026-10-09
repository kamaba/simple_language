import Std;
#Json 测试：BaseJson（Core）纯文本解析/类型化取值/序列化，
#JsonValue（Core）层次化节点视图（多层嵌套导航/asXxx 转换/容器与 data 转换/迭代），
#Text.Json（Std）文件加载/保存往返，
#data/class 实例 <-> JSON 双向转换（toJson/fromData/toData）。

# ---- data<->json 双向转换测试用类型 ----

enum JtKind
{
    None = 0
    Basic = 1
    Advanced = 2
}

#带 toJson() 的 class：正向转换时虚调 toJson() 把返回的 JSON 文本
#解析后嫁接为子树。
class JtPoint
{
    x = 0
    y = 0

    public string toJson()
    {
        ret "{\"px\":" + this.x.toString() + ",\"py\":" + this.y.toString() + "}"
    }
}

#无 toJson() 的 class：正向转换回退虚调 toString() 作为字符串叶子。
class JtTag
{
    tag = "t"

    override string toString()
    {
        ret "tag:" + this.tag
    }
}

#嵌套 data 成员类型。
data JtMeta
{
    level = 1
    passed = false
}

#可完整往返的 data：标量/string/enum/data/数组（int 数组、string 数组、
#嵌套数组）成员。反向 toData 会还原这些成员。
data JtSample
{
    sid = 7
    name = "sam"
    score = 95.5
    active = true
    kind = JtKind.Advanced
    meta = JtMeta(){ level = 3, passed = true }
    scores = [95, 88, 91]
    tags = ["math", "final"]
    nested = [[1, 2], [3, 4]]
}

#含 class 成员的 data：class 引用成员正向可转出（toJson 嫁接 /
#toString 串叶），反向不还原（保持 null），嵌套 data 成员正常还原。
data JtExtra
{
    pt = JtPoint(){ x = 3, y = 4 }
    tag = JtTag(){ tag = "hello" }
    s = JtSample(){ sid = 100 }
}

JsonTest
{
    static fun()
    {
        global.println("===== JsonTest start =====")
        testBaseJsonText()
        testJsonValue()
        testJsonFile()
        testDataJson()
        testDataContainers()
        global.println("===== JsonTest end =====")
    }

    # BaseJson：文本解析 -> 路径取值 -> 类型判定 -> 往返序列化
    static testBaseJsonText()
    {
        global.println("----- testBaseJsonText -----")
        string text = "{\"user\":{\"name\":\"alice\",\"age\":30,\"vip\":true},\"tags\":[\"a\",\"b\"],\"scores\":[90,85,77],\"pi\":3.14,\"nothing\":null}"

        BaseJson j = BaseJson(text)
        global.println("parse ok = " + j.isNotEmpty.toString())
        global.println("memberCount = " + j.memberCount.toString())          # 5
        global.println("nodeCount = " + j.nodeCount.toString())             # 1+4+3+4+1+1 = 14

        # 路径取值
        global.println("user/name = " + j.getStr("user/name", ""))          # alice
        global.println("user/age = " + j.getInt("user/age", 0).toString())  # 30
        global.println("user/vip = " + j.getBool("user/vip", false).toString())  # true
        global.println("pi = " + j.getFloat("pi", 0.0d).toString())         # 3.14
        global.println("tags/1 = " + j.getStr("tags/1", ""))                # b
        global.println("scores/2 = " + j.getInt("scores/2", 0).toString()) # 77
        # null 语义：null 字面量节点 vs 缺失路径返回的 null 引用
        # （j.isNull 为实例语法调 static 方法，同时回归覆盖 receiver 弹栈）
        TreeNode<string> nothingNode = j.select("nothing")
        global.println("nothing is null = " + j.isNull(nothingNode).toString())
        TreeNode<string> nilNode = j.select("user/missing")
        global.println("missing select null = " + (nilNode == null).toString())
        global.println("missing isNull = " + BaseJson.isNull(nilNode).toString())
        global.println("missing name = " + j.getStr("user/missing", "def"))    # def

        # 类型判定
        global.println("user isObject = " + BaseJson.isObject(j.select("user")).toString())
        global.println("tags isArray = " + BaseJson.isArray(j.select("tags")).toString())

        # 树透传
        global.println("has user/name = " + j.has("user/name").toString())
        global.println("findByName(name) value = " + j.findByName("name")._value)   # alice

        # 往返
        global.println("toJson = " + j.toJson())
        global.println("toJsonPretty = " + j.toJsonPretty())

        # 空实例与静态工厂
        BaseJson empty = BaseJson()
        global.println("empty isEmpty = " + empty.isEmpty.toString())
        BaseJson j2 = BaseJson.parse("{\"k\":\"v\"}")
        global.println("static parse k = " + j2.getStr("k", ""))
    }

    # 断言辅助：OK/FAIL 单行输出
    static check( string name, bool cond )
    {
        if cond
        {
            global.println( "[JsonValue] " + name + " : OK" )
        }
        else
        {
            global.println( "[JsonValue] " + name + " : FAIL" )
        }
    }

    # JsonValue：层次化节点视图。
    #覆盖点：静态 parse/wrap、多层嵌套导航（["name"]/[N] -> _getItem_）、
    #缺失视图链式安全、null 字面量语义、类型判定（6 种）、数组下标、
    #has/select、标量转换（6 种 + 默认值）、children/keys、
    #asArray/asList/asMap、foreach 迭代、asData<T>/asData(typeName)、
    #toJson/toJsonPretty/toString、BaseJson 集成（value/$ 索引）。
    static testJsonValue()
    {
        global.println( "----- testJsonValue -----" )
        #根 object 6 成员；user 6 成员含 string/int/bool/float/大整数；
        #tags 字符串数组；nums 混合数字数组；空对象/空数组/null 字面量
        string text = "{\"user\":{\"name\":\"alice\",\"age\":30,\"vip\":true,\"off\":false,\"height\":1.68,\"big\":4294967296},\"tags\":[\"x\",\"y\",\"z\"],\"nums\":[10,-3,4.5],\"emptyObj\":{},\"emptyArr\":[],\"nothing\":null}"

        # ---- 静态 parse / wrap / 根视图 ----
        JsonValue jv = JsonValue.parse( text )
        check( "parse 根视图存在", jv.exists )
        check( "根 isObject", jv.isObject )
        check( "根 length == 6", jv.length == 6 )

        BaseJson bj = BaseJson( text )
        JsonValue wv = JsonValue.wrap( bj.root )
        check( "wrap 根 isObject", wv.isObject )
        JsonValue wu = wv["user"]
        check( "wrap 与 parse 同源取值", wu["age"].asInt32() == 30 )
        check( "_getItem_ 显式调用", jv._getItem_( "user" ).isObject )

        # ---- 多层嵌套导航 ----
        JsonValue user = jv["user"]
        check( "user isObject", user.isObject )
        check( "user length == 6", user.length == 6 )
        check( "嵌套 string 取值", user["name"].asString() == "alice" )
        check( "嵌套 int 取值", user["age"].asInt32() == 30 )
        check( "嵌套 bool 取值", user["vip"].asBool() )
        # 无后缀小数字面量默认 Float32（词法约定），1.68 经 Float32 有精度损失（≈1.68000007），
        # 与 asFloat64 的 Float64 精确值（≈1.6799999999999999）不等——比较 Float64 须用 d 后缀
        check( "嵌套 float 取值", user["height"].asFloat64() == 1.68d )
        check( "嵌套 float 取值(f32)", user["height"].asFloat32() == 1.68f )
        check( "Int64 超出 Int32 范围", user["big"].asInt64() == 4294967296 )

        # ---- 缺失视图与链式安全（_getItem_ 未命中不返回 null 引用）----
        JsonValue miss = jv["nobody"]
        check( "缺失键 isNull 视图", miss.isNull )
        check( "缺失键 exists false", !miss.exists )
        check( "缺失键 length 0", miss.length == 0 )
        check( "缺失键 name null", miss.name == null )
        check( "缺失键 asInt32 默认值", miss.asInt32( 42 ) == 42 )
        check( "缺失键 asString 默认值", miss.asString( "def" ) == "def" )
        JsonValue deep = jv["ghost"]
        JsonValue deeper = deep["child"]
        check( "深层缺失链不中断", deeper.asFloat64( 9.5d ) == 9.5d )

        # ---- JSON null 字面量语义（节点存在但值为 null）----
        JsonValue nothing = jv["nothing"]
        check( "null 字面量 isNull", nothing.isNull )
        check( "null 字面量 exists true", nothing.exists )
        check( "null 字面量 asString 默认", nothing.asString( "d" ) == "d" )

        # ---- 类型判定 ----
        check( "string isString", user["name"].isString )
        check( "string 非 number", !user["name"].isNumber )
        check( "int isNumber", user["age"].isNumber )
        check( "float isNumber", user["height"].isNumber )
        check( "bool isBool", user["vip"].isBool )
        check( "object 非 number", !user.isNumber )
        check( "array 非 string", !jv["tags"].isString )
        check( "空对象 isObject", jv["emptyObj"].isObject )
        check( "空数组 isArray", jv["emptyArr"].isArray )
        check( "空对象 length 0", jv["emptyObj"].length == 0 )

        # ---- 数组下标导航 ----
        JsonValue tags = jv["tags"]
        check( "tags length 3", tags.length == 3 )
        check( "tags[0] 取值", tags[0].asString() == "x" )
        check( "tags[1] 取值", tags[1].asString() == "y" )
        check( "数组元素 isString", tags[1].isString )
        check( "数组元素 name 空串", tags[0].name == "" )
        check( "越界 isNull 视图", tags._getItem_( 9 ).isNull )
        check( "负下标 isNull 视图", tags._getItem_( -1 ).isNull )

        JsonValue nums = jv["nums"]
        check( "nums[0] asInt32", nums[0].asInt32() == 10 )
        check( "负数 asInt32", nums[1].asInt32() == -3 )
        check( "小数 asFloat64", nums[2].asFloat64() == 4.5 )
        check( "小数 asFloat32", nums[2].asFloat32() == 4.5f )
        check( "小数 isNumber", nums[2].isNumber )

        # ---- has / select ----
        check( "has(name) 命中", user.has( "name" ) )
        check( "has(name) 未命中", !user.has( "weight" ) )
        check( "has(1) 命中", tags.has( 1 ) )
        check( "has(3) 越界", !tags.has( 3 ) )
        check( "select 路径取值", jv.select( "user/name" ).asString() == "alice" )
        check( "select 数组下标", jv.select( "nums/1" ).asInt32() == -3 )
        check( "select 缺失 isNull", jv.select( "user/xx/yy" ).isNull )

        # ---- 标量转换默认值语义（非数字文本 -> 默认值）----
        check( "asString 容器默认值", user.asString( "d" ) == "d" )
        check( "asInt32 非数字默认值", user["name"].asInt32( -7 ) == -7 )
        check( "asInt64 布尔文本默认值", user["vip"].asInt64( 5 ) == 5 )
        check( "asFloat64 字符串默认值", user["name"].asFloat64( 1.5d ) == 1.5d )
        bool offV = user["off"].asBool( true )
        check( "asBool false 文本", !offV )
        check( "asBool 数字文本默认值", user["age"].asBool( true ) )
        check( "asFloat32 小数压窄", nums[2].asFloat32( 0.0f ) == 4.5f )

        # ---- children / keys ----
        Array<JsonValue> ch = user.children()
        check( "children 数量", ch.length == 6 )
        check( "children[0] 取值", ch[0].asString() == "alice" )
        Array<string> ks = user.keys()
        check( "keys 数量", ks.length == 6 )
        check( "keys[0] 键名", ks[0] == "name" )

        # ---- asArray / asList / asMap ----
        Array<JsonValue> arr = tags.asArray()
        check( "asArray 数量", arr.length == 3 )
        check( "asArray[2] 取值", arr[2].asString() == "z" )
        List<JsonValue> l = nums.asList()
        check( "asList 数量", l.length == 3 )
        check( "asList[1] 取值", l._getItem_( 1 ).asInt32() == -3 )
        Map<string, JsonValue> m = user.asMap()
        check( "asMap 数量", m.length == 6 )
        check( "asMap containsKey", m.containsKey( "name" ) )
        check( "asMap 取值", m["name"].asString() == "alice" )
        check( "asMap 缺失返回 null", m["nobody"] == null )

        # ---- foreach 迭代（数组元素 / object 成员 / 空节点）----
        int cnt = 0
        string joined = ""
        for item in tags
        {
            joined = joined + item.asString()
            cnt = cnt + 1
            if cnt > 10
            {
                break
            }
        }
        check( "迭代数组数量", cnt == 3 )
        check( "迭代拼接 xyz", joined == "xyz" )

        int ucnt = 0
        bool nameOk = false
        for member in user
        {
            ucnt = ucnt + 1
            if member.name == "name"
            {
                nameOk = member.asString() == "alice"
            }
        }
        check( "迭代对象成员数", ucnt == 6 )
        check( "迭代定位成员", nameOk )

        JsonValue ea = jv["emptyArr"]
        int ecnt = 0
        for item in ea
        {
            ecnt = ecnt + 1
        }
        check( "空数组迭代零次", ecnt == 0 )

        # ---- asData（复用 SystemJsonToData 后端）----
        JtSample s = new()
        s.meta.level = 3
        s.meta.passed = true
        JsonValue js = JsonValue.parse( s.toJson() )
        JtSample back = js.asData<JtSample>()
        check( "asData<T> 非 null", back != null )
        check( "asData 标量成员", back.sid == 7 )
        check( "asData string 成员", back.name == "sam" )
        check( "asData 数组成员", back.scores[1] == 88 )
        check( "asData 嵌套 data 成员", back.meta.level == 3 )
        var back2 = js.asData( "JtSample" )
        check( "asData(typeName) 非 null", back2 != null )
        JsonValue meta = js["meta"]
        JtMeta backM = meta.asData<JtMeta>()
        check( "子节点 asData<JtMeta> 非 null", backM != null )
        check( "子节点 asData 取值", backM.level == 3 )

        # ---- 序列化（复用 SystemJsonToString 后端）----
        check( "根 toJson 往返一致", js.toJson() == s.toJson() )
        check( "toString == toJson", js.toString() == js.toJson() )
        check( "toJsonPretty 非空", js.toJsonPretty().length > 0 )
        check( "子树 toJson", tags.toJson() == "[\"x\",\"y\",\"z\"]" )
        check( "缺失视图 toJson null", jv["ghost"].toJson() == "null" )

        # ---- BaseJson 集成入口（value / 顶层 $ 索引）----
        check( "BaseJson.value 视图", bj.value.isObject )
        JsonValue uv = bj["user"]
        check( "BaseJson 顶层键索引", uv["name"].asString() == "alice" )
        check( "BaseJson has 集成", bj.has( "user/name" ) )
        BaseJson bjArr = BaseJson( "[10,20,30]" )
        check( "BaseJson 顶层下标索引", bjArr[1].asInt32() == 20 )
    }

    # Text.Json（Std）：saveAs -> load 往返 -> 关联文件信息
    static testJsonFile()
    {
        global.println("----- testJsonFile -----")
        string path = "json_test_out.json"

        var jf = Text.Json("{\"a\":1,\"b\":\"two\",\"c\":[true,false]}")
        global.println("saveAs = " + jf.saveAs(path).toString())
        global.println("path = " + jf.path)
        global.println("fileExists = " + jf.fileExists.toString())
        global.println("fileSize > 0 = " + (jf.fileSize() > 0).toString())

        var loaded = Text.Json.load(path)
        global.println("loaded notEmpty = " + loaded.isNotEmpty.toString())
        global.println("a = " + loaded.getInt("a", 0).toString())           # 1
        global.println("b = " + loaded.getStr("b", ""))                      # two
        global.println("c/0 = " + loaded.getBool("c/0", false).toString())  # true
        global.println("c/1 = " + loaded.getBool("c/1", true).toString())   # false

        # reload
        global.println("reload = " + loaded.reload().toString())

        # pretty 保存
        global.println("savePretty = " + loaded.savePretty().toString())
    }

    # data/class -> JSON 正向 + JSON -> data 反向 + 往返一致性
    static testDataJson()
    {
        global.println("----- testDataJson -----")

        # 正向：toJson / toJsonPretty / BaseJson.fromData
        # 注意：data 成员处具名 data 初始化的花括号覆盖值当前不生效
        #（meta = JtMeta(){ level = 3 } 只取 JtMeta 默认值），故构造后显式赋值
        JtSample s = new()
        s.meta.level = 3
        s.meta.passed = true
        global.println("toJson = " + s.toJson())
        global.println("toJsonPretty = " + s.toJsonPretty())

        BaseJson j = BaseJson.fromData(s)
        global.println("fromData = " + j.toJson())

        # 字段级验证（正向成员映射规则）
        global.println("sid = " + j.getInt("sid", 0).toString())                    # 7 标量
        global.println("name = " + j.getStr("name", ""))                            # sam string
        global.println("score = " + j.getFloat("score", 0.0d).toString())           # 95.5 浮点
        global.println("active = " + j.getBool("active", false).toString())         # true bool
        global.println("kind = " + j.getInt("kind", 0).toString())                  # 2 enum 取底层值
        global.println("meta/level = " + j.getInt("meta/level", 0).toString())      # 3 data 成员递归展开
        global.println("meta/passed = " + j.getBool("meta/passed", false).toString()) # true
        global.println("scores/1 = " + j.getInt("scores/1", 0).toString())          # 88 数组
        global.println("tags/0 = " + j.getStr("tags/0", ""))                        # math
        global.println("nested/1/0 = " + j.getInt("nested/1/0", 0).toString())      # 3 嵌套数组

        # 反向：糖形式 toData<JtSample>()（前端注入类型名，返回强类型）
        JtSample back = j.toData<JtSample>()
        global.println("back not null = " + (back != null).toString())
        global.println("back sid = " + back.sid.toString())
        global.println("back name = " + back.name)
        global.println("back score = " + back.score.toString())
        global.println("back active = " + back.active.toString())
        global.println("back meta/level = " + back.meta.level.toString())
        global.println("back meta/passed = " + back.meta.passed.toString())
        global.println("back scores/1 = " + back.scores[1].toString())
        global.println("back tags/0 = " + back.tags[0])
        global.println("back nested/1/0 = " + back.nested[1][0].toString())

        # 往返一致性：toJson -> toData -> toJson 文本一致
        global.println("roundtrip same = " + (back.toJson() == s.toJson()).toString())

        # 反向：显式类型名 toData("JtSample")（非模板版，返回 object；
        #object as data 类型前端暂不支持，此处只验证非空）
        var back2 = j.toData("JtSample")
        global.println("back2 not null = " + (back2 != null).toString())

        # class 成员正向：toJson() 嫁接 / toString() 串叶回退
        #（s 成员同样受"具名 data 初始化花括号值不生效"限制，构造后显式赋值）
        JtExtra e = new()
        e.s.sid = 100
        BaseJson je = BaseJson.fromData(e)
        global.println("extra = " + je.toJson())
        global.println("pt/px = " + je.getInt("pt/px", 0).toString())               # 3 class toJson 嫁接
        global.println("pt/py = " + je.getInt("pt/py", 0).toString())               # 4
        global.println("tag = " + je.getStr("tag", ""))                             # tag:hello toString 串叶
        global.println("s/sid = " + je.getInt("s/sid", 0).toString())               # 100 嵌套 data

        # 反向：class 成员不还原（保持 null），嵌套 data 成员正常还原
        JtExtra backE = je.toData<JtExtra>()
        global.println("backE not null = " + (backE != null).toString())
        global.println("backE pt null = " + (backE.pt == null).toString())
        global.println("backE tag null = " + (backE.tag == null).toString())
        global.println("backE s/sid = " + backE.s.sid.toString())

        # 失败场景：未知类型名 -> null；fromData(null) -> 空 BaseJson
        var nil = j.toData("NoSuchType")
        global.println("bad type null = " + (nil == null).toString())
        BaseJson jn = BaseJson.fromData(null)
        global.println("fromData(null) isEmpty = " + jn.isEmpty.toString())
    }

    # 顶层容器正向：List/Map/HashSet/Tuple -> 数组/对象
    static testDataContainers()
    {
        global.println("----- testDataContainers -----")

        # List -> 数组
        List<Int32> l = List<Int32>()
        l.add(1)
        l.add(2)
        l.add(3)
        BaseJson jl = BaseJson.fromData(l)
        global.println("list = " + jl.toJson())                # [1,2,3]

        # Map -> 对象
        Map<string, Int32> m = Map<string, Int32>()
        m.add("a", 1)
        m.add("b", 2)
        BaseJson jm = BaseJson.fromData(m)
        global.println("map = " + jm.toJson())                 # {"a":1,"b":2}

        # HashSet -> 数组（顺序由哈希决定，不做精确断言）
        HashSet<Int32> hs = HashSet<Int32>()
        hs.add(5)
        hs.add(7)
        BaseJson jhs = BaseJson.fromData(hs)
        global.println("hashset = " + jhs.toJson())

        # Tuple -> 数组
        var t = Tuple<Int32, string>(7, "seven")
        BaseJson jt = BaseJson.fromData(t)
        global.println("tuple = " + jt.toJson())               # [7,"seven"]
    }
}
