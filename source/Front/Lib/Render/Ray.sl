# 射线：origin + direction（direction 建议保持单位长度）。
# 使用 Math 库的 Float32_3 / Float32_4x4。
public class Ray
{
    public Float32_3 origin = Float32_3.zero()
    public Float32_3 direction = Float32_3.forward()

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.origin = Float32_3.zero()
        this.direction = Float32_3.forward()
    }

    public void _init_( Float32_3 _origin, Float32_3 _direction )
    {
        this.origin = _origin.clone()
        Float32 len = _direction.length()
        if len > 0.0f
        {
            this.direction = _direction.normalize()
        }
        else
        {
            this.direction = Float32_3.forward()
        }
    }

    # ── 查询 ─────────────────────────────────────────────
    # 沿射线前进 distance 后的点
    public Float32_3 getPoint( Float32 distance )
    {
        ret this.origin._add_( this.direction.scale( distance ) ) as Float32_3
    }

    # 与包围盒求交，返回进入距离；未命中返回 -1
    public Float32 intersectBounds( Bounds b )
    {
        ret b.intersectRayDistance( this )
    }

    public bool intersectsBounds( Bounds b )
    {
        ret b.intersectsRay( this )
    }

    # 与平面求交，返回沿射线的距离；平行或背向返回 -1
    public Float32 intersectPlane( Plane p )
    {
        Float32 denom = p.normal.dot( this.direction )
        if Mathf.abs( denom ) < 0.000001f
        {
            ret -1.0f
        }
        Float32 t = 0.0f - ( p.normal.dot( this.origin ) + p.distance ) / denom
        if t < 0.0f
        {
            ret -1.0f
        }
        ret t
    }

    # 与平面求交点；不相交返回 null
    public Float32_3 intersectPlanePoint( Plane p )
    {
        Float32 t = this.intersectPlane( p )
        if t < 0.0f
        {
            ret null
        }
        ret this.getPoint( t )
    }

    # ── 变换 ─────────────────────────────────────────────
    public Ray transformBy( Float32_4x4 m )
    {
        Float32_3 o = m.transformPoint( this.origin )
        Float32_3 d = m.transformDirection( this.direction ).normalize()
        ret Ray( o, d )
    }

    public Ray clone()
    {
        ret Ray( this.origin, this.direction )
    }

    # ── 静态工厂 ─────────────────────────────────────────
    # 由两点构造（from -> to，方向自动归一化）
    public static Ray fromTo( Float32_3 from, Float32_3 to )
    {
        ret Ray( from, to._sub_( from ) as Float32_3 )
    }

    override string toString()
    {
        ret String.toFormat( "Ray(origin={0}, dir={1})", this.origin.toString(), this.direction.toString() )
    }
}
