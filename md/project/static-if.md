# Static If 条件编译（编译期 if）

> 适用范围：`Front` 工程，`*.sp` / `*.sl` 源码 + `<ProjectName>.jsonc` 工程配置
>
> 参考 D 语言的 `static if`：**判断发生在编译期（MetaCore 层），到 IR 层时只保留判断出来的那部分逻辑**。
>
> **核心原则：static 不进 runtime 层。** 静态条件本身在最终代码里零开销，未选中的分支连语义分析都不会参与。

---

## 1. 概述

`static if` 用来根据**工程外部定义的宏字段**（jsonc 里 `global.macro` 段），在编译期决定保留哪一段代码。典型用途：跨平台分支、调试/发布分支、按配置裁剪功能。

与普通 `if` 的区别：

| | 普通 `if` | `static if` |
|---|---|---|
| 判断时机 | 运行期 | **编译期（MetaCore 层）** |
| 判断对象 | 任意表达式 | **只能是 `global.macro.X` 宏字段参与的常量表达式** |
| 未命中分支 | 仍在 IR 中 | **完全不进 IR** |
| IR 产物 | 条件跳转指令 | 只保留命中分支内的语句，无任何条件判断指令 |

---

## 2. jsonc 配置：global.data 与 global.macro

`data` 的结构放在 `global` 下边（旧版根级 `"data"` 仍兼容读取），`macro` 用来存放 static if 静态编译的数据。两者字段定义方式完全一样，值支持**布尔 / 数值 / 字符串**：

