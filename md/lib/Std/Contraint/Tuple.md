# Tuple（元组）

元组是按顺序组合多个值的容器，支持不同类型混合。SimpleLanguage 提供两种形式：

- **模板形式** `Tuple<T1>` ~ `Tuple<T1,...,T8>`：C# 风格的强类型定长元组，公有字段 `item1`..`itemN`。
- **无模板形式** `Tuple`：Python 风格的动态类型元组，内部基于 `Array<object>` 实现，可无限扩展。

> 注意：当前不支持匿名元组字面量 `()`，所有元组必须显式使用 `Tuple` 标记。

---

## 1. 模板形式（强类型定长）

支持 1~8 个泛型参数，每个元素有具名字段 `item1`、`item2`、…、`itemN`。

### 1.1 构造与字段访问

```sl
# 单元素
Tuple<int> t1 = Tuple<int>(100)
Console.println(t1.item1)          # 100

# 双元素（混合类型）
Tuple<int, string> t2 = Tuple<int, string>(1, "one")
Console.println(t2.item1)           # 1
Console.println(t2.item2)           # one

# 三元素
Tuple<string, int, bool> t3 = Tuple<string, int, bool>("key", 42, true)
Console.println(t3.item3)           # true

# 四元素
Tuple<int, int, int, int> t4 = Tuple<int, int, int, int>(1, 2, 3, 4)
Console.println(t4.item4)           # 4

# 五到八元素
Tuple<int, int, int, int, int, int, int, int> t8 = Tuple<int, int, int, int, int, int, int, int>(1,2,3,4,5,6,7,8)
Console.println(t8.item8)           # 8
```

### 1.2 静态工厂方法

每个模板形式都有 `create` 静态方法：

```sl
Tuple<int, string> t = Tuple<int, string>.create(9, "nine")
Console.println(t.item1)            # 9
Console.println(t.item2)            # nine
```

### 1.3 长度属性

```sl
Tuple<int, string> t = Tuple<int, string>(1, "a")
Console.println(t.length)           # 2
```

### 1.4 下标读写（`$N` 语法）

模板形式支持下标语法 `t.$0`、`t.$1` 等，等价于 `_getItem_` / `_setItem_`：

```sl
Tuple<int> t = Tuple<int>(100)

# 读
Console.println(t.$0)               # 100
Console.println(t._getItem_(0))     # 100（等价）

# 写
t.$0 = 200
Console.println(t.item1)            # 200
t._setItem_(0, 300)
Console.println(t.item1)            # 300
```

越界访问：`_getItem_` 返回 `null`，`_setItem_` 静默忽略。

### 1.5 toString

```sl
Tuple<int, string> t = Tuple<int, string>(1, "one")
Console.println(t.toString())       # (1, one)
```

---

## 2. 无模板形式（动态类型）

内部基于 `Array<object>`，支持任意数量、任意类型元素，可动态扩展。

### 2.1 构造

支持空构造到 8 参构造：

```sl
# 空元组
Tuple t0 = Tuple()

# 1~8 参构造
Tuple t1 = Tuple(1)
Tuple t2 = Tuple(1, "a")
Tuple t3 = Tuple(1, "a", 2.5)
Tuple t4 = Tuple(1, "a", 2.5, true)
# ...最多 8 个直接参数
```

### 2.2 静态工厂（不限长度）

`create(params)` 接受任意数量参数：

```sl
Tuple t = Tuple.create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)  # 10 个元素
Console.println(t.length)           # 10
```

### 2.3 链式追加 add

`add` 返回 `this`，支持链式调用，自动扩容（初始容量 4，倍增策略 4->8->16->32…）：

```sl
Tuple t = Tuple()
t.add(1).add(2).add(3)
Console.println(t.length)           # 3

# 大量追加触发多次扩容
Tuple t2 = Tuple()
for i = 0, i < 20, i++
{
    t2.add(i)
}
Console.println(t2.length)          # 20
```

### 2.4 下标读写

与模板形式一样，使用 `$N` 语法或 `_getItem_` / `_setItem_`：

```sl
Tuple t = Tuple(10, 20, 30)

Console.println(t.$0)               # 10
Console.println(t.$1)               # 20

t.$1 = 999
Console.println(t._getItem_(1))     # 999

t._setItem_(2, "hello")
Console.println(t.$2)               # hello
```

