# List（列表 / 可变数组）

本节按照 Dart 风格说明 S 语言中 List 的语义与常用 API。List 是按顺序保存元素的集合，支持泛型 `List<T>`。

基本特性：
- 可变（growable）与固定长度（fixed-length）两种 List。
- 支持按索引访问、插入、删除、遍历与常见高阶操作（map/filter/reduce）。

创建：
- 空的可变 List：`var a = List<int>();` 或 `var a = <int>[];`
- 使用字面量：`var a = [1, 2, 3];`
- 固定长度：`var a = List<int>(5); // 长度固定，默认为 0/默认值`

常用操作：
- `a.length`、`a.isEmpty`、`a.isNotEmpty`
- `a.add(x)`、`a.insert(index, x)`、`a.remove(x)`、`a.removeAt(index)`、`a.clear()`
- `a[index]`、`a[index] = value`
- `a.map((v) => ...)`、`a.where((v) => ...)`、`a.reduce(...)`、`a.forEach(...)`

示例：

```s
var lst = [1,2,3];
lst.add(4);
lst.insert(0, 0);
for v in lst { Debug.Write(v); }
var even = lst.where(x => x % 2 == 0);
```

线程与并发：List 本身为非线程安全，需在并发场景做同步控制。

