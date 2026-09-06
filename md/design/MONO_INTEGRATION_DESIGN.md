# Mono 机制整合设计文档

> 目标：在 `csimple_lang`（Simple Language 的 C99 实现 + 自带 VM 运行时）中，
> 让 SL 代码能够**直接调用运行在 Mono 下的 C# 代码**（类、方法、属性）。
> 本文档说明整合的**设计、逻辑与落地路径**，不涉及最终实现代码。

---

## 1. 背景与目标

`csimple_lang` 目前是一个纯 C 的脚本语言 VM：词法 → 语法 → 元数据(Meta) → IR → VM 派发执行。
项目 PRD（`simple_language/PRD.md`）已明确把「与 C# 互操作 (foreign language interop C#)」列为语言目标之一。

本设计要解决的核心问题：
- 在 SL 源码里声明/引用一个外部 C# 程序集（DLL）。
- SL 运行到某次调用时，真正把控制权交给 **Mono 运行时** 去执行对应的 C# 方法。
- C# 的返回值/异常正确编组回 SL 侧。

### 1.1 已具备的基础（重要，避免重复造轮子）

代码库里**已经存在一层 C# 桥接骨架**，只是尚未真正接通 Mono：

| 已有产物 | 位置 | 现状 |
|---|---|---|
| C# 元数据管理层 `CSharpManager` | `src/csharp/csharp_manager.{h,c}` | 已能登记 assembly/type/method/property/field，但 `method_handle` 始终是 `NULL` |
| C# 调用 IR 节点 | `src/csharp/ir_csharp_call.{h,c}` | `IRCSharpCallInstruction` / `IRCSharpCallStatements` 已定义，但 `ir_csharp_call_instruction_execute()` 是**空桩** |
| C# 成员函数类型 | `src/csharp/meta_member_function_csharp.{h,c}` | `MetaMemberFunctionCSharp` 的 `method_call_type = METHOD_CALL_TYPE_CSHARP` 已就绪 |
| 调用类型枚举 | `METHOD_CALL_TYPE_CSHARP`（派发层已能区分） | VM 派发可据此分支 |
| **Mono 运行时本体** | `third_party/mono/mono-2.0-sgen.dll`（10.5MB，含 sgen GC） | 已 vendored，**未使用** |
| **Mono 头文件** | `third_party/mono/include/mono/{jit,metadata,utils}/*.h` | 完整嵌入 API，已 vendored，**未使用** |

**结论**：本设计不是从零做 FFI，而是「把空桩接通」——
让 `method_handle` 承载 `MonoMethod*`、`execute` 真正走 `mono_runtime_invoke`，
并在加载期用 Mono 把 C# 程序集的元数据灌进已有的 `CSharpManager`。

---

## 2. 现状缺口（Gap Analysis）

| 缺口 | 位置 | 现状 | 本设计补法 |
|---|---|---|---|
| Mono 初始化/清理 | 全局生命周期 | 无 | 新增 `mono_bridge_init/shutdown`，挂到 CLI 启动/退出 |
| 程序集真正加载 | `csharp_manager_init_can_search_assembly_list` | 空桩 | 调 `mono_bridge_load_assembly()` 用 `mono_domain_assembly_open` 真正加载 |
| 方法句柄 | `CSharpMethodInfo.method_handle` (`void*`) | 永远 `NULL` | 语义改为 `MonoMethod*`，由 `mono_class_get_method_from_name` 填充 |
| 真正调用 | `ir_csharp_call_instruction_execute` | 空桩 | 调 `mono_bridge_invoke()` → `mono_runtime_invoke` |
| 类型编组 | `ir_csharp_call_instruction` 的 `param_objs` | 仅 `void*`，无语义 | 在 bridge 内做 SL 值 ↔ Mono 值 的装箱/拆箱 |
| 对象生命周期 | 跨边界 C# 对象引用 | 无 | bridge 内维护 GCHandle 句柄表 |
| SL 侧语法 | 词法/语法 | 无 import C# 机制 | 新增 `using csharp "x.dll"` / `import` 语法 → 注册到 `CSharpManager` |
| 链接 | `CMakeLists.txt` | 未链 Mono | 动态加载 DLL（无需 `.lib`）；post-build 拷贝 DLL |

---

## 3. 总体架构

