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
| 最优雅的来源 | 两种用户视角分离：库**使用者**只写张量表达式（零心智负担）；库**作者**才需要 `Compile.*` 注册与调度控制 |
| 最先该做的两件事 | ① 对 `Float32_3` 这类小 class 做 **POD 布局特化** + 逃逸分析 / SROA 拆箱　② 阻止 F32 的 f64 加宽 ABI 泄漏到设备端 |
| 要不要 `data` | **不需要**。`Float32_3` 保持 `class` 原样，由编译器在布局阶段**特殊处理**成 POD；`data` / `bind` 只作为可选的显式手段（§5.4） |
| `@` 的使用约束 | `@` 是 SL 的 **attribute 专用关键符号**（且你有其它用途），本设计**一律不用 `@`**：编译期指令函数化（`Compile.*`），矩阵乘写作 `A · B` / `matmul(A,B)`（见 §10.1） |
| HAL（硬件抽象层） | 新增**统一运行时底座**：设备发现 / 内存 / 命令提交 / 同步原语；后端（CPU / CUDA / ROCm / NPU / VM）各自实现 HAL 接口；编译器只面向 HAL，回退在 HAL 层统一做（§9.1） |
| 安全网 | 保留并强化现有「**失败即回退 CVM**」机制——这是能激进重构的唯一前提 |
| 理想形态 | **§16 理想形态 Demo 集**（21 组：POD 布局特化 / 张量 / 两区 / 控制流 / 并行 / 异步 / 多目标特化 / autotune / 图捕获 / 混合精度 / 布局 / 库作者 / 库使用者 / 诊断 / 可移植） |

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
| 分区标注雏形 | `@GPU` / `@AOT`（`Lib/Std/GPU/GPU.sl`、`Front/Export/MLIR/`） | 重做为**编译期函数** `Compile.kernel()` + 自动着色（不再新增 `@`，§10.1） |
| 多实现兜底模式 | `Mathf.sl` 的 `@DllImport` + SL fallback 本体 | 泛化为 **cpu / gpu / npu 多目标特化** |
| POD 数据记录 | `data`（`md/syntax/data.md`：无函数、无 `extends`、`==` 按结构 + `m_MemberDataBuffer` 内容比较、连续数据缓冲） | **张量元素 / 设备侧布局载体**（天然 POD） |
| 行为与布局分离 | `bind`（`md/syntax/bind.md`：`class` bind `data` 自动注入成员、get/set 访问器、`_init_` 重载） | 小向量「有方法」与「能进设备」兼得 |
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

- 用编译期函数 **`Compile.kernel( fn )`** 注册 kernel 入口（**不用 `@`**，见 §10.1）。
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
- 默认 `Compile.kernel( fn )` **不写任何调度参数**，由运行时按 shape 推导。
- 需要时用**具名参数**（如 `block=256, tile=[16,16]`）。
- 更进一步：**autotuner** —— 同一 kernel 编译若干 tile 变体，首次运行实测选最优并缓存
  （Triton / TVM 的标准做法）。**让机器选，比让用户手写 `256` 靠谱得多。**

---

## 5. 轴二：数据抽象——三层，各管一件事

| 层 | 类型形态 | 语义 | 存放 | 解决什么 |
|----|---------|------|------|---------|
| 标量 / 小向量 | `Vec3` / `Mat4`：**`class`（行为）+ `bind` 一个 POD `data`（布局）** | 行为与布局分离；AOT 路径拆箱为寄存器 | 寄存器 / 设备寄存器 | 图形、物理；消除 GC |
| 多维数组 | `Tensor<T, Rank>` | 句柄 + strided 布局 | host / device 内存 | 通用张量，rank 静态、dim 可动态 |
| 静态形状 | `Tensor<T, [M, N]>` | shape 进类型 | 栈 / 完全展开 | 小矩阵；NPU 最爱的静态图 |

### 5.1 决策 A：Shape 进类型要"可选"，不能强制

rank 必填（静态），dim 可选（动态用 `?`）。与 MLIR 的 `tensor<?x?xf32>` 一一对上。
强制静态 shape 会让动态 shape 的模型无法表达，也会让代码爆炸。

### 5.2 决策 B：Layout 是一等参数，不是注释

`Tensor<T, Rank, Layout>`。NPU 对布局极其敏感（NCHW / NHWC / 昇腾 NZ 分形格式）。
布局转换由编译器**自动插入且可见**（在 IR 中表现为 `linalg` 的 transpose / copy），
而不是让用户手写。

### 5.3 决策 C：元素类型必须"可 POD 特化"（但不强制改用 `data`）

张量元素必须能落到 **POD 布局**：

- 数值标量（含已实现的 `Float8_E4M3 / Float8_E5M2 / Float16 / Float16_Brain`、int8 等）；
- 满足 §5.4.1 判定条件的 **`class`**（如 `Float32_3`）—— 特化后即为紧凑 POD；
- `data`（天然 POD 记录）。

- ✅ `Tensor<Float32, 2>`　✅ `Tensor<Float32_3, 1>`（特化后 = 3×`f32` 紧凑布局）
- ❌ `Tensor<SomeComplexClass, 1>`（含引用成员 / 有继承链 / 被反射依赖）——
  编译期拒绝，并**指出具体哪一条判定未通过**

**为什么**：GPU / NPU 只认 POD。但**不必因此强迫用户改用 `data`** ——
在布局阶段对 `class` 做特化（§5.4.1）即可，用户的类型定义与写法都不变。

### 5.4 小向量：**不是"改成 data"，而是"class bind data"**

> **结论先行**：**完全可以不引入 `data`，直接沿用 `Float32_3`**。
> 正确的做法是在**布局阶段对这类小 class 做特殊处理**，而不是改类型。

### 5.4.1 POD 布局特化（主方案）

编译器在 Meta / IR 阶段识别一类**"可 POD 化的小 class"**，并为其分配紧凑的 POD 布局：

| 判定条件（全部满足才特化） | 理由 |
|---|---|
| 字段全部是标量 POD（数值 / bool / char），无引用类型成员 | 布局可按字节搬运 |
| 无 `extends` 继承链（或继承链上无虚方法） | 不需要对象头里的多态信息 |
| 未被 `as` / `cast` / 反射 / `equals` 引用语义依赖 | 拆箱后不会破坏语义 |
| 逃逸分析判定为**不逃逸**（或仅在 AOT 路径内流转） | 拆箱安全 |

满足则在 AOT / kernel 路径**拆箱（unboxing）+ 标量替换（SROA）**：
对象在生成代码里退化为若干寄存器（如 `Float32_3` → 3 个 `f32` / `vector<3xf32>`），
**无堆分配、无 GC、无虚调用**。

- **用户代码零改动**：`Float32_3` 还是 `class`，`a + b` 照写；
- **语义零改动**：解释路径（CVM）完全不受影响，仍按对象处理；
- **顺带解开** GPU 路径「禁止 Struct / ObjRef 槽位」的限制（`MLIR_AOT_实现与待办.md` §4）——
  进设备的是特化后的 POD 布局，不是 `ObjRef`；
- 未满足条件的实例**自动退回对象语义**，由逃逸分析保守保证正确性。

> 这正是你说的"**对这种类型布局的时候，特殊处理下**"。
> 与 C# 的 `struct` / JVM 的标量替换 / Julia 的 `isbits` 是同一思路：
> **类型是普通类型，优化发生在编译期布局决策上。**

### 5.4.2 `data` / `bind` 作为可选手段（非必需）

若希望**显式**声明某类型的 POD 布局，可用已有机制（都不是必须）：

| 手段 | 说明 |
|---|---|
| 直接沿用 `class` | 推荐。靠 §5.4.1 的自动特化 |
| `class X bind SomeData` | 用 `bind`（`md/syntax/bind.md`）把 POD `data` 挂到 class 上；`data` 本身仍是纯数据记录、无函数（`md/syntax/data.md`） |
| 编译期函数 `Compile.pod( X )` | 显式断言"该类型按 POD 布局"，未满足时编译期报错（§10.1） |

> `data` 的定位（无函数、无 `extends`、`==` 按结构 + `m_MemberDataBuffer` 内容比较）
> 使其天然是 POD 记录，但它**不能承载运算符重载**，因此**不是小向量的必需品**。

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
| `A · B`（matmul）、`conv`、`pool` | `linalg.matmul` / `linalg.conv_2d_*` 等 **named op** | **NPU 后端直接认领** |
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
  并**给出诊断**（"此处 if 已消除为 select；若确为发散控制流请标注 `varying`"）。
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
  │
  ▼ 各后端降到的目标，都经 HAL 统一接口（见 §9.1）
