import Std

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

public class Class1
{
    public int a = 20

    public static Class1 instance = Class1(){a = 0}
}
List0<T>
{
    List1<List2<T> > List0_list0t = null
    T List0_list0t1 = null
    List1<T> List0_list0t2 = null
    List0<int> List0_list0t3 = null
    T getA(){ ret null}

    _init_()
    {

    }

    _init_( int c )
    {

    }
}
List1<T> extends List0<T>
{
    T List1_list1t = null
    T List1_getB(){ ret null}
}
List2<T> extends List1<List0<object> >
{
    T list2t = null
    T getC(){ ret null} 
}
Map<T1,T2>
{
}

public interface IList
{
    int add(object value)
    void clear( IList<int> a )
    #interface bool contains( object value )
    #interface int indexOf( object value )
    #interface void insert( int index, object val )
    #interface void remove( object value )
    #interface void removeAt( int index )
}
public interface IList<T>
{
    #interface T getValue( int index )
    void insert( int index, T t )
}
public interface ILInstList<T> extends IList<object>
{
    void insert2( int index, T t )
}
public interface IList2<T>
{
    void T getABC()
}

public class List extends List<object> interface IList2<List<int> >    #如果出现T会报错
{
    override List<int> getABC(){ ret null }
}

public class List<T> interface IList<T>, IList
{
    List0<List1<T> > _listObj1 = null
    List<string> _listObj2 = null
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
        #this._listPtr = ListMetaClass.SetListCount( _count )
    }
    #!
    _init_( short _count = 0s, short _b1 = 0s )
    {
        #this.m_Count = _count;
        #this.m_Bound1 = _b1;
    } 
    !#   
    
    override add( T t )
    {
        var Listt_r1 = null;  #CSharp.SL.Core.MetaArrayClass.Add( this, t );
        if Listt_r1 != null
        {
        #    this.m_Count++
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
    T1 Level2_t1_t2_t21 = null
    T2 Level2_t1_t2_t22 = null

    int x = 30
    #!
    {
        get(){ ret value }
        set( int v ){ value = v }
    }
    !#
}
public class Gift
{
    _init_( int a )
    {

    }
}
public class Level1<T222> 
{
    qqq<T222>
    {
    }
    q2
    {        
    }
    enum q3
    {
        v = 0
    }
    data q4
    {

    }

    fn1()
    {

    }
    fn2(){

    }

    T222 Level1_t1 = null
    T222 Level1_t2 = null

    _init_(){

    }

    _init_( T222 it1 )
    {
        this.Level1_t1 = it1
    }

    public T222 add()
    {
        T222 Level1t_t = new()

        List0<Map<T222, T222> > Level1t_list = new(1)

        Level2<int,string> Level1t_level222 = {}

        List0<int> Level1t_lint11 = List0<int>()

        Level1t_t250 = Level1<int>.Level1t_test<List0<List1<long> > >( Level1t_t, List0<List1<long> >() )

        ret Level1t_t
    }

    static T2 Level1t_test<T2>( T222 it1, T2 it2 )
    {
        List0<Map< T2, List2<T2> > > Level1t_test_t2n = new()

        ret Level1t_test_t2n
    }
    static T222 Level1t_test<T2,T3>( T2 it2, T3 it3 )
    {
        ret null
    }

    public static T2 min<T2>( T2 t1, T2 t2 )
    {
        #r1 = t1 ? t1 < t2 && t1 > t2 : t2        
        #rx1 = if t1 < t2 { tr t1 } else{ tr t2 }
        #ret r1 
        #ret t1 ? t1 > t2 : t2
        ret t2
    }
    public TTT GetComponent<TTT>()
    {
        TTT t = new()
        ret t;
    }
}
ClassRoom
{

}

GenClass
{
    cl2<T,T2,T3> extends Level1<int>
    {

    }
    Level1<Level2<int,int> > GenClass_ls = new()
    GenClass_ls_21 = Level1<string>("aaa")   #正常
    GenClass_ls_22 = Level1<Level2<int,int> >("aaa")    #应该报错，因为"aaa" 不是Level2<int,int> 正确的应该传 Level2<int,int>() 这样的格式
    GenClass_ls2 = ( GenClass.GenClass_x < 3) == 4 > 3
    #ls3 = 10 ? x < 11 && x < 3 || 13 > x && 12 > x : 4
    #a = Level1b < Level2b || Level3b > Level4b
    static GenClass_x = 100;
    #Level1<string> ls2 = null
    #Level1<string> ls3 = new()     #报错，提示，不允许这种形式
    #Level1<string> ls4 = {}
    static GenClass_fun()
    {
        Level1<int> GenClass_fun_l1 = Level1<int>()
        GenClass_fun_l2 = GenClass_fun_l1.add()
        #Debug.Write( "Addresult: " + GenClass_fun_l2 )

        retstr = GenClass_fun_l1.GetComponent<string>()


        float32 GenClass_fun_a = Level1<float32>.min<float32>( 1.3, 2.5 )

        #Debug.Write("Flaoat" + a )
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