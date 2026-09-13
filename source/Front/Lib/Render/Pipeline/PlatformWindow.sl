# PlatformWindow —— 窗口系统 / 原生表面抽象（WSI）
#
# 各平台原生对象：
#   Win32   : HWND + HINSTANCE + HDC，消息泵走 GetMessage / DispatchMessage
#   Linux   : X11 Window + Display* / Wayland wl_surface
#   macOS   : NSWindow / CAMetalLayer（Metal）或 NSOpenGLView（GL）
#   Android : ANativeWindow*（EGL 表面）
#   iOS     : UIView / CAEAGLLayer
#   Web     : HTMLCanvasElement（WebGL 上下文）
#   Headless: 无窗口，离屏渲染 / 服务端烘焙
#
# SL 侧只持有句柄（Int64 透传），真正创建与消息处理由后端 SystemCall 完成

enum EWindowSystem
{
    Headless = 0
    Win32
    X11
    Wayland
    Cocoa
    Android
    IOS
    Web
}

# ── 通用窗口 ───────────────────────────────────────────
public class PlatformWindow
{
    public EWindowSystem system = EWindowSystem.Win32
    public string title = "SimpleLanguage"

    public Int32 x = 100
    public Int32 y = 100
    public Int32 width = 1280
    public Int32 height = 720

    public bool fullscreen = false
    public bool resizable = true
    public bool visible = true
    public bool isCreated = false

    # 原生句柄：Win32=HWND / X11=Window / Android=ANativeWindow*
    public Int64 nativeHandle = 0
    # 显示连接：X11=Display* / EGLDisplay
    public Int64 nativeDisplay = 0
    # 实例：Win32=HINSTANCE / Wayland=wl_display
    public Int64 nativeInstance = 0

    public void _init_()
    {
        this.system = EWindowSystem.Win32
        this.title = "SimpleLanguage"
        this.width = 1280
        this.height = 720
        this.fullscreen = false
        this.resizable = true
        this.visible = true
        this.isCreated = false
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createWindow",
            this.system, this.title, this.x, this.y, this.width, this.height,
            this.fullscreen, this.resizable )
        if result is Int64 h
        {
            this.nativeHandle = h
            this.isCreated = true
            ret true
        }
        if GpuUtil.asBool( result )
        {
            this.isCreated = true
            ret true
        }
        ret false
    }

    public void destroy()
    {
        if this.isCreated
        {
            SystemCallExternalFunction( "Render.destroyWindow", this.nativeHandle )
            this.isCreated = false
            this.nativeHandle = 0
        }
    }

    public void resize( Int32 w, Int32 h )
    {
        this.width = w
        this.height = h
        SystemCallExternalFunction( "Render.resizeWindow", this.nativeHandle, w, h )
    }

    public void setTitle( string t )
    {
        this.title = t
        SystemCallExternalFunction( "Render.setWindowTitle", this.nativeHandle, t )
    }

    public void setFullscreen( bool enabled )
    {
        this.fullscreen = enabled
        SystemCallExternalFunction( "Render.setWindowFullscreen", this.nativeHandle, enabled )
    }

    # 消息泵：Win32 需要每帧调用；返回 false 表示收到退出消息
    public bool pumpMessages()
    {
        object result = SystemCallExternalFunction( "Render.pumpWindowMessages", this.nativeHandle )
        if result is bool alive
        {
            ret alive
        }
        ret true
    }

    public get Float32 aspect()
    {
        if this.height <= 0
        {
            ret 1.0f
        }
        ret SystemConvertFloat32( this.width ) / SystemConvertFloat32( this.height )
    }

    # ── 工厂 ─────────────────────────────────────────────
    public static PlatformWindow forWin32( string title, Int32 w, Int32 h )
    {
        PlatformWindow win = PlatformWindow()
        win.system = EWindowSystem.Win32
        win.title = title
        win.width = w
        win.height = h
        ret win
    }

    public static PlatformWindow forHeadless( Int32 w, Int32 h )
    {
        PlatformWindow win = PlatformWindow()
        win.system = EWindowSystem.Headless
        win.width = w
        win.height = h
        win.visible = false
        ret win
    }

    public static PlatformWindow forSystem( EWindowSystem sys, Int32 w, Int32 h )
    {
        PlatformWindow win = PlatformWindow()
        win.system = sys
        win.width = w
        win.height = h
        ret win
    }

    override string toString()
    {
        ret "PlatformWindow(" + this.system.toString() + ", " + this.width.toString() + "x" + this.height.toString() + ")"
    }
}

# ── Win32 专用（HWND / HINSTANCE / WndProc / 消息）────────
public class Win32Window extends PlatformWindow
{
    # RegisterClassEx 用的窗口类名
    public string className = "SLWindowClass"

    # 窗口样式：WS_OVERLAPPEDWINDOW 等
    public Int32 style = 0
    public Int32 exStyle = 0

