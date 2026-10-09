# MLIR AOT 导出实现说明与后续实现指南

> 位置：`Front/Export/MLIR/`
> 涉及文件：`MLIRExportManager.cs`（编排）、`MLIRExporter.cs`（发射器，核心）、
> `MLIRToolchain.cs`（外部工具链）、`MLIRAotTypes.cs`（结构体类型注册表）
> 本文档同时记录：已完整实现的指令族、no-op/占位放行的指令族、导出期显式 Fail 的情形、
> GPU 路径限制、发射器基础设施约定，以及后续继续实现时的路线建议。

---

## 1. 管线架构总览

```
stage 1   收集 @AOT() 候选方法（isAot && isStatic && !isTemplateFunction）
stage 2   MLIRExporter.ExportModuleToFile → aot.mlir（+ 方法清单 manifest）
stage 3   MLIRToolchain.TryBuildAotDll：
            mlir-opt           （lowering pass 链）      → aot.opt.mlir
            mlir-translate     （--mlir-to-llvmir）      → aot.ll
            llc -filetype=obj                            → aot.obj
            link.exe /DLL /NOENTRY /EXPORT:sym...        → aot.dll
stage 3.5 方法清单并入 module.json 的 "aot" 字段
stage 4   VM 启动时读 module.json 加载 aot.dll，命中即直接调用本地代码
stage 5   反向桥（reverse bridge）：AOT 代码回调 CVM 解释器
            —— CallStatic 走此路径：dll 内调用宿主导出的
            @sl_aot_bridge_init(fnptr) 注入的 invoke_vm 函数指针，
            callee 的 Front 预计算 int method id（FNV-1a）以 i32 常量直传，
            C 侧 vm_find_method_by_id 解析；桥 ABI：
            int64 fn(void* ctx, int32 method_id, SLAotValue* args,
                     int32 argc, SLAotValue* ret)
```

要点：

- **失败即回退**：任一方法在 stage 2/3 失败（发射 Fail 或工具链报错），该方法回退 CVM 解释器执行，不影响其它方法。整个导出可由工程 jsonc `"export": { "aot": { "enabled": false } }` 关闭；`"buildDll": false` 跳过 dll 构建只出 mlir。
- **方法内 Fail 是"软失败"**：no-op 占位值（见 §3.2）在运行期第一次被真实使用时才触发失败并标记方法回退。而发射器 `throw Fail(...)`（§3.3）是"硬失败"，导出期直接拒绝。
- **无环境变量配置**：工具链路径不走环境变量（`ToolchainPaths.Resolve()` 固定探测，见 §8）；AOT 开关走工程 jsonc `export.aot` 段（`MLIRExportConfig.FromProject()`）。CVM 侧同样不读任何 SIMPLELANG_* 环境变量——前后端的唯一关联是 module.json 的 `aot` 字段（Ref Module 机制），A 机编译产物可整体拷到 B 机运行。仅剩 GPU 开发期调参用的 `SIMPLELANG_GPU_RUNTIME` / `SIMPLELANG_GPU_ARCH` 两个环境变量（均有探测默认值）。
- **链接库**：CPU dll 仅链 `kernel32.lib msvcrt.lib ucrt.lib libvcruntime.lib`（无 clang_rt/compiler-rt）。这一点决定了某些 lowering 不可用（见 §6.1）。
- **GPU 方法**：@GPU 方法走 gpu.func 发射 + GPU 专用 pass 链 + sl_gpu_runtime 链接；限制见 §4。

---

## 2. 核心数据模型与 ABI

### 2.1 SLType（槽位/栈值类型域）

