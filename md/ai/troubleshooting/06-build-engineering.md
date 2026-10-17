# 复盘分片 06 · 构建工程链路

> 收录：vcxproj/CMake 漏登记、LNK2019/LNK2001、双树覆盖、CWD/路径、-e ir 假成功、Linux/WSL、wasm 构建、测试宿主工程类复盘。
> 路由索引与追加规则见 [故障排查流程 §6](../故障排查流程.md)；新复盘按域追加至本片末尾。

### [2026-09-12] P2 lib 负例变体 26 项脚本仅 25 过：L4b 正则替换串重发原闭合引号致 JSON 非法
- 现象：`test-platform-lib.ps1` 首跑 25/26——L4b「lib cmp 改 ge 999.0 阻断运行」exit 1 判定 PASS，但诊断行断言 `lib        : sqlite3` FAIL（同格式断言的 L4a exists 负例却 PASS）。
- 首个异常阶段：测试脚本自身（PowerShell `-replace` 替换串），非产品代码。
- 根因：替换串 `'$1ge$3$4", "value": "999.0"$5'` 中 `$4`（`sqlite3`）后手写的 `"` 与 `$5`（原文 `"key": "sqlite3"` 的**原闭合引号**）叠加，产物为 `"key": "sqlite3", "value": "999.0""`——双引号 JSON 非法。exit 1 实为 **JSON 解析失败**，ge 判定从未执行；「exit 1 + 诊断行缺失」的组合被误读为「判定成功拦截但诊断格式不同」。教训：**exit code 相同不代表失败原因相同**。
- 修复点（文件/模块）：`csimple_lang/scripts/test-platform-lib.ps1` L4b 替换串去掉末尾 `$5`（我的闭合引号顶替原引号），并加注释说明。
- 如何快速复现：对 module.json 手动重放原正则改写后，打印改写产物中 sqlite3 atom 片段即可见 `999.0""` 双引号。
- 如何快速验证：`test-platform-lib.ps1` 26/26 PASS（L4b exit 1 + `✗ lib : sqlite3 >= 999.0` 诊断齐）；回归 `test-platform-override.ps1` 25/25 PASS。
- 预防建议：①**负例变体改写 module.json 后，必须先验证产物 JSON 仍合法**（如 PowerShell `ConvertFrom-Json` 试解析）再喂给 exe，否则「exit 1」可能来自解析失败而非预期判定，出现假 PASS；②断言失败时先 dump 实际输出/产物再改断言，勿先猜格式；③正则分组替换串里，凡 `$N` 匹配的是**定界符**（引号/括号），替换串中自行重写定界符的同时必须显式丢弃 `$N`，二者只留其一。

### [2026-09-12] P2.5 AOT 一致性校验三坑：require/features 名字归一化不匹配误报 + compile 默认不跑 Export（须 -e ir）+ link 依赖 LIB 环境变量
- 现象：① P2.5 一致性校验（Error 20037）首验时，require 写 `"sse4.2"`、features 写 `"+sse42"`（同一特性两种写法）被误判矛盾——形式不一致即误报；② 正例工程 `compile -p <sp>` 编译"成功"但无 aot.mlir/aot.dll/module.json，Front.txt 也无 22115（llc target options）日志；③ stage-3 最后 link 步骤报 `LNK1181: 无法打开输入文件"libvcruntime.lib"`。
- 首个异常阶段：① MetaCore（ValidateAotTargetConsistency 比对逻辑）；② ProcessManager 相位调度（Export [Pending]）；③ 工具链（link.exe 环境依赖）。
- 根因：① 归一化（PlatformDefTables.TryNormalize → `sse42`）只发生在 require 侧（`ValidatePlatformRequireValues` 前置归一），features 侧原样入集比对；且 require 的 cpu `eq`/`ne` atom 的 Value 连前置归一都没有——同一名两侧形异必误报；② Front `compile` 动词默认只跑到 IR（RunToPhase），仅 `-e ir` 时 `CommandExecutor.ExecuteCompile` 才 RunPhase(Export)（同既有 [2026-09-12] Component 条目经验①）；③ MSVC link.exe 需要 `LIB` 环境变量（MSVC lib\x64 + Windows SDK ucrt\x64），普通终端无此变量。
- 修复点（文件/模块）：`source/Front/Project/ProjectJsoncLoader.cs`——ParseAotFeatures 解析时对每项过 `PlatformDefTables.TryNormalize(KindIsa,...)` 后入启用集；新增 `IsAotFeatureEnabled(name, enabled)` 辅助（查询时归一 require 侧名字再查集），并接入 `ValidateAotCpuAtom` 全部四个 case（all/eq/any/ne）。**注意 features 串传 llc 仍原样**（LLVM 特性名语法就是 `sse4.2`），归一化只用于比对侧。
- 如何快速复现：① 正例 `dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\SpecialTest\PlatformAotPosTest\PlatformAotPosTest.sp -e ir`（去掉修复会误报 20037）；② 同命令去掉 `-e ir` 即复现 Export [Pending] 无产物；③ 无 `$env:LIB` 环境下跑 aot buildDll 即复现 LNK1181。
- 如何快速验证：正例带 `-e ir` + `$env:LIB = "C:\Program Files\Microsoft Visual Studio\18\Community\VC\Tools\MSVC\14.51.36231\lib\x64;C:\Program Files (x86)\Windows Kits\10\Lib\10.0.26100.0\ucrt\x64"`：Front.txt 依次出现 `AOT: 1 candidate(s) collected` → `AOT: export aot.mlir success` → `[Info] [Project] [ExportMLIRAOTTarget] … -mattr=+sse4.2,+avx2` → `AOT: build dll success` → `export module success`，全程无 20037（别名 `sse4.2` ↔ `+sse4.2` 归一匹配）；负例 PlatformAotNegTest（require cpu all(avx2) vs features +sse42）报 Error 20037 并中止导出；回归 PlatformOverrideNegTest（20034/20036 通道）与 BaseTest 全量均零回归。
- 预防建议：①两侧名字比对（require 值 vs aot features）一律先过 PlatformDefTables 归一，勿假设两侧书写形式一致；②验证导出类功能必带 `-e ir`，并核对 ProcessSummary 的 Export/ExportModule 非 [Pending]；③llc/link 真实构建前先备好 LIB 环境变量（或从 VS 开发者终端跑）；④别名（`sse4.2`/`sse4_2`/`avx512f`）在设计文档与测试 fixture 中混用是有意的，勿"顺手统一"——这正是归一化机制的验证场景。

