# HookBeforeExprNegTest Project Guide

Negative case for the `_before_()` macro hook — companion of the positive probe
`test/Other/HookProbe/` (mechanism reference: `md/project/static-if.md` §4 / §7).

## Negative Case

- **Trigger**: `_before_()` body assigns `global.macro.maxThreads = 1 + 2` — the right
  side is an arithmetic **expression**. Only constants (bool/number/string) and plain
  macro references `global.macro.X` are allowed as right values.
- **Earliest error**: **LID 23007** (`ProjectMacroManagerMacroNotConst`) — the expression
  AST root is an `Add` symbol node, neither a const term nor a macro ref
  (`CompileBeforeManager.TryEvaluateMacroConstExpress`).
- **Known Front limitation with the same root cause**: unary minus on numbers, e.g.
  `global.macro.retryLimit = -1`, also lands here — `-1` parses as a unary-minus symbol
  node in the expression path, not a const term (see HookProbe.sp header comment).
  Negative macro values must be set via the jsonc `global.macro` initial value instead
  (the jsonc path supports negative numbers, e.g. `"retryLimit": -1`).
- **Chaining**: the statement fails pre-scan evaluation and is NOT removed from the
  statement stream; it survives into the Meta layer and chains **LID 23006**
  (2 errors total).
- **Pass criterion of this negative case** (compile must FAIL):
  - `Logs/Front.txt` earliest Error is 23007, followed by 23006;
  - MetaCore Failed, IR / Export Skipped;
  - no `HookBeforeExprNegTest.module.json` produced.
- ⚠️ dotnet exit code stays 0 even on compile failure — never use the exit code as the
  criterion for negative cases.
- Do NOT register this project into any test-suite jsonc (it can never compile).

## Project Info

- Project Entry File: `HookBeforeExprNegTest.sp`
- Project Config File: `HookBeforeExprNegTest.jsonc`

## Reproduce

```powershell
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\Other\HookBeforeExprNegTest\HookBeforeExprNegTest.sp -e ir
```

## Project Entry Conventions

### `_main_`

Primary runtime entry. Normal execution starts here.

### `_before_`

Compile pre-hook entry (Project class). Executed before compilation.
