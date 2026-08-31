@Nickname("Rectangle")
@Nickname("IntRect")
public class Rect
{
    public Int32 x = 0
    public Int32 y = 0
    public Int32 width = 0
    public Int32 height = 0

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.x = 0
        this.y = 0
        this.width = 0
        this.height = 0
    }

    public void _init_( Int32 _x, Int32 _y, Int32 _width, Int32 _height )
    {
        this.x = _x
        this.y = _y
        this.width = _width
        this.height = _height
    }

    public void _init_( Int32_2 position, Int32_2 size )
    {
        this.x = position.x
        this.y = position.y
        this.width = size.x
        this.height = size.y
    }

    # ── 边界属性 ─────────────────────────────────────────
    public get Int32 left()
    {
        ret this.x
    }

    public get Int32 top()
    {
        ret this.y
    }

    public get Int32 right()
    {
        ret this.x + this.width
    }

    public get Int32 bottom()
    {
        ret this.y + this.height
    }

    public get Int32_2 position()
    {
        ret Int32_2( this.x, this.y )
    }

    public get Int32_2 size()
    {
        ret Int32_2( this.width, this.height )
    }

    public get Int32_2 min()
    {
        ret Int32_2( this.x, this.y )
    }

    public get Int32_2 max()
    {
        ret Int32_2( this.x + this.width, this.y + this.height )
    }

    # 中心点（整除）
    public get Int32_2 center()
    {
        ret Int32_2( this.x + this.width / 2, this.y + this.height / 2 )
    }

    public get Int32 area()
    {
        ret this.width * this.height
    }

    public bool isEmpty()
    {
        ret this.width <= 0 || this.height <= 0
    }

    # ── 索引访问 ─────────────────────────────────────────
    Int32 _getItem_( int index )
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
            ret this.width
        }
        ret this.height
    }

    void _setItem_( int index, Int32 value )
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
            this.width = value
        }
        else
        {
            this.height = value
        }
    }

    # ── 运算符重载 ───────────────────────────────────────
    override bool _eq_( Object obj1 )
    {
        if obj1 is Rect r
        {
            ret this.x == r.x && this.y == r.y && this.width == r.width && this.height == r.height
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 几何运算 ─────────────────────────────────────────
    public bool contains( Int32 px, Int32 py )
    {
        ret px >= this.x && px < this.x + this.width && py >= this.y && py < this.y + this.height
    }

    public bool contains( Int32_2 p )
    {
        ret this.contains( p.x, p.y )
    }

    public bool contains( Rect other )
    {
        ret other.x >= this.x && other.y >= this.y &&
            other.x + other.width <= this.x + this.width &&
            other.y + other.height <= this.y + this.height
    }

    public bool intersects( Rect other )
    {
        ret this.x < other.x + other.width &&
            this.x + this.width > other.x &&
            this.y < other.y + other.height &&
            this.y + this.height > other.y
    }

    # 交集（无交集返回空矩形）
    public Rect intersection( Rect other )
    {
        if !this.intersects( other )
        {
            ret Rect( 0, 0, 0, 0 )
        }
        Int32 x1 = Mathf.max( this.x, other.x )
        Int32 y1 = Mathf.max( this.y, other.y )
        Int32 x2 = Mathf.min( this.x + this.width, other.x + other.width )
        Int32 y2 = Mathf.min( this.y + this.height, other.y + other.height )
        ret Rect( x1, y1, x2 - x1, y2 - y1 )
    }

    # 并集（方法名避开 union 关键字）
    public Rect unionWith( Rect other )
    {
        Int32 x1 = Mathf.min( this.x, other.x )
        Int32 y1 = Mathf.min( this.y, other.y )
        Int32 x2 = Mathf.max( this.x + this.width, other.x + other.width )
        Int32 y2 = Mathf.max( this.y + this.height, other.y + other.height )
        ret Rect( x1, y1, x2 - x1, y2 - y1 )
    }

    public Rect inflate( Int32 dx, Int32 dy )
    {
        ret Rect( this.x - dx, this.y - dy, this.width + dx * 2, this.height + dy * 2 )
    }

    public Rect offset( Int32 dx, Int32 dy )
    {
        ret Rect( this.x + dx, this.y + dy, this.width, this.height )
    }

    public Rect offset( Int32_2 d )
    {
        ret this.offset( d.x, d.y )
    }

    public Rect set( Int32 _x, Int32 _y, Int32 _width, Int32 _height )
    {
        this.x = _x
        this.y = _y
        this.width = _width
        this.height = _height
        ret this
    }

    public Rect clone()
    {
        ret Rect( this.x, this.y, this.width, this.height )
    }

    # ── 静态工厂 ─────────────────────────────────────────
    public static get Rect zero()
    {
        ret Rect( 0, 0, 0, 0 )
    }

    public static Rect fromLTRB( Int32 left, Int32 top, Int32 right, Int32 bottom )
    {
        ret Rect( left, top, right - left, bottom - top )
    }

    public static Rect fromPoints( Int32_2 a, Int32_2 b )
    {
        Int32 x1 = Mathf.min( a.x, b.x )
        Int32 y1 = Mathf.min( a.y, b.y )
        Int32 x2 = Mathf.max( a.x, b.x )
        Int32 y2 = Mathf.max( a.y, b.y )
        ret Rect( x1, y1, x2 - x1, y2 - y1 )
    }

    override string toString()
    {
        ret String.toFormat( "Rect(x={0}, y={1}, w={2}, h={3})",
            this.x, this.y, this.width, this.height )
    }
}
