# HookProbe Project Guide

<!-- ============================================================
     AI 项目描述区：本文件由模板生成（jsonc "export"."guide" 或 CLI --guide），
     生成后由 AI 在下方填写对项目的整体描述。
     重新生成本文件时，AI-DESCRIPTION:BEGIN/END 标记之间的内容会被原样保留。
     ============================================================ -->
## Project Description (AI)

<!-- AI-DESCRIPTION:BEGIN -->
HookProbe - Project 钩子 `_before_` / `_after_` 的正例探针（`md/project/static-if.md` 的可执行验证用例）。

**覆盖点**

| 宏 | jsonc 初值 | `_before_()` 覆盖 | 运行期预期 | 验证目标 |
|----|-----------|-------------------|-----------|---------|
| `platform` | `"Win32"` | `"Linux"` | `Linux` | 字符串覆盖 |
| `useFastMath` | `true` | `false` | `False` | 布尔翻转 |
| `maxThreads` | `8` | `16` | `16` | 数值改写 |
| `retryLimit` | `-1` | 不覆盖 | `-1` | 负数经 jsonc 宏路径验证；钩子内一元负号数字是 Front 已知限制（表达式 AST 根为符号节点 → LID 23007，见 HookProbe.sp 头注释） |
| `alias` | `"none"` | `global.macro.platform` | `Linux` | 宏引用右值（读到已覆盖的 platform，依赖预扫描顺序） |
| `buildFlavor` | `"release"` | 不覆盖 | `release` | jsonc 初值在无钩子赋值时原样保留 |

**机制验证**

- `_before_()` 内的宏赋值语句由 `PreScanCompileBeforeMacroAssign` 编译期消费并从语句流移除 → IR.txt 中 `_before_` 函数体为空；
- 宏定稿值注入为 Project 静态成员 `macro`，`_main_()` 运行期经 `global.macro.<name>` 只读访问；
- `_after_()` 是管线扩展点（编译后、导出 IR 前），函数体按普通函数编入 IR、VM 不自动调用 → `_main_` 显式调用一次证明函数体存活，其输出只在该显式调用后出现。

**预期 Result.txt**

```
===== HookProbe v1 start =====
platform = Linux
useFastMath = False
maxThreads = 16
retryLimit = -1
alias = Linux
buildFlavor = release
after hook body executed (explicit call from _main_)
===== HookProbe v1 end =====
```

**运行方式**

```powershell
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\Other\HookProbe\HookProbe.sp -e ir
csimple_lang\build\Debug\bin\csimple_lang.exe run simple_language\out\export\HookProbe\HookProbe.module.json
```

**配套负例**（编译期拦截，Front 编译必失败，勿注册进任何用例集 jsonc；四个工程均已验证命中：
MetaCore Failed、IR/Export Skipped、无 module.json 产出。⚠️ dotnet 退出码在编译失败时仍为 0，
判据只看 Front.txt 最早错误 + 无 module.json）：

| 工程 | 最早错误 | 连锁 | 错误数 |
|------|---------|------|--------|
| `test/Other/HookBeforeUndefNegTest` | 23001（右值引用未定义宏 `notExist`） | 23006 | 2 |
| `test/Other/HookBeforeSubscriptNegTest` | 23002（宏引用带下标 `maxThreads[0]`） | 23006 | 2 |
| `test/Other/HookBeforeExprNegTest` | 23007（`1 + 2` 表达式根；`-1` 一元负号同根因） | 23006 | 2 |
| `test/Other/HookMacroOutsideNegTest` | 23006（`_main_` 内宏赋值） | — | 1 |
<!-- AI-DESCRIPTION:END -->

## Project Info

- Project Entry File: `HookProbe.sp`
- Project Config File: `HookProbe.jsonc`

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
