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
    override Float32_4x4 _mul_( Object obj1 )
    {
        if obj1 is Float32_4x4 b
        {
            ret this.multiply( b )
        }
        ret this
    }

    override Float32_4x4 _add_( Object obj1 )
    {
        if obj1 is Float32_4x4 b
        {
            Float32_4x4 r = Float32_4x4()
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
        if obj1 is Float32_4x4 b
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
    # 前 3 项经 Mathf.dot3 走 FFI，第 4 项（w 列）在 SL 层补齐
    Float32_4x4 multiply( Float32_4x4 b )
    {
        Float32_4x4 r = Float32_4x4()
        r.m00 = Mathf.dot3( this.m00, this.m01, this.m02, b.m00, b.m10, b.m20 ) + this.m03 * b.m30
        r.m01 = Mathf.dot3( this.m00, this.m01, this.m02, b.m01, b.m11, b.m21 ) + this.m03 * b.m31
        r.m02 = Mathf.dot3( this.m00, this.m01, this.m02, b.m02, b.m12, b.m22 ) + this.m03 * b.m32
        r.m03 = Mathf.dot3( this.m00, this.m01, this.m02, b.m03, b.m13, b.m23 ) + this.m03 * b.m33
        r.m10 = Mathf.dot3( this.m10, this.m11, this.m12, b.m00, b.m10, b.m20 ) + this.m13 * b.m30
        r.m11 = Mathf.dot3( this.m10, this.m11, this.m12, b.m01, b.m11, b.m21 ) + this.m13 * b.m31
        r.m12 = Mathf.dot3( this.m10, this.m11, this.m12, b.m02, b.m12, b.m22 ) + this.m13 * b.m32
        r.m13 = Mathf.dot3( this.m10, this.m11, this.m12, b.m03, b.m13, b.m23 ) + this.m13 * b.m33
        r.m20 = Mathf.dot3( this.m20, this.m21, this.m22, b.m00, b.m10, b.m20 ) + this.m23 * b.m30
        r.m21 = Mathf.dot3( this.m20, this.m21, this.m22, b.m01, b.m11, b.m21 ) + this.m23 * b.m31
        r.m22 = Mathf.dot3( this.m20, this.m21, this.m22, b.m02, b.m12, b.m22 ) + this.m23 * b.m32
        r.m23 = Mathf.dot3( this.m20, this.m21, this.m22, b.m03, b.m13, b.m23 ) + this.m23 * b.m33
        r.m30 = Mathf.dot3( this.m30, this.m31, this.m32, b.m00, b.m10, b.m20 ) + this.m33 * b.m30
        r.m31 = Mathf.dot3( this.m30, this.m31, this.m32, b.m01, b.m11, b.m21 ) + this.m33 * b.m31
        r.m32 = Mathf.dot3( this.m30, this.m31, this.m32, b.m02, b.m12, b.m22 ) + this.m33 * b.m32
        r.m33 = Mathf.dot3( this.m30, this.m31, this.m32, b.m03, b.m13, b.m23 ) + this.m33 * b.m33
        ret r
    }

    # 变换点（w 补 1，带平移）
    Float32_3 transformPoint( Float32_3 v )
    {
        Float32 nx = Mathf.dot3( this.m00, this.m01, this.m02, v.x, v.y, v.z ) + this.m03
        Float32 ny = Mathf.dot3( this.m10, this.m11, this.m12, v.x, v.y, v.z ) + this.m13
        Float32 nz = Mathf.dot3( this.m20, this.m21, this.m22, v.x, v.y, v.z ) + this.m23
        ret Float32_3( nx, ny, nz )
    }

    # 变换方向（w 补 0，忽略平移）
    Float32_3 transformDirection( Float32_3 v )
    {
        Float32 nx = Mathf.dot3( this.m00, this.m01, this.m02, v.x, v.y, v.z )
        Float32 ny = Mathf.dot3( this.m10, this.m11, this.m12, v.x, v.y, v.z )
        Float32 nz = Mathf.dot3( this.m20, this.m21, this.m22, v.x, v.y, v.z )
        ret Float32_3( nx, ny, nz )
    }

    Float32_4x4 transpose()
    {
        ret Float32_4x4( this.m00, this.m10, this.m20, this.m30,
                         this.m01, this.m11, this.m21, this.m31,
                         this.m02, this.m12, this.m22, this.m32,
                         this.m03, this.m13, this.m23, this.m33 )
    }

    Float32 determinant()
    {
        Float32 b00 = this.m00 * this.m11 - this.m01 * this.m10
        Float32 b01 = this.m00 * this.m12 - this.m02 * this.m10
        Float32 b02 = this.m00 * this.m13 - this.m03 * this.m10
        Float32 b03 = this.m01 * this.m12 - this.m02 * this.m11
        Float32 b04 = this.m01 * this.m13 - this.m03 * this.m11
        Float32 b05 = this.m02 * this.m13 - this.m03 * this.m12
        Float32 b06 = this.m20 * this.m31 - this.m21 * this.m30
        Float32 b07 = this.m20 * this.m32 - this.m22 * this.m30
        Float32 b08 = this.m20 * this.m33 - this.m23 * this.m30
        Float32 b09 = this.m21 * this.m32 - this.m22 * this.m31
        Float32 b10 = this.m21 * this.m33 - this.m23 * this.m31
        Float32 b11 = this.m22 * this.m33 - this.m23 * this.m32
        ret b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float32_4x4 inverse()
    {
        Float32 det = this.determinant()
        if det == 0.0f
        {
            ret Float32_4x4()
        }
        Float32 inv = 1.0f / det

        Float32 b00 = this.m00 * this.m11 - this.m01 * this.m10
        Float32 b01 = this.m00 * this.m12 - this.m02 * this.m10
        Float32 b02 = this.m00 * this.m13 - this.m03 * this.m10
        Float32 b03 = this.m01 * this.m12 - this.m02 * this.m11
        Float32 b04 = this.m01 * this.m13 - this.m03 * this.m11
        Float32 b05 = this.m02 * this.m13 - this.m03 * this.m12
        Float32 b06 = this.m20 * this.m31 - this.m21 * this.m30
        Float32 b07 = this.m20 * this.m32 - this.m22 * this.m30
        Float32 b08 = this.m20 * this.m33 - this.m23 * this.m30
        Float32 b09 = this.m21 * this.m32 - this.m22 * this.m31
        Float32 b10 = this.m21 * this.m33 - this.m23 * this.m31
        Float32 b11 = this.m22 * this.m33 - this.m23 * this.m32

        Float32_4x4 r = Float32_4x4()
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

    Float32_4x4 clone()
    {
        ret Float32_4x4( this.m00, this.m01, this.m02, this.m03,
                         this.m10, this.m11, this.m12, this.m13,
                         this.m20, this.m21, this.m22, this.m23,
                         this.m30, this.m31, this.m32, this.m33 )
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
        ret Float32_4x4( 1.0f, 0.0f, 0.0f, 0.0f,
                         0.0f, 1.0f, 0.0f, 0.0f,
                         0.0f, 0.0f, 1.0f, 0.0f,
                         0.0f, 0.0f, 0.0f, 1.0f )
    }

    public static get Float32_4x4 zero()
    {
        ret Float32_4x4()
    }

    public static Float32_4x4 translation( Float32 x, Float32 y, Float32 z )
    {
        ret Float32_4x4( 1.0f, 0.0f, 0.0f, x,
                         0.0f, 1.0f, 0.0f, y,
                         0.0f, 0.0f, 1.0f, z,
                         0.0f, 0.0f, 0.0f, 1.0f )
    }

    public static Float32_4x4 translation( Float32_3 t )
    {
        ret Float32_4x4.translation( t.x, t.y, t.z )
    }

    public static Float32_4x4 scale( Float32 x, Float32 y, Float32 z )
    {
        ret Float32_4x4( x, 0.0f, 0.0f, 0.0f,
                         0.0f, y, 0.0f, 0.0f,
                         0.0f, 0.0f, z, 0.0f,
                         0.0f, 0.0f, 0.0f, 1.0f )
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
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        ret Float32_4x4( 1.0f, 0.0f, 0.0f, 0.0f,
                         0.0f, c, -s, 0.0f,
                         0.0f, s, c, 0.0f,
                         0.0f, 0.0f, 0.0f, 1.0f )
    }

    # 绕 Y 轴旋转（弧度）
    public static Float32_4x4 rotationY( Float32 radians )
    {
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        ret Float32_4x4( c, 0.0f, s, 0.0f,
                         0.0f, 1.0f, 0.0f, 0.0f,
                         -s, 0.0f, c, 0.0f,
                         0.0f, 0.0f, 0.0f, 1.0f )
    }

    # 绕 Z 轴旋转（弧度）
    public static Float32_4x4 rotationZ( Float32 radians )
    {
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        ret Float32_4x4( c, -s, 0.0f, 0.0f,
                         s, c, 0.0f, 0.0f,
                         0.0f, 0.0f, 1.0f, 0.0f,
                         0.0f, 0.0f, 0.0f, 1.0f )
    }

    # 绕任意轴旋转（弧度，axis 需为单位向量）
    public static Float32_4x4 rotationAxis( Float32_3 axis, Float32 radians )
    {
        Float32_3 a = axis.normalize()
        Float32 x = a.x
        Float32 y = a.y
        Float32 z = a.z
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        Float32 t = 1.0f - c
        ret Float32_4x4( t * x * x + c, t * x * y - s * z, t * x * z + s * y, 0.0f,
                         t * x * y + s * z, t * y * y + c, t * y * z - s * x, 0.0f,
                         t * x * z - s * y, t * y * z + s * x, t * z * z + c, 0.0f,
                         0.0f, 0.0f, 0.0f, 1.0f )
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
        Float32 f = 1.0f / Mathf.tan( fovYRadians * 0.5f )
        ret Float32_4x4( f / aspect, 0.0f, 0.0f, 0.0f,
                         0.0f, f, 0.0f, 0.0f,
                         0.0f, 0.0f, ( far + near ) / ( near - far ), ( 2.0f * far * near ) / ( near - far ),
                         0.0f, 0.0f, -1.0f, 0.0f )
    }

    # 正交投影
    public static Float32_4x4 ortho( Float32 left, Float32 right, Float32 bottom, Float32 top, Float32 near, Float32 far )
    {
        ret Float32_4x4( 2.0f / ( right - left ), 0.0f, 0.0f, 0.0f - ( right + left ) / ( right - left ),
                         0.0f, 2.0f / ( top - bottom ), 0.0f, 0.0f - ( top + bottom ) / ( top - bottom ),
                         0.0f, 0.0f, 0.0f - 2.0f / ( far - near ), 0.0f - ( far + near ) / ( far - near ),
                         0.0f, 0.0f, 0.0f, 1.0f )
    }

    # 视图矩阵（右手系 lookAt）
    public static Float32_4x4 lookAt( Float32_3 eye, Float32_3 target, Float32_3 upHint )
    {
        Float32_3 zAxis = eye._sub_( target ).normalize()
        Float32_3 xAxis = upHint.cross( zAxis ).normalize()
        Float32_3 yAxis = zAxis.cross( xAxis )
        ret Float32_4x4( xAxis.x, xAxis.y, xAxis.z, 0.0f - xAxis.dot( eye ),
                         yAxis.x, yAxis.y, yAxis.z, 0.0f - yAxis.dot( eye ),
                         zAxis.x, zAxis.y, zAxis.z, 0.0f - zAxis.dot( eye ),
                         0.0f, 0.0f, 0.0f, 1.0f )
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
