# SLang Array 语法与安全实践（按当前实现）

本文档按当前 Front 行为整理，目标是：**写法清楚、边界清楚、容易排错**。  
核心参考：`MetaExpressNewObject`、`MetaAssignStatements`（赋值第二轮数组字面量纠正）、`MetaCallNode`、`TypeManager` 与 `test/BaseTest/ArrayTest.sl`。

## 1) Array 的定义方式

### 1.1 泛型与别名

```sl
Array<Int32> a1 = Array<Int32>.create(5)
Int32[] a2 = Int32[5]
ObjectArray a3 = object[2]   # ObjectArray = Array<Object>
```

说明：
- `ObjectArray`、`Int32Array` 等来自全局 type alias（`TypeManager.m_GlobalTypeAliasDict`）。
- `[]` 语法最终也是 `Array<T>` 的数组实例模型。

### 1.2 字面量初始化

```sl
Int32[] nums = [1,2,3,4]
string[] ss = string[]{"a","b"}
object[] mixed = object[]{"aa", 1, 1.0f}
```

推荐：
- **有明确类型就写明确类型**（避免推导到 `Object` 造成后续调用不稳定）。

## 2) 多维/锯齿数组写法

### 2.1 推荐写法（最稳定）

```sl
int[][][] cube = [ [[1,2,3],[1,2,3,4]], [[1,2,3],[5,6,7,8]] ]
```

### 2.2 你的项目当前约定写法

外层可用 `{}` 作为 Array 对象初始化容器，但**内部数组元素使用 `[]`**：

```sl
int[][][] cube = { [[1,2,3],[1,2,3,4]], [[1,2,3],[5,6,7,8]] }
```

不建议/不支持（当前行为）：

```sl
int[][][] cube = { { {1,2,3},{1,2,3,4} }, { {1,2,3},{5,6,7,8} } }
```

## 3) 访问与常见 API

```sl
Int32[] nums = Array<Int32>.create(5)
nums.fill(7)
nums.setValue(2, 99)
v1 = nums.getValue(2)

nums[1] = 50
nums.$1 += 100      # 等价 nums[1] += 100

nums.index = 1
cur = nums.current()
nums.current = 123
```

说明：
- `arr.$i` 与 `arr[i]` 等价（`$` 链式在多层访问时可读性更高）。
- `index/current` 是数组迭代游标语义的一部分。

## 4) 遍历语法

```sl
for v in nums
{
    if v != null
    {
        global.println(v.toString())
    }
}
```

for-in 需要对象满足可迭代接口语义（`IIterable` / `IIterator` 路径）。

## 5) 协变规则（重点）

### 5.1 数组实体不协变（严格同型）

- `Array<T>` 赋值给 `Array<U>`，要求 `T` 与 `U` 完全一致（含维度/模板结构）。
- 例如：`Int32[] -> Object[]` 不允许直接赋值。
- `int[][] -> object[][]` 也不允许整表直接赋值。

### 5.2 接口侧允许受控协变（只读视角）

当前保留的接口协变：
1. `IIterator<Num> <- concrete.iterator`
2. `IIterable<Object> <- Int32[]`（元素可赋值时）

它的目的：便于遍历/读，不代表右侧可当作可写的 `Array<目标元素>` 使用。

## 6) 安全建议（实战）

- **建议 1：优先显式声明类型**
  - 避免 `var a = [...]` 推导成过宽类型，后续成员调用被降级。

- **建议 2：Object 场景显式装箱**
  - 需要 `ObjectArray` 时，直接 `object[n]` 或 `object[]{...}`，不要依赖数组实体协变。

- **建议 3：多维字面量优先全 `[]`**
  - 可读、稳定、最不容易触发解析边界问题。

- **建议 4：复杂表达式拆小步**
  - 先创建数组，再逐步赋值（尤其是三维以上 + 对象元素）。

- **建议 5：优先用 for-in 做只读遍历**
  - 如果要改值，改用 index 循环，意图更清晰。

## 7) 常见错误与排查

- 错误：`里边的元素与边的数据类型不对应`
  - 原因：元素类型混杂或目标数组类型过窄。
  - 处理：显式目标类型，必要时改为 `object[]`。

- 错误：数组字面量结构不合法
  - 原因：括号层级不配平、外层 `{}` 与内层数组元素风格混乱。
  - 处理：先改成全 `[]` 版本验证，再按项目风格调整。

