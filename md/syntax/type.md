# 类型（Type）

S 语言提供强类型系统，包括基本类型与用户定义类型。值类型与引用类型在某些上下文中可以互相转换（通过 cast/as/is）。

基础类型：
- 整数：`Int8/Int16/Int32/Int64`（或别名 `int`）
- 无符号整数：`UInt8/UInt16/UInt32/UInt64`（或别名 `uint`）
- 浮点：`Float32`、`Float64`
- 布尔：`Boolean`（`true` / `false`）
- 字符串：`String`

用户类型：
- `class`：引用类型，支持继承与方法重写
- `data`：记录/结构体样式的轻量数据类型
- `enum`：枚举类型
- `interface`：接口

泛型（模板）示例：

```s
List<Int32> list = List<Int32>(10);
fun<T> id(T x) { ret x; }
```

类型检查与转换：
- 使用 `is` 检查类型：`if (x is String) { ... }`。
- 使用 `as` 或 `Cast<T>()` 做运行时转换：`var s = x as String;`。