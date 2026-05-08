# data（数据类型）

`data` 是一种轻量的数据结构定义方式。它的成员声明风格接近 `class`，但用途更偏向于描述结构化数据、配置数据以及数据字面量。

它更接近 C# 里的 `struct` / 纯数据载体概念，但这里要特别强调：

- `data` 只用于提供数据，不提供函数行为
- `data` 内部不支持定义成员函数 / 普通函数
- `data` 的重点是结构、成员值、结构匹配、打印与比较

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

### 1.1 const 约束

推荐的稳定语义是“以整个 `data` 容器为单位施加只读约束”：

```sl
const data Rule
{
    min = 1
    max = 100
}
```

在这种情况下，应把 `Rule` 视为只读数据对象：

- 可以读取 `Rule.min`、`Rule.max`
- 不应允许对其成员重新赋值
- 不应允许对整个 `Rule` 对象重新赋值

例如下面这些都应视为非法或受限语义：

```sl
# Rule.min = 2
# Rule = { min = 2, max = 200 }
```

对于下面这种写法：

```sl
# data a = { const aa = 20 }
```

它表达的是“匿名 `data` 内部成员级 const 约束”的设计意图。

当前文档建议：

- 成员级 `const` 约束应被记录为 `data` 语义的一部分
- 如果当前解析/运行时尚未完整支持该写法，则应优先使用容器级 `const data`
- 后续如果启用该语法，则 `aa` 应被视为只读成员，不允许被再次赋值

## 2. 成员声明形式

`data` 内部可以像 `class` 字段一样声明成员，也可以直接使用数组字面量、对象字面量，以及嵌套 `data` 字面量。

支持的核心成员值类型包括：

- 常数：`int8/int16/int32/int64/uint/string/bool/float/double/...`
- 数组：`[]`
- 匿名对象 / 匿名 data：`{}`
- 具名 `class` 实例：`ClassName(){}`
- 具名 `data` 实例：`DataName(){}`
- `enum` 值：`EnumName.Member`

### 2.1 标量成员

```sl
data UserInfo
{
    id = 0
    name = "guest"
    enabled = True
}
```

`data` 中的数组成员，数组内部应支持以下元素：

- 常数
- 数组
- 对象
- 匿名对象 / 匿名 data
- 具名 `class` / `data` /`enum` 初始化结果

也就是说，数组不只是基础类型数组，也可以是多层结构数组。

### 2.2 数组成员

可以直接用 `[]` 初始化数组成员：

```sl
data ScoreInfo
{
    scores = [100, 98, 87]
    tags = ["math", "final", "top"]
}
```

这类匿名结构本质上也应按匿名 `data` 处理，即它是纯数据结构，不带函数能力。

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

也支持直接写匿名 `data`：

```sl
data A
{
    nd = {
        a = 20
        b = 30
    }
}
```

这里的 `nd = { ... }` 应被视为匿名 `data` / 匿名结构数据。

### 2.5 class / data / enum 成员支持

`data` 的成员值不只支持匿名结构，也支持具名 `class`、具名 `data`、`enum`。

#### class 成员值

```sl
class CC
{
    va = 100
}

data A
{
    cc = CC(){ va = 200 }
}
```

#### data 成员值

```sl
data DA
{
    a = 20
}

data B
{
    vb = DA(){ a = 30 }
}
```

#### enum 成员值

```sl
enum DataKind
{
    Base = 1
    Advanced = 2
}

data C
{
    kind = DataKind.Advanced
}
```

### 2.6 组合示例

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

对于可写 `data` 对象，还应支持在 `new()` 之后继续整体或局部重赋值，例如：

```sl
StudentRecord a = new()
a.sid = 10
a = { sid = 11, name = "next" }
a = StudentRecord(){ sid = 12, name = "final" }
```

也就是说：

- `new()` 创建后的 `data` 对象，如果不是 `const`，应允许成员重写
- 也应覆盖“整个对象重新赋值”的测试

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

### 3.4 静态 data 对象的重新赋值

