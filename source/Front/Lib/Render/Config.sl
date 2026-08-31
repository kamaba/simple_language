# 渲染配置：分辨率 / 同步 / 抗锯齿 / 后端 / 裁剪面等。
public class Config
{
    public Int32 width = 1280
    public Int32 height = 720
    public bool fullscreen = false
    public bool vsync = true

    # 多重采样数量：0 / 2 / 4 / 8
    public Int32 msaaSamples = 0

    # 渲染后端标识：opengl / vulkan / d3d11 / metal / software
    public string backend = "opengl"

    # 清屏颜色
    public Color clearColor = Color.black()

    # 相机默认参数（视场角为角度制）
    public Float32 fieldOfView = 60.0f
    public Float32 nearClip = 0.1f
    public Float32 farClip = 1000.0f

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.width = 1280
        this.height = 720
        this.fullscreen = false
        this.vsync = true
        this.msaaSamples = 0
        this.backend = "opengl"
        this.clearColor = Color.black()
        this.fieldOfView = 60.0f
        this.nearClip = 0.1f
        this.farClip = 1000.0f
    }

    public void _init_( Int32 _width, Int32 _height )
    {
        this._init_()
        this.width = _width
        this.height = _height
    }

    # ── 属性 ─────────────────────────────────────────────
    public get Float32 aspect()
    {
        if this.height <= 0
        {
            ret 1.0f
        }
        ret this.width / this.height
    }

    # ── 设置 ─────────────────────────────────────────────
    public void setResolution( Int32 w, Int32 h )
    {
        this.width = w
        this.height = h
    }

    public void setClearColor( Color c )
    {
        this.clearColor = c.clone()
    }

    # 裁剪面合法性修正：near 必须 > 0 且 < far
    public void clampClipping()
    {
        if this.nearClip <= 0.0f
        {
            this.nearClip = 0.01f
        }
        if this.farClip <= this.nearClip
        {
            this.farClip = this.nearClip + 1.0f
        }
    }

    # MSAA 只能取 0/2/4/8
    public void clampMSAA()
    {
        if this.msaaSamples >= 8
        {
            this.msaaSamples = 8
        }
        elif this.msaaSamples >= 4
        {
            this.msaaSamples = 4
        }
        elif this.msaaSamples >= 2
        {
            this.msaaSamples = 2
        }
        else
        {
            this.msaaSamples = 0
        }
    }

    # ── 静态预设 ─────────────────────────────────────────
    public static Config defaultConfig()
    {
        ret Config()
    }

    # 1920x1080 + 4x MSAA + 垂直同步
    public static Config highQuality()
    {
        Config c = Config( 1920, 1080 )
        c.msaaSamples = 4
        c.vsync = true
        ret c
    }

    # 低配：720p、关闭 MSAA 与垂直同步
    public static Config performance()
    {
        Config c = Config( 1280, 720 )
        c.msaaSamples = 0
        c.vsync = false
        c.farClip = 500.0f
        ret c
    }

    override string toString()
    {
        ret String.toFormat( "Config({0}x{1}, backend={2}, msaa={3}, fov={4})",
            this.width.toString(), this.height.toString(), this.backend,
            this.msaaSamples.toString(), this.fieldOfView.toString() )
    }
}
