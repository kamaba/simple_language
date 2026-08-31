@Nickname("Point")
@Nickname("Vector2")
@Nickname("Vec2")
@Nickname("float2")
public class Float32_2
{
    public Float32 x = 0.0f
    public Float32 y = 0.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0f
        this.y = 0.0f
    }

    public void _init_( Float32 _x, Float32 _y )
    {
        this.x = _x
        this.y = _y
    }

    public void _init_( Float32 v )
    {
        this.x = v
        this.y = v
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float32 _getItem_( int index )
    {
        if index == 0
        {
            ret this.x
        }
        ret this.y
    }

    void _setItem_( int index, Float32 value )
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
    override Float32_2 _add_( Object obj1 )
    {
        if obj1 is Float32_2 v
        {
            ret Float32_2( this.x + v.x, this.y + v.y )
        }
        ret this
    }

    override Float32_2 _sub_( Object obj1 )
    {
        if obj1 is Float32_2 v
        {
            ret Float32_2( this.x - v.x, this.y - v.y )
        }
        ret this
    }

    override Float32_2 _mul_( Object obj1 )
    {
        if obj1 is Float32_2 v
        {
            ret Float32_2( this.x * v.x, this.y * v.y )
        }
        if obj1 is Float32 s
        {
            ret Float32_2( this.x * s, this.y * s )
        }
        ret this
    }

    override Float32_2 _truediv_( Object obj1 )
    {
        if obj1 is Float32_2 v
        {
            ret Float32_2( this.x / v.x, this.y / v.y )
        }
        if obj1 is Float32 s
        {
            ret Float32_2( this.x / s, this.y / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float32_2 v
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
    Float32 dot( Float32_2 other )
    {
        ret this.x * other.x + this.y * other.y
    }

    Float32 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y
    }

    Float32 length()
    {
        ret Mathf.sqrt( this.lengthSquared() )
    }

    Float32_2 normalize()
    {
        Float32 len = this.length()
        if len > 0.0f
        {
            ret Float32_2( this.x / len, this.y / len )
        }
        ret Float32_2( 0.0f, 0.0f )
    }

    Float32 distance( Float32_2 other )
    {
        Float32 dx = this.x - other.x
        Float32 dy = this.y - other.y
        ret Mathf.sqrt( dx * dx + dy * dy )
    }

    # 2D 叉积（标量）
    Float32 cross( Float32_2 other )
    {
        ret this.x * other.y - this.y * other.x
    }

    Float32_2 lerp( Float32_2 other, Float32 t )
    {
        ret Float32_2( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t )
    }

    Float32_2 scale( Float32 s )
    {
        ret Float32_2( this.x * s, this.y * s )
    }

    Float32_2 negate()
    {
        ret Float32_2( -this.x, -this.y )
    }

    Float32_2 set( Float32 _x, Float32 _y )
    {
        this.x = _x
        this.y = _y
        ret this
    }

    Float32_2 clone()
    {
        ret Float32_2( this.x, this.y )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float32_2 zero()
    {
        ret Float32_2( 0.0f, 0.0f )
    }

    public static get Float32_2 one()
    {
        ret Float32_2( 1.0f, 1.0f )
    }

    public static get Float32_2 up()
    {
        ret Float32_2( 0.0f, 1.0f )
    }

    public static get Float32_2 down()
    {
        ret Float32_2( 0.0f, -1.0f )
    }

    public static get Float32_2 left()
    {
        ret Float32_2( -1.0f, 0.0f )
    }

    public static get Float32_2 right()
    {
        ret Float32_2( 1.0f, 0.0f )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float32 dot( Float32_2 a, Float32_2 b )
    {
        ret a.x * b.x + a.y * b.y
    }

    public static Float32 distance( Float32_2 a, Float32_2 b )
    {
        Float32 dx = a.x - b.x
        Float32 dy = a.y - b.y
        ret Mathf.sqrt( dx * dx + dy * dy )
    }

    public static Float32_2 lerp( Float32_2 a, Float32_2 b, Float32 t )
    {
        ret Float32_2( a.x + ( b.x - a.x ) * t, a.y + ( b.y - a.y ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1})", this.x, this.y )
    }
}
