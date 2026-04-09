# S 语言的类 
当你定义一个类时，你定义了一个数据类型的集合。这实际上并没有定义任何的数据，但它定义了类的名称意味着什么，也就是说，类的对象由什么组成及在这个对象上可执行什么操作。对象是类的实例。构成类的方法和变量称为类的成员。

—————————————————————————————————————————————————————————

## 类的定义

类定义用于声明对象的结构（字段）与行为（方法）。S 语言允许在顶层、`namespace` 内或其它类中嵌套定义类。

语法示例：

```s
// 简写形式
MyClass {
    // fields
    Int32 x = 0;
    String name = "";

    // constructor
    _init_(Int32 a) { this.x = a; }

    // instance method
    Int32 getX() { ret this.x; }

    // static method
    static Int32 Zero() { ret 0; }
}

// 或显式使用 class 关键字
class OtherClass { }
```
### 访问控制与继承

- 访问修饰符：`public` / `private` / `internal` / `projected` 等用于控制成员可见性；默认成员为 `public`。
- 继承使用 `extends`：`class Child extends Parent {}`。
- `final` 修饰类或成员，表示禁止继承或重写。

示例：

```s
namespace Application {
    MyClass {
        MyChildClass { }
    }
}

final Application.MyClass2 { }

// 使用
var amc = Application.MyClass();
var amcc = Application.MyClass.MyChildClass();
```

```python
namespace Application
{
    MyClass       #这类的位置在Application.MyClass
    {
        MyChildClass
        {

        }
    }
}
final Application.MyClass2   #不允许继承
{
    
}
MyClass3
{
    
}


```

file: test.sp
```python
ProjectEnter
{
    static Main()
    {  
        amc = Application.MyClass();
        amcc = Application.MyClass.MyChildClass();
        amc2 = Application.MyClass2();
    }
    static Test()
    {
    }
}
```