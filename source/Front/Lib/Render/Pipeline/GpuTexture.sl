# GpuTexture —— GPU 纹理 / 渲染目标 / 视图 / 采样器
#
# 对齐 DX12 / Vulkan：
#   Texture  资源本体（可当 SRV / RTV / DSV / UAV 使用）
#   View     描述符（ShaderResourceView / RenderTargetView / DepthStencilView / UnorderedAccessView）
#   Sampler  采样状态（独立描述符，Vulkan 里是 immutable sampler 或独立 descriptor）
#   用法标记决定资源创建时挂哪些 usage flag

enum ETextureFormat
{
    Unknown = 0
    R8
    RG8
    RGBA8
    RGBA8_SRGB
    R16F
    RG16F
    RGBA16F
    R32F
    RGBA32F
    Depth16
    Depth24Stencil8
    Depth32F
    BC1
    BC3
    BC5
    BC7
}

enum ETextureDimension
{
    Tex2D = 0
    Tex2DArray
    TexCube
    Tex3D
}

enum ETextureUsage
{
    ShaderResource = 1
    RenderTarget = 2
    DepthStencil = 4
    UnorderedAccess = 8
}

enum ETextureViewType
{
    SRV = 0
    RTV
    DSV
    UAV
}

enum EFilterMode
{
    Point = 0
    Linear
    Anisotropic
}

enum EAddressMode
{
    Repeat = 0
    Clamp
    Mirror
    Border
}

# ── 采样器描述 ─────────────────────────────────────────
public class SamplerDesc
{
    public EFilterMode filter = EFilterMode.Linear
    public EAddressMode addressU = EAddressMode.Clamp
    public EAddressMode addressV = EAddressMode.Clamp
    public EAddressMode addressW = EAddressMode.Clamp
    public Int32 anisotropy = 1
    public Int32 comparison = 0        # 0 = 不比较；1 = LessEqual（阴影采样用）
    public Color borderColor = Color.black()

    public void _init_()
    {
        this.filter = EFilterMode.Linear
        this.addressU = EAddressMode.Clamp
        this.addressV = EAddressMode.Clamp
        this.addressW = EAddressMode.Clamp
        this.anisotropy = 1
        this.comparison = 0
        this.borderColor = Color.black()
    }

    public static SamplerDesc linearClamp()
    {
        SamplerDesc s = SamplerDesc()
        s.filter = EFilterMode.Linear
        s.addressU = EAddressMode.Clamp
        s.addressV = EAddressMode.Clamp
        ret s
    }

    public static SamplerDesc pointClamp()
    {
        SamplerDesc s = SamplerDesc()
        s.filter = EFilterMode.Point
        ret s
    }

    public static SamplerDesc anisotropicRepeat()
    {
        SamplerDesc s = SamplerDesc()
        s.filter = EFilterMode.Anisotropic
        s.anisotropy = 8
        s.addressU = EAddressMode.Repeat
        s.addressV = EAddressMode.Repeat
        ret s
    }

    # 阴影专用：比较采样 + Clamp
    public static SamplerDesc shadowCompare()
    {
        SamplerDesc s = SamplerDesc()
        s.filter = EFilterMode.Linear
        s.comparison = 1
        ret s
    }
}

# ── 视图（描述符）───────────────────────────────────────
public class GpuTextureView
{
    public GpuTexture owner = null
    public ETextureViewType viewType = ETextureViewType.SRV
    public ETextureFormat format = ETextureFormat.RGBA8
    public Int32 mipLevel = 0
    public Int32 mipCount = 1
    public Int32 arraySlice = 0

    # 描述符堆中的索引（CBV_SRV_UAV / RTV / DSV 各自独立编号）
    public Int32 descriptorIndex = -1

    public void _init_()
    {
        this.owner = null
        this.viewType = ETextureViewType.SRV
        this.descriptorIndex = -1
    }

    public void _init_( GpuTexture _owner, ETextureViewType _viewType )
    {
        this.owner = _owner
        this.viewType = _viewType
        this.format = _owner.format
        this.mipLevel = 0
        this.mipCount = 1
        this.arraySlice = 0
        this.descriptorIndex = -1
    }

    public bool create()
    {
        if this.owner == null
        {
            ret false
        }
        object result = SystemCallExternalFunction( "Render.createTextureView",
            this.owner.handle, this.viewType, this.format, this.mipLevel, this.mipCount, this.arraySlice )
        if result is Int32 idx
        {
            this.descriptorIndex = idx
            ret true
        }
        ret false
    }

    override string toString()
    {
        ret "TextureView(type=" + this.viewType.toString() + ", idx=" + this.descriptorIndex.toString() + ")"
    }
}

