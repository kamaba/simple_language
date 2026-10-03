import Std;
import Math;

# ============================================================
# MatrixBigTest - Matrix / BigNumber / BigDecimal / Factor 测试
# 验证 system method call 优化路径：
#   Matrix     : SystemArrayFillValue / SystemArrayCopy /
#                SystemArrayAdd/Sub/Mul/EqualsFloat32 /
#                SystemMathMatMul/TransposeFloat32 / SystemArrayToString
#   BigNumber  : SystemStringCharCodeAt（parse 逐字符）、divMod 绝对值路径
#   BigDecimal : SystemStringCharCodeAt（_charCodeAt）、roundTo/rescale
#   Factor     : _slice 经 SystemArrayCopy 的截断拷贝
# 断言全部选取整数与 2 的幂等浮点精确可表示值，规避舍入误差。
# ============================================================
class MatrixBigTest
{
    static check( string name, bool cond )
    {
        if ( cond )
        {
            Console.println( "  [PASS] " + name )
        }
        else
        {
            Console.println( "  [FAIL] " + name )
        }
    }

    # ── Matrix（通用行主序矩阵）────────────────────────────
    static testMatrix()
    {
        Console.println( "===== MatrixBigTest.testMatrix =====" )

        # 零矩阵构造（SystemArrayFillValue 批量清零）
        Matrix z = Matrix( 2, 3 )
        check( "ctor rows", z.rows == 2 )
        check( "ctor cols", z.cols == 3 )
        check( "ctor count", z.count() == 6 )
        check( "ctor not square", z.isSquare() == false )
        check( "fill zero [0]", z[0] == 0.0f )
        check( "fill zero [5]", z[5] == 0.0f )
        check( "fill zero getValue(1,2)", z.getValue( 1, 2 ) == 0.0f )

        # 值构造（SystemArrayCopy 系统级拷贝，生成新数组）
        Array<Float32> vals = Array<Float32>( 6 )
        vals[0] = 1.0f
        vals[1] = 2.0f
        vals[2] = 3.0f
        vals[3] = 4.0f
        vals[4] = 5.0f
        vals[5] = 6.0f
        Matrix a = Matrix( 2, 3, vals )
        check( "values ctor [0]", a[0] == 1.0f )
        check( "values ctor [5]", a[5] == 6.0f )
        check( "values ctor getValue(1,0)", a.getValue( 1, 0 ) == 4.0f )

        # 拷贝语义：修改源数组不影响矩阵
        vals[0] = 99.0f
        check( "copy independent", a[0] == 1.0f )
        vals[0] = 1.0f

        # setValue / _setItem_
        a.setValue( 0, 1, 20.0f )
        check( "setValue(0,1)", a.getValue( 0, 1 ) == 20.0f )
        a[0] = 10.0f
        check( "setItem [0]", a[0] == 10.0f )
        a.setValue( 0, 0, 1.0f )
        a.setValue( 0, 1, 2.0f )

        # 全 1 矩阵（经 _setItem_ 逐个写入）
        Matrix onesM = Matrix( 2, 3 )
        int f = 0
        while f < 6
        {
            onesM[f] = 1.0f
            f = f + 1
        }

        # 运算符 + / -（SystemArrayAddFloat32 / SystemArraySubFloat32）
        Matrix sumM = a + onesM
        check( "add [0]", sumM[0] == 2.0f )
        check( "add [5]", sumM[5] == 7.0f )
        Matrix subM = a - onesM
        check( "sub [0]", subM[0] == 0.0f )
        check( "sub [5]", subM[5] == 5.0f )

        # 标量乘（SystemArrayMulFloat32）
        Matrix scaleM = a * 2.0f
        check( "scalar mul [0]", scaleM[0] == 2.0f )
        check( "scalar mul [5]", scaleM[5] == 12.0f )

        # 矩阵乘（SystemMathMatMulFloat32）：A(2x3) * B(3x2)
        Matrix bm = Matrix( 3, 2 )
        bm.setValue( 0, 0, 7.0f )
        bm.setValue( 0, 1, 8.0f )
        bm.setValue( 1, 0, 9.0f )
        bm.setValue( 1, 1, 10.0f )
        bm.setValue( 2, 0, 11.0f )
        bm.setValue( 2, 1, 12.0f )
        Matrix cm = a * bm
        check( "matmul rows", cm.rows == 2 )
        check( "matmul cols", cm.cols == 2 )
        check( "matmul (0,0) == 58", cm.getValue( 0, 0 ) == 58.0f )
        check( "matmul (0,1) == 64", cm.getValue( 0, 1 ) == 64.0f )
        check( "matmul (1,0) == 139", cm.getValue( 1, 0 ) == 139.0f )
        check( "matmul (1,1) == 154", cm.getValue( 1, 1 ) == 154.0f )

        # 单位矩阵乘（SystemArrayEqualsFloat32 精确比较）
        Matrix idm = Matrix.identity( 3 )
        Matrix aid = a * idm
        check( "identity multiply", aid == a )

        # 转置（SystemMathTransposeFloat32）
        Matrix at = a.transpose()
        check( "transpose rows", at.rows == 3 )
        check( "transpose cols", at.cols == 2 )
        check( "transpose (0,1) == 4", at.getValue( 0, 1 ) == 4.0f )
        check( "transpose (2,0) == 3", at.getValue( 2, 0 ) == 3.0f )
        Matrix att = at.transpose()
        check( "transpose twice", att == a )

        # clone（SystemArrayCopy 深拷贝）
        Matrix ac = a.clone()
        check( "clone equal", ac == a )
        check( "clone differs", ac != onesM )
        ac.setValue( 0, 0, 100.0f )
        check( "clone deep copy", a.getValue( 0, 0 ) == 1.0f )

        # 行列式（递归展开 + minorMatrix）
        Matrix d2 = Matrix( 2, 2 )
        d2.setValue( 0, 0, 1.0f )
        d2.setValue( 0, 1, 2.0f )
        d2.setValue( 1, 0, 3.0f )
        d2.setValue( 1, 1, 4.0f )
        check( "det 2x2 == -2", d2.determinant() == -2.0f )

        Matrix d3 = Matrix( 3, 3 )
        d3.setValue( 0, 0, 1.0f )
        d3.setValue( 0, 1, 2.0f )
        d3.setValue( 0, 2, 3.0f )
        d3.setValue( 1, 0, 4.0f )
        d3.setValue( 1, 1, 5.0f )
        d3.setValue( 1, 2, 6.0f )
        d3.setValue( 2, 0, 7.0f )
        d3.setValue( 2, 1, 8.0f )
        d3.setValue( 2, 2, 10.0f )
        check( "det 3x3 == -3", d3.determinant() == -3.0f )
        check( "identity det == 1", idm.determinant() == 1.0f )
        check( "isInvertible", d3.isInvertible() )
        check( "non-square det == 0", z.determinant() == 0.0f )

        Matrix sing = Matrix( 2, 2 )
        sing.setValue( 0, 0, 2.0f )
        sing.setValue( 0, 1, 4.0f )
        sing.setValue( 1, 0, 1.0f )
        sing.setValue( 1, 1, 2.0f )
        check( "singular not invertible", sing.isInvertible() == false )

        # 零矩阵工厂
        Matrix zf = Matrix.zero( 2, 2 )
        check( "zero factory [0]", zf[0] == 0.0f )
        check( "zero factory det", zf.determinant() == 0.0f )

        Console.println( "  toString: " + a.toString() )
    }