HAL（硬件抽象层）──────── 统一原语：设备发现 / 内存 / 命令提交 / 同步 / 特性查询
  ├─→ CpuHal  ：线程池 + vector + FMA            → LLVM
  ├─→ CudaHal  ：CUDA runtime（nvvm / ptx）
  ├─→ RocmHal  ：ROCm（rocdl / hsaco）
  ├─→ NpuHal   ：厂商 SDK（CANN / ACL / 寒武纪）→ 经 TOSA
  └─→ VmHal    ：CVM 解释器（兜底，永远存在）
```

要点：

1. **SLIR 不动**。它是 CVM 解释路径与"失败即回退"的基石，动它会毁掉现有全部验证。
2. **新路径只对被标注的 kernel / 张量代码生效**，其余照旧。两条路并存，渐进迁移。
3. **"失败即回退"是这套架构最大的安全网**，必须保留并推广到 kernel 层：
   新路径任一步失败（发射失败 / 形状不兼容 / 设备不存在）→ 回退 CVM 解释。
   **这是能够激进重构的唯一前提。**

### 9.1 HAL：硬件抽象层（运行时统一底座）

> **为什么需要 HAL**：与其让编译器（SLTIR→MLIR）直接调用 CUDA / ROCm / 各家 NPU SDK，
> 不如在「生成的设备代码」与「具体硬件驱动」之间插一层统一抽象。
> **编译器只面向 HAL 编程**，后端可插拔、可独立测试、可热替换、可缺失。

**HAL 的职责（统一原语）**：

| 原语族 | 语义 | 对应 SL 语法 |
|--------|------|-------------|
| 设备发现 | 枚举可用设备、按优先级选最优 | `Device.list()` / `Device.best(npu, gpu, cpu)` |
| 内存 | 分配 / 释放 / 跨设备拷贝 | `Tensor.zeros(..., device=)` / `t.to(dst)` |
| 命令提交 | 把一个 kernel 排入设备的执行队列 | `launch kernel[grid, block](...)` |
| 同步 | 流 / 事件 / 等待 | `await task`（复用协程 `spawn / await`） |
| 特性查询 | 算力、显存、支持的 dtype / 布局 | `Device.capability` / `Device.memory` |

**后端实现（各自实现 HAL 接口）**：

| 后端 | 实现 | 落到 |
|------|------|------|
| `CpuHal` | 线程池 + `vector` + FMA | LLVM（原生 CPU） |
| `CudaHal` | CUDA runtime | nvvm / ptx |
| `RocmHal` | ROCm | rocdl / hsaco |
| `NpuHal` | 厂商 SDK（CANN / ACL / 寒武纪） | 经 TOSA → 厂商 IR |
| `VmHal` | CVM 解释器 | **兜底，永远存在** |

**HAL 与本设计其它概念的关系（关键：复用现有并发设施，不另造"等待"）**：

- **多目标特化 = 为某 HAL 后端注册特化函数**：`Compile.target( Device.npu, gemmNpu )`
  即「HAL 选 `NpuHal` 时用 `gemmNpu` 变体」；`Compile.fallback` 即 `VmHal` 兜底。
- **HAL 后端就是一个长生命周期 worker isolate**：`CpuHal` / `GpuHal` / `NpuHal` / `VmHal`
  各自是独立 isolate（独立堆 / 独立 GC / 故障隔离）——这恰好是"设备执行"最自然的载体：
  NPU 驱动崩溃不应拖垮主程序。运行时按 `module.json` 的设备枚举**自动起**这些 isolate，用户无感。
- **"等待"在 SL 里只有一套语义 = 协程 `await`**：`launch(kernel)` 向 HAL isolate 的命令端口
  `sp.send(cmd)`（异步、永不阻塞，复用 `SendPort`），执行完经 `SendPort` / `Channel` 回传；
  `launch` 返回一个可 `await` 的 `Task`，`await task` 即 `Coroutine.awaitHandle` 挂起当前协程
  等待设备完成。**HAL 完全落在 `Isolate` + `Coroutine` + `Channel` + `SendPort` 体系上，
  零新并发原语（除受控的"设备特化块" `@gpu{...}` 语法糖外，不新增 `@` 指令/关键字，详见 §10.2）。**
- **跨设备张量搬运用 `TransferableData`（零拷贝），不走 `SendPort` 深拷贝**：张量是大型共享数据，
  深拷贝跨设备成本爆炸；设备间移动应走所有权转移（发送后源失效），与 Isolate 的
  `TransferableData` 机制同构。`t.to(device)` 语义 = 把张量底层 buffer 转移给目标 HAL isolate。
- **失败回退在 HAL 层统一做**：某后端不可用 / 不支持某 shape → 选下一个 HAL isolate，
  最终落到 `VmHal`。与「SLIR→CVM 永远兜底」一致，只是从二选一变成**多后端链式回退**。
- **复用 CVM 作兜底**：`VmHal` 直接复用现有解释器，无需重写。

> **待定决策（需你拍板）**：
> - **HAL isolate 隐式 vs 显式**：默认**隐式**——运行时按设备自动起 HAL isolate，用户只写
>   `Device.best + Compile.target + launch + await`；与现有 Isolate 模型无冲突、用户零感知。
>   备选**显式**：允许用户 `Isolate.spawn` 出一个设备 worker（更灵活但更重）。
> - **`launch` 返回类型**：默认复用现有 `Task`（协程句柄），因 `await` 语义完全一致；
>   若设备事件需携带额外元数据（timing / 功耗）再升级为 `DeviceTask`（不影响 `await` 语法）。

**HAL 的接入约定（接口预留，不在本设计范围）**：

- 编译器后端只调用 HAL 接口，不直接 `#include` 厂商头文件；
- 厂商后端以**插件 / 动态库**形式提供，运行时按设备枚举结果加载；
- 同一份 `module.json` 可在只有 CPU 的机器上只加载 `CpuHal` + `VmHal`，
  在有 NPU 的机器上额外加载 `NpuHal`——**代码不变，底座可变**。

---

## 10. 语法优雅化建议（仅形态，不含实现）

| 目标 | 建议形态 | 依据 / 理由 |
|------|---------|------------|
| 类型别名 | `f32[m,n]`、`Vec3`、`Mat4` | 已有 `@Nickname("float3")`，推广到张量 |
| 索引 | `A[i, j]`、`A[0:m, :]` | 取代 `A[i][j]` / `A[i*N+j]` |
| 逐元素 vs 矩阵乘 | `A ⊙ B` 逐元素，`A · B` 矩阵乘（或 `matmul(A,B)`） | `@` 在 SL 另有用途，矩阵乘**不用 `@`**；改用 `·` / `matmul` |
| 广播 | `A + 1.0f`、`A * B`（形状自动广播） | 数学直觉 |
| 归约 | `A.sum(axis = 0)` | 取代手写归约循环 |
| 无分支选择 | `select(c, a, b)` / `where(c, a, b)` | 取代元素级 if |
| 并行逃生舱 | `parallel for i in 0..M` | 一个修饰符，不是新控制流体系 |
| 多目标特化 | `Compile.target( Device.npu, gemmNpu )` + `Compile.fallback( gemmGeneric )` | 泛化 `Mathf.sl` 的 `@DllImport` + fallback 模式，**但用函数而非 attribute** |

### 10.1 编译期指令一律函数化（**不再新增 `@` 指令**）

> **约束**：`@` 在 SL 中是 **attribute 的专用关键符号**
> （`md/syntax/attribute.md`：`@MyAttr(...)`；现有 `@Nickname` / `@AOT` / `@GPU` / `@DllImport` 均已占用）。
> 本设计**不再往 `@` 命名空间里加任何新指令**，避免与用户自定义 attribute 混淆。

> **约束（补充：运算符也不用 `@`）**：不仅编译期指令不用 `@`，**张量代码里的运算符也不许用 `@`**。
> 你明确说过 `@` 在 SL 中有其它作用，写 `A @ B` 会与你的用法冲突。
> 因此矩阵乘写作 **`A · B`**（点运算符 `·`，数学直觉最强）或 **`matmul(A, B)`**；
> 逐元素乘写作 **`A ⊙ B`**（`⊙`，Hadamard）。若实现层不便支持 `·` / `⊙` 字形，
> 直接退化为 `matmul` / `elementwise_mul` 函数即可——**绝不碰 `@`**。

