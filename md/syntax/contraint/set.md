# Set（集合 / 无序不重复）

`Set<T>` 是不保证顺序且不包含重复元素的集合，仿 C# `HashSet<T>`（只存 key 的字典），
API 同时参考 Python `set` 与 Dart `Set`。底层基于哈希桶（`_buckets` + `_entries`），
所有查找 / 插入 / 删除 / 集合运算 / 迭代推进的耗时逻辑都通过 `SystemSet*`
系统调用下沉到 CVM 原生实现（`set_system_method.c`），SL 层仅负责 `hashCode` 虚调用、
`SetEntity` 泛型实例化与初始数组分配。

## 创建

```s
var s = Set<int>();            # 默认构造，容量为 0，首次 add 时分配为 4
var s2 = Set<int>(8);          # 指定初始容量（负数按 0 处理）
var s3 = Set<int>(arr);        # 从数组构造（逐个 add，哈希在 SL 层计算）
var s4 = {1, 2, 3};            # 字面量（若前端支持）
```

## 元素实体（SetEntity<T>）

内部每个元素是一个独立顶级类 `SetEntity<T>`，便于调试观察：

| 字段 | 类型 | 说明 |
|------|------|------|
| `hashId` | `int` | 缓存 `value.hashCode()`，桶链快速过滤用 |
| `value` | `T` | 实际元素值，公开可读写 |
| `link` | `int` | 哈希桶冲突链（同 bucket 的链式索引）；删除后复用作空闲链表节点 |

## 核心字段（与 Core.Map 一致）

| 字段 | 类型 | 说明 |
|------|------|------|
| `_buckets` | `Array<int>` | 桶数组，存 entry 索引 + 1（0 表示空桶） |
| `_entries` | `Array<SetEntity<T>>` | 实际元素实体数组（-1 表示空闲槽） |
| `_count` | `int` | 已使用槽位数（含已删除的空闲槽） |
| `_freeList` | `int` | 被删除元素的空闲链表头（-1 表示无空闲槽） |
| `_freeCount` | `int` | 空闲槽数量 |

> 有效元素个数 = `_count - _freeCount`，即 `length` / `count`。

## 属性（Property）

| 属性 | 类型 | 说明 |
|------|------|------|
| `length` | `int` | 有效元素个数（Python `len(set)` / Dart `set.length`） |
| `count` | `int` | 同 `length` |
| `isEmpty` | `bool` | 是否无元素 |
| `isNotEmpty` | `bool` | 是否至少有一个元素 |
| `capacity` | `int` | 内部数组长度；也可 `set` 触发 `resize` |
| `first` | `T` | 首元素（Dart `Set.first`），空集合返回 `null` |
| `last` | `T` | 末元素（Dart `Set.last`），空集合返回 `null` |

## 容量管理

- 默认构造容量为 0，首次 `add` 时分配为 **4**。
- 扩容策略：`0 -> 4`，之后**倍增** `4 -> 8 -> 16 ...`（与 C# 容器一致）。
- `ensureCapacity(min)`：预分配容量，避免多次扩容重哈希（C# `EnsureCapacity`）。
- `grow()`：扩容为当前 2 倍（或首次 4）。
- `capacity = value`：`value` 小于当前元素数时忽略；否则 `resize(value)`。

## 增删查改 API

| 方法 | 返回 | 说明 |
|------|------|------|
| `add(T item)` | `bool` | 新增返回 `true`；已存在或 `null` 返回 `false`（不覆盖） |
| `addRange(Array<T> items)` | `void` | 批量添加（Python `set.update`） |
| `contains(T item)` | `bool` | 是否包含（Python `in` / C# `Contains`） |
| `remove(T item)` | `bool` | 移除成功返回 `true`，不存在 / `null` 返回 `false` |
| `clear()` | `void` | 清空集合并复位迭代器 |

> 元素匹配用 `hashCode + ==` 值比较（与 Map 的 key 匹配语义一致）。
> `add(null)` / `contains(null)` / `remove(null)` 一律返回 `false`（集合不存 `null`）。

## 修改型集合运算（原地修改，VM 层完成）

| 方法 | 等价于 | 说明 |
|------|--------|------|
| `unionWith(Set<T> other)` | Python `\|=` / `set.update` | 并入并集 |
| `intersectWith(Set<T> other)` | Python `&=` | 仅保留交集 |
| `exceptWith(Set<T> other)` | Python `-=` | 删除也在 other 中的元素 |
| `symmetricExceptWith(Set<T> other)` | Python `^=` | 删除交集并并入双方独有元素 |

## 非修改型集合运算（返回新 Set）

| 方法 | 等价于 | 说明 |
|------|--------|------|
| `union(Set<T> other)` | Python `\|` / `set.union` | 并集 |
| `intersection(Set<T> other)` | Python `&` / `set.intersection` | 交集 |
| `difference(Set<T> other)` | Python `-` / `set.difference` | 差集（this - other） |
| `symmetricDifference(Set<T> other)` | Python `^` / `set.symmetric_difference` | 对称差 |
| `copy()` | Python `set.copy` / Dart `toSet` | 浅拷贝 |

## 判断型集合运算

| 方法 | 等价于 | 说明 |
|------|--------|------|
| `isSubsetOf(Set<T> other)` | Python `issubset` | 子集（空集是任何集合的子集） |
| `isSupersetOf(Set<T> other)` | Python `issuperset` | 超集（任何集合是空集的超集） |
| `isProperSubsetOf(Set<T> other)` | Python `set < set` | 真子集（子集且 `length` 更小） |
| `isProperSupersetOf(Set<T> other)` | Python `set > set` | 真超集（超集且 `length` 更大） |
| `overlaps(Set<T> other)` | C# `Overlaps` | 交集非空（任一公共元素即 `true`） |
| `setEquals(Set<T> other)` | C# `SetEquals` | 元素完全相同（与顺序无关） |

## 转换 / 迭代

| 方法 | 返回 | 说明 |
|------|------|------|
| `toArray()` | `Array<T>` | 精确长度的元素数组（无序） |
| `toList()` | `List<T>` | 转为列表 |
| `reset()` / `moveNext()` / `current` / `iterator` | — | 实现 `IIterable<T>` / `IIterator<T>`，`foreach` 热路径 |
| `toString()` | `string` | 输出 `{a,b,c}` 格式（同 Python/Dart 字面量） |

## 示例

```s
import Std;
import Core;

Set<int> s = new()
s.add(1)
s.add(2)
s.add(2)                       # 重复，返回 false
Console.println("" + s.length)          # 2
Console.println("" + s.contains(1))     # true
s.remove(1)
Console.println("" + s.contains(1))     # false

Set<int> a = Set<int>({1,2,3})
Set<int> b = Set<int>({2,3,4})
Console.println("" + a.union(b))        # {1,2,3,4}
Console.println("" + a.intersection(b)) # {2,3}
Console.println("" + a.difference(b))   # {1}
Console.println("" + a.setEquals(b))    # false
```

## 注意事项

- 集合**不存储 `null`**：`add(null)` / `contains(null)` / `remove(null)` 全部返回 `false`。
- 顺序不保证：`toArray()` / `toString()` / `foreach` 的结果顺序依赖哈希桶分布，
  不应依赖稳定顺序（集合相等用 `setEquals` 比较，与顺序无关）。
- 自定义类作为元素时，可重写 `hashCode()` 与相等逻辑来控制去重语义。
