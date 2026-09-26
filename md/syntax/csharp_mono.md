# @csharp\_mono(){} 内联 C# 块

> **本文档以「当前实现」为准**（IR 路线 + 通道会话机制 + string 出通道，2026-09-26），对应五层：
> Front 圈地层（语言无关通用改写器）`source/Front/Compile/Parse/AtSignLabelSourceRewriter.cs`
>
> - 反射宿主 `source/Front/Compile/Parse/PluginFrontendParser.cs`（**统一接口化转调**：Parse 契约 + Build 契约，均为单 JSON 请求）；
>   插件 frontend 工程（**全部块体解析 + csc 编译部署逻辑**）
>   `simple_language_plugins/csharp_mono/frontend/`——`SLLabelParser.cs`（块体解析）+
>   `SLLabelBuilder.cs`（csc 合并编译与部署），经 `plugin.jsonc` 的
>   `parserType` / `builderType` / `builderMethod` 反射转调；IR 拦截
>   `source/Front/IR/IRCall.cs`（`TryParseAtSignLabelCall`）；导出期
>   `source/Front/Export/AtSignLabelBuildManager.cs` + `Export/SLIR/SLIRTypes.cs`（`atSignLabel[]` 表）。
>   CVM 侧：opcode `OpCode_CallAtSignLabel(124)`（`csimple_lang/src/vm/vm.h`）、
>   装配期改写 `src/vm/assembly/sl_runtime_assembly.c`、绑定表
>   `src/vm/runtime/atsign/sl_atsign_binding.{h,c}`、**通道会话管理器
>   `src/vm/runtime/atsign/sl_atsign_channel.{h,c}`（Core 域三原语
>   `AtSignChannelIn/OutInt/OutString`，`<-`** **赋值的取值落点）**、
>   运行期 `src/vm/runtime/vm_runtime.c` case 124、
>   插件接收端 `simple_language_plugins/csharp_mono/cvm/src/cvm_csharp_mono/cvm_csharp_mono.c`（labelExec capability）；
>   通道包装层 `simple_language_plugins/csharp_mono/slang/CSharpMono.sl`
>   （`ChannelIn/Out<Kind>`，plugin.jsonc `refModule.channelClassPath` 指向）。
>   用例见 `test/SpecialTest/CSharpTest.sl`（7 个 `testAtSign*` 方法）与负向套件
>   `test/Other/AtSignLabel/AtSignNegativeTest.sl`。
>   通道语法铁律与调度模型的设计规格见
>   `csimple_lang/md/design/PLUGIN_SYSTEM_DESIGN.md` §A4.5 / §A20——
>   **该蓝图的「特殊 IR + CVM 中间层」路线现已落地**（差异见 [§6 实现机制](#6-实现机制ir-路线)）。

***

## 1. 模型概述

`@csharp_mono(){}` 是一段**在 SL 源码里直接书写的 C# 代码块**：

| 阶段                | 发生了什么                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **编译期（Front 圈地）** | `AtSignLabelSourceRewriter`（语言无关，任意 `@<tag>`）只做圈地：识别 `@csharp_mono` 标签、配平 `()`/`{}`、截取参数表与块体原文，块体三分初判（头区入通道 / 尾行出通道，双出通道即报 20056；代码段原文零处理），**反射转调插件解析器**；按解析结果把整块脱糖为**三步通道序列**——入通道预暂存（`ChannelIn<Kind>( entry, slVar )`，每入通道一条）→ 哨兵 `AtSignLabelCallVoid( entry )` → 出通道消费（`outVar = ChannelOut<Kind>( entry )`，有出通道时）                                                                                                                                                                                                                                |
| **编译期（插件解析）**     | csharp\_mono 插件 frontend 工程 `SLCSharpMonoFrontend.dll`（`SLLabelParser.ParseLabel`，Parse 契约单 JSON）承担**全部块体解析**：小括号参数形态校验、代码段整编（段首指令行吸收、代码区 `<-`/`$` 前缀/指令行检出、字符串/注释内 `<-` 字面量放行）、生成 C# `Main` 函数源码，结果以 JSON（小驼峰键）回传 Front；**Front 零复核**——代码段是目标语言自己的编译域，Front 不做任何处理                                                                                                                                                                                                                                                                                    |
| **IR 期**          | `IRCall.TryParseAtSignLabelCall` 拦截哨兵调用 → 发射 `OpCode_CallAtSignLabel(124)`，payload = `SLAtSignLabelCallPackage` JSON（`{entryIndex, paramCount, tryCatch, methodName}`）；哨兵调用**只允许出现在方法体内**——成员变量/全局变量/enum 成员初始化表达式等无宿主 IRMethod 的场景被拒绝（LID 20059，回退普通 `CallSystemMethod`）                                                                                                                                                                                                                                                                              |
| **导出期**           | 模块级 `atSignLabel[]` 绑定表（每块一条：`pluginId/tag/entry/entryMethod/lib/channels[]`）；`AtSignLabelBuildManager` **只组装构建上下文**（Build 契约单 JSON：`{label, outDir, libDir, entries[{entryName, source}]}`）反射转调插件 frontend 的 `SLLabelBuilder.BuildLabels`——由插件完成 .NET Framework `csc.exe` 合并编译（`SLAtSign.dll`）与部署（`libDir`），Front 按回传 `kind` 五态分流 LID 22135\~22138                                                                                                                                                                                                    |
| **运行期（CVM）**      | **装配期**：`sl_assembly_rewrite_atsign_instruction` 解析 payload JSON → 按 `entryIndex` 定位 `atSignLabel[]` 条目 → 通道校验 → `sl_atsign_binding` 注册绑定 → 指令改写为 4 字节绑定索引（**只扫方法体**——成员变量初始化表达式不在扫描范围，Front 已编译期拒绝；手改包硬塞 124 进字段表达式则运行期报 -86）；**执行期**：`ChannelIn<Kind>`（普通系统方法）把入值深拷贝暂存进 `sl_atsign_channel` 会话表（`(VM*, entryIndex)` 键控）→ case 124 哨兵从会话取暂存值 → 插件 labelExec capability（mono JIT `Entry_N.Main`）→ 出值捕获回会话 → `ChannelOut<Kind>`（普通系统方法）消费会话压栈赋值（one-shot，随即 drop）；会话/编组失败 = **可捕获 VM 异常**（`vm_sys_throw`，-86/-89，与 coroutine/isolate/process 系统方法同错误模型） |

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
    using SLCSharp;
    var c = MathUtil.Add( a, b );
    $c <- c;
}
Console.println( c.toString() )    # 42
```

## 3. 块体结构（三分：头区 / 代码段 / 尾行）

块体按位置分三段，`<-` 通道行**只允许出现在头区/尾行**：

| 段           | 范围                                       | 规则                                                                                                                                                                |
| ----------- | ---------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **头区**（入通道） | 块首起连续消费：空白行 / 入通道 `var [slType] x <- $y` | 首个非入通道实质行即代码段起点（Front 初判）                                                                                                                                         |
| **代码段**     | 头区之后到尾行（出通道行）之前                          | **目标语言原文，Front 零处理**：整段透传插件解析器自决整编——段首 `import NS;` / `using NS;` 指令行被插件吸收；代码区出现 `<-`、`$` 前缀行、指令行 → 插件报 20055；字符串/字符/行注释/块注释内的 `<-` 是合法原文直接放行（插件三态扫描自决，Front 零复核） |
| **尾行**（出通道） | 块尾起倒序连续消费：空白行 / 出通道 `[slType] $x <- y;`  | Front 初判；**至多 1 个**出通道（第二个 → 20056）；`[slType]` 为可选类型标记（见下）                                                                                                        |

通道铁律（设计档 §A4.5）：`$名称` 必须一端；箭头指向数据目的地
（`C#形参 <- $slVar` 传入、`$slVar <- C#变量` 传出）。

