# 极简语言 [English](https://github.com/kamaba/simple_language)

------------------------------------------------------------------------

### 简介: 极简语言是一门静态语言，它初版是在csharp的基础上写出来的，语法大体与C#有些相似，但又有其它语言的特点，他的工程配置与语言是一体的，所以在使用语言的时候，必须是在工程的基础上。语言分三期功能
- 第一个阶段: 
    1. 语言的前端解析
    2. 语言通过c#的平台的集成库
    3. 拥有自己完整的语言体系
    4. 但暂不支持模版操作
    5. 可以导出IR中间语言，并且在自己内部的虚拟机中运行。
- 第二个阶段：
    1. 语言可以导出c#的IR层，使用Mono或者是.NetCore虚拟机运行导出代码
    2. 并且在内部可以直接调用c#的库或者是c/c++的库，并且快整模块化。
    3. 可以导出javascript等语言，兼容javascript的一些库的执行。
- 第三个阶段: 
    1. 使用llvm中间层，
    2. 未来.netcore 的native功能
    3. 把语言本地化，脱离虚拟机运行，然后使用llvm转化，可以正常语言一些，直接打包，链接，运行。


### 语言的特色
1. 写法较为简单，无强制格式化行为，更多使用大括号来代表代码段。
2. 注释支持多层嵌套，并且支持markdown注释



### 语言的宗旨
1. 可读性较强
2. 可写性较强
3. 轻度的语法糖，一定要建立在1，2的基础之上。
4. 纯面向对象的语言。
5. 轻度使用继承，接口，不允许有重名变量.

### 语言初体验
```csharp
file:test.sp

import CSharp.System;

DemoClass
{
    a = 0i;
    b = 100i;

    __Init__( int _a, int _b )
    {
        this.a = _a;
        this.b = _b * 2;
    }

    Add()
    {
        return this.a + this.b;
    }
    PrintAddRes()
    {
        # 如果a=10 b=100的话 输入 a[10]+b[200]=210
        Console.Write( "a[@this.a]+b[@this.b]=" + Add().ToString() );
    }
}

ProjectEnter
{
    static Main()
    {    
        DC = DemoClass(10, 100);
        DC.PrintAddRes();
    }
    static Test()
    {
    }
}
```

### 语法说明
1. [命名空间](https://github.com/kamaba/simple_language/md/namespace.md)
2. 基本语法(https://github.com/kamaba/simple_language/md/base.md)
3. 类型转换
4. 变量
5. 运算符
6. 判断
7. 循环
8. 封装
9. 方法
10. [字符串](https://github.com/kamaba/simple_language/md/string.md)
11. [枚举](https://github.com/kamaba/simple_language/md/enum.md)
12. [类](https://github.com/kamaba/simple_language/md/class.md)
13. [对象](https://github.com/kamaba/simple_language/md/object.md)
14. 继承
15. 接口

### 支持平台

### 安装使用


