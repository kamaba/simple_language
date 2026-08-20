import ETC1

Project
{
    println( txt )
    {
        SystemPrintln( txt )
    }
    print( text )
    {
        SystemPrint( text )
    }
    _main_()
    {     
        SystemPrintln("===== ProjectTest _main_ start =====")   
        #!
        ObjectTest.fun()
        StringTest.fun()
        NumberTest.fun()
        GlobalTest.fun()
        BoolTest.fun()
        TypeTest.fun()
        EnumTest.fun()
        DataTest.fun()
        ArrayTest.fun()
        BlockTest.fun()
        CallLinkTestNS.CallLinkTest.fun()
        Class1TestSmoke.fun()
        Class2TestSmoke.fun()
        ClassAs_IsNS.ClassAs_Is.fun()
        CommitTest.fun()
        ConStrCC.ConstructionTest.fun()
        ExpressTest.fun()
        ExtendsClass.fun()
        AssignStatement.fun()
        NCTest.fun()
        ForTest.fun()
        GenClass_Interface.fun()
        GenClass.fun()
        GC2.GenClass2.fun()
        DeferTest.fun()
        TryTest.fun()
        CheckedCalcTest.fun()
        GC3.GenClass3.fun()
        RangeTest.fun()
        InterfaceTest.fun()
        MVTest1.fun()
        MVTest2.fun()        
        EnvironmentTest.fun()
        GuidTest.fun()
        !#
        RandomTest.fun()
        PtrTest.fun()
        SystemPrintln("===== ProjectTest _main_ end =====")
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
