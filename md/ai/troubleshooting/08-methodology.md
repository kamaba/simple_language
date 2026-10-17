# 复盘分片 08 · 误诊与验证方法论

> 收录：假绿、陈旧 obj、环境漂移、性能对照纪律、疑似回归误报等方法论复盘。
> 路由索引与追加规则见 [故障排查流程 §6](../故障排查流程.md)；新复盘按域追加至本片末尾。

### [2026-09-06] C VM 读写路径重构（pop_top_write 去掉 RuntimeValue 中间层）后疑似 HashSet 回归，实为测试方法误报
- 现象：`vm_runtime_object_try_pop_top_write` 重构（求值栈直写 runtime_object，删除 slow path 与 `_typed` 变体）后，曾被报告 testIterator `iterated count = 0`、testStringElements `toString = {null,null}` 两处回归。
- 首个异常阶段：VM（读写路径重构等价性验证）。
- 根因：重构本身等价。疑似回归未能在含全部重构 + 插桩的构建上复现（`iterated count = 3`、`toString = {apple,banana}`），ProjectTest 全部输出与重构前基线逐行一致；疑与早期测试方式有关——相对路径运行时模块引用 `../Core/Core.module.json` 解析失败（resolved: /../Core/...），SystemPrintln 未注册、测试静默失败，易被误读为回归。
- 修复点：无代码修复；验证方法修正为必须用绝对路径运行 `csimple_lang.exe f:\project\lang\simple_language\out\export\ProjectTest\ProjectTest.module.json`；VM 日志在 `<csimple_lang 根>\build\logs\VM.txt`（非 out\export\...\Logs\）。
- 如何快速复现：重构后重建 Release，绝对路径运行 ProjectTest，输出 diff 基线快照。
- 如何快速验证：179 行输出与基线逐字符一致（含 `[16ms]` 时间戳）；插桩移除后复核仍一致。
- 预防建议：C VM 改动后的烟测一律绝对路径运行并 diff 基线；排查"回归"先确认模块加载无 `skipping` 行（build\logs\VM.txt）再下结论。

### [2026-10-13] references uuid 改造后 SpecialTest 0xc0000005：非新代码 bug——结构体插字段后增量构建残留陈旧 obj，旧偏移读 `aot` 落在新布局 `at_sign_labels_count`（ABI 错位）
- 现象：`SLModulePackage` 插入 `module_reference_uuids` 字段 + references 改 `.module` 后缀/必写 uuid 改造完成后，全套件回归仅 SpecialTest 崩溃（exit -1073741819 / 0xc0000005），其余 5 套件全绿；sl_crash-win32 SEH dump 符号化到 `vm_aot_registry.c:451`（`if (!aot->enabled)`），AV 目标地址 0x23。
- 排障路径（R12 证据先行）：静态审查新代码全排除（`sl_parse_module_references` 分配/边界/零初始化正确、`module_reference_uuids` 释放链平行正确、slb 解码器本轮未执行、graph→model 所有权链干净、插件 DLL 不引用该结构体）；按 R12 在 `vm_aot_load_embedded`/`vm_aot_registry_load_from_model` 加 AOT-PROBE 插桩 → 增量重建（仅重编 vm_aot_registry.c.obj）后崩溃消失（10/10 通过），海森堡特征 → 转向构建侧取证：时间线显示头文件 `slir_assembly_data.h` 9:35 改、崩溃 exe 9:42 构建于其间；结构体偏移推算锁定根因。
- 根因：中文本地化 MSVC + Ninja 环境下头文件依赖跟踪（/showIncludes 前缀探测）失效——改 `SLModulePackage` 布局后增量构建只重编了部分 .c，`vm_aot_registry.c.obj` 仍是旧布局产物。旧代码读 `aot` 的偏移恰好落在新布局 `at_sign_labels_count`（int32+4 填充，其后 8B 即原 aot 位）；malloc0 使高 4 字节为 0，读出的"指针"值 = count 本身。**可验证预言命中**：SpecialTest.module.json 的 atSignLabel 数组长度 == 35 == 0x23 == 崩溃 AV 目标地址，精确吻合。本会话改该 .c 触发重编，顺带"修复"。
- 修复点（文件/模块）：无逻辑修复——移除探针 + **删 `csimple_lang\build\Debug` 树全量重建**（609/609）后 SpecialTest 20/20 稳定、全套件回归 6/6 exit 0。
- 如何快速复现：结构体插字段后仅增量构建（不 clean），跑引用该结构体且字段在插入点之后的模块（SpecialTest：aot 非空 + atSignLabel=35 → 崩在小地址读）。
- 如何快速验证：clean 全量重建后连跑 20 次 exit 0；全套件 `run-tests-win.ps1` 6/6 exit 0。
- 预防建议：① **凡改 `src/vm/assembly/*.h` 等公共结构体布局（插/删/换序字段），必须删对应 build 树全量重建**，增量构建在此环境不可信（已同步 AGENTS.md §7 已知坑）；② 崩溃 AV 目标是小整数（如 0x23）时优先怀疑"把 count/len 当指针解引用"，可用"字段值 == AV 地址"反查结构体错位（本例 atSignLabel=35 是现成指纹）；③ 海森堡崩溃（加日志即消失）先怀疑陈旧对象/ABI 错位再怀疑竞态；④ 时间线三件套（头文件 mtime / exe mtime / 崩溃符号所在 .c 的 obj 是否重编）是判定陈旧对象的最快证据。

