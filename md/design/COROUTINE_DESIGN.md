# SimpleLanguage 协程（spawn / await）程序设计文档

- 版本：v1.0（设计冻结稿）
- 适用范围：前端编译器（C# 前端 / C 编译器）、C VM（`src/vm`）、C# VM（`src/csharp`）
- 目标读者：负责实现本特性的开发 AI
- 关联文档：`md/GC_DESIGN.md`、`md/RUNTIME_LAYOUT_GUIDE.md`、`md/README_IR_VM_REWRITE.md`

***

## 1. 概述

### 1.1 目标

为 SimpleLanguage 增加\*\*协程（coroutine）\*\*能力，满足以下全部约束：

1. **无 async 污染**：不引入 `async`/`await` 函数修饰符，函数签名不因内部可挂起而改变；不出现 `Task<T>`/`Promise<T>` 包装类型。
2. **关键字极少**：语言层只新增 **`spawn`** 和 **`await`** 两个关键字。
3. **自动让出为主，`yield`** **仅是逃生舱语法糖**："让出 CPU"完全由运行时自动完成（安全点 + 循环回边调度检查 + 阻塞点隐式让出）。可选 `yield;` 关键字显式让出一次，等价 `cor.Yield()`，**不是状态机关键字**（不挂起帧、不产生完成值）。
4. **调用透明**：任意嵌套深度的普通函数内都可以 `await`，调用者无感知。
5. **协程返回值 =** **`return`** **的值**：`await t` 拿到的就是协程 `return` 的结果，无包装类型。

### 1.2 结论（必须遵守）

> **无 async 污染 + 任意深度挂起 ⇒ 必须是有栈协程（stackful coroutine）。**

有栈协程的前提是对 VM 调用模型做一次结构性重构：把当前"C 递归调用 + 上下文保存在 C 栈局部变量"的模型，改造成"**VM 内显式帧链（frame chain）**"模型。本设计的一切后续内容都建立在这个重构之上（见第 4 章）。

禁止采用无栈（状态机/CPS）方案，因为它必然要求编译器知道"哪个函数会挂起"，最终导致隐性函数着色，违背目标 1。

### 1.3 术语

| 术语                  | 含义                                                              |
| ------------------- | --------------------------------------------------------------- |
| 协程（Coroutine）       | 一个可挂起/恢复的执行单元，拥有自己的帧链和求值栈                                       |
| 帧（VMCallFrame）      | 一次 SL 方法调用的完整上下文（原 `vm_execute_method_by_id` 保存在 C 栈局部变量里的全部状态） |
| 帧链（frame chain）     | 协程内自下而上的帧栈，替代现在的 C 递归                                           |
| 挂起（suspend）         | 协程从 Running 变为 Suspended，执行权交还调度器                               |
| 安全点（safe point）     | 指令边界（解释循环中的每条指令执行前后），挂起/取消只允许发生在安全点                             |
| 根协程（root coroutine） | `main` 所在的协程，调度器启动的第一个协程                                        |

***

## 2. 语言设计

### 2.1 关键字

| 关键字     | 语法身份        | 说明                                                                     |
| ------- | ----------- | ---------------------------------------------------------------------- |
| `spawn` | 一元前缀**表达式** | 创建并启动新协程，立即返回协程句柄，不阻塞当前协程                                              |
| `await` | 一元前缀**表达式** | 等待句柄（或句柄数组）完成，取回返回值；阻塞当前协程、不阻塞线程                                       |
| `yield` | 语句          | `cor.Yield()` 的关键字语法糖：显式让出一次（见 2.3.4）                                  |
| `cor`   | 类型          | 协程句柄类型 `Core.Coroutine` 的**短别名**，仅为书写方便（正式类名 `Coroutine` 依旧可用，见 2.4） |

**不引入**：`async`、`awaitable`、`async/await` 函数修饰符等任何其他关键字。

### 2.2 语法规则

```ebnf
spawn_expr   := 'spawn' ( call_expr | function_expr )
await_expr   := 'await' unary_expr                    (* 操作数类型为 cor(=Coroutine) 或 cor[] *)
await_stmt   := await_expr ';'                        (* 语句形式：忽略返回值 *)
yield_stmt   := 'yield' ';'                           (* 等价 cor.Yield() *)
```

详细规则：

1. `spawn` 后必须跟**调用表达式或函数字面量** **`function() { ... }`**。禁止 `spawn x + 1` 这类任意表达式。
2. `await` 是表达式，优先级与 `new` 同级，可嵌套在任意表达式位置：
   - `var v = await t;`
   - `return await t;`
   - `int x = 1 + await t;`
   - `var rs = await [t1, t2, t3];`（数组字面量语法糖，见 3.4）
3. `await` 操作数编译期类型必须是 `cor`（即 `Coroutine`，动态类型则运行期校验）。
4. `spawn` 用作语句（丢弃句柄）等价 fire-and-forget 后台任务，合法。
5. `yield` 是**语句**（无参、不能作表达式），等价 `cor.Yield()`；它只做"主动让出 CPU 一次"，不挂起帧、不产生完成值（完成值只能来自 `return`）。

### 2.3 语义定义

#### 2.3.1 `spawn E`

1. 在**当前协程**的求值栈上求值 `E` 的参数（与普通方法调用完全一致）。
2. 运行时执行 `OpCode_CoroutineCreate`（见 4.5.1）：分配 `VMCoroutine`（新帧链 + 新求值栈 + 入口帧），把参数从当前栈搬入新协程的 arg slots。
3. 新协程进入 `Created → Ready`（加入调度器就绪队列），**当前协程立即继续**，不等待。
4. 表达式结果是协程句柄（`cor` 堆对象）。
5. `spawn` 本身不抛错误；失败（如资源耗尽）以 `Error` 值报告。

#### 2.3.2 `await t`

1. 求值 `t`，必须为 `cor` 句柄。
2. 若 `t` 已 `Dead`：**不挂起**，直接把 `t.return_value` push 到当前协程求值栈（同步完成，等价普通调用）。
3. 若 `t` 活跃：挂起当前协程（`state = Suspended`），设置 `current.blocked_on = t`，将 `current` 加入 `t.waiter_list`，调度器切换到下一个就绪协程。
4. 当 `t` 变 `Dead` 时，调度器遍历 `t.waiter_list`，把 `t.return_value` 写入各 waiter 的求值栈顶，并把 `t.error`（`Error` 值）写入当前帧错误槽，waiter 恢复为 `Ready`。
5. **错误**：若 `t.error != Error.OK`，`await t` 在 waiter 处以该 `Error` 值报错（错误跨协程传播，禁止吞错）。
6. `await` 自己的句柄 → 运行期报 `Error.InvalidOperation`（死锁检测，见 4.9）。

#### 2.3.3 `return`（协程完成值通道）

- 协程函数的 `return v` 使协程进入 `Dead`，`v` 存入 `return_value`。
- `return` 是协程完成值的**唯一通道**；带值的 yield（生成器语义：`yield v` 产生一个值并继续执行）明确列为二期功能，本期不实现。

#### 2.3.4 自动让出（让出不写业务代码）

让出 CPU 由三个自动机制完成，用户无需写任何东西：

| 机制       | 触发位置                                                    | 说明                                             |
| -------- | ------------------------------------------------------- | ---------------------------------------------- |
| 阻塞点隐式让出  | `await`、`Time.Sleep`、Channel 收发、阻塞 IO、`cor.WaitTimeout` | 这些操作内部统一调用 `vm_block_current(reason)`（见 4.6.4） |
| 循环回边调度检查 | 编译器在 `for`/`while` 每条回边插入 `OpCode_SchedCheck`（见 3.5）    | 时间片用完则自动放回就绪队列尾部，防止纯计算协程霸占 CPU                 |
| 函数调用点检查  | 每次 SL 调用返回后顺带执行                                         | 成本极低，复用现有调用路径                                  |

逃生舱：`yield;` 语句（等价 `cor.Yield()`，运行时让出一次）。它是关键字语法糖，但不是状态机关键字——只做"主动让出 CPU 一次"，不挂起帧、不产生完成值。

### 2.4 `Coroutine` / `cor` 类型与 API

