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

    # private 成员在类内方法中访问
    int getPriVal()
    {
        ret this.priVal
    }

    int setPriVal(int v)
    {
        this.priVal = v
        ret this.priVal
    }

    static int getPriStVal()
    {
        ret MVModifier.priStVal
    }
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

    # 子类方法中引用基类成员
    int sumPC()
    {
        ret this.pVal + this.cVal
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

    # 孙类方法中引用多层继承的基类成员
    int sumAll()
    {
        ret this.pVal + this.cVal + this.gVal
    }
}

MVStaticRef
{
    static int s1 = 1
    static int s2 = MVStaticRef.s1 + 2
    static int s3 = MVStaticRef.s2 + 3
    static int s4 = MVStaticRef.s3 * 2
}

# ---- 构造链：基类成员由基类构造初始化 ----
MVCtorBase
{
    int v = 0

    _init_(int v)
    {
        this.v = v
    }
}

MVCtorDerived extends MVCtorBase
{
    int w = 0

    _init_(int a, int b)
    {
        base._init_(a + 100)
        this.w = b
    }

    int sum()
    {
        ret this.v + this.w
    }
}

# ---- 自引用成员（链表节点，注意 next 是保留字，用 nextNode） ----
MVNode
{
    int val = 0
    MVNode nextNode = null

    _init_(int v)
    {
        this.val = v
    }
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
        MVTest1.testCtorChain()
        MVTest1.testMethodBaseRef()
        MVTest1.testBaseClassRef()
        MVTest1.testPrivateMember()
        MVTest1.testInstanceIsolation()
        MVTest1.testSelfRefChain()
        MVTest1.testGrandStatic()
        MVTest1.testLiteralInherit()
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

    # 构造链：base._init_ 传参初始化基类成员，子类构造初始化自身成员
    static testCtorChain()
    {
        global.println("----- testCtorChain -----")
        MVCtorBase b = MVCtorBase(5)
        global.println("base: v=" + b.v)

        MVCtorDerived d = MVCtorDerived(11, 22)
        global.println("derived: v=" + d.v + " w=" + d.w)
        global.println("derived.sum()=" + d.sum())
    }

    # 子类/孙类方法中引用基类成员
    static testMethodBaseRef()
    {
        global.println("----- testMethodBaseRef -----")
        MVChild c = MVChild(30, 40)
        global.println("c.sumPC()=" + c.sumPC() + " (pVal + cVal)")

        MVGrandChild gc = MVGrandChild()
        global.println("gc.sumAll()=" + gc.sumAll() + " (pVal + cVal + gVal)")
    }

    # 基类引用指向子类对象，读/写继承的基类成员
    static testBaseClassRef()
    {
        global.println("----- testBaseClassRef -----")

        MVChild c = MVChild(7, 8)
        MVParent p = c as MVParent
        global.println("p.pVal=" + p.pVal + " p.pName=" + p.pName + " (read via base ref)")

        # 通过基类引用写继承成员，子类引用读回（同一存储）
        p.pVal = 500
        global.println("c.pVal=" + c.pVal + " (after write via base ref)")

        # 基类引用指向孙类对象
        MVGrandChild gc2 = MVGrandChild()
        MVParent p2 = gc2 as MVParent
        global.println("p2.pVal=" + p2.pVal + " p2.pName=" + p2.pName)
    }

    # private 成员在类内方法中可读写
    static testPrivateMember()
    {
        global.println("----- testPrivateMember -----")
        MVModifier m = MVModifier()
        global.println("getPriVal=" + m.getPriVal())
        global.println("setPriVal(250)=" + m.setPriVal(250))
        global.println("getPriVal(after)=" + m.getPriVal())
        global.println("getPriStVal=" + MVModifier.getPriStVal())
    }

    # 实例成员相互隔离，静态成员全局共享
    static testInstanceIsolation()
    {
        global.println("----- testInstanceIsolation -----")
        MVModifier m1 = MVModifier()
        MVModifier m2 = MVModifier()
        m1.pubVal = 111
        m2.pubVal = 222
        global.println("m1.pubVal=" + m1.pubVal + " m2.pubVal=" + m2.pubVal + " (isolated)")

        MVModifier.stVal = 3000
        global.println("stVal(via m1 read)=" + MVModifier.stVal)
        global.println("stVal(static shared)=" + MVModifier.stVal)
    }

    # 自引用成员构成链表，遍历读取
    static testSelfRefChain()
    {
        global.println("----- testSelfRefChain -----")
        MVNode n1 = MVNode(1)
        MVNode n2 = MVNode(2)
        MVNode n3 = MVNode(3)
        n1.nextNode = n2
        n2.nextNode = n3

        global.println("n1.val=" + n1.val + " n1.nextNode.val=" + n1.nextNode.val + " n1.nextNode.nextNode.val=" + n1.nextNode.nextNode.val)
        global.println("n3.nextNode=" + n3.nextNode)

        n2.nextNode = null
        global.println("after n2.nextNode=null, n1.nextNode.nextNode=" + n1.nextNode.nextNode)
    }

    # 静态继承语义：静态成员经继承扁平化后每个类持有自己的静态槽副本，
    # 经某类名读写只操作该类自己的槽，父类槽保持独立
    static testGrandStatic()
    {
        global.println("----- testGrandStatic -----")
        global.println("pStatic(via GrandChild)=" + MVGrandChild.pStatic)

        MVChild.pStatic = 5000
        global.println("pStatic(via Child after Child write)=" + MVChild.pStatic)
        global.println("pStatic(via Parent after Child write)=" + MVParent.pStatic)
        global.println("pStatic(via GrandChild after Child write)=" + MVGrandChild.pStatic)

        # 还原子类自己的槽，避免影响其他测试段
        MVChild.pStatic = 1000
        global.println("pStatic(restored via Child)=" + MVChild.pStatic)
    }

    # 继承类的字面量初始化：直接设置基类+子类成员
    static testLiteralInherit()
    {
        global.println("----- testLiteralInherit -----")
        MVChild lc = MVChild(){ pVal = 55, cVal = 66 }
        global.println("lc: pVal=" + lc.pVal + " pName=" + lc.pName + " cVal=" + lc.cVal + " cName=" + lc.cName)

        MVGrandChild lgc = MVGrandChild(){ pVal = 77, gVal = 88 }
        global.println("lgc: pVal=" + lgc.pVal + " cVal=" + lgc.cVal + " gVal=" + lgc.gVal)
    }
}
