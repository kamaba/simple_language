# S 语言的类 
当你定义一个类时，你定义了一个数据类型的集合。这实际上并没有定义任何的数据，但它定义了类的名称意味着什么，也就是说，类的对象由什么组成及在这个对象上可执行什么操作。对象是类的实例。构成类的方法和变量称为类的成员。

—————————————————————————————————————————————————————————

## 类的定义 
类的定义没有具体的关键字，只要在一个module的空间中，或者是file文件中最外层，或者是namespace的{}中，也可以在某个类中，直接定义类，下面是类定义的一般形式

in the file mycore1.s
```python
data Book  #定义数据
{
    #member variables
    name = "我的天空";
    desc = "描述不可描述的事情!";
    rent = 
    { 
        manager = "X"; 
        time = "2012-10-12"
    }
    author= ["Lif", "Paper", "Mango"];
    buy = 
    [
        {
            time="2022-12-12"; 
            price=25;
        },
        {
            time="2021-10-10"; 
            price = 20;
        },
        {
            time="2020-12-20"; 
            price=22;
            sex = 1;
        }
    ] 
}
class class_name2    #也可以使用 class关键字
{

}
```
### 类的使用方式
- 访问标识符 [access specifier] 指定了对类及其成员的访问规则。如果没有指定，则使用默认的访问标识的，默认访问符都为public
- 如果要访问类的成员，你要使用点（.）运算符。
- 类的定义，在前边可以使用namespace导航,例: class Application.MyClass{} 的方式，或者使用namespace Application{ MyClass{} }方式来确定类的名称。
- 类中，可以嵌套子类 MyClass{  MyClassChildClass{} } 其实 访问也是通过[.]符访问子类。
- 类可以使用final的方式，不让其它类进行继承。

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