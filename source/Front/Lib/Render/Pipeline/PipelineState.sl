# PipelineState —— 图形/计算管线状态对象（PSO）
#
# DX12 / Vulkan 都是「先把状态烘成一个不可变对象，再绑定」：
#   BlendState + RasterizerState + DepthStencilState + ShaderModule + InputLayout
#   + RenderTarget 格式 + MSAA  ->  GraphicsPipelineState (PSO)
# 运行时切换 PSO 就是一次 SetPipelineState，不再逐项设状态（减少驱动开销）

enum EBlendFactor
{
    Zero = 0
    One
    SrcColor
    OneMinusSrcColor
    SrcAlpha
    OneMinusSrcAlpha
    DstColor
    OneMinusDstColor
    DstAlpha
    OneMinusDstAlpha
}

enum EBlendOp
{
    Add = 0
    Subtract
    ReverseSubtract
    Min
    Max
}

enum ECullMode
{
    Off = 0
    Front
    Back
}

enum EFillMode
{
    Solid = 0
    Wireframe
}

enum ECompareFunction
{
    Never = 0
    Less
    Equal
    LessEqual
    Greater
    NotEqual
    GreaterEqual
    Always
}

enum EStencilOp
{
    Keep = 0
    Zero
    Replace
    IncrementClamp
    DecrementClamp
    Invert
}

enum EPrimitiveTopology
{
    PointList = 0
    LineList
    LineStrip
    TriangleList
    TriangleStrip
}

enum EVertexAttribute
{
    Position = 0
    Normal
    Tangent
    Color
    TexCoord0
    TexCoord1
    TexCoord2
    TexCoord3
}

enum EVertexAttributeFormat
{
    Float32x2 = 0
    Float32x3
    Float32x4
    Uint8x4Norm
    Uint16x2Norm
    Uint32x1
}

# ── 混合状态 ───────────────────────────────────────────
public class BlendState
{
    public bool enabled = false
    public EBlendFactor srcColor = EBlendFactor.One
    public EBlendFactor dstColor = EBlendFactor.Zero
    public EBlendFactor srcAlpha = EBlendFactor.One
    public EBlendFactor dstAlpha = EBlendFactor.Zero
    public EBlendOp colorOp = EBlendOp.Add
    public EBlendOp alphaOp = EBlendOp.Add

    # 逐 RT 混合时用的写入掩码
    public Int32 writeMask = 15

    public void _init_()
    {
        this.enabled = false
        this.srcColor = EBlendFactor.One
        this.dstColor = EBlendFactor.Zero
        this.srcAlpha = EBlendFactor.One
        this.dstAlpha = EBlendFactor.Zero
        this.colorOp = EBlendOp.Add
        this.alphaOp = EBlendOp.Add
        this.writeMask = 15
    }

    public static BlendState opaque()
    {
        BlendState b = BlendState()
        b.enabled = false
        ret b
    }

    public static BlendState alphaBlend()
    {
        BlendState b = BlendState()
        b.enabled = true
        b.srcColor = EBlendFactor.SrcAlpha
        b.dstColor = EBlendFactor.OneMinusSrcAlpha
        b.srcAlpha = EBlendFactor.One
        b.dstAlpha = EBlendFactor.OneMinusSrcAlpha
        ret b
    }

    public static BlendState additive()
    {
        BlendState b = BlendState()
        b.enabled = true
        b.srcColor = EBlendFactor.One
        b.dstColor = EBlendFactor.One
        ret b
    }

    public static BlendState premultiplied()
    {
        BlendState b = BlendState()
        b.enabled = true
        b.srcColor = EBlendFactor.One
        b.dstColor = EBlendFactor.OneMinusSrcAlpha
        ret b
    }
}

# ── 光栅化状态 ─────────────────────────────────────────
public class RasterizerState
{
    public ECullMode cullMode = ECullMode.Back
    public EFillMode fillMode = EFillMode.Solid
    public bool frontCounterClockwise = false
    public bool depthClipEnable = true
    public bool scissorEnable = false
    public Int32 depthBias = 0
    public Float32 depthBiasClamp = 0.0f
    public Float32 slopeScaledDepthBias = 0.0f

