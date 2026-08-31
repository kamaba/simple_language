@Nickname("Matrix3x3d")
@Nickname("Mat3d")
@Nickname("double3x3")
public class Float64_3x3
{
    # 行主序存储：m[row * 3 + col]
    public Array<Float64> _mat3x3 = null

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._mat3x3 = Array<Float64>( 9 )
        int i = 0
        while i < 9
        {
            this._mat3x3[i] = 0.0d
            i++
        }
    }

    public void _init_( Float64 m00, Float64 m01, Float64 m02,
                        Float64 m10, Float64 m11, Float64 m12,
                        Float64 m20, Float64 m21, Float64 m22 )
    {
        this._mat3x3 = Array<Float64>( 9 )
        this._mat3x3[0] = m00
        this._mat3x3[1] = m01
        this._mat3x3[2] = m02
        this._mat3x3[3] = m10
        this._mat3x3[4] = m11
        this._mat3x3[5] = m12
        this._mat3x3[6] = m20
        this._mat3x3[7] = m21
        this._mat3x3[8] = m22
    }

    # 由 Float32_3x3 提升
    public void _init_( Float32_3x3 m )
    {
        this._mat3x3 = Array<Float64>( 9 )
        int i = 0
        while i < 9
        {
            this._mat3x3[i] = m._mat3x3[i].toFloat64()
            i++
        }
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float64 _getItem_( int index )
    {
        ret this._mat3x3[index]
    }

    void _setItem_( int index, Float64 value )
    {
        this._mat3x3[index] = value
    }

    Float64 get( int row, int col )
    {
        ret this._mat3x3[ row * 3 + col ]
    }

    void set( int row, int col, Float64 value )
    {
        this._mat3x3[ row * 3 + col ] = value
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
            int i = 0
            while i < 9
            {
                r._mat3x3[i] = this._mat3x3[i] + b._mat3x3[i]
                i++
            }
            ret r
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float64_3x3 b
        {
            int i = 0
            while i < 9
            {
                if this._mat3x3[i] != b._mat3x3[i]
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
    Float64_3x3 multiply( Float64_3x3 b )
    {
        Float64_3x3 r = Float64_3x3()
        int row = 0
        while row < 3
        {
            int col = 0
            while col < 3
            {
                Float64 sum = 0.0d
                int k = 0
                while k < 3
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

    Float64_3 transform( Float64_3 v )
    {
        Float64 nx = this.get( 0, 0 ) * v.x + this.get( 0, 1 ) * v.y + this.get( 0, 2 ) * v.z
        Float64 ny = this.get( 1, 0 ) * v.x + this.get( 1, 1 ) * v.y + this.get( 1, 2 ) * v.z
        Float64 nz = this.get( 2, 0 ) * v.x + this.get( 2, 1 ) * v.y + this.get( 2, 2 ) * v.z
        ret Float64_3( nx, ny, nz )
    }

    Float64_3x3 transpose()
    {
        Float64_3x3 r = Float64_3x3()
        int row = 0
        while row < 3
        {
            int col = 0
            while col < 3
            {
                r.set( row, col, this.get( col, row ) )
                col++
            }
            row++
        }
        ret r
    }

    Float64 determinant()
    {
        Float64 a = this.get( 0, 0 )
        Float64 b = this.get( 0, 1 )
        Float64 c = this.get( 0, 2 )
        Float64 d = this.get( 1, 0 )
        Float64 e = this.get( 1, 1 )
        Float64 f = this.get( 1, 2 )
        Float64 g = this.get( 2, 0 )
        Float64 h = this.get( 2, 1 )
        Float64 i = this.get( 2, 2 )
        ret a * ( e * i - f * h ) - b * ( d * i - f * g ) + c * ( d * h - e * g )
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

        Float64 a = this.get( 0, 0 )
        Float64 b = this.get( 0, 1 )
        Float64 c = this.get( 0, 2 )
        Float64 d = this.get( 1, 0 )
        Float64 e = this.get( 1, 1 )
        Float64 f = this.get( 1, 2 )
        Float64 g = this.get( 2, 0 )
        Float64 h = this.get( 2, 1 )
        Float64 i = this.get( 2, 2 )

        Float64_3x3 r = Float64_3x3()
        r.set( 0, 0, ( e * i - f * h ) * inv )
        r.set( 0, 1, ( c * h - b * i ) * inv )
        r.set( 0, 2, ( b * f - c * e ) * inv )
        r.set( 1, 0, ( f * g - d * i ) * inv )
        r.set( 1, 1, ( a * i - c * g ) * inv )
        r.set( 1, 2, ( c * d - a * f ) * inv )
        r.set( 2, 0, ( d * h - e * g ) * inv )
        r.set( 2, 1, ( b * g - a * h ) * inv )
        r.set( 2, 2, ( a * e - b * d ) * inv )
        ret r
    }

    Float64_3x3 clone()
    {
        Float64_3x3 r = Float64_3x3()
        int i = 0
        while i < 9
        {
            r._mat3x3[i] = this._mat3x3[i]
            i++
        }
        ret r
    }

    Float32_3x3 toFloat32_3x3()
    {
        Float32_3x3 r = Float32_3x3()
        int i = 0
        while i < 9
        {
            r._mat3x3[i] = this._mat3x3[i].toFloat32()
            i++
        }
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

    public static Float64_3x3 rotationX( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        ret Float64_3x3( 1.0d, 0.0d, 0.0d,
                         0.0d, c, 0.0d - s,
                         0.0d, s, c )
    }

    public static Float64_3x3 rotationY( Float64 radians )
    {
        Float64 c = Mathd.cos( radians )
        Float64 s = Mathd.sin( radians )
        ret Float64_3x3( c, 0.0d, s,
                         0.0d, 1.0d, 0.0d,
                         0.0d - s, 0.0d, c )
    }

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
            this._mat3x3[0], this._mat3x3[1], this._mat3x3[2],
            this._mat3x3[3], this._mat3x3[4], this._mat3x3[5],
            this._mat3x3[6], this._mat3x3[7], this._mat3x3[8] )
    }
}
