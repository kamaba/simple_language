# VM 文件作用说明

> 范围：`source/VM`（排除 `bin/obj` 生成文件）
> 
> 说明：以下为当前代码结构的职责梳理，便于后续重构与定位。

## `InnerCLRRuntime`

| 文件 | 作用 |
|---|---|
| `source/VM/InnerCLRRuntime/CLRRRuntimeVM.cs` | CLR 运行时入口/调度辅助（创建、压栈、执行 IR VM）。 |
| `source/VM/InnerCLRRuntime/Instruction.cs` | VM 指令模型；承载 `opCode/opValue/payload/index` 及操作数读取方法。 |
| `source/VM/InnerCLRRuntime/RawSValue.cs` | `SValue` 的低层原始结构（偏向性能与内存布局）。 |
| `source/VM/InnerCLRRuntime/RuntimeAssembly.cs` | 运行时程序集与模块容器。 |
| `source/VM/InnerCLRRuntime/RuntimeCall.cs` | 运行时调用描述（静态类型、模板类型、目标方法、参数个数）。 |
| `source/VM/InnerCLRRuntime/RuntimeClass.cs` | 运行时类模型与 `RuntimeClassManager`（类查找、方法查找、模板关系）。 |
| `source/VM/InnerCLRRuntime/RuntimeCLRCall.cs` | CLR 调用封装（桥接到 CLR 方法调用）。 |
| `source/VM/InnerCLRRuntime/RuntimeDefType.cs` | 运行时类型定义（含模板参数、owner/templateIndex）。 |
| `source/VM/InnerCLRRuntime/RuntimeMethod.cs` | 运行时方法模型（参数/局部/返回/指令列表）。 |
| `source/VM/InnerCLRRuntime/RuntimeModule.cs` | 运行时模块容器定义。 |
| `source/VM/InnerCLRRuntime/RuntimeType.cs` | 运行时类型实例与 `RuntimeTypeManager`（基础类型、模板类型注册与查询）。 |
| `source/VM/InnerCLRRuntime/RuntimeVariable.cs` | 运行时变量模型。 |
| `source/VM/InnerCLRRuntime/RuntimeVM.cs` | 核心解释执行器：指令分发、栈/对象/调用/分支执行。 |
| `source/VM/InnerCLRRuntime/SValue.cs` | VM 值类型（基础类型与对象引用统一表示）。 |
| `source/VM/InnerCLRRuntime/SValueCompare.cs` | `SValue` 比较逻辑。 |
| `source/VM/InnerCLRRuntime/SValueCompute.cs` | `SValue` 算术计算逻辑。 |

## `Parse`（当前主流程：`module.package.json`）

| 文件 | 作用 |
|---|---|
| `source/VM/Parse/SLAssembly.cs` | 包解析后的轻量程序集/模块/类型/方法结构。 |
| `source/VM/Parse/SLIRJsonModuleLoaderBootstrap.cs` | JSON 常量字符串字典共享入口（`LoadConstString` 兼容）。 |
| `source/VM/Parse/SLIRModuleParse.cs` | 程序启动解析流程编排（包加载、构建、全局初始化、入口决议）。 |
| `source/VM/Parse/SLModulePackage.cs` | `module.package.json` 数据结构定义。 |
| `source/VM/Parse/SLModulePackageLoader.cs` | `module.package.json` 读取与 VM 指令转换。 |
| `source/VM/Parse/SLRuntimeModuleRegistry.cs` | 运行时方法注册中心；调用绑定；类型定义解析与运行时类型注册。 |

## `Load`（读取器）

| 文件 | 作用 |
|---|---|
| `source/VM/Load/ILReader.cs` | .NET IL 反汇读取器（`MethodBase/Module + byte[]` -> 指令序列）。 |
| `source/VM/Load/SLIRBinModuleLoader.cs` | 二进制 SLIR 读取器（`SLIRBin`）。 |
| `source/VM/Load/SLIRJsonModuleLoader.cs` | JSON SLIR 读取器（`SLIRJson`），并与当前 package 流程对齐。 |

## `Object`

