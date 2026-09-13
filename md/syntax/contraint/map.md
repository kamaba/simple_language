# Map（字典 / 哈希表）

`Std.Map<TKey,TValue>` 是键值对集合，仿 Java `HashMap` / CLR `Dictionary` / Dart `Map` 设计，支持泛型。实现 `IMap<TKey,TValue>`、`Core.IIterable<MapEntity<TKey,TValue>>` 与 `Core.IIterator<MapEntity<TKey,TValue>>` 接口，可用 `for e in map` 直接遍历（迭代元素为 `MapEntity`）。

键值对实体 `MapEntity<TKey,TValue>` 是独立的顶级公开类：

- `key` / `value`：公开可读写的键与值。
- `hashId`：缓存 `key.hashCode`，供哈希类容器扩展使用（外部模块经 `map.current` / `map.entryAt(i)` 访问）。

创建：

- 空 Map（内部预分配 4 槽数组，首次 add 时容量变为 4）：`var map = new Std.Map<int,int>();` 或 `Std.Map<int,int> map = new();`
- 指定初始容量：`var map = Std.Map<int,int>(8);`（capacity < 0 时按 0 处理），也支持模块限定名构造 `Std.Map<int,int>(8)`。
- 集合初始化器（`key:value` 字面量）：`var map = Map<int,string>(4){100:"aaa", 200:"bbb"};`

容量机制（与 C# `Dictionary<K,V>` / `List<T>` 一致）：

- 默认构造首次 add 时容量 0->4，写满时倍增扩容 4->8->16…（`grow`）。
- `ensureCapacity(min)`：一次性预留至少 min 的容量（倍增仍不够时直接取 min）。
- `capacity` 可读写：调小需不小于当前 length（否则忽略）；调大重排内部数组。
- `clear()` 后 length 与 capacity 均归 0。

常用属性：

- `map.length`：键值对个数。
- `map.count`：同 length（CLR Dictionary.Count 语义）。
- `map.isEmpty` / `map.isNotEmpty`：是否为空 / 是否非空。
- `map.capacity`：当前容量（内部存储长度）。
- `map.index`：迭代器当前位置（可读写，写时做 0..length 边界检查）。

增删改查：

- `map.add(key, value)`：add 语义（同 C# `Dictionary.Add` 的无异常版 / `TryAdd`）--key 已存在时不修改原值并返回 `false`，新插入返回 `true`。需要覆盖旧值请用 `map[key] = value`（put 语义）。
- `map[key] = value`：put 语义（同 Java `HashMap.put` / Dart `m[k]=v`），key 已存在则更新 value，不存在则插入；内部走 `_setItem_`。
- `map[key]`：读取（`_getItem_`），key 不存在返回 null（Dart Map 语义）。
- `map.remove(key)`：删除并返回旧值（Java / Dart remove 语义），key 不存在返回 null。
- `map.removeAt(index)`：按实体下标删除（非法下标静默忽略）。
- `map.clear()`：清空并释放容量。

查找：

- `map.containsKey(key)` / `map.containsValue(value)`：是否包含指定键 / 值。
- `map.indexOfKey(key)`：key 首次出现的实体下标，未找到返回 -1。
- `map.entryAt(index)`：按下标取 `MapEntity`（非法下标返回 null）。
- `map.getOrDefault(key, defaultValue)`：key 不存在返回默认值（Java 8 `getOrDefault`）。
- `map.putIfAbsent(key, value)`：key 不存在时才插入；返回已存在的值，原本不存在则插入并返回 null（Java 8 `putIfAbsent`）。

键值集合导出：

- `map.keys`：全部 key 组成的 `List<TKey>`（Dart Map.keys）。
- `map.values`：全部 value 组成的 `List<TValue>`（Dart Map.values）。
- `map.toArray()`：导出为长度恰为 length 的 `Array<MapEntity<TKey,TValue>>`。
- `map.toList()`：导出为 `List<MapEntity<TKey,TValue>>`。
- `map.toString()`：格式化为 `{key=value,key=value}`（同 Java HashMap.toString；null 键值输出 `null`）。

遍历：

- for-in：`for e in map { ... }`（e 为 MapEntity，取 `e.key` / `e.value`）。
- 手动迭代器：`map.reset(); while map.moveNext() { var e = map.current; }`。
- 迭代中写当前实体：`map.current = newValue;`（替换当前实体的 value，key 不可变）。

示例：

```s
Std.Map<int,string> map = new();
map.add(1, "one");                  # true（新插入）
map.add(1, "uno");                  # false（key 已存在，不覆盖）

map[2] = "two";                     # put 语义，插入
map[2] = "TWO";                     # put 语义，覆盖
int k = 3
map[k] = "three";                   # 变量 key 写入

Console.println(map.length);        # 3
Console.println(map[1]);            # one（add 未覆盖）
Console.println(map[2]);            # TWO
Console.println(map[k]);            # three（变量 key 读取）
Console.println(map[99]);           # null（key 不存在）

Console.println(map.containsKey(2));    # true
Console.println(map.getOrDefault(9, "n/a"));  # n/a
Console.println(map.putIfAbsent(9, "nine"));  # null（已插入）

var old = map.remove(2);                # 返回 "TWO"
List<int> ks = map.keys;
Console.println(map.toString());

for e in map
{
    Console.println(e.key + " = " + e.value);
}

map.clear();                            # length = capacity = 0
```

边界与异常行为：

- key 不存在时 `_getItem_` / `remove` / `entryAt(越界)` 返回 null，调用方需判空。
- `removeAt` 对非法下标静默忽略，不抛异常。
- `capacity` setter 传入小于 length 的值时忽略。
- key 相等语义：`indexOfKey` / `containsKey` 使用 `==` 值比较（数值、字符串、布尔按值；类类型按 equals 语义，与 `List.indexOf` 一致）。

性能与实现说明：

- 当前实现底层与 List 同构：`Array<MapEntity>` 顺序存储，增删改查全部通过 `SystemArray*` 系统函数操作底层数组；key 查找为线性 O(n)。
- `MapEntity.hashId` 已缓存 key 哈希，为后续哈希桶实现预留（目标平均 O(1)）。

线程与并发：

- Map 本身为非线程安全，需在并发场景做同步控制。
