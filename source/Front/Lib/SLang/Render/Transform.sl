# 变换组件：位置 / 旋转 / 缩放 + 父子层级 + 空间变换。
#
# 数学类型使用 Math 库的 Float32 系列：
#   位置 / 缩放 -> Float32_3，旋转 -> Quaternion，矩阵 -> Float32_4x4
public class Transform extends Component
{
    public Float32_3 position = Float32_3.zero()
    public Float32_3 localScale = Float32_3.one()
    public Quaternion rotation = Quaternion.identity()

    public Transform parent = null

    # ── 方向（局部前向经旋转到世界空间）────────────────────
    # 说明：Quaternion 的运算符重载返回 Object，此处用带静态类型的 rotate() 避免转型
    public get Float32_3 forward()
    {
        ret this.rotation.rotate( Float32_3.forward() )
    }

    public get Float32_3 back()
    {
        ret this.rotation.rotate( Float32_3.back() )
    }

    public get Float32_3 right()
    {
        ret this.rotation.rotate( Float32_3.right() )
    }

    public get Float32_3 left()
    {
        ret this.rotation.rotate( Float32_3.left() )
    }

    public get Float32_3 up()
    {
        ret this.rotation.rotate( Float32_3.up() )
    }

    public get Float32_3 down()
    {
        ret this.rotation.rotate( Float32_3.down() )
    }

    # ── 矩阵 ─────────────────────────────────────────────
    # 本地 -> 世界（含父级链）
    public get Float32_4x4 localToWorldMatrix()
    {
        Float32_4x4 local = MatrixUtil.trs( this.position, this.rotation, this.localScale )
        if this.parent != null
        {
            ret this.parent.localToWorldMatrix().multiply( local )
        }
        ret local
    }

    # 世界 -> 本地
    public get Float32_4x4 worldToLocalMatrix()
    {
        ret MatrixUtil.inverseRigid( this.localToWorldMatrix() )
    }

    # ── 世界坐标 ─────────────────────────────────────────
    public get Float32_3 worldPosition()
    {
        if this.parent == null
        {
            ret this.position.clone()
        }
        ret this.parent.transformPoint( this.position )
    }

    public void setWorldPosition( Float32_3 p )
    {
        if this.parent == null
        {
            this.position = p.clone()
            ret
        }
        this.position = this.parent.inverseTransformPoint( p )
    }

    public get Quaternion worldRotation()
    {
        if this.parent == null
        {
            ret this.rotation.clone()
        }
        ret this.parent.worldRotation.multiply( this.rotation ).normalize()
    }

    # ── 移动 / 旋转 ──────────────────────────────────────
    # 按世界空间增量平移
    public void translate( Float32_3 delta )
    {
        this.position = this.position._add_( delta ) as Float32_3
    }

    # 按自身局部方向平移（localDirection 为局部向量）
    public void translateLocal( Float32_3 localDirection )
    {
        Float32_3 worldDelta = this.rotation.rotate( localDirection )
        this.translate( worldDelta )
    }

    # 叠加欧拉角旋转（弧度）
    public void rotate( Float32_3 eulerRadians )
    {
        Quaternion delta = Quaternion.euler( eulerRadians )
        this.rotation = delta.multiply( this.rotation ).normalize()
    }

    # 绕世界轴旋转
    public void rotateAxis( Float32_3 axis, Float32 radians )
    {
        Quaternion delta = Quaternion.axisAngle( axis, radians )
        this.rotation = delta.multiply( this.rotation ).normalize()
    }

    # 绕某个世界点 + 轴旋转
    public void rotateAround( Float32_3 point, Float32_3 axis, Float32 radians )
    {
        Quaternion q = Quaternion.axisAngle( axis, radians )
        Float32_3 offset = this.position._sub_( point ) as Float32_3
        this.position = point._add_( q.rotate( offset ) ) as Float32_3
        this.rotation = q.multiply( this.rotation ).normalize()
    }

    # 朝向目标（+Z 对准）
    public void lookAt( Float32_3 target )
    {
        this.lookAt( target, Float32_3.up() )
    }

    public void lookAt( Float32_3 target, Float32_3 upHint )
    {
        Float32_3 dir = target._sub_( this.worldPosition() ) as Float32_3
        if dir.lengthSquared() <= 0.0f
        {
            ret
        }
        this.rotation = Quaternion.lookRotation( dir, upHint )
    }

    # ── 空间变换 ─────────────────────────────────────────
    public Float32_3 transformPoint( Float32_3 p )
    {
        ret this.localToWorldMatrix().transformPoint( p )
    }

    public Float32_3 transformDirection( Float32_3 v )
    {
        ret this.localToWorldMatrix().transformDirection( v )
    }

    public Float32_3 inverseTransformPoint( Float32_3 p )
    {
        ret this.worldToLocalMatrix().transformPoint( p )
    }

    public Float32_3 inverseTransformDirection( Float32_3 v )
    {
        ret this.worldToLocalMatrix().transformDirection( v )
    }

    # ── 其它 ─────────────────────────────────────────────
    public void setPositionAndRotation( Float32_3 p, Quaternion q )
    {
        this.position = p.clone()
        this.rotation = q.normalize()
    }

    public void reset()
    {
        this.position = Float32_3.zero()
        this.localScale = Float32_3.one()
        this.rotation = Quaternion.identity()
    }

    override string toString()
    {
        ret String.toFormat( "Transform(pos={0}, scale={1}, rot={2})",
            this.position.toString(), this.localScale.toString(), this.rotation.toString() )
    }
}