入通道行支持**可选类型标记**（`var` 与形参名之间的标识符）：

```
var a <- $a            # 无标记 → 缺省 int（既有块全兼容）
var string name <- $name   # slType = string → 插件映射为 C# 形参 string
var double r <- $r     # slType = double → 脱糖 ChannelInDouble + 映射 C# 形参 double
```

slType 标记是**语言无关原文**：Front 圈地时不解释语义，只随通道表
（`channels[].slType`）下发给插件，由插件按目标语言映射——csharp\_mono
映射表：`int/int32/i32→int`、`long/int64/i64→long`、
`float/double/float64/f64→double`、`string→string`、`bool/boolean→bool`，
缺省 `int`；无法映射 → LID 20055（`入通道 N 类型标记无法映射为 C# 类型`）。
Java/Python/CUDA 插件各自定义映射表，Front 零感知。

**出通道行也支持类型标记**（`$` 之前），但首期**仅 int/string 两变体**：

```
$c <- g;              # 无标记 → 缺省 int（ChannelOutInt，Main 返回 int）
string $s <- g;       # slType = string → ChannelOutString，Main 返回 string
```

- 缺省/`int` → 插件生成 `int Main(...)`，`ChannelOutInt` 消费；
- `string` → 插件生成 `string Main(...)`，`ChannelOutString` 消费
  （运行期按返回对象实际类型判型：`System.String` 装箱引用 → 深拷贝
  utf8 跨界回 SL 栈；`null` → 空值；其余对象 → unbox int）；
