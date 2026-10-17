@Nickname("Matrix3x3")
@Nickname("Mat3")
@Nickname("float3x3")
public class Float32_3x3
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # 高消耗运算（乘/加/转置/行列式/求逆/变换/工厂）下沉 cvm：
    # SystemMathMat3*（systemCalls -> math_lib.dll 的 mathvm_mat3f_*），
    # C 侧直接按 member_data 读写字段内存；结果经 out 参数写回
    public Float32 m00 = 0.0f
    public Float32 m01 = 0.0f
    public Float32 m02 = 0.0f
    public Float32 m10 = 0.0f
    public Float32 m11 = 0.0f
    public Float32 m12 = 0.0f
    public Float32 m20 = 0.0f
    public Float32 m21 = 0.0f
    public Float32 m22 = 0.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.m00 = 0.0f
        this.m01 = 0.0f
        this.m02 = 0.0f
        this.m10 = 0.0f
        this.m11 = 0.0f
        this.m12 = 0.0f
        this.m20 = 0.0f
        this.m21 = 0.0f
        this.m22 = 0.0f
    }

    public void _init_( Float32 v00, Float32 v01, Float32 v02,
                        Float32 v10, Float32 v11, Float32 v12,
                        Float32 v20, Float32 v21, Float32 v22 )
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

    public void _init_( Array<Float32> values )
    {
        int i = 0
        while i < 9
        {
            this._setItem_( i, values[i] )
            i++
        }
    }

    # ── 索引访问 ─────────────────────────────────────────
    override Float32 _getItem_( int index )
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
        ret 0.0f
    }

    override void _setItem_( int index, Float32 value )
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
    Float32 getValue( int row, int col )
    {
        ret this._getItem_( row * 3 + col )
    }

    void setValue( int row, int col, Float32 value )
    {
        this._setItem_( row * 3 + col, value )
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Float32_3x3 _mul_( Float32_3x3 b )
    {
        ret this.multiply( b )
    }

    override Float32_3x3 _add_( Float32_3x3 b )
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3Add( this, b, r )
        ret r
    }

    override bool _eq_( Float32_3x3 b )
    {
        ret SystemMathMat3Eq( this, b )
    }

    override bool _ne_( Float32_3x3 b )
    {
        ret !this._eq_( b )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    Float32_3x3 multiply( Float32_3x3 b )
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3Mul( this, b, r )
        ret r
    }

    Float32_3 transform( Float32_3 v )
    {
        Float32_3 r = Float32_3()
        SystemMathMat3MulVec( this, v, r )
        ret r
    }

    Float32_3x3 transpose()
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3Transpose( this, r )
        ret r
    }

    Float32 determinant()
    {
        ret SystemMathMat3Determinant( this )
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float32_3x3 inverse()
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3Inverse( this, r )
        ret r
    }

    Float32_3x3 clone()
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3Copy( this, r )
        ret r
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Float32_3x3 identity()
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3Identity( r )
        ret r
    }

    public static get Float32_3x3 zero()
    {
        ret Float32_3x3()
    }

    # 绕 X 轴旋转（弧度）
    public static Float32_3x3 rotationX( Float32 radians )
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3RotationX( radians, r )
        ret r
    }

    # 绕 Y 轴旋转（弧度）
    public static Float32_3x3 rotationY( Float32 radians )
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3RotationY( radians, r )
        ret r
    }

    # 绕 Z 轴旋转（弧度）
    public static Float32_3x3 rotationZ( Float32 radians )
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3RotationZ( radians, r )
        ret r
    }

    public static Float32_3x3 scale( Float32 sx, Float32 sy )
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3Scale( sx, sy, r )
        ret r
    }

    public static Float32_3x3 translation( Float32 tx, Float32 ty )
    {
        Float32_3x3 r = Float32_3x3()
        SystemMathMat3Translation( tx, ty, r )
        ret r
    }

    override string toString()
    {
        ret String.toFormat( "Float32_3x3[{0},{1},{2} | {3},{4},{5} | {6},{7},{8}]",
            this.m00, this.m01, this.m02,
            this.m10, this.m11, this.m12,
            this.m20, this.m21, this.m22 )
    }
}
