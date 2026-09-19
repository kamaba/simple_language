## 提交规范
### 每个PR只针对一项内容的改进或修复，请勿合并提交。

### PR标题尽量选择英文，备注内容可选中文。

## 对齐规范
Tab键对齐（可以在VS里设置）

## 命名规范
 [规则1-1] 英文单词命名。禁止使用拼音或无意义的字母命名。

[规则1-2] 直观易懂。使用能够描述其功能或有意义的英文单词或词组。

[规则1-3] 不要采用下划线命名法。
```csharp
int car_type //错误：下划线命名。 
```

[规则1-4] 常量、静态字段、类、结构体、非私有字段、方法等名称采用大驼峰式命名法
```csharp
public const float MaxSpeed = 100f; //常量
public static float MaxSpeed = 100f; //静态字段
public class GameClass; //类
public struct GameStruct; //结构体
public string firstName; //public字段
protected string m_FirstName; //protected字段
public void SendMessage(string message) {} //方法
```

[规则1-5] 私有字段、方法形参、局部变量采用 小驼峰式命名法

注意：私有字段以下划线开头
```csharp
private string m_FirstName; //私有字段
public void FindByFirstName(string firstName) {} //方法参数
string m_FirstName; //局部变量
```

[规则1-6] 接口命名

注意：接口以大写字母I开头
```csharp
public interface IState; //接口
```

[规则1-7] 枚举命名

注意：枚举以大写字母E开头
```csharp
public enum EGameType {Simple, Hard}//枚举及枚举值
```

编码规范
[规则2-1] 声明变量时，一行只声明一个变量。
```csharp
private string _firstName;
private string _lastName;
```

[规则2-2] 类的字段声明统一放置于类的最前端。
```csharp
public class Student 
{
    private string m_FirstName;
    private string m_LastName;

    public string GetFirstName() 
    {
        return "";
    }
}
```

[规则2-3] 一行代码长度不要超过屏幕宽度。如果超过了，将超过部分换行。

注释规范
[规则3-1] 公共方法注释，采用 /// 形式自动产生XML标签格式的注释。包括方法介绍，参数含义，返回内容。

注意：私有方法可以不用注释。

```csharp
/// <summary>
/// 设置场景名称
/// </summary>
/// <param name="sceneName">场景名</param>
/// <returns>如果设置成功返回True</returns>
public bool SetSceneName(string sceneName)
{
}
```

[规则3-2] 公共字段注释，采用 /// 形式自动产生XML标签格式的注释。

注意：私有字段可以不用注释。
```csharp
public class SceneManager
{
    /// <summary>
    /// 场景的名字
    /// </summary>
    public string SceneName;
}
```

[规则3-3] 私有字段注释，注释位于代码后面，中间Space键隔开。
```csharp
public class Student
{
    private string m_FirstName; //姓氏
    private string m_LastName; //姓名
}
```

[规则3-4] 方法内的代码块注释。
```csharp
public void UpdateHost
{
    // 和服务器通信
    ...
        
    // 检测通信结果
    ...
        
    // 分析数据
    ...
}
```

关键字使用规范

关键字的完整清单与语法糖总表见 [syntax/keywords.md](./syntax/keywords.md)，此处只列编码时的硬性约束。

[规则4-1] 禁止使用关键字作标识符（变量名、方法名、类名、参数名等）。
关键字包括：`typealias` `import` `as` `is` `isnot` `namespace` `class` `extends` `enum` `data` `dynamic` `void` `abstract` `interface` `extern` `bind` `label` `public` `protected` `private` `const` `mut` `final` `static` `partial` `override` `operator` `params` `tr` `if` `elif` `else` `while` `dowhile` `for` `in` `out` `switch` `case` `default` `next` `continue` `break` `goto` `ret` `try` `catch` `finally` `throw` `throws` `defer` `errdefer` `checked` `unchecked` `new` `var` `this` `base` `null` `true` `false` `get` `set` `function` `await` `spawn` `yield`，以及类型词 `object` `byte` `sbyte` `short` `ushort` `int` `uint` `long` `ulong` `bool` `half` `float` `double` `string`。

[规则4-2] `get` / `set` 是属性访问器关键字，禁止用作成员名和方法名（词法阶段即报错）。

[规则4-3] 小写容器构造糖名（`map` `list` `stack` `hashset` `queue` `tuple` `array` `range`）与 Result 保留名（`error` `errmsg`）不是词法关键字，可作局部变量名/参数名；但**禁止用作类成员声明名**（字段/方法/getter/setter，MetaCore 层编译报错 LID 11042）。`async` 不是保留字，按普通标识符处理。

[规则4-4] 循环跳出统一用 `continue` / `break`；`next` 只在 switch case 体内表示显式贯穿（fall-through），不要在循环内用 `next` 代替 `continue`。

[规则4-5] 优先使用语法糖保持简洁：`ret` 代替 `return`、`a ?? b` 代替判空取值、`a?.member` 代替判空访问、`$var` / `${expr}` 插值代替字符串拼接。同一文件内风格保持一致。

[规则4-6] 协程中使用 `await` / `spawn` / `yield` 语法糖，不要手写等价的 `Coroutine.awaitTask(...)` / `Coroutine.spawnClosureN(...)` / `Coroutine.yieldNow()` 调用。

Core 库使用规范

工程引用 Core（jsonc `references` 指向 `out/export/Core`）后，Core 模块根下的类型**裸短名直接使用，无需 import**。完整类型清单见 [syntax/keywords.md](./syntax/keywords.md) §4。

[规则5-1] Core 类型直接使用短名，不要写 `Core.` 前缀，也不要重复 import。
```sl
List<Int32> list = new List<Int32>();    // 正确：直接使用
Core.List<Int32> list = new Core.List<Int32>();  // 不推荐：冗余前缀
```

[规则5-2] 类型声明优先使用语言类型词（`int`、`float`、`string`…），与 Core 类型类等价；数值精度有明确要求时使用类名（`Int32`、`Float64`…）。

[规则5-3] `Core.Environment` 与 `Core.Environment.Platform` 下的类型需用限定名 `Environment.env`（配 `import Core;`）或全限定 `Core.Environment.env`，不能裸用 `env`。

[规则5-4] 引用非 Core 模块（Std / Math / Tensora 等）的类型时，短名必须先 `import`，否则使用限定名（如 `Std.Console.X`）。

[规则5-5] 禁止使用 Core 内部私有类（`_` 下划线开头的实现类，如 `_MapStream<T>`）；业务代码只使用公开类型。

[规则5-6] 使用 Core 内注册的别名时保持原样：`coro`（= `Coroutine`）、`Float8_E4M3`（= `Float8`），不要自行再造别名。