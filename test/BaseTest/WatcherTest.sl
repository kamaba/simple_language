# Watcher 独立监视类测试（DEBUG_SYSTEM_DESIGN.md §6 + §13.7 P7 验收）
# 纯 SL 实现：零 VM 改动，编译/运行与 optimize 等级完全无关。
# 覆盖: create 初值不触发 / update 变化触发+返回值 / 未变化返回 false /
#       last/value/changed getter 链 / 多回调绑定 / unlisten 按引用解绑 /
#       emit 手动触发(不改 changed) / 回调内 update 重入保护 + 链后复位 /
#       string 值监视 / data 成员监视 / data 实例引用比较语义
# 布局: 用例按 W1~W11 拆分为独立方法 (原 W2/W3/W4、W5/W6 合组已细分为
#       自包含方法, 可独立运行, 断言输出与拆分前一致);
# 单跑: csimple_lang run ProjectTest.module.json -- w3   (只跑 W3; 无参数全跑)

data WatcherPointData
{
    x = 0
    y = 0
}

WatcherTest
{
    static fun()
    {
        # 分发: 无 CLI 参数时全跑 (ProjectTest 全量回归路径);
        # 带 -- wN 时只跑指定组 (global._inputArgs 为系统注入静态成员,
        # Array<Object>, 无参数时为空数组, 详见 InputArgsTest)
        inputArgs = global._inputArgs
        string only = ""
        if inputArgs.length > 0
        {
            only = inputArgs[0].toString()
        }
        SystemPrintln("========== WatcherTest (start) ==========")
        if want(only, "w1")
        {
            w1Create()
        }
        if want(only, "w2")
        {
            w2UpdateReturn()
        }
        if want(only, "w3")
        {
            w3CallbackOnChange()
        }
        if want(only, "w4")
        {
            w4GetterChain()
        }
        if want(only, "w5")
        {
            w5MultiCallbacks()
        }
        if want(only, "w6")
        {
            w6Unlisten()
        }
        if want(only, "w7")
        {
            w7Emit()
        }
        if want(only, "w8")
        {
            w8ReentryGuard()
        }
        if want(only, "w9")
        {
            w9StringWatcher()
        }
        if want(only, "w10")
        {
            w10MemberWatcher()
        }
        if want(only, "w11")
        {
            w11ReferenceWatcher()
        }
        SystemPrintln("========== WatcherTest (end) ==========")
    }

    # 分发辅助: only 为空全跑, 否则精确匹配组号
    static bool want(string only, string id)
    {
        if only == ""
        {
            ret true
        }
        ret only == id
    }

    # ---- W1 create: 初值不触发信号, getter 初始态 ----
    static void w1Create()
    {
        SystemPrintln("--- W1 create initial state ---")
        int fired = 0
        var w = Watcher<int>.create( 0 )
        function onW1()
        {
            fired = fired + 1
        }
        w.listen( onW1 )
        if (w.value == 0 && w.last == 0 && w.changed == false)
        {
            SystemPrintln("W1 initial getters: OK")
        }
        else
        {
            SystemPrintln("W1 initial getters: FAIL (value=" + w.value.toString() + " last=" + w.last.toString() + " changed=" + w.changed.toString() + ")")
        }
        if (fired == 0)
        {
            SystemPrintln("W1 create does not fire: OK")
        }
        else
        {
            SystemPrintln("W1 create does not fire: FAIL (fired=" + fired.toString() + ")")
        }
    }

    # ---- W2 update 返回值: 变化 true / 未变化 false ----
    static void w2UpdateReturn()
    {
        SystemPrintln("--- W2 update return values ---")
        var wb = Watcher<int>.create( 0 )
        bool u1 = wb.update( 5 )
        bool u2 = wb.update( 5 )
        bool u3 = wb.update( 9 )
        if (u1 == true && u2 == false && u3 == true)
        {
            SystemPrintln("W2/W3 update return values: OK")
        }
        else
        {
            SystemPrintln("W2/W3 update return values: FAIL (" + u1.toString() + "," + u2.toString() + "," + u3.toString() + ")")
        }
    }

    # ---- W3 回调仅在值变化时触发 ----
    static void w3CallbackOnChange()
    {
        SystemPrintln("--- W3 callback on change only ---")
        int bFired = 0
        var wb = Watcher<int>.create( 0 )
        function onBasic()
        {
            bFired = bFired + 1
        }
        wb.listen( onBasic )
        wb.update( 5 )
        wb.update( 5 )
        wb.update( 9 )
        if (bFired == 2)
        {
            SystemPrintln("W2 callback fired on change only: OK")
        }
        else
        {
            SystemPrintln("W2 callback fired on change only: FAIL (bFired=" + bFired.toString() + ")")
        }
    }

    # ---- W4 回调内 last/value 链 + 更新后 getter ----
    static void w4GetterChain()
    {
        SystemPrintln("--- W4 last/value chain & getters ---")
        string bLog = ""
        var wb = Watcher<int>.create( 0 )
        function onChain()
        {
            bLog = bLog + "[" + wb.last.toString() + "->" + wb.value.toString() + "]"
        }
        wb.listen( onChain )
        wb.update( 5 )
        wb.update( 9 )
        if (bLog == "[0->5][5->9]")
        {
            SystemPrintln("W4 last/value chain in callback: OK")
        }
        else
        {
            SystemPrintln("W4 last/value chain in callback: FAIL (bLog=" + bLog + ")")
        }
        if (wb.value == 9 && wb.last == 5 && wb.changed == true)
        {
            SystemPrintln("W4 getters after updates: OK")
        }
        else
        {
            SystemPrintln("W4 getters after updates: FAIL")
        }
    }

    # ---- W5 多回调绑定: 同一 watcher 上多个回调都触发 ----
    static void w5MultiCallbacks()
    {
        SystemPrintln("--- W5 multi callbacks ---")
        int m1Count = 0
        int m2Count = 0
        var wm = Watcher<int>.create( 0 )
        function multiA()
        {
            m1Count = m1Count + 1
        }
        function multiB()
        {
            m2Count = m2Count + 1
        }
        wm.listen( multiA )
        wm.listen( multiB )
        wm.update( 1 )
        if (m1Count == 1 && m2Count == 1)
        {
            SystemPrintln("W5 multi callbacks all fired: OK")
        }
        else
        {
            SystemPrintln("W5 multi callbacks all fired: FAIL (" + m1Count.toString() + "," + m2Count.toString() + ")")
        }
    }

    # ---- W6 unlisten 按引用解绑: true/false 返回 + 解绑后不再触发 ----
    static void w6Unlisten()
    {
        SystemPrintln("--- W6 unlisten ---")
        int m1Count = 0
        int m2Count = 0
        var wm = Watcher<int>.create( 0 )
        function multiA()
        {
            m1Count = m1Count + 1
        }
        function multiB()
        {
            m2Count = m2Count + 1
        }
        wm.listen( multiA )
        wm.listen( multiB )
        wm.update( 1 )
        bool ub1 = wm.unlisten( multiA )
        bool ub2 = wm.unlisten( multiA )
        wm.update( 2 )
        if (ub1 == true && ub2 == false)
        {
            SystemPrintln("W6 unlisten returns: OK")
        }
        else
        {
            SystemPrintln("W6 unlisten returns: FAIL (" + ub1.toString() + "," + ub2.toString() + ")")
        }
        if (m1Count == 1 && m2Count == 2)
        {
            SystemPrintln("W6 unbound callback not fired: OK")
        }
        else
        {
            SystemPrintln("W6 unbound callback not fired: FAIL (" + m1Count.toString() + "," + m2Count.toString() + ")")
        }
    }

    # ---- W7 emit 手动触发: 无视变化, 不改 changed ----
    static void w7Emit()
    {
        SystemPrintln("--- W7 emit ---")
        int eFired = 0
        var we = Watcher<int>.create( 10 )
        function onEmit()
        {
            eFired = eFired + 1
        }
        we.listen( onEmit )
        we.emit()
        bool e1 = we.update( 10 )
        we.emit()
        if (eFired == 2 && e1 == false && we.changed == false)
        {
            SystemPrintln("W7 emit fires without change (changed untouched): OK")
        }
        else
        {
            SystemPrintln("W7 emit fires without change (changed untouched): FAIL (eFired=" + eFired.toString() + " e1=" + e1.toString() + " changed=" + we.changed.toString() + ")")
        }
    }

    # ---- W8 回调内 update 重入保护 + 链结束复位 ----
    static void w8ReentryGuard()
    {
        SystemPrintln("--- W8 re-entry guard ---")
        int chainCount = 0
        var w2 = Watcher<int>.create( 0 )
        function onReenter()
        {
            chainCount = chainCount + 1
            bool inner = w2.update( 99 )
            if (inner)
            {
                SystemPrintln("W8 inner update returns true: OK")
            }
            else
            {
                SystemPrintln("W8 inner update returns true: FAIL")
            }
        }
        w2.listen( onReenter )
        w2.update( 5 )
        if (chainCount == 1)
        {
            SystemPrintln("W8 no re-entry inside chain: OK")
        }
        else
        {
            SystemPrintln("W8 no re-entry inside chain: FAIL (chainCount=" + chainCount.toString() + ")")
        }
        if (w2.value == 99 && w2.last == 5)
        {
            SystemPrintln("W8 inner update still writes value/last: OK")
        }
        else
        {
            SystemPrintln("W8 inner update still writes value/last: FAIL (value=" + w2.value.toString() + " last=" + w2.last.toString() + ")")
        }
        w2.update( 100 )
        if (chainCount == 2)
        {
            SystemPrintln("W8 guard reset after chain: OK")
        }
        else
        {
            SystemPrintln("W8 guard reset after chain: FAIL (chainCount=" + chainCount.toString() + ")")
        }
    }

    # ---- W9 string 值监视（== 值比较）----
    static void w9StringWatcher()
    {
        SystemPrintln("--- W9 string watcher ---")
        int sFired = 0
        var ws = Watcher<string>.create( "a" )
        function onStr()
        {
            sFired = sFired + 1
        }
        ws.listen( onStr )
        bool s1 = ws.update( "b" )
        bool s2 = ws.update( "b" )
        bool s3 = ws.update( "a" )
        if (s1 == true && s2 == false && s3 == true && sFired == 2)
        {
            SystemPrintln("W9 string value comparison: OK")
        }
        else
        {
            SystemPrintln("W9 string value comparison: FAIL (" + s1.toString() + "," + s2.toString() + "," + s3.toString() + " fired=" + sFired.toString() + ")")
        }
    }

    # ---- W10 data 成员监视（每个 update 点传成员值，§6.3）----
    static void w10MemberWatcher()
    {
        SystemPrintln("--- W10 data member watcher ---")
        WatcherPointData md = WatcherPointData()
        md.x = 3
        int dFired = 0
        var wx = Watcher<int>.create( md.x )
        function onMember()
        {
            dFired = dFired + 1
        }
        wx.listen( onMember )
        md.x = 10
        bool d1 = wx.update( md.x )
        md.x = 10
        bool d2 = wx.update( md.x )
        md.x = 7
        bool d3 = wx.update( md.x )
        if (d1 == true && d2 == false && d3 == true && dFired == 2)
        {
            SystemPrintln("W10 data member watch: OK")
        }
        else
        {
            SystemPrintln("W10 data member watch: FAIL (" + d1.toString() + "," + d2.toString() + "," + d3.toString() + " fired=" + dFired.toString() + ")")
        }
    }

    # ---- W11 data 实例引用比较（== 对类实例按引用，§6.3）----
    static void w11ReferenceWatcher()
    {
        SystemPrintln("--- W11 reference comparison ---")
        WatcherPointData ra = WatcherPointData()
        var wr = Watcher<WatcherPointData>.create( ra )
        int rFired = 0
        function onRef()
        {
            rFired = rFired + 1
        }
        wr.listen( onRef )
        WatcherPointData rb = WatcherPointData()
        rb.x = 99
        bool q1 = wr.update( rb )
        bool q2 = wr.update( rb )
        bool q3 = wr.update( ra )
        if (q1 == true && q2 == false && q3 == true && rFired == 2)
        {
            SystemPrintln("W11 reference comparison: OK")
        }
        else
        {
            SystemPrintln("W11 reference comparison: FAIL (" + q1.toString() + "," + q2.toString() + "," + q3.toString() + " fired=" + rFired.toString() + ")")
        }
    }
}
