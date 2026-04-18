# 01 — 基础类与插值（Base）

- **文档**：`md/syntax/base.md`
- **源码**：`Doc01_BaseRectangle.sl`

## 如何编译运行

当前 Front 在一次进程内通常只完整加载首个工程；**文档示例**建议并入主线 **`source/Front/Lib/Core`** 再编译：

1. 打开 `source/Front/Lib/Core/Core.jsonc`，在 **`compileFiles.files`** 数组中**追加一行**（见同目录 `CORE_JSONC_COMPILE_FILES_APPEND.txt`）。
2. 打开 `source/Front/Lib/Core/Core.sp`，在 **`_main_()`** 中调用：`MdEx01BaseRectangle.Run();`
3. 按仓库说明执行 `dotnet run ...`，产物见 `export.outputDir` 下 `Logs/Result.txt` 等。

调试完可从 `Core.jsonc` 与 `Core.sp` 中删掉上述追加内容。
