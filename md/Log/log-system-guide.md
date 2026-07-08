# Log 系统设计与使用说明

## 目标

Log 系统用于统一管理编译链路（`Token/Node/File/Meta/IR/Other`）中的诊断信息，支持：

- 基于 CSV 的错误码定义（唯一 `id`）。
- 结构化日志级别（`Assert/Info/Warning/Error/Trace`）。
- 可配置的阻断策略（当前阶段阻断 / 编译流程中断）。
- 多线程并发写入。
- Token 上下文解析与展示（文件、行列、词素、类型）。

---

## CSV 定义规范

建议文件名：`source/Log/ErrorDefinitions.csv`

### 表头

```csv
id,module,logType,enableAssert,blockOnErrorAssert,abortCompilation,messageTemplate,paramCount,fixHint
```

### 字段语义

- `id`
  - 错误码，必须唯一。
- `module`
  - 来源模块：`Token` / `Node` / `File` / `Meta` / `IR` / `Other`。
- `logType`
  - 日志类型：`Assert` / `Info` / `Warning` / `Error` / `Trace`。
- `enableAssert`
  - 是否启用 Assert 语义（针对该条定义）。
- `blockOnErrorAssert`
  - 当该条为 `Error` 或 `Assert` 时是否阻断当前流程。
- `abortCompilation`
  - 是否中断整个编译过程。
- `messageTemplate`
  - 消息模板（`string.Format` 风格）。
- `paramCount`
  - 模板参数数量。
- `fixHint`
  - 修复建议。

### 示例

```csv
id,module,logType,enableAssert,blockOnErrorAssert,abortCompilation,messageTemplate,paramCount,fixHint
10001,Token,Error,true,true,true,"Unrecognized token '{0}'",1,"检查输入字符是否合法，或补充词法规则"
20001,Meta,Warning,true,false,false,"Type '{0}' inferred as Object",1,"为模板参数添加约束，如 T:Num"
30001,IR,Info,true,false,false,"IR export completed: {0}",1,"可在 DebugCode/IR.txt 中复核输出"
```

---

## 运行行为（建议）

1. 先从 CSV 加载定义，再注册临时定义。
2. 多线程环境下，使用并发容器保存诊断事件。
3. 如果传入 Token：
   - 自动解析并展示：`path`、`sourceBeginLine/sourceBeginChar`、`sourceEndLine/sourceEndChar`、`lexeme`、`type`。
4. 阻断策略：
   - `blockOnErrorAssert=true`：阻断当前执行路径。
   - `abortCompilation=true`：抛出“编译中断”异常并停止后续流程。

---

## 落地建议

- 新增错误时，优先写入 CSV，避免硬编码。
- `messageTemplate` + `paramCount` 必须对齐。
- `fixHint` 尽量给出可执行建议（文件、语法点、替代写法）。
- 调试顺序建议仍沿用项目既定链路：`IR -> Meta -> File -> Node -> Token -> Code`。
