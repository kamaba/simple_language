import Std

public class Class1
{
    public int a = 20

    public static Class1 instance = Class1(){a = 0}
}
List0<T>
{

}
List1<T>
{

}
List2<T>
{

}
Map<T1,T2>
{

}

# where (T1 limit Class1), (T2 limit int, string, data)
public class Level2<T1,T2> extends Level1<List0<T2> > interface List1<List2<Map<T2,string> > >, Map<T2,string>
{
    T1 t21 = T1.instance
    T2 t22 = T2.instance
}
public class Level1<T> 
{
    qqq<T>
    {
    }
    q2
    {        
    }
    enum q3
    {

    }
    data q4
    {

    }

    fn1()
    {

    }
    fn2(){

    }

    T t1 = T.instance
    T t2 = T.instance;

    _init_( T it1 )
    {
        this.t1 = it1
    }

    public T add()
    {
        T t = T()

        List0<Map<T, T> > list = List0<Map<T, T> >()

        Level2<int,string> level222 = {}

        List0<int> lint11 = List0<int>()

        Level1.test<List0<int> >( t, lint11 ) = null

        ret t
    }

    # where (T2 limite Int32,String),
    static T2 test<T2>( T it1, T2 it2 )
    {
        T2 t2n = T2.instance

        ret t2n
    }

    public static T2 min<T2>( T2 t1, T2 t2 )
    {
        #r1 = t1 ? t1 < t2 && t1 > t2 : t2        
        #rx1 = if t1 < t2 { tr t1 } else{ tr t2 }
        #ret r1 
        #ret t1 ? t1 > t2 : t2
        ret t2
    }
}

GenClass
{
    cl2<T,T2,T3> extends Level1<int>
    {

    }
    Level1<Level2<int,int> > ls = Level1<Level2<int,int> >("aaa")
    ls2 = ( GenClass.x < 3) == 4 > 3
    #ls3 = 10 ? x < 11 && x < 3 || 13 > x && 12 > x : 4
    #a = Level1b < Level2b || Level3b > Level4b
    static x = 100;
    #Level1<string> ls2 = null
    #Level1<string> ls3 = ()     #报错，提示，不允许这种形式
    #Level1<string> ls4 = {}
    static fun()
    {
        Level1<int> l1 = Level1<int>()
        l2 = l1.add()
        Debug.Write( "Addresult: " + l2 )

        float a = Level1.min<float>( 1.3, 2.5 )

        Debug.Write("Flaoat" + a )
    }
}
#!
public class ListC<T>
{
    Array<T> m_Array = Array<T>(4);
    
    public T getIndex( int index )
    {
        if index < 0 || index > count
        {
            ret null
        }

        ret m_Array[index]
    }
}
!#

# 关于生成类的规则 
# 1. 使用T可以定义生成类里边的元素，在检索语句，或者是 其它元素调用时 会生成相关的新类

# 关于生成函数的规则 