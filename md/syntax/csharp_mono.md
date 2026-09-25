# @csharp_mono(){} 内联 C# 块

> **本文档以「当前实现」为准**，对应三层：
> Front 圈地层 `source/Front/Compile/Parse/CSharpMonoSourceRewriter.cs` + 反射宿主
> `source/Front/Compile/Parse/CSharpMonoFrontendParser.cs`；插件解析器（全部解析逻辑）
> `SLPlugin/csharp_mono/src/frontend/SLCSharpMonoFrontend/SLLabelParser.cs`（经 `plugin.jsonc`
> 的 `frontendLibs` 清单反射转调）；导出期 `source/Front/Export/CscAtSignBuildManager.cs`。
> 用例见 `test/specialtest/CSharpTest.sl` 的 `testAtSignMono()`。
> 通道语法铁律与调度模型的设计规格见
> `csimple_lang/md/design/PLUGIN_SYSTEM_DESIGN.md` §A4.5 / §A20——
> **该蓝图（特殊 IR + CVM 中间层）首期未采用**，当前为源码文本预改写路线，
> 差异见 [§6 实现机制](#6-实现机制首期路线)。

---

## 1. 模型概述

`@csharp_mono(){} 是一段**在 SL 源码里直接书写的 C# 代码块**：

| 阶段 | 发生了什么 |
|---|---|
| **编译期（Front 圈地）** | `CSharpMonoSourceRewriter` 只做圈地：识别 `@csharp_mono` 标签、配平 `()`/`{}`、截取参数表与块体原文，**反射转调插件解析器**；按解析结果把整块脱糖为一条 `CSharpCallInt` / `CSharpCallVoid` 系统调用语句 |
| **编译期（插件解析）** | csharp_mono 插件 frontendLibs 解析器 `SLCSharpMonoFrontend.dll`（`SLLabelParser.ParseLabel`）承担**全部解析**：小括号参数形态校验、块体结构化头尾区判定、生成 C# `Main` 函数源码（import 提升 using 外提、中段罗列进 Main、出通道作 return），结果以 JSON（小驼峰键）回传 Front |
| **导出期** | `CscAtSignBuildManager` 把插件生成的各块 .cs 用 .NET Framework `csc.exe` 编译为 `SLAtSign.dll`，部署到 `SLPlugin/csharp_mono/lib/windows-x64/`（引用同目录已有用户程序集，如 `SLCSharpTestLib.dll`） |
| **运行期（CVM）** | 复用 csharp_mono 插件的 mono 链路：`CSharpCallXxx("SLAtSign.dll","SLAtSign","Entry_N","Main",…)` → mono JIT → 返回值编组回推 VM 栈 |

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
| **头区** | 块首起连续消费：空白行 / 入通道 `var x <- $y` / `import NS;` | 首条普通代码行即头区终点 |
| **中段** | 头区与尾区之间 | **纯 C#**：`<-`、`$` 开头行、`import` 前缀一律报错（词法感知：字符串/行注释/块注释/逐字串内的 `<-` 不算） |
| **尾区** | 块尾起连续消费：空白行 / 出通道 `$x <- y;` | 末条普通代码行即尾区起点；**至多 1 个**出通道 |

通道铁律（设计档 §A4.5）：`$名称` 必须一端；箭头指向数据目的地
（`C#形参 <- $slVar` 传入、`$slVar <- C#变量` 传出）。

`import NS;` 在头区被**提升为 `using NS;`**（外提到 namespace 之外）——
C# 需要把代码罗列进 Main 函数，using 类指令不留在函数体内。

## 4. 脱糖产物

上例脱糖为一条**裸赋值**（`c` 已声明则赋值，未声明则自动定义为 int）：

```
c = CSharpCallInt( "SLAtSign.dll", "SLAtSign", "Entry_1", "Main", 1, a, b )
```

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
- **无出通道**的块脱糖为 `CSharpCallVoid( "SLAtSign.dll", "SLAtSign", "Entry_N", "Main", 0, … )`，C# 侧生成 `void Main(...)`。

## 5. 约束与边界（首期）

| 约束 | 说明 |
|---|---|
| **类型边界** | 入通道形参与出通道返回值均为 **int**（string/float 后续放开） |
| **出通道个数** | 每块**至多 1 个**（第二个出通道 → LID 20056，整块透传由后续词法报错兜底） |
| **`<-` 位置** | 只允许头区/尾区；中段命中 `<-` → LID 20055（消息「中段为纯 C# 代码，<- 通道行只能出现在块体头区/尾区」） |
| **小括号参数** | 仅**形态校验占位**：`name=value` 逗号分隔、名称须标识符、value 须简单字面量（禁 `; , ( ) { } < > $ " ' #`）；解析结果存 `labelParams` 元数据，**首期不参与代码生成**（空参数表 `()` 合法） |
| **C# 语言版本** | .NET Framework 自带 csc（v4.0.30319）= **C#5**；块内禁用 C#6+ 语法 |
| **外部程序集** | 块内引用的类型所在 dll 必须已位于 `SLPlugin/csharp_mono/lib/windows-x64/`（如 `SLCSharpTestLib.dll`），编译期自动 `/r:` 引用、运行期 assemblies_path 兜底 |
| **csc 探测** | 环境变量 `SIMPLELANG_CSC` 覆盖 → Framework64 → Framework（64 位优先） |
| **失败策略** | csc 缺失/编译失败/部署失败仅记 Front 日志（LID 22135-22138），**不中断导出**；运行期调用再报 NOT_FOUND -93 |
| **插件解析器容错** | 解析器加载/契约失败 → LID 20057（进程内只报一次），整块原样透传由后续词法解析暴露原始错误 |

## 6. 实现机制（首期路线）

```
.sp/.sl 源码
   │ FileParse.ParseTokenStep（Token 解析前）
   ├─① CSharpMonoSourceRewriter.Rewrite      ← 与 DllImportSourceRewriter 同挂点
   │     圈地：识别 @csharp_mono 标签、配平 ()/{}、截取参数表/块体原文
   │     ├─ AllocateEntryName() 先占 Entry_N（失败编号跳空无害，仅要求唯一）
   │     ├─ CSharpMonoFrontendParser.Parse → 反射转调插件 SLCSharpMonoFrontend.dll
   │     │   的 SLLabelParser.ParseLabel（按 .sl 源文件上溯定位 plugin.jsonc 的
   │     │   frontendLibs 清单 → Assembly.LoadFrom；结果 JSON 小驼峰键）
   │     └─ 按 JSON 结果脱糖整块（补齐换行保行号），块体登记 CSharpMonoBlockCollector
   │     错误分流（均带文件行号）：
   │       outCountError → 20056（出通道首期仅支持 1 个）
   │       !Ok/error     → 20055（透传插件原始消息；行号 = '{' 所在行 + 块体段号 - 1）
   │       解析器加载失败 → 20057（基础设施错误，进程内只报一次）
   │     （Lexer/TokenParseToNode/Meta 全解析层零侵入）
   ▼
Front 正常编译管线（脱糖语句 = 普通 CSharpCallInt 系统调用）
   │ ExportLangManager.Export
   ├─② CscAtSignBuildManager.Run              ← VmDllBuildManager 之后
   │     逐块写插件生成的 .cs（UTF-8 BOM）→ csc /target:library → SLAtSign.dll
   │     → 部署 csharp_mono lib 目录（/r: 同目录 managed dll）
   ▼
module.json（无新增字段，CSharpCallInt 走既有 systemCalls 注册）
   │ CVM 运行
   └─③ csharp_exec 栈协议 → mono JIT Entry_N.Main → unbox int 回推 VM 栈
```

与设计档 §A20 蓝图（`OpCode_CallAtSignLabel` 特殊 IR + `channelTable` + CVM 标签中间层）的差异：

- **实现机制不同**：首期走源码文本预改写（编译期脱糖），蓝图是特殊 IR + 运行期中间层；
- **解析归属已按蓝图 §A20 工程分离**：块体/参数解析不进 Front 本体，在插件
  frontendLibs 定制逻辑中（`SLLabelParser`），Front 只圈地+转调+脱糖；
- **用户可见语义等价**：通道语法、块体书写方式、csc 编译期报错位置（Front.txt）一致；
- **升级路径**：后续把改写器换为 IR 生成（`OpCode_CallAtSignLabel`）即可平滑演进，
  SL 侧语法与测试用例不动。

## 7. 排障

| 症状 | 先看 |
|---|---|
| 块没被识别 | `Logs/Front.txt` 找 `CSharpMonoSourceRewriter`（LID 20054）；确认参数表为空 `()` 或合法 `name=value` 列表、花括号配平 |
| 解析错误（中段 `<-`、import 位置、通道形态、坏参数） | LID 20055（带文件行号，消息为插件原文透传）；行号换算基准 = `{` 所在行 + 块体 1-based 段号 - 1 |
| 多出通道 | LID 20056；每块只留一个出通道 |
| 插件解析器没找到 | LID 20057；确认 `SLPlugin/csharp_mono/frontendLibs/SLCSharpMonoFrontend.dll` 存在（跑 `build-csharp-mono.ps1` 生成）且 `plugin.jsonc` frontendLibs 段已登记 |
| SLAtSign.dll 没生成 | `Logs/Front.txt` 找 `cscAtSign:`（22135 csc 缺失 / 22136 编译失败 / 22137 部署失败） |
| 运行期 -93 NOT_FOUND | 确认 `SLPlugin/csharp_mono/lib/windows-x64/SLAtSign.dll` 存在且为最新编译产物 |
| C# 语法错误 | 22136 日志透传 csc 原始输出（行号对应生成的 Entry_N.cs） |

## 8. 测试

`test/specialtest/CSharpTest.sl` → `testAtSignMono()` 等（宿主 `project/CSimpleVMSpecialTest`）：

- 入+出通道完整往返（30+12=42）
- 无出通道块（`CSharpCallVoid`，ret_kind=0）
- 协程内块（`testAtSignCoro`：spawn+await 往返 42）
- isolate 线程内块（`testAtSignIsolate`：Isolate.run 15+27=42）
- 小括号参数占位（`@csharp_mono( name=value, mode=fast )` 编译通过，参数仅存元数据）
- 负向：中段 `<-` / import 出头区 / `$` 行出尾区 → LID 20055（行号精确到文件行）；
  双出通道 → LID 20056；坏参数形态 → LID 20055
