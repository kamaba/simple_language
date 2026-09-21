Project
{
    _main_()
    {
        # calculator 已从 ConsoleTest.fun() 中注释掉（交互式阻塞 stdin），可在此单独调用
        #!
        LinkedListTest.fun()
        CsvTest.fun()
        ListTest.fun()
        MapTest.fun()
        FileTest.fun()
        DirectoryTest.fun()
       #ConsoleTest.fun()
        DateTimeTest.fun()
        Sqlite3Test.fun()
        CsvTest.fun();
        ModuleGlobalTest.fun()
        !#
        # IsolateTest 置于末尾:其 IO 组存在遗留缺陷(闭包捕获 string 到 worker 为空,
        # worker 报错不可发送导致 VM 提前退出),放最后不影响其余测试执行
        #ComponentTest.fun()
        #StreamFileTest.fun()
        #SerializeTest.fun()
        ZlibTest.fun()
        GZipTest.fun()
        Base64Test.fun()
        #EncodingTest.fun() # var 推断暂不支持(Node 层关键字冲突),待 var 语法落地后启用
        LogTest.fun()
        #IsolateTest.fun()
        # LogFatalTest 必须放最末尾：fatal 触发 fatal_halt 硬停，整个进程终止（退出码 1）
        LogFatalTest.fun()
    }
    CompileBefore()
    {
    }
    CompileAfter()
    {
    }
}
