# @csharp_mono(){} 内联 C# 块

> **本文档以「当前实现」为准**（IR 路线已落地，2026-09-25），对应五层：
> Front 圈地层（语言无关通用改写器）`source/Front/Compile/Parse/AtSignLabelSourceRewriter.cs`
> + 反射宿主 `source/Front/Compile/Parse/PluginFrontendParser.cs`；
> 插件解析器（全部块体解析逻辑）`SLPlugin/csharp_mono/src/frontend/SLCSharpMonoFrontend/SLLabelParser.cs`
> （经 `plugin.jsonc` 的 `frontendLibs` 清单反射转调）；IR 拦截
> `source/Front/IR/IRCall.cs`（`TryParseAtSignLabelCall`）；导出期
> `source/Front/Export/AtSignLabelBuildManager.cs` + `Export/SLIR/SLIRTypes.cs`（`atSignLabel[]` 表）。
> CVM 侧：opcode `OpCode_CallAtSignLabel(124)`（`csimple_lang/src/vm/vm.h`）、
> 装配期改写 `src/vm/assembly/sl_runtime_assembly.c`、绑定表
> `src/vm/runtime/atsign/sl_atsign_binding.{h,c}`、运行期 `src/vm/runtime/vm_runtime.c` case 124、
> 插件接收端 `SLPlugin/csharp_mono/src/cvm_csharp_mono/cvm_csharp_mono.c`（labelExec capability）。
> 用例见 `test/SpecialTest/CSharpTest.sl`（4 个 `testAtSign*` 方法）与负向套件
> `test/SpecialTest/AtSignNegativeTest.sl`。
> 通道语法铁律与调度模型的设计规格见
> `csimple_lang/md/design/PLUGIN_SYSTEM_DESIGN.md` §A4.5 / §A20——
> **该蓝图的「特殊 IR + CVM 中间层」路线现已落地**（差异见 [§6 实现机制](#6-实现机制ir-路线)）。

---

## 1. 模型概述

`@csharp_mono(){}` 是一段**在 SL 源码里直接书写的 C# 代码块**：

| 阶段 | 发生了什么 |
|---|---|
| **编译期（Front 圈地）** | `AtSignLabelSourceRewriter`（语言无关，任意 `@<tag>`）只做圈地：识别 `@csharp_mono` 标签、配平 `()`/`{}`、截取参数表与块体原文，头尾区初判（双出通道即报 20056），**反射转调插件解析器**；按解析结果把整块脱糖为哨兵系统调用语句 `outVar = AtSignLabelCall( entryIndex, inVars... )` / `AtSignLabelCallVoid( ... )` |
| **编译期（插件解析）** | csharp_mono 插件 frontendLibs 解析器 `SLCSharpMonoFrontend.dll`（`SLLabelParser.ParseLabel`）承担**全部块体解析**：小括号参数形态校验、块体结构化头尾区判定（含 `headExtra` 头区延伸协商、中段三态扫描与字面量/注释 `<-` 豁免）、生成 C# `Main` 函数源码，结果以 JSON（小驼峰键）回传 Front；Front 只做复核（中段朴素扫 `<-` 跳过插件豁免段） |
| **IR 期** | `IRCall.TryParseAtSignLabelCall` 拦截哨兵调用 → 发射 `OpCode_CallAtSignLabel(124)`，payload = `SLAtSignLabelCallPackage` JSON（`{entryIndex, paramCount, tryCatch, methodName}`） |
| **导出期** | 模块级 `atSignLabel[]` 绑定表（每块一条：`pluginId/tag/entry/entryMethod/lib/channels[]`）；`AtSignLabelBuildManager` 按块 Label 分组分发构建 handler——csharp_mono handler 用 .NET Framework `csc.exe` 把各块 .cs 编译为 `SLAtSign.dll`，部署到 `SLPlugin/csharp_mono/lib/windows-x64/` |
| **运行期（CVM）** | **装配期**：`sl_assembly_rewrite_atsign_instruction` 解析 payload JSON → 按 `entryIndex` 定位 `atSignLabel[]` 条目 → 通道校验 → `sl_atsign_binding` 注册绑定 → 指令改写为 4 字节绑定索引；**执行期**：case 124 弹 `paramCount` 个入参（栈槽编组 `SLLabelValue`）→ 插件 labelExec capability（mono JIT `Entry_N.Main`）→ 返回值压栈 |

SL 变量与 C# 变量之间**不共享内存**，一切数据往来走**通道（channel）**：
入通道把 SL 变量值拷入 C# 形参，出通道把 C# 返回值拷回 SL 变量。

## 2. 快速开始

```
int a = 30
int b = 12
int c = 0
@csharp_mono(){
    var a <- $a
    var b <- $b
    import SLCSharp;
    var c = MathUtil.Add( a, b );
    $c <- c;
}
Console.println( c.toString() )    # 42
```

## 3. 块体结构（结构化头尾区）

块体按位置分三个区，`<-` 通道行**只允许出现在头区/尾区**：

| 区 | 范围 | 允许的行 |
|---|---|---|
| **头区** | 块首起连续消费：空白行 / 入通道 `var x <- $y` / `import NS;` | 首条普通代码行即头区终点；插件可协商 `headExtra` 把头区多延伸若干行（如入通道前的 import） |
| **中段** | 头区与尾区之间 | **纯 C#**：`<-`、`$` 开头行、`import` 前缀一律报错（词法感知：字符串/行注释/块注释内的 `<-` 记入豁免段 `exemptArrows`，Front 复核跳过） |
| **尾区** | 块尾起连续消费：空白行 / 出通道 `$x <- y;` | 末条普通代码行即尾区起点；**至多 1 个**出通道 |

通道铁律（设计档 §A4.5）：`$名称` 必须一端；箭头指向数据目的地
（`C#形参 <- $slVar` 传入、`$slVar <- C#变量` 传出）。

`import NS;` 在头区被**提升为 `using NS;`**（外提到 namespace 之外）——
C# 需要把代码罗列进 Main 函数，using 类指令不留在函数体内。

## 4. 脱糖产物

上例脱糖为一条**哨兵系统调用**（`c` 已声明则赋值，未声明则自动定义为 int）：

```
c = AtSignLabelCall( <entryIndex>, a, b )
```

- `AtSignLabelCall` / `AtSignLabelCallVoid` 在工程 `systemCalls[]` 注册（`cvmFunction: "atsign:label"`），**IR 生成阶段被 `IRCall.TryParseAtSignLabelCall` 拦截**，不会进入普通系统调用路径；
- 拦截后发射 `OpCode_CallAtSignLabel(124)`，payload = `SLAtSignLabelCallPackage` JSON：`{entryIndex, paramCount, tryCatch, methodName}`（`entryIndex` = Front 块收集器列表位置，与模块 `atSignLabel[]` 表一一对应）；
- **无出通道**的块脱糖为 `AtSignLabelCallVoid( <entryIndex>, ... )`，C# 侧生成 `void Main(...)`。

插件为该块生成如下 C# 源码，交 csc 编进 `SLAtSign.dll`：

```csharp
using System;
using SLCSharp;
namespace SLAtSign
{
    public static class Entry_1
    {
        public static int Main(int a, int b)
        {
            var c = MathUtil.Add( a, b );
            return c;
        }
    }
}
```

- 入口类名 `Entry_N` 全项目顺序分配（编译开始即重置；先占号后解析，解析失败编号跳空无害）。
- 入口方法名 `Main` / 命名空间 `SLAtSign` 由插件 JSON 回传，Front 侧兜底同名常量。

## 5. 约束与边界（首期）

| 约束 | 说明 |
|---|---|
| **类型边界** | 入通道形参与出通道返回值均为 **int**（string/float 后续放开）；运行期不支持编组的值形态报 -89 |
| **出通道个数** | 每块**至多 1 个**（第二个出通道 → LID 20056，Front 头尾区初判先于插件检出） |
| **`<-` 位置** | 只允许头区/尾区；中段命中 `<-` → LID 20055（消息「中段为纯 C# 代码，<- 通道行只能出现在块体头区/尾区」）；字符串/行注释/块注释内的 `<-` 豁免（插件三态扫描回传 `exemptArrows`） |
| **小括号参数** | 仅**形态校验占位**：`name=value` 逗号分隔、名称须标识符、value 须简单字面量（禁 `; , ( ) { } < > $ " ' #`）；解析结果存 `labelParams` 元数据，**首期不参与代码生成**（空参数表 `()` 合法） |
| **C# 语言版本** | .NET Framework 自带 csc（v4.0.30319）= **C#5**；块内禁用 C#6+ 语法 |
| **外部程序集** | 块内引用的类型所在 dll 必须已位于 `SLPlugin/csharp_mono/lib/windows-x64/`（如 `SLCSharpTestLib.dll`），编译期自动 `/r:` 引用、运行期 assemblies_path 兜底 |
| **csc 探测** | 环境变量 `SIMPLELANG_CSC` 覆盖 → Framework64 → Framework（64 位优先） |
| **失败策略** | csc 缺失/编译失败/部署失败仅记 Front 日志（LID 22135-22138），**不中断导出**；无构建 handler 的标签同告警不中断 |
| **插件解析器容错** | 解析器加载/契约失败 → LID 20057（进程内只报一次），整块原样透传由后续词法解析暴露原始错误 |
| **payload 序列化** | ⚠ IR 指令 payload 类是 `IRData.PackOpValue` 的**已知类型白名单**——新增 payload 类必须同步加分支，缺失会退化为 `ToString()` 类型全名，CVM 装配期解析失败、运行期报 -86（详见排障表） |

## 6. 实现机制（IR 路线）

```
.sp/.sl 源码
   │ FileParse.ParseTokenStep（Token 解析前）
   ├─① AtSignLabelSourceRewriter.Rewrite      ← 与 DllImportSourceRewriter 同挂点
   │     圈地：识别任意 @<tag>（路由 SLPlugin/<tag>/plugin.jsonc）、配平 ()/{}、
   │     截取参数表/块体原文
   │     ├─ AllocateEntryName() 先占 Entry_N（失败编号跳空无害，仅要求唯一）
   │     ├─ ScanHeadTail 头尾区初判（双出通道 → 20056）
   │     ├─ PluginFrontendParser.Parse → 反射转调插件 SLCSharpMonoFrontend.dll
   │     │   的 SLLabelParser.ParseLabel（按 .sl 源文件上溯定位 plugin.jsonc 的
   │     │   frontendLibs 清单 → Assembly.LoadFrom；结果 JSON 小驼峰键）
   │     ├─ Front 复核：headExtra 越界 / headExtra 区 <- / 中段朴素扫 <-
   │     │   （跳过插件回传的 exemptArrows 豁免段）→ 20055
   │     └─ 脱糖整块（补齐换行保行号）：outVar = AtSignLabelCall( entryIndex,
   │         inVars... ) / AtSignLabelCallVoid(...)；块体登记 AtSignLabelBlockCollector
   │     错误分流（均带文件行号，基准 = '{' 所在行 + 块体段号 - 1）：
   │       outCountError → 20056（出通道首期仅支持 1 个）
   │       !Ok/error     → 20055（透传插件原始消息）
   │       解析器加载失败 → 20057（基础设施错误，进程内只报一次）
   │     （Lexer/TokenParseToNode/Meta 全解析层零侵入）
   ▼
Front 正常编译管线（脱糖语句 = 普通 AtSignLabelCall 系统调用语句）
   │ IR 生成：IRCall.TryParseAtSignLabelCall 拦截哨兵
   │     → OpCode_CallAtSignLabel(124)，payload = SLAtSignLabelCallPackage JSON
   │       {entryIndex, paramCount, tryCatch, methodName}
   │ Export（SLModulePackageWriter）
   ├─② 模块级 atSignLabel[] 表：每块一条 {entryIndex, pluginId, tag, entry,
   │     entryMethod, lib, channels[{dir, slVar, slType, target}]}
   ├─③ AtSignLabelBuildManager.Run              ← VmDllBuildManager 之后
   │     按块 Label 分组分发构建 handler：csharp_mono → 逐块写插件生成的 .cs
   │     （UTF-8 BOM）→ csc /target:library → SLAtSign.dll → 部署 csharp_mono
   │     lib 目录（/r: 同目录 managed dll）；无 handler 标签仅告警不中断
   ▼
module.json（新增 atSignLabel[] 表；124 指令 payload = JSON 原文）
   │ CVM 装配（sl_runtime_assembly.c）
   ├─④ sl_assembly_rewrite_atsign_instruction：
   │     vm_parse_call_package(payload JSON) → entryIndex 定位 atSignLabel[] 条目
   │     → 通道校验（out≤1 / in==paramCount）→ sl_atsign_binding_add 注册绑定
   │     → 指令改写 4 字节绑定索引（LE）
   │     失败：保留 JSON 原文 + Warning 日志，运行期报 -86
   ▼
CVM 运行
   └─⑤ vm_runtime.c case 124：按绑定索引取 SLLabelExecFn → 弹 paramCount 个
         入参（栈槽编组 SLLabelValue）→ 插件 labelExec capability
         （cvm_csharp_mono.c：mono JIT Entry_N.Main → unbox int）→ 返回值压栈
```

与设计档 §A20 蓝图的关系：**「特殊 IR + CVM 中间层」骨架已按蓝图落地**
（`OpCode_CallAtSignLabel` 专用 opcode、`atSignLabel[]` 通道表、装配期绑定、
插件 labelExec 契约），剩余差异：

- **操作数形态**：蓝图建议 5 个 operand（tag/entrySymbol/dllRef/channelTableId/execForm）；
  实现为单 payload JSON → **装配期改写为 4 字节绑定索引**（更紧凑，绑定集中
  `sl_atsign_binding` 表管理）；
- **编组**：蓝图走 VMS（§A11）marshalling；首期为 int 栈槽直编组
  （`channels[].slType` 空 = 运行期按栈槽 kind 判定）；
- **调度**：蓝图 `execForm` 三形态（同步/协程/隔离岛）；首期 case 124 同步直调——
  SL 协程 / Isolate 内调用 @ 块由 SL 侧原有语义承载（`testAtSignCoro` /
  `testAtSignIsolate` 已验证）；
- **解析归属**与蓝图一致：块体/参数解析不进 Front 本体，在插件 frontendLibs
  定制逻辑中（`SLLabelParser`），Front 只圈地+转调+脱糖——**改写器已语言无关**，
  新增 `@<tag>` 只需新增插件目录，Front 零改动。

## 7. 排障

| 症状 | 先看 |
|---|---|
| 块没被识别 | `Logs/Front.txt` 找 `AtSignLabelSourceRewriter`（LID 20054）；确认参数表为空 `()` 或合法 `name=value` 列表、花括号配平 |
| 解析错误（中段 `<-`、import 位置、通道形态、坏参数） | LID 20055（带文件行号，消息为插件原文透传）；行号换算基准 = `{` 所在行 + 块体 1-based 段号 - 1 |
| 多出通道 | LID 20056；每块只留一个出通道 |
| 插件解析器没找到 | LID 20057；确认 `SLPlugin/csharp_mono/frontendLibs/SLCSharpMonoFrontend.dll` 存在（跑 `build-csharp-mono.ps1` 生成）且 `plugin.jsonc` frontendLibs 段已登记 |
| SLAtSign.dll 没生成 | `Logs/Front.txt` 找 `cscAtSign:`（22135 csc 缺失 / 22136 编译失败 / 22137 部署失败） |
| C# 语法错误 | 22136 日志透传 csc 原始输出（行号对应生成的 Entry_N.cs） |
| 运行期 **-86**（payload/绑定畸形） | ① 检查 module.json 中 124 指令 payload 是否为 JSON 对象（若形如类型全名字符串 → `IRData.PackOpValue` 白名单缺分支，见 §5）；② `csimple_lang\build\logs\VM.txt` 找装配期 `CallAtSignLabel: payload is not a JSON call package` / entry 缺失 / 通道数不匹配 Warning |
| 运行期 **-87**（插件/labelExec 不可用） | 确认插件 dll 已构建部署（`build-csharp-mono.ps1`）且 `plugin.jsonc` 声明 labelExec capability |
| 运行期 **-88**（labelExec 执行失败） | `VM.txt`（真实位置 `csimple_lang\build\logs\VM.txt`，cli 重定向；**非** `out\export\...\Logs\`）查插件侧原始错误 |
| 运行期 **-89**（值编组不支持） | 入通道/返回值类型超出首期 int 边界（OBJECT 入通道等） |

## 8. 测试

**正向用例**（`test/SpecialTest/CSharpTest.sl`，宿主 `project/CSimpleVMSpecialTest`，
SpecialTest 全量 **21 passed / 0 failed**）：

- `testAtSignMono()`：入+出通道完整往返（30+12=42）、无出通道块（`AtSignLabelCallVoid`）、
  小括号参数占位（`@csharp_mono( name=value, mode=fast )` 编译通过，参数仅存元数据）
- `testAtSignCoro()`：协程内块（spawn+await 往返 42）
- `testAtSignIsolate()`：isolate 线程内块（Isolate.run 15+27=42）
- `testAtSignExemptArrow()`：中段字面量豁免（字符串 `"a <- b"` / 行注释内 `<-`
  不算通道，只有代码区 `<-` 才报 20055）

**负向套件**（独立工程 `test/SpecialTest/AtSignNegativeTest.{sl,sp,jsonc}`，
**刻意不入** `ProjectTest.jsonc` 主回归清单——负例必须编译失败）：

| 用例 | 触发点 | 预期 LID |
|---|---|---|
| `negDoubleOut` | 尾区两个出通道（Front 初判） | 20056 |
| `negMidArrow` | 中段 `int r = a <- 1;` | 20055 |
| `negImportInMid` | 中段 `import SLCSharp;` | 20055 |
| `negImportBeforeIn` | import 在入通道之前（头区延伸吸收后入通道落入中段） | 20055 |
| `negDollarInMid` | 出通道行之后还有代码行（非尾区） | 20055 |
| `negBadParams` | `@csharp_mono( bad-name )` 坏参数形态 | 20055 |

驱动脚本（断言 Front.txt LID 计数 20055×5 + 20056×1；Front CLI 退出码恒 0 不可作断言）：

```powershell
powershell -ExecutionPolicy Bypass -File test\SpecialTest\atsign-negative-test.ps1
```
