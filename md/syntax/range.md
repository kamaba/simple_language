# SLang 的范围
范围是一个用来存储数字区间的结构，如果配置了该结构，则可以拥有数字的起始与结束区间，并且可以定义他遍历时候的跨度，更快速的遍历，但不占用太多空间。

—————————————————————————————————————————————————————————


## 范围的定义 
范围定义，可以有几种方式，可以使用 **Range\<T>** 的方式，也可以使用 **range()** 小写构造糖的方式，也可以1..100的方式确定为范围的形式，一但确定范围后，就可以进行范围相关的操作。
（Range 定义于 Core 库 `source/Front/Lib/Core/Range.sl`，泛型参数受 `Range<T:Num>` 数值类型约束。）

```ruby 
Project
{
   _main_()
   {
      r1 = 1..100;    #快速int range  以后再支持 1..n   n..200的方式    相当于   range( 1, 100, 1 )的调用        
      r2 = range( 1.0f, 200.0f, 1.0f );
      Range<double> r3 = new(3.2d, 54.3d, 0.22d );
      r4 = Range<short>( 1s, 100s, 2s );

      #r2/r3/r4 的步进通过构造第三个参数指定（如 r4 步进为 2s）；
      #Range 没有 step()/SetStep 方法，步进只能在构造时给出

      for v in r1   
      {
         global.println("value=$v");
      }

      a1 = r1.toArray();   #转化成数组：遍历区间物化为 Array<int>，结果带缓存（首次遍历写入 _toArrayCache 复用）
   }
}
```

## 范围的一些注意事项
- 判断某值是否包含在该范围当中：`r1.isContain( 2 );`（注意是 `isContain`，不是 `IsIn`，见 `Range.sl`）
- Range 没有独立的 index 游标属性；内部游标 `_iteratorValue` 只在迭代推进时使用
- 在 for a in arr1 中，是对数组的迭代；迭代器自带的 index/value 属性当前未实现（同 for-in，见 [forwhiledowhile.md](./forwhiledowhile.md)）
- Range 转 Array 使用 `toArray()`（带缓存）；原文档"用 Cast 与 array 互转"的说法已过时——`Range.sl` 中没有 Cast 方法

