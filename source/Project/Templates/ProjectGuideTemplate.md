# {{ProjectName}} Project Guide

<!-- ============================================================
     AI 项目描述区：本文件由模板生成（jsonc "export"."guide" 或 CLI --guide），
     生成后由 AI 在下方填写对项目的整体描述。
     重新生成本文件时，AI-DESCRIPTION:BEGIN/END 标记之间的内容会被原样保留。
     ============================================================ -->
## Project Description (AI)

<!-- AI-DESCRIPTION:BEGIN -->
{{AiDescription}}
<!-- AI-DESCRIPTION:END -->

## Project Info

- Project Entry File: `{{EntryFile}}`
- Project Config File: `{{ConfigFile}}`

## Project Entry Conventions

### `_main_`

Primary runtime entry. Normal execution starts here.

### `_test_`

Test entry. Used when test mode is enabled.

### `_before_`

Compile pre-hook entry (Project class). Executed before compilation.

### `_after_`

Compile post-hook entry (Project class). Executed after compilation, before IR export.

## Global Integration

`global.xxx` / `global.func()` is integrated with `Project{}` semantic source.

### JSONC `global.data` on `Project`

Data injection surface is `根 -> global -> data` (the `"global"."data"` block) — the only supported location. A root-level `"data"` block is deprecated and no longer read (compiler emits warning LID 20041).

- Primitive values (`int32`/`string`/`float`/`bool`/`null`) are injected as **static members on `Project`** (not on the compiler `global` MetaData shell). Access via `global.<name>`.
- Array values are supported (primitive and nested arrays), e.g. `global.arr[0]`, `global.arr2[1][0]`.
- Object values become `MetaData` shape types, still as **fields on `Project`**, e.g. `global.vardata2.a`.

{{GlobalDataKeys}}
## Example

```jsonc
"global": {
  "imports": ["Some.Module"],
  "data": {
    "var1": 12,
    "arr": [1,2,3],
    "arr2": [[1,2],[3,4]],
    "vardata2": { "a": 10, "b": 20, "flags": [true,false] }
  }
}
```

Access:

- `global.var1`
- `global.arr[0]`
- `global.arr2[1][0]`
- `global.vardata2.a`
- `global.vardata2.b`
