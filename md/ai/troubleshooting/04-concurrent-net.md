# 复盘分片 04 · 协程 / isolate / 网络 / 调度器

> 收录：协程、isolate、通道、spawn 脱糖、Tcp/Udp/WebSocket、调度器挂死/自旋类复盘。
> 路由索引与追加规则见 [故障排查流程 §6](../故障排查流程.md)；新复盘按域追加至本片末尾。

### [2026-09-13] StreamTest 泛型闭包协程类型解析失败：vm_coroutine_create 的 rt_list 先写后被 prepare 清空 + prepare 期 VM 类型上下文错挂
- 现象：P1 `Stream<T>`（Core/IO/Stream.sl）落地后 StreamTest 多项断言 FAIL——凡经 `Coroutine.spawnClosure0` 创建的泛型闭包协程（`_ControllerStream` 分发协程、`StreamIterator` 拉取等）内，泛型槽（如 `IIterator<T>`）类型解析失败，T 解析为 NULL，事件分发/拉取不工作。
- 首个异常阶段：VM（`vm_coroutine_create` 入口帧 prepare）。
- 根因：两处叠加。①entry 帧的 `callee_rt_list`（类模板+方法模板合并列表）原实现先写帧再调 `vm_frame_prepare_method_slots`，而 prepare 第一步 `vm_frame_free_callee_resources` **无条件清空**帧上的 rt_list，先写的信息全部丢失；②prepare 运行期间 VM 的 `current_runtime_type`/`runtime_type_list` 仍是 spawn 调用方上下文（系统方法调用里即 `Core.Coroutine`），slot 行类型按错误上下文解析，泛型槽退回裸泛型定义、T→NULL。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/coroutine/vm_coroutine.c` `vm_coroutine_create`——rt_list 构造提前到 prepare 之前，prepare 期间把 VM 类型上下文临时切换到 `call_rt`+合并模板列表（镜像 `push_call` 的组合语义），完成后恢复原上下文；`callee_rt_list` 改为 prepare **之后**写入帧（避开 free_callee_resources 清空），owns 所有权与失败路径释放同步整理。
- 如何快速复现：Core 库下 `Stream<Int32>.fromIterable( arr ).toList()`（toList 闭包内经泛型 iterator 拉取，必经 spawnClosure0 协程）。
- 如何快速验证：`dotnet run --project project\CSimpleVMCoreTest` 跑 StreamTest，C/C2/D/E/F 组 15 项全 OK。
- 预防建议：①凡「构造帧后再调 prepare」的路径，先读 `vm_frame_free_callee_resources` 确认它会清什么——所有先写后 prepare 的帧字段都是嫌疑；②协程/帧创建处的类型上下文要与 `push_call` 的组合语义（类模板+方法模板+VM 当前上下文临时切换）保持镜像，两处实现极易漂移；③改 `.sl` 标准库源码后必须重编 Core（`CompileCore`）导出 module.json 才生效——测试宿主加载的是编译产物而非源码，不重编会跑旧代码造成「清理了源码却仍见旧打印」的假象。

### [2026-09-15] Isolate 调用表达式糖形式（Isolate.run( f(1,b) )）实参场景脱糖遗漏：实参 Par 挂链尾节点致链头扫描盲区（Fix 4）
- 现象：IsolateTest T3 场景 `isosT3._setItem_( i, Isolate.spawn( fnT3() ) )`（Isolate 链在**实参内**的糖形式）编译期 MetaCore 报「解析链 节点[spawn] 没有在类[Std.Isolate]中被发现」（+ _setItem_、isosT3 两条连锁）；独立语句形式 `Isolate.spawn( fnT2( spT2 ) )` 正常脱糖零警告。另：首次验证时 IsolateTest 整段无输出，先误入 VM.txt 尾部 Float8 Assert 歧途。
- 首个异常阶段：Meta（TransformIsolateCallNodes 未脱糖 → Meta 层找不到已改名的 spawn/spawnClosure，连锁）。
- 根因：标识符链 `a.b.c(...)` 的实参 Par 由 `AddParBegin` 挂在**链尾节点**（`m_CurrentNode.identifierNode.SetParNode`，即 `_setItem_.parNode`），链头 `isosT3.parNode == null`；`TransformIsolateCallNodes` 下钻只看语句成员自身的 `cnode.parNode`，实参 Par1.childList 里的 Isolate 链完全扫不到 → 脱糖不发生。排障弯路二则：①P/Invoke 模式不写 Logs/VM.txt、Result.txt，磁盘上残留的是 8/27 旧日志（Float8 Assert 为历史遗留），必须以时间戳甄别、以 stdout 为准；②ProjectTest.sp 的 `_main_` 用 `#!...!#` 块注释掉了 IsolateTest.fun()——测试从未运行，「零输出」≠「通过」。
- 修复点（文件/模块）：`source/Front/Compile/Parse/StructParseToSyntax.cs`——`TransformIsolateCallNodes` 对 `nodeType == IdentifierLink` 的语句成员追加调用 `DigLinkExtendParNodes(cnode)`；新增该方法：沿 `extendLinkNodeList` 嵌套结构递归下钻各链节点的 `parNode.childList`，但**链节点本身不进语句列表**（防 `x.Isolate.run(...)` 的 x 链成员被误当独立调用匹配）；另临时启用 ProjectTest.sp 的 IsolateTest.fun()（经确认保留，供后续回归）。
- 如何快速复现：IsolateTest T3：`test\ExpendTest\IsolateTest.sl` L694 `isosT3._setItem_( i, Isolate.spawn( fnT3() ) )`——修复前编译报解析链警告。
- 如何快速验证：重编译后 MetaCore 零警告；IsolateTest 全量运行 A~I 组 + T1~T9 全 PASS（T3 4worker PASS、T8 8worker 400消息、T9 kill 运行中 worker）。
- 预防建议：①标识符链是嵌套结构（`a.b.c` 中 b 挂 a 的 extendLinkNodeList），任何「沿语句找调用形态」的脱糖/检查遍历都要意识到实参挂在**链尾**而非链头，只下钻链头 parNode 必漏实参场景；②下钻链结构时链节点本身不可当语句列表成员处理，否则 `x.Isolate.run(...)` 类前缀链会误匹配；③P/Invoke 联调模式下 Logs/ 下文件不更新，排障前先核对时间戳再决定信不信；④测试集「无输出」时先查测试入口（.sp 的 `#!...!#` 块注释）是否真的调用了该 fun()——本轮 IsolateTest 长期未被 ProjectTest 运行正是如此。

