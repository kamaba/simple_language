# MLIR llvm.struct 与 CVM 对象互转及继承机制设计方案

> 承接 `MLIR_AOT_DESIGN.md`。本文档定义 class/data/enum 三种类型如何映射为
> `!llvm.struct`、CVM 对象与原生布局缓冲之间的双向转换（marshal/unmarshal）、
> 以及基类/子类之间的前缀布局与类关系判定机制。
>
> 状态：**待评审**（文末"开放问题"需要确认后才进入实现）。

---

## 1. 目标与范围

### 1.1 目标

1. class / data / enum 映射为 `!llvm.struct`，结构体首部携带元信息
   （class_id、名称、模板实参指针及其描述字段）。
2. 提供 **CVM 对象（VMObject/member_data）↔ 原生布局缓冲（llvm.struct）**
   的双向转换方法——LLVM 侧与 CVM 侧各有自己的数据结构，互转是本方案的核心。
3. 定义继承布局规则：子类结构体的前缀与父类结构体完全相同（父类字段在前），
   并在 LLVM 侧建立可确定类关系（is-a）的机制。
4. 定义互转结果的**过界调用约定**：数据如何传入 AOT 函数（正向），
   以及 AOT 回调 CVM 时 llvm.struct 地址如何转回 VMObject（反向），
   双向均以"执行快"为硬约束（§5.6–§5.8）。

### 1.2 非目标

- 不改变 VMObject 的内存布局与 GC/refcount 语义。
- 不改变 AOT 调用 ABI（`SLAotValue { i32 kind; i64 data; }` 保持不变）。
- 不在第一阶段实现虚方法分派（本文只处理数据布局与类型判定）。

---

## 2. 现状事实（代码级证据）

### 2.1 VMObject 与 member_data 布局

`csimple_lang/src/vm/vm_object.h`：

```c
typedef struct _VMObject {
    VMObjectHeader  header;                  /* 8B 压缩头：etype/meta_kind/refcount/hash/gc */
    uint8*          member_data;             /* 扁平成员数据（真实数据）                  */
    uint32          member_data_size;        /* 总字节数                                   */
    VMRuntimeObject* member_runtime_objects; /* 每成员的定义（类型/名字/数据切片）        */
    int32           member_runtime_object_count;
    RuntimeType*    runtime_type_ref;        /* 对象自身的 RuntimeType                    */
} VMObject;
```

要点：

- **`member_data` 是成员的真实数据区**；**每个成员的定义**（类型、名字、
  指向 member_data 的切片 `member_data_start/length`）放在
  `member_runtime_objects` 数组里。llvm.struct 只对接 `member_data`，
  `member_runtime_objects` 完全留在 VM 侧，AOT 不感知。
- **member_data 是顺序紧凑（packed）布局，无对齐填充**：
  `vm_object_init_member_layout`（vm_object.c:197）中
  `cursor += vm_object_member_byte_length(et)` 逐成员累加，成员 i 的偏移
  = 前面所有成员槽宽之和。
- **槽宽规则**（vm_object.c:112）：

| EVMType | 槽宽 |
|---|---|
| Boolean/UInt8/Int8/Float8_* | 1 |
| Int16/UInt16/Float16* | 2 |
| Int32/UInt32/Float32（及 enum 值） | 4 |
| Int64/UInt64/Float64/Num | 8 |
| Object/String/Array/Class | 8（指针槽） |

- 类实例创建（`vm_object_new_by_runtime_class`，vm_object.c:478）：
  每个成员的 `field_types[i]` 来自成员 RuntimeType 的 EVMType；
  模板参数 T 已在实例的模板绑定下解析为具体 EVMType。
- **嵌套 data 成员在 member_data 中是一个 8 字节指针槽**（EVMType_Object，
  指向子 VMObject）——这与原生侧"递归内联展开"是两侧布局的最大差异点。

### 2.2 AOT ABI 现状

- MLIRExporter.cs:94：`!slv = !llvm.struct<(i32, i64)>`（VM 值 ABI）。
- csimple_lang `SLAotValue`（vm_aot_registry.h:19）与之镜像，16 字节。
- 数组已按"对象指针放 i64"过界（MLIRExporter 的 Array 槽类型模型），
  对象沿用同一模式，**ABI 零改动**。
- 当前 `SlotTable.ResolveSLType`（MLIRExporter.cs:463）遇到 class/data/enum
  直接抛异常回退解释器——本方案第一期的落点。

### 2.3 现有转换参考实现

`memory_system_method.c` 已有一套语义等价的互转（`SystemMemoryDataToStruct`
/ `SystemMemoryNativeStructToData`），槽分类 `vm_mem_data_member_slot`
（1552 行起）与自然对齐布局 `vm_mem_native_struct_layout`（804 行起）。
本方案的 C 侧转换函数**复用同一套分类与偏移规则**，只是把
"SL 代码手动调用" 变成 "AOT 调用边界自动进行"。

---

## 3. 总体架构：三层视图

```
┌─────────────────────────── CVM（解释执行） ───────────────────────────┐
│  VMObject                                                              │
│  ┌──────────┬──────────────────────┬────────────────────────────┐    │
│  │ header 8B│ member_data (packed) │ member_runtime_objects[]   │    │
│  │ etype... │ [slot0][slot1][...]  │ 每成员定义+数据切片         │    │
│  └──────────┴──────────┬───────────┴──────────────（VM 私有）────┘    │
└────────────────────────┼───────────────────────────────────────────────┘
                         │  marshal ──────────────►  unmarshal（写回）
┌────────────────────────┼───────────────────────────────────────────────┐
│  AOT 边界（vm_aot_registry.c 调用前后）                                 │
│        native_buf = [ !sl_meta 32B | 成员区（自然对齐重排） ]            │
└────────────────────────┼───────────────────────────────────────────────┘
                         │  i64 载 native_buf 指针（同 Array 模式）
┌────────────────────────┼───────────────────────────────────────────────┐
│  AOT（aot.dll，MLIR 发射）                                              │
│  !sl_t_Point = !llvm.struct<(!sl_meta, i32, i32, ...)>                 │
│  成员访问 = llvm.getelementptr 常量下标 + load/store                    │
└─────────────────────────────────────────────────────────────────────────┘
```

职责划分：

| 层 | 职责 |
|---|---|
| VM 侧 | 持有对象本体、成员定义（VMRuntimeObject）、GC/refcount |
| 边界（C） | 槽分类、偏移计算、marshal/unmarshal、缓冲分配与回收 |
| AOT 侧 | 只认 `!sl_t_<T>` 原生布局；元信息从 `!sl_meta` 头读取 |

---

## 4. llvm.struct 布局规范

### 4.1 元数据头 `!sl_meta`（32 字节，对齐 8）

