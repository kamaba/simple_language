# SimpleLanguage — Releases and Roadmap [中文](https://github.com/kamaba/simple_language/blob/main/Release_CN.md)

This document is the English counterpart of **`Release_CN.md`**. It summarizes the **end-to-end implementation plan** (roadmap), the **current snapshot v0.0.001**, and how it relates to **`target.md`** at the repository root. Fine-grained ideas remain in `target.md`; here they are grouped for planning.

---

## 1. Overall design: implementation phases (roadmap)

Phases are ordered by **dependency**. Items inside a phase may proceed in parallel.

### Phase 1 — Language core and type system

| Step | Scope |
|------|--------|
| 1.1 | **Core framework and syntax**: lexer/parser pipeline, file and namespace layout, diagnostics and logging. |
| 1.2 | **Control flow**: `if` / `elif` / `else`, `for` / `while`, `break` / `continue`, `switch` (including expression-style forms where applicable). |
| 1.3 | **Type system**: `as` / `is`; `string` and fixed-width numeric types (`int8`–`int64`, etc.) with coherent rules. |
| 1.4 | **OO core**: `namespace`, `class`, `data`, `enum`; `extends`; `interface` and implementation rules. |
| 1.5 | **Sequences**: `array`, `range`, indexing, and interaction with loops. |
| 1.6 | **Operators and scope**: operator overloading (e.g. `_add_`, `_sub_`, …); **`global` / `local`** and related scoping rules kept consistent with IR/VM. |

### Phase 2 — Project model and CLI

| Step | Scope |
|------|--------|
| 2.1 | **Project as part of the language**: entry file, compile list, globals, and source roots (today: **`*.sp` + same-name `*.jsonc`**). |
| 2.2 | **CLI**: new project, register sources, compile, export IR, run/debug entry points; integrate with logs and `DebugCode` artifacts. |

### Phase 3 — Extended keywords and metaprogramming

| Step | Scope |
|------|--------|
| 3.1 | **`bind` / `result` / `trycatch` / `attribute`**, etc.: grow these on top of a stable core while keeping IR/VM semantics aligned. |

### Phase 4 — Multi-backend and native integration

| Step | Scope |
|------|--------|
| 4.1 | **MLIR / LLVM path**: export selected function bodies to MLIR (or equivalent) and invoke from **VM or runtime** as native/precompiled where appropriate. |
| 4.2 | **VM rewrite**: move the VM from **C#** toward **C** (or C-centric) for performance and simpler native/FFI integration. |

### Phase 5 — Containers and standard library

| Step | Scope |
|------|--------|
| 5.1 | **Constraint container system**: `list`, `set`, `map`, `queue`, `link`, … under shared typing/interface rules. |
| 5.2 | **`std` modules**: staged delivery of `io`, `net`, `os`, `sys`, `text`, `component`, `node`, `table`, `tree`, … wired to project `references` / module graph. |

### Phase 6 — Protocols and async

| Step | Scope |
|------|--------|
| 6.1 | **Protocol system** and **`async` / `await`**, finalized together with scheduling and VM threading. |

### Phase 7 — Editor and debugging

| Step | Scope |
|------|--------|
| 7.1 | **VSCode extension**: CLI-backed compile/run; later iterations for breakpoints, stacks, variables. |

### Phase 8 — Math types, rendering, debug visualization

| Step | Scope |
|------|--------|
| 8.1 | **Common math types**: `matrix`, `vector2`/`vector3`, `color`, … implemented as low-level as practical plus a **`math` library**. |
| 8.2 | **Debug rendering**: debug-mode `render`-style hooks with **OpenGL** output (optional Vulkan/DX12 paths appear in `target.md`). |

### Phase 9 — FFI, concurrency, UI architecture

| Step | Scope |
|------|--------|
| 9.1 | **FFI**: packaged calls into **DLL** and other native libraries. |
| 9.2 | **Isolate / threading** model; **UI rendering** split from application logic. |

### Phase 10 — Windows, cross-language, data visualization

| Step | Scope |
|------|--------|
| 10.1 | **Windowing**: create windows and tie into the render pipeline. |
| 10.2 | **Cross-language libraries**: e.g. **JavaScript / C# / Native** interop (aligned with `@js` / `@cs` / `@c`-style ideas in `target.md`). |
| 10.3 | **Tables and charts**: load tabular data and render statistics into a window. |

