# 运算符（Operators）

S 语言提供常见的算术、位运算和逻辑运算符。运算符在编译期间会被构建成 `MetaOpExpressNode` 并进行类型推断与必要的转换。

## 内置运算符

算术： `+ - * / %`

位运算： `<< >> | ^ &`

逻辑： `&& || !`

比较： `== != === !== > >= < <=`

说明：
- `==`：用于对象等价（reference equality）或值相等（对于基本类型），编译器在语义层区分具体比较语义。使用 `===` 可表示严格的值相等（value equality）。
- 自增/自减：语言中通常推荐 `i++` 或 `i += 1`，不鼓励使用前置 `--i` 来简化解析和阅读（实现可能受限）。
- 一元负号：若需要明确表达负数，请使用括号：`x + (-1)` 而避免 `x + -1` 在某些上下文产生歧义。

```s
var a = 1 + 2 * 3;
var b = (a >> 2) & 0xFF;
if (a == b) { }
if (a === b) { } // 严格值相等
```

## 运算符重载

自定义类可以通过约定方法名实现运算符重载。编译器按方法名将匹配的方法归入运算符方法表，运行时由 VM 按名查找并调用。

### 算术运算符重载

| 方法名 | 运算符 | 示例签名 |
|--------|--------|---------|
| `_add_` | `+` | `static Class _add_(Class a, Class b)` |
| `_sub_` | `-` | `static Class _sub_(Class a, Class b)` |
| `_mul_` | `*` | `static Class _mul_(Class a, Class b)` |
| `_truediv_` | `/` | `static Class _truediv_(Class a, Class b)` |
| `_mod_` | `%` | `static Class _mod_(Class a, Class b)` |

### 复合赋值运算符重载

| 方法名 | 运算符 | 示例签名 |
|--------|--------|---------|
| `_iadd_` | `+=` | `static Class _iadd_(Class a, Class b)` |
| `_imul_` | `*=` | `static Class _imul_(Class a, Class b)` |
| `_itruediv_` | `/=` | `static Class _itruediv_(Class a, Class b)` |

### 比较运算符重载

| 方法名 | 运算符 | 示例签名 |
|--------|--------|---------|
| `_lt_` | `<` | `static bool _lt_(Class a, Class b)` |
| `_le_` | `<=` | `static bool _le_(Class a, Class b)` |
| `_gt_` | `>` | `static bool _gt_(Class a, Class b)` |
| `_ge_` | `>=` | `static bool _ge_(Class a, Class b)` |
| `_eq_` | `==` | `static bool _eq_(Class a, Class b)` |
| `_ne_` | `!=` | `static bool _ne_(Class a, Class b)` |

### 逻辑运算符重载

| 方法名 | 运算符 | 示例签名 |
|--------|--------|---------|
| `_and_` | `&&` | `static bool _and_(Class a, Class b)` |
| `_or_` | `\|\|` | `static bool _or_(Class a, Class b)` |

> **注意**：`_and_`/`_or_` 在运行时通过 `TryRunClassLogicalOperator` 查找，需确保方法已定义在类中。

### 运算符重载使用示例

```s
class Vector2
{
    float x;
    float y;

    _init_(float _x, float _y)
    {
        x = _x;
        y = _y;
    }

    // 重载 + 运算符
    static Vector2 _add_(Vector2 a, Vector2 b)
    {
        return new Vector2(a.x + b.x, a.y + b.y);
    }

    // 重载 == 运算符
    static bool _eq_(Vector2 a, Vector2 b)
    {
        return a.x == b.x && a.y == b.y;
    }

    // 重载 += 运算符
    static Vector2 _iadd_(Vector2 a, Vector2 b)
    {
        a.x += b.x;
        a.y += b.y;
        return a;
    }

    override string toString()
    {
        return "(" + x + ", " + y + ")";
    }
}

var v1 = new Vector2(1.0, 2.0);
var v2 = new Vector2(3.0, 4.0);
var v3 = v1 + v2;       // 调用 _add_
var eq = (v1 == v2);    // 调用 _eq_
v1 += v2;               // 调用 _iadd_
```

## 索引器（下标访问）

通过定义 `_getItem_` 和 `_setItem_` 约定方法，自定义类型可以支持 `obj[index]` 形式的下标访问。

### `_getItem_` - 下标读取

使类型支持 `obj[index]` 或 `obj.$index` 形式的下标读取。

```s
T _getItem_(int _index)
```

- 参数：下标索引（`int` 类型）
- 返回值：指定位置的元素值
- 定义后，`obj[i]` 和 `obj.$i` 读取操作会自动调用此方法

### `_setItem_` - 下标写入

使类型支持 `obj[index] = value` 形式的下标赋值。

```s
void _setItem_(int _index, T _value)
```

- 参数：下标索引（`int` 类型）、要写入的值（`T` 类型）
- 返回值：`void`
- 定义后，`obj[i] = v` 赋值操作会自动调用此方法

### 索引器使用示例

