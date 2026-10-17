# 复盘分片 01 · Front 编译期

> 收录：词法/语法/类型系统/IR 生成/导出 SLIR/关键字拦截类复盘（含内联 lambda、typealias、import、字段初始化）。
> 路由索引与追加规则见 [故障排查流程 §6](../故障排查流程.md)；新复盘按域追加至本片末尾。

### [2026-09-11] JsonTest 嵌套 float 取值失败：非 bug——无后缀小数字面量默认 Float32 的精度语义
- 现象：`user["height"].asFloat64() == 1.68` FAIL，而同形式 `nums[2].asFloat64() == 4.5` OK。
- 首个异常阶段：非 VM 缺陷（词法语义 + 精度）。
- 根因：无后缀小数字面量默认 Float32（`LexerParseToToken.cs` EndReadNumberByOtherChar，注释"无后缀的小数字面量（含 0.0）默认 float32"）。module.json 中 1.68 常量 payload 4 字节 `3D 0A D7 3F` = Float32(1.68)≈1.6800000667572021，而 asFloat64 用 strtod 得 Float64 1.68≈1.6799999999999999；C# 基准（RuntimeValueCompare.cs TryCompareNumericValue）同样把 Float32 提升为 double 后比较，C# VM 下此断言也 FAIL。4.5 二进制精确（100.1₂），Float32 无损所以碰巧通过。
- 修复点：无代码修复；测试断言改为 `1.68d`（Float64 字面量）并补 `asFloat32() == 1.68f` 覆盖 Float32 路径（test/JsonTest/JsonTest.sl）。
- 如何快速复现：任一 SL 工程写 `X.asFloat64() == 1.68`（1.68 换任意二进制不精确小数）。
- 如何快速验证：JsonTest 全绿；Grep module.json 确认常量 byteLength=4（Float32）vs `1.68d` 为 8 字节。
- 预防建议：比较 Float64 返回值一律用 `d` 后缀字面量（Float32 用 `f`）；无后缀小数按 Float32 理解，勿与 C/C#/Java 的 double 默认混淆。

### [2026-09-12] Component 系统方法下沉后 ComponentTest 虚表错位：Front 导入侧 methodList 桩 declaringClassId=0 致 owner 错设
- 现象：Std/Component.sl 查询/消息方法下沉为 SystemComponent* 后 C VM 复测 ComponentTest 大面积 FAIL（getComponent 返回 null、广播顺序错乱）；VM 侧插桩证实 CallVirt 分派进了错误方法。另踩一坑：重编 Std/ProjectTest 后复测仍复现 FAIL 且输出旧文案（跑的旧模块）。
- 首个异常阶段：Export/导入侧（ProjectReferenceModuleLoader，非 C VM 缺陷）。
- 根因：①引用模块 methodList 桩的 IRMethod.declaringClassId=0（旧导出兼容），`BuildMetaMemberFunctionFromIR` 把 owner 落到当前导入类（如 Std.Component），functionAllName 被重算成 `Std.Component.type`，与引用模块 methodList 中的 id（`Core.Object.type`）不一致；C VM 装配虚表按 id 查找失败，跳过 3 个继承方法（type/hashCode/equals），虚表槽位整体左移，CallVirt 分派全部错位。②Front `compile` 动词默认只跑到 IR（`ProjectCompile.Compile()`=RunToPhase(IR)），仅带 `-e ir` 时 `CommandExecutor.ExecuteCompile` 才 RunPhase(Export)；不带则 IR.txt/DebugCode 更新而 module.json 不更新，C VM 跑旧模块。
- 修复点（文件/模块）：`source/Front/Project/ProjectReferenceModuleLoader.cs` `BuildMetaMemberFunctionFromIR`：declaringClassId==0 时按 irm.id 前缀还原声明类（`TryResolveDeclaringClassFromMethodId`），owner/functionAllName 与导出端对齐。
- 如何快速复现：重编引用了含继承方法的模块（如 Std.Component extends Core.Object）的工程后跑 C VM，观察系统性 FAIL + VM 装配日志中方法被跳过。
- 如何快速验证：`compile -p <.sp> -e ir` 重编 Std + ProjectTest → C VM 跑 ProjectTest.module.json，ComponentTest 36 check 全 PASS；全套件（cwd=测试集目录）196 PASS / 0 FAIL。
- 预防建议：①导出模块必带 `-e ir`，以 module.json 时间戳 + 日志 `export module success`（ProcessSummary 的 Export/ExportModule 不得为 [Pending]）确认真导出；②独立 exe 跑测试集须以测试集目录为 cwd（Sqlite3Test 的 `Resources/ttest` 相对路径，宿主约定见 md/project/test-guide.md）；③"CallVirt 分派错乱/槽位错移"类症状先查 VM 装配日志被跳过的方法（owner/functionAllName 不一致），定位到 Front 导入侧再动手，勿先改 C VM；④本仓库非 git 管理，Trae 编辑器本地历史（AppData\Roaming\Trae CN\User\History\&lt;hash&gt;\entries.json）可考古任意文件改动前基准；⑤遗留观察：IsolateTest 激活后独立 exe 在 `_main_` 完成后进程不退出（worker isolate 线程保活），两份已知问题清单（test/未解决问题.txt、csimple_lang/问题.txt）均未记载，待排查，与 Component 下沉改动无关。

### [2026-09-13] null 实参匹配 Array<T> 形参时调用被静默丢弃：TypeManager IsArray 分支拒绝 null（万物之子语义未覆盖数组）
- 现象：ProtocalBuffersTest 首跑两项异常——`writeBytes(2, null)` 后 hex 为空（连 tag 都没写入）、`newReader(null)` 不抛异常；对照 `writeString(3, null)`（String 形参）正常。IR.txt 显示 null 匹配数组形参的 CallStatic/CallVirt 调用**零指令生成**（静态调用残留孤立 Pop，实例调用连同 receiver 压栈一起消失），编译期无 Error 无 Warning。
- 首个异常阶段：Meta（重载匹配）→ IR（空 visitNodeList 静默产 0 指令）。
- 根因：`TypeManager.CompareFunctionDefineMetaTypeAndInputMetaType` 的 `declaredMt.IsArray()` 分支对非数组实参无条件 `return false`——null 字面量的 MetaType 是 nullMetaClass（IsArray()=false），被静默拒绝，重载匹配失败；而 null 对类形参能走 `ValidateClassTypeRelation` 的「万物之子（Parent）」语义匹配成功，数组形参分支先于该校验执行，形成不对称。失败后 `MetaCallNode.GetFirstNode` 兜底 return true（callNodeType 停留 None）、`CreateCallNode` 忽略 `HandleMetaVariable` 返回值，`MetaCallLink.AddVisitNodeList` if-else 链无 None 分支无兜底 else——节点被完全跳过；IR 层不查 ParsedState，空列表静默产 0 条指令。标准库 Tuple/Stack/Queue/Map 的 `if items == null` 参数防御模式（30+ 处）全部踩在此坑上。
- 修复点（文件/模块）：`source/Front/Core/TypeManager.cs` `CompareFunctionDefineMetaTypeAndInputMetaType` 的 IsArray 分支：非数组实参先判 `argMt.isNull`（MetaType.isNull = metaClass == nullMetaClass），null 允许匹配任意数组形参，与类形参的 null 语义对齐。
- 如何快速复现：`w.writeBytes( 2, null )`（形参 `Array<UInt8> bytes`）后 `w.toBytes()` 为空；或 `f( null )`（形参 `Array<UInt8>`）函数体不执行；查 `out/export/<模块>/DebugCode/<文件>/IR.txt` 无 CallVirt/CallStatic。
- 如何快速验证：`dotnet run --project project\CSimpleVMCoreTest` 跑 ProtocalBuffersTest（`hex bytes(2,null) : OK`、`empty input throws : OK`）+ 全量 BaseTest 零回归。
- 预防建议：①「调用无任何效果且无报错」优先查 IR.txt 里该调用是否存在（零指令 = Front 静默丢弃，meta 层匹配链有吞错误的路径）；②null 语义的匹配规则改动须同步审视全部形参类别分支（类/数组/data/enum），勿只改一处造成不对称；③防御性改进（未做，留待后续）：MetaCallNode 两处静默失败路径补错误上报、AddVisitNodeList 补兜底 else——本条修复前，任何形参-实参类型失配都可能以同样方式静默吞掉调用。

### [2026-09-14] CodecFrameTest 泛型 codec 编译期失败：闭包 ret null 先行锁死返回类型 + data/enum 模板实参 metaClass=null 物化链断裂（G1）
- 现象：P2 codec 组（`Lib/Core/IO/` 下 Utf8Codec/BinaryCodec/JsonCodec/Serialize/ProtoCodec/LengthPrefix 等）落地后，BaseTest 新增 `CodecFrameTest.sl` Front 编译期报错：①泛型类以 data 作模板实参（如 `Codec<CodecUserData, String>`）物化时 Meta 层报 `metaClass.AddInstanceMetaClass MetaClass is Null`；②闭包函数体内 `ret null` 出现在真实类型 `ret` 之前时，返回类型被推断锁定为 Null，后续真实类型 ret 触发 `left compare right` 类型不匹配。
- 首个异常阶段：Meta（泛型物化 + 闭包返回类型推断）。
- 根因：三处叠加。①data/enum 的 MetaType 不携带 `metaClass`（类型信息在 metaData/metaEnum/metaTemplate 上），`UpdateMetaTypeByGenClassAndFunction` 把实参集合按 `List<MetaClass>` 收集传给 `AddInstanceMetaClass`，null 元素直接导致物化失败；②泛型物化缓存比较只比 metaClass，两个不同的 data 实参 `null==null` 被误判同参，物化类撞车复用错实体；③闭包返回类型推断首次遇到带返回值的 ret 即把返回类型从 Void 更新为实际类型——`ret null` 先出现时锁成 Null（bottom type），后续真实类型 ret 无法覆盖。
- 修复点（文件/模块）：`source/Front/Core/TypeManager.cs`——实参集合改 `List<MetaType>`、物化入口换 `AddInstanceMetaTypeClass`，模板约束比较对 `argMt.isNull` 放行；`source/Front/Core/MetaGenTemplateClass.cs`——新增 `m_GenMetaTypeTemplateList` 完整形态记录与比较（metaClass=null 时按 metaData/metaEnum/metaTemplate 恒等判异，杜绝撞车），泛型实体继承父实体虚方法时**槽位与模板类严格对齐**（父方法保序在前、override 移入父槽位、子类独有方法追加尾部——CallVirt 操作数按声明类方法表索引编码，槽位错位即调用错方法，如 `Utf8Codec extends Codec<String,Array<UInt8>>`）；`source/Front/Core/MetaType.cs`——ToString 对 data/enum 实参退回完整 MetaType 形态输出；`source/Front/Core/Statements/MetaReturnStatements.cs`——闭包返回类型推断允许「Null → 真实类型」覆盖（ret null 先出现不定型，null 是 bottom type 可赋任意类型）。
- 如何快速复现：BaseTest 编译 `CodecFrameTest.sl`——`data CodecUserData` 作 `Codec<T,String>` 实参 + 闭包体内 `ret null` 先于 `ret obj` 出现。
- 如何快速验证：`dotnet run --project project\CSimpleVMCoreTest` 跑 CodecFrameTest：A~I 九组 325 check 全 OK；全量 BaseTest 回归 0 FAIL。
- 预防建议：①data/enum 形态的 MetaType 不携带 metaClass，凡按 `List<MetaClass>` 流转模板实参的路径都是雷区，须以 MetaType 完整形态传递与比较；②「先 null 后实体」的推断场景（闭包/多分支 ret）须把 null 当 bottom type 而非定型类型；③泛型实体的虚方法表槽位序必须与模板类一致——继承链上任何重排都会让 CallVirt 索引错位，此问题会在 VM 期以「调用了别的方法」的形态远端爆发。

