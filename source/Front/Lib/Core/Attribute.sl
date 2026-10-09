# attribute 生效时点:
#   PreCompile = 0  编译前: Front Meta 层入口回调, 可跳过代码段
#   Compiling  = 1  编译中: Front IR/SLIR 物化
#   Preload    = 2  运行前: cvm 装载/装配期注册
#   Runtime    = 3  运行中: cvm 执行期触发/查询
enum EAttributeStage extends UInt8
{
    PreCompile = 0
    Compiling = 1
    Preload = 2
    Runtime = 3
}

# attribute 合法注册点（位掩码, 可组合）:
#   Class  = 1   class 声明
#   Data   = 2   模块级 data（全局数据）
#   Enum   = 4   enum 声明
#   Field  = 8   成员变量
#   Method = 16  成员函数
#   All    = 31  全部注册点（1|2|4|8|16; enum 位或暂受 Front 限制, 组合值以字面量预定义）
enum EAttributeTarget extends UInt8
{
    Class = 1
    Data = 2
    Enum = 4
    Field = 8
    Method = 16
    All = 31
}

public class Attribute extends Object
{
    # 生效时点 + 合法注册点（位掩码, 可组合）, 子类在 _init_ 中设置
    protected UInt8 _attributeStage = 0
    protected UInt8 _attributeTargets = 0

    # 宿主信息（由系统注入; Runtime 触发/查询回调中可读）
    protected string _ownerClassName = ""
    protected string _ownerMemberName = ""

    # 执行前回调（返回自身, 支持链式）; 仅 Runtime 触发型子类重写并携带真实逻辑
    public override Attribute _enter_()
    {
        ret this
    }

    # 退出回调; 仅 Runtime 触发型子类重写并携带真实逻辑
    public override void _exit_()
    {
    }

    # ---- Runtime 查询通道（ATTRIBUTE_DESIGN.md P7 / Q6）----
    # 返回宿主上绑定的 stage==Runtime 的 attribute 实例数组（与 _enter_/_exit_
    # 触发通道共享同一份实例缓存; 宿主未命中返回空数组）
    # className 支持全名/短名; 方法级仅匹配声明类自身, 同名方法有指令体者优先

    # 查询类级 attribute（含继承自 Attribute 的全部子类实例）
    public static Array<Attribute> getClassAttributes(string className)
    {
        ret SystemAttributeGetClassAttributes(className) as Array<Attribute>
    }

    # 查询成员变量 attribute（覆盖 static 与 instance 字段）
    public static Array<Attribute> getFieldAttributes(string className, string fieldName)
    {
        ret SystemAttributeGetFieldAttributes(className, fieldName) as Array<Attribute>
    }

    # 查询成员函数 attribute
    public static Array<Attribute> getMethodAttributes(string className, string methodName)
    {
        ret SystemAttributeGetMethodAttributes(className, methodName) as Array<Attribute>
    }
}

# 序列化标签: 标注在 class/data 上, 声明该类型支持通用序列化
# 成员序列化规则:
#   public 成员默认参与序列化
#   protected/private 成员默认不参与, 可用 @SerializeField() 强制参与
#   public 成员可用 @NonSerialized() 排除
public class Serializable extends Attribute
{
    _init_()
    {
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Class
    }
}

# 序列化标签: 强制 protected/private 成员参与序列化
public class SerializeField extends Attribute
{
    _init_()
    {
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Field
    }
}

# 序列化标签: 排除该 public 成员不参与序列化
public class NonSerialized extends Attribute
{
    _init_()
    {
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Field
    }
}
