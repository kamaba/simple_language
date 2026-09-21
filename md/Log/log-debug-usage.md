# SLang.Log / SLang.Logger / SLang.Debug / Core.Debug 使用指南

> 适用范围：两套日志/调试设施、四个类。
> 源码位置：`source/Front/Lib/Std/Debug/Log.sl`（Log、Logger）、`source/Front/Lib/Std/Debug/Debug.sl`（SLang.Debug）、`source/Front/Lib/Core/Debug.sl`（Core.Debug 点监视系统）。
> 本文同时是「用 Log/Debug 加速功能点验证」的调试工作流参考。
> Core.Debug 的完整设计（含 payload 布局、分期 P1~P8）见 `csimple_lang/md/design/DEBUG_SYSTEM_DESIGN.md`（v4）。

---

## 1. 四个类的定位与选择

| 类 | 形态 | 定位 | 典型场景 |
|----|------|------|----------|
| `SLang.Log` | 静态全局 | 级别化运行日志（info/warn/error/fatal + 时间戳 + 可选文件落盘） | 一般业务/运行期记录 |
| `SLang.Logger` | 实例 | 同 Log，但每实例独立配置 | isolate / 协程中各持一份互不干扰的日志器 |
| `SLang.Debug` | 静态 | 开发期调试手段（显式作用域追踪/断言/计时/断点标记） | 定位问题、性能分析 |
| `Debug`（Core 模块） | 静态 | **点监视采样系统**：watch 落帧进环形缓冲，事后查询 / 实时监听 / 栈查看 | 观察一个值在执行过程中的演变、不变量断言、调用链过滤采样 |

> 注意两个 Debug 是不同的类：`SLang.Debug`（Std 模块，显式 Enter/Exit 追踪）与 `Debug`（Core 模块，采样式点监视）。后者不带 `SLang.` 前缀直接可用（Core 模块默认引用）。

与 `Console` 的区别：`Console` 是裸打印（`write`/`println`/`input`），无级别、无文件落盘；Log/Logger 是带级别 + 可选文件落盘的结构化输出；Core.Debug 是采样进缓冲、事后可查询的调试器设施。

Log/Logger 每条日志行格式（两个前缀块可独立开关）：

```
[yyyy:MM:dd hh:mm:ss ffff] [LEVEL] message
```

---

## 2. SLang.Log（静态全局日志）

### 2.1 输出方法

| 方法 | 级别 | 受控开关 | 说明 |
|------|------|----------|------|
| `Log.verbose(msg)` | VERBOSE | info 开关 | 最细粒度（= debug） |
| `Log.debug(msg)` | DEBUG | info 开关 | |
| `Log.info(msg)` | INFO | info 开关 | |
| `Log.warning(msg)` | WARN | warning 开关 | |
| `Log.logError(msg)` | ERROR | error 开关 | 方法名 logError：`error` 为 Result 语义保留名 |
| `Log.fatal(msg)` | FATAL | **始终输出** | **触发 VM 硬停**（见 §3） |
| `Log.exception(msg)` | EXCEPTION | error 开关 | 异常信息 |
| `Log.exception(msg, detail)` | EXCEPTION | error 开关 | 附详情（缩进拼接） |
| `Log.log(level, msg)` | 按 level 映射 | 按级别 | 通用入口，level 数值见下 |
| `Log.print(msg)` | 无前缀 | 控制台开关 | 实时原始打印 |
| `Log.assert(flag, msg)` | ASSERT | flag=false 时输出 | 断言（不触发停机） |

级别数值（`log`/`setMinLevel`/`isEnabled` 共用）：`0`=verbose/debug，`1`=info，`2`=warning，`3`=error，`4`=fatal。

### 2.2 级别开关

```sl
# 按最小级别一次性开关：level <= 阈值才输出
SLang.Log.setMinLevel(2)          # 只留 warning 及以上（info 被静默）

# 逐级开关：(info级, warning级, error级)
SLang.Log.setLogType(false, false, true)

# 查询某级别是否启用
bool on = SLang.Log.isEnabled(2)

# 恢复默认（全部级别开、停文件输出、恢复实时控制台、默认时间格式、立即落盘）
SLang.Log.reset()
```

注意：Verbose/Debug 跟随 info 开关；Fatal/Exception 跟随 error 开关但 fatal 输出无视开关（`_emit("FATAL", msg, true)`）。

