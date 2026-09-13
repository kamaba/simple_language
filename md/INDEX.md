# SimpleLanguage 文档总索引

本页按**主题**汇总仓库内 Markdown，路径以仓库根目录为基准。深入语法细节以 `md/syntax/` 下各章为准；工程与编译以 `md/project/` 与 `md/ai/EXPORT_PATHS.md` 为准。

---

## 1. 从这里开始

| 文档 | 说明 |
|------|------|
| [syntax/introduction.md](./syntax/introduction.md) | 语法文档集总述：覆盖范围、IR 关系、示例约定 |
| [project/project-config-jsonc-guide.md](./project/project-config-jsonc-guide.md) | **当前**工程配置：`*.sp` + 同名 `*.jsonc`、主要 JSONC 字段、CLI 联动 |
| [project/project_sp-guide.md](./project/project_sp-guide.md) | 入口 `Core.sp` 约定：`_main_` / `_test_`、`global` 与 `Project{}`、`global.data` 注入规则 |
| [ai/故障排查流程.md](./ai/故障排查流程.md) | 问题定位主文档（与 `DEBUG_WORKFLOW.md` 等互补） |
| [ai/DEBUG_WORKFLOW.md](./ai/DEBUG_WORKFLOW.md) | 调试产物目录、`DebugCode` 流水线说明 |
| [ai/EXPORT_PATHS.md](./ai/EXPORT_PATHS.md) | 导出目录、环境变量、模块 JSON 形态 |
| [ai/PROJECT_MAP.md](./ai/PROJECT_MAP.md) | 解决方案结构、`Front` / `VM` 职责与路径书签 |

---

## 2. 工程与模块

| 文档 | 说明 |
|------|------|
| [project/project.md](./project/project.md) | 工程概念：`ProjectConfig`、编译文件列表、入口与全局变量（偏语言侧叙述） |
| [project/project-module.md](./project/project-module.md) | 模块与类组织方式说明 |
| [project/project-config-jsonc-guide.md](./project/project-config-jsonc-guide.md) | JSONC 字段详解与迁移注意点 |
| [project/ffi.md](./project/ffi.md) | **FFI 外部函数接口（已落地）**：普通 FFI 调用、`FFI.Library` / `FFI.StaticLibrary`、`dllImports` 配置、`@DllImport` 与 `@DllStaticImport`(opcode 118)、sig 规则、内部原理 |
| [project/test-guide.md](./project/test-guide.md) | **测试引导**：`test/` 各测试用例集（测什么、代表用例）与 `project/` 各测试宿主工程（默认测试集、C VM / C# VM）对照表、运行方式（VS / dotnet / Debug vs Release） |

---

## 3. 语法（`md/syntax/`）

### 3.1 基础与结构

| 文档 | 说明 |
|------|------|
| [syntax/introduction.md](./syntax/introduction.md) | 简介与文档约定 |
| [syntax/base.md](./syntax/base.md) | 基本语法 |
| [syntax/namespace.md](./syntax/namespace.md) | 命名空间 |
| [syntax/variable.md](./syntax/variable.md) | 变量 |
| [syntax/local.md](./syntax/local.md) | 局部与作用域相关 |
| [syntax/global.md](./syntax/global.md) | `global` 与工程 / `Project{}` 联动 |

### 3.2 类型、字面量与运算

| 文档 | 说明 |
|------|------|
| [syntax/number.md](./syntax/number.md) | 数值类型：字面量、内置类型词法、`Num` 语义 |
| [syntax/float8_16.md](./syntax/float8_16.md) | 低精度浮点类型：`Float8`/`Float8_E5M2`/`Float16`/`Float16_Brain` 字面量后缀、存储约定、转换与运算语义 |
| [syntax/string.md](./syntax/string.md) | 字符串 |
| [syntax/data.md](./syntax/data.md) | 数据类型 |
| [syntax/type.md](./syntax/type.md) | 类型 |
| [syntax/typealias.md](./syntax/typealias.md) | 类型别名 |
| [syntax/express.md](./syntax/express.md) | 表达式 |
| [syntax/operator.md](./syntax/operator.md) | 运算符 |
| [syntax/cast.md](./syntax/cast.md) | 类型转换 |

