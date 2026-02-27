# Switch 条件控制

## 概述

`switch` 语句将一个值与多个可能的匹配模式进行比较，并执行第一个匹配成功的 case 分支中的代码。S 语言的 `switch` 提供了强大的模式匹配能力，包括值匹配、类型匹配、枚举匹配等多种方式。

与传统的 C 风格 switch 不同，S 语言的 switch：
- **不需要 break**：默认情况下不会贯穿到下一个 case
- **支持多种匹配模式**：值、类型、枚举等
- **支持值绑定**：在匹配时可以声明变量
- **可作为表达式**：可以返回值并赋值给变量
- **显式贯穿**：使用 `next` 关键字可以继续执行下一个 case

---

## 基本语法

### 值匹配

最基本的 switch 形式是匹配常量值：

```ruby
switch expression 
{
    case value1 { statement(s); }
    case value2 { statement(s); }
    case value3 { statement(s); }
    default { statement(s); }
}
```

**示例：**
```ruby
let day = 3;
switch day
{
    case 1 { Debug.Write("星期一"); }
    case 2 { Debug.Write("星期二"); }
    case 3 { Debug.Write("星期三"); }
    case 4 { Debug.Write("星期四"); }
    case 5 { Debug.Write("星期五"); }
    case 6, 7 { Debug.Write("周末"); }  # 多个值可以用逗号分隔
    default { Debug.Write("无效的日期"); }
}
```

---

### 多值匹配

一个 case 可以匹配多个值，用逗号分隔：

```ruby
switch score
{
    case 1, 2, 3 { Debug.Write("低分"); }
    case 4, 5, 6 { Debug.Write("中分"); }
    case 7, 8, 9, 10 { Debug.Write("高分"); }
    default { Debug.Write("无效分数"); }
}
```

---

## 类型匹配

### 类型模式匹配

switch 可以检查值的类型，并根据类型执行不同的代码：

```ruby
switch classObject
{
    case Int32 { statement(); }
    case Int64 { statement(); }
    case String { statement(); }
    default { statement(); }
}
```

---

### 类型匹配与值绑定

在匹配类型的同时，可以声明一个变量来使用该值：

```ruby
switch classObject
{
    case Class1 obj1 
    {
        statement(obj1);  # obj1 是 Class1 类型
    }
    case Class2 obj2 
    {
        statement(obj2);  # obj2 是 Class2 类型
    }
    case Class3 obj3 
    {
        statement(obj3);  # obj3 是 Class3 类型
    }
    default 
    {
        Debug.Write("未知类型");
    }
}
```

---

## 枚举匹配

switch 与枚举配合使用非常强大：

```ruby
# 定义枚举
enum Status
{
    pending = 0;
    processing = 1;
    completed = 2;
    failed = 3;
}

# 匹配枚举值
var currentStatus = Status.processing;
switch currentStatus
{
    case Status.pending { Debug.Write("等待中"); }
    case Status.processing { Debug.Write("处理中"); }
    case Status.completed { Debug.Write("已完成"); }
    case Status.failed { Debug.Write("失败"); }
}
```

### 枚举值绑定

```ruby
enum Book
{
    name = "Book Title";
    mut price = 20;
}
Book.price = 30;   #必须有mut标签，才可以使用赋值 否则报错
let book = Book.price;
switch book
{
    case Book.name n {
        Debug.Write("书名: @n");
    }
    case Book.price p {
        Debug.Write("价格: @p");
    }
}
```

---

## Switch 表达式（返回值）

switch 可以作为表达式使用，返回一个值：

```ruby
let result = switch value
{
    case 1 { tr 10; }      # 使用 tr 返回值
    case 2 { tr 20; }
    case 3 { tr 30; }
    default { tr 0; }
}
# result 将根据 value 被赋值为 10, 20, 30 或 0
```

**完整示例：**
```ruby
let grade = 85;
let level = switch grade
{
    case 90..100 { tr "优秀"; }
    case 80..89 { tr "良好"; }
    case 70..79 { tr "中等"; }
    case 60..69 { tr "及格"; }
    default { tr "不及格"; }
}
Debug.Write("等级: @level");
```

---

## 贯穿行为（Fallthrough）

### 默认无贯穿

与 C/C++ 不同，S 语言的 switch 默认不会贯穿到下一个 case，执行完一个 case 后自动退出。

### 显式贯穿 - next 关键字

如果需要继续执行下一个 case，使用 `next` 关键字：

```ruby
let x = 0;
switch value
{
    case 1 
    { 
        x = 1; 
        Debug.Write("匹配 1");
        next;  # 继续执行下一个 case
    }
    case 2 
    { 
        x = 2; 
        Debug.Write("匹配 2");
    }
    default 
    { 
        Debug.Write("默认");
    }
}
```

---

## 复合匹配示例

