# local (文件级全局初始化块)

## 概述

`local{ ... }` 用于在 **单个源文件** 内声明“文件级全局代码”。它类似于“模块初始化/文件初始化”的概念：

- `local{}` 中可以直接写执行语句（赋值、调用等）
- 也可以在 `local{}` 中定义函数
- `local{}` 中定义的变量与函数，均通过 `local.xxx` 的方式在该文件内引用
- `local{}` 的执行顺序由编译器按 **编译文件列表顺序** 依次执行（先执行前面的文件的 `local{}`，再执行后面的文件）

该机制适用于：

- 文件级资源初始化（如数据库连接、缓存、配置读取）
- 给该文件提供可复用的“内部工具函数”

---

## 语法

```sl
import A.B;
import C.D;

local
{
    # 直接执行语句
    db = Mysql("127.0.0.1:3306", "user", "password")

    # 定义函数
    func PrintDbInfo()
    {
        Debug.Write(local.db)
    }
}
```

- `local` 后必须紧跟 `{}` 块
- `local{}` 只能出现一次（建议），如需多段初始化应写在同一个块内

---

## 位置约束（非常重要）

`local{}` 仅允许写在：

- **所有 `import` 之后**
- **任何 `namespace` / `class` / `data` / `enum` 定义之前**

正确示例：

```sl
import Core.Debug;

local { x = 1 }

class A { }
```

错误示例：

```sl
namespace N;
local { x = 1 }   # 不允许：local 不能在 namespace 之后
```

```sl
class A { }
local { x = 1 }   # 不允许：local 不能在类定义之后
```

---

## 访问规则

### 访问 local 变量

```sl
local
{
    db = Mysql("127.0.0.1:3306", "user", "password")
}

ClassDef
{
    Test()
    {
        local.db.Query("select 1")
    }
}
```

### 访问 local 函数

```sl
local
{
    func Init()
    {
        Debug.Write("init")
    }
}

ClassDef
{
    Main()
    {
        local.Init()
    }
}
```

---

## 执行顺序

当工程包含多个源文件时：

- 编译器按工程的编译文件顺序（配置中的文件列表顺序）
- 逐个执行每个文件的 `local{}` 初始化块

因此：

- 若文件 A 在文件 B 之前，A 的 `local{}` 会先执行
- `local{}` 为文件局部作用域，不建议跨文件依赖另一个文件的 `local` 变量

---

## 作用域与限制

- `local{}` 的成员（变量/函数）仅在 **当前文件内** 可见
- 不推荐在 `local{}` 内定义类/命名空间；如需定义结构，应使用普通 `class/data/enum`

---

## 错误提示（建议）

- 位置错误：
  - `Error local{} 只能写在 import 后、namespace/class/data/enum 前`
- 重复定义：
  - `Error local{} 在同一文件中只允许定义一次`
