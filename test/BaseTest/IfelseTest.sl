
IfelseTest
{
    Class1
    {
    }

    Class2
    {
        i = 0
    }

    static string getWhenInstance()
    {
        Class2 a = Class2()
        if a != null
        {
            ret "non-null"
        }
        ret ""
    }

    static int chainWithNext()
    {
        m = 0
        if m >= 0 && m < 100 && m != 50
        {
            m = 20
            if m == 14
            {
                m = 10
            }
            if m == 20
            {
                m = 20
            }
            else
            {
                m = 30
            }
        }
        else
        {
            m = 20
        }

        if true
        {
            int x1 = 200
        }
        elif 2 == 120 && false
        {
            m = 300
        }

        a = 30
        if a == 25
        {
            m = 20
        }
        elif a == 30 && a < 35
        {
            # next 仅允许出现在 for/while/dowhile 循环体内，此处留空分支验证空 elif 体
        }
        elif a == 31
        {
            m = 100
        }

        if false
        {
            int xxx = 100
        }

        ab = 1
        if true
        {
            a = 20
        }
        else
        {
            a = 30
        }

        a = 10
        if true
        {
            a = 20
        }
        elif a == 20
        {
            if a / 10 == 2
            {
                a = 30
            }
        }
        elif a == 40
        {
            a = 300
        }
        else
        {
            a = 10
        }

        ret m
    }

    static fun()
    {
        global.println("========== IfelseTest (start) ==========")

        aa = getWhenInstance()
        global.println("getWhenInstance -> " + aa)

        mFinal = chainWithNext()
        global.println("chainWithNext m -> " + mFinal.toString())

        basicBranchTest()
        compareConditionTest()
        logicConditionTest()
        boolVarConditionTest()
        numberEdgeConditionTest()
        nestedIfTest()
        assignFlowTest()
        emptyBranchTest()
        earlyReturnTest()
        multiElifChainTest()

        global.println("========== IfelseTest (end) ==========")

        FunIfCondition()
    }

    static FunIfCondition()
    {
        global.println("----- FunIfCondition (planned syntax, mostly commented) -----")

        # 以下为计划或部分实现的条件语法说明，当前以注释保留，避免未实现特性破坏编译：
        # if a       # 计划：等价 if a != null
        # if !a      # 计划：等价 if a == null
        # if c2      # 计划：引用非空
        # if c2.i    # 计划：相当于 c2 != null && c2.i != 0
        # if ai      # 计划：数值非零、字符串非空等truthiness
        # int a = if true { tr 20 } else { tr 10 }   # 计划：条件表达式内局部返回 tr
        # a = cond ? b : c   # 计划：三元运算符

        Class2 c2 = Class2()
        c2.i = 7
        if c2 != null && c2.i != 0
        {
            global.println("FunIfCondition: c2.i -> " + c2.i.toString())
        }
    }

    static basicBranchTest()
    {
        global.println("----- basicBranchTest -----")

        int a = 10

        # if 为真走 then
        if a == 10
        {
            global.println("if-true branch -> a == 10")
        }

        # if 为假无 else：直接跳过
        if a == 99
        {
            global.println("unexpected: a == 99")
        }

        # if-else：真分支
        if a > 0
        {
            global.println("if-else -> true branch")
        }
        else
        {
            global.println("if-else -> false branch")
        }

        # if-else：假分支
        if a < 0
        {
            global.println("unexpected: a < 0")
        }
        else
        {
            global.println("if-else else -> a >= 0")
        }

        # if-elif-else：命中 if
        if a == 10
        {
            global.println("chain -> hit if")
        }
        elif a == 20
        {
            global.println("unexpected: hit elif(20)")
        }
        else
        {
            global.println("unexpected: hit else")
        }

        # if-elif-else：命中 elif
        if a == 1
        {
            global.println("unexpected: hit if(1)")
        }
        elif a == 10
        {
            global.println("chain -> hit elif(10)")
        }
        else
        {
            global.println("unexpected: hit else")
        }

        # if-elif-else：命中 else
        if a == 1
        {
            global.println("unexpected: hit if(1)")
        }
        elif a == 2
        {
            global.println("unexpected: hit elif(2)")
        }
        else
        {
            global.println("chain -> hit else")
        }
    }

    static compareConditionTest()
    {
        global.println("----- compareConditionTest -----")

        int x = 10
        int y = 20

        if x < y { global.println("x < y -> true") } else { global.println("x < y -> false") }
        if x <= 10 { global.println("x <= 10 -> true") } else { global.println("x <= 10 -> false") }
        if y > x { global.println("y > x -> true") } else { global.println("y > x -> false") }
        if y >= 20 { global.println("y >= 20 -> true") } else { global.println("y >= 20 -> false") }
        if x == 10 { global.println("x == 10 -> true") } else { global.println("x == 10 -> false") }
        if x != y { global.println("x != y -> true") } else { global.println("x != y -> false") }

        # 浮点比较条件
        Num f = 1.5
        if f > 1.0 && f < 2.0
        {
            global.println("Num f in (1.0, 2.0) -> true")
        }
        else
        {
            global.println("Num f in (1.0, 2.0) -> false")
        }
    }

    static logicConditionTest()
    {
        global.println("----- logicConditionTest -----")

        int a = 5

        # && 全真
        if a > 0 && a < 10
        {
            global.println("a>0 && a<10 -> true")
        }

        # && 一假（短路：右侧不应被执行）
        int probe = 0
        if a > 10 && a < 100
        {
            probe = 1
        }
        if probe == 0
        {
            global.println("a>10 && a<100 -> false (short-circuit)")
        }

        # || 一真
        if a == 5 || a == 6
        {
            global.println("a==5 || a==6 -> true")
        }

        # || 全假
        if a == 7 || a == 8
        {
            global.println("unexpected: a==7||a==8")
        }
        else
        {
            global.println("a==7 || a==8 -> false")
        }

        # ! 取反
        if !(a > 10)
        {
            global.println("!(a>10) -> true")
        }

        # 混合优先级：&& 高于 ||（等价 (a<0 && a>10) || a==5）
        if a < 0 && a > 10 || a == 5
        {
            global.println("a<0 && a>10 || a==5 -> true")
        }
        else
        {
            global.println("a<0 && a>10 || a==5 -> false")
        }
    }

    static boolVarConditionTest()
    {
        global.println("----- boolVarConditionTest -----")

        bool flag = true
        bool open = false

        if flag
        {
            global.println("if flag(true) -> hit")
        }
        else
        {
            global.println("unexpected: flag else")
        }

        if open
        {
            global.println("unexpected: open true")
        }
        else
        {
            global.println("if open(false) -> else hit")
        }

        # bool 变量参与逻辑组合
        if flag && !open
        {
            global.println("flag && !open -> true")
        }

        # bool 变量取反后作为条件
        if !open
        {
            global.println("!open -> true")
        }

        # 比较产生 bool 再入 if
        bool eq = (10 == 10)
        if eq
        {
            global.println("bool eq=(10==10) -> true")
        }
    }

    static numberEdgeConditionTest()
    {
        global.println("----- numberEdgeConditionTest -----")

        int zero = 0
        int neg = -3

        # 0 相关边界
        if zero == 0 { global.println("zero == 0 -> true") }
        if zero >= 0 { global.println("zero >= 0 -> true") }
        if zero < 0 { } else { global.println("zero < 0 -> false") }

        # 负数比较
        if neg < zero
        {
            global.println("neg < zero -> true")
        }
        else
        {
            global.println("unexpected: neg >= zero")
        }

        if neg * -1 > zero
        {
            global.println("neg*-1 > zero -> true")
        }

        # 负数与正数混合链
        if neg < -1 && neg > -5
        {
            global.println("neg in (-5, -1) -> true")
        }
    }

    static nestedIfTest()
    {
        global.println("----- nestedIfTest -----")

        int level = 2

        # 两层嵌套：外层真 + 内层真
        if level > 0
        {
            if level == 2
            {
                global.println("nested L1(true) L2(true) -> hit")
            }
            else
            {
                global.println("unexpected: L2 else")
            }
        }
        else
        {
            global.println("unexpected: L1 else")
        }

        # 两层嵌套：外层假，内层不应执行
        if level > 10
        {
            if level == 2
            {
                global.println("unexpected: inner hit")
            }
        }
        else
        {
            global.println("outer false -> else hit, inner skipped")
        }

        # 三层嵌套
        int v = 7
        if v > 0
        {
            if v > 5
            {
                if v == 7
                {
                    global.println("nested 3-level -> hit v == 7")
                }
                else
                {
                    global.println("unexpected: 3-level else")
                }
            }
        }

        # 嵌套中带 elif
        if v > 10
        {
            global.println("unexpected: v > 10")
        }
        elif v > 5
        {
            if v == 7
            {
                global.println("elif(v>5) + inner if(v==7) -> hit")
            }
            elif v == 6
            {
                global.println("unexpected: v == 6")
            }
        }
        else
        {
            global.println("unexpected: v <= 5")
        }
    }

    static assignFlowTest()
    {
        global.println("----- assignFlowTest -----")

        int m = 0

        # if 内修改变量，影响后续条件
        if m == 0
        {
            m = 10
        }
        if m == 10
        {
            m = 20
        }
        else
        {
            m = -1
        }
        global.println("assignFlow m -> " + m.toString())

        # else 分支修改变量
        int n = 5
        if n > 100
        {
            n = 1000
        }
        else
        {
            n = 50
        }
        global.println("assignFlow n -> " + n.toString())

        # elif 分支修改变量
        int k = 3
        if k == 1
        {
            k = 100
        }
        elif k == 3
        {
            k = 30
        }
        else
        {
            k = 0
        }
        global.println("assignFlow k -> " + k.toString())
    }

    static emptyBranchTest()
    {
        global.println("----- emptyBranchTest -----")

        int a = 1

        # 空 then 分支：不应崩溃，走完即过
        if a == 1
        {
        }

        # 空 else 分支
        if a == 2
        {
            global.println("unexpected: a == 2")
        }
        else
        {
        }

        # 空分支 + 后续语句仍执行
        if a == 1
        {
        }
        global.println("emptyBranch -> after empty if, still runs")

        # 单语句无换行分支体
        if a == 1 { a = 2 }
        if a == 2 { global.println("single-line then -> hit") } else { }
    }

    static earlyReturnTest()
    {
        global.println("----- earlyReturnTest -----")

        global.println("earlyReturn(if hit) -> " + pick(1).toString())
        global.println("earlyReturn(elif hit) -> " + pick(2).toString())
        global.println("earlyReturn(else hit) -> " + pick(9).toString())

        # 嵌套 if 内 ret
        global.println("earlyReturn(nested) -> " + pickNested(5).toString())
    }

    static int pick(int v)
    {
        if v == 1
        {
            ret 100
        }
        elif v == 2
        {
            ret 200
        }
        else
        {
            ret -1
        }
    }

    static int pickNested(int v)
    {
        if v > 0
        {
            if v > 10
            {
                ret 10
            }
            ret 1
        }
        ret 0
    }

    static multiElifChainTest()
    {
        global.println("----- multiElifChainTest -----")

        int calls = 0
        int s = 90
        if s >= 60 { calls = calls + 1 }
        elif s >= 70 { calls = calls + 1 }
        elif s >= 80 { calls = calls + 1 }
        else { calls = calls + 1 }
        if calls == 1
        {
            global.println("multiElif -> only first match evaluated")
        }
        else
        {
            global.println("unexpected: multiElif calls -> " + calls.toString())
        }

        # 5 连 elif 链，分别命中每一档
        grade(95)
        grade(85)
        grade(75)
        grade(65)
        grade(55)
        grade(0)
    }

    static grade(int score)
    {
        if score >= 90
        {
            global.println("grade(" + score.toString() + ") -> A")
        }
        elif score >= 80
        {
            global.println("grade(" + score.toString() + ") -> B")
        }
        elif score >= 70
        {
            global.println("grade(" + score.toString() + ") -> C")
        }
        elif score >= 60
        {
            global.println("grade(" + score.toString() + ") -> D")
        }
        else
        {
            global.println("grade(" + score.toString() + ") -> F")
        }
    }
}

