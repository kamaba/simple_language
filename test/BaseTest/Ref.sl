import Std
import CSharp.System


Level1
{
}

GenClass2
{
    static fun()
    {
        global.println("========== Ref / GenClass2 (start) ==========")
        Level1 l1 = new()
        # Switch to manual management so we can control lifetime
        Memory.Manual(l1)

        var refl1 = l1
        refl11 = Memory.Ref(l1)

        Memory.Release(l1)

        ref12 = Memory.WeakRef(l1)
        Memory.Free(l1)

        var cl1 = l1.clone()
        global.println("clone non-null -> " + (cl1 != null).toString())
        global.println("========== Ref / GenClass2 (end) ==========")
    }
}

# 测试说明：引用计数 ref、release、refWeak、free 等对象生命周期 API 的 smoke。
# 预期：无异常结束；clone 非空为 true；具体计数值依赖运行时 Memory.Manual 实现，用于回归对比输出。

#!
生成模板原则
1. 通过模板类，生成实体类后，初始化变量与继承的变量，还有就是方法和继承的方法里边的 参数与返回值，几个，如果包含模板后，进行替换，用做代码类型检查
2. 代码内部是不生成的，正常情况，只有运行时报错，比如 new() 如果 传进来的模板，没有不带参数的，会有报错，但只有运行时报错
3. 如果在编辑器模试，在写完某一部分，或者改动某一些地方后， 编辑器模式下，会生成函数具体的代码，用做检查，在检查完后，隔一段时间会删除掉
4. 如果使用dll，同样的，只生成外边接口的实例，生成后，内部export的元素进行生成 用做检查， 同样的，dll的代码直接运行时执行
5. 如果aot方式，需要编译时，需要先编译引入的dll生成模板相关的内容，然后再编译本地的实例，最终在llvm里边直接使用编译完的代码，然后执行。
6. 本地虚拟机中，增加模板概念，如果传入来的是模板，需要进行替换后，进行执行。
!#
