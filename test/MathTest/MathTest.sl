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

        Console.println("===== Math test end =====")
    }
}
