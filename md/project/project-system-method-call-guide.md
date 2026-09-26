# System Method Call 绑定指南（CVM 原生方法制作 / jsonc 配置 / 工程配置）

S 语言将热点方法下沉到 C VM（cvm）原生实现的完整链路指南：如何在 CVM 扩展 DLL 里编写绑定函数、在库 jsonc 里声明 `systemCalls` / `vmDlls` / `dllImports`、以及 VS 工程如何配置。以 Math 库（`source/Front/Lib/Math/`）与 `cvm_math_lib` 工程为完整范例。

**适用读者**：需要把 SL 热点代码（数值计算、批量数组操作等）下沉为原生实现的库/模块作者。

---

## 1. 概述：三种原生绑定方式

S 语言调用原生代码共有三条通路，全部由 `math_lib.dll` 这一范例工程同时示范：

| 方式 | jsonc / SL 侧写法 | C 函数签名 | 实现位置 | 特点 |
|---|---|---|---|---|
| ① FFI `@DllImport` | jsonc `dllImports` 别名表 + SL `@DllImport( "math_lib", "mathf_sin", "Float32->Float32" )` | 普通 C ABI：`float mathf_sin(float x)` | `cvm_math_lib/math_lib.c` | 无 VM 栈依赖；**加载失败自动执行 SL 方法体兜底** |
| ② systemCalls → 扩展 DLL | jsonc `systemCalls` + `"cvmFunction": "math_lib.dll!mathvm_mat3f_mul"` | `int32 f(VM* vm, int32 param_count)` | `cvm_math_lib/*_vm_method_call.c` | 可读写 VM 求值栈与 VM 对象成员；由 `OpCode_CallSystemMethod` 直接派发 |
| ③ systemCalls → 主工程符号 | jsonc `systemCalls` + `"cvmFunction": "vm_sys_math_matmul_float32"`（不带 `!`） | 同② | `csimple_lang/src/vm/system_method_call/array_system_method.c` 等 | 函数编进 cvm 自身；适合通用基础设施（数组、字符串） |

**选型建议**：

- 纯标量计算（sin/pow 等）且希望有 SL fallback → 方式①；
- 库专属的类型运算（矩阵/向量等需要按对象指针直写字段内存）→ 方式②，独立 DLL 便于按库分发；
- 跨库通用能力（数组批量操作等）→ 方式③，直接进 csimple_lang 源码树。

方式②是本文重点；①③在关键差异处对照说明。

---

## 2. 制作 CVM 扩展 DLL（方式②）

### 2.1 函数签名约定

每个绑定函数必须满足（见 [system_method_registry.h](../../../csimple_lang/src/vm/system_method_call/system_method_registry.h) 的 `VMSystemFunc`）：

```c
SLVM_FUNC_EXPORT int32 mathvm_xxx(VM* vm, int32 param_count);
```

- **参数已在求值栈上**：调用前 IR 已按参数顺序压栈，**最后一个参数在栈顶**，C 侧按逆序 pop；
- **返回值由实现自己压回求值栈**（`slvm_push_f32` 等）；
- 返回 `TRUE(1)` 成功 / `FALSE(0)` 失败（含 `param_count` 不符、栈 pop 失败等防御分支）。

### 2.2 引入 sl_vm_ext_api.h（外部工程唯一依赖）

[sl_vm_ext_api.h](../../../csimple_lang/src/vm/system_method_call/sl_vm_ext_api.h) 是扩展 DLL 的公共 API 头，**自包含**（外部工程无需引用 csimple_lang 内部头文件）：

