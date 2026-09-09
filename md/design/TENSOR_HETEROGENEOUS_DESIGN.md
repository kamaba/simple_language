# SimpleLanguage 张量 / 数学库异构计算设计（CPU / GPU / NPU）

> 版本：v1.0　|　日期：2026-09-09　|　状态：**设计草案（待评审）**
>
> 目标：让「数学库 + 张量库」在语言层**写法优雅简单**，在运行层把
> **控制流（if / while / for）留在 CPU**、把**密集浮点计算与比较下沉到 GPU / NPU**，
> 并基于已引入的 MLIR 机制落地。
>
> 前置阅读：`md/design/MLIR_AOT_DESIGN.md`（三期后端总设计）、
> `source/Front/Export/MLIR/MLIR_AOT_实现与待办.md`（发射器现状与限制清单）。
> 本文不写代码，只给设计决策与取舍理由；实现前请先 grep 源码确认。

---

## 0. TL;DR — 一分钟速览

| 问题 | 结论 |
|------|------|
| 核心哲学 | **两区模型**：Host 区（控制流）+ Device 区（数据并行），边界显式、编译期可检查。不要追求"全语言自动上 GPU" |
| 最根本的瓶颈 | SLIR 是**栈机 + 标量槽位**，并行化所需信息已丢失。**不要在其上做并行分析**，必须另起一层 SSA/张量化 IR |
| 后端关键决策 | **直接发射 `linalg` / `scf` / `tensor`，不大量自造 dialect**——白拿 MLIR 官方的融合 / tiling / 向量化 / GPU 映射，并同时拿到 NPU |
| NPU 关键决策 | NPU 是"大算子加速器"，只吃 `matmul` / `conv` 这类**粗粒度命名算子**，散落的标量循环会退化。**必须走算子派** |
| 最优雅的来源 | 两种用户视角分离：库**使用者**只写张量表达式（零心智负担）；库**作者**才需要分区标注与调度控制 |
| 最先该做的两件事 | ① `Float32_3/4/3x3/4x4` 从 `class` 改 `data`（值类型）　② 阻止 F32 的 f64 加宽 ABI 泄漏到设备端 |
| 安全网 | 保留并强化现有「**失败即回退 CVM**」机制——这是能激进重构的唯一前提 |

---

## 1. 背景与目标

### 1.1 需求

数学库（`Lib/Math/*`）与未来的张量库需要：

- 库内部用 `if / while / for` 做形状判断、循环调度、分支选择 —— 这些在 **CPU** 上跑；
- 密集的**浮点计算与比较** —— 在 **GPU / NPU** 上跑；
- 语言写法要**优雅、简单**；
- 运行速度要**合理**（不是"能跑"，而是接近手写）。

### 1.2 现状资产（可直接复用）

| 资产 | 位置 | 本文如何复用 |
|------|------|-------------|
| 分区标注雏形 | `@GPU` / `@AOT`（`Lib/Std/GPU/GPU.sl`、`Front/Export/MLIR/`） | 重做为 `@device` + 自动着色 |
| 多实现兜底模式 | `Mathf.sl` 的 `@DllImport` + SL fallback 本体 | 泛化为 **cpu / gpu / npu 多目标特化** |
| 值语义类型 | `data`（`target.md` 第 25 条：栈上生成，相当于结构体） | 小向量 / 矩阵的载体 |
| 运算符重载 | `_add_` / `_mul_` / `_eq_` …（`md/syntax/operator.md`） | 张量表达式的语法基础 |
| 模板 | `class<T>` / `fun<T>`（`md/syntax/template.md`） | `Tensor<T, Rank>` 的参数化 |
| 类型别名 | `@Nickname("float3")`（`Lib/Math/Float32_3.sl`） | 推广为张量类型别名 |
| 协程 | `spawn / await / Channel`（`md/syntax/coroutine.md`） | **零新语法**地表达设备异步 |
| 窄浮点四格式 | e4m3/e5m2/f16/bf16 位精确软实现 | 按目标选实现（§11-7） |
| 失败即回退 | 发射/工具链失败 → 回退 CVM 解释 | 推广为四路选择（cpu/gpu/npu/vm） |

---