### Phase 11 — Scientific stack, web, deployment

| Step | Scope |
|------|--------|
| 11.1 | **Scientific ecosystem**: **NumPy**, **Transformers**, common training stacks (likely via host or subprocess FFI, phased). |
| 11.2 | **Web services**: richer API surface, **Swagger**, small services and **minimal packaged deployment**. |

### Phase 12 — IDE and AI assistance

| Step | Scope |
|------|--------|
| 12.1 | **Qt-based open-source IDE**: install, project management, debugging, emphasizing **scientific and visualization** workflows. |
| 12.2 | **AI-assisted codegen**: after the language and libraries stabilize, train models on canonical corpora so generated code compiles and tests cleanly. |

### Relationship to `target.md`

`target.md` captures finer ideas (**CLR / JS / WASM** backends, **Markdown-integrated comments**, **profiling/reflection/Table types**, **embedded multi-language snippets**, **in-browser WASM compilation**, …). When scheduling, **map those bullets** onto the phases above or spin them into their own milestones.

---

## 2. Current snapshot: v0.0.001 (2026-04-18)

**v0.0.001** labels the **repository snapshot** of what runs on the mainline today—not necessarily a semver shipping artifact. Scope is split into **shipped on mainline**, **partial/experimental**, and **out of scope for this label**.

### 2.1 On mainline (usable)

- **Front compiler and VM** (C#): **`.sl`** sources compile to **IR** and **`*.module.json`** for the VM.  
- **Projects**: **`*.sp` + same-name `*.jsonc`** for roots, file lists, options, `global.data`; **`_main_` / `_test_`** and `Project{}` / `global` integration (see `md/project/`).  
- **CLI**: project scaffolding, class-file registration, compile, IR export (see `md/project/project-config-jsonc-guide.md`).  
- **Types**: multiple integer/unsigned/float types, strings, booleans; **`as` / `is`**; **`enum`**.  
- **Control flow**: **`if` / `elif` / `else`**, **`for` / `while` / `do-while`**, **`switch`**, **`break` / `continue`** (covered by tests).  
- **OO**: **`namespace`**, **`class`**, **`extends`**, **`interface`**; methods, statics, template-class scenarios in tests.  
- **Arrays and ranges**: **`array`**, **`range`** with tests.  
- **Operator overloading**: convention-based overload tests.  
- **Scoping**: **`global` / `local`** tied to project and metadata.  
- **Keywords (partial)**: **`bind`**; **`attribute`** scenarios; **`try` / `catch`** present in the lexer/parser stack (full semantics: follow tests and syntax docs).  
- **C# interop**: binding/replacement-style tests and infrastructure.  
- **Diagnostics**: `Logs/`, `DebugCode/` pipeline and env vars (`md/ai/DEBUG_WORKFLOW.md`, `EXPORT_PATHS.md`).  
- **Core library**: many **`.sl`** primitives under `samples/SLang/source`, driven by `Core.jsonc`.

### 2.2 Partial or experimental

- **MLIR**: export/toolchain hooks exist (e.g. external `mlir-opt`); **not** the same as stable per-function export plus VM integration.  
- **Constraint containers**: docs and some tests (e.g. **List**); a **unified** `list/set/map/queue/link` constraint system remains on the roadmap.  
- **ExpendTest** scenarios (e.g. coroutine-style tests): **experimental** until promoted and documented.

### 2.3 Not covered by v0.0.001 (see Section 1)

Including but not limited to: **async/await as a finalized protocol**, **full `std(io/net/…)`**, **published VSCode extension**, **OpenGL debug render path**, **production FFI layer**, **Isolate threading model**, **C VM rewrite as primary runtime**, **first-class matrix/vector/color**, **unified JS interop**, **NumPy/training stack**, **Qt IDE**, **Swagger/minimal web deploy**, **in-house AI codegen**, etc.

---

## 3. Historical entries

### v0.0.5 [2022-12-23] by kamaba

1. Front-end parsing integrated with language logic.  
2. No `array` or template support yet.  
3. Simple VM.  
4. IR lowering/export.  
5. Basic execution.  
6. Basic types: `byte`, `sbyte`, `char`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `string`.

> The tree has evolved since (e.g. arrays, template-class tests, JSONC projects). This block is **archival** relative to **v0.0.001**.
