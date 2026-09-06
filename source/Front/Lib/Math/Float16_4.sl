@Nickname("F4h")
@Nickname("Vector4h")
@Nickname("Vec4h")
@Nickname("half4")
public class Float16_4
{
    public Float16 x = 0.0h
    public Float16 y = 0.0h
    public Float16 z = 0.0h
    public Float16 w = 0.0h

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0.0h
        this.y = 0.0h
        this.z = 0.0h
        this.w = 0.0h
    }

    public void _init_( Float16 _x, Float16 _y, Float16 _z, Float16 _w )
    {
        this.x = _x
        this.y = _y
        this.z = _z
        this.w = _w
    }

    public void _init_( Float16 v )
    {
        this.x = v
        this.y = v
        this.z = v
        this.w = v
    }

    public void _init_( Float16_3 v, Float16 _w )
    {
        this.x = v.x
        this.y = v.y
        this.z = v.z
        this.w = _w
    }

    public void _init_( Float32_4 v )
    {
        this.x = v.x
        this.y = v.y
        this.z = v.z
        this.w = v.w
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
        if index == 2
        {
            ret this.z
        }
        ret this.w
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
    override Float16_4 _add_( Object obj1 )
    {
        if obj1 is Float16_4 v
        {
            ret Float16_4( this.x + v.x, this.y + v.y, this.z + v.z, this.w + v.w )
        }
        ret this
    }

    override Float16_4 _sub_( Object obj1 )
    {
        if obj1 is Float16_4 v
        {
            ret Float16_4( this.x - v.x, this.y - v.y, this.z - v.z, this.w - v.w )
        }
        ret this
    }

    override Float16_4 _mul_( Object obj1 )
    {
        if obj1 is Float16_4 v
        {
            ret Float16_4( this.x * v.x, this.y * v.y, this.z * v.z, this.w * v.w )
        }
        if obj1 is Float16 s
        {
            ret Float16_4( this.x * s, this.y * s, this.z * s, this.w * s )
        }
        ret this
    }

    override Float16_4 _truediv_( Object obj1 )
    {
        if obj1 is Float16_4 v
        {
            ret Float16_4( this.x / v.x, this.y / v.y, this.z / v.z, this.w / v.w )
        }
        if obj1 is Float16 s
        {
            ret Float16_4( this.x / s, this.y / s, this.z / s, this.w / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Float16_4 v
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
    Float16 dot( Float16_4 other )
    {
        ret this.x * other.x + this.y * other.y + this.z * other.z + this.w * other.w
    }

    Float16 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y + this.z * this.z + this.w * this.w
    }

    Float16 length()
    {
        ret Mathh.sqrt( this.lengthSquared() )
    }

    Float16_4 normalize()
    {
        Float16 len = this.length()
        if len > 0.0h
        {
            ret Float16_4( this.x / len, this.y / len, this.z / len, this.w / len )
        }
        ret Float16_4( 0.0h, 0.0h, 0.0h, 0.0h )
    }

    Float16_4 lerp( Float16_4 other, Float16 t )
    {
        ret Float16_4( this.x + ( other.x - this.x ) * t,
                       this.y + ( other.y - this.y ) * t,
                       this.z + ( other.z - this.z ) * t,
                       this.w + ( other.w - this.w ) * t )
    }

    Float16_4 negate()
    {
        ret Float16_4( 0.0h - this.x, 0.0h - this.y, 0.0h - this.z, 0.0h - this.w )
    }

    Float16_4 clone()
    {
        ret Float16_4( this.x, this.y, this.z, this.w )
    }

    # ── 精度转换 ─────────────────────────────────────────
    Float32_4 toFloat32_4()
    {
        ret Float32_4( this.x.toFloat32(), this.y.toFloat32(), this.z.toFloat32(), this.w.toFloat32() )
    }

    Float64_4 toFloat64_4()
    {
        ret Float64_4( this.x.toFloat64(), this.y.toFloat64(), this.z.toFloat64(), this.w.toFloat64() )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Float16_4 zero()
    {
        ret Float16_4( 0.0h, 0.0h, 0.0h, 0.0h )
    }

    public static get Float16_4 one()
    {
        ret Float16_4( 1.0h, 1.0h, 1.0h, 1.0h )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Float16 dot( Float16_4 a, Float16_4 b )
    {
        ret a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w
    }

    public static Float16_4 lerp( Float16_4 a, Float16_4 b, Float16 t )
    {
        ret Float16_4( a.x + ( b.x - a.x ) * t,
                       a.y + ( b.y - a.y ) * t,
                       a.z + ( b.z - a.z ) * t,
                       a.w + ( b.w - a.w ) * t )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1}, {2}, {3})", this.x, this.y, this.z, this.w )
    }
}
