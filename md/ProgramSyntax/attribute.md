# S 的属性

属性（Attributes / Annotations）

S 语言支持在类型、字段或方法上声明属性（类似 C# 的 attributes 或 Dart 的 metadata），用于标记编译器/运行时行为。

语法示例：

```s
[@NoDefaultConstruction]
class X { }

[@Obsolete("use Y instead")]
fun old() { }
```

说明：
 属性以 `@` 或 `[@Name]` 形式放置在声明之前。
 属性可以带参数（字符串、数字或命名参数），由编译器/运行时读取并决定特殊逻辑。



Class1
{
    @displayName="semws", @des="这是个什么什么函数，用来干什么的", @ret=[], @instance, @condition=[debug,info]
    Fun1()
    {

    }
}