越界访问：`_getItem_` 返回 `null`，`_setItem_` 静默忽略。

### 2.5 搜索

- `indexOf(object)`：返回首次出现下标，未找到返回 -1。
- `contains(object)`：是否包含指定元素，返回 `bool`。

```sl
Tuple t = Tuple("aa", "bb", "cc", "bb")
Console.println(t.indexOf("bb"))     # 1
Console.println(t.lastIndexOf("bb"))# 3
Console.println(t.contains("cc"))   # true
Console.println(t.contains("zz"))   # false
Console.println(t.contains(123))    # false
```

### 2.6 清空

```sl
Tuple t = Tuple(1, 2, 3)
t.clear()
Console.println(t.length)           # 0
Console.println(t.isEmpty)          # true

# clear 后可继续 add
t.add(99)
Console.println(t.$0)               # 99
```

### 2.7 isEmpty 属性

```sl
Tuple t = Tuple()
Console.println(t.isEmpty)          # true
t.add(1)
Console.println(t.isEmpty)          # false
```

### 2.8 toString

```sl
Tuple t = Tuple(1, "a", 2.5)
Console.println(t.toString())       # (1, a, 2.5)
```

---

## 3. 嵌套元组

元组可以嵌套使用：

```sl
# 模板套模板
Tuple<int, Tuple<string, bool>> nested = Tuple<int, Tuple<string, bool>>(1, Tuple<string, bool>("y", true))
Console.println(nested.item2.item1)    # y
Console.println(nested.item2.item2)    # true

# 无模板套模板
Tuple outer = Tuple(1, Tuple<int, string>(5, "five"), "tail")
Console.println(outer.$1)               # Tuple<int,string>(5, five)

# 模板套无模板
Tuple<string, Tuple> nested2 = Tuple<string, Tuple>("key", Tuple(1, 2))
Console.println(nested2.item2.$0)      # 1
```

---

## 4. 在标准库中的实际使用

Tuple 常用作 SQL 参数绑定的容器（见 Sqlite 和 Redis 标准库）：

```sl
# Sqlite 参数绑定
cursor.execute("INSERT INTO test1 (uid, name) VALUES (?, ?)", Tuple(1, "alice"))
cursor.execute("SELECT * FROM test1 WHERE uid > ?", Tuple(1))
cursor.execute("UPDATE test1 SET name = ? WHERE name = ?", Tuple("Alice", "alice"))
cursor.execute("DELETE FROM test1 WHERE name = ?", Tuple("bob"))

# Connection 级便捷方法
var cursor2 = conn.execute("SELECT * FROM test1 WHERE uid = ?", Tuple(1))
```

---

## 5. API 速查

### 模板形式 `Tuple<T1,...,TN>`

| 成员 | 说明 |
|------|------|
| `item1`..`itemN` | 公有字段，类型对应泛型参数 |
| `_init_(v1, ..., vN)` | 构造函数 |
| `static create(v1, ..., vN)` | 静态工厂 |
| `length` | 元素个数（只读属性） |
| `_getItem_(index)` | 下标读取，越界返回 null |
| `_setItem_(index, value)` | 下标写入，越界静默忽略 |
| `toString()` | 格式 `(v1, v2, ...)` |

### 无模板形式 `Tuple`

| 成员 | 说明 |
|------|------|
| `_init_()` / `_init_(v1,...,v8)` | 空构造到 8 参构造 |
| `static create(params)` | 不限长度工厂 |
| `add(value)` | 链式追加，返回 this，自动扩容 |
| `length` | 元素个数（只读属性） |
| `isEmpty` | 是否为空（只读属性） |
| `indexOf(value)` | 首次出现下标，未找到 -1 |
| `lastIndexOf(value)` | 最后出现下标，未找到 -1 |
| `contains(value)` | 是否包含 |
| `clear()` | 清空 |
| `_getItem_(index)` | 下标读取，越界返回 null |
| `_setItem_(index, value)` | 下标写入，越界静默忽略 |
| `toString()` | 格式 `(v1, v2, ...)` |

---

## 6. 参考文件

- 实现：[source/Front/Lib/Core/Tuple.sl](../../source/Front/Lib/Core/Tuple.sl)
- 测试：[test/BaseTest/TupleTest.sl](../../test/BaseTest/TupleTest.sl)
