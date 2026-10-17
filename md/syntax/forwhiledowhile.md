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

for i = 10
{
    #可选默认定义参数
}
for i = 10, i < 100
{
    #可选默认定义参数
}
for i = 10, i < 100, i+= 2
{
    #全参数
}
for i = 10, i < 100, i++
{
    #全参数
}
#可选
```
其中init_express, end_condition_express, change_variable_express都是可选，但顺序不能变，只能从后边向前省略，与函数传参类似。
> 注意：`for { }`（三段全部省略）当前**不支持**，解析器要求 for 后至少一段表达式（见 `test/BaseTest/ForTest.sl` 尾注「for{} 无条件循环 —— 当前不支持」）。

for的遍历Array示例
```python
Array arr = [1,2,3,5]
for v in arr
{
    #语句
}
```
for的遍历Range示例
```python
range ran = 1..100
for v in ran
{
    #语句
}
```

for的遍历enum示例
```csharp
enum EMusicType{ classic = 1, rocky = 2}
for v in EMusicType
{
    #语句
    Console.print("MusicType=$v")
}
```

break,continue的使用
```python
for v in 1..10
{
    if v < 4
    {
        #输入小于v说明
        continue
    }
    if v == 8
    {
        break
    }
    #输出当前遍历值
    # v是当前遍历的对象，直接识别为当前元素值
    # 注意：for-in 迭代器自带的 index/value 属性当前未实现（解析器中 indexVariable 已注释，
    # 见 test/BaseTest/ForTest.sl 尾注「文档1.2.1: x.index 索引访问 —— 当前未实现」）。
    # 若 v 本身是带 index 游标成员的对象（如 Array，初值 -1，可读写），v.index 访问的是该对象自身的游标。
    global.println( "v=$v");
}
```
上边示例中，v是遍历的迭代值，如果v小于4 则使用continue,在continue之前的语句执行，后边的则跳过，如果v == 8则break，相当于跳出循环执行。

### for 语句必须遵循下面的规则：
- for{}语句，可以配合break/continue等使用
- for 后边的表达示都为可选表达式，但如果省略只能从后往前依次略（三段全省的 `for { }` 暂不支持），一般结束条件可用break结束。
- for in表达式，支持数组、Range（含 `1..100` 快捷写法与 `range()` 函数）与 enum 的遍历；List/Map/Set/Queue/LinkedList 的 for-in 暂不支持（见 `test/BaseTest/ForTest.sl` 尾注）。
- for-in 迭代器自带的 index/value 属性当前未实现（indexVariable 已注释）；若迭代对象自身带 index 游标成员（如 Array），可按成员方式访问。
-------
### while的语法
1. 使用while语句执行，相当于每次语句结束后，通过while判断是否返回的是布尔表达式，如果是true,则继续执行
2. 使用dowhile语句和while类似，但判断条件是首次跳过判断，而在执行后进行判断布尔表达式，如果true则继续执行。

while示例
```python 
while boolean_express
{
    #语句  注意，上边的参数不允许省略
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
enum ArrEnum
{
    a = 1;
    b = 2;
    c = 3;
}
Project
{
    _main_()
    {
        #for enum
        for v in ArrEnum
        {
            global.println("V=" + v.name.toString());
        }
        #输入结果
        #>>V=a
        #>>V=b
        #>>V=c

        #for 数组(range 快捷写法)
        for v in 1..3
        {
            global.println("v=$v");
        }
        #输入结果
        #>>v=1
        #>>v=2
        #>>v=3

        #for 数组(遍历中修改元素/continue/break，索引用局部变量计数)
        arrString = ["a","b","c","d","e"];
        idx = 0
        for v in arrString
        {
            if idx == 0
            {
                arrString[0] = "mc";
                global.println("arrString[0]=mc");
                idx++
                continue;
            }
            if v == "b"
            {
                global.println( "v=$v" );
            }
            global.println("idx=$idx");
            idx++
        }

        #for 条件
        for i = 0, i < 2, i++
        {
            global.println("v=$i");  #条件迭代没有index值
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
            global.println("i2=$i2");
        }
        #输入结果(先自增再输出)
        #>>i2=12
        #>>i2=14

        #while
        bool flag1 = true;
        while flag1
        {
            global.println("while" );
            flag1 = false;
        }
        #输入结果
        #>>while

        #dowhile
        dowhile false
        {
            global.println("dowhile");
            #输出一次dowhile
        }
        #输入结果
        #>>dowhile
    }
}
```