- `long/double/bool` 等其它标记 → 插件契约防御报 20055（编译期拒收）。

代码段段首指令行支持**双语法**：`import NS;` 与 `using NS;` 等价（均被插件吸收并**提升为
C#** **`using NS;`** 外提到 namespace 之外）——C# 需要把代码罗列进 Main 函数，
using 类指令不留在函数体内。指令行只允许贴着代码段首部（入通道之后、普通代码之前）：
import 出现在入通道**之前**会截断头区初判，后续入通道行落入代码段代码区 → 20055；
入通道之后的代码区再出现指令行同样报 20055（`using ( ... )` 语句是合法 C# 代码，放行）。

## 4. 脱糖产物

上例（int 出通道，`c` 已声明则赋值，未声明则自动定义为 int）脱糖为**三步通道序列**：

```
SLang.Plugin.CSharpMono.ChannelInInt( <entryIndex>, a )
SLang.Plugin.CSharpMono.ChannelInInt( <entryIndex>, b )
AtSignLabelCallVoid( <entryIndex> )
c = SLang.Plugin.CSharpMono.ChannelOutInt( <entryIndex> )
```

入通道按 `channels[].slType` **三分流**选 `ChannelIn<Kind>` 变体：缺省/其它可映射
标记 → `ChannelInInt`、`string` → `ChannelInString`、**`double/float/float64/f64` →
`ChannelInDouble`**——Kind 只区分 SL 侧承载形态（值经会话 `SLLabelValue` 的
INT/FLOAT/STRING/NULL 编组跨界），C# 形参类型仍由插件映射表自决（见 §3）。

- **通道包装函数**（`ChannelInInt/ChannelInString/ChannelInDouble/ChannelOutInt/ChannelOutString`）
  位于插件 slang ref module（`plugin.jsonc` 的 `refModule.channelClassPath`
  指向 `SLang.Plugin.CSharpMono`），函数体内部转调 Core 域系统方法
  `AtSignChannelIn` / `AtSignChannelOutInt` / `AtSignChannelOutString`——
  **`<-`** **赋值不写死进 opcode，是普通 SL 函数调用系统方法**，CVM 侧由
  `sl_atsign_channel` 会话管理器取值并按执行环境路由；
- **哨兵** `AtSignLabelCallVoid` / `AtSignLabelCall` 在工程 `systemCalls[]`
  注册（`cvmFunction: "atsign:label"`），**IR 生成阶段被
  `IRCall.TryParseAtSignLabelCall`** **拦截**发射 `OpCode_CallAtSignLabel(124)`，
  payload = `SLAtSignLabelCallPackage` JSON：`{entryIndex, paramCount, tryCatch,
  methodName}`（`entryIndex` = Front 块收集器列表位置，与模块 `atSignLabel[]`
  表一一对应）；CVM 桥接层从通道会话取暂存入值 → 插件 labelExec →
  出值捕获回会话（`AtSignLabelCall` 非空返回值直推栈的旧形态仅保留兼容，
  正常块一律走 void 哨兵）；**位置限制**：整个机制只允许出现在**方法体内**
  （三步通道序列是语句序列，单条初始化表达式无法成立）——手写哨兵调用
  出现在成员变量/全局变量/enum 成员初始化表达式时，IR 生成阶段报 **LID 20059**
  并回退普通 `CallSystemMethod`（CVM 装配期也只扫方法体，镜像契约）；