## 2. 现状诊断：三处结构性矛盾

### 2.1 矛盾一：GPU 语义靠"猜循环"，不可控也不可读

`test/SpecialTest/AOTGPUTest.sl` 中，GPU kernel 写的是三重串行 `for`，由发射器
「识别出一个可并行的 for 循环（`i += 1` 步进），识别失败 Fail」（`MLIR_AOT_实现与待办.md` §4）。
`Lib/Std/GPU/GPU.sl` 的标注则是 13 个位置参数：
`@GPU( 0, 0, 0, 0, 0, 0, 0, 256, 1, 1, 0, 0, "" )`。

三个后果：

1. **写得丑**：13 个魔数位置传参，无人能记住第 8 位是 `blockDimX`。
2. **猜得脆**：循环识别失败即整方法回退，且识别规则与用户写法强耦合。
3. **拿不到性能**：手写 `a[i * K + j]` 扁平索引，到 MLIR 只是带复杂仿射表达式的
   `memref.load`，别名分析 / 向量化 / tiling 全部半残。

### 2.2 矛盾二（最根本）：栈机 SLIR 是并行化的死路

SLIR 是**线性栈机 + 标量槽位**（`I64 / F64 / F32 / Struct / ObjRef`，见
`MLIR_AOT_实现与待办.md` §2.1）。

并行化与 NPU 映射所需的全部信息 —— **迭代空间、循环嵌套结构、访存函数、数据依赖、形状** ——
在栈机里已被拍平成一条指令流。`tensor → 栈机` 是一次**降维**，降下去就升不回来。

> **结论：不要试图在 SLIR 上加并行分析。** 所有 `linalg` / `vector` / `affine` / `scf`
> 的现成 pass 都对不上栈机的输入格式。必须另起一层（§9）。

### 2.3 矛盾三：数学库的对象模型在吃性能

- `Lib/Math/Float32_3.sl` 是 `class`（GC 堆对象），`_add_` 是运算符重载。
  一次 `a + b` = 一次堆分配 + 一次分发 + 后续 GC。**小向量运算的开销全在对象模型上**，
  后端再快也补不回来。
- GPU 路径**显式禁止** Struct / ObjRef 槽位（`MLIR_AOT_实现与待办.md` §4），
  因此现有向量类型**根本进不了 kernel** —— 数学库与 GPU 是断开的。
- **隐藏炸弹**：F32 域「i64 位模式中恒存精确 f64 加宽值」（§2.1）。在 CVM 解释路径无碍，
  但 **GPU 上 f64 通常只有 f32 的 1/2 ~ 1/64 吞吐，多数 NPU 根本没有 f64**。
  该 ABI 泄漏到设备端会直接毁掉设备性能。

---

## 3. 设计总纲：一个哲学 + 三条正交的轴

### 3.1 哲学：两区模型（Host / Device）

> **控制流（if / while / for）默认属于 CPU 区；密集的浮点计算与比较属于设备区。
> 两区边界显式、编译期可检查。**

不要追求"任意 SL 代码自动跑 GPU"。CUDA / SYCL / Taichi / Futhark 都已证明该野心不可行。
这条规则正是需求本身（"控制流在 CPU，浮点计算与比较在 GPU/NPU"），本文要做的是
**把它变成语言的一等分区规则，而不是靠库作者自觉**。

### 3.2 两种用户视角（"优雅"的真正来源）

| 视角 | 看到什么 | 需要什么机制 |
|------|---------|-------------|
| **库使用者**（写业务 / 模型） | 一行张量表达式，零心智负担 | 张量表达式 + 广播 + 命名算子 |
| **库作者**（写 Math / Tensor 库） | 精确控制"这段在 CPU 那段在设备"、tile、布局 | 分区标注 + 多目标特化 + 显式调度逃生舱 |

> **与现状的连接**：`Mathf.sl` 的 `sin` 用 `@DllImport` 绑定 C 实现、函数本体作 SL fallback ——
> 这个"绑定优先 + 本体兜底"就是**多目标特化的雏形**。本方案把它从
> 「FFI vs 纯 SL」泛化为「cpu / gpu / npu 多版本 + 自动选择」。

### 3.3 三条轴

