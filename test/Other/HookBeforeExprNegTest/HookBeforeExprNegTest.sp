# HookBeforeExprNegTest - negative case: _before_() assigns an EXPRESSION
# to a macro. Only constants (bool/number/string) and plain macro refs
# `global.macro.X` are allowed as right values.
#
# Expected: compile FAILS.
#   Earliest error: LID 23007 (ProjectMacroManagerMacroNotConst) on
#   `1 + 2` (AST root is an Add symbol node - neither a const term nor a
#   macro ref). Same root cause as unary minus: `global.macro.x = -1`
#   also lands here because `-1` parses as a unary-minus symbol node
#   (known Front limitation, see HookProbe.sp header comment).
#   The statement is NOT removed by the pre-scan, so it survives into the
#   Meta layer and chains LID 23006.
#   No HookBeforeExprNegTest.module.json is produced.
Project
{
    _main_()
    {
    }
    _before_()
    {
        global.macro.maxThreads = 1 + 2
    }
}