```
┌──────────────────────────────────────────────────────────────────────┐
│  SL 源码  (using csharp "MyLib.dll";  var o = new MathOps(); o.Add()) │
└──────────────────────────────────────────────────────────────────────┘
        │  parse / compile
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Meta 层                                                              │
│   MetaClassCSharp / MetaMemberFunctionCSharp                          │
│   method_call_type = METHOD_CALL_TYPE_CSHARP                          │
│   └─ 绑定 CSharpMethodInfo* (method_handle : MonoMethod*)             │
└──────────────────────────────────────────────────────────────────────┘
        │  lower to IR
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  IR 层   IRCSharpCallStatements / IRCSharpCallInstruction             │
│   (target, param_objs[], method_info)                                 │
└──────────────────────────────────────────────────────────────────────┘
        │  VM 执行派发 (runtime_dispatch.c)
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  VM dispatch : if (call_type == METHOD_CALL_TYPE_CSHARP)              │
│        └─► ir_csharp_call_instruction_execute()                       │
└──────────────────────────────────────────────────────────────────────┘
        │
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  mono_bridge  (新增：src/csharp/mono_bridge.{h,c})                     │
│   - marshalling:  SL 值 ↔ Mono 值 (装箱/拆箱)                         │
│   - mono_bridge_invoke(method, target, params, exc)                   │
│   - GCHandle 句柄表 (跨边界对象存活)                                  │
│   - 异常 → SL 异常 映射                                               │
└──────────────────────────────────────────────────────────────────────┘
        │  调用 Mono 嵌入 API
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Mono Runtime  (mono-2.0-sgen.dll，由 mono_loader 动态加载)            │
│   mono_jit_init → default domain → 已加载的 C# Assembly               │
│        └─► C# 方法真实执行                                            │
└──────────────────────────────────────────────────────────────────────┘
```

数据流补充：`mono_loader` 在进程启动期把 `mono-2.0-sgen.dll` 载入并解析出
`mono_jit_init` / `mono_runtime_invoke` 等函数指针表；
`mono_bridge` 用该表驱动一切 Mono 调用；若 DLL 缺失则降级为「纯 SL 运行时」。

---

## 4. Mono 嵌入方案

### 4.1 Mono 运行时加载（`mono_loader`）

工程只 vendored 了 `mono-2.0-sgen.dll` 与头文件，**没有 `.lib` 导入库**
（`include/mono` 下唯一的 `.def` 是 `cil/opcode.def`，并非导出库）。
因此采用**动态加载**最稳：

- Windows：`LoadLibraryW(L"mono-2.0-sgen.dll")` + `GetProcAddress` 逐项解析所需符号。
- Linux/macOS：`dlopen("libmonosgen-2.0.so" / "libmono-2.0.dylib")` + `dlsym`。
- 定义一张函数指针表 `MonoAPI`，全工程统一通过 `g_mono->mono_runtime_invoke(...)` 调用，
  避免到处 `#ifdef`。
- 缺失时 `g_mono == NULL`，SL 仍可正常运行纯 SL 代码（C# 调用在解析期即报「Mono 不可用」）。

> 备选：用 `dumpbin /exports` + `lib /def` 从 DLL 生成 `mono-2.0-sgen.lib` 做静态链接。
> 动态加载实现更简单、跨编译器（MSVC/MinGW）一致，本文档以动态加载为默认方案。

### 4.2 生命周期

挂载点在 CLI 入口 `cli_main`（`src/cli/cli.c`，`main.c` 仅转发）：

```
cli_main(argc, argv)
  ├─ mono_bridge_init()            // LoadLibrary + 解析 API；mono_jit_init("sl_csharp_domain")
  ├─ ... 现有编译/加载/运行流程 ...
  └─ mono_bridge_shutdown()        // mono_jit_cleanup(domain)；FreeLibrary
```

- 一个进程一个 default `MonoDomain` 即可（SL VM 为单线程驱动）。
- 若后续引入多线程/协程，每次进入 C# 调用前需 `mono_thread_attach(domain)`。

### 4.3 程序集加载与元数据注册

`csharp_manager_init_can_search_assembly_list()`（当前空桩）改为：

