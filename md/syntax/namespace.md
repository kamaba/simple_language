# 语言的命名空间 
<b>命名空间</b>设计目 的，是让类的分类更清晰，并且，具体查找导航作用，类名称与不同层级的命名空间使用相同的名称不冲突，但在一个层级内不允许。

—————————————————————————————————————————————————————————

## 命名空间的使用
1. 命名空间，的使用，在项目配置中分三种模式
    - 全部在工程内部定义，包括命名空间，类。 该模式下在其它文件内部不允许再次定义命名空间，及类相关，有使用选项 LimitUseNamespaceAndClass
    - 全在工程内部定义命名空间，但类，可以自由定义，该模式下，其它文件内部，不允许 定义命名空间，只允许定义类相关内容，使用选项LimitUseNamespace
    - 可以在工程内部定义，但不限在文件中定义，该模式下，可以在工程内部定义，也可以在文件中随意定义命名空间，及类相关内部，使用选项

需要在<b><i> sp </b></i>工程定义中，先预定好一些路径，才可以在代码中，引用使用 例: sp中  namespace[ "Application.Core", "Appcalition.UI", "Application.Logic", "Application.Logic.Net" ] 如果没有定义，则在工程中使用，出现错误报错!!
2. 在空间中使用,在工程配置中定义命名空间后，需要在使用import 命名空间名称.命名空间子名称;的方式引入, 也可以使用as 关键字，进行命名空间名称替换，防止引导冲突。
3. 在引导类时，也可以根据全局命名空间引导使用，如果使用外部模块，则要使用模块的名称，再使用命名空间的名称

命名空间配置实例:
```javascript
file:test.sp


const data ProjectConfig
{
    name = "Test1";
    desc = "这是一个测试用例";    
    proojectStruct =          #命名空间设计
    {
        Std =
        {
            type = "namespace"
            child = 
            {
                Collection =
                {
                    type = "namespace"
                }
                Math =
                {
                    type = "class"
                }
                Layer1_1 =
                {
                    type = "namespace"
                    child ={
                        Layer2_1 =
                        {
                            type="namespace"
                        }
                        Layer2_2 =
                        {
                            type="namespace"
                        }
                    }
                }
                Layer1_1 =
                {
                    type = "namespace"
                }
            }
        } 
    }
}

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
        Console.print("width=$this.width ,height=$this.height " );
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
                Console.print("Core1Class");
            }
        }
    }
    UtilClass
    {
        static PrintUtilClass()
        {
            Console.print("UtilClass");
        }
    }
    HelloClass
    {
        static PrintHello()
        {
            Console.print("Hello");
        }
    }
}
Core.MyClass
{
    static PrintMyClass()
    {
        Console.print("MyClass");
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

—————————————————————————————————————————————————————————

## import as（导入别名）三种形态

`import ... as` 为导入目标建立本地别名，别名可用于**表达式前缀链**（`别名.X.Y`）与**类型位置前缀**（`别名.X obj`）。支持三种形态：

| 形态 | 语法 | 别名指向 |
|------|------|----------|
| 命名空间别名 | `import NS.a as NSA` | 目标命名空间节点 |
| 模块别名 | `import 模块名 as S` | 引用模块的**模块根** |
| 字符串 import 别名 | `import "模块:命名空间" as IOx` | 引用模块内的指定命名空间 |

### 1. 命名空间别名：`import NS.a as NSA`

别名指向目标命名空间，后续路径从该命名空间继续续解，任意深度均可：

```sl
import AliasNS.Sub as SUB     # AliasNS.Sub 为本工程已声明的命名空间

SUB.Alpha a = SUB.Alpha( 5 )           # 等价 AliasNS.Sub.Alpha( 5 )
a.sid                                  # 等价 AliasNS.Sub.Alpha 的静态成员
SUB.Alpha.twice( 21 )                  # 等价 AliasNS.Sub.Alpha.twice( 21 )
```

### 2. 模块别名：`import 模块名 as S`

别名指向引用模块的**模块根**，后续路径按"模块根成员 → 命名空间成员"两段续解（表达式与类型位置均可用）：

```sl
import Std as S

S.Console.println( "..." )              # Console 注册在 Std 模块根
S.FileMode fm = S.FileMode.Create       # 类型位置 + enum 成员
S.FileStream sfs = null                 # 类型位置
S.IO.Directory.exists( "." )            # 四段链：模块别名 → IO 命名空间 → 类 → 静态方法
```

**关键规则**：续解的成员来自引用模块的 **.sl 实际声明**（SLIR 编译产物包内容），而非引用工程 jsonc `struct[]` 的骨架声明：

- 无命名空间声明的类型（如 Console / FileMode / FileStream）注册在**模块根** → 写 `S.FileMode`
- 带命名空间限定声明的类型（如 `enum IO.SearchOption`、`public class IO.Directory`）→ 写 `S.IO.SearchOption`

### 3. 字符串 import 别名：`import "模块:命名空间" as 别名`

直接把别名指向引用模块内的命名空间，省去模块名前缀：

```sl
import "Std:IO" as IOx

IOx.Directory.exists( "." )                    # 等价 Std.IO.Directory.exists
IOx.SearchOption.TopDirectoryOnly.toString()   # enum 成员
typealias LibIOxSearch = IOx.SearchOption       # typealias 目标前缀（类型位置，见 typealias.md）
```

### 与 typealias 配合

typealias 的目标类型可以以 import 别名作为路径前缀（三种别名形态均可），详见 [typealias.md](typealias.md)：

```sl
typealias LibSFileMode = S.FileMode
typealias LibIOxSearch = IOx.SearchOption
typealias LibAliasAlpha = SUB.Alpha
```

### 验证用例

以上全部形态在探针工程 `test/Other/AliasProbe/`（AliasProbe.sp + AliasProbeLib.sl + AliasProbe.jsonc）中覆盖：Front 编译零 Error，C VM（csimple_lang）运行输出全部正确。