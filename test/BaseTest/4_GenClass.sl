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
Map<MapT1,MapT2>
{
    MapT1 m1 = null
    MapT2 m2 = null

    static MapT3 MapFunc<MapT3>()
    {
        ret null
    }
}
Level1<LevelT1>
{
    static LevelT1 LevelStaticValue = null
    LevelT1 LevelMemValue = new()
    Map<LevelT1, Level1<LevelT1> > mappp22 = new()

    Level1Com()
    {
        this.LevelMemValue = new()

        LevelT1 lt2 = new()
    }

    LevelT3 Level1Fun<LevelT3>( )
    {
        #LevelStaticValue = new()

        #Map< Level1<LevelT3>, LevelT1> map2 = new()

        this.LevelMemValue = new()

        #Map<LevelT1,Level1<LevelT3> > map = new()

        LevelT3 lt2 = new()

        ret lt2
    }
    static LevelT4 Level1SF<LevelT4>( LevelT4 t4 )
    {
        Map<LevelT4, LevelT1>.MapFunc<LevelT4>()
        
        ret t4
    }
}
GenClass
{
    #Level1<Level2<int,int> > GenClass_ls = new()
    #GenClass_ls_21 = Level1<string>("aaa")   #正常    
    #GenClass_ls_22 = Level1<Level2<int,int> >("aaa")    #应该报错，因为"aaa" 不是Level2<int,int> 正确的应该传 Level2<int,int>() 这样的格式
    #GenClass_ls2 = ( GenClass.GenClass_x < 3) == 4 > 3
    #ls3 = 10 ? x < 11 && x < 3 || 13 > x && 12 > x : 4
    #a = Level1b < Level2b || Level3b > Level4b
    #static GenClass_x = 100;
    #Level1<string> ls2 = null
    #Level1<string> ls3 = new()     #报错，提示，不允许这种形式
    #Level1<string> ls4 = {}
    static GenClass_fun()
    {
        #!
        Level1<Level1<int> > GenClass_fun_l1 = Level1< Level1<int> >()
        var t3 = GenClass_fun_l1.Level1Fun<Level1<string> >()
        t3.LevelMemValue = "aaaaa"
        Level1< Level1<int> > GenClass_fun_l2 = Level1<Level1<int> >()
        GenClass_fun_l2.LevelMemValue.LevelMemValue = 300

        System.Console.WriteLine("-----------------------------------" + t3.LevelMemValue  )
        System.Console.WriteLine("+++++++++++++++++++++" + GenClass_fun_l2.LevelMemValue.LevelMemValue  )
        !#

        #Level1<string>.LevelStaticValue = "2000"
        #System.Console.WriteLine("Level1<string>.LevelStaticValue = 2000->" + Level1<string>.LevelStaticValue  )

        var t4 = Level1<string>.Level1SF<int>(100)
        System.Console.WriteLine("Level1<string>.Level1SF<int>(100)->" + t4  )
    }
}

# 关于生成类的规则 
# 1. 使用T可以定义生成类里边的元素，在检索语句，或者是 其它元素调用时 会生成相关的新类

# 关于生成函数的规则 
# 关于类模板为直接生成型，在编译时，已经生成了新的类模板
# 还有一种为，在代码运行时，生成，未来JIT方式，可以在运行时，生成新类
# 模板函数 默认为不生成新的函数，直接在编译是，把模板编译进代码中，在执行时，再虚拟机中替换运行
# 如果开启了AOT模式，模板函数，即在编译时生成，这种方式 会生成多种的模板函数，如果检查到代码中包含了类模板，仍然要生成 比如 class C1<T>{ fun(){  T t = null } }仍然会认为是模板类，在后期生成，属于自己的函数体  
# 如果是类模板，但是普通 函数，则只编译一份，然后类似于继承方式，共同使用。  
# 如果是纯模板函数  则在最后生成一份属于自己的函数体
# 未来，在导出C语言的时候，函数体会有所不同。



#! 解析过程 
1. 先解析类的 名称，类别(class/enum/data), 绑定模板, 是否内部类
2. 解析模板的时候，确定该类是否注册过，非模板类，  结构是   类树结构，都为无模板模式，即使没有，有onlyRead标记，告知，只负责查找时候使用，  在该类下边，进行 模板类的查找 
3. 因为前边注册过所有的类，这时候，先解析第一批，已注册的类，通过 extendlevel排序后，再进行[成员]变量的解析,解析的同时，还会再注册一批新的 注册类
4. 后边这批都是注册的模板类，肯定是能从类列表找到的，所以类的metatype已关联
5. 在解析完上边的，剩余一批还没有解析的模板类实体类，然后再去创建该模板实体类
6. 这时候就解析完了所有的类结构，和接口的结构
!#

#! 类的查找过程
1. 可以从 import的方式查找类
2. 可以从当前类定义位置查找类
3. 查找到类后，进行模板匹配，匹配的类，是真实的类
!#