```
mono_bridge_load_assembly(path):
    assembly = mono_domain_assembly_open(domain, path)
    image    = mono_assembly_get_image(assembly)
    // 方式 A（推荐，惰性）：不预先扫描，等 SL 侧 import 时按 name_space+class 解析
    // 方式 B（热注册）：遍历 image 的 TABLE_TYPEDEF 行，逐类调
    //        mono_class_from_name 后把 method/property/field 灌入 CSharpManager
    csharp_manager_add_assembly(...)   // 复用已有结构
```

- **推荐惰性解析**：SL 里 `import MyLib.MathOps;` 时，用
  `mono_class_from_name(image, "MyLib", "MathOps")` 拿到 `MonoClass*`，
  再把 `mono_class_get_method_from_name` 得到的 `MonoMethod*` 填入
  `CSharpMethodInfo.method_handle`（即把该 `void*` 的语义定为 `MonoMethod*`）。
- 这样避免一次性反射整个程序集，启动更快。

### 4.4 方法句柄语义变更

仅**约定**变更，不改结构体布局：

```c
// src/csharp/csharp_manager.h
// 旧：void* method_handle;   // 永远 NULL
// 新语义：
typedef struct MonoMethod MonoMethod;            // 来自 mono/metadata/class.h
struct CSharpMethodInfo {
    ...
    MonoMethod* method_handle;   // 由 mono_bridge 填充，NULL 表示未解析/不可用
};
```

同理 `field_handle` 可语义化为 `MonoClassField*`。

---

## 5. SL 侧调用语法设计（建议）

SL 语法受 C#/Dart 启发，建议新增最小关键字集（具体词法/语法实现另议）：

```sl
// 1) 装载并注册一个 C# 程序集（触发 mono_bridge_load_assembly）
using csharp "MyLib.dll";

// 2) 引入命名空间/类，生成 MetaClassCSharp
import MyLib.MathOps;

function main() {
    var ops = new MathOps();        // C# 类实例 → MonoObject*（GCHandle 持有）
    int r = ops.Add(1, 2);          // 调用 → METHOD_CALL_TYPE_CSHARP → mono_bridge_invoke
    print(r);                       // 3

    string s = ops.Greet("world");  // string 编组：mono_string_new / mono_string_to_utf8
    print(s);
}
```

映射关系（复用既有类型，无需新造）：

- `using csharp "x.dll"` → `mono_bridge_load_assembly` + `csharp_manager_add_assembly`
- `import Ns.Klass` → 在 `CSharpManager` 注册 `MetaClassCSharp`
  （对应 `csharp_manager_find_and_create_meta_node` 现有空桩分支）
- `ops.Add(...)` → 解析为 `MetaMemberFunctionCSharp`（`METHOD_CALL_TYPE_CSHARP`）
  → `ir_csharp_call_statements_create` → `IRCSharpCallInstruction`
- 静态方法：`import static MyLib.Utils;` → `is_static=1`，invoke 时 `target=NULL`

> 备选语法：用 `[csharp]` 标注或 `extern csharp` 声明。本文档以 `using csharp`/`import`
> 为推荐形态，重点是**语义落到既有 `MetaClassCSharp`/`METHOD_CALL_TYPE_CSHARP` 通路**。

---

## 6. 调用链路（SL → Mono 真实执行）

以 `ops.Add(1,2)` 为例，端到端步骤：

1. SL VM 派发命中 `METHOD_CALL_TYPE_CSHARP` 分支（`runtime_dispatch.c`）。
2. 取出 `MetaMemberFunctionCSharp`，得到其 `CSharpMethodInfo*`，即 `MonoMethod* method`。
3. 进入 `ir_csharp_call_instruction_execute(instruction)`（填实桩）：
   - `instruction->target`：SL 侧持有的 C# 实例（`MonoObject*`，经 GCHandle 取得）。
   - `instruction->param_objs[]`：SL 运行时值，需先**装箱**为 Mono 可识别的 `void*` 参数数组。
4. `mono_bridge_invoke(method, target, mono_params, &exc)`：
   - `mono_runtime_invoke(method, target, mono_params, &exc)`。
   - 若 `exc != NULL`：把 C# 异常转为 SL 运行时异常并抛出（VM 已有 try/catch 体系）。
   - 否则对返回值**拆箱**为 SL 运行时值，写回 `instruction->return_obj`。
