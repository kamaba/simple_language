//****************************************************************************
//  AtSignNegativeTest.sl —— @<tag>(){} 内联块负向用例（预期 Front 编译失败）
//  ------------------------------------------------
//  验证 AtSignLabelSourceRewriter / SLLabelParser 的通道铁律报错（md/syntax/csharp_mono.md §5/§8）：
//    negDoubleOut      双出通道            → LID 20056（Front 初判，先于插件）
//    negMidArrow       中段代码区 <-        → LID 20055（插件三态扫描 CodeArrow）
//    negImportInMid    import 出头区（中段）→ LID 20055（import 行只能出现在头区）
//    negImportBeforeIn import 在入通道之前  → LID 20055（入通道行被挤进中段 → CodeArrow）
//    negDollarInMid    $ 行出尾区（中段）    → LID 20055（出通道行只能出现在尾区）
//    negBadParams      坏参数形态           → LID 20055（参数表须 name=value）
//    negBadSlType      类型标记无法映射      → LID 20055（slType 须在插件映射表内）
//  全部块解析失败后整块原样透传，Lexer 随之报错（本错误在前，符合"看最早 Error"）。
//  驱动：test/Other/AtSignLabel/atsign-negative-test.ps1（断言 Front.txt LID 计数）。
//  本工程独立编译，不进 ProjectTest.jsonc 清单（负例必失败，不能混入主回归）。
//****************************************************************************

class AtSignNegativeTest
{
    # 20056：尾区两个出通道（$a 与 $b 冲突，每块至多 1 个）
    static negDoubleOut()
    {
        int a = 1
        int b = 2
        @csharp_mono()
        {
            var x <- $a
            var r = x + 1;
            $a <- r;
            $b <- r;
        }
    }

    # 20055：中段代码区 <-（<- 只允许头区/尾区）
    static negMidArrow()
    {
        int b = 0
        @csharp_mono()
        {
            var a <- $b
            int r = a <- 1;
            $b <- r;
        }
    }

    # 20055：import 出头区（import 只能出现在头区入通道行之后）
    static negImportInMid()
    {
        int b = 0
        @csharp_mono()
        {
            var a <- $b
            int r = a + 2;
            import SLCSharp;
            $b <- r;
        }
    }

    # 20055：import 在入通道之前——import 行被头区延伸吸收（headExtra），
    # 入通道行 var a <- $b 失去头区身份落进中段，按代码区 <- 报错
    static negImportBeforeIn()
    {
        int b = 0
        @csharp_mono()
        {
            import SLCSharp;
            var a <- $b
            int r = a + 3;
            $b <- r;
        }
    }

    # 20055：$ 行出尾区（出通道行只能出现在块体尾区）
    static negDollarInMid()
    {
        int b = 0
        @csharp_mono()
        {
            var a <- $b
            int r = 0;
            $b <- r + 4;
            int t = r + 5;
        }
    }

    # 20055：坏参数形态（参数表须 name=value 逗号分隔）
    static negBadParams()
    {
        int a = 6
        @csharp_mono( bad-name )
        {
            var x <- $a
            int r = x + 6;
            $a <- r;
        }
    }

    # 20055：入通道类型标记无法映射（slType 须在插件 MapCSType 映射表内；
    # 语言无关原文由插件判定，Front 不解释语义）
    static negBadSlType()
    {
        string name = "sl"
        int a = 7
        @csharp_mono()
        {
            var unknown_tp name <- $name
            int r = name.Length + 7;
            $a <- r;
        }
    }
}
