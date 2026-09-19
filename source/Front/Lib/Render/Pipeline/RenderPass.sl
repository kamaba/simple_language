# RenderPass —— 一次具体的渲染步骤（Unity SRP Pass）
#
# Unity 的 RenderPass 模型：
#   configure()   确定本次要写的 RT / 是否清屏
#   render()      用 ScriptableRenderContext 录制命令并绘制
# 这里把常用 pass 也实现出来：深度预通道、阴影投射、不透明/透明绘制、
# 天空盒、全屏后处理。上层管线（URP）把若干 pass 串起来。

# Pass 执行顺序（值越小越靠前），与 Unity RenderPassEvent 同义
enum ERenderPassEvent
{
    BeforeRendering = 0
    ShadowCasters = 50
    DepthPrepass = 100
    Opaque = 200
    Skybox = 250
    Transparent = 300
    PostProcessing = 800
    AfterRendering = 1000
}

# ── Pass 基类 ─────────────────────────────────────────
public class RenderPass
{
    public string name = "RenderPass"
    public int passEvent = ERenderPassEvent.Opaque
    public string profilerName = "RenderPass"

    # 目标（null 表示写回 backbuffer）
    public RenderTargetIdentifier colorAttachment = null
    public RenderTargetIdentifier depthAttachment = null

    public bool clearColorFlag = false
    public bool clearDepthFlag = false
    public Color clearColor = Color.black()

    # 本 pass 用到的剔除结果（由管线在 setup 阶段填好）
    public CullingResults cullingResults = null
    public Camera camera = null

    public void _init_()
    {
        this.name = "RenderPass"
        this.passEvent = ERenderPassEvent.Opaque
        this.profilerName = "RenderPass"
        this.clearColorFlag = false
        this.clearDepthFlag = false
        this.clearColor = Color.black()
        this.cullingResults = CullingResults()
    }

    public void _init_( string _name )
    {
        this._init_()
        this.name = _name
        this.profilerName = _name
    }

    # 设置写哪个 RT、是否清屏（在 render 之前调用）
    public void configure( RenderTargetIdentifier color, RenderTargetIdentifier depth, bool clearC, bool clearD, Color clearCol )
    {
        this.colorAttachment = color
        this.depthAttachment = depth
        this.clearColorFlag = clearC
        this.clearDepthFlag = clearD
        this.clearColor = clearCol
    }

    # 由管线注入剔除结果
    public void setup( CullingResults cull, Camera cam )
    {
        this.cullingResults = cull
        this.camera = cam
    }

    # ── 生命周期（子类重写 render）─────────────────────────
    public void begin( ScriptableRenderContext ctx )
    {
        if this.profilerName != ""
        {
            ctx.cmd.beginEvent( this.profilerName )
        }
    }

    public virtual void render( ScriptableRenderContext ctx )
    {
        # 基类默认：清一次屏
        if this.clearColorFlag || this.clearDepthFlag
        {
            ctx.cmd.clearRenderTarget( this.clearColorFlag, this.clearDepthFlag, this.clearColor )
        }
    }

    public void end( ScriptableRenderContext ctx )
    {
        if this.profilerName != ""
        {
            ctx.cmd.endEvent()
        }
    }

    override string toString()
    {
        ret "RenderPass(" + this.name + ", event=" + this.passEvent.toString() + ")"
    }
}

# ── 清屏 Pass ─────────────────────────────────────────
public class ClearPass extends RenderPass
{
    public void _init_( string _name, Color clearCol )
    {
        this._init_()
        this.name = _name
        this.profilerName = _name
        this.clearColorFlag = true
        this.clearDepthFlag = true
        this.clearColor = clearCol
    }

    override void render( ScriptableRenderContext ctx )
    {
        ctx.cmd.clearRenderTarget( true, true, this.clearColor )
    }
}

# ── 阴影投射 Pass（把物体深度渲到 shadow map）─────────────
public class ShadowCasterPass extends RenderPass
{
    public GpuTexture shadowMap = null
    public Material depthOnlyMaterial = null
    public Float32_4x4 lightViewProjection = Float32_4x4.identity()

    public void _init_()
    {
        this.name = "ShadowCaster"
        this.profilerName = "RenderPass.ShadowCaster"
        this.passEvent = ERenderPassEvent.ShadowCasters
    }

    public void setLightData( Float32_4x4 lightVP, GpuTexture shadowTex )
    {
        this.lightViewProjection = lightVP
        this.shadowMap = shadowTex
    }

    override void render( ScriptableRenderContext ctx )
    {
        if this.shadowMap == null || this.cullingResults == null
        {
            ret
        }
        ctx.cmd.setRenderTarget( RenderTargetBinding( null, RenderTargetIdentifier.fromTexture( this.shadowMap ) ) )
        ctx.cmd.clearRenderTarget( false, true, Color.black() )

        # 深度只需写深度；这里用 DepthOnly 材质做 override
        if this.depthOnlyMaterial != null
        {
            ctx.drawRenderers( this.cullingResults, 0, 5000, this.depthOnlyMaterial )
        }
        else
        {
            ctx.drawRenderers( this.cullingResults, 0, 5000, null )
        }
    }
}

