# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Attach FileMeta attribute metadata to the Meta layer first, then enforce via checks at runtime/execute-before/new-class/member access/call sites.
- Use CSV-driven log definitions with id-based enum mapping and route Debug.Assert/Debug.Write style diagnostics through the centralized Log system.
- Treat `simple_language/source/VM` as the source of truth before logic changes and always compare against it.
- Maintain ongoing mapping/relationship documentation between the csimple_lang and SimpleLanguageVM projects.
- **Sibling C VM checkout (reference path):** `F:/project/lang/csimple_lang` — ANSI C99 VM; wired into `SimpleLanguage.sln` as `../csimple_lang/csimple_lang.vcxproj` (keep repos as siblings under the same parent folder).
- Migrate VM source loading logic now; if the JSON library is missing, add a high-performance cJSON-style library under `csimple_lang/lib`, keeping the directory layout closely mirroring SimpleLanguageVM.
- When implementing code in csimple_lang, prefer and optimize usage of existing methods under `src/base` whenever possible before adding alternative utility logic.
- When optimizing string-related functionality in csimple_lang, prioritize reusing existing capabilities in `src/base` (especially `chars.h`/`chars.c`); if missing, supplement with equivalent functions in the current module and replace direct standard library calls (e.g., `strstr`).
- For common utilities and shared type definitions, first search and reuse implementations in `src/define.h` and `src/base` before creating new ones in business/runtime modules.
- If a common helper is missing, add it to the appropriate `src/base` module using existing `base_*` naming conventions, then update call sites to use that base helper.
- When adding or refactoring csimple_lang code, modularize VMValue-related capabilities into separate `svalue.h/svalue.c` files (referencing the SValue organization in SimpleLanguageVM) to avoid duplicate implementations in `vm_runtime.c/runtime_object.c`.
- Parse `defineMetaType` before parsing statements; `realMetaType` should be resolved later when the function returns or runtime flow requires it.
- Data should be treated like a struct-like data container with no functions. Preserve support for primitive constants, arrays (containing constants/arrays/objects/anonymous objects), anonymous nested data, class instances, data instances, enum values, data equality by first comparing structure and then `m_MemberDataBuffer` contents, and data printing in data-format with values.
- Data documentation and tests should explicitly cover const constraints, anonymous data const members, reassignment after new(), static data reassignment, chain member reads, and struct-like data semantics.
- Implement const constraints in the simple_language Front layer: const is effective only at compile time; adding const before ordinary statements makes them non-assignable thereafter; const can be used for variables, member variables, and members under class/data/enum; when const is added to data, all sub-nodes become const by default; enums are const by default, and modification is only allowed with `mut`.
- For anonymous data members, first build the anonymous MetaData type, then represent the value as a MetaNewObjectExpressNode that performs a new of that anonymous data type and applies child assignments via MetaBraceAssignStatements; preserve useful prior code only if it still contributes to this flow.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- In this repo, LID enum members should use meaningful English names derived from CSV message content, not numeric-style names like Id10000.
- Implement csimple_lang translation in ANSI C style and implementation.
- Keep `csimple_lang/src/vm` structure closely aligned with SimpleLanguageVM structure, including splitting VM concepts like RuntimeClass and RuntimeObject into independent files (not embedded in vm_runtime). 
- `src/base` should serve as the C standard/global foundation.

## Project-Specific Rules
- VM execution order: after loading modules, first initialize `globalStaticVariableList`, then execute its initialization expressions, and finally execute main.
- Users have decided to cancel the global processing flow during the VM startup phase, shifting this responsibility to the Project{} entry point itself.
- Users have decided to remove `global{}` parsing at the Front layer, retaining only `local{}`; `FileMetaGlobalOrLocalSyntax` must be changed to `FileMetaLocalSyntax` and all global-related logic removed.
- Users require the VM side to no longer process `globalInitInstructionList`, using only the newly exported `globalStaticInstructionList` from the Front.
- Users require the Front layer to continue supporting `global.xx` / `global.func()` calls, but the semantic source must now read from the contents of `Project{}` instead of `global{}`.
- VM member state should be sourced from `m_MemberDataBuffer`, with reference slots treated as object pointers; `m_SObject` should be DEBUG-only as a debugging mirror, not the primary runtime source of truth.
- Export custom bytecode/IR container (SLIR) from Front IR, including class metadata (member vars/functions/relations) plus reader and dump tooling; export should be opt-in via env vars and not add VM->Front dependencies.
- NativeBridge design: Front parses bridge calls into intrinsic opcodes (CallCLRMethod/CallNativeMethod/CallJVMMethod); bridge metadata is pre-registered during SLIR load; runtime call should support using instruction index as registry index, resolve/cache MethodInfo from registry data, then invoke and map return value back into VM value flow.
- In MetaCallNode.GetFirstNode, classify CallCLRMethod/CallNativeMethod/CallJVMMethod as SystemFunctionCall; MetaCallLink should create MetaVisitNode with EVisitType.SystemCall and bind MetaMethodCall; IR conversion should continue through IRCallFunction into bridge opcodes.
- Debug export semantics must distinguish layers: `File.txt` exports FileMeta layer data, while `Meta.txt` exports MetaCore layer data, and must include complete logic (e.g., method statements) rather than just an incomplete summary of core nodes.
- Export `IR.txt`, aggregating all IR methods under each class for unified export.
- `SLIRJsonModuleLoader` is the JSON SLIR reader, and `SLIRBinModuleLoader` is the binary SLIR reader.
- When debugging parse or compile pipeline issues, first check debug outputs under `source/Front/bin/Debug/net8.0/DebugCode` and follow the strict order: `IR.txt` -> `Meta.txt` -> `File.txt` -> `Node.txt` -> `Token.txt` -> `Code.txt`; if a layer is wrong, trace to the previous upstream layer immediately.
- Required troubleshooting chain: start from `IR.txt`, then verify `Meta.txt`, then `File.txt`, then `Node.txt`, then `Token.txt`, and finally `Code.txt`.
- If one layer is incorrect, immediately trace to the previous upstream layer to find where the incorrect output was introduced.
- In `MetaMemberEnum.CreateValuesArrayElementExpress`, do not use `MetaNewObjectExpressNode`; instead, use an expression similar to `MetaCallLink` to directly read enum member variables.

## Runtime/System Method Organization
- Organize runtime/system method implementations by domain:
  - Language built-in class logic in `src/core` (e.g., String/Int32/Int64/Object)
  - IO-related functions in `src/lib/io`
  - OS-related functions in `src/lib/os`
  - Time-related functions in `src/lib/time`
- Document this organization in Markdown files.

## Logging System (High Priority)
- Follow `.github/ai-log-system-guide.md` as mandatory guidance when adding or modifying logs.
- For every new log entry, always update the corresponding project's `ErrorDefinitions.csv` and `LID.cs` together with code call sites.
- Prefer typed log API.
