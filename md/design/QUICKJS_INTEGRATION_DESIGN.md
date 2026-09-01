# QuickJS 机制整合设计文档

> 目标：在 `csimple_lang`（Simple Language 的 C99 实现 + 自带 VM 运行时）中，
> 让 SL 代码能够**直接调用运行在 QuickJS 下的 JavaScript 代码**（函数、对象方法）。
> 本设计沿用在 `csimple_lang/md/MONO_INTEGRATION_DESIGN.md` 中确立的整体思路，
> 给出 QuickJS 版本的**设计、逻辑与落地路径**。不涉及最终实现代码。
>
> 约定：下文称 Mono 方案为「参照方案」，其文件路径见 Mono 设计文档。

---

## 1. 背景与目标

SL 的 PRD（`simple_language/PRD.md`）把「foreign language interop」列为语言目标。
Mono 方案解决「调用 C#」；本方案解决「调用 JavaScript（QuickJS）」。

核心诉求一致：SL 运行时到某次调用时，把控制权交给 **QuickJS 引擎** 去执行对应的 JS 函数，
并把返回值/异常正确编组回 SL 侧。

QuickJS 特点（决定与本方案实现的差异）：
- 由 Fabrice Bellard 开发的小型可嵌入 JS 引擎，纯 C、MIT 协议。
- 单文件核心（`quickjs.c` + `quickjs.h`），可直接编译进工程，**无需 DLL / 导入库**
  （与 Mono 的 `mono-2.0-sgen.dll` + 动态加载形成对比）。
- 自带引用计数与 NaN-boxing 的 `JSValue`，无独立 GC（区别于 Mono 的 sgen GC）。
- 一个 `JSRuntime` 仅限单线程使用（区别于 Mono 的 `mono_thread_attach`）。

---

## 2. 现状与缺口（Gap Analysis）

与 Mono 不同，**工程里目前没有任何 JS 相关代码或依赖**（已全文搜索确认），
也未 vendored QuickJS。因此本方案相对「从零搭建一个平行桥接子系统」，而非「接通既有空桩」。

| 缺口 | 位置 | 现状 | 本设计补法 |
|---|---|---|---|
| JS 引擎本体 | `third_party/quickjs/` | 不存在 | 引入 QuickJS 源码（编译进工程，非 DLL） |
| JS 元数据/注册层 | `src/js/js_manager.*` | 不存在（需新建，对标 `src/csharp/csharp_manager.*`） | 管理已加载脚本/模块、名字→`JSValue` 映射 |
| JS 调用 IR 节点 | `src/js/ir_js_call.*` | 不存在（对标 `ir_csharp_call.*`） | `IRJsCallInstruction` 持有 `JSValue func` |
| JS 成员函数类型 | `src/js/meta_member_function_js.*` | 不存在（对标 `meta_member_function_csharp.*`） | `method_call_type = METHOD_CALL_TYPE_JS` |
| 真正调用 | `ir_js_call` 的 execute | 不存在 | 调 `js_bridge_invoke()` → `JS_Call` |
| 类型编组 | — | 无 | bridge 内做 SL 值 ↔ `JSValue` 互转 |
| 对象生命周期 | 跨边界 JS 对象 | 无 | `JS_DupValue` / `JS_FreeValue` 引用计数 |
| SL 侧语法 | 词法/语法 | 无 | 新增 `using js "x.js"` / `import` |
| 构建 | `CMakeLists.txt` | 未含 QuickJS | 把 quickjs 源文件加入 `CSIMPLE_LIB_SOURCES` |
| 派发分支 | `runtime_dispatch.c` | 仅有 `METHOD_CALL_TYPE_CSHARP` | 增加 `METHOD_CALL_TYPE_JS` 分支 |

