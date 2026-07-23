# S语言异常处理（try/catch/throws）重新设计

> 本文档描述重新设计的异常处理模块，涵盖 `throws` 声明、`try`/`try?`/`try!` 表达式、`errdefer` 延迟错误处理、`enumError` 错误类型定义、块后缀 `catch` 模式匹配、`.catch{}` 后缀链式调用，以及 `checked`/`unchecked` 溢出检测。

---

## 目录

1. [概述](#1-概述)
2. [throws — 函数异常声明](#2-throws--函数异常声明)
3. [enumError — 错误类型定义](#3-enumerror--错误类型定义)
4. [try 表达式 — try / try? / try!](#4-try-表达式--try--try-try)
5. [errdefer — 延迟错误处理](#5-errdefer--延迟错误处理)
6. [块后缀 catch — {}catch{} 模式匹配](#6-块后缀-catch--catch-模式匹配)
7. [.catch{} — 后缀链式捕获](#7-catch--后缀链式捕获)
8. [checked / unchecked — 溢出检测](#8-checked--unchecked--溢出检测)
9. [执行流程与语义](#9-执行流程与语义)
10. [完整示例](#10-完整示例)

---

## 1. 概述

重新设计的异常系统围绕以下核心理念：

| 机制 | 用途 |
|------|------|
| `throws` | 函数级声明，标记该函数可能抛出可被捕捉的异常 |
| `enumError` | 定义错误类型，语法与 `enum` 一致，但专用于错误场景 |
| `try` / `try?` / `try!` | 表达式级异常控制：普通捕获、可空返回、强制解包 |
| `errdefer` | 函数内延迟错误处理块，异常发生时优先执行 |
| `{}catch{}` | 块后缀 catch，对代码块进行模式匹配捕获 |
| `.catch{}` | 后缀链式调用，在函数调用后链式捕获异常 |
| `checked` / `unchecked` | 算术溢出检测，`checked` 块内溢出会抛出可捕获异常 |

### 设计原则

- **默认 unchecked**：表达式计算默认不检测溢出，与 C/C++ 行为一致。
- **错误即值**：`enumError` 将错误定义为可比较的枚举值，catch 中通过模式匹配进行精确捕获。
- **延迟优先**：`errdefer` 在函数内最先响应异常，做清理或设置回退值，之后异常继续传播。
- **链式友好**：`.catch{}` 允许在函数调用后直接链式处理异常，无需包裹 try 块。

---

## 2. throws — 函数异常声明

### 语法

```sl
[修饰符] [返回类型] 函数名(参数列表) throws
{
    // 函数体
}
```

### 语义

- `throws` 关键字放在参数列表之后、函数体之前，声明该函数**可能抛出可被捕捉的异常**。
- 未声明 `throws` 的函数：内部发生的异常仍会抛出，但调用方是否能通过 `try`/`catch`/`.catch{}` 捕获取决于具体实现策略（建议：所有异常均可被捕获，`throws` 仅作为文档/编译期检查标记）。
- `throws` 可与 `enumError` 类型配合，声明可能抛出的具体错误类型（可选扩展）：

```sl
# 基本形式：声明可抛出异常
Int32 divide(Int32 a, Int32 b) throws
{
    if (b == 0)
    {
        throw MathError.DivErrorOverflow
    }
    ret a / b
}

# 扩展形式（可选）：声明具体错误类型
Int32 divide(Int32 a, Int32 b) throws MathError
{
    if (b == 0)
    {
        throw MathError.DivErrorOverflow
    }
    ret a / b
}
```

---

## 3. enumError — 错误类型定义

### 语法

`enumError` 的使用方式与 `enum` 完全一致，但语义上专门用于定义错误类型。支持两种成员形式：**简单值**和**结构化对象**。

```sl
enumError 错误类型名 [extends 底层类型]
{
    # 简单值形式
    错误名 = 整数值

    # 结构化对象形式（含 id、error、msg 等字段）
    错误名 = { id = 整数值, error = 错误码, msg = "描述信息" }
}
```

### 示例

```sl
enumError MathError
{
    # 简单值
    MinusError = 1

    # 结构化对象：携带详细错误信息
    MinusError2 = { id = 2, error = 100, msg = "减法错误" }

    # 结构化对象：除法溢出
    DivErrorOverflow = { id = 111, error = 101, msg = "除法溢出" }
}
```

### 成员访问

- 简单值成员：直接比较整数值，如 `MathError.MinusError`
- 结构化对象成员：可通过点号访问字段，如 `MathError.MinusError2.msg`、`MathError.DivErrorOverflow.id`

### 与 enum 的区别

| 特性 | `enum` | `enumError` |
|------|--------|-------------|
| 用途 | 通用枚举（状态、配置等） | 专用于错误定义 |
| 成员形式 | 简单值 / data 对象 | 简单值 / 结构化对象 `{ id, error, msg }` |
| catch 匹配 | 不直接支持 | 原生支持在 `catch` 中进行模式匹配 |
| throw | 不能直接 throw | 可以直接 `throw MathError.MinusError` |

---

## 4. try 表达式 — try / try? / try!

`try` 作为表达式级关键字，直接作用于后续表达式，提供三种模式：

### 4.1 try — 基本捕获

```sl
# try 后跟表达式，异常会被捕获
# 需要配合 catch 或 errdefer 使用
try funcThatMayThrow()
```

### 4.2 try? — 可空返回

```sl
# try? 表示：如果发生异常，返回 null（结果类型可空）
Int32? result = try? divide(10, 0)
# result 为 null，不崩溃

# 可以配合空值检查使用
if (result != null)
{
    global.println("结果: " + result.toString())
}
else
{
    global.println("除法失败，返回空")
}
```

**语义**：
- `try?` 将表达式结果类型变为可空类型（`T?`）。
- 异常发生时，表达式求值为 `null`，不抛出、不崩溃。
- 调用方需处理 `null` 情况。

### 4.3 try! — 强制解包

```sl
# try! 表示：如果发生异常（结果为空），直接崩溃
Int32 result = try! divide(10, 0)
# divide 抛出异常 → try! 检测到 → 程序直接崩溃（panic）
```

**语义**：
- `try!` 表示"我确信不会出错"，如果异常发生则程序直接崩溃（不可恢复）。
- 适用于调用方有充分理由保证不会异常的场景。
- 等价于 `try?` + 强制解包 + 空值断言。

### 三种模式对比

| 模式 | 异常时行为 | 返回类型 | 适用场景 |
|------|-----------|----------|----------|
| `try` | 异常被抛出，需 catch 捕获 | 原始类型 | 需要精确错误处理的场景 |
| `try?` | 返回 `null`，不崩溃 | `T?`（可空） | 错误是预期内的，可降级处理 |
| `try!` | 程序直接崩溃 | 原始类型 | 确信不会出错，出错即 bug |

---

## 5. errdefer — 延迟错误处理

### 语法

```sl
[修饰符] [返回类型] 函数名(参数列表) [throws]
{
    // 函数体（可能抛出异常的代码）

    errdefer
    {
        // 异常发生时执行的延迟处理块
        // 可以做资源清理、日志记录、设置回退返回值
        ret 回退值
    }
}
```

### 语义

- `errdefer` 块在函数内**发生异常时**自动执行，类似 Swift 的 `defer` 但仅在错误路径触发。
- `errdefer` 块内可以使用 `ret` 设置回退返回值。
- **执行顺序**：异常发生 → `errdefer` 块执行 → 异常继续向调用方传播。
- `errdefer` 类似 `finally` 但仅对错误路径生效，且可以影响返回值。

### 示例

```sl
Int32 riskyCompute(Int32 a, Int32 b) throws
{
    errdefer
    {
        # 异常发生时，先走这里做清理
        global.println("riskyCompute 发生异常，执行 errdefer")
        ret 100    # 设置回退返回值
    }

    Int32 o = null
    o.toString()    # 此处抛出异常（空指针）
    # 不会执行到这里
    ret a + b
}
```

**执行流程**：
1. 调用 `riskyCompute(1, 2)`
2. 执行到 `o.toString()` → 抛出异常
3. `errdefer{}` 块被触发 → 打印日志 → `ret 100`
4. 异常继续向调用方传播（或根据 errdefer 的 ret 决定是否吞掉异常）

> **关于 errdefer 中 ret 的语义**：`errdefer` 中的 `ret` 设置函数的回退返回值。如果 `errdefer` 执行了 `ret`，则函数以该值返回，异常不再向调用方传播（即错误被 errdefer 拦截）。如果 `errdefer` 中没有 `ret`，则仅做清理，异常继续传播。

---

## 6. 块后缀 catch — {}catch{} 模式匹配

### 语法

代码块 `{ }` 后紧跟 `catch`，表示该块为 try 块。catch 支持通过 `enumError` 进行模式匹配：

```sl
{
    // try 代码块（可能抛出异常的代码）
}
catch 错误类型.具体错误
{
    // 匹配到特定错误时的处理
}
catch 错误类型.另一个错误
{
    // 匹配到另一个错误时的处理
}
catch
{
    // 兜底：捕获所有未匹配的错误
}
```

### 模式匹配规则

1. catch 按从上到下的顺序匹配，命中第一个匹配项后执行对应块，不再继续匹配。
2. `catch MathError.MinusError` 精确匹配 `MathError.MinusError` 错误。
3. `catch MathError.DivErrorOverflow` 精确匹配 `MathError.DivErrorOverflow` 错误。
4. `catch`（无参数）为兜底块，捕获所有未被上方 catch 匹配的错误。
5. 如果没有任何 catch 匹配成功，异常继续向外层传播。

### 示例

```sl
enumError MathError
{
    MinusError = 1
    MinusError2 = { id = 2, error = 100, msg = "减法错误" }
    DivErrorOverflow = { id = 111, error = 101, msg = "除法溢出" }
}

# 块后缀 catch 模式匹配
{
    Int32 result = divide(10, 0)    # 抛出 MathError.DivErrorOverflow
}
catch MathError.MinusError
{
    global.println("捕获到减法错误")
}
catch MathError.DivErrorOverflow
{
    global.println("捕获到除法溢出错误")
    # 可以访问结构化字段
}
catch
{
    global.println("捕获到未知错误")
}
```

### catch 绑定变量

catch 也可以绑定异常变量，以便在块内访问错误的详细信息：

```sl
{
    riskyOperation()
}
catch MathError.DivErrorOverflow e
{
    global.println("错误ID: " + e.id.toString())
    global.println("错误码: " + e.error.toString())
    global.println("错误信息: " + e.msg)
}
catch e
{
    global.println("未知错误: " + e.toString())
}
```

---

## 7. .catch{} — 后缀链式捕获

### 语法

在函数调用后直接使用 `.catch{}` 进行链式异常捕获：

```sl
返回值 = 函数名(参数).catch
{
    // 异常处理逻辑
    ret 回退值
}
```

### 语义

- `.catch{}` 是一种语法糖，将函数调用包裹在隐式 try 块中。
- **执行流程**（关键）：
  1. 执行函数调用 `fun1(1, 2, 3)`
  2. 函数内部发生异常
  3. **先执行函数内部的 `errdefer{}`**（如果存在）
  4. 异常传播到调用方
  5. **再执行 `.catch{}`** 块
  6. `.catch{}` 中的 `ret` 决定最终返回值

### 示例

```sl
Int32 fun1(Int32 a, Int32 b, Int32 c) throws
{
    errdefer
    {
        global.println("fun1 errdefer 执行")
        ret 100
    }

    Int32 o = null
    o.toString()    # 抛出异常
    ret a + b + c
}

# 链式捕获
Int32 result = fun1(1, 2, 3).catch
{
    global.println(".catch 执行")
    ret 10
}
# 输出:
#   fun1 errdefer 执行
#   .catch 执行
# result = 10
```

**执行顺序详解**：

```
fun1(1,2,3).catch{ ret 10 }
     │
     ▼
  1. 进入 fun1(1, 2, 3)
     │
     ▼
  2. 执行 fun1 函数体
     │  int o = null
     │  o.toString()  ──→ 抛出异常
     │
     ▼
  3. fun1 的 errdefer{} 被触发
     │  打印 "fun1 errdefer 执行"
     │  ret 100（设置回退值）
     │
     ▼
  4. 异常传播到调用方
     │
     ▼
  5. .catch{ ret 10 } 被触发
     │  打印 ".catch 执行"
     │  ret 10
     │
     ▼
  6. 最终 result = 10
```

### .catch{} 与 try?/try! 的组合

```sl
# .catch{} 提供回退值
Int32 r1 = fun1(1, 2, 3).catch { ret 10 }

# try? 返回可空
Int32? r2 = try? fun1(1, 2, 3)

# try! 强制解包，异常则崩溃
Int32 r3 = try! fun1(1, 2, 3)
```

### .catch{} 支持模式匹配

`.catch{}` 同样支持 `enumError` 模式匹配：

```sl
Int32 result = divide(10, 0)
    .catch MathError.DivErrorOverflow { ret -1 }
    .catch MathError.MinusError { ret -2 }
    .catch { ret 0 }
```

---

## 8. checked / unchecked — 溢出检测

### 概念

- **unchecked（默认）**：表达式计算不检测溢出，溢出时按底层类型回绕（wrap-around），与 C/C++ 行为一致。
- **checked**：在 `checked` 块内，算术运算（加、减、乘等）如果发生溢出，抛出可被 `catch` 捕获的异常。

### 语法

```sl
checked
{
    // 受溢出检测保护的计算
}
catch
{
    // 溢出时的处理
}
```

### 示例

```sl
# 默认 unchecked：溢出静默回绕
Int32 a = 100000000000000 + 11111111    # 不检测，结果回绕

# checked 块：溢出抛出异常
Int32 a = 0
checked
{
    a = 1111111111111111 + 1    # 溢出！抛出异常
}
catch
{
    # 捕获溢出异常，设置安全值
    a = 10000000000
}
```

### checked 与 enumError 配合

```sl
enumError OverflowError
{
    AddOverflow = { id = 1, error = 200, msg = "加法溢出" }
    MulOverflow = { id = 2, error = 201, msg = "乘法溢出" }
}

checked
{
    Int32 big = 1111111111111111 + 1
}
catch OverflowError.AddOverflow e
{
    global.println("加法溢出: " + e.msg)
}
catch
{
    global.println("其他溢出")
}
```

### 作用域规则

- `checked` / `unchecked` 块仅影响块内的算术运算。
- 嵌套时内层覆盖外层：

```sl
checked
{
    // 此处检测溢出
    unchecked
    {
        // 此处不检测溢出（内层覆盖）
    }
    // 此处恢复检测溢出
}
```

- 也可作为表达式前缀（扩展形式）：

```sl
# 单表达式 checked
Int32 a = checked(1111111111111111 + 1)
```

---

## 9. 执行流程与语义

### 9.1 异常传播路径

```
函数内异常发生
       │
       ▼
  errdefer{} 执行（如果存在）
  ├── 有 ret → 设置回退返回值，错误被拦截（不传播）
  └── 无 ret → 仅清理，错误继续传播
       │
       ▼
  调用方捕获
  ├── try?  → 返回 null
  ├── try!  → 崩溃
  ├── try + {}catch{} → 模式匹配捕获
  ├── .catch{} → 链式捕获
  └── 未捕获 → 继续向外层调用栈传播
```

### 9.2 各机制对比

| 机制 | 触发位置 | 作用 | 是否吞掉异常 |
|------|----------|------|-------------|
| `errdefer{}` | 函数内部 | 延迟清理 / 设置回退值 | 有 `ret` 则吞掉，无 `ret` 则传播 |
| `try?` | 表达式级 | 异常转 null | 是（转为 null） |
| `try!` | 表达式级 | 异常则崩溃 | 否（崩溃） |
| `{}catch{}` | 块级 | 模式匹配捕获 | 是（匹配后处理） |
| `.catch{}` | 调用级 | 链式捕获 | 是（处理后返回回退值） |
| `checked{}` | 块级 | 溢出检测 → 抛异常 | 否（抛出异常，需 catch 捕获） |

### 9.3 errdefer 与 .catch{} 的协作

当函数内 `errdefer` 没有 `ret`（仅做清理）时，异常会继续传播到 `.catch{}`：

```sl
Int32 fun1(Int32 a, Int32 b, Int32 c) throws
{
    errdefer
    {
        # 仅清理，不 ret → 异常继续传播
        global.println("清理资源...")
    }

    Int32 o = null
    o.toString()    # 抛出异常
    ret a + b + c
}

# .catch 会捕获到异常
Int32 result = fun1(1, 2, 3).catch { ret 10 }
# 输出: 清理资源...
# result = 10
```

当函数内 `errdefer` 有 `ret` 时，异常被拦截，`.catch{}` 不会触发：

```sl
Int32 fun1(Int32 a, Int32 b, Int32 c) throws
{
    errdefer
    {
        ret 100    # 拦截异常，函数返回 100
    }

    Int32 o = null
    o.toString()    # 抛出异常
    ret a + b + c
}

# errdefer 已拦截，函数正常返回 100，.catch 不触发
Int32 result = fun1(1, 2, 3).catch { ret 10 }
# result = 100（来自 errdefer 的 ret）
```

---

## 10. 完整示例

### 10.1 综合示例：除法运算

```sl
# 定义错误类型
enumError MathError
{
    MinusError = 1
    MinusError2 = { id = 2, error = 100, msg = "减法错误" }
    DivErrorOverflow = { id = 111, error = 101, msg = "除法溢出" }
}

# 声明 throws 的函数
Int32 safeDivide(Int32 a, Int32 b) throws
{
    errdefer
    {
        global.println("safeDivide 异常清理")
    }

    if (b == 0)
    {
        throw MathError.DivErrorOverflow
    }
    ret a / b
}

# 方式1：try? 可空返回
Int32? r1 = try? safeDivide(10, 0)
# r1 = null

# 方式2：try! 强制解包（此处会崩溃，仅作演示）
# Int32 r2 = try! safeDivide(10, 0)

# 方式3：块后缀 catch 模式匹配
Int32 r3 = 0
{
    r3 = safeDivide(10, 0)
}
catch MathError.DivErrorOverflow e
{
    global.println("除法错误: " + e.msg)
    r3 = -1
}
catch
{
    r3 = -2
}

# 方式4：.catch{} 链式捕获
Int32 r4 = safeDivide(10, 0).catch { ret -1 }
```

### 10.2 综合示例：溢出检测

```sl
enumError OverflowError
{
    IntOverflow = { id = 1, error = 300, msg = "整数溢出" }
}

Int32 safeAdd(Int32 a, Int32 b) throws
{
    Int32 result = 0
    checked
    {
        result = a + b
    }
    catch OverflowError.IntOverflow e
    {
        global.println("溢出: " + e.msg)
        throw e    # 重新抛出，交给调用方处理
    }
    ret result
}

# 调用方处理
Int32 r = safeAdd(1111111111111111, 1).catch
{
    ret 10000000000    # 溢出时使用安全值
}
```

### 10.3 综合示例：errdefer 与 .catch 协作

```sl
enumError FileError
{
    NotFound = { id = 1, error = 404, msg = "文件未找到" }
    ReadError = { id = 2, error = 500, msg = "读取失败" }
}

string readFile(string path) throws
{
    errdefer
    {
        # 无论是否异常，都先清理打开的文件句柄等资源
        global.println("清理文件资源")
    }

    if (path == "")
    {
        throw FileError.NotFound
    }

    # ... 读取文件逻辑 ...
    ret "file content"
}

# 链式捕获 + 模式匹配
string content = readFile("").catch FileError.NotFound
{
    ret "默认内容"
}
.catch
{
    ret "未知错误"
}
```

---



### Isolate 错误传播模型

```
┌─────────────────────────────────┐
│         主 Isolate               │
│  ┌───────────────────────────┐  │
│  │    子 Isolate (worker)     │  │
│  │                           │  │
│  │  throws 错误               │  │
│  │    └─> catch 捕获          │  │
│  │         └─> channel.send() │──┼──> 主 Isolate 收到消息
│  │                           │  │
│  │  panic                     │  │
│  │    └─> 子 Isolate 终止     │  │
│  │         └─> onPanic 回调   │──┼──> 主 Isolate 收到 panic 信息
│  └───────────────────────────┘  │
└─────────────────────────────────┘
```

***

## 12. VM 底层实现方案

### 选型：语法糖方案（推荐，不做完整栈展开）

### 底层原理

`do-catch` 只是编译器语法糖，错误底层依然是 `T | Error` 联合体（Zig 思路）。

### 优势

- **不需要复杂异常栈展开逻辑**，VM 大幅简化。
- GC、Isolate、协程更容易兼容。
- 性能稳定，无运行时栈回溯开销。

### 编译器 lowering 逻辑

```
源码:
    do {
        let res = try func()
    } catch IoError.NotFound {
        handleA()
    } catch all err {
        handleB()
    }

Lowering 后（伪 IR）:
    %result = call func()           // 返回 T | Error 联合体
    if %result.isError {
        %err = %result.error
        if %err == IoError.NotFound {
            goto catch_block_1
        }
        goto catch_block_2          // catch all
    }
    // 正常路径
    let res = %result.value
    goto end

catch_block_1:
    handleA()
    goto end

catch_block_2:
    let err = %err
    handleB()
    goto end

end:
    // finally 块（如果有）
```

### throws 函数的传播逻辑

```
源码:
    fn propagationFunc() throws -> Int {
        let x = try riskyFunc()   // 未被 catch 捕获
        return x + 1
    }

Lowering 后（伪 IR）:
    %result = call riskyFunc()
    if %result.isError {
        return %result.error      // 错误作为返回值向上传递
    }
    let x = %result.value
    return x + 1
```

### 对比原生 Dart

| 维度            | 本方案（语法糖） | Dart（运行时栈展开） |
| ------------- | -------- | ------------ |
| VM 复杂度        | 低        | 高            |
| 栈展开           | 不需要      | 需要           |
| GC/Isolate 兼容 | 容易       | 复杂           |
| 运行时开销         | 低（分支判断）  | 高（栈回溯）       |
| 性能            | 稳定       | 异常路径有开销      |

***

## 附：语法速查表

```
# 函数声明
[修饰符] 返回类型 函数名(参数) throws { ... }

# 错误类型定义
enumError Name { ErrorA = 1, ErrorB = { id = 2, error = 100, msg = "..." } }

# 延迟错误处理
errdefer { ret 回退值 }        # 有 ret：拦截异常
errdefer { 清理代码 }          # 无 ret：仅清理，异常传播

# try 表达式
try? expr                       # 异常 → null
try! expr                       # 异常 → 崩溃
try expr                        # 异常 → 需 catch 捕获

# 块后缀 catch
{ ... } catch Type.Error { ... } catch Type.Error2 { ... } catch { ... }

# 后缀链式 catch
func(args).catch { ret 回退值 }
func(args).catch Type.Error { ret 回退值 }.catch { ret 默认值 }

# 溢出检测
checked { ... } catch { ... }           # 块级
unchecked { ... }                        # 显式不检测
checked(expr)                            # 表达式级（扩展）
```