### [2026-09-15] Isolate 直通形态独立语句被 as 后缀平铺破坏：appendAs 未区分语句级/表达式上下文（spawn/run 统一三方法改造 Fix 5）
- 现象：`Isolate.spawn( fn, a, b )` 直通形态写在**独立语句**（丢弃返回值）时编译期报「没有该节点」类解析错误；同一写法出现在赋值右侧（`Int32 r = Isolate.run( fn, a, b ) as int`）或嵌套实参中则正常。
- 首个异常阶段：Node/Parse（脱糖产出的平铺节点序列落入定义语句解析 ParseDefineStatements）。
- 根因：脱糖器 `BuildIsolateClosureCall` 一律在系统调用节点后追加 as Key 节点；独立语句上下文里该 as 节点平铺后使整条语句被定义语句解析分支接住，误报「没有该节点」。as 后缀本就只在表达式上下文（赋值右侧 / 嵌套实参 / 下标）合法。
- 修复点（文件/模块）：`source/Front/Compile/Parse/StructParseToSyntax.cs`——`appendAs = !isStatementLevel || HasAssignBeforeNode(pNodeList, i)`：语句级且前置无赋值符号时不追加 as 节点。
- 如何快速复现：任一工程写独立语句 `Isolate.spawn( fn )`（不带返回值接收）——修复前编译报解析错误。
- 如何快速验证：IsolateTest A 组独立语句 spawn/run 直通形态全 PASS（61/61）。
- 预防建议：脱糖器生成节点序列时必须感知**所在语句上下文**（语句级 vs 表达式级）——凡生成 as/赋值类后缀节点，先判定宿主是独立语句还是表达式子节点，盲目平铺会把语句踢进错误的解析分支。

