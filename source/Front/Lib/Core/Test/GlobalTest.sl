import CSharp.System
import CSharpLang.SimpleLanguage

GlobalTest
{
    static fun()
    {
        #global.print(a.toString())
        #SystemPrint(a.toString())
        #Call( BridgeKind.CLR )
        #global.println( "--------------------for enum-------------------" )
        #global.println( "Pi=$global.Pi }" )
        #global.println( "config data $global.var1 " )
        #str = global.arrvar1.toString()
        #global.println( "global arr: $str "  )
        aa = global.vardata2.a
        global.println( "config data ${aa + global.vardata2.b} " )
    }
}