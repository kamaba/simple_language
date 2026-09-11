# @ 标签块（AtSignLabel）设计文档

> 版本：v1（设计稿）
> 状态：规划（未落地）
> 需求来源：
> - `test/未解决问题.txt:15`：「使用 `@csharp{ }` 调用相关的接口，在 `{}` 内是中转逻辑 `a = 0; @csharp { a = TestCSharp.TTC.getIndex( 10, a ) }`，`@js{ a = Test.calc( a, a ) }`，需要在工程里边配置 C# 版本」
> - `test/未解决问题.txt:16`：「增加 `@标签{}` 的一种逻辑……在 jsonc 的配置里边增加了 `"atSignLabel":[ { "name":"asm", "handle":{}, "platform":{...} }, { "name":"csharp", ..., "lib":"mono-static.dll" } ]`」
> - `target.md:27-32`：`@js{}` / `@cs{}` / `@java{}` / `@c{}` / `@py{}` 愿景
> - 本次补充诉求：`@gpu` 的局部参数（tile 数、每 tile 宽高）怎么设？**当前栈与运行内存怎么传入？返回值怎么回？同步还是异步？**
>
> 强相关现状文档（本设计尽量复用、不重新发明）：
> - `md/syntax/attribute.md` — `@` 的**现有唯一语义**（`@Name(...)`）
> - `md/design/TENSOR_HETEROGENEOUS_DESIGN.md` §10.2 — 已有 `@gpu{} / @cuda{} / @csharp{} / @asm{}` 设备特化体提案 + `deviceTags` jsonc 配置
> - `md/design/MONO_INTEGRATION_DESIGN.md` / `QUICKJS_INTEGRATION_DESIGN.md` / `HOTSPOT_JNI_INTEGRATION_DESIGN.md` — 三套桥接（均**未落地**）
> - `md/project/ffi.md` — 已落地的 FFI（x64、`SL_FFI_MAX_ARGS=6`、回调 8 槽、无异步）
> - `md/syntax/coroutine.md` — `Coroutine` / `Task` / `await`（异步机制的现成底座）
> - `source/Front/Lib/Core/NativeBridge.sl` — `BridgeKind{SELF,CLR,JVM,NATIVE}` + `SystemCallCLR/Native/JVMMethod`（**C VM 侧空桩**）
>
> 硬规则：`AGENT.md` R7（系统方法四处同步）、R13（新增语法同步 `md/syntax/` + `md/INDEX.md`）、R2（opcode 对齐）、R6（新 `.c` 登记 CMakeLists）。

---

## 0. 七问速答（先看这里）

| 你的问题 | 设计答案 | 一句话写法 |
|---------|---------|-----------|
| **① `@gpu` 的局部参数（tile / width / height）怎么设？** | **区分「标签参数」与「执行数据」**：参数写 `()` 里且**必须具名**（位置参数编译失败），数据用 `$` 写在 `{}` 里 | `@gpu( tile=[16,16] ) { $C[i,j] <- ... }` |
| **② 标签名能带点分层吗？** | ★ **能，层数不限**：`@gpu` / `@gpu.amd` / `@gpu.amd.tensor` / `@npu.ascend.cann.v2`。**最长匹配优先**，子标签可 `extends` 父标签 | `@gpu.amd.tensor( tile=[16,16] ) { ... }` |
| **③ SL 的数据怎么传进去 / 取出来？** | ★ **用 `$名称` 标记 SL 变量 + `<-` / `->` 做数据通道**。箭头指向**目的地**；**通道必须有一端是 `$`**。这是**数据传输**不是赋值（同步=marshal，协程=共享 pin，隔离岛=批量跨岛传，设备=DMA） | `int a <- $a;` / `$count <- addFunc(a,20);` |
| **④ 返回值怎么回？** | 两条：**(a) 通道写出** `$r <- expr`（最常用）；**(b) 宿主语言 `return`**（块作表达式时用）。两者可共存 | `@csharp { $count <- f($a); }` |
| **⑤ 协程 / 隔离岛怎么跑？** | ★ **零新语法**：`@tag(){}` 本身就是**一个特殊的闭包函数**，交给既有的 `Coroutine.spawnFunc0(...)` / `Isolate.spawnFunc0(...)` 调度。**不 spawn = 同步** | `Task t = Coroutine.spawnFunc0( @csharp { $r <- f($a); } )` |
| **⑥ 协程 / 隔离岛下通道有什么不同？** | 协程：**borrow**，共享堆只 pin，**块结束时写回**。隔离岛：**copy/move**，批量打包成 `ChannelBlob` 一次跨岛，**在 `await` 点才写回** | 见 §8.6（管道实现） |
| **⑦ 异步期间源数据被改怎么办？一致性怎么保证？** | ★ 由 **§11 值转换系统（VMS）** 保证：**默认 `SNAPSHOT` 快照语义**（提交时冻结值，源后续改动**不影响**已提交的块）；写回时用 **epoch 版本号**检测目标槽是否被改，冲突按 `onConflict`（`overwrite`/`keep`/`error`）处理。零拷贝需显式 opt-in 到 `PINNED` 或用 `TRANSFER`（源失效） | `@gpu( onConflict = "error" ) { $count <- f($A); }` |
| **⑧ signLabel 该不该独立于语言的解析体系？** | ★ **应该**（§10）。解析外置 Parser 插件、执行外置 Handler DLL，SL 只保留**三处耦合**（入参 / 出参 / 调度）。**新增标签不改编译器一行代码**。而且因为用 `$` 标记，Parser 只需**两条正则**就能覆盖所有语言 | 插件 = 一段 jsonc + 一个 dll |

**贯穿全局的一条原则**：*块内代码用宿主语言自己的语法（C# 写 `return`、JS 写 `return`、CUDA 写 `__global__`），SL 只负责「圈出这段代码 + 用 `$` 标出我方数据 + 用 `<-`/`->` 开通道 + 声明怎么执行」。* 这样既优雅，又不会让 SL 解析器去理解 N 种语言。

---

## 1. 目标、范围与原则

### 1.1 目标

让 SL 代码能够**就地内嵌一小段外部语言/设备代码**，并：

1. 自然地读写**当前作用域的局部变量**（`@csharp { a = TTC.getIndex(10, a) }`）。
2. 用**具名参数**声明该段代码的执行参数（设备调度参数、运行时选项），而非位置参数。
3. 以**最省力的方式**把结果带回 SL（赋值 / return / out 约定）。
4. **同步或异步**由标签声明、调用点可覆盖，复用既有 `Task` + `await`。
5. 全部由 `.jsonc` 的 `atSignLabel` 驱动——**标签是数据，不是语言内置关键字**。

### 1.2 设计原则（按优先级）

| # | 原则 | 具体化 |
|---|------|--------|
| **P1** | **写法优雅简单** | 99% 场景零样板：写变量名即可捕获，写宿主语言 `return` 即可返回。额外配置永远可选 |
| **P2** | **标签即数据，非关键字** | `@gpu` / `@csharp` 不是编译器内置，而是 `.jsonc` 里 `atSignLabel` 注册项。语言内核只认 `@name{...}` 形态 |
| **P3** | **不暴露栈布局** | 绝不把 VM 栈指针/帧地址交给外部语言。只给「通道表 + 访问器 + pin 句柄」 |
| **P4** | **编译期尽可能早失败** | 标签未注册、捕获了设备侧不支持的类型、`platform` 不匹配→ 能编译期报的绝不拖到运行期 |
| **P5** | **复用既有机制** | 桥接复用 `NativeBridge`/Mono/QuickJS/JNI 设计；native 产物复用 FFI 静态绑定（`OpCode_CallFFIStatic`）；异步复用 `Task`/`Coroutine`；零拷贝复用 `TransferableData` |
| **P6** | **可降级** | 平台不匹配 / 运行时缺失 → 回落 SL 主实现或报错，由 `fallback` 配置决定 |
| **P7** | ★ **插件化 / 独立子系统** | signLabel **不进 SL 解析体系**：解析外置 Parser 插件、执行外置 Handler DLL；与 SL 只保留**三处耦合**（入参 / 出参 / 调度）。详见 §10 |
| **P8** | ★ **显式数据边界** | 块内用 **`$名称`** 标记 SL 变量，用 **`<-` / `->`** 做数据通道。不猜标识符归属、不做隐式捕获。这让 Parser 只需两条正则，也让隔离岛/设备的传输语义天然明确（§4.5、§6） |

### 1.3 范围边界

**纳入**：`@name(params){ body }` 语法、捕获与参数传递、返回值、同步/异步、`atSignLabel` jsonc schema、编译流水线、marshal、降级、诊断。

**不纳入**：
- 各桥接运行时（Mono/QuickJS/JNI/HAL）的**内部实现** —— 分别在三份集成文档与 `TENSOR_HETEROGENEOUS_DESIGN.md` 中，本文只定义**它们与标签块之间的契约**。
- `@DllImport` / `@DllStaticImport` / `@Nickname` / `@AOT` / `@GPU` 等**已有 attribute 的改造**（保持兼容，见 §15.1）。

---

## 2. 现状与前置

### 2.1 `@` 符号的占用情况（必须绕开）

| 用途 | 形态 | 状态 | 证据 |
|------|------|------|------|
| **attribute** | `@Name( args )` | ✅ 已落地 | `md/syntax/attribute.md:11-17`；lexer `LexerParseToToken.cs:1043-1079` |
| 自定义 attribute | `class X extends Attribute` | ✅ 已落地 | `attribute.md:61-79`；`Core/Attribute.sl` |
| 已用 attribute | `@Nickname` / `@AOT` / `@GPU` / `@DllImport` / `@DllStaticImport` / `@Serializable` | ✅ | `Lib/Core/Nickname.sl:1`、`Std/GPU/GPU.sl:1`、`md/project/ffi.md:19-20` |
| 宏 | `$xxx`（**不是 `@`**） | ✅ | `md/syntax/marco.md:3,8-9` |
| **`@{` 块** | `@name{ ... }` | ⛔ **历史上被注释掉** | `LexerParseToToken.cs:1051-1055`：`//else if (ch == '{') { ReadChar(); AddToken(ETokenType.LeftBrace); }` |
| 矩阵乘 | 明确**禁止**用 `@` | — | `TENSOR_HETEROGENEOUS_DESIGN.md:26,455-465` |

**歧义消除规则（关键）**：`@` 后紧跟标识符，再看下一个**有效字符**：

```
@Name ( ... )   → attribute（现有行为，不变）
@Name { ... }   → ★ 标签块（本设计新增）
@Name           → 现有行为（AddAtOpSign，仅作 attribute 名）
```

`TENSOR_HETEROGENEOUS_DESIGN.md:498-504` 已为这种区分背书：
> 「`@gpu { ... }` 这种**带函数体 `{ }` 的写法**是设备特化块……与 attribute 语法形态不同（`{ }` vs `( )`），**不污染 attribute 命名空间**。」

### 2.2 已有提案与基础设施

| 已有 | 位置 | 可复用度 |
|------|------|---------|
| `@gpu/@cuda/@csharp/@asm/@arm_npu` 设备特化体提案 + `deviceTags` jsonc | `TENSOR_HETEROGENEOUS_DESIGN.md:512-570` | ★ 高 —— 本设计**接管并泛化**为 `atSignLabel`（其标签集是本设计的子集） |
| kernel 注册 `Compile.kernel(fn, tile=[16,16], block=256)`、具名调度参数 | 同上 `:475,885,1110` | ★ 直接沿用为 `@gpu` 的具名参数命名 |
| 反对 13 位置参数、主张「具名 + 默认 + autotune」 | 同上 `:152-158,630` | ★ 定为硬约束 |
| `NativeBridge` + `SystemCallCLR/Native/JVMMethod` | `Core/NativeBridge.sl`；`Core.jsonc:104-107` | 中 —— C VM 侧 `vm_sys_call_native_method` 仅 `warn-and-drop`（空桩） |
| FFI（x64、≤6 参、`CallFFIStatic` opcode=118） | `Std.jsonc:254-273`；`ffi.md`；`src/lib/ffi/` | ★ 高 —— **编译期生成的 native 产物走 `OpCode_CallFFIStatic` 直调，零 SL 帧** |
| Mono 桥（`mono-2.0-sgen.dll` 已 vendored，空桩待接通） | `MONO_INTEGRATION_DESIGN.md:19-34` | ★ 高 —— `@csharp` 的运行时 |
| QuickJS / JNI 桥 | 两份同名设计文档 | ★ —— `@js` / `@java` 的运行时 |
| `Coroutine` / `Task` / `await` / `Channel` | `Coroutine.sl`（30 syscall） | ★ —— 异步机制底座 |
| `.jsonc` 新增顶层块的登记点 | `Project/ProjectConfig.cs` + `Project/ProjectJsoncLoader.cs` | ★ —— 只需改这 2 个文件（未知块当前被**静默忽略**） |
| `TransferableData` | `Std/Isolate/TransferableData.sl` | 中 —— 设备/跨岛大数据零拷贝 |

### 2.3 缺口

| 缺口 | 说明 | 影响 |
|------|------|------|
| `ReadAt()` 不认 `@{` | 走 else 分支只打日志、不产 token | P0，必须改 |
| 无 raw 文本捕获机制 | 块体是异质代码，**不能被 SL lexer 解析** | P0，需花括号配平扫描 |
| `atSignLabel` 零实现 | jsonc loader 对未知块静默忽略 | P0 |
| 桥接 C 侧全空桩 | `vm_sys_call_clr/native/jvm_method` 未实现 | P1（各集成文档负责） |
| FFI 无异步、参≤6、无 cdecl 概念 | 见 `ffi.md` | P1（用 `CallFFIStatic` 绕开部分限制） |
| 无 `md/syntax/` 篇章 | 53 篇中无相关 | P4（R13 要求补） |

---

## 3. 概念模型

### 3.1 标签六分类（`kind`）

| kind | 含义 | 块体语言 | 典型标签 | 数据传递 | 备注 |
|------|------|---------|---------|---------|------|
| `foreign` | 外部**通用语言**运行时 | C# / JS / Java / Python / C / C++ / Dart | `@csharp` `@javascript` `@java` `@python` `@c` `@cpluslang` | `$` 通道 + accessor（可读写任意 SL 值） | 三种执行形态皆可（§8） |
| `device` | **异构设备** kernel | SL 子集 / CUDA C / HIP / OpenCL / NPU SDK | `@gpu.nvidia` `@gpu.amd` `@npu.ascend` `@cpu` `@simd.neon` | **仅 POD 标量 + Tensor**（编译期强制） | 内部可走 HAL worker 岛（实现细节，对用户透明） |
| `shader` | 图形/计算着色器 | HLSL / GLSL / WGSL / MSL | `@hlsl` `@glsl` `@wgsl` `@msl` | 同 `device` + 纹理/采样器/渲染状态 | |
| `asm` | 内联**汇编**（20+ ISA 家族） | 平台汇编 | `@asm.x86` `@asm.armv9` `@asm.riscv` `@asm.cortexm` `@asm.mcs51` | 需 `in = ()` / `out = ()` 标注（汇编无标识符解析） | **只能同步**（就地执行） |
| `ir` | **IR / 编译器后端**（非执行环境） | LLVM IR / MLIR / PTX / SPIR-V / WASM / TOSA | `@llvm` `@mlir` `@ptx` `@spirv` `@wasm` `@tosa` | POD + 标量 | AOT 成 native 后走 `OpCode_CallFFIStatic`，零 SL 帧 |
| `dsl` | 专用**领域语言** | SQL / 正则 / Protobuf IDL / OpenMP | `@sql` `@regex` `@proto` `@openmp` | 视具体 DSL | |

> **`device` 与 `shader` 为何分开**：着色器有纹理/采样器/渲染状态等额外资源绑定，与纯计算 kernel 的资源模型不同。
> **`ir` 为何独立**：它不是「执行环境」而是「我直接给你中间表示，你编译成 native 后直调」——走 codegen 路径 A（§10.9），与其它 kind 的运行时语义完全不同。
> 完整标签清单见 **§18 标签注册表**（60+ 个标签，含家族继承与别名）。

### 3.2 四个正交维度（★ 所有标签共享同一套机制）

```
        ┌── ① 参数（Parameter）    ：执行该块所需的"元参数"（调度/运行时选项）
        │      @gpu( tile=[16,16], block=256 )     ← 具名，有默认值，可 autotune
        │
        ├── ② 捕获（Capture）      ：该块能看见的 SL 数据（局部变量/this/global）
@tag ───┤      隐式：块内直接写变量名 ← 默认，零样板（仅 inline/coroutine 可用）
        │      显式：in:() out:() inout:()        ← device/shader/asm/isolate 强制
        │
        ├── ③ 结果（Result）       ：如何把结果带回 SL
        │      赋值捕获变量 / 宿主语言 return / out 约定
        │
        └── ④ 执行形态             ：★ 同步 / 协程 / 隔离岛（见 §8）
               直接写                      = 同步（当前栈跑完）
               Coroutine.spawnFunc0(...)   = 协程（不阻塞线程，拿 Task）
               Isolate.spawnFunc0(...)     = 隔离岛（崩溃隔离）
```

> **④ 之外还有「怎么解析 / 怎么执行」，但它们不属于本图**——因为它们**不在 SL 体系内**（§10）：
> 解析交给 **Parser 插件**、执行交给 **Handler DLL**，SL 只通过 `ParseResult` 与 `SLLabelHost` 两个契约对接。
> 本图的 ①②④ 恰好对应 SL 与插件之间的**三处耦合**（入参 / 出参 / 调度）。

**关键区分（回答你的 ①）**：

| | 参数（Parameter） | 捕获（Capture） |
|---|---|---|
| 写在 | `@tag( ... )` 圆括号里 | `{ ... }` 花括号里直接引用变量名 |
| 是什么 | **告诉编译器/运行时怎么跑**（tile、block、超时、C# 版本…） | **块要读写的 SL 数据** |
| 谁消费 | 标签的 handler / codegen | 块体代码 |
| 类型 | 编译期常量优先，允许运行期值 | 任意 SL 值（设备侧受限） |
| 例子 | `@gpu( tile=[16,16], block=256 )` | `{ C[i,j] = A[i,k] * B[k,j] }` 里的 `A/B/C` |

> 你问的「tile 数、每个 tile 的 width/height」属于**参数**，不是捕获。所以：`@gpu( tileNum=4, tile=[16,16] )`。

---

## 4. 语法总览（★ 本次确定的核心规则）

### 4.1 完整形式

```sl
@<tagName>( <参数名> = <参数值>, ... ) { <块体：异质代码 + $通道传输> }
```

分两段看：

```
@gpu( tile = [16,16] ) { int n <- $count;  $C[i,j] <- A[i,j] + B[i,j]; }
     └──────┬──────┘   └───────────────┬───────────────────────────┘
       ① 标签自身参数              ② 数据：用 $ 引用 SL 变量，用 <- / -> 传输
       （只描述"怎么跑"）           （← 这是"通道"，不是赋值）
```

### 4.2 标签名支持任意层级（★ 本次确定）

`@` 后的名字是**点分路径**，层级不限：

```sl
@gpu            { ... }      # 1 层
@gpu.amd        { ... }      # 2 层（AMD GPU 通用）
@gpu.amd.tensor { ... }      # 3 层（AMD tensor/matrix core 特化）
@npu.ascend.cann.v2 { ... }  # 4 层
```

| 规则 | 说明 |
|------|------|
| 分隔符 | `.`（对齐 SL 命名空间习惯 `Core.IIterable`） |
| 解析顺序 | 先查 `atSignLabel` 全名 → 再查 `atSignLabelAlias` → 都没有报 `UnknownAtSignLabel` |
| **最长匹配优先** | 若同时注册了 `gpu.amd` 与 `gpu.amd.tensor`，写 `@gpu.amd.tensor{}` 命中更具体的那个 |
| 家族继承 | `gpu.amd.tensor` 可 `extends` 到 `gpu.amd`，逐层继承（§18.3） |
| 词法 | `ReadAt()` 的标识符扫描需允许 `.`（已在 §9.2 登记） |

> 回退链同理：`atSignLabelFallbackOrder` 里写全名，如 `[ "npu.ascend.cann.v2", "gpu.amd.tensor", "gpu.nvidia", "cpu" ]`。

### 4.3 四条语法铁律（★ 你确定的）

| # | 规则 | 说明 |
|---|------|------|
| **铁律 1** | **`()` 只定义 signLabel 的参数，不是数据栈** | `()` 里只能放标签自身的配置（`@gpu` 的 `tile`/`block`、`@csharp` 的 `version`/`timeout`…），由 jsonc 的 `params[]` 声明。**执行数据不在这里传** |
| **铁律 2** | **`()` 内必须具名：`参数名 = 值`** | **位置参数一律非法**，编译期直接失败。杜绝 `@GPU(0,0,0,0,0,0,0,256,1,1,0,0,"")` 这种 13 位置参数的反面教材 |
| **铁律 3** | ★ **块内引用 SL 变量必须写 `$名称`** | `$a` = 外层 SL 作用域里的 `a`。**不带 `$` 的标识符一律是宿主语言自己的东西**，SL 绝不碰 |
| **铁律 4** | ★ **进出必须走通道 `<-` / `->`** | `目标 <- 源` 或 `源 -> 目标`，**箭头指向数据目的地**；**通道必须有一端是 `$`**。这是**数据传输**，不是赋值 |

> **铁律 3 + 4 修正了 v2 的隐式捕获**：不再靠"猜这个标识符是不是 SL 变量"，而是**显式标记 `$` + 显式方向 `<-`**。
> 收益：① 零歧义；② **Parser 插件无需懂宿主语言的标识符规则**（只需找 `$name` 和 `<-`/`->`），`rule` 形态就能覆盖所有语言；③ 方向显式 → 设备/隔离岛天然支持。

### 4.4 合法 / 非法对照（★ 铁律 2 的验收标准）

```sl
# ── ✅ 合法：全部具名 ────────────────────────────────────────────
@gpu( tile = [16,16] ) { ... }
@gpu( tile = [16,16], block = 256, shared = 4096 ) { ... }
@gpu() { ... }                                    # 空参数列表，全取默认
@gpu { ... }                                      # 省略 () 亦可
@csharp( version = "net8", timeout = 500 ) { ... }

# ── ⛔ 非法：位置参数，编译期报错 LabelParamMustBeNamed ────────────
@gpu( [16,16] ) { ... }                           # 缺参数名
@gpu( [16,16], 256 ) { ... }                      # 全是位置参数
@gpu( tile = [16,16], 256 ) { ... }               # 混合：第二个无名 → 也非法
@csharp( "net8" ) { ... }
```

**错误信息示例**：
```
error[LabelParamMustBeNamed]: @gpu 的参数必须写成「参数名 = 值」形式，不允许位置参数
  --> test.sl:12:11
   |
12 |  @gpu( [16,16] ) { ... }
   |        ^^^^^^^ 缺少参数名
   |
   help: 该标签已声明的参数有：tile, tileNum, block, grid, shared, device
         示例：@gpu( tile = [16,16] ) { ... }
```

### 4.5 ★ 通道语法：`$` 与 `<-` / `->`（本次确定）

#### 4.5.1 `$名称` = 引用外层 SL 变量

只出现在 `{}` 块体内；**不带 `$` 的标识符一律是宿主语言自己的东西**，SL 绝不碰。

```sl
Int32 a = 20
@csharp { int x <- $a; }        # $a = SL 的 a；x / int 都是 C# 的
```

#### 4.5.2 通道操作符：箭头指向**数据目的地**

```
$slVar -> 宿主目标          传入：SL → 外部
宿主目标 <- $slVar          传入（等价写法，推荐：目标在左）

宿主表达式 -> $slVar        传出：外部 → SL
$slVar <- 宿主表达式        传出（等价写法，推荐）
```

**唯一判定规则**：**通道必须恰好有一端是 `$`**。哪一端是 `$` 就决定方向。

| 写法 | 方向 | 含义 |
|------|------|------|
| `int a <- $a;` | SL → 宿主 | 把 SL 的 `a` 传入 C# 的 `a` |
| `$count <- testClass.addFunc( a, 20 );` | 宿主 → SL | 把 C# 结果传出到 SL 的 `count` |
| `$a -> int a;` | SL → 宿主 | 反向写法 不允许这样写 |
| `testClass.addFunc( a, 20 ) -> $count;` | 宿主 → SL | 反向写法，不允许|

#### 4.5.3 你的原始示例（原样保留）

```sl
test()
{
    a = 20
    count = 100
    str = "aaa"

    @csharp()
    {
        int  a <- $a;                              # ← 传入：SL 的 a  → C# 的 a
        string bstr <- $str;                       # ← 传入：SL 的 str → C# 的 bstr
        $count <- testClass.addFunc( a, 20, bstr );      # ← 传出：C# 结果 → SL 的 count  传参的时候，必须是经过本地化转换的，不允许直接传参 因为有个管道概念，要不允许数据出错
    }

    print( count )        # 已被更新
}
```

> 注意 `a` 与 `$a` 是**两个东西**：`$a` 是 SL 变量，`a` 是 C# 局部变量（由通道拷入）。这样连"同名遮蔽"都清清楚楚。

#### 4.5.4 为什么是"通道"而不是赋值（★ 你强调的点）

`<-` / `->` **不是一条赋值指令**，编译器为它生成的是**一段数据传输代码**：

| 执行形态 | `目标 <- $源` 实际生成什么 | `$目标 <- 源` 实际生成什么 |
|---------|--------------------------|--------------------------|
| **同步 / 直接执行** | `pin($源)` → marshal → 拷入外部侧 → `unpin` | marshal → 写回 SL 槽 → `unpin` |
| **协程** | 入口一次性 marshal 全部入参（共享堆，大块不拷贝，仅 pin） | 出口一次性写回（同岛共享，**写回有效**） |
| **隔离岛** | Sendable 校验 → **深拷贝或 `TransferableData` 零拷贝** → 打包进 job → `port.send` | 岛内收集结果 → 回传 → **在 `await` 点**写回 SL |
| **设备** | 标量拷入 kernel 参数槽；**Tensor 自动 H2D** | 标量拷出；**Tensor 自动 D2H** |

**关键差异**：

