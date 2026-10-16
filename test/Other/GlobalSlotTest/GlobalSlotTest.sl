GlobalSlotTest
{
    static fun()
    {
        SystemPrintln("========== GlobalSlotTest (start) ==========")

        # 1. jsonc global.data 标量读（LoadGlobal 直发：登记成员 var1）
        SystemPrintln("[1] global.var1 = " + global.var1.toString() + "   (expect 12)")

        # 2. .sp Project 可写静态字段：读初值（LoadGlobal 直发）
        SystemPrintln("[2] global.gx initial = " + global.gx.toString() + "   (expect 100)")

        # 3. Project 可写静态字段：写 + 读回（StoreGlobal/LoadGlobal 双向直发主靶点）
        global.gx = 555
        SystemPrintln("[3] global.gx after write = " + global.gx.toString() + "   (expect 555)")

        # 4. 多轮写读（循环放大，每圈 store+load 都走槽直访）
        Int32 sum = 0
        Int32 i = 0
        while i < 100
        {
            global.gx = i
            sum = sum + global.gx
            i = i + 1
        }
        SystemPrintln("[4] loop sum = " + sum.toString() + "   (expect 4950)")
        SystemPrintln("[4] global.gx final = " + global.gx.toString() + "   (expect 99)")

        # 5. Project 浮点静态字段读写（另一槽位）
        global.gy = 2.75
        SystemPrintln("[5] global.gy after write = " + global.gy.toString() + "   (expect 2.75)")

        # 6. 本地类 const 静态字段读（const 登记路径）
        SystemPrintln("[6] GSC.PI = " + GSC.PI.toString() + "   (expect 3.14)")
        SystemPrintln("[6] GSC.COUNT = " + GSC.COUNT.toString() + "   (expect 42)")

        SystemPrintln("========== GlobalSlotTest (end) ==========")
    }
}

class GSC
{
    static const Float64 PI = 3.14
    static const Int32 COUNT = 42
}
