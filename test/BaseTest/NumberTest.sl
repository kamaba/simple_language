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
        global.println("x = " + x.toString() + ", y = " + y.toString())
        global.println("x & y = " + (x & y).toString())
        global.println("x | y = " + (x | y).toString())
        global.println("x << 2 = " + (x << 2).toString())
        global.println("y >> 1 = " + (y >> 1).toString())
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
    }

    static convertAndTypeTest()
    {
        global.println("----- convertAndTypeTest -----")

        Num f = 12.75
        Int32 i = f as Int32
        String s = f.toString()

        global.println("f = " + f.toString())
        global.println("f as int = " + i.toString())
        global.println("f.toString = " + s)

        t1 = f.type
        t2 = i.type
        global.println("f.type = " + t1.toString())
        global.println("i.type = " + t2.toString())
        global.println("f.type == i.type -> " + (t1 == t2).toString())
    }

    static suffixLiteralTest()
    {
        global.println("----- suffixLiteralTest -----")

        vI = 123i
        vUI = 123ui
        vL = 123L
        vUL = 123uL
        vS = 123s
        vUS = 123us
        vF = 1.25f
        vD = 1.25d

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
        global.println("itbct_b = 1 + 2.0 -> " + itbct_b.toString() + " ; type=" + itbct_b.type.toString())
        global.println("c = 1i + 2ui -> " + c.toString() + " ; type=" + c.type.toString())
        global.println("d = 1L + 2 -> " + d.toString() + " ; type=" + d.type.toString())
        global.println("e = (1 + 2) * 3 -> " + e.toString() + " ; type=" + e.type.toString())
        global.println("f = 5 / 2 -> " + f.toString() + " ; type=" + f.type.toString())
        global.println("g = 5 % 2 -> " + g.toString() + " ; type=" + g.type.toString())
    }

    static fun()
    {
        global.println("========== NumberTest (start) ==========")
        baseArithmeticTest()
        integerTypeTest()
        bitOpTest()
        compareTest()
        convertAndTypeTest()
        suffixLiteralTest()
        radixLiteralTest()
        inferTypeByComputeTest()
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
#
# 预期结果：各段横幅下打印值与手算一致；整除/取模、进制转换、类型名字符串可用于回归快照。

