# 复盘分片 02 · 崩溃与内存

> 收录：0xC0000005 / 0xC0000374 崩溃、GC/ARC、TLSF/SBA、mimalloc、越界写、悬垂指针、容器裸存类复盘。
> 路由索引与追加规则见 [故障排查流程 §6](../故障排查流程.md)；新复盘按域追加至本片末尾。

### [2026-09-19] DebugTest T2 字符串 watch 0xC0000005：两个 pop 函数解包语义不对称，string wrapper 被自动解成 char* 后当 VMObject* 解引用
- 现象：Debug 系统 P1（vm_debug.c 落帧）验收时，DebugTest T2（字符串 watch）运行期 0xC0000005；数值/bool watch 全正常。崩点随修复推进分两处：先在 clone 阶段（wrapper 被当普通对象处理），放行后推进到 format 阶段（build_string 首行解引用野指针）。
- 首个异常阶段：VM（vm/runtime/debug/vm_debug.c 落帧管线的 clone + format 两处；按 R12 先 stderr 插桩取证再改逻辑）。
- 根因（链）：
  1. 两个弹参函数族**解包语义不对称**：`vm_try_pop_runtime_value` 对 PTR 槽硬编码 etype=EVMType_Object（system_method_helpers.c），弹出完整 wrapper；而 `vm_registry_try_pop_ptr` / `vm_sys_data_pop_ptr`（data_system_method.c L36-105）对 string wrapper **自动解包成裸 char\***；
  2. clone 阶段：字符串 wrapper 未能识别，被 `vm_debug_clone_value` 当普通对象走对象克隆路径（崩点一）；
  3. format 阶段：`vm_debug_format_object_text` 推 ptr 调 `vm_sys_data_build_string`，其弹参 `vm_sys_data_pop_ptr` 已把 wrapper 解包成 char*，随后强转 `(VMObject*)raw` 传 `try_build_string_by_object`，其首行 `vm_type_object_try_get_runtime_type(obj)` 解引用即崩（崩点二）——**期望 VMObject\* 的管线拿到裸 char\*，把字符串内容当地址解引用**。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/debug/vm_debug.c` 三处——①新增 `vm_debug_value_is_string_wrapper`：etype 限定引用槽（Object/String）后**向对象本身探测**（`vm_object_try_get_string_ptr` 只对真 string wrapper 返回 TRUE，其余对象安全拒绝；标量槽 union 复用 vm_object 存值，禁止探测）；②`vm_debug_clone_value` 字符串分支：直取内容 strdup → `vm_object_new_string` 新 wrapper 入池（字符串不可变，无需走对象克隆）；③`vm_debug_format_value` 对字符串 wrapper 直接取内容 dup 返回，**整段绕过 build_string 管线**。
- 如何快速复现：BaseTest DebugTest T2——`String s = "..."` 后 `Debug.watch( s, "s" )`，修复前 0xC0000005。
- 如何快速验证：DebugTest 六段断言（T1 数值 / T2 字符串 / getFrame / T3 混合 / 截断 / 环形）+ §5.5 边界五用例（null / bool / float / 字面量 / 快照语义）全 OK exit 0；另 `-O2` 快速路径 frameCount==0 不落帧、compile.debug=false 时 IR 无任何 Debug opcode。
- 预防建议：①PTR 槽弹出的值 etype 统一为 Object（硬编码），**真实类型必须向对象本身探测**，勿信 pop 后的槽位 etype；②字符串 wrapper 严禁经会自动解包的 pop 函数（vm_registry_try_pop_ptr / vm_sys_data_pop_ptr）流转到期望 VMObject* 的管线——同源坑已在 mem_clone 与 build_string 双双实锤；③崩点随修复推进（修一处崩下一处）说明同根因多点触发，识别出一类解包坑后先 grep 同型调用点收齐再改，勿修一处验一处；④排障守 R12：崩溃类问题先 stderr 插桩拿崩点证据再动逻辑。

### [2026-09-19] DebugTest P2 0xC0000374/0xC0000005 交替崩溃且崩点漂移：renew_type 宏参数无括号，`len + 1u` 按乘法优先级展开致 39 字节越界写堆
- 现象：Debug 系统 P2（区间与采样控制）验收时 DebugTest 运行期随机崩溃——0xC0000374（堆损坏）与 0xC0000005（访问违例）交替出现、崩点漂移（多落在 RING 环形帧缓冲段附近），并伴 RING 段诡异慢：带 stderr 插桩单跑 776ms（输出 21MB）甚至 60s 超时，插桩全清后全绿仅 19ms（约 40 倍差）。
- 首个异常阶段：VM 运行期（`vm/runtime/debug/vm_debug_stats.c` 区间统计扩容路径；Front/IR/装载全正常）。
- 根因（链）：
  1. `base/mem.h` 的 `renew_type(type, mem, n_structs)` 宏体 `sizeof(type) * n_structs` **参数未加括号**——同文件同族 new_type/new0_type/newa_type/newa0_type 四个宏的 size 全带括号，唯独它漏（手写笔误，修复前就潜伏）；
  2. 新调用点 `vm_debug_stats.c:36` 传入复合表达式 `st->stats_len + 1u`，展开成 `sizeof(VMDebugIntervalStat) * st->stats_len + 1u`，`+1u` 被排除在乘法之外；
  3. 首条区间记录（len=0）扩容：期望 40 字节（VMDebugIntervalStat x64 恰为 40 字节），实际只分配 `40×0+1 = 1` 字节，随后按 40 字节结构体整体写入 → **越界 39 字节**，踩烂相邻堆块头部与填充区；
  4. 堆元数据损坏 → 后续任意 free/realloc 随机触发 0xC0000374；被踩指针解引用 → 0xC0000005；崩点随堆布局漂移——典型"受害者报错、肇事者在别处"。
- 关键取证（R12 先插桩后修复）：①被覆盖内存呈 `0xCDCD…C0` 模式（MSVC Debug 堆 0xCD=未初始化填充）证明越界写入了相邻块，残留 `0x60` 为旧 label 指针低字节；②`_CrtSetAllocHook` 打印 realloc size：**期望 80（40×2）实际 41（40×1+1）**，一击实锤宏展开缺陷。
- 修复点（文件/模块）：`csimple_lang/src/base/mem.h` L92 宏体加一对括号：`sizeof(type) * (n_structs)`，与同族四宏风格对齐。renew_type 全仓仅 2 处调用（vm_debug_stats.c:36 / vm_debug.c:702，均为本次 Debug 系统新增），无既有调用点受影响。
- 如何快速复现：修复前跑 BaseTest DebugTest——首个区间落账（Debug.begin/end 首次 stats_record）即触发 len=0 越界写 39 字节（**确定性写入越界**）；崩溃是否当次显现随堆布局而定（随机报错）。
- 如何快速验证：修复后 `csimple_lang.exe run --test out\export\ProjectTest\ProjectTest.module.json`：DebugTest 断言全 OK（含采样控制 setSkip/pause/disable 3 项），全工程 32 项断言全 OK、exit 0、全速 19ms；可疑时用 AllocHook 抽查该处 realloc size = 40×(len+1)。
- 预防建议：①**宏体中每个参数必须括号包裹**，新写/复制宏先比对同文件同族宏风格（本次五连宏仅一个漏括号，恰是唯一踩坑者）；②0xC0000374 与 0xC0000005 交替、崩点漂移 = 优先怀疑堆越界写（受害者模式），勿被表面崩点带偏方向；③MSVC Debug 堆填充是免费取证手段：0xCD 未初始化 / 0xDD 已释放 / 0xFD 前后护栏，读被覆盖处数值形态可反推覆盖源；④"期望 N 实际 M"型尺寸缺陷用 `_CrtSetAllocHook` 打印 size 一击定位，比读崩溃现场快一个量级；⑤排查用的 crtdbg 钩子/DBG 插桩事后必须清理——本次 21MB stderr 插桩让运行慢约 40 倍，还诱发"超时误判未跑完"；⑥连带教训（2026-09-12 已复盘、本次复发）：Front `compile` 默认不跑 Export，**必须带 `-e ir`** 才更新 module.json——编译"成功"但断言依旧 FAIL 时，先查 module.json 时间戳与内容（如 Contains 新变量名 skipHits），别急着怀疑修复无效。

### [2026-09-20] ARC P2 probe：objectCount 释放后 +1——vm_pool_remove_object 只置槽 NULL 不减 obj_count，确定性释放对 Memory.ObjectCount() 不可见
- 现象：ARC P2（非逃逸 new 局部插入 ArcRetain/ArcRelease）运行 probe 验收时，`Memory.objectCount()` 前后 745 → 746（+1 泄漏假象），§12 验收「块结束后不增长」判定 `(c1 <= c0)` 为 False；但 `Memory.refCount(claimed)` 返回 1 正确，retain/release 语义本身无恙。
- 首个异常阶段：VM（Front/IR/Export 全对：module.json 里 makeClaimed 17 条指令含 122/123 配对且 release 在可达出口；vm_runtime.c 分派与 vm_object.c retain/release 实现均正确）。
- 根因（链）：
  1. `Memory.ObjectCount()`（memory_system_method.c）直接返回 `vm->obj_count`——计数口径即池水位；
  2. `vm_pool_remove_object`（runtime_object_pool.c）摘除小对象时只置槽 NULL **不递减 obj_count**——槽位留洞，计数保持旧水位；
  3. 而 GC 压缩路径 `vm_gc_sweep`（vm_gc.c）以 `obj_count = write_idx` 收缩计数——同字段两个写入方语义不一致，反证 obj_count 语义应为「存活对象数」而非「历史水位」；
  4. 因此 ARC release 归零级联 free、`Memory.Free/Release` 手动释放等**所有确定性释放路径**的计数可见性都被此缺陷掩盖——对象已 free、refCount 正确，唯独 ObjectCount() 不降。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/object_pool/runtime_object_pool.c` `vm_pool_remove_object` 改 swap-remove——尾槽补洞 + `obj_count` 递减，池保持紧凑不变量（全池遍历方 vm_free_object_pool/vm_gc_sweep/vm_gc_decay_arc_holds 均有 NULL 防御，安全）。
