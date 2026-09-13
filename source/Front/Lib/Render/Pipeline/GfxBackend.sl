# GfxBackend —— 图形后端 / 平台能力 / 设备描述符
#
# 兼容矩阵（与 Unity GraphicsDeviceType 一个思路）：
#   D3D11 / D3D12  Windows（DXGI 交换链）
#   Vulkan         Windows / Linux / Android（VK_KHR_* 扩展）
#   Metal          macOS / iOS
#   OpenGL         桌面（WGL / GLX / CGL 上下文）
#   OpenGLES       Android / iOS / 嵌入式（EGL 上下文）
#   WebGL          Web（canvas 上下文）
#   Software       无 GPU 兜底（CPU 光栅，用于 CI / 服务端）
#
# 统一流程：PlatformWindow -> GfxDeviceDescriptor -> GfxDevice -> SwapChain
# 后端不支持的特性由 GfxCapabilities 描述，上层按能力降级（例如 GLES2 无计算着色器）

enum EGraphicsBackend
{
    Unknown = 0
    D3D11
    D3D12
    Vulkan
    Metal
    OpenGL
    OpenGLES
    WebGL
    Software
}

enum ED3DFeatureLevel
{
    Level_11_0 = 0
    Level_11_1
    Level_12_0
    Level_12_1
    Level_12_2
}

enum EGLProfile
{
    Core = 0
    Compatibility
    ES
}

# ── 设备描述符 ─────────────────────────────────────────
public class GfxDeviceDescriptor
{
    public EGraphicsBackend backend = EGraphicsBackend.D3D12
    public EWindowSystem windowSystem = EWindowSystem.Win32

    # OpenGL / GLES 版本
    public Int32 glMajorVersion = 4
    public Int32 glMinorVersion = 5
    public EGLProfile glProfile = EGLProfile.Core

    # D3D 特性等级
    public ED3DFeatureLevel d3dFeatureLevel = ED3DFeatureLevel.Level_11_0

    public Int32 msaaSamples = 1
    public Int32 swapChainBufferCount = 2
    public bool vsync = true
    public bool enableHDR = false

    # 调试 / 校验层：D3D Debug Layer、Vulkan Validation、GL debug output
    public bool enableDebugLayer = false
    public bool enableGpuValidation = false
    public bool enableValidationLayer = false

    # 后端不可用时按顺序降级（例如 D3D12 -> D3D11 -> OpenGL）
    public Array<Int32> fallbackBackends = null

    # Vulkan / GL 需要的扩展名
    public Array<string> requiredExtensions = null
    public Array<string> optionalExtensions = null

    public string applicationName = "SimpleLanguage"

    public void _init_()
    {
        this.backend = EGraphicsBackend.D3D12
        this.windowSystem = EWindowSystem.Win32
        this.glMajorVersion = 4
        this.glMinorVersion = 5
        this.glProfile = EGLProfile.Core
        this.d3dFeatureLevel = ED3DFeatureLevel.Level_11_0
        this.msaaSamples = 1
        this.swapChainBufferCount = 2
        this.vsync = true
        this.enableHDR = false
        this.fallbackBackends = Array<Int32>( 0 )
        this.requiredExtensions = Array<string>( 0 )
        this.optionalExtensions = Array<string>( 0 )
        this.applicationName = "SimpleLanguage"
    }

    public void addFallback( EGraphicsBackend b )
    {
        Array<Int32> nb = Array<Int32>( this.fallbackBackends.length + 1 )
        int i = 0
        while i < this.fallbackBackends.length
        {
            nb[i] = this.fallbackBackends[i]
            i++
        }
        nb[ this.fallbackBackends.length ] = b
        this.fallbackBackends = nb
    }

    public void requireExtension( string name )
    {
        Array<string> ne = Array<string>( this.requiredExtensions.length + 1 )
        int i = 0
        while i < this.requiredExtensions.length
        {
            ne[i] = this.requiredExtensions[i]
            i++
        }
        ne[ this.requiredExtensions.length ] = name
        this.requiredExtensions = ne
    }

    # ── 预设 ─────────────────────────────────────────────
    # Windows 桌面默认：D3D12，不可用时降到 D3D11，再降到 OpenGL
    public static GfxDeviceDescriptor defaultDesktop()
    {
        GfxDeviceDescriptor d = GfxDeviceDescriptor()
        d.backend = EGraphicsBackend.D3D12
        d.windowSystem = EWindowSystem.Win32
        d.addFallback( EGraphicsBackend.D3D11 )
        d.addFallback( EGraphicsBackend.OpenGL )
        ret d
    }

    public static GfxDeviceDescriptor d3d12()
    {
        GfxDeviceDescriptor d = GfxDeviceDescriptor()
        d.backend = EGraphicsBackend.D3D12
        d.d3dFeatureLevel = ED3DFeatureLevel.Level_12_0
        ret d
    }

