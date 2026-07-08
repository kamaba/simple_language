# 极简语言 [English](https://github.com/kamaba/simple_language/blob/main/README.md)

------------------------------------------------------------------------

### 简介

极简语言（SimpleLanguage）是一门**静态类型、纯面向对象**的语言，初版在 C# 生态上实现。语法整体接近 C#，并吸收其它语言的习惯写法。**语言与工程强绑定**：编译、入口与全局配置都通过工程文件驱动，不能脱离工程单独当零散脚本使用。

语言分三期能力（路线概览）：

- **第一阶段**：前端解析；与 C# 平台集成；完整语言体系；模板与数据等特色类型；导出 IR 并在内置 VM 上运行。
- **第二阶段**：C99 重写；导出 C# IR / JVM 等；与 C#/C/C++ 或 JVM 库互操作；可导出 JavaScript 等；可导出 C99 并编为库。
- **第三阶段**：LLVM 等后端；多目标导出；本地化运行时与链接发布；自有 VM 演进。

### 语言特色

1. 写法简单，无强制格式化，代码块以大括号为主。
2. 注释支持多层嵌套，并支持 Markdown 风格注释。

### 语言宗旨

1. 可读性强  
2. 可写性强  
3. 轻度语法糖须建立在 1、2 之上  
4. 纯面向对象  
5. 适度使用继承与接口；避免重名变量等模糊写法  

### 文档与工程（精华）

- **总索引**（推荐收藏）：[`md/INDEX.md`](md/INDEX.md) — 语法、工程、VM、日志、AI 文档的统一目录。  
- **语法总述**：[`md/syntax/introduction.md`](md/syntax/introduction.md) — 文档覆盖范围、与 IR 的关系、示例约定。  
- **工程配置（当前）**：入口 **`<项目名>.sp`** 与 **同名 `<项目名>.jsonc`** 放在同一目录；JSONC 中配置源码根、入口文件、编译列表、`compile`/`global`/`references` 等。详见 [`md/project/project-config-jsonc-guide.md`](md/project/project-config-jsonc-guide.md)。  
- **入口与 `global`**：`_main_` / `_test_` 约定，`global` 与 `Project{}`、`jsonc` 里 `global.data` 的注入关系见 [`md/project/project_sp-guide.md`](md/project/project_sp-guide.md)。  
- **调试与导出路径**：编译与 VM 产物、日志与 `DebugCode` 流水线见 [`md/ai/DEBUG_WORKFLOW.md`](md/ai/DEBUG_WORKFLOW.md)、[`md/ai/EXPORT_PATHS.md`](md/ai/EXPORT_PATHS.md)。  
- **常用 CLI**（摘自工程配置说明）：`sl new project`、`sl new classfile`、`sl c`、`sl c -e ir` 等，与 `jsonc` 联动方式见上文 jsonc 指南。

### 语言初体验

```csharp
file:test.sp

import CSharp.System;

DemoClass
{
    a = 0i;
    b = 100i;

    _init_( int _a, int _b )
    {
        this.a = _a;
        this.b = _b * 2;
    }

    Add()
    {
        return this.a + this.b;
    }
    PrintAddRes()
    {
        # 如果a=10 b=100的话 输入 a[10]+b[200]=210
        Debug.Write( "a[$this.a ]+b[$this.b ]=" + Add().ToString() );
    }
}

Project
{
    static _main_()
    {    
        DC = DemoClass(10, 100);
        DC.PrintAddRes();
    }
    static _test_()
    {
    }
}
```

### 语法说明

更完整的章节列表见 **[`md/INDEX.md`](md/INDEX.md)** 第三节；下列为常用直达链接。

#### 基本使用

1. [命名空间](md/syntax/namespace.md)  
2. [基本语法](md/syntax/base.md)  
3. [数字](md/syntax/number.md)  
4. [字符串](md/syntax/string.md)  
5. [变量](md/syntax/variable.md)  
6. [表达式](md/syntax/express.md)  
7. [运算符](md/syntax/operator.md)  
8. [if 判断](md/syntax/if.md)  
9. [switch](md/syntax/switch.md)  
10. [循环](md/syntax/forwhiledowhile.md)  
11. [方法 / 函数](md/syntax/function.md)  
12. [枚举](md/syntax/enum.md)  
13. [数据类型](md/syntax/data.md)  
14. [类](md/syntax/class.md)  
15. [对象](md/syntax/object.md)  
16. [数组](md/syntax/array.md)  

#### 进阶

1. [继承](md/syntax/extend.md)  
2. [接口](md/syntax/interface.md)  
3. [标签与 goto](md/syntax/labelgoto.md)  
4. [宏](md/syntax/marco.md)  
5. [模块与工程中的类组织](md/project/project-module.md)  
6. [类型转换](md/syntax/cast.md)  
7. [List](md/syntax/contraint/list.md)  
8. [Set](md/syntax/contraint/set.md)  
9. [Map](md/syntax/contraint/map.md)  
10. [Tuple](md/syntax/contraint/tuple.md)  
11. [Queue](md/syntax/contraint/queue.md)  
12. [Stack](md/syntax/contraint/stack.md)  
13. [try / catch](md/syntax/trycatch.md)  
14. [模板](md/syntax/template.md)  
15. [global 与工程](md/syntax/global.md)  

#### 标准库与系统（现有文档入口）

1. [std / 环境](md/syntax/std/env.md)  
2. [系统方法](md/syntax/system_method.md)  
3. [虚拟机相关](md/syntax/virtualmachine.md)  
4. [导出与 IR 说明](md/syntax/exporter.md)  

### 支持平台

当前主线实现基于 **.NET**（`Front` 编译前端与 `VM`）；具体工程与运行路径以 [`md/project/project-config-jsonc-guide.md`](md/project/project-config-jsonc-guide.md) 与 [`md/ai/EXPORT_PATHS.md`](md/ai/EXPORT_PATHS.md) 为准。

### 安装与使用

1. 克隆本仓库并准备好 .NET 开发环境（与解决方案目标版本一致）。  
2. 使用 **`sl`** 命令行创建工程、注册源文件并编译；命令与 `jsonc` 字段说明见 [`md/project/project-config-jsonc-guide.md`](md/project/project-config-jsonc-guide.md)。  
3. 排查编译与运行问题时，按 [`md/ai/DEBUG_WORKFLOW.md`](md/ai/DEBUG_WORKFLOW.md) 建议查看 `Logs/` 与 `DebugCode/`。

### 联系作者

mail: kamaba233@gmail.com
