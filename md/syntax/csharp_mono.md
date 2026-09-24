# @csharp_mono(){} 内联 C# 块

> **本文档以「当前实现」为准**，对应 `source/Front/Compile/Parse/CSharpMonoSourceRewriter.cs`
> + `source/Front/Export/CscAtSignBuildManager.cs`，
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
| **编译期（Front）** | `CSharpMonoSourceRewriter` 在 Token 解析前把整块脱糖为一条 `CSharpCallInt` / `CSharpCallVoid` 系统调用语句；块体 C# 源码登记到 `CSharpMonoBlockCollector` |
| **导出期** | `CscAtSignBuildManager` 把全部块体用 .NET Framework `csc.exe` 编译为 `SLAtSign.dll`，部署到 `SLPlugin/csharp_mono/lib/windows-x64/`（引用同目录已有用户程序集，如 `SLCSharpTestLib.dll`） |
| **运行期（CVM）** | 复用 csharp_mono 插件的 mono 链路：`CSharpCallXxx("SLAtSign.dll","SLAtSign","Entry_N","Run",…)` → mono JIT → 返回值编组回推 VM 栈 |

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

## 3. 块体结构（按行分类）

块体逐行识别，四类行各司其职：

| 行形态 | 分类 | 语义 |
|---|---|---|
| `var a <- $a` | **入通道** | `$a` 引用外层 SL 变量 `a`，其值作为 C# 形参 `a` 传入 |
| `import SLCSharp;` | **import 行** | 提升为 C# `using SLCSharp;` |
| `var c = MathUtil.Add( a, b );` | **中段行** | 原样保留进 C# 方法体（可用完整 C#5 语法） |
| `$c <- c;` | **出通道** | C# 返回表达式 `c` 作为 `Run` 的 `return`，返回值写回 SL 变量 `c` |

通道铁律（设计档 §A4.5）：`$名称` 必须一端；箭头指向数据目的地
（`C#形参 <- $slVar` 传入、`$slVar <- C#表达式` 传出）。

## 4. 脱糖产物

上例脱糖为一条**裸赋值**（`c` 已声明则赋值，未声明则自动定义为 int）：

```
c = CSharpCallInt( "SLAtSign.dll", "SLAtSign", "Entry_1", "Run", 1, a, b )
```

块体则生成如下 C# 源码，交 csc 编进 `SLAtSign.dll`：

```csharp
using System;
using SLCSharp;
namespace SLAtSign
{
    public static class Entry_1
    {
        public static int Run(int a, int b)
        {
            var c = MathUtil.Add( a, b );
            return c;
        }
    }
}
```

- 入口类名 `Entry_N` 全项目顺序分配（编译开始即重置）。
- **无出通道**的块脱糖为 `CSharpCallVoid( "SLAtSign.dll", "SLAtSign", "Entry_N", "Run", 0, … )`，C# 侧生成 `void Run(...)`。

## 5. 约束与边界（首期）

| 约束 | 说明 |
|---|---|
| **类型边界** | 入通道形参与出通道返回值均为 **int**（string/float 后续放开） |
| **出通道个数** | 每块**至多 1 个**（多出通道编译期报错，整块透传由后续词法报错兜底） |
| **C# 语言版本** | .NET Framework 自带 csc（v4.0.30319）= **C#5**；块内禁用 C#6+ 语法 |
| **外部程序集** | 块内引用的类型所在 dll 必须已位于 `SLPlugin/csharp_mono/lib/windows-x64/`（如 `SLCSharpTestLib.dll`），编译期自动 `/r:` 引用、运行期 assemblies_path 兜底 |
| **csc 探测** | 环境变量 `SIMPLELANG_CSC` 覆盖 → Framework64 → Framework（64 位优先） |
| **失败策略** | csc 缺失/编译失败/部署失败仅记 Front 日志（LID 22135-22138），**不中断导出**；运行期调用再报 NOT_FOUND -93 |
| **通道行容错** | 通道/import 行形态错误记日志（带行号）并整块原样透传，由后续词法解析暴露原始错误 |

## 6. 实现机制（首期路线）

```
.sp/.sl 源码
   │ FileParse.ParseTokenStep（Token 解析前）
   ├─① CSharpMonoSourceRewriter.Rewrite      ← 与 DllImportSourceRewriter 同挂点
   │     整块替换为脱糖语句（补齐换行保行号），块体登记 CSharpMonoBlockCollector
   │     （Lexer/TokenParseToNode/Meta 全解析层零侵入）
   ▼
Front 正常编译管线（脱糖语句 = 普通 CSharpCallInt 系统调用）
   │ ExportLangManager.Export
   ├─② CscAtSignBuildManager.Run              ← VmDllBuildManager 之后
   │     逐块写 .cs（UTF-8 BOM）→ csc /target:library → SLAtSign.dll
   │     → 部署 csharp_mono lib 目录（/r: 同目录 managed dll）
   ▼
module.json（无新增字段，CSharpCallInt 走既有 systemCalls 注册）
   │ CVM 运行
   └─③ csharp_exec 栈协议 → mono JIT Entry_N.Run → unbox int 回推 VM 栈
```

与设计档 §A20 蓝图（`OpCode_CallAtSignLabel` 特殊 IR + `channelTable` + CVM 标签中间层）的差异：

- **实现机制不同**：首期走源码文本预改写（编译期脱糖），蓝图是特殊 IR + 运行期中间层；
- **用户可见语义等价**：通道语法、块体书写方式、csc 编译期报错位置（Front.txt）一致；
- **升级路径**：后续把改写器换为 IR 生成（`OpCode_CallAtSignLabel`）即可平滑演进，
  SL 侧语法与测试用例不动。

## 7. 排障

| 症状 | 先看 |
|---|---|
| 块没被识别 | `Logs/Front.txt` 找 `CSharpMonoSourceRewriter`（LID 20054）；确认空参数表 `()` 与花括号配平 |
| 通道行报语法错 | LID 20055（带行号）；核对 `<-` 两侧形态（入 `var x <- $y` / 出 `$x <- y`） |
| 多出通道 | LID 20056；每块只留一个出通道 |
| SLAtSign.dll 没生成 | `Logs/Front.txt` 找 `cscAtSign:`（22135 csc 缺失 / 22136 编译失败 / 22137 部署失败） |
| 运行期 -93 NOT_FOUND | 确认 `SLPlugin/csharp_mono/lib/windows-x64/SLAtSign.dll` 存在且为最新编译产物 |
| C# 语法错误 | 22136 日志透传 csc 原始输出（行号对应生成的 Entry_N.cs） |

## 8. 测试

`test/specialtest/CSharpTest.sl` → `testAtSignMono()`（宿主 `project/CSimpleVMSpecialTest`）：

- 入+出通道完整往返（30+12=42）
- 无出通道块（`CSharpCallVoid`，ret_kind=0）