| SLType | 含义 | ABI kind |
|---|---|---|
| I64 | 所有整数/布尔/字符（统一 i64 位模式） | 0 |
| F64 | 双精度（i64 位模式中存 f64 位） | 1 |
| F32 | 单精度域：**i64 位模式中恒存精确 f64 加宽值**（不是原始 f32 位） | 1 |
| Struct | 数据结构体，带 ClassId（`#id`），指向模块类型表 | 2 |
| ObjRef | 不透明 VMObject*（String/用户类/接口/Core.Object） | 3 |
| Array 形态 | I64 的变体：i64 位中存 VMArray*（LoadArrayIndex* 等按此解释） | 0 |

### 2.2 SlotTable / ProfileVal

- 每个方法的参数/局部槽位在导出期由 `SlotTable` 从 `IRMetaVariable` 的 `IRMetaClass` 名解析为 I64/F64/F32/Struct/Array/ObjRef。
- `ResolveVarType` 的拒绝规则（见 §3.3）：模板参数 T、Enum、Member、窄浮点类型名。
- 结构体槽位携带 ClassId，成员访问发射时到 `MLIRAotTypes` 模块类型表查 native 布局（GEP 偏移）。
- 类型表注册是**惰性**的：方法发射过程中遇到才注册 `!sl_t_*` 别名，所以模块组装是两阶段的（先全部方法，再补 alias 块）。

### 2.3 栈模拟

- 发射前先做**全方法线性栈模拟**（`Profile*` 系列）验证一致性（每个前驱块汇合处栈型必须一致，否则 Fail "inconsistent stack profile"），模拟通过后逐块真实发射。
- 不可达基本块不发射。

---

## 3. 指令实现状态总表

### 3.1 真实翻译（完整实现）

**常量与槽位**
| opcode | 说明 |
|---|---|
| Nop / Label | 空操作/标签（Label 用于块名） |
| LoadConstUInt8/Int8/Int16/UInt16/Int32/UInt32/Int64/UInt64/Boolean | CI64 常量 |
| LoadConstFloat32/Float64 | CI64 位模式 |
| LoadConstFloat8_E4M3/E5M2/Float16/Float16_Brain | `NarrowFloatBits`：窄浮点位模式 →（C# 参考实现 Float816Convert）f32 → f64 位模式，以 F32 域入栈 |
| LoadArgument / LoadLocal / StoreLocal / StoreArgument / StoreReturn / Ret | 槽位读写（StoreReturn 仅 slot 0） |
| LoadBegin | 函数序言标记，no-op（栈模拟中即空） |

**算术/位/比较/逻辑**
| opcode | 说明 |
|---|---|
| Add/Minus/Multiply/Divide/Modulo | 整数与浮点各自分派（含符号处理） |
| InclusiveOr / Combine(And) / XOR / Shr / Shi / Not / Neg | 位运算仅接受整数操作数（浮点操作数 Fail） |
| Ceq/Cne/Cgt/Cge/Clt/Cle | 比较产生 i1 |
| And / Or | 逻辑与/或 |

**控制流**
| opcode | 说明 |
|---|---|
| Br / BrLabel / BrFalse / BrTrue / Break / Jmp | Break/Jmp 与 Br 同编码 |
| Beq/Bge/Bgt/Ble/Bne | 条件分支（比较+cond_br） |
| Switch | 展开为一串合成块的 cond_br 链；payload 长度必须匹配；必须有 default |
| Dup / Pop | 栈操作 |

**类型转换**
| opcode | 说明 |
|---|---|
| Convert_I8/SI8/I16/UI16/I32/UI32/I64/UI64 | 整数截断/扩展（shli/shrui/shrsi） |
| Convert_R8 | → f64 |
| Convert_R4 | → f32 域（结果仍以精确 f64 加宽位模式表示） |
| Convert_F8E4M3/F8E5M2/F16/F16B | **窄浮点往返**：整数位运算 RNE 编码+解码，详见 §6 |

