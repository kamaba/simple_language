# 复盘分片 05 · 插件 / FFI / 跨语言

> 收录：csharp_mono、java_hotspot、mono、FFI、AOT 工具链、@块 errorLine、负例套件类复盘。
> 路由索引与追加规则见 [故障排查流程 §6](../故障排查流程.md)；新复盘按域追加至本片末尾。

### [2026-09-17] FFITest GetStaticLib cached same instance FAIL：跨模块泛型方法体形参槽保持 T[0] 占位符，validate_generic_reference_assignment 误拒（target 侧无豁免）
- 现象：FFITest testDllStaticImport 中 `GetStaticLib cached same instance` FAIL——`FFI.Library.GetStaticLib` 内 `s_staticLibs.add(newLib)` 后第二次调用遍历判重（`lib.handle == handle`）失败，返回新实例；SL 层无任何报错，缓存静默失效。
- 首个异常阶段：VM（runtime_object.c 的 validate_generic_reference_assignment，StoreObject/TryWriteValue 两条路径均受影响）。
- 根因（链）：
  1. 泛型方法体（`Core.List<T>.add(T item)`）只编译一次、跨模块共享——Std 调用 `List<Library>` 时形参槽的 runtime_type_ref 保持 `"T[0]"` 占位符（id==0, etype=22）而非解析后的实参类型（同模块调用编译期已特化，不受影响，故 BaseTest/CoreTest 全绿无此错）；
  2. `validate_generic_reference_assignment` 对 **source 是占位符**有豁免（id==0 放行），**target 是占位符**无豁免——`add(item)` 绑定时 item 槽即 target=T[0]，落入 non-generic 分支被误拒（PROBE-D 取证：`tgt_cls=T[0]/0 tgt_etype=22 | src_cls=Std.FFI.Library`）；
  3. 静默失败链：校验 FALSE → 形参绑定失败（item 变 null）→ `SystemArraySetValueThis` 存入 null → `this._length++` 照常执行 → toArray/索引读回 null → 判重失败。全程仅 VM.txt 留一行 `Generic reference assignment validation failed`，无 SL 层可见错误。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/runtime_object.c` `validate_generic_reference_assignment`——紧跟既有 source 占位符豁免之后新增对称的 target 占位符豁免（`target_type->class_ptr->id == 0 && name 以 "T[" 开头` 即放行；实参在每个调用点已由前端类型检查）。
- 如何快速复现：修复前跑 SpecialTest 的 FFITest（testDllStaticImport 段），`GetStaticLib cached same instance` FAIL；`test\SpecialTest\build\logs\VM.txt` 可见 `Generic reference assignment validation failed` 行（出现在 List.add 路径、无任何 SL 报错）。
- 如何快速验证：SpecialTest 全量回归——FFITest 无 FAIL、GetStaticLib 4 断言全 PASS（含 cached same instance）、ProcessTest 33/33、exit=0。
- 预防建议：①占位符豁免类校验必须 source/target 双侧对称——只豁免一侧时另一侧是占位符即误拒；②「存进去的值取出来是 null 且无报错」优先查 VM.txt 中该错误行（全库仅 runtime_object.c 两处失败点发出），属典型的校验失败静默吞值链；③跨模块泛型调用场景（引用模块的 `List<X>`/`Map<X,Y>` 等）测试覆盖必须在跨模块工程里做——同模块用例因编译期特化永远测不到占位符路径；④git 历史（`git log -- <文件>`）可快速鉴别「本次改动引入」vs「既有 bug」，本轮即借此排除重构嫌疑（runtime_object.c 上一提交后未动过）。

### [2026-09-21] CSimpleVMSpecialTest 无限挂死：MLIRToolchain.Run 顺序 ReadToEnd 管道死锁，GPU AOT 失败回退路径走不到
- 现象：`dotnet run --project project\CSimpleVMSpecialTest\CSimpeVMSpecialTest.csproj` 在 Front 编译完成、`AOT: export aot.mlir success (17 ok, 1 failed)` 后 4 分钟无输出挂死（CPU≈0）；日志目录只有 Front.txt 无 VM 日志；进程表残留 `mlir-opt.exe`（CPU≈0.015）。
- 首个异常阶段：Export（AOT stage-3 工具链调用），非 VM。
- 根因（链）：
  1. SpecialTest 含 `@GPU` 方法（`gpu.module @gpu_k0 [#nvvm.target<chip="sm_75">]`），`TryBuildAotDll` 走 GPU pass 链调 `mlir-opt --convert-gpu-to-nvvm --gpu-module-to-binary ...`；
  2. 本机无 CUDA/ptxas，`--gpu-module-to-binary` 序列化失败 exit 1，并把整个模块 IR dump 到 **stderr（巨量，远超管道缓冲区 ~4-64KB）**；
  3. `MLIRToolchain.Run`/`TryRunCapture` 用顺序同步读（先 `stdout.ReadToEnd()` 再 `stderr.ReadToEnd()` 最后 `WaitForExit`）：父进程阻塞等 stdout EOF，子进程阻塞在写满的 stderr 管道 → 双向死锁，`BuildAotDll` 约定的"失败返回 false → 回退 CVM"降级路径永远走不到。
- 修复点（文件/模块）：`simple_language/source/Front/Export/MLIR/MLIRToolchain.cs`：`Run`/`TryRunCapture` 改为 `ReadToEndAsync()` 并发排空双流后再 `WaitForExit`；新增 `Clip()`（4000 字符截断）用于异常消息——否则 mlir-opt 失败时的巨量 stderr 经 `ex.Message → error → Front.txt` 把日志灌爆（实测单次 33677 字符被截断）。
- 如何快速复现：无 CUDA 机器上跑 SpecialTest（含 AOTGPUTest.GpuMatMul），修复前必挂死在 mlir-opt。
- 如何快速验证：SpecialTest exit 0 跑完（ProcessTest 33 项 PASS），Front.txt 出现 `AOT: build dll failed, fallback to CVM` 且带 `chars, clipped` 截断标记，@GPU 方法回退 CVM 解释执行。
- 预防建议：①外部进程重定向双流时**必须并发读**（ReadToEndAsync×2 / BeginOutputReadLine），顺序 ReadToEnd 是经典死锁模式；②子进程错误输出进日志/异常消息前先截断（错误头部在 stderr 前几行，IR dump 在后）；③"AOT 失败回退 CVM"是设计约定（MLIRToolchain.BuildAotDll 文档注释），排障时若测试挂死在 Export 之后、VM 之前，先查进程表有无残留工具进程（mlir-opt/llc/link/cl）。

### [2026-09-24] P1-e SL→C# 静态调用"返回最后一个实参"：mono_domain_assembly_open 裸名不搜 assemblies_path + CallSystemMethod 失败静默不清参（csharp_mono 插件附记）
- 现象：SpecialTest 插件链路端到端 `CSharpCallInt(...Add, 1, 40, 2)` 得 2（应 42）、`CSharpCallString(...Greet, 2, "mono")` 得 "mono"（应 "hello, mono from mono"）——**返回值恰好等于最后一个实参**；后续调用 enter depth 逐次抬高（12 而非 6）。
- 排查路径：静态排除法（Front paramCount / IR 压栈序列 / VM 压栈协议 / ext API 弹出协议 / SLVmExtVm 镜像布局逐项比对全对）→ 7 处 stderr 直写插桩（[CSDBG]，绕开宿主日志链路）一次运行定位：实参弹出/编组全对，invoke/unbox 插桩不输出 → 失败在 find_static_method → find 失败分支补打 last_error 拿到铁证 `assembly open failed: SLCSharpTestLib.dll`；手动拷 dll 到插件目录仍失败，结合 probe（用 exe 目录拼绝对路径）与 mono API 语义锁定根因。
- 根因（两层叠加）：① 装载层——`mono_domain_assembly_open` 只按**路径**打开，`set_assemblies_path` 对其裸文件名解析**不生效**，SL 侧传裸名 `SLCSharpTestLib.dll` 必然失败（build 脚本同时漏拷测试程序集）；② 症状层——`csharp_exec` fail 回滚 sp/depth 到入口快照（7 槽原样保留），而 VM `OpCode_CallSystemMethod` 执行器在 fn 返回 FALSE 且 error_code==0 时只记日志**不清参不置错**，SL 赋值语句从栈顶取"返回值"取到残留的最后一个实参（Add 栈顶=LoadConstInt32 2、Greet 栈顶="mono" 字符串），形成静默错值。
- 修复点（文件/模块）：`SLPlugin/csharp_mono/src/cvm_csharp_mono/mono_bridge.c`——init 把插件 dll 目录存静态 `s_dll_dir`，`mono_bridge_load_assembly` 裸名（无路径分隔符）open 失败时拼 `s_dll_dir\<name>` 重试一次；`build-csharp-mono.ps1` deploy 段补拷 `SLCSharpTestLib.dll`。插桩全部移除。
- 如何快速复现：插件目录不放 SLCSharpTestLib.dll（或传裸名程序集）跑 SpecialTest 的 CSharpCallInt/CSharpCallString。
- 如何快速验证：CWD=`simple_language\test\SpecialTest` 下 `csimple_lang.exe run <SpecialTest.module.json>` → `C# MathUtil.Add(40,2)=42`、`C# MathUtil.Greet(mono)=hello, mono from mono`，全测零 FAIL exit 0。
- 预防建议：① mono API 中 `mono_domain_assembly_open`（路径语义）与 `mono_assembly_load_reference`/隐式解析（搜 assemblies_path）语义不同，凡对外接受程序集名的桥接层都需自带裸名→目录解析兜底；② 插件 systemCall 返回 FALSE 时 VM 侧不清参 + error_code==0 的组合会造成"返回最后一个实参"式静默错值——调用失败必须显式置 error（后续可在 CallSystemMethod 层加固）；③ 运行期取证优先 stderr 直写插桩（`--debug --log` 无 trace 落盘）；④ "返回值=某个实参"是栈残留的指纹特征，见到即查"失败路径不清栈"。

### [2026-09-24] F5 跑 testDegradation 在 catch 后 __debugbreak：CallSystemMethod 误记 Assert（vm_sys_throw 同步 throw 被 catch 清零 error_code）+ 修复回归又踩 SLVmExtVm 镜像 ABI
- 现象：VS 调试启动 CSimpeVMSpecialTest，`CSharpTest.testDegradation` 四组降级用例（no-asm/no-class/no-method/managed-exc）在 stderr 显示 `[OOMTRACE] throw CAUGHT` 即异常已正确捕获后，随即弹 `__debugbreak()` 异常中断框，指向 vm_runtime.c CallSystemMethod 附近。
- 排查路径：err.txt 显示 throw 链完整走到 CAUGHT、栈干净收尾（ROOT FRAME POPPED pending=0）→ 异常流本身正常，嫌疑落在**异常流结束后的 Assert 判定**；读 `CallSystemMethod`（vm_runtime.c 两处）见判据「fn 返回 FALSE 且 error_code==0 → 记 LID_ShowMessageAssert 日志」→ Debug 构建 sl_log Assert 路径带 `__debugbreak()`（log.c：_DEBUG + IsDebuggerPresent）→ F5 必弹框。
- 根因（链）：① `vm_sys_throw`（system_method_helpers.c）在系统方法内部**同步**调用 `vm_execute_throw` 派发 VM 异常；② csharp_mono 插件四个降级路径都走 vm_sys_throw 返回 FALSE；③ 异常被 SL `catch` 消费时 `vm_execute_throw` catch 分支执行 `vm->error_code = 0; vm->is_running = 1;`——**在系统方法返回 FALSE 之前** error_code 已被清零；④ CallSystemMethod 判据「FALSE 且 error_code==0」把「已被 catch 的业务异常流」误判为「意外失败」记 Assert → 断点中断。
- 修复：VM 结构新增 `uint32 sys_throw_epoch` 计数，`vm_sys_throw` 每次派发自增；CallSystemMethod 两处判定收紧为「FALSE 且 error_code==0 **且 epoch 未变**」才记 Assert（epoch 变 = 系统方法确实派发过 VM 异常，属预期业务流）。
- 修复引入的回归（本条核心教训）：第一版把 `sys_throw_epoch` 插进 `SLVmExtVm` **镜像前缀中部**（error_code/execute_index 之间）→ 平移其后所有镜像字段偏移 → 未重编的旧插件 DLL（echo、csharp_mono）按**旧偏移**错位读取镜像字段（症状：Echo(3.5) 出 Assert、testIntRoundtrip error_code=-91 提前停止）。取证：插件 DLL（09-23/09-24）与 csimple_lang_dll.dll 时间戳对比确认二进制 ABI 失配。二次修复：字段挪到镜像前缀**之后**的 VM 专属区（stack_slot_depth 后）+ 撤销 sl_vm_ext_api.h 镜像改动，镜像前缀恢复原始布局，旧插件无需重编。
- 如何快速验证：重建 Debug 后沙盒宿主 `passed=16 failed=0`、testDegradation 五项全 PASS（含 after-catch 栈清洁 Add(2,3)==5）、VM.txt 中 `CallSystemMethod failed` 与 `[Assert]` 均 0 条、F5 不再弹断点中断框。
- 预防建议：① 判定「系统方法意外失败」不能只看 error_code——凡存在同步 throw 路径（vm_sys_throw 等），必须区分「失败」与「已派发并被 catch 的业务异常」（时序快照/epoch 类手段）；② **SLVmExtVm 镜像前缀（止于 stack_slot_depth）冻结**：编译期被外部插件 DLL 固化偏移，中部插字段=全体旧插件错位失效；VM 新字段一律放镜像之后的 VM 专属区（约定已写入 vm_runtime.h 注释）；③ 排障期间引入的 stderr 直写插桩（OOMTRACE/BOOT 等）闭环后必须全量清理（本次 48 处），否则污染正常输出且日后误导排障；④「测试异常流中断」与「测试断言失败中断」都表现为 __debugbreak，先看 stderr/logs 分辨是哪条 Assert 路径，别急着怀疑异常派发本身。

### [2026-09-24] @csharp_mono 协程+isolate 示例 20 全绿后退出阶段 0xC0000005：isolate 三段式根因——shell 反向引用过早清空 / 跨线程分配域不对称 / 主 shell 在 vm_destroy 前被释放致 TLS 悬垂
- 现象：SpecialTest 新增 testAtSignCoro（协程 spawn+await 调 mono）/testAtSignIsolate（Isolate.run 在另一线程执行 mono 并返回结果）后全部 20 用例 PASS 全绿，但进程退出阶段崩溃 0xC0000005——「测试全绿 + 退出崩溃」，退出码非 0。
- 排查路径：R12 先插桩（SLCS_TRACE 门控 stderr，Start-Process OS 级重定向落盘，逐层加桩逐层跑）→ trace 停在 VD5：`run_vm_with_module → vm_isolate_shutdown() → vm_destroy → VD5 vm_free_object_pool → obj[0] vm_array_free → vm_mem_sba_free → vm_mem_sba_current`；结合崩溃偏移符号化 + 读源码确认 sba 路由链 `TLS→iso→iso->vm→vm->mem_manager->sba` 中 iso 悬垂、iso->vm 读出 -1、按 (VM*)(-1) 解引用 mem_manager 即 AV。三段式：每修一层重跑取证，暴露下一层。
- 根因（三段叠加，按发现顺序）：
  ① shell 反向引用过早清空：主 isolate shell 的 iso->vm 在 VD5 之前被清，VD5 池对象释放经 shell 路由取不到 vm → 修复①：反向引用清除块移到 VD6 之后（vm_runtime.c post-VD6 块）。
  ② 跨线程分配域不对称：worker 线程在主 VM 分配域 carve 的池对象，释放时按释放线程路由错 sba 域 → 修复②：vm_memory.h/c 新增 vm_mem_alloc_domain_set per-thread 分配域 override；vm_isolate_reap 内 set/restore 域重定向；isolate_system_method.c launch_common 显式 domain_set(NULL)。
  ③ 主 shell 在 vm_destroy 前被释放致 TLS 悬垂（本崩溃直接根因）：vm_isolate_shutdown 在 vm_destroy 之前把主 shell 一并 base_free，但主线程 TLS 仍指向它；随后 vm_destroy 的 VD5/VD6 池释放经 TLS 拿到悬垂 shell → 修复③：shutdown 循环改断链遍历，主 shell（is_main）摘除保留（s_main_isolate 继续指向）；新增 vm_isolate_release_main() 由 CLI 在 vm_destroy 之后调用（释放 shell 字段+本体，最后清主线程 TLS）。worker 路径不变：thread_main 尾部自 reap（毁 worker VM、shell 保留）→ 清自己线程 TLS → shutdown 循环释放 worker shell。
- 关键设计约束（为何不能简单清 TLS=NULL）：主 VM 的池对象 carve 在主 VM 自己的 mem_manager->sba，VD5/VD6 释放依赖 `TLS→iso→iso->vm→mem_manager->sba` 路由；TLS 先清 NULL 会错路由到进程静态 s_main_sba → 堆损坏（历史上冻结 bug 的镜像）。主 shell 必须活到 vm_destroy VD6 之后——「路由载体必须活过依赖该路由的最后一次释放」。
- 修复点（文件/模块）：vm_runtime.c（post-VD6 反向引用清除块）；vm_memory.h/c（per-thread 分配域 override）；vm_isolate.c（reap 域重定向 / shutdown 断链+主 shell 保留 / vm_isolate_release_main 新增）；vm_isolate.h（声明）；isolate_system_method.c（launch_common domain_set(NULL)）；cli_command.c（vm_destroy 后调 vm_isolate_release_main）。排障插桩 7 文件 77 处调用已全量清理，重建零 error 零 warning。
- 如何快速复现（修复前）：`csimple_lang.exe run <SpecialTest.module.json>`（含 isolate/协程用例）→ 20 [PASS] 全绿打印完后退出阶段 AV 0xC0000005。
- 如何快速验证（修复后）：同一命令 → 退出码 0 + 20 [PASS]/0 [FAIL]；`dotnet run --project project\CSimpleVMSpecialTest` 全量联合测试（Front 编译 + C VM P/Invoke）20 PASS/0 FAIL 正常收尾。
- 预防建议：① shutdown/destroy 序列要区分「VM 所有权」与「shell 所有权」：凡带线程本地路由（sba 按线程域路由）的资源，载体生命周期必须覆盖依赖该路由的最后一次释放，先用后松；② 「对象本体释放」与「线程绑定（TLS）清除」必须同点成对出现，只做一半 = 悬垂或错路由二选一；③ 头插链表 shutdown 优先断链逐个摘除而非边遍历边释放再置全局头，主/特殊节点（is_main）显式豁免并交由所有者释放；④ 全绿≠收工：退出码必须纳入验收（Start-Process -PassThru 查 .ExitCode），「测试断言 + 进程退出码」双指标。

### [2026-09-24] @csharp_mono(){} 插件化后负向用例报错行号差 1：插件 errorLine 为块体段号（1-based），Front 换算基准误用 @ 标签行而非 '{' 所在行
- 现象：块体解析移入 csharp_mono 插件 frontendLibs 解析器（SLLabelParser）后，负向用例在中段注入 `var m <- $a`（实际文件行 169），Front 报 LID 20055「行 168」——差 1 行。
- 排查路径：插件侧 errorLine 语义逐点核对（SLLabelParser.ParseBody：`lines = body.Split('\n')`，中段命中 `errorLine = i + 1`）→ Front 侧换算公式比对（CSharpMonoSourceRewriter：`startLine + parse.ErrorLine - 1`，startLine = `@csharp_mono()` 所在行 163）→ 验算：errorLine=6 时 163+6-1=168，与报错值吻合、与实际 169 恒差 1。
- 根因：跨组件行号契约错位。插件收到的 bodyText 从 `{` 之后截取，其 `Split('\n')` **首段是 `{` 所在行的行尾残余**——即插件 1-based 段号以 `{` 行为第 1 段计数，换算回文件行必须以 lbraceLine（`{` 所在行）为基准：`lbraceLine + errorLine - 1`；Front 却用了 `@` 标签行（startLine）。`@` 与 `{` 同行时（`@csharp_mono(){`）二者相等、巧合掩盖；分行书写（本仓库测试风格）即暴露。
- 修复点（文件/模块）：CSharpMonoSourceRewriter.TryMatchBlock——新增 `lbraceLine = 1 + CountNewlines(s, 0, lbrace)`，OutCountError（20056）与 !Ok（20055）两处换算基准 startLine → lbraceLine，注释写明「bodyText 首段 = `{` 行尾残余」的推导。
- 如何快速复现（修复前）：CSharpTest.sl testAtSignMono 第一块中段插入 `var m <- $a`（落位文件行 169）→ `compile -p test\specialtest\ProjectTest.sp` → 20055 报「行 168」。
- 如何快速验证（修复后）：同注入同命令 → 报「行 169」与实际一致；还原注入后 `dotnet run --project project\CSimpleVMSpecialTest` 全量回归：cscAtSign 4 blocks 构建成功 + CSharpTest 20 PASS/0 FAIL + exit 0。
- 预防建议：① 跨组件传「行号」必须连同**行号基准**写进契约（本例插件段号基准=`{` 行，既非块首也非标签行）；② 文本偏移类接口优先传「绝对行号」或「区间起点+相对值」二选一，接收方按发送方声明换算，禁止各自猜；③ `@标签` 与 `{` 分行是常见书写风格，凡按「标签行+相对偏移」换算处都应自带一个分行样例的负向用例。

### [2026-09-25] @块 IR 路线 payload 退化为类型全名 → CVM 装配静默失败 → 运行期 -86：IRData.PackOpValue 白名单缺 SLAtSignLabelCallPackage 分支
- 现象：`@csharp_mono(){}` 重构为通用 IR 路线（`OpCode_CallAtSignLabel(124)` + payload JSON）后，testAtSignMono/testAtSignCoro/testAtSignIsolate 三用例运行期报 -86（绑定畸形）；Front 编译、SLIR 导出、CVM 装配全程无任何错误日志。
- 首个异常阶段：VM 运行期（-86）——但真正根因在 Front IR 序列化（`IRData.PackOpValue`）。
- 排查路径：R12 先插桩取证——module.json 里 124 指令 payload 不是 JSON 对象而是 `"SimpleLanguage...SLAtSignLabelCallPackage"` 类型全名字符串 → 反查 Front 序列化点：`IRData.PackOpValue` 是已知 payload 类型的白名单分支序列化，`FinalizePack` 的 fallback 是 `ToString()`；`SLAtSignLabelCallPackage` 是本次新增类但未登记分支。CVM 侧装配期 `vm_parse_call_package` 对非 JSON payload 解析失败后**静默 return（保留原文）**，运行期 case 124 拿到非 4 字节绑定索引才报 -86——两级延迟掩盖了出错位置。
- 根因：① 新 payload 类未登记 `PackOpValue` 白名单 → fallback `ToString()`；② C VM 装配期失败路径无日志（诊断盲点）。
- 修复点（文件/模块）：Fix 1 = `simple_language/source/Front/IR/IRData.cs` `PackOpValue` 增加 `SLAtSignLabelCallPackage` 分支（JSON 序列化）；Fix 2 = `csimple_lang/src/vm/assembly/sl_runtime_assembly.c` `sl_assembly_rewrite_atsign_instruction` 的 `vm_parse_call_package` 失败分支补 Warning 日志（与 entry 缺失/通道数不匹配两处既有日志同模式）。
- 如何快速复现（修复前）：任意 `@csharp_mono(){}` 块（入+出通道）→ 编译导出 → module.json 中 124 指令 payload 为类型全名字符串 → CVM 运行报 -86。
- 如何快速验证（修复后）：module.json payload = `{"entryIndex":N,"paramCount":K,"tryCatch":false,"methodName":"..."}`；`dotnet run --project project\CSimpleVMSpecialTest` → C VM 21 PASS/0 FAIL（含负向豁免用例）+ 装配期无 Warning。
- 预防建议：① **新增 IR payload 类必须同步登记 `IRData.PackOpValue` 白名单分支**（与 R7 四处同步同级约束；fallback 是静默 `ToString()`，编译期无任何提示）；② C 侧装配期凡「保留原文 + return」的容错路径必须带日志，禁止纯静默；③ 运行期 -86/-87/-88/-89 一族（`sl_atsign_binding.h`）先查 VM 日志真实位置 `csimple_lang\build\logs\VM.txt`（cli 重定向，非 `out\export\...\Logs\`）；④ payload 是否合法在 module.json 一眼可验——导出物巡检比运行期报错早两级。

### [2026-09-25] signLabel 构建收编进插件 frontendHandle 后 csc 报 CS1003/CS1026：块体 `using X;` 指令行未外提落进 Main 方法体
- 现象：`@csharp_mono(){}` 块体编译（SLLabelBuilder → csc 合并编译）收编进插件 frontendHandle 工程后，`compile-tests.ps1 -Tests SpecialTest` Front 端报 LID 22136 构建失败，csc 输出 Entry_N.cs 报 CS1003（应输入 ...）/CS1026（应输入 )），指向生成源码中 `using System...;` 所在位置。
- 排查路径：csc 报错自带 (文件,行,列)——**列号定位法**取证：Entry_3 报错列恰好多 1 列，对应测试源码该行双空格缩进，证明生成代码原样保留了块体行的列偏移；反查 SLLabelParser 头区吸收逻辑，`using X;` 未在指令行白名单内。
- 根因：头区 TryParseDirective 只匹配 `import X;`；块体写 `using X;` 时被当中段代码保留 → SLLabelBuilder 生成 Entry_N.cs 时中段代码原样落进 Main 方法体 → 方法体内出现 using **指令** → CS1003/CS1026。
- 修复点（文件/模块）：`simple_language_plugins/csharp_mono/frontendHandle/SLLabelParser.cs`——TryParseDirective 统一匹配 `import X;` 与 `using X;` 双语法（均吸收并提升为 C# `using NS;` 外提）；中段出现指令行一律报 LID 20055；`using ( ... )` **语句**是合法 C# 代码放行（IsUsingStatement 判别：`using` 后跟 `(`）。
- 如何快速复现（修复前）：块体写 `using System.Collections.Generic;`（头区或中段）→ `powershell -File d:\project\lang\scripts\compile-tests.ps1 -Tests SpecialTest` → 22136 + csc CS1003/CS1026。
- 如何快速验证（修复后）：同命令 → `build success 5 blocks`；CVM `run SpecialTest.module.json` → 21/21 PASS + exit 0；负向套件 `test\Other\AtSignLabel\atsign-negative-test.ps1` PASS（20055×5 + 20056×1）。
- 预防建议：① 源码到源码转译链路的报错定位优先用**列号差值**对照原文缩进——列号不受跨组件行号基准错位干扰（对照本节 [2026-09-24] 行号差 1 条目）；② 目标语言关键词常有双重语义（`using` 指令 vs `using` 语句），插件解析器必须显式区分而非整体拒绝；③ 头区双语法约定已同步 `md/lib/AtSign/CSharp_Mono.md`（R13），新目标语言插件实现 SLLabelParser 时先定义「指令行白名单 + 语句放行」边界再写圈地逻辑。

### [2026-09-25] 负向新用例 negBadSlType「静默」+ SpecialTest 哨兵重名 20053：陈旧 frontendHandle dll（版本错配静默放行）+ 同名同签注册幂等豁免
- 现象：① AtSignNegativeTest 新增 negBadSlType（`var unknown_tp name <- $name`，预期 LID 20055「类型标记无法映射」）后，套件 Front.txt 只有 5×20055 + 1×20056，该用例无任何报错；② SpecialTest 全链路回归 Front 编译报 LID 20053 `import system method call name had define! name=AtSignLabelCall`。
- 排查路径：改写器主循环复查（无错误预算、无提前中止，块必然下发插件）排除 Front 侧 → 嫌疑收敛到插件解析器装配版本：检视部署的 SLCSharpMonoFrontend.dll 二进制判定新旧，期间按文本模式检索消息串被 .NET UTF-16 字符串堆误导（多区直查全落空，误判绕弯）→ 改用可靠物证（部署副本 vs obj\Release 产物时间戳/字节数对照）确认部署 dll 陈旧、未随 SLLabelParser.cs 的 slType 契约预检重编 → 手动重编部署 → **探针对照法**：把负例原样复制进正向文件 CSharpTest.sl（probeBadSlType），跑一次编译 → 20055 正常报出（unknown_tp），自证链路无恙（验证完即删）；20053 另线定位到哨兵双侧注册。
- 根因（两个独立问题）：① **Front 与插件解析器跨装配版本耦合**——新 Front 已支持 3 段入通道 `var slType target <- $sl` 并把 slType 原文随通道表下发，旧版插件 SLLabelParser 无 MapCSType 契约防御预检，未知标记被静默接受（解析 Ok、无 20055）：版本错配表现为**静默放行**而非报错；② 哨兵 AtSignLabelCall/AtSignLabelCallVoid 在 SpecialTest.jsonc `systemCalls[]` 与被引用模块（Core）导出表中**同名重复注册**，旧逻辑不比对签名一律报 20053。
- 修复点（文件/模块）：① 重编部署 frontendHandle（`dotnet build simple_language_plugins\csharp_mono\frontendHandle\SLCSharpMonoFrontend.csproj -c Release`）；② `source/Front/Project/SystemMethodCallDeclaration.cs` 同名导入幂等豁免——同名且签名一致（IsSameDecl）静默保留首个，仅签名冲突才报 20053（注释明确哨兵跨工程重导出场景）。
- 如何快速复现（修复前）：插件目录放旧版 SLCSharpMonoFrontend.dll + 负例 `var unknown_tp name <- $name` → 编译无任何 20055；SpecialTest 编译报 20053。
- 如何快速验证（修复后）：负向套件 `powershell -File test\Other\AtSignLabel\atsign-negative-test.ps1` → 6×20055 + 1×20056（含「行 103: 入通道 1 类型标记无法映射为 C# 类型（'unknown_tp'）」）；`dotnet run --project project\CSimpleVMSpecialTest` → Front 0 error + 22/22 PASS + exit 0。
- 预防建议：① **插件 Parse/Build 契约变更必须同步重编部署 frontendHandle dll**——Front 与插件单侧升级都可能静默错配（新语法被旧 dll 吞掉），后续可考虑契约版本握手；② 判定 dll 新旧用时间戳/字节数对照构建产物等物证，勿在二进制里按文本模式检索消息串（.NET 字符串堆是 UTF-16，UTF-8/ANSI 直查必落空，多区混看必误判）；③ 探针对照法（负例复制进正向工程编译一次）能快速剥离环境因素，但**验证完必须删除**——留在正向文件里主回归必败（块解析失败整块透传 → Lexer Token 错误）；④ 系统方法注册表的跨模块同名导入应幂等（同签保留首个），只有签名漂移才是错误。

### [2026-09-25] dll 修复后负向套件仍 FAIL（5×20055 ≠ 6 预期）：jsonc outputDir 相对深度错位，断言脚本读到的是陈旧日志——判日志新旧以文件系统时间戳为准
- 现象：frontendHandle dll 已重编、探针已删，重跑 `atsign-negative-test.ps1` 仍报 `[FAIL] 20055 count mismatch`（实测 5×），且被读 Front.txt 行内时间戳 [11:52:56] 看似「刚刚跑过」，形成「修复无效」假象。
- 排查路径：先怀疑 dll 修复无效/改写器逻辑（读 AtSignLabelSourceRewriter 全流程排除提前中止与错误预算假设）→ 文件系统 LastWriteTime 取证：被读 Front.txt = 2026/9/25 **11:52:56（上午）**，当前时刻 00:07——脚本本次运行根本没有重写该文件 → 手动跑同命令，看 Front CLI 输出的 `OutputPath:` 行 → 实际日志落在 `test\out\export\AtSignNegativeTest\Logs\Front.txt`，而脚本读的是 `simple_language\out\export\...`——**两条路径错位，脚本永远在读旧日志**。
- 根因：① AtSignNegativeTest.jsonc `outputDir: "..\..\out\export"` 的解析基准是**工程目录**（.sp 所在 = `test\Other\AtSignLabel`）而非 CWD，2 级上溯只到 `test\` → 产物误落 `test\out\export`（违反「产物只写 simple_language/out/」约定），与脚本既有读取路径（3 级上溯）不一致；② 行内时间戳为 12 小时制且无 AM/PM，[11:52:56] 被误读成 23:52，加深「重跑过但仍失败」的误判。
- 修复点（文件/模块）：`test/Other/AtSignLabel/AtSignNegativeTest.jsonc` outputDir 改 `..\..\..\out\export`（3 级上溯 = simple_language 根，对齐脚本读取路径与工作区产物路径约定；脚本零改动）+ 同步修正 desc 计数（20055×5 → ×6）；删除误生成的 `test\out` 整树。
- 如何快速复现（修复前）：outputDir 少一级上溯 → 跑负向套件脚本 → 断言永远拿陈旧日志（计数恒定、不随源码变化）。
- 如何快速验证（修复后）：重跑脚本 → `[PASS]`（6×20055 + 1×20056）；`Test-Path test\out` = False；`simple_language\out\export\AtSignNegativeTest\Logs\Front.txt` LastWriteTime 与当前运行时刻一致。
- 预防建议：① **判断日志新旧一律用文件系统时间戳（LastWriteTime），不看日志行内时间戳**——行内 12 小时制无 AM/PM，跨午/深夜排查必误读；② jsonc 相对路径的解析基准是工程目录而非 CWD，独立测试工程新增时先数清上溯级数（`test\Other\AtSignLabel` → `simple_language` 需 3 级）；③ 断言脚本读取路径与被测命令实际落盘路径必须同源——Front CLI 运行时打印的 `OutputPath:` 行就是权威落点，排障第一步先核它；④「修了还坏」先证伪「看到的输出是新产生的」（mtime 对照当前时刻），再怀疑修复本身。

### [2026-09-26] @csharp_mono double 入通道 -86：脱糖 Kind 分流缺 Double 支，double 实参误投 ChannelInInt（CSharpTest2 bring-up 之一）
- 现象：新增 `CSharpTest2.sl`（SampleCompute 六步计算链移植，`var double a <- $a` 等 double 入通道）首跑，带 double 入通道的块在 opcode 124 处带 error_code=-86（BINDING）停机；Front.txt 同源可见 Meta 层 ChannelInInt 解析链 Warning（double 实参喂 int 形参），与运行期会话 stage 缺失互证。
- 首个异常阶段：Front 脱糖（Kind 分流缺支）；运行期 -86 为下游连锁。
- 根因（链）：
  1. `AtSignLabelSourceRewriter` 入通道脱糖分流只有两支（`string` → ChannelInString、缺省 → ChannelInInt），`double/float/float64/f64` 标记误落 Int 变体；
  2. `ChannelInInt` 的 SL 包装签名为 `(int entry, int value)`，double 变量无法正确暂存进会话 → 运行期通道侧 staged 数与 paramCount 不符 → -86；
  3. 下游其实早已就绪：CVM 会话层 `SLLabelValue` 支持 FLOAT 四态编组（§A20.6 stage 3 文档写对）、插件 labelExec 的 FLOAT→mono double 形参编组也已支持——缺的只是 Front 分流与 refModule 包装这最上游两小块。
- 修复点（文件/模块）：① `simple_language_plugins/csharp_mono/slang/CSharpMono.sl` 增 `ChannelInDouble( int entry, double value )` 包装（转调变参系统方法 AtSignChannelIn）；② `AtSignLabelSourceRewriter.cs` 分流扩为三分支（`double/float/float64/f64` → `ChannelInDouble`）。改 refModule 后必须先重编 CSharpMono refModule 再编引用方工程。
- 如何快速复现：修复前跑 SpecialTest CSharpTest2.testComputeGeometry——块在 opcode 124 报 -86 停机。
- 如何快速验证：重编 refModule → 重编 SpecialTest → C VM 跑：`[PASS] geometry(3,4): 3-4-5 triangle -> geo == 5.480000`（F6 字符串精确断言通过）。
- 预防建议：① 新增通道承载形态时**三处契约一处不能少**：Front 脱糖分流 / refModule 包装函数 / CVM 会话编组——动手前先查下游是否已支持（本例下游全就绪，白等了一轮排障）；② -86 排障对三处：payload `paramCount` ↔ 会话 staged 数 ↔ `channels[].slType` 映射；③ Front 的 Warning（非 Error）也要看——上游 Warning 常是下游 -8x 停机的第一信号。

### [2026-09-26] @csharp_mono 块内 managed 异常 -88 致命不可 catch：labelExec exc 分支补 V8 throw_error 上抛 -95（CSharpTest2 bring-up 之二）
- 现象：double 修复后重跑，CSharpTest 26/26 + CSharpTest2 前两段 PASS，testComputeThrow 打印段头后即崩 exit 1、无 [PASS]/[FAIL]；`csimple_lang\build\logs\VM.txt` 链：`labelExec: managed exception raised by SLAtSign entry` → `AtSignLabel block 11: labelExec failed (-1)` → `VM finished with error_code=-88`。SL 侧 `label xxxBlock { try computeThrow( 0.0, 4.0 ) } catch { caught = 1 }` 结构性接不住。
- 首个异常阶段：VM 运行期（opcode 124 插件异常传播契约）。
- 根因（链）：
  1. 插件 `csharp_label_exec_inner` 的 invoke exc 分支只 `return SL_PLUGIN_ERR_GENERIC`，未像 csharp_exec 失败路径那样经 V8 `host->throw_error` 上抛；
  2. `sl_atsign_invoke` 契约：非 OK 返回码由调用者（vm_runtime.c case 124）赋 `vm->error_code` 并 `is_running=0` 直接停机——异常状态不参与，SL catch 永远接不住；
  3. 深层矛盾：同为 managed 异常，CSharpCall 路径（csharp_exec）走 -95 可捕获，labelExec 路径却走 -88 致命——同一插件两条路径错误模型不一致。
- 修复点：`cvm_csharp_mono.c` labelExec exc 分支改 `slcs_throw( (VM*)ctx->vm, SLCS_ERR_MANAGED_EXC )`——throw 已派发（`vm_sys_throw` 当场 `vm_execute_throw` 展开 try 栈跳 catch）时 **return SL_PLUGIN_OK**：rc==0 使 VM 免走 -88 致命路径（若此时仍返回非 OK，主循环 `is_running=0` 会覆盖异常跳转，白跳）；rc==OK 且 is_void&&out==0 时 VM 侧照常 drop session。老宿主无 V8 维持 -88 原报告。
- 如何快速复现：块内 C# `throw new ArgumentException(...)` + SL 侧 `label{}try{}catch{}` 包裹断言 caught==1——修复前 exit 1 停机于 -88。
- 如何快速验证：msbuild 重建 cvm DLL（vswhere 定位 MSBuild，其不在 PATH）重跑：exit 0，`[PASS] compute(0,4): validation throws -> caught (MANAGED_EXC -95)`，CSharpTest 26/26 零回归。
- 预防建议：① **插件→VM 失败传播区分两条路**：致命 error_code 路（契约=调用者赋 error_code 停机，异常状态不参与）vs 可捕获 vm_sys_throw 路（V8 throw_error + epoch/execute_throw）——新 capability 设计时先定错误模型并写进契约注释；② **throw 已派发后插件必须返回 OK**，否则 `is_running=0` 覆盖异常跳转（本例最隐蔽的一步）；③ 负例（try/catch 捕获断言）与能力同期落地（延续同日通道原语条目经验）。

### [2026-09-27] @csharp_mono data 镜像 -88：RuntimeClass.name 是 fullName，mono find_class 按纯名检索必落空——镜像编组 type_name 取短名（strrchr 截尾）
- 现象：新增 testAtSignDataMirror（`AtSignMetrics m <- $m` data 镜像用例）首跑，块在 opcode 124 处 error_code=-88 停机；`csimple_lang\build\logs\VM.txt` 见 `AtSignChannelIn: value 0 marshal failed`，labelExec 侧失败。
- 首个异常阶段：CVM 入通道编组 → 插件 labelExec（跨界类型名形态不一致）。
- 根因（链）：
  1. CVM `sl_atsign_marshal_object` 填 `SLLabelObject.type_name` 时直接用 `RuntimeClass.name`——它是 fullName（含模块前缀，如 `SpecialTest.AtSignMetrics`）；
  2. 插件 `mono_bridge_find_class` 按纯类名（同 `SLAtSign` 命名空间拼名）检索，fullName 必 miss；
  3. find_class 失败路径 s_err 未清写，残留上届错误文案，误导排障方向。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/atsign/sl_atsign_channel.c`——type_name 取短名（`strrchr(name, '.')` 截尾，无 `.` 用原名）。
- 如何快速复现：修复前跑 SpecialTest CSharpTest2.testAtSignDataMirror——`marshal failed` 后 -88 停机。
- 如何快速验证：重建 CVM 后重跑：`[PASS] dataMirror: mirror fields width*height == 24`。
- 预防建议：① 跨界（VM↔插件）传类型名先问清两侧形态（fullName vs 短名），必显式归一——"名字"这类看似无害的元数据也写进契约注释；② 插件错误路径清写 s_err，别让上届文案残留骗人；③ -88 真因多在插件侧（桥接失败），先看插件侧日志再动 CVM。

### [2026-09-27] @csharp_mono 嵌套 data 镜像 -89：成员读出 etype=EVMType_Class(20) 落 switch default——`case EVMType_Class` 并入对象镜像编组
- 现象：dataMirror 修复后新增 testAtSignDataNested（`o.inner` 嵌套字段读取）仍 `value 0 marshal failed`，error_code=-89；顶层 data 通道正常、仅嵌套成员挂。
- 首个异常阶段：CVM 入通道编组（嵌套对象字段读出的 etype 恢复）。
- 根因（链）：
  1. 嵌套 data 字段经 `vm_runtime_object_try_read_value` 读出，Object 成员 actual_type 恢复逻辑返回 etype=EVMType_Class(20)（而非顶层 DATA 值的 EVMType_Object）；
  2. `sl_atsign_to_label_value_depth` 的 switch 只覆盖 Boolean/整型组/浮点组/String/Object，`EVMType_Class` 落 default → -89 拒收；
  3. 顶层 data 值走 EVMType_Object 分支无恙——只有嵌套成员踩 Class 支路，负向面极窄。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/atsign/sl_atsign_channel.c` 两处——① `case EVMType_Class` 并入对象镜像编组分支（与 EVMType_Object 同路）；② 递归守卫条件由仅 Object 扩为 Object||Class。TYPE_OBJECT 等非 REGULAR/DATA 对象仍被 meta kind 守卫拒绝，行为不变。
- 如何快速复现：修复前跑 SpecialTest CSharpTest2.testAtSignDataNested——`marshal failed` + -89。
- 如何快速验证：重跑：`[PASS] dataNested: recursive mirror o.inner (level=3, weight=2.5) -> 325`，classMirror（REGULAR class 实例镜像）一并转绿。
- 预防建议：① 对外值接口的 etype switch 逐 case 穷举全枚举或注释明确漏盖项——default 拒收会掩盖"哪个 etype 没盖"的关键信息；② R12 生效：三行插桩（`probe unsupported etype=20`）一次定位，别靠猜；③ "顶层过、嵌套挂"先查成员读出路径的 etype 恢复逻辑（actual_type），与顶层入口 etype 并不同源。

### [2026-09-27] @csharp_mono jsonc references 引入库 CS0234：命名空间与内部类同名——"类名.方法"被解析为"命名空间.成员"查找
- 现象：新增 jsonc `plugins.csharp_mono.references[]`（预编译 ExtraMathLib.dll）+ `sources[]`（SlExtraUtil.cs）首跑，Front.txt 报 `Entry_17.cs(14,63): error CS0234: 命名空间"ExtraMath"中不存在类型或命名空间名"Twice"`——AtSignLabel build failed（kind=buildFailed），导出不中断但 SLAtSign.dll 未更新。
- 首个异常阶段：插件 frontend csc 合并编译（引用库内部结构）。
- 根因（链）：
  1. 引用库源写成 `namespace ExtraMath { public static class ExtraMath {...} }`——命名空间与内部类同名；
  2. 块内 `using ExtraMath;` 后写 `ExtraMath.Twice( 5 )`：C# 名字解析把 `ExtraMath` 绑定到**命名空间**（using 引入的命名空间与类型冲突时，`X.Y` 成员查找走命名空间子级而非类型成员），在命名空间 ExtraMath 里找 Twice（子命名空间/类型）→ 必然 miss；
  3. 报错是 CS0234（命名空间内无成员）而非 CS0246（缺 using）——错误码本身即暗示"命名空间找到了但成员不对"，与常规"忘 using"症状不同，应第一时间警觉命名冲突。
- 修复点（文件/模块）：`simple_language/test/specialtest/CsExtra/ExtraMathLib.cs`——命名空间改 `ExtraMathLib`（与类名 ExtraMath 分离），重编 dll；块内 `using ExtraMathLib;`（`simple_language/test/SpecialTest/CSharpTest2.sl` testAtSignCsExtra）。
- 如何快速复现：引用库命名空间与类同名（如 `namespace ExtraMath { class ExtraMath }`），块内 using 后 `类名.方法(...)` 调用——csc 报 CS0234。
- 如何快速验证：重跑 SpecialTest 编译（17 blocks build success）→ C VM 运行：`[PASS] csExtra: sources(SLExtra)+references(ExtraMathLib) merge -> Sum(4,Twice(5))+|(3,-4)| == 21`，CSharpTest2 9/9。
- 预防建议：① **对外发布的库（尤其供 @csharp_mono 块内 using 调用的）命名空间勿与内部类同名**——写库骨架时即分开命名；② csc 报 CS0234（命名空间有、成员无）≠ CS0246（名字全无）——先查"同名命名空间遮蔽类型"；③ AtSignLabel buildFailed 不中断导出，运行期才暴露旧 SLAtSign.dll——改 jsonc references/sources 后必须核对 Front.txt 无 `cscAtSign: build failed` 再跑运行期。

### [2026-09-27] CSharpTest3 WinForms+HTTP bring-up 三层故障链：mono JIT eager 类型解析两缺 dll + embedded domain ExeConfigFilename null + 测试 URL 网络不可达
- 现象：`@csharp_mono(){}` 块内 WinForms 弹窗 + `HttpWebRequest` GET，逐层暴露三故障：① 窗口闪退（XplatUI 类型初始化失败，`XplatUIX11:pollfds` 不可解析）；② HTTP 触发 mono FATAL UNHANDLED EXCEPTION 进程 exit 255（`HttpWebRequest:tlsProvider` TypeLoadException）；③ 补齐 dll 后 HTTP 仍失败：`ConfigurationErrorsException: Error Initializing the configuration system`（inner：`ArgumentException: The 'ExeConfigFilename' argument cannot be null`）；④ 域配置修复后 HTTP 超时——curl 实测 ip-api.com 在当前网络不可达（非 mono 缺陷）。
- 首个异常阶段：插件运行期（mono JIT / BCL）。
- 根因（链）：
  1. **mono JIT eager 类型解析**：编译方法体时解析其中引用的**全部**类型——Windows 上 XplatUI 走 Win32 分支，但 `InitializeDriver` 中 X11 分支的 `new XplatUIX11()` 也要求 `XplatUIX11:pollfds`（`Mono.Unix.Pollfd[]`）可解析 → BCL 树必须含 `Mono.Posix.dll`；同理 `HttpWebRequest:tlsProvider` → `Mono.Security.dll`（Windows 走 SslStream 分支同样要求 TLS 依赖类型可解析）。官方发行版 BCL 目录本就带这两个 dll，vendored 树裁剪时漏了；
  2. **嵌入式 domain 的 ConfigurationFile 为 null**：`mono_jit_init` 建的 domain 不像 mono.exe 会挂 exe 同名 config；`WebRequest.Create → PopulatePrefixList → ConfigurationManager` 因 `ExeConfigFilename` null 抛异常——任何走 System.Configuration 的 BCL 路径全挂；
  3. **测试 URL 选型只看"返回 json"，没先验证网络可达性**：ip-api.com 在当前网络不可达（curl exit 28），HTTP 超时是环境问题却伪装成"运行期缺陷"。
- 修复点（文件/模块）：
  1. `Mono.Posix.dll` + `Mono.Security.dll`（mono 6.12.0.206 MSI 提取）拷入 vendored 与部署两处 BCL 树 `lib/mono/4.5/`；
  2. `simple_language_plugins/csharp_mono/cvm/src/cvm_csharp_mono/mono_loader.{h,c}`：MonoAPI 表 + 符号表登记 `mono_domain_set_config`；`mono_bridge.c` init 在 jit_init 成功后调 `api->domain_set_config(s_domain, dllDir, "cvm_csharp_mono.config")`（宿主职责，全局受益）；空骨架 `cvm_csharp_mono.config` 放插件根两处 + `build-csharp-mono.ps1` deploy 段补拷贝行（原 BCL 通配只拷 `*.dll`，machine.config 与 .config 不随脚本同步）；
  3. `simple_language/test/SpecialTest/CSharpTest3.sl`：URL 换 `http://httpbin.org/get`（经典 json 测试服务，curl 实测可达）；顺带收紧断言 `n >= 3` → `n == 3 || n == 7`（flags=5「HTTP 失败但 Label 显示错误文本」曾误判 PASS）。
- 如何快速复现：vendored BCL 树删 `Mono.Posix.dll` 跑 WinForms 块；或注释 bridge 的 domain_set_config 跑任意 `WebRequest.Create` 块；或块内请求不可达 URL。
- 如何快速验证：跑 SpecialTest：`[CSharpTest3] flags=7 (shown=1 http=2 label=4)` + 两断言 PASS，CSharpTest 26 + CSharpTest2 9 + CSharpTest3 2 / 0 failed。
- 预防建议：① vendored BCL 裁剪以官方发行版 `lib/mono/4.5/` 目录为准，勿按"用到才留"裁（JIT eager 解析会踩非执行分支的类型）；② embed 宿主用 System.Configuration 前必挂 domain config（`mono_domain_set_config`），machine.config 探测位 `<prefix>\lib\mono\<ver>\`；③ 外网 HTTP 用例选 URL 前先 `curl.exe -m 8 <url>` 验证环境可达，优先 httpbin.org 这类稳定测试服务；④ 排障"HTTP 超时/失败"时先在宿主 curl 同 URL 排除环境因素，再查 managed 栈；⑤ build 脚本部署通配符只认 `*.dll` 时，新放非 dll 文件（.config 等）必须同步加拷贝行。

### [2026-09-29] java_hotspot 插件接入 SpecialTest 四连坑：`half` 关键字做变量名 + refModule 须 compiled module.json（source jsonc 不建类链）+ SL int 是 32 位
- 现象：java_hotspot 插件（`@java_hotspot(){}`，HotSpot via JNI）接入 SpecialTest 四轮编译依次暴露：① File 层 `NodeStructParseNameIsKeyword: 变量定义的名称不允许使用关键字!! Lex: half`（88/91 行），连锁 Meta/IR/Export 全跳过；② 修复后 MetaCore 层 24 错误 `解析链 节点[JavaHotspot] 没有找到`（每个 @块行一条），但日志显示 `Reference module loaded (source): JavaHotspot.jsonc structCount=1` 像是加载成功；③ 编译 slang refModule 时 IR 全过但 `out/export/JavaHotspot/JavaHotspot.module.json` 不产出（ProcessSummary 中 Export/ExportModule 恒 [Pending]）；④ Warning `value '4000000000' (Int64) is out of range for target type 'Int32'`（用例断言 4000000000/2==2000000000 实际会回绕失败）。
- 首个异常阶段：① File（关键字检查）；② MetaCore（refModule 类链）；③ Export（compile 阶段选择）；④ 词法/类型宽度（Warning 不阻断）。
- 根因：① `half` 是 SL 的 Float16 类型关键字（`md/syntax/float8_16.md` 关键字体系），不能做变量名；② refModule 多级加载策略中 compiled `out/export/<M>/<M>.module.json` 优先于 source `.jsonc`，**source jsonc 装载不建立类链**（structCount=1 只是 manifest 计数）——对照 csharp_mono 能工作纯因其 out/export 侧存在历史编译产物 CSharpMono.module.json，而 build-csharp-mono.ps1 本身不含 slang 编译步；③ Front `compile` 不带 `-e ir` 时只跑到 IR、Export 永远 Pending（与 2026-09-12 Component 复盘②同因，common.ps1 口径 `compile -p $info.Sp -e ir`）；④ SL `int` = Int32（32 位），64 位字面量/变量须用 `long`——勿以 C#/Java 的 64 位 int 直觉写 SL。
- 修复点（文件/模块）：`test/SpecialTest/JavaTest.sl`（`half`→`halfVal` 五处；`int big/int halfVal`→`long`）；`simple_language_plugins/java_hotspot/cvm/build-java-hotspot.ps1` 新增 2.6 步 slang refModule 编译（`compile -p <绝对路径>.sp -e ir` + module.json 重写时间戳判据，Push-Location 到 simple_language 根规避相对路径解析坑）；`ProjectTest.jsonc`（compileFiles + plugins 段 java_hotspot 登记）。
- 如何快速复现：清空 `out/export/JavaHotspot/`（只留 source jsonc）后编译任何引用该插件 refModule 的工程。
- 如何快速验证：SpecialTest `compile -e ir` 全绿无 Warning；Front.txt 出现 `AtSignLabelSourceRewriter: 'JavaTest.sl' rewritten N block(s)`；module.json `atSignLabel[]` 含 java_hotspot 条目（entryMethod 完整方法规格如 `run:(J)J`）；build-java-hotspot.ps1 三段全绿。
- 预防建议：① SL 用例变量名先对照 `md/syntax/` 关键字表（half/float8 系均保留）；② 插件 refModule 必须先编译出 module.json 再被引用方使用——插件 build 脚本应内含 slang 编译步（PLUGIN_TEMPLATE.md T10 已固化）；③ `compile -p` 用绝对路径或正确 cwd（相对路径 `..\..\` 会错解析并触发 F: 盘历史路径异常）；④ 跨语言桥用例的整数宽度断言按 SL 类型表写（int=32/long=64），64 位全范围用 long 字面量。

### [2026-09-29] java_hotspot 端到端三连坑：MSVC `_snprintf` 截断判定读未初始化栈字节（伪「静默退出」）+ 类缓存容量照抄 mono 装配集上限
- 现象：SpecialTest 运行，首块 Entry_19（testPlainBlock）完整 PASS（HotSpot stdout + SL 断言），进程在 `===== JavaTest.testIntOutChannel =====` 头打印后终止——ExitCode=0、无 stderr、无 hs_err_pid，像被静默杀掉；修复后推进到 testSessionReuse 又停在块头，仍 exit 0。
- 首个异常阶段：VM 运行期（插件 `sljh_label_exec` / `jvm_bridge`），非 Front/IR——DebugCode IR.txt 反查确认 desugar 无误（CallAtSignLabelVoid + LoadConstInt32 19 + ChannelOutInt + StoreLocal）。
- 根因：①（静默伪象·方法论坑）插件 `sljh_throw` 抛的 VM 异常只写 `csimple_lang\build\logs\VM.txt`（LID=10004 Runtime Error），**不进 stdout/stderr，且 CVM 进程退出码为 0**——「stdout 静默 + exit 0」不代表无错误，独立运行排障第一取证点是 VM.txt（本案证据在 4335 行 `[Error] @java_hotspot: entry name too long`）；②（误报元凶）`_snprintf(class_slash, sizeof(class_slash), "SLAtSign/%s", entry)` 后以 `class_slash[sizeof-1] != '\0'` 判截断——MSVC `_snprintf` **未截断时只写 NUL 到 buf[len]，buf[len+1..size-1] 保持未初始化栈垃圾**（与 C99 snprintf 语义不同），判尾字节读的是垃圾：Entry_19 调用时垃圾恰为 0 蒙混、Entry_20 垃圾非 0 误报（实际 "SLAtSign/Entry_20" 仅 17 字节，256 缓冲远未截断）；③（类缓存容量）`SLJH_MAX_CLASSES=8` 注释"镜像 mono 装配集上限"——mono 是一个 assembly 装多个类，java_hotspot 是**每个 @块 一个 Entry 类**，JavaTest 一组就 10 个类，第 9 个类（Entry_27）即 `class cache full (8)` → error_code=-104。
- 修复点（文件/模块）：`simple_language_plugins/java_hotspot/cvm/src/java_hotspot/cvm_java_hotspot.c`（class_slash 截断判定改保存返回值 `n < 0 || (size_t)n >= sizeof(buf)`）；`jvm_bridge.c` 同模式 3 处一并修（resolve_classes_dir 裸名分支 / find_class_cached key / find_method_cached key）+ 容量 `SLJH_MAX_CLASSES 8→64`、`SLJH_MAX_METHODS 32→128`（每条目 ~400B 静态数组，开销可忽略）。
- 如何快速复现：旧 dll 跑 SpecialTest JavaTest（≥2 块触发栈垃圾随机误报；≥9 块触发类缓存满）。
- 如何快速验证：`build-java-hotspot.ps1 -NoDeploy` 三段全绿 → `csimple_lang.exe run --no-banner out/export/SpecialTest/SpecialTest.module.json` 输出 `========== JavaTest end: passed=10 failed=0 ==========`（8 块 10 断言，含 testThrow managed-exc -105 捕获与 testSessionReuse cache hit）。
- 预防建议：① MSVC `_snprintf` 截断判定**只看返回值**，禁止以尾字节非 NUL 判定（勿假设 C99 全缓冲填充语义）；② CVM 独立运行排障第一取证点永远是 `csimple_lang\build\logs\VM.txt`，stdout 与退出码均不可靠（VM 异常不进标准流 + 退出码恒 0）；③ 插件缓存容量按自身粒度定（java 每块一类 vs mono 每模块一 assembly），照抄蓝本常量前先对齐语义；④ abi 注释与实现冲突时以实现为准并记录（`sl_plugin_abi.h` out_count 注释 "CallVoid=0" vs `sl_atsign_binding.c` is_void 分支实传 out_count=1 并期望插件填 out_values[0]）。

### [2026-09-29] P4 OBJECT 树镜像接入：SLAtSign.jar 三方定位不同位（C 侧 dll 同级零级 vs C# 侧上溯两级）+ 部署脚本思路过时
- 现象：JavaTest.sl 新增 testObjectTree（入通道 SL 类名 `JavaPoint pv <- $p` + 出通道 `sl.SLLabelValue.ofObject(...)`）后重编 SpecialTest，java_hotspot 块 Entry_29.java javac 报 6 错「找不到 sl 包符号」（Front 日志中 javac 输出为 GBK 乱码，靠乱码形态 + Entry_N.java 行号定位）；Front 记 `ExportCscAtSignBuildFailed`（kind=buildFailed）但整体 exit 0——**编译看似成功实为块体未编出**，运行期该块必然 -86。
- 首个异常阶段：Front 导出期（SLLabelBuilder javac 子进程），非 VM——日志判据必须看 `AtSignLabel: build success (N block(s))` 行而非 dotnet 退出码。
- 根因：SLAtSign.jar（`cvm/runtime/src/sl/SLLabelValue.java` 编出的 helper jar，块体编译期 javac `-cp` + 运行期 C 侧 URLClassLoader 双消费）**三方定位不同位**：① C 侧 `jvm_bridge.c` `SLJH_JAR_RELATIVE "runtime\\SLAtSign.jar"`——相对插件 dll 目录**零级**拼；② C# 侧 `SLLabelBuilder.ResolveRuntimeJar` 上溯两级（`cvm\runtime\`）找 jar；③ 实际产物在 `cvm\runtime\build\`，且 build-java-hotspot.ps1 旧部署逻辑拷贝到 `out\export\SpecialTest\plugins\...`（-NoDeploy 后整个目录不存在）。而 ProjectTest.jsonc java_hotspot 段是**原位 lib 引用**（不走 PluginLibExportManager 拷贝），SLLabelBuilder 拿到的 libDir = 源仓库 dll 目录 `cvm\lib\windows-x64\`——三方唯一的正确位置 = dll 同级 `lib\windows-x64\runtime\SLAtSign.jar`。
- 修复点（文件/模块）：`simple_language_plugins/java_hotspot/frontend/SLLabelBuilder.cs`（ResolveRuntimeJar 改零级直拼 `libDir\runtime\SLAtSign.jar`，与 C 侧同位；130-132 行注释同步）；`simple_language_plugins/java_hotspot/cvm/build-java-hotspot.ps1`（删 -DeployDir/-NoDeploy 参数，jar 无条件原位部署到 `lib\windows-x64\runtime\`）。
- 如何快速复现：删 `cvm\lib\windows-x64\runtime\SLAtSign.jar` 后编译任何含 `sl.SLLabelValue` 类型引用的 java_hotspot 块体。
- 如何快速验证：build-java-hotspot.ps1 全链 exit 0（含 "runtime jar deployed -> ...\lib\windows-x64\runtime\SLAtSign.jar"）→ 重编 SpecialTest Front.txt 出现 `AtSignLabel: build success (11 block(s))` → 运行 JavaTest `passed=12 failed=0`（testObjectTree 断言 q.x==7、q.y==12 双向通过）。
- 预防建议：① 跨语言共享资源（jar/头文件/清单）的定位必须**双侧同位锚定单一真源**（本案三方：C 运行期 / C# 编译期 / 构建部署，锚定 dll 同级相对路径）；② Front 导出期插件构建失败在 onUnavailable=warn 下不阻断编译（dotnet 仍 exit 0）——判据看日志 build success 行；③ jsonc 原位 lib 引用的插件，其运行时资源部署目标也须原位（勿沿用拷 out\export 的旧思路）；④ javac/GBK 子进程输出乱码时按乱码形态 + 生成文件名行号反查即可，不必先修编码。

### [2026-09-29] P4 callSL 通道回调：字符串实参悬垂（JavaTest2 callSL void 乱码）——vm_eval_push_string 浅包装插件临时 buffer，callSL 返回即被 free_label_value_tree 释放
- 现象：JavaTest2.sl 五用例中 int/double/string 返回值/嵌套全绿，唯独 testCallSLVoid 的 `[FAIL] callSL void: host side effect visible`——SL 侧 `g_helperNote` 读出乱码（HelperLog 的字符串实参在静态字段存储后内容被改写），且乱码形态随运行变化（复用堆内存）。
- 首个异常阶段：VM 运行期（Front 全绿、module.json 正常；乱码只在字符串**实参**流向静态字段的长生命周期路径出现，返回值路径正常）。
- 根因：**ABI 内存契约违规**——`sl_plugin_abi.h` 约定 "args 树插件分配插件释放（callSL 返回后宿主只读已失效）"，`jvm_bridge.c` `java_sl_sl_channel_call` 在 `cleanup_args` 用 `free_label_value_tree` 释放实参树；而宿主侧 `sl_plugin_host.c` hostCallSL 的 STRING 分支用 `vm_eval_push_string`（= `vm_object_new_string` **浅包装**：member 0 只存 `char*` 的裸指针，不拷贝内容，契约=调用方保证字符串生命周期 ≥ 对象生命周期）把插件临时 buffer 直接交给 VM 长期持有。`OpCode_LoadConstString` 传常量池指针（生命周期=VM 全程）所以安全，外部临时 buffer 传入即悬垂。排障证据链：① 插桩打印对象地址 + `--debug`（开 `vm_object_set_trace_frees`）对照 `csimple_lang\build\logs\VM.txt` FREE_PATH 日志，三对象运行期**零释放记录**（仅 vm_destroy 的 `FREE_PATH=direct`），但 7135 行运行中字符串内容已乱码 → 排除 ARC/GC/manual 全部释放路径（ARC is_owned=0 直通、MANUAL 模式 gc_managed=0 sweep 跳过、Java 侧不调 Memory.free）；② 唯一剩余解释=对象活着、member 0 的 char* 指向的 C buffer 被释放复用 → 命中 jvm_bridge.c cleanup_args。
- 修复点（文件/模块）：`csimple_lang/src/vm/plugin/sl_plugin_host.c`（hostCallSL STRING 分支改 `vm_push_cstr` 深拷贝——base_malloc 拷贝 + 浅包装 + `vm_pool_add_object` 入池 + 压 PTR 槽，与系统方法侧"外部 C 串压栈"同口径，process_system_method.c `vm_proc_new_string_obj` 同款蓝本；失败返 SL_PLUGIN_ERR_NOMEM）。
- 如何快速复现：修复前跑 JavaTest2 testCallSLVoid——Java 侧 `sl.SLChannel.call("JavaTest2.HelperLog", sl.SLLabelValue.ofString("from-java"))`，SL 侧 HelperLog 把实参存入静态字段 `g_helperNote`，callSL 返回后读出乱码。
- 如何快速验证：SpecialTest 运行 `[PASS] callSL void: host side effect visible (g_helperNote)`、JavaTest2 `passed=6 failed=0`、JavaTest `passed=12 failed=0`，整体 exit 0。
- 预防建议：① **`vm_object_new_string`/`vm_eval_push_string` 是浅包装，只可用于生命周期覆盖 VM 全程的指针**（常量池、base_malloc 后自行保活的 buffer）；外部 C 字符串压栈一律走 `vm_push_cstr`；② 宿主实现插件 ABI 的 callSL/出参通道时，凡**跨调用持有**插件侧数据必须深拷贝，ABI 契约"返回后只读已失效"是硬边界；③ `--debug`（CLI 同时开 `vm_set_debug(1)` + `vm_object_set_trace_frees(1)`）的 FREE_PATH 四路日志（arc/manual/direct/gc_sweep）是排除"对象被释放"假设的利器——零命中 + 内容乱码 = 悬垂而非释放；④ VM run 的 sl_log 日志落 **cwd 相对 `build/logs/VM.txt`**（本次=`csimple_lang\build\logs\`），不在模块导出目录也不在 stdout——找日志用 `Get-ChildItem -Recurse -Filter VM.txt` 按修改时间过滤。
