# @csharp\_mono(){} 内联 C# 块

> **本文档以「当前实现」为准**（IR 路线 + 通道会话机制 + string 出通道；2026-09-27 起入通道类型标记**显式必填**（禁 var/缺省）+ SL data/class → C# **镜像 class** 对象编组；2026-09-29 起统一 `@` 识别：`@<tag>(...){...}` 形态由 Lexer 整块捕获、AtSignLabel 块解析上移 MetaCore，源码预改写器退役），对应五层：
> 统一 `@` 识别层 = 词法 + 语义核心：Lexer `source/Front/Parse/LexerParseToToken.cs`
> （`ReadAt`/`TryCaptureAtSignBlock`——`@<tag>(...){...}` 形态整块捕获为不透明
> AtSignBlock token，不匹配零成本回退 At token 走 attribute 管线）+ MetaCore
> `source/Front/Core/AtSignLabelBlockDispatch.cs`（标签路由/三分初判/Parse 契约
> 转调/块登记/就地脱糖，经 `Core/MetaMemberFunction.cs` 语句链接入）
>
> - 反射宿主 `source/Front/Parse/PluginFrontendParser.cs`（**统一接口化转调**：Parse 契约 + Build 契约，均为单 JSON 请求）；
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
>   用例见 `test/SpecialTest/CSharpTest.sl`（7 个 `testAtSign*` 方法）+
>   `test/SpecialTest/CSharpTest2.sl`（计算链 + 镜像 class 三用例）+
>   `test/SpecialTest/CSharpTest3.sl`（WinForms 窗口 + HTTP json 回显）与负向套件
>   `test/Other/AtSignLabel/AtSignNegativeTest.sl`。
>   通道语法铁律与调度模型的设计规格见
>   `csimple_lang/md/design/PLUGIN_SYSTEM_DESIGN.md` §A4.5 / §A20——
>   **该蓝图的「特殊 IR + CVM 中间层」路线现已落地**（差异见 [§6 实现机制](#6-实现机制ir-路线)）。

***

## 1. 模型概述

`@csharp_mono(){}` 是一段**在 SL 源码里直接书写的 C# 代码块**：

| 阶段                | 发生了什么                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **编译期（统一 `@` 识别 + MetaCore 脱糖）** | Lexer `ReadAt`/`TryCaptureAtSignBlock`（语言无关）形态判别：`@<tag>(...)` 后紧跟块体 `{...}` 即整块原文捕获为不透明 AtSignBlock token（配平 `()`/`{}`、截取参数表与块体；不匹配零成本回退走 attribute 管线，**其余 `@` 行为完全不变**）→ Token/StructParse/FileMeta 透传拦截为块语法节点 → MetaCore `AtSignLabelBlockDispatch`：标签路由（未命中 20055）+ 块体三分初判（头区入通道 / 尾行出通道，双出通道即报 20056；代码段原文零处理）+ **反射转调插件解析器** + 块登记 + 就地脱糖为**三步通道序列**——入通道预暂存（`ChannelIn<Kind>( entry, slVar )`，每入通道一条）→ 哨兵 `AtSignLabelCallVoid( entry )` → 出通道消费（`outVar = ChannelOut<Kind>( entry )`，有出通道时）                                                                                                                                                                                                                                |
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
    Int32 a <- $a
    Int32 b <- $b
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
| **头区**（入通道） | 块首起连续消费：空白行 / 入通道 `<slType> x <- $y`（类型标记**显式必填**，禁 var/缺省） | 首个非入通道实质行即代码段起点（Front 初判） |
| **代码段**     | 头区之后到尾行（出通道行）之前                          | **目标语言原文，Front 零处理**：整段透传插件解析器自决整编——段首 `import NS;` / `using NS;` 指令行被插件吸收；代码区出现 `<-`、`$` 前缀行、指令行 → 插件报 20055；字符串/字符/行注释/块注释内的 `<-` 是合法原文直接放行（插件三态扫描自决，Front 零复核） |
| **尾行**（出通道） | 块尾起倒序连续消费：空白行 / 出通道 `[slType] $x <- y;`  | Front 初判；**至多 1 个**出通道（第二个 → 20056）；`[slType]` 为可选类型标记（见下）                                                                                                        |

通道铁律（设计档 §A4.5）：`$名称` 必须一端；箭头指向数据目的地
（`C#形参 <- $slVar` 传入、`$slVar <- C#变量` 传出）。

入通道行**必须带显式类型标记**（2026-09-27 起 `var` 与缺省形态已禁用——
Front `TryMatchInChannelLine` 唯一合法形态 = 2 段 `<slType> target <- $slVar`，
var/缺省/多段一律 LID 20055 拒收整块）：

```
Int32 a <- $a          # C# 已定义类型 → 脱糖 ChannelInInt + 映射 C# 形参 int
String name <- $name   # → ChannelInString + 形参 string
Double r <- $r         # → ChannelInDouble + 形参 double
Boolean ok <- $ok      # → ChannelInBoolean（CVM 侧 INT 0|1 编组）
AtSignMetrics m <- $m  # SL 类名 → ChannelInObject 对象编组（镜像 class，见 §4 末）
```

slType 标记是**语言无关原文**：Front 圈地时不解释语义，只随通道表
（`channels[].slType`）下发给插件，由插件按目标语言映射——csharp\_mono
映射表（`SLLabelParser.MapCSType`）：`Int32/int/int32/i32→int`、
`Int64/long/int64/i64→long`、`Single/float/float32/f32/Float32→float`、
`Double/double/float64/f64/Float64→double`、`String/string→string`、
`Boolean/bool/boolean→bool`、`Object/object→object`；**任意合法标识符
（SL data/class 名）直通**（镜像 C# class 由 SLLabelBuilder 同批生成，
拼错/未下发 → csc 报 CS0246 拒收整块）；无法映射 → LID 20055。
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

入通道按 `channels[].slType` **七分流**（`AtSignLabelBlockDispatch.MapChannelInKind`）
选 `ChannelIn<Kind>` 变体：`Int32/int…` → `Int`、`Int64/long…` → `Long`、
`Single/float…` → `Float`、`Double/double…` → `Double`、`String` → `String`、
`Boolean` → `Boolean`、**`Object/object` 与 SL 类名（default）→ `Object`**
（对象编组，见 §4 末镜像 class）——Kind 只区分 SL 侧承载形态（值经会话
`SLLabelValue` 的 INT/FLOAT/STRING/NULL/OBJECT 编组跨界），C# 形参类型仍由
插件映射表自决（见 §3）。

- **通道包装函数**（入通道七变体 `ChannelIn{Int,Long,Float,Double,String,Boolean,Object}` +
  出通道两变体 `ChannelOut{Int,String}`）
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

### SL data/class → C# 镜像 class（对象编组）

入通道类型标记写 **SL data/class 名**（如 `AtSignMetrics m <- $m`）即触发
**镜像 class 数据映射**——SL 侧结构体整树跨界成 C# 对象，块内直接点访问字段：

1. **布局下发**（编译期）：`AtSignLabelBuildManager.CollectSlTypes` 扫描全部块的
   入通道 slType，凡非 BCL 标量白名单者按名解析 SL 类型并**递归展开字段布局**
   （data → `exportMetaDataList`；class → `ClassManager.GetClassByName` 短名兜底；
   环字段 visiting 防无限递归，类型名仍下发——镜像类自引用/互引合法），随
   Build 请求 `slTypes` 下发插件；
2. **镜像类生成**（插件）：`SLLabelBuilder` 按布局生成同字段 C# class（同
   `SLAtSign` 命名空间）合并编进 `SLAtSign.dll`，csc 编译期即校验类名/字段
   对位（拼错/未下发 → CS0246 拒收整块）；
3. **CVM 编组**（运行期 `ChannelInObject`）：SL 值按 REGULAR/DATA 布局**递归编组成
   `SLLabelObject` 镜像树**（etype `Object`/`Class` 均入此路；type_name 取短名
   ——fullName 按 `.` 截尾；嵌套 data/class 字段递归下沉，**深度限 32**，
   环/超深 → NULL 字段；非 REGULAR/DATA 对象（TYPE_OBJECT 等）仍 -89）；
4. **插件装配**（labelExec）：`mono_bridge` 按 type_name 找镜像类 → 逐字段装配
   mono 实例（int/long 直传、float/double R8、bool 0|1、string MonoString、
   嵌套对象递归装配）→ `Entry_N.Main` 形参即普通 C# 对象。

```sl
data AtSignInner { level = 0, weight = 0.0d }
data AtSignOuter { id = 0, inner = AtSignInner() }

static testAtSignDataNested()
{
    AtSignOuter o = new()
    o.id = 7
    o.inner.level = 3
    o.inner.weight = 2.5d
    int n = 0
    @csharp_mono()
    {
        AtSignOuter o <- $o
        using System;
        int n = o.inner.level * 100 + (int)(o.inner.weight * 10.0);
        $n <- n;
    }
    check( "dataNested: recursive mirror o.inner -> 325", n == 325 )
}
```

- **class 同路**：SL `class`（REGULAR 元类型 + 类信息实例）走同一编组/装配链，
  仅布局收集路径不同（class → GetClassByName，data → exportMetaDataList）；
- 已知限制：具名 data 成员的**花括号覆盖不生效**（构造后须显式赋值，
  `md/syntax/data.md` 既有限制）；出通道**无 OBJECT 变体**（只读跨界——
  C# 侧改字段不回写 SL，镜像树编组后与原实例无共享内存）；
- 用例：`CSharpTest2.sl` 的 `testAtSignDataMirror`（五标量字段 int/double/
  string/bool 往返）、`testAtSignDataNested`（嵌套 data 递归镜像）、
  `testAtSignClassMirror`（class 实例跨界 `total/step == 20`）。

### jsonc plugins 段：references / sources（额外引用与同编源码，2026-09-27）

引用方工程的 module.jsonc `plugins.csharp_mono` 段可声明两个可选键（路径相对
jsonc 所在目录，也接受绝对路径），**无需把文件手工放进插件 lib 目录**：

```jsonc
"csharp_mono": {
  "...": "...",
  "references": [ "CsExtra/ExtraMathLib.dll" ],   // 预编译 managed 程序集
  "sources":    [ "CsExtra/SlExtraUtil.cs" ]     // 与块体同批编译的 .cs 源码
}
```

- **`references[]`（预编译程序集引用）**：导出期 Front（`AtSignLabelBuildManager.CollectBuildExtras`）
  把 dll **拷入插件 libDir**（`windows-x64/`）——编译期 csc 自动扫描 `/r:` 引用（插件另按
  下发的绝对路径显式 `/r:`，与扫描去重）、运行期 mono `assemblies_path` 从插件 dll 同目录
  **按裸名解析加载**（与 SLAtSign.dll 同机制）；同时随 Build 请求下发绝对路径。改动 dll 需重编。
- **`sources[]`（同编源码）**：导出期 Front 读文件内容随 Build 请求下发
  （条目名 = `CsSrc_<文件名去扩展>`），插件**与全部块体条目同批合并编译进
  SLAtSign.dll**——每趟导出自动重编，块内头区 `using` 其命名空间即可调用其中类型
  （静态方法 / 可 new 的 class 均可）。
- 两者可同时声明、单条目可混用两侧类型（`using SLExtra; using ExtraMathLib;`）；
  sources 里的类型与镜像 class（SLAtSign 命名空间）同库共存，撞名由 csc 报错拒收。
- ⚠ **命名空间勿与内部类同名**：`namespace ExtraMath { class ExtraMath }` 会使块内
  `ExtraMath.Twice` 被 C# 解析为"在命名空间 ExtraMath 中找成员 Twice"→ **CS0234**
  （实测踩坑）；命名空间与类分开命名即可（如 `ExtraMathLib.ExtraMath`）。
- 文件缺失/读取失败仅记 Front 日志跳过，不中断导出（csc 缺引用自然报错）。
- 用例：`CSharpTest2.sl` 的 `testAtSignCsExtra`（sources 两类型 + references 一 dll
  混用：`Sum(4, Twice(5)) + |(3,-4)| == 21`）。

## 5. 约束与边界（首期）

| 约束              | 说明                                                                                                                                                                                                                 |
| --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **类型边界**        | 入通道：slType **显式必填**（禁 var/缺省 → 20055），白名单 `Int32/Int64/Single/Double/String/Boolean/Object` 及别名（映射表见 §3）+ **SL data/class 名直通**（镜像 class，见 §4 末）；脱糖按 Kind 七分流选 `ChannelIn<Kind>` 变体（见 §4）；入通道值运行期按栈槽 kind 编组 `SLLabelValue` 五态（INT/FLOAT/STRING/NULL/OBJECT——OBJECT = SL data/class 的 SLLabelObject 镜像树），不支持形态报 -89；出通道：**int/string**（类型标记变体，缺省 int；long/double/bool 等标记编译期拒收 20055，**无 OBJECT 出通道**）；出值运行期按返回对象实际类型判型（`System.String` → 深拷贝跨界 / `null` → 空值 / 其余 unbox int） |
| **出通道个数**       | 每块**至多 1 个**（第二个出通道 → LID 20056，Front 三分初判先于插件检出）                                                                                                                                                                  |
| **`<-`** **位置** | 只允许头区/尾行；代码段代码区命中 `<-` → LID 20055（消息为插件原文透传）；字符串/字符/行注释/块注释内的 `<-` 是合法目标语言原文直接放行（插件三态扫描自决，Front 零复核）                                                                                                              |
| **小括号参数**       | 仅**形态校验占位**：`name=value` 逗号分隔、名称须标识符、value 须简单字面量（禁 `; , ( ) { } < > $ " ' #`）；解析结果存 `labelParams` 元数据，**首期不参与代码生成**（空参数表 `()` 合法）                                                                                 |
| **C# 语言版本**     | .NET Framework 自带 csc（v4.0.30319）= **C#5**；块内禁用 C#6+ 语法                                                                                                                                                            |
| **外部程序集**       | 两条路：① 手工放 `simple_language_plugins/csharp_mono/cvm/lib/windows-x64/`（如 `SLCSharpTestLib.dll`，编译期自动 `/r:`、运行期 assemblies_path 兜底）；② jsonc `plugins.csharp_mono.references[]` 声明（Front 导出期自动拷入 libDir + 下发路径，见 §4 末 references/sources 小节）；额外 .cs 源码走 `sources[]` 同批合并编译                                                               |
| **csc 探测**      | 环境变量 `SIMPLELANG_CSC` 覆盖 → Framework64 → Framework（64 位优先）                                                                                                                                                         |
| **WinForms / HTTP** | 块内可用 mono BCL 的 `System.Windows.Forms` / `System.Drawing` / `System.Net`：插件 vendored BCL 树（`cvm/lib/windows-x64/lib/mono/4.5/`）已含 SWF/Drawing 五件套 + `Mono.Posix.dll`（XplatUI JIT eager 解析依赖）+ `Mono.Security.dll`（HttpWebRequest TLS 依赖）+ `machine.config`；WinForms 消息循环须 STA 子线程（块体在方法内无法标 `[STAThread]`——`SetApartmentState(ApartmentState.STA)` + `Application.Run` + `Join`），`System.Windows.Forms.Timer` 与 `System.Threading.Timer` 撞名须全限定；HTTP 等走 System.Configuration 的 BCL 路径依赖 bridge 层 `mono_domain_set_config` 挂宿主 config（`cvm_csharp_mono.config`——embedded domain 的 ConfigurationFile 缺省 null，不挂则 `WebRequest.Create` 抛 `ArgumentException: ExeConfigFilename cannot be null`）；用例 `CSharpTest3.sl` |
| **失败策略**        | csc 缺失/编译失败/部署失败仅记 Front 日志（LID 22135-22138），**不中断导出**；无构建 handler 的标签同告警不中断                                                                                                                                       |
| **插件解析器容错**     | 解析器加载/契约失败 → LID 20057（进程内只报一次），整块原样透传由后续词法解析暴露原始错误                                                                                                                                                                |
| **payload 序列化** | ⚠ IR 指令 payload 类是 `IRData.PackOpValue` 的**已知类型白名单**——新增 payload 类必须同步加分支，缺失会退化为 `ToString()` 类型全名，CVM 装配期解析失败、运行期报 -86（详见排障表）                                                                                     |

## 6. 实现机制（IR 路线）

```
.sp/.sl 源码
   │ ① Lexer（Parse/LexerParseToToken.cs，统一 @ 识别）
   │    ReadAt 读出 @<tag> → TryCaptureAtSignBlock 形态判别：
   │    参数表 (...)（FindCloseParenLexer 原文截取，未闭合不接管）+
   │    其后紧跟块体 {...}（SkipBracedBlockLexer 跳字符串/注释配平，未闭合不接管）
   │    → 整块捕获为不透明 AtSignBlock token（params/body 各一个 children
   │      token，行号指向 ( / { 所在行），m_Index 推进到 } 后；
   │    不匹配（缺参数表/缺块体/未闭合）→ 零状态改动回退 At token
   │    走 attribute 管线（其余 @ 行为完全不变）
   ▼
   │ ② TokenParseToNode → ENodeType.AtSignBlock 节点（透传）
   │ ③ StructParseToSyntax 语句层拦截 → FileMetaAtSignBlockSyntax
   │    （表达式位置不拦截 → FileMetatUtil.CreateFileMetaExpress 20059 守卫兜底）
   ▼
   │ ④ MetaCore（Core/AtSignLabelBlockDispatch.HandleDispatch，每块一次；
   │    挂 MetaMemberFunction.HandleMetaSyntax 新 case）
   │    ├─ IsPluginLabel 标签路由：按各插件清单声明的 plugin.id 路由——
   │    │  FindPluginRootById id 索引，目录名仅缺 id 时兼容；
   │    │  未命中 → 20055（"块标签未命中任何插件清单声明的 plugin.id"）
   │    │  ScanHeadTail 三分初判（头区入通道/尾行出通道；
   │    │    双出通道 → 20056；入通道非 2 段形态 → 20055；
   │    │    代码段原文零处理）
   │    ├─ AllocateEntryName() 先占 Entry_N（失败编号跳空无害，仅要求唯一）
   │    │  + 组 Parse 契约请求 → PluginFrontendParser.Parse 反射转调插件
   │    │  frontend 工程 SLCSharpMonoFrontend.dll 的 SLLabelParser.ParseLabel
   │    │  （Parse 契约单 JSON，结果小驼峰键；代码段整编全权归插件，Front 零复核；
   │    │    加载失败 → 20057（基础设施错误，进程内只报一次））
   │    │  !Ok/error → 20055（透传插件原始消息）
   │    ├─ AtSignLabelBlockCollector 块登记 + 回填（export atSignLabel[] 用）
   │    │  通道原语宿主类缺失（plugin.jsonc refModule.channelClassPath
   │    │  未配置且本块有通道）→ 20058
   │    └─ 就地脱糖（不再改写源码文本！合成 FileMeta 语句节点，行号统一 =
   │        块主 token 位置 → 诊断指向 '@' 块头行）：三步通道序列——
   │        ChannelIn<Kind>( entry, slVar )（每入通道一条）→
   │        AtSignLabelCallVoid( entry ) → outVar = ChannelOut<Kind>( entry )，
   │        逐条经 MetaMemberFunction.HandleMetaSyntax 平铺喂回当前语句链
   │    错误分流（行号基准：dispatch 层 = '@' 标签行；插件报错 =
   │      '{' 所在行 + 块体 1-based 段号 - 1，无 errorLine 兜底 '@' 行）：
   │      未知标签/入通道形态非法/!Ok/error → 20055
   │      outCountError → 20056（出通道首期仅支持 1 个）
   │      解析器加载失败 → 20057；channelClassPath 缺失 → 20058
   ▼
Front 正常编译管线（脱糖语句 = 普通系统调用语句；哨兵被 IRCall 拦截）
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
  （INT/FLOAT/STRING/NULL/OBJECT——OBJECT = SL data/class 的 `SLLabelObject`
  镜像树递归编组，深度限 32；`channels[].slType` 显式必填，供插件映射
  形参类型并触发 SL 类布局下发（镜像 class，见 §4 末）——入通道七变体、
  出通道仅 int/string）；string 入通道 MAIN/COROUTINE 借用 VMObject 内部缓冲、
  ISOLATE 走 `host->alloc` 深拷贝（V3 跨界内存规则）；string 出通道深拷贝
  utf8 跨界回 SL 栈（出值所有权独立于 mono GC 堆，V3 对称）；
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
  Front 只做统一 `@` 形态判别 + 组装上下文 + 接口化转调 + 就地脱糖——
  **Lexer 形态判别与 MetaCore 块处理已语言无关**，Front 侧与
  目标语言/工具链完全解耦，接入任意语言或特殊 SDK 只需新增插件目录（含
  frontend 实现 Parse/Build 两个单 JSON 契约），Front 零改动。

## 7. 排障

| 症状                                | 先看                                                                                                                                                                                                                                                                                                                                                                           |
| --------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 块没被识别                             | `Logs/Front.txt` 找 `AtSignLabelBlockDispatch`（未知标签 LID 20055，消息 = "块标签未命中任何插件清单声明的 plugin.id"）；确认块形态为 `@<tag>( ... ){ ... }`——**参数表与块体缺一不可**（缺参数表 / 缺块体 / 未闭合会整体回退 At token 走 attribute 管线，"没被识别"即此），参数表为空 `()` 或合法 `name=value` 列表、花括号配平                                                                                                                                                                                                                                                                             |
| 解析错误（代码段 `<-`、import 位置、通道形态、坏参数） | LID 20055（带文件行号，消息为插件原文透传）；行号换算基准 = `{` 所在行 + 块体 1-based 段号 - 1                                                                                                                                                                                                                                                                                                              |
| 多出通道                              | LID 20056；每块只留一个出通道                                                                                                                                                                                                                                                                                                                                                          |
| 插件解析器没找到                          | LID 20057；确认 `simple_language_plugins/csharp_mono/frontend/SLCSharpMonoFrontend.dll` 存在（跑 `cvm\build-csharp-mono.ps1` 或 `dotnet build frontend\SLCSharpMonoFrontend.csproj -c Release` 生成）且 `plugin.jsonc` 已声明 `frontendLibs` + `parserType`                                                                                                                                 |
| SLAtSign.dll 没生成                  | `Logs/Front.txt` 找 `cscAtSign:`（22135 csc 缺失 / 22136 编译失败 / 22137 部署失败）                                                                                                                                                                                                                                                                                                      |
| C# 语法错误                           | 22136 日志透传 csc 原始输出（行号对应生成的 Entry\_N.cs）                                                                                                                                                                                                                                                                                                                                     |
| 运行期 **-86**（payload/绑定/通道会话畸形）    | ① 检查 module.json 中 124 指令 payload 是否为 JSON 对象（若形如类型全名字符串 → `IRData.PackOpValue` 白名单缺分支，见 §5）；② `csimple_lang\build\logs\VM.txt` 找装配期 `CallAtSignLabel: payload is not a JSON call package` / entry 缺失 / 通道数不匹配 Warning；③ **通道原语失败也是 -86**（如裸调 `ChannelOut*` 无会话、会话值与消费原语类型不符）——经 `vm_sys_throw` 派发为**可捕获 VM 异常**，SL 侧 try/catch 可拦截恢复（与 coroutine/isolate/process 系统方法同错误模型） |
| 运行期 **-87**（插件/labelExec 不可用）     | 确认插件 dll 已构建部署（`cvm\build-csharp-mono.ps1`）且 `plugin.jsonc` 声明 labelExec capability                                                                                                                                                                                                                                                                                          |
| 运行期 **-88**（labelExec 执行失败）       | `VM.txt`（真实位置 `csimple_lang\build\logs\VM.txt`，cli 重定向；**非** `out\export\...\Logs\`）查插件侧原始错误                                                                                                                                                                                                                                                                                 |
| 运行期 **-89**（值编组不支持）               | 入通道/出值类型超出已支持边界（非 REGULAR/DATA 对象（TYPE_OBJECT 等）入通道、嵌套字段超深度限 32、出通道会话值与消费原语类型不符等；SL data/class 正常镜像链见 §4 末）                                                                                                                                                                                                                                                           |
| WinForms / HTTP 运行期异常                 | WinForms 初始化失败（XplatUI 类型加载）→ BCL 树缺 `Mono.Posix.dll`；`HttpWebRequest: tlsProvider` TypeLoadException / 进程 FATAL exit 255 → 缺 `Mono.Security.dll`（mono JIT eager 解析方法体全部类型引用，Windows 走 Win32 分支也要求 X11/TLS 依赖类型可解析）；`Error Initializing the configuration system`（ExeConfigFilename null）→ 宿主 config 未挂（bridge `mono_domain_set_config` + 插件目录 `cvm_csharp_mono.config`）；HTTP 超时 → 先 `curl.exe -m 8 <url>` 验证目标可达（如 ip-api.com 当前网络不可达，用例已换 httpbin.org） |

## 8. 测试

**正向用例**（`test/SpecialTest/CSharpTest.sl` + `test/SpecialTest/CSharpTest2.sl`
+ `test/SpecialTest/CSharpTest3.sl`，
宿主 `project/CSimpleVMSpecialTest`，SpecialTest 全量 **CSharpTest 26 passed +
CSharpTest2 9 passed + CSharpTest3 2 passed / 0 failed**）：

- `testAtSignMono()`：入+出通道完整往返（30+12=42）、无出通道块（`AtSignLabelCallVoid`）、
  小括号参数占位（`@csharp_mono( name=value, mode=fast )` 编译通过，参数仅存元数据）
- `testAtSignCoro()`：协程内块（spawn+await 往返 42）
- `testAtSignIsolate()`：isolate 线程内块（Isolate.run 15+27=42）
- `testAtSignIsoString()`：isolate 线程内 **string 入通道**（slType 标记
  `String name <- $name` → `Greet('sl').Length == 19`；桥接层 ISOLATE
  深拷贝路径 + `host->alloc` 所有权移交）
- `testAtSignExemptArrow()`：代码段字面量放行（字符串 `"a <- b"` / 行注释内的 `<-`
  是合法 C# 原文，插件三态扫描自决；只有代码区 `<-` 才报 20055，Front 零复核）
- `testAtSignMonoString()`：**string 出通道**（`string $s <- g` → `Main` 返回
  string，mono 装箱 `System.String` → 深拷贝 utf8 跨界 → `ChannelOutString`
  压栈，`s == "hello, sl from mono"` 往返）
- `testAtSignChannelNegative()`：**通道会话负例**（裸调 `ChannelOutInt/OutString`
  无会话 → 可捕获 VM 异常 -86，try/catch 拦截；负例后通道链路完好，正向
  出通道 6+7==13 恢复验证）
- `testAtSignDataMirror()`（CSharpTest2）：**SL data → C# 镜像 class**
  （`AtSignMetrics m <- $m`，int/double/string/bool 五标量字段全形态跨界
  往返，`width*height == 24` + 三形态 intact）
- `testAtSignDataNested()`（CSharpTest2）：**嵌套 data 递归镜像**
  （`AtSignOuter.inner` 子对象递归编组/装配，`level*100+weight*10 == 325`）
- `testAtSignClassMirror()`（CSharpTest2）：**SL class → C# 镜像 class**
  （REGULAR 实例跨界，`total/step == 20`）
- `testAtSignCsExtra()`（CSharpTest2）：**jsonc references/sources 混用**
  （sources `SLExtra.SlExtraUtil/SlPoint` 同批合并编译 + references
  `ExtraMathLib.dll` 预编译引用运行期裸名加载，
  `Sum(4, Twice(5)) + |(3,-4)| == 21`）
- `testWinFormsHttp()`（CSharpTest3）：**WinForms 窗口 + HTTP json 回显**
  （全用 mono BCL：STA 子线程 `Form` + `Label` 初始固化文字 → `Shown` 后
  `ThreadPool` 后台 `HttpWebRequest` GET `http://httpbin.org/get` →
  `BeginInvoke` 回 UI 线程把响应 json 写进 Label → dwell/safety 双 Timer
  自动关窗；`$n` 单出通道回传阶段标志和（窗口显示 1 + HTTP 响应非空 2 +
  Label 已更新 4 == 7），SL 侧两断言；依赖见 §5 WinForms/HTTP 行）

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
| `negBadSlType`      | `var unknown_tp name <- $name` 多段形态（slType 须 2 段显式；`var x <- $y` 同被 20055 拒收——var/缺省形态已禁用） | 20055  |

驱动脚本（断言 Front.txt LID 计数 20055×6 + 20056×1；Front CLI 退出码恒 0 不可作断言）：

```powershell
powershell -ExecutionPolicy Bypass -File test\Other\AtSignLabel\atsign-negative-test.ps1
```