### [2026-10-14] P0-2 调用绑定重开 A/B：隔日环境漂移 ±2% 差点误判「回退」——性能对照必须同环境背靠背取得
- 现象/任务：P1-C 期把 P0-2（装配期调用绑定重写，§4.4 首轮因「算术 +8% 回退」停用）重新置 1 验收。复测首轮 BenchMark 总计比 §4.4 记录明显偏慢、RegExp 全段均匀上移，表面上坐实「启用有回退」。
- 根因：**隔日环境漂移**——当日全部配置（含 rewrite=0 对照）比前日 P1-B 基准整体慢 ~2%，属机器环境日间波动，非代码效应；首轮「+8% 回退」同样混入了漂移噪声，才导致当时误停用。
- 修复点/决策：同环境背靠背复测 rewrite=1（17187/17187/17292ms）vs rewrite=0（17374/17380ms），净快 ~0.9% 且范围不重叠 → 回退消失、**置 1 保留启用**；装配统计行核验 1266 rewritten / 18 kept（98.6%，kept 为泛型/带 target_def 的预期保留）。数据与决策全文见 `csimple_lang/md/RUNTIME_OPTIMIZATION_PLAN.md` §5.3（§4.4 留有重开指针）。
- 如何快速验证：同一环境、同一二进制树，`SL_ENABLE_CALL_BINDING_REWRITE` 两个取值各跑 ≥2~3 轮背靠背对比看范围重叠性；装配统计行 `P0-2 call binding rewrite: N rewritten, M kept` 写在进程 cwd 的 `build/logs/VM.txt`（注意不是模块目录 Logs/）。
- 预防建议：① 性能 A/B 结论只能来自**同环境背靠背**轮次，隔日/隔机数字禁止直接对表下结论；② 观测到「所有用例全段均匀平移」优先怀疑环境漂移而非「改动普遍变慢」；③ 单轮异常值（如首跑 17379ms 高于复测带）补跑一轮即可归位，勿据单轮推翻批次决策。

### [2026-10-17] NetTest TlsTest K1 全套件静默提前退出（TLS 握手失败→VM 收摊）：runner CWD 不对导致 `Resources/cert.pem` 相对路径读空，两轮对照实验白做——先验证测试前置条件再怀疑代码回归
- 现象：P1 收尾复跑 NetTest 回归，套件跑到 TlsTest K1 即静默终止（exit 0、无 FAIL 无异常输出）；且两次运行停止点不同（H1 / TlsTest banner）疑似非确定性回归。曾高度怀疑当日 C VM 槽位分配改动，做了 first-fit 对照实验（还原旧分配+重编+重跑）——同样失败，实验白做。
- 首个异常阶段：测试执行环境（非代码）。
- 根因：TlsTest K1 用 CWD 相对路径 `File.readAllText("Resources/cert.pem")`，规范跑法要求 CWD=`test/Other/NetTest`；本次 runner 以 CWD=`simple_language` 启动（该目录无 Resources/），读证书抛异常被 `try?` 吞掉、服务端协程静默死亡，客户端 TLS 握手无人应答抛 TlsError.Handshake 未捕获 → VM 正常收摊（VM 日志只有 teardown 无 error，stdout 无 FAIL 行）——"全套件静默提前退出"的完整解释。
- 修复点（文件/模块）：无代码修复；runner 修正为 `Set-Location test\Other\NetTest` + 绝对路径传 module.json（与 2026-09-06 绝对路径纪律同源）。
- 如何快速复现：CWD=仓库根/simple_language 跑 NetTest module.json → TlsTest 静默死；CWD=test/Other/NetTest 同一 module.json → 104/104 全绿。
- 如何快速验证：改 CWD 重跑即见分晓；VM 日志（build/logs/VM.txt）确认 teardown 前最后一簇 throw 无 catch 即"前置资源读取失败"指纹。
- 预防建议：① 复跑既有套件前先确认其 CWD 敏感资产（相对路径证书/数据文件）——查测试源码里的 `File.read*`/相对引用与套件 md 说明；② "静默提前退出且无 FAIL 行"= 主流程未捕获异常特征，先看 VM 日志 teardown 前最后的 throw 簇，别急着怀疑刚改的代码；③ 对照实验前先做零成本环境核对（CWD/路径/环境变量），本项目已有两起同族教训（2026-09-06 相对模块引用、本例相对证书）。
