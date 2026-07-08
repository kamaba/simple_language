# 系统内置方法（System methods）

VM 通过 `EIROpCode.CallSystemMethod` 调用；`systemMethodKind` 与 Front 中 `SimpleLanguage.ESystemMethodCall`（`Define.cs`）枚举顺序一致，需保持同步。

---

## 桥接 / 互操作（已实现）

| 名称 | 说明 |
|------|------|
| `SystemCallCLRMethod` | 调用 C# / CLR 方法（由桥接注册表解析） |
| `SystemCallNativeMethod` | 调用 C/C++ 原生方法 |
| `SystemCallJVMMethod` | 调用 JVM 方法 |

---

## 控制台 I/O

| 名称 | 说明 | 栈约定 |
|------|------|--------|
| `SystemPrint` | 向标准输出打印 | 弹出 `paramCount` 个参数；约定第一个为要输出的文本（`ToString`） |
| `SystemReadLine` | 从标准输入读取一行（`Console.ReadLine`） | 弹出 `paramCount` 个参数（一般为 0）；**压入**一行 `string` |
| `SystemReadKey` | 读取一次按键（`Console.ReadKey(true)`，不回显） | 弹出 `paramCount` 个参数（一般为 0）；**压入**单字符 `string`（`KeyChar`） |

---

## data 比较（Data*Equal）

| 名称 | 说明 | 参数 |
|------|------|------|
| `DataAllEqual` | 定义类型（`RuntimeClass` id）相同且实例成员缓冲区一致 | `(object data1, object data2)` → `bool` |
| `DataTypeEqual` | 字段名顺序与各字段**格式形状**相同（如 int/string/byte、数组、嵌套 data），不要求具名类型 id 相同 | 同上 |
| `DataNameAndTypeEqual` | 字段名与各字段**类型签名**相同（含模板实参） | 同上 |
| `DataDataEqual` | 字段名对齐后**数据值**相同；数值可按宽化规则兼容（如 `int8` 与 `int32`） | 同上 |

运行时仅接受 `metaClassKind == Data` 的 `ClassObject` 实例（含匿名 `data`）；否则返回 `false`。Front 侧注册返回 `bool`、参数为 `object`；具体比较在 VM `DataSystemMethodCall` 中实现。

## 类型强制转换（SystemConvert*）

从当前栈顶按调用约定弹出操作数（通常为 **1 个**：待转换的值），在 VM 内先**解包**为 CLR 可用的对象（数值、`SObject` 包装类型、字符串等），再按 **BCL `Convert`** 与 `CultureInfo.InvariantCulture` 转为目标类型，最后把结果 **压回栈**。

`SystemConvertInt8` 与 `SystemConvertUInt8` 例外：可带 **第二个 `int32` 参数**（从栈上先于第一个值压入，与实例方法其它双参系统调用一致）。`index == -1` 时与原先单参行为相同（`Int8`：`Convert.ToByte`；`UInt8`：同样走 legacy `Convert.ToByte` 路径，含字符串解析）；`index >= 0` 时在数值的**无符号位模式**上，从**最低位**起取连续 **4 位**：`index` 为窗口最低位下标（例如 `0` 取最低 4 位，`4` 取第 4–7 位，`2` 取第 2–5 位）。须满足 `index + 4 <=` 该类型的存储位宽；字符串在 `index >= 0` 时结果为 null。兼容旧 IR 仍可出现 **仅 1 个参数**，等价于 `index == -1`。

若转换失败或输入为 null，结果可为 null 值（`SValue` 置空）。

| 名称 | 目标类型 |
|------|----------|
| `SystemConvertInt8` | `byte`（可选第二参 `int32`：`index`，见上文） |
| `SystemConvertUInt8` | `byte`（与 `SystemConvertInt8` 相同的第二参 `index` 约定；单参等价 `index == -1`） |
| `SystemConvertSInt8` | `sbyte`（第二参 `index` 与 `SystemConvertInt8` 相同：`-1` 为 `Convert.ToSByte` / 含字符串；`≥0` 为低起算 4 位窗口） |
| `SystemConvertInt16` | `short` |
| `SystemConvertUInt16` | `ushort` |
| `SystemConvertInt32` | `int` |
| `SystemConvertUInt32` | `uint` |
| `SystemConvertInt64` | `long` |
| `SystemConvertUInt64` | `ulong` |
| `SystemConvertFloat32` | `float` |
| `SystemConvertFloat64` | `double` |
| `SystemConvertString` | `string`（`ToString`） |

---

## 参考代码位置

- 枚举：`source/Front/Define.cs` — `ESystemMethodCall`
- VM 分发：`source/VM/InnerCLRRuntime/RuntimeVM.cs` — `EIROpCode.CallSystemMethod` 分支
- IR 生成：`source/Front/IR/IRCall.cs` — `ParseSystemCall`，`systemMethodKind = (int)sysEnum`


# SLang 的系统函数
系统函数，主要是用来 重载，内置一些方法，可以通过系统函数，改变编译内部的一些逻辑

—————————————————————————————————————————————————————————

## 重载类函数 init
说明: 在类中使用 _init_函数 可以重载函数， 通过不同参数，重载，比如 _init_( int a ){} _init_( int a, int b ){}  

## 重载符号 reloadsign
说明: 在类中，如果要重载符号比如 类的相关 类的相除 类的位移等 
1. 比如 Class1 _reloadsign_( Class1 c, Class1 c2, "+" ){} 可以重载+法

## 重载通过$获取 
说明: 可以重载该方法，快速获取 比如在一个自动写的数组类，可以通过重载快速获取下标的某个值

1. _getIndex_( int index ){} 获取该下载的返回值 ，也可以返回某个定义值 array_variable.$0 或者是使用 array_variable[0] 有同样的方法
2. _getKey_( string key ){} 类似于上边的方法，可以重载后，通过 variable.$"nb" 的方式获取 

## 类型变化 cast
说明： 可以通过该方法，对类进行转换，
1. 比如 var v = intvalue.cast<string>()  对某个int值，转为string, v 是一个string类型
2. Class1{} Class2 extends Class1{}  Class2 class2Value  Class1 c = Class2Value.cast<Class2>()