| 文件 | 作用 |
|---|---|
| `source/VM/Object/SObject.cs` | VM 对象基类。 |
| `source/VM/Object/ObjectManager.cs` | 对象创建/注册管理。 |
| `source/VM/Object/ClassObject.cs` | 类实例对象实现。 |
| `source/VM/Object/ArrayObject.cs` | 数组对象实现（读写元素）。 |
| `source/VM/Object/TemplateObject.cs` | 模板对象封装。 |
| `source/VM/Object/TypeObject.cs` | 类型对象（`type` 相关）。 |
| `source/VM/Object/VoidObject.cs` | `void` 对象占位。 |
| `source/VM/Object/StringObject.cs` | 字符串对象封装。 |
| `source/VM/Object/MethodHandleObject.cs` | 方法句柄对象封装。 |
| `source/VM/Object/NumObject.cs` | 数值对象基类。 |
| `source/VM/Object/IntObject.cs` | 整数类对象族（含有符号/无符号实现）。 |
| `source/VM/Object/FloatObject.cs` | 浮点对象族。 |

## `NativeBridge`

| 文件 | 作用 |
|---|---|
| `source/VM/NativeBridge/NativeBridge.cs` | 原生桥接总入口。 |
| `source/VM/NativeBridge/CSharpBridgeRegistry.cs` | C# Bridge 元数据注册、方法绑定缓存与解析。 |
| `source/VM/NativeBridge/CallCSharpDynamicLib.cs` | 调用 C# 动态库。 |
| `source/VM/NativeBridge/CallNativeDynamicLib.cs` | 调用 Native 动态库。 |
| `source/VM/NativeBridge/CallJavaDynmaicLib.cs` | 调用 Java 动态库（桥接入口）。 |

## `LocalRuntime`

| 文件 | 作用 |
|---|---|
| `source/VM/LocalRuntime/LocalRuntime.cs` | 本地运行时状态容器。 |
| `source/VM/LocalRuntime/LocalRuntimeVM.cs` | 本地运行时执行器。 |
| `source/VM/LocalRuntime/Memory/Malloc.cs` | 本地内存分配辅助。 |

## `Lib`

| 文件 | 作用 |
|---|---|
| `source/VM/Lib/CallMethodJsonExporter.cs` | 方法调用元数据导出（JSON）。 |
| `source/VM/Lib/CallMethodJsonImporter.cs` | 方法调用元数据导入（JSON）。 |
| `source/VM/Lib/Mem/Mem.cs` | 内存工具。 |
| `source/VM/Lib/Core/Object.cs` | 核心库 `Object` 相关运行实现。 |
| `source/VM/Lib/Core/Type.cs` | 核心库 `Type` 相关运行实现。 |
| `source/VM/Lib/Core/String.cs` | 核心库 `String` 相关运行实现。 |
| `source/VM/Lib/Core/Boolean.cs` | 核心库 `Boolean` 相关运行实现。 |
| `source/VM/Lib/Core/Number.cs` | 核心库 `Number` 相关运行实现。 |
| `source/VM/Lib/Core/Array.cs` | 核心库 `Array` 相关运行实现。 |
| `source/VM/Lib/Core/Range.cs` | 核心库 `Range` 相关运行实现。 |
| `source/VM/Lib/Core/Ptr.cs` | 核心库 `Ptr` 指针相关运行实现。 |

## `NewObject`

| 文件 | 作用 |
|---|---|
| `source/VM/NewObject/NewObjectHead.cs` | 新对象构造流程头部/上下文定义。 |
| `source/VM/NewObject/NewObjectManager.cs` | 新对象构建流程管理。 |

## `OtherLanuage/CSharp`

| 文件 | 作用 |
|---|---|
| `source/VM/OtherLanuage/CSharp/IRCSharpCallInStruction.cs` | C# 调用相关的 IR 指令适配定义。 |
| `source/VM/OtherLanuage/CSharp/IRCSharpCallStatements.cs` | C# 调用相关语句适配。 |

## 根目录文件

| 文件 | 作用 |
|---|---|
| `source/VM/Runtime/EVMType.cs` | VM 运行时值类型枚举。 |
| `source/VM/IROpEnum.cs` | IR 操作码枚举定义。 |
| `source/VM/Program.cs` | VM 控制台启动入口。 |

---

如果需要，我可以再补一版“**启动时序图**（从 `Program` 到 `RuntimeVM.RunInstruction`）”，放到 `md/VM_Startup_Flow.md`。