# return 关键字误用探针 round2：定位静默 vs 报错的差异因子
# 假设：static 方法中 return 报错，实例方法中 return 静默（FrameData/Sort/TemplateClass 均实例方法）
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

    static run()
    {
        Console.println( "ReturnProbe done" )
    }
}