- 同步 / 协程下，多条 `<-` **逐条即时执行**（但可能被编译器合并优化）
- 隔离岛下，所有入参在**提交时批量打包**、所有出参在**回传时批量写回**——中间的 `<-` 不会触发 N 次跨岛往返
- 设备下，`<-` 可能变成一次 **DMA 传输**而非 CPU 拷贝

> **管道的完整物理实现（缓冲、所有权语义、pin 周期、生成代码）见 §8.6。**

#### 4.5.5 不会误判的保证

宿主语言里也有 `->`（如 C/C# 的指针成员访问 `ptr->field`），但**它两端都没有 `$`**，按「通道必须有一端是 `$`」的规则不会被误判。

```sl
@csharp { int x <- $a;  ptr->field = 5; }    # 只有第一行是通道
@c      { int x <- $a;  p->next = NULL;  }   # 同理
```

#### 4.5.6 与 `$xxx` 宏的关系（⚠ 需明确的边界）

`$xxx` 在 SL 里是**宏**（`md/syntax/marco.md`，编译前文本替换，如 `$love` → `if`）。二者冲突吗？

| 规则 | 说明 |
|------|------|
| **块体是 raw，宏替换默认不作用于块体** | 块内的 `$a` **永远**是通道引用，不会被宏展开 |
| 需要宏生效时 | 标签 jsonc 加 `"expandMacroInBody": true` 显式开启（**不推荐**，会让 `$` 语义二义） |
| 建议 | 宏名与 SL 变量尽量避免同名；实在需要时用 `global.replace` 的宏**只作用于 SL 代码区** |

### 4.6 与 attribute 的形态对比（防歧义）

```sl
@GPU( 0,0,0,0,0,0,0,256,1,1,0,0,"" )     # attribute：圆括号 + 位置参数（旧，保留兼容）
static void GpuMatMul( ... ) { }

@gpu( tile = [16,16] ) { ... }            # 标签块：圆括号具名 + 花括号体（新）
```

> 两者**可以同时存在**：`@GPU(...)` 继续作为 attribute 标注方法（旧路径保留兼容）记个其实是 @attribute 的机制，和我现在的SignLabel的机制不是一套，`@gpu{...}` 是新机制。
> **三重区分**：① `{}` vs `()`；② attribute 允许位置参数，标签块**强制具名**；③ attribute 名**不含** `$`/通道，标签块体内才有。

### 4.7 一眼分辨：参数 vs 数据 vs 通道

| 写法 | 是什么 | 放哪 |
|------|--------|------|
| `@gpu( tile = [16,16] )` 的 `tile` | **参数**（描述怎么跑） | `()` 里，具名 |
| `@csharp( version = "net8" )` 的 `version` | **参数** | `()` 里，具名 |
| `{ $C[i,j] <- A[i,j] + B[i,j] }` 的 `$C` | **通道目标**（SL 张量） | `{}` 里，带 `$` + `<-` |
| `{ int a <- $a; }` 的 `$a` | **通道源**（SL 标量） | `{}` 里，带 `$` |
| `{ $count <- addFunc(a,20); }` 的 `addFunc` | 宿主语言的函数 | `{}` 里，无 `$` |
| `{ mov eax, [a] }` 的 `a` | 宿主语言符号 | `{}` 里，无 `$` |

> 记忆口诀：**`()` 里是"设置"，`$` 是"我方数据"，`<-`/`->` 是"过管道"**。

---

## 5. ★ 参数（Parameter）：回答「`@gpu` 怎么传 tile / width / height」

> **本节说的"参数"=`()` 里的标签参数，不是执行数据**（铁律 1）。执行数据见 §6。
> **⚠ 铁律 2：所有参数必须写成 `参数名 = 值`，位置参数编译失败。**

### 5.0 参数语法的强制约定

| 约定 | 说明 |
|------|------|
| **必须具名** | `tile = [16,16]` ✅；`[16,16]` ⛔ → `LabelParamMustBeNamed` |
| **顺序无关** | 因为都具名，书写顺序不影响语义 |
| **可省略** | 声明了 `default` 的可不写 |
| **不允许未声明** | 写了 jsonc `params[]` 里没有的名字 → `UnknownLabelParam`（防拼写错） |
| **不允许混合** | 一旦出现任一项无名，整块报错（不允许"前几个具名、后面位置"） |
| **值优先常量** | 编译期常量优先；确需运行期值则走运行期传参通道 |

### 5.1 命名规范（沿用 `TENSOR_HETEROGENEOUS_DESIGN.md` 的 CUDA 词汇）

| 参数 | 视角 | 含义 | 默认 | 适用 kind |
|------|------|------|------|----------|
| `tile` | **数据** | 每个数据分块处理的元素范围，如 `[16,16]` | `auto` | device / shader |
| `tileNum` | 数据 | 分块总数；`0` = 由数据规模与 tile 推导 | `0`（自动） | device |
| `block` | **执行** | 线程块大小（CUDA `blockDim`），如 `256` 或 `[16,16]` | `auto` | device / shader |
| `grid` | 执行 | 网格维度（CUDA `gridDim`）；`auto` = 由 tileNum 推导 | `auto` | device |
| `shared` | 内存 | 每 block 动态共享内存字节数 | `0` | device / shader |
| `device` | 设备 | 目标设备 `Device.gpu(0)` / `Device.best()` | `Device.best()` | device |
| `stream` | 执行 | 设备流编号（多流 overlap） | `0` | device |
| `timeout` | 通用 | 超时毫秒；`0`=无限 | `0` | 全部 |
| `version` / `runtime` | 通用 | 运行时版本（如 C# `net8`） | jsonc 声明值 | foreign |
| `opt` | 通用 | 优化级别串（透传给 codegen） | jsonc 声明值 | 全部 |

**`auto` 与 autotune**：`TENSOR_HETEROGENEOUS_DESIGN.md:152-158,630` 明确反对写死调度参数。因此：
- 所有调度参数默认 `auto`，由 HAL 按 shape 推导；
- 用户显式给值 → 覆盖推导；
- 未来接 autotuner（`Compile.autotune`）时，`auto` 自动升级为实测选优。

### 5.2 参数如何在 jsonc 里声明（标签作者视角）

```jsonc
"atSignLabel": [
  {
    "name": "gpu",
    "kind": "device",
    "params": [
      { "name": "tile",    "type": "Int32[]", "default": "auto" },
      { "name": "tileNum", "type": "Int32",   "default": "0" },
      { "name": "block",   "type": "Int32[]", "default": "auto" },
      { "name": "grid",    "type": "Int32[]", "default": "auto" },
      { "name": "shared",  "type": "Int32",   "default": "0" },
      { "name": "device",  "type": "object",  "default": "Device.best()" }
    ]
  }
]
```

校验规则：
- 未声明的参数 → **编译期报错** `UnknownLabelParam`（防拼写错误，这是位置参数的通病）。
- 声明了 `default` 的可省略。
- 类型不匹配 → 编译期报错。
- 值**优先要求是编译期常量**；确需运行期值（如变量 `n`）则走运行期传参通道（codegen 侧退化为普通参数）。

### 5.3 典型写法

```sl
# 全部自动（推荐，交给 autotune）
@gpu { $C[i,j] <- A[i,j] + B[i,j] }

# 只要指定 tile
@gpu( tile = [16,16] ) { $C[i,j] <- A[i,j] + B[i,j] }

# 完整指定
@gpu( tile = [32,32], tileNum = 64, block = 256, shared = 4096, device = Device.gpu(0) )
{
    $C[i,j] <- A[i,j] + B[i,j]
}

# 非设备标签的参数
@csharp( version = "net8", timeout = 500 ) { $r <- TTC.calc( a ); }
```

---

## 6. ★ 通道 · 传入规则：回答「执行数据怎么进去」

> **铁律 3 + 4 的具体化**：块内用 `$名称` 引用 SL 变量，用 `目标 <- $源` / `$源 -> 目标` 做**数据通道传输**。
> **不再有隐式捕获**——不靠猜标识符归属，一切显式。

### 6.1 判定规则（★ 唯一权威，且极其简单）

编译器对块体的处理只有两步：

| 步 | 做什么 | 由谁 |
|----|--------|------|
| ① | 用正则找出所有 `$name` 和它所在的 `<-` / `->` | **Parser 插件**（`rule` 形态就够，见下） |
| ② | 把 `$name` 的名字拿到**当前 SL 作用域**查符号表 | **SL 编译器**（耦合①） |

**方向判定**（只看 `$` 在箭头哪边）：

| 写法 | 方向 | 运行期处理 |
|------|------|-----------|
| `目标 <- $源` 或 `$源 -> 目标` | **传入**（SL → 外部） | pin → marshal → 写入外部侧 |
| `$目标 <- 源` 或 `源 -> $目标` | **传出**（外部 → SL） | marshal → 写回 SL 槽 |

**合法性判定**：

| 条件 | 结果 |
|------|------|
| `$x` 的名字在当前 SL 作用域**存在** | ✅ 正常，生成通道 |
| `$x` 不存在 | ⛔ 报 `ChannelVarNotFound` |
| `<-` / `->` **两端都不是 `$`** | 不是通道（宿主语言自己的 `->`，如 `ptr->field`）→ 忽略 |
| `<-` / `->` **两端都是 `$`** | ⛔ 报 `InvalidChannel`（SL→SL 不需要通道，直接写 SL 代码） |
| `$x` 出现但**没有任何箭头** | ⛔ 报 `ChannelMissingOperator`（必须有 `<-`/`->`） |

### 6.2 ★ 这个规则让 Parser 插件变得极简

**关键收益**：插件**不需要懂宿主语言的标识符/词法规则**，只要两条正则：

```jsonc
{
  // 找所有 $name
  "channelVar":   "\\$([A-Za-z_]\\w*)",
  // 找通道操作符，并捕获左右两侧
  "channelIn":    "([^;\\n]+?)\\s*(?:<-)\\s*\\$(\\w+)",   // 目标 <- $源
  "channelInR":   "\\$(\\w+)\\s*(?:->)\\s*([^;\\n]+)",    // $源 -> 目标
  "channelOut":   "\\$(\\w+)\\s*(?:<-)\\s*([^;\\n]+)",    // $目标 <- 源
  "channelOutR":  "([^;\\n]+?)\\s*(?:->)\\s*\\$(\\w+)"    // 源 -> $目标
}
```

对比 v2 需要 `identifier` / `skipRegions` / `reserved` 等一堆语言相关配置，**这次一条正则通吃所有语言**——`@csharp` `@cuda` `@hlsl` `@asm` `@sql` 全用同一份规则。

> 这也意味着：**新增一个标签，几乎不需要写专门的 parser**，用通用 `rule` 即可（§14.14）。

### 6.3 完整示例：逐通道判定

```sl
Int32 a = 10
Int32 b = 20
Int32 r = 0
Tensor<Float32,2> A = ...
Tensor<Float32,2> C = ...

@gpu( tile = [16,16] )              # ← 只有参数
{
    Int32 i = gid(0)                #   gid/i：宿主语言（CUDA）的，无 $ → 不管
    $C[i,j] <- A[i,j] * a + b       #   $C 在 <- 左边 → 传出（写回 SL 的 C）
    int n <- $a;                    #   $a 在 <- 右边 → 传入（读 SL 的 a）
    int m <- $b;                    #   $b → 传入
    $r <- i * n + m;                #   $r → 传出
}
print( C )                          # 已更新
print( r )                          # 已更新
```

生成的通道表：

```
ChannelTable {
  { dir: In,  slVar:"a", slType: Int32,             target:"n", deviceTransfer: none }
  { dir: In,  slVar:"b", slType: Int32,             target:"m", deviceTransfer: none }
  { dir: Out, slVar:"C", slType: Tensor<Float32,2>, target:"C", deviceTransfer: D2H }
  { dir: Out, slVar:"r", slType: Int32,             target:"r", deviceTransfer: none }
}
```

> 注意 `A` **没有** `$`，所以它是宿主语言（CUDA）侧的符号——**SL 完全不碰**。
> 若你想传 SL 的 `A`，必须写 `$A`（在设备块里即自动 H2D）。

### 6.4 核心决策：给访问器，不给裸指针

> **绝不把 VM 栈指针 / 帧地址交给外部语言。** 栈布局是 VM 实现细节，一旦暴露：栈机改栈式布局、加寄存器分配、开 GC 移动 → 全部外部代码崩。

```
① Parser 插件扫出所有通道 → ParseResult.channels（含 dir / slVar / target，§10.5）
        ↓
② SL 拿 slVar 查当前 SL 符号表 → 补全类型与 slot = ChannelTable  ★ 耦合①
        ↓
③ 运行期：
   enter 块：对每个 In 通道 pin → getter → marshal → 写入外部侧
             （C# 字段 / JS 变量 / kernel 参数槽 / 汇编栈位）
   exit  块：对每个 Out 通道 setter → marshal → 写回 SL 变量
   全程：pin（防 GC 移动/回收），exit 后 unpin
```

### 6.5 可见范围（标签 jsonc 的 `scope`）

`$name` 能引用到哪些 SL 符号，由 `scope` 决定：

| `scope` 值 | `$name` 可解析到 | 适用 |
|-----------|-----------------|------|
| `"local"`（默认） | 当前函数的局部变量 + 参数 + `this` | 全部 |
| `"local,global"` | 再加 `global.*` | foreign / dsl |
| `"host"` | 不解析 `$`，仅注入 `host` 访问对象 | 逃生舱（动态名、调试） |

> `@asm` 现在**也用 `$`**（`$a <- eax` 之类），不再需要 `in = ()` / `out = ()` 参数——因为 `$` + 箭头已经表达了方向。见 §7.5。

### 6.6 设备的额外规则（回答「运行内存怎么传进去」）

| 规则 | 内容 |
|------|------|
| **只允许 POD + Tensor** | 与 `TENSOR_HETEROGENEOUS_DESIGN.md:183-194` 一致：`$A`（`Tensor<Float32,2>`）✅、`$v`（`Float32_3`）✅、`$obj`（普通 class）❌（编译期拒绝） |
| **Tensor 按方向自动搬运** | `目标 <- $A`（读）→ 自动 H2D；`$C <- 源`（写）→ 自动 D2H；若同一个 `$X` 既读又写 → H2D + D2H |
| **零拷贝优先** | 大块数据走 `TransferableData` 语义（与 Isolate 同构，`TENSOR` 文档 `:419-421`） |
| **标量按值** | `int n <- $count;` → 标量拷入 kernel 参数槽（常量缓冲） |
| **禁止引用类型** | 编译期报 `DeviceChannelNotPOD` |

### 6.7 GC 与生命周期

| 阶段 | 动作 |
|------|------|
| 进入块 | 对每个 In 通道的 SL 对象 **pin**（`vm_gc_pin`），防止移动/回收 |
| 块执行中 | 外部侧持有 GCHandle（Mono）/ `JS_DupValue`（QuickJS）/ `NewGlobalRef`（JNI）/ 设备句柄 |
| 退出块 | 执行 Out 通道写回 → unpin → 释放外部侧句柄 |
| 异常退出 | 保证 unpin（用 VM 的 defer/try-finally 语义，见 `debug-c6-throw-crash.md` 的栈隔离教训） |
| 协程 / 隔离岛 | pin 持续到 `Task` 完成（或岛回传完成）；取消时正确 unpin |

### 6.8 块执行期间能否调用回 SL？

提供受控回调（复用 FFI 的 trampoline 思路，但放宽限制）：

```sl
@csharp {
    var v = SL.call( "MyMod.Helper.Compute", a );   // 回调 SL 静态方法
    SL.throw( "boom" );                              // 抛 SL 异常
    SL.pin( obj );                                   // 显式 pin
}
```
`SL.*` 是注入宿主语言侧的**标准服务门面**（`host services`），所有标签共用同一套名字 → 学习成本一次。

---

## 7. ★ 通道 · 传出规则：回答「结果怎么出来」

### 7.1 两条传出路径（★ 你确定的规则）

| 路径 | 写法 | 何时用 | 优雅度 |
|------|------|--------|--------|
| **R1 通道传出** | `$count <- testClass.addFunc( a, 20 );` | **最常用**。写回外层 SL 变量 | ★★★ |
| **R2 宿主语言 `return`** | `Int32 r = @csharp { return f(a); }` | 需要**表达式值**时（块作表达式） | ★★★ |

> **R1 与 R2 可以共存**：既有 `$r <- ...` 又有 `return` 时，两者都生效（通道写回变量，`return` 作为块的值）。

### 7.2 R1：`$目标 <- 源`（你的写法）

```sl
Int32 r = 0
Int32 count = 100
Int32 sum = 0

@csharp { $r <- TTC.getIndex( 10, a ); }          # 写回 r
@csharp { $count <- testClass.addFunc( a, 20 ); } # 写回 count
@csharp { $sum <- $sum + a; }                     # 读且写 sum（两侧都带 $ → 见下）

print( r )      # 新值
print( count )  # 新值
print( sum )    # 新值
```

**等价的 `->` 写法**（箭头指向目的地，两种任选）：

```sl
@csharp { TTC.getIndex( 10, a ) -> $r; }
@csharp { $sum + a -> $sum; }
```

> **同侧两个 `$`**（`$sum <- $sum + a`）：仍是通道。规则是**箭头左边是目的地、右边是来源**：
> 左边 `$sum` → 生成 Out 通道；右边 `$sum` → 生成 In 通道。
> 若觉得绕，可分两段写清楚：`int t <- $sum;  $sum <- t + a;`

**R1 在各执行形态下的语义**：

| 执行形态 | R1 是否可用 | 写回时机 |
|---------|-----------|---------|
| 直接执行 | ✅ | 同步写回，块结束即可见 |
| `Coroutine.spawnFunc0(...)` | ✅ | 同岛共享，**块结束时写回**；`await` 后可见 |
| **`Isolate.spawnFunc0(...)`** | ✅ **但延迟** | 跨岛：岛内收集 → 回传 → **在 `await` 点写回** |

> 隔离岛下 R1 **仍然可用**（与 v2 不同）——因为 `$x <- ...` 本身是**显式通道**，编译器明确知道要跨岛传回。
> v2 的 `IsolateNoImplicitWriteBack` 现在只针对「不带 `$` 的隐式赋值」，而那种写法在铁律 3 下**根本不合法**，会被 `ChannelMissingOperator` 前置拦截。

### 7.3 R2：`return` 返回（跨模式通用）

块内是**宿主语言代码**，所以用**宿主语言自己的 `return`** 返回，语义零歧义：

```sl
Int32 r = @csharp { return TestCSharp.TTC.getIndex( 10, a ); }     # C# 的 return
Int32 s = @js     { return Test.calc( a, a ); }                     # JS 的 return
```

SL 侧只需：
1. 语法上允许 `@tag{...}` 出现在**表达式位置**；
2. 编译期从 jsonc 读该标签的 `returnKeyword`（默认 `return`；`asm` 为 `null` 表示不支持）；
3. 若块体缺失 `return` 且块被当表达式用 → 编译期报错 `MissingBlockReturn`。

> 这条设计让 `@csharp{}` 看起来就像「内联的 C# 方法体」，几乎不需要新学规则。
> 与 R1 的关系：`return` 的值**不走 `$` 通道**，而是作为块的表达式值直接给 SL。想同时写回某变量，再写一条 `$x <- ...`。

### 7.4 多返回值

```sl
# 方式 A：多条通道（R1，最自然）
Int32 sum = 0
Int32 count = 0
@csharp { $sum <- Calc.Sum( a );  $count <- Calc.Count( a ); }

# 方式 B：返回一个对象（R2）
var r = @csharp { return new { Sum = s, Count = c }; }     # C# 匿名对象 → SL Map/data

# 方式 C：@asm 也用 $（见 §7.5）
@asm { $sum <- eax;  $count <- ecx }
```

### 7.5 `@asm` 现在也用 `$`（统一语法）

因为 `$` + 箭头已经表达了方向，汇编可以与其他标签**写法完全一致**：

```sl
Int32 a = 3
Int32 b = 4
Int32 r = 0

@asm( isa = "x86-64" )
{
    mov eax, [$a]        # $a 在 <- 右边 → 传入：把 SL 的 a 送进 eax
    add eax, [$b]        # $b → 传入
    $r <- eax            # $r 在 <- 左边 → 传出：eax 写回 SL 的 r
}
print( r )      # 7
```

| 写法 | 方向 |
|------|------|
| `mov eax, [$a]` | 传入（读 SL 的 `a`） |
| `$r <- eax` | 传出（写 SL 的 `r`） |

> 因此 `@asm` **不再需要** `in = (...)` / `out = (...)` 参数（§4.5 统一规则）。
> 旧写法 `@asm( in = (a,b), out = (r) )` **仍兼容**（作为可选方向提示），但推荐 `$` 写法。

### 7.6 返回值的类型确定

| 场景 | 类型来源 |
|------|---------|
| R1（赋值变量） | 变量本身的类型（编译期已知，运行期校验） |
| R2（return） | **优先编译期**：若 jsonc 标签声明 `returnType` 或块被赋给有类型标注的变量，则编译期校验；否则运行期 marshal 推导 |
| 设备块 | 被赋值的 Tensor 类型（编译期） |
| 未标注 | 推导为 `object`，运行期按 marshal 表转 |

### 7.7 异常如何传回

所有标签统一：块内抛出的异常 → 转成 SL 异常**在当前位置抛出**，可被 SL 的 `try/catch` 捕获（见 `md/syntax/try.md`）。

| 宿主 | 异常映射 |
|------|---------|
| Mono | `mono_runtime_invoke` 的 `exc` 出参 → `ForeignException` |
| QuickJS | `JS_IsException` → `JS_GetException` → `ForeignException` |
| JNI | `ExceptionCheck` → `ForeignException` |
| 设备 | 设备错误 → `DeviceError`（`TENSOR` 文档 HAL 层统一） |
| 汇编 | 无异常；靠返回码 + `SL.throw()` |

---

## 8. ★ 协程与隔离岛：signLabel 作为特殊闭包（★ 你确定的方式）

> 你确定的方式：**不发明新语法**——把 `@signLabel(){ ... }` 当作**一个特殊的闭包函数**，直接交给既有的
> `Coroutine.spawnFuncX` / `Isolate.spawnFuncX` 去调度。
>
> 因此 **取消** v2 的 `place` / `wait` 双轴与 `:coro` / `:iso` / `:block` 修饰符——
> 它们被现有的 spawn 系列**完全覆盖**，而且**零新语法**。

### 8.1 三种执行形态（★ 核心）

| 形态 | 写法 | 语义 | 拿到什么 |
|------|------|------|---------|
| **① 直接执行**（默认） | `@csharp( version = "net8" ) { ... }` | 当前栈、当前协程内**同步**跑完 | 无 / 表达式值 |
| **② 协程** | `Coroutine.spawnFunc0( @csharp(...) { ... } )` | 同岛新起协程，**不阻塞线程** | `Task` |
| **③ 隔离岛** | `Isolate.spawnFunc0( @csharp(...) { ... } )` | 跨岛执行，**崩溃隔离** | `Isolate` |

> 关键：**不写 spawn 就是同步**。要异步就显式 spawn —— 与 SL 现有的 `Coroutine.spawnClosure0(function(){...})` 心智模型**完全一致**，无需学新东西。

### 8.2 为什么这个方式更好（对比 v2 的 place/wait）

| 维度 | v2 的 `place`/`wait` | ★ 你确定的 `spawnFunc` |
|------|---------------------|------------------------|
| 新语法量 | `place`/`wait` 两个字段 + `:coro`/`:iso`/`:block` 三个修饰符 | **零新语法** |
| 学习成本 | 要记 3×2 组合矩阵 | 会 `Coroutine.spawnFunc0` / `Isolate.spawnFunc0` 即可 |
| 与既有机制 | 平行造一套调度语义 | **完全复用**现有 `Task` / `await` / `SendPort` |
| 组合能力 | 固定几种组合 | 可与 `waitAll` / `waitAny` / `Channel` / `waitTimeout` 任意组合 |
| 编译期强制 | 需要 `PlaceNotDetermined` 检查 | **不需要**（不 spawn 就是同步，语义天然确定） |
| 可读性 | `@csharp :iso :block { }` | `Isolate.spawnFunc0( @csharp(){ } )` 更直白 |

### 8.3 `spawnFunc` 系列（新增重载）

```sl
# ── 现有：普通闭包 ──────────────────────────────────────────
Coroutine.spawnClosure0( closure ) -> Task
Coroutine.spawnClosure1( closure, arg0 ) -> Task
Coroutine.spawnClosure( closure, Array<object> objs ) -> Task

# ── 新增：signLabel 块（特殊闭包）★ 推荐写法 ────────────────
Coroutine.spawnFunc0( @tag( p = v ) { ... } ) -> Task
Coroutine.spawnFunc1( @tag( p = v ) { ... }, arg0 ) -> Task
Coroutine.spawnFunc2( @tag( p = v ) { ... }, arg0, arg1 ) -> Task
Coroutine.spawnFunc3( @tag( p = v ) { ... }, arg0, arg1, arg2 ) -> Task

# ── Isolate 同理（现有 spawn0..3 之外新增 Func 版）──────────
Isolate.spawnFunc0( @tag( p = v ) { ... } ) -> Isolate
Isolate.spawnFunc1( @tag( p = v ) { ... }, arg0 ) -> Isolate
```

| 问题 | 说明 |
|------|------|
| `spawnFunc` vs `spawnClosure` | `Coroutine.spawnClosure0( @csharp(){...} )` **也能工作**（signLabel 本来就是闭包的一种）；`spawnFunc` 是**语义更明确的重载**，额外负责：通道表 marshal、handler dll 加载、可能的跨岛传输 |
| 底层实现 | 复用现有 `SystemCoroutineSpawnClosure*` 与 `SystemIsolateSpawn*`，**不新增 syscall** |
| `arg0..2` 的作用 | 运行期实参，**追加进通道表**（等价于一条 In 通道）。日常推荐直接用 `$x`（更简洁），arg 版本主要用于「同一块跑不同数据」的循环场景 |

### 8.4 协程形态

```sl
# 起协程，拿 Task
Task t = Coroutine.spawnFunc0( @csharp( version = "net8" ) { return TTC.calc( a ); } )

prepareNextBatch()          # CPU 同时干活，不阻塞
var result = await t        # 同步点（挂起协程，不占线程）
```

