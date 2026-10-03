# SwitchTest: switch 语句测试
# 匹配逻辑已下沉到 CVM（单条 Switch 指令 + 跳转表 payload），
# 支持以下 case 形式：
#   1. 单常量:            case 1 { }  case "red" { }  case true { }  case 1.5 { }
#   2. 多值( | 或 , ):    case 1|2|3|4 { }  case "a"|"b" { }  case 10,11,12 { }
#   3. 类型模式( is ):    case is ClassName { }  case is ClassName 变量名 { }  (参考 C#)
#   4. 枚举成员:          case SwitchColor.Red { }
#   5. default 兜底 / 无匹配时直接穿过
#   6. next 继续匹配后续 case
# 注: 范围匹配(case >=1 && <=10)与 switch 表达式(tr)暂不支持，保留注释。

enum SwitchColor extends int
{
    Red = 1
    Green = 2
    Blue = 3
}

SwitchTest
{
    Class2
    {
        v = 0
    }
    Class3 extends Class2
    {
        c3_val = 100
    }
    Class4 extends Class2
    {
        c4_val = 200
    }

    static fun()
    {
        global.println("========== SwitchTest (start) ==========")

        constIntTest()
        multiValueOrTest()
        multiValueCommaTest()
        floatValueTest()
        stringValueTest()
        boolValueTest()
        enumValueTest()
        typePatternIsTest()
        defaultValueTest()
        nextCaseTest()
        parenSourceTest()

        global.println("========== SwitchTest (end) ==========")
    }

    # 单值整数匹配（基础回归）
    static constIntTest()
    {
        global.println("----- constIntTest -----")

        i = 2
        switch i
        {
            case 1{ global.println("i=2 -> case 1 (unexpected)") }
            case 2{ global.println("i=2 -> case 2 hit") }
            case 3{ global.println("i=2 -> case 3 (unexpected)") }
            default{ global.println("i=2 -> default (unexpected)") }
        }

        i = 9
        switch i
        {
            case 1{ global.println("i=9 -> case 1 (unexpected)") }
            case 2{ global.println("i=9 -> case 2 (unexpected)") }
            default{ global.println("i=9 -> default hit") }
        }
    }

    # 多值匹配: case 1|2|3|4 / case 5|6|7
    static multiValueOrTest()
    {
        global.println("----- multiValueOrTest -----")

        i = 3
        switch i
        {
            case 1|2|3|4{ global.println("i=3 -> case 1|2|3|4 hit") }
            case 5|6|7{ global.println("i=3 -> case 5|6|7 (unexpected)") }
            default{ global.println("i=3 -> default (unexpected)") }
        }

        i = 7
        switch i
        {
            case 1|2|3|4{ global.println("i=7 -> case 1|2|3|4 (unexpected)") }
            case 5|6|7{ global.println("i=7 -> case 5|6|7 hit") }
            default{ global.println("i=7 -> default (unexpected)") }
        }

        # 首个命中后不再继续匹配（自带 break 语义）
        i = 2
        switch i
        {
            case 1|2|3{ global.println("i=2 -> first group hit (stops matching)") }
            case 2|4{ global.println("i=2 -> second group (unexpected: duplicate)") }
            default{ global.println("i=2 -> default (unexpected)") }
        }

        # 无命中走 default
        i = 99
        switch i
        {
            case 1|2|3|4{ global.println("i=99 -> case 1|2|3|4 (unexpected)") }
            case 5|6|7{ global.println("i=99 -> case 5|6|7 (unexpected)") }
            default{ global.println("i=99 -> default hit") }
        }
    }

    # 多值匹配(逗号分隔): case 10,11,12 / case 13,14
    static multiValueCommaTest()
    {
        global.println("----- multiValueCommaTest -----")

        i = 11
        switch i
        {
            case 10,11,12{ global.println("i=11 -> case 10,11,12 hit") }
            case 13,14{ global.println("i=11 -> case 13,14 (unexpected)") }
            default{ global.println("i=11 -> default (unexpected)") }
        }

        i = 14
        switch i
        {
            case 10,11,12{ global.println("i=14 -> case 10,11,12 (unexpected)") }
            case 13,14{ global.println("i=14 -> case 13,14 hit") }
            default{ global.println("i=14 -> default (unexpected)") }
        }

        i = 20
        switch i
        {
            case 10,11,12{ global.println("i=20 -> case 10,11,12 (unexpected)") }
            case 13,14{ global.println("i=20 -> case 13,14 (unexpected)") }
            default{ global.println("i=20 -> default hit") }
        }
    }

    # 浮点常量匹配（含多值）
    static floatValueTest()
    {
        global.println("----- floatValueTest -----")

        Num f = 2.5
        switch f
        {
            case 1.5{ global.println("f=2.5 -> case 1.5 (unexpected)") }
            case 2.5|3.5{ global.println("f=2.5 -> case 2.5|3.5 hit") }
            default{ global.println("f=2.5 -> default (unexpected)") }
        }

        f = 3.5
        switch f
        {
            case 1.5{ global.println("f=3.5 -> case 1.5 (unexpected)") }
            case 2.5|3.5{ global.println("f=3.5 -> case 2.5|3.5 hit") }
            default{ global.println("f=3.5 -> default (unexpected)") }
        }

        f = 9.5
        switch f
        {
            case 1.5{ global.println("f=9.5 -> case 1.5 (unexpected)") }
            case 2.5|3.5{ global.println("f=9.5 -> case 2.5|3.5 (unexpected)") }
            default{ global.println("f=9.5 -> default hit") }
        }
    }

    # 字符串常量匹配（含多值）
    static stringValueTest()
    {
        global.println("----- stringValueTest -----")

        s = "green"
        switch s
        {
            case "red"{ global.println("s=green -> case red (unexpected)") }
            case "green"|"blue"{ global.println("s=green -> case green|blue hit") }
            default{ global.println("s=green -> default (unexpected)") }
        }

        s = "blue"
        switch s
        {
            case "red"{ global.println("s=blue -> case red (unexpected)") }
            case "green"|"blue"{ global.println("s=blue -> case green|blue hit") }
            default{ global.println("s=blue -> default (unexpected)") }
        }

        s = "yellow"
        switch s
        {
            case "red"{ global.println("s=yellow -> case red (unexpected)") }
            case "green"|"blue"{ global.println("s=yellow -> case green|blue (unexpected)") }
            default{ global.println("s=yellow -> default hit") }
        }
    }

    # 布尔常量匹配
    static boolValueTest()
    {
        global.println("----- boolValueTest -----")

        b = true
        switch b
        {
            case true{ global.println("b=true -> case true hit") }
            case false{ global.println("b=true -> case false (unexpected)") }
            default{ global.println("b=true -> default (unexpected)") }
        }

        b = false
        switch b
        {
            case true{ global.println("b=false -> case true (unexpected)") }
            case false{ global.println("b=false -> case false hit") }
            default{ global.println("b=false -> default (unexpected)") }
        }
    }

    # 枚举成员匹配
    static enumValueTest()
    {
        global.println("----- enumValueTest -----")

        color = SwitchColor.Red
        switch color
        {
            case SwitchColor.Red{ global.println("color=Red -> case SwitchColor.Red hit") }
            case SwitchColor.Green{ global.println("color=Red -> case Green (unexpected)") }
            case SwitchColor.Blue{ global.println("color=Red -> case Blue (unexpected)") }
            default{ global.println("color=Red -> default (unexpected)") }
        }

        color = SwitchColor.Blue
        switch color
        {
            case SwitchColor.Red{ global.println("color=Blue -> case Red (unexpected)") }
            case SwitchColor.Green{ global.println("color=Blue -> case Green (unexpected)") }
            case SwitchColor.Blue{ global.println("color=Blue -> case SwitchColor.Blue hit") }
            default{ global.println("color=Blue -> default (unexpected)") }
        }
    }

    # 类型模式匹配: case is ClassName [变量名]（参考 C# 的 is 用法）
    static typePatternIsTest()
    {
        global.println("----- typePatternIsTest -----")

        # 带绑定变量: case is Class3 c3 -> c3 在体内可用
        obj = Class3()
        switch obj
        {
            case is Class3 c3
            {
                c3.c3_val = 300
                global.println("obj(Class3) -> case is Class3 c3 hit, c3_val=" + c3.c3_val.toString())
            }
            case is Class4
            {
                global.println("obj(Class3) -> case is Class4 (unexpected)")
            }
            default{ global.println("obj(Class3) -> default (unexpected)") }
        }

        # 不带绑定变量: case is Class4
        obj2 = Class4()
        switch obj2
        {
            case is Class3
            {
                global.println("obj2(Class4) -> case is Class3 (unexpected)")
            }
            case is Class4 c4
            {
                global.println("obj2(Class4) -> case is Class4 c4 hit, c4_val=" + c4.c4_val.toString())
            }
            default{ global.println("obj2(Class4) -> default (unexpected)") }
        }

        # 继承关系匹配: Class3 extends Class2, case is Class2 命中
        obj3 = Class3()
        switch obj3
        {
            case is Class4{ global.println("obj3(Class3) -> case is Class4 (unexpected)") }
            case is Class2 c2
            {
                c2.v = 33
                global.println("obj3(Class3) -> case is Class2 hit (extends relation), v=" + c2.v.toString())
            }
            default{ global.println("obj3(Class3) -> default (unexpected)") }
        }

        # 数值源匹配内建类型: case is int
        int n = 10
        switch n
        {
            case is int
            {
                global.println("n(int 10) -> case is int hit")
            }
            case is string{ global.println("n(int 10) -> case is string (unexpected)") }
            default{ global.println("n(int 10) -> default (unexpected)") }
        }

        # 字符串源匹配: case is string
        string s = "abc"
        switch s
        {
            case is int{ global.println("s(string) -> case is int (unexpected)") }
            case is string
            {
                global.println("s(string abc) -> case is string hit")
            }
            default{ global.println("s(string) -> default (unexpected)") }
        }

        # 类型 case 与 常量 case 混合
        mixed = Class4()
        switch mixed
        {
            case 1{ global.println("mixed(Class4) -> case 1 (unexpected)") }
            case is Class3{ global.println("mixed(Class4) -> case is Class3 (unexpected)") }
            case is Class4{ global.println("mixed(Class4) -> mixed: case is Class4 hit") }
            default{ global.println("mixed(Class4) -> default (unexpected)") }
        }
    }

    # default 与无匹配穿过
    static defaultValueTest()
    {
        global.println("----- defaultValueTest -----")

        # 有 default 命中
        i = 500
        r = 0
        switch i
        {
            case 1{ r = 1 }
            case 2{ r = 2 }
            default{ r = -1 }
        }
        global.println("i=500 -> default r=" + r.toString())

        # 无 default 无匹配: 直接穿过 switch 继续执行
        i = 500
        r = 0
        switch i
        {
            case 1{ r = 1 }
            case 2{ r = 2 }
        }
        global.println("i=500 no default -> r=" + r.toString() + " (fall through switch)")
    }

    # next: 本 case 体执行完后继续匹配后续 case
    static nextCaseTest()
    {
        global.println("----- nextCaseTest -----")

        m = 1
        switch m
        {
            case 1
            {
                global.println("m=1 -> case 1 hit, then next")
                next
            }
            case 1{ global.println("m=1 -> case 1 (second) hit via next") }
            case 2{ global.println("m=1 -> case 2 (unexpected)") }
            default{ global.println("m=1 -> default (unexpected)") }
        }

        # 命中后无 next: 不再匹配后续 case
        m = 2
        switch m
        {
            case 2
            {
                global.println("m=2 -> case 2 hit (no next, stops)")
            }
            case 2{ global.println("m=2 -> case 2 (second) (unexpected: no next)") }
            default{ global.println("m=2 -> default (unexpected)") }
        }
    }

    # 带括号的源表达式: switch( i )
    static parenSourceTest()
    {
        global.println("----- parenSourceTest -----")

        i = 6
        switch( i )
        {
            case 1|2|3|4{ global.println("switch(i) i=6 -> case 1|2|3|4 (unexpected)") }
            case 5|6|7{ global.println("switch(i) i=6 -> case 5|6|7 hit") }
            default{ global.println("switch(i) i=6 -> default (unexpected)") }
        }

        # 源为表达式
        x = 3
        y = 4
        switch( x + y )
        {
            case 6|7{ global.println("switch(x+y) =7 -> case 6|7 hit") }
            default{ global.println("switch(x+y) =7 -> default (unexpected)") }
        }
    }
}

# 测试说明：
# - constIntTest: 单值整数命中/default 兜底。
# - multiValueOrTest: case 1|2|3|4 多值分组、首个命中后停止、无命中走 default。
# - multiValueCommaTest: case 10,11,12 逗号多值分组。
# - floatValueTest: Num 浮点常量匹配（含 2.5|3.5 多值）。
# - stringValueTest: 字符串常量匹配（含 "green"|"blue" 多值）。
# - boolValueTest: true/false 布尔匹配。
# - enumValueTest: case SwitchColor.Red 枚举成员匹配。
# - typePatternIsTest: case is Class3 c3（带绑定）/ case is Class4（不带绑定）/
#   case is Class2（继承关系命中）/ case is int / case is string / 类型与常量混合。
# - defaultValueTest: default 兜底；无 default 且无匹配时直接穿过 switch。
# - nextCaseTest: case 体内 next 继续匹配后续 case；无 next 则停止。
# - parenSourceTest: switch( i ) 带括号源与表达式源。
# - 范围匹配 case >=1 && <=10 暂不支持，等后续版本。
#
# 预期结果：每节输出 hit 行，所有 (unexpected) 行不应出现。
