GlobalSlotTest
{
    static fun()
    {
        SystemPrintln("========== GlobalSlotTest (start) ==========")

        # 1. jsonc global.data 标量读（LoadGlobal 直发：const 登记成员）
        SystemPrintln("[1] global.var1 = " + global.var1.toString() + "   (expect 12)")

        # 2. .sp Project const 全局读（LoadGlobal 直发）
        SystemPrintln("[2] global.gx = " + global.gx.toString() + "   (expect 100)")
        SystemPrintln("[2] global.gy = " + global.gy.toString() + "   (expect 1.5)")

        # 3. jsonc global.data 数组成员整体赋值（StoreGlobal 直发：数组引用成员非 const 可写）。
        #    SL 既有语义（-O0/-O2 输出一致已实证）：数组整体赋值先清空再逐元素覆盖
        #    （清零 + StoreArrayIndex×N + StoreGlobal 写回引用），不替换引用本身
        #    ——长度不变，前 N 个元素被覆盖，尾部清零。
        global.arrvar1 = [9, 8, 7]
        SystemPrintln("[3] after write, arrvar1.length = " + global.arrvar1.length.toString() + "   (expect 5, keep ref)")
        SystemPrintln("[3] arrvar1[0..2] = " + global.arrvar1[0].toString() + "," + global.arrvar1[1].toString() + "," + global.arrvar1[2].toString() + "   (expect 9,8,7)")
        SystemPrintln("[3] arrvar1[3..4] = " + global.arrvar1[3].toString() + "," + global.arrvar1[4].toString() + "   (expect 0,0, tail zeroed)")

        # 4. 普通类可写静态（走 StoreStaticField(37) -> P1 装配改写 38 槽直访的对照路径）
        Counter.value = 7
        SystemPrintln("[4] Counter.value = " + Counter.value.toString() + "   (expect 7)")
        Int32 sum = 0
        Int32 i = 0
        while i < 100
        {
            Counter.value = i
            sum = sum + Counter.value
            i = i + 1
        }
        SystemPrintln("[4] loop sum = " + sum.toString() + "   (expect 4950)")
        SystemPrintln("[4] Counter.value final = " + Counter.value.toString() + "   (expect 99)")

        # 5. 本地类 const 静态字段读（const 登记路径，LoadGlobal 直发）
        SystemPrintln("[5] GSC.PI = " + GSC.PI.toString() + "   (expect 3.14)")
        SystemPrintln("[5] GSC.COUNT = " + GSC.COUNT.toString() + "   (expect 42)")

        # 6. jsonc global.data 对象成员读
        SystemPrintln("[6] global.vardata2.a = " + global.vardata2.a.toString() + "   (expect 10)")
        SystemPrintln("[6] global.vardata2.b = " + global.vardata2.b.toString() + "   (expect 20.1)")

        SystemPrintln("========== GlobalSlotTest (end) ==========")
    }
}

class Counter
{
    static Int32 value = 0
}

class GSC
{
    static const Float64 PI = 3.14
    static const Int32 COUNT = 42
}
