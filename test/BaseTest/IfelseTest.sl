import Std
import CSharp.SimpleLanguage
import CSharp.System

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
            next
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
            next
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
}

# 测试用例说明：
# - getWhenInstance：非空实例走 if 分支，返回固定字符串。
# - chainWithNext：保留原草稿中的嵌套 if/elif/else 与 next 组合，返回值 m 用于观察控制流是否按预期汇合。
# - FunIfCondition：用已支持语法演示对可空/成员判断；其余 truthiness、tr、三元等仅在注释中列出设计意图。
#
# 预期结果：
# - getWhenInstance 输出 "non-null"。
# - chainWithNext 的 m 与实现细节相关，用于回归时对比历史输出。
# - FunIfCondition 打印 c2.i 为 7；不因未实现语法产生编译错误。
