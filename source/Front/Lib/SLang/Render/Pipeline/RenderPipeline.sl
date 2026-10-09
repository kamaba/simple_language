# RenderPipeline —— 渲染管线基类 / 资产 / 管理器
#
# 对齐 Unity：
#   RenderPipeline            抽象基类，render(ctx, camera) 由子类实现
#   RenderPipelineAsset       配置 + 工厂（创建具体管线实例）
#   RenderPipelineManager     框架入口：持有 device + 当前管线，驱动每帧 begin/渲染/present

public class RenderPipeline
{
    public string name = "RenderPipeline"
    public Config config = null
    public GfxDevice device = null
    public ScriptableRenderContext context = null
    public bool isInitialized = false

    public void _init_()
    {
        this.name = "RenderPipeline"
        this.config = null
        this.device = null
        this.context = ScriptableRenderContext()
        this.isInitialized = false
    }

    # ── 生命周期 ─────────────────────────────────────────
    public virtual bool initialize( GfxDevice dev, Config cfg )
    {
        if dev == null || cfg == null
        {
            ret false
        }
        this.device = dev
        this.config = cfg
        this.context = ScriptableRenderContext( dev )
        this.isInitialized = true
        ret true
    }

    public virtual void dispose()
    {
        this.isInitialized = false
    }

    # ── 单相机完整渲染（子类实现）──────────────────────────
    public virtual void render( ScriptableRenderContext ctx, Camera camera )
    {
        # 基类留空：具体管线（URP）实现 pass 编排
    }

    # 标准辅助：剔除 + 执行一个 pass 列表（已按 event 排序）
    void renderSingleCamera( ScriptableRenderContext ctx, Camera camera, RenderPassList passes )
    {
        ctx.beginCommandBuffer( "Camera." + camera.name )
        ctx.setupCameraProperties( camera )

        int i = 0
        while i < passes.count
        {
            RenderPass pass = passes.at( i )
            pass.begin( ctx )
            pass.render( ctx )
            pass.end( ctx )
            i++
        }

        ctx.submit()
    }

    override string toString()
    {
        ret "RenderPipeline(" + this.name + ", init=" + this.isInitialized.toString() + ")"
    }
}

# ── 管线资产（配置 + 工厂）──────────────────────────────
public class RenderPipelineAsset
{
    public string name = "RenderPipelineAsset"

    public Int32 msaaSamples = 1
    public Int32 shadowMapSize = 2048
    public Float32 renderScale = 1.0f
    public bool hdr = true
    public bool softShadows = true
    public Int32 maxVisibleLights = 4
    public Int32 pixelLights = 1

    public void _init_()
    {
        this.name = "RenderPipelineAsset"
        this.msaaSamples = 1
        this.shadowMapSize = 2048
        this.renderScale = 1.0f
        this.hdr = true
        this.softShadows = true
        this.maxVisibleLights = 4
        this.pixelLights = 1
    }

    # 由子类（URP asset）创建具体管线
    public virtual RenderPipeline createPipeline()
    {
        ret RenderPipeline()
    }

    # 包装：实例化 + 初始化（在 GfxDevice 建好后调用）
    public RenderPipeline instantiate( GfxDevice device )
    {
        RenderPipeline pipeline = this.createPipeline()
        Config cfg = Config()
        cfg.msaaSamples = this.msaaSamples
        cfg.renderScale = this.renderScale
        cfg.hdr = this.hdr
        pipeline.initialize( device, cfg )
        ret pipeline
    }

    override string toString()
    {
        ret "RenderPipelineAsset(" + this.name + ", msaa=" + this.msaaSamples.toString() + ")"
    }
}

# ── 管理器（框架驱动入口）────────────────────────────────
public class RenderPipelineManager
{
    public static RenderPipelineManager _instance = null

    public RenderPipeline activePipeline = null
    public GfxDevice device = null
    public ScriptableRenderContext context = null
    public RenderPipelineAsset asset = null
    public bool isValid = false

    # 单例（SL 支持静态字段；不支持则每次 new 一个）
    public static RenderPipelineManager instance()
    {
        if _instance == null
        {
            _instance = RenderPipelineManager()
        }
        ret _instance
    }

    public bool initialize( RenderPipelineAsset a )
    {
        if a == null
        {
            ret false
        }
        this.asset = a
        this.activePipeline = a.instantiate( this.device )
        this.context = this.activePipeline.context
        this.isValid = this.activePipeline.isInitialized
        ret this.isValid
    }

    public void setDevice( GfxDevice dev )
    {
        this.device = dev
    }

    public void beginFrame()
    {
        if this.device == null
        {
            ret
        }
        this.device.beginFrame()
        this.device.waitForNextFrame()
    }

    # 渲染一个相机（内部取命令缓冲、跑管线、提交）
    public void renderCamera( Camera cam )
    {
        if this.activePipeline == null || this.context == null
        {
            ret
        }
        this.context.camera = cam
        this.activePipeline.render( this.context, cam )
    }

    public void endFrame()
    {
        if this.device == null
        {
            ret
        }
        this.device.endFrame()
    }

    public void present()
    {
        if this.device == null
        {
            ret
        }
        this.device.present()
    }

    public void shutdown()
    {
        if this.activePipeline != null
        {
            this.activePipeline.dispose()
        }
        if this.device != null
        {
            this.device.destroy()
        }
        this.isValid = false
    }
}
