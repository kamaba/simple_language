import Std
import CSharp.System

namespace Core
{
    export class Object
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
    LT11 Level1_t1 = new()
    LT12 Level1_t2 = new()
}
interface Interface1<IT1>
{
    IT1 add()
}
Level2<LT21, LT22, LT23> extends Level1<LT23,LT22> interface Interface1<LT23>
{
    LT22 Level21_t = new()

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
Level3<LT31, LT32> extends Level2<LT32, LT32, LT31>
{
    _init_( LT31 lt31 )
    {
        this.Level3_t = lt31
    }

    LT31 Level3_t = new()
}

Level4<LT41,LT42> extends Level3<LT42,LT41>
{

}

GenClass{
    static fun()
    {

        Level4<string,int> llll3333 = new("300")
        addval = llll3333.add(1000)
        addval2 = Level2<string,int,int>.getTest( 2000 )
        #llll3333.Level1_t2 = 10
        #llll3333.Level21_t = 20

        System.Console.WriteLine("_this_33333 " + llll3333.Level3_t )
        System.Console.WriteLine("_this_33333 " + addval )
        System.Console.WriteLine("_this_33333 " + addval2 )
        #System.Console.WriteLine("_this_22222 " + llll3333.Level21_t )
        #System.Console.WriteLine("_this_11111 " + llll3333.Level1_t2 )
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