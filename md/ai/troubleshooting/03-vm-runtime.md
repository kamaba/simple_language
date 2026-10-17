# 复盘分片 03 · C VM 运行期语义

> 收录：读写路径、调用帧、值比较/包装、类型槽、payload 二进制、模块装载、JIT 语义类复盘。
> 路由索引与追加规则见 [故障排查流程 §6](../故障排查流程.md)；新复盘按域追加至本片末尾。

### [2026-09-11] JsonTest asMap containsKey/取值失败：比较函数缺少 Object 槽标量包装归一化（移植 C# NormalizeObjectReferenceKindInPlace）
- 现象：新增 JsonTest 中 `asMap containsKey`、`asMap 取值` FAIL；map 插桩日志显示 hashId 命中（ent_hash==want）但 `runtime_value_compare_equal` 对 key 返回 false。
- 首个异常阶段：VM（runtime_value_compare_equal 字符串分支）。
- 根因：MapEntity.key 经成员读取得 `etype=EVMType_String`，栈上弹出的字符串 key 经 `vm_try_pop_runtime_value` 得 `etype=EVMType_Object`（vm_object 指向 String 包装）。字符串比较分支要求双边 String 才 strcmp，一边 Object 即 equals=FALSE，随后 fall through 到引用比较（两个不同 String 对象指针不等）。C# VM 在比较前有 `TryNormalizeObjectScalarInPlace` + `NormalizeObjectReferenceKindInPlace`（Object 槽装标量包装时采用包装真实 etype），C VM 缺此步骤。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/runtime_value/runtime_value_compare.c`——新增 `vm_cmp_normalize_object_reference_kind_in_place` 并在 `runtime_value_compare_equal` 开头对 a/b 调用；String 包装仅原地改 etype（包装 member[0] 存裸 char*，`vm_object_new_string`，不能走 `vm_object_try_load_member_value` 解包，与 vm_runtime.c Switch 解包 `!= EVMType_String` 排除一致）；其他标量包装（Int32/Float64…）整体替换为 `vm_object_try_load_member_value(obj,0)` 读出的值（等价 C# SetValueBySObject）。
- 如何快速复现：JsonTest 的 `user.asMap()` 后 `containsKey("name")` / `m["name"].asString()=="alice"`。
- 如何快速验证：`dotnet run --project project\CSimpleVMStdTest -- -p test\JsonTest\JsonTest.sp -test` 全绿（含 asMap 三断言）。
- 预防建议：C VM 比较语义与 C# 基准逐分支对照（`source/VM_extracted/.../RuntimeValueCompare.cs`）；凡"类型相同值相同却比较失败"，先怀疑 etype 不一致（成员读出 vs 栈弹出路径的 etype 约定不同），用插桩打两边 etype 取证；该归一化同时修复所有「Object 槽标量包装 vs 直接值」比较场景。

### [2026-09-12] DISABLE 覆盖命中硬要求误判否：sl_requirement.c 未按 §8.5.5① 区分硬/软要求
- 现象：P1.8 Y 组验收项「--disable 使硬要求判过」无法通过——`--disable os` 对 os=linux 的必失败硬要求仍 `✗ os : linux` + exit 1 阻断加载； DISABLE 命中一律按失败处理。
- 首个异常阶段：VM（`sl_requirement.c` eval_atom_effective 的 DISABLE 分支）。
- 根因：原实现 DISABLE 命中时不分硬/软一律 record_fail + return FALSE，违反设计 §8.5.5①「条件屏蔽」——语义应为：命中**硬要求 → 直接判过、模块照常加载**（原始判定失败时记 ✗→✓ 条目）；命中**软要求 → ⚠ 判否**（进 warns 不阻断）。§8.5.4 伪代码的 return 0/1 是简化示意，掩盖了硬/软分流。
- 修复点（文件/模块）：`csimple_lang/src/vm/platform/sl_requirement.c` eval_atom_effective 的 DISABLE 分支按 atom 是否软要求（optional）分流：软要求 ⚠ 判否；硬要求条件屏蔽直接判过，原始判定失败时在报告中记 `✗→✓ … [override by CLI --disable os]`。
- 如何快速复现：复制 ProjectTest.module.json 为 linuxos 变体（`"set": ["window"]` → `["linux"]`，须放同目录，VM 从 module 位置推导运行资源），直跑 exit 1；再加 `--disable os` 若仍 exit 1 即复现。
- 如何快速验证：`csimple_lang\scripts\test-platform-override.ps1` 25/25 全 PASS（尤其 T9a linuxos 直跑 exit 1 / T9b `--disable os` 后 `✗→✓ os : linux [override by CLI --disable os]` + `_main_ start` + exit 0）。
- 预防建议：①覆盖语义以 §8.5.5①「★ 需求核心」为准，勿照抄 §8.5.4 伪代码的简化返回值；②平台检查语义修正后先跑 `test-platform-override.ps1` 回归（该脚本同时覆盖四通道优先级/--enable/--set/--no-override/未知 key/负例 20036）；③测试用 module 变体必须与原 module 同目录（放 TEMP 会诊断判过但 VM 执行中途 exit 1）。

