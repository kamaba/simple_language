# HotSpot VM (JNI) 整合设计文档

> 目标：在 `csimple_lang`（Simple Language 的 C99 实现 + 自带 VM 运行时）中，
> 让 SL 代码能够**直接调用运行在 HotSpot VM（JVM）下的 Java 代码**（类、静态/实例方法）。
> 本设计沿用 `MONO_INTEGRATION_DESIGN.md` 与 `QUICKJS_INTEGRATION_DESIGN.md` 确立的整体思路，
> 给出 HotSpot/JNI 版本的**设计、逻辑与落地路径**。不涉及最终实现代码。
>
> 配套文档：
> - `csimple_lang/md/MONO_INTEGRATION_DESIGN.md`（C# / Mono）
> - `simple_language/md/design/QUICKJS_INTEGRATION_DESIGN.md`（JS / QuickJS）

---

## 0. 选型说明：HotSpot VM 还是 GraalVM？

本项目要在 C 进程内嵌入一个「能跑托管代码的虚拟机」来被 SL 调用。候选二选一：

| 维度 | HotSpot VM（经 JNI） | GraalVM |
|---|---|---|
| 嵌入方式 | `JNI_CreateJavaVM`（Invocation API，纯 C） | `graal_create_isolate` + `polyglot_*`（C 嵌入 API，isolate 模型） |
| 语言/API 贴合 C99 | ✅ `jni.h` 纯 C，最贴合 | ⚠️ isolate 模型，较重 |
| 静态反射模型 | ✅ `FindClass→GetMethodID→Call*Method`，与现有 `CSharpManager` 同构 | ⚠️ polyglot Value 模型，与 C# 桥接差异大 |
| 与 Mono 嵌入方式一致 | ✅ 同样动态加载 `libjvm`（类比 `mono_loader`） | ⚠️ 需整套 GraalVM 发行版 |
| 依赖体积 | 任意 JRE/JDK（轻） | 专门 GraalVM 发行版（重，且与已规划的 Mono+QuickJS 重叠） |

