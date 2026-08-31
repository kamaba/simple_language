@Nickname("Matrix3x3h")
@Nickname("Mat3h")
@Nickname("half3x3")
public class Float16_3x3
{
    # 行主序存储：m[row * 3 + col]
    public Array<Float16> _mat3x3 = null

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._mat3x3 = Array<Float16>( 9 )
        int i = 0
        while i < 9
        {
            this._mat3x3[i] = 0.0h
            i++
        }
    }

    public void _init_( Float16 m00, Float16 m01, Float16 m02,
                        Float16 m10, Float16 m11, Float16 m12,
                        Float16 m20, Float16 m21, Float16 m22 )
    {
        this._mat3x3 = Array<Float16>( 9 )
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

    # 由 Float32_3x3 降精度
    public void _init_( Float32_3x3 m )
    {
        this._mat3x3 = Array<Float16>( 9 )
        int i = 0
        while i < 9
        {
            this._mat3x3[i] = m._mat3x3[i]
            i++
        }
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float16 _getItem_( int index )
    {
        ret this._mat3x3[index]
    }

    void _setItem_( int index, Float16 value )
    {
        this._mat3x3[index] = value
    }

    Float16 get( int row, int col )
    {
        ret this._mat3x3[ row * 3 + col ]
    }

    void set( int row, int col, Float16 value )
    {
        this._mat3x3[ row * 3 + col ] = value
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
        if obj1 is Float16_3x3 b
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
    Float16_3x3 multiply( Float16_3x3 b )
    {
        Float16_3x3 r = Float16_3x3()
        int row = 0
        while row < 3
        {
            int col = 0
            while col < 3
            {
                Float16 sum = 0.0h
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

    Float16_3 transform( Float16_3 v )
    {
        Float16 nx = this.get( 0, 0 ) * v.x + this.get( 0, 1 ) * v.y + this.get( 0, 2 ) * v.z
        Float16 ny = this.get( 1, 0 ) * v.x + this.get( 1, 1 ) * v.y + this.get( 1, 2 ) * v.z
        Float16 nz = this.get( 2, 0 ) * v.x + this.get( 2, 1 ) * v.y + this.get( 2, 2 ) * v.z
        ret Float16_3( nx, ny, nz )
    }

    Float16_3x3 transpose()
    {
        Float16_3x3 r = Float16_3x3()
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

    Float16 determinant()
    {
        Float16 a = this.get( 0, 0 )
        Float16 b = this.get( 0, 1 )
        Float16 c = this.get( 0, 2 )
        Float16 d = this.get( 1, 0 )
        Float16 e = this.get( 1, 1 )
        Float16 f = this.get( 1, 2 )
        Float16 g = this.get( 2, 0 )
        Float16 h = this.get( 2, 1 )
        Float16 i = this.get( 2, 2 )
        ret a * ( e * i - f * h ) - b * ( d * i - f * g ) + c * ( d * h - e * g )
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

        Float16 a = this.get( 0, 0 )
        Float16 b = this.get( 0, 1 )
        Float16 c = this.get( 0, 2 )
        Float16 d = this.get( 1, 0 )
        Float16 e = this.get( 1, 1 )
        Float16 f = this.get( 1, 2 )
        Float16 g = this.get( 2, 0 )
        Float16 h = this.get( 2, 1 )
        Float16 i = this.get( 2, 2 )

        Float16_3x3 r = Float16_3x3()
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

    Float16_3x3 clone()
    {
        Float16_3x3 r = Float16_3x3()
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

    public static Float16_3x3 rotationX( Float16 radians )
    {
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        ret Float16_3x3( 1.0h, 0.0h, 0.0h,
                         0.0h, c, 0.0h - s,
                         0.0h, s, c )
    }

    public static Float16_3x3 rotationY( Float16 radians )
    {
        Float16 c = Mathh.cos( radians )
        Float16 s = Mathh.sin( radians )
        ret Float16_3x3( c, 0.0h, s,
                         0.0h, 1.0h, 0.0h,
                         0.0h - s, 0.0h, c )
    }

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
            this._mat3x3[0], this._mat3x3[1], this._mat3x3[2],
            this._mat3x3[3], this._mat3x3[4], this._mat3x3[5],
            this._mat3x3[6], this._mat3x3[7], this._mat3x3[8] )
    }
}
