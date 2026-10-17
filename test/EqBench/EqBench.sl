import Std

EqBench
{
    static fun()
    {
        Console.println("========== EqBench (start) ==========")

        # 1. Int32 != 循环（Cne：Int32 x Int32）
        nowMs = Environment.sys.nowMillis()
        k = 0
        s = 0
        while (k != 2000000)
        {
            s += k
            k++
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println("i32 != 2e6 = " + s.toString() + "  [$nowMs.toString() ms]")

        # 2. Int64 != 循环（Cne：Int64 x Int64）
        nowMs = Environment.sys.nowMillis()
        k64 = 0L
        s64 = 0L
        while (k64 != 2000000L)
        {
            s64 += k64
            k64++
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println("i64 != 2e6 = " + s64.toString() + "  [$nowMs.toString() ms]")

        # 3. Num/F64 != 循环（Cne：F64 x F64）
        nowMs = Environment.sys.nowMillis()
        kd = 0.0
        sd = 0.0
        while (kd != 2000000.0)
        {
            sd += kd
            kd += 1.0
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println("f64 != 2e6 = " + sd.toString() + "  [$nowMs.toString() ms]")

        # 4. bool == 循环（Ceq：BOOL x BOOL）
        nowMs = Environment.sys.nowMillis()
        b = true
        cnt = 0
        for i = 0, i < 1000000, i++
        {
            if (b == true)
            {
                cnt++
            }
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println("bool == 1e6 = " + cnt.toString() + "  [$nowMs.toString() ms]")

        Console.println("========== EqBench (end) ==========")
    }
}
