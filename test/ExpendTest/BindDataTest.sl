

const data BookData
{
    name = "ABC"
    pageCount = 20
    price = 20
}

data BP
{
    width = 30
    height = 40
}


BookC bind BookData,BP
{

}

BindDataTest
{
    static fun()
    {
        global.println("========== BindDataTest (start) ==========")

        BookC bc = new()

        bc.name = "hahah"
        bc.width = 20               
        bc.height = 40

        global.println("[base] direct mapped fields")
        global.println(bc.name)
        global.println(bc.width)
        global.println(bc.height)

        bindNameAccessTest()
        conflictOverrideTest()
        interfaceBindBehaviorTest()

        global.println("========== BindDataTest (end) ==========")
    }

    static bindNameAccessTest()
    {
        global.println("[bindNameAccessTest] start")
        BookC bc = new()
        bc.BookData.name = "bind-name-access"
        bc.BP.width = 101
        bc.BP.height = 202

        global.println(bc.BookData.name)
        global.println(bc.name)
        global.println(bc.BP.width)
        global.println(bc.width)
        global.println("[bindNameAccessTest] end")
    }

    static conflictOverrideTest()
    {
        global.println("[conflictOverrideTest] start")
        BindConflictClass c = new()
        c.a = 77

        global.println(c.a)
        global.println(c.DA2.a)
        global.println(c.DB2.a)
        global.println("[conflictOverrideTest] end")
    }

    static interfaceBindBehaviorTest()
    {
        global.println("[interfaceBindBehaviorTest] start")
        SeltBookData d = new()
        d.price = 15
        d.count = 3
        global.println(d.calc())
        global.println("[interfaceBindBehaviorTest] end")
    }
}

data DA2
{
    a = 1
}

data DB2
{
    a = 2
}

BindConflictClass bind DA2,DB2
{
    # 冲突重写策略: 统一优先映射到 DA2.a
    get a()
    {
        ret this.DA2.a
    }

    set a( int v )
    {
        this.DA2.a = v
    }
}

public interface CalcPrice bind BookData
{
    float calc(){
        ret this.price * 1
    }
}

public class SeltBookData bind BookData interface CalcPrice
{
    public int count = 20
    override float calc(){
        ret this.price * count
    }
    #!
    这个里边，如果使用了CalePrice的接口，是bind 的类，必须在该类上，有绑定该类的关系 
    这样的话，就可以对该接口进行绑定数据的计算，例如， 书的价格是固定price 在seltBookData类中，有卖出多少本的count ，又bind了BookData
    这时，使用了CalcPrice的接口，接口有bind BookData的数据，所以SeltBookData 验证是否也绑定了BookData，发现绑定过，不会报错
    然后实现 override float calc() 方法是，可以直接使用 当然，在interface定义里边，也可以验证this.price * 1 这样的语句 
    !#
}

#!
bind 过程，相当于
BookC
{
    set name( string _name ){
        this._BookData.name = _name
    }
    get name()
    {
        ret this._BookData.name
    }
    set pageCount( int _count ){
        this._BookData.pageCount = _count
    }
    get pageCount()
    {
        ret this._BookData.pageCount
    }
    set width( int _w ){
        this._BP.width = _w
    }
    get width()
    {
        ret this._BookData.name
    }
    set height( _h ){
        this._BP.height = _h
    }
    get height()
    {
        ret this._BP.height
    }
    BookData _BookData = new()
    BP _BP = new()
}
1. 进行绑定，相当于，一个数据直接放入 _+数据名称 的一个数据 bind BookData 相当于 BookData _BookData = new() 并且自动加入了他里边的方法 set name( string aa ) get name() ...的方法
!#