### [2026-09-15] void 函数 ret 带值 / 非 void 函数裸 ret 编译期零报错：CompareMetaClass 普通 class 分支全注释 + 兜底 return true 放行
- 现象：`static void func(){ ret "aaa"; }` 与 `String func(){ ret; }` 两个方向 Front 均编译通过且零告警，void 与任意类型互斥规则完全失效，void 函数带值 ret 照常产出返回值压栈的脏语义流向 VM。
- 首个异常阶段：Meta（MetaReturnStatements 的 CompareLeftRightMetaType 比对）。
- 根因：`TypeManager.CompareLeftRightMetaType` 对 void 无任何显式处理（object 左值放行/数值窄化链均不覆盖 void），直接落到 `MetaClass.CompareMetaClass` 兜底；而后者 1940-2005 普通 class 继承分支**全部被注释**、2008 行兜底 `return true`——void 与 String 等任意类型比对恒真放行。该兜底不可动：所有非数值类型的赋值/继承比较都依赖它，动它全仓连锁误报。
- 修复点（文件/模块）：`source/Front/Core/Statements/MetaReturnStatements.cs`——非闭包、非 Result 改写分支内、CompareLeftRightMetaType 之前新增 void 互斥专项拦截（函数返回类型 void 且 ret 带表达式 → 21443；函数返回类型非 void 且 ret 裸 → 21444）；`source/Front/Log/LID.cs` 新增两个枚举；`source/Front/Log/ErrorDefinitions.csv` 补两行 Error 阻断级模板。安全性：闭包（isClosureFunction 推断分支）、`_init_` 构造函数（显式 void）、setter、`_main_`/`_test_` 入口、Result 函数（hasResultVariable 改写分支）均不走该比对分支或本就是 void，零误伤。
- 如何快速复现：任一工程写 `static void func(){ ret "aaa"; }`——修复前编译通过零报错。
- 如何快速验证：临时负例工程（void+带值 ret / String+裸 ret / void+裸 ret / String+带值 ret 四函数）——前两函数精准报 Error（位置 sl_5_0 / sl_9_0），MetaCore Failed → IR/Export Skipped 不生成 module.json，后两正例零误报；BaseTest 71 文件全量编译零新增 Error；C VM 全量回归 EXITCODE=0。
- 预防建议：①`CompareMetaClass` 末尾的 `return true` 是「宽松放行」语义，任何经它比出「类型兼容」结论的调用点都要警惕——void/never 等特殊类型必须在调用点先行专项拦截；②新增类型检查规则优先在单一语句语义层（如 MetaReturnStatements）局部拦截，勿动全局比较器兜底；③负例验证须独立临时工程——正例用例集（BaseTest）塞负例会阻断整包编译污染回归基线，验证完即删源文件与 out 产物。

### [2026-09-15] StreamFileTest A1 length FAIL：`.sl` 用了 `else if`，词法只认 `elif`，分支静默失效
- 现象：ExpendTest/StreamFileTest A 组 `A1 length : FAIL`（`fr.length == 17` 不成立），A2 position 却 OK。
- 首个异常阶段：Front 词法层——但零报错，错误静默带入运行期才显形。
- 根因：`Front/Lib/Std/IO/FileStream.sl` 实现里写了 `else if`；SL 词法/语法只支持 `elif`，`else if` 组合被错误切分，length 相关分支未按预期命中。
- 修复点（文件/模块）：`FileStream.sl` 改 `elif`，重编标准库（CompileCore/CompileStd）。
- 如何快速复现：任一 `.sl` 中写 `else if`，编译运行观察分支不进且无报错。
- 如何快速验证：A1 `length == 17` 转 OK。
- 预防建议：`.sl` 控制流只有 `elif`（见 `md/syntax/if` 篇）；排查「分支不进但无编译错」时先扫 `else if`。词法对非法组合静默容忍属隐患，Front 可考虑加告警。

### [2026-09-16] StreamFileTest E1 后 OpCode_Pop 栈下溢：`try voidFunc()` 表达式语句漏 CalcReturnType，IR 层把 null 当非 Void 误发 Pop
- 现象：`E1 dual hex : OK` 后 `[Assert] OpCode_Pop could not consume stack value. idx=0`，__debugbreak 崩溃（CSimpleVMStdTest / ProjectTest2）。
- 首个异常阶段：**IR 生成期（Front）**，非 VM bug——VM 侧插桩（R12）先定位到 __closure_f_1 第 69 条指令 Pop 在 depth=0 下溢，反查 Core.module.json 见 CallStatic `drainFrames`（void）后跟多余 Pop。
- 根因（链）：
  1. `IO/LengthPrefix.sl:234` `try LengthPrefix.drainFrames<T>(...)`（static 模板 void 函数 + try 包装）走 MetaCallStatements **表达式语句路径**（`fmcl.expressTerm != null`）；
  2. 该路径 `men.Parse(...)` 后**未调 `men.CalcReturnType()`**（对照：调用链路径 L62-72 用函数真实返回类型判断，无此依赖）；
  3. `MetaTryExpressNode` 未重写 `GetReturnMetaType()`（全仓仅 Calllink/TypeConvert/ExecuteStatements 三处重写，其余节点靠 CalcReturnType 固化 `m_ExpressReturnMetaType`）→ 返回 null；
  4. `IRCallStatements` 判 `expType?.metaClass?.eType != EType.Void` → `null != Void` 恒 true → void 调用后误发射 IRPop；
  5. VM 对 void 返回不压栈 → Pop 下溢断言。
- 修复点：`MetaCallStatements.cs` 表达式路径 Parse 后补一行 `men.CalcReturnType();`（与 Return/If/While/TryCatch/DefineVar 等所有语句路径的模式一致，是根因修复而非只补 try 节点）。
- 如何快速取证：VM 侧插桩拿闭包 methodId+指令号 → module.json 反查指令序列 → DebugCode IR.txt 确认 CallStatic(void)+Pop 相邻即 Front 生成错。
- 如何快速验证：重编 Core 标准库 + 跑 ProjectTest2 全量 exit=0；StreamFileTest 19 OK / 0 FAIL（E1 之后 E2/F/G 全过）；ComponentTest 仅剩既有 tryGetComponent FAIL（无关）。
- 预防建议：凡构造表达式语句类 MetaStatements，Parse 后必须 CalcReturnType 固化类型；下游消费 `GetReturnMetaType()` 不得把 null 当「有值」处理（null≠Void 的写法对未计算类型必误判）。
- 附带修复：`vm_runtime.c` 三处栈断言日志（StoreGlobal/Dup/Pop）的 `uidx` 声明后从未赋值恒打 0，已改为真实 `execute_index-1`；本次靠插桩拿到 methodId 才没被 idx=0 误导，修掉后同类问题可直接看日志定位。

### [2026-09-16] InlineLambdaTest 内联 lambda 展开体重复翻译（组合表达式 r8=600 应 330）：TryParseInlineLambdaCall 多 Add 一次 m_MetaCallNodeList
- 现象：9 用例中 8 个数值正确，仅用例 8 `add(10,20) + add(100,200)` 输出 600（应 330）；IR.txt 中每个调用点展开体（含定义行号的 Add）完整出现两份，一份被 StoreLocal 消费、一份残留栈上——简单语句残留值不干扰输出（碰巧正确），同语句内组合表达式栈布局错乱才暴露。
- 首个异常阶段：**IR 生成期（Front）**。定位法：① IR 形态观察（`[sl:16] Add` 出现两次）；② 加同构对照组（`Int32 c4 = (10+20)+(100+200)` 普通表达式单份且 330 正确）排除语言固有行为/VM 因素；③ 栈推演定量验证（`[30,30,300,300]→Add→[30,30,600]` 与输出吻合）。
- 根因（链）：
  1. `MetaCallNode.TryParseInlineLambdaCall()` 末尾有 `m_MetaCallNodeList.Add(this)`；
  2. 而调用它的 `ParseNode()` L301 `CreateCallNode()` 返回 true 后，L304 **又统一 Add 一次** → 同一节点入链两次；
  3. `MetaCallLink.Parse()` L211 `newList.AddRange(m_CallNodeList[i].metaCallNodeList)` 展平 → visitNodeList 出现两个 Express 节点；
  4. `IRMetaCallLink.ProcessVisitNodeList` 逐节点 `ExecOnceCnode` → 展开体翻译两遍。
- 修复点：删除 TryParseInlineLambdaCall 内的 Add，ParseNode L304 统一添加。括号表达式先例（L274-298）自行 Add 不重复是因其 L296 提前 return 跳过 L304——分派链内创建 Express 的路径**不能**自行 Add。
- 同特性早前三个 bug（简记）：bug1 展开体形参替换后 BuildAST 旧 root 残留（改 `ReplaceInlineLambdaParamInTerm` out bool changed + 发生替换层就地 BuildAST 自底向上重建）；bug2 同 bug1 修复内；bug3 内联变量漏进局部变量槽（`MetaBlockStatements.GetCalcMetaVariableList` 过滤 InlineLambda）。
- 预防建议：在 CreateCallNode 分派链内部新增路径时，先确认返回后 ParseNode 是否有统一收尾动作（Add/校验等），避免与先例（走提前 return 的路径）表面一致实则重复执行。

### [2026-09-16] 内联 lambda 负例 6（try?）零报错漏检：try?/try! 走 AddSymbol 生成 Symbol 节点，扫描器 Key 分派够不到
- 现象：负例工程 6 条中 5 条命中预期 LID（21449×3 / 21452×2），唯负例 6 `var f6 = ( x ) => try? safeThrowsFn()` 无任何日志——拦截器对 try? 完全失明。
- 首个异常阶段：**MetaCore（内联体限制扫描）**。定位法：① Node.txt 快照是扁平 token 流无树结构，看不出节点类型；② 读 StructParseToSyntax 确认 bodyNodeList 组装无误；③ grep 全仓 TryQuestion 的消费点。
- 根因（链）：
  1. `TokenParseToNode.cs` L595-596：`TryQuestion`/`TryExclamation` 走 `AddSymbol`（与 `!`/`~` 同路径，priority Level2_LinkOp）→ 生成 **Symbol 节点**；
  2. 而 `try`/`catch`/`finally`/`defer` 等走 `AddKeyNode`（L737-743）→ **Key 节点**；
  3. 扫描器只按 `nodeType == Key` 分派检查 Try 系 token → Symbol 形态的 try?/try! 永远命中不了。
  4. 本质：`try?`/`try!` 是**表达式前缀运算符**（try.md §1，任意表达式位置可用），词法归类与块级 try 同族、语法树归类却与一元运算符同族。
- 修复点：`MetaInlineLambdaDefineStatements.cs` 扫描器新增独立 Symbol 分支（`nodeType == Symbol && token.type ∈ {TryQuestion, TryExclamation}` → ExceptionFrame）。第一版错误示范：直接把这两个 token 加进 Key 分支的 token 检查——无效，因节点类型根本不是 Key。
- 如何快速复现：编译 `test/Other/InlineLambdaForbiddenTest/ProjectTest.sp`，负例 6 行无 21451。
- 如何快速验证：同命令重跑，`MetaCoreInlineLambdaBodyForbiddenExceptionFrame` 命中「表达式前缀 try?」；正例 `InlineLambdaTest` 回归 C VM 10/10 不受影响。
- 预防建议：基于语法树节点写检查时，先到 TokenParseToNode 确认目标 token 走 AddKeyNode 还是 AddSymbol——**词法同族的 token 语法树节点类型未必同族**（try vs try? 即反例）。

