# SLang 的数组 
数组是一个存储相同类型元素的固定大小的顺序集合。数组是用来存储数据的集合，通常认为数组是一个同一类型变量的集合。

—————————————————————————————————————————————————————————


## 数组的定义 
数组定义，可以有几种方式，可以使用 **Array\<T>** 的方式，也可以使用 **array()** 的方式，也可以[]的方式确定为数组的形式，一但确定数组后，就可以进行数组相关的操作，数组如果是定义，一般不申请空间，只有对数组长度定义后，是真正的申请内存空间操作，数组申请数组后，只能通过SetLength方式，重置空间长度。

```ruby 
import CSharp.System;
class ArrNodeClass
{
   int i = 0;
}

ProjectEnter
{
   static Main()
   {
      a1 = Array<int>(3); #申请一个int型，长度为3的数组
      a2 = [1,2,3,4,5];    #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  相当于Array(5, int.runtimeType ){1,2,3,4,5}
      a3 = Array<float>(5){1,2,3,4,5.0f};   #默认int array 没有任何定义时，看属性是否相同，如果相同则决定该数组类型  相当于Array<float>(5){1.0,2.0,3.0,4.0,5.0}        
      a4 = array<object>( 20 );               # 长度为20的Array<object>
             
      int[] a5 = [1.2,1.3,1.5];    #通过int[] 决定后边是否与配置一样，不一样时，使用提示，否则使用强制转换如果类型不一样 相当于 Array<float>{ 1.2, 1.3, 1.5};
        
      a6 = ["aa", 1, "232", 1.0f];  # 相当于Array<Object>( "aa", 1, "232", 1.0f, XC() );
        
      # c# 的方法  ArrNodeClass[] arr2 = new ArrNodeClass[100]; 这里边使用的是 arr2 = ArrClass2[100];
      float[] a7 = Array<float>(){ 1.2, 2.2, 3.4 };  #   
        
      Array<float> a8 = Array<float>( 20 ){1,2,3,5,3.3};   #申请一个长度为20的数组
       
      a9 = Array<int>( 27 ); #申请一个二维数组，边界分别为 int[9,3]
      
      bb2 = Array<int>( 100 ){ 1,2,3,4,5 };    # 等于 Array<int>( 100, 10 )  {1,2,3,4,5};        
             
      int[] bb3 = [1,2,3,4,5 ];    #与上相同  Array<int>(5){ 1,2,3,4,5}
    }
}
```
## 数组的的使用
数组的使用，在定义数组后，数组一般都具有遍历和对数组下标操作的快捷方法，当然也有一些内部方法，具体可以查看array相关的API获取更多。

```ruby 
import CSharp.System;
class ArrNodeClass
{
   int i = 0;
}

ProjectEnter
{
   static Main()
   {
      arr1 = Array<ArrNodeClass>(30);     #申请一个该类型的数组对象，但长度为0

      arr1[1] = { i = 20 };   #直接设置数组下标为1的内容
      arr1.$0 = ArrNodeClass();
      arr1.$0.instValue();             #实体化$0,相当于 arr1[0] = ArrNodeClass();
      arr1.$0.instValue().i = 10;         #使用$值下标，进行值置换，如果$0为空，则需要先实体化对象才可      以，设置，否则会有空对象报错, 可以调用instValue进行初始化，如果已有内容，则不进行new对象
      arr1[1000].i = 10000; # 在编译时，处理是否有超过长度现象，如果有的话，则编译不通过
      
      int i11 = 11;
      arr1.$i11.i = 10;    #动态设置数组为i11的变量，值为11下标的数组内容，相当于 arr1[11].i = 10;
       
      arr1.SetValue( 20, ArrNodeClass(){i=20} );
        
      arr1.RemoveIndex( 2 );       #删除数据内容，长度不缩短，必须调用ReCalcLength()才可以长度缩短，但下标映射关系是变了       
      arr1.Remove( arr1[20] );     #同上，只是把这个节点置空，长度没变化，下标有变化。
      
      arr1.index = 20;     #数组的当前游标
      arr1.value.i = 10;   #数组当前游标的植
        
      for a in arr1      # 使用 `for in` 迭代，`a` 是迭代封装类型，包含 `index` 与 `value`
      {
         if a.index == 20   # 系统自带 Index() 属性
         {
            a.value = ArrNodeClass(){ i = 100 }
            continue
         }
         a.value.i = 200
      }

      for (a in [1,2,3,4])
      {
         i = a.index + 1         # 遍历时，a.index 返回当前项索引；注意不要在用户代码中命名字段为 `index` 或 `value`（保留字）
      }

      for i = 0, i < arr1.length, i++
      {
         if i < 40
         {
            continue
         }
         arr1[i] = ArrNodeClass()
         arr1[i].i = 100
         arr1.$i.i = 120  # 等价于 arr1[i].i = 120

         i += 2;
      }
    }
}
```
## 数组的一些注意事项
- 或者是 arr.isIn( 2 ); 是否包含在该数组当中
- 数组有当前游标的概念， Array arr1 = Array(14);  arr1.index = 0;  给游标赋值   arr1.value = 20; 相当于 arr1[arr1.index] = 20; 即给当前游标赋值
- 在for a in arr1 中，给是数组的迭代，当迭代时，a 有默认的索引值index
- 如果是数组或者其它容器，可以使用 arr1.$0 = 10; 来表示值，效果与 arr1[0] = 10; 一样
- 使用for循环时，如果使用了in 则，使数组如果没有变量先创建变量，如果有，则看变量是否是可遍历对象，如果是，则创建一个变量或者是搜索，已有的it变量类型，如果是则使用， in前边的变量其实不是直接的对象，而是包装了一层it，里边包含了那个对象
- 数组的使用，其实是已经分配好的空间，传T只是为了识别他的长度使用的，里边其实是不含T的，只有返回的时候有个强转的作用。
- 数组的参数，只有一个，就是数组的长度，正常情况，使用[1,2,3,4]的方式，给其传默认值  当然也可以写作 int[1,2,3,4,5]
- 数组一般可以和range可以互转，使用Cast就能实现。

## 数组赋值与协变（Meta 语义）

自 Meta 层收紧规则后，**引用类型数组默认不协变**：不能把 `Int32[]` 直接赋给 `Object[]`（`ObjectArray` / `Array<Object>`），也不能把 `Int32[][]` 整表赋给 `Object[][]`，否则会按类型不一致处理。需要时请**显式构造** `Object[]` 再装箱元素，或对锯齿数组逐行赋值。

**数值类型（`Num` 抽象）的受控例外**（实现见 `ClassManager` / `MetaType.IsIterator`）：

1. **`IIterator<Num> ← Array<具体数值>`**  
   左值为抽象数值迭代器、右值为元素为具体数值类型（`Int32`、`Float32`、`Int64` 等，且非抽象 `Num` 本身）的数组时，允许赋值。语义是**只读遍历**（通过迭代器访问），不表示可把右值当作 `Array<Num>` 随意改写元素类型。

2. **`const Array<Num> ← Array<具体数值>`**  
   仅当**目标变量为 const**（例如类成员带 `const`）且左侧元素类型为抽象 `Num` 时，允许用具体数值数组初始化；用于从 `Object` 等位置解包后的通用数值缓冲等场景。

其他泛型数组（如 `Array<某类>` 与 `Array<子类>`）仍要求**类型结构完全一致**（不变性），不可用继承关系放宽。

详见测试：`test/BaseTest/ArrayTest.sl` 中 `arrayCovariantAndLiteralForInTest`、`arrayNumberIteratorFromConcreteArrayTest`、`arrayJagged2DAssignTest`。

