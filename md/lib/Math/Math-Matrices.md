# Math-Matrices（矩阵值类型：3x3 / 4x4 × Float16 / Float32 / Float64）

`Math` 命名空间下 6 个固定尺寸矩阵类。元素直接以 `m{row}{col}` 字段存储（行主序），高消耗运算（乘 / 加 / 转置 / 行列式 / 求逆 / 变换 / 工厂）全部经 systemCalls 下沉到 cvm（`math_lib.dll` 的 `mathvm_*`），C 侧直接按对象内存（`member_data`）读写字段，结果经 out 参数写回。

| 类 | 别名（@Nickname） |
|------|------|
| `Math.Float32_3x3` | Matrix3x3 / Mat3 / float3x3 |
| `Math.Float32_4x4` | Matrix4x4 / Mat4 / float4x4 |
| `Math.Float64_3x3` | Matrix3x3d / Mat3d / double3x3 |
| `Math.Float64_4x4` | Matrix4x4d / Mat4d / double4x4 |
| `Math.Float16_3x3` | Mat3h / half3x3 |
| `Math.Float16_4x4` | Mat4h / half4x4 |

以下以 `Float32` 系列为基准描述签名，其余两系列按第 5 节差异替换。

---

## 1. 字段与索引访问

```sl
# Float32_3x3：9 个字段，行主序（m{行}{列}）
public Float32 m00 m01 m02
public Float32 m10 m11 m12
public Float32 m20 m21 m22

# Float32_4x4：16 个字段
public Float32 m00 m01 m02 m03
...
public Float32 m30 m31 m32 m33
```

两种访问方式：

```sl
Float32_3x3 m = Float32_3x3.identity()
Float32 v = m[4]                # _getItem_：扁平下标 0..8（4x4 为 0..15）
Float32 e = m.getValue( 1, 1 )  # 行列式访问（get/set 为语言关键字，故方法名带 Value）
m.setValue( 0, 1, 5.0f )        # m.m01 = 5.0f
```

---

## 2. 构造

| 构造 | 3x3 | 4x4 | 说明 |
|------|-----|-----|----|
| `_init_()` | ✔ | ✔ | 全零矩阵 |
| `_init_( 9 或 16 个标量 )` | ✔ | ✔ | 按行主序逐元素 |
| `_init_( Array<Float32> values )` | ✔ | ✔ | 取前 9 / 16 个元素 |
| `_init_( Float32_3x3 m )` | ✘ | ✔ | 3x3 提升：线性部分左上，m33 = 1，平移置 0 |

---

## 3. 运算符重载

| 运算符 | 签名（3x3 版） | 下沉实现 |
|------|------|------|
| `*` | `_mul_( Float32_3x3 b ) -> Float32_3x3` | `SystemMathMat3Mul`（矩阵乘） |
| `+` | `_add_( Float32_3x3 b ) -> Float32_3x3` | `SystemMathMat3Add` |
| `==` | `_eq_( Float32_3x3 b ) -> bool` | `SystemMathMat3Eq`（逐元素精确比较） |
| `!=` | `_ne_( Float32_3x3 b ) -> bool` | `_eq_` 取反 |

无 `-`（减法）与标量乘运算符；标量缩放用静态工厂组合。

---

## 4. 实例方法与静态工厂

### 4.1 3x3

| 方法 / 工厂 | 签名 | 说明 |
|------|------|------|
| `multiply` | `( Float32_3x3 b ) -> Float32_3x3` | 矩阵乘 |
| `transform` | `( Float32_3 v ) -> Float32_3` | 2D 仿射变换（x, y 齐次），独有 |
| `transpose` | `() -> Float32_3x3` | 转置 |
| `determinant` | `() -> Float32` | 行列式 |
| `inverse` | `() -> Float32_3x3` | 伴随矩阵 / det；**不可逆时返回零矩阵**（不抛异常） |
| `clone` | `() -> Float32_3x3` | 拷贝 |
| `identity` | `static get` | 单位阵 |
| `zero` | `static get` | 零矩阵 |
| `rotationX / Y / Z` | `static ( Float32 radians ) -> Float32_3x3` | 绕轴旋转（弧度） |
| `scale` | `static ( Float32 sx, Float32 sy ) -> Float32_3x3` | 缩放 |
| `translation` | `static ( Float32 tx, Float32 ty ) -> Float32_3x3` | 平移 |

### 4.2 4x4

| 方法 / 工厂 | 签名 | 说明 |
|------|------|------|
| `multiply` | `( Float32_4x4 b ) -> Float32_4x4` | 矩阵乘 |
| `transformPoint` | `( Float32_3 v ) -> Float32_3` | 变换点（w 补 1，带平移） |
| `transformDirection` | `( Float32_3 v ) -> Float32_3` | 变换方向（w 补 0，忽略平移） |
| `transpose` / `determinant` / `inverse` / `clone` | 同 3x3 | 同语义 |
| `toFloat32_3x3` | `() -> Float32_3x3` | 降维到 2D 仿射：x/y 基取前两列，平移分量落第三列，底行 (0, 0, 1) |
| `identity` / `zero` | `static get` | — |
| `translation` | `static ( x, y, z )` 或 `static ( Float32_3 t )` | 平移 |
| `scale` | `static ( x, y, z )` / `static ( Float32_3 s )` / `static ( Float32 s )` | 三参 / 向量 / 等比 |
| `rotationX / Y / Z` | `static ( Float32 radians )` | 绕轴旋转（弧度） |
| `rotationAxis` | `static ( Float32_3 axis, Float32 radians )` | 绕任意轴（axis 内部归一化） |
| `trs` | `static ( Float32_3 t, Float32_3 euler, Float32_3 s )` | 局部 TRS：`T * Ry * Rx * Rz * S`（纯 SL 组合） |
| `perspective` | `static ( fovYRadians, aspect, near, far )` | 透视投影（右手系，depth 映射 [-1, 1]） |
| `ortho` | `static ( left, right, bottom, top, near, far )` | 正交投影 |
| `lookAt` | `static ( Float32_3 eye, target, upHint )` | 视图矩阵（右手系） |

