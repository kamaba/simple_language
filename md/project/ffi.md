# FFI 外部函数接口（Foreign Function Interface）

> 本文描述 SimpleLanguage **当前已落地** 的 FFI 机制：普通的动态库加载与调用、`FFI.Library` /
> `FFI.StaticLibrary` 两个封装类、`project.jsonc` 的 `dllImports` 配置、`@DllImport` 与
> `@DllStaticImport` 两个声明式 attribute，以及各层内部实现原理。
>
> 设计草案（部分未落地）见 [design/ffi-design.md](../design/ffi-design.md)；本文以**实现**为准。
> 端到端可运行样例见 [test/SpecialTest/FFITest.sl](../../test/SpecialTest/FFITest.sl)。

---

## 0. 总览：三条调用路径

SL 的 FFI 按"绑定时机 + 运行期开销"分成三条路径，共用同一套底层（`cvm` 的 `src/lib/ffi/`）：

| 路径 | 写法 | 绑定时机 | 运行期开销 | 回退 |
|---|---|---|---|---|
| **① 命令式（普通 FFI）** | `FFI.Library(path).getFunction(...)` / `SystemFFICallXxx(...)` | 运行期，每次 `LoadLibrary` + `GetSymbol` | 一次库查表 + 函数值闭包调用 | 无（自己判 null） |
| **② `@DllImport` 声明式** | `@DllImport("lib","sym")` + 函数定义 | 运行期，类加载静态初始化器 | **进 SL 帧** + null 判断 + 闭包调用 | 编译期生成 `else` 分支 |
| **③ `@DllStaticImport` 静态绑定（新）** | `@DllStaticImport("静态名","sym")` + 函数定义 | **cvm assembly build 期**（进程级一次性） | **零 SL 帧**，直调 native | 运行期按 `methodId` 回退函数体 |

分层：

```
SL 源码
  │ FFI.Library / FFI.StaticLibrary / @DllImport / @DllStaticImport
  ▼
编译层 Front(C#)
  │ 普通调用   -> CallStatic / CallClosure
  │ @DllImport -> 源码改写 + bindFunction 注入 -> CallStatic/CallClosure
  │ @DllStaticImport -> CallFFIStatic(118) + SLFFIStaticCallPackage JSON
  ▼
cvm 运行层(C)
  ├─ assembly build 期：静态库预载 + 符号解析 + payload 改写为 4 字节索引
  ├─ lib 层  src/lib/ffi/ ：签名解析 / 库加载去重引用计数 / x64 ABI 派发
  └─ VM 层   src/vm/system_method_call/ffi_system_method.c ：弹参压结果的适配
  ▼
原生 C 动态库（.dll / .so）
```

---

## 1. `project.jsonc` 的 `dllImports` 配置

所有"按名字引用库"的能力都来自这段配置（`ProjectConfig.DllImports`）：

```jsonc
"dllImports": [
  {
    "path": "../../source/CLangdll/x64/Debug/CLangdll.dll",
    "name": "CLangdll",
    "alias": "CLangdll",
    "static": "CLangdll",
    "functions": [
      { "name": "libaddfunc", "symbol": "simplelanguage_addtest", "sig": "i32,i32->i32" }
    ]
  },
  { "path": "../../source/CLangdll/x64/Debug/CLangdll.dll", "name": "CLangdll", "alias": "cl" }
]
```

| 字段 | 作用 |
|---|---|
| `path` | 库文件相对路径（相对工程文件；cvm 侧按进程 CWD 规范化为绝对路径） |
| `name` | 库名 |
| `alias` | 别名。`@DllImport("别名", ...)` 与 `global.dllImport.<alias>` 都按 `alias / name / path` 三者查表（`ProjectConfig.ResolveDllImportPath`） |
| `static` | **静态绑定名**。非空时该库在 cvm 加载模块时预载，句柄持续到进程退出；是 `@DllStaticImport` 与 `FFI.StaticLibrary.GetStaticLib` 的键 |
| `functions` | 库函数变量列表，注入为 `global.<name>(...)` 可直接调用 |

配置带来的两个注入产物：

```sl
// 1) global.dllImport.<alias> —— FFI.Library 实例（免写长路径）
var lib = global.dllImport.CLangdll
var add = lib.lookupFunction<int,int,int>( "simplelanguage_addtest" )

// 2) global.<funcName> —— functions 段注入的库函数变量
var r = global.libaddfunc( 20, 22 )     // == 42
```

> `dllImports` 会随模块导出（`module.json` 的同名段），引用方加载本模块时自动合并别名，
> 本地已配置的同名 alias 优先（不覆盖）。

---

## 2. 路径①：普通 FFI 调用（命令式）

### 2.1 加载库并取符号

```sl
var lib = FFI.Library( "../../source/CLangdll/x64/Debug/CLangdll.dll" )
if ( lib.isValid )
{
    Int64 addfn = lib.getSymbol( "simplelanguage_addtest" )
    Int64 r = SystemFFICallI32( addfn, "i32,i32->i32", 20, 22 )   // 42
    lib.release()
}
```

### 2.2 变参调用 `SystemFFICallXxx`

