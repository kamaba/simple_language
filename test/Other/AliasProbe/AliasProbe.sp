# AliasProbe - import X as Y 与 typealias 用法探针
# .sp 工程级 typealias（只能写在 .sp 顶级，工程内所有文件可见）覆盖容器/限定名两形态。

import Std

# .sp 顶级 typealias（Project 声明区，工程内所有文件可见；类体内不允许 typealias）
typealias SpTopKV = Map<string, string>
typealias SpTopAlpha = AliasNS.Sub.Alpha
typealias SpIntList = List<int>
typealias SpAliasAlpha = AliasNS.Sub.Alpha

Project
{
    _main_()
    {
        Console.println( "===== AliasProbe start =====" )
        AliasProbeTests.importAsNamespaceTest()
        AliasProbeTests.importAsModuleTest()
        AliasProbeTests.typeAliasBasicTest()
        AliasProbeTests.typeAliasChainTest()
        AliasProbeTests.typeAliasFuncTest()
        AliasProbeTests.typeAliasProjectScopeTest()
        Console.println( "===== AliasProbe end =====" )
    }
}
