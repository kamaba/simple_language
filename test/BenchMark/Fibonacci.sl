import Std

Fibonacci
{
    static fun()
    {
        Console.println("========== Fibonacci (start) ==========")
        nowMs = Environment.sys.nowMillis()
        sum = 0L
        for i = 0, i < 22, i++
        {
            sum += Fibonacci.fib(i)
        }
        nowMs = Environment.sys.nowMillis() - nowMs
        Console.println("fib(0..21) sum = " + sum.toString() + "  [$nowMs.toString() ms]")
        Console.println("========== Fibonacci (end) ==========")
    }

    static int fib( int n)
    {
        ret n <= 1 ? n : Fibonacci.fib(n - 1) + fib(n - 2)
    }
}