统一形态 `SystemFFICallXxx( Int64 fn, string sig, ...实参 )`，`sig` 描述 C 侧真实签名：

```sl
SystemFFICallVoid ( fn, "u8,ptr->void", 60, p )
SystemFFICallI32  ( addfn, "i32,i32->i32", 20, 22 )
SystemFFICallI64  ( fn,    "i64,i64->i64", 11, 31 )
SystemFFICallF64  ( fn,    "u8,i16,u32,i64,f32,f64->f64", 200, -1000, u32v, i64v, 2.5f, 1.25 )
SystemFFICallUtf8 ( fn,    "utf8->utf8", "hello ffi" )
SystemFFICallF8E4M3( fn,   "f8e4m3->f8e4m3", 1.5fe4 )
SystemFFICallBool ( fn,    "bool->bool", true )
SystemFFICallF32  ( fn,    "f32->f32", 1.5f )
SystemFFICallF8E5M2( fn,   "f8e5m2->f8e5m2", 2.0fe5 )
```

**返回类型必须与 sig 的返回类型相容**，否则 cvm 告警并压该变体的零值（不中断 VM）：

| 变体 | sig 允许的返回类型 |
|---|---|
| `Void` | `void` |
| `Bool` | `bool, i8, u8, i16, u16, i32, u32` |
| `I32` | `i8, u8, i16, u16, i32, u32` |
| `I64` | `i64, u64, ptr, utf8` |
| `F32` | `f32, f16, bf16` |
| `F64` | `f64` |
| `Utf8` | `utf8, ptr` |
| `F8E4M3` / `F8E5M2` | 仅对应 `f8e4m3` / `f8e5m2` |

### 2.3 函数值：`getFunction` / `lookupFunction`（Dart `lookupFunction` 风格）

把符号地址包装成可直接调用的 SL 函数值：

```sl
var lib = FFI.Library( path )

// 显式 sig
Func<int,int,int> addf = lib.getFunction( "simplelanguage_addtest", "i32,i32->i32" )

// 只传 name —— 编译期从左侧函数类型定义推导 sig 并注入
Func<int,int,int> addg = lib.getFunction( "simplelanguage_addtest" )
AddFuncTA         addta = lib.getFunction( "simplelanguage_addtest" )   // typealias 同样可推导

// 模板形态：lookupFunction<Ret, P...>( name ) -> 内部改写为 getFunction
var addl = lib.lookupFunction<Int64,Int64,Int64>( "sl_add" )
Func<Int64,string> strlenf = lib.lookupFunction<Int64,string>( "sl_strlen_utf8" )

addf( 20, 22 )      // 42
```

> `getSymbol` 返回裸地址；当它被赋给函数类型变量时，前端同样改写为 `getFunction` 并注入推导 sig。

### 2.4 回调：把 SL 静态方法交给 native 调用

```sl
Int64 cb = FFI.StaticLibrary.createCallback(
    "SpecialTest.FFICallbacks.OnAdd_2_Core.Int64_Core.Int64", "i64,i64->i64" )
Int64 r = SystemFFICallI64( callCb, "ptr,i64->i64", cb, 20 )   // C 侧回调回 SL
FFI.StaticLibrary.freeCallback( cb )
```

约束：trampoline 形状固定 `int64(int64,int64)` —— **INT 类参数 ≤ 2，返回 void 或 INT 类**（不支持浮点参数）；进程级共 8 个槽。`method` 可传完整 method id 或短方法名（按参数个数匹配）。

### 2.5 配套：原生内存 `Memory` / `SystemPtr*`

FFI 常与原生内存配合。核心原生内存 API 已迁入 `Core/Memory`（`Memory.alloc / allocArray / readXxx / writeXxx / sizeOf / structSize / structFieldOffset / newStruct / newUtf8 / copyUtf8 / freeNative`），另有底层 `SystemPtrAlloc / SystemPtrFree / SystemPtrReadInt32 / SystemPtrWriteFloat64 ...`，以及 `data` ↔ C struct 互转：

```sl
Int64 p  = Memory.alloc( 64 )
Memory.writeI32( p, 0, -100 );  Memory.writeI64( p, 8, 9000000000 )
Float64 total = SystemFFICallF64( structSum, "ptr->f64", p )
Memory.freeNative( p )

Int64 addr = Memory.dataToNativeStruct<FFICBook>( "SLBookStruct", book )   // data -> C struct
FFICBook b = Memory.nativeStructToData<FFICBook>( addr )                   // C struct -> data
```

---

## 3. `FFI.Library` 类

源码：[source/Front/Lib/Std/FFI/Library.sl](../../source/Front/Lib/Std/FFI/Library.sl)

动态库句柄封装：加载 / 符号解析 / 引用计数 / 释放。**同路径共享同一份底层句柄（去重缓存）**，每次加载引用计数 +1。

