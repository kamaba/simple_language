# SimpleLanguage MLIR AOT 设计文档

> 版本：v1.0　|　日期：2026-08-31　|　状态：**设计草案（待评审）**
>
> 目标：把 SimpleLanguage（SL）源码经 SLIR 翻译为 **MLIR**，再借助 `llvm-project` 已构建的 MLIR/LLVM 工具链（`mlir-opt` → `mlir-translate` → `llc`）与 CVM 运行时（`csimple_lang`）链接，产出**原生可执行文件（AOT）**。

---

## 1. 背景与目标

SL 目前有两种执行路径：

1. **解释执行**：`module.json`（SLIR 指令包）加载进 CVM（`csimple_lang`）逐条解释；
2. **部分 AOT 尝试**：`LLVMEmitter`（C#）能直接把 SLIR 方法翻译成 LLVM IR 文本（栈式 double 模拟），但**未走 MLIR，也未打通链接**。

本设计的目标是建立第三条路径——**MLIR AOT**：

- 以 MLIR 作为中间表示层，用官方降级链完成 `高层方言 → LLVM dialect → LLVM IR → 目标文件`；
- 通过链接 CVM 运行时（对象模型 + GC + 数组/字符串 + 系统调用）保留 SL 完整语义；
- 复用 `llvm-project/build/Release/bin` 下**已构建好**的工具链，补齐缺失的链接器环节。

### 1.1 与现有路径的关系

```
                    ┌─────────────────────────────┐
                    │   SL 源码 (.sl)             │
                    └──────────────┬──────────────┘
                                   ▼
                    ┌─────────────────────────────┐
                    │   Front（lexer→parser→IR）   │
                    └──────────────┬──────────────┘
                                   ▼
                    ┌─────────────────────────────┐
                    │   SLIR（IRMethod+IRDataList）│
                    └──┬──────────┬───────────────┘
                       │          │
         [已有]        │          │         [本设计]
                       ▼          ▼
              ┌────────────┐   ┌────────────────────┐
              │module.json │   │  MLIRExporter      │
              │(交换格式)   │   │  SLIR → MLIR 文本  │
              └─────┬──────┘   └─────────┬──────────┘
                    │                    ▼
              ┌─────┴──────┐   ┌────────────────────┐
              │  CVM 解释   │   │  mlir-opt（降级）   │
              │ 执行        │   └─────────┬──────────┘
              └────────────┘             ▼
                                  ┌────────────────────┐
                                  │  mlir-translate     │ → LLVM IR
                                  └─────────┬──────────┘
                                            ▼
                                  ┌────────────────────┐
                                  │  llc（目标文件）     │
                                  └─────────┬──────────┘
                                            ▼
                                  ┌────────────────────┐
                                  │ 链接器 + CVM runtime│ → a.exe
                                  └────────────────────┘
```

---

## 2. 现状盘点

### 2.1 已有资产

| # | 资产 | 位置 | 说明 |
|---|------|------|------|
| 1 | 前端（C#） | `simple_language/source/Front/` | lexer → parser → meta → IR 全链路成熟 |
| 2 | SLIR 模型 | `source/Front/IR/IRBase.cs`、`IRMethod.cs`、`IRData.cs` | 线性栈机指令列表（`IRDataList`），跳转靠指令索引 |
| 3 | 统一导出 | `source/Front/Export/ExportLangManager.cs` | 生成 `module.json`（`SLModulePackageWriter`） |
| 4 | LLVM 参考实现 | `source/Front/Export/AOT/LLVMEmitter.cs` | **已实现**栈式 double 模拟生成 LLVM IR 文本（`LoadArgument`/`CallStatic`/`Br*`/`C*`/`LoadLocal`/`LoadConst*`/`Add`…`Ret`），是本设计"策略 A"的直接基线 |
| 5 | MLIR 骨架 | `source/Front/Export/MLIR/MLIRExporter.cs` | 仅把每条 IR 指令导出为注释，**无真实 MLIR op** |
| 6 | 工具链封装 | `source/Front/Export/MLIR/MLIRToolchain.cs` | 已封装 `mlir-opt` → `mlir-translate --mlir-to-llvmir` → `llc -filetype=obj` → `clang` 序列（**有缺陷，见 6.2**） |
| 7 | C 运行时 | `csimple_lang/` | CVM：对象模型（`VMObjectHeader`）、三色 GC、数组/字符串、系统调用注册表 |
| 8 | MLIR/LLVM 工具链 | `llvm-project/build/Release/bin/` | 已构建：`mlir-opt.exe`、`mlir-translate.exe`、`mlir-runner.exe`、`llc.exe`、`llvm-as/dis/config.exe` |
| 9 | 互操作样例 | `gosimple_lang/mylib.c/h`、`cwrapper.go` | C 互操作已验证 |
| 10 | 空目录预留 | `gosimple_lang/front/Export/{AOT,MLIR,SLIR}/` | Go 版导出器待实现（可选阶段） |

### 2.2 缺口清单