| 语义 | 说明 |
|------|------|
| 内存 | **共享**（同岛）→ 通道两端指向同一份数据，大块**不拷贝**（仅 pin） |
| 线程 | `await` 时 `vm_coroutine_suspend_current(CORO_BLOCK_IO, reexecute=TRUE)`，线程去跑别的协程 |
| root 上下文 | `await` 退化为真实阻塞（与 `Channel.recv`、`sleep` 一致） |
| 取消 | `t.cancel()` 复用 `Coroutine.cancel`（协作式），取消时正确 unpin |

**★ 协程下的通道语义**（`$x` 怎么传）：

| 通道 | 协程下做什么 | 写回时机 |
|------|-------------|---------|
| `目标 <- $源`（传入） | 协程**启动时一次性** marshal 全部入参；同岛共享，Tensor/大对象只 pin 不拷贝 | — |
| `$目标 <- 源`（传出） | 协程**结束前**执行写回（同岛共享 → 直接写 SL 槽） | 块结束；`await` 后对调用方可见 |

```sl
Int32 count = 0
Task t = Coroutine.spawnFunc0( @csharp { $count <- Heavy.Calc( $a ); } )
# 此刻 count 还是旧值（协程未结束）
await t
print( count )     # 新值
```

> 因为同岛共享，协程下**多个 `<-` 之间没有额外开销**，编译器可合并优化。
> 管道实现细节（通道槽、pin 周期、取消时的 unpin）见 §8.6.5。

### 8.5 隔离岛形态

```sl
Isolate iso = Isolate.spawnFunc0( @csharp( version = "net8" ) { return Heavy.Calc( a ); } )
var r = await iso
```

**硬约束**（因为隔离岛按定义**不共享内存**）：

| # | 约束 | 原因 | 违反后果 |
|---|------|------|---------|
| **I1** | **禁止赋值写回**：块内 `a = ...` **无效**，必须用 `return` | 岛内改的是副本；若允许会出现「coroutine 下生效、isolate 下静默失效」的隐蔽 bug | 编译期报 `IsolateNoImplicitWriteBack` |
| **I2** | **捕获类型必须可发送**：遵循 `Sendable` 白名单（null/数值/bool/string/SendPort/Capability/TransferableData/List/Map/Set/闭包）或显式 `TransferableData` | 见 `ISOLATE_DESIGN.md` §4.5.3、`STREAM_DESIGN.md` §12.1 可发送性矩阵 | 报 `CaptureNotSendable` |
| **I3** | **块内不可回调 SL**（`SL.call` 不可用） | 岛内没有调用者的 SL 运行时上下文 | 报 `IsolateNoCallback`；需提前算好传进去 |
| **I4** | **岛是常驻 worker 池**，不是每块新建 | Mono domain / QuickJS runtime / 设备上下文都是**重资源**，每块 `mono_jit_init` 会慢到不可用 | 由 `isolate.pool` 配置（§9.1） |

> **I1 是唯一的语义代价**：换来崩溃隔离（`IsolateCrashed` 可 `try/catch` 后重试，主程序存活）。
> 与 `TENSOR_HETEROGENEOUS_DESIGN.md:411-413`「HAL 后端就是一个长生命周期 worker isolate」同构。

**★ 隔离岛下的通道语义（重点，与协程不同）**：

因为跨岛**不共享内存**，`$` 通道在隔离岛下变成**真正的批量数据传输**，而不是逐条拷贝：

| 阶段 | 编译器生成什么 |
|------|--------------|
| **① 提交前（In 通道）** | 收集所有 `目标 <- $源` → 类型可发送性校验 → **深拷贝或 `TransferableData` 零拷贝** → 打包成一条 job 消息 → `port.send(job)` |
| **② 岛内执行** | job 解包 → 宿主侧变量就绪 → 执行块体 → 所有 `$目标 <- 源` **在岛内收集到结果包**（**不写回，因为外层不可见**） |
| **③ 回传（Out 通道）** | 结果包经 `replyPort.send(result)` 回主岛 → 主岛在 **`await` 点**执行所有 Out 通道写回 → unpin |

```
主岛                                    worker 岛
─────                                   ────────
pin($a)
marshal ──┐
          ├─ job ──port.send──────────▶ 解包 → int a <- $a 的值就绪
                                        执行块体
                                        $count <- addFunc(a,20)  → 收集进 result
          ◀────result──replyPort────── 
unmarshal ┘
写回 SL 的 count（在 await 点）
unpin
```

**关键差异（务必注意）**：

| | 协程 | 隔离岛 |
|---|---|---|
| 入参 | 共享引用，只 pin | **必须拷贝 / 转移**（值语义） |
| 出参写回时机 | 块结束 | **`await` 点**（回传后） |
| 中间多条 `<-` | 逐条即时（可合并） | **批量打包**，不会触发 N 次跨岛往返 |
| 大对象 | 共享，零开销 | 必须 `TransferableData` 才零拷贝，否则深拷贝（有 `IsolateDeepCopy` 警告） |
| 块内能否再读刚写出的 `$x` | 能（同一份数据） | 不能保证（岛内是副本，`$x <- ...` 后岛内再读 `$x` 读到的是副本值） |

```sl
Int32 count = 0
Isolate iso = Isolate.spawnFunc0( @csharp { $count <- Heavy.Calc( $a ); } )
print( count )      # ⚠ 仍是旧值（还没回传）
await iso
print( count )      # ✅ 新值
```

> 管道实现细节（`ChannelBlob` 格式、批量打包、move 零拷贝、回传时序）见 §8.6.6。

### 8.6 ★ 数据传输管道（Channel Transport）

> §4.5 / §6 / §7 定义了**通道的语法与语义**（`目标 <- $源`）；本节定义**管道的物理实现**——数据到底怎么从 SL 搬到对端。
> 三种执行形态对应**三套管道实现**，但共享同一套三阶段模型与同一批数据类型策略。
>
> **管道只管"什么时候搬、搬到哪"**；"值怎么变成对方认识的形式、如何保证一致"由 **§11 值转换系统（VMS）** 负责——两者分工明确，VMS 可独立单测。

#### 8.6.1 统一模型：三个阶段

无论哪种模式，一条通道都跑完这三步：

```
┌─ ① Prepare（准备）──────────────────────────────────────┐
│  遍历 ChannelTable → 类型/可发送性校验 → 分配缓冲 → pin   │
└──────────────────────┬──────────────────────────────────┘
                       ↓
┌─ ② Transfer（传输）─────────────────────────────────────┐
│  marshal（SL 值 → SLLabelValue）→ 搬到对端可达的位置      │
│  所有权语义：borrow（借用） / copy（拷贝） / move（转移）  │
└──────────────────────┬──────────────────────────────────┘
                       ↓
┌─ ③ Commit（提交）───────────────────────────────────────┐
│  Out 通道：反向 marshal → 写回 SL 槽                     │
│  In  通道：释放临时缓冲                                   │
│  统一 unpin；异常路径也要保证 unpin                        │
└─────────────────────────────────────────────────────────┘
```

#### 8.6.2 三种所有权语义（★ 管道最本质的变量）

| 语义 | 管道做什么 | 源对象 | 开销 | 用于 |
|------|-----------|--------|------|------|
| **borrow（借用）** | 只交出**数据指针 + 类型标签**，不复制 | 不变，期间 pin | **O(1)** | 同步、协程 |
| **copy（拷贝）** | 深拷贝出独立副本 | 不变 | **O(n)** | isolate 默认、设备标量 |
| **move（转移）** | 所有权易主（指针转交） | **源失效** | **O(1)** | isolate + `TransferableData`、设备 Tensor |

> **一句话区别**：同步/协程的管道是**借用**（零拷贝，靠 pin 保证安全）；isolate 的管道只能是**拷贝或转移**（地址空间不同，指针无意义）。

#### 8.6.3 按数据类型的传输策略（三种模式共用一张表）

| 数据类型 | `SLLabelValue` 表示 | 同步 / 协程（borrow） | isolate（copy / move） | 设备 |
|---------|-------------------|---------------------|----------------------|------|
| `Bool` / `Int32` / `Int64` | `b` / `i32` / `i64` | **值直接传**，无缓冲 | 值打包进 job | 拷入 kernel 参数槽 |
| `Float32` / `Float64` | `f32` / `f64` | 值直接传 | 值打包 | 拷入参数槽 |
| `string` | `str{ptr,len}`（UTF-8，非拥有） | **借用底层字节** + pin | **深拷贝** UTF-8 字节 | ❌ 不支持 |
| `Array<T>` | `array{items,count}` | **借用底层数据指针** + pin | 深拷贝 / `TransferableData` move | ❌（用 Tensor） |
| `Tensor<T,R>` | `tensor{data,rank,shape,elemType}` | 借用数据指针 + pin | **move（零拷贝）** 优先 | **H2D / D2H（DMA）** |
| `class` 对象 | `obj`（不透明句柄） | **借用引用** + pin | **深拷贝**（需可发送）否则报 `CaptureNotSendable` | ❌（须 POD） |
| POD 小向量（`Vec3`/`Mat4`） | `bytes{ptr,len}` | 借用栈上 POD 布局 + pin | 深拷贝（按字节） | ✅ 拷入寄存器/参数槽 |
| 句柄类（`Channel`/`Stream`/socket） | `handle`（Int64） | 传句柄 | ⛔ **不可发送**（`STREAM_DESIGN.md` §12.1） | — |

#### 8.6.4 同步模式的管道（borrow，零堆分配）

**结构**：没有中间缓冲，`SLLabelValue` 直接指向 SL 栈/堆上的原始数据。

```
SL 栈帧                            SLLabelValue[]（栈上，O(1)）
┌──────────────┐                  ┌────────────────────────────┐
│ $a  Int32=20 │ ──borrow───────▶ │ { .i32 = 20 }              │
│ $str →"aaa"  │ ──borrow───────▶ │ { .str = {ptr, 3} }        │
│ $C  Tensor   │ ──borrow───────▶ │ { .tensor = {data,2,shape} }│
└──────────────┘                  └────────────┬───────────────┘
                                               ↓ 直接传给 handler dll
                                        sl_label_exec( ctx )
                                               ↓
                                    Out 通道写回：$count 的槽 ← result
```

**编译器生成的代码序列**（一条 In + 一条 Out）：

```c
/* ① Prepare */
vm_gc_pin( ctx, obj_a );                 /* 若 $a 是对象/Tensor */
vm_gc_pin( ctx, obj_C );

/* ② Transfer（无拷贝，只填指针/值） */
SLLabelValue argv[2];
argv[0].type = SL_VAL_I32;  argv[0].v.i32 = slot_read_i32( frame, slot_a );
argv[1].type = SL_VAL_TENSOR; argv[1].v.tensor = tensor_view( obj_C );

/* 执行 */
SLLabelValue retv[1];
sl_label_exec( &(SLLabelExecCtx){ .argv=argv, .argc=2, .retv=retv, .retc=1, .host=host } );

/* ③ Commit */
slot_write_i32( frame, slot_count, retv[0].v.i32 );
vm_gc_unpin( ctx, obj_a );
vm_gc_unpin( ctx, obj_C );
```

| 特性 | 说明 |
|------|------|
| 内存分配 | **零堆分配**（`SLLabelValue[]` 在 VM 栈上） |
| 拷贝 | **零拷贝**（ borrow） |
| 阻塞 | 调用线程被占住 |
| 失败处理 | 异常直接抛，unpin 由 defer 保证 |
| 适用 | 99% 的小调用、`@asm`、设备同步块 |

#### 8.6.5 协程模式的管道（borrow，但跨栈）

**关键问题**：协程有**独立的栈**。SL 是栈式 VM，`$a` 在**原函数的栈帧**里；块在**新协程**里跑，那个协程的栈帧上**没有** `a`。

**解法：通道槽（Channel Slot）**——一块与协程绑定的堆内存，在协程启动时一次性填好。

```
原协程栈帧                 通道槽（堆，协程私有）         新协程
┌────────────┐           ┌──────────────────┐        ┌──────────────┐
│ $a  =20    │─marshal─▶ │ SLLabelValue[0]  │─借用─▶ │ 块体读 $a    │
│ $str→"aaa" │─marshal─▶ │ SLLabelValue[1]  │─借用─▶ │              │
│ $C  Tensor │─marshal─▶ │ SLLabelValue[2]  │─借用─▶ │              │
└────────────┘           └──────────────────┘        └──────┬───────┘
                                  ↑                          │ 写回
                                  └──────────────────────────┘
                                    $count 的结果（块结束时）
```

**编译器生成**：

```c
/* 协程入口（由 Coroutine.spawnFunc0 包装） */
ChannelSlot* slot = channelslot_alloc( 3 );          /* 一次分配 */
channelslot_fill( slot, 0, frame, slot_a );          /* 填值（标量直接拷，对象只存指针） */
channelslot_fill( slot, 1, frame, slot_str );
channelslot_fill( slot, 2, frame, slot_C );
vm_gc_pin( ctx, obj_str );  vm_gc_pin( ctx, obj_C ); /* pin 覆盖协程全生命期 */

/* ... 协程执行块体 ... */

/* 协程出口（块结束） */
channelslot_drain( slot, frame );                    /* Out 通道写回原帧 */
vm_gc_unpin_all( slot );
channelslot_free( slot );
```

| 特性 | 说明 |
|------|------|
| 内存分配 | **一次**堆分配（通道槽），不是每通道一次 |
| 拷贝 | 标量**值拷贝**（必须，因为跨栈）；对象/字符串/Tensor **只存指针 + pin** |
| pin 周期 | **跨越协程全生命期**（不是块内），取消时也要 unpin |
| 取消 | `t.cancel()` → 协程中止 → 走正常出口的 unpin 路径（不执行 drain，避免半途写回） |
| 可见性 | 调用方要 `await` 后才看到写回结果 |
| 适用 | 耗时外部调用、想不阻塞线程、需要可取消 |

> **为什么标量必须拷？** 因为原协程可能已经返回/被调度，其栈帧可能被复用。借用原栈指针在协程下是**不安全的**。而对象/Tensor 在**堆**上，由 GC 管理，pin 后安全，所以可以继续借用（零拷贝）。

#### 8.6.6 isolate 模式的管道（copy / move，序列化）

**关键约束**：地址空间不同 → 指针无意义 → 只能 copy 或 move。

**批量打包格式 `ChannelBlob`**（不是每条通道单独发一次）：

```
ChannelBlob {
  magic  : "SLCH"
  version: 1
  entryCount : 3
  entries[] {
    nameLen, name          : "$a" / "$str" / "$C"
    typeTag                : SL 类型枚举（Int32 / String / Tensor<Float32,2>）
    encoding               : 0 = inline（小值直接内嵌）
                             1 = blob（大块数据，深拷贝）
                             2 = transferable（零拷贝转移句柄）
    size, payload
  }
}
```

**时序**：

```
主岛                                              worker 岛
────                                              ─────────
① Prepare
   - 可发送性校验（Sendable 白名单）
   - 大对象 → 打 IsolateDeepCopy 警告
   - pin
② Transfer
   - marshal → ChannelBlob
   - Tensor/大 Array：toTransferable() → move（零拷贝）
   - 其余：deep copy
   - port.send( blob )  ─────────────────────────▶  ③ 解包
                                                     - blob → SLLabelValue[]
                                                     - 执行块体
                                                     - Out 通道结果收集进 ResultBlob
   ◀────────────────────────────────────────────
④ Commit
   - ResultBlob 反向 marshal
   - 写回 SL 槽（在 await 点）
   - unpin
```

**生成代码要点**：

```c
/* 主岛：提交 */
ChannelBlob* blob = chblob_new( 3 );
chblob_put_i32   ( blob, "$a",  slot_read_i32(frame, slot_a) );            /* inline */
chblob_put_str   ( blob, "$str", str_ptr, str_len );                       /* copy   */
chblob_put_transfer( blob, "$C", transferable_from_tensor( obj_C ) );      /* move   */
port_send( jobPort, blob );        /* ★ 一次发送，不触发 N 次往返 */

/* worker 岛：执行 + 回传 */
SLLabelValue* argv = chblob_unpack( blob );
SLLabelValue  retv[1];
sl_label_exec( ctx );
ResultBlob* res = rblob_from_retv( retv, 1 );
replyPort_send( res );

/* 主岛：await 点写回 */
ResultBlob* res = recv_port_recv( replyPort );      /* 协程挂起，不占线程 */
rblob_apply( res, frame );                           /* Out 通道写回 */
vm_gc_unpin_all();
```

| 特性 | 说明 |
|------|------|
| 内存分配 | ChannelBlob（一次）+ 可能的深拷贝缓冲 |
| 拷贝 | **必须**：默认 deep copy；`TransferableData` 才 move（零拷贝） |
| 往返次数 | **1 次**（批量打包），不是 N 条通道 N 次 |
| pin 周期 | 从提交到 `await` 完成 |
| 写回时机 | **`await` 点**（不是块结束） |
| 崩溃 | 岛崩溃 → `IsolateCrashed`；主程序存活 |
| 适用 | 第三方/不可信 dll、设备驱动（HAL）、需要崩溃隔离 |

#### 8.6.7 三种管道对比总表

| | **同步** | **协程** | **isolate** |
|---|---|---|---|
| 所有权语义 | borrow | borrow（标量拷、对象借用） | copy / move |
| 中间缓冲 | 无（栈上 `SLLabelValue[]`） | 通道槽（一次堆分配） | `ChannelBlob` |
| 标量 | 值直接传 | **值拷贝**（跨栈） | 值打包 |
| 对象/字符串/Tensor | 借指针 + pin | 借指针 + pin | **深拷贝**（或 move） |
| 拷贝量 | **0** | 仅标量 | **全部**（除 move） |
| pin 周期 | 块内 | **协程全生命期** | 提交 → `await` |
| 写回时机 | 立即 | 块结束 | **`await` 点** |
| 往返 | 0 | 0 | **1** |
| 不阻塞线程 | ❌ | ✅ | ✅ |
| 崩溃隔离 | ❌ | ❌ | ✅ |
| 典型开销 | ~几十 ns | ~1 次 malloc + spawn | ~1 次序列化 + 端口往返 |

#### 8.6.8 性能优化点

| 优化 | 说明 | 分期 |
|------|------|------|
| **同步管道零分配** | `SLLabelValue[]` 放 VM 栈上，避免 malloc | P1 |
| **协程通道槽池化** | 通道槽按大小分档复用（小块内存池） | P2 |
| **isolate 大对象强制 move** | 超过阈值（如 64 KB）自动走 `TransferableData`，并提示 | P2 |
| **Tensor 直接 DMA** | 设备管道不走 CPU 中转，`$A` 的 H2D 用 `cudaMemcpyAsync` | P2 |
| **同一 blob 复用** | 循环里同一个块跑不同数据时，只更新 payload 不重建结构 | P3 |
| **编译期常量通道折叠** | `$a` 是常量时直接内联，省掉管道 | P3 |

### 8.7 设备块的默认形态

`@gpu` / `@npu` 等设备块**默认同步语义**（直接写完就等它完成）：

```sl
@gpu( tile = [16,16] ) { $C[i,j] <- A[i,j] + B[i,j] }
print( C )          # 到这行时 C 一定已就绪
```

需要 overlap 时**自己 spawn**：

```sl
Task t = Coroutine.spawnFunc0( @gpu( tile = [16,16] ) { $C[i,j] <- A[i,j] + B[i,j] } )
prepareNextBatch()
await t
```

> 设备实现层内部是否用 worker 岛（HAL）是**实现细节**，对 SL 用户透明；SL 层统一为「写就同步，spawn 就异步」。
> 设备块的管道走 §8.6.3 的「设备」列：标量拷参数槽、Tensor 走 H2D/D2H（DMA），见 §6.6。

### 8.8 挂起与唤醒时序（与 Channel / 网络 IO 同构）

```
调用点（协程内）:  await Coroutine.spawnFunc0( @gpu( tile=[16,16] ) { ... } )
  └─> vm_sys_atsign_submit( jobId, captureBlob )
        ├─ 目标（协程 / worker 岛）空闲 → 直接执行
        └─ 否则入队
      vm_coroutine_suspend_current(vm, CORO_BLOCK_IO /*=5*/, reexecute=TRUE)

执行侧:   执行块体 → 结果打包（含异常） → 回传（Task 完成 / replyPort.send）
调用侧:   收到 → vm_channel_wake_one → enqueue_ready
调度器:   恢复协程 → 重跑系统调用 → 拿到结果 → marshal 写回（R1 赋值 / R2 返回值）
```

- **root 上下文**：`await` 退化为真实阻塞（与 `Channel.recv`、`sleep` 一致）。
- **不阻塞线程**：挂起期间线程跑其它协程。
- **直接执行形态**（不 spawn）：无挂起，同步跑完即返回。

### 8.9 取消与异常

| 场景 | 行为 |
|------|------|
| `t.cancel()` | 复用 `Coroutine.cancel`（协作式）；正确 unpin 捕获变量 |
| `Isolate` 取消 | 向岛发取消消息，岛内下一调度点中止 |
| 块内抛异常（直接执行 / 协程） | 转成 SL 异常**在当前位置抛出**，`try/catch` 可捕获（§7.7） |
| 块内抛异常（隔离岛） | 经端口回传为 `ForeignException`，**在 `await` 处重新抛出** |
| 岛崩溃 | 抛 `IsolateCrashed`，主程序存活，岛按 `restartOnCrash` 重建后可重试 |
| 超时 | 超 `timeout` 参数抛 `LabelTimeout`，并尝试取消 |

### 8.10 成本模型与 lint（防止滥用 spawn）

编译器按下表给出**警告**（非错误）：

| 条件 | 警告 | 建议 |
|------|------|------|
| `Isolate.spawnFunc0` 但捕获数据总量 < 4 KB 且块体是简单表达式 | `IsolateOverkill` | 改直接执行或 `Coroutine.spawnFunc0` |
| `Isolate.spawnFunc0` 且捕获含大对象但**未**用 `Transferable` | `IsolateDeepCopy` | 改用 `TransferableData` 零拷贝 |
| `Coroutine.spawnFunc0` 但块体无 I/O、纯 CPU 且很短 | `CoroutineOverkill` | 改直接执行 |
| 直接执行但块体明显耗时（含 `sleep` / 网络 / 大 kernel） | `InlineBlocking` | 改 `Coroutine.spawnFunc0` |

> 判定是启发式的（块体是异质代码，编译器只能按 `lang` + 简单模式匹配），**只警告不报错**。

### 8.11 三种形态速查

| 需求 | 写法 |
|------|------|
| 简单同步调用 | `@csharp( version = "net8" ) { r = f(a); }` |
| 要表达式值 | `Int32 r = @csharp() { return f(a); }` |
| 不阻塞线程 + 可取消 | `Task t = Coroutine.spawnFunc0( @csharp() { return f(a); } ); await t` |
| 多任务并发 | `Coroutine.waitAll( t1, t2, t3 )` |
| 崩溃隔离 | `Isolate iso = Isolate.spawnFunc0( @csharp() { return f(a); } )` |
| CPU 与设备 overlap | `Task t = Coroutine.spawnFunc0( @gpu( tile = [16,16] ) { ... } ); prepare(); await t` |

---

## 9. `.jsonc` 配置：`atSignLabel` 完整 Schema

> 你给的形状（`name` / `handle` / `platform{os,cpu,lib}`）**保留并扩展**。
> ⚠️ 当前 jsonc loader 对未知块**静默忽略**，落地必须改 `source/Front/Project/ProjectConfig.cs` + `ProjectJsoncLoader.cs`。

```jsonc
{
  "atSignLabel": [
    {
      "name": "csharp",                       // ★ 必需：标签名，代码中写 @csharp{ }
      "kind": "foreign",                      // ★ 必需：foreign | device | shader | asm
      "lang": "csharp",                       // 块体语言（codegen/高亮/lexHints 用）
      "desc": "内联 C#（Mono 运行时）",

      "runtime": {                            // 运行时定位（对应你说的"配置 C# 版本"）
        "lib": "mono-2.0-sgen.dll",           // 运行时库（vendored / 相对路径 / 绝对）
        "version": "net8",                    // 语言/框架版本
        "entry": "mono_jit_init",             // 可选：初始化入口
        "probePath": [ "${SL_HOME}/runtime/mono", "./runtime" ]
      },

      "platform": {                           // ★ 你给的形状，扩展为数组
        "os":  [ "windows", "linux" ],
        "cpu": [ "x64", "arm64" ],
        "lib": "mono-2.0-sgen.dll"            // 保留：平台特定的库名（兼容你原设计）
      },

      "scope": "local",                       // local | local,global | host
      "returnKeyword": "return",              // 块内返回关键字；asm 用 null

      "params": [                             // ★ 具名参数声明（回答 @gpu 参数问题）
        { "name": "version", "type": "string", "default": "net8" },
        { "name": "timeout", "type": "Int32",  "default": "0" }
      ],

      "lexHints": {                           // ★ 花括号配平所需（块体是异质代码）
        "lineComment":   [ "//" ],
        "blockComment":  [ "/*", "*/" ],
        "stringDelims":  [ "\"", "'" ],       // C# 还有 @"..." / $"..." → 见 verbatimPrefix
        "verbatimPrefix": [ "@", "$", "$@" ],
        "escape": "\\\\"
      },

      "marshal": {                            // 类型映射覆盖（缺省用全局默认表 §11.12）
        "Int32": "System.Int32",
        "string": "System.String"
      },

      "parser": {                             // ★ 外置解析器（§10.4，形态 C：原生 dll）
        "kind":  "native",                    // rule | script | native
        "stage": "compile",                   // compile | runtime | both（§10.7）
        "dll":   "SlLabelCSharp.dll",
        "entry": "sl_label_csharp_parse"
      },

      "exec": {                               // ★ CVM 调用的 handler dll（§10.6 ABI）
        "kind": "dll",                        // dll | bridge | aot | sl
        "dll":  "SlLabelCSharp.dll",
        "entry": "sl_label_exec",
        "trusted": true,                      // 自有可信 dll → 可直接同步执行；否则建议 Isolate.spawnFunc0
        "options": { "optimize": true, "unsafe": false }
      },

      "fallback": "sl"                        // sl | error | next（平台不匹配时）
    },

    {
      "name": "gpu",
      "kind": "device",
      "lang": "sl-subset",
      "scope": "explicit",                    // ★ 设备必须显式 in/out
      "returnKeyword": null,
      "isolate": {                            // 被 Isolate.spawnFunc0 调度时的 worker 岛池配置
        "pool": "hal-gpu",                    // 常驻 worker 岛池名（不每块新建，§8.5 I4）
        "minWorkers": 1,
        "maxWorkers": 2,
        "restartOnCrash": true                // 岛崩溃后自动重建，当前任务抛 IsolateCrashed
      },
      "params": [
        { "name": "tile",    "type": "Int32[]", "default": "auto" },
        { "name": "tileNum", "type": "Int32",   "default": "0" },
        { "name": "block",   "type": "Int32[]", "default": "auto" },
        { "name": "grid",    "type": "Int32[]", "default": "auto" },
        { "name": "shared",  "type": "Int32",   "default": "0" },
        { "name": "device",  "type": "object",  "default": "Device.best()" }
      ],
      "captureRule": "pod-and-tensor-only",
      "fallback": "sl"
    },

    {
      "name": "asm",
      "kind": "asm",
      "lang": "x86-64",
      "scope": "explicit",                    // ★ 汇编强制显式 in/out
      "returnKeyword": null,
      "platform": { "os": ["windows","linux"], "cpu": ["x64"] },
      "fallback": "error"                     // 汇编不可降级，平台不匹配直接报错
    }
  ],

  "atSignLabelFallbackOrder": [ "asm", "cuda", "gpu", "csharp" ]   // 多标签时的优先级
}
```