改为**编译期函数调用**：函数照常定义（就是普通 `static` 方法），
再用一行 `Compile.*` 注册。这些调用在**编译期求值、不产生运行时代码**，
可写在模块级，也可写在 project 的 `_before_( metaType type )` 编译前钩子里
（该钩子已在 `md/syntax/operator.md` 中定义）。

| 意图 | 函数化写法 | 取代的写法 |
|------|-----------|-----------|
| 声明 kernel | `Compile.kernel( ln_kernel )` | ~~`@device`~~ |
| kernel 调参（可选） | `Compile.kernel( conv, tile = [16, 16], block = 256 )` | ~~`@GPU(0,0,0,0,0,0,0,256,…)`~~ |
| autotune | `Compile.autotune( conv_kernel )` | ~~手写 13 个魔数~~ |
| 多目标特化（函数式） | `Compile.target( Device.npu, gemmNpu )` | ~~`@target(npu)`~~ |
| 多目标特化（声明式糖） | `kernel matmul { ... } @gpu { ... } @npu { ... }` | ~~多个 `@target` 分散写~~ |
| 通用兜底 | `Compile.fallback( gemmGeneric )` | ~~无标注兜底体~~ |
| POD 布局断言 | `Compile.pod( Float32_3 )` | ~~`@ValueType` / `@Pod`~~ |
| 精度策略 | `Compile.precision( gemmMp, storage = Float16, accumulate = Float32 )` | ~~`@Precision(...)`~~ |
| 图捕获 | `capture { ... }` 块 | （关键字，非 `@`） |

**为什么函数化更好**：

1. **不污染 `@`** —— `@` 保持 attribute 语义单一；
2. **参数可读** —— 具名参数取代 13 个位置魔数；
3. **可组合** —— 函数调用可写在 `_before_` 钩子里，能按条件/循环批量注册；
4. **可诊断** —— 参数写错是普通的函数调用错误，报错位置准确；
5. **函数是一等值**（`Func<>` / 匿名闭包，见 `STREAM_DESIGN.md` 的约定），
   注册就是把函数值交给编译器，无需新的语法类别。

**核心原则：尽量不发明新关键字，更不新增 `@` 指令。**
用运算符重载 + 已有协程 / 模板 + `Compile.*` 编译期函数，可覆盖全部需求。
真正值得新增的关键字只有 **`parallel`**（循环修饰符）与 **`capture`**（图捕获块），
外加可选的 `where / select` 与 `launch`（kernel 启动）。

> **受控例外：设备特化块 `@<tag> { ... }` 不算 `@` 指令。**
> 上文"不再新增 `@` 指令"针对的是 **attribute 类** `@Name(...)`（仅贴元数据、无函数体）。
> 而 `@gpu { ... }` / `@arm_npu { ... }` 这种**带函数体 `{ }` 的写法**是**设备特化块**——
> 它本身是一段"该 kernel 在某设备上的实现体"，等价于 `Compile.target(Device.gpu, body)`，
> 与 attribute 语法形态不同（`{ }` vs `( )`），**不污染 attribute 命名空间**。
> 它只是声明式语法糖，标签→后端的真实映射在 jsonc 里（见 §10.2）。矩阵乘仍**不用 `@`**（用 `·`/`matmul`），
> 因为那是运算符、与"特化块"无关。

> **禁止用宏做算子抽象。** `md/syntax/marco.md` 中的宏是纯文本替换（`love` → `if`）。
> 用它构建算子库会彻底失控（无法类型检查、无法报错定位、组合爆炸）。
> 应使用**模板 + attribute + 编译期函数**替代。

---

### 10.2 统一设备标签 `@gpu` / `@cuda` / `@csharp` / `@asm` / `@arm_npu` 与 jsonc 配置

> **核心主张**：用户写的 `@gpu { ... }` 等只是**同一 kernel 的 per-device 特化体**（声明式糖），
> 与 §9.1 的 `Compile.target(Device.gpu, body)` **完全等价**；真正"标签→哪个 HAL 后端 / 哪种实现语言 /
> 几个核心 / 回退顺序"由 **jsonc 配置**决定。**调用端永远只写 `launch` + `await`，不感知 isolate / 分区 / 跨设备搬运。**

**标签语义（一份语义，多份实现）**：

```
kernel matmul(A, B) {            # 主实现：纯 SL，兜底（等价于 Compile.fallback）
    return A · B
} @gpu {                         # GpuHal 特化体（SL 写的 GPU 核函数，编译器降 PTX）
    # gpu 核函数体（grid/block 由 launch 的 split 推导）
} @cuda {                        # GpuHal 的另一种落地：直接写 CUDA C（impl=cuda-c）
    // __global__ void matmul(...) { ... }
} @arm_npu {                     # NpuHal(arm) 特化体
    # arm npu 指令体
} @csharp {                      # CpuHal 的高性能路径：调 C# BLAS（impl=csharp-ffi）
    // 互操作 C# 代码
} @asm {                         # CpuHal 的极致路径：CPU 汇编落地（impl=asm）
    # 汇编
}
```

**jsonc 配置（把"标签"落进"后端"，运行时按可用设备选）**——`module.jsonc`：

```jsonc
{
  // 源码里的 @标签 → 具体 HAL 后端 + 实现语言 + 核心数
  "deviceTags": {
    "gpu":      { "hal": "GpuHal",   "impl": "sl->ptx",     "cores": 8 },
    "cuda":     { "hal": "GpuHal",   "impl": "cuda-c",      "cores": 8 },
    "arm_npu":  { "hal": "NpuHal",   "impl": "arm-npu-isd","cores": 4 },
    "csharp":   { "hal": "CpuHal",   "impl": "csharp-ffi",  "cores": 1 },
    "asm":      { "hal": "CpuHal",   "impl": "asm",         "cores": 1 }
  },
  // Device.best 用：回退优先级（没有 arm_npu 就试 cuda，再 gpu，再 csharp …）
  "devicePriority": ["arm_npu", "cuda", "gpu", "csharp", "asm"],
  // launch 默认如何把数据拆到设备核心（用户可覆盖）
  "partition": { "default": "tiled", "by": "rows" }
}
```

**调用端（自动路由 + 分区 + 等待 + 零拷贝搬运，全部被 `launch`/`await` 吞掉）**：

```
# 用户只写语义；device / 几个核心 / 回退，全由配置 + launch 决定
Task t = launch( matmul, A, B, device = Device.best() )   # 自动选最优后端 + 按 cores 分区
Tensor C = await t                                        # 协程 await 挂起，等设备上算完
```

**为什么这样最优雅**：
1. **声明与特化同处一处**：`@gpu { }` 紧贴主实现，比先定义 5 个函数再 `Compile.target` 注册更易读；
2. **落点可配置、代码不变**：换机器 / 换驱动只改 jsonc，源码零改动（呼应 §9.1"底座可变"）；
3. **分区与后端解耦**：`@gpu` 决定"用什么实现"，`launch(..., split=ByRows(4))` 决定"拆几个核心"，二者正交；
4. **调用极简**：用户永不写 `Isolate.spawn` / `SendPort` / `TransferableData`——这些是 HAL isolate（§9.1）的内部机制。

> **与 §10.1 的关系**：`@gpu { ... }` 是 `Compile.target` 的声明式糖，二者等价可混用；
> 函数式（`Compile.target`）适合"实现体已单独定义"，声明式（`@gpu { }`）适合"实现体紧贴主 kernel"。

---

## 11. 性能落地清单（按性价比排序）

| # | 动作 | 针对的痛点 | 预期收益 |
|---|------|-----------|---------|
| 1 | 小向量：`class` + `bind` POD `data` 拆分行为与布局；AOT 路径拆箱 + 标量替换 / 逃逸分析 | `Float32_3` 每次运算进 GC | CPU 数学库数量级提升 |
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

> **HAL 是贯穿所有 Step 的运行时底座**（§9.1）：每一步产出的后端变体都通过 HAL 接口提交执行，
> 回退也在 HAL 层统一做。Step 0~2 可先用 `CpuHal` + `VmHal` 跑通，Step 3 起逐步接入
> `CudaHal` / `RocmHal` / `NpuHal`。

