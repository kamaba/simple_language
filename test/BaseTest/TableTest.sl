TableTest
{
    # 测试构造 + 基础属性 + addColumn/addRow/getValue/setValue
    static testBasic()
    {
        global.println("===== testBasic =====")
        Table t = new()
        global.println("empty rowCount = " + t.rowCount)
        global.println("empty columnCount = " + t.columnCount)
        global.println("empty isEmpty = " + t.isEmpty)

        t.addColumn("name")
        t.addColumn("age")
        global.println("columnCount = " + t.columnCount)          # 2
        global.println("getColumnName(1) = " + t.getColumnName(1)) # age
        global.println("getColumnIndex(age) = " + t.getColumnIndex("age"))    # 1
        global.println("containsColumn(name) = " + t.containsColumn("name"))  # true
        global.println("containsColumn(x) = " + t.containsColumn("x"))        # false

        Array<Object> r0 = Array<Object>(2)
        r0._setItem_(0, "alice")
        r0._setItem_(1, 30)
        Array<Object> r1 = Array<Object>(2)
        r1._setItem_(0, "bob")
        r1._setItem_(1, 25)
        t.addRow(r0)
        t.addRow(r1)
        global.println("rowCount = " + t.rowCount)                # 2
        global.println("getValue(0,0) = " + t.getValue(0, 0).toString())      # alice
        global.println("getValue(1,1) = " + t.getValue(1, 1).toString())      # 25
        global.println("getValueByName(0,name) = " + t.getValueByName(0, "name").toString())

        t.setValue(1, 1, 26)
        global.println("after setValue(1,1,26) = " + t.getValue(1, 1).toString())
        t.setValueByName(0, "age", 31)
        global.println("after setValueByName(0,age,31) = " + t.getValue(0, 1).toString())
    }

    # 测试类型化取值族（含默认值与健壮转换）
    static testTypedGet()
    {
        global.println("===== testTypedGet =====")
        Table t = new()
        t.addColumn("name")
        t.addColumn("age")
        t.addColumn("score")
        t.addColumn("vip")
        Array<Object> row = Array<Object>(4)
        row._setItem_(0, "alice")
        row._setItem_(1, 30)
        row._setItem_(2, 92.5d)
        row._setItem_(3, true)
        t.addRow(row)
        global.println("getInt(0,1) = " + t.getInt(0, 1))                 # 30
        global.println("getFloat(0,2) = " + t.getFloat(0, 2).toString())  # 92.5
        global.println("getBool(0,3) = " + t.getBool(0, 3))               # true
        global.println("getStr(0,0) = " + t.getStr(0, 0))                 # alice
        global.println("getStr(0,1) = " + t.getStr(0, 1))                 # 30（数值转字符串）

        # 字符串数值单元格健壮转换
        Array<Object> row2 = Array<Object>(4)
        row2._setItem_(0, "42")
        row2._setItem_(1, "18")
        row2._setItem_(2, "3.14")
        row2._setItem_(3, "true")
        t.addRow(row2)
        global.println("getInt(1,1) = " + t.getInt(1, 1))                # 18
        global.println("getFloat(1,2) = " + t.getFloat(1, 2).toString()) # 3.14
        global.println("getBool(1,3) = " + t.getBool(1, 3))              # true

        # 越界 / null 单元格返回默认值
        global.println("getInt(9,9,-1) = " + t.getInt(9, 9, -1))         # -1
        global.println("getFloatByName(0,x,1.5) = " + t.getFloatByName(0, "x", 1.5d).toString())  # 1.5
        global.println("getStrByName(0,x,none) = " + t.getStrByName(0, "x", "none"))              # none
        global.println("getIntByName(0,age,0) = " + t.getIntByName(0, "age", 0))                  # 30
    }

    # 测试表头维护（insertColumn/removeColumn/renameColumn）
    static testColumnOps()
    {
        global.println("===== testColumnOps =====")
        Table t = new()
        t.addColumn("a")
        t.addColumn("c")
        Array<Object> row = Array<Object>(2)
        row._setItem_(0, 1)
        row._setItem_(1, 3)
        t.addRow(row)

        t.insertColumn(1, "b")
        global.println("columnCount = " + t.columnCount)                  # 3
        global.println("getColumnName(1) = " + t.getColumnName(1))        # b
        global.println("getValue(0,2) = " + t.getValue(0, 2).toString())  # 3（右移）
        global.println("getValue(0,1) = " + t.getValue(0, 1))             # null（插入补空）

        global.println("renameColumn(a,aa) = " + t.renameColumn("a", "aa"))  # true
        global.println("getColumnName(0) = " + t.getColumnName(0))           # aa

        global.println("removeColumn(aa) = " + t.removeColumn("aa"))      # true
        global.println("columnCount = " + t.columnCount)                  # 2
        global.println("getValue(0,0) = " + t.getValue(0, 0))             # null
        global.println("getValue(0,1) = " + t.getValue(0, 1).toString())  # 3（左移）

        global.println("removeColumn(zz) = " + t.removeColumn("zz"))      # false
    }

    # 测试克隆族（VM 层 clone / cloneStructure / cloneRange / cloneRows）
    static testClone()
    {
        global.println("===== testClone =====")
        Table t = new()
        t.addColumn("name")
        t.addColumn("age")
        Array<Object> r0 = Array<Object>(2)
        r0._setItem_(0, "alice")
        r0._setItem_(1, 30)
        Array<Object> r1 = Array<Object>(2)
        r1._setItem_(0, "bob")
        r1._setItem_(1, 25)
        t.addRow(r0)
        t.addRow(r1)

        Table c = t.clone()
        c.setValue(0, 1, 99)
        global.println("clone rowCount = " + c.rowCount)                        # 2
        global.println("clone getValue(0,1) = " + c.getValue(0, 1).toString())  # 99
        global.println("src getValue(0,1) = " + t.getValue(0, 1).toString())    # 30（深拷贝互不影响）

        Table s = t.cloneStructure()
        global.println("structure rowCount = " + s.rowCount)              # 0
        global.println("structure columnCount = " + s.columnCount)        # 2

        Table rg = t.cloneRange(0, 1, 0, 2)
        global.println("range rowCount = " + rg.rowCount)                  # 1
        global.println("range columnCount = " + rg.columnCount)            # 2
        global.println("range getValue(0,0) = " + rg.getValue(0, 0).toString())

        Array<int> idx = Array<int>(1)
        idx._setItem_(0, 1)
        Table pick = t.cloneRows(idx)
        global.println("pickRows rowCount = " + pick.rowCount)             # 1
        global.println("pickRows getValue(0,0) = " + pick.getValue(0, 0).toString())  # bob
    }

    # 测试合并（VM 层 merge：列名对齐、缺失列补 null）与 union/join
    static testMergeJoin()
    {
        global.println("===== testMergeJoin =====")
        Table a = new()
        a.addColumn("name")
        a.addColumn("age")
        Array<Object> r0 = Array<Object>(2)
        r0._setItem_(0, "alice")
        r0._setItem_(1, 30)
        a.addRow(r0)

        Table b = new()
        b.addColumn("name")
        b.addColumn("age")
        b.addColumn("city")
        Array<Object> r1 = Array<Object>(3)
        r1._setItem_(0, "bob")
        r1._setItem_(1, 25)
        r1._setItem_(2, "bj")
        b.addRow(r1)

        global.println("merge = " + a.merge(b))                        # true
        global.println("rowCount = " + a.rowCount)                     # 2
        global.println("columnCount = " + a.columnCount)               # 3（city 自动补建）
        global.println("city(0) = " + a.getStr(0, 2))                  # （左表行补 null）
        global.println("city(1) = " + a.getStr(1, 2))                  # bj

        Table u = a.union(b)
        global.println("union rowCount = " + u.rowCount)               # 3（clone(a)=2行 + 追加b的1行，不去重）

        # join：键列内连接
        Table left = new()
        left.addColumn("id")
        left.addColumn("name")
        Array<Object> l0 = Array<Object>(2)
        l0._setItem_(0, "1")
        l0._setItem_(1, "alice")
        Array<Object> l1 = Array<Object>(2)
        l1._setItem_(0, "2")
        l1._setItem_(1, "bob")
        left.addRow(l0)
        left.addRow(l1)

        Table right = new()
        right.addColumn("id")
        right.addColumn("score")
        Array<Object> rr0 = Array<Object>(2)
        rr0._setItem_(0, "2")
        rr0._setItem_(1, 88)
        Array<Object> rr1 = Array<Object>(2)
        rr1._setItem_(0, "1")
        rr1._setItem_(1, 92)
        right.addRow(rr0)
        right.addRow(rr1)

        Table j = left.join(right, "id")
        global.println("join rowCount = " + j.rowCount)                # 2
        global.println("join columnCount = " + j.columnCount)          # 3
        global.println("join getStrByName(0,name) = " + j.getStrByName(0, "name", ""))
        global.println("join getIntByName(0,score,0) = " + j.getIntByName(0, "score", 0))
    }

    # 测试查询（VM 层 findRows / sortBy / distinctRows）
    static testQuery()
    {
        global.println("===== testQuery =====")
        Table t = new()
        t.addColumn("name")
        t.addColumn("age")
        int i = 0
        while i < 5
        {
            Array<Object> row = Array<Object>(2)
            row._setItem_(0, "u" + i.toString())
            row._setItem_(1, 30 - i * 5)
            t.addRow(row)
            i++
        }
        # 追加一行 age=25 用于 findRows 多命中与去重
        Array<Object> dup = Array<Object>(2)
        dup._setItem_(0, "u1")
        dup._setItem_(1, 25)
        t.addRow(dup)

        Array<int> hits = t.findRows("age", 25)
        global.println("findRows(age=25).length = " + hits.length)      # 2
        int k = 0
        string hitStr = ""
        while k < hits.length
        {
            hitStr = hitStr + hits._getItem_(k).toString() + " "
            k++
        }
        global.println("findRows(age=25) = " + hitStr)                  # 1 5

        Array<int> none = t.findRows("age", 999)
        global.println("findRows(age=999).length = " + none.length)     # 0
        global.println("findRows(nocol).length = " + t.findRows("nocol", 1).length)  # 0

        t.sortBy("age")
        global.println("sorted getStr(0,0) = " + t.getStr(0, 0))        # u4（age=10 最小）
        global.println("sorted getStr(5,0) = " + t.getStr(5, 0))        # u0（age=30 最大）
        t.sortBy("age", false)
        global.println("desc getStr(0,0) = " + t.getStr(0, 0))          # u0

        int removed = t.distinctRows()
        global.println("distinctRows removed = " + removed)             # 1（u1 重复行）
        global.println("after distinct rowCount = " + t.rowCount)       # 5
    }

    # 测试统计（min/max/sum/avg）
    # 注：VM 的 Float64.toString 用最短往返规则（10.0 -> "1e1"），故数据避开整十值
    static testStats()
    {
        global.println("===== testStats =====")
        Table t = new()
        t.addColumn("v")
        int i = 0
        while i < 4
        {
            Array<Object> row = Array<Object>(1)
            row._setItem_(0, 11 + i * 10)
            t.addRow(row)
            i++
        }
        global.println("min = " + t.min("v").toString())    # 11
        global.println("max = " + t.max("v").toString())    # 41
        global.println("sum = " + t.sum("v").toString())    # 104
        global.println("avg = " + t.avg("v").toString())    # 26
    }

    # 测试 CSV 解析与序列化（VM 层：引号转义、自定义分隔符、往返）
    static testCsv()
    {
        global.println("===== testCsv =====")
        Table t = Table.fromCsv("name,age,city\nalice,30,bj\nbob,25,sh")
        global.println("rowCount = " + t.rowCount)                     # 2
        global.println("columnCount = " + t.columnCount)               # 3
        global.println("getColumnName(0) = " + t.getColumnName(0))     # name
        global.println("getStrByName(1,name) = " + t.getStrByName(1, "name", ""))  # bob
        global.println("getIntByName(0,age,0) = " + t.getIntByName(0, "age", 0))   # 30

        string out1 = t.toCsv()
        global.println("toCsv = " + out1)
        Table t2 = Table.fromCsv(out1)
        global.println("roundtrip rowCount = " + t2.rowCount)          # 2
        global.println("roundtrip getStr(0,0) = " + t2.getStr(0, 0))   # alice

        # 引号转义：含分隔符 / 引号 / 换行的字段
        Table q = Table.fromCsv("a,b\n\"x,1\",\"he said \"\"hi\"\"\"\n")
        global.println("quoted rowCount = " + q.rowCount)              # 1
        global.println("quoted getStr(0,0) = " + q.getStr(0, 0))       # x,1
        global.println("quoted getStr(0,1) = " + q.getStr(0, 1))       # he said "hi"
        string qout = q.toCsv()
        global.println("quoted toCsv = " + qout)
        Table q2 = Table.fromCsv(qout)
        global.println("quoted roundtrip getStr(0,1) = " + q2.getStr(0, 1))  # he said "hi"

        # 无表头解析
        Table nh = Table.fromCsvNoHeader("1,2\n3,4")
        global.println("noHeader columnCount = " + nh.columnCount)     # 2
        global.println("noHeader getColumnName(0) = " + nh.getColumnName(0))  # col0
        global.println("noHeader getInt(1,0) = " + nh.getInt(1, 0))    # 3

        # 自定义分隔符
        Table s = new()
        s.addColumn("x")
        s.addColumn("y")
        Array<Object> sr = Array<Object>(2)
        sr._setItem_(0, 1)
        sr._setItem_(1, 2)
        s.addRow(sr)
        global.println("toCsvDelimited(;) = " + s.toCsvDelimited(";"))  # x;y / 1;2

        # 空文本 / null 文本
        Table empty = Table.fromCsv("")
        global.println("empty rowCount = " + empty.rowCount)           # 0
        Table nullT = Table.fromCsv(null)
        global.println("null rowCount = " + nullT.rowCount)            # 0
    }

    # 测试预览与迭代器
    static testPreviewIter()
    {
        global.println("===== testPreviewIter =====")
        Table t = new()
        t.addColumn("name")
        t.addColumn("age")
        int i = 0
        while i < 12
        {
            Array<Object> row = Array<Object>(2)
            row._setItem_(0, "u" + i.toString())
            row._setItem_(1, 20 + i)
            t.addRow(row)
            i++
        }
        global.println("--- preview(3) ---")
        global.println(t.preview(3))
        global.println("--- toString (默认10行+截断) ---")
        global.println(t.toString())

        # for-in 暂不支持引用模块中的类（如 Core.Table），用索引访问验证行数据
        int count = 0
        while count < t.rowCount
        {
            Array<Object> row = t._getItem_(count)
            count++
        }
        global.println("iterate rows = " + count)                      # 12
        global.println("row[0] first cell = " + t.getStr(0, 0))        # u0
    }

    # 测试 Csv 门面类（Table 能力的 CSV 视图）
    static testCsvFacade()
    {
        global.println("===== testCsvFacade =====")
        BaseCsv c = BaseCsv("name,age\nalice,30\nbob,25")
        global.println("rowCount = " + c.rowCount)                     # 2
        global.println("columnCount = " + c.columnCount)               # 2
        global.println("getIntByName(0,age,0) = " + c.getIntByName(0, "age", 0))   # 30
        global.println("getStrByName(1,name) = " + c.getStrByName(1, "name", "")) # bob

        # 透传 Table 能力：排序 + 统计 + 查询
        c.sortBy("age", false)
        global.println("sorted first name = " + c.getStrByName(0, "name", ""))    # alice
        global.println("sum(age) = " + c.sum("age").toString())        # 55
        global.println("findRows(age=25).length = " + c.findRows("age", 25).length)  # 1

        # 修改 + 序列化往返
        c.setValueByName(1, "age", 26)
        string text = c.toString()
        global.println("toCsv = " + text)
        BaseCsv c2 = BaseCsv.parse(text)
        global.println("roundtrip getIntByName(1,age) = " + c2.getIntByName(1, "age", 0))  # 26

        # 无表头 / 自定义分隔符
        BaseCsv nh = BaseCsv.parseNoHeader("1,2\n3,4")
        global.println("parseNoHeader getColumnName(0) = " + nh.getColumnName(0))  # col0
        BaseCsv semi = BaseCsv.parseDelimited("x;y\n1;2", true, ";")
        global.println("semi getStrByName(0,x) = " + semi.getStrByName(0, "x", ""))  # 1
        global.println("semi toCsvDelimited = " + semi.toCsvDelimited())  # x;y / 1;2
        global.println("semi toCsvNoHeader = " + semi.toCsvNoHeader())    # 1;2

        # 包装 Table / mergeCsv / joinCsv
        Table t = new()
        t.addColumn("name")
        t.addColumn("age")
        Array<Object> r0 = Array<Object>(2)
        r0._setItem_(0, "carl")
        r0._setItem_(1, 40)
        t.addRow(r0)
        BaseCsv wrap = BaseCsv(t)
        global.println("wrap rowCount = " + wrap.rowCount)             # 1
        global.println("wrap getStr(0,0) = " + wrap.getStr(0, 0))       # carl
        global.println("mergeCsv = " + c.mergeCsv(wrap))               # true
        global.println("merged rowCount = " + c.rowCount)              # 3

        BaseCsv kj = BaseCsv("id,name\n1,alice\n2,bob")
        BaseCsv kr = BaseCsv("id,score\n2,88\n1,92")
        Table jt = kj.joinCsv(kr, "id")
        global.println("joinCsv rowCount = " + jt.rowCount)            # 2
        global.println("joinCsv score(0) = " + jt.getIntByName(0, "score", 0))  # 92

        # clone / preview
        BaseCsv cl = c.clone()
        global.println("clone rowCount = " + cl.rowCount)              # 3
        global.println("--- csv preview(2) ---")
        global.println(cl.preview(2))
    }

    static fun()
    {
        global.println("===== TableTest =====")
        testBasic()
        testTypedGet()
        testColumnOps()
        testClone()
        testMergeJoin()
        testQuery()
        testStats()
        testCsv()
        testPreviewIter()
        testCsvFacade()
    }
}
