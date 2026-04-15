import Application.Core

Class1
{
    int a = 20

    _init_(int _a)
    {
        this.a = _a
    }
}

Class1_1 extends Class1
{
    x1 = 0
    y1 = 0
    z1 = 0

    _init_(int _x1, int _y1)
    {
        base._init_(_x1 + 1)
        this.x1 = _x1
        this.y1 = _y1
    }

    _init_(int z1)
    {
        _init_(1, 2)
        base._init_(z1 + 10)
        this.z1 = z1
    }

    int sumFields()
    {
        ret this.a + this.x1 + this.y1 + this.z1
    }
}

ValueCompareTest
{
    static refEqualitySmoke()
    {
        global.println("----- refEqualitySmoke -----")
        Class1_1 cA = Class1_1(20, 30)
        Class1_1 cB = cA
        Class1_1 cC = Class1_1(20, 30)
        global.println("cA==cB (alias) -> " + (cA == cB).toString())
        global.println("cA==cC (distinct) -> " + (cA == cC).toString())
    }

    static intCompareSmoke()
    {
        global.println("----- intCompareSmoke -----")
        int x = 2
        int y = 2
        global.println("x == y -> " + (x == y).toString())
        global.println("x === y (value) -> " + (x === y).toString())
    }

    static Fun()
    {
        global.println("========== ValueCompareTest (start) ==========")
        refEqualitySmoke()
        intCompareSmoke()

        c11 = Class1_1(20)
        global.println("Class1_1(20) sumFields -> " + c11.sumFields().toString())

        c12 = Class1_1(5, 6)
        global.println("Class1_1(5,6) sumFields -> " + c12.sumFields().toString())

        global.println("========== ValueCompareTest (end) ==========")
    }
}

# 测试用例说明：
# - refEqualitySmoke：同一引用别名应相等；另建同参实例引用比较一般为 false（语言若重载 == 另议）。
# - intCompareSmoke：基本整型的 == / === 行为对照。
# - Class1_1 构造链：单参构造会链式调用双参与基类 _init_，sumFields 用于观察字段是否按预期写入。
#
# 预期结果：
# - cA==cB 为 true；cA==cC 一般为 false。
# - 整型 x、y 均为 2 时 == 与 === 均为 true。
# - sumFields 数值随构造规则变化，以输出为准用于回归对比。