**数组（Array&lt;T&gt;，4/8 字节标量元素）**
| opcode | 说明 |
|---|---|
| LoadArrayIndex | 常量下标：[array] → [element] |
| LoadArrayIndexField | 变量下标：[array, index] → [element] |
| StoreArrayIndex | 常量下标存（payload [flag:1]） |
| StoreArrayIndexField | 变量下标存 |
| CallVirt | **仅内联数组 getter**（arr.length 等）；其它虚调用 Fail |

**结构体（Data 类）**
| opcode | 说明 |
|---|---|
| LoadNotStaticField | 成员读取：模块类型表查 native 偏移 → GEP/load |
| StoreNotStaticField1 | 花括号初始化：[inst,val] → [inst] |
| StoreNotStaticField2 | 成员存储：[inst,val] → [] |
| 引用类型成员（类/接口/数组/字符串成员） | Fail（slot kind 见 §3.3） |

**调用**
| opcode | 说明 |
|---|---|
| CallStatic | **stage-5 反向桥**回调 CVM。限制：callee 不能多返回值、不能返回 struct/objref（Fail）；参数按槽位 ABI marshal |

**防御性翻译（isAot 方法实际不会发射，但已列出）**
- `Store*ConstValue` 全族（O3 常量融合存储；`IRVariable.TryCreateConstValueStore` 对 isAot 关闭）

### 3.2 no-op / 占位放行（CPU 路径）——运行期软失败回退

> 策略：这些指令**能通过导出**（发射一行注释 + 合法栈效果），让整条指令集在导出器中
> round-trip。产物中的值是 `ObjRef` 占位（`i64 0` + kind=3 标记），第一次被真实使用
> （算术/GEP/marshal）时失败，方法整体标记失败回退解释器。
> **用途**：不含这些指令实际数据流的方法依然能 AOT 化。

| opcode | 栈效果 | 发射内容 | 未实现的真实语义 | 未来实现方向 |
|---|---|---|---|---|
| LoadConstNull | → ObjRef(0) | 注释 + 占位 | null 常量本身无害 | 保持 |
| LoadConstString | → ObjRef 占位 | 注释 + 占位 | AOT 侧无字符串池 | 需要模块级字符串 global + 运行时 interning 桥 |
| LoadConstType | → ObjRef 占位 | 注释 + 占位 | 运行时类型句柄 | 需要类型句柄桥（method table id） |
| LoadStaticField | → ObjRef 占位 | 注释 + 占位 | 无静态字段桥 | 需要静态字段地址桥（宿主提供 slab/偏移） |
| LoadGlobal | → ObjRef 占位 | 注释 + 占位 | 无全局桥 | 同上 |
| NewObject / NewTemplateObject | → ObjRef 占位 | 注释 + 占位 | CVM 堆分配 | 需要 AOT 分配桥或逃逸分析消除 |
| NewArray | [len] → ObjRef 占位 | 注释 + 占位 | CVM SLArray 分配 | 需要 VMArray 分配桥（调用宿主分配函数） |
| NewClosure | [ctx] → ObjRef 占位 | 注释 + 占位 | 解释器专用闭包对象 | 需要 AOT 闭包表示 |
| AllocClosureContext | → ObjRef 占位 | 注释 + 占位 | 解释器专用 | 同上 |
| CallClosure | [closure,args] → ret? | 注释 + 占位 | 解释器专用派发 | 需要闭包调用约定 |
| CallDynamic | [args] → ret? | 注释 + 占位（需可解析 callee 元数据，否则 Fail） | 解释器专用动态派发 | 若未来语言层加虚调用，需 vtable 桥 |
| CallSystemMethod | [args] → ret? | 注释 + 占位（需 call package，否则 Fail；declared 未注册也 Fail） | CVM 运行时方法调用 | 逐个映射为 AOT 侧等价（如 Math.* → libm） |
| Convert_ToString | [v] → ObjRef 占位 | 注释 + 占位 | 无字符串桥 | 需要数值→字符串运行时桥 |
| CastClass | [v] → [v] | 注释 + passthrough | as 式引用转型，VM 保留原值 | 引用语义落地后可加真实 check（目前无引用域） |
| StoreStaticField / StoreGlobal | [v] → [] | 注释 + pop+drop | 无桥 | 与 Load 对称 |
| Throw | [v] → [] | 注释 + pop+drop | 无展开机制 | 需要 C++ 异常或宿主 longjmp 桥 |
| BeginTry / EndTry / LeaveTry / EndFinally | — | 注释 | try 帧在 AOT 中无操作 | 同上（先决条件：Throw） |
| BeginChecked / EndChecked / BeginUnchecked / EndUnchecked | — | 注释 | checked 上下文无溢出陷阱 | 需要溢出检测算术变体（arith.addi + overflow intrinsics） |

