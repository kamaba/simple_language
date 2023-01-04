# 语言的命名空间 
<b>命名空间</b>设计目 的，是让类的分类更清晰，并且，具体查找导航作用，类名称与不同层级的命名空间使用相同的名称不冲突，但在一个层级内不允许。

—————————————————————————————————————————————————————————

## 命名空间的使用
1. 命名空间，需要在<b><i> sp </b></i>工程定义中，先预定好一些路径，才可以在代码中，引用使用 例: sp中  namespace[ "Application.Core", "Appcalition.UI", "Application.Logic", "Application.Logic.Net" ] 如果没有定义，则在工程中使用，出现错误报错!!
2. 在空间中使用,在工程配置中定义命名空间后，需要在使用import 命名空间名称.命名空间子名称;的方式引入, 也可以使用as 关键字，进行命名空间名称替换，防止引导冲突。
3. 在引导类时，也可以根据全局命名空间引导使用，如果使用外部模块，则要使用模块的名称，再使用命名空间的名称

命名空间配置实例:
```csharp
file:test.sp

"globalNamespace":
     [
            "Application",
            "Application.Core",
            "Application.Core.UI",
            "Qt",
            "Qt.Util",
            "Core"
    ]

```

命名类定义使用实例:
```csharp
file:class1.s
import CSharp.System;

Application.Core.UI.Layout
{
    width = 0i;
    height = 0i;
    private int count = 0;

    PrintWidthAndHeight()
    {
        Console.Write("width=@this.width ,height=@this.height " );
    }
}
namespace Application
{
    namespace Core
    {
        Core1Class
        {            
            static PrintCore1Class()
            {
                Console.Write("Core1Class");
            }
        }
    }
    UtilClass
    {
        static PrintUtilClass()
        {
            Console.Write("UtilClass");
        }
    }
    HelloClass
    {
        static PrintHello()
        {
            Console.Write("Hello");
        }
    }
}
Core.MyClass
{
    static PrintMyClass()
    {
        Console.Write("MyClass");
    }
}

```

命名空间类引用的实例:
```csharp
file:test.sp

import Application;
import Application.Core as AC;

ProjectEnter
{
    static Main()
    {    
        layout = Application.Core.UI.Layout();
        layout.PrintWidthAndHeight();

        UtilClass.PrintUtilClass();

        Application.HelloClass.PrintHello();

        Core.MyClass.PrintMyClass();  #虽然没有引用，但因为Core是第一层级，会自动查找该命名空间

        AC.Core1Class.PrintCore1Class(); #因为使用了import as 所以该语句相当于 Application.Core.Core1Class.PrintCore1Class();
    }
}
```