> 可选演进：将 `METHOD_CALL_TYPE_CSHARP` 泛化为 `METHOD_CALL_TYPE_EXTERNAL`，
> 在 `MetaMemberFunction` 上携带「语言标签（CSHARP / JS）」，
> 由统一 `external_call` 派发层按标签分派到 Mono / QuickJS bridge。
> 短期为降低风险，建议先新增独立 `METHOD_CALL_TYPE_JS` 分支（与现有 C# 分支并列）。

---

## 3. 总体架构

```
┌──────────────────────────────────────────────────────────────────────┐
│  SL 源码  (using js "lib.js";  var o = new MathOps(); o.add(1,2))      │
└──────────────────────────────────────────────────────────────────────┘
        │  parse / compile
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Meta 层                                                              │
│   MetaClassJs / MetaMemberFunctionJs                                  │
│   method_call_type = METHOD_CALL_TYPE_JS                              │
│   └─ 绑定 JsFunctionInfo* (func_handle : JSValue)                     │
└──────────────────────────────────────────────────────────────────────┘
        │  lower to IR
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  IR 层   IRJsCallStatements / IRJsCallInstruction                     │
│   (target JSValue, argv[], func_handle)                               │
└──────────────────────────────────────────────────────────────────────┘
        │  VM 执行派发 (runtime_dispatch.c)
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  VM dispatch : if (call_type == METHOD_CALL_TYPE_JS)                  │
│        └─► ir_js_call_instruction_execute()                           │
└──────────────────────────────────────────────────────────────────────┘
        │
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  js_bridge  (新增：src/js/js_bridge.{h,c})                             │
│   - marshalling:  SL 值 ↔ JSValue (互转)                              │
│   - js_bridge_invoke(func, thisVal, argv, exc) → JS_Call              │
│   - JSValue 引用计数 (JS_DupValue / JS_FreeValue)                     │
│   - 异常 → SL 异常 映射                                               │
└──────────────────────────────────────────────────────────────────────┘
        │  调用 QuickJS C API
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  QuickJS 引擎 (编译进工程的 quickjs.c；由 js_loader 初始化)            │
│   JSRuntime (进程级, 单线程) → JSContext (每个加载的 js 文件一个)      │
│        └─► JS 函数真实执行                                            │
└──────────────────────────────────────────────────────────────────────┘
```

数据流补充：`js_loader` 在进程启动期 `JS_NewRuntime()` + `JS_NewContext()` 建立引擎；
`js_bridge` 用其驱动 `JS_Eval` / `JS_Call`。
由于 QuickJS 直接编进工程，**不需要** Mono 那种 `LoadLibrary` 动态加载与 API 函数指针表。

---

## 4. QuickJS 嵌入方案

### 4.1 引入 QuickJS（vendoring，编译进工程）

- 取官方 QuickJS 源码放入 `third_party/quickjs/`，最小化集合：
  `quickjs.c`、`quickjs.h`、`quickjs-atom.h`、`quickjs-opcode.h`、
  `libregexp.c`、`libunicode.c`、`cutils.c`、`cutils.h`、`list.h`、`quickjs-libc.h`（可选）。
- 作为**普通 C 源文件**加入 `CSIMPLE_LIB_SOURCES`（见 §10），与 SL 运行时一并编译/链接。
- 无需 `.lib`、无需拷贝 DLL —— 这是相对 Mono 的主要简化点。
- 注意：QuickJS 默认用系统 `malloc`/`free`，与 SL 手动内存管理**不冲突**
  （不像 Mono 自带 eglib 可能与项目 eglib 产生符号冲突）。

### 4.2 生命周期

挂载点与 Mono 方案一致：CLI 入口 `cli_main`（`src/cli/cli.c`）。

```
cli_main(argc, argv)
  ├─ js_bridge_init()       // JS_NewRuntime() + JS_NewContext()；可选 JS_SetMemoryLimit
  ├─ ... 现有编译/加载/运行流程 ...
  └─ js_bridge_shutdown()   // JS_FreeContext() + JS_FreeRuntime()
```