| # | 缺口 | 影响 | 解决方案（详见章节） |
|---|------|------|----------------------|
| G1 | `MLIRExporter` 无真实 op | AOT 无源 | §4 实现 SLIR → MLIR 翻译 |
| G2 | `mlir-opt` 调用未传 pass | 高层方言不会降级，下游 `mlir-translate` 失败 | §6.2 补标准降级 pass 链 |
| G3 | **无 `clang.exe` / `lld`** | 无法链接出 exe | §6.4 三选一（推荐增量构建 lld） |
| G4 | CVM 无 AOT 静态库 | 对象/GC/系统调用无法链接 | §5 裁剪 `slruntime` 静态库 + `sl_abi.h` |
| G5 | 无 `main` 启动器 | 可执行文件缺入口 | §5.4 C 启动器 |
| G6 | `ExportLangManager` 的 MLIR/AOT 分支被注释 | 命令行无法触发 | §7 接通分支 |

---

## 3. 总体架构

### 3.1 端到端数据流

```
SL 源码
  │ ① Front（已有）
  ▼
SLIR（内存 IR） ──[可选]──► module.json（交换/调试）
  │ ② MLIRExporter（新增实现）
  ▼
*.mlir   （func / arith / cf / memref 高层方言）
  │ ③ mlir-opt（canonicalize + 降级到 LLVM dialect）
  ▼
*.opt.mlir（LLVM dialect）
  │ ④ mlir-translate --mlir-to-llvmir
  ▼
*.ll（LLVM IR）
  │ ⑤ llc -filetype=obj
  ▼
*.obj
  │ ⑥ lld-link / link.exe + slruntime.lib + main.obj
  ▼
a.exe  ← AOT 可执行文件
```

### 3.2 关键决策一览

| 决策点 | 结论 | 理由 |
|--------|------|------|
| D1 方言选型 | 高层方言（`func`+`arith`+`cf`+`memref`） | 官方降级 pass 齐全；可插优化；符合 MLIR 哲学 |
| D2 栈机→SSA | 先"内存栈模拟"（A），后"SSA 虚拟栈"（B） | A 与 `LLVMEmitter` 1:1 移植最快跑通；B 再优化性能 |
| D3 运行时 | 链接 CVM 裁剪出的 `slruntime` 静态库 | 复用对象/GC/系统调用，保留完整语义 |
| D4 链接器 | 增量构建 `lld`（备选 VS `link.exe`） | 全 LLVM 生态、无外部依赖 |
| D5 实现路径 | 优先 C# 版（骨架已存在）；Go 版复用同一设计 | 最小改动、最快闭环 |

---

## 4. MLIR 导出器设计（核心，对应缺口 G1）

### 4.1 方言选型（决策 D1）

生成以下**高层方言**，不直接写 LLVM dialect：

| 方言 | 用途 | 降级 pass |
|------|------|-----------|
| `func` | 函数定义/调用 | `--convert-func-to-llvm` |
| `arith` | 整数/浮点算术、比较 | `--convert-arith-to-llvm` |
| `cf` | 控制流（`cf.br` / `cf.cond_br` / `cf.switch`） | `--convert-cf-to-llvm` |
| `memref` | 栈数组模拟、局部变量槽（策略 A） | `--convert-memref-to-llvm` |

### 4.2 模块结构示例

SL 源码 `add.sl`：

```
int add(int a, int b) { return a + b; }
void _main_() { Print(add(1, 2)); }
```

对应 MLIR 输出骨架：

```mlir
module {
  func.func @add(%a: i32, %b: i32) -> i32 {
    %0 = arith.addi %a, %b : i32
    return %0 : i32
  }

  func.func @_main_() {
    %c1 = arith.constant 1 : i32
    %c2 = arith.constant 2 : i32
    %r = func.call @add(%c1, %c2) : (i32, i32) -> i32
    // Print 映射为系统调用（见 §4.8）
    ...
    return
  }
}
```

### 4.3 栈机 → SSA 映射策略（决策 D2）

SLIR 是**线性栈机**（`IRDataList`，跳转靠指令索引，无显式基本块）。两条路线：

| | 策略 A：内存栈模拟（先做） | 策略 B：SSA 虚拟栈（后做） |
|---|---|---|
| 思路 | 开一块 `memref` 固定栈 + 栈顶指针；`push/pop` = `memref.store/load` | 扫描跳转目标切分基本块；每 block 维护"虚拟栈"（`push` 追加 value / `pop` 取末位）；分支用 `cf.br` 的 block arguments 传栈状态 |
| 工作量 | 小（与 `LLVMEmitter` 逻辑几乎 1:1） | 大（需先做 CFG 分析 + block args 合并） |
| 性能 | 一般 | 好（原生 SSA） |
| 跳转 | 天然正确（跳哪算哪） | 需正确切块与 φ 等价合并 |
| 产出 | 先打通全链路 | 作为后端内部优化替换 A |

> 迁移路径：A 是 B 的子集——B 只是在 A 的每个 `push/pop` 点把"内存槽"换成"SSA 值"，函数签名、工具链、运行时**均不变**。

### 4.4 类型映射表

SL 类型（`IRMetaType` / `EVMType`）→ MLIR/LLVM 类型：

