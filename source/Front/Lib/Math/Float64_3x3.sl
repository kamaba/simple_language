@Nickname("Matrix3x3d")
@Nickname("Mat3d")
@Nickname("double3x3")
public class Float64_3x3
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # 高消耗运算（乘/加/转置/行列式/求逆/变换/工厂）下沉 cvm：
    # SystemMathMat3d*（systemCalls -> math_lib.dll 的 mathvm_mat3d_*），
    # C 侧直接按 member_data 读写字段内存；结果经 out 参数写回
    public Float64 m00 = 0.0d
    public Float64 m01 = 0.0d
    public Float64 m02 = 0.0d
    public Float64 m10 = 0.0d
    public Float64 m11 = 0.0d
    public Float64 m12 = 0.0d
    public Float64 m20 = 0.0d
    public Float64 m21 = 0.0d
    public Float64 m22 = 0.0d

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.m00 = 0.0d
        this.m01 = 0.0d
        this.m02 = 0.0d
        this.m10 = 0.0d
        this.m11 = 0.0d
        this.m12 = 0.0d
        this.m20 = 0.0d
        this.m21 = 0.0d
        this.m22 = 0.0d
    }

    public void _init_( Float64 v00, Float64 v01, Float64 v02,
                        Float64 v10, Float64 v11, Float64 v12,
                        Float64 v20, Float64 v21, Float64 v22 )
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

    public void _init_( Array<Float64> values )
    {
        int i = 0
        while i < 9
        {
            this._setItem_( i, values[i] )
            i++
        }
    }

    # 由 Float32_3x3 提升（扩精度）
    public void _init_( Float32_3x3 m )
    {
        this.m00 = m.m00.toFloat64()
        this.m01 = m.m01.toFloat64()
        this.m02 = m.m02.toFloat64()
        this.m10 = m.m10.toFloat64()
        this.m11 = m.m11.toFloat64()
        this.m12 = m.m12.toFloat64()
        this.m20 = m.m20.toFloat64()
        this.m21 = m.m21.toFloat64()
        this.m22 = m.m22.toFloat64()
    }

    # ── 索引访问 ─────────────────────────────────────────
    override Float64 _getItem_( int index )
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
        ret 0.0d
    }

    override void _setItem_( int index, Float64 value )
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
    Float64 getValue( int row, int col )
    {
        ret this._getItem_( row * 3 + col )
    }

    void setValue( int row, int col, Float64 value )
    {
        this._setItem_( row * 3 + col, value )
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Float64_3x3 _mul_( Float64_3x3 b )
    {
        ret this.multiply( b )
    }

    override Float64_3x3 _add_( Float64_3x3 b )
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dAdd( this, b, r )
        ret r
    }

    override bool _eq_( Float64_3x3 b )
    {
        ret SystemMathMat3dEq( this, b )
    }

    override bool _ne_( Float64_3x3 b )
    {
        ret !this._eq_( b )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    Float64_3x3 multiply( Float64_3x3 b )
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dMul( this, b, r )
        ret r
    }

    Float64_3 transform( Float64_3 v )
    {
        Float64_3 r = Float64_3()
        SystemMathMat3dMulVec( this, v, r )
        ret r
    }

    Float64_3x3 transpose()
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dTranspose( this, r )
        ret r
    }

    Float64 determinant()
    {
        ret SystemMathMat3dDeterminant( this )
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float64_3x3 inverse()
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dInverse( this, r )
        ret r
    }

    Float64_3x3 clone()
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dCopy( this, r )
        ret r
    }

    # 降精度到 Float32_3x3
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
    public static get Float64_3x3 identity()
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dIdentity( r )
        ret r
    }

    public static get Float64_3x3 zero()
    {
        ret Float64_3x3()
    }

    # 绕 X 轴旋转（弧度）
    public static Float64_3x3 rotationX( Float64 radians )
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dRotationX( radians, r )
        ret r
    }

    # 绕 Y 轴旋转（弧度）
    public static Float64_3x3 rotationY( Float64 radians )
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dRotationY( radians, r )
        ret r
    }

    # 绕 Z 轴旋转（弧度）
    public static Float64_3x3 rotationZ( Float64 radians )
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dRotationZ( radians, r )
        ret r
    }

    public static Float64_3x3 scale( Float64 sx, Float64 sy )
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dScale( sx, sy, r )
        ret r
    }

    public static Float64_3x3 translation( Float64 tx, Float64 ty )
    {
        Float64_3x3 r = Float64_3x3()
        SystemMathMat3dTranslation( tx, ty, r )
        ret r
    }

    override string toString()
    {
        ret String.toFormat( "Float64_3x3[{0},{1},{2} | {3},{4},{5} | {6},{7},{8}]",
            this.m00, this.m01, this.m02,
            this.m10, this.m11, this.m12,
            this.m20, this.m21, this.m22 )
    }
}
