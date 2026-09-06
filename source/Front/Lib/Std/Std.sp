Project
{
    # 模块级静态成员变量：引用方通过 Std.name 限定访问
    # 注意：Project 成员名不允许与 Std 模块下定义的名称（namespace/data/class/enum）相同
    Float64 Pi = 3.14159265358979
    Float64 E = 2.71828182845904
    Int32 MaxThreadCount = 8
    string ModuleVersion = "1.0.0"

    # 模块级静态成员函数：引用方通过 Std.name() 限定调用
    Int32 Add(Int32 a, Int32 b)
    {
        ret a + b
    }

    Float64 CircleArea(Float64 r)
    {
        ret Pi * r * r
    }

    string Hello()
    {
        ret "hello from Std.Project"
    }

    _main_()
    {
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
