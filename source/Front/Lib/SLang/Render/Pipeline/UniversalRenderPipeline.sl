# UniversalRenderPipeline —— Unity URP 风格的可编程管线
#
# 单次相机渲染流程（与 URP 对齐）：
#   1. 剔除可见物体
#   2. 阴影 Pass（主方向光深度 -> shadow map）
#   3. 深度预通道（Z-Prepass，可选）
#   4. 不透明物体（写入 HDR 颜色缓冲 + 深度）
#   5. 天空盒
#   6. 透明物体（关深度写入）
#   7. 后处理（Tonemap + 暗角）合成到 backbuffer
#
# 所有 Shader 用 HLSL 编写，经 HlslShader 编译成 PSO（DX12/Vulkan 风格）。

enum EShadowQuality
{
    Off = 0
    Hard
    Soft
}

enum EShadowCascades
{
    Single = 0
    Two = 1
    Four = 2
}

# ── 资产 ───────────────────────────────────────────────
public class UniversalRenderPipelineAsset extends RenderPipelineAsset
{
    public EShadowQuality shadowQuality = EShadowQuality.Soft
    public EShadowCascades shadowCascades = EShadowCascades.Single
    public bool useDepthPrepass = true
    public bool usePostProcessing = true
    public string pipelineName = "UniversalRenderPipeline"

    public void _init_()
    {
        this.name = "UniversalRenderPipelineAsset"
        this.shadowQuality = EShadowQuality.Soft
        this.shadowCascades = EShadowCascades.Single
        this.useDepthPrepass = true
        this.usePostProcessing = true
        this.pipelineName = "UniversalRenderPipeline"
        this.shadowMapSize = 2048
        this.msaaSamples = 1
        this.hdr = true
    }

    override RenderPipeline createPipeline()
    {
        ret UniversalRenderPipeline()
    }

    override string toString()
    {
        ret "UniversalRenderPipelineAsset(shadow=" + this.shadowQuality.toString() +
            ", cascades=" + this.shadowCascades.toString() + ")"
    }
}

# ── 管线实现 ───────────────────────────────────────────
public class UniversalRenderPipeline extends RenderPipeline
{
    # ── Shader / PSO / Material ─────────────────────────
    HlslShader _litShader = null
    HlslShader _unlitShader = null
    HlslShader _shadowShader = null
    HlslShader _skyboxShader = null
    HlslShader _postShader = null

    GraphicsPipelineState _litPso = null
    GraphicsPipelineState _unlitPso = null
    GraphicsPipelineState _shadowPso = null
    GraphicsPipelineState _skyboxPso = null
    GraphicsPipelineState _postPso = null

    Material _litMaterial = null
    Material _shadowMaterial = null
    Material _skyboxMaterial = null
    Material _postMaterial = null

    # ── 渲染目标 ─────────────────────────────────────────
    RenderTargetPool _rtPool = null
    GpuTexture _colorTarget = null
    GpuTexture _depthTarget = null
    GpuTexture _shadowMap = null
    RenderTargetIdentifier _colorId = null
    RenderTargetIdentifier _depthId = null
    RenderTargetIdentifier _shadowId = null
    Sampler _shadowSampler = null

    # ── Pass ─────────────────────────────────────────────
    ShadowCasterPass _shadowPass = null
    DepthPrepass _depthPrepass = null
    DrawObjectsPass _opaquePass = null
    SkyboxPass _skyboxPass = null
    DrawObjectsPass _transparentPass = null
    PostProcessPass _postPass = null
    RenderPassList _passes = null

    # ── 场景（引擎侧注入的待渲染物体）──────────────────────
    Array<VisibleRenderer> sceneRenderers = null

    # ── 配置 ─────────────────────────────────────────────
    EShadowQuality _shadowQuality = EShadowQuality.Soft
    bool _useDepthPrepass = true
    bool _usePostProcessing = true
    Color _clearColor = Color.black()

    public void _init_()
    {
        this.name = "UniversalRenderPipeline"
        this._rtPool = RenderTargetPool()
        this._passes = RenderPassList()
        this.sceneRenderers = Array<VisibleRenderer>( 0 )
        this._shadowQuality = EShadowQuality.Soft
        this._useDepthPrepass = true
        this._usePostProcessing = true
    }

    override bool initialize( GfxDevice dev, Config cfg )
    {
        if !super.initialize( dev, cfg )
        {
            ret false
        }
        if cfg != null
        {
            this._shadowQuality = EShadowQuality.Soft
            this._useDepthPrepass = cfg.useDepthPrepass
            this._usePostProcessing = cfg.usePostProcessing
            this._clearColor = cfg.clearColor.clone()
        }
        this.buildShaders()
        this.createTargets()
        this.buildPasses()
        ret true
    }

