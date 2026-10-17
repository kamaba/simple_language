import Std

NaNBench
{
    static fun()
    {
        Console.println("========== NaNBench (start) ==========")

        nan = 0.0 / 0.0
        inf = 1.0 / 0.0
        one = 1.0

        # NaN/Inf 参与相等比较（Ceq/Cne F64 形态：comisd unordered 必须 False）
        if (nan == nan) { Console.println("nan==nan -> True") } else { Console.println("nan==nan -> False") }
        if (nan != nan) { Console.println("nan!=nan -> True") } else { Console.println("nan!=nan -> False") }
        if (nan == one) { Console.println("nan==one -> True") } else { Console.println("nan==one -> False") }
        if (one != nan) { Console.println("one!=nan -> True") } else { Console.println("one!=nan -> False") }
        if (nan == inf) { Console.println("nan==inf -> True") } else { Console.println("nan==inf -> False") }
        if (inf != one) { Console.println("inf!=one -> True") } else { Console.println("inf!=one -> False") }
        if (inf == inf) { Console.println("inf==inf -> True") } else { Console.println("inf==inf -> False") }

        # NaN/Inf 参与加法（Add F64 形态：IEEE 传播）
        Console.println("nan+one -> " + (nan + one).toString())
        Console.println("inf+one -> " + (inf + one).toString())
        Console.println("inf-inf -> " + (inf - inf).toString())
        if ((0.0 - nan) == nan) { Console.println("negnan==nan -> True") } else { Console.println("negnan==nan -> False") }

        # 边界值（I32 回绕加法 / I64 / F64 精度）
        Console.println("i32wrap -> " + (2147483647 + 1).toString())
        Console.println("i64sub  -> " + (9223372036854775807L - 1L).toString())
        Console.println("f64add  -> " + (0.1 + 0.2).toString())

        # 显式 Float64 类型（F64xF64 槽形态，X86_ASM_FASTPATH F64 分支探针）
        f64a = 0.1d
        f64b = 0.2d
        f64sum = 0.0d
        i = 0
        while (i < 1000000)
        {
            f64sum += f64a
            i++
        }
        Console.println("f64sum1e6 -> " + f64sum.toString())
        if (f64a == f64b) { Console.println("f64eq -> True") } else { Console.println("f64eq -> False") }
        if (f64a != f64b) { Console.println("f64ne -> True") } else { Console.println("f64ne -> False") }
        if (f64a < f64b) { Console.println("f64lt -> True") } else { Console.println("f64lt -> False") }
        f64inf = 1.0d / 0.0d
        f64nan2 = f64inf - f64inf
        if (f64inf == f64inf) { Console.println("f64inf==inf -> True") } else { Console.println("f64inf==inf -> False") }
        if (f64nan2 == f64nan2) { Console.println("f64nan2==nan2 -> True") } else { Console.println("f64nan2==nan2 -> False") }
        if (f64nan2 != f64nan2) { Console.println("f64nan2!=nan2 -> True") } else { Console.println("f64nan2!=nan2 -> False") }
        if (f64nan2 == f64b) { Console.println("f64nan2==b -> True") } else { Console.println("f64nan2==b -> False") }
        if (f64nan2 < f64b) { Console.println("f64nan2<b -> True") } else { Console.println("f64nan2<b -> False") }
        if (f64nan2 > f64b) { Console.println("f64nan2>b -> True") } else { Console.println("f64nan2>b -> False") }
        Console.println("f64nan2+0.1 -> " + (f64nan2 + f64a).toString())

        Console.println("========== NaNBench (end) ==========")
    }
}
