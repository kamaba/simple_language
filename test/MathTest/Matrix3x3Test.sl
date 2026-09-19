import Std;
import Math;

# ============================================================
# Matrix3x3Test - 3x3 矩阵归类测试（Float32 / Float64 / Float16）
# 断言全部选取浮点精确可表示值（2 的幂与小整数）规避舍入误差；
# multiply / transform 元素级运算经 Mathf.dot3 走 FFI（math_lib.dll，
# @DllImport 三参形式带参数签名），half 精度经 toFloat32() 中转。
# ============================================================
class Matrix3x3Test
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

    # ── Float32_3x3 ─────────────────────────────────────────
    static testFloat32()
    {
        Console.println( "===== Matrix3x3Test.testFloat32 =====" )

        # identity / zero 静态工厂
        Float32_3x3 id = Float32_3x3.identity()
        check( "identity m00", id.m00 == 1.0f )
        check( "identity m11", id.m11 == 1.0f )
        check( "identity m22", id.m22 == 1.0f )
        check( "identity offdiag", id.m01 == 0.0f )
        check( "zero m00", Float32_3x3.zero().m00 == 0.0f )

        # 构造 + 索引（_getItem_ / _setItem_ / get / set）
        Float32_3x3 d = Float32_3x3( 2.0f, 0.0f, 0.0f,
                                     0.0f, 4.0f, 0.0f,
                                     0.0f, 0.0f, 8.0f )
        check( "ctor d[0]", d[0] == 2.0f )
        check( "ctor d[4]", d[4] == 4.0f )
        check( "ctor d[8]", d[8] == 8.0f )
        check( "getValue(2,2)", d.getValue( 2, 2 ) == 8.0f )
        d.setValue( 0, 0, 3.0f )
        check( "setValue(0,0)", d.getValue( 0, 0 ) == 3.0f )
        d.setValue( 0, 0, 2.0f )
        d[1] = 6.0f
        check( "operator [] write", d[1] == 6.0f )
        d[1] = 0.0f

        # multiply（FFI dot3 路径）：对角阵平方
        Float32_3x3 dd = d.multiply( d )
        check( "multiply diag m00", dd.m00 == 4.0f )
        check( "multiply diag m11", dd.m11 == 16.0f )
        check( "multiply diag m22", dd.m22 == 64.0f )
        check( "multiply offdiag", dd.m01 == 0.0f )
        check( "identity multiply", id.multiply( d ) == d )

        # 非对角矩阵：A = [[1,2],[3,4]] 嵌入 3x3
        Float32_3x3 a = Float32_3x3( 1.0f, 2.0f, 0.0f,
                                    3.0f, 4.0f, 0.0f,
                                    0.0f, 0.0f, 1.0f )
        Float32_3x3 aa = a.multiply( a )
        check( "A^2 m00 == 7", aa.m00 == 7.0f )
        check( "A^2 m01 == 10", aa.m01 == 10.0f )
        check( "A^2 m10 == 15", aa.m10 == 15.0f )
        check( "A^2 m11 == 22", aa.m11 == 22.0f )

        # transform（FFI dot3 路径）
        Float32_3 tv = d.transform( Float32_3( 1.0f, 1.0f, 1.0f ) )
        check( "transform x", tv.x == 2.0f )
        check( "transform y", tv.y == 4.0f )
        check( "transform z", tv.z == 8.0f )

        # determinant / inverse
        check( "det(diag 2,4,8) == 64", d.determinant() == 64.0f )
        check( "det(A) == -2", a.determinant() == -2.0f )

        Float32_3x3 inv = d.inverse()
        check( "inverse m00 == 0.5", inv.m00 == 0.5f )
        check( "inverse m11 == 0.25", inv.m11 == 0.25f )
        check( "inverse m22 == 0.125", inv.m22 == 0.125f )
        check( "A * A^-1 == I", a.multiply( a.inverse() ) == id )
        check( "singular inverse is zero", Float32_3x3.zero().inverse() == Float32_3x3.zero() )

        # transpose
        Float32_3x3 at = a.transpose()
        check( "transpose m01", at.m01 == 3.0f )
        check( "transpose m10", at.m10 == 2.0f )
        check( "transpose twice", at.transpose() == a )

        # 旋转 / 平移 / 缩放静态工厂
        check( "rotationX(0) == I", Float32_3x3.rotationX( 0.0f ) == id )
        check( "rotationY(0) == I", Float32_3x3.rotationY( 0.0f ) == id )
        check( "rotationZ(0) == I", Float32_3x3.rotationZ( 0.0f ) == id )

        Float32_3 tp = Float32_3x3.translation( 3.0f, 4.0f ).transform( Float32_3( 1.0f, 1.0f, 1.0f ) )
        check( "translation x", tp.x == 4.0f )
        check( "translation y", tp.y == 5.0f )
        Float32_3 sp = Float32_3x3.scale( 2.0f, 3.0f ).transform( Float32_3( 1.0f, 1.0f, 1.0f ) )
        check( "scale x", sp.x == 2.0f )
        check( "scale y", sp.y == 3.0f )

        # clone / 运算符重载
        check( "clone equal", d.clone() == d )
        Float32_3x3 sum = d + d
        check( "operator + m00", sum.m00 == 4.0f )
        Float32_3x3 prod = d * d
        check( "operator * m00", prod.m00 == 4.0f )
        check( "operator !=", d != id )

        Console.println( "  toString: " + id.toString() )
    }

    # ── Float64_3x3 ─────────────────────────────────────────
    static testFloat64()
    {
        Console.println( "===== Matrix3x3Test.testFloat64 =====" )

        Float64_3x3 id = Float64_3x3.identity()
        check( "identity m00", id.m00 == 1.0d )
        check( "identity m11", id.m11 == 1.0d )
        check( "identity m22", id.m22 == 1.0d )
        check( "identity offdiag", id.m01 == 0.0d )
        check( "zero m00", Float64_3x3.zero().m00 == 0.0d )

        Float64_3x3 d = Float64_3x3( 2.0d, 0.0d, 0.0d,
                                     0.0d, 4.0d, 0.0d,
                                     0.0d, 0.0d, 8.0d )
        check( "ctor d[0]", d[0] == 2.0d )
        check( "ctor d[4]", d[4] == 4.0d )
        check( "getValue(2,2)", d.getValue( 2, 2 ) == 8.0d )
        d.setValue( 0, 0, 3.0d )
        check( "setValue(0,0)", d.getValue( 0, 0 ) == 3.0d )
        d.setValue( 0, 0, 2.0d )

        Float64_3x3 dd = d.multiply( d )
        check( "multiply diag m00", dd.m00 == 4.0d )
        check( "multiply diag m11", dd.m11 == 16.0d )
        check( "multiply diag m22", dd.m22 == 64.0d )
        check( "multiply offdiag", dd.m01 == 0.0d )
        check( "identity multiply", id.multiply( d ) == d )

        Float64_3x3 a = Float64_3x3( 1.0d, 2.0d, 0.0d,
                                    3.0d, 4.0d, 0.0d,
                                    0.0d, 0.0d, 1.0d )
        Float64_3x3 aa = a.multiply( a )
        check( "A^2 m00 == 7", aa.m00 == 7.0d )
        check( "A^2 m01 == 10", aa.m01 == 10.0d )
        check( "A^2 m10 == 15", aa.m10 == 15.0d )
        check( "A^2 m11 == 22", aa.m11 == 22.0d )

        Float64_3 tv = d.transform( Float64_3( 1.0d, 1.0d, 1.0d ) )
        check( "transform x", tv.x == 2.0d )
        check( "transform y", tv.y == 4.0d )
        check( "transform z", tv.z == 8.0d )

        check( "det(diag 2,4,8) == 64", d.determinant() == 64.0d )
        check( "det(A) == -2", a.determinant() == -2.0d )

        Float64_3x3 inv = d.inverse()
        check( "inverse m00 == 0.5", inv.m00 == 0.5d )
        check( "inverse m11 == 0.25", inv.m11 == 0.25d )
        check( "inverse m22 == 0.125", inv.m22 == 0.125d )
        check( "A * A^-1 == I", a.multiply( a.inverse() ) == id )
        check( "singular inverse is zero", Float64_3x3.zero().inverse() == Float64_3x3.zero() )

        Float64_3x3 at = a.transpose()
        check( "transpose m01", at.m01 == 3.0d )
        check( "transpose m10", at.m10 == 2.0d )
        check( "transpose twice", at.transpose() == a )

        check( "rotationX(0) == I", Float64_3x3.rotationX( 0.0d ) == id )
        check( "rotationY(0) == I", Float64_3x3.rotationY( 0.0d ) == id )
        check( "rotationZ(0) == I", Float64_3x3.rotationZ( 0.0d ) == id )

        Float64_3 tp = Float64_3x3.translation( 3.0d, 4.0d ).transform( Float64_3( 1.0d, 1.0d, 1.0d ) )
        check( "translation x", tp.x == 4.0d )
        check( "translation y", tp.y == 5.0d )
        Float64_3 sp = Float64_3x3.scale( 2.0d, 3.0d ).transform( Float64_3( 1.0d, 1.0d, 1.0d ) )
        check( "scale x", sp.x == 2.0d )
        check( "scale y", sp.y == 3.0d )

        check( "clone equal", d.clone() == d )
        Float64_3x3 sum = d + d
        check( "operator + m00", sum.m00 == 4.0d )
        Float64_3x3 prod = d * d
        check( "operator * m00", prod.m00 == 4.0d )
        check( "operator !=", d != id )

        # Float32_3x3 提升构造
        Float32_3x3 d32 = Float32_3x3( 2.0f, 0.0f, 0.0f,
                                      0.0f, 4.0f, 0.0f,
                                      0.0f, 0.0f, 8.0f )
        Float64_3x3 from32 = Float64_3x3( d32 )
        check( "Float64_3x3( Float32_3x3 ) m00", from32.m00 == 2.0d )
        check( "Float64_3x3( Float32_3x3 ) m22", from32.m22 == 8.0d )

        Console.println( "  toString: " + id.toString() )
    }

    # ── Float16_3x3 ─────────────────────────────────────────
    static testFloat16()
    {
        Console.println( "===== Matrix3x3Test.testFloat16 =====" )

        Float16_3x3 id = Float16_3x3.identity()
        check( "identity m00", id.m00 == 1.0h )
        check( "identity m11", id.m11 == 1.0h )
        check( "identity m22", id.m22 == 1.0h )
        check( "identity offdiag", id.m01 == 0.0h )
        check( "zero m00", Float16_3x3.zero().m00 == 0.0h )

        Float16_3x3 d = Float16_3x3( 2.0h, 0.0h, 0.0h,
                                     0.0h, 4.0h, 0.0h,
                                     0.0h, 0.0h, 8.0h )
        check( "ctor d[0]", d[0] == 2.0h )
        check( "ctor d[4]", d[4] == 4.0h )
        check( "getValue(2,2)", d.getValue( 2, 2 ) == 8.0h )
        d.setValue( 0, 0, 3.0h )
        check( "setValue(0,0)", d.getValue( 0, 0 ) == 3.0h )
        d.setValue( 0, 0, 2.0h )

        # multiply（toFloat32 中转走 FFI dot3，结果隐式收敛回 half）
        Float16_3x3 dd = d.multiply( d )
        check( "multiply diag m00", dd.m00 == 4.0h )
        check( "multiply diag m11", dd.m11 == 16.0h )
        check( "multiply diag m22", dd.m22 == 64.0h )
        check( "multiply offdiag", dd.m01 == 0.0h )
        check( "identity multiply", id.multiply( d ) == d )

        Float16_3x3 a = Float16_3x3( 1.0h, 2.0h, 0.0h,
                                    3.0h, 4.0h, 0.0h,
                                    0.0h, 0.0h, 1.0h )
        Float16_3x3 aa = a.multiply( a )
        check( "A^2 m00 == 7", aa.m00 == 7.0h )
        check( "A^2 m01 == 10", aa.m01 == 10.0h )
        check( "A^2 m10 == 15", aa.m10 == 15.0h )
        check( "A^2 m11 == 22", aa.m11 == 22.0h )

        Float16_3 tv = d.transform( Float16_3( 1.0h, 1.0h, 1.0h ) )
        check( "transform x", tv.x == 2.0h )
        check( "transform y", tv.y == 4.0h )
        check( "transform z", tv.z == 8.0h )

        check( "det(diag 2,4,8) == 64", d.determinant() == 64.0h )
        check( "det(A) == -2", a.determinant() == -2.0h )

        Float16_3x3 inv = d.inverse()
        check( "inverse m00 == 0.5", inv.m00 == 0.5h )
        check( "inverse m11 == 0.25", inv.m11 == 0.25h )
        check( "inverse m22 == 0.125", inv.m22 == 0.125h )
        check( "A * A^-1 == I", a.multiply( a.inverse() ) == id )
        check( "singular inverse is zero", Float16_3x3.zero().inverse() == Float16_3x3.zero() )

        Float16_3x3 at = a.transpose()
        check( "transpose m01", at.m01 == 3.0h )
        check( "transpose m10", at.m10 == 2.0h )
        check( "transpose twice", at.transpose() == a )

        check( "rotationX(0) == I", Float16_3x3.rotationX( 0.0h ) == id )
        check( "rotationY(0) == I", Float16_3x3.rotationY( 0.0h ) == id )
        check( "rotationZ(0) == I", Float16_3x3.rotationZ( 0.0h ) == id )

        Float16_3 tp = Float16_3x3.translation( 3.0h, 4.0h ).transform( Float16_3( 1.0h, 1.0h, 1.0h ) )
        check( "translation x", tp.x == 4.0h )
        check( "translation y", tp.y == 5.0h )
        Float16_3 sp = Float16_3x3.scale( 2.0h, 3.0h ).transform( Float16_3( 1.0h, 1.0h, 1.0h ) )
        check( "scale x", sp.x == 2.0h )
        check( "scale y", sp.y == 3.0h )

        check( "clone equal", d.clone() == d )
        Float16_3x3 sum = d + d
        check( "operator + m00", sum.m00 == 4.0h )
        Float16_3x3 prod = d * d
        check( "operator * m00", prod.m00 == 4.0h )
        check( "operator !=", d != id )

        # Float32_3x3 降精度构造 + 提升回转
        Float32_3x3 d32 = Float32_3x3( 2.0f, 0.0f, 0.0f,
                                      0.0f, 4.0f, 0.0f,
                                      0.0f, 0.0f, 8.0f )
        Float16_3x3 from32 = Float16_3x3( d32 )
        check( "Float16_3x3( Float32_3x3 ) m00", from32.m00 == 2.0h )
        check( "Float16_3x3( Float32_3x3 ) m22", from32.m22 == 8.0h )
        Float32_3x3 rt = from32.toFloat32_3x3()
        check( "toFloat32_3x3 roundtrip", rt == d32 )

        Console.println( "  toString: " + id.toString() )
    }

    static fun()
    {
        testFloat32()
        testFloat64()
        testFloat16()
    }
}
