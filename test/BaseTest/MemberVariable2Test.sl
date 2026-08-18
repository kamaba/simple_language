# MemberVariable2Test.sl - 成员变量进阶测试：partial、data、enum、嵌套对象

partial C2
{
    x1 = 10
    y1 = 100
    static int y2 = 200

    _init_( int a )
    {
        this.x = a
        this.x1 = a + 1
    }

    int getX1()
    {
        ret this.x1
    }
}

MVLiteralInit
{
    int a = 0
    string b = ""
    bool c = false
}

data MVDataPoint
{
    x = 0
    y = 0
}

class MVDataHolder
{
    int px = 0
    int py = 0

    override _init_()
    {
        this.px = 1
        this.py = 2
    }
}

class MVArrayMember
{
    int n0 = 1
    int n1 = 2
    int n2 = 3

    override _init_()
    {
        this.n0 = 100
    }
}

class MVInner
{
    int val = 0
}

class MVOuter
{
    MVInner inner = new()

    override _init_()
    {
        this.inner.val = 10
    }
}

partial MVPartialTarget
{
    int extra = 2

    int sum()
    {
        ret this.baseVal + this.extra
    }
}

# ---- 跨文件继承：扩展 file1 中定义的类 ----

# 继承 MVParent（定义在 MemberVariable1Test.sl），添加新成员
class MVExtendParent extends MVParent
{
    int eVal = 40
    string eName = "extend"

    _init_(int ev)
    {
        this.pVal = 100
        this.eVal = ev
    }
}

# 继承 MVChild（定义在 MemberVariable1Test.sl），多层跨文件继承
class MVExtendChild extends MVChild
{
    int xVal = 50

    override _init_()
    {
        this.pVal = 801
        this.cVal = 802
        this.xVal = 803
    }

    int total()
    {
        ret this.pVal + this.cVal + this.xVal
    }
}

# 继承 MVGrandChild，四层继承链
class MVExtendGrand extends MVGrandChild
{
    int exVal = 60

    override _init_()
    {
        this.exVal = 999
    }
}

MVTest2
{
    static fun()
    {
        global.println("========== MemberVariable2Test (start) ==========")
        MVTest2.testPartialExtend()
        MVTest2.testLiteralInit()
        MVTest2.testDataMember()
        MVTest2.testArrayMember()
        MVTest2.testNestedObject()
        MVTest2.testPartialMethod()
        MVTest2.testInheritCrossFile()
        global.println("========== MemberVariable2Test (end) ==========")
    }

    static testPartialExtend()
    {
        global.println("----- testPartialExtend -----")
        C2 c1 = C2(7, 2.0f)
        global.println("x=" + c1.x + " x1=" + c1.x1 + " y1=" + c1.y1)
        global.println("x2=" + C2.x2 + " y2=" + C2.y2)

        C2 c2 = C2(99)
        global.println("x=" + c2.x + " x1=" + c2.x1 + " getX1=" + c2.getX1())
    }

    static testLiteralInit()
    {
        global.println("----- testLiteralInit -----")

        # 方式1: 字面量初始化
        MVLiteralInit obj1 = MVLiteralInit(){ a = 10, b = "hi" }
        global.println("obj1: a=" + obj1.a + " b=" + obj1.b + " c=" + obj1.c)

        # 方式2: 匿名初始化
        MVLiteralInit obj2 = { a = 20, c = true }
        global.println("obj2: a=" + obj2.a + " b=" + obj2.b + " c=" + obj2.c)

        # 方式3: 先构造再赋值
        MVLiteralInit obj3 = MVLiteralInit()
        obj3.a = 30
        obj3.b = "manual"
        obj3.c = true
        global.println("obj3: a=" + obj3.a + " b=" + obj3.b + " c=" + obj3.c)
    }

    static testDataMember()
    {
        global.println("----- testDataMember -----")
        MVDataHolder h = MVDataHolder()
        global.println("px=" + h.px + " py=" + h.py)
    }

    static testArrayMember()
    {
        global.println("----- testArrayMember -----")
        MVArrayMember a = MVArrayMember()
        global.println("n0=" + a.n0 + " n1=" + a.n1 + " n2=" + a.n2)
    }

    static testNestedObject()
    {
        global.println("----- testNestedObject -----")
        MVOuter o = MVOuter()
        global.println("inner.val=" + o.inner.val)
    }

    static testPartialMethod()
    {
        global.println("----- testPartialMethod -----")
        MVPartialTarget t = MVPartialTarget()
        global.println("baseVal=" + t.baseVal + " extra=" + t.extra + " sum=" + t.sum())
    }

    static testInheritCrossFile()
    {
        global.println("----- testInheritCrossFile -----")

        # 继承 file1 中的 MVParent，访问继承的父类成员
        MVExtendParent ep = MVExtendParent(777)
        global.println("ep: pVal=" + ep.pVal + " pName=" + ep.pName + " eVal=" + ep.eVal + " eName=" + ep.eName)

        # 继承 file1 中的 MVChild，多层继承访问
        MVExtendChild ec = MVExtendChild()
        global.println("ec: pVal=" + ec.pVal + " cVal=" + ec.cVal + " xVal=" + ec.xVal)
        global.println("ec.total()=" + ec.total())

        # 四层继承链：MVExtendGrand -> MVGrandChild -> MVChild -> MVParent
        MVExtendGrand eg = MVExtendGrand()
        global.println("eg: pVal=" + eg.pVal + " cVal=" + eg.cVal + " gVal=" + eg.gVal + " exVal=" + eg.exVal)

        # 修改跨文件继承的成员
        eg.pName = "four_levels"
        eg.cName = "extend_child"
        global.println("eg: pName=" + eg.pName + " cName=" + eg.cName)

        # 父类静态成员通过跨文件子类访问
        global.println("pStatic(via ExtendParent)=" + MVExtendParent.pStatic)
    }
}
