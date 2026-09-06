@Nickname("F4d")
@Nickname("Vector4d")
@Nickname("Vec4d")
@Nickname("double4")
public class Float64_4
{
    public Float64 x = 0.0d
    public Float64 y = 0.0d
    public Float64 z = 0.0d
    public Float64 w = 0.0d

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0d
        this.y = 0.0d
        this.z = 0.0d
        this.w = 0.0d
    }

    public void _init_( Float64 _x, Float64 _y, Float64 _z, Float64 _w )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        this.w = _w
    }

    public void _init_( Float64 v )
    {
        this.x = v
        this.y = v
        this.z = v
        this.w = v
    }

    public void _init_( Float64_3 v, Float64 _w )
    {
        this.x = v.x
        this.y = v.y
        this.z = v.z
        this.w = _w
    }

    public void _init_( Float32_4 v )
    {
        this.x = v.x.toFloat64()
        this.y = v.y.toFloat64()
        this.z = v.z.toFloat64()
        this.w = v.w.toFloat64()
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
        if index == 2
        {
            ret this.z
        }
        ret this.w
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
    override Float64_4 _add_( Object obj1 )
    {
        if obj1 is Float64_4 v
        {
            ret Float64_4( this.x + v.x, this.y + v.y, this.z + v.z, this.w + v.w )
        }
        ret this
    }

    override Float64_4 _sub_( Object obj1 )
    {
        if obj1 is Float64_4 v
        {
            ret Float64_4( this.x - v.x, this.y - v.y, this.z - v.z, this.w - v.w )
        }
        ret this
    }

    override Float64_4 _mul_( Object obj1 )
    {
        if obj1 is Float64_4 v
        {
            ret Float64_4( this.x * v.x, this.y * v.y, this.z * v.z, this.w * v.w )
        }
        if obj1 is Float64 s
        {
            ret Float64_4( this.x * s, this.y * s, this.z * s, this.w * s )
        }
        ret this
    }

    override Float64_4 _truediv_( Object obj1 )
    {
        if obj1 is Float64_4 v
        {
            ret Float64_4( this.x / v.x, this.y / v.y, this.z / v.z, this.w / v.w )
        }
        if obj1 is Float64 s
        {
            ret Float64_4( this.x / s, this.y / s, this.z / s, this.w / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float64_4 v
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
    Float64 dot( Float64_4 other )
    {
        ret this.x * other.x + this.y * other.y + this.z * other.z + this.w * other.w
    }

    Float64 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w
    }

    Float64 length()
    {
        ret Mathd.sqrt( this.lengthSquared() )
    }

    Float64_4 normalize()
    {
        Float64 len = this.length()
        if len > 0.0d
        {
            ret Float64_4( this.x / len, this.y / len, this.z / len, this.w / len )
        }
        ret Float64_4( 0.0d, 0.0d, 0.0d, 0.0d )
    }

    Float64_4 lerp( Float64_4 other, Float64 t )
    {
        ret Float64_4( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t,
                       this.z + ( other.z - this.z ) * t,
                       this.w + ( other.w - this.w ) * t )
    }

    Float64_4 negate()
    {
        ret Float64_4( 0.0d - this.x, 0.0d - this.y, 0.0d - this.z, 0.0d - this.w )
    }

    Float64_4 clone()
    {
        ret Float64_4( this.x, this.y, this.z, this.w )
    }

    Float32_4 toFloat32_4()
    {
        ret Float32_4( this.x.toFloat32(), this.y.toFloat32(), this.z.toFloat32(), this.w.toFloat32() )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float64_4 zero()
    {
        ret Float64_4( 0.0d, 0.0d, 0.0d, 0.0d )
    }

    public static get Float64_4 one()
    {
        ret Float64_4( 1.0d, 1.0d, 1.0d, 1.0d )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float64 dot( Float64_4 a, Float64_4 b )
    {
        ret a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w
    }

    public static Float64_4 lerp( Float64_4 a, Float64_4 b, Float64 t )
    {
        ret Float64_4( a.x + ( b.x - a.x ) * t,
                       a.y + ( b.y - a.y ) * t,
                       a.z + ( b.z - a.z ) * t,
                       a.w + ( b.w - a.w ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1}, {2}, {3})", this.x, this.y, this.z, this.w )
    }
}
