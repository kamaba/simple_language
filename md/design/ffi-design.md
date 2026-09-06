# FFI 机制设计 —— SimpleLanguage 仿 Dart FFI

> 状态:设计版(草案)。本文先整理 Dart 的 FFI 机制,再基于本语言 **现有 Native 互操作基础**
> (`source/Front/External/Native/` 下的 `DartStyleNativeFunction`、`NativeLibraryLoader`、
> `NativeExportManifestReader`、`NativeExportModels`,以及 `CLangdll` 示例与 `MathExternalModule`
> 自动注册机制)给出 SL 的 FFI 语法/语义/降级方案与各编译层设计,并明确列出 **要实现的
> 功能点清单**(第 4 节,★ 重点)。

---

## 0. 背景与现有机制盘点(结合现在的机制)

### 0.1 已有的"现成积木"(直接复用,不必从零造)

| 模块 | 文件 | 现状 | FFI 中的角色 |
|---|---|---|---|
| 库加载器 | `source/Front/External/Native/NativeLibraryLoader.cs` | 已实现 `Load/GetExport/TryFree`(`NativeLibrary.Load/GetExport/Free`,即 `dlopen/LoadLibrary` + `dlsym/GetProcAddress` 的 .NET 抽象) | `DynamicLibrary` 的底层引擎 |
| 导出清单读取 | `source/Front/External/Native/NativeExportManifestReader.cs` | 已实现:优先调用原生库导出的 `const char* sl_exports_json()`,否则回退到同名 `*.slffi.json` 边车文件 | 库能力自动发现 |
| 类型/函数描述模型 | `source/Front/External/Native/NativeExportModels.cs` | 已有 `SLNativeValueType{Void,Bool,I32,I64,F32,F64,Ptr,Utf8String}`、`SLNativeFunctionExport`、`SLNativeLibraryExportManifest`、`SLNativeCallingConvention{Cdecl,StdCall}` | FFI 类型映射表的雏形 |
| **Dart 风格查找** | `source/Front/External/Native/DartStyle/DartStyleNativeFunction.cs` | 已实现 `DartStyleNativeLibrary` + `DartStyleNative.Lookup<TDelegate>` / `LookupCdecl<T1,T2,TRet>`(`delegate* unmanaged[Cdecl]<...>`) | `Lookup` 的参考实现 |
| 端到端示例 | `source/CLangdll/CLangdll.cpp` + `CLangdll.dll.slffi.json` | 导出一个 `(int,int)→int` 的 `simplelanguage_addtest` | 最小可用范例 |
| 外部函数自动注册 | `source/MathNativeImpl/MathExternalModule.cs` | 实现 `ISLExternalFunctionModule`,VM 加载 DLL 时 `Register`,`SystemCallExternalFunction("Math.sin",...)` 可调用 | 模块级批量注册范式 |
| SL 侧桥接声明 | `source/Front/Lib/Core/NativeBridge.sl` | `BridgeKind{SELF,CLR,JVM,NATIVE}` + `NativeBridge.Call` | 统一桥接入口号 |
| 值承载/编组样例 | `samples/SLang/source/VM/Object/SValue.sl` | `CreateCSharpObject()`(SValue→CLR)、`CreateSObjectByCSharpObject()`(CLR→SValue) | Marshal 方向参考 |

### 0.2 关键缺口(FFI 必须新建/扩展)

| 缺口 | 说明 | 涉及层 |
|---|---|---|
| C VM 原生桥为空桩 | `csimple_lang/src/vm/system_method_call/bridge_system_method.c` 中 `vm_sys_call_native_method` 仅 `warn-and-drop`,注释明确 "cvm has no foreign-runtime hosting yet" | C VM 运行时 |
| 标量类型过粗 | `SLNativeValueType` 只有 `I32/I64`,缺 `i8/u8/i16/u16`、`char`、句柄(`Handle`) | 编译器/IR/VM |
| 无复合类型 | Struct/Union/Opaque/Array 完全没有建模 | 编译器/IR/VM |
| 无 Pointer<T> 语义 | 仅一个笼统 `Ptr`,无泛型化指针、`.value`、`.elementAt`、`.address` | 类型系统/IR/VM |
| 无字符串编解码 | 缺 `String ↔ Pointer<Utf8>` 的 `toNative/fromNative` | 运行时库 |
| 无原生内存分配 | 缺 `malloc/free`(或 Arena) | 运行时库/VM |
| 无回调(原生→SL) | 缺 `NativeCallable`(反向调用) | 编译器/VM |
| 无 Finalizer | 缺 GC 关联的原生资源释放 | VM |
| 无语言级语法 | 现有都靠 C# 反射/委托手工接线,SL 源码里没有任何 `ffi` 语法 | 全编译层 |

