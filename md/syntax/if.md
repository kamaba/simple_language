# S语言的条件控制

## if表达式

判断结构要求程序员指定一个或多个要评估或测试的条件，以及条件为真时要执行的语句（必需的）和条件为假时要执行的语句（可选的）。

下面是大多数编程语言中典型的判断结构的一般形式：



## SLang 中的判断语句
SLang 提供了以下类型的判断语句。点击链接查看每个语句的细节。

一个 if 语句 由一个布尔表达式后跟一个或多个语句组成。

## 语法
### S 中 if 语句的语法：
```s
if boolean_expression {
   // 如果布尔表达式为真将执行的语句
}
```

如果布尔表达式为 true，则执行 if 块内的代码。

#### SLang 中 if...else 语句的语法：
```ruby 
if boolean_expression
{
   #如果布尔表达式为真将执行的语句 
}
else
{
   #如果布尔表达式为假将执行的语句
}
```
如果布尔表达式为 true，则执行 if 块内的代码。如果布尔表达式为 false，则执行 else 块内的代码


### SLang 中 if...elif 语句的语法：
```ruby 
if boolean_expression1
{
   #如果布尔表达式1为真将执行的语句 
}
elif boolean_expression2 
{
   #如果布尔表达式2为假将执行的语句
}
elif boolean_expression3
{
   #如果布尔表达式3为假将执行的语句
}
else
{
     #以上都不符合，则执行该段内容
}
```
如果boolean_expression1表达式为 true，则执行 if 块内的代码。如果boolean_expression2表达式为true 则执行elif( boolean_expression2 )后边的内容,后续可以跟多个，一但符合布尔条件，则执行。

### SLang中使用next字段
在if语句与elif多级联用时，如果elif执行通过后，想继承再检查后边的elif可以在语句中使用next字段，可以让下边的语句继承检查
```ruby
if false
{
    #语句1
}
elif true
{
    #语句2
    next
}
elif false
{
    #语句3
}
elif true
{
    #语句4
}
else
{
    #语句5
}
```
在该实例中，我们因为elif true第一个是true执行语句2，但因为有next关键字，下一个elif还会继承检查，但语句3的条件是false,则再检查下一个elif，发现该个elif为true,则执行语句4


### 实例
```ruby
import CSharp.System;
ProjectEnter
{
    static Main( x )
    {
        if x > 1
        {
            Console.print("执行x>1的内容");
        }
        elif x > 3
        {
            Console.print("执行x>3的内容");
        }
        elif x > 14
        {
            Console.print("执行x>14的内容");
        }
        else
        {
            Console.print("执行else的内容");
        }
    }
}
```
$ projectrun 20
执行以上代码，输出结果为:
执行x>1的内容
执行x>3的内容
执行x>14的内容

$ projectrun 0
执行else的内容

## 与 `elif` 链配合的 `next`

在 `if` / `elif` 多级联用时，若在某个已匹配的分支末尾书写 `next`，控制流会继续向下尝试匹配后续 `elif`（而不是像常见语言那样在第一个真分支后必定结束整条链）。典型用法与语义见上文「SLang中使用next字段」示例。

## 计划中或部分实现的扩展（以测试文件中的注释为准）

以下能力在 `test/BaseTest/IfelseTest.sl` 的 `FunIfCondition` 一节以 `#` 注释形式记录设计意图，**默认不应假定已完全实现**：

- **Truthiness**：`if a` 计划等价于 `if a != null`；`if !a` 计划等价于 `if a == null`；对数值/字符串等计划按非零、非空等规则解释。
- **条件表达式内局部返回 `tr`**：形如 `int x = if cond { tr 1 } else { tr 2 }`，用于在分支块内返回值而不离开外层函数（与 `ret` 区分）。
- **三元运算符**：`a ? b : c` 类语法。

实现状态以编译器/VM 报错或发行说明为准；文档仅描述目标语义。

