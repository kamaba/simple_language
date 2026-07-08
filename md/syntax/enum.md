# Enum 枚举

## 概述

`enum` 是一种特殊的静态类，用于定义一组命名的常量或数据项。枚举支持多种底层类型（通过 `extends` 关键字指定），可以遍历、匹配、访问和（在 `mut` 修饰时）修改。

---

## 基本定义

枚举使用 `enum` 关键字定义，与 `class` / `data` 同级别。枚举项之间使用换行或 `;` 分隔，**每个枚举项必须使用 `=` 赋值**。

```ruby
enum Book
{
    C1 = 1
    mut string Str = ""
    C4 = 10
}
```

- `mut` 修饰的字段允许在运行时修改
- 不带 `mut` 的字段默认为只读

---

## extends — 指定底层类型

通过 `extends` 关键字可以为枚举指定底层类型，不同底层类型有不同的行为规则。

### extends 允许的类型（重要）

编译器在 `MetaEnum.ParseExtendsRelation` 中强制：**`enum` 的 `extends` 只能是下面几类，其余一律报错并回退为 `int` 语义。**

| 类别 | 说明 |
|------|------|
| **内置整数族** | 语言映射到 VM 的 `byte`/`sbyte`/`short`/`ushort`/`int`/`uint`/`long`/`ulong` 等整型底层 |
| **`string`** | 每项为字符串常量 |
| **`data`（具名）** | `extends` 某个已定义的 `data` 名，例如 `extends AA`（`data AA { ... }`） |
| **`data`（泛型写法）** | 如 `extends data`：表示底层为「任意 `data` 实例」的联合语义（实现上绑定到动态 data），成员可为多种已定义 `data` 的 `new` 表达式 |

**不允许：**

- 继承普通 **`class`**（用户类、模板类等均不可作为 enum 的 `extends` 目标）
- 继承另一个 **`enum`**
- 将 **`interface`** 或其它非 `MetaClass` 内置整型 / `string` / `MetaData` 的节点作为 extends 目标

### 具名 `data` 与成员取值的对应关系

当写 **`enum E extends AA`** 且存在 **`data AA { ... }`** 时：

- 枚举成员的 `=` 右侧**必须是** `AA() { ... }` 形式的 **data 构造**（与 `extends` 的 **`AA` 为同一 `MetaData`**）。
- **不允许**写其它 `data` 类型（例如 `extends AA` 却写 `BB() { ... }`）。

当写 **`enum E extends data`**（泛型 data）时：

- 成员仍须为 **某个已定义 `data` 类型** 的 `new` 表达式，但**可以**在各项中使用**不同**的 `data` 类型（见下文「data 类型」示例）。

### 整数类型（`int` / `uint` / `byte` / `sbyte` / `short` / `ushort` / `long` / `ulong`）

当底层类型为整数时，枚举项**可以省略 `=` 号**，后续项自动从上一个值递增。

```ruby
enum EErr extends int
{
    None = 1    # 值为 1
    First       # 自动为 2
    Second      # 自动为 3
    Thrill      # 自动为 4
    Four = 5    # 显式设为 5
    Six         # 自动为 6
}

enum EBytes extends byte
{
    x = 1
    x2          # 自动为 2
    x3 = 10
    x4 = 13
    x5          # 自动为 14
}
```

- 使用 `uint` 等无符号类型时，不能设置负值
- 使用 `int` 时，超出最大值后可以使用负值（溢出回绕）

### 字符串类型（`string`）

底层类型为 `string` 时，每个枚举项必须显式赋字符串值。

```ruby
enum Season extends string
{
    Spring = "春天"
    Summer = "夏天"
    Autumn = "秋天"
    Winter = "冬天"
}
```

### data 类型

#### `extends data`（多种 data）

底层类型为关键字 **`data`**（即「未写具体 data 名」）时，枚举项的值必须是已定义的 **`data`** 类型实例，**各项可以属于不同的 `data` 类型**。使用 `mut` 修饰的项可在运行时重新赋值。

```ruby
data RectShape
{
    x = 0
    y = 0
    width = 0
    height = 0
}
data CircleShape
{
    x = 0
    y = 0
    r = 1.0f
}

enum EShape extends data
{
    r1 = RectShape()  { x = 1, y = 1, width = 100, height = 100 }
    r2 = RectShape()  { x = 2, y = 2, width = 200, height = 200 }
    c1 = CircleShape(){ x = 1, y = 2, r = 100 }
    c2 = CircleShape(){ x = 2, y = 2, r = 300 }
    mut cd = CircleShape()   # mut 允许运行时修改
}
```

#### `extends` 某个具名 `data`（单一 data）

若写 **`enum EOnly extends RectShape`**，则**每一项**的 `=` 右侧**只能**是 **`RectShape() { ... }`**，不能再写 `CircleShape()` 等其它 `data`。

```ruby
data Point { x = 0; y = 0; }

enum EPoints extends Point
{
    A = Point() { x = 1, y = 2 }
    B = Point() { x = 3, y = 4 }
}
```

> **注意**：`extends data` 与 `extends SomeData` 的区别由编译器区分：前者允许多种 `data` 混排；后者成员类型必须与 `SomeData` 一致。

