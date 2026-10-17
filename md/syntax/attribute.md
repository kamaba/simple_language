# attribute（属性 / 注解）

用于模拟 CLR 的 `Attribute` 或 Java 的 `Annotation`：在**类声明**、**成员变量**、**成员函数**、**enum**、**模块级 data** 上附加"元数据标记"，供编译器 / 运行时 / 工具链消费。

> 本篇为 2026-10「四时点重构」后的现行规范。架构总纲、迁移方案与决策记录见
> `csimple_lang/md/design/ATTRIBUTE_DESIGN.md`（唯一权威设计基线）。

每个 attribute 由两个维度刻画，均在定义处显式声明：

| 维度 | 含义 | 取值 |
|------|------|------|
| **stage**（生效时点） | 元数据由谁、在哪个阶段消费 | PreCompile(0) / Compiling(1) / Preload(2) / Runtime(3) |
| **targets**（合法注册点） | 允许标注在哪些声明上 | Class(1) / Data(2) / Enum(4) / Field(8) / Method(16) 位掩码 |

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

## 声明位置（五注册点）

Attribute 写在被修饰的声明**之前**，允许多个：

```sl
# 修饰 class
@Serializable()
class Player
{
    # 修饰成员变量（Field）
    @Range(0, 100)
    hp = 100

    # 修饰成员函数（Method）
    @Obsolete("Use NewMove instead")
    Move(x, y)
    {
    }
}

# 修饰 enum（Enum）
@MyAttr()
enum Color
{
    Red = 1
    Green = 2
}

# 修饰模块级 data / 全局数据（Data）
@MyAttr()
data Config
{
    width = 800
    height = 600
}
```

注册点全集（`EAttributeTarget`，位掩码）：

| 位 | 名称 | 挂载对象 |
|----|------|----------|
| 1 | Class | `class` 声明 |
| 2 | Data | 模块级 `data`（全局数据）声明 |
| 4 | Enum | `enum` 声明 |
| 8 | Field | 成员变量（static 与 instance 均可） |
| 16 | Method | 成员函数 |
| 31 | All | 全部注册点（1\|2\|4\|8\|16 的预定义字面量成员） |

**放置校验**：attribute 标注在不属于其 `targets` 掩码的位置 → 编译 Error（LID 21465）。
注意 **data 的内部成员**上不支持挂载（仅 data 声明级可挂）；字段级标签只用于 class 成员。

---

## 四时点（EAttributeStage）

| stage | 值 | 生效层 | 执行者 | 效果 |
|-------|----|--------|--------|------|
| PreCompile | 0 | Front · Meta 层成员构建入口 | C# 编译器内建处理器 | 命中处理器的符号**跳过编译**——成员整体不进 module.json |
| Compiling | 1 | Front · IR/SLIR 物化 | C# 编译器内建处理器 | 效果物化进 IR 并随 module.json 导出，运行期只剩结果 |
| Preload | 2 | cvm · load/assembly（`run` 前装配期） | C VM 原生 handler（注册表） | 装配期完成原生资源注册（如 FFI 绑定）；绑定失败回退 SL 函数体 |
| Runtime | 3 | cvm · 执行期 | SL 实例的 `_enter_`/`_exit_` 回调 + 查询 API | 方法帧触发打点；运行中随时查询宿主上绑定的实例数据 |

**核心约定 D2**：PreCompile / Compiling / Preload 的子类**只写数据、不写回调**——SL 代码不可能在 C# 编译进程或装配前的 VM 里执行；**只有 Runtime 触发型子类重写 `_enter_`/`_exit_` 并携带真实 SL 逻辑**。

### PreCompile（编译前）

Front 在成员进入语义分析**之前**读取 `stage == PreCompile` 的 attribute 并查内建处理器注册表；处理器返回"跳过"则该符号不进入后续编译（不生成 Meta/IR，module.json 中无痕消失）。

内建示例：`@Exclude()`——条件隔离，收编旧 `@IF/@ELSE/@ENDIF` 宏与旧 static-if 条件编译。

### Compiling（编译中）

Front 从"按名称硬编码"改为按 `stage == Compiling` 分发到注册表处理器，效果物化进 IR / SLIR：

- `@Nickname` — 别名登记（`ExportName`，类/方法跨模块引用的短名注册）；
- `@AOT` — AOT 标记位（方法 `flags |= AotFlag`）；
- `@GPU` — GPU kernel 标记（gpuAttribute，位于 Std 库）。

