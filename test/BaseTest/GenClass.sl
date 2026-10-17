
Map<MapT1,MapT2>
{
    MapT1 m1 = null
    MapT2 m2 = null

    static MapT3 MapFunc<MapT3>( MapT3 t3 )
    {
        ret t3
    }
}
Level1<LevelT1>
{
    static LevelT1 LevelStaticValue = null
    LevelT1 LevelMemValue = new()
    Map<LevelT1, Level1<LevelT1> > mappp22 = new()

    _init_( LevelT1 t1 )
    {
        this.LevelMemValue = t1
    }

    LevelT1 Level1Com()
    {
        this.LevelMemValue = new()

        LevelT1 lt2 = new()

        ret lt2;
    }

    LevelT3 Level1Fun<LevelT3>( )
    {
        LevelStaticValue = new()

        Map< Level1<LevelT3>, LevelT1> map22222 = new()

        this.LevelMemValue = new()

        Map<LevelT1,Level1<LevelT3> > map = new()

        LevelT3 lt2 = new()

        ret lt2
    }
    static LevelT4 Level1SF<LevelT4>( LevelT4 t4 )
    {
        rt4 = Map<LevelT4, LevelT1>.MapFunc<LevelT4>( t4 )
        
        ret rt4
    }
}
Level2<LevelT2,LevelT3>
{
    LevelT2 LevelMemValue = new()
    LevelT3 LevelMemValue2 = new()

    _init_( LevelT2 t2, LevelT3 t3 )
    {
        this.LevelMemValue = t2
        this.LevelMemValue2 = t3
    }
}
GenClass
{
    static Level1<Map<int,int> > GenClass_ls = new()
    static GenClass_ls_21 = Level1<string>("aaa")   #正常    
    #GenClass_ls_22 = Level1<Level2<int,int> >("bbb")    #应该报错，因为"aaa" 不是Level2<int,int> 正确的应该传 Level2<int,int>() 这样的格式
    GenClass_ls2 = ( GenClass.GenClass_x < 3) == 4 > 3
    #ls3 = 10 ? x < 11 && x < 3 || 13 > x && 12 > x : 4
    #a = Level1b < Level2b || Level3b > Level4b
    static GenClass_x = 100;    
    #Level1<string> ls2 = null
    #Level1<string> ls3 = new()     #报错，提示，不允许这种形式
    #Level1<string> ls4 = {}
    # [1][2][3] 静态成员：模板静态成员、无模板静态变量、静态模板成员赋值
    static testStaticMember()
    {
        global.println("====== [1] 静态模板成员 GenClass.GenClass_ls_21.LevelMemValue ======" )
        global.println("期望: aaa  实际: " + GenClass.GenClass_ls_21.LevelMemValue.toString()  )     #静态模板成员还没有生成

        global.println("====== [2] 无模板静态变量 GenClass.GenClass_x ======" )
        global.println("期望: 100  实际: " + GenClass.GenClass_x.toString()  )     #改造无模板静态变量还没有完成

        global.println("====== [3] Level1<string>.LevelStaticValue 赋值与读取 ======" )
        Level1<string>.LevelStaticValue = "2000"
        global.println("期望: 2000  实际: " + Level1<string>.LevelStaticValue.toString()  )
    }

    # [4] 嵌套模板构造 + 泛型方法 Level1Fun<Level1<string>> 调用
    static testLevel1Fun()
    {
        global.println("====== [4] Level1<Level1<int>>.Level1Fun<Level1<string>>() ======" )
        Level1<Level1<int> > GenClass_fun_l1 = Level1<Level1<int>>()
        global.println("GenClass_fun_l1.LevelMemValue 构造后默认值: " + GenClass_fun_l1.LevelMemValue.toString()  )
        var t3 = GenClass_fun_l1.Level1Fun<Level1<string> >()
        t3.LevelMemValue = "aaaaa"
        global.println("t3.LevelMemValue 期望: aaaaa  实际: " + t3.LevelMemValue.toString()  )
    }

    # [5] 嵌套成员链式赋值 Level1<Level1<int>>.LevelMemValue.LevelMemValue
    static testNestedAssign()
    {
        global.println("====== [5] 嵌套成员链式赋值 Level1<Level1<int>>.LevelMemValue.LevelMemValue ======" )
        Level1<Level1<int>> GenClass_fun_l2 = Level1<Level1<int> >()
        global.println("赋值前 GenClass_fun_l2.LevelMemValue.LevelMemValue: " + GenClass_fun_l2.LevelMemValue.LevelMemValue.toString()  )
        GenClass_fun_l2.LevelMemValue.LevelMemValue = 300
        global.println("期望: 300  实际: " + GenClass_fun_l2.LevelMemValue.LevelMemValue.toString()  )
    }

    # [6] 静态泛型方法 Level1SF<int>，内部调用 Map.MapFunc
    static testLevel1SF()
    {
        global.println("====== [6] Level1<string>.Level1SF<int>(100) ======" )
        var t4 = Level1<string>.Level1SF<int>(100)
        global.println("期望: 100  实际: " + t4.toString()  )
    }

    # [7] Map<MapT1,MapT2> 静态泛型方法 MapFunc 直接调用
    static testMapFunc()
    {
        global.println("====== [7] Map<int,string>.MapFunc<string>(\"MapFuncTest\") ======" )
        var mapRet = Map<int, string>.MapFunc<string>("MapFuncTest")
        global.println("期望: MapFuncTest  实际: " + mapRet.toString()  )
    }

    # [8] 实例成员 GenClass_ls（Level1<Map<int,int>>）访问
    static testInstanceMember()
    {
        global.println("====== [8] 实例成员 GenClass.GenClass_ls ======" )
        global.println("GenClass.GenClass_ls: " + GenClass.GenClass_ls.toString()  )
        global.println("GenClass.GenClass_ls.LevelMemValue: " + GenClass.GenClass_ls.LevelMemValue.toString()  )
    }

    # [9] Level1<string> 实例方法 Level1Com 调用前后对比
    static testLevel1Com()
    {
        global.println("====== [9] Level1<string> 实例方法 Level1Com ======" )
        Level1<string> comObj = Level1<string>("comInit")
        global.println("Level1Com 前 LevelMemValue 期望: comInit  实际: " + comObj.LevelMemValue.toString()  )
        comObj.Level1Com()
        global.println("Level1Com 后 LevelMemValue（new() 默认值）: " + comObj.LevelMemValue.toString()  )
    }

    # [10] Level1<Level1<int>> 成员 mappp22（Map<LevelT1, Level1<LevelT1>>）访问
    static testMappp22()
    {
        global.println("====== [10] Level1<Level1<int>>.mappp22 成员访问 ======" )
        Level1<Level1<int> > l1 = Level1<Level1<int> >()
        global.println("l1.mappp22: " + l1.mappp22.toString()  )
    }

    # [11] Level2<T2,T3> 双模板参数构造与成员访问
    static testLevel2()
    {
        global.println("====== [11] Level2<int,string> 构造与成员访问 ======" )
        Level2<int, string> l2 = Level2<int, string>(100, "l2str")
        global.println("LevelMemValue 期望: 100  实际: " + l2.LevelMemValue.toString()  )
        global.println("LevelMemValue2 期望: l2str  实际: " + l2.LevelMemValue2.toString()  )

        # 修改成员后再读
        l2.LevelMemValue = 200
        l2.LevelMemValue2 = "changed"
        global.println("修改后 LevelMemValue 期望: 200  实际: " + l2.LevelMemValue.toString()  )
        global.println("修改后 LevelMemValue2 期望: changed  实际: " + l2.LevelMemValue2.toString()  )
    }

    # [12] Map 成员 m1/m2 读写
    static testMapMember()
    {
        global.println("====== [12] Map<int,string> 成员 m1/m2 读写 ======" )
        Map<int, string> mm = Map<int, string>()
        mm.m1 = 100;
        mm.m2 = "mapm1"
        global.println("默认 m1: " + mm.m1.toString()  )
        global.println("默认 m2: " + mm.m2.toString()   )
        mm.m1 = 10
        mm.m2 = "mapm2"
        global.println("m1 期望: 10  实际: " + mm.m1.toString()   )
        global.println("m2 期望: mapm2  实际: " + mm.m2.toString()   )
    }

    # [13] Level1Fun 返回值直接使用（不经过中间变量）
    static testLevel1FunDirect()
    {
        global.println("====== [13] Level1Fun<Level1<string>> 返回值直接链式访问 ======" )
        Level1<Level1<int>> dl1 = Level1<Level1<int>>()
        # 直接对返回值赋值并读取
        # 注意: Level1Fun 每次调用返回新对象，必须用变量保存返回值后再操作
        var dl1Ret = dl1.Level1Fun<Level1<string>>()
        dl1Ret.LevelMemValue = "direct"
        global.println("直接返回值.LevelMemValue 期望: direct  实际: " + dl1Ret.LevelMemValue.toString()  )
    }

    # [14] 静态泛型方法 Level1SF 多次不同模板参数调用
    static testLevel1SFMulti()
    {
        global.println("====== [14] Level1<string>.Level1SF 多模板参数 ======" )
        var r1 = Level1<string>.Level1SF<int>(100)
        global.println("Level1SF<int>(100) 期望: 100  实际: " + r1.toString()  )
        var r2 = Level1<string>.Level1SF<string>("sfstr")
        global.println("Level1SF<string>(sfstr) 期望: sfstr  实际: " + r2.toString() )
    }

    # [15] 错误场景：类型不匹配赋值（应报错或运行时异常）
    static testErrorTypeMismatch()
    {
        global.println("====== [15] 错误场景：类型不匹配 ======" )
        # 期望：给 Level1<int> 传入 string 应报错
        #Level1<int> err1 = Level1<int>("notInt")     # 错误：构造参数类型不匹配
        #global.println("err1.LevelMemValue: " + err1.LevelMemValue.toString()  )

        # 期望：给 int 成员赋值 string 应报错
        Level1<int> err2 = Level1<int>(10)
        #err2.LevelMemValue = "shouldBeInt"           # 错误：成员类型不匹配
        global.println("err2.LevelMemValue 期望: 10  实际: " + err2.LevelMemValue.toString()  )

        # 期望：给 string 静态成员赋值 int 应报错
        #Level1<string>.LevelStaticValue = 999        # 错误：静态成员类型不匹配
        global.println("Level1<string>.LevelStaticValue 当前值: " + Level1<string>.LevelStaticValue.toString()  )
    }

    # [16] 错误场景：MapFunc 模板参数与实参类型不一致（应报错）
    static testErrorMapFunc()
    {
        global.println("====== [16] 错误场景：MapFunc 模板与实参不一致 ======" )
        # 期望：MapFunc<string> 传入 int 应报错
        #var err = Map<int, string>.MapFunc<string>(100)   # 错误：参数应为 string
        #global.println("err: " + err.toString()  )

        # 正确用法对照
        var ok = Map<int, string>.MapFunc<string>("ok")
        global.println("正确用法 期望: ok  实际: " + ok.toString()  )
    }

    # [17] 错误场景：跨模板实例静态成员混用（应报错或类型校验失败）
    static testErrorCrossStatic()
    {
        global.println("====== [17] 错误场景：跨模板实例静态成员 ======" )
        Level1<int>.LevelStaticValue = 100
        global.println("Level1<int>.LevelStaticValue: " + Level1<int>.LevelStaticValue  )
        # 期望：Level1<string>.LevelStaticValue 不应被 int 赋值影响
        global.println("Level1<string>.LevelStaticValue: " + Level1<string>.LevelStaticValue  )
        # 期望：不同模板实例静态成员相互独立
        #Level1<int>.LevelStaticValue = "strTo"          # 错误：类型不匹配
    }

    # [18] 边界场景：null 赋值与读取
    static testNullAssign()
    {
        global.println("====== [18] null 赋值与读取 ======" )
        Level1<string> nl = Level1<string>("init")
        global.println("初始 LevelMemValue: " + nl.LevelMemValue?.toString()  )
        nl.LevelMemValue = null
        global.println("null 赋值后 LevelMemValue: " + nl.LevelMemValue?.toString()  )

        # Map 成员置 null
        Map<int, string> nlm = Map<int, string>()
        nlm.m1 = 1
        nlm.m2 = "v"
        global.println("赋值后 m1: " + nlm.m1 + "  m2: " + nlm.m2?.toString()  )
        nlm.m1 = null
        nlm.m2 = null
        global.println("置 null 后 m1: " + nlm.m1 + "  m2: " + nlm.m2?.toString()  )
    }

    # [19] 边界场景：Level1Fun 内部对 LevelStaticValue 的副作用
    static testLevel1FunSideEffect()
    {
        global.println("====== [19] Level1Fun 对 LevelStaticValue 的副作用 ======" )
        # Level1Fun 内部执行 LevelStaticValue = new()
        Level1<int>.LevelStaticValue = 12345
        global.println("调用前 Level1<int>.LevelStaticValue: " + Level1<int>.LevelStaticValue?.toString()  )
        Level1<Level1<int> > se = Level1<Level1<int> >()
        se.Level1Fun<Level1<string> >()
        global.println("调用后 Level1<int>.LevelStaticValue（可能被 new() 覆盖）: " + Level1<int>.LevelStaticValue?.toString()  )
    }

    # [20] 边界场景：Level1Com 重复调用
    static testLevel1ComRepeat()
    {
        global.println("====== [20] Level1Com 重复调用 ======" )
        Level1<string> rc = Level1<string>("first")
        global.println("第1次 LevelMemValue: " + rc.LevelMemValue?.toString()  )
        rc.Level1Com()
        global.println("Level1Com 后 LevelMemValue: " + rc.LevelMemValue?.toString()  )
        rc.Level1Com()
        global.println("再次 Level1Com 后 LevelMemValue: " + rc.LevelMemValue?.toString()  )
    }

    static fun()
    {
        GenClass.testStaticMember()
        GenClass.testLevel1Fun()
        GenClass.testNestedAssign()
        GenClass.testLevel1SF()
        GenClass.testMapFunc()
        GenClass.testInstanceMember()
        GenClass.testLevel1Com()
        GenClass.testMappp22()
        GenClass.testLevel2()
        GenClass.testMapMember()
        GenClass.testLevel1FunDirect()
        GenClass.testLevel1SFMulti()
        GenClass.testErrorTypeMismatch()
        GenClass.testErrorMapFunc()
        GenClass.testErrorCrossStatic()
        GenClass.testNullAssign()
        GenClass.testLevel1FunSideEffect()
        GenClass.testLevel1ComRepeat()
        global.println("====== 测试结束 ======" )
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

# GenClass static fun 测试面向：Map<,> 辅助、Level1<T> 静态/实例成员、嵌套 Level1<Level1<int>> 与 Level1Fun/Level1SF 泛型方法。
# 预期：Console 中含 LevelMemValue、LevelStaticValue、内层 300 等；含「静态模板成员尚未生成」类已知限制的打印。