- `SLVmExtVm`：真实 VM（`vm_runtime.h` 的 `struct _VM`）的**前缀镜像**，从结构体起点到 `stack_slot_depth` 的字段偏移完全一致（指针统一以 `void*` 表达）。扩展只允许访问镜像内字段（`sp` / `stack` / `stack_slot_kind` / `stack_slot_depth` 等）；
- `SLVM_FUNC_EXPORT`：导出宏（Windows `__declspec(dllexport)` / 非 Windows `__attribute__((visibility("default")))`），**cvmFunction 按 `GetProcAddress` 名字解析，函数必须进导出表**；
- `SLVmExtStackSlotKind`：槽类型枚举（INT32=1 … NULL=18），与 `vm_runtime.h` 的 `VMStackSlotKind` 完全一致；
- `SLVM_EXT_VM_STACK_SIZE = 8192`：与 `VM_STACK_SIZE` 一致。

**布局同步保障**：csimple_lang 工程内的 `sl_vm_ext_api_check.c` 在编译期校验镜像偏移与槽类型枚举值。一旦 `vm_runtime.h` 漂移即可阻止构建，此时必须同步修改 `sl_vm_ext_api.h`——因此**扩展工程只 include 这一个头，不要复制内部结构定义**。

### 2.3 求值栈 push / pop 帮助函数

头文件内置静态帮助函数（与 cvm 内部 `system_method_helpers.c` 语义一致）：

| 函数 | 说明 |
|---|---|
| `slvm_push_i32 / slvm_push_bool / slvm_push_f32 / slvm_push_f64` | 压栈并写槽类型标记（`stack_slot_depth++`） |
| `slvm_pop_f64(vm, &out)` | 弹栈顶按 float64 解释：Float64/Float32 直接取值（F32 拓宽）、整数槽按位宽转换、NULL 槽输出 0.0；**float8/float16 位模式槽不支持（返回 FALSE）** |
| `slvm_pop_f32(vm, &out)` | 同上，输出收窄为 float32 |
| `slvm_pop2_f64 / slvm_pop2_f32` | 弹两个 float 参数：**先弹出的是最后一个参数（栈顶），后弹出的是第一个参数** |

### 2.4 参数栈序与完整示例

以 `Float32_3x3` 矩阵乘为例（`float32_3x3_vm_method_call.c`）。SL 侧调用 `SystemMathMat3Mul( this, b, r )`，压栈顺序 a→b→r，**r（out）在栈顶**，C 侧逆序 pop：

```c
SLVM_FUNC_EXPORT int32 mathvm_mat3f_mul(VM* vm, int32 param_count)
{
    float* a = NULL; float* b = NULL; float* o = NULL;
    int32 r = 0; int32 c = 0;

    if (param_count != 3) { return FALSE; }
    if (!mathvm_mat3f_pop(vm, &o) ||   /* out：栈顶 */
        !mathvm_mat3f_pop(vm, &b) ||
        !mathvm_mat3f_pop(vm, &a))
    {
        return FALSE;
    }

    for (r = 0; r < 3; r++)
        for (c = 0; c < 3; c++)
            o[r * 3 + c] = a[r * 3 + 0] * b[0 * 3 + c]
                         + a[r * 3 + 1] * b[1 * 3 + c]
                         + a[r * 3 + 2] * b[2 * 3 + c];
    return TRUE;
}
```

返回标量的两类典型：

```c
/* determinant：params ["object"]，returnType "Float32" */
return slvm_push_f32(vm, det) ? TRUE : FALSE;

/* eq：params ["object","object"]，returnType "bool" */
return slvm_push_bool(vm, eq) ? TRUE : FALSE;
```

标量 + 对象混合参数（如 `SystemMathMat3RotationX( radians, out )`，params `["Float32","object"]`）：先 pop 栈顶对象（out），再 `slvm_pop_f32` 取标量，顺序与参数声明严格相反。

### 2.5 out 参数模式（返回对象的统一做法）

**外部 DLL 无法分配 VM 对象**（需要 `vm->obj_pool` + RuntimeClass 体系）。因此凡是结果为对象的运算，统一走 out 参数：

1. **SL 侧先构造结果对象再传入**（[Float32_3x3.sl](../../source/Front/Lib/Math/Float32_3x3.sl)）：

