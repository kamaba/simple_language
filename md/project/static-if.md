# global.macro 宏与编译期条件（static if 已废弃）

> 适用范围：`Front` 工程，`*.sp` / `*.sl` 源码 + `<ProjectName>.jsonc` 工程配置
>
> ⚠️ **`static if` / `static elif` / `static else` 编译期条件编译已废弃**（attribute 重构，`csimple_lang/md/design/ATTRIBUTE_DESIGN.md` §7 旧隔离机制收编）：
> 现在写 `static if` 会报 **LID 21467 Error**，不再做编译期分支裁剪，迁移方式见第 2 节。
>
> **保留部分**：`global.macro` 宏数据面全套（jsonc 定义 / `CompileBefore()` 编译期修改 / CLI `--macro` / 环境变量 `SL_MACRO_*` / 宿主 API / 运行期只读访问），本文第 3~6 节仍有效。

---

## 1. 概述

`global.macro` 是一套**编译期宏数据面**：宏值在编译期定稿，注入为 Project 静态成员 `macro`，运行期可只读访问（`global.macro.X`）。

与 `global.data` 的区别：

| | `global.data` | `global.macro` |
|---|---|---|
| 注入形式 | Project 数据成员 `global.<name>` | Project 宏成员 `global.macro.<name>` |
| 源码内修改入口 | 无（只读） | `.sp` 的 `CompileBefore(){}`（编译期，唯一合法修改点） |
| 编译前外部注入 | 无 | CLI `--macro` / 环境变量 `SL_MACRO_*` / 宿主 API |
| 运行期 | 只读 | 只读（值是编译定稿后的最终值） |

---

## 2. static if 已废弃：迁移指引

旧 `static if` 的各用途迁移去向：

| 旧用途 | 迁移到 |
|--------|--------|
| 按平台/配置裁剪整个成员或类 | `@Exclude` attribute（PreCompile 时点，标注即跳过编译，成员不进 module.json） |
| 按工程配置剔除整个文件 | jsonc `compileFiles.files[].ignore`（工程级配置，见 `md/project/project-config-jsonc-guide.md`） |
| 运行期自适应分支 | 普通 `if` + `Environment.current`（见 `md/project/environment-guide.md`） |
| 读取配置值 | 保留：`global.macro.X` 运行期只读访问 |

`@Exclude` 用法（标准库 `Lib/Core/Exclude.sl`，PreCompile · 全点位）：

```python
class Foo
{
    @Exclude
    static winOnlyFun()
    {
        # 该方法不参与编译，不进 module.json
    }
}
```

- `@Exclude` 当前为 v1 无条件形态（标注即跳过）；条件参数（平台/DEBUG 等常量求值）为后续形态，见 ATTRIBUTE_DESIGN §6.9。
- 旧代码里的 `static if`：Front 保留 `static` 修饰检测并报 Error（LID 21467），提示改用 `@Exclude` 或 `compileFiles.ignore`；报错后该语句**按普通 `if` 继续解析**（退化为运行期逻辑），不会静默改变语义。

---

## 3. jsonc 配置：global.data 与 global.macro

`data` 的结构放在 `global` 下边（旧版根级 `"data"` 仍兼容读取），`macro` 用来存放编译期宏数据。两者字段定义方式完全一样，值支持**布尔 / 数值 / 字符串**：

```jsonc
{
  "global": {
    "imports": [ "Std.Console" ],
    "replace": { "DEBUG": "true" },
    "data": {
      "greeting": "hello-macro",
      "baseCount": 42
    },
    "macro": {
      "platform": "Win32",
      "useFastMath": true,
      "maxThreads": 8
    }
  }
}
```

区别：

- `global.data`：注入为 Project 的数据成员，**运行期可读**（`global.greeting`），用于普通数据。
- `global.macro`：注入为 Project 的宏成员 `global.macro`，**编译期由 `CompileBefore()` 求值消费**（不进 runtime 语句流）；运行期也可以只读访问 `global.macro.platform`（值是编译定稿后的最终值）。

---

## 4. 在 .sp（Project）里修改宏值：CompileBefore()

宏的初始值来自 jsonc。如果需要变化，**只能写在 `.sp` 工程文件的 `CompileBefore(){}` 里**，其它任何地方都不允许对 `global.macro` 赋值：

```python
Project
{
    _main_()
    {
        global.println(global.macro.platform)
    }
    CompileBefore()
    {
        # 编译期把 platform 从 jsonc 里的 "Win32" 改为 "Linux"
        global.macro.platform = "Linux"
    }
}
```

规则：

- 只允许 `global.macro.X = <常量>` 形式（`==` 不是赋值；复合赋值、`++`/`--` 均不支持）。
- `CompileBefore()` 内的宏赋值在编译期被求值并**消费掉**——不会出现在最终代码里（不进 runtime）。
- 在 `CompileBefore()` 之外（任何 `.sl`、`.sp` 的其它函数里）对 `global.macro` 赋值，编译直接报错：**LID 23006**（"global.macro 只能在 Project 的 CompileBefore() 中修改"）。

---

## 5. 外部宏注入接口（编译前由外部环境设置）

jsonc 里的 `global.macro` 只是**初值**。编译前，外部环境（不同编译机器 / CI 流水线 / IDE 构建配置）可以通过三个入口设置宏值：

### 5.1 CLI 参数（推荐）

```
SimpleLanguageFront.exe <Project.sp> --macro platform=Linux --macro useFastMath=false
# 或简写 -m，可重复出现，后者覆盖前者；compile/run/export 命令同样支持
SimpleLanguageFront.exe compile -p <Project.sp> -m maxThreads=2
```

### 5.2 环境变量

```
# SL_MACRO_<宏名>=<值>，优先级低于 CLI --macro
set SL_MACRO_platform=Linux
SimpleLanguageFront.exe <Project.sp>
```

