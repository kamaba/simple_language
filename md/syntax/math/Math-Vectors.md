# Math-Vectors（向量：Float16 / Float32 / Float64 × 2 / 3 / 4 维）

`Math` 命名空间下 9 个向量类，全部为 public 字段 + 运算符重载的轻量值类型。三种精度的 API 面同构，以 `Float32` 系列为基准描述，其余两系列只列差异。

| 类 | 别名（@Nickname） |
|------|------|
| `Math.Float32_2` | Point / Vector2 / Vec2 / float2 |
| `Math.Float32_3` | Point3D / Vector3 / Vec3 / float3 |
| `Math.Float32_4` | F4 / Vector4 / Vec4 / float4 |
| `Math.Float64_2` | PointD / Vector2d / Vec2d / double2 |
| `Math.Float64_3` | Vector3d / Vec3d / double3 |
| `Math.Float64_4` | F4d / Vector4d / Vec4d / double4 |
| `Math.Float16_2` | PointH / Vector2h / Vec2h / half2 |
| `Math.Float16_3` | Point3Dh / Vector3h / Vec3h / half3 |
| `Math.Float16_4` | Vector4h / Vec4h / half4 |

以下签名以 `Float32` 系列书写；`Float64` / `Float16` 系列把标量类型对应替换即可。

---

## 1. 字段与索引访问

```sl
public Float32 x = 0.0f
public Float32 y = 0.0f
public Float32 z = 0.0f    # 仅 _3 / _4
public Float32 w = 0.0f    # 仅 _4
```

支持 `[]` 索引（`_getItem_` / `_setItem_`，越界安全：读返回 0，写忽略）：

```sl
Float32_3 v = Float32_3( 1.0f, 2.0f, 3.0f )
Float32 first = v[0]     # 1.0f
v[1] = 20.0f
```

---

## 2. 构造

| 构造 | _2 | _3 | _4 | 说明 |
|------|----|----|----|----|
| `_init_()` | ✔ | ✔ | ✔ | 全部分量置 0 |
| `_init_( x, y [, z [, w]] )` | ✔ | ✔ | ✔ | 按分量 |
| `_init_( v )` | ✔ | ✔ | ✔ | 标量广播，全分量 = v |
| `_init_( Float32_2 v, Float32 z )` | — | ✔ | — | 2D 提升 + z |
| `_init_( Float32_3 v, Float32 w )` | — | — | ✔ | 3D 提升 + w |

---

## 3. 运算符重载

| 运算符 | 签名（_3 版） | 行为 |
|------|------|------|
| `+` | `_add_( Float32_3 v ) -> Float32_3` | 逐分量相加 |
| `-` | `_sub_( Float32_3 v ) -> Float32_3` | 逐分量相减 |
| `*` | `_mul_( Object obj1 ) -> Float32_3` | 向量 × 向量 = 逐分量相乘；向量 × 标量 = 数乘 |
| `/` | `_truediv_( Object obj1 ) -> Float32_3` | 向量 / 向量 = 逐分量相除；向量 / 标量 = 数除 |
| `==` | `_eq_( Float32_3 v ) -> bool` | 逐分量精确相等 |
| `!=` | `_ne_( Float32_3 v ) -> bool` | 取反 |

同类型二元运算形参收窄为自身类型（静态分派）；标量乘 / 除保持 `Object` 动态分派。类型不匹配时 `*` / `/` 原样返回 `this`。

```sl
Float32_3 a = Float32_3( 1.0f, 2.0f, 3.0f )
Float32_3 b = a * 2.0f          # (2, 4, 6)
Float32_3 c = a + b             # (3, 6, 9)
```

---

## 4. 实例方法

