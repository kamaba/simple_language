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
- label 后边增加的标签不允许在同一函数中有重复，否则编译不通过（LID 12281：`标签[{0}]在同一函数中重复定义!!`）。
- goto 后边的标签必须在函数中已定义，否则会编译报错（LID 12280：`使用goto跳转，标签[{0}]未在本函数中定义!!`）。
- goto 后只允许 `goto 标签名;` 的形式（LID 10300）。
- 除 `label 名字;` 声明式外，label 还支持块形式 `label 名字 { }`（配合 catch/finally 做异常捕获，见 [try.md](./try.md)）；函数内裸块 `{}` 已不支持（LID 12282）。

#### 实例
```python
Project
{
    _main_()
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
        global.println( a );
        a++;

    }
}
```

执行以上代码，输出结果为（`a = 30`，`a > 35` 不成立，goto 未跳转）:
```
30
```




