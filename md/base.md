# 语言的基础使用
语言是项目与代码一体化管理的，所以创建项目，然后配置，再运行，配置项目，可以参考[配置项目](), 代码可以写到项目文件中，也可以分开写入自定义文件，然后在工程配置中，引用后，并且在函数入口运行。

—————————————————————————————————————————————————————————

## 项目的创建
1. 进入编译器后，使用new sproject [工程名称],创建项目文件。
3. 使用new sfile [代码文件名称] 创建代码文件
3. 在执行指令目录，查找[工程名称.sp]文件，并且打开，这里边，file可以配置引入刚才使用[代码文件名称.s]的文件，当然这个是个可选的过程，在执行一些简单指令，完全可以在sp工程中，写入并且执行。
4. 在ProjectEnter 的 Test中，或者是Main中写入代码。
5. 在目录入，执行run 工程名称.sp -test 执行Test中的代码 不带参数，则执行Main中的代码。



创建的test.s 文件
```csharp
import CSharp.System;

Rectangle
{
    length = 0.0f;
    float width = 0.0f;
    _init_( float _l, float _w )
    {
        this.length = _l;
        this.width = _w;
    }
    DefaultLW()
    {
        this.length = 100.0f;
        this.width = 100.0f;
    }
    public float GetArea()
    {
        return this.length * this.width;
    }
    Display()
    {
        Console.WriteLine("Length: @this.length" );
        Console.WriteLine("Width: @this.width" );
        Console.WriteLine("Area: " + GetArea() );
    }
}

```

创建后的工程文件:
```python
file:test_project.sp

ProjectEnter
{
    static Main()
    {    
        r = Rectangle(10.0f, 20.0f );
        r.Display();

    }
    static Test()
    {
        r = Rectangle();
        r.DefaultLW();
        r.Display();
    }
}


const data ProjectConfig{
    name = "test project";
    desc = "这是一个测试用例";
    compileFileList =
    [ 
        {
            path = "test.s";
        }
    ]
}
```

#### 执行的代码:
```bash
run test_project.sp 
执行结果: Length: 10
Width: 20
Area: 200
run -test test_project.sp 
执行结果: Length: 100
Width: 100
Area: 10000
```

### 类的定义
在S语言中，定义类，可选使用class class 类名称{}, 也可以不使用class 只需要 类名称{},即为定义类

### 变量的定义
在S语言中，可使用 变量名称 = 默认值， 具体默认值如何识别，可以去[数字]()或[字符串]()或[类]()查看


### 注释的使用
1. 可以使用 #注释内容 的方式，进行注释
2. 也可以使用 #!  注释内容 #! 进行注释
3. 注释，可以嵌套多套， 如下代码区，如果去掉###! !###这个对称注释后，里边的注释继续可以使用，并且不破坏结构。
```python
###! 
内容3_1
    ##!
    内容2_1_1  
        #! 
        内容1 
        !# 
    内容2_1_2 

    ##!
    ##!
        内容2_2_1    #单选注释
    !## 
!###
```
4. 注释，可以默认识别markdown内容， 使用方式 #!md 注释markdown内容 #!md

#!md
## 这个标题2号
### 这个标题3号
- 使用.符

!#md


### 标识符
标识符是用来识别类、变量、函数或任何其它用户定义的项目。在 S 中，命名空间，变量，函数名称等自定义命名必须遵循如下基本规则：

- 标识符必须以字母、下划线或 @ 开头，后面可以跟一系列的字母、数字（ 0 - 9 ）、下划线（ _ ）、@。
- 标识符中的第一个字符不能是数字。
- 标识符必须不包含任何嵌入的空格或符号，比如 ? - +! # % ^ & * ( ) [ ] { } . ; : " ' / \。
- 标识符不能是 S语言 关键字。
- 标识符必须区分大小写。大写字母和小写字母被认为是不同的字母。
- 不能与S语言的类库名称相同。

### 关键字

<table border="1">
<tr>
<td>namespace</td>
<td>import</td>
<td>as</td> 
<td>class</td><td>partial</td> 
<td>enum</td>
<td>data</td>
<td>public</td><td>internal</td> <td> projected</td><td>private</td> 
</tr>

<tr>
<td>interface</td>
<td>override</td>
<td>const</td><td> final</td>
<td>static</td>
<td>get</td><td>set</td> 
<td>label</td><td>goto</td> 
<td>break</td><td>continue</td>
</tr>

<tr>
<td>if</td><td>elif</td><td>else</td>
<td>switch</td><td>case</td><td>default</td><td>next</td>
<td>while</td><td>dowhile</td><td>for</td> <td> in</td>
</tr>

<tr>
<td>byte</td> <td> sbyte </td><td>char</td> <td> short </td><td>ushort</td> <td> int</td><td>uint</td> <td> ulong</td>
<td> object</td><td>null</td><td>bool</td>
</tr>

<tr>
<td>string</td>
<td>void</td> 
<td>true</td>
<td>false</td>
<td>this</td>
<td>base</td>
<td>tr</td><td>ret</td>
<td>is</td>
</table>




