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
        StaticIfTest.fun()
    }
    CompileBefore()
    {
        # global.macro 只允许在 CompileBefore() 中修改
        # 编译期把 platform 从 jsonc 里的 "Win32" 改为 "Linux"
        global.macro.platform = "Linux"
    }
    CompileAfter()
    {
    }
}
