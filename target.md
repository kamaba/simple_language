3

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
  在函数后可加附加执行语句 如  [release]func<x,y>(a,b).[auto]x<tt>(xx);        表示立马释放掉函数中的内存分配    ？
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
21. 在类前如果声明 [@TypeSerizable] 如  Vec3{ x = 0.0f; y = 0.0f; z = 0.0f; } [TypeSerizable] Ligth{ float inetnsity = 1.0f; Vec3 position; } 则 使用 Light( 2.0f, Vec3( 1.0f, 2.0f, 3.0f ) ); 则初始化的方式，可以使用 类型传入，序列化的方式，初始化值。 当然，内部如果有了这种方法，则提示，与[TypeSerizable]冲突,相当于，使用[TypeS]标签，会自动生成一个__Init( float, Vector3 )的初始化函数。 当然，也可以使用{}再补后边的操作. 还是先执行{}里边的，然后再执行_init_中的.
22. 风格化统一功能， 将 新建类，统一成  c = Class( 20, 30 ){ a = 10, b = 30 }; c = Class(); c.a = 10; c.b = 30; c.Init( 20, 30 ); 的方式去处理!!,像表达式中，如果支持++，则可以 使用，如果不支持，则统一成 i = i+1;的方式， 使用代码中，有多重表达方式的，最终统一成一个方式去处理， 无论，书写，还是继承读代码，都比较容易理解。
23. 模仿rust使用生命周期机制，但一般申名后，可以 使用自动化管理，也可以使用手动管理生命周期。
24. 导出类使用export方式导出，如果所有元素，都直接不过滤， 如果有过滤，则要写名参数，导出类，可以确定导出类，是否能新建对象，还是只允许使用对象的静态方式，在导出前，可以确定导出的一些初始化工作。导出类，必须使用工具，打包成一个全体，然后在export中定义过，才可以使用。
25. 增加data关键字，为了直接定义数据，例 data Book{ name = "ILOVEU"; desc = "this is a lover book"; rent = { manager = "X"; time = "2012-10-12"} author=["Lif", "Paper", "Mango"] buy = [{time="2022-12-12"; price=25;},{time="2021-10-10"; price = 20;}, {time="2020-12-20"; price=22; sex = 1 } ] } 数据只能使用定义数据结构用，，可以使用{}[]和普通的赋值语句， 当然也可以和class 互转  Book.Clone(); 是复制结构。 Book.ToClass(); 生成匿名类， 当然class也可以ToData()的方式，转成data数据。 data也可以直接转为Json, Xml, yaml等格式化，data 不允许有任何函数，只能通过外部修改值。 data 一般在栈上生成, 有点相当于结构体，但不允许有方法，在定义时，已经确定了数据类型，如果有类的传入，例 class bookDesc{ paperCount = 20; characterSize = 1000000; } data Book{ desc = bookDesc(); },支持类的传入，会计算类的长度，然后自动填充到desc上边。 在改的时候，是否可以传父类，可以，但该类的指引还是原来的类类型，只不过，后边的补充数据，为默认。
26. 指针的支持，在一些接触底层环境中，支持指针，但表现形式与普通想通，例 :   c = Class1();   IntPtr<Class1> cp = c.ToIntPtr();  cp.a = 20;
27. 注释部分 如果在任务单位前边加 #[function("null无返回值", "elementId 用来传入网页的节点Id", "传入需要导出的根层级"), desc("该函数用来转换html5格式,其它使用到全局xx来控制...")] 来标注函数的注释 
28. @[]用来扩展节点的属性功能， 例 @Condition("Debug"), @Display="Heelo"
29. 使用.sc来架接c语言，并且支持内置c语言的编译.
30. 可选配模块，如果选配，直接可以使用  float2, float3, float4, float5-12, float2x2,
float3x3, float2x3, float3x3, float3x4, float4x3, float4x4, matrix3x3 matrix16的快捷计算，如果选配该模块后，支持相关的数据方法， mod(float2), step(float2), min( float2 ), complex, 等数学上常用类型的表达
31. 每个基础元素上，可以扩展一个byte 0-8位的标记位, 和一个扩展tag位，可配置， 这两个，通过项目，可以配置，如果配置后，相当于，每个对象上数据，都相应的扩展 byte,(int,long,?)其它位，如果使用后，则对可以在任意地方使用该元素， 尤其在遍历，map,list等方法时，如果标记后，则写统一方式可以删除，极大的提高了删除方法的写作方法快捷度。
32. 对象最少有一个byte伴，点位，在对象管理池中，有class方法的扩展数据，有class.id,class.mem.config 包括占用内存大小，和内存排列方式，引用数量，等。
33. 项目支持，接口是否可以使用，继承方法是否可以使用，如果继承方法不可以使用，则子类中的方法，不允许继承
34. 格式化字符使用 Println( "@3 : @1 = @2", 1, 2, 3 );的方式进行定位并且打印!!
35. 多线程使用，需要独立关键字在函数名称前边，sync 如果通信，支持通道的建立，通道，只在多线程中，传输数据使用。
36. 异常处理，在{}内使用 if exception{} else{}的方式处理异常 不能自定义异常区间段只能使用{}如果前边定义过label则和class.function.label一块显示。不管使用那种方式，都有收集异常的处理中心，收集后
可以对异常进行管理，并且和exception联动使用， 如果没有定义exception 则异常甚至可以走异常处理过程。在工程中有对应的接口处理。 如果函数调用，则同时可以使用exception( ok{}, err{} 方式处理 ), 例:
a = Convert.Int("e").Exception( {}, { a = 0; } ); 当然在Convert.Int函数中，函数的最外层 即fun(){}配合的 if exception{ } else { }的方式 如果没有定义，则走系统的exception方法.
37. 确定封号，是否强制使用。
38. 有专门的反射类，使用在 Instance.Create( type, params );里边。
39. 全局情引入值的使用，在sp中，可以定义全局值，在普通文件中，可以读取。如果有改动全局值，一般在EnterProject.SetGlobal( "", "" ) 实现。在普通文件中使用的话，可以使用global.varname使用。 在EnterProject.InitGlobal() 中，进行全局值的初始化。 如果定义了global,必须在项目配置中，enableGlobal的字段，需要是true,否则会提示没有打开关键字，global字段失效。并且可以设置全局变量是否属性寄存器区,以便更快的访问。
40. env参数，在os或者是system类中。
41. 增加使用 byte,sbyte,char,short,int,long,string 各 s_temp1 全局参数1-n 个 形式， byte[], sbyte[], char[], short[], int[], long[], string[] 1-n个，形式应该为 g_byte[]
42. Table的类型，主要用来储存表格数据，可以与 object[][], object[][][] 互转， 一般直接读取excel,cvs 后，直接得到的是table类型的数据， 属性扩展容器库。  支持数据格式有, json, xml,yaml,cvs, excel 如果并下S.模下块，表明已入官方库，正常情况S字段，不允许其它乱用。
43. label标签与{} 能联合使用 label s1{}
44. 通过 asm51{} 的区域划分方式，或者是 asm_adm64_x86{} 方式，可执行对应的汇编内容，在编译的时候，只有这种输出这种环境的时候，才会执行该汇编语句。 文件不可以用.s 而用 .asm   如果是和c混编需要 用例 .sc的方式  c纯使用.c的方式，并且可以在.sp中配置.c的编译过程和参数。
45. 数据类型没有真正的占用内容，如果在函数中构建，则返回时，是返回的数据，而不是引用，如果使用了引用，则只能引用全局引用，因为使用完后就被释放了。
46. instance可以通过 @instance 或者 [@instance,@other]的方式，进行使用静态instance, 当然初始化顺序也可以通过在config中配置 instanceSort进行调整。
47. 可以设置最多继承层数，外来插件可以继承层数。
48. 暂定，不支持模板函数，而传入Type类，进行逻辑判断， 只针对Cast<T>() 方法，进行单独处理。 当函数已定义类型，参数未定义类型，如果是常规类型如Int32,Float等，  赋值相当于调用 .ToInt32()  .ToFloat()语句  如果是自定义类型，  其实也是.Cast<Int32>() 的方式，但发现这个后转到.ToInt32() 则相当于调用   inputparam.Cast<DefineClass>()的方式去处理
49.通过函数方式申请手动管理 居部管理等
50.初始化数据必须是可序列化数据引用 在data内部不允许new class
51. webassemyly的方式，通过编译成wasm直接在浏览器中，运行编译过程，相当于 只要有浏览器，就有编译代码的能力，而且浏览器绑定后，可以模拟任何环境运行。