**注意**：栈模拟器（Profile*）对以上指令同样按占位栈效果建模，保证后续栈一致性校验能通过。

### 3.3 显式 Fail（导出期硬拒绝）——方法直接回退解释器

**类型解析期（ResolveVarType / SlotTable）**

| 条件 | 原因 | 解除路径 |
|---|---|---|
| `irMetaType.templateIndex >= 0`（模板参数 T 槽位） | 模板方法被去重回定义级 IR，签名里 T 无逐实例布局；静默擦除为 ObjRef 会错 | 需要逐实例模板实例化管道（strip/replace T），见 §7.1 |
| Enum 类型槽位 | VM 打包形式 AOT 无法重建 | 语言层用 Int64 算术替代，或 AOT 侧把 enum 当 I64 |
| `Member` 类型槽位 | 同上 | — |
| 窄浮点类型名槽位（Float8_E4M3/Float8_E5M2/Float16/Float16_Brain 变量） | CVM 运行时值既非 VMObject（kind=3 marshal 失败）也非浮点（kind=1 失败）；eval 栈以独立 slot tag 存原始位模式，无 ABI 槽可承载 | 见 §7.2（窄浮点槽位 ABI） |

**指令发射期**

| 条件 | 说明 |
|---|---|
| 不在 s_SupportedOps 的 opcode | "unsupported opcode" |
| 栈下溢 / Dup/Pop 超出栈深 | 结构性错误 |
| 算术/位/比较/Neg/Convert 等的**数组形态操作数** | 数组是 VMArray*，不能当标量 |
| 位运算/移位的**浮点操作数** | 类型域错误 |
| Switch payload 畸形 / 长度不匹配 / 无 default 在方法尾 | 结构性错误 |
| 跳转目标越界 / 方法尾出现条件分支 | 结构性错误 |
| 前驱块汇合处栈 profile 不一致 | 结构性错误 |
| CallVirt 非数组 getter / 接收者非数组 | 仅内联数组 getter |
| CallStatic：callee 多返回值 / 返回 struct 或 objref / 无参表 / 参数槽位不可解析 / 参数为 struct（objref 传参本身允许按 kind=3？——实际按 ABI marshal，struct 参数暂不支持） | 反向桥 marshal 能力边界 |
| CallDynamic/CallClosure 无可解析 callee、CallSystemMethod 无 call package 或声明未注册 | 元数据缺失 |
| LoadNotStaticField/Store*：接收者非 Struct、ClassId 未注册、成员索引越界、**引用类型成员**（slot 不支持）、成员存储类型不匹配 | 结构体成员桥边界 |
| StoreReturn index != 0 / 方法无返回值 | 仅支持单返回槽 0 |
| CallSystemMethod 声明未注册 | — |

**GPU 专用 Fail 见 §4。**

---

## 4. GPU 路径限制（GpuMethodEmitter）

`@GPU` 方法发射为 `gpu.func`（grid-stride 并行循环，thread_id/block_dim/block_id/grid_dim），
额外限制（`CheckSupported`，导出期 Fail）：

**一票拒绝清单（s_GpuUnsupportedOps + 显式判断）**

