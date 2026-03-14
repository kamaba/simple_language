# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Attach FileMeta attribute metadata to the Meta layer first, then enforce via checks at runtime/execute-before/new-class/member access/call sites.

## Code Style
- Use specific formatting rules
- Follow naming conventions

## Project-Specific Rules
- VM ????????????????§µ?????????? Front ???/??????
- ????? Assembly/Module/IR ???????????/?????×Ï??????? JSON/?????????????????? VM ?? ProjectReference ?? Front??
- Export custom bytecode/IR container (SLIR) from Front IR, including class metadata (member vars/functions/relations) plus reader and dump tooling; export should be opt-in via env vars and not add VM->Front dependencies.
- NativeBridge design: Front parses bridge calls into intrinsic opcodes (CallCLRMethod/CallNativeMethod/CallJVMMethod); bridge metadata is pre-registered during SLIR load; runtime call should support using instruction index as registry index, resolve/cache MethodInfo from registry data, then invoke and map return value back into VM value flow.
- In MetaCallNode.GetFirstNode, classify CallCLRMethod/CallNativeMethod/CallJVMMethod as SystemFunctionCall; MetaCallLink should create MetaVisitNode with EVisitType.SystemCall and bind MetaMethodCall; IR conversion should continue through IRCallFunction into bridge opcodes.