| 轴 | 内容 | 章节 |
|----|------|------|
| 轴一 | **分区与着色**（Zone & Coloring） | §4 |
| 轴二 | **数据抽象**（三层类型） | §5 |
| 轴三 | **算子 vs 循环**（以算子派为主） | §6 |

---

## 4. 轴一：分区与着色

### 4.1 显式边界 + 自动着色推导

- 用 `@device`（或重做后的 `@GPU`）标记 kernel 入口。
- **着色推导**：从 device 入口出发，被调用的函数自动染成 device 色；
  同一 `sin()` 生成 host 版与 device 版两份（复用 `Mathf.sl` 的 fallback 机制，只是变成多份并存）。
- **反向约束必须是编译期诊断，不是导出期 Fail**。现状中 device 区内 `CallStatic`
  是导出期硬 Fail（§4），用户只看到"方法回退了"却不知原因。应改为编译期报错：
  「device 区内不能调用 host 函数 `xxx`（其为 host 色，因为 …）」并给出调用链。

### 4.2 跨设备访问是编译错误

张量携带 `device` 归属（`host:0` / `cuda:0` / `npu:0`）。
**跨设备直接访问 = 编译错误**，必须显式 `t.to(cuda:0)` 搬运。

> 隐式拷贝是所有异构语言性能黑洞的来源。把它变成**编译期拒绝**，
> 用户就永远不会被"莫名其妙慢 100 倍"坑到。

### 4.3 调度参数应从语言里拿出来

- 删除 `@GPU` 的 13 个位置参数。
- 默认 `@device` **不写任何调度参数**，由运行时按 shape 推导。
- 需要时用**具名参数**（如 `block=256, tile=[16,16]`）。
- 更进一步：**autotuner** —— 同一 kernel 编译若干 tile 变体，首次运行实测选最优并缓存
  （Triton / TVM 的标准做法）。**让机器选，比让用户手写 `256` 靠谱得多。**

---

## 5. 轴二：数据抽象——三层，各管一件事

| 层 | 类型形态 | 语义 | 存放 | 解决什么 |
|----|---------|------|------|---------|
| 标量 / 小向量 | `Vec3`、`Mat4`（改为 **`data`**） | 值语义 | 寄存器 / 栈 | 图形、物理；消除 GC |
| 多维数组 | `Tensor<T, Rank>` | 句柄 + strided 布局 | host / device 内存 | 通用张量，rank 静态、dim 可动态 |
| 静态形状 | `Tensor<T, [M, N]>` | shape 进类型 | 栈 / 完全展开 | 小矩阵；NPU 最爱的静态图 |

### 5.1 决策 A：Shape 进类型要"可选"，不能强制

rank 必填（静态），dim 可选（动态用 `?`）。与 MLIR 的 `tensor<?x?xf32>` 一一对上。
强制静态 shape 会让动态 shape 的模型无法表达，也会让代码爆炸。

### 5.2 决策 B：Layout 是一等参数，不是注释

`Tensor<T, Rank, Layout>`。NPU 对布局极其敏感（NCHW / NHWC / 昇腾 NZ 分形格式）。
布局转换由编译器**自动插入且可见**（在 IR 中表现为 `linalg` 的 transpose / copy），
而不是让用户手写。

### 5.3 决策 C：元素类型限制为 POD

张量元素只能是数值 POD（含已实现的 `Float8_E4M3 / Float8_E5M2 / Float16 / Float16_Brain`、int8）。
**对象元素在编译期拒绝** —— GPU / NPU 只认 POD，放入对象必然一路退化。

### 5.4 顺带修复：小向量改为值类型

把 `Float32_3 / Float32_4 / Float32_3x3 / Float32_4x4` 从 `class` 改为 **`data`**
（`target.md` 第 25 条中 `data` 本就是"栈上生成、相当于结构体"的值语义类型，正好对位）。
改完立刻获得：

- 小向量运算不再进 GC；
- AOT 可标量替换为若干寄存器（配合逃逸分析）；
- 值类型能按值传进 kernel（配合 `MLIR_AOT_实现与待办.md` §7.9 的 GPU 结构体成员访问）。

---

## 6. 轴三：算子 vs 循环——主张要鲜明