    public static GfxDeviceDescriptor vulkan()
    {
        GfxDeviceDescriptor d = GfxDeviceDescriptor()
        d.backend = EGraphicsBackend.Vulkan
        d.requireExtension( "VK_KHR_swapchain" )
        ret d
    }

    # 桌面 OpenGL 4.5 Core（WGL / GLX / CGL）
    public static GfxDeviceDescriptor openGL()
    {
        GfxDeviceDescriptor d = GfxDeviceDescriptor()
        d.backend = EGraphicsBackend.OpenGL
        d.glMajorVersion = 4
        d.glMinorVersion = 5
        d.glProfile = EGLProfile.Core
        ret d
    }

    # OpenGL ES 3.x（Android / iOS / 嵌入式，走 EGL）
    public static GfxDeviceDescriptor openGLES30()
    {
        GfxDeviceDescriptor d = GfxDeviceDescriptor()
        d.backend = EGraphicsBackend.OpenGLES
        d.glMajorVersion = 3
        d.glMinorVersion = 0
        d.glProfile = EGLProfile.ES
        ret d
    }

    public static GfxDeviceDescriptor metal()
    {
        GfxDeviceDescriptor d = GfxDeviceDescriptor()
        d.backend = EGraphicsBackend.Metal
        d.windowSystem = EWindowSystem.Cocoa
        ret d
    }

    # 无窗口离屏（烘焙 / 服务器 / 单元测试）
    public static GfxDeviceDescriptor headless()
    {
        GfxDeviceDescriptor d = GfxDeviceDescriptor()
        d.backend = EGraphicsBackend.Software
        d.windowSystem = EWindowSystem.Headless
        d.vsync = false
        ret d
    }

    # 从渲染配置构造（兼容旧 Config 入口）
    public static GfxDeviceDescriptor fromConfig( Config config )
    {
        GfxDeviceDescriptor d = GfxDeviceDescriptor()
        d.msaaSamples = config.msaaSamples
        d.vsync = config.vsync
        d.backend = GfxBackend.parseBackend( config.backend )
        ret d
    }

    override string toString()
    {
        ret "GfxDeviceDescriptor(" + GfxBackend.backendName( this.backend ) + ", msaa=" + this.msaaSamples.toString() + ")"
    }
}

# ── 能力集 ─────────────────────────────────────────────
public class GfxCapabilities
{
    public EGraphicsBackend backend = EGraphicsBackend.Unknown
    public string deviceName = ""
    public string vendorName = ""
    public string driverVersion = ""
    public string apiVersion = ""

    public Int32 maxTextureSize = 4096
    public Int32 maxTextureArrayLayers = 256
    public Int32 maxRenderTargetCount = 8
    public Int32 maxVertexAttributes = 16
    public Int32 maxVertexBuffers = 8

    # 对齐要求：常量缓冲偏移必须按它对齐（DX12 = 256）
    public Int32 constantBufferOffsetAlignment = 256
    public Int32 texturePitchAlignment = 256
    public Int32 maxSamplerAnisotropy = 16

    public Int32 maxComputeWorkGroupSize = 0
    public Int32 maxComputeSharedMemory = 0

    public Int64 dedicatedVideoMemory = 0
    public Int64 sharedSystemMemory = 0

    # 特性开关
    public bool supportsCompute = false
    public bool supportsGeometryShader = false
    public bool supportsTessellation = false
    public bool supportsInstancing = false
    public bool supportsMSAA = false
    public bool supportsSRGB = false
    public bool supportsAnisotropicFilter = false
    public bool supportsDepthBounds = false
    public bool supportsOcclusionQuery = false
    public bool supportsMultiDrawIndirect = false
    public bool supportsTimelineFence = false
    public bool supportsBindless = false
    public bool supportsRayTracing = false
    public bool supportsMeshShader = false
    public bool supportsVariableRateShading = false

    # 纹理压缩：BC(桌面 DX) / ETC(安卓) / ASTC / PVRTC(iOS)
    public bool supportsBC = false
    public bool supportsETC2 = false
    public bool supportsASTC = false
    public bool supportsPVRTC = false

    # GL 系没有描述符堆与 PSO，后端内部用 VAO + Program 模拟
    public get bool isModernApi()
    {
        ret this.backend == EGraphicsBackend.D3D12 || this.backend == EGraphicsBackend.Vulkan || this.backend == EGraphicsBackend.Metal
    }

    public get bool isGlStyle()
    {
        ret this.backend == EGraphicsBackend.OpenGL || this.backend == EGraphicsBackend.OpenGLES || this.backend == EGraphicsBackend.WebGL
    }

    public static GfxCapabilities query( GfxDeviceDescriptor desc )
    {
        object result = SystemCallExternalFunction( "Render.queryCapabilities", desc.backend )
        if result is GfxCapabilities c
        {
            ret c
        }
        GfxCapabilities caps = GfxCapabilities()
        caps.backend = desc.backend
        ret caps
    }

    override string toString()
    {
        ret "GfxCapabilities(" + GfxBackend.backendName( this.backend ) + ", " + this.deviceName + ", " + this.apiVersion + ")"
    }
}

