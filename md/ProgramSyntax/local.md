# local（文件级初始化 + 文件私有成员）

## 概述

`local { ... }` 用于在**单个源文件**内声明“文件级初始化逻辑”和“文件私有成员（变量/函数）”。

核心特性：

- `local{}` 内的成员通过 `local.xxx` 在**当前文件**内访问。
- 多个文件都可以写 `local{}`，且可以**重复定义同名成员**，互不冲突。
- `local{}` 的执行顺序按照工程的**编译文件列表顺序**执行（先编译列表前面的文件，再执行后面的文件）。

常见用途：

- 本文件内的资源初始化（配置、缓存、db 连接等）
- 本文件内可复用的工具函数（仅该文件可见）

---

## 位置约束

`local{}` 只能写在：

- **所有 `import` 之后**
- **任何 `namespace` / `class` / `data` / `enum` 之前**

正确：

```sl
import Core.Debug;

local { a = 1 }

class A { }
```

错误：

```sl
class A { }
local { a = 1 }  # 不允许
```

---

## 语法与规则

### 基本语法

```sl
import Core.Debug;

local
{
    a = 1
    int Add(x)
    {
        return x + local.a
    }
}
```

约束：

- `local` 后必须跟 `{}`。
- 同一文件只允许出现一个 `local{}`。

### 语句与函数混排规则（重要）

当前实现约束为：

1. 在 `local{}` 内，**在出现第一个函数定义之前**，允许写“初始化语句”。
2. 一旦出现函数定义，则**后续只能继续定义函数**（不允许再写初始化语句）。
3. `local{}` 中定义的函数**不允许**带 `static`。

---

## 访问规则（文件私有）

### local 成员只在当前文件可见

- 在 `LocalTest1.sl` 里：`local.xxx` 只能访问 `LocalTest1.sl` 自己的 `local{}`。
- 在 `LocalTest2.sl` 里：`local.xxx` 只能访问 `LocalTest2.sl` 自己的 `local{}`。

不同文件即使成员同名，也互不影响。

---

## 执行顺序（按编译文件顺序）

当工程编译列表为：

1. `LocalTest1.sl`
2. `LocalTest2.sl`

则执行顺序为：

1. `LocalTest1.sl` 的 `local{}` 初始化（`__local_init__`）
2. `LocalTest2.sl` 的 `local{}` 初始化（`__local_init__`）

---

## 示例：LocalTest1 / LocalTest2（测试顺序与隔离）

> 对应测试文件：`test/BaseTest/LocalTest1.sl` 与 `test/BaseTest/LocalTest2.sl`

### `LocalTest1.sl`

```sl
import Core.Debug;

local
{
    a = 1
    order = "L1"

    int Add(x)
    {
        return x + local.a
    }

    PrintLocal()
    {
        Debug.Write("LocalTest1 local.a=" + local.a)
        Debug.Write("LocalTest1 local.order=" + local.order)
    }
}

class LocalTest1
{
    static Test()
    {
        local.a = local.a + 10
        v = local.Add(5)
        Debug.Write("LocalTest1 v=" + v)
        local.PrintLocal()
    }
}
```

### `LocalTest2.sl`

```sl
import Core.Debug;

local
{
    # 与 LocalTest1 重复定义同名 a，不冲突
    a = 100
    order = "L2"

    func Add(x)
    {
        return x + local.a
    }

    func PrintLocal()
    {
        Debug.Write("LocalTest2 local.a=" + local.a)
        Debug.Write("LocalTest2 local.order=" + local.order)
    }
}

class LocalTest2
{
    static Test()
    {
        local.a = local.a + 1
        v = local.Add(5)
        Debug.Write("LocalTest2 v=" + v)
        local.PrintLocal()
    }
}
```

观察点：

- 如果编译文件顺序是 `LocalTest1.sl` → `LocalTest2.sl`，则初始化顺序也是这个顺序。
- `LocalTest1.sl` 的 `local.a` 和 `LocalTest2.sl` 的 `local.a` 相互独立。

---

## 常见错误

- 位置错误：`Error local{} 只能写在 import 后、namespace/class/data/enum 前`
- 重复定义：`Error local{} 在同一文件中只允许定义一次`
- 函数后出现语句：`Error local{} 中出现函数定义后，后边只允许继续定义函数`
- local 函数使用 static：`Error local{} 中定义的函数不允许使用 static`