| 类别 | opcode | 原因 |
|---|---|---|
| 占位族 | LoadConstString/Type、LoadStaticField/Global、NewObject/NewTemplateObject/NewArray/NewClosure/AllocClosureContext、Convert_ToString | CPU 路径也仅是占位，内核中无意义 |
| 值丢弃族 | StoreStaticField/Global（含 ConstValue 融合形态）、Throw | 无桥 |
| 解释器派发 | CallDynamic / CallClosure / CallSystemMethod | 无桥 |
| 引用转型 | CastClass | 内核无引用域 |
| 反向桥 | CallStatic | **显式拒绝**：AOT→CVM 桥不能在 gpu.func 内调用 |
| 结构体成员 | LoadNotStaticField / StoreNotStaticField1/2（含 ConstValue） | 需要宿主侧 native buffer view，gpu.func 内不存在 |

**结构性限制（同样 Fail）**

- GPU 方法必须 void（无返回值）
- 参数/局部不允许 Struct/ObjRef 槽位
- 方法体必须能识别出一个可并行的 for 循环（`i += 1` 步进）；识别失败 Fail
- gpu.func 内用 `llvm.alloca` 局部 slab（不用 memref）

**GPU 已继承的能力**：所有数值/位/比较/分支/整数转换/窄浮点四格式 Convert（EmitLowpRoundTrip
非虚方法，整数实现所需 op 在 NVVM 全部合法，自动继承；常量提升双路径安全，见 §5）。

---

## 5. 发射器基础设施与命名约定（写新发射代码前必读）

### 5.1 常量/临时助手（MethodEmitterBase 域）

| 助手 | 产生 | 前缀 | 方言 | 备注 |
|---|---|---|---|---|
| `NV()` | 临时 SSA 名 | `%v{N}` | — | 每方法计数器 |
| `CI64(long)` | i64 常量 | `%c_{v}` | arith.constant | 整数/浮点位模式常量 |
| `CK32(int)` | i32 常量 | `%k_{v}` | llvm.mlir.constant | |
| `CIDX(int)` | index 常量 | `%x_{v}` | arith.constant | |
| `CA32(int)` | i32 常量 | `%a_{v}` | arith.constant | 窄浮点用（移位量等） |
| `CAF32(string)` | f32 常量 | `%fa_{N}` | arith.constant | 窄浮点次正规缩放因子（字面量去重） |

字典缓存（同值复用）：`m_K32Consts` / `m_ArithI32Consts` / `m_ArithF32Consts` 等。

### 5.2 关键陷阱

1. **移位量必须是 SSA 操作数**：此 MLIR 构建的 `arith.shrui/shli/shrsi` 不接受字面量
   （报 "expected SSA operand"）。**所有移位/掩码常量必须先经 CA32() 命名再引用**。
2. **常量提升位置**：`EmitConst` 追加到 `m_ConstSb`，组装点在函数体开头——CPU 在
   func 体内（组装于 L867 附近），GPU 在 gpu.func 体内（L3787 附近），两处都
   dominates all uses，双路径安全。新增助手时必须确认这点。
3. **方言选择**：函数体内数值运算用 arith.*；llvm.bitcast/f32↔i32 位重解释用 llvm 方言。
4. **毒值规避**：`arith.select` 的数据流会**全分支求值**，非选中分支的移位量可能为负
   （UB/poison）。窄浮点次正规路径的 shift 已 clamp 到 [1,31]（两次 cmpi+select）。
   未来写类似 select 包裹的移位时必须同样 clamp。
5. **F32 域表示**：栈上 F32 槽位的 i64 位模式**恒为精确 f64 加宽**。发射任何
   "f32 语义" 运算前先 `arith.truncf` 到 f32，算完 `arith.extf` 回 f64 再 bitcast 存栈。
6. **窄浮点 opcode 拼写**：`Convert_F8E4M3`（无下划线）vs `LoadConstFloat8_E4M3`
   （有下划线）。易错。

