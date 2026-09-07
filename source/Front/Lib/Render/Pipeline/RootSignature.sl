# RootSignature —— 着色器资源绑定模型
#
# DX12：RootSignature 描述「根参数（CBV/SRV/UAV/描述符表）」，描述符放 DescriptorHeap
# Vulkan：对应 PipelineLayout + DescriptorSetLayout + DescriptorSet
# 这里把两者折中：
#   RootSignature  声明绑定布局（哪些槽位、什么类型、对哪个阶段可见）
#   DescriptorHeap GPU 可见描述符的分配器
#   DescriptorSet  一组具体绑定（CBV 指向哪个 buffer、SRV 指向哪张贴图）

enum EShaderVisibility
{
    All = 0
    Vertex
    Pixel
    Compute
}

enum EDescriptorType
{
    CBV = 0
    SRV
    UAV
    Sampler
}

enum ERootParameterType
{
    ConstantBufferView = 0
    ShaderResourceView
    UnorderedAccessView
    DescriptorTable
    RootConstants
}

# ── 根参数 ─────────────────────────────────────────────
public class RootParameter
{
    public ERootParameterType paramType = ERootParameterType.ConstantBufferView
    public EDescriptorType descriptorType = EDescriptorType.CBV
    public Int32 shaderRegister = 0
    public Int32 registerSpace = 0
    public EShaderVisibility visibility = EShaderVisibility.All
    public Int32 descriptorCount = 1
    public Int32 constantsSizeInBytes = 0

    public void _init_()
    {
        this.paramType = ERootParameterType.ConstantBufferView
        this.descriptorType = EDescriptorType.CBV
        this.shaderRegister = 0
        this.registerSpace = 0
        this.visibility = EShaderVisibility.All
        this.descriptorCount = 1
        this.constantsSizeInBytes = 0
    }

    public static RootParameter cbv( Int32 reg, Int32 space, EShaderVisibility vis )
    {
        RootParameter p = RootParameter()
        p.paramType = ERootParameterType.ConstantBufferView
        p.descriptorType = EDescriptorType.CBV
        p.shaderRegister = reg
        p.registerSpace = space
        p.visibility = vis
        ret p
    }

    public static RootParameter srv( Int32 reg, Int32 space, EShaderVisibility vis )
    {
        RootParameter p = RootParameter()
        p.paramType = ERootParameterType.ShaderResourceView
        p.descriptorType = EDescriptorType.SRV
        p.shaderRegister = reg
        p.registerSpace = space
        p.visibility = vis
        ret p
    }

    public static RootParameter uav( Int32 reg, Int32 space, EShaderVisibility vis )
    {
        RootParameter p = RootParameter()
        p.paramType = ERootParameterType.UnorderedAccessView
        p.descriptorType = EDescriptorType.UAV
        p.shaderRegister = reg
        p.registerSpace = space
        p.visibility = vis
        ret p
    }

    override string toString()
    {
        ret "RootParam(reg=" + this.shaderRegister.toString() + ", space=" + this.registerSpace.toString() + ")"
    }
}

# ── 根签名 ─────────────────────────────────────────────
public class RootSignature
{
    public string name = "rootSignature"
    Array<RootParameter> _parameters = null
    Array<SamplerDesc> _staticSamplers = null

    public bool allowInputLayout = true
    public bool allowStreamOutput = false
    public bool isCreated = false
    public Int32 handle = 0

    public void _init_()
    {
        this.name = "rootSignature"
        this._parameters = Array<RootParameter>( 0 )
        this._staticSamplers = Array<SamplerDesc>( 0 )
        this.allowInputLayout = true
        this.isCreated = false
        this.handle = 0
    }

    public void _init_( string _name )
    {
        this._init_()
        this.name = _name
    }

    void push( RootParameter p )
    {
        Array<RootParameter> np = Array<RootParameter>( this._parameters.length + 1 )
        int i = 0
        while i < this._parameters.length
        {
            np[i] = this._parameters[i]
            i++
        }
        np[ this._parameters.length ] = p
        this._parameters = np
    }

    public void addConstantBufferView( Int32 reg, Int32 space, EShaderVisibility vis )
    {
        this.push( RootParameter.cbv( reg, space, vis ) )
    }

    public void addShaderResourceView( Int32 reg, Int32 space, EShaderVisibility vis )
    {
        this.push( RootParameter.srv( reg, space, vis ) )
    }

    public void addUnorderedAccessView( Int32 reg, Int32 space, EShaderVisibility vis )
    {
        this.push( RootParameter.uav( reg, space, vis ) )
    }

    # 把若干连续描述符打包成一张表（DX12 DescriptorTable / Vulkan 一个 binding 数组）
    public void addDescriptorTable( EDescriptorType type, Int32 reg, Int32 count, EShaderVisibility vis )
    {
        RootParameter p = RootParameter()
        p.paramType = ERootParameterType.DescriptorTable
        p.descriptorType = type
        p.shaderRegister = reg
        p.descriptorCount = count
        p.visibility = vis
        this.push( p )
    }