```sl
public class FFI.Library extends Object
{
    Int64 _handle = 0

    public void _init_()                    // 空句柄
    public void _init_( string path )       // 按路径加载（引用计数 +1），失败句柄为 0
    public void _init_( Int64 handle )      // 以已有句柄包装（@DllStaticImport 静态绑定辅助）

    get Int64 handle()                      // 句柄（诊断用）
    get bool  isValid()                     // 句柄 != 0

    public Int64 getSymbol( string name )                    // 符号地址，0 = 未找到
    public Function getFunction( string name, string sig )   // 包装为可调用 Function 值，失败 null
    public Int32 addRef()                                    // 引用 +1，返回新计数
    public Int32 refcount()                                  // 当前计数，-1 = 句柄无效
    public bool release()                                    // 引用 -1
}
```

要点：

- `release()` 返回 `false` 表示**句柄无效**；仅当本次是最后一个引用（真正卸载）时才把 `_handle` 清 0，否则句柄仍有效。
- 对死句柄（加载失败 / 已卸载）调用 `release()` 返回 `false`，不报错。
- `getFunction(name)` 省略 `sig` 时，前端按声明目标（`Func<Ret,P...>` 或函数 `typealias`）推导并注入。

### 库生命周期实测语义

```sl
var lib  = FFI.Library( path )
int r0   = lib.refcount()          // 已有静态引用时 r0 >= 1
lib.addRef()                       // -> r0+1
var lib2 = FFI.Library( path )     // 同路径去重：共享句柄，refcount -> r0+2
lib2.release()                     // -> r0+1，lib2 仍 isValid
lib.release()                      // -> r0，句柄保持有效

var bad = FFI.Library( "no_such_library_zzz.dll" )
bad.isValid == false ; bad.release() == false
```

---

## 4. `FFI.StaticLibrary` 类

源码同上文件尾部。承载**不依赖具体句柄**的全局能力：静态绑定库缓存、全局统计、回调注册、`@DllImport` 绑定辅助。

```sl
public class FFI.StaticLibrary extends Object
{
    static List<Library> s_staticLibs = List<Library>()

    public static Int32 libraryCount()                                  // 当前已加载（未卸载）库个数
    public static Library GetStaticLib( string name )                   // 按静态名取预载库，未注册 null
    public static Int64 createCallback( string method, string sig )     // SL 静态方法 -> trampoline 地址
    public static bool freeCallback( Int64 addr )                       // 释放回调槽
    public static Function bindFunction( string path, string symbol, string sig )   // @DllImport 辅助
}
```

### `GetStaticLib( name )`

按 `project.jsonc` 的 `"static"` 字段名取**已预载**的库，返回 `FFI.Library` 包装：

```sl
var lib  = FFI.StaticLibrary.GetStaticLib( "CLangdll" )
var lib2 = FFI.StaticLibrary.GetStaticLib( "CLangdll" )   // 同一实例（内部 List 去重）
FFI.StaticLibrary.GetStaticLib( "no_such_static_lib" )    // null
```

静态名必须在 `dllImports` 里配了 `"static"`，否则返回 `null`。

### `bindFunction( path, symbol, sig )`

`@DllImport` 的编译期注入目标，等价于：

```sl
ret FFI.Library( path ).getFunction( symbol, sig )
```

库按 C# `DllImport` 语义**进程级常驻**（去重缓存共享句柄，不 release）。

---

## 5. 路径②：`@DllImport` 声明式绑定

C# `[DllImport("lib")] static extern ...` 风格。**必须带函数体**——dll 绑定不可用时执行函数体（fallback 本体）。

```sl
@DllImport( "../../source/CLangdll/x64/Debug/CLangdll.dll", "simplelanguage_addtest" )
static int s_dllAdd( int a, int b )
{
    ret a + b           // fallback：与 dll 等价的 SL 实现
}

@DllImport( "CLangdll", "sl_mul2", "i64->i64" )      // 第 1 实参可用 dllImports 别名
static Int64 s_dllMul2( Int64 v ) { ret v * 2 }

@DllImport( "CLangdll", "sl_book_alloc", "i32,f32,i32,utf8->ptr" )   // Ptr 不可推导 -> 手写 sig
static Int64 s_bookAlloc( int id, float price, int pages, string title ) { ret 0 }
```

**实参**：`( 库路径或别名, 符号名 [, sig] )`。`sig` 缺省时从函数签名推导。

### 编译期做了什么

`DllImportSourceRewriter`（**Token 之前的纯文本改写**，行号守恒）把函数定义"炸开"成两段：

```sl
// 改写后（概念形态）
@DllImport( "CLangdll", "sl_add" ) static Func<Int64,Int64,Int64> __dll_addCs
static Int64 addCs( Int64 a, Int64 b )
{
    if ( __dll_addCs != null ) { ret __dll_addCs( a, b ) }
    else { ret a + b }                       // 原函数体原样保留为 fallback
}
```

随后 `MetaMemberVariable.TryCreateDllImportExpress` 给隐藏字段注入初始化表达式：

```sl
__dll_addCs = FFI.StaticLibrary.bindFunction( "CLangdll", "sl_add", "i64,i64->i64" )
```

**运行期**：类加载时执行静态初始化器 → `LoadLibrary + GetSymbol` → 成功则字段为函数值；失败则为 `null`，调用走 `else` 分支。

