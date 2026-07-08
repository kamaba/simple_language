# 环境与运行（Environment & Execution）

本节描述如何组织项目、运行脚本和使用命令行工具启动 S 语言程序。

项目入口：
- 使用 `ProjectEnter { ... }` 声明项目入口和静态方法（如 `static Main()`、`static Test()`）。

运行与命令行：
- `run project.sp`：构建并运行 `ProjectEnter` 中的 `Main`。
- `run project.sp -test`：运行 `Test`（如果存在）。

配置文件：
- `ProjectConfig`（通常在 `.sp` 文件内以 `const data` 声明）包含 `compileFileList`、`globalVariable` 等编译/运行时配置。

示例：

```bash
run test_project.sp
run test_project.sp -test
```

运行时集成：
- 可通过 `import CSharp.System` 等引入底层 .NET 功能。
- 标准库位于 `source/Lib/Core`，运行时绑定在 `source/IR/Lib` 与 `source/VM`。

