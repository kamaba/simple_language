
Project
{
    string cpk = "cpkkk"
    float Pi = 3.14f
    print( string text )
    {
        Array<BridgeObject> paramObjs = new(1)
        paramObjs[0] = BridgeObject(text)
        NativeBridge.Call( BridgeKind.CLR, "System", "Console", "WriteLine", null, paramObjs );
    }
    _main_()
    {
        #ObjectTest.fun()
        #NumberTest.fun()
        #StringTest.fun()
        #TypeTest.fun()

        #a = 2 + 4
        #global.print(a.toString())
        #SystemPrint(a.toString())
        Call( BridgeKind.CLR )
    }
    Call( BridgeKind kind )
    {
        if kind == BridgeKind.CLR
        {
            SystemPrint("----")
        }
    }
    _test_()
    {
       #TempTest.Fun();
    }
    CompileBefore()
    {        
    }
    CompileAfter()
    {        
    }
}

# 在newproject后，会生成两个文件 。一个是 项目名称.sp 是扫描的入口 还有一个是项目名称.config这个是通过json配置的工程
# 在.sp中，可以放global,也可以通过配置json的方式配置global.sl
# 在.sp中，有Main是函数的正式入口 有Test是测试入口 ，如果在cmd中，可以加-test 可以就走test入口
# 在Project中，可以写CompileBefore即编译前的先执行函数，和CompileAfter的函数，也可以设计 ImportDll, ExportDll的导入插件相关的函数