# Set（集合 / 无序不重复）

Set 是不保证顺序且不包含重复元素的集合，通常基于哈希实现。

创建：`var s = Set<int>();` 或字面量 `var s = {1,2,3};`

API：`add(x)`, `remove(x)`, `contains(x)`, `clear()`, `length`, `isEmpty`

示例：

```s
var s = Set<int>();
s.add(1);
if (s.contains(1)) { Console.print("has 1"); }
```

