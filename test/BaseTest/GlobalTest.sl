import Std
import CSharp.System
import CSharpLang.SimpleLanguage

GlobalTest
{
    static fun()
    {
        global.println("========== GlobalTest (start) ==========")
        #global.print(a.toString())
        #SystemPrint(a.toString())
        #Call( BridgeKind.CLR )
        #global.println( "--------------------for enum-------------------" )
        #global.println( "Pi=$global.Pi }" )
        #global.println( "config data $global.var1 " )
        #str = global.arrvar1.toString()
        #global.println( "global arr: $str "  )
        aa = global.vardata2.a
        global.println("config data (vardata2.a+b) -> " + (aa + global.vardata2.b).toString())
        global.println("========== GlobalTest (end) ==========")
    }
}

# 测试说明：依赖 ProjectConfig 中 globalVariable（如 vardata2）的集成用例；其余 global 访问保留为注释便于按需启用。
# 预期：若 vardata2 存在且含数值成员 a、b，则打印二者之和；缺少配置时编译或运行失败属预期，可改回注释行做离线语法检查。
