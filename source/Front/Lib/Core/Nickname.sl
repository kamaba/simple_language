public class Nickname extends Attribute
{
    private string _nickname = ""

    _init_( string nickname )
    {
        this._nickname = nickname
        this._attributeHandleType = 0
    }

    public get string nickname()
    {
        ret this._nickname
    }

    # 编译时回调：在宿主类的父命名空间下注册别名节点
    # 例如 Std.Float32_2 上标注 @Nickname("Vector2")
    # 则在 Std 节点下创建 Vector2 子节点，指向 Float32_2 的 MetaClass
    # 通过 Std.Vector2 可以访问到同一个类
    # 实际由 C# 侧 AttributeManager 执行：
    #   parentNode.AddMetaClassAlias(nickname, mc)
    override void OnCompile()
    {
    }
}
