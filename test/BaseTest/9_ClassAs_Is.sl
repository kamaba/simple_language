import Std
import CSharp.System

namespace Core
{
    class Object
    {
        public void _init_()
        {

        }

        public string toString()
        {
            ret ""
        }
    }
    class Byte
    {
        
    }
    class Boolean
    {

    }
    class SByte
    {
        
    }
    class Int16
    {
        
    }
    class UInt16
    {
        
    }
    class Int32
    {
        
    }
    class UInt32
    {
        
    }
    class Int64
    {
        
    }
    class UInt64
    {
        
    }
    class Float32
    {
        
    }
    class Float64
    {
        _init_(Float64 f)
        {

        }
    }
    class String
    {
        _init_( String str )
        {

        }
    }

}

Level1
{
    Level1_var1 = 20
}
Level2 extends Level1
{
    Level2_var1 = 30;
}
Level3 extends Level2
{
    Level3_var3 = 40
}


ClassAsIs{
    static fun()
    {
        Level3 level3 = new()
        Level1 level1 = level3
        Level2 = level1 as Level2

        bool aaa = level1 is Level2 level2tt
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