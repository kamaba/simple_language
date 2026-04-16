# SLIR 导出路径（约定）

**调试时各文件含义与阅读顺序**：见同目录 **`DEBUG_WORKFLOW.md`**（与 Cursor 规则 `.cursor/rules/simplelang-debug-artifacts.mdc` 一致）。

在 `jsonc` 的 **`export.outputDir`** 下再建一层 **`export.moduleName`**（未填则用 `project.name` / `.sp` 主名），得到模块根目录：

`{export.outputDir}/{moduleName}/`

该目录下固定包含：

- **`Logs/`**：`Front.txt`（Front 固定文本日志）、`VM.txt`（VM 固定文本日志）、`Result.txt`（`print`/`println` 镜像）。
- **`DebugCode/`**：Front 各阶段调试文本（`Code.txt`、`Token.txt`、…），按源文件相对路径分子目录（`Common.GetDebugCodeDir`）。
- **VM 包**：**`{project.name}.module.json`**（如 `Core.module.json`），与上两项同级，由 `ExportLangManager` → `SLModulePackageWriter.Write` 写入该模块目录。

## 环境变量（加载工程后由 Front 设置）

- **`SIMPLELANG_EXPORT_OUTDIR`**：上述模块根目录（`*.module.json` 所在目录）。
- **`SIMPLELANG_LOGS_DIR`**：`{export.outputDir}/{moduleName}/Logs/`。
- **`SIMPLELANG_DEBUGCODE_ROOT`**：`{export.outputDir}/{moduleName}/DebugCode/`。

未走 `LoadProject` 时仍可用这些变量手动覆盖。`export.debugText.outputDir` 不再作为调试文本根路径；**`debugText` 块以各阶段开关为主**（`code`/`token`/…）。

## `module.package.json` 根结构（当前约定）

根对象仅两个字段：

- `entryModule`：字符串，指向 `moduleList` 中入口模块的 `moduleName`。
- `moduleList`：数组，每项为完整模块对象（与原先单模块时的结构一致：`moduleName`、`irStringDict`、`classList`、`methodList` 等）。
