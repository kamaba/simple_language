@Nickname("Matrix3x3")
@Nickname("Mat3")
@Nickname("float3x3")
public class Float32_3x3
{
    # 行主序存储：m[row * 3 + col]
    public Array<Float32> _mat3x3 = null

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this._mat3x3 = Array<Float32>( 9 )
        int i = 0
        while i < 9
        {
            this._mat3x3[i] = 0.0f
            i++
        }
    }

    public void _init_( Float32 m00, Float32 m01, Float32 m02,
                        Float32 m10, Float32 m11, Float32 m12,
                        Float32 m20, Float32 m21, Float32 m22 )
    {
        this._mat3x3 = Array<Float32>( 9 )
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

    # ── 索引访问 ─────────────────────────────────────────
    Float32 _getItem_( int index )
    {
        ret this._mat3x3[index]
    }

    void _setItem_( int index, Float32 value )
    {
        this._mat3x3[index] = value
    }

    Float32 get( int row, int col )
    {
        ret this._mat3x3[ row * 3 + col ]
    }

    void set( int row, int col, Float32 value )
    {
        this._mat3x3[ row * 3 + col ] = value
    }

    # ── 运算符重载 ───────────────────────────────────────
    override Float32_3x3 _mul_( Object obj1 )
    {
        if obj1 is Float32_3x3 b
        {
            ret this.multiply( b )
        }
        ret this
    }

    override Float32_3x3 _add_( Object obj1 )
    {
        if obj1 is Float32_3x3 b
        {
            Float32_3x3 r = Float32_3x3()
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
        if obj1 is Float32_3x3 b
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
    Float32_3x3 multiply( Float32_3x3 b )
    {
        Float32_3x3 r = Float32_3x3()
        int row = 0
        while row < 3
        {
            int col = 0
            while col < 3
            {
                Float32 sum = 0.0f
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

    Float32_3 transform( Float32_3 v )
    {
        Float32 nx = this.get( 0, 0 ) * v.x + this.get( 0, 1 ) * v.y + this.get( 0, 2 ) * v.z
        Float32 ny = this.get( 1, 0 ) * v.x + this.get( 1, 1 ) * v.y + this.get( 1, 2 ) * v.z
        Float32 nz = this.get( 2, 0 ) * v.x + this.get( 2, 1 ) * v.y + this.get( 2, 2 ) * v.z
        ret Float32_3( nx, ny, nz )
    }

    Float32_3x3 transpose()
    {
        Float32_3x3 r = Float32_3x3()
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

    Float32 determinant()
    {
        Float32 a = this.get( 0, 0 )
        Float32 b = this.get( 0, 1 )
        Float32 c = this.get( 0, 2 )
        Float32 d = this.get( 1, 0 )
        Float32 e = this.get( 1, 1 )
        Float32 f = this.get( 1, 2 )
        Float32 g = this.get( 2, 0 )
        Float32 h = this.get( 2, 1 )
        Float32 i = this.get( 2, 2 )
        ret a * ( e * i - f * h ) - b * ( d * i - f * g ) + c * ( d * h - e * g )
    }

    # 伴随矩阵 / det，不可逆时返回零矩阵
    Float32_3x3 inverse()
    {
        Float32 det = this.determinant()
        if det == 0.0f
        {
            ret Float32_3x3()
        }
        Float32 inv = 1.0f / det

        Float32 a = this.get( 0, 0 )
        Float32 b = this.get( 0, 1 )
        Float32 c = this.get( 0, 2 )
        Float32 d = this.get( 1, 0 )
        Float32 e = this.get( 1, 1 )
        Float32 f = this.get( 1, 2 )
        Float32 g = this.get( 2, 0 )
        Float32 h = this.get( 2, 1 )
        Float32 i = this.get( 2, 2 )

        Float32_3x3 r = Float32_3x3()
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

    Float32_3x3 clone()
    {
        Float32_3x3 r = Float32_3x3()
        int i = 0
        while i < 9
        {
            r._mat3x3[i] = this._mat3x3[i]
            i++
        }
        ret r
    }

    # ── 静态常量与工厂 ────────────────────────────────────
    public static get Float32_3x3 identity()
    {
        ret Float32_3x3( 1.0f, 0.0f, 0.0f,
                         0.0f, 1.0f, 0.0f,
                         0.0f, 0.0f, 1.0f )
    }

    public static get Float32_3x3 zero()
    {
        ret Float32_3x3()
    }

    # 绕 X 轴旋转（弧度）
    public static Float32_3x3 rotationX( Float32 radians )
    {
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        ret Float32_3x3( 1.0f, 0.0f, 0.0f,
                         0.0f, c, -s,
                         0.0f, s, c )
    }

    # 绕 Y 轴旋转（弧度）
    public static Float32_3x3 rotationY( Float32 radians )
    {
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        ret Float32_3x3( c, 0.0f, s,
                         0.0f, 1.0f, 0.0f,
                         -s, 0.0f, c )
    }

    # 绕 Z 轴旋转（弧度）
    public static Float32_3x3 rotationZ( Float32 radians )
    {
        Float32 c = Mathf.cos( radians )
        Float32 s = Mathf.sin( radians )
        ret Float32_3x3( c, -s, 0.0f,
                         s, c, 0.0f,
                         0.0f, 0.0f, 1.0f )
    }

    public static Float32_3x3 scale( Float32 sx, Float32 sy )
    {
        ret Float32_3x3( sx, 0.0f, 0.0f,
                         0.0f, sy, 0.0f,
                         0.0f, 0.0f, 1.0f )
    }

    public static Float32_3x3 translation( Float32 tx, Float32 ty )
    {
        ret Float32_3x3( 1.0f, 0.0f, tx,
                         0.0f, 1.0f, ty,
                         0.0f, 0.0f, 1.0f )
    }

    override string toString()
    {
        ret String.toFormat( "Float32_3x3[{0},{1},{2} | {3},{4},{5} | {6},{7},{8}]",
            this._mat3x3[0], this._mat3x3[1], this._mat3x3[2],
            this._mat3x3[3], this._mat3x3[4], this._mat3x3[5],
            this._mat3x3[6], this._mat3x3[7], this._mat3x3[8] )
    }
}