```sl
Float32_3x3 multiply( Float32_3x3 b )
{
    Float32_3x3 r = Float32_3x3()
    SystemMathMat3Mul( this, b, r )     # r 作为最后一个参数（栈顶）
    ret r
}
```

2. **C 侧直接改写对象的 member_data**（即字段内存）；
3. **jsonc 中 returnType 声明为 `"void"`**（返回值经 out 对象带出，无栈上返回）。

### 2.6 VMObject 桥接（对象参数读写）

`sl_vm_ext_api.h` 只提供标量 push/pop；对象参数经 [math_vm_object_bridge.h](../../source/Front/Lib/Math/cvm_math_lib/math_vm_object_bridge.h) 桥接（x64 布局与 `vm_object.h` 的 `struct _VMObject` 严格一致）：

```c
偏移  0 : header（uint64 联合）
偏移  8 : member_data（uint8*）
偏移 16 : member_data_size（uint32）
偏移 24 : member_runtime_objects ...
```

- **member_data 按字段声明顺序紧凑排布、无对齐填充**：Float32=4B、Float64=8B、Float16=2B（位模式）、引用=8B。`Float32_3x3` 的 m00..m22 即 `member_data[0..36)` 的 `float[9]`（行主序），`Float32_3` 的 x/y/z 即 `member_data[0..12)`；
- `mathvm_pop_object(vm, &obj)`：弹出对象槽（PTR=8 字节；NULL 槽输出 NULL 视为成功）；
- `mathvm_obj_is_valid(obj, expect_member_size)`：防御检查（地址 >= 0x10000、8 字节对齐、member_data 非空、尺寸与预期一致，如 3x3 矩阵 = 36）。SL 类型系统已保证调用点类型正确，这里是运行期兜底；
- Float16 矩阵额外提供 `mathvm_half_to_float` / `mathvm_float_to_half`（IEEE 754 binary16 位模式 ↔ float32，round-to-nearest-even）。

通用模式封装（每个类型一个 pop 帮助）：

```c
static int32 mathvm_mat3f_pop(VM* vm, float** out_mat)
{
    void* obj = NULL;
    if (!mathvm_pop_object(vm, &obj))      { return FALSE; }
    if (!mathvm_obj_is_valid(obj, 36))     { return FALSE; }
    *out_mat = (float*)MATHVM_OBJ_MEMBER_DATA(obj);
    return TRUE;
}
```

### 2.7 Float16 参数的两个注意点

1. **标量参数在 jsonc 中声明为 `Float32`**（前端 F16↔F32 自动收敛；`slvm_pop_f64` 不支持 float16 位模式槽）。对照 Math.jsonc：`SystemMathMat3hRotationX` 的 params 是 `["Float32", "object"]`，determinant 的 returnType 是 `"Float32"`；
2. 对象成员数据中 Float16 以 **uint16 位模式**存储，C 侧计算前需经 `mathvm_half_to_float` 转换。

### 2.8 VS 工程配置（vcxproj）

参照 [cvm_math_lib.vcxproj](../../project/cvm_math_lib/cvm_math_lib.vcxproj)：

| 配置项 | 值 | 说明 |
|---|---|---|
| ConfigurationType | `DynamicLibrary` | 产出 DLL |
| Platform | x64 | **必须与 csimple_lang 相同架构**（镜像布局按 x64） |
| PlatformToolset | v145 | 与主工程一致 |
| AdditionalIncludeDirectories | `$(ProjectDir)..\..\..\csimple_lang\src\vm\system_method_call` | 指向 `sl_vm_ext_api.h` 所在目录（路径相对 vcxproj 位置换算） |
| ClCompile | 逐个列出 `math_lib.c` 与 `*_vm_method_call.c` | 源文件放在库目录 `cvm_math_lib/` 子目录下 |

同一个 DLL 可同时承载方式①（`math_lib.c` 的 `mathf_*`/`mathd_*` 普通 C ABI）与方式②（`*_vm_method_call.c` 的 `mathvm_*` VM 函数），互不冲突——FFI 与 systemCalls 的解析链路彼此独立。