    # ── 编译 Shader + 创建 PSO + 材质 ─────────────────────
    void buildShaders()
    {
        # Lit
        this._litShader = HlslLibrary.litShader()
        this._litShader.compile()
        this._litPso = GraphicsPipelineState( "Hidden/SL/Lit", this._litShader.vertexModule(), this._litShader.pixelModule() )
        this._litPso.setRenderTargetFormats( UniversalRenderPipeline.hdrFormatList() )
        this._litPso.create()
        this._litMaterial = Material( Shader( "Hidden/SL/Lit" ) )

        # Unlit（备用）
        this._unlitShader = HlslLibrary.unlitShader()
        this._unlitShader.compile()
        this._unlitPso = GraphicsPipelineState( "Hidden/SL/Unlit", this._unlitShader.vertexModule(), this._unlitShader.pixelModule() )
        this._unlitPso.create()

        # 深度 / 阴影投射
        this._shadowShader = HlslLibrary.depthOnlyShader()
        this._shadowShader.compile()
        this._shadowPso = GraphicsPipelineState( "Hidden/SL/DepthOnly", this._shadowShader.vertexModule(), this._shadowShader.pixelModule() )
        this._shadowPso.create()
        this._shadowMaterial = Material( Shader( "Hidden/SL/DepthOnly" ) )

        # 天空盒
        this._skyboxShader = HlslLibrary.skyboxShader()
        this._skyboxShader.compile()
        this._skyboxPso = GraphicsPipelineState( "Hidden/SL/Skybox", this._skyboxShader.vertexModule(), this._skyboxShader.pixelModule() )
        this._skyboxPso.create()
        this._skyboxMaterial = Material( Shader( "Hidden/SL/Skybox" ) )

        # 后处理
        this._postShader = HlslLibrary.postProcessShader()
        this._postShader.compile()
        this._postPso = GraphicsPipelineState( "Hidden/SL/PostProcess", this._postShader.vertexModule(), this._postShader.pixelModule() )
        this._postPso.create()
        this._postMaterial = Material( Shader( "Hidden/SL/PostProcess" ) )

        # 阴影采样器（比较采样）
        this._shadowSampler = Sampler( SamplerDesc.shadowCompare() )
        this._shadowSampler.create()
    }

    static Array<Int32> hdrFormatList()
    {
        Array<Int32> f = Array<Int32>( 1 )
        f[0] = ETextureFormat.RGBA16F
        ret f
    }

    # ── 创建 RT / 阴影贴图 ───────────────────────────────
    void createTargets()
    {
        Int32 w = 1280
        Int32 h = 720
        if this.device != null && this.device.window != null
        {
            w = this.device.window.width
            h = this.device.window.height
        }

        RenderTextureDescriptor colorDesc = RenderTextureDescriptor.hdr( w, h )
        this._colorId = this._rtPool.get( colorDesc )
        this._colorTarget = this._colorId.texture

        RenderTextureDescriptor depthDesc = RenderTextureDescriptor.screen( w, h )
        depthDesc.colorFormat = ETextureFormat.Unknown
        depthDesc.depthFormat = ETextureFormat.Depth24Stencil8
        this._depthId = this._rtPool.get( depthDesc )
        this._depthTarget = this._depthId.texture

        # 阴影贴图（深度格式，无颜色）
        GpuTexture shadow = GpuTexture( "MainShadow", this.shadowMapSize(), this.shadowMapSize(),
            ETextureFormat.Depth32F, ETextureUsage.DepthStencil )
        shadow.create()
        this._shadowMap = shadow
        this._shadowId = RenderTargetIdentifier.fromTexture( shadow )
    }

    Int32 shadowMapSize()
    {
        if this.config != null
        {
            ret this.config.shadowMapSize
        }
        ret 2048
    }

    # ── 组装 Pass ────────────────────────────────────────
    void buildPasses()
    {
        this._shadowPass = ShadowCasterPass()
        this._shadowPass.setLightData( Float32_4x4.identity(), this._shadowMap )
        this._shadowPass.depthOnlyMaterial = this._shadowMaterial

        this._depthPrepass = DepthPrepass()
        this._depthPrepass.depthAttachment = this._depthId

        this._opaquePass = DrawObjectsPass( "Opaque", 0, 2500 )
        this._opaquePass.configure( this._colorId, this._depthId, true, false, Color.black() )

        this._skyboxPass = SkyboxPass()
        this._skyboxPass.colorAttachment = this._colorId
        this._skyboxPass.depthAttachment = this._depthId
        this._skyboxPass.skyboxMaterial = this._skyboxMaterial

        this._transparentPass = DrawObjectsPass( "Transparent", 2500, 5000 )
        this._transparentPass.configure( this._colorId, this._depthId, false, false, Color.black() )

        this._postPass = PostProcessPass( "PostProcess" )
        this._postPass.colorAttachment = RenderTargetIdentifier.backBuffer()
        this._postPass.postMaterial = this._postMaterial
        this._postPass.source = this._colorTarget

        # 压入列表（render 里按 event 排序后顺序执行）
        this._passes.add( this._shadowPass )
        this._passes.add( this._depthPrepass )
        this._passes.add( this._opaquePass )
        this._passes.add( this._skyboxPass )
        this._passes.add( this._transparentPass )
        this._passes.add( this._postPass )
        this._passes.sortByEvent()
    }