### [2026-09-13] P3 设备探测回归两缺陷：count(cpu) 双计 + current 通道 deviceKind 不融合 device 覆盖
- 现象：PlatformSevenTest 回归两项异常——① `Environment.current.device.count(cpu)` 观测为 2（物理机逻辑核心众多但设备表 CPU 条目应只计 1）；② ProjectTest.jsonc 自带 `override: {"device.gpu": false}`（本机核显环境）时，`current.device.has(gpu)=false` 但 `current.device.kind=2(GPU)`——同一通道内 kind 与 has 互相矛盾；而 probe 通道 kind=2 合理（probe 恒 raw）。
- 首个异常阶段：均在 VM 平台层 `csimple_lang/src/vm/platform/sl_environment.c`（① build 累积逻辑；② sl_env_effective_view 遮蔽范围）。
- 根因：① build 路径预置 `deviceCountBy[SL_DEV_CPU]=1` 与探测列表循环 `+=1` 叠加——p3-1b 起探测列表恒含 CPU 条目（sys_info 契约），预置未删除导致双计；② sl_env_effective_view 原只做 SET 六项遮蔽（cpucount/memory/runtime/osversion/os/arch），device 域 DISABLE/ENABLE 覆盖完全不进遮蔽路径——快路径判断 hasSet 只认 SL_OV_SET，且遮蔽主体 sl_env_shadow_set 不含设备字段。
- 修复点（文件/模块）：`sl_environment.c` 三处——① build 改为「探测列表循环累积后条件预置」（`deviceMask` 无 CPU 位才防御性预置 1）；② 新增 `sl_env_shadow_device`（两段 key `device.gpu` 精确优先、单段 `device` 整类兜底；DISABLE 清位+清数、ENABLE 置位+原 0 计 1 与 DeviceCount 转发口径一致；changed 后按优先级链 gpu>npu>dsp>fpga>cpu 重选 deviceKind，复用从 build 抽出的 `sl_env_pick_device_kind`，R5）；③ 快路径判断 hasSet 扩为 hasShadow（SET **或** device 域 DISABLE/ENABLE，经新增 `sl_env_is_device_key` 判 key 前缀）。同步修正测试断言语义：有 device override 的字段（kind）双通道必然不同是预期行为，断言改「双通道各自自洽（kind 是本通道存在的设备）+ 无 override 字段（cpu count）严格镜像」。
- 如何快速复现：① `dotnet run --project project\CSimpleVMCoreTest` 跑 PlatformSevenTest，观测 `deviceCount(cpu)`；② 本机带核显 + ProjectTest.jsonc 的 `override: {"device.gpu": false}` 下看 `device kind : 2` 与 `count cpu/gpu/npu : 1/3/0`（raw 泄漏到 effective）的组合。
- 如何快速验证：回归后观测——`device kind : 1`（effective 融合 gpu disable 后重选 CPU）、`device probe kind (raw) : 2`（probe 恒 raw 不动）、`deviceCount(cpu) = 1`、effective `count cpu/gpu/npu : 1/0/0` vs probe raw `1/3/0`（Windows DXGI 枚举含软件适配器属探测层正常行为，非 bug）；CSimpleVMCoreTest 全量（含 EnvironmentTest/PlatformOverrideTest/PlatformLibTest/PlatformSixTest/PlatformSevenTest）全绿 exit 0。
- 预防建议：①「预置兜底」类代码在下游契约改为「恒提供」后必须重审兜底是否仍在预期路径上（本例探测层契约 P3.1b 已保证恒含 CPU，预置即成双计）；② effective/probe 双通道断言：**有覆盖的字段断双通道差异符合语义（各自自洽），无覆盖的字段断严格镜像**，勿对全部字段一刀切 mirror；③ 新增静态函数定义必须放使用点之前（C99 无隐式前向声明，本批曾因定义后置报 call to undeclared function 后才移位）；④ device 覆盖的转发层是两处独立融合（GetInt/GetStr 走 effective_view 含 deviceMask/countBy 遮蔽；DeviceHas/DeviceCount 走 plat_env_ov_hit 独立融合），改一处后必须核对另一处口径一致（ENABLE 计数均 max(raw,1)）。