---

## 6. 路径③：`@DllStaticImport` 静态绑定快速调用（新机制）

### 6.1 是什么

`@DllStaticImport` 让被标注的**静态函数**在调用点编译为专用指令 `CallFFIStatic(118)`：

- 库在 **cvm 加载模块（assembly build 期）** 就按 `"static"` 名预载，符号**只解析一次**；
- 指令 payload 从 JSON 改写为 **4 字节绑定表索引**；
- 运行期 handler 直接整合栈上参数调用 native —— **不进 SL 帧、不走 `Library.Load` 链路**；
- 绑定失败（静态名未注册 / 符号缺失 / sig 非法）时**运行期回退执行函数体**（SL 实现的等价逻辑）。

### 6.2 用法

前置条件：`project.jsonc` 中该库必须配 `"static"` 字段。

```jsonc
{ "path": "../../source/CLangdll/x64/Debug/CLangdll.dll",
  "name": "CLangdll", "alias": "CLangdll", "static": "CLangdll" }
```

```sl
# 第 1 实参 = jsonc "static" 字段的静态名；第 3 实参 sig 可省略（从签名推导）
@DllStaticImport( "CLangdll", "simplelanguage_addtest" )
static int s_statAdd( int a, int b )
{
    ret a + b                       # 绑定失败时的 fallback 体
}

@DllStaticImport( "CLangdll", "sl_mul2", "i64->i64" )
static Int64 s_statMul2( Int64 v ) { ret v * 2 }

@DllStaticImport( "CLangdll", "sl_strlen_utf8" )
static Int64 s_statStrlen( string s ) { ret s.length }

# 静态名未注册 -> 绑定失败 -> 回退函数体
@DllStaticImport( "no_such_static_lib", "no_such_symbol" )
static int s_statFbAdd( int a, int b ) { ret a + b }
```

调用点就是普通静态函数调用，无感知：

```sl
check( "static add(20,22)==42", s_statAdd( 20, 22 ) == 42 )
check( "static strlen('abc123')==6", s_statStrlen( "abc123" ) == 6 )
```

### 6.3 与 `@DllImport` 的关键差异

| | `@DllImport` | `@DllStaticImport` |
|---|---|---|
| 第 1 实参语义 | 库**路径**或 dllImports 别名（会查表替换为路径） | jsonc **`"static"` 静态名**（**不做别名查表**） |
| 源码改写 | 有（隐藏 `__dll_x` 字段 + `if/else` wrapper） | **无**，函数体原样保留 |
| 绑定时机 | 运行期（类加载静态初始化器） | **cvm assembly build 期** |
| 库生命周期 | 引用计数管理，进程常驻（不 release） | 预载 + `addRef`，**永不 release**，持续到进程退出 |
| 调用指令 | `CallStatic` + `CallClosure` | **`CallFFIStatic(118)`** |
| 成功路径开销 | 建 SL 帧 + null 判断 + 闭包调用 | **零 SL 帧**，直接 marshalling 后调 native |
| 回退判定 | 编译期生成的 `if (__dll_x != null)` | 运行期：绑定项 `native_fn == NULL` |
| 回退目标 | `else` 分支（源码级） | `methodId` 指定的原 SL 方法（元数据级） |
| 额外编译期回退 | 无 | 实参 < 2 或 sig 推导失败 → 直接发射 `CallStatic` |

### 6.4 选择建议

- 需要**跨平台兜底 / 库可能不存在** → `@DllImport`（fallback 语义直观，且可随时换库）。
- 库**必定存在**、调用**高频**、追求最低开销 → `@DllStaticImport`。
- 需要动态决定库路径、或运行时多次装卸 → 普通 `FFI.Library` 命令式。

---

## 7. FFI 签名字符串（sig）

格式：`"参数类型,参数类型,...->返回类型"`，与 cvm `sl_ffi_sig_parse` 一致。

### 7.1 类型表

| FFI 短名 | C 类型 | 传参分类 | SL 类型名（运行期可写） |
|---|---|---|---|
| `void` | `void` | — | `void` |
| `bool` | `bool`(C) | INT | `bool` / `boolean` |
| `i8` / `u8` | `int8_t` / `uint8_t` | INT | `Int8` / `UInt8` / `byte` |
| `i16` / `u16` | `int16_t` / `uint16_t` | INT | `Int16` / `UInt16` / `short` |
| `i32` / `u32` | `int32_t` / `uint32_t` | INT | `int` / `Int32` / `UInt32` / `uint` |
| `i64` / `u64` | `int64_t` / `uint64_t` | INT | `long` / `Int64` / `UInt64` |
| `f32` | `float` | DBL | `float` / `Float32` |
| `f64` | `double` | DBL | `double` / `Float64` |
| `f16` / `bf16` | half / bfloat16 | DBL | `Float16` / `Float16_Brain` |
| `f8e4m3` / `f8e5m2` | FP8 E4M3 / E5M2 | DBL | `Float8` / `Float8_E5M2` |
| `ptr` | `void*` | INT | `ptr` / `Ptr` |
| `utf8` | `char*` | INT | `string` / `String` |

