# S 语言的标签与跳转

## SLang中的label语句
label语句，是表示在执行流程中，这个标签可以配合像goto,和一些辅助功能的使用。

### label语法
```ruby 
label s1;
int a = 20;
label s2;
int b = 30;
label s3;
```
一般使用label，标签不是全局标签，和命名空间.类.函数.标签的结构使用。但同一函数内，可以直接使用。

## SLang中的goto语句
goto语句，是表示在执行流程中，可以跳到已定义的标签中，改变程序执行流程。

### goto语法
```ruby 
label s1;
int a = 20;
label s2;
int b = 30;
label s3;
if b == 30
{
    b++;
    goto s1;
}
```
一般使用label，标签不是全局标签，和命名空间.类.函数.标签的结构使用。但同一函数内，可以直接使用。

### lable/go 语句必须遵循下面的规则：
- label 后边增加的标签不允许在同一函数中有重复，否则编译不通过。
- goto 后边的标签必须在函数中已定义，否则会编译报错。

#### 实例
```python
import CShyarp.System;
ProjectEnter
{
    static Main()
    {
        label s1;
        a = 20;
        label s2;
        a = 30;
        label s3;
        if( a > 35 )
        {
            goto s2;
        }
        Console.WriteLine( a );
        a++;

    }
}
```

```
$ projectrun 20
执行以上代码，输出结果为:
执行x>1的内容
执行x>3的内容
执行x>14的内容

this is ClassBase

BookPrice [P=30]     




