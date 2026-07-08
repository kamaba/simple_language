# attribute（属性 / 注解）

用于模拟 CLR 的 `Attribute` 或 Java 的 `Annotation`：在**类声明**、**成员变量**、**成员函数**上附加“元数据标记”，供编译器/运行时/工具链读取。

---

## 语法

统一采用调用形式：

```sl
@extendsAttribute(Param1, param2)
```

- `@extendsAttribute` 是 Attribute 的类型名/标记名。
- `(...)` 参数列表可为空。
- 参数表达式语法与普通函数调用一致（字符串、数字、bool、标识符、表达式等）。

---

## 声明位置

Attribute 写在被修饰的声明**之前**，允许多个：

### 修饰类

```sl
@Serializable()
@DisplayName("Player")
class Player
{
}
```

### 修饰成员变量

```sl
class Player
{
    @Range(0, 100)
    hp = 100
}
```

### 修饰成员函数

```sl
class Player
{
    @Obsolete("Use NewMove instead")
    Move(x, y)
    {
    }
}
```

---

## 定义 Attribute 类

自定义 Attribute 需要继承内置基类 `Attribute`，语法与普通类继承相同，使用 `extends` 关键字。

### 基本结构

```sl
MyAttr extends Attribute
{
    // 成员变量（可选，用于保存参数）
    string label = ""

    // 接收参数的构造函数
    _init_( string name )
    {
        this.label = name
    }
}
```

> `Attribute` 是语言内置基类，无需 import，所有自定义 Attribute 类必须 `extends Attribute`。

---

## 生命周期方法

Attribute 类支持两种特殊的 `_init_` 重载，分别在不同时机被调用：

### `_init_( 参数列表 )` — 接收使用时的参数

在 `@MyAttr(...)` 处直接传入的参数会触发该重载。

```sl
Nickname extends Attribute
{
    string firstName = ""
    string fullPath  = ""

    _init_( string name, string path )
    {
        this.firstName = name
        this.fullPath  = path
    }
}

// 使用：两个参数对应 _init_(string, string)
@Nickname("玩家", "Game.Player")
Player
{
}
```

### `_init_( metaType type )` — 接收被修饰的类型

当 Attribute 需要在编译期感知被修饰的类/成员的类型信息时，定义此重载；运行时/编译器会自动将目标类型注入。

```sl
TypeLogger extends Attribute
{
    _init_( metaType type )
    {
        #type 即被 @TypeLogger 修饰的类的元类型
        # 可在此执行编译期反射、注册等操作
    }
}

@TypeLogger()
MyService
{
}
```

### `void construct` — 无参默认构造（可选）

若 Attribute 不需要任何参数，可以只保留空体；`void construct` 是明确声明的无参构造块，与 `_init_()` 等价，也可不写。

```sl
MarkerAttr extends Attribute
{
    // 无参数，也可以不写任何内容
}

@MarkerAttr()
SomeClass
{
}
```

---

## 完整示例

```sl
import System;

// 定义：接收一个或多个字符串别名
Nickname extends Attribute
{
    string[] names = null

    _init_( string[] nameList )
    {
        names = nameList
    }
}

// 定义：无参标记型
Deprecated extends Attribute
{
}

// 使用：修饰类
@Nickname("Player", "Game.Player")
@Deprecated()
Character
{
    // 使用：修饰成员变量
    @Nickname("生命值")
    int hp = 100

    // 使用：修饰成员函数
    @Deprecated()
    Move( int x, int y )
    {
    }
}
```

---

## 约定与限制（Front 层解析阶段）

- Attribute 只负责"挂载元数据"，不直接改变语义。
- 是否产生额外语义（如序列化、反射、AOT 导出等）由后续编译阶段/运行时决定。
- `_init_( 参数 )` 与 `_init_( metaType type )` 可同时定义，编译器根据调用形式选择匹配的重载。
- Attribute 类本身不能再被 Attribute 修饰（不支持元元数据叠加）。