### 4.3 systemCalls 映射表

SL 方法与 cvm 实现符号一一对应（`Math.jsonc` 声明 → `cvm_math_lib/float*_vm_method_call.c`）：

| SL 方法 | systemCall 名 | cvm 函数（Float32 组） |
|------|------|------|
| `multiply` / `_mul_` | `SystemMathMat3Mul` | `math_lib.dll!mathvm_mat3f_mul` |
| `transform` | `SystemMathMat3MulVec` | `math_lib.dll!mathvm_mat3f_mul_vec` |
| `transpose` | `SystemMathMat3Transpose` | `math_lib.dll!mathvm_mat3f_transpose` |
| `determinant` | `SystemMathMat3Determinant` | `math_lib.dll!mathvm_mat3f_determinant` |
| `inverse` | `SystemMathMat3Inverse` | `math_lib.dll!mathvm_mat3f_inverse` |
| `_add_` | `SystemMathMat3Add` | `math_lib.dll!mathvm_mat3f_add` |
| `_eq_` | `SystemMathMat3Eq` | `math_lib.dll!mathvm_mat3f_eq` |
| `clone` | `SystemMathMat3Copy` | `math_lib.dll!mathvm_mat3f_copy` |
| `identity` | `SystemMathMat3Identity` | `math_lib.dll!mathvm_mat3f_identity` |
| `rotationX / Y / Z` | `SystemMathMat3RotationX / Y / Z` | `math_lib.dll!mathvm_mat3f_rotation_x / _y / _z` |
| `scale` | `SystemMathMat3Scale` | `math_lib.dll!mathvm_mat3f_scale` |
| `translation` | `SystemMathMat3Translation` | `math_lib.dll!mathvm_mat3f_translation` |

4x4 组同构（`SystemMathMat4*` → `mathvm_mat4f_*`，含 `TransformPoint` / `TransformDirection` / `RotationAxis` / `Perspective` / `Ortho` / `LookAt`）；`trs` 例外，为纯 SL 组合调用其余工厂。

**out 参数模式**：返回矩阵 / 向量的调用一律 `returnType: "void"`，SL 侧先构造结果对象，系统调用把结果直写该对象的字段内存；只有 `determinant`（标量）与 `_eq_`（bool）直接返回值。

---

## 5. Float64 / Float16 系列差异

| 项 | Float64 系列（Mat3d / Mat4d） | Float16 系列（Mat3h / Mat4h） |
|------|------|------|
| systemCall 前缀 | `SystemMathMat3d*` / `Mat4d*` → `mathvm_mat3d_*` / `mat4d_*` | `SystemMathMat3h*` / `Mat4h*` → `mathvm_mat3h_*` / `mat4h_*` |
| 标量参数 / `determinant` 返回 | `Float64` | SL 侧为 `Float16`；jsonc 声明为 `Float32`（前端 F16↔F32 自动收敛，C 侧按 float 处理后写回半精度字段） |
| 转换方法 | `toFloat32_3x3()`；`Float64_4x4` 另有 `toFloat64_3x3()` 与 `toFloat32_4x4()` | `toFloat32_3x3()` / `toFloat32_4x4()` |
| 跨精度构造 | `Float64_4x4._init_( Float64_3x3 m )`、`_init_( Float32_4x4 m )` | `Float16_3x3 / 4x4` 可由 Float32 对应类型构造 |
| 静态工厂标量 | `rotationX / Y / Z( Float64 radians )` 等 | `rotationX / Y / Z( Float16 radians )` 等 |

---

## 6. 使用示例

```sl
# 2D 仿射：平移 + 旋转
Float32_3x3 t = Float32_3x3.translation( 100.0f, 50.0f )
Float32_3x3 r = Float32_3x3.rotationZ( 0.785f )       # 45°
Float32_3x3 m = t * r                                  # 先旋转后平移
Float32_3 p = m.transform( Float32_3( 10.0f, 0.0f, 1.0f ) )

# 3D：TRS 模型矩阵 → 视图 → 投影
Float32_4x4 model = Float32_4x4.trs(
    Float32_3( 0.0f, 1.0f, 0.0f ),
    Float32_3( 0.0f, 0.785f, 0.0f ),
    Float32_3.one() )
Float32_4x4 view = Float32_4x4.lookAt(
    Float32_3( 0.0f, 2.0f, -5.0f ),
    Float32_3.zero(),
    Float32_3.up() )
Float32_4x4 proj = Float32_4x4.perspective( 0.9f, 1.77f, 0.1f, 100.0f )

Float32_4x4 mvp = proj * view * model
Float32_3 world = model.transformPoint( Float32_3( 1.0f, 0.0f, 0.0f ) )
Float32_3 fwd   = model.transformDirection( Float32_3.forward() )

# 求逆（不可逆返回零矩阵）
Float32_4x4 inv = model.inverse()
if inv == Float32_4x4.zero()
{
    Console.println( "not invertible" )
}
```
