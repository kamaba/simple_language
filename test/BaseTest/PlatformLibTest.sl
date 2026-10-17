# PlatformLibTest - P2 lib/SDK probe test case (design §9.2 / §12.8 / §16).
#
# jsonc fixtures (ProjectTest.jsonc platform.require):
#   "lib":    [ { "name": "sqlite3" } ]  - hard req, sqlite3.dll sits next to
#                                          the C VM exe -> must load & pass
#   "sdk":    [ { "name": "cuda" } ]     - soft req (optional), absent host
#                                          SDK downgrades to Warning
#   "custom": [ { "name": "p2smoke" } ]  - no host hook -> default pass (§9.3)
#
# Split into two assertion styles:
#   check()   - host-independent facts (fake lib absent, current/probe mirror,
#               empty descriptive fields). Same verdict in every host.
#   observe() - environment-dependent observations (sqlite3 presence / version
#               / path). Printed raw; the exe-run script
#   csimple_lang/scripts/test-platform-lib.ps1 asserts the exact output,
#   including the --disable lib.sqlite3 current-vs-probe divergence.
# Note: SystemPlatform* dispatch is CVM-only (same as PlatformOverrideTest).

PlatformLibTest
{
    # 统一断言辅助：cond 为 true 打印 OK，否则打印 FAIL
    static check( string name, bool cond )
    {
        if cond
        {
            SystemPrintln( "[PlatformLibTest] " + name + " : OK" )
        }
        else
        {
            SystemPrintln( "[PlatformLibTest] " + name + " : FAIL" )
        }
    }

    # 布尔观测：打印原值（不断言），由 exe 直跑脚本断言
    static observeB( string name, bool val )
    {
        if val
        {
            SystemPrintln( "[PlatformLibTest] " + name + " : true" )
        }
        else
        {
            SystemPrintln( "[PlatformLibTest] " + name + " : false" )
        }
    }

    # 字符串观测：打印原值（不断言），由 exe 直跑脚本断言
    static observeS( string name, string val )
    {
        SystemPrintln( "[PlatformLibTest] " + name + " : " + val )
    }

    static fun()
    {
        SystemPrintln( "========== PlatformLibTest (start) ==========" )

        string fake = "nosuchlib_p2xyz"

        # --- 1. fake lib: deterministic on every host (§9.2 probe miss) ---
        check( "fake libExists == false", Environment.current.libExists( fake ) == false )
        check( "fake probe.libExists == false", Environment.probe.libExists( fake ) == false )
        check( "fake libVersion == empty", Environment.current.libVersion( fake ) == "" )
        check( "fake probe.libVersion == empty", Environment.probe.libVersion( fake ) == "" )
        check( "fake libPath == empty", Environment.current.libPath( fake ) == "" )
        check( "fake libHas(presence) == false", Environment.current.libHas( fake, "" ) == false )
        check( "fake probe.libHas(presence) == false", Environment.probe.libHas( fake, "" ) == false )
        check( "fake current/probe version mirror",
            Environment.probe.libVersion( fake ) == Environment.current.libVersion( fake ) )

        # --- 2. no override entry for the fake lib key (§8.5.6) ---
        check( "fake Override.source == empty", Environment.Override.source( "lib." + fake ) == "" )
        check( "fake Override.isDisabled == false", Environment.Override.isDisabled( "lib." + fake ) == false )

        # --- 3. sqlite3 observations (exe-run script asserts; §9.2) ---
        observeB( "cur.libExists(sqlite3)", Environment.current.libExists( "sqlite3" ) )
        observeB( "prb.libExists(sqlite3)", Environment.probe.libExists( "sqlite3" ) )
        observeS( "cur.libVersion(sqlite3)", Environment.current.libVersion( "sqlite3" ) )
        observeS( "cur.libPath(sqlite3)", Environment.current.libPath( "sqlite3" ) )
        observeB( "cur.libHas(sqlite3,presence)", Environment.current.libHas( "sqlite3", "" ) )
        observeB( "cur.libHas(sqlite3,min3)", Environment.current.libHas( "sqlite3", "3" ) )
        observeB( "prb.libHas(sqlite3,min3)", Environment.probe.libHas( "sqlite3", "3" ) )

        SystemPrintln( "========== PlatformLibTest (end) ==========" )
    }
}
