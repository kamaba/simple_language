WhileTest
{
    # fun 作为入口，统一调用所有 while/dowhile 分类测试
    static fun()
    {
        global.println("========== WhileTest (start) ==========")

        basicWhileTest()
        whileBreakTest()
        whileContinueTest()
        whileTrueBreakTest()
        whileBoolVarTest()
        whileLogicConditionTest()
        whileSumTest()
        whileFloatCounterTest()
        nestedWhileTest()
        whileEarlyReturnTest()
        dowhileBasicTest()
        dowhileExecutesOnceTest()
        dowhileBreakTest()
        dowhileContinueTest()
        dowhileNestedTest()
        whileDowhileMixedTest()
        originalSmokeTest()

        global.println("========== WhileTest (end) ==========")
    }

    # while 基础计数 + 条件初始为假时零次执行
    static basicWhileTest()
    {
        global.println("---------------basicWhileTest--------------")

        int i = 0
        int count = 0
        while i < 5
        {
            count += 1
            i++
        }
        global.println("basicWhile count -> " + count.toString())   # 5
        global.println("basicWhile i -> " + i.toString())           # 5

        # 条件一开始就是假：循环体一次都不执行
        int j = 10
        int hits = 0
        while j < 5
        {
            hits++
            j++
        }
        global.println("basicWhile zero-run hits -> " + hits.toString())   # 0
    }

    # while 中 break 跳出循环
    static whileBreakTest()
    {
        global.println("---------------whileBreakTest--------------")

        int i = 0
        while true
        {
            if i == 3
            {
                break
            }
            global.println("whileBreak i = $i")
            i++
        }
        global.println("whileBreak final i -> " + i.toString())   # 3

        # 条件型 while 中 break 提前结束
        int n = 0
        while n < 100
        {
            if n == 2
            {
                break
            }
            n++
        }
        global.println("whileBreak n -> " + n.toString())   # 2
    }

    # while 中 continue 跳过本次迭代（奇数求和 1+3+5+7+9 = 25）
    static whileContinueTest()
    {
        global.println("---------------whileContinueTest--------------")

        int i = 0
        int sum = 0
        while i < 10
        {
            i++
            if i % 2 == 0
            {
                continue
            }
            sum += i
        }
        global.println("whileContinue odd sum -> " + sum.toString())   # 25

        # continue 首轮即触发：仍能正常推进并结束
        int k = 0
        int kept = 0
        while k < 4
        {
            k++
            if k == 1
            {
                continue
            }
            kept++
        }
        global.println("whileContinue kept -> " + kept.toString())   # 3
    }

    # while true + break 组合
    static whileTrueBreakTest()
    {
        global.println("---------------whileTrueBreakTest--------------")

        int m = 0
        while true
        {
            m = 20
            break
        }
        global.println("whileTrueBreak m -> " + m.toString())   # 20

        # 第一轮即 break：循环体前半段语句仍执行
        int n = 0
        while true
        {
            n++
            if n == 1
            {
                break
            }
        }
        global.println("whileTrueBreak n -> " + n.toString())   # 1
    }

    # bool 变量作为 while 条件
    static whileBoolVarTest()
    {
        global.println("---------------whileBoolVarTest--------------")

        bool flag = true
        int runs = 0
        while flag
        {
            runs++
            if runs >= 3
            {
                flag = false
            }
        }
        global.println("whileBoolVar runs -> " + runs.toString())   # 3

        # 初始为 false：零次执行
        bool off = false
        int hits = 0
        while off
        {
            hits++
        }
        global.println("whileBoolVar zero-run hits -> " + hits.toString())   # 0
    }

    # while 条件使用 && / || / ! 逻辑组合
    static whileLogicConditionTest()
    {
        global.println("---------------whileLogicConditionTest--------------")

        int i = 0
        while i < 10 && i != 4
        {
            global.println("logicCond i = $i")
            i++
        }
        global.println("logicCond stop at -> " + i.toString())   # 4

        int j = 0
        while j > 100 || j < 3
        {
            j++
        }
        global.println("logicCond j -> " + j.toString())   # 3

        int k = 3
        while !(k >= 8) && k > 2
        {
            k++
        }
        global.println("logicCond k -> " + k.toString())   # 8
    }

    # while 累加求和 1..100 = 5050
    static whileSumTest()
    {
        global.println("---------------whileSumTest--------------")

        int i = 1
        int sum = 0
        while i <= 100
        {
            sum += i
            i++
        }
        global.println("whileSum 1..100 -> " + sum.toString())   # 5050

        # 等差累计：5,10,...,50 共 10 项和为 275
        int step = 5
        int total = 0
        while step <= 50
        {
            total += step
            step += 5
        }
        global.println("whileSum 5..50 step5 -> " + total.toString())   # 275
    }

    # Num 浮点计数器驱动的 while
    static whileFloatCounterTest()
    {
        global.println("---------------whileFloatCounterTest--------------")

        Num f = 0.0
        int runs = 0
        while f < 1.0
        {
            f += 0.25
            runs++
        }
        global.println("whileFloat runs -> " + runs.toString())   # 4
        global.println("whileFloat f -> " + f.toString())         # 1.0
    }

    # 嵌套 while：内层 break/continue 只影响内层
    static nestedWhileTest()
    {
        global.println("---------------nestedWhileTest--------------")

        int outer = 0
        int total = 0
        while outer < 3
        {
            int inner = 0
            while inner < 5
            {
                if inner == 2
                {
                    inner++
                    continue
                }
                if inner == 4
                {
                    break
                }
                total++
                inner++
            }
            outer++
        }
        # 每轮外层：inner 取 0,1,3 计数（2 被 continue 跳过，4 处 break），共 3*3=9
        global.println("nestedWhile total -> " + total.toString())   # 9

        # 内层 break 后外层继续推进
        int a = 0
        int outerRounds = 0
        while a < 3
        {
            int b = 0
            while true
            {
                b++
                if b == 2
                {
                    break
                }
            }
            outerRounds++
            a++
        }
        global.println("nestedWhile outerRounds -> " + outerRounds.toString())   # 3
    }

    # while 体内 ret 提前返回
    static whileEarlyReturnTest()
    {
        global.println("---------------whileEarlyReturnTest--------------")

        global.println("findFirstGt(8) -> " + findFirstGt(8).toString())     # 9
        global.println("findFirstGt(100) -> " + findFirstGt(100).toString()) # -1
    }

    static int findFirstGt(int threshold)
    {
        int i = 0
        while i < 10
        {
            if i > threshold
            {
                ret i
            }
            i++
        }
        ret -1
    }

    # dowhile 基础：先执行后判断
    static dowhileBasicTest()
    {
        global.println("---------------dowhileBasicTest--------------")

        int i = 0
        int runs = 0
        dowhile i < 3
        {
            i++
            runs++
        }
        global.println("dowhileBasic i -> " + i.toString())       # 3
        global.println("dowhileBasic runs -> " + runs.toString()) # 3
    }

    # dowhile 条件为假也至少执行一次（与 while 的核心区别）
    static dowhileExecutesOnceTest()
    {
        global.println("---------------dowhileExecutesOnceTest--------------")

        int runs = 0
        dowhile false
        {
            runs++
        }
        global.println("dowhileFalse runs -> " + runs.toString())   # 1

        # 条件首轮即为假：仍执行一次后结束
        int j = 100
        int hits = 0
        dowhile j < 50
        {
            hits++
            j++
        }
        global.println("dowhileFalseCond hits -> " + hits.toString())   # 1

        # 对照：同样条件 while 一次都不执行
        int w = 100
        int wHits = 0
        while w < 50
        {
            wHits++
            w++
        }
        global.println("whileSameCond hits -> " + wHits.toString())   # 0
    }

    # dowhile 中 break 跳出
    static dowhileBreakTest()
    {
        global.println("---------------dowhileBreakTest--------------")

        int i = 0
        int runs = 0
        dowhile i < 100
        {
            runs++
            if i == 2
            {
                break
            }
            i++
        }
        global.println("dowhileBreak i -> " + i.toString())       # 2
        global.println("dowhileBreak runs -> " + runs.toString()) # 3

        # 首轮即 break：dowhile 体执行一次
        int n = 0
        int nRuns = 0
        dowhile true
        {
            nRuns++
            break
        }
        global.println("dowhileBreak firstRound nRuns -> " + nRuns.toString())   # 1
    }

    # dowhile 中 continue：跳到条件判断（i 先自增避免死循环）
    static dowhileContinueTest()
    {
        global.println("---------------dowhileContinueTest--------------")

        int i = 0
        int sum = 0
        dowhile i < 10
        {
            i++
            if i == 3 || i == 5
            {
                continue
            }
            sum += i
        }
        # 1..10 去掉 3 和 5：1+2+4+6+7+8+9+10 = 47
        global.println("dowhileContinue sum -> " + sum.toString())   # 47

        int k = 0
        int kept = 0
        dowhile k < 4
        {
            k++
            if k == 1
            {
                continue
            }
            kept++
        }
        global.println("dowhileContinue kept -> " + kept.toString())   # 3
    }

    # 嵌套 dowhile
    static dowhileNestedTest()
    {
        global.println("---------------dowhileNestedTest--------------")

        int outer = 0
        int total = 0
        dowhile outer < 3
        {
            int inner = 0
            dowhile inner < 4
            {
                total++
                inner++
            }
            outer++
        }
        global.println("dowhileNested total -> " + total.toString())   # 12
    }

    # while 与 dowhile 相互嵌套
    static whileDowhileMixedTest()
    {
        global.println("---------------whileDowhileMixedTest--------------")

        # while 外层 + dowhile 内层
        int i = 0
        int count = 0
        while i < 3
        {
            int j = 0
            dowhile j < 2
            {
                count++
                j++
            }
            i++
        }
        global.println("mixed while>dowhile count -> " + count.toString())   # 6

        # dowhile 外层 + while 内层（外层条件为假，但内层仍执行一轮）
        int a = 10
        int total2 = 0
        dowhile a < 2
        {
            int b = 0
            while b < 3
            {
                total2++
                b++
            }
            a++
        }
        global.println("mixed dowhile>while total -> " + total2.toString())   # 3
    }

    # 原始冒烟用例（保留自最初版本，修正了末尾 while true 的死循环问题）
    static originalSmokeTest()
    {
        global.println("---------------originalSmokeTest--------------")

        int i = 0
        while i < 14
        {
            i++
            if i == 5
            {
                continue
            }
            if i > 10
            {
                break
            }
            global.println("smoke i = $i")
        }
        global.println("smoke final i -> " + i.toString())   # 11

        # 倒退循环：i 从 10 递减到 7（dowhile 假条件对照见下方）
        i = 10
        int downRuns = 0
        while i > 7
        {
            i -= 1
            downRuns++
        }
        global.println("smoke countdown i -> " + i.toString())   # 7
        global.println("smoke countdown runs -> " + downRuns.toString())   # 3

        int m = 0
        while true
        {
            m = 20
            break
        }
        global.println("smoke whileTrue m -> " + m.toString())   # 20

        # dowhile 条件为假：执行一次
        i = 30
        int dwRuns = 0
        dowhile i < 20
        {
            dwRuns++
        }
        global.println("smoke dowhileOnce dwRuns -> " + dwRuns.toString())   # 1

        int m2 = 0
        dowhile false
        {
            m2 = 20
        }
        global.println("smoke dowhileFalse m2 -> " + m2.toString())   # 20（体至少执行一次）

        i = 0
        while true
        {
            i += 20
            if i == 20
            {
                break
            }
        }
        global.println("smoke assignBreak i -> " + i.toString())   # 20
    }
}

