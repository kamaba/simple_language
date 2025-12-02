import Std
import CSharp.SimpleLanguage
import CSharp.System

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
    class Byte extends Object
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
        _init_(Int32 val )
        {
            
        }        
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
    public class MetaClass
    {
        _namespaceName = "";
        _className = "";

        get string className(){ ret this._className; }
    }
    public class Type
    {
        int _hashCode = 0
        MetaClass _metaClass = null
        public Type[] typelist = new()

        override string toString()
        {
            if( this._metaClass == null )
            {
                ret "no_meta_class"
            }

            ret this._metaClass.className;
        }
    }    
    public class Array
    {
        int _length = 0
        int type = 0;
        _index = 0;
        _current = null
        long _ptr = 0

        _init_(){
            this._listPtr = 0
        }
        _init_( int length )
        {
            uint allSize = length * 4
            #this._ptr = Lib.Array.CreateArray( length, 4 )
        }      
        bool hasNext()
        {
            this._index++;
            bool hasNext_var = this._index < this._length

            if hasNext_var
            {
                this._current = this._value[this._index];
            }
            else
            {
                this._current = null
            }
            ret hasNext

        }
        get object current()
        {
            ret this._current;
        }
        get T current<T>()
        {
            ret this._current as T;
        }
        get int index()
        {
            ret this._index;
        }
        set void index( int ind )
        {
            if( ind < 0 )
            {
                #throw error("");
                ret
            }
            if( ind >= this._length )
            {
                #throw error("超出了范围")
                ret 
            }
            this._index = ind;
            this._current = this._value[this._index];
        }
        set setValue( int index, object val )
        {
            #Lib.Array.SetArrayValue( this._ptr, 5,  index, val )
            SimpleLanguage.Lib.Array.SetArrayValueThis( this, index, val )
        }
        get object getValue( int index )
        {
            #ret Lib.Array.GetArrayValue( this._ptr, 5,  index )
            ret SimpleLanguage.Lib.Array.GetArrayValueThis( this, index )
        }
        setValues( Int64 valPtr, int len )
        {
            #Lib.Array.SetArrayValue( this._ptr, 1,  valPtr, len )
        }
        #!
        public static Array CreateInstance(Type elementType, int length);
        public static Array CreateInstance(Type elementType, int length1, int length2 );
        public static Array CreateInstance(Type elementType, int length1, int length2, int lenght3 );;
        !#
        #!
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
        !#
    }
}


