

##

1. 支持翻译成CLR中间语言，直接在.net框架上跑起来
2. 支持与md语言合进代码，注释可以使用md的自定义格式，
    如   #!md 这里写md的注释 !#   注释格式化独有定义格式，可以直接导出文档与md混合使用 
    例:   {"name":"fun1", "isStatic":false, "inputParam":[{"name":"input1","type":"int32"}], "return":{"type":"int32"} }  
    可以在函数体中用[]定义
    例:  []] 当然名称可以直接默认使用函数名称  

  类的关联，在debug中可以显示 ，有树关系，拓扑关系等。
  函数的运行时间统计 在函数前增加[Profile()标签]
  调试类 帧数据记录  帧数据变动表  帧数据运行概念
  类设计 相关 申请1-400字节之间的class，通过在class中，一个分配序列保存，他们之间默认对象信息.
  在函数后可加附加执行语句 如  [release]func<x,y>(a,b).[auto]x<tt>(xx);        表示立马释放掉函数中的内存分配
  如果变量使用 @可以直接读取内存地址，不进行检查  而使用.则以传统方式访问内存 例  a.x.c   而使用  a..x..c &a.&x.&c  a->x->c
  支持51 x86机器运行 以后
  编译解析过程 先读所有要编译文件，然后读取行号与大小，然后进行分类，然后平行N个进度，然后放到多线程中去解析。
3. sl可以解释成js语言   
4. 编译x86平台，编译成c#dll然后调用c#内部相关的逻辑 
5. 编译成51的代码，然后支行在模拟器上跑起来。
7. 编译成x86平台，然后可以支行，或者是使用c为入口，然后做dll调用。代码制作成dll
8. 支持cocos平台的编译，分成可编译与解释型脚本，两种，第二种，使用js解释器
9. 使用sl语言，制作渲染平台，兼容opengl,vukkan,dx12,然后做一套，GUI系统。
10. 兼容vscode
11. 自己的编译器
12. 内部集成js编译器 使用 @js{ 来执行 }
13. 内部集成clr工具，使用@cs{ }来执行 @java{ }
14. 文件分治，使用[partial( "tag1,2,3")]来让文件可以分开写
15. 自己虚拟机系统，建立一个虚拟系统，可以分配硬件与内存管理，还有cpu调动，然后建立一个内部小系统，然后读取文件运行。 如果遇到win32环境，刚考滤，是否换到
系统内部底层跑动。
16. 执行其它语言使用@cs{ var obj = new object(); 的方式来使用}   @js{ var o = {}}  @c{  int a = 20; void* m = malloc(100); } 的方式，来嵌入使用  并且可以通过一些方式往sl中传值 ，如 @cs{ var obj = new object();  SL.Class2.instance.obj = obj.ToSLObject(); }
17. 在访问网页方式  只能通过网页接口方式  前端使用sl变成javascript语言，封闭好，post,get,api接口一类的，统一json格式， 即 <label id="aa"> {{ message}} </label>  @js{ message = sl.server.message; } 
18. 尽量使用静态方式处理调用函数，或者是其它的调用关系，而不像python一样，动态的方式去调用，因为检查不方便。
19. 程序自带一个解释器，建议是轻量级的，集成到代码中，可以通过自定义包体的文件，或者是其它方式，进行前期脚本的一些执行。
20. 内部类，所有的都是开放的，即public 形式， 如果使用模块导出，则要在sp中，定义导出类




语言细节:
1. 当函数已定义类型，参数未定义类型，如果是常规类型如Int32,Float等，  赋值相当于调用 .ToInt32()  .ToFloat()语句  如果是自定义类型，  其实也是.Cast<Int32>() 的方式，但发现这个后转到.ToInt32()
则相当于调用   inputparam.Cast<DefineClass>()的方式去处理
2. 默认变量不可以使用 外部引入变量，只能使用const 或者是 常量
3. 定义变量不能于本类及父类的名称一样，否则报错。


近期目标:
1. 编译过程，代码化，过程化，有提示，有报错，有修改意见，有自动修复
2. 写基础类int32,int16, string,char 
3 解释成c# 中间语言 或者是 javascript语言
4. 其它语言在本代码中写


18. 定义快速取值函数  public string Get(){ return "AA" }  public void Set( string a ){ this.aa = a; } 相当于c# this[] 和 return this[]

# 测试用例完成
1. ifelse State 完成测试  包含 a= if  和 tr 指令
2. forwhiledowhile State 包含 case default 完成测试
3. switch State 完成测试  包含 a= switch  和 tr 指令

二期目标:
1. 使用模版类
2. 使用模版函数
3. 自写List,Tulpe,Map, Set,Link 等操作
4. 定义ClassT< T in [Class1,Class2], T2 in Class3> 使用in [] 或者是in ClassName 来限制模版类
5. 接口的使用，在使用接口的父类中，有interface的必须实现，并且在他的子类中，如果还要实现，也必须使用interface接口，否则在下边的类中会报错!!


S语言的规则
1. 可读性较强
2. 可写性较强
3. 轻度的语法糖，一定要建立在1，2的基础之上。
4. 纯面向对象的语言。
5. 轻度使用继承，接口，不允许有重名变量，

默认类:

byte,sbyte,char,short,ushort,int,uint,long,ulong,string
array,list,set,dict

首先支持:
1. 函数直接使用，在语句中
2. 函数的基本类重载，然后查找方法类也重载，直接通过int32的系统方式直接查找
3. 通过.s文件，重写，int32,ing64,string等类，如果重载后，直接走.s文件中的