
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
        #ObjectTest.fun()
        #NumberTest.fun()
        #GlobalTest.fun()
        #ArrayTest.fun()
        #AssignStatement.fun()
        #BoolTest.fun()
        #StringTest.fun()
        #TypeTest.fun()
        #DataTest.fun() 
        EnumTest.fun();      
    }
    _test_()
    {
       EnumTest.fun()
    }
    CompileBefore()
    {        
    }
    CompileAfter()
    {        
    }
}