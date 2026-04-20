# SLang Array 语法与安全实践（按当前实现）

本文档按当前 Front 行为整理，目标是：**写法清楚、边界清楚、容易排错**。  
核心参考：`MetaExpressNewObject`、`MetaCallNode`、`TypeManager` 与 `test/BaseTest/ArrayTest.sl`。

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

