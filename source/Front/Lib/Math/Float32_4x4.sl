@Nickname("Matrix4x4")
@Nickname("Mat4")
@Nickname("float4x4")
public class Float32_4x4
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # 元素级运算经 Mathf.dot3 走 FFI（math_lib.dll）加速
    public Float32 m00 = 0.0f
    public Float32 m01 = 0.0f
    public Float32 m02 = 0.0f
    public Float32 m03 = 0.0f
    public Float32 m10 = 0.0f
    public Float32 m11 = 0.0f
    public Float32 m12 = 0.0f
    public Float32 m13 = 0.0f
    public Float32 m20 = 0.0f
    public Float32 m21 = 0.0f
    public Float32 m22 = 0.0f
    public Float32 m23 = 0.0f
    public Float32 m30 = 0.0f
    public Float32 m31 = 0.0f
    public Float32 m32 = 0.0f
    public Float32 m33 = 0.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.m00 = 0.0f
        this.m01 = 0.0f
        this.m02 = 0.0f
        this.m03 = 0.0f
        this.m10 = 0.0f
        this.m11 = 0.0f
        this.m12 = 0.0f
        this.m13 = 0.0f
        this.m20 = 0.0f
        this.m21 = 0.0f
        this.m22 = 0.0f
        this.m23 = 0.0f
        this.m30 = 0.0f
        this.m31 = 0.0f
        this.m32 = 0.0f
        this.m33 = 0.0f
    }

    public void _init_( Float32 v00, Float32 v01, Float32 v02, Float32 v03,
                        Float32 v10, Float32 v11, Float32 v12, Float32 v13,
                        Float32 v20, Float32 v21, Float32 v22, Float32 v23,
                        Float32 v30, Float32 v31, Float32 v32, Float32 v33 )
    {
        this.m00 = v00
        this.m01 = v01
        this.m02 = v02
        this.m03 = v03
        this.m10 = v10
        this.m11 = v11
        this.m12 = v12
        this.m13 = v13
        this.m20 = v20
        this.m21 = v21
        this.m22 = v22
        this.m23 = v23
        this.m30 = v30
        this.m31 = v31
        this.m32 = v32
        this.m33 = v33
    }

    public void _init_( Array<Float32> values )
    {
        int i = 0
        while i < 16
        {
            this._setItem_( i, values[i] )
            i++
        }
    }

    public void _init_( Float32_3x3 m )
    {
        this.m00 = m.m00
        this.m01 = m.m01
        this.m02 = m.m02
        this.m03 = 0.0f
        this.m10 = m.m10
        this.m11 = m.m11
        this.m12 = m.m12
        this.m13 = 0.0f
        this.m20 = m.m20
        this.m21 = m.m21
        this.m22 = m.m22
        this.m23 = 0.0f
        this.m30 = 0.0f
        this.m31 = 0.0f
        this.m32 = 0.0f
        this.m33 = 1.0f
    }

    # ── 索引访问 ─────────────────────────────────────────
    override Float32 _getItem_( int index )
    {
        if ( index == 0 ) { ret this.m00 }
        if ( index == 1 ) { ret this.m01 }
        if ( index == 2 ) { ret this.m02 }
        if ( index == 3 ) { ret this.m03 }
        if ( index == 4 ) { ret this.m10 }
        if ( index == 5 ) { ret this.m11 }
        if ( index == 6 ) { ret this.m12 }
        if ( index == 7 ) { ret this.m13 }
        if ( index == 8 ) { ret this.m20 }
        if ( index == 9 ) { ret this.m21 }
        if ( index == 10 ) { ret this.m22 }
        if ( index == 11 ) { ret this.m23 }
        if ( index == 12 ) { ret this.m30 }
        if ( index == 13 ) { ret this.m31 }
        if ( index == 14 ) { ret this.m32 }
        if ( index == 15 ) { ret this.m33 }
        ret 0.0f
    }

    override void _setItem_( int index, Float32 value )
    {
        if ( index == 0 ) { this.m00 = value }
        if ( index == 1 ) { this.m01 = value }
        if ( index == 2 ) { this.m02 = value }
        if ( index == 3 ) { this.m03 = value }
        if ( index == 4 ) { this.m10 = value }
        if ( index == 5 ) { this.m11 = value }
        if ( index == 6 ) { this.m12 = value }
        if ( index == 7 ) { this.m13 = value }
        if ( index == 8 ) { this.m20 = value }
        if ( index == 9 ) { this.m21 = value }
        if ( index == 10 ) { this.m22 = value }
        if ( index == 11 ) { this.m23 = value }
        if ( index == 12 ) { this.m30 = value }
        if ( index == 13 ) { this.m31 = value }
        if ( index == 14 ) { this.m32 = value }
        if ( index == 15 ) { this.m33 = value }
    }

    # getValue/setValue（get/set 为语言关键字，不可作方法名）
    Float32 getValue( int row, int col )
    {
        ret this._getItem_( row * 4 + col )
    }

    void setValue( int row, int col, Float32 value )
    {
        this._setItem_( row * 4 + col, value )
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Float32_4x4 _mul_( Float32_4x4 b )
    {
        ret this.multiply( b )
    }

    override Float32_4x4 _add_( Float32_4x4 b )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Add( this, b, r )
        ret r
    }

    override bool _eq_( Float32_4x4 b )
    {
        ret SystemMathMat4Eq( this, b )
    }

    override bool _ne_( Float32_4x4 b )
    {
        ret !this._eq_( b )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    Float32_4x4 multiply( Float32_4x4 b )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Mul( this, b, r )
        ret r
    }

    # 变换点（w 补 1，带平移）
    Float32_3 transformPoint( Float32_3 v )
    {
        Float32_3 r = Float32_3()
        SystemMathMat4TransformPoint( this, v, r )
        ret r
    }

    # 变换方向（w 补 0，忽略平移）
    Float32_3 transformDirection( Float32_3 v )
    {
        Float32_3 r = Float32_3()
        SystemMathMat4TransformDirection( this, v, r )
        ret r
    }

    Float32_4x4 transpose()
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Transpose( this, r )
        ret r
    }

    Float32 determinant()
    {
        ret SystemMathMat4Determinant( this )
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float32_4x4 inverse()
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Inverse( this, r )
        ret r
    }

    Float32_4x4 clone()
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Copy( this, r )
        ret r
    }

    # 降维到 2D 仿射：x/y 基取前两列，平移分量（第四列 x/y）落入第三列，底行取 (0,0,1)
    Float32_3x3 toFloat32_3x3()
    {
        ret Float32_3x3( this.m00, this.m01, this.m03,
                        this.m10, this.m11, this.m13,
                        0.0f,    0.0f,    1.0f )
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Float32_4x4 identity()
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Identity( r )
        ret r
    }

    public static get Float32_4x4 zero()
    {
        ret Float32_4x4()
    }

    public static Float32_4x4 translation( Float32 x, Float32 y, Float32 z )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Translation( x, y, z, r )
        ret r
    }

    public static Float32_4x4 translation( Float32_3 t )
    {
        ret Float32_4x4.translation( t.x, t.y, t.z )
    }

    public static Float32_4x4 scale( Float32 x, Float32 y, Float32 z )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Scale( x, y, z, r )
        ret r
    }

    public static Float32_4x4 scale( Float32_3 s )
    {
        ret Float32_4x4.scale( s.x, s.y, s.z )
    }

    public static Float32_4x4 scale( Float32 s )
    {
        ret Float32_4x4.scale( s, s, s )
    }

    # 绕 X 轴旋转（弧度）
    public static Float32_4x4 rotationX( Float32 radians )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4RotationX( radians, r )
        ret r
    }

    # 绕 Y 轴旋转（弧度）
    public static Float32_4x4 rotationY( Float32 radians )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4RotationY( radians, r )
        ret r
    }

    # 绕 Z 轴旋转（弧度）
    public static Float32_4x4 rotationZ( Float32 radians )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4RotationZ( radians, r )
        ret r
    }

    # 绕任意轴旋转（弧度，axis 内部会归一化）
    public static Float32_4x4 rotationAxis( Float32_3 axis, Float32 radians )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4RotationAxis( axis, radians, r )
        ret r
    }

    # 局部 TRS 组合：translation * rotation * scale
    public static Float32_4x4 trs( Float32_3 translation, Float32_3 rotationEuler, Float32_3 scale )
    {
        Float32_4x4 t = Float32_4x4.translation( translation )
        Float32_4x4 rx = Float32_4x4.rotationX( rotationEuler.x )
        Float32_4x4 ry = Float32_4x4.rotationY( rotationEuler.y )
        Float32_4x4 rz = Float32_4x4.rotationZ( rotationEuler.z )
        Float32_4x4 s = Float32_4x4.scale( scale )
        ret t.multiply( ry ).multiply( rx ).multiply( rz ).multiply( s )
    }

    # 透视投影（右手系，depth 映射到 [-1,1]）
    public static Float32_4x4 perspective( Float32 fovYRadians, Float32 aspect, Float32 near, Float32 far )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Perspective( fovYRadians, aspect, near, far, r )
        ret r
    }

    # 正交投影
    public static Float32_4x4 ortho( Float32 left, Float32 right, Float32 bottom, Float32 top, Float32 near, Float32 far )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4Ortho( left, right, bottom, top, near, far, r )
        ret r
    }

    # 视图矩阵（右手系 lookAt）
    public static Float32_4x4 lookAt( Float32_3 eye, Float32_3 target, Float32_3 upHint )
    {
        Float32_4x4 r = Float32_4x4()
        SystemMathMat4LookAt( eye, target, upHint, r )
        ret r
    }

    override string toString()
    {
        ret String.toFormat(
            "Float32_4x4[{0},{1},{2},{3} | {4},{5},{6},{7} | {8},{9},{10},{11} | {12},{13},{14},{15}]",
            this.m00, this.m01, this.m02, this.m03,
            this.m10, this.m11, this.m12, this.m13,
            this.m20, this.m21, this.m22, this.m23,
            this.m30, this.m31, this.m32, this.m33 )
    }
}