| SL 类型 | MLIR 类型 | 说明 |
|---------|-----------|------|
| `void` | `()` | 无返回值 |
| `bool` | `i1` | |
| `int8` / `uint8` | `i8` | 有符号按 `arith.*si*`，无符号按 `*ui*` |
| `int16` / `uint16` | `i16` | |
| `int32` / `uint32` | `i32` | |
| `int64` / `uint64` | `i64` | |
| `float8(e4m3/e5m2)` | `i8`（位模式） | 低精度浮点按位模式存储，运算前扩展为 `f32` |
| `float16` / `float16brain` | `i16`（位模式） | 同上 |
| `float32` | `f32` | |
| `float64` | `f64` | |
| `char` | `i8` / `i32` | 视目标编码 |
| `string` | `ptr<i8>`（句柄） | 实际为 `struct sl_object*`，见 §5 |
| `class` / 对象 | `ptr`（对象指针） | 布局见 §5.2 |
| `array<T>` | `ptr`（数组对象指针） | 运行时数组 |
| `type`（类型对象） | `ptr` | 反射用类型对象 |
| `function` / 闭包 | `ptr` | 闭包对象（`NewClosure`） |

**类型推断规则**：栈机上同一指令（如 `Add`）的类型由**栈顶上下文 + IR 元数据**共同决定。策略 A 中 `LLVMEmitter` 的做法可复用：用 `IRMetaType` 的类名/方法签名启发式判定；策略 B 中改为纯 SSA 类型传播（更精确）。

### 4.5 指令映射表（完整 `EIROpCode`）

**加载常量**（对应 `LoadConst*`）：

| 指令 | MLIR 映射 |
|------|-----------|
| `LoadConstNull` | 常量 `null` 对象指针（`arith.constant 0` 位模式） |
| `LoadConstUInt8/Int8/Int16/UInt16/Int32/UInt32/Int64/UInt64` | `arith.constant`（对应整型） |
| `LoadConstFloat32/Float64` | `arith.constant`（`f32`/`f64`） |
| `LoadConstFloat8_E4M3/E5M2/Float16/Float16_Brain` | `arith.constant`（位模式整型） |
| `LoadConstBoolean` | `arith.constant`（`i1`） |
| `LoadConstString` | 常量字符串 → 数据段 → 运行时 `sl_rt_str_new`（§5.3） |
| `LoadConstType` | 类型对象查找 → `sl_rt_type_get` |

**加载/存取**：

| 指令 | MLIR 映射（A） | MLIR 映射（B） |
|------|----------------|----------------|
| `LoadArgument` / `LoadLocal` | `memref.load` 局部槽 | 直接引用 SSA 参数/块参数 |
| `StoreLocal` / `StoreArgument` | `memref.store` | `cf` 分支传参或改块参数 |
| `LoadGlobal` / `StoreGlobal` | 全局槽 `memref`/`global` | 同左 |
| `LoadStaticField` / `StoreStaticField` | 静态区 gep | 同左 |
| `LoadNotStaticField` | 对象头偏移 + `gep + load`（`llvm.getelementptr`） | 同左 |
| `StoreNotStaticField1/2` | 同上 + 存储（注意操作数顺序标志） | 同左 |
| `LoadArrayIndex` | 运行时 `sl_rt_array_get(obj, idx)` | 同左 |
| `LoadArrayIndexField` | 数组元素字段 gep | 同左 |
| `StoreArrayIndex` | 运行时 `sl_rt_array_set`（注意 `EStoreArrayIndexFlag` 两种栈序） | 同左 |
| `StoreArrayIndexField` | 同上 + 字段 | 同左 |

**对象/栈操作**：

| 指令 | MLIR 映射 |
|------|-----------|
| `NewObject` | `sl_rt_alloc_object(type, size)` |
| `NewTemplateObject` | 同上（模板实例化） |
| `NewArray` | `sl_rt_array_new(len, elemType)` |
| `NewClosure` / `AllocClosureContext` | `sl_rt_closure_new(ctxArray, methodId)` / `sl_rt_ctx_alloc(n)` |
| `Dup` | 复制栈顶（A：load+store；B：value 引用） |
| `Pop` | 丢弃栈顶 |

**算术/位运算**（按操作数类型选 `arith.addi/addf`、`muli/mulf`、`divsi/divui/divf`、`remsi/remui/remf`）：

| 指令 | MLIR 映射 |
|------|-----------|
| `Add`/`Minus`/`Multiply`/`Divide`/`Modulo` | `arith.add*`/`sub*`/`mul*`/`div*`/`rem*` |
| `InclusiveOr`/`Combine`/`XOR` | `arith.ori`/`andi`/`xori` |
| `Shr`/`Shi` | `arith.shrui/shrsi`/`shli` |
| `Not`/`Neg` | `arith.andi`（i1 非）/`arith.negf`（浮点）或 `subi 0-x` |
| `And`/`Or`（短路） | `cf.cond_br` 短路求值（不能直接 `arith.andi`） |

**比较**：

| 指令 | MLIR 映射 |
|------|-----------|
| `Ceq`/`Cne` | `arith.cmpi eq/ne`（整型）、`arith.cmpf oeq/une`（浮点）、对象指针 `eq/ne` |
| `Cgt`/`Cge`/`Clt`/`Cle` | `arith.cmpi sgt/sge/slt/sle` 或 `ugt/…`（按符号）、`arith.cmpf ogt/oge/olt/ole` |

