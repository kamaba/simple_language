import Std

# ── 错误类型定义（try.md §3 enum extends Error）──
enum TestError extends Error
{
    TestError1 = { code = 1, message = "test-error" }
    TestError2 = { code = 2, message = "binding-test" }
    TestError3 = { code = 3, message = "boom" }
    InnerError = { code = 4, message = "inner" }
    RethrowError = { code = 5, message = "rethrow-me" }
    SkipError = { code = 6, message = "skip-two" }
    FallbackError = { code = 7, message = "fallback-test" }
    FirstError = { code = 8, message = "first" }
    SecondError = { code = 9, message = "second" }
    CombinedError = { code = 10, message = "combined-test" }
    FromFuncError = { code = 11, message = "from-throwExceptionFunc" }
    FromLevel3 = { code = 12, message = "from-level3" }
}

enum MathError extends Error
{
    DivZero = { code = 101, message = "除以零" }
    Overflow = { code = 102, message = "溢出" }
}

TryTest
{
    # ── 辅助函数 ──

    # throws 标记：可能抛出异常的函数（try.md §2）
    static string riskyFunc(bool shouldFail) throws
    {
        if (shouldFail)
        {
            throw TestError.TestError1
        }
        ret "success"
    }

    # throws 标记但不实际抛异常
    static string safeThrowsFunc() throws
    {
        ret "safe-ok"
    }

    # throws 标记的多层调用链
    static void level1() throws
    {
        level2()
    }

    static void level2() throws
    {
        level3()
    }

    static void level3() throws
    {
        throw TestError.FromLevel3
    }

    # ── 1. 基本try/catch：throws函数异常被捕获 ──
    static basicTryCatchTest()
    {
        global.println("========== 1. basic try/catch ==========")
        string log = "before"
        try
        {
            log = "try"
            throw TestError.TestError1
        }
        catch
        {
            log = log + "-catch"
        }
        global.println("log = " + log)
    }

    # ── 2. try正常结束时catch不执行 ──
    static tryNoExceptionTest()
    {
        global.println("========== 2. try no exception ==========")
        string log = "before"
        try
        {
            log = "try"
        }
        catch
        {
            log = log + "-catch"
        }
        global.println("log = " + log)
    }

    # ── 3. catch绑定变量（try.md §6）──
    static catchBindingTest()
    {
        global.println("========== 3. catch binding ==========")
        string captured = "none"
        try
        {
            throw TestError.TestError2
        }
        catch ex
        {
            captured = ex.toString()
        }
        global.println("captured = " + captured)
    }

    # ── 4. finally块总是执行（try.md §5）──
    static finallyAlwaysRunsTest()
    {
        global.println("========== 4. finally always runs ==========")
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

    # ── 5. 异常时finally仍执行 ──
    static finallyOnExceptionTest()
    {
        global.println("========== 5. finally on exception ==========")
        string log = ""
        try
        {
            log = "try"
            throw TestError.TestError3
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

    # ── 6. 嵌套try/catch ──
    static nestedTryCatchTest()
    {
        global.println("========== 6. nested try/catch ==========")
        string log = ""
        try
        {
            log = "outer-try"
            try
            {
                log = log + "-inner-try"
                throw TestError.InnerError
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

    # ── 7. 重新抛出（re-throw）──
    static rethrowTest()
    {
        global.println("========== 7. re-throw ==========")
        string log = ""
        try
        {
            try
            {
                log = "inner-try"
                throw TestError.RethrowError
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

    # ── 8. try/catch在循环中 ──
    static tryCatchInLoopTest()
    {
        global.println("========== 8. try/catch in loop ==========")
        Int32 sum = 0
        for Int32 i = 0, i < 5, i = i + 1
        {
            try
            {
                if (i == 2)
                {
                    throw TestError.SkipError
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

    # ── 9. try中ret，finally仍执行 ──
    static tryReturnWithFinallyTest()
    {
        global.println("========== 9. try ret with finally ==========")
        global.println("returned = " + doReturnWithFinally().toString())
    }

    static Int32 doReturnWithFinally() throws
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

    # ── 10. finally不带catch ──
    static finallyWithoutCatchTest()
    {
        global.println("========== 10. finally without catch ==========")
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

    # ── 11. 跨函数异常传播（throws函数）（try.md §2）──
    static crossFunctionPropagationTest()
    {
        global.println("========== 11. cross-function propagation ==========")
        string log = "start"
        try
        {
            log = log + "-call"
            throwExceptionFunc()
            log = log + "-after-call"
        }
        catch
        {
            log = log + "-caught"
        }
        global.println("log = " + log)
    }

    static void throwExceptionFunc() throws
    {
        throw TestError.FromFuncError
    }

    # ── 12. 多层调用链异常传播 ──
    static multiLevelPropagationTest()
    {
        global.println("========== 12. multi-level propagation ==========")
        string log = "start"
        try
        {
            log = log + "-call"
            level1()
            log = log + "-after-call"
        }
        catch
        {
            log = log + "-caught"
        }
        global.println("log = " + log)
    }

    # ── 13. throws函数正常返回不触发catch ──
    static throwsNoExceptionTest()
    {
        global.println("========== 13. throws no exception ==========")
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

    # ── 14. catch兜底捕获 ──
    static catchAllFallbackTest()
    {
        global.println("========== 14. catch-all fallback ==========")
        string log = ""
        try
        {
            throw TestError.FallbackError
        }
        catch ex
        {
            log = "caught: " + ex.toString()
        }
        catch
        {
            log = "fallback"
        }
        global.println("log = " + log)
    }

    # ── 15. catch块中再次throw ──
    static throwInCatchTest()
    {
        global.println("========== 15. throw in catch ==========")
        string log = ""
        try
        {
            try
            {
                throw TestError.FirstError
            }
            catch
            {
                log = "first-catch"
                throw TestError.SecondError
            }
        }
        catch
        {
            log = log + "-second-catch"
        }
        global.println("log = " + log)
    }

    # ── 16. try? 对throws函数：异常时返回null（§4.2）──
    static tryQuestionOnExceptionTest()
    {
        global.println("========== 16. try? on throws func (exception) ==========")
        string result = try? riskyFunc(true)
        if (result == null)
        {
            global.println("try? returned null (expected)")
        }
        else
        {
            global.println("try? returned: " + result)
        }
    }

    # ── 17. try? 对throws函数：正常时返回值（§4.2）──
    static tryQuestionOnSuccessTest()
    {
        global.println("========== 17. try? on throws func (success) ==========")
        string result = try? riskyFunc(false)
        if (result == null)
        {
            global.println("try? returned null (unexpected)")
        }
        else
        {
            global.println("try? returned: " + result)
        }
    }

    # ── 18. try? 对throws但安全函数 ──
    static tryQuestionOnSafeThrowsTest()
    {
        global.println("========== 18. try? on safe throws func ==========")
        string result = try? safeThrowsFunc()
        global.println("try? returned: " + result)
    }

    # ── 19. try! 对throws函数：异常时传播（§4.3）──
    static tryExclamationOnExceptionTest()
    {
        global.println("========== 19. try! on throws func (exception) ==========")
        try
        {
            string result = try! riskyFunc(true)
            global.println("try! returned: " + result)
        }
        catch
        {
            global.println("caught exception from try!")
        }
    }

    # ── 20. try! 对throws函数：正常时返回值（§4.3）──
    static tryExclamationOnSuccessTest()
    {
        global.println("========== 20. try! on throws func (success) ==========")
        string result = try! riskyFunc(false)
        global.println("try! returned: " + result)
    }

    # ── 21. try! 对throws但安全函数 ──
    static tryExclamationOnSafeThrowsTest()
    {
        global.println("========== 21. try! on safe throws func ==========")
        string result = try! safeThrowsFunc()
        global.println("try! returned: " + result)
    }

    # ── main entry ──
    static fun()
    {
        # try/catch/finally 基础
        basicTryCatchTest()
        tryNoExceptionTest()
        catchBindingTest()
        finallyAlwaysRunsTest()
        finallyOnExceptionTest()
        nestedTryCatchTest()
        rethrowTest()
        tryCatchInLoopTest()
        tryReturnWithFinallyTest()
        finallyWithoutCatchTest()

        # throws 传播
        crossFunctionPropagationTest()
        multiLevelPropagationTest()
        throwsNoExceptionTest()

        # catch 变体
        catchAllFallbackTest()
        throwInCatchTest()

        # try? / try! 表达式
        tryQuestionOnExceptionTest()
        tryQuestionOnSuccessTest()
        tryQuestionOnSafeThrowsTest()
        tryExclamationOnExceptionTest()
        tryExclamationOnSuccessTest()
        tryExclamationOnSafeThrowsTest()

        global.println("========== all try tests done ==========")
    }
}
