# Render 层向量工具。
#
# 约定：Render 库统一使用 Math 库的 **Float32** 系列类型
#   向量 -> Float32_2 / Float32_3 / Float32_4
#   矩阵 -> Float32_3x3 / Float32_4x4
#   函数 -> Mathf.*
#   旋转 -> Quaternion
public class Vector
{
    # ── 构造工厂 ─────────────────────────────────────────
    public static Float32_2 vec2( Float32 x, Float32 y )
    {
        ret Float32_2( x, y )
    }

    public static Float32_3 vec3( Float32 x, Float32 y, Float32 z )
    {
        ret Float32_3( x, y, z )
    }

    public static Float32_3 vec3( Float32_2 v, Float32 z )
    {
        ret Float32_3( v.x, v.y, z )
    }

    public static Float32_4 vec4( Float32 x, Float32 y, Float32 z, Float32 w )
    {
        ret Float32_4( x, y, z, w )
    }

    public static Float32_4 vec4( Float32_3 v, Float32 w )
    {
        ret Float32_4( v.x, v.y, v.z, w )
    }

    # ── 常用常量 ─────────────────────────────────────────
    public static get Float32_2 zero2()
    {
        ret Float32_2.zero()
    }

    public static get Float32_2 one2()
    {
        ret Float32_2.one()
    }

    public static get Float32_3 zero()
    {
        ret Float32_3.zero()
    }

    public static get Float32_3 one()
    {
        ret Float32_3.one()
    }

    # 前向：(0, 0, 1)
    public static get Float32_3 forward()
    {
        ret Float32_3.forward()
    }

    public static get Float32_3 back()
    {
        ret Float32_3.back()
    }

    public static get Float32_3 up()
    {
        ret Float32_3.up()
    }

    public static get Float32_3 down()
    {
        ret Float32_3.down()
    }

    public static get Float32_3 left()
    {
        ret Float32_3.left()
    }

    public static get Float32_3 right()
    {
        ret Float32_3.right()
    }

    # ── 几何工具 ─────────────────────────────────────────
    # 两向量夹角（弧度，无符号）
    public static Float32 angle( Float32_3 from, Float32_3 to )
    {
        Float32_3 a = from.normalize()
        Float32_3 b = to.normalize()
        Float32 d = Mathf.clamp( a.dot( b ), -1.0f, 1.0f )
        ret Mathf.acos( d )
    }

    # 带符号夹角（绕 axis，右手定则）
    public static Float32 signedAngle( Float32_3 from, Float32_3 to, Float32_3 axis )
    {
        Float32 unsigned = Vector.angle( from, to )
        Float32_3 cross = from.cross( to )
        Float32 s = Mathf.sign( cross.dot( axis ) )
        ret unsigned * s
    }

    public static Float32 distance( Float32_3 a, Float32_3 b )
    {
        ret a.distance( b )
    }

    public static Float32_3 lerp( Float32_3 a, Float32_3 b, Float32 t )
    {
        ret Float32_3.lerp( a, b, t )
    }

    # t 会被限制在 [0,1]
    public static Float32_3 lerpClamped( Float32_3 a, Float32_3 b, Float32 t )
    {
        ret Float32_3.lerp( a, b, Mathf.clamp( t, 0.0f, 1.0f ) )
    }

    # 以 normal 为镜面反射方向
    public static Float32_3 reflect( Float32_3 direction, Float32_3 normal )
    {
        ret direction.reflect( normal.normalize() )
    }

    # v 在 normal 方向上的投影分量
    public static Float32_3 project( Float32_3 v, Float32_3 normal )
    {
        Float32 sq = normal.lengthSquared()
        if sq <= 0.0f
        {
            ret Float32_3.zero()
        }
        Float32 k = v.dot( normal ) / sq
        ret normal.scale( k )
    }

    # v 投影到以 planeNormal 为法线的平面上
    public static Float32_3 projectOnPlane( Float32_3 v, Float32_3 planeNormal )
    {
        ret v._sub_( Vector.project( v, planeNormal ) ) as Float32_3
    }

    # 朝目标移动，单步不超过 maxDelta
    public static Float32_3 moveTowards( Float32_3 current, Float32_3 target, Float32 maxDelta )
    {
        Float32_3 delta = target._sub_( current ) as Float32_3
        Float32 dist = delta.length()
        if dist <= maxDelta || dist == 0.0f
        {
            ret target.clone()
        }
        ret current._add_( delta.scale( maxDelta / dist ) ) as Float32_3
    }

    # 限制向量长度
    public static Float32_3 clampMagnitude( Float32_3 v, Float32 maxLength )
    {
        Float32 sq = v.lengthSquared()
        if sq <= maxLength * maxLength
        {
            ret v.clone()
        }
        ret v.normalize().scale( maxLength )
    }

    # 求与 v 垂直的任一单位向量
    public static Float32_3 orthogonal( Float32_3 v )
    {
        Float32_3 n = v.normalize()
        Float32 ax = Mathf.abs( n.x )
        Float32 ay = Mathf.abs( n.y )
        Float32 az = Mathf.abs( n.z )
        Float32_3 helper = Float32_3.up()
        if ax <= ay && ax <= az
        {
            helper = Float32_3.right()
        }
        elif ay <= az
        {
            helper = Float32_3.forward()
        }
        ret n.cross( helper ).normalize()
    }

    # ── 分量工具 ─────────────────────────────────────────
    # 逐分量取最小 / 最大
    public static Float32_3 min3( Float32_3 a, Float32_3 b )
    {
        ret Float32_3( Mathf.min( a.x, b.x ), Mathf.min( a.y, b.y ), Mathf.min( a.z, b.z ) )
    }

    public static Float32_3 max3( Float32_3 a, Float32_3 b )
    {
        ret Float32_3( Mathf.max( a.x, b.x ), Mathf.max( a.y, b.y ), Mathf.max( a.z, b.z ) )
    }

    override string toString()
    {
        ret "Vector(static utility)"
    }
}
