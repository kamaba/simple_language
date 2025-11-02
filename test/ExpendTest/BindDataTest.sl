

const data BookData
{
    name = "ABC"
    pageCount = 20
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
        BookC bc = new()

        bc.name = "hahah"
        bc.width = 20               
        bc.height = 40      
    }
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