# 测试用例说明：
# - basicWhileTest：while 计数循环执行 5 次；条件初始为假时零次执行。
# - whileBreakTest：while true 中 i==3 break；条件型 while 中 n==2 break。
# - whileContinueTest：continue 跳过偶数，奇数和为 25；首轮 continue 仍能推进。
# - whileTrueBreakTest：while true + 立即 break / 首轮 break。
# - whileBoolVarTest：bool 变量作条件，循环内翻转 flag 结束；初始 false 零次执行。
# - whileLogicConditionTest：&& 短路停止于 4；|| 条件推进到 3；!(k>=8)&&k>2 推进到 8。
# - whileSumTest：1..100 累加 5050；5..50 步长 5 求和 275。
# - whileFloatCounterTest：Num 浮点计数器 0.0->1.0 步长 0.25，共 4 次。
# - nestedWhileTest：内层 continue 跳过 2、break 于 4，每轮外层计数 3，共 9；内层 break 不影响外层。
# - whileEarlyReturnTest：循环体内 ret 提前返回（findFirstGt(8)=9，超界返回 -1）。
# - dowhileBasicTest：先执行后判断，i 与 runs 均为 3。
# - dowhileExecutesOnceTest：dowhile false 执行 1 次；条件首轮为假仍执行 1 次；同条件 while 为 0 次（核心语义对照）。
# - dowhileBreakTest：i==2 时 break（runs=3）；首轮 break 体仍执行 1 次。
# - dowhileContinueTest：continue 跳过 3、5，1..10 求和 47；首轮 continue 正常推进。
# - dowhileNestedTest：3 轮外层 × 4 次内层 = 12。
# - whileDowhileMixedTest：while>dowhile 计数 6；dowhile(假)>while 内层仍执行一轮共 3。
# - originalSmokeTest：原始冒烟用例（含 continue/break/倒退 while/dowhile false），末尾 while true 死循环已修正。
#
# 预期结果：
# - basicWhileTest 输出 count=5、i=5、zero-run hits=0。
# - whileBreakTest 输出 i=0,1,2 三行后 final i=3；n=2。
# - whileContinueTest 输出 odd sum=25、kept=3。
# - whileTrueBreakTest 输出 m=20、n=1。
# - whileBoolVarTest 输出 runs=3、zero-run hits=0。
# - whileLogicConditionTest 输出 i=0..3 四行后 stop at=4；j=3；k=8。
# - whileSumTest 输出 5050、275。
# - whileFloatCounterTest 输出 runs=4、f=1。
# - nestedWhileTest 输出 total=9、outerRounds=3。
# - whileEarlyReturnTest 输出 9、-1。
# - dowhileBasicTest 输出 i=3、runs=3。
# - dowhileExecutesOnceTest 输出 runs=1、hits=1、whileSameCond hits=0。
# - dowhileBreakTest 输出 i=2、runs=3、firstRound nRuns=1。
# - dowhileContinueTest 输出 sum=47、kept=3。
# - dowhileNestedTest 输出 total=12。
# - whileDowhileMixedTest 输出 count=6、total=3。
# - originalSmokeTest 输出 i=1..4,6..10（跳过 5，10 处 break）、final i=11、countdown i=7、countdown runs=3、m=20、dwRuns=1、m2=20、assignBreak i=20。
