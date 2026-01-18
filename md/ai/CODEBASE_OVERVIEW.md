# Codebase Overview

This document gives a concise map of the repository and the responsibilities of main folders and files.

Top-level source areas

- `source/Compile` ！ lexer, token parser, file-level parsing, compile phases
  - `Parse/LexerParse.cs` ！ lexer/tokenizer
  - `Parse/TokenParse.cs` ！ token -> node tree
  - `FileMeta/*` ！ file-level syntactic constructs
  - `Process/*` ！ compile-state machinery

- `source/Core` ！ language meta-model, semantic analysis and expression management
  - `MetaClass.cs`, `MetaMemberFunction.cs`, `MetaVariable.cs` ！ meta objects for types, functions, variables
  - `ExpressManager.cs` ！ create typed expression nodes, perform simple optimizations
  - `MetaExpressNode/*` ！ expression AST nodes and related parsing
  - `MethodManager.cs`, `ClassManager.cs` ！ function/class registries

- `source/IR` ！ intermediate representation and helpers
  - `IRData.cs`, `IROpEnum.cs`, `IR*Statements` ！ IR representation and helpers
  - `IR/Lib/*` ！ runtime helpers used by stdlib

- `source/VM` ！ runtime value types and VM layers
  - `Object/*` ！ `SObject` and runtime wrappers for primitives
  - `InnerCLRRuntime/*` ！ bridge to .NET CLR, runtime type/registration
  - `NewObject/*`, `LocalRuntime/*` ！ object allocation and local VM

- `source/Lib` ！ standard library written in the language (core primitives, collections)
  - `Lib/Core/*` ！ `Object.sl`, `Array.sl`, `List.sl`, `Map.sl`, `Num.sl`, etc.

- `source/Export` ！ code generation and AOT hooks (LLVM, AOT metadata)

- `source/OtherLanguage/CSharp` ！ integration tools to map language constructs to C# metadata

Key observations
- The repo implements an end-to-end toolchain: lexer -> file-meta -> meta model -> expression nodes -> IR -> VM/runtime.
- Standard library is implemented in-project `source/Lib/Core` and calls into `IR/Lib` and `VM` layers.
- Abstract/override mechanics are implemented in meta layers (lexer produces `abstract` token; `MetaMemberFunction` supports `m_IsAbstract` and skipping of function bodies).

If you need a per-module file list or a map for a subdirectory, request the directory name and I will produce a focused listing.
