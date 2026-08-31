@Nickname("VectorInt2")
@Nickname("VecInt2")
@Nickname("int2")
public class Int32_2
{
    public Int32 x = 0
    public Int32 y = 0

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0
        this.y = 0
    }

    public void _init_( Int32 _x, Int32 _y )
    {
        this.x = _x
        this.y = _y
    }

    public void _init_( Int32 v )
    {
        this.x = v
        this.y = v
    }

    # ── 索引访问 ─────────────────────────────────────────
    Int32 _getItem_( int index )
    {
        if index == 0
        {
            ret this.x
        }
        ret this.y
    }

    void _setItem_( int index, Int32 value )
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
    override Int32_2 _add_( Object obj1 )
    {
        if obj1 is Int32_2 v
        {
            ret Int32_2( this.x + v.x, this.y + v.y )
        }
        ret this
    }

    override Int32_2 _sub_( Object obj1 )
    {
        if obj1 is Int32_2 v
        {
            ret Int32_2( this.x - v.x, this.y - v.y )
        }
        ret this
    }

    override Int32_2 _mul_( Object obj1 )
    {
        if obj1 is Int32_2 v
        {
            ret Int32_2( this.x * v.x, this.y * v.y )
        }
        if obj1 is Int32 s
        {
            ret Int32_2( this.x * s, this.y * s )
        }
        ret this
    }

    override Int32_2 _truediv_( Object obj1 )
    {
        if obj1 is Int32 s
        {
            ret Int32_2( this.x / s, this.y / s )
        }
        ret this
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Int32_2 v
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
    Int32 dot( Int32_2 other )
    {
        ret this.x * other.x + this.y * other.y
    }

    # 曼哈顿距离
    Int32 manhattan( Int32_2 other )
    {
        Int32 dx = this.x - other.x
        Int32 dy = this.y - other.y
        if dx < 0
        {
            dx = -dx
        }
        if dy < 0
        {
            dy = -dy
        }
        ret dx + dy
    }

    Float32 length()
    {
        ret Mathf.sqrt( ( this.x * this.x + this.y * this.y ).toFloat32() )
    }

    Int32 lengthSquared()
    {
        ret this.x * this.x + this.y * this.y
    }

    Int32_2 negate()
    {
        ret Int32_2( -this.x, -this.y )
    }

    Int32_2 set( Int32 _x, Int32 _y )
    {
        this.x = _x
        this.y = _y
        ret this
    }

    Int32_2 clone()
    {
        ret Int32_2( this.x, this.y )
    }

    # ── 静态常量 ─────────────────────────────────────────
    public static get Int32_2 zero()
    {
        ret Int32_2( 0, 0 )
    }

    public static get Int32_2 one()
    {
        ret Int32_2( 1, 1 )
    }

    public static get Int32_2 up()
    {
        ret Int32_2( 0, 1 )
    }

    public static get Int32_2 down()
    {
        ret Int32_2( 0, -1 )
    }

    public static get Int32_2 left()
    {
        ret Int32_2( -1, 0 )
    }

    public static get Int32_2 right()
    {
        ret Int32_2( 1, 0 )
    }

    # ── 静态工具 ─────────────────────────────────────────
    public static Int32 dot( Int32_2 a, Int32_2 b )
    {
        ret a.x * b.x + a.y * b.y
    }

    public static Int32 manhattan( Int32_2 a, Int32_2 b )
    {
        ret a.manhattan( b )
    }

    override string toString()
    {
        ret String.toFormat( "({0}, {1})", this.x, this.y )
    }
}
