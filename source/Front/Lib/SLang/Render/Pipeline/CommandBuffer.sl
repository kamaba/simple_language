# CommandBuffer —— GPU 命令录制
#
# 与 Unity CommandBuffer / DX12 GraphicsCommandList 同思路：
#   先把命令录进缓冲（SetPipelineState / SetRenderTarget / DrawIndexed ...）
#   再由 CommandQueue 一次性提交执行（ExecuteCommandLists）
#
# 本实现采用「直接转发到后端命令流」的方式，SL 侧只负责参数组织与顺序保证

public class CommandBuffer
{
    public string name = "cmd"
    public Int32 handle = 0
    public bool isRecording = false
    public Int32 commandCount = 0

    public void _init_()
    {
        this.name = "cmd"
        this.handle = 0
        this.isRecording = false
        this.commandCount = 0
    }

    public void _init_( string _name )
    {
        this._init_()
        this.name = _name
    }

    # ── 录制 ─────────────────────────────────────────────
    public void begin()
    {
        SystemCallExternalFunction( "Render.cmdBegin", this.name )
        this.isRecording = true
        this.commandCount = 0
    }

    public void end()
    {
        SystemCallExternalFunction( "Render.cmdEnd" )
        this.isRecording = false
    }

    void push()
    {
        this.commandCount++
    }

    # ── 状态 ─────────────────────────────────────────────
    public void setViewport( Int32 x, Int32 y, Int32 width, Int32 height )
    {
        SystemCallExternalFunction( "Render.cmdSetViewport", x, y, width, height )
        this.push()
    }

    public void setScissorRect( Int32 x, Int32 y, Int32 width, Int32 height )
    {
        SystemCallExternalFunction( "Render.cmdSetScissor", x, y, width, height )
        this.push()
    }

    # ── 渲染目标 ─────────────────────────────────────────
    public void setRenderTarget( RenderTargetBinding binding )
    {
        if binding == null
        {
            ret
        }
        SystemCallExternalFunction( "Render.cmdSetRenderTargets", binding )
        this.push()
    }

    public void setRenderTarget( RenderTargetIdentifier color, RenderTargetIdentifier depth )
    {
        RenderTargetBinding b = RenderTargetBinding( color, depth )
        this.setRenderTarget( b )
    }

    public void clearRenderTarget( bool clearColor, bool clearDepth, Color c )
    {
        SystemCallExternalFunction( "Render.cmdClear",
            clearColor, clearDepth, c.r, c.g, c.b, c.a )
        this.push()
    }

    # ── 管线 ─────────────────────────────────────────────
    public void setPipelineState( GraphicsPipelineState pso )
    {
        if pso == null
        {
            ret
        }
        SystemCallExternalFunction( "Render.cmdSetPipelineState", pso.handle )
        this.push()
    }

    public void setComputePipelineState( ComputePipelineState pso )
    {
        if pso == null
        {
            ret
        }
        SystemCallExternalFunction( "Render.cmdSetComputePipelineState", pso.handle )
        this.push()
    }

    public void setRootSignature( RootSignature sig )
    {
        if sig == null
        {
            ret
        }
        SystemCallExternalFunction( "Render.cmdSetRootSignature", sig.handle )
        this.push()
    }

    public void setDescriptorSet( DescriptorSet set )
    {
        if set == null
        {
            ret
        }
        SystemCallExternalFunction( "Render.cmdSetDescriptorSet", set )
        this.push()
    }

    # ── 资源绑定 ─────────────────────────────────────────
    public void setVertexBuffer( VertexBuffer vb )
    {
        SystemCallExternalFunction( "Render.cmdSetVertexBuffer", vb.handle, vb.stride )
        this.push()
    }

    public void setIndexBuffer( IndexBuffer ib )
    {
        SystemCallExternalFunction( "Render.cmdSetIndexBuffer", ib.handle, ib.use32Bit )
        this.push()
    }

    public void setConstantBuffer( Int32 slot, ConstantBuffer cb )
    {
        SystemCallExternalFunction( "Render.cmdSetConstantBuffer", slot, cb.handle )
        this.push()
    }

    public void setTexture( Int32 slot, GpuTextureView view )
    {
        SystemCallExternalFunction( "Render.cmdSetTexture", slot, view.descriptorIndex )
        this.push()
    }