### [2026-09-14] CodecFrameTest 泛型闭包运行期崩坏：闭包调用帧丢失模板实参 + LoadConstType 失败不压栈造成连锁栈错乱（F1）
- 现象：G1 修复后 CodecFrameTest D/F/H 等组（Utf8Codec/JsonCodec roundtrip、bindStream）VM 运行期仍 FAIL：泛型宿主内闭包（如 `_FramedBindStream<T>` 回调里的 `drainFrames<T>`）被调用时 callee 拿不到宿主模板实参，泛型槽 T 解析为占位类型、storeobj 被拒（插桩 REJ-R6 取证）；LoadConstType 解析 RuntimeType 失败时不压任何值，其消费方（系统调用弹参/局部槽写入）弹到错位栈值，连锁栈错乱扩散至后续全部断言。
- 首个异常阶段：VM（CallVirt/CallClosure 调用帧装配 + LoadConstType 栈平衡）。
- 根因：四处叠加。①`vm_resolve_call_method_templates` 不携带 VM 上下文，无法借当前帧组合模板表解析方法模板实参；②闭包对象只记 host_rt 不记定义点帧的组合模板表 `[类模板..., 方法模板...]`，且定义帧弹出后其 rt_list（合并堆缓冲）即释放——闭包调用时只能退回 assembly 方法 id 反查，拿到裸定义类 rt，丢失泛型实例化；③非泛型接收者（Utf8Codec）进入泛型父类方法时，`vm_alloc_init_runtime_object_row` 的 `runtime_type_list_count > 0` 守卫误入 def_class 兜底，类模板引用（`Codec<K,V>.decode` 返回槽 T）产出占位 T，storeobj 拒绝；④LoadConstType 解析失败分支只记日志不压栈，破坏表达式栈平衡。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/vm_runtime.c`——`vm_resolve_call_method_templates` 增加 `vm` 首参（Call/CallVirt 两处调用点同步）；闭包创建处捕获 `vm->runtime_type_list(_count)`；闭包调用优先 `vm_closure_object_try_get_host_rt` 取定义点宿主类型、经 `vm_closure_object_try_get_method_templates` 取方法模板尾传给 `vm_frame_push_call` 重建同样的组合表；LoadConstType 失败分支补 `vm_eval_push_null` 占位。`csimple_lang/src/vm/runtime/method/runtime_closure_method.c`——`vm_closure_object_new` 增加 rt_list 深拷贝参数（帧弹出后缓冲即失效，指针不可留存）；新增 `vm_closure_object_try_get_method_templates`（按宿主类模板数偏移取方法模板尾）。`csimple_lang/src/vm/runtime/value_ops/runtime_value_ops.c`——`vm_alloc_init_runtime_object_row` 组合模板表为空时仍以帧自身类为解析上下文（类模板引用经类关系绑定解析）；新增 `vm_try_pop_top_slot_raw`（按槽位 kind 原样弹出原始字节，不构造中间 VMRuntimeValue）。
- 如何快速复现：`JsonCodec` roundtrip（F 组）或 `Stream.bindStream`（H 组）——泛型宿主闭包回调 + data 实参 LoadConstType 组合。
- 如何快速验证：CodecFrameTest 325 check 全 OK；R12 插桩清理（全仓 P2DBG/T6DBG/DBG- 清零）后重编全量回归 0 FAIL、0 DBG 残留，check 计数与清理前一致（`: OK` 300 / `OK` 301）。
- 预防建议：①闭包对象生命周期长于定义帧，凡引用帧上数据必须深拷贝（合并模板表缓冲随帧释放）；②「解析失败不产出」类分支必须审视表达式栈平衡——消费方按表达式产值约定弹栈，缺一个占位即连锁错位；③泛型方法/闭包/协程三条调用路径必须与 `push_call` 的组合语义（类模板+方法模板合并表）镜像——vm_coroutine_create 漂移刚复盘过，本次闭包调用又重演，组合表构造应集中一处避免多路漂移；④非泛型子类进入泛型父类代码时，类型解析以帧的声明类关系为上下文，勿以「模板表为空」作切兜底判据。

### [2026-09-15] StreamFileTest F 组挂死：初始化链回放把负数 base_class_id（导入类）当「无基类」截断
- 现象：StreamFileTest F 组（StdOutStream）运行中永久挂起，无输出无崩溃。
- 首个异常阶段：VM 运行期（`runtime_call.c` 初始化链回放循环）。
- 根因：`csimple_lang/src/vm/runtime/call/runtime_call.c` 继承链回放用 `if (cur->base_class_id <= 0) break;` 判终止——语义是「0 = 无基类」，但**负数 class id 是导入类的合法哨兵**（Front 导出侧直接透传），`<= 0` 把负 id 也当终点，基类字段初始化链被截断，下游读到未初始化状态导致挂死。
- 修复点（文件/模块）：`runtime_call.c` L372 改 `== 0`。
- 如何快速复现：改回 `<= 0` 跑 StreamFileTest F 组即挂死。
- 如何快速验证：A–G 七组全 OK（本轮全量 19 OK / 0 FAIL）。
- 预防建议：凡以 0 为哨兵的判断一律用 `== 0`，不用 `<= 0`/`>= 0` 兼收负值域；class id 负值段是跨模块导入的合法区间，任何边界判断都要先查 `sl_runtime_assembly.c` 的直传约定。

### [2026-09-19] vm_frame_push_call 从不赋值 callee_method：watchIn 按声明类型匹配调用链恒短路（SLIR 数据正确、帧填充缺失）
- 现象：P3 watchIn 验收时按类型全名匹配的用例（`Debug.watchIn( 9, "wix2", "DebugTest" )`）零命中、断言 FAIL——而 module.json 里该方法的 declaringTypeFullName="DebugTest" 数据正确，strstr 本应命中；同文件按方法名匹配的用例也零命中（复合根因见下条）。
- 首个异常阶段：VM（vm/runtime/call/vm_call_frame.c 建帧填充；IR/Export 数据均正确）。
- 根因：同步调用主路径 `vm_frame_push_call` 只赋 `callee_name`，**从不赋 `callee_method`**——全仓该字段仅协程入口（vm_coroutine.c）、sync 路径（vm_call_frame.c:201）、spawn 槽准备（L359）三处赋值 → `vm_debug_call_chain_has` 遍历调用链时 `f->callee_method->declaring_type_full_name` 分支因指针恒 NULL 短路；连带 `vm_runtime.c` PopUnderflow 日志的 `callee_method->method_id` 也从未生效。
- 修复点（文件/模块）：`vm_call_frame.c` push_call 步骤 5 补 `f->callee_method = method;`——borrowed 语义：method package 归 module registry 所有、生命周期长于帧，`vm_frame_free_callee_resources` 不释放该指针，无 double-free 风险。
- 如何快速复现：实例方法内 `Debug.watchIn( n, "label", "ClassName" )`（按声明类型匹配），修复前恒 0 命中。
- 如何快速验证：DebugTest P3「watchIn matches declaring type name」OK；DebugTest 全段（P1/P2/P3）全绿 exit 0。
- 预防建议：①VMCallFrame 新增字段后 grep 全部建帧路径（push_call/sync/coroutine/spawn 分散多处）核对赋值——「只有协程填、同步忘填」这类半成品填充极难从编译期暴露；②borrowed 字段在头文件注释里写明 owner 与生命周期，防止后人误加 free。

### [2026-09-19] Watcher 回调零触发：闭包经 `as Function`（`Array<T>._getItem_`）被 CastClass 静默置 null——闭包对象不携带 Function 类型信息
- 现象：Debug 系统 P7（Watcher 独立监视类）验收时，`listen(闭包)` 后 `update` 变化，回调计数恒 0（不触发）。两方案对照放大怪象：方案 A（`Array<object>` 存储 + 取出 `as object` + 显式 `as Function`）下 unlisten 按引用匹配**正常**但回调不触发；方案 B（`Array<Function>` 字段）下 unlisten 匹配**也失效**。
- 首个异常阶段：VM（Front/IR 全对：NewClosure 正常生成传入、fireCallbacks 里 `cb()` 正确生成 CallClosure opcode 105；值在 CastClass 处被静默破坏）。
- 根因：闭包对象在 VM 层是普通 VMObject（`vm_closure_object_new` 只设 etype=EVMType_Object + member_data 装 VMClosureData，**不设置任何与 Core.Function 的类型关联**）→ `as Function` 走 `OpCode_CastClass`（vm_runtime.c）Step 6 类/接口分支，`runtime_type_is_extends_relation(闭包, Function)` 必失败 → `vm_eval_push_null` **静默置 null 且无任何日志**。而 `Array<T>._getItem_` = `SystemArrayGetValueThis(...) as T`——T=Function 时取出即 null；方案 A 的 `as object` 走 Step 2（目标 EVMType_Object 恒等通过）所以匹配正常、只有显式 `as Function` 破坏——双方案现象差异反证根因。存储侧（_setItem_/SystemArraySetValueThis）不经 CastClass，故写入无恙。
- 修复点（文件/模块）：`Front/Lib/Core/Watcher.sl`——回调存储从 `Array<Function>` 改为单向链表 `_WatcherCallbackNode`（`Function _fn` 字段直存闭包 + 遍历时 `var cb = cur._fn`（var 接收字段，无转型）+ `cb()` 直接调用），完全复刻 Stream 的 `_MapStream._mapper` 已验证模式；`fireCallbacks` 先取 `nxt` 再调用保证迭代安全。纯 SL 零 VM 改动，符合设计 §13.7。
- 如何快速复现：闭包装入 `Array<Function>` 后 `_getItem_` 取出直接调用——闭包已被置 null，CallClosure 不执行且无报错。
- 如何快速验证：WatcherTest W1~W11 全绿（回调触发/多回调/unlisten 按引用解绑/emit/重入保护/string/data/引用比较）；DebugTest（P1~P6）无回归。
- 预防建议：①**闭包值禁止经 `as Function` 转型路径存取**——需存储回调时用 `Function` 字段直存（_MapStream / _WatcherCallbackNode 模式）或 object 弱类型通道（CastClass Step 2 恒通过），`Array<T>` 元素存取必经 `as T` 故 T=Function 不可用；②CastClass 失败静默置 null 无日志——排障「值莫名变 null / 调用静默不执行」时优先怀疑转型链路；③`bind`、`next` 均为语言保留字（参数/局部变量命名撞上即 File 层报「名称不允许使用关键字」，next 还会连锁误报「不允许在 Else 后增加任何代码」），命名避开关键字清单。

### [2026-09-23] 纯文件名参数 run 模块 0xC0000005：sl_load_recursive 空 dirname 把 moduleReferences 拼成 "/../Core/..." 按盘符根解析（P0.5 echo 插件端到端验证附记）
- 现象：P0.5 回声插件端到端验证，CWD=out\export\SpecialTest 下 `csimple_lang.exe run SpecialTest.module.json`（**纯文件名参数**）稳定崩溃 exit 0xC0000005；同一 module.json 用带目录的相对/绝对路径参数在任意 CWD 均正常。
- 排查路径：① 三连对照——CWD=csimple_lang（绝对路径参数，Echo 正常输出但 FFITest FAIL exit 1）/ CWD=test\SpecialTest（相对路径参数，全绿 exit 0）/ 崩溃组，锁定差异为**参数字符串形式**；② 崩溃处理器（sl_crash-win32）打印 `AV read target=FFFFFFFFFFFFFFFF`（-1 哨兵被直接解引用）且 `[BOOT] slir load done (packages=1)`（正常 3）；③ 读装载链 cli_command.c→slir_runtime_load_in_execution_order→sl_load_recursive，moduleReferences 为 `../Core/Core.module.json` 相对路径，拼接基准取自**参数字符串的 dirname**。
- 根因：`sl_dirname_from_path("SpecialTest.module.json")` 无斜杠返回**空串**，L2453 `snprintf("%s/%s", "", "../Core/...")` 拼出以 `/` 开头的路径 → Windows 按**盘符根**（D:\Core\...）解析失败 → 依赖装载失败被 Warning+skip 容忍 → packages=1 带病装配 → 运行期引用未注册的 Core 符号拿到 -1 → 0xC0000005。
- 修复：sl_load_recursive 中 dirname 为空串时归一为 `"."`（依赖相对当前目录解析），一处修复同时覆盖 nativeDll/AOT/plugins 装载基准（package_directories 同源）。
- 如何快速验证：CWD=模块目录下 `csimple_lang.exe run SpecialTest.module.json` → packages=3、不再崩溃（FFITest 段 FAIL 属 CWD 约定，见上一条）；CWD=test\SpecialTest 全绿回归。
- 预防建议：① 装载/资源解析的路径拼接，空目录串必须归一（"."），否则以 `/` 开头的拼接在 Windows 会静默漂到盘符根；② 依赖装载失败被容忍跳过是"带病运行"的温床——缺包后应在装配期给出明确错误而非运行期崩（后续可加固）；③ 排障判据——BOOT 行 packages 数是模块装载完整性的第一指标，与预期不符时先查依赖解析再查 VM 代码。

### [2026-09-23] --strict-plugins 拒绝装配后仍进 scheduler：vm_set_runtime_load_model 无返回值，装配失败状态不传播（P0.5g 负向验证附记）
- 现象：P0.5g 负向场景 b（`--set os=linux --strict-plugins`）中，插件平台校验失败升 error、装配已中止（plugin 报告块正常打印），但 VM 仍输出 `vm_initialize ok / entering scheduler` 继续调度，进程未以失败退出。
- 首个异常阶段：VM 启动（runtime assembly 装配链，非用户代码）。
- 根因：`vm_set_runtime_load_model` 为 void 返回——装配内部插件平台校验（sl_require_check）失败置 error 后装配中止，但失败状态无通道传回 `cli_command.c` 的 boot 序列，CLI 无从感知，按成功路径继续 vm_create/vm_initialize/进调度器。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/vm_runtime.h`/`vm_runtime.c` `vm_set_runtime_load_model` void→int32 返回装配结果；`src/cli/cli_command.c` 检查失败即打印 `[BOOT] runtime assembly rejected (module not loaded)` + 清理 + return 1。
- 如何快速复现：CWD=test\SpecialTest 下 `csimple_lang.exe run --no-banner --set os=linux --strict-plugins <SpecialTest.module.json>`，修复前报告块之后仍见 `entering scheduler`。
- 如何快速验证：三态闭合——场景 b 立即中止 exit 1、止步 `[BOOT] runtime assembly rejected`（无 scheduler 输出）；场景 b + `--force-run` 豁免放行全绿（Echo(3.5)=3.5、ProcessTest passed=33）；正向（onUnavailable=warn 恢复）加 --strict-plugins 零影响。
- 预防建议：① boot 序列各步骤的失败必须经返回值向上传播，CLI 收到失败后禁止进入 scheduler；② "拒绝装载"类校验的负向验证必须检查 exit code 与 scheduler 输出双指标，不能只看报告块是否打印。

