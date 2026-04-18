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
