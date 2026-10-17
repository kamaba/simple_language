# 复盘分片 07 · 标准库实现

> 收录：GZip、Encoding、Sqlite、RegExp、Process、Math 等标准库实现类复盘。
> 路由索引与追加规则见 [故障排查流程 §6](../故障排查流程.md)；新复盘按域追加至本片末尾。

### [2026-09-17] ProcessTest run 路径 stdio 配置全为指针垃圾值：ProcessStdio 枚举成员槽存 Core.Member 包装对象，直接读 i32 命中 union 指针重叠
- 现象：ProcessTest run 系用例中子进程 stdout/stderr 未被管道捕获（pipeReadAll 返回空）；win_spawn 插桩打印 `stdio={-1762344224,-1762344224,-1762344224}`，正确值应为 `{0,1,1}`（INHERIT/PIPE/PIPE）——垃圾值 -1762344224=0x9777A6E0 恰是包装对象指针低位。
- 首个异常阶段：VM（system_method_call 转发层读对象成员，process_system_method.c）。
- 根因：SL 枚举成员（`ProcessStdio extends Int32`）在 VM 对象槽位存 **Core.Member 包装对象**而非裸 Int32（EVMType 无 Enum 类目，枚举值以成员对象表示）。`vm_proc_read_i32_member` 用 `vm_object_try_load_member_value` 读出后直接 `runtime_value_get_int32`，而后者是裸 `return v->i32`——`i32` 与 `vm_object` 指针在同一 union 重叠，对象引用读 i32 即得指针低位垃圾值。`environment_system_method.c` 的 `plat_env_pop_i32` 注释早已写明标准解法：「枚举类型形参……系统方法直传时走 vm_switch_try_read_enum_value 拆出底层常量」。
- 修复点（文件/模块）：`csimple_lang/src/vm/system_method_call/process_system_method.c` `vm_proc_read_i32_member`——成员值读出后先 `vm_switch_try_read_enum_value` 尝试拆箱（枚举包装命中则整体替换为 unwrapped），再 `runtime_value_get_int32`。
- 如何快速复现：修复前跑 ProcessTest 任一 run 系用例（如 testRunBasic），观察输出捕获为空 / stdio 参数为亿级负数。
- 如何快速验证：从 `test/SpecialTest` 目录运行 `csimple_lang\build\Debug\bin\csimple_lang.exe run f:\project\lang\simple_language\out\export\SpecialTest\SpecialTest.module.json`，ProcessTest 33/33 PASS、run 系 stdout/stderr 捕获正常。
- 预防建议：①系统方法读「声明为枚举类型的成员/形参」必须先 `vm_switch_try_read_enum_value` 拆箱再取标量（参照 `plat_env_pop_i32` 模式）；②`runtime_value_get_int32` 等访问器无任何类型检查，对 Object 引用调用即读 union 重叠字段——见到「亿级正负数」先怀疑指针重叠而非数值计算错误；③动手前先读既有同类场景（environment/switch）的注释与实现，本坑在 plat_env_pop_i32 已有完整记载。

### [2026-09-17] ProcessTest testKill 断言 hasExited 立即为假且进程链中断：TerminateProcess 异步发起即返回，kill 封装缺有界等待
- 现象：`p.kill()` 后紧邻断言 `p.hasExited == true` FAIL；输出在 testKill 第二个 check 后中断（testTerminate 等后续用例未执行）——kill 未生效时紧邻的 `p.exitCode` 查询抛 NotExited 异常（-83）终止整个测试链。
- 首个异常阶段：lib 平台层（sys_process.c 的 kill 同步语义）。
- 根因：Windows `TerminateProcess`（Unix `kill(pid, SIGKILL)` 同理）**异步**——发起即返回，终止完成需内核调度；紧邻的 `WaitForSingleObject(proc, 0)` 可能尚未 signaled。而测试/设计期望 kill 具备同步语义：调用返回后状态查询即真。
- 修复点（文件/模块）：`csimple_lang/src/lib/os/sys_process.c` Windows/Unix 两侧 `sl_process_kill`——发起终止后轮询 `sl_process_has_exited`（复用其轮询 + exit_code 缓存）做 5000×1ms 有界等待；超时仍返回 0（终止已发起，最终状态留给后续查询兜底）。
- 如何快速复现：修复前跑 testKill（`p.kill()` 后立即查 `p.hasExited`）必现 false 且后续用例不执行。
- 如何快速验证：同上命令运行 SpecialTest 模块，ProcessTest 33/33 PASS（testKill 三项含 `exitCode == 1`、testTerminate 全过）。
- 预防建议：①异步平台 API（TerminateProcess/kill）后跟同步状态断言时，封装层须做有界等待（正常 <10ms，5s 仅为安全上限），勿假设「紧邻调用立刻可见」；②测试输出中断在某个 check 且后续用例不执行时，优先怀疑上一 check 触发 throw（SL 未捕获异常终止整个 fun 链），而非后续用例自身问题。

