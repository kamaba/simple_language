# GpuResource —— GPU 资源基类与缓冲
#
# 对齐 DX12 / Vulkan 的概念：
#   Resource      显存中的一块资源（Buffer / Texture）
#   ResourceState 资源当前状态，跨状态访问必须插入屏障（Barrier）
#   HeapType      显存类型：默认显存 / 上传堆 / 回读堆
#
# 底层统一通过 SystemCallExternalFunction("Render.xxx", ...) 交给后端实现

# 资源状态（对应 D3D12_RESOURCE_STATES / VkImageLayout）
enum EResourceState
{
    Common = 0
    VertexBuffer
    IndexBuffer
    ConstantBuffer
    RenderTarget
    DepthWrite
    DepthRead
    ShaderResource
    UnorderedAccess
    CopySource
    CopyDest
    Present
}

# 堆类型（对应 D3D12_HEAP_TYPE / VkMemoryProperty）
enum EHeapType
{
    Default = 0
    Upload
    Readback
}

# ── 资源基类 ───────────────────────────────────────────
public class GpuResource
{
    public string name = ""
    public Int64 sizeBytes = 0
    public EHeapType heapType = EHeapType.Default
    public EResourceState state = EResourceState.Common
    public bool isCreated = false

    # 后端句柄（由后端分配，SL 侧只透传）
    public Int32 handle = 0

    public void _init_()
    {
        this.name = "resource"
        this.sizeBytes = 0
        this.heapType = EHeapType.Default
        this.state = EResourceState.Common
        this.isCreated = false
        this.handle = 0
    }

    public void _init_( string _name )
    {
        this._init_()
        this.name = _name
    }

    public virtual bool create()
    {
        object result = SystemCallExternalFunction( "Render.createResource",
            this.name, this.sizeBytes, this.heapType, this.state )
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
            SystemCallExternalFunction( "Render.destroyResource", this.handle )
            this.isCreated = false
            this.handle = 0
        }
    }

    # 状态转换：真实实现里要往命令缓冲插 ResourceBarrier，这里只记录状态
    public void transitionTo( EResourceState newState )
    {
        if this.state == newState
        {
            ret
        }
        SystemCallExternalFunction( "Render.resourceBarrier", this.handle, this.state, newState )
        this.state = newState
    }

    override string toString()
    {
        ret "GpuResource(" + this.name + ", " + this.sizeBytes.toString() + " bytes)"
    }
}

# ── 缓冲 ───────────────────────────────────────────────
public class GpuBuffer extends GpuResource
{
    public Int32 stride = 0
    public Int32 elementCount = 0

    public void _init_()
    {
        this.name = "buffer"
        this.stride = 0
        this.elementCount = 0
    }

    public void _init_( Int32 _stride, Int32 _elementCount )
    {
        this._init_()
        this.stride = _stride
        this.elementCount = _elementCount
        this.sizeBytes = SystemConvertInt64( _stride * _elementCount )
    }

    public get int byteSize()
    {
        ret this.stride * this.elementCount
    }

    # 从 CPU 数据上传（数据先落到 Upload 堆，再由后端做拷贝）
    public bool upload( Array<Float32> data )
    {
        if !this.isCreated
        {
            ret false
        }
        this.transitionTo( EResourceState.CopyDest )
        object result = SystemCallExternalFunction( "Render.uploadBuffer", this.handle, data )
        this.transitionTo( EResourceState.ShaderResource )
        ret GpuUtil.asBool( result )
    }
}

# 顶点缓冲：记录顶点布局（stride 与属性偏移）
public class VertexBuffer extends GpuBuffer
{
    public Int32 vertexLayoutId = 0

    public void _init_( Int32 _stride, Int32 _elementCount )
    {
        this.name = "vertexBuffer"
        this.stride = _stride
        this.elementCount = _elementCount
        this.sizeBytes = SystemConvertInt64( _stride * _elementCount )
        this.state = EResourceState.VertexBuffer
        this.vertexLayoutId = 0
    }

    public static VertexBuffer fromMesh( Mesh mesh )
    {
        # 位置(3) + 法线(3) + UV(2) = 8 float
        VertexBuffer vb = VertexBuffer( 32, mesh.vertexCount() )
        vb.create()
        ret vb
    }
}

# 索引缓冲
public class IndexBuffer extends GpuBuffer
{
    public bool use32Bit = false

    public void _init_( Int32 _elementCount, bool _use32Bit )
    {
        this.name = "indexBuffer"
        this.use32Bit = _use32Bit
        if _use32Bit
        {
            this.stride = 4
        }
        else
        {
            this.stride = 2
        }
        this.elementCount = _elementCount
        this.sizeBytes = SystemConvertInt64( this.stride * _elementCount )
        this.state = EResourceState.IndexBuffer
    }

    public static IndexBuffer fromMesh( Mesh mesh )
    {
        Array<Int32> tris = mesh.getTriangles()
        IndexBuffer ib = IndexBuffer( tris.length, true )
        ib.create()
        ret ib
    }

    public get int indexCount()
    {
        ret this.elementCount
    }
}

# 常量缓冲（CBV）：CPU 每帧写入，Shader 只读
public class ConstantBuffer extends GpuBuffer
{
    Array<Float32> _staging = null

    public void _init_( Int32 _sizeInBytes )
    {
        this.name = "constantBuffer"
        this.stride = 1
        this.elementCount = _sizeInBytes
        this.sizeBytes = SystemConvertInt64( _sizeInBytes )
        this.state = EResourceState.ConstantBuffer
        this.heapType = EHeapType.Upload
        this._staging = Array<Float32>( _sizeInBytes / 4 )
    }

    public void setFloat( Int32 offsetInFloats, Float32 v )
    {
        this._staging[ offsetInFloats ] = v
    }

    public void setVector3( Int32 offsetInFloats, Float32_3 v )
    {
        this._staging[ offsetInFloats ] = v.x
        this._staging[ offsetInFloats + 1 ] = v.y
        this._staging[ offsetInFloats + 2 ] = v.z
    }

    public void setVector4( Int32 offsetInFloats, Float32_4 v )
    {
        this._staging[ offsetInFloats ] = v.x
        this._staging[ offsetInFloats + 1 ] = v.y
        this._staging[ offsetInFloats + 2 ] = v.z
        this._staging[ offsetInFloats + 3 ] = v.w
    }

    # 4x4 矩阵按行主序写入 16 个 float
    public void setMatrix( Int32 offsetInFloats, Float32_4x4 m )
    {
        int k = 0
        while k < 16
        {
            this._staging[ offsetInFloats + k ] = m._mat4x4[k]
            k++
        }
    }

    # 把 staging 刷到 GPU（真实实现里是 memcpy 到 Upload 堆的映射地址）
    public void flush()
    {
        if !this.isCreated
        {
            ret
        }
        SystemCallExternalFunction( "Render.uploadConstantBuffer", this.handle, this._staging )
    }

    public Array<Float32> staging()
    {
        ret this._staging
    }
}

# 通用工具（避免各处重复写 is bool 拆箱）
public class GpuUtil
{
    public static bool asBool( object result )
    {
        if result is bool b
        {
            ret b
        }
        ret false
    }

    public static Int32 asInt( object result )
    {
        if result is Int32 n
        {
            ret n
        }
        ret 0
    }
}