**控制流**（见 §4.6）：

| 指令 | MLIR 映射 |
|------|-----------|
| `Label` / `BrLabel` | 标记基本块 |
| `Br` / `Jmp` | `cf.br` |
| `BrFalse` / `BrTrue` | `cf.cond_br` |
| `Beq`/`Bne`/`Bgt`/`Bge`/`Blt`/`Ble` | 比较 + `cf.cond_br` |
| `Switch` | `cf.switch` |
| `Break` | 跳转到最近 `Label`（循环出口） |

**调用**：

| 指令 | MLIR 映射 |
|------|-----------|
| `CallStatic` | `func.call @方法名`（编译期已知） |
| `CallVirt` / `CallDynamic` | 运行时 `sl_rt_virt_call(methodId, obj, args…)` 或间接调用表 |
| `CallSystemMethod` | 系统调用映射表 → `func.call @sl_rt_xxx`（§4.8） |
| `CallClosure` | 运行时 `sl_rt_closure_invoke(closure, args…)` |
| `CastClass` | 运行时类型检查 + 转换（`sl_rt_cast`） |

**类型转换**（`Convert_*`）：

| 指令 | MLIR 映射 |
|------|-----------|
| `Convert_I8/SI8/I16/UI16/I32/UI32/I64/UI64` | `arith.extsi/trunci/zexti` 链 |
| `Convert_R4/R8` | `arith.fptosi/sitofp/fpext/fptrunc` |
| `Convert_F8E4M3/F8E5M2/F16/F16B` | `arith.fptrunc` 到 f32 再取位模式 |
| `Convert_ToString` | 运行时 `sl_rt_to_string(value)` |

**返回/异常/溢出**：

| 指令 | MLIR 映射 |
|------|-----------|
| `Ret` | `return` |
| `BeginTry`/`EndTry`/`Throw`/`LeaveTry`/`EndFinally` | **阶段一**：全部映射为运行时异常调用（`sl_rt_try_begin`/`sl_rt_throw`/`sl_rt_try_end`，异常帧存运行时栈）；**阶段二**：可升级为 LLVM `invoke`/`landingpad` |
| `BeginChecked`/`EndChecked`/`BeginUnchecked`/`EndUnchecked` | 阶段一：溢出检查用 `llvm.sadd.with.overflow` 族 intrinsics 或运行时 `sl_rt_overflow_add`；阶段二：纯 arith + 条件分支 |

### 4.6 控制流与基本块

- **策略 A**：跳转目标 = 指令索引。导出时按 `Label`/跳转指令出现位置切分 `^bb` 块，块间仅 `cf.br`/`cf.cond_br` 连接；栈内存是统一的 `memref`，块间**无需传递值**，天然正确。
- **策略 B**：跳转目标切块后，块间用 block arguments 传递"活跃栈值"。**活跃栈值分析**（live-out 计算）是唯一新增复杂度，其余同 A。
- `Switch` → `cf.switch`；`And`/`Or` 短路 → 本地临时块。

### 4.7 函数签名与调用约定

| 项 | 设计 |
|----|------|
| 函数名 | 以 IR 方法全名（含类名/重载标记）mangle，如 `SL_Class_Method_v1`，避免冲突 |
| 参数 | 平铺展开：`i64`/`f64`/`ptr`（对象）按 SL 形参顺序 |
| 返回值 | SL 返回类型映射（§4.4）；多返回值场景用 `sret` 指针 |
| `_main_` | 特殊入口，`@_main_` 无参或带 `argc/argv`，由 C 启动器调用 |
| 调用约定 | 默认 C ABI（便于与 C 运行时互调）；后期可改 fastcc 做优化 |

### 4.8 系统调用映射（`CallSystemMethod`）

SL 内置方法（`Print`、`String.Concat`、`Memory.*`、数学库等）在 CVM 中由 `vm_system_registry` 分发。AOT 导出器维护一张 **方法名 → 运行时 C 函数** 的映射表，编译期直接发射 `func.call @sl_rt_xxx`：

| SL 内置方法示例 | 运行时函数 |
|-----------------|-----------|
| `Print(...)` | `sl_rt_print(value)` |
| `String.Concat(a, b)` | `sl_rt_str_concat(a, b)` |
| `Memory.Alloc/Free` | `sl_rt_alloc/free` |
| `Math.*` | 对应 C 数学函数包装 |

映射表集中定义，导出器 + C 桥接层**共用同一份**（防止两侧漂移）。

### 4.9 数据、静态字段与常量池

- **静态字段/全局**：编译为 MLIR `llvm.mlir.global`（或统一运行时静态区），导出器按类收集静态槽位，生成 `sl_rt_static_init` 初始化函数。
- **字符串常量**：进入只读数据段，运行时按需构造 `sl_object` 字符串句柄（避免全局构造问题）。
- **类型对象/反射数据**：运行时按 `IRMetaData` 元数据构建类型表（`sl_rt_type_get(name)` 延迟查找）。

### 4.10 语言分层类型（data / enum / interface）的映射设计

SL 的类型在 IR 层已分四类（`IRMetaClassKind`）：`Class=0`、`Enum=1`、`Data=2`、`Interface=3`。
导出器按 `IRMetaClass.metaClassKind` 分派，给出各自的 AOT 布局。