---

## 3. jsonc 配置（以 Math.jsonc 为例）

### 3.1 systemCalls 字段

```jsonc
"systemCalls": [
  { "name": "SystemMathMat3Mul",         // SL 侧调用名（全局可见，建议 System 前缀 + 库名）
    "returnType": "void",                // "void" / "Float32" / "Float64" / "bool" / "Int32" ...
    "params": ["object", "object", "object"],  // 按参数顺序；object = VM 对象；Array<object> = VMArray
    "isVariadic": false,
    "cvmFunction": "math_lib.dll!mathvm_mat3f_mul" },  // C 实现符号（见 3.2）
  { "name": "SystemMathMat3Determinant", "returnType": "Float32", "params": ["object"],
    "isVariadic": false, "cvmFunction": "math_lib.dll!mathvm_mat3f_determinant" }
]
```

字段要点：

| 字段 | 说明 |
|---|---|
| `name` | SL 代码中的直接调用名。**必须与 C 实现的 pop 顺序、个数一致**（`params` 数组顺序 = 压栈顺序，最后一个是栈顶） |
| `returnType` | out 对象模式的运算一律 `"void"`；真正压栈返回的写实际类型 |
| `params` | `object`（单对象）、`Array<object>`（VMArray，主工程符号用）、标量类型名。**Float16 标量声明为 `Float32`** |
| `cvmFunction` | 见 3.2 |

### 3.2 cvmFunction 的两种形式

```
"DllName!symbol"   →  在外部扩展 DLL 中解析（LoadLibraryA + GetProcAddress）
"symbol"           →  在 cvm 自身模块内解析（GetModuleHandleExW FROM_ADDRESS + GetProcAddress）
```

- 带 `!` 形式：`DllName` **必须与 `vmDlls.name` 一致**（用于运行时预载与缓存命中），如 `"math_lib.dll!mathvm_mat3f_mul"`；
- 不带 `!` 形式：符号必须存在于 cvm 二进制内（方式③），如 Matrix 的 `"vm_sys_math_matmul_float32"`、Core 的 `"SystemArrayFillValue"` 系列。

### 3.3 vmDlls（构建规则 + 运行时预载声明）

```jsonc
"vmDlls": [
  { "project": "../../../../project/cvm_math_lib/cvm_math_lib.vcxproj",  // 路径相对本 jsonc 所在目录
    "name": "math_lib.dll",            // 产物名；cvmFunction 的 DllName 与运行时预载都以此为准
    "configuration": "Debug",
    "platform": "x64" }
]
```

两重作用：

1. **构建**：Front 导出模块时用 MSBuild 编译 `project` 指向的 VS 工程，并把产出的 DLL 拷贝到与 `Math.module.json` 相同的目录（`out/export/Math/`）；
2. **预载**：jsonc 的 `vmDlls` 被写入 module.json 的 `vmDllImports` 数组，cvm 加载程序集时**先按包目录预载**（见 4.2），DLL 因此不必放在标准搜索路径上。

### 3.4 dllImports 与 vmDlls 的区别（易混淆）

| | `dllImports` | `vmDlls` |
|---|---|---|
| 服务对象 | 方式① FFI `@DllImport` 的**库别名表** | 方式② systemCalls 的**构建 + 预载** |
| 机制 | 编译期经 ResolveDllImportPath 把别名替换为 `path`；path 运行时**相对进程 CWD** 解析 | MSBuild 构建 + 运行时按**包目录** LoadLibrary |
| 失败行为 | FFI 加载失败/符号缺失时自动执行 SL 方法体（fallback） | 预载失败打 warning，对应 systemCall 注册失败 |

Math.jsonc 两者并存且指向**同一个 DLL**：

```jsonc
"dllImports": [ { "path": "../../out/export/Math/math_lib.dll", "name": "math_lib", "alias": "math_lib" } ],
"vmDlls":     [ { "project": "../../../../project/cvm_math_lib/cvm_math_lib.vcxproj", "name": "math_lib.dll", "configuration": "Debug", "platform": "x64" } ]
```

