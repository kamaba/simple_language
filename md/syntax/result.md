# `result` 返回机制（Result / Result<T>）

`result` 是一种用于函数返回的“约定式内部结果类型”语法。只要函数签名中使用了 `result`，该函数的返回值会自动包装成 `Result` 对象，并在返回 `ret` 时把返回表达式写入 `Result.value`。

---

## 1. 基础类型：`result`

当函数返回类型为 `result` 时，等价于返回一个内部类型 `Result`：

```txt
class Result
{
    int error = 0
    string errmsg = ""
    object value = null
}
```

---

## 2. 泛型类型：`result<T>`

当函数返回类型为 `result<T>` 时，等价于返回一个内部泛型结果类型 `Result<T>`：

```txt
class Result<T>
{
    int error = 0
    string errmsg = ""
    T value = null
}
```

其中 `error` / `errmsg` 与 `Result` 保持一致；`value` 的类型为 `T`。

---

## 3. 函数内的写法

### 3.1 直接设置错误信息

如果函数使用了 `result` / `result<T>`，则在函数体内可以直接写：

```ruby
error = 100
errmsg = "ok"
```

等价于设置返回包装对象里的 `error` 和 `errmsg` 字段。

### 3.2 返回时用 `ret`

当函数使用了 `result` / `result<T>`，当执行：

```txt
ret <expr>
```

表示将 `<expr>` 作为 `Result.value` 写入，并返回整个 `Result` 对象。

---

## 4. 调用方如何使用

调用函数得到的结果是一个 `Result`（或 `Result<T>`），调用方可以访问：

```ruby
a.error
a.errmsg
a.value
```

如果是 `result<T>`，则 `a.value` 的类型为 `T`，可以直接对其调用 `T` 的方法/字段。

---

## 5. 示例

### 5.1 `result`（value 为 object）

```ruby
result fun()
{
     error = 1
     errmsg = "ok"
     ret "aaa"
}

test()
{
     a = fun()
     if( a.error == 1 )
     {
         console.print(a.errmsg)
     }
}
```

上例中，`ret "aaa"` 会把 `"aaa"` 写入 `a.value`。

### 5.2 `result<ResponseClass>`（value 为 ResponseClass）

```ruby
result<ResponseClass> funRespon()
{
    ResponseClass c = new()

    // 可选：设置 error / errmsg
    // error = 0
    // errmsg = "ok"

    ret c
}
```

---

## 6. 默认值

当函数没有显式修改 `error` / `errmsg` / `value` 时：
- `error` 默认是 `0`
- `errmsg` 默认是空字符串 `""`
- `value` 默认是 `null`（`result`）或 `default(T)`（`result<T>`）