### 5.3 宿主程序 API

IDE / 构建脚本等直接嵌入 Front 库时，在 `ProjectManager.Run` 之前调用：

```csharp
// SimpleLanguage.Project 命名空间
CompileBeforeManager.ClearExternalMacros();   // 每次编译前重置
CompileBeforeManager.SetExternalMacro("platform", "Linux");
CompileBeforeManager.SetExternalMacro("maxThreads", "2");
ProjectManager.Run(spPath, inputArgs);
```

### 5.4 值类型推断

外部传入的都是字符串，按以下规则推断类型：

| 传入值 | 推断类型 |
|---|---|
| `true` / `false` | 布尔（注意：必须小写，`True` 会被当作字符串） |
| `8` / `2` / `1.5` | 数值 |
| 其它（如 `Linux`、`x86_64-pc`） | 字符串（原样保留） |

### 5.5 优先级链（从低到高）

```
jsonc global.macro 初值  <  环境变量 SL_MACRO_*  <  CLI --macro / 宿主 SetExternalMacro  <  CompileBefore() 编译期修改
```

- 外部注入可以覆盖 jsonc 里已定义的宏，也可以**追加 jsonc 中没有的新宏**。
- `CompileBefore()` 依然是最高优先级——即使外部传了 `platform=Mac`，`.sp` 里 `CompileBefore()` 的 `global.macro.platform = "Linux"` 仍会生效。
- 每次应用外部宏时会输出 Info 日志（LID 23009），标注来源（env / cli），便于 CI 排查。

### 5.6 验证示例

jsonc 初值：`platform="Win32", useFastMath=true, maxThreads=8`，`.sp` 的 `CompileBefore()` 把 platform 改为 `"Linux"`：

```
SimpleLanguageFront.exe ProjectTest.sp --macro useFastMath=false --macro maxThreads=2
```

日志（LID 23009）：

```
macro: 外部设置已应用: useFastMath = false (cli)
macro: 外部设置已应用: maxThreads = 2 (cli)
```

运行期读取定稿值：`global.println(global.macro.platform)` 输出 `Linux`（CompileBefore 优先级高于外部注入）。

---

## 6. 编译流程（宏在哪一层生效）

```
CLI --macro / 宿主 SetExternalMacro ──┐
环境变量 SL_MACRO_* ──────────────────┤
jsonc global.macro ───────────────────┼→ InjectProjectData 步骤：
                                          LoadFromConfig 装载 jsonc 初值 → 应用外部宏（env 先、cli 后）
.sp CompileBefore ────────────────────┘    → 预扫描 CompileBefore 内的 global.macro.X = 常量 赋值，
                                            编译期求值写入 CompileBeforeManager，并把该赋值语句从语句流中移除
                                         → 把定稿后的宏值注入为 Project 静态成员（运行期只读可见）
```

要点：

- 宏赋值/求值在编译后台 **MetaCore 层之前**（InjectProjectData 步骤）完成，不进 IR、不进 runtime。
- 语句流中的宏赋值语句被**移除**；其余语句照常编译。
- 运行期只能通过 `global.macro.<name>` 只读访问定稿值。

---

## 7. 错误码

| LID | 名称 | 含义 |
|---|---|---|
| 21467 | FileMetaSyntaxStaticIfDeprecated | `static if` 编译期条件编译已废弃：请改用 `@Exclude` attribute（PreCompile）或工程级 `compileFiles.ignore` |
| 23001 | ProjectMacroManagerMacroUndefined | CompileBefore 引用了未在 `global.macro` 中定义的宏 |
| 23002 | ProjectMacroManagerNotSupportExpress | CompileBefore 宏表达式不支持该语法 |
| 23005 | ProjectMacroManagerMacroValueInvalid | 宏值不是布尔/数值/字符串 |
| 23006 | ProjectMacroManagerMacroOnlyModifyInCompileBefore | 在 `CompileBefore()` 之外对 `global.macro` 赋值 |
| 23007 | ProjectMacroManagerMacroNotConst | 宏赋值右侧不是常量表达式 |
| 23009 | ProjectMacroManagerExternalMacroApplied | 外部宏注入已应用（Info 日志，标注来源 env/cli，非错误） |

（23000 / 23003 / 23004 / 23008 已随 static if 条件求值链一并删除。）

---

## 8. 相关源码位置

| 功能 | 文件 |
|---|---|
| jsonc 解析 global.data / global.macro | `source/Front/Project/ProjectJsoncLoader.cs` |
| 宏值存储、CompileBefore 赋值求值 | `source/Front/Project/CompileBeforeManager.cs` |
| 宏成员注入 + CompileBefore 预扫描 | `source/Front/Project/PorjectClass.cs`（`InjectProjectMacroMember` / `PreScanCompileBeforeMacroAssign`） |
| `static` 修饰检测（废弃报错 LID 21467） | `source/Front/Parse/StructParseToSyntax.cs` |
| 非 CompileBefore 赋值检查 | `source/Front/Core/Statements/MetaAssignStatements.cs` |
| CLI `--macro` / `-m` 参数解析 | `source/Front/CLI/CommandInputArgs.cs`（`macroDefines` / `TryAddMacroDefine`） |
| 外部宏接线（CLI → CompileBeforeManager） | `source/Front/Project/ProjectManager.cs`（`Run`） |
| `@Exclude` 处理器（PreCompile 跳过） | `source/Front/Core/AttributeManager.cs`（`RegisterPreCompileExcludeHandler` / `ShouldExcludeByPreCompileAttribute`）、`Lib/Core/Exclude.sl` |
| 错误码定义 | `source/Front/Log/LID.cs`、`source/Front/Log/ErrorDefinitions.csv` |
