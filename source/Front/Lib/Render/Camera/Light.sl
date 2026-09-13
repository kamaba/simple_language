# 光源组件：方向光 / 点光 / 聚光 + 颜色 / 强度 / 范围。
# 数学类型使用 Math 库的 Float32 系列。
public class Light extends Component
{
    # 光照类型
    public Int32 type = 0   # 0 = Directional, 1 = Point, 2 = Spot

    public Color color = Color.white()

    # 强度（通常 [0, 8+]）
    public Float32 intensity = 1.0f

    # 点光 / 聚光有效：0 表示无限远
    public Float32 range = 0.0f

    # 聚光有效：圆锥半角（角度制，光轴两侧）
    public Float32 spotAngle = 30.0f

    # 阴影
    public bool castShadow = false
    public Float32 shadowStrength = 1.0f

    # ── 构造 / 预设 ──────────────────────────────────────
    public void _init_()
    {
        this.type = 0
        this.color = Color.white()
        this.intensity = 1.0f
        this.range = 0.0f
        this.spotAngle = 30.0f
        this.castShadow = false
        this.shadowStrength = 1.0f
    }

    # ── 类型判定 ─────────────────────────────────────────
    public get bool isDirectional()
    {
        ret this.type == 0
    }

    public get bool isPoint()
    {
        ret this.type == 1
    }

    public get bool isSpot()
    {
        ret this.type == 2
    }

    public void setType( Int32 lightType )
    {
        this.type = lightType
    }

    # ── 方向 ─────────────────────────────────────────────
    # 方向光：从 Transform 旋转推出光照方向（默认沿 +Z 照射）
    public get Float32_3 direction()
    {
        Component c = this.GetComponent<Transform>()
        if c is Transform t
        {
            ret t.forward()
        }
        ret Float32_3.forward()
    }

    public get Float32_3 worldPosition()
    {
        Component c = this.GetComponent<Transform>()
        if c is Transform t
        {
            ret t.worldPosition()
        }
        ret Float32_3.zero()
    }

    # ── 着色参数 ─────────────────────────────────────────
    # 返回用于着色器的颜色 * 强度
    public Color effectiveColor()
    {
        ret this.color.clone()._mul_( this.intensity ) as Color
    }

    # 点光 / 聚光衰减（距离平方反比，带范围截断）
    public Float32 attenuation( Float32 distance )
    {
        if this.type == 0
        {
            ret 1.0f
        }
        if this.range > 0.0f && distance >= this.range
        {
            ret 0.0f
        }
        Float32 d = Mathf.max( distance, 0.0001f )
        Float32 atten = 1.0f / ( 1.0f + d * d )
        if this.range > 0.0f
        {
            Float32 k = 1.0f - Mathf.clamp( distance / this.range, 0.0f, 1.0f )
            atten = atten * k * k
        }
        ret atten
    }

    # 聚光锥角衰减（worldPoint 为被照点）
    public Float32 spotAttenuation( Float32_3 worldPoint )
    {
        if this.type != 2
        {
            ret 1.0f
        }
        Float32_3 toPoint = worldPoint._sub_( this.worldPosition() ) as Float32_3
        Float32 dist = toPoint.length()
        if dist <= 0.0001f
        {
            ret 1.0f
        }
        toPoint = toPoint.normalize()
        Float32 cosAngle = toPoint.dot( this.direction() )
        Float32 cosEdge = Mathf.cos( Mathf.radians( this.spotAngle ) )
        if cosAngle <= cosEdge
        {
            ret 0.0f
        }
        Float32 t = ( cosAngle - cosEdge ) / ( 1.0f - cosEdge )
        ret t * t
    }

    # ── 预设工厂 ─────────────────────────────────────────
    public static Light directional( Color c, Float32 intensity )
    {
        Light l = Light()
        l.type = 0
        l.color = c.clone()
        l.intensity = intensity
        ret l
    }

    public static Light point( Color c, Float32 intensity, Float32 range )
    {
        Light l = Light()
        l.type = 1
        l.color = c.clone()
        l.intensity = intensity
        l.range = range
        ret l
    }

    public static Light spot( Color c, Float32 intensity, Float32 range, Float32 spotAngleDegrees )
    {
        Light l = Light()
        l.type = 2
        l.color = c.clone()
        l.intensity = intensity
        l.range = range
        l.spotAngle = spotAngleDegrees
        ret l
    }

    override string toString()
    {
        ret String.toFormat( "Light(type={0}, color={1}, intensity={2})",
            this.type.toString(), this.color.toString(), this.intensity.toString() )
    }
}
