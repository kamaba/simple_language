# 类型（Type）

S 语言提供强类型系统，包括基本类型与用户定义类型。值类型与引用类型在某些上下文中可以互相转换（通过 cast/as/is）。

基础类型（括号内为等价别名，见 `CoreMetaClassManager.GetSelfMetaName`）：
- 整数：`Int8/Int16/Int32/Int64`（别名 `sbyte`/`short`/`int`/`long`）
- 无符号整数：`UInt8/UInt16/UInt32/UInt64`（别名 `byte`/`ushort`/`uint`/`ulong`；另有内置全局别名 `Byte`→`UInt8`、`SByte`→`Int8`）
- 浮点：`Float32`、`Float64`（别名 `float`、`double`）
- 布尔：`Boolean`（别名 `bool`，`true` / `false`）
- 字符串：`String`（别名 `string`）

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
- 使用 `is` / `isnot` 检查类型：`if (x is String) { ... }`。
- 使用 `as` 做运行时转换（失败得 `null`）：`var s = x as String;`。完整转换规则（`as`/`is`/`isnot`）见 cast 章节。