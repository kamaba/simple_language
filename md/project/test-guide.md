# 测试引导：测试用例与测试工程对照

> 面向三个问题：**测试用例源码在哪里？每个测试工程测什么？想测某个功能该跑哪个工程？**

仓库测试分两层：

- **测试用例集**：[`test/`](../../test/) 下的 SimpleLanguage 源码，每个子目录一个主题（`ProjectTest.sp` + `ProjectTest.jsonc` + 若干 `.sl`）。
- **测试宿主工程**：[`project/`](../../project/) 下的 C# 控制台工程，负责「调用 C# 前端编译测试集 → 导出模块 JSON → 交给 VM 运行」。
  - `CSimpleVM*` 系列 → **C VM**（`../csimple_lang`，`csimple_lang.exe` / `csimple_lang_dll.dll`）
  - `Test` / `Test2` → **C# VM**（`source/VM/SimpleLanuageVM.csproj`）

---

## 1. 测试用例集（`test/`）

| 目录 | 测什么 | 代表用例 | 推荐宿主工程 |
|------|--------|----------|--------------|
| `BaseTest/` | 语言核心语法：变量/作用域、类/继承/接口、泛型与模板、enum/data、闭包、协程、控制流、容器（List/Map/Set/Queue/Stack/Tuple）、Float8/16、指针/引用、Result、goto 等（60+ 个 `.sl`） | `GlobalTest.sl`、`StringTest.sl`、`GenClass.sl`、`ClosureTest.sl`、`CoroutineTest.sl`、`TryTest.sl`、`Float8Test.sl` | `CSimpleVMCoreTest`（C VM）/ `Test`（C# VM） |
| `NullFastTest/` | Null 相关快速回归（单文件） | `NullFastTest.sl` | `CSimpleVMTest`（传参 `1`） |
| `ExpendTest/` | 标准库与扩展库：IO（Console/File/Directory/DateTime）、数据格式（Json/Xml/Yaml/Toml/Csv）、嵌入式数据库（Sqlite3）、容器（List/Map/LinkedList/Sort）、Isolate、宏、Attribute、模块与跨语言调用 | `JsonTest.sl`、`FileTest.sl`、`Sqlite3Test.sl`、`ListTest.sl`、`IsolateTest.sl`、`AttributeTest.sl` | `CSimpleVMStdTest`（C VM）/ `Test2`（C# VM） |
| `MathTest/` | 数学库：内置 Math 成员函数、MathVMLib 系统方法、运算符分派、矩阵（3x3/4x4/任意尺寸）、FFI 数学、大数（BigNumber/BigDecimal）、因数分解 | `MathTest.sl`、`FFIMathTest.sl`、`Matrix4x4Test.sl`、`MatrixBigTest.sl` | `CSimpleVMMathTest` |
| `SpecialTest/` | FFI 与跨语言互操作、MLIR AOT 实验 | `FFITest.sl`、`CSharpCall.sl`、`CPythonCall.sl`、`JavascriptCall.sl`、`LuaCall.sl`、`AOTTest.sl` | `CSimpleVMSpecialTest` |
| `BenchMark/` | 性能基准 | `Fibonacci.sl`、`Loop.sl`、`Levenshtein.sl`、`StringBench.sl` | `CSimpleVMBenchMarkTest` |
| `MysqlTest/` | MySQL 库（需本机数据库环境） | `MysqlTest.sl`、`MysqlApiTest.sl` | 无专属宿主，向任一宿主传参运行 |
| `RenderTest/` | 渲染相关（进行中） | `MathTest.sl` | `CSimpleVMRenderTest`（需传参，见下） |
| `TensoraTest/` | 张量与机器学习库：张量、自动微分、CNN/NN、经典 ML、TTS | `TensorTest.sl`、`AutogradTest.sl`、`CnnTest.sl` | 无专属宿主，向任一宿主传参运行 |

---

## 2. 测试宿主工程（`project/`）

| 工程（csproj） | 默认测试集（无参数时） | VM | 说明 |
|----------------|------------------------|----|------|
| `CSimpleVMCoreTest` | `test\BaseTest\ProjectTest` | C | 语言核心语法回归 |
| `CSimpleVMTest` | `test\ExpendTest\ProjectTest`；传参 `1` → `test\NullFastTest\ProjectTest` | C | 通用回归 |
| `CSimpleVMStdTest` | `test\ExpendTest\ProjectTest` | C | 标准库 / 扩展库 |
| `CSimpleVMMathTest` | `test\MathTest\ProjectTest` | C | 数学库 |
| `CSimpleVMSpecialTest`（csproj 名为 `CSimpeVMSpecialTest.csproj`） | `test\SpecialTest\ProjectTest` | C | FFI / 互操作 / AOT |
| `CSimpleVMBenchMarkTest` | `test\BenchMark\BenchMark` | C | 性能基准 |
| `CSimpleVMRenderTest`（csproj 名为 `RenderTest.csproj`） | `test\ExpendTest\ProjectTest`（跑渲染用例需传 `test\RenderTest\ProjectTest`） | C | 渲染 |
| `Test`（`SimpleLanguageTest.csproj`） | `test\BaseTest\ProjectTest` | C# | C# 前端 + C# VM 全链路 |
| `Test2`（`SimpleLanguageTest2.csproj`） | `test\ExpendTest\ProjectTest` | C# | 同 `Test`（第二套） |

