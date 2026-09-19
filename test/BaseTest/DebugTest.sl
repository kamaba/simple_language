# Debug 采样系统测试（DEBUG_SYSTEM_DESIGN.md v4 §13.1 P1 + §13.2 P2 验收）
# P1 覆盖: T1 单值监视 / T2 字符串监视 / T3 data 成员监视 / 帧查询 /
#          截断标注 ...(len=N) / 环形缓冲覆盖最旧帧
# P2 覆盖: begin/end/scopeChain 区间嵌套 / 异常回卷 / stats 区间统计 /
#          setSkip/pause/enable 三种跳过路径 / find 标签+scope 前缀 /
#          series 值序列 / mark 行边界 / diff 帧对比
# P3 覆盖: watchAssert 违规计数 + kind=3 违规帧 / watchIn 调用链过滤
# P4 覆盖: listen 全帧订阅时序 / 单标签过滤 + mark 触发 / 回调内
#          lastFrame / 重入保护(回调内落帧只入缓冲) / 违规帧通知 /
#          表满 -1 / unlisten 静默 / 重复 unlisten false
# P5 覆盖: stack 语句级空栈 + 表达式快照 / frames 栈顶帧方法名行号 /
#          frameData locals+args 行 / frameVar 本帧+跨帧+args+未找到 /
#          越界降级 / 协程内帧链独立
# 前提: ProjectTest.jsonc compile.debug=true → Debug.* 特译 opcode 119/120/121;
#       optimize=false → optimizeLevel<2 → VM 采样开启。

enum DebugTestError extends Error
{
    ScopeBoom = { code = 90, message = "scope-boom" }
}

data DebugScoreData
{
    math = 0
    english = 0
}

