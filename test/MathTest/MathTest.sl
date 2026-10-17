import Std;
import Math;

class MathTest
{
    static fun()
    {
        Console.println("===== Math builtin member function test =====")

        Vector2 v21 = Vector2(1.0f, 2.0f)
        Float32_2 v22 = Float32_2(3.0f, 4.0f)

        # 算术内置方法（_add_/_sub_/_mul_/_truediv_，返回当前类类型）
        Float32_2 vAdd = v21 + v22
        Float32_2 vSub = v21 - v22
        Float32_2 vMul = v21 * v22
        Float32_2 vDiv = v21 / v22
        Console.println("add = " + vAdd.toString())
        Console.println("sub = " + vSub.toString())
        Console.println("mul = " + vMul.toString())
        Console.println("div = " + vDiv.toString())

        # 标量乘法（_mul_ 的 Float32 分支）
        Float32_2 vScale = v21 * 2.0f
        Console.println("scale = " + vScale.toString())

        # 比较内置方法（_eq_/_ne_，返回 bool；_ne_ 内部显式调用 this._eq_）
        bool bEq = v21 == Vector2(1.0f, 2.0f)
        bool bNe = v21 != v22
        Console.println("v21 == Vector2(1,2) : " + bEq.toString())
        Console.println("v21 != v22 : " + bNe.toString())

        # 索引内置方法（_getItem_/_setItem_）
        Float32 x0 = v21[0]
        v21[1] = 9.0f
        Console.println("v21[0] = " + x0.toString())
        Console.println("v21[1] = " + v21[1].toString())

        # 普通成员方法
        Float32 d = v21.dot(v22)
        Console.println("dot = " + d.toString())
        Console.println("length = " + v22.length().toString())

        # ── cvm 扩展 DLL（MathVMLib.dll）system method call 测试 ──
        # 底层经 Math.jsonc 的 systemCalls 声明，由 cvm 通过
        # "MathVMLib.dll!mathvm_xxx" 解析到 MathVMLib.dll 的导出函数。
        Console.println("===== MathVMLib system method call test =====")

        Float32 fSin = Mathf.sin( 1.0f )
        Float32 fSqrt = Mathf.sqrt( 2.0f )
        Float32 fPow = Mathf.pow( 2.0f, 10.0f )
        Float32 fAtan2 = Mathf.atan2( 1.0f, 1.0f )
        Int32 fTrunc = Mathf.truncate( 3.7f )
        Console.println("Mathf.sin(1) = " + fSin.toString())
        Console.println("Mathf.sqrt(2) = " + fSqrt.toString())
        Console.println("Mathf.pow(2,10) = " + fPow.toString())
        Console.println("Mathf.atan2(1,1) = " + fAtan2.toString())
        Console.println("Mathf.truncate(3.7) = " + fTrunc.toString())

        Float64 dSin = Mathd.sin( 1.0 )
        Float64 dSqrt = Mathd.sqrt( 2.0 )
        Float64 dPow = Mathd.pow( 2.0, 10.0 )
        Float64 dAtan2 = Mathd.atan2( 1.0, 1.0 )
        Int32 dTrunc = Mathd.truncate( 0 - 3.7 )
        Console.println("Mathd.sin(1) = " + dSin.toString())
        Console.println("Mathd.sqrt(2) = " + dSqrt.toString())
        Console.println("Mathd.pow(2,10) = " + dPow.toString())
        Console.println("Mathd.atan2(1,1) = " + dAtan2.toString())
        Console.println("Mathd.truncate(-3.7) = " + dTrunc.toString())

        # ── VM 层运算符魔法方法动态分发测试 ──────────────────
        # Num 静态类型持有 BigNumber：前端按纯数值运算直接发 opcode，
        # cvm 在 opcode 层发现操作数是类对象后，动态查找 _add_ 等魔法
        # 方法并调用，返回值压回操作栈（区别于上面的静态类型解析路径）。
        Console.println("===== VM operator-method dispatch test =====")

        Num ba = BigNumber( 15 )
        Num bb = BigNumber( 4 )

        # 算术分发（_add_/_sub_/_mul_/_truediv_/_mod_）
        Num bAdd2 = ba + bb
        Num bSub2 = ba - bb
        Num bMul2 = ba * bb
        Num bDiv2 = ba / bb
        Num bMod2 = ba % bb
        Console.println("Num 15 + 4 = " + bAdd2.toString())
        Console.println("Num 15 - 4 = " + bSub2.toString())
        Console.println("Num 15 * 4 = " + bMul2.toString())
        Console.println("Num 15 / 4 = " + bDiv2.toString())
        Console.println("Num 15 % 4 = " + bMod2.toString())

        # 类对象与标量混合：右侧标量作 _mul_ 参数。
        # （左标量 + 右类对象如 2 + ba 在前端 MetaExpressOperator
        #   阶段即报"加减运算类型计算错误"，编译不过，无法测）
        Num bScale2 = ba * 2
        Console.println("Num ba * 2 = " + bScale2.toString())

        # 比较分发（_eq_/_ne_/_lt_/_le_/_gt_/_ge_）
        bool dEq = ba == BigNumber( 15 )
        bool dNe = ba != bb
        bool dLt = ba < bb
        bool dLe = ba <= BigNumber( 15 )
        bool dGt = ba > bb
        bool dGe = ba >= BigNumber( 16 )
        Console.println("ba == 15 : " + dEq.toString())
        Console.println("ba != bb : " + dNe.toString())
        Console.println("ba < bb : " + dLt.toString())
        Console.println("ba <= 15 : " + dLe.toString())
        Console.println("ba > bb : " + dGt.toString())
        Console.println("ba >= 16 : " + dGe.toString())

        # 分支形式：关系/相等分支 opcode 上的同套分发
        if ba > bb
        {
            Console.println("branch: ba > bb holds")
        }
        else
        {
            Console.println("branch: ba <= bb")
        }
        if ba == BigNumber( 15 )
        {
            Console.println("branch: ba == 15 holds")
        }
        else
        {
            Console.println("branch: ba != 15")
        }
        if ba < bb
        {
            Console.println("branch: ba < bb (unexpected)")
        }
        else
        {
            Console.println("branch: ba >= bb holds")
        }

        # Object 静态类型相等分发（左是类只查左，镜像 csharpVM TryCompareClassValue）
        Object oa = BigNumber( 15 )
        Object ob = BigNumber( 15 )
        bool oEq = oa == ob
        bool oNe = oa != ob
        Console.println("oa == ob : " + oEq.toString())
        Console.println("oa != ob : " + oNe.toString())

        # Float32_2（Vector2）经 Object 静态类型的 _eq_/_ne_ 分发
        Object fa = Float32_2( 1.0f, 2.0f )
        Object fb = Float32_2( 1.0f, 2.0f )
        Object fc = Float32_2( 3.0f, 4.0f )
        bool vEq = fa == fb
        bool vNe = fa != fc
        Console.println("fa == fb : " + vEq.toString())
        Console.println("fa != fc : " + vNe.toString())

        # 逻辑分发（_and_/_or_ 注册在 non-static 方法表，仅查左操作数）：
        # 语言约定 _and_/_or_ 返回当前类类型，VM 同步调用后对返回对象
        # is_truthy（非 null 恒真）——结果值与退化路径同为 true，
        # 分发生效与否靠方法内的 trace 打印证明。
        Object f1 = OpFlag( false )
        Object f2 = OpFlag( true )
        bool lAndF = f1 && f2
        bool lOrF = f1 || f2
        bool lAndT = f2 && f1
        bool lOrT = f2 || f1
        Console.println("OpFlag(false) && OpFlag(true) : " + lAndF.toString())
        Console.println("OpFlag(false) || OpFlag(true) : " + lOrF.toString())
        Console.println("OpFlag(true) && OpFlag(false) : " + lAndT.toString())
        Console.println("OpFlag(true) || OpFlag(false) : " + lOrT.toString())

        # 相等分发 + _ne_ 缺失回退：OpFlag 只定义 _eq_（返回 value 字段，
        # 非引用相等），!= 走 "_eq_ 结果取反" 路径（镜像 C# TryRunClassEqualityOperator）
        Object f3 = OpFlag( true )
        bool fSelfEq = f1 == f1
        bool fEq = f2 == f3
        bool fNe = f2 != f3
        bool fNeT = f1 != f3
        Console.println("OpFlag(false) == OpFlag(false) : " + fSelfEq.toString())
        Console.println("OpFlag(true) == OpFlag(true) : " + fEq.toString())
        Console.println("OpFlag(true) != OpFlag(true) : " + fNe.toString())
        Console.println("OpFlag(false) != OpFlag(true) : " + fNeT.toString())

        Console.println("===== Math test end =====")
    }
}

# 逻辑/相等魔法方法分发专用测试类：语言约定 _and_/_or_ 返回当前类
# 类型（VM 对返回对象 is_truthy，非 null 恒真），_eq_ 返回 bool 且返回
# value 字段而非引用相等——自比较返回 false，与退化路径（引用相等恒
# true）结果相反，可区分"方法被调用"与"退化为内置语义"。
class OpFlag
{
    bool value = false

    public void _init_( bool v )
    {
        this.value = v
    }

    override OpFlag _and_( Object other )
    {
        Console.println("  [OpFlag._and_ dispatched]")
        ret OpFlag( this.value )
    }

    override OpFlag _or_( Object other )
    {
        Console.println("  [OpFlag._or_ dispatched]")
        ret OpFlag( this.value )
    }

    override bool _eq_( Object other )
    {
        Console.println("  [OpFlag._eq_ dispatched]")
        ret this.value
    }

    override string toString()
    {
        ret "OpFlag(" + this.value.toString() + ")"
    }
}
