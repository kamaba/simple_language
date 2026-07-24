import Std

TryExpressionTest
{
    # A function that throws
    static string riskyFunc(bool shouldFail)
    {
        if (shouldFail)
        {
            throw "error-from-riskyFunc"
        }
        ret "success"
    }

    # ── 1. try? returns null on exception ──
    static tryQuestionOnException()
    {
        global.println("========== 1. try? on exception ==========")
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

    # ── 2. try? returns value on success ──
    static tryQuestionOnSuccess()
    {
        global.println("========== 2. try? on success ==========")
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

    # ── 3. try! propagates exception ──
    static tryExclamationTest()
    {
        global.println("========== 3. try! propagates exception ==========")
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

    # ── 4. try! on success returns value ──
    static tryExclamationSuccess()
    {
        global.println("========== 4. try! on success ==========")
        string result = try! riskyFunc(false)
        global.println("try! returned: " + result)
    }

    # ── main entry ──
    static fun()
    {
        tryQuestionOnException()
        tryQuestionOnSuccess()

        tryExclamationTest()
        tryExclamationSuccess()

        global.println("========== all try expression tests done ==========")
    }
}