### 2.3 文件落盘

```sl
SLang.Log.setFilePath("run.log")      # 设置路径并立即启用文件输出（不存在则创建）
SLang.Log.setFlushInterval(60)        # 缓冲模式：60 秒内不落盘（默认 0 = 每条立即写）
SLang.Log.save()                      # 手动立即落盘（缓冲模式退出前建议调用）
SLang.Log.save("next.log")            # 落盘并切换后续输出文件
SLang.Log.closeFile()                 # 停止文件输出（控制台照常）
SLang.Log.clearFile()                 # 清空日志文件（已设路径时）
```

### 2.4 显示与控制台配置

```sl
SLang.Log.setShowTime(false)          # 关 [时间] 前缀块
SLang.Log.setShowLevel(false)         # 关 [LEVEL] 前缀块
SLang.Log.setTimeFormat("hh:mm:ss")   # 时间格式占位符：yyyy/MM/dd/hh/mm/ss/ffff
SLang.Log.setConsoleOutput(false)     # 关实时控制台输出（仅落盘）
```

---

## 3. Log.fatal 的 VM 硬停语义（重点）

`Log.fatal` / `Logger.fatal` 表示**不可恢复错误**，触发后 VM 立即硬停：

- **触发条件**：`SystemLogEmit` 的 level 参数为 `"FATAL"`。检测在级别开关**之前**——即使 `setLogType` 关掉了 error 级，fatal 依然停机（级别开关只管日志输出，不影响停机语义）。
- **不可截获**：硬停不走异常路径，`label/try/catch` 无法截获。
- **后续语句不执行**：fatal 调用之后的任何代码（含同函数后续行、外层调用链）不再运行。
- **进程退出码**：VM 内部错误码 `-91`（日志致命停机），宿主进程退出码 `1`。
- **协程安全**：置 `fatal_halt` 后所有执行复活点（调度器续跑、native 包装循环、协程唤醒、调用帧返回）均被拒绝，任何协程触发 fatal 都会停掉整个 VM。

实现链路：`Log.fatal` → `_emit("FATAL", ...)` → 系统调用 `SystemLogEmit` → C 层 `vm_sys_log_emit`（`csimple_lang/src/vm/system_method_call/log_system_method.c`）→ `vm_request_fatal_halt(vm, -91, msg)`（`vm/runtime/vm_runtime.c`）→ `vm->fatal_halt = 1` → 主循环退出。

与 `Log.assert` 的区别：assert 失败只输出 `[ASSERT]` 行，**不停机**；fatal 停机且不可恢复。

---

## 4. SLang.Logger（实例日志器）

每个实例独立配置，可在 isolate / 协程中分别持有，互不干扰（与全局静态 Log 正好相反）。

```sl
SLang.Logger lg = new()
lg._init_(true)            # 显式初始化；true = 开启实时控制台输出（默认）
lg.setFilePath("iso1.log")
lg.setMinLevel(2)
lg.info("from isolate #1") # 受实例开关控制，不影响全局 SLang.Log
lg.fatal("boom")           # 实例 fatal 同样触发 VM 硬停（全局语义）
```

API 与静态 Log 一一对应（`setLogType/setFilePath/setConsoleOutput/setShowTime/setShowLevel/setTimeFormat/setFlushInterval/save/closeFile/clearFile/reset/setMinLevel/isEnabled/print/verbose/debug/info/warning/logError/fatal/exception/log/assert`），只是成员方法而非静态方法。

---

## 5. SLang.Debug（显式作用域追踪）

总开关：`Debug.enabled = false` 时所有输出/断言静默（等价 Release 剔除）。

> VM 未暴露真实调用栈回溯，本类用 `Enter/Exit` 维护**模拟调用栈**；要看真实栈请用 Core.Debug 的 `frames()/frameData()`（§6.7）。

### 5.1 基础日志（复用全局 Log，保持单一输出格式）

```sl
SLang.Debug.log(msg)       # = Log.info
SLang.Debug.warn(msg)      # = Log.warning
SLang.Debug.err(msg)       # = Log.logError
SLang.Debug.logError(msg)  # = Log.logError
```

### 5.2 作用域追踪

