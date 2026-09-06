@Nickname("Matrix4x4")
@Nickname("Mat4")
@Nickname("float4x4")
public class Float32_4x4
{
    # 行主序存储：_mat4x4[row * 4 + col]
    public Array<Float32> _mat4x4 = null

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._mat4x4 = Array<Float32>( 16 )
        this._mat4x4.fill( 0.0f )
    }

    public void _init_( Array<Float32> values )
    {
        this._mat4x4 = Array<Float32>( 16 )
        int i = 0
        while i < 16
        {
            this._mat4x4[i] = values[i]
            i++
        }
    }

    public void _init_( Float32_3x3 m )
    {
        this._mat4x4 = Array<Float32>( 16 )
        this.set( 0, 0, m.get( 0, 0 ) )
        this.set( 0, 1, m.get( 0, 1 ) )
        this.set( 0, 2, m.get( 0, 2 ) )
        this.set( 1, 0, m.get( 1, 0 ) )
        this.set( 1, 1, m.get( 1, 1 ) )
        this.set( 1, 2, m.get( 1, 2 ) )
        this.set( 2, 0, m.get( 2, 0 ) )
        this.set( 2, 1, m.get( 2, 1 ) )
        this.set( 2, 2, m.get( 2, 2 ) )
        this.set( 3, 3, 1.0f )
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float32 _getItem_( int index )
    {
        ret this._mat4x4[index]
    }

    void _setItem_( int index, Float32 value )
    {
        this._mat4x4[index] = value
    }

    Float32 getValue( int row, int col )
    {
        ret this._mat4x4[ row * 4 + col ]
    }

    void setValue( int row, int col, Float32 value )
    {
        this._mat4x4[ row * 4 + col ] = value
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
            int i = 0
            while i < 16
            {
                r._mat4x4[i] = this._mat4x4[i] + b._mat4x4[i]
                i++
            }
            ret r
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float32_4x4 b
        {
            int i = 0
            while i < 16
            {
                if this._mat4x4[i] != b._mat4x4[i]
                {
                    ret false
                }
                i++
            }
            ret true
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 矩阵运算 ─────────────────────────────────────────
    Float32_4x4 multiply( Float32_4x4 b )
    {
        Float32_4x4 r = Float32_4x4()
        int row = 0
        while row < 4
        {
            int col = 0
            while col < 4
            {
                Float32 sum = 0.0f
                int k = 0
                while k < 4
                {
                    sum = sum + this.get( row, k ) * b.get( k, col )
                    k++
                }
                r.set( row, col, sum )
                col++
            }
            row++
        }
        ret r
    }

    # 变换点（w 补 1，带平移）
    Float32_3 transformPoint( Float32_3 v )
    {
        Float32 x = this.get( 0, 0 ) * v.x + this.get( 0, 1 ) * v.y + this.get( 0, 2 ) * v.z + this.get( 0, 3 )
        Float32 y = this.get( 1, 0 ) * v.x + this.get( 1, 1 ) * v.y + this.get( 1, 2 ) * v.z + this.get( 1, 3 )
        Float32 z = this.get( 2, 0 ) * v.x + this.get( 2, 1 ) * v.y + this.get( 2, 2 ) * v.z + this.get( 2, 3 )
        ret Float32_3( x, y, z )
    }

    # 变换方向（w 补 0，忽略平移）
    Float32_3 transformDirection( Float32_3 v )
    {
        Float32 x = this.get( 0, 0 ) * v.x + this.get( 0, 1 ) * v.y + this.get( 0, 2 ) * v.z
        Float32 y = this.get( 1, 0 ) * v.x + this.get( 1, 1 ) * v.y + this.get( 1, 2 ) * v.z
        Float32 z = this.get( 2, 0 ) * v.x + this.get( 2, 1 ) * v.y + this.get( 2, 2 ) * v.z
        ret Float32_3( x, y, z )
    }

    Float32_4x4 transpose()
    {
        Float32_4x4 r = Float32_4x4()
        int row = 0
        while row < 4
        {
            int col = 0
            while col < 4
            {
                r.set( row, col, this.get( col, row ) )
                col++
            }
            row++
        }
        ret r
    }

    Float32_4x4 clone()
    {
        Float32_4x4 r = Float32_4x4()
        int i = 0
        while i < 16
        {
            r._mat4x4[i] = this._mat4x4[i]
            i++
        }
        ret r
    }

    Float32_3x3 toFloat32_3x3()
    {
        Float32_3x3 r = Float32_3x3()
        int row = 0
        while row < 3
        {
            int col = 0
            while col < 3
            {
                r.set( row, col, this.get( row, col ) )
                col++
            }
            row++
        }
        ret r
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Float32_4x4 identity()
    {
        Float32_4x4 r = Float32_4x4()
        r.set( 0, 0, 1.0f )
        r.set( 1, 1, 1.0f )
        r.set( 2, 2, 1.0f )
        r.set( 3, 3, 1.0f )
        ret r
    }

    public static get Float32_4x4 zero()
    {
        ret Float32_4x4()
    }

    public static Float32_4x4 translation( Float32 x, Float32 y, Float32 z )
    {
        Float32_4x4 r = Float32_4x4.identity()
        r.set( 0, 3, x )
        r.set( 1, 3, y )
        r.set( 2, 3, z )
        ret r
    }

    public static Float32_4x4 translation( Float32_3 t )
    {
        ret Float32_4x4.translation( t.x, t.y, t.z )
    }

    public static Float32_4x4 scale( Float32 x, Float32 y, Float32 z )
    {
        Float32_4x4 r = Float32_4x4()
        r.set( 0, 0, x )
        r.set( 1, 1, y )
        r.set( 2, 2, z )
        r.set( 3, 3, 1.0f )
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
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        Float32_4x4 r = Float32_4x4.identity()
        r.set( 1, 1, c )
        r.set( 1, 2, -s )
        r.set( 2, 1, s )
        r.set( 2, 2, c )
        ret r
    }

    public static Float32_4x4 rotationY( Float32 radians )
    {
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        Float32_4x4 r = Float32_4x4.identity()
        r.set( 0, 0, c )
        r.set( 0, 2, s )
        r.set( 2, 0, -s )
        r.set( 2, 2, c )
        ret r
    }

    public static Float32_4x4 rotationZ( Float32 radians )
    {
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        Float32_4x4 r = Float32_4x4.identity()
        r.set( 0, 0, c )
        r.set( 0, 1, -s )
        r.set( 1, 0, s )
        r.set( 1, 1, c )
        ret r
    }

    public static Float32_4x4 rotationAxis( Float32_3 axis, Float32 radians )
    {
        Float32_3 a = axis.normalize()
        Float32 x = a.x
        Float32 y = a.y
        Float32 z = a.z
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        Float32 t = 1.0f - c

        Float32_4x4 r = Float32_4x4()
        r.set( 0, 0, t * x * x + c )
        r.set( 0, 1, t * x * y - s * z )
        r.set( 0, 2, t * x * z + s * y )
        r.set( 1, 0, t * x * y + s * z )
        r.set( 1, 1, t * y * y + c )
        r.set( 1, 2, t * y * z - s * x )
        r.set( 2, 0, t * x * z - s * y )
        r.set( 2, 1, t * y * z + s * x )
        r.set( 2, 2, t * z * z + c )
        r.set( 3, 3, 1.0f )
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
        Float32 f = 1.0f / Mathf.tan( fovYRadians * 0.5f )
        Float32_4x4 r = Float32_4x4()
        r.set( 0, 0, f / aspect )
        r.set( 1, 1, f )
        r.set( 2, 2, ( far + near ) / ( near - far ) )
        r.set( 2, 3, ( 2.0f * far * near ) / ( near - far ) )
        r.set( 3, 2, -1.0f )
        ret r
    }

    # 正交投影
    public static Float32_4x4 ortho( Float32 left, Float32 right, Float32 bottom, Float32 top, Float32 near, Float32 far )
    {
        Float32_4x4 r = Float32_4x4()
        r.set( 0, 0, 2.0f / ( right - left ) )
        r.set( 1, 1, 2.0f / ( top - bottom ) )
        r.set( 2, 2, -2.0f / ( far - near ) )
        r.set( 0, 3, -( right + left ) / ( right - left ) )
        r.set( 1, 3, -( top + bottom ) / ( top - bottom ) )
        r.set( 2, 3, -( far + near ) / ( far - near ) )
        r.set( 3, 3, 1.0f )
        ret r
    }

    # 视图矩阵（右手系 lookAt）
    public static Float32_4x4 lookAt( Float32_3 eye, Float32_3 target, Float32_3 upHint )
    {
        Float32_3 zAxis = eye._sub_( target ).normalize()
        Float32_3 xAxis = upHint.cross( zAxis ).normalize()
        Float32_3 yAxis = zAxis.cross( xAxis )

        Float32_4x4 r = Float32_4x4()
        r.set( 0, 0, xAxis.x )
        r.set( 0, 1, xAxis.y )
        r.set( 0, 2, xAxis.z )
        r.set( 0, 3, -xAxis.dot( eye ) )
        r.set( 1, 0, yAxis.x )
        r.set( 1, 1, yAxis.y )
        r.set( 1, 2, yAxis.z )
        r.set( 1, 3, -yAxis.dot( eye ) )
        r.set( 2, 0, zAxis.x )
        r.set( 2, 1, zAxis.y )
        r.set( 2, 2, zAxis.z )
        r.set( 2, 3, -zAxis.dot( eye ) )
        r.set( 3, 3, 1.0f )
        ret r
    }

    override string toString()
    {
        ret String.toFormat(
            "Float32_4x4[{0},{1},{2},{3} | {4},{5},{6},{7} | {8},{9},{10},{11} | {12},{13},{14},{15}]",
            this._mat4x4[0], this._mat4x4[1], this._mat4x4[2], this._mat4x4[3],
            this._mat4x4[4], this._mat4x4[5], this._mat4x4[6], this._mat4x4[7],
            this._mat4x4[8], this._mat4x4[9], this._mat4x4[10], this._mat4x4[11],
            this._mat4x4[12], this._mat4x4[13], this._mat4x4[14], this._mat4x4[15] )
    }
}