| 步 | 内容 | 验收标准 |
|----|------|---------|
| **0** | 行为 / 布局分离：小向量 `class` bind POD `data`；编译器拆箱（SROA）+ 逃逸分析；AOT 支持 POD 按值传 | CPU AOT 数学库 vs 现状：数量级提升；GPU 路径解锁 POD 传参 |
| **1** | 新增 SLTIR（或直接 MLIR 高层）发射路径，与栈机并存 | `AOTGPUTest` 的 matmul 走新路径，性能不低于现状 |
| **2** | `Tensor` 类型 + 元素级算子 + `linalg.generic` + 静态融合 | `relu(a*b+c)` 生成 1 个 kernel；CPU 上出现 vectorization |
| **3** | 命名算子（matmul / conv / reduce）+ 多后端分发 | matmul 在 GPU 接近手写；NPU 后端能认领 `linalg.matmul` |
| **4** | autotune + 图捕获块 | `Compile.kernel( fn )` 无参数也能跑出合理性能 |
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
| 4 | 把 `class`（带对象头与行为）当张量元素 | 设备只认 POD；`class` 元素应编译期拒绝，并引导改用其 bind 的 `data` |
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
| R7 | 对象模型（`class`）与张量 POD 约束冲突 | 元素类型编译期限制为 POD（含 POD `data`）；`class` 元素给出"改用 bind 的 data"的诊断；小向量靠 `bind` + SROA 拆箱消除开销 |
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
| **`md/syntax/data.md`** | 🔥 `data` 语义权威：无函数、无 `extends`、`==` 按结构+内容比较、连续 `m_MemberDataBuffer`（本文 §5.3 / §5.4 的依据） |
| `md/syntax/bind.md` | `bind` 展开语义：`class` bind `data` = **行为 + POD 布局**（本文 §5.4 的核心机制） |
| `md/syntax/coroutine.md` | 协程权威文档（异步复用基础） |
| `md/syntax/marco.md` | 宏（**禁用于算子抽象**） |
| `target.md` | 第 30 条：floatN / matrixN 快捷计算<br>⚠️ 第 25 条「data 一般在栈上生成」与 `data.md` 不一致，**以 `data.md` 为准** |

---

## 16. 理想形态 Demo 集

> **重要说明**：以下全部为**目标形态示意**，语法尚未实现，仅用于对齐"最终想要长什么样"。
> 语法风格刻意贴近现有 SL 写法（`data` / `ret` / `if cond {}` / `@Nickname()` /
> `for i = 0, i < M, i += 1`），以便后续真正落地时改动最小。
> 每组 demo 末尾标注「**落到 MLIR**」与「**为什么这样设计**」。

### 16.0 全景一张图：库作者 vs 库使用者

| | 库作者（`Math` / `Tensor` 库内部） | 库使用者（业务 / 模型代码） |
|---|---|---|
| 关心 | 分区、tile、布局、精度、多目标实现 | 只关心数学语义 |
| 工具 | `Compile.kernel()` / `Compile.target()` / `Compile.pod()` / `uniform` / `varying` / `parallel for` / `capture` | 张量表达式 + 运算符 |
| 心智负担 | 高（但可控、可诊断） | **零** |

下面的 demo 按这个分工组织：16.1–16.3 基础，16.4–16.11 库作者视角，
16.12–16.13 库使用者视角，16.14–16.16 精度/布局/可移植，16.17–16.21 诊断与工程化。

---

### 16.1 小向量 / 矩阵：原样用 `Float32_3`，只在**布局阶段**特殊处理

> **不改类型、不引入 `data`、不加 `@` 指令。**
> `Float32_3` 仍是 `class`，运算符重载照写；编译器在布局阶段把它特化成 POD（§5.4.1）。

```sl
import Std;

# ── ① 类型定义：与现状完全一致，一行不改 ──
#    字段全是标量 POD、无继承链 → 满足 POD 布局特化的判定条件
@Nickname("Vec3")                       # 既有 attribute，不是新增指令
public class Float32_3
{
    public Float32 x = 0.0f
    public Float32 y = 0.0f
    public Float32 z = 0.0f

    _init_( Float32 _x, Float32 _y, Float32 _z )
    {
        this.x = _x
        this.y = _y
        this.z = _z
    }

    static Float32_3 _add_( Float32_3 a, Float32_3 b )
    {
        ret Float32_3( a.x + b.x, a.y + b.y, a.z + b.z )
    }

    static Float32_3 _mul_( Float32_3 v, Float32 s )
    {
        ret Float32_3( v.x * s, v.y * s, v.z * s )
    }

    static Float32 dot( Float32_3 a, Float32_3 b )
    {
        ret a.x * b.x + a.y * b.y + a.z * b.z
    }
}

# ── ② 编译期声明：按 POD 布局特化（不满足条件时编译期报错，而不是静默退化）──
Compile.pod( Float32_3 )

# ── ③ 使用：写法与今天完全一致 ──
Vec3 a = Vec3( 1.0f, 2.0f, 3.0f )
Vec3 b = Vec3( 4.0f, 5.0f, 6.0f )
Vec3 c = a + b * 2.0f            # AOT 路径：3 个 f32 寄存器，零分配、无 GC、无虚调用
Float32 d = Vec3.dot( a, b )

# ── ④ 直接做张量元素（特化后即 3×f32 紧凑布局）──
Tensor<Float32_3, 1> ps = Tensor.zeros( [1024], device = cuda:0 )
```

**落到 MLIR**：

- `c` 经 **POD 布局特化 + SROA** → 3 个 `f32`（或 `vector<3xf32>`）寄存器值；
- `Tensor<Float32_3, 1>` → `memref<1024xvector<3xf32>>`（或 `struct<(f32,f32,f32)>`），
  POD 布局可直接进 kernel。

**为什么**：

- **类型系统与用户写法都不变** —— 优化发生在**布局决策**上，不是类型上；
- **语义零风险** —— 解释路径（CVM）完全不受影响，仍按对象处理；
  未通过判定的实例由逃逸分析保守地退回对象语义；
- 顺带解开 GPU 路径「禁止 Struct / ObjRef 槽位」的死结 ——
  进设备的是特化后的 POD 布局，不是对象引用；
- `data` 完全不必介入（它无函数、无法承载运算符重载，不是小向量的必需品）。

---

### 16.2 现状 vs 理想：同一个 matmul

**现状**（`test/SpecialTest/AOTGPUTest.sl`）——三重串行 `for` + 13 个魔数 + 手写扁平索引：

```sl
@AOT()
@GPU( 0, 0, 0, 0, 0, 0, 0, 256, 1, 1, 0, 0, "" )
static void GpuMatMul( double[] a, double[] b, double[] c, int M, int N, int K )
{
    int i = 0
    for i = 0, i < M, i += 1
    {
        int k = 0
        for k = 0, k < N, k += 1
        {
            double s = 0.0
            int j = 0
            for j = 0, j < K, j += 1
            {
                s = s + a[i * K + j] * b[j * N + k]
            }
            c[i * N + k] = s
        }
    }
}
```

**理想**：

```sl
static Tensor<Float32, 2> matmul( Tensor<Float32, 2> A, Tensor<Float32, 2> B )
{
    ret A · B
}
```

**落到 MLIR**：一句 `linalg.matmul`（不是三层循环）→ GPU 走 `gpu.launch` + 库实现，
NPU 直接**认领该 op**，CPU 走 tiling + `vector` + FMA。

**为什么**：同一份语义，一个能被 NPU 认领，一个只能退化；一个用户要写 20 行并猜编译器怎么识别，
一个只需 1 行。这就是"优雅"与"快"同时成立的地方。

---

### 16.3 张量基础：形状 / dtype / 设备

```sl
# rank 静态、dim 可动态（?）
Tensor<Float32, 2> A = Tensor.zeros( [1024, 512], device = cuda:0 )
Tensor<Float32, 2> B = Tensor.randn( [512, 256],  device = cuda:0 )

Int32 m = A.shape[0]                 # 1024
Int32 r = A.rank                     # 2（编译期常量）

Tensor<Float32, 2> C = A · B         # 结果留在 cuda:0，无隐式搬运
Tensor<Float32, 2> D = C.to( host )  # 唯一一次显式搬运
```

**落到 MLIR**：`tensor<1024x512xf32>`（或 `tensor<?x?xf32>` + 维度值）；
`to(host)` 发射为一次显式 copy + 同步点。

**为什么**：设备归属是类型/值的一部分，跨设备访问编译期拒绝（见 16.18），
用户永远不会被"隐式拷贝导致慢 100 倍"坑到。

---

### 16.4 元素级表达式 + 广播 + 自动融合

```sl
# 库使用者视角：就是数学
Tensor<Float32, 2> Y = relu( X @ W + b )
```

**落到 MLIR**：`X @ W` 是 `linalg.matmul`；`+ b` 与 `relu` 被融合进
**同一个** `linalg.generic`（`--linalg-fuse-elementwise-ops`）。

**未融合时会是 3 个 kernel**（matmul / add / relu），三次访存、两份中间临时。
融合后一次读写。

