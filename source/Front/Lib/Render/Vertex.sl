# 顶点：位置 / 法线 / UV / 颜色 / 切线。
# 使用 Math 库的 Float32_3 / Float32_2 与 Render 的 Color。
public class Vertex
{
    public Float32_3 position = Float32_3.zero()
    public Float32_3 normal = Float32_3.up()
    public Float32_2 uv = Float32_2.zero()
    public Color color = Color.white()
    public Float32_3 tangent = Float32_3.right()

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.position = Float32_3.zero()
        this.normal = Float32_3.up()
        this.uv = Float32_2.zero()
        this.color = Color.white()
        this.tangent = Float32_3.right()
    }

    public void _init_( Float32_3 _position )
    {
        this._init_()
        this.position = _position.clone()
    }

    public void _init_( Float32_3 _position, Float32_3 _normal, Float32_2 _uv )
    {
        this._init_()
        this.position = _position.clone()
        this.normal = _normal.normalize()
        this.uv = _uv.clone()
    }

    public void _init_( Float32_3 _position, Float32_3 _normal, Float32_2 _uv, Color _color )
    {
        this._init_()
        this.position = _position.clone()
        this.normal = _normal.normalize()
        this.uv = _uv.clone()
        this.color = _color.clone()
    }

    # ── 变换 ─────────────────────────────────────────────
    # 位置做完整变换，法线 / 切线只做方向变换并归一化
    public Vertex transformed( Float32_4x4 m )
    {
        Vertex v = Vertex()
        v.position = m.transformPoint( this.position )
        v.normal = m.transformDirection( this.normal ).normalize()
        v.tangent = m.transformDirection( this.tangent ).normalize()
        v.uv = this.uv.clone()
        v.color = this.color.clone()
        ret v
    }

    public void transform( Float32_4x4 m )
    {
        this.position = m.transformPoint( this.position )
        this.normal = m.transformDirection( this.normal ).normalize()
        this.tangent = m.transformDirection( this.tangent ).normalize()
    }

    public void flipNormal()
    {
        this.normal = this.normal.negate()
    }

    public Vertex clone()
    {
        Vertex v = Vertex()
        v.position = this.position.clone()
        v.normal = this.normal.clone()
        v.uv = this.uv.clone()
        v.color = this.color.clone()
        v.tangent = this.tangent.clone()
        ret v
    }

    # ── 索引访问（分量级）─────────────────────────────────
    Float32 _getItem_( int index )
    {
        if index == 0
        {
            ret this.position.x
        }
        if index == 1
        {
            ret this.position.y
        }
        ret this.position.z
    }

    # ── 静态工具 ─────────────────────────────────────────
    # 三角形面积（由三个顶点位置计算）
    public static Float32 triangleArea( Vertex a, Vertex b, Vertex c )
    {
        Float32_3 ab = b.position._sub_( a.position ) as Float32_3
        Float32_3 ac = c.position._sub_( a.position ) as Float32_3
        ret ab.cross( ac ).length() * 0.5f
    }

    # 由三个顶点计算面法线（未归一化时为面积 * 2）
    public static Float32_3 triangleNormal( Vertex a, Vertex b, Vertex c )
    {
        Float32_3 ab = b.position._sub_( a.position ) as Float32_3
        Float32_3 ac = c.position._sub_( a.position ) as Float32_3
        Float32_3 n = ab.cross( ac )
        Float32 len = n.length()
        if len <= 0.0f
        {
            ret Float32_3.up()
        }
        ret n.normalize()
    }

    override string toString()
    {
        ret String.toFormat( "Vertex(pos={0}, uv={1})", this.position.toString(), this.uv.toString() )
    }
}