每个 class/data 实例的原生缓冲以统一元数据头开始（对应你说的
`llvm.struct<class_id, member1, member2>` 里打头的 class_id，扩展为完整头）：

```mlir
!sl_meta = !llvm.struct<(
  i32,   /* [0] class_id      FNV-1a(全名) = IRMetaClass.id，0=无          */
  i32,   /* [1] kind_flags    低4位 = metaClassKind                        */
         /*                   (0=class 1=enum 2=data 3=interface)           */
         /*                   bit31 = is_template                          */
  i64,   /* [2] name_ptr      -> @sl_name_<t> 常量字符串，0=无              */
  i64,   /* [3] template_ptr  -> 模板实参表 @sl_tmpl_<t>，非模板=0          */
  i32,   /* [4] tmpl_cnt      描述 template_ptr 的实参条数（非模板=0）       */
  i32    /* [5] base_class_id 直接基类 id，0=根类（继承判定用，见 §6）      */
)>
```

字段来源（全部可从 `IRMetaClass` / `SLClassPackage` 直接取得，无需新计算）：

| 字段 | 来源 |
|---|---|
| class_id | `IRMetaClass.id`（MetaClassIdentity FNV-1a，跨会话稳定） |
| kind_flags | `IRMetaClass.metaClassKind`（IRMetaClassKind: Class=0 Enum=1 Data=2 Interface=3） |
| name_ptr | 新发模块级常量 `@sl_name_<t>("全名\00")` |
| template_ptr / tmpl_cnt | 模板实例：`templateTypeList` / `templateParameterCount` |
| base_class_id | `SLClassPackage.baseClassId`（= `extendClass?.classId ?? 0`） |

### 4.2 类型别名

```mlir
/* class Foo { Int32 a; Double b; Bar c; String s; } */
!sl_t_Foo = !llvm.struct<(!sl_meta, i32, f64, !llvm.ptr, !llvm.ptr)>

/* data Point { Int32 x; Int32 y; data Inner in; }   嵌套 data 递归内联 */
!sl_t_Inner = !llvm.struct<(!sl_meta, i32, i32)>
!sl_t_Point = !llvm.struct<(!sl_meta, i32, i32, !sl_t_Inner)>

/* enum Color { Red, Green }   轻量盒（8B），见 §8 */
!sl_t_Color = !llvm.struct<(i32 /*class_id*/, i32 /*value*/)>

/* 模板实例 List<Int32>：布局按实参完全展开，模板信息只进 header */
!sl_t_List_I32 = !llvm.struct<(!sl_meta, i32, !llvm.ptr)>
```

命名规则：`!sl_t_` + 全名 mangle（`.`/`<`/`>`/`,` → `_`，与现有符号
sanitize 规则一致）；同名冲突时追加 class_id 十六进制后缀。

### 4.3 成员映射表（VM 槽 ↔ llvm 字段）

| SL 成员类别 | VM 侧 member_data 槽 | llvm 字段 | 槽宽/对齐 | 互转动作 |
|---|---|---|---|---|
| Int64/UInt64/Double/Num | 8B | i64 / f64 | 8/8 | 定长拷贝 |
| Int32/UInt32/Float32 | 4B | i32 / f32 | 4/4 | 定长拷贝 |
| Int16/UInt16/Float16 | 2B | i16 | 2/2 | 定长拷贝 |
| Int8/UInt8/Boolean/Char | 1B | i8 | 1/1 | 定长拷贝 |
| enum 成员 | 4B（Int32 槽） | i32 | 4/4 | 定长拷贝（值 = 底层常量） |
| string | 8B（char* 直存） | !llvm.ptr | 8/8 | 指针直传 |
| class/interface 引用 | 8B（VMObject*） | !llvm.ptr | 8/8 | 指针直传（opaque） |
| Array\<T\> 引用 | 8B（VMArray*） | !llvm.ptr | 8/8 | 指针直传（同现有数组模型） |
| 嵌套 data | 8B（VMObject* 指针槽） | 递归内联 !llvm.struct | 递归 | **解引用 + 递归 marshal**（两侧布局差异点） |

### 4.4 布局口径：方案 A（自然对齐，推荐）与方案 B（packed 直映）

**方案 A（本方案主案，前期已确认口径）**：llvm.struct 用 C 自然对齐布局
（非 packed），偏移规则与 `vm_mem_native_struct_layout` 逐字节一致
（`offset = round_up(cur, align)`，尾部按最大对齐补齐）。

- 优点：与 FFI / `SystemMemory*Native*` 一套规则；无非对齐访问风险；
  `getelementptr` 直接用 LLVM 算出的常量偏移。
- 代价：member_data（packed）≠ 原生布局（自然对齐），
  **互转必须逐成员重排，不能整块 memcpy**（§5 的核心工作）。

**方案 B（备选，性能优先时切换）**：llvm.struct 用
`!llvm.struct<packed (...)>` 逐字段镜像 member_data 的 packed 顺序。

- 优点：互转退化为 `memcpy(member_data, buf + 32, member_data_size)`
  + 填 meta 头，O(1)。
- 风险：packed struct 对齐=1，i64 成员可能落在奇数偏移上——x64 可容忍
  （慢），ARM 严格模式可能 fault；且与现有 native interop 布局不一致，
  同一类型出现两套偏移口径。

> member_data 本身已是 packed 顺序（§2.1），所以 B 技术上完全可行。
> 若确认走 B，§5 的逐槽重排简化为 memcpy，其余章节不变。

---

## 5. 互转机制（核心）

### 5.1 触发点与调用时序

转换发生在 `vm_aot_invoke`（vm_aot_registry.c）调用 aot.dll 前后，
由 manifest 的参数槽类别驱动：

```
CVM 解释器
  │  参数栈：VMObject*（data/class 型实参）
  ▼
vm_aot_invoke
  ├─ 1. 对 manifest 标记 slot="struct" 的参数：
  │      native_buf = 边界缓冲池分配(sizeof(!sl_t_T))
  │      sl_aot_marshal_object_to_native(vm, obj, native_buf, size)
  │        · 填 !sl_meta（class_id/kind/name_ptr/template/base）
  │        · 逐成员按 §4.3 重排拷入（嵌套 data 递归）
  │      SLAotValue.data = (int64)native_buf   /* 指针过界，同 Array */
  ├─ 2. slot="obj" 的参数：SLAotValue.data = (int64)VMObject*（不转换）
  ├─ 3. 调用 aot.dll 导出函数（AOT 体内 getelementptr 访问成员）
  ├─ 4. 返回后对 struct 型实参：
  │      sl_aot_unmarshal_native_to_object(vm, native_buf, size, obj)
  │        · 逐成员写回 member_data（嵌套 data 递归写回）
  │      释放边界缓冲
  └─ 5. 返回值若为 struct：反向 marshal 成新 VMObject（或经 NewObject 建对象）
```

