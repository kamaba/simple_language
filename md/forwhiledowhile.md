# S语言的循环语句

## 循环表达式
循环语句可以使用在 遍历数组或者迭代表达式，也可以使用条件语句，进行循环执行, 分三种 分别为: for, while, dowhile

-------------------------

### for的语法
1. 使用条件判断式去执行循环 即，for 初始化, 执行约束条件， 变量变化{} 这三个语句是顺序化的，但可以依次从后往前不写。
2. 使用 for in 语句，一般是迭代数组或者是迭代表达式去执行， for 迭代变量 in 数组/迭代表达式{} 这个语句不可以省略任何组成部分。

for条件示例
```ruby 
for init_express, end_condition_express, change_variable_express
{
    #语句
}
```
其中init_express, end_condition_express, change_variable_express都是可选，但顺序不能变，只能从后边向前省略，与函数传参类似。

for的遍历示例
```python
Array arr = {1,2,3,4};
for v in arr
{
    #语句
}
```

break,continue的使用
```python
for v in 1..10
{
    if v < 4
    {
        #输入小于v说明
        continue;
    }
    if v == 8
    {
        break;
    }
    #输出当前遍历值与它的索引
    Console.WriteLine( "v=@v index=@v.index ");
}
```
上边示例中，v是遍历的迭代器， 使用v则，直接识别当前v的值，如果使用v.index则表达，当前的索引值,
如果v小于4 则使用continue,在continue之前的语句执行，后边的则跳过，如果v == 8则break，相当于跳出循环执行。

### for 语句必须遵循下面的规则：
- for{}语句，可以配合break/continue等使用
- for 后边的表达示都为可选表达式，但如果省略只能从后往前依次略，一般结束条件可用break结束。
- for in表达式，可以对enum,和迭代表达式配合使用，必须迭代变量，自带一些属性index,是当前的索引
- for in表达式，后边的迭代表达式如果使用了自带游标，即 arr.index的变化 例: for v in arr{ a = arr.index; }这个时候，arr.index就是它的游标，游标这种情况是只读的，在其它情况，可以设置游标值，然后进行读取。
-------
### while的语法
1. 使用while语句执行，相当于每次语句结束后，通过while判断是否返回的是布尔表达式，如果是true,则继续执行
2. 使用dowhile语句和while类似，但判断条件是首次跳过判断，而在执行后进行判断布尔表达式，如果true则继续执行。

while示例
```python 
while boolean_express
{
    #语句
}
```
dowhile示例
```python
dowhile boolean_express
{
    #语句
}
```
### while/dowhile 语句必须遵循下面的规则：
- while/dowhile 后边只允许布尔类型的呈现
- while/dowhile中，可与break/continue配合使用。
------


#### 实例
```ruby
import CSharp.System;

const enum ArrEnum
{
    a = 1;
    b = 2;
    c = 3;
}
ProjectEnter
{
    static Main()
    {
        #for enum
        for v in ArrEnum
        {
            Console.WriteLine("V=@v");
        }
        #输入结果
        #>>v=a
        #>>v=b
        #>>v=c

        #for 数组
        for v in 1..3
        {
            Console.WriteLine("v=@v index=@v.index");
        }
        #输入结果
        #>>v=1 index=0
        #>>v=2 index=1
        #>>v=3 index=2
        Array<string> arrString = {"a","b","c","d","e"};
        for v in arrString
        {
            if v.index > 4
            {
                break;
            }
            if v.index == 0 
            {
                arrString[v.index] = "mc";
                Console.WriteLine("v[0]=mc");
                continue;
            }
            if v == "b" 
            {
                Console.WriteLine( "v=@v" );
            }
            Console.WriteLine("index=@v.index");
        }
        #输入结果
        #>>v[0]=mc
        #>>index=0
        #>>v=b
        #>>index=1
        #>>index=2
        #>>index=3
        #>>index=4

        #for 条件
        for i = 0, i < 2, i++
        {
            Console.WriteLine("v=@i");  #条件迭代没有index值
        }
        #输入结果
        #>>v=0
        #>>v=1
        i2 = 10;
        for
        {
            if i2 > 12
            {
                break;
            }
            i2 += 2;
            Console.WriteLine("i2=@i2");
        }
        #输入结果
        #>>i2=10
        #>>i2=12

        #while
        bool flag1 = true;
        while flag1
        {
            Console.WriteLine("while" );
            flag1 = false;
        }
        #输入结果
        #>>while

        #dowhile
        dowhile false
        {
            Console.WriteLine("dowhile");
            #输出一次dowhile
        }
        #输入结果
        #>>dowhile  
    }
}
```


