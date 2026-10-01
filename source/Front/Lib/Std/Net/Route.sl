public class Route extends Attribute
{
    # 运行时路由属性，类似 FastAPI 的路由装饰器
    # 用法: @Route("/action/getfin") 标注在类或方法上
    # 运行前（Preload）生效: 装配期注册路由（未接线, 实际接线随 Net 库另立设计）

    private string _route = ""

    _init_( string route )
    {
        this._route = route
        this._attributeStage = EAttributeStage.Preload
        this._attributeTargets = EAttributeTarget.All
    }

    public get string route()
    {
        ret this._route
    }
}
