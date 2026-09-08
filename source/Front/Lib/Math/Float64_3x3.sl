@Nickname("Matrix3x3d")
@Nickname("Mat3d")
@Nickname("double3x3")
public class Float64_3x3
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # 元素级运算经 Mathd.dot3 走 FFI（math_lib.dll）加速
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
    override Float64_3x3 _mul_( Object obj1 )
    {
        if obj1 is Float64_3x3 b
        {
            ret this.multiply( b )
        }
        ret this
    }

    override Float64_3x3 _add_( Object obj1 )
    {
        if obj1 is Float64_3x3 b
        {
            Float64_3x3 r = Float64_3x3()
            r.m00 = this.m00 + b.m00
            r.m01 = this.m01 + b.m01
            r.m02 = this.m02 + b.m02
            r.m10 = this.m10 + b.m10
            r.m11 = this.m11 + b.m11
            r.m12 = this.m12 + b.m12
            r.m20 = this.m20 + b.m20
            r.m21 = this.m21 + b.m21
            r.m22 = this.m22 + b.m22
            ret r
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float64_3x3 b
        {
            if ( this.m00 != b.m00 ) { ret false }
            if ( this.m01 != b.m01 ) { ret false }
            if ( this.m02 != b.m02 ) { ret false }
            if ( this.m10 != b.m10 ) { ret false }
            if ( this.m11 != b.m11 ) { ret false }
            if ( this.m12 != b.m12 ) { ret false }
            if ( this.m20 != b.m20 ) { ret false }
            if ( this.m21 != b.m21 ) { ret false }
            if ( this.m22 != b.m22 ) { ret false }
            ret true
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    Float64_3x3 multiply( Float64_3x3 b )
    {
        Float64_3x3 r = Float64_3x3()
        r.m00 = Mathd.dot3( this.m00, this.m01, this.m02, b.m00, b.m10, b.m20 )
        r.m01 = Mathd.dot3( this.m00, this.m01, this.m02, b.m01, b.m11, b.m21 )
        r.m02 = Mathd.dot3( this.m00, this.m01, this.m02, b.m02, b.m12, b.m22 )
        r.m10 = Mathd.dot3( this.m10, this.m11, this.m12, b.m00, b.m10, b.m20 )
        r.m11 = Mathd.dot3( this.m10, this.m11, this.m12, b.m01, b.m11, b.m21 )
        r.m12 = Mathd.dot3( this.m10, this.m11, this.m12, b.m02, b.m12, b.m22 )
        r.m20 = Mathd.dot3( this.m20, this.m21, this.m22, b.m00, b.m10, b.m20 )
        r.m21 = Mathd.dot3( this.m20, this.m21, this.m22, b.m01, b.m11, b.m21 )
        r.m22 = Mathd.dot3( this.m20, this.m21, this.m22, b.m02, b.m12, b.m22 )
        ret r
    }

    Float64_3 transform( Float64_3 v )
    {
        Float64 nx = Mathd.dot3( this.m00, this.m01, this.m02, v.x, v.y, v.z )
        Float64 ny = Mathd.dot3( this.m10, this.m11, this.m12, v.x, v.y, v.z )
        Float64 nz = Mathd.dot3( this.m20, this.m21, this.m22, v.x, v.y, v.z )
        ret Float64_3( nx, ny, nz )
    }

    Float64_3x3 transpose()
    {
        Float64_3x3 r = Float64_3x3()
        r.m00 = this.m00
        r.m01 = this.m10
        r.m02 = this.m20
        r.m10 = this.m01
        r.m11 = this.m11
        r.m12 = this.m21
        r.m20 = this.m02
        r.m21 = this.m12
        r.m22 = this.m22
        ret r
    }

    Float64 determinant()
    {
        ret this.m00 * ( this.m11 * this.m22 - this.m12 * this.m21 ) - this.m01 * ( this.m10 * this.m22 - this.m12 * this.m20 ) + this.m02 * ( this.m10 * this.m21 - this.m11 * this.m20 )
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float64_3x3 inverse()
    {
        Float64 det = this.determinant()
        if det == 0.0d
        {
            ret Float64_3x3()
        }
        Float64 inv = 1.0d / det

        Float64 a = this.m00
        Float64 b = this.m01
        Float64 c = this.m02
        Float64 d = this.m10
        Float64 e = this.m11
        Float64 f = this.m12
        Float64 g = this.m20
        Float64 h = this.m21
        Float64 i = this.m22

        Float64_3x3 r = Float64_3x3()
        r.m00 = ( e * i - f * h ) * inv
        r.m01 = ( c * h - b * i ) * inv
        r.m02 = ( b * f - c * e ) * inv
        r.m10 = ( f * g - d * i ) * inv
        r.m11 = ( a * i - c * g ) * inv
        r.m12 = ( c * d - a * f ) * inv
        r.m20 = ( d * h - e * g ) * inv
        r.m21 = ( b * g - a * h ) * inv
        r.m22 = ( a * e - b * d ) * inv
        ret r
    }

    Float64_3x3 clone()
    {
        Float64_3x3 r = Float64_3x3()
        r.m00 = this.m00
        r.m01 = this.m01
        r.m02 = this.m02
        r.m10 = this.m10
        r.m11 = this.m11
        r.m12 = this.m12
        r.m20 = this.m20
        r.m21 = this.m21
        r.m22 = this.m22
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
        ret Float64_3x3( 1.0d, 0.0d, 0.0d,
                         0.0d, 1.0d, 0.0d,
                         0.0d, 0.0d, 1.0d )
    }

    public static get Float64_3x3 zero()
    {
        ret Float64_3x3()
    }

    # 绕 X 轴旋转（弧度）
    public static Float64_3x3 rotationX( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        ret Float64_3x3( 1.0d, 0.0d, 0.0d,
                         0.0d, c, 0.0d - s,
                         0.0d, s, c )
    }

    # 绕 Y 轴旋转（弧度）
    public static Float64_3x3 rotationY( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        ret Float64_3x3( c, 0.0d, s,
                         0.0d, 1.0d, 0.0d,
                         0.0d - s, 0.0d, c )
    }

    # 绕 Z 轴旋转（弧度）
    public static Float64_3x3 rotationZ( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        ret Float64_3x3( c, 0.0d - s, 0.0d,
                         s, c, 0.0d,
                         0.0d, 0.0d, 1.0d )
    }

    public static Float64_3x3 scale( Float64 sx, Float64 sy )
    {
        ret Float64_3x3( sx, 0.0d, 0.0d,
                         0.0d, sy, 0.0d,
                         0.0d, 0.0d, 1.0d )
    }

    public static Float64_3x3 translation( Float64 tx, Float64 ty )
    {
        ret Float64_3x3( 1.0d, 0.0d, tx,
                         0.0d, 1.0d, ty,
                         0.0d, 0.0d, 1.0d )
    }

    override string toString()
    {
        ret String.toFormat( "Float64_3x3[{0},{1},{2} | {3},{4},{5} | {6},{7},{8}]",
            this.m00, this.m01, this.m02,
            this.m10, this.m11, this.m12,
            this.m20, this.m21, this.m22 )
    }
}