- 推荐：**一个进程一个 `JSRuntime`**（便于统一内存上限），**每个加载的 `.js` 文件一个 `JSContext`**
  （`JS_NewContext(rt)` 可在同一 runtime 上创建多个上下文，互不污染全局变量）。
- QuickJS 单线程：SL VM 自身单线程驱动即可；若未来引入多线程/协程，
  必须保证同一 `JSRuntime` 的所有操作都在同一线程（无 `mono_thread_attach` 等价物）。

### 4.3 脚本/模块加载与注册

`js_manager_init_loaded_list()`（新建，对标 `csharp_manager` 空桩逻辑）改为：

```
js_bridge_load_script(path):
    ctx = js_manager_get_or_create_context(path)   // 一个 js 文件 ↔ 一个 JSContext
    code = read_file(path)
    JS_Eval(ctx, code, len, path, JS_EVAL_TYPE_GLOBAL)
    // 全局函数/对象现在挂在 ctx 的 global 上
    // 记录 path → ctx 的映射，供后续 import 解析
```

- JS 没有 C# 那样的静态类型反射，因此「元数据」模型改为：
  **名字 → `JSValue` 函数/对象** 的注册表（`JsManager` 持有）。
- SL 的 `import MathOps;` 对应：在目标 `JSContext` 的 global 上
  `JS_GetPropertyStr(ctx, global, "MathOps")` 取到 `JSValue`，存入 `JsFunctionInfo.func_handle`。
- 若使用 ES Module（`import`/`export` 语法），需通过 `JS_SetModuleLoaderFunc` 注册模块加载器；
  初版建议用全局脚本（global eval）即可，降低复杂度。

### 4.4 函数句柄语义

```c
// src/js/js_manager.h
typedef struct JSValue JSValue;          // 来自 quickjs.h
struct JsFunctionInfo {
    char name[256];
    JSValue func_handle;                 // JS function / object（引用计数需 JS_DupValue 持有）
    int param_count;                     // 由 JS 侧约定（JS 本身无强签名，可做可选校验）
    char is_static;                      // JS 全局函数=1；对象方法=0（this 为对象）
};
```

- `func_handle` 为 `JSValue`（NaN-boxing 的 32/64 位值）。
- **持有它必须用 `JS_DupValue(ctx, v)` 增引用**，释放用 `JS_FreeValue(ctx, v)`，
  否则 QuickJS 内部引用计数归零会回收该值（对应 Mono 方案的 GCHandle 角色）。

---

## 5. SL 侧调用语法设计（建议，与 Mono 对称）

```sl
// 1) 加载并执行一个 JS 文件（建立 JSContext，注册其全局符号）
using js "lib.js";

// 2) 引入一个 JS 对象/函数，生成 MetaClassJs
import MathOps;

function main() {
    var o = new MathOps();          // JS 构造器 → 新建 JS 对象（JSValue）
    int r = o.add(1, 2);            // 调用 → METHOD_CALL_TYPE_JS → js_bridge_invoke
    print(r);                       // 3

    string s = o.greet("world");    // string 编组：JS_NewString / JS_ToCString
    print(s);
}
```

映射关系（仿照 Mono 方案落到既有通路）：

- `using js "x.js"` → `js_bridge_load_script` + 在 `JsManager` 注册上下文。
- `import MathOps` → 在目标 `JSContext` 取 global 上的 `MathOps` → 注册 `MetaClassJs`。
- `o.add(...)` → 解析为 `MetaMemberFunctionJs`（`METHOD_CALL_TYPE_JS`）
  → `ir_js_call_statements_create` → `IRJsCallInstruction`。
- 全局函数形式：`import static add;` → `is_static=1`，调用时 `thisVal = JS_UNDEFINED`。
- JS 对象方法：`thisVal` 为调用者持有的 JS 对象 `JSValue`。

> 与 Mono 的差异点：JS 无静态类型系统，参数个数/类型由 JS 侧约定，
> SL 侧可做可选的类型/个数校验并在不匹配时报「JS 调用签名错误」。