```jsonc
{
  "global": {
    "imports": [ "Std.Console" ],
    "replace": { "DEBUG": "true" },
    "data": {
      "greeting": "hello-static-if",
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
- `global.macro`：注入为 Project 的宏成员 `global.macro`，**主要供 static if 编译期判断**；运行期也可以只读访问 `global.macro.platform`（值是编译定稿后的最终值）。

---

## 3. 语法

```python
static if <条件表达式> { }
static elif <条件表达式> { }
static else { }
```

- `elif` 是一个单词（与语言普通 `if/elif/else` 关键字一致）。
- **follow 分支（static elif / static else）必须与 static if 的 static 修饰一致**，否则报 LID 23008。即：`static if` 之后要么全是 `static elif` / `static else`，要么没有 follow 分支；不允许 `static if` 后面直接跟裸的 `elif` / `else`。
- `static if` 可以嵌套，也可以和普通 `if` 混用（选中分支里的普通 `if` 照常作为运行期逻辑保留）。

### 条件表达式支持的能力

条件里**只能**引用 `global.macro.宏名` 和常量：

| 能力 | 示例 |
|---|---|
| 比较：`==` `!=` `<` `<=` `>` `>=` | `global.macro.platform == "Win32"`、`global.macro.maxThreads > 4` |
| 布尔宏直接引用 | `static if global.macro.useFastMath` |
| 逻辑：`&&` `||` `!` | `global.macro.useFastMath && global.macro.maxThreads > 4` |
| 括号分组 | `(global.macro.a == 1 || global.macro.b == 2) && !global.macro.c` |

不支持（会报错）：

- 引用非宏变量、函数调用、下标、模板（LID 23002/23003）
- 引用未定义的宏（LID 23001）
- 宏值不是布尔/数值/字符串（LID 23005）
- 条件不是常量表达式（LID 23007）

---

## 4. 在 .sp（Project）里修改宏值：CompileBefore()

宏的初始值来自 jsonc。如果需要变化，**只能写在 `.sp` 工程文件的 `CompileBefore(){}` 里**，其它任何地方都不允许对 `global.macro` 赋值：

```python
Project
{
    _main_()
    {
        StaticIfTest.fun()
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
MacroManager.ClearExternalMacros();          // 每次编译前重置
MacroManager.SetExternalMacro("platform", "Linux");
MacroManager.SetExternalMacro("maxThreads", "2");
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

IR 分支翻转结果：

| 用例 | 结果 |
|---|---|
| `[1] platform` | 命中 `static elif "Linux"`——CompileBefore 优先级高于外部注入 |
| `[2] useFastMath` | `!useFastMath` 分支被选中（布尔宏被外部翻转为 false） |
| `[3] maxThreads > 4` | else 分支被选中（数值宏被外部改为 2） |
| `[4] &&` | 整段剔除（useFastMath=false 使条件为假，且无 else 分支） |
| `[5] 嵌套 ==8` | 嵌套 else 分支被选中（外部值 2 参与嵌套判断） |
| `[6]/[7]` | 运行期 if、global.data 访问不受影响 |

---

## 6. 编译流程（static 在哪一层生效）

```
CLI --macro / 宿主 SetExternalMacro ──┐
环境变量 SL_MACRO_* ──────────────────┤
jsonc global.macro ───────────────────┼→ InjectProjectData 步骤：
                                          LoadFromConfig 装载 jsonc 初值 → 应用外部宏（env 先、cli 后）
.sp CompileBefore ────────────────────┘    → 预扫描 CompileBefore 内的 global.macro.X = 常量 赋值，
                                            编译期求值写入 MacroManager，并把该赋值语句从语句流中移除
                                         → 把定稿后的宏值注入为 Project 静态成员（运行期只读可见）
...
ParseStatements 步骤：
    遇到 static if → MetaCore 层立刻用 MacroManager 求值
    → 只把命中分支的子语句按原顺序平铺接入当前语句链
    → 未命中分支、static if 结构本身都不进入语义分析
...
TranslateIR 步骤：
    IR 里只有命中分支的内容，没有任何 static 条件判断指令
```

要点：

- **static 的判断逻辑在编译后台 MetaCore 层就开始执行**，不是 IR 层、更不是 runtime。
- 到 IR 层时只剩 if 判断出来的那部分逻辑（见第 7 节示例的 IR 对比）。
- `static if` 不引入作用域：选中分支的语句平铺进外层，分支内定义的局部变量在外层作用域可见。

---

## 7. 完整示例（test/StaticIfTest）

**StaticIfTest.sl：**

```python
StaticIfTest
{
    static fun()
    {
        global.println("===== static if compile-time test =====")

        # 1. 字符串宏比较：CompileBefore 已把 platform 改为 "Linux"，命中 static elif
        static if global.macro.platform == "Win32"
        {
            global.println("[1] platform branch -> Win32 (should NOT print)")
        }
        static elif global.macro.platform == "Linux"
        {
            global.println("[1] platform branch -> Linux")
        }
        static else
        {
            global.println("[1] platform branch -> Other (should NOT print)")
        }

        # 2. 布尔宏直接引用
        static if global.macro.useFastMath
        {
            global.println("[2] useFastMath -> true")
        }

        # 3. 数值宏比较
        static if global.macro.maxThreads > 4
        {
            global.println("[3] maxThreads > 4")
        }

        # 4. 逻辑组合
        static if global.macro.useFastMath && global.macro.maxThreads > 4
        {
            global.println("[4] useFastMath && maxThreads > 4")
        }

        # 5. 嵌套
        static if global.macro.platform == "Linux"
        {
            static if global.macro.maxThreads == 8
            {
                global.println("[5] nested : Linux + maxThreads == 8")
            }
        }

        # 6. static if 与普通 if 混用：内部 runtime if 正常保留
        int a = 10
        static if global.macro.platform == "Linux"
        {
            if a > 5
            {
                global.println("[6] runtime if inside static if -> a > 5")
            }
        }

        # 7. global.data 注入成员（jsonc 新结构 global.data）
        global.println("[7] data greeting = " + global.greeting)
        global.println("[7] data baseCount = " + global.baseCount.toString())
    }
}
```

**编译后 IR（`out/export/StaticIfTest/DebugCode/StaticIfTest/IR.txt`，fun 方法指令）只剩：**

```
LoadConstString "===== static if compile-time test ====="; CallStatic println
LoadConstString "[1] platform branch -> Linux";            CallStatic println   ← 命中的 elif
LoadConstString "[2] useFastMath -> true";                 CallStatic println
LoadConstString "[3] maxThreads > 4";                      CallStatic println
LoadConstString "[4] useFastMath && maxThreads > 4";       CallStatic println
LoadConstString "[5] nested : Linux + maxThreads == 8";    CallStatic println
LoadConstInt32 10; StoreLocal a                                                 ← int a = 10
LoadLocal a; LoadConstInt32 5; Cgt; StoreLocal; BrFalse/Br/Label...            ← 运行期 if 完整保留
LoadConstString "[7] data greeting = "; LoadStaticField; Add; CallStatic println
LoadConstString "[7] data baseCount = "; LoadStaticField; CallVirt toString; ...
```

所有 "should NOT print" 分支、以及 `platform == "Win32"` 等 static 条件判断本身，在 IR 里**完全不存在**。

运行输出：

```
===== static if compile-time test =====
[1] platform branch -> Linux
[2] useFastMath -> true
[3] maxThreads > 4
[4] useFastMath && maxThreads > 4
[5] nested : Linux + maxThreads == 8
[6] runtime if inside static if -> a > 5
[7] data greeting = hello-static-if
[7] data baseCount = 42
```

编译验证：

```
SimpleLanguageFront.exe <StaticIfTest 路径>/ProjectTest.sp
```

---

## 8. 错误码（LID 23000 段）

| LID | 名称 | 含义 |
|---|---|---|
| 23001 | MacroUndefined | 引用了未定义的宏 |
| 23002 | NotSupportExpress | static if 条件中出现不支持的表达式 |
| 23003 | NotSupportOperate | 不支持的操作/比较类型组合 |
| 23004 | TypeMismatch | 比较两侧类型不匹配 |
| 23005 | MacroValueInvalid | 宏值不是布尔/数值/字符串 |
| 23006 | ProjectMacroManagerMacroOnlyModifyInCompileBefore | 在 CompileBefore() 之外对 global.macro 赋值 |
| 23007 | MacroNotConst | 宏赋值右侧不是常量表达式 |
| 23008 | FileMetaSyntaxStaticIfFollowKey | static if 的 follow 分支（elif/else）static 修饰不一致 |
| 23009 | ProjectMacroManagerExternalMacroApplied | 外部宏注入已应用（Info 日志，标注来源 env/cli，非错误） |

---

## 9. 相关源码位置

| 功能 | 文件 |
|---|---|
| jsonc 解析 global.data / global.macro | `source/Front/Project/ProjectJsoncLoader.cs` |
| 宏值存储、条件求值、链识别 | `source/Front/Project/MacroManager.cs` |
| 宏成员注入 + CompileBefore 预扫描 | `source/Front/Project/PorjectClass.cs`（`InjectProjectMacroMember` / `PreScanCompileBeforeMacroAssign`） |
| static if 语法识别 | `source/Front/Compile/Parse/StructParseToSyntax.cs` |
| static if FileMeta 节点 | `source/Front/Compile/FileMeta/FileMetaSyntax.cs`（`FileMetaKeyStaticIfSyntax`） |
| 编译期求值平铺（只保留命中分支） | `source/Front/Core/MetaMemberFunction.cs`（`HandleMetaSyntax` 的 `case FileMetaKeyStaticIfSyntax`） |
| 非 CompileBefore 赋值检查 | `source/Front/Core/Statements/MetaAssignStatements.cs` |
| CLI `--macro` / `-m` 参数解析 | `source/Front/CLI/CommandInputArgs.cs`（`macroDefines` / `TryAddMacroDefine`） |
| 外部宏接线（CLI → MacroManager） | `source/Front/Project/ProjectManager.cs`（`Run`） |
| 错误码定义 | `source/Front/Log/LID.cs`、`source/Front/Log/ErrorDefinitions.csv`（23000 段） |
| 测试项目 | `test/StaticIfTest/` |
