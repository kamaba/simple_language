@Nickname("Matrix3x3h")
@Nickname("Mat3h")
@Nickname("half3x3")
public class Float16_3x3
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # 高消耗运算（乘/加/转置/行列式/求逆/变换/工厂）下沉 cvm：
    # SystemMathMat3h*（systemCalls -> math_lib.dll 的 mathvm_mat3h_*），
    # C 侧直接按 member_data 读写字段内存（half 位模式，f32 中转计算）；
    # 结果经 out 参数写回；标量形参走 Float32（前端自动 F16<->F32 收敛）
    public Float16 m00 = 0.0h
    public Float16 m01 = 0.0h
    public Float16 m02 = 0.0h
    public Float16 m10 = 0.0h
    public Float16 m11 = 0.0h
    public Float16 m12 = 0.0h
    public Float16 m20 = 0.0h
    public Float16 m21 = 0.0h
    public Float16 m22 = 0.0h

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.m00 = 0.0h
        this.m01 = 0.0h
        this.m02 = 0.0h
        this.m10 = 0.0h
        this.m11 = 0.0h
        this.m12 = 0.0h
        this.m20 = 0.0h
        this.m21 = 0.0h
        this.m22 = 0.0h
    }

    public void _init_( Float16 v00, Float16 v01, Float16 v02,
                        Float16 v10, Float16 v11, Float16 v12,
                        Float16 v20, Float16 v21, Float16 v22 )
    {
        this.m00 = v00
        this.m01 = v01
        this.m02 = v02
        this.m10 = v10
        this.m11 = v11
        this.m12 = v12
        this.m20 = v20
        this.m21 = v21
        this.m22 = v22
    }

    public void _init_( Array<Float16> values )
    {
        int i = 0
        while i < 9
        {
            this._setItem_( i, values[i] )
            i++
        }
    }

    # 由 Float32_3x3 降精度（隐式收敛，见 Float16_3._init_(Float32_3) 先例）
    public void _init_( Float32_3x3 m )
    {
        this.m00 = m.m00
        this.m01 = m.m01
        this.m02 = m.m02
        this.m10 = m.m10
        this.m11 = m.m11
        this.m12 = m.m12
        this.m20 = m.m20
        this.m21 = m.m21
        this.m22 = m.m22
    }

    # ── 索引访问 ─────────────────────────────────────────
    override Float16 _getItem_( int index )
    {
        if ( index == 0 ) { ret this.m00 }
        if ( index == 1 ) { ret this.m01 }
        if ( index == 2 ) { ret this.m02 }
        if ( index == 3 ) { ret this.m10 }
        if ( index == 4 ) { ret this.m11 }
        if ( index == 5 ) { ret this.m12 }
        if ( index == 6 ) { ret this.m20 }
        if ( index == 7 ) { ret this.m21 }
        if ( index == 8 ) { ret this.m22 }
        ret 0.0h
    }

    override void _setItem_( int index, Float16 value )
    {
        if ( index == 0 ) { this.m00 = value }
        if ( index == 1 ) { this.m01 = value }
        if ( index == 2 ) { this.m02 = value }
        if ( index == 3 ) { this.m10 = value }
        if ( index == 4 ) { this.m11 = value }
        if ( index == 5 ) { this.m12 = value }
        if ( index == 6 ) { this.m20 = value }
        if ( index == 7 ) { this.m21 = value }
        if ( index == 8 ) { this.m22 = value }
    }

    # getValue/setValue（get/set 为语言关键字，不可作方法名）
    Float16 getValue( int row, int col )
    {
        ret this._getItem_( row * 3 + col )
    }

    void setValue( int row, int col, Float16 value )
    {
        this._setItem_( row * 3 + col, value )
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Float16_3x3 _mul_( Float16_3x3 b )
    {
        ret this.multiply( b )
    }

    override Float16_3x3 _add_( Float16_3x3 b )
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hAdd( this, b, r )
        ret r
    }

    override bool _eq_( Float16_3x3 b )
    {
        ret SystemMathMat3hEq( this, b )
    }

    override bool _ne_( Float16_3x3 b )
    {
        ret !this._eq_( b )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    Float16_3x3 multiply( Float16_3x3 b )
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hMul( this, b, r )
        ret r
    }

    Float16_3 transform( Float16_3 v )
    {
        Float16_3 r = Float16_3()
        SystemMathMat3hMulVec( this, v, r )
        ret r
    }

    Float16_3x3 transpose()
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hTranspose( this, r )
        ret r
    }

    Float16 determinant()
    {
        ret SystemMathMat3hDeterminant( this )
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float16_3x3 inverse()
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hInverse( this, r )
        ret r
    }

    Float16_3x3 clone()
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hCopy( this, r )
        ret r
    }

    # 提升到 Float32_3x3
    Float32_3x3 toFloat32_3x3()
    {
        Float32_3x3 r = Float32_3x3()
        r.m00 = this.m00.toFloat32()
        r.m01 = this.m01.toFloat32()
        r.m02 = this.m02.toFloat32()
        r.m10 = this.m10.toFloat32()
        r.m11 = this.m11.toFloat32()
        r.m12 = this.m12.toFloat32()
        r.m20 = this.m20.toFloat32()
        r.m21 = this.m21.toFloat32()
        r.m22 = this.m22.toFloat32()
        ret r
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Float16_3x3 identity()
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hIdentity( r )
        ret r
    }

    public static get Float16_3x3 zero()
    {
        ret Float16_3x3()
    }

    # 绕 X 轴旋转（弧度）
    public static Float16_3x3 rotationX( Float16 radians )
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hRotationX( radians, r )
        ret r
    }

    # 绕 Y 轴旋转（弧度）
    public static Float16_3x3 rotationY( Float16 radians )
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hRotationY( radians, r )
        ret r
    }

    # 绕 Z 轴旋转（弧度）
    public static Float16_3x3 rotationZ( Float16 radians )
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hRotationZ( radians, r )
        ret r
    }

    public static Float16_3x3 scale( Float16 sx, Float16 sy )
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hScale( sx, sy, r )
        ret r
    }

    public static Float16_3x3 translation( Float16 tx, Float16 ty )
    {
        Float16_3x3 r = Float16_3x3()
        SystemMathMat3hTranslation( tx, ty, r )
        ret r
    }

    override string toString()
    {
        ret String.toFormat( "Float16_3x3[{0},{1},{2} | {3},{4},{5} | {6},{7},{8}]",
            this.m00, this.m01, this.m02,
            this.m10, this.m11, this.m12,
            this.m20, this.m21, this.m22 )
    }
}