### 3.3 控制流

| 文档 | 说明 |
|------|------|
| [syntax/if.md](./syntax/if.md) | 条件 |
| [syntax/switch.md](./syntax/switch.md) | `switch` |
| [syntax/forwhiledowhile.md](./syntax/forwhiledowhile.md) | 循环 |
| [syntax/labelgoto.md](./syntax/labelgoto.md) | 标签与 goto |
| [syntax/trycatch.md](./syntax/trycatch.md) | 异常处理 |

### 3.4 面向对象与复用

| 文档 | 说明 |
|------|------|
| [syntax/class.md](./syntax/class.md) | 类 |
| [syntax/object.md](./syntax/object.md) | 对象 |
| [syntax/interface.md](./syntax/interface.md) | 接口 |
| [syntax/extend.md](./syntax/extend.md) | 继承 |
| [syntax/attribute.md](./syntax/attribute.md) | 特性 / 属性 |
| [syntax/function.md](./syntax/function.md) | 方法 / 函数 |
| [syntax/enum.md](./syntax/enum.md) | 枚举 |

### 3.5 模板、宏与高级机制

| 文档 | 说明 |
|------|------|
| [syntax/template.md](./syntax/template.md) | 模板与泛型 |
| [syntax/marco.md](./syntax/marco.md) | 宏 |
| [syntax/virtualmachine.md](./syntax/virtualmachine.md) | 虚拟机相关语法与概念 |
| [syntax/exporter.md](./syntax/exporter.md) | 导出 / IR 侧说明 |
| [syntax/coroutine.md](./syntax/coroutine.md) | 协程（Coroutine）与通道（Channel） |
| [syntax/isolate.md](./syntax/isolate.md) | 隔离岛（Isolate）：跨堆并行、端口消息通信（深拷贝）、生命周期控制、TransferableData 零拷贝转移 |

### 3.6 集合与容器（`md/syntax/contraint/`）

| 文档 | 说明 |
|------|------|
| [syntax/array.md](./syntax/array.md) | 数组 |
| [syntax/contraint/list.md](./syntax/contraint/list.md) | List |
| [syntax/contraint/set.md](./syntax/contraint/set.md) | Set |
| [syntax/contraint/map.md](./syntax/contraint/map.md) | Map |
| [syntax/contraint/tuple.md](./syntax/contraint/tuple.md) | Tuple |
| [syntax/contraint/queue.md](./syntax/contraint/queue.md) | Queue |
| [syntax/contraint/stack.md](./syntax/contraint/stack.md) | Stack |

### 3.7 标准库与其它

| 文档 | 说明 |
|------|------|
| [syntax/std/env.md](./syntax/std/env.md) | 环境相关 |
| [syntax/std/Component.md](./syntax/std/Component.md) | Component 组件基类（组件即节点组合树：查询 / 门控 / 消息） |
| [syntax/std/Sqlite.md](./syntax/std/Sqlite.md) | Sqlite 数据库（DB.Sqlite3） |
| [syntax/system_method.md](./syntax/system_method.md) | 系统方法 |
| [syntax/range.md](./syntax/range.md) | 范围 / range |
| [syntax/result.md](./syntax/result.md) | Result 等结果类型 |

---

## 4. 虚拟机与运行时

| 文档 | 说明 |
|------|------|
| [vm/VM_FileRoles.md](./vm/VM_FileRoles.md) | VM 侧文件角色说明 |
| [../source/VM/VM_CStyle_GUIDE.md](../source/VM/VM_CStyle_GUIDE.md) | VM C 风格指南（源码树内） |

---

## 5. 日志

| 文档 | 说明 |
|------|------|
| [log/log-system-guide.md](./log/log-system-guide.md) | 日志系统与诊断 |

---

## 5.1 设计与规划（`md/design/`）

> ⚠️ 该目录多为**未落地方案**，实现前请先 grep 源码确认。

