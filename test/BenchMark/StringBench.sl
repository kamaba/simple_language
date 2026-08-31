StringBench
{
    static fun()
    {
        Console.println("========== StringBench (start) ==========")

        # 1. 字符串拼接
        nowMs = Environment.nowMillis()
        s = ""
        for i = 0, i < 10000, i++
        {
            s = s + "ab"
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("concat 1e4  [$nowMs.toString() ms]")

        # 2. 插值构建
        nowMs = Environment.nowMillis()
        t = ""
        for i = 0, i < 10000, i++
        {
            t = "v=$i s=${(i * 2).toString()}"
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("interp 1e4  [$nowMs.toString() ms]")

        # 3. format
        nowMs = Environment.nowMillis()
        u = ""
        for i = 0, i < 10000, i++
        {
            u = "{0}-{1}".format(i, i + 1)
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("format 1e4  [$nowMs.toString() ms]")

        # 4. 字符串比较
        nowMs = Environment.nowMillis()
        c = 0
        for i = 0, i < 100000, i++
        {
            if (s == t)
            {
                c++
            }
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("compare 1e5  count=" + c.toString() + "  [$nowMs.toString() ms]")

        # 5. 最终内容校验
        Console.println("concat endswith ab = " + (s == u).toString())

        Console.println("========== StringBench (end) ==========")
    }
}