    public void addStaticSampler( SamplerDesc desc )
    {
        Array<SamplerDesc> ns = Array<SamplerDesc>( this._staticSamplers.length + 1 )
        int i = 0
        while i < this._staticSamplers.length
        {
            ns[i] = this._staticSamplers[i]
            i++
        }
        ns[ this._staticSamplers.length ] = desc
        this._staticSamplers = ns
    }

    public get int parameterCount()
    {
        ret this._parameters.length
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createRootSignature",
            this.name, this._parameters, this._staticSamplers,
            this.allowInputLayout, this.allowStreamOutput )
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
            SystemCallExternalFunction( "Render.destroyRootSignature", this.handle )
            this.isCreated = false
            this.handle = 0
        }
    }

    # Unity 风格默认布局：b0 每帧 / b1 每物体 / b2 材质 / b3 光照 + t0 主贴图 + s0 采样器
    public static RootSignature defaultGraphics()
    {
        RootSignature sig = RootSignature( "DefaultGraphics" )
        sig.addConstantBufferView( 0, 0, EShaderVisibility.All )
        sig.addConstantBufferView( 1, 0, EShaderVisibility.Vertex )
        sig.addConstantBufferView( 2, 0, EShaderVisibility.Pixel )
        sig.addConstantBufferView( 3, 0, EShaderVisibility.Pixel )
        sig.addDescriptorTable( EDescriptorType.SRV, 0, 8, EShaderVisibility.Pixel )
        sig.addStaticSampler( SamplerDesc.linearClamp() )
        ret sig
    }

    override string toString()
    {
        ret "RootSignature(" + this.name + ", params=" + this.parameterCount().toString() + ")"
    }
}

# ── 描述符堆 ───────────────────────────────────────────
public class DescriptorHeap
{
    public EDescriptorType type = EDescriptorType.CBV
    public Int32 capacity = 256
    public Int32 used = 0
    public Int32 incrementSize = 32
    public bool shaderVisible = true
    public Int32 handle = 0

    public void _init_( EDescriptorType _type, Int32 _capacity )
    {
        this.type = _type
        this.capacity = _capacity
        this.used = 0
        this.handle = 0
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createDescriptorHeap",
            this.type, this.capacity, this.shaderVisible )
        if result is Int32 h
        {
            this.handle = h
            ret true
        }
        ret false
    }

    public bool hasSpace( Int32 count )
    {
        ret this.used + count <= this.capacity
    }

    # 线性分配（真实实现里要有空闲链表 + 帧级回收）
    public Int32 allocate( Int32 count )
    {
        if !this.hasSpace( count )
        {
            ret -1
        }
        Int32 start = this.used
        this.used = this.used + count
        ret start
    }

    # 每帧开头重置（配合环形缓冲使用）
    public void reset()
    {
        this.used = 0
    }

    override string toString()
    {
        ret "DescriptorHeap(type=" + this.type.toString() + ", used=" + this.used.toString() + "/" + this.capacity.toString() + ")"
    }
}

# ── 描述符集（具体绑定）────────────────────────────────
public class DescriptorSet
{
    public RootSignature signature = null
    public DescriptorHeap heap = null

    Array<Int32> _cbv = null
    Array<Int32> _srv = null
    Array<Int32> _uav = null
    Array<Int32> _samplers = null

    # 固定容量：8 CBV / 8 SRV / 4 UAV / 4 Sampler，-1 表示该槽未绑定
    public void _init_()
    {
        this._cbv = DescriptorSet.makeSlots( 8 )
        this._srv = DescriptorSet.makeSlots( 8 )
        this._uav = DescriptorSet.makeSlots( 4 )
        this._samplers = DescriptorSet.makeSlots( 4 )
    }

    static Array<Int32> makeSlots( Int32 count )
    {
        Array<Int32> a = Array<Int32>( count )
        int i = 0
        while i < count
        {
            a[i] = -1
            i++
        }
        ret a
    }

    public void _init_( RootSignature _signature, DescriptorHeap _heap )
    {
        this._init_()
        this.signature = _signature
        this.heap = _heap
    }

    public void setConstantBuffer( Int32 slot, ConstantBuffer cb )
    {
        DescriptorSet.growInt( this._cbv, slot, cb.handle )
    }

    public void setTexture( Int32 slot, GpuTextureView view )
    {
        DescriptorSet.growInt( this._srv, slot, view.descriptorIndex )
    }

    public void setUnorderedAccess( Int32 slot, GpuTextureView view )
    {
        DescriptorSet.growInt( this._uav, slot, view.descriptorIndex )
    }

    public void setSampler( Int32 slot, Sampler s )
    {
        DescriptorSet.growInt( this._samplers, slot, s.descriptorIndex )
    }

    # 把 CPU 侧绑定刷进 GPU 可见堆
    public bool apply()
    {
        if this.signature == null
        {
            ret false
        }
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.applyDescriptorSet",
            this.signature.handle, this._cbv, this._srv, this._uav, this._samplers ) )
    }

    # 按 slot 扩容写入（slot 越界时补 -1）
    static void growInt( Array<Int32> arr, Int32 slot, Int32 value )
    {
        if slot < arr.length
        {
            arr[slot] = value
        }
    }

    public get int textureCount()
    {
        ret this._srv.length
    }
}
