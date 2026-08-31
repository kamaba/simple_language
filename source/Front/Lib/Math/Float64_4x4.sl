@Nickname("Matrix4x4d")
@Nickname("Mat4d")
@Nickname("double4x4")
public class Float64_4x4
{
    # 行主序存储：_mat4x4[row * 4 + col]
    public Array<Float64> _mat4x4 = null

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._mat4x4 = Array<Float64>( 16 )
        int i = 0
        while i < 16
        {
            this._mat4x4[i] = 0.0d
            i++
        }
    }

    public void _init_( Array<Float64> values )
    {
        this._mat4x4 = Array<Float64>( 16 )
        int i = 0
        while i < 16
        {
            this._mat4x4[i] = values[i]
            i++
        }
    }

    public void _init_( Float64_3x3 m )
    {
        this._mat4x4 = Array<Float64>( 16 )
        this.set( 0, 0, m.get( 0, 0 ) )
        this.set( 0, 1, m.get( 0, 1 ) )
        this.set( 0, 2, m.get( 0, 2 ) )
        this.set( 1, 0, m.get( 1, 0 ) )
        this.set( 1, 1, m.get( 1, 1 ) )
        this.set( 1, 2, m.get( 1, 2 ) )
        this.set( 2, 0, m.get( 2, 0 ) )
        this.set( 2, 1, m.get( 2, 1 ) )
        this.set( 2, 2, m.get( 2, 2 ) )
        this.set( 3, 3, 1.0d )
    }

    public void _init_( Float32_4x4 m )
    {
        this._mat4x4 = Array<Float64>( 16 )
        int i = 0
        while i < 16
        {
            this._mat4x4[i] = m._mat4x4[i].toFloat64()
            i++
        }
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float64 _getItem_( int index )
    {
        ret this._mat4x4[index]
    }

    void _setItem_( int index, Float64 value )
    {
        this._mat4x4[index] = value
    }

    Float64 get( int row, int col )
    {
        ret this._mat4x4[ row * 4 + col ]
    }

    void set( int row, int col, Float64 value )
    {
        this._mat4x4[ row * 4 + col ] = value
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
        if obj1 is Float64_4x4 b
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
    Float64_4x4 multiply( Float64_4x4 b )
    {
        Float64_4x4 r = Float64_4x4()
        int row = 0
        while row < 4
        {
            int col = 0
            while col < 4
            {
                Float64 sum = 0.0d
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
    Float64_3 transformPoint( Float64_3 v )
    {
        Float64 x = this.get( 0, 0 ) * v.x + this.get( 0, 1 ) * v.y + this.get( 0, 2 ) * v.z + this.get( 0, 3 )
        Float64 y = this.get( 1, 0 ) * v.x + this.get( 1, 1 ) * v.y + this.get( 1, 2 ) * v.z + this.get( 1, 3 )
        Float64 z = this.get( 2, 0 ) * v.x + this.get( 2, 1 ) * v.y + this.get( 2, 2 ) * v.z + this.get( 2, 3 )
        ret Float64_3( x, y, z )
    }

    # 变换方向（w 补 0，忽略平移）
    Float64_3 transformDirection( Float64_3 v )
    {
        Float64 x = this.get( 0, 0 ) * v.x + this.get( 0, 1 ) * v.y + this.get( 0, 2 ) * v.z
        Float64 y = this.get( 1, 0 ) * v.x + this.get( 1, 1 ) * v.y + this.get( 1, 2 ) * v.z
        Float64 z = this.get( 2, 0 ) * v.x + this.get( 2, 1 ) * v.y + this.get( 2, 2 ) * v.z
        ret Float64_3( x, y, z )
    }

    Float64_4x4 transpose()
    {
        Float64_4x4 r = Float64_4x4()
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

    Float64_4x4 clone()
    {
        Float64_4x4 r = Float64_4x4()
        int i = 0
        while i < 16
        {
            r._mat4x4[i] = this._mat4x4[i]
            i++
        }
        ret r
    }

    Float64_3x3 toFloat64_3x3()
    {
        Float64_3x3 r = Float64_3x3()
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

    Float32_4x4 toFloat32_4x4()
    {
        Float32_4x4 r = Float32_4x4()
        int i = 0
        while i < 16
        {
            r._mat4x4[i] = this._mat4x4[i].toFloat32()
            i++
        }
        ret r
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Float64_4x4 identity()
    {
        Float64_4x4 r = Float64_4x4()
        r.set( 0, 0, 1.0d )
        r.set( 1, 1, 1.0d )
        r.set( 2, 2, 1.0d )
        r.set( 3, 3, 1.0d )
        ret r
    }

    public static get Float64_4x4 zero()
    {
        ret Float64_4x4()
    }

    public static Float64_4x4 translation( Float64 x, Float64 y, Float64 z )
    {
        Float64_4x4 r = Float64_4x4.identity()
        r.set( 0, 3, x )
        r.set( 1, 3, y )
        r.set( 2, 3, z )
        ret r
    }

    public static Float64_4x4 translation( Float64_3 t )
    {
        ret Float64_4x4.translation( t.x, t.y, t.z )
    }

    public static Float64_4x4 scale( Float64 x, Float64 y, Float64 z )
    {
        Float64_4x4 r = Float64_4x4()
        r.set( 0, 0, x )
        r.set( 1, 1, y )
        r.set( 2, 2, z )
        r.set( 3, 3, 1.0d )
        ret r
    }

    public static Float64_4x4 scale( Float64_3 s )
    {
        ret Float64_4x4.scale( s.x, s.y, s.z )
    }

    public static Float64_4x4 scale( Float64 s )
    {
        ret Float64_4x4.scale( s, s, s )
    }

    public static Float64_4x4 rotationX( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        Float64_4x4 r = Float64_4x4.identity()
        r.set( 1, 1, c )
        r.set( 1, 2, 0.0d - s )
        r.set( 2, 1, s )
        r.set( 2, 2, c )
        ret r
    }

    public static Float64_4x4 rotationY( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        Float64_4x4 r = Float64_4x4.identity()
        r.set( 0, 0, c )
        r.set( 0, 2, s )
        r.set( 2, 0, 0.0d - s )
        r.set( 2, 2, c )
        ret r
    }

    public static Float64_4x4 rotationZ( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        Float64_4x4 r = Float64_4x4.identity()
        r.set( 0, 0, c )
        r.set( 0, 1, 0.0d - s )
        r.set( 1, 0, s )
        r.set( 1, 1, c )
        ret r
    }

    public static Float64_4x4 rotationAxis( Float64_3 axis, Float64 radians )
    {
        Float64_3 a = axis.normalize()
        Float64 x = a.x
        Float64 y = a.y
        Float64 z = a.z
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        Float64 t = 1.0d - c

        Float64_4x4 r = Float64_4x4()
        r.set( 0, 0, t * x * x + c )
        r.set( 0, 1, t * x * y - s * z )
        r.set( 0, 2, t * x * z + s * y )
        r.set( 1, 0, t * x * y + s * z )
        r.set( 1, 1, t * y * y + c )
        r.set( 1, 2, t * y * z - s * x )
        r.set( 2, 0, t * x * z - s * y )
        r.set( 2, 1, t * y * z + s * x )
        r.set( 2, 2, t * z * z + c )
        r.set( 3, 3, 1.0d )
        ret r
    }

    public static Float64_4x4 trs( Float64_3 translation, Float64_3 rotationEuler, Float64_3 scale )
    {
        Float64_4x4 t = Float64_4x4.translation( translation )
        Float64_4x4 rx = Float64_4x4.rotationX( rotationEuler.x )
        Float64_4x4 ry = Float64_4x4.rotationY( rotationEuler.y )
        Float64_4x4 rz = Float64_4x4.rotationZ( rotationEuler.z )
        Float64_4x4 s = Float64_4x4.scale( scale )
        ret t.multiply( ry ).multiply( rx ).multiply( rz ).multiply( s )
    }

    public static Float64_4x4 perspective( Float64 fovYRadians, Float64 aspect, Float64 near, Float64 far )
    {
        Float64 f = 1.0d / Mathd.tan( fovYRadians * 0.5d )
        Float64_4x4 r = Float64_4x4()
        r.set( 0, 0, f / aspect )
        r.set( 1, 1, f )
        r.set( 2, 2, ( far + near ) / ( near - far ) )
        r.set( 2, 3, ( 2.0d * far * near ) / ( near - far ) )
        r.set( 3, 2, -1.0d )
        ret r
    }

    public static Float64_4x4 ortho( Float64 left, Float64 right, Float64 bottom, Float64 top, Float64 near, Float64 far )
    {
        Float64_4x4 r = Float64_4x4()
        r.set( 0, 0, 2.0d / ( right - left ) )
        r.set( 1, 1, 2.0d / ( top - bottom ) )
        r.set( 2, 2, -2.0d / ( far - near ) )
        r.set( 0, 3, 0.0d - ( right + left ) / ( right - left ) )
        r.set( 1, 3, 0.0d - ( top + bottom ) / ( top - bottom ) )
        r.set( 2, 3, 0.0d - ( far + near ) / ( far - near ) )
        r.set( 3, 3, 1.0d )
        ret r
    }

    public static Float64_4x4 lookAt( Float64_3 eye, Float64_3 target, Float64_3 upHint )
    {
        Float64_3 zAxis = eye._sub_( target ).normalize()
        Float64_3 xAxis = upHint.cross( zAxis ).normalize()
        Float64_3 yAxis = zAxis.cross( xAxis )

        Float64_4x4 r = Float64_4x4()
        r.set( 0, 0, xAxis.x )
        r.set( 0, 1, xAxis.y )
        r.set( 0, 2, xAxis.z )
        r.set( 0, 3, 0.0d - xAxis.dot( eye ) )
        r.set( 1, 0, yAxis.x )
        r.set( 1, 1, yAxis.y )
        r.set( 1, 2, yAxis.z )
        r.set( 1, 3, 0.0d - yAxis.dot( eye ) )
        r.set( 2, 0, zAxis.x )
        r.set( 2, 1, zAxis.y )
        r.set( 2, 2, zAxis.z )
        r.set( 2, 3, 0.0d - zAxis.dot( eye ) )
        r.set( 3, 3, 1.0d )
        ret r
    }

    override string toString()
    {
        ret String.toFormat(
            "Float64_4x4[{0},{1},{2},{3} | {4},{5},{6},{7} | {8},{9},{10},{11} | {12},{13},{14},{15}]",
            this._mat4x4[0], this._mat4x4[1], this._mat4x4[2], this._mat4x4[3],
            this._mat4x4[4], this._mat4x4[5], this._mat4x4[6], this._mat4x4[7],
            this._mat4x4[8], this._mat4x4[9], this._mat4x4[10], this._mat4x4[11],
            this._mat4x4[12], this._mat4x4[13], this._mat4x4[14], this._mat4x4[15] )
    }
}