`Core.Coroutine` 是协程句柄的**正式类名**；`cor` 是它的**短别名（关键字）**，仅为书写方便，二者是同一个类型。`Coroutine` 原名依旧完全可用。文档示例统一用短名 `cor`，下表所有 `cor.` 静态成员均可等价写作 `Coroutine.`。

| 成员                         | 签名           | 语义                                                        |
| -------------------------- | ------------ | --------------------------------------------------------- |
| `cor.Current`              | 静态属性         | 当前协程句柄                                                    |
| `cor.Status(c)`            | 静态方法         | 返回状态枚举 `CoroutineStatus`：`Created/Running/Suspended/Dead` |
| `cor.All(c1, c2, ...)`     | 静态方法，可变参数    | 等全部完成，返回 `Object[]`（顺序对应入参）                               |
| `cor.Any(c1, c2, ...)`     | 静态方法，可变参数    | 先完成者胜出，返回该协程句柄                                            |
| `cor.NextCompleted(tasks)` | 静态方法         | 返回 `(cor done, Object value)`；`done == null` 表示全部消费完      |
| `cor.WaitTimeout(c, ms)`   | 静态方法，返回 bool | 超时返回 false；未超时且完成返回 true                                  |
| `cor.Yield()`              | 静态方法         | 显式让出一次（`yield;` 关键字是其语法糖，库方法保留）                           |
| `c.Cancel()`               | 实例方法         | 请求取消，协程在下一个安全点以 `Error.Cancelled` 终止（见 4.9）               |
| `c.Status`                 | 实例属性         | 同 `cor.Status`                                            |
| `c.Error`                  | 实例属性         | 未捕获错误的 `Error` 值（Dead 后有效）                                |

协程不使用异常对象，错误统一由语言既有 `Error` 枚举（int32 错误码）承载。本文档涉及的取值：

| `Error` 值                | 含义                                  |
| ------------------------ | ----------------------------------- |
| `Error.OK`               | 无错误（默认值）                            |
| `Error.Runtime`          | 通用运行期错误（业务代码主动抛出）                   |
| `Error.InvalidOperation` | 非法操作：`await` 自己、`await` 非 `cor` 句柄等 |
| `Error.Cancelled`        | 协程被取消                               |
| `Error.StackOverflow`    | 协程求值栈溢出                             |
| `Error.AssertFailed`     | 断言失败（测试框架使用）                        |

泛型 `Coroutine<T>`（短名 `cor<T>`）为本期可选特性（若类型系统支持模板类则实现，否则用 `cor` + 运行期类型校验）。

### 2.5 编译期限制

以下情况**编译期报错**：

1. `spawn` / `await` / `yield` 出现在**静态初始化器**（即会进入"子 VM"执行的上下文：静态字段初始化表达式、静态构造函数）中。
2. `await` 操作数静态类型既不是 `cor` 也不是 `cor[]`（动态类型除外，运行期校验）。
3. `spawn` 后不是调用表达式或 `function() {}` 函数字面量。

***

## 3. 前端 / 编译器改动

### 3.1 Token 层

文件：`src/compile/define.h`（C 编译器）/ 前端 `SimpleLanguage.Core` TokenType 枚举。

新增三个 token（当前不存在，无冲突）：

```
TOKEN_SPAWN     // "spawn"
TOKEN_AWAIT     // "await"
TOKEN_YIELD     // "yield"
```

### 3.2 语法分析

- `spawn` / `await` 进入一元表达式分支（解析优先级与 `new` 相同）。
- `spawn` 的子树必须是 `CallExpression` 或 `FunctionExpression`（`function() {}`），否则语法错误。
- `await` 之后解析一个 unary 表达式作为操作数。
- `yield` 是语句关键字，只在语句位置出现（`yield;`），解析后展开为 `cor.Yield()` 系统方法调用（见 3.4）。

### 3.3 IR 层

文件：`src/ir/ir_data.h`。

新增三个 IR opcode（与 VM opcode 一一对应）：

| IR opcode                | payload              | 说明        |
| ------------------------ | -------------------- | --------- |
| `IR_OP_COROUTINE_CREATE` | `{ methodId, argc }` | 创建协程      |
| `IR_OP_AWAIT`            | 无                    | 等待协程完成    |
| `IR_OP_SCHED_CHECK`      | 无                    | 循环回边调度检查点 |

### 3.4 语法糖展开

编译器负责两处**编译期展开**，均不产生额外运行时成本：

1. `await [t1, t2, t3]`（`await` + 数组字面量）展开为 `await cor.All(t1, t2, t3)`：先生成对 `cor.All(...)` 的系统方法调用，再对结果执行 `IR_OP_AWAIT`。
2. `yield;` 展开为对 `cor.Yield()` 的系统方法调用（不新增 IR opcode；实现方也可选择新增 `IR_OP_YIELD`，二选一）。

### 3.5 `SCHED_CHECK` 插入规则

编译器在以下位置自动插入 `OpCode_SchedCheck`：

1. 每个 `for` 循环回边（`i++` 之后、条件跳转之前）。
2. 每个 `while` / `do-while` 循环回边。
3. `foreach` 的迭代回边。

插入必须保证：**循环体任意一次迭代结束都会经过调度检查点**。纯计算死循环因此可被公平调度（见 4.6.3）。

### 3.6 SLIR 输出

- `SLModulePackage` 结构不变（`slir_assembly_data.h`）；新 opcode 通过现有 `SLInstructionPackage.op_code` + `payload` 机制下发。
- `SLRuntimeDefTypePackage` 中注册**正式类型** **`System.Coroutine`**；关键字短名 `cor` 注册为别名，二者指向同一运行时类型，`Coroutine` 原名完全可用。
- 前端需把 `Coroutine`（短名 `cor`）类的方法导出为系统方法声明（`SLSystemCallPackage` 机制），VM 端注册实现。

***

## 4. VM 运行时设计（C VM 为主）

### 4.1 现状（重构输入，务必理解）

当前调用链（`src/vm/runtime/call/runtime_call.c`）：

```
vm_execute_method_by_id(vm, method_id, ...)：
  1. 把 vm 的下列状态保存到 C 栈局部变量（saved_*/bak_*）：
     - code / code_length / ip / name / is_running / error_code
     - runtime_arg_objs/cap, runtime_local_objs/cap, runtime_return_objs/cap
     - active_ir_method, current_runtime_type, runtime_type_list/count
     - debug_info_array/count, execute_index, instr_byte_offsets/count
     - try_stack 副本(saved_try_stack[64]), try_stack_depth
     - parent_try_stack, parent_try_stack_depth
     - has_pending_error, pending_error（Error 值）
  2. 给 callee 分配新的 runtime slots（vm_method_try_prepare_runtime_slots）
  3. 设置 callee 的 code/ip/name 等
  4. ++s_vm_call_depth; vm_run(vm); --s_vm_call_depth   ← C 递归！
  5. 恢复 caller 的全部状态，并把 callee 的 return slots 值 push 回 caller 求值栈
  6. 错误传播：callee_has_pending → vm_propagate_error / vm_set_pending_error（Error 值）
```

**这就是"帧链化"要消灭的 C 递归**。第 1 步保存的所有字段，原样搬入 `VMCallFrame` 结构（4.2）；第 4 步的"递归 + 返回恢复"改为"push frame / pop frame"。

其他现状约束：

- 调用深度硬限 64（`s_vm_call_depth > 64`，`runtime_call.c:455`）。帧链化后此限制解除，改为动态帧链容量。
- 求值栈是 VM 内固定数组 `uint8 stack[VM_STACK_SIZE]`（`vm_runtime.h`，8192 字节），**所有帧共享**。协程化后必须 per-coroutine（4.10）。
- 栈槽类型用并行数组 `stack_slot_kind` 记录（`VM_PTR_SIZE = sizeof(void*)`，栈上可存原生指针）。
- 参数绑定：`vm_bind_runtime_arguments_from_stack(vm, arg_extent)` 从 caller 栈按逆序弹参数写入 callee arg slots（`runtime_call.c:779-809`）。该函数在帧链化后**复用不变**。
- 返回值回传：callee 结束后遍历 `runtime_return_objs`，跳过 void 槽，把值 push 回 caller 栈（`runtime_call.c:696-722`）。同样复用。