# ── 纹理本体 ───────────────────────────────────────────
public class GpuTexture extends GpuResource
{
    public Int32 width = 0
    public Int32 height = 0
    public Int32 depth = 1
    public Int32 mipLevels = 1
    public Int32 arraySize = 1
    public ETextureFormat format = ETextureFormat.RGBA8
    public ETextureDimension dimension = ETextureDimension.Tex2D
    public Int32 usageFlags = ETextureUsage.ShaderResource
    public Int32 sampleCount = 1

    GpuTextureView _srv = null
    GpuTextureView _rtv = null
    GpuTextureView _dsv = null
    GpuTextureView _uav = null

    public void _init_()
    {
        this.name = "texture"
        this.width = 1
        this.height = 1
    }

    public void _init_( string _name, Int32 _width, Int32 _height, ETextureFormat _format, Int32 _usageFlags )
    {
        this.name = _name
        this.width = _width
        this.height = _height
        this.format = _format
        this.usageFlags = _usageFlags
        this.sizeBytes = SystemConvertInt64( _width * _height * GpuTexture.formatBytesPerPixel( _format ) )
    }

    public static Int32 formatBytesPerPixel( ETextureFormat f )
    {
        if f == ETextureFormat.RGBA32F
        {
            ret 16
        }
        if f == ETextureFormat.RGBA16F
        {
            ret 8
        }
        if f == ETextureFormat.RG16F
        {
            ret 4
        }
        if f == ETextureFormat.R8
        {
            ret 1
        }
        if f == ETextureFormat.Depth32F
        {
            ret 4
        }
        if f == ETextureFormat.Depth24Stencil8
        {
            ret 4
        }
        ret 4
    }

    override bool create()
    {
        object result = SystemCallExternalFunction( "Render.createTexture",
            this.name, this.width, this.height, this.depth, this.mipLevels,
            this.arraySize, this.format, this.dimension, this.usageFlags, this.sampleCount )
        if result is Int32 h
        {
            this.handle = h
            this.isCreated = true
            ret true
        }
        this.isCreated = false
        ret false
    }

    # ── 视图获取（懒创建）────────────────────────────────
    public GpuTextureView srv()
    {
        if this._srv == null
        {
            this._srv = GpuTextureView( this, ETextureViewType.SRV )
            this._srv.create()
        }
        ret this._srv
    }

    public GpuTextureView rtv()
    {
        if this._rtv == null
        {
            this._rtv = GpuTextureView( this, ETextureViewType.RTV )
            this._rtv.create()
        }
        ret this._rtv
    }

    public GpuTextureView dsv()
    {
        if this._dsv == null
        {
            this._dsv = GpuTextureView( this, ETextureViewType.DSV )
            this._dsv.create()
        }
        ret this._dsv
    }

    public GpuTextureView uav()
    {
        if this._uav == null
        {
            this._uav = GpuTextureView( this, ETextureViewType.UAV )
            this._uav.create()
        }
        ret this._uav
    }

    # ── 数据 ─────────────────────────────────────────────
    public bool uploadPixels( Texture src )
    {
        if src == null || !this.isCreated
        {
            ret false
        }
        this.transitionTo( EResourceState.CopyDest )
        object result = SystemCallExternalFunction( "Render.uploadTexture", this.handle, src )
        this.transitionTo( EResourceState.ShaderResource )
        ret GpuUtil.asBool( result )
    }

    public void generateMipmaps()
    {
        SystemCallExternalFunction( "Render.generateMipmaps", this.handle )
    }

    public get bool isRenderTarget()
    {
        ret ( this.usageFlags & ETextureUsage.RenderTarget ) != 0
    }

    public get bool isDepthStencil()
    {
        ret ( this.usageFlags & ETextureUsage.DepthStencil ) != 0
    }

    override string toString()
    {
        ret "GpuTexture(" + this.name + ", " + this.width.toString() + "x" + this.height.toString() + ")"
    }
}

# ── 采样器（运行时对象）────────────────────────────────
public class Sampler
{
    public string name = "sampler"
    public SamplerDesc desc = null
    public Int32 descriptorIndex = -1

    public void _init_()
    {
        this.name = "sampler"
        this.desc = SamplerDesc.linearClamp()
        this.descriptorIndex = -1
    }

    public void _init_( SamplerDesc _desc )
    {
        this.name = "sampler"
        this.desc = _desc
        this.descriptorIndex = -1
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createSampler",
            this.desc.filter, this.desc.addressU, this.desc.addressV, this.desc.anisotropy, this.desc.comparison )
        if result is Int32 idx
        {
            this.descriptorIndex = idx
            ret true
        }
        ret false
    }
}