- 错误：将数组当实体协变使用
  - 原因：把 `Int32[]` 直接赋给 `ObjectArray`。
  - 处理：显式构造 `ObjectArray` 并逐项装箱，或走 `IIterable`/`IIterator` 接口路径。

## 8) 相关测试样例

参考 `test/BaseTest/ArrayTest.sl`：
- `arrayBasicApiTest`
- `arrayCovariantAndLiteralForInTest`
- `arrayNumberIteratorFromConcreteArrayTest`
- `arrayJagged2DAssignTest`
- `arrayConstructorsMultidimAndArrClassBulkTest`

## 9) 赋值解析顺序与数组字面量类型纠正（编译期）

### 9.1 为什么要「先右后左」

常见赋值形如 `a = expr` 或 `obj.prop = expr`。若左值是 **setter / 方法调用**（`set void f(T v)`），编译器需要先把 **右值表达式** 放进参数列表，再解析 **左值调用链** 才能拿到 **参数/成员** 的精确定义类型。因此 **右值往往先于左值完成第一轮 Parse/CalcReturnType**，此时 `CreateExpressNode` 侧拿不到左值数组的 `Array<Int32>` 等模板，字面量 `[1,2,100]` 只能按字面量自身做数值升阶推断（例如 `Array<Int16>`），与左侧已声明的 `Int32[]` 可能不一致。

### 9.2 纠正时机（第二轮）

在 `MetaAssignStatements.Parse` 中，当 **左值变量** 已解析并得到 `GetFinalMetaType()` 后，在 `CheckLeftAndRightExpress` 之前会调用 **`TryCoerceRightArrayLiteralToLeftArrayTypeAfterLeftResolved()`**（见 `MetaAssignStatements.cs`）。其意图是：在左值类型已确定的前提下，对 **「模糊」右值数组字面量** 做一次类型对齐。

### 9.3 何时纠正、何时不纠正

**会纠正**（满足全部条件时）：

- 非 setter 赋值（`m_IsSettings` 为假，且不是仅左值 method call 而无普通赋值右值）；
- 右值是 **`MetaNewObjectExpressNode` 且为数组字面量**（`newType == ArrayClass`）；
- 右值 **未** 在语法上使用显式元素类型构造（即 **不是** `Array<Int16>(n){ ... }` 那种由调用链标明的模板；此类由 `usesExplicitArrayElementTypeSyntax` 标记，**不由左值覆盖**，避免与程序员显式选择冲突）；
- 左值 `GetFinalMetaType()` 为 **数组**，且元素类型 **不是** `Object`（避免把任意字面量强行绑到过宽语义）；
- 右值当前推断的 **元素类型** 与左值 **不一致**（已一致则不再调用 `CalcReturnType`，减少重复工作）。

**不纠正**：

- Setter / 仅设置调用、右值为 null；
- 右值已 **显式** `Array<T>(...){ ... }`；
- 左值非数组、或元素为 `Object`；
- 左右元素类型已相同。

纠正动作为：对右值节点 **`SetAssignmentTargetArrayMetaType(左值数组 MetaType)`** 后 **`CalcReturnType()`**，内部与 **`MetaExpressNewObject.CalcReturnType`**、**`NumberManager.TryUnifyNumericArrayLiteralMembersToDeclaredArrayType`** 等配合，完成 **define/real 合并、字面量常量强转到左值元素类型** 等（详见源码注释）。

### 9.4 与文档其它条的关系

- 与 **§5.1「数组实体不协变」** 不矛盾：这里针对的是 **字面量** 在编译期 **重写推断与常量表示**，不是把 `Array<Int16>` 变量引用当成 `Array<Int32>` 使用。
- 仍建议 **§6 建议 1**：能写清左值类型时，右值也尽量显式，减少仅靠第二轮纠正的路径。

## 10) ArrayTest 语法总表（用于解析逻辑对照）

本节直接按 `test/BaseTest/ArrayTest.sl` 出现过的写法归档，目标是给 Front/Meta 解析提供“出现过 + 期望行为”的快速索引。

### 10.1 声明与构造

```sl
Int32[] nums = Array<Int32>.create(3)
Int32[] a = Int32[5]
ObjectArray boxed = object[2]
Level<Int32>[] levels = new(3) { Level<Int32>(10), Level<Int32>(100), Level<Int32>(30) }
ArrClass[] arr1 = new(3)
```

要点：
- 同时支持 `Array<T>.create(n)`、`T[n]`、`new(n){...}`。
- `ObjectArray` 作为 `Array<Object>` 别名在测试中频繁使用。

