# S语言的条件控制

## Switch表达式

一个 switch 语句允许测试一个变量等于多个值时的情况。每个值称为一个 case，且被测试的变量会对每个 switch case 进行检查。

下面是大多数编程语言中典型的判断结构的一般形式：


### 语法
#### S 中 switch 语句的常量匹配语法：

```ruby 
switch expression 
{
    case constant-express{ statement(s); }
    case constant-express{ statement(s); next; }
    default{ statement(s);}
}
```

#### S 中 switch 语句的类型匹配语法：
```ruby
switch classObject.Type()
{
    case Int32
    {
        statement();
    }
    case Int64
    {
        statement();
    }
}
```

#### S 中 switch 语句的类型带增加变量匹配语法：
```ruby
switch classObject
{
    case Class2 defaultVariable2
    {
        statement( defaultVariable2 );
    }
    case Class3 defaultVariable3
    {
        statement(defaultVariable3);
    }
}
```

#### S 中 switch 语句与返回语句结合：
```ruby
a = switch expression
{
    case constant-express{ statement(s); tr 20; }
    case constant-express{ statement(s); tr 30; }
}
```

### switch 语句必须遵循下面的规则：
- switch 语句中的 expression 必须是一个整型或枚举类型，或者是一个 class 类型，其中 class 有一个单一的转换函数将其转换为整型或枚举类型。
- 在一个 switch 中可以有任意数量的 case 语句。每个 case 后跟一个要比较的值在后边要跟一个{}。
- case 的 constant-expression 必须与 switch 中的变量具有相同的数据类型，且必须是一个常量。
- 当被测试的变量等于 case 中的常量时，case 后跟的语句将被执行，直到整个switch被执行完后，如果要继续进行后续的检查，需要增加next;的语法，让语句继续执行。
- 该switch不像传统带break模式，break;
- 一个 switch 语句可以有一个可选的 default 语句，在 switch 的结尾。default 语句用于在上面所有 case 都不为 true 时执行的一个任务。default 也需要包含 break 语句，这是一个良好的习惯。
- s 不支持从一个 case 标签显式贯穿到另一个 case 标签。如果要使 s 支持从一个 case 标签显式贯穿到另一个 case 标签，可以使用 goto 一个 switch-case 或 goto default。
- S 中，可以与返回值语句结合使用，但需要使用tr value; 的语法结合，相当于 a = value;的语法。

#### 实例
```ruby
ProjectEnter
{
    ClassBase
    {

    }
    ClassChild1 :: ClassBase
    {

    }
    ClassChild2 :: ClassBase
    {

    }
    static static Main( int a )
    {
        x = 0;
        ok = switch a
        {
            case 1{ x = 1; }
            case 2{ x = 2; tr 3; }
            case 3{ x = 3; tr 4; next; }
            case 4,5,6{ x = 10; tr 10; }
            default{ x = 100; tr 20; }}
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