### 9.1 字段说明

| 字段 | 必需 | 说明 |
|------|-----|------|
| `name` | ✔ | 标签名，`@<name>{}` 使用它。不得与已注册 attribute 冲突（见 §13.3） |
| `kind` | ✔ | `foreign` / `device` / `shader` / `asm` / `ir` / `dsl`（§3.1），决定捕获规则与 marshal 策略 |
| `lang` | | 块体语言标识，供 codegen 与 IDE 高亮 |
| `runtime` | foreign 必需 | 运行时定位：`lib` / `version` / `probePath` / `entry` |
| `platform.os` / `.cpu` | | 支持的平台数组；不匹配 → 按 `fallback` 处理 |
| `scope` | | 捕获可见范围：`local`（默认）/ `local,global` / `host` |
| **`isolate`** | 可选 | 被 `Isolate.spawnFunc0` 调度时的 worker 岛池配置：`pool`（池名，同池复用常驻岛）/ `minWorkers` / `maxWorkers` / `restartOnCrash` |
| `returnKeyword` | | 块内返回关键字，`asm` 为 `null` |
| `params[]` | | **具名参数声明**，未声明即报错（防拼写错） |
| `extends` | | 继承 `atSignLabelDefaults` 的家族模板，**避免重复定义**（见 §18.3） |
| `alias` | | 短名映射，见 §18.5 |
| `lexHints` | ✔ | 花括号配平必需的词法提示（注释/字符串/转义规则） |
| `marshal` | | 类型映射覆盖 |
| `fallback` | | `sl`（回落 SL 主实现）/ `error` / `next`（按 `atSignLabelFallbackOrder` 试下一个） |
| **`parser`** | ★ 见 §10.4 | **外置解析器**：`kind`(`rule`/`script`/`native`) + `stage`(`compile`/`runtime`/`both`) + `file`/`dll`/`entry`。输出 `ParseResult`（§10.5） |
| **`exec`** | ★ 见 §10.6 | **执行体**：`kind`(`dll`/`bridge`/`aot`/`sl`) + `dll`/`entry` + `trusted`。`trusted=true` 表示自有可信 dll（可直接同步）；不可信 dll 建议用 `Isolate.spawnFunc0` |
| `handle` | | **简写兼容名** = `parser` + `exec` 的合并写法：`{ compile → parser, runtime → exec }`。新配置请用 `parser`/`exec` |
| 其它自定义字段 | | 允许出现 `hal` / `sdk` / `isa` / `compiler` / `assembler` / `chip` 等**扩展字段**，loader 原样透传给 handler，不做校验（便于各家 SDK 自定义） |

### 9.2 落地需改的 C# 文件

| 文件 | 改动 |
|------|------|
| `source/Front/Project/ProjectConfig.cs` | 新增 `AtSignLabelSection` + `AtSignLabelItem` 数据模型（参考 `:154-171` `CompileSection`） |
| `source/Front/Project/ProjectJsoncLoader.cs` | 在 `FromJsonc` 加 `atSignLabel` 解析（参考 `:212-248` `dllImports`/`vmDlls`） |
| 新增 `source/Front/Core/MetaAtSignBlock.cs` | 通道分析（`ParseResult.channels` 的 `slVar` 查 SL 符号表）、参数校验、类型检查 |
| 新增 `source/Front/Label/LabelRegistry.cs` | 标签注册表：全名 / 别名解析 + `atSignLabelDefaults` 家族继承合并（§18.3） |
| **新增 `source/Front/Label/LabelPluginLoader.cs`** | ★ **Parser 插件加载器**（§10.4）：`rule` 内置规则引擎 / `script` 脚本宿主 / `native` 经 FFI 调 `sl_label_parse`；负责诊断行号映射 |
| 新增 `source/Front/Compile/FileMeta/FileMetaAtSignBlockSyntax.cs` | 语法节点（参考 `FileMetaAttributeSyntax.cs`） |
| `source/Front/Compile/Parse/LexerParseToToken.cs` | ① `ReadAt()` 增加 `@{` 分支 + raw 捕获（见 §10.1）；② **标识符扫描额外允许 `.`**，以支持 `@asm.x86` / `@gpu.nvidia` 分层标签名（§18.1） |
| `source/Front/Compile/Parse/StructParseFrame.cs` | 语句/表达式层识别 `@` 块 |
| `source/Front/IR/*` | 生成 `IRAtSignBlockStatements` + `IRAtSignBlockInstruction`（含调度包装） |
| `source/Front/Export/SLIR/SLModulePackageWriter.cs` | 导出 raw source + `metaJson` + 通道表 + 参数 + `exec.dll` 绑定信息 |
| **新增 `csimple_lang/src/vm/label/`** | ★ VM 侧：`vm_sys_atsign_load/free/exec`；`SLLabelHost` 服务表实现；`SLLabelValue` marshal（R6：登记 `CMakeLists.txt`） |
| 新增 `csimple_lang/src/vm/system_method_call/atsign_system_method.c` | 栈 ↔ handler dll 转发（R3：只做转发，不写平台代码） |

---

## 10. 插件化流水线：外置解析 + DLL 执行 + 三处耦合

> 本节按你的追加诉求重写：**「signLabel 是比较独立的一个模式，严格意义上和当前语言的解析过程不在一个体系；只有参数传入传出、以及 isolate/coroutine 这两处和当前语言有关系」**。
> 结论：**完全成立，且原设计有一处越界**——v1 的 §10 ④ 写「扫描 rawBody 标识符」，那等于要求 SL 编译器懂 C#/JS/CUDA 的标识符规则。这一步必须**外置给 Parser 插件**。

### 10.1 架构原则：signLabel 是独立子系统

```
┌──────────────────────────────────────────────────────────────┐
│                SL 语言体系（编译器 + VM）                      │
│                                                               │
│   ┌────────────────┐  ┌──────────────┐  ┌────────────────┐  │
│   │ ① 入参         │  │ ③ 调度       │  │ ② 出参         │  │
│   │  Capture In    │  │  Coroutine / │  │  Result Out    │  │
│   │  （栈→marshal）│  │  Isolate     │  │  （marshal→栈）│  │
│   └───────┬────────┘  └──────┬───────┘  └───────┬────────┘  │
└───────────┼──────────────────┼──────────────────┼────────────┘
            │                  │                  │
   ═════════╪══════════════════╪══════════════════╪═════ 仅此三处耦合
            │                  │                  │
┌───────────▼──────────────────▼──────────────────▼────────────┐
│          @signLabel 独立子系统（外置插件，可独立演进）          │
│                                                               │
│   ┌─────────────────────┐   ┌─────────────────────────────┐  │
│   │ Parser 插件         │   │ Handler DLL                 │  │
│   │ 规则 / 脚本 / native│──▶│ C / C++ / Rust / Zig /      │  │
│   │ → ParseResult       │   │ C#-NativeAOT / 桥接运行时    │  │
│   └─────────────────────┘   └─────────────────────────────┘  │
│   ┌───────────────────────────────────────────────────────┐  │
│   │ SLLabelHost 宿主服务表（dll 反向触碰 SL 的唯一通道）    │  │
│   └───────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────┘
```

### 10.2 三处耦合（★ 与当前语言有关的全部内容）

| # | 耦合点 | 为什么**必须**耦合 | 谁实现 |
|---|--------|------------------|--------|
| **①** | **入参（Capture In）** | 只有 SL 懂自己的**栈布局、类型系统、GC**。外部插件不可能知道 `a` 在第几个 slot | SL 编译器生成 `getter`，VM 执行 |
| **②** | **出参（Result Out）** | 同上，写回要过 SL 的类型转换与 GC 写屏障 | SL 编译器生成 `setter`，VM 执行 |
| **③** | **调度（同步 / 协程 / 隔离岛）** | 直接复用语言既有的 `Coroutine.spawnFunc0` / `Task` / `await` / `Isolate.spawnFunc0`，**零新并发原语**（§8） | SL 编译器生成包装代码 |

**除此之外的一切**——怎么解析、什么语言、怎么执行、什么 ABI、报错格式——**全部外置**，SL 编译器与 VM **零知识**。

> 这条原则的直接收益：新增一个标签（如 `@zig`）**不需要改 SL 编译器一行代码**，只需加一段 jsonc + 一个 dll。

### 10.3 编译期流水线（重画）

```
① Lexer       圈 raw（花括号配平 + lexHints）          ← SL 做
        ↓
② Parser      挂 AtSignBlock 节点（不解析内容）         ← SL 做
        ↓
③ ★ 插件调用  SL 编译器按 jsonc 调用 Parser 插件
              → ParseResult{ channels, returnsValue, diagnostics, meta }
                                                        ← 插件做（只需找 $name 与 <-/->）
        ↓
④ ★ 耦合①    通道分析：ParseResult.channels 的 slVar 查 SL 符号表 = ChannelTable
              合法性校验（POD / Sendable / 方向）        ← SL 做（要查 SL 符号表）
        ↓
⑤ ★ 耦合③    调度包装（按调用形态）：
              - 直接执行             → 同步调用序列
              - Coroutine.spawnFunc0 → 包 Task + 协程调度
              - Isolate.spawnFunc0   → 打包 job + 端口传输
                                                        ← SL 做
        ↓
⑥ Export      SLIR 存 { raw, meta, channelTable, handlerDll, execMode }
────────────────────────── 以下为运行期 ──────────────────────────
⑦ VM          加载 handler dll
              - 隔离岛形态       → 在 worker 岛加载（崩溃隔离）
              - 直接执行 / 协程  → 在主岛加载
        ↓
⑧ ★ 耦合①   入参：按 channelTable 的 In 通道从栈取值 → marshal → SLLabelValue[]
        ↓
⑨ 插件执行   vm_sys_atsign_exec( ctx ) → dll 跑具体逻辑
        ↓
⑩ ★ 耦合②   出参：SLLabelValue → marshal → 写回 SL 变量 / 返回值 → unpin
```

**关键改变**：④ 的通道分析**输入来自插件的 `channels`**（插件只需正则扫 `$name` + `<-`/`->`），SL 编译器只做「拿 `slVar` 查 SL 符号表」，**不再自己扫描异质源码、也不需要懂宿主语言的标识符规则**。

### 10.4 Parser 插件三种形态（你说「规则或脚本」）

| 形态 | jsonc 声明 | 适用 | 复杂度 | 举例 |
|------|-----------|------|--------|------|
| **A. 声明式规则** | `"kind": "rule", "file": "csharp.rule.json"` | 简单语言：只需提取标识符 + 注释/字符串规则 | 低 | `@asm.*`（标识符规则简单）、`@sql` |
| **B. 脚本** | `"kind": "script", "lang": "lua", "file": "csharp_parser.lua"` | 需要一点逻辑：括号匹配、作用域、简单语法 | 中 | `@regex`、`@shell`、DSL |
| **C. 原生 dll** | `"kind": "native", "dll": "SlLabelCSharp.dll", "entry": "sl_label_csharp_parse"` | 完整语言：需要**真解析器** | 高 | `@csharp`(Roslyn) `@cpluslang`(clang) `@hlsl`(DXC) `@cuda`(nvcc) |

**规则文件示例（形态 A）**：

```jsonc
// csharp.rule.json —— 声明式：标识符规则 + 注释/字符串 + 返回关键字
{
  "identifier":   "[A-Za-z_][A-Za-z0-9_]*(\\.[A-Za-z_][A-Za-z0-9_]*)*",
  "skipRegions": [
    { "begin": "//",  "end": "\\n" },
    { "begin": "/*",  "end": "*/" },
    { "begin": "\"",  "end": "\"", "escape": "\\\\" },
    { "begin": "@\"", "end": "\"", "escape": "\"\"" }
  ],
  "returnKeyword": "return",
  "writePatterns": [ "^\\s*([A-Za-z_][\\w.]*)\\s*=[^=]" ],
  "callPatterns":  [ "([A-Za-z_][\\w.]*)\\s*\\(" ],
  "reserved": [ "if","for","while","return","new","var","class","using" ]
}
```

### 10.5 `ParseResult` 契约（★ SL ⇄ 插件的核心接口）

```c
typedef struct SLLabelDiag {
    int         severity;   /* 0=info 1=warn 2=error */
    const char* message;
    int         line, col;  /* 相对块体；SL 编译器加上块起始行 = SL 源文件行号 */
} SLLabelDiag;

/* ★ 通道（Channel）—— 本次确定后，这是解析的唯一核心产物 */
typedef struct SLLabelChannel {
    const char* slVar;      /* 去掉 $ 的 SL 变量名，如 "count" */
    int         dir;        /* 0 = In(目标 <- $源，SL→外部)  1 = Out($目标 <- 源，外部→SL) */
    const char* target;     /* 通道另一端（宿主侧目标/来源的文本），仅诊断用 */
    int         line, col;
} SLLabelChannel;

typedef struct SLLabelParseResult {
    int   ok;
    /* 诊断：直接转成 SL 编译错误/警告，行号自动映射 */
    int   diagCount;             SLLabelDiag*       diags;
    /* ★ 通道表：捕获分析的唯一输入 */
    int   chanCount;             SLLabelChannel*    channels;
    /* 返回值信息（支撑 §7 的 R2） */
    int   returnsValue;
    const char* returnTypeHint;
    /* 块内解析出的参数（如 CUDA 里写的 tile） */
    int   paramCount;            const char** paramNames;
    /* 可选：规范化源码（宏展开 / 去注释 / 去空白） */
    const char* normalizedSource;
    /* 透传给 exec 阶段的自由数据（JSON 串，SL 不解释） */
    const char* metaJson;
} SLLabelParseResult;
```

> **`channels` 是整个设计的枢纽**：插件只需用正则找出 `$name` 与 `<-`/`->`（**不需要懂宿主语言**），
> SL 负责把 `slVar` 拿到当前符号表查类型与 slot。**双方都不需要理解对方**。
> 这也是为什么 `parser.kind = "rule"`（两条正则）就能覆盖 `@csharp` / `@cuda` / `@hlsl` / `@asm` / `@sql` 全部语言（§6.2）。

### 10.6 Handler DLL 契约（★ CVM 调用 dll 的 ABI）

```c
/* ── 值表示：跨边界传值，句柄而非裸指针 ────────────────────── */
typedef enum SLLabelValueType {
    SL_VAL_NULL=0, SL_VAL_BOOL, SL_VAL_I32, SL_VAL_I64,
    SL_VAL_F32, SL_VAL_F64, SL_VAL_STR, SL_VAL_BYTES,
    SL_VAL_ARRAY, SL_VAL_TENSOR, SL_VAL_OBJECT, SL_VAL_HANDLE
} SLLabelValueType;

typedef struct SLLabelValue {
    SLLabelValueType type;
    union {
        int     b;
        int32_t i32;
        int64_t i64;
        float   f32;
        double  f64;
        struct { const char*   ptr; int32_t len; } str;    /* UTF-8，非拥有 */
        struct { const uint8_t* ptr; int32_t len; } bytes; /* 非拥有 */
        struct { SLLabelValue* items; int32_t count; } array;
        /* 张量：device 侧专用 */
        struct { void* data; int32_t rank; const int32_t* shape; int32_t elemType; } tensor;
        void*   obj;      /* SL 对象：宿主拥有，dll 使用前必须 host->pin */
        int64_t handle;   /* ★ VM 句柄（Channel/Stream/socket…）绝不给裸指针 */
    } v;
} SLLabelValue;

/* ── 宿主服务表：dll 反向触碰 SL 的唯一通道 ─────────────────── */
typedef struct SLLabelHost {
    int   abiVersion;
    void* (*alloc)( size_t n );
    void  (*free)( void* p );
    void  (*log)( int level, const char* msg );
    void  (*error)( const SLLabelDiag* diag );
    void  (*pin)( void* slObj );            /* GC 协作 */
    void  (*unpin)( void* slObj );
    /* 反向调用 SL（仅直接执行 / 协程可用；隔离岛返回 SL_ERR_NO_CALLBACK） */
    int   (*callSL)( const char* methodSig, const SLLabelValue* args,
                     int argc, SLLabelValue* out );
    /* 设备/张量（device / shader 专用） */
    void* (*tensorData)( void* tensor );
    int   (*tensorShape)( void* tensor, int32_t* outShape, int maxRank );
    int   (*deviceCopy)( void* tensor, int direction );   /* 0=H2D 1=D2H */
    void  (*yield)( void );                 /* 长任务主动让出协程 */
} SLLabelHost;

/* ── 执行上下文 ────────────────────────────────────────────── */
typedef struct SLLabelExecCtx {
    const char*   source;      /* raw 源码（或编译期产出的 IR / AOT 产物） */
    const char*   metaJson;    /* parse 阶段的 metaJson，原样回传 */
    int           argc;        SLLabelValue* argv;   /* 入（按 captureTable 顺序） */
    int           retc;        SLLabelValue* retv;   /* 出（按 out/return 顺序） */
    SLLabelHost*  host;
    void*         execState;   /* handler 私有（编译好的 PTX / MonoMethod* / JIT code） */
    int           timeoutMs;
} SLLabelExecCtx;

/* ── dll 必须导出的 5 个符号（约定命名）──────────────────────── */
#define SL_LABEL_ABI_VERSION 1
int  sl_label_abi_version( void );
int  sl_label_init  ( SLLabelHost* host, const char* configJson );
int  sl_label_parse ( const char* source, const char* optionsJson,
                      SLLabelParseResult** out );     /* 编译期，可选 */
void sl_label_free_parse_result( SLLabelParseResult* r );
int  sl_label_exec  ( SLLabelExecCtx* ctx );
void sl_label_shutdown( void );
```

**设计要点**：

| 要点 | 说明 |
|------|------|
| **句柄而非指针** | `SL_VAL_HANDLE` 传 `Int64` 句柄（与 `Channel._chid` 同思路），dll 拿不到 VM 内部地址 |
| **宿主服务表** | dll 不直接链接 VM，只用 `SLLabelHost` 函数表 → dll 可用任何语言写，VM 重构不影响 dll |
| **内存所有权** | `str`/`bytes` 非拥有（指向 SL 侧，块执行期间有效）；dll 要留存必须 `host->alloc` + 拷贝 |
| **pin / unpin** | dll 持有 SL 对象期间必须 pin，与 §6.4 一致 |
| **ABI 版本** | `sl_label_abi_version` 不匹配 → 加载失败，避免静默崩溃 |

### 10.7 解析时机（`parser.stage`）

| stage | 含义 | 能力 | 要求 |
|-------|------|------|------|
| **`compile`** | 编译期调用插件解析 | 能校验 `$x` 是否存在于 SL 作用域、做类型校验、编译期报错 | 编译器需能加载 dll / 脚本宿主 |
| **`runtime`** | 运行期由 handler 自己解析 | 编译器**零负担** | 拿不到 SL 类型信息 → **只能做值 marshal，无法做类型校验** |
| **`both`** | 编译期检查 + 运行期执行 | 最全 | 插件需幂等（同一源码两次解析结果一致） |

> **权衡**：想让 `$x` 拼写错在**编译期**就报错 → 必须 `stage=compile`；
> `runtime` 下编译器完全不加载插件，负担最小，但 `$x` 写错要到运行期才发现。
> 建议：**常用标签走 `compile`**——因为解析只要两条正则（找 `$name` 与 `<-`/`->`），额外开销很小。

### 10.8 DLL 加载位置与执行形态的关系

| 执行形态 | dll 在哪加载 | 崩溃影响 | 建议 |
|---------|-------------|---------|------|
| 直接执行 | 主岛（进程内） | **dll 崩溃 → 进程崩溃** | 仅 `trusted: true` 的自有 dll |
| `Coroutine.spawnFunc0` | 主岛 | 同上 | 同上 |
| **`Isolate.spawnFunc0`** | **worker 岛** | 岛崩溃 → 抛 `IsolateCrashed`，**主程序存活** | **第三方 / 未验证 dll 的推荐选择** |

> 因为 dll 是外部代码，**是否隔离应按可信度决定**：
> 自有可信 dll（`trusted: true`）→ 可直接执行或用协程；第三方 / 设备驱动 dll → 用 `Isolate.spawnFunc0`。

### 10.9 执行路径对比（原 §10.2 扩展）

| 路径 | jsonc `exec.kind` | 适用 | 产物 | 开销 | 分期 |
|------|------------------|------|------|------|------|
| **A. 编译期 AOT → FFI 直调** | `aot` | `@cuda`(nvcc) `@cpluslang`(clang) `@hlsl`(DXC) | native dll / ptx / dxil | **零 SL 帧**（`OpCode_CallFFIStatic`=118） | P2 |
| **B. Handler DLL 解释执行** | `dll` | `@js` `@python` `@lua` `@sql` | 无（dll 内含运行时） | marshal + `sl_label_exec` | P1 |
| **C. VM 内建桥接** | `bridge` | `@csharp`(Mono) `@java`(JNI) | 无（复用三份集成文档） | marshal + bridge | P1 |
| **D. 降级 SL** | `sl` | 平台不匹配且 `fallback="sl"` | 无 | 普通 SL 调用 | P1 |

> B 与 C 的区别：C 是 VM **内建**（Mono/JNI 编进 VM），B 是**外置 dll**（语言运行时打包在 dll 里）。**新语言一律优先 B**，避免 VM 膨胀——这正是「独立子系统」原则的体现。

### 10.10 Lexer 关键点：raw 源码捕获

```c
// 伪代码：ReadAtBlock()
// 从 '@' 读标识符 name（允许 '.'，如 asm.x86）；再读 '(' ... ')' 具名参数（走 SL 正常解析）；
// 遇 '{' → 进入 raw 捕获模式：
//   depth = 1
//   while (depth > 0):
//       按 lexHints 跳过 lineComment / blockComment / string（含 @"..." 与转义）
//       遇 '{' → depth++；遇 '}' → depth--
//   记录起止偏移 → rawSource = src[start..end]
//   AddToken(ETokenType.At, '@', name, rawSource)
```

**注意**：
- 块体内若出现不配平的花括号（如 C# 字符串里的 `"{"`）必须靠 `lexHints` 正确跳过，否则捕获截断。这是本设计的**主要工程风险点**，建议 P1 先做严格配平 + 报错。
- 记录 raw 的**起始行列号**——这是诊断行号映射的基准（§10.5 的 `diag.line` 相对块体，需加上它）。
- 当 `parser.stage = runtime` 且 `scope = explicit` 时，lexer **仍要**做花括号配平（否则连块边界都定不了），但**不需要** `lexHints` 精确到语言级别——只需保证不误判。

---

## 11. ★ 值转换系统（Value Marshalling System, VMS）

> **这是 §8.6 管道的底层**（管道负责"搬到哪"，VMS 负责"怎么变成对方认识的值 + 保证一致"）。
> 你指出的核心风险：**同步/异步、协程/隔离岛、多语言/多线程下，同一份数据可能有多个副本或视图，稍有不慎就不一致**。
> 本节给出**带源码的一致性模型**来解决它。

### 11.1 问题：不一致从哪来

| 风险 | 场景 | 后果 |
|------|------|------|
| **① 异步期间源被改** | `Isolate.spawnFunc0(@gpu { $C <- f($A) })` 之后、await 之前，外层改了 `A` | 块用的是快照还是新值？不确定 |
| **② 写回覆盖新值** | 异步块算完后写回 `$count`，但 await 前外层已把 `count` 改成新值 | 旧结果覆盖新值 |
| **③ 共享引用被改** | `borrow` 语义下外部语言拿到了 SL 对象引用，异步期间 SL 侧改了它 | 外部看到"半新半旧" |
| **④ GC 移动** | 外部持有裸指针，GC 移动了对象 | 野指针 |
| **⑤ 设备/主机分歧** | Tensor H2D 后主机侧又改了 | 设备算的是旧数据 |
| **⑥ 跨线程** | 外部 dll 在自己的线程访问 SL 对象 | VM 非线程安全 → 崩溃 |

> **统一对策**：引入**域（Domain）** + **一致性等级（Consistency）** + **版本号（epoch）**，把"值在哪里、谁能改、改了怎么发现"全部显式化。

### 11.2 域模型（Domain）

```c
/* 值当前"住在"哪个世界里 —— 决定转换路径与一致性要求 */
typedef enum SLValueDomain {
    SL_DOMAIN_SL      = 0,   /* SL 栈 / SL 堆（本协程，可 pin）        */
    SL_DOMAIN_FOREIGN = 1,   /* 外部语言运行时（Mono / QuickJS / JNI） */
    SL_DOMAIN_DEVICE  = 2,   /* 设备内存（CUDA / NPU / 显存）          */
    SL_DOMAIN_ISOLATE = 3    /* 另一个 isolate（不同地址空间）         */
} SLValueDomain;
```

