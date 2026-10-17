# 非本语言关键字拦截探针 round3：词法层统一拦截验证
# 背景：return 曾作为普通标识符流入语义层，产生 Name:return not found! 等误导性报错（静默/报错
#       表现还不稳定）。2026-10-11 起词法层统一拦截：命中拦截表即报 Error（return=20118 专用，
#       其余=20119 通用）并阻断编译，报错 Extend 附带改写指引。本探针为该机制的回归用例。
import Std;

class ReturnProbe
{
    # A2: 实例方法 + 函数体顶层 + 数字字面量（FrameData.frameCount 形态）
    Int32 giveFive()
    {
        return 5;
    }

    # B2: 实例方法 + if 块内 + 字符串字面量（TemplateClass.ToTest 形态）
    string toTest()
    {
        if true
        {
            return "Int32";
        }
        return "fallback";
    }

    # C2: 实例方法 + 函数体顶层 + 字符串字面量
    string giveStr()
    {
        return "top";
    }

    # D2: 实例方法 + 尾部无分号（Sort.merge 形态）
    Int32 giveTail()
    {
        Int32 v = 1
        return v
    }

    # E3: 其他语言常见关键字（SL 无对应语法）——统一走 LID 20119
    static probeOtherLangKeywords()
    {
        foreach item in list
        using Std;
        elseif a > b
        struct Point { }
        let z = 1
        raise "err"
        lambda f = 1
        def helper()
        package Core
        include Std
        impl Foo
        trait Bar
        pub Int32 counter = 0
        unless flag
        until flag
    }

    static run()
    {
        Console.println( "ReturnProbe done" )
    }
}
