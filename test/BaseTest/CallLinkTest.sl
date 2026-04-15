import Std
import CSharp.System

Class1
{
    c1_1 = 10
}

Class2 extends Class1
{
    c2_1 = 20
    Class1 c2_2 = new()
}

Class3
{
    c3_1 = Class2()

    Class2 GetClass2()
    {
        ret this.c3_1
    }
}

Class4 extends Class3
{
    Class3 c4_1 = null
    int c4_2 = 0
    Class3 c4_3 = new()
}

CallLinkTest
{
    static fun()
    {
        global.println("========== CallLinkTest (start) ==========")
        global.println("面向：成员访问链 obj.f().g.h 与赋值、中间返回引用的一致性。")

        Class4 c4 = Class4()
        c4.c4_1 = Class3()
        c4.c4_3.GetClass2().c2_2.c1_1 = 40
        c4.GetClass2().c2_1 = c4.c4_3.GetClass2().c2_2.c1_1
        newc1 = c4.GetClass2().c2_1
        global.println("GetClass2().c2_1 (应等于 40) -> " + newc1.toString())
        global.println("末端 c1_1 -> " + c4.c4_3.GetClass2().c2_2.c1_1.toString())

        # 以下为依赖 Application.Core / ClassT 的链式与匿名 data 草稿，需在对应工程就绪后启用
        # a20 = 10000 + 20
        # a21 = ClassT(10){ t = 1 }
        # d2 = { ct = ClassT(20), childd1 = { a3 = { qx = 1024 } }, ch2 = [1,2,3,4], ha = a21, ch3 = [ { a = 20 }, { a = { ax = 10241 } } ], y2 = "sss" }

        global.println("========== CallLinkTest (end) ==========")
    }
}

# 测试面向：多级类组合下「变量.成员」「返回值.成员」连续访问（调用链）与跨层赋值。
# 预期：c4_3.GetClass2() 与 GetClass2() 指向同一套嵌套对象时，c2_1 与末端 c1_1 均为 40；无 ClassT 依赖段时不应引用 vx 等未定义符号。
