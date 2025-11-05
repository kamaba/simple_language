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
    class Byte extends Object
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
        _init_(Int32 val )
        {
            
        }        
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
LT
{
    private _init_()
    {

    }
    _init_( string1 )
    {
        
    }
}

Level1<LT11,LT12>
{
    Test2<LT11> Level1_t1 = new()
    LT12 Level1_t2 = new()

    ok()
    {
        Test2<LT11> llll = new()
        ret llll
    }
}
Test1<T>
{
    T t1 = new()
}
Test2<T>
{
    T t2 = new()
}
Level2<LT21, LT22, LT23> extends Level1<Test2<LT23>,Test1<LT22> >
{
    LT22 Level21_t = new()  # Test1<AAAAA<string>> l2

    override LT22 add( LT22 tttt )
    {
        LT22 llevel11sx = new()

        ret tttt
    }

    static LT23 _test = new() 
    static LT23 getTest( LT23 lt23 )
    {
        _test = lt23

        ret _test
    }
}
Level3<LT31,LT32> extends Level2<LT31,Test1<LT32>, LT31>
{
    LT32 Level31_t = new()
}
AAAAA<AA>
{
    AA aa = new()
}
Level4<LT41,LT42> extends Level3<LT41, AAAAA<LT42> >
{
    LT41 Level41_t = new()
}

GenClass{
    static fun()
    {
        Level4<int,string> ll41 = new()
        ll41.Level41_t = 20
        ll41.Level31_t.aa = "aaa"
        ll41.Level21_t.t1.aa = "ttt11"
        ll41.Level1_t1.t2.t2 = 300
        ll41.Level1_t2.t1.t1.aa = "400"

        
        System.Console.WriteLine("_this_333331 " + ll41.Level41_t )
        System.Console.WriteLine("_this_333331 " + ll41.Level31_t.aa )
        System.Console.WriteLine("_this_333331 " + ll41.Level21_t.t1.aa  )
        System.Console.WriteLine("_this_333331 " + ll41.Level1_t1.t2.t2  )
        System.Console.WriteLine("_this_333331 " + ll41.Level1_t2.t1.t1.aa  )


        Level2<string,string,int> lll31 = new()
        lll31.Level21_t = "30000"
        lll31.Level1_t1.t2.t2 = 20000
        lll31.Level1_t2.t1 = "1000"
        

        System.Console.WriteLine("_this_333332 " + lll31.Level21_t  )
        System.Console.WriteLine("_this_333333 " + lll31.Level1_t1.t2.t2 )
        System.Console.WriteLine("_this_333333 " + lll31.Level1_t2.t1 )
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