---

## 6. 窄浮点四格式 Convert 实现详解（当前最新完成项）

### 6.1 设计决策：为什么不用原生 truncf/extf

f16/bf16 若走原生 `arith.truncf/arith.extf` 链，**llc 在通用 x86-64 目标上会下化为
compiler-rt 软浮点调用**（`__truncsfhf2/__extendhfsf2/__truncsfbf2` 等），而 AOT dll
只链 libvcruntime.lib（无 clang_rt），链接失败。

**最终方案（已确认）**：四格式统一为**参数化整数位运算**实现，与 e4m3/e5m2 同构。
代价是每条 Convert 展开为 ~90 行 MLIR，收益是：

- 无运行库依赖，CPU/GPU 双路径可用（NVVM 下全部 op 合法）
- **位级精确对齐 C VM**（`csimple_lang/src/vm/runtime/runtime_value/runtime_value_convert.c`），
  含 NaN 载荷处理（输出恒 canonical 0x7FC00000）与 e4m3 无 inf 槽语义

### 6.2 VM 语义（对齐目标）

VM 的 Convert 是一次往返：`dval(f64) → (float32)dval → lp 编码位 → 读回 f32 → f64`。
所以发射序列为：

```mlir
%t32 = arith.truncf %d : f64 to f32
%lp  = <encode 内联：f32 位 → lp 位(i32)>
%dec = <decode 内联：lp 位(i32) → f32 位(i32)>
%w32 = llvm.bitcast %dec : i32 to f32
%w   = arith.extf %w32 : f32 to f64
%r   = llvm.bitcast %w : f64 to i64     // F32 域入栈
```

### 6.3 LowpFmt 参数表（`LowpFmtFor`）

| 参数 | e4m3 | e5m2 | f16 | bf16 |
|---|---|---|---|---|
| Ebits/Mbits | 4/3 | 5/2 | 5/10 | 8/7 |
| EtOff (127-bias) | 120 | 112 | 112 | 0 |
| HasInf | **false** | true | true | true |
| InfBase / NanBase | —/127 | 124/127 | 31744/32767 | 32640/32767 |
| OvfMax / OvfPred | 15 / **sgt** | 31 / sge | 31 / sge | 255 / sge |
| SubScale (decode 次正规) | 2^-9 | 2^-16 | 2^-24 | **null**（移位变体） |

派生量（`LowpFmt` 属性，杜绝逐格式手写常量）：
`SignSh=Ebits+Mbits`、`Drop=23-Mbits`、`HalfN=1<<(Drop-1)`、`MaskN=(1<<Drop)-1`、
`Carry=1<<Mbits`、`BaseN=Drop+1`、`BaseS=Drop`、`LexpMask=(1<<Ebits)-1`、`LmantMask=(1<<Mbits)-1`。

### 6.4 encode 结构（`EmitLowpEncode`，f32 位 → lp 位）

1. `llvm.bitcast` f32→i32；拆 sign/exp/mant；`et = exp - EtOff`；符号左移 SignSh
2. nanBits = signSh | NanBase；HasInf 时再算 infBits 和 lpIN（mant==0 选 inf 否则 NaN）
3. **正规路径**（et > 0）：RNE 截断 mantissa —— m=mant>>Drop、rem=mant&MaskN、
   remGt(ugt half)/remEq/tie(eq∧m 奇)/rndUp → m2；carry（m2 进位）时 et+1；
   e4m3 溢出判定 `et2 sgt OvfMax` → NaN 槽，其余 `sge` → inf
4. **次正规路径**（et ≤ 0）：sig = mant|implicitBit，base 按 et 选（BaseN/BaseS），
   shift = base+... **clamp 到 [1,31]**（select 全分支求值防毒值），
   mm=sig>>shiftC、remB=sig-(mm<<shiftC) 还原、halfB=1<<(shiftC-1)、RNE、
   carry2 时规格化回正规（sCar）
