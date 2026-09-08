@Nickname("Matrix4x4h")
@Nickname("Mat4h")
@Nickname("half4x4")
public class Float16_4x4
{
    # 元素直接以字段存储（m{row}{col}，行主序），避免数组寻址开销；
    # half 无原生 FFI 实现，元素级运算经 toFloat32() 中转走 Mathf.dot3（FFI），
    # 结果隐式收敛回 Float16（见 Float16_3x3 / Mathh.sl 同款模式）
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
    override Float16_4x4 _mul_( Object obj1 )
    {
        if obj1 is Float16_4x4 b
        {
            ret this.multiply( b )
        }
        ret this
    }

    override Float16_4x4 _add_( Object obj1 )
    {
        if obj1 is Float16_4x4 b
        {
            Float16_4x4 r = Float16_4x4()
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
        if obj1 is Float16_4x4 b
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
    # 前 3 项经 Mathf.dot3 走 FFI（toFloat32 中转），第 4 项（w 列）在 SL 层补齐
    Float16_4x4 multiply( Float16_4x4 b )
    {
        Float16_4x4 r = Float16_4x4()
        r.m00 = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), b.m00.toFloat32(), b.m10.toFloat32(), b.m20.toFloat32() ) + this.m03.toFloat32() * b.m30.toFloat32()
        r.m01 = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), b.m01.toFloat32(), b.m11.toFloat32(), b.m21.toFloat32() ) + this.m03.toFloat32() * b.m31.toFloat32()
        r.m02 = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), b.m02.toFloat32(), b.m12.toFloat32(), b.m22.toFloat32() ) + this.m03.toFloat32() * b.m32.toFloat32()
        r.m03 = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), b.m03.toFloat32(), b.m13.toFloat32(), b.m23.toFloat32() ) + this.m03.toFloat32() * b.m33.toFloat32()
        r.m10 = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), b.m00.toFloat32(), b.m10.toFloat32(), b.m20.toFloat32() ) + this.m13.toFloat32() * b.m30.toFloat32()
        r.m11 = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), b.m01.toFloat32(), b.m11.toFloat32(), b.m21.toFloat32() ) + this.m13.toFloat32() * b.m31.toFloat32()
        r.m12 = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), b.m02.toFloat32(), b.m12.toFloat32(), b.m22.toFloat32() ) + this.m13.toFloat32() * b.m32.toFloat32()
        r.m13 = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), b.m03.toFloat32(), b.m13.toFloat32(), b.m23.toFloat32() ) + this.m13.toFloat32() * b.m33.toFloat32()
        r.m20 = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), b.m00.toFloat32(), b.m10.toFloat32(), b.m20.toFloat32() ) + this.m23.toFloat32() * b.m30.toFloat32()
        r.m21 = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), b.m01.toFloat32(), b.m11.toFloat32(), b.m21.toFloat32() ) + this.m23.toFloat32() * b.m31.toFloat32()
        r.m22 = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), b.m02.toFloat32(), b.m12.toFloat32(), b.m22.toFloat32() ) + this.m23.toFloat32() * b.m32.toFloat32()
        r.m23 = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), b.m03.toFloat32(), b.m13.toFloat32(), b.m23.toFloat32() ) + this.m23.toFloat32() * b.m33.toFloat32()
        r.m30 = Mathf.dot3( this.m30.toFloat32(), this.m31.toFloat32(), this.m32.toFloat32(), b.m00.toFloat32(), b.m10.toFloat32(), b.m20.toFloat32() ) + this.m33.toFloat32() * b.m30.toFloat32()
        r.m31 = Mathf.dot3( this.m30.toFloat32(), this.m31.toFloat32(), this.m32.toFloat32(), b.m01.toFloat32(), b.m11.toFloat32(), b.m21.toFloat32() ) + this.m33.toFloat32() * b.m31.toFloat32()
        r.m32 = Mathf.dot3( this.m30.toFloat32(), this.m31.toFloat32(), this.m32.toFloat32(), b.m02.toFloat32(), b.m12.toFloat32(), b.m22.toFloat32() ) + this.m33.toFloat32() * b.m32.toFloat32()
        r.m33 = Mathf.dot3( this.m30.toFloat32(), this.m31.toFloat32(), this.m32.toFloat32(), b.m03.toFloat32(), b.m13.toFloat32(), b.m23.toFloat32() ) + this.m33.toFloat32() * b.m33.toFloat32()
        ret r
    }

    # 变换点（w 补 1，带平移）
    Float16_3 transformPoint( Float16_3 v )
    {
        Float16 nx = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() ) + this.m03.toFloat32()
        Float16 ny = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() ) + this.m13.toFloat32()
        Float16 nz = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() ) + this.m23.toFloat32()
        ret Float16_3( nx, ny, nz )
    }

    # 变换方向（w 补 0，忽略平移）
    Float16_3 transformDirection( Float16_3 v )
    {
        Float16 nx = Mathf.dot3( this.m00.toFloat32(), this.m01.toFloat32(), this.m02.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() )
        Float16 ny = Mathf.dot3( this.m10.toFloat32(), this.m11.toFloat32(), this.m12.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() )
        Float16 nz = Mathf.dot3( this.m20.toFloat32(), this.m21.toFloat32(), this.m22.toFloat32(), v.x.toFloat32(), v.y.toFloat32(), v.z.toFloat32() )
        ret Float16_3( nx, ny, nz )
    }

    Float16_4x4 transpose()
    {
        ret Float16_4x4( this.m00, this.m10, this.m20, this.m30,
                         this.m01, this.m11, this.m21, this.m31,
                         this.m02, this.m12, this.m22, this.m32,
                         this.m03, this.m13, this.m23, this.m33 )
    }

    Float16 determinant()
    {
        Float16 b00 = this.m00 * this.m11 - this.m01 * this.m10
        Float16 b01 = this.m00 * this.m12 - this.m02 * this.m10
        Float16 b02 = this.m00 * this.m13 - this.m03 * this.m10
        Float16 b03 = this.m01 * this.m12 - this.m02 * this.m11
        Float16 b04 = this.m01 * this.m13 - this.m03 * this.m11
        Float16 b05 = this.m02 * this.m13 - this.m03 * this.m12
        Float16 b06 = this.m20 * this.m31 - this.m21 * this.m30
        Float16 b07 = this.m20 * this.m32 - this.m22 * this.m30
        Float16 b08 = this.m20 * this.m33 - this.m23 * this.m30
        Float16 b09 = this.m21 * this.m32 - this.m22 * this.m31
        Float16 b10 = this.m21 * this.m33 - this.m23 * this.m31
        Float16 b11 = this.m22 * this.m33 - this.m23 * this.m32
        ret b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float16_4x4 inverse()
    {
        Float16 det = this.determinant()
        if det == 0.0h
        {
            ret Float16_4x4()
        }
        Float16 inv = 1.0h / det

        Float16 b00 = this.m00 * this.m11 - this.m01 * this.m10
        Float16 b01 = this.m00 * this.m12 - this.m02 * this.m10
        Float16 b02 = this.m00 * this.m13 - this.m03 * this.m10
        Float16 b03 = this.m01 * this.m12 - this.m02 * this.m11
        Float16 b04 = this.m01 * this.m13 - this.m03 * this.m11
        Float16 b05 = this.m02 * this.m13 - this.m03 * this.m12
        Float16 b06 = this.m20 * this.m31 - this.m21 * this.m30
        Float16 b07 = this.m20 * this.m32 - this.m22 * this.m30
        Float16 b08 = this.m20 * this.m33 - this.m23 * this.m30
        Float16 b09 = this.m21 * this.m32 - this.m22 * this.m31
        Float16 b10 = this.m21 * this.m33 - this.m23 * this.m31
        Float16 b11 = this.m22 * this.m33 - this.m23 * this.m32

        Float16_4x4 r = Float16_4x4()
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

    Float16_4x4 clone()
    {
        ret Float16_4x4( this.m00, this.m01, this.m02, this.m03,
                         this.m10, this.m11, this.m12, this.m13,
                         this.m20, this.m21, this.m22, this.m23,
                         this.m30, this.m31, this.m32, this.m33 )
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
        ret Float16_4x4( 1.0h, 0.0h, 0.0h, 0.0h,
                         0.0h, 1.0h, 0.0h, 0.0h,
                         0.0h, 0.0h, 1.0h, 0.0h,
                         0.0h, 0.0h, 0.0h, 1.0h )
    }

    public static get Float16_4x4 zero()
    {
        ret Float16_4x4()
    }

    public static Float16_4x4 translation( Float16 x, Float16 y, Float16 z )
    {
        ret Float16_4x4( 1.0h, 0.0h, 0.0h, x,
                         0.0h, 1.0h, 0.0h, y,
                         0.0h, 0.0h, 1.0h, z,
                         0.0h, 0.0h, 0.0h, 1.0h )
    }

    public static Float16_4x4 translation( Float16_3 t )
    {
        ret Float16_4x4.translation( t.x, t.y, t.z )
    }

    public static Float16_4x4 scale( Float16 x, Float16 y, Float16 z )
    {
        ret Float16_4x4( x, 0.0h, 0.0h, 0.0h,
                         0.0h, y, 0.0h, 0.0h,
                         0.0h, 0.0h, z, 0.0h,
                         0.0h, 0.0h, 0.0h, 1.0h )
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
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        ret Float16_4x4( 1.0h, 0.0h, 0.0h, 0.0h,
                         0.0h, c, 0.0h - s, 0.0h,
                         0.0h, s, c, 0.0h,
                         0.0h, 0.0h, 0.0h, 1.0h )
    }

    # 绕 Y 轴旋转（弧度）
    public static Float16_4x4 rotationY( Float16 radians )
    {
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        ret Float16_4x4( c, 0.0h, s, 0.0h,
                         0.0h, 1.0h, 0.0h, 0.0h,
                         0.0h - s, 0.0h, c, 0.0h,
                         0.0h, 0.0h, 0.0h, 1.0h )
    }

    # 绕 Z 轴旋转（弧度）
    public static Float16_4x4 rotationZ( Float16 radians )
    {
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        ret Float16_4x4( c, 0.0h - s, 0.0h, 0.0h,
                         s, c, 0.0h, 0.0h,
                         0.0h, 0.0h, 1.0h, 0.0h,
                         0.0h, 0.0h, 0.0h, 1.0h )
    }

    # 绕任意轴旋转（弧度，axis 需为单位向量）
    public static Float16_4x4 rotationAxis( Float16_3 axis, Float16 radians )
    {
        Float16_3 a = axis.normalize()
        Float16 x = a.x
        Float16 y = a.y
        Float16 z = a.z
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        Float16 t = 1.0h - c
        ret Float16_4x4( t * x * x + c, t * x * y - s * z, t * x * z + s * y, 0.0h,
                         t * x * y + s * z, t * y * y + c, t * y * z - s * x, 0.0h,
                         t * x * z - s * y, t * y * z + s * x, t * z * z + c, 0.0h,
                         0.0h, 0.0h, 0.0h, 1.0h )
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
        Float16 f = 1.0h / Mathh.tan( fovYRadians * 0.5h )
        ret Float16_4x4( f / aspect, 0.0h, 0.0h, 0.0h,
                         0.0h, f, 0.0h, 0.0h,
                         0.0h, 0.0h, ( far + near ) / ( near - far ), ( 2.0h * far * near ) / ( near - far ),
                         0.0h, 0.0h, -1.0h, 0.0h )
    }

    # 正交投影
    public static Float16_4x4 ortho( Float16 left, Float16 right, Float16 bottom, Float16 top, Float16 near, Float16 far )
    {
        ret Float16_4x4( 2.0h / ( right - left ), 0.0h, 0.0h, 0.0h - ( right + left ) / ( right - left ),
                         0.0h, 2.0h / ( top - bottom ), 0.0h, 0.0h - ( top + bottom ) / ( top - bottom ),
                         0.0h, 0.0h, 0.0h - 2.0h / ( far - near ), 0.0h - ( far + near ) / ( far - near ),
                         0.0h, 0.0h, 0.0h, 1.0h )
    }

    # 视图矩阵（右手系 lookAt）
    public static Float16_4x4 lookAt( Float16_3 eye, Float16_3 target, Float16_3 upHint )
    {
        Float16_3 zAxis = eye._sub_( target ).normalize()
        Float16_3 xAxis = upHint.cross( zAxis ).normalize()
        Float16_3 yAxis = zAxis.cross( xAxis )
        ret Float16_4x4( xAxis.x, xAxis.y, xAxis.z, 0.0h - xAxis.dot( eye ),
                         yAxis.x, yAxis.y, yAxis.z, 0.0h - yAxis.dot( eye ),
                         zAxis.x, zAxis.y, zAxis.z, 0.0h - zAxis.dot( eye ),
                         0.0h, 0.0h, 0.0h, 1.0h )
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