    public void _init_()
    {
        this.cullMode = ECullMode.Back
        this.fillMode = EFillMode.Solid
        this.frontCounterClockwise = false
        this.depthClipEnable = true
        this.scissorEnable = false
        this.depthBias = 0
        this.depthBiasClamp = 0.0f
        this.slopeScaledDepthBias = 0.0f
    }

    public static RasterizerState defaultState()
    {
        ret RasterizerState()
    }

    public static RasterizerState shadowCaster()
    {
        RasterizerState r = RasterizerState()
        r.cullMode = ECullMode.Front
        r.depthBias = 1000
        r.slopeScaledDepthBias = 2.0f
        ret r
    }

    public static RasterizerState noCull()
    {
        RasterizerState r = RasterizerState()
        r.cullMode = ECullMode.Off
        ret r
    }
}

# ── 深度模板状态 ───────────────────────────────────────
public class DepthStencilState
{
    public bool depthEnable = true
    public bool depthWrite = true
    public ECompareFunction depthFunc = ECompareFunction.LessEqual

    public bool stencilEnable = false
    public Int32 stencilReadMask = 255
    public Int32 stencilWriteMask = 255
    public EStencilOp stencilFailOp = EStencilOp.Keep
    public EStencilOp stencilDepthFailOp = EStencilOp.Keep
    public EStencilOp stencilPassOp = EStencilOp.Keep
    public ECompareFunction stencilFunc = ECompareFunction.Always

    public void _init_()
    {
        this.depthEnable = true
        this.depthWrite = true
        this.depthFunc = ECompareFunction.LessEqual
        this.stencilEnable = false
        this.stencilReadMask = 255
        this.stencilWriteMask = 255
        this.stencilFailOp = EStencilOp.Keep
        this.stencilDepthFailOp = EStencilOp.Keep
        this.stencilPassOp = EStencilOp.Keep
        this.stencilFunc = ECompareFunction.Always
    }

    # 默认：写深度 + LessEqual
    public static DepthStencilState defaultState()
    {
        ret DepthStencilState()
    }

    # 深度预通道 / 不透明：写深度
    public static DepthStencilState depthWrite()
    {
        DepthStencilState d = DepthStencilState()
        d.depthEnable = true
        d.depthWrite = true
        d.depthFunc = ECompareFunction.LessEqual
        ret d
    }

    # 透明 / 后处理：只测不写
    public static DepthStencilState depthRead()
    {
        DepthStencilState d = DepthStencilState()
        d.depthEnable = true
        d.depthWrite = false
        d.depthFunc = ECompareFunction.LessEqual
        ret d
    }

    # 全屏通道：关闭深度
    public static DepthStencilState none()
    {
        DepthStencilState d = DepthStencilState()
        d.depthEnable = false
        d.depthWrite = false
        ret d
    }
}

# ── 顶点输入布局 ───────────────────────────────────────
public class VertexInputLayout
{
    Array<Int32> _attributes = null
    Array<Int32> _formats = null
    Array<Int32> _offsets = null
    public Int32 stride = 0

    public void _init_()
    {
        this._attributes = Array<Int32>( 0 )
        this._formats = Array<Int32>( 0 )
        this._offsets = Array<Int32>( 0 )
        this.stride = 0
    }

    public void add( EVertexAttribute attribute, EVertexAttributeFormat format, Int32 offset )
    {
        Array<Int32> na = Array<Int32>( this._attributes.length + 1 )
        Array<Int32> nf = Array<Int32>( this._formats.length + 1 )
        Array<Int32> no = Array<Int32>( this._offsets.length + 1 )
        int i = 0
        while i < this._attributes.length
        {
            na[i] = this._attributes[i]
            nf[i] = this._formats[i]
            no[i] = this._offsets[i]
            i++
        }
        na[ this._attributes.length ] = attribute
        nf[ this._formats.length ] = format
        no[ this._offsets.length ] = offset
        this._attributes = na
        this._formats = nf
        this._offsets = no
    }