5. **合并**：et>0 选正规；e4m3 用 bad=isInf||ovf → nanBits；其余 isInf→lpIN、ovf→infBits

### 6.5 decode 结构（`EmitLowpDecode`，lp 位 → f32 位）

1. 拆 lsign/lexp/lmant；rsign = lsign<<31
2. **次正规**：SubScale 非空（e4m3/e5m2/f16）→ `sitofp(lmant) × scale` → bitcast
   （整数≤2^24 在 f32 精确、2 的幂缩放无舍入，故位级精确）；
   bf16（SubScale=null）→ `rsign | (lmant<<16)` 位级等价
3. **正规**：fe=lexp+EtOff、feSh<<23、lmSh<<Drop、拼装 ori
4. **特殊值**：e4m3（无 inf）：leMax&&lmMax → NaN 槽位 0x7FC00000；
   其余：lmant==0 ? inf : NaN（canonical）
5. leZero（exp==0）选择次正规/正规，输出 dec（i32 f32 位）

### 6.6 正确性验证（已完成，资产保留）

- 蓝本探针：`test/SpecialTest/mlir_f8_probe/f8int_probe.mlir`（发射器逐行复刻此文件的
  encode/decode op 序列；**修改发射器前先改探针并重跑对拍**）
- 对拍驱动：`f8int_driver.c`（cl.exe 编译，与 C VM 位级比较）
- 五阶段全 PASS：decode 穷举（f8 全 256 值 + f16/bf16 全 65536 值）、边界往返、
  随机 2M×4、encode 穷举（全 2^32 ×4）、f16/bf16 往返穷举（全 2^32 ×2）

---

## 7. 后续实现路线图（按依赖与收益排序）

### 7.1 模板方法 AOT 化（当前显式 Fail：templateIndex ≥ 0）

现状：模板方法被 IRManager 去重回定义级 IRMethod，签名保留未解析 T。
方向：
1. stage 1 候选收集改为**逐实例**（不 dedup，或为每个实例克隆签名并替换 T 槽位类型）
2. 需要一个"strip/replace"通道：把 `templateIndex>=0` 的槽位按实例实参替换为具体类型
   （值类型 T → I64/F64/F32/Struct；引用 T → ObjRef）
3. 与 `NewTemplateObject` 占位联动（若 T 是 struct 则 NewTemplateObject 也需要实例布局）

### 7.2 窄浮点槽位（变量类型为 Float8/Float16）

现状：局部/参数不能是窄浮点类型（ResolveVarType Fail）；窄浮点值只能通过常量加载 +
Convert 往返在 F32 域流转。
方向：VM 侧 eval 栈以独立 slot tag 存原始位模式 → 或定义第 5 种 ABI kind
（lp 位以 i64 低 16/8 位承载），或语言层规约窄浮点变量一律即时 Convert 到 F32 域
（现状即如此，成本最低）。

### 7.3 字符串桥（LoadConstString / Convert_ToString）

需要：模块级字符串 global（mlir.global constant）+ 宿主导出的 intern/interned-id 查询
（可复用 stage-5 桥注入模式：bridge_init 传函数指针）。Convert_ToString 需要数值→
字符串的运行时服务（宿主导出）。

### 7.4 静态字段 / 全局桥（Load/StoreStaticField、Load/StoreGlobal）

需要宿主侧静态存储 slab：bridge_init 传基址 + 导出每个静态字段的偏移表
（method-id → offset 同构）。发射侧把 Load/Store 改为 GEP + load/store 到桥指针。

### 7.5 数组分配桥（NewArray）

NewArray 目前占位（首次数组使用即失败）。已有 Load/StoreArrayIndex*（消费 VMArray*），
缺的是"产出 VMArray*"：调用宿主导出的分配函数（桥注入函数指针），返回 VMArray*。
注意 C VM 侧 VMArray 布局（unit_length 4/8 字节标量元素已对齐）。