    public void setSampler( Int32 slot, Sampler s )
    {
        SystemCallExternalFunction( "Render.cmdSetSampler", slot, s.descriptorIndex )
        this.push()
    }

    # ── 绘制 ─────────────────────────────────────────────
    public void draw( Int32 vertexCount, Int32 instanceCount, Int32 startVertex )
    {
        SystemCallExternalFunction( "Render.cmdDraw", vertexCount, instanceCount, startVertex )
        this.push()
    }

    public void drawIndexed( Int32 indexCount, Int32 instanceCount, Int32 startIndex, Int32 baseVertex )
    {
        SystemCallExternalFunction( "Render.cmdDrawIndexed", indexCount, instanceCount, startIndex, baseVertex )
        this.push()
    }

    # 无顶点缓冲绘制（全屏三角 / 程序化几何）
    public void drawProcedural( Int32 vertexCount, Int32 instanceCount )
    {
        SystemCallExternalFunction( "Render.cmdDrawProcedural", vertexCount, instanceCount )
        this.push()
    }

    # 便捷接口：把 Mesh / Material / 变换直接交给后端（与旧 Render.drawMesh 对齐）
    public void drawMesh( Mesh mesh, Material material, Float32_4x4 model, Float32_4x4 viewProjection )
    {
        if mesh == null || material == null
        {
            ret
        }
        SystemCallExternalFunction( "Render.cmdDrawMesh", mesh, material, model, viewProjection )
        this.push()
    }

    public void dispatch( Int32 groupX, Int32 groupY, Int32 groupZ )
    {
        SystemCallExternalFunction( "Render.cmdDispatch", groupX, groupY, groupZ )
        this.push()
    }

    # ── 资源屏障 / 拷贝 ──────────────────────────────────
    public void resourceBarrier( GpuResource res, EResourceState before, EResourceState after )
    {
        if res == null
        {
            ret
        }
        SystemCallExternalFunction( "Render.cmdResourceBarrier", res.handle, before, after )
        res.state = after
        this.push()
    }

    public void copyBuffer( GpuBuffer src, GpuBuffer dst )
    {
        SystemCallExternalFunction( "Render.cmdCopyBuffer", src.handle, dst.handle )
        this.push()
    }

    public void copyTexture( GpuTexture src, GpuTexture dst )
    {
        SystemCallExternalFunction( "Render.cmdCopyTexture", src.handle, dst.handle )
        this.push()
    }

    # ── 调试标记（RenderDoc / PIX 会显示）──────────────────
    public void beginEvent( string label )
    {
        SystemCallExternalFunction( "Render.cmdBeginEvent", label )
    }

    public void endEvent()
    {
        SystemCallExternalFunction( "Render.cmdEndEvent" )
    }

    public void clear()
    {
        SystemCallExternalFunction( "Render.cmdClearCommands" )
        this.commandCount = 0
    }

    override string toString()
    {
        ret "CommandBuffer(" + this.name + ", cmds=" + this.commandCount.toString() + ")"
    }
}

# CommandBufferPool —— 复用命令缓冲，避免每帧新建（Unity 同名类）
public class CommandBufferPool
{
    Array<CommandBuffer> _pool = null
    Int32 _used = 0

    public void _init_()
    {
        this._pool = Array<CommandBuffer>( 0 )
        this._used = 0
    }

    public CommandBuffer get( string name )
    {
        if this._used < this._pool.length
        {
            CommandBuffer cb = this._pool[ this._used ]
            this._used = this._used + 1
            cb.name = name
            cb.clear()
            ret cb
        }
        CommandBuffer ncb = CommandBuffer( name )
        Array<CommandBuffer> np = Array<CommandBuffer>( this._pool.length + 1 )
        int i = 0
        while i < this._pool.length
        {
            np[i] = this._pool[i]
            i++
        }
        np[ this._pool.length ] = ncb
        this._pool = np
        this._used = this._used + 1
        ret ncb
    }

    # 每帧结束调用：游标归零，池子本身保留
    public void releaseAll()
    {
        this._used = 0
    }

    public get int poolSize()
    {
        ret this._pool.length
    }
}
