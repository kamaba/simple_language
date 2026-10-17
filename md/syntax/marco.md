# S语言的宏（global.macro）

> ⚠️ 本文档描述的是**现行宏机制**：编译期宏值 `global.macro`。
> 旧版「`$xxx` 文本替换 + `ProjectConfigBegin{ globalReplace }` 内嵌配置 + `ProjectEnter` 入口」机制已从编译器移除（词法层与工程解析层均无对应实现），不再支持。
> 权威文档：`md/project/static-if.md`（`global.macro` 与 `_before_()` 钩子）、`md/project/project-config-jsonc-guide.md`（`global.macro` 段）。

S 语言的宏不是文本替换，而是一套**编译期宏数据面**：宏值在工程 jsonc 中定义、编译期定稿，注入为 Project 静态成员 `macro`，源码中以 `global.macro.<name>` 只读访问；按平台/配置裁剪代码用 `@Exclude` attribute。

—————————————————————————————————————————————————————————

## 宏的定义（工程 jsonc）

1. 在 `<ProjectName>.jsonc` 的 `global.macro` 段定义宏，值只支持**布尔 / 数值 / 字符串**。
2. 源码中直接以 `global.macro.xxx` 读取（运行期只读，值为编译定稿后的最终值）。

```python
file:test_project.jsonc

{
  "global": {
    "macro": {
      "platform": "Win32",
      "useFastMath": true,
      "maxThreads": 8
    }
  }
}
```

```python
file:test_project.sp

Project
{
    _main_()
    {
        if global.macro.useFastMath
        {
            global.println("fast math on")
        }
        global.println(global.macro.platform)   # Win32
    }
}
```

## 宏的修改：_before_() 钩子

宏值只能在 `.sp` 工程文件 `Project` 类的 `_before_(){}` 钩子中修改（编译期求值，唯一合法的源码内修改入口，其它位置对 `global.macro` 赋值直接报错），右值只允许常量（布尔/数值/字符串）或另一个宏引用 `global.macro.Y`：

```python
Project
{
    _before_()
    {
        # 编译期把 platform 从 jsonc 里的 "Win32" 改为 "Linux"
        global.macro.platform = "Linux"
    }
    _main_()
    {
        global.println(global.macro.platform)   # Linux
    }
}
```

## 外部注入（优先级从低到高）

| 来源 | 形式 |
|------|------|
| jsonc 初值 | `global.macro` 段 |
| 环境变量 | `SL_MACRO_<name>=<value>` |
| CLI | `--macro name=value`（可重复） |
| 钩子 | `Project._before_(){}` 内赋值（优先级最高） |

## 条件编译（@Exclude）

旧的 `static if` / `static elif` / `static else` 编译期条件已废弃（继续书写报 **LID 21467**），按平台/配置裁剪成员或类改用 `@Exclude` attribute（PreCompile 时点，标注即跳过编译，成员不进 module.json）：

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

详见 `md/syntax/attribute.md` 与 `md/project/static-if.md`。
