# 渲染核心：帧循环 + 绘制提交。
# 底层通过 SystemCallExternalFunction("Render.xxx", ...) 转交渲染后端。
#
# 说明：状态（是否初始化、绘制统计等）统一由后端维护，
# 本类只提供查询接口，避免使用静态可变字段。
public class Render
{
    # ── 初始化 / 释放 ────────────────────────────────────
    public static bool initialize( Config config )
    {
        if config == null
        {
            ret false
        }
        object result = SystemCallExternalFunction( "Render.initialize",
            config.width, config.height, config.fullscreen, config.vsync,
            config.msaaSamples, config.backend )
        ret Render._asBool( result )
    }

    public static void shutdown()
    {
        SystemCallExternalFunction( "Render.shutdown" )
    }

    public static bool isReady()
    {
        ret Render._asBool( SystemCallExternalFunction( "Render.isReady" ) )
    }

    # ── 帧循环 ───────────────────────────────────────────
    public static void beginFrame( Color clearColor )
    {
        if clearColor == null
        {
            SystemCallExternalFunction( "Render.beginFrame", 0.0f, 0.0f, 0.0f, 1.0f )
            ret
        }
        SystemCallExternalFunction( "Render.beginFrame",
            clearColor.r, clearColor.g, clearColor.b, clearColor.a )
    }

    public static void endFrame()
    {
        SystemCallExternalFunction( "Render.endFrame" )
    }

    # 交换缓冲（呈现到屏幕）
    public static void present()
    {
        SystemCallExternalFunction( "Render.present" )
    }

    # ── 状态设置 ─────────────────────────────────────────
    public static void setViewport( Int32 x, Int32 y, Int32 width, Int32 height )
    {
        SystemCallExternalFunction( "Render.setViewport", x, y, width, height )
    }

    public static void clear( Color c )
    {
        if c == null
        {
            SystemCallExternalFunction( "Render.clear", 0.0f, 0.0f, 0.0f, 1.0f )
            ret
        }
        SystemCallExternalFunction( "Render.clear", c.r, c.g, c.b, c.a )
    }

    # ── 绘制 ─────────────────────────────────────────────
    # model：本地 -> 世界；viewProjection：相机提供的视图投影矩阵
    public static bool drawMesh( Mesh mesh, Material material, Float32_4x4 model, Float32_4x4 viewProjection )
    {
        if mesh == null || material == null
        {
            ret false
        }
        object result = SystemCallExternalFunction( "Render.drawMesh",
            mesh, material, model, viewProjection )
        ret Render._asBool( result )
    }

    # 便捷重载：只给世界矩阵与相机
    public static bool drawMesh( Mesh mesh, Material material, Float32_4x4 viewProjection )
    {
        ret Render.drawMesh( mesh, material, MatrixUtil.identity(), viewProjection )
    }

    # ── 统计 ─────────────────────────────────────────────
    public static Int32 getDrawCallCount()
    {
        object result = SystemCallExternalFunction( "Render.getDrawCallCount" )
        if result is Int32 n
        {
            ret n
        }
        ret 0
    }

    public static void resetStats()
    {
        SystemCallExternalFunction( "Render.resetStats" )
    }

    # ── 内部辅助 ─────────────────────────────────────────
    static bool _asBool( object result )
    {
        if result is bool b
        {
            ret b
        }
        ret false
    }

    override string toString()
    {
        ret "Render(static core)"
    }
}
