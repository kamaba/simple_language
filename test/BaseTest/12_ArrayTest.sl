
import CSharp.SimpleLanguage.Core.SelfMeta;


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

public class List<T> interface IList, IList<T>
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
    
    override int add( object obj )
    {

    }
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
ArrayTest
{
    ArrClass
    {
        int i = 0;
    }
    static fun()
    {  
        a1 = [1,2,3,4,5];    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  相当于List<int>(5){1,2,3,4,5}
        var a42 = [[1.2,1.3,1.5],[3,4,5]];    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于 List<List<float>>{ {1.2, 1.3, 1.5}, {3,4,5} };
        #!
        a2 = List(5){1,2,3,4,5.0f};   #默认int List 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  List<int>(5){1,2,3,4,5}        
        a3 = List( 20 );               # 长度为20的List
             
        
        var a4 = [1.2,1.3,1.5];    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于 List<float>{ 1.2, 1.3, 1.5};
        
        a5 = ["aa", 1, "232", 1.0f];  # 相当于List<Object>( "aa", 1, "232", 1.0f, XC() );
        
        # c# 的方法  List<ArrClass2> arr2 = new ArrClass2[100]; 这里边使用的是 arr2 = ArrClass2[100];
        int[] a6 = List<Int32>(4);  # 数组表示使用 List<T>() new List对象 长度为4的int
        
        float[] a7 = List<float>(){ 1.2, 2.2, 3.4 };  #  需要{}的内容特殊处理   
        
        List<float> a8 = List( 20 ){1,2,3,5,3.3};   #申请一个长度为20的数组
       
        a9 = List( 27 ).gen(3); #申请一个三维数组，边界分别为3,3,3     
        
        bb2 = List<int>(100){ 1,2,3,4,5 };    # 等于 Array<int>( 100, 10 )  {1,2,3,4,5};        
             
        int[] bb3 = [1,2,3,4,5 ];    #与上相同  Array<int>(5){ 1,2,3,4,5}
        
        arr1 = List<ArrClass>();     #申请一个该类型的数组对象，但长度为0

        #ArrClass[] arr2 = ArrClass[10]{};    #不允许 这种的写法
        #arr2 = List<ArrClass>();           
        #arr1.setLength( 100 );         #设置数组的长度
        #arr1[0].i = 20;
 
        int i11 = 11;
        arr1.$i11.i = 10;
         
        arr1[1] = { i = 20 };
        
        arr1[1000].i = 10000; # 在编译时，处理是否有超过长度现象，如果有的话，则编译不通过
        
        arr1.add( ArrClass() );  #增加数据+1 
        
        arr1.removeIndex( 2 );       #删除数据-1        
        
        arr1.remove( arr1[20] );     #删除数据-1 
        arr1.@0.i = 10;
        arr1.@"aa".i = 20;          #需要重写_index_( string s )才可以使用
        
        arr1.index = 2;     #数组的当前游标
        arr1.value.i = 10;   #数组当前游标的植
        for a in arr1      #使用for 的 a 是封装过的it里边包含 Index() 也可以直接a = ArrClass();替代里边的值
        {
            if a.index == 20   #系统自带Index()函数  如果在使用for 时，则object.Index()表示他的下标
            {
                a.value = ArrClass(){ i = 100 }
                continue
            }
            a.i = 200
        }
        for( a in [1,2,3,4] )
        {
            i = a.index + 1
        }
        for a in [1..5]                 #自己的迭代器
        {
            i = a.index + 1
        }
        for i = 0, i < arr1.count()
        {
            i++
            if i < 40
            {
                continue;
            }

            arr1[i] = ArrClass();
            arr1[i].i = 100;
            arr1.$i.i = 100;

            i+=2;
        }
        !#

        #Array 继承集合接口 Collection 可以使用  a in Collection的遍历
    }
}
# 3.1.1 先实现了，在函数里，直接调用C#层写的方法。
# 5. Range 转成List
# 7. value, index成为不能使用关键字
# list 如果重写Set 则是相当于 array[?] = 20;这种的写法  如果重写 _setValue__( int index, T t )   T _getValue_( int index )  每个都有__SetValue__ 方法