### Preload（运行前）

cvm 装载（解析 module.json `attributeList`）后在装配期统一执行：

- `@DllImport` — 解析 native 库与符号 → 静态绑定 → 命中方法的调用点改写为 FFI 直调；绑定失败回退执行 SL fallback 函数体；
- `@DllStaticImport` — 编译期即发射 FFI 静态调用 opcode + 装配期完成静态绑定（双轨，不走 SL fallback）。

新增一种 Preload attribute 只需在 C VM 注册表登记原生 handler，装配主流程零改动。

### Runtime（运行中）

两种消费模式（详见下文「Runtime 时点详解」）：

- **触发型**：方法帧压入时调 `_enter_()`、弹出时调 `_exit_()`；
- **查询型**：SL 代码运行中经 `Attribute.getClassAttributes` 等三个静态方法读取宿主上绑定的实例数据。

---

## 定义 Attribute 类

自定义 Attribute 需要继承内置基类 `Attribute`（无需 import），语法与普通类继承相同：

```sl
MyAttr extends Attribute
{
    # ① 生效时点 + 合法注册点：_init_ 中必填
    _init_()
    {
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Method
    }
}
```

> `Attribute` 是语言内置基类，所有自定义 Attribute 类必须 `extends Attribute`；
> `EAttributeStage` / `EAttributeTarget` 两个枚举定义于 Core 库（`Core/Attribute.sl`）。

### 书写规范

**① `_init_` 中显式设置 stage / targets（必填）**

```sl
_init_()
{
    this._attributeStage = EAttributeStage.Runtime      # 生效时点
    this._attributeTargets = EAttributeTarget.Field     # 合法注册点
}
```

未声明的子类回退为 `Compiling + All`（编译器同时报告 Error LID 21464）。

**② 使用参数（接收标注处实参）**

`@MyAttr(...)` 的参数经 `_init_` 形参写入子类字段；查询型子类配 `get` 前缀的 getter 供消费方读取：

```sl
public class NumRange extends Attribute
{
    private string _title = ""
    private double _min = 0
    private double _max = 0

    _init_( string title, double min, double max )
    {
        this._title = title
        this._min = min
        this._max = max
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Field
    }

    public get string title()
    {
        ret this._title
    }
    public get double min()
    {
        ret this._min
    }
    public get double max()
    {
        ret this._max
    }
}

# 使用：参数与 _init_ 形参按位置对应
@NumRange( "血量", 0, 9999 )
public double hp = 100
```

**③ 触发型回调（仅 Runtime 触发型子类）**

```sl
public class LogTrace extends Attribute
{
    _init_()
    {
        this._attributeStage = EAttributeStage.Runtime
        this._attributeTargets = EAttributeTarget.Method
    }

    # 协变返回：返回声明类自身（对齐 _add_ 等 CurrentClass 惯例），支持链式
    public override LogTrace _enter_()
    {
        Console.println( "[enter] " + this._ownerClassName + "." + this._ownerMemberName )
        ret this
    }

    public override void _exit_()
    {
        Console.println( "[exit ] " + this._ownerClassName + "." + this._ownerMemberName )
    }
}
```

### 宿主信息（系统注入）

回调与查询拿到的实例由系统构造，两个 protected 字段自动注入宿主信息：

| 字段 | 含义 |
|------|------|
| `_ownerClassName` | 宿主类名 |
| `_ownerMemberName` | 宿主成员名（方法级/字段级；类级为空串） |

---

## Runtime 时点详解

### 触发型：`_enter_` / `_exit_`

基类 `Attribute` 与根类 `Object` 均有默认空实现（`_enter_` 返回 this、`_exit_` 为空体），子类按需 override：

- **`Attribute _enter_()`** — 宿主方法帧压入时触发，返回声明类自身（协变返回合法）；
- **`void _exit_()`** — 宿主方法帧弹出时触发（带返回值的方法在**返回值求值之后**触发）。

触发规则：

| 规则 | 说明 |
|------|------|
| 作用范围 | **仅方法级**严格配对（类级不触发，见下） |
| 多 attribute 叠加 | `_enter_` 按声明顺序正序执行；`_exit_` 逆序对称 |
| 实例缓存 | 同一宿主的触发与查询通道共享同一份实例缓存（参数已注入） |
| 零开销门控 | 类/字段/方法三级装配期预计算 `has_runtime_attribute` 布尔；无 attribute 的方法走原路径，零额外开销 |
| 异常路径 | 方法帧正常或异常弹出均触发 `_exit_` |
| 类级 | 类级 Runtime attribute 不触发回调（仅供查询）；进程 teardown 阶段统一逆序 `_exit_` |

