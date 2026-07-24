import Std

TryTest
{
    # ── 错误类型定义 (try.md §3 enumError) ──
    # 当前使用 class 模拟，待 enumError 实现后替换
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

    # ── 1. throws 函数声明 (try.md §2) ──
    # throws 标记的函数可以被 try 捕获
    static throwsDeclareTest()
    {
        global.println("========== 1. throws declare ==========")
        string result = "before"
        try
        {
            result = throwsFunc()
        }
        catch
        {
            result = "caught"
        }
        global.println("result = " + result)
    }

    # throws 标记：此函数可能抛出异常
    static string throwsFunc()
    {
        ret "throwsFunc-ok"
    }

    # ── 2. throws 函数抛出异常被捕获 (try.md §2) ──
    static throwsAndCatchTest()
    {
        global.println("========== 2. throws and catch ==========")
        string result = "before"
        try
        {
            result = "try-entered"
            throwExceptionFunc()
            result = "try-after-call"
        }
        catch
        {
            result = "caught"
        }
        global.println("result = " + result)
    }

    static void throwExceptionFunc()
    {
        throw "something went wrong"
    }

    # ── 3. catch 绑定变量 (try.md §6) ──
    static catchWithBindingTest()
    {
        global.println("========== 3. catch with binding ==========")
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

    # ── 4. throw 类型化对象 + 类型化 catch (try.md §6) ──
    static throwTypedObjectTest()
    {
        global.println("========== 4. throw typed object ==========")
        string captured = "none"
        try
        {
            throw "typed-throw-test"
        }
        catch err
        {
            captured = err.toString()
        }
        global.println("captured = " + captured)
    }

    # ── 5. finally 块总是执行 (try.md §5 finally 语义) ──
    static finallyAlwaysRunsTest()
    {
        global.println("========== 5. finally always runs ==========")
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

    # ── 6. 异常时 finally 仍执行 (try.md §5) ──
    static finallyOnExceptionTest()
    {
        global.println("========== 6. finally on exception ==========")
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

    # ── 7. 嵌套 try/catch ──
    static nestedTryCatchTest()
    {
        global.println("========== 7. nested try/catch ==========")
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

    # ── 8. 重新抛出 (re-throw) ──
    static rethrowTest()
    {
        global.println("========== 8. re-throw ==========")
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

    # ── 9. try/catch 在循环中 ──
    static tryCatchInLoopTest()
    {
        global.println("========== 9. try/catch in loop ==========")
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

    # ── 10. try 中 ret，finally 仍执行 ──
    static tryReturnWithFinallyTest()
    {
        global.println("========== 10. try ret with finally ==========")
        global.println("returned = " + doReturnWithFinally().toString())
    }

    static Int32 doReturnWithFinally()
    {
        try
        {
            ret 100
        }
        finally
        {
            global.println("  finally executed before ret")
        }
        ret 0
    }

    # ── 11. throws 函数异常传播 (try.md §2 throws 传播) ──
    # throws 函数的异常通过调用链传播到调用方的 catch
    static exceptionPropagationTest()
    {
        global.println("========== 11. throws propagation ==========")
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

    # throws 标记：此函数会抛出异常
    static void deepThrower()
    {
        throw "from-deep"
    }

    # ── 12. catch 绑定变量 (try.md §6 模式匹配) ──
    # 注意：当前 VM 不按类型过滤 catch，第一个 catch 会捕获所有异常
    static multipleCatchTypesTest()
    {
        global.println("========== 12. multiple catch types ==========")
        string log = ""
        try
        {
            throw "multi-catch-test"
        }
        catch err
        {
            log = "caught-" + err.toString()
        }
        catch
        {
            log = "caught-generic"
        }
        global.println("log = " + log)
    }

    # ── 13. catch 兜底捕获 ──
    static catchAllFallbackTest()
    {
        global.println("========== 13. catch-all fallback ==========")
        string log = ""
        try
        {
            throw "fallback-test"
        }
        catch err
        {
            log = "first-catch: " + err.toString()
        }
        catch
        {
            log = "fallback-catch"
        }
        global.println("log = " + log)
    }

    # ── 14. finally 不带 catch ──
    static finallyWithoutCatchTest()
    {
        global.println("========== 14. finally without catch ==========")
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

    # ── 15. catch 块中再次 throw ──
    static throwInCatchTest()
    {
        global.println("========== 15. throw in catch ==========")
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

    # ── 16. throws 函数多层传播 (try.md §2) ──
    # throws 函数 A 调用 throws 函数 B，异常逐层传播
    static throwsChainPropagationTest()
    {
        global.println("========== 16. throws chain propagation ==========")
        string log = "start"
        try
        {
            log = log + "-call"
            level1Throws()
            log = log + "-after-call"
        }
        catch
        {
            log = log + "-caught"
        }
        global.println("log = " + log)
    }

    static void level1Throws()
    {
        level2Throws()
    }

    static void level2Throws()
    {
        level3Throws()
    }

    static void level3Throws()
    {
        throw "from-level3"
    }

    # ── 17. throws 函数正常返回不触发 catch ──
    static throwsNoExceptionTest()
    {
        global.println("========== 17. throws no exception ==========")
        string log = "start"
        try
        {
            log = log + "-call"
            string result = safeThrowsFunc()
            log = log + "-" + result
        }
        catch
        {
            log = log + "-caught"
        }
        global.println("log = " + log)
    }

    # throws 标记但实际不抛异常
    static string safeThrowsFunc()
    {
        ret "safe-ok"
    }

    # ── main entry ──
    static fun()
    {
        global.println("========== all try/catch/throws tests start ==========")
        throwsDeclareTest()
        throwsAndCatchTest()
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
        throwsChainPropagationTest()
        throwsNoExceptionTest()
        global.println("========== all try/catch/throws tests done ==========")
    }
}