    # ── 每帧渲染 ─────────────────────────────────────────
    override void render( ScriptableRenderContext ctx, Camera camera )
    {
        if camera == null || this.device == null
        {
            ret
        }

        ctx.beginCommandBuffer( "URP." + camera.name )
        ctx.setupCameraProperties( camera )

        # 1) 剔除
        CullingParameters cullParams = CullingParameters.fromCamera( camera )
        ctx.cullingResults = ctx.cull( cullParams, this.sceneRenderers )

        # 2) 帧级常量（VP / 相机位置 / 时间）写入 lit shader
        this.updateFrameConstants( ctx, camera )

        # 3) 注入剔除结果到各 pass
        int i = 0
        while i < this._passes.count
        {
            this._passes.at( i ).setup( ctx.cullingResults, camera )
            i++
        }

        # 4) 阴影
        if this._shadowQuality != EShadowQuality.Off
        {
            this.renderShadows( ctx, camera )
        }

        # 5) 场景颜色（深度预通道 + 不透明 + 天空盒 + 透明）
        this.renderColor( ctx, camera )

        # 6) 后处理到 backbuffer
        if this._usePostProcessing
        {
            this.renderPost( ctx, camera )
        }

        ctx.submit()
    }

    void updateFrameConstants( ScriptableRenderContext ctx, Camera camera )
    {
        if this._litShader != null
        {
            this._litShader.setMatrix( "unity_MatrixVP", camera.viewProjectionMatrix() )
            this._litShader.setVector3( "_WorldSpaceCameraPos", camera.position() )
        }
    }

    void renderShadows( ScriptableRenderContext ctx, Camera camera )
    {
        # 主方向光（取场景第一盏灯，没有则沿相机前向）
        # 简化：用相机的 VP 当光 VP（示意，真实实现要按光的朝向算正交矩阵）
        Float32_4x4 lightVP = camera.viewProjectionMatrix()
        this._shadowPass.setLightData( lightVP, this._shadowMap )

        ctx.cmd.setRenderTarget( RenderTargetBinding( null, this._shadowId ) )
        ctx.cmd.clearRenderTarget( false, true, Color.black() )
        if ctx.cullingResults != null
        {
            ctx.drawRenderers( ctx.cullingResults, 0, 5000, this._shadowMaterial )
        }
    }

    void renderColor( ScriptableRenderContext ctx, Camera camera )
    {
        # 深度预通道
        if this._useDepthPrepass
        {
            ctx.cmd.setRenderTarget( RenderTargetBinding( null, this._depthId ) )
            if ctx.cullingResults != null
            {
                ctx.drawRenderers( ctx.cullingResults, 0, 2500, this._shadowMaterial )
            }
        }

        # 不透明 + 天空盒 + 透明，均写入 HDR 颜色缓冲
        RenderTargetBinding sceneBinding = RenderTargetBinding( this._colorId, this._depthId )
        ctx.cmd.setRenderTarget( sceneBinding )
        ctx.cmd.clearRenderTarget( true, true, this._clearColor )

        if ctx.cullingResults != null
        {
            ctx.drawRenderers( ctx.cullingResults, 0, 2500, null )
            ctx.drawSkybox( camera )
            ctx.drawRenderers( ctx.cullingResults, 2500, 5000, null )
        }
    }

    void renderPost( ScriptableRenderContext ctx, Camera camera )
    {
        ctx.cmd.setRenderTarget( RenderTargetBinding( RenderTargetIdentifier.backBuffer(), null ) )
        ctx.cmd.clearRenderTarget( true, true, Color.black() )
        if this._colorTarget != null
        {
            ctx.cmd.setTexture( 0, this._colorTarget.srv() )
        }
        ctx.drawFullscreen( this._postMaterial )
    }

    # ── 场景注入（引擎侧每帧更新可见物体列表）──────────────
    public void setSceneRenderers( Array<VisibleRenderer> renderers )
    {
        this.sceneRenderers = renderers
    }

    override void dispose()
    {
        if this._rtPool != null
        {
            this._rtPool.releaseAll()
        }
        if this._litPso != null
        {
            this._litPso.destroy()
        }
        if this._shadowPso != null
        {
            this._shadowPso.destroy()
        }
        if this._skyboxPso != null
        {
            this._skyboxPso.destroy()
        }
        if this._postPso != null
        {
            this._postPso.destroy()
        }
        if this._colorTarget != null
        {
            this._colorTarget.destroy()
        }
        if this._depthTarget != null
        {
            this._depthTarget.destroy()
        }
        if this._shadowMap != null
        {
            this._shadowMap.destroy()
        }
        super.dispose()
    }
}
