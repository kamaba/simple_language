import Std

Loop
{
    static fun()
    {
        Console.println("========== Loop (start) ==========")

        # 1. 双层 for + 数组读写
        n = 1000
        Int32[] arr = Array<Int32>.create(n)
        for i = 0, i < n, i++
        {
            arr[i] = i * i
        }
        nowMs = Environment.nowMillis()
        total = 0L
        for j = 0, j < 3000, j++
        {
            s = 0L
            for i = 0, i < n, i++
            {
                s += arr[i]
            }
            total += s
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("array sum x3000 = " + total.toString() + "  [$nowMs.toString() ms]")

        # 2. while 循环
        nowMs = Environment.nowMillis()
        wsum = 0L
        k = 0
        while (k < 5000000)
        {
            wsum += k
            k++
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("while sum 5e6 = " + wsum.toString() + "  [$nowMs.toString() ms]")

        # 3. for 步进
        nowMs = Environment.nowMillis()
        stepSum = 0L
        for i = 0, i < 1000000, i += 7
        {
            stepSum += i
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("for step +7 1e6 = " + stepSum.toString() + "  [$nowMs.toString() ms]")

        Console.println("========== Loop (end) ==========")
    }
}
