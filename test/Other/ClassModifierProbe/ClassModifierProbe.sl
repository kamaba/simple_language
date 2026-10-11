namespace ClassModifierProbe
{
    #! 正例：类级修饰符合法组合（应全部编译通过） !#

    # P1: static data —— 合法（静态数据单例）
    static data GoodStaticData
    {
        counter = 0
    }

    # P2: const data —— 合法（只读数据）
    const data GoodConstData
    {
        name = "abc"
    }

    # P3: const enum —— 合法（只读枚举）
    const enum GoodConstEnum
    {
        One = 1
    }

    # P4: abstract class —— 合法
    abstract class GoodAbstractClass
    {
    }

    # P5: final class —— 合法
    final class GoodFinalClass
    {
        static GoodFinalCase()
        {
        }
    }

    ClassModifierProbeCase
    {
        static start()
        {
            global.println("ClassModifierProbe done")
        }
    }
}
