public class Route extends Attribute
{
    # 运行时路由属性，类似 FastAPI 的路由装饰器
    # 用法: @Route("/action/getfin") 标注在类或方法上

    private string _route = ""

    _init_( string route )
    {
        this._route = route
        this._attributeHandleType = 1
    }

    public string route
    {
        ret this._route
    }

    # 运行时加载回调：注册路由
    # 由 VM 实例化后调用，执行系统调用注册到 RuntimeAttributeRegistry
    override void OnRuntimeLoad()
    {
        SystemRouteRegister( this._route, this._ownerClassName, this._ownerMemberName )
    }
}
