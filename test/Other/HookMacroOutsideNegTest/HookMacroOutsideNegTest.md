# HookMacroOutsideNegTest Project Guide

Negative case for the `_before_()` macro hook — companion of the positive probe
`test/Other/HookProbe/` (mechanism reference: `md/project/static-if.md` §4 / §7).

## Negative Case

- **Trigger**: `global.macro.platform = "Linux"` is written inside `_main_()` — a macro
  assignment **outside** the `_before_()` hook.
- **Mechanism**: the pre-scan (`PreScanCompileBeforeMacroAssign`) only scans the Project
  `_before_()` body, so this statement is never consumed; it survives into the Meta
  layer, where ANY surviving `global.macro` assignment is rejected by
  `MetaAssignStatements` (the TryGetMacroRefName check).
- **Earliest / only error**: **LID 23006**
  (`ProjectMacroManagerMacroOnlyModifyInCompileBefore`) — single error, no chaining:
  the right value is a valid constant and the Meta assign statement aborts right after
  reporting (1 error total).
- **Pass criterion of this negative case** (compile must FAIL):
  - `Logs/Front.txt` contains exactly one Error: 23006;
  - MetaCore Failed, IR / Export Skipped;
  - no `HookMacroOutsideNegTest.module.json` produced.
- ⚠️ dotnet exit code stays 0 even on compile failure — never use the exit code as the
  criterion for negative cases.
- Do NOT register this project into any test-suite jsonc (it can never compile).

## Project Info

- Project Entry File: `HookMacroOutsideNegTest.sp`
- Project Config File: `HookMacroOutsideNegTest.jsonc`

## Reproduce

```powershell
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\Other\HookMacroOutsideNegTest\HookMacroOutsideNegTest.sp -e ir
```

## Project Entry Conventions

### `_main_`

Primary runtime entry. Normal execution starts here.

### `_before_`

Compile pre-hook entry (Project class). Executed before compilation.
