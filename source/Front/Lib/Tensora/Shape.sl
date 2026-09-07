# Shape —— 张量形状描述
#
# 职责：
#   1. 维度存放（dims）与元素总数（size）
#   2. 行主序（row-major）步长计算：最后一维连续
#   3. 多维索引 <-> 扁平偏移 的双向换算
#   4. 广播（broadcast）规则推导
#
# 约定：dims.length == 0 表示标量（scalar），size 为 1。

@Nickname("Shape")
public class Shape
{
    # 每一维的长度；长度 0 视为标量
    public Array<Int32> dims = null

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.dims = Array<Int32>( 0 )
    }

    public void _init_( Array<Int32> _dims )
    {
        this.dims = Array<Int32>( _dims.length )
        for i = 0, i < _dims.length, i++
        {
            this.dims[i] = _dims[i]
        }
    }

    public void _init_( Int32 d0 )
    {
        this.dims = Array<Int32>( 1 )
        this.dims[0] = d0
    }

    public void _init_( Int32 d0, Int32 d1 )
    {
        this.dims = Array<Int32>( 2 )
        this.dims[0] = d0
        this.dims[1] = d1
    }

    public void _init_( Int32 d0, Int32 d1, Int32 d2 )
    {
        this.dims = Array<Int32>( 3 )
        this.dims[0] = d0
        this.dims[1] = d1
        this.dims[2] = d2
    }

    public void _init_( Int32 d0, Int32 d1, Int32 d2, Int32 d3 )
    {
        this.dims = Array<Int32>( 4 )
        this.dims[0] = d0
        this.dims[1] = d1
        this.dims[2] = d2
        this.dims[3] = d3
    }

    # ── 基本属性 ─────────────────────────────────────────
    get int rank()
    {
        ret this.dims.length
    }

    get int size()
    {
        int n = 1
        for i = 0, i < this.dims.length, i++
        {
            n = n * this.dims[i]
        }
        ret n
    }

    get bool isScalar()
    {
        ret this.dims.length == 0
    }

    get bool isVector()
    {
        ret this.dims.length == 1
    }

    get bool isMatrix()
    {
        ret this.dims.length == 2
    }

    Int32 get( int axis )
    {
        ret this.dims[axis]
    }

    void set( int axis, Int32 value )
    {
        this.dims[axis] = value
    }

    # ── 步长：strides[i] = dims[i+1] * ... * dims[last] ──
    Array<Int32> strides()
    {
        int n = this.dims.length
        Array<Int32> st = Array<Int32>( n )
        int acc = 1
        for i = n - 1, i >= 0, i--
        {
            st[i] = acc
            acc = acc * this.dims[i]
        }
        ret st
    }

    # ── 索引换算 ─────────────────────────────────────────
    # 多维索引 -> 扁平偏移（行主序）
    int offset( Array<Int32> index )
    {
        Array<Int32> st = this.strides()
        int off = 0
        for i = 0, i < this.dims.length, i++
        {
            off = off + index[i] * st[i]
        }
        ret off
    }

    # 扁平偏移 -> 多维索引
    Array<Int32> unflatten( int flatIndex )
    {
        int n = this.dims.length
        Array<Int32> idx = Array<Int32>( n )
        int rest = flatIndex
        for i = n - 1, i >= 0, i--
        {
            int d = this.dims[i]
            idx[i] = rest % d
            rest = rest / d
        }
        ret idx
    }

    # ── 比较 / 拷贝 ──────────────────────────────────────
    bool equals( Shape other )
    {
        if other == null
        {
            ret false
        }
        if this.dims.length != other.dims.length
        {
            ret false
        }
        for i = 0, i < this.dims.length, i++
        {
            if this.dims[i] != other.dims[i]
            {
                ret false
            }
        }
        ret true
    }

    override bool _eq_( Object obj1 )
    {
        if obj1 is Shape other
        {
            ret this.equals( other )
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    Shape clone()
    {
        ret Shape( this.dims )
    }

    # 元素总数一致即可 reshape
    bool canReshape( Array<Int32> newDims )
    {
        Shape s = Shape( newDims )
        ret s.size() == this.size()
    }

    # ── 广播 ─────────────────────────────────────────────
    # 规则：从尾维对齐，维度相等或其中一个为 1 则可广播
    static Shape broadcastShape( Shape a, Shape b )
    {
        int ra = a.rank()
        int rb = b.rank()
        int n = ra
        if rb > n
        {
            n = rb
        }
        Array<Int32> out = Array<Int32>( n )
        for k = 0, k < n, k++
        {
            int da = 1
            int db = 1
            if k >= n - ra
            {
                da = a.dims[ k - ( n - ra ) ]
            }
            if k >= n - rb
            {
                db = b.dims[ k - ( n - rb ) ]
            }
            if da == db
            {
                out[k] = da
            }
            elif da == 1
            {
                out[k] = db
            }
            elif db == 1
            {
                out[k] = da
            }
            else
            {
                # 不可广播：返回 null，由调用方处理
                ret null
            }
        }
        ret Shape( out )
    }

    # ── 工厂 ─────────────────────────────────────────────
    public static get Shape scalar()
    {
        ret Shape()
    }

    public static Shape vector( Int32 n )
    {
        ret Shape( n )
    }

    public static Shape matrix( Int32 rows, Int32 cols )
    {
        ret Shape( rows, cols )
    }

    public static Shape cube( Int32 d0, Int32 d1, Int32 d2 )
    {
        ret Shape( d0, d1, d2 )
    }

    public static Shape from( Array<Int32> dims )
    {
        ret Shape( dims )
    }

    override string toString()
    {
        string s = "("
        for i = 0, i < this.dims.length, i++
        {
            if i > 0
            {
                s = s + ","
            }
            s = s + SystemConvertString( this.dims[i] )
        }
        s = s + ")"
        ret s
    }
}
