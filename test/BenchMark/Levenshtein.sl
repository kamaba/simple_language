import Std

Levenshtein
{
    static fun()
    {
        Console.println("========== Levenshtein (start) ==========")
        # 两个整数序列（模拟字符码点）做编辑距离 DP
        Int32[] a = Array<Int32>.create(6)
        a[0] = 10
        a[1] = 20
        a[2] = 30
        a[3] = 40
        a[4] = 50
        a[5] = 60
        Int32[] b = Array<Int32>.create(7)
        b[0] = 10
        b[1] = 25
        b[2] = 30
        b[3] = 35
        b[4] = 40
        b[5] = 50
        b[6] = 65

        dist = Levenshtein.distance(a, b)
       Console.println("levenshtein(a,b) = " + dist.toString())

        nowMs = Environment.nowMillis()
        total = 0L
        for r = 0, r < 5000, r++
        {
            total += Levenshtein.distance(a, b)
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("levenshtein x5000 = " + total.toString() + "  [$nowMs.toString() ms]")
        Console.println("========== Levenshtein (end) ==========")
    }

    static int distance(Int32[] a, Int32[] b)
    {
        m = a.length
        n = b.length
        Int32[] prev = Array<Int32>.create(n + 1)
        Int32[] curr = Array<Int32>.create(n + 1)
        for j = 0, j <= n, j++
        {
            prev[j] = j
        }
        for i = 1, i <= m, i++
        {
            curr[0] = i
            for j = 1, j <= n, j++
            {
                cost = a[i - 1] == b[j - 1] ? 0 : 1
                del = prev[j] + 1
                ins = curr[j - 1] + 1
                sub = prev[j - 1] + cost
                curr[j] = Levenshtein.min3(del, ins, sub)
            }
            tmp = prev
            prev = curr
            curr = tmp
        }
        ret prev[n]
    }

    static Int32 min3(Int32 x, Int32 y, Int32 z)  
    {
        if (x < y && x < z)
        {
            ret x
        }
        if (y < z)
        {
            ret y
        }
        ret z
    }
}
