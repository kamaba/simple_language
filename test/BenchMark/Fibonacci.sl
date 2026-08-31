Fibonacci
{
    static fun()
    {
        Console.println("========== Fibonacci (start) ==========")
        nowMs = Environment.nowMillis()
        sum = 0L
        for i = 0, i < 22, i++
        {
            sum += Fibonacci.fib(i)
        }
        nowMs = Environment.nowMillis() - nowMs
        Console.println("fib(0..21) sum = " + sum.toString() + "  [$nowMs.toString() ms]")
        Console.println("========== Fibonacci (end) ==========")
    }

    static fun fib(n)
    {
        ret n <= 1 ? n : Fibonacci.fib(n - 1) + Fibonacci.fib(n - 2)
    }
}
