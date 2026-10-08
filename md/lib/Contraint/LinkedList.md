# LinkedList（双向链表）

`Std.LinkedList<T>` 是泛型双向链表容器，实现 `Core.IIterable<T>` 和 `Core.IIterator<T>` 接口，支持泛型实例化。

采用 SL/C 双层架构：SL 层负责 Node 对象创建和泛型实例化，C 层负责所有指针操作和遍历，避免在脚本层做低效的指针操作。

创建：

```sl
Std.LinkedList<int> list = new()
# 或
var list = new Std.LinkedList<string>()
```

---

## 1. 常用属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `length` | int | 元素个数 |
| `isEmpty` | bool | 是否为空 |
| `isNotEmpty` | bool | 是否非空 |
| `first` | T | 首元素，空列表返回 null |
| `last` | T | 末元素，空列表返回 null |

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)

Console.println(list.length)        # 3
Console.println(list.isEmpty)       # false
Console.println(list.isNotEmpty)    # true
Console.println(list.first)         # 10
Console.println(list.last)          # 30
```

---

## 2. 添加元素

### 2.1 add / addLast

末尾添加：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.addLast(30)
# 链表: [10, 20, 30]
```

### 2.2 addFirst

头部添加：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.addFirst(5)
# 链表: [5, 10, 20]
Console.println(list.first)        # 5
```

### 2.3 addBefore / addAfter

在指定索引处之前/之后插入：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(30)
# [10, 30]

list.addBefore(1, 20)   # 在 index=1 之前插入 -> [10, 20, 30]
list.addAfter(0, 15)    # 在 index=0 之后插入 -> [10, 15, 20, 30]
```

### 2.4 insert

在指定索引处插入（等价于 addBefore）：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(30)
list.insert(1, 20)      # [10, 20, 30]

# 越界 index 静默忽略
list.insert(100, 999)   # 无效果
```

---

## 3. 删除元素

### 3.1 remove

按值删除首个匹配元素：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(10)
list.remove(10)         # [20, 10]（删除首个 10）

# 不存在的值，静默忽略
list.remove(999)        # 无效果
```

### 3.2 removeFirst / removeLast

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)

list.removeFirst()      # [20, 30]
list.removeLast()       # [20]

# 空列表操作，静默忽略
list.clear()
list.removeFirst()      # 无效果
list.removeLast()       # 无效果
```

### 3.3 removeAt

按下标删除：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)

list.removeAt(1)        # [10, 30]

# 越界 index 静默忽略
list.removeAt(100)      # 无效果
```

### 3.4 clear

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.clear()
Console.println(list.isEmpty)      # true
Console.println(list.first)        # null
Console.println(list.last)         # null
```

---

## 4. 查找

| 方法 | 说明 |
|------|------|
| `indexOf(item)` | 首次出现下标，未找到返回 -1 |
| `lastIndexOf(item)` | 最后一次出现下标，未找到返回 -1 |
| `contains(item)` | 是否包含指定元素 |

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(10)
list.add(30)

Console.println(list.indexOf(10))      # 0
Console.println(list.lastIndexOf(10))   # 2
Console.println(list.contains(20))      # true
Console.println(list.contains(999))    # false

# string 类型
Std.LinkedList<string> slist = new()
slist.add("aa")
slist.add("bb")
Console.println(slist.indexOf("bb"))    # 1
Console.println(slist.contains("cc"))   # false
```

---

## 5. 索引器

`_getItem_` / `_setItem_` 用于按下标读写元素，越界返回 null / 静默忽略：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)

# 读
Console.println(list._getItem_(0))     # 10
Console.println(list._getItem_(2))     # 30

# 写
list._setItem_(1, 999)
Console.println(list._getItem_(1))     # 999

# 越界
Console.println(list._getItem_(100))   # null
list._setItem_(100, 999)               # 无效果
```

---

## 6. 遍历

### 6.1 for 循环 + 索引器

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)

for i = 0, i < list.length, i++
{
    Console.println(list._getItem_(i))
}
```

### 6.2 迭代器协议

使用 `reset()` / `moveNext()` / `current` 遍历，支持多次迭代：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)

# 第一次遍历
list.reset()
while list.moveNext()
{
    Console.println("iter: " + list.current)
}

