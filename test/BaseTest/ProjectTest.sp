import ETC1
import MFT

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
        nowMs = Environment.nowMillis()
        #!
        ObjectTest.fun()
        StringTest.fun()
        NumberTest.fun()
        WhileTest.fun()        
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
        RandomTest.fun()        
        EnvironmentTest.fun()
        GuidTest.fun()
        RandomTest.fun()
        Float8Test.fun()
        Float16Test.fun()
        IfelseTest.fun()
        NSClosureTest.ClosureTest.fun()
        #PtrTest.fun()

        MemberFunction1Test.fun()
        MemberFunction2Test.fun()
        MemberStaticFunction.fun()
        TupleTest.fun()
        !#
        BindDataTest.fun()

        LocalTest1.fun()
        LocalTest2.fun()

        ResultTest.fun()

        TableTest.fun()

        nowMs = Environment.nowMillis() - nowMs
        SystemPrintln("===== ProjectTest _main_ end [$nowMs.toString() ms]=====")
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
