# S语言的宏替换功能

语言的宏替换功能，其实是在编译前，对逻辑代码，进行了置换，但里边只对关键字，或者是语句进行替换。

—————————————————————————————————————————————————————————

## 项目的宏替换
1. 需要在工程配置中，配置要替换的原语句，和替换成的语句。
2. 在工程中直接使用$xxx进行调用替换。



创建后的工程文件:
```python
file:test_project.sp

import CSharp.System;
Class1
{

}
ProjectEnter
{
    static Main()
    {
        $print( "hello world1---" );
        $println( "hello world 2" );
        $println("hello world3" );
        c1 = new Class1();    #初始化对象
        love true
        {
            $println("love = true ");
        }
        i = 1;
        循环 i 大于等于 0 {
            $println("i??" );
            i--;
        }
    }
    static Test()
    {
        
    }
}


ProjectConfigBegin{
    "name":"test project",
    "desc":"这是一个测试用例",
    globalReplace #全局宏替换
    {
        print = "Console.Write";
        println = "Console.WriteLine";
        "new (x)" = "(x)";
        love = "if";  #关键字进行替换 关键字替换的新字，不能与现有的关键字相同
        "循环" = "while";
        "大于等于" = ">=";
    }
    
}ProjectConfigEnd
```

## 输入结果
hello world1---hello world 2

hello world3

love = true 

i??
