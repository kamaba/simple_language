# RenderTarget —— 渲染目标描述、标识与绑定
#
# Unity：RenderTextureDescriptor / RenderTargetIdentifier / RenderTargetBinding
# DX12 ：RTV / DSV 描述符
# Vulkan：Framebuffer + Attachment
#
# 另外提供 RenderTargetPool：按描述复用 RT，避免每帧创建销毁

public class RenderTextureDescriptor
{
    public Int32 width = 1280
    public Int32 height = 720
    public ETextureFormat colorFormat = ETextureFormat.RGBA8
    public ETextureFormat depthFormat = ETextureFormat.Depth24Stencil8
    public Int32 msaaSamples = 1
    public bool useDepthBuffer = true
    public bool enableRandomWrite = false
    public bool autoGenerateMips = false
    public string name = "renderTexture"

    public void _init_()
    {
        this.width = 1280
        this.height = 720
        this.colorFormat = ETextureFormat.RGBA8
        this.depthFormat = ETextureFormat.Depth24Stencil8
        this.msaaSamples = 1
        this.useDepthBuffer = true
        this.enableRandomWrite = false
        this.autoGenerateMips = false
        this.name = "renderTexture"
    }

    # 屏幕尺寸的颜色 + 深度 RT
    public static RenderTextureDescriptor screen( Int32 w, Int32 h )
    {
        RenderTextureDescriptor d = RenderTextureDescriptor()
        d.width = w
        d.height = h
        d.name = "CameraTarget"
        ret d
    }

    # HDR 目标（后处理链用）
    public static RenderTextureDescriptor hdr( Int32 w, Int32 h )
    {
        RenderTextureDescriptor d = RenderTextureDescriptor()
        d.width = w
        d.height = h
        d.colorFormat = ETextureFormat.RGBA16F
        d.name = "CameraTargetHDR"
        ret d
    }

    # 阴影贴图（只关心深度）
    public static RenderTextureDescriptor shadowMap( Int32 size )
    {
        RenderTextureDescriptor d = RenderTextureDescriptor()
        d.width = size
        d.height = size
        d.colorFormat = ETextureFormat.Unknown
        d.depthFormat = ETextureFormat.Depth32F
        d.useDepthBuffer = true
        d.msaaSamples = 1
        d.name = "ShadowMap"
        ret d
    }

    override string toString()
    {
        ret "RTDesc(" + this.name + ", " + this.width.toString() + "x" + this.height.toString() + ", msaa=" + this.msaaSamples.toString() + ")"
    }
}

# ── 渲染目标标识 ───────────────────────────────────────
public class RenderTargetIdentifier
{
    public GpuTexture texture = null
    public Int32 mipLevel = 0
    public Int32 arraySlice = 0
    public bool isBackBuffer = false

    public void _init_()
    {
        this.texture = null
        this.mipLevel = 0
        this.arraySlice = 0
        this.isBackBuffer = false
    }

    public static RenderTargetIdentifier backBuffer()
    {
        RenderTargetIdentifier id = RenderTargetIdentifier()
        id.isBackBuffer = true
        ret id
    }

    public static RenderTargetIdentifier fromTexture( GpuTexture tex )
    {
        RenderTargetIdentifier id = RenderTargetIdentifier()
        id.texture = tex
        ret id
    }

    public get bool isValid()
    {
        ret this.isBackBuffer || this.texture != null
    }

    override string toString()
    {
        if this.isBackBuffer
        {
            ret "RTI(BackBuffer)"
        }
        if this.texture == null
        {
            ret "RTI(Invalid)"
        }
        ret "RTI(" + this.texture.name + ")"
    }
}

# ── 一次绑定的 MRT + 深度 ─────────────────────────────
public class RenderTargetBinding
{
    Array<RenderTargetIdentifier> _colors = null
    public RenderTargetIdentifier depthTarget = null
    public Int32 msaaSamples = 1

    public void _init_()
    {
        this._colors = Array<RenderTargetIdentifier>( 0 )
        this.depthTarget = null
        this.msaaSamples = 1
    }

