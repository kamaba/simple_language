# ── Float16 (IEEE half) / Float16Brain (bfloat16) 低精度浮点类型测试 ──
# 存储约定: 底层用 uint16 保存位模式, h = IEEE float16 字面量, hb = bfloat16 字面量
Float16Test
{
    # ── 1. 字面量定义与舍入（h / hb 后缀）──
    static literalTest()
    {
        global.println("========== 1. float16 literal ==========")
        Float16 a = 1.5h
        global.println("1.5h = " + a.toString())
        Float16_Brain b = 2.0hb
        global.println("2.0hb = " + b.toString())
        # 舍入: IEEE half 尾数 10 位, 0.3 舍入到 0.300048828125
        Float16 c = 0.3h
        global.println("0.3h = " + c.toString())
        # bfloat16 尾数只有 7 位, 0.3 舍入到 0.30078125
        Float16_Brain d = 0.3hb
        global.println("0.3hb = " + d.toString())
        # IEEE half 最大有限值 65504 (exp=30, mant=1023)
        Float16 maxv = 65504.0h
        global.println("65504.0h = " + maxv.toString())
        # bfloat16 指数域与 Float32 相同, 最大有限值约 3.3895e38
        Float16_Brain maxb = 3.3895314e38hb
        global.println("3.3895314e38hb = " + maxb.toString())
        # IEEE half 最小次正规数 2^-24 = 0.000000059604644775390625
        Float16 tiny = 5.9604644775390625e-8h
        global.println("5.9604644775390625e-8h = " + tiny.toString())
        # 负数字面量
        Float16 neg = -2.5h
        global.println("-2.5h = " + neg.toString())
        # 直接打印
        global.println(1.5h)
        global.println(1.5hb)
    }

    # ── 2. 算术运算（解码 -> double 计算 -> 重新编码）──
    static arithmeticTest()
    {
        global.println("========== 2. float16 arithmetic ==========")
        Float16 a = 1.5h
        Float16 b = 0.5h
        global.println("1.5 + 0.5 = " + (a + b).toString())
        global.println("1.5 - 0.5 = " + (a - b).toString())
        global.println("1.5 * 0.5 = " + (a * b).toString())
        global.println("1.5 / 0.5 = " + (a / b).toString())
        # 81 = 1.265625 * 2^6, half 尾数 10 位可精确表示 -> 81
        Float16 x = 9.0h
        global.println("9 * 9 = " + (x * x).toString())
        # bfloat16: 81 = 1.265625 * 2^6, 尾数 7 位(0.265625 -> 0100010)同样精确
        Float16_Brain y = 9.0hb
        global.println("9 * 9 (brain) = " + (y * y).toString())
        # 取反（翻转符号位）
        Float16 n = 2.5h
        Float16 neg = -n
        global.println("-2.5 = " + neg.toString())
        # 负数参与运算
        Float16 m = -1.5h
        global.println("-1.5 + 2.5 = " + (m + n).toString())
        # bfloat16 0.3 + 0.3 = 0.6015625
        Float16_Brain p = 0.3hb
        global.println("0.3hb + 0.3hb = " + (p + p).toString())
    }

    # ── 3. 与 Float32 / Float64 / bfloat16 的强制转换 ──
    static convertTest()
    {
        global.println("========== 3. float16 convert ==========")
        # Float32 -> Float16（普通类型需要强制转换）
        Float32 f = 9.5f
        Float16 a = f
        global.println("Float32 9.5 -> Float16 = " + a.toString())
        # Float16 -> Float32
        Float16 b = 0.3h
        Float32 g = b
        global.println("Float16 0.300048828125 -> Float32 = " + g.toString())
        # Float64 -> Float16
        Float64 d = 12.0
        Float16 c = d
        global.println("Float64 12 -> Float16 = " + c.toString())
        # Float16 -> Float64
        Float64 e = c
        global.println("Float16 12 -> Float64 = " + e.toString())
        # Float32 -> bfloat16
        Float32 fb = 9.5f
        Float16_Brain ab = fb
        global.println("Float32 9.5 -> Float16_Brain = " + ab.toString())
        # IEEE half 与 bfloat16 互转
        Float16 m = 3.5h
        Float16_Brain n = m
        global.println("Float16 3.5 -> Float16_Brain = " + n.toString())
        Float16 o = n
        global.println("Float16_Brain 3.5 -> Float16 = " + o.toString())
        # 赋值语句中的转换（非初始化）
        Float16 p = 0.0h
        p = 6.25f
        global.println("p = 6.25f -> " + p.toString())
    }

    # ── 4. 比较运算（位模式解码后按值比较）──
    static compareTest()
    {
        global.println("========== 4. float16 compare ==========")
        Float16 a = 2.0h
        Float16 b = 3.0h
        global.println("2 > 3 = " + (a > b).toString())
        global.println("2 < 3 = " + (a < b).toString())
        global.println("2 == 3 = " + (a == b).toString())
        Float16 c = 2.0h
        global.println("2 == 2 = " + (a == c).toString())
        global.println("2 != 3 = " + (a != b).toString())
        # 负数: -3 的位模式(0xC200)大于 2 的位模式(0x4000), 但值比较应正确
        Float16 n = -3.0h
        global.println("-3 < 2 = " + (n < a).toString())
        global.println("-3 >= 2 = " + (n >= a).toString())
        # 与 Float32 混合比较
        Float32 f = 2.0f
        global.println("2h == 2.0f = " + (a == f).toString())
        # bfloat16 比较
        Float16_Brain p = 128.0hb
        Float16_Brain q = 0.25hb
        global.println("128 > 0.25 = " + (p > q).toString())
        # half 与 brain 混合比较（升 Float32 后比较）
        global.println("128.0hb > 2.0h = " + (p > a).toString())
    }

    # ── 5. 变量存储（底层 uint16 位模式存取）──
    static storageTest()
    {
        global.println("========== 5. float16 storage ==========")
        Float16 a = 7.5h
        global.println("a = " + a.toString())
        a = 0.5h
        global.println("a = " + a.toString())
        Float16_Brain b = 128.0hb
        global.println("b = " + b.toString())
        b = 0.25hb
        global.println("b = " + b.toString())
        # 条件判断 (显式比较)
        Float16 z = 0.0h
        if (z != 0.0h)
        {
            global.println("0.0h is truthy (ERROR)")
        }
        else
        {
            global.println("0.0h is falsy")
        }
        Float16 nz = 5.9604644775390625e-8h
        if (nz != 0.0h)
        {
            global.println("5.96e-8h is truthy")
        }
        else
        {
            global.println("5.96e-8h is falsy (ERROR)")
        }
    }

    # ── main entry ──
    static fun()
    {
        literalTest()
        arithmeticTest()
        convertTest()
        compareTest()
        storageTest()
        global.println("========== all float16 tests done ==========")
    }
}