### [2026-09-16] InlineLambdaTest 用例 6 `inc(inc(41))` 误报互递归（21452）：外层展开 Parse 递归解析内层实参调用时展开栈未区分「实参来源 vs 体重建来源」
- 现象：正例工程 L42 `inc(inc(41))` 命中 `MetaCoreInlineLambdaBodyForbiddenSelfReference` 误拦，编译失败；简单调用（`inc(41)`）正常。
- 首个异常阶段：**MetaCore（内联展开互递归拦截）**。定位法：只在「调用点出现在另一内联调用的实参位置」时触发，直接读 `TryParseInlineLambdaCall` 展开栈逻辑。
- 根因（链）：
  1. 外层 `inc(inc(41))` 展开：步骤 1 重建体 term → 步骤 5 `ExpressManager.Parse` 解析替换后的表达式；
  2. 解析过程中**实参位置的**内层 `inc(41)`（不是体重建来源，是调用点原始 term 子树）再次进入 `TryParseInlineLambdaCall`；
  3. 此时外层 ilv 仍在展开栈上 → `IsExpanding(ilv)` 为真 → 误报互递归。
  4. 本质：**实参位置嵌套是有限展开（合法），体位置引用才是无限递归（非法）**，拦截只看栈命中区分不了两者。
- 修复点（对象身份标记方案，`MetaInlineLambdaVariable` + `MetaCallNode.cs`）：
  1. 新增 `s_ArgSourceCallNodes`（`HashSet<FileMetaCallNode>`）：体每次展开经 `CreateFileMetaExpress` 重建**全新 term 对象**（身份绝不在集合），实参是调用点原始 term（步骤 1.5 收集进集合）；
  2. 步骤 1.5 收集实参子树全部 `FileMetaCallNode` 身份（遍历形态与 `ReplaceInlineLambdaParamInTerm` 对齐，额外覆盖下标/花括号初始化器/as-is 左值等 Parse 能到达的结构）；
  3. 拦截条件精化为「栈命中 **且** 不在实参集合」才报 21452；
  4. 身份链依据：`MetaCallLink` 构造器直通持有原始 `FileMetaCallLink` → `CreateCallLinkNode` 用原始 `FileMetaCallNode` 构造 `MetaCallNode`（term 树无父指针，只能靠对象身份）。
- 如何快速验证：正例 `InlineLambdaTest` 编译 0 错误 + C VM 10/10（`inc(inc(41)) = 43`）；负例 `InlineLambdaForbiddenTest` 6 条拦截不受影响。
- 预防建议：term/AST 节点无父指针时，「节点来源区分」可用对象身份（HashSet 引用相等）而非回溯；前提是两来源的构造路径保证身份隔离（重建 vs 复用）。

### [2026-09-16] InlineMethodTest 新增类字段无初始化表达式报 MetaCoreExpressIsNull ×12：误导 extendMessage「传入参数与要求参数不对应」实为 SL 类字段必须带初始化表达式
- 现象：m1b-5 扩充 inline 测试用例时新增 Vec2 类（字段写 `Int32 x` / `Int32 y`，无初始化表达式），编译报 12 处 `MetaCoreExpressIsNull Position:[InlineMethodTest.sl_82_0]` 等，extendMessage 一律为「express传入参数与要求参数不对应!」，字面极易误导向参数数量/类型不匹配排查。
- 首个异常阶段：Meta（MetaMemberVariable 语义表达式生成）。
- 根因：SL **类字段声明必须带初始化表达式**（语言规则）——`MetaMemberVariable.cs:474` 处 `m_Express == null && m_FileMetaMemeberVariable != null`（File 层有源定义但 Meta 层未生成表达式）触发 LID 11009「没有生成成功表达式:{0}」。extendMessage 是 Log 层在 LID 模板参数个数不匹配（11009 需 1 个参数、调用传 0 个）时的通用兜底文案，与真实根因无关；grep 全 test 树验证既有用例类字段全部带初始化表达式，仅本次新加两行违例。
- 修复点：无代码修复；测试用例字段改为 `Int32 x = 0` / `Int32 y = 0`。
- 如何快速复现：任一 .sl 类体内写无初始化表达式的字段声明（如 `Int32 x`）后 `compile -p <sp> -e ir`。
- 如何快速验证：补 `= 0` 后重编译 0 错误、日志出现 `export module success`，C VM 16 项输出全对。
- 预防建议：①SL 类字段声明必须带初始化表达式（对比 C#/Java 可省略，SL 不可）；②见「传入参数与要求参数不对应」extendMessage 时先对照 `Front/Log/ErrorDefinitions.csv` 查该 LID 的 paramCount 与实际传参个数——该文案是参数数量兜底，非错误本体，勿被带偏；③排障仍守 R8：只看最早一条 Error 的 LID 本体（11009=表达式未生成），extendMessage 仅作参考。

### [2026-09-16] inline 方法负例 3（变量初始化器调用）零报错漏检：AllowUseSettings 克隆构造漏拷 parseFrom，解析来源标记在 MetaCallLink 克隆后丢失
- 现象：新增 M2+ 三条形态限制中，负例 1（体 > 10 条，21453）与负例 2（inline+throws，21457）正常命中，唯负例 3（字段初始化器 `Int32 fieldBad = baseVal()` 调用 inline 方法，21458）无任何日志——检查代码位于 `MetaCallNode` 的 `m_IsFunction` → MemberFunctionName 出口，理论上裸名调用必然经过。
- 首个异常阶段：**MetaCore（语义层调用解析，MetaCallNode）**。定位法：沿调用链反向追踪——`MetaMemberVariable.ParseMetaExpress` 确实传 `new AllowUseSettings() { parseFrom = EParseFrom.MemberVariableExpress }` → `MetaCallLinkExpressNode.Parse` → `MetaCallLink.Parse` L170 `allowUseSettings = new AllowUseSettings(_useConst)`（**克隆**）→ `MetaCallNode.ParseNode(allowUseSettings)`，逐层检查 auc 传递。
- 根因（链）：
  1. `MetaCallLink.Parse` 对传入的 auc 做**克隆**后再分发给各 MetaCallNode（克隆本体是合理的——setter/getter 按节点位置覆写，不污染调用方设置）；
  2. 但 `AllowUseSettings` 克隆构造（`MetaCallNode.cs` L79-89）**逐字段拷贝时漏了 `parseFrom`**（且它是属性而非公有字段，容易漏）；
  3. 于是「字段初始化器上下文」标记在进入 call link 后被重置为默认 `None` → 依赖 `parseFrom == MemberVariableExpress` 的全部检查失明——不止新加的 21458，**既有 `this`/`base` 关键字在初始化器中的禁用检查（L627/L688）同样从未生效过**（属顺带修复的潜伏缺陷）；
  4. 旁证：`MetaParam.cs:61` 克隆后**显式重设** `parseFrom = InputParamExpress`——此前已有人踩过同坑并绕过，未修根因。
- 修复点（文件/模块）：`MetaCallNode.cs` `AllowUseSettings(AllowUseSettings clone)` 构造补 `parseFrom = clone.parseFrom;`（附注释说明透传语义与消费方）。
- 如何快速复现：类字段初始化器写 `Int32 f = someInlineMethod()` 或初始化器中使用 `this.x`，编译——修复前零报错直接放行。
- 如何快速验证：负例工程重编译，`MetaCoreInlineMethodCallVarInitForbidden`（21458）命中；正例 `InlineMethodTest` / `InlineLambdaTest` 编译 + C VM 运行全对；`CSimpleVMCoreTest`（BaseTest 全量）回归通过。
- 预防建议：①克隆/拷贝构造逐字段核对时，**属性与公有字段分开扫**（本例 parseFrom 是属性，肉眼对字段清单会漏）；②「上下文标记」类字段的透传断裂是静默失败（无报错、仅检查失明），新增依赖上下文的检查前先写一个最小负例证明标记真的到达了检查点；③grep `new AllowUseSettings(` 可快速审计所有设置重置点。

### [2026-09-17] 新写 Std/IO/Path.sl 全文件 11 错：无大括号 if 体被条件表达式吞并 + `else if` 非法（只认 `elif`）
- 现象：`CompileStd` 编译新增的 `IO/Path.sl` 报 10 条 `FileExpressFormatError`（在 `==`/`||`/`>` 的表达式不合法）+ 3 条 `NodeStructParseElse`（不允许在Else后增加任何代码）+ 1 条 File 解析 NPE，其余 34 文件全过。
- 首个异常阶段：File（表达式）/ Node（else 结构）。
- 根因（链）：
  1. SL 的 if/elif/else **分支体必须 `{}` 包裹**（见 md/syntax/if.md）：无大括号时 if 条件表达式不在行尾终止，会把下一行体的 token（`ret "..."` / 赋值）继续吸入同一扁平表达式序列，随后在 `==`/`>`/`||` 处报「表达式不合法」——报错点显示的 Extend 正是**下一行体的 token**（判定特征）；连吸多行后在 Node 层触发 NPE；
  2. 链式分支关键字是 `elif`，**`else if` 不存在**——`else` 后只允许 `{`，故报「不允许在Else后增加任何代码」；
  3. 反向排除：`"\\"` 反斜杠转义、`SystemPlatformEnvGetInt("os") == 1` 内联比较、`for i = 0, i < n, i++`、`continue`、行尾 `#` 注释均有先例/实现（ReadString case '\\'、Environment.sl:153 等），并非根因。
- 修复点（文件/模块）：`simple_language/source/Front/Lib/Std/IO/Path.sl` 全部 if/elif/else 体补 `{}`；`else if` 链改 `elif`。
- 如何快速复现：在 .sl 方法体里写无大括号 `if cond\nret x` 或 `else if cond`，编译即得同类错误。
- 如何快速验证：`CompileStd` 全阶段 Completed 且 `export module success`；`Std.module.json` 中出现 `IO.Path` 全部 17 成员（4 静态属性 + 13 方法）。
- 预防建议：①新写 .sl 前先扫 md/syntax/if.md 与同库既有文件的分支写法，分支体一律带 `{}`；②见到「在 XX 的表达式，不合法」且 Extend 指向**下一行 token**，第一嫌疑即无大括号 if 体被吸并，勿先怀疑运算符/字面量；③SL 无 `else if`，链式一律 `elif`。

### [2026-09-18] EncodingTest 12 错「方法未找到」实为跨模块枚举返回类型被导出为 Core.Member：IRMetaVariable 枚举槽位包装规则漏豁免方法返回变量
- 现象：`CSimpleVMStdTest` 编译期报 13 条错——每行 `Encoding.nameFromCode( Encoding.codeFromName( "utf-8" ) )` 产生一组：`MetaCoreTypeNotEnumMember`（无 Position，第一现场）+ `MetaCoreParseCallNodeNotFoundInClass 节点[nameFromCode]没有在类[Std.Encoding]被发现`（表象）+ CallNotSupport 连锁。
- 首个异常阶段：MetaCore（实根在 Export——上一轮编译产出的 Std.module.json）。
- 根因（链）：
  1. 错误定位勿信表象：「节点没在类中被发现」是重载匹配失败的连锁 Warning，第一现场是 `TypeManager.CompareEnumMetaType` 报 `MetaCoreTypeNotEnumMember`——形参（枚举 EncodingCode）vs 实参（普通类 Member）；
  2. 实参类型来自内层 `codeFromName` 的**返回类型**：跨模块导入侧 `ProjectReferenceModuleLoader.BuildMetaMemberFunctionFromIR` 用 `returnList[0].typeDef` 复原方法返回 MetaType（唯一来源）；
  3. 导出侧 `IR/Core/IRMetaVariable.cs` 有意规则：枚举类型变量（运行时槽位统一 Core.Member 包装）导出 Member，**仅形参豁免**（形参类型是 functionAllName/跨模块重载决议的签名组成部分）——但**方法返回变量（`MetaVariable.EVariableFrom.None`，仅 MetaFunction/MetaMemberFunction 两处创建）漏在豁免外**，被同规则打成 Member；
  4. 于是导入侧把返回类型复原为 Member 普通类 → 外层 nameFromCode 形参枚举匹配失败 → 「方法未找到」。佐证：Std.module.json 中 `"className": "Member"` 出现 82 次（所有枚举返回方法系统性受损，既有测试无跨模块枚举链式调用故未暴露）；参数位置的枚举 typeDef 一直正确。
