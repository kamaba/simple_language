# 着色器：源码 + 编译状态 + uniform 设置。
# 底层操作通过 SystemCallExternalFunction("Render.xxx", ...) 转交渲染后端实现。
public class Shader
{
    public string name = ""
    public string vertexSource = ""
    public string fragmentSource = ""
    public bool isCompiled = false

    # ── 构造 ─────────────────────────────────────────────
    public void _init_()
    {
        this.name = "Shader"
        this.vertexSource = ""
        this.fragmentSource = ""
        this.isCompiled = false
    }

    public void _init_( string _name )
    {
        this.name = _name
        this.vertexSource = ""
        this.fragmentSource = ""
        this.isCompiled = false
    }

    public void _init_( string _name, string _vertexSource, string _fragmentSource )
    {
        this.name = _name
        this.vertexSource = _vertexSource
        this.fragmentSource = _fragmentSource
        this.isCompiled = false
    }

    # ── 编译 ─────────────────────────────────────────────
    public bool compile()
    {
        if this.vertexSource == "" || this.fragmentSource == ""
        {
            this.isCompiled = false
            ret false
        }
        object result = SystemCallExternalFunction( "Render.compileShader",
            this.name, this.vertexSource, this.fragmentSource )
        if result is bool ok
        {
            this.isCompiled = ok
            ret ok
        }
        this.isCompiled = false
        ret false
    }

    public void release()
    {
        if this.isCompiled
        {
            SystemCallExternalFunction( "Render.releaseShader", this.name )
            this.isCompiled = false
        }
    }

    # ── uniform 设置 ─────────────────────────────────────
    public bool setUniformFloat( string uniformName, Float32 value )
    {
        ret Shader._setUniform( uniformName, value )
    }

    # 4 分量向量（Float32_4）
    public bool setUniformVector( string uniformName, Float32_4 value )
    {
        object result = SystemCallExternalFunction( "Render.setUniformVector4",
            this.name, uniformName, value.x, value.y, value.z, value.w )
        ret Shader._asBool( result )
    }

    public bool setUniformColor( string uniformName, Color value )
    {
        ret this.setUniformVector( uniformName, value.toFloat32_4() )
    }

    # 3 分量向量（Float32_3）
    public bool setUniformVector3( string uniformName, Float32_3 value )
    {
        object result = SystemCallExternalFunction( "Render.setUniformVector3",
            this.name, uniformName, value.x, value.y, value.z )
        ret Shader._asBool( result )
    }

    # 4x4 矩阵（Float32_4x4）
    public bool setUniformMatrix( string uniformName, Float32_4x4 value )
    {
        object result = SystemCallExternalFunction( "Render.setUniformMatrix4",
            this.name, uniformName, value )
        ret Shader._asBool( result )
    }

    public bool setUniformTexture( string uniformName, Texture tex )
    {
        object result = SystemCallExternalFunction( "Render.setUniformTexture",
            this.name, uniformName, tex )
        ret Shader._asBool( result )
    }

    # ── 内部辅助 ─────────────────────────────────────────
    static bool _setUniform( string uniformName, Float32 value )
    {
        object result = SystemCallExternalFunction( "Render.setUniformFloat", uniformName, value )
        ret Shader._asBool( result )
    }

    static bool _asBool( object result )
    {
        if result is bool b
        {
            ret b
        }
        ret false
    }

    # ── 查找 ─────────────────────────────────────────────
    # 由后端按名字查找已注册着色器
    public static Shader find( string shaderName )
    {
        object result = SystemCallExternalFunction( "Render.findShader", shaderName )
        if result is Shader s
        {
            ret s
        }
        ret Shader( shaderName )
    }

    override string toString()
    {
        ret String.toFormat( "Shader(name={0}, compiled={1})",
            this.name, this.isCompiled.toString() )
    }
}
