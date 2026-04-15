import Std
import CSharp.SimpleLanguage
import CSharp.System

ArrayTest
{
    ArrClass
    {
        int i1 = 0;
        i2 = "aaa"

        override string toString()
        {
            ret "ArrClass(){ i1= " + this.i1.toString() + "  i2= " + this.i2 + "}"
        }
    }
    Level<T>
    {
        T t = new()
        _init_( obj )
        {
            this.t = obj as T
        }
        override string toString()
        {
            ret this.t.toString()
        }
    }
    static testArray( arr )
    {
        var iter = arr as Array<Object>
        #iter = arr as IIterable<Object>
        #iter = arr as IIterable
        if iter != null
        {
            for v in iter
            {
                if v != null
                {
                 System.Console.WriteLine("1111111111= " + v.toString() )
                }
            }
        }
    }
    static fun()
    { 
        int intvalue = 20
        var ac = ArrClass(){ i1 = intvalue, i2 = "okok" }
        #System.Console.WriteLine("1111111111= " + ac.i1 + "    " + ac.i2 )

         # arr22 = int[2][] { [1,2,3,4] }
        #li = Level<int>(100)
        #a1 = Level<int>[5]{ Level<int>(3), null, Level<int>(4), li }
        #a1 = [101,102,null,104]

        #a1 = object[4]{intvalue,null,3 };    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  Array(5, int.type ){1,2,3,4,5}
        
        #System.Console.WriteLine("1111111111= " + a1[1] )
        
        #ax = [ Class2[3], Class2(2,10){ qx= 100, y = [1,2,3,4] } ]

        #int[] a1 = {1,2,4,5}
        #a1 = [[[1,2,3],[3,4,5],[6,7,8]],[ [10,11,12],[14,15,15],[16,17,18] ] ]; 
        #int[2][3][] a1 = {[[1,2,3],[3,4,5],[6,7,8]],[ [10,11,12],[14,15,15],[16,17,18] ]};  # int[2][3][3]     
        #object[3][2][] a1 = int[3][2][]{ [ [1,2,3], [] ], [ [5], [7,8,9,5] ], [[100]] };    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  Array(5, int.type ){1,2,3,4,5}
        #还需要处理  [[100]] => 直接写100的情况，这种情况的话，需要检查 外层是否直接是array形式，如果是，则需要对应关系化处理
        #!
        Int32[] a2 = Array<Int32>.createInstance(intvalue)        
        a2[1] = 50
        a2.$1 += 100    
        a2[1] = a2.$1 + 200
        a2[1]--
        System.Console.WriteLine("1111111111= " + a2[1] )
        
        System.Console.WriteLine("1111111111= " + intvalue )
        intvalue = 40
        intvalue -= 13
        System.Console.WriteLine("1111111111= " + intvalue )
        for i = 0, i < a2.length, i++
        {
            if i > 0 
            {
                a2[i] = a2[i-1] + 100  #$()访问变量  a2[i-1] 都还没有做
            }
            else
            {
                a2[0] = 123
            }
            System.Console.WriteLine("1111111111= " + a2.$i )
        }
        !#
        #a2[2] += 300    
        #System.Console.WriteLine("1111111111= " + a2.$2 )
        
        #axxx = int[3]{1,2,3}
        #int[] axxx2 = new(4){ null,4,5 }
        #axxx3 = int[2][]{axxx,axxx2}
        
        #!
        object[] axxx11 = int[2]
        axxx11[0] = 5
        axxx11[1] = 6

        
        for v in axxx11
        {
            if v != null{
                Console.print("level2---------value2: = " + v.toString() )
            }
        }
        !#

        #!
        for v in [1000,2000,3000,1005]
        {
            if v != null {
                System.Console.WriteLine("level2---------value2: = " + v.toString() )
            }
        } 
        !#

        #Array arr = Array(2){ 111, "222" } 如果使用了typedef Array = Array<object> 会进行类似于宏的替换 会变成Array<Object> arr = A
        #需要把数组的类型的逆变也计算出来，然后确定是否正确
        #testArray( [arr,"10001", 3000] )
        
        #!
        int[] aaaxx12 = Array<int>.createInstance(2)
        aaaxx12[0] = 5
        aaaxx12[1] = 6    
        axxx12 = [ 7,8,9,5 ]
        #axxx13 = Array<Array<int> >(2) { aaaxx12, [1,2,3,4] } 
        Array<Array<Object> > axxx13 = object[2][] { aaaxx12, [991,992,993,994] } 
         
        #testArray( [101,102] ) 
        #testArray( axxx13 )

        axx22 = int[1]{100}
        axx23 = object[1]{ axx22}
        a1 = Array<Object>(3){ 1, axxx13, axx23 }       
        testArray(a1)
        
        for v in a1
        {
            if v != null{
                System.Console.WriteLine("level2=================value2: = " + v.toString() )
            }
        }
        
        #!
        for v in a1
        {
            if v != null
            {         
                for v2 in v
                {
                    System.Console.WriteLine("level2---------value2: = " + v2.toString() )
                    for i = 0, i < v2.length, i++
                    {
                        System.Console.WriteLine("level3---------value3 :==" + v2[i].toString() )
                    }
                }
                #System.Console.WriteLine("------------value: " + v.toString() )
            }
            else
            {                
                System.Console.WriteLine("============index: " + v )
            }
        }
        !#


        #System.Console.WriteLine("1111111111= " + a1[0] )
       
        #!
        object[][] a2 = int[2][4];
        a2[0] = int[4]       #通过传入的int[]类型决定 是否可以new
        a2[1] = Array<int>(10)
        a2[0][0] = 999
        a2[0].$1 = 998
        a2.$0.$2 = 997
        a2.$0.$3 = 996
        a2[1] = [1,100,1000]
        a2[1].setValue( 0, 2222 );
        testArray(a2)
        !#
        
        #!
        int[] a33 = {1,2,3,4};
        a33[3] = 123
        var aa333 =  a33[0];
        System.Console.WriteLine("1111111111= " + a33[3] + "-----" + a33[0] + "xxxxx=" + aa333 )
        !#

        #!
        int[4][] a335 = {[], int[3]{ 871,872,873 }, int[20] };
        a335[2][1] = 123
        var aa333 =  a335[0];
        System.Console.WriteLine("1111111111= " + a335[2].toString() + "-----" + a335[0].toString() );# + "xxxxx=" + a335 )
        #testArray(a335)
        !# 
        #!
        a35 = [[0,1,2,ac,4],[[11,12],[13,14]]];
        testArray(a35)
        !#      

        #!            
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
        levelvar = Level<int>(100);
        Level<int>[][] a35 = { [ levelvar, levelvar ], [ levelvar, levelvar ] };        
        testArray(a35)
        !#
        
        #!
        strarr = string[]{"abbc", "cccc", "a100"}
        testArray(strarr)
        

        #a44 = Level<int>[10]
        Level<int>[] a44 = new(15) { Level<int>(200) }
        a44[1] = Level<int>(10000)
        int xxx = -2
        a44[(xxx*2+5)].t = 100
        #a44[1].t = 100
        
        for i = 4, i < 8, i++
        {
            a44[i] = Level<int>( i * 10000 )
            a44[i].t += 135 
        }
        
        testArray(a44)
        !#

        
        object[][] a42 = { [1.2,1.3,1.4,1.5],[3,4,5] };    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于  
        
        testArray(a42)
        #Array( 2, Array.type ){ Array(5, float.type ){ 1.2, 1.3, 1.4, 1.5 }, Array( 3, int.tye ){3,4,5}   } 
        #!
        a2 = Array<int>(5){1,2,3,4,5.0f};   #默认int List 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  先申请 int 长度为5的数组，然后把后边的数据进行填存，但这时
        #发现5.0f写入时，会提示  存在 float-> int 
        a3 = Array( 20 );               # 长度为20的 Array<object>(20)
        a4 = Array( 3 ){ Array(0), Array(0), Array(0) }   # 请申一个3x1的数组 内容为null

        int[][][] a = { { {1,2,3},{1,2,3,4} }, { {1,2,3},{5,6,7,8} } }  # 
        a[1][1][1] = 12    #这种情况，需要拿到 先拿第一维的数组，然后再拿第一维中第一组，
        ArrClass[][] arrclass1 = new(10,10);
        arrClass2 = ArrClass[10][10][];
        avalue222 = a[1][1][1]             
        
        var a4 = {1.2,1.3,1.5};    #如果使用{}的形式，必须在前边声明类型，才可以使用 通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于 Array( 3, float.type ){ 1.2, 1.3, 1.5};  
        a5 = object[]{"aa", 1, "232", 1.0f };  # 相当于Array( 5, object.type)( "aa", 1, "232", 1.0f, XC() );
        
        # c# 的方法  List<ArrClass2> arr2 = new ArrClass2[100]; 这里边使用的是 arr2 = ArrClass2[100];
        ArrClass2[] a6 = Array(4, ArrClass2.type );  # 数组表示使用 List<T>() new List对象 长度为4的int
        
        float[] a7 = Array<float>(){ 1.2, 2.2, 3.4 };  #  需要{}的内容特殊处理   
        
        float[] a8 = Array<float>( 20 ){1,2,3,5,3.3};   #申请一个长度为20的数组  通过后边数据决定 其实使用的是ArrayInt
                            
        #ArrClass[] arr2 = ArrClass[10]{};    #不允许 这种的写法  只允许new(10)
        #arr2 = Array(0);           
        #arr1.setLength( 100 );         #设置数组的长度
        #arr1[0].i = 20;
 
        int i11 = 11;
        arr1.$i11.i = 10;
         
        arr1[1] = { i = 20 };
        
        arr1[1000].i = 10000; # 在编译时，处理是否有超过长度现象，如果有的话，则编译不通过
        
        arr1.$0.i = 10;
        arr1.$"aa".i = 20;          #需要重写_index_( string s )才可以使用
        
        arr1.index = 2;     #数组的当前游标
        arr1.current.i = 10;   #数组当前游标的植
        for a in arr1      #使用for 的 a 是封装过的it里边包含 Index() 也可以直接a = ArrClass();替代里边的值
        {
            if a.index == 20   #系统自带Index()函数  如果在使用for 时，则object.Index()表示他的下标
            {
                a.current = ArrClass(){ i = 100 }
                continue
            }
            a.i = 200
        }
        for( a in [1,2,3,4] )
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

            i += 2
        }
        !#

        #Array 继承集合接口 Collection 可以使用  a in Collection的遍历
    }
}
# 3.1.1 先实现了，在函数里，直接调用C#层写的方法。

