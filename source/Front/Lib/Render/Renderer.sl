# 渲染器组件：持有网格与材质，负责把自身提交给渲染管线。
public class Renderer extends Component
{
    public Mesh mesh = null
    public Material material = null

    public bool isVisible = true
    public Int32 sortingOrder = 0
    public Int32 layer = 0

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.mesh = null
        this.material = null
        this.isVisible = true
        this.sortingOrder = 0
        this.layer = 0
    }

    # ── 包围盒 ───────────────────────────────────────────
    # 网格本地包围盒（无网格时返回空盒）
    public get Bounds localBounds()
    {
        if this.mesh == null
        {
            ret Bounds()
        }
        ret this.mesh.bounds()
    }

    # ── 设置 ─────────────────────────────────────────────
    public void setMesh( Mesh m )
    {
        this.mesh = m
    }

    public Mesh getMesh()
    {
        ret this.mesh
    }

    public void setMaterial( Material m )
    {
        this.material = m
    }

    public Material getMaterial()
    {
        ret this.material
    }

    public void setVisible( bool visible )
    {
        this.isVisible = visible
    }

    # ── 提交绘制 ─────────────────────────────────────────
    # model 为本地 -> 世界矩阵，viewProjection 由相机提供
    public bool render( Float32_4x4 model, Float32_4x4 viewProjection )
    {
        if !this.isVisible || this.mesh == null || this.material == null
        {
            ret false
        }
        ret Render.drawMesh( this.mesh, this.material, model, viewProjection )
    }

    # 便捷重载：直接传入 Transform
    public bool render( Transform t, Float32_4x4 viewProjection )
    {
        if t == null
        {
            ret false
        }
        ret this.render( t.localToWorldMatrix(), viewProjection )
    }

    override string toString()
    {
        ret String.toFormat( "Renderer(visible={0}, order={1})",
            this.isVisible.toString(), this.sortingOrder.toString() )
    }
}
