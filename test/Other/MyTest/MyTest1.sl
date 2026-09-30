import Std;
import Std.OS;

MyTest1
{
    public static void WaitTimeaa()
    {
        Console.println("aaaaaaaaaaa");

        var start1 = OS.Timer.clock();

        Coroutine.delay(2000)

        Console.println("2222222222========" + (OS.Timer.clock() - start1 ).toString() )
    }
    public static void Waitimeabbb()
    {
        Console.println("bbbbbbbbbbbb");

        var start1 = OS.Timer.clock();

        Coroutine.delay(1500)

        Console.println("3333333333333========" + (OS.Timer.clock() - start1 ).toString() )
    }
    static fun()
    {
        spawn WaitTimeaa()
        Waitimeabbb();
    }
}