**为什么**：这是"写法优雅"与"跑得快"不冲突的核心机制——**用户写多个算子，
编译器产出一个 kernel**。

---

### 16.5 归约与命名算子

```sl
Tensor<Float32, 1> mu  = X.mean( axis = 0 )
Tensor<Float32, 1> mx  = X.max( axis = 1 )
Float32            tot = X.sum()
Bool               any = ( X > 0.0f ).any()

Tensor<Float32, 2> P   = softmax( S, axis = -1 )
Tensor<Float32, 4> O   = conv2d( I, W, stride = [1, 1], padding = [1, 1] )
Tensor<Float32, 4> Q   = maxpool2d( O, ksize = [2, 2] )
```

**落到 MLIR**：`linalg.reduce`（sum/max）、`linalg.softmax`、`linalg.conv_2d_nchw_fchw`、
`linalg.pooling_nchw_max`。后三者是 NPU 唯一吃得下的粒度。

**为什么**：手写归约循环在 GPU 上要自己处理 warp shuffle、在 NPU 上直接不支持。
提供算子等于三端一次性解决。

---

### 16.6 两区模型：kernel 与着色推导

```sl
# kernel 就是普通 static 方法，不加任何标注
static void saxpy_kernel( TensorView<Float32, 1> y,
                          TensorView<Float32, 1> x,
                          Float32 alpha )
{
    Int32 i = gid( 0 )                 # 全局线性索引
    if i < y.length
    {
        # 此处 Mathf.exp 自动选用 device 版本（着色推导）
        y[i] = alpha * x[i] + Mathf.exp( x[i] )
    }
}

# 注册为 kernel：编译期函数，不产生运行时代码（不用 @，见 §10.1）
Compile.kernel( saxpy_kernel )

# Host 区：普通 SL 代码，if/while/for 全都能用
static void run( Tensor<Float32, 1> x, Tensor<Float32, 1> y )
{
    if x.length == 0                   # CPU 分支，正常编译
    {
        ret
    }
    Int32 blocks = ( x.length + 255 ) / 256     # 或交给 autotune（16.11）
    launch saxpy_kernel[ blocks, 256 ]( y, x, 2.0f )
}
```

**落到 MLIR**：kernel → `gpu.func` / `scf.forall`；`launch` → `gpu.launch_func`。
`Mathf.exp` 被自动染成 device 色，取 `Compile.target( Device.gpu, ... )` 注册的版本
或原生 intrinsic 版本。

**为什么**：Host 区代码就是普通 SL —— 用户不需要学"第二种语言"。
只有被 `Compile.kernel()` 注册的那一小段受设备限制，且越界是**编译期报错**（16.18）。

---

### 16.7 控制流三分：host if / uniform if / varying

```sl
static void kernel( TensorView<Float32, 1> y, TensorView<Float32, 1> x, Int32 flags )
{
    Int32 i = gid( 0 )

    # ① uniform（一致）分支：条件与元素无关 → 真分支，全线程同走一侧
    uniform if ( flags & 1 ) == 1
    {
        y[i] = x[i] * 2.0f
    }
    else
    {
        y[i] = x[i] * 0.5f
    }

    # ② varying（发散）→ 不该写 if，写 select / where
    y[i] = select( x[i] > 0.0f, x[i], 0.0f )

    # ③ 若确实需要发散控制流，显式断言
    varying if x[i] > 1.0f
    {
        y[i] = Mathf.log( x[i] )
    }
    else
    {
        y[i] = x[i]
    }
}

Compile.kernel( kernel )
```

**落到 MLIR**：

| 写法 | 发射 |
|---|---|
| `uniform if` | `scf.if`（保留真实分支，无掩码开销） |
| `select(...)` | `arith.select`（单指令，可向量化） |
| `varying if` | mask 化执行（`scf.if` + 谓词 / 或 `scf.index_switch` 风格重组） |

**为什么**：GPU 分支发散会串行化、NPU 根本不支持分支。把三种意图**显式区分**，
编译器才可能各自生成最优代码；默认不做 `uniform` 分析时，用 `select` 兜底并给诊断。

---

### 16.8 自动分支消除（用户仍写 if，编译器改写成 select）

```sl
static void clamp_kernel( TensorView<Float32, 1> y, Float32 lo, Float32 hi )
{
    Int32 i = gid( 0 )
    Float32 v = y[i]

    if v < lo                 # 两侧均为纯赋值 → 自动消除
    {
        v = lo
    }
    elif v > hi
    {
        v = hi
    }

    y[i] = v
}

Compile.kernel( clamp_kernel )
```

**落到 MLIR**：整个 if/elif 被改写为
`arith.select` 链（`%v' = select(cmpf olt), select(cmpf ogt)`），**无分支**。

**诊断**：编译器给出信息级提示
「`clamp_kernel:12` if-else 已消除为 select（无分支）；若确为发散控制流，请标注 `varying`」。

**为什么**：用户心智里就是"夹取到区间"，没必要逼他改写代码。
**让编译器负责性能，让用户保持可读。**

---

### 16.9 并行逃生舱：`parallel for`

```sl
static void nbody_kernel( TensorView<Float32_3, 1> pos,     # Float32_3 经 POD 布局特化
                          TensorView<Float32_3, 1> vel,
                          Float32 dt )
{
    parallel for i in 0..pos.length
    {
        Vec3 acc = Vec3( 0.0f, 0.0f, 0.0f )     # 写法仍是 Vec3，编译器拆箱为 3 个 f32

        # 不规则：可变步数 + 依赖数据的终止条件
        Int32 j = 0
        while j < pos.length
        {
            Vec3 d = pos[j] - pos[i]
            Float32 r2 = Vec3.dot( d, d ) + 1e-6f
            if r2 < 4.0f                       # 发散：由编译器 mask 化
            {
                acc = acc + d * ( 1.0f / ( r2 * Mathf.sqrt( r2 ) ) )
            }
            j = j + 1
        }
        vel[i] = vel[i] + acc * dt
    }
}

Compile.kernel( nbody_kernel )
```

**落到 MLIR**：`scf.parallel`（CPU 多线程）/ `scf.forall` + 映射到 `gpu.launch`（GPU）。

**为什么**：算子派覆盖不了不规则算法。**必须留一个能写任意循环的口子**，
否则库作者会被迫用算子拼出极其扭曲的写法。这也是 16.2 现状写法的"正当归处"。

---

### 16.10 多目标特化（泛化 `Mathf.sl` 的 `@DllImport` + fallback 模式）

```sl
# 现状雏形：Mathf.sin = @DllImport(C 实现) + SL fallback 本体
# 理想：把同样的"绑定优先 + 本体兜底"泛化到后端选择 —— 但用函数，不新增 @

static Tensor<Float32, 2> gemmNpu( Tensor<Float32, 2> A, Tensor<Float32, 2> B )
{
    ret Npu.gemm( A, B )                # 厂商粗算子，静态 shape 最优
}

static Tensor<Float32, 2> gemmGpu( Tensor<Float32, 2> A, Tensor<Float32, 2> B )
{
    ret A · B                           # linalg.matmul → 张量核
}

static Tensor<Float32, 2> gemmCpu( Tensor<Float32, 2> A, Tensor<Float32, 2> B )
{
    ret CpuGemm.tiled( A, B, tile = [64, 256, 64] )   # 分块 + vector + FMA
}

# 通用兜底：任何环境（含 CVM 解释器）都能跑
static Tensor<Float32, 2> gemmGeneric( Tensor<Float32, 2> A, Tensor<Float32, 2> B )
{
    Int32 M = A.shape[0]
    Int32 K = A.shape[1]
    Int32 N = B.shape[1]
    Tensor<Float32, 2> C = Tensor.zeros( [M, N] )
    Int32 i = 0
    for i = 0, i < M, i += 1
    {
        Int32 j = 0
        for j = 0, j < N, j += 1
        {
            Float32 s = 0.0f
            Int32 k = 0
            for k = 0, k < K, k += 1
            {
                s = s + A[i, k] * B[k, j]
            }
            C[i, j] = s
        }
    }
    ret C
}

# 编译期注册：模块级语句，或写在 project 的 _before_( metaType type ) 钩子里
Compile.target( Device.npu, gemmNpu )
Compile.target( Device.gpu, gemmGpu )
Compile.target( Device.cpu, gemmCpu )
Compile.fallback( gemmGeneric )
```

**落到 MLIR**：四个变体分别编译进 `aot` 段的 native 表 / gpu module / npu module；
**兜底版本留在 `module.json` 走 CVM**。运行时按设备可用性选择（16.21）。