```s
class IntArray
{
    Array<int> _data;

    _init_(int capacity)
    {
        _data = new Array<int>(capacity);
    }

    // 下标读取
    int _getItem_(int _index)
    {
        return _data._getItem_(_index);
    }

    // 下标写入
    void _setItem_(int _index, int _value)
    {
        _data._setItem_(_index, _value);
    }
}

var arr = new IntArray(10);
arr[0] = 42;        // 调用 _setItem_(0, 42)
var v = arr[0];     // 调用 _getItem_(0)
```

### 标准库中的索引器

`Array<T>` 和 `List<T>` 均已实现 `_getItem_`/`_setItem_`：

```s
var list = new List<int>();
list.add(10);
list.add(20);
list.add(30);

list[0] = 100;          // _setItem_(0, 100)
Console.println(list[1]); // _getItem_(1) -> 20
```

## Object 约定方法

以下方法定义在 `Object` 根类上，所有类均继承。子类可通过 `override` 重写。

| 方法名 | 签名 | 用途 |
|--------|------|------|
| `toString()` | `string toString()` | 返回对象字符串表示。字符串拼接 `+` 运算时自动调用 |
| `equals()` | `bool equals(object obj)` | 逻辑相等判断（区别于引用相等） |
| `hashCode` | `Int32 get hashCode()` | 返回对象哈希码（getter 属性） |
| `refEquals()` | `static bool refEquals(object a, object b)` | 静态引用相等比较（null 安全） |
| `type` | `Type get type()` | 获取对象运行时类型（getter 属性） |
| `release()` | `void release()` | 释放资源（`while` 语句结束时自动调用） |

```s
class Point
{
    int x;
    int y;

    _init_(int _x, int _y) { x = _x; y = _y; }

    override string toString()
    {
        return "(" + x + ", " + y + ")";
    }

    override bool equals(object obj)
    {
        var p = obj as Point;
        if (p == null) return false;
        return x == p.x && y == p.y;
    }
}

var p1 = new Point(1, 2);
var p2 = new Point(1, 2);
Console.println(p1);           // 自动调用 toString() -> (1, 2)
Console.println(p1 == p2);    // false（引用比较，除非重载 _eq_）
Console.println(p1.equals(p2)); // true（逻辑相等）
```

> **`==` 与 `equals` 的区别**：`==` 走 `_eq_` 运算符重载（未定义时为引用比较），`equals` 是普通方法调用。

## 类型转换

### `cast<T>()` - 显式类型转换

```s
var obj = someValue as object;
var str = obj.cast<string>();  // 显式转换为 string
```

- 所有类继承自 `Object`，因此都可用 `cast<T>()`
- 数字类型已内置常用重载

### `as` - 类型检查与转换

```s
var p = obj as Point;  // 若 obj 不是 Point 类型则返回 null
if (p != null) { ... }
```

## 程序入口与编译钩子

| 方法名 | 所在类 | 用途 |
|--------|--------|------|
| `_init_` | 所有类 | 构造函数，支持重载与 `base._init_()` 链式调用 |
| `_main_` | Project 类 | 程序主入口 |
| `_test_` | Project 类 | 测试入口 |
| `_before_` | Project 类 | 编译前回调（静态） |
| `_after_` | Project 类 | 编译后回调（静态） |

```s
project MyApp
{
    static void _before_(metaType type) { /* 编译前执行 */ }
    static void _after_(metaType type)  { /* 编译后执行 */ }

    static void _main_()
    {
        Console.println("Hello, World!");
    }
}
```

## 约定方法名总表

| 方法名 | 类别 | 用途 |
|--------|------|------|
| `_init_` | 构造 | 构造函数，支持重载 |
| `_getItem_` | 索引器 | 下标读取 `obj[i]` |
| `_setItem_` | 索引器 | 下标赋值 `obj[i] = v` |
| `_add_` | 运算符重载 | `+` |
| `_sub_` | 运算符重载 | `-` |
| `_mul_` | 运算符重载 | `*` |
| `_truediv_` | 运算符重载 | `/` |
| `_mod_` | 运算符重载 | `%` |
| `_iadd_` | 运算符重载 | `+=` |
| `_imul_` | 运算符重载 | `*=` |
| `_itruediv_` | 运算符重载 | `/=` |
| `_lt_` | 运算符重载 | `<` |
| `_le_` | 运算符重载 | `<=` |
| `_gt_` | 运算符重载 | `>` |
| `_ge_` | 运算符重载 | `>=` |
| `_eq_` | 运算符重载 | `==` |
| `_ne_` | 运算符重载 | `!=` |
| `_and_` | 运算符重载 | `&&` |
| `_or_` | 运算符重载 | `\|\|` |
| `cast<T>()` | 类型转换 | 显式类型转换 |
| `toString()` | Object 约定 | 字符串表示 |
| `equals()` | Object 约定 | 逻辑相等 |
| `hashCode` | Object 约定 | 哈希码 |
| `refEquals()` | Object 约定 | 引用相等 |
| `type` | Object 约定 | 运行时类型 |
| `release()` | 资源管理 | 释放资源 |
| `_main_` | 程序入口 | 主入口 |
| `_test_` | 程序入口 | 测试入口 |
| `_before_` | 编译钩子 | 编译前回调 |
| `_after_` | 编译钩子 | 编译后回调 |