两条路线：

- **(A) 循环派**（Taichi / Futhark / CUDA）：用户写 `parallel for`，编译器分析并行性。
- **(B) 算子派**（NumPy / XLA / Triton）：用户写整体张量表达式，编译器负责 tiling / fusion。

### 6.1 主张：**(B) 为主，(A) 为逃生舱**

1. **(B) 写法最像数学**，这就是需求要的"优雅"。
2. **NPU 本质上是"大算子加速器"**：昇腾 / TPU / 寒武纪擅长 `matmul`、`conv` 这类
   **粗粒度命名算子**，对散落的标量循环几乎无能为力。
   **给 NPU 一堆 `for` 只能退化成极慢的通用执行；给 `linalg.matmul` 才能跑满。**
   这一点决定了 NPU 支持必须走算子派。
3. **(A) 仍然需要**，作为不规则算法（稀疏、图、粒子、光线追踪）的逃生舱，
   编译成 `scf.forall` / `scf.parallel`。

### 6.2 语义 → MLIR 映射表（核心技术内容）

| 用户写的东西 | 发射成 | 意义 |
|-------------|--------|------|
| 张量 `+ - * /`、比较、数学函数（广播语义） | **`linalg.generic`**（body 为标量 `arith`） | 天然可融合、可 tiling、可向量化、可 GPU 映射 |
| 沿轴 `sum / mean / max / min / any / all` | `linalg.reduce` | 三端均有成熟 lowering |
| `A @ B`（matmul）、`conv`、`pool` | `linalg.matmul` / `linalg.conv_2d_*` 等 **named op** | **NPU 后端直接认领** |
| `parallel for` | `scf.forall` / `scf.parallel` | CPU 多线程 + GPU 线程映射 |
| if / while / for（Host 区） | `scf.if` / `scf.for` / `scf.while` | 普通控制流 |

### 6.3 关键建议：不要自造太多 dialect

直接发 `func` + `tensor` / `memref` + `linalg` + `scf` + `arith`。
只在 SL 特有语义（对象、GC、异常、协程、窄浮点）处自定义 op，而这些**只在 host 路径出现**。

收益是决定性的：**融合、tiling、向量化、GPU 映射、NPU 认领全部是官方 pass，
一行都不用写；直接发 linalg 就等于同时拿到了 GPU 和 NPU。**

### 6.4 NPU 接入点

- **不要在 SL 里打 NPU 指令**。路径为：`linalg`（或更受限、更硬件友好的 **TOSA**）→
  厂商 IR（CANN / ACL / 寒武纪 / …）。昇腾、ARM Ethos 都认 TOSA 这一层。
- **能力分层**：静态 shape 的粗算子 → NPU；动态 shape / 复杂控制流 → 回退 GPU / CPU。
- **决策放在运行时的 planner（成本模型），不要编译期硬编码。** 编译期生成多个变体，
  运行时按 shape / 数据量 / 设备可用性选择。这正是现有 `aot / vm / bridge` 三态机制的推广：
  从「要么 AOT 要么 VM」推广为「cpu / gpu / npu / vm 四选一」。

---

## 7. 控制流 vs 浮点计算与比较：三条规则

### 规则 1：元素级的"计算与比较"不要写 if，写 select / where

在张量上写 `if (x > 0) …` 是灾难 —— GPU 分支发散、无法向量化、NPU 直接不支持。

- 语言提供 `select(cond, a, b)` / `where(cond, a, b)`，或让三元表达式对张量自动走 select。
- 提供无分支原语：`min / max / clamp / sign / step / mix`。
- **编译器做"分支消除"**：对两侧均为纯计算 / 赋值的 if-else，自动改写为 `arith.select`
  并**给出诊断**（"此处 if 已消除为 select；若确为发散控制流请标注 `@varying`"）。
  用户写起来仍是 `if`，发射出来是 select。

### 规则 2：kernel 内控制流必须区分 uniform / varying

| 类型 | 含义 | GPU 处理 | CPU 处理 |
|------|------|---------|---------|
| **uniform** | 条件对所有元素 / 线程相同 | 保留真分支，高效 | 正常分支 |
| **varying** | 条件依赖元素索引或数据 | 必须 mask 化 / select 化 | 正常分支 |