- **出通道消费**：`outVar = ChannelOut<Kind>( entry )` 按出通道类型标记选
  变体（`string $s <- g` → `ChannelOutString`，缺省 → `ChannelOutInt`），
  one-shot 消费会话（取值压栈后随即 drop，循环体内重复执行安全）；
- **无出通道**的块省略最后一步，C# 侧生成 `void Main(...)`；无入通道的块
  省略 ChannelIn 行。

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
- `Main` 返回类型随出通道类型标记：缺省/`int` → `int`，`string` → `string`（§3 出通道标记），无出通道 → `void`。

## 5. 约束与边界（首期）

| 约束              | 说明                                                                                                                                                                                                                 |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **类型边界**        | 入通道：slType 可映射标记 int/long/float/double/string/bool（映射表见 §3，缺省 int），脱糖按 Kind 三分流选 `ChannelIn<Kind>` 变体（缺省/int/long/bool → Int、`string` → String、`float/double/float64/f64` → Double）；入通道值运行期按栈槽 kind 编组 `SLLabelValue` 四态（INT/FLOAT/STRING/NULL），不支持形态报 -89；出通道：**int/string**（类型标记变体，缺省 int；long/double/bool 等标记编译期拒收 20055）；出值运行期按返回对象实际类型判型（`System.String` → 深拷贝跨界 / `null` → 空值 / 其余 unbox int） |
| **出通道个数**       | 每块**至多 1 个**（第二个出通道 → LID 20056，Front 三分初判先于插件检出）                                                                                                                                                                  |
| **`<-`** **位置** | 只允许头区/尾行；代码段代码区命中 `<-` → LID 20055（消息为插件原文透传）；字符串/字符/行注释/块注释内的 `<-` 是合法目标语言原文直接放行（插件三态扫描自决，Front 零复核）                                                                                                              |
| **小括号参数**       | 仅**形态校验占位**：`name=value` 逗号分隔、名称须标识符、value 须简单字面量（禁 `; , ( ) { } < > $ " ' #`）；解析结果存 `labelParams` 元数据，**首期不参与代码生成**（空参数表 `()` 合法）                                                                                 |
| **C# 语言版本**     | .NET Framework 自带 csc（v4.0.30319）= **C#5**；块内禁用 C#6+ 语法                                                                                                                                                            |
| **外部程序集**       | 块内引用的类型所在 dll 必须已位于 `simple_language_plugins/csharp_mono/cvm/lib/windows-x64/`（如 `SLCSharpTestLib.dll`），编译期自动 `/r:` 引用、运行期 assemblies\_path 兜底                                                                     |
| **csc 探测**      | 环境变量 `SIMPLELANG_CSC` 覆盖 → Framework64 → Framework（64 位优先）                                                                                                                                                         |
| **失败策略**        | csc 缺失/编译失败/部署失败仅记 Front 日志（LID 22135-22138），**不中断导出**；无构建 handler 的标签同告警不中断                                                                                                                                       |
| **插件解析器容错**     | 解析器加载/契约失败 → LID 20057（进程内只报一次），整块原样透传由后续词法解析暴露原始错误                                                                                                                                                                |
| **payload 序列化** | ⚠ IR 指令 payload 类是 `IRData.PackOpValue` 的**已知类型白名单**——新增 payload 类必须同步加分支，缺失会退化为 `ToString()` 类型全名，CVM 装配期解析失败、运行期报 -86（详见排障表）                                                                                     |

## 6. 实现机制（IR 路线）

