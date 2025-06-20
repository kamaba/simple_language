
# 可以在继承类重写 _index_ 方法 
# 5. Range 转成Array
# 7. value, index成为不能使用关键字
# array 如果重写Set 则是相当于 array[?] = 20;这种的写法  如果重写 __SetValue__( int index, T )   T __GetValue__( int index )  每个都有__SetValue__ 方法


public class C
{
    private int a = 20;
    get a(){ ret this.a }
    _inti_( _a )
    {
        this.a = _a;
    }
}

EList<T> extends List<T>
{
    override add( T t )
    {
        base.add(t)
    }
    override get _index_( int index )
    {
        ret base._index_(index)
    }
    override set _index_( int index, string aa )
    {
        base._index_(aa)
    }
}


IndexTest
{
    static fun()
    {  
       
        a1 = [1,2,3,4,5];    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  相当于Array<int>(5){1,2,3,4,5}
        
        va1 = a1.@1         #读取下标为1的值  = 2

        var map1 = Map<string, string>();
        map1.add("a", "map1" );
        map1.add("b", "map2" );
        mv = map1.@"a";

        EList<C> el = {};
        el.add(C(20));
        el.add(C(30));
        el.add(C(40));      #这里记住，在创建类时，只允许函数形式创建，不允许 C(40){} 这种方式  也不允许 使用 C(20).fun() 的方式调用
        ax = el.@"2".a  #使用重写_index_
    }
}