### [2026-09-17] GZipTest junk header fails：解压 sizing 遍只信 trailer ISIZE，全零垃圾被当「空载荷」放行
- 现象：GZipTest 压缩往返/破坏流三组中 `gzip junk header fails` FAIL——20 字节全零垃圾数据 `GZip.decompress` 未抛异常，静默「成功」返回空缓冲。
- 首个异常阶段：VM（core 层 `sl_core_gzip.c` decompress 的 sizing 遍）。
- 根因（两层叠加）：
  1. 转发层 `vm_bb_zlib_body` 对解压是两段式：第一遍 sizing（dst=NULL）取原长、第二遍 decode；而 `sl_core_gzip.c` 初版 sizing 遍只从 trailer 读 ISIZE 就返回——全零垃圾的 trailer ISIZE 恰为 0，返回 0 后 body 层第二遍 `dst_cap=0` 又落回 sizing 分支返回 0，SL 层拿到 id≠0 即「成功」；
  2. 与 zlib 容器不同：zlib 的 4 字节长度头由编码器产出，0 长度可信；gzip 的 trailer 是数据自描述字段，垃圾数据可伪造任意 ISIZE（含 0），sizing 遍不能作为唯一校验依据。
- 修复点（文件/模块）：`csimple_lang/src/core/sl_core_gzip.c` decompress sizing 分支——先校验 RFC 1952 magic（src[0]==0x1f && src[1]==0x8b && src[2]==0x08，CM=deflate）再返回 ISIZE；完整完整性（CRC32 + ISIZE 匹配）仍由 decode 遍 `inflate(Z_FINISH)` 强制。同时删除早期 `isize == 0` 短路（空载荷也须过 inflate 验 magic/CRC）。
- 如何快速复现：修复前 `ByteBuffer.fromBytes( 20 字节全零 )` 传入 `GZip.decompress`，不抛异常。
- 如何快速验证：GZipTest 24 项全 OK（junk header / short stream / isize mismatch 三破坏用例全部正确抛 GZipError）。
- 预防建议：①「sizing 遍返回 0」与「decode 遍 dst_cap=0」会形成两遍都走 sizing 分支的环——两段式协议里 0 必须有独立判据（本例 magic），不能只靠 trailer/头部自述值；②容器头部字段是否「编码器可信产出」决定其能否被 sizing 遍单独信任：zlib 长度头可信、gzip ISIZE 不可信（外部数据可伪造）；③解压路径的最小校验集 = magic + 方法字节（sizing 期）+ CRC/长度匹配（decode 期），缺一层即有绕过面。

### [2026-09-18] Math 库重编报「ret 带返回值表达式，但函数返回类型为 void」×5：向量类 `set` 撞属性 setter 关键字，9/15 修 void 检查时只改 Float32_2 漏其余 5 文件
- 现象：Math 标准库 Front 编译在 MetaCore 阶段报 5 个 `MetaCoreReturnStatementVoidWithExpress`（Float16_2/Float16_3/Float32_3/Float64_2/Float64_3 各一，全部指向 `set()` 方法内的 `ret this`），IR/Export 被跳过，`out\export\Math\Math.module.json` 停留旧版；`CSimpleVMMathTest` csproj 本身构建 exit 0，纯 .sl 侧失败。
- 首个异常阶段：**MetaCore（Front）**，非 VM/导出问题——`out\export\Math\Logs\Front.txt` 最早 Error 即上述 5 条。
- 根因（链）：
  1. `set` 在词法层是关键字（`LexerParseToToken.cs` case "set" → `ETokenType.Set`，属性 setter 用），`Float32_3 set(...)` 被按 setter 解析，返回类型被强制为 void；
  2. 9/15 新增的 void ret 检查（`MetaReturnStatements.cs`，见同日「void 函数 ret 带值」条目）开始拦截 `ret this`；
  3. 当天只改了 `Float32_2.sl`（`set`→`setValue`），漏掉其余 5 个仍带非 void `set` 的向量文件（Quaternion/Rect 同型但 `ignore: true` 不参与编译，未动）。
