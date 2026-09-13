import Std;

# Std.name 限定访问测试：
# Std 模块 Project{} 下定义的静态成员（Pi / E / MaxThreadCount / ModuleVersion / Add / CircleArea / Hello）
# 引用方工程通过 Std.name 直接限定访问，与 Std 模块下的类型（IO/Text/OS/Net/Data/DB/Component/Node）共用同一命名空间
ModuleGlobalTest
{
    # 用例1：读取 Std.Project 静态成员变量
    static testProjectVar()
    {
        Console.println("===== ModuleGlobalTest testProjectVar =====")
        Console.println("Std.Pi = " + Std.Pi.toString())                            # 3.14159265358979
        Console.println("Std.E = " + Std.E.toString())                              # 2.71828182845904
        Console.println("Std.MaxThreadCount = " + Std.MaxThreadCount.toString())    # 8
        Console.println("Std.ModuleVersion = " + Std.ModuleVersion)                # 1.0.0
    }

    # 用例2：调用 Std.Project 静态成员函数
    static testProjectFunction()
    {
        Console.println("===== ModuleGlobalTest testProjectFunction =====")
        Console.println("Std.Add(3, 4) = " + Std.Add(3, 4).toString())             # 7
        Console.println("Std.CircleArea(2.0) = " + Std.CircleArea(2.0).toString()) # 12.5663706143592
        Console.println("Std.Hello() = " + Std.Hello())                            # hello from Std.Project
    }

    # 用例3：静态成员参与表达式运算后赋给局部变量
    static testProjectExpression()
    {
        Console.println("===== ModuleGlobalTest testProjectExpression =====")
        Float64 r = 1.5
        Float64 area = Std.Pi * r * r
        Console.println("area(Std.Pi * 1.5 * 1.5) = " + area.toString())           # 7.06858347057703

        Int32 total = Std.Add(1, 2) + Std.MaxThreadCount
        Console.println("total(Std.Add(1,2) + Std.MaxThreadCount) = " + total.toString())  # 11

        Float64 diff = Std.Pi - Std.E
        Console.println("diff(Std.Pi - Std.E) = " + diff.toString())               # 0.42331082513075
    }

    # 用例4：与模块类型限定访问混用（Text.Csv / Console 与 Std.name 并存，两条解析链路互不干扰）
    static testProjectMixed()
    {
        Console.println("===== ModuleGlobalTest testProjectMixed =====")
        var c = Text.Csv("name,score\nalice,90\nbob,85")
        Console.println("Csv rowCount = " + c.rowCount.toString())                  # 2
        Console.println("Csv columnCount = " + c.columnCount.toString())           # 2
        Console.println("Std.E = " + Std.E.toString())                              # 2.71828182845904
        Console.println("sum(score) * Std.E = " + (c.sum("score") * Std.E).toString())  # 475.699319980332
    }

    static fun()
    {
        testProjectVar()
        testProjectFunction()
        testProjectExpression()
        testProjectMixed()
    }
}

# 测试说明：
# 1) 依赖 Std.sp -> Project 的静态成员 Pi / E / MaxThreadCount / ModuleVersion 与函数 Add / CircleArea / Hello
# 2) 覆盖 Std.name 读静态变量、调静态函数、静态成员参与表达式、与模块类型访问混用 四条链路
# 3) 依赖 references 中的 Std.module.json（out/export/Std）
