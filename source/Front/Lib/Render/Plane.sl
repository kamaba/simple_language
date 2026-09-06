# 平面：法线 normal + 到原点距离 distance。
# 平面方程：dot(normal, p) + distance = 0
# 使用 Math 库的 Float32_3。
public class Plane
{
    public Float32_3 normal = Float32_3.up()
    public Float32 distance = 0.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.normal = Float32_3.up()
        this.distance = 0.0f
    }

    # normal 需为单位向量；此处自动归一化
    public void _init_( Float32_3 _normal, Float32 _distance )
    {
        Float32 len = _normal.length()
        if len > 0.0f
        {
            this.normal = _normal.normalize()
            this.distance = _distance / len
        }
        else
        {
            this.normal = Float32_3.up()
            this.distance = _distance
        }
    }

    # 由法线与平面上一点构造
    public void _init_( Float32_3 _normal, Float32_3 point, bool isPointOnPlane )
    {
        Float32_3 n = _normal.normalize()
        this.normal = n
        this.distance = 0.0f - n.dot( point )
    }

    # ── 查询 ─────────────────────────────────────────────
    # 有符号距离：正表示在法线一侧，负表示背面
    public Float32 distanceToPoint( Float32_3 p )
    {
        ret this.normal.dot( p ) + this.distance
    }

    # 点是否在法线正侧
    public bool getSide( Float32_3 p )
    {
        ret this.distanceToPoint( p ) >= 0.0f
    }

    # 平面上距离 p 最近的点
    public Float32_3 closestPointOnPlane( Float32_3 p )
    {
        Float32 d = this.distanceToPoint( p )
        ret p._sub_( this.normal.scale( d ) ) as Float32_3
    }

    # 与射线求交，返回沿射线的距离；不相交返回 -1
    public Float32 raycast( Ray ray )
    {
        ret ray.intersectPlane( this )
    }

    # ── 变换 ─────────────────────────────────────────────
    # 翻转法线朝向
    public Plane flip()
    {
        ret Plane( this.normal.negate(), 0.0f - this.distance )
    }

    # 沿法线方向平移 delta（正表示朝法线方向移动）
    public Plane translate( Float32 delta )
    {
        ret Plane( this.normal, this.distance - delta )
    }

    public Plane clone()
    {
        ret Plane( this.normal, this.distance )
    }

    # ── 静态工厂 ─────────────────────────────────────────
    public static Plane fromNormalAndPoint( Float32_3 normal, Float32_3 point )
    {
        ret Plane( normal, point, true )
    }

    # 三点定平面（a -> b -> c 按右手定则确定法线）
    public static Plane fromThreePoints( Float32_3 a, Float32_3 b, Float32_3 c )
    {
        Float32_3 ab = b._sub_( a ) as Float32_3
        Float32_3 ac = c._sub_( a ) as Float32_3
        Float32_3 n = ab.cross( ac )
        Float32 len = n.length()
        if len <= 0.0f
        {
            ret Plane()
        }
        n = n.normalize()
        ret Plane( n, 0.0f - n.dot( a ) )
    }

    # 常用：地平面 y = height
    public static Plane ground( Float32 height )
    {
        ret Plane( Float32_3.up(), height )
    }

    override string toString()
    {
        ret String.toFormat( "Plane(normal={0}, d={1})", this.normal.toString(), this.distance.toString() )
    }
}