| 域 | 地址空间 | 可被 SL 直接访问 | 需要什么保护 |
|---|---------|----------------|------------|
| `SL` | 本进程 | ✅ | pin（防 GC） |
| `FOREIGN` | 本进程 | ⚠ 经桥接 | GCHandle / DupValue / GlobalRef |
| `DEVICE` | 设备 | ❌ | 显式 H2D / D2H |
| `ISOLATE` | 另地址空间 | ❌ | 序列化 / TransferableData |

### 11.3 ★ 一致性模型（核心）

```c
typedef enum SLConsistency {
    SL_CONS_SNAPSHOT = 0,  /* 快照：Transfer 时冻结值，之后源变化【不影响】已提交的块  ← 默认 */
    SL_CONS_PINNED   = 1,  /* 共享：借用同一份，pin 保护，【双向可见】，需 opt-in       */
    SL_CONS_TRANSFER = 2,  /* 转移：所有权易主，【源失效】，零拷贝                      */
    SL_CONS_DETACHED = 3   /* 分离：深拷贝独立副本，【互不影响】                        */
} SLConsistency;
```

| 等级 | 拷贝 | 源是否可改 | 块看到的值 | 用于 |
|------|------|-----------|-----------|------|
| **`SNAPSHOT`**（默认） | 标量拷值；对象拷**引用+版本号** | 可以，但**已冻结** | 提交瞬间的值，**确定** | 所有异步场景 |
| `PINNED` | 不拷贝 | 可以，**互相可见** | 实时值，**不确定** | 同步小调用（要 opt-in） |
| `TRANSFER` | 不拷贝 | **源失效** | 独占 | isolate 大对象、设备 Tensor |
| `DETACHED` | 深拷贝 | 可以，独立 | 独立副本 | 需要完全隔离 |

> **默认 `SNAPSHOT` 是刻意的**：异步下只有快照才能保证**结果可复现**。想要零拷贝必须显式 opt-in 到 `PINNED`（并承担一致性风险）或用 `TRANSFER`（源失效，无分歧）。

### 11.4 版本号（epoch）：检测"有没有被改"

```c
/* 每个可被通道引用的 SL 对象/槽位带一个 epoch；全局单调递增 */
typedef uint32_t SLEpoch;

/* VM 侧：任何写入都会 bump epoch */
void sl_slot_write_i32( SLFrame* f, int slot, int32_t v )
{
    f->slots[slot].v.i32 = v;
    f->epochs[slot] = ++g_globalEpoch;      /* ★ bump */
}
```

快照时记录 epoch，写回时比对：

```c
typedef struct SLValueView {
    SLTypeDesc*   type;
    SLValueDomain domain;
    SLConsistency cons;
    SLEpoch       epoch;        /* ★ 快照时的版本号，用于一致性检测 */
    void*         data;         /* 数据地址（含义由 domain 决定）   */
    uint32_t      size;
    void*         pinHandle;    /* pin 句柄（NULL = 未 pin）        */
    uint32_t      flags;        /* 见下 */
} SLValueView;

#define SLVF_OWN_DATA  0x01     /* data 由本视图分配，需释放 */
#define SLVF_STALE     0x02     /* epoch 已变化，值可能是旧的 */
```

### 11.5 类型描述符（让转换器知道布局）

```c
typedef enum SLTypeKind {
    SL_KIND_VOID=0, SL_KIND_BOOL, SL_KIND_I32, SL_KIND_I64,
    SL_KIND_F32, SL_KIND_F64, SL_KIND_F16, SL_KIND_F8,
    SL_KIND_STRING, SL_KIND_ARRAY, SL_KIND_TENSOR,
    SL_KIND_POD, SL_KIND_OBJECT, SL_KIND_HANDLE
} SLTypeKind;

typedef struct SLTypeDesc {
    const char*  name;       /* "Int32" / "Tensor<Float32,2>" */
    SLTypeKind   kind;
    uint32_t     size;       /* 标量/POD 大小；0 = 变长 */
    uint32_t     elemSize;   /* Array/Tensor 元素大小 */
    uint32_t     rank;       /* Tensor 秩 */
    uint32_t     isPod     : 1;
    uint32_t     isSendable: 1;   /* 能否跨 isolate */
    uint32_t     needsPin  : 1;   /* 借用时是否要 pin */
    uint32_t     isDeviceable:1;  /* 能否上设备 */
} SLTypeDesc;
```

### 11.6 转换器注册表（可扩展）

```c
typedef struct SLMarshalCtx SLMarshalCtx;

/* 转换器签名：src → dst，返回 0 成功 */
typedef int (*SLConvertFn)( SLMarshalCtx* ctx, SLValueView* src, SLValueView* dst );

typedef struct SLConverter {
    SLValueDomain from, to;
    SLTypeKind    kind;
    SLConsistency cons;        /* 该转换器要求/产生的一致性 */
    SLConvertFn   fn;
} SLConverter;

/* 注册表（启动时由各域模块注册） */
int  sl_conv_register  ( const SLConverter* c );
const SLConverter* sl_conv_find( SLValueDomain from, SLValueDomain to, SLTypeKind kind );
```

**内置转换器的注册示例**：

```c
/* SL → FOREIGN：Int32（同步/协程，值拷贝，SNAPSHOT） */
static int conv_sl_foreign_i32( SLMarshalCtx* ctx, SLValueView* s, SLValueView* d )
{
    d->type   = s->type;
    d->domain = SL_DOMAIN_FOREIGN;
    d->cons   = SL_CONS_SNAPSHOT;          /* ★ 值拷贝 = 天然快照 */
    d->data   = sl_arena_alloc( ctx->arena, 4 );
    *(int32_t*)d->data = *(int32_t*)s->data;
    d->size   = 4;
    d->flags  = 0;                          /* 数据在 arena，由 ctx 统一释放 */
    ret 0;
}

/* SL → ISOLATE：string（深拷贝，SNAPSHOT） */
static int conv_sl_isolate_string( SLMarshalCtx* ctx, SLValueView* s, SLValueView* d )
{
    d->domain = SL_DOMAIN_ISOLATE;
    d->cons   = SL_CONS_SNAPSHOT;
    d->data   = sl_arena_dup( ctx->arena, s->data, s->size );   /* 深拷贝 UTF-8 */
    d->size   = s->size;
    d->flags  = SLVF_OWN_DATA;
    ret 0;
}

/* SL → DEVICE：Tensor（DMA，TRANSFER） */
static int conv_sl_device_tensor( SLMarshalCtx* ctx, SLValueView* s, SLValueView* d )
{
    d->domain = SL_DOMAIN_DEVICE;
    d->cons   = SL_CONS_TRANSFER;
    d->data   = hal_memcpy_h2d( ctx->hal, s->data, s->size );   /* 异步 DMA */
    d->size   = s->size;
    sl_mark_host_detached( s->data );       /* ★ 主机侧标记：设备独占，主机访问报错 */
    ret 0;
}

/* 注册 */
sl_conv_register( &(SLConverter){ SL_DOMAIN_SL, SL_DOMAIN_FOREIGN, SL_KIND_I32,
                                   SL_CONS_SNAPSHOT, conv_sl_foreign_i32 } );
sl_conv_register( &(SLConverter){ SL_DOMAIN_SL, SL_DOMAIN_ISOLATE, SL_KIND_STRING,
                                   SL_CONS_SNAPSHOT, conv_sl_isolate_string } );
sl_conv_register( &(SLConverter){ SL_DOMAIN_SL, SL_DOMAIN_DEVICE, SL_KIND_TENSOR,
                                   SL_CONS_TRANSFER, conv_sl_device_tensor } );
```

### 11.7 转换上下文（统一管理生命周期）

```c
struct SLMarshalCtx {
    SLLabelHost* host;
    SLValueDomain targetDomain;

    SLArena*   arena;      /* 临时缓冲，一次性释放（避免每条通道 malloc） */
    SLPinList* pins;       /* ★ 所有 pin 的句柄，统一 unpin（异常路径也保证） */
    SLHalCtx*  hal;        /* 设备上下文（DEVICE 域用） */

    SLLabelDiag* diags;    /* 诊断累积 */
    int          diagCount;
    int          diagCap;
};

/* 统一释放：unpin 全部 → 释放 arena → 释放设备临时缓冲 */
void sl_marshal_release( SLMarshalCtx* ctx )
{
    if ( ctx->pins ) { for (...) vm_gc_unpin( h ); sl_pinlist_free( ctx->pins ); }
    if ( ctx->arena ) sl_arena_destroy( ctx->arena );
    if ( ctx->hal   ) hal_release_temp( ctx->hal );
}
```

> **关键**：`pins` 是**列表**而不是散落的调用——这样即使中途抛异常，也能在 `defer` 里一次 unpin 干净（吸取 `debug-c6-throw-crash.md` 的教训）。

### 11.8 转换主流程（传入 / 写出）

```c
/* ── 传入：SL 值 → 目标域 ───────────────────────────────── */
int sl_value_marshal_in( SLMarshalCtx* ctx, SLFrame* f, int slot,
                         SLValueView* out )
{
    SLValueView src = { 0 };
    src.type   = sl_slot_type( f, slot );
    src.domain = SL_DOMAIN_SL;
    src.data   = sl_slot_addr( f, slot );
    src.size   = sl_slot_size( f, slot );
    src.epoch  = f->epochs[slot];                    /* ★ 记录版本号 */

    /* 借用语义下必须 pin（防 GC 移动/回收） */
    if ( src.type->needsPin ) {
        src.pinHandle = vm_gc_pin( src.data );
        sl_pinlist_add( ctx->pins, src.pinHandle );
    }

    const SLConverter* c = sl_conv_find( SL_DOMAIN_SL, ctx->targetDomain, src.type->kind );
    if ( !c ) ret SL_ERR_NO_CONVERTER;

    int rc = c->fn( ctx, &src, out );
    if ( rc == 0 ) out->epoch = src.epoch;            /* ★ 传递版本号 */
    ret rc;
}

/* ── 写出：目标域 → SL 值（含一致性检查）────────────────── */
int sl_value_marshal_out( SLMarshalCtx* ctx, SLValueView* src,
                          SLFrame* f, int slot, SLOnConflict onConflict )
{
    /* ★★ 一致性检查：异步期间外层是否改过这个槽？ */
    if ( src->cons == SL_CONS_SNAPSHOT && sl_slot_is_object( f, slot ) )
    {
        if ( f->epochs[slot] != src->epoch )
        {
            switch ( onConflict ) {
            case SL_CONFLICT_OVERWRITE:                /* 默认：以块结果为准 */
                break;
            case SL_CONFLICT_KEEP:                     /* 保留外层新值，丢弃结果 */
                ret SL_OK_SKIPPED;
            case SL_CONFLICT_ERROR:                    /* 报错，交给业务 */
                ret SL_ERR_STALE_WRITEBACK;
            }
        }
    }

    const SLConverter* c = sl_conv_find( src->domain, SL_DOMAIN_SL, src->type->kind );
    if ( !c ) ret SL_ERR_NO_CONVERTER;

    SLValueView dst = { 0 };
    dst.type = sl_slot_type( f, slot );
    int rc = c->fn( ctx, src, &dst );
    if ( rc ) ret rc;

    sl_slot_write( f, slot, dst.data, dst.size );      /* 写入会 bump epoch */
    ret 0;
}
```

### 11.9 ★ 一致性检查的四个时机

| 时机 | 检查什么 | 不一致时 |
|------|---------|---------|
| **① 提交前**（Prepare） | 类型/可发送性/POD | 编译期或运行期报错 |
| **② 传输中**（Transfer） | pin 是否成功、转换是否成功 | `MarshalError` |
| **③ 异步执行中** | Snapshot 保证块看到冻结值；`PINNED` 无保证 | 由一致性等级决定 |
| **④ 写回时**（Commit） | **目标槽 epoch 是否变化** | `OVERWRITE` / `KEEP` / `ERROR` |

**冲突策略**（jsonc 可配，默认 `overwrite`）：

```jsonc
"atSignLabel": [{
  "name": "gpu",
  "onConflict": "overwrite"     // overwrite | keep | error
}]
```

```sl
# 显式要求：如果 await 期间外层改过 count，就报错而不是覆盖
Isolate iso = Isolate.spawnFunc0( @gpu( onConflict = "error" ) { $count <- f($A); } )
```

### 11.10 各场景的一致性保证（汇总）

| 场景 | 默认一致性 | 源可改？ | 块看到 | 写回冲突 |
|------|-----------|---------|--------|---------|
| **同步** | `SNAPSHOT`（标量值拷）+ `PINNED`（对象借用） | 同步期间不会 | 当前值 | 不可能（无并发） |
| **协程** | `SNAPSHOT`（标量必拷，跨栈）+ `PINNED`（对象 pin） | ✅ 可改 | **冻结值**（标量）/ 实时值（对象，需 opt-in） | 按 `onConflict` |
| **隔离岛** | `SNAPSHOT`（深拷贝）/ `TRANSFER`（move） | ✅ 可改，但已拷贝 | **冻结副本** | 按 `onConflict` |
| **设备** | `TRANSFER`（H2D 后主机侧标记 detached） | ⛔ 主机访问报错 | 设备副本 | D2H 后主机恢复 |

**设备侧的特殊处理**（风险 ⑤）：

```
$A 被 H2D  ──▶  主机侧 Tensor 标记为 DEVICE_OWNED
                 ├─ 主机访问 → 报 TensorOnDevice（或自动 D2H 同步，可配）
                 └─ 块执行完 D2H → 主机侧解除标记，数据更新
```

### 11.11 跨线程（风险 ⑥）

外部 dll 在自己的线程**不能直接碰 SL 对象**（VM 非线程安全）。规则：

| 情况 | 处理 |
|------|------|
| dll 想回调 SL | 必须经 `host->callSL(...)`，由 VM 投递回**主 VM 线程**执行 |
| isolate 模式 | 岛有独立的 VM 实例，`SL.call` **不可用**（`IsolateNoCallback`） |
| 设备 | 设备在另一个硬件域，无 SL 运行时 |

### 11.12 类型映射表（转换器据此实现）

| SL 类型 | C#（Mono） | JS（QuickJS） | Java（JNI） | `@asm` | 设备 |
|---------|-----------|--------------|------------|--------|------|
| `bool` | `System.Boolean` | boolean | `jboolean` | 8/32 位寄存器 | ❌（用 `Int32`） |
| `Int8/16/32` | `System.Int32` | number | `jint` | 寄存器 | ✅ i32 |
| `Int64` | `System.Int64` | number/BigInt | `jlong` | 寄存器 | ✅ i64 |
| `Float32/64` | `System.Single/Double` | number | `jfloat/jdouble` | XMM | ✅ f32/f64 |
| `Float8/16` | `Half`/自定义 | number | `jshort`(位模式) | XMM | ✅ f16/f8 |
| `string` | `System.String` | string | `jstring` | 指针 | ❌ |
| `Array<T>` | `T[]` | Array | `jarray` | 指针 | ❌（用 `Tensor`） |
| `Tensor<T,R>` | 桥接对象 | — | — | — | ✅ **设备指针** |
| `Vec3/Mat4`（POD） | 结构体 | — | — | 栈 | ✅ |
| 普通 `class` | GCHandle 对象 | JSObject | jobject | ❌ | ❌ 编译期拒绝 |
| `object` | `System.Object` | any | — | ❌ | ❌ |

> 与 `MONO_INTEGRATION_DESIGN.md:225-241`、`QUICKJS:222-238`、`HOTSPOT_JNI:225-237` 三份文档的 marshal 表**保持一致**；本设计只新增「设备」列与**一致性/版本机制**。

### 11.13 错误与诊断

| 错误 | 触发 |
|------|------|
| `MarshalNoConverter` | 找不到 (源域, 目标域, 类型) 的转换器 |
| `MarshalTypeError` | 类型与目标槽不匹配 |
| `MarshalPinFailed` | pin 失败（GC 状态异常） |
| **`StaleWriteBack`** | ★ 异步写回时发现目标槽 epoch 已变，且 `onConflict = "error"` |
| `TensorOnDevice` | Tensor 已 H2D，主机侧仍访问（风险 ⑤） |
| `MarshalCrossThread` | 外部线程直接访问 SL 对象（风险 ⑥） |

### 11.14 实现清单（落地文件）

| 文件 | 内容 |
|------|------|
| `csimple_lang/src/vm/value/`（新增，R6 登记 CMakeLists） | `sl_value.h/.c`（域/一致性/epoch）、`sl_type_desc.c`、`sl_converter.c`（注册表 + 内置转换器）、`sl_marshal_ctx.c`（arena + pinlist） |
| `csimple_lang/src/vm/label/` | 管道实现，调用 VMS（§8.6） |
| `csimple_lang/src/base/` | `sl_arena`（临时缓冲）、`sl_pinlist`（pin 列表）复用/新增 |

> **与管道的分工**：**VMS 管"值变成什么、一致吗"，管道管"什么时候搬、搬到哪"**。
> VMS 是纯函数式的（不碰调度），因此**可独立单测**——这是它必须独立成系统的主要理由。

---

## 12. 降级与回退

| 触发 | `fallback` | 行为 |
|------|-----------|------|
| 平台不匹配（os/cpu） | `sl` | 使用同一位置的 SL 主实现体（见 §14.4） |
| 运行时缺失（Mono dll 找不到） | `sl` | 同上，并可选告警 |
| 运行时缺失 | `error` | 编译期报错（`@asm` 默认） |
| 多个设备特化块 | `next` | 按 `atSignLabelFallbackOrder` 选第一个可用的 |

**设备特化体的经典形态**（沿用 `TENSOR_HETEROGENEOUS_DESIGN.md:521-534`）：

```sl
kernel matmul( A, B )          # SL 主实现 = 兜底
{
    ret A · B
}
@gpu { ... }                   # GpuHal 特化
@cuda { ... }                  # CUDA C
@arm_npu { ... }               # NPU
@csharp { ... }                # C# BLAS
@asm { ... }                   # CPU 汇编
```
运行时按优先级挑一个可用的；全不可用 → 用 SL 主体。**这套写法在本文档下无需改动即可工作**，因为「kernel 主实现 + 若干 `@tag{}` 特化体」只是标签块的一种组合用法。

---

## 13. 错误与诊断

### 13.1 编译期错误

| 错误 | 触发 |
|------|------|
| `UnknownAtSignLabel` | `@foo{}` 未在 `atSignLabel` 注册 |
| `AtSignLabelNotAvailable` | 平台/运行时不可用且 `fallback=error` |
| `UnknownLabelParam` | 传了未声明的具名参数 |
| `LabelParamTypeMismatch` | 参数类型不符 |
| `LabelParamNotConst` | 需要编译期常量的参数传了运行期值 |
| `MissingBlockReturn` | 块作表达式用但缺 `returnKeyword` |
| **`ChannelVarNotFound`** | §6.1：`$x` 的名字在当前 SL 作用域**不存在**（help 列出作用域内可用变量） |
| **`ChannelMissingOperator`** | §6.1：`$x` 出现但**不在任何 `<-` / `->` 的任一侧**（必须有通道操作符） |
| **`InvalidChannel`** | §6.1：`<-` / `->` **两端都是 `$`**（SL→SL 不需要通道，直接写 SL 代码） |
| **`DeviceChannelNotPOD`** | §6.6：设备块的 `$x` 是非 POD / 非 Tensor（普通 class） |
| `UnbalancedBraceInBlock` | raw 捕获花括号不配平 |
| `AtSignLabelNameConflict` | 标签名与已注册 attribute 同名（见 §13.3） |
| **`LabelParamMustBeNamed`** | ★ 铁律 2：`()` 里出现位置参数（缺 `参数名 =`），见 §4.3 |
| **`CaptureNotSendable`** | §8.5 I2：`Isolate.spawnFunc0` 的 `$x` 是不可跨岛发送的类型（句柄类、`ByteBuf.slice()` 等） |
| **`IsolateNoImplicitWriteBack`** | §8.5 I1：`Isolate.spawnFunc0` 下对**非 `$` 变量**赋值（副本写不回），必须改用 `$x <-` 或 `return` |
| **`IsolateNoCallback`** | §8.5 I3：`Isolate.spawnFunc0` 块内调用 `SL.call` 等回调 |
| **`PluginNotFound`** | §10.4：`parser.dll` / `exec.dll` 文件不存在或加载失败 |
| **`PluginAbiMismatch`** | §10.6：`sl_label_abi_version` 与 VM 不一致 |
| **`PluginMissingSymbol`** | dll 缺 `sl_label_parse` / `sl_label_exec` 等必需导出 |
| **`PluginParseFailed`** | 插件解析块体失败（诊断信息由 `ParseResult.diagnostics` 提供，行号自动映射回 SL 源文件） |
| **`PluginExecFailed`** | `sl_label_exec` 返回非 0（错误信息由 `host->error` 提供） |
**编译期警告（非错误）**：

| 警告 | 触发 | 建议 |
|------|------|------|
| `IsolateOverkill` | §8.10：`Isolate.spawnFunc0` 但数据量小、块体简单 | 改直接执行或协程 |
| `IsolateDeepCopy` | §8.10：`Isolate.spawnFunc0` 含大对象但未用 `Transferable` | 改用零拷贝 |
| `CoroutineOverkill` | §8.10：`Coroutine.spawnFunc0` 但块体短且无 I/O | 改直接执行 |
| `InlineBlocking` | §8.10：直接执行但块体明显耗时 | 改 `Coroutine.spawnFunc0` |

### 13.2 运行期错误

| 错误 | 触发 |
|------|------|
| `ForeignException` | 块内抛出（携带宿主语言栈；隔离岛形态下经端口回传后在 `await` 处重抛） |
| `DeviceError` | 设备执行失败（含 launch 失败、越界） |
| `MarshalError` | 值转换失败（统称，见下面细分） |
| **`MarshalNoConverter`** | §11.13：找不到（源域, 目标域, 类型）的转换器 |
| **`MarshalTypeError`** | §11.13：类型与目标槽不匹配 |
| **`MarshalPinFailed`** | §11.13：pin 失败（GC 状态异常） |
| **`StaleWriteBack`** | ★ §11.9：异步写回时发现目标槽 epoch 已变，且 `onConflict = "error"` |
| **`TensorOnDevice`** | §11.10：Tensor 已 H2D，主机侧仍访问 |
| **`MarshalCrossThread`** | §11.11：外部线程直接访问 SL 对象 |
| `LabelTimeout` | 超 `timeout` 参数 |
| `BlockCancelled` | Task 被取消 |
| **`IsolateCrashed`** | worker 岛崩溃（主程序存活；按 `restartOnCrash` 重建岛后可重试） |
| **`PluginCrashed`** | handler dll 崩溃（直接执行/协程时为进程级；隔离岛形态下降级为 `IsolateCrashed`） |
| **`PluginTimeout`** | `sl_label_exec` 超 `timeoutMs` |

### 13.3 与 attribute 的名字冲突

`atSignLabel` 的 `name` **不得与已注册 attribute 同名**（如不能注册名为 `GPU` 的标签，因为 `@GPU` 已存在）。检测：
- 标签注册表 vs `AttributeManager.RegisterBuiltInHandlers` 的内置表 + 用户 `extends Attribute` 的类。
- 冲突时编译期报 `AtSignLabelNameConflict`。
- 大小写**：建议**大小写敏感区分（`@GPU(...)` attribute vs `@gpu{}` 标签可共存），但默认禁止，需显式 `"allowCaseVariant": true`。

---

## 14. 示例集

### 14.1 `@csharp`（你的原始需求 · 原样保留）

```sl
test()
{
    a = 20
    count = 100
    str = "aaa"

    @csharp()
    {
        int  a <- $a;                              # 传入：SL 的 a    → C# 的 a
        string bstr <- $str;                       # 传入：SL 的 str  → C# 的 bstr
        $count <- testClass.addFunc( a, 20 );      # 传出：C# 的结果  → SL 的 count
    }

    print( count )        # 已被更新
}
```

**逐个符号看清楚**：

| 符号 | 是什么 |
|------|--------|
| `$a` / `$str` / `$count` | **SL 变量**（带 `$`） |
| `int` / `string` / `a` / `bstr` / `testClass` / `addFunc` | **C# 自己的**（无 `$`） |
| `<-` | **数据通道**（箭头指向目的地） |

**等价的 `->` 写法**（箭头指向目的地，两种任选）：

```sl
@csharp()
{
    $a -> int a;
    $str -> string bstr;
    testClass.addFunc( a, 20 ) -> $count;
}
```

### 14.2 `@csharp` 其它写法

```sl
Int32 a = 0
Int32 r = 0

# 传出到已有变量
@csharp { $r <- TTC.getIndex( 10, $a ); }
print( r )

# 用宿主语言 return 取表达式值（不走通道）
Int32 r2 = @csharp { return TTC.getIndex( 10, a ); }

# 带具名参数（铁律 2）
Int32 r3 = @csharp( version = "net8", timeout = 500 ) { return TTC.getIndex( 10, a ); }

# 先传入到局部变量再算（最清楚）
@csharp
{
    int x <- $a;
    int y <- 10;
    $r <- TTC.getIndex( y, x );
}
```

### 14.3 `@js`

```sl
Int32 sum = 0
@js { $sum <- Test.calc( a, a ); }

# 或 return 形式
Int32 s2 = @js( version = "es2020" ) { return Test.calc( a, b ); }
```

### 14.4 `@gpu`（★ 参数在 `()`，数据走 `$` 通道）

```sl
# ── 最简：不写参数（全部 auto），数据用 $ ──────────────────────────────
@gpu { $C[i,j] <- A[i,j] + B[i,j] }

# ── 只指定 tile ────────────────────────────────────────────────────────
@gpu( tile = [16,16] ) { $C[i,j] <- A[i,j] + B[i,j] }

# ── 完整调度参数 + 先传入标量 ──────────────────────────────────────────
@gpu( tile = [32,32], tileNum = 64, block = 256, shared = 4096, device = Device.gpu(0) )
{
    int i = gid(0);
    int j = gid(1);
    float acc = 0.0f;
    for ( int k = 0; k < K; k++ ) { acc += A[i,k] * B[k,j]; }
    $C[i,j] <- acc;                    # 传出：写回 SL 的 C（自动 D2H）
}
```

**判定**：`$C` 在 `<-` 左边 → **传出**（Tensor 自动 D2H）；`A`/`B`/`K`/`i`/`j`/`acc`/`gid` 都**不带 `$`** → CUDA 自己的符号，SL 不碰。