**为什么**：这与 `Mathf.sl` 现有写法**同构**，团队零学习成本。
且保证"任何时刻都有能跑的版本"——这是失败即回退的语言层表达。

---

### 16.11 autotune：调度参数移出语言

```sl
# 理想：不写任何调度参数
static void conv_kernel( TensorView<Float32, 4> out,
                         TensorView<Float32, 4> in,
                         TensorView<Float32, 4> w )
{
    # 只描述计算，不描述怎么并行
    parallel for n in 0..out.shape[0]
    {
        parallel for c in 0..out.shape[1]
        {
            # ... 计算一个输出通道
        }
    }
}

# 注册：不写参数 = 交给 autotune
Compile.kernel( conv_kernel )

# 需要人工干预时才用具名参数（不再是 13 个位置魔数）
static void conv_kernel_tuned( ... ) { ... }
Compile.kernel( conv_kernel_tuned, tile = [16, 16], block = 256 )
```

**运行时行为**：首次遇到某 shape 时，实测若干 tile 变体（如 `[8,8] / [16,16] / [32,32]`），
选最优并**缓存到模块级**（下次直接命中）。

**为什么**：现状 `@GPU(0,0,0,0,0,0,0,256,1,1,0,0,"")` 要求人写死 `256`——
人不可能为每种 shape / 每种显卡写对。**让机器测，比让人猜靠谱。**

---

### 16.12 图捕获：跨函数融合逃生舱

```sl
static Tensor<Float32, 2> mlp( Tensor<Float32, 2> X,
                                Tensor<Float32, 2> W1, Tensor<Float32, 1> b1,
                                Tensor<Float32, 2> W2, Tensor<Float32, 1> b2 )
{
    capture
    {
        var h = X @ W1 + b1
        h = relu( h )
        h = h @ W2 + b2
        ret softmax( h, axis = -1 )
    }
    # 出口：整块融合为 1~2 个 kernel，按 shape 缓存、可 AOT 化复用
}
```

**落到 MLIR**：块内先构 `linalg` 图，出口统一跑融合 / tiling pass。

**为什么**：编译期融合跨函数边界会失效。`capture` 是一个**显式、局部、可预测**的
图模式开关——默认仍是 eager（好调试），只有明确包起来才延迟物化。
（TF1 默认懒执行导致调试地狱，这里刻意不犯同样的错。）

---

### 16.13 异步与流水线：复用已有协程，零新语法

```sl
static void pipeline( Tensor<Float32, 2> A, Tensor<Float32, 2> B, Int32 chunks )
{
    Int32 c = 0
    for c = 0, c < chunks, c += 1
    {
        # 上传与计算 overlap：复用 spawn / await，不发明新语法
        Task up = spawn A.chunk( c ).to( cuda:0, async = true )
        Task upB = spawn B.chunk( c ).to( cuda:0, async = true )
        await up
        await upB

        Task k = spawn launch( gemm, A.chunk( c ), B.chunk( c ) )

        prepare_next_chunk( c + 1 )     # CPU 同时干别的活

        await k
    }
}
```

**落到 MLIR / 运行时**：`launch` 返回 `Task`，内部是异步 stream + event；
`await` 即同步。CPU 数据准备与设备计算天然重叠。

**为什么**：**不引入任何新语法**就拿到流水线能力。
你已经有了 `spawn / await / Channel`，异构异步只是它的一个应用场景。

---

### 16.14 混合精度

```sl
# 存储 f16 / bf16 / f8，累加 f32 —— 张量核与 NPU 的甜点区
static Tensor<Float32, 2> gemmMp( Tensor<Float32, 2> A, Tensor<Float32, 2> B )
{
    Tensor<Float16, 2> Ah = A.to( Float16 )
    Tensor<Float16, 2> Bh = B.to( Float16 )
    ret Ah · Bh                      # f16 输入、f32 累加，结果 f32
}

Compile.precision( gemmMp, storage = Float16, accumulate = Float32 )

# 推理场景用你已实现的 f8 格式
Tensor<Float8_E4M3, 2> Wq = W.quantize( Float8_E4M3, scale = ws )
Tensor<Float8_E5M2, 2> Xq = X.quantize( Float8_E5M2, scale = xs )
Tensor<Float32, 2>     C  = Xq · Wq
```

**落到 MLIR**：GPU 用原生 `arith.truncf / extf`（快）；
CPU 保留你现有的**位精确整数软实现**（对齐 CVM 语义）；
`--strict-lowp-bitexact` 开关用于双跑对拍（§11.1）。

**为什么**：你已经投入很大的成本做了位级精确的 e4m3/e5m2/f16/bf16，
这是稀缺资产。**但位精确与设备性能冲突时，要能按目标切换，而不是一刀切。**

---

### 16.15 布局（NCHW / NHWC / SoA）

```sl
Tensor<Float32, 4, NHWC> X = Tensor.zeros( [1, 224, 224, 3], device = npu:0 )
Tensor<Float32, 4, NCHW> Y = Tensor.zeros( [1, 3, 224, 224], device = cuda:0 )

var O1 = conv2d( X, W )              # NPU 原生吃 NHWC，零转换
var O2 = conv2d( Y, W )              # 编译器自动插入布局转换，且在 IR 中可见

# 粒子系统：AoS → SoA，GPU 合并访存
# Float32_3 经 POD 布局特化后可直接做元素（等价于 3×f32 紧凑布局）
Tensor<Float32_3, 1, AoS> ps = ...
Tensor<Float32, 2, SoA>   ps2 = ps.to( SoA )     # [[x...],[y...],[z...]]
```

**落到 MLIR**：布局是 `memref` 的 `strided<..., offset: ...>` 属性；
转换是显式 `linalg.transpose` / `memref.copy`，**可在 IR 里看到、可优化、可消除**。

**为什么**：NPU 对布局极其敏感（昇腾 NZ 分形等）。让布局成为**类型参数 + 一等 op**，
就能自动插入转换、并有机会把相邻转换抵消掉；手写则必然遗漏。

---

### 16.16 库作者完整案例：LayerNorm（两区协作）

```sl
public class LayerNorm
{
    Tensor<Float32, 1> gamma
    Tensor<Float32, 1> bias
    Float32 eps

    # ── Host 区：形状校验、参数准备 —— 全都是普通 if/for ──
    _init_( Int32 hidden, Float32 _eps = 1e-5f )
    {
        if hidden <= 0
        {
            throw Error( "LayerNorm: hidden must be positive" )
        }
        this.eps = _eps
        this.gamma = Tensor.ones( [hidden] )
        this.bias  = Tensor.zeros( [hidden] )

        Int32 i = 0
        for i = 0, i < hidden, i += 1
        {
            this.gamma[i] = 1.0f
        }
    }

    # ── Device 区：两趟归约 + 一趟归一化 ──
    #    （普通 static 方法；注册语句见下方 Compile.kernel）
    static void ln_kernel( TensorView<Float32, 2> out,
                           TensorView<Float32, 2> inp,
                           TensorView<Float32, 1> g,
                           TensorView<Float32, 1> b,
                           Float32 eps )
    {
        Int32 row = gid( 0 )
        if row >= out.shape[0]
        {
            ret
        }

        # 第一趟：均值（reduce）
        Float32 mu = 0.0f
        Int32 j = 0
        for j = 0, j < inp.shape[1], j += 1
        {
            mu = mu + inp[row, j]
        }
        mu = mu / inp.shape[1]

        # 第二趟：方差（reduce）
        Float32 va = 0.0f
        for j = 0, j < inp.shape[1], j += 1
        {
            Float32 d = inp[row, j] - mu
            va = va + d * d
        }
        va = va / inp.shape[1]

        # 第三趟：归一化（elementwise）
        Float32 inv = 1.0f / Mathf.sqrt( va + eps )
        for j = 0, j < inp.shape[1], j += 1
        {
            out[row, j] = ( inp[row, j] - mu ) * inv * g[j] + b[j]
        }
    }

    # ── 编译期注册：把 ln_kernel 登记为 device kernel ──
    Compile.kernel( ln_kernel )

    # ── 对使用者暴露的 API：只剩一行 ──
    Tensor<Float32, 2> forward( Tensor<Float32, 2> X )
    {
        Tensor<Float32, 2> Y = Tensor.zeros_like( X, device = X.device )
        launch ln_kernel[ X.shape[0], 256 ]( Y, X, this.gamma, this.bias, this.eps )
        ret Y
    }
}
```

**为什么这组是"两区模型"的最佳注脚**：