- 修复点（文件/模块）：`simple_language/source/Front/IR/Core/IRMetaVariable.cs` 枚举 Member 包装的条件加 `&& mv.variableFrom != MetaVariable.EVariableFrom.None`——返回变量与形参同理保留枚举类型（签名组成部分）。VM 侧无需改动：返回槽位与参数槽位走同一 `vm_alloc_init_runtime_object_row` 初始化，枚举类名与 Member 都落非标量 hint（EVMType_Class），行为一致。
- 如何快速复现：库模块暴露「返回枚举的方法 f」+「形参为该枚举的方法 g」，另一模块写 `g( f(...) )` 链式调用，修复前必现 MetaCoreTypeNotEnumMember + 方法未找到。
- 如何快速验证：重编译 Std 后 Std.module.json 中 `codeFromName` 的 `returnList[0].typeDef.className` 由 `Member` 变为 `EncodingCode`；`CSimpleVMStdTest` 全绿（EncodingTest ~113 项 + Base64 39/39 无回归）。
- 预防建议：①跨模块「方法未找到」但方法确实存在时，先 diff module.json 中该方法的 argumentList/returnList typeDef 与声明——导出数据的类型错位会在消费端伪装成各种匹配失败；②「运行时包装类型」与「签名类型」是两个语义槽：凡进入方法签名（形参/返回值）的类型必须保留声明原貌，Member 包装仅限纯槽位（局部/静态字段）；③无 Position 的 MetaCore 错误多为深层类型比较的第一现场，比带 Position 的「节点未找到」Warning 更可信。

### [2026-09-18] 新增「变量名/参数名禁止关键字」校验：两轮漏报与一轮误伤
- 现象：按要求在 Front 解析层增加变量定义名称不得为关键字的报错（LID 20287 `NodeStructParseNameIsKeyword`）。首轮实现后① `string string = "x"`、`f( string string )` 漏报；② 随后扩条件后标准库（Boolean/UInt8/Int8…）大面积误伤 `this._value = b`。
- 首个异常阶段：Node/File（`CrateFileMetaSyntaxNoKey` 与 `FileMetaParamterDefine`）。
- 根因：① 词法层 15 个基本类型词（int/string/object/bool/float/double/byte/sbyte/short/ushort/uint/long/ulong/half）统一映射 `ETokenType.Type` → Node 层走 `AddIdentifier` 生成 **IdentifierLink 节点而非 Key 节点**，`nodeType == ENodeType.Key` 判不中；② `this/base/local/global` 是 Key 节点但属**限定前缀关键字**，在 `CrateFileMetaSyntaxNoKey` L737 被合法收入 defineNodeList（`this._value = b` / `global.x = 5`），兜底校验一刀切误伤。
- 修复点（文件/模块）：`StructParseToSyntax.cs` 兜底校验扩展为 `nodeType == Key || token.type == ETokenType.Type`，并对 this/base/local/global 加前缀豁免（`GetLinkTokenList().Count > 1` = 后跟成员访问链 = 前缀用法放行；链长 1 = 单独作名仍报错）；`FileMetaMemberFunction.cs` 参数名校验同步加 Type 条件。另注意：SL 行内注释是 `#! … !#`，写 `//` 会被当除法符号报错（临时用例曾因此干扰验证）。
- 如何快速复现：临时工程写 `var new = 5` / `string string = "x"` / `var int = 5` / `int global = 5` / `f( int if )` / `g( string string )` 编译，应全部报 20287；`var ok = 1` / `h( int okName )` 不得报。
- 如何快速验证：CompileCore/Math/Std 三标准库全绿（`export module success`）；`dotnet run --project project\CSimpleVMCoreTest` 端到端零失败（含 this._value 赋值路径）。
- 预防建议：关键字相关校验勿只按 `ENodeType.Key` 判断——词法层把类型词折成 `ETokenType.Type` 的 IdentifierLink、this/base/local/global 是合法前缀 Key，三类语义各不相同；新校验上线必跑标准库回归，"合法用法误伤"往往比"非法用法漏报"更早暴露。

### [2026-09-19] 行尾 `#` 注释吞 LineEnd：语句隔离失效，watchAssert 实参被下一行 `if (` 的 ParNode 覆盖丢失
- 现象：Debug 系统 P3（watchAssert）验收时，带行尾注释的调用语句实参整棵丢失——`Debug.watchAssert( 100, "ax", 100 >= 50 )` 行尾带 `# 注释`、下一行是 `if (…)` 时，watchAssert 的实参在 IR 中消失/错挂，断言行为与源码不符；去掉行尾注释立即恢复正常。
- 首个异常阶段：Node（Token 层数据已缺失，建树层放大为语句结构损坏）。
- 根因：词法层 `ReadSharp`（LexerParseToToken.cs）行注释分支（遇 `\n` 结束）连行尾换行一起消费（m_Index++/m_SourceLine++），**该行因此没有 LineEnd token** → 建树层依赖 LineEnd 执行的语句隔离清理（`SetIdentifierNode(null)` + Angle 复位）不执行 → identifierNode（watchAssert 调用节点）残留到下一行：下一行 `if (` 的 `(` 触发 SetParNode 覆盖该节点已有 ParNode，**原实参（100/"ax"/条件）整棵子树孤儿丢失**，if 条件被误接为调用实参。
- 修复点（文件/模块）：`TokenParseToNode.cs` `case ETokenType.Sharp` 分支——行注释（extend=="0"）镜像 LineEnd 分支的清理动作（Angle 内先 RestoreAngleNode，再 `SetIdentifierNode(null)`）恢复语句隔离；块注释 `#!…!#` 可出现在行中，保持原行为不动。
- 如何快速复现：`.sl` 写 `foo( a )  # 行尾注释` 且下一行以 `(` 开头（如 `if (…)`），编译后看 DebugCode IR.txt——foo 的实参丢失。
- 如何快速验证：DebugTest P3 watchAssert 段（多条语句带行尾注释紧跟 if）四断言全 OK；CompileCore/Math/Std 三标准库回归全绿。
- 预防建议：①「行 = 语句隔离单元」是 Node 层隐含契约，词法层任何吞行尾换行却不补发 LineEnd 的入口都会破坏它——新增此类入口时必查 Node 层 LineEnd 分支的副作用清单并镜像；②排障遇「语句边界类」怪象（实参丢失/跨行错挂）先查 DebugCode Token.txt 里该行有没有 LineEnd。

### [2026-09-19] watchIn 与 M4 自动内联交互：optimizeLevel 默认=1，static 小方法（≤2 语句）被内联展开，调用链上无本方法帧
- 现象：上述 callee_method 修复后，按方法名匹配的 watchIn（`Debug.watchIn( 5, "wix", "inTarget" )`）仍零命中——调用链上遍历不到 "inTarget" 帧；而 ProjectTest.jsonc 明明写着 `"optimize": false`，一度据此排除内联假设（误导项）。
- 首个异常阶段：IR（DebugCode IR.txt 中 DebugWatch mode=3 三处发射正常，紧邻 `[Nop] info:[inline end: inTarget]`——方法体已被内联展开进调用方）。
- 根因：`optimizeLevel` 在 `CommandInputArgs.cs` **默认 = 1**（CLI 不传 -O 时；jsonc 的 `"optimize": false` 与它无关）→ `ProjectCompile.cs` AutoInlineMark 取 maxStatementCount=2 → 辅助方法 inTarget() 恰 2 条语句且 static → 被标记自动内联、IR 展开进 fun → 运行时调用链上没有 "inTarget" 帧 → 方法名匹配必失败。AutoInlineMark 排除清单中 `!mmf.isStatic`（实例方法）是**硬排除项**。
- 修复点（文件/模块）：`test/BaseTest/DebugTest.sl`——inTarget/outOther 两辅助方法去 static 改实例方法，调用点改实例调用（`DebugTest dt = DebugTest(); dt.inTarget(); dt.outOther();`）。
- 如何快速复现：static 且 ≤2 语句的方法内放 `Debug.watchIn( n, "label", "方法名" )`，默认参数编译，IR.txt 搜 `inline end:`。
- 如何快速验证：DebugTest `wix hits=1`、「watchIn samples only on target chain」OK。
- 预防建议：①涉及运行时帧/调用链的测试辅助方法勿用小 static 方法（会被 M4 自动内联，帧不在链上），实例方法是硬排除项可保帧；②jsonc 的 `optimize` 字段**不控制** optimizeLevel（CLI 默认 1），排查内联相关问题先看 CommandInputArgs 默认值与 IR.txt 的 inline 标记，勿被 jsonc 字段误导。

### [2026-09-24] SpecialTest 宿主 Front 编译 "not find owner type, error count: 2"：jsonc data 段合成 MetaData 无源文件绑定被 WIP 改成 Error 阻断 IR 阶段
- 现象：`dotnet run --project project\CSimpleVMSpecialTest`（及 VS 内跑宿主）报 `Front compile failed, error count: 2 / exit code: 1`；Front.txt 最早 Error = `[ParseMeta][MetaCoreAssertShowMessage] Extend:[not find owner type]`，随后 `[ProcessPhaseFailed] 编译阶段[IR]失败`、Export Skipped。csproj 的 C# 编译本身 0 错误——报错全部来自宿主 in-process 内嵌 Front 编译。
- 排查路径：CLI `dotnet build` Debug/Release 双绿 + IDE 诊断空 → 定性为运行期内嵌编译失败（R8：只看最早一条 Error）→ Front.txt 时间戳 12 小时制（08:30 = 20:30）与文件时间对照锁定运行 → git diff 取证：else 分支报错是当晚 20:22 未提交 WIP 新增（HEAD 旧版为 try/catch 静默容忍），20:03 旧版曾编译成功 → 按 R12 在 else 分支临时插桩 owner 身份（clrType/allName/classId）复跑一次即取证：`clrType=MetaData allName=___ProjectGlobalData_vardata2___`。
- 根因：`.jsonc` 的 `data` 段对象值（如 ProjectTest.jsonc 的 `vardata2: {a:10, b:20.1}`）由 `PorjectClass.CreateMetaDataByJsonObject` 合成为 `___ProjectGlobalData_<名>___` 的 MetaData（L394/L962），**不从 .sl 源文件解析、天然无 boundFileMetaClass**——IRMetaClass.InitFromMetaOwner 的 else 分支被其正常命中，属设计使然而非 bug；WIP 把容忍改成 Error（LID.MetaCoreAssertShowMessage，pass=false）后 errorCount=2（根源+阶段连锁），IR 阶段失败。
- 修复点（文件/模块）：新增专用 Warning 级 LID `MetaCoreIRMetaClassNoSourcePathBinding=12288`（LID.cs + ErrorDefinitions.csv 三处同步）；IRMetaClass.cs else 分支 Error→Warning（消息带 clrType/allName 身份，注释说明合成类型场景）——不阻断编译且保留可见性。
- 如何快速复现（修复前）：工程 jsonc `data` 段含对象值 + IRMetaClass else 分支记 Error → `compile -e ir -p test\SpecialTest\ProjectTest` 报 error count: 2。
- 如何快速验证（修复后）：同命令 → `[Warning][MetaCoreIRMetaClassNoSourcePathBinding] ... allName=___ProjectGlobalData_vardata2___`、IR [Completed]、module.json 导出成功；`dotnet run --project project\CSimpleVMSpecialTest` → Front 编译 0 error + C VM 20 PASS/0 FAIL + exit 0。
- 预防建议：① 「无源文件绑定」≠异常：jsonc data/global 段合成类型、模板实例等本就无 boundFileMetaClass，防御性兜底分支应记 Warning 而非 Error；② 重构吞异常的 try/catch 前先想清楚该分支是否会被正常路径命中（旧版 catch 静默 = 作者已知会有此场景）；③ 新增 LID 三处同步：LID.cs 枚举 + ErrorDefinitions.csv 行（logType 决定计数走向，Warning 只进 warningCount）+ 调用点。