```
.sp/.sl 源码
   │ FileParse.ParseTokenStep（Token 解析前）
   ├─① AtSignLabelSourceRewriter.Rewrite      ← 与 DllImportSourceRewriter 同挂点
   │     圈地：识别任意 @<tag>（按各插件清单声明的 plugin.id 路由——
   │     FindPluginRootById id 索引，目录名仅缺 id 时兼容）、
   │     配平 ()/{}、截取参数表/块体原文
   │     ├─ AllocateEntryName() 先占 Entry_N（失败编号跳空无害，仅要求唯一）
   │     ├─ ScanHeadTail 三分初判（头区入通道/尾行出通道；双出通道 → 20056；
   │     │   代码段原文零处理）
   │     ├─ PluginFrontendParser.Parse → 反射转调插件 frontend 工程
   │     │   SLCSharpMonoFrontend.dll 的 SLLabelParser.ParseLabel（按 .sl 源文件
   │     │   上溯定位插件根 → plugin.jsonc 的 parserType → frontend/ 目录
   │     │   Assembly.LoadFrom；Parse 契约单 JSON，结果小驼峰键；代码段整编
   │     │   全权归插件，Front 零复核）
   │     └─ 脱糖整块（补齐换行保行号）：三步通道序列——
   │         ChannelIn<Kind>( entry, slVar )（每入通道一条）→
   │         AtSignLabelCallVoid( entry ) → outVar = ChannelOut<Kind>( entry )；
   │         块体登记 AtSignLabelBlockCollector
   │     错误分流（均带文件行号，基准 = '{' 所在行 + 块体段号 - 1）：
   │       outCountError → 20056（出通道首期仅支持 1 个）
   │       !Ok/error     → 20055（透传插件原始消息）
   │       解析器加载失败 → 20057（基础设施错误，进程内只报一次）
   │     （Lexer/TokenParseToNode/Meta 全解析层零侵入）
   ▼
Front 正常编译管线（通道调用 = 普通系统调用语句；哨兵被 IRCall 拦截）
   │ IR 生成：IRCall.TryParseAtSignLabelCall 拦截哨兵
   │     → OpCode_CallAtSignLabel(124)，payload = SLAtSignLabelCallPackage JSON
   │       {entryIndex, paramCount, tryCatch, methodName}
   │ Export（SLModulePackageWriter）
   ├─② 模块级 atSignLabel[] 表：每块一条 {entryIndex, pluginId, tag, entry,
   │     entryMethod, lib, channels[{dir, slVar, slType, target}]}
   ├─③ AtSignLabelBuildManager.Run              ← VmDllBuildManager 之后
   │     Front 只组装构建上下文（Build 契约单 JSON：{label, outDir, libDir,
   │     entries[{entryName, source}]}）→ PluginFrontendParser.Build 反射转调
   │     插件 frontend 的 SLLabelBuilder.BuildLabels：csc 合并编译各块
   │     .cs（UTF-8 BOM，/target:library，/r: libDir 下 managed dll）→
   │     SLAtSign.dll → 部署 libDir；按回传 kind 五态分流日志（success → 22138
   │     Info / csc 缺失 22135 / 编译失败 22136 / 部署失败 22137 / 契约异常 22136）；
   │     无构建入口（builderType 未声明或缺失）仅告警不中断
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
   ├─⑤ 通道原语（普通系统方法，Core 域 cvmFunction sl_atsign_channel_*）：
   │     ChannelIn 深拷贝入值暂存会话（(VM*, entryIndex) 键控）；
   │     ChannelOut 消费会话压栈赋值（one-shot drop，int/string 变体）
   └─⑥ vm_runtime.c case 124（void 哨兵）：按绑定索引取 SLLabelExecFn →
         take 暂存入值（数量与通道表核对）→ 插件 labelExec capability
         （cvm_csharp_mono.c：mono JIT Entry_N.Main，出值按返回对象实际
         类型判型——System.String → host->alloc 深拷贝 utf8 / null / 其余
         unbox int）→ 出值捕获回会话
   通道原语失败 = vm_sys_throw 可捕获 VM 异常（-86/-89）
```

与设计档 §A20 蓝图的关系：**「特殊 IR + CVM 中间层」骨架已按蓝图落地**
（`OpCode_CallAtSignLabel` 专用 opcode、`atSignLabel[]` 通道表、装配期绑定、
插件 labelExec 契约），剩余差异：