---

## 6. 调用链路（SL → QuickJS 真实执行）

以 `o.add(1,2)` 为例：

1. SL VM 派发命中 `METHOD_CALL_TYPE_JS` 分支（`runtime_dispatch.c`）。
2. 取出 `MetaMemberFunctionJs`，得到 `JsFunctionInfo*`，即 `JSValue func`。
3. 进入 `ir_js_call_instruction_execute(instruction)`：
   - `instruction->target`：调用者持有的 JS 对象 `JSValue`（`thisVal`）。
   - `instruction->argv[]`：SL 运行时值，先**转为 `JSValue`**（见 §7）放入 `JSValue argv[]`。
4. `js_bridge_invoke(func, thisVal, argv, &exc)`：
   - `result = JS_Call(ctx, func, thisVal, argc, argv)`。
   - 若 `JS_IsException(result)`：取 `JS_GetException(ctx)` → 构造 SL 运行时异常抛出。
   - 否则对 `result` **转为 SL 值**，写回 `instruction->return_obj`。
   - 释放 `argv` 各 `JSValue`、`result`（按引用计数 `JS_FreeValue`）。
5. VM 把返回值压栈，继续后续 SL 指令。

---

## 7. 类型映射与编组（Marshalling）

| SL 类型 | C 侧表示 | QuickJS 类型 | 编组方式 |
|---|---|---|---|
| `int` | `int32_t` | JS number | `JS_NewInt32(ctx, v)` / `JS_ToInt32(ctx, &out, val)` |
| `float`/`double` | `double` | JS number | `JS_NewFloat64(ctx, v)` / `JS_ToFloat64(ctx, &out, val)` |
| `bool` | int | JS boolean | `JS_NewBool(ctx, v)` / `JS_ToBool(ctx, val)` |
| `string` | `char*`/`utf8` | JS string | `JS_NewString(ctx, cstr)` / `JS_ToCString`(+`JS_FreeCString`) |
| `array<T>` | SL 数组对象 | JS Array | `JS_NewArray` + `JS_SetPropertyInt` 逐元素；回程 `JS_GetPropertyStr("length")` + 下标取 |
| `object`/`class` | SL 对象 | JS Object | `JS_NewObject` + `JS_SetPropertyStr`；持有为 `JSValue`（需 `JS_DupValue`） |

要点：
- `JSValue` 是**带引用计数的值**，任何跨调用边界保存的 `JSValue` 都要 `JS_DupValue` 增引用，
  用完 `JS_FreeValue` 释放（与 Mono 的 GCHandle 目的一致，但机制是值本身引用计数）。
- 字符串跨边界：`JS_ToCString` 返回的是引擎内部分配、需用 `JS_FreeCString` 释放的缓冲区。
- `JSValue` 不能跨 `JSContext` 混用（即使是同一 `JSRuntime` 下的不同 context，也需显式传递/转换）。

---

## 8. 对象生命周期与引用计数

QuickJS **无独立 GC**，靠 `JSValue` 的引用计数；这正是它与 Mono(sgen) 的关键区别。

- SL 创建/持有一个 JS 对象（`new MathOps()`）：
  `JS_Call` 构造器得到 `JSValue obj` → `JS_DupValue(ctx, obj)` 提升为「被 SL 持有」，
  把该 `JSValue` 存进 SL 对象（作为不透明句柄）。
- 每次方法调用前：直接以该 `JSValue` 作为 `thisVal`。
- SL 对象被 GC/释放时：回调 `JS_FreeValue(ctx, obj)` 减引用。
- 注意 `JSValue` 是 NaN-boxing 的紧凑值，**不要**把它当裸指针长期存而不增引用，也不要跨 runtime 线程传递。

---

## 9. 异常处理

QuickJS 用「返回值即异常」约定：

