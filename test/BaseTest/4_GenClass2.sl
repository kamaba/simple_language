import Std
import CSharp.System


Level1<T>
{
    T Level1_t = null
    _init_( T t )
    {
        this.Level1_t = t
    }
    void setLevel1( T t )
    {
        if t == null
        {
            this.Level1_t = new()
        }
        else
        {
            this.Level1_t = t
        }
    }
    get T Level1_t()
    {
        ret this.Level1_t
    }
}

GenClass2{
    static fun()
    {

        Level1<int>  GenClass2_fun_l1 = Level1<int>()
        #GenClass2_fun_l1.setLevel1( 200 )
        #System.Console.Write("Flaoat" + GenClass2_fun_l1.Level1_t )

        #Level1<Level1<int> > GenClass2_fun_l2 = Level1<Level1<int> >()
        #GenClass2_fun_l2.setLevel1( Level1<int>(20) )
        #System.Console.Write("Flaoat2" + GenClass2_fun_l2.Level1_t.Level1_t )
    }
}

#!
生成模板原则
1. 通过模板类，生成实体类后，初始化变量与继承的变量，还有就是方法和继承的方法里边的 参数与返回值，几个，如果包含模板后，进行替换，用做代码类型检查
2. 代码内部是不生成的，正常情况，只有运行时才会检查是否正常，比如 new() 如果 传进来的模板，没有不带参数的，会有报错，但只有运行时报错
3. 如果在编辑器模试，在写完某一部分，或者改动某一些地方后， 编辑器模式下，会生成函数具体的代码，用做检查，在检查完后，隔一段时间会删除掉
4. 如果使用dll，同样的，只生成外边接口的实例，生成后，内部export的元素进行生成 用做检查， 同样的，dll的代码直接运行时执行
5. 如果aot方式，需要编译时，需要先编译引入的dll生成模板相关的内容，然后再编译本地的实例，最终在llvm里边直接使用编译完的代码，然后执行。
6. 本地虚拟机中，增加模板概念，如果传入来的是模板，需要进行替换后，进行执行。
!#