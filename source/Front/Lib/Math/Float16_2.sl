@Nickname("PointH")
@Nickname("Vector2h")
@Nickname("Vec2h")
@Nickname("half2")
public class Float16_2
{
    public Float16 x = 0.0h
    public Float16 y = 0.0h

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0h
        this.y = 0.0h
    }

    public void _init_( Float16 _x, Float16 _y )
    {
        this.x = _x
        this.y = _y
    }

    public void _init_( Float16 v )
    {
        this.x = v
        this.y = v
    }

    # 由 Float32_2 降精度
    public void _init_( Float32_2 v )
    {
        this.x = v.x
        this.y = v.y
    }

    # ── 索引访问 ─────────────────────────────────────────
    Float16 _getItem_( int index )
    {
        if index == 0
        {
            ret this.x
        }
        ret this.y
    }

    void _setItem_( int index, Float16 value )
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
    override Float16_2 _add_( Object obj1 )
    {
        if obj1 is Float16_2 v
        {
            ret Float16_2( this.x + v.x, this.y + v.y )
        }
        ret this
    }

    override Float16_2 _sub_( Object obj1 )
    {
        if obj1 is Float16_2 v
        {
            ret Float16_2( this.x - v.x, this.y - v.y )
        }
        ret this
    }

    override Float16_2 _mul_( Object obj1 )
    {
        if obj1 is Float16_2 v
        {
            ret Float16_2( this.x * v.x, this.y * v.y )
        }
        if obj1 is Float16 s
        {
            ret Float16_2( this.x * s, this.y * s )
        }
        ret this
    }

    override Float16_2 _truediv_( Object obj1 )
    {
        if obj1 is Float16_2 v
        {
            ret Float16_2( this.x / v.x, this.y / v.y )
        }
        if obj1 is Float16 s
        {
            ret Float16_2( this.x / s, this.y / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float16_2 v
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
    Float16 dot( Float16_2 other )
    {
        ret this.x * other.x + this.y * other.y
    }

    Float16 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y
    }

    Float16 length()
    {
        ret Mathh.sqrt( this.lengthSquared() )
    }

    Float16_2 normalize()
    {
        Float16 len = this.length()
        if len > 0.0h
        {
            ret Float16_2( this.x / len, this.y / len )
        }
        ret Float16_2( 0.0h, 0.0h )
    }

    Float16 distance( Float16_2 other )
    {
        Float16 dx = this.x - other.x
        Float16 dy = this.y - other.y
        ret Mathh.sqrt( dx * dx + dy * dy )
    }

    # 2D 叉积（标量）
    Float16 cross( Float16_2 other )
    {
        ret this.x * other.y - this.y * other.x
    }

    Float16_2 lerp( Float16_2 other, Float16 t )
    {
        ret Float16_2( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t )
    }

    Float16_2 scale( Float16 s )
    {
        ret Float16_2( this.x * s, this.y * s )
    }

    Float16_2 negate()
    {
        ret Float16_2( 0.0h - this.x, 0.0h - this.y )
    }

    Float16_2 set( Float16 _x, Float16 _y )
    {
        this.x = _x
        this.y = _y
        ret this
    }

    Float16_2 clone()
    {
        ret Float16_2( this.x, this.y )
    }

    # ── 精度转换 ─────────────────────────────────────────
    Float32_2 toFloat32_2()
    {
        ret Float32_2( this.x.toFloat32(), this.y.toFloat32() )
    }

    Float64_2 toFloat64_2()
    {
        ret Float64_2( this.x.toFloat64(), this.y.toFloat64() )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float16_2 zero()
    {
        ret Float16_2( 0.0h, 0.0h )
    }

    public static get Float16_2 one()
    {
        ret Float16_2( 1.0h, 1.0h )
    }

    public static get Float16_2 up()
    {
        ret Float16_2( 0.0h, 1.0h )
    }

    public static get Float16_2 down()
    {
        ret Float16_2( 0.0h, -1.0h )
    }

    public static get Float16_2 left()
    {
        ret Float16_2( -1.0h, 0.0h )
    }

    public static get Float16_2 right()
    {
        ret Float16_2( 1.0h, 0.0h )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float16 dot( Float16_2 a, Float16_2 b )
    {
        ret a.x * b.x + a.y * b.y
    }

    public static Float16 distance( Float16_2 a, Float16_2 b )
    {
        Float16 dx = a.x - b.x
        Float16 dy = a.y - b.y
        ret Mathh.sqrt( dx * dx + dy * dy )
    }

    public static Float16_2 lerp( Float16_2 a, Float16_2 b, Float16 t )
    {
        ret Float16_2( a.x + ( b.x - a.x ) * t, a.y + ( b.y - a.y ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1})", this.x, this.y )
    }
}
