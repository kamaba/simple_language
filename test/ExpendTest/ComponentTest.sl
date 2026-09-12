import Std;
import Core;

# Component 查询/消息系列下沉 C VM 系统方法（SystemComponent*）后的语义验证用例。
# 覆盖：getComponent<T>/Type/string、getComponents<T>、InChildren/InParent 系列、
#       includeInactive 门控、tryGetComponent、sendMessage/Upwards/broadcast、压力循环。

CompSpeed extends Component
{
    public int power = 0

    override void onMessage( string methodName )
    {
        Console.println("  [onMessage] " + this.name + "(CompSpeed) <= " + methodName)
    }
}

CompLife extends Component
{
    public int hp = 0

    override void onMessage( string methodName )
    {
        Console.println("  [onMessage] " + this.name + "(CompLife) <= " + methodName)
    }
}

ComponentTest
{
    static check( string title, bool ok )
    {
        if ok
        {
            Console.println("[PASS] " + title)
        }
        else
        {
            Console.println("[FAIL] " + title)
        }
    }

    # 树1（全部启用）：
    # root ─ speedA(CompSpeed) ─ life1(CompLife)
    #      │                   └ life2(CompLife)
    #      ├ lifeB(CompLife) ─ speed3(CompSpeed)
    #      └ speedC(CompSpeed)
    static Component makeTree1()
    {
        Component root = new()
        root.name = "root"

        CompSpeed speedA = new()
        speedA.name = "speedA"
        CompLife life1 = new()
        life1.name = "life1"
        CompLife life2 = new()
        life2.name = "life2"
        CompLife lifeB = new()
        lifeB.name = "lifeB"
        CompSpeed speed3 = new()
        speed3.name = "speed3"
        CompSpeed speedC = new()
        speedC.name = "speedC"

        root.addComponent(speedA)
        root.addComponent(lifeB)
        root.addComponent(speedC)
        speedA.addComponent(life1)
        speedA.addComponent(life2)
        lifeB.addComponent(speed3)
        ret root
    }

    # 树2（门控）：gate(CompLife, 禁用)
    # ├── hiddenOff(CompSpeed, 禁用) ─ deepLife(CompLife, 启用)
    # └── okSpeed(CompSpeed, 启用)
    static Component makeTree2()
    {
        CompLife gate = new()
        gate.name = "gate"
        gate.setEnabled(false)

        CompSpeed hiddenOff = new()
        hiddenOff.name = "hiddenOff"
        hiddenOff.setEnabled(false)

        CompLife deepLife = new()
        deepLife.name = "deepLife"

        CompSpeed okSpeed = new()
        okSpeed.name = "okSpeed"

        gate.addComponent(hiddenOff)
        gate.addComponent(okSpeed)
        hiddenOff.addComponent(deepLife)
        ret gate
    }

    static testBasicQuery()
    {
        Console.println("===== testBasicQuery =====")
        Component root = makeTree1()

        # getComponent<T>()：第一个匹配的直接子级
        CompSpeed r1 = root.getComponent<CompSpeed>()
        check("getComponent<CompSpeed> -> speedA", r1 != null && r1.name == "speedA")

        CompLife r2 = root.getComponent<CompLife>()
        check("getComponent<CompLife> -> lifeB", r2 != null && r2.name == "lifeB")

        # getComponent(Type)：RuntimeType 指针比较（修复 SL 层引用比较恒 false 的 bug）
        CompSpeed r3 = root.getComponent(r1.type)
        check("getComponent(Type) -> speedA", r3 != null && r3.name == "speedA")

        # getComponent(string)：短名比较
        CompSpeed r4 = root.getComponent("CompSpeed")
        check("getComponent(string CompSpeed) -> speedA", r4 != null && r4.name == "speedA")

        Component r5 = root.getComponent("NoSuchType")
        check("getComponent(string NoSuchType) -> null", r5 == null)

        # getComponents<T>()：按挂载顺序收集
        List<Component> rs = root.getComponents<CompSpeed>()
        check("getComponents<CompSpeed>.length == 2", rs != null && rs.length == 2)
        if rs != null && rs.length == 2
        {
            check("getComponents[0] == speedA", rs._getItem_(0) != null && rs._getItem_(0).name == "speedA")
            check("getComponents[1] == speedC", rs._getItem_(1) != null && rs._getItem_(1).name == "speedC")
        }

        # 未命中返回空列表（非 null）
        CompSpeed leaf = new()
        leaf.name = "leaf"
        List<Component> rs2 = leaf.getComponents<CompSpeed>()
        check("leaf getComponents -> empty", rs2 != null && rs2.length == 0)

        # tryGetComponent：与 getComponent 同语义
        CompLife r6 = root.tryGetComponent<CompLife>()
        check("tryGetComponent<CompLife> -> lifeB", r6 != null && r6.name == "lifeB")

        CompSpeed r7 = leaf.tryGetComponent<CompSpeed>()
        check("leaf tryGetComponent -> null", r7 == null)
    }

    static testHierarchyQuery()
    {
        Console.println("===== testHierarchyQuery =====")
        Component root = makeTree1()
        CompLife lifeB = root.getComponent<CompLife>()
        CompSpeed speedA = root.getComponent<CompSpeed>()

        # getComponentInChildren：先序深度优先，不含自身
        CompLife r1 = root.getComponentInChildren<CompLife>(false)
        check("root InChildren<CompLife> -> life1", r1 != null && r1.name == "life1")

        CompSpeed r2 = lifeB.getComponentInChildren<CompSpeed>(false)
        check("lifeB InChildren<CompSpeed> -> speed3", r2 != null && r2.name == "speed3")

        # getComponentInParent：沿 parent 链（含自身）第一个匹配
        CompLife life1 = speedA.getComponentInChildren<CompLife>(false)
        CompSpeed r3 = life1.getComponentInParent<CompSpeed>(false)
        check("life1 InParent<CompSpeed> -> speedA", r3 != null && r3.name == "speedA")

        # 基类匹配：含自身沿链，life1 自身即 Component（CompLife extends Component，as 上转型命中）
        Component r4 = life1.getComponentInParent<Component>(false)
        check("life1 InParent<Component> -> life1", r4 != null && r4.name == "life1")

        # getComponentsInParent：沿链全部收集（含自身：life1, speedA, root）
        List<Component> rs = life1.getComponentsInParent<Component>(false)
        check("life1 InParents<Component>.length == 3", rs != null && rs.length == 3)
        if rs != null && rs.length == 3
        {
            check("InParents[0] == life1", rs._getItem_(0).name == "life1")
            check("InParents[1] == speedA", rs._getItem_(1).name == "speedA")
            check("InParents[2] == root", rs._getItem_(2).name == "root")
        }

        # getComponentsInChildren：整棵子树先序收集
        List<Component> rs2 = root.getComponentsInChildren<CompLife>(false)
        # life1, life2, lifeB 共 3 个
        check("root InChildrenList<CompLife>.length == 3", rs2 != null && rs2.length == 3)
    }

    static testInactiveGate()
    {
        Console.println("===== testInactiveGate =====")
        Component gate = makeTree2()

        # includeInactive=false：hiddenOff(禁用) 被门挡，okSpeed 匹配
        CompSpeed r1 = gate.getComponentInChildren<CompSpeed>(false)
        check("gate InChildren<CompSpeed>(false) -> okSpeed", r1 != null && r1.name == "okSpeed")

        # includeInactive=true：第一个挂载的 hiddenOff 匹配
        CompSpeed r2 = gate.getComponentInChildren<CompSpeed>(true)
        check("gate InChildren<CompSpeed>(true) -> hiddenOff", r2 != null && r2.name == "hiddenOff")

        # 递归不受门控：hiddenOff 自身被门挡，但其子树内 deepLife 仍可命中
        CompLife deepLife = gate.getComponentInChildren<CompLife>(false)
        check("gate InChildren<CompLife>(false) -> deepLife（递归越门）", deepLife != null && deepLife.name == "deepLife")

        # InParent 门控：deepLife 链 = hiddenOff(禁用) -> gate(CompLife 不匹配)
        CompSpeed r4 = deepLife.getComponentInParent<CompSpeed>(false)
        check("deepLife InParent<CompSpeed>(false) -> null", r4 == null)

        CompSpeed r5 = deepLife.getComponentInParent<CompSpeed>(true)
        check("deepLife InParent<CompSpeed>(true) -> hiddenOff", r5 != null && r5.name == "hiddenOff")

        # 列表门控
        List<Component> rs1 = gate.getComponentsInChildren<CompSpeed>(false)
        check("gate InChildrenList<CompSpeed>(false).length == 1", rs1 != null && rs1.length == 1)

        List<Component> rs2 = gate.getComponentsInChildren<CompSpeed>(true)
        check("gate InChildrenList<CompSpeed>(true).length == 2", rs2 != null && rs2.length == 2)

        List<Component> rs3 = deepLife.getComponentsInParent<CompSpeed>(false)
        check("deepLife InParentsList<CompSpeed>(false).length == 0", rs3 != null && rs3.length == 0)

        List<Component> rs4 = deepLife.getComponentsInParent<CompSpeed>(true)
        check("deepLife InParentsList<CompSpeed>(true).length == 1", rs4 != null && rs4.length == 1)
    }

    static testMessages()
    {
        Console.println("===== testMessages =====")
        Component root = makeTree1()
        CompSpeed speedA = root.getComponent<CompSpeed>()
        CompLife life1 = speedA.getComponentInChildren<CompLife>(false)

        # sendMessage：自身 + 每个子组件（整棵子树，不受 enabled 门控）
        Console.println("-- sendMessage(ping) from speedA（期望 speedA, life1, life2 各一行）--")
        speedA.sendMessage("ping")

        # sendMessageUpwards：沿 parent 链含自身（root 基类 onMessage 为空，无输出）
        Console.println("-- sendMessageUpwards(up) from life1（期望 life1, speedA 各一行）--")
        life1.sendMessageUpwards("up")

        # broadcastMessage：转发 sendMessage
        Console.println("-- broadcastMessage(bc) from root（期望 speedA, life1, life2, lifeB, speed3, speedC 顺序各一行）--")
        root.broadcastMessage("bc")
    }

    static testStress()
    {
        Console.println("===== testStress（10000 次查询 + 列表构造，验证池化稳定）=====")
        Component root = makeTree1()
        int hit = 0
        CompSpeed r = null
        List<Component> rs = null
        for i = 0, i < 10000, i++
        {
            r = root.getComponent<CompSpeed>()
            if r != null
            {
                hit = hit + 1
            }
            rs = root.getComponentsInChildren<CompLife>(false)
            if rs.length == 3
            {
                hit = hit + 1
            }
        }
        check("stress hit == 20000", hit == 20000)
    }

    static fun()
    {
        Console.println("========== ComponentTest ==========")
        testBasicQuery()
        testHierarchyQuery()
        testInactiveGate()
        testMessages()
        testStress()
        Console.println("========== ComponentTest done ==========")
    }
}
