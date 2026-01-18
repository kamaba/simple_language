# SimpleLanguage �� Product Requirements Document (PRD)

## 1. Overview
SimpleLanguage is a small language toolchain providing parsing, compiling, metadata and a VM runtime. It is designed with an approachable syntax inspired by Dart and C#, and supports class, data, array, generics, templates, and IR generation for export/AOT. The repository includes a standard library written in the language itself and runtime helpers in C#.

Goals:
- Provide a compact language suited for scripting and embedding.
- Offer a simple compilation pipeline with AST, meta types, IR, and VM execution.
- Support language extensibility: templates/generics, foreign language interop (C#), and code generation (IR/LLVM/AOT).

Audience: Developers and language researchers wanting an embeddable scripting language with compile-time metaprogramming and runtime VM.

## 2. Key Features
- Lexer/Parser that converts source text to tokens and syntax nodes.
- FileMeta layer to map tokens into file-level syntactic constructs.
- Meta model (MetaClass, MetaMemberFunction, MetaVariable) to describe types and functions.
- ExpressManager: expression creation, type inference and simple optimization.
- IR generation with `IRData` and IR-level ops; runtime `SObject` family for value representation.
- Standard library (`source/Lib/Core`) with collections, numeric primitives, and base `Object` interfaces.
- Integration with C# via `OtherLanguage/CSharp` modules and `IR/Lib` runtime helpers.

## 3. Architecture
- Compiler Frontend
  - Lexer: `source/Compile/Parse/LexerParse.cs`
  - Token parser: `source/Compile/Parse/TokenParse.cs`
  - File-level parsing: `source/Compile/FileMeta/*`
- Meta Model
  - Core meta classes: `source/Core/*` (MetaClass, MetaMemberFunction, MetaVariable)
  - Type management: `source/Core/MetaType.cs`, `TypeManager.cs`
- Express & Semantic
  - ExpressManager: create typed expression nodes. `source/Core/ExpressManager.cs`
  - Expression nodes: `source/Core/MetaExpressNode/*`
- IR & VM
  - IR classes: `source/IR/*` (IRData, IRCall, IRConvert)
  - VM value types: `source/VM/Object/*` (SObject and derived types)
  - InnerCLR runtime bridge and local runtime
- Libraries & Tools
  - Standard library: `source/Lib/Core` (written in language itself)
  - C# runtime bridges: `source/IR/Lib/*`
  - Exporting/AOT/LLVM: `source/Export/*`

## 4. Modules & Responsibilities
- `Compile` - parsing and tokenization, file meta building, project compile states.
- `Core` - meta model, semantic checks, expression tree creation, code generation helpers.
- `IR` - intermediate representation and helpers for lowering meta/expressions to executable IR.
- `VM` - runtime SObject, object managers, local runtime and InnerCLR runtime.
- `Lib` - standard library in language source.
- `OtherLanguage/CSharp` - utilities to integrate with external C# modules.

## 5. Development Setup
Prereqs: .NET 6 SDK
Build: `dotnet build SimpleLanguage.csproj`
Run: `dotnet run --project SimpleLanguage.csproj` (entry points and sample script runners available)

## 6. Language Highlights
- Class and data declarations, with `abstract`, `override` semantics.
- Generic templates and meta-template instantiation.
- Collection primitives (Array, List, Map) in `source/Lib/Core`.
- IR export and AOT support for generating platform code.

## 7. Known Limitations & Next Steps
- `Map` currently implemented as a simple list-backed store �� needs hash-bucket optimization.
- `Object.CloneObject` and `ObjectWeakRef` are placeholders; deep/shallow clone semantics need to be defined.
- Standard library `Num` methods need to be implemented for Int/Float types.
- Improve error handling: escalate some semantic checks (e.g., missing override) to hard compilation errors.

## 8. Roadmap
- Q1: Complete standard library implementations for core numeric types and collections.
- Q2: Implement Map hash buckets and more collection utilities (map/filter/reduce).
- Q3: Add optimization passes for IR and better VM integration.
- Q4: Provide language docs, samples and CLI tools for packaging and AOT.

## 9. File Map (selected)
- `source/Compile/Parse/LexerParse.cs` �� lexer
- `source/Compile/Parse/TokenParse.cs` �� token -> node
- `source/Compile/FileMeta/*` �� file-level parsing
- `source/Core/*` �� meta model and expression management
- `source/IR/*` �� intermediate representation and runtime library
- `source/VM/*` �� runtime and SObject value types
- `source/Lib/Core/*` �� standard library in language source
- `source/OtherLanguage/CSharp/*`  utilities to integrate with external C# modules
- `source/Export/*`  exporting/AOT/LLVM
- `source/SimpleLanguage.csproj`  project file
- `source/README.md`  this file
- `source/LICENSE`  license
- `source/md/*`  documentation
- `source/md/ai/*`  ai guide documents
- `source/md/ProgramSyntax/*`  design the syntax of a programming language 
- `source/obj/*`  intermediate build output
- `source/bin/*`  build output
- `source/tests/*`  unit tests
- `source/samples/*`  sample scripts
---
Generated by dev-assistant for repository snapshot. For more detail, explore `source/` top-level directories.
