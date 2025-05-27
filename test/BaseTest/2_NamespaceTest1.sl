
namespace Std.Layer1_1;    #这种方式 只适合于在外屋已经定义类名后，然后在该节点下有类名的前缀

namespace Std.Layer1_1
{
    class TClass                        #嵌套定义
    {
        
    }
    class Layer2_1.TClass2              #嵌套查找定义
    {

    }
}
namespace N1
{
    namespace N2.N3
    {
        N1_N2_N3_C1                 
        {

        }
    }
    N2.N1_C1   #属于有topNS有，但仍要再次查访的类型
    {

    }
}

Layer1_1.C2                     #搜索命名空间定义
{
    

}


# 分三种定义方式
#    1. 使用全部外部定义 命名空间与类名  该模式下，内部不允许新增类和其它命名空间
#    2. 使用定义命名空间，但可以自定义类，不允许新增类
#    3. 无限制模式，即可以定义命名空间，也可以随意定义类
# 命名空间分两种使用
#    1. 一种是，使用搜索定义类方式，即 namespace NamespaceName1.NamespaceName2; 后边无封号，即定义为搜索命名空间，如果要在定义类中使用，必须在类前使用NamespaceName2.ClassName 这样的方式，进行搜索关联
#    2. 另外，可以内嵌套的方式，即 namespace N1{ namespace N2{ class C1{} } class C2{} }这种形式
# 关于类的前缀空间定义 如果使用了前缀定义，要不与namespace{}这种形式的嵌套定义，要不就以搜索结尾定义