# 测试用例说明：
# - getWhenInstance：非空实例走 if 分支，返回固定字符串。
# - chainWithNext：嵌套 if/elif/else 组合（注：next 仅允许出现在循环体内，原草稿中 if 分支内的 next 已移除），返回值 m 用于观察控制流是否按预期汇合。
# - basicBranchTest：if 真假、if-else 真假分支、if-elif-else 三种命中（if/elif/else）的基础分支选择。
# - compareConditionTest：六种比较运算符（< <= > >= == !=）作为条件，含 Num 浮点区间判断。
# - logicConditionTest：&& || ! 组合条件、短路求值（假分支不应触发副作用）、&& 优先级高于 ||。
# - boolVarConditionTest：bool 变量直接作条件、取反作条件、比较产生的 bool 值再入 if。
# - numberEdgeConditionTest：0 边界（==0 >=0 <0）、负数比较、负数参与算术后比较、负数区间。
# - nestedIfTest：两层/三层嵌套 if、外层假时内层应跳过、嵌套中混合 elif。
# - assignFlowTest：分支内赋值影响后续条件判断（m=0→10→20）、else/elif 分支修改变量。
# - emptyBranchTest：空 then/else 分支不崩溃，后续语句照常执行，单行分支体。
# - earlyReturnTest：if/elif/else 内 ret 提前返回（100/200/-1），嵌套 if 内 ret（v>10→10，v>0→1，否则 0）。
# - multiElifChainTest：5 连 elif 成绩分档（A/B/C/D/F 各命中一次），命中后不再评估后续 elif。
# - FunIfCondition：用已支持语法演示对可空/成员判断；其余 truthiness、tr、三元等仅在注释中列出设计意图。
#
# 预期结果：
# - getWhenInstance 输出 "non-null"。
# - chainWithNext 的 m 与实现细节相关，用于回归时对比历史输出。
# - basicBranchTest 输出 if-true / true branch / else 分支 / chain 三种命中，无 unexpected 行。
# - compareConditionTest 六个比较全部 true，Num f in (1.0, 2.0) -> true。
# - logicConditionTest：短路提示 "false (short-circuit)"，!(a>10) 为 true，混合优先级整体 true。
# - boolVarConditionTest：flag 命中、open 走 else、flag && !open 与 !open 为 true、eq 为 true。
# - numberEdgeConditionTest：zero 边界三条符合预期，neg 相关均为 true。
# - nestedIfTest：无 unexpected 行，L1/L2 均命中，外层假时 else 命中且内层跳过。
# - assignFlowTest：m -> 20，n -> 50，k -> 30。
# - emptyBranchTest：打印 after empty if 与 single-line then -> hit，无崩溃。
# - earlyReturnTest：依次输出 100 / 200 / -1 / 1。
# - multiElifChainTest：grade 依次 A/B/C/D/F，且 "only first match evaluated"。
# - FunIfCondition 打印 c2.i 为 7；不因未实现语法产生编译错误。
