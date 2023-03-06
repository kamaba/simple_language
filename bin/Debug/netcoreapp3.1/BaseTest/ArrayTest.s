
Range<T>
{
    __Init__( T _start, T _end, T _step )
    {
        this.m_Start = _start;
        this.m_End = _end;
        this.m_Step = _step;
    }
}

Array<T>
{
    uint32 m_Count = 0;
    uint16 m_Bound1 = 0;
    uint16 m_Bound2 = 0;

    __Init__( int _count = 0, int _b1 = 0i )
    {
        this.m_Count = _count;
        this.m_Bound1 = _b1;
    }
    __Init__( short _count = 0s, short _b1 = 0s )
    {
        this.m_Count = _count;
        this.m_Bound1 = _b1;
        this.m_Bound2 = _b2;
    }    
    
    __Cast__( Type t )
    {
        if( t == Int16.type )
        {
            #ret Convert.Int32ConvertToInt16( m_Value )
            ret 10s;
        }
    }
    Add( T t )
    {
        #m_Count += 1;
    }
    RemoveIndex( int index )
    {

    }
    #!
    int get count()
    {
        return m_Count;
    }
    T First()
    {
        return null;
    }
    bool In( T t )
    {
        ret false;
    }
    !#
}
ArrayTest
{
    ArrClass
    {
        int i = 0;
    }
    static Range()
    {
        CSharp.System.Console.Write("aaa");
        r1 = 1..100;    #快速int range  以后再支持 1..n   n..200的方式    相当于   range( 1, 100, 1 )的调用        
        r2 = range( 1.0f, 200.0f, 1.0f );
        Range<double> r3 = (3.2d, 54.3d, 0.22d );
        r4 = Range<short>( 1s, 100s, 2s );
    }
    static Fun()
    {   #!
        a1 = [1,2,3,4,5];    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  相当于Array<int>(5){1,2,3,4,5}
        a2 = Array(5){1,2,3,4,5.0f};   #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  相当于Array<int>(5){1,2,3,4,5}        
        a3 = array( 20 );               # 长度为20的Array
             
        
        int[] a4 = [1.2,1.3,1.5];    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于 Array<float>{ 1.2, 1.3, 1.5};
        
        a5 = ["aa", 1, "232", 1.0f];  # 相当于Array<Object>( "aa", 1, "232", 1.0f, XC() );
        
        # c# 的方法  ArrClass[] arr2 = new ArrClass2[100]; 这里边使用的是 arr2 = ArrClass2[100];
        int[] a6 = Array<Int32>(4);  # 数组表示使用 Array<T>() new Array对象 长度为4的int
        
        float[] a7 = Array<float>(){ 1.2, 2.2, 3.4 };  #   
        
        Array<float> a8 = Array( 20 ){1,2,3,5,3.3};   #申请一个长度为20的数组
       
        a9 = array( 27,3 ); #申请一个三维数组，边界分别为3,3,3     
        
        bb2 = int[10,20]{ 1,2,3,4,5 };    # 等于 Array<int>( 100, 10 )  {1,2,3,4,5};        
             
        int[] bb3 = [1,2,3,4,5 ];    #与上相同  Array<int>(5){ 1,2,3,4,5}
        !#  
        arr1 = Array<ArrClass>();     #申请一个该类型的数组对象，但长度为0

        #ArrClass[] arr2 = ArrClass[10]{};
        #arr1.SetLength( 100 );         #设置数组的长度
        #arr1[0].i = 20;
 
        int i11 = 11;
        arr1.$i11.i = 10;
         
        arr1[1] = { i = 20 };
        
        arr1[1000].i = 10000; # 在编译时，处理是否有超过长度现象，如果有的话，则编译不通过
       
        arr1.Add( ArrClass(){i=100 } );  #增加数据+1 
        
        arr1.RemoveIndex( 2 );       #删除数据-1 
       
         #!
        arr1.Remove( arr1[20] );     #删除数据-1 
        arr1.$0.i = 10;
        
        arr1.index = 20;     #数组的当前游标
        arr1.value.i = 10;   #数组当前游标的植
         
        for( a in arr1 )      #使用for 的 a 是封装过的it里边包含 Index() 也可以直接a = ArrClass();替代里边的值
        {
            if a.index == 20   #系统自带Index()函数  如果在使用for 时，则object.Index()表示他的下标
            {
                a = ArrClass(){ i = 100 };
                continue;
            }

            a.i = 200;
        }
        for( a in [1,2,3,4] )
        {
            i = a.index + 1;
        }
        for i = 0, i < arr1.Count()
        {
            i++;
            if i < 40
            {
                continue;
            }

            arr1[i] = ArrClass();
            arr1[i].i = 100;
            arr1.@i.i = 100;

            i+=2;
        }

        #Array 继承集合接口 Collection 可以使用  a in Collection的遍历
        !#
    }
}

# 这个实现的功能有
# 2. Array的构建
# 3. Array的各种构建
# 3.1.1 先实现了，在函数里，直接调用C#层写的方法。
# 3.1 先实现type和Cast 
# 4. 遍历Array或者是range
# 5. Range 转成Array
# 6. Array的方法实现，与c#代码操作内容的关系 slang与csharplang的通讯