### 4.2 `VMCallFrame` 结构（P0 交付物）

```c
typedef struct VMCallFrame
{
    /* 执行上下文（原 saved_* / bak_* 字段） */
    const uint8*   code;
    uint32         code_length;
    const uint8*   ip;
    char*          name;
    uint8          is_running;
    int32          error_code;

    /* runtime slots（每帧一份） */
    VMRuntimeObject* arg_objs;
    VMRuntimeObject* local_objs;
    VMRuntimeObject* ret_objs;
    int32            arg_cap;
    int32            local_cap;
    int32            ret_cap;

    /* 类型上下文 */
    const SLMethodPackage* active_ir_method;
    RuntimeType*  current_runtime_type;
    RuntimeType** runtime_type_list;
    int32         runtime_type_list_count;

    /* 调试信息 */
    SLInstructionDebugInfo** debug_info_array;
    int32         debug_info_count;
    int32*        instr_byte_offsets;
    int32         instr_byte_count;
    int32         execute_index;

    /* 错误状态（per-frame try 栈） */
    VMTryFrame   try_stack[VM_TRY_STACK_CAPACITY];
    int32        try_stack_depth;
    VMTryFrame*  parent_try_stack;      /* 链接到 caller 帧的 try 栈（原机制保留） */
    int32        parent_try_stack_depth;
    uint8        has_pending_error;
    VMRuntimeValue pending_error;      /* Error 值（int32 错误码承载） */

    /* 帧链成员 */
    struct VMCallFrame* caller;         /* 调用者帧，NULL = 协程入口帧 */
} VMCallFrame;
```

**P0 要求**：只做"把 `vm_execute_method_by_id` 的 saved\_*/bak\_* 局部变量替换为帧对象读写"，帧对象仍分配在 C 栈上（`VMCallFrame frame;` 局部变量），行为逐字节不变，通过现有全部测试。

### 4.3 帧驱动 `vm_run`（P1 交付物）

解释循环 `vm_run`（`src/vm/runtime/vm_runtime.c`）保持"while + switch"结构，但调用不再递归：

```
调用指令（CallStatic/CallDynamic/CallVirt/CallClosure）：
  → 构造 callee_frame（新帧链元素）
  → push_frame(coro, callee_frame)
  → vm_bind_runtime_arguments_from_stack(vm, argc)
  → continue                                       // 不递归

OpCode_Ret：
  → 把 ret_objs 的值 push 回 caller 帧的求值栈（复用 runtime_call.c:696-722 逻辑）
  → pop_frame(coro) → 恢复 caller 帧的 ip/code/slots/try_stack
  → 若 caller 帧存在 continue；否则协程结束（见 4.5）
```

错误传播改造：原第 5-6 步的"callee 错误恢复 + `vm_propagate_error`"改为帧对象间传递——`pop_frame` 时若帧有 pending error，把 `Error` 值传给 caller 帧，由 caller 帧的 try 栈决定是否捕获（`tryCatch` 语义保留在 `parent_try_stack` 链上，捕获对象为 `Error` 枚举值而非异常对象）。`vm_propagate_error`、`vm_execute_throw`、`vm_jump_to_instruction_index` 的既有逻辑全部保留，只是输入从"全局 vm 字段"改为"帧字段"。

**P1 验收**：C 递归清零（`s_vm_call_depth` 删除），`vm_run` 嵌套深度恒为 1；调用深度测试 > 64 层通过；现有测试全部通过。

### 4.4 `VMCoroutine` 结构（P3 交付物） 需要在cvm里边，增加一个vm/runtime/coroutine/vm_coroutine.h vm/runtime/coroutine/vm_routine.c的文件来放这些逻辑

```c
typedef enum
{
    CoroutineStatus_Created = 0,
    CoroutineStatus_Running,
    CoroutineStatus_Suspended,
    CoroutineStatus_Dead
} CoroutineStatus;

typedef struct VMCoroutine
{
    int32          id;
    CoroutineStatus state;

    /* 帧链 */
    VMCallFrame*   frame_chain;       /* 帧数组，栈式增长 */
    int32          frame_count;
    int32          frame_capacity;

    /* 求值栈（P2 起 per-coroutine） */
    VMStackSegment* stack_head;       /* 分段栈链表头（4.10） */

    /* 完成值与错误 */
    Object*        return_value;
    int32          error;             /* Error 值（Error.OK = 正常完成） */

    /* 等待关系 */
    VMCoroutine**  waiter_list;       /* 等我的协程（await 我的人） */
    int32          waiter_count;
    VMCoroutine*   blocked_on;        /* 我 await 的协程（死锁检测用） */
    int32          block_reason;      /* YIELD / SLEEP / CHANNEL / IO / AWAIT / TIMEOUT */

    /* 取消 */
    uint8          cancel_requested;

    /* 调度成员 */
    VMCoroutine*   sched_next;
    VMCoroutine*   sched_prev;        /* 就绪队列双向链表 */
    int64          quantum_used;      /* 本时间片已执行指令数（SCHED_CHECK 检查） */
    VMTimerNode*   timer_node;        /* 定时器堆节点（Sleep/WaitTimeout） */

    /* GC 链表（所有协程组成 GC 根列表） */
    struct VMCoroutine* gc_next;
    struct VMCoroutine* gc_prev;
} VMCoroutine;
```

`VM` 结构新增字段：

```c
VMCoroutine* current_coroutine;   /* 当前执行协程 */
VMCoroutine* root_coroutine;      /* main 所在协程 */
VMScheduler* scheduler;           /* 调度器（4.6） */
VMCoroutine* all_coroutines;      /* 全部协程链表头（GC 根集合） */
int32        coroutine_count;
```

### 4.5 新 opcode（编号 107-109）

文件：`src/vm/vm.h`，`OpCode_COUNT` 当前为 107。追加：

```c
OpCode_CoroutineCreate = 107,  /* Payload: methodId(int32) + argc(int32) */
OpCode_Await            = 108, /* 无 payload */
OpCode_SchedCheck       = 109, /* 无 payload */
```

同步在 opcode 名字表（`vm.h` 中的字符串数组）追加三个名字，保证名字/编号对齐。

#### 4.5.1 `OpCode_CoroutineCreate`

```
1. argc = payload->argc
2. 从当前协程求值栈弹出 argc 个参数（顺序：栈顶为最后一个参数）
3. coro = vm_coroutine_create(methodId, argc)
   - 分配 VMCoroutine，初始化状态 Created
   - 分配帧链（capacity 起步 8），创建入口帧，设置 code/ip/name/slots
   - 分配求值栈（4.10，起步 64KB）
   - 参数从弹出的 VMRuntimeValue 数组写入入口帧 arg slots
   - 登记到 vm->all_coroutines
4. coro->state = Ready；调度器 ready_enqueue(coro)
5. 把 coro 的句柄（Object*）push 到当前协程求值栈
```

#### 4.5.2 `OpCode_Await`

```
1. t = pop()  （VMRuntimeValue → Object*，校验为 `cor` 句柄（`VMCoroutine`），否则报 Error.InvalidOperation）
2. if t->state == Dead:
      if t->error != Error.OK: 以 Error 值报错（vm_execute_throw_error(t->error)）
      else: push(t->return_value); continue
3. if t == current_coroutine: 报 Error.InvalidOperation（await 自己）
4. current->blocked_on = t; current->block_reason = BLOCK_AWAIT
5. waiter_list_add(t, current)
6. vm_block_current(current)   // 见 4.6.4：挂起 + 调度
```

#### 4.5.3 `OpCode_SchedCheck`

```
1. current->quantum_used += 1
2. if quantum_used >= scheduler->quantum:      // 默认 10000 条指令
      current->quantum_used = 0
      vm_block_current(current)                 // 放回就绪队列尾部，公平轮转
3. if current->cancel_requested: 以 Error.Cancelled 报错（见 4.9 取消）
```

`Await` 恢复入口处同样检查 `cancel_requested`（保证被长期 await 的协程可被取消）。

### 4.6 调度器（P5 交付物）  vm/runtime/coroutine/vm_scheduler.h vm/runtime/coroutine/vm_scheduler.c