    public get int elementCount()
    {
        ret this._attributes.length
    }

    # 标准布局：POSITION(0) NORMAL(12) TANGENT(24) TEXCOORD0(40)，stride 48
    public static VertexInputLayout standard()
    {
        VertexInputLayout l = VertexInputLayout()
        l.add( EVertexAttribute.Position, EVertexAttributeFormat.Float32x3, 0 )
        l.add( EVertexAttribute.Normal, EVertexAttributeFormat.Float32x3, 12 )
        l.add( EVertexAttribute.Tangent, EVertexAttributeFormat.Float32x4, 24 )
        l.add( EVertexAttribute.TexCoord0, EVertexAttributeFormat.Float32x2, 40 )
        l.stride = 48
        ret l
    }

    # 全屏通道：无顶点输入
    public static VertexInputLayout empty()
    {
        VertexInputLayout l = VertexInputLayout()
        l.stride = 0
        ret l
    }
}

# ── 图形 PSO ───────────────────────────────────────────
public class GraphicsPipelineState
{
    public string name = "pso"
    public ShaderModule vertexShader = null
    public ShaderModule pixelShader = null

    public VertexInputLayout inputLayout = null
    public BlendState blend = null
    public RasterizerState raster = null
    public DepthStencilState depthStencil = null

    public Array<Int32> renderTargetFormats = null
    public Int32 depthFormat = ETextureFormat.Depth24Stencil8
    public Int32 sampleCount = 1
    public EPrimitiveTopology topology = EPrimitiveTopology.TriangleList

    public bool isCreated = false
    public Int32 handle = 0

    public void _init_()
    {
        this.name = "pso"
        this.inputLayout = VertexInputLayout.standard()
        this.blend = BlendState.opaque()
        this.raster = RasterizerState.defaultState()
        this.depthStencil = DepthStencilState.defaultState()
        this.renderTargetFormats = Array<Int32>( 0 )
        this.sampleCount = 1
        this.isCreated = false
        this.handle = 0
    }

    public void _init_( string _name, ShaderModule _vs, ShaderModule _ps )
    {
        this._init_()
        this.name = _name
        this.vertexShader = _vs
        this.pixelShader = _ps
    }

    public void setRenderTargetFormats( Array<Int32> formats )
    {
        this.renderTargetFormats = formats
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createGraphicsPipeline",
            this.name, this.vertexShader, this.pixelShader,
            this.inputLayout, this.blend, this.raster, this.depthStencil,
            this.renderTargetFormats, this.depthFormat, this.sampleCount, this.topology )
        if result is Int32 h
        {
            this.handle = h
            this.isCreated = true
            ret true
        }
        this.isCreated = false
        ret false
    }

    public void destroy()
    {
        if this.isCreated
        {
            SystemCallExternalFunction( "Render.destroyPipeline", this.handle )
            this.isCreated = false
            this.handle = 0
        }
    }

    override string toString()
    {
        ret "PSO(" + this.name + ")"
    }
}

# ── 计算 PSO ───────────────────────────────────────────
public class ComputePipelineState
{
    public string name = "computePso"
    public ShaderModule computeShader = null
    public bool isCreated = false
    public Int32 handle = 0

    public void _init_()
    {
        this.name = "computePso"
        this.isCreated = false
        this.handle = 0
    }

    public void _init_( string _name, ShaderModule _cs )
    {
        this._init_()
        this.name = _name
        this.computeShader = _cs
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createComputePipeline", this.name, this.computeShader )
        if result is Int32 h
        {
            this.handle = h
            this.isCreated = true
            ret true
        }
        this.isCreated = false
        ret false
    }

    public void destroy()
    {
        if this.isCreated
        {
            SystemCallExternalFunction( "Render.destroyPipeline", this.handle )
            this.isCreated = false
            this.handle = 0
        }
    }
}