> **设计原则**:上层(SL 语法、类型系统、IR)做"Dart 风格"的完整封装;底层**复用**已有的
> `NativeLibraryLoader` / `slffi.json` 清单 / `CallSystemMethod` 挂钩点,只在 C VM 侧把空桩补全,
> 并把 `SLNativeValueType` 扩展为精细宽度 + 复合类型。

---

## 1. Dart FFI 机制整理

### 1.1 核心概念

Dart FFI(Foreign Function Interface)让 Dart 代码**直接调用 C 库**,无需 `dart:async`、不依赖
平台通道。三步走:

```dart
import 'dart:ffi';
import 'dart:ffi' show Uint8, Uint16, Int32, Int64, Float, Double, Pointer, Utf8;

// 1) 打开库
final lib = DynamicLibrary.open('libm.so.6');

// 2) 用两个 typedef 描述同一函数:原生签名(值类型) + Dart 签名(对标类型)
typedef C_sin = Double Function(Double);
typedef Dart_sin = double Function(double);

// 3) 查找并调用
final sin = lib.lookupFunction<C_sin, Dart_sin>('sin');
print(sin(3.14));
```

要点:**原生签名用 FFI 原生类型(Int32/Double/Pointer…),Dart 签名用 Dart 类型(int/double/…)**,
两者由 VM 在调用边界自动编解码(marshal)。

### 1.2 原生类型系统(Native Types)

| Dart FFI 类型 | 含义 | C 对应 |
|---|---|---|
| `Int8..Int64` / `Uint8..Uint64` | 有符号/无符号定宽整数 | `int8_t..int64_t` / `uint8_t..` |
| `IntPtr` / `UintPtr` | 指针宽整数 | `intptr_t` / `uintptr_t` |
| `Float` / `Double` | 32/64 位浮点 | `float` / `double` |
| `Bool` | 8 位布尔 | `bool`(C) |
| `Pointer<T>` | 指向 T 的指针,T 可为原生类型/Struct/Opaque | `T*` |
| `Array<T,n>`(仅作 Struct 字段) | 内联定长数组 | `T[n]` |
| `Struct`(子类) | C 结构体(按值) | `struct` |
| `Union`(子类) | C 联合体 | `union` |
| `Opaque`(子类) | 不透明句柄(无字段) | `struct X* / void*` |
| `Handle` | Dart 对象句柄(传给 Native 回调) | Dart VM 内部 |
| `Void` | 无返回 | `void` |
| `Utf8` / `Utf16` | 编码标记,配合 `Pointer<Utf8>` 做字符串 | `char*` / `wchar_t*` |

### 1.3 库加载与符号查找

- `DynamicLibrary.open(path)` —— 打开具体文件。
- `DynamicLibrary.process()` —— 进程全局符号(如 libc)。
- `DynamicLibrary.executable()` —— 当前可执行文件自身符号。
- `lib.lookup<T>('name')`(新 API)或 `lookupFunction<C,D>('name')`(旧 API)——按名取函数指针,
  由泛型实例化出可调用的 Dart 闭包。

### 1.4 Struct / Opaque / Union

```dart
class Point extends Struct {
  @Int32() external int x;
  @Int32() external int y;
  @Double() external double z;
}
class NativeFile extends Opaque {}   // 只有类型,没有字段
```

Struct 字段按 FFI 布局规则(默认自然对齐,`@Packed(1)` 可改)生成偏移表,按值传给 C。

### 1.5 Pointer 操作

```dart
final p = calloc<Int32>();     // 分配
p.value = 42;                  // 解引用写
final v = p.value;             // 解引用读
final p2 = p.elementAt(3);    // 指针算术
final p3 = Pointer<Int32>.fromAddress(rawAddr);  // 由地址重建
print(p.address);              // 取地址
calloc.free(p);                // 释放
```

### 1.6 回调(Callbacks,原生 → Dart)

```dart
final cb = NativeCallable<Void Function(Int32)>.isolateLocal(myDartFunc);
// 把 cb.nativeFunction 作为 Pointer 传给 C,C 回调时回到 Dart isolate 执行
```

`NativeCallable.isolateLocal`(同 isolate,同步回调) / `.listener`(事件循环派发)。

### 1.7 内存管理与 Finalizer

- `calloc` / `malloc`(来自 `package:ffi`)在原生堆分配,需手动 `free`。
- `Arena`(`using`)作用域自动释放一批分配。
- `NativeFinalizer` / `Finalizer`:Dart 对象被 GC 时,自动调用一个原生清理函数释放关联的原生资源
  (如关闭 C 文件句柄)。