- 如何快速复现：跑 `out/arc_probe`（makeClaimed 内 `new` + 块结束），修复前 objectCount before 745 → after 746。
- 如何快速验证：probe 输出 `objectCount before == after (745==745)` 且 `(c1 <= c0) -> True`；BaseTest 全量回归（DebugTest/WatcherTest 全段）全绿 exit 0。
- 预防建议：①**同一计数字段的全部写入方语义必须一致**——add 递增、remove 递减、GC 压缩按 write_idx 收缩，发现「某路径不递减」先对照其他写入方定语义再改；②新增确定性释放路径（ARC/手动 Free）验收时必须含 `Memory.ObjectCount()` 前后对比用例，仅验 refCount 不足以暴露池计数缺陷；③对象池若允许留洞，遍历方需 NULL 防御——本次选择保持池紧凑（swap-remove）而非留洞，是让 `obj_count == 存活对象数` 不变量最简单的维护方式。
- 附带记录：①仓库自 F:\ 迁 D:\ 后共 **29 个 jsonc 含 31 处 `F:\` outputDir 硬编码**（test/ 各用例、samples/Game/TankGame、Front/Lib 各库、SLPlugin），已于 2026-09-21 全部统一改为相对路径（grep `F:\project` 于 `**/*.jsonc` 零残留），后续引出 references 解析缺陷见下一条复盘；②`compile` 不带 `-e ir` 不更新 module.json 的坑本次再次复现（第 4 次），已有多条记载，不再重复立条。

### [2026-09-21] ProjectTest 挂死 8/8 + QDEQ_STUCK 60 万次：queue/stack/set 的 store_array 裸存绕过 ARC transfer-hold 协议，grow 换数组即被 SL 层释放
- 现象：ProjectTest（QueueTest 的 testInterleavedStress）8/8 挂死；插桩后 `QDEQ_STUCK`（_count 字段声称有元素但 items=null/len=0）单次运行 609,119 次死循环；`QGROW` 存入的新数组同秒被 `FREE_PATH=arc` 释放。
- 首个异常阶段：VM（容器系统方法 `*_system_method.c`），非 Front。
- 根因（链）：
  1. `vm_queue_store_array`（queue_system_method.c，stack/set 同构复制粘贴）把数组指针**裸存**进对象成员槽，绕过 transfer-hold 协议（GC_DESIGN 13.4 / ARC_MEMORY_DESIGN 5.1）；
  2. grow 路径中 SL 层 `resizeArray` 局部变量 `newArr` 是唯一强引用：函数 epilogue 的 ArcRelease -1 后无槽位 +1 抵消 → 新数组归零释放、`_items` 成员悬垂；
  3. 反向缺陷：旧数组被替换时不 -1 槽位持有计数 → 泄漏；
  4. 挂死形态：dequeue 读 `_count`>0 但 items 悬垂/长度 0，空队分支永不满足退出条件。
- 修复点（文件/模块）：`csimple_lang/src/vm/system_method_call/{queue,stack,set}_system_method.c` 三个 `vm_*_store_array` 辅助函数统一补协议：签名加 `VM* vm`；先对传入数组 `vm_object_is_valid && vm_obj_is_owned` 才 `vm_object_retain_saturated`（+1），再对被替换旧值 `vm_mem_arc_release`（-1，迟于 +1，别名时净零）；全部调用点同步（queue 2 / stack 2 / set 10 处，set 的 `vm_set_clear_arrays` 穿透加 VM 参数）。
- 如何快速复现：修复前跑 ProjectTest，testInterleavedStress 的 `dequeue sum(1..100)` 永不返回。
- 如何快速验证：ProjectTest 全过且 `dequeue sum(1..100) = 5050`、`sum(101..200) = 15050`；QDEQ_STUCK 609,119 → 1（合法空队：items 有效、countfield=0）；回归 10/10 轮全绿（~6.7s/轮）。
- 预防建议：①**容器 C 侧辅助函数往成员槽存引用时，必须走 transfer-hold 协议**——权威模板 `vm_runtime_object_hold_owned_reference`，C 层 `vm_array_new` 创建的数组 is_owned=0 时 +1 自然 no-op，但 -1 旧值永远不能省（SL 层构造器 opcode 存入的数组是 owned）；②排查同类时 grep `vm_object_try_store_member_value` 的裸调用点（linkedlist/json 等存疑点）；③**插桩日志若读 `->length` 等字段，必须在可能释放该对象的调用之前输出**——本次 QGROW 日志放在 store_array 之后读 old_arr->length，命中 MSVC debug 堆 dead-fill（0xDDDDDDDD=-572662307）UAF 读，先以为是新缺陷实为插桩自身缺陷。

### [2026-09-21] store_array 裸存审计第四例：table 的 `vm_table_store_array` 同构缺陷（泄漏方向，非悬垂）
- 现象：无运行期症状——审计发现（grep `vm_object_try_store_member_value` 裸调用点第四例），与 queue/stack/set 复制粘贴同源。
- 首个异常阶段：VM（`table_system_method.c`），非 Front。
- 根因（链）：`Table.sl` 的 `_columns`/`_rows` 是 List（SL 构造器初始数组 owned、成员槽 +1 过）；`vm_table_list_append` grow 路径用 C 层 `vm_array_new` 建新数组后**裸存**进槽位——与 queue 相反，新数组 GC 托管（is_owned=0）无悬垂风险，但被替换的旧数组槽位计数不 -1 → **纯泄漏**（旧数组计数永 ≥1 不释放）。同一函数里 `vm_table_list_append` 每次 grow 都触发一次。
- 修复点（文件/模块）：`csimple_lang/src/vm/system_method_call/table_system_method.c` 的 `vm_table_store_array` 补协议：签名加 `VM* vm`；`vm_obj_is_owned` 判定后 `vm_object_retain_saturated`（+1 新数组，C 层数组 no-op），再 `vm_mem_arc_release`（-1 旧数组）；唯一调用点（grow 分支）同步传 vm。
- 如何快速复现：泄漏无可见症状，靠审计定位。
- 如何快速验证：ProjectTest 全量回归 10/10 轮全绿（~6.7s/轮），TableTest 全部 10 个子用例（testBasic/testTypedGet/testColumnOps/testClone/testMergeJoin/testQuery/testStats/testCsv/testPreviewIter/testCsvFacade）输出与注释预期逐项一致。
- 预防建议：①同族修复完成度检查清单更新为 queue/stack/set/table 四处——**再新增容器 C 侧 `vm_*_store_*` 辅助函数时，直接拷 queue 修复版模板**；②审计剩余结论存档：linkedlist（Node 仅非空构造器→is_owned=0→GC 兜底，安全）、json/memory（存值均为 C 层自建托管对象，安全）、tree（**潜伏缺陷未修**：TreeNode 有空构造器可被认领 owned，`vm_tree_store_obj_member` 40+ 处裸存，用户 `TreeNode<T>()` + `tree.attach(n)` 即悬垂，与 queue 挂死同族，触发条件依赖空构造器路径）。

### [2026-09-22] SBA P3 回收证据零命中：非记账缺陷，探针批量小于存量 chunk 空闲容量致混布，判据"整 chunk 全页空闲"无法满足
- 现象：SBA P3 验收时探针 v2（两批各 2000 对象批量持有→整批丢弃→强制 collect）对象生灭完全正常（collectFreed≈2008/2010、objectsAfter=750、cycles=2、exit=0），但 VM.txt 始终无 `[SBA] reclaimed N chunks to the OS`——reclaim 判据从未命中。
- 首个异常阶段：VM 运行期（`sb->used` 记账链静态复核自洽：alloc++/free--/reclaim 判据/grow-carve 全对，问题不在实现）。
- 根因（链）：
  1. 插桩取证（Phase 4.5 target==current 指针对比）先排除"SBA 实例分裂"假设——路由层与 GC 用同一实例；
  2. 插桩逐 chunk 普查 `pages=%u livePages=%u`：3 个存量 chunk 布局 `2/1`、`16/2`、`16/16`——**模块常驻 ~750 对象散布钉页 + 存量 chunk 尚有大量空闲页**；
  3. 探针批量 2000 对象全被存量空闲页吸收，**没有开辟新 chunk**，因此不存在"纯 batch 的全空闲 chunk"——判据（design §3.5：chunk 整体可回收 ⇔ 每个已切页 `sb->used==0`）逻辑正确但负载形态不满足。
- 修复点（文件/模块）：非 VM 代码缺陷——探针 `test/SBAProbe/SBAProbe.sp` 批量 2000→20000（远超存量 34 页容量），SBA 被迫为 batch 开辟 ~15 个纯 batch chunk，整批死亡即整体回收。
- 如何快速复现：修复前批量 2000 跑 SBAProbe，VM.txt 无 `[SBA] reclaimed` 行；插桩行可见 `reclaim scan: pages=2 livePages=1` 等混布证据。
- 如何快速验证：批量 20000 跑 SBAProbe，VM.txt 出现 `[SBA] reclaimed 9 chunks to the OS` + `reclaimed 7 chunks`（两批可重复命中）；B 变体（`/DVM_SBA_DISABLE=1` 单独树）同探针 stdout 与 A 逐行一致（0 行为差异）、`[SBA]` 行 count=0（reclaim 彻底 no-op）。
- 预防建议：①**设计判据的"可触发条件"必须与探针负载形态匹配**——chunk 级整回收需要"工作集整体生灭且规模超过存量容量"的负载，验收探针批量级应估算存量空闲页数（本机模块加载后 ~34 页 ≈ 常驻 750 对象 + 空闲）再放大一个量级；②插桩指针对比（target==current）是排除"多实例分裂"最快的一步，先于逐块普查；③VS 生成器树的**输出目录硬编码在 CMakeLists（build/\<Config\>/bin），任何第二构建树（build-b 等 A/B 对照树）的链接产物都会覆盖主树 exe**——A/B 对照做完必须重编主树并跑行为验证恢复（本次已执行：恢复后 reclaimed 行复现）。
- 附带记录：①本机构建入口为 `scripts\cmake-build-msvc.ps1`（VS 生成器 + MSBuild + cl.exe，无需 vcvars/clang/ninja）；`scripts\cmake-build-clang.ps1` 需装 LLVM clang，本机不可用；②P2 阶段 25 模块 A/B 回归结论不受影响（当时 B 树独立、未覆盖主树）。

### [2026-09-22] P4 TLSF 两个落地坑：新增 cvmFunction 漏 .h 的 SLVM_FUNC_EXPORT 声明 → 运行时静默 exit 1；CMake 缓存变量未转编译宏 → VM_TLSF_ARENA_SIZE 无效
- 现象：① TLSFProbe A 变体（TLSF off 默认构建）打印 "tlsf: disabled (host default)" 后第一个 `Memory.tlsfUsedBytes()` 调用进程即 exit 1，无任何错误输出；② C2 变体（`-DVM_TLSF_ARENA_SIZE=65536`）配置后探针 `tlsfFreeBytes()` 仍 ~1MiB（1039888），arena 尺寸没生效。
- 首个异常阶段：VM（system method 动态符号解析 / 构建配置透传）。
- 根因：① `Core.jsonc` 每条 systemCalls 的 `cvmFunction` 是 C 符号名，C VM 装载程序集时经 `vm_system_func_dynamic_register` → `vm_sysfunc_resolve_symbol` → `vm_sysfunc_resolve_in_self`（GetModuleHandleExW + GetProcAddress）按名解析，**符号必须出现在 exe 导出表**——靠 `memory_system_method.h` 声明上的 `SLVM_FUNC_EXPORT`（`__declspec(dllexport)`，define.h:148）；新 4 个 tlsf getter 只写了 .c 实现漏了 .h 导出声明，GetProcAddress 失败即静默崩（exit 1 无输出）。② `cmake -D<VAR>=...` 只写入 CMakeCache，**不会自动成为编译宏**，须显式 `add_compile_definitions(VM_TLSF_ARENA_SIZE=${VM_TLSF_ARENA_SIZE})` 才进 C 代码。
- 修复点（文件/模块）：① `csimple_lang/src/vm/system_method_call/memory_system_method.h` 补 4 条 `SLVM_FUNC_EXPORT int32 vm_sys_mem_tlsf_*(...)` 声明；② `csimple_lang/CMakeLists.txt` 将 `VM_TLSF_ARENA_SIZE` 改为 `set(... CACHE STRING)` 并在 `VM_TLSF_ENABLE` 分支内 `add_compile_definitions()` 透传。
- 如何快速复现：① 新增 cvmFunction 只写 .c 实现不写 .h 导出声明，跑任何调用该方法的 .sl；② `cmake -DVM_TLSF_ENABLE=ON -DVM_TLSF_ARENA_SIZE=65536` 构建后跑 TLSFProbe 看 `tlsfFreeBytes()`。
- 如何快速验证：① 重编后 TLSFProbe A 变体全绿（disabled 分支 4 getter 全 0、alloc/free 回退 ok、exit=0）；② C2 重配重建后 `free=56848`（64KB arena 生效），超 arena 请求全部回退 base 堆且 arena 计数不动。
- 预防建议：① 新增系统方法（R7 三处同步：.h+.c 实现、.sl 声明、Core.jsonc 绑定）之外，**凡 cvmFunction 对应实现必须在 .h 声明处带 SLVM_FUNC_EXPORT**——运行时 exit 1 且无任何输出时先查 exe 导出表（dumpbin /exports 或 grep .h 是否漏导出声明）；② CMake 想把调参暴露给 C 代码，一律 `option()/set(CACHE)` 与 `add_compile_definitions()` 成对写，只 `-D` 不透传必不生效；③ TLSF on/off 变体验证均在主树重配置完成，跑完必须 `-DVM_TLSF_ENABLE=OFF` 恢复 + 重建 + 复跑 SBAProbe/TLSFProbe 双基线（本次已执行，双 EXIT=0）。

### [2026-09-22] P5-2 L0 档开机 0xC0000005：TLSF v3.1 control_t ~6KB，4KB arena 装不下，control_construct 向 arena 尾后泼溅 ~2KB 踩相邻静态数据
- 现象：P5-2 四档分档（`VM_SBA_PROFILE=L0/F4/H7/T1`）验证时，L0 档（原设计 arena 4KB）构建后开机即 0xC0000005 访问违例；F4/H7/T1 全部正常。
- 首个异常阶段：VM 启动（静态初始化/boot 期，尚未进入用户代码）。
- 根因：TLSF v3.1 的 `control_t` 元数据（位图+空闲链表头）自身约 6KB；L0 档 arena 仅 4KB，`tlsf_create`→`control_construct` 把 control 结构写进 arena 时**向 arena 尾后泼溅 ~2KB**，踩坏相邻静态变量 → 开机即崩。取证：`sl_crash-win32.c` SEH 增强捕获崩溃 RVA + 符号定位，锁定越界写发生在 control 初始化路径（"先插桩后修复" R12 流程的 SEH 版）。
- 修复点（文件/模块）：① `csimple_lang/CMakeLists.txt` L0 档 `VM_TLSF_ARENA_SIZE=8192`（4KB→8KB，注释记录根因）；② `vm_tlsf.c` 包装 `vm_tlsf_instance()` 运行时用真实 `tlsf_size()` 复核 arena，不足回退 `base_malloc0`（对其他目标的双保险）；③ 设计文档 §9 预算表 L0 行修正（TLSF 区 4KB→**8KB** 并注明下限依据）。
- 如何快速复现：`-DVM_SBA_PROFILE=L0`（arena=4096）构建，任何模块 `run` 即崩。
- 如何快速验证：四档矩阵闭合——L0 崩溃消失 ✓、F4 语义对齐 ✓、H7 正常 ✓、T1（H7×4 主机测试档）编译/运行 ✓。
- 预防建议：① **引入第三方分配器先量化其元数据自身占用**（`tlsf_size()` 一类 API）再定 arena 下限，"预算=数据"是错的一半；② 最小档（边界条件档位）必须实测 boot，不能只跑主力档；③ SEH 崩溃取证（RVA→符号）保留为 C VM 崩溃类问题标准手段，配合 R12 先取证后改逻辑。

### [2026-09-22] P5-3d OOM catch 体静默（C catch hit 永不出现、EXIT=1）：vm_eval_push_string 分配失败静默降级为 0 字节 null 槽 + OOM 路径无 GC 重试；修复 = GC-on-catch/finally + GC 根集补异常值
- 现象：OOMProbe（T1 档）三阶段 catch 全不命中——phase A 仅 "A finally ran"（finally 有、catch 无），phase C 头打印后 VM 终止 EXIT=1；但 throw 链取证显示 CAUGHT 跳转正常、catch 体指令完整派发（disp 桩 idx 57–63 全部执行、running=1）。
- 首个异常阶段：VM 运行期（异常恢复后的 catch 体执行）。
- 根因（链）：
  1. disp 桩（逐指令 idx/op/sp）裁决：catch 体**有派发**（推翻"vm_run 不迭代/调度器不交还控制权"两假设），真凶是"指令执行但输出静默"；
  2. 决定性证据 idx=60 LoadString 后 **sp=0**：`vm_eval_push_string`（runtime_slots.c）在 `vm_object_new_string` 返回 NULL 时调 `vm_eval_push_null`——null 槽 **0 字节**，sp 不变——**静默降级**，无任何日志；
  3. 分配为何失败：NewObject/NewArray OOM 路径（vm_runtime.c）直接 `vm_runtime_throw_memory_oom` **无 GC 重试**；GC 仅按需（阈值）触发，holdBatch 帧展开后 ~777 对象已不可达但永不回收 → catch 体全程在耗尽池上分配；
  4. 连锁：println 弹空栈无输出 → `e.toString()` 再失败 → 第三次 OOM 无 try 区间可命中 → UNCAUGHT → pending → VM 终止。
- 修复点（文件/模块）：
  1. `runtime_trace.c` `vm_execute_throw`：CAUGHT 与 FINALLY-only 两个恢复点、跳转前 `vm_gc_collect(vm)`——展开刚丢弃 throw 点到 catch 之间所有帧，此刻回收不可达对象，catch/finally 体才有内存可运行（`vm_gc_collect` 自带 ARC/重入门卫，空回收为 no-op）；
  2. `vm_gc.c` 新增 `vm_gc_mark_exception_roots`：根集补 `try_stack[i].exception_value`（`has_exception=1`，异常穿越 finally 期间的唯一引用）与 `vm->pending_exception`（跨 VM 待传播）。**这不是防御性设计而是必需**：`vm_mem_maybe_collect` 在对象池/LOS 登记路径按阈值自动触发——finally 体内任意分配都可能回收异常对象 → EndTry 重抛悬挂（预存隐患，本次顺带闭合）。
- 如何快速复现：`-DVM_SBA_PROFILE=T1` 构建，**cwd=csimple_lang\build**（`..\..\simple_language` 相对路径才解析到本仓库）运行 OOMProbe；修复前 EXIT=1、stdout 止于 phase C 头。
- 如何快速验证：修复后 stdout 出现 `C catch hit` + 异常内容 + D 阶段全绿，EXIT=0；disp 对照表：idx=60 LoadString 后 sp 0→8、idx=63 toString 由"戛然而止"→成功输出。回归：DebugMerged 重跑 SBAProbe（双 batch 各 ~20.7k 对象→collect 回落基线 757，EXIT=0）。
- 预防建议：① **分配失败路径禁止静默降级**——push 类 API 失败至少要 one-shot 可观测；"0 字节 null 槽"让 sp 不变、后续指令空转，从输出侧几乎不可定位，本次靠逐指令 disp 桩（idx/op/sp 三元组对照）才裁决；② **异常恢复点必须考虑资源恢复**——展开丢弃的帧可能持有大量对象，恢复执行前先回收，否则 catch 体在耗尽池上必然连锁失败；③ **GC 根集演进要跟随"新增持有对象引用的 VM 状态字段"**（try 帧副本、pending、channel…每加一处同步登记 GC_DESIGN §3.4）；④ 取证运行注意 cwd——错误 cwd 下相对路径会解析到**另一个旧仓库**的同名模块，得到完全无效的取证（本次踩过）；⑤ 插桩结论要形成修复前后对照表再撤桩（p5-5 善后）。
- 附带记录（p5-3e）：SBA 耗尽类插桩改进程级 one-shot 门控（跨 VM 首次打印后续抑制），刷屏 ~30 组→1 组；disp 桩为临时取证件。

### [2026-09-22] 协程栈 GC 根集缺口闭合（GC_DESIGN §3.4.1 登记项）：新增 vm_gc_mark_coroutine_roots；插桩定位 String 标量包装 member[0] 裸 char* 被当 VMObject* trace 致 0xC0000005
- 现象：① 挂起协程仅被其帧/私有栈引用的对象在 collect 后被 sweep，resume 即 UAF（CoroGCProbe S1 在 awaitTask 死于 err -64）；② 实现协程根扫描后 NestedGCProbe 又现 0xC0000005，崩溃前最后插桩 `[GCT] etype=18 cnt=1 i=0 ref=0x...`，AV read 0xFFFFFFFFFFFFFFFF。
- 首个异常阶段：VM（GC MARK / trace 阶段）。
- 根因（链）：
  1. 缺口：`vm_gc_mark_stack_roots` 只扫 `vm->stack`（当前协程）；挂起协程的私有栈块、帧链行、saved_try、ret_values 不在任何根集 → 仅被挂起协程持有的对象被回收；
  2. 崩溃：同一 `EVMType_String` 行存在**两种 slice 语义**——帧行（VMRuntimeObject）的 slice 内联 VMObject\*（语言级 string 引用），而 **String 标量包装 VMObject 的 member[0] slice 存裸 char\***（`vm_object_new_string` 直写绕过 VMRuntimeValue）。`vm_runtime_object_read_gc_reference` 的 String 分支把包装对象的 char\* 当 VMObject\* 返回，mark_gray 将字符串字节当对象头解析——位域恰好命中 WHITE+large 时直接 trace → 读 0xFFFFFFFFFFFFFFFF → AV。共享空串的字节图案恰好不触发、反复"成功" trace，掩盖问题，崩溃仅在特定字符串字节下出现。
- 修复点（文件/模块）：`csimple_lang/src/vm/memory/vm_gc.c`：① 新增 `vm_gc_mark_coroutine_roots` 接入 `vm_gc_collect`（遍历 all_coroutines：非当前协程私有栈快照复用参数化辅助 `vm_gc_mark_stack_block`、自 current_frame 沿 caller_link 扫六组 caller_/callee_ 行 + 帧级双 pending、saved_try 异常值、ret_values、协程级 pending；当前协程的栈快照/saved_try 特意跳过——coro 侧快照仅切出时同步，扫它复活已弹出临时量，SBAProbe 曾每周期 +2/+3 死串）；② `vm_gc_trace_members` 入口跳过 `EVMType_String && vm_object_is_scalar_wrapper`（语义判定放知道"行主"的层，而非 read_gc_reference 内判——它只见行不知行主）；③ `vm_gc_mark_stack_block` 参数化 + NULL 槽 0 字节语义修正（原内联 switch 对 NULL 槽跳 4 字节，槽深全错位）。`runtime_object.c`：`vm_runtime_object_read_gc_reference` 五分支行引用解析并导出。
- 如何快速复现：修复前跑 CoroGCProbe（S1 UAF）；或嵌套协程挂起 + 特定字节图案字符串 + 洪泛 alloc 触发 collect（NestedGCProbe 崩溃形态）。
- 如何快速验证：CoroGCProbe S1/S2/S3 全 OK 且 DebugMerged/ProfileT1 双配置数字一致（S1 freed=6 after=758；S2 freed=13 after=761；S3 freed=12 after=764）；NestedGCProbe@DebugMerged OK/OK（freed=20003）；SBAProbe（freed=20006/20009）、OOMProbe（ProfileT1 恢复链完整）基线不变。
- 预防建议：① **新增 VM 状态字段凡持有对象引用必须同步登记 GC 根集（GC_DESIGN §3.4）**，probe 必须覆盖"挂起方持引用 + 另一方触发 collect"形态；② **"同一 etype 两种 slice 语义"是 C VM 高危区**（frame row vs 标量包装 member row），逐分支审 `vm_object_new_*` 直写 slice 的所有路径；③ 插桩取证优于纯推理（R12）：GCR/GCT 双桩日志链 + "共享空串反复成功 trace"的反常现象是定位关键，纯静态推理曾把方向带偏到短 slice 越界/复用残留；④ probe 断言失败时先 diff 期望值本身（NestedGCProbe 曾比较串写 "ROOT-KEEP" 而实为 "MID-KEEP"，恒 FAIL 与 VM 无关）。
- 附带记录：`compile` 不带 `-e ir` 不更新 module.json 本会话第 5 次复现（重编译后行为不变 + 新打印不出现 → grep module.json 新串 0 命中即实锤旧产物）。

### [2026-09-23] mimalloc 接管版统计系统方法全 0：mi_stats_get 校验 size/version 头部，零化结构体直接拒绝
- 现象：VM_MIMALLOC_ENABLE=ON 构建下 Memory.mimallocEnabled()=true、mimallocVersion()=30500 正常，但 5 个统计 getter（Current/Peak/Requested/Committed/Reserved Bytes）全返回 0；而 MIMALLOC_SHOW_STATS=1 退出打印显示 67 MiB 真实流量。
- 首个异常阶段：VM（vm_mimalloc.c 统计桥接）。
- 根因：mimalloc v3.5 的 mi_stats_copy（third_party/mimalloc/src/stats.c）要求入参 `stats->size == sizeof(mi_stats_t)` 且 `stats->version == MI_STAT_VERSION(5)`，否则返回 false 且不填充。vm_mimalloc_stats() 原实现 memset 清零后直接调 mi_stats_get——size/version 均为 0，校验失败静默返回 false，所有 getter 走 fallback 返回 0。
- 修复点（文件/模块）：`csimple_lang/src/vm/memory/vm_mimalloc.c` vm_mimalloc_stats()——memset 改为 `mi_stats_init(stats)`（= memset + size/version 头部初始化，mimalloc-stats.h 内联）。
- 如何快速复现：DebugMi 树（`-DVM_MIMALLOC_ENABLE=ON`）跑 `test/MiProbe`，统计行全 0 即中招。
- 如何快速验证：重编后 MiProbe 输出 baseline current≈25.5 MB（VM 自身运行时结构在 mimalloc 上的直接证据）、alloc 1M+4M 后 current 精涨 ~5 MiB、free+collect 后回落 baseline；默认树（VM_MIMALLOC_DISABLE）保持全 0 语义。
- 预防建议：① 凡「开关量正常但读数恒 0」先查 API 入参头部契约——版本化结构体常以 size/version 双字段防 ABI 错配，零化 ≠ 初始化；② mimalloc 统计字段布局以 `third_party/mimalloc/include/mimalloc-stats.h` 为准（mi_stat_count_t{total,peak,current}；malloc_requested 也是 count 结构而非裸 size_t）。

### [2026-09-23] VM_MIMALLOC_DISABLE 双头 fallback 方向分裂 + cmake --build 跨环境触发 cache 重建致 DebugMi 树静默漂移
- 现象：① 用户质疑"禁用构建是否真没编 mimalloc"——查 compile_commands 发现默认树（VM_MIMALLOC_ENABLE=OFF）编译命令里**没有任何** `-DVM_MIMALLOC_DISABLE`，"禁用"只靠头文件 fallback，构建日志层无证据；② vm_mimalloc.h 的 fallback 曾被改为 `#define VM_MIMALLOC_DISABLE 0`（与 mem.h 的 fallback=1、与自身注释 "1 (default)" 三方矛盾）——若以此重编默认树，vm_mimalloc.c 会拿到"启用"视图（`#include <mimalloc.h>` 失败 C1083，因 OFF 树无 mimalloc include 路径），而 mem.c 等其余 TU 走 mem.h fallback=1 保持 libc——同进程宏视图分裂；③ 对 DebugMi 树 `cmake --build` 时环境编译器与 cache 不一致（原 cache 为 clang/preset 配置，当前 vcvars 只有 cl），CMake 报 "changed variables require cache delete" 并**静默重配置**：VM_MIMALLOC_ENABLE 丢回 OFF、CMAKE_RUNTIME_OUTPUT_DIRECTORY 丢回源码默认 `build/$<CONFIG>/bin`——DebugMi 树产物串写进 build\Debug\bin 覆盖默认树 exe，ON 树名存实亡。
- 首个异常阶段：构建（CMakeLists.txt mimalloc 段 + 双构建树管理）。
- 根因：① 双头各自 `#ifndef VM_MIMALLOC_DISABLE` fallback（mem.h=1 / vm_mimalloc.h 一度为 0）是 include 顺序敏感设计，无 CMake 层统一钉死；② CMakeLists 只在 ON 分支 `add_compile_definitions(VM_MIMALLOC_DISABLE=0)`，OFF 分支不注入；③ CMAKE_RUNTIME_OUTPUT_DIRECTORY 默认值是**源码绝对路径** `build/$<CONFIG>/bin`（CMakeLists:16-17）而非 `${CMAKE_BINARY_DIR}/bin`，任何构建树 cache 重建后产物都会串到 build\Debug\bin；④ `cmake --build` 遇编译器环境变化会删 cache 重配置，configure 期命令行传入的 cache 变量（ENABLE=ON、输出目录）全部静默丢失。
- 修复点（文件/模块）：`csimple_lang/src/vm/memory/vm_mimalloc.h` fallback 改回 1（与 mem.h 一致）；`csimple_lang/CMakeLists.txt` mimalloc 段新增 else 分支 `add_compile_definitions(VM_MIMALLOC_DISABLE=1)`——两种形态的编译命令均显式携带开关；DebugMi 树删除后用 cl+Ninja 重新 configure，显式钉死 `-DCMAKE_C_COMPILER=cl -DCMAKE_MAKE_PROGRAM=<scripts/tools/ninja.exe> -DVM_MIMALLOC_ENABLE=ON -DCMAKE_RUNTIME_OUTPUT_DIRECTORY=<…>/build/DebugMi/bin -DCMAKE_BUILD_TYPE=Debug`。
- 如何快速复现：对 DebugMi 树（原 preset 配置）在纯 vcvars 环境跑 `cmake --build build\DebugMi`，观察 "You have changed variables…CMAKE_C_COMPILER" 警告后 cache 被重建、产物写到 build\Debug\bin；或 grep `build\Debug\compile_commands.json` 无 `VM_MIMALLOC_DISABLE` 命中即旧版 OFF 树无编译层证据。
- 如何快速验证：两树 compile_commands 全量统计——Debug 树 315/315 TU 带 `-DVM_MIMALLOC_DISABLE=1`，DebugMi 树 353/353 TU 带 `=0`；二进制特征扫描（Debug exe "mimalloc" 串 8 处 vs DebugMi 998 处）；MiTest 双树重跑 disabled 11/11 + takeover 13/13 全 PASS，套件全量 0 FAIL。
- 预防建议：① **编译开关必须由 CMake 层双向显式注入**（ON 注入 0、OFF 注入 1），头文件 fallback 仅作双保险且多头的 fallback 值必须一致——验证开关状态以 compile_commands.json 为准，不以头文件注释为准；② **CMAKE_RUNTIME_OUTPUT_DIRECTORY 应基于 `${CMAKE_BINARY_DIR}` 而非源码固定路径**（当前源码默认 `build/$<CONFIG>/bin` 会让多构建树产物互串，暂以 configure 显式传参规避）；③ 多构建树（Debug/DebugMi）build 前先核对 cache 三要素（ENABLE/COMPILER/输出目录），跨环境 `cmake --build` 出现 "changed variables" 警告即停手，不要让静默重配置发生；④ DebugMi 树现改为 cl 构建（与默认树同编译器，纯 ON/OFF 对照），原 clang preset 需 LLVM 安装后才可复用。

### [2026-09-28] mimalloc 常开接管（CMake 钉死 VM_MIMALLOC_DISABLE=0）源码清单三坑：free.c/page-queue.c 只能内联、平台 prim 必须走 prim/prim.c 调度器、prim-tls.c 独立必编
- 现象/任务：用户要求 mimalloc 常开接管全堆——保留 `#if VM_MIMALLOC_DISABLE` 宏双分支代码，但编译期由 CMake 把宏钉死为 0（vendored third_party/mimalloc v3.5 无条件编入、`base_*` 路由 `mi_*`、探针上报真实数值；与 TLSF 静态 arena 互斥，FATAL_ERROR 守卫保留）。初次源码清单按目录直觉罗列 `src/*.c` + 平台 `prim/windows/prim.c`，构建三连败。
- 关键事实（mimalloc v3.5 源码组织）：
  1. **C1189 守卫文件只能被内联**：`free.c` 由 alloc.c:22 `#include`、`page-queue.c` 由 page.c 内联（`alloc-override.c` 只属 override 场景）——上游靠自己的 CMake 按构建模式选文件，静态接管态三个都不能进清单。
  2. **平台 prim 不能直编**：LNK1120 12 个符号中 `_mi_is_redirected/_mi_allocator_init/_mi_allocator_done` 的家是 `prim/prim.c` 调度器——它 `#if defined(_WIN32) #include "windows/prim.c"` 自行分发，且 `#ifndef MI_PRIM_HAS_ALLOCATOR_INIT` 兜底块提供这三个符号（windows/prim.c 里同名定义被 `MI_SHARED_LIB` 守卫，静态链不激活）；直编 prim/windows/prim.c 则调度层兜底缺失。
  3. **prim-tls.c 是独立编译单元**（无人 include 它）：其余 9 个符号 `_mi_tls_slots_init/_mi_tls_slots_done/_mi_theap_default|cached_slot/_mi_theap_default|cached_expansion_slot/_mi_theap_default|cached_set/_mi_thread_id` 全在此，必编。
- 修复点（文件/模块）：根 CMakeLists.txt——`VM_MIMALLOC_SOURCES` = 17 个 src/*.c（不含 free.c/page-queue.c）+ `prim/prim.c` + `prim/prim-tls.c`（平台由 prim.c 分发，不写 if(WIN32)）；`add_compile_definitions(VM_MIMALLOC_DISABLE=0)`；per-file `/W0`(MSVC)/`-w`(其他) 豁免零警告预算；双 target（dll+exe）补 `third_party/mimalloc/include` 与 psapi/shell32/user32/advapi32/bcrypt。mem.h/mem.c 恢复宏双分支（默认回退 1 兜底不编 mimalloc 的假设树），vm_mimalloc.{h,c} 真实实现 + DISABLE=1 桩。
- 如何快速验证：`scripts\build-vm-win.ps1 -Config Debug` exit 0（dll+exe 双链接通过）；MiProbe.module.json exit 0（enabled (host takeover)/version 30500/alloc 1M+4M 后 current 增长/free+`mimallocCollect(true)` 后释放判定 ok）；ProjectTest exit 0 且 9 FAIL 与两平台基线一致（P5 调试帧 7 + P6 trace 2），无新增回归。
- 续（同日，VS vcxproj 树同步启用）：`project/vs/vm_lib/csimple_lang_lib.vcxproj` 四个配置注入 `VM_MIMALLOC_DISABLE=0`、`AdditionalDependencies` 补同 5 个系统库、19 个 mimalloc 源从 `ExcludedFromBuild` 改为编入并 per-file 关警告（free.c/page-queue.c/alloc-override.c/prim/windows/prim.c 四个守卫文件保持排除）。**新坑：MSVC vcxproj 的 `<WarningLevel>` 枚举没有 `Level0` 值**——写 Level0 直接 `error : 元素 <WarningLevel> 具有无效值` + MSB6011，正确值是 `TurnOffAllWarnings`。验证：MSBuild Debug|x64（产 dll）与 Release|x64（产 exe）均 exit 0，Release exe 跑 MiProbe 同样 enabled/30500/计数移动。注意 vcxproj 产物与 Ninja 树同目录（`build\{Debug,Release}\bin\`，AGENTS.md §7 双树坑），测完 vcxproj 版需重跑 `build-vm-win.ps1` 恢复。
- 预防建议：① 接第三方库先读它自身构建脚本的源文件清单与分文件模式，带 "should be included from" 守卫的文件绝不能独立编译；② 链接期缺符号先用 grep 定位符号定义归属再补编译单元，不按目录名猜；③ 宏"常开"语义 = 编译期定义 `=0` 注入而非删代码——双分支结构留着，兜底不编该库的假设构建树；④ 改 vcxproj 专有字段前先查 MSBuild 枚举合法值（WarningLevel/ExternalWarningLevel 等），不要照 CMake 的 `-w`/`/W0` 直译。

### [2026-10-02] mimallocCollect 弹参 kind 拒收：私有弹参只认 INT32/INT64，BOOL 槽被拒致 CallSystemMethod failed（"测试全 PASS"假阴性警示）
- 现象：用户定位 `vm_sys_mem_mimalloc_collect` 执行时"从栈上弹出 kindType=12"报错；VM.txt 持续出现 `CallSystemMethod failed. id=1463228328`（共 2 处），但 MiTest 13 项"全 PASS"且 exit 0——修复前先解开了"未修却全绿"悖论。
- 假阴性机理（重要教训）：① CallSystemMethod 分发器失败路径**非致命**——handler 返回 FALSE 且无 sys_throw 时仅记一条 Assert 日志即 break，**不做任何栈修复**（vm_runtime.c 分发 handler）；② 包装方法 `mimallocCollect_1_Core.Boolean` 方法体为 `[Nop]→[LoadArgument]→[CallSystemMethod]→[Label]`，调用后立即返回、帧栈复位，残留 bool 槽被掩盖；③ MiTest 断言感知不到 collect 未执行（free() 已让 current 计数下降，"collect safe"项只查不崩）→ **"测试全绿"≠"功能正常"，必须 grep VM.txt 的 failed/Assert 标记**。
- 根因：`vm_sys_mem_mimalloc_collect` 用文件内私有弹参 `vm_mem_native_pop_i32_arg` 取实参，该 helper 只收 VM_STACK_SLOT_INT32/INT64 两种 kind；SL 声明 bool 实参的槽 kind 为 BOOL(17) → 弹参失败返回 FALSE。用户观测的 kind=12（FLOAT16_Brain）来自旧中间态 exe——[PROBE-KIND] 插桩实证**当前构建实际弹出 kind=17（BOOL，正确）**，类型管线无恙，唯一病灶是弹参 helper 过严。R7 四处同步核验无缺漏（Core.jsonc 声明 params=["bool"] / Memory.sl 包装 / C 实现 / 注册均一致）。
- 修复点（文件/模块）：`csimple_lang/src/vm/system_method_call/memory_system_method.c`——`vm_sys_mem_mimalloc_collect` 改用共享全标量弹参 helper `vm_try_pop_scalar_or_ptr_as_i32`（R5；BOOL 按 0/1 归一取值，容忍全标量 kind），并显式补 `#include "system_method_helpers.h"`。
- 波及面审计：全部 17 个带 bool 参数的系统方法 C 实现逐一核对（Console/Log×2/Isolate/ByteBufferWriteBool/BinaryLink/Json×2/Table×4/Component×4）——其余 16 个均已用宽容弹参（`vm_try_pop_scalar_or_ptr_as_i32` / `vm_sys_string_pop_int_arg` / `vm_try_pop_runtime_value`+EVMType_Boolean），**mimallocCollect 是唯一踩坑者，无连带修复**。
- 如何快速复现：旧版 VM 以 cwd=csimple_lang 运行 `csimple_lang.exe run --no-banner <ProjectTest2>/ProjectTest.module.json` → VM.txt 出现 `CallSystemMethod failed. id=1463228328` ×2 且 MiTest 仍全 PASS（假阴性）。
- 如何快速验证：修复版重跑同一命令 → 该失败行消失、MiTest 13/13 PASS exit 0（PROBE-KIND 临时日志曾实证 pop kind=17，闭环后已移除）。
- 预防建议：① 系统方法弹参一律优先用 `system_method_helpers.c` 共享 helper（全标量容忍），勿新写只收个别 kind 的私有弹参（R5）；② "全 PASS"运行仍须 grep VM.txt `failed`/`Assert` 标记——分发器失败非致命且可被帧复位+宽松断言双重掩盖；③ 弹参类排障按 R12 先插桩取证实际 kind 再定性——本例用户观测 kind=12 与当前构建实际 kind=17 不同，靠 PROBE-KIND 插桩闭环，避免误改类型管线。
- 附带记录（a5）：当前运行 VM.txt 头部 10 条 `system call not registered in cvm` 警告查明为**预期噪音**——`Std.jsonc` 8 个 SystemList* 占位声明 + `Core.jsonc` SystemCallExternalFunction 均 `"cvmFunction": ""` 刻意留空（渲染后端逃逸口/历史占位，全仓无调用点），装配期两级注册（符号解析→插件路径）按设计失败后告警，运行期从未触发，无需修复。

### [2026-10-13] runtime 优化期 teardown 0xC0000005：`base_chars_dup` 缺原型 → C 隐式声明按 int 返回 → 64 位指针截断成垃圾值
- 现象/任务：C VM runtime 优化（RUNTIME_OPTIMIZATION_PLAN.md P0-2 调用绑定表）落地后 BenchMark 主体全部跑完、输出正确，但进程 teardown 阶段稳定 0xC0000005（同 rip 同栈 100% 复现），SEH dump 定位不到符号（Release 无 pdb）。
- 排障路径（R12 证据先行）：先假设 vm_destroy 帧池/行池收尾出错 → vm_destroy 各释放步骤加 VD7A~VD7E 标记日志 → 全部走完（帧池 23/行池 64 正常回收）→ 日志尾行停在 `P02BC clear` 附近 → 怀疑 `vm_call_binding_clear` → 释放循环逐条打印条目指针 → **i=0 即 `name=FFFFFFFF89C1DA10`**：高 32 位全 F、低 32 位恰为合法堆址低半（0x89C1DA10 符号位为 1）——教科书级「32 位截断 + 符号扩展」指纹。
- 根因：`runtime_method.c` 新增的 `vm_call_binding_add` 调用 `base_chars_dup` 但未 `#include "../../../base/chars.h"`。C99 下无原型调用退化为隐式声明「返回 int」，64 位进程里 `char*` 返回值经 RAX 传递，调用方却按 int 截断 EAX 再符号扩展——堆地址低 32 位符号位为 1 时得到垃圾指针；后续 `base_free(垃圾指针)` 即崩。注意本仓 MSVC 构建对该场景零警告（C4013 类提示默认关闭），clang -Wall 亦不报——**缺原型在两个工具链下都静默**。
- 修复点（文件/模块）：`runtime_method.c` include 区补 `base/chars.h`（一行）。教训通用化：**`src/base/` 任何工具函数（mem/chars/hash_map…）被新文件引用时，必须 include 对应头**，勿依赖「别处已包含」的传递可见性——传递链一变就退化成隐式声明。
- 如何快速验证：修复后 BenchMark EXIT=0；`build\logs\VM.txt` 尾部 `P02BC clear count=3712` 完整走完无 CRASH。附：C VM 运行日志固定落在 **CWD 相对** `./Logs/VM.txt`（仓库根跑则 `csimple_lang\build\logs\VM.txt`，见 log.c `s_log_path`），排障时别在 out/export 下找。
- 预防建议：① 崩溃目标地址/寄存器值呈「高 32 位全 F + 低 32 位像堆址」形态 → 第一反应查指针截断（隐式声明/强转 int/int32 中转），而非野指针；② 新文件引用 base 工具函数先补自身 include（自包含纪律）；③ SEH dump 无 pdb 时，「逐段标记日志 + 最后一条打印点」是缩小崩溃区间的最快手段——比逐条二分注释代码快且不改变代码布局。