- 修复点（文件/模块）：5 处方法改名对齐全库既有 `setValue` 命名（矩阵类/Float32_2/Int32_2/Int32_3 均如此）：`Float16_2.sl`、`Float16_3.sl`、`Float32_3.sl`、`Float64_2.sl`、`Float64_3.sl`。改前 grep 确认全仓无 `.set(` 调用指向向量类型（Tensora/Render 的 `.set(` 是各自类声明的 void `set`，void 不触发本检查）。
- 如何快速取证：`Logs\Front.txt` 看最早 Error 行号反查 `.sl`；对照 `LexerParseToToken.cs` 关键字表确认 `set`/`get` 是保留字。
- 如何快速验证：`dotnet run --project source\CompileMath\...` 出现 `ExportSLModulePackageExportModuleSuccess`（Math.module.json 重新生成）；`dotnet run --project project\CSimpleVMMathTest` 端到端全 [PASS] exit 0。
- 预防建议：语言关键字（get/set 等）不可作普通方法名，标准库/测试新增成员名先过词法关键字表；改名类修复必须 grep 全库找同型方法收齐一次改完（本次即 5 处漏改的复利）。

### [2026-10-08] Sqlite3Test 3 FAIL「无数据」：ttest 资源库本为空，test1 表从未创建 + Sqlite.sl prepare 失败静默吞错放大隐蔽性
- 现象：ProjectTest 中 Sqlite3Test 的 fetchall/fetchone/条件查询三项 FAIL（0 行）；插入段 rowid 恒 0 却打「[PASS] 插入 3 条数据」；testCommand（test2 表）路径全部正常。
- 首个异常阶段：非编译期——SL 库层吞错。sqlite3_prepare_v2 返回 NULL stmt 被静默放行。
- 根因：双重问题。① `Resources/ttest` 本是空库（Python sqlite3 查 sqlite_master = `[]`；文件内 test2/"hello" 字节仅为 DROP 后页面残留，勿误判 DROP 失效），而 Sqlite3Test.sl 全文件只 CREATE 过 test2，**test1 表从未被建**，所有对 test1 的语句 prepare 失败（no such table）；② `Std/DB/Sqlite.sl` `_prepareAndBind` 中 prepare 失败（handle=0）直接 `ret` 无任何提示，下游 fetchone/changes/lastInsertRowid 全部返回空/0，且 `execute` 链式不报错，形成「插入 PASS rowid=0」的假阳性。
- 修复点（文件/模块）：① `test/ExpendTest/Sqlite3Test.sl` testInsert 开头补 `CREATE TABLE IF NOT EXISTS test1 (uid INTEGER, name TEXT)`（置于 DELETE 前，后续测试依赖此顺序）；② `source/Front/Lib/Std/DB/Sqlite.sl` `_prepareAndBind` prepare 失败分支补 `Console.println("[Sqlite3] SQL 准备失败: " + SystemSqlite3Errmsg(this._dbHandle))`，不再静默。
- 如何快速复现：空库 ttest + 修复前 Sqlite3Test 跑 `csimple_lang run out\export\ProjectTest2\ProjectTest.module.json`，观察 rowid=0 与三个「无数据」FAIL。
- 如何快速验证：重编 Std → 重编 ProjectTest → 同命令运行：Sqlite3Test 8 项全 PASS（rowid=1/2/3、fetchall 3 行、条件查询 2 行、UPDATE/DELETE 影响行数 1/1）；全套 127 PASS / 0 FAIL。
- 预防建议：① 用例依赖的库表应在用例内自建（IF NOT EXISTS 幂等），不依赖外部资源预置态；② 包装层对底层失败句柄（stmt=0/db=0）禁止静默 ret，至少打 errorMessage（本项目已有 `SystemSqlite3Errmsg` 先例：Connection.errorMessage()）；③ 判定 SQLite 真实 schema 用 `select name from sqlite_master`，文件字节 dump 会把 DROP 残留误读为有效表。

