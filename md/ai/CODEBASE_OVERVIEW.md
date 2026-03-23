# Codebase Overview

This document gives a concise map of the repository and the responsibilities of main folders and files.

**Note:** Paths below reflect the current layout (`source/Front`, `source/VM`). For a fuller index (solution projects, pipeline, bookmarks), see **`PROJECT_MAP.md`** in this folder.

## Top-level source areas

- **`source/Front`** ? compiler frontend: lexer/parser, file meta, semantic core, IR, exports, stdlib sources
  - `Compile/Parse/` ? lexer/tokenizer (`LexerParse.cs`), token/node parsing (`TokenParse.cs` / `FileParse.cs`), structural parsing
  - `Compile/FileMeta/*` ? file-level syntactic constructs
  - `Compile/Process/*` ? compile pipeline and state (`ProcessController`, etc.)
  - `Core/` ? language meta-model: types, functions, variables (`MetaClass`, `MetaMemberFunction`, `MetaVariable`, б─), `ExpressManager`, `MetaExpressNode/*`, `MethodManager`, `ClassManager`, `Statements/*`
  - `IR/` ? intermediate representation (`IRData`, `IROpEnum`, `IR*Statements`, `IR/Core/*`, `IR/Lib/*`)
  - `Export/` ? code generation and backends: **`SLIR/`**, C#, Java, AOT, MLIR, Local PE, etc.
  - `External/Native/` ? native library loading and FFI manifests
  - `OtherLanguage/CSharp/` ? C# interop metadata/IR hooks
  - `Lib/` ? standard library **source** (`.sl` files): `Lib/Core`, `Lib/Std`, б─
  - `Project/` ? project configuration
  - `Wrapper/` ? CLR wrappers for expressions/calls

- **`source/VM`** ? runtime: SLIR load/parse, module registry, VM execution, objects, native bridges
  - `Load/` ? `SLIRAssemblyData`, `SLIRJsonModuleLoader`
  - `Parse/` ? `SLIRModuleParse`, `SLModulePackage`, `SLRuntimeModuleRegistry`
  - `Object/*` ? `SObject` and runtime wrappers
  - `InnerCLRRuntime/*` ? instructions, `SValue`, CLR bridge (`CLRRRuntimeVM`, б─)
  - `NewObject/*`, `LocalRuntime/*` ? allocation and local VM
  - `NativeBridge/*` ? dynamic libraries and language bridges
  - `Runtime/` ? VM facades and types (`CLRVM`, `EVMType`, б─)

- **`source/Log`** ? logging and diagnostics (`Log`, `Diagnostic`, `ErrorDefinition`, б─)

- **`source/CLangdll`** ? C++ native project (Visual Studio), used with native/FFI tooling

## Key observations

- The repo implements an end-to-end toolchain: lexer вк file-meta вк meta model вк expression nodes вк IR вк export (e.g. SLIR JSON) вк VM/runtime.
- Standard library lives under `source/Front/Lib` and compiles with the frontend; runtime support spans `Front/IR/Lib` and `VM`.
- Abstract/override is modeled in meta layers (`MetaMemberFunction` and related).

If you need a per-module file list or a map for a subdirectory, see **`PROJECT_MAP.md`** or request a focused listing for that directory name.
