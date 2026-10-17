import Std;

# AttributeExcludeTest - @Exclude attribute（PreCompile 时点）编译期裁剪用例
# 设计基线: csimple_lang/md/design/ATTRIBUTE_DESIGN.md §6.9 + P4-D
# 验证点（module.json 用 csimple_lang.exe info 检查）：
#   1. 成员级：hiddenVar / hiddenStatic 被 @Exclude 标注 → 整体不进 module.json
#   2. 类级：HiddenClass 被 @Exclude 标注 → 整类（含成员）不进 module.json
#   3. 未标注成员（visible / fun）照常编译与运行
# 注意：被 @Exclude 的符号在本文件其他位置不可引用（符号已消失，引用点报编译 Error）

@Exclude
HiddenClass
{
    static int hiddenField = 1

    static hiddenFun()
    {
        Console.println( "HiddenClass.hiddenFun() 不应被编译" )
    }
}

AttributeExcludeTest
{
    @Exclude
    static int hiddenVar = 10

    @Exclude
    static hiddenStatic()
    {
        Console.println( "hiddenStatic() 不应被编译" )
    }

    static visible()
    {
        Console.println( "  visible() 正常编译执行" )
    }

    static fun()
    {
        Console.println( "===== AttributeExcludeTest.fun (@Exclude PreCompile) =====" )
        visible()
        Console.println( "===== AttributeExcludeTest end =====" )
    }
}
