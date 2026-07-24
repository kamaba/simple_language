import Std

DeferTest
{
    # ── 1. defer 在函数正常结束时执行 ──
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

    # ── 2. defer 在 ret 之后执行 ──
    static deferAfterReturnTest()
    {
        global.println("========== 2. defer after ret ==========")
        defer
        {
            global.println("defer ran after ret")
        }
        global.println("before ret")
        ret
    }

    # ── 3. 多个 defer 按 LIFO（后进先出）顺序执行 ──
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

    # ── 4. 多个 ret 路径，defer 都执行 ──
    static deferMultiReturnTest()
    {
        global.println("========== 4. defer multi-return ==========")
        defer
        {
            global.println("defer ran (multi-return path)")
        }
        bool flag = true
        if (flag)
        {
            global.println("taking if branch")
            ret
        }
        global.println("taking else path")
    }

    # ── 5. defer 可以修改外部变量 ──
    static deferModifyVarTest()
    {
        global.println("========== 5. defer modify var ==========")
        string log = "start"
        defer
        {
            log = log + "-modified-by-defer"
            global.println("defer sees: " + log)
        }
        log = log + "-body"
        global.println("body: " + log)
    }

    # ── 6. defer 在函数末尾声明（最后一个语句） ──
    static deferAtEndTest()
    {
        global.println("========== 6. defer at end ==========")
        global.println("body before defer")
        defer
        {
            global.println("defer declared at end ran")
        }
    }

    # ── 7. defer 在 if 块内 ──
    static deferInIfBlockTest()
    {
        global.println("========== 7. defer in if block ==========")
        bool flag = true
        if (flag)
        {
            defer
            {
                global.println("defer from if block ran")
            }
            global.println("inside if block")
        }
        global.println("after if block")
    }

    # ── 8. defer 调用另一个函数（清理辅助函数） ──
    static deferCallsFunctionTest()
    {
        global.println("========== 8. defer calls function ==========")
        defer
        {
            cleanupHelper()
        }
        global.println("body end")
    }

    static cleanupHelper()
    {
        global.println("cleanupHelper called from defer")
    }

    # ── 9. defer 和 for 循环配合使用 ──
    static deferInLoopTest()
    {
        global.println("========== 9. defer in loop ==========")
        for Int32 i = 0, i < 3, i = i + 1
        {
            defer
            {
                global.println("defer in loop, i=" + i.toString())
            }
            global.println("loop body i=" + i.toString())
        }
        global.println("loop end")
    }

    # ── 10. defer 内部声明变量 ──
    static deferLocalVarTest()
    {
        global.println("========== 10. defer local var ==========")
        defer
        {
            string msg = "defer-local-msg"
            global.println("defer local var: " + msg)
        }
        global.println("body end")
    }

    # ── 11. 多个 defer 交替执行（验证 LIFO 和变量捕获） ──
    static deferCaptureTest()
    {
        global.println("========== 11. defer capture ==========")
        string log = "start"
        defer
        {
            global.println("defer 1: " + log)
        }
        log = log + "-mid"
        defer
        {
            global.println("defer 2: " + log)
        }
        log = log + "-end"
        global.println("body: " + log)
    }

    # ── 12. 无 defer 的普通函数（对照基准） ──
    static noDeferTest()
    {
        global.println("========== 12. no defer ==========")
        global.println("plain function, no defer")
    }

    # ── main entry ──
    static fun()
    {
        deferBasicTest()
        deferAfterReturnTest()
        deferLifoTest()
        deferMultiReturnTest()
        deferModifyVarTest()
        deferAtEndTest()
        deferInIfBlockTest()
        deferCallsFunctionTest()
        deferInLoopTest()
        deferLocalVarTest()
        deferCaptureTest()
        noDeferTest()
        global.println("========== all defer tests done ==========")
    }
}
