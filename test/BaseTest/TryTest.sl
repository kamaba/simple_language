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
    FromFuncError = { code = 10, message = "from-throwExceptionFunc" }
    FromLevel3 = { code = 11, message = "from-level3" }
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

    # ── 1. 基本 label{}catch{} + try 表达式 ──
    static basicTryCatchTest() throws
    {
        global.println("========== 1. basic label{}catch{} + try ==========")
        string log = "before"

        label basicBlock
        {
            log = "try"
            try riskyFunc(true)
        }
        catch
        {
            log = log + "-catch"
        }
        global.println("log = " + log)
    }

    # ── 2. 块正常结束时catch不执行 ──
    static tryNoExceptionTest()
    {
        global.println("========== 2. block no exception ==========")
        string log = "before"

        label noExceptionBlock
        {
            log = "try"
            try riskyFunc(false)
        }
        catch
        {
            log = log + "-catch"
        }
        global.println("log = " + log)
    }

    # ── 3. catch绑定变量（try.md §5）──
    static catchBindingTest() throws
    {
        global.println("========== 3. catch binding ==========")
        string captured = "none"
        label bindBlock
        {
            try riskyFunc(true)
        }
        catch TestError ex
        {
            captured = ex.toString()
        }
        global.println("captured = " + captured)
    }

    # ── 4. finally块总是执行 ──
    static finallyAlwaysRunsTest()
    {
        global.println("========== 4. finally always runs ==========")
        string log = ""
        label finallyBlock
        {
            log = "try"
            try riskyFunc(false)
        }
        finally
        {
            log = log + "-finally"
        }
        global.println("log = " + log)
    }

    # ── 5. 异常时finally仍执行 ──
    static finallyOnExceptionTest() throws
    {
        global.println("========== 5. finally on exception ==========")
        string log = ""
        label exceptFinallyBlock
        {
            log = "try"
            try riskyFunc(true)
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

    # ── 6. 嵌套 label{}catch{} ──
    static nestedTryCatchTest() throws
    {
        global.println("========== 6. nested label{}catch{} ==========")
        string log = ""
        label outerBlock
        {
            log = "outer-try"
            label innerBlock
            {
                log = log + "-inner-try"
                try riskyFunc(true)
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
    static rethrowTest() throws
    {
        global.println("========== 7. re-throw ==========")
        string log = ""
        label rethrowOuter
        {
            label rethrowInner
            {
                log = "inner-try"
                try riskyFunc(true)
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

    # ── 8. label{}catch{}在循环中 ──
    static tryCatchInLoopTest() throws
    {
        global.println("========== 8. label{}catch{} in loop ==========")
        Int32 sum = 0
        for Int32 i = 0, i < 5, i = i + 1
        {
            label loopBlock
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

    # ── 9. 块中ret，finally仍执行 ──
    static tryReturnWithFinallyTest()
    {
        global.println("========== 9. block ret with finally ==========")
        global.println("returned = " + doReturnWithFinally().toString())
    }

    static Int32 doReturnWithFinally() throws
    {
        label retBlock
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
        label onlyFinallyBlock
        {
            log = "try"
            try riskyFunc(false)
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
        label crossFuncBlock
        {
            log = log + "-call"
            try throwExceptionFunc()
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
        label multiLevelBlock
        {
            log = log + "-call"
            try level1()
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
        label safeThrowsBlock
        {
            log = log + "-call"
            string result = try safeThrowsFunc()
            log = log + "-" + result
        }
        catch
        {
            log = log + "-caught"
        }
        global.println("log = " + log)
    }

    # ── 14. catch兜底捕获 ──
    static catchAllFallbackTest() throws
    {
        global.println("========== 14. catch-all fallback ==========")
        string log = ""
        label fallbackBlock
        {
            try riskyFunc(true)
        }
        catch TestError ex
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
    static throwInCatchTest() throws
    {
        global.println("========== 15. throw in catch ==========")
        string log = ""
        label outerThrowBlock
        {
            label innerThrowBlock
            {
                try riskyFunc(true)
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

    # ── 16. try? 对throws函数：异常时返回null（§6.2）──
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

    # ── 17. try? 对throws函数：正常时返回值（§6.2）──
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

    # ── 19. try! 对throws函数：异常时传播（§6.3）──
    static tryExclamationOnExceptionTest()
    {
        global.println("========== 19. try! on throws func (exception) ==========")
        label tryExclBlock
        {
            string result = try! riskyFunc(true)
            global.println("try! returned: " + result)
        }
        catch
        {
            global.println("caught exception from try!")
        }
    }

    # ── 20. try! 对throws函数：正常时返回值（§6.3）──
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

    # ── 22. 作用域共享：catch/finally 访问 label{} 块内变量 ──
    static scopeSharingTest() throws
    {
        global.println("========== 22. scope sharing ==========")
        label scopeBlock
        {
            string innerLog = "try"
            Int32 count = 0
            try riskyFunc(true)
        }
        catch
        {
            innerLog = innerLog + "-catch"
            count = count + 1
            global.println("innerLog = " + innerLog + ", count = " + count.toString())
        }
    }

    # ── 23. 作用域共享：finally 也能访问 label{} 块内变量 ──
    static scopeSharingFinallyTest()
    {
        global.println("========== 23. scope sharing finally ==========")
        label scopeFinallyBlock
        {
            string status = "running"
            try riskyFunc(false)
            status = "done"
        }
        finally
        {
            global.println("finally status = " + status)
        }
    }

    # ── 24. 作用域共享：catch+finally 同时访问 ──
    static scopeSharingCatchFinallyTest() throws
    {
        global.println("========== 24. scope sharing catch+finally ==========")
        label scopeCFBlock
        {
            string log = "try"
            Int32 step = 0
            try riskyFunc(true)
            step = 1
        }
        catch
        {
            log = log + "-catch"
            step = step + 10
        }
        finally
        {
            log = log + "-finally"
            global.println("log = " + log + ", step = " + step.toString())
        }
    }

    # ── main entry ──
    static fun()
    {
        # label{}catch{} + try 基础
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

        # 作用域共享
        scopeSharingTest()
        scopeSharingFinallyTest()
        scopeSharingCatchFinallyTest()

        global.println("========== all try tests done ==========")
    }
}
