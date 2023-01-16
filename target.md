

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
21. 在类前如果声明 [TypeSerizable] 如  Vec3{ x = 0.0f; y = 0.0f; z = 0.0f; } [TypeSerizable] Ligth{ float inetnsity = 1.0f; Vec3 position; } 则 使用 Light( 2.0f, Vec3( 1.0f, 2.0f, 3.0f ) ); 则初始化的方式，可以使用 类型传入，序列化的方式，初始化值。 当然，内部如果有了这种方法，则提示，与[TypeSerizable]冲突,相当于，使用[TypeS]标签，会自动生成一个__Init( float, Vector3 )的初始化函数。 当然，也可以使用{}再补后边的操作. 还是先执行{}里边的，然后再执行__Init__中的.
22. 风格化统一功能， 将 新建类，统一成  c = Class( 20, 30 ){ a = 10, b = 30 }; c = Class(); c.a = 10; c.b = 30; c.Init( 20, 30 ); 的方式去处理!!,像表达式中，如果支持++，则可以 使用，如果不支持，则统一成 i = i+1;的方式， 使用代码中，有多重表达方式的，最终统一成一个方式去处理， 无论，书写，还是继承读代码，都比较容易理解。
23. 模仿rust使用生命周期机制，但一般申名后，可以 使用自动化管理，也可以使用手动管理生命周期。
24. 导出类使用export方式导出，如果所有元素，都直接不过滤， 如果有过滤，则要写名参数，导出类，可以确定导出类，是否能新建对象，还是只允许使用对象的静态方式，在导出前，可以确定导出的一些初始化工作。导出类，必须使用工具，打包成一个全体，然后在export中定义过，才可以使用。
25. 增加data关键字，为了直接定义数据，例 data Book{ name = "ILOVEU"; desc = "this is a lover book"; rent = { manager = "X"; time = "2012-10-12"} author=["Lif", "Paper", "Mango"] buy = [{time="2022-12-12"; price=25;},{time="2021-10-10"; price = 20;}, {time="2020-12-20"; price=22; sex = 1 } ] } 数据只能使用定义数据结构用，，可以使用{}[]和普通的赋值语句， 当然也可以和class 互转  Book.Clone(); 是复制结构。 Book.ToClass(); 生成匿名类， 当然class也可以ToData()的方式，转成data数据。 data也可以直接转为Json, Xml, yaml等格式化，data 不允许有任何函数，只能通过外部修改值。 data 一般在栈上生成, 有点相当于结构体，但不允许有方法，在定义时，已经确定了数据类型，如果有类的传入，例 class bookDesc{ paperCount = 20; characterSize = 1000000; } data Book{ desc = bookDesc(); },支持类的传入，会计算类的长度，然后自动填充到desc上边。 在改的时候，是否可以传父类，可以，但该类的指引还是原来的类类型，只不过，后边的补充数据，为默认。
26. 指针的支持，在一些接触底层环境中，支持指针，但表现形式与普通想通，例 :   c = Class1();   IntPtr<Class1> cp = c.ToIntPtr();  cp.a = 20;
27. 注释部分 如果在任务单位前边加 #[function("null无返回值", "elementId 用来传入网页的节点Id", "传入需要导出的根层级"), desc("该函数用来转换html5格式,其它使用到全局xx来控制...")] 来标注函数的注释 
28. @[]用来扩展节点的属性功能， 例 @[Condition("Debug"), Display="Heelo"]



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
6. 在sp中，可以引入全局import 


默认类:

byte,sbyte,char,short,ushort,int,uint,long,ulong,string
array,list,set,dict

首先支持:
1. 函数直接使用，在语句中
2. 函数的基本类重载，然后查找方法类也重载，直接通过int32的系统方式直接查找
3. 通过.s文件，重写，int32,ing64,string等类，如果重载后，直接走.s文件中的




李克成:
1. 合并PlayerController的蓝图  1.14
2. 完成共用类的合并 包括: CitySampleGameInstance, CitySampleGameState, CitySampleGameMode, BP_CitySampleGameInstance, BP_CitySampleGameMode,BP_CitySampleGameState。  1.16-1.17
3. 修改车辆类 CitySampleVehicleBase/BP_Vehicle/BP_VehicleBase_Deformable/BP_VehicleBase_Drivable 1.18-1.20, 1.28
4. 车辆的交互流程 1.29-1.30
5. 后期调整，与ody工程的冲突整理 1.31-2.3

张硕:
1. 合并ACharacter类及蓝图 合并 1.16-1.17
2. 与其它同事的后续联调与角色色类相关的处理 1.18-1.19

左瑞
1. 跑通，整理CitySimple 人物的动画、玩家驾驶、IK方案 1.14、1.16
2. 确定车辆交互：上下车、转向、撞击等动画方案、动作重定向调整 [与叶子亚协同进行] 1.17-1.18
3. 状态机、动画功能制作 1.18-1.20   1.28-1.31
4. 后期调整 2.1-2.3

何家朋:(暂定使用ody方案，如果方案变动，进度再变更)
1. 摄像机切换效果 1.16
2. 摄像机挂载在车上后的旋转问题 1.17
3. 为摄像机增加角度限制 1.18-1.19

鄂金瑞
1. 通过MassAI生成自带Npc 1.16-1.17
2. 通过HUD创建车辆交互UI与controller按键操作 1.18
3. 通过MassAI生成的Npc遵守红绿灯交通规则1.19-1.20
4. 将MassAI生成的Npc替换为项目中的Npc1.28-1.29
5. 将被替换后的Npc符合Npc交互流程1.30-1.31
6. 后期调整 2.1