### 7.2 语法规则

1. **必须含 `->`**；返回段可省略（视为 `void`）。
2. 零参写 `"->i32"`；但 **`"->void"` 非法**。
3. 逗号分隔，前后空白与尾随逗号容忍。
4. **参数个数 ≤ 6**（`SL_FFI_MAX_ARGS`）——x64 寄存器传参上限，不支持栈传参。
5. 未知类型名 → 解析失败。

### 7.3 编译期推导 vs 运行期解析（重要不对称）

- **编译期推导**（`@DllImport` / `@DllStaticImport` / `getFunction` 省略 sig / `lookupFunction<T>`）
  由 `MetaDefineVarStatements.FFISigNameOfMetaType` 完成，**`Ptr` 被保守排除**——
  带指针参数/返回的函数必须**手写第 3 实参 sig**。
- **运行期解析**（`SystemFFICallXxx` / `SystemFFIMakeFunction` / `createCallback`）
  会先做 **SL 名 → FFI 短名**映射（`vm_ffi_sl_name_to_ffi`），未登记的名字原样直传。
- **静态绑定路径例外**：assembly build 期直接 `sl_ffi_sig_parse(sig, &plan)`，**不做 SL 名映射**，
  因此 `@DllStaticImport` 的 sig 必须写 FFI 短名（`"i32,i32->i32"` 而非 `"Int32,Int32->Int32"`）。

---

## 8. 内部原理

### 8.1 分层

| 层 | 位置 | 职责 |
|---|---|---|
| lib 层（不依赖 VM） | `csimple_lang/src/lib/ffi/` | `sl_ffi_types`（类型/签名解析）、`sl_ffi_lib_manager`（加载/去重/引用计数/卸载）、`sl_ffi_call`（x64 ABI 派发，由 `gen_sl_ffi_call.py` 生成） |
| VM 适配层 | `src/vm/system_method_call/ffi_system_method.c` | `SystemFFI*` 系统方法：弹参 → 装载 `SLFFIArg` → lib 层调用 → 压结果；回调 trampoline 重入 VM；静态绑定表 |
| 汇编/加载层 | `src/vm/load/slir_json_module_loader.c`、`src/vm/assembly/sl_runtime_assembly.c` | 解析 `dllImports`、assembly build 期预载静态库 + 解析符号 + 改写 payload |
| 解释执行 | `src/vm/runtime/vm_runtime.c` | `case OpCode_CallFFIStatic`、`case OpCode_CallClosure` 的 native 闭包分支 |

系统方法绑定方式：Front 在 `Std.jsonc` 用 `cvmFunction` 字段写 C 符号名，VM 运行期按符号地址解析，
**没有硬编码 name→函数映射表**。

### 8.2 `sl_ffi_lib_manager`：去重与引用计数

```c
typedef struct _SLFFILibHandle {
    void* dl_handle; int32 refcount; int32 slot; char path[512];
} SLFFILibHandle;
static SLFFILibHandle* s_libs[32];      // 上限 32 个库
```

`sl_ffi_lib_load` 流程：

1. 相对路径 → **规范化为基于进程 CWD 的绝对路径**（避免宿主 `SetDllDirectory` 干扰搜索序）；
2. 按规范化路径**去重**（Windows `_stricmp` / Unix `strcmp`），命中则 `++refcount` 返回同一句柄；
3. 依次尝试：规范化路径 → **可执行文件所在目录 + 裸文件名**；
4. 新建句柄 `refcount = 1`。

`sl_ffi_lib_release`：`--refcount`，归零才真正 `FreeLibrary/dlclose`，并用表尾元素填补空洞保持紧凑。
`sl_ffi_lib_add_ref` 返回新计数；`sl_ffi_lib_refcount` 对无效句柄返回 `-1`。

### 8.3 `sl_ffi_call`：不用 libffi 的 x64 ABI 派发

调用形状 = `argc(0..6) × fp_mask(2^argc)`，由脚本**静态展开成 127 个 case**：

```c
case 2:  /* sl_ffi_call.c */
  case 0: return ((int64(*)(int64,int64))fn)(gi[0], gi[1]);
  case 1: return ((int64(*)(double,int64))fn)(gd[0], gi[1]);
  ...
```

参数分两类：INT 类（bool/i8..u64/ptr/utf8，统一 int64 走通用寄存器）、DBL 类
（f32/f64/f16/bf16/f8*，统一 double 走 XMM，f32 位模式放低 32 位）。返回按类型三分派：
Void / F64 类（读 XMM0 全 64 位）/ F32 类（读 XMM0 低 32 位）/ 默认 INT 类（读 RAX）。

> 因此**仅支持 x64**，且**不支持 >6 参**（栈传参）。

### 8.4 `SystemFFICallXxx` 的执行流程（`vm_ffi_call_core`）