### [2026-10-02] "系统函数找不到"双结论：探针 namespace 同名坑致误诊；调用链解析失败仅 Warning 静默丢弃（升 Error 连带暴露 ExpendTest 存量裸名实例成员）
- 现象/任务：用户报告 `SystemMemoryMimallocCollect` 系统函数"找不到、直接报空错误"。排障三步走：① 原始问题在当前代码库**不复现**——MiTest（mimalloc host allocator）13 项全 PASS；② 第一轮探针曾误诊"语句位裸系统调用被 Front 静默丢弃"，推翻——实为探针工程 namespace/类名同名配置坑；③ 真病灶是**调用链解析失败仅产 Warning 不阻断**，语句从 IR 无声消失（语句位裸系统调用本身不丢弃：编入 IR 且 VM 正常执行，3 行 println 全输出、exit 0）。
- 首个异常阶段：误诊的"丢弃"是探针配置假象；真病灶在 MetaCore 阶段——`MetaCallNode` 调用链解析失败 → Warning 11020/11021 → errorCount 不增 → 后续 IR/Export 照常 → 带病 module.json 照常导出，运行期表现为语句静默消失。
- 根因：① 探针 .jsonc 中 `namespace` 与导出模块名同名，系统函数所在类名解析被劫持到错误实体，产生"函数找不到"假象（与系统函数注册无关）；② 调用链失败路径（`MetaCallNode.cs` 首节点裸名解析：实例成员裸名只允许 isStatic 或 local{} init 上下文，否则 return false）仅产 Warning，病态代码与合法语法同流、运行期无声丢弃；③ 存量用例 `AttributeNicknameTest.sl`/`AttributeTest.sl` 构造器裸名实例成员访问（`m = 10;` 无 this.）正是此类带病代码，一直靠 Warning 静默"假过"。
- 修复点（文件/模块）：① `source/Front/Log/ErrorDefinitions.csv` 11020/11021 升 Error（errorCount++ → MetaCore Failed → IR/Export Skipped → module.json 不导出）；② `source/Front/Core/Statements/MetaCallStatements.cs` expressTerm 分支补兜底日志（errorBegin/warningBegin 差值防重 + LID.MetaCoreParseCallLinkFailed）；③ `test/ExpendTest/AttributeNicknameTest.sl`、`AttributeTest.sl`：类体 `m = 10;`→`int m = 10;`、构造器 `m = 10;`→`this.m = 10;`（按 class.md 语法权威修正存量病态用例）；④ `scripts/common.ps1` TestMap ExpendTest 补 NoteWin/NoteLinux（Sqlite3Test 连接串 `Resources/ttest` 相对 cwd，脚本 cwd 下运行 8 项 FAIL 属环境问题，`test\ExpendTest` cwd 下全 PASS）。
- 如何快速复现：① 真病灶——写一条调用链失败语句（如 `NotExist.foo(1);`），旧版仅 Warning 且语句无声消失；② 误诊坑——探针 .jsonc 的 namespace 与模块名同名时复现"系统函数找不到"假象。
- 如何快速验证：① 病态语句编译即报 Error 11020/11021 且 module.json 不导出（Export Skipped）；② 合法裸调用+包装调用探针 VM 运行 3 行 println 全输出、exit 0（f2 对合法代码零影响）；③ `compile-tests.ps1 -Tests BaseTest,ExpendTest` 双绿；④ ExpendTest 运行 exit 0，MiTest 13 项全 PASS（原始问题不复现的直接证据）。
- 预防建议：① 探针工程 .jsonc 的 namespace 勿与模块名/类名同名（解析劫持产生假故障），探针结论须在既有工程做对照验证再定性；② 调用链失败的 Warning 11020/11021 = 语句将被静默丢弃的前兆，现已升 Error 阻断——旧工程若因此新报错，按语法修正调用链而非降级回 Warning；③ Sqlite3Test 类相对路径用例运行须在 `test\ExpendTest` cwd（TestMap 已记 note）；④ `Define.cs` `ESystemMethodCall` 缺 mimalloc 枚举项（systemMethodKind=-1 走 id 快路径不受影响）——若用户环境仍复现"空错误"，先确认其本地 csimple_lang VM 二进制是否为最新构建。

### [2026-10-02] const 关键字扩展落地：三类"天然常量"误伤与 GetStoreMetaVariable 缺口、return/ret 静默坑
- 现象：const 赋值拦截 + 传参约束（用户需求四点）在 ConstProbe 探针 5 场景全过之后，标准库回归 Std/Math/BaseTest(ExpendTest) 依次大面积误伤：`FileStream(path, FileMode.Read)`（enum 实参）、`Mathd.min(x, BigNumber.CAPACITY)`（const static 实参）、`global.var1 = 99`（jsonc 注入成员赋值）、`IsIntType(ArrClass.type)`（.type 实参 Assert）。另有正向用例 ConstLib 输出丢数字（17 不打印）。
- 首个异常阶段：Meta（三类误伤）/ VM 运行期（丢数字，实为源码问题）。
- 根因：① const 语义边界过宽——enum 枚举值（EVariableFrom.EnumMember）、const static 类成员、`const data` 容器传染成员（DataMember）、jsonc global.data 注入成员（Manual，const 是初始值折叠标记）都是"机制性 const"而非用户显式 `const` 关键字，一刀切拦截破坏既有合法用法；② `MetaVisitCall.GetStoreMetaVariable()` 缺 `EVisitType.GetTypeValue` case（GetReturnMetaVariable 有），const 传参判定新调用链对 `.type` 实参触发 default Assert；③ ConstLib 用了 `return`——SL 返回语句是 `ret`，`return` 被静默忽略不报错，函数无返回值。
- 修复点（文件/模块）：`MetaVariable.cs` 新增 `isExplicitConst`（白名单 ClassMember+fromType==Code / LocalStatement / Argument）；`MetaAssignStatements.cs` 赋值拦截与 `MetaParam.cs` `isFromConstVariable` 均改用该判定（传参约束另加 `!isStatic` 放行编译期常量）；`MetaVisitCall.cs` GetStoreMetaVariable 补 GetTypeValue case；SLIR 跨模块链（SLVariablePackage.isConst 导出 / IRMetaVariable 双构造器透传 / ProjectReferenceModuleLoader SetIsConstImported）+ `MetaDefineParam.isConst` 改 OR 组合（导入路径构造器创建 MetaVariable 会短路单一判定）。
- 如何快速复现：改 isFromConstVariable/isExplicitConst 后 `scripts\compile-stdlib.ps1`（Math FAILED = const static 误伤）→ `compile-tests.ps1 BaseTest`（BindDataTest = const data 误伤、TypeTest = GetTypeValue 缺口）→ ExpendTest（IsolateTest = global data 误伤）。
- 如何快速验证：ConstProbe 5 错 / ConstCross 1 错（跨模块 addTwo）/ ConstLib 0 错 + C VM 输出 `ConstLib done 17`；标准库三件套 + BaseTest/ExpendTest/MathTest/NullFastTest 编译全 OK、C VM 运行全绿（已知差异除外）。
- 预防建议：① 语义级新检查上线前先跑标准库三件套——Lib/Core/Std/Math 是语义边界的"天然测试集"，本次三类误伤全部由它首先暴露；② 判定"用户显式声明"优先找**来源标志**（variableFrom / fromType）而非单一 bool；③ 两个同名 Get*MetaVariable 的 switch case 需人工对齐（GetTypeValue 只加了一处）；④ SL 返回语句是 `ret`，`return` 静默失效（已记 test/未解决问题.txt），排障输出为空时先查函数体用的哪个关键字。

### [2026-10-08] extern class 恒不导出：BindFileMetaClass 残留直赋值覆盖 ApplyClassPermission（10-07 回归）+ const 成员跨模块链路三断点
- 现象：① `extern class` 声明的类编译后不出现在 `module.json` 的 classList（publicExport 三档下均不导出），三态配置形同虚设；② 跨模块引用类中，const 成员字段（实例/静态）导出侧丢失 const 标记（flags 无 bit16），导入侧赋值不被拦截（方法形参 isConst 链路正常）。
- 首个异常阶段：Export（SLModulePackageWriter）；② 的断点分散在 Meta→IR→Export→RefModule 加载四处。
- 根因：① `MetaClass.BindFileMetaClass` 中存在 10-07 重构残留的 permissionToken 直赋值块（`m_Permission = GetPerMissionByType(fmc.permissionToken.type)`），在 extern→EPermission.Export 特判（`MetaBase.ApplyClassPermission`）**之后**执行并覆盖回 Public/Null，导致 extern 标记恒丢失；② 成员字段 const 链路三断点——IRMetaVariable(MetaMemberVariable) 构造器未拷贝 m_IsConst、SLModulePackageWriter.BuildFieldFlags 未置 bit16、MetaVariable.isExplicitConst 白名单未含 RefModule 来源（导入端 AddClassFieldFromIR 构造的成员 fromType=Manual 且已 SetRefFromType(RefModule) + SetIsConst）。
- 修复点（文件/模块）：① `source/Front/Core/MetaClass.cs` 删除 BindFileMetaClass 直赋值残留块（权限统一由 ApplyClassPermission 收口）；② `source/Front/IR/Core/IRMetaVariable.cs` 构造器补 `m_IsConst = mmv.isExplicitConst`（取 isExplicitConst 排除 jsonc global.data 注入的初始值折叠标记）；③ `source/Front/Export/SLIR/SLModulePackageWriter.cs` BuildFieldFlags 补 `if (v.isConst) flags |= 16`；④ `source/Front/Core/MetaVariable.cs` isExplicitConst 白名单补 `refFromType == RefFromType.RefModule` 分支。
- 如何快速复现：① 编译 `test/Other/ExportPermTest`（publicExport=none）检查 module.json classList 无 ExternClass；② 跨模块 `ConstLib.SCF = 5`（引用 test/Other/ConstProbe 的 ConstLib）不报 21372。
- 如何快速验证：① ExportPermTest 三档重编译——none 档仅 ExternClass/ExternData/ExternEnum 导出（permission=1）、public 档 public/plain 照常导出（permission=2）、extern 档全量导出且 permission 全记 1；② ConstLib 的 CF flags=18（public+const）、SCF flags=50（public+const+static）；ConstCross 中 `ConstLib.SCF = 5` 报 21372、`addTwo(la)` 报 21468，注释负向后编译导出成功。
- 预防建议：① 权限/标志类字段的赋值必须单点收口（本例 ApplyClassPermission），重构时全仓 Grep 该字段的所有写入点再动手，新代码写入点即回归点；② 「标记位跨层传递」（Meta→IR→SLIR flags→导入端）新增位时逐层核对拷贝代码，BuildFieldFlags 的位注释（16=const, 32=static）与写入逻辑同步维护；③ PowerShell 检查 module.json 时对不存在属性（dataList/enumList）的迭代会产出空行假象，判定键是否存在先看 JSON 顶层键名（本仓库 module.json 顶层只有 classList，data/enum 以 metaClassKind 区分）。

