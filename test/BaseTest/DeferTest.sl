import Std

DeferTest
{
    # ── 1. defer runs at function end (normal return) ──
    static deferBasicTest()
    {
        global.println("========== 1. defer basic ==========")
        string log = "start"
        defer
        {
            log = log + "-defer"
            global.println("defer executed: " + log)
        }
        log = log + "-body"
        global.println("body: " + log)
    }

    # ── 2. defer runs after return ──
    static deferAfterReturnTest()
    {
        global.println("========== 2. defer after return ==========")
        defer
        {
            global.println("defer ran after return")
        }
        global.println("before return")
        ret
    }

    # ── 3. multiple defers run in LIFO order ──
    static deferLifoTest()
    {
        global.println("========== 3. defer LIFO ==========")
        defer
        {
            global.println("defer 1 (first declared)")
        }
        defer
        {
            global.println("defer 2 (second declared)")
        }
        defer
        {
            global.println("defer 3 (third declared)")
        }
        global.println("body end")
    }

    # ── 4. errdefer runs on exception ──
    static errdeferOnExceptionTest()
    {
        global.println("========== 4. errdefer on exception ==========")
        string log = "start"
        errdefer
        {
            log = log + "-errdefer"
            global.println("errdefer executed: " + log)
        }
        log = log + "-body"
        global.println("body: " + log)
        throw "test-exception"
    }

    # ── 5. errdefer does NOT run on normal exit ──
    static errdeferNoRunOnNormalTest()
    {
        global.println("========== 5. errdefer no run on normal ==========")
        errdefer
        {
            global.println("ERROR: errdefer should not run on normal exit!")
        }
        global.println("normal exit")
    }

    # ── 6. defer runs on exception too ──
    static deferOnExceptionTest()
    {
        global.println("========== 6. defer on exception ==========")
        string log = "start"
        defer
        {
            log = log + "-defer"
            global.println("defer on exception: " + log)
        }
        log = log + "-body"
        throw "defer-on-exception-test"
    }

    # ── 7. defer + errdefer together on exception ──
    static deferAndErrdeferTest()
    {
        global.println("========== 7. defer + errdefer ==========")
        defer
        {
            global.println("defer ran")
        }
        errdefer
        {
            global.println("errdefer ran")
        }
        global.println("body")
        throw "combined-test"
    }

    # ── 8. errdefer runs then exception propagates ──
    static errdeferWithRetTest()
    {
        global.println("========== 8. errdefer then propagate ==========")
        errdefer
        {
            global.println("errdefer cleanup before propagation")
        }
        throw "will-be-caught"
    }

    # ── main entry ──
    static fun()
    {
        deferBasicTest()
        deferAfterReturnTest()
        deferLifoTest()

        # Test 4: errdefer on exception (wrapped in try/catch)
        try
        {
            errdeferOnExceptionTest()
        }
        catch
        {
            global.println("caught exception from test 4")
        }

        # Test 5: errdefer no run on normal
        errdeferNoRunOnNormalTest()

        # Test 6: defer on exception (wrapped in try/catch)
        try
        {
            deferOnExceptionTest()
        }
        catch
        {
            global.println("caught exception from test 6")
        }

        # Test 7: defer + errdefer (wrapped in try/catch)
        try
        {
            deferAndErrdeferTest()
        }
        catch
        {
            global.println("caught exception from test 7")
        }

        # Test 8: errdefer runs then exception propagates (wrapped in try/catch)
        try
        {
            errdeferWithRetTest()
        }
        catch
        {
            global.println("caught exception from test 8")
        }

        global.println("========== all defer/errdefer tests done ==========")
    }
}