#### 4.10.1 `data` 类型

| 项 | 设计 |
|----|------|
| 语义 | 结构体式数据载体（JSON/record 风格），成员为 `MetaMemberData`；可带 static / 实例方法 |
| IR 表示 | `IRMetaClass(kind=Data)`；成员由 `CreateMemberDataFromMetaData` 同时写入 `staticIRMetaVariableList`（静态初始化视角）与 `localIRMetaVariableList`（实例布局视角） |
| 实例布局 | `struct sl_object` 头部 + 按 `localIRMetaVariableList` 顺序排列的字段（对齐规则与 CVM 一致），与 class 实例**共用同一对象模型**（GC、类型分派、`sl_abi.h` 完全一致） |
| 静态区 | 按 `staticIRMetaVariableList` 生成 `sl_rt_static_init`（字段默认值/常量） |
| MLIR 映射 | 创建 `NewObject` → `sl_rt_alloc_data(typeId, size)`；字段访问 `LoadNotStaticField`/`StoreNotStaticField1/2` → 对象头后偏移 `gep + load/store`；静态字段 → 静态区 `gep` |
| 动态 data（`isDynamic`） | 匿名/动态 data 字面量 `{ a = 1, b = 2 }` 运行时构造为对象，AOT 侧仅提供 `sl_rt_anon_data_new(fieldCount, typeIds…)` 桥接，不展开为静态布局 |

#### 4.10.2 `enum` 类型

| 项 | 设计 |
|----|------|
| 语义 | `enum Name : underlying { member = value; ... }`；底层允许 `uint8..uint64` / `string` / `float` / `data` / 关键字 `data`(动态) / `Error` |
| IR 表示 | `IRMetaClass(kind=Enum)`；成员全部为 **static** `IRMetaVariable`（`CreateMemberDataFromMetaEnum`），且运行时类型统一为 **`Core.Member` 对象**（`IRMetaVariable` 构造器：`m_IRMetaType = Core.Member`）；自动生成 `values` 静态数组 |
| 访问形式 | `enumName.Member` → `LoadStaticField`（枚举成员本就是类的 static 字段），**无需新 opcode** |
| AOT 策略（分层） | 枚举成员的**运行时形态是 `Member` 对象**（含底层 value）。前期**保守策略**：成员在静态初始化阶段按 `Member` 对象构造（`sl_rt_enum_member_new(underlyingValue)`），AOT 代码对枚举的访问等价于静态字段访问，行为与 CVM 完全一致 |
| 后期优化（策略 B） | 底层为整型的枚举，若使用点为**常量比较/Switch**，可编译期为 `arith.constant` + `cf.switch`，完全消除 `Member` 对象——需在优化 pass 前做"枚举值传播 + 对象消除"，作为可选项 |

#### 4.10.3 `interface`

| 项 | 设计 |
|----|------|
| IR 表示 | `IRMetaClass(kind=Interface)` |
| AOT | interface 本身不产生代码；涉及 `CastClass` / `CallVirt` / `CallDynamic` 的调用点一律走运行时（CVM 解释），**前期不在 AOT 编译范围**（见 §4.11） |

### 4.11 分层编译策略：哪些函数走 MLIR（AOT）、哪些走 CVM

> 核心原则：**函数级分层，AOT 与 CVM 共享同一运行时**。大部分代码 AOT，小部分（虚分发/异常/闭包/动态）保留 CVM，二者可互相调用。

#### 4.11.1 判定规则

**AOT 编译候选（前期阶段 1~4 只编译这些）：**

| # | 条件 | 依据（IR 字段） |
|---|------|-----------------|
| 1 | static 方法 | `IRMethod.isStatic == true`：无 this、无虚分发、签名+方法体确定 |
| 2 | final 实例方法 | `isFinal == true`：不可被 override，调用可静态绑定 |
| 3 | 非虚实例方法 | 不在 `nonStaticVirtualMetaMemberFunctionList`（文件级定义且无 override 链） |
| 4 | 私有方法 | 无外部可见性，必然单实现 |
| 5 | 调用点可静态解析 | `CallStatic`，或 `CallVirt` 目标编译期已知（final / 单实现） |

**不编译（保留 CVM 解释）：**

| # | 情形 | 原因 |
|---|------|------|
| 1 | `CallVirt` / `CallDynamic`（多态分发） | 需 vtable/运行时解析 |
| 2 | abstract / interface 方法 | 无方法体 |
| 3 | 模板函数 `isTemplateFunction` | 需按实例化逐个展开（后期可支持） |
| 4 | 闭包 `NewClosure` / `CallClosure` | 阶段 5 前不支持 |
| 5 | 异常 `BeginTry`/`Throw`/`EndTry`/`LeaveTry`/`EndFinally` | 阶段 5 前不支持 |
| 6 | 动态/反射 `LoadConstType` / `CastClass` / `CallDynamic` | 运行时行为复杂 |
| 7 | 仅有签名无方法体（ref module） | 无法编译 |

