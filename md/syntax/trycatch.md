# S语言异常捕捉（try/catch）设计

本节描述一种“块后缀 `catch`”的异常捕捉用法：**当代码块 `{ ... }` 结束后紧跟 `catch` 时，表示该块作为 try 块**；try 块内部抛出的异常会被对应的 catch 捕获处理。

> 说明：以下为基于你提出的需求做出的语法/语义设计稿，具体落地实现时可再按编译器现有 AST 约束微调。

---

## 1. 基础形式：catch 绑定异常变量

### 语法

```txt
<block-statement> {
    ...
}
catch Except <e>
{
    ...
}
```

- `<block-statement>` 可以是任意“带大括号”的语句（例如 `for`/`if` 后的块）。
- `Except` 表示异常基类（或异常总接口）。
- `<e>` 的类型在语义上按 `Except`（或其子类）推导；在 catch 块内可以直接使用 `<e>`。

### 示例

```ruby
for a in keys
{
}
catch Except e
{
    global.println(e.message)
}
```

---

## 2. 可选形式：catch{} 内进行“单个捕捉/类型匹配”

当 catch 中**没有写 `Except e`** 时，catch 块内部采用“类型匹配”方式对异常进行分支处理。

### 语法

```txt
<block-statement> {
    ...
}
catch
{
    if( <ExceptionType> <e> != null )
    {
        ...
    }
    // 可继续写多个 if 来进行更细粒度处理
}
```

### 匹配语义（推荐约定）

1. catch 在运行时能拿到一个“当前异常对象”（其静态类型至少是 `Except`）。
2. `if( NullPointerException e != null )` 这种写法表示：
   - 如果当前异常可以转成 `NullPointerException`，则条件为真；
   - 同时在条件块作用域内，把 `e` 绑定为 `NullPointerException` 类型的异常对象。
3. **如果 catch{} 内没有任何分支匹配成功**，则该异常应当继续向外抛出（由外层 catch 处理），以避免吞异常。

### 示例（对应你的需求）

```ruby
{
    // try 代码块
    if( a == 20 )
    {
        aa = a.m.toString()
    }
}
catch
{
    if( NullPointerException e != null )
    {
        global.println(e.message)
    }
}
```

> 你原始草稿里 `catch` 关键字缺失的部分，我按“catch 是块后缀”的规则补全到了示例中。

---

## 3. 规则与注意事项

### 3.1 catch 必须紧跟在块后

- 只有“紧贴块结尾”的 `catch` 才会绑定到该块，避免歧义。
- 不建议在块内部随意嵌套 `catch` 关键字（若需要可用外层块来包住）。

### 3.2 作用域

- `catch Except e { ... }`：`e` 的作用域仅在该 catch 块内。
- `catch { if( SomeException e != null ){ ... } }`：
  - `e` 的作用域限定在对应 `if` 的花括号块内（或 `if` 条件表达式到 if 块的范围内，按你实现的词法/语法规则）。

### 3.3 匹配优先级

- `catch Except e`：等价于“捕获所有异常（或至少捕获 Except 体系）”。
- `catch { if( T e != null ) { ... } ... }`：
  - 如果写多个 `if` 分支，按代码顺序执行，通常只会命中第一个为真分支（因为后面 `if` 仍可能检查；如需“命中即停止”，建议在命中后写一个 `return/next` 或语言层面提供默认跳出策略）。

---

## 4. 推荐补充：默认捕获的异常基类

为保证 `catch`（不带 `Except e`）在实现上有统一入口，建议内部将捕获异常提升为统一基类 `Except`，然后在 `if( ExceptionType e != null )` 中做动态类型判断。

---

## 5. 小结

- `catch Except e { ... }`：直接捕获并使用异常对象 `e`。
- `catch { ... }`：在 catch 块里通过 `if( ExceptionType e != null )` 做类型分支处理（未匹配则外抛）。
- `catch` 作为“块后缀”，绑定最近的 `{ ... }` try 块。