### [2026-10-11] 指令 payload 二进制化（v1）落地期 NewArray 静态初始化 0xC0000005：runtime_def_type_add_template_arg 返回值是「新槽位索引」不是 bool
- 现象/任务：调用包/类型树 payload 由 JSON 文本改为二进制 v1（魔术字节 0x01，双格式兼容）后，重编标准库 + 用例集，NullFastTest 起始三条指令（`__static_field_init__Core.Project` 的 NewArray）即崩 `0xC0000005 AV read target=FFFFFFFFFFFFFFFF`；Debug/Release 同崩（排除宏配置）；旧 JSON 模块（linux-dist）同 VM 正常（锁定新解码路径）。
- 排障路径（R12 插桩闭环）：① VM.txt 缓冲写丢失崩溃尾日志 → 改 stderr+fflush 直写 + Start-Process 重定向落文件；② 指令级插桩锁定 opcode=29 NewArray、plen=85，十六进制 dump 手工解码树内容正确（`Core.Array<T>[Core.Object]`，字节序无误）；③ 逐层打点收敛到 `get_runtime_type_by_def_type` 内 1033 行 `owner_runtime_class->template_count`；④ PDB + dbghelp（SymGetLineFromAddr64 脚本）把崩溃 RVA 映射回源码行，调用链与插桩互证；⑤ 在 template_index 分支加探针，命中 `owner=0xDFDFDFDFDFDFDFDF`（MSVC 已释放堆填充）→ 定性 use-after-free。
- 根因：`runtime_def_type.c` 的 `runtime_def_type_add_template_arg(parent, child)` **成功返回新槽位索引（首个 child 返回 0）、失败返回 -1**——非 bool 语义。二进制解码器 `bin_decode_def_type_node` 首版写成 `if (!add(node, child)) { free(child); }`：首个 child 成功挂入返回 0，`!0` 为真，把**已归 parent 所有的 child 释放**，列表留悬垂指针；后续 ensure_static_member 递归解析该树时解引用 0xDF 填充区崩溃。JSON 老路径 `(void)add(...)` 忽略返回值故无恙——**同一 API 的两种调用姿势暴露了返回值语义陷阱**。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/payload/runtime_payload_binary.c` 判定改为 `< 0` 才释放。全部临时插桩（vm_runtime.c / runtime_payload_json.c / vm_runtime_type.c 共 11 处）与临时脚本（tmp_sym.ps1 等）清理归零。
- 如何快速复现：带 bug 版解码器 + 任意含 `static Array<T>` 类字段的工程（如 NullFastTest 引 Core.Project._inputArgs 初始化链），运行 module.json 即崩；旧 JSON 模块不触发（不走二进制分支）。
- 如何快速验证：修复后 Debug/Release × 6 套件（BaseTest/ExpendTest/MathTest/SpecialTest/BenchMark/NullFastTest）全 exit 0；旧 JSON 模块（linux-dist NullFastTest/BenchMark/Core）全 exit 0（双格式兼容同时验证）。
- 预防建议：① 非布尔返回值 API（索引/-1、指针/NULL）严禁用 `!` 真值判定，code review 对 `if (!xxx_add` 模式保持敏感；② 头文件应把返回值契约写进声明注释（本次已在实现处补注）；③ 崩溃尾部日志丢失时优先 stderr+fflush 直写而非翻日志缓冲；④ PDB 行号映射（dbghelp 脚本）与运行时插桩互证是 Windows 无调试器环境下的低成本定位组合。格式规范见 `csimple_lang/md/design/PAYLOAD_BINARY_FORMAT.md`。

### [2026-10-14] P1-3 ip 直进落地暴露 16 处 handler 违约 bug 群 + 一轮「plen 不可靠」假阳性误诊（实为编辑未落盘）
- 现象/任务：runtime 优化 P1-B（RUNTIME_OPTIMIZATION_PLAN.md §5.2）实施「顺序执行 ip 直进」——主循环不再每条指令从 `instr_byte_offsets[execute_index]` 重取 ip，顺序执行直接沿用上一 handler 推过的 ip。落地后 BenchMark 偶发输出错乱。
- 排障路径（R12 证据先行）：直进 else 分支临时加 P13SEQ 校验桩（顺序取指时比对 `vm->ip` vs offsets 表，desync 记 prevOp/curOp/exec_idx/got/want 并纠正）→ 审计期捕获 **185 万条 desync** → 逐条归因出 **16 处 handler 不推进/推进不足 ip** 的违约：Nop/Label/LoadConstNull/LoadBegin/default + opcode 23/33/34/108~111/113~115 + 36（StoreArrayIndexField，1 字节 bool opValue 未推）+ 112（StoreNotStaticField1ConstValue，etype+value 未推）。全部补推修复后 desync 归零，桩移除。
- 根因（两层）：
  ① **隐式契约被系统性掩盖**：原版主循环每条指令都查 offsets 表重定位 ip，任何 handler 忘推 ip 都被无条件兜底——16 处违约在原版下零症状、潜伏已久。直进把兜底撤了，契约违约才显形。
  ② **假阳性误诊一轮**：修复 36/112 后曾观察到「desync 逐字节不变」，据此下过「Front plen 序列化小于物理 payload」的错误结论。实情是**那两处编辑在上下文压缩时丢失未落盘**，磁盘 handler 仍是旧代码。重新落盘后 desync 立即归零。Front 侧证据链复核（IRData.cs `UpdateByteLength` 全路径 + C VM `vm_patch_branch_bytecode_offsets` 用同一 plen 构建 offsets 表——plen 若错全表必错乱，不可能只错个别指令）确认 **Front plen 序列化完全可靠**。
- 修复点（文件/模块）：`vm_runtime.c` 16 处 handler 补推 ip（36/112 精确补推，其余 35 处统一 `vm->ip += payload_length` 整推）；直进契约已文档化进 RUNTIME_OPTIMIZATION_PLAN.md §5.2（每条 handler 必须把 ip 推过自身 payload——新增 opcode 的 handler 铁律）。
- 如何快速验证：P13SEQ 审计桩（已移除）desync 计数归零；BenchMark Release 总计 17347 → 16948~17091ms（对原始基线 -15%），6 套件回归全绿、FAIL 全存量。
- 预防建议：① 撤掉兜底类优化的前提是先审计兜底掩盖了多少违约——「每循环无条件重算」掩盖的隐式契约，用校验桩量化取证后再移除（R12 在性能优化中的变体应用）；② **多轮编辑（尤其经历上下文压缩的长会话）后，下任何「修复无效」结论前必须先 Grep/Read 验证磁盘状态**——编辑丢失的假阳性会把排障引向完全错误的层（本次差点冤枉 Front 序列化层）；③ 观测到「修复前后数字逐字节不变」这种强不变性，优先怀疑「改动根本没生效」，而非「改动没起作用的原因很深」。

### [2026-10-14] 算术快路径 V1（头文件 inline）「优化了反而慢」：`vm_run` 巨函数内宏展开 10 份副本的 I-cache 布局劣化，跨 TU outlined 后双段兼得
- 现象/任务：P2-A 算术专项把 16 组合标量快路径 `vm_binary_op_scalar_fast` 写成 `runtime_dispatch.h` 的 static inline（`VM_CASE_BINARY_OP` 宏调用），A/B 后 BenchMark 总计 17888~18192ms，比基线 17187~17379 **全面劣化**：算术段确有收益，但 RegExp 段（compile/matchAll/replace，占总计 ~78%）+9%，完全抵消并反超。
- 根因：**代码布局（code layout）效应，非逻辑 bug**——inline 函数体经宏在 `vm_run` 巨函数内展开成 **10 份 ~330 行副本**（Add~Shr 十个 opcode 各一份），分派循环指令足迹膨胀 → I-cache/分支预测布局劣化 → 未命中快路径的 RegExp 段首当其冲。这解释了「优化了快路径却拖累了不相关段」的反直觉现象。
- 修复点/决策：V2 改为**跨 TU outlined 单副本**（函数体放 `runtime_dispatch.c`，头文件只留声明；10 个 case 各付 1 次 call/ret；sign 转运行期参数，switch 不折叠代价可忽略）。V2 三轮 16971/16964/16990（±0.15%）：RegExp 回落基线带 + 算术收益保住，布局归因实锤。曾评估头文件裸 `static`（同 TU 折叠），因 clang `-Wunused-function` 破坏零警告纪律（R4）放弃。
- 如何快速验证：`csimple_lang\build\Release\bin\csimple_lang.exe run --no-banner simple_language\out\export\BenchMark\BenchMark.module.json` 背靠背 ≥2 轮，分段看 RegExp compile/matchAll/replace 是否回到基线带（2411~2465/3279~3310/5164~5263ms）。数据与两形态对照表见 `csimple_lang/md/RUNTIME_OPTIMIZATION_PLAN.md` §6.6。
- 预防建议：① **巨函数 + 分派宏架构下，`static inline` 热路径体不是免费的**——一个被 N 个 case 共用的宏调用点会把函数体克隆 N 份；② 热路径共用体优先 outlined 跨 TU 单副本（call/ret 成本 ≪ I-cache 劣化）；③ 出现「快路径段变快、无关段变慢」的组合型漂移，第一假设应是代码布局而非数据竞争/竞态（海森堡现象反直觉方向）；④ A/B 必须按段拆解看，只看总分会把 +9% 和 -10% 平均成「没变化」。

### [2026-10-15] JIT P1 MVP bring-up 两个 codegen 笔误：快路径完成跳转误 patch 进 helper（运算重复执行）+ pop2push1 结果写回踩 rhs 死区（fib 无限递归）
- 现象：`--jit` 跑 BenchMark Fibonacci 停机 EXIT=1（`[JIT-ENTRY<] status=1 exec_idx=11 running=0`，exec_idx=11 实为 Minus(10) 桩 `sync_at(10+1)` 的遗留写回值）；第一处修复后转为 **fib 无限递归死循环**（帧无限增长、每层 callee `arg0` 恒 5 不递减）。
- 首个异常阶段：VM（JIT 生成机器码正确性；语义层 helper/前端均无恙）。
- 根因：两处独立的 `jit_x64_codegen.c` 笔误，均为「源码逻辑看着对、生成机器码错」级别：
  1. **快路径完成跳转 patch 目标错**：`cg_emit_binary_op`/`cg_emit_unary_neg` 中 `s_d1/s_d2/s_d3`（快路径算完的跳转）被 `cg_patch_to(…, helper_pos)` patch 到 **helper 序列开头**而非其后的汇合点——INT32/INT64/F64/F32 快路径算完直接落进 helper 把同一运算**再执行一遍**（每条二元运算净多吃 1 槽；栈空时 helper 弹 2 下溢 → `jit_helper_binary_op` 失败路径 `is_running=0` → SUSPEND 停机）。正确范式见 `cg_emit_unary_not`/`cg_emit_slot_load`（hj 慢路径跳 helper、完成跳转 patch 到 `cg_finish_helper` 之后）。
  2. **pop2push1 结果写回偏移错**：求值栈 sp 语义为「指向下一空位」，净收缩后新栈顶 = 收缩前的 lhs 槽（负偏移）；`cg_emit_binary_i32/i64/F64/F32` 却把结果写 `[sp+0]`（已弹出的 rhs 死区）→ 栈顶残留 lhs 旧值。fib 表现为 Minus(5-1) 的结果 4 落死区、CallStatic 弹走 lhs 5 → 每层 n 恒 5 → 无限自调用。
- 排障路径（R12 纪律全程）：CallStatic helper 插桩一次证伪三个假设（next_index 差一 / push_call 失败 / is_running 外部写点）→ BB 入口 trace helper（锁定三帧全走 BB2 递归分支）→ 对照 fib IR 做栈收支账（每层恰好少 1 槽）→ **`--jit-verify` dump 机器码手工反汇编实锤**（BB2 Minus 快路径尾部 `E9` 直落 helper 序列 + `mov [r13+0]` 写回错位；源码静态复读 4 轮未决、反汇编 10 分钟终结猜测）→ 修复后 trace 复验 arg0 5→4→3→2→1 正常递减、递归树完整展开回收。
- 修复点：`csimple_lang/src/lib/jit/jit_x64_codegen.c`——① `s_d*` patch 移到 `cg_finish_helper` 之后（落点=helper 序列之后，与 skip 汇合）；② 写回偏移 i32 `[sp-4]`、i64 `[sp-8]`、F64 `[sp-16]`（收缩前 lhs 位）、F32 `[sp-8]`。
- 附带坑（非 VM 缺陷）：VM println 走 `vm_output_text` 的 **`_write(1,…)` 裸 fd 写**（Windows 侧追加 \r\n），PowerShell `1>` 管道包装重定向下程序输出会**整段丢失**（console 显示与 `Start-Process -RedirectStandardOutput` 真句柄均正常、stdio printf 的 JIT stats 行不丢）——据 `1>` 判「程序无输出」会误诊回归。
- 如何快速验证：`build\DebugJit\bin\csimple_lang.exe run --jit --no-banner BenchMark.module.json` EXIT=0 且 `fib(0..21) sum = 28656`；`--jit` on/off 输出 diff（排除计时行）逐字节一致；NullFastTest/BaseTest `--jit` 全绿。
- 预防建议：① codegen 里「hj 慢路径跳 helper」与「s_d* 完成跳转」方向相反，新发射器必须先核对两族 patch 目标；② 字节栈任何「先收缩后写回」序列，写回位置一律按收缩后坐标重算（负偏移），`[sp+0]` 永远可疑；③ JIT 语义疑云的终审手段是 `--jit-verify` 机器码 dump + 反汇编对照 IR，插桩只负责指路。

### [2026-10-15] JIT P1 验收期 fib 段 2.1x 劣化：`jit_entry` 退场状态日志无 verify 门控，per-YIELD 记日志把助记成本放大 ~900ms
- 现象：P1 正确性全绿后 A/B，BenchMark fib 段 JIT 慢 2.1x（1698ms vs 解释器 804ms）；trace 已清、生成的机器码本身正确。
- 首个异常阶段：VM（JIT 退场路径的热点开销，非语义错误）。
- 根因：[jit_entry.c] 内 `JIT-TRACE entry ret` 日志注释写「--jit-verify 取证」，实际 `if (vm->is_running == 0 || status != JIT_RUN_DONE)` **无条件执行**——每次 YIELD（CallStatic 续跑一次一记，BenchMark 全程 ~9.1 万次）都走 `sl_log_add_runtime_log` 格式化+落盘 ≈10µs/次 → fib 段累计 ~900ms，恰好等于 A/B 差额。差分模式的取证日志必须由差分开关本身门控，否则"诊断仪器"本身成了负载。
- 修复点：`jit_manager.c` 补 `jit_config_verify_enabled()` 访问器（镜像 stats），`jit_entry.c` 日志块前置 `--jit-verify` 门控。修复后 fib 段 763ms vs 844ms（JIT 首次反超，1.1x）。
- 如何快速复现：DebugJit 树 `--jit --jit-stats` 跑 BenchMark，观察 fib 段耗时显著高于解释器且 `runs(yield)` 计数巨大。
- 如何快速验证：on/off 背靠背 A/B，fib 段不再出现倍数级劣化；输出仍逐字节一致。
- 预防建议：① 任何"每次退场/每 BB/每指令"级别的插桩或日志，交工前必须确认有**编译期常量或一次性缓存的开关门控** + 限量计数，且把门控参数写进注释；② 移除/门控插桩后必须重跑一次 A/B——插桩本身可能就是性能差异的全部来源；③ A/B 差额与插桩调用次数×单次成本数量级吻合时，先算账再找代码布局/竞态等复杂解释。
