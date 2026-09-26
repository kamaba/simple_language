public class Condition extends Attribute
{
    # 运行时条件标记属性
    # 用法: @Condition("Debug") 标注在类或方法上
    # 内置条件: "Debug", "Release"
    # 也可通过 ActivateCondition/DeactivateCondition 自定义条件

    private string _condition = ""

    _init_( string condition )
    {
        this._condition = condition
        this._attributeHandleType = 1
    }

    public string condition
    {
        ret this._condition
    }

    # 运行时加载回调：注册条件
    # 由 VM 实例化后调用，执行系统调用注册到 RuntimeAttributeRegistry
    override void OnRuntimeLoad()
    {
        SystemConditionRegister( this._condition, this._ownerClassName, this._ownerMemberName )
    }
}