**回退规则（粒度 = 函数）**：一个函数只要包含任一"不编译"特征（异常指令、闭包指令、动态调用、模板、虚调用无法静态解析），该函数**整体**标记 `aot=false`，运行时走解释器。**同一份 `module.json` 仍然完整生成**（CVM 可全量解释），AOT 只是在其上叠加一个增量层——保证任何时刻可回退、可对拍。

#### 4.11.2 混合执行架构

```
SL 程序
 ├── AOT 部分（static + final + 非虚方法）→ MLIR → a.obj → 原生代码
 ├── CVM 部分（虚方法/异常/闭包/动态）→ module.json → CVM 解释器
 └── 二者共享同一个运行时上下文（GC / 类型表 / 静态区 / 常量池）
```

互操作 ABI（三态标记）：

| 状态 | 含义 | 产物 |
|------|------|------|
| `aot` | 编译进原生代码 | MLIR 函数 + 注册进运行时 **native 表** |
| `vm` | 保留解释执行 | 留在 `module.json`（解释器路径） |
| `bridge` | 两侧互相调用的边界 | `sl_rt_invoke_vm()` / native 表查表 |

1. **CVM → AOT**：编译函数以统一签名注册进运行时 native 表——
   `intptr_t (*sl_native_fn)(sl_rt_ctx*, sl_value* args, int argc)`。
   解释器执行 `CallStatic`/`CallVirt` 时**先按 methodId 查 native 表**：命中 → 转调原生函数；未命中 → 原解释路径。
2. **AOT → CVM**：编译代码对未编译函数（虚方法、异常路径等）发射 `sl_rt_invoke_vm(methodId, this, args…)`，运行时查找并执行解释器方法。
3. **数据共享**：对象 / 静态区 / 常量池完全共享——AOT 分配的对象与解释器分配的对象布局一致（同一 `sl_abi.h`），GC 同时扫描 AOT 根与解释器帧根。
4. **静态初始化**：`sl_rt_static_init` 一次性完成（AOT 常量 + 枚举成员对象 + 字符串），解释器与 AOT 代码看到同一份静态区。

#### 4.11.3 与路线图的衔接

| 阶段 | AOT 覆盖范围 |
|------|--------------|
| 1~4 | 仅 `static` + `final` + 非虚实例方法；其余函数 `vm`（CVM 解释） |
| 5 | 异常 / 闭包指令支持后，对应函数转入 `aot` |
| 6 | 策略 B（SSA）+ 去虚拟化（devirtualization）后，扩大 `aot` 覆盖面 |

---

## 5. 运行时集成 `slruntime`（对应缺口 G4/G5）

### 5.1 ABI 头文件 `sl_abi.h`（AOT 后端与 C 运行时共享）

```c
// 值类型（栈槽 / 形参统一载体）
typedef struct sl_value {
    enum sl_kind { SL_I64, SL_F64, SL_OBJ } kind;
    union {
        int64_t  i64;
        double   f64;
        struct sl_object* obj;   // 所有对象基类指针（含 string/array/closure）
    } data;
} sl_value;

// 对象头（复用 CVM 的 64 位 VMObjectHeader，含 GC 标记位）
typedef struct sl_object {
    uint64_t header;   // typeRef + gcMark + 其他标记（与 CVM 布局一致）
    // ... 后续字段由运行时按类型布局
} sl_object;
```

**关键约束**：`sl_abi.h` 中的对象布局**必须与 CVM `VMObjectHeader` 一致**（`csimple_lang` 的 `core/object.h` 等），否则 GC 与类型分派全错。以 CVM 头文件为准，AOT 侧只引用、不重复定义。

### 5.2 运行时桥接层（新增 C 文件）

| 模块 | 内容 |
|------|------|
| `sl_rt_core.c` | 上下文创建/销毁、对象分配（带类型）、`sl_rt_type_get` |
| `sl_rt_array.c` | 数组 new/get/set（含下标越界检查） |
| `sl_rt_string.c` | 字符串构造/拼接/转换/比较 |
| `sl_rt_closure.c` | 闭包创建/调用/上下文捕获（对应 `NewClosure` 族） |
| `sl_rt_syscall.c` | 系统调用映射实现（`Print` 等） |
| `sl_rt_exception.c` | 异常帧（try/catch/finally）运行时实现 |
| `sl_rt_gc.c` | 复用 CVM GC：注册 AOT 侧 GC 根 |

### 5.3 GC 集成

- AOT 编译代码中**持有的对象指针**（局部变量、表达式中间值）必须在 GC 可达性分析中可见。
- **策略 A**：内存栈（`memref`）统一在函数入口注册为 GC 根区域，运行时 GC 扫描整块栈；
- **策略 B**：函数帧按活跃 SSA 值构造 `sl_rt_gc_roots` 列表，GC 前调用 `sl_rt_gc_add_roots(ptr_list, n)`。
- 分配点（`sl_rt_alloc_*`）内部自动执行 safe-point 检查（复用 CVM GC 的触发机制）。

### 5.4 `main` 启动器（C）

```c
// main.c（由 AOT 导出器生成或模板化）
#include "sl_abi.h"
extern int _main_(void);   // 编译后的 SL 入口

int main(int argc, char** argv) {
    sl_rt_ctx* ctx = sl_rt_create_context();   // 初始化 GC/类型表/静态区
    int code = _main_();
    sl_rt_destroy_context(ctx);
    return code;
}
```

