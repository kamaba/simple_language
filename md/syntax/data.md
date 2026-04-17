# 数据（data / 记录/结构体）

`data` 用于声明轻量的数据结构，通常用于静态配置、常量数据或值语义对象。`data` 的语法与 `class` 相似，但更偏向于声明静态/可序列化的数据。

语法示例：

```s
data Book {
    string name = "我的天空";
    string desc = "描述";
    RentInfo rent = { manager = "X", time = "2012-10-12" };
    string[] author = ["Lif", "Paper"];
}
```

说明：
- `data` 常用于配置或序列化数据；可用 `const data` 将其标记为只读常量。
- `data` 可以直接作为值使用：`print("book name: $Book.name");`。
- 可通过 `toStream` / `toJson` / `toClass<T>` 等辅助方法在 runtime 中转换。

示例使用：

```s
Project {
    _main_()
    {
        Console.println(Book.name);
        BookRef = Book; # 复制/引用 data
        Book.name = "新书名"; // 若非 const 可修改
        BookClass bc = Book.toClass<BookClass>();
        Book bk = bc.ToData<Book>();
    }
}
```

约束：
- `data` 可引用其它 `data`，但不能引用 `class` 或 `enum`（避免复杂依赖）。
- 不允许循环引用的 `data` 定义。

## `global.data`（jsonc）中的 data/array 支持

`Project.jsonc` 下 `global.data` 现在支持：
- 基础值：`int32` / `float64` / `string` / `bool` / `null`
- 对象：会转换为 `MetaData` 树，可通过 `global.xxx.yyy` 访问
- 数组：支持基础值数组与嵌套数组，可通过下标访问

示例：

```jsonc
"global": {
  "data": {
    "var1": 12,
    "arr": [1, 2, 3],
    "arr2": [[1, 2], [3, 4]],
    "cfg": {
      "name": "demo",
      "flags": [true, false]
    }
  }
}
```

访问示例：
- `global.var1`
- `global.arr[0]`
- `global.arr2[1][0]`
- `global.cfg.name`
- `global.cfg.flags[1]`

## `bind`（绑定 data 到 class / interface）

`bind` 用于在声明 `class` 或 `interface` 时，把一个或多个 `data` 结构“绑定”到该类型上，使其内部可以把绑定的数据当作该类型的一部分来访问（通过 `this.binddata`）。

### 语法（示例）

```ruby
data A { string name = "a" }
data B { string name = "b" }

class C bind A, B
{
    // C 内部可以访问 this.binddata.A.name / this.binddata.B.name
}

interface I bind A, B
{
    // interface 也可以声明同样的绑定数据访问/接口
}
```

### 数据访问结构

- 绑定数据统一入口为 `this.binddata`
- 访问方式为：`this.binddata.<DataName>.<fieldName>`
- 当你在 `class` / `interface` 中访问某个字段名（如 `name`）时，解析规则以“当前类型自身成员定义”为准：
  - 若当前类型已定义了同名的 `get/set` 属性（或字段），则使用当前类型的实现
  - 若未定义同名成员，则可通过 `this.binddata.<DataName>.<fieldName>` 显式访问具体来源

### bind 数据重复字段名问题（必须重写）

当绑定了多个 `data`，且它们内部存在**同名字段**时（例如都叫 `name`），会出现“同名歧义”。此时需要在绑定类/接口里**重写（实现）冲突字段的 `get/set`**，明确该字段应该映射到哪一个绑定 data。

例如（你的示例）：

```ruby
data a { name = "a" }
data b { name = "b" }

class c bind a,b
{
    // 如果不重写，这里的 name 会有歧义
    // 因此需要明确 name 来自 a 或 b
}
```

为了消除重复字段歧义，你可以在 `c` 内尽量重写该字段（用 `this.binddata.<data>.<field>` 做转发）：

```ruby
class c bind a,b
{
    string get name
    { 
        ret this.binddata.a.name
    }

    void set name( string n )
    {
        this.binddata.a.name = n
    }
}
```

#### 重写策略建议

- 对于每个冲突字段，必须在绑定目标类型上显式提供 `get/set`（或等价的属性定义）
- 映射到的来源要在 `this.binddata.<sourceData>.<field>` 里明确写出
- 如果你希望用户访问的是 `b` 的字段，就把映射改成 `this.binddata.b.name`