# ── 后端 / 平台探测与选择 ──────────────────────────────
public class GfxBackend
{
    public static string backendName( EGraphicsBackend b )
    {
        if b == EGraphicsBackend.D3D11
        {
            ret "Direct3D11"
        }
        if b == EGraphicsBackend.D3D12
        {
            ret "Direct3D12"
        }
        if b == EGraphicsBackend.Vulkan
        {
            ret "Vulkan"
        }
        if b == EGraphicsBackend.Metal
        {
            ret "Metal"
        }
        if b == EGraphicsBackend.OpenGL
        {
            ret "OpenGL"
        }
        if b == EGraphicsBackend.OpenGLES
        {
            ret "OpenGLES"
        }
        if b == EGraphicsBackend.WebGL
        {
            ret "WebGL"
        }
        if b == EGraphicsBackend.Software
        {
            ret "Software"
        }
        ret "Unknown"
    }

    # 反解析：配置里的字符串 -> 枚举
    public static EGraphicsBackend parseBackend( string name )
    {
        if name == "d3d12" || name == "direct3d12"
        {
            ret EGraphicsBackend.D3D12
        }
        if name == "d3d11" || name == "direct3d11"
        {
            ret EGraphicsBackend.D3D11
        }
        if name == "vulkan"
        {
            ret EGraphicsBackend.Vulkan
        }
        if name == "metal"
        {
            ret EGraphicsBackend.Metal
        }
        if name == "opengl"
        {
            ret EGraphicsBackend.OpenGL
        }
        if name == "opengles" || name == "gles"
        {
            ret EGraphicsBackend.OpenGLES
        }
        if name == "webgl"
        {
            ret EGraphicsBackend.WebGL
        }
        ret EGraphicsBackend.Software
    }

    # 当前进程可用的后端列表（由后端实现探测）
    public static Array<Int32> availableBackends()
    {
        object result = SystemCallExternalFunction( "Render.getAvailableBackends" )
        if result is Array<Int32> list
        {
            ret list
        }
        ret Array<Int32>( 0 )
    }

    public static bool isBackendAvailable( EGraphicsBackend b )
    {
        Array<Int32> list = GfxBackend.availableBackends()
        int i = 0
        while i < list.length
        {
            if list[i] == b
            {
                ret true
            }
            i++
        }
        ret false
    }

    # 首选 + 降级链，返回第一个可用的
    public static EGraphicsBackend selectBest( EGraphicsBackend preferred, Array<Int32> fallbacks )
    {
        if GfxBackend.isBackendAvailable( preferred )
        {
            ret preferred
        }
        int i = 0
        while i < fallbacks.length
        {
            if GfxBackend.isBackendAvailable( fallbacks[i] )
            {
                ret fallbacks[i]
            }
            i++
        }
        ret EGraphicsBackend.Software
    }

    # ── 平台探测 ─────────────────────────────────────────
    public static EWindowSystem currentWindowSystem()
    {
        object result = SystemCallExternalFunction( "Render.getCurrentWindowSystem" )
        if result is Int32 v
        {
            ret v
        }
        ret EWindowSystem.Headless
    }

    public static bool isWindows()
    {
        ret GfxBackend.currentWindowSystem() == EWindowSystem.Win32
    }

    public static bool isLinux()
    {
        EWindowSystem s = GfxBackend.currentWindowSystem()
        ret s == EWindowSystem.X11 || s == EWindowSystem.Wayland
    }

    public static bool isMacOS()
    {
        ret GfxBackend.currentWindowSystem() == EWindowSystem.Cocoa
    }

    public static bool isAndroid()
    {
        ret GfxBackend.currentWindowSystem() == EWindowSystem.Android
    }

    public static bool isIOS()
    {
        ret GfxBackend.currentWindowSystem() == EWindowSystem.IOS
    }

    # GL / GLES 扩展查询
    public static bool supportsExtension( string name )
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.supportsGLExtension", name ) )
    }

    public static string graphicsApiVersion()
    {
        object result = SystemCallExternalFunction( "Render.getGraphicsApiVersion" )
        if result is string s
        {
            ret s
        }
        ret ""
    }

    # 一步到位：探测后端 -> 建窗口 -> 建设备
    public static GfxDevice createDevice( GfxDeviceDescriptor desc )
    {
        EGraphicsBackend chosen = GfxBackend.selectBest( desc.backend, desc.fallbackBackends )
        GfxDevice device = GfxDevice()
        device.desc.backend = chosen
        device.desc.windowSystem = desc.windowSystem
        device.desc.msaaSamples = desc.msaaSamples
        device.desc.vsync = desc.vsync
        device.desc.enableDebugLayer = desc.enableDebugLayer
        device.desc.glMajorVersion = desc.glMajorVersion
        device.desc.glMinorVersion = desc.glMinorVersion
        device.desc.glProfile = desc.glProfile
        device.desc.d3dFeatureLevel = desc.d3dFeatureLevel
        device.initialize( device.desc )
        ret device
    }
}