#!
1. 使用Array为数组的关键字，通过模板T实例化，生成一个数组
2. 生成数组的方式有Array<int> Array<object> Array<Array<int> > 传统这种的方式,也可以使用 int[] object[], string[][][] 这种方式生成数组
3. 生成数组还可以使用直接赋值的方式 比如 val = [1,2,3,4]  这种情况，会自动计算数组的初始长度
4. 如果定义了前边的类型，可以直接使用{}的方式 比如 int[] val = {1,2,3,4}, 这种情况，会自动计算数组的初始长度
5. 如果使用了生成函数方式 比如  int[] val = int[5]{1,2,3,4} 当然也可以省略掉前边的 val = int[5]{1,2,3} 在使用函数生成时，必须要给数组的最后一维增加长度
6. 数组可以进行协变，比如 object[][] val = int[20][] 这种的子类向父类协变
7. 数组如果继承了IIterator, IIterable, 相关内容后，即可进行for的遍历
8. 数组的访问，可以通过 val[1][2] 这种方式访问，也可以使用 val.$1.$2 这种方式访问，$1 = [1] 是相同的，语法上，没有差别


1. 在使用迭代器时，需要先new一个iterateVariable 对象
2. 然后把IIterable放到本地的_iterable节点中
3. 
!#

# ArrayTest static fun 测试面向：Array<T> 构造、下标与 `$index` 访问、index/current 游标、多种 for-in/for 形式。
# 预期：与集合/IR 实现强相关；已去除 `ui6yh` 类垃圾 token 以免词法错误；其余行为以运行时为准做回归。
