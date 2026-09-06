import ConStrCC;

data ConstructionPoint
{
    x = 0
    y = 0
}

data ConstructionMeta
{
    level = 0
    passed = false
}

data ConstructionRecord
{
    id = 0
    name = ""
    point = ConstructionPoint(){ x = 1, y = 2 }
    meta = ConstructionMeta(){ level = 1, passed = true }
    profile = {
        grade = 3,
        address = {
            city = "Shenzhen",
            zip = 518000
        }
    }
    tags = ["class", "data", "construction"]
    history = [
        { name = "default", value = 1 },
        { name = "brace", value = 2 }
    ]
}

data ConstructionCounter
{
    createCount = 0
    totalValue = 0
}

ConStrCC.ClassLeaf
{
    override _init_()
    {
        this.value = 0
    }

    _init_( int _value )
    {
        this.value = _value
    }

    int value = 0

    int getValue()
    {
        ret this.value
    }
}

ConStrCC.ClassBase
{
    override _init_()
    {
        this.x = 10
        this.y = 20
    }

    _init_( int _x, int _y )
    {
        this.x = _x
        this.y = _y
    }

    int x = 0
    int y = 0

    int sum()
    {
        ret this.x + this.y
    }
}

ConStrCC.ClassChild extends ConStrCC.ClassBase
{
    _init_( int _x, int _y, int _z )
    {
        base._init_( _x, _y )
        this.z = _z
    }

    int z = 0

    int sumAll()
    {
        ret this.sum() + this.z
    }
}

ConStrCC.ClassHolder
{
    _init_( int _value )
    {
        this.leaf = ConStrCC.ClassLeaf( _value )
        this.record = ConstructionRecord(){ id = _value, name = "holder" }
    }

    ConStrCC.ClassLeaf leaf = ConStrCC.ClassLeaf()
    ConstructionRecord record = new()

    int getLeafValue()
    {
        ret this.leaf.getValue()
    }
}

ConStrCC.ConstructionTest
{
    static fun()
    {
        global.println("========== ConstructionClass tests (start) ==========")
        ConStrCC.ConstructionTest.classConstructionTest()
        ConStrCC.ConstructionTest.dataConstructionTest()
        global.println("========== ConstructionClass tests (end) ==========")
    }

    static classConstructionTest()
    {
        global.println("----- classConstructionTest -----")

        ConStrCC.ClassBase baseDefault = ConStrCC.ClassBase()
        ConStrCC.ClassBase baseArgs = ConStrCC.ClassBase( 3, 4 )
        ConStrCC.ClassBase baseBrace = ConStrCC.ClassBase(){ x = 5, y = 6 }
        ConStrCC.ClassChild child = ConStrCC.ClassChild( 7, 8, 9 )
        ConStrCC.ClassHolder holder = ConStrCC.ClassHolder( 30 )

        global.println("baseDefault sum -> " + baseDefault.sum().toString())
        global.println("baseArgs sum -> " + baseArgs.sum().toString())
        global.println("baseBrace sum -> " + baseBrace.sum().toString())
        global.println("child sumAll -> " + child.sumAll().toString())
        global.println("holder leaf value -> " + holder.getLeafValue().toString())
        global.println("holder record id/name -> " + holder.record.id.toString() + "/" + holder.record.name)

        Object obj1 = new()
        Object obj2 = obj1
        Object obj3 = new()
        global.println("object alias refEquals -> " + Object.refEquals(obj1, obj2).toString())
        global.println("object distinct refEquals -> " + Object.refEquals(obj1, obj3).toString())
    }

    static dataConstructionTest()
    {
        global.println("----- dataConstructionTest -----")

        ConstructionRecord recordDefault = new()
        recordDefault.id = 1
        recordDefault.name = "default-new"
        recordDefault.point.x = 11
        recordDefault.point.y = 12

        ConstructionRecord recordNamed = ConstructionRecord(){ id = 2, name = "named", point = ConstructionPoint(){ x = 21, y = 22 }, meta = ConstructionMeta(){ level = 2, passed = true } }
        ConstructionRecord recordBrace = { id = 3, name = "brace", point = ConstructionPoint(){ x = 31, y = 32 }, meta = ConstructionMeta(){ level = 3, passed = false } }
        recordBrace = ConstructionRecord(){ id = 4, name = "reassign", point = ConstructionPoint(){ x = 41, y = 42 }, meta = ConstructionMeta(){ level = 4, passed = true } }

        ConstructionCounter.createCount = 3
        ConstructionCounter.totalValue = recordDefault.id + recordNamed.id + recordBrace.id

        global.println("recordDefault -> " + recordDefault.toString())
        global.println("recordNamed -> " + recordNamed.toString())
        global.println("recordBrace -> " + recordBrace.toString())
        global.println("recordDefault point chain -> " + recordDefault.point.x.toString() + "/" + recordDefault.point.y.toString())
        global.println("recordNamed meta chain -> " + recordNamed.meta.level.toString() + "/" + recordNamed.meta.passed.toString())
        global.println("static data counter -> " + ConstructionCounter.createCount.toString() + "/" + ConstructionCounter.totalValue.toString())
        global.println("static data nested chain -> " + ConstructionRecord.profile.address.city)
    }
}

#!
构建类测试规则：
1. ClassName() 调用默认 _init_。
2. ClassName(args) 按参数匹配重载 _init_。
3. ClassName(){ member = value } 先构建对象，再对当前对象成员赋值。
4. 子类构造中使用 base._init_(...) 初始化父类成员。
5. 构造函数只负责当前实例成员初始化，不在构造体中直接改写其它对象的内部成员。
6. 不测试 ClassName().method() 这种临时对象链式调用；对象应先落到变量再访问成员或方法。

构建 data 测试规则：
1. DataName v = new() 创建默认 data 实例，非 const data 可继续写成员。
2. DataName(){ ... } 直接构建并覆盖成员。
3. DataName v = { ... } 使用 brace 初始化目标 data。
4. 非 const data 支持整体重新赋值和成员重新赋值。
5. data 支持匿名对象、数组、具名 data、具名 class 成员和链式成员读取。
6. static data 只通过成员访问和成员写入覆盖，不把静态 data 当普通临时对象 new。
!#