---

## data 类型枚举项（不使用 extends）

不指定 `extends` 时，枚举项可以混合使用任意非 `class`/`enum` 类型（如 `int`、`string`、`data` 等）。

```ruby
data BData
{
    i2 = 0
    url = ""
    xc1 = XC()
}

enum Book
{
    B1 = BData()
    {
        i2 = 20,
        url = "http://www.baidu.com",
        xc1 = XC()
    }
    B2 = BData(){ i2 = 10 }
    C1 = 1
    mut string Str = ""
    C4 = 10
}
```

---

## const enum — 常量枚举

使用 `const` 修饰的枚举为常量枚举，所有项均不可修改。

```ruby
data MixColor
{
    Red   = 0.0f
    Green = 0.0f
    Blue  = 0.0f
}

const enum ConstColor
{
    Red   = 0xff0000
    Green = 0x00ff00
    Blue  = 0x0000ff

    MixColor1 = MixColor(){ Red = 0.9f, Green = 0.1f, Blue = 0.01f }
    MixColor2 = MixColor(){ Red = 0.4f, Green = 0.22f, Blue = 0.7f }
}
```

---

## 限制规则

- **不允许**在 enum 内部嵌套 `enum` 或 `class`
- **`extends` 只能是**：内置整型族、`string`、**`data`**（关键字或具体 `data` 名）；**禁止** `extends` 普通 `class`、**禁止** `extends` 另一个 `enum`
- 使用整数类 `extends` 时，枚举项可省略 `=`，自动从上一个值递增
- **`extends` 具体 `data` 名时**：成员右侧**仅允许**该 `data` 类型的构造表达式
- **`extends data`（关键字）时**：成员须为**已定义**的 `data` 构造，但**可以**混用多种 `data` 类型
- 不使用 `extends` 时，内部项可以是除 `enum`/`class` 之外的任意类型
- 只有 `const` 和 `mut` 修饰符合法，其他限制关键字不可用

---

## 访问与使用

### 声明与赋值

```ruby
EErr e = EErr.None
EShape shape = EShape.r1
Season s = Season.Spring
```

### 访问内置属性

每个枚举项自带以下属性：

| 属性 | 说明 |
|------|------|
| `.index` | 该枚举项在枚举中的定义顺序（从 0 开始） |
| `.name`  | 该枚举项的名称字符串 |
| `.toString()` | 该枚举项的字符串表示 |

```ruby
EErr e = EErr.First
int idx = e.index         # 1（定义顺序）
string nm = e.name        # "First"
string str = e.toString() # "2"（实际值）
```

### mut 字段的动态修改

```ruby
EShape shape = EShape.cd
EShape.cd = CircleShape(){ x = 100, y = 100, r = 1000 }
# shape 同步变化（引用同一对象）
```

---

## 遍历枚举

使用 `for in` 可以遍历枚举的所有项，遍历对象为枚举定义名称的集合。

```ruby
for b in Season
{
    Core.print(b.name + " = " + b.ToString())
}

# 也可以遍历 .values 获取值集合
for v in Season.values
{
    Core.print(v)
}
```

---

## 与 switch 配合使用

switch 可以对枚举的定义名称进行匹配：

```ruby
GameState state = GameState.Begin

switch state
{
    case GameState.Init  { Core.print("初始化"); }
    case GameState.Begin { Core.print("游戏开始"); }
    case GameState.End   { Core.print("游戏结束"); }
}
```

与 `if` 配合：

```ruby
if state == GameState.Begin
{
    Core.print(state.toString())
}
```

---

## 完整示例

```ruby
import CSharp.System;

data RectShape { x = 0; y = 0; width = 0; height = 0; }
data CircleShape { x = 0; y = 0; r = 1.0f; }

enum EErr extends int
{
    None = 1
    First
    Second
    Four = 5
    Six
}

enum Season extends string
{
    Spring = "春天"
    Summer = "夏天"
    Autumn = "秋天"
    Winter = "冬天"
}

enum EShape extends data
{
    r1 = RectShape() { x = 1, y = 1, width = 100, height = 100 }
    r2 = RectShape() { x = 2, y = 2, width = 200, height = 200 }
    c1 = CircleShape(){ x = 1, y = 2, r = 100 }
    mut cd = CircleShape()
}

enum GameState
{
    Init  = 1
    Begin = 2
    End   = 3
}

EnumTest
{
    static func()
    {
        # 遍历 string 枚举
        for s in Season.values
        {
            Console.print("季节: " + s)
        }

        # 类型为 data 的枚举，mut 字段动态修改
        EShape shape = EShape.r1
        if shape == EShape.r1
        {
            Console.print("当前是 r1")
        }
        elif shape == EShape.cd
        {
            EShape.cd = CircleShape(){ x = 100, y = 100, r = 1000 }
        }

        # switch 匹配枚举
        GameState gs = GameState.Begin
        switch gs
        {
            case GameState.Init  { Console.print("初始化"); }
            case GameState.Begin { Console.print("游戏开始"); }
            case GameState.End   { Console.print("游戏结束"); }
        }
    }
}
```