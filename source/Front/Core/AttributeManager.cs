//****************************************************************************
//  File:      AttributeManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/3/1 12:00:00
//  Description: attribute processing pipeline - dispatch by EAttributeStage
//****************************************************************************

using SimpleLanguage.Compile;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    /// <summary>
    /// 编译时属性处理器委托。
    /// 当 attribute 的 stage ∈ {PreCompile, Compiling} 时，由 C# 侧执行此处理器。
    /// （ParseAttributes 步骤全局分派 + MemberFunctionInject 挂点函数级分派共用）
    /// </summary>
    public delegate void CompileAttributeHandler(MetaAttribute attr, MetaBase owner);

    /// <summary>
    /// 成员变量初始化表达式合成处理器委托（MemberVariableExpress 挂点）。
    /// 成员变量无初始化表达式且挂有对应 attribute 时调用；
    /// 返回 null = 不接管（交下一个 attribute / 常规校验）。
    /// </summary>
    public delegate FileMetaBaseTerm MemberVariableExpressAttributeHandler(MetaAttribute attr, MetaMemberVariable mmv);

    /// <summary>
    /// PreCompile（编译前）跳过处理器委托：返回 true = 宿主符号整体跳过编译
    /// （成员/类不进 Meta/IR/module.json，引用点由符号解析自然报编译 Error，Q2 拍板）。
    /// 送检点 = Meta 层成员构建入口，早于语句语义分析（ATTRIBUTE_DESIGN §4.1）。
    /// </summary>
    public delegate bool PreCompileExcludeHandler(MetaAttribute attr);

    /// <summary>
    /// Compiling（编译中）时点的 IR 物化消费语义（ATTRIBUTE_DESIGN §4.2 / P5）：
    /// IR 层不再按 attribute 名称硬编码，统一经 AttributeManager.GetCompilingConsumer
    /// 按 stage==Compiling + 消费语义注册表分发。
    /// </summary>
    public enum ECompilingConsumer
    {
        None = 0,        // 非 Compiling 时点或无 IR 消费语义
        ExportName = 1,  // Nickname：arg0 为宿主导出别名（IRMethod/IRMetaClass/IRMetaVariable 的 exportNameList）
        AotFlag = 2,     // AOT：方法 SLIR flags |= 256（IRMethod.m_IsAot）
        GpuKernel = 3,   // GPU：kernel 参数（tile/launch）由 MLIRExporter 读取（IRMethod.m_GpuAttribute）
    }

    public static class AttributeManager
    {
        // 编译时属性处理器：按属性名注册（ParseAttributes 步骤全局分派）
        private static Dictionary<string, CompileAttributeHandler> s_CompileHandlers
            = new Dictionary<string, CompileAttributeHandler>(StringComparer.OrdinalIgnoreCase);

        // 成员变量初始化表达式合成挂点：按属性名注册。
        // MetaMemberVariable.CreateMetaExpress 在 express==null 时统一分派
        //（ParseMemberExpress 步骤，成员定义类型已收集，sig 可推导）——
        // 新增 attribute 不必再在 MetaMemberVariable 加硬编码调用点。
        private static Dictionary<string, MemberVariableExpressAttributeHandler> s_MemberVariableExpressHandlers
            = new Dictionary<string, MemberVariableExpressAttributeHandler>(StringComparer.OrdinalIgnoreCase);

        // 函数语句解析后注入挂点：按属性名注册。
        // MetaMemberFunction.ParseStatements 在 CreateMetaSyntax 之后统一分派
        //（CheckAllPathsReturn 之前，注入语句须参与返回路径校验）——
        // 新增 attribute 不必再在 MetaMemberFunction 加硬编码调用点。
        private static Dictionary<string, CompileAttributeHandler> s_MemberFunctionInjectHandlers
            = new Dictionary<string, CompileAttributeHandler>(StringComparer.OrdinalIgnoreCase);

        // PreCompile 跳过处理器：按属性名注册。
        // 送检发生在 Meta 层成员构建入口（比 ParseAllAttributes 更早），
        // 名字不在本表中的 attribute 直接跳过送检（零开销快速预筛），
        // 命中名字才建 MetaAttribute 走 Parse 静态解释（三层策略）确认 stage==PreCompile。
        private static Dictionary<string, PreCompileExcludeHandler> s_PreCompileExcludeHandlers
            = new Dictionary<string, PreCompileExcludeHandler>(StringComparer.OrdinalIgnoreCase);

        // Compiling 时点 IR 物化消费语义注册表：按属性名注册（P5）。
        // IR 层消费点（IRMethod/IRMetaClass/IRMetaVariable）不再认识具体名字，
        // 统一经 GetCompilingConsumer 查询（stage==Compiling 才可能非 None）——
        // 新增 Compiling 语义 attribute 注册进本表即可，无需改 IR 层各消费点。
        private static Dictionary<string, ECompilingConsumer> s_CompilingConsumers
            = new Dictionary<string, ECompilingConsumer>(StringComparer.OrdinalIgnoreCase);

        static AttributeManager()
        {
            RegisterBuiltInHandlers();
        }

        /// <summary>注册编译时属性处理器（ParseAttributes 步骤全局分派）</summary>
        public static void RegisterCompileHandler(string attrName, CompileAttributeHandler handler)
        {
            if (string.IsNullOrEmpty(attrName) || handler == null) return;
            s_CompileHandlers[attrName] = handler;
        }

        /// <summary>注册成员变量初始化表达式合成处理器（CreateMetaExpress 时分派）</summary>
        public static void RegisterMemberVariableExpressHandler(string attrName, MemberVariableExpressAttributeHandler handler)
        {
            if (string.IsNullOrEmpty(attrName) || handler == null) return;
            s_MemberVariableExpressHandlers[attrName] = handler;
        }

        /// <summary>注册函数语句解析后注入处理器（ParseStatements 的 CreateMetaSyntax 之后分派）</summary>
        public static void RegisterMemberFunctionInjectHandler(string attrName, CompileAttributeHandler handler)
        {
            if (string.IsNullOrEmpty(attrName) || handler == null) return;
            s_MemberFunctionInjectHandlers[attrName] = handler;
        }

        /// <summary>注册 PreCompile（编译前）跳过处理器（成员构建入口送检）</summary>
        public static void RegisterPreCompileExcludeHandler(string attrName, PreCompileExcludeHandler handler)
        {
            if (string.IsNullOrEmpty(attrName) || handler == null) return;
            s_PreCompileExcludeHandlers[attrName] = handler;
        }

        /// <summary>
        /// PreCompile 送检入口：宿主（类/成员函数/成员变量）的 FileMeta attribute 列表中
        /// 是否存在 stage==PreCompile 且处理器判定跳过的条目。
        /// 调用点 = 成员构建入口（AddClass / ParseFileMetaClassMemeberVarAndFunc /
        /// GlobalManager / LocalManager），早于语句语义分析；
        /// 命中则宿主符号整体不进入后续编译（不进 Meta/IR/module.json）。
        /// 说明：此处 new MetaAttribute + Parse 的静态解释依赖三层策略——
        /// 同模块 FileMeta 树解释 / 跨模块 s_BuiltInContracts 契约表兜底（此时
        /// Attribute 子类 MetaClass 可能尚未构建，查不到属正常，契约表保证 stage 正确）。
        /// </summary>
        public static bool ShouldExcludeByPreCompileAttribute(List<FileMetaAttributeSyntax> attrs)
        {
            if (attrs == null || attrs.Count == 0) return false;
            for (int i = 0; i < attrs.Count; i++)
            {
                var fmas = attrs[i];
                if (fmas == null || string.IsNullOrEmpty(fmas.name)) continue;
                if (!s_PreCompileExcludeHandlers.TryGetValue(fmas.name, out var handler)) continue;
                var ma = new MetaAttribute(fmas);
                ma.Parse();
                if (ma.attributeStage != MetaAttribute.StagePreCompile) continue;
                if (handler != null && handler(ma))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 查询 attribute 在 Compiling（编译中）时点的 IR 物化消费语义。
        /// IR 层消费点（IRMethod 的别名/AOT/GPU、IRMetaClass/IRMetaVariable 的别名）
        /// 统一走此入口：stage != Compiling 或名字未注册消费语义 → None（安全跳过）。
        /// stage 数据两条路径均可用——直接编译路径经 ParseAttributes 步骤（先于 IR 物化）
        /// 静态解释填充；ref module 路径构造时即携带导出包契约值。
        /// </summary>
        public static ECompilingConsumer GetCompilingConsumer(MetaAttribute attr)
        {
            if (attr == null || string.IsNullOrEmpty(attr.name))
                return ECompilingConsumer.None;
            if (attr.attributeStage != MetaAttribute.StageCompiling)
                return ECompilingConsumer.None;
            return s_CompilingConsumers.TryGetValue(attr.name, out var kind) ? kind : ECompilingConsumer.None;
        }

        /// <summary>注册内置编译时属性处理器</summary>
        private static void RegisterBuiltInHandlers()
        {
            // Nickname: 编译时注册别名
            // 在宿主类的父 MetaNode 下创建一个别名节点，指向同一个 MetaClass
            // 例如 Std.Float32_2 上 @Nickname("Vector2") ->
            //   Std 节点下新增 Vector2 子节点，指向 Float32_2 的 MetaClass
            s_CompilingConsumers["Nickname"] = ECompilingConsumer.ExportName;
            RegisterCompileHandler("Nickname", (attr, owner) =>
            {
                string nickname = attr.GetStringArg(0);
                if (string.IsNullOrEmpty(nickname) || owner == null) return;

                // 获取宿主的 MetaClass
                MetaClass mc = null;
                if (owner is MetaClass ownerMc)
                {
                    mc = ownerMc;
                }
                else if (owner is MetaMemberFunction mmf)
                {
                    mc = mmf.ownerMetaClass;
                }
                else if (owner is MetaMemberVariable mmv)
                {
                    mc = mmv.ownerMetaClass;
                }

                if (mc == null) return;

                // 获取父 MetaNode（类所在的命名空间/模块节点）
                var parentNode = mc.metaNode?.parentNode;
                if (parentNode == null)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAttributeCannotNicknameFind,
                        $"Nickname: cannot find parent MetaNode for '{mc.allName}'");
                    return;
                }

                // 在父节点下注册别名节点
                var aliasNode = parentNode.AddMetaClassAlias(nickname, mc);
                if (aliasNode != null)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAttributeNicknameRegisteredAlias,
                        $"Nickname: registered alias '{nickname}' -> '{mc.allName}' under '{parentNode.allName}'");
                }
            });

            // AOT: 编译时预编译标记
            // 仅注册到处理器，暂不关联其它逻辑（导出/LLVM 后续接入）
            // 参数（参考其它语言）：
            //   0: optimizeLevel          - GraalVM -O / GCC -O0~-O3
            //   1: target                 - .NET RID / GraalVM --target / GCC target triple
            //   2: linkMode               - GraalVM --static/--shared / GCC -static/-shared
            //   3: isDebugInfo            - GraalVM -g / GCC -g
            //   4: isTrimming             - .NET PublishTrimmed / TrimMode
            //   5: isInitializeAtBuildTime- GraalVM --initialize-at-build-time
            s_CompilingConsumers["AOT"] = ECompilingConsumer.AotFlag;
            RegisterCompileHandler("AOT", (attr, owner) =>
            {
                // 预留：暂无逻辑，仅记录挂载信息
                Log.AddMetaCoreLog(LID.MetaCoreAttributeAOTAttributeRegistered,
                    $"AOT: attribute registered on '{owner?.allName}' (no logic yet)");
            });

            // GPU: 设备计算（kernel）标记
            // 标注在成员函数上，由 MLIRExporter 读取参数发射 gpu.module/gpu.func/gpu.launch。
            // 位置实参（全部可选，未提供使用默认值）：
            //   0:  tileSizeWidth     - tile 宽度（默认 16）
            //   1:  tileSizeHeight    - tile 高度（默认 16）
            //   2:  tileNum           - tile 总数（0 = 自动推导）
            //   3:  groupId           - 工作组编号（默认 0）
            //   4:  gridDimX/Y/Z      - grid 维度（默认 1/1/1）
            //   7:  blockDimX/Y/Z     - block 维度（默认 256/1/1）
            //   10: sharedMemorySize  - 动态共享内存字节数（默认 0）
            //   11: deviceId          - 设备编号（默认 0）
            //   12: kernelName        - kernel 符号名（空 = 方法名）
            s_CompilingConsumers["GPU"] = ECompilingConsumer.GpuKernel;
            RegisterCompileHandler("GPU", (attr, owner) =>
            {
                var raw = attr.GetSplitRawArgs();
                Log.AddMetaCoreLog(LID.MetaCoreAttributeGPUAttributeRegistered,
                    $"GPU: attribute registered on '{owner?.allName}' args={raw.Count} " +
                    $"(tile={attr.GetIntArg(0)}x{attr.GetIntArg(1)} tileNum={attr.GetIntArg(2)} groupId={attr.GetIntArg(3)})");
            });

            // DllImport: C# P/Invoke 风格 FFI 函数声明标记（Preload 时点 attribute）
            //   @DllImport( "libdemo.so", "addcalc" )
            //   static int add( int a, int b ) { ret a + b }   // 函数体 = 绑定失败时的 fallback
            // stage = Preload(2)：Front 不做任何代码注入（旧隐藏字段/链头分派注入已退役），
            // 仅将 attribute 数据 (lib, symbol, sig) 随 module.json 方法级 attributeList 导出，
            // 由 cvm 在装配期（run 前）读取并完成 native 绑定（改写 CallStatic→CallFFIStatic）。
            // 实参: (库路径或别名, 符号名 [, 签名 "i32,i32->i32"])，sig 缺省时由导出层
            // 从函数签名推导补全（SLModulePackageWriter 方法级导出处）。
            // 旧 static Func<...> 变量声明形式已废弃，不再支持。

            // DllStaticImport: 静态绑定 FFI 快速调用声明标记
            //   @DllStaticImport( "mydll", "simplelanguage_addtest" )
            //   static int s_dllAdd( int a, int b ) { ret a + b }
            // 实参: (静态库名或别名, 符号名 [, 签名 "i32,i32->i32"])。
            // 库必须在 project.jsonc dllImports 中配置 "static" 字段，
            // cvm 加载模块时预载并注册到 FFI.StaticLibrary；
            // IRCall.Parse 在静态调用处发射 CallFFIStatic(77)，
            // cvm assembly build 期解析绑定并改写 payload 为绑定表索引，
            // 运行期直接整合栈上参数调用 FFI，绕过 SL 函数体/Library.Load 链路。
            RegisterCompileHandler("DllStaticImport", (attr, owner) =>
            {
                var args = attr.GetSplitStringArgs();
                if (args.Count < 2)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAttributeDllImportOwner,
                        $"DllStaticImport: 需要 (静态库名或别名, 符号名) 两个字符串实参, owner='{owner?.allName}'");
                    return;
                }
                Log.AddMetaCoreLog(LID.MetaCoreAttributeDllImportAttributeRegistered,
                    $"DllStaticImport: attribute registered on '{owner?.allName}' lib='{args[0]}' symbol='{args[1]}'" +
                    (args.Count >= 3 ? $" sig='{args[2]}'" : " (sig derived from function signature)"));
            });

            // Exclude: 编译前隔离标记（PreCompile 时点，ATTRIBUTE_DESIGN §6.9）
            //   @Exclude() 标注在 class/data/enum/成员变量/成员函数上 → 该符号整体
            //   跳过编译（不进 Meta/IR/module.json）；引用点由符号解析报编译 Error（Q2）。
            //   收编旧 @IF/@ELSE/@ENDIF 宏与已删除的 static if 编译期条件编译（Q3）。
            // v1 无条件形态：标注即跳过；平台级条件隔离请用工程级 compileFiles.ignore。
            RegisterPreCompileExcludeHandler("Exclude", (attr) => true);
        }

        #region Compile-Time Processing

        /// <summary>
        /// 解析所有 attribute：遍历全部 MetaClass 及其成员，调用 MetaAttribute.Parse()。
        /// 在 ClassManager 的 ParseInitMetaClassListThroughInheritance 之后调用。
        /// P2: 注册点扩展 —— 一并遍历 data（类级+方法级）与 enum（声明级）。
        /// </summary>
        public static void ParseAllAttributes()
        {
            foreach (var mc in ClassManager.instance.exportMetaClassList)
            {
                if (mc == null) continue;
                ParseAttributesForClass(mc);
            }
            foreach (var md in ClassManager.instance.exportMetaDataList)
            {
                if (md == null) continue;
                ParseAttributesForData(md);
            }
            foreach (var me in ClassManager.instance.exportMetaEnumList)
            {
                if (me == null) continue;
                ParseAttributesForEnum(me);
            }
        }

        /// <summary>解析单个 MetaClass 上及其成员的 attribute</summary>
        public static void ParseAttributesForClass(MetaClass mc)
        {
            if (mc == null) return;
            // 类级别的 attribute
            foreach (var attr in mc.attributeList)
            {
                if (attr == null) continue;
                attr.SetOwner(mc);
                attr.Parse();
            }
            // 成员函数
            foreach (var mmf in mc.nonStaticVirtualMetaMemberFunctionList)
            {
                if (mmf == null) continue;
                foreach (var attr in mmf.attributeList)
                {
                    if (attr == null) continue;
                    attr.SetOwner(mmf);
                    attr.Parse();
                }
            }
            foreach (var mmf in mc.staticMetaMemberFunctionList)
            {
                if (mmf == null) continue;
                foreach (var attr in mmf.attributeList)
                {
                    if (attr == null) continue;
                    attr.SetOwner(mmf);
                    attr.Parse();
                }
            }
            // 成员变量
            foreach (var mmv in mc.allMetaMemberVariableList)
            {
                if (mmv == null) continue;
                foreach (var attr in mmv.attributeList)
                {
                    if (attr == null) continue;
                    attr.SetOwner(mmv);
                    attr.Parse();
                }
            }
        }

        /// <summary>
        /// 解析单个 MetaData 上及其方法的 attribute（P2 注册点扩展：TargetData=2）。
        /// 注：data 字段为 MetaMemberData（不带 attributeList），字段级注册点暂不支持。
        /// </summary>
        public static void ParseAttributesForData(MetaData md)
        {
            if (md == null) return;
            foreach (var attr in md.attributeList)
            {
                if (attr == null) continue;
                attr.SetOwner(md);
                attr.Parse();
            }
            // 成员函数（当前语法 data 不允许成员函数，列表恒空，遍历安全）
            foreach (var mmf in md.nonStaticVirtualMetaMemberFunctionList)
            {
                if (mmf == null) continue;
                foreach (var attr in mmf.attributeList)
                {
                    if (attr == null) continue;
                    attr.SetOwner(mmf);
                    attr.Parse();
                }
            }
            foreach (var mmf in md.staticMetaMemberFunctionList)
            {
                if (mmf == null) continue;
                foreach (var attr in mmf.attributeList)
                {
                    if (attr == null) continue;
                    attr.SetOwner(mmf);
                    attr.Parse();
                }
            }
        }

        /// <summary>解析单个 MetaEnum 声明级的 attribute（P2 注册点扩展：TargetEnum=4）</summary>
        public static void ParseAttributesForEnum(MetaEnum me)
        {
            if (me == null) return;
            foreach (var attr in me.attributeList)
            {
                if (attr == null) continue;
                attr.SetOwner(me);
                attr.Parse();
            }
        }

        /// <summary>
        /// 执行编译时属性处理。
        /// 遍历所有 attribute，根据 stage 分发：
        /// - PreCompile (0) / Compiling (1): 执行 C# 侧注册的编译时处理器
        /// - Preload (2) / Runtime (3): 跳过（由导出层序列化，cvm 装配期/执行期处理）
        /// </summary>
        public static void ProcessCompileTimeAttributes()
        {
            int compileCount = 0;
            int runtimeCount = 0;

            foreach (var mc in ClassManager.instance.exportMetaClassList)
            {
                if (mc == null) continue;
                var (c, r) = ProcessClassAttributes(mc);
                compileCount += c;
                runtimeCount += r;
            }
            foreach (var md in ClassManager.instance.exportMetaDataList)
            {
                if (md == null) continue;
                var (c, r) = ProcessDataAttributes(md);
                compileCount += c;
                runtimeCount += r;
            }
            foreach (var me in ClassManager.instance.exportMetaEnumList)
            {
                if (me == null) continue;
                var (c, r) = ProcessAttributeList(me.attributeList, me);
                compileCount += c;
                runtimeCount += r;
            }

            Log.AddMetaCoreLog(LID.MetaCoreAttributeAttributeManagerProcessCompileTimeCompile,
                $"AttributeManager.ProcessCompileTime: compile={compileCount}, runtime={runtimeCount}");
        }

        /// <summary>处理单个 MetaClass 上及其成员的 attribute</summary>
        private static (int compile, int runtime) ProcessClassAttributes(MetaClass mc)
        {
            int compileCount = 0;
            int runtimeCount = 0;

            // 类级别
            var (c1, r1) = ProcessAttributeList(mc.attributeList, mc);
            compileCount += c1; runtimeCount += r1;

            // 成员函数
            foreach (var mmf in mc.nonStaticVirtualMetaMemberFunctionList)
            {
                if (mmf == null) continue;
                var (c, r) = ProcessAttributeList(mmf.attributeList, mmf);
                compileCount += c; runtimeCount += r;
            }
            foreach (var mmf in mc.staticMetaMemberFunctionList)
            {
                if (mmf == null) continue;
                var (c, r) = ProcessAttributeList(mmf.attributeList, mmf);
                compileCount += c; runtimeCount += r;
            }

            // 成员变量
            foreach (var mmv in mc.allMetaMemberVariableList)
            {
                if (mmv == null) continue;
                var (c, r) = ProcessAttributeList(mmv.attributeList, mmv);
                compileCount += c; runtimeCount += r;
            }

            return (compileCount, runtimeCount);
        }

        /// <summary>处理单个 MetaData 上及其方法的 attribute（P2 注册点扩展）</summary>
        private static (int compile, int runtime) ProcessDataAttributes(MetaData md)
        {
            int compileCount = 0;
            int runtimeCount = 0;

            var (c1, r1) = ProcessAttributeList(md.attributeList, md);
            compileCount += c1; runtimeCount += r1;

            foreach (var mmf in md.nonStaticVirtualMetaMemberFunctionList)
            {
                if (mmf == null) continue;
                var (c, r) = ProcessAttributeList(mmf.attributeList, mmf);
                compileCount += c; runtimeCount += r;
            }
            foreach (var mmf in md.staticMetaMemberFunctionList)
            {
                if (mmf == null) continue;
                var (c, r) = ProcessAttributeList(mmf.attributeList, mmf);
                compileCount += c; runtimeCount += r;
            }

            return (compileCount, runtimeCount);
        }

        /// <summary>处理单个 attribute 列表</summary>
        private static (int compile, int runtime) ProcessAttributeList(
            List<MetaAttribute> list, MetaBase owner)
        {
            if (list == null || list.Count == 0) return (0, 0);

            int compileCount = 0;
            int runtimeCount = 0;

            for (int i = 0; i < list.Count; i++)
            {
                var attr = list[i];
                if (attr == null || string.IsNullOrEmpty(attr.name)) continue;

                // 根据 stage 分发（编译期时点走 C# 处理器，运行期时点仅随 SLIR 导出）
                if (attr.attributeStage == MetaAttribute.StagePreCompile
                    || attr.attributeStage == MetaAttribute.StageCompiling)
                {
                    if (s_CompileHandlers.TryGetValue(attr.name, out var handler))
                    {
                        try
                        {
                            handler(attr, owner);
                            compileCount++;
                        }
                        catch (Exception ex)
                        {
                            Log.AddMetaCoreLog(LID.MetaCoreAttributeCompileAttributeAttr,
                                $"Compile attribute error: attr={attr.name} owner={owner?.allName} err={ex.Message}");
                        }
                    }
                    else
                    {
                        Log.AddMetaCoreLog(LID.MetaCoreAttributeCompileHandlerAttribute,
                            $"No compile handler for attribute '{attr.name}' on {owner?.allName}");
                    }
                }
                else // Preload (2) / Runtime (3)
                {
                    // 运行期属性不在编译时处理，由导出层序列化，cvm 装配期/执行期处理
                    runtimeCount++;
                }
            }

            return (compileCount, runtimeCount);
        }

        /// <summary>
        /// MemberVariableExpress 挂点统一分派：成员变量无初始化表达式时，按
        /// attributeList 依次尝试已注册的初值合成处理器，首个非 null 生效。
        /// 调用点 = MetaMemberVariable.CreateMetaExpress（ParseMemberExpress 步骤，
        /// 成员定义类型已收集，sig 可推导）。
        /// </summary>
        public static FileMetaBaseTerm TryCreateMemberVariableExpress(MetaMemberVariable mmv)
        {
            var list = mmv?.attributeList;
            if (list == null || list.Count == 0) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var attr = list[i];
                if (attr == null || string.IsNullOrEmpty(attr.name)) continue;
                if (!s_MemberVariableExpressHandlers.TryGetValue(attr.name, out var handler)) continue;
                var term = handler(attr, mmv);
                if (term != null) return term;
            }
            return null;
        }

        /// <summary>
        /// MemberFunctionInject 挂点统一分派：函数语句解析（CreateMetaSyntax）之后，
        /// 遍历 attributeList 执行已注册的注入处理器（如 DllImport 链头分派）。
        /// 调用点 = MetaMemberFunction.ParseStatements（CheckAllPathsReturn 之前，
        /// 注入语句须参与返回路径校验；异常保护同 ProcessAttributeList）。
        /// </summary>
        public static void ProcessMemberFunctionAttributes(MetaMemberFunction mmf)
        {
            var list = mmf?.attributeList;
            if (list == null || list.Count == 0) return;
            for (int i = 0; i < list.Count; i++)
            {
                var attr = list[i];
                if (attr == null || string.IsNullOrEmpty(attr.name)) continue;
                if (!s_MemberFunctionInjectHandlers.TryGetValue(attr.name, out var handler)) continue;
                try
                {
                    handler(attr, mmf);
                }
                catch (Exception ex)
                {
                    Log.AddMetaCoreLog(LID.MetaCoreAttributeCompileAttributeAttr,
                        $"MemberFunctionInject attribute error: attr={attr.name} owner={mmf?.allName} err={ex.Message}");
                }
            }
        }

        #endregion

    }
}
