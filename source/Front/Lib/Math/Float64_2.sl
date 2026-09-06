@Nickname("PointD")
@Nickname("Vector2d")
@Nickname("Vec2d")
@Nickname("double2")
public class Float64_2
{
    public Float64 x = 0.0d
    public Float64 y = 0.0d

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0d
        this.y = 0.0d
    }

    public void _init_( Float64 _x, Float64 _y )
    {
        this.x = _x
        this.y = _y
    }

    public void _init_( Float64 v )
    {
        this.x = v
        this.y = v
    }

    # 由 Float32_2 提升
    public void _init_( Float32_2 v )
    {
        this.x = v.x.toFloat64()
        this.y = v.y.toFloat64()
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float64 _getItem_( int index )
    {
        if index == 0
        {
            ret this.x
        }
        ret this.y
    }

    void _setItem_( int index, Float64 value )
    {
        if index == 0
        {
            this.x = value
        }
        else
        {
            this.y = value
        }
    }

    # ── 运算符重载（参数必须为 Object，内部做类型判断）────────
    override Float64_2 _add_( Object obj1 )
    {
        if obj1 is Float64_2 v
        {
            ret Float64_2( this.x + v.x, this.y + v.y )
        }
        ret this
    }

    override Float64_2 _sub_( Object obj1 )
    {
        if obj1 is Float64_2 v
        {
            ret Float64_2( this.x - v.x, this.y - v.y )
        }
        ret this
    }

    override Float64_2 _mul_( Object obj1 )
    {
        if obj1 is Float64_2 v
        {
            ret Float64_2( this.x * v.x, this.y * v.y )
        }
        if obj1 is Float64 s
        {
            ret Float64_2( this.x * s, this.y * s )
        }
        ret this
    }

    override Float64_2 _truediv_( Object obj1 )
    {
        if obj1 is Float64_2 v
        {
            ret Float64_2( this.x / v.x, this.y / v.y )
        }
        if obj1 is Float64 s
        {
            ret Float64_2( this.x / s, this.y / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float64_2 v
        {
            ret this.x == v.x && this.y == v.y
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 向量运算 ─────────────────────────────────────────
    Float64 dot( Float64_2 other )
    {
        ret this.x * other.x + this.y * other.y
    }

    Float64 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y
    }

    Float64 length()
    {
        ret Mathd.sqrt( this.lengthSquared() )
    }

    Float64_2 normalize()
    {
        Float64 len = this.length()
        if len > 0.0d
        {
            ret Float64_2( this.x / len, this.y / len )
        }
        ret Float64_2( 0.0d, 0.0d )
    }

    Float64 distance( Float64_2 other )
    {
        Float64 dx = this.x - other.x
        Float64 dy = this.y - other.y
        ret Mathd.sqrt( dx * dx + dy * dy )
    }

    # 2D 叉积（标量）
    Float64 cross( Float64_2 other )
    {
        ret this.x * other.y - this.y * other.x
    }

    Float64_2 lerp( Float64_2 other, Float64 t )
    {
        ret Float64_2( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t )
    }

    Float64_2 scale( Float64 s )
    {
        ret Float64_2( this.x * s, this.y * s )
    }

    Float64_2 negate()
    {
        ret Float64_2( 0.0d - this.x, 0.0d - this.y )
    }

    Float64_2 set( Float64 _x, Float64 _y )
    {
        this.x = _x
        this.y = _y
        ret this
    }

    Float64_2 clone()
    {
        ret Float64_2( this.x, this.y )
    }

    # ── 精度转换 ─────────────────────────────────────────
    Float32_2 toFloat32_2()
    {
        ret Float32_2( this.x.toFloat32(), this.y.toFloat32() )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float64_2 zero()
    {
        ret Float64_2( 0.0d, 0.0d )
    }

    public static get Float64_2 one()
    {
        ret Float64_2( 1.0d, 1.0d )
    }

    public static get Float64_2 up()
    {
        ret Float64_2( 0.0d, 1.0d )
    }

    public static get Float64_2 down()
    {
        ret Float64_2( 0.0d, -1.0d )
    }

    public static get Float64_2 left()
    {
        ret Float64_2( -1.0d, 0.0d )
    }

    public static get Float64_2 right()
    {
        ret Float64_2( 1.0d, 0.0d )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float64 dot( Float64_2 a, Float64_2 b )
    {
        ret a.x * b.x + a.y * b.y
    }

    public static Float64 distance( Float64_2 a, Float64_2 b )
    {
        Float64 dx = a.x - b.x
        Float64 dy = a.y - b.y
        ret Mathd.sqrt( dx * dx + dy * dy )
    }

    public static Float64_2 lerp( Float64_2 a, Float64_2 b, Float64 t )
    {
        ret Float64_2( a.x + ( b.x - a.x ) * t, a.y + ( b.y - a.y ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1})", this.x, this.y )
    }
}