### C VM 构建方式（csproj 内 `BuildCVM` Target）

- **Ninja/clang 树（推荐，已统一）**：`CSimpleVMTest` / `CSimpleVMStdTest` / `CSimpleVMMathTest` / `CSimpleVMSpecialTest` 构建时自动用 CMake（Ninja + clang）构建 `csimple_lang\build\Debug`，与命令行开发共用**同一棵树**，同时产出 `csimple_lang.exe` 与 `csimple_lang_dll.dll`。
- **VS vcxproj**：`CSimpleVMCoreTest` 通过 `build_cvm.bat`（vswhere 定位 VS MSBuild）构建 `csimple_lang\project\vs\vm_lib\csimple_lang_lib.vcxproj`。
- **旧 VS 生成器树（待迁移）**：`CSimpleVMBenchMarkTest` / `CSimpleVMRenderTest` 仍用 `cmake -B build`（VS 多配置树）。⚠️ 其输出目录与 Ninja 树相同，两边交替构建会互相覆盖 DLL，引发 P/Invoke 加载旧版 DLL 等问题。

---

## 3. 一个测试集长什么样

每个测试集是「三件套」+ 若干用例文件，以 `test/BaseTest/` 为例：

```
test/BaseTest/
├── ProjectTest.sp      # 入口：_main_ 里逐个调用 XxxTest.fun()
├── ProjectTest.jsonc   # 工程配置：compileFiles.files 列出参与编译的 .sl
├── ObjectTest.sl       # 用例：一个类，提供 fun() 测试入口
├── StringTest.sl
└── ...（60+ 个 .sl）
```

**新增一个用例**（三步）：

1. 在测试集目录新建 `XxxTest.sl`，写一个类并提供 `fun()`（或带断言的测试方法）；
2. 在 `ProjectTest.jsonc` 的 `compileFiles.files` 里注册该文件；
3. 在 `ProjectTest.sp` 的 `_main_` 中追加 `XxxTest.fun();`。

---

## 4. 怎么跑

### Visual Studio

打开 csproj（或解决方案）直接 **F5 调试 / Ctrl+F5 运行**。构建时会先自动触发 C VM 构建（见上节）。宿主运行前会把工作目录切到测试集目录，因此用例里的相对路径资源（如 `Sqlite3Test` 的 `Resources/`）在 VS 与命令行下行为一致。

### 命令行（dotnet）

```powershell
# 按工程默认测试集跑（Debug 配置）
dotnet run --project project\CSimpleVMMathTest\CSimpleVMMathTest.csproj -c Debug

# Release 配置
dotnet run --project project\CSimpleVMStdTest\CSimpleVMStdTest.csproj -c Release

# 用任意宿主跑任意测试集：第一个参数传测试集路径（不带 .sp 也可）
dotnet run --project project\CSimpleVMStdTest\CSimpleVMStdTest.csproj -- test\MysqlTest\ProjectTest

# CSimpleVMTest 传 "1" 跑 NullFastTest
dotnet run --project project\CSimpleVMTest\CSimpleVMTest.csproj -- 1
```

### Debug 与 Release 的行为差异（`CSimpleVM*` 宿主）

| 配置 | C VM 运行方式 | 适合场景 |
|------|---------------|----------|
| Debug | **P/Invoke** 进程内加载 `csimple_lang\build\Debug\bin\csimple_lang_dll.dll` | C# 与 C 代码可在同一进程断点调试 |
| Release | 启动独立进程 `csimple_lang.exe run <module.json>`（优先 `build\Debug\bin`，回退 `build\Release\bin`） | 性能 / 接近真实部署 |

### 常用参数

| 参数 | 作用 |
|------|------|
| `<测试集路径>` | 指定要编译运行的测试集（可相对仓库根或绝对路径，可不带 `.sp`） |
| `-test` | 运行工程入口的 `_test_` 而不是 `_main_` |
| `-start=false` | 跳过前端编译，直接运行上一次导出的模块 |

---

## 5. 常见问题

- **`csimple_lang.exe not found`**：C VM 尚未构建。用 Ninja 树模式的工程在 VS / dotnet 构建时会自动构建；手动构建：`cd csimple_lang && cmake --build build\Debug --target csimple_lang csimple_lang_dll`。
- **字符串错乱 / 断言随机失败**：通常是新旧两棵构建树互相覆盖 DLL 所致，坚持只用 `csimple_lang\build\Debug` 这棵 Ninja 树（见第 2 节构建方式说明）。
- **前端编译失败**：看 `out/export/<测试集名>/` 与 `build/logs/` 下的日志定位（详见 [`md/ai/DEBUG_WORKFLOW.md`](../ai/DEBUG_WORKFLOW.md)、[`md/ai/EXPORT_PATHS.md`](../ai/EXPORT_PATHS.md)）。