### [2026-10-08] return 关键字「静默失效」定性修正：不在 jsonc 清单=从未编译；词法层拦截（LID 20118）终结误导性报错
- 现象/任务：用户报告「return 在 .sl 中被静默忽略（返回语句是 ret），函数写了 return 依然无返回值且编译不报错」（2026-10-02 ConstProbe 排查时发现），要求核实并修复。
- 首个异常阶段：定性过程三段——① 探针 ReturnProbe（static 版四形态）全部报错，与「TemplateClass.sl 静默」表象矛盾；② 实例方法版复测依然全报错，排除 static/instance 假设；③ 真因：「静默」场景（TemplateClass/Sort/FrameData/LocalSqliteTest 等含 return 的文件）根本不在 jsonc `compileFiles` 清单里、**从未被编译**；真正被编译的 return 会报错，但是 `Name:return not found!` / 「在定义的类名[return]没有找到相关的定义」这类无 ret 指引的误导性信息。
- 根因：`return` 不在关键字表（词法仅 `case "ret"` 映射 ETokenType.Return），作为普通 Identifier 流入语义层，产生与「写错标识符」同款的误导性报错；「完全不报错」的观感全部来自文件未被编译（不在清单）。
- 修复点（文件/模块）：① `source/Front/Log/LID.cs` 新增 `TokenLexerReturnNotSupported=20118`；② `source/Front/Log/ErrorDefinitions.csv` 新增 20118 Error 行（pass=FALSE，文案指引改用 ret）；③ `source/Front/Compile/Parse/LexerParseToToken.cs` 关键字 switch 新增 `case "return"`——记 Error 日志后 tokenType 仍置 Identifier（不引入新解析路径，下游行为不变，仅靠 Error 级 + pass=FALSE 使 File 阶段失败、Export Skipped、module.json 不导出）。
- 兼容性核查：主链路（标准库/6 用例集 jsonc 清单内）无任何把 return 当合法标识符的用码；现存 return 实码文件均不在编译清单；`@tag(){}` 块体是 AtSignBlock 不透明 token（内联 JS/C# 的 return 不受拦截影响）；字符串字面量内 return 不走 ReadIdentifier——词法拦截零破坏。
- 如何快速复现：清单内任意 .sl 写 `return 5;`，旧版报 `Name:return not found!`（误导性）；不在清单的文件写 return 则完全无感（从未编译）。
- 如何快速验证：ReturnProbe（test/Other/ReturnProbe/）修复后编译，5 处 return 全报 `Error 关键字[return]不受支持：返回语句请使用 ret 关键字!`（20118）、File 阶段失败、Export Skipped；标准库三件套 + `compile-tests.ps1` 六用例集（BaseTest/ExpendTest/MathTest/SpecialTest/BenchMark/NullFastTest）全 OK。
- 预防建议：① 「静默失效」类报告先核对涉事文件是否真在 jsonc `compileFiles` 清单内——不在清单=从未编译，任何语义都无从谈起（判据：Logs/Front.txt 有无该文件编译记录）；② 误导性报错的根治优先在最早层（词法）拦截并给出正确关键字指引，而非放行到语义层产生「查无此名」噪音；③ 词法层加拦截用「Error 日志 + tokenType 保持 Identifier」手法可不改变下游解析流，回归面最小。

### [2026-10-08] 类成员变量无初始化声明的报错修正：新 LID 21472 取代 MetaCoreExpressIsNull 误导文案
- 现象/任务：未解决问题.txt 条目——类成员变量声明不带初始化（如 `int v;`）报 MetaCoreExpressIsNull（extend「express传入参数与要求参数不对应!」）而非「成员变量必须初始化」指引（2026-10-02 编写 FinalProbe 用例时发现）。
- 首个异常阶段：ParseMemberExpress（MetaCore）；复现探针 test/Other/MemberInitProbe/（问题行 `int v;` + 对照组 `int ok = 1;`）。
- 根因：三层叠加——① CreateMetaExpress 对无 express 的源成员静默 return（其注释称「交由下方统一校验」，但该 return 跳过了 CreateMetaExpress 内 273 行的明确校验）；② ParseMetaExpress 对「m_Express==null 且有 FileMeta 源」统一报 11009 MetaCoreExpressIsNull；③ Log.WriteCoreByToken 在「csv 模板 paramCount 与调用传参不匹配/无参」时给 extendMessage 拼「传入参数与要求参数不对应!」兜底（11009 模板要求 1 参、该调用只传 msg）——与 2026-09-16 InlineMethodTest 复盘（本节）同一误导源。
- 修复点（文件/模块）：① `source/Front/Log/LID.cs` 新增 `MetaCoreMemberVariableRequireInit=21472`；② `source/Front/Log/ErrorDefinitions.csv` 新增 21472 Error 行（pass=FALSE，明确文案+修复指引）；③ `source/Front/Core/MetaMemberVariable.cs` ParseMetaExpress 区分两分支——`FileMeta.express==null`（源码未写初始化）报 21472（extend 带类名.成员名），`express!=null 但创建失败`保留 11009（创建失败的具体原因已在 CreateMetaExpress 阶段报过）。
- 兼容性：11009 与 21472 同为 Error/pass=FALSE，报错替换零行为变化（编译成败不变，仅文案改善）；local{} 合成成员（FileMeta==null）、RefModule 成员、attribute 注入路径守卫不变；标准库三件套 + compile-tests.ps1 六用例集全 OK，无 21472 误报。
- 如何快速复现：清单内任意 .sl 类体写 `int v;`，旧版报 `MetaCoreExpressIsNull`+「传入参数与要求参数不对应!」；同文件正常成员 `int ok = 1;` 不报错。
- 如何快速验证：MemberInitProbe 编译报 21472 `Error 类成员变量声明必须初始化: SL 成员变量不支持无初始化声明，必须要有等号及初始值表达式（如 int v = 1;）!!`、Extend 带类名.成员名、Tips 给修复指引，阶段恰好 1 error。
- 预防建议：① 用户可见的第一错误必须能给出正确指引，通用兜底文案（参数数量不匹配拼接）不应承担这个角色；② AddXXXLog 调用点与 csv 模板 paramCount 不匹配本身就是编码缺陷，Log 层兜底可考虑带上 LID 与期望/实际参数个数（本次未动 Log.cs，聚焦成员变量场景）；③ SL 类成员变量必须带初始化表达式（区别于 C#/Java），写标准库/用例时注意。

### [2026-10-11] next 语句 IR 分派缺 case：BaseTest 全量编译失败（Assert 计 errorCount 阻断 Export）
- 现象/任务：P2（SLB 内存直传）验收时 SlangAllInOne `run ProjectTest` 与 Front CLI 标准姿势均 `编译阶段[IR]失败: 阶段内产生了 1 个错误`，Export Skipped、无 package 产出；`[Assert] [GenIR] IRMethodNotFoundVariable "没有解析IR的语句类型…传入参数与要求参数不对应!"`（extend 兜底文案再次误导——实际与参数无关）。
- 首个异常阶段：IR（TranslateIR）；最早的 Error 只有 ProcessPhaseFailed 汇总行，Assert 行无 Position 无 token，定位靠插桩。
- 根因：`next` 语句特性（switch case 显式贯穿，`MetaNextStatements`/`ETokenType.Next`/`metaCase.isContinueNext` 全链已落地）合入时，`IRBlockStatements.ParseAnyIRStatements` 的语句类型 switch 漏登记 `MetaNextStatements` case → 落 default → Assert 级日志（pass=FALSE 计入 errorCount）→ IR 阶段失败。BaseTest 自该特性合入起整体不可编译；10-09 的 ProjectTest.module.json/.slb 为前代产物（SwitchTest 当时无 next 用例）。
- 修复点（文件/模块）：① `source/Front/IR/IRStatements/IRBlockStatements.cs` 补 `case MetaNextStatements`——按 MetaSwitchStatements 既有设计注释（"next 语句本身在 IR 层不发射指令，由 IRSwitchStatements 的 isContinueNext 分发"）：无 continue 目标（switch case 体、无外围循环）不发射（贯穿由 case 体后的分发发射承担）；有 continue 目标（循环体内 / case 体内有外围循环，Meta 层"循环优先"绑定）发 `Br continueTarget` 等价 continue（与 IRContinueStatements 同源目标栈）；② default 分支插桩保留：打出未知语句类型名（`type=MetaXxx`），根治"没有解析IR的语句类型"无类型信息的历史盲区。
- 兼容性核查：IRSwitchStatements 的 fall-through 发射路径零改动（本就存在）；`next` 在循环内 code.md 规则4-4 不推荐（用 continue），但 keywords.md 声明等价 continue——本修复两者均覆盖（有 continue 目标即发跳转）。修复后 ProjectTest 全量 73 文件编译恢复、`--format binary` 导出恢复、P2 双链路验收通过。
- 如何快速复现：修复前清单内任意含 `next` 的 .sl（如 BaseTest/SwitchTest.sl nextCaseTest）编译即 `[Failed] IR`，Assert 行可数；修复后消失。
- 如何快速验证：`compile -p test\BaseTest\ProjectTest -e ir` 出现 `export module success` 且 module.json mtime 更新；SlangAllInOne `run … --format binary` 双链路输出一致；磁盘 `run module.json` vs `module.slb` T3 差分一致。
- 预防建议：① 新增 Meta 语句类型是四处同步的第 4 处之外易漏点——IRBlockStatements 的类型 switch 应与 Meta 语句类全集对齐（可加静态自检/单测遍历 MetaStatements 子类清单）；② Assert 级日志无 token 无类型名的盲区靠"打出GetType().Name"级别的插桩一次根治，勿依赖误导性兜底 extend；③ "IR 阶段失败但无详细错误"时直接查 Front.txt 中 IR 阶段区间的 `[Assert]` 行，Assert 计入 errorCount 的机制见 Log.AddLog。

### [2026-10-11] 非本语言关键字通用拦截：return 词法接线补落 + 15 词拦截表（LID 20119），准入纪律=全仓 Grep 真实用法
- 现象/任务：用户要求「识别到非关键字但语法不符的也要报错，如 return 不是关键字是错误语法」并推广到所有其他语言常见关键字。核查发现 [2026-10-08] return 复盘所述词法接线（`case "return"`）**实际从未落地**——LID.cs/CSV/文档就位但 `LexerParseToToken.cs` 无对应分支，return 仍走 default Identifier 流入语义层（该复盘的"如何快速验证"一节在当时是超前的）。
- 首个异常阶段：Token（词法层拦截缺失，属补全）。
- 根因：return 拦截当年只落了 LID/CSV/文档三件，词法器 switch 的 case 漏写（无任何构建/测试拦截此类"文档先行、代码缺位"）；且单 case 写法不可扩展，无法承载"所有其他语言关键字"的通用需求。
- 修复点（文件/模块）：① `source/Front/Parse/LexerParseToToken.cs`——新增静态拦截表 `s_UnsupportedKeywords`（词 → (LID, 改写指引)），`ReadIdentifier` 读词后先查表：命中即 `Log.AddTokenByString` 报 Error（return→20118 专用，其余 15 词→20119）并**仍发 Identifier token**（token 流完整，编译靠 errorCount 阻断），然后 return；② `source/Front/Log/LID.cs` 新增 `TokenLexerUnsupportedKeyword=20119`；③ `source/Front/Log/ErrorDefinitions.csv` 新增 20119 行（列结构与 20118 镜像，paramCount=0 固定文案 + Extend 带逐词指引）；④ 拦截词 16 个：return/foreach/using/elseif/struct/let/raise/lambda/def/package/include/impl/trait/pub/unless/until。
- 准入纪律（本次确立）：候选词必须全仓 Grep `.sl/.s`（source/Front/Lib + test + plugins）确认**注释/字符串以外无真实标识符用法**——`end`（String.slice 参数/Monitor.end 方法）、`fn`（Codec 系局部变量）、`match`（RegExp.match 方法）、`delete`（File/Directory.delete 方法）、`self`（CoroutineTest 变量）、`func`（ExpressTest 的 `func<int,int>` 类型）、`nil`（JsonTest 变量）、`then/loop`（过通用）均因此排除；`@csharp(){}` 块体由 TryCaptureAtSignBlock 整块原文捕获，块内 using/return 不受影响。
- 如何快速复现：编译 `test\Other\ReturnProbe\ReturnProbe.sp`（round3 探针：5 处 return + 15 个其他语言关键字各一行）。
- 如何快速验证：ReturnProbe 报 5×20118 + 15×20119（Extend 各带改写指引）且「Token 解析存在错误」阻断导出；三路回归 SpecialTest（@csharp 块含 using/return）、BaseTest（self/func/字符串 foreach）、compile-stdlib.ps1（Core/Math/Std，end/fn/match/delete 标识符）均 `export module success`。
- 预防建议：① 「文档先行」的特性落地后必须回验文档声称的行为（本次 10-08 复盘的验证节即为超前书写）；② 词法层拦截统一走「Error 日志 + tokenType 保持 Identifier + errorCount 阻断」手法，勿在词法层 throw 中断 token 流；③ 新增拦截词先全仓 Grep 真实用法并同步 `md/syntax/keywords.md` §1.8 总表（含"刻意不拦截"清单与理由）。

