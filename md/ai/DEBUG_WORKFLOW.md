# SimpleLanguage 调试产物与排查顺序（权威）

本文说明 **`{export.outputDir}/{export.moduleName}/`** 下各文件含义，以及推荐阅读顺序。所有与「编译 / 导出 / VM 运行」相关的排查都应先对齐本目录结构。

完整路径约定见 `EXPORT_PATHS.md`；环境变量：`SIMPLELANG_EXPORT_OUTDIR`、`SIMPLELANG_LOGS_DIR`、`SIMPLELANG_DEBUGCODE_ROOT`。

---

## 1. 模块根目录下有什么

| 位置 | 含义 |
|------|------|
| **`Logs/Result.txt`** | VM 运行期 **`print` / `println` 镜像输出**（用户可见结果）。 |
| **`Logs/VM.txt`** | VM **运行时**结构化文本日志（加载、解析、执行中的 Info/Error 等）。 |
| **`Logs/Front.txt`** | **编译期** Front 固定文本日志（Token/File/Meta/IR 等阶段的错误与信息）。 |
| **`*.module.json`**（如 `Core.module.json`） | 从 IR **导出**的 VM 指令包（指令集、类/方法元数据等），**供 VM 执行**。 |
| **`DebugCode/`** | **编译期**按阶段落盘的调试文本；**每个子文件夹对应一个源文件**（相对路径与 `.sl` 一致）。 |

---

## 2. `DebugCode/<源文件相对路径>/` 内各文件（单文件一条流水线）

以下描述**单个**已编译 `.sl` 文件对应目录下的典型文件（名称由 `export.debugText` 开关控制是否生成）。

| 文件 | 含义 |
|------|------|
| **`Code.txt`** | 该文件的 **原始 `.sl` 源码**快照（输入基准）。 |
| **`Token.txt`** | 对 `Code` 的 **词法 / Token 解析**结果。 |
| **`Node.txt`** | 在 Token 之上的 **结构整合**：对 `()` `[]` `{}` `<>` 以及 **`.` 链式**等的处理，以及与子元素、结构相关的归并（语法树雏形，仍偏「结构」而非完整语义实体）。 |
| **`File.txt`** | 对 **Node** 的进一步整合，**面向文件 / FileMeta** 层：已有语言阶段的**初步逻辑**，但**尚未**落到完整「实体语法」语义。 |
| **`Meta.txt`** | **实体语法 / Meta** 层：类型、成员、调用关系等 **语言逻辑关系**在此建立。 |
| **`IR.txt`** | 在 **Meta** 之上生成的 **IR**（中间表示），将语言逻辑整理为 **可导出的 IR 形态**；后续导出器读取 IR（及内存中的 IR 结构）写入 **`*.module.json`**。 |

流水线关系（编译侧，由浅入深）：

```text
Code → Token → Node → File → Meta → IR →（导出）→ *.module.json
```

---

## 3. 推荐排查顺序

### 3.1 先看「跑得对不对」（运行时）

1. **`Logs/Result.txt`** — 业务输出是否符合预期。  
2. **`Logs/VM.txt`** — 是否有加载失败、运行时异常、opcode 相关问题。  
3. 若怀疑导出或指令错误：对照 **`*.module.json`** 与对应源文件在 **`DebugCode/`** 下的各阶段文件。  
4. 在 **DebugCode** 内对**具体问题文件**，通常按 **由导出反推编译** 的方向阅读：

   **`IR.txt` → `Meta.txt` → `File.txt` → `Node.txt` → `Token.txt` → `Code.txt`**

   即：先看 IR 与最终导出是否一致，再逐层下钻到 Meta/File/Node/Token，最后核对源码 `Code.txt`。

### 3.2 编译失败或 IR 以前就错（编译期）

1. **`Logs/Front.txt`** — 定位最早一条 **Error**、阶段（ParseToken / ParseFile / ParseMeta / GenIR 等）与 **Position**。  
2. 再打开同一文件在 **`DebugCode/`** 下对应目录，按阶段对照；仍可采用 **IR→…→Code** 反查，但若错误发生在 Meta 之前，多在 **File / Node / Token** 即可定位。

### 3.3 原则

- **上游阶段未通过时，不要只改 VM 或只改导出**；先让 Front 流水线在该文件上自洽。  
- **`IR.txt` 正确**后，再重点查 **导出**（`*.module.json`）与 **VM**（`VM.txt` / `Result.txt`）。

---

## 4. 与主文档的关系

更完整的流程说明、语法规则与复盘模板见 **`故障排查流程.md`**；本文专注 **目录与产物语义 + 阅读顺序**，修改导出布局时请同步更新 **`EXPORT_PATHS.md`** 与本文件。
