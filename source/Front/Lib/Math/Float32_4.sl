@Nickname("F4")
@Nickname("Vector4")
@Nickname("Vec4")
@Nickname("float4")
public class Float32_4
{
    public Float32 x = 0.0f
    public Float32 y = 0.0f
    public Float32 z = 0.0f
    public Float32 w = 0.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0f
        this.y = 0.0f
        this.z = 0.0f
        this.w = 0.0f
    }

    public void _init_( Float32 _x, Float32 _y, Float32 _z, Float32 _w )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        this.w = _w
    }

    public void _init_( Float32 v )
    {
        this.x = v
        this.y = v
        this.z = v
        this.w = v
    }

    # vec3 + w
    public void _init_( Float32_3 v, Float32 _w )
    {
        this.x = v.x
        this.y = v.y
        this.z = v.z
        this.w = _w
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
        if index == 2
        {
            ret this.z
        }
        ret this.w
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
        elif index == 2
        {
            this.z = value
        }
        else
        {
            this.w = value
        }
    }

    # ── 运算符重载（参数必须为 Object，内部做类型判断）────────
    override Float32_4 _add_( Object obj1 )
    {
        if obj1 is Float32_4 v
        {
            ret Float32_4( this.x + v.x, this.y + v.y, this.z + v.z, this.w + v.w )
        }
        ret this
    }

    override Float32_4 _sub_( Object obj1 )
    {
        if obj1 is Float32_4 v
        {
            ret Float32_4( this.x - v.x, this.y - v.y, this.z - v.z, this.w - v.w )
        }
        ret this
    }

    override Float32_4 _mul_( Object obj1 )
    {
        if obj1 is Float32_4 v
        {
            ret Float32_4( this.x * v.x, this.y * v.y, this.z * v.z, this.w * v.w )
        }
        if obj1 is Float32 s
        {
            ret Float32_4( this.x * s, this.y * s, this.z * s, this.w * s )
        }
        ret this
    }

    override Float32_4 _truediv_( Object obj1 )
    {
        if obj1 is Float32_4 v
        {
            ret Float32_4( this.x / v.x, this.y / v.y, this.z / v.z, this.w / v.w )
        }
        if obj1 is Float32 s
        {
            ret Float32_4( this.x / s, this.y / s, this.z / s, this.w / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float32_4 v
        {
            ret this.x == v.x && this.y == v.y && this.z == v.z && this.w == v.w
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 向量运算 ─────────────────────────────────────────
    Float32 dot( Float32_4 other )
    {
        ret this.x * other.x + this.y * other.y + this.z * other.z + this.w * other.w
    }

    Float32 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w
    }

    Float32 length()
    {
        ret Mathf.sqrt( this.lengthSquared() )
    }

    Float32_4 normalize()
    {
        Float32 len = this.length()
        if len > 0.0f
        {
            ret Float32_4( this.x / len, this.y / len, this.z / len, this.w / len )
        }
        ret Float32_4( 0.0f, 0.0f, 0.0f, 0.0f )
    }

    Float32_4 lerp( Float32_4 other, Float32 t )
    {
        ret Float32_4( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t,
                       this.z + ( other.z - this.z ) * t,
                       this.w + ( other.w - this.w ) * t )
    }

    Float32_4 negate()
    {
        ret Float32_4( -this.x, -this.y, -this.z, -this.w )
    }

    Float32_4 clone()
    {
        ret Float32_4( this.x, this.y, this.z, this.w )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float32_4 zero()
    {
        ret Float32_4( 0.0f, 0.0f, 0.0f, 0.0f )
    }

    public static get Float32_4 one()
    {
        ret Float32_4( 1.0f, 1.0f, 1.0f, 1.0f )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float32 dot( Float32_4 a, Float32_4 b )
    {
        ret a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w
    }

    public static Float32_4 lerp( Float32_4 a, Float32_4 b, Float32 t )
    {
        ret Float32_4( a.x + ( b.x - a.x ) * t,
                       a.y + ( b.y - a.y ) * t,
                       a.z + ( b.z - a.z ) * t,
                       a.w + ( b.w - a.w ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1}, {2}, {3})", this.x, this.y, this.z, this.w )
    }
}
