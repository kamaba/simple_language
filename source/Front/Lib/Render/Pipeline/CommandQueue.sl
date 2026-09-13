# CommandQueue —— 命令队列 / 分配器 / 列表 / 围栏 / 交换链
#
# DX12：CommandQueue(GRAPHICS) + CommandAllocator + GraphicsCommandList + Fence + SwapChain
# Vulkan：Queue + CommandPool + CommandBuffer + Fence/Semaphore + SwapchainKHR

enum ECommandQueueType
{
    Graphics = 0
    Compute
    Copy
}

# ── 命令分配器 ─────────────────────────────────────────
public class CommandAllocator
{
    public ECommandQueueType type = ECommandQueueType.Graphics
    public Int32 handle = 0
    public bool isCreated = false

    public void _init_( ECommandQueueType _type )
    {
        this.type = _type
        this.handle = 0
        this.isCreated = false
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createCommandAllocator", this.type )
        if result is Int32 h
        {
            this.handle = h
            this.isCreated = true
            ret true
        }
        ret false
    }

    # 每帧复用前必须 reset（要求该 allocator 上的命令已执行完）
    public bool reset()
    {
        ret GpuUtil.asBool( SystemCallExternalFunction( "Render.resetCommandAllocator", this.handle ) )
    }

    public void destroy()
    {
        if this.isCreated
        {
            SystemCallExternalFunction( "Render.destroyCommandAllocator", this.handle )
            this.isCreated = false
        }
    }
}

# ── 底层命令列表 ───────────────────────────────────────
public class CommandList
{
    public CommandAllocator allocator = null
    public Int32 handle = 0
    public bool isClosed = true

    public void _init_( CommandAllocator _allocator )
    {
        this.allocator = _allocator
        this.handle = 0
        this.isClosed = true
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createCommandList", this.allocator.handle )
        if result is Int32 h
        {
            this.handle = h
            ret true
        }
        ret false
    }

    public bool begin()
    {
        bool ok = GpuUtil.asBool( SystemCallExternalFunction( "Render.commandListBegin", this.handle ) )
        if ok
        {
            this.isClosed = false
        }
        ret ok
    }

    public void close()
    {
        SystemCallExternalFunction( "Render.commandListClose", this.handle )
        this.isClosed = true
    }

    public void destroy()
    {
        SystemCallExternalFunction( "Render.destroyCommandList", this.handle )
    }
}

# ── 围栏（CPU/GPU 同步）─────────────────────────────────
public class Fence
{
    public string name = "fence"
    public Int32 handle = 0
    public Int64 value = 0
    public bool isCreated = false

    public void _init_()
    {
        this.name = "fence"
        this.handle = 0
        this.value = 0
        this.isCreated = false
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createFence" )
        if result is Int32 h
        {
            this.handle = h
            this.isCreated = true
            ret true
        }
        ret false
    }

    # 队列执行到此处时把围栏值推进
    public void signal( CommandQueue queue )
    {
        this.value = this.value + 1
        SystemCallExternalFunction( "Render.signalFence", queue.handle, this.handle, this.value )
    }

    public Int64 completedValue()
    {
        object result = SystemCallExternalFunction( "Render.getFenceValue", this.handle )
        if result is Int64 v
        {
            ret v
        }
        ret 0
    }

    public bool isCompleted( Int64 target )
    {
        ret this.completedValue() >= target
    }

    # 阻塞等待（帧同步时才用，正常走 isCompleted 轮询）
    public void wait( Int64 target )
    {
        SystemCallExternalFunction( "Render.waitFence", this.handle, target )
    }

    public void destroy()
    {
        if this.isCreated
        {
            SystemCallExternalFunction( "Render.destroyFence", this.handle )
            this.isCreated = false
        }
    }
}

# ── 命令队列 ───────────────────────────────────────────
public class CommandQueue
{
    public string name = "graphicsQueue"
    public ECommandQueueType type = ECommandQueueType.Graphics
    public Int32 handle = 0
    public bool isCreated = false

    public void _init_( ECommandQueueType _type )
    {
        this.type = _type
        if _type == ECommandQueueType.Compute
        {
            this.name = "computeQueue"
        }
        elif _type == ECommandQueueType.Copy
        {
            this.name = "copyQueue"
        }
        else
        {
            this.name = "graphicsQueue"
        }
        this.isCreated = false
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createCommandQueue", this.type )
        if result is Int32 h
        {
            this.handle = h
            this.isCreated = true
            ret true
        }
        ret false
    }

    # 提交命令缓冲（内部 ExecuteCommandLists）
    public void execute( CommandBuffer cb )
    {
        if cb == null
        {
            ret
        }
        SystemCallExternalFunction( "Render.executeCommandBuffer", this.handle, cb.handle )
    }

    public void wait( Fence fence, Int64 target )
    {
        SystemCallExternalFunction( "Render.queueWait", this.handle, fence.handle, target )
    }

    public void signal( Fence fence, Int64 target )
    {
        SystemCallExternalFunction( "Render.queueSignal", this.handle, fence.handle, target )
    }

    # 等队列排空（仅调试/关闭时用）
    public void waitIdle()
    {
        SystemCallExternalFunction( "Render.queueWaitIdle", this.handle )
    }

    public void destroy()
    {
        if this.isCreated
        {
            SystemCallExternalFunction( "Render.destroyCommandQueue", this.handle )
            this.isCreated = false
        }
    }

    override string toString()
    {
        ret "CommandQueue(" + this.name + ")"
    }
}

# ── 交换链 ─────────────────────────────────────────────
public class SwapChain
{
    public Int32 width = 1280
    public Int32 height = 720
    public Int32 bufferCount = 2
    public ETextureFormat format = ETextureFormat.RGBA8_SRGB
    public Int32 currentIndex = 0
    public bool vsync = true

    Array<GpuTexture> _backBuffers = null

    public void _init_()
    {
        this.width = 1280
        this.height = 720
        this.bufferCount = 2
        this.currentIndex = 0
        this._backBuffers = Array<GpuTexture>( 0 )
    }

    public bool create()
    {
        object result = SystemCallExternalFunction( "Render.createSwapChain",
            this.width, this.height, this.bufferCount, this.format )
        if !GpuUtil.asBool( result )
        {
            ret false
        }
        this._backBuffers = Array<GpuTexture>( this.bufferCount )
        int i = 0
        while i < this.bufferCount
        {
            GpuTexture t = GpuTexture( "BackBuffer" + i.toString(), this.width, this.height,
                this.format, ETextureUsage.RenderTarget )
            t.create()
            this._backBuffers[i] = t
            i++
        }
        ret true
    }

    public GpuTexture currentBackBuffer()
    {
        ret this._backBuffers[ this.currentIndex ]
    }

    public RenderTargetIdentifier currentBackBufferId()
    {
        ret RenderTargetIdentifier.fromTexture( this.currentBackBuffer() )
    }

    public void present()
    {
        SystemCallExternalFunction( "Render.presentSwapChain", this.vsync )
        this.currentIndex = ( this.currentIndex + 1 ) % this.bufferCount
    }

    public void resize( Int32 w, Int32 h )
    {
        this.width = w
        this.height = h
        SystemCallExternalFunction( "Render.resizeSwapChain", w, h )
    }

    public void destroy()
    {
        SystemCallExternalFunction( "Render.destroySwapChain" )
        this._backBuffers = Array<GpuTexture>( 0 )
    }

    override string toString()
    {
        ret "SwapChain(" + this.width.toString() + "x" + this.height.toString() + ", buffers=" + this.bufferCount.toString() + ")"
    }
}
