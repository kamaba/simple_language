# GfxDevice —— 逻辑设备（DX12 ID3D12Device / Vulkan VkDevice / GL 上下文 / Metal device）
#
# 统一入口：GfxDeviceDescriptor -> 创建窗口 -> 按后端初始化 -> 交换链 + 队列 + 描述符堆
#
# 后端差异处理：
#   D3D12 / Vulkan / Metal ：显式 API，有命令队列、描述符堆、PSO、资源屏障
#   OpenGL / OpenGLES      ：隐式 API，无 PSO / 无描述符堆，后端内部用
#                            VAO + Program + 状态缓存模拟；同步走 glFinish / fence
#   Software               ：CPU 光栅，接口同 D3D12 语义，便于无 GPU 环境跑通流程
#
# 窗口系统：Win32(HWND) / X11 / Wayland / Cocoa / Android / iOS / Web / Headless

public class GfxDevice
{
    public GfxDeviceDescriptor desc = null
    public GfxCapabilities caps = null
    public PlatformWindow window = null

    # 后端：d3d11 / d3d12 / vulkan / metal / opengl / opengles / webgl / software
    public EGraphicsBackend backend = EGraphicsBackend.D3D12
    public EWindowSystem windowSystem = EWindowSystem.Win32

    public SwapChain swapChain = null
    public CommandQueue graphicsQueue = null
    public CommandQueue computeQueue = null
    public CommandQueue copyQueue = null

    public DescriptorHeap rtvHeap = null
    public DescriptorHeap dsvHeap = null
    public DescriptorHeap cbvSrvUavHeap = null
    public DescriptorHeap samplerHeap = null

    public RootSignature defaultSignature = null
    public CommandBufferPool cmdPool = null
    public Fence frameFence = null

    # GL 系上下文（OpenGL / GLES / WebGL 有效）
    public Int64 glContext = 0
    public bool glContextOwned = false

    public Int32 frameIndex = 0
    public Int32 frameLatency = 2
    public bool isValid = false

    public void _init_()
    {
        this.desc = GfxDeviceDescriptor()
        this.caps = GfxCapabilities()
        this.backend = EGraphicsBackend.D3D12
        this.windowSystem = EWindowSystem.Win32
        this.cmdPool = CommandBufferPool()
        this.frameIndex = 0
        this.isValid = false
    }

    # ── 初始化（新接口）──────────────────────────────────
    public bool initialize( GfxDeviceDescriptor descriptor )
    {
        if descriptor == null
        {
            ret false
        }
        this.desc = descriptor
        this.backend = descriptor.backend
        this.windowSystem = descriptor.windowSystem

        # 1) 窗口 / 原生表面
        if descriptor.windowSystem != EWindowSystem.Headless
        {
            this.window = this.createWindow( descriptor )
            if this.window == null
            {
                ret false
            }
        }

        # 2) 按后端初始化设备
        bool ok = false
        if this.backend == EGraphicsBackend.D3D12
        {
            ok = this.createD3D12()
        }
        elif this.backend == EGraphicsBackend.D3D11
        {
            ok = this.createD3D11()
        }
        elif this.backend == EGraphicsBackend.Vulkan
        {
            ok = this.createVulkan()
        }
        elif this.backend == EGraphicsBackend.Metal
        {
            ok = this.createMetal()
        }
        elif this.backend == EGraphicsBackend.OpenGL
        {
            ok = this.createOpenGL()
        }
        elif this.backend == EGraphicsBackend.OpenGLES
        {
            ok = this.createOpenGLES()
        }
        elif this.backend == EGraphicsBackend.WebGL
        {
            ok = this.createWebGL()
        }
        else
        {
            ok = this.createSoftware()
        }

        if !ok
        {
            this.isValid = false
            ret false
        }

        # 3) 能力查询 + 交换链
        this.caps = GfxCapabilities.query( descriptor )
        this.caps.backend = this.backend
        this.createSwapChain()

        # 4) 默认根签名（GL 系不需要，后端内部忽略）
        if this.caps.isModernApi()
        {
            this.defaultSignature = RootSignature.defaultGraphics()
            this.defaultSignature.create()
        }

        this.frameFence = Fence()
        this.frameFence.create()
        this.isValid = true
        ret true
    }

    # 兼容旧入口：从渲染配置直接初始化
    public bool initialize( Config config )
    {
        ret this.initialize( GfxDeviceDescriptor.fromConfig( config ) )
    }

