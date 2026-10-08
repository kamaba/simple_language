# Math（数学库总览）

`Math` 模块是 SimpleLanguage 的数学库，位于 `source/Front/Lib/Math/`，工程配置为 `Math.jsonc`，全部类声明在 `Math` 命名空间下。

覆盖三大块：

- **三精度数学函数**：`Mathd`（Float64）/ `Mathf`（Float32）/ `Mathh`（Float16）
- **向量与矩阵值类型**：2/3/4 维向量、3x3 / 4x4 矩阵，各提供 Float16 / Float32 / Float64 三种精度
- **大数与数论**：`BigNumber`（大整数）、`BigDecimal`（精确小数）、`Factor`（数论工具）

---

## 1. 类清单

| 类 | 精度 | 别名（@Nickname） | 说明 |
|------|------|------|------|
| `Math.Mathd` | Float64 | — | 数学函数（双精度，基准实现） |
| `Math.Mathf` | Float32 | — | 数学函数（单精度，委托 Mathd 中转） |
| `Math.Mathh` | Float16 | — | 数学函数（半精度，委托 Mathf 中转） |
| `Math.Float32_2` | Float32 | Point / Vector2 / Vec2 / float2 | 2D 向量 |
| `Math.Float32_3` | Float32 | Point3D / Vector3 / Vec3 / float3 | 3D 向量 |
| `Math.Float32_4` | Float32 | F4 / Vector4 / Vec4 / float4 | 4D 向量 |
| `Math.Float64_2` | Float64 | PointD / Vector2d / Vec2d / double2 | 2D 向量（双精度） |
| `Math.Float64_3` | Float64 | Vector3d / Vec3d / double3 | 3D 向量（双精度） |
| `Math.Float64_4` | Float64 | F4d / Vector4d / Vec4d / double4 | 4D 向量（双精度） |
| `Math.Float16_2` | Float16 | PointH / Vector2h / Vec2h / half2 | 2D 向量（半精度） |
| `Math.Float16_3` | Float16 | Point3Dh / Vector3h / Vec3h / half3 | 3D 向量（半精度） |
| `Math.Float16_4` | Float16 | Vector4h / Vec4h / half4 | 4D 向量（半精度） |
| `Math.Float32_3x3` | Float32 | Matrix3x3 / Mat3 / float3x3 | 3x3 矩阵 |
| `Math.Float32_4x4` | Float32 | Matrix4x4 / Mat4 / float4x4 | 4x4 矩阵 |
| `Math.Float64_3x3` | Float64 | Matrix3x3d / Mat3d / double3x3 | 3x3 矩阵（双精度） |
| `Math.Float64_4x4` | Float64 | Matrix4x4d / Mat4d / double4x4 | 4x4 矩阵（双精度） |
| `Math.Float16_3x3` | Float16 | Mat3h / half3x3 | 3x3 矩阵（半精度） |
| `Math.Float16_4x4` | Float16 | Mat4h / half4x4 | 4x4 矩阵（半精度） |
| `Math.Matrix` | Float32 | Mat | 通用动态矩阵（`Array<Float32>` 行主序，维度任意） |
| `Math.BigNumber` | — | BigInt | 大整数（约 256 位十进制） |
| `Math.BigDecimal` | — | Decimal | 精确十进制小数 |
| `Math.Factor` | — | — | 数论 / 因子静态工具 |

另有 3 个源文件存在但默认不参与编译（`Math.jsonc` 中 `ignore: true`）：`Int32_2`、`Quaternion`、`Rect`。

---

## 2. 三精度体系

同一 API 面在三种浮点精度下平行提供，命名约定：

| 精度 | 函数类 | 矩阵调用前缀 | 标量后缀示例 |
|------|------|------|------|
| Float64（双精度） | `Mathd` | `SystemMathMat3d*` / `Mat4d*` | `mathd_*` |
| Float32（单精度） | `Mathf` | `SystemMathMat3*` / `Mat4*` | `mathf_*` |
| Float16（半精度） | `Mathh` | `SystemMathMat3h*` / `Mat4h*` | 标量走 Float32 中转 |

函数类之间的依赖链（精度中转策略）：

```
Mathh (Float16) ──委托──> Mathf (Float32) ──委托──> Mathd (Float64)
```

- `Mathd`：FFI 绑定 `math_lib.dll` 的 `mathd_*`（C ABI），DLL 不可用时自动回退纯 SL 实现（泰勒级数 / 牛顿迭代）
- `Mathf`：FFI 绑定 `mathf_*`，回退实现委托 `Mathd` 以 Float64 中转
- `Mathh`：无 FFI、无系统调用，全部委托 `Mathf` 以 Float32 中转

---

## 3. 性能分层（system method call 使用总览）

Math 库是 system method call 的典型使用者，四个层级：