- 主路：**编译器做 uniform 分析**（条件是否依赖并行索引 / 元素数据）。
- 判定不了时：**保守 select 化 + 性能警告**（不是错误）。
- 提供显式断言供用户覆盖：`uniform if` / `varying if`（ISPC / SYCL 的做法）。不强制使用。

### 规则 3：循环模式归约成算子

`for` 累加 → `reduce`；`for` 前缀和 → `scan`。提供沿轴 `sum / prod / min / max / any / all`
及通用 `reduce(op)`。三端均有成熟实现（`linalg.reduce` + GPU 的 shuffle reduce），
用户不必手写归约循环。

---

## 8. 执行模型：融合与异步

### 8.1 主推：编译期静态融合

靠 MLIR 的 `linalg` 元素级融合 pass（`--linalg-fuse-elementwise-ops`）。
`relu(a * b + c)` 编译成**一个** `linalg.generic`，而不是三个 kernel。

- 优点：可预测、零运行时开销、调试友好。
- 代价：跨函数边界融合失效。可接受 —— 库内部本就应在同一函数内写完表达式。

### 8.2 逃生舱：显式图捕获块

提供 `capture { ... }` 块：块内表达式延迟物化，出口处融合为一个 kernel，并可缓存 / AOT 化。

- 这是 JAX / TF graph 的思路，解决跨函数融合与动态 shape 缓存。
- **不要默认开懒执行**。默认 eager（好调试、语义直观），图模式显式开启。
  TF1 的教训就是默认懒执行导致调试地狱。

### 8.3 异步：复用已有协程，不发明新语法

设备执行天然异步。语言已有 `spawn / await` + `Channel`（`md/syntax/coroutine.md`）。

- kernel launch 返回 `Task`（或 `DeviceFuture`），`await` 即同步。
- **零新语法**，且天然支持 CPU 数据准备与 GPU 计算 overlap（流水线）——
  这是"运行速度更合理"的重要一环，写起来却只有 `await` 两个字。

> **一致性提醒**：`AGENT.md` §10 第 12 条记录的
> `waitAll(params Array<Task>)` 与 `waitAny(params Task[])` 参数形式不一致，
> **不要带进新库**。

---

## 9. IR 架构改造（最关键的一步）

```
SL 源码
  │  Front（已有，不动）
  ▼
SLIR（栈机）──────────────────────────► CVM 解释（保留，作为 golden reference）
  │
  ├──[新增] SLTIR：SSA + region + 显式循环 + tensor/memref   ← 主力优化层
  │        （可直接就是 MLIR 高层：func + tensor + linalg + scf + arith）
  ▼
MLIR 高层
  │  通用优化：linalg 化 → 融合 → tiling → vectorize → bufferize
  ├─→ CPU：scf.parallel + vector → LLVM
  ├─→ GPU：gpu.launch + nvvm / rocdl / spirv
  └─→ NPU：linalg / TOSA → 厂商 IR
```

要点：

1. **SLIR 不动**。它是 CVM 解释路径与"失败即回退"的基石，动它会毁掉现有全部验证。
2. **新路径只对被标注的 kernel / 张量代码生效**，其余照旧。两条路并存，渐进迁移。
3. **"失败即回退"是这套架构最大的安全网**，必须保留并推广到 kernel 层：
   新路径任一步失败（发射失败 / 形状不兼容 / 设备不存在）→ 回退 CVM 解释。
   **这是能够激进重构的唯一前提。**

---

## 10. 语法优雅化建议（仅形态，不含实现）

| 目标 | 建议形态 | 依据 / 理由 |
|------|---------|------------|
| 类型别名 | `f32[m,n]`、`Vec3`、`Mat4` | 已有 `@Nickname("float3")`，推广到张量 |
| 索引 | `A[i, j]`、`A[0:m, :]` | 取代 `A[i][j]` / `A[i*N+j]` |
| 逐元素 vs 矩阵乘 | `A * B` 逐元素，`A @ B` 矩阵乘 | NumPy 已验证的区分方式，歧义最小 |
| 广播 | `A + 1.0f`、`A * B`（形状自动广播） | 数学直觉 |
| 归约 | `A.sum(axis = 0)` | 取代手写归约循环 |
| 无分支选择 | `select(c, a, b)` / `where(c, a, b)` | 取代元素级 if |
| 并行逃生舱 | `parallel for i in 0..M` | 一个修饰符，不是新控制流体系 |
| 多目标特化 | 同一函数按 cpu / gpu / npu 提供多实现 + 通用兜底体 | 泛化 `Mathf.sl` 的 `@DllImport` + fallback 模式 |