- 构造器、校验、参数初始化 → **Host 区**，普通 `if` / `for` / `throw`，随便写；
- 三个密集循环 → **Device 区**，一行 `Compile.kernel( ln_kernel )` 登记；
- 使用者只看到 `ln.forward(X)`。

---

### 16.17 库使用者完整案例：Transformer Block（零心智负担）

```sl
public class TransformerBlock
{
    Tensor<Float32, 2> Wq, Wk, Wv, Wo, W1, W2
    LayerNorm ln1, ln2

    Tensor<Float32, 3> forward( Tensor<Float32, 3> X, Tensor<Float32, 3> mask )
    {
        # 全程张量表达式：没有一次 kernel 手写、没有一行并行代码
        var q = X · this.Wq
        var k = X · this.Wk
        var v = X · this.Wv

        var s = q · k.transpose( -2, -1 ) / Mathf.sqrt( Float32( k.shape[-1] ) )
        s = s + mask * -1e9f
        var att = softmax( s, axis = -1 )

        var o  = att · v · this.Wo
        var h  = this.ln1.forward( X + o )

        var f  = relu( h · this.W1 ) · this.W2
        ret this.ln2.forward( h + f )
    }
}
```

**落到 MLIR**：4 个 `linalg.matmul` + 若干被融合的 `linalg.generic`
（add / scale / mask / relu 各自并入相邻算子）+ 1 个 `linalg.softmax` + 2 次 reduce。

**为什么**：这就是"优雅"的验收标准——**一个不懂 GPU 的人能写出跑满 GPU/NPU 的代码**，
并且这段代码在没有 GPU 的机器上也能正确跑（只是慢）。

---

### 16.18 诊断：编译器应该报什么错

| 用户写了什么 | 编译器输出 |
|---|---|
| `Console.println( C[0, 0] )`（`C` 在 `cuda:0`） | **Error** SL8xx：`C` 位于 `cuda:0`，Host 区不可访问设备内存。<br>建议：`C.to(host)` 后访问，或将该语句移入 kernel（`Compile.kernel()` 注册的函数）。 |
| `Tensor<Player, 1> list = ...`（成员含引用类型） | **Error** SL8xx：张量元素必须可 POD 特化，`Player` 未通过判定 —— 成员 `name` 为 `String`（引用类型）。<br>建议：张量内只放 POD 字段，引用数据改用 `Tensor<Int32, 1>` 索引表旁挂。 |
| 在 `data` 里定义成员函数 | **Error**（已有规则）：`data` 不提供成员函数，职责是承载数据。<br>建议：改为 `class bind <该 data>`，由 `class` 提供行为（§5.4）。 |
| 在 `Compile.kernel()` 注册的函数里调 `Console.println` | **Error** SL8xx：`Console.println` 为 Host 色（调用链：`ln_kernel` → `Console.println`），Device 区不可调用。<br>（**编译期报错，而不是导出期静默回退**——这是现状 §4 硬 Fail 的最大改进点） |
| 元素级 `if` 未被消除 | **Warning** SL9xx：`nbody_kernel:14` 该分支依赖元素索引，已 mask 化，预计发散损失约 40%。<br>建议：改用 `select(...)`，或标注 `varying if` 确认意图。 |
| 某 kernel 无法 AOT | **Info** SL9xx：`gemm` 走 CVM 解释（原因：动态 shape 不支持）。<br>结果正确，性能较低；可用 `Compile.target( Device.cpu, ... )` 注册特化提升。 |
| 设备不存在 | **运行时** Info：未检测到 NPU，`gemm` 回退到 `Compile.target( Device.gpu, ... )` 注册的变体；未检测到 GPU，回退 cpu 变体。 |
| `Compile.pod( X )` 判定失败 | **Error** SL8xx：`X` 不能按 POD 布局特化 —— 成员 `tag` 为 `String`（引用类型）。<br>建议：移除引用成员，或去掉 `Compile.pod` 声明（退回对象语义，仅不能进设备 / 做张量元素）。 |

**为什么**：现状 GPU 路径的限制是**导出期静默 Fail**（用户只看到"回退了"）。
把它变成**带调用链、带修复建议的编译期诊断**，是使用体验上最大的单点改进。

---

### 16.19 可移植性：同一份代码在不同机器

```sl
# 运行时选择设备；都没有也能跑
Device dev = Device.best( npu, gpu, cpu )

Tensor<Float32, 2> A = Tensor.randn( [2048, 1024], device = dev )
Tensor<Float32, 2> B = Tensor.randn( [1024,  512], device = dev )
Tensor<Float32, 2> C = A · B                 # 16.10 的四个变体在此分派
Tensor<Float32, 2> R = C.to( host )
Console.println( R[0, 0] )
```

| 运行环境 | 实际执行路径 |
|---|---|
| 有 NPU | `Compile.target( Device.npu, ... )` 变体 → 厂商粗算子 |
| 有 GPU 无 NPU | `Compile.target( Device.gpu, ... )` 变体 → `gpu.launch` |
| 只有 CPU + AOT | `Compile.target( Device.cpu, ... )` 变体 → tiling + vector |
| 无任何 AOT（如解释器环境） | 兜底三重循环 → CVM 解释 |

**为什么**：这是现有 `aot / vm / bridge` 三态机制的推广
（从"要么 AOT 要么 VM"到"cpu/gpu/npu/vm 四选一"），
也是**能激进重构却不会把现有功能搞坏**的根本保障。

---

### 16.20 与现状 API 的兼容性

```sl
# 现有写法一律保留，只是变慢，不会报错
double[] a = ...
double[] b = ...
double[] c = ...
AOTGPUTest.VmMatMul( a, b, c, M, N, K )      # 继续走 CVM 解释

# 新写法与旧写法可以混用（数组 ⇄ 张量视图零拷贝）
Tensor<Float64, 2> Av = Tensor.view( a, [M, K] )
Tensor<Float64, 2> Cv = Av · Tensor.view( b, [K, N] )
```

**为什么**：`Tensor.view` 让现有基于 `Array<T>` 的存量代码**零成本接入**新路径，
而不是要求一次性重写。这对一个有 500+ `.sl` 标准库的项目是硬性要求。

---

### 16.21 端到端：从源码到三端产物

```sl
# 一份源码
static void k( ... ) { ... }
Compile.kernel( k )                                  # 编译期注册
static Tensor<Float32,2> gemm( ... ) { ret A · B }

# 编译期产出（同一份 module.json 内并存）
module.json
├── slir            # 兜底：CVM 解释（永远存在）
├── aot/cpu         # MLIR → LLVM → 原生（scf.parallel + vector）
├── aot/gpu         # MLIR → gpu.func → NVVM / ROCm / SPIR-V
└── aot/npu         # MLIR → linalg / TOSA → 厂商 IR

# 运行时
Device.best( npu, gpu, cpu )  →  按可用性与 shape 选择，失败逐级回退
```

**验收方式**（沿用 `MLIR_AOT_DESIGN.md` §6.5）：

```
CVM 解释版输出  ≡  CPU AOT 版输出  ≡  GPU 版输出  ≡  NPU 版输出
```

四者一致即为通过；任一环节缺失时自动降級，不阻断。

---

### 16.23 HAL 视角：多后端统一底座

# HAL 把所有硬件差异收进一层接口；用户 / 库作者只面对 Device 与 Compile.target（接 §9.1）

# ① 设备发现：枚举 + 按优先级选最优，不存在即跳过
Device dev = Device.best( npu, gpu, cpu )      # 内部走 HAL 的设备发现原语
Console.println( dev.name, dev.capability )     # 例如 "npu0 / 64TOPS"

# ② 同一份 kernel，按 HAL 后端分发（多目标特化 = 为某 HAL 注册变体）
static void gemmNpu( ... ) { ... }              # 仅 NpuHal 可用
static void gemmGpu( ... ) { ... }              # 仅 CudaHal / RocmHal 可用
static void gemmCpu( ... ) { ... }              # 仅 CpuHal 可用
static void gemmFallback( ... ) { ... }         # 所有后端都没有时走 VmHal

Compile.target( Device.npu, gemmNpu )
Compile.target( Device.gpu, gemmGpu )
Compile.target( Device.cpu, gemmCpu )
Compile.fallback( gemmFallback )

