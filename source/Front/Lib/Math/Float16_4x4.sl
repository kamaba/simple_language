@Nickname("Matrix4x4h")
@Nickname("Mat4h")
@Nickname("half4x4")
public class Float16_4x4
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # 高消耗运算（乘/加/转置/行列式/求逆/变换/工厂）下沉 cvm：
    # SystemMathMat4h*（systemCalls -> math_lib.dll 的 mathvm_mat4h_*），
    # C 侧直接按 member_data 读写字段内存（half 位模式，f32 中转计算）；
    # 结果经 out 参数写回；标量形参走 Float32（前端自动 F16<->F32 收敛）
    public Float16 m00 = 0.0h
    public Float16 m01 = 0.0h
    public Float16 m02 = 0.0h
    public Float16 m03 = 0.0h
    public Float16 m10 = 0.0h
    public Float16 m11 = 0.0h
    public Float16 m12 = 0.0h
    public Float16 m13 = 0.0h
    public Float16 m20 = 0.0h
    public Float16 m21 = 0.0h
    public Float16 m22 = 0.0h
    public Float16 m23 = 0.0h
    public Float16 m30 = 0.0h
    public Float16 m31 = 0.0h
    public Float16 m32 = 0.0h
    public Float16 m33 = 0.0h

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.m00 = 0.0h
        this.m01 = 0.0h
        this.m02 = 0.0h
        this.m03 = 0.0h
        this.m10 = 0.0h
        this.m11 = 0.0h
        this.m12 = 0.0h
        this.m13 = 0.0h
        this.m20 = 0.0h
        this.m21 = 0.0h
        this.m22 = 0.0h
        this.m23 = 0.0h
        this.m30 = 0.0h
        this.m31 = 0.0h
        this.m32 = 0.0h
        this.m33 = 0.0h
    }

    public void _init_( Float16 v00, Float16 v01, Float16 v02, Float16 v03,
                        Float16 v10, Float16 v11, Float16 v12, Float16 v13,
                        Float16 v20, Float16 v21, Float16 v22, Float16 v23,
                        Float16 v30, Float16 v31, Float16 v32, Float16 v33 )
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

    public void _init_( Array<Float16> values )
    {
        int i = 0
        while i < 16
        {
            this._setItem_( i, values[i] )
            i++
        }
    }

    public void _init_( Float16_3x3 m )
    {
        this.m00 = m.m00
        this.m01 = m.m01
        this.m02 = m.m02
        this.m03 = 0.0h
        this.m10 = m.m10
        this.m11 = m.m11
        this.m12 = m.m12
        this.m13 = 0.0h
        this.m20 = m.m20
        this.m21 = m.m21
        this.m22 = m.m22
        this.m23 = 0.0h
        this.m30 = 0.0h
        this.m31 = 0.0h
        this.m32 = 0.0h
        this.m33 = 1.0h
    }

    # 由 Float32_4x4 降精度（隐式收敛，见 Float16_3._init_(Float32_3) 先例）
    public void _init_( Float32_4x4 m )
    {
        this.m00 = m.m00
        this.m01 = m.m01
        this.m02 = m.m02
        this.m03 = m.m03
        this.m10 = m.m10
        this.m11 = m.m11
        this.m12 = m.m12
        this.m13 = m.m13
        this.m20 = m.m20
        this.m21 = m.m21
        this.m22 = m.m22
        this.m23 = m.m23
        this.m30 = m.m30
        this.m31 = m.m31
        this.m32 = m.m32
        this.m33 = m.m33
    }

    # ── 索引访问 ─────────────────────────────────────────
    override Float16 _getItem_( int index )
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
        ret 0.0h
    }

    override void _setItem_( int index, Float16 value )
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
    Float16 getValue( int row, int col )
    {
        ret this._getItem_( row * 4 + col )
    }

    void setValue( int row, int col, Float16 value )
    {
        this._setItem_( row * 4 + col, value )
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Float16_4x4 _mul_( Float16_4x4 b )
    {
        ret this.multiply( b )
    }

    override Float16_4x4 _add_( Float16_4x4 b )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hAdd( this, b, r )
        ret r
    }

    override bool _eq_( Float16_4x4 b )
    {
        ret SystemMathMat4hEq( this, b )
    }

    override bool _ne_( Float16_4x4 b )
    {
        ret !this._eq_( b )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    Float16_4x4 multiply( Float16_4x4 b )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hMul( this, b, r )
        ret r
    }

    # 变换点（w 补 1，带平移）
    Float16_3 transformPoint( Float16_3 v )
    {
        Float16_3 r = Float16_3()
        SystemMathMat4hTransformPoint( this, v, r )
        ret r
    }

    # 变换方向（w 补 0，忽略平移）
    Float16_3 transformDirection( Float16_3 v )
    {
        Float16_3 r = Float16_3()
        SystemMathMat4hTransformDirection( this, v, r )
        ret r
    }

    Float16_4x4 transpose()
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hTranspose( this, r )
        ret r
    }

    Float16 determinant()
    {
        ret SystemMathMat4hDeterminant( this )
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float16_4x4 inverse()
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hInverse( this, r )
        ret r
    }

    Float16_4x4 clone()
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hCopy( this, r )
        ret r
    }

    # 降维到 2D 仿射：x/y 基取前两列，平移分量（第四列 x/y）落入第三列，底行取 (0,0,1)
    Float16_3x3 toFloat16_3x3()
    {
        ret Float16_3x3( this.m00, this.m01, this.m03,
                        this.m10, this.m11, this.m13,
                        0.0h,    0.0h,    1.0h )
    }

    # 提升到 Float32_4x4
    Float32_4x4 toFloat32_4x4()
    {
        ret Float32_4x4( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), this.m03.toFloat32(),
                         this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), this.m13.toFloat32(),
                         this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), this.m23.toFloat32(),
                         this.m30.toFloat32(), this.m31.toFloat32(), this.m32.toFloat32(), this.m33.toFloat32() )
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Float16_4x4 identity()
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hIdentity( r )
        ret r
    }

    public static get Float16_4x4 zero()
    {
        ret Float16_4x4()
    }

    public static Float16_4x4 translation( Float16 x, Float16 y, Float16 z )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hTranslation( x, y, z, r )
        ret r
    }

    public static Float16_4x4 translation( Float16_3 t )
    {
        ret Float16_4x4.translation( t.x, t.y, t.z )
    }

    public static Float16_4x4 scale( Float16 x, Float16 y, Float16 z )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hScale( x, y, z, r )
        ret r
    }

    public static Float16_4x4 scale( Float16_3 s )
    {
        ret Float16_4x4.scale( s.x, s.y, s.z )
    }

    public static Float16_4x4 scale( Float16 s )
    {
        ret Float16_4x4.scale( s, s, s )
    }

    # 绕 X 轴旋转（弧度）
    public static Float16_4x4 rotationX( Float16 radians )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hRotationX( radians, r )
        ret r
    }

    # 绕 Y 轴旋转（弧度）
    public static Float16_4x4 rotationY( Float16 radians )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hRotationY( radians, r )
        ret r
    }

    # 绕 Z 轴旋转（弧度）
    public static Float16_4x4 rotationZ( Float16 radians )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hRotationZ( radians, r )
        ret r
    }

    # 绕任意轴旋转（弧度，axis 内部会归一化）
    public static Float16_4x4 rotationAxis( Float16_3 axis, Float16 radians )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hRotationAxis( axis, radians, r )
        ret r
    }

    # 局部 TRS 组合：translation * rotation * scale
    public static Float16_4x4 trs( Float16_3 translation, Float16_3 rotationEuler, Float16_3 scale )
    {
        Float16_4x4 t = Float16_4x4.translation( translation )
        Float16_4x4 rx = Float16_4x4.rotationX( rotationEuler.x )
        Float16_4x4 ry = Float16_4x4.rotationY( rotationEuler.y )
        Float16_4x4 rz = Float16_4x4.rotationZ( rotationEuler.z )
        Float16_4x4 s = Float16_4x4.scale( scale )
        ret t.multiply( ry ).multiply( rx ).multiply( rz ).multiply( s )
    }

    # 透视投影（右手系，depth 映射到 [-1,1]）
    public static Float16_4x4 perspective( Float16 fovYRadians, Float16 aspect, Float16 near, Float16 far )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hPerspective( fovYRadians, aspect, near, far, r )
        ret r
    }

    # 正交投影
    public static Float16_4x4 ortho( Float16 left, Float16 right, Float16 bottom, Float16 top, Float16 near, Float16 far )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hOrtho( left, right, bottom, top, near, far, r )
        ret r
    }

    # 视图矩阵（右手系 lookAt）
    public static Float16_4x4 lookAt( Float16_3 eye, Float16_3 target, Float16_3 upHint )
    {
        Float16_4x4 r = Float16_4x4()
        SystemMathMat4hLookAt( eye, target, upHint, r )
        ret r
    }

    override string toString()
    {
        ret String.toFormat(
            "Float16_4x4[{0},{1},{2},{3} | {4},{5},{6},{7} | {8},{9},{10},{11} | {12},{13},{14},{15}]",
            this.m00, this.m01, this.m02, this.m03,
            this.m10, this.m11, this.m12, this.m13,
            this.m20, this.m21, this.m22, this.m23,
            this.m30, this.m31, this.m32, this.m33 )
    }
}
