# Watcher 独立监视类测试（DEBUG_SYSTEM_DESIGN.md §6 + §13.7 P7 验收）
# 纯 SL 实现：零 VM 改动，编译/运行与 optimize 等级完全无关。
# 覆盖: create 初值不触发 / update 变化触发+返回值 / 未变化返回 false /
#       last/value/changed getter 链 / 多回调绑定 / unlisten 按引用解绑 /
#       emit 手动触发(不改 changed) / 回调内 update 重入保护 + 链后复位 /
#       string 值监视 / data 成员监视 / data 实例引用比较语义

data WatcherPointData
{
    x = 0
    y = 0
}

WatcherTest
{
    static fun()
    {
        SystemPrintln("========== WatcherTest (start) ==========")

        # ---- W1 create: 初值不触发信号, getter 初始态 ----
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

        # ---- W2~W4 update 变化检测 + 回调触发 + getter 链 ----
        SystemPrintln("--- W2/W3/W4 update & getters ---")
        int bFired = 0
        string bLog = ""
        var wb = Watcher<int>.create( 0 )
        function onBasic()
        {
            bFired = bFired + 1
            bLog = bLog + "[" + wb.last.toString() + "->" + wb.value.toString() + "]"
        }
        wb.listen( onBasic )
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
        if (bFired == 2)
        {
            SystemPrintln("W2 callback fired on change only: OK")
        }
        else
        {
            SystemPrintln("W2 callback fired on change only: FAIL (bFired=" + bFired.toString() + ")")
        }
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

        # ---- W5/W6 多回调绑定 + unlisten 按引用解绑 ----
        SystemPrintln("--- W5/W6 listen multi & unlisten ---")
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

        # ---- W7 emit 手动触发: 无视变化, 不改 changed ----
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

        # ---- W8 回调内 update 重入保护 + 链结束复位 ----
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

        # ---- W9 string 值监视（== 值比较）----
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

        # ---- W10 data 成员监视（每个 update 点传成员值，§6.3）----
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

        # ---- W11 data 实例引用比较（== 对类实例按引用，§6.3）----
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

        SystemPrintln("========== WatcherTest (end) ==========")
    }
}