| 文档 | 说明 | 状态 |
|------|------|------|
| [design/MLIR_AOT_DESIGN.md](./design/MLIR_AOT_DESIGN.md) | SLIR→MLIR→LLVM→exe 全链路、指令映射、运行时 ABI | 规划（三期） |
| [design/TENSOR_HETEROGENEOUS_DESIGN.md](./design/TENSOR_HETEROGENEOUS_DESIGN.md) | 张量/数学库异构计算（CPU/GPU/NPU）：两区模型、三层数据抽象、算子派为主、控制流三分规则、SLTIR 新 IR 层、**HAL 硬件抽象层**、落地路线、22 组 demo（含 HAL 视角）。<br>⚠️ 编译期指令函数化（`Compile.*`，不新增 `@`）；矩阵乘用 `A · B`/`matmul`（**不用 `@`**）；小向量沿用 `class` + POD 布局特化（不引入 `data`） | 规划 |
| [design/MLIR_AOT_LLVM_STRUCT_DESIGN.md](./design/MLIR_AOT_LLVM_STRUCT_DESIGN.md) | class/data/enum → `!llvm.struct`、CVM 对象 ↔ 原生布局双向 marshal、继承前缀布局 | 规划 |
| [design/STREAM_DESIGN.md](./design/STREAM_DESIGN.md) | **v2 流体系总纲**：三层模型 —— L0 `ByteBuf`（共用缓存载体）+ `abstract ByteStream`（文件/Tcp/Udp/Tls/WebSocket/变换流）、L1 `abstract Stream<T>`（Dart 风格元素流，复用 `Channel` 背压 + 协程）、L2 `Codec`（ProtoBuf/序列化/Json/压缩）；含 `NetStream` 家族、WebSocket 双视图、跨 isolate 零拷贝、**共用 `system_method_call` 契约**（`vm_sys_bytebuf_*` / `vm_sys_codec_varint_*`）与 ~97 个新增 syscall 分期表 | 规划 |
| [design/PLATFORM_CAPABILITY_DESIGN.md](./design/PLATFORM_CAPABILITY_DESIGN.md) | **平台能力与运行条件**：Front 声明 `platform.require`（系统/版本/架构/指令集/CPU 核数/内存/lib/SDK/环境变量/设备/特殊环境/CVM 版本）→ 编译为**条件 AST** → 导出 `module.json` 的 `platform` 段 → CVM 用 `sl_platform_probe()` 探测当前环境并与 AST 比对，硬要求不满足拒绝加载、软要求降级。含 C 源码（探测 + 求值 + 诊断）、多目标变体（v2）、与 `static if`/标签 `platform`/AOT triple 的分工；★ **§12 `Environment` 运行时分类体系**：**16 大类**（os/form/arch/**cpu 多核拓扑**/isa/device/**AI 栈**/**渲染后端**/**网络**/**脚本**/**嵌入式 MCU+RTOS+资源**/runtime/lib/env/build/capability）；★ **§13.0 SL 语法合规基线**：示例已按真实语法重写（`ret`/`Console.println`/`#`注释/`elif`/`_main_()`/`enum extends int`/**无嵌套 class**）；**§12.0.1 导入规则**：文件头**必须 `import Core`**（否则 `MissingEnvironmentImport`）；**§12.0.2 短写用 `typealias`**（只能写在 `.sp` 的 `Project{ Global(){} }` 内、只能别名类型，故 `OS.window` 可用而 `Environment.current.*` 必须全称）；**§13.2 结构**：`Environment` 为**嵌套 namespace**（SL 不支持嵌套 class），下含 17 个 `enum extends int`；**§13.13 落地冲突**：现有 `Core/Environment.sl` 是 class，需升级为 namespace（含 `legacy` 兼容壳）；**§13 条件定义类 `Environment.Platform`**（Core module `Core/Environment.sl`）：`Environment` 下**嵌套** `Platform`（定义区，`Environment.Platform.os.window/linux/mac/ios/android/**ps4/ps5/xboxSeries/nintendoSwitch**/bareMetal/rtos`；`osVersion` 含 **linuxArch/linuxKeil**；含 arch/cpu/isa/device/ai/gfx/network/embedded/runtime/build）与 `Current`（运行时区，`Environment.current.os`）；`Platform.defs` 注册表；**jsonc key 直接等于定义成员名**（`"linux"`），编译前期查定义校验，报错列出全部可选值+纠错；CVM 侧 `sl_platform_def.h` 对称枚举（代码生成保证数值一致），检查时**环境→定义枚举→比对**；**§13.12 `global.env` 已废弃**，全部收归 `Environment` 四区（`current` 运行时值 / `Platform` 定义常量 / `env` 环境变量 / `custom` 自定义），原 `platform` 形态大类改名 `form` 避免与 `Platform` 撞名；定义类拆细（`gfx`/`shaderModel`、`network`/`link` 分离）使短写一一对应；补 C 侧现状缺口（无 OS 版本/CPU 特性检测、module.json 无平台字段、`Compile.Target` 死字符串）；★ **§8.5 运行时参数覆盖 / 代码屏蔽**：CVM 可**传参**（`--disable/--enable/--set/--override-file`、`SL_RT_*`、宿主 API）注入**覆盖表**，优先级 jsonc<env<CLI<host 且**高于探测值**，可**屏蔽条件 / `@tag` / 条件化代码块**（`--force-run` 是全局放行，覆盖是逐条屏蔽）；`Environment.current`=有效值、`Environment.probe`=原始值，诊断强制 `[override by …]` 标注 | 规划 |
| [design/AT_SIGN_LABEL_DESIGN.md](./design/AT_SIGN_LABEL_DESIGN.md) | **`@标签{}` 异质代码块**（AtSignLabel）：`@tag(params){ raw }` 四 kind（foreign/device/shader/asm）× 四正交维度（**参数**具名可 auto、**捕获**隐式+访问器+pin 不暴露栈指针、**结果**宿主语言 `return`、**执行位置** `place{inline/coroutine/isolate}`×`wait{block/await}` 编译期必须确定）；`:iso` 自动搬常驻 worker 岛（强制显式 in/out + 可发送 + 禁隐式写回）；**§10 插件化架构**：signLabel 是独立子系统——解析外置 Parser 插件（rule/script/dll）、执行外置 Handler DLL（CVM 调 dll，纯 C ABI `SLLabelExecCtx*` + `SLLabelHost` 服务表），SL 只保留三处耦合（入参/出参/调度 coroutine·isolate），**协程/隔离岛零新语法**：`@tag(){}` 即特殊闭包，用 `Coroutine.spawnFunc0(@tag(){})` / `Isolate.spawnFunc0(@tag(){})` 驱动；**参数必须具名**（位置参数编译失败）；**数据用 `$变量` + `<-`/`->` 通道传输**（`int a <- $a;` 传入、`$count <- f(a,20);` 传出，箭头指向目的地，隔离岛下批量跨岛且在 await 点写回）；**标签名分层不限层数**（`@gpu.amd.tensor`）；Parser 只需两条正则；`ParseResult.channels` 是枢纽契约；**§8.6 数据传输管道**：三阶段 Prepare/Transfer/Commit × 三种所有权（同步 borrow 零拷贝 / 协程通道槽+全生命期 pin / isolate `ChannelBlob` 批量 copy·move 且 await 点写回 / 设备 DMA）；**§11 值转换系统 VMS（含 C 源码）**：域模型（SL/FOREIGN/DEVICE/ISOLATE）+ 一致性等级（默认 SNAPSHOT 快照冻结 / PINNED / TRANSFER / DETACHED）+ **epoch 版本号**检测异步期间改写（onConflict: overwrite/keep/error）+ 转换器注册表 + marshal 上下文（arena+pinlist）；**§18 完整标签注册表：60+ 标签**（foreign 25 / asm 26 / device:cpu·gpu·npu·dsp·fpga / shader 5 / ir 16 / dsl 6）+ 家族继承 `atSignLabelDefaults` + 70+ 别名 + 回退链；接管 `TENSOR` §10.2 的设备特化块提案 | 规划 |
| [design/COROUTINE_DESIGN.md](./design/COROUTINE_DESIGN.md) | 协程设计（⚠️ 与实现差异较大，**以 `syntax/coroutine.md` 为准**） | 已部分落地 |
| [design/ISOLATE_DESIGN.md](./design/ISOLATE_DESIGN.md) | Isolate 隔离设计 | 规划 |
| [design/ffi-design.md](./design/ffi-design.md) | FFI 设计（⚠️ 草案，**已落地实现见 `project/ffi.md`**） | 部分已落地 |
| [design/HOTSPOT_JNI_INTEGRATION_DESIGN.md](./design/HOTSPOT_JNI_INTEGRATION_DESIGN.md) | HotSpot JNI 集成 | 规划 |
| [design/MONO_INTEGRATION_DESIGN.md](./design/MONO_INTEGRATION_DESIGN.md) | Mono 集成 | 规划 |
| [design/QUICKJS_INTEGRATION_DESIGN.md](./design/QUICKJS_INTEGRATION_DESIGN.md) | QuickJS 集成 | 规划 |
| [design/RENDER_LIFECYCLE_DESIGN.md](./design/RENDER_LIFECYCLE_DESIGN.md) | **生命周期体系（Unity 风格）**：现状盘点（`Behaviour` 只有钩子、**无驱动者**）+ 五大缺口（G1 无驱动 / G2 无状态机 / G3 无实体·场景容器 / G4 无时间语义 / G5 无销毁语义）；`Scene`/`GameObject`/`Behaviour` 三件套（`GameObject extends Component`，**不引入新树**）；状态机 + 8 条触发规则 + 5 条不变式；12 步帧循环（`fixedUpdate`×N → 事件派发 → `start` → `update` → 协程 → `lateUpdate` → ★**延迟销毁** → 渲染段 → `present`），与 `RenderPipelineManager` 的渲染段接缝；`Time`（`timeScale`/`fixedDeltaTime`/**死亡螺旋保护**）；`@ExecutionOrder(n)` 顺序控制；延迟销毁队列与协程联动；`dispose()`↔`onDestroy()` 兼容桥接（基类默认转调，现有代码零改动）；P0–P5 分期 | 规划 |

---

## 6. AI 协作与仓库维护

| 文档 | 说明 |
|------|------|
| [ai/INDEX.md](./ai/INDEX.md) | `md/ai` 子目录索引表 |
| [ai/AI_GUIDE.md](./ai/AI_GUIDE.md) | AI 协作约定 |
| [ai/AI_PROMPTS.md](./ai/AI_PROMPTS.md) | 提示词模板 |
| [ai/CONTRIBUTING_GUIDE.md](./ai/CONTRIBUTING_GUIDE.md) | 贡献指南 |
| [ai/CODEBASE_OVERVIEW.md](./ai/CODEBASE_OVERVIEW.md) | 代码库英文简览 |
| [ai/代码解析流程.md](./ai/代码解析流程.md) | 解析流程（与故障排查文档交叉引用） |
| [ai/语法规则.md](./ai/语法规则.md) | 语法规则备忘（与故障排查交叉引用） |

---

## 7. 其它根级文档

| 文档 | 说明 |
|------|------|
| [code.md](./code.md) | 代码相关说明 |
| [../README_CN.md](../README_CN.md) / [../README.md](../README.md) | 项目主自述（中/英） |
| [../PRD.md](../PRD.md) | 产品说明 |
| [../Release_CN.md](../Release_CN.md) / [../Release.md](../Release.md) | 发布说明 |
| [../target.md](../target.md) | 目标与规划 |
| [../source/Front/Lib/Core/Core.md](../source/Front/Lib/Core/Core.md) | Core 库说明（源码树内） |

---

**维护提示**：新增语法章节时请在本文件对应小节补链；避免使用已不存在的文件名（旧索引中的 `ranage.md` / `module.md` 等已按实际文件修正；`num.md` 已并入 `number.md`）。
