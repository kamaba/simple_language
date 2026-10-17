# 轴对齐包围盒（AABB）。
# 使用 Math 库的 Float32_3 表示 min / max。
public class Bounds
{
    public Float32_3 min = Float32_3.zero()
    public Float32_3 max = Float32_3.zero()

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.min = Float32_3.zero()
        this.max = Float32_3.zero()
    }

    public void _init_( Float32_3 _min, Float32_3 _max )
    {
        this.min = _min.clone()
        this.max = _max.clone()
    }

    # ── 基本属性 ─────────────────────────────────────────
    public get Float32_3 center()
    {
        ret this.min._add_( this.max )._mul_( 0.5f ) as Float32_3
    }

    public get Float32_3 size()
    {
        ret this.max._sub_( this.min ) as Float32_3
    }

    # 半尺寸
    public get Float32_3 extents()
    {
        ret this.size()._mul_( 0.5f ) as Float32_3
    }

    # max 各分量均 >= min 视为有效
    public get bool isValid()
    {
        ret this.max.x >= this.min.x && this.max.y >= this.min.y && this.max.z >= this.min.z
    }

    # ── 运算符重载 ───────────────────────────────────────
    override bool _eq_( Object obj1 )
    {
        if obj1 is Bounds b
        {
            ret this.min._eq_( b.min ) && this.max._eq_( b.max )
        }
        ret false
    }

    override bool _ne_( Object obj1 )
    {
        ret !this._eq_( obj1 )
    }

    # ── 包含 / 相交 ──────────────────────────────────────
    public bool contains( Float32_3 p )
    {
        ret p.x >= this.min.x && p.x <= this.max.x &&
            p.y >= this.min.y && p.y <= this.max.y &&
            p.z >= this.min.z && p.z <= this.max.z
    }

    public bool intersects( Bounds other )
    {
        ret this.min.x <= other.max.x && this.max.x >= other.min.x &&
            this.min.y <= other.max.y && this.max.y >= other.min.y &&
            this.min.z <= other.max.z && this.max.z >= other.min.z
    }

    # ── 扩展 ─────────────────────────────────────────────
    # 扩展以包含某个点
    public void encapsulate( Float32_3 p )
    {
        this.min = Vector.min3( this.min, p )
        this.max = Vector.max3( this.max, p )
    }

    # 扩展以包含另一个包围盒
    public void encapsulate( Bounds other )
    {
        this.encapsulate( other.min )
        this.encapsulate( other.max )
    }

    # 各方向均匀外扩
    public Bounds expand( Float32 amount )
    {
        Float32_3 e = Float32_3( amount, amount, amount )
        ret Bounds( this.min._sub_( e ) as Float32_3, this.max._add_( e ) as Float32_3 )
    }

    # 按向量外扩
    public Bounds expand( Float32_3 amount )
    {
        ret Bounds( this.min._sub_( amount ) as Float32_3, this.max._add_( amount ) as Float32_3 )
    }

    # ── 查询 ─────────────────────────────────────────────
    # 盒内（或边界上）距 p 最近的点
    public Float32_3 closestPoint( Float32_3 p )
    {
        Float32 x = Mathf.clamp( p.x, this.min.x, this.max.x )
        Float32 y = Mathf.clamp( p.y, this.min.y, this.max.y )
        Float32 z = Mathf.clamp( p.z, this.min.z, this.max.z )
        ret Float32_3( x, y, z )
    }

    # 外部点到盒表面的距离（点在盒内返回 0）
    public Float32 distanceTo( Float32_3 p )
    {
        Float32_3 closest = this.closestPoint( p )
        ret closest.distance( p )
    }

    # 是否被射线命中（slab 算法）
    public bool intersectsRay( Ray ray )
    {
        ret this.intersectRayDistance( ray ) >= 0.0f
    }

    # 射线进入距离；未命中返回 -1
    public Float32 intersectRayDistance( Ray ray )
    {
        Float32 tmin = 0.0f
        Float32 tmax = Float32.MaxValue

        # X
        if Mathf.abs( ray.direction.x ) < 0.000001f
        {
            if ray.origin.x < this.min.x || ray.origin.x > this.max.x
            {
                ret -1.0f
            }
        }
        else
        {
            Float32 inv = 1.0f / ray.direction.x
            Float32 t1 = ( this.min.x - ray.origin.x ) * inv
            Float32 t2 = ( this.max.x - ray.origin.x ) * inv
            if t1 > t2
            {
                Float32 tmp = t1
                t1 = t2
                t2 = tmp
            }
            tmin = Mathf.max( tmin, t1 )
            tmax = Mathf.min( tmax, t2 )
            if tmin > tmax
            {
                ret -1.0f
            }
        }

        # Y
        if Mathf.abs( ray.direction.y ) < 0.000001f
        {
            if ray.origin.y < this.min.y || ray.origin.y > this.max.y
            {
                ret -1.0f
            }
        }
        else
        {
            Float32 inv = 1.0f / ray.direction.y
            Float32 t1 = ( this.min.y - ray.origin.y ) * inv
            Float32 t2 = ( this.max.y - ray.origin.y ) * inv
            if t1 > t2
            {
                Float32 tmp = t1
                t1 = t2
                t2 = tmp
            }
            tmin = Mathf.max( tmin, t1 )
            tmax = Mathf.min( tmax, t2 )
            if tmin > tmax
            {
                ret -1.0f
            }
        }

        # Z
        if Mathf.abs( ray.direction.z ) < 0.000001f
        {
            if ray.origin.z < this.min.z || ray.origin.z > this.max.z
            {
                ret -1.0f
            }
        }
        else
        {
            Float32 inv = 1.0f / ray.direction.z
            Float32 t1 = ( this.min.z - ray.origin.z ) * inv
            Float32 t2 = ( this.max.z - ray.origin.z ) * inv
            if t1 > t2
            {
                Float32 tmp = t1
                t1 = t2
                t2 = tmp
            }
            tmin = Mathf.max( tmin, t1 )
            tmax = Mathf.min( tmax, t2 )
            if tmin > tmax
            {
                ret -1.0f
            }
        }

        ret tmin
    }

    public Bounds clone()
    {
        ret Bounds( this.min.clone(), this.max.clone() )
    }

    # ── 静态工厂 ─────────────────────────────────────────
    public static Bounds fromCenterSize( Float32_3 center, Float32_3 size )
    {
        Float32_3 half = size._mul_( 0.5f ) as Float32_3
        ret Bounds( center._sub_( half ) as Float32_3, center._add_( half ) as Float32_3 )
    }

    # 由点集构造（points 为空时返回无效盒）
    public static Bounds fromPoints( Array<Float32_3> points )
    {
        if points == null || points.length == 0
        {
            ret Bounds()
        }
        Float32_3 first = points[0]
        Float32_3 lo = first.clone()
        Float32_3 hi = first.clone()
        int i = 1
        while i < points.length
        {
            lo = Vector.min3( lo, points[i] )
            hi = Vector.max3( hi, points[i] )
            i++
        }
        ret Bounds( lo, hi )
    }

    public static get Bounds empty()
    {
        ret Bounds()
    }

    override string toString()
    {
        ret String.toFormat( "Bounds(min={0}, max={1})", this.min.toString(), this.max.toString() )
    }
}