### [2026-10-12] import as 三形态 + typealias 完善期探针 17 错误误判代码 bug：插桩（R12）锁定根因——引用模块成员树来自 SLIR 包 .sl 实际声明而非 jsonc struct[] 骨架
- 现象/任务：落地 `import Std as S`（模块别名）、`import "Std:IO" as IOx`（字符串 import 别名）续解逻辑与 typealias 使用点禁追加模板实参（LID 23016）后，探针工程 AliasProbe 编译报 17 个错误：`S.IO.Console` / `S.IO.FileMode` / `S.IO.FileStream` / `IOx.Console` 等路径全部解析失败，并伴随「println 没有在类[Std.Console]中被发现」，疑似新写的别名续解代码有 bug。
- 排障路径（R12 插桩闭环）：FileMetaGlobalSyntax.Parse()（PROBE-Import 看 as 目标注册了什么）+ FileMeta.cs AddImportAliasMetaNode / GetMetaNodeByImportAlias（PROBE-RegNodeAlias / AliasLookup / AliasHit）→ 插桩显示别名注册与查询全部正确命中（`S -> Std` ×3、`IOx -> IO` ×2），续解从正确 MetaNode 出发但找不到成员 → 推翻「代码 bug」假设，矛头指向用例路径假设。
- 根因：探针按 `Std.jsonc` struct[] 骨架声明假设 Console/FileMode/FileStream 在 `Std.IO` 命名空间下；实际引用模块（references 指向 `out/export/Std/Std.module.json` SLIR 包）的成员树来自 **.sl 实际声明**：无命名空间声明的类型注册在 Std 模块根（应为 `S.FileMode`），`enum IO.SearchOption` / `public class IO.Directory` 等 .sl 限定声明才是 IO 命名空间成员（IO 命名空间真实成员仅 Directory/Path/SearchOption）；jsonc struct[] 骨架在 SLIR 加载路径下不参与解析（`ProjectReferenceModuleLoader.TryLoadSourceModule` 仅是无 SLIR 包时的回退）。「println 没有被发现」为级联错误——外层 `Console.println(...)` 因实参链（`S.IO.FileMode...`）失败而报，Console 本身解析正常。
- 修复点（文件/模块）：① `test/Other/AliasProbe/AliasProbeLib.sl` 路径修正（`S.FileMode` / `S.IO.Directory` 四段链 / `IOx.Directory`）+ 补 typealias 目标用模块别名/字符串 import 别名前缀用例，全部插桩清理归零；② 功能侧（前会话落盘、本次验证有效）：`FileMeta.cs` m_ImportAliasNodeDict + GetMetaNodeByImportAlias 合并查询（模块/命名空间别名统一续解）；`FileMetaGlobalSyntax.cs` isMetaModule 分支注册模块别名 + ParseStringImportTarget 反转修正；`TypeManager.cs` 两处 LID 23016 检查 + `LID.cs` 23016 + `ErrorDefinitions.csv`。
- 如何快速复现：探针按 jsonc struct[] 假设写 `S.IO.FileMode` 编译即报链式解析失败；此症状在「代码 bug」与「用例错」之间无法直接区分，须插桩看注册/命中两点位。
- 如何快速验证：修正路径后 Front 零 Error + `export module success`；C VM 运行 EXIT=0 全部输出正确（SUB.Alpha 静态/实例、S.Console.println、S.IO.Directory.exists('.')、IOx.Directory/SearchOption、LibSFileMode.Create、LibIOxSearch.TopDirectoryOnly、LibFuncTA(3,4)=7、工程级 SpIntList/SpAliasAlpha 跨文件可见）；负例 `LibBoxInt<string>` 触发 23016 阻断编译，删除负例恢复全绿。
- 预防建议：① 写跨模块引用类用例前先看引用模块的 **.sl 源码实际声明**（或 module.json allName），jsonc struct[] 骨架在 SLIR 加载路径下不构成解析依据；② 实参链失败时外层调用的「方法未发现」是级联噪音，只看最早 Error（R8）；③ 别名/前缀续解类新功能的插桩优先打「注册了什么 + 查到什么」两点位，可最快区分注册错/续解错/用例错；④ 排障中发现 typealias.md 与实现脱节（声称仅允许 .sp Global(){}，实际 .sl 文件级 + .sp Project 类体均支持），已按实证重写并同步 namespace.md「import as 三形态」专章。

### [2026-10-13] import 导入失败静默放行连锁下游错误：LID 20377-20382 升 Error 严格化 + Core 存量死导入清理
- 现象/任务：用户要求「import 导入错误的显示——检查如果没有导出正确，需要有错误提示」。此前 import 解析失败只打 **Info 级且 pass=TRUE**（不计 errorCount、不阻断编译），编译照常走到 IR/Export，在下游产生大量连锁迷惑错误（如「println 没有在类[Std.Console]中被发现」），真正的病因（引用模块未在 references 声明、未编译导出、命名空间不存在）被淹没。
- 首个异常阶段：定性点在 Log 层——`ErrorDefinitions.csv` 中 import 六个 LID（20377-20382）登记为 `Info,FALSE,TRUE`（pass=TRUE 即不计 errorCount），且 `FileMetaGlobalSyntax.ResolveImportMetaNode` 对「无可解析目标」分支根本不发日志（静默 return null）。
- 根因：三层叠加——① LID 级别登记为 Info+pass=TRUE，错误信息仅作提示不拦截；② ResolveImportMetaNode 的 m_NamespaceStatement 空/空 tokenList 分支无日志直接 return null，下游拿 null 继续解析产生连锁噪音；③ 20381 的 csv 模板 zhMessage 带 3 个 `{}` 占位但 paramCount=0，模板占位永不填充。宽松放行的直接副作用：历史死导入（`import CSharp.System;`——全仓无 CSharp 命名空间定义）在 Float32.sl/Float64.sl 静默躺了多版本，无人察觉。
- 修复点（文件/模块）：① `source/Front/Log/ErrorDefinitions.csv`：20377-20382 六行由 `Info,FALSE,TRUE` 改 `Error,FALSE,FALSE`，补 zhFixedTips（指向「模块需在 references 声明且已完成编译导出」）；20381 zhMessage 改静态文案（原 3 `{}` 与 paramCount=0 不匹配）；20020（引用模块加载回退源 jsonc）保持 Info/pass=TRUE（RefModule NotifyOnly 设计语义不动）；② `source/Front/FileMeta/FileMetaGlobalSyntax.cs`：ResolveImportMetaNode 空 tokenList 分支补首发日志（激活 20382）、命名空间下钻失败处 extend 补 references hint、ParseImportSyntax 字符串目标解析失败短路 + CreateStateBlock null 检查、Parse() 对 ParseImportSyntax 失败短路 return（消除下游 NRE 风险）。
- 兼容性核查：MetaCore 阶段为 AbortPhase 策略（阶段内 errorCount 差值非 0 即失败），LID 升 Error+pass=FALSE 后 import 失败自动阻断 IR/Export、module.json 不导出——无需改任何阶段控制代码。20020 保持 Info 使「三级加载回退」（直接路径 → SLIR 包 → 源 jsonc）机制不受影响。
- 如何快速复现：清单内 .sl 文件头注入三类坏 import：`import "Std:";`（字符串空命名空间）、`import "Missing:Foo";`（字符串模块不存在）、`import MissingModule.Foo;`（路径首段不存在）——旧版静默通过或仅 Info，连锁错误出现在下游。
- 如何快速验证：三类坏 import 各打出一条 **Error 级**日志（20378/20380/20381），Position 精确到行、Extend 带明细、Tips 可操作；阶段汇总 `[Failed] MetaCore : 阶段内产生了 3 个错误`，IR/Export `[Skipped]`，module.json 文件级 mtime 不更新（dotnet 退出码仍为 0，勿以退出码判成败）；还原注入用例后 `export module success` 恢复。
- 回归暴露的存量问题：严格化后 stdlib 编译即失败——Core 模块 Float32.sl/Float64.sl 首行 `import CSharp.System;` 为历史死导入（全仓 Grep 确认无 CSharp 命名空间、文件内无 System. 成员引用、其余命中皆注释），删除两行（留注释说明）后 stdlib 三件套 + 6 用例集（BaseTest/ExpendTest/MathTest/SpecialTest/BenchMark/NullFastTest）全部编译通过。
- 预防建议：① **错误级别严格化会暴露存量死代码**——LID 从 Info/pass=TRUE 升 Error 前先全仓 Grep 该日志点对应的既有用例（本类死导入即躺枪者），回归脚本 mtime 新鲜度判据（module.json mtime > 启动时间）是发现这类回归的有效手段；② 「无日志静默 return null」分支是连锁错误的放大器，排障时对可疑路径先确认是否根本没发日志；③ csv 模板 `{}` 占位个数必须与 paramCount 一致，否则占位永不填充（20381 曾 3 占位配 0 参）；④ 新增行的尾逗号数不要求与表头列数严格一致（曾担心 LID 注册静默失败，实测正常），但按表头 11 列书写仍是纪律。

### [2026-10-13] 空体无参 _init_ 构造调用省略（Front 层优化，非缺陷修复）
- 现象/任务：用户要求「去掉无用的 init——如果 `_init_` 里内容为空，就不在 runtime 里调用 init，尽量在 FrontEnd 层优化」。此前形如 `_init_() { }` 的空体构造函数，每个 `new` 点都会发射 `Dup`+`CallVirt _init_` 空调用并进 VM 执行一次（跨模块时还要经过 SLIR 装载出的空指令体方法），纯属开销。
- 优化点：① `source/Front/Core/MetaMemberFunction.cs` 新增公共属性 `isEmptyCtorBody`——本地方法走 `isConstructInitFunction && metaBlockStatements 非空且语句链为空`，跨模块（RefModule）走 `name == "_init_" && isEmptyBodyFromRefModule`（由 SLIR 包 `isEmptyInstructionBody` 回填，判据=instructionList 为空或仅 Nop/Label）；② 构造 CallVirt 发射点共三处接省略守卫：`IRExpress.cs` `IRNewExpress` 数组路径 + 普通类/泛型类路径（新增 `SkipEmptyCtorCall` 静态辅助），`IRMetaCallLink.cs` `ParseNewInCallLink` 链中 New；③ `IRDefineVarStatements.cs` 原 ARC v1 私有判据 `IsEmptyCtorBody` 上移并入该属性（判据单点化，双消费方：ARC 非逃逸 new 认领 + IR 省略）。
- 安全性论证：SL 的 `new` 只调用解析出的单个 ctor（无 C# 式隐式基类构造链）；空体 ctor 无任何用户代码接触 `this`，不可能有副作用；省略后 `NewObject` 压入的新对象原样留在栈上，栈平衡不变。**带实参的空体 ctor 不省略**——实参表达式可能含副作用，求值语义必须保留（`SkipEmptyCtorCall` 同时要求 `metaInputParamList.Count == 0`）。
- 发射点圈定方法：全仓 Grep CallVirt 发射共 7 处，逐一甄别后确认构造路径仅 3 处；`IRExpress.cs` L747/L861（brace 初始化 add 方法/set 访问器）与 `IRCall.cs` L412（callType==1 通用方法分发）均非构造路径，勿误改。
- 如何快速验证：`out/export/ProjectTest/DebugCode/HashSetTest/IR.txt`——`Core.HashSet<int> s = new()`（HashSet.sl `override _init_()` 空体）现为 `NewTemplateObject → ArcRetain → StoreLocal`，无 Dup+CallVirt；对照 `HashSet<int>(8)` / `HashSet<int>(arr)` 仍完整保留 `Dup+实参+CallVirt`（带实参不省略）。
- 预防建议：① 判据属性不置 null `metaMemberFunction`（省略发射≠解除绑定），ARC 认领及其它消费方按原绑定路径走 `isEmptyCtorBody` 第二分支，语义兼容；② 跨模块空体判据依赖 SLIR 包 `isEmptyInstructionBody`——**重新导出标准库后**跨模块 new 点才享受省略（旧 module.json 无该回填时保守视为非空）；③ 未来若引入「构造函数链」语义（隐式基类 ctor），本优化必须重新评估（空体 override ctor 的基类 ctor 可能非空）。

