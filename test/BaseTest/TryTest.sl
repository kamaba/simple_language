import Std

TryTest
{
    # ── helper: a simple exception-like class for testing ──
    TryError
    {
        string message = ""
        Int32 code = 0

        _init_(string msg)
        {
            this.message = msg
        }

        _init_(string msg, Int32 code)
        {
            this.message = msg
            this.code = code
        }

        override string toString()
        {
            ret "TryError(" + this.code.toString() + "): " + this.message
        }
    }

    # ── 1. basic try / catch ──
    static basicTryCatchTest()
    {
        global.println("========== basic try/catch ==========")
        string result = "no-exception"
        try
        {
            result = "try-body"
        }
        catch
        {
            result = "catch-body"
        }
        global.println("result = " + result)
    }

    # ── 2. throw inside try, caught by catch ──
    static throwAndCatchTest()
    {
        global.println("========== throw and catch ==========")
        string result = "before"
        try
        {
            result = "try-entered"
            throw "something went wrong"
            result = "try-after-throw"
        }
        catch
        {
            result = "caught"
        }
        global.println("result = " + result)
    }

    # ── 3. catch with variable binding ──
    static catchWithBindingTest()
    {
        global.println("========== catch with binding ==========")
        string captured = "none"
        try
        {
            throw "binding-test"
        }
        catch ex
        {
            captured = ex.toString()
        }
        global.println("captured = " + captured)
    }

    # ── 4. throw a typed object ──
    static throwTypedObjectTest()
    {
        global.println("========== throw typed object ==========")
        string captured = "none"
        try
        {
            throw new TryError("division by zero", 42)
        }
        catch TryError err
        {
            captured = err.toString()
        }
        global.println("captured = " + captured)
    }

    # ── 5. finally block always runs ──
    static finallyAlwaysRunsTest()
    {
        global.println("========== finally always runs ==========")
        string log = ""
        try
        {
            log = "try"
        }
        finally
        {
            log = log + "-finally"
        }
        global.println("log = " + log)
    }

    # ── 6. finally runs even when exception is thrown ──
    static finallyOnExceptionTest()
    {
        global.println("========== finally on exception ==========")
        string log = ""
        try
        {
            log = "try"
            throw "boom"
        }
        catch
        {
            log = log + "-catch"
        }
        finally
        {
            log = log + "-finally"
        }
        global.println("log = " + log)
    }

    # ── 7. nested try / catch ──
    static nestedTryCatchTest()
    {
        global.println("========== nested try/catch ==========")
        string log = ""
        try
        {
            log = "outer-try"
            try
            {
                log = log + "-inner-try"
                throw "inner"
            }
            catch
            {
                log = log + "-inner-catch"
            }
            log = log + "-outer-continue"
        }
        catch
        {
            log = log + "-outer-catch"
        }
        global.println("log = " + log)
    }

    # ── 8. re-throw from inner catch to outer catch ──
    static rethrowTest()
    {
        global.println("========== re-throw ==========")
        string log = ""
        try
        {
            try
            {
                log = "inner-try"
                throw "rethrow-me"
            }
            catch
            {
                log = log + "-inner-catch"
                throw
            }
        }
        catch
        {
            log = log + "-outer-catch"
        }
        global.println("log = " + log)
    }

    # ── 9. try / catch inside a loop ──
    static tryCatchInLoopTest()
    {
        global.println("========== try/catch in loop ==========")
        Int32 sum = 0
        for Int32 i = 0, i < 5, i = i + 1
        {
            try
            {
                if (i == 2)
                {
                    throw "skip-two"
                }
                sum = sum + i
            }
            catch
            {
                global.println("  skipped i=" + i.toString())
            }
        }
        global.println("sum = " + sum.toString())
    }

    # ── 10. try with return (finally still executes) ──
    static tryReturnWithFinallyTest()
    {
        global.println("========== try return with finally ==========")
        global.println("returned = " + doReturnWithFinally().toString())
    }

    static Int32 doReturnWithFinally()
    {
        try
        {
            return 100
        }
        finally
        {
            global.println("  finally executed before return")
        }
        return 0
    }

    # ── 11. exception propagation through method calls ──
    static exceptionPropagationTest()
    {
        global.println("========== exception propagation ==========")
        string log = "start"
        try
        {
            log = log + "-call"
            deepThrower()
            log = log + "-after-call"
        }
        catch
        {
            log = log + "-caught"
        }
        global.println("log = " + log)
    }

    static void deepThrower()
    {
        throw "from-deep"
    }

    # ── 12. multiple catch types (CLR style) ──
    static multipleCatchTypesTest()
    {
        global.println("========== multiple catch types ==========")
        string log = ""
        try
        {
            throw new TryError("multi-catch", 7)
        }
        catch TryError err
        {
            log = "caught-TryError-" + err.code.toString()
        }
        catch
        {
            log = "caught-generic"
        }
        global.println("log = " + log)
    }

    # ── 13. catch-all fallback ──
    static catchAllFallbackTest()
    {
        global.println("========== catch-all fallback ==========")
        string log = ""
        try
        {
            throw "fallback-test"
        }
        catch TryError err
        {
            log = "typed-catch"
        }
        catch
        {
            log = "fallback-catch"
        }
        global.println("log = " + log)
    }

    # ── 14. finally without catch ──
    static finallyWithoutCatchTest()
    {
        global.println("========== finally without catch ==========")
        string log = ""
        try
        {
            log = "try"
        }
        finally
        {
            log = log + "-finally"
        }
        global.println("log = " + log)
    }

    # ── 15. throw in catch block ──
    static throwInCatchTest()
    {
        global.println("========== throw in catch ==========")
        string log = ""
        try
        {
            try
            {
                throw "first"
            }
            catch
            {
                log = "first-catch"
                throw "second"
            }
        }
        catch
        {
            log = log + "-second-catch"
        }
        global.println("log = " + log)
    }

    # ── 16. throw built-in Exception with cause chain ──
    static builtinExceptionTest()
    {
        global.println("========== built-in Exception ==========")
        string log = ""
        try
        {
            try
            {
                throw new Exception("low-level error", 10)
            }
            catch Exception inner
            {
                throw new Exception("operation failed", inner)
            }
        }
        catch Exception e
        {
            log = e.toString()
            if (e.hasCause())
            {
                log = log + " -> caused by: " + e.getCause().toString()
            }
        }
        global.println("log = " + log)
    }

    # ── main entry ──
    static _main_()
    {
        basicTryCatchTest()
        throwAndCatchTest()
        catchWithBindingTest()
        throwTypedObjectTest()
        finallyAlwaysRunsTest()
        finallyOnExceptionTest()
        nestedTryCatchTest()
        rethrowTest()
        tryCatchInLoopTest()
        tryReturnWithFinallyTest()
        exceptionPropagationTest()
        multipleCatchTypesTest()
        catchAllFallbackTest()
        finallyWithoutCatchTest()
        throwInCatchTest()
        builtinExceptionTest()
        global.println("========== all try/catch tests done ==========")
    }
}