### [2026-09-15] IsolateTest A9 断言时序竞争：P2 真并行下 recv 唤醒时 worker 可能已置 Dead(5)
- 现象：A9（spawnInstance 别名形态）断言 worker `status == Running(2)` 之类特定状态偶发 FAIL，同代码多次运行结果不稳定。
- 首个异常阶段：VM 运行期（测试断言层，非 VM 缺陷）。
- 根因：P2 (1:1) 线程模型下 worker 独占 OS 线程真并行——主端 `recv()` 被唤醒拿到回传消息时，worker 线程可能已正常返回、状态机已走到 Dead(5)；「收到消息 ⟺ worker 仍在 Running」在跨线程下不成立，属固有竞态而非可修 bug。
- 修复点（文件/模块）：`test/ExpendTest/IsolateTest.sl` A9——断言改为 `status >= 0 && status <= 5`（合法 VMIsolateStatus 枚举区间）+ 消息到达 + 句柄非 null。
- 如何快速复现：A9 改回断言单一状态值，重复运行偶发 FAIL。
- 如何快速验证：A9 连续多次运行全 PASS。
- 预防建议：P2 线程模型下一切「跨线程读状态 + 断言精确值」的测试断言都要按竞态设计——断言**合法区间**或**终态收敛**（轮询等待），不断言瞬时中间态；`recv` 返回只保证消息已入队，不保证发送方仍存活。

### [2026-09-15] ProjectTest2 `_main_` 跑完后进程忙转不退：idle_wait 的 pending_messages fast-path 遇孤儿消息永久自旋
- 现象：`_main_` 全部输出（含 `===== StreamFileTest end =====`）跑完后进程不退出，~120s 墙钟烧 444s CPU（多线程自旋）；stderr 无任何报错。
- 首个异常阶段：VM 运行期收尾——主 isolate（iso=2 main）在 `vm_scheduler_enter` drain 循环里出不来，`vm_isolate_shutdown` 从未到达（插桩确证）。
- 根因（两层）：
  1. 直接原因：`vm_isolate.c` `vm_isolate_idle_wait` 的 `pending_messages > 0` 分支**立即返回 TRUE 不等待**，且排在主 isolate 退出判定（is_main && 无 timer && workers 全退 → FALSE）**之前**——「有消息就必有人来消费」的假设被孤儿消息打破，主线程纯自旋。
  2. 上游原因：IsolateTest **T5 组按设计遗留孤儿消息**（L778-779 建两个 ReceivePort，worker 回发后仅查 `.count == 1` 不 recv，端口随之抛弃）——port=87/88 各 1 条永无消费者（vm_port create/send/recv 流水探针确证）。测试行为合法，VM 必须容忍。
- 修复点（文件/模块）：`vm_isolate.c` 删除该 fast-path 分支（留注释说明），落入正常 50ms park 路径。正确性依据：`vm_port_send` 已先 enqueue waiter 到 ready 队列再 `vm_isolate_wake_locked(owner)`，真正需要立即调度的场景被更早的 `ready_head != NULL` 分支覆盖；孤儿场景退化为低频轮询，主 isolate 在 workers 全退后走正常退出判定 → shutdown → 端口消息由 `vm_port_destroy` 兜底释放。
- 如何快速复现：还原 fast-path 分支跑 ProjectTest2（IsolateTest+ComponentTest+StreamFileTest），输出跑完即 100% CPU 自旋。
- 如何快速取证：`vm_isolate_idle_wait` 三分支 + `vm_port_create/send/try_recv` 加一行式流水探针（stderr 落盘），对账哪条 send 无对应 recv。
- 如何快速验证：ProjectTest2 全量 exit=0 干净退出、stderr 0 行；IsolateTest 全 [PASS]、StreamFileTest 19 OK / 0 FAIL（ComponentTest 仅剩既有 tryGetComponent FAIL，与本修复无关）。
- 预防建议：「立即返回 TRUE」类 fast-path 的每个分支都要能回答「谁最终会让它变 FALSE」；主 isolate 的退出判定必须对孤儿资源（端口/消息/协程）免疫；R12 限频探针（`(++hits % N) == 1`）+ stderr 落盘是自旋类问题的标准取证手法。

