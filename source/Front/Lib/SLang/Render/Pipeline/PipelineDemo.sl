# PipelineDemo —— 把整条「设备 -> 管线 -> 一帧渲染」串起来的示例入口
#
# 用法（DX12 / Vulkan 风格）：
#   PipelineDemo demo = PipelineDemo()
#   demo.startup( 1280, 720 )
#   demo.renderFrame( camera, scene )
#   demo.shutdown()
#
# 只演示调用流程，不验证；后端 Render.xxx 由具体实现注册。

public class PipelineDemo
{
    public GfxDevice device = null
    public UniversalRenderPipeline pipeline = null
    public RenderPipelineManager manager = null

    public void _init_()
    {
        this.device = null
        this.pipeline = null
        this.manager = null
    }

    # ── 启动：建设备 + 装管线 ───────────────────────────
    public bool startup( Int32 width, Int32 height )
    {
        # 1) 设备描述：默认 D3D12 -> D3D11 -> OpenGL 降级
        GfxDeviceDescriptor desc = GfxDeviceDescriptor.defaultDesktop()
        desc.windowSystem = EWindowSystem.Win32
        desc.msaaSamples = 1
        desc.vsync = true

        this.device = GfxDevice()
        if !this.device.initialize( desc )
        {
            # 全部后端失败则退到软件光栅（CI / 无 GPU）
            desc.backend = EGraphicsBackend.Software
            desc.windowSystem = EWindowSystem.Headless
            if !this.device.initialize( desc )
            {
                ret false
            }
        }

        # 2) URP 资产 + 管线
        UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset()
        asset.shadowMapSize = 2048
        asset.useDepthPrepass = true
        asset.usePostProcessing = true

        Config cfg = Config()
        cfg.width = width
        cfg.height = height
        cfg.msaaSamples = 1
        cfg.useDepthPrepass = true
        cfg.usePostProcessing = true
        cfg.shadowMapSize = 2048

        this.pipeline = UniversalRenderPipeline()
        this.pipeline.initialize( this.device, cfg )

        # 3) 挂到管理器
        this.manager = RenderPipelineManager.instance()
        this.manager.setDevice( this.device )
        this.manager.activePipeline = this.pipeline
        this.manager.context = this.pipeline.context
        this.manager.isValid = this.pipeline.isInitialized
        ret this.manager.isValid
    }

    # ── 每帧：剔除 -> 渲染 -> 提交 -> 呈现 ───────────────
    public void renderFrame( Camera camera, Array<VisibleRenderer> scene )
    {
        if this.manager == null || !this.manager.isValid
        {
            ret
        }
        this.manager.beginFrame()

        this.pipeline.setSceneRenderers( scene )
        this.manager.renderCamera( camera )

        this.manager.endFrame()
        this.manager.present()
    }

    # ── 关闭 ───────────────────────────────────────────
    public void shutdown()
    {
        if this.manager != null
        {
            this.manager.shutdown()
        }
    }

    # 一次性跑通（无窗口环境也能走完流程）
    static void quickStart()
    {
        PipelineDemo demo = PipelineDemo()
        if demo.startup( 1280, 720 )
        {
            Camera cam = Camera()
            cam.name = "MainCamera"
            cam.fieldOfView = 60.0f
            cam.nearClipPlane = 0.1f
            cam.farClipPlane = 1000.0f

            Array<VisibleRenderer> scene = Array<VisibleRenderer>( 0 )
            demo.renderFrame( cam, scene )
            demo.shutdown()
        }
    }
}
