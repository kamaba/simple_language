# 语言基础（Base Syntax）

本节说明 **S 语言与工程是一体的**：没有单独的「零散脚本」模型，日常开发以 **工程目录** 为单位，由 **`.sp` 入口** + **同名 `.jsonc` 配置** + **若干 `.sl` 源码** 共同描述「编译什么、从哪运行、全局注入什么」。

更细的工程字段与 CLI 见 [`../project/project-config-jsonc-guide.md`](../project/project-config-jsonc-guide.md)；入口与 `global` 见 [`../project/project_sp-guide.md`](../project/project_sp-guide.md) 与 [`./global.md`](./global.md)。

---

## 1. 概览

- **源码**：以 **`.sl`** 为主（仓库内 Core、测试、示例均如此）；文档或历史材料里偶见 `.s` 写法，语义上仍指同一类源文件。
- **工程入口**：**`<工程名>.sp`**，内写工程级声明（如 `Project { }` / `ProjectEnter { }`）、运行入口与可暴露给全工程的成员。
- **工程配置**：与 `.sp` **同名、同目录** 的 **`<工程名>.jsonc`**（JSON with Comments），供 `Front` 加载：源码根、参与编译的文件列表、编译选项、`global` 段、`references`、`struct` 等。
- **语言特性**：受 C#/Dart 启发，支持 `namespace`、`class`、`data`、`enum`、`interface`、函数、模板/泛型、模块化等；编译产物经 **IR** 导出后可由内置 **VM** 执行（见 `introduction.md`、[`../ai/EXPORT_PATHS.md`](../ai/EXPORT_PATHS.md)）。

---

## 2. 工程三件套（必须先理解）

| 角色 | 文件 | 作用 |
|------|------|------|
| 入口 | `<Name>.sp` | 声明工程级 `Project` / `ProjectEnter` 块；定义 **`_main_` / `_test_`** 等入口；可在此声明供 **`global.xxx`** 使用的静态成员与函数。 |
| 配置 | `<Name>.jsonc` | 声明 **`source.root`**、**`compileFiles.files`**、**`compile`** 开关、**`global.imports` / `global.replace` / `global.data`**、**`references`**、**`struct.tree`** 等。 |
| 源码 | 多个 **`.sl`** | 实际类型与业务逻辑；路径相对于 `jsonc` 所在目录或 `source.root` 约定（以加载器与 `jsonc` 为准）。 |

约定：**`<Name>.sp` 与 `<Name>.jsonc` 必须同名且同目录**；`sl c` 会在当前目录查找 `.sp` 并加载对应 `jsonc`。同一目录建议只保留 **一个** `.sp`，避免多入口歧义。

---

## 3. `.jsonc` 里要表达什么（方向）

`jsonc` 描述的是 **「编译器如何收束工程」**，而不是业务语法本身。典型职责包括：

- **`project`**：工程名、版本号、描述等元信息。
- **`source`**：源码根 **`root`**、可选 **`entryFile`** 等，决定 `.sl` 从哪里开始组织。
- **`compileFiles.files`**：列出参与编译的 `.sl`（`path`、`group`、`tag`、`ignore`、`priority`），与 **`compileFilter`** 一起做分组/标签筛选。
- **`compile`**：优化、目标平台、debug、分号策略、是否强制 `class` 关键字等开关。
- **`global`**：
  - **`imports`**：工程级默认 import 等；
  - **`replace`**：全局文本/宏类替换配置；
  - **`data`**：注入到运行时的结构化常量（在语言侧通过 **`global.xxx`** 访问，见下节与 [`./global.md`](./global.md)）。
- **`references`**：引用其它目录/库。
- **`struct`**：结构树（命名空间/类等）供工程组织与校验使用。

完整字段说明、示例与 CLI（`sl new project`、`sl new classfile`、`sl c`、`sl c -e ir`）见 [**工程配置（JSONC）**](../project/project-config-jsonc-guide.md)。

---

## 4. `.sp` 里要表达什么（方向）