```
1. argc = param_count - 2                       （去掉 fn 与 sig 两个前置参数）
2. 从栈顶向右到左弹出 argc 个实参               （最后一个实参在栈顶）
3. 弹 sig 字符串 -> vm_ffi_sig_rewrite_sl_names （SL 名 -> FFI 短名，缓冲 160 字节）
4. 弹 fn 地址
5. sl_ffi_sig_parse( sig, &plan )
6. 校验：param_count 匹配 / 返回类型与变体相容 / fn != 0   -> 任一失败：告警 + 压零值
7. vm_ffi_load_arg：VMRuntimeValue -> SLFFIArg
8. sl_ffi_call( fn, &plan, argc, args, &ret )
9. vm_ffi_push_result：按 plan.ret 压回 VM 栈
```

失败语义统一为「日志 warning + 压该变体的零值默认」，**不中断 VM**。

### 8.5 `SystemFFIMakeFunction`：native 闭包

两种形态：

```sl
SystemFFIMakeFunction( fn, "i32,i32->i32" )          // 含 "->" -> s1 即完整 sig
SystemFFIMakeFunction( fn, "Int32", "Int32", "Int32" ) // 分离形态：返回类型 + 各参数类型
```

生成的对象带 magic `'NATF'`；解释器的 `CallClosure` 分支先逆序保存实参、弹闭包对象，
命中 native 闭包后同样走 `vm_sys_ffi_invoke_native_closure`（失败才按普通 SL 闭包处理）。

### 8.6 回调 trampoline

`createCallback` 分配 8 个静态槽之一，返回 `s_ffi_trampolines[slot]` 地址（形状 `int64(int64,int64)`）。
C 侧调用时经 trampoline **重入 VM**：解析宿主 `RuntimeType` → 压实参 → `vm_execute_method_by_id` → 弹返回值。
异常/失败返回 0 并丢弃栈上实参。

### 8.7 `@DllImport` 的完整编译流水线

```
[File/Token 前置] DllImportSourceRewriter.Rewrite( buffer )       FileParse.cs
   └─ 精确匹配 "@DllImport"（"@DllStaticImport" 不命中）
   └─ 校验：≥2 字符串实参 / [修饰符]* static / Ret name(T n,...) / 必填 {...} 体
   └─ 产出 @DllImport(...) static Func<...> __dll_name  +  同名 wrapper( if/else )
[File/Node]  IsBareMemberVariableDecl：把无 '=' 的裸声明切为独立成员
[MetaCore]   MetaMemberVariable.TryCreateDllImportExpress
   ├─ 必须 static / 必须无已有初始化表达式 / args >= 2
   ├─ ResolveDllImportPath( 别名 ) -> 完整路径
   ├─ sig = 第 3 实参 ?? BuildFFIFunctionSig( Func<Ret,P...> )
   └─ 合成 FFI.StaticLibrary.bindFunction( path, symbol, sig )
[MetaCore]   AttributeManager "DllImport" handler：仅校验 + 日志（注入早已完成）
[ParseStmt]  wrapper 体内 __dll_x(a,b) -> 成员函数未命中 -> 回退查成员变量 -> ClosureCall
[IR]         静态初始化器：bindFunction 调用 IR + StoreStaticField -> __dll_x
[运行期]     类加载 -> bindFunction；成功=函数值，失败=null -> else 分支
```

### 8.8 `@DllStaticImport` 的完整编译流水线

```
[File]       无源码改写（函数定义原样保留）
[MetaCore]   MetaMemberFunction 收集 attribute 到 m_AttributeList
[MetaCore]   AttributeManager "DllStaticImport" handler：仅校验 + 日志
[IR]         IRCall.Parse（callType == 0，即静态调用）
   ├─ 参数按 CallStatic 完全一样顺序压栈
   ├─ 扫 mmf.attributeList 找 "DllStaticImport"
   ├─ sig = 第 3 实参 ?? BuildFFIFunctionSigFromMetaFunction( mmf )
   ├─ IRData{ opCode = CallFFIStatic(118), payload = SLFFIStaticCallPackage JSON,
   │          index  = paramCount }
   └─ 【编译期回退】args < 2 或 sig 推导失败 -> 记 Log -> 退化 CallStatic
[Export]     module.json 的 dllImports[].static 随包导出
[cvm build]  预载静态库 + 解析符号 + payload 改写为 4 字节索引
[运行期]     CallFFIStatic handler
```

前端 payload（`SLFFIStaticCallPackage`）：

```csharp
public sealed class SLFFIStaticCallPackage {
    public string lib;          // 静态库名（jsonc "static" 字段）
    public string symbol;       // 导出符号名
    public string sig;          // FFI 签名
    public string methodId;     // 回退目标：被标注的 SL 函数 id
    public string methodName;
    public int    paramCount;
    public bool   tryCatch;
}
```

### 8.9 assembly build 期：静态库预载与 payload 改写

**预载**（`sl_runtime_assembly.c`，在 class/method 注册之前）：

```c
for (pi...) for (mi...) for (i = 0; i < module->dll_imports_count; ++i) {
    SLDllImportPackage* di = &module->dll_imports[i];
    if (di->static_name == NULL || di->static_name[0] == '\0') continue;
    vm_sys_ffi_static_lib_register(di->static_name, di->path, pkg_dir);
}
```