```sl
SLang.Debug.Enter("ProcessData")   # 入栈 + 缩进
    SLang.Debug.Step("load")       # 逐步标记（常用于循环/长流程）
    SLang.Debug.Step("compute")
    SLang.Debug.trace("reached step 3")   # 单条追踪（带时间戳）
SLang.Debug.Exit("ProcessData")    # 出栈
SLang.Debug.DumpStack()            # 打印当前模拟调用栈
```

### 5.3 断言与断点

```sl
SLang.Debug.Assert(x > 0, "x must be positive")   # 失败：输出 + DumpStack（不停机）
SLang.Debug.AssertNotNull(obj, "obj must not be null")
SLang.Debug.Break()   # 打印 [BREAK] 标记 + 作用域栈（当前无真实断点 API，仅定位辅助）
```

### 5.4 计时（性能分析）

```sl
SLang.Debug.time("phase1")
# ... 被测代码 ...
SLang.Debug.timeEnd("phase1")     # 输出 [TIME] phase1: <N> ms；key 未启动时输出 <not started>
```

---

## 6. Core.Debug 点监视（Debug 采样系统）★

**定位**：调试器设施。在代码里埋监视点，执行到点时把值快照采样进 VM 内环形帧缓冲（1024 帧），事后查询、相邻帧 diff、按标签/区间检索、实时监听回调。与打印式日志的本质区别：**不刷屏、不打断执行流、事后可回看**。

**链路**：`.sl` 调 `Debug.*` → Front 特译为 Debug IR（`compile.debug=true` 才生成，见 §6.2）→ `*.module.json` → C VM 分派（`vm/runtime/debug/vm_debug.c`）→ 落帧 / 查询 / 触发监听。

### 6.1 三级开关矩阵（先懂开关再用）

| # | 开关 | 层级 | 效果 |
|---|------|------|------|
| 1 | `compile.debug`（jsonc `compile` 段） | 编译期 | `false` → **采样类调用点整体消除**（watch 族 / begin / end / mark / log，连参数求值一起消失）；查询类保留，运行期返回空 |
| 2 | `optimizeLevel >= 2` | VM 运行期 | Debug opcode 走快速路径（只弹栈保栈平衡），不落帧、不报错；查询返回空/0；监听不触发。同一份模块在 `<o2` 调试、`>=o2` 正跑，无感降级 |
| 3 | 采样软开关 | VM 运行期 | `pause()/setSkip(n)/disable(tag)`：开了 debug 但指定点不采 |

| compile.debug | optimizeLevel | 结果 |
|---------------|---------------|------|
| true | 0 / 1 | 正常采样、查询、监听 |
| true | ≥ 2 | VM 无视（快速路径），执行结果与无 Debug 完全一致 |
| false | 任意 | 采样调用点不存在；查询返回空 |

> BaseTest 的 `ProjectTest.jsonc` 即 `debug:true` + `optimize:false`，是本系统的启用范本。

### 6.2 监视点采样（watch 族，特译 opcode 121）

```sl
# 单值监视（mode 0）：标量/字符串/data 整表/class 实例
Debug.watch( answer, "answer" )          # 落帧 "[watch] answer = 42"
Debug.watch( name, "name" )              # 落帧 "[watch] name = hello"

# 成员监视（mode 1）：data/class 单成员
Debug.watch( sd, "math", "sd.math" )     # 落帧 "[watch] sd.math = 95"
#        目标   成员名   标签

# 断言监视（mode 2）：cond=false 时落违规帧并计数
Debug.watchAssert( value, "ax", value >= 50 )
# cond=true  → 照常落 "[watch] ax = 100"
# cond=false → 落 "[assert] axbad = -1"（kind=3），assertViolations() 累加

# 调用链过滤（mode 3）：仅在调用链含 caller 时采样（方法名或类型全名子串）
Debug.watchIn( 5, "wix", "inTarget" )    # 只在 inTarget 调用链上落帧
```

### 6.3 区间与日志（特译 opcode 119/120）

```sl
Debug.begin( "p2main" )                  # 进入区间（压入区间链）
    Debug.begin( "p2loop" )
    Debug.scopeChain()                   # "p2main > p2loop"
    Debug.end( "p2loop" )                # 退出区间（计入耗时统计）
Debug.end( "p2main" )
Debug.stats( "p2main" )                  # 区间耗时统计 JSON

Debug.mark( "m1" )                       # 行边界标记（帧表行轴用）
Debug.log( "reached here" )              # 日志帧：自动附 method:line + 调用链摘要
Debug.getTraceLog()                      # 日志流 "#<行> <method>:<line> | <链> | <msg>"
```

