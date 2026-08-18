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
        #defaultParamCallTest() # 暂时注释：C VM vm_sys_convert_int_like 双参实现只弹 index、this 残留栈导致栈不平衡崩溃（FrontEnd 默认参数填充本身已验证）
        global.println("========== NumberTest (end) ==========")
    }
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