### 1.8 调用约定与已知限制

- 支持 `Cdecl`、`StdCall`(Windows)等 ABI。
- **不支持可变参数(varargs)**;Dart FFI 明确不支持 `printf` 这类。
- 结构体按值传递受平台 ABI 约束(大结构体走隐式指针)。

### 1.9 实现原理(Dart VM 视角)

VM 为 `lookupFunction` 生成 **trampoline**:一份把 Dart 参数按原生 ABI 打包到栈/寄存器、调用
真实 C 函数、再把返回值解包回 Dart 对象的胶水代码。Struct 字段读写是对 `Pointer` 做带偏移的
load/store。本语言设计完全采用"清单描述 + trampoline 降级"的思路。

---

## 2. 本语言 FFI 语法设计

> 语法为提案,关键字名最终以编译器接入为准。整体风格贴合 SL 现有 Dart/C# 风(`class`、`@` 注解、
> `var`、`void`、`Int32` 等)。

### 2.1 总体原则

1. 复用既有 `DynamicLibrary` / `Lookup` 语义,但**上升为语言级语法 + 编译器校验**,而不是靠 C# 手工接线。
2. 原生签名与 Dart 签名**合一**:SL 没有 Dart 那种 int/double 装箱差异,故一个 `native typedef`
   同时充当"原生契约",编译器据此生成 marshalling。
