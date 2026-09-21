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
| [project/optimization.md](./project/optimization.md) | **编译优化等级（CLI `-O0..-O3`）**：各等级优化内容（null peephole / 常数融合 store / 小函数自动 inline——O1≤2 / O2≤4 / O3≤7 条）、排除集、与 jsonc `compile.optimize` 的关系 |
| [project/environment-guide.md](./project/environment-guide.md) | **Environment 平台环境**：`Environment.*` API（current/probe/Override/env/custom/sys）、`Platform` 定义枚举全表、jsonc `platform` 段全量关键字（require 18 字段 / override / variants）、运行期覆盖四通道 |
| [project/ffi.md](./project/ffi.md) | **FFI 外部函数接口（已落地）**：普通 FFI 调用、`FFI.Library` / `FFI.StaticLibrary`、`dllImports` 配置、`@DllImport` 与 `@DllStaticImport`(opcode 118)、sig 规则、内部原理 |
| [project/test-guide.md](./project/test-guide.md) | **测试引导**：`test/` 各测试用例集（测什么、代表用例）与 `project/` 各测试宿主工程（默认测试集、C VM / C# VM）对照表、运行方式（VS / dotnet / Debug vs Release） |

---

## 3. 语法（`md/syntax/`）

### 3.1 基础与结构

| 文档 | 说明 |
|------|------|
| [syntax/introduction.md](./syntax/introduction.md) | 简介与文档约定 |
| [syntax/keywords.md](./syntax/keywords.md) | **关键字与语法糖总表**：全量关键字分类表（词法器实测）、运算符与特殊符号、语法糖对照（ret/next/yield/await/spawn/static if/$插值/小写容器构造糖）、类成员保留名规则、Core 引用后直接可用类型清单、Core / Environment / Platform 命名空间 |
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
| [syntax/try.md](./syntax/try.md) | 异常处理（throw / throws / try / catch / checked） |

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
| [syntax/inline_lambda.md](./syntax/inline_lambda.md) | 内联体：`=>` 内联表达式（调用点编译期就地展开、参数可选类型标注 + 调用点类型校验）+ `inline` 方法修饰符（语义层普通函数规则、IR 层体替换无 Call、实例隐含 final、Result 兜底）、禁止项与错误码（spawn/isolate/await/try/递归/跨边界/类型不匹配 21456/体超限 21453/throws 互斥 21457/初始化器禁调 21458）、与 function 闭包边界 |

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
| [../../../csimple_lang/md/design/PROCESS_DESIGN.md](../../../csimple_lang/md/design/PROCESS_DESIGN.md) | Std.OS.Process 外部进程执行（run/start/wait、stdio 三态管道、kill/terminate、环境变量；SL API + C 层设计） |
| [syntax/core/Lz4.md](./syntax/core/Lz4.md) | Lz4 块压缩（Core 库：自包含容器格式 + ByteBuffer 底座） |
| [syntax/core/ProtocalBuffers.md](./syntax/core/ProtocalBuffers.md) | ProtocalBuffers / protobuf 线格式编解码（Core 库：PbWriter / PbReader，proto3 兼容子集） |
| [syntax/core/Stream.md](./syntax/core/Stream.md) | Stream 元素流（Core 库：Stream\<T\>/Controller/Transformer，推/拉双模 + 背压） |
| [syntax/system_method.md](./syntax/system_method.md) | 系统方法 |
| [syntax/range.md](./syntax/range.md) | 范围 / range |
| [syntax/result.md](./syntax/result.md) | Result 等结果类型 |

---

## 4. 虚拟机与运行时

| 文档 | 说明 |
|------|------|
| [vm/VM_FileRoles.md](./vm/VM_FileRoles.md) | VM 侧文件角色说明 |

---

## 5. 日志

| 文档 | 说明 |
|------|------|
| [log/log-system-guide.md](./log/log-system-guide.md) | 日志系统与诊断 |
| [log/log-debug-usage.md](./log/log-debug-usage.md) | **SLang.Log/Logger/Profiling/Trace/Monitor 使用指南**：级别/文件落盘/显示配置、`Log.fatal` VM 硬停语义、调试工作流、测试用例（MonitorTest/LogTest/LogFatalTest） |

---

### 5.1 设计与规划

> `md/design/` 目录已于 2026-09-13 整体删除，历史设计文档（MLIR_AOT / TENSOR / STREAM / COROUTINE / ISOLATE / ffi 等 13 篇）不再随仓库维护。落地现状以源码与 `md/syntax/` 为准；三期 MLIR/AOT 方向仅存 `source/Front/Export/MLIR/MLIR_AOT_实现与待办.md`。

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
| [SL语法与编码规范总表.md](./SL语法与编码规范总表.md) | **语法与编码规范单点引用总表**（由 syntax/ 48 篇 + code.md 提炼去重）：编码规范（命名/对齐/注释/提交/Core 库）、关键字总表（词法保留 + 非词法保留 + 类型词）、语法糖总表、语法速查 21 节（声明与语句格式）、Core 类型清单、16 条已知坑 |
| [../README_CN.md](../README_CN.md) / [../README.md](../README.md) | 项目主自述（中/英） |
| [../PRD.md](../PRD.md) | 产品说明 |
| [../Release_CN.md](../Release_CN.md) / [../Release.md](../Release.md) | 发布说明 |
| [../target.md](../target.md) | 目标与规划 |
| [../source/Front/Lib/Core/Core.md](../source/Front/Lib/Core/Core.md) | Core 库说明（源码树内） |

---

**维护提示**：新增语法章节时请在本文件对应小节补链；避免使用已不存在的文件名（旧索引中的 `ranage.md` / `module.md` 等已按实际文件修正；`num.md` 已并入 `number.md`）。