对于顶层、可直接访问的非 `const data` 对象，也应覆盖“静态对象写入”场景。

至少应包含下面两类：

```sl
GlobalCounter.totalExamCount = 10
GlobalCounter.totalScore = 200
```

以及在语法/运行时支持时，覆盖整个对象重赋值：

```sl
GlobalCounter = { totalExamCount = 11, totalScore = 210 }
```

与之相对，`const data` 不应允许上述修改行为。

### 3.3 先声明，再赋值 `{ ... }`

```sl
StudentRecord d = { sid = 4, name = "n4" }
```

这种方式适合先声明变量，再在后续位置补全对象内容。

同样可以结合对象嵌套：

```sl
FullStudentRecord e = {
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
- `class` 成员，如 `cc = CC(){ va = 200 }`
- `enum` 成员，如 `kind = DataKind.Advanced`
- 多层组合嵌套，如“对象里套数组、数组里套对象、对象里再套 data”

同时要保持一个明确限制：

- `data` 不是行为对象
- `data` 不提供成员函数
- `data` 的职责是承载和组织数据

## 4.1 链式读取

`data` 应支持链式读取其内部成员值，包括：

- 匿名对象链：`record.profile.address.city`
- 具名 `data` 链：`record.meta.level`
- `class` 成员链：`sample.cc.value`
- `const data` 成员链：`Rule.min`

示例：

```sl
city = StudentRecord.profile.address.city
level = StudentRecord.meta.level
value = ClassDataEnumSample.cc.value
passLine = ScoreRule.passLine
```

这类访问应作为 `data` 的基础测试面之一。

## 5. Meta 层规则

在 Meta 层里，`data` 的核心规则可以概括为下面几条：

- `data` 的成员初始化统一走 `=` 赋值。
- 右值可以是常量、数组、匿名 `data`、具名 `data`、`class`、`enum`。
- 匿名 `data` 的本质不是“带方法的对象”，而是“纯结构数据”。
- `data` 进入 IR 时，整体按接近 `class` 字段布局的方式导出，但没有 method 概念。
- 匿名 `{}` 在 `data` 语义里应优先按匿名 `data` / 匿名结构数据理解。

### 5.1 允许的右值形式

```sl
data DataMemberShape
{
    i1 = 10
    arr = [1, 2, 3]
    anon = { x = 1, y = 2 }
    meta = MetaInfo(){ level = 2, passed = True }
    holder = DataHolder(){ value = 7 }
    kind = DataKind.Base
}
```

也就是说，在 `data` 内部：

- `[]` 表示数组数据
- `{}` 表示匿名结构数据
- `DataName(){}` 表示具名 `data`
- `ClassName(){}` 表示 `class`
- `EnumName.Member` 表示 `enum`

并且 `[]` 内部允许继续放：

- 常数
- 数组
- 对象
- 匿名对象 / 匿名 `data`
- `ClassName(){}`
- `DataName(){}`

### 5.2 匿名 data 的结构匹配

匿名 `data` 需要按“字段结构 + 字段类型”进行匹配，而不是只按名字处理。

例如：

```sl
data typedProfile = {
    a2 = 10
    a3 = 10000L
    string a = "333"
    int[] a4 = {1, 2, 3, 4}
}
```

这类匿名结构在 Meta 层中，会被理解为一组纯字段：

- `a2` -> `int32`（或项目内部实际推导后的整数类型）
- `a3` -> `int64`
- `a` -> `string` / `string ptr`
- `a4` -> `array ptr`

如果字段写成显式类型声明，例如 `string a = ...`、`int[] a4 = ...`，则优先使用显式声明的类型；如果未显式声明，则按右值表达式结果推导类型。

### 5.3 匿名 data 的复用规则

只要是匿名 `data`，都应先按结构进行匹配：

- 如果当前结构已经存在对应的 `MetaData`，直接复用已有结构类型。
- 如果当前结构尚不存在，则先生成新的匿名结构类型，再继续后续 Meta / IR 逻辑。

这条规则同样适用于：

- `data` 成员中的匿名 `{}`
- 语句级 `data x = { ... }`
- 匿名结构内部继续嵌套匿名结构的情况

### 5.4 匿名 data 中的带类型字段

匿名 `data` 内部允许同时出现：

- 不带显式类型的字段，如 `a2 = 10`
- 带显式类型的字段，如 `string a = "333"`

例如：

```sl
data profile = {
    a2 = 10
    string a = "333"
    MetaInfo meta = MetaInfo(){ level = 8, passed = True }
    DataHolder holder = DataHolder(){ value = 11 }
    DataKind kind = DataKind.Advanced
    anon = {
        code = 7
        title = "ok"
    }
}
```

这里的类型处理规则是：

- 无类型字段按右值推导
- 显式类型字段按声明类型约束
- 匿名子结构继续做结构匹配

## 6. 运行时语义

### 6.1 data 比较

当两个 `data` 使用 `==` 进行比较时，规则应为：

1. 先比较两边的结构是否相同
2. 结构相同后，再比较内部成员数据缓冲区 `m_MemberDataBuffer` 是否一致

也就是说，`data == data` 不是单纯比较引用，而是以“结构 + 数据内容”为基础进行比较。

### 6.2 data 打印

`data` 打印时，应输出为 `data` 的结构化格式与当前成员数据值。

目标效果不是只打印类型名，也不是只打印对象地址，而是输出接近 `data` 字面量/格式化结构的可读结果。

例如：

```sl
data ScoreData
{
    id = 1
    math = 90
    english = 95
}
```

打印时，应表现为包含字段名与字段值的 `data` 格式化结果。

## 7. 结论

`data` 的定位应固定为：

- 类似 `struct` 的纯数据结构
- 无函数、无行为，只负责承载数据
- 支持常数、数组、匿名对象、匿名 `data`、具名 `class`、具名 `data`、`enum`
- 支持多层嵌套
- `==` 按结构 + `m_MemberDataBuffer` 内容比较
- 打印输出结构化 `data` 内容

## 6. 约束

`data` 比 `class` 更强调结构，因此约束也更严格：

- `data` 不支持继承，不能使用 `extends`。
- `data` 不定义方法，通常只包含成员变量。
- `const data` 应视为只读结构。

## 7. 当前编译/运行状态说明

就当前实现而言，前端解析层已经支持 `data` 的声明和多种字面量写法；本轮规则补充后，Meta 层也补齐了匿名 `data` 的结构匹配、带类型字段处理，以及匿名结构复用的主干逻辑。

但在部分运行时路径上，复杂成员默认值、深层嵌套对象、对象数组等路径，仍可能存在未完全补齐的 IR / VM 细节差异。

因此建议这样使用这份文档：

- 这份文档可以作为 `data` 的语法目标和语义说明。
- 做语法覆盖测试时，可以尽量把结构形式都写全。
- 做可执行运行时测试时，建议和“纯结构声明测试”分开验证，避免把运行时现有缺口和语法能力混在一起。

## 8. bind 相关

`bind` 可以把一个或多个 `data` 结构绑定到 `class` / `interface` 上。示例可参考：

- [test/ExpendTest/BindDataTest.sl](../../test/ExpendTest/BindDataTest.sl)

如果多个被绑定的 `data` 中存在同名字段，应当在目标类型中显式消除歧义，而不要依赖隐式推断。


## 9. 关于data类型的比较
 在data类型中，比较是对里边的数据进行比较，也就是对里边内容直接比较 比如 
 data Book{ name = "ok", price = 25 }    
 Book a = { name = "ok1" }  Book b = { name = "ok" } Book c = new()
 if a == b 是不正确的   if b == c 是正确 的  

 在data里边，还要提供一些系统方法 比如 布局对比，   排列对比   

 在data里边，如果是数字类型，默认是32位的int型的  只有加了限制，才会确定更多的类型

 比较字符串，尽量的，把字符串提取出来，然后进行对比 

 也要对data的数据，提供一个系统方法，就是打印其内容

 匿名的内容，都需要,号处理
