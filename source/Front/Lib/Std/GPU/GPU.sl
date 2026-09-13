public class GPU extends Attribute
{
    # GPU（Device Compute）属性 - 标注方法为 GPU kernel
    # 用法:
    #   @GPU()                        使用全部默认值
    #   @GPU( 32, 32 )                指定 tile 尺寸
    #   @GPU( 32, 32, 4, 0 )          tile 尺寸 + tileNum + groupId
    #   @GPU( 32, 32, 4, 0, 16, 16, 1, 256, 1, 1, 0, 0, "kernel" )  全参
    # 标注在成员函数上，由 MLIRExporter 发射 gpu.module/gpu.func，
    # host 侧发射 gpu.launch 调度逻辑

    # ---- Tile 分块参数（矩阵分块计算） ----

    # tile 宽度（列方向元素数）
    private Int32 _tileSizeWidth = 16

    # tile 高度（行方向元素数）
    private Int32 _tileSizeHeight = 16

    # tile 总数（一维展开的 tile 编号上限，0 = 按 grid*block 自动推导）
    private Int32 _tileNum = 0

    # 工作组 id（多 kernel 协作时的分组编号，0 = 默认组）
    private Int32 _groupId = 0

    # ---- Launch 网格参数（CUDA grid/block 语义） ----

    # grid 维度（block 数量）
    private Int32 _gridDimX = 1
    private Int32 _gridDimY = 1
    private Int32 _gridDimZ = 1

    # block 维度（每 block 线程数）
    private Int32 _blockDimX = 256
    private Int32 _blockDimY = 1
    private Int32 _blockDimZ = 1

    # 每 block 动态共享内存字节数
    private Int32 _sharedMemorySize = 0

    # 目标设备编号（多 GPU 时选择）
    private Int32 _deviceId = 0

    # kernel 符号名（空 = 使用方法名）
    private string _kernelName = ""

    # 无参构造: @GPU() 使用全部默认值
    override _init_()
    {
        this._attributeHandleType = 0
    }

    # 全参构造:
    #   @GPU( tileSizeWidth, tileSizeHeight, tileNum, groupId,
    #         gridDimX, gridDimY, gridDimZ, blockDimX, blockDimY, blockDimZ,
    #         sharedMemorySize, deviceId, kernelName )
    _init_( Int32 tileSizeWidth, Int32 tileSizeHeight, Int32 tileNum, Int32 groupId,
            Int32 gridDimX, Int32 gridDimY, Int32 gridDimZ,
            Int32 blockDimX, Int32 blockDimY, Int32 blockDimZ,
            Int32 sharedMemorySize, Int32 deviceId, string kernelName )
    {
        this._tileSizeWidth = tileSizeWidth
        this._tileSizeHeight = tileSizeHeight
        this._tileNum = tileNum
        this._groupId = groupId
        this._gridDimX = gridDimX
        this._gridDimY = gridDimY
        this._gridDimZ = gridDimZ
        this._blockDimX = blockDimX
        this._blockDimY = blockDimY
        this._blockDimZ = blockDimZ
        this._sharedMemorySize = sharedMemorySize
        this._deviceId = deviceId
        this._kernelName = kernelName
        this._attributeHandleType = 0
    }

    # ---- Tile 参数 getter ----
    public get Int32 tileSizeWidth()
    {
        ret this._tileSizeWidth
    }

    public get Int32 tileSizeHeight()
    {
        ret this._tileSizeHeight
    }

    public get Int32 tileNum()
    {
        ret this._tileNum
    }

    public get Int32 groupId()
    {
        ret this._groupId
    }

    # ---- Launch 网格参数 getter ----
    public get Int32 gridDimX()
    {
        ret this._gridDimX
    }

    public get Int32 gridDimY()
    {
        ret this._gridDimY
    }

    public get Int32 gridDimZ()
    {
        ret this._gridDimZ
    }

    public get Int32 blockDimX()
    {
        ret this._blockDimX
    }

    public get Int32 blockDimY()
    {
        ret this._blockDimY
    }

    public get Int32 blockDimZ()
    {
        ret this._blockDimZ
    }

    public get Int32 sharedMemorySize()
    {
        ret this._sharedMemorySize
    }

    public get Int32 deviceId()
    {
        ret this._deviceId
    }

    public get string kernelName()
    {
        ret this._kernelName
    }

    # 编译时回调 - 由 C# 侧 AttributeManager 处理:
    # MLIRExporter 读取 tile/launch 参数发射 gpu.module + gpu.func + gpu.launch
    override void OnCompile()
    {
    }
}