`vm_sys_ffi_static_lib_register` 幂等：已注册直接返回；否则 `sl_ffi_lib_load`
（先按原路径，相对路径失败再拼 `pkg_dir/path`），成功后**再 `addRef` 一次且永不 release**，
保证进程级常驻。

**指令改写**（`sl_assembly_rewrite_ffi_static_instruction`）：

```
1. 解析 payload JSON -> lib / symbol / sig / methodId / paramCount / tryCatch
2. vm_sys_ffi_static_lib_find( lib ) -> sl_ffi_lib_resolve( handle, symbol )
       库未预载 / 符号缺失 -> 告警，native_fn 保持 NULL
3. native_fn != NULL 时 sl_ffi_sig_parse( sig, &plan )
       解析失败 -> native_fn = NULL（快速路径不可用）
4. target_rt = 按 methodId 预解析回退帧的类型上下文
5. index = vm_sys_ffi_static_binding_add( native_fn, &plan, methodId, param_count, try_catch, target_rt )
6. 就地改写：memcpy( payload, &index_le, 4 );  payload_length = 4
```

**全量扫描**：遍历所有 package → module → 方法的 `instruction_list` 与类字段的 `express_list`，
凡 `op_code == OpCode_CallFFIStatic` 即改写。字段初始化表达式与方法指令共享 payload，改写自动传播。

绑定表条目：

```c
typedef struct _VMFFIStaticBinding {
    void*        native_fn;    // NULL = 回退 SL 方法体
    SLFFICallPlan plan;
    char*        method_id;    // 回退目标
    int32        param_count;
    int32        try_catch;
    RuntimeType* target_rt;    // 回退帧类型上下文（预解析缓存）
} VMFFIStaticBinding;
```

两张表（静态库表 + 绑定表）均为**进程级常驻**，`vm_sys_ffi_cleanup` 不清绑定表（同进程多 VM 场景下
索引仍有效），只在进程/根 VM 退出时统一卸载动态库。

### 8.10 运行期：`OpCode_CallFFIStatic`(118) handler

栈约定：调用前 `[..., arg0, ..., arg_{M-1}]`（**栈顶是最后一个实参**），无 this、无闭包对象。

```
指令外层格式 [op:1][plen:4][payload:plen]
   ├─ plen == 4  -> 已绑定：memcpy 出 uint32 索引 -> vm_sys_ffi_static_binding_get(idx)
   │                entry == NULL -> error_code = -54，停止 VM
   └─ plen != 4  -> 未改写（OOM 兜底）：解析 JSON 取 methodId/paramCount/tryCatch
                    -> vm_frame_push_call（实参留在栈上，等同 CallStatic）

entry->native_fn != NULL（快速路径）:
    base_malloc0( param_count * sizeof(VMRuntimeValue) )      // 失败 -> error_code = -53
    for (ai = param_count-1; ai >= 0; --ai)                   // 逆序弹参还原声明序
        vm_pop_stack_top_to_vmvalue( vm, &saved_args[ai] )    // 下溢 -> null + warning
    vm_sys_ffi_invoke_native_closure( vm, native_fn, &plan, saved_args, param_count )
        └─ vm_ffi_load_arg 逐参装载 -> sl_ffi_call -> vm_ffi_push_result 压栈
    base_free( saved_args )

entry->native_fn == NULL（回退路径）:
    vm_frame_push_call( vm, method_id, target_rt, NULL, 0, try_catch )
    // 失败或无 methodId -> 丢弃实参保持栈平衡 / error_code = -54
```

错误码：`-53` 参数缓冲分配失败；`-54` 绑定索引越界或无回退 methodId。

### 8.11 生命周期与清理

| 时机 | 动作 |
|---|---|
| 每 VM 结束 | `vm_sys_ffi_callback_registry_clear_vm(vm)`（对象池释放前，防悬空 `VM*`） |
| 根 VM（`level == 0`）结束 | `vm_sys_ffi_cleanup()`：清回调槽 + `sl_ffi_lib_manager_clear` 卸载全部动态库 |
| 模块卸载 | `dll_imports` 镜像字符串释放；**预载静态库不卸载** |
| 静态绑定表 | 进程级常驻，**不随 VM 重建清理** |

---

## 9. 限制与已知边界

| 项 | 说明 |
|---|---|
| 架构 | 仅 **x64**（`sl_ffi_call` 静态展开 x64 ABI） |
| 参数个数 | ≤ 6（`SL_FFI_MAX_ARGS`），不支持栈传参 |
| 变参 C 函数 | 不支持 `printf` 类 varargs（与 Dart FFI 一致） |
| 库个数 | ≤ 32（`SL_FFI_LIB_TABLE_MAX`） |
| 回调槽 | ≤ 8，且 trampoline 形状固定 `int64(int64,int64)`（**不支持浮点参数，INT 参数 ≤ 2**） |
| 静态绑定 sig | 必须是 FFI 短名，不接受 SL 类型名（build 期不做映射） |
| `Ptr` 推导 | 编译期 sig 推导排除 `Ptr`，须手写 sig |
| `->void` | 非法写法 |
| 回调 method id | ≤ 191 字符 |
| C++ / 类成员函数 | 仅支持 `extern "C"` 扁平 ABI |
| 静态名校验 | 前端**不校验**静态名是否已注册，写错只在运行期表现为"回退函数体" |

