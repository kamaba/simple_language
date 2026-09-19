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

## 内置序列化标签（@Serializable / @SerializeField / @NonSerialized）

标准库内置三个 Attribute 类（定义于 `Core/Attribute.sl`），配合 `Core/IO/Serialize.sl` 的 JSON 直连门面（`Serialize.toJson` / `toJsonPretty` / `fromJson`）控制对象与 JSON 的互转范围：

| 标签 | 修饰对象 | 作用 |
|------|----------|------|
| `@Serializable()` | class / data | 声明该类型按成员展开参与 JSON 序列化 |
| `@SerializeField()` | 成员变量 | 强制 protected/private 成员参与序列化（补票） |
| `@NonSerialized()` | 成员变量 | 将该成员从序列化中淘汰（一票否决，优先级最高） |

### 类型级规则

- `class` 标注 `@Serializable()` 后走**成员展开**（与 `data` 同款，逐成员过滤输出 JSON 对象）。
- 未标注的 `class` 维持原嫁接链：虚调 `toJson()` 子树嫁接 → 无 `toJson()` 时回退 `toString()` 字符串叶 → 再失败为 `null` 叶。
- `data` **默认可序列化**，`@Serializable()` 为可选的显式标注。
- 嵌套的 `@Serializable` class / data 成员递归展开子树。

### 成员级规则（正反向对称）

判定优先级：`@NonSerialized()` > `@SerializeField()` > 可见性默认。

| 成员情形 | 是否参与序列化 |
|----------|----------------|
| class `public` 成员 | 默认参与 |
| class `protected` / `private` 成员 | 默认排除；标 `@SerializeField()` 后参与 |
| 任意可见性 + `@NonSerialized()` | 淘汰（一票否决，优先级最高） |
| `static` 成员 | 一律排除（不属于实例状态） |
| `data` 成员 | 默认全量（data 无可见性修饰） |

### 反向（fromJson）

与正向使用**同一套过滤规则**：按类型 new 实例（不执行构造器）后按 JSON key 对名填充；JSON 中缺失的成员保持默认零值 / null；嵌套的 `@Serializable` class 成员同样可还原；未标注 class 的引用成员不还原（保持 null）。

### 示例

```sl
@Serializable()
class Point
{
    public Int32 x = 0                    # 默认参与
    public Int32 y = 0                    # 默认参与
    protected Int32 z = 0                 # 默认排除
    @SerializeField()
    protected Int32 forced = 0            # 补票参与
    @NonSerialized()
    public Int32 skipped = 0              # public 被淘汰
    public static Int32 Version = 9       # static 排除
}

Point p = Point()
p.x = 1
p.y = 2
p.forced = 5
string json = Serialize.toJson( p )          # {"x":1,"y":2,"forced":5}
Point back = Serialize.fromJson<Point>( json )
```

> 门面方法契约详见 `Core/IO/Serialize.sl` 文件头注释；data 侧规则见 [data.md](data.md) §4.2；测试用例见 `test/ExpendTest/SerializeTest.sl`。

---

## 约定与限制（Front 层解析阶段）

- Attribute 只负责"挂载元数据"，不直接改变语义。
- 是否产生额外语义（如序列化、反射、AOT 导出等）由后续编译阶段/运行时决定。
- `_init_( 参数 )` 与 `_init_( metaType type )` 可同时定义，编译器根据调用形式选择匹配的重载。
- Attribute 类本身不能再被 Attribute 修饰（不支持元元数据叠加）。

