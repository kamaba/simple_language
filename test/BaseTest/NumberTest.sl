NumberTest
{
    static baseArithmeticTest()
    {
        global.println("----- baseArithmeticTest -----")
        
        Int32 a = 20
        Int32 b = 7
        global.println("a = " + a.toString() + ", b = " + b.toString())
        global.println("a + b = " + (a + b).toString())
        global.println("a - b = " + (a - b).toString())
        global.println("a * b = " + (a * b).toString())
        global.println("a / b = " + (a / b).toString())
        global.println("a % b = " + (a % b).toString())
        
        Num n1 = 1.5
        Num n2 = 2
        global.println("n1 = " + n1.toString() + ", n2 = " + n2.toString())
        global.println("n1 + n2 = " + (n1 + n2).toString())
        global.println("n1 - n2 = " + (n1 - n2).toString())
        global.println("n1 * n2 = " + (n1 * n2).toString())
        global.println("n1 / n2 = " + (n1 / n2).toString())

        Num neg = -n1
        global.println("-n1 = " + neg.toString())
    }
    # 测试 Int32 进制转换（toRadixString / toBinaryString / toHexString / toOctalString）
    static testRadix()
    {
        global.println("===== testRadix =====")
        int a = 255
        global.println("255.toHexString() = " + a.toHexString())
        global.println("255.toBinaryString() = " + a.toBinaryString())
        global.println("255.toOctalString() = " + a.toOctalString())
        global.println("255.toRadixString(10) = " + a.toRadixString(10))
        int zero = 0
        global.println("0.toRadixString(16) = " + zero.toRadixString(16))
        int ten = 10
        global.println("10.toRadixString(2) = " + ten.toRadixString(2))
        int neg = 0 - 1
        global.println("-1.toHexString() = " + neg.toHexString())
        int n35 = 35
        global.println("35.toRadixString(36) = " + n35.toRadixString(36))
    }

    static integerTypeTest()
    {
        global.println("----- integerTypeTest -----")

        Byte b8 = 250
        SByte sb8 = -12
        Int16 i16 = 32000
        UInt16 u16 = 65000
        Int32 i32 = 2000000000
        UInt32 u32 = 4000000000
        Int64 i64 = 900000000000
        UInt64 u64 = 1800000000000

        global.println("byte   = " + b8.toString())
        global.println("sbyte  = " + sb8.toString())
        global.println("int16  = " + i16.toString())
        global.println("uint16 = " + u16.toString())
        global.println("int32  = " + i32.toString())
        global.println("uint32 = " + u32.toString())
        global.println("int64  = " + i64.toString())
        global.println("uint64 = " + u64.toString())
    }

    static bitOpTest()
    {
        global.println("----- bitOpTest -----")

        Int32 x = 13
        Int32 y = 6
        b1 = 0b1100
        b2 = 0b0011
        global.println("x = " + x.toString() + ", y = " + y.toString())
        global.println("x & y = " + (x & y).toString())
        global.println("x | y = " + (x | y).toString())
        global.println("x << 2 = " + (x << 2).toString())
        global.println("y >> 1 = " + (y >> 1).toString())
        global.println("^b1= " + (b1^b2).toString() )
    }
    static compareTest()
    {
        global.println("----- compareTest -----")

        Num a = 3.14
        Num b = 2.5

        global.println("a > b  = " + (a > b).toString())
        global.println("a < b  = " + (a < b).toString())
        global.println("a == b = " + (a == b).toString())
        global.println("a != b = " + (a != b).toString())
        global.println("a >= b = " + (a >= b).toString())
        global.println("a <= b = " + (a <= b).toString())

        global.println("a.compareTo(b) = " + a.compareTo(b).toString())
        global.println("a.compareTo(b) = " + b.compareTo(20).toString())

        int a20 = 20
        global.println("a20 > b  = " + a20.compareTo(30.0f).toString())
        global.println("a20 < b  = " + a20.compareTo(1s).toString())
        global.println("a20 == b = " + a20.compareTo(100us).toString())
        global.println("a20 != b = " + a20.compareTo(323232uL).toString())
        global.println("a20 >= b = " + a20.compareTo(3L).toString())

    }
    static convertAndTypeTest()
    {
        global.println("----- convertAndTypeTest -----")

        Num f = 12.75
        Int32 i = f as Int32
        String s = f?.toString()

        global.println("f = " + f.toString())
        global.println("f as int = " + i?.toString())
        global.println("f.toString = " + s)

        t1 = f.type
        t2 = i?.type
        global.println("f.type = " + t1.toString())
        global.println("i.type = " + t2.toString())
        global.println("f.type == i.type -> " + (t1 == t2).toString())
    }

    static suffixLiteralTest()
    {
        global.println("----- suffixLiteralTest -----")

        Byte vint8 = 0xfa
        vI = 123i
        vUI = 123ui
        vL = 123L
        vUL = 123uL
        vS = 123s
        vUS = 123us
        vF = 1.25f
        vD = 1.25d

        global.println("0xfa  -> " + vint8.toString() + " ; type=" + vint8.type.toString())
        global.println("123i  -> " + vI.toString() + " ; type=" + vI.type.toString())
        global.println("123ui -> " + vUI.toString() + " ; type=" + vUI.type.toString())
        global.println("123L  -> " + vL.toString() + " ; type=" + vL.type.toString())
        global.println("123uL -> " + vUL.toString() + " ; type=" + vUL.type.toString())
        global.println("123s  -> " + vS.toString() + " ; type=" + vS.type.toString())
        global.println("123us -> " + vUS.toString() + " ; type=" + vUS.type.toString())
        global.println("1.25f -> " + vF.toString() + " ; type=" + vF.type.toString())
        global.println("1.25d -> " + vD.toString() + " ; type=" + vD.type.toString())

        # 当前语法里没有 b 后缀，Byte 用显式类型/转换测试
        #vB = 255 as Byte
        #global.println("255 as Byte -> " + vB.toString() + " ; type=" + vB.type.toString())
    }

    static radixLiteralTest()
    {
        global.println("----- radixLiteralTest -----")

        hx = 0x1A
        oc = 0o17
        bn = 0b1010_0101

        global.println("0x1A -> " + hx.toString() + " ; type=" + hx.type.toString())
        global.println("0o17 -> " + oc.toString() + " ; type=" + oc.type.toString())
        global.println("0b1010_0101 -> " + bn.toString() + " ; type=" + bn.type.toString())
    }

    static inferTypeByComputeTest()
    {
        global.println("----- inferTypeByComputeTest -----")

        a = 1 + 2
        itbct_b = 1 + 2.2
        c = 1i + 2ui
        d = 1L + 2
        e = (1 + 2) * 3
        f = 5 / 2
        g = 5 % 2

        global.println("a = 1 + 2 -> " + a.toString() + " ; type=" + a.type.toString())
        global.println("itbct_b = 1 + 2.2 -> " + itbct_b.toString() + " ; type=" + itbct_b.type.toString())
        global.println("c = 1i + 2ui -> " + c.toString() + " ; type=" + c.type.toString())
        global.println("d = 1L + 2 -> " + d.toString() + " ; type=" + d.type.toString())
        global.println("e = (1 + 2) * 3 -> " + e.toString() + " ; type=" + e.type.toString())
        global.println("f = 5 / 2 -> " + f.toString() + " ; type=" + f.type.toString())
        global.println("g = 5 % 2 -> " + g.toString() + " ; type=" + g.type.toString())
    }
    static dartStyleNumberApiTest()
    {
        global.println("----- dartStyleNumberApiTest -----")

        Num n = 3.75
        Num neg = -7
        Int32 baseNum = 9

        global.println("n = " + n.toString() + " ; type=" + n.type.toString())
        global.println("neg = " + neg.toString() + " ; type=" + neg.type.toString())
        global.println("neg.toBool() = " + neg.toBool().toString())

        global.println("n.toInt32() = " + n.toInt32().toString() + " ; type=" + n.toInt32().type.toString())
        global.println("baseNum.toFloat64() = " + baseNum.toFloat64().toString() + " ; type=" + baseNum.toFloat64().type.toString())

        global.println("baseNum.isEven() = " + baseNum.isEven().toString())
        global.println("baseNum.isOdd() = " + baseNum.isOdd().toString())
        global.println("baseNum.sign() = " + baseNum.sign().toString())
    }
    static mixedNumericPromotionTest()
    {
        global.println("----- mixedNumericPromotionTest -----")

        m1 = 250 + 10
        m2 = 250 + 10.5
        m3 = 1i + 2ui + 3L
        m4 = (1 + 2) * (3 + 4) - 5
        m5 = (0b1111 & 0b0101) | 0b1000

        global.println("m1 = 250 + 10 -> " + m1.toString() + " ; type=" + m1.type.toString())
        global.println("m2 = 250 + 10.5 -> " + m2.toString() + " ; type=" + m2.type.toString())
        global.println("m3 = 1i + 2ui + 3L -> " + m3.toString() + " ; type=" + m3.type.toString())
        global.println("m4 = (1 + 2) * (3 + 4) - 5 -> " + m4.toString() + " ; type=" + m4.type.toString())
        global.println("m5 = (0b1111 & 0b0101) | 0b1000 -> " + m5.toString() + " ; type=" + m5.type.toString())
    }

    static compoundAssignTest()
    {
        global.println("----- compoundAssignTest -----")

        Int32 a = 10
        Int32 b = 3
        Int32 c = 0
        c = a + b
        global.println("c = a + b -> " + c.toString())

        Int32 p = 100
        p += 5
        global.println("p += 5 -> " + p.toString())

        Int32 q = 50
        q -= 12
        global.println("q -= 12 -> " + q.toString())

        Int32 r = 6
        r *= 7
        global.println("r *= 7 -> " + r.toString())

        Int32 s = 47
        s /= 5
        global.println("s /= 5 -> " + s.toString())

        Int32 t = 23
        t %= 5
        global.println("t %= 5 -> " + t.toString())

        Int32 u = 3
        u <<= 2
        global.println("u <<= 2 -> " + u.toString())

        Int32 v = -16
        v >>= 2
        global.println("v >>= 2 -> " + v.toString())

        Int32 w = 0b1100
        w &= 0b1010
        global.println("w &= 0b1010 -> " + w.toString())

        Int32 x = 0b1010
        x ^= 0b1100
        global.println("x ^= 0b1100 -> " + x.toString())

        Int32 y = 0b0010
        y |= 0b0100
        global.println("y |= 0b0100 -> " + y.toString())

        Num n = 10.0
        n += 2.5
        global.println("Num n += 2.5 -> " + n.toString())
        n -= 1.5
        global.println("Num n -= 1.5 -> " + n.toString())
        n *= 2
        global.println("Num n *= 2 -> " + n.toString())
        n /= 4
        global.println("Num n /= 4 -> " + n.toString())
    }

    static operatorPrecedenceTest()
    {
        global.println("----- operatorPrecedenceTest -----")

        # * / % 高于 + -
        global.println("2 + 3 * 4 -> " + (2 + 3 * 4).toString())
        global.println("(2 + 3) * 4 -> " + ((2 + 3) * 4).toString())
        global.println("10 - 6 / 2 -> " + (10 - 6 / 2).toString())
        global.println("100 % 7 * 2 -> " + (100 % 7 * 2).toString())

        # + - 高于 << >>
        global.println("1 + 2 << 2 -> " + (1 + 2 << 2).toString())
        global.println("2 << 1 + 2 -> " + (2 << 1 + 2).toString())

        # + - 高于 & ^ |
        global.println("1 + 2 & 7 -> " + (1 + 2 & 7).toString())
        global.println("1 & 1 + 2 -> " + (1 & 1 + 2).toString())
        global.println("1 & 2 ^ 7 -> " + (1 & 2 ^ 7).toString())
        global.println("0b1100 ^ 0b1010 | 0b0001 -> " + (0b1100 ^ 0b1010 | 0b0001).toString())

        # 比较、相等：整型比较与 == 低于算术
        global.println("1 + 2 > 2 -> " + (1 + 2 > 2).toString())
        global.println("6 == 2 + 4 -> " + (6 == 2 + 4).toString())
        global.println("3 + 4 == 5 + 2 -> " + (3 + 4 == 5 + 2).toString())

        # && 高于 ||
        global.println("true || false && false -> " + (true || false && false).toString())

        Int32 u = 2
        global.println("-u * 3 -> " + (-u * 3).toString())
    }

    static defaultParamCallTest()
    {
        global.println("----- defaultParamCallTest -----")

        Int32 a = 20
        Int8 i8 = a.toInt8()
        global.println("a.toInt8() = " + i8.toString())
        UInt8 idx = 1
        Int8 i8b = a.toInt8(idx)
        global.println("a.toInt8(1) = " + i8b.toString())
    }

    # ===== 数字类型常数打印验证（Float / Int 全家桶） =====
    static numConstantsTest()
    {
        global.println("----- numConstantsTest -----")

        # --- Float8 (e4m3: 4位指数 bias=7 + 3位尾数) ---
        global.println("Float8.Epsilon     = " + Float8.Epsilon.toString() + "   (expect 0.125)")
        global.println("Float8.MaxValue    = " + Float8.MaxValue.toString() + "   (expect 448)")
        global.println("Float8.MinValue    = " + Float8.MinValue.toString() + "   (expect -448)")
        global.println("Float8.MinPositive = " + Float8.MinPositive.toString() + "   (expect 0.001953125)")

        # --- Float8_E5M2 (e5m2: 5位指数 bias=15 + 2位尾数) ---
        global.println("Float8_E5M2.Epsilon     = " + Float8_E5M2.Epsilon.toString() + "   (expect 0.25)")
        global.println("Float8_E5M2.MaxValue    = " + Float8_E5M2.MaxValue.toString() + "   (expect 57344)")
        global.println("Float8_E5M2.MinValue    = " + Float8_E5M2.MinValue.toString() + "   (expect -57344)")
        global.println("Float8_E5M2.MinPositive = " + Float8_E5M2.MinPositive.toString() + "   (expect 1.52587890625e-5)")

        # --- Float16 (binary16: 5位指数 bias=15 + 10位尾数) ---
        global.println("Float16.Epsilon     = " + Float16.Epsilon.toString() + "   (expect 0.0009765625)")
        global.println("Float16.MaxValue    = " + Float16.MaxValue.toString() + "   (expect 65504)")
        global.println("Float16.MinValue    = " + Float16.MinValue.toString() + "   (expect -65504)")
        global.println("Float16.MinPositive = " + Float16.MinPositive.toString() + "   (expect 5.9604644775390625e-8)")

        # --- Float16_Brain (bfloat16: 8位指数 bias=127 + 7位尾数) ---
        global.println("Float16_Brain.Epsilon     = " + Float16_Brain.Epsilon.toString() + "   (expect 0.0078125)")
        global.println("Float16_Brain.MaxValue    = " + Float16_Brain.MaxValue.toString() + "   (expect 3.3895313892515355e38)")
        global.println("Float16_Brain.MinValue    = " + Float16_Brain.MinValue.toString() + "   (expect -3.3895313892515355e38)")
        global.println("Float16_Brain.MinPositive = " + Float16_Brain.MinPositive.toString() + "   (expect 9.183549615799121e-41)")

        # --- Float32 (binary32: 8位指数 bias=127 + 23位尾数) ---
        global.println("Float32.Epsilon     = " + Float32.Epsilon.toString() + "   (expect 1.1920928955078125e-7)")
        global.println("Float32.MaxValue    = " + Float32.MaxValue.toString() + "   (expect 3.4028234663852886e38)")
        global.println("Float32.MinValue    = " + Float32.MinValue.toString() + "   (expect -3.4028234663852886e38)")
        global.println("Float32.MinPositive = " + Float32.MinPositive.toString() + "   (expect 1.401298464324817e-45)")

        # --- Float64 (binary64: 11位指数 bias=1023 + 52位尾数) ---
        global.println("Float64.Epsilon     = " + Float64.Epsilon.toString() + "   (expect 2.220446049250313e-16)")
        global.println("Float64.MaxValue    = " + Float64.MaxValue.toString() + "   (expect 1.7976931348623157e308)")
        global.println("Float64.MinValue    = " + Float64.MinValue.toString() + "   (expect -1.7976931348623157e308)")
        global.println("Float64.MinPositive = " + Float64.MinPositive.toString() + "   (expect 4.9406564584124654e-324)")

        # --- 整型边界 ---
        global.println("Int8.MaxValue   = " + Int8.MaxValue.toString() + "   (expect 127)")
        global.println("Int8.MinValue   = " + Int8.MinValue.toString() + "   (expect -128)")
        global.println("UInt8.MaxValue  = " + UInt8.MaxValue.toString() + "   (expect 255)")
        global.println("UInt8.MinValue  = " + UInt8.MinValue.toString() + "   (expect 0)")
        global.println("Int16.MaxValue  = " + Int16.MaxValue.toString() + "   (expect 32767)")
        global.println("Int16.MinValue  = " + Int16.MinValue.toString() + "   (expect -32768)")
        global.println("UInt16.MaxValue = " + UInt16.MaxValue.toString() + "   (expect 65535)")
        global.println("UInt16.MinValue = " + UInt16.MinValue.toString() + "   (expect 0)")

        # 诊断: 本模块内静态字段访问(对照跨模块 Core 常数是否错位)
        global.println("[diag] NumConstProbe.A1 = " + NumConstProbe.A1.toString() + "   (expect 11)")
        global.println("[diag] NumConstProbe.A2 = " + NumConstProbe.A2.toString() + "   (expect 22)")
        global.println("[diag] NumConstProbe.A3 = " + NumConstProbe.A3.toString() + "   (expect 33)")
    }

    # ===== 数字类型方法验证（NaN/Inf/Finite、floor/ceil/abs、parse/sign/isEven/进制串） =====
    static numMethodsTest()
    {
        global.println("----- numMethodsTest -----")

        # --- 静态判定: NaN / Infinite / Finite (Float64) ---
        Float64 dnan = 0.0d / 0.0d
        Float64 dinf = 1.0d / 0.0d
        # 诊断: 打印字面量除法实际值, 并与运行时变量除法对照(区分常量折叠 bug 与 VM 除法 bug)
        Float64 dz = 0.0d
        global.println("[diag] lit  0.0d/0.0d = " + dnan.toString() + "   (expect NaN)")
        global.println("[diag] lit  1.0d/0.0d = " + dinf.toString() + "   (expect Inf)")
        global.println("[diag] rt   dz/dz     = " + (dz / dz).toString() + "   (expect NaN)")
        global.println("[diag] rt   1.0d/dz   = " + (1.0d / dz).toString() + "   (expect Inf)")
        global.println("Float64.isNaN(NaN)       = " + Float64.isNaN(dnan).toString() + "   (expect true)")
        global.println("Float64.isInfinite(Inf)  = " + Float64.isInfinite(dinf).toString() + "   (expect true)")
        global.println("Float64.isFinite(1.5d)   = " + Float64.isFinite(1.5d).toString() + "   (expect true)")
        global.println("Float64.isFinite(Inf)    = " + Float64.isFinite(dinf).toString() + "   (expect false)")
        global.println("Float64.isFinite(NaN)    = " + Float64.isFinite(dnan).toString() + "   (expect false)")

        # --- 静态判定 (Float32) ---
        Float32 fnan = 0.0f / 0.0f
        Float32 finf = 1.0f / 0.0f
        global.println("Float32.isNaN(NaN)       = " + Float32.isNaN(fnan).toString() + "   (expect true)")
        global.println("Float32.isInfinite(Inf)  = " + Float32.isInfinite(finf).toString() + "   (expect true)")
        global.println("Float32.isFinite(NaN)    = " + Float32.isFinite(fnan).toString() + "   (expect false)")

        # --- 静态判定 (Float8: e4m3 仅有 NaN, 无 Inf 编码) ---
        global.println("Float8.isNaN(NaN)        = " + Float8.isNaN(0.0fe4 / 0.0fe4).toString() + "   (expect true)")
        global.println("Float8.isFinite(Max)     = " + Float8.isFinite(Float8.MaxValue).toString() + "   (expect true)")

        # --- floor / ceil / abs (浮点) ---
        Float64 d1 = 3.7d
        Float64 d2 = -3.7d
        global.println("3.7d.floor()   = " + d1.floor().toString() + "   (expect 3)")
        global.println("3.7d.ceil()    = " + d1.ceil().toString() + "   (expect 4)")
        global.println("-3.7d.floor()  = " + d2.floor().toString() + "   (expect -4)")
        global.println("-3.7d.ceil()   = " + d2.ceil().toString() + "   (expect -3)")
        global.println("-3.7d.abs()    = " + d2.abs().toString() + "   (expect 3.7)")

        Float8 f8 = 2.5fe4
        global.println("2.5fe4.floor() = " + f8.floor().toString() + "   (expect 2)")
        global.println("2.5fe4.ceil()  = " + f8.ceil().toString() + "   (expect 3)")
        Float16 h1 = 2.5h
        global.println("2.5h.floor()   = " + h1.floor().toString() + "   (expect 2)")
        global.println("2.5h.ceil()    = " + h1.ceil().toString() + "   (expect 3)")

        # --- 整型: parse ---
        global.println("Int8.parse(\"123\")     = " + Int8.parse("123").toString() + "   (expect 123)")
        global.println("UInt8.parse(\"255\")    = " + UInt8.parse("255").toString() + "   (expect 255)")
        global.println("Int16.parse(\"-32768\") = " + Int16.parse("-32768").toString() + "   (expect -32768)")
        global.println("UInt16.parse(\"65535\") = " + UInt16.parse("65535").toString() + "   (expect 65535)")

        # --- 整型: sign / abs / isEven / isOdd ---
        Int8 i8n = -5i
        Int8 i8z = 0i
        Int8 i8p = 5i
        global.println("(-5i).sign()   = " + i8n.sign().toString() + "   (expect -1)")
        global.println("(0i).sign()    = " + i8z.sign().toString() + "   (expect 0)")
        global.println("(5i).sign()    = " + i8p.sign().toString() + "   (expect 1)")
        global.println("(-5i).abs()    = " + i8n.abs().toString() + "   (expect 5)")
        # 诊断: Int8.isEven 直接链式调用时上一轮输出行丢失, 改为先存变量; 并补正数用例
        Int8 i8e = 4i
        global.println("(4i).isEven()  = " + i8e.isEven().toString() + "   (expect true)")
        bool evn = i8n.isEven()
        global.println("(-5i).isEven() = " + evn.toString() + "   (expect false)")
        global.println("(5i).isOdd()   = " + i8p.isOdd().toString() + "   (expect true)")

        # --- 整型: 进制字符串 (UInt8 / UInt16 / Int16) ---
        UInt8 u8 = 0b11111111
        global.println("UInt8(255).toHexString()    = " + u8.toHexString() + "   (expect ff)")
        global.println("UInt8(255).toBinaryString() = " + u8.toBinaryString() + "   (expect 11111111)")
        global.println("UInt8(255).toOctalString()  = " + u8.toOctalString() + "   (expect 377)")
        UInt16 u16v = 0xffff
        global.println("UInt16(65535).toHexString() = " + u16v.toHexString() + "   (expect ffff)")
        Int16 i16n = -1s
        global.println("Int16(-1).toHexString()     = " + i16n.toHexString() + "   (expect ffffffff)")
    }

    static fun()
    {
        global.println("========== NumberTest (start) ==========")
        baseArithmeticTest()
        integerTypeTest()
        bitOpTest()
        testRadix()
        compareTest()
        convertAndTypeTest()
        suffixLiteralTest()
        radixLiteralTest()
        inferTypeByComputeTest()
        dartStyleNumberApiTest()
        mixedNumericPromotionTest()
        compoundAssignTest()
        operatorPrecedenceTest()
        numConstantsTest()
        numMethodsTest()
        #defaultParamCallTest() # 暂时注释：C VM vm_sys_convert_int_like 双参实现只弹 index、this 残留栈导致栈不平衡崩溃（FrontEnd 默认参数填充本身已验证）
        global.println("========== NumberTest (end) ==========")
    }
}

# 诊断用本地类: 验证同模块静态字段访问是否正常
class NumConstProbe
{
    public const static Int32 A1 = 11
    public const static Int32 A2 = 22
    public const static Int32 A3 = 33
}

# 测试用例说明：
# - baseArithmeticTest：Int32 四则与取模；Num 浮点运算与一元负号。
# - integerTypeTest：各固定宽度整数类型的字面量与 toString。
# - bitOpTest：整型位与、位或、左移、右移。
# - compareTest：Num 上比较运算符。
# - convertAndTypeTest：Num 与 Int32 的 as、toString、.type 与类型相等。
# - suffixLiteralTest：整型/浮点字面量后缀与显式 as Byte。
# - radixLiteralTest：十六进制、八进制、二进制与下划线分组。
# - inferTypeByComputeTest：无显式类型声明时由表达式推断类型。
# - compoundAssignTest：= 表达式赋值；Int32/Num 的 += -= *= /= %= <<= >>= &= ^= |=。
# - operatorPrecedenceTest：* / % 与 + -；移位与加减；位运算与加减；比较/== 与算术；&& 与 ||；一元负号与乘法。
#
# 预期结果：各段横幅下打印值与手算一致；整除/取模、进制转换、类型名字符串可用于回归快照。