### 5.2 C 侧 API（csimple_lang 新增）

```c
/* VMObject(data/class 实例) -> 原生布局缓冲（含 !sl_meta 头）。
 * buf 由调用方分配，容量 = 32 + 布局尺寸；失败返回 FALSE。 */
int32 sl_aot_marshal_object_to_native(VM* vm, VMObject* obj,
                                      uint8_t* buf, int32 buf_size);

/* 原生布局缓冲 -> 写回 VMObject（成员逐槽回写，嵌套 data 递归；
 * meta 头不写回——VMObject 的身份由 runtime_type_ref/header 承载）。 */
int32 sl_aot_unmarshal_native_to_object(VM* vm, const uint8_t* buf,
                                        int32 buf_size, VMObject* obj);

/* 类型布局描述：发射期算好，随 AOT manifest 下发（见 5.4），
 * C 侧不需要在运行时重算自然对齐偏移。 */
```

实现要点：

- 槽分类直接复用 `vm_mem_data_member_slot`（VMD_SLOT_*）。
- 偏移不用运行时算：**MLIR 发射器在导出时把每个类型的成员布局表
  （成员序号 → 原生偏移/宽度/类别）写进 manifest**，C 侧照表搬运。
  这与 `vm_mem_native_struct_layout` 的规则一致，但把计算移到了编译期。
- 嵌套 data 递归深度上限沿用 `VMD_MAX_DEPTH = 8`。
- string/class/array 指针槽：**借用语义**——直传指针，不 retain、不深拷；
  AOT 不得把指针生命期延长到调用返回之后。

### 5.3 生命周期与 GC 约束

| 对象 | 生命期 | 说明 |
|---|---|---|
| native_buf | 单次 AOT 调用期间 | 边界缓冲池（或 alloca），不进 GC，调用后回收 |
| class/array 引用槽 | 借用 | 指针直传，refcount 不变；AOT 内不解引用其内部布局 |
| 嵌套 data 内联区 | 值拷贝 | marshal 时展开，unmarshal 时写回；两侧独立 |

### 5.4 manifest 扩展（SLIR / AOT 包）

`SLAotMethodPackage`（MLIRExportManager.cs:296 落点）增加参数槽类别与
类型布局表；`SLAotPackage` 增加模块级类型表：

```json
"aot": {
  "methods": [{
    "id": "...", "symbol": "...", "status": "ok",
    "params": [
      {"slot": "i64"},
      {"slot": "struct", "typeId": 12345, "typeName": "Module.Ns.Point",
       "size": 40, "byRef": true}
    ]
  }],
  "types": [{
    "classId": 12345, "fullName": "Module.Ns.Point",
    "metaClassKind": 2, "baseClassId": 0,
    "templateParameterCount": 0, "templateArgCount": 0,
    "layout": [
      {"index": 0, "offset": 32, "size": 4, "slot": 1, "name": "x"},
      {"index": 1, "offset": 36, "size": 4, "slot": 1, "name": "y"},
      {"index": 2, "offset": 40, "size": 16, "slot": 3,
       "name": "in", "nestedTypeId": 6789}
    ],
    "nativeSize": 56
  }]
}
```

`slot` 复用 VMD_SLOT_* 编码（1=scalar 2=string 3=data 4=enum 5=ptr），
C 侧与现有代码共用同一套常量。

### 5.5 MLIR 侧成员访问示例

AOT 体内访问 struct 型参数 `p.x`：

```mlir
// 入口 ABI 不变：args 是 SLAotValue 数组
%p_raw = llvm.load %args_gep : !llvm.ptr -> i64        // p 的 native_buf 指针
%p     = llvm.bitcast %p_raw : i64 -> !llvm.ptr         // 视为 i64 位模式转换
// 实际发射：ptrtoint/inttoptr 组合，或直接以 !llvm.ptr 存于 kind 通道
%x_ptr = llvm.getelementptr %p[0, 1] : (!llvm.ptr) -> !llvm.ptr
%x_i64 = llvm.load %x_ptr : !llvm.ptr -> i32
```

> 细节：i64 与 ptr 之间的搬运用 `llvm.inttoptr` / `llvm.ptrtoint`
> （与现有 unrealized_cast 汇合链兼容）。

### 5.6 正向调用约定：互转结果传入 AOT 函数

互转完成后，数据经现有 `SLAotValue` 通道过界——**结构体布局不变
（仍为 16B 的 `i32 kind + i64 data`，§1.2 的"不改 ABI"承诺不破坏），
仅扩展 kind 取值域**：

| kind | 语义 | data 内容 | 状态 |
|---|---|---|---|
| 0 | i64 位模式（整数/布尔；Array 沿用此值传 VMArray\*） | 位模式 | 既有，不动 |
| 1 | f64 位模式 | 位模式 | 既有，不动 |
| **2** | **struct 原生缓冲** | **native_buf 指针（AOT 侧可见 `!sl_t_<T>` 布局）** | 新增 |
| **3** | **VMObject 引用（opaque）** | **VMObject\*（AOT 侧不解引用内部布局，仅判定/透传用）** | 新增 |

**AOT 函数 prologue 发射**（MLIRExporter，方法
`M.f(p: Point, k: Int64)` 为例）：

```mlir
llvm.func @sl_m_M_f(%ctx: !llvm.ptr, %args: !llvm.ptr,
                    %argc: i32, %ret: !llvm.ptr) -> i64 {
  /* 参数 0（Point, slot=struct）：args[0] 即 !slv = (i32 kind, i64 data) */
  %slot0 = llvm.load %args : !llvm.ptr -> !slv
  %kind0 = llvm.extractvalue %slot0[0] : !slv -> i32
  %raw0  = llvm.extractvalue %slot0[1] : !slv -> i64
  /* release 直转；debug 构建可加 kind0==2 断言 */
  %p     = llvm.inttoptr %raw0 : i64 -> !llvm.ptr<!sl_t_Point>
  /* 参数 1（Int64, slot=i64）：无转换直接取 data                */
  ...
  /* 函数体内 %p 走 §5.5 的 getelementptr 访问成员               */
}
```

开销：每参数 2 条 load/extract + 1 条 inttoptr，**纯寄存器操作，
纳秒级**；kind 与参数类别的匹配由 manifest（发射器与 C 侧共享同一张
槽类别表）静态保证，不依赖运行时协商。

**struct 返回值：ret 缓冲预置协议（避免 alloca 生命期陷阱）**

AOT 侧用 `alloca` 装返回值、返回后由 C 侧读取是 UB（callee 栈帧已
失效）。采用对称的预置协议：

