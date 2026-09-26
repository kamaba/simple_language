import Std

Project
{
    _main_()
    {
        Console.println("===== BenchMark _main_ start =====")
        nowMs = Environment.sys.nowMillis()

        #HelloWorld.fun()
        Fibonacci.fun()
        
        Loop.fun()
        Levenshtein.fun()
        DataTypes.fun()
        StringBench.fun()
        

        nowMs = Environment.sys.nowMillis() - nowMs
         Console.println("===== BenchMark _main_ end [$nowMs.toString() ms] =====")
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
