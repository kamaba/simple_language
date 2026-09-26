
namespace ETC1
{
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

        override _init_(int z1)
        {
            this.x1 = 1
            this.y1 = 2
            base._init_(z1 + 10)
            this.z1 = z1
        }

        string describe()
        {
            ret "Class1_1 a=" + this.a.toString() + " x1=" + this.x1.toString() + " y1=" + this.y1.toString() + " z1=" + this.z1.toString()
        }
    }

    ExtendsClass
    {
        static fun()
        {
            global.println("========== ExtendClass / ExtendsClass (start) ==========")

            c11 = Class1_1(20)
            global.println("c11: " + c11.describe())

            c12 = Class1_1(20, 30)
            global.println("c12: " + c12.describe())

            c13 = Class1_1(0)
            global.println("c13 (单参 0): " + c13.describe())

            c14 = Class1(99)
            global.println("c14 base Class1 a=" + c14.a.toString())

            c15 = Class1(20)
            global.println("c15 base Class1 a=" + c15.a.toString())

            global.println("========== ExtendClass / ExtendsClass (end) ==========")
        }
    }
}

# 测试用例说明：
# - 覆盖子类多重重载 _init_ 与 base._init_ 调用顺序。
# - Class1_1()：默认/无参构造路径（若语言允许空括号）。
# - Class1 / Class1_1 带参构造：基类字段 a 与子类 x1/y1/z1 的组合结果。
#
# 预期结果：
# - 每次修改构造语义后，describe() 打印应整体一致地变化，用于回归。
# - 无异常、无未初始化访问；若某构造在语言中非法，编译期会报错，此时应调整用例以匹配当前语法。
