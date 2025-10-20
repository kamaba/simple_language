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
LT
{
    private _init_()
    {

    }
    _init_( string1 )
    {
        
    }
}

List<T>
{
    
}

Level1<T,T2> extends List<T2>
{
    List<List<T2> > listt = null
    T _Level1_t = null
    T2 _Level1_t2 = null
    static T static_t = null;
    _init_( T t )
    {
        this._Level1_t = t
    }
    
    void setLevel1( T t )
    {
        if t == null
        {
            this._Level1_t = new()   # 如果T是一个private _init_() 的形式，会引用不调用该T的_init_的问题，因为定义private 意味着，不能直接调用_init_ 所以这个时候需要检查非反射情况，传入T的时候是否有new的情况，然后报错， 需要得到new后，在初始化生成类的时候
            #检查是否有new的调用,然后反向的提醒

            #这种情况，需要先获取类型
            # Type t = this._Level1_t.type;
            # MetaClass tc = t.metaClass;
            # MetaMemberFunction tcmc = tc.GetConstruction( int.metaClass, int.metaClass )
            # var newmc = MetaClass.CreateInstance( tc )
            # tcmc.Invoke( newmc, 10, 20 )
        }
        else
        {
            this._Level1_t = new()
            setlevel1_t2 = this.Level1_t()
            var str = setlevel1_t2.toString()
            this._Level1_t = t 
            #Level1<string, string>.static_t = str
            #Level1<string,object> aaa = new()
            #Open(true)
            #t1 = this.Level1_t()
            #Level1<List<T2>, List<string> >.Open(  List<T2>() )
        }
    }
    get T Level1_t()
    {
        ret this._Level1_t
    }   

    Level2<T>
    {
        static T Level2_t = null

        _init_( obj )
        {

        }
        _init_( int i )
        {

        }
    }
    static T Open( T t )
    {
        Level1<Level1<Level2<int>,Level2<T2> >, T >.static_t = Level1<Level2<int>,Level2<T2> >()
        T t1111 = new()
        T2 t2222 = new()
        Level2<T>.Level2_t = t
        Level1<T,T2>.static_t = t
        Level1<T2,T>.static_t = t2222
        Level1<T,short>.static_t = t1111;
        Level1<short,string>.static_t = 20s
        static_t = t
        ret static_t
    }
    override string toString()
    {
        #!
        if this._Level1_t.type == int.type
        {
            ret "int32"
        }
        else
        {
            ret this._Level1_t.toString()
        }
        !#
        ret "Level1 String----"
    }
}

GenClass{
    static fun()
    {
        Level1<int,string> testintstring = new()
        penret = Level1<string,byte>.Open("  !!!!!tttstring!!!!!!!    ")
        System.Console.WriteLine("_this_——————————————————————————————  " + penret )

        Level1<int, int>  GenClass2_fun_l1 = Level1<int, int>(100)
        GenClass2_fun_l1.setLevel1( 200 )
        System.Console.WriteLine("_this_—————————————————————————————— 222 " + GenClass2_fun_l1.Level1_t )

        Level1<Level1<int,int>, string > GenClass2_fun_l2 = Level1<Level1<int, int>, string >()
        GenClass2_fun_l2.setLevel1( Level1<int, int>(20) )
        System.Console.WriteLine("_this2_----------------------333333-" + GenClass2_fun_l2.Level1_t.Level1_t + "    string:" + Level1<string,byte>.static_t + "   short:" + Level1<short, string>.static_t )
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