@Nickname("Point3Dh")
@Nickname("Vector3h")
@Nickname("Vec3h")
@Nickname("half3")
public class Float16_3
{
    public Float16 x = 0.0h
    public Float16 y = 0.0h
    public Float16 z = 0.0h

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0h
        this.y = 0.0h
        this.z = 0.0h
    }

    public void _init_( Float16 _x, Float16 _y, Float16 _z )
    {
        this.x = _x
        this.y = _y
        this.z = _z
    }

    public void _init_( Float16 v )
    {
        this.x = v
        this.y = v
        this.z = v
    }

    public void _init_( Float16_2 v, Float16 _z )
    {
        this.x = v.x
        this.y = v.y
        this.z = _z
    }

    # 由 Float32_3 降精度
    public void _init_( Float32_3 v )
    {
        this.x = v.x
        this.y = v.y
        this.z = v.z
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float16 _getItem_( int index )
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

    void _setItem_( int index, Float16 value )
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
    override Float16_3 _add_( Object obj1 )
    {
        if obj1 is Float16_3 v
        {
            ret Float16_3( this.x + v.x, this.y + v.y, this.z + v.z )
        }
        ret this
    }

    override Float16_3 _sub_( Object obj1 )
    {
        if obj1 is Float16_3 v
        {
            ret Float16_3( this.x - v.x, this.y - v.y, this.z - v.z )
        }
        ret this
    }

    override Float16_3 _mul_( Object obj1 )
    {
        if obj1 is Float16_3 v
        {
            ret Float16_3( this.x * v.x, this.y * v.y, this.z * v.z )
        }
        if obj1 is Float16 s
        {
            ret Float16_3( this.x * s, this.y * s, this.z * s )
        }
        ret this
    }

    override Float16_3 _truediv_( Object obj1 )
    {
        if obj1 is Float16_3 v
        {
            ret Float16_3( this.x / v.x, this.y / v.y, this.z / v.z )
        }
        if obj1 is Float16 s
        {
            ret Float16_3( this.x / s, this.y / s, this.z / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float16_3 v
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
    Float16 dot( Float16_3 other )
    {
        ret this.x * other.x + this.y * other.y + this.z * other.z
    }

    Float16 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y + this.z * this.z
    }

    Float16 length()
    {
        ret Mathh.sqrt( this.lengthSquared() )
    }

    Float16_3 normalize()
    {
        Float16 len = this.length()
        if len > 0.0h
        {
            ret Float16_3( this.x / len, this.y / len, this.z / len )
        }
        ret Float16_3( 0.0h, 0.0h, 0.0h )
    }

    Float16 distance( Float16_3 other )
    {
        Float16 dx = this.x - other.x
        Float16 dy = this.y - other.y
        Float16 dz = this.z - other.z
        ret Mathh.sqrt( dx * dx + dy * dy + dz * dz )
    }

    Float16_3 cross( Float16_3 other )
    {
        ret Float16_3( this.y * other.z - this.z * other.y,
                       this.z * other.x - this.x * other.z,
                       this.x * other.y - this.y * other.x )
    }

    Float16_3 lerp( Float16_3 other, Float16 t )
    {
        ret Float16_3( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t,
                       this.z + ( other.z - this.z ) * t )
    }

    Float16_3 scale( Float16 s )
    {
        ret Float16_3( this.x * s, this.y * s, this.z * s )
    }

    Float16_3 negate()
    {
        ret Float16_3( 0.0h - this.x, 0.0h - this.y, 0.0h - this.z )
    }

    # 绕法线反射（normal 需已归一化）
    Float16_3 reflect( Float16_3 normal )
    {
        Float16 d = this.dot( normal )
        ret Float16_3( this.x - 2.0h * d * normal.x,
                       this.y - 2.0h * d * normal.y,
                       this.z - 2.0h * d * normal.z )
    }

    Float16_3 set( Float16 _x, Float16 _y, Float16 _z )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        ret this
    }

    Float16_3 clone()
    {
        ret Float16_3( this.x, this.y, this.z )
    }

    # ── 精度转换 ─────────────────────────────────────────
    Float32_3 toFloat32_3()
    {
        ret Float32_3( this.x.toFloat32(), this.y.toFloat32(), this.z.toFloat32() )
    }

    Float64_3 toFloat64_3()
    {
        ret Float64_3( this.x.toFloat64(), this.y.toFloat64(), this.z.toFloat64() )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float16_3 zero()
    {
        ret Float16_3( 0.0h, 0.0h, 0.0h )
    }

    public static get Float16_3 one()
    {
        ret Float16_3( 1.0h, 1.0h, 1.0h )
    }

    public static get Float16_3 forward()
    {
        ret Float16_3( 0.0h, 0.0h, 1.0h )
    }

    public static get Float16_3 back()
    {
        ret Float16_3( 0.0h, 0.0h, -1.0h )
    }

    public static get Float16_3 up()
    {
        ret Float16_3( 0.0h, 1.0h, 0.0h )
    }

    public static get Float16_3 down()
    {
        ret Float16_3( 0.0h, -1.0h, 0.0h )
    }

    public static get Float16_3 left()
    {
        ret Float16_3( -1.0h, 0.0h, 0.0h )
    }

    public static get Float16_3 right()
    {
        ret Float16_3( 1.0h, 0.0h, 0.0h )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float16 dot( Float16_3 a, Float16_3 b )
    {
        ret a.x * b.x + a.y * b.y + a.z * b.z
    }

    public static Float16_3 cross( Float16_3 a, Float16_3 b )
    {
        ret Float16_3( a.y * b.z - a.z * b.y,
                       a.z * b.x - a.x * b.z,
                       a.x * b.y - a.y * b.x )
    }

    public static Float16 distance( Float16_3 a, Float16_3 b )
    {
        Float16 dx = a.x - b.x
        Float16 dy = a.y - b.y
        Float16 dz = a.z - b.z
        ret Mathh.sqrt( dx * dx + dy * dy + dz * dz )
    }

    public static Float16_3 lerp( Float16_3 a, Float16_3 b, Float16 t )
    {
        ret Float16_3( a.x + ( b.x - a.x ) * t,
                       a.y + ( b.y - a.y ) * t,
                       a.z + ( b.z - a.z ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1}, {2})", this.x, this.y, this.z )
    }
}