ArrayTest
{
    ArrClass
    {
        int i1 = 0;
        i2 = "aaa"
    }
    Level<T>
    {
        T t = new()
    }
    static fun()
    { 
        int intvalue = 20
        #var ac = ArrClass(){ i1 = intvalue, i2 = "okok" }
        #System.Console.WriteLine("1111111111= " + ac.i1 + "    " + ac.i2 )

         # arr22 = int[2][] { [1,2,3,4] }

        a1 = object[2]{intvalue,1};    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  Array(5, int.type ){1,2,3,4,5}
        
        System.Console.WriteLine("1111111111= " + a1[0] )
        
        #var iter = a1.iter
        for v in a1 
        {
            System.Console.WriteLine("----------= " + v.toString() )
        }
        #!
        label start
        if a1._hasNext_
        {
            v = a1._next_
            then_statement
            goto start
        }
        v = null
        !#
        
        # alist = List(2){ intvalue, 1 }
        # int[] a30 = {1,2,3,4}
        # map = Map<int,string>(){ a1.$0:"al", 33:"wang" }
        # a111 = [[[1,2,3],[3,4,5],[6,7,8]],[ [10,11,12],[14,15,15],[16,17,18] ]];  # int[2][3][3] 
    
        #object[3][2][] a1 = int[3][2][]{ [ [1,2,3], [] ], [ [5], [7,8,9,5] ], [[100]] };    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  Array(5, int.type ){1,2,3,4,5}
        #还需要处理  [[100]] => 直接写100的情况，这种情况的话，需要检查 外层是否直接是array形式，如果是，则需要对应关系化处理

        #!
        for v in a1
        {
            var v2 = v.length
            if v.length > 0
            {
                var v2 = v.$0
                if( v.$0.length > 0 )
                {
                    System.Console.WriteLine("----------= " + v2.$0 )
                }
            }
        }
                
        axxx = int[3]{1,2,3}
        axxx2 = int[3]{ 3,4,5 }
        axxx3 = int[2][]{axxx,axxx2}
        axxx11 = int[2]{5,6}
        axxx12 = int[4]{ 7,8,9,5 }
        axxx13 = arry[2]{axxx11,axxx12} 
        axx22 = int[1]{100}
        axx23 = array[1]{ axx22}
        a1 = array[3]{ axxx3, axxx13, axx23 }
        !#

        #System.Console.WriteLine("1111111111= " + a1[0] )
       
        #object[][] a2 = int[2][3];
        #a2[0] = int[3]
        #a2[1] = [[1,2,3],[2],[3,4]]
        #a1._setValue_( 1, 123 )
        #aa = a1._getValue_(1)
        #System.Console.WriteLine("1111111111= " + aa )
        #! !#

        #!
        int[] a33 = {1,2,3,4};
        a33[3] = 123
        var aa333 =  a33[0];
        System.Console.WriteLine("1111111111= " + a33[3] + "-----" + a33[0] + "xxxxx=" + aa333 )
        !#

         #!
        int[2][] a335 = {[], int[3]{ 1,2,3 } };
        a33[3] = 123
        var aa333 =  a33[0];
        System.Console.WriteLine("1111111111= " + a33[3] + "-----" + a33[0] + "xxxxx=" + aa333 )
        !#

        #!
        a34 = List<int>[]{1,2,3,4}
        aa = 3
        a34.$aa = 111
        var aaaa34v = a34.$3
        System.Console.WriteLine("1111111111= " + aaaa34v )
        !#

        
        #!
        a35 = [[0,1,2,ac,4],[[11,12],[13,14]]];
        # a35[0] = [0,1,2,3,4] a35[1] = [[1,2,3],[2,3,4],[4,5,6],[7,8,9]]  a34[1][0][2] = 3  a35是个一维两值数组，访问a35[1] 是确定对象访问 再访问 是一个二维纯int数组，然后是a35[1][0][2] 后边两位是纯数组访问
        aa = 0
        #var aaaa35111 = a35.$aa.$1
        #System.Console.WriteLine("1111111111= " + aaaa35111 )
        a35.$1.$aa.$1 = 3000;  相当于  a35[1][3] = 3000        
        System.Console.WriteLine("1111111111= " + a35.$1.$aa.$1 )

        var tt1 = a35.$aa.$3
        if tt1 is ArrClass tt2 
        {
            tt2.i = 200
            var aa1111 = tt2.i;
            System.Console.WriteLine("22222222= " +aa1111 )
        }
        !#
        
        #!
        array arr = {1,2,3}
        System.Console.WriteLine("22222222= " +arr[1] )
        !#

        #array arr = Array( 10, int.type )
        #arr[2] = 100
        #System.Console.WriteLine("22222222= " +arr[2] )
        
        #levelvar = Level<int>();
        #Level<int>[][] a43 = { { levelvar, levelvar}, { levelvar, levelvar } };
        # a44 = Level<int>[]
        
        #object[][] a42 = { {1.2,1.3,1.4,1.5},{3,4,5} };    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于  
        #Array( 2, Array.type ){ Array(5, float.type ){ 1.2, 1.3, 1.4, 1.5 }, Array( 3, int.tye ){3,4,5}   } 
        #!
        a2 = Array(5, int.type ){1,2,3,4,5.0f};   #默认int List 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  先申请 int 长度为5的数组，然后把后边的数据进行填存，但这时
        #发现5.0f写入时，会提示  存在 float-> int 
        a3 = Array( 20 );               # 长度为20的List
        a4 = Array.dim( 3 ){ Array(), Array(), Array() }   # 请申一个3x1的数组 内容为null

        int[][][] a = { { {1,2,3},{1,2,3,4} }, { {1,2,3},{5,6,7,8} } }  # 
        a[1][1][1] = 12    #这种情况，需要拿到 先拿第一维的数组，然后再拿第一维中第一组，
        ArrClass[][] arrclass1 = new(10,10);
        arrClass2 = ArrClass[10][10][];
        avalue222 = a[1][1][1]
             
        
        var a4 = {1.2,1.3,1.5};    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于 Array( 3, float.type ){ 1.2, 1.3, 1.5};
        
        a5 = {"aa", 1, "232", 1.0f };  # 相当于Array( 5, object.type)( "aa", 1, "232", 1.0f, XC() );
        
        # c# 的方法  List<ArrClass2> arr2 = new ArrClass2[100]; 这里边使用的是 arr2 = ArrClass2[100];
        ArrClass2[] a6 = Array(4, ArrClass2.type );  # 数组表示使用 List<T>() new List对象 长度为4的int
        
        float[] a7 = Array(){ 1.2, 2.2, 3.4 };  #  需要{}的内容特殊处理   
        
        float[] a8 = Array( 20, float.type ){1,2,3,5,3.3};   #申请一个长度为20的数组  通过后边数据决定 其实使用的是ArrayInt
       
        #a9 = Array( 27 ).gen(3); #申请一个三维数组，边界分别为3,3,3     
        
        bb2 = Array(100, int.type ){ 1,2,3,4,5 };    # 等于 Array<int>( 100, 10 )  {1,2,3,4,5};        
             
        int[] bb3 = {1,2,3,4,5 };    #与上相同  Array<int>(5){ 1,2,3,4,5}

        #ArrClass[] arr2 = ArrClass[10]{};    #不允许 这种的写法  只允许new(10)
        #arr2 = Array();           
        #arr1.setLength( 100 );         #设置数组的长度
        #arr1[0].i = 20;
 
        int i11 = 11;
        arr1.$i11.i = 10;
         
        arr1[1] = { i = 20 };
        
        arr1[1000].i = 10000; # 在编译时，处理是否有超过长度现象，如果有的话，则编译不通过
        
        arr1.$0.i = 10;
        arr1.$"aa".i = 20;          #需要重写_index_( string s )才可以使用
        
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
        for( a in {1,2,3,4} )
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
# 3.1.6 数组不支持多维数组，只支持交错数组，如果要实现多维数组，需要用户自己实现
# 3.1.7 给数据赋值，只能使用 {} 的方法，往里边填存数据  不能使用[]的方式，该符号，只在定义数组，或者是数组取值的时候使用
