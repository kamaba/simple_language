# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Attach FileMeta attribute metadata to the Meta layer first, then enforce via checks at runtime/execute-before/new-class/member access/call sites.
- Use CSV-driven log definitions with id-based enum mapping and route Debug.Assert/Debug.Write style diagnostics through the centralized Log system.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- In this repo, LID enum members should use meaningful English names derived from CSV message content, not numeric-style names like Id10000.

## Project-Specific Rules
- VM execution order: after loading modules, first initialize `globalStaticVariableList`, then execute its initialization expressions, and finally execute main.
- Users have decided to cancel the global processing flow during the VM startup phase, shifting this responsibility to the Project{} entry point itself.
- Users have decided to remove `global{}` parsing at the Front layer, retaining only `local{}`; `FileMetaGlobalOrLocalSyntax` must be changed to `FileMetaLocalSyntax` and all global-related logic removed.
- Users require the VM side to no longer process `globalInitInstructionList`, using only the newly exported `globalStaticInstructionList` from the Front.
- Users require the Front layer to continue supporting `global.xx` / `global.func()` calls, but the semantic source must now read from the contents of `Project{}` instead of `global{}`.
- VM ???????????????????????????? Front ???/??????
- ????? Assembly/Module/IR ???????????/?????????????? JSON/?????????????????? VM ?? ProjectReference ?? Front??
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

## Logging System (High Priority)
- Follow `.github/ai-log-system-guide.md` as mandatory guidance when adding or modifying logs.
- For every new log entry, always update the corresponding project's `ErrorDefinitions.csv` and `LID.cs` together with code call sites.
- Prefer typed log APIs under `SimpleLanguage.Logging.Log` (`AddProjectLog`/`AddMetaCoreLog`/`AddRuntimeLog` etc.); do not add direct `Debug.Write*` / `Console.WriteLine` as business log outputs.
