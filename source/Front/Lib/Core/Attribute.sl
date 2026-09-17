enum EAttributeHandleType extends UInt8
{
   Compile = 0
   Runtime
}

public class Attribute extends Object
{
    # 处理时机：子类在 _init_ 中设置
    # Compile = 编译时处理（C# 侧执行）
    # Runtime = 运行时处理（VM 实例化后执行 OnRuntimeLoad/OnRuntime）
    protected UInt8 _attributeHandleType = 0

    # 宿主信息（由系统在处理时注入）
    private string _ownerClassName = ""
    private string _ownerMemberName = ""

    # 编译时回调 - 子类可重写
    # 在编译阶段被调用，可操作 MetaCore 层
    # 注意：编译时由 C# 侧执行对应逻辑，此方法为行为声明
    public void OnCompile()
    {
    }

    # 运行时加载回调 - 子类可重写
    # 在 VM 加载模块时被调用，注册运行时数据
    public void OnRuntimeLoad()
    {
    }

    # 运行时回调 - 子类可重写
    # 在运行到标注节点时被调用
    public void OnRuntime()
    {
    }
}

# 序列化标签：标注在 class/data 上，声明该类型支持通用序列化
# 成员序列化规则：
#   public 成员默认参与序列化
#   protected/private 成员默认不参与，可用 @SerializeField() 强制参与
#   public 成员可用 @NonSerialized() 排除
public class Serializable extends Attribute
{
    _init_()
    {
        this._attributeHandleType = 0
    }

    override void OnCompile()
    {
    }
}

# 序列化标签：强制 protected/private 成员参与序列化
public class SerializeField extends Attribute
{
    _init_()
    {
        this._attributeHandleType = 0
    }

    override void OnCompile()
    {
    }
}

# 序列化标签：排除该 public 成员不参与序列化
public class NonSerialized extends Attribute
{
    _init_()
    {
        this._attributeHandleType = 0
    }

    override void OnCompile()
    {
    }
}