**结论：选 HotSpot VM（JNI）**。理由：纯 C API 贴合 C99；其静态反射模型与**已有** `CSharpManager` 元数据骨架 1:1 同构（可复用同一套 "assembly→type→method" 思路）；嵌入方式（动态加载 `libjvm`）与 Mono 的 `mono_loader` 完全同构。GraalVM 的 polyglot 价值在「一 VM 多语言」，而本项目已分别规划 Mono(C#) 与 QuickJS(JS) 独立桥，故 GraalVM 在此场景下冗余且更重。

---

## 1. 背景与目标

SL 的 PRD 把「foreign language interop」列为目标。Mono 解决 C#、QuickJS 解决 JS，本方案解决 **Java**。
诉求一致：SL 运行时到某次调用，把控制权交给 **HotSpot VM** 执行对应 Java 方法，并正确编回返回值/异常。

---

## 2. 现状与缺口（Gap Analysis）

工程里**无任何 JVM/Java/GraalVM 代码或依赖**（已全文搜索确认），属全新集成。
且**无法像 Mono（DLL）/ QuickJS（源码）那样 vendored**——JRE 体积大，需运行时发现宿主 `libjvm`。

| 缺口 | 位置 | 现状 | 本设计补法 |
|---|---|---|---|
| JVM 引擎 | 宿主 `libjvm` | 不 vendored | `jvm_loader` 运行时发现并动态加载 |
| Java 元数据/注册层 | `src/jvm/jvm_manager.*` | 不存在（新建，对标 `csharp_manager`） | classpath→类→`jclass`/`jmethodID` 注册 |
| Java 调用 IR 节点 | `src/jvm/ir_jvm_call.*` | 不存在（对标 `ir_csharp_call`） | `IRJvmCallInstruction` 持有 `jmethodID` |
| Java 成员函数类型 | `src/jvm/meta_member_function_jvm.*` | 不存在 | `method_call_type = METHOD_CALL_TYPE_JVM` |
| 真正调用 | execute | 不存在 | 调 `jvm_bridge_invoke()` → `Call*Method` |
| 类型编组 | — | 无 | bridge 内 SL 值 ↔ JNI 类型互转 |
| 对象生命周期 | 跨边界 Java 对象 | 无 | `NewGlobalRef`/`DeleteGlobalRef`（全局引用） |
| SL 侧语法 | 词法/语法 | 无 | 新增 `using jvm "x" classpath="..."` / `import` |
| 构建 | `CMakeLists.txt` | 未含 | 加 `src/jvm/*`；编译期仅需 `jni.h`（可 vendored 单头文件） |
| 派发分支 | `runtime_dispatch.c` | 仅 `METHOD_CALL_TYPE_CSHARP` | 增加 `METHOD_CALL_TYPE_JVM` |

> 元数据模型可**直接复用 `CSharpManager` 的形状**：`CSharpType`↔`jclass`，
> `CSharpMethodInfo.method_handle`↔`jmethodID`，assembly 搜索列表↔classpath。
> 因此 `jvm_manager` 可与 `csharp_manager` 几乎同构实现。

---

## 3. 总体架构

```
┌──────────────────────────────────────────────────────────────────────┐
│  SL 源码  (using jvm "com.example.MathOps" classpath="...";            │
│            var o = new MathOps(); o.add(1,2))                          │
└──────────────────────────────────────────────────────────────────────┘
        │  parse / compile
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  Meta 层                                                              │
│   MetaClassJvm / MetaMemberFunctionJvm                                │
│   method_call_type = METHOD_CALL_TYPE_JVM                             │
│   └─ 绑定 JvmMethodInfo* (method_handle : jmethodID)                  │
└──────────────────────────────────────────────────────────────────────┘
        │  lower to IR
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  IR 层   IRJvmCallStatements / IRJvmCallInstruction                    │
│   (target jobject, argv[], method_info)                               │
└──────────────────────────────────────────────────────────────────────┘
        │  VM 执行派发 (runtime_dispatch.c)
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  VM dispatch : if (call_type == METHOD_CALL_TYPE_JVM)                 │
│        └─► ir_jvm_call_instruction_execute()                          │
└──────────────────────────────────────────────────────────────────────┘
        │
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  jvm_bridge  (新增：src/jvm/jvm_bridge.{h,c})                          │
│   - marshalling:  SL 值 ↔ JNI 类型 (jint/jdouble/jstring/jobject)      │
│   - jvm_bridge_invoke(method, thisObj, argv, exc) → Call*Method        │
│   - 全局引用管理 (NewGlobalRef / DeleteGlobalRef)                     │
│   - 异常 → SL 异常 映射                                               │
└──────────────────────────────────────────────────────────────────────┘
        │  调用 JNI
        ▼
┌──────────────────────────────────────────────────────────────────────┐
│  HotSpot VM（宿主 libjvm，由 jvm_loader 动态加载）                     │
│   JNI_CreateJavaVM → JavaVM* + JNIEnv* → 已加载的 Java Class          │
│        └─► Java 方法真实执行                                           │
└──────────────────────────────────────────────────────────────────────┘
```

数据流补充：`jvm_loader` 在进程启动期定位 `libjvm`（`LoadLibrary`/`dlopen`），
解析 `JNI_CreateJavaVM` 并创建 JVM；`jvm_bridge` 用其驱动 `FindClass`/`Call*Method`。
**缺失 `libjvm` 时降级为纯 SL 运行时**（与 Mono 的 `mono_loader` 同构）。

---

## 4. HotSpot/JNI 嵌入方案

### 4.1 JVM 运行时发现与加载（`jvm_loader`）

JRE 不 vendored，需运行时定位 `libjvm`：

- Windows：`<JAVA_HOME>\bin\server\jvm.dll`（或 `bin\client\jvm.dll`）。
- Linux：`<JAVA_HOME>/lib/server/libjvm.so`。
- macOS：`<JAVA_HOME>/lib/server/libjvm.dylib`。
- 解析顺序：环境变量 `SL_JVM_LIB` → `JAVA_HOME` → 配置文件/CLI 参数 → 同目录 `jvm.dll`。

找到后 `LoadLibrary`/`dlopen`，解析符号：
`JNI_CreateJavaVM`、`JNI_GetCreatedJavaVMs`、`JNI_DestroyJavaVM`、
`JNI_GetDefaultJavaVMInitArgs`。与 Mono 的 `mono_loader` 完全同构（动态加载 + 函数指针表）。

### 4.2 生命周期

挂载点同前：CLI 入口 `cli_main`（`src/cli/cli.c`）。

```
cli_main(argc, argv)
  ├─ jvm_bridge_init()       // 发现 libjvm；JNI_CreateJavaVM(&jvm,&env,&vm_args)
  │                           //   vm_args.options 含 "-Djava.class.path=<cp>"
  ├─ ... 现有编译/加载/运行流程 ...
  └─ jvm_bridge_shutdown()   // JNI_DestroyJavaVM(jvm)
```

- `JavaVM*` 进程级、单例；`JNIEnv*` 线程级（本进程主线程创建 VM 后持有）。
- 若 SL VM 后续多线程/协程：进入 Java 调用前需 `AttachCurrentThread`/`DetachCurrentThread`。

### 4.3 类加载与元数据注册

`jvm_manager_init_loaded_list()`（新建，对标 `csharp_manager` 空桩逻辑）：

```
jvm_bridge_load_class(fqcn, classpath):
    // 设置 classpath（VM 初始化 option 或运行时 System.setProperty）
    cls = (*env)->FindClass(env, "com/example/MathOps")   // 斜杠分隔，无 .class
    // 缓存 jclass（需 NewGlobalRef 防被 GC）
    jvm_manager_add_class(...)   // 复用 CSharpManager 同构结构
```

- Java 无 assembly，对应概念为 **classpath / class loader**；`jvm_manager` 维护
  `fqcn → (jclass, jmethodID[])` 映射，与 `CSharpManager` 的 `assembly→type→method` 同构。
- SL 的 `import MathOps;` → `FindClass` 取 `jclass` → 注册 `MetaClassJvm`。
- 方法解析：`GetStaticMethodID(env, cls, "add", "(II)I")`（静态）/
  `GetMethodID(env, cls, "<init>", "()V")`（构造器）/ `GetMethodID(env, cls, "add", "(II)I")`（实例）。
- `method_handle` 语义定为 **`jmethodID`**（与 Mono 定为 `MonoMethod*`、QuickJS 定为 `JSValue` 一一对应）。

### 4.4 方法句柄语义

```c
// src/jvm/jvm_manager.h
struct JvmMethodInfo {
    char name[256];
    jmethodID method_handle;        // JNI 方法 ID（由 Get*MethodID 填充）
    jclass   owner_class;           // 所属类（global ref）
    char is_static;
    char sig[256];                  // JNI 签名，如 "(II)I"
};
```

- `jmethodID` 是 VM 内部稳定句柄，无需额外引用管理；`jclass` 需 `NewGlobalRef` 持有。

---

## 5. SL 侧调用语法设计（建议，与 Mono/QuickJS 对称）

```sl
// 1) 声明要使用的 Java 类，并指定 classpath（触发 FindClass）
using jvm "com.example.MathOps" classpath="lib/mylib.jar";

// 2) 引入类，生成 MetaClassJvm
import MathOps;

function main() {
    var o = new MathOps();          // Java 构造器 → NewObject → jobject(global ref)
    int r = o.add(1, 2);            // 实例方法 → METHOD_CALL_TYPE_JVM → jvm_bridge_invoke
    print(r);                       // 3

    string s = o.greet("world");    // string 编组：NewStringUTF / GetStringUTFChars
    print(s);
}
```

映射（复用既有通路）：
- `using jvm "fqcn" classpath="..."` → `jvm_bridge_load_class` + `FindClass` + classpath 设置。
- `import MathOps` → 注册 `MetaClassJvm`（对标 `MetaClassCSharp`）。
- `o.add(...)` → `MetaMemberFunctionJvm`（`METHOD_CALL_TYPE_JVM`）→ `IRJvmCallInstruction`。
- 静态方法：`import static Utils;` → `is_static=1`，调用时 `thisObj=NULL`（`CallStatic*Method`）。

---

## 6. 调用链路（SL → HotSpot 真实执行）

以 `o.add(1,2)` 为例：

1. SL VM 派发命中 `METHOD_CALL_TYPE_JVM` 分支（`runtime_dispatch.c`）。
2. 取出 `MetaMemberFunctionJvm`，得到 `JvmMethodInfo*`，即 `jmethodID method` 与 `jclass cls`。
3. 进入 `ir_jvm_call_instruction_execute(instruction)`：
   - `instruction->target`：SL 持有的 Java 对象 `jobject`（global ref）。
   - `instruction->argv[]`：SL 运行时值，先**转为 JNI 参数**（见 §7）。
4. `jvm_bridge_invoke(cls, method, target, argv, &exc)`：
   - 实例：`(*env)->CallIntMethod(env, target, method, a, b)`；
     静态：`(*env)->CallStaticIntMethod(env, cls, method, a, b)`。
   - 若 `(*env)->ExceptionCheck(env)` 真：取 `ExceptionOccurred` → 构造 SL 运行时异常 → `ExceptionClear` → 抛出。
   - 否则把返回值**转为 SL 值**，写回 `instruction->return_obj`。
5. VM 把返回值压栈，继续后续 SL 指令。

---

## 7. 类型映射与编组（Marshalling）

| SL 类型 | C 侧表示 | JNI 类型 | 编组方式 |
|---|---|---|---|
| `int` | `int32_t` | `jint` | `CallStaticIntMethod` / `CallIntMethod` 直接传值；回程取 `jint` |
| `float`/`double` | `double` | `jdouble` | `CallStaticDoubleMethod` / `CallDoubleMethod` |
| `bool` | int | `jboolean` | `CallStaticBooleanMethod` / `CallBooleanMethod` |
| `string` | `char*`/`utf8` | `jstring` | `NewStringUTF(env, cstr)`；回程 `GetStringUTFChars`+`ReleaseStringUTFChars` |
| `array<T>` | SL 数组对象 | `jarray` | `New<Type>Array` / `NewObjectArray` + `Set<Type>ArrayRegion`；回程反向 |
| `object`/`class` | SL 对象 | `jobject` | `NewObject`(构造器) / `Call*Method` 返回；持有为 global ref |

要点：
- JNI 调用按 **Java 签名**严格匹配（`(II)I` 表示两 int 参数、返回 int），bridge 需按 `JvmMethodInfo.sig` 选择对应的 `Call*Method` 变体（`CallIntMethod`/`CallDoubleMethod`/`CallObjectMethod`/`CallBooleanMethod`…）。
- 字符串跨边界：Java 内部 UTF-16，`GetStringUTFChars` 返回 UTF-8 缓冲区，必须 `ReleaseStringUTFChars` 释放。
- 基本类型按值传递；对象/数组按 `jobject` 引用传递。

---

## 8. 对象生命周期与全局引用

JVM 使用**分代 GC**，而 SL VM 用**手动内存 + 引用计数**（见 `md/GC_DESIGN.md`）。
跨边界的 Java 对象若只被 C 侧 `jobject` 引用，JVM 可能误回收。

方案：在 `jvm_bridge` 内用 **全局引用** 持有：

- SL 创建/持有一个 Java 对象（`new MathOps()`）：`NewObject` 得到局部 `jobject` →
  `jobject g = (*env)->NewGlobalRef(env, obj)` 提升为全局引用，存进 SL 对象（不透明句柄）。
- 每次方法调用前：直接以该 global ref 作为 `target`。
- SL 对象被 GC/释放时：回调 `(*env)->DeleteGlobalRef(env, g)`。
- 这是 JNI 版的 GCHandle（Mono）/ `JS_DupValue`（QuickJS）——三者角色一致，机制各异。

> 注意：JNI **局部引用**（`FindClass`/`NewObject` 的返回值）仅在当前 native 调用栈有效，
> 跨越多次 SL 调用必须转成 global ref，否则悬垂。

---

## 9. 异常处理

JNI 的异常是「pending 异常」模型：

- 每次 `Call*Method` 后需 `(*env)->ExceptionCheck(env)`（或 `ExceptionOccurred`）。
- 若有异常：`(*env)->ExceptionOccurred(env)` 取 `jthrowable`；读取 `getMessage()` /
  `printStackTrace` 得到信息；`(*env)->ExceptionClear(env)` 清除 pending 状态。
- 把信息构造为 **SL 运行时异常**并向上抛出；SL 侧可用现有 `try/catch`（`test_catchfix*`、`test_try*`）捕获。
- 切勿在未 `ExceptionClear` 的情况下继续调用 JNI（会报 `PendingException` 错误）。

---

## 10. 构建与集成（CMake）

改动 `csimple_lang/CMakeLists.txt`：

1. **新增源文件**到 `CSIMPLE_LIB_SOURCES`：
   ```
   src/jvm/jvm_loader.c
   src/jvm/jvm_manager.c
   src/jvm/jvm_bridge.c
   src/jvm/ir_jvm_call.c
   src/jvm/meta_member_function_jvm.c
   ```
2. **编译期仅需 `jni.h`**：可 vendored 单个稳定的 `jni.h` 头文件到 `third_party/jni/jni.h`
   （JNI 公共头很小、API 稳定），加入 include 目录：
   ```cmake
   target_include_directories(csimple_lang PRIVATE "${CMAKE_SOURCE_DIR}/third_party/jni")
   ```
3. **无需链接 `libjvm`**：采用动态加载（运行时 `LoadLibrary`/`dlopen`），不在 `target_link_libraries` 加 JVM。
4. **无需 post-build 拷 DLL**：`libjvm` 来自宿主 JRE，不由本工程分发。

---

## 11. 改造文件清单

**新增**（`src/jvm/` + `third_party/jni/`）
- `third_party/jni/jni.h`（vendored 单头文件，编译期使用）。
- `src/jvm/jvm_loader.h/.c` —— 发现/加载 `libjvm`、创建/销毁 JVM、解析 JNI 符号。
- `src/jvm/jvm_manager.h/.c` —— classpath/类/`jmethodID` 注册表（同构于 `csharp_manager`）。
- `src/jvm/jvm_bridge.h/.c` —— 类加载、`jvm_bridge_invoke`、marshalling、全局引用管理、异常映射。
- `src/jvm/ir_jvm_call.h/.c` —— `IRJvmCallInstruction`/`IRJvmCallStatements`（对标 `ir_csharp_call`）。
- `src/jvm/meta_member_function_jvm.h/.c` —— `MetaMemberFunctionJvm`（`METHOD_CALL_TYPE_JVM`）。

**修改**
- `src/vm/runtime/dispatch/runtime_dispatch.c` —— 新增 `METHOD_CALL_TYPE_JVM` 分支（或统一 external 派发）。
- `src/cli/cli.c` —— 启动/退出挂 `jvm_bridge_init` / `jvm_bridge_shutdown`。
- `CMakeLists.txt` —— 加入 `src/jvm/*`、include `third_party/jni`、`jni.h` 仅编译期依赖。
- （可选）`src/compile/*` 词法/语法 —— 新增 `using jvm` / `import` 解析，注册 `MetaClassJvm`。
- （可选）调用类型枚举 —— 新增 `METHOD_CALL_TYPE_JVM`（或统一为 `METHOD_CALL_TYPE_EXTERNAL` + 语言标签）。

---

## 12. 分阶段落地计划

- **阶段 1 · 跑通 JVM**：`jvm_loader` 发现并加载 `libjvm`、`JNI_CreateJavaVM` 创建 VM；
  在 C 侧硬编码 `FindClass` + `CallStaticIntMethod` 调用一个静态方法，验证嵌入可用。
- **阶段 2 · 接通调用桩**：`JvmManager` 注册 + `ir_jvm_call_instruction_execute` 真实调用；
  支持 `int`/`string` 基础编组（静态方法优先）。
- **阶段 3 · SL 端到端**：`using jvm` / `import` 语法解析 → `MetaClassJvm` 注册 → 从 SL 调用 Java。
- **阶段 4 · 完整能力**：Java 对象实例（global ref）、属性/字段读写、数组、异常映射、多线程 `AttachCurrentThread`。

---

## 13. 风险与注意事项

1. **JRE 不可 vendored**：需运行时发现宿主 `libjvm`；缺失则降级纯 SL。部署文档需说明「目标机须装 JRE/JDK」。
2. **局部引用陷阱**：跨 SL 调用持有的 `jobject` 必须转 global ref，否则悬垂（§8）。
3. **签名严格匹配**：`Call*Method` 必须按 `JvmMethodInfo.sig` 选对变体，否则 JNI 报错。
4. **Pending 异常**：每次调用后必须 `ExceptionCheck` + `ExceptionClear`，否则后续 JNI 调用失败。
5. **线程模型**：`JNIEnv*` 线程级；多线程需 `AttachCurrentThread`/`DetachCurrentThread`；`DestroyJavaVM` 仅主线程且需等待非守护线程结束。
6. **类加载器/ classpath**：动态增删 classpath 受限，建议在 VM 初始化 option 一次设好 `java.class.path`。
7. **classpath 与 .jar**：需把依赖 jar 列入 classpath；native library（JNI 的 `.so`/`.dll`）用 `System.loadLibrary`。

---

## 14. 小结

本方案**复用** Mono / QuickJS 文档的整体思路，并因 Java 的**静态反射模型**而与 Mono(C#) 桥接骨架**高度同构**：
- `method_handle` → `jmethodID`（类比 Mono 的 `MonoMethod*`、QuickJS 的 `JSValue`）。
- 元数据 `jvm_manager` 直接复用 `csharp_manager` 的 assembly→type→method 形状（assembly≈classpath）。
- 嵌入方式 `jvm_loader`（`LoadLibrary`+函数指针表）与 `mono_loader` 完全同构。
- 跨边界对象用 **global ref**（`NewGlobalRef`/`DeleteGlobalRef`）——对应 Mono 的 GCHandle、QuickJS 的 `JS_DupValue`。
- SL 侧语法 `using jvm "fqcn" classpath="..."` 与 Mono/QuickJS 对称。

---

## 15. 三种外部语言机制对照

| 维度 | Mono (C#) | QuickJS (JS) | HotSpot/JNI (Java) |
|---|---|---|---|
| 引擎引入 | `mono-2.0-sgen.dll`（vendored） | 源码编译进工程（需引入） | 宿主 `libjvm`（运行时发现，不 vendored） |
| 加载方式 | 动态加载 DLL + 函数表 | 直接编译链接 | 动态加载 `libjvm` + 函数表（同 Mono） |
| 调用 API | `mono_runtime_invoke` | `JS_Call` | `Call*Method`（按签名选变体） |
| 元数据模型 | 静态反射（C# 类型） | 动态（名字→`JSValue`） | 静态反射（`FindClass`/`GetMethodID`） |
| 与 `CSharpManager` 同构 | 本身就是 | 否（JS 无静态类型） | **是**（classpath≈assembly） |
| 跨边界对象存活 | `mono_gchandle_new` | `JS_DupValue`/`JS_FreeValue` | `NewGlobalRef`/`DeleteGlobalRef` |
| 线程模型 | `mono_thread_attach` | `JSRuntime` 单线程 | `AttachCurrentThread`/`JNIEnv` 线程级 |
| 异常模型 | `mono_runtime_invoke` 的 exc 出参 | `JS_IsException` 返回值 | pending 异常（`ExceptionCheck`/`Clear`） |
| SL 语法 | `using csharp` | `using js` | `using jvm "fqcn" classpath=` |
| 派发分支 | `METHOD_CALL_TYPE_CSHARP` | `METHOD_CALL_TYPE_JS` | `METHOD_CALL_TYPE_JVM` |

**统一演进建议**：三套 bridge 跑通后，在 VM 派发层抽象出统一的
「外部语言调用（External Language Call）」接口，由 `MetaMemberFunction` 携带语言标签
派发到 `mono_bridge` / `js_bridge` / `jvm_bridge`，使 SL 的 `using <lang>` 成为可插拔的多语言互操作框架。
Java 与 C# 因同构的静态反射模型，可进一步共享同一份「静态语言元数据 + 调用」抽象层。