### [2026-09-15] SystemComponentGetComponent not registered：MSVC 树 vcxproj 漏登记新源文件，缺符号 DLL 覆盖 Ninja 树（§7 双树覆盖坑实锤）
- 现象：CSimpleVMStdTest 跑到 ProjectTest 的 `==== testBasicQuery ====`，`__new_object__ProjectTest.CompSpeed` 构造完成后报 `[Assert] [LID=10005] CallSystemMethod not registered. method=SystemComponentGetComponent`，Component 系统方法全军覆没。
- 首个异常阶段：VM 运行期——动态系统方法注册链（SLIR `module.json` 的 systemCalls → `vm_system_func_dynamic_register` → GetProcAddress 解析 `cvmFunction` 符号）失败后**静默不注册**，运行期 OpCode_CallSystemMethod 查 id 表/名字表均未命中才报 LID=10005。
- 根因（三层叠加）：
  1. `csimple_lang/project/vs/vm_lib/csimple_lang_lib.vcxproj` ClCompile 清单漏登记 7 个新文件（`component_system_method.c`、`json_system_method.c`、`file_system_method.c`、`sl_vm_ext_api_check.c`、`vm/parse/` 下 3 个）——R6 登记 CMakeLists 时 vcxproj 漏同步；
  2. MSVC 树 OutDir 与 Ninja 树同为 `build\Debug\bin`（AGENTS.md §7 双树覆盖坑）：MSVC link 出的缺符号 DLL 覆盖后，ninja 按 mtime 判定 DLL 比 .obj 新而**跳过重链接**，P/Invoke 加载的正是坏 DLL；
  3. 缺 7 文件为何链接不报错：这些文件零静态调用方（parse 层为孤儿代码、component/json/file 系统方法仅运行时经 GetProcAddress 动态解析）→ 产出「能跑但缺新符号」的隐蔽坏 DLL。
- 修复点（文件/模块）：vcxproj 补齐 7 个 .c + 7 个 .h、移除 2 个废弃 `vm_system_registry` 条目（该文件已是 217 字节纯注释占位，内容已拆分至 system_method_call/），两清单与 CMakeLists 达成严格一致；DeleteFile 删除 `build\Debug\bin` 陈旧 DLL/EXE 强制 ninja 重链接。
- 如何快速复现：MSVC 构建一次 vm_lib 后，跑任何用到 Component/Json/File 系统方法的用例即报 LID=10005。
- 如何快速取证：`llvm-readobj --coff-exports csimple_lang_dll.dll` 比对导出表——修复前 543 导出、`vm_sys_component_*` 零导出；修复后 570（+27 = component 11 + json 7 + file 9），`vm_sys_component_get_component` 在列。
- 如何快速验证：重跑 CSimpleVMStdTest，testBasicQuery 段 getComponent/getComponents 全 PASS，ComponentTest 整段（含层级查询/inactive gate/消息广播/stress 20000 命中）跑完不报 not registered。
- 预防建议：R6 的「新增 .c 必须登记」实为**两处同步**（CMakeLists `CSIMPLE_LIB_SOURCES` + vcxproj ClCompile）；怀疑 DLL 内容不对时先查导出表取证再动代码（R12）；双树交替构建后跑用例前，抽查关键新符号是否在导出表内。

### [2026-09-16] 内联 lambda 负例工程宿主 exit 1 被 VS 显示为报错：拦截正确但负例无「预期失败=成功」通道
- 现象：`InlineLambdaForbiddenTest` 6 条拦截全部正确命中（21449×3/21452×2/21451×1），但 CSimpleVMTest 宿主打印 `Front compile failed, error count: 8` 且 exit 1，VS 将正常工作的负例显示为编译报错。
- 根因：`Program.cs` 中 `RunFrontInProcess` 以 `Log.errorCount > 0` 判失败返回 1，`Main` 失败直通 `return frontExit`——负例工程天然以非零码退出。`errorCount=8` 是 6 拦截 + 1 连锁（`MetaCoreExpressOperatorLeft`，g1 拦截后调用链解析失败的连锁）+ 1 阶段失败日志（`ProcessPhaseFailed`）。
- 修复点（`Program.cs`）：新增 `NegativeFrontCase` 注册表（启动项名 → 导出模块名 → 预期 LID 计数），Front 失败后读 `Log.FrontLogFilePath`（in-process 准确，回退 `out/export/<模块>/Logs/Front.txt`）按 `"[LID]"` 行统计命中；全命中 → 打印通过信息、跳过 C VM、exit 0；缺命中 → 真失败。只校验预期 LID，不计连锁错误。
- 预防建议：负例工程宿主化时必须配「预期失败校验」通道，否则每次跑负例 VS 都报错，会淹没真回归；校验用 LID 名称精确匹配而非 errorCount（阶段失败/连锁日志也计入）。