    # ── 窗口 ─────────────────────────────────────────────
    PlatformWindow createWindow( GfxDeviceDescriptor descriptor )
    {
        PlatformWindow win = null
        if descriptor.windowSystem == EWindowSystem.Win32
        {
            win = Win32Window()
            win.title = descriptor.applicationName
        }
        elif descriptor.windowSystem == EWindowSystem.X11
        {
            win = X11Window()
            win.title = descriptor.applicationName
        }
        elif descriptor.windowSystem == EWindowSystem.Android
        {
            win = MobileWindow( EWindowSystem.Android )
        }
        elif descriptor.windowSystem == EWindowSystem.IOS
        {
            win = MobileWindow( EWindowSystem.IOS )
        }
        elif descriptor.windowSystem == EWindowSystem.Web
        {
            win = MobileWindow( EWindowSystem.Web )
        }
        else
        {
            win = PlatformWindow.forSystem( descriptor.windowSystem, 1280, 720 )
        }
        if !win.create()
        {
            ret null
        }
        ret win
    }

    # ── D3D12 ───────────────────────────────────────────
    bool createD3D12()
    {
        object result = SystemCallExternalFunction( "Render.createDeviceD3D12",
            this.desc.d3dFeatureLevel, this.desc.enableDebugLayer, this.desc.enableGpuValidation )
        if !GpuUtil.asBool( result )
        {
            ret false
        }
        this.createQueues()
        this.createDescriptorHeaps()
        ret true
    }

    bool createD3D11()
    {
        object result = SystemCallExternalFunction( "Render.createDeviceD3D11",
            this.desc.d3dFeatureLevel, this.desc.enableDebugLayer )
        if !GpuUtil.asBool( result )
        {
            ret false
        }
        this.createQueues()
        ret true
    }

    # ── Vulkan ──────────────────────────────────────────
    bool createVulkan()
    {
        object result = SystemCallExternalFunction( "Render.createDeviceVulkan",
            this.desc.applicationName, this.desc.enableValidationLayer,
            this.desc.requiredExtensions, this.desc.optionalExtensions )
        if !GpuUtil.asBool( result )
        {
            ret false
        }
        this.createQueues()
        this.createDescriptorHeaps()
        ret true
    }

    # ── Metal（macOS / iOS）──────────────────────────────
    bool createMetal()
    {
        object result = SystemCallExternalFunction( "Render.createDeviceMetal", this.window.nativeHandle )
        if !GpuUtil.asBool( result )
        {
            ret false
        }
        this.createQueues()
        this.createDescriptorHeaps()
        ret true
    }

    # ── OpenGL 桌面（WGL / GLX / CGL 上下文）───────────────
    bool createOpenGL()
    {
        object result = SystemCallExternalFunction( "Render.createGLContext",
            EGraphicsBackend.OpenGL, this.desc.glMajorVersion, this.desc.glMinorVersion,
            this.desc.glProfile, this.window.nativeHandle, this.window.nativeDisplay )
        if result is Int64 ctx
        {
            this.glContext = ctx
            this.glContextOwned = true
        }
        else
        {
            if !GpuUtil.asBool( result )
            {
                ret false
            }
        }
        this.makeCurrent()
        SystemCallExternalFunction( "Render.glEnableDebugOutput", this.desc.enableDebugLayer )
        ret true
    }

    # ── OpenGL ES（EGL，Android / iOS / 嵌入式）────────────
    bool createOpenGLES()
    {
        object result = SystemCallExternalFunction( "Render.createGLContext",
            EGraphicsBackend.OpenGLES, this.desc.glMajorVersion, this.desc.glMinorVersion,
            EGLProfile.ES, this.window.nativeHandle, this.window.nativeDisplay )
        if result is Int64 ctx
        {
            this.glContext = ctx
            this.glContextOwned = true
        }
        else
        {
            if !GpuUtil.asBool( result )
            {
                ret false
            }
        }
        this.makeCurrent()
        ret true
    }

    bool createWebGL()
    {
        object result = SystemCallExternalFunction( "Render.createWebGLContext",
            this.desc.glMajorVersion, this.window.nativeHandle )
        if !GpuUtil.asBool( result )
        {
            ret false
        }
        ret true
    }

    bool createSoftware()
    {
        object result = SystemCallExternalFunction( "Render.createDeviceSoftware",
            this.desc.msaaSamples )
        if !GpuUtil.asBool( result )
        {
            ret false
        }
        this.createQueues()
        ret true
    }

    # ── GL 上下文操作 ────────────────────────────────────
    public void makeCurrent()
    {
        if this.glContext == 0
        {
            ret
        }
        SystemCallExternalFunction( "Render.glMakeCurrent", this.glContext, this.window.nativeHandle )
    }

    public void swapBuffers()
    {
        if !this.caps.isGlStyle()
        {
            ret
        }
        SystemCallExternalFunction( "Render.glSwapBuffers", this.window.nativeHandle )
    }

    public void destroyGLContext()
    {
        if this.glContextOwned && this.glContext != 0
        {
            SystemCallExternalFunction( "Render.destroyGLContext", this.glContext )
            this.glContext = 0
            this.glContextOwned = false
        }
    }

    # ── 通用资源 ─────────────────────────────────────────
    void createQueues()
    {
        this.graphicsQueue = CommandQueue( ECommandQueueType.Graphics )
        this.graphicsQueue.create()
        this.computeQueue = CommandQueue( ECommandQueueType.Compute )
        this.computeQueue.create()
        this.copyQueue = CommandQueue( ECommandQueueType.Copy )
        this.copyQueue.create()
    }