### [2026-09-26] 通道原语失败硬停 VM 与全仓系统方法错误模型不一致：sl_channel_fail 改走 vm_sys_throw 可捕获（需求 3 · `<-` 通道会话 v8f）
- 现象：v8d 新增 testAtSignChannelNegative（裸调 `ChannelOutInt(999)` 后断言 try/catch 捕获 -86 并恢复）后，SpecialTest 跑到该用例 VM 直接终止 exit 1，catch 体不执行；日志（实际落 `csimple_lang\build\logs\VM.txt`，**非** `out\export\...\Logs\`）显示 "AtSignChannelOutInt: no captured out value (entry 999)" + "VM finished with error_code=-86"。
- 首个异常阶段：VM 运行期（通道原语错误模型）。
- 根因（链）：
  1. v6 实现 `sl_channel_fail` 用硬错误模型：置 error_code + `is_running=0`，头注释写明"栈契约已破，无可回退"；
  2. 负例测试（v8d）前提 = 通道原语失败是 try/catch 可捕获的 VM 异常；
  3. 深层矛盾：**全仓系统方法错误模型统一为 `vm_sys_throw`**（coroutine/isolate/process 三域及插件 host `throw_error` 桥均如此）——置 error_code + `sys_throw_epoch++` + `vm_execute_throw`（有 catch 清错误码续跑 handler，无 catch 硬停），随后系统方法 `return FALSE`，`CallSystemMethod` 分发器以 epoch 快照比对区分"异常流"与"意外失败"（后者才记 Assert）；
  4. 通道三原语（ChannelIn/ChannelOutInt/ChannelOutString）在 CVM 侧就是普通系统方法，其失败理应走同一模型；唯一应保持硬错误的是哨兵 opcode 124（CallAtSignLabel 拦截路径，栈契约真正破坏）——用户决策"其它的还是走原来的流程"。
- 修复点（文件/模块）：`csimple_lang/src/vm/runtime/atsign/sl_atsign_channel.c` `sl_channel_fail` 单点改造——改调 `vm_sys_throw(vm, code, NULL)`（各调用点失败前已打具体日志，故 msg 传 NULL 防双打），仍 `return FALSE` 遵循 VMSystemFunc 契约；文件头注释 + `sl_atsign_channel.h` 系统方法原语段注释同步为可捕获错误模型。哨兵 opcode 124 及其硬错误模型不动。
- 如何快速复现：修复前跑 SpecialTest——前 27 项 PASS 后进程在第一个裸 `ChannelOutInt(999)` 处停机 exit 1（`csimple_lang\build\logs\VM.txt` 见 -86）。
- 如何快速验证：修复后重跑：exit 0，CSharpTest 26/26 PASS，含 "channel out no-session int/string: caught (BINDING -86)" + "channel negative recovered: mono 6+7 == 13"（恢复后链路完好）。
- 预防建议：① **新增系统方法的失败路径一律走 `vm_sys_throw` 统一模型**（可捕获异常），硬停仅保留给栈契约真正破坏的哨兵/opcode 层——写新域适配器前先读 `system_method_helpers.c` 的 vm_sys_throw 与 `vm_runtime.h` 的 sys_throw_epoch 文档；② 新特性的负例测试（try/catch 捕错误码）须与实现同期落地——本例正是负例暴露模型不一致；③ C VM 日志实际落 `csimple_lang\build\logs\VM.txt`（configure_export_log_path 从 exe 位置上溯 project root 定位），排障勿在 out\export 侧白找。

### [2026-09-30] spawn 统一包装闭包脱糖引入 D3/J2 实参延迟求值回归：分流脱糖修复（IsLocalScopeDefineName 作用域判据）
- 现象：为支持 `spawn 静态方法( 实参... )` 直接写法（MyTest 要求时序 a1→b1→b2→a2），把 spawn 目标统一脱糖为无参包装闭包（实参表达式原样内联进闭包体）后，MyTest 目标达成，但 BaseTest D3（1000 协程全部完成）与 J2（扇出-扇入）回归 FAIL——1000 个协程全部读到 i=1000（终值），无一取到当次迭代号。
- 首个异常阶段：VM 运行期语义（Front 编译全绿、无崩溃，纯行为回归；D3/J2 此前一直 OK）。
- 根因：**脱糖形态悄悄改变了实参求值时机语义**——函数值形式的历史路径是 `Coroutine.spawnClosureN( f, 实参... )` 直传，实参在 **spawn 点立即求值**并装箱传值（循环变量取当次值）；统一包装闭包把实参表达式内联进闭包体，求值**延迟到协程首跑**，且闭包按引用捕获循环变量 → 循环内 `spawn f(i)` 的 1000 个闭包共享同一变量，协程实际开跑时 i 已到终值 1000。
- 修复点（文件/模块）：`simple_language/source/Front/Compile/Parse/StructParseToSyntax.cs` `TransformCoroutineKeywordNodes`——按目标形态**分流脱糖**，新增判据 `IsLocalScopeDefineName`（沿 `m_CurrentNodeInfoStack` 由内向外遍历 Statements/Function 层，查已发射语句中的局部定义与方法参数；spawn 语句自身尚未入列，天然只见到使用点之前的定义，与词法作用域一致）：函数值变量（作用域内有同名局部定义：闭包/变量/方法参数）→ 分流一 `spawnClosureN( f, 实参... )` 直传（实参立即求值，D3/J2 修复路径）；裸静态方法名/实例链/this → 分流二无参包装闭包（实参延迟到协程首跑求值——裸静态方法名只能走此路：作 spawnClosureN 实参会被解析为同步调用传入 null 假成功）。
- 如何快速复现：修复前跑 BaseTest D3/J2（CoroutineTest.sl 1000 协程循环 spawn 计数）；或最小化 `for` 循环内 `spawn f(i)`，全部协程读到循环终值即中招。
- 如何快速验证：`run-tests-win.ps1 -Tests BaseTest` D3/J2 转 OK 且 K/N 组保持全绿；MyTest 时序 a1→b1→b2→a2 + probe=0/100/200（直传立即求值探针）+ sps=405（裸静态方法名带参包装闭包探针）。
- 预防建议：① `CreateStaticClassCallNode` 平铺实参进 Par childList **不补逗号**——手工拼接实参序列时函数引用与首实参之间必须显式新建 Comma 节点（复用 `CreateIsolateCommaNode`，Isolate 调用糖直传分支同款蓝本）；argCount=0 时不能带尾逗号（漏逗号=FileExpressFormatError，多尾逗号同样编译失败）；② **裸静态方法名作 `spawnClosureN` 实参会被解析为同步调用**（返回值 null 当函数值传入，静默假成功），函数值直传仅限真正的函数值变量；③ 协程探针必须核对**时序**而非单行输出——主流程被同步调用阻塞也会打印看似成功的行（probe4b 假成功教训）；④ 改变脱糖形态 = 同时改变实参求值时机（立即/延迟）与捕获方式（传值/闭包引用）两个语义维度，回归面必须覆盖**循环内 spawn**；⑤ Front 编译失败 dotnet 退出码仍为 0，判据 = module.json 是否被本次重写；残留旧 module.json 的运行输出会伪装"部分成功"，改完前端务必先确认 module.json 时间戳刷新再跑 VM。

### [2026-10-03] Net Phase 1（Tcp/Udp/NetStream 协程化网络）落地期间 NetTest 暴露的四类 C VM 缺陷（peek 装箱解箱 / connect 事件位 / 泛型接口校验 / root delay 阻塞）
- 现象：NetTest A~H 首轮流水中——G1 NetStreamComposeTest 的 datagrams() 引流协程内挂起型系统方法报 handle 参数缺失/非法；A4 连接被拒场景（TcpBasicTest）connect 完成事件被漏掉、挂到超时才报错；G1 泛型接口赋值报 "Generic reference assignment validation failed"；B1 root 协程 delay(100) 期间调度器主循环停转，worker 的 accept 唤醒死等。
- 首个异常阶段：VM（net_system_method / coroutine_system_method / runtime_object / vm_coroutine）。
- 根因：①挂起型系统方法在挂起与指令重执行间隙 peek 栈上 Int64 参数，闭包上下文数组是 Core.Array<Core.Object>，捕获的句柄经 LoadArrayIndex load 出是 scalar wrapper 的 PTR 槽（vm_array_try_load_push 对 Object 元素 push_ptr），peek 只认裸 INT64 槽即失败；②SystemTcpConnectWait 只提交 VM_NET_WRITABLE 等待，Windows WSAPoll 下非阻塞 connect 完成事件（含连接被拒）可能报 readable，漏事件；③validate 接口赋值协变校验对 target 含未解析模板参数的泛型接口类型无法证明协变，误拒合法赋值；④vm_coroutine_delay_current 对 root 协程返回 FALSE 走真阻塞 sleep，root 阻塞期间调度器主循环停转，IO 线程/timer 唤醒无法执行。
- 修复点（文件/模块）：①coroutine_system_method.c vm_coro_peek_i64_at（99-127）与 net_system_method.c vm_net_peek_i64_at（109-137）各增装箱分支（vm_object_is_scalar_wrapper + 仅接受 Int64/UInt64 wrapper + 解成员 0，模板 SystemConvertInt32 的 boxed 分支）；②net_system_method.c:312 ConnectWait 事件掩码改 VM_NET_WRITABLE|VM_NET_READABLE；③runtime_object.c 新增 runtime_type_has_unresolved_template_arg（614-649），validate 接口分支遇 target 有未解析模板参数直接放行；④vm_coroutine.c vm_coroutine_delay_current（822-854）去 root 特判，root 与 spawn 协程一样挂 timer list（注释引用 NetTest B1 / NETSTREAM_DESIGN ADR-7）。
- 如何快速复现：NetTest 对应用例——G1 NetStreamComposeTest.sl（datagrams 闭包）、A4 TcpBasicTest.sl（连接被拒端口）、B1 NetSuspendTest.sl（root delay + worker accept 并发）。
- 如何快速验证：`dotnet run --project project\CSimpleVMStdTest -- test\NetTest\<用例>.sp` 全绿；插桩清理后全量 NetTest 34/34 OK；BaseTest/ExpendTest 回归与基线一致（MonitorTest P5×7/P6×2 为调试器域遗留，与本特性无关）。
- 预防建议：①挂起型系统方法 peek 栈参数一律处理 VM_STACK_SLOT_PTR 装箱标量槽（闭包捕获/泛型容器路径必然产生）；②非阻塞 connect 完成事件按 readable|writable 双位等待（Winsock 边角）；③赋值校验对编译期无法证明的泛型协变（unresolved template arg）放行而非误拒；④root 协程是完整 VMCoroutine，任何阻塞 fallback 都会冻结调度器，挂起类原语对 root 必须走正常挂起路径。

### [2026-10-03] Net Phase 2 读写超时落地期间调度器提前退出（TcpBasicTest A2 EOF 偶失 / NetTimeoutTest T 组纯协程全超时挂起）：摘表与唤醒落地之间的 TOCTOU 窗口，单查等待表计数误判归零
- 现象：TcpBasicTest A2「对端立即关闭 → client read 得 EOF」偶发不达（进程在唤醒落地前退出）；NetTimeoutTest T 组纯协程 + 全员 setReadTimeout 挂起的用例中，到期唤醒尚未落地时调度器即判「无事可等」退出 VM，超时异常与后续断言输出丢失。
- 首个异常阶段：VM（vm_coroutine.c iso==NULL 空闲分支 / vm_isolate.c 主 isolate idle_wait 谓词 / vm_net_io.c 计数接口）。
- 根因：等待条目被 IO 线程摘除（READY 就绪 / EXPIRE 到期 / cancel）与协程唤醒落地（enqueue_ready + isolate wake）之间存在窗口——窗口内等待表计数已归零而协程尚未就绪；两处调度退出判定（纯协程 iso==NULL 守卫、主 isolate idle_wait 谓词）当时都只查等待表计数，会误判「无事可等」提前退出，唤醒还在路上而 VM 已收摊。
- 修复点（文件/模块）：vm_net_io.c 以 vm_net_outstanding_count() 取代原 vm_net_wait_count()（净在途 = s_net_count + s_net_pending_wake，锁内取一致快照；摘条目三处 +1、唤醒落地 -1，vm_net_io.h 注释为权威定义）；vm_coroutine.c iso==NULL 守卫（约 L1169）与 vm_isolate.c idle_wait 谓词（约 L1022）两处同改查净在途——同款 TOCTOU，只修一处必复发。
- 如何快速复现：NetTest TcpBasicTest.sl A2（对端即关 EOF）与 NetTimeoutTest T 组（全员超时挂起）循环运行，低概率命中摘除-落地窗口。
- 如何快速验证：修复后 NetTest 43/43 OK（T 组含超时到期唤醒、迟到数据优先于超时、连接保持可用等用例全绿）；ExpendTest / BaseTest 回归与基线一致。
- 预防建议：调度器「可否退出」类判定不得依赖单一瞬时计数——凡存在「摘除 → 落地」两段式生命周期的资源（等待表条目、消息、任务），退出谓词必须查净在途（含在途移交中数量）；且多处同型判定点必须同步修（本例两处同款缺陷，靠 grep 同名调用点收全）。

### [2026-10-07] WebSocket 全链路（cvm + Std 门面 + WebsocketTest）落地期间的六类缺陷（半开区间误用 / 101 Version 误校验 / 掩码键写入错位 / follow-key 遇注释丢块 / Close 回显被状态守卫拦截 / enum as Int32 恒 0）
- 现象：WebsocketTest 首轮流水中——客户端握手直接失败（服务端误判协议版本拒绝 101）；握手成功后对端收到的首帧载荷整体错位（掩码键被当载荷读走）；SL 侧 `else` 分支整块消失（编译期 Front 层）；关闭协商时对端只读到 EOF、closeInfo 丢失；`WsState.Open as Int32` 恒 0（状态机断言全乱）。
- 首个异常阶段：混杂——①②③⑤ 为 Std 层 Websocket.sl（语义/RFC 误用），④ 为 Front（Parse/StructParseToSyntax），⑥ 为 VM（vm_runtime 的 OpCode_CastClass）。
- 根因：① `SystemStringRange` 第三参是结束下标（半开区间 [start, end)）而非长度，握手头解析多处按"长度"传参越界；② 服务端 accept 校验了 101 响应中的 `Sec-WebSocket-Version`，而 RFC 6455 §4.2.2 规定成功握手响应不含该头（仅 426 拒绝响应携带），校验必然失败；③ 客户端发送帧构造时掩码键写在载荷之后，RFC 6455 §5.3 要求掩码键紧跟帧头先于载荷，对端按规范读帧即整体错位；④ follow-key 探测（if/elif/else、try/catch/finally）遇行注释即 break——词法层行注释连 '\n' 一起消费、注释成为块与后续关键字之间唯一分隔，'}' 块后隔注释行的 else 与主键失配、整块被静默丢弃；⑤ Close 收尾先置 Closed 再回显，而 `_writeFrame` 对 Closed 状态抛 WsError.Closed，回显帧永远发不出、对端只能读到 EOF；⑥ 整数底层枚举成员运行期是 Core.Member 包装对象，`OpCode_CastClass` 转换分支直接按 Int32 读 union 命中指针重叠得 0（与 ProcessStdio / EncodingTest 返回值同族：枚举槽位包装，第三次踩坑）。
- 修复点（文件/模块）：① Websocket.sl（握手头解析各处，L1187 注释固化半开区间语义）；② Websocket.sl L389 服务端 101 校验去掉 Version 头断言（注释说明仅 426 携带）；③ Websocket.sl L831-874 掩码键移至帧头之后载荷之前（SystemRandomFill 4 字节 → 读 m0..m3 → 异或写入 masked 再整体发送）；④ Parse/StructParseToSyntax.cs L304-319 Comment 分支镜像同函数 LineEnd 分支——condition.isFirstKey 且 keyNode 为空时 continue 跳过注释继续探测；⑤ Websocket.sl L533-541 先 slice 取回显载荷、`_trySend` 回显、再置 Closed（注释固化顺序契约）；⑥ vm_runtime.c L4840-4843 OpCode_CastClass 转换分支先 `vm_switch_try_read_enum_value` 解包再转（对齐 switch / 枚举 == / throw 三条既有解包路径）。
- 如何快速复现：`dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\Other\WebsocketTest\<sp> -e ir` 后 `csimple_lang run` 对应 module.json，观察握手失败/帧错位/else 丢失/EOF 关闭/enum 断言。
- 如何快速验证：WebsocketTest 全量 pass=33 fail=0（握手/echo/分片重组/掩码/关闭协商/错误路径/HttpClient 风格 WebSocketClient 门面四回调）；EnumTest 补 `as` 用例（`First as Int32` = 2 等三组）；StructParseToSyntax 修复后 BaseTest/ExpendTest 回归与基线一致（无新引入失败）。
- 预防建议：① 调用既有系统方法先读语义注释/实现，警惕"第三参是长度还是结束下标"类半开区间约定；② 按规范实现协议时逐条核对请求/响应各头的方向性与出现条件（101 vs 426），不凭直觉加校验；③ 帧构造字段顺序（帧头→掩码键→载荷）以 RFC 为准不凭记忆；④ follow-key/结构探测类循环对"分隔节点"（LineEnd / Comment）的处理必须对称——新增一种分隔节点即检查所有探测点是否同步跳过；⑤ 状态守卫与副作用顺序敏感的方法（close/flush 类）先完成输出再置终态；⑥ 枚举值在 VM 内统一先经解包 helper 读取，CastClass 类新增转换点先查包装家族前科。
- 遗留未修：`EBytes eb5 = EBytes.x5` 局部变量声明链报 MetaCoreParseCallLinkFailed（EnumTest 调试中发现，测试改用 EErr.Thrill 绕开，待独立排查）。

### [2026-10-17] SLangServer P1 WebSocketServer T7 连接被抢：net_slot_alloc 槽位下标即 sid、close 后立即复用——僵尸 accept 协程持旧 sid 抢走新一代监听器的连接
- 现象：ExpendTest/Server/ServerP1Test.sl T7（WebSocketServer 独立端口 19407）——T6 各代 stop 后，T7 的 wss 握手 400、accept 循环 accept-fail；插桩显示 HttpServerCore（另一形态）竟收到 T7 端口的连接（`[dbg-hc-acc] got` + 400 no-ws-route），wss 自己的 accept 报失败——两个 accept 循环争抢同一 socket。
- 首个异常阶段：VM（csimple_lang/src/lib/os/sys_net.c net_slot_alloc）。
- 根因：句柄槽位号复用——`net_slot_alloc` 按槽位下标发 sid（`sid = i+1`），close 后槽位立即回池复用；上一代 stop 关监听时 `vm_net_cancel_wait` 唤醒的僵尸 accept 协程重执行系统调用，旧 sid 已映射到复用槽位上的新 socket，`libsys_net_get_fd(sid)` 的 fd<0 失效防线被复用击穿，僵尸"合法"收走新监听器的连接（详细锁序与 cancel 语义见 vm_net_io.c 头注 §5.5）。
- 修复点（文件/模块）：sys_net.c `net_slot_alloc` 改单调推进分配——新增 `g_net_slot_hint` 游标（g_net_lock 保护），优先从上次分配点向后找空槽不回头，刚 close 的槽位尽量晚复用；僵尸协程的旧 sid 在复用发生前必然命中空槽（fd<0）走错误路径确定性死亡。全表 256 槽满才从头回收（常规负载不触达；Phase 2 generation 计数根治残余窗口）。42 处 `sid-1` 解析点零触碰。
- 如何快速复现：修复前跑 `test/ExpendTest/Server/ServerP1Test.sl` T6（多代 stop/restart）紧接 T7（独立 WS 端口），T7 握手必 400。
- 如何快速验证：修复后 ExpendTest 293/293 OK（T7a–T7e 全绿）+ NetTest 104/104 OK（P0 回归）；first-fit 对照实验（临时还原旧分配）证实 T7 必现、单调版必绿。
- 预防建议：① 协程被 cancel_wait 唤醒后重执行系统调用，句柄必须重查有效性且"失效"判定要经得起句柄号复用（本例 fd<0 防线在复用下失效）；② 句柄分配避免立即复用刚释放的号（单调游标是最小改动，generation 编码是彻底方案）；③ 排障时"两个循环争抢同一连接"类症状优先怀疑句柄复用而非调度竞态——插桩 fd/sid 对号即可实锤。
