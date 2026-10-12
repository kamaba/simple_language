# AliasProbe - import X as Y 与 typealias 用法探针
# .sp 工程级 typealias（Project 类体内，工程内所有文件可见）覆盖容器/限定名两形态。

import Std

Project
{
    typealias SpIntList = List<int>
    typealias SpAliasAlpha = AliasNS.Sub.Alpha

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
