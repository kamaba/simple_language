# HookMacroOutsideNegTest - negative case: global.macro assignment
# OUTSIDE the _before_() hook (here: inside _main_).
#
# The pre-scan (PreScanCompileBeforeMacroAssign) only scans the Project
# _before_() body, so this statement is never consumed; it survives into
# the Meta layer, where ANY surviving global.macro assignment is rejected
# (MetaAssignStatements -> TryGetMacroRefName check).
#
# Expected: compile FAILS.
#   Single error: LID 23006 (ProjectMacroManagerMacroOnlyModifyInCompileBefore).
#   No chained errors (the right value is a valid constant, and the Meta
#   assign statement aborts right after reporting).
#   No HookMacroOutsideNegTest.module.json is produced.
Project
{
    _main_()
    {
        global.macro.platform = "Linux"
    }
}