- 区间支持嵌套；被调方法内 `begin` 未配对 `end` 时，throw 后由 VM 帧弹出**自动回卷**（编译期另给 `DebugScopeNotClosed` warning 兜底提示）。
- `Debug.log` 受 pause/skip 采样语义控制（静默期丢弃）。

### 6.4 采样控制

```sl
Debug.setSkip( 2 )      # 全局：前 2 次命中不采样（预热跳过）
Debug.pause()           # 全局软暂停采样（查询类不受影响）
Debug.resume()          # 恢复
Debug.enable( "tag" )   # 单监视点启用
Debug.disable( "tag" )  # 单监视点停用
```

### 6.5 查询 API

| 方法 | 返回 | 说明 |
|------|------|------|
| `frameCount()` | Int32 | 有效帧总数（o2/关 debug 时 0） |
| `getFrame(index)` | string | 按索引回看帧文本；越界返回 `""` |
| `lastFrame()` | string | 最新帧文本（监听回调配套快取） |
| `diff(index)` | string | 第 index 帧与前帧对比（仅 data/class 整对象帧；行级 `-旧/+新`） |
| `find(tag, scope="")` | Array\<Int32\> | 按标签（+可选区间链前缀）找帧索引列表 |
| `series(tag)` | Array\<string\> | 某标签的值序列（画帧表用） |
| `stats(tag)` | string | 区间耗时统计 JSON |
| `scopeChain()` | string | 当前区间链，如 `"main > loop"` |
| `assertViolations()` | Int32 | watchAssert cond==false 累计次数 |
| `getTraceLog()` | string | Debug.log 日志流文本 |
| `frameTable(labels)` | string | 帧表：行=mark 边界、列=labels、单元格=该行处该 label 最近一帧的**纯值**（无 `[watch]` 前缀；无匹配 `-`） |
| `setMaxTextLength(n)` | void | 帧文本截断上限（超长标注 `...(len=N)`；`<=0` 恢复默认 256） |

帧表输出示例：

```
row | p6w | p6j
1   | -   | -
4   | 11  | -
5   | 22  | -
6   | -   | 2
```

### 6.6 监听回调（落帧实时推送）

```sl
int hit = 0
function onFrame()
{
    hit = hit + 1
    string fresh = Debug.lastFrame()   # 回调内即刚落的帧
}
int id = Debug.listen( onFrame )           # 全帧订阅，返回订阅号（1 起）
int id2 = Debug.listen( "p4tag", onTag )   # 单标签订阅
Debug.unlisten( id )                       # 退订成功 true；不存在/已退订 false
```

- 监听表容量 **16**，表满再订阅返回 `-1`。
- **重入保护**：回调内再 watch 只入缓冲，不递归触发通知。
- 违规帧（watchAssert kind=3）与 mark 帧同样通知订阅者。

### 6.7 栈查看（真实 VM 栈，非模拟）

| 方法 | 返回 | 说明 |
|------|------|------|
| `stackView()` | string | 当前**操作数栈**快照：每槽 `{index,type,value}` 的 JSON 数组；无栈 `[]` |
| `frames()` | string | 调用帧列表 `[{index,method,line}]`（栈顶→栈底）；协程内只列本协程链 |
| `frameData(depth)` | string | 第 depth 层调用帧（0=当前执行帧）locals+args 行；越界 `[]` |
| `frameVar(depth, name)` | string | 某帧上指定变量/参数值文本（先 locals 后 args）；未找到 `""` |

```sl
# 表达式上下文中能看到已压栈的左操作数
string sv = pre + Debug.stackView()
# 例: "x[{\"index\":0,\"type\":\"ptr\",\"value\":\"str:x\"}]"
```

### 6.8 关键语义

- **快照克隆**：帧持有值的克隆副本，watch 之后修改原值不影响已落帧。
- **环形缓冲**：容量 1024 帧，满后覆盖最旧帧，`frameCount()` 封顶。
- **越界/未找到降级**：`getFrame` 越界 `""`、`frameData` 越界 `[]`、`frameVar` 未找到 `""`，不报错。
- **diff 仅对象帧**：mode 0 整对象 watch 的帧才进对比（成员帧是标量）。
- **watchIn 匹配**：caller 可为方法名或类型全名（declaring type full name 子串）；注意 static 小方法在 `-O1+` 可能被内联导致链上无帧、匹配失败——需过滤时用实例方法承载 watchIn。

