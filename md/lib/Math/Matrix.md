# Matrix（通用动态矩阵）

`Math.Matrix`（别名 `Mat`）是维度任意的通用矩阵，内部以 `Array<Float32>` 行主序连续存储。与固定尺寸的 `Float32_3x3` / `Float32_4x4` 值类型互补：

| | `Matrix` | `Float32_3x3` / `Float32_4x4` |
|------|------|------|
| 维度 | 任意（运行期确定） | 编译期固定 |
| 存储 | `Array<Float32>` 堆数组 | 9 / 16 个标量字段 |
| 精度 | 仅 Float32 | 三精度 |
| 批量运算 | systemCalls 批量下沉（数组级） | systemCalls 对象级下沉 |
| 适用 | 线性代数通用计算、动态规模 | 图形变换热路径 |

`Matrix` 的批量运算同样走 systemCalls：矩阵乘 / 转置为主工程符号（`vm_sys_math_matmul_float32` / `vm_sys_math_transpose_float32`，声明于 `Math.jsonc`），逐元素运算复用 `Core` 模块的 `SystemArray*` 家族。

---

## 1. 字段

```sl
public Int32 rows    # 行数
public Int32 cols    # 列数
# 内部：Array<Float32> _data，行主序，长度 rows * cols
```

---

## 2. 构造

```sl
# 零矩阵（内部 SystemArrayFillValue 批量清零，一次系统调用替代逐元素循环）
Matrix( Int32 rows, Int32 cols )

# 由一维数组构造（行主序，取前 rows*cols 个元素）
Matrix( Int32 rows, Int32 cols, Array<Float32> values )
```

---

## 3. 元素访问

| 方法 | 签名 | 说明 |
|------|------|------|
| `[]` | `_getItem_( Int32 index ) -> Float32` | 扁平下标（行主序，`index = row * cols + col`） |
| `[]=` | `_setItem_( Int32 index, Float32 value )` | 扁平写 |
| `getValue` | `( Int32 row, Int32 col ) -> Float32` | 行列访问 |
| `setValue` | `( Int32 row, Int32 col, Float32 value )` | 行列写 |
| `count` | `() -> Int32` | 元素总数（rows * cols） |
| `isSquare` | `() -> bool` | 是否方阵 |

```sl
Matrix m = Matrix( 2, 3 )
m.setValue( 0, 1, 5.0f )     # 第 0 行第 1 列
Float32 v = m[1]             # 扁平下标 = 0 * 3 + 1
```

---

## 4. 运算符重载

| 运算符 | 签名 | 下沉实现 |
|------|------|------|
| `+` | `_add_( Matrix b ) -> Matrix` | `SystemArrayAddFloat32`（Core，数组逐元素加） |
| `-` | `_sub_( Matrix b ) -> Matrix` | `SystemArraySubFloat32`（数组逐元素减） |
| `*` | `_mul_( Object obj ) -> Matrix` | `Matrix`：`SystemMathMatMulFloat32`（`vm_sys_math_matmul_float32`，三数组 + 维度参数批量下沉）；`Float32`：`SystemArrayMulFloat32`（标量数乘） |
| `==` | `_eq_( Matrix b ) -> bool` | `SystemArrayEqualsFloat32`（逐元素精确比较，含维度检查） |
| `!=` | `_ne_( Matrix b ) -> bool` | 取反 |

矩阵乘要求 `this.cols == b.rows`，结果为 `rows × b.cols`。

---

## 5. 实例方法

| 方法 | 签名 | 说明 |
|------|------|------|
| `transpose` | `() -> Matrix` | 转置（`SystemMathTransposeFloat32` 批量下沉） |
| `determinant` | `() -> Float32` | 行列式（方阵；按第一行余子式递归展开，纯 SL） |
| `isInvertible` | `() -> bool` | 行列式非 0（方阵） |
| `clone` | `() -> Matrix` | 深拷贝（`SystemArrayCopy`，生成新数组不与源共享存储） |
| `toString` | `() -> string` | `"Matrix(RxC)[v0, v1, ...]"` 行主序展开 |

注意：`Matrix` 不提供 `inverse()`；需要求逆时优先用固定尺寸类型（`Float32_3x3` / `Float32_4x4` 的 `inverse()` 已下沉 cvm），或自行以伴随矩阵 / 高斯消元实现。

---

## 6. 静态工厂

| 方法 | 签名 | 说明 |
|------|------|------|
| `identity` | `static ( Int32 n ) -> Matrix` | n × n 单位阵 |
| `zero` | `static ( Int32 rows, Int32 cols ) -> Matrix` | 零矩阵 |

---

## 7. systemCalls 映射

| SL 调用 | systemCall 名 | cvm 函数 | 声明位置 |
|------|------|------|------|
| 矩阵乘（`_mul_` 内部） | `SystemMathMatMulFloat32` | `vm_sys_math_matmul_float32`（主工程符号，无 `!`） | `Math.jsonc` |
| 转置（`transpose` 内部） | `SystemMathTransposeFloat32` | `vm_sys_math_transpose_float32`（主工程符号） | `Math.jsonc` |
| 批量清零 / 拷贝 / 逐元素四则 / 比较 | `SystemArrayFillValue` / `SystemArrayCopy` / `SystemArrayAddFloat32` 等 | `vm_sys_array_*`（主工程符号） | `Core.jsonc` |

与矩阵值类型的差异：值类型传 `object`（C 侧按 `member_data` 布局直读字段）；`Matrix` 传 `Array<object>`（C 侧取 `VMArray` 数据指针后按 `float32*` 批量处理）。

---

## 8. 使用示例

```sl
# 线性方程组 A * x = b 的系数矩阵
Matrix A = Matrix( 2, 2, [ 2.0f, 1.0f, 1.0f, 3.0f ] )
if A.isInvertible()
{
    Console.println( A.determinant().toString() )    # 5
}

# 批量运算
Matrix B = Matrix( 2, 2, [ 1.0f, 0.0f, 0.0f, 1.0f ] )
Matrix C = A + B
Matrix D = A * B            # 矩阵乘（下沉 cvm）
Matrix E = A * 2.0f         # 标量数乘
Matrix F = D.transpose()

Console.println( C.toString() )
```
