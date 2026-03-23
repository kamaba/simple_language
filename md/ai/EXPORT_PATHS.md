# SLIR 导出路径（约定）

- **默认输出目录**（从 `Front` 工程运行时，`Environment.CurrentDirectory` 一般为 `bin\Debug\net8.0`）：  
  `source/Front/bin/Debug/net8.0/out/export/`
- **主包文件名**：`module.package.json`（由 `ExportLangManager` → `SLModulePackageWriter.Write` 写入）。
- 可通过环境变量 **`SIMPLELANG_EXPORT_OUTDIR`** 覆盖输出目录。

## `module.package.json` 根结构（当前约定）

根对象仅两个字段：

- `entryModule`：字符串，指向 `moduleList` 中入口模块的 `moduleName`。
- `moduleList`：数组，每项为完整模块对象（与原先单模块时的结构一致：`moduleName`、`irStringDict`、`classList`、`methodList` 等）。