### 6.9 最小示例

```sl
# 前提：工程 jsonc compile 段 debug=true、optimize=false
data ScoreData
{
    math = 0
}

WatchDemo
{
    static fun()
    {
        Int32 answer = 42
        Debug.watch( answer, "answer" )        # 落帧 "[watch] answer = 42"

        ScoreData sd = ScoreData()
        sd.math = 95
        Debug.watch( sd, "math", "sd.math" )   # 落帧 "[watch] sd.math = 95"

        # 循环里观察值演变（不刷屏，全部进缓冲）
        Debug.begin( "loop" )
        int i = 0
        while (i < 5)
        {
            Debug.watch( i * i, "sq" )
            i = i + 1
        }
        Debug.end( "loop" )

        # 事后查询
        Console.println("frames = " + Debug.frameCount().toString())
        Console.println("last   = " + Debug.lastFrame())
        Array<Int32> hits = Debug.find( "sq" )
        Console.println("sq hit count = " + hits.length.toString())
        Console.println("stats  = " + Debug.stats( "loop" ))

        # 不变量：值恒 >= 0，违反则计违规
        Debug.watchAssert( answer, "answer>=0", answer >= 0 )
        Console.println("violations = " + Debug.assertViolations().toString())
    }
}
```

---

## 7. 调试工作流：用 Log/Debug 加速功能点验证

验证一个功能点时的推荐套路：

1. **打点**：入口 `SLang.Debug.Enter("功能点名")`，关键分支 `Step(...)`/`Log.debug(...)`，出口 `Exit(...)`；长流程用 `Debug.time/timeEnd` 圈出耗时段。
2. **点监视**：怀疑某个值不对时，在关键位置 `Debug.watch(值, "标签")` + 事后 `Debug.lastFrame()/find/series/frameTable` 回看演变，比打印更省事（不刷屏、可 diff、可按标签检索）。
3. **不变量**：把「预期」写成 `Debug.Assert(实际 == 预期, ...)`（输出式）或 `Debug.watchAssert(值, 标签, 条件)`（采样式，事后 `assertViolations()` 一眼看出违反次数）。
4. **看真实栈**：意外路径到达时 `Debug.frames()/frameVar(0, "变量名")` 直接看调用链与帧上变量，不必层层加打印。
5. **门控**：临时打点用 `Log.debug`（`setMinLevel(2)` 一键静默）；整段调试代码用 `Debug.enabled` 总开关；点监视用 jsonc `compile.debug` 编译期整句消除。
6. **留痕**：复现偶发问题时 `setFilePath("repro.log")` + `setFlushInterval(60)` + 结尾 `Log.save()`（或用 fatal 硬停兜底，日志在停机前已实时输出）。
7. **兜底**：探查「不该到达的路径」用 `Log.fatal("unreachable: ...")` —— 直接硬停，退出码 1，一眼定位。

---

## 8. 测试用例与运行方式

### 8.1 用例位置

| 用例 | 覆盖 |
|------|------|
| `test/BaseTest/DebugTest.sl` | **Core.Debug 点监视全量验收**（P1~P6）：单值/字符串/成员/整表 watch、帧查询、截断标注、环形覆盖、区间嵌套与异常回卷、采样控制三路径、find/series/stats/diff、watchAssert 违规计数、watchIn 调用链过滤、listen 订阅时序/重入保护/表满、stackView/frames/frameData/frameVar、log/getTraceLog/frameTable |
| `test/ExpendTest/LogTest.sl` | 三级别输出、assert、级别开关（setMinLevel/setLogType/reset/isEnabled）、Logger 实例独立性、SLang.Debug 工具（err/Assert/time）、显示配置（showTime/showLevel/timeFormat）、文件缓冲 + save |
| `test/ExpendTest/LogFatalTest.sl` | fatal 硬停：后续语句不执行、try/catch 不可截获、进程退出码 1 |

### 8.2 LogFatalTest 核心片段