- **操作数形态**：蓝图建议 5 个 operand（tag/entrySymbol/dllRef/channelTableId/execForm）；
  实现为单 payload JSON → **装配期改写为 4 字节绑定索引**（更紧凑，绑定集中
  `sl_atsign_binding` 表管理）；
- **编组**：蓝图走 VMS（§A11）marshalling；已落地按栈槽 kind 直编组
  （INT/FLOAT/STRING/NULL；`channels[].slType` 仅声明意图的元数据，非空供
  插件映射形参类型——入通道全五型、出通道仅 int/string，空 = 缺省 int）；
  string 入通道 MAIN/COROUTINE 借用 VMObject 内部缓冲、ISOLATE 走
  `host->alloc` 深拷贝（V3 跨界内存规则）；string 出通道深拷贝 utf8
  跨界回 SL 栈（出值所有权独立于 mono GC 堆，V3 对称）；
- **调度与环境感知**：蓝图 `execForm` 三形态；已落地 **exec\_env 三态检测**
  （`sl_atsign_detect_env`：`vm->isolate` 非空且非 main → ISOLATE /
  `vm->current_coroutine` 非空且非 root → COROUTINE / 否则 MAIN），桥接层
  五阶段：通道普查 → 环境检查 → 入通道编组（按环境区分所有权策略）→
  插件 exec → 出通道推栈 + 清扫（`sl_atsign_binding.c`，ABI v2
  `SL_LABEL_EXEC_CTX_VERSION=2`，ctx 尾部 `exec_env` 字段）；SL 协程 /
  Isolate 内调用 @ 块由 SL 侧原有语义承载（`testAtSignCoro` /
  `testAtSignIsolate` / `testAtSignIsoString` 已验证，VM.txt 有
  `exec env MAIN/COROUTINE/ISOLATE` 三态日志）；
- **解析/构建归属**与蓝图一致：块体/参数解析与目标产物（csc 编译）均不进 Front
  本体，在插件 frontend 工程中（`SLLabelParser` / `SLLabelBuilder`），
  Front 只圈地+组装上下文+接口化转调+脱糖——**改写器已语言无关**，Front 侧与
  目标语言/工具链完全解耦，接入任意语言或特殊 SDK 只需新增插件目录（含
  frontend 实现 Parse/Build 两个单 JSON 契约），Front 零改动。

## 7. 排障