### 3.5 references（FFI 需要而 systemCalls 不需要）

```jsonc
"references": [
  { "path": "../Core", "name": "Core" },
  // Std 必须引用：@DllImport 依赖 Std 模块的 FFI.Library / FFI.StaticLibrary
  // （source/Front/Lib/Std/FFI/Library.sl）。systemCalls（vmDll 路径）不依赖它。
  { "path": "../Std", "name": "Std" }
]
```

只用方式②③（不用 FFI）的模块可以不引 Std。

### 3.6 SL 侧调用写法

jsonc 声明后即可在任意 SL 代码中**直接按名调用**（无需 import / 声明）：

```sl
Float32_3x3 r = Float32_3x3()
SystemMathMat3Mul( a, b, r )              # 方式②：math_lib.dll!mathvm_mat3f_mul
Float32 det = SystemMathMat3Determinant( a )
SystemMathMatMulFloat32( ra, rb, rout, 3, 3, 3 )   # 方式③：主工程符号
```

---

## 4. 工程配置与运行时链路（配置后发生了什么）

### 4.1 导出：jsonc → module.json

Front 导出模块时把 `systemCalls` 写入 `Math.module.json`：每条含 `name`、`returnType`、`params`、`cvmFunction`，以及一个**唯一 int id**（由 `SystemMethodCallDeclaration.GetIndex` 基于 MD5 计算，随 systemCalls 条目与 CallSystemMethod 字节码 payload 一起携带）；`vmDlls` 写入 `vmDllImports` 数组（C 侧由 [slir_json_module_loader.c](../../../csimple_lang/src/vm/load/slir_json_module_loader.c) 解析）。

### 4.2 加载顺序（sl_runtime_assembly.c 的 build_from_load_model）

cvm 加载程序集时（[sl_runtime_assembly.c](../../../csimple_lang/src/vm/assembly/sl_runtime_assembly.c)）按固定顺序执行：

1. `vm_system_func_dynamic_registry_clear()` —— 动态注册表随程序集生命周期重建（重载时先清空）；
2. **预载扩展 DLL**：遍历所有包所有模块的 `vmDllImports`，`vm_system_func_preload_ext_lib(dll, 包目录)` 以 `包目录/ DLL名` 显式 LoadLibrary 并**按短名缓存**（失败打 warning `"vmDll import not preloaded"`，不缓存失败句柄）；与第 3 步顺序无关（注册时按 DLL 名命中缓存）；
3. **注册 systemCalls**：逐条 `vm_system_func_dynamic_register(id, name, cvmFunction)`，**双注册**——name 表（FNV-1a 哈希，服务遗留 by-name 回退路径）+ id 表（Knuth 乘法哈希，**O(1) 派发**）。失败（无 cvmFunction / 符号解析不到）打 warning：`system call not registered in cvm. name=%s id=%d`。

### 4.3 符号解析（system_method_registry.c）

`vm_sysfunc_resolve_symbol(cvmFunction)` 的两条路径：

- **含 `!`**：`vm_sysfunc_ext_lib_load(DllName)` —— 先查进程级 DLL 名缓存（句柄缓存整个进程生命周期），未命中则 `LoadLibraryA(DllName)`（标准搜索路径），再 `GetProcAddress(handle, symbol)`；
- **不含 `!`**：`vm_sysfunc_resolve_in_self(symbol)` —— `GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS, 本函数地址)` 拿到 cvm 自身模块句柄，再 `GetProcAddress`（非 Windows 为 `dlopen(NULL)` + `dlsym`）。

### 4.4 运行时派发

SL 调用编译为 `OpCode_CallSystemMethod` 字节码：优先以 payload 携带的 int id 走 id 表 **O(1) 定位**实现函数；未命中（如旧导出无 id / 注册失败）回退 by-name 查找；均未命中则运行时报错。