实测示例（`test/ExpendTest/AttributeRuntimeTest.sl`）：

```sl
@LogTrace()
@Meter()
public void tick()
{
    ...
}
# 调用 tick() 后打点顺序：
# [enter]tick（LogTrace._enter_）→ [m-enter]（Meter._enter_）
# → [m-exit]（Meter._exit_）→ [exit]tick（LogTrace._exit_）
```

### 查询型：三个静态 API

定义于基类 `Attribute`（转调系统方法），返回宿主上绑定的 **stage == Runtime** 的实例数组：

```sl
# 类级（含继承）
public static Array<Attribute> getClassAttributes( string className )

# 字段级（覆盖 static 与 instance 字段）
public static Array<Attribute> getFieldAttributes( string className, string fieldName )

# 方法级
public static Array<Attribute> getMethodAttributes( string className, string methodName )
```

语义约定：

- `className` 支持**全名与短名**（如 `ProjectTest.AttrHost1` 与 `AttrHost1` 均可）；
- 方法级仅匹配**声明类自身**（不含继承来的方法）；同名方法有指令体者优先；
- 未命中 / 非法类名一律返回**空数组（非 null）**；
- 拿到 `Array<Attribute>` 后按需下行转型读取参数：

```sl
Array<Attribute> attrs = Attribute.getFieldAttributes( "AttrRuntimeDemo", "hp" )
if attrs != null && attrs.length == 1
{
    NumRange r = attrs._getItem_( 0 ) as NumRange
    Console.println( r.title )      # "血量"
}
```

---

## SLIR 契约（module.json 导出形态）

attribute 数据随 module.json 的 `attributeList` 导出（类 / 字段 / 方法三级均有），每条携带 `stage`/`targets`：

```json
"attributeList": [ { "name": "NumRange", "args": ["血量", "0", "9999"], "stage": 3, "targets": 8 } ]
```

- `stage`：0=PreCompile 1=Compiling 2=Preload 3=Runtime（PreCompile 命中跳过的成员整体不出现）；
- `targets`：位掩码（1=Class 2=Data 4=Enum 8=Field 16=Method）；
- 旧字段 `handleType`（0/1 两值）已删除——`Preload`/`Runtime` 在旧模型对应 `handleType=1`、`PreCompile`/`Compiling` 对应 0。旧 module.json 与新 VM 互不兼容属预期，需全量重编。

> ⚠ Front `compile` 命令必须带 `-e ir` 选项才会执行导出阶段、重写 module.json；缺省只编译到 IR，module.json 保持旧文件（dotnet 退出码仍为 0）。

---

## 内建 attribute 全景

| 标签 | stage | targets | 定义文件 | 效果 |
|------|-------|---------|----------|------|
| `@Exclude()` | PreCompile | All | `Lib/Core/Exclude.sl` | 该符号跳过编译、不进 module.json（条件隔离，收编旧 `@IF` 宏） |
| `@Nickname(...)` | Compiling | All | `Lib/Core/Nickname.sl` | 别名登记（`ExportName` 跨模块短名引用） |
| `@AOT(...)` | Compiling | All | `Lib/Core/AOT.sl` | AOT 标记位（方法 flags） |
| `@GPU(...)` | Compiling | Method | `Lib/Std/GPU/GPU.sl` | GPU kernel 标记 |
| `@DllImport(...)` | Preload | Method | `Lib/Core/DllImport.sl` | 装配期 FFI 静态绑定 + 调用点改写，失败回退 SL 体（详见 [ffi.md](../project/ffi.md)） |
| `@DllStaticImport(...)` | Preload | Method | `Lib/Core/DllStaticImport.sl` | 编译期发射 FFI 静态调用 + 装配期绑定（无 SL fallback） |
| `@Route(...)` | Preload | All | `Lib/Std/Net/Route.sl` | 占位（随 Net 库另立设计，未接入编译清单） |
| `@Serializable()` | Runtime | Class | `Lib/Core/Attribute.sl` | 声明该类型按成员展开参与 JSON 序列化 |
| `@SerializeField()` | Runtime | Field | 同上 | 强制 protected/private 成员参与序列化（补票） |
| `@NonSerialized()` | Runtime | Field | 同上 | 将该成员从序列化中淘汰（一票否决） |