DebugTest
{
    static fun()
    {
        SystemPrintln("========== DebugTest (start) ==========")

        # ---- 1. T1 单值监视: 标量入帧, frameCount 递增 ----
        SystemPrintln("--- T1 scalar watch ---")
        Int32 before = Debug.frameCount()
        Int32 answer = 42
        Debug.watch( answer, "answer" )
        Int32 after = Debug.frameCount()
        SystemPrintln("frameCount before=" + before.toString() + " after=" + after.toString())
        if (after == before + 1)
        {
            SystemPrintln("T1 frameCount increments: OK")
        }
        else
        {
            SystemPrintln("T1 frameCount increments: FAIL")
        }
        string f0 = Debug.lastFrame()
        SystemPrintln("lastFrame = [" + f0 + "]")
        if (f0 == "[watch] answer = 42")
        {
            SystemPrintln("T1 lastFrame text: OK")
        }
        else
        {
            SystemPrintln("T1 lastFrame text: FAIL")
        }

        # ---- 2. T2 字符串监视 ----
        SystemPrintln("--- T2 string watch ---")
        string name = "hello"
        Debug.watch( name, "name" )
        string f1 = Debug.lastFrame()
        SystemPrintln("lastFrame = [" + f1 + "]")
        if (f1 == "[watch] name = hello")
        {
            SystemPrintln("T2 string frame text: OK")
        }
        else
        {
            SystemPrintln("T2 string frame text: FAIL")
        }

        # ---- 3. getFrame 按索引回看 + 越界空串 ----
        SystemPrintln("--- getFrame by index ---")
        string g0 = Debug.getFrame( after - 1 )
        SystemPrintln("getFrame(" + (after - 1).toString() + ") = [" + g0 + "]")
        if (g0 == "[watch] answer = 42")
        {
            SystemPrintln("getFrame history: OK")
        }
        else
        {
            SystemPrintln("getFrame history: FAIL")
        }
        string outOfRange = Debug.getFrame( 99999 )
        if (outOfRange == "")
        {
            SystemPrintln("getFrame out-of-range returns empty: OK")
        }
        else
        {
            SystemPrintln("getFrame out-of-range returns empty: FAIL")
        }

        # ---- 4. T3 data 成员监视 ----
        SystemPrintln("--- T3 data member watch ---")
        DebugScoreData sd = DebugScoreData()
        sd.math = 95
        sd.english = 88
        Debug.watch( sd, "math", "sd.math" )
        string fm = Debug.lastFrame()
        SystemPrintln("lastFrame = [" + fm + "]")
        if (fm == "[watch] sd.math = 95")
        {
            SystemPrintln("T3 member frame text: OK")
        }
        else
        {
            SystemPrintln("T3 member frame text: FAIL")
        }
        # 整表监视 (mode 0): data 实例文本化
        Debug.watch( sd, "sd" )
        string fd = Debug.lastFrame()
        SystemPrintln("sd whole frame len = " + fd.length.toString())
        if (fd.length > 0)
        {
            SystemPrintln("T2 data whole frame non-empty: OK")
        }
        else
        {
            SystemPrintln("T2 data whole frame non-empty: FAIL")
        }

        # ---- 5. 截断: setMaxTextLength 后超长文本标注 ...(len=N) ----
        SystemPrintln("--- truncation ---")
        Debug.setMaxTextLength( 16 )
        string longText = "0123456789ABCDEFGHIJ"
        Debug.watch( longText, "long" )
        string ft = Debug.lastFrame()
        SystemPrintln("lastFrame = [" + ft + "]")
        if (ft == "[watch] long = 0123456789ABCDEF...(len=20)")
        {
            SystemPrintln("truncation marks ...(len=N): OK")
        }
        else
        {
            SystemPrintln("truncation marks ...(len=N): FAIL")
        }
        # 恢复默认, 不影响后续
        Debug.setMaxTextLength( 0 )

        # ---- 5.5 边界补充: null / bool / float / 字面量 / 快照语义 ----
        SystemPrintln("--- edge cases ---")
        DebugScoreData nd = null
        Debug.watch( nd, "nd" )
        string fn = Debug.lastFrame()
        if (fn == "[watch] nd = null")
        {
            SystemPrintln("null watch text: OK")
        }
        else
        {
            SystemPrintln("null watch text: FAIL")
        }
        bool flag = true
        Debug.watch( flag, "flag" )
        if (Debug.lastFrame() == "[watch] flag = true")
        {
            SystemPrintln("bool watch text: OK")
        }
        else
        {
            SystemPrintln("bool watch text: FAIL")
        }
        Float64 pi = 3.5
        Debug.watch( pi, "pi" )
        if (Debug.lastFrame() == "[watch] pi = 3.5")
        {
            SystemPrintln("float watch text: OK")
        }
        else
        {
            SystemPrintln("float watch text: FAIL")
        }
        Debug.watch( "literal", "lit" )
        if (Debug.lastFrame() == "[watch] lit = literal")
        {
            SystemPrintln("literal watch text: OK")
        }
        else
        {
            SystemPrintln("literal watch text: FAIL")
        }
        # 快照语义(§11): 帧持克隆副本, watch 后修改原值不影响已落帧
        Int32 snap = 2
        Debug.watch( snap, "snap" )
        snap = 99
        if (Debug.getFrame( Debug.frameCount() - 1 ) == "[watch] snap = 2")
        {
            SystemPrintln("snapshot semantics: OK")
        }
        else
        {
            SystemPrintln("snapshot semantics: FAIL")
        }

        # ---- 6. 环形缓冲: 超容量覆盖最旧帧, count 封顶 ----
        SystemPrintln("--- ring buffer overflow ---")
        Int32 capBefore = Debug.frameCount()
        int i = 0
        while (i < 1100)
        {
            Debug.watch( i, "ring" )
            i = i + 1
        }
        Int32 capAfter = Debug.frameCount()
        SystemPrintln("frameCount before=" + capBefore.toString() + " after=" + capAfter.toString())
        if (capAfter == 1024)
        {
            SystemPrintln("ring caps at 1024: OK")
        }
        else
        {
            SystemPrintln("ring caps at 1024: FAIL")
        }
        string newest = Debug.lastFrame()
        SystemPrintln("newest = [" + newest + "]")
        if (newest == "[watch] ring = 1099")
        {
            SystemPrintln("ring newest frame survives: OK")
        }
        else
        {
            SystemPrintln("ring newest frame survives: FAIL")
        }
        # 最旧帧应是被覆盖后的一帧 (ring=76 左右, 不再是 ring=0)
        string oldest = Debug.getFrame( 0 )
        SystemPrintln("oldest = [" + oldest + "]")
        if (oldest != "[watch] ring = 0")
        {
            SystemPrintln("ring oldest overwritten: OK")
        }
        else
        {
            SystemPrintln("ring oldest overwritten: FAIL")
        }

        # ---- 7. P2 区间嵌套: begin/end/scopeChain ----
        SystemPrintln("--- P2 scope chain nesting ---")
        Debug.begin( "p2main" )
        if (Debug.scopeChain() == "p2main")
        {
            SystemPrintln("begin enters chain: OK")
        }
        else
        {
            SystemPrintln("begin enters chain: FAIL [" + Debug.scopeChain() + "]")
        }
        Debug.begin( "p2loop" )
        if (Debug.scopeChain() == "p2main > p2loop")
        {
            SystemPrintln("nested chain joins with ' > ': OK")
        }
        else
        {
            SystemPrintln("nested chain joins with ' > ': FAIL [" + Debug.scopeChain() + "]")
        }
        Debug.end( "p2loop" )
        if (Debug.scopeChain() == "p2main")
        {
            SystemPrintln("end pops inner scope: OK")
        }
        else
        {
            SystemPrintln("end pops inner scope: FAIL [" + Debug.scopeChain() + "]")
        }
        Debug.end( "p2main" )
        if (Debug.scopeChain() == "")
        {
            SystemPrintln("end pops to empty: OK")
        }
        else
        {
            SystemPrintln("end pops to empty: FAIL [" + Debug.scopeChain() + "]")
        }

        # ---- 8. P2 异常回卷: 被调方法内 begin 未配对 end, throw 后由帧弹出回卷 ----
        SystemPrintln("--- P2 exception unwind ---")
        Debug.begin( "safeOuter" )
        label unwindBlock
        {
            try DebugTest.scopeThrow()
        }
        catch
        {
            SystemPrintln("exception caught")
        }
        if (Debug.scopeChain() == "safeOuter")
        {
            SystemPrintln("unwind pops callee scopes: OK")
        }
        else
        {
            SystemPrintln("unwind pops callee scopes: FAIL [" + Debug.scopeChain() + "]")
        }
        Debug.end( "safeOuter" )
        if (Debug.scopeChain() == "")
        {
            SystemPrintln("outer scope still closable: OK")
        }
        else
        {
            SystemPrintln("outer scope still closable: FAIL [" + Debug.scopeChain() + "]")
        }

        # ---- 9. P2 采样控制三路径: setSkip / pause / disable ----
        # 环形缓冲已在测试 6 填满(1024)，frameCount 不再变化，
        # 改用 find 计数验证（跳过生效 = 该 label 只落 1 帧）
        SystemPrintln("--- P2 sampling controls ---")
        Int32 skipVal = 7
        Debug.setSkip( 2 )
        Debug.watch( skipVal, "skipfx" )
        Debug.watch( skipVal, "skipfx" )
        Debug.watch( skipVal, "skipfx" )
        Array<Int32> skipHits = Debug.find( "skipfx" )
        if (skipHits.length == 1)
        {
            SystemPrintln("setSkip skips first 2 hits: OK")
        }
        else
        {
            SystemPrintln("setSkip skips first 2 hits: FAIL")
        }
        Debug.setSkip( 0 )

        Debug.pause()
        Debug.watch( skipVal, "pfx" )
        Debug.resume()
        Debug.watch( skipVal, "pfx" )
        Array<Int32> pauseHits = Debug.find( "pfx" )
        if (pauseHits.length == 1)
        {
            SystemPrintln("pause blocks then resume restores: OK")
        }
        else
        {
            SystemPrintln("pause blocks then resume restores: FAIL")
        }

        Debug.disable( "dfx" )
        Debug.watch( skipVal, "dfx" )
        Debug.enable( "dfx" )
        Debug.watch( skipVal, "dfx" )
        Array<Int32> disableHits = Debug.find( "dfx" )
        if (disableHits.length == 1)
        {
            SystemPrintln("disable blocks single label only: OK")
        }
        else
        {
            SystemPrintln("disable blocks single label only: FAIL")
        }

        # ---- 10. P2 find: 标签 + scope 段级前缀过滤 ----
        SystemPrintln("--- P2 find by label + scope prefix ---")
        Debug.watch( skipVal, "ofx" )
        Debug.begin( "o1" )
        Debug.watch( 1, "fx2" )
        Debug.begin( "o2" )
        Debug.watch( 2, "fx2" )
        Debug.end( "o2" )
        Debug.end( "o1" )
        Array<Int32> allFx2 = Debug.find( "fx2" )
        Array<Int32> inO1 = Debug.find( "fx2", "o1" )
        Array<Int32> inO2 = Debug.find( "fx2", "o2" )
        Array<Int32> inNoSuch = Debug.find( "fx2", "nosuch" )
        SystemPrintln("find all=" + allFx2.length.toString()
            + " o1=" + inO1.length.toString()
            + " o2=" + inO2.length.toString()
            + " nosuch=" + inNoSuch.length.toString())
        if (allFx2.length == 2 && inO1.length == 2 && inO2.length == 0 && inNoSuch.length == 0)
        {
            SystemPrintln("find label + scope-segment prefix: OK")
        }
        else
        {
            SystemPrintln("find label + scope-segment prefix: FAIL")
        }

        # ---- 11. P2 series: 标签值序列 ----
        SystemPrintln("--- P2 series ---")
        Array<string> fx2Values = Debug.series( "fx2" )
        if (fx2Values.length == 2 && fx2Values[0].length > 0 && fx2Values[1].length > 0)
        {
            SystemPrintln("series returns per-label value list: OK")
        }
        else
        {
            SystemPrintln("series returns per-label value list: FAIL")
        }

        # ---- 12. P2 mark: 行边界帧也按 label 可查 ----
        SystemPrintln("--- P2 mark ---")
        Debug.mark( "m1" )
        Array<Int32> marks = Debug.find( "m1" )
        if (marks.length == 1)
        {
            SystemPrintln("mark lands findable frame: OK")
        }
        else
        {
            SystemPrintln("mark lands findable frame: FAIL")
        }

        # ---- 13. P2 stats: 区间耗时统计 JSON ----
        SystemPrintln("--- P2 interval stats ---")
        Debug.begin( "loopStats" )
        int spin = 0
        while (spin < 1000)
        {
            spin = spin + 1
        }
        Debug.end( "loopStats" )
        string statJson = Debug.stats( "loopStats" )
        SystemPrintln("stats = [" + statJson + "]")
        # stats JSON 形如 {"label":"loopStats","count":N,...}：
        # 0-based 位置 10..18 恰为 label 名，slice 半开区间比对
        if (statJson.length > 19 && statJson.slice(10, 19) == "loopStats")
        {
            SystemPrintln("stats JSON contains label: OK")
        }
        else
        {
            SystemPrintln("stats JSON contains label: FAIL")
        }

        # ---- 14. P2 diff: 相邻帧行级对比（帧持有原始克隆，不受截断影响） ----
        SystemPrintln("--- P2 diff ---")
        # mode 0 整对象 watch：帧 value 为对象克隆（diff 仅支持对象帧，
        # mode 1 成员监视的帧 value 是标量，不进 diff 对比）
        DebugScoreData dd = DebugScoreData()
        dd.math = 95
        Debug.watch( dd, "dd" )
        dd.math = 100
        Debug.watch( dd, "dd" )
        string df = Debug.diff( Debug.frameCount() - 1 )
        SystemPrintln("diff = [" + df + "]")
        # 行级对比输出 "-旧行" 在前 "+新行" 在后；克隆语义使旧帧仍见 95
        if (df.front(1) == "-" )
        {
            SystemPrintln("diff shows +new/-old lines: OK")
        }
        else
        {
            SystemPrintln("diff shows +new/-old lines: FAIL")
        }

        # ---- 15. P3 watchAssert: 违规计数 + kind=3 违规帧 ----
        SystemPrintln("--- P3 watchAssert ---")
        Int32 viol0 = Debug.assertViolations()
        Debug.watchAssert( 100, "ax", 100 >= 50 )    # cond=true: 照常落 watch 帧
        if (Debug.lastFrame() == "[watch] ax = 100")
        {
            SystemPrintln("watchAssert pass lands watch frame: OK")
        }
        else
        {
            SystemPrintln("watchAssert pass lands watch frame: FAIL")
        }
        Debug.watchAssert( -1, "axbad", 2 > 3 )      # cond=false: 违规帧 kind=3
        if (Debug.lastFrame() == "[assert] axbad = -1")
        {
            SystemPrintln("watchAssert violation lands assert frame: OK")
        }
        else
        {
            SystemPrintln("watchAssert violation lands assert frame: FAIL")
        }
        Debug.watchAssert( 2, "axbad2", false )      # 字面量 cond=false: 第二次违规
        Int32 viol1 = Debug.assertViolations()
        SystemPrintln("assertViolations before=" + viol0.toString() + " after=" + viol1.toString())
        if (viol0 == 0 && viol1 == 2)
        {
            SystemPrintln("assertViolations counts only false conds: OK")
        }
        else
        {
            SystemPrintln("assertViolations counts only false conds: FAIL")
        }
        # 违规/命中帧均按 label 可查 (find 不分 kind)
        Array<Int32> axHits = Debug.find( "ax" )
        Array<Int32> axbadHits = Debug.find( "axbad" )
        if (axHits.length == 1 && axbadHits.length == 1)
        {
            SystemPrintln("assert frames findable by label: OK")
        }
        else
        {
            SystemPrintln("assert frames findable by label: FAIL")
        }

        # ---- 16. P3 watchIn: 只在目标调用链上采样 ----
        SystemPrintln("--- P3 watchIn ---")
        # 实例方法: M4 自动 inline 只收 static, 帧可靠落在调用链上
        DebugTest dt = DebugTest()
        dt.inTarget()      # 链含 inTarget: wix/wix2 各落 1 帧
        dt.outOther()     # 链不含 inTarget: 不落帧
        Array<Int32> wixHits = Debug.find( "wix" )
        SystemPrintln("wix hits=" + wixHits.length.toString())
        if (wixHits.length == 1)
        {
            SystemPrintln("watchIn samples only on target chain: OK")
        }
        else
        {
            SystemPrintln("watchIn samples only on target chain: FAIL")
        }
        if (wixHits.length == 1 && Debug.getFrame( wixHits[0] ) == "[watch] wix = 5")
        {
            SystemPrintln("watchIn frame text: OK")
        }
        else
        {
            SystemPrintln("watchIn frame text: FAIL")
        }
        # caller 也可匹配类型全名 (declaring_type_full_name 子串)
        Array<Int32> wix2Hits = Debug.find( "wix2" )
        if (wix2Hits.length == 1)
        {
            SystemPrintln("watchIn matches declaring type name: OK")
        }
        else
        {
            SystemPrintln("watchIn matches declaring type name: FAIL")
        }

        # ---- 17. P4 全帧订阅: 每帧触发一次, unlisten 后静默 ----
        SystemPrintln("--- P4 listen all frames ---")
        int hit = 0
        function onFrame()
        {
            hit = hit + 1
        }
        int id1 = Debug.listen( onFrame )
        SystemPrintln("listen id = " + id1.toString())
        if (id1 >= 1)
        {
            SystemPrintln("listen returns positive id: OK")
        }
        else
        {
            SystemPrintln("listen returns positive id: FAIL")
        }
        Debug.watch( 1, "p4a" )
        Debug.watch( 2, "p4a" )
        if (hit == 2)
        {
            SystemPrintln("all-frame listener fires per frame: OK")
        }
        else
        {
            SystemPrintln("all-frame listener fires per frame: FAIL hit=" + hit.toString())
        }
        if (Debug.unlisten( id1 ))
        {
            SystemPrintln("unlisten returns true: OK")
        }
        else
        {
            SystemPrintln("unlisten returns true: FAIL")
        }
        Debug.watch( 3, "p4a" )
        if (hit == 2)
        {
            SystemPrintln("listener silent after unlisten: OK")
        }
        else
        {
            SystemPrintln("listener silent after unlisten: FAIL hit=" + hit.toString())
        }

        # ---- 18. P4 单标签订阅: 标签过滤 + mark 帧触发 ----
        SystemPrintln("--- P4 listen by label ---")
        int tagHit = 0
        function onTag()
        {
            tagHit = tagHit + 1
        }
        int id2 = Debug.listen( "p4tag", onTag )
        Debug.watch( 1, "p4tag" )
        Debug.watch( 2, "p4other" )
        Debug.mark( "p4tag" )
        Debug.mark( "p4other" )
        if (tagHit == 2)
        {
            SystemPrintln("label listener filters + mark fires: OK")
        }
        else
        {
            SystemPrintln("label listener filters + mark fires: FAIL hit=" + tagHit.toString())
        }
        Debug.unlisten( id2 )

        # ---- 19. P4 回调内 lastFrame: 即刚落的帧 ----
        SystemPrintln("--- P4 lastFrame inside callback ---")
        string seen = ""
        function onSeen()
        {
            seen = Debug.lastFrame()
        }
        int id3 = Debug.listen( onSeen )
        Debug.watch( 7, "seen1" )
        if (seen == "[watch] seen1 = 7")
        {
            SystemPrintln("callback sees fresh frame via lastFrame: OK")
        }
        else
        {
            SystemPrintln("callback sees fresh frame via lastFrame: FAIL [" + seen + "]")
        }
        Debug.unlisten( id3 )

        # ---- 20. P4 重入保护: 回调内落帧只入缓冲不递归触发 ----
        SystemPrintln("--- P4 reentry guard ---")
        int reHit = 0
        function onRe()
        {
            reHit = reHit + 1
            Debug.watch( 99, "nested" )
        }
        int id4 = Debug.listen( onRe )
        Debug.watch( 7, "re1" )
        if (reHit == 1)
        {
            SystemPrintln("reentry guard blocks nested notify: OK")
        }
        else
        {
            SystemPrintln("reentry guard blocks nested notify: FAIL hit=" + reHit.toString())
        }
        Array<Int32> nestedHits = Debug.find( "nested" )
        if (nestedHits.length == 1)
        {
            SystemPrintln("nested frame still buffered: OK")
        }
        else
        {
            SystemPrintln("nested frame still buffered: FAIL len=" + nestedHits.length.toString())
        }
        Debug.unlisten( id4 )

        # ---- 21. P4 违规帧(kind=3)也通知订阅者 ----
        SystemPrintln("--- P4 assert frame notifies ---")
        int axHit = 0
        function onAx()
        {
            axHit = axHit + 1
        }
        int id5 = Debug.listen( onAx )
        Debug.watch( 1, "axt" )
        Debug.watchAssert( -1, "axbad", 2 > 3 )
        if (axHit == 2)
        {
            SystemPrintln("assert violation frame notifies: OK")
        }
        else
        {
            SystemPrintln("assert violation frame notifies: FAIL hit=" + axHit.toString())
        }
        Debug.unlisten( id5 )

        # ---- 22. P4 表满: 容量 16, 第 17 次 -1; 重复 unlisten false ----
        SystemPrintln("--- P4 table full + double unlisten ---")
        function fillCb()
        {
        }
        Array<Int32> ids = Array<Int32>( 20 )
        int okCount = 0
        int j = 0
        while (j < 20)
        {
            int r = Debug.listen( fillCb )
            ids[j] = r
            if (r > 0)
            {
                okCount = okCount + 1
            }
            j = j + 1
        }
        SystemPrintln("listen ok = " + okCount.toString())
        if (okCount == 16)
        {
            SystemPrintln("listener table caps at 16: OK")
        }
        else
        {
            SystemPrintln("listener table caps at 16: FAIL ok=" + okCount.toString())
        }
        bool firstOut = Debug.unlisten( ids[0] )
        bool secondOut = Debug.unlisten( ids[0] )
        if (firstOut && !secondOut)
        {
            SystemPrintln("double unlisten returns true then false: OK")
        }
        else
        {
            SystemPrintln("double unlisten returns true then false: FAIL")
        }
        # 清退剩余订阅, 保持监听表干净
        j = 1
        while (j < okCount)
        {
            Debug.unlisten( ids[j] )
            j = j + 1
        }

        # ---- 23. P5 StackView: stack / frames / frameData / frameVar ----
        SystemPrintln("--- P5 StackView ---")

        # 23a. stackView(): 语句级干净调用, 操作数栈为空
        string sv0 = Debug.stackView()
        SystemPrintln("stack(clean) = [" + sv0 + "]")
        if (sv0 == "[]")
        {
            SystemPrintln("P5 stack empty at statement level: OK")
        }
        else
        {
            SystemPrintln("P5 stack empty at statement level: FAIL")
        }

        # 23b. stackView(): 表达式上下文, 左操作数已压在操作数栈上
        # (SL string 在操作数栈是 PTR 槽, value 带 str: 前缀; 拼接结果带 x 前缀)
        string pre = "x"
        string sv1 = pre + Debug.stackView()
        SystemPrintln("stack(expr) = " + sv1)
        if (sv1 == "x[{\"index\":0,\"type\":\"ptr\",\"value\":\"str:x\"}]")
        {
            SystemPrintln("P5 stack snapshot sees left operand: OK")
        }
        else
        {
            SystemPrintln("P5 stack snapshot sees left operand: FAIL")
        }

        # 23c. frames(): 栈顶帧为调用方法自身, 行号为调用点行
        # (p5* 全转实例方法: M4 AutoInline 只收 static, 帧可靠落链)
        string fr = dt.p5fr()
        SystemPrintln("frames = " + fr)
        if (fr.front(35) == "[{\"index\":0,\"method\":\"p5fr\",\"line\":")
        {
            SystemPrintln("P5 frames top frame is caller: OK")
        }
        else
        {
            SystemPrintln("P5 frames top frame is caller: FAIL")
        }

        # 23d. frameData(0): 本帧 locals 行 (int-only, 按声明序)
        # (改名 p5fd: L115 已有同块 fd, 重复声明会被 Info 级拒收沿用旧值)
        string p5fd = dt.p5data()
        SystemPrintln("frameData(0) = " + p5fd)
        if (p5fd == "[{\"scope\":\"local\",\"name\":\"aa\",\"value\":\"i32:7\"},{\"scope\":\"local\",\"name\":\"bb\",\"value\":\"i32:8\"}]")
        {
            SystemPrintln("P5 frameData locals rows: OK")
        }
        else
        {
            SystemPrintln("P5 frameData locals rows: FAIL")
        }

        # 23e. frameVar(0): 本帧变量名值 (int 与 string 两种格式)
        string vv = dt.p5var()
        SystemPrintln("frameVar(0) = [" + vv + "]")
        if (vv == "i32:42|str:\"abc\"")
        {
            SystemPrintln("P5 frameVar local values: OK")
        }
        else
        {
            SystemPrintln("P5 frameVar local values: FAIL")
        }

        # 23f. frameVar(1): 非栈顶帧 (调用者 fun) 可读
        Int32 p5HostVar = 777
        string ov = dt.p5outer()
        SystemPrintln("frameVar(1,p5HostVar) = [" + ov + "]")
        if (ov == "i32:777")
        {
            SystemPrintln("P5 frameVar reads caller frame: OK")
        }
        else
        {
            SystemPrintln("P5 frameVar reads caller frame: FAIL")
        }

        # 23g. frameData(0): args 行 (locals 先 args 后)
        string fa = dt.p5arg( 9 )
        SystemPrintln("frameData(args) = " + fa)
        if (fa == "[{\"scope\":\"arg\",\"name\":\"av\",\"value\":\"i32:9\"}]")
        {
            SystemPrintln("P5 frameData arg rows: OK")
        }
        else
        {
            SystemPrintln("P5 frameData arg rows: FAIL")
        }

        # 23h. frameVar 按 args 名取值
        string fv = dt.p5argvar( 9 )
        SystemPrintln("frameVar(arg) = [" + fv + "]")
        if (fv == "i32:9")
        {
            SystemPrintln("P5 frameVar arg value: OK")
        }
        else
        {
            SystemPrintln("P5 frameVar arg value: FAIL")
        }

        # 23i. 越界与未找到: frameData "[]" / frameVar "" 降级
        string fd99 = Debug.frameData( 99 )
        string fv99 = Debug.frameVar( 99, "xx" )
        string fvnf = Debug.frameVar( 0, "p5NoSuchVar" )
        if (fd99 == "[]" && fv99 == "" && fvnf == "")
        {
            SystemPrintln("P5 oob/not-found degrade: OK")
        }
        else
        {
            SystemPrintln("P5 oob/not-found degrade: FAIL fd99=[" + fd99 + "] fv99=[" + fv99 + "] fvnf=[" + fvnf + "]")
        }

        # 23j. 协程内 frames(): 只列本协程链 (栈顶为协程方法而非 fun)
        Task ct = spawn dt.p5cf()
        string cfr = await ct as string
        SystemPrintln("coro frames = " + cfr)
        if (cfr.front(35) == "[{\"index\":0,\"method\":\"p5cf\",\"line\":")
        {
            SystemPrintln("P5 coroutine frames isolated: OK")
        }
        else
        {
            SystemPrintln("P5 coroutine frames isolated: FAIL")
        }

        # ---- 24. P6 trace 与帧表 (log/getTraceLog/frameTable, §13.6) ----
        # mark_seq 基线: 23 组已 mark 3 次 (m1/p4tag/p4other) → 未 mark 时
        # log 行号 "#3"; 24 组首个 mark row=4; 行轴含历史行 1~3 (跨全缓冲去重)
        SystemPrintln("--- P6 trace & frameTable ---")

        # 24a. log 基本落帧: 未 mark → "#3 "; fun 直调无调用者 → 链段省略
        Debug.log( "p6 first" )
        string tr1 = Debug.getTraceLog()
        SystemPrintln("trace1 = " + tr1)
        if (tr1.length > 10 && tr1.front( 3 ) == "#3 " && tr1.end( 11 ) == " | p6 first")
        {
            SystemPrintln("P6 log basic frame: OK")
        }
        else
        {
            SystemPrintln("P6 log basic frame: FAIL tr1=[" + tr1 + "]")
        }

        # 24b. 调用链摘要: 实例两层调用 (p6mid → p6logIn), 链最外层在前,
        #      只锁内层尾段 (外层可能是 _main_ > fun, 不锁定)
        dt.p6mid()
        string tr2 = Debug.getTraceLog()
        SystemPrintln("trace2 = " + tr2)
        if (tr2.end( 19 ) == " > p6mid | p6 chain")
        {
            SystemPrintln("P6 log chain summary: OK")
        }
        else
        {
            SystemPrintln("P6 log chain summary: FAIL tr2=[" + tr2 + "]")
        }

        # 24c. pause 静默丢弃: 丢弃后末行仍是 24b 条目; resume 后恢复落帧
        Debug.pause()
        Debug.log( "p6 dropped" )
        string trp = Debug.getTraceLog()
        Debug.resume()
        Debug.log( "p6 kept" )
        string trk = Debug.getTraceLog()
        if (trp.end( 19 ) == " > p6mid | p6 chain" && trk.end( 10 ) == " | p6 kept")
        {
            SystemPrintln("P6 pause drops & resume keeps: OK")
        }
        else
        {
            SystemPrintln("P6 pause drops & resume keeps: FAIL trp=[" + trp + "] trk=[" + trk + "]")
        }

        # 24d. mark 后 log 关联新行 (row 4)
        Debug.mark( "p6m" )
        Debug.log( "p6 after mark" )
        string tr4 = Debug.getTraceLog()
        SystemPrintln("trace4 = " + tr4)
        if (tr4.end( 16 ) == " | p6 after mark")
        {
            SystemPrintln("P6 log after mark: OK")
        }
        else
        {
            SystemPrintln("P6 log after mark: FAIL tr4=[" + tr4 + "]")
        }

        # 24e. frameTable 空 labels: 返回 ""
        Array<string> noLabels = Array<string>( 0 )
        string tbEmpty = Debug.frameTable( noLabels )
        if (tbEmpty == "")
        {
            SystemPrintln("P6 frameTable empty labels: OK")
        }
        else
        {
            SystemPrintln("P6 frameTable empty labels: FAIL [" + tbEmpty + "]")
        }

        # 24f. 行轴=mark 行边界 (历史 1~3 + 新 4), 无匹配 label 单元格 "-"
        Array<string> lb1 = Array<string>( 1 )
        lb1[0] = "p6nope"
        string tb1 = Debug.frameTable( lb1 )
        SystemPrintln("table1 = " + tb1)
        if (tb1 == "row | p6nope\n1 | -\n2 | -\n3 | -\n4 | -")
        {
            SystemPrintln("P6 frameTable rows & miss cell: OK")
        }
        else
        {
            SystemPrintln("P6 frameTable rows & miss cell: FAIL tb1=[" + tb1 + "]")
        }

        # 24g. 行列对齐: watch 落在 mark 行边界 (11 落行 4, 22 落行 5)
        Debug.watch( 11, "p6w" )
        Debug.mark( "p6m2" )
        Debug.watch( 22, "p6w" )
        Array<string> lb2 = Array<string>( 1 )
        lb2[0] = "p6w"
        string tb2 = Debug.frameTable( lb2 )
        SystemPrintln("table2 = " + tb2)
        # 设计 §5.4: "单元格=该行处最近一帧的值" → 纯值文本 (无 [watch] 前缀,
        # 该前缀仅 getFrame/lastFrame 渲染格式)
        if (tb2 == "row | p6w\n1 | -\n2 | -\n3 | -\n4 | 11\n5 | 22")
        {
            SystemPrintln("P6 frameTable watch aligned: OK")
        }
        else
        {
            SystemPrintln("P6 frameTable watch aligned: FAIL tb2=[" + tb2 + "]")
        }

        # 24h. 单元格取该行该 label 最近一帧 (同 mark 行两次 watch 取后值)
        Debug.mark( "p6m3" )
        Debug.watch( 1, "p6j" )
        Debug.watch( 2, "p6j" )
        Array<string> lb3 = Array<string>( 1 )
        lb3[0] = "p6j"
        string tb3 = Debug.frameTable( lb3 )
        SystemPrintln("table3 = " + tb3)
        if (tb3 == "row | p6j\n1 | -\n2 | -\n3 | -\n4 | -\n5 | -\n6 | 2")
        {
            SystemPrintln("P6 frameTable latest cell: OK")
        }
        else
        {
            SystemPrintln("P6 frameTable latest cell: FAIL tb3=[" + tb3 + "]")
        }

        # 24i. 多 label 列对齐: p6w 列行 6 无帧 → "-", p6j 列取最近帧
        Array<string> lb4 = Array<string>( 2 )
        lb4[0] = "p6w"
        lb4[1] = "p6j"
        string tb4 = Debug.frameTable( lb4 )
        SystemPrintln("table4 = " + tb4)
        if (tb4 == "row | p6w | p6j\n1 | - | -\n2 | - | -\n3 | - | -\n4 | 11 | -\n5 | 22 | -\n6 | - | 2")
        {
            SystemPrintln("P6 frameTable multi labels: OK")
        }
        else
        {
            SystemPrintln("P6 frameTable multi labels: FAIL tb4=[" + tb4 + "]")
        }

        SystemPrintln("========== DebugTest (end) ==========")
    }

    # P5 辅助: 帧列表快照 (栈顶帧 = 本方法)
    # (实例方法: M4 AutoInlineMark 硬排除 !isStatic, 保证本方法有自身帧)
    string p5fr()
    {
        ret Debug.frames()
    }

    # P5 辅助: 当前帧 locals 行 (int-only, 按声明序 aa/bb)
    string p5data()
    {
        Int32 aa = 7
        Int32 bb = 8
        ret Debug.frameData( 0 )
    }

    # P5 辅助: 本帧变量取值 (int 与 string 两种格式)
    string p5var()
    {
        Int32 xx = 42
        string yy = "abc"
        string r1 = Debug.frameVar( 0, "xx" )
        string r2 = Debug.frameVar( 0, "yy" )
        ret r1 + "|" + r2
    }

    # P5 辅助: 跨帧读调用者(fun)的局部变量 (非栈顶帧可读)
    string p5outer()
    {
        ret Debug.frameVar( 1, "p5HostVar" )
    }

    # P5 辅助: 帧数据 args 行 (locals 先 args 后)
    string p5arg( Int32 av )
    {
        ret Debug.frameData( 0 )
    }

    # P5 辅助: frameVar 按 args 名取值
    string p5argvar( Int32 av )
    {
        ret Debug.frameVar( 0, "av" )
    }

    # P5 辅助: 协程体内帧列表 (验证协程链独立)
    string p5cf()
    {
        ret Debug.frames()
    }

    # P2 异常回卷辅助: begin 无配对 end 即 throw —— 编译期触发
    # DebugScopeNotClosed warning (22034), 运行期由 VM 帧回卷兜底弹出。
    static void scopeThrow() throws
    {
        Debug.begin( "risky" )
        throw DebugTestError.ScopeBoom
    }

    # P3 watchIn 辅助: 调用链含 "inTarget" 时才采样
    # (实例方法被 M4 自动 inline 排除 (isStatic 硬条件), 帧可靠落在链上;
    #   static 小方法在 -O1+ 会被内联, 链上无本方法帧, watchIn 匹配必失败)
    void inTarget()
    {
        Debug.watchIn( 5, "wix", "inTarget" )      # 方法名命中
        Debug.watchIn( 9, "wix2", "DebugTest" )    # 类型全名子串命中
    }

    # P3 watchIn 辅助: 调用链不含 "inTarget", 不采样
    void outOther()
    {
        Debug.watchIn( 6, "wix", "inTarget" )
    }

    # P6 辅助: 两层实例调用 (p6mid → p6logIn → Debug.log),
    # 保证 log 的调用链摘要含多层 (实例方法被 M4 自动 inline 排除, 帧可靠落在链上)
    void p6mid()
    {
        this.p6logIn()
    }

    # P6 辅助: 链最内层 log 调用点
    void p6logIn()
    {
        Debug.log( "p6 chain" )
    }
}
