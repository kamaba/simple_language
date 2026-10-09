# ── Float8 (e4m3 / e5m2) 低精度浮点类型测试 ──
# 存储约定: Float8 底层用 uint8 保存位模式, fe4 = e4m3 字面量, fe5 = e5m2 字面量
Float8Test
{
    # ── 1. 字面量定义与舍入（fe4 / fe5 后缀）──
    static literalTest()
    {
        global.println("========== 1. float8 literal ==========")
        Float8 a = 1.5fe4
        global.println("1.5fe4 = " + a.toString())
        Float8_E5M2 b = 2.0fe5
        global.println("2.0fe5 = " + b.toString())
        # 舍入: e4m3 尾数 3 位, 0.3 舍入到最近值 0.25
        Float8 c = 0.3fe4
        global.println("0.3fe4 = " + c.toString())
        # e5m2 尾数只有 2 位, 0.3 同样舍入到 0.25
        Float8_E5M2 d = 0.3fe5
        global.println("0.3fe5 = " + d.toString())
        # e4m3 最大有限值 448 (exp=15, mant=110)
        Float8 maxv = 448.0fe4
        global.println("448.0fe4 = " + maxv.toString())
        # e5m2 最大有限值 57344 (exp=30, mant=11)
        Float8_E5M2 maxe5 = 57344.0fe5
        global.println("57344.0fe5 = " + maxe5.toString())
        # e4m3 最小次正规数 2^-9 = 0.001953125
        Float8 tiny = 0.001953125fe4
        global.println("0.001953125fe4 = " + tiny.toString())
        # 负数字面量
        Float8 neg = -2.5fe4
        global.println("-2.5fe4 = " + neg.toString())
        # 直接打印
        global.println(1.5fe4)
    }

    # ── 2. 算术运算（解码 -> double 计算 -> 重新编码）──
    static arithmeticTest()
    {
        global.println("========== 2. float8 arithmetic ==========")
        Float8 a = 1.5fe4
        Float8 b = 0.5fe4
        global.println("1.5 + 0.5 = " + (a + b).toString())
        global.println("1.5 - 0.5 = " + (a - b).toString())
        global.println("1.5 * 0.5 = " + (a * b).toString())
        global.println("1.5 / 0.5 = " + (a / b).toString())
        # 舍入: 81 = 1.265625 * 2^6, e4m3 尾数 3 位只能取 1.25 -> 80
        Float8 x = 9.0fe4
        global.println("9 * 9 = " + (x * x).toString())
        # 取反（翻转符号位）
        Float8 n = 2.5fe4
        Float8 neg = -n
        global.println("-2.5 = " + neg.toString())
        # 负数参与运算
        Float8 m = -1.5fe4
        global.println("-1.5 + 2.5 = " + (m + n).toString())
    }

    # ── 3. 与 Float32 / Float64 / e5m2 的强制转换 ──
    static convertTest()
    {
        global.println("========== 3. float8 convert ==========")
        # Float32 -> Float8（普通类型需要强制转换）
        Float32 f = 9.5f
        Float8 a = f
        global.println("Float32 9.5 -> Float8 = " + a.toString())
        # Float8 -> Float32
        Float8 b = 0.3fe4
        Float32 g = b
        global.println("Float8 0.25 -> Float32 = " + g.toString())
        # Float64 -> Float8
        Float64 d = 12.0
        Float8 c = d
        global.println("Float64 12 -> Float8 = " + c.toString())
        # Float8 -> Float64
        Float64 e = c
        global.println("Float8 12 -> Float64 = " + e.toString())
        # e4m3 与 e5m2 互转
        Float8 m = 3.5fe4
        Float8_E5M2 n = m
        global.println("Float8 3.5 -> Float8_E5M2 = " + n.toString())
        Float8 o = n
        global.println("Float8_E5M2 3.5 -> Float8 = " + o.toString())
        # 赋值语句中的转换（非初始化）
        Float8 p = 0.0fe4
        p = 6.25f
        global.println("p = 6.25f -> " + p.toString())
    }

    # ── 4. 比较运算（位模式解码后按值比较）──
    static compareTest()
    {
        global.println("========== 4. float8 compare ==========")
        Float8 a = 2.0fe4
        Float8 b = 3.0fe4
        global.println("2 > 3 = " + (a > b).toString())
        global.println("2 < 3 = " + (a < b).toString())
        global.println("2 == 3 = " + (a == b).toString())
        Float8 c = 2.0fe4
        global.println("2 == 2 = " + (a == c).toString())
        global.println("2 != 3 = " + (a != b).toString())
        # 负数: -3 的位模式(0xC4=196)大于 2 的位模式(0x40=64), 但值比较应正确
        Float8 n = -3.0fe4
        global.println("-3 < 2 = " + (n < a).toString())
        global.println("-3 >= 2 = " + (n >= a).toString())
        # 与 Float32 混合比较
        Float32 f = 2.0f
        global.println("2fe4 == 2.0f = " + (a == f).toString())
        # e5m2 比较
        Float8_E5M2 p = 57344.0fe5
        Float8_E5M2 q = 1.0fe5
        global.println("57344 > 1 = " + (p > q).toString())
    }

    # ── 5. 变量存储（底层 uint8 位模式存取）──
    static storageTest()
    {
        global.println("========== 5. float8 storage ==========")
        Float8 a = 7.5fe4
        global.println("a = " + a.toString())
        a = 0.5fe4
        global.println("a = " + a.toString())
        Float8_E5M2 b = 128.0fe5
        global.println("b = " + b.toString())
        b = 0.25fe5
        global.println("b = " + b.toString())
        # 条件判断 (显式比较)
        Float8 z = 0.0fe4
        if (z != 0.0fe4)
        {
            global.println("0.0fe4 is truthy (ERROR)")
        }
        else
        {
            global.println("0.0fe4 is falsy")
        }
        Float8 nz = 0.001953125fe4
        if (nz != 0.0fe4)
        {
            global.println("0.001953125fe4 is truthy")
        }
        else
        {
            global.println("0.001953125fe4 is falsy (ERROR)")
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
        global.println("========== all float8 tests done ==========")
    }
}
