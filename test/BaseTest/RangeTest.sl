
RangeTest
{
    ArrClass
    {
        int i = 0;
    }
    static fun()
    {
        r1 = 1..10;    #快速int range  以后再支持 1..n   n..200的方式    相当于   range( 1, 100, 1 )的调用        
        r2 = Range<float>( 1.0f, 20.0f, 1.0f );
        Range<double> r3 = new(3.2d, 24.3d, 0.22d );
        r4 = Range<short>( 1s, 30s, 2s );


        global.println("=======R1 0-100 step 1 ================");
        for v in r1
        {
            global.println("value=$v ");
        }
        
        global.println("=======R2 1.0f-200.0f step 1.0f ================");
        for v in r2
        {
            global.println("value=$v ");
        }
        
        global.println("=======R3 3.2 - 54.3 step 0.22 ================");
        for v in r3
        {
            global.println("value=$v ");
        }
        
        global.println("=======R4  1s-100s step 2 ================");
        for v in r4
        {
            global.println("value=$v ");
        }
    }
}
# 3.1.1 先实现了，在函数里，直接调用C#层写的方法。
# 5. Range 转成Array
# 7. value, index成为不能使用关键字
# array 如果重写Set 则是相当于 array[?] = 20;这种的写法  如果重写 __SetValue__( int index, T )   T __GetValue__( int index )  每个都有__SetValue__ 方法