### 7.6 CallSystemMethod 逐个映射

`declared` 已注册的才能过（未注册 Fail）。方向：高频数学函数（Math.*）映射到
llvm intrinsics / libm（注意 dll 链接集，需评估 msvcrt 覆盖）；其余维持占位。

### 7.7 异常（BeginTry/EndTry/Throw）

需要 C++ 异常（/EHsc + landingpad 路径，llc 侧支持有限）或宿主 longjmp 桥。
复杂度高，建议最后做。

### 7.8 checked 上下文真实化

BeginChecked/EndChecked 目前 no-op（无溢出陷阱）。若语言语义要求 checked 算术，
需在 checked 区间内把算术换成带溢出检测的变体（llvm.*.with.overflow intrinsics，
llc 在 x86-64 可下化为 jo 分支，无运行库依赖，可行性高）。

### 7.9 GPU 结构体成员访问

需要在 gpu.func 内可用的成员视图：把 Struct 参数改为 memref 传入（GPU 入口
marshalling 改造），成员访问从"宿主 native buffer GEP"改为 memref GEP。
涉及 ABI 变更，独立成项。

---

## 8. 验证与构建备忘

- 构建命令（在 `f:\project\lang\simple_language\source` 下）：
  `dotnet build Front\SimpleLanguageFront.csproj --nologo -v q`（当前 exit 0，仅既有警告）
- 工具链：mlir-opt/mlir-translate/llc/mlir-transform-opt 已迁移到
  `simple_language\tools\llvm`（4 个 exe + mlir_float16_utils.dll，静态链接自包含）。
  发现顺序（`MLIRToolchain.ToolchainPaths.Resolve`，**无环境变量**）：
  1. exe 目录（auto-deploy 副本，优先）——首次使用时 `EnsureLocalTools` 把
     mlir-opt.exe / mlir-translate.exe / llc.exe / mlir_float16_utils.dll
     从探测到的源目录拷到 exe 所在目录（`AppContext.BaseDirectory`），已存在的不覆盖
     （升级工具需手动删 exe 目录下旧文件），源目录没有或拷贝失败则静默跳过、
     回退源目录；mlir-transform-opt.exe 管线未用、不拷
  2. 向上遍历找 `<root>\simple_language\tools\llvm`（新位置，优先）
  3. 向上遍历找 `<root>\llvm-project\build\Release\bin`（旧 monorepo 布局，回退兼容）
  link.exe 不设路径：`ResolveLinkExe` 走 vswhere → VS 安装目录 → PATH 自动发现
  （MSVC 在 `C:\Program Files\Microsoft Visual Studio\18\Community`）。
  已验证：把 `tools\llvm` 临时改名（模拟删除源码树）后 AOT dll 构建仍成功，
  证明 exe 目录副本可独立完成 stage-3 全链。
- AOT 开关（jsonc `export.aot` 段，默认全开）：
  - `"enabled": false`——整个 AOT 导出管线跳过（全部走 CVM 解释执行）
  - `"buildDll": false`——只导出 aot.mlir，不构建 aot.dll（module.json 的 aot.dll 为空）
- 修改窄浮点发射逻辑的流程：**先改探针 f8int_probe.mlir → 工具链全链 → f8int_driver.c
  对拍全绿 → 再同步发射器**（发射器是探针的逐行内联复刻，L3483/L3608）
- 新增指令发射的 checklist：
  1. s_SupportedOps 加 opcode（带注释分类：真实翻译 / no-op / 占位）
  2. 栈模拟器 Profile* 加同构栈效果（保证一致性校验）
  3. EmitInstruction 加真实发射（注意 §5.2 陷阱）
  4. 评估 GPU 是否可继承；若不可，加入 s_GpuUnsupportedOps 或 CheckSupported
  5. dotnet build + 一个端到端用例（stage 1→3.5 + 运行时对拍）