    void createDescriptorHeaps()
    {
        this.rtvHeap = DescriptorHeap( EDescriptorType.RTV, 64 )
        this.rtvHeap.create()
        this.dsvHeap = DescriptorHeap( EDescriptorType.DSV, 32 )
        this.dsvHeap.create()
        this.cbvSrvUavHeap = DescriptorHeap( EDescriptorType.CBV, 1024 )
        this.cbvSrvUavHeap.create()
        this.samplerHeap = DescriptorHeap( EDescriptorType.Sampler, 32 )
        this.samplerHeap.create()
    }

    void createSwapChain()
    {
        this.swapChain = SwapChain()
        if this.window != null
        {
            this.swapChain.width = this.window.width
            this.swapChain.height = this.window.height
        }
        this.swapChain.bufferCount = this.desc.swapChainBufferCount
        this.swapChain.vsync = this.desc.vsync
        this.swapChain.create()
    }

    # ── 命令缓冲 ─────────────────────────────────────────
    public CommandBuffer createCommandBuffer( string name )
    {
        ret this.cmdPool.get( name )
    }

    public void execute( CommandBuffer cb )
    {
        if cb == null || this.graphicsQueue == null
        {
            ret
        }
        this.graphicsQueue.execute( cb )
    }

    public void executeCompute( CommandBuffer cb )
    {
        if cb == null || this.computeQueue == null
        {
            ret
        }
        this.computeQueue.execute( cb )
    }

    # ── 帧 ───────────────────────────────────────────────
    public void beginFrame()
    {
        this.cmdPool.releaseAll()
        if this.window != null
        {
            this.window.pumpMessages()
        }
    }

    public void endFrame()
    {
        if this.graphicsQueue != null && this.frameFence != null
        {
            this.graphicsQueue.signal( this.frameFence, this.frameFence.value + 1 )
        }
    }

    # 等待上一帧（frameLatency 帧之前）完成，实现 CPU/GPU 重叠
    public void waitForNextFrame()
    {
        if this.frameFence == null
        {
            ret
        }
        Int64 target = SystemConvertInt64( this.frameIndex + 1 - this.frameLatency )
        if target > 0
        {
            this.frameFence.wait( target )
        }
        this.frameIndex++
    }

    public void present()
    {
        if this.caps.isGlStyle()
        {
            this.swapBuffers()
            ret
        }
        if this.swapChain != null
        {
            this.swapChain.present()
        }
    }

    public void waitForGpu()
    {
        if this.caps.isGlStyle()
        {
            SystemCallExternalFunction( "Render.glFinish" )
            ret
        }
        if this.graphicsQueue != null
        {
            this.graphicsQueue.waitIdle()
        }
    }

    public void resizeSwapChain( Int32 width, Int32 height )
    {
        if this.window != null
        {
            this.window.resize( width, height )
        }
        if this.swapChain != null
        {
            this.swapChain.resize( width, height )
        }
        if this.caps.isGlStyle()
        {
            SystemCallExternalFunction( "Render.glViewport", 0, 0, width, height )
        }
    }

    public GpuTexture currentBackBuffer()
    {
        if this.swapChain == null
        {
            ret null
        }
        ret this.swapChain.currentBackBuffer()
    }

    # ── 能力查询便捷接口 ─────────────────────────────────
    public bool supportsCompute()
    {
        if this.caps == null
        {
            ret false
        }
        ret this.caps.supportsCompute
    }

    public bool supportsMSAA( Int32 samples )
    {
        if this.caps == null
        {
            ret false
        }
        ret this.caps.supportsMSAA && samples > 1
    }

    public string backendName()
    {
        ret GfxBackend.backendName( this.backend )
    }

    # ── 释放 ─────────────────────────────────────────────
    public void destroy()
    {
        if this.defaultSignature != null
        {
            this.defaultSignature.destroy()
        }
        if this.frameFence != null
        {
            this.frameFence.destroy()
        }
        if this.graphicsQueue != null
        {
            this.graphicsQueue.destroy()
        }
        if this.computeQueue != null
        {
            this.computeQueue.destroy()
        }
        if this.copyQueue != null
        {
            this.copyQueue.destroy()
        }
        if this.swapChain != null
        {
            this.swapChain.destroy()
        }
        if this.caps != null && this.caps.isGlStyle()
        {
            this.destroyGLContext()
        }
        else
        {
            SystemCallExternalFunction( "Render.destroyDevice" )
        }
        if this.window != null
        {
            this.window.destroy()
        }
        this.isValid = false
    }

    override string toString()
    {
        ret "GfxDevice(" + this.backendName() + ", frame=" + this.frameIndex.toString() + ", valid=" + this.isValid.toString() + ")"
    }
}