3. 类型映射表以现有 `SLNativeValueType` 为基,扩展为精细宽度与复合类型(见 §2.3)。
4. 调用边界强制走 VM 的 `CallSystemMethod`(C#) / `vm_sys_call_native_method`(C)挂钩,不另辟路径。

### 2.2 库加载

```sl
import "ffi";                       // 引入 FFI 原生类型与 DynamicLibrary

// 打开具体库(对应 Dart DynamicLibrary.open)
var lib = DynamicLibrary.Open( "clib.dll" );

// 进程全局符号 / 当前可执行文件(可选能力)
var proc = DynamicLibrary.Process();
var self = DynamicLibrary.Executable();
```

`DynamicLibrary` 是编译器内置类,`Open` 底层调 `NativeLibraryLoader.Load`。

### 2.3 原生类型别名与映射表(扩展 `SLNativeValueType`)

SL 内置一套 FFI 原生类型(编译器常量 + 枚举 `SLNativeValueType` 扩展后一一对应):

| SL FFI 类型 | 语义 | C 对应 | 映射枚举(扩展) |
|---|---|---|---|
| `Void` | 无返回 | `void` | `Void`(已有) |
| `Bool` | 8 位布尔 | `bool` | `Bool`(已有) |
| `Int8` / `UInt8` | 8 位 | `int8_t/uint8_t` | `I8` / `U8`(新增) |
| `Int16` / `UInt16` | 16 位 | `int16_t/uint16_t` | `I16` / `U16`(新增) |
| `Int32` / `UInt32` | 32 位 | `int32_t/uint32_t` | `I32` / `U32`(新增/已有) |
| `Int64` / `UInt64` | 64 位 | `int64_t/uint64_t` | `I64` / `U64`(新增/已有) |
| `IntPtr` / `UIntPtr` | 指针宽 | `intptr_t` | `Ptr`(已有,宽度=`VM_PTR_SIZE`) |
| `Float` / `Double` | 32/64 位浮点 | `float/double` | `F32` / `F64`(已有) |
| `Char` | 8 位字符 | `char` | `Char`(新增,映射 `U8`) |
| `Utf8` | 字符串编码标记 | `char*` | `Utf8String`(已有) |
| `Pointer<T>` | 指针泛型 | `T*` | `Ptr` + 子类型描述 |
| `Struct` / `Opaque` / `Union` | 复合类型 | `struct/union` | 新增 `Struct/Opaque/Union` 描述符 |

> 注:SL 端整数统一以 `Int32/Int64` 表示,所以 `I8/U8/I16/U16` 主要用于**与原生 ABI 对齐**,
> 在 marshalling 时做符号扩展/截断,SL 侧仍以 `Int32` 承载。

### 2.4 原生函数签名与查找

```sl
// 原生 typedef:参数/返回用 FFI 原生类型
native typedef CAdd = Int32 ( Int32 a, Int32 b );
native typedef CHello = Void ( Pointer<Utf8> msg );

// 查找:返回可直接调用的 SL 函数对象
var add = lib.Lookup< CAdd >( "add" );          // 对应 DartStyleNative.Lookup
var hello = lib.Lookup< CHello >( "hello" );

// 调用(与普通 SL 函数一致)
var r = add( 1, 2 );
hello( "hello from sl".ToUtf8() );
```

`native typedef` 在 MetaCore 层登记为 **FFI 函数签名元类型**,携带 `(retType, [paramType...])`;
`Lookup` 在 IR 层生成 `LoadLibrary + GetExport + MakeNativeClosure` 序列,运行时绑定到 `delegate*`
(参考 `DartStyleNative.LookupCdecl`)。

### 2.5 Struct / Opaque / Union

```sl
// 按值结构体:字段用 FFI 原生类型注解
@FFIStruct
struct Point
{
    @Int32 x;
    @Int32 y;
    @Double z;
}

@FFIPacked(1)
struct PackedRec
{
    @UInt8 a;
    @Int32 b;
}

// 不透明句柄:只有类型,没有字段(C 侧 struct 的内部结构对 SL 不可见)
@FFIOpaque
class NativeFile { }

var p = Point();          // 在原生堆/栈构造(Pointer<Point>)
p.x = 10; p.y = 20;
someNativeThatTakesPoint( p );   // 按值传递
```

`@FFIStruct` 触发编译器计算**字段偏移表**(默认自然对齐,`@FFIPacked(n)` 改对齐),并生成
`load/store` 的 marshalling 代码。

### 2.6 Pointer 操作

```sl
var p = malloc<Int32>( 1 );      // 分配 1 个 Int32
p.value = 42;                    // 解引用写
var v = p.value;                 // 解引用读
var p2 = p.elementAt( 3 );       // 指针算术
var p3 = Pointer<Int32>.FromAddress( rawAddr );  // 由地址重建
var addr = p.address;            // 取地址
free( p );                       // 释放
```

`Pointer<T>` 是泛型类,`.value`(load/store)、`.elementAt`、`FromAddress`、`.address` 由
IR 层的指针指令实现(P0:仅 `value`/`address`/`elementAt`)。
对应 C 端:`svalue.h` 已支持 `Ptr`,需新增 `svalue_ptr_load/store` 按子类型宽度读写。

### 2.7 字符串(Utf8)编解码

```sl
var s = "hello";
var cstr = s.ToUtf8();           // String → Pointer<Utf8>(内部 malloc + 拷贝 + '\0')
var back = cstr.FromUtf8();      // Pointer<Utf8> → String
// 注意:cstr 需手动 free,或用 Finalizer 托管
```

底层:写入 UTF-8 字节到原生内存(C 端 `char* string_value` 已有承载),读取时 `Marshal.PtrToStringUTF8`
风格重建 SL `String`。

### 2.8 原生内存分配

```sl
var buf = malloc<UInt8>( 256 );  // 原生堆分配,返回 Pointer<UInt8>
free( buf );                     // 显式释放
```

运行库提供 `malloc/free`(封装 `NativeMemory.Alloc/Free` 或 C 端 `vm_malloc`),**P1 引入
`Arena` 作用域自动释放**(对标 `package:ffi` 的 `using`)。

### 2.9 回调 NativeCallable(原生 → SL)

```sl
// 声明一个原生可调用的 SL 函数
native callback void OnEvent( Int32 code )
{
    Println( "event: @1", code );
}

// 生成可被 C 调用的函数指针
var cb = NativeCallable.IsolateLocal< void(Int32) >( OnEvent );
RegisterCHandler( cb.nativeFunction );   // 把 Pointer 传给 C
```

编译器为 `callback` 函数生成一段 **trampoline**:C 调用进入 → 把原生参数解包成 SValue → 进入
SL 调用帧执行用户函数 → 返回值打包回原生。`IsolateLocal`(同 isolate 同步)先实现,
`.Listener`(事件循环派发)后置。

### 2.10 Finalizer(释放托管原生资源)

```sl
var handle = OpenNativeFile( "x.bin" );   // 返回 Opaque 句柄
AttachFinalizer( handle, freeNativeFile, token );  // GC 时自动调 freeNativeFile(handle)
```

VM 在对象被 GC 时,调用注册的原生清理函数。底层用 `NativeFinalizer` 机制(C 端用 GC 钩子)。

---

## 3. 编译流水线各层设计

### 3.0 总览:降级策略

```
用户写的 FFI                     编译器降级为
──────────────────────────────────────────────────────────────────
native typedef CAdd = Int32(Int32,Int32)   →  FFI 签名元类型(ret=I32, params=[I32,I32])
lib.Lookup<CAdd>("add")                    →  LoadLibrary(缓存) + GetExport + MakeNativeClosure
add(1,2)                                  →  Marshal(实参→原生) + CallNative(fp) + Unmarshal(返回)
@FFIStruct struct Point{...}              →  偏移表 + MarshalIn/MarshalOut 生成器
Pointer<T> p; p.value = x                 →  PtrLoad/PtrStore 指令(按子类型宽度)
malloc<T>(n) / free(p)                    →  AllocNative / FreeNative 指令
callback void F(){...}                    →  trampoline 合成函数 + 注册
```

### 3.1 Lexer 层

`ReadIdentifier` 关键字表新增:
- `native`(修饰 typedef:FFI 签名声明)
- `struct`(FFI 结构体,**复用 C 结构体关键字或新增**,与 SL 既有 `data` 区分)
- `DynamicLibrary`(内置类名,普通标识符即可,无需新 keyword)
- `@` 注解类新增 `FFIStruct` / `FFIOpaque` / `FFIPacked`(已有注解解析机制)

### 3.2 StructParse / FileMeta 层

1. 识别 `native typedef <名> = <类型> ( <参数列表> )` → `FileMetaFFIFunctionSyntax`。
2. 识别 `@FFIStruct struct ...` → `FileMetaFFIStructSyntax`,字段带 `@类型` 注解解析为
   `List<(FFIType, fieldName, offset)>`。
3. `lib.Lookup<...>(...)` 作为表达式节点,记录泛型实参(即 FFI 签名元类型名)与符号名。

### 3.3 MetaCore 层

1. **新增 `MetaFFIFunctionSignature`**:持有 `retType` + `paramTypes[]`(均为 `MetaFFIType`),
   复用以 `SLNativeValueType` 为存储的 `FFITypeDescriptor`。
2. `native typedef` → 注册到 `TypeManager`,供 `Lookup` 泛型实参解析。
3. `@FFIStruct` → 计算**布局偏移表**(递归处理嵌套 Struct / 定长 Array;自然对齐 + `@FFIPacked`),
   生成 `MarshalIn`(SObject→原生字节)/`MarshalOut`(原生字节→SObject)描述。
4. 校验:签名里的 SL 类型必须可映射到 FFI 原生类型;String 必须显式经 `ToUtf8`;Opaque 不可有字段。
5. `Lookup` 表达式 → 产出 `MetaFFICallStatements`(携带库名 + 符号名 + 签名描述符)。

### 3.4 IR 层

**(a) 新增 / 复用操作码**(`EIROpCode`):

| 指令 | 栈行为 | 语义 |
|---|---|---|
| `CallNative` | `... a1..aN → ret` | 弹出 N 个 SValue 实参,按签名 marshalling 后调用原生 `fp`,返回值 unmarshal 压栈。**P0 主入口,挂在 `CallSystemMethod` 体系下** |
| `AllocNative` | `size → ptr` | 在原生堆分配 size 字节,返回 `Ptr`(对标 `malloc`) |
| `FreeNative` | `ptr →` | 释放原生内存(对标 `free`) |
| `PtrLoad` | `ptr → value` | 按子类型宽度从 `ptr` 读值(`.value`) |
| `PtrStore` | `ptr value →` | 按子类型宽度写值(`.value =`) |
| `PtrAddress` | `ptr → addr` | 取指针地址(`.address`) |
| `PtrElementAt` | `ptr idx → ptr'` | 指针算术(`.elementAt`) |
| `MakeNativeClosure` | `fp sig → closure` | 把原生函数指针 + 签名封成可 SL 调用的闭包(`Lookup` 内部用) |
| `RegisterFinalizer` | `obj fp token →` | 注册 GC 终结器 |

> C 端 `EIROpCode` 与 C# 完全一致(`csimple_lang/src/vm/vm.h`),新增需在两侧同步,
> 并扩展 `SLNativeValueType`(C 端用 `evm_type.h` 的 `_EVMType` 宽度表做 pack)。

**(b) `lib.Lookup<Sig>("sym")` 的 IR**:
```
; 取库句柄(编译器常量或 DynamicLibrary.Open 的结果 SValue)
LoadLibraryHandle  <lib>
LoadConstString    "sym"
MakeNativeClosure  <sig_descriptor>   ; 内部调 NativeLibrary.GetExport + 生成 delegate*
StoreLocal         add
```

**(c) `add(1,2)` 调用的 IR**(走 `CallNative`):
```
LoadLocal          add        ; 原生闭包(含 fp + sig)
LoadConstInt32     1
LoadConstInt32     2
CallNative         2          ; 内部:marshal SValue→native → invoke fp → unmarshal
StoreLocal         r
```

**(d) Struct 传参的 IR**:调用前插入 `MarshalIn`(构造原生 struct 临时块 → 填字段 → 传 `Ptr`/`by-value`),
返回后 `MarshalOut` 还原。

### 3.5 VM 运行时(C# 与 C)

**C# 参考 VM(`samples/SLang/source/VM/InnerCLRRuntime/RuntimeVM.sl`)**:
- 复用现有 `OpCode_CallCSharpMethod` 的模式,新增 `OpCode_CallNative`:
  从栈取参 → 按 `sig` 调 `DartStyleNative.LookupCdecl` 已绑定的 `delegate*` → 收集返回值 →
  `CreateSObjectByCSharpObject` 风格 unmarshal。
- `AllocNative/FreeNative` 走 `NativeMemory.Alloc/Free`。

**C VM(`csimple_lang`,移植目标,重点补齐)**:
- `vm_sys_call_native_method`(`bridge_system_method.c`)从 **空桩** 改为真实实现:
  读 `VM_PTR_SIZE` 宽的函数指针 `fp`,按 `SLNativeValueType` 扩展后的描述符把 `SValue[]`
  参数 pack 到调用栈/寄存器(参考 `svalue.h` 的 `svalue_to_vmvalue`),调用 `fp`,再把返回值
  `svalue_from_vmvalue` 压栈。
- 新增 `vm_alloc_native` / `vm_free_native` 对接 `vm_malloc`/`vm_free`。
- 指针指令在 `runtime_value/svalue.c` 增加按子类型宽度的 load/store。

---

## 4. 实现功能点清单(★ 重点:我们要实现哪些功能)

> 优先级:`P0`= 最小可用(MVP)、`P1`= 完整可用、`P2`= 进阶/可选。
> 状态:`[复用]`= 已有代码直接复用、`[扩展]`= 在现有基础上改、`[新建]`= 从零实现。

### 4.1 P0 —— 最小可用(MVP):能像 Dart 一样 `open → lookup → 调用标量函数`

| 编号 | 功能点 | 优先级 | 状态 | 主要改动 |
|---|---|---|---|---|
| **F1** | **DynamicLibrary 加载**(`Open`/`Process`/`Executable`) | P0 | [复用] | `NativeLibraryLoader.cs` 直接作为底层;SL 侧新增内置类 `DynamicLibrary` 封装 |
| **F2** | **符号查找 `Lookup<Sig>("name")`** | P0 | [复用] | 扩展 `DartStyleNativeFunction.cs` 的 `Lookup`/`LookupCdecl` 支持泛型签名解析 |
| **F3** | **标量类型映射**(`Int8..Int64/UInt8..UInt64/Float/Double/Bool/Void/IntPtr`) | P0 | [扩展] | `SLNativeValueType` 扩展 `I8/U8/I16/U16/U32/U64/Char/Handle`,并补 C 端 `evm_type.h` 宽度 |
| **F4** | **调用约定 Cdecl/StdCall** | P0 | [复用] | `SLNativeCallingConvention` 已支持,接入 `CallNative` 的 ABI 标记 |
| **F5** | **实参/返回值 Marshall 层(SValue↔native)** | P0 | [新建] | `SLNativeMarshalling` 扩展按精细宽度的 pack/unpack;VM 端 `CallNative` 分发 |
| **F6** | **VM 调用入口 `CallNative`** | P0 | [新建] | C# VM 加 `OpCode_CallNative`;**C VM 把 `vm_sys_call_native_method` 空桩补全为真实分发** |
| **F7** | **`*.slffi.json` / `sl_exports_json()` 清单自动注册** | P0 | [复用] | `NativeExportManifestReader.cs` + `MathExternalModule` 范式,模块加载即注册 |
| **F8** | **端到端示例**:把 `CLangdll` 改造为带 SL 语法调用 | P0 | [复用] | 新增 `.sl` 样例 + `CLangdll.dll.slffi.json` 已就绪 |

### 4.2 P1 —— 完整可用:指针、结构体、字符串、内存、不透明句柄、C VM 对齐

| 编号 | 功能点 | 优先级 | 状态 | 主要改动 |
|---|---|---|---|---|
| **F9** | **Pointer<T>**:`.value` / `.address` / `.elementAt` / `FromAddress` | P1 | [扩展] | 新增 `PtrLoad/PtrStore/PtrAddress/PtrElementAt` 指令;`svalue.h` 增加按子类型宽度的指针读写 |
| **F10** | **Struct 按值**(`@FFIStruct` + 字段注解 + 偏移表 + 进出 marshalling) | P1 | [新建] | `MetaFFIStruct` 布局计算 + `MarshalIn/Out`;IR 插入 struct 构造/拆解序列 |
| **F11** | **Utf8 字符串编解码**(`ToUtf8` / `FromUtf8`) | P1 | [新建] | 运行库函数:UTF-8 字节写入原生内存 / 反向重建 SL `String` |
| **F12** | **原生内存分配**(`malloc<T>(n)` / `free(p)`) | P1 | [新建] | `AllocNative`/`FreeNative` 指令 + 运行库封装(`NativeMemory` / C 端 `vm_malloc`) |
| **F13** | **Opaque 不透明句柄**(`@FFIOpaque class`) | P1 | [新建] | 仅类型登记,无字段;作为 `Ptr` 的子类型在 SL 侧表征原生对象句柄 |
| **F14** | **C VM 与 C# VM FFI 行为对齐** | P1 | [扩展] | 统一 `SLNativeValueType` 两侧描述符;补齐 C 端所有 FFI 指令的解释 |
| **F15** | **`@FFIPacked(n)` 自定义对齐** | P1 | [新建] | 布局算法支持 packed 偏移计算 |

### 4.3 P2 —— 进阶/可选

| 编号 | 功能点 | 优先级 | 状态 | 主要改动 |
|---|---|---|---|---|
| **F16** | **回调 NativeCallable(原生→SL)**(`IsolateLocal` / `.Listener`) | P2 | [新建] | 合成 trampoline + 注册;参数解包回 SL 调用帧 |
| **F17** | **Union 类型**(`@FFIUnion`) | P2 | [新建] | 共享偏移布局 |
| **F18** | **Struct 内联定长 Array**(`Array<T,n>`) | P2 | [新建] | 嵌套布局 |
| **F19** | **Finalizer**(`AttachFinalizer` / GC 关联释放) | P2 | [新建] | VM GC 钩子调原生清理函数 |
| **F20** | **Arena 作用域自动释放**(`using`/`Arena`) | P2 | [新建] | 对标 `package:ffi` Arena |
| **F21** | **`DynamicLibrary` 跨实现统一**(Go 端 gosimple_lang 复用 cgo 思路) | P2 | [新建] | 参考 `gosimple_lang/cwrapper.go` 的 `C.int`/`C.CString` 编组 |

### 4.4 明确不做(❌ 限制,对标 Dart FFI)

| 项 | 说明 |
|---|---|
| **可变参数(varargs)** | 与 Dart FFI 一致,**不支持** `printf` 类变参函数 |
| **Struct 内嵌 Union 的位域(bitfield)** | 首版不建模 C 位域 |
| **C++ 名字修饰 / 类成员函数** | 仅支持 `extern "C"` 扁平 ABI |
| **SL FFI 调用 CLR/JVM** | `NativeBridge` 的 CLR/JVM 路径维持既有,本设计只覆盖 `NATIVE` 路径 |
| **跨语言异常传播** | 原生侧异常不跨边界,统一转 SL 错误码/异常 |

---

## 5. 验证方式

### 5.1 P0 验证(标量)
1. 用现有 `CLangdll`(已导出 `(int,int)→int`),编写 `.sl` 用例:
   ```sl
   import "ffi";
   var lib = DynamicLibrary.Open( "CLangdll.dll" );
   native typedef CAdd = Int32 ( Int32, Int32 );
   var add = lib.Lookup< CAdd >( "add" );
   var r = add( 3, 4 );     // 期望 7
   ```
2. 编译全流程无报错;`IR.txt` 出现 `MakeNativeClosure` + `CallNative` 序列。
3. C# VM 与 **C VM(`csimple_lang`)** 均能跑出 `r==7`(验证 C 端空桩已补全)。

### 5.2 P1 验证(指针/结构/字符串)
1. 原生库导出 `struct point {int x;int y;}` 相关函数 + `char* echo(char*)`。
2. SL 侧 `@FFIStruct struct Point{ @Int32 x; @Int32 y; }`,调用按值传参并读回。
3. `malloc<Int32>(1)` + `p.value=42` + `p.value==42` + `free`。
4. `"hi".ToUtf8()` 传给 `echo` 并 `FromUtf8()` 还原。

### 5.3 P2 验证(回调/Finalizer)
1. C 库接收一个函数指针并回调;`NativeCallable.IsolateLocal` 注册的 SL 函数被正确调用。
2. 创建带 `AttachFinalizer` 的 Opaque 句柄,强制 GC 后观察原生清理函数被调用。

---

## 6. 文件改动清单(草案)

| 层 | 文件 | 改动 |
|---|---|---|
| 类型模型 | `Front/External/Native/NativeExportModels.cs` | `SLNativeValueType` 增 `I8/U8/I16/U16/U32/U64/Char/Handle` + `Struct/Opaque/Union` 描述符 |
| 加载器 | `Front/External/Native/NativeLibraryLoader.cs` | [复用],按需加 `Open/Process/Executable` 语义 |
| 查找 | `Front/External/Native/DartStyle/DartStyleNativeFunction.cs` | `Lookup` 支持泛型 FFI 签名解析 |
| 内置类 | `Front/Lib/Core/` 新增 `FFI.sl`(或并入 `NativeBridge.sl`) | `DynamicLibrary`、`malloc/free`、`ToUtf8/FromUtf8`、`NativeCallable`、`AttachFinalizer` |
| Lexer | `Front/Compile/Parse/LexerParseToToken.cs` | 加 `native`/`struct` 关键字;`@FFIStruct/@FFIOpaque/@FFIPacked` 注解 |
| StructParse | `Front/Compile/Parse/StructParseToSyntax.cs` | `native typedef`、FFI struct、Lookup 表达式路由 |
| FileMeta | `Front/Compile/FileMeta/` | 新增 `FileMetaFFIFunctionSyntax` / `FileMetaFFIStructSyntax` |
| MetaCore | `Front/Core/` 新增 `MetaFFISignature.cs` / `MetaFFIStruct.cs` | 签名元类型、布局偏移表、Marshal 描述 |
| IR 枚举 | `Front/IROpEnum.cs` | 加 `CallNative`/`AllocNative`/`FreeNative`/`PtrLoad`/`PtrStore`/`PtrAddress`/`PtrElementAt`/`MakeNativeClosure`/`RegisterFinalizer` |
| IR 生成 | `Front/IR/` 新增 `IRFFICallStatements.cs` 等 | Lookup / CallNative / 指针 / 内存 指令序列 |
| C# VM | `samples/SLang/source/VM/InnerCLRRuntime/RuntimeVM.sl` | `OpCode_CallNative` + 指针/内存指令解释 |
| C VM 运行时 | `csimple_lang/src/vm/system_method_call/bridge_system_method.c` | **`vm_sys_call_native_method` 真实实现** |
| C VM 指令 | `csimple_lang/src/vm/runtime/vm_runtime.c` + `runtime_value/svalue.c` | 新增 FFI 指令解释 + 指针按宽度读写 |
| C 端类型 | `csimple_lang/src/vm/runtime/evm_type.h` | 扩展标量宽度表以匹配 `SLNativeValueType` |
| 示例 | `source/CLangdll/` + `samples/` | 新增 `.sl` 调用样例与 P1/P2 原生库 |

---

## 7. 端到端示例(目标形态)

**C 侧(`clib.c`)**:
```c
#include <stdint.h>
extern "C" int32_t sl_add(int32_t a, int32_t b) { return a + b; }
extern "C" typedef struct { int32_t x; int32_t y; } Point;
extern "C" int32_t point_dist2(Point p) { return p.x*p.x + p.y*p.y; }
```
**SL 侧**:
```sl
import "ffi";
var lib = DynamicLibrary.Open( "clib.dll" );

native typedef CAdd   = Int32 ( Int32, Int32 );
native typedef CDist  = Int32 ( Point );

var add  = lib.Lookup< CAdd >( "sl_add" );
Println( "@1", add( 3, 4 ) );          // 7

@FFIStruct
struct Point { @Int32 x; @Int32 y; }

var dist = lib.Lookup< CDist >( "point_dist2" );
var p = Point(); p.x = 3; p.y = 4;
Println( "@1", dist( p ) );            // 25
```

---

## 8. 与现有模块的关系图(一图流)

```
   SL 源码
     │  import "ffi"; native typedef; @FFIStruct; lib.Lookup<Sig>("sym")
     ▼
 ┌──────────── 编译层(Front) ────────────┐
 │ Lexer → StructParse → FileMeta →        │
 │ MetaCore(FFI签名/布局) → IR(CallNative…) │
 └────────────────┬───────────────────────┘
                  │ IR 字节码 + FFI 签名描述符
                  ▼
 ┌──────────── 运行时 ────────────────────┐
 │ C# VM: OpCode_CallNative ─┐             │
 │ C  VM : vm_sys_call_       │ 复用       │
 │         native_method ─────┘ NativeLibraryLoader
 │                             + slffi.json 清单
 │  Marshal(SValue↔native) ← SLNativeValueType(扩展)
 └────────────────┬───────────────────────┘
                  ▼
       原生 C 动态库(.dll/.so)
```