- **C 侧（vm_aot_try_invoke）**：方法声明返回类型为 struct 时，调用前
  在**自身栈帧**放好返回缓冲，预填 `ret->kind = 2`、`ret->data = 栈缓冲地址`。
- **AOT 侧 epilogue**：发射器看到返回类型为 struct，直接把返回值
  store 到 `inttoptr(ret->data)` 指向的缓冲（**构造即写入调用方缓冲，
  零中转拷贝**）；kind=2 已由调用方预填，不覆盖。
- 回调桥方向对称（见 §5.7）：AOT 调用点预填 `ret.kind=2 + ret.data=alloca`
  （此处 alloca 在 AOT 自己的帧里，桥写入后 AOT 继续执行，生命期合法），
  桥在解释方法返回后 marshal 到该地址。

```
vm_aot_try_invoke 返回 struct 的收尾：
  fn() 返回 → ret.data 已指向 C 栈缓冲（AOT 直接构造进去了）
           → unmarshal(buf → 新 VMObject) → 压栈
           → 无需分配/释放（栈缓冲随帧消亡）
```

**正向调用完整时序**（§5.1 的展开，含登记动作）：

```
1. slot=struct 参数：buf = 缓冲池分配；marshal(obj→buf)；
   args[i] = {kind=2, data=buf}；登记身份缓存 (buf → obj)   /* §5.8 */
2. slot=obj   参数：args[i] = {kind=3, data=(int64)VMObject*}   /* 零转换 */
3. fn(vm, args, argc, &ret)
4. 逐个处理 struct 参数：unmarshal(buf→obj)（AOT 体内可能改写过 buf）
   + 撤销本帧登记的身份缓存条目 + buf 归还缓冲池
5. ret.kind==2：unmarshal(ret 缓冲 → 新 VMObject) 压栈
```

### 5.7 回调桥：llvm.struct 地址 → VMObject（反向转换）

AOT 体内 `CallStatic` 命中解释方法时，走现有 stage-5 桥
`vm_aot_invoke_vm_bridge`（`sl_aot_bridge_init` 注入的函数指针，
vm_aot_registry.c:529）。桥当前只认 kind 0/1；struct 参数（kind=2）
与对象引用（kind=3）按下述路径扩展：

```
AOT（持有 native_buf 指针 / VMObject*）
  │  args[i] = {kind=2, data=buf} 或 {kind=3, data=VMObject*}
  ▼
vm_aot_invoke_vm_bridge（C 侧，按 kind 分发）
  ├─ kind=3 → 直接取 data 压栈 VMObject*，零转换
  ├─ kind=2 → 查身份缓存（native_buf → 源 VMObject，指针开放寻址哈希）
  │    ├─ 命中：unmarshal(buf→obj)  ★进桥写回：AOT 对 buf 的修改同步给解释侧
  │    │        压栈 obj → 执行解释方法
  │    │        marshal(obj→buf)    ★出桥同步：解释侧修改写回 buf
  │    └─ 未命中（AOT 内新建的 struct）：unmarshal 建新对象
  │             NewObject（按 class_id 查 RuntimeClass）+ 灌 member_data
  │             压栈执行 → 出桥 marshal 回 buf（值语义）
  └─ kind=0/1 → 现有标量路径，不动
```

**身份缓存（identity cache）**——回调桥"必须快"的核心：

| 属性 | 设计 |
|---|---|
| 结构 | 开放寻址指针哈希表（模式同 `vm_aot_find_slot`），负载 < 0.5，单探测命中 |
| key / value | native_buf 地址 / 源 VMObject\*（**借用引用**，不加 refcount——对象生命期由发起调用的求值栈帧保证） |
| 登记时机 | `vm_aot_try_invoke` 第 1 步 marshal 时（正向调用传入的 buf 才有身份） |
| 撤销时机 | **每个 try_invoke 帧只撤销自己登记的条目**（帧内记录登记过的 buf 列表，fn 返回后逐个 remove）。buf 归还缓冲池后地址会被复用，残留映射会张冠李戴，必须清除 |
| 嵌套 | 解释代码再调 AOT 方法 → 新的 try_invoke 帧登记新条目、返回时各自撤销，互不干扰 |

**双向同步语义**（命中路径）——两侧都可能修改数据，进桥/出桥各同步一次：

- **进桥写回**：buf → member_data。AOT 在回调前可能已改写 buf，
  解释方法必须看到最新值。
- **出桥同步**：member_data → buf。解释方法可能改写对象字段，
  AOT 在回调返回后继续读 buf 时必须看到最新值。

未命中路径（AOT 内新建 struct，如 AOT 方法内构造 data 后传给解释方法）
是**完整 unmarshal**：按 manifest 类型表（§5.4）的 class_id 找到
RuntimeClass → NewObject → 逐槽灌 member_data。新建 VMObject 的生命期
交给现有 refcount 语义：若解释代码把它存进某字段（引用逃逸）则被持有，
否则调用完即回收。

### 5.8 性能设计（边界转换执行必须快）

**预算**：全标量 struct（≤8 成员）单次 marshal ≤ 50ns 量级；
回调桥身份命中路径**不发生任何对象壳构建**。

**(1) copy plan 编译期展平——运行时零递归零判型**

嵌套 data 的布局差异（VM 侧指针槽 / 原生侧递归内联，§4.3）不在运行时
递归处理：**导出时（C# 侧）把嵌套布局完全展平为平面指令表**，每个叶子
成员一条指令，随 manifest 下发：

```c
typedef enum {
    SL_COPY,       /* 标量定长：member_data[vm_off] <-> buf[nat_off]，size 字节 */
    SL_COPY_DEREF, /* 嵌套 data：先解引用 VM 侧指针槽，再整块/继续逐条 */
    SL_STORE_PTR,  /* string/class/array：指针直存（借用） */
} SLCopyOp;
typedef struct {
    uint8_t  op;       /* SLCopyOp */
    uint16_t size;     /* 拷贝字节数 */
    uint32_t vm_off;   /* member_data 偏移（含嵌套子对象的绝对偏移） */
    uint32_t nat_off;  /* native buf 偏移（含 meta 头 32B 起点） */
} SLCopyInstr;
```

运行时执行 = **一个 for 循环 + switch(op)**：无递归、无类型名 strcmp、
无运行时偏移计算、无哈希重算——这些全部在编译期完成。marshal 与
unmarshal 共用同一张表，只是搬运方向相反。

**(2) 全标量 memcpy 快路径**

导出时检测：类型所有成员均为标量，且 packed 偏移序列与自然对齐偏移
序列逐个相等（如全 i32、或 i64 打头的 i64 序列）→ manifest 标记
`fastPath: "memcpy"`。运行时整块 `memcpy(member_data, buf+32, n)`，
判定 O(1)。典型 data 类型（Point/Rect/Color 等）几乎全部命中。