语言细节:
2. 默认变量不可以使用 外部引入变量，只能使用const 或者是 常量
3. 定义变量不能于本类及父类的名称一样，否则报错。


近期目标:
1. 编译过程，代码化，过程化，有提示，有报错，有修改意见，有自动修复
2. 写基础类int32,int16, string,char 
4. 其它语言在本代码中写
7. 空间的分配与管理
8. 可以调用大部分csharp类
3. 解释成c# 中间语言 或者是 javascript语言

1. 内部构建的代码与重写代码的不冲突
2. 在内部，可以有自由的数据结构，相当于如果内部定义数据，在外边的.s中，不允许再定义数据，只可以写函数
3. 在内部构建后，函数可以绑定内部静态函数调用，相当于，可以有部分，通过csharp代码处理。
4. 写完int,float,string, range,array的方法相关
5. 带T的，新要建立一个TemplateMetaClass 然后 在建立的时候，先查找是否是模版类，如果是，再去模版类中匹配，找到相应的模版类。
6. Array的建立，可以通过T,建立一个真正的数组，然后分数组维度，然后再建立自己的空间。


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
6. 在sp中，可以引入全局import 


默认类:
byte,sbyte,char,short,ushort,int,uint,long,ulong,string
array,list,set,dict

首先支持:
1. 函数直接使用，在语句中
2. 函数的基本类重载，然后查找方法类也重载，直接通过int32的系统方式直接查找
3. 通过.s文件，重写，int32,ing64,string等类，如果重载后，直接走.s文件中的





