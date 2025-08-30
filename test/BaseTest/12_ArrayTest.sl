
import CSharp.SimpleLanguage.Core.SelfMeta;


public class Type
{
    public int length = 4
}

public class Array
{
    public int length = 0
    public int rank = 1

    int _listPtr = 0
    _init_( uint length )
    {
        uint allSize = length * 4

        this._listPtr = ArrayMetaClass.SetArrayLength( allSize )

    }
    _init_( uint length, Type type )
    {        
        uint allSize = length * type.length
        this._listPtr = ArrayMetaClass.SetArrayLength( allSize )
    }
    _init_( uint length, Type type, int rank )
    {
        uint unitLength = type.length
        this.length = length
        this.rank = rank
        uint allSize = length * type.length

        this._listPtr = ArrayMetaClass.SetArrayLength( allSize )
    }
}

ArrayTest
{
    ArrClass
    {
        int i = 0;
    }
    static fun()
    {  
        a1 = [1,2,3,4,5];    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  Array(5, int.type ){1,2,3,4,5}
        #var a42 = [[1.2,1.3,1.4,1.5],[3,4,5]];    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于  
        #Array( 2, Array.type ){ Array(5, float.type ){ 1.2, 1.3, 1.4, 1.5 }, Array( 3, int.tye ){3,4,5}   } 
        #!
        a2 = Array(5, int.type ){1,2,3,4,5.0f};   #默认int List 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  先申请 int 长度为5的数组，然后把后边的数据进行填存，但这时
        #发现5.0f写入时，会提示  存在 float-> int 
        a3 = Array( 20 );               # 长度为20的List
        a4 = Array.dim( 3 ){ Array(), Array(), Array() }   # 请申一个3x1的数组 内容为null
             
        
        var a4 = [1.2,1.3,1.5];    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于 List<float>{ 1.2, 1.3, 1.5};
        
        a5 = ["aa", 1, "232", 1.0f];  # 相当于List<Object>( "aa", 1, "232", 1.0f, XC() );
        
        # c# 的方法  List<ArrClass2> arr2 = new ArrClass2[100]; 这里边使用的是 arr2 = ArrClass2[100];
        int[] a6 = Array(4, ArrClass2.type );  # 数组表示使用 List<T>() new List对象 长度为4的int
        
        float[] a7 = Array(){ 1.2, 2.2, 3.4 };  #  需要{}的内容特殊处理   
        
        float[] a8 = Array( 20 ){1,2,3,5,3.3};   #申请一个长度为20的数组  通过后边数据决定 其实使用的是ArrayInt
       
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
        
        arr1.removeAt( 2 );       #删除数据-1        
        
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
# 3.1.2 数据创建 两种方式 一种为  Array( 个数, 类型 )  如果不传类型， 默认为object 即 对象类型，在访问的时候，是个指针引用  个数必须是 uint型 当uint=0时，只创建数据对象，不创建数组
# 3.1.3 创建Array() 时，默认为1
# 3.1.4 Array 没有具体的Add方法，只有 Copy
# 3.1.5 Array(){ Array(){ Array(){} } } 可申请多维数组，多维数组时，必须有数量    Array(5){ Array(2){   Array(10){}, Array(12){}  } }   即为一个 5x2x12的三维数据