| 症状                                | 先看                                                                                                                                                                                                                                                                                                                                                                           |
| --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 块没被识别                             | `Logs/Front.txt` 找 `AtSignLabelSourceRewriter`（LID 20054）；确认参数表为空 `()` 或合法 `name=value` 列表、花括号配平                                                                                                                                                                                                                                                                             |
| 解析错误（代码段 `<-`、import 位置、通道形态、坏参数） | LID 20055（带文件行号，消息为插件原文透传）；行号换算基准 = `{` 所在行 + 块体 1-based 段号 - 1                                                                                                                                                                                                                                                                                                              |
| 多出通道                              | LID 20056；每块只留一个出通道                                                                                                                                                                                                                                                                                                                                                          |
| 插件解析器没找到                          | LID 20057；确认 `simple_language_plugins/csharp_mono/frontend/SLCSharpMonoFrontend.dll` 存在（跑 `cvm\build-csharp-mono.ps1` 或 `dotnet build frontend\SLCSharpMonoFrontend.csproj -c Release` 生成）且 `plugin.jsonc` 已声明 `frontendLibs` + `parserType`                                                                                                                                 |
| SLAtSign.dll 没生成                  | `Logs/Front.txt` 找 `cscAtSign:`（22135 csc 缺失 / 22136 编译失败 / 22137 部署失败）                                                                                                                                                                                                                                                                                                      |
| C# 语法错误                           | 22136 日志透传 csc 原始输出（行号对应生成的 Entry\_N.cs）                                                                                                                                                                                                                                                                                                                                     |
| 运行期 **-86**（payload/绑定/通道会话畸形）    | ① 检查 module.json 中 124 指令 payload 是否为 JSON 对象（若形如类型全名字符串 → `IRData.PackOpValue` 白名单缺分支，见 §5）；② `csimple_lang\build\logs\VM.txt` 找装配期 `CallAtSignLabel: payload is not a JSON call package` / entry 缺失 / 通道数不匹配 Warning；③ **通道原语失败也是 -86**（如裸调 `ChannelOut*` 无会话、会话值与消费原语类型不符）——经 `vm_sys_throw` 派发为**可捕获 VM 异常**，SL 侧 try/catch 可拦截恢复（与 coroutine/isolate/process 系统方法同错误模型） |
| 运行期 **-87**（插件/labelExec 不可用）     | 确认插件 dll 已构建部署（`cvm\build-csharp-mono.ps1`）且 `plugin.jsonc` 声明 labelExec capability                                                                                                                                                                                                                                                                                          |
| 运行期 **-88**（labelExec 执行失败）       | `VM.txt`（真实位置 `csimple_lang\build\logs\VM.txt`，cli 重定向；**非** `out\export\...\Logs\`）查插件侧原始错误                                                                                                                                                                                                                                                                                 |
| 运行期 **-89**（值编组不支持）               | 入通道/出值类型超出已支持边界（OBJECT 入通道、出通道会话值与消费原语类型不符等）                                                                                                                                                                                                                                                                                                                                 |

## 8. 测试

**正向用例**（`test/SpecialTest/CSharpTest.sl`，宿主 `project/CSimpleVMSpecialTest`，
SpecialTest 全量 **26 passed / 0 failed**）：

- `testAtSignMono()`：入+出通道完整往返（30+12=42）、无出通道块（`AtSignLabelCallVoid`）、
  小括号参数占位（`@csharp_mono( name=value, mode=fast )` 编译通过，参数仅存元数据）
- `testAtSignCoro()`：协程内块（spawn+await 往返 42）
- `testAtSignIsolate()`：isolate 线程内块（Isolate.run 15+27=42）
- `testAtSignIsoString()`：isolate 线程内 **string 入通道**（slType 标记
  `var string name <- $name` → `Greet('sl').Length == 19`；桥接层 ISOLATE
  深拷贝路径 + `host->alloc` 所有权移交）
- `testAtSignExemptArrow()`：代码段字面量放行（字符串 `"a <- b"` / 行注释内的 `<-`
  是合法 C# 原文，插件三态扫描自决；只有代码区 `<-` 才报 20055，Front 零复核）
- `testAtSignMonoString()`：**string 出通道**（`string $s <- g` → `Main` 返回
  string，mono 装箱 `System.String` → 深拷贝 utf8 跨界 → `ChannelOutString`
  压栈，`s == "hello, sl from mono"` 往返）
- `testAtSignChannelNegative()`：**通道会话负例**（裸调 `ChannelOutInt/OutString`
  无会话 → 可捕获 VM 异常 -86，try/catch 拦截；负例后通道链路完好，正向
  出通道 6+7==13 恢复验证）

**负向套件**（独立工程 `test/Other/AtSignLabel/AtSignNegativeTest.{sl,sp,jsonc}`，
**刻意不入** `ProjectTest.jsonc` 主回归清单——负例必须编译失败）：

| 用例                  | 触发点                                     | 预期 LID |
| ------------------- | --------------------------------------- | ------ |
| `negDoubleOut`      | 尾区两个出通道（Front 三分初判检出）                   | 20056  |
| `negMidArrow`       | 代码段 `int r = a <- 1;`                   | 20055  |
| `negImportInMid`    | 代码段中部 `import SLCSharp;`                | 20055  |
| `negImportBeforeIn` | import 在入通道之前（截断头区初判，入通道行落入代码段）         | 20055  |
| `negDollarInMid`    | 出通道行之后还有代码行（出通道不在块尾，行落入代码段）             | 20055  |
| `negBadParams`      | `@csharp_mono( bad-name )` 坏参数形态        | 20055  |
| `negBadSlType`      | `var unknown_tp name <- $name` 类型标记无法映射 | 20055  |

驱动脚本（断言 Front.txt LID 计数 20055×6 + 20056×1；Front CLI 退出码恒 0 不可作断言）：

```powershell
powershell -ExecutionPolicy Bypass -File test\Other\AtSignLabel\atsign-negative-test.ps1
```