- `JS_Call` 返回 `JSValue`；若 `JS_IsException(v)` 为真，说明 JS 抛了异常。
- 用 `JS_GetException(ctx)` 取异常对象，读取其 `.message` / `.stack`
  （通过 `JS_GetPropertyStr(ctx, exc, "message")` + `JS_ToCString`）。
- 构造为 **SL 运行时异常**并向上抛出；SL 侧可用现有 `try/catch`（`test_catchfix*`、`test_try*` 体系）捕获。
- 取完异常信息后 `JS_FreeValue(ctx, exc)`，并 `JS_ResetUncatchableError`/清理 pending 状态。

---

## 10. 构建与集成（CMake）

改动 `csimple_lang/CMakeLists.txt`：

1. **新增 QuickJS 源文件**到 `CSIMPLE_LIB_SOURCES`：
   ```
   third_party/quickjs/quickjs.c
   third_party/quickjs/libregexp.c
   third_party/quickjs/libunicode.c
   third_party/quickjs/cutils.c
   src/js/js_manager.c
   src/js/ir_js_call.c
   src/js/meta_member_function_js.c
   src/js/js_loader.c
   src/js/js_bridge.c
   ```
2. **无需** `target_include_directories` 之外的特殊处理；QuickJS 头文件随 `src` 一并包含
   （`target_include_directories(... "${CMAKE_SOURCE_DIR}/third_party/quickjs")`）。
3. **无需** `target_link_libraries` 添加 QuickJS（已编译进静态库/可执行体）。
4. **无需** post-build 拷贝 DLL（与 Mono 的 `mono-2.0-sgen.dll` 拷贝步骤相反）。
5. 编译选项：QuickJS 部分源可能需要 `-fno-strict-aliasing` / 关闭某些警告，
   可对 `third_party/quickjs/*.c` 单独设置 `COMPILE_OPTIONS`（MSVC `/wd`、GCC `-Wno-*`）。

---

## 11. 改造文件清单

**新增**（`src/js/` + `third_party/quickjs/`）
- `third_party/quickjs/{quickjs.c,quickjs.h,libregexp.c,libunicode.c,cutils.c,...}` —— 引擎本体。
- `src/js/js_loader.h/.c` —— 初始化/销毁 `JSRuntime`/`JSContext`。
- `src/js/js_manager.h/.c` —— 上下文与名字→`JSValue` 注册表。
- `src/js/js_bridge.h/.c` —— 脚本加载、`js_bridge_invoke`、marshalling、异常映射、引用计数管理。
- `src/js/ir_js_call.h/.c` —— `IRJsCallInstruction`/`IRJsCallStatements`（对标 `ir_csharp_call`）。
- `src/js/meta_member_function_js.h/.c` —— `MetaMemberFunctionJs`（`METHOD_CALL_TYPE_JS`）。

**修改**
- `src/vm/runtime/dispatch/runtime_dispatch.c` —— 新增 `METHOD_CALL_TYPE_JS` 分支（或统一 external 派发）。
- `src/cli/cli.c` —— 启动/退出挂 `js_bridge_init` / `js_bridge_shutdown`。
- `CMakeLists.txt` —— 加入 quickjs 源、include 目录、编译选项。
- （可选）`src/compile/*` 词法/语法 —— 新增 `using js` / `import` 解析，注册 `MetaClassJs`。
- （可选）调用类型枚举 —— 新增 `METHOD_CALL_TYPE_JS`（或在 `meta_member_function.h` 增加语言标签字段）。

---

## 12. 分阶段落地计划

- **阶段 1 · 跑通引擎**：引入 QuickJS 源并编译；`js_loader` 建立 `JSRuntime`+`JSContext`；
  在 C 侧硬编码 `JS_Eval` 一段脚本并 `JS_Call` 一个全局函数，验证引擎可用。
- **阶段 2 · 接通调用桩**：`JsManager` 注册 + `ir_js_call_instruction_execute` 真实调用；
  支持 `int`/`string` 基础编组（全局函数优先）。
