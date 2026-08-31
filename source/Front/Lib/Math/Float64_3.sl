@Nickname("Point3Dd")
@Nickname("Vector3d")
@Nickname("Vec3d")
@Nickname("double3")
public class Float64_3
{
    public Float64 x = 0.0d
    public Float64 y = 0.0d
    public Float64 z = 0.0d

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0d
        this.y = 0.0d
        this.z = 0.0d
    }

    public void _init_( Float64 _x, Float64 _y, Float64 _z )
    {
        this.x = _x
        this.y = _y
        this.z = _z
    }

    public void _init_( Float64 v )
    {
        this.x = v
        this.y = v
        this.z = v
    }

    public void _init_( Float64_2 v, Float64 _z )
    {
        this.x = v.x
        this.y = v.y
        this.z = _z
    }

    # 由 Float32_3 提升
    public void _init_( Float32_3 v )
    {
        this.x = v.x.toFloat64()
        this.y = v.y.toFloat64()
        this.z = v.z.toFloat64()
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float64 _getItem_( int index )
    {
        if index == 0
        {
            ret this.x
        }
        if index == 1
        {
            ret this.y
        }
        ret this.z
    }

    void _setItem_( int index, Float64 value )
    {
        if index == 0
        {
            this.x = value
        }
        elif index == 1
        {
            this.y = value
        }
        else
        {
            this.z = value
        }
    }

    # ── 运算符重载（参数必须为 Object，内部做类型判断）────────
    override Float64_3 _add_( Object obj1 )
    {
        if obj1 is Float64_3 v
        {
            ret Float64_3( this.x + v.x, this.y + v.y, this.z + v.z )
        }
        ret this
    }

    override Float64_3 _sub_( Object obj1 )
    {
        if obj1 is Float64_3 v
        {
            ret Float64_3( this.x - v.x, this.y - v.y, this.z - v.z )
        }
        ret this
    }

    override Float64_3 _mul_( Object obj1 )
    {
        if obj1 is Float64_3 v
        {
            ret Float64_3( this.x * v.x, this.y * v.y, this.z * v.z )
        }
        if obj1 is Float64 s
        {
            ret Float64_3( this.x * s, this.y * s, this.z * s )
        }
        ret this
    }

    override Float64_3 _truediv_( Object obj1 )
    {
        if obj1 is Float64_3 v
        {
            ret Float64_3( this.x / v.x, this.y / v.y, this.z / v.z )
        }
        if obj1 is Float64 s
        {
            ret Float64_3( this.x / s, this.y / s, this.z / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float64_3 v
        {
            ret this.x == v.x && this.y == v.y && this.z == v.z
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 向量运算 ─────────────────────────────────────────
    Float64 dot( Float64_3 other )
    {
        ret this.x * other.x + this.y * other.y + this.z * other.z
    }

    Float64 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y + this.z * this.z
    }

    Float64 length()
    {
        ret Mathd.sqrt( this.lengthSquared() )
    }

    Float64_3 normalize()
    {
        Float64 len = this.length()
        if len > 0.0d
        {
            ret Float64_3( this.x / len, this.y / len, this.z / len )
        }
        ret Float64_3( 0.0d, 0.0d, 0.0d )
    }

    Float64 distance( Float64_3 other )
    {
        Float64 dx = this.x - other.x
        Float64 dy = this.y - other.y
        Float64 dz = this.z - other.z
        ret Mathd.sqrt( dx * dx + dy * dy + dz * dz )
    }

    Float64_3 cross( Float64_3 other )
    {
        ret Float64_3( this.y * other.z - this.z * other.y,
                       this.z * other.x - this.x * other.z,
                       this.x * other.y - this.y * other.x )
    }

    Float64_3 lerp( Float64_3 other, Float64 t )
    {
        ret Float64_3( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t,
                       this.z + ( other.z - this.z ) * t )
    }

    Float64_3 scale( Float64 s )
    {
        ret Float64_3( this.x * s, this.y * s, this.z * s )
    }

    Float64_3 negate()
    {
        ret Float64_3( 0.0d - this.x, 0.0d - this.y, 0.0d - this.z )
    }

    # 绕法线反射（normal 需已归一化）
    Float64_3 reflect( Float64_3 normal )
    {
        Float64 d = this.dot( normal )
        ret Float64_3( this.x - 2.0d * d * normal.x,
                       this.y - 2.0d * d * normal.y,
                       this.z - 2.0d * d * normal.z )
    }

    Float64_3 set( Float64 _x, Float64 _y, Float64 _z )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        ret this
    }

    Float64_3 clone()
    {
        ret Float64_3( this.x, this.y, this.z )
    }

    # ── 精度转换 ─────────────────────────────────────────
    Float32_3 toFloat32_3()
    {
        ret Float32_3( this.x.toFloat32(), this.y.toFloat32(), this.z.toFloat32() )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float64_3 zero()
    {
        ret Float64_3( 0.0d, 0.0d, 0.0d )
    }

    public static get Float64_3 one()
    {
        ret Float64_3( 1.0d, 1.0d, 1.0d )
    }

    public static get Float64_3 forward()
    {
        ret Float64_3( 0.0d, 0.0d, 1.0d )
    }

    public static get Float64_3 back()
    {
        ret Float64_3( 0.0d, 0.0d, -1.0d )
    }

    public static get Float64_3 up()
    {
        ret Float64_3( 0.0d, 1.0d, 0.0d )
    }

    public static get Float64_3 down()
    {
        ret Float64_3( 0.0d, -1.0d, 0.0d )
    }

    public static get Float64_3 left()
    {
        ret Float64_3( -1.0d, 0.0d, 0.0d )
    }

    public static get Float64_3 right()
    {
        ret Float64_3( 1.0d, 0.0d, 0.0d )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float64 dot( Float64_3 a, Float64_3 b )
    {
        ret a.x * b.x + a.y * b.y + a.z * b.z
    }

    public static Float64_3 cross( Float64_3 a, Float64_3 b )
    {
        ret Float64_3( a.y * b.z - a.z * b.y,
                       a.z * b.x - a.x * b.z,
                       a.x * b.y - a.y * b.x )
    }

    public static Float64 distance( Float64_3 a, Float64_3 b )
    {
        Float64 dx = a.x - b.x
        Float64 dy = a.y - b.y
        Float64 dz = a.z - b.z
        ret Mathd.sqrt( dx * dx + dy * dy + dz * dz )
    }

    public static Float64_3 lerp( Float64_3 a, Float64_3 b, Float64 t )
    {
        ret Float64_3( a.x + ( b.x - a.x ) * t,
                       a.y + ( b.y - a.y ) * t,
                       a.z + ( b.z - a.z ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1}, {2})", this.x, this.y, this.z )
    }
}