    # ── BigNumber（大整数）─────────────────────────────────
    static testBigNumber()
    {
        Console.println( "===== MatrixBigTest.testBigNumber =====" )

        # 构造 / toString（_pad4 跨段补零路径）
        BigNumber a = BigNumber( 125 )
        check( "ctor toString", a.toString() == "125" )
        BigNumber big = BigNumber( 1234567 )
        check( "ctor multi-digit", big.toString() == "1234567" )
        BigNumber neg = BigNumber( -42 )
        check( "ctor negative", neg.toString() == "-42" )
        BigNumber z = BigNumber()
        check( "ctor zero", z.toString() == "0" )

        # parse（SystemStringCharCodeAt 逐位解析）
        BigNumber p = BigNumber.parse( "987654321" )
        check( "parse", p.toString() == "987654321" )
        BigNumber pn = BigNumber.parse( "-500" )
        check( "parse negative", pn.toString() == "-500" )
        BigNumber sctor = BigNumber( "31415" )
        check( "string ctor", sctor.toString() == "31415" )

        # 四则（含异号路径）
        BigNumber b = BigNumber( 100 )
        check( "add", a.add( b ).toString() == "225" )
        check( "sub", a.sub( b ).toString() == "25" )
        check( "multiply", a.multiply( b ).toString() == "12500" )
        check( "div", a.div( b ).toString() == "1" )
        check( "mod", a.mod( b ).toString() == "25" )
        check( "negate", neg.negate().toString() == "42" )
        check( "add negative", a.add( neg ).toString() == "83" )
        check( "sub negative", a.sub( neg ).toString() == "167" )
        check( "multiply negative", a.multiply( neg ).toString() == "-5250" )

        # divMod（长除法二分试商，abs() 经 as 转型落地为 BigNumber）
        Array<BigNumber> dm = a.divMod( b )
        check( "divMod quotient", dm[0].toString() == "1" )
        check( "divMod remainder", dm[1].toString() == "25" )

        # 跨段进位 / 借位（9999+1 / 10000-1）
        BigNumber n9999 = BigNumber.parse( "9999" )
        BigNumber n1 = BigNumber( 1 )
        check( "carry 9999+1", n9999.add( n1 ).toString() == "10000" )
        BigNumber n10000 = BigNumber.parse( "10000" )
        check( "borrow 10000-1", n10000.sub( n1 ).toString() == "9999" )

        # abs / clone
        BigNumber an = neg.abs() as BigNumber
        check( "abs", an.toString() == "42" )
        check( "clone equal", a.clone().toString() == "125" )

        # 比较
        check( "compare gt", a.compare( b ) > 0 )
        check( "compare lt", b.compare( a ) < 0 )
        check( "compare eq", a.compare( a.clone() ) == 0 )

        # 运算符重载（形参为 Object 的 _lt_/_eq_ 系列，经 compareToBoxed）
        # 对照：显式方法调用 vs 运算符（追查字符串参数丢失）
        check( "explicit _lt_", neg._lt_( z ) )
        check( "box cmp", neg.compareToBoxed( z ) < 0 )
        check( "operator <", neg < z )
        check( "operator >", a > b )
        check( "operator <=", b <= b.clone() )
        check( "operator >=", a >= b )
        check( "operator ==", a == a.clone() )
        check( "operator !=", a != b )

        # Num 接口
        check( "toInt32", a.toInt32() == 125 )
        check( "neg toInt32", neg.toInt32() == -42 )

        # 静态工具
        check( "zero", BigNumber.zero.toString() == "0" )
        check( "one", BigNumber.one.toString() == "1" )
        check( "valueOf", BigNumber.valueOf( 7 ).toString() == "7" )
        check( "absValue", BigNumber.absValue( neg ).toString() == "42" )
        check( "maxValue", BigNumber.maxValue( a, b ).toString() == "125" )
        check( "minValue", BigNumber.minValue( a, b ).toString() == "100" )

        # 幂 / 阶乘 / 最大公约数
        check( "pow 10^2", BigNumber.pow( BigNumber( 10 ), 2 ).toString() == "100" )
        check( "pow 2^16", BigNumber.pow( BigNumber( 2 ), 16 ).toString() == "65536" )
        check( "factorial 5", BigNumber.factorial( 5 ).toString() == "120" )
        check( "factorial 10", BigNumber.factorial( 10 ).toString() == "3628800" )
        check( "gcd", BigNumber.gcd( BigNumber( 48 ), BigNumber( 36 ) ).toString() == "12" )

        Console.println( "  toString: " + BigNumber.pow( BigNumber( 2 ), 32 ).toString() )
    }

