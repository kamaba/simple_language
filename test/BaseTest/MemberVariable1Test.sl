# MemberVariable1Test.sl - 成员变量基础测试

partial C2
{
    x = 1
    static int x2 = 10

    _init_( int a, float b )
    {
        this.x = a
    }
}

MVBase
{
    Int32 id = 0
    string name = "default"
    bool active = true
    float score = 0.0f
    long big = 0L
}

MVModifier
{
    public int pubVal = 100
    private int priVal = 200
    static int stVal = 300
    public static int pubStVal = 400
    private static int priStVal = 500
    static public int stPubVal = 700
}

MVExprInit
{
    static int stRef = 50
    int a = 10 + 20 * 3
    int b = 100 / 4 - 5
    bool c = 10 <= 10 / (20 + 1 * 35 - (32 / 15))
    int d = -20
    int e = 10 / 5 + 2 * 10 - MVExprInit.stRef
    int f = MVExprInit.stRef + 100
    int g = (1 - 20) * 33 / 20 + 10
}

MVObjectMember
{
    MVBase obj = new()
    MVBase obj2 = MVBase()
    MVBase nullObj = null

    override _init_()
    {
        this.obj.id = 999
    }
}

MVParent
{
    int pVal = 10
    string pName = "parent"
    static int pStatic = 1000
}

MVChild extends MVParent
{
    int cVal = 20
    string cName = "child"

    _init_(int pv, int cv)
    {
        this.pVal = pv
        this.cVal = cv
    }
}

MVGrandChild extends MVChild
{
    int gVal = 30

    override _init_()
    {
        this.pVal = 111
        this.cVal = 222
        this.gVal = 333
    }
}

MVStaticRef
{
    static int s1 = 1
    static int s2 = MVStaticRef.s1 + 2
    static int s3 = MVStaticRef.s2 + 3
    static int s4 = MVStaticRef.s3 * 2
}

partial MVPartialTarget
{
    int baseVal = 1
}

MVTest1
{
    static fun()
    {
        global.println("========== MemberVariable1Test (start) ==========")
        MVTest1.testBaseTypes()
        MVTest1.testModifiers()
        MVTest1.testExprInit()
        MVTest1.testObjectMember()
        MVTest1.testInheritMember()
        MVTest1.testInheritModify()
        MVTest1.testStaticRef()
        MVTest1.testPartial()
        global.println("========== MemberVariable1Test (end) ==========")
    }

    static testBaseTypes()
    {
        global.println("----- testBaseTypes -----")
        MVBase b = MVBase()
        b.id = 42
        b.name = "hello"
        b.active = false
        b.score = 3.14f
        b.big = 9999999999L
        global.println("id=" + b.id + " name=" + b.name + " active=" + b.active)
        global.println("score=" + b.score + " big=" + b.big)
    }

    static testModifiers()
    {
        global.println("----- testModifiers -----")
        MVModifier m = MVModifier()
        global.println("pubVal=" + m.pubVal)
        global.println("stVal=" + MVModifier.stVal)
        global.println("pubStVal=" + MVModifier.pubStVal)
        global.println("stPubVal=" + MVModifier.stPubVal)
        MVModifier.stVal = 999
        global.println("stVal(after set)=" + MVModifier.stVal)
    }

    static testExprInit()
    {
        global.println("----- testExprInit -----")
        MVExprInit e = MVExprInit()
        global.println("a=" + e.a + " b=" + e.b + " c=" + e.c)
        global.println("d=" + e.d + " e=" + e.e + " f=" + e.f + " g=" + e.g)
    }

    static testObjectMember()
    {
        global.println("----- testObjectMember -----")
        MVObjectMember m = MVObjectMember()
        global.println("obj.id=" + m.obj.id)
        global.println("obj2.id=" + m.obj2.id)
        global.println("nullObj=" + m.nullObj)
    }

    static testInheritMember()
    {
        global.println("----- testInheritMember -----")

        # 默认初始化值：子类继承父类成员
        MVParent p = MVParent()
        global.println("Parent: pVal=" + p.pVal + " pName=" + p.pName)

        MVChild c = MVChild()
        global.println("Child: pVal=" + c.pVal + " pName=" + c.pName + " cVal=" + c.cVal + " cName=" + c.cName)

        MVGrandChild gc = MVGrandChild()
        global.println("GrandChild: pVal=" + gc.pVal + " cVal=" + gc.cVal + " gVal=" + gc.gVal)

        # 父类静态成员通过子类访问
        global.println("pStatic(via Parent)=" + MVParent.pStatic)
        global.println("pStatic(via Child)=" + MVChild.pStatic)
    }

    static testInheritModify()
    {
        global.println("----- testInheritModify -----")

        # 构造函数中修改继承的父类成员
        MVChild c1 = MVChild(501, 502)
        global.println("c1: pVal=" + c1.pVal + " cVal=" + c1.cVal)

        # 子类对象独立修改继承的成员，不影响父类对象
        MVChild c2 = MVChild()
        c2.pVal = 999
        global.println("c2.pVal=" + c2.pVal + " (after set)")

        MVParent p = MVParent()
        global.println("p.pVal=" + p.pVal + " (unaffected by c2)")

        # 孙类构造函数中修改多层继承的成员
        MVGrandChild gc = MVGrandChild()
        global.println("gc: pVal=" + gc.pVal + " cVal=" + gc.cVal + " gVal=" + gc.gVal)

        # 修改孙类继承的成员
        gc.pName = "grand"
        gc.cName = "gc_child"
        global.println("gc: pName=" + gc.pName + " cName=" + gc.cName)
    }

    static testStaticRef()
    {
        global.println("----- testStaticRef -----")
        global.println("s1=" + MVStaticRef.s1 + " s2=" + MVStaticRef.s2)
        global.println("s3=" + MVStaticRef.s3 + " s4=" + MVStaticRef.s4)
    }

    static testPartial()
    {
        global.println("----- testPartial -----")
        C2 c = C2(7, 2.0f)
        global.println("x=" + c.x + " x2=" + C2.x2)
    }
}
