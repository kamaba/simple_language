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