若要传 SL 的张量进去：

```sl
@gpu( tile = [16,16] )
{
    float a <- $A[i,j];                # 传入：SL 的 A 的元素（自动 H2D）
    float b <- $B[i,j];                # 传入
    $C[i,j] <- a + b;                  # 传出
}
```

```sl
# ── ⛔ 非法：位置参数（铁律 2）────────────────────────────────────────
@gpu( [16,16] ) { ... }                    # LabelParamMustBeNamed
@gpu( tile = [16,16], 256 ) { ... }        # 混合也非法
```

### 14.5 `@asm`（现在也用 `$`，统一语法）

```sl
Int32 a = 3
Int32 b = 4
Int32 r = 0

@asm( isa = "x86-64" )
{
    mov   eax, [$a]        # 传入：SL 的 a → eax
    add   eax, [$b]        # 传入：SL 的 b
    $r <- eax              # 传出：eax → SL 的 r
}
print( r )                 # 7

# 指定 SIMD
@asm( isa = "x86-64", simd = "avx2" )
{
    vmovdqu ymm0, [$a]
    vpaddd  ymm0, ymm0, [$b]
    $r <- ymm0
}

# 旧写法（in/out 参数）仍兼容，但推荐上面这种
@asm( in = (a, b), out = (r) ) { mov eax, [a]; add eax, [b]; $r <- eax }   # 旧写法，兼容保留
```

### 14.6 `@hlsl`

```sl
@hlsl( entry = "CSMain", target = "cs_6_0", thread = [64,1,1] )
{
    [numthreads(64,1,1)]
    void CSMain( uint3 id : SV_DispatchThreadID )
    {
        $outBuf[id.x] <- buf[id.x] * 2.0f;      # $outBuf = SL 变量，通道传出
    }
}
```

### 14.7 kernel 特化体（设备降级链）

```sl
kernel matmul( Tensor<Float32,2> A, Tensor<Float32,2> B )
{
    ret A · B                                    # SL 主实现（永远兜底）
}
@gpu            { $C[i,j] <- A[i,j] * B[i,j]; }  # GpuHal 自动调度
@gpu.amd.tensor { $C[i,j] <- A[i,j] * B[i,j]; }  # AMD matrix core 特化（3 层标签名）
@cuda           { __global__ void m(...) { $C[i,j] <- ...; } }
@npu.ascend     { $C[i,j] <- A[i,j] * B[i,j]; }
@csharp         { $C <- BLAS.MatMul( A, B ); }
@asm            { $C <- eax }
```

调用端（沿用 `TENSOR_HETEROGENEOUS_DESIGN.md:558-560`）：

```sl
Task t = launch( matmul, A, B, device = Device.best() )
Tensor C = await t
```

> 运行时按 `atSignLabelFallbackOrder` 挑第一个平台可用的特化体；全不可用 → 用 SL 主体。
> 每个特化体的**数据都用 `$` 通道**，调度参数写在各自的 `()` 里。
> 注意 `@gpu.amd.tensor` 是**三层标签名**（§4.2），命中优先级高于 `@gpu`。

### 14.8 ★ 三种执行形态对照（同一个 C# 调用）

```sl
# ── ① 直接执行（默认，同步）─────────────────────────────────────────
@csharp { $r <- TTC.getIndex( 10, $a ); }         # 通道写回，同步返回
print( r )                                        # 立即可见

Int32 v = @csharp { return TTC.getIndex( 10, a ); }   # return 取表达式值

# ── ② 协程（不阻塞线程，可取消）─────────────────────────────────────
Task t = Coroutine.spawnFunc0( @csharp { return TTC.getIndex( 10, $a ); } )
prepareNextBatch()                # CPU 同时干活
var v2 = await t                  # 同步点（挂起协程，不占线程）

# 协程里的通道写回：块结束时写回，await 后可见
Task t2 = Coroutine.spawnFunc0( @csharp { $r <- TTC.getIndex( 10, $a ); } )
print( r )                        # ⚠ 可能还是旧值（协程未结束）
await t2
print( r )                        # ✅ 已写回

# ── ③ 隔离岛（崩溃隔离；通道在 await 点才写回）──────────────────────
Isolate iso = Isolate.spawnFunc0( @csharp { $count <- Heavy.Calc( $a ); } )
print( count )                    # ⚠ 旧值（还没回传）
await iso
print( count )                    # ✅ 新值（回传后写回）
```

**语义差异一览**（唯一需要留意的地方）：

| | 直接执行 | `Coroutine.spawnFunc0` | `Isolate.spawnFunc0` |
|---|---|---|---|
| `$目标 <- 源`（通道写出） | ✅ 立即 | ✅ 块结束时 | ✅ **`await` 点时** |
| `return` 返回 | ✅ | ✅ | ✅ |
| 传入 `$源` 的开销 | pin 即可 | pin（共享） | **必须拷贝 / 转移** |
| 不阻塞线程 | ❌ | ✅ | ✅ |
| 可取消 | ❌ | ✅ `t.cancel()` | ✅ |
| 崩溃隔离 | ❌ | ❌ | ✅ |
| `$` 类型需可发送 | ❌ | ❌ | ✅ 必须 |

### 14.9 多任务组合（复用既有 Coroutine API）

```sl
Int32 r1 = 0
Int32 r2 = 0
Int32 r3 = 0

Task t1 = Coroutine.spawnFunc0( @csharp { $r1 <- Cs.calc( $a ); } )
Task t2 = Coroutine.spawnFunc0( @js     { $r2 <- Js.calc( $b ); } )
Task t3 = Coroutine.spawnFunc0( @python { $r3 <- Py.calc( $c ); } )

Coroutine.waitAll( t1, t2, t3 )                    # 现有 API，直接可用
print( r1 + r2 + r3 )                              # 三个都已写回

var fastest = Coroutine.waitAny( t1, t2, t3 )      # 谁先完成用谁
```

### 14.10 不可信运行时的崩溃隔离

```sl
static Int32 safeCall( Int32 x )
{
    Int32 r = 0
    try {
        Isolate iso = Isolate.spawnFunc0( @csharp { $r <- Unsafe.ThirdParty.Calc( $x ); } )
        await iso
        ret r
    }
    catch ( IsolateCrashed e ) {
        # 岛已按 restartOnCrash 重建，用可信实现重试
        Isolate iso2 = Isolate.spawnFunc0( @csharp { $r <- Safe.Fallback.Calc( $x ); } )
        await iso2
        ret r
    }
}
```

### 14.11 设备块：同步默认 + overlap

```sl
# 默认同步：写完这一行，C 一定已就绪
@gpu( tile = [16,16] ) { $C[i,j] <- A[i,j] + B[i,j] }
print( C )

# 要 overlap：自己 spawnFunc
Task t = Coroutine.spawnFunc0( @gpu( tile = [16,16] ) { $C[i,j] <- A[i,j] + B[i,j] } )
prepareNextBatch()
await t
print( C )
```

### 14.12 与现有机制组合

```sl
# 协程内使用设备块
Coroutine.spawnClosure0( function() {
    @gpu( tile = [16,16] ) { $C[i,j] <- A[i,j] + B[i,j] }
    print( "done" )
})

# 与 Channel 组合：外部调用结果送进 CSP 通道
Int32 tmp = 0
Channel<Int32> ch = Channel<Int32>( 16 )
Coroutine.spawnFunc0( @csharp { $tmp <- TTC.getIndex( 10, $a ); ch.send( tmp ); } )
Int32 v = ch.recv()

# 与 Stream 组合：设备产出 → 元素流
Stream<Float32> s = Stream<Float32>.fromByteStream( deviceResult, codec )

# 与 Isolate 端口组合：把块结果发给别的岛
Int32 w = 0
Isolate worker = Isolate.spawnFunc0( @csharp { $w <- Heavy.Calc( $a ); } )
await worker
port.send( w )
```

### 14.13 ★ 参数具名 + 标签名层级：合法 / 非法全对照

```sl
# ✅ 合法
@gpu() { ... }                                              # 空列表
@gpu { ... }                                                # 省略 ()
@gpu( tile = [16,16] ) { ... }                              # 单个具名
@gpu( tile = [16,16], block = 256 ) { ... }                 # 多个具名
@gpu( block = 256, tile = [16,16] ) { ... }                 # 顺序无关
@gpu( tile = [16,16], device = Device.gpu(0) ) { ... }      # 值可以是表达式
@asm( isa = "x86-64", in = (a,b), out = (r) ) { ... }       # in/out 也是具名参数（兼容旧写法）
@csharp( version = "net8", timeout = 500 ) { ... }

# ✅ 合法：多层标签名（§4.2）
@gpu.amd { $C[i,j] <- ... }
@gpu.amd.tensor( tile = [16,16] ) { $C[i,j] <- ... }
@npu.ascend.cann.v2 { $C[i,j] <- ... }

# ⛔ 非法 —— LabelParamMustBeNamed
@gpu( [16,16] ) { ... }
@gpu( [16,16], 256 ) { ... }
@gpu( tile = [16,16], 256 ) { ... }
@csharp( "net8" ) { ... }
@asm( "x86-64", (a,b), (r) ) { ... }

# ⛔ 非法 —— UnknownLabelParam（拼错 / 未声明）
@gpu( til = [16,16] ) { ... }          # help: 已声明 tile/tileNum/block/grid/shared/device
@csharp( verison = "net8" ) { ... }
```

### 14.14 ★ 插件化：新增一个标签所需的一切（无需改编译器）

以新增 `@zig` 为例，总共就两件事：

**① jsonc 加一段配置**

```jsonc
{ "name": "zig", "extends": "foreign", "lang": "zig",
  "parser": { "kind": "native", "stage": "compile",
              "dll": "SlLabelZig.dll", "entry": "sl_label_zig_parse" },
  "exec":   { "kind": "dll", "dll": "SlLabelZig.dll", "entry": "sl_label_exec" },
  "platform": { "os": ["windows","linux"], "cpu": ["x64","arm64"] },
  "fallback": "sl" }
```

**② 提供一个 dll，导出 5 个约定符号**

```c
int  sl_label_abi_version( void )                  { ret SL_LABEL_ABI_VERSION }
int  sl_label_init( SLLabelHost* h, const char* cfg ) { g_host = h; ret 0 }
int  sl_label_parse( const char* src, const char* opt, SLLabelParseResult** out ) {
        /* 扫出 $name 与 <- / -> ，填 channels / returnsValue / diagnostics */
}
int  sl_label_exec( SLLabelExecCtx* ctx ) {
        /* 调 zig 编译器或解释器跑 ctx->source，结果写 ctx->retv */
}
void sl_label_shutdown( void ) { }
```

**SL 编译器与 VM 的代码改动量：0 行。**

### 14.15 ★ 声明式规则插件（形态 A，无 dll）

**因为数据交互全靠 `$` + `<-`/`->`（§6.2），Parser 不再需要懂宿主语言**——新标签连 dll 都不用写，一份通用规则即可：

```jsonc
// 通用规则（可被任意语言复用；下面以 zig 为例，只多了 returnKeyword）
{
  "channelVar":   "\\$([A-Za-z_]\\w*)",                 // 找 $name
  "channelIn":    "([^;\\n]+?)\\s*<-\\s*\\$(\\w+)",     // 目标 <- $源    → In
  "channelInR":   "\\$(\\w+)\\s*->\\s*([^;\\n]+)",      // $源 -> 目标    → In
  "channelOut":   "\\$(\\w+)\\s*<-\\s*([^;\\n]+)",      // $目标 <- 源    → Out
  "channelOutR":  "([^;\\n]+?)\\s*->\\s*\\$(\\w+)",     // 源 -> $目标    → Out
  "returnKeyword": "return"                              // zig 用 return
}
```

SL 编译器内置的**规则引擎**（`LabelPluginLoader` 的 `rule` 分支）按它提取 `channels` → 查 SL 符号表 → 生成通道代码。执行侧仍走 `exec.dll`。

> 对比 v2 需要 `identifier` / `skipRegions` / `reserved` 等一堆**语言相关**配置，现在**一套正则通吃所有语言**——
> `@csharp` `@cuda` `@hlsl` `@asm` `@sql` 可以共用同一份 `channel.rule.json`，只需各自的 `exec.dll`。

---

## 15. 与现有机制的关系

### 15.1 与 attribute

| | attribute `@Name(...)` | 标签块 `@name{...}` |
|---|---|---|
| 形态 | 圆括号 + 位置/具名参数 | 花括号 + 异质代码 |
| 位置 | 类/成员**之前**（声明修饰） | **语句/表达式位置**（可执行） |
| 内容 | SL 表达式 | 外部语言原文 |
| 消费方 | `AttributeManager`（C# 编译期 handler） | 标签 handler（codegen / 桥接） |
| 兼容 | ✅ 完全不变 | 新增 |

`@GPU(...)` 与 `@gpu{}` 可长期共存；新代码建议用 `@gpu{}`（具名参数 + 内联体，更可读）。

### 15.2 与 FFI

- 编译期 AOT 产物 → 绑定为 `OpCode_CallFFIStatic`（118）→ **零 SL 帧、零 LoadLibrary/GetSymbol**，性能等同现有 `@DllStaticImport`。
- 运行期桥接不走 FFI（直接进 Mono/QuickJS），避免 FFI 的 ≤6 参限制与 trampoline 形状限制。
- 回调 SL 复用 FFI 的 trampoline 设施（建议后续放宽其 `int64(int64,int64)` 固定形状，见 `ffi.md:161`）。

### 15.3 与 Mono / QuickJS / JNI 三份集成文档

三份文档定义**各自运行时如何打通**；本文档定义**它们与 `@tag{}` 之间的统一契约**：

| 契约点 | 内容 |
|--------|------|
| 入参 | In 通道（`目标 <- $源`）→ 各 bridge 的 marshal（三文档已定义类型表，直接复用） |
| 出参 | Out 通道（`$目标 <- 源`）+ `returnKeyword` 的返回值 → 反向 marshal |
| 对象存活 | 各 bridge 自有的 GCHandle / DupValue / GlobalRef；本文档只要求「块期间 pin，退出释放」 |
| 线程 | 沿用各文档的线程模型（Mono 需 `mono_thread_attach`；QuickJS 单线程；JNI `AttachCurrentThread`） |
| 异步 | 本文档统一为 `Task` + `await`；各 bridge 只需提供「提交 + 完成通知」 |

### 15.4 与 `TENSOR_HETEROGENEOUS_DESIGN.md`

- 本文档**接管**其 §10.2 的 `@gpu/@cuda/@csharp/@asm` 提案，泛化为 `atSignLabel`，并补齐其**缺失的**参数/捕获/返回/异步四要素。
- 其 `deviceTags` / `devicePriority` / `partition` 配置 → 统一到 `atSignLabel` + `atSignLabelFallbackOrder`（建议该文档后续改为引用本文）。
- 其硬约束（编译期指令函数化、不用 `@` 做矩阵乘、tile/block 具名 + autotune）**全部遵守**。

### 15.5 与 Stream / Isolate

- 设备块的大块数据搬运复用 `TransferableData`（`STREAM_DESIGN.md` §12 同构）。
- 异步复用 `Task` / `Coroutine` / `await`，**零新并发原语**。
- 外部运行时建议放独立 isolate（崩溃隔离），与 `TENSOR` 文档 HAL 的「worker isolate」思路一致。

### 15.6 ★ 与 `dllImports` / 插件 dll 体系

`dllImports`（`md/project/ffi.md`）是**通用**的 dll 导入表；本设计的 handler dll 是**专用**的一类插件 dll。二者关系：

| | `dllImports`（通用） | handler dll（标签插件） |
|---|---|---|
| 用途 | 任意 native 函数绑定 | 只处理 `@tag{}` 的解析与执行 |
| 声明位置 | `dllImports[]` | `atSignLabel[].parser/exec.dll` |
| 加载方式 | `vm_sys_ffi_load_library` | `vm_sys_atsign_load`（额外校验 ABI 版本与必需导出） |
| 调用约定 | 变参 FFI，≤6 参，x64 only | **固定 ABI**（`SLLabelExecCtx*` 单参数），无参数数量限制 |
| 反向调用 | `SystemFFICreateCallback`（trampoline 形状固定 `int64(int64,int64)`，8 槽） | `SLLabelHost` **函数表**（无形状限制、无槽位限制） |
| 崩溃隔离 | 无（进程内） | 可用 `Isolate.spawnFunc0` 隔离 |

> **建议**：复用 `dllImports` 的**加载/去重/引用计数**基础设施（`sl_ffi_lib_manager.c`），但**不走** FFI 的变参调用路径——handler dll 用固定 ABI 的 `SLLabelExecCtx*`，绕开 ≤6 参限制与 x64-only 约束。

### 15.7 ★ 与「语言解析体系」的边界（你的核心诉求）

| 环节 | 归属 | 理由 |
|------|------|------|
| 词法：圈出 raw 块 | **SL** | 不圈边界就无法继续 |
| 语法：挂 AtSignBlock 节点 | **SL** | 要接入 SL 的语句/表达式结构 |
| **解析块体内容** | **插件** | SL 不该懂 C#/JS/CUDA |
| 查 SL 符号表做捕获 | **SL** | 只有 SL 知道作用域里有什么 |
| 生成入参/出参 marshal | **SL** | 只有 SL 懂自己的栈与 GC |
| 生成调度（coroutine/isolate） | **SL** | 复用既有并发原语 |
| **执行块体** | **插件（dll）** | 外部语言由外部运行时负责 |
| 诊断行号映射 | **SL**（用块起始行 + 插件相对行） | 需要 SL 源文件坐标 |

**一句话**：SL 负责「**边界 + 进出 + 调度**」，插件负责「**里面是什么、怎么跑**」。

---

## 16. 分期路线

| 期 | 目标 | 交付 | 依赖 |
|----|------|------|------|
| **P0 骨架** | 语法 + 配置 + **插件框架**打通，无真实运行时 | `LexerParseToToken.ReadAt` 的 `@{` 分支 + raw 捕获；`FileMetaAtSignBlockSyntax`；`ProjectConfig.cs` + `ProjectJsoncLoader.cs` 的 `atSignLabel`；`LabelRegistry`（别名 + 家族继承）；**`LabelPluginLoader` 先只做 `rule` 形态**；`csimple_lang/src/vm/label/` 骨架 + **回声 handler dll**（`sl_label_exec` 只回显入参） | 无 |
| **P1 首个真标签** | `@csharp` 端到端（Mono） | 接通 `MONO_INTEGRATION_DESIGN.md` 的空桩；`Core/NativeBridge.sl` 的 `SystemCallCLRMethod` 落地；`exec.kind=bridge` 路径；通道表 → marshal → invoke → 写回（**同步形态**） | P0 + Mono 集成阶段 1~2 |
| **P1.1** ★ | **Handler DLL 路径** | `vm_sys_atsign_load/exec` + `SLLabelHost` 服务表 + `SLLabelValue` marshal；写一个**示例 dll**（任意语言）验证 ABI；`PluginAbiMismatch` / `PluginMissingSymbol` 检查 | P0 |
| **P1.2** ★ | **协程 / 隔离岛驱动** | `Coroutine.spawnFunc0..3` / `Isolate.spawnFunc0..3` 重载（底层复用现有 spawn syscall）；隔离岛约束 I1~I4 检查；`IsolateNoImplicitWriteBack` / `CaptureNotSendable` | P1 |
| **P1.5** | `@js`（QuickJS） | 同上，走 `js_bridge`；验证「同一套机制支持第二种语言」 | P1 |
| **P2 设备** | `@gpu` / `@cuda` | POD 校验；`tile/block/grid` **具名**参数 + `auto` 推导；Tensor 自动 H2D/D2H；HAL 对接 | P1 + TENSOR 路线第 1~3 步 |
| **P2.5** | 编译期 AOT 路径 | `@cuda`→nvcc→ptx、`@csharp`→Roslyn→dll；绑定 `OpCode_CallFFIStatic` | P2 |
| **P3** | `@asm` / `@hlsl` / `@java` / `@py` | 各 kind 的 handler；`@asm` 的 `in/out` 寄存器约定；shader 资源绑定 | P2 |
| **P4** | 体验 | autotune（`auto` 升级为实测选优）；IDE 高亮（`lang` 字段）；块内报错行号映射；补 `md/syntax/atsignlabel.md` 并登记 `md/INDEX.md`（R13） | P3 |

---

## 17. 测试用例

| 组 | 覆盖 | 断言要点 |
|----|------|---------|
| **A** | 词法 raw 捕获 | 花括号嵌套；字符串内含 `{`；注释内含 `{`；C# `@"..."` 逐字串；不配平 → 报错 |
| **B** | 配置 | `atSignLabel` 解析；未注册标签报错；平台不匹配按 `fallback` 处理 |
| **C** | 参数 | 具名参数；默认值；未声明参数报错；类型错报错 |
| **D** | ★ 通道（foreign） | `int a <- $a;` 传入；`$r <- f();` 传出；`$this.x` / `$global.cfg` 解析；`$x` 不存在报 `ChannelVarNotFound`；无箭头报 `ChannelMissingOperator`；两端都 `$` 报 `InvalidChannel`；pin/unpin 正确性（GC 压力下不崩） |
| **E** | 返回 | 通道写出；`return` 返回（表达式位置）；多通道；缺 return 报错 |
| **F** | `@csharp` 端到端 | 标量/字符串/数组往返；异常映射；`host` 服务调用 SL |
| **G** | `@js` 端到端 | 同上，验证机制可移植 |
| **H** | `@gpu` 参数 | `tile/block/grid` 生效；`auto` 推导；参数拼写错报错 |
| **I** | `@gpu` 捕获 | `in/out` 显式；捕获非 POD 报错；Tensor 自动 H2D/D2H；结果正确 |
| **J** | `wait` | `await` 挂起不阻塞线程（flag 验证）；root 退化为阻塞；超时；取消 |
| **K** | 降级 | 运行时缺失 → 回落 SL 主体；`fallback=error` 报错；多标签按优先级选 |
| **L** | 与现有机制 | 协程内使用；isolate 内使用；与 FFI 静态绑定产物混用 |
| **M** | ★ 协程驱动 | `Coroutine.spawnFunc0( @tag(){} )` 返回 Task；同岛共享 → **赋值仍可写回**；`await` 不阻塞其它协程（flag 验证）；`t.cancel()` 后 unpin |
| **N** | ★ 隔离岛驱动 | `Isolate.spawnFunc0( @tag(){} )`；不可发送类型报 `CaptureNotSendable`；**赋值报 `IsolateNoImplicitWriteBack`**；`SL.call` 报 `IsolateNoCallback`；worker 岛**复用** |
| **O** | ★ 崩溃隔离 | 岛内强制崩溃 → 主程序存活并抛 `IsolateCrashed`；`restartOnCrash` 后重试成功 |
| **P** | ★ 具名参数 | `@gpu( tile=[16,16] )` 通过；`@gpu( [16,16] )` 报 `LabelParamMustBeNamed`；`@gpu( til=[16,16] )` 报 `UnknownLabelParam`；混合写法也报错 |
| **Q** | 成本 lint | 小数据 `Isolate.spawnFunc0` → `IsolateOverkill`；大对象未用 Transferable → `IsolateDeepCopy`；直接执行耗时块 → `InlineBlocking` |
| **R** | ★ Parser 插件 | `rule` 形态用正则提取 `channels` 正确（`$name`、`<-`/`->` 两侧、`ptr->field` 不误判）；`script` 形态；`native` 形态经 dll；诊断行号映射回 SL 源文件正确 |
| **S** | ★ Handler DLL | dll 加载/卸载；ABI 版本不匹配报错；缺导出符号报错；`sl_label_exec` 正常执行；`host->pin/unpin` 后 GC 压力下不崩；`host->callSL` 回调成功 |
| **T** | ★ 三处耦合隔离 | 换一个 dll 实现同一标签 → SL 侧代码零改动；`parser.stage=runtime` 时强制 `explicit`；`trusted=false` + `inline` → 报 `UntrustedInline` |

> 落位建议：`test/SpecialTest/` 下新增 `AtSignLabelTest/`（宿主 `CSimpleVMSpecialTest`），与既有 `CSharpCall.sl`、`CallOtherLanguage.sl` 草稿合并整理。

---

## 18. 标签注册表（完整清单）

> 本节把你列出的标签（`csharp / java / javascript / asm / cpu / npu / asm(51,stm32,armv7,armv9,asm-x86,loong,mips) / hlsl / mlir / llvm / cuda / amd(tensor) / clang / cpluslang / python / dart`）
> **按性质重新归类**——因为你列的里面混了三类不同东西：
>
> | 你写的 | 实际性质 | 归类 |
> |--------|---------|------|
> | `csharp` `java` `javascript` `python` `dart` `cpluslang` | **语言** | `foreign` |
> | `clang` | **编译器**（不是语言；指 C/C++ 走 clang 编译） | `foreign` + `compiler: clang` |
> | `mlir` `llvm` | **IR / 编译器后端** | `ir` |
> | `hlsl` | **着色器语言** | `shader` |
> | `cuda` `amd(tensor)` `npu` | **硬件设备** | `device` |
> | `cpu` | **硬件设备**（CpuHal，TENSOR 两区模型里的 CPU 区） | `device` |
> | `asm(...)` | **指令集**（一整个家族，不是单个标签） | `asm` 家族（20+ ISA） |
>
> 归类后新增 **§18.3 家族继承机制**——否则 60+ 个标签要重复写 60 遍 `lexHints`。

### 18.1 命名规范

```
@<domain>[.<target>[.<variant>]]
```

| 规则 | 说明 |
|------|------|
| 层级用 `.` | 对齐 SL 命名空间习惯（`Core.IIterable`）。如 `@asm.x86`、`@gpu.nvidia`、`@npu.ascend` |
| **层数不限**（★ 本次确定） | 1~N 层都合法：`@gpu` / `@gpu.amd` / `@gpu.amd.tensor` / `@npu.ascend.cann.v2` |
| **最长匹配优先** | 同时注册 `gpu.amd` 与 `gpu.amd.tensor` 时，写 `@gpu.amd.tensor{}` 命中更具体的 |
| 逐层继承 | `gpu.amd.tensor` 可 `extends` 到 `gpu.amd`，再 `extends` 到 `device` 家族（§18.3） |
| 家族名可独立使用 | `@asm` 合法，但**必须**带 `isa` 参数：`@asm( isa = "x86-64" ) { }` |
| 别名 | 见 §18.5，`@cs` ≡ `@csharp`，`@cuda` ≡ `@gpu.nvidia` |
| 词法改动 | `LexerParseToToken.ReadAt()` 的标识符扫描需**额外允许 `.`**（当前 `IsIdentifier2` 不含点号） |