**核心原则：尽量不发明新关键字。** 用 attribute + 运算符重载 + 已有协程 / 模板，可覆盖 80%。
真正值得新增的只有 `parallel` 一个修饰符，外加可选的 `where / select`。

> **禁止用宏做算子抽象。** `md/syntax/marco.md` 中的宏是纯文本替换（`love` → `if`）。
> 用它构建算子库会彻底失控（无法类型检查、无法报错定位、组合爆炸）。
> 应使用**模板 + attribute + 编译期函数**替代。

---

## 11. 性能落地清单（按性价比排序）

| # | 动作 | 针对的痛点 | 预期收益 |
|---|------|-----------|---------|
| 1 | 小向量 / 矩阵 `class` → `data`（值类型）+ 标量替换 / 逃逸分析 | `Float32_3` 每次运算进 GC | CPU 数学库数量级提升 |
| 2 | **F32 加宽的 f64 不许泄漏到设备端**：AOT / GPU 路径存真 f32 位，CVM 解释路径保留加宽 | §2.1 的 F32 域约定毁 GPU / NPU 性能 | 设备端吞吐数倍 |
| 3 | 多维索引 `A[i, j]` 取代手写 `a[i * K + j]` | 扁平索引让 affine 分析半残 | 解锁向量化 / 越界消除 |
| 4 | 元素级表达式发射成 `linalg.generic` + 静态融合 | 现在每个算子一个 kernel | kernel 数下降一个量级 |
| 5 | named op（matmul / conv / reduce）+ 多后端分发 | NPU 只认粗算子 | NPU 可用 |
| 6 | autotune tile / block | `@GPU` 13 个魔数 | 去掉人工调参 |
| 7 | 窄浮点**按目标选实现** | 现有软实现约 90 行 MLIR / 条 | GPU 大幅加速 |
| 8 | 布局选项（含 SoA）+ 自动布局转换 | NPU 布局敏感 | NPU / 访存收益 |

### 11.1 第 7 条展开：双实现 + 按目标选择

现有窄浮点采用**整数位运算软实现**，理由是真实的（避免 `compiler-rt` 链接失败），
且做到了**位级精确对齐 CVM**（`MLIR_AOT_实现与待办.md` §6），该资产必须保留。
但它不该是唯一实现。应做成：

- **CPU**：保留位精确软实现（语义对齐 CVM）；
- **GPU / NPU**：用原生 `arith.truncf / extf`（性能）；
- 提供"严格位精确模式"开关，供双跑对拍与验证。

> 这个「**双实现 + 按目标选择**」的模式，可推广用于解决第 2 条
> （F32 加宽）等一系列"为对齐解释器语义而牺牲设备性能"的问题。

---

## 12. 落地路线（6 步，每步独立可验收、可回退）

| 步 | 内容 | 验收标准 |
|----|------|---------|
| **0** | 值类型化：小向量 / 矩阵改 `data`；AOT 支持 struct 按值传；逃逸分析 | CPU AOT 数学库 vs 现状：数量级提升；GPU 路径解锁 struct |
| **1** | 新增 SLTIR（或直接 MLIR 高层）发射路径，与栈机并存 | `AOTGPUTest` 的 matmul 走新路径，性能不低于现状 |
| **2** | `Tensor` 类型 + 元素级算子 + `linalg.generic` + 静态融合 | `relu(a*b+c)` 生成 1 个 kernel；CPU 上出现 vectorization |
| **3** | 命名算子（matmul / conv / reduce）+ 多后端分发 | matmul 在 GPU 接近手写；NPU 后端能认领 `linalg.matmul` |
| **4** | autotune + 图捕获块 | `@device` 无参数也能跑出合理性能 |
| **5** | NPU 后端（TOSA / 厂商 IR）+ 成本模型 + 运行时设备选择 | 同一份代码：有 NPU 走 NPU，无 NPU 回退 GPU / CPU |