### 10.2 字面量与带长度初始化

```sl
arr2 = [1000,2000,3000,1005]
int[] a33 = {1,2,3,4}
int[] a332 = new(4){21,22,23,24}
int[4][] a335 = {[11,22,33], int[3]{871,872,873}, int[20]}
float[] floatsFromLiteral = {1.2,1.3,1.5}
mixedObj = object[8]{"aa", 1, "232", 1.0f}
```

要点：
- 支持直接 `[]` 推导字面量。
- 左值已知类型时支持 `{...}`。
- 支持 `T[n]{...}`（最后一维给长度）。
- 支持“长度大于 initializer 个数”的部分初始化。

### 10.3 下标与 `$` 等价访问

```sl
a2[1] = 50
a2.$1 += 100
a2[1] = a2.$1 + 200
jagged2.$0.$2 = 997
var tt1 = mixedNest.$aa.$3
arr1.$i11.i1 = 10
```

要点：
- `arr.$k` 与 `arr[k]` 等价，支持链式多层访问。
- `$` 后支持字面常量下标、变量下标、表达式结果变量下标。

### 10.4 index/current 游标语义

```sl
nums.index = 2
nums.current = 123
arr1.index = 2
arr1.current.i1 = 10
```

要点：
- `index/current` 可读写。
- 在 `for-in` 场景可结合 `arr.index` 定位当前位置。

### 10.5 for-in / 计数 for / while 迭代

```sl
for v in levels { ... }
for a in arr1 { ... }
for a in [11111111,2222222222,3,4444444444444] { ... }
for i = 0, i < a2.length, i++ { ... }
while it.moveNext() { Num n = it.current() }
```

要点：
- 覆盖 `for-in`、三段式 `for`、`while + IIterator`。
- `for-in` 支持变量数组与字面量数组。

### 10.6 多维与锯齿数组

```sl
int[][] jagged2 = int[2][]
jagged2[0] = int[4]
jagged2[1] = [1,100,1000]

int[][][] cube = { [[1,2,3],[1,2,3,4]], [[1,2,3],[5,6,7,8]] }
ArrClass[][] arrclass1 = new(10)
arrClass2 = ArrClass[10][10][]
object[][] mixedNest = [[137,138,139,ac,148],[[11],[13,14,15]]]
```

要点：
- 支持规则多维与锯齿混合。
- 支持对象元素嵌套（`object[][]` 中放基础值、对象、子数组）。
- 未完全支持的形态在测试中有注释标记，作为负例保留。

### 10.7 元素类型包含泛型/类

```sl
Level<int>[] a44 = new(15) { Level<int>(200) }
Level<int>[][] levelGrid = { [levelvar, levelvar], [levelvar, levelvar], [levelvar] }
ArrClass[] arr1 = new(3)
arr1[1] = { i1 = 20 }
arr1[1].i1 = 10000
```

要点：
- 数组元素可为泛型类实例。
- 支持数组元素对象初始化器（`arr[i] = { field = ... }`）。

### 10.8 数组 API 与成员

```sl
nums.fill(7)
nums.setValue(2, 99)
nums.getValue(2)
nums.length
concrete.iterator
```

要点：
- 已覆盖 `fill/setValue/getValue/length/iterator`。
- `iterator` 可桥接到接口变量（见下一节协变规则）。

### 10.9 协变/可赋值边界（以测试行为为准）

```sl
IIterator<Num> it = concrete.iterator
IIterable<Object> it2 = concrete
forObject(arr2)           # 预期报错：不支持数组实体协变
#ObjectArray oaa = arr2   # 预期报错
```

要点：
- 数组实体默认不协变（`Int32[]` 不能当 `Object[]`）。
- 接口视角存在受控协变路径（用于遍历语义）。

## 11) 解析实现建议（后续改动时建议同步维护）

- 若新增数组语法，请在 `ArrayTest.sl` 增加“正例 + 负例注释”，并在本文件第 10 节补一条最小示例。
- 若调整 `MetaAssignStatements` / `MetaExpressNewObject` 的数组推断逻辑，优先检查：
  - 字面量二次纠正是否被绕过；
  - 显式 `Array<T>(...){...}` 是否仍保持“显式优先”；
  - `Object` 元素类型是否被错误强制窄化。
- 建议把“语法支持”与“语义限制（暂不支持）”分开记录，避免把负例误当成可用写法。