### [2026-10-13] .sp 顶级 typealias 工程级支持缺口（文档先行、代码未跟上）+ 类体内 typealias 一律禁止（20223/20204 精确化）
- 现象/任务：用户要求「全局 typealias 须在 .sp 中定义即可全工程使用」并在现有 AliasProbe 探针中验证；追加约束「typealias 只允许写在类最外边，类体内不允许」。实测 `.sp` 顶级写的 typealias 仅本文件可见，跨文件使用点全部报「类型不存在」——`md/syntax/typealias.md` 早已记载该行为为工程级，属「文档先行、代码未跟上」型缺口。
- 根因：① `StructParseFrame.ParseRootNodeToFileMeta` 文件级 typealias 消费点调用 `ConsumeTypeAliasAt(m_RootNode, pnode.parseIndex, false)` **恒传 projectScope:false**，`.sp` 与普通 `.sl` 无差别，TypeManager 既有 `ResolveAllDeclaredTypeAliases` 工程级多轮收敛机制（任何 FileMeta 的 IsProjectScope==true 声明都会注册为全工程可见）从未被喂到料；② `ParseClassNode` 原有 `.sp` `Project` 类体放行 typealias 的分支（旧行为），与用户新约束冲突。
- 修复点（文件/模块）：`StructParseFrame.cs` 四处——① 文件级消费点按 `m_FileMeta.path` 后缀 `.sp` 检测传 `isSpProjectFile`（工程级注册接管，TypeManager 零改动）；② 根循环 IdentifierLink 分支（无关键字顶级类如 `Project{}`）置位 `hasNamespaceOrClass`；③ `ParseNamespaceOrTopClass` Key 分支新增 TypeAlias 专门处理：报 20204 并跳过整条语句（不注册别名）；④ `ParseClassNode` 类体内 typealias 改无条件报 20223。配套：`ErrorDefinitions.csv` 20223 文案、AliasProbe.sp/.sl 用例与 `md/syntax/typealias.md` 文档同步。
- 排障结构特性（负向场景关键发现）：`Project{}` 之后写 typealias 原先报 20218 泛化错误而非 20204——`ParseNamespaceOrTopClass` 解析完类后**递归重入同 pnode 吞掉其后所有同级节点**，根循环的 20204 分支不可达。修复选择「在函数内报错并跳过」而非「break 交回根循环」，因 namespace 体内场景（`ParseNamespaceOrTopClass(block)`）若不吞会静默丢失别名。
- 如何快速验证：AliasProbe 干净态编译（slb+json 双导出成功）+ C VM 运行，工程级四别名跨文件全绿（`SpIntList[0] = 7`、`SpAliasAlpha(9).x = 9`、`SpTopKV[k] = v`、`SpTopAlpha(10).x = 10`）；负向两类位置实测触发——类体内 → 20223「类体内不允许 typealias，只能写在类外的文件顶级」、`Project{}` 之后 → 20204「typealias 只能写在 import/local 后、namespace/class/data/enum 前」。注意 Node 层 Log 错误不中断编译（export 仍成功），但被拒别名不注册 → 使用点连锁报错，拒收实际有效。
- 预防建议：① 「文档先行」型功能缺口最易躺平——文档记载某行为时应立即补正向测试锚定（AliasProbe 此前恰缺工程级跨文件用例）；② 解析器「类解析完成后递归吞后续节点」结构下，**外层循环的语法分支可能永远不可达**，需要在类头解析流内精确报错的语法必须在该函数内加专门分支；③ grep 编译日志中的 LID 时按**枚举名**过滤（日志行含 `[NodeStructFrameTypealiasSpProject]` 而非数字 20223）。

### [2026-10-17] SLangServer P0（HttpServerCore 连接引擎）落地期三类 SL/Front 语义坑：跨模块多实例字段串扰 / string 字段默认值消费侧丢失 / 泛型参数禁点分
- 现象：SLangServer P0 验收（NetTest S 组）过程中——① 同进程连续 new 多个 HttpServerCore 实例（S1→S2→S3 各自 configure 路由），S2 的连接读到 S1 的路由表（POST /x 命中 GET /hello 路由表，404）、S3 读到 S2 的表，实例状态整体滞后一轮；② `SLang.ServerConfig cfg = SLang.ServerConfig()` 后读 `cfg.host` 得**空串/null**（netstat 实证 listen 绑到了本机 IP `172.16.3.60` 而非 `0.0.0.0`，`getaddrinfo("")` + AI_PASSIVE 在 Windows 上按本机名解析），而同结构 `cfg.port` / `cfg.keepAliveTimeoutMs`（Int32）读值正常；③ `List<Net.TcpStream>` 字段声明报「模板中不允许使用 . 连接符号」（MetaCoreTypeNotAllowSign Info）且**不阻断编译**——带坏类型的类装配后首个静态调用协程静默死亡（`_acceptLoop` 进去后任何打印都消失）。
- 首个异常阶段：全部为 VM 运行期 / Front-Meta 混杂（非引擎逻辑错误）。
- 根因与修复点（文件/模块 = `source/Front/Lib/SLang/Server/HttpServerCore.sl`）：
  ① **跨模块类多实例化实例态串扰**：连接/启动协程闭包先按「零捕获 + spawn 实参传值」（NetConcurrentTest.ef 先例）改造仍复现——问题不在捕获而在 C VM 对**跨模块引用类的多实例**本身的实例态隔离不可靠。修复：HttpServerCore 改**全静态模块级单例**（`configure(cfg)/reset()/start()/stop()` + 静态 `_routes/_conns`），对齐 v1 服务器语义（SERVER_DESIGN.md §1.2「单 VM 协程调度即全部并发」）；多服务器实例需求留待 P1 门面按单实例收口或查 C VM instance 隔离。
  ② **跨模块 string 字段默认值在消费侧丢失**：定义侧 `public string host = "0.0.0.0"`（Mqtt clientId 同款先例），消费模块 new 后读出空——加无参 `_init_` 显式赋值亦无效（该 `_init_` 在消费侧未被调用）；修复 = `start()` 内对 host 空/null **兜底 `"0.0.0.0"`**（模块内字符串常量可靠）。附：`this._cfg.host` 双层字段链求值与 Websocket.sl `this.字段` 条件坑同源，构造期经 `参数.字段`（cfg.host）提取为单层静态字段再消费。
  ③ **泛型参数禁点分且坏类型静默**：`List<Net.TcpStream>` 非法但不报 Error → 类布局错乱 → 静默死。修复 = 连接表改 `List<object>` + `as Net.TcpStream` 转型（消费点仅 3 处）；typealias 中转亦不识别（`typealias NetTcpStream = Net.TcpStream` 作泛型参数报「没有找到模板参数 NetTcpStream」）。静态字段/局部变量/方法形参位点分类型（`Net.TcpServer _server`）合法不受影响。
- 附带坑：④ PowerShell `Select-Object -First N` 管道截杀 dotnet 编译进程（[2026-10-08] 复盘 ③ 的重演，且 `| Select-String ... | Select-Object -Last 2` 同样会提前 dispose 上游）——「打印了 success 行但 module.slb mtime 没刷新」即中招，长跑编译一律 `*> file` 落盘再二次过滤；⑤ Windows SO_REUSEADDR 端口可被多进程同时 LISTEN——调试期僵尸 csimple_lang 进程占住测试端口导致新一轮连接被抢（netstat 出现两条同端口 LISTENING），排障前先 `Get-Process csimple_lang` 清场；⑥ RST 断连：413/504 响应写完直接 `close()` 时入站 body 未读完，TCP 触发 RST、客户端读不到响应（HttpClient 现象 = 收不到/404）——优雅收尾 = `closeWrite()`（FIN）→ `delay(50)` 让出 → 丢弃式收尾读 → close（HttpServerCore._gracefulClose）。
- 如何快速复现：NetTest S2/S3 段（连续 configure 多轮服务器）；读 `ServerConfig().host`；`List<Net.X>` 字段声明后观察编译日志 TypeNotFoundTypeParam Info 行。
- 如何快速验证：NetTest 全套 FAIL count = 0（ServerProbeTest A1-B6 + ServerCoreTest S1a-S4b 共 22 项；S2a 413 / S3a 504 / S4b WS 预读残留帧 echo:hello 均过）。
- 预防建议：① 跨模块库类（SLangServer 消费 Core/Std 同理）**优先静态门面**，实例态字段多实例场景在 C VM 修好隔离前一律回避；② 跨模块消费的配置类 string 默认值一律在**消费点兜底**，不信任定义侧默认值（Int32 默认值实测可靠，string 不可靠）；③ 泛型参数位出现点分类型即改 typealias/object 转型，且每次编译扫一遍「TypeNotFoundTypeParam / NotAllowSign」Info 行——它们不阻断编译但必是运行期雷；④ 排障期终端管道过滤只用完整输出落盘方式，警惕「success 行打印了但产物没刷新」。

### [2026-10-17] SLangServer P1 验收用例编译期 Front NRE 双连跳：闭包形参作调用对象的泛型方法调用（`req.body<T>()`）——CreateCallNode realMetaType 判空缺失 + 泛型展开链 mc 无兜底
- 现象：ExpendTest/Server/ServerP1Test.sl 编译，Front 直接 NullReferenceException 崩溃（无 LID 报错行）——首发 `MetaCallNode.cs:1164 CreateCallNode`；按栈深修一处后 NRE 挪到 `MetaMemberFunction.AddGenTemplateMemberFunctionByMetaTypeList`（NRE 修复链典型症状：崩点随兜底逐层下移）。
- 首个异常阶段：Meta（语义分析，CallNode 创建 / 泛型方法展开）。
- 根因：P1 用例 handler 闭包形参（如 `function( Net.HttpRequest req )`）上调用泛型方法 `req.body<P1User>()`——闭包形参作调用对象时 `realMetaType` 解析为空直接解引用；且泛型方法展开链对传入 `mc == null` 无兜底继续深挖。
- 修复点（文件/模块）：`source/Front/Core/MetaCallNode.cs` CreateCallNode——闭包形参作调用对象时 realMetaType 判空回退 + `mcagm == null` 时回退 `mmf.ownerMetaClass`；`source/Front/Core/MetaMemberFunction.cs` AddGenTemplateMemberFunctionByMetaTypeList 开头 `if (mc == null) return null` 兜底。
- 如何快速复现：编译 ExpendTest（含 ServerP1Test.sl，内含闭包形参泛型方法调用）。
- 如何快速验证：ExpendTest 编译通过 + 全量运行 293/293 OK（BaseTest 同批无回归）。
- 预防建议：① 闭包形参作调用对象是类型解析弱位（realMetaType 常为空），新增调用形态处理先查空再解引用；② NRE 修复链「修一处挪一处」属正常推进，按栈逐层兜底直到编译通过，但每层兜底须回退到语义正确值（ownerMetaClass）而非吞错返回 null 泄漏到下游。










1. 运算符重载命名规则采用下划线前后缀风格，常见包括：  
   `_add_` `_sub_` `_mul_` `_div_` `_mod_` `_pow_` `_eq_` `_ne_` `_lt_` `_gt_` `_le_` `_ge_` `_and_` `_or_` `_not_` `_neg_` `_abs_` `_toInt_` `_toFloat_` `_toBool_` `_toString_` `_toArray_` `_toMap_` `_toList_` `_toObject_` `_toFunction_` `_toClass_` `_toInterface_` `_toEnum_` `_toType_` `_toTypeRef_` `_toTypeName_` `_toTypeNameRef_` `_toTypeName`
2. 上述重载风格与 Python 的“通过约定函数名绑定语义”思路类似，但以本项目命名约定为准。

> 新增语法规则请直接追加在本节，不再维护独立 `语法规则.md`。
