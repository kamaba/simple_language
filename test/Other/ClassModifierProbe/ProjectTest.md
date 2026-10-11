# ProjectTest Project Guide

- Project Entry File: `ProjectTest.sp`
- Project Config File: `ProjectTest.jsonc`

## Project Entry Conventions

### `_main_`

Primary runtime entry. Normal execution starts here.

### `_test_`

Test entry. Used when test mode is enabled.

### `_before_`

Compile pre-hook entry (from `Compile` class). Executed before compile core flow when configured.

### `_after_`

Compile post-hook entry (from `Compile` class). Executed after compile core flow when configured.

## Global Integration

`global.xxx` / `global.func()` is integrated with `Project{}` semantic source.

### JSONC `data` on `Project`

Prefer a root-level `"data": { ... }` block. Legacy `"global"."data"` is still read; root keys override on name clash.

- Primitive values (`int32`/`string`/`float`/`bool`/`null`) are injected as **static members on `Project`** (not on the compiler `global` MetaData shell). Access via `global.<name>`. 
- Array values are supported (primitive and nested arrays), e.g. `global.arr[0]`, `global.arr2[1][0]`. 
- Object values become `MetaData` shape types, still as **fields on `Project`**, e.g. `global.vardata2.a`. 

## Example

```jsonc
"data": {
  "var1": 12,
  "arr": [1,2,3],
  "arr2": [[1,2],[3,4]],
  "vardata2": { "a": 10, "b": 20, "flags": [true,false] }
},
"global": {
  "imports": ["Some.Module"]
}
```

Access:

- `global.var1`
- `global.arr[0]`
- `global.arr2[1][0]`
- `global.vardata2.a`
- `global.vardata2.b`
