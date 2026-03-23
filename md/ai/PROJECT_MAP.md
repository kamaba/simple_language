# Simple Language — 项目地图与索引

> 供 AI 与人类快速定位代码与文档。生成时仓库结构以 `source/` 下 **Front / VM / Log / CLangdll** 为主；忽略 `bin/`、`obj/` 等构建产物。

## 1. 解决方案与工程

| 工程 | 路径 | 说明 |
|------|------|------|
| SimpleLanguageFront | `source/Front/SimpleLanguageFront.csproj` | 编译器前端：词法/语法、语义、IR、导出（含 SLIR、C#/Java/AOT、LLVM 相关） |
| SimpleLanuageVM | `source/VM/SimpleLanuageVM.csproj` | 运行时：加载 SLIR JSON、解析模块、CLR VM、本地运行时、Native 桥 |
| SimpleLanguageLog | `source/Log/SimpleLanguageLog.csproj` | 日志与诊断，被 Front/VM 引用 |
| CLangdll | `source/CLangdll/CLangdll.vcxproj` | C++ 原生 DLL（与 Clang/FFI 等集成） |

入口程序：

- 编译器：`source/Front/Program.cs`（`SimpleLanguage.Program`）
- VM：`source/VM/Program.cs`（读取 `*.package.json` 等，经 `SLIRJsonModuleLoader` → `SLRuntimeModuleRegistry` → `CLRVM`）

## 2. 端到端数据流（简图）

```text
.sl 源码
  → LexerParse / TokenParse / FileParse（Compile/Parse）
  → FileMeta*（Compile/FileMeta）
  → Core（Meta*、Statements、ExpressManager）
  → IR（IR*、IRStatements）
  → Export（SLIRWriter / SLModulePackageWriter 等）→ JSON / package
  → VM：Load（SLIRAssemblyData、SLIRJsonModuleLoader）→ Parse（SLIRModuleParse、SLRuntimeModuleRegistry）
  → InnerCLRRuntime / LocalRuntime / Object / NativeBridge
```

## 3. `source/Front` 子目录职责

| 目录 | 职责 |
|------|------|
| `Compile/Parse` | 词法、节点、文件级解析（如 `LexerParse.cs`、`FileParse.cs`、`StructParseToSyntax.cs`） |
| `Compile/FileMeta` | 文件级 AST/元结构（类、成员、命名空间、表达式片段等） |
| `Compile/Process` | 编译流程与状态（如 `ProcessController.cs`、`ProjectCompileState.cs`） |
| `Core` | 语言语义模型：`MetaClass`、`MetaMethod`、`ModuleManager`、`ExpressManager`、`Statements/*`、`MetaExpressNode/*`、`BaseMetaClass/*` |
| `IR` | 中间表示：`IRData`、`IRManager`、`IRStatements/*`、`IR/Core/*`、`IR/Lib/*` |
| `Export/SLIR` | SLIR 读写与打包（如 `SLIRWriter`、`SLIRReader`、`SLModulePackageWriter`、`SLIRTypes`） |
| `Export/CSharp`、`Export/Java`、`Export/AOT`、`Export/MLIR`、`Export/Local` | 各目标或实验性后端 |
| `External/Native` | 原生库加载、FFI manifest（如 `NativeBindingManager`、`NativeExportManifestReader`） |
| `OtherLanguage/CSharp` | 与 C# 互操作/IR 侧集成 |
| `Project` | 工程与配置（如 `ProjectConfig.cs`） |
| `Wrapper` | CLR 包装表达式/调用（`Wrapper*`） |
| `Lib` | 标准库源码（`.sl`）：`Lib/Core`、`Lib/Std`、`Lib/Render` 等 |

## 4. `source/VM` 子目录职责

| 目录 | 职责 |
|------|------|
| `Load` | SLIR 装配数据与 JSON 加载（`SLIRAssemblyData`、`SLIRJsonModuleLoader`） |
| `Parse` | 模块包解析、运行时注册（`SLIRModuleParse`、`SLModulePackage`、`SLRuntimeModuleRegistry`、`SLIRAssemly`） |
| `InnerCLRRuntime` | 指令、SValue、与 CLR 交互（`Instruction`、`CLRRRuntimeVM`、`RuntimeCall` 等） |
| `Object` | 运行时对象模型（`SObject` 族） |
| `LocalRuntime` | 本地 VM 与内存 |
| `NewObject` | 对象分配头/策略 |
| `NativeBridge` | 动态库与 C#/Java 等桥接 |
| `Runtime` | 运行时类型与 VM 门面（如 `CLRVM`、`EVMType`） |
| `OtherLanuage/CSharp` | VM 侧 C# 相关指令/调用 |
| `Lib` | VM 侧辅助（如 `Lib/Core` 与导出 JSON） |

## 5. `source/Log`

诊断、错误码、日志接口（`Log.cs`、`Diagnostic.cs`、`ErrorDefinition.cs`、`ILogger.cs`）。

## 6. 仓库其他重要位置

| 路径 | 说明 |
|------|------|
| `test/` | 语言样例与回归脚本（`BaseTest/`、`ExpendTest/`、`TestScript/`），扩展名含 `.sl`、`.s`、`.sp` 等 |
| `md/ProgramSyntax/` | 语法与特性说明文档 |
| `md/ai/` | AI/协作说明：`AI_GUIDE.md`、`代码解析流程.md`、`CODEBASE_OVERVIEW.md`、本文 `PROJECT_MAP.md` |
| `tools/ExtractLogs` | 小工具工程 |
| `SimpleLanguage.sln` | VS 解决方案 |

## 7. 按任务快速定位（书签）

| 任务 | 优先查看 |
|------|-----------|
| 改词法/Token | `Compile/Parse/LexerParse.cs` |
| 改语法树/FileMeta | `Compile/FileMeta/*`、`Compile/Parse/FileParse.cs` |
| 改语义/类型/表达式 | `Core/*`、`MetaExpressNode/*` |
| 改 IR 或 lowering | `IR/*` |
| 改 SLIR 序列化/包格式 | `Export/SLIR/*`、`VM/Load/*`、`VM/Parse/*` |
| 改 VM 执行或指令 | `InnerCLRRuntime/*`、`Runtime/CLRVM`（按实际类型名搜索） |
| Native / FFI | `Front/External/Native/*`、`VM/NativeBridge/*`、`*.slffi.json` |

## 8. 相关现有文档

- 解析阶段说明：`md/ai/代码解析流程.md`
- 旧版总览（路径已在新版 `CODEBASE_OVERVIEW.md` 中纠正）：与本文互补，细节以本文为准

---

*若目录更名或大规模重构，请同步更新本文件。*
