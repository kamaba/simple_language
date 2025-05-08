
Range<T>
{
    _init_( T _start, T _end, T _step )
    {
        this.m_Start = _start;
        this.m_End = _end;
        this.m_Step = _step;
    }
}

RangeTest
{
    ArrClass
    {
        int i = 0;
    }
    static Fun()
    {
        r1 = 1..100;    #快速int range  以后再支持 1..n   n..200的方式    相当于   range( 1, 100, 1 )的调用        
        r2 = range( 1.0f, 200.0f, 1.0f );
        Range<double> r3 = (3.2d, 54.3d, 0.22d );
        r4 = Range<short>( 1s, 100s, 2s );

        for v in r1
        {
            CSharp.System.Console.Write("value=$v ");
        }
    }
}
# 3.1.1 先实现了，在函数里，直接调用C#层写的方法。
# 5. Range 转成Array
# 7. value, index成为不能使用关键字
# array 如果重写Set 则是相当于 array[?] = 20;这种的写法  如果重写 __SetValue__( int index, T )   T __GetValue__( int index )  每个都有__SetValue__ 方法