    # ── BigDecimal（定点小数）──────────────────────────────
    static testBigDecimal()
    {
        Console.println( "===== MatrixBigTest.testBigDecimal =====" )

        # 解析（_charCodeAt 经 SystemStringCharCodeAt 统计小数位）
        BigDecimal a = BigDecimal.parse( "1.25" )
        check( "parse toString", a.toString() == "1.25" )
        BigDecimal b = BigDecimal.parse( "2.50" )

        # 四则
        BigDecimal s = a.add( b )
        check( "add", s.toString() == "3.75" )
        BigDecimal d = b.sub( a )
        check( "sub", d.toString() == "1.25" )
        BigDecimal m = a.multiply( b )
        check( "multiply", m.toString() == "3.1250" )

        BigDecimal ten = BigDecimal.parse( "10.00" )
        BigDecimal four = BigDecimal.parse( "4.00" )
        BigDecimal q = ten.div( four, 0 )
        check( "div extraScale=0", q.toString() == "250.00" )
        BigDecimal q2 = ten.div( four )
        check( "div default extraScale", q2.toString() == "250.0000000000" )

        # roundTo（四舍五入，走 div/mod 路径）
        BigDecimal x25 = BigDecimal.parse( "2.5" )
        BigDecimal rx = x25.roundTo( 0 )
        check( "roundTo 2.5 -> 3", rx.toString() == "3" )
        BigDecimal x24 = BigDecimal.parse( "2.4" )
        BigDecimal ry = x24.roundTo( 0 )
        check( "roundTo 2.4 -> 2", ry.toString() == "2" )
        BigDecimal pi = BigDecimal.parse( "3.14159" )
        BigDecimal r2 = pi.roundTo( 2 )
        check( "roundTo 3.14159 -> 3.14", r2.toString() == "3.14" )

        # rescale（截断）
        BigDecimal rs = BigDecimal.parse( "1.234" )
        BigDecimal rs1 = rs.rescale( 1 )
        check( "rescale 1.234 -> 1.2", rs1.toString() == "1.2" )

        # negate / abs / clone
        BigDecimal n = BigDecimal.parse( "-1.50" )
        BigDecimal nn = n.negate()
        check( "negate", nn.toString() == "1.50" )
        BigDecimal na = n.abs() as BigDecimal
        check( "abs", na.toString() == "1.50" )
        BigDecimal nc = n.clone()
        check( "clone", nc.toString() == "-1.50" )

        # 比较
        check( "compare lt", a.compare( b ) < 0 )
        check( "compare gt", b.compare( a ) > 0 )
        check( "compare eq", a.compare( a.clone() ) == 0 )

        # 运算符重载（同 BigNumber：_add_ 形参为 Object，用显式方法调用）
        BigDecimal opS = a.add( b )
        check( "operator + (add)", opS.toString() == "3.75" )
        check( "operator <", a < b )

        # 静态工具
        BigDecimal mx = BigDecimal.maxValue( a, b )
        check( "maxValue", mx.toString() == "2.50" )
        BigDecimal mn = BigDecimal.minValue( a, b )
        check( "minValue", mn.toString() == "1.25" )

        # 整数形态：scale <= 0 直接输出 unscaled
        BigDecimal intd = BigDecimal( 42, 0 )
        check( "int toString", intd.toString() == "42" )

        Console.println( "  toString: " + pi.toString() )
    }

