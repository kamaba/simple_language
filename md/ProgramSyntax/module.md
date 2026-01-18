# S语言的模块
SLang在工程中，如果使用导出dll方式，则视为导出的一个模块，一个模块，可以挂载到另一个工程中，
—————————————————————————————————————————————————————————

## 模块的定义
类的定义没有具体的关键字，只要在一个module的空间中，或者是file文件中最外层，或者是namespace的{}中，也可以在某个类中，直接定义类，下面是类定义的一般形式
```
in the file mycore1.s
class_name  #这里就是类的定义
{
    #member variables
    <access specifier> <data type> variable1;
    <access specifier> <data type> variable2;

    variable1 = const value or class define;

    #member methods
    <access specifier > <return type> method1( parameter list )
    {
        //method body
    }
}

```
### 请注意:
- 访问标识符 <access specifier> 指定了对类及其成员的访问规则。如果没有指定，则使用默认的访问标识的，默认访问符都为public
- 数据类型 <data type> 指定了变量的类型，返回类型 <return type> 指定了返回的方法返回的数据类型。该返回类型是可以不写的，但必须函数可以计算出他的返回类型。
- 如果要访问类的成员，你要使用点（.）运算符。
- 点运算符链接了对象的名称和成员的名称。

### 建立对象的几种方式
1. ClassName.New()  该方式使用类名为静态函数使用，在ClassName是使用某个类，他继承着父类中的New的静态函数，该静态函数像一个工厂一样，通过适配器，自动匹配类中的 下边的例子说明

```javascript
Class1
{
    Class1( p1 )
    {
        m1 = p1;
    }
    m1 = 10;
}

c1 = Class1( 20 );          
 ```

2. ClassName c = { };   该方式自动创建为ClassName的类，并且通过{}中，可能为ClassName中的元素赋值,但必须使用前置的ClassName来确定类型，否则的话，视为匿名对象，在使用的时候，虽然可以通过转化来

3. ClassName c; 该方式自动创建为ClassName的对象，即便不使用.New()函数，仍然可以创建一个ClassName的对象，如果不想让它自动创建，可以使用ClassName c = null; 来确定默认值为空对象