# ③ 内存 / 命令 / 同步全经 HAL；用户只写语义
#    launch 即"向 dev 对应 HAL isolate 的命令端口异步提交"（复用 SendPort，永不阻塞）
#    返回的 Task 就是该 isolate 的协程句柄，可直接 await
Tensor<Float32, 2> A = Tensor.randn( [2048, 1024], device = dev )
Tensor<Float32, 2> B = Tensor.randn( [1024,  512], device = dev )
Task t = launch( gemm, A, B )                   # 提交到 HAL 队列，立刻返回 Task
Tensor<Float32, 2> C = await t                  # 协程 await：挂起等设备算完（复用 Coroutine.awaitHandle）

# ④ 跨设备搬运经 HAL 内存原语：用 TransferableData（零拷贝所有权转移，复用 Isolate 同款机制）
#    张量是大块共享数据，不能走 SendPort 深拷贝；语义 = 把 C 的底层 buffer 转移给目标设备 isolate
TransferableData buf = TransferableData.fromTensor( C )   # 示意：装为转移块（具体 API 名待定）
Tensor<Float32, 2> Cpu = Tensor.materialize( host, buf )  # 示意：在 host 堆物化（源段失效）

# ⑤ 某后端缺失时的回退链（HAL 层自动发生，用户无感）
#    有 NPU → NpuHal isolate(gemmNpu)   无 NPU 有 GPU → CudaHal isolate(gemmGpu)
#    都没有   → CpuHal isolate(gemmCpu) 连 AOT 都没有 → VmHal isolate(gemmFallback，CVM 解释)
#
# 💡 HAL 后端本质就是"设备隔离岛"：每个后端是独立 isolate（独立堆/GC/故障隔离），
#    与现有 Isolate + Coroutine + Channel + SendPort 完全同构，等待只用协程 await，
#    不需要为"等待设备"发明任何新语法、新关键字或新 `@` 指令。
```

**落到 HAL**：`Device.best` → 调 `CpuHal / CudaHal / NpuHal` 的发现接口；
`launch` → `hal.submit(kernel, queue)`；`to(host)` → `hal.copy(dst, src)`；
`await` → `hal.wait(event)`。所有后端对上层暴露同一组接口，
**所以 Step 0 可以只有 `CpuHal + VmHal`，之后接入 `NpuHal` 时用户代码零改动**。

**为什么**：HAL 把"异构"这个最难、最易碎的部分收口到一个**可插拔、可测试、可缺失**的层。
编译器、库作者、使用者都不必关心具体驱动；回退在 HAL 内链式发生，
最坏情况落到 `VmHal`（CVM 解释），保证**任何机器都能跑、任何一步失败都能降级**。

---

### 16.24 综合示例：CPU 入口 → CPU/GPU/NPU 多核心流水回环

# 场景：CPU 读入一批数据 → 预处理 → 三路并行（CPU isolate 多 worker / GPU 多核心 / NPU 多核心）
#       → GPU 完成回传 CPU 中转 → 再发 NPU 后处理 → NPU 完成再回传 GPU 融合 → 最终聚合出结果。
# 全程只用 launch + await；isolate / 核心分区 / 跨设备零拷贝搬运都被底层吞掉（接 §9.1、§10.2）。

# ── 0. 配置（module.jsonc 节选）：标签→后端、核心数、回退 ──
#   "deviceTags": { "gpu":{hal:"GpuHal",cores:4}, "arm_npu":{hal:"NpuHal",cores:2}, "csharp":{hal:"CpuHal",cores:4} }
#   "devicePriority": ["arm_npu","gpu","csharp"]

# ── 1. kernel 定义：一份语义 + 多设备特化体（@gpu / @arm_npu / @csharp）──
kernel featExtract(x) {                 # 主实现（兜底）
    return relu( conv(x) )              # 纯 SL
} @gpu {                                # GPU 4 核心：每个核心算一行分块
    # gpu 核函数体（grid = 4, block 由 split 推导）
} @arm_npu {                            # NPU 2 核心
    # arm npu isd 体
} @csharp {                             # CPU 高性能路径（C# BLAS）
    // C# 互操作
}

kernel postProcess(x) { ... } @arm_npu { ... } @gpu { ... } @csharp { ... }   # NPU 后处理特化
kernel fuse(x)        { ... } @gpu { ... } @arm_npu { ... } @csharp { ... }   # GPU 最终融合特化
kernel cpuBranch(x)   { ... } @csharp { ... }                                 # CPU isolate worker 支路

# ── 2. CPU 入口（main isolate 协程）：IO 读入 + 预处理 ──
func main() {
    # ① 数据 IO 读入（文件 / 网络，复用协程 await 不阻塞）
    RawBatch raw = await File.read( "dataset.bin" )          # 协程挂起等 IO
    Batch X = preprocess( raw )                              # CPU 侧简单逻辑：归一化 / padding

    # ② 三路并行启动（全部异步提交，立刻返回 Task，互不阻塞）
    #    CPU 其它线程：一个 isolate 内的 4 个 worker，算独立支路
    Task tCpu = launch( cpuBranch, X, device = csharp, split = ByRows(4) )

    #    GPU：4 个核心算不同数据分块（split 把 X 按行拆 4 份）
    Task tGpu = launch( featExtract, X, device = gpu, split = ByRows(4) )

    #    NPU：2 个核心算不同数据分块
    Task tNpu = launch( featExtract, X, device = arm_npu, split = ByRows(2) )

    # ③ 等 GPU 这路先完成（GPU 各核心结果聚回 CPU）
    Tensor gpuFeat = await tGpu

    # ④ GPU → CPU 中转 → 再发 NPU 做后处理（跨设备零拷贝，底层 TransferableData）
    #    launch 内部自动：gpuFeat 经 TransferableData 转移到 NpuHal isolate
    Task tNpu2 = launch( postProcess, gpuFeat, device = arm_npu, split = ByRows(2) )
    Tensor npuPost = await tNpu2

    # ⑤ NPU 完成 → 回传 GPU 做最终融合（又一次跨设备转移）
    Task tGpu2 = launch( fuse, npuPost, device = gpu, split = ByRows(4) )
    Tensor fused = await tGpu2

    # ⑥ 与 NPU 首路(tNpu)、CPU 支路(tCpu) 汇合，最终归约
    Tensor npuFirst = await tNpu            # NPU 首路（featExtract）结果
    Tensor cpuRes   = await tCpu            # CPU isolate 支路结果
    Tensor result   = reduce( fused, npuFirst, cpuRes )   # 最终聚合

    Console.println( "done: ", result.shape )
}

# 💡 要点回顾：
#   · 全程只出现 launch + await；核心分区（split）、设备选择（device）、回退（配置）全被吞掉。
#   · "GPU→CPU→NPU→GPU" 的回环就是第 ③④⑤ 步的 await + 再次 launch，语义与单设备无差别。
#   · 每个 device 背后是一个 HAL worker isolate（§9.1）；跨设备搬运用 TransferableData 零拷贝；
#     多核心 = launch 的 split 把数据拆块到该 isolate 的命令队列。
#   · 若某设备缺失（如没 NPU），launch 按 devicePriority 自动回退 gpu/csharp，用户代码零改动。

---

### 16.22 Demo 索引（按"想看什么"查）

| 想看 | 小节 |
|---|---|
| 小向量原样用 `Float32_3` + POD 布局特化 | 16.1 |
| 现状 matmul vs 理想 matmul | 16.2 |
| 张量形状 / 设备 / 显式搬运 | 16.3 |
| 元素级表达式与融合 | 16.4 |
| 归约与命名算子（NPU 关键） | 16.5 |
| Host / Device 分区与着色推导 | 16.6 |
| uniform / varying / select 三分 | 16.7 |
| 自动分支消除（仍写 if） | 16.8 |
| 不规则并行的逃生舱 | 16.9 |
| 多目标特化（cpu/gpu/npu + 兜底） | 16.10 |
| autotune 取代手写 256 | 16.11 |
| 图捕获（跨函数融合） | 16.12 |
| 异步流水线（复用协程） | 16.13 |
| 混合精度 f16/bf16/f8 | 16.14 |
| 布局 NCHW/NHWC/SoA | 16.15 |
| 库作者完整案例 | 16.16 |
| 库使用者完整案例 | 16.17 |
| 编译诊断长什么样 | 16.18 |
| 可移植与回退矩阵 | 16.19 |
| 与现有 Array 代码兼容 | 16.20 |
| 端到端产物与验收 | 16.21 |
| HAL 视角：多后端统一底座（设备发现 / 分发 / 回退） | 16.23 |
| 统一设备标签 `@gpu`/`@cuda`/`@csharp`/`@asm`/`@arm_npu` 与 jsonc 配置 | 10.2 |
| 综合示例：CPU 入口 → CPU/GPU/NPU 多核心流水回环 | 16.24 |
