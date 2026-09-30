import Std;
import Std.OS;

MyTest1
{
    public static void WaitTimeaa()
    {
        Console.println("a1");

        var start1 = OS.Timer.clock();

        Coroutine.delay(2000);

        Console.println("a2========" + (OS.Timer.clock() - start1 ).toString() )
    }
    public static void Waitimeabbb()
    {
        Console.println("b1");

        var start1 = OS.Timer.clock();

        Coroutine.delay(1500);

        Console.println("b2========" + (OS.Timer.clock() - start1 ).toString() )
    }
    static fun()
    {
        spawn WaitTimeaa()
        Coroutine.yieldNow()
        Waitimeabbb();
        #!
        # [探针1] 分流一直传: 函数值变量实参在 spawn 点立即求值, probe 应为 0/100/200
        function probeFn = function( int a )
        {
            ret a * 100
        }
        List<Task> ts = List<Task>()
        for Int32 i = 0, i < 3, i = i + 1
        {
            Task t = spawn probeFn( i )
            ts.add( t )
        }
        for v in ts
        {
            Console.println( "probe=" + ( Coroutine.awaitTask( v ) as int ).toString() )
        }

        # [探针2] 分流二包装闭包: 裸静态方法名 + 变量实参, 延迟到协程首跑求值, sps 应为 405
        Int32 iv = 5
        Task tsps = spawn probeStatic( 4, iv )
        
        Console.println( "sps=" + ( Coroutine.awaitTask( tsps ) as int ).toString() )
        !#
    }

    static int probeStatic( int a, int b )
    {
        ret a * 100 + b
    }
}