### [2026-09-17] VS 调试下 ProcessTest testExitCodeNotExited 卡住（命令行全过）：预期业务异常被分派层打 Assert 级日志，触发 __debugbreak 冻结
- 现象：VS 里跑 CSimpeVMSpecialTest，ProcessTest 到 `testExitCodeNotExited` 输出标题后不再前进；同一测试命令行下 33/33 全过。
- 首个异常阶段：VM（vm_runtime.c OpCode_CallSystemMethod 分派层）。
- 根因（链）：
  1. `p.exitCode` 在进程未退出时按设计走 SL throws 流程：`vm_sys_throw(-83)` 设 error_code + Error 级日志 + `vm_execute_throw` 分派异常，然后 `return FALSE`——这是系统方法异常机制的正常路径；
  2. 主分派（vm_runtime.c 两处 `CallSystemMethod failed`）把任何 FALSE 一律打 **Assert 级**（LID=10005）日志；
  3. Debug 构建下 log.c 对 Assert 级日志在 `IsDebuggerPresent()` 为真时执行 `__debugbreak()` → VS 捕获断点中断并冻结全部线程（取证：卡住进程 28 线程中 25 个 Suspended、CPU 时间仅 3 秒级非忙转；VM.txt 停在 Assert 行）；命令行无调试器 → 跳过 debugbreak → 全过。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/vm_runtime.c` OpCode_CallSystemMethod 两处失败日志——仅当 `vm->error_code == 0`（即 FALSE 属于静默意外失败）才打 Assert 级；error_code 非 0 说明异常已由 vm_sys_throw 上报（Error 日志已打、异常值已分派），不再重复 Assert。
- 如何快速复现：修复前在 VS 调试运行 SpecialTest（或任何触发 throws 系统方法的用例，如 exitCode/kill/terminate/SpawnFailed），VS 中断在 `__debugbreak`；VM.txt 可见 `CallSystemMethod failed` Assert 行紧随业务 Error 行。
- 如何快速验证：命令行 SpecialTest 回归全过（ProcessTest 33/33、exit=0），且 VM.txt 中预期异常路径只剩 `[Error] Process.exitCode: not exited yet` 等 Error 行、不再出现 `CallSystemMethod failed` Assert 行。
- 预防建议：①系统方法返回 FALSE 具双重语义（预期 throws vs 意外失败），日志级别须按 `vm->error_code` 区分——已抛异常的 FALSE 不是断言失败；②「命令行过、VS 卡」的差异化第一嫌疑即 Assert 级日志 + `IsDebuggerPresent()` 的 `__debugbreak()`（log.c Debug 专属逻辑）；③卡住进程取证看线程 WaitReason：大量 Suspended + 低 CPU = 调试器冻结，非死锁忙转；④卡住的调试会话会锁定 exe/dll 导致后续构建 MSB3027，先终止冻结的被调试进程（勿杀 VS 本身）。

### [2026-09-18] CoreTest 回归 MSVC 树 LNK2019×20：新 C 源文件与 vendored zlib 只登记 CMakeLists 未同步 vcxproj（双树登记遗漏复发 + zlib 首次引入即漏）
- 现象：修复 IRMetaVariable 枚举返回豁免后跑 `dotnet run --project project\CSimpleVMCoreTest` 回归，失败于 MSVC 树 `csimple_lang_lib.vcxproj` 链接：LNK2019 ×20 + LNK1120——`sl_core_gzip.obj`/`sl_core_zlib.obj` 缺 deflate/deflateEnd/inflate/inflateEnd/deflateBound/deflateInit2_/inflateInit2_/compress2/compressBound/uncompress（zlib 系 10 符号），`byte_buffer_system_method.obj` 缺 core_base64_*/core_encoding_*（新 core 文件系 10 符号）。
- 首个异常阶段：MSVC 链接期（与本次 C# Front 修复无关——Ninja 树 CSimpleVMStdTest 全绿是佐证；CoreTest 走 vcxproj，StdTest 走 Ninja DLL）。
- 根因（链）：任务一/二的 C 侧改动只登记了 CMakeLists（Ninja 树），未同步 MSVC 树 vcxproj：①新文件 `sl_core_base64.c`/`sl_core_encoding.c`/`sl_core_gb2312_table.c` 未登记（与 [2026-09-15] SystemComponentGetComponent 同型复发）；②CMakeLists 引入 vendored zlib 源码（`third_party/zlib` 10 个 .c，注释明确 gz* 文件 API 不编）时 vcxproj 未对齐——`sl_core_gzip.c`/`sl_core_zlib.c` 既有文件靠相对路径 `../../third_party/zlib/zlib.h` 编译通过，但实现体未链接，从此 MSVC 树必然 LNK2019。
- 修复点（文件/模块）：`csimple_lang/project/vs/vm_lib/csimple_lang_lib.vcxproj` 三处登记——ClCompile 加 3 个新 core .c + third_party/zlib 10 个 .c（与 CMakeLists 相同清单，不含 gzclose/gzlib/gzread/gzwrite/infback）；ClInclude 加 3 个新 core .h。无需补 include 目录（zlib 头为相对路径引入，zlib 内部同目录引号 include MSVC 可解析）。
- 如何快速复现：新增/引入 C 源文件只登记 CMakeLists 后，跑 `dotnet run --project project\CSimpleVMCoreTest`（该宿主每次构建 vcxproj，LNK2019 立现）。
- 如何快速验证：重跑 CoreTest——构建日志出现 csimple_lang_lib.vcxproj 且零 LNK 错误（仅既有无害警告 D9002：MSVC 忽略 `-std=c99`），exit 0，ProjectTest _main_ 全绿（CoroutineTest/StringBuilderTest OK）。
- 预防建议：R6 需按双树理解——新增/引入 C 源文件（含 third_party vendored）要**两处同步登记**：`CMakeLists.txt` CSIMPLE_LIB_SOURCES + `project/vs/vm_lib/csimple_lang_lib.vcxproj`；引入第三方库时先看 vcxproj 侧既有登记模式（sqlite3=lib 链接、yyjson/lz4=源码直编 ClCompile），按同型登记；跑 CoreTest 前自检「本轮新增了哪些 .c/引入了哪些库符号」。

### [2026-09-21] Std/Math 标准库自 8 月起编译失败——references Strategy 2 相对 outputDir 按进程 CWD 解析，预编译模块引用链断裂
- 现象：CompileStd 在 ParseMetaClassLink 步骤 NRE 崩溃（`MetaParam.cs:231`），最早 Error 为 `FFI/Library.sl_124_0` 的 `MetaCoreDefineTypeIsNull`（"传入参数与要求参数不对应"）；CompileMath 连锁失败（`Std.FFI.Library not found`）；`out/export/Std` 最后成功导出停在 2026/8/1、Core 8/20——本机（D 盘）标准库链长期未成功构建。
- 首个异常阶段：看似 Front MetaCore（语义阶段），实根源在 Project 引用装载（references 解析）。
- 根因（链）：
  1. jsonc outputDir 原为 `F:\` 硬编码，仓库迁 D:\ 后 Strategy 2（读被引用模块 jsonc 的 outputDir 拼 module.json 路径）失效，长期落 Strategy 3 源码兜底；
  2. 2026-09-21 将 29 个 jsonc outputDir 统一改相对路径后，主模块导出恢复（`ProjectOutputEnvironment.ApplyFromConfig` 按 jsonc 所在目录解析 ✓），但 `ProjectReferenceModuleLoader.TryResolveExportModulePath`（Strategy 2）读到相对 outputDir 后**直接 `Path.Combine` 拼接、按进程 CWD 解析**——未按被引用 jsonc 所在目录解析，必然失败，行为与 F:\ 时代同样坏；
  3. Strategy 3 源码兜底对泛型构造（`List<Library>`，Library.sl:124 静态字段初始化）解析不完整 → `MetaCoreDefineTypeIsNull` → Std 失败 → Math 引用 Std 连锁失败。
- 修复点（文件/模块）：`simple_language/source/Front/Project/ProjectReferenceModuleLoader.cs` `TryResolveExportModulePath`：相对 outputDir 改按 `Path.GetDirectoryName(jsoncFiles[0])`（被引用模块 jsonc 所在目录）解析，与 `ProjectOutputEnvironment.ApplyFromConfig` 同语义。
- 如何快速复现：修复前 `dotnet run --project source\CompileStd\SimpleLanguageCompileStd.csproj` exit 0 但日志 3 错误、`out/export/Std/Std.module.json` 不更新。
- 如何快速验证：按依赖序 CompileCore → CompileStd → CompileMath 全绿：Std.module.json（10.5MB）、Math.module.json（16.2MB）恢复导出（Std 为 8/1 以来首次），日志零 `[Error]`。
- 预防建议：①**路径解析语义必须单一来源**——凡读 jsonc 路径字段处，相对路径一律按"该 jsonc 所在目录"解析（本次是 ApplyFromConfig 之外的第二处解析点，新增第三处时先找现有实现对齐）；②依赖链编译验证按 Core→Std→Math 顺序，下游失败先查上游导出物时间戳（长期未更新 = 上游早已断链）；③**驱动 exit 0 ≠ 编译成功**（CompileXxx 驱动 catch 异常才返回非零，Front 内部错误只记日志），验证必须看 module.json 时间戳 + 日志 `[Error]`；④判断"是否本次改动引入"优先取导出目录时间戳证据（git 仓库缺失时同样有效）。
- 附带记录：BaseTest 的 references 直接指向导出目录（`../../out/export/Core`），走 Strategy 1，不受本修复影响，P2 回归结论仍有效。

### [2026-09-23] MSVC vm_lib 工程链接失败 20 个 LNK2019：TLSF/SBA/mimalloc/crash 源文件未登记 vcxproj
- 现象：VS 打开 SimpleLanguage.sln 编 csimple_lang_lib 报 LNK2019×20——vm_tlsf_*（6）、vm_sba_*（5）、vm_mimalloc_*（8）、sl_crash_install（1）全部无法解析，尾部 LNK1120。
- 首个异常阶段：链接（编译全过，缺的是符号提供方源文件）。
- 根因：R6 只约束 CMakeLists 的 CSIMPLE_LIB_SOURCES；VM 内存演进（TLSF→SBA→mimalloc wrapper、crash handler）新增的 4 个 .c 均未登记 `project/vs/vm_lib/csimple_lang_lib.vcxproj`，其 ClCompile 的 `src\vm\memory\` 段只有 vm_memory/vm_gc/vm_manual_mem 三个旧文件；平时只用 CMake 构建掩盖了该漂移（vm_lib\build 下 unsuccessfulbuild 残留即早前失败证据）。
- 修复点：vcxproj 登记 `vm_tlsf.c`、`vm_small_block_allocator.c`、`vm_mimalloc.c`、`src\platform\windows\sl_crash-win32.c`（ClCompile）+ 3 个 .h（ClInclude），filters 同步并新增 `src\platform\windows` Filter；顺手给 `src/define.h` 的 EXTERN_C 加 #ifndef 保护（sl_crash-win32.c 引 Windows 头后 winnt.h 先定义，cl 报 C4005；两处值在 C/C++ 两模式均一致，纯消除告警）。
- 如何快速复现：MSBuild 编 csimple_lang_lib.vcxproj（Debug|x64），链接期集中爆 vm_tlsf_*/vm_sba_*/vm_mimalloc_*/sl_crash_install 的 LNK2019。
- 如何快速验证：MSBuild 重编——vm_lib → `build\Debug\bin\csimple_lang_dll.dll` 生成、EXTERN_C 警告消失（遗留 D9002/-std=c99 与 runtime_call.c C4101 为原有，非本次引入）；vm_exe exit=0；test_mini 仅 Release|x64 且引用的 main.c 不存在（空壳历史遗留，未被 sln 引用，未处理）。VS 工程无 TLSF/MIMALLOC/SBA 宏 → wrapper 走头文件 fallback 禁用桩（SBA 走默认 host 实现），全部符号自包含可链，无需 third_party 源。
- 预防建议：① **R6 的登记义务同样适用于 `project/vs/vm_lib/csimple_lang_lib.vcxproj`**：新增 .c 必须同时登记 CMakeLists（CSIMPLE_LIB_SOURCES）与 vcxproj（ClCompile/ClInclude + filters 两文件四处），建议把 vcxproj 检查并入新增源文件的收尾清单；② vcxproj 输出与 Ninja 树同路径（`build\Debug\bin\csimple_lang_dll.dll`），MSVC/CMake 交替构建会互覆盖 dll——P/Invoke 宿主用前先确认 dll 来源（§7 已知坑的延伸）；③ 多形态 wrapper（TLSF/mimalloc 双形态、SBA 三形态）依赖头文件 fallback 对齐 VS 工程的"无宏默认"，改 fallback 前先想 VS 工程（本次 fallback=1 修复同时救了 VS 工程：若为 0，vm_mimalloc.c 在 VS 下会 `#include <mimalloc.h>` 失败）。