### 4.5 方式③（主工程符号）的工程配置

与方式②的差别仅在实现落点：C 函数直接写在 csimple_lang 源码树（如 `array_system_method.c` 的 `vm_sys_math_matmul_float32`），随 cvm 一起编译，jsonc 的 `cvmFunction` 不带 `!`、也不需要 `vmDlls`。其余（jsonc systemCalls 字段、SL 调用）完全相同。

以 `SystemMathMatMulFloat32`（通用 Matrix 的矩阵乘）为例，参数含 `Array<object>`，C 侧用 `vm_registry_try_pop_ptr` 取 VMArray、`vm_sys_array_f32_data` 取 `float32*` 数据指针，栈布局 "top-down: k, cols, rows, r, b, a"（同样逆序 pop）。

---

## 5. 检查清单与故障排查

### 5.1 新增一个 systemCall 的检查清单

- [ ] C 函数签名 `SLVM_FUNC_EXPORT int32 f(VM* vm, int32 param_count)`，函数名进导出表
- [ ] `param_count` 校验 + 全部 pop 分支失败返回 FALSE
- [ ] 参数 pop 顺序与 jsonc `params` **严格相反**（最后一个参数最先 pop）
- [ ] 返回对象走 out 参数（SL 侧先构造、C 侧写 member_data、returnType "void"）；返回标量用 `slvm_push_*` 压回
- [ ] Float16 标量参数声明 Float32；Float16 对象成员做位模式转换
- [ ] vcxproj：DynamicLibrary / x64 / include 指向 `csimple_lang/src/vm/system_method_call` / 源文件入 ClCompile
- [ ] jsonc：`systemCalls` 条目的 name / returnType / params / cvmFunction 与 C 实现一一对应
- [ ] `cvmFunction` 的 `DllName` 与 `vmDlls.name` 一致；`vmDlls.project` 路径相对 jsonc 正确
- [ ] `name` 全局不与其它模块的 systemCall 冲突（建议 `System` + 库 + 功能前缀）

### 5.2 常见 warning 与排查

| warning（日志） | 含义 | 排查 |
|---|---|---|
| `system call not registered in cvm. name=%s id=%d` | cvmFunction 为空，或符号未解析到 | 检查 jsonc 拼写、DLL 是否产出/预载成功、导出表是否含该符号（dumpbin /exports） |
| `vmDll import not preloaded: %s (dir=%s)` | 包目录下找不到 DLL | 确认 vmDlls 构建成功、DLL 已拷到 module.json 同目录 |
| `vmDll: LoadLibrary failed (GetLastError=...): 路径` | 显式路径加载失败 | 路径/权限/依赖缺失；x64 依赖需齐全 |
| `system method: LoadLibrary failed ...: 名字` | 标准搜索路径加载失败 | DLL 不在搜索路径且未被 vmDllImports 预载 |

### 5.3 与 FFI fallback 的行为差异

- **方式①（FFI）**：DLL 缺失/符号缺失时**静默走 SL 方法体**（Mathf 委托 Mathd、Mathd 走泰勒/牛顿纯 SL 实现），程序不中断；
- **方式②③（systemCalls）**：注册失败仅 warning，运行期调用时按未注册处理（无 SL 自动 fallback）——**下沉前保留 SL 参考实现**是良好实践（Float32_3x3 的 trs/perspective 等复杂组合仍保留纯 SL，仅算子级下沉）。

---

## 相关文档

- [project-config-jsonc-guide.md](project-config-jsonc-guide.md) —— 库 jsonc 整体结构（project/compile/compileFiles/struct 等段）
- [Math.md](../syntax/math/Math.md) —— Math 库总览（三精度体系与性能分层）
- [Math-Matrices.md](../syntax/math/Math-Matrices.md) —— 矩阵值类型的 systemCalls 映射全表
- [Matrix.md](../syntax/math/Matrix.md) —— 通用动态矩阵（主工程符号 + Array\<object\> 模式）
