# HookBeforeSubscriptNegTest Project Guide

Negative case for the `_before_()` macro hook — companion of the positive probe
`test/Other/HookProbe/` (mechanism reference: `md/project/static-if.md` §4 / §7).

## Negative Case

- **Trigger**: `_before_()` body assigns `global.macro.platform = global.macro.maxThreads[0]` —
  the right side is a macro reference carrying a **subscript**. The same LID 23002 guard
  also rejects function calls and template arguments on macro references
  (`isCallFunction` / `isArray` / `isTemplate` in `CompileBeforeManager.TryMacroRefToOperand`).
- **Earliest error**: **LID 23002** (`ProjectMacroManagerNotSupportExpress`).
- **Chaining**: the statement fails pre-scan evaluation and is NOT removed from the
  statement stream; it survives into the Meta layer and chains **LID 23006**
  (2 errors total).
- **Pass criterion of this negative case** (compile must FAIL):
  - `Logs/Front.txt` earliest Error is 23002, followed by 23006;
  - MetaCore Failed, IR / Export Skipped;
  - no `HookBeforeSubscriptNegTest.module.json` produced.
- ⚠️ dotnet exit code stays 0 even on compile failure — never use the exit code as the
  criterion for negative cases.
- Do NOT register this project into any test-suite jsonc (it can never compile).

## Project Info

- Project Entry File: `HookBeforeSubscriptNegTest.sp`
- Project Config File: `HookBeforeSubscriptNegTest.jsonc`

## Reproduce

```powershell
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\Other\HookBeforeSubscriptNegTest\HookBeforeSubscriptNegTest.sp -e ir
```

## Project Entry Conventions

### `_main_`

Primary runtime entry. Normal execution starts here.

### `_before_`

Compile pre-hook entry (Project class). Executed before compilation.
