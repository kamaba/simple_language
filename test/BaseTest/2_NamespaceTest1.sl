import Std;

namespace Std.Layer1_1;    #这种方式 只适合于在外屋已经定义类名后，然后在该节点下有类名的前缀

namespace Std.Layer1_1
{
    class TClass
    {
        
    }
    class Layer2_1.TClass2
    {

    }
}
namespace Std.NewDe
{

}

Layer1_1.TClass3
{

}

Mawangye
{
    
}


# import 是导入包与命名空间，只允许命名空间的导入
# 如果使用 然后在下边的代码中，通过import路径，可以在表达式中，或者是定义变量时，查找对应的类
# 使用isUseDefineNamespace 方式,可以使用 namespace N1{ namespace N2{} }的方式， 定义在项目中，没有配置过的节点 否则，不允许对新的命名空间进行定义
# 使用isUseNamespaceSearch     了namespace N1.N2方式 非{}的方式 会根据在项目中定义 namespace关系，然后进行自动匹配，如果没有匹配到，则会自动加入最外层namespace
# 如果在项目中，定义，只允许使用namespace已定义，则全程搜索相关的节点。不会自己查找额外节点
# 不使用 isUseDefineNamespace 方式， 则不是使用 namespace N1.N2的方式寻找