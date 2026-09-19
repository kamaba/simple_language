# local（文件级初始化 + 文件私有成员）

## 概述

`local { ... }` 用于在**单个源文件**内声明"文件级初始化逻辑"和"文件私有成员（变量/函数）"。

`local{}` 会生成自己的类和实例代码：编译器为每个定义了 `local{}` 的文件生成一个 `<FileName>_Local` 类，并在该类上创建静态成员 `instance`（类型为该类自身），在静态初始化阶段创建实例。`local{}` 中的变量提升为该类的实例成员变量，函数成为实例成员函数，初始化语句放入 `__local_init__()` 实例函数。

核心特性：

- `local{}` 内的成员通过 `local.xxx` 在**当前文件**内访问。
- 多个文件都可以写 `local{}`，且可以**重复定义同名成员**，互不冲突。
- `local{}` 的执行顺序按照工程的 `.jsonc` 配置文件中 `compileFiles.files.priority` 的顺序执行（priority 数值小的先执行）。
- `local{}` 的执行时机：在**初始化完类静态成员变量和 const 变量之后**，再执行每个文件里边的 local 逻辑（即 `__local_init__()` 调用注入到 `_main_()` 前部，按 priority 顺序排列）。

常见用途：

- 本文件内的资源初始化（配置、缓存、db 连接等）
- 本文件内可复用的工具函数（仅该文件可见）

---

## 位置约束

`local{}` 只能放在**任何类（class/data/interface/enum）前边**。

在 `local{}` 前边，**只允许**放以下内容：

- `import` 语句
- `typealias` 语句
- 注释

**其它任何内容都不允许**放在 `local{}` 前边。

正确：

```sl
import Std;

local { a = 1 }

class A { }
```

错误：

```sl
class A { }
local { a = 1 }  # 不允许：local{} 必须在类之前
```

```sl
data D { x = 0 }
local { a = 1 }  # 不允许：data 定义在 local{} 前边
```

---

## 语法与规则

### 基本语法

```sl
local
{
    a = 1
    int Add(x)
    {
        ret x + local.a
    }
}
```

约束：

- `local` 后必须跟 `{}`。
- 同一文件只允许出现一个 `local{}`。
- `local{}` 中定义的函数**不允许**带 `static`。

### init 内裸名字访问（隐式 this）

`__local_init__()` 内的裸名字（不带 `local.` 前缀）等价于隐式 `this.xxx`，即直接读写提升后的实例成员变量：

```sl
local
{
    a = Vector2(){ x = 1.0f, y = 2.0f }
    a.addVector(c)          # 等价 this.a.addVector(c)：按 a 的实际类型解析链式调用
    float len = a.length()  # 定义局部变量 len；语句末尾自动同步 this.len = len
}
```

- `a = expr`（无类型前缀）按成员赋值 `this.a = expr` 解析，成员类型由右值类型推导。
- `Type name = expr`（带类型前缀）按局部变量定义解析，编译器在 init 末尾注入 `this.name = name` 同步语句，成员类型取局部变量类型。
- 后续语句中对裸名字的链式调用（如 `a.addVector(c)`）按成员的**实际类型**（而非占位 Object 类型）解析。

### 语句与函数混排规则（重要）

1. 在 `local{}` 内，**前边允许放语句**。如果是定义变量，也可以直接定义（如 `a = 1` 或 `int len = a.length()`）。
2. **一旦出现函数体（函数定义），在函数体后边不允许再放任何语句**。也就是说，函数定义之后只能继续定义函数，不能再写初始化语句或变量定义。

正确：

```sl
local
{
    a = 1           # 语句：变量定义
    b = 2           # 语句：变量定义
    Add(x)          # 函数定义
    {
        ret x + local.a
    }
    Sub(x)          # 函数定义（允许在函数后继续定义函数）
    {
        ret x - local.a
    }
}
```

错误：

```sl
local
{
    a = 1
    Add(x)
    {
        ret x + local.a
    }
    b = 2           # 错误：函数体后边不允许再放语句
}
```

---

## 访问规则（文件私有）

### local 成员只在当前文件可见

- 在 `LocalTest1.sl` 里：`local.xxx` 只能访问 `LocalTest1.sl` 自己的 `local{}`。
- 在 `LocalTest2.sl` 里：`local.xxx` 只能访问 `LocalTest2.sl` 自己的 `local{}`。

不同文件即使成员同名，也互不影响。

`local.xxx` 的解析路径：`local` 关键字 -> 查找当前文件的 `<FileName>_Local` 类 -> 访问其静态成员 `instance` -> 通过实例访问成员变量或函数。

---

## 执行顺序（按 priority 顺序）

文件的执行顺序由 `.jsonc` 配置文件中 `compileFiles.files.priority` 的值决定。**priority 数值小的先执行**，相同 priority 的按配置文件中的出现顺序执行（稳定排序）。

当工程 `.jsonc` 配置为：

```jsonc
"compileFiles": {
    "files": [
        { "path": "LocalTest1.sl", "priority": 1 },
        { "path": "LocalTest2.sl", "priority": 2 }
    ]
}
```

则执行顺序为：

1. `LocalTest1.sl` 的 `local{}` 初始化（`LocalTest1_Local.instance.__local_init__()`）
2. `LocalTest2.sl` 的 `local{}` 初始化（`LocalTest2_Local.instance.__local_init__()`）

**执行时机**：上述 `__local_init__()` 调用被注入到 `_main_()` 函数前部，在所有类的静态成员变量和 const 变量初始化完成之后执行。这保证了 local 逻辑可以安全地引用已初始化的静态成员。

---

## 示例：LocalTest1 / LocalTest2（测试顺序与隔离）

> 对应测试文件：`test/BaseTest/LocalTest1.sl` 与 `test/BaseTest/LocalTest2.sl`

### `LocalTest1.sl`

```sl
local
{
    a = 1
    order = "L1"

    Add(x)
    {
        ret x + local.a
    }

    PrintLocal()
    {
        global.println("LocalTest1 local.a=" + local.a)
        global.println("LocalTest1 local.order=" + local.order)
    }
}

class LocalTest1
{
    static Test()
    {
        local.a = local.a + 10
        v = local.Add(5)
        global.println("LocalTest1 v=" + v)
        local.PrintLocal()
    }
}
```

### `LocalTest2.sl`

```sl
local
{
    # 与 LocalTest1 重复定义同名 a，不冲突
    a = 100
    order = "L2"

    int Add(x)
    {
        ret x + local.a
    }

    PrintLocal()
    {
        global.println("LocalTest2 local.a=" + local.a)
        global.println("LocalTest2 local.order=" + local.order)
    }
}

class LocalTest2
{
    static Test()
    {
        local.a = local.a + 1
        v = local.Add(5)
        global.println("LocalTest2 v=" + v)
        local.PrintLocal()
    }
}
```

观察点：

- 如果 `LocalTest1.sl` 的 priority=1，`LocalTest2.sl` 的 priority=2，则初始化顺序也是这个顺序。
- `LocalTest1.sl` 的 `local.a` 和 `LocalTest2.sl` 的 `local.a` 相互独立。

---

## 常见错误

- 位置错误：`Error local can only be used at first position` — `local` 必须在调用链首位
- 位置错误：`Error current file does not define local{}, cannot use local.xxx` — 当前文件没有定义 `local{}` 却使用了 `local.xxx`
- 函数后出现语句：`Error local{} 中出现函数定义后，后边只允许继续定义函数`
- local 函数使用 static：`Error local{} functions cannot use static keyword`
- 生成的类名冲突：`Error local{} generated class name conflict: <FileName>_Local`