```c
typedef struct VMScheduler
{
    VMCoroutine* ready_head;          /* 就绪队列（FIFO 双向链表） */
    VMTimerNode* timer_heap;          /* 定时器最小堆（按 wake_at 排序） */
    int32        timer_count;
    int64        quantum;             /* SCHED_CHECK 时间片，默认 10000 */
    uint8        running;             /* 调度器是否在运行 */
    /* IO 等待表（二期）：fd → VMCoroutine* 等待者 */
} VMScheduler;
```

#### 4.6.1 调度循环（替代现有入口）

```
vm_run_coroutine(coro)：
  把 vm 的"当前执行上下文"切到 coro：
    vm->current_coroutine = coro
    vm->code/ip/... = coro->frame_chain[top] 的字段（帧驱动，见 4.3）
    coro->state = Running
    vm_run(vm)                     // 原有解释循环，直到该协程挂起/结束返回

vm_run_scheduler()：
  while (true):
    if (coro = ready_dequeue()): vm_run_coroutine(coro); continue
    if (timer 到期): 唤醒对应协程入 ready; continue
    if (timer_heap 空 且 无 IO 等待): break        // 所有工作完成
    // 无就绪协程但有定时器：阻塞等待最近的定时器（OS sleep 到最近 wake_at）
```

- `root_coroutine` 首先入队。
- 所有协程 Dead、无定时器、无 IO 等待时调度器退出，`Scheduler.Run()`（系统方法）返回 0。

#### 4.6.2 协程状态机

```
Created → Ready → Running ──await/Sleep/IO/Channel/SchedCheck──→ Suspended → Ready → ...
                     │                                                        ↑
                     └── return / 未捕获错误 ───────────────────────────────────┘
                          （设置 return_value / error，唤醒 waiter_list 全部协程）
```

**协程结束（Dead 化）处理**：

```
1. state = Dead
2. 把 return_value 与 error（Error 值）复制给每个 waiter：
   waiter 恢复后从 4.5.2 步骤 2 的"已 Dead"路径继续（取回值或按 error 报错）
3. 释放 frame_chain 与求值栈内存（对象不释放，句柄仍有效）
4. 若当前协程 Dead → 调度器切换
```

#### 4.6.3 公平性

- 协作式 + `SCHED_CHECK` 时间片轮转（默认每协程 10000 条指令，可配）。
- 纯计算循环协程靠 3.5 的插入规则获得调度机会（用例 D1 验证）。

#### 4.6.4 `vm_block_current` 原语

```
vm_block_current(coro)：
  coro->state = Suspended
  // 返回 vm_run 解释循环 → 回到 vm_run_scheduler 调度循环
```

阻塞系统调用统一入口：

```
vm_block_current_reason(coro, reason)：
  记录 coro->block_reason
  根据 reason 挂入对应等待结构（定时器堆 / 队列 / 等待表）
  vm_block_current(coro)
```

#### 4.6.5 定时器

```
vm_sleep_current(ms)：
  分配 VMTimerNode { wake_at = now + ms, coroutine }
  插入 timer_heap（最小堆）
  vm_block_current_reason(coro, BLOCK_SLEEP)
到期：出堆 → coro->state = Ready → ready_enqueue(coro)
```

#### 4.6.6 `Time` 类（计时与睡眠）

`Time` 是系统静态类，现有实现集中在 `time_system_method.c`（底层 `src/lib/time/sys_time.c`），共三个成员：

| 成员               | 签名           | 语义                                                                                                        |
| ---------------- | ------------ | --------------------------------------------------------------------------------------------------------- |
| `Time.Now`       | 静态属性，`int64` | **Unix 毫秒时间戳**（`vm_sys_timer_now_millis` → `libtime_now_unix_millis`）：自 1970-01-01 以来的毫秒数，用于记录绝对时刻、换算秒/日期 |
| `Time.Clock`     | 静态属性，`int64` | **高精度单调时钟**（毫秒，`vm_sys_timer_clock` → `libtime_clock_millis`）：与墙上时间无关、只增不减，适合计时/Stopwatch/超时计算，不受系统校时影响   |
| `Time.Sleep(ms)` | 静态方法         | 睡眠指定毫秒：协程上下文走 4.6.5 定时器（挂起让出，`ms <= 0` 只让出一次不阻塞）；根协程/子 VM 退化为真阻塞（4.7 规则）                                  |

计时惯例：**计算耗时用** **`Time.Clock`** **差值，记录绝对时刻用** **`Time.Now`**。`Time.Sleep(0)` 等价"让出一次"（用例 E2 验证调度器切换）。

### 4.7 阻塞系统调用改造（隐式让出，P5 交付物）

| 系统方法                                                    | 现状                           | 改造                                                                     |
| ------------------------------------------------------- | ---------------------------- | ---------------------------------------------------------------------- |
| `Time.Sleep(ms)`（`vm_sys_sleep`，`time_system_method.c`） | `libtime_sleep_millis` 真阻塞线程 | 走 4.6.5 定时器；`ms <= 0` 时只让出一次不阻塞                                        |
| `Console.ReadLine` / 文件读                                | 同步阻塞                         | 一期：线程池执行 + 完成回调唤醒（`vm_block_current_reason(coro, BLOCK_IO)`）；二期：真异步 IO |
| `Channel.Send/Recv`                                     | 无                            | 见 4.8（队列满/空则 `vm_block_current_reason`）                                |
| `Lock` / `cor.Join`                                     | 无                            | 拿不到锁/目标未 Dead → `vm_block_current_reason`                              |
| `cor.WaitTimeout(c, ms)`                                | 无                            | 注册定时器 + 等待 c，双条件唤醒                                                     |

**规则**：判断"是否让出"依据是"当前是否在协程上下文"。根协程/子 VM 内退化为同步执行。`main` 本身就是根协程，因此所有 SL 代码都天然在协程上下文。

### 4.8 Channel 与同步原语

`Channel<T>` 是库类型（运行时实现，非关键字）：

```
Send(v)  ：buffer 未满 → 入队；已满 → 挂起当前协程到 channel 发送队列
Recv()   ：buffer 非空 → 出队；为空 → 挂起当前协程到接收队列
Close()  ：置关闭标记，唤醒所有发送/接收等待者；之后 Recv 返回 null
```

唤醒规则：Send 后唤醒一个接收等待者；Recv 后唤醒一个发送等待者。有界/无界均支持（无界 = 容量不限）。

### 4.9 错误与取消

**设计原则**：没有异常对象，一切错误用 `Error` 枚举（int32 错误码）承载；`throw` / `tryCatch` 指令机制保留，只是抛出/捕获的值是 `Error` 值而非异常对象。

| 场景                        | 行为                                                                                                                             |
| ------------------------- | ------------------------------------------------------------------------------------------------------------------------------ |
| 协程内 `throw Error.xxx` 未捕获 | 协程 Dead，`error` 存入该 `Error` 值；等待者通过 `await` 以同值报错                                                                              |
| `await` 传播                | 在 waiter 协程的当前帧以该 `Error` 值报错（走现有 `vm_execute_throw` / try 栈机制），`Error` 值不变                                                    |
| 取消                        | `c.Cancel()` 置 `cancel_requested`；协程在下一个 `SCHED_CHECK` 或 `Await` 恢复点以 `Error.Cancelled` 终止；`finally` 正常执行（通过现有 try/finally 指令） |
| 取消已 Dead 协程               | 无操作，不产生错误                                                                                                                      |
| `All` 中某协程出错              | **立即失败**：取消其余子协程，向 await 者传播该 `Error` 值（用例 C6）                                                                                 |

### 4.10 求值栈 per-coroutine（分段栈，P2 交付物）

```c
typedef struct VMStackSegment
{
    uint8*    data;          /* 栈字节区 */
    SlotKind* kinds;         /* 与 data 一一对应的槽类型数组 */
    int32     capacity;      /* 段容量（默认 64KB 槽） */
    int32     top;           /* 段内栈顶偏移 */
    struct VMStackSegment* next;
} VMStackSegment;
```

规则：