每一步保持**双跑对拍**（CVM 解释版 vs 新路径），沿用
`MLIR_AOT_DESIGN.md` §6.5 已有的 golden reference 约定。

---

## 13. 需要避开的 7 个坑

| # | 坑 | 说明 |
|---|----|----|
| 1 | 在栈机 SLIR 上做并行化 | 信息已丢失，做不出来 |
| 2 | 用宏做算子抽象 | 纯文本替换，必然失控 |
| 3 | 让 F32 的 f64 加宽泄漏到 GPU / NPU | 会毁掉设备吞吐 |
| 4 | `data`（值）与 `class`（引用）在张量元素中混用 | 设备只认 POD，应编译期拒绝 |
| 5 | 把调度参数写死进语言 | 13 参数的 `@GPU` 是反面教材；改为具名 + 默认 + autotune |
| 6 | 一开始就追求"全语言自动上 GPU" | 两区模型是唯一现实路线 |
| 7 | 让"对齐 CVM 语义"一刀切牺牲设备性能 | 改为按目标选实现 + 严格模式开关（窄浮点已证明可行） |

---

## 14. 风险与对策

| # | 风险 | 对策 |
|---|------|------|
| R1 | 新增 SLTIR 与栈机双轨维护成本 | 新路径只覆盖被标注代码；未覆盖部分始终走 SLIR；失败即回退 |
| R2 | 直接发 `linalg` 遇到 SL 语义无法表达的算子 | 保留 `parallel for` 逃生舱 + SL 自定义 op（仅 host 路径） |
| R3 | NPU 厂商工具链不可得 / 接口不稳定 | 以 TOSA / linalg 为中立层，厂商侧做成可插拔后端；缺失时回退 GPU / CPU |
| R4 | autotune 首次开销大、结果不稳定 | 结果缓存到模块级；提供固定 tile 的具名参数覆盖 |
| R5 | 张量表达式引入隐式分配 / 隐式拷贝 | 跨设备访问编译期拒绝；显式 `to(device)`；融合消除中间临时 |
| R6 | 双实现（位精确 vs 原生）语义漂移 | 严格位精确模式 + 穷举对拍（沿用窄浮点现有验证资产） |
| R7 | 对象模型（`class`）与张量 POD 约束冲突 | 元素类型编译期限制为 POD；小向量改 `data` |
| R8 | 回归失控 | 保留 CVM 解释为 golden reference，自动化双跑对拍 |

---

## 15. 附录：相关文件索引

| 文件 | 与本文的关系 |
|------|-------------|
| `md/design/MLIR_AOT_DESIGN.md` | 三期后端总设计（SLIR → MLIR → LLVM → exe） |
| `source/Front/Export/MLIR/MLIR_AOT_实现与待办.md` | 发射器现状、指令实现状态、GPU 限制清单、路线图 |
| `test/SpecialTest/AOTGPUTest.sl` | 现有 GPU 写法样例（三重 for + 13 参数 `@GPU`） |
| `source/Front/Lib/Std/GPU/GPU.sl` | `@GPU` attribute 定义（13 参数） |
| `source/Front/Lib/Math/Float32_3.sl` | 小向量现状（`class` + 运算符重载 + `@Nickname`） |
| `source/Front/Lib/Math/Mathf.sl` | `@DllImport` + fallback 模式（多目标特化雏形） |
| `md/syntax/operator.md` | 运算符重载约定方法表 |
| `md/syntax/number.md` | 数值类型、`Num`、字面量后缀 |
| `md/syntax/template.md` | 模板 / 泛型 |
| `md/syntax/attribute.md` | attribute 定义与使用 |
| `md/syntax/bind.md` | `bind` 与 `data` 展开语义 |
| `md/syntax/coroutine.md` | 协程权威文档（异步复用基础） |
| `md/syntax/marco.md` | 宏（**禁用于算子抽象**） |
| `target.md` | 第 25 条：`data` 值语义；第 30 条：floatN / matrixN 快捷计算 |
