
import Std

namespace GC3{

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

    override string toString()
    {
        ret "  Level1_t1=" + this.Level1_t1.toString() + "  Level1_t2=" + this.Level1_t2.toString()
    }
}
interface Interface1<IT1>
{
    IT1 add( IT1 t )
}
Level2<LT21, LT22, LT23> extends Level1<LT23,LT22 > interface Interface1<LT23>
{
    LT22 Level21_t = new()

    override LT23 add( LT23 tttt )
    {
        LT23 llevel11sx = new()

        ret tttt
    }

    static LT23 _test = new()
    static LT23 getTest( LT23 lt23 )
    {
        _test = lt23

        ret _test
    }
    override string toString()
    {
        ret "Level21_t=" + this.Level21_t.toString() + "\n"  + base.toString()
    }
}
Level3<LT31, LT32> extends Level2<LT32, LT32, LT31>
{
    LT31 Level3_t = new()
    _init_( LT31 lt31 )
    {
        this.Level3_t = lt31
    }
    override LT31 add( LT31 tttt )
    {
        ret tttt
    }
    override string toString()
    {
        ret "Level3_t=" + this.Level3_t.toString() + "\n" + base.toString()
    }
}

Level4<LT41,LT42> extends Level3<LT42,LT41>
{
    override LT42 add( LT42 tttt )
    {
        ret tttt
    }

    override string toString()
    {
        ret "Level4" + "\n" +  base.toString()
    }
}

GenClass3{
    static fun()
    {
        # 已知限制: Level4<string,int> 的 4 层泛型继承链 (Level4→Level3→Level2→Level1)
        # VM 在构造对象时字段布局不正确，导致 IndexOutOfRangeException
        
        GenClass3.testConstruct()
        GenClass3.testAdd()
        GenClass3.testGetTest()
        GenClass3.testFields()
        
        global.println("====== [11] testTemplate ======" )
        GenClass3.testTemplate<int,int>(300)
        
        global.println("====== [12] testTemplate ======" )
        GenClass3.testTemplate<float,int>(123.45f)

        global.println("====== [13] testTemplate ======" )
        GenClass3.testTemplate<string,string>("string___string")
        #global.println("====== GenClass3: 已知 VM 限制，4层泛型继承链构造未支持 ======" )
    }
    static testConstruct()
    {
        global.println("====== [1] Level4<string,int> 构造与 Level3_t ======" )
        Level4<string,int> llll3333 = new(300)
        global.println("llll3333 期望: 300  实际: " + llll3333.Level3_t.toString() )
        Level4<int,string> llll4444 = new("stringtest")
        global.println("llll4444 期望: stringtest  实际: " + llll4444.Level3_t.toString() )
    }
    static testAdd()
    {
        global.println("====== [2] add 方法返回值 ======" )
        Level4<string,int> llll3333 = new(300)
        # add 签名为 add(LT23 tttt)，Level4<string,int> 中 LT23=int，传 int 1000 类型匹配
        addval = llll3333.add(1000)
        global.println("add(1000) 返回值 期望: 1000  实际: " + addval.toString() )
    }
    static testGetTest()
    {
        global.println("====== [3] Level2<string,int,int> 静态 getTest ======" )
        addval2 = Level2<string,int,int>.getTest( 2000 )
        global.println("getTest(2000) 返回值 期望: 2000  实际: " + addval2.toString() )
    }
    static testFields()
    {
        global.println("====== [4] 字段赋值与读取 ======" )
        Level4<int,int> llll3333 = new(300)
        # Level3_t (直接父类字段) 赋值正常
        llll3333.Level3_t = 999
        global.println("Level3_t 期望: 999  实际: " + llll3333.Level3_t.toString() )
        # 已知限制: 深层继承的泛型字段 (Level1_t2, Level21_t) 的 store 路径
        # callMetaType 为 null 时字段索引解析为 -1，读取正常
        llll3333.Level1_t2 = 888
        llll3333.Level21_t = 777
        global.println("Level1_t2 读取 期望: 888 (默认值)  实际: " + llll3333.Level1_t2?.toString() )
        global.println("Level21_t 读取 期望: 777 (默认值)  实际: " + llll3333.Level21_t?.toString() )
    }
    static testTemplate<TT1,TT2>( TT1 t1 )
    {
        global.println("====== [5] 模板类 ======" )
        Level4<TT2,TT1> llll3333 = new(t1)
        global.println("llll3333 模板类 输出: " + llll3333.toString() )
    }
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

# GenClass3 static fun 测试面向：LT 占位类型、多参数模板继承（Level4→Level3→Level2）、接口 Interface1 实现与 static getTest。
# 预期：Level4<string,int> 经 new(300) 与 add/getTest 赋值后，多行 Console 输出与字段一致；依赖 LT 与模板替换实现。
