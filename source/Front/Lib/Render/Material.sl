# 材质：着色器 + 基础颜色 + 主纹理 + 常用 PBR 参数。
public class Material
{
    public string name = ""
    public Color color = Color.white()
    public Color emission = Color.black()
    public Texture mainTexture = null
    public Shader shader = null

    public Float32 metallic = 0.0f
    public Float32 smoothness = 0.5f

    # 渲染状态
    public bool enableDepthTest = true
    public bool enableDepthWrite = true
    public bool isTransparent = false

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.name = "Material"
        this.color = Color.white()
        this.emission = Color.black()
        this.mainTexture = null
        this.shader = null
        this.metallic = 0.0f
        this.smoothness = 0.5f
        this.enableDepthTest = true
        this.enableDepthWrite = true
        this.isTransparent = false
    }

    public void _init_( Shader _shader )
    {
        this._init_()
        this.shader = _shader
    }

    public void _init_( Shader _shader, Color _color )
    {
        this._init_()
        this.shader = _shader
        this.color = _color.clone()
    }

    # ── 设置 ─────────────────────────────────────────────
    public void setColor( Color c )
    {
        this.color = c.clone()
    }

    public Color getColor()
    {
        ret this.color.clone()
    }

    public void setTexture( Texture tex )
    {
        this.mainTexture = tex
    }

    public Texture getTexture()
    {
        ret this.mainTexture
    }

    public void setShader( Shader s )
    {
        this.shader = s
    }

    public Shader getShader()
    {
        ret this.shader
    }

    # 各参数统一限制到 [0,1]
    public void clampParameters()
    {
        this.metallic = Mathf.clamp( this.metallic, 0.0f, 1.0f )
        this.smoothness = Mathf.clamp( this.smoothness, 0.0f, 1.0f )
        this.color = this.color.clamp01()
        this.emission = this.emission.clamp01()
    }

    # 透明材质默认关闭深度写入
    public void setTransparent( bool transparent )
    {
        this.isTransparent = transparent
        if transparent
        {
            this.enableDepthWrite = false
        }
        else
        {
            this.enableDepthWrite = true
        }
    }

    public Material clone()
    {
        Material m = Material()
        m.name = this.name
        m.color = this.color.clone()
        m.emission = this.emission.clone()
        m.mainTexture = this.mainTexture
        m.shader = this.shader
        m.metallic = this.metallic
        m.smoothness = this.smoothness
        m.enableDepthTest = this.enableDepthTest
        m.enableDepthWrite = this.enableDepthWrite
        m.isTransparent = this.isTransparent
        ret m
    }

    override string toString()
    {
        ret String.toFormat( "Material(name={0}, color={1})", this.name, this.color.toString() )
    }
}
