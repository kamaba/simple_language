# SLang 的范围
范围是一个用来存储数字区间的结构，如果配置了该结构，则可以拥有数字的起始与结束区间，并且可以定义他遍历时候的跨度，更快速的遍历，但不占用太多空间。

—————————————————————————————————————————————————————————


## 范围的定义 
范围定义，可以有几种方式，可以使用 ** Range\<T>** 的方式，也可以使用 **range()** 的方式，也可以1..100的方式确定为范围的形式，一但确定范围后，就可以进行范围相关的操作。

```ruby 
import CSharp.System;
ProjectEnter
{
   static Main()
   {
      r1 = 1..100;    #快速int range  以后再支持 1..n   n..200的方式    相当于   range( 1, 100, 1 )的调用        
      r2 = range( 1.0f, 200.0f, 1.0f );
      Range<double> r3 = (3.2d, 54.3d, 0.22d );
      r4 = Range<short>( 1s, 100s, 2s );

      r1.SetStep( 1 )   #设置r1的步进
      r4.step = 2       #设置r4的步进 与SetStep方法一样
      for v in r1   
      {
         CSharp.System.Console.Write("value=$v");
      }

      a1 = r1.Cast<Array<int>>();   #转化成数组，并且申请成数组的内存空间 相当于遍历，然后给数组赋值
   }
}
```

## 范围的一些注意事项
- 或者是 r1.IsIn( 2 ); 是否包含在该范围当中
- 范围有当前游标的概念， r1 = 1..100  r1.index = 0
- 在for a in arr1 中，给是数组的迭代，当迭代时，a 有默认的索引值index
- 范围一般可以和array可以互转，使用Cast就能实现。

