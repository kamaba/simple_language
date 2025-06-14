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

public class IList
{
    interface int add(object value)
    #interface void clear()
    #interface bool contains( object value )
    #interface int indexOf( object value )
    #interface void insert( int index, object val )
    #interface void remove( object value )
    #interface void removeAt( int index )
}
public class IList<T>
{
    #interface T getValue( int index )
    interface void insert( int index, T t )
}

public class List extends List<object>
{

}

public class List<T> interface IList<T>
{
    private Int32 _count = 0;
    #UInt16 m_Bound1 = 0;
    #UInt16 m_Bound2 = 0;

    int _index = -1;
    T _value = null

    int _listPtr = 0;

    _init_( int _count = 0 )
    {
        #this.m_Count = _count
        #this.m_Bound1 = _b1
        this._listPtr = ListMetaClass.SetListCount( _count )
    }
    #!
    _init_( short _count = 0s, short _b1 = 0s )
    {
        this.m_Count = _count;
        this.m_Bound1 = _b1;
    } 
    !#   
    
    override add( T t )
    {
        var r1 = null;  #CSharp.SL.Core.MetaArrayClass.Add( this, t );
        if r1 != null
        {
            this.m_Count++
        }
    }
    #!
    bool removeAt( int index )
    {
        byte ret1 = 1;  #CSharp.SL.Core.MetaArrayClass.RemoveIndex( this, index )
        if( ret1 == 1 )
        {
            this.m_Count--;
        }
        ret true ? ret1 == 1 : false
    }
    bool remove( T t )
    {
        CSharp.SL.Core.MetaArrayClass.Remove( this, t );
        ret false
    }
    int get index()
    {
        ret this.m_Index;
    }
    #[index]
    public T _index_( int _index )
    {
        Ptr obj = CSharp.SL.Core.MetaArrayClass.GetValue( this, _index )
        return obj.cast<T>()
    }
    #["index"]
    public T _index_( string _index )
    {
        int index = _index.tryCast<int>(-1);
        if( index != -1 )
        {
            ret this._index_( index );
        }
        ret T.default;
    }
    set index( int a )
    {
        this.m_Index = a;
    }
    get T value()
    {
        ret this.m_Value;
    }
    public void set value( T t )
    {
        this.m_Value = t;
    }
    bool contraint( T t )
    {
        ret CSharp.SL.Core.MetaArrayClass.In( this, t )
    }
    int get count()
    {
        ret CSharp.SL.Core.MetaArrayClass.Count( this )
    }
    void set count( int _c )
    {
        int arr = CSharp.SL.Core.MetaArrayClass.SetArrayCount( this, _c )
        this.m_Count = arr
    }
    !#
}

public class Level2<T1 ,T2> extends Level1<List0<T2> > interface List1<List2<Map<T2,string> > >, Map<T2,string>
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
        #Debug.Write( "Addresult: " + l2 )

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
# 关于类模板为直接生成型，在编译时，已经生成了新的类模板
# 还有一种为，在代码运行时，生成，未来JIT方式，可以在运行时，生成新类
# 模板函数 默认为不生成新的函数，直接在编译是，把模板编译进代码中，在执行时，再虚拟机中替换运行
# 如果开启了AOT模式，模板函数，即在编译时生成，这种方式 会生成多种的模板函数，如果检查到代码中包含了类模板，仍然要生成 比如 class C1<T>{ fun(){  T t = null } }仍然会认为是模板类，在后期生成，属于自己的函数体  
# 如果是类模板，但是普通 函数，则只编译一份，然后类似于继承方式，共同使用。  
# 如果是纯模板函数  则在最后生成一份属于自己的函数体
# 未来，在导出C语言的时候，函数体会有所不同。