```sl
LogFatalTest
{
    static fun()
    {
        Console.println("---- LogFatalTest start ----")
        SLang.Log.info("before fatal: this must appear")

        label fatalBlock
        {
            # try 标记也无法截获：FATAL 是硬停（fatal_halt），不走异常路径
            try SLang.Log.fatal("fatal halt: VM must stop here (not catchable)")
        }
        catch
        {
            Console.println("[FAIL] fatal must NOT be catchable")
        }

        Console.println("[FAIL] this line must NOT appear (VM halted)")
    }
}
```

预期结果（已验证）：

1. `before fatal` 一行 info 正常出现；
2. `[FATAL] ...` 一行出现；
3. 之后无任何输出（catch 块与末尾 println 均不执行）；
4. 进程退出码 = 1。

注意：LogFatalTest 必须放在 `_main_` 调用链**最末尾**（fatal 终止整个进程，会截断后续用例）——`test/ExpendTest/ProjectTest.sp` 中已如此安排。

### 8.3 运行方式

```powershell
# 编译 ExpendTest 用例集（必须带 -e ir 才会导出 module.json）
cd simple_language
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\ExpendTest\ProjectTest.sp -e ir

# C VM 直接运行
csimple_lang\build\Debug\bin\csimple_lang.exe run out\export\ProjectTest2\ProjectTest.module.json
```

- DebugTest 所在的 BaseTest 用例集：联合测试宿主 `project/CSimpleVMCoreTest`（默认用例集即 BaseTest）。
- ExpendTest 用例集：联合测试宿主 `project/CSimpleVMStdTest`。
- **跑 DebugTest 的前提**：`test/BaseTest/ProjectTest.jsonc` 中 `compile.debug=true` 且 `optimize=false`（当前已如此配置）。

---

## 9. 底层实现速查

| 层 | 位置 | 说明 |
|----|------|------|
| SL API（Log/Logger） | `source/Front/Lib/Std/Debug/Log.sl` | 全部方法；`_emit`/`assert` 下沉 C 原生 |
| SL API（SLang.Debug） | `source/Front/Lib/Std/Debug/Debug.sl` | 显式作用域追踪/断言/计时 |
| SL API（Core.Debug） | `source/Front/Lib/Core/Debug.sl` | 点监视门面；采样类由 Front 特译 |
| 系统调用注册（Log） | `source/Front/Lib/Std/Std.jsonc` `systemCalls[]` | `SystemLogEmit`/`SystemLogAssert`/`SystemLogSave` |
| 系统调用注册（Debug） | `source/Front/Lib/Core/Core.jsonc` `systemCalls[]` | `SystemDebugWatch` 族查询类调用 |
| C# 枚举 | `source/Front/Define.cs` `ESystemMethodCall` | 与 cvmFunction 对齐（R7 四处同步） |
| Debug 特译 | `source/Front/IR/IRCall.cs` `ParseDebugWatchCall` | watch 族→opcode 121（mode 0~3）、begin→119、end→120 |
| opcode 对齐 | `Front/IROpEnum.cs` ↔ `csimple_lang/src/vm/vm.h`（R2） | `DebugBegin=119` `DebugEnd=120` `DebugWatch=121` |
| C 实现（Log） | `csimple_lang/src/vm/system_method_call/log_system_method.c` | `vm_sys_log_emit` 等；FATAL 特判在此 |
| C 实现（Debug 采样） | `csimple_lang/src/vm/runtime/debug/vm_debug.c/.h` | 环形帧缓冲 + 区间链 + 统计 + 订阅表 + 栈查看 |
| FATAL 硬停 | `csimple_lang/src/vm/runtime/vm_runtime.c` `vm_request_fatal_halt` | 置 `fatal_halt` + 错误码 -91；复活点防护散布 `runtime_call.c`/`vm_call_frame.c`/`vm_coroutine.c`/`vm_runtime.c` |

修改注意：

- FATAL 语义：级别检测置于 `enabled` 开关之前；`fatal_halt` 置位后不得被任何路径复位。
- Debug 采样：`optimizeLevel >= 2` 快速路径**必须保持栈平衡**（按 mode 弹栈：mode 0/1/3 弹 1 槽、mode 2 弹 2 槽）；`compile.debug=false` 时采样调用点连参数求值一起消除。
- 设计全貌（payload 布局、P1~P8 分期、Watcher 预留）：`csimple_lang/md/design/DEBUG_SYSTEM_DESIGN.md`。
