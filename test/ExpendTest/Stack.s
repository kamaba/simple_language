
ArrayTest
{
    ArrClass
    {
        int i = 0;
    }
    static Fun()
    {
        int[] a2 = 1..100;    #快速int array
        int[,] a3 = 1...1000;
        int[2,3,4] a4 = 1...1000;
        int[2,3,4,5,6] = 1...1000;
        #!
        a3 = Array(1,2,3,4,5);   #默认int array
        a5 = [1,2,3,4,5];   #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型 
        int[] a4 = [1.2,1.3,1.5];    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样

        # c# 的方法  ArrClass[] arr2 = new ArrClass2[100]; 这里边使用的是 arr2 = ArrClass2[100];
        int[] a1 = Array<Int32>(1,2,3,4);  # 数组表示使用 Array<T>() new Array对象 
        float[] b = Array<float>( 1,2,3,4,5,6 );  #   
        b = int[]( 1,2,3,4,5 );    # 等于 Array<int>( 1,2,3,4,5 );
        int[] b1 = (1,2,3,4,5);    #与上相同
        arr1 = Array<ArrClass>();
        arr1.SetCount( 100 );
        arr1[0].i = 20;
        arr1[1] = { i = 20 };
        arr1[1000].i = 10000; # 在编译时，处理是否有超过长度现象，如果有的话，则编译不通过
        arr1.Add( ArrClass(){i=100 } );  #增加数据+1 
        arr1.RemoveIndex( 2 );       #删除数据-1 
        arr1.Remove( arr1[20] );     #删除数据-1 
        
        for( a in arr1 )      #使用for 的 a 是封装过的it里边包含 Index() 也可以直接a = ArrClass();替代里边的值
        {
            if a.Index() == 20   #系统自带Index()函数  如果在使用for 时，则object.Index()表示他的下标
            {
                a = ArrClass(){ i = 100 };
                continue;
            }

            a.i = 200;
        }
        for( a in 1..100 )
        {
            i = a.Index() + 1;
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

            i+=2;
        }

        #Array 继承集合接口 Collection 可以使用  a in Collection的遍历
        !#
    }
}