# S语言的变量， 主要用来存储某个类型的存储的名称表示，对象内部变量，对象静态变量，全局变量，函数内临时变量。

## 对象变量
1. 非静态变量，在定义对象后，在申请类型，获取对象后，该对象自动集成了，定义类的所有非静态变量，通过对象访问。
2. 静态变量，是在类申明时，已定义的变量，可以通过类名称直接访问

### 类/对象变量的语法
```ruby 

Class0
{
    Variable0 = 0;
}

Class1 :: Class0
{
    Variable1 = 0i;
    Variable2 = "abcd";
    static Variable3 = 2.0f;
}

Main()
{
    Class1 c;
    v0 = c.Variable0;   #继承与父类的非静态成员，在子类中可以访问。
    c.Variable1 = 20;  #改变c对象内部Variable1的值，Variable1，即c的内部变量
    string x = c.Variable2;  #c的Variable2引用。

    float xx = Class1.Variable3; #静态变量需要，使用[类名.静态名称]的读取。
}
```
规则: 在使用类的变量时，不允许有任何重名，包括父类与子类中的名称， 如果父类定义了name,则在子类中不允再重新定义该名称，所以在定义的时候，尽量只歧义的名称， 在新建对象时，子类对象会继承父类对象的非静态成员，并且计算大小。 静态成员，只能通过原类名.静态成员名称的方式访问。

------------------
## 全局变量 是指通过ProjectConfig配置，然后在代码中通过global关键字，直接访问

# 类/对象变量的语法

```python
file:test_project.sp

ProjectEnter
{
    static Main()
    {    
        pi = global.pi;
        h1 = global.minInt;
    }
    static SetMaxInt( int a )
    {
        global.maxInt = a;
    }
}


const data ProjectConfig{
    name = "test project";
    globalVariable
    {
        pi = 3.1415f;
        minInt = -1i;
        maxInt = 10000i;
    }
}
```

### 规则，在全局变量使用中，只有在globalVariable定义后，代码中才可以引用，不允许其它方式创建，在设置global变量时，只能在Project函数中，或者是DllExpore中，或者是Compile中进行设置，不允许在其它类中进行设置。

## 函数内变量 指在函数内定义的临时变量，可以通过{}的区域，定义变量的使用范围。
# 函数内变量的语法

```python
file:test_project.sp

ProjectEnter
{
    static Main()
    {    
        var1 = 20;
        {
            var2 = "aaa";
        }
        var1 = 30;
    }
}


const data ProjectConfig{
    name = "test project";
    globalVariable
    {
        pi = 3.1415f;
        minInt = -1i;
        maxInt = 10000i;
    }
}
```

### 规则，在函数每个{}区间对应的变量的范围，如果超出范围，不能使用该变量， 在函数变量定义中，不允许有重复名称，如果重复名称，则一般会报错。