> 若 SL 程序读取命令行参数，`_main_` 签名扩展为 `(argc, argv)` 并透传。

---

## 6. 工具链串联（对应缺口 G2/G3）

### 6.1 工具清单与路径（已验证存在）

| 工具 | 路径 | 用途 |
|------|------|------|
| `mlir-opt.exe` | `f:/project/lang/llvm-project/build/Release/bin/mlir-opt.exe` | 降级/优化 |
| `mlir-translate.exe` | `…/bin/mlir-translate.exe` | MLIR → LLVM IR |
| `llc.exe` | `…/bin/llc.exe` | LLVM IR → 目标文件 |
| `llvm-as.exe` | `…/bin/llvm-as.exe` | 可选（LLVM IR 汇编） |
| `mlir-runner.exe` | `…/bin/mlir-runner.exe` | **调试利器**：不需链接即可执行 MLIR（先于 llc 验证语义） |
| ~~`clang.exe`~~ | **不存在** | 缺 |
| ~~`lld-link.exe`~~ | **不存在** | 缺 |

### 6.2 `mlir-opt` 降级链（修复 G2）

**现状缺陷**：`MLIRToolchain.cs` 调 `mlir-opt` 时**未传任何 pass**，高层方言无法降级，`mlir-translate --mlir-to-llvmir` 会失败。

**修正后的 pass 链**：

```powershell
$BIN\mlir-opt a.mlir --canonicalize `
  --convert-arith-to-llvm --convert-cf-to-llvm `
  --convert-func-to-llvm --convert-memref-to-llvm `
  -o a.opt.mlir
```

> 可选增强：`--cse`、`--enable-loop-inversion` 等优化 pass 在功能闭环后加入（§8 阶段 6）。

### 6.3 `mlir-translate` / `llc`

```powershell
$BIN\mlir-translate --mlir-to-llvmir a.opt.mlir -o a.ll
$BIN\llc a.ll -filetype=obj -mtriple=x86_64-pc-windows-msvc -o a.obj
```

- `-mtriple` 需与目标平台一致（Windows MSVC 或 GNU 变体），保证 C ABI 匹配 CVM。
- 调试期可先 `-filetype=asm` 人工检查。

### 6.4 链接器方案（对应 G3，三选一）

| 方案 | 做法 | 优劣 |
|------|------|------|
| **A（推荐）增量构建 lld** | 在 `llvm-project/build` 中构建 `lld` target：`cmake --build build --target lld`（若当前配置未含 lld，则新开 `build-lld` 目录，以 `-DLLVM_ENABLE_PROJECTS=lld` 重新 configure，复用已有 Release 依赖） | 全 LLVM 生态；`lld-link` 直接输出 PE；无外部依赖 |
| B 使用 MSVC `link.exe` | 机器装有 VS 时，`vcvars64.bat` 环境下 `link a.obj slruntime.lib /SUBSYSTEM:CONSOLE /ENTRY:mainCRTStartup` | 零新增构建；依赖 VS 环境 |
| C 增量构建 `clang` | `cmake --build build --target clang` | 顺带获得 C 编译器；构建重、耗时 |

> 无论选哪个，`MLIRToolchain.cs` 最终统一通过环境变量/配置暴露链接器路径与命令，保持后端可替换。

### 6.5 调试手段（重要）

- **`mlir-runner`**：`mlir-runner a.mlir -e main -entry-point-result=...` 可**不经 llc/链接**直接执行 MLIR（JIT 或解释），适合先验证导出器语义，再进入 AOT 全链。
- 保留 `module.json` + CVM 解释路径作为 **golden reference**：同一程序跑解释版与 AOT 版，对比输出，作为回归基准。

---

## 7. 模块划分与改动清单

### `simple_language`（C#，主实现）

| 文件 | 动作 |
|------|------|
| `source/Front/Export/MLIR/MLIRExporter.cs` | **重写**：实现 §4 全部映射（先策略 A） |
| `source/Front/Export/MLIR/MLIRToolchain.cs` | 修正 pass 链（G2）；补链接步骤；链接器路径可配置 |
| `source/Front/Export/ExportLangManager.cs` | 接通 MLIR/AOT 分支（G6） |
| `source/Front/Export/MLIR/SysCallMap.cs`（新增） | 系统调用映射表（§4.8），与 C 侧共用 |
| `source/Front/Export/AOT/LLVMEmitter.cs` | 保留为参考，不改 |

### `csimple_lang`（C 运行时）

| 文件 | 动作 |
|------|------|
| `aot/sl_abi.h`（新增） | ABI 定义（引用 CVM 对象头） |
| `aot/sl_rt_*.c`（新增） | 桥接层实现（§5.2），CMake 新增 `slruntime` 静态库 target |
| `aot/main.c.tpl`（新增） | 启动器模板（§5.4） |

### `llvm-project`

| 项 | 动作 |
|----|------|
| 构建 `lld` | 决策 D4 方案 A |

### `gosimple_lang`（可选第二阶段）

| 项 | 动作 |
|----|------|
| `front/Export/MLIR/` | 按同一设计实现 Go 版导出器（目录已预留） |

---

## 8. 落地路线图（里程碑 + 验收标准）

| 阶段 | 内容 | 验收标准 |
|------|------|---------|
| **0. 环境打通** | 构建 lld（或选定 link.exe）；用手写最小 MLIR 跑通 ③④⑤⑥ 全链 | 手写 MLIR → `a.exe` 可运行 |
| **1. 最小闭环** | `MLIRExporter` 支持数值运算 + `CallStatic` + `Ret`；接通 `ExportLangManager`；用 `mlir-runner` 先验证语义；**按 §4.11 分层策略，只编 `static`/`final`/非虚方法，其余函数标记 `vm` 走 CVM** | `add(1,2)` 的 SL 源码 → exe 输出 `3`；非 AOT 函数在 CVM 侧运行正常 |
| **2. 完整指令集** | 覆盖 §4.5 全部 `EIROpCode`（除异常/闭包）；实现 `aot/vm/bridge` 三态标记与 native 表互操作（§4.11.2） | SLIR 指令集映射无遗漏；**AOT 函数调 CVM 函数、CVM 调 AOT 函数均正确**；解释版与 AOT 版输出一致 |
| **3. 数据与类型** | 静态字段、常量池、类型对象、对象布局 | 含字段/静态变量的程序正确 |
| **4. 运行时桥接** | `sl_rt_*` 系统调用映射（Print/字符串/数组/Math） | 调用内置方法的程序正确 |
| **5. 异常/闭包/GC** | try/catch/finally、闭包、GC 根注册 | 异常路径、闭包程序正确；循环分配内存稳定 |
| **6. 优化** | 切换策略 B（SSA 虚拟栈）；`mlir-opt` 加优化 pass | 性能对比提升；回归通过 |

> 每个阶段都建议**双跑对比**：`CVM 解释版 vs MLIR AOT 版` 输出一致即为通过（§6.5 golden reference）。

---

## 9. 风险与对策

| # | 风险 | 对策 |
|---|------|------|
| R1 | 对象布局/GC 位与 CVM 不一致导致崩溃 | `sl_abi.h` 以 CVM 头文件为准（§5.1）；增加布局断言测试 |
| R2 | `mlir-translate` 对未完全降级的 dialect 报错 | 严格按 pass 链降级到 LLVM dialect；用 `mlir-opt --mlir-print-ir-after-all` 调试 |
| R3 | 栈机无类型信息导致 `Add` 等指令歧义 | 复用 `LLVMEmitter` 的类型推断 + `IRMetaType`；策略 B 用 SSA 类型传播 |
| R4 | 异常/闭包映射复杂拖慢进度 | 阶段一全部走运行时实现，语义正确优先，性能后置（§4.5） |
| R5 | lld 构建失败/耗时 | 方案 B 备选（VS `link.exe`）；链接器接口抽象可替换 |
| R6 | 回归失控 | 保留解释执行路径为 golden reference，自动化对拍（§6.5） |
| R7 | GC safe-point 在长算数段缺失 | 分配点触发 GC 检查（§5.3），必要时插入显式 safe-point |

---

## 10. 附录

### 10.1 完整命令示例（Windows / PowerShell）

```powershell
$BIN = "f:\project\lang\llvm-project\build\Release\bin"