1. 求值栈从 `vm->stack`（共享固定数组）迁移为协程自己的**分段链表**，段内自底向上，溢出时**追加新段，绝不 realloc 搬迁**。
2. **禁止搬迁**的原因：栈槽可存放 `sizeof(void*)` 的原生指针（`VM_PTR_SIZE`），搬迁即悬垂。
3. 栈操作函数（`vm_push_*` / `vm_pop_*` / `vm_pop_stack_top_to_vmvalue` 等，`src/vm/runtime/stack/` 下）改为基于 `current_coroutine->stack_head`，接口签名尽量不变以减少调用点改动。
4. 越界写栈 → 报 `Error.StackOverflow`（用例 H2）。
5. 每协程起步段容量可配（默认 64KB 槽），上限按需追加。

### 4.11 库方法运行时实现

**`cor.All(...)`** **/** **`await [..]`**（用聚合上下文对象，不额外创建协程）：

```c
typedef struct VMCoroutineWaitAll
{
    VMCoroutine*    target;         /* 发起等待的协程 */
    VMRuntimeValue* results;        /* 结果数组（长度 = total） */
    int32           total;          /* 子协程数 */
    int32           pending;        /* 未完成数 */
    uint8           failed;         /* 是否已失败（立即失败模式） */
    Error           first_error;    /* 第一个错误（failed 时有效，默认 Error.OK） */
    int32*          slot_of;        /* 子协程 id → results 下标 */
} VMCoroutineWaitAll;
```

1. `All` 调用时：把当前协程挂起（`BLOCK_AWAIT`），为每个子协程记录"完成回调槽"（内含 results 下标）。
2. 每个子协程 Dead 时：写入 `results[下标]`，`pending--`；若该子协程 `error != Error.OK` 且未失败 → 置 `failed`，取消其余子协程（对每个调用 `Cancel()`），记 `first_error`。
3. `pending == 0`（或 `failed`）时：恢复目标协程，push 结果数组（`Object[]`）或以 `first_error` 报错。

**`cor.Any`**：任一子协程 Dead 即恢复目标，push 赢家句柄；不取消其余。

**`cor.NextCompleted(tasks)`**：

1. 创建完成队列（`VMCoroutine**` FIFO），把当前协程注册为每个 task 的 waiter，`pending = tasks.length`。
2. 每个 task Dead 时：`done` 入队，`pending--`；若 `pending == 0` 或队列非空且有当前协程等待 → 恢复当前协程。
3. 当前协程恢复后从队列队首取 `done`：`done == null` 表示全部消费完（循环结束）；否则返回 `(done, done->return_value)`。
4. 恢复后的协程再次调用 `NextCompleted` 时：若队列仍非空则同步返回，否则继续挂起（循环等待）。

**`cor.WaitTimeout(c, ms)`**：双条件唤醒（定时器 + c Dead）：

1. 注册定时器（`wake_at = now + ms`）。
2. 注册为 c 的 waiter。
3. 挂起当前协程；恢复条件：定时器到期（返回 false）或 c Dead（返回 true）。
4. 恢复时撤销另一个未触发的等待（取消定时器或从 waiter\_list 移除）。

***

## 5. GC 设计

文件：`src/vm/memory/vm_gc.c`，设计基线见 `md/GC_DESIGN.md:233-243`。

### 5.1 现状问题（协程化必须一并修复）

当前 GC 根集合只包含：手动对象 + **当前 VM** 的求值栈 PTR/STRING 槽 + **当前 VM** 的 arg/local/return slots 行。在 C 递归模型下，外层帧的 slots 在 C 栈上、不在根集合——这是既有隐患；协程化后必须彻底解决。

### 5.2 新根集合

```
GC 根 = {
    vm 手动对象集合（手动持有根，不变）
    root_coroutine + all_coroutines 链上每个协程：
        - frame_chain 内每个帧的 arg/local/ret slots（VMRuntimeObject 数组）
        - 该协程求值栈的所有 PTR/STRING 槽（遍历 stack_head 分段）
    cor 句柄的 waiter_list、blocked_on 等内部引用（协程对象本身是 GC 可达对象）
    聚合上下文 VMCoroutineWaitAll（All/Any/NextCompleted 挂起期间）
    调度器：ready 队列、定时器堆中的 VMCoroutine 引用
}
```

### 5.3 对象生命周期

| 对象                       | 生命周期                                           |
| ------------------------ | ---------------------------------------------- |
| `VMCoroutine`（协程对象 + 句柄） | 由 SL 侧引用决定；Dead 后释放 frame\_chain 与求值栈内存，句柄对象保留 |
| 挂起协程持有的 SL 对象            | 根可达，不回收（用例 H3）                                 |
| Dead 且无引用的协程             | 帧链/栈内存已释放，句柄对象可回收（用例 H4）                       |

### 5.4 实现注意

1. `vm->all_coroutines` 双向链表供 GC 遍历，协程创建/结束时维护。
2. 协程的求值栈槽扫描：遍历分段栈的 `kinds` 数组，对 `PTR`/`STRING` 槽用既有 `vm_gc_mark_*` 路径标记。
3. 帧 slots 标记：`VMRuntimeObject` 数组本来就是对象根，复用现有标记函数，只是遍历范围从"当前 VM"变为"每个协程的每个帧"。
4. 不要对已经 Dead 且内存已释放的协程重复扫描（`state == Dead` 跳过帧链/栈）。

***

## 6. 与其他子系统交互

| 子系统          | 影响与要求                                                                                        |
| ------------ | -------------------------------------------------------------------------------------------- |
| 闭包           | 捕获是堆上的 `Object[]`，天然跨挂起存活，**无需改动**                                                           |
| 子 VM（静态初始化器） | 编译期禁止三关键字（2.5）；运行期若系统方法触发阻塞，退化为真阻塞；子 VM 内 `vm_block_current` 返回"不支持"错误                       |
| native 边界    | **native 函数体内不能挂起**。可能阻塞的系统方法必须在入口走 `vm_block_current_reason`，或拆成"发起 + 完成回调"；禁止在 native 中间让出 |
| 调试/栈追踪       | 帧链化后天然可打印完整 SL 调用链；挂起协程也可打印（用例 `vm_dbg.txt` 相关工具可扩展）                                         |
| 静态字段/全局      | 单线程协作式调度下天然无撕裂；若未来开 M:N 多线程，需补充内存模型规则（本期不做）                                                  |
| 手动对象持有（强引用）  | 现有"手动对象"机制继续作为根；协程对象创建时若需要，也注册为手动持有，避免调度器切换间隙被回收                                             |

***

## 7. 分阶段实施计划

每个阶段**必须通过现有全部测试 + 新增验收用例**，再进入下一阶段。改动文件清单以 C VM 为主。

### P0 帧结构抽取（纯重构，不改行为）

- 目标：`VMCallFrame` 结构定义落库，`vm_execute_method_by_id` 的 saved\_*/bak\_* 局部变量改为帧对象字段读写。
- 改动：`src/vm/runtime/call/runtime_call.c`、新增 `src/vm/runtime/call/vm_call_frame.h/.c`。
- 验收：行为逐字节不变；`run_test.bat` 全绿。

### P1 帧驱动 vm\_run（去 C 递归）

- 目标：调用/返回改为帧链 push/pop；删除 `s_vm_call_depth`；解除 64 层限制。
- 改动：`runtime_call.c`、`vm_runtime.c`（`vm_run` 中 Call/Ret 分支）、错误传播路径（`parent_try_stack` 链改为帧间传递）。
- 验收：用例 H1（200 层深递归）通过；调用深度测试 > 64 通过。

### P2 求值栈 per-coroutine

- 目标：`VMStackSegment` 分段栈；`vm_push_*`/`vm_pop_*` 改为基于 `current_coroutine->stack_head`。
- 改动：`src/vm/runtime/vm_runtime.h`（VM 结构）、`src/vm/runtime/stack/` 全部栈操作函数。
- 验收：用例 H2（栈溢出保护）通过；现有测试全绿。

### P3 `cor` 对象 + 三指令（非对称 MVP）

- 目标：`VMCoroutine` 结构、`OpCode_CoroutineCreate/Await`（`SchedCheck` 先实现为空操作）、`cor` 运行时类型注册、`await` 单个。
- 改动：`src/vm/vm.h`（opcode）、`src/vm/load/slir_json_module_loader.c`（payload 解析）、`vm_runtime.c`（指令分发）、新增 `src/vm/runtime/coroutine/` 目录（`vm_coroutine.c/.h`）。
- 验收：用例 A 组、B 组、H1 通过。

### P4 GC 根集合扩展

- 目标：根集合覆盖所有协程帧链与求值栈；Dead 协程资源回收。
- 改动：`src/vm/memory/vm_gc.c`。
- 验收：用例 H3、H4 通过；`Gc.Collect()` 相关测试通过。

### P5 调度器 + 阻塞改造

- 目标：就绪队列、定时器堆、`SCHED_CHECK` 生效、`vm_block_current_reason`、`Time.Sleep`/`yield`（`cor.Yield`）/`cor.WaitTimeout` 落库。
- 改动：新增 `src/vm/runtime/coroutine/vm_scheduler.c/.h`、`time_system_method.c`、`vm_coroutine.c`。
- 验收：用例 D、E、G5 通过。

### P6 库方法与前端收尾

- 目标：`All`/`Any`/`NextCompleted`、`Channel`、`Cancel`、C# VM 对齐、前端三关键字 + `SCHED_CHECK` 插入 + 语法糖展开。
- 改动：前端（token/parser/IR/emit）、`src/csharp`、系统方法注册表。
- 验收：全部用例（第 8 章）通过；C VM 与 C# VM 行为一致性测试通过。

***

## 8. 测试用例（验收标准）

语言层用例以 SL 源码形式给出。测试框架约定（非语言特性，仅为用例可读性）：

```csharp
// require(cond, name)：统一断言辅助，失败抛 Error.AssertFailed（name 仅供日志输出）
void require(bool cond, string name) throws { if (!cond) throw Error.AssertFailed; }
```

`g_*` 为全局辅助变量；每个 `test_xxx()` 一个用例；`Main` 中顺序执行并汇总。下列用例在 P6 完成时**必须全部通过**。

### A 组：`spawn` 基础

```csharp
// A1 基本 spawn + await 取回返回值
int Add(int a, int b) { ret a + b; }
void test_spawn_basic() {
    var t = spawn Add(3, 4);
    require(await t == 7, "A1 协程返回值");
}