**(3) 边界缓冲池**

按 size class 分桶的 freelist（8B 步进至 4KB，超桶直接 malloc），
挂在 VM 上。取/还各 O(1)，免 malloc/free 的系统调用与堆碎片。
native_buf 生命期严格限于单次 AOT 调用（§5.3），归还即可复用。

**(4) 身份缓存（§5.7）**

回调桥命中即免 NewObject + VMRuntimeObject 数组构建——这是 unmarshal
最贵的部分（一次对象壳构建 ≈ 数百 ns，缓存查询 ≈ 数 ns）。命中路径的
双向同步退化为两次 copy plan 执行（快路径下即两次 memcpy）。

**(5) obj 引用零转换**

class/interface 引用参数全程 kind=3 直传指针：正向、AOT 体内传递、
回调桥三个环节**均无 marshal**。AOT 侧只在 `@sl_isa` 判型与透传时
接触该指针，从不解引用其内部布局。

**(6) 设计上直接排除的慢操作**

| 慢操作 | 排除手段 |
|---|---|
| 运行时类型名 strcmp / 哈希重算 | 全部编译期进 manifest（class_id + 布局表） |
| 运行时递归布局计算 / 类型判别 | copy plan 平面化（(1)） |
| 每次 malloc/free 边界缓冲 | 缓冲池（(3)） |
| 回调桥对象壳重建 | 身份缓存（(4)） |
| 返回值中转拷贝 | ret 缓冲预置协议（§5.6） |

### 5.9 端到端调用链路总览

本节把 §5.1–§5.8 分散的机制按**一次完整调用的时间轴**串成一条链，
两个方向的转换点（CVM→llvm 格式、llvm 格式→VMObject）全部标注。
以 `M.f(p: Point, k: Int64)` 调用 `c1: Circle` 的场景为例
（Point 是 data → kind=2；Circle 是 class → kind=3）：

```
CVM 解释器（求值栈：[p: VMObject(Point), k: i64, c1: VMObject(Circle)]）
   │ CallStatic M.f
   ▼
vm_aot_try_invoke（vm_aot_registry.c）
   ├─【T1 正向·struct 参数】p：缓冲池取 buf → copy plan 执行
   │        member_data(packed) → buf(自然对齐 + !sl_meta 头)
   │        args[0] = {kind=2, data=buf}；身份缓存登记 buf→p
   ├─【T2 正向·标量参数】k：args[1] = {kind=0, data=k 位模式}
   ├─【T3 正向·引用参数】c1：args[2] = {kind=3, data=(int64)c1}  零转换
   ▼
aot.dll @sl_m_M_f（LLVM 函数）
   ├─【T4 prologue】args[0] load/extract + inttoptr
   │        %p : !llvm.ptr<!sl_t_Point>（§5.6）
   │        args[2] 同理 → %c1 : !llvm.ptr<!sl_vmobj>（§5.10 视图）
   ├─ 函数体执行
   │    · %p 成员读写：getelementptr %p[0, i]（native 偏移，§5.5）
   │    · %c1 成员读写：视图 GEP member_data + vm_off（packed 偏移，§5.10）
   │
   │    ────── 反向调用点（AOT → CVM）──────
   │    ① CallStatic 命中解释方法：
   │        call @sl_vm_bridge(ctx, method_id, args', argc, ret')
   │        ├─【T5】kind=2 参数' → 查身份缓存 → 命中得源 VMObject*
   │        │        进桥写回 buf→obj；执行；出桥同步 obj→buf（§5.7）
   │        │        未命中（AOT 内 alloca 新建的 struct）→ 完整 unmarshal
   │        │        + NewObject → 调用后 marshal 回 buf（值语义）
   │        └─【T6】kind=3 参数' → 直接取 data 得 VMObject*，零转换
   │    ② 引用成员写（c1.name = s）：@sl_obj_member_store 桥（refcount）
   │    ③ 新建对象（Circle(...)）：@sl_new 桥 → NewObject → VMObject*
   │    ④ 类型判定（c1 as Shape）：@sl_isa 桥 → RuntimeClass 链遍历
   │
   ├─【T11 epilogue】返回值 struct → 构造进 ret 预置缓冲（§5.6）
   │                  返回值标量 → ret->data 位模式
   ▼
vm_aot_try_invoke 收尾
   ├─【T12 反向·写回】p：copy plan 执行 buf → member_data
   │        （AOT 体内可能改写过 buf）；身份缓存撤销本帧条目；buf 归池
   └─【T13 反向·返回值】ret.kind==2 → unmarshal 建新 VMObject → 压栈
```

**转换点汇总表**（"之间的调用"全清单，每行一个转换点）：

| # | 方向 | 输入 | 输出 | 机制 | 章节 |
|---|---|---|---|---|---|
| T1 | CVM→AOT 参数 | VMObject（member_data packed） | native_buf（自然对齐 + meta 头） | copy plan / memcpy 快路径 | §5.5/5.8 |
| T2 | CVM→AOT 参数 | 标量 | SLAotValue kind=0/1 | 位模式（现有） | §2.2 |
| T3 | CVM→AOT 参数 | class/interface 引用 | SLAotValue kind=3 | 指针直传，零转换 | §5.6 |
| T4 | AOT 入口 | kind=2 的 data | `!llvm.ptr<!sl_t_T>` | prologue inttoptr | §5.6 |
| T5 | AOT→CVM 回调参数 | native_buf | VMObject\* | 身份缓存命中 / unmarshal 新建 | §5.7 |
| T6 | AOT→CVM 回调参数 | kind=3 的 data | VMObject\* | 零转换 | §5.7 |
| T7 | AOT 内读成员 | VMObject\*（kind=3） | 标量值 | VMObject 视图 GEP + vm_off 直读 | §5.10 |
| T8 | AOT 内写成员 | VMObject\* + 引用值 | member_data 槽 | @sl_obj_member_store 桥（refcount） | §5.10 |
| T9 | AOT 内新建 | class_id | VMObject\* | @sl_new 桥（NewObject 完整语义） | §5.10 |
| T10 | AOT 内判型 | VMObject\* + 目标 id | i1 | @sl_isa 桥（RuntimeClass 链） | §5.10/§6.3 |
| T11 | AOT 返回 | 计算结果 | ret 预置缓冲 / 位模式 | epilogue 直写 | §5.6 |
| T12 | CVM 收尾 | native_buf | member_data | copy plan 写回 + 缓冲归池 | §5.5/5.8 |
| T13 | CVM 收尾 | 返回缓冲 | 新 VMObject → 求值栈 | unmarshal | §5.6 |

