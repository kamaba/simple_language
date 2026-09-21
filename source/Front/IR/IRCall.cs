//****************************************************************************
//  File:      IRCall.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage;
using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR.Types;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimpleLanguage.IR
{
    public class IRCallFunction : IRBase
    {
        public int paramCount { get; set; } = 0;
        public bool target { get; set; } = false;

        private MethodInfo m_MethodInfo = null;
        private IRMethod m_IRRuntimeMethod = null;

        public IRCallFunction(IRMethod _irMethod) : base(_irMethod)
        {
        }

        /// <summary>Prefer member <see cref="MetaFunction.token"/>; fallback to call site from <see cref="MetaMethodCall.token"/>.</summary>
        private void ApplyCallInstructionDebug(IRData datacall, MetaFunction mf, MetaMethodCall mfc)
        {
            var site = mf?.token ?? mfc?.metaMemberFunction?.token;
            string name = m_IRRuntimeMethod?.onlyFunctionName ?? mf?.name ?? "";
            string detail = string.IsNullOrEmpty(name)
                ? datacall.opCode.ToString()
                : $"{datacall.opCode} {name}";
            datacall.SetDebugInfoByToken(site, detail);
        }

        public void ParseSystemCall(MetaMethodCall mfc)
        {
            // Keep the same argument emission pipeline as regular calls.
            IRMetaType irmt = null;
            IRMetaClass owirmc = null;
            //if (mfc.loadMetaVariable != null)
            //{
            //    owirmc = IRManager.GetIRMetaClassByMetaVariable(mfc.loadMetaVariable);
            //    irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.loadMetaVariable.defineMetaType, owirmc);
            //    IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, owirmc, m_IRMethod, mfc.loadMetaVariable);
            //    AddIRRangeData(irload.IRDataList);
            //}

            var mf = mfc.GetTemplateMemberFunction();
            string systemName = mf?.name ?? string.Empty;
            int systemKind = -1;
            // Unique int id from the declaration (module "systemCalls"): the C VM
            // registers id -> implementation at load time and dispatches by id.
            int systemId = 0;
            SystemMethodCallDeclaration sysDecl = null;
            if (SystemMethodCallDeclarationRegistry.TryGetDeclaration(systemName, out sysDecl))
            {
                systemId = sysDecl.GetIndex();
            }

            paramCount = mfc.metaInputParamList.Count;
            for (int j = 0; j < paramCount; j++)
            {
                var argNode = mfc.metaInputParamList[j];
                IRExpressBase irexpress = IRExpressManager.CreateExpress(m_IRMethod, argNode);
                AddIRRangeData(irexpress.IRDataList);
                TryAddDataTypeLiteralFallback(argNode, irexpress);

                // systemCall 实参没有 VM 绑定矫正：C 侧直接按声明的形参类型 pop 栈槽。
                // 数值类型不匹配时（如 Float16 实参 -> Float32 形参）在调用前插入
                // IRConvert，保证栈上槽位与声明一致（与赋值路径 IRAssignStatements 同策略）。
                if (sysDecl != null && j < sysDecl.paramMetaTypeList.Count)
                {
                    EType argEType = CoreMetaClassManager.GetETypeByMetaClass(argNode.GetReturnMetaType()?.metaClass);
                    EType paramEType = CoreMetaClassManager.GetETypeByMetaClass(sysDecl.paramMetaTypeList[j]?.metaClass);
                    if (argEType != paramEType
                        && NumberManager.IsNumericEType(argEType)
                        && NumberManager.IsNumericEType(paramEType))
                    {
                        IRConvert irconv = new IRConvert(m_IRMethod, argEType, paramEType);
                        AddIRRangeData(irconv.IRDataList);
                    }
                }
            }
            var sysPkg = new SLSystemMethodCallPackage
            {
                name = systemName,
                paramCount = paramCount,
                systemMethodKind = systemKind,
                id = systemId,
            };

            IRData datacall2 = new IRData();
            datacall2.opCode = EIROpCode.CallSystemMethod;
            // Legacy bridge pops this many stack slots (args only); payload carries full metadata.
            datacall2.index = paramCount;
            datacall2.SetOpValue(sysPkg);
            datacall2.SetDebugInfoByToken(mf?.token ?? mfc?.metaMemberFunction?.token,
                string.IsNullOrEmpty(systemName) ? "CallSystemMethod" : $"CallSystemMethod {systemName}");
            AddIRData(datacall2);
        }
        public void Parse(MetaMethodCall mfc)
        {
            IRMetaType irmt = null;
            IRMetaClass owirmc = null;
            //if (mfc.loadMetaVariable != null)
            //{
            //    owirmc = IRManager.GetIRMetaClassByMetaVariable(mfc.loadMetaVariable);
            //    irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.loadMetaVariable.defineMetaType, owirmc);
            //    IRLoadVariable irload = IRLoadVariable.CreateLoadVariable(irmt, owirmc, m_IRMethod, mfc.loadMetaVariable);
            //    AddIRRangeData(irload.IRDataList);
            //}

            // ── Debug 采样门面特译 (DEBUG_SYSTEM_DESIGN v4 §7.2) ──
            // 必须先于实参发射循环: compile.debug 关闭时整句消除, 连实参 IR 都不发射。
            if (ParseDebugWatchCall(mfc))
                return;

            paramCount = mfc.metaInputParamList.Count;
            for (int j = 0; j < paramCount; j++)
            {
                var argNode = mfc.metaInputParamList[j];
                IRExpressBase irexpress = IRExpressManager.CreateExpress(m_IRMethod, argNode);
                AddIRRangeData(irexpress.IRDataList);
                TryAddDataTypeLiteralFallback(argNode, irexpress);
            }
            MetaFunction mf = mfc.GetTemplateMemberFunction();
            MetaMemberFunction mmf = mf as MetaMemberFunction;

            // ── inline 方法调用展开 (M1b §5.5(b)) ──
            // 语义层按普通函数规则 (上方实参 IR 已照常发射), IR 层把 Call 替换为
            // 被调函数体原位展开段, 栈效果与 Call 指令严格等价:
            // 弹 (isStatic?0:1)+paramCount 槽, 非 void 压回结果值。
            // M4 (-O 自动 inline): 无宿主函数 (字段初始化器等 CreateExpress(null,...) 场景)
            // 无法展开 (槽注册/ret 改写依赖宿主 IRMethod) —— 显式 inline 已被语义层 21458
            // 前置拦截, 自动 inline 该场景静默回退正常 Call (不产生用户可见错误)
            if (mmf != null && mmf.isInline && m_IRMethod != null)
            {
                if (ParseInlineMethodBodyReplacement(mfc, mmf))
                    return;
                // 自动 inline 环守卫命中 (间接递归 A->B->A): 静默回退正常 Call
            }
            
            //MetaMemberFunctionCSharp mmfcsharp = mf as MetaMemberFunctionCSharp;
            //if (mmfcsharp != null)
            //{
            //    m_MethodInfo = mmfcsharp.methodInfo;
            //    IRData data = new IRData();
            //    data.opCode = EIROpCode.Ca;
            //    data.opValue = this;
            //    data.SetDebugInfoByToken(mmfcsharp.GetToken());
            //    AddIRData(data);
            //    return;
            //}


            int callMethodIndex = -1;
            string fname = "";
            IRMetaClass irmc = null;
            List<IRMetaType> types = new List<IRMetaType>();

            int callType = -1;// 0->static call 1->virtual call  2->dynamic call
            if( mfc.staticCallMetaType != null || mmf.isStatic || mmf?.isFinal == true )
            {
                MetaClass scmc = null;
                MetaType staticMt = mfc.staticCallMetaType;
                if (staticMt == null )
                {
                    staticMt = new MetaType(mf.ownerMetaClass);
                }
                if (staticMt.metaClass != null )
                {
                    scmc = staticMt.metaClass;
                    if (scmc != null && scmc is MetaGenTemplateClass mgtc)
                    {
                        scmc = mgtc.metaTemplateClass;
                    }
                    irmc = IRManager.instance.GetIRMetaClassById(scmc.classId);
                }
                else if(staticMt.metaData != null )
                {
                    irmc = IRManager.instance.GetIRMetaClassById(CoreMetaClassManager.dataMetaClass.classId);
                }

                if (mf is MetaGenTemplateFunction mgtf)
                {
                    var srcFn = mgtf.sourceMetaMemberFunction ?? mgtf.sourceTemplateFunctionMetaMemberFunction;
                    fname = (srcFn ?? mgtf).functionAllName;
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(srcFn?.ownerMetaBase ?? mgtf.ownerMetaBase);
                }
                else if (mf is MetaMemberFunction mmf22)
                {
                    if (mmf22.sourceMetaMemberFunction != null)
                    {
                        fname = mmf22.sourceMetaMemberFunction.functionAllName;
                        owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf22.sourceMetaMemberFunction.ownerMetaBase);
                    }
                    else
                    {
                        fname = mmf22.functionAllName;
                        owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf22.ownerMetaBase);
                    }
                }
                else
                {
                    fname = mf.functionAllName;
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(mf.ownerMetaBase);
                }

                // 静态成员变量初始化表达式走 CreateExpress(null, ...)（见
                // IRMetaClass.CreateStaticMetaMetaVariableIRList），此时用全局
                // IRManager 单例查找目标方法，其余场景用 m_IRMethod.irManager。
                var irManagerForLookup = m_IRMethod != null ? m_IRMethod.irManager : IRManager.instance;
                m_IRRuntimeMethod = irManagerForLookup.GetIRMethod(fname);

                var list = staticMt.GetGenTemplateMetaTypeList();
                for (int i = 0; i < list.Count; i++)
                {
                    types.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(list[i], owirmc));
                }
                callType = 0;
            }
            else
            {
                MetaBase ownerBase = null;
                var mmf2 = mf as MetaMemberFunction;
                if (mmf2 != null)
                {
                    ownerBase = mmf2.sourceMetaMemberFunction?.ownerMetaBase ?? mmf2.ownerMetaBase;
                }
                else
                {
                    ownerBase = mf.ownerMetaBase;
                }
                fname = mf.virtualFunctionName;
                irmc = IRManager.GetIRMetaClassByMetaOwner(ownerBase);


                if (mf is MetaGenTemplateFunction mgtf)
                {
                    //fname = mgtf.sourceMetaMemberFunction.functionAllName;
                    var srcFn = mgtf.sourceMetaMemberFunction ?? mgtf.sourceTemplateFunctionMetaMemberFunction;
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(srcFn?.ownerMetaBase ?? mgtf.ownerMetaBase);
                }
                else if (mf is MetaMemberFunction mmf22)
                {
                    if (mmf22.sourceMetaMemberFunction != null)
                    {
                        //fname = mmf22.sourceMetaMemberFunction.functionAllName;
                        owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf22.sourceMetaMemberFunction.ownerMetaBase);
                    }
                    else
                    {
                        //fname = mmf22.functionAllName;
                        owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf22.ownerMetaBase);
                    }
                }
                else
                {
                    //fname = mf.functionAllName;
                    owirmc = IRManager.GetIRMetaClassByMetaOwner(mf.ownerMetaBase);
                }


                m_IRRuntimeMethod = irmc?.GetIRNonStaticMethodIndexByMethod(fname, out callMethodIndex);
                callType = 1;
                if (m_IRRuntimeMethod?.interfaceMethod == true)
                {
                    callType = 2;
                }
            }
            if (m_IRRuntimeMethod == null)
            {
                // 运算符重载方法（_add_/_eq_ 等）不进虚表：IRMetaClass 构建时按方法名分流到
                // operatorMethodList，GetIRNonStaticMethodIndexByMethod 在非静态方法表中查不到。
                // 类内显式调用（如 this._eq_(obj1)）在此按 functionAllName 从 IRManager 全局
                // 方法字典定位目标方法，并降级为静态调用（与 final 方法的调用方式一致）。
                var irManagerForLookup = m_IRMethod != null ? m_IRMethod.irManager : IRManager.instance;
                m_IRRuntimeMethod = mf != null ? irManagerForLookup?.GetIRMethod(mf.functionAllName) : null;
                if (m_IRRuntimeMethod == null && mf != null && IRManager.instance != irManagerForLookup)
                {
                    m_IRRuntimeMethod = IRManager.instance.GetIRMethod(mf.functionAllName);
                }
                if (m_IRRuntimeMethod != null)
                {
                    callType = 0;
                }
            }
            if (m_IRRuntimeMethod == null)
            {
                Log.AddIRLog(LID.IRCallNotFoundIrRuntime, mfc.token, $"ir runtime[{fname}] method not found!! func: {mf?.functionAllName ?? "null"}");
                return;
            }
            irmt = new IRMetaType(irmc, types);
            List<IRMetaType> functionMtList = new List<IRMetaType>();
            for( int i = 0; i < mfc.metaFunctionInputTemplateList.Count; i++ )
            {
                functionMtList.Add(IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mfc.metaFunctionInputTemplateList[i], owirmc));
            }
            bool tryCatch = m_IRMethod != null && m_IRMethod.isInTryCatch;
            var irmethodcall = new IRMethodCall(irmt, functionMtList, m_IRRuntimeMethod, paramCount, tryCatch);
            if(callType == 0 )
            {
                // @DllStaticImport 静态绑定 FFI 快速调用：目标函数声明带
                // DllStaticImport attribute 且本次为静态调用时，发射
                // CallFFIStatic(118)。payload 为 SLFFIStaticCallPackage（JSON），
                // cvm assembly build 期解析静态库绑定（lib+symbol+sig ->
                // FunctionHandle）并把 payload 改写为 4 字节绑定表索引，
                // 运行期直接整合栈上参数调用 FFI；绑定失败时运行期按
                // methodId 回退到原 SL 函数体（旧慢链路）。
                MetaAttribute staticAttr = null;
                if( mmf != null && mmf.attributeList != null )
                {
                    foreach( var attr in mmf.attributeList )
                    {
                        if( attr != null && attr.name == "DllStaticImport" )
                        {
                            staticAttr = attr;
                            break;
                        }
                    }
                }
                if( staticAttr != null )
                {
                    var sargs = staticAttr.GetSplitStringArgs();
                    string staticSig = sargs.Count >= 3 ? sargs[2]
                        : Core.MetaDefineVarStatements.BuildFFIFunctionSigFromMetaFunction( mmf );
                    if( sargs.Count >= 2 && !string.IsNullOrEmpty( staticSig ) )
                    {
                        var ffipkg = new SLFFIStaticCallPackage
                        {
                            lib = sargs[0],
                            symbol = sargs[1],
                            sig = staticSig,
                            methodId = ClassManager.GetMethodId(m_IRRuntimeMethod.id ?? string.Empty),
                            methodName = m_IRRuntimeMethod.onlyFunctionName ?? string.Empty,
                            paramCount = paramCount,
                            tryCatch = tryCatch,
                        };
                        IRData datacallffi = new IRData();
                        datacallffi.opCode = EIROpCode.CallFFIStatic;
                        datacallffi.SetOpValue(ffipkg);
                        datacallffi.index = paramCount;
                        ApplyCallInstructionDebug(datacallffi, mf, mfc);
                        AddIRData(datacallffi);
                        return;
                    }
                    Log.AddIRLog(LID.IRCallIssue, mfc.token,
                        $"DllStaticImport: 函数[{mmf.functionAllName}] 需要 (静态库名, 符号名 [, sig]) 实参且 sig 可推导, 回退 CallStatic!!");
                }
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallStatic;
                datacall.SetOpValue(irmethodcall);
                datacall.index = paramCount;
                ApplyCallInstructionDebug(datacall, mf, mfc);
                AddIRData(datacall);
            }
            else if( callType == 1 )
            {
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallVirt;
                datacall.index = callMethodIndex;
                datacall.SetOpValue(irmethodcall);
                ApplyCallInstructionDebug(datacall, mf, mfc);
                AddIRData(datacall);
            }
            else if( callType == 2  )
            {
                IRData datacall = new IRData();
                datacall.opCode = EIROpCode.CallDynamic;
                datacall.SetOpValue(irmethodcall);
                datacall.index = paramCount + 1;
                ApplyCallInstructionDebug(datacall, mf, mfc);
                AddIRData(datacall);
            }
            else
            {
                Log.AddIRLog(LID.IRCallIssue, mfc.token, "aaaa");
            }
        }

        // ────────────────────────────────────────────────────────────────
        // Monitor 采样门面特译 (DEBUG_SYSTEM_DESIGN v4 §7.2)
        // ────────────────────────────────────────────────────────────────

        /// <summary>Monitor 门面类 allName (Monitor.sl 顶层类挂 module "Core" 直下, 单层 allName)。</summary>
        private const string DebugFacadeClassAllName = "Core.Monitor";

        /// <summary>
        /// 拦截 Monitor 门面调用点并特译为采样指令 (v4 §7.2)。
        /// 特译发生在用户调用点而非门面体内的 SystemDebug* 系统调用处:
        /// label/member 定位参数只有在这里才是编译期字符串字面量, 可提取为
        /// strIdx 进 payload; 门面体内系统调用因此永远编译为死代码
        /// (Core.jsonc 的 SystemDebug* 注册仅供类型检查, VM 不实现)。
        /// P1 范围: watch 双重载 — 2 参 watch(value,label) → mode 0 (值监视),
        /// 3 参 watch(data,member,label) → mode 1 (成员监视)。
        /// P2 范围: begin(label) → DebugBegin(119) 8B payload / end(label) →
        /// DebugEnd(120) 4B payload — 区间 API 无被监视值, 实参不发射 IR。
        /// P3 范围: watchAssert(value,label,cond) → mode 2 (断言监视, 先发射
        /// cond 再发射 value, VM 弹栈先得 value 后得 cond §7.2-4) /
        /// watchIn(value,label,caller) → mode 3 (调用链过滤, caller 字面量
        /// → callerStrIdx 写 payload[14..18))。
        /// compile.debug 关闭 → 整句消除: 不发射任何 IR, 连实参求值一起消失 (§4.3)。
        /// label/member 非字符串字面量 → 打点告警并回退正常 Call
        /// (门面体系统调用在 VM 侧无 cvmFunction, 运行期报错兜底)。
        /// 返回 true = 已特译/已消除, 调用方不再走正常 Call 路径; false = 回退。
        /// </summary>
        private bool ParseDebugWatchCall(MetaMethodCall mfc)
        {
            var mf = mfc.GetTemplateMemberFunction();
            if (mf == null)
                return false;
            var mmf = mf as MetaMemberFunction;
            // 门面方法全部为静态形态
            if (mmf == null || !mmf.isStatic)
                return false;
            if (mf.ownerMetaClass?.allName != DebugFacadeClassAllName)
                return false;
            string methodName = mf.name ?? string.Empty;
            if (methodName != "watch" && methodName != "watchAssert" && methodName != "watchIn"
                && methodName != "begin" && methodName != "end")
                return false;

            paramCount = mfc.metaInputParamList.Count;
            // §4.3: compile.debug 关闭 → 采样调用点整体消除
            if (ProjectManager.config?.Compile?.Debug == false)
                return true;

            // P2 区间 API (§7.2): begin/end 单参 label, 无被监视值实参
            if (methodName == "begin" || methodName == "end")
            {
                if (paramCount != 1)
                {
                    Log.AddIRLog(LID.IRCallIssue, mfc.token,
                        $"Monitor.{methodName}: 区间 API 需恰好 1 个 label 字面量参数, 回退正常调用! 函数[{mf.functionAllName}]");
                    return false;
                }
                int intervalLabelStrIdx = GetDebugLiteralStringIndex(mfc, 0);
                if (intervalLabelStrIdx < 0)
                {
                    Log.AddIRLog(LID.IRCallIssue, mfc.token,
                        $"Monitor.{methodName}: label 必须是字符串字面量, 回退正常调用! 函数[{mf.functionAllName}]");
                    return false;
                }

                IRData dataInterval = new IRData();
                if (methodName == "begin")
                {
                    // payload 布局见 IROpEnum.cs 注释 (注释为权威):
                    // [labelStrIdx:4][line:4] — line 为前端留档, VM 不用
                    byte[] intervalPayload = new byte[8];
                    Buffer.BlockCopy(BitConverter.GetBytes(intervalLabelStrIdx), 0, intervalPayload, 0, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(mfc.token?.sourceBeginLine ?? 0), 0, intervalPayload, 4, 4);
                    dataInterval.opCode = EIROpCode.DebugBegin;
                    dataInterval.Payload = intervalPayload;
                }
                else
                {
                    // payload: [labelStrIdx:4]
                    byte[] intervalPayload = new byte[4];
                    Buffer.BlockCopy(BitConverter.GetBytes(intervalLabelStrIdx), 0, intervalPayload, 0, 4);
                    dataInterval.opCode = EIROpCode.DebugEnd;
                    dataInterval.Payload = intervalPayload;
                }
                // 手拼 payload: 不走 SetOpValue (会经 PackOpValue 重置 Payload)
                dataInterval.UpdateByteLength();
                dataInterval.SetDebugInfoByToken(mf.token ?? mfc.token,
                    methodName == "begin" ? $"DebugBegin label#{intervalLabelStrIdx}" : $"DebugEnd label#{intervalLabelStrIdx}");
                AddIRData(dataInterval);
                return true;
            }

            // 重载分流 (§7.1): watch 2 参 → mode 0; 3 参 → mode 1;
            // watchAssert 3 参 → mode 2; watchIn 3 参 → mode 3
            byte mode;
            if (methodName == "watchAssert")
            {
                if (paramCount != 3)
                {
                    Log.AddIRLog(LID.IRCallIssue, mfc.token,
                        $"Monitor.watchAssert: 需恰好 3 个参数 (value,label,cond), 回退正常调用! 函数[{mf.functionAllName}]");
                    return false;
                }
                mode = 2;
            }
            else if (methodName == "watchIn")
            {
                if (paramCount != 3)
                {
                    Log.AddIRLog(LID.IRCallIssue, mfc.token,
                        $"Monitor.watchIn: 需恰好 3 个参数 (value,label,caller), 回退正常调用! 函数[{mf.functionAllName}]");
                    return false;
                }
                mode = 3;
            }
            else
            {
                mode = (byte)(paramCount == 3 ? 1 : 0);
            }

            // 定位参数提取 (字面量 → strIdx): mode 0/2/3 → label = arg[1];
            // mode 1 → member = arg[1], label = arg[2]; mode 3 → caller = arg[2]
            int memberStrIdx = mode == 1
                ? GetDebugLiteralStringIndex(mfc, 1)
                : 0;
            int callerStrIdx = mode == 3
                ? GetDebugLiteralStringIndex(mfc, 2)
                : 0;
            int labelStrIdx = GetDebugLiteralStringIndex(mfc, mode == 1 ? 2 : 1);
            if (labelStrIdx < 0 || (mode == 1 && memberStrIdx < 0) || (mode == 3 && callerStrIdx < 0))
            {
                Log.AddIRLog(LID.IRCallIssue, mfc.token,
                    $"Monitor.{methodName}: 定位参数 (label/member/caller) 必须是字符串字面量, 回退正常调用! 函数[{mf.functionAllName}]");
                return false;
            }

            // 被监视值实参照常发射 (arg[0], 与正常 Call 同管线); label/member/caller 不发射 IR。
            // mode 2 求值顺序 (§7.2-4): 先发射 cond (arg[2]) 再发射 value (arg[0]) —
            // VM 弹栈先得 value 后得 cond, value 后压居栈顶。
            if (mode == 2)
            {
                var condNode = mfc.metaInputParamList[2];
                IRExpressBase condExpress = IRExpressManager.CreateExpress(m_IRMethod, condNode);
                AddIRRangeData(condExpress.IRDataList);
                TryAddDataTypeLiteralFallback(condNode, condExpress);
            }
            var valueNode = mfc.metaInputParamList[0];
            IRExpressBase irexpress = IRExpressManager.CreateExpress(m_IRMethod, valueNode);
            AddIRRangeData(irexpress.IRDataList);
            TryAddDataTypeLiteralFallback(valueNode, irexpress);

            // payload 布局见 IROpEnum.cs 注释 (注释为权威):
            // [targetKind:1][mode:1][line:4][labelStrIdx:4][memberStrIdx:4][callerStrIdx:4]
            // targetKind 固定 0; callerStrIdx 仅 mode 3 (watchIn) 使用。
            byte[] payload = new byte[18];
            payload[0] = 0; // targetKind
            payload[1] = mode;
            Buffer.BlockCopy(BitConverter.GetBytes(mfc.token?.sourceBeginLine ?? 0), 0, payload, 2, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(labelStrIdx), 0, payload, 6, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(memberStrIdx), 0, payload, 10, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(callerStrIdx), 0, payload, 14, 4);

            IRData datawatch = new IRData();
            datawatch.opCode = EIROpCode.DebugWatch;
            // 手拼 payload: 不走 SetOpValue (会经 PackOpValue 重置 Payload);
            // UsesIndex 白名单不含 DebugWatch, index 不嵌入 payload。
            datawatch.Payload = payload;
            datawatch.UpdateByteLength();
            datawatch.SetDebugInfoByToken(mf.token ?? mfc.token, $"DebugWatch mode={mode}");
            AddIRData(datawatch);
            return true;
        }

        /// <summary>提取 <paramref name="argIndex"/> 实参的字符串字面量并注册进字符串表; 非字面量返回 -1。</summary>
        private int GetDebugLiteralStringIndex(MetaMethodCall mfc, int argIndex)
        {
            if (argIndex >= mfc.metaInputParamList.Count)
                return -1;
            var constNode = mfc.metaInputParamList[argIndex] as MetaConstExpressNode;
            if (constNode == null || constNode.eType != EType.String)
                return -1;
            return IRManager.instance.AddStringIRStack(constNode.value?.ToString() ?? string.Empty);
        }

        /// <summary>
        /// inline 方法调用展开 (M1b §5.5(b)): 实参 IR 已由 Parse 循环照常发射,
        /// 此处把 Call 替换为被调函数体的原位展开段。发射序列:
        ///   [实参逆序 StoreLocal 形参槽][this StoreLocal][result 初始化]
        ///   [函数体重放][段尾锚][非 void: LoadLocal 结果槽]
        /// 栈效果: 弹 (isStatic?0:1)+paramCount 槽, 非 void 压回结果值 — 与 CallStatic 严格等价。
        /// 形参/this/result/体内变量全部注册进宿主局部表 (每次展开独立槽位);
        /// 体内 ret 改写 (IRReturnStatements) 与形参 Argument 回退 (IRVariable)
        /// 都以 IRMethod 展开栈顶上下文为准。
        /// 返回 false = 调用方应回退正常 Call (M4 自动 inline 静默回退路径)。
        /// </summary>
        private bool ParseInlineMethodBodyReplacement(MetaMethodCall mfc, MetaMemberFunction mmf)
        {
            // 环守卫: 间接递归 A->B->A 会无限展开 (与自引用同一违禁, LID 21452)
            // M4: 显式 inline 报错 (保持原行为); 自动 inline 静默回退正常 Call
            if (m_IRMethod.IsInInlineExpansion(mmf))
            {
                if (mmf.isInlineExplicit)
                {
                    Log.AddIRLog(LID.MetaCoreInlineLambdaBodyForbiddenSelfReference, mmf.token,
                        "inline 方法循环展开: " + mmf.functionAllName);
                    return true;
                }
                return false;
            }

            // 1. 形参槽注册 (宿主局部表): 体内读形参经 Argument 回退查到这里的槽
            var paramSlots = new List<IRMetaVariable>();
            var plist = mmf.metaMemberParamCollection.metaDefineParamList;
            for (int j = 0; j < plist.Count; j++)
            {
                var tmv = plist[j]?.metaVariable;
                if (tmv == null) continue;
                var slot = new IRMetaVariable(tmv, m_IRMethod.methodLocalVariableList.Count);
                m_IRMethod.methodLocalVariableList.Add(slot);
                paramSlots.Add(slot);
            }
            // this 槽 (实例方法): thisMetaVariable 是 EVariableFrom.Argument, 同样走回退
            IRMetaVariable thisSlot = null;
            if (!mmf.isStatic && mmf.thisMetaVariable != null)
            {
                thisSlot = new IRMetaVariable(mmf.thisMetaVariable, m_IRMethod.methodLocalVariableList.Count);
                m_IRMethod.methodLocalVariableList.Add(thisSlot);
            }

            // 2. 实参绑定 (逆序 StoreLocal): eval 栈顶是最后实参先出栈;
            //    实例方法 receiver 在栈底, 最后绑定。形参个数以实参为准 (语义层保证一致)
            int bindCount = Math.Min(paramCount, paramSlots.Count);
            for (int j = bindCount - 1; j >= 0; j--)
            {
                IRStoreVariable storeArg = new IRStoreVariable(null, m_IRMethod,
                    paramSlots[j].index, IRMetaVariableFrom.LocalStatement);
                AddIRRangeData(storeArg.IRDataList);
            }
            if (thisSlot != null)
            {
                IRStoreVariable storeThis = new IRStoreVariable(null, m_IRMethod,
                    thisSlot.index, IRMetaVariableFrom.LocalStatement);
                AddIRRangeData(storeThis.IRDataList);
            }

            // 3. 体预注册 (幂等: GetCalcMetaVariableList 每次新建列表纯收集, 含 result 变量)
            var calcVars = mmf.GetCalcMetaVariableList();
            for (int i = 0; i < calcVars.Count; i++)
            {
                if (calcVars[i] == null) continue;
                m_IRMethod.methodLocalVariableList.Add(
                    new IRMetaVariable(calcVars[i], m_IRMethod.methodLocalVariableList.Count));
            }

            // 4. result 初始化: 函数含 result 变量时先 NewObject, 体内才可读写
            //    (蓝本 IRMethod.GenerateResultPrologue)
            if (mmf.hasResultVariable)
            {
                ParseInlineResultPrologue(mmf);
            }

            // 5. 结果槽合成 (非 void): 不能复用 returnMetaVariable (EVariableFrom.None, 不进任何表)
            IRMetaVariable resultSlot = null;
            var retMt = mmf.returnMetaVariable?.GetFinalMetaType();
            bool isVoidReturn = retMt == null || retMt.metaClass == CoreMetaClassManager.voidMetaClass;
            if (!isVoidReturn)
            {
                var slotMv = new MetaVariable(
                    m_IRMethod.id + ".inline." + (mmf.name ?? "func") + ".result",
                    MetaVariable.EVariableFrom.LocalStatement, null, mmf.ownerMetaClass, retMt);
                resultSlot = new IRMetaVariable(slotMv, m_IRMethod.methodLocalVariableList.Count);
                m_IRMethod.methodLocalVariableList.Add(resultSlot);
            }

            // 6. 压入展开上下文并重放函数体: 体内 ret 改写/形参回退以栈顶上下文为准
            IRNop endAnchor = new IRNop(m_IRMethod, mmf.token, "inline end: " + mmf.name);
            var ctx = new IRMethod.InlineExpansionContext
            {
                endAnchorData = endAnchor.data,
                resultSlot = resultSlot,
                inlineMmf = mmf,
            };
            m_IRMethod.PushInlineExpansion(ctx);
            try
            {
                IRBlockStatements irbs = new IRBlockStatements(m_IRMethod);
                irbs.ParseAllIRStatements(mmf.metaBlockStatements);
                foreach (var irst in irbs.irStatements)
                {
                    if (irst == null) continue;
                    AddIRRangeData(irst.IRDataList);
                }
            }
            finally
            {
                m_IRMethod.PopInlineExpansion();
            }

            // 6.5 result 兜底 epilogue (蓝本 IRMethod.GenerateResultEpilogue):
            //     函数非所有路径显式 ret 时, 自然结束路径需 [Load result][StoreLocal 结果槽]
            //     (StoreReturn 改写为写展开结果槽); 显式 ret 路径已 BrLabel 跳过此段直达锚,
            //     两条路径在锚点汇合时结果槽均持有 result 对象。
            if (resultSlot != null && mmf.hasResultVariable && !mmf.isBlockAlwaysReturn)
            {
                var rmv = mmf.resultVariable;
                var mt = rmv.GetFinalMetaType();
                var irmc = IRManager.GetIRMetaClassByMetaType(mt);
                if (irmc != null)
                {
                    IRLoadVariable loadResult = IRLoadVariable.CreateLoadVariable(new IRMetaType(irmc), irmc, m_IRMethod, rmv);
                    AddIRRangeData(loadResult.IRDataList);

                    IRStoreVariable storeResult = new IRStoreVariable(null, m_IRMethod,
                        resultSlot.index, IRMetaVariableFrom.LocalStatement);
                    AddIRRangeData(storeResult.IRDataList);
                }
            }

            // 7. 段尾锚: 改写后的 ret BrLabel 全部跳到这里;
            //    IRMethod.Parse 末尾回填循环会把锚改写为 Label 指令
            AddIRData(endAnchor.data);

            // 8. 非 void: 压回结果值, 供调用方消费 (与 Call 后栈顶为返回值一致)
            if (resultSlot != null)
            {
                IRLoadVariable loadResult = new IRLoadVariable(null, m_IRMethod,
                    resultSlot.index, IRMetaVariableFrom.LocalStatement);
                AddIRRangeData(loadResult.IRDataList);
            }
            return true;
        }

        /// <summary>
        /// inline 展开段的 result 变量初始化 (蓝本 IRMethod.GenerateResultPrologue):
        ///   [NewObject/NewTemplateObject][StoreLocal result]
        /// result 变量经体预注册进入宿主局部表, Store 走工厂方法按 LocalStatement 路由。
        /// </summary>
        private void ParseInlineResultPrologue(MetaMemberFunction mmf)
        {
            var rmv = mmf.resultVariable;
            var mt = rmv.GetFinalMetaType();
            if (mt == null) return;

            var owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf.ownerMetaBase);
            IRNew irNew;
            if (mt.GetTemplateMetaClass() == CoreMetaClassManager.resultTMetaClass)
            {
                var irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mt, owirmc);
                irNew = new IRNew(m_IRMethod, irmt);
            }
            else
            {
                var irmc = IRManager.GetIRMetaClassByMetaType(mt);
                if (irmc == null) return;
                irNew = new IRNew(m_IRMethod, irmc);
            }
            AddIRRangeData(irNew.IRDataList);

            var storeIrmc = IRManager.GetIRMetaClassByMetaType(mt);
            var storeIrmt = storeIrmc != null ? new IRMetaType(storeIrmc) : null;
            IRStoreVariable storeResult = IRStoreVariable.CreateIRStoreVariable(storeIrmt, storeIrmc, m_IRMethod, rmv);
            if (storeResult != null)
            {
                AddIRRangeData(storeResult.IRDataList);
            }
        }

        public override string ToIRString()
        {
            return base.ToIRString();
        }

        private void TryAddDataTypeLiteralFallback(MetaExpressNodeBase? argNode, IRExpressBase? irexpress)
        {
            if (argNode == null || irexpress == null)
                return;

            if (irexpress.IRDataList.Count > 0)
                return;

            var metaType = argNode.expressReturnMetaType;
            if (argNode is MetaCallLinkExpressNode callLinkNode)
            {
                var metaCallLink = callLinkNode.metaCallLink;
                metaType = metaCallLink?.finalCallNode?.GetMetaType() ?? metaType;
                if (metaCallLink?.visitNodeList != null)
                {
                    for (int i = metaCallLink.visitNodeList.Count - 1; i >= 0; i--)
                    {
                        var visitMetaType = metaCallLink.visitNodeList[i]?.GetMetaType();
                        if (visitMetaType?.metaData != null)
                        {
                            metaType = visitMetaType;
                            break;
                        }
                    }
                }
            }

            var targetMetaData = metaType?.metaData;
            if (targetMetaData == null)
                return;

            int ownerHashCode = argNode.ownerMetaBase != null
                ? argNode.ownerMetaBase.classId
                : targetMetaData.classId;
            var ownerIrMetaClass = IRManager.instance.GetIRMetaClassById(ownerHashCode);
            if (ownerIrMetaClass == null)
                return;

            var irMetaType = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(metaType, ownerIrMetaClass);
            var irdata = new IRData();
            irdata.opCode = EIROpCode.LoadConstType;
            irdata.SetOpValue(irMetaType);
            irdata.SetDebugInfoByToken(argNode.token, "LoadConstType data literal fallback");
            AddIRData(irdata);
        }
    }
}