### 18.2 完整标签清单

#### A. 通用语言 `foreign`

| 标签 | 别名 | 运行时 / 编译器 | 优先级 | 备注 |
|------|------|----------------|--------|------|
| `csharp` | `cs` `c#` | Mono（`mono-2.0-sgen.dll` 已 vendored） | **P1** | 你的原始需求 |
| `javascript` | `js` | QuickJS（源码直编，无 dll 依赖） | **P1.5** | |
| `c` | | clang / gcc / msvc → native | **P1** | |
| `cpluslang` | `cpp` `c++` | clang / gcc → native | **P2** | 你点名 |
| `clang` | | 同上（显式指定 clang 编译器） | **P2** | 你点名；= `c`/`cpp` 的 `compiler: clang` |
| `java` | | HotSpot JNI（运行时发现 `libjvm`） | **P2** | |
| `kotlin` | `kt` | JVM（`kotlinc` → jar） | P3 | |
| `scala` | | JVM | P3 | |
| `groovy` | | JVM | P3 | |
| `python` | `py` | CPython 嵌入 | **P2** | 你点名 |
| `dart` | | Dart VM / AOT | **P2** | 你点名（SL 的 Stream 设计即参考 Dart） |
| `lua` | | LuaJIT（极轻，游戏脚本首选） | P2 | |
| `typescript` | `ts` | `tsc` 前置转译 → QuickJS | P3 | |
| `rust` | `rs` | `rustc` → cdylib | P3 | |
| `go` | `golang` | `go build -buildmode=c-shared` | P3 | |
| `zig` | | `zig build-lib` | P3 | |
| `objectivec` | `objc` | clang（Apple） | P3 | |
| `swift` | | Swift 运行时（Apple） | P3 | |
| `ruby` | `rb` | mruby / CRuby | P4 | |
| `php` | | | P4 | |
| `perl` | | | P4 | |
| `shell` | `bash` `sh` | 系统 shell | P3 | 运维脚本 |
| `powershell` | `pwsh` | | P4 | Windows |
| `julia` | | | P4 | 科学计算 |
| `octave` | `matlab` | | P4 | 科学计算 |
| `fortran` | | gfortran | P4 | 遗留科学计算 |
| `r` | | | P4 | 统计 |

#### B. 指令集 `asm`（只能同步就地执行）

> 你列的 `51 / stm32 / armv7 / armv9 / asm-x86 / loong / mips` 全部在此，另补齐主流 MCU / 服务器 / 专用 ISA。

| 标签 | 别名 | 目标 | 优先级 | 备注 |
|------|------|------|--------|------|
| **`asm.x86`** | `x86` `asm-x86` `x64` | x86 / x86-64 | **P3** | 你点名；含 SSE/AVX（用 `simd` 参数选择） |
| `asm.x86.avx512` | `avx512` | AVX-512 | P4 | |
| **`asm.armv9`** | `armv9` `arm64` `aarch64` `sve2` | ARMv9-A / ARMv8-A | **P3** | 你点名；含 NEON / SVE / SVE2 |
| **`asm.armv7`** | `armv7` `arm32` | ARMv7-A/R | **P3** | 你点名 |
| `asm.thumb` | `thumb` `thumb2` | Thumb-2 | P3 | |
| **`asm.cortexm`** | `cortexm` `stm32` | Cortex-M（M0~M55） | **P3** | 你点名 stm32；`chip` 参数选型号 |
| **`asm.mcs51`** | `c51` `8051` `51` | Intel 8051 / MCS-51 | **P3** | 你点名 |
| **`asm.loongarch`** | `loong` `loongarch64` | 龙芯 LoongArch | **P3** | 你点名；含 LSX/LASX 向量 |
| **`asm.mips`** | `mips` `mips32` `mips64` | MIPS | **P3** | 你点名；龙芯旧版也是 MIPS |
| `asm.riscv` | `rv32` `rv64` `riscv` | RISC-V（含 RVV） | **P3** | 当前热点 |
| `asm.xtensa` | `esp32` | Xtensa（ESP32） | P3 | |
| `asm.avr` | | AVR（Arduino） | P4 | |
| `asm.pic` | | Microchip PIC | P4 | |
| `asm.msp430` | | TI MSP430 | P4 | |
| `asm.rl78` | | 瑞萨 RL78 | P4 | |
| `asm.rx` | | 瑞萨 RX | P4 | |
| `asm.hc08` | `s08` | NXP HCS08 | P4 | |
| `asm.tricore` | | 英飞凌 TriCore（汽车 MCU） | P4 | |
| `asm.ppc` | `powerpc` | PowerPC | P4 | |
| `asm.sparc` | | SPARC | P4 | |
| `asm.sw64` | | 申威 | P4 | |
| `asm.m68k` | | Motorola 68000 | P5 | 复古 |
| `asm.z80` | | Zilog Z80 | P5 | 复古 |
| `asm.6502` | | MOS 6502 | P5 | 复古 |
| `asm.wasm` | `wasm` | WebAssembly（文本/字节码） | P3 | 也可归 `ir` |
| `asm.bpf` | `ebpf` | eBPF（内核 / 网络） | P4 | |
| `asm.s390x` | | IBM Z | P5 | |

#### C. 计算设备 `device`

**C1. CPU 族（CpuHal）** —— 你列的 `cpu`

| 标签 | 别名 | 说明 | 优先级 |
|------|------|------|--------|
| **`cpu`** | `cpu.scalar` | 标量 CPU（CpuHal 默认路径，也是 SL 主实现的落点） | **P2** |
| `cpu.vm` | `vm` | **VmHal** —— CVM 解释器兜底（永远可用，见 `TENSOR` 文档 §9.1） | **P2** |
| `simd` | | 通用 SIMD（由 `isa` 参数选具体扩展） | P3 |
| `simd.neon` | `neon` | ARM NEON（128-bit） | P3 |
| `simd.sve` | `sve` `sve2` | ARM SVE / SVE2（可变长） | P3 |
| `simd.sse` | `sse` `sse4` | x86 SSE / SSE2 / SSE4.2 | P3 |
| `simd.avx` | `avx` `avx2` | x86 AVX / AVX2（256-bit） | P3 |
| `simd.avx512` | `avx512` | x86 AVX-512 | P4 |
| `simd.rvv` | `rvv` | RISC-V Vector | P3 |
| `simd.lsx` | `lsx` `lasx` | 龙芯 LSX / LASX | P4 |
| `simd.altivec` | `altivec` `vmx` | PowerPC AltiVec / VSX | P4 |
| `simd.mmi` | | 龙芯旧版 MMI | P5 |

**C2. GPU 族（GpuHal）** —— 你列的 `cuda` / `amd(tensor)`

| 标签 | 别名 | 运行时 / 语言 | 优先级 | 备注 |
|------|------|--------------|--------|------|
| **`gpu.nvidia`** | `cuda` `nvidia` `nv` | CUDA C（nvcc → PTX/cubin） | **P2** | 你点名；`precision` 参数控制 TensorCore（tf32/bf16/fp16/fp8） |
| **`gpu.amd`** | `amd` `hip` `rocm` | ROCm / HIP（hipcc → amdgcn） | **P2** | 你点名 `amd(tensor)`；CDNA 的 matrix core 同理 |
| `gpu.intel` | `sycl` `oneapi` `intel` | oneAPI / DPC++ / SYCL | P3 | Arc / Data Center GPU |
| `gpu.apple` | `metal-compute` | Metal Compute | P4 | |
| `gpu.opencl` | `opencl` | 通用 OpenCL C | P3 | 跨平台兜底 |
| `gpu.vulkan` | `vulkan` | Vulkan compute（GLSL / SPIR-V） | P4 | |
| `gpu.webgpu` | `webgpu` | WebGPU compute（WGSL） | P4 | |
| `gpu.arm` | `mali` | ARM Mali（OpenCL） | P4 | |
| `gpu.qualcomm` | `adreno` | 高通 Adreno（OpenCL） | P4 | |
| `gpu.imagination` | `powervr` | Imagination PowerVR | P5 | |

**C3. NPU / AI 加速器（NpuHal）** —— 你列的 `npu`

| 标签 | 别名 | 厂商 / SDK | 优先级 |
|------|------|-----------|--------|
| **`npu`** `npu.ascend` | `ascend` `cann` | 华为昇腾（CANN / AscendCL） | **P3** |
| `npu.cambricon` | `cambricon` `bang` | 寒武纪（BANG / CNRT） | P3 |
| `npu.horizon` | `horizon` `bpu` | 地平线（BPU / Horizon OpenExplorer） | P3 |
| `npu.rockchip` | `rknn` | 瑞芯微（RKNN Toolkit） | P3 |
| `npu.amlogic` | `amlogic` | 晶晨（AML NPU） | P4 |
| `npu.hisilicon` | `nnie` `hisi` | 海思（NNIE / SVP） | P4 |
| `npu.qualcomm` | `hexagon` `qnn` `snpe` | 高通 Hexagon（QNN / SNPE） | P4 |
| `npu.intel` | `movidius` `openvino` | Intel VPU / AI Boost（OpenVINO） | P4 |
| `npu.amd` | `xdna` `ryzen-ai` | AMD XDNA（Ryzen AI） | P4 |
| `npu.apple` | `ane` `coreml` | Apple Neural Engine（CoreML） | P4 |
| `npu.mediatek` | `apu` `neuropilot` | 联发科 APU（NeuroPilot） | P4 |
| `npu.sophon` | `sophon` `bitmain` | 算能（Sophon SDK） | P5 |
| `npu.google` | `edge-tpu` `coral` | Google Edge TPU（Coral） | P5 |
| `npu.hailo` | `hailo` | Hailo | P5 |

**C4. DSP 族**

| 标签 | 别名 | 说明 | 优先级 |
|------|------|------|--------|
| `dsp.ti` | `c6000` `c7000` | TI C6000 / C7000 | P4 |
| `dsp.cadence` | `xtensa-hifi` | Cadence HiFi | P4 |
| `dsp.ceva` | `ceva` | CEVA DSP | P4 |
| `dsp.adi` | `sharc` | ADI SHARC | P5 |
| `dsp.hexagon` | | 高通 Hexagon DSP（纯 DSP 模式） | P4 |

**C5. FPGA 族**

| 标签 | 别名 | 说明 | 优先级 |
|------|------|------|--------|
| `fpga.verilog` | `verilog` | Verilog HDL | P4 |
| `fpga.vhdl` | `vhdl` | VHDL | P4 |
| `fpga.sv` | `systemverilog` | SystemVerilog | P4 |
| `fpga.hls` | `hls` | C++ HLS（Vivado / Intel HLS） | P4 |

#### D. 着色器 `shader`

| 标签 | 别名 | 目标 | 优先级 | 备注 |
|------|------|------|--------|------|
| **`hlsl`** | | D3D11 / D3D12（DXC / FXC） | **P3** | 你点名；`entry`/`target`（`vs_6_0`/`ps_6_0`/`cs_6_0`）参数 |
| `glsl` | | OpenGL / OpenGL ES | P3 | |
| `glslvk` | `glsl-vulkan` | Vulkan GLSL（glslangValidator） | P4 | |
| `msl` | `metal` | Apple Metal Shading Language | P4 | |
| `wgsl` | | WebGPU Shading Language | P4 | |

#### E. IR / 编译器后端 `ir`（AOT 后走 `OpCode_CallFFIStatic`）

> 这一类**不是执行环境**，而是「我直接给你 IR / 汇编，你编译成 native 后直调」——所以走 codegen 路径 A（§10.9），同步执行。

| 标签 | 别名 | 说明 | 优先级 | 备注 |
|------|------|------|--------|------|
| **`llvm`** | `llvmir` | LLVM IR（`.ll` 文本 / bitcode） | **P2** | 你点名 |
| **`mlir`** | | MLIR（需 `dialect` 参数：`linalg`/`gpu`/`tosa`/`stablehlo`…） | **P2** | 你点名；与 `TENSOR` 文档 SLTIR 路线衔接 |
| `ptx` | | NVIDIA PTX | P3 | |
| `nvvm` | | LLVM NVPTX IR | P4 | |
| `spirv` | `spir-v` | SPIR-V（Vulkan / OpenCL） | P3 | 与 shader 共用 |
| `wasm` | `wasm32` `wasm64` | WebAssembly（可解释也可 AOT） | P3 | |
| `dxil` | | D3D12 二进制 | P4 | |
| `amdgcn` | `gcn` | AMD GCN / CDNA / RDNA ISA | P4 | |
| `air` | `metal-air` | Apple Metal AIR | P5 | |
| `tosa` | | TOSA（NPU 通用 IR） | P3 | NPU 后端主力 |
| `stablehlo` | `hlo` `mhlo` | StableHLO / XLA HLO | P3 | |
| `onnx` | | ONNX 图 | P4 | |
| `tflite` | | TensorFlow Lite 模型/算子 | P4 | |
| `coreml` | | CoreML 模型 | P5 | |
| `nnef` | | NNEF | P5 | |
| `bytecode` | `slbc` | SL 自有 SLIR 字节码 | P4 | 用于 AOT 缓存/回放 |

#### F. 专用 DSL `dsl`

| 标签 | 别名 | 说明 | 优先级 | 备注 |
|------|------|------|--------|------|
| `sql` | | 嵌入式 SQL | P3 | 与 `SLMysql.sl` 衔接 |
| `regex` | `regexp` | 正则（编译期编译，避免运行期解析） | P3 | 现有 `Std/Text/RegExp.sl` 是空壳 |
| `proto` | `protobuf` | Protobuf IDL | P3 | 与 `STREAM_DESIGN.md` §8 衔接 |
| `flatbuffers` | `fbs` | FlatBuffers IDL | P4 | |
| `openmp` | `omp` | OpenMP pragma（CPU 并行） | P4 | 指令式并行 |
| `openacc` | | OpenACC pragma（GPU offload） | P4 | |

### 18.3 家族继承机制（★ 避免 60 个标签重复定义）

```jsonc
"atSignLabelDefaults": {
  "asm":     { "kind":"asm",     "scope":"local",   "returnKeyword":null, ... },
  "foreign": { "kind":"foreign", "scope":"local",   "returnKeyword":"return", ... },
  "device":  { "kind":"device",  "scope":"local",   "returnKeyword":null, ... },
  "shader":  { "kind":"shader",  "scope":"local",   ... },
  "ir":      { "kind":"ir",      "scope":"local",   ... },
  "dsl":     { "kind":"dsl",     "scope":"local",   ... }
}
```

具体标签用 `"extends": "<familyKey>"` 继承，只写差异字段：

```jsonc
{ "name": "asm.riscv", "extends": "asm", "lang": "riscv", "isa": "rv64gc",
  "platform": { "cpu": ["rv64"] }, "params": [ { "name":"ext", "type":"string", "default":"gc" } ] }
```

**合并规则**：`子标签字段` 覆盖 `extends 家族字段`；`params[]` **追加**而非替换（同名则覆盖）；`platform` 取交集覆盖。

### 18.4 完整 `atSignLabel` jsonc（可直接粘贴）

```jsonc
{
  "atSignLabelDefaults": {
    "asm": {
      "kind": "asm",
      "scope": "local", "returnKeyword": null,
      "params": [
        { "name": "isa",   "type": "string", "default": null },
        { "name": "simd",  "type": "string", "default": "none" },
        { "name": "chip",  "type": "string", "default": "" },
        { "name": "opt",   "type": "string", "default": "-O2" }
      ],
      "captureRule": "pod-and-scalar-only",
      "fallback": "error",
      "handle": { "compile": "AsmAtSignHandler", "runtime": "vm_atsign_asm_call" }
    },

    "foreign": {
      "kind": "foreign",
      "scope": "local", "returnKeyword": "return",
      "params": [
        { "name": "version", "type": "string", "default": null },
        { "name": "timeout", "type": "Int32",  "default": "0" }
      ],
      "lexHints": { "lineComment": ["//"], "blockComment": ["/*","*/"],
                    "stringDelims": ["\"","'"], "escape": "\\\\" },
      "fallback": "sl",
      "handle": { "compile": "ForeignAtSignHandler", "runtime": "vm_atsign_foreign_call" }
    },

    "device": {
      "kind": "device",
      "scope": "local", "returnKeyword": null,
      "params": [
        { "name": "tile",    "type": "Int32[]", "default": "auto" },
        { "name": "tileNum", "type": "Int32",   "default": "0" },
        { "name": "block",   "type": "Int32[]", "default": "auto" },
        { "name": "grid",    "type": "Int32[]", "default": "auto" },
        { "name": "shared",  "type": "Int32",   "default": "0" },
        { "name": "device",  "type": "object",  "default": "Device.best()" },
        { "name": "stream",  "type": "Int32",   "default": "0" }
      ],
      "captureRule": "pod-and-tensor-only",
      "isolate": { "pool": null, "minWorkers": 1, "maxWorkers": 2, "restartOnCrash": true },
      "fallback": "next",
      "handle": { "compile": "HalAtSignHandler", "runtime": "vm_atsign_device_submit" }
    },

    "shader": {
      "kind": "shader",
      "scope": "local", "returnKeyword": null,
      "params": [
        { "name": "entry",  "type": "string", "default": "main" },
        { "name": "target", "type": "string", "default": null },
        { "name": "thread", "type": "Int32[]", "default": "[64,1,1]" }
      ],
      "captureRule": "pod-tensor-texture-only",
      "fallback": "sl",
      "handle": { "compile": "ShaderAtSignHandler", "runtime": "vm_atsign_shader_submit" }
    },

    "ir": {
      "kind": "ir",
      "scope": "local", "returnKeyword": null,
      "params": [
        { "name": "dialect", "type": "string", "default": null },
        { "name": "target",  "type": "string", "default": "host" },
        { "name": "opt",     "type": "string", "default": "-O2" }
      ],
      "captureRule": "pod-and-scalar-only",
      "fallback": "sl",
      "handle": { "compile": "IrAtSignHandler", "runtime": "vm_atsign_ir_call" }
    },

    "dsl": {
      "kind": "dsl",
      "scope": "local", "returnKeyword": null,
      "fallback": "sl",
      "handle": { "compile": "DslAtSignHandler", "runtime": "vm_atsign_dsl_call" }
    }
  },

  "atSignLabel": [
    // ── A. 通用语言 ──────────────────────────────────────────────
    { "name": "csharp", "extends": "foreign", "lang": "csharp",
      "runtime": { "lib": "mono-2.0-sgen.dll", "version": "net8", "entry": "mono_jit_init" },
      "platform": { "os": ["windows","linux"], "cpu": ["x64","arm64"] },
      "marshal": { "Int32": "System.Int32", "string": "System.String" },
      "handle": { "compile": "RoslynAtSignHandler", "runtime": "mono_bridge_invoke" } },

    { "name": "javascript", "extends": "foreign", "lang": "javascript",
      "runtime": { "lib": null, "version": "es2020", "entry": "JS_NewRuntime" },
      "handle": { "runtime": "js_bridge_invoke" } },

    { "name": "java", "extends": "foreign", "lang": "java",
      "runtime": { "lib": "jvm.dll", "probePath": ["${JAVA_HOME}/bin/server"], "version": "17" },
      "handle": { "runtime": "jvm_bridge_invoke" } },

    { "name": "c", "extends": "foreign", "lang": "c", "compiler": "clang" },
    { "name": "cpluslang", "extends": "foreign", "lang": "c++", "compiler": "clang" },
    { "name": "clang", "extends": "foreign", "lang": "c", "compiler": "clang" },
    { "name": "python", "extends": "foreign", "lang": "python", "runtime": { "version": "3.12" } },
    { "name": "dart",   "extends": "foreign", "lang": "dart" },
    { "name": "lua",    "extends": "foreign", "lang": "lua", "runtime": { "lib": "lua51.dll" } },
    { "name": "kotlin", "extends": "foreign", "lang": "kotlin", "runtime": { "version": "1.9" } },
    { "name": "shell",  "extends": "foreign", "lang": "bash" },

    // ── B. 指令集（你点名的 6 个在前）────────────────────────────
    { "name": "asm.x86",      "extends": "asm", "lang": "x86-64", "isa": "x86-64",
      "platform": { "cpu": ["x64"] }, "assembler": "nasm/yasm" },
    { "name": "asm.armv9",    "extends": "asm", "lang": "aarch64", "isa": "armv9-a",
      "platform": { "cpu": ["arm64"] } },
    { "name": "asm.armv7",    "extends": "asm", "lang": "arm", "isa": "armv7-a",
      "platform": { "cpu": ["arm","arm64"] } },
    { "name": "asm.cortexm",  "extends": "asm", "lang": "thumb-2", "isa": "armv7-m",
      "chip": ["stm32f1","stm32f4","stm32h7"], "platform": { "cpu": ["cortex-m"] } },
    { "name": "asm.mcs51",    "extends": "asm", "lang": "asm51", "isa": "mcs-51",
      "assembler": "as51/sdcc" },
    { "name": "asm.loongarch","extends": "asm", "lang": "loongarch", "isa": "loongarch64",
      "platform": { "cpu": ["loongarch64"] } },
    { "name": "asm.mips",     "extends": "asm", "lang": "mips", "isa": "mips32r2" },
    { "name": "asm.riscv",    "extends": "asm", "lang": "riscv", "isa": "rv64gc",
      "params": [ { "name":"ext", "type":"string", "default":"gc" } ] },
    { "name": "asm.thumb",    "extends": "asm", "lang": "thumb", "isa": "thumb-2" },
    { "name": "asm.xtensa",   "extends": "asm", "lang": "xtensa", "isa": "xtensa-lx6" },
    { "name": "asm.avr",      "extends": "asm", "lang": "avr",  "isa": "avr" },
    { "name": "asm.pic",      "extends": "asm", "lang": "pic",  "isa": "pic16" },
    { "name": "asm.msp430",   "extends": "asm", "lang": "msp430", "isa": "msp430" },
    { "name": "asm.rl78",     "extends": "asm", "lang": "rl78", "isa": "rl78" },
    { "name": "asm.rx",       "extends": "asm", "lang": "rx",   "isa": "rxv2" },
    { "name": "asm.hc08",     "extends": "asm", "lang": "hc08", "isa": "hcs08" },
    { "name": "asm.tricore",  "extends": "asm", "lang": "tricore", "isa": "tc1.6" },
    { "name": "asm.ppc",      "extends": "asm", "lang": "ppc",  "isa": "powerpc64" },
    { "name": "asm.sparc",    "extends": "asm", "lang": "sparc","isa": "sparcv9" },
    { "name": "asm.sw64",     "extends": "asm", "lang": "sw64", "isa": "sw64" },
    { "name": "asm.wasm",     "extends": "asm", "lang": "wat",  "isa": "wasm" },
    { "name": "asm.bpf",      "extends": "asm", "lang": "bpf",  "isa": "ebpf" },
    { "name": "asm.x86.avx512","extends": "asm", "lang": "x86-64", "isa": "x86-64+avx512" },

    // ── C. 计算设备 ──────────────────────────────────────────────
    // C1 CPU
    { "name": "cpu",      "extends": "device", "hal": "CpuHal", "lang": "sl-subset" },
    { "name": "cpu.vm",   "extends": "device", "hal": "VmHal", "lang": "sl" },
    { "name": "simd.neon","extends": "device", "hal": "CpuHal", "isa": "neon",
      "platform": { "cpu": ["arm64"] }},
    { "name": "simd.sve",  "extends": "device", "hal": "CpuHal", "isa": "sve2",
      "platform": { "cpu": ["arm64"] }},
    { "name": "simd.sse",  "extends": "device", "hal": "CpuHal", "isa": "sse4.2",
      "platform": { "cpu": ["x64"] }},
    { "name": "simd.avx",  "extends": "device", "hal": "CpuHal", "isa": "avx2",
      "platform": { "cpu": ["x64"] }},
    { "name": "simd.avx512","extends": "device", "hal": "CpuHal", "isa": "avx512",
      "platform": { "cpu": ["x64"] }},
    { "name": "simd.rvv",  "extends": "device", "hal": "CpuHal", "isa": "rvv",
      "platform": { "cpu": ["rv64"] }},
    { "name": "simd.lsx",  "extends": "device", "hal": "CpuHal", "isa": "lsx",
      "platform": { "cpu": ["loongarch64"] }},

    // C2 GPU（你点名的 cuda / amd 在前）
    { "name": "gpu.nvidia", "extends": "device", "hal": "CudaHal", "lang": "cuda-c",
      "compiler": "nvcc", "isolate": { "pool": "hal-cuda" },
      "params": [ { "name":"precision","type":"string","default":"auto" },
                  { "name":"useTensorCore","type":"string","default":"auto" } ] },
    { "name": "gpu.amd",    "extends": "device", "hal": "RocmHal", "lang": "hip-c",
      "compiler": "hipcc", "isolate": { "pool": "hal-rocm" },
      "params": [ { "name":"useMatrixCore","type":"string","default":"auto" } ] },
    { "name": "gpu.intel",  "extends": "device", "hal": "SyclHal", "lang": "sycl",
      "compiler": "icpx", "isolate": { "pool": "hal-sycl" } },
    { "name": "gpu.opencl", "extends": "device", "hal": "OpenClHal", "lang": "opencl-c",
      "isolate": { "pool": "hal-opencl" } },
    { "name": "gpu.apple",  "extends": "device", "hal": "MetalHal", "lang": "metal",
      "isolate": { "pool": "hal-metal" } },
    { "name": "gpu.vulkan", "extends": "device", "hal": "VulkanHal", "lang": "glsl",
      "isolate": { "pool": "hal-vulkan" } },
    { "name": "gpu.webgpu", "extends": "device", "hal": "WebGpuHal", "lang": "wgsl" },

    // C3 NPU（你点名的 npu）
    { "name": "npu",           "extends": "device", "hal": "NpuHal", "lang": "sl-subset",
      "isolate": { "pool": "hal-npu" } },
    { "name": "npu.ascend",    "extends": "device", "hal": "NpuHal", "lang": "ascend-c",
      "sdk": "CANN/AscendCL", "isolate": { "pool": "hal-ascend" } },
    { "name": "npu.cambricon", "extends": "device", "hal": "NpuHal", "lang": "bang-c",
      "sdk": "CNRT", "isolate": { "pool": "hal-cambricon" } },
    { "name": "npu.horizon",   "extends": "device", "hal": "NpuHal", "sdk": "OpenExplorer" },
    { "name": "npu.rockchip",  "extends": "device", "hal": "NpuHal", "sdk": "RKNN" },
    { "name": "npu.qualcomm",  "extends": "device", "hal": "NpuHal", "sdk": "QNN/SNPE" },
    { "name": "npu.intel",     "extends": "device", "hal": "NpuHal", "sdk": "OpenVINO" },
    { "name": "npu.amd",       "extends": "device", "hal": "NpuHal", "sdk": "XDNA" },
    { "name": "npu.apple",     "extends": "device", "hal": "NpuHal", "sdk": "CoreML" },
    { "name": "npu.mediatek",  "extends": "device", "hal": "NpuHal", "sdk": "NeuroPilot" },

    // C4 DSP / C5 FPGA
    { "name": "dsp.ti",       "extends": "device", "hal": "DspHal", "isa": "c66x" },
    { "name": "dsp.hexagon",  "extends": "device", "hal": "DspHal", "isa": "hexagon" },
    { "name": "fpga.hls",     "extends": "device", "hal": "FpgaHal", "lang": "c++" },
    { "name": "fpga.verilog", "extends": "device", "hal": "FpgaHal", "lang": "verilog" },

    // ── D. 着色器（你点名的 hlsl 在前）───────────────────────────
    { "name": "hlsl",  "extends": "shader", "lang": "hlsl", "compiler": "dxc",
      "params": [ { "name":"target","type":"string","default":"cs_6_0" } ] },
    { "name": "glsl",  "extends": "shader", "lang": "glsl", "compiler": "glslang" },
    { "name": "msl",   "extends": "shader", "lang": "metal" },
    { "name": "wgsl",  "extends": "shader", "lang": "wgsl" },

    // ── E. IR（你点名的 mlir / llvm 在前）────────────────────────
    { "name": "mlir",  "extends": "ir", "lang": "mlir",
      "params": [ { "name":"dialect","type":"string","default":"linalg" } ] },
    { "name": "llvm",  "extends": "ir", "lang": "llvm-ir" },
    { "name": "ptx",   "extends": "ir", "lang": "ptx", "platform": { "vendor": ["nvidia"] } },
    { "name": "spirv", "extends": "ir", "lang": "spir-v" },
    { "name": "wasm",  "extends": "ir", "lang": "wat" },
    { "name": "tosa",  "extends": "ir", "lang": "tosa" },
    { "name": "stablehlo", "extends": "ir", "lang": "stablehlo" },

    // ── F. DSL ───────────────────────────────────────────────────
    { "name": "sql",   "extends": "dsl", "lang": "sql" },
    { "name": "regex", "extends": "dsl", "lang": "regex" },
    { "name": "proto", "extends": "dsl", "lang": "proto3" }
  ],

  "atSignLabelAlias": {
    "cs":"csharp", "c#":"csharp", "js":"javascript", "ts":"typescript",
    "cpp":"cpluslang", "c++":"cpluslang", "py":"python", "kt":"kotlin",
    "rs":"rust", "golang":"go", "bash":"shell", "sh":"shell", "objc":"objectivec",
    "x86":"asm.x86", "x64":"asm.x86", "asm-x86":"asm.x86", "avx512":"asm.x86.avx512",
    "arm64":"asm.armv9", "aarch64":"asm.armv9", "armv9":"asm.armv9", "armv8":"asm.armv9",
    "armv7":"asm.armv7", "arm32":"asm.armv7", "thumb":"asm.thumb",
    "stm32":"asm.cortexm", "cortexm":"asm.cortexm",
    "c51":"asm.mcs51", "8051":"asm.mcs51", "51":"asm.mcs51",
    "loong":"asm.loongarch", "loongarch64":"asm.loongarch",
    "mips":"asm.mips", "mips32":"asm.mips", "mips64":"asm.mips",
    "rv32":"asm.riscv", "rv64":"asm.riscv", "riscv":"asm.riscv",
    "esp32":"asm.xtensa", "wasm":"asm.wasm", "ebpf":"asm.bpf",
    "neon":"simd.neon", "sve":"simd.sve", "sse":"simd.sse", "sse4":"simd.sse",
    "avx":"simd.avx", "avx2":"simd.avx", "rvv":"simd.rvv", "lsx":"simd.lsx",
    "cuda":"gpu.nvidia", "nvidia":"gpu.nvidia", "nv":"gpu.nvidia",
    "amd":"gpu.amd", "hip":"gpu.amd", "rocm":"gpu.amd",
    "sycl":"gpu.intel", "oneapi":"gpu.intel", "opencl":"gpu.opencl",
    "vulkan":"gpu.vulkan", "webgpu":"gpu.webgpu",
    "ascend":"npu.ascend", "cann":"npu.ascend",
    "cambricon":"npu.cambricon", "bang":"npu.cambricon",
    "horizon":"npu.horizon", "bpu":"npu.horizon", "rknn":"npu.rockchip",
    "hexagon":"npu.qualcomm", "qnn":"npu.qualcomm", "snpe":"npu.qualcomm",
    "movidius":"npu.intel", "openvino":"npu.intel",
    "xdna":"npu.amd", "ryzen-ai":"npu.amd",
    "ane":"npu.apple", "coreml":"npu.apple", "apu":"npu.mediatek",
    "metal":"msl", "spir-v":"spirv", "hlo":"stablehlo", "mhlo":"stablehlo",
    "llvmir":"llvm", "regexp":"regex", "protobuf":"proto"
  },

  "atSignLabelFallbackOrder": [
    "npu.ascend","npu.cambricon","npu.horizon","npu.qualcomm","npu.intel",
    "gpu.nvidia","gpu.amd","gpu.intel","gpu.apple","gpu.opencl",
    "simd.avx512","simd.avx","simd.sve","simd.neon","simd.rvv",
    "asm.x86","asm.armv9","asm.riscv","asm.loongarch","asm.mips",
    "cpluslang","csharp","javascript","cpu","cpu.vm"
  ]
}
```

