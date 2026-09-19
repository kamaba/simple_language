# S语言异常处理（throws / throw / try / catch / checked）

> 本文档描述异常处理模块，涵盖 `throws` 声明、`throw` 抛出、`label{}catch{}` 异常捕获块、`try`/`try?`/`try!` 表达式前缀、以及 `checked` 溢出检测。

---

## 目录

1. [概述](#1-概述)
2. [throws - 函数异常声明](#2-throws--函数异常声明)
3. [enum extends Error - 错误类型定义](#3-enum-extends-error--错误类型定义)
4. [throw - 抛出异常](#4-throw--抛出异常)
5. [label{}catch{} - 异常捕获块](#5-labelcatch--异常捕获块)
6. [try 表达式 - try / try? / try!](#6-try-表达式--try--try-try)
7. [checked / unchecked - 溢出检测](#7-checked--unchecked--溢出检测)
8. [errdefer - 延迟错误处理](#8-errdefer--延迟错误处理)
9. [执行流程与语义](#9-执行流程与语义)
10. [完整示例](#10-完整示例)

---

## 1. 概述

异常系统围绕以下核心理念：

| 机制 | 用途 |
|------|------|
| `throws` | 函数级声明，标记该函数可能抛出异常。只有 `throws` 函数才能使用 `throw` |
| `throw` | 抛出异常，只能在 `throws` 函数中使用 |
| `enum extends Error` | 定义错误类型，只有继承 `Error` 的 enum 才能被 `throw` |
| `label Name {} catch {}` | 异常捕获块，对代码块内的异常进行模式匹配捕获 |
| `try` | 表达式前缀，标记可能抛出异常的调用，异常由周围 `catch` 捕获 |
| `try?` | 表达式前缀，异常时返回 null，不崩溃 |
| `try!` | 表达式前缀，异常时程序崩溃（不可恢复） |
| `checked` | 溢出检测块，溢出时抛出可被 `catch` 捕获的异常 |

### 核心规则

1. **函数必须声明 `throws`** 才能使用 `throw`。
2. **`throw` 后只能跟 `enum extends Error` 的成员**。
3. **`label Name {} catch {}`** 定义异常捕获块，`catch` 支持按 enum 类型匹配。
4. **`try expr`** 在 `label{}catch{}` 块内使用，标记可能抛出异常的表达式。
5. **`try? expr`** 和 **`try! expr`** 可在任何地方使用，不依赖 `label{}catch{}`。

---

## 2. throws - 函数异常声明

### 语法

```sl
[修饰符] [返回类型] 函数名(参数列表) throws
{
    // 函数体：可以使用 throw 抛出异常
}
```

### 规则

- `throws` 关键字放在参数列表之后、函数体之前。
- **只有 `throws` 函数才能使用 `throw`**：未声明 `throws` 的函数中使用 `throw` 会被编译器报错。
- `throws` 函数可以被调用方通过 `try`/`try?`/`try!` 或 `label{}catch{}` 处理异常。

### 示例

```sl
Int32 divide(Int32 a, Int32 b) throws
{
    if (b == 0)
    {
        throw MathError.DivZero
    }
    ret a / b
}
```

---

## 3. enum extends Error - 错误类型定义

### 语法

```sl
enum 错误类型名 extends Error
{
    错误名 = { code = 整数值, message = "描述信息" }
}
```

### 规则

1. **只有 `extends Error` 的 enum 才能被 `throw`**。
2. **只有 `throws` 函数才能使用 `throw`**。
3. **`throw` 后只能跟 Error enum 成员**：如 `throw MathError.DivZero`。
4. **catch 可按 enum 类型匹配**：`catch MathError e` 匹配 `MathError` 类型的所有错误。

### 示例

```sl
enum MathError extends Error
{
    DivZero = { code = 1, message = "除以零" }
    Overflow = { code = 2, message = "溢出" }
}

enum FileError extends Error
{
    NotFound = { code = 101, message = "文件未找到" }
    ReadError = { code = 102, message = "读取失败" }
}
```

---

## 4. throw - 抛出异常

### 语法

```sl
throw ErrorEnum.成员名
```

### 规则

- `throw` 只能在 `throws` 函数中使用。
- `throw` 后只能跟 `enum extends Error` 的成员。
- `throw`（无参数）表示重新抛出当前捕获的异常，只能在 `catch` 块中使用。

### 示例

```sl
Int32 safeDivide(Int32 a, Int32 b) throws
{
    if (b == 0)
    {
        throw MathError.DivZero
    }
    ret a / b
}
```

---

## 5. label{}catch{} - 异常捕获块

### 语法

使用 `label LabelName {}` 定义异常捕获块，后跟 `catch` 进行模式匹配：

```sl
label LabelName
{
    // 异常捕获块
    // 在这里使用 try expr 标记可能抛出异常的调用
    try riskyFunc()
}
catch EnumType e
{
    // 匹配 EnumType 类型的错误，e 为绑定的错误变量
}
catch EnumType
{
    // 匹配 EnumType 类型的错误，不绑定变量
}
catch e
{
    // 兜底：捕获所有未匹配的错误，绑定到 e
}
catch
{
    // 兜底：捕获所有未匹配错误，不绑定变量
}
finally
{
    // 无论是否异常都会执行
}
```

### 模式匹配规则

1. catch 按从上到下顺序匹配，命中第一个匹配项后执行对应块，不再继续匹配。
2. `catch MathError e` 匹配 `MathError` 类型的所有错误，`e` 为绑定的错误变量。
3. `catch MathError` 匹配 `MathError` 类型的所有错误，不绑定变量。
4. `catch e`（仅变量名）为兜底块，捕获所有未被上方 catch 匹配的错误，绑定到 `e`。
5. `catch`（无参数）为兜底块，捕获所有未匹配的错误。
6. `finally` 块无论是否发生异常都会执行。
7. 如果没有任何 catch 匹配成功，异常继续向外层传播。

### 关键：try 标记

在 `label{}catch{}` 块内，**只有使用 `try` 前缀标记的调用**，其异常才会被 `catch` 捕获。未使用 `try` 的调用，异常会直接传播。

```sl
label myBlock
{
    try riskyFunc()          # 异常被 catch 捕获
    riskyFunc()              # 异常直接传播，不被 catch 捕获
}
catch MathError e
{
    global.println("捕获到: " + e.message)
}
```

### 作用域共享

`label{}catch{}` 块内的变量在 `catch` 和 `finally` 中可以直接访问和修改。即 `catch`/`finally` 共享 `label {}` 块的作用域：

```sl
label myBlock
{
    string log = "try"       # 在 label {} 块内声明
    Int32 count = 0
    try riskyFunc()
}
catch MathError e
{
    # 可以直接访问和修改 label {} 块内的变量
    log = log + "-catch"
    count = count + 1
}
finally
{
    # finally 也可以访问
    log = log + "-finally"
}
```

> 注意：`catch` 绑定的异常变量（如 `catch MathError e` 中的 `e`）只在 `catch` 块内有效。

### 嵌套

```sl
label outer
{
    label inner
    {
        try riskyFunc()
    }
    catch MathError e
    {
        # 内层 catch 先匹配
    }
    # 内层未匹配的异常继续到外层
}
catch
{
    # 外层兜底
}
```

### re-throw

在 catch 块中可以使用 `throw`（无参数）重新抛出当前异常：

```sl
label myBlock
{
    try riskyFunc()
}
catch MathError e
{
    # 处理后重新抛出，交给外层
    throw
}
```

---

## 6. try 表达式 - try / try? / try!

`try` 作为表达式前缀，作用于后续表达式。三种模式：

### 6.1 try - 标记捕获

```sl
# 在 label{}catch{} 块内使用
# 异常被周围 catch 捕获
label myBlock
{
    Int32 result = try divide(10, 0)
    global.println("结果: " + result.toString())
}
catch MathError e
{
    global.println("除法错误: " + e.message)
}
```

**语义**：
- `try` 标记表达式为"可被捕获"。
- 如果表达式抛出异常，异常由周围 `label{}catch{}` 的 `catch` 块处理。
- `try` 只能在 `label{}catch{}` 块内使用。

### 6.2 try? - 可空返回

```sl
# 异常时返回 null，不崩溃
Int32? result = try? divide(10, 0)
# result 为 null，不崩溃

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
- 可在任何地方使用，不依赖 `label{}catch{}`。

### 6.3 try! - 强制解包

```sl
# 异常时程序直接崩溃
Int32 result = try! divide(10, 0)
# divide 抛出异常 -> try! 检测到 -> 程序崩溃
```

**语义**：
- `try!` 表示"确信不会出错"，如果异常发生则程序直接崩溃。
- 可在任何地方使用，不依赖 `label{}catch{}`。

### 6.4 链式调用

`try` / `try?` / `try!` 可以作用于链式调用：

```sl
# try? 作用于整个链式调用
string result = try? a.fun1().bfun2().cfun3()

# try 作用于链式调用
label myBlock
{
    Int32 val = try a.fun2().bfun3().cfun1()
}
catch MathError e
{
    global.println("错误: " + e.message)
}
```

### 三种模式对比

| 模式 | 异常时行为 | 返回类型 | 依赖 label{}catch{} | 适用场景 |
|------|-----------|----------|---------------------|----------|
| `try` | 异常由 catch 捕获 | 原始类型 | 是 | 在 catch 块内精确处理 |
| `try?` | 返回 null，不崩溃 | `T?`（可空） | 否 | 错误是预期内的，可降级 |
| `try!` | 程序直接崩溃 | 原始类型 | 否 | 确信不会出错，出错即 bug |

---

## 7. checked / unchecked - 溢出检测

### 概念

- **unchecked（默认）**：表达式计算不检测溢出，溢出时按底层类型回绕。
- **checked**：在 `checked` 块内，算术运算溢出时抛出可被 `catch` 捕获的异常。

### 语法

```sl
checked
{
    // 受溢出检测保护的计算
    // 也可以在这里边使用 try
}
catch
{
    // 溢出时的处理
}
```

> `checked` 块与 `label{}catch{}` 类似，`checked` 后跟 `catch` 进行异常捕获。`checked` 块内也可以使用 `try` 标记表达式。

### 示例

```sl
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

### checked 内使用 try

```sl
checked
{
    Int32 big = try riskyCompute()
    big = big + 1111111111111111
}
catch MathError e
{
    global.println("计算错误: " + e.message)
}
catch
{
    global.println("溢出")
}
```

### 作用域规则

- `checked` / `unchecked` 块仅影响块内的算术运算。
- 嵌套时内层覆盖外层。

---

## 8. errdefer - 延迟错误处理

### 语法

```sl
[修饰符] [返回类型] 函数名(参数列表) throws
{
    errdefer
    {
        // 异常发生时执行的延迟处理块
        ret 回退值    // 可选：设置回退返回值
    }

    // 函数体（可能抛出异常的代码）
}
```

### 语义

- `errdefer` 块在函数内**发生异常时**自动执行。
- `errdefer` 中的 `ret` 设置回退返回值，异常不再传播（错误被拦截）。
- 如果 `errdefer` 中没有 `ret`，仅做清理，异常继续传播。

### 示例

```sl
Int32 riskyCompute(Int32 a, Int32 b) throws
{
    errdefer
    {
        global.println("riskyCompute 发生异常")
        ret 100    # 拦截异常，返回 100
    }

    Int32 o = null
    o.toString()    # 抛出异常
    ret a + b
}
```

---

## 9. 执行流程与语义

### 9.1 异常传播路径

```
throws 函数内异常发生
       │
       ▼
  errdefer{} 执行（如果存在）
  ├── 有 ret -> 设置回退返回值，错误被拦截
  └── 无 ret -> 仅清理，错误继续传播
       │
       ▼
  调用方捕获
  ├── try  -> 异常由周围 catch 捕获
  ├── try? -> 返回 null
  ├── try! -> 崩溃
  ├── 未使用 try -> 异常继续传播
  └── 未捕获 -> 继续向外层调用栈传播
```

### 9.2 try 与 label{}catch{} 的关系

```
label myBlock
{
    try funcA()       # funcA 抛出异常 -> 被 catch 捕获
    funcB()           # funcB 抛出异常 -> 不被捕获，直接传播
    try? funcC()      # funcC 抛出异常 -> 返回 null
    try! funcD()      # funcD 抛出异常 -> 崩溃
}
catch MathError e
{
    # 捕获 try funcA() 的异常
}
```

### 9.3 各机制对比

| 机制 | 触发位置 | 作用 | 是否吞掉异常 |
|------|----------|------|-------------|
| `errdefer{}` | 函数内部 | 延迟清理 / 设置回退值 | 有 `ret` 则吞掉 |
| `try` | label{}catch{} 块内 | 标记表达式，异常由 catch 捕获 | 是（由 catch 处理） |
| `try?` | 表达式级 | 异常转 null | 是（转为 null） |
| `try!` | 表达式级 | 异常则崩溃 | 否（崩溃） |
| `checked{}` | 块级 | 溢出检测 -> 抛异常 | 否（需 catch 捕获） |

---

## 10. 完整示例

### 10.1 综合示例

```sl
enum MathError extends Error
{
    DivZero = { code = 1, message = "除以零" }
    Overflow = { code = 2, message = "溢出" }
}

# throws 函数
Int32 safeDivide(Int32 a, Int32 b) throws
{
    errdefer
    {
        global.println("safeDivide 异常清理")
    }

    if (b == 0)
    {
        throw MathError.DivZero
    }
    ret a / b
}

# 方式1：label{}catch{} + try
Int32 r1 = 0
label divBlock
{
    r1 = try safeDivide(10, 0)
}
catch MathError e
{
    global.println("除法错误: " + e.message)
    r1 = -1
}
catch
{
    r1 = -2
}

# 方式2：try? 可空返回
Int32? r2 = try? safeDivide(10, 0)
# r2 = null

# 方式3：try! 强制解包
Int32 r3 = try! safeDivide(10, 2)
# r3 = 5（正常）

# 方式4：try 链式调用
label chainBlock
{
    Int32 r4 = try safeDivide(100, 2).toString().toInt32()
}
catch MathError e
{
    global.println("链式调用错误: " + e.message)
}

# 方式5：checked 溢出检测
Int32 r5 = 0
checked
{
    r5 = 1111111111111111 + 1
}
catch
{
    r5 = 10000000000
}
```

### 10.2 嵌套与 re-throw

```sl
label outer
{
    label inner
    {
        try riskyFunc()
    }
    catch MathError e
    {
        global.println("内层捕获: " + e.message)
        throw        # 重新抛出，交给外层
    }
}
catch
{
    global.println("外层兜底捕获")
}
```

---

## 附：语法速查表

```
# 函数声明
[修饰符] 返回类型 函数名(参数) throws { ... }

# 错误类型定义
enum Name extends Error { ErrorA = { code = 1, message = "..." } }

# 抛出异常
throw ErrorEnum.MemberName        # 抛出指定错误
throw                             # re-throw（仅在 catch 块内）

# 异常捕获块
label Name
{
    try expr                       # 标记表达式，异常由 catch 捕获
    try? expr                      # 异常 -> null
    try! expr                      # 异常 -> 崩溃
}
catch EnumType e { ... }          # 匹配 EnumType，绑定到 e
catch EnumType { ... }             # 匹配 EnumType，不绑定
catch e { ... }                    # 兜底，绑定到 e
catch { ... }                      # 兜底，不绑定
finally { ... }                    # 总是执行

# 溢出检测
checked { ... } catch { ... }      # 溢出时抛出异常
unchecked { ... }                   # 显式不检测

# 延迟错误处理
errdefer { ret 回退值 }             # 有 ret：拦截异常
errdefer { 清理代码 }               # 无 ret：仅清理，异常传播

# try 表达式（在 label{}catch{} 块内使用）
try funcThatMayThrow()              # 异常 -> 需 catch 捕获
try? expr                           # 异常 -> null（任何地方可用）
try! expr                           # 异常 -> 崩溃（任何地方可用）
try a.b().c()                       # 链式调用
```