### [2026-10-08] RegExp 引擎下沉 C 期间的三类坑：git 历史空类致基线采集失败 + 编译期/运行时 Std 版本错位出假数据 + PowerShell -First 管道截杀 dotnet 假失败
- 现象：RegExp 回溯解释器下沉为单个 C 系统调用 `SystemRegExpRun`（regexp_system_method.c）后采 SL 基线：① `git stash` 切回 HEAD 版 RegExp.sl 重编 Std 后，BenchMark 编译报 `MetaCoreCallNotSupportNotSu / MetaCoreParseCallLinkFailed`（RegExpBench.sl 的 `Text.RegExp(...)` 构造调用处）；强行运行 BenchMark 输出全空、各 case 耗时近 0（假数据）；② CompileStd 明明在跑（Front.txt 更新）却 exit -1 且 module.json 未重写。
- 首个异常阶段：① Front/MetaCore（引用类不存在该构造）；② 无产品异常——排障工具链自身。
- 根因：① **RegExp 整个实现只存在于未提交的工作区**——HEAD 版及其全部 git 历史里 RegExp.sl 都只是 4 行空类（`public class RegExp {}`），stash 切走后下游 BenchMark 引用的构造/方法全部不存在，编译必然失败；而「BenchMark module.json 是新 Std（空类）编不出的旧产物 + 运行时加载磁盘上另一版 Std.module.json」的版本错位组合产出全空假数据；② `dotnet run ... | Select-String ... | Select-Object -First 5` 的 `-First` 取满后提前 dispose 上游管道，把 dotnet 进程中途杀掉（PowerShell 经典行为），exit -1 并非编译失败。
- 修复点（文件/模块）：基线方案改为不切版本——在当前 RegExp.sl 内新增 `_runOnSl`（驱动语义照抄 C 侧 vm_sys_regexp_run：逐起点尝试 + 每起点新建 RegExpState），两处调用点临时切 `_runOnSl` 采完基线再切回 `_runOnCvm`（`_runOnSl` 保留作对照工具）。
- 如何快速复现：① `git stash push -- source/Front/Lib/Std/Text/RegExp.sl` 后 CompileStd 再编 BenchMark，即见 Call link parse failed；② `dotnet run --project xxx | Select-String xxx | Select-Object -First 1` 观察进程被截杀。
- 如何快速验证：基线数据（Release，84KB 文本）：search miss x20 SL 170308ms vs C 33ms（≈5161x）；matchAll 44073 vs 4003（11x）；replace 45678 vs 6354（7x）；backref 22803 vs 2433（9x）；search hit 460 vs 36（13x）；group 109 vs 21（5x）；utf8 95 vs 6（16x）；compile 两版同为 ~3.4s（模式解析仍在 SL，符合预期）。切回 C 路径重编后 sanity 数据与首轮一致，输出值逐项相等。
- 预防建议：① **改动依赖 Std/Core 等基础模块的库文件时，先确认该文件的 git 提交态**——若整文件未提交（历史仅空壳），禁用 stash/checkout 类版本切换操作，改用文件内开关（双路径函数）做 A/B 对比；② 「测试模块 module.json 引用声明」在编译期冻结、「运行时加载磁盘 module.json」是两份快照——Std 换版后必须重编所有引用它的测试模块再跑，否则方法表错位会以「输出全空/耗时近 0」这类假数据形式出现，比报错更危险；③ 长跑编译命令的输出过滤禁用 `Select-Object -First N`（会截杀上游进程），用 `> file` 落盘后二次 Select-String；④ 根目录 `scripts\compile-tests.ps1` 在仓库根而非 simple_language 下，嵌套 powershell -File 传相对路径会静默失败，一律用绝对路径。

### [2026-10-16] RegExp P4-B 编译下沉首版 31 FAIL：cpack 扁平数组里 cs 段表偏移误用「绝对下标」，SL 侧拷出子数组后所有 class 判定读错段
- 现象：`SystemRegExpCompile` 落地后 RegExpTest 250 断言 31 FAIL，失败集中在 search/isMatch/全部 class 系（`[a-z]` `\d` `\w` `\s` 等）；字面量与量词组正常（`match anchored at head : OK`）。
- 首个异常阶段：VM 运行期（C 侧数据装配错位，非 Front）。
- 根因：C 侧把 `[errCode, groupCount, guardCount, progLen, prog.., csFlat..]` 打包进一个 `Array<Int32>` 返回，SL 侧拷出 prog/cs 两个**子数组**再交给运行入口。cs 段表格式约定 `[segN, 各段偏移..., 段数据...]` 且偏移**相对 csFlat 起点**（与 SL `_flatCs()` 一致），但首版 C 实现按「整个 cpack 数组的绝对下标」填偏移（多算了 `4+progLen` 头前缀）→ `sl_re_cs_contains` 的 `cs_flat[1+idx]` 读到错误偏移 → class 指令全判错。
- 修复点：`vm_sys_regexp_compile` 的 cs 展平改为 `off = 1 + cs_count`（相对段表起点），段数据写入统一 `base + off + k`；数据本身仍在 cpack 尾部，只是偏移语义对齐 SL 契约。修复后 250/0。
- 如何快速复现：`SystemRegExpCompile` 打包逻辑把段偏移写成 `base + 1 + seg` 形式（绝对）即触发。
- 如何快速验证：RegExpTest 全绿（class 系断言全部恢复）+ BenchMark `matchAll count=12000`。
- 预防建议：**跨边界的扁平格式里，偏移/长度一律声明清楚「相对谁」**——打包头（errCode 等）与段表共数组时尤其易错；C 侧生成、SL 侧消费的双向格式应在注释里写明坐标系，并在首批断言里覆盖「带 class 的 search」这类读段表路径（纯字面量模式测不出）。