**带 Runtime stage attribute 的方法不参与自动 inline**：方法帧是 cvm 触发 `_enter_`/`_exit_` 的锚点，inline 展开会吞掉调用点（对标 C# P/Invoke 不可内联）。

---

## 内置序列化标签（@Serializable / @SerializeField / @NonSerialized）

标准库内置三个 Attribute 类（定义于 `Core/Attribute.sl`），配合 `Core/IO/Serialize.sl` 的 JSON 直连门面（`Serialize.toJson` / `toJsonPretty` / `fromJson`）控制对象与 JSON 的互转范围。VM 侧消费点已切换到 Runtime 查询通道（`stage==Runtime` 按名过滤），行为与旧版一致：

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

> 门面方法契约详见 `Core/IO/Serialize.sl` 文件头注释；data 侧规则见 [data.md](data.md) §4.2；测试用例见 `test/ExpendTest/SerializeTest.sl` / `AttributeRuntimeTest.sl`。

---

## 书写规则与限制

- **stage / targets 必填**：子类 `_init_` 中必须显式设置 `_attributeStage` / `_attributeTargets`；缺省回退 `Compiling + All`（Error LID 21464）。
- **放置校验**：标注位置不在 `targets` 掩码内 → Error LID 21465。
- **回调仅 Runtime 触发型**：PreCompile / Compiling / Preload 子类不声明 `_enter_`/`_exit_`（写了也不会被执行）。
- **协变返回**：`_enter_` override 返回声明类自身合法（`IsEqualMetaFunction` 只比较方法名+模板+形参，不比较返回类型）。
- **enum 声明坑（P1 实测）**：`EAttributeStage` / `EAttributeTarget` 的**成员行间禁写任何注释**——行内 `#` 注释会吞掉 LineEnd 导致成员黏连、enum 成员表整体丢失。注释只放 enum 外部上方独立行。
- **enum 位或暂不支持**：`EAttributeTarget.Class | EAttributeTarget.Method` 这类组合表达式会被 Front 拒绝（LID 21258）；组合值用预定义字面量成员（`All = 31`）。
- **自定义 Attribute 是被动元数据**：是否产生额外语义由编译器注册表处理器（内建）或运行时消费方（你的 SL 代码 + 内建系统）决定；用户子类本身只能携带数据与 Runtime 回调。
- Attribute 类本身不能再被 Attribute 修饰（不支持元元数据叠加）。
- 子类命名需避开内建类型名与已注册 alias（同模块全局注册表，撞名报 DuplicateDefine；如测试中 `Range` 与 Core 容器 `Range<T>` 撞名改 `NumRange`）。

---

## 旧语法与机制废弃清单

以下旧写法已随四时点重构退役，遇见即改写：

| 旧写法 | 现行替代 |
|--------|----------|
| 方括号语法 `[Ab1("a")]` / `["Am"="o"]` | 统一 `@Name(args)` |
| `@IF / @ELSE / @ENDIF` 宏、static-if 条件编译 | `@Exclude()`（PreCompile） |
| `handleType`（Compile=0 / Runtime=1 两值） | `stage` + `targets` 双字段（module.json 契约原子切换，无兼容层） |
| 旧三挂点表（ClassLinkComplete / MemberVariableExpress / MemberFunctionInject） | 五注册点（Class/Data/Enum/Field/Method）× 四时点；Front 侧按 `stage==Compiling` 分发 |
| `@DllImport` 编译期代码注入（隐藏字段 `__dll_<name>` + 链头 if 转发） | Preload 原生 handler 装配期绑定；旧 `static Func<...>` 变量声明形式不再支持 |
| 子类 `void construct` / `_init_( metaType type )` 旧形态 | `_init_` 中设 stage/targets；编译期类型感知逻辑移入 Front 处理器 |
| `Condition` 标签 | 已删除（声明了 Runtime 但从未接线）；条件语义由 `@Exclude` 承接 |
| `Nickname` / `AOT` 等子类内的 `override void OnCompile()` 钩子 | 已删除——PreCompile/Compiling/Preload 子类只写数据不写回调（D2） |

> 测试样张：`test/ExpendTest/AttributeTest.sl`（自定义子类 + 三级标注）、`AttributeRuntimeTest.sl`（Runtime 触发+查询 19 断言）、`AttributeExcludeTest.sl`（PreCompile）、`AttributeNicknameTest.sl`（Compiling）。
