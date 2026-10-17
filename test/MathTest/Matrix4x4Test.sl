import Std;
import Math;

# ============================================================
# Matrix4x4Test - 4x4 矩阵归类测试（Float32 / Float64 / Float16）
# 断言全部选取浮点精确可表示值（2 的幂与小整数）规避舍入误差；
# multiply / transformPoint / transformDirection 元素级运算经
# Mathf.dot3 走 FFI（math_lib.dll，@DllImport 三参形式带参数签名），
# half 精度经 toFloat32() 中转。
# ============================================================
class Matrix4x4Test
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

    # ── Float32_4x4 ─────────────────────────────────────────
    static testFloat32()
    {
        Console.println( "===== Matrix4x4Test.testFloat32 =====" )

        Float32_4x4 id = Float32_4x4.identity()
        check( "identity m00", id.m00 == 1.0f )
        check( "identity m11", id.m11 == 1.0f )
        check( "identity m33", id.m33 == 1.0f )
        check( "identity offdiag", id.m01 == 0.0f )
        check( "zero m00", Float32_4x4.zero().m00 == 0.0f )

        # 构造 + 索引
        Float32_4x4 d = Float32_4x4( 2.0f, 0.0f, 0.0f, 0.0f,
                                     0.0f, 4.0f, 0.0f, 0.0f,
                                     0.0f, 0.0f, 8.0f, 0.0f,
                                     0.0f, 0.0f, 0.0f, 1.0f )
        check( "ctor d[0]", d[0] == 2.0f )
        check( "ctor d[5]", d[5] == 4.0f )
        check( "ctor d[10]", d[10] == 8.0f )
        check( "getValue(3,3)", d.getValue( 3, 3 ) == 1.0f )
        d.setValue( 1, 1, 5.0f )
        check( "setValue(1,1)", d.getValue( 1, 1 ) == 5.0f )
        d.setValue( 1, 1, 4.0f )

        # multiply（FFI dot3 前 3 项 + SL 层第 4 项）
        Float32_4x4 dd = d.multiply( d )
        check( "multiply diag m00", dd.m00 == 4.0f )
        check( "multiply diag m11", dd.m11 == 16.0f )
        check( "multiply diag m22", dd.m22 == 64.0f )
        check( "multiply diag m33", dd.m33 == 1.0f )
        check( "multiply offdiag", dd.m01 == 0.0f )
        check( "identity multiply", id.multiply( d ) == d )

        # 非对角矩阵：A = [[1,2],[3,4]] 嵌入 4x4
        Float32_4x4 a = Float32_4x4( 1.0f, 2.0f, 0.0f, 0.0f,
                                    3.0f, 4.0f, 0.0f, 0.0f,
                                    0.0f, 0.0f, 1.0f, 0.0f,
                                    0.0f, 0.0f, 0.0f, 1.0f )
        Float32_4x4 aa = a.multiply( a )
        check( "A^2 m00 == 7", aa.m00 == 7.0f )
        check( "A^2 m01 == 10", aa.m01 == 10.0f )
        check( "A^2 m10 == 15", aa.m10 == 15.0f )
        check( "A^2 m11 == 22", aa.m11 == 22.0f )

        # transformPoint（w 补 1，带平移）/ transformDirection（w 补 0）
        Float32_4x4 tmat = Float32_4x4.translation( 1.0f, 2.0f, 3.0f )
        Float32_3 pt = tmat.transformPoint( Float32_3( 1.0f, 1.0f, 1.0f ) )
        check( "transformPoint x", pt.x == 2.0f )
        check( "transformPoint y", pt.y == 3.0f )
        check( "transformPoint z", pt.z == 4.0f )
        Float32_3 dir = tmat.transformDirection( Float32_3( 1.0f, 1.0f, 1.0f ) )
        check( "transformDirection x", dir.x == 1.0f )
        check( "transformDirection y", dir.y == 1.0f )
        check( "transformDirection z", dir.z == 1.0f )

        Float32_3 sv = d.transformPoint( Float32_3( 1.0f, 1.0f, 1.0f ) )
        check( "diag transformPoint x", sv.x == 2.0f )
        check( "diag transformPoint y", sv.y == 4.0f )
        check( "diag transformPoint z", sv.z == 8.0f )

        # determinant / inverse
        check( "det(diag 2,4,8,1) == 64", d.determinant() == 64.0f )
        check( "det(scale 2,4,8) == 64", Float32_4x4.scale( 2.0f, 4.0f, 8.0f ).determinant() == 64.0f )
        check( "det(A) == -2", a.determinant() == -2.0f )

        Float32_4x4 inv = d.inverse()
        check( "inverse m00 == 0.5", inv.m00 == 0.5f )
        check( "inverse m11 == 0.25", inv.m11 == 0.25f )
        check( "inverse m22 == 0.125", inv.m22 == 0.125f )
        check( "inverse m33 == 1", inv.m33 == 1.0f )
        check( "A * A^-1 == I", a.multiply( a.inverse() ) == id )
        check( "singular inverse is zero", Float32_4x4.zero().inverse() == Float32_4x4.zero() )

        # transpose
        Float32_4x4 at = a.transpose()
        check( "transpose m01", at.m01 == 3.0f )
        check( "transpose m10", at.m10 == 2.0f )
        check( "transpose twice", at.transpose() == a )

        # 旋转静态工厂
        check( "rotationX(0) == I", Float32_4x4.rotationX( 0.0f ) == id )
        check( "rotationY(0) == I", Float32_4x4.rotationY( 0.0f ) == id )
        check( "rotationZ(0) == I", Float32_4x4.rotationZ( 0.0f ) == id )
        check( "rotationAxis((0,1,0),0) == I", Float32_4x4.rotationAxis( Float32_3( 0.0f, 1.0f, 0.0f ), 0.0f ) == id )

        # 平移 / 缩放工厂
        check( "translation m03", tmat.m03 == 1.0f )
        check( "translation m13", tmat.m13 == 2.0f )
        check( "translation m23", tmat.m23 == 3.0f )
        check( "translation(v3) m23", Float32_4x4.translation( Float32_3( 1.0f, 2.0f, 3.0f ) ).m23 == 3.0f )
        check( "scale m00", Float32_4x4.scale( 2.0f, 4.0f, 8.0f ).m00 == 2.0f )
        check( "scale(v3) m11", Float32_4x4.scale( Float32_3( 2.0f, 4.0f, 8.0f ) ).m11 == 4.0f )
        check( "scale(s) m22", Float32_4x4.scale( 8.0f ).m22 == 8.0f )

        # TRS 组合：零平移零旋转单位缩放 == identity
        check( "trs(zero,zero,one) == I", Float32_4x4.trs( Float32_3( 0.0f, 0.0f, 0.0f ),
                                                           Float32_3( 0.0f, 0.0f, 0.0f ),
                                                           Float32_3( 1.0f, 1.0f, 1.0f ) ) == id )

        # ortho 对称区间：对角 (1,1,-1,1) 且平移分量全 0
        Float32_4x4 o = Float32_4x4.ortho( -1.0f, 1.0f, -1.0f, 1.0f, -1.0f, 1.0f )
        check( "ortho m00 == 1", o.m00 == 1.0f )
        check( "ortho m11 == 1", o.m11 == 1.0f )
        check( "ortho m22 == -1", o.m22 == -1.0f )
        check( "ortho m33 == 1", o.m33 == 1.0f )
        check( "ortho m30 == 0", o.m30 == 0.0f )
        check( "ortho m32 == 0", o.m32 == 0.0f )

        # perspective 结构（near=1 far=2 的精确分量；f 依赖 tan 不做精确断言）
        Float32_4x4 p = Float32_4x4.perspective( 1.0f, 1.0f, 1.0f, 2.0f )
        check( "perspective m01 == 0", p.m01 == 0.0f )
        check( "perspective m10 == 0", p.m10 == 0.0f )
        check( "perspective m22 == -3", p.m22 == -3.0f )
        check( "perspective m23 == -4", p.m23 == -4.0f )
        check( "perspective m32 == -1", p.m32 == -1.0f )
        check( "perspective m30 == 0", p.m30 == 0.0f )
        check( "perspective m33 == 0", p.m33 == 0.0f )

        # lookAt 轴对齐场景：eye=(0,0,5) 看原点 => 平移 (0,0,-5)
        Float32_4x4 la = Float32_4x4.lookAt( Float32_3( 0.0f, 0.0f, 5.0f ),
                                             Float32_3( 0.0f, 0.0f, 0.0f ),
                                             Float32_3( 0.0f, 1.0f, 0.0f ) )
        check( "lookAt m00 == 1", la.m00 == 1.0f )
        check( "lookAt m11 == 1", la.m11 == 1.0f )
        check( "lookAt m22 == 1", la.m22 == 1.0f )
        check( "lookAt m03 == 0", la.m03 == 0.0f )
        check( "lookAt m13 == 0", la.m13 == 0.0f )
        check( "lookAt m23 == -5", la.m23 == -5.0f )
        check( "lookAt m33 == 1", la.m33 == 1.0f )

        # clone / 运算符重载 / 3x3 提取
        check( "clone equal", d.clone() == d )
        Float32_4x4 sum = d + d
        check( "operator + m00", sum.m00 == 4.0f )
        Float32_4x4 prod = d * d
        check( "operator * m00", prod.m00 == 4.0f )
        check( "operator !=", d != id )

        Float32_3x3 sub = tmat.toFloat32_3x3()
        check( "toFloat32_3x3 m02", sub.m02 == 1.0f )
        check( "toFloat32_3x3 m12", sub.m12 == 2.0f )
        check( "toFloat32_3x3 m22", sub.m22 == 1.0f )

        Console.println( "  toString: " + id.toString() )
    }

    # ── Float64_4x4 ─────────────────────────────────────────
    static testFloat64()
    {
        Console.println( "===== Matrix4x4Test.testFloat64 =====" )

        Float64_4x4 id = Float64_4x4.identity()
        check( "identity m00", id.m00 == 1.0d )
        check( "identity m11", id.m11 == 1.0d )
        check( "identity m33", id.m33 == 1.0d )
        check( "identity offdiag", id.m01 == 0.0d )
        check( "zero m00", Float64_4x4.zero().m00 == 0.0d )

        Float64_4x4 d = Float64_4x4( 2.0d, 0.0d, 0.0d, 0.0d,
                                     0.0d, 4.0d, 0.0d, 0.0d,
                                     0.0d, 0.0d, 8.0d, 0.0d,
                                     0.0d, 0.0d, 0.0d, 1.0d )
        check( "ctor d[0]", d[0] == 2.0d )
        check( "ctor d[5]", d[5] == 4.0d )
        check( "ctor d[10]", d[10] == 8.0d )
        check( "getValue(3,3)", d.getValue( 3, 3 ) == 1.0d )
        d.setValue( 1, 1, 5.0d )
        check( "setValue(1,1)", d.getValue( 1, 1 ) == 5.0d )
        d.setValue( 1, 1, 4.0d )

        Float64_4x4 dd = d.multiply( d )
        check( "multiply diag m00", dd.m00 == 4.0d )
        check( "multiply diag m11", dd.m11 == 16.0d )
        check( "multiply diag m22", dd.m22 == 64.0d )
        check( "multiply diag m33", dd.m33 == 1.0d )
        check( "multiply offdiag", dd.m01 == 0.0d )
        check( "identity multiply", id.multiply( d ) == d )

        Float64_4x4 a = Float64_4x4( 1.0d, 2.0d, 0.0d, 0.0d,
                                    3.0d, 4.0d, 0.0d, 0.0d,
                                    0.0d, 0.0d, 1.0d, 0.0d,
                                    0.0d, 0.0d, 0.0d, 1.0d )
        Float64_4x4 aa = a.multiply( a )
        check( "A^2 m00 == 7", aa.m00 == 7.0d )
        check( "A^2 m01 == 10", aa.m01 == 10.0d )
        check( "A^2 m10 == 15", aa.m10 == 15.0d )
        check( "A^2 m11 == 22", aa.m11 == 22.0d )

        Float64_4x4 tmat = Float64_4x4.translation( 1.0d, 2.0d, 3.0d )
        Float64_3 pt = tmat.transformPoint( Float64_3( 1.0d, 1.0d, 1.0d ) )
        check( "transformPoint x", pt.x == 2.0d )
        check( "transformPoint y", pt.y == 3.0d )
        check( "transformPoint z", pt.z == 4.0d )
        Float64_3 dir = tmat.transformDirection( Float64_3( 1.0d, 1.0d, 1.0d ) )
        check( "transformDirection x", dir.x == 1.0d )
        check( "transformDirection y", dir.y == 1.0d )
        check( "transformDirection z", dir.z == 1.0d )

        check( "det(diag 2,4,8,1) == 64", d.determinant() == 64.0d )
        check( "det(scale 2,4,8) == 64", Float64_4x4.scale( 2.0d, 4.0d, 8.0d ).determinant() == 64.0d )
        check( "det(A) == -2", a.determinant() == -2.0d )

        Float64_4x4 inv = d.inverse()
        check( "inverse m00 == 0.5", inv.m00 == 0.5d )
        check( "inverse m11 == 0.25", inv.m11 == 0.25d )
        check( "inverse m22 == 0.125", inv.m22 == 0.125d )
        check( "inverse m33 == 1", inv.m33 == 1.0d )
        check( "A * A^-1 == I", a.multiply( a.inverse() ) == id )
        check( "singular inverse is zero", Float64_4x4.zero().inverse() == Float64_4x4.zero() )

        Float64_4x4 at = a.transpose()
        check( "transpose m01", at.m01 == 3.0d )
        check( "transpose m10", at.m10 == 2.0d )
        check( "transpose twice", at.transpose() == a )

        check( "rotationX(0) == I", Float64_4x4.rotationX( 0.0d ) == id )
        check( "rotationY(0) == I", Float64_4x4.rotationY( 0.0d ) == id )
        check( "rotationZ(0) == I", Float64_4x4.rotationZ( 0.0d ) == id )
        check( "rotationAxis((0,1,0),0) == I", Float64_4x4.rotationAxis( Float64_3( 0.0d, 1.0d, 0.0d ), 0.0d ) == id )

        check( "translation m03", tmat.m03 == 1.0d )
        check( "translation m13", tmat.m13 == 2.0d )
        check( "translation m23", tmat.m23 == 3.0d )
        check( "translation(v3) m23", Float64_4x4.translation( Float64_3( 1.0d, 2.0d, 3.0d ) ).m23 == 3.0d )
        check( "scale m00", Float64_4x4.scale( 2.0d, 4.0d, 8.0d ).m00 == 2.0d )
        check( "scale(v3) m11", Float64_4x4.scale( Float64_3( 2.0d, 4.0d, 8.0d ) ).m11 == 4.0d )
        check( "scale(s) m22", Float64_4x4.scale( 8.0d ).m22 == 8.0d )

        check( "trs(zero,zero,one) == I", Float64_4x4.trs( Float64_3( 0.0d, 0.0d, 0.0d ),
                                                            Float64_3( 0.0d, 0.0d, 0.0d ),
                                                            Float64_3( 1.0d, 1.0d, 1.0d ) ) == id )

        Float64_4x4 o = Float64_4x4.ortho( -1.0d, 1.0d, -1.0d, 1.0d, -1.0d, 1.0d )
        check( "ortho m00 == 1", o.m00 == 1.0d )
        check( "ortho m11 == 1", o.m11 == 1.0d )
        check( "ortho m22 == -1", o.m22 == -1.0d )
        check( "ortho m33 == 1", o.m33 == 1.0d )
        check( "ortho m30 == 0", o.m30 == 0.0d )
        check( "ortho m32 == 0", o.m32 == 0.0d )

        Float64_4x4 p = Float64_4x4.perspective( 1.0d, 1.0d, 1.0d, 2.0d )
        check( "perspective m01 == 0", p.m01 == 0.0d )
        check( "perspective m10 == 0", p.m10 == 0.0d )
        check( "perspective m22 == -3", p.m22 == -3.0d )
        check( "perspective m23 == -4", p.m23 == -4.0d )
        check( "perspective m32 == -1", p.m32 == -1.0d )
        check( "perspective m30 == 0", p.m30 == 0.0d )
        check( "perspective m33 == 0", p.m33 == 0.0d )

        Float64_4x4 la = Float64_4x4.lookAt( Float64_3( 0.0d, 0.0d, 5.0d ),
                                            Float64_3( 0.0d, 0.0d, 0.0d ),
                                            Float64_3( 0.0d, 1.0d, 0.0d ) )
        check( "lookAt m00 == 1", la.m00 == 1.0d )
        check( "lookAt m11 == 1", la.m11 == 1.0d )
        check( "lookAt m22 == 1", la.m22 == 1.0d )
        check( "lookAt m03 == 0", la.m03 == 0.0d )
        check( "lookAt m13 == 0", la.m13 == 0.0d )
        check( "lookAt m23 == -5", la.m23 == -5.0d )
        check( "lookAt m33 == 1", la.m33 == 1.0d )

        check( "clone equal", d.clone() == d )
        Float64_4x4 sum = d + d
        check( "operator + m00", sum.m00 == 4.0d )
        Float64_4x4 prod = d * d
        check( "operator * m00", prod.m00 == 4.0d )
        check( "operator !=", d != id )

        Float64_3x3 sub = tmat.toFloat64_3x3()
        check( "toFloat64_3x3 m02", sub.m02 == 1.0d )
        check( "toFloat64_3x3 m12", sub.m12 == 2.0d )
        check( "toFloat64_3x3 m22", sub.m22 == 1.0d )

        Console.println( "  toString: " + id.toString() )
    }

    # ── Float16_4x4 ─────────────────────────────────────────
    static testFloat16()
    {
        Console.println( "===== Matrix4x4Test.testFloat16 =====" )

        Float16_4x4 id = Float16_4x4.identity()
        check( "identity m00", id.m00 == 1.0h )
        check( "identity m11", id.m11 == 1.0h )
        check( "identity m33", id.m33 == 1.0h )
        check( "identity offdiag", id.m01 == 0.0h )
        check( "zero m00", Float16_4x4.zero().m00 == 0.0h )

        Float16_4x4 d = Float16_4x4( 2.0h, 0.0h, 0.0h, 0.0h,
                                     0.0h, 4.0h, 0.0h, 0.0h,
                                     0.0h, 0.0h, 8.0h, 0.0h,
                                     0.0h, 0.0h, 0.0h, 1.0h )
        check( "ctor d[0]", d[0] == 2.0h )
        check( "ctor d[5]", d[5] == 4.0h )
        check( "ctor d[10]", d[10] == 8.0h )
        check( "getValue(3,3)", d.getValue( 3, 3 ) == 1.0h )
        d.setValue( 1, 1, 5.0h )
        check( "setValue(1,1)", d.getValue( 1, 1 ) == 5.0h )
        d.setValue( 1, 1, 4.0h )

        # multiply（toFloat32 中转走 FFI dot3，结果隐式收敛回 half）
        Float16_4x4 dd = d.multiply( d )
        check( "multiply diag m00", dd.m00 == 4.0h )
        check( "multiply diag m11", dd.m11 == 16.0h )
        check( "multiply diag m22", dd.m22 == 64.0h )
        check( "multiply diag m33", dd.m33 == 1.0h )
        check( "multiply offdiag", dd.m01 == 0.0h )
        check( "identity multiply", id.multiply( d ) == d )

        Float16_4x4 a = Float16_4x4( 1.0h, 2.0h, 0.0h, 0.0h,
                                    3.0h, 4.0h, 0.0h, 0.0h,
                                    0.0h, 0.0h, 1.0h, 0.0h,
                                    0.0h, 0.0h, 0.0h, 1.0h )
        Float16_4x4 aa = a.multiply( a )
        check( "A^2 m00 == 7", aa.m00 == 7.0h )
        check( "A^2 m01 == 10", aa.m01 == 10.0h )
        check( "A^2 m10 == 15", aa.m10 == 15.0h )
        check( "A^2 m11 == 22", aa.m11 == 22.0h )

        Float16_4x4 tmat = Float16_4x4.translation( 1.0h, 2.0h, 3.0h )
        Float16_3 pt = tmat.transformPoint( Float16_3( 1.0h, 1.0h, 1.0h ) )
        check( "transformPoint x", pt.x == 2.0h )
        check( "transformPoint y", pt.y == 3.0h )
        check( "transformPoint z", pt.z == 4.0h )
        Float16_3 dir = tmat.transformDirection( Float16_3( 1.0h, 1.0h, 1.0h ) )
        check( "transformDirection x", dir.x == 1.0h )
        check( "transformDirection y", dir.y == 1.0h )
        check( "transformDirection z", dir.z == 1.0h )

        check( "det(diag 2,4,8,1) == 64", d.determinant() == 64.0h )
        check( "det(scale 2,4,8) == 64", Float16_4x4.scale( 2.0h, 4.0h, 8.0h ).determinant() == 64.0h )
        check( "det(A) == -2", a.determinant() == -2.0h )

        Float16_4x4 inv = d.inverse()
        check( "inverse m00 == 0.5", inv.m00 == 0.5h )
        check( "inverse m11 == 0.25", inv.m11 == 0.25h )
        check( "inverse m22 == 0.125", inv.m22 == 0.125h )
        check( "inverse m33 == 1", inv.m33 == 1.0h )
        check( "A * A^-1 == I", a.multiply( a.inverse() ) == id )
        check( "singular inverse is zero", Float16_4x4.zero().inverse() == Float16_4x4.zero() )

        Float16_4x4 at = a.transpose()
        check( "transpose m01", at.m01 == 3.0h )
        check( "transpose m10", at.m10 == 2.0h )
        check( "transpose twice", at.transpose() == a )

        check( "rotationX(0) == I", Float16_4x4.rotationX( 0.0h ) == id )
        check( "rotationY(0) == I", Float16_4x4.rotationY( 0.0h ) == id )
        check( "rotationZ(0) == I", Float16_4x4.rotationZ( 0.0h ) == id )
        check( "rotationAxis((0,1,0),0) == I", Float16_4x4.rotationAxis( Float16_3( 0.0h, 1.0h, 0.0h ), 0.0h ) == id )

        check( "translation m03", tmat.m03 == 1.0h )
        check( "translation m13", tmat.m13 == 2.0h )
        check( "translation m23", tmat.m23 == 3.0h )
        check( "translation(v3) m23", Float16_4x4.translation( Float16_3( 1.0h, 2.0h, 3.0h ) ).m23 == 3.0h )
        check( "scale m00", Float16_4x4.scale( 2.0h, 4.0h, 8.0h ).m00 == 2.0h )
        check( "scale(v3) m11", Float16_4x4.scale( Float16_3( 2.0h, 4.0h, 8.0h ) ).m11 == 4.0h )
        check( "scale(s) m22", Float16_4x4.scale( 8.0h ).m22 == 8.0h )

        check( "trs(zero,zero,one) == I", Float16_4x4.trs( Float16_3( 0.0h, 0.0h, 0.0h ),
                                                           Float16_3( 0.0h, 0.0h, 0.0h ),
                                                           Float16_3( 1.0h, 1.0h, 1.0h ) ) == id )

        Float16_4x4 o = Float16_4x4.ortho( -1.0h, 1.0h, -1.0h, 1.0h, -1.0h, 1.0h )
        check( "ortho m00 == 1", o.m00 == 1.0h )
        check( "ortho m11 == 1", o.m11 == 1.0h )
        check( "ortho m22 == -1", o.m22 == -1.0h )
        check( "ortho m33 == 1", o.m33 == 1.0h )
        check( "ortho m30 == 0", o.m30 == 0.0h )
        check( "ortho m32 == 0", o.m32 == 0.0h )

        Float16_4x4 p = Float16_4x4.perspective( 1.0h, 1.0h, 1.0h, 2.0h )
        check( "perspective m01 == 0", p.m01 == 0.0h )
        check( "perspective m10 == 0", p.m10 == 0.0h )
        check( "perspective m22 == -3", p.m22 == -3.0h )
        check( "perspective m23 == -4", p.m23 == -4.0h )
        check( "perspective m32 == -1", p.m32 == -1.0h )
        check( "perspective m30 == 0", p.m30 == 0.0h )
        check( "perspective m33 == 0", p.m33 == 0.0h )

        Float16_4x4 la = Float16_4x4.lookAt( Float16_3( 0.0h, 0.0h, 5.0h ),
                                             Float16_3( 0.0h, 0.0h, 0.0h ),
                                             Float16_3( 0.0h, 1.0h, 0.0h ) )
        check( "lookAt m00 == 1", la.m00 == 1.0h )
        check( "lookAt m11 == 1", la.m11 == 1.0h )
        check( "lookAt m22 == 1", la.m22 == 1.0h )
        check( "lookAt m03 == 0", la.m03 == 0.0h )
        check( "lookAt m13 == 0", la.m13 == 0.0h )
        check( "lookAt m23 == -5", la.m23 == -5.0h )
        check( "lookAt m33 == 1", la.m33 == 1.0h )

        check( "clone equal", d.clone() == d )
        Float16_4x4 sum = d + d
        check( "operator + m00", sum.m00 == 4.0h )
        Float16_4x4 prod = d * d
        check( "operator * m00", prod.m00 == 4.0h )
        check( "operator !=", d != id )

        # Float32_4x4 降精度构造 + 提升回转
        Float32_4x4 d32 = Float32_4x4( 2.0f, 0.0f, 0.0f, 0.0f,
                                      0.0f, 4.0f, 0.0f, 0.0f,
                                      0.0f, 0.0f, 8.0f, 0.0f,
                                      0.0f, 0.0f, 0.0f, 1.0f )
        Float16_4x4 from32 = Float16_4x4( d32 )
        check( "Float16_4x4( Float32_4x4 ) m00", from32.m00 == 2.0h )
        check( "Float16_4x4( Float32_4x4 ) m22", from32.m22 == 8.0h )
        Float32_4x4 rt = from32.toFloat32_4x4()
        check( "toFloat32_4x4 roundtrip", rt == d32 )

        Console.println( "  toString: " + id.toString() )
    }

    static fun()
    {
        testFloat32()
        testFloat64()
        testFloat16()
    }
}