### [2026-09-23] SpecialTest 直接 `csimple_lang.exe run` 全线 87 FAIL：CWD 运行约定，非 VM 缺陷（P0.2 插件骨架零影响验证附记）
- 现象：P0.2 插件骨架收尾烟测中 Core/ProjectTest/BenchMark 全过，唯 SpecialTest exit 1、FFITest 87 项 FAIL（isValid/符号解析/回调/utf8/原生内存全线挂，一整域全灭）。
- 排查路径：① CLangdll.dll 在位、module.json dllImports 正常；② A/B 对照——移除全部 plugin 改动（CMakeLists/loader/assembly 三处回退）重建重跑，FAIL 集逐条一致（87/87）→ 排除新改动引入；③ 核对 FFITest.sl `s_dllPath = "../../source/CLangdll/..."` 的相对基准：`../../` 需回到 simple_language 根 → 基准只能是 `test\SpecialTest` 用例目录。
- 根因：`sl_ffi_lib_load` 相对路径按**进程 CWD** 规范化（sl_ffi_lib_manager.h 语义），而用例内嵌相对路径的基准是**用例目录**——联合测试宿主（P/Invoke 进程内）自设 CWD 故无感；直接 run 时 CWD 指到 simple_language 根，`../../source/` 落到 `d:\project\source` 装载失败，整域 FAIL。
- 如何快速验证：CWD=`simple_language\test\SpecialTest` 下 `csimple_lang.exe run <SpecialTest.module.json>` → exit 0、0 FAIL 全绿。
- 预防建议：① 直接 run 含相对路径资源的模块时，CWD 必须与用例编写基准一致（用例目录）；② 排障判据——**单域全灭式 FAIL 优先怀疑外部资源装载（路径/CWD/位数），而非 VM 代码**；③ A/B 对照（临时回退新改动→重建→比对 FAIL 集）是判定"存量 vs 新引入"的低成本手段，比对须用完整 FAIL 列表而非记忆/摘要；④ PowerShell `Copy-Item` 保留源时间戳，Ninja 增量构建对"恢复的文件"会误判 no work to do，恢复后须 touch 再建。

