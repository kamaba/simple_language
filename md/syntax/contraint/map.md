# Map（字典 / 哈希表）

按 Dart 风格，Map 是键值对集合，常用 `Map<K, V>` 泛型形式。Map 支持通过键快速查找值（通常基于哈希）。

创建：
- 字面量：`var m = { "a": 1, "b": 2 };`
- 空 Map：`var m = Map<String, Int32>();`

常用 API：
- `m[key]`、`m[key] = value`
- `m.containsKey(key)`、`m.remove(key)`、`m.clear()`
- `m.keys`、`m.values`、`m.entries`

示例：

```s
var m = { "x": 1, "y": 2 };
if (m.containsKey("x")) { Console.print(m["x"]); }
for e in m.entries { Console.print("${e.key}=${e.value}"); }
```

注意：
- Map 的内部实现建议使用哈希桶以保证平均 O(1) 的查找性能（当前实现可能为线性列表）。

性能与并发：
- 平均情况下，合适的哈希实现能提供 O(1) 的读写；在负载因子过高或哈希冲突严重时，性能会退化。
- Map 不是线程安全的；在多线程/并发场景需要通过外部锁或并发容器进行保护。

常见模式：
- 迭代键/值：`for k in m.keys { ... } for v in m.values { ... }`。
- 条件插入：`if (!m.containsKey(k)) { m[k] = default; }`

