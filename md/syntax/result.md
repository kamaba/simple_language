# `result` 返回机制（Result / Result<T>）

当函数返回类型声明为 `Result` / `Result<T>` 时，编译器会自动在函数体内注入一个名为 `result` 的局部变量，函数体内可直接写 `result.code` / `result.message` / `result.value`；`ret` 语句的语义也会被改写（见 §3.2）。

---

## 1. 基础类型：`Result`

等价于 Core 库中的 `Result` 类（`source/Front/Lib/Core/Result.sl`）：

```txt
class Result
{
    Object value = null
    int code = 0
    String message = ""
}
```

---

## 2. 泛型类型：`Result<T>`

等价于 `Result<T>`：

```txt
class Result<T>
{
    T value = null
    int code = 0
    String message = ""
}
```

其中 `code` / `message` 与 `Result` 保持一致；`value` 的类型为 `T`。

`Result<T>` 可以协变赋值给 `Result`（反向不合法）。

---

## 3. 函数内的写法

### 3.1 自动注入的 result 变量

返回类型为 `Result` / `Result<T>` 的函数，函数体内直接使用编译器注入的 `result` 变量：

```ruby
result.code = 100
result.message = "ok"
```

### 3.2 `ret` 的改写规则

| 写法 | 语义 |
|------|------|
| `ret <非 Result 表达式>` | 改写为 `result.value = <expr>; ret result`（值提取进 value） |
| `ret result` | 原样返回注入的 result 对象 |
| `ret <其他 Result 对象>` | 走正常返回路径，不做 value 提取 |
| 函数自然结束（没有显式 ret） | epilogue 兜底返回 result |

---

## 4. 调用方如何使用

调用函数得到的结果是一个 `Result`（或 `Result<T>`），调用方可以访问：

```ruby
a.code
a.message
a.value
```

如果是 `Result<T>`，则 `a.value` 的类型为 `T`，可以直接对其调用 `T` 的方法/字段。

---

## 5. 示例

### 5.1 自动注入 + 值返回改写

```ruby
# 直接使用注入的 result 变量
Result useAutoResult()
{
    result.code = 100
    result.message = "ok"
    ret result
}

# ret 100 等价 result.value = 100; ret result
Result retValue()
{
    ret 100
}

# 泛型版同理
Result<int> retValueT()
{
    ret 200
}
```

### 5.2 部分路径显式返回，其余路径 epilogue 兜底

```ruby
Result mixReturn( bool flag )
{
    if flag
    {
        result.code = 1
        result.message = "flag-true"
        ret result
    }
    result.code = 2
    result.message = "flag-false"
    # 自然结束，epilogue 兜底返回 result
}
```

### 5.3 返回其他 Result 对象（不做值提取）

```ruby
Result retOther()
{
    Result r = new()
    r.code = 500
    r.message = "other"
    ret r
}
```

完整用例见 `test/BaseTest/ResultTest.sl`。

---

## 6. 默认值

当函数没有显式修改 `code` / `message` / `value` 时：
- `code` 默认是 `0`（0 = 成功）
- `message` 默认是空字符串 `""`
- `value` 默认是 `null`（`Result`）或 `default(T)`（`Result<T>`）

---

## 7. 命名约束

`error` / `errmsg` 是 Result 机制的保留名（历史字段语义），与 8 个小写容器构造糖名（`map` / `list` / `stack` / `hashset` / `queue` / `tuple` / `array` / `range`）一样，**不允许作为类成员声明名**（MetaCore 层编译报错，LID 11042）。见 [keywords.md](./keywords.md) §3.8。