| 层级 | 机制 | 典型使用者 | 位置 |
|------|------|------|------|
| 纯 SL 解释执行 | — | 向量类全部方法、`BigNumber` / `BigDecimal` / `Factor` | 各 `.sl` |
| FFI `@DllImport` | 普通 C ABI 调用 `math_lib.dll`，失败自动走 SL fallback | `Mathd` / `Mathf` 标量函数、`Float32_4x4` 元素级 `dot3` | `cvm_math_lib/math_lib.c` |
| systemCalls → 扩展 DLL | `"math_lib.dll!mathvm_*"`，C 侧直读 VM 栈与对象内存 | 6 个矩阵值类型的全部重运算（乘 / 加 / 转置 / 行列式 / 求逆 / 变换 / 工厂） | `cvm_math_lib/float*_3x3/4x4_vm_method_call.c` |
| systemCalls → 主工程符号 | `vm_sys_math_*` 等不带 `!` 的符号，在 VM 自身模块内解析 | `Matrix` 的矩阵乘 / 转置（`SystemMathMatMulFloat32` 等）、`BigNumber` / `Factor` 用到的 `SystemArray*`（声明于 Core 模块） | `csimple_lang/src/vm/system_method_call/` |

如何在 cvm 侧新增这类绑定（函数签名、栈约定、jsonc 与工程配置），见 [project-system-method-call-guide.md](../../project/project-system-method-call-guide.md)。

---

## 4. 工程配置速览（Math.jsonc）

```jsonc
{
  // FFI 别名表：@DllImport("math_lib", ...) 首参按别名查此表替换为 path
  "dllImports": [
    { "path": "../../out/export/Math/math_lib.dll", "name": "math_lib", "alias": "math_lib" }
  ],
  // vmDlls：导出 Math 时 Front 用 MSBuild 编译该 VS 工程，
  // 并把产出的 math_lib.dll 拷贝到与 Math.module.json 相同的目录
  "vmDlls": [
    { "project": "../../../../project/cvm_math_lib/cvm_math_lib.vcxproj",
      "name": "math_lib.dll", "configuration": "Debug", "platform": "x64" }
  ],
  // systemCalls：矩阵运算下沉 cvm 声明；
  // cvmFunction 的 DllName 必须与上方 vmDlls.name 一致
  "systemCalls": [
    { "name": "SystemMathMat3Mul", "returnType": "void",
      "params": ["object", "object", "object"],
      "isVariadic": false, "cvmFunction": "math_lib.dll!mathvm_mat3f_mul" },
    { "name": "SystemMathMatMulFloat32", "returnType": "void",
      "params": ["Array<object>", "Array<object>", "Array<object>", "Int32", "Int32", "Int32"],
      "isVariadic": false, "cvmFunction": "vm_sys_math_matmul_float32" }
    // ... 共 86 条（6 组矩阵 + Matrix 2 条）
  ],
  "references": [
    { "path": "../Core", "name": "Core" },
    // Std 必须引用：@DllImport 依赖 Std 模块的 FFI.Library / FFI.StaticLibrary
    { "path": "../Std", "name": "Std" }
  ]
}
```

要点：

- `dllImports` 服务 FFI（`@DllImport`），`vmDlls` + `systemCalls` 服务 VM 系统调用，两条链路独立
- `vmDlls.name`（DLL 文件名）必须与 `systemCalls.cvmFunction` 的 `DllName!` 前缀一致
- 模块引用 `Core`（提供 `SystemArray*` 系统调用声明）与 `Std`（提供 FFI 基础类）

---

## 5. 快速上手

```sl
# 三角函数（三精度同名）
Float64 a = Math.Mathd.sin( 0.5 )
Float32 b = Math.Mathf.sin( 0.5f )
Float16 c = Math.Mathh.sin( 0.5h )

# 向量
Math.Float32_3 v = Math.Float32_3( 1.0f, 2.0f, 3.0f )
Float32 len = v.length()
Math.Float32_3 n = v.normalize()

# 矩阵（运算下沉 cvm）
Math.Float32_4x4 m = Math.Float32_4x4.trs(
    Math.Float32_3( 0.0f, 0.0f, 0.0f ),
    Math.Float32_3( 0.0f, 0.785f, 0.0f ),
    Math.Float32_3( 1.0f, 1.0f, 1.0f ) )
Math.Float32_3 p = m.transformPoint( Math.Float32_3( 1.0f, 0.0f, 0.0f ) )

# 大数
Math.BigNumber fact100 = Math.BigNumber.factorial( 100 )
Math.BigDecimal price = Math.BigDecimal.parse( "19.99" )
```

同命名空间模块内（`Math.jsonc` 覆盖的文件之间）可直接短名调用，如 `Mathf.sqrt(...)`；跨模块使用 `Math.` 前缀全名。

---

## 6. 文档索引

| 文档 | 内容 |
|------|------|
| [Math-Functions.md](./Math-Functions.md) | `Mathd` / `Mathf` / `Mathh` 常量与全部函数签名、FFI 与 fallback 策略 |
| [Math-Vectors.md](./Math-Vectors.md) | 9 个向量类（Float16/32/64 × 2/3/4 维）完整 API |
| [Math-Matrices.md](./Math-Matrices.md) | 6 个矩阵值类型（3x3/4x4 × 三精度）完整 API 与 systemCalls 映射 |
| [Matrix.md](./Matrix.md) | 通用动态矩阵 `Matrix`（任意维度） |
| [Math-BigNumbers.md](./Math-BigNumbers.md) | `BigNumber` / `BigDecimal` / `Factor` |
