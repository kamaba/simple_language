# data（数据类型）

`data` 是一种轻量的数据结构定义方式。它的成员声明风格接近 `class`，但用途更偏向于描述结构化数据、配置数据以及数据字面量。

## 1. 基本声明

普通 `data`：

```sl
data UserInfo
{
    id = 0
    name = ""
}
```

只读 `const data`：

```sl
const data Rule
{
    min = 1
    max = 100
}
```

说明：

- `data` 适合描述一组字段，不强调行为。
- `const data` 表示只读数据，语义上不应再修改其成员值。

## 2. 成员声明形式

`data` 内部可以像 `class` 字段一样声明成员，也可以直接使用数组字面量、对象字面量，以及嵌套 `data` 字面量。

### 2.1 标量成员

```sl
data UserInfo
{
    id = 0
    name = "guest"
    enabled = True
}
```

### 2.2 数组成员

可以直接用 `[]` 初始化数组成员：

```sl
data ScoreInfo
{
    scores = [100, 98, 87]
    tags = ["math", "final", "top"]
}
```

如果需要表达更复杂的层级，也可以写成对象数组或嵌套数组：

```sl
data GroupInfo
{
    matrix = [[1, 2], [3, 4]]
    students = [
        { name = "A", age = 18 },
        { name = "B", age = 19 }
    ]
}
```

### 2.3 对象成员

可以直接用 `{}` 表示一个匿名对象结构：

```sl
data StudentRecord
{
    sid = 0
    name = ""
    profile = { grade = 3, rank = 1 }
}
```

对象成员内部同样可以继续包含数组或子对象：

```sl
data ComplexProfile
{
    profile = {
        grade = 3
        rank = 1
        contact = {
            city = "Shenzhen"
            zip = 518000
        }
        subjects = ["math", "physics"]
    }
}
```

### 2.4 嵌套 data 成员

可以直接把另一个 `data` 当作成员值来初始化：

```sl
data MetaInfo
{
    level = 1
    passed = False
}

data StudentRecord
{
    sid = 0
    name = ""
    meta = MetaInfo(){ level = 2, passed = True }
}
```

### 2.5 组合示例

下面这个例子把标量、数组、对象、对象数组、嵌套 `data` 放在同一个 `data` 里：

```sl
data MetaInfo
{
    level = 1
    passed = False
}

data FullStudentRecord
{
    sid = 0
    name = ""
    scores = [95, 88, 91]
    profile = {
        grade = 3
        rank = 5
        address = {
            city = "Shenzhen"
            zip = 518000
        }
    }
    awards = [
        { name = "Math", year = 2024 },
        { name = "Physics", year = 2025 }
    ]
    meta = MetaInfo(){ level = 2, passed = True }
}
```

## 3. 初始化方式

`data` 的初始化方式与 `class` 对象初始化的实际使用方式很接近，常见有下面三种。

### 3.1 先声明，再 `new()`

```sl
StudentRecord a = new()
```

适合先创建默认实例，再逐步赋值。

### 3.2 直接使用 `DataName(){ ... }`

```sl
StudentRecord b = StudentRecord(){ sid = 2, name = "n2" }
```

适合在一处直接完成构造和初始化。

也可以在里面继续写数组、对象或嵌套 `data`：

```sl
FullStudentRecord c = FullStudentRecord()
{
    sid = 3
    name = "n3"
    scores = [99, 97, 96]
    profile = {
        grade = 4
        rank = 2
    }
    meta = MetaInfo(){ level = 3, passed = True }
}
```

### 3.3 先声明，再赋值 `{ ... }`

```sl
StudentRecord d
d = { sid = 4, name = "n4" }
```

这种方式适合先声明变量，再在后续位置补全对象内容。

同样可以结合对象嵌套：

```sl
FullStudentRecord e
e = {
    sid = 5
    name = "n5"
    profile = {
        grade = 5
        rank = 1
        address = {
            city = "Guangzhou"
            zip = 510000
        }
    }
}
```

## 4. 当前建议理解

从语法设计上，`data` 可以表达以下几类结构：

- 普通字段，如 `id = 0`
- 数组字段，如 `scores = [1, 2, 3]`
- 匿名对象字段，如 `profile = { grade = 3, rank = 1 }`
- 对象数组，如 `items = [{ id = 1 }, { id = 2 }]`
- 嵌套 `data`，如 `meta = MetaInfo(){ ... }`
- 多层组合嵌套，如“对象里套数组、数组里套对象、对象里再套 data”

## 5. 约束

`data` 比 `class` 更强调结构，因此约束也更严格：

- `data` 不支持继承，不能使用 `extends`。
- `data` 不定义方法，通常只包含成员变量。
- `const data` 应视为只读结构。

## 6. 当前编译/运行状态说明

就当前实现而言，前端解析层已经支持 `data` 的声明和多种字面量写法；但某些复杂的成员默认值、嵌套对象、嵌套 `data`、对象数组等路径，在部分 Meta / IR / VM 流程下仍可能存在未补齐的实现缺口。

因此建议这样使用这份文档：

- 这份文档可以作为 `data` 的语法目标和语义说明。
- 做语法覆盖测试时，可以尽量把结构形式都写全。
- 做可执行运行时测试时，建议和“纯结构声明测试”分开验证，避免把运行时现有缺口和语法能力混在一起。

## 7. bind 相关

`bind` 可以把一个或多个 `data` 结构绑定到 `class` / `interface` 上。示例可参考：

- [test/ExpendTest/BindDataTest.sl](../../test/ExpendTest/BindDataTest.sl)

如果多个被绑定的 `data` 中存在同名字段，应当在目标类型中显式消除歧义，而不要依赖隐式推断。

