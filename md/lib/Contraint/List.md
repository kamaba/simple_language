# List（列表 / 可变数组）

`Std.List<T>` 是按顺序保存元素的可变集合（动态数组），支持泛型。实现 `Core.IIterable<T>`、`Core.IIterator<T>` 与 `IList<T>` 接口，可用 `for v in list` 直接遍历。

创建：

- 空列表（内部初始容量为 4）：`var list = new Std.List<int>();` 或 `Std.List<int> list = new();`
- 指定初始容量：`var list = Std.List<int>(8);`（capacity < 0 时按 0 处理）
- 集合初始化器：`var list = List<int>(4){123, 456, 789};`
- 静态工厂：`var list = Std.List<int>.create(8);`

容量机制（与 C# `List<T>` 一致）：

- 默认构造容量为 4；`add` 写满时倍增扩容 4→8→16…（`grow`）。
- `ensureCapacity(min)`：一次性预留至少 min 的容量（倍增仍不够时直接取 min）。
- `capacity` 可读写：调小需不小于当前 length（否则忽略）；调大会重排内部数组。
- `clear()` 后 length 与 capacity 均归 0。

常用属性：

- `list.length`：元素个数。
- `list.isEmpty` / `list.isNotEmpty`：是否为空 / 是否非空。
- `list.first` / `list.last`：首元素 / 末元素；空列表返回 null。
- `list.capacity`：当前容量（内部存储长度）。
- `list.index`：迭代器当前位置（可读写，写时做 0..length 边界检查）。

增删：

- `list.add(x)`：尾部追加，满时自动扩容。
- `list.insert(index, x)`：index 处插入（非法 index 静默忽略）。
- `list.addRange(other)`：尾部批量追加另一个 List 的全部元素（other 为 null 时忽略）。
- `list.insertRange(index, other)`：index 处批量插入另一个 List 的全部元素。
- `list.remove(x)`：删除第一个等于 x 的元素（值比较），无则不动。
- `list.removeAt(index)`：按下标删除（非法 index 静默忽略）。
- `list.removeRange(index, count)`：删除区间 [index, index+count)，越界部分自动截断。
- `list.clear()`：清空并释放容量。

查找：

- `list.indexOf(x)`：首次出现下标，未找到返回 -1。
- `list.lastIndexOf(x)`：最后一次出现下标，未找到返回 -1。
- `list.contains(x)`：是否包含（内部走 indexOf）。
- `list[i]` / `list[i] = v`：下标读写；也可用 `list.$3` 成员形式访问下标 3。

填充与变换：

- `list.fill(value, startIndex = 0, count = 0)`：
  - `count == 0`：从 startIndex 填到当前 length 末尾；
  - `count > 0`：精确填 count 个，超出 capacity 剩余槽位自动截断；
  - `count < 0`：参数非法，打印 `List.fill: index out of range` 并返回；
  - 填充区间超出当前 length（但不超过 capacity）时，length 自动扩展到区间末尾。
  - 注意：跨模块调用省略参数时由 VM 做零值填充，因此默认值必须与类型零值一致（count 默认 0 而非 -1）。
- `list.reverse()`：原地反转。
- `list.getRange(index, count)`：拷贝区间 [index, index+count) 为新 List（越界截断；index 非法返回 null）。
- `list.toArray()`：导出为长度恰为 length 的 `Array<T>`。
- `list.toString()`：格式化为 `[a,b,c]`（null 元素输出 `null`）。

遍历：

- for-in：`for v in list { ... }`
- 手动迭代器：`list.reset(); while list.moveNext() { var v = list.current; }`
- 迭代中写当前元素：`list.current = v`。

示例：

```s
Std.List<int> list = new();
list.add(1);
list.add(2);
list.add(3);

Console.println(list.length);        # 3
Console.println(list.first);         # 1
Console.println(list.last);          # 3
Console.println(list.contains(2));   # true
Console.println(list.indexOf(3));    # 2

list.insert(1, 99);                  # [1,99,2,3]
list.reverse();                      # [3,2,99,1]

var sub = list.getRange(1, 2);       # [2,99]
var arr = list.toArray();            # Array<int>

for v in list
{
    Console.println(v);
}

list.removeRange(0, 2);              # 移除前两个
list.clear();                        # 清空，length = capacity = 0
```

边界与异常行为：

- 所有按下标的操作（insert / removeAt / removeRange / getRange / index setter）对非法下标采取静默忽略策略，不抛异常。
- `first` / `last` / `getRange` 在空列表或非法参数时返回 null，调用方需判空。
- 元素相等语义：`remove` / `indexOf` / `contains` 使用 `==` 值比较（数值、字符串、布尔按值；类类型按 equals 语义）。

线程与并发：

- List 本身为非线程安全，需在并发场景做同步控制。