### 18.5 别名机制

`atSignLabelAlias` 让短名映射到全名，`@cs` ≡ `@csharp`、`@cuda` ≡ `@gpu.nvidia`、`@stm32` ≡ `@asm.cortexm`。

| 规则 | 说明 |
|------|------|
| 解析顺序 | 先查 `atSignLabel` 全名 → 再查 `atSignLabelAlias` → 都没有报 `UnknownAtSignLabel` |
| 别名不定义新行为 | 只是名字映射，**不能**在 alias 上配置字段 |
| 冲突检测 | 别名与已有标签/attribute 同名 → 报 `AtSignLabelNameConflict` |
| 建议 | 别名用于**高频短写**（`@js` `@cs` `@cuda` `@neon`），正式文档用全名 |

### 18.6 优先级与回退链

`atSignLabelFallbackOrder` 决定 kernel 特化体的选择顺序（§12）：

```sl
kernel matmul( A, B ) { ret A · B }        # SL 主体（永远兜底）
@npu.ascend   { ... }                       # 1st
@gpu.nvidia   { ... }                       # 2nd
@gpu.amd      { ... }                       # 3rd
@simd.avx     { ... }                       # 4th
@cpu          { ... }                       # 5th
```
运行时按 `fallbackOrder` 挑**第一个平台可用**的；全不可用 → 用 SL 主体。

### 18.7 你未列出但建议纳入的（补充清单）

| 类别 | 建议补充 | 理由 |
|------|---------|------|
| **CPU SIMD** | `simd.neon/sve/sse/avx/avx512/rvv/lsx` | 你列了 `cpu`，但 CPU 极致优化靠 SIMD，不是标量 |
| **VmHal** | `cpu.vm` | `TENSOR` 文档 §9.1 明确「VmHal：CVM 解释器（兜底，永远存在）」 |
| **NPU 细分** | 9 家国产/国际 NPU | 你只写了 `npu`，实际各家 SDK 完全不兼容，必须分开 |
| **DSP** | `dsp.ti/hexagon/ceva/cadence` | 音频/通信领域刚需 |
| **FPGA** | `fpga.verilog/vhdl/hls` | 硬件加速另一条路 |
| **Shader 全平台** | `glsl` `msl` `wgsl` | 你只列了 `hlsl`（D3D），跨端需另三种 |
| **WASM** | `wasm` / `asm.wasm` | 网页端落地（`target.md:73` 已提到 WebAssembly） |
| **eBPF** | `asm.bpf` | 内核/网络可观测性 |
| **DSL** | `sql` `regex` `proto` | 与你现有 `SLMysql.sl` / `RegExp.sl` / `STREAM_DESIGN.md` 直接衔接 |
| **OpenMP/OpenACC** | `openmp` `openacc` | 指令式并行，比手写 kernel 门槛低 |
| **国产 CPU** | `asm.loongarch` `asm.sw64` | 你列了 loong，补申威 |

### 18.8 分期（对应 §16）

标签**不需要一次全做**——同一套机制，加标签只是加 jsonc 配置：

| 期 | 新增标签 |
|----|---------|
| P0 | 无（只有回声 handler） |
| P1 | `csharp` |
| P1.2 | 协程 / 隔离岛驱动（`spawnFunc`） |
| P1.5 | `javascript` |
| P2 | `gpu.nvidia` `gpu.amd` `npu.ascend` `cpu` `cpu.vm` `c` `cpluslang` |
| P2.5 | AOT 路径：`llvm` `mlir` `ptx` |
| P3 | `asm.x86` `asm.armv9` `asm.riscv` `asm.cortexm` `asm.mcs51` `hlsl` `simd.*` `python` `dart` `java` |
| P4 | 其余 NPU / DSP / FPGA / shader / DSL / 复古 ISA |

---

## 19. 附录

### 19.1 FAQ

**Q1：为什么不直接暴露栈指针，那样最快？**
A：栈布局是 VM 实现细节。一旦暴露，未来任何栈布局调整、寄存器分配、GC 移动都会让所有外部代码崩溃，且**无法静态检查**。通道（访问器 + marshal）的开销在 AOT 路径（P2.5）下可大部分被内联消除，性价比远高。

**Q2：`@gpu` 的 `tile` 和 `block` 到底有什么区别？**
A：`tile` 是**数据视角**（每个分块处理多少元素，如 `[16,16]`）；`block` 是**执行视角**（CUDA `blockDim`，一个线程块多少线程）。`grid` 由 `数据规模 / tile` 推导。三者默认 `auto`，交给 HAL 与未来的 autotuner。

**Q3：块内能调用 SL 函数吗？**
A：能，通过 `SL.call(...)` 门面（§6.5）。但**设备块内不行**（设备侧无 SL 运行时），需提前把结果算好传进去。

**Q4：为什么设备/asm 强制显式 `in/out`？**
A：GPU/NPU 不可能「顺手读主机对象」。强制显式能让**编译期就拒绝**不可能实现的代码，而不是运行期崩。这也和 `TENSOR_HETEROGENEOUS_DESIGN.md:144-150`「跨设备访问 = 编译错误」一致。

**Q5：不写 spawn 就是同步，会不会有人忘了处理耗时调用？**
A：有 `InlineBlocking` 警告兜底（§8.10）：编译器检测到块体明显耗时（含 `sleep` / 网络 / 大 kernel）却直接执行时给出警告，建议改 `Coroutine.spawnFunc0`。**只警告不报错**——因为是否要异步是业务决定。

**Q6：`Isolate.spawnFunc0` 是每个块新建一个岛吗？**
A：**不是**（§8.5 I4）。Mono domain / QuickJS runtime / 设备上下文都是重资源，每块 `mono_jit_init` 会慢到不可用。语义是「提交到**该标签的常驻 worker 岛池**」（`isolate.pool` 配置，同池复用），与 `TENSOR_HETEROGENEOUS_DESIGN.md:411-413`「HAL 后端就是一个长生命周期 worker isolate」完全同构。

**Q7：协程和隔离岛是二选一吗？**
A：**不是**（§8.5 I4）。`Isolate.spawnFunc0` 提交到**该标签的常驻 worker 岛池**（`isolate.pool` 配置，同池复用）。Mono domain / QuickJS runtime / 设备上下文都是重资源，每块新建会慢到不可用。

**Q8：`isolate` 下为什么不能 `a = f(a)` 这样写回？**
A：岛内改的是**副本**，写不回外层（§8.5 I1）。如果允许，同一个写法在「直接执行 / 协程」下生效、在「隔离岛」下静默失效——这是最难查的 bug 类型。所以**编译期直接报错**，强制改用 `return`。这是用隔离岛唯一的语义代价，换来的是崩溃隔离。

**Q9：已有 `@GPU(...)` 13 参数 attribute 怎么办？**
A：保留兼容不动。新代码用 `@gpu( tile=[16,16] ) { }`。长期建议把 `@GPU` 标记为 deprecated。

**Q10：为什么解析必须外置？SL 编译器自己扫一遍 rawBody 不行吗？**
A：不行，那是**越界**。要正确扫出 C# 的标识符，你得处理 `@"..."` 逐字串、`$"{a}"` 插值、预处理指令、泛型 `<>`、属性 `[Obsolete]`……扫 CUDA 又要处理 `__global__`、`<<<>>>`、模板。SL 编译器为每种语言都写一遍扫描器，等于**把 N 种语言的词法规则塞进 SL 内核**——以后加语言就要改编译器。外置后：插件用各自语言的**真解析器**（Roslyn 扫 C#、clang 扫 C++、DXC 扫 HLSL），准确率还更高。

**Q11：parser 编译期跑还是运行期跑？**
A：由 `parser.stage` 配置（§10.7）。**想让 `$x` 拼写错在编译期就报错 → 必须 `compile`**（编译期要查 SL 符号表）；`runtime` 下编译器完全不加载插件，负担最小，但 `$x` 写错要到运行期才发现。建议常用标签走 `compile`——因为解析只要两条正则，开销很小。

**Q12：handler dll 崩溃了怎么办？**
A：取决于执行形态（§10.8）。用 `Isolate.spawnFunc0` 时 dll 在 worker 岛里跑，崩溃只塌岛 → 抛 `IsolateCrashed`，**主程序存活且岛自动重建**，可 `try/catch` 后重试（§14.9）。直接执行 / 协程时 dll 在主岛，崩溃是**进程级**的——所以只给 `exec.trusted = true` 的自有 dll。

**Q13：handler dll 能用 C# / Rust / Zig 写吗？**
A：能。ABI 是纯 C 的（`SLLabelExecCtx*` 单参数 + 函数表），任何能导出 C 符号的语言都行：
- C/C++、Rust（`#[no_mangle] extern "C"`）、Zig（`export fn`）→ 直接写
- C# → NativeAOT 或 `UnmanagedCallersOnly` 导出
- Go → `//export` + c-shared
> 唯一要求是导出 §10.6 的 5 个符号并实现 `SL_LABEL_ABI_VERSION`。

**Q14：加一个新标签要改编译器吗？**
A：**不需要**（§14.11）。加一段 jsonc + 提供一个 dll 即可；简单标签连 dll 都不用（写个声明式规则文件，§14.12）。SL 编译器与 VM 代码改动量为 **0 行**——这正是「独立子系统」原则的收益。

**Q15：为什么要 `$` 前缀，直接写变量名不行吗？**
A：直接写会**分不清是 SL 的还是宿主语言的**。`@csharp { a = f(a); }` 里的 `a` 到底是 SL 的 `a` 还是 C# 的 `a`？同名时尤其致命。
加上 `$` 后：`$a` 必是 SL 的，`a` 必是 C# 的——**零歧义**。附带好处是 **Parser 插件只需两条正则**（找 `$name` 和 `<-`/`->`），完全不用懂宿主语言词法（§6.2）。

**Q16：`<-` 和 `->` 有什么区别？**
A：**没有语义区别，只是方向反过来写**。规则统一为「**箭头指向数据目的地**」：
- `目标 <- $源` ≡ `$源 -> 目标`（都是 SL → 外部）
- `$目标 <- 源` ≡ `源 -> $目标`（都是外部 → SL）
推荐用 `<-`（目标在左，符合赋值直觉），`->` 作为等价的反向写法。

**Q17：C# 里也有 `->`（`ptr->field`），会不会误判？**
A：不会。规则是**通道必须有一端是 `$`**（§6.1）。`ptr->field` 两端都没有 `$`，直接忽略。

**Q18：隔离岛下 `$x <- ...` 什么时候生效？**
A：在 **`await` 点**（§8.5）。流程是：岛内收集所有 Out 通道结果 → 打包回传 → 主岛在 `await` 处统一写回 → unpin。
所以 `Isolate.spawnFunc0(...)` 之后**立刻** `print($x)` 读到的还是旧值，必须 `await` 之后才是新值。**中间的 `<-` 不会触发 N 次跨岛往返**，是批量打包的。

**Q19：`$xxx` 和宏（`$xxx`）冲突吗？**
A：不冲突，因为**块体是 raw，宏替换默认不作用于块体**（§4.5.6）。块内的 `$a` 永远是通道引用。若某个标签确实需要宏生效，加 `"expandMacroInBody": true`（不推荐，会让 `$` 二义）。

**Q20：块内代码有语法高亮/补全吗？**
A：P4 利用 `atSignLabel.lang` 字段交给 IDE 做嵌套语言高亮（VS Code 的 embedded language 机制）。

### 19.2 术语表

| 术语 | 含义 |
|------|------|
| **AtSignLabel / @ 标签块** | `@name(params){ body }` 形式的异质代码块 |
| **raw source** | 块体的原始文本，SL lexer 不解析，靠花括号配平捕获 |
| **通道表（Channel Table）** | 编译期生成的 `{dir, slVar, slType, target, deviceTransfer}` 列表，描述块的进出数据（由 `ParseResult.channels` 查 SL 符号表而来） |
| **参数（Parameter）** | 写在 `@tag(...)` 里、描述「怎么执行」的具名配置（tile/block/timeout…） |
| **捕获（Capture）** | 写在 `{ }` 里引用、描述「处理什么数据」的 SL 变量 |
| **accessor** | 读写捕获变量的 getter/setter，替代裸栈指针 |
| **pin** | 块执行期间固定 GC 对象，防止移动/回收 |
| **执行形态** | **同步**（直接写）/ **协程**（`Coroutine.spawnFunc0`）/ **隔离岛**（`Isolate.spawnFunc0`） |
| **`$名称`** | ★ 块内引用**外层 SL 变量**的标记。不带 `$` 的标识符 = 宿主语言自己的 |
| **通道（Channel）** | ★ `<-` / `->` 表达的**数据传输**，箭头指向目的地；必须有一端是 `$`。不是赋值 |
| **In 通道** | `目标 <- $源` / `$源 -> 目标`：SL → 外部 |
| **Out 通道** | `$目标 <- 源` / `源 -> $目标`：外部 → SL |
| **ChannelTable** | 编译期生成的通道表 `{dir, slVar, slType, target, deviceTransfer}` |
| **管道（Transport）** | 通道的**物理实现**：三阶段 Prepare / Transfer / Commit，见 §8.6 |
| **borrow / copy / move** | 管道的三种所有权语义：借用（零拷贝+pin）/ 深拷贝 / 转移（源失效） |
| **通道槽（Channel Slot）** | 协程模式下存放 In 通道值的堆内存（因为协程栈上没有原函数的局部变量），一次分配 |
| **`ChannelBlob`** | isolate 模式的批量打包格式，所有通道一次序列化，只往返一次 |
| **域（Domain）** | §11.2：值"住在"哪个世界——`SL` / `FOREIGN` / `DEVICE` / `ISOLATE` |
| **一致性等级** | §11.3：`SNAPSHOT`（默认，冻结快照）/ `PINNED`（共享双向可见）/ `TRANSFER`（源失效）/ `DETACHED`（独立副本） |
| **epoch（版本号）** | §11.4：每次写入 bump；快照时记录，**写回时比对**以发现"异步期间被改" |
| **`StaleWriteBack`** | §11.9：异步写回时发现目标槽 epoch 已变，且 `onConflict = "error"` |
| **VMS** | 值转换系统（Value Marshalling System）——§11，管道的底层；纯函数式，可独立单测 |
| **worker 岛池** | `Isolate.spawnFunc0` 的目标：按 `isolate.pool` 复用的常驻隔离岛，**不是每块新建** |
| **隐式 await** | 块在语句位置时编译器自动插入的等待 |
| **fallback** | 平台/运行时不可用时回落 SL 主实现或报错 |
| **lexHints** | 花括号配平所需的宿主语言词法规则（注释/字符串/转义） |

### 19.3 相关文档

| 文档 | 关联 |
|------|------|
| `md/syntax/attribute.md` | `@` 的现有语义，歧义消除的边界 |
| `md/design/TENSOR_HETEROGENEOUS_DESIGN.md` §10.2 | 设备特化块提案（本设计接管并泛化） |
| `md/design/MONO_INTEGRATION_DESIGN.md` | `@csharp` 的运行时 |
| `md/design/QUICKJS_INTEGRATION_DESIGN.md` | `@js` 的运行时 |
| `md/design/HOTSPOT_JNI_INTEGRATION_DESIGN.md` | `@java` 的运行时 |
| `md/project/ffi.md` | 编译期 AOT 产物绑定 `OpCode_CallFFIStatic`；回调 trampoline |
| `md/syntax/coroutine.md` | `Task` / `await`（异步底座） |
| `md/design/ISOLATE_DESIGN.md` | `Sendable` 白名单、`TransferableData` 零拷贝（`Isolate.spawnFunc0` 的传输语义依据） |
| `md/design/STREAM_DESIGN.md` §12.1 | 可发送性矩阵（哪些类型不可跨岛） |
| `md/syntax/isolate.md` | isolate / `SendPort` / `ReceivePort` 语法 |
| `md/project/project-config-jsonc-guide.md` | `.jsonc` 顶层块规范与扩展点 |
| `md/project/project_sp-guide.md` | `CompileBefore()` 编译期钩子 |
| `md/syntax/marco.md` | `$xxx` 宏（**不是** `@`，勿混） |

### 19.4 设计要点复盘

> **一个形态（`@tag(params){ raw }`）承载六种 kind（foreign / device / shader / asm / ir / dsl）、60+ 个标签（§18），四个正交维度（参数 / 通道 / 结果 / 执行形态）。**
>
> **四条铁律**：
> 1. **`()` 只放标签参数，不是数据栈** —— `tile` / `version` / `timeout` 这些"怎么跑"的配置
> 2. **`()` 内必须具名**（`参数名 = 值`） —— 位置参数编译失败
> 3. **块内引用 SL 变量必须写 `$名称`** —— 不带 `$` 的一律是宿主语言自己的
> 4. **进出必须走通道 `<-` / `->`** —— 箭头指向目的地，必须有一端是 `$`
>
> **各维度怎么解决你的疑问**：
> - **参数**：`@gpu( tile = [16,16] )` —— 解决「不知怎么传 tile/width/height」
> - **标签名**：`@gpu.amd.tensor` —— 分层不限层数，最长匹配优先，逐层继承
> - **通道**：`int a <- $a;` 传入、`$count <- f(a,20);` 传出 —— 解决「我的语言的参数怎么传入传出」。编译期生成通道表，运行时访问器 + marshal + pin，**绝不暴露栈指针**
> - **结果**：通道写出 + 宿主语言 `return` 两条路径
> - **执行形态**：直接写 = 同步；`Coroutine.spawnFunc0( @tag(){} )` = 协程；`Isolate.spawnFunc0( @tag(){} )` = 隔离岛。**零新语法**
>
> **管道（§8.6，通道的物理实现）**：三阶段 Prepare / Transfer / Commit；三种所有权语义
> | | 同步 | 协程 | 隔离岛 | 设备 |
> |---|---|---|---|---|
> | 语义 | **borrow** | **borrow**（标量拷、对象借用） | **copy / move** | DMA |
> | 缓冲 | 无（栈上 `SLLabelValue[]`） | 通道槽（一次分配） | `ChannelBlob` | 设备内存 |
> | 拷贝量 | **0** | 仅标量 | **全部**（除 move） | H2D / D2H |
> | 写回 | 立即 | 块结束 | **`await` 点** | 拷出 |
> | 往返 | 0 | 0 | **1**（批量） | 异步 DMA |
>
> **★ 值转换系统（§11，管道的底层）**：
> - **域**：`SL` / `FOREIGN` / `DEVICE` / `ISOLATE` —— 值"住在"哪个世界
> - **一致性等级**：默认 **`SNAPSHOT`**（提交时冻结，源后续改动不影响块，结果可复现）；`PINNED` 共享双向可见（需 opt-in）；`TRANSFER` 源失效；`DETACHED` 独立副本
> - **epoch 版本号**：每次写入 bump，快照时记录、**写回时比对** → 发现「异步期间被改」→ 按 `onConflict` 处理
> - 解决你担心的六类不一致：异步源被改 / 写回覆盖新值 / 共享引用半新半旧 / GC 移动 / 设备主机分歧 / 跨线程
>
> **代价与收益**：
> - **隔离岛的代价**：不共享内存 → 入参必须可发送、出参在 `await` 点才写回、不可回调 SL。换来崩溃隔离
> - **★ 最大收益**：因为用 `$` 标记，Parser 插件**不需要懂任何宿主语言**，两条正则通吃所有标签
> - **★ signLabel 是独立子系统**（§10）：解析外置 Parser 插件、执行外置 Handler DLL，SL 只保留三处耦合（入参 / 出参 / 调度）。**新增标签 = 一段 jsonc + 一个 dll，编译器改 0 行**
>
> 全部标签都是 `.jsonc` 的数据，不是语言内置关键字；底层复用 FFI 静态绑定、Mono/QuickJS/JNI 桥接、`Task`/`await` 协程、`Isolate`/`SendPort` —— **新增的只有「圈出这段代码并声明契约」的能力**。