`.sp` 描述的是 **「工程对外暴露什么 + 从哪里启动」**，与 `jsonc` 互补：

- **`Project { }` 或 `ProjectEnter { }`**：工程级容器；内置 **Core** 使用 `Project { }`，部分 Std/测试使用 `ProjectEnter { }`，二者在「工程入口块」语义上一致，以你工程实际为准。
- **运行入口**
  - **`_main_()`**：正常运行时进入。
  - **`_test_()`**：测试模式入口。
- **`global` 映射**：在 **`Project` / `ProjectEnter` 块内** 声明的静态成员与函数，会在语言侧映射为 **`global.成员名` / `global.函数(...)`**（详见 [`./global.md`](./global.md) 与 [`../project/project_sp-guide.md`](../project/project_sp-guide.md)）。
- **编译期钩子**：可在入口附近或 `Compile` 相关约定中扩展 **编译前/编译后** 逻辑（具体命名与挂载方式以 [`../project/project_sp-guide.md`](../project/project_sp-guide.md) 与当前 `Core.sp` 为准）。

示例（摘录自主线 `Core.sp` 形态，仅说明结构）：

```sl
Project
{
    float Pi = 3.14f

    print( object str )
    {
        SystemPrint(str)
    }

    _main_()
    {
        # 调用某测试或业务入口
        SomeModule.fun()
    }

    _test_()
    {
    }
}
```

---

## 5. `global` 与配置的关系（方向）

当前语义可记两句话：

1. **`global.xxx` / `global.func()`**（非 `global.data` 树形配置段）：主要来自 **`.sp` 里 `Project` / `ProjectEnter` 的静态成员与函数**。
2. **`global` 上来自配置的常量/数据**：来自 **`.jsonc` → `global.data`**（数值、字符串、数组、嵌套对象等会注入为可链式访问的成员，规则见 [`../project/project_sp-guide.md`](../project/project_sp-guide.md) 第 3 节）。

示例用例：**`test/BaseTest/GlobalTest.sl`**。

---

## 6. 注释

- **单行注释**：`# 注释内容`（以及工程中常见的 `//`，若 lexer 支持则与 `#` 等价使用场景以编译器为准）。
- **块注释**：`#! ... !#`，支持嵌套与 Markdown 片段（如 `#!md ... !#`）。
- 测试目录中常在文件末尾用连续 **`# ...`** 记录用例意图与预期输出，不参与编译。

---

## 7. 标识符与关键字

- **标识符**：以字母或下划线开头，后续可为字母、数字、下划线；区分大小写。
- **字符串插值**：使用 **`$名称`**、**`$对象.成员`** 或 **`${表达式}`**，不使用 `@` 前缀（见 [`./string.md`](./string.md)）。
- **关键字**（节选）：`namespace, import, class, enum, data, interface, abstract, override, static, public, private, if, elif, else, for, while, dowhile, switch, case, break, continue, ret, is` 等；工程入口相关：**`Project` / `ProjectEnter`、`_main_`、`_test_`**。

---

## 8. 最小串联示例（概念）

下面用「一个假工程名 `Demo`」把三件套串起来（路径与类名仅作演示）。

**`Demo.jsonc`**（只列关键字段；完整形态见工程配置文档）：

```jsonc
{
  "project": { "name": "Demo" },
  "source": { "root": "src" },
  "compileFiles": {
    "files": [{ "path": "Main.sl", "group": "main", "tag": "all", "ignore": false, "priority": 0 }]
  },
  "global": {
    "data": { "appTitle": "HelloApp" }
  }
}
```

**`Demo.sp`**：

```sl
import Std

Project
{
    _main_()
    {
        Console.print(global.appTitle)
    }
}
```

**`src/Main.sl`**：放置 `namespace` / `class` 等业务代码；若 `global.data` 与 `Project` 成员同名，以加载与合并规则为准（优先查工程文档与测试）。

更多语法细节见本目录下各主题文档（[`./namespace.md`](./namespace.md)、[`./class.md`](./class.md)、[`./function.md`](./function.md)、控制流、集合等）。