# ② SLIR → MLIR（C# 导出器输出 a.mlir）
# ③ 降级
& "$BIN\mlir-opt" a.mlir --canonicalize `
  --convert-arith-to-llvm --convert-cf-to-llvm `
  --convert-func-to-llvm --convert-memref-to-llvm -o a.opt.mlir
# ④ MLIR → LLVM IR
& "$BIN\mlir-translate" --mlir-to-llvmir a.opt.mlir -o a.ll
# ⑤ LLVM IR → 目标文件
& "$BIN\llc" a.ll -filetype=obj -mtriple=x86_64-pc-windows-msvc -o a.obj
# ⑥ 链接（方案 A：lld-link；方案 B：MSVC link）
& "$BIN\lld-link" a.obj slruntime.lib /SUBSYSTEM:CONSOLE /OUT:a.exe
# 或（方案 B）
# link a.obj slruntime.lib /SUBSYSTEM:CONSOLE /OUT:a.exe
```

### 10.2 最小 MLIR 样例（阶段 0 用）

```mlir
module {
  func.func @main() -> i32 {
    %c1 = arith.constant 1 : i32
    %c2 = arith.constant 2 : i32
    %0 = arith.addi %c1, %c2 : i32
    return %0 : i32
  }
}
```

### 10.3 调试建议命令

```powershell
# 看降级中间过程
& "$BIN\mlir-opt" a.mlir --mlir-print-ir-after-all --convert-arith-to-llvm -o NUL
# 不经链接验证语义（mlir-runner）
& "$BIN\mlir-runner" a.opt.mlir -e main -entry-point-result=i32
```

### 10.4 参考资料

- `source/Front/Export/AOT/LLVMEmitter.cs` —— 策略 A 的移植基线
- `source/Front/Export/MLIR/MLIRToolchain.cs` —— 现有工具链封装（需按 §6.2 修正）
- `csimple_lang` 的 `core/object.h` / GC —— `sl_abi.h` 布局依据
- `llvm-project/build/Release/bin` —— 已构建工具链
