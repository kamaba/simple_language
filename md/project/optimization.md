# 编译优化等级（CLI `-O0` ~ `-O3`）

## 概述

Front 编译器的 **IR 优化等级**由 CLI 选项控制，作用于**编译期 IR 生成阶段**（`source/Front` 层），不涉及 VM 运行时行为：

```
sl compile -p <工程> -O0     # 关闭全部 IR 优化
sl compile -p <工程> -O1     # 基础优化（默认等级）
sl compile -p <工程> -O2     # 中级优化
sl compile -p <工程> -O3     # 最高优化
```

- 写法大小写均可：`-O1` 与 `-o1` 等价。
- **默认等级 1（`-O1`）**：不传 `-O` 选项时按 `-O1` 编译。
- 等级单调递增：`-O2` 包含 `-O1` 的全部优化项，`-O3` 包含 `-O2` 的全部。
- 全部优化项**只影响生成的 IR / 指令选择**，不改变语言语义；同一份源码在任何等级下运行结果一致。

> **与 jsonc `compile.optimize` 是两套独立机制**：工程配置 `*.jsonc` 中 `compile.optimize`（boolean）决定**构建模式标记**（导出包中记为 `release` / `debug`，见 `SLIRTypes.cs` `SLProjectPackage.buildMode`）；CLI `-O0..-O3` 决定 **IR 优化等级**。二者互不干扰，可同时使用。

---

## 等级与优化内容一览

| 等级 | 优化项 | 生效条件 | 实现位置 |
|------|--------|----------|----------|
| `-O0` | （无——关闭全部 IR 优化） | — | — |
| `-O1`（默认） | ① null 快速判断 peephole | `optimizeLevel >= 1` | `IR/IRMethod.cs` |
| | ② 小函数自动 inline（体顶层语句 ≤ 2） | `optimizeLevel >= 1` | `Core/MetaMemberFunction.cs` + `Project/`（自动标记步骤） |
| `-O2` | ① + ② 全部内容；自动 inline 阈值放宽到 **≤ 4** 条 | `optimizeLevel >= 2` | 同上 |
| `-O3` | ① + ② 全部内容；自动 inline 阈值放宽到 **≤ 7** 条；新增 ③ 常数融合 store | `optimizeLevel >= 3` | 同上 + `IR/IRVariable.cs` + `Export/SLIR/SLModulePackageWriter.cs` |

各项详解见下文。

---

## 优化项详解

### ① null 快速判断 peephole（`-O1` 起生效）

把函数体内 `?.` / `??` / `var == null` / `var != null` 判断产生的 IR 序列：

```
[Dup]LoadConstNull + Ceq/Cne + BrFalse/BrTrue
```

融合为单条专用分支指令 `BrIsNullPeek` / `BrIsNull` / `BrNotNull`（null 判断直接在 C VM 内完成，省去压 null 常量与比较两条指令）。扫描发生在函数 IR 末尾的 label 回填之前（`IRMethod.cs` `ApplyNullCheckFastBranchPeephole`）。

### ② 小函数自动 inline（`-O1` 起生效，阈值随等级放宽）

编译器在 Meta 层语义分析完成后、IR 生成前，**自动给满足条件的小函数打 inline 标记**，其后的 IR 生成阶段按 `inline` 方法规则把函数体替换到调用点（无 `Call` 指令，零函数帧开销）。阈值按优化等级分档：

| 优化等级 | 自动 inline 条件（体内顶层语句数） |
|----------|-----------------------------------|
| `-O0` | 不启用 |
| `-O1`（默认） | ≤ 2 条 |
| `-O2` | ≤ 4 条 |
| `-O3` | ≤ 7 条 |

- **语句计数口径**：与 `inline` 方法修饰符的规模检查（LID 21453）一致——**方法体顶层语句数**，嵌套子语句不单独计数；上限恒为 10 条（`-O3` 的 7 也在其内）。
- **前提**：函数**不允许带 `throws` 标签**（`inline` 是调用点体替换，与 `throws` 异常帧语义冲突，同 LID 21457 的约束）；且函数必须有非空方法体。

**排除集**（满足任一即不自动标记；编译器只做保守安全的优化）：