5. VM 把返回值压栈，继续后续 SL 指令。

---

## 7. 类型映射与编组（Marshalling）

| SL 类型 | C 侧表示 | Mono/C# 类型 | 编组方式 |
|---|---|---|---|
| `int` | `int32_t` | `System.Int32` (gint32) | 值直接传入 `void**` 参数槽（值类型需先 `mono_value_box` 或按签名传地址） |
| `float`/`double` | `double` | `System.Double` | 同上 |
| `bool` | int | `System.Boolean` (gboolean) | 同上 |
| `string` | `char*`/`utf8` | `System.String` | `mono_string_new(domain, cstr)`；回程 `mono_string_to_utf8` |
| `array<T>` | SL 数组对象 | `System.Array` | `mono_array_new(domain, elem_class, n)` + 逐元素填充 |
| `object`/`class` | SL 对象 | C# 类实例 | `MonoObject*`，经 GCHandle 持有 |

要点：
- **值类型参数**：`mono_runtime_invoke` 的 `params` 是 `void**`，每个元素是「指向该值的指针」；
  对 `int/double/bool` 等需把栈上值地址放入 `void**` 数组（或在需装箱时 `mono_value_box`）。
- **返回值**：基本值类型需 `mono_object_unbox(result)` 取指针再拷贝；引用类型直接是 `MonoObject*`。
- 字符串跨边界复制（UTF-8 ↔ UTF-16），注意释放 `mono_string_to_utf8` 返回的 `g_free`。

---

## 8. 对象生命周期与 GC 协作

Mono 用 **sgen GC**，SL VM 用**手动内存 + 引用计数**（见 `md/GC_DESIGN.md`）。
跨边界的 C# 对象若只被 C 侧 `MonoObject*` 引用，sgen 看不到 → 会被错误回收。

方案：在 `mono_bridge` 内维护**句柄表**：

- SL 创建 C# 实例（`new MathOps()`）时：`mono_object_new` → `mono_gchandle_new(obj, FALSE)`
  得到 `guint32 handle`，把 handle 存进 SL 对象（作为不透明 id），而非裸指针。
- 每次调用前：`MonoObject* target = mono_gchandle_get_target(handle)`。
- SL 对象被 GC 回收时：回调 `mono_gchandle_free(handle)`。
- 这样 sgen 能正确追踪，避免悬垂指针。

> 注意：不要把 `MonoObject*` 原始指针长期保存在 C 堆而不挂 GCHandle。

---

## 9. 异常处理

`mono_runtime_invoke` 的最后一个参数 `MonoObject** exc`：

- 若 `exc != NULL`，拿到的是 C# 抛出的异常对象（`System.Exception`）。
- bridge 提取 `Message`/`StackTrace`，构造为 **SL 运行时异常**，沿 VM 调用栈向上抛出。
- SL 侧可用现有 `try/catch`（参考 `test_catchfix*`、`test_try*` 相关实现）捕获。
- C# 内部未捕获异常的栈信息，建议一并记录到 `log_manager` 便于排查。

---

## 10. 构建与集成（CMake）

改动 `csimple_lang/CMakeLists.txt`：

1. **包含目录**（已 vendored，确认加入）：
   ```cmake
   target_include_directories(csimple_lang PRIVATE "${CMAKE_SOURCE_DIR}/third_party/mono/include")
   ```
2. **新增源文件**到 `CSIMPLE_LIB_SOURCES`：
   ```
   src/csharp/mono_loader.c
   src/csharp/mono_bridge.c
   ```
3. **post-build 拷贝 DLL**（沿用 sqlite3 的做法）：
   ```cmake
   add_custom_command(TARGET csimple_lang POST_BUILD
     COMMAND ${CMAKE_COMMAND} -E copy_if_different
             "${CMAKE_SOURCE_DIR}/third_party/mono/mono-2.0-sgen.dll"
             "$<TARGET_FILE_DIR:csimple_lang>/mono-2.0-sgen.dll")
   # 对 csimple_lang_dll 目标做同样拷贝
   ```
4. **无需链接 `.lib`**：因采用动态加载（`LoadLibrary`），不在 `target_link_libraries` 中加 Mono。
5. Unix 平台需系统安装 Mono 或把对应 `.so` 放到 `third_party/mono/`，并在 `mono_loader` 中按平台选文件名。