### [2026-09-25] WSL/Linux 首跑 ProjectTest：系统方法全部未注册（SystemPrintln 等）→ error_code=-13；根因是 Linux exe 不导出动态符号，dlsym(dlopen(NULL)) 全落空，修复 = -rdynamic
- 现象：WSL(Ubuntu, gcc) 构建成功后运行 ProjectTest.module.json：静态初始化、`__new_object__` 均正常，但 VM 日志刷 `CallSystemMethod not registered. method=SystemPrintln`（还有 SystemTimerNowMillis/SystemDebug* 等），最终 `EIROpCode binary op need 2 words sp=0` → `VM finished with error_code=-13`、EXIT=1；stdout 仅平台校验横幅无任何测试输出。
- 首个异常阶段：VM（模块装载后的 systemCalls 注册/符号解析，非执行逻辑）。
- 根因：系统方法不是静态注册表，而是**运行期按 cvmFunction 符号名解析**（`system_method_registry.c` `vm_sysfunc_resolve_in_self`）。Windows 侧 `GetModuleHandleExW(FROM_ADDRESS)` + `GetProcAddress` 能命中 exe 内符号；Linux 侧走 `dlsym(dlopen(NULL), symbol)`——**可执行文件默认不把全局符号导出到 .dynsym**，dlsym 全部返回 NULL → `vm_system_func_dynamic_register` 静默 FALSE → 名字表/ID 表全空 → CallSystemMethod 全部落"not registered"→ println 无输出且弹空栈，栈失衡连锁出 -13。shared library 目标（csimple_lang_dll）默认导出符号所以 P/Invoke 宿主不受影响，**只有独立 exe 中招**。
- 修复点（文件/模块）：`csimple_lang/CMakeLists.txt` exe 目标的 `if(UNIX)` 块新增 `target_link_options(csimple_lang PRIVATE -rdynamic)`（等价 `--export-dynamic`，把全局符号并入动态符号表）。注意：同样走 dlsym 的插件/FFI 外部库路径在 Linux 上从此才具备工作前提。
- 如何快速复现：WSL 中 `cmake --build build/DebugLinux --target csimple_lang` 后 `SL_FORCE_RUN=1 build/DebugLinux/bin/csimple_lang run <绝对路径>/ProjectTest.module.json`（ProjectTest.jsonc platform.root 硬编码 win64，须 `SL_FORCE_RUN=1` 或 `--force-run` 跳过硬校验）。
- 如何快速验证：重跑后 `===== ProjectTest _main_ end [14ms]=====`、EXIT=0；`grep -c LID=10004/10005/10016 build/logs/VM.txt` 全 0（Linux 下 CLI 日志固定落 **cwd 的 `./build/logs/VM.txt`**，非 out\export\...\Logs\）。唯一残留 Warning `system call not registered in cvm. name=SystemCallExternalFunction` 为 Windows 专属 FFI 能力按预期跳过。
- 预防建议：① Linux/Unix 新平台首跑若"零输出 + not registered"成串出现，第一嫌疑就是**符号可见性**（exe 缺 -rdynamic / dll 缺导出），不要先怀疑注册逻辑本身；② 跨平台移植时凡依赖"按名取自身符号"的机制（GetProcAddress/dlsym）必须逐平台核对符号导出语义；③ Linux 侧日志/结果文件落点与 Windows 不同（cwd 相对 `./build/logs/`），排障先找对文件位置。

### [2026-09-26] compile 缺 `-e ir` 致 Export 阶段静默跳过：module.json 不重写而 dotnet exit 0 + "Compile completed." 双误导（升级 09-22 条目附带记录为正式复盘）
- 现象：改插件 frontend/CVM 契约后重编译 SpecialTest，`dotnet run ... compile -p test\SpecialTest\ProjectTest.sp` 退出码 0 且打印 "Compile completed."，但 C VM 运行行为不变、新用例/新打印不出现；ProcessSummary 显示 Export Pending，module.json mtime 与旧产物一致。
- 首个异常阶段：前端编译（CLI Export 门控，非代码 bug）。
- 根因（链）：
  1. `ProjectCompile.Compile()` 只跑 `RunToPhase(ECompilePhase.IR)`，源码注释明示"导出阶段由调用方按需触发"；
  2. CLI 侧 `CommandExecutor` 仅当命令行带 `-e ir`（置 `inputArgs.exportIR`）才 `RunPhase(ECompilePhase.Export)`——`compile` 动词默认**不导出**；
  3. dotnet 退出码恒 0、且无 Export 也照常打印 "Compile completed."——两个表层信号都指向成功，唯一可靠判据只剩 module.json 产物本身。
- 修复点：无代码修复（行为符合设计）；调用规范 = 产物导向的编译一律 `compile -p <sp> -e ir`（`scripts/common.ps1` 的规范编译调用即如此）。
- 如何快速复现：`compile -p <sp>`（不带 `-e ir`）跑完后 `ls` module.json mtime 不变；grep 新增符号（如新方法名）0 命中即实锤旧产物。
- 如何快速验证：补 `-e ir` 重跑 → 打印 "export module success" + module.json 被重写（grep 新串命中）。
- 预防建议：① **Front 编译成功判据 = module.json 在本次运行被重写**（mtime / grep 新串），不以退出码与 "Compile completed." 打印为准（AGENTS.md §5 已内建此条）；② "改了代码但运行行为不变"时第一怀疑项 = 产物未更新而非逻辑未生效；③ 本条为 09-22 条目附带记录（第 5 次复现）之后又一次复现，故升级为正式复盘——写脚本/文档化命令行时把 `-e ir` 与 compile 绑定，勿依赖记忆。

### [2026-09-27] lib 平台层合并入 platform/：sl_crash-win32.c 误删后靠编辑器本地历史恢复 + eglib/mono 死文件清理
- 现象：目录重构（用户指令"lib 目录下源码合并到 platform，没用的删除，抄的源码删除不用 eglib"）：`src/lib/{os,time,thread}` → `src/platform/{os,time,thread}`；中途发现 `src/platform/windows/sl_crash-win32.c`（SEH 崩溃取证，main.c 启动即装）在上一轮清理中**被误删**，且三处构建登记（根 CMakeLists / project/cmake / vcxproj+filters）需同步。
- 根因：
  1. 删除"疑似 mono 抄来"文件时只 grep 了 eglib/glib 符号面，未交叉验证 `sl_crash_install` 被 main.c 引用——同目录 mono 残骸与自研文件混放，连坐误删；
  2. 同一份文件在多个构建系统登记（Ninja CMakeLists ×2 + MSVC vcxproj + filters），路径替换漏一处即 LNK2019/文件不参与编译（§7 双树坑的又一面）。
