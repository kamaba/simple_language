# HookBeforeUndefNegTest - negative case: _before_() references an
# undefined macro on the RIGHT side of a macro assignment.
#
# Expected: compile FAILS.
#   Earliest error: LID 23001 (ProjectMacroManagerMacroUndefined) on
#   `global.macro.notExist` (TryMacroRefToOperand -> undefined macro).
#   The statement is NOT removed by the pre-scan, so it survives into the
#   Meta layer and chains LID 23006 (macro assignment outside the
#   consumed pre-scan set).
#   No HookBeforeUndefNegTest.module.json is produced.
Project
{
    _main_()
    {
    }
    _before_()
    {
        global.macro.platform = global.macro.notExist
    }
}
