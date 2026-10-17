# HookBeforeSubscriptNegTest - negative case: _before_() macro reference
# carries a subscript (same LID 23002 guard also rejects function calls and
# template args on macro refs: isCallFunction / isArray / isTemplate).
#
# Expected: compile FAILS.
#   Earliest error: LID 23002 (ProjectMacroManagerNotSupportExpress) on
#   `global.macro.maxThreads[0]` (TryMacroRefToOperand rejects subscript).
#   The statement is NOT removed by the pre-scan, so it survives into the
#   Meta layer and chains LID 23006.
#   No HookBeforeSubscriptNegTest.module.json is produced.
Project
{
    _main_()
    {
    }
    _before_()
    {
        global.macro.platform = global.macro.maxThreads[0]
    }
}