// A2 fire-and-forget：不 await 也要跑完（副作用可见）
void SetFlag() { g_done = true; }
void test_spawn_no_await() {
    g_done = false;
    spawn SetFlag();
    for (int i = 0; i < 1000 && !g_done; i++) Time.Sleep(1);   // 轮询等后台协程跑完
    require(g_done, "A2 不 await 也执行完");
}

// A3 多参数
int Sum3(int a, int b, int c) => a + b + c;
void test_spawn_multi_arg() {
    var t = spawn Sum3(1, 2, 3);
    require(await t == 6, "A3 多参数");
}

// A4 句柄类型与状态
void test_spawn_handle_type() {
    var t = spawn Sum3(1, 2, 3);
    require(t is cor, "A4 句柄类型");
    require(cor.Status(t) == CoroutineStatus.Created || cor.Status(t) == CoroutineStatus.Running, "A4 状态");
}

// A5 void 协程：await 得 null
void DoNothing() {}
void test_spawn_void() {
    var t = spawn DoNothing();
    require(await t == null, "A5 void 协程 await 得 null");
}

// A6 返回值是数组
int[] MakeArr() => new int[] { 1, 2, 3 };
void test_spawn_complex_result() {
    var t = spawn MakeArr();
    var arr = await t;
    require(arr.Length == 3 && arr[2] == 3, "A6 返回数组");
}

// A7 spawn function 函数字面量
void test_spawn_function_literal() {
    var t = spawn function() { return 42; };
    require(await t == 42, "A7 spawn function");
}
```

### B 组：`await` 基础与串行/并行

```csharp
// B1 await 已完成协程 = 同步返回不挂起
void test_await_done() {
    var t = spawn Add(1, 2);
    await t;
    var t2 = spawn Add(2, 3);
    require(await t2 == 5, "B1 已完成协程直接取值");
}

// B2 串行执行：await spawn 连写，F2 等 F1 完成才启动
int Track() { g_order.Add("start"); Time.Sleep(50); g_order.Add("end"); return 1; }
void test_await_serial_exec() {
    g_order = new List<string>();
    await spawn Track();
    await spawn Track();
    require(g_order[0] == "start" && g_order[1] == "end" && g_order[2] == "start", "B2 串行");
}

// B3 并行执行 + 串行消费
void test_await_parallel_consume() {
    g_order = new List<string>();
    var t1 = spawn Track();
    var t2 = spawn Track();
    await t1;
    await t2;
    require(g_order[0] == "start" && g_order[1] == "start", "B3 并行执行");
}

// B4 await 嵌套表达式
void test_await_in_expr() {
    var t = spawn Add(10, 20);
    require(1 + await t == 31, "B4 await 嵌套表达式");
}

// B5 return await t
int Indirect() { var t = spawn Add(5, 6); return await t; }
void test_await_in_return() {
    require(Indirect() == 11, "B5 return await");
}

// B6 await 自己 → 运行期错误
void test_await_self() {
    cor self = cor.Current;
    bool threw = false;
    local {  try await self; } catch (Error e) { threw = e == Error.InvalidOperation; }
    require(threw, "B6 await 自己报错");
}
```

### C 组：批量等待

```csharp
// C1 await [..] 语法糖：全部完成，数组顺序对应
void test_all_array() {
    var rs = await [spawn Add(1,1), spawn Add(2,2), spawn Add(3,3)];
    require(rs.Length == 3 && rs[0] == 2 && rs[1] == 4 && rs[2] == 6, "C1 await 数组");
}

// C2 cor.All 显式写法
void test_all_explicit() {
    var t1 = spawn Add(1, 1);
    var t2 = spawn Add(2, 2);
    var rs = await cor.All(t1, t2);
    require(rs.Length == 2 && rs[0] == 2 && rs[1] == 4, "C2 All 库方法");
}

// C3 All 空列表 → 立即返回空数组
void test_all_empty() {
    var rs = await cor.All();
    require(rs.Length == 0, "C3 All 空");
}

// C4 Any：先完成者胜出
void test_any() {
    var t1 = spawn function() { Time.Sleep(100); return 1; };
    var t2 = spawn function() { Time.Sleep(10);  return 2; };
    var winner = await cor.Any(t1, t2);
    require(winner == t2, "C4 Any 快者胜出");
}

// C5 NextCompleted：按完成顺序消费
void test_next_completed() {
    var tasks = new cor[] {
        spawn function() { Time.Sleep(50); return 1; },
        spawn function() { Time.Sleep(10); return 2; },
        spawn function() { Time.Sleep(30); return 3; }
    };
    int sum = 0;
    while (true) {
        var (done, v) = await cor.NextCompleted(tasks);
        if (done == null) break;
        sum += v;
    }
    require(sum == 6, "C5 NextCompleted 全部消费");
}

// C6 All 中某协程出错 → 立即失败并取消其余
void test_all_error() {
    bool t2killed = false;
    var t1 = spawn function() { throw Error.Runtime; };
    var t2 = spawn function() { try { Time.Sleep(1000); } catch (Error e) { t2killed = e == Error.Cancelled; } return 0; };
    bool threw = false;
    try { await cor.All(t1, t2); } catch (Error e) { threw = e == Error.Runtime; }
    require(threw && t2killed, "C6 All 错误立即失败并取消其余");
}
```

### D 组：自动让出与调度公平性

```csharp
// D1 两个纯计算协程：回边 SCHED_CHECK 保证交替执行
void test_sched_fairness() {
    g_order = new List<string>();
    var t1 = spawn function() { for (int i = 0; i < 100; i++) g_order.Add("A"); return 0; };
    var t2 = spawn function() { for (int i = 0; i < 100; i++) g_order.Add("B"); return 0; };
    await cor.All(t1, t2);
    bool interleaved = false;
    for (int i = 1; i < g_order.Length; i++)
        if (g_order[i] != g_order[i-1]) { interleaved = true; break; }
    require(interleaved, "D1 自动让出交替执行");
}

