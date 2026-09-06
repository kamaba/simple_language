import Std

DataTypes
{
    static fun()
    {
        Console.println("========== DataTypes (start) ==========")

        # 1. Int32
        nowMs = Environment.nowMillis()
        s32 = 0
        for i = 0, i < 2000000, i++
        {
            s32 += i
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("Int32 sum 2e6 = " + s32.toString() + "  [$nowMs.toString() ms]")

        # 2. Int64
        nowMs = Environment.nowMillis()
        s64 = 0L
        for i = 0, i < 2000000, i++
        {
            s64 += i
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("Int64 sum 2e6 = " + s64.toString() + "  [$nowMs.toString() ms]")

        # 3. Num (double)
        nowMs = Environment.nowMillis()
        sf = 0.0
        for i = 0, i < 2000000, i++
        {
            sf += i * 1.5
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("Num sum 2e6 = " + sf.toString() + "  [$nowMs.toString() ms]")

        # 4. 混合运算（int/float 混用 + 取模）
        nowMs = Environment.nowMillis()
        mix = 0.0
        for i = 0, i < 1000000, i++
        {
            mix = mix + i / 3.0 + i % 7
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("Mixed ops 1e6 = " + mix.toString() + "  [$nowMs.toString() ms]")

        # 5. bool 短路判断
        nowMs = Environment.nowMillis()
        count = 0
        for i = 0, i < 1000000, i++
        {
            if (i % 3 == 0 && i % 5 == 0)
            {
                count++
            }
        }
        nowMs = Environment.nowMillis() - nowMs
       Console.println("bool mod 1e6 = " + count.toString() + "  [$nowMs.toString() ms]")

        Console.println("========== DataTypes (end) ==========")
    }
}
