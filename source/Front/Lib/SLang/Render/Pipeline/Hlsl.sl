# Hlsl —— HLSL 着色器对象模型
#
# 着色器统一用 HLSL 编写（与 Unity 一致）：
#   入口：VSMain / PSMain / CSMain（可配置）
#   目标：vs_5_0 / ps_5_0 / cs_6_0（DX12 走 DXIL，Vulkan 走 DXC -> SPIR-V）
#   语义：POSITION / NORMAL / TEXCOORD0 / SV_POSITION / SV_Target
#   cbuffer：UnityPerFrame / UnityPerDraw / UnityPerMaterial（见 HlslLibrary）
#
# 编译产物是 ShaderModule（字节码 + 反射信息），交给 GraphicsPipelineState 组装成 PSO

enum EShaderStage
{
    Vertex = 0
    Pixel
    Geometry
    Hull
    Domain
    Compute
}

enum EShaderParamType
{
    Float = 0
    Float2
    Float3
    Float4
    Matrix4x4
    Texture2D
    Sampler
    ConstantBuffer
    StructuredBuffer
}

# ── 反射出来的着色器参数 ───────────────────────────────
public class ShaderParameter
{
    public string name = ""
    public EShaderParamType paramType = EShaderParamType.Float
    public Int32 bindPoint = 0        # register(b0) / (t0) / (s0) 里的数字
    public Int32 bindSpace = 0        # space0 / space1
    public Int32 stageFlags = 1       # 可见阶段位掩码：1=VS 2=PS 4=CS

    public void _init_()
    {
        this.name = ""
        this.paramType = EShaderParamType.Float
        this.bindPoint = 0
        this.bindSpace = 0
        this.stageFlags = 1
    }

    public void _init_( string _name, EShaderParamType _type, Int32 _bindPoint )
    {
        this.name = _name
        this.paramType = _type
        this.bindPoint = _bindPoint
        this.bindSpace = 0
        this.stageFlags = 1
    }

    override string toString()
    {
        ret this.name + " : register(b" + this.bindPoint.toString() + ")"
    }
}

# ── 编译后的着色器模块（DXIL / SPIR-V 字节码）───────────
public class ShaderModule
{
    public string name = ""
    public string entryPoint = "main"
    public string target = "vs_5_0"
    public EShaderStage stage = EShaderStage.Vertex
    public Array<Int32> bytecode = null
    public Array<ShaderParameter> parameters = null
    public bool isValid = false

    public void _init_()
    {
        this.name = ""
        this.entryPoint = "main"
        this.target = "vs_5_0"
        this.stage = EShaderStage.Vertex
        this.bytecode = Array<Int32>( 0 )
        this.parameters = Array<ShaderParameter>( 0 )
        this.isValid = false
    }

    # 调用后端编译器（DXC / FXC）把 HLSL 编成字节码
    public static ShaderModule compile( string name, string source, string entry, string target, EShaderStage stage )
    {
        ShaderModule m = ShaderModule()
        m.name = name
        m.entryPoint = entry
        m.target = target
        m.stage = stage

        object result = SystemCallExternalFunction( "Render.compileHlsl", name, source, entry, target, stage )
        if result is ShaderModule ok
        {
            ret ok
        }
        if GpuUtil.asBool( result )
        {
            m.isValid = true
        }
        ret m
    }

    public int parameterCount()
    {
        ret this.parameters.length
    }

    override string toString()
    {
        ret "ShaderModule(" + this.name + ", " + this.target + ", valid=" + this.isValid.toString() + ")"
    }
}

# ── HLSL 着色器（源码 + 编译 + uniform 设置）────────────
public class HlslShader
{
    public string name = "HlslShader"

    # 源码：VS / PS / CS 三段，按需填
    public string vertexSource = ""
    public string pixelSource = ""
    public string computeSource = ""

    public string vertexEntry = "VSMain"
    public string pixelEntry = "PSMain"
    public string computeEntry = "CSMain"

    # 目标模型：DX12 用 5_1 / 6_x；Vulkan 转 SPIR-V 时后端自动处理
    public string vertexTarget = "vs_5_1"
    public string pixelTarget = "ps_5_1"
    public string computeTarget = "cs_6_0"

    public Array<ShaderParameter> parameters = null

    ShaderModule _vs = null
    ShaderModule _ps = null
    ShaderModule _cs = null

    public void _init_()
    {
        this.name = "HlslShader"
        this.vertexSource = ""
        this.pixelSource = ""
        this.computeSource = ""
        this.parameters = Array<ShaderParameter>( 0 )
    }

    public void _init_( string _name )
    {
        this._init_()
        this.name = _name
    }

    public void _init_( string _name, string _vsSource, string _psSource )
    {
        this._init_()
        this.name = _name
        this.vertexSource = _vsSource
        this.pixelSource = _psSource
    }

    # ── 编译 ─────────────────────────────────────────────
    public bool compile()
    {
        bool ok = true
        if this.vertexSource != ""
        {
            this._vs = ShaderModule.compile( this.name + ".vs", this.vertexSource, this.vertexEntry, this.vertexTarget, EShaderStage.Vertex )
            if !this._vs.isValid
            {
                ok = false
            }
        }
        if this.pixelSource != ""
        {
            this._ps = ShaderModule.compile( this.name + ".ps", this.pixelSource, this.pixelEntry, this.pixelTarget, EShaderStage.Pixel )
            if !this._ps.isValid
            {
                ok = false
            }
        }
        if this.computeSource != ""
        {
            this._cs = ShaderModule.compile( this.name + ".cs", this.computeSource, this.computeEntry, this.computeTarget, EShaderStage.Compute )
            if !this._cs.isValid
            {
                ok = false
            }
        }
        ret ok
    }

    public ShaderModule vertexModule()
    {
        ret this._vs
    }

    public ShaderModule pixelModule()
    {
        ret this._ps
    }

    public ShaderModule computeModule()
    {
        ret this._cs
    }

    public get bool isCompiled()
    {
        ret this._vs != null || this._ps != null || this._cs != null
    }

    # ── 参数反射 ─────────────────────────────────────────
    public void addParameter( string pname, EShaderParamType ptype, Int32 bindPoint )
    {
        Array<ShaderParameter> np = Array<ShaderParameter>( this.parameters.length + 1 )
        int i = 0
        while i < this.parameters.length
        {
            np[i] = this.parameters[i]
            i++
        }
        np[ this.parameters.length ] = ShaderParameter( pname, ptype, bindPoint )
        this.parameters = np
    }

    # ── uniform 设置（走后端，按名字绑定）─────────────────
    public bool setFloat( string pname, Float32 value )
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.setUniformFloat", this.name, pname, value ) )
    }

    public bool setVector3( string pname, Float32_3 value )
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.setUniformVector3", this.name, pname, value.x, value.y, value.z ) )
    }

    public bool setVector4( string pname, Float32_4 value )
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.setUniformVector4", this.name, pname, value.x, value.y, value.z, value.w ) )
    }

    public bool setColor( string pname, Color value )
    {
        ret this.setVector4( pname, value.toFloat32_4() )
    }

    public bool setMatrix( string pname, Float32_4x4 value )
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.setUniformMatrix4", this.name, pname, value ) )
    }

    public bool setTexture( string pname, GpuTextureView view )
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.setUniformTextureView", this.name, pname, view.descriptorIndex ) )
    }

    public bool setSampler( string pname, Sampler s )
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.setUniformSampler", this.name, pname, s.descriptorIndex ) )
    }

    override string toString()
    {
        ret "HlslShader(" + this.name + ", compiled=" + this.isCompiled().toString() + ")"
    }
}