---

## 11. 改造文件清单

**新增**
- `src/csharp/mono_loader.h` / `mono_loader.c` —— 动态加载 Mono API，提供 `MonoAPI` 函数指针表。
- `src/csharp/mono_bridge.h` / `mono_bridge.c` —— 装配集加载、method 解析、invoke、marshalling、GCHandle 句柄表、异常映射。

**修改**
- `src/csharp/csharp_manager.c` —— `init_can_search_assembly_list` 调 `mono_bridge_load_assembly`；`method_handle` 语义改为 `MonoMethod*`。
- `src/csharp/csharp_manager.h` —— `method_handle` 类型标注为 `MonoMethod*`（含前向声明）。
- `src/csharp/ir_csharp_call.c` —— `ir_csharp_call_instruction_execute` 调 `mono_bridge_invoke`，落地真实调用 + 参数/返回值编组。
- `src/vm/runtime/dispatch/runtime_dispatch.c` —— 确认 `METHOD_CALL_TYPE_CSHARP` 分支走到 IR 执行（或显式调 bridge）。
- `src/cli/cli.c` —— 启动/退出挂 `mono_bridge_init` / `mono_bridge_shutdown`。
- `CMakeLists.txt` —— include 目录、新增源、post-build 拷贝 DLL。
- （可选）`src/compile/*` 词法/语法 —— 新增 `using csharp` / `import` 解析，注册 `MetaClassCSharp`。

---

## 12. 分阶段落地计划

- **阶段 1 · 跑通 Mono 加载**：实现 `mono_loader` + `mono_bridge_init/shutdown`；在 C 侧硬编码加载一个测试 DLL
  并 `mono_runtime_invoke` 一个静态方法，验证 JIT 与 API 调用可用。
- **阶段 2 · 接通元数据与调用桩**：`CSharpManager` 注册 + `ir_csharp_call_instruction_execute` 真实调用；
  支持 `int`/`string` 基础编组（静态方法优先）。
- **阶段 3 · SL 端到端**：`using csharp` / `import` 语法解析 → `MetaClassCSharp` 注册 → 从 SL 调用 C# 方法。
- **阶段 4 · 完整能力**：实例对象（GCHandle 句柄表）、属性读写、数组、异常映射、GC 协作、多线程 `mono_thread_attach`。

---

## 13. 风险与注意事项

1. **GC 跨边界**：sgen GC 与 C 手动内存共存，跨边界 C# 对象必须走 GCHandle（见 §8），否则悬垂指针。
2. **eglib 符号冲突**：Mono 自带 eglib，项目也用 eglib（`README` 提及）。需确认两者版本/符号不冲突，
   必要时让 Mono 用其自带 eglib、项目侧隔离，或统一版本。
3. **线程模型**：Mono 默认要求每个调用线程 `mono_thread_attach(domain)`；若 SL VM 后续引入多线程/协程需补此步。
4. **指针宽度**：`MonoObject*`、`void**` 参数数组在 32/64 位下尺寸一致处理即可，注意 `uintptr_t`。
5. **平台可用性**：Windows 已 vendored `mono-2.0-sgen.dll`；Linux/macOS 需另行提供 `libmonosgen-2.0.so`/`.dylib`，
   缺失时按 §4.1 降级为纯 SL 运行时。
6. **ABI 稳定**：Mono 嵌入 API（`mono_jit_init`/`mono_runtime_invoke` 等）在 classic Mono 长期稳定；
   若未来切到 .NET (CoreCLR hosting / NativeAOT)，需替换为对应 hosting API（本文档不展开）。

---

## 14. 小结

本设计**复用已有 C# 桥接骨架**，把「空桩」接通为「真实 Mono 调用」：
- `method_handle` → `MonoMethod*`
- `ir_csharp_call_instruction_execute` → `mono_bridge_invoke` → `mono_runtime_invoke`
- 通过 `mono_loader` 动态加载 vendored 的 `mono-2.0-sgen.dll`，缺失可降级
- 新增 `using csharp`/`import` 语法让 SL 直接调用 C#

下一步按 §12 四阶段推进，先以阶段 1 验证 Mono 在 Windows 下可被本进程加载并执行 C# 静态方法。
