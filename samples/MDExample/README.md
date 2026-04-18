# MDExample — 文档示例源码

本目录为 **`md/syntax`** 等文档中示例的**可运行源码**落地位置；每个子文件夹是一个**独立主题**（各自 `README` + `.sl` + 供合并到 **`Core.jsonc`** 的一行片段）。

## 为何并入 `Core` 而不是单独 `.sp`

当前编译器在一次进程内对**多工程/二次加载**的支持有限；单独小工程若缺少与 **`source/Front/Lib/Core`** 相同的依赖图，容易出现 Meta/IR 阶段缺失类型（如 `NativeBridge`、`BridgeKind` 等）。因此推荐：**把示例 `.sl` 登记进 `Core.jsonc` 的 `compileFiles.files`，并在 `Core.sp` 的 `_main_` 里调用对应 `Run()`**。

仓库主线里 **已在 `source/Front/Lib/Core/Core.jsonc` 中登记** 下列五个 `Doc*.sl`（`group`: `mdex`）。调试时在 `Core.sp` 的 `_main_` 里调用对应 `Run()` 即可；若不需要参与编译，可将对应条目的 **`ignore`** 改为 `true` 或删除该行。

示例里打印使用 **`global.println`**（与 `test/BaseTest` 一致，依赖 `Core.sp` 里 `Project` 的映射）。文档里若写 `Console.print`，在并入 Core 调试时可等价改用 `global.println`。

当前默认 `_main_` 仍会跑 `ArrayTest` 等用例；在本机 Front 版本上，完整编到 **IR** 阶段可能触发既有断言（与 `new`/`IRMetaCallLink` 相关）。若只验证 **Meta**，日志里出现 **`编译Meta层结束`** 即表示含 `MDExample` 在内的源码已通过 Meta。

## 子目录

| 目录 | 文档 | 入口类 |
|------|------|--------|
| `01_BaseRectangle/` | `md/syntax/base.md` | `MdEx01BaseRectangle.Run` |
| `02_StringInterpolation/` | `md/syntax/string.md` | `MdEx02StringInterpolation.Run` |
| `03_GlobalJsonc/` | `md/syntax/global.md` | `MdEx03GlobalJsonc.Run` |
| `04_RangeFor/` | `md/syntax/range.md` | `MdEx04RangeFor.Run` |
| `05_SwitchValue/` | `md/syntax/switch.md` | `MdEx05SwitchValue.Run` |

每个子目录内的 **`CORE_JSONC_COMPILE_FILES_APPEND.txt`** 为要粘贴到 `Core.jsonc` 的 **`compileFiles.files`** 中的**一行**（注意逗号与 JSON 语法）。

## 通用步骤

1. 将对应 `CORE_JSONC_COMPILE_FILES_APPEND.txt` 中的条目追加到 `source/Front/Lib/Core/Core.jsonc`。
2. 在 `Core.sp` 的 `_main_()` 中调用上表中的 `Run()`（一次只测一个示例时，其它测试调用可暂时注释）。
3. 按仓库 README 执行编译/运行，查看导出目录下 `Logs/Result.txt`。
