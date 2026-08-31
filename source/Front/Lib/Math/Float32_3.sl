@Nickname("Point3D")
@Nickname("Vector3")
@Nickname("Vec3")
@Nickname("float3")
public class Float32_3
{
    public Float32 x = 0.0f
    public Float32 y = 0.0f
    public Float32 z = 0.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0f
        this.y = 0.0f
        this.z = 0.0f
    }

    public void _init_( Float32 _x, Float32 _y, Float32 _z )
    {
        this.x = _x
        this.y = _y
        this.z = _z
    }

    public void _init_( Float32 v )
    {
        this.x = v
        this.y = v
        this.z = v
    }

    # 由 vec2 + z 构造（w 分量用于 vec3 -> vec4 提升）
    public void _init_( Float32_2 v, Float32 _z )
    {
        this.x = v.x
        this.y = v.y
        this.z = _z
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float32 _getItem_( int index )
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

    void _setItem_( int index, Float32 value )
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
    override Float32_3 _add_( Object obj1 )
    {
        if obj1 is Float32_3 v
        {
            ret Float32_3( this.x + v.x, this.y + v.y, this.z + v.z )
        }
        ret this
    }

    override Float32_3 _sub_( Object obj1 )
    {
        if obj1 is Float32_3 v
        {
            ret Float32_3( this.x - v.x, this.y - v.y, this.z - v.z )
        }
        ret this
    }

    override Float32_3 _mul_( Object obj1 )
    {
        if obj1 is Float32_3 v
        {
            ret Float32_3( this.x * v.x, this.y * v.y, this.z * v.z )
        }
        if obj1 is Float32 s
        {
            ret Float32_3( this.x * s, this.y * s, this.z * s )
        }
        ret this
    }

    override Float32_3 _truediv_( Object obj1 )
    {
        if obj1 is Float32_3 v
        {
            ret Float32_3( this.x / v.x, this.y / v.y, this.z / v.z )
        }
        if obj1 is Float32 s
        {
            ret Float32_3( this.x / s, this.y / s, this.z / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float32_3 v
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
    Float32 dot( Float32_3 other )
    {
        ret this.x * other.x + this.y * other.y + this.z * other.z
    }

    Float32 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y + this.z * this.z
    }

    Float32 length()
    {
        ret Mathf.sqrt( this.lengthSquared() )
    }

    Float32_3 normalize()
    {
        Float32 len = this.length()
        if len > 0.0f
        {
            ret Float32_3( this.x / len, this.y / len, this.z / len )
        }
        ret Float32_3( 0.0f, 0.0f, 0.0f )
    }

    Float32 distance( Float32_3 other )
    {
        Float32 dx = this.x - other.x
        Float32 dy = this.y - other.y
        Float32 dz = this.z - other.z
        ret Mathf.sqrt( dx * dx + dy * dy + dz * dz )
    }

    Float32_3 cross( Float32_3 other )
    {
        ret Float32_3( this.y * other.z - this.z * other.y,
                       this.z * other.x - this.x * other.z,
                       this.x * other.y - this.y * other.x )
    }

    Float32_3 lerp( Float32_3 other, Float32 t )
    {
        ret Float32_3( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t,
                       this.z + ( other.z - this.z ) * t )
    }

    Float32_3 scale( Float32 s )
    {
        ret Float32_3( this.x * s, this.y * s, this.z * s )
    }

    Float32_3 negate()
    {
        ret Float32_3( -this.x, -this.y, -this.z )
    }

    # 绕法线反射（normal 需已归一化）
    Float32_3 reflect( Float32_3 normal )
    {
        Float32 d = this.dot( normal )
        ret Float32_3( this.x - 2.0f * d * normal.x,
                       this.y - 2.0f * d * normal.y,
                       this.z - 2.0f * d * normal.z )
    }

    Float32_3 set( Float32 _x, Float32 _y, Float32 _z )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        ret this
    }

    Float32_3 clone()
    {
        ret Float32_3( this.x, this.y, this.z )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float32_3 zero()
    {
        ret Float32_3( 0.0f, 0.0f, 0.0f )
    }

    public static get Float32_3 one()
    {
        ret Float32_3( 1.0f, 1.0f, 1.0f )
    }

    public static get Float32_3 forward()
    {
        ret Float32_3( 0.0f, 0.0f, 1.0f )
    }

    public static get Float32_3 back()
    {
        ret Float32_3( 0.0f, 0.0f, -1.0f )
    }

    public static get Float32_3 up()
    {
        ret Float32_3( 0.0f, 1.0f, 0.0f )
    }

    public static get Float32_3 down()
    {
        ret Float32_3( 0.0f, -1.0f, 0.0f )
    }

    public static get Float32_3 left()
    {
        ret Float32_3( -1.0f, 0.0f, 0.0f )
    }

    public static get Float32_3 right()
    {
        ret Float32_3( 1.0f, 0.0f, 0.0f )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float32 dot( Float32_3 a, Float32_3 b )
    {
        ret a.x * b.x + a.y * b.y + a.z * b.z
    }

    public static Float32_3 cross( Float32_3 a, Float32_3 b )
    {
        ret Float32_3( a.y * b.z - a.z * b.y,
                       a.z * b.x - a.x * b.z,
                       a.x * b.y - a.y * b.x )
    }

    public static Float32 distance( Float32_3 a, Float32_3 b )
    {
        Float32 dx = a.x - b.x
        Float32 dy = a.y - b.y
        Float32 dz = a.z - b.z
        ret Mathf.sqrt( dx * dx + dy * dy + dz * dz )
    }

    public static Float32_3 lerp( Float32_3 a, Float32_3 b, Float32 t )
    {
        ret Float32_3( a.x + ( b.x - a.x ) * t,
                       a.y + ( b.y - a.y ) * t,
                       a.z + ( b.z - a.z ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1}, {2})", this.x, this.y, this.z )
    }
}
