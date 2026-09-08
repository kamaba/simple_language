@Nickname("Matrix4x4d")
@Nickname("Mat4d")
@Nickname("double4x4")
public class Float64_4x4
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # 元素级运算经 Mathd.dot3 走 FFI（math_lib.dll）加速
    public Float64 m00 = 0.0d
    public Float64 m01 = 0.0d
    public Float64 m02 = 0.0d
    public Float64 m03 = 0.0d
    public Float64 m10 = 0.0d
    public Float64 m11 = 0.0d
    public Float64 m12 = 0.0d
    public Float64 m13 = 0.0d
    public Float64 m20 = 0.0d
    public Float64 m21 = 0.0d
    public Float64 m22 = 0.0d
    public Float64 m23 = 0.0d
    public Float64 m30 = 0.0d
    public Float64 m31 = 0.0d
    public Float64 m32 = 0.0d
    public Float64 m33 = 0.0d

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.m00 = 0.0d
        this.m01 = 0.0d
        this.m02 = 0.0d
        this.m03 = 0.0d
        this.m10 = 0.0d
        this.m11 = 0.0d
        this.m12 = 0.0d
        this.m13 = 0.0d
        this.m20 = 0.0d
        this.m21 = 0.0d
        this.m22 = 0.0d
        this.m23 = 0.0d
        this.m30 = 0.0d
        this.m31 = 0.0d
        this.m32 = 0.0d
        this.m33 = 0.0d
    }

    public void _init_( Float64 v00, Float64 v01, Float64 v02, Float64 v03,
                        Float64 v10, Float64 v11, Float64 v12, Float64 v13,
                        Float64 v20, Float64 v21, Float64 v22, Float64 v23,
                        Float64 v30, Float64 v31, Float64 v32, Float64 v33 )
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

    public void _init_( Array<Float64> values )
    {
        int i = 0
        while i < 16
        {
            this._setItem_( i, values[i] )
            i++
        }
    }

    public void _init_( Float64_3x3 m )
    {
        this.m00 = m.m00
        this.m01 = m.m01
        this.m02 = m.m02
        this.m03 = 0.0d
        this.m10 = m.m10
        this.m11 = m.m11
        this.m12 = m.m12
        this.m13 = 0.0d
        this.m20 = m.m20
        this.m21 = m.m21
        this.m22 = m.m22
        this.m23 = 0.0d
        this.m30 = 0.0d
        this.m31 = 0.0d
        this.m32 = 0.0d
        this.m33 = 1.0d
    }

    public void _init_( Float32_4x4 m )
    {
        this.m00 = m.m00.toFloat64()
        this.m01 = m.m01.toFloat64()
        this.m02 = m.m02.toFloat64()
        this.m03 = m.m03.toFloat64()
        this.m10 = m.m10.toFloat64()
        this.m11 = m.m11.toFloat64()
        this.m12 = m.m12.toFloat64()
        this.m13 = m.m13.toFloat64()
        this.m20 = m.m20.toFloat64()
        this.m21 = m.m21.toFloat64()
        this.m22 = m.m22.toFloat64()
        this.m23 = m.m23.toFloat64()
        this.m30 = m.m30.toFloat64()
        this.m31 = m.m31.toFloat64()
        this.m32 = m.m32.toFloat64()
        this.m33 = m.m33.toFloat64()
    }

    # ── 索引访问 ─────────────────────────────────────────
    override Float64 _getItem_( int index )
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
        ret 0.0d
    }

    override void _setItem_( int index, Float64 value )
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
    Float64 getValue( int row, int col )
    {
        ret this._getItem_( row * 4 + col )
    }

    void setValue( int row, int col, Float64 value )
    {
        this._setItem_( row * 4 + col, value )
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Float64_4x4 _mul_( Object obj1 )
    {
        if obj1 is Float64_4x4 b
        {
            ret this.multiply( b )
        }
        ret this
    }

    override Float64_4x4 _add_( Object obj1 )
    {
        if obj1 is Float64_4x4 b
        {
            Float64_4x4 r = Float64_4x4()
            r.m00 = this.m00 + b.m00
            r.m01 = this.m01 + b.m01
            r.m02 = this.m02 + b.m02
            r.m03 = this.m03 + b.m03
            r.m10 = this.m10 + b.m10
            r.m11 = this.m11 + b.m11
            r.m12 = this.m12 + b.m12
            r.m13 = this.m13 + b.m13
            r.m20 = this.m20 + b.m20
            r.m21 = this.m21 + b.m21
            r.m22 = this.m22 + b.m22
            r.m23 = this.m23 + b.m23
            r.m30 = this.m30 + b.m30
            r.m31 = this.m31 + b.m31
            r.m32 = this.m32 + b.m32
            r.m33 = this.m33 + b.m33
            ret r
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float64_4x4 b
        {
            if ( this.m00 != b.m00 ) { ret false }
            if ( this.m01 != b.m01 ) { ret false }
            if ( this.m02 != b.m02 ) { ret false }
            if ( this.m03 != b.m03 ) { ret false }
            if ( this.m10 != b.m10 ) { ret false }
            if ( this.m11 != b.m11 ) { ret false }
            if ( this.m12 != b.m12 ) { ret false }
            if ( this.m13 != b.m13 ) { ret false }
            if ( this.m20 != b.m20 ) { ret false }
            if ( this.m21 != b.m21 ) { ret false }
            if ( this.m22 != b.m22 ) { ret false }
            if ( this.m23 != b.m23 ) { ret false }
            if ( this.m30 != b.m30 ) { ret false }
            if ( this.m31 != b.m31 ) { ret false }
            if ( this.m32 != b.m32 ) { ret false }
            if ( this.m33 != b.m33 ) { ret false }
            ret true
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    # 前 3 项经 Mathd.dot3 走 FFI，第 4 项（w 列）在 SL 层补齐
    Float64_4x4 multiply( Float64_4x4 b )
    {
        Float64_4x4 r = Float64_4x4()
        r.m00 = Mathd.dot3( this.m00, this.m01, this.m02, b.m00, b.m10, b.m20 ) + this.m03 * b.m30
        r.m01 = Mathd.dot3( this.m00, this.m01, this.m02, b.m01, b.m11, b.m21 ) + this.m03 * b.m31
        r.m02 = Mathd.dot3( this.m00, this.m01, this.m02, b.m02, b.m12, b.m22 ) + this.m03 * b.m32
        r.m03 = Mathd.dot3( this.m00, this.m01, this.m02, b.m03, b.m13, b.m23 ) + this.m03 * b.m33
        r.m10 = Mathd.dot3( this.m10, this.m11, this.m12, b.m00, b.m10, b.m20 ) + this.m13 * b.m30
        r.m11 = Mathd.dot3( this.m10, this.m11, this.m12, b.m01, b.m11, b.m21 ) + this.m13 * b.m31
        r.m12 = Mathd.dot3( this.m10, this.m11, this.m12, b.m02, b.m12, b.m22 ) + this.m13 * b.m32
        r.m13 = Mathd.dot3( this.m10, this.m11, this.m12, b.m03, b.m13, b.m23 ) + this.m13 * b.m33
        r.m20 = Mathd.dot3( this.m20, this.m21, this.m22, b.m00, b.m10, b.m20 ) + this.m23 * b.m30
        r.m21 = Mathd.dot3( this.m20, this.m21, this.m22, b.m01, b.m11, b.m21 ) + this.m23 * b.m31
        r.m22 = Mathd.dot3( this.m20, this.m21, this.m22, b.m02, b.m12, b.m22 ) + this.m23 * b.m32
        r.m23 = Mathd.dot3( this.m20, this.m21, this.m22, b.m03, b.m13, b.m23 ) + this.m23 * b.m33
        r.m30 = Mathd.dot3( this.m30, this.m31, this.m32, b.m00, b.m10, b.m20 ) + this.m33 * b.m30
        r.m31 = Mathd.dot3( this.m30, this.m31, this.m32, b.m01, b.m11, b.m21 ) + this.m33 * b.m31
        r.m32 = Mathd.dot3( this.m30, this.m31, this.m32, b.m02, b.m12, b.m22 ) + this.m33 * b.m32
        r.m33 = Mathd.dot3( this.m30, this.m31, this.m32, b.m03, b.m13, b.m23 ) + this.m33 * b.m33
        ret r
    }

    # 变换点（w 补 1，带平移）
    Float64_3 transformPoint( Float64_3 v )
    {
        Float64 nx = Mathd.dot3( this.m00, this.m01, this.m02, v.x, v.y, v.z ) + this.m03
        Float64 ny = Mathd.dot3( this.m10, this.m11, this.m12, v.x, v.y, v.z ) + this.m13
        Float64 nz = Mathd.dot3( this.m20, this.m21, this.m22, v.x, v.y, v.z ) + this.m23
        ret Float64_3( nx, ny, nz )
    }

    # 变换方向（w 补 0，忽略平移）
    Float64_3 transformDirection( Float64_3 v )
    {
        Float64 nx = Mathd.dot3( this.m00, this.m01, this.m02, v.x, v.y, v.z )
        Float64 ny = Mathd.dot3( this.m10, this.m11, this.m12, v.x, v.y, v.z )
        Float64 nz = Mathd.dot3( this.m20, this.m21, this.m22, v.x, v.y, v.z )
        ret Float64_3( nx, ny, nz )
    }

    Float64_4x4 transpose()
    {
        ret Float64_4x4( this.m00, this.m10, this.m20, this.m30,
                         this.m01, this.m11, this.m21, this.m31,
                         this.m02, this.m12, this.m22, this.m32,
                         this.m03, this.m13, this.m23, this.m33 )
    }

    Float64 determinant()
    {
        Float64 b00 = this.m00 * this.m11 - this.m01 * this.m10
        Float64 b01 = this.m00 * this.m12 - this.m02 * this.m10
        Float64 b02 = this.m00 * this.m13 - this.m03 * this.m10
        Float64 b03 = this.m01 * this.m12 - this.m02 * this.m11
        Float64 b04 = this.m01 * this.m13 - this.m03 * this.m11
        Float64 b05 = this.m02 * this.m13 - this.m03 * this.m12
        Float64 b06 = this.m20 * this.m31 - this.m21 * this.m30
        Float64 b07 = this.m20 * this.m32 - this.m22 * this.m30
        Float64 b08 = this.m20 * this.m33 - this.m23 * this.m30
        Float64 b09 = this.m21 * this.m32 - this.m22 * this.m31
        Float64 b10 = this.m21 * this.m33 - this.m23 * this.m31
        Float64 b11 = this.m22 * this.m33 - this.m23 * this.m32
        ret b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float64_4x4 inverse()
    {
        Float64 det = this.determinant()
        if det == 0.0d
        {
            ret Float64_4x4()
        }
        Float64 inv = 1.0d / det

        Float64 b00 = this.m00 * this.m11 - this.m01 * this.m10
        Float64 b01 = this.m00 * this.m12 - this.m02 * this.m10
        Float64 b02 = this.m00 * this.m13 - this.m03 * this.m10
        Float64 b03 = this.m01 * this.m12 - this.m02 * this.m11
        Float64 b04 = this.m01 * this.m13 - this.m03 * this.m11
        Float64 b05 = this.m02 * this.m13 - this.m03 * this.m12
        Float64 b06 = this.m20 * this.m31 - this.m21 * this.m30
        Float64 b07 = this.m20 * this.m32 - this.m22 * this.m30
        Float64 b08 = this.m20 * this.m33 - this.m23 * this.m30
        Float64 b09 = this.m21 * this.m32 - this.m22 * this.m31
        Float64 b10 = this.m21 * this.m33 - this.m23 * this.m31
        Float64 b11 = this.m22 * this.m33 - this.m23 * this.m32

        Float64_4x4 r = Float64_4x4()
        r.m00 = ( this.m11 * b11 - this.m12 * b10 + this.m13 * b09 ) * inv
        r.m01 = ( this.m02 * b10 - this.m01 * b11 - this.m03 * b09 ) * inv
        r.m02 = ( this.m31 * b05 - this.m32 * b04 + this.m33 * b03 ) * inv
        r.m03 = ( this.m22 * b04 - this.m23 * b05 - this.m21 * b03 ) * inv
        r.m10 = ( this.m12 * b08 - this.m10 * b11 - this.m13 * b07 ) * inv
        r.m11 = ( this.m00 * b11 - this.m02 * b08 + this.m03 * b07 ) * inv
        r.m12 = ( this.m32 * b02 - this.m30 * b05 - this.m33 * b01 ) * inv
        r.m13 = ( this.m20 * b05 - this.m22 * b02 + this.m23 * b01 ) * inv
        r.m20 = ( this.m10 * b10 - this.m11 * b08 + this.m13 * b06 ) * inv
        r.m21 = ( this.m01 * b08 - this.m00 * b10 - this.m03 * b06 ) * inv
        r.m22 = ( this.m30 * b04 - this.m31 * b02 + this.m33 * b00 ) * inv
        r.m23 = ( this.m21 * b02 - this.m20 * b04 - this.m23 * b00 ) * inv
        r.m30 = ( this.m11 * b07 - this.m10 * b09 - this.m12 * b06 ) * inv
        r.m31 = ( this.m00 * b09 - this.m01 * b07 + this.m02 * b06 ) * inv
        r.m32 = ( this.m31 * b01 - this.m30 * b03 - this.m32 * b00 ) * inv
        r.m33 = ( this.m20 * b03 - this.m21 * b01 + this.m22 * b00 ) * inv
        ret r
    }

    Float64_4x4 clone()
    {
        ret Float64_4x4( this.m00, this.m01, this.m02, this.m03,
                         this.m10, this.m11, this.m12, this.m13,
                         this.m20, this.m21, this.m22, this.m23,
                         this.m30, this.m31, this.m32, this.m33 )
    }

    # 降维到 2D 仿射：x/y 基取前两列，平移分量（第四列 x/y）落入第三列，底行取 (0,0,1)
    Float64_3x3 toFloat64_3x3()
    {
        ret Float64_3x3( this.m00, this.m01, this.m03,
                        this.m10, this.m11, this.m13,
                        0.0d,    0.0d,    1.0d )
    }

    # 收敛到 Float32_4x4
    Float32_4x4 toFloat32_4x4()
    {
        ret Float32_4x4( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), this.m03.toFloat32(),
                         this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), this.m13.toFloat32(),
                         this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), this.m23.toFloat32(),
                         this.m30.toFloat32(), this.m31.toFloat32(), this.m32.toFloat32(), this.m33.toFloat32() )
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Float64_4x4 identity()
    {
        ret Float64_4x4( 1.0d, 0.0d, 0.0d, 0.0d,
                         0.0d, 1.0d, 0.0d, 0.0d,
                         0.0d, 0.0d, 1.0d, 0.0d,
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    public static get Float64_4x4 zero()
    {
        ret Float64_4x4()
    }

    public static Float64_4x4 translation( Float64 x, Float64 y, Float64 z )
    {
        ret Float64_4x4( 1.0d, 0.0d, 0.0d, x,
                         0.0d, 1.0d, 0.0d, y,
                         0.0d, 0.0d, 1.0d, z,
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    public static Float64_4x4 translation( Float64_3 t )
    {
        ret Float64_4x4.translation( t.x, t.y, t.z )
    }

    public static Float64_4x4 scale( Float64 x, Float64 y, Float64 z )
    {
        ret Float64_4x4( x, 0.0d, 0.0d, 0.0d,
                         0.0d, y, 0.0d, 0.0d,
                         0.0d, 0.0d, z, 0.0d,
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    public static Float64_4x4 scale( Float64_3 s )
    {
        ret Float64_4x4.scale( s.x, s.y, s.z )
    }

    public static Float64_4x4 scale( Float64 s )
    {
        ret Float64_4x4.scale( s, s, s )
    }

    # 绕 X 轴旋转（弧度）
    public static Float64_4x4 rotationX( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        ret Float64_4x4( 1.0d, 0.0d, 0.0d, 0.0d,
                         0.0d, c, 0.0d - s, 0.0d,
                         0.0d, s, c, 0.0d,
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    # 绕 Y 轴旋转（弧度）
    public static Float64_4x4 rotationY( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        ret Float64_4x4( c, 0.0d, s, 0.0d,
                         0.0d, 1.0d, 0.0d, 0.0d,
                         0.0d - s, 0.0d, c, 0.0d,
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    # 绕 Z 轴旋转（弧度）
    public static Float64_4x4 rotationZ( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        ret Float64_4x4( c, 0.0d - s, 0.0d, 0.0d,
                         s, c, 0.0d, 0.0d,
                         0.0d, 0.0d, 1.0d, 0.0d,
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    # 绕任意轴旋转（弧度，axis 需为单位向量）
    public static Float64_4x4 rotationAxis( Float64_3 axis, Float64 radians )
    {
        Float64_3 a = axis.normalize()
        Float64 x = a.x
        Float64 y = a.y
        Float64 z = a.z
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        Float64 t = 1.0d - c
        ret Float64_4x4( t * x * x + c, t * x * y - s * z, t * x * z + s * y, 0.0d,
                         t * x * y + s * z, t * y * y + c, t * y * z - s * x, 0.0d,
                         t * x * z - s * y, t * y * z + s * x, t * z * z + c, 0.0d,
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    # 局部 TRS 组合：translation * rotation * scale
    public static Float64_4x4 trs( Float64_3 translation, Float64_3 rotationEuler, Float64_3 scale )
    {
        Float64_4x4 t = Float64_4x4.translation( translation )
        Float64_4x4 rx = Float64_4x4.rotationX( rotationEuler.x )
        Float64_4x4 ry = Float64_4x4.rotationY( rotationEuler.y )
        Float64_4x4 rz = Float64_4x4.rotationZ( rotationEuler.z )
        Float64_4x4 s = Float64_4x4.scale( scale )
        ret t.multiply( ry ).multiply( rx ).multiply( rz ).multiply( s )
    }

    # 透视投影（右手系，depth 映射到 [-1,1]）
    public static Float64_4x4 perspective( Float64 fovYRadians, Float64 aspect, Float64 near, Float64 far )
    {
        Float64 f = 1.0d / Mathd.tan( fovYRadians * 0.5d )
        ret Float64_4x4( f / aspect, 0.0d, 0.0d, 0.0d,
                         0.0d, f, 0.0d, 0.0d,
                         0.0d, 0.0d, ( far + near ) / ( near - far ), ( 2.0d * far * near ) / ( near - far ),
                         0.0d, 0.0d, -1.0d, 0.0d )
    }

    # 正交投影
    public static Float64_4x4 ortho( Float64 left, Float64 right, Float64 bottom, Float64 top, Float64 near, Float64 far )
    {
        ret Float64_4x4( 2.0d / ( right - left ), 0.0d, 0.0d, 0.0d - ( right + left ) / ( right - left ),
                         0.0d, 2.0d / ( top - bottom ), 0.0d, 0.0d - ( top + bottom ) / ( top - bottom ),
                         0.0d, 0.0d, 0.0d - 2.0d / ( far - near ), 0.0d - ( far + near ) / ( far - near ),
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    # 视图矩阵（右手系 lookAt）
    public static Float64_4x4 lookAt( Float64_3 eye, Float64_3 target, Float64_3 upHint )
    {
        Float64_3 zAxis = eye._sub_( target ).normalize()
        Float64_3 xAxis = upHint.cross( zAxis ).normalize()
        Float64_3 yAxis = zAxis.cross( xAxis )
        ret Float64_4x4( xAxis.x, xAxis.y, xAxis.z, 0.0d - xAxis.dot( eye ),
                         yAxis.x, yAxis.y, yAxis.z, 0.0d - yAxis.dot( eye ),
                         zAxis.x, zAxis.y, zAxis.z, 0.0d - zAxis.dot( eye ),
                         0.0d, 0.0d, 0.0d, 1.0d )
    }

    override string toString()
    {
        ret String.toFormat(
            "Float64_4x4[{0},{1},{2},{3} | {4},{5},{6},{7} | {8},{9},{10},{11} | {12},{13},{14},{15}]",
            this.m00, this.m01, this.m02, this.m03,
            this.m10, this.m11, this.m12, this.m13,
            this.m20, this.m21, this.m22, this.m23,
            this.m30, this.m31, this.m32, this.m33 )
    }
}
