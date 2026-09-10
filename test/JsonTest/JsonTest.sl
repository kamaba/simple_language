import Std;
#Json 测试：BaseJson（Core）纯文本解析/类型化取值/序列化，
#与 Text.Json（Std）文件加载/保存往返。
JsonTest
{
    static fun()
    {
        global.println("===== JsonTest start =====")
        testBaseJsonText()
        testJsonFile()
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
}