- 修复点（文件/模块）：
  1. `sl_crash-win32.c`：从编辑器本地历史快照（`%APPDATA%\Trae CN\User\History\`）全文恢复（236 行），`sl_crash_install` 符号与 main.c 调用对齐；
  2. 35 个源文件 include 批量替换 `lib/(os|time|thread)/` → `platform/$1/`（PowerShell 字节级 UTF-8 读写保 BOM 不丢）；根 CMakeLists 12 处 + project/cmake 1 处 + vcxproj 21 处 + filters 27 处（Filter 定义一并改）；
  3. 删除：`third_party/eglib/`（构建零引用实测确认）、13 个 mono 死文件、`project/build.sh`（GBK 乱码 + apt 引用不存在的 libeglib-dev）、`project/cmake/Makefile`（-luv -leglib 死链接）；`base/chars.c` 两处 "eglib: " 文案前缀清理。
- 如何快速验证：双平台重构建 Windows 223/223 + WSL Linux 152/152 全过；SpecialTest exit 0 全绿（CSharpTest 26 + CSharpTest2 9 + CSharpTest3 2 / 0 failed，flags=7）；Linux ProjectTest LINUX_EXIT_0_OK。
- 预防建议：① 批量删除前必做"符号反向引用"检查（grep 待删文件导出函数名在被调方的引用），不能只查"它引用了谁"；② 目录迁移三处登记用同一替换串逐处验证 remaining=0；③ 误删恢复优先找编辑器本地历史（VSCode/Trae History 目录按文件 hash 存快照），早于 git 未提交场景；④ vcxproj/filters 改动前先读前 4 字节确认有无 BOM，无 BOM 才做字符串替换；⑤ 平台代码归属统一按 `md/RUNTIME_LAYOUT_GUIDE.md`（R3），lib/ 只留通用库（cjson/io/console/ffi）。

### [2026-09-27] platform 回迁 lib（用户方向调整）+ ErrorDefinitions_data.h 误删致 C1083：上届增量构建"223/223 全绿"实为假绿
- 现象：用户指令推翻上一轮合并方向（"把 platform 下的文件都合并到 src/lib 下边，在 VS 的工程里跑通"）。执行 `src/platform/{os,time,thread,windows}` → `src/lib/*` 反向迁移后，vcxproj Debug|x64 首次构建即 `log_manager.c(2,10): fatal error C1083: 无法打开包括文件: "ErrorDefinitions_data.h"`。
- 根因：
  1. 上一轮迁移中 `src/log/ErrorDefinitions_data.h`（ErrorDefinitions.csv 的 C 嵌入形式，log_manager.c L2 无条件 include）被连坐误删；当时 Ninja 增量构建 223/223 全绿是**假绿**——log_manager.c 的 mtime 未变，Ninja 直接复用旧 obj 不触发重编，缺失的头文件根本没被探测到；
  2. 全仓库无该文件副本（非 git 仓库、编辑器本地历史亦无记录），只能从真源 ErrorDefinitions.csv 重新生成。
- 修复点（文件/模块）：
  1. 平台层 4 目录平移 `src/platform/{os,time,thread,windows}` → `src/lib/`；空目录 `platform/unix` 与 `src/platform/` 整体删除；
  2. 36 个源文件 include 批量替换 `platform/(os|time|thread|windows)/` → `lib/$1/`（remaining=0）；构建登记同步：根 CMakeLists 13 处 + project/cmake 1 处 + vcxproj 22 处 + filters 46 处（Filter 定义一并改）；
  3. 从 `src/log/ErrorDefinitions.csv` 重新生成 `ErrorDefinitions_data.h`（22 行 CSV → 转义字符串 `s_embedded_error_definitions_csv`，UTF-8 无 BOM），并补登记 vcxproj `<ClInclude>` + filters（log_manager.c 已在册无需动）。
- 如何快速验证：vcxproj Debug|x64 / Release|x64 均 exit 0；SpecialTest 用 vcxproj 产物（`dotnet run --no-build`，DLL 时间戳全程未变）exit 0，CSharpTest3 passed=2 failed=0 flags=7；Ninja Windows Debug 198/198 全绿（sl_crash-win32.c 编译自 `src\lib\windows\`）；WSL Linux 增量链接 exit 0 + ProjectTest 运行 `===== ProjectTest _main_ end [13ms]=====`。
- 预防建议：① **增量全绿 ≠ 全量可编**：目录重构/删文件后必须清树或 touch 依赖方强制重编一次，换构建系统（Ninja→MSVC）交叉构建是最有效的全量校验；② 重构中删文件前先 grep 其 include 引用面（本次先 grep `ErrorDefinitions_data.h` 即可预防）；③ 验证 vcxproj 产物必须 msbuild 后 `dotnet run --no-build` + DLL 时间戳监测——裸 `dotnet run` 会触发宿主 csproj 内嵌 CMake 步骤覆盖 DLL，造成"测的不是 vcxproj 版"假象；④ 平台层最终归属 = `src/lib/{os,time,thread,windows}/`（R3，RUNTIME_LAYOUT_GUIDE.md / AGENTS.md 已同步），上一条复盘"lib/ 只留通用库"的表述自本次起作废。

### [2026-09-27] 三件清理任务：删 ErrorDefinitions_data.h（fallback 行为变化）+ vm_byte_buffer 跨目录移动 include 断裂 + nouse/ 存量隔离
- 现象/任务：① 用户判断 `src/log/ErrorDefinitions_data.h` "逻辑里没用到"要求删除；② 用户自行把 vm_byte_buffer.{c,h} 从 `src/base/` 移到 `src/vm/`，要求同步全部关联；③ 要求把 `src/` 下未接入构建的文件移入 `nouse/`（不在工程里体现）。
- 关键事实修正：
  1. ErrorDefinitions_data.h 并非"完全没用"——它是 `sl_log_manager_initialize_from_csv("./src/log/ErrorDefinitions.csv")` 加载外部 CSV 失败时的 fallback（`s_embedded_error_definitions_csv`）。删除后：CWD 在仓库根时 CSV 正常加载（无变化）；CWD 不在仓库根时仅剩 log_manager 内置 3 条 builtin defaults，错误码模板会退化。已按用户指令删除（文件 + log_manager.c 的 include L2 + fallback 分支 + vcxproj/filters 登记）。
  2. vcxproj 全量构建立刻暴露 `vm_byte_buffer.c(8,10): C1083: 无法打开包括文件: "mem.h"`——**跨目录移动必须检查被移文件自身的 include**：`#include "mem.h"` 原是 src/base/ 同目录裸引用，移到 src/vm/ 后断裂，改 `#include "../base/mem.h"`。这正是上条复盘"增量全绿≠全量可编"的再兑现（若只跑 Ninja 增量，log_manager.c / vm_byte_buffer.c mtime 变化会触发重编，但依赖断裂只有在全量构建里最先暴露）。
  3. nouse/ 盘点方法：LS 磁盘全量清单 vs 根 CMakeLists `CSIMPLE_LIB_SOURCES` 逐目录 diff + Grep 全仓 include 引用（含 `#include "..."` 各级相对路径）+ vcxproj/filters 登记交叉。移入 57 文件（core/float{3x3,4x4,8}×6、lib/render×16、lib/std×32、runtime_value/svalue×2、vm_system_registry.c 墓碑），另发现用户已自行放入 compile/csharp/ir/project 四个 zip。
  4. 两个"看似该移实为活文件"的判定：`src/vm/runtime/vm_system_registry.h` 是转发 shim（→ system_method_call/system_method_registry.h），被 7 个在编文件 include，**只移 .c 墓碑、.h 留原位**；`src/lib/ffi/gen_sl_ffi_call.py` 是 sl_ffi_call.c 的活代码生成器，保留。另清掉 project/cmake/CMakeLists 里两行磁盘上不存在的死引用（lib/std/console.c、dir.c）。
- 修复点（文件/模块）：log_manager.c（删 include + fallback 改为直接 return TRUE）；vm_byte_buffer.c（mem.h → ../base/mem.h）；byte_buffer_system_method.{c,h}、file_system_method.c（`../../base/vm_byte_buffer.h` → `../vm_byte_buffer.h` + 注释路径）；根 CMakeLists L48、vcxproj L194/L449、filters（base→vm + 删 ErrorDefinitions_data.h 条目 + 删孤儿 Filter `src\lib\std`）；md/design/STREAM_DESIGN.md 落位表述同步；AGENTS.md §4（删 std/render/compile 四目录行、补 nouse/、core 清单更新）。
- 如何快速验证：vcxproj Debug|x64 exit 0；Ninja（本机无 clang，`scripts\build-vm-win.ps1` 自动回退 MSVC+Ninja 增量）exit 0——重编恰为本次改动的 4 个文件（vm_byte_buffer.c / log_manager.c / byte_buffer_system_method.c / file_system_method.c），增量探测路径变更正确；Core.module.json exit 0、ProjectTest exit 0（Monitor P5/P6 FAIL 为已知两平台同败项，非本次引入）。
- 预防建议：① 跨目录移动文件时"改别人的引用"与"查文件自身的 include"要同步做，同目录裸引用（无路径前缀）在移动后必断；② 删"疑似无用"文件前先确认它在失败路径里的角色（fallback/降级分支平时不可见）；③ 移入 nouse/ 的文件如需恢复：移回原位 + R6 三处登记 + grep include 引用三步缺一不可。

### [2026-10-01] attribute 四时点重构验证期再现「compile 缺 -e ir 致 module.json 不重写」：dotnet exit 0 假成功，判据收敛为日志行 + 文件级 mtime
- 现象：attribute 四时点重构 P1 验证期，`compile -p <.sp>`（未带 `-e ir`）Meta/IR 阶段全 Completed、dotnet 退出码 0，看似成功；但 `out/export/<模块>/<模块>.module.json` 的 mtime 停在 15:36 原地不动（连跑两轮均如此），C VM 执行的仍是旧模块——attribute 改动全部"没生效"。ProcessSummary 中 Export 阶段显 `[Pending]`。排障初期还被 launchSettings.json 中 F: 盘历史失效路径带偏，实为噪音。
- 首个异常阶段：Export（`CommandExecutor.ExecuteCompile` 相位调度），非 Meta/IR——上游各阶段日志全绿。
- 根因：Front `compile` 动词缺省只跑到 IR 相位（RunToPhase(IR)），仅命令行带 `-e ir` 时 `CommandExecutor.ExecuteCompile` 才 `RunPhase(Export)`；缺参 → module.json 不重写，且 Front 不把"未导出"反映到 dotnet 退出码（恒 0）。另两个干扰判据一并澄清：① ProcessSummary 的 `[Pending] Export` **不能单独当失败证据**——因打印时序，成功编译（带 `-e ir`）的汇总里也会出现 `[Pending]` 字样；② **目录 mtime 不算数**（Windows 目录时间戳更新时机与文件写入不同步），必须看 module.json 文件本身的 LastWriteTime。
- 修复点（文件/模块）：无代码修复（相位调度行为符合设计）；判据修正回写三处——根 `AGENTS.md` §5（compile 命令样例补 `-e ir` + 判据注释）、`md/syntax/attribute.md` SLIR 契约节 ⚠ 警告框、`md/ai/故障排查流程.md` 本条。批量脚本 `scripts/compile-stdlib.ps1`/`compile-tests.ps1`（build-all.ps1 内部调用）本就正确携带 `-e ir`，本次 build-all 全量回归实测佐证。
- 如何快速复现：任一工程 `dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p <.sp>`（不带 `-e ir`）——本轮与下一轮 module.json 文件 mtime 均停留旧值；补 `-e ir` 后同一命令即刻重写。
- 如何快速验证：判据只认两条且须同时满足：① `Logs/Front.txt` 出现 `export module success`（ExportSLModulePackage 阶段成功）日志行；② module.json **文件级** mtime 在本次运行时间点被刷新。
- 预防建议：①本坑已在 §6 四次复发（[2026-09-12] Component ②、[2026-09-12] P2.5 AOT ②、[2026-09-29] java_hotspot ③、本次），手敲 compile 命令一律默认带 `-e ir`，日常回归走 `build-all.ps1`/`compile-*.ps1`（内建该参）；② `[Pending]` 是相位状态的瞬时快照非终态，成败判据 = 日志行 + 文件 mtime 双确认，单看任何一项都可能误判（成功也可能显 Pending、失败也可能 exit 0）；③ 排障遇"改动没生效"先查 module.json 文件 mtime 是否刷新，再查 launchSettings 等环境噪音，勿先怀疑上游语义改动。

### [2026-10-07] HttpWebTest 宿主分支找不到 .sp（ProjectSPFilePathNotFound + F 盘日志路径连锁）：Program.cs 拼名末段必须与实际 .sp 文件名一致
- 现象：`dotnet run --project CSimpleVMTest -- HttpWebTest` 报 `ProjectSPFilePathNotFound: 没有找到工程的路径:D:\project\lang\simple_language\Project.sp`（ResolveProjectSp 回退 cwd 兜底失败），连锁 `DirectoryNotFoundException: 'F:\project\lang\simple_language\out\export\Core\Logs\Front.txt'`，exit 1。NetTest 分支同构写法却正常。
- 首个异常阶段：Front CLI（`CommandInputArgs.ParseCompileProjectPath` 对 `-p` 路径的解析），jsonc 尚未加载 → Log 走 F 盘默认路径属连锁非根因。
- 根因：① 宿主 `Program.cs` L121 拼的是 `Path.Combine(repoRoot, "test", "Other", "HttpWebTest", "ProjectTest")`——`ParseCompileProjectPath` 对不以 `.sp` 结尾的路径按 `<目录>\<末段名>.sp` 解析，末段 "ProjectTest" 解析出不存在的 `HttpWebTest\ProjectTest.sp`（实际文件名是 `HttpWebTest.sp`）；NetTest 能跑纯因其 sp 恰好就叫 `ProjectTest.sp`；② `Log.cs` L88 `FrontLogDefaultPath` 硬编码 F 盘历史路径（项目 F→D 迁移遗留），jsonc 未加载时写日志即用此默认值抛异常——sp 路径修好后此异常自然消失。
- 修复点（文件/模块）：`simple_language/project/CSimpleVMTest/Program.cs` L121 末段 `"ProjectTest"` → `"HttpWebTest"`。
- 如何快速复现：修复前跑 `dotnet run --project project\CSimpleVMTest -- HttpWebTest`，Front.txt 首条即 ProjectSPFilePathNotFound。
- 如何快速验证：修复后 HttpWebTest G1-G17 全 OK（含新增 G17 协程等待组与 Text.Json 新构造）、pass=81 fail=0、exit 0；Front 日志显示 `export module success` 且 FrontLog 正确写 D 盘。
- 预防建议：① 宿主新增测试分支时 `Path.Combine(repoRoot, "test", "Other", <目录>, <sp名去扩展>)` 的末段必须与实际 .sp 文件名（去扩展）一致（JsonTest/NetTest 先例对照）；② F 盘 `FrontLogDefaultPath` 是迁移遗留默认值，凡见 F 盘路径异常先怀疑"jsonc 未加载"，优先查上游 sp 解析失败而非日志系统本身。

### [2026-10-13] wasm Playground 端到端双坑：MEMFS 布局须与磁盘同构 + Emscripten 同名库二次 dlopen abort
- 现象/任务：Playground（编辑器写 SL → 服务端 Front 编译 → 浏览器 wasm VM 执行，`wasm/playground.html` + `server.js POST /api/compile`）联调：运行 exit 0 但终端零输出；修好后又崩 `Aborted(Assertion failed: attempt to load '../Lib/Std.dll' a second time)`。
- 排障路径（R12 证据先行）：wasm 侧文件不落真磁盘、`build/logs/VM.txt` 只存在于 MEMFS → 写 `wasm/smoke-playground.js`（Node 复现前端管线）运行后 `FS.readFile('/build/logs/VM.txt')` 抓日志 → 最早 Error = `could not load module reference '../../export/Core/Core.module.json'`，后续 `CallSystemMethod not registered. method=SystemPrintln` 为连锁（R8 只看最早）。
- 根因（两个独立坑）：① Front 把 moduleReferences 重写为**相对模块导出目录**（`out/playground/Playground/`）的相对路径，前端 MEMFS 只建了一层浅目录（`/work/Playground`），`../../export/...` 解析落空 → 依赖模块整棵缺失；② Emscripten 的 dlopen shim 对同名库第二次加载在首次失败未清状态时直接 abort——`system_method_registry.c` 每个 `DllName!symbol` 注册都重进装载函数（Std 模块几十个 systemCall），首次 `../Lib/Std.dll` dlopen 失败后第二次即崩；`sl_ffi_lib_manager.c` 候选1/候选2 一次装载内也两次 dlopen。
- 修复点（文件/模块）：① `wasm/playground.html` + `smoke-playground.js` MEMFS staging 改磁盘同构（`/work/out/playground/Playground` + `/work/out/export/{Core,Std}`）；② `system_method_registry.c` 装载失败也缓存 NULL 占位（`vm_sysfunc_ext_lib_load` / `vm_system_func_preload_ext_lib` 两处），后续同名查找短路返回 NULL（宿主顺带省重复探测）；③ `sl_ffi_lib_manager.c` 抽 `sl_ffi_platform_load`，wasm 分支整体短路恒 NULL（宿主 Win/Unix 分支行为不变）。
- 如何快速复现：修前版本 `node wasm\smoke-playground.js`：浅布局 → 引用装载失败 + SystemPrintln 未注册零输出；同构布局引入 Std 后 → 二次 dlopen abort。
- 如何快速验证：浏览器 http://localhost:8080/playground 三条路径全绿（默认示例 exit 0、错误代码编译失败回显、重置后重跑）；宿主回归 Math 模块（CWD=project\CSimpleVMMathTest）VM.txt 记录 `FFI: library loaded ...math_lib.dll` + `static lib 'math_lib' preloaded` + `40/40 method(s) native-bound, 0 fallback`，重复装载命中同句柄（refcount 递增）、退出正常 unload——成功路径零回归。
- 预防建议：① wasm 排障第一手段 = Node 冒烟脚本抓 MEMFS 里的 `/build/logs/VM.txt`（真磁盘上没有）；② MEMFS staging 一律与磁盘目录完全同构，勿按"能省一层是一层"建浅目录；③ 任何「失败后重试 dlopen/LoadLibrary」逻辑在 Emscripten 下先想二次加载 abort——失败必须留 NULL 占位或整体短路；④ vmDllImports 相对路径按设计**相对进程 CWD** 解析（Math.jsonc 注释明示），standalone 跑 exe 时 CWD 须为 `project\<测试工程>`，从仓库根报 not preloaded 是预期路径约定不是回归。

### [2026-10-16] ReleaseJit 树 LNK2001：`jit_x64_encode.c` 裸用 `base_malloc` 未 include `base/mem.h`——宏名被当函数隐式声明，链接期符号不存在
- 现象：ReleaseJit 树 clean-first 重建，编译全过、双目标链接失败 `jit_x64_encode.c.obj : error LNK2001: 无法解析的外部符号 base_malloc`；同源码 Release/Debug 树（VM_JIT_ENABLE=OFF，jit 文件不参与编译）正常。
- 首个异常阶段：链接期（编译期隐式声明只给 warning，错误推迟到 LNK 才爆）。
- 根因：`base_malloc` 不是函数而是 `base/mem.h` 里的**宏**（mimalloc 接管时映射 `mi_malloc`，否则 CRT `malloc`）。`jit_buf_init` 裸写 `base_malloc(...)` 且该 TU 只 include 了 `stdlib.h` → C 隐式函数声明生成对"函数 base_malloc"的引用 → 全仓无此符号。§4.5 `base_chars_dup` 坑的链接期变体：那次是隐式声明按 int 截断指针崩在运行期，这次是符号根本不存在崩在链接期——同源不同爆点。
- 修复点：`jit_x64_encode.c` 补 `#include "../../base/mem.h"`；同时把 `jit_buf_grow/jit_buf_free` 的 `realloc/free` 换成 `base_realloc/base_free`——原代码 `base_malloc`（mi_malloc）配 CRT `free` 是分配器族混用 UB。
- 如何快速复现：`Import-VsDevEnv; cmake --build build\ReleaseJit --clean-first`。
- 如何快速验证：链接通过 + ReleaseJit `--jit` BenchMark 输出值与解释器一致。
- 预防建议：用 `base_*` 家族 API 的 TU 必须显式 include 对应 `base/*.h`（宏无法被链接器兜底，比函数缺声明更隐蔽）；JIT 树（ReleaseJit/DebugJit）不随日常构建走，改动 `src/lib/jit/**` 或公共头后**别忘单独重建两棵 JIT 树**再交工。
