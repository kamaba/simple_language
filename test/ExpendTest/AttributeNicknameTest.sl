import Std;

@Nickname("OKK", "Root.ClassOK")
Class1
{
    @Nickname("你","x" )
    int a = 0;

    # 旧方括号语法 ["Am"="mmm"] 已废弃（File 层解析越界），统一 @Name(args) 新语法
    @Nickname("Prt", "Root.Print")
    pinrt()
    {
        # 原嵌套裸块语法已废弃（label 必显式命名），本用例焦点是 Nickname 三级标注，函数体简化
        int localA = 10;
        Console.println( "localA = " + localA );
    }
}
Class2
{
    static int m2 = 10;
    int m = 10;
    Class2( int x )
    {
        this.m = 10;
    }
}