# ── 深度预通道 ─────────────────────────────────────────
public class DepthPrepass extends RenderPass
{
    public Material depthOnlyMaterial = null

    public void _init_()
    {
        this.name = "DepthPrepass"
        this.profilerName = "RenderPass.DepthPrepass"
        this.passEvent = ERenderPassEvent.DepthPrepass
    }

    override void render( ScriptableRenderContext ctx )
    {
        if this.depthAttachment == null
        {
            ret
        }
        ctx.cmd.setRenderTarget( RenderTargetBinding( null, this.depthAttachment ) )
        if this.cullingResults != null
        {
            ctx.drawRenderers( this.cullingResults, 0, 2500, this.depthOnlyMaterial )
        }
    }
}

# ── 物体绘制 Pass（不透明 / 透明共用）──────────────────────
public class DrawObjectsPass extends RenderPass
{
    public Int32 queueMin = 0
    public Int32 queueMax = 2500
    public Material overrideMaterial = null

    public void _init_( string _name, Int32 minQ, Int32 maxQ )
    {
        this._init_()
        this.name = _name
        this.profilerName = _name
        this.queueMin = minQ
        this.queueMax = maxQ
        if maxQ <= 2500
        {
            this.passEvent = ERenderPassEvent.Opaque
        }
        else
        {
            this.passEvent = ERenderPassEvent.Transparent
        }
    }

    override void render( ScriptableRenderContext ctx )
    {
        if this.colorAttachment == null
        {
            ret
        }
        RenderTargetBinding binding = RenderTargetBinding( this.colorAttachment, this.depthAttachment )
        ctx.cmd.setRenderTarget( binding )

        if this.clearColorFlag
        {
            ctx.cmd.clearRenderTarget( true, false, this.clearColor )
        }

        if this.cullingResults != null
        {
            ctx.drawRenderers( this.cullingResults, this.queueMin, this.queueMax, this.overrideMaterial )
        }
    }
}

# ── 天空盒 Pass ─────────────────────────────────────────
public class SkyboxPass extends RenderPass
{
    public Material skyboxMaterial = null

    public void _init_()
    {
        this.name = "Skybox"
        this.profilerName = "RenderPass.Skybox"
        this.passEvent = ERenderPassEvent.Skybox
    }

    override void render( ScriptableRenderContext ctx )
    {
        if this.colorAttachment == null || this.camera == null
        {
            ret
        }
        RenderTargetBinding binding = RenderTargetBinding( this.colorAttachment, this.depthAttachment )
        ctx.cmd.setRenderTarget( binding )
        ctx.drawSkybox( this.camera )
    }
}

# ── 全屏后处理 Pass ────────────────────────────────────
public class PostProcessPass extends RenderPass
{
    public Material postMaterial = null
    public GpuTexture source = null

    public void _init_( string _name )
    {
        this._init_()
        this.name = _name
        this.profilerName = _name
        this.passEvent = ERenderPassEvent.PostProcessing
        this.clearColorFlag = true
        this.clearDepthFlag = true
    }

    override void render( ScriptableRenderContext ctx )
    {
        # 后处理写回 backbuffer，不需要深度
        ctx.cmd.setRenderTarget( RenderTargetBinding( this.colorAttachment, null ) )
        ctx.cmd.clearRenderTarget( true, true, Color.black() )
        if this.postMaterial != null
        {
            ctx.cmd.setTexture( 0, this.source.srv() )
        }
        ctx.drawFullscreen( this.postMaterial )
    }
}

# ── Pass 列表（管线持有，按 passEvent 排序执行）────────────
public class RenderPassList
{
    Array<RenderPass> _passes = null

    public void _init_()
    {
        this._passes = Array<RenderPass>( 0 )
    }

    public void add( RenderPass pass )
    {
        Array<RenderPass> np = Array<RenderPass>( this._passes.length + 1 )
        int i = 0
        while i < this._passes.length
        {
            np[i] = this._passes[i]
            i++
        }
        np[ this._passes.length ] = pass
        this._passes = np
    }

    public get int count()
    {
        ret this._passes.length
    }

    # 冒泡按 passEvent 升序
    public void sortByEvent()
    {
        int i = 1
        while i < this._passes.length
        {
            RenderPass key = this._passes[i]
            int j = i - 1
            while j >= 0
            {
                if this._passes[j].passEvent > key.passEvent
                {
                    this._passes[ j + 1 ] = this._passes[j]
                    j--
                }
                else
                {
                    break
                }
            }
            this._passes[ j + 1 ] = key
            i++
        }
    }

    public RenderPass at( Int32 index )
    {
        ret this._passes[ index ]
    }

    public void clear()
    {
        this._passes = Array<RenderPass>( 0 )
    }
}