    # 设备上下文（OpenGL 走 HDC，DXGI 不需要）
    public Int64 deviceContext = 0

    # 高 DPI：1=系统感知 2=每显示器感知
    public Int32 dpiAwareness = 2

    public void _init_()
    {
        this.system = EWindowSystem.Win32
        this.className = "SLWindowClass"
        this.style = 0
        this.exStyle = 0
        this.deviceContext = 0
        this.dpiAwareness = 2
    }

    # 设置进程 DPI 感知（SetProcessDpiAwarenessContext）
    public bool setDpiAwareness()
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.win32SetDpiAwareness", this.dpiAwareness ) )
    }

    # RegisterClassEx + CreateWindowEx
    override bool create()
    {
        this.setDpiAwareness()
        object result = SystemCallExternalFunction( "Render.win32CreateWindow",
            this.className, this.title, this.x, this.y, this.width, this.height,
            this.style, this.exStyle )
        if result is Int64 hwnd
        {
            this.nativeHandle = hwnd
            this.deviceContext = this.getDeviceContext()
            this.isCreated = true
            ret true
        }
        this.isCreated = false
        ret false
    }

    # GetDC：OpenGL 需要，D3D/Vulkan 通过交换链自己管理
    public Int64 getDeviceContext()
    {
        object result = SystemCallExternalFunction( "Render.win32GetDC", this.nativeHandle )
        if result is Int64 hdc
        {
            ret hdc
        }
        ret 0
    }

    public void releaseDeviceContext()
    {
        SystemCallExternalFunction( "Render.win32ReleaseDC", this.nativeHandle, this.deviceContext )
        this.deviceContext = 0
    }

    # ShowWindow(SW_SHOW) / UpdateWindow
    public void show()
    {
        SystemCallExternalFunction( "Render.win32ShowWindow", this.nativeHandle )
        this.visible = true
    }

    # 取客户区尺寸（GetClientRect），可能与窗口边框尺寸不同
    public Int32 clientWidth()
    {
        object result = SystemCallExternalFunction( "Render.win32GetClientWidth", this.nativeHandle )
        if result is Int32 w
        {
            ret w
        }
        ret this.width
    }

    public Int32 clientHeight()
    {
        object result = SystemCallExternalFunction( "Render.win32GetClientHeight", this.nativeHandle )
        if result is Int32 h
        {
            ret h
        }
        ret this.height
    }

    # PeekMessage 版本：不阻塞，用于游戏主循环
    public bool peekMessages()
    {
        object result = SystemCallExternalFunction( "Render.win32PeekMessages" )
        if result is bool alive
        {
            ret alive
        }
        ret true
    }

    # 向窗口投递退出消息（PostQuitMessage）
    public void postQuit()
    {
        SystemCallExternalFunction( "Render.win32PostQuit", this.nativeHandle )
    }

    override string toString()
    {
        ret "Win32Window(hwnd=" + this.nativeHandle.toString() + ", " + this.clientWidth().toString() + "x" + this.clientHeight().toString() + ")"
    }
}

# ── Linux / X11 ───────────────────────────────────────
public class X11Window extends PlatformWindow
{
    public Int32 screenIndex = 0
    public Int64 visual = 0

    public void _init_()
    {
        this.system = EWindowSystem.X11
        this.screenIndex = 0
        this.visual = 0
    }

    override bool create()
    {
        object result = SystemCallExternalFunction( "Render.x11CreateWindow",
            this.title, this.x, this.y, this.width, this.height )
        if result is Int64 w
        {
            this.nativeHandle = w
            this.isCreated = true
            ret true
        }
        ret false
    }

    public bool pumpEvents()
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.x11PumpEvents", this.nativeDisplay ) )
    }
}

# ── 移动端 / Web（ANativeWindow、UIView、Canvas）──────────
public class MobileWindow extends PlatformWindow
{
    # Android: ANativeWindow；iOS: UIView；Web: canvas
    public Int64 nativeSurface = 0
    public Int32 orientation = 0

    public void _init_( EWindowSystem sys )
    {
        this.system = sys
        this.nativeSurface = 0
        this.orientation = 0
    }

    # 由平台回调把 ANativeWindow / UIView 传进来
    public void bindSurface( Int64 surface )
    {
        this.nativeSurface = surface
        SystemCallExternalFunction( "Render.bindNativeSurface", this.system, surface )
    }

    public void onSurfaceChanged( Int32 w, Int32 h )
    {
        this.width = w
        this.height = h
        SystemCallExternalFunction( "Render.onSurfaceChanged", this.nativeSurface, w, h )
    }

    public void onSurfaceDestroyed()
    {
        SystemCallExternalFunction( "Render.onSurfaceDestroyed", this.nativeSurface )
        this.isCreated = false
    }
}
