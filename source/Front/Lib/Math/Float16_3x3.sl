@Nickname("Matrix3x3h")
@Nickname("Mat3h")
@Nickname("half3x3")
public class Float16_3x3
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # half 无原生 FFI 实现，元素级运算经 toFloat32() 中转走 Mathf.dot3（FFI），
    # 结果隐式收敛回 Float16（见 Mathh.sl 同款模式）
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
    override Float16_3x3 _mul_( Object obj1 )
    {
        if obj1 is Float16_3x3 b
        {
            ret this.multiply( b )
        }
        ret this
    }

    override Float16_3x3 _add_( Object obj1 )
    {
        if obj1 is Float16_3x3 b
        {
            Float16_3x3 r = Float16_3x3()
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
        if obj1 is Float16_3x3 b
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
    Float16_3x3 multiply( Float16_3x3 b )
    {
        Float16_3x3 r = Float16_3x3()
        r.m00 = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), b.m00.toFloat32(), b.m10.toFloat32(), b.m20.toFloat32() )
        r.m01 = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), b.m01.toFloat32(), b.m11.toFloat32(), b.m21.toFloat32() )
        r.m02 = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), b.m02.toFloat32(), b.m12.toFloat32(), b.m22.toFloat32() )
        r.m10 = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), b.m00.toFloat32(), b.m10.toFloat32(), b.m20.toFloat32() )
        r.m11 = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), b.m01.toFloat32(), b.m11.toFloat32(), b.m21.toFloat32() )
        r.m12 = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), b.m02.toFloat32(), b.m12.toFloat32(), b.m22.toFloat32() )
        r.m20 = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), b.m00.toFloat32(), b.m10.toFloat32(), b.m20.toFloat32() )
        r.m21 = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), b.m01.toFloat32(), b.m11.toFloat32(), b.m21.toFloat32() )
        r.m22 = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), b.m02.toFloat32(), b.m12.toFloat32(), b.m22.toFloat32() )
        ret r
    }

    Float16_3 transform( Float16_3 v )
    {
        Float16 nx = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() )
        Float16 ny = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() )
        Float16 nz = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() )
        ret Float16_3( nx, ny, nz )
    }

    Float16_3x3 transpose()
    {
        Float16_3x3 r = Float16_3x3()
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

    Float16 determinant()
    {
        ret this.m00 * ( this.m11 * this.m22 - this.m12 * this.m21 ) - this.m01 * ( this.m10 * this.m22 - this.m12 * this.m20 ) + this.m02 * ( this.m10 * this.m21 - this.m11 * this.m20 )
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float16_3x3 inverse()
    {
        Float16 det = this.determinant()
        if det == 0.0h
        {
            ret Float16_3x3()
        }
        Float16 inv = 1.0h / det

        Float16 a = this.m00
        Float16 b = this.m01
        Float16 c = this.m02
        Float16 d = this.m10
        Float16 e = this.m11
        Float16 f = this.m12
        Float16 g = this.m20
        Float16 h = this.m21
        Float16 i = this.m22

        Float16_3x3 r = Float16_3x3()
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

    Float16_3x3 clone()
    {
        Float16_3x3 r = Float16_3x3()
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
        ret Float16_3x3( 1.0h, 0.0h, 0.0h,
                         0.0h, 1.0h, 0.0h,
                         0.0h, 0.0h, 1.0h )
    }

    public static get Float16_3x3 zero()
    {
        ret Float16_3x3()
    }

    # 绕 X 轴旋转（弧度）
    public static Float16_3x3 rotationX( Float16 radians )
    {
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        ret Float16_3x3( 1.0h, 0.0h, 0.0h,
                         0.0h, c, 0.0h - s,
                         0.0h, s, c )
    }

    # 绕 Y 轴旋转（弧度）
    public static Float16_3x3 rotationY( Float16 radians )
    {
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        ret Float16_3x3( c, 0.0h, s,
                         0.0h, 1.0h, 0.0h,
                         0.0h - s, 0.0h, c )
    }

    # 绕 Z 轴旋转（弧度）
    public static Float16_3x3 rotationZ( Float16 radians )
    {
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        ret Float16_3x3( c, 0.0h - s, 0.0h,
                         s, c, 0.0h,
                         0.0h, 0.0h, 1.0h )
    }

    public static Float16_3x3 scale( Float16 sx, Float16 sy )
    {
        ret Float16_3x3( sx, 0.0h, 0.0h,
                         0.0h, sy, 0.0h,
                         0.0h, 0.0h, 1.0h )
    }

    public static Float16_3x3 translation( Float16 tx, Float16 ty )
    {
        ret Float16_3x3( 1.0h, 0.0h, tx,
                         0.0h, 1.0h, ty,
                         0.0h, 0.0h, 1.0h )
    }

    override string toString()
    {
        ret String.toFormat( "Float16_3x3[{0},{1},{2} | {3},{4},{5} | {6},{7},{8}]",
            this.m00, this.m01, this.m02,
            this.m10, this.m11, this.m12,
            this.m20, this.m21, this.m22 )
    }
}