> kind=2 与 kind=3 的分界即"data 值语义 / class 引用语义"：前者两侧
> 各有布局需逐槽搬运（T1/T5/T12），后者指针同一、服务走桥（T3/T7-T10）。

### 5.10 AOT → CVM 对象服务桥

§5.7 只覆盖了 CallStatic 回调。AOT 体内凡是接触 **kind=3 对象**或需要
**CVM 语义完整操作**的场景，统一归入"对象服务桥"。设计原则：

> **读走直 GEP（快），写引用走桥（refcount 安全），创建/判型走桥
> （语义权威在 C 侧）。**

**(1) VMObject 原生视图**

AOT 侧不重定义 VMObject，只发一个"只读视图"别名（仅含需要访问的字段，
偏移以 C 侧 `offsetof` 导出值为准，示意如下）：

```mlir
/* 示意偏移（自然对齐推算），真值 = C 侧 offsetof 导出，
 * 加载期校验，不匹配则整个 aot.dll 降级解释器（Q10） */
!sl_vmobj = !llvm.struct<(
  i64,       /* [0] header（位域打包，AOT 不解）           */
  !llvm.ptr, /* [1] member_data（offset 8）                */
  i32,       /* [2] member_data_size（offset 16）          */
  i32,       /* [3] 对齐填充                                */
  !llvm.ptr, /* [4] member_runtime_objects（AOT 不用）     */
  i32,       /* [5] member_runtime_object_count            */
  i32,       /* [6] 对齐填充                                */
  !llvm.ptr  /* [7] runtime_type_ref（isa 入口）           */
)>
```

C 侧在 `vm_aot_load_library` 成功后、注册方法前，用 `offsetof` 实测值
与 manifest 里的 `vmobjLayout` 段比对（member_data / runtime_type_ref
两个关键偏移 + sizeof），不一致整模块降级——**布局演进有护栏**。

**(2) 标量成员直读/直写（T7）**

```mlir
/* c1.radius（Float64，packed 偏移 24） */
%c    = llvm.inttoptr %c1_raw : i64 -> !llvm.ptr<!sl_vmobj>
%md   = llvm.load %md_gep : !llvm.ptr        /* gep %c[0,1] → member_data */
%slot = /* gep %md + 24（vm_off，来自 copy plan 同一张表） */
%r    = llvm.load %slot : !llvm.ptr -> f64
```

- 偏移**复用 copy plan 的 `vm_off`**（member_data packed 偏移），
  manifest 零新增元数据——同一类型一张表，两种消费方式。
- 读 = 2 次 GEP + 1 load（纯指令，无调用）；标量写同理（无 refcount）。
- 嵌套 data 成员：槽里读出 VMObject\*（子对象），套同一视图继续 GEP。
- **读引用槽**（string/class）：直 GEP 取指针，按 opaque 借用（§5.3），
  不解引用其内部布局。

**(3) 引用成员写：@sl_obj_member_store 桥（T8）**

member_data 的引用槽（string/class/嵌套 data）由 refcount 管
（旧值 release、新值 retain，见 VMObjectHeader.refcount）。AOT 裸
`llvm.store` 会漏计数 → 悬垂。写引用槽统一走桥：

```c
/* C 侧实现（建议与身份缓存同文件 aot_bridge.c）：
 * 按 member_index 定位槽（member_runtime_objects 权威定义），
 * 完成旧值 release + 新值 retain + 写入。 */
void sl_obj_member_store(void* ctx, VMObject* obj,
                         int32 member_index, void* value);
```

**(4) 对象创建：@sl_new 桥（T9）**

```mlir
%c2 = llvm.call @sl_new(%ctx, %circle_id : i32)
      : (!llvm.ptr, i32) -> !llvm.ptr
```

C 侧按 class_id 查 RuntimeClass → **NewObject 完整语义**（含初始化器，
沿 base_class_id 链基类先跑，复用 runtime_call.c 既有逻辑）→ 返回
VMObject\*。与 Q7 的区分：`@sl_new` 造 VM 对象（class，进 GC/refcount
世界）；alloca 造纯 data 值（AOT 帧内，值语义）。NewObject 本身是
数百 ns 级操作，桥的一跳 C call 完全可忽略。

**(5) 类型判定：@sl_isa 桥（T10，kind=3 专用）**

§6.3 的 meta 链上溯只适用于 **kind=2**（native buf 的 `!sl_meta` 头在
AOT 手里，可内联上溯零调用）。kind=3 是 VMObject\*，继承链数据在 C 侧
RuntimeClass 表 → 走桥：

```mlir
%ok = llvm.call @sl_isa(%ctx, %c1, %shape_id : i32)
      : (!llvm.ptr, !llvm.ptr, i32) -> i1
```

即 §6.3 所述 C 侧 `sl_aot_isa`（沿 RuntimeClass.base_class_id 走
`runtime_class_manager_get_runtime_class_by_id`），此处仅明确其
AOT 侧调用形态。一次 C call + 链遍历（深度 ≤16）。

**(6) 服务表注入（桥函数的注册机制）**

与 stage-5 的 `sl_aot_bridge_init` 同一模式，但一次注入**服务函数表**
（后续扩展只加表项，不逐个导出新符号）：

```c
typedef struct {
    void  (*obj_member_store)(void* ctx, VMObject*, int32 member_index, void* value);
    void* (*obj_new)(void* ctx, int32 class_id);
    int32 (*isa)(void* ctx, VMObject* obj, int32 target_class_id);
    /* 预留：str_new / str_data / array_new / array_len ...（T3+ 按需） */
} SLAotServiceTable;

/* aot.dll 导出：int32 sl_aot_service_init(const SLAotServiceTable* tbl);
 * C 侧加载库后调用（紧跟 sl_aot_bridge_init 之后）。 */
```

AOT 体内对这些函数的调用一律经表指针间接调用（`%tbl` 在
`sl_aot_service_init` 时存入模块级全局），不依赖符号链接顺序。

**(7) 性能小结**

| 操作 | 路径 | 开销量级 |
|---|---|---|
| 标量成员读（kind=3） | 视图 GEP 直读（T7） | 3 条指令，无调用 |
| 标量成员写（kind=3） | 同上 store | 3 条指令 |
| 引用成员写（T8） | 桥 1 跳 | ~5 ns + refcount 原子操作 |
| 新建对象（T9） | 桥 + NewObject | 数百 ns（NewObject 本身） |
| isa（T10） | 桥 + 链遍历 | ~10 ns（浅链） |
| 回调 CallStatic（T5 命中） | 身份缓存 + 双向同步 | 2 次 copy plan + 解释执行 |

---

## 6. 继承机制

### 6.1 布局不变量（前缀兼容）