| 排除项 | 理由 |
|--------|------|
| 已显式标 `inline` 的函数 | 无需重复标记 |
| 带 `throws` 标签 | 与 inline 语义冲突（同 21457） |
| 构造函数（`_init_`） | 构造语义特殊，不参与体替换 |
| abstract / 接口方法 / override 接口方法 | 无体或接口分派 |
| override 链上的实例方法（自身 `override` 父类，或被任何子类 override） | 实例 inline 按静态绑定且隐含 `final`，与多态冲突 |
| operator 运算符重载方法 | 运算符调用解析链特殊，保守排除 |
| get / set 属性访问器 | 属性访问不走普通调用点展开路径 |
| 闭包合成函数、模板函数、ref 模块导入（`ref` 声明）的函数 | 无源体 / 实例化拷贝无 FileMeta / 无体 |
| 体内含违禁构造（`spawn`/`isolate`/`await`/`yield`、`try`/`catch`/`defer`、`goto`、闭包或内联 lambda 定义、递归自引用等，同 LID 21449-21452 扫描口径） | 展开后无法等价发射——**静默预检不过即跳过，不报编译错误** |

其余语义细节（实例方法隐含 `final`、`this` 槽绑定、`ret` 改写等）与 `inline` 方法修饰符完全一致，见 `md/syntax/inline_lambda.md` 第二章。

**与显式 `inline` 的差异**：

| 维度 | 显式 `inline` 修饰符 | 自动 inline（`-O` 触发） |
|------|---------------------|--------------------------|
| 触发方式 | 用户源码声明 | 编译器按 `-O` 等级自动标记 |
| 定义点检查（21453/21457 等） | 命中即**编译错误** | 排除集**静默跳过**（优化不制造错误） |
| 在变量初始化器处直接调用 | **编译错误**（LID 21458：初始化器无宿主函数语句流，无法展开） | **自动回退为正常 `Call` 指令**（该调用点不做内联，其余调用点照常展开） |
| 源码可见性 | 显式可见 | 不可见（可在 `DebugCode/.../IR.txt` 中观察：调用点无 `Call` 指令） |

> 自动 inline 的初始化器回退说明：字段 / enum 成员 / data 成员初始化器不在任何函数体内，体展开无宿主语句流。显式 `inline` 因此直接禁止（21458）；自动 inline 属编译器优化，此场景**降级为普通调用**，语义不变、优化不生效。

### ③ 常数融合 store（`-O3` 生效）

把 `变量 = 常量` 赋值的 IR 序列：

```
LoadConst* + Store*
```

融合为单条 `Store*ConstValue` 指令（如 `StoreLocalConstValue` / `StoreGlobalConstValue` / `StoreArrayIndexConstValue` 等 opcode 族，见 `IROpEnum.cs`）。值直接编码进指令 payload，不再压栈再弹出。不满足融合条件（AOT/MLIR 后端、多指令序列、`StoreStaticField` 变长 payload 等）时自动回退经典 `LoadConst` + `Store` 路径。

---

## 用法示例

```powershell
# 默认 -O1（null peephole + 自动 inline ≤2）
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\Other\SomeTest\ProjectTest

# -O3 全开（null peephole + 自动 inline ≤7 + 常数融合 store）
dotnet run --project source\Front\SimpleLanguageFront.csproj -- compile -p test\Other\SomeTest\ProjectTest -O3
```

验证方式：编译产物 `out/export/<模块>/DebugCode/<源文件>/IR.txt` 中查看调用点是否仍有 `Call` 指令（自动 inline 生效后小函数调用点无 `Call`）；或对比 `Logs/Result.txt` 运行输出（各等级应完全一致）。

---

## 实现位置（全部在 Front 层）

| 模块 | 职责 |
|------|------|
| `Front/CLI/CommandInputArgs.cs` | 解析 `-O0..-O3`（含小写）→ `optimizeLevel`（默认 1） |
| `Front/Project/ProjectManager.cs` | 静态存储 `optimizeLevel`，`Run` 入口在任何 IR 生成前应用 |
| `Front/Core/MetaMemberFunction.cs` | `isInline` 标记（显式 + 自动两来源）；`ScanInlineMethodBody` 违禁扫描（自动标记复用其静默预检模式） |
| `Front/Project/`（编译流水线） | 自动标记步骤：Meta 层语义分析完成后、IR 生成前，按等级阈值（2/4/7）标记候选函数 |
| `Front/IR/IRCall.cs` | 调用点体展开（`isInline` 命中即展开；自动 inline 在无宿主上下文〔初始化器〕处回退正常 `Call`） |
| `Front/IR/IRMethod.cs` | ① null peephole（`optimizeLevel >= 1`） |
| `Front/IR/IRVariable.cs` + `Export/SLIR/SLModulePackageWriter.cs` | ③ 常数融合 store（`optimizeLevel >= 3`） |