```ruby
switch classObject
{
    case BaseClass base
    {
        Debug.Write("这是基类");
        next;  # 继续检查是否是子类
    }
    case ChildClass1 child1
    {
        Debug.Write("这是子类1: @child1.name");
    }
    case ChildClass2 child2
    {
        Debug.Write("这是子类2: @child2.name");
    }
    default
    {
        Debug.Write("未知类型");
    }
}
```

---

## 语法规则总结

### 必须遵循的规则：

1. **表达式类型**：switch 中的 expression 可以是整型、枚举类型、类类型等
2. **Case 数量**：可以有任意数量的 case 语句
3. **Case 语法**：每个 case 后跟匹配模式，然后是 `{}` 包裹的语句块
4. **无自动贯穿**：默认情况下，执行完一个 case 后自动退出
5. **显式贯穿**：使用 `next` 关键字可以继续执行下一个 case
6. **Default 子句**：可选的 `default` 用于处理所有未匹配的情况
7. **表达式模式**：使用 `tr value;` 返回值，使 switch 可以作为表达式使用

### 最佳实践：

- 使用多值匹配简化代码
- 合理使用 `default` 处理边界情况
- 类型匹配时使用值绑定获得类型安全
- 仅在必要时使用 `next` 实现贯穿
- 使用 switch 表达式让代码更简洁

---

## 完整示例

```ruby
import CSharp.System;

ProjectEnter
{
    # 定义基类
    ClassBase
    {
        public name = "Base";
    }
    
    # 定义子类
    ClassChild1 extend ClassBase
    {
        public name = "Child1";
    }
    
    ClassChild2 extend ClassBase
    {
        public name = "Child2";
    }
    
    # 定义枚举
    enum Book
    {
        name = "DefaultBook";
        price = 20;
    }
    
    static Main(int a)
    {
        # 示例 1: 值匹配与返回值
        x = 0;
        ok = switch a
        {
            case 1 { 
                x = 1; 
                tr 100; 
            }
            case 2 { 
                x = 2; 
                tr 200; 
            }
            case 3 { 
                x = 3; 
                tr 300; 
                next;  # 继续执行下一个 case
            }
            case 4, 5, 6 { 
                x = 10; 
                tr 1000; 
            }
            default { 
                x = 100; 
                tr -1; 
            }
        }
        Debug.Write("x = @x, ok = @ok");
        
        
        # 示例 2: 类型匹配与值绑定
        ClassBase cb = ClassChild1();
        switch cb
        {
            case ClassBase base {
                Debug.Write("匹配到基类: @base.name");
                next;  # 继续检查是否是更具体的子类
            }
            case ClassChild1 c1 {
                Debug.Write("匹配到子类1: @c1.name");
            }
            case ClassChild2 c2 {
                Debug.Write("匹配到子类2: @c2.name");
            }
            default {
                Debug.Write("未知类型");
            }
        }
        
        
        # 示例 3: 枚举匹配
        Book b = Book.price(30);
        switch b
        {
            case Book.name n {
                Debug.Write("书名: @n");
            }
            case Book.price p {
                Debug.Write("书的价格: @p");
            }
        }
        
        
        # 示例 4: 更复杂的值匹配
        score = 85;
        grade = switch score
        {
            case 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100 { 
                tr "A"; 
            }
            case 80, 81, 82, 83, 84, 85, 86, 87, 88, 89 { 
                tr "B"; 
            }
            case 70, 71, 72, 73, 74, 75, 76, 77, 78, 79 { 
                tr "C"; 
            }
            case 60, 61, 62, 63, 64, 65, 66, 67, 68, 69 { 
                tr "D"; 
            }
            default { 
                tr "F"; 
            }
        }
        Debug.Write("成绩等级: @grade");
    }
}
```

### 输出结果

```
$ projectrun 3
x = 10, ok = 1000
匹配到基类: Base
匹配到子类1: Child1
书的价格: 30
成绩等级: B
```

---

## 与其他语言的对比

| 特性 | S 语言 | Swift | C/C++ |
|------|--------|-------|-------|
| 默认贯穿 | 否 | 否 | 是 |
| 显式贯穿 | `next` | `fallthrough` | 默认行为 |
| 类型匹配 | ✓ | ✓ | ✗ |
| 值绑定 | ✓ | ✓ | ✗ |
| 返回值 | ✓ | ✓ | ✗ |
| 多值匹配 | ✓ | ✓ | ✗ |
| 枚举匹配 | ✓ | ✓ | 部分 |

---

## 注意事项

1. **类型安全**：使用类型匹配时，编译器会确保类型安全
2. **穷尽性**：建议添加 `default` 子句以处理所有可能的情况
3. **性能**：switch 通常比多个 if-else 语句性能更好
4. **可读性**：使用 switch 可以让代码更清晰、更易维护
5. **返回值**：当作为表达式使用时，所有分支都应该返回相同类型的值     




