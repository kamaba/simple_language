import ETC1

Project
{
    string cpk = "cpkkk"
    float Pi = 3.14f
    Global()
    {
    }
    print( object str )
    {        
        SystemPrint(str)
    }
    println( object str )
    {
        SystemPrintln(str)
    }
    _main_()
    {
        #!
        ObjectTest.fun()
        NumberTest.fun()
        GlobalTest.fun()        
        ArrayTest.fun()        
        AssignStatement.fun()
        BoolTest.fun()
        StringTest.fun()        
        TypeTest.fun()        
        DataTest.fun()
        EnumTest.fun();   
        BlockTest.fun()   
        CallLinkTestNS.CallLinkTest.fun();
        Class1TestSmoke.fun();
        Class2TestSmoke.fun();
        ClassAs_IsNS.ClassAs_Is.fun();
        CommitTest.fun();
        ConStrCC.ConstructionTest.fun()
        ExpressTest.fun()
        !#
        ExtendsClass.fun()     
        
    }
    _test_()
    {
       ETC1.ExtendsClass.fun()
    }
    CompileBefore()
    {        
    }
    CompileAfter()
    {        
    }
}