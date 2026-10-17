# 系统内置方法（System methods）

VM 通过 `EIROpCode.CallSystemMethod` 调用；系统方法由工程 `.jsonc` 的 `systemCalls[]` 声明注册（`name` / `returnType` / `params` / `isVariadic` / `cvmFunction`），Front 导出时按名称哈希生成 `id` 并携带 `cvmFunction` 符号名，C VM 装载期按符号名解析实现（不再有枚举顺序约定）。

## 调用限制（compile.isAllowSystemMethodCall）

工程 jsonc `compile` 段的 `isAllowSystemMethodCall`（缺省 `true`）控制系统方法是否可用：

- `false` 时，Front 语义解析层（`MetaCallNode` 的全局裸调用与类路径回退两处解析点）拒绝解析到 `systemCalls` 注册表声明的调用，报编译错误 **21473**；仅约束当前工程自身源码，不影响引用模块与 `@tag{}` 内部机制
- **类型转换映射不受限**：`int()` / `Int32()` / `sbyte()` / `float()` 等 C 型别名写法（及其 `SystemConvert*` 目标声明的全名写法）属语言级纯值转换，恒可用
- 判定实现：`SystemMethodCallDeclarationRegistry.IsTypeConvertDecl`（别名映射目标名集合）

---

## 桥接 / 互操作

早期的 `SystemCallCLRMethod` / `SystemCallNativeMethod` / `SystemCallJVMMethod` 已移除。现行互操作方式：

- FFI / native 内存：声明式 systemCalls（如 `SystemMemoryNative*` 系列，见 `md/project/ffi.md`）
- 插件 / 内联标签：`AtSignLabelCall` / `AtSignLabelCallVoid`（`@<tag>(){...}` 语法，经 C VM `atsign:label`）

---

## 控制台 I/O

| 名称 | 说明 | 栈约定 |
|------|------|--------|
| `SystemPrint` | 向标准输出打印 | 弹出 `paramCount` 个参数；约定第一个为要输出的文本（`ToString`） |
| `SystemReadLine` | 从标准输入读取一行（`Console.ReadLine`） | 弹出 `paramCount` 个参数（一般为 0）；**压入**一行 `string` |
| `SystemReadKey` | 读取一个字符（stdin） | 弹出 `paramCount` 个参数（一般为 0）；**压入**单字符 `string` |

---

## data 比较（Data*Equal）

| 名称 | 说明 | 参数 |
|------|------|------|
| `DataAllEqual` | 定义类型（`RuntimeClass` id）相同且实例成员缓冲区一致 | `(object data1, object data2)` → `bool` |
| `DataTypeEqual` | 字段名顺序与各字段**格式形状**相同（如 int/string/byte、数组、嵌套 data），不要求具名类型 id 相同 | 同上 |
| `DataNameAndTypeEqual` | 字段名与各字段**类型签名**相同（含模板实参） | 同上 |
| `DataDataEqual` | 字段名对齐后**数据值**相同；数值可按宽化规则兼容（如 `int8` 与 `int32`） | 同上 |

运行时仅接受 `metaClassKind == Data` 的 `ClassObject` 实例（含匿名 `data`）；否则返回 `false`。Front 侧注册返回 `bool`、参数为 `object`；具体比较在 C VM `data_system_method.c`（`vm_sys_data_all_equal` 等）中实现。

## 类型强制转换（SystemConvert*）

从当前栈顶弹出 **1 个**操作数（待转换的值），在 C VM 内转换后把结果**压回栈**（实现：`csimple_lang/src/vm/system_method_call/convert_system_method.c`）：

- 字符串实参：按目标类别用 `strtol` / `strtod` 解析；解析失败结果为 `null`
- 装箱数值对象（PTR 槽）：先解包取出标量值再转换
- 其余标量：按目标整数/浮点槽位截断取值

| 名称 | 目标类型（Front 声明） |
|------|----------|
| `SystemConvertInt8` | `Int8` |
| `SystemConvertUInt8` | `UInt8` |
| `SystemConvertSInt8` | `Int8`（有符号语义） |
| `SystemConvertInt16` | `Int16` |
| `SystemConvertUInt16` | `UInt16` |
| `SystemConvertInt32` | `Int32` |
| `SystemConvertUInt32` | `UInt32` |
| `SystemConvertInt64` | `Int64` |
| `SystemConvertUInt64` | `UInt64` |
| `SystemConvertFloat32` | `Float32` |
| `SystemConvertFloat64` | `Float64` |
| `SystemConvertString` | `string`（`ToString`） |

C 型别名（`byte`/`sbyte`/`int`/`float` 等）在 `SystemMethodCallDeclarationRegistry` 中映射到对应 `SystemConvert*` 调用。

---

## 参考代码位置

- 声明注册：`source/Front/Project/SystemMethodCallDeclaration.cs` — `SystemMethodCallDeclarationRegistry`（从 jsonc `systemCalls` 加载；`GetIndex()` 按名称哈希生成 `id`）
- IR 生成：`source/Front/IR/IRCall.cs` — `ParseSystemCall`，`CallSystemMethod` payload 携带 `name` / `id` / `paramCount`
- VM 注册/分发：`csimple_lang/src/vm/system_method_call/system_method_registry.c`（按 `cvmFunction` 符号名解析，支持扩展 DLL `dll:func` 形式）+ 同目录各域 `*_system_method.c`


# SLang 的系统函数
系统函数，主要是用来 重载，内置一些方法，可以通过系统函数，改变编译内部的一些逻辑

—————————————————————————————————————————————————————————

## 重载类函数 _init_
说明: 在类中使用 _init_ 函数可以重载构造，通过不同参数重载，比如 _init_( int a ){} _init_( int a, int b ){}

## 运算符重载（约定方法名）
说明: 在类中通过约定方法名重载运算符（+ - * / % 与比较、逻辑等）
1. 比如 static Class1 _add_( Class1 a, Class1 b ){} 可以重载+法；完整约定方法名表见 operator.md

## 重载通过$获取（_getItem_ / _setItem_）
说明: 在类中定义下标约定方法，快速获取/赋值下标元素
1. T _getItem_( int index ){} 读取下标，variable.$0 与 variable[0] 等价
2. void _setItem_( int index, T value ){} 写入下标，variable[0] = v

## 类型转换 as
说明： 运行时类型转换用 as 关键字完成（详见 cast.md）
1. 比如 Level2 v = level1 as Level2  转换失败得 null