// D2 yield; 关键字显式让出
void test_yield_keyword() {
    g_order = new List<string>();
    var t1 = spawn function() { g_order.Add("A1"); yield; g_order.Add("A2"); return 0; };
    var t2 = spawn function() { g_order.Add("B1"); return 0; };
    await cor.All(t1, t2);
    require(g_order[1] == "B1", "D2 yield 让出后 B 先跑");
}

// D3 1000 个协程：无栈溢出、无死锁
void test_many_coroutines() {
    var tasks = new cor[1000];
    for (int i = 0; i < 1000; i++) tasks[i] = spawn Add(i, 1);
    var rs = await cor.All(tasks);
    require(rs.Length == 1000 && rs[999] == 1000, "D3 1000 协程");
}

// D4 无就绪协程时调度器正常退出
void test_scheduler_exit() {
    spawn function() { Time.Sleep(5); return 0; };
    int exitCode = Scheduler.Run();
    require(exitCode == 0, "D4 调度器退出");
}
```

### E 组：定时器与 Sleep

```csharp
// E1 并行 Sleep：总耗时 ≈ max，不是 sum
void test_sleep_parallel() {
    var sw = Stopwatch.StartNew();
    var t1 = spawn function() { Time.Sleep(100); return 1; };
    var t2 = spawn function() { Time.Sleep(100); return 2; };
    await cor.All(t1, t2);
    var ms = sw.ElapsedMilliseconds;
    require(ms >= 100 && ms < 190, "E1 Sleep 并行总时长≈max");
}

// E2 Sleep(0) 只让出不阻塞线程
void test_sleep_zero() {
    var t = spawn function() { Time.Sleep(0); return 1; };
    require(await t == 1, "E2 Sleep(0)");
}

// E3 定时器唤醒顺序
void test_timer_order() {
    g_order = new List<int>();
    spawn function() { Time.Sleep(30); g_order.Add(30); return 0; };
    spawn function() { Time.Sleep(10); g_order.Add(10); return 0; };
    spawn function() { Time.Sleep(20); g_order.Add(20); return 0; };
    Scheduler.Run();
    require(g_order[0] == 10 && g_order[1] == 20 && g_order[2] == 30, "E3 定时器顺序");
}
```

### F 组：Channel 通信

```csharp
// F1 基本生产者-消费者
void test_channel_basic() {
    var ch = new Channel<int>(4);
    int sum = 0;
    var p = spawn function() { for (int i = 0; i < 5; i++) ch.Send(i); ch.Close(); return 0; };
    var c = spawn function() { while (true) { var v = ch.Recv(); if (v == null) break; sum += v; } return 0; };
    await cor.All(p, c);
    require(sum == 10, "F1 基本生产消费");
}

// F2 有界 channel 满时 Send 隐式让出，不忙等
void test_channel_blocked_send() {
    var ch = new Channel<int>(2);
    spawn function() { ch.Send(1); ch.Send(2); ch.Send(3); return 0; };   // 容量 2，第 3 个必须让出
    spawn function() { Time.Sleep(10); ch.Recv(); ch.Recv(); ch.Recv(); return 0; };
    Scheduler.Run();
    require(true, "F2 满时让出不忙等");
}

// F3 多生产者单消费者：不丢不重
void test_channel_multi_producer() {
    var ch = new Channel<int>(8);
    int count = 0;
    for (int i = 0; i < 4; i++)
        spawn function() { for (int j = 0; j < 10; j++) ch.Send(1); return 0; };
    spawn function() { for (int i = 0; i < 40; i++) ch.Recv(); count++; return 0; };
    Scheduler.Run();
    require(count == 40, "F3 多生产者不丢不重");
}

// F4 单生产者多消费者
void test_channel_multi_consumer() {
    var ch = new Channel<int>(8);
    var cnt = new Counter();
    spawn function() { for (int i = 0; i < 100; i++) ch.Send(i); ch.Close(); return 0; };
    for (int i = 0; i < 4; i++)
        spawn function() { while (true) { var v = ch.Recv(); if (v == null) break; cnt.Add(1); } return 0; };
    Scheduler.Run();
    require(cnt.Value == 100, "F4 100 个值被 4 个消费者消费");
}
```

### G 组：错误与取消

```csharp
// G1 协程内出错 → await 处报错，Error 值不变
void test_error_propagate() {
    var t = spawn function() { throw Error.Runtime; return 0; };
    bool threw = false;
    try { await t; } catch (Error e) { threw = e == Error.Runtime; }
    require(threw, "G1 错误跨协程传播");
}

// G2 未捕获错误的协程：状态 Dead，Error 可从句柄查
void test_error_status() {
    var t = spawn function() { throw Error.Runtime; return 0; };
    try { await t; } catch { }
    require(cor.Status(t) == CoroutineStatus.Dead && t.Error == Error.Runtime, "G2 句柄记录错误");
}

// G3 嵌套：子协程抛 → 父协程捕获
void test_error_nested() {
    var inner = spawn function() { throw Error.Runtime; return 0; };
    var outer = spawn function() { try { await inner; } catch (Error e) { return e; } };
    require(await outer == Error.Runtime, "G3 嵌套错误");
}

// G4 try/finally 在协程内正常执行（含挂起后）
void test_finally() {
    g_done = false;
    var t = spawn function() { try { Time.Sleep(10); } finally { g_done = true; } return 0; };
    await t;
    require(g_done, "G4 finally 执行");
}

// G5 Cancel()：下个安全点以 Error.Cancelled 终止，finally 必须执行
void test_cancel_finally() {
    g_done = false;
    var t = spawn function() {
        try { while (true) { for (int i = 0; i < 100000; i++) {} } }
        finally { g_done = true; }
        return 0;
    };
    t.Cancel();
    bool threw = false;
    try { await t; } catch (Error e) { threw = e == Error.Cancelled; }
    require(threw && g_done, "G5 取消时 finally 执行");
}

// G6 Cancel 对已结束协程无效
void test_cancel_done() {
    var t = spawn Add(1, 1);
    await t;
    t.Cancel();
    require(await t == 2, "G6 取消已结束协程无影响");
}
```

### H 组：资源与边界

```csharp
// H1 深递归协程（>64 层）：帧链化必须解除旧 64 层限制
int Deep(int n) { if (n <= 0) return 0; return await spawn Deep(n - 1) + 1; }
void test_deep_recursion() {
    var t = spawn Deep(200);
    require(await t == 200, "H1 深递归 200 层");
}

// H2 协程栈保护：超大局部数组触发溢出，报错而非崩溃
void test_stack_overflow() {
    var t = spawn function() { int[] big = new int[100000]; return big.Length; };
    bool threw = false;
    try { await t; } catch (Error e) { threw = e == Error.StackOverflow; }
    require(threw, "H2 栈溢出被捕获");
}

// H3 GC：挂起协程持有的对象不被回收
void test_gc_suspended_refs() {
    var holder = new RefHolder();
    var t = spawn function() { Time.Sleep(100); return holder.Get(); };
    Gc.Collect();
    var t2 = spawn Add(1, 1);
    await t2;
    require(await t == holder.Get(), "H3 挂起协程的对象存活");
}

// H4 Dead 且无引用的协程：帧链/栈被回收
void test_gc_reclaim() {
    for (int i = 0; i < 1000; i++) { var t = spawn Add(i, 1); await t; }
    Gc.Collect();
    require(Gc.CoroutineCount() < 100, "H4 Dead 协程被回收");
}

// H5 协程间共享静态字段（协作式单线程下无撕裂）
void test_shared_static() {
    g_counter = 0;
    var tasks = new cor[10];
    for (int i = 0; i < 10; i++)
        tasks[i] = spawn function() { for (int j = 0; j < 100; j++) g_counter++; return 0; };
    await cor.All(tasks);
    require(g_counter == 1000, "H5 共享静态字段无丢失");
}

// H6 闭包捕获跨挂起存活（按语言既有闭包捕获语义）
void test_closure_across_suspend() {
    int local = 5;
    var t = spawn function() { Time.Sleep(10); return local + 1; };
    local = 100;
    require(await t == 6, "H6 闭包捕获快照语义");
}