    public void _init_( RenderTargetIdentifier color, RenderTargetIdentifier depth )
    {
        this._init_()
        if color != null
        {
            this.addColor( color )
        }
        this.depthTarget = depth
    }

    public void addColor( RenderTargetIdentifier color )
    {
        Array<RenderTargetIdentifier> nc = Array<RenderTargetIdentifier>( this._colors.length + 1 )
        int i = 0
        while i < this._colors.length
        {
            nc[i] = this._colors[i]
            i++
        }
        nc[ this._colors.length ] = color
        this._colors = nc
    }

    public RenderTargetIdentifier colorAt( Int32 index )
    {
        ret this._colors[ index ]
    }

    public get int colorCount()
    {
        ret this._colors.length
    }

    public get bool hasDepth()
    {
        ret this.depthTarget != null
    }
}

# ── RT 池 ─────────────────────────────────────────────
public class RenderTargetPool
{
    Array<GpuTexture> _freeTextures = null
    Array<GpuTexture> _usedTextures = null

    public void _init_()
    {
        this._freeTextures = Array<GpuTexture>( 0 )
        this._usedTextures = Array<GpuTexture>( 0 )
    }

    # 命中缓存直接复用，否则新建
    public RenderTargetIdentifier get( RenderTextureDescriptor desc )
    {
        int i = 0
        while i < this._freeTextures.length
        {
            GpuTexture t = this._freeTextures[i]
            if t.width == desc.width && t.height == desc.height && t.format == desc.colorFormat
            {
                this._freeTextures = RenderTargetPool.removeAt( this._freeTextures, i )
                this._usedTextures = RenderTargetPool.append( this._usedTextures, t )
                ret RenderTargetIdentifier.fromTexture( t )
            }
            i++
        }

        GpuTexture nt = GpuTexture( desc.name, desc.width, desc.height, desc.colorFormat, ETextureUsage.RenderTarget | ETextureUsage.ShaderResource )
        nt.create()
        this._usedTextures = RenderTargetPool.append( this._usedTextures, nt )
        ret RenderTargetIdentifier.fromTexture( nt )
    }

    public GpuTexture getDepth( RenderTextureDescriptor desc )
    {
        GpuTexture dt = GpuTexture( desc.name + "_Depth", desc.width, desc.height, desc.depthFormat, ETextureUsage.DepthStencil )
        dt.create()
        ret dt
    }

    public void release( RenderTargetIdentifier rt )
    {
        if rt == null || rt.texture == null
        {
            ret
        }
        int i = 0
        while i < this._usedTextures.length
        {
            if this._usedTextures[i].handle == rt.texture.handle
            {
                this._usedTextures = RenderTargetPool.removeAt( this._usedTextures, i )
                this._freeTextures = RenderTargetPool.append( this._freeTextures, rt.texture )
                ret
            }
            i++
        }
    }

    # 每帧末把用到的都还给池子
    public void releaseAll()
    {
        int i = 0
        while i < this._usedTextures.length
        {
            this._freeTextures = RenderTargetPool.append( this._freeTextures, this._usedTextures[i] )
            i++
        }
        this._usedTextures = Array<GpuTexture>( 0 )
    }

    static Array<GpuTexture> append( Array<GpuTexture> src, GpuTexture item )
    {
        Array<GpuTexture> dst = Array<GpuTexture>( src.length + 1 )
        int i = 0
        while i < src.length
        {
            dst[i] = src[i]
            i++
        }
        dst[ src.length ] = item
        ret dst
    }

    static Array<GpuTexture> removeAt( Array<GpuTexture> src, Int32 index )
    {
        Array<GpuTexture> dst = Array<GpuTexture>( src.length - 1 )
        int k = 0
        int i = 0
        while i < src.length
        {
            if i != index
            {
                dst[k] = src[i]
                k++
            }
            i++
        }
        ret dst
    }

    public get int freeCount()
    {
        ret this._freeTextures.length
    }
}