| 方法 | _2 | _3 | _4 | 说明 |
|------|----|----|----|----|
| `dot( other ) -> Float32` | ✔ | ✔ | ✔ | 点积 |
| `lengthSquared() -> Float32` | ✔ | ✔ | ✔ | 长度平方 |
| `length() -> Float32` | ✔ | ✔ | ✔ | 模长（内部走 `Mathf.sqrt`，FFI 加速） |
| `normalize() -> Vector` | ✔ | ✔ | ✔ | 返回归一化**新对象**（返回值语义，不修改 this）；零向量返回零向量 |
| `distance( other ) -> Float32` | ✔ | ✔ | ✘ | 两点欧氏距离 |
| `cross( other )` | ✔（返回标量） | ✔（返回向量） | ✘ | _2：2D 叉积标量 `x1*y2 - y1*x2`；_3：三维叉积向量 |
| `lerp( other, Float32 t ) -> Vector` | ✔ | ✔ | ✔ | 线性插值（新对象） |
| `scale( Float32 s ) -> Vector` | ✔ | ✔ | ✘ | 数乘（新对象） |
| `negate() -> Vector` | ✔ | ✔ | ✔ | 取反（新对象） |
| `reflect( Float32_3 normal ) -> Float32_3` | ✘ | ✔ | ✘ | 绕法线反射（normal 需已归一化），仅 _3 |
| `set( x, y [, z] ) -> this` | ✔ | ✔ | ✘ | 原地写分量并返回 this（链式） |
| `clone() -> Vector` | ✔ | ✔ | ✔ | 深拷贝（值类型语义复制） |

```sl
Float32_3 v  = Float32_3( 3.0f, 0.0f, 4.0f )
Float32 len  = v.length()          # 5.0
Float32_3 n  = v.normalize()       # (0.6, 0, 0.8)，v 不变

Float32_3 fwd = Float32_3.forward()
Float32_3 r   = fwd.reflect( Float32_3.up() )   # 绕法线反射
```

---

## 5. 静态常量与静态工具

静态常量（`public static get`，每次返回新对象）：

| 常量 | _2 | _3 | _4 | 值 |
|------|----|----|----|----|
| `zero` | ✔ | ✔ | ✔ | 全 0 |
| `one` | ✔ | ✔ | ✔ | 全 1 |
| `up` | ✔ | ✔ | ✘ | (0, 1[, 0]) |
| `down` | ✔ | ✔ | ✘ | (0, -1[, 0]) |
| `left` | ✔ | ✔ | ✘ | (-1, 0[, 0]) |
| `right` | ✔ | ✔ | ✘ | (1, 0[, 0]) |
| `forward` | ✘ | ✔ | ✘ | (0, 0, 1) |
| `back` | ✘ | ✔ | ✘ | (0, 0, -1) |

静态工具方法：

| 方法 | _2 | _3 | _4 |
|------|----|----|----|
| `dot( a, b ) -> Float32` | ✔ | ✔ | ✔ |
| `distance( a, b ) -> Float32` | ✔ | ✔ | ✘ |
| `cross( a, b )` | ✘ | ✔ | ✘ |
| `lerp( a, b, t ) -> Vector` | ✔ | ✔ | ✔ |

---

## 6. Float64 / Float16 系列差异

三系列 API 同构（把 `Float32` 换成 `Float64` / `Float16`），仅以下差异：

| 项 | Float64 系列 | Float16 系列 |
|------|------|------|
| 精度中转构造 | `_init_( Float32_N v )`：由 Float32 向量逐分量转 Float64 | `_3` / `_4` 提供 `_init_( Float32_N v )`：由 Float32 向量转 Float16 |
| 转换方法 | `toFloat32_N() -> Float32_N`（单向，无 Float16 转换） | `toFloat32_N() -> Float32_N` 与 `toFloat64_N() -> Float64_N`（双向） |
| 运算实现 | 内部标量运算委托 `Mathd` | 内部标量运算委托 `Mathf` 以 Float32 中转 |

```sl
Float32_3 f = Float32_3( 1.0f, 2.0f, 3.0f )

Float64_3 d = Float64_3( f )       # Float32 -> Float64
Float32_3 back = d.toFloat32_3()

Float16_3 h = Float16_3( f )       # Float32 -> Float16
Float32_3 f2 = h.toFloat32_3()
Float64_3 d2 = h.toFloat64_3()
```

---

## 7. 使用示例

```sl
# 位移与距离
Float32_2 p1 = Float32_2( 0.0f, 0.0f )
Float32_2 p2 = Float32_2( 3.0f, 4.0f )
Console.println( p1.distance( p2 ).toString() )     # 5

# 方向向量与反射
Float32_3 dir  = Float32_3( 1.0f, -1.0f, 0.0f ).normalize()
Float32_3 nrm  = Float32_3.up()
Float32_3 refl = dir.reflect( nrm )

# 运算符
Float32_3 sum = Float32_3.one() * 0.5f + Float32_3( 1.0f, 0.0f, 0.0f )
bool same = ( sum == sum.clone() )                   # true
```