# 可再次遍历
list.reset()
while list.moveNext()
{
    Console.println("again: " + list.current)
}
```

获取迭代器对象：

```sl
var iter = list.iterator
iter.reset()
while iter.moveNext()
{
    Console.println(iter.current)
}
```

---

## 7. toArray

转换为 `Array<T>`：

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)

Array<int> arr = list.toArray()
for i = 0, i < arr.length, i++
{
    Console.println("arr[" + i + "] = " + arr[i])
}

# 空列表转换
Std.LinkedList<int> empty = new()
Array<int> emptyArr = empty.toArray()
Console.println(emptyArr.length)      # 0
```

---

## 8. toString

```sl
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)
Console.println(list.toString())      # [10,20,30]

# 空列表
Std.LinkedList<int> empty = new()
Console.println(empty.toString())     # []

# 单元素
Std.LinkedList<int> single = new()
single.add(5)
Console.println(single.toString())    # [5]
```

---

## 9. 泛型与混合类型

### 9.1 泛型字符串列表

```sl
Std.LinkedList<string> slist = new()
slist.add("aa")
slist.add("bb")
slist.add("cc")
Console.println(slist.first)           # aa
Console.println(slist.last)            # cc
Console.println(slist.indexOf("bb"))   # 1
```

### 9.2 混合类型列表

```sl
Std.LinkedList<object> list = new()
list.add(10)
list.add("hello")
list.add(3.14)
Console.println(list._getItem_(0))    # 10
Console.println(list._getItem_(1))    # hello
Console.println(list._getItem_(2))    # 3.14
```

---

## 10. 综合示例

```sl
# 交替添加删除
Std.LinkedList<int> list = new()
list.add(10)
list.add(20)
list.add(30)
list.addFirst(5)          # [5, 10, 20, 30]
list.remove(20)           # [5, 10, 30]
list.insert(1, 15)        # [5, 15, 10, 30]
list.removeLast()         # [5, 15, 10]

Console.println(list.length)    # 3
Console.println(list.first)     # 5
Console.println(list.last)      # 10

# 批量添加 100 个元素
Std.LinkedList<int> bulk = new()
for i = 0, i < 100, i++
{
    bulk.add(i)
}
Console.println(bulk.length)    # 100

# 删一半
for i = 0, i < 50, i++
{
    bulk.removeFirst()
}
Console.println(bulk.length)    # 50
Console.println(bulk.first)     # 50
Console.println(bulk._getitem_(49))  # 99
```

---

## 11. 边界与异常行为

- 所有按下标的操作（insert / removeAt / addBefore / addAfter）对非法下标采取静默忽略策略，不抛异常。
- `first` / `last` 在空列表时返回 null，调用方需判空。
- 元素相等语义：`remove` / `indexOf` / `contains` 使用 `==` 值比较。
- 非线程安全，并发场景需外部同步。

---

## 12. API 速查

| 成员 | 说明 |
|------|------|
| `length` | 元素个数（只读属性） |
| `isEmpty` / `isNotEmpty` | 是否为空 / 非空（只读属性） |
| `first` / `last` | 首元素 / 末元素，空列表返回 null |
| `add(item)` / `addLast(item)` | 末尾添加 |
| `addFirst(item)` | 头部添加 |
| `addBefore(index, item)` | 在指定索引之前插入 |
| `addAfter(index, item)` | 在指定索引之后插入 |
| `insert(index, item)` | 在指定索引处插入 |
| `remove(item)` | 删除首个匹配元素 |
| `removeFirst()` | 删除首元素 |
| `removeLast()` | 删除末元素 |
| `removeAt(index)` | 按下标删除 |
| `clear()` | 清空列表 |
| `indexOf(item)` | 首次出现下标，未找到 -1 |
| `lastIndexOf(item)` | 最后出现下标，未找到 -1 |
| `contains(item)` | 是否包含 |
| `_getItem_(index)` | 下标读取，越界返回 null |
| `_setItem_(index, value)` | 下标写入，越界静默忽略 |
| `toArray()` | 转换为 `Array<T>` |
| `toString()` | 格式 `[v1,v2,...]` |
| `reset()` | 重置迭代器 |
| `moveNext()` | 推进迭代器，返回是否还有元素 |
| `current` | 当前迭代元素（只读属性） |
| `iterator` | 获取迭代器对象（只读属性） |

---

## 13. 参考文件

- 实现：[source/Front/Lib/Std/Container/LinkedList.sl](../../source/Front/Lib/Std/Container/LinkedList.sl)
- C 层实现：[csimple_lang/src/vm/system_method_call/linkedlist_system_method.c](../../csimple_lang/src/vm/system_method_call/linkedlist_system_method.c)
- 测试：[test/ExpendTest/LinkedListTest.sl](../../test/ExpendTest/LinkedListTest.sl)