// H7 协程内再 spawn（树状并发）
void test_nested_spawn() {
    var t = spawn function() {
        var inner = spawn Add(40, 2);
        return await inner;
    };
    require(await t == 42, "H7 协程内再 spawn");
}
```

### I 组：与现有特性交互 / 限制

```csharp
// I1 静态初始化器（子 VM）里用 spawn/await → 编译或运行期报错
void test_subvm_forbidden() {
    // static int x = spawn Add(1,2);  → 编译期必须报错（不允许）
    require(true, "I1 子 VM 禁止关键字（编译期报错）");
}

// I2 多模块/命名空间下的 spawn
void test_spawn_across_module() {
    var t = spawn OtherModule.Worker(1);
    require(await t == 1, "I2 跨模块 spawn");
}

// I3 动态类型下 await（运行期校验）
void test_await_dynamic() {
    dynamic t = spawn Add(1, 2);
    require(await t == 3, "I3 动态类型 await");
    dynamic bad = 42;
    bool threw = false;
    try { await bad; } catch (Error e) { threw = e == Error.InvalidOperation; }
    require(threw, "I3 非 cor 运行期报错");
}

// I4 泛型 cor<T>（若类型系统支持）
void test_generic_coroutine() {
    cor<int> t = spawn Add(1, 2);
    require(await t == 3, "I4 泛型句柄");
}
```

### J 组：组合场景（冒烟）

```csharp
// J1 经典 pipeline：生产者 → 处理 → 聚合
void test_pipeline() {
    var raw  = new Channel<int>(4);
    var proc = new Channel<int>(4);
    int aggSum = 0;
    spawn function() { for (int i = 0; i < 100; i++) raw.Send(i); raw.Close(); return 0; };
    spawn function() { while (true) { var v = raw.Recv(); if (v == null) break; proc.Send(v * 2); } proc.Close(); return 0; };
    var agg = spawn function() {
        while (true) { var v = proc.Recv(); if (v == null) break; aggSum += v; }
        return aggSum;
    };
    await agg;
    require(aggSum == 9900, "J1 pipeline 聚合");   // 0..99 两倍和 = 9900
}

// J2 并发请求扇出-扇入
void test_fanout_fanin() {
    var tasks = new cor[50];
    for (int i = 0; i < 50; i++)
        tasks[i] = spawn Http.Get("http://localhost:8080/api/" + i);
    var rs = await cor.All(tasks);
    for (int i = 0; i < 50; i++) require(rs[i] != null, "J2 扇出扇入");
}

// J3 公平性 + 超时混合
void test_mixed_timeout() {
    var t = spawn function() { Time.Sleep(500); return 1; };
    bool ok = cor.WaitTimeout(t, 100);
    require(ok == false && cor.Status(t) == CoroutineStatus.Suspended, "J3 超时未完成");
    require(cor.WaitTimeout(t, 1000), "J3 第二次等到完成");
}
```

### 8.1 测试覆盖矩阵

| 组 | 覆盖点                                               | 对应章节           |
| - | ------------------------------------------------- | -------------- |
| A | spawn 基本/返回值/多参/句柄/void/数组/lambda/fire-and-forget | 2.3.1          |
| B | await 同步/挂起/串行 vs 并行/表达式嵌套/await 自己               | 2.3.2          |
| C | await\[..] 语法糖/All 空与错误/Any/NextCompleted         | 2.4、3.4、4.11   |
| D | 自动让出公平性/yield 关键字/1000 协程/调度器退出                   | 2.3.4、4.6      |
| E | Sleep 并行时长/Sleep(0)/定时器顺序                         | 4.6.5          |
| F | Channel 生产消费/满时让出/多生产者/多消费者                       | 4.8            |
| G | 错误跨协程/句柄记录/嵌套/finally/Cancel                      | 4.9            |
| H | 深递归 200 层/栈溢出保护/GC 挂起引用/GC 回收/共享字段/闭包/嵌套 spawn    | 4.3、4.10、第 5 章 |
| I | 子 VM 禁止/跨模块/动态类型/泛型                               | 2.5            |
| J | pipeline/扇出扇入/超时混合                                | 综合             |

***

## 9. 风险与注意事项（实现红线）

1. **求值栈禁止搬迁**：栈槽可存 `sizeof(void*)` 原生指针（`VM_PTR_SIZE`），分段栈只追加不 realloc。任何"栈复制/搬迁"实现都会造成悬垂指针。
2. **native 函数体内禁止挂起**：挂起只允许发生在解释循环的安全点（指令边界）。系统方法要么瞬时返回，要么在入口 `vm_block_current_reason`，要么拆成"发起 + 完成回调"。
3. **协程不持有独立堆**：对象池、LOS、弱引用表全部保持 per-VM 共享；协程只私有"帧链 + 求值栈"。违反此条 = 内存模型返工。
4. **子 VM 不支持挂起**：静态初始化器等子 VM 上下文内遇到阻塞调用退化为真阻塞；语言层编译期拦截三关键字。
5. **取消必须在安全点生效**：`Cancel()` 只置标志，禁止在任意指令中间打断；保证 finally 可执行。
6. **`Await`** **恢复点与** **`SCHED_CHECK`** **都要检查** **`cancel_requested`**，否则长期被 await/死循环的协程无法取消。
7. **Dead 协程的 waiter 唤醒顺序**：waiter 按注册顺序逐个恢复即可，不保证顺序语义；`All` 的结果顺序以入参顺序为准，与完成顺序无关。
8. **C# VM 行为一致性**：三指令语义、错误传播、调度器行为必须与 C VM 一致；以本文档为唯一规格。
9. **OpCode 编号**：新 opcode 从 107 开始（当前 `OpCode_COUNT = 107`），同时更新 opcode 名字表，禁止复用已有编号。
10. **`SCHED_CHECK`** **成本**：回边插入会增大代码体积，插入必须只在回边（不在循环体内）；默认时间片 10000 条指令可按需调大以降低开销。

***

## 附录 A：改动文件清单（C VM）

| 文件                                               | 阶段    | 改动                                           |
| ------------------------------------------------ | ----- | -------------------------------------------- |
| `src/compile/define.h`                           | P6    | 新增 `TOKEN_SPAWN`/`TOKEN_AWAIT`/`TOKEN_YIELD` |
| `src/compile/parser.c`                           | P6    | 一元表达式分支 + `yield;` 语句                        |
| `src/ir/ir_data.h`                               | P6    | 三个新 IR opcode                                |
| `src/vm/vm.h`                                    | P3    | 新 opcode + 名字表                               |
| `src/vm/load/slir_json_module_loader.c`          | P3    | payload 解析                                   |
| `src/vm/runtime/vm_runtime.h`                    | P0-P2 | `VM` 结构新增协程字段                                |
| `src/vm/runtime/vm_runtime.c`                    | P1-P3 | 帧驱动 + 指令分发                                   |
| `src/vm/runtime/call/runtime_call.c`             | P0-P1 | 帧链化改造                                        |
| `src/vm/runtime/call/vm_call_frame.h/.c`         | P0    | 新增：帧结构                                       |
| `src/vm/runtime/coroutine/vm_coroutine.h/.c`     | P3-P5 | 新增：协程对象                                      |
| `src/vm/runtime/coroutine/vm_scheduler.h/.c`     | P5    | 新增：调度器                                       |
| `src/vm/memory/vm_gc.c`                          | P4    | 根集合扩展                                        |
| `src/vm/runtime/stack/`                          | P2    | 栈操作 per-coroutine                            |
| `src/vm/system_method_call/time_system_method.c` | P5    | Sleep 改造                                     |
| `src/vm/system_method_call/`（Channel/Lock）       | P6    | 新增系统方法                                       |
| `src/csharp/`（对应 RuntimeVM）                      | P6    | 三指令对齐                                        |

## 附录 B：C 级单测补充建议（实施后随阶段添加）

在 `tests/vm/vm_test.c` 体系内为以下内部组件各补一组 C 单测：

1. `VMCallFrame` push/pop 与字段迁移正确性（P0）。
2. 帧驱动下深调用（500 层）不耗尽 C 栈（P1）。
3. `VMStackSegment` 追加/回退/越界报错（P2）。
4. `VMCoroutine` 生命周期与状态机迁移（P3）。
5. 调度器就绪队列 FIFO、定时器堆排序与唤醒（P5）。
6. GC 在挂起/Dead 协程存在时的标记正确性（P4）。

