# 语言的命名空间 
<b>命名空间</b>设计目 的，是让类的分类更清晰，并且，具体查找导航作用，类名称与不同层级的命名空间使用相同的名称不冲突，但在一个层级内不允许。

—————————————————————————————————————————————————————————

## 命名空间的使用
1. 命名空间，直接在 `.sl` 源码中声明：用 `namespace X { ... }` 关键字形式（可嵌套），也可以用「限定名 + `{}`」形式直接声明命名空间下的类（如 `Application.Core.UI.Layout { ... }`），无需在工程配置中预先登记。工程 `.jsonc` 的 `struct[]`（命名空间树）用于工程组织与校验登记：

命名空间登记实例（jsonc `struct[]`）:
```javascript
file:test.jsonc

"struct": [
    { "namespace": "Std", "children":
        [
            { "namespace": "Collection", "children": [] },
            { "namespace": "IO", "children": [] }
        ]
    },
    { "namespace": "Application", "children": [] }
]
```
2. 在空间中使用,在源码中声明命名空间后，需要在使用import 命名空间名称.命名空间子名称;的方式引入, 也可以使用as 关键字，进行命名空间名称替换，防止引导冲突。
3. 在引导类时，也可以根据全局命名空间引导使用，如果使用外部模块，则要使用模块的名称，再使用命名空间的名称

命名类定义使用实例:
```csharp
file:class1.s

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
    static _main_()
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

## import 导入失败的处理（2026-10-13 起为硬错误）

自 2026-10-13 起，**import 解析失败不再静默放行**：一律按 **Error 级**报错（LID 20377-20382，pass=FALSE 计入 errorCount），MetaCore 阶段失败后 **IR/Export 阶段跳过、不导出 module.json**——不会在下游产生「方法未发现」之类的连锁迷惑错误，最早的 import 错误即根因（排障只看最早 Error）。

常见失败场景与提示含义：

| 错误提示（Info 摘要） | 触发写法示例 | 排查方向 |
|------|------|------|
| Error import 字符串中没有命名空间名称 | `import "Std:";` | 字符串形式（模块:命名空间）冒号后必须是非空命名空间 |
| 没有找到 import 模块: | `import "Missing:Foo";` | 模块未在工程 `.jsonc` 的 `references` 中声明，或未完成编译导出（其 `export.outputDir` 下无 `*.module.json`） |
| import 命名空间路径解析失败，没有找到对应成员 | `import MissingModule.Foo;` | 路径逐段拼写有误；若首段是模块名，须在 `references` 中声明并完成编译导出 |
| import 语句没有可解析的命名空间目标 | `import ;` | import 目标为空，检查语句语法 |

排查要点：

- **跨模块 import 的前提**：目标模块必须已在当前工程 `.jsonc` 的 `references` 中声明，并且**先于当前工程完成编译导出**（`out/export/<模块>/<模块>.module.json` 已生成）。
- 引用模块的成员树来自其 **.sl 实际声明**（SLIR 包内容），而非其 jsonc `struct[]` 骨架——路径按实际声明写（同下文「模块别名」关键规则）。
- 「没有找到 import 模块」日志的 Tips 会直接给出 references 声明与编译导出两步指引；若不确定目标模块是否导出过，先看其 `out/export/<模块>/Logs/Front.txt` 是否有 `export module success`。
- 历史遗留的死导入（所指命名空间并不存在、仅因旧版静默放行而残留）在严格化后会直接编译失败，应删除（如 Core 库 Float32/Float64 曾残留的 `import CSharp.System;`）。

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