# Device —— 计算设备抽象（当前全部逻辑跑在 CPU，GPU 走后续后端接入）
#
# 用途：Tensor / 模型记录自己跑在哪个设备上；算子可根据 kind 选择不同实现

enum EDeviceKind
{
   CPU = 0
   GPU
   NPU
}

@Nickname("Device")
public class Device
{
    public Int32 kind = 0
    public Int32 id = 0
    public string name = "cpu"
    public bool available = true

    # 统计用：本设备当前分配的字节数（仅记录，不做真实内存管理）
    public Int64 bytesUsed = 0

    public void _init_()
    {
        this.kind = EDeviceKind.CPU
        this.id = 0
        this.name = "cpu"
        this.available = true
        this.bytesUsed = 0
    }

    public void _init_( Int32 kind, Int32 id )
    {
        this.kind = kind
        this.id = id
        this.available = true
        this.bytesUsed = 0
        if kind == EDeviceKind.GPU
        {
            this.name = "gpu:" + SystemConvertString( id )
        }
        elif kind == EDeviceKind.NPU
        {
            this.name = "npu:" + SystemConvertString( id )
        }
        else
        {
            this.name = "cpu"
        }
    }

    public bool isCpu()
    {
        ret this.kind == EDeviceKind.CPU
    }

    public static Device cpu()
    {
        ret Device( EDeviceKind.CPU, 0 )
    }

    public static Device gpu( Int32 id )
    {
        ret Device( EDeviceKind.GPU, id )
    }

    public static Device npu( Int32 id )
    {
        ret Device( EDeviceKind.NPU, id )
    }

    # 记录一次分配（仅统计）
    public void alloc( Int64 bytes )
    {
        this.bytesUsed = this.bytesUsed + bytes
    }

    public void free( Int64 bytes )
    {
        this.bytesUsed = this.bytesUsed - bytes
    }

    override string toString()
    {
        ret "Device(" + this.name + ")"
    }
}