- **阶段 3 · SL 端到端**：`using js` / `import` 语法解析 → `MetaClassJs` 注册 → 从 SL 调用 JS 函数。
- **阶段 4 · 完整能力**：JS 对象实例（引用计数）、属性读写、数组、异常映射、ES Module 加载器。

---

## 13. 风险与注意事项

1. **单线程约束**：`JSRuntime` 非线程安全，所有操作须在同一线程；SL VM 单线程驱动 OK，
   多线程/协程需谨慎（无 `mono_thread_attach` 等价物）。
2. **JSValue 引用计数**：跨边界保存必须 `JS_DupValue`，释放 `JS_FreeValue`，否则悬垂/泄漏。
3. **JSValue 不跨 Context**：同一 runtime 下不同 context 的 `JSValue` 不可混用。
4. **类型弱化**：JS 无静态签名，SL 侧需自行约定并校验参数个数/类型，避免运行时类型错乱。
5. **无强异常类型**：JS 异常是运行时值，需主动 `JS_IsException` 检测并映射，易遗漏。
6. **编译兼容**：QuickJS 对严格别名/编译器警告较敏感，建议对 quickjs 源单独设编译选项。
7. **内存上限**：`JS_SetMemoryLimit(rt, ...)` 建议设置，防止失控 JS 脚本耗尽进程内存。

---

## 14. 小结

本方案**平行复用** Mono 文档确立的整体思路（SL → Meta → IR → VM 派发 → bridge → 外部引擎），
但针对 QuickJS 特性做了适配：

- 引擎**编译进工程**（非 DLL 动态加载），无 eglib 冲突、无 `.lib` 需求。
- 元数据模型从「C# 反射」变为「名字 → `JSValue` 注册表」。
- 生命周期靠 `JSValue` **引用计数**（`JS_DupValue`/`JS_FreeValue`），而非 Mono 的 GCHandle。
- 异常用「返回值即异常」约定（`JS_IsException`/`JS_GetException`）。
- 新增独立 `METHOD_CALL_TYPE_JS` 派发分支，与现有 `METHOD_CALL_TYPE_CSHARP` 并列。

---

## 15. 与 Mono 方案的共性与差异（对照）

| 维度 | Mono 方案（参照） | QuickJS 方案（本设计） |
|---|---|---|
| 引擎提供方 | `mono-2.0-sgen.dll`（已 vendored） | QuickJS 源码（需引入，编译进工程） |
| 加载方式 | `LoadLibrary` 动态加载 + API 函数指针表 | 直接编译链接，无需动态加载 |
| 调用 API | `mono_runtime_invoke(MonoMethod*, ...)` | `JS_Call(JSContext*, JSValue, ...)` |
| 元数据来源 | 反射 C# 类型/方法（静态） | JS 全局符号按名查找（动态、无强签名） |
| 跨边界对象存活 | `mono_gchandle_new`（sgen GC root） | `JS_DupValue`/`JS_FreeValue`（引用计数） |
| 线程模型 | 需 `mono_thread_attach` | `JSRuntime` 单线程，无 attach |
| 依赖冲突风险 | eglib 可能与项目 eglib 冲突 | 无（用系统 malloc） |
| SL 语法 | `using csharp "x.dll"` / `import` | `using js "x.js"` / `import` |
| 派发分支 | `METHOD_CALL_TYPE_CSHARP` | `METHOD_CALL_TYPE_JS`（建议后续统一为 `METHOD_CALL_TYPE_EXTERNAL` + 语言标签） |
| 缺失降级 | DLL 缺失→纯 SL 运行 | 源码必编入，不存在「缺失」情形 |

**统一演进建议**：当两套 bridge 都跑通后，可在 VM 派发层抽象出统一的
「外部语言调用（External Language Call）」接口，由 `MetaMemberFunction` 携带语言标签
派发到 `mono_bridge` 或 `js_bridge`，使 SL 侧 `using <lang>` 成为可插拔的多语言互操作框架。
