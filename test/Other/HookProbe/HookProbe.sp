# HookProbe v1 - Project `_before_` / `_after_` hook probe.
#
# _before_(): compile-time macro override entry. Only plain assignments
#   `global.macro.<name> = <const | macro ref>` are consumed by the
#   pre-scan (value fixed at compile time, statement removed from the
#   statement flow - it never reaches IR/runtime):
#     platform    "Win32"   -> "Linux"   string override
#     useFastMath true      -> false     bool flip
#     maxThreads  8         -> 16        number rewrite
#     retryLimit  -1 (jsonc initial value, hook does NOT touch it)
#                                     negative number via the jsonc macro path;
#                                     hook-side unary minus on numbers is a
#                                     known Front limitation (express AST root
#                                     is a unary-minus symbol node, not a
#                                     const term -> LID 23007)
#     alias       "none"    -> "Linux"   macro-ref right value (reads the
#                                     already-overridden platform, so the
#                                     pre-scan order matters)
#     buildFlavor "release" unchanged   (no hook assignment -> jsonc value)
#
# _after_(): pipeline extension point (after compile, before IR export);
#   its body is compiled into IR as a normal function. The VM does not
#   auto-invoke it - _main_ calls it explicitly to prove the body is live.
Project
{
    _main_()
    {
        SystemPrintln( "===== HookProbe v1 start =====" )
        SystemPrintln( "platform = " + global.macro.platform )
        SystemPrintln( "useFastMath = " + global.macro.useFastMath.toString() )
        SystemPrintln( "maxThreads = " + global.macro.maxThreads.toString() )
        SystemPrintln( "retryLimit = " + global.macro.retryLimit.toString() )
        SystemPrintln( "alias = " + global.macro.alias )
        SystemPrintln( "buildFlavor = " + global.macro.buildFlavor )
        _after_()
        SystemPrintln( "===== HookProbe v1 end =====" )
    }
    _before_()
    {
        global.macro.platform = "Linux"
        global.macro.useFastMath = false
        global.macro.maxThreads = 16
        global.macro.alias = global.macro.platform
    }
    _after_()
    {
        SystemPrintln( "after hook body executed (explicit call from _main_)" )
    }
}
