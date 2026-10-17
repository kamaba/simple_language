# 类型转换（as / is / isnot）

S 语言的类型转换与类型测试通过三个关键字完成：

| 关键字 | 作用 | 结果 |
|--------|------|------|
| `as`   | 安全类型转换 | 转换成功返回新类型值，失败返回 `null`（不抛异常） |
| `is`   | 类型测试 | 返回 `Boolean`；带绑定变量时同时完成转换 + 判空 + 绑定 |
| `isnot`| 类型测试取反 | `is` 结果取反 |

三者最终都编译为同一条 VM 指令 `CastClass`（opcode 78），区别在于 IR 层对结果的后续处理。

## as：安全转换

```s
Level3 level3 = new()
Level1 level1 = level3
level2 = level1 as Level2          # 向下转型：成功得到 Level2，失败得到 null

if level2 != null
{
    global.println(level2.Level2_var1)
}
```

要点：
- `as` 只做**继承关系探测**（父类转子类、子类转父类、转接口），不抛异常，失败统一为 `null`，因此 `as` 之后通常紧跟 null 判断。
- 支持泛型类型：`levelt3 as LevelT3<int,string>`、`arr as Array<Object>`。
- 支持接口转换：`obj as IName`。
- 支持基元类型间的转换：`20.3f as int`（数值截断）、`enumMember as Int32`（枚举解包）。
- `null as T` 结果恒为 `null`。
- `as` 的结果类型即右侧目标类型，可直接赋给对应类型变量：

```s
Error ne = e as Error
Int32 a1 = Isolate.run( fnA1( 3, 4 ) ) as int
List<int> src = arg as List<int>
```

## is：类型测试

`is` 有两种形式。

**形式一：不带绑定变量** —— 表达式返回 `Boolean`：

```s
if a is string
{
    global.println("a is string")
}
```

**形式二：带绑定变量** —— `expr is T v`。等价于：先按 `as` 语义转换并赋给 `v`，再判断 `v != null`；整个 `is` 表达式的值就是该布尔结果：

```s
if a is int str
{
    global.println("_int is  " + str )     # str 在此分支内一定非 null
}
elif b is string str2
{
    global.println("_int is2  " + str2 )
}
```

绑定变量也可以单独拿出来用：

```s
bool flag = level2 is Level3 level3ttt
if flag
{
    global.println("is ok  " + level3ttt.Level3_var3 )
}
```

泛型同样支持：

```s
LevelT2<int,int> levelt2 = new()
if levelt2 is LevelT2<int,int> levelt3_22
{
    global.println(levelt3_22.levelT2_var1)
}
```

## isnot：取反的类型测试

`isnot` 与 `is` 语法完全一致（含绑定变量形式），结果取反：

```s
if x isnot string
{
    global.println("x 不是 string")
}

if obj isnot Level3 lv        # 等价于 !(obj is Level3 lv)
{
    global.println("not level3")
}
```

## 编译期检查

编译阶段会对 `as/is/isnot` 两侧类型做关系解析（双向探测，满足其一即可），允许的关系为：

- `Same`（同类型）
- `Child`（表达式类型是目标的子类型）
- `Parent`（表达式类型是目标的父类型）
- `Interface`（目标为接口且表达式类型实现了它）
- `Num`（数值类型之间）

两侧完全无关时编译报错（`data type convert error`）。实现见 `Core/MetaExpressNode/MetaExpressAsIs.cs`（`TypeManager.ResolveTypeRelation` + `IsAcceptableForAsIs`）。

## 运行时规则

`CastClass`（C VM `vm/runtime/vm_runtime.c`）按目标类型分类处理：

| 目标类型 | 行为 |
|----------|------|
| `Object` | 恒成功 |
| `null` 值 | 直通，结果 `null` |
| `Num` | 宽松数值超类型，恒成功 |
| 基元类型（整数/浮点/字符串等） | 类型相同则原值通过；否则做数值/字符串转换（如 `Float64 -> Int32` 截断）；枚举成员先解包为底层整数再比较 |
| 类 / 接口 | 按继承链（`isExtendsRelation`）或接口实现（`implementsInterface`）检查，成功原值通过，失败推 `null` |

原始标量值永远不满足类/接口目标（结果 `null`）。

## 语法限制

1. `as/is/isnot` 优先级为 `Level9_AsOsIs`（低于 `&&`/`||` 与算术运算符），**表达式必须独立成句**，不能与其他运算混用：

```s
#! 以下写法是语法错误 !#
if a is string str || b is string str2 { ... }

#! 正确写法：分开判断 !#
if a is string str { ... }
elif b is string str2 { ... }
```

2. `as` 右侧必须是类型；`is/isnot` 右侧类型后可选跟一个绑定变量名。
3. `is/isnot` 的绑定变量由编译器自动生成隐藏局部变量承载，作用域限定在所在语句块内。

## import 中的 as

`as` 关键字另有第二职责：`import` 语句中做模块/命名空间别名（代名），与类型转换无关：

```s
import Math as M
```

## 与旧 Cast<T>() 的关系

所有类默认继承 `Core.Object`，历史上通过 `Cast<T>()` / `_cast_<T>()` 系统方法完成转换（`20.3f.Cast<int>()`）。现行推荐写法是 `as/is/isnot`；若类定义了 `_cast_<T>()` 自定义转换方法，转换时会优先走该方法（此优先逻辑为预留设计，当前以 `CastClass` 的默认继承探测为准）。

## 参考用例

- `test/BaseTest/ClassAs_Is.sl` —— as/is/isnot 全量覆盖（继承链、泛型、绑定变量、错误写法示例）
- `test/ExpendTest/IsolateTest.sl` —— `Isolate.run(...) as int`、`rp.recv() as SendPort` 等跨边界取值
- `test/ExpendTest/MqttTest.sl` —— `Error ne = e as Error`
- `test/BaseTest/GenClass_Interface.sl` —— `obj as IName` 接口转换