**INV-1**：派生类成员序列 = [基类全部成员（按基类声明序） | 自有成员]，
VM 的 `member_data` 与原生布局两侧同序。

由此推出原生侧前缀兼容：

```
!sl_t_Base    = !llvm.struct<(!sl_meta, base_f0, base_f1, ...)>
!sl_t_Derived = !llvm.struct<(!sl_meta, base_f0, base_f1, ..., own_f0, ...)>
                                    └────── 与 Base 成员区逐字段相同 ──────┘
```

- 基类成员在 Derived 里的偏移 == 在 Base 里的偏移（自然对齐规则下，
  前缀相同 → 偏移相同）。
- `!sl_meta` 头在两种视图下都在 offset 0，**内容填"实例的动态类型"**
  （构造 Derived 实例时 class_id = Derived 的 id）——这正是 OOP 中
  vtable/type-info 槽的语义。

**前置验证项 V-1**（实现前必须确认）：C# 导出端派生类的
`non_static_member_variable_list` 是否已包含继承成员且基类在前。
VM 侧证据（runtime_call.c:348 NewObject 沿 base_class_id 链基类先跑
字段初始化器、且初始化器要写进派生对象）支持该结论，但需在
SLModulePackageWriter 导出路径上落实。若现状不含继承成员，则导出端
需先补齐（派生类合并基类成员，基类在前）。

### 6.2 指针视图规则（Derived* ↔ Base*）

LLVM 结构体类型之间没有形式上的子类型关系，采用 C 的前缀转型惯例：

```mlir
%d : !llvm.ptr<!sl_t_Derived>
%b = llvm.bitcast %d : !llvm.ptr<!sl_t_Derived> -> !llvm.ptr<!sl_t_Base>
// 之后 getelementptr %b[0, i] 访问基类成员，偏移与 Derived 视图一致
```

- **向上转型（Derived → Base）**：发射器已知继承链（编译期信息），
  直接 bitcast，**免运行期检查**。
- **向下转型（Base → Derived）/ 接口判定**：必须运行期检查（§6.3）。

### 6.3 类关系判定机制（is-a）

三层机制，按编译期信息量分级：

**(1) meta 链上溯（通用兜底）**

`!sl_meta.base_class_id` 记录直接基类。运行时沿链上溯比较 class_id：

```mlir
// 模块级类型表：class_id -> 该类 meta 描述全局的地址
llvm.mlir.global constant @sl_class_table(...) : !llvm.array<N x !llvm.struct<(i32, i32, i64)>>
//                                            (class_id, base_class_id, meta_ptr)

// MLIR 发射的辅助函数（进入 aot.dll）
llvm.func @sl_isa(%obj : !llvm.ptr<!sl_meta...>, %target : i32) -> i1 {
  // 读 %obj 的 class_id；不等则沿 base_class_id 上溯（深度上限 16，
  // 与 VM_INITIALIZER_CHAIN_MAX_DEPTH 同量级），命中返回 true
}
```

C 侧同构函数 `sl_aot_isa(VMObject*, int32 target_class_id)` 供边界使用
（沿 `RuntimeClass.base_class_id` 走 `runtime_class_manager_get_runtime_class_by_id`，
与 runtime_call.c 的链遍历一致）。

**kind 通道适配**：上述 meta 链上溯仅适用于 **kind=2**（native buf 的
`!sl_meta` 头在 AOT 手里，内联上溯零调用）；**kind=3**（VMObject\*）
的判型经 `@sl_isa` 桥调用本节所述 C 侧函数（AOT 侧调用形态见 §5.10(5)），
一次 C call + 链遍历。

**(2) 祖先数组（O(depth) → O(1) 比较，可选优化）**

每个类型额外发射常量祖先链，`@sl_isa` 改为数组包含判定：

```mlir
llvm.mlir.global constant @sl_anc_Derived(dense<[...]> : tensor<...>)
// 内容：[depth, id_root, ..., id_base, id_derived]
```

depth 相等且 id 相等 → 同型；否则线性扫数组。适合深继承链高频转型场景。

**(3) 编译期静态判定（主路径）**

发射器在导出时已知静态类型的继承链：

- 静态向上转型：直接 bitcast，零检查。
- 静态类型即目标类型：什么都不做。
- 仅当转型目标无法静态证明（经 Object/接口中转）时才生成 `@sl_isa` 调用。

### 6.4 与 VM 侧判定的关系

VM 侧已有 `runtime_call.c` 的 base_class_id 链遍历。**判定权威在
RuntimeClass 表**（id 唯一、跨模块一致），`@sl_class_table` 的内容在
模块加载时由 C 侧从 manifest 校验/对齐（发现 id 冲突或 base 缺失时，
该 AOT 方法降级回解释器，不崩）。

---

## 7. 模板类

- **实例化展开**：`List<Int32>` 按实参完全展开成员布局
  （VM 侧 `vm_object_new_by_runtime_class` 已把 T 解析成具体 EVMType，
  两侧一致）。
- **模板信息只进 header**：`kind_flags.bit31 = is_template`；
  `template_ptr` 指向实参表，`tmpl_cnt` 为实参条数（即"描述这个指针
  的字段"）：

```mlir
!sl_tmpl_arg = !llvm.struct<(i32 /*arg_index*/, i32 /*arg_kind*/, i64 /*arg_ref*/)>
/* arg_kind: 0=i64 1=f64 2=string 3=class 4=data 5=enum 6=array
   arg_ref : class/data -> 该类型 meta 描述全局地址
             enum       -> 其 class_id
             标量       -> 位模式                                    */

llvm.mlir.global constant @sl_tmpl_List_I32(...) : !llvm.array<1x!sl_tmpl_arg>
```

- **泛型形参 T 的成员槽**（未经实例化的上下文，如泛型方法体内）：
  一律视为 8 字节 opaque 槽（i64/!llvm.ptr），与 VM 侧
  EVMType_Object 槽宽一致，不展开。
- 模板实参表内容同时写入 manifest `types[].templateArgs`，C 侧
  marshal 时用它核对实例绑定。

---

## 8. enum 处理

- **独立值（跨 AOT 边界）**：轻量盒 `!sl_t_Color = !llvm.struct<(i32 class_id, i32 value)>`
  （8 字节）。class_id 使枚举值自描述（可判别归属枚举类型），
  对应 VM 侧 Member 包装对象的语义。
- **作为成员**：退化为 i32 槽（与 VMD_SLOT_ENUM 一致），归属信息由
  所属对象的成员定义承载，不重复存储。
- enum 无名称/模板的运行时需求，不使用完整 `!sl_meta` 头。

---

## 9. 实现分期

