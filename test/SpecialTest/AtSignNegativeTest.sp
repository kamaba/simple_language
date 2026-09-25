Project
{
    _main_()
    {
        # 负向用例工程：编译期必失败（改写器 20055/20056 + 透传块 Lexer 报错），
        # 此入口仅为工程结构完整（驱动脚本只断言 Front.txt LID 计数，不运行 VM）。
        SystemPrintln( "unreachable: negative cases must fail Front compile" );
    }
}