    # ── Factor（数论工具）──────────────────────────────────
    static testFactor()
    {
        Console.println( "===== MatrixBigTest.testFactor =====" )

        # 素数判断
        check( "isPrime 2", Factor.isPrime( 2 ) )
        check( "isPrime 97", Factor.isPrime( 97 ) )
        check( "isPrime 96", Factor.isPrime( 96 ) == false )
        check( "isPrime 1", Factor.isPrime( 1 ) == false )

        # 质因数分解（_slice 经 SystemArrayCopy 截断拷贝）
        Array<Int32> pf = Factor.primeFactors( 12 )
        check( "primeFactors len", pf.length == 3 )
        check( "primeFactors[0]", pf[0] == 2 )
        check( "primeFactors[1]", pf[1] == 2 )
        check( "primeFactors[2]", pf[2] == 3 )

        Array<Int32> pf1 = Factor.primeFactors( 1 )
        check( "primeFactors(1) empty", pf1.length == 0 )

        Array<Int32> pf2 = Factor.primeFactors( 97 )
        check( "primeFactors(97) len", pf2.length == 1 )
        check( "primeFactors(97)[0]", pf2[0] == 97 )

        Array<Int32> dpf = Factor.distinctPrimeFactors( 360 )
        check( "distinct len", dpf.length == 3 )
        check( "distinct[0]", dpf[0] == 2 )
        check( "distinct[1]", dpf[1] == 3 )
        check( "distinct[2]", dpf[2] == 5 )

        # 约数
        Array<Int32> dv = Factor.divisors( 12 )
        check( "divisors len", dv.length == 6 )
        check( "divisors[0]", dv[0] == 1 )
        check( "divisors[3]", dv[3] == 4 )
        check( "divisors[5]", dv[5] == 12 )
        check( "divisorCount", Factor.divisorCount( 12 ) == 6 )
        check( "divisorSum", Factor.divisorSum( 12 ) == 28 )

        # gcd / lcm / 扩展欧几里得
        check( "gcd", Factor.gcd( 12, 18 ) == 6 )
        check( "lcm", Factor.lcm( 4, 6 ) == 12 )
        Array<Int32> eg = Factor.extendedGcd( 12, 18 )
        check( "extgcd g", eg[0] == 6 )
        Int32 bez = 12 * eg[1] + 18 * eg[2]
        check( "extgcd bezout", bez == 6 )

        # 幂 / 阶乘 / 排列组合
        check( "powInt", Factor.powInt( 2, 10 ) == 1024 )
        BigNumber ff = Factor.factorial( 10 )
        check( "factorial", ff.toString() == "3628800" )
        BigNumber perm = Factor.permutation( 5, 2 )
        check( "permutation", perm.toString() == "20" )
        BigNumber comb = Factor.combination( 10, 3 )
        check( "combination", comb.toString() == "120" )

        # 2 的幂 / 其他
        check( "isPowerOfTwo 64", Factor.isPowerOfTwo( 64 ) )
        check( "isPowerOfTwo 96", Factor.isPowerOfTwo( 96 ) == false )
        check( "nextPowerOfTwo 5", Factor.nextPowerOfTwo( 5 ) == 8 )
        check( "absInt", Factor.absInt( -5 ) == 5 )
        check( "isqrt 17", Factor.isqrt( 17 ) == 4 )
        check( "isqrt 16", Factor.isqrt( 16 ) == 4 )
        check( "eulerPhi 10", Factor.eulerPhi( 10 ) == 4 )
        check( "eulerPhi 36", Factor.eulerPhi( 36 ) == 12 )
    }

    static fun()
    {
        testMatrix()
        testBigNumber()
        testBigDecimal()
        testFactor()
    }
}