| 阶段 | 内容 | 验证标准 |
|---|---|---|
| **P1 类型发射** | MLIRExporter：`!sl_meta`/`!sl_t_<t>` 别名、`@sl_name_*`/`@sl_tmpl_*`/`@sl_class_table` 全局；`ResolveSLType` 放行 class/data/enum（i64 载指针）+ 模块级 TypeTable；manifest 写入 types/params 槽类别；**布局表平面化（copy plan，含 fastPath 标记）**；**prologue inttoptr 发射 + kind=2/3 填写 + ret 缓冲预置 epilogue（§5.6）** | aot.mlir 过 `mlir-opt LowerPasses` + `mlir-translate`；含 class 参数的方法不再标 failed |
| **P2 边界互转** | csimple_lang：`sl_aot_marshal_object_to_native` / `unmarshal`（copy plan 解释器）+ `sl_aot_isa`；`vm_aot_try_invoke` 按 manifest 槽类别接入（kind=2/3 分发）；**边界缓冲池 + 身份缓存登记/撤销（§5.7/5.8）** | SL 写用例：@AOT 方法收发 data 参数，成员值往返一致 |
| **P2.5 回调桥扩展** | `vm_aot_invoke_vm_bridge` 的 kind=2/3 分发：身份缓存命中双向同步 + 未命中完整 unmarshal；AOT 调用点预填 ret 缓冲的对称协议 | SL 用例：AOT 内 CallStatic 传 data 参数给解释方法，两侧修改互相可见 |
| **P3 成员访问** | MLIRExporter：LoadMember/StoreMember/嵌套成员 → getelementptr + load/store；继承字段前缀访问；**对象服务桥：`!sl_vmobj` 视图发射 + `@sl_new`/`@sl_isa`/`@sl_obj_member_store` 服务表注入（§5.10），含 VMObject offsetof 加载期校验** | SL 用例：AOT 内读写基类/派生类成员、嵌套 data、class 引用成员；AOT 内 as 转型与新建对象 |
| **P4 类型判定** | `@sl_isa` 发射 + 向下转型 opcode；@sl_anc_* 可选优化 | SL 用例：AOT 内 as 转型 + 类型检查 |

依赖顺序：P1 → P2 → P2.5 → P3 → P4；P2 只依赖 manifest 里的布局表，可与 P3 并行开发；
P2.5（回调桥）依赖 P2 的 marshal/unmarshal 与身份缓存，可与 P3 并行。

---

## 10. 风险与开放问题（需确认）

| # | 问题 | 选项 | 建议 |
|---|---|---|---|
| Q1 | 布局口径 | A：自然对齐+逐槽重排（本方案主案）；B：packed 直映+memcpy | A（与现有 interop 一致；后续有性能证据再评估 B） |
| Q2 | INV-1 前置验证 | 派生类成员表是否已含基类成员且前置 | 实现前在 SLModulePackageWriter 导出路径核实；若否则先补导出端 |
| Q3 | meta 头位置 | AOT 原生缓冲头部（AOT 侧附加，VM 不存）；或塞进 member_data 头 32B（改 VM 布局） | 前者（VM 零改动） |
| Q4 | enum 形态 | 轻量盒 (class_id,value)；或完整 !sl_meta；或纯 i32 | 轻量盒 |
| Q5 | struct 型返回值 | 边界新建 VMObject 回装（走 NewObject 语义）；或暂不支持、标 failed | P2 先支持参数，返回值 P2.5（§5.6 的 ret 缓冲预置协议） |
| Q6 | @sl_class_table 与 RuntimeClass 表的一致性 | 加载期校验，冲突降级 | 如建议 |
| Q7 | AOT 内新建 struct 的分配方式 | alloca（函数内，零成本，返回即回收）；或 @sl_alloc 桥函数（跨调用生命期，向 VM 申请、可被回调桥 unmarshal 后引用逃逸） | P3 先 alloca（AOT 方法内构造 data 局部值/传参场景已够用）；出现"struct 逃逸出 AOT 调用"的需求（如存进 VM 侧字段）再加 @sl_alloc |
| Q8 | 回调桥双向同步粒度 | 进桥/出桥各一次全量 copy plan（简单正确）；或脏标记（AOT/解释侧改过才同步，省一半拷贝但两侧都要插桩） | 先全量：快路径下即两次 memcpy，插桩复杂度不值；有性能证据再评估脏标记 |
| Q9 | 身份缓存对嵌套调用的撤销策略 | 每 try_invoke 帧本地记录、各自撤销（当前设计）；或全局水位标记 | 前者（帧本地小数组即可，无交错风险） |
| Q10 | kind=3 对象成员访问路径 | `!sl_vmobj` 视图直 GEP（快，但耦合 VM 私有布局）；或全走 `@sl_obj_member_load/store` 桥（稳，每次一跳 C 调用） | 读直 GEP + 加载期 offsetof 校验（不匹配整模块降级）；引用写/创建/判型走桥（refcount 与继承链的权威在 C 侧） |
| Q11 | 桥函数暴露方式 | `sl_aot_service_init` 一次性注入服务表（与 `sl_aot_bridge_init` 同模式）；或每个服务逐符号 dlsym | 服务表（一次注入，后续扩展只加表项，aot.dll 不必逐个导出新符号） |

---

## 11. 涉及文件清单（实现时）

**simple_language（C#，发射侧）**
- `source/Front/Export/MLIR/MLIRExporter.cs` —— 类型别名/全局发射、ResolveSLType、TypeTable、**struct 参数 prologue（inttoptr）与返回值 epilogue（ret 缓冲预置）发射（§5.6）**、**`!sl_vmobj` 视图 + 服务表间接调用发射（§5.10）**
- `source/Front/Export/MLIR/MLIRExportManager.cs` —— manifest types/params 合并、**copy plan 平面化与 fastPath 标记（§5.8）**、**vmobjLayout 段（offsetof 校验数据）**
- `source/Front/Export/SLIR/SLIRTypes.cs` —— SLAotMethodPackage/SLAotPackage 字段扩展

**csimple_lang（C，VM 侧）**
- `src/vm/runtime/aot/vm_aot_registry.c/.h` —— invoke 前后 marshal/unmarshal 接入（kind=2/3 分发）、**回调桥 kind=2/3 扩展与双向同步（§5.7）**、sl_aot_isa、**身份缓存**、**`sl_aot_service_init` 服务表注入 + VMObject offsetof 校验（§5.10）**
- `src/vm/system_method_call/memory_system_method.c` —— 复用槽分类（VMD_SLOT_*），新增转换函数（建议独立 `aot_marshal.c`，含 **copy plan 解释器与边界缓冲池（§5.8）**）
- `src/vm/assembly/slir_assembly_data.h` + `src/vm/load/slir_json_module_loader.c` —— manifest types/copy plan/vmobjLayout 解析