---

## 10. 相关文件清单

**SL 侧**

| 文件 | 说明 |
|---|---|
| `source/Front/Lib/Std/FFI/Library.sl` | `FFI.Library` / `FFI.StaticLibrary` |
| `source/Front/Lib/Std/Std.jsonc` | `SystemFFI*` / `SystemPtr*` 系统方法声明与 `cvmFunction` 绑定 |
| `source/Front/Core/AttributeManager.cs` | `DllImport` / `DllStaticImport` handler（校验 + 日志） |
| `source/Front/Compile/Parse/DllImportSourceRewriter.cs` | `@DllImport` 源码文本改写器 |
| `source/Front/Core/MetaMemberVariable.cs` | `bindFunction` 初始化表达式注入 |
| `source/Front/Core/Statements/MetaDefineVarStatements.cs` | sig 推导（`BuildFFIFunctionSig*`） |
| `source/Front/IR/IRCall.cs` | `CallFFIStatic` 发射（118） |
| `source/Front/IROpEnum.cs` | opcode 定义 |
| `source/Front/Export/SLIR/SLIRTypes.cs` | `SLDllImportPackage` / `SLFFIStaticCallPackage` |
| `source/Front/Project/ProjectConfig.cs`、`ProjectJsoncLoader.cs` | `dllImports` 配置与别名解析 |
| `source/Front/Project/PorjectClass.cs` | `global.dllImport.<alias>` / `global.<funcName>` 注入 |

**cvm 侧（`csimple_lang`）**

| 文件 | 说明 |
|---|---|
| `src/lib/ffi/sl_ffi_types.{h,c}` | 类型表、sig 解析 |
| `src/lib/ffi/sl_ffi_lib_manager.{h,c}` | 加载 / 去重 / 引用计数 / 卸载 |
| `src/lib/ffi/sl_ffi_call.{h,c}`（+ `gen_sl_ffi_call.py`） | x64 ABI 派发 |
| `src/vm/system_method_call/ffi_system_method.{h,c}` | `SystemFFI*`、回调 trampoline、静态绑定表 |
| `src/vm/assembly/sl_runtime_assembly.c` | 静态库预载 + payload 改写 |
| `src/vm/assembly/slir_assembly_data.h` | `SLDllImportPackage`（含 `static_name`） |
| `src/vm/load/slir_json_module_loader.c` | `dllImports`（含 `"static"`）解析 |
| `src/vm/vm.h` | `OpCode_CallFFIStatic = 118` |
| `src/vm/runtime/vm_runtime.c` | opcode 118 handler、`CallClosure` native 分支、cleanup |

---

## 11. 可运行样例

`test/SpecialTest/FFITest.sl`（工程配置 `test/SpecialTest/ProjectTest.jsonc`）覆盖：

1. 库生命周期（加载 / addRef / 同路径去重 / 释放 / 引用计数）
2. 基础标量调用（i32 / i64）
3. 多宽度标量混合（`u8,i16,u32,i64,f32,f64->f64`）
4. Float8 / Float16 与 struct 位域分解、roundtrip
5. 混合标量打包进 struct + 指针读回/改写
6. 函数指针（native 返回函数指针、ptr 形参调用）
7. 回调（含嵌套重入 / 双回调）
8. Utf8 字符串进出
9. 函数值（`Func<>` / typealias / `lookupFunction<T>`）
10. `Memory` 原生内存、`data` ↔ C struct 互转
11. `@DllImport`（路径 / 别名 / C# P/Invoke 风格 / fallback）
12. `dllImports` 配置与 `global.dllImport.<alias>`、`global.<funcName>`
13. **`@DllStaticImport` 与 `FFI.StaticLibrary.GetStaticLib`**

---

## 12. 速查

```sl
// ── 命令式 ─────────────────────────────────────────────
var lib = FFI.Library( path )
Int64 fn = lib.getSymbol( "sym" )
Int64 r  = SystemFFICallI32( fn, "i32,i32->i32", 20, 22 )
Func<int,int,int> f = lib.getFunction( "sym" )          // sig 编译期推导
lib.release()

// ── 全局能力 ───────────────────────────────────────────
FFI.StaticLibrary.libraryCount()
FFI.StaticLibrary.GetStaticLib( "静态名" )               // 预载库，未注册 null
FFI.StaticLibrary.createCallback( "methodId", "i64,i64->i64" )
FFI.StaticLibrary.freeCallback( addr )

// ── 声明式 ─────────────────────────────────────────────
@DllImport( "别名或路径", "符号" [, "sig"] )
static int f( int a, int b ) { ret a + b }              // 函数体 = fallback

@DllStaticImport( "静态名", "符号" [, "sig"] )
static int g( int a, int b ) { ret a + b }              // 函数体 = fallback

// ── 配置驱动 ───────────────────────────────────────────
global.dllImport.<alias>        // FFI.Library 实例
global.<funcName>( ... )        // dllImports functions 段注入的库函数
```
