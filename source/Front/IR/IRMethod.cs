//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR.Types;
using SimpleLanguage.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRMethod
    {
        public string id { get; set; } = "";
        public string virtualFunctionName { get; set; } = "";
        public string onlyFunctionName { get; set; } = "";
        public bool interfaceMethod => m_InterfaceMethod;
        /// <summary>ref module 方法修饰符（从 SLMethodPackage.flags 还原），用于反向构建 MetaMemberFunction。
        /// isOverrideInterface 复用 interfaceMethod（flags bit 16 == isOverrideInterface）。</summary>
        public bool isStatic => m_IsStatic;
        public bool isFinal => m_IsFinal;
        public bool isAbstract => m_IsAbstract;
        public bool isOverrideFunction => m_IsOverrideFunction;
        public bool isExtendParams => m_IsExtendParams;
        /// <summary>是否标注了 @AOT() 属性（AOT 预编译候选，阶段1先支持非模板静态成员函数）。
        /// 本地编译从 MetaMemberFunction.attributeList 提取（name=="AOT"），
        /// ref module 从 SLMethodPackage.flags bit 256 还原。</summary>
        public bool isAot => m_IsAot;
        /// <summary>声明该方法的类的 classId（来自 SLMethodPackage.declaringClassId）。
        /// 对于继承到子类的方法，指向声明类（如 Object）。0 表示未设置（按当前类处理）。</summary>
        public int declaringClassId => m_DeclaringClassId;
        /// <summary>是否为模板函数（fun&lt;T&gt;()）。导出时从 MetaMemberFunction.isTemplateFunction 读取，
        /// 导入时从 SLMethodPackage.isTemplateFunction 还原。</summary>
        public bool isTemplateFunction => m_IsTemplateFunction;
        /// <summary>模板函数的模板参数名列表，导入时用于重建 MetaTemplate。</summary>
        public List<string> templateParameterNames => m_TemplateParameterNames;
        public IRManager irManager => m_IRManager;
        public IRData funEndLabelData => m_FunEndLabelData;
        public IRMetaClass irOwnerMetaClass => m_IROwnerMetaClass;
        /// <summary>Bound MetaFunction; used as fallback source token for synthesized IRData debug info.</summary>
        public MetaFunction bindMetaFunction => m_BindMetaFunction;
        //private List<IRMetaVariable> methodInputTemplateObject => m_MethodInputTemplateObject;
        public List<IRMetaVariable> methodArgumentList => m_MethodArgumentList;
        public List<IRMetaVariable> methodLocalVariableList => m_MethodLocalVariableList;
        public List<IRMetaVariable> methodReturnVariableList => m_MethodReturnList;
        public List<IRData> IRDataList => m_IRDataList;

        //private List<IRMetaVariable> m_MethodInputTemplateObject = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_MethodArgumentList = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_MethodLocalVariableList = new List<IRMetaVariable>();
        private List<IRMetaVariable> m_MethodReturnList = new List<IRMetaVariable>();
        private List<IRData> m_LabelList = new List<IRData>();
        private List<IRData> m_IRDataList = new List<IRData>();
        /// <summary>
        /// goto/label 目标注册表: label 名 -> 目标 IRData。
        /// 同一函数内同名的 label 语句与 goto 语句共享同一目标实例,
        /// 支持前向跳转时 goto 先创建占位、label 语句处发射同一实例。
        /// </summary>
        private Dictionary<string, IRData> m_GotoLabelTargetDict = new Dictionary<string, IRData>();
        /// <summary>switch 跳转表延迟构建列表：指令编号完成后由 BuildPendingSwitchPayloads() 序列化。</summary>
        private List<IRSwitchStatements.PendingSwitchTable> m_PendingSwitchTableList = new List<IRSwitchStatements.PendingSwitchTable>();
        private Stack<IRData> m_BreakTargetStack = new Stack<IRData>();
        private Stack<IRData> m_ContinueTargetStack = new Stack<IRData>();
        private MetaFunction m_BindMetaFunction = null;
        private IRMetaClass m_IROwnerMetaClass = null;
        private bool m_InterfaceMethod = false;
        private bool m_IsStatic = false;
        private bool m_IsFinal = false;
        private bool m_IsAbstract = false;
        private bool m_IsOverrideFunction = false;
        private bool m_IsExtendParams = false;
        private bool m_IsAot = false;
        private int m_DeclaringClassId = 0;
        private bool m_IsTemplateFunction = false;
        private List<string> m_TemplateParameterNames = new List<string>();
        private IRData m_FunEndLabelData = null;
        private IRManager m_IRManager = null;

        /// <summary>
        /// 编译期上下文标记：当前是否在 try 表达式内部生成 IR。
        /// IRCallFunction.Parse 读取此标记，将 tryCatch 写入 call 指令。
        /// </summary>
        public bool isInTryCatch { get; set; } = false;

        /// <summary>
        /// 导出名称列表（含原名 + Nickname 别名）。
        /// 从 MetaMemberFunction 的 attribute 中收集 @Nickname 得到。
        /// </summary>
        public List<string> exportNameList => m_ExportNameList;
        private List<string> m_ExportNameList = new List<string>();

        /// <summary>导出用的合并名称（逗号分隔），供 SLIR 序列化使用。</summary>
        public string exportNames => m_ExportNameList.Count <= 1 ? null : string.Join(",", m_ExportNameList);

        public IRMethod(IRManager irma, MetaFunction func )
        {
            m_IRManager = irma;
            m_BindMetaFunction = func;
            this.id = func.functionAllName;
            this.virtualFunctionName = func.virtualFunctionName;
            this.onlyFunctionName = func.name;
            m_IROwnerMetaClass = IRManager.GetIRMetaClassByMetaOwner(func.ownerMetaBase);

            if( func is MetaMemberFunction mmf )
            {
                m_InterfaceMethod = mmf.isOverrideInterface;
                m_IsStatic = mmf.isStatic;
                m_IsTemplateFunction = mmf.isTemplateFunction;
                m_IsAot = HasAotAttribute(mmf);
                if (m_IsTemplateFunction && mmf.metaMemberTemplateCollection?.metaTemplateList != null)
                {
                    foreach (var mt in mmf.metaMemberTemplateCollection.metaTemplateList)
                    {
                        m_TemplateParameterNames.Add(mt?.name ?? string.Empty);
                    }
                }
            }
            m_FunEndLabelData = new IRData();
            m_FunEndLabelData.opCode = EIROpCode.Label;
            m_FunEndLabelData.SetDebugInfoByToken(func?.token, "FunEndLabel");

            // 收集成员函数的 @Nickname 别名
            if (func is MetaMemberFunction mmf2)
                CollectExportNames(mmf2);
        }

        /// <summary>
        /// 从 MetaMemberFunction 的 attribute 中收集 @Nickname 别名，
        /// 加上原名组成 exportNameList。
        /// </summary>
        private void CollectExportNames(MetaMemberFunction mmf)
        {
            m_ExportNameList.Clear();
            if (mmf == null) return;
            // 原名（短名）
            if (!string.IsNullOrEmpty(mmf.name))
                m_ExportNameList.Add(mmf.name);
            // 收集 @Nickname
            var attrs = mmf.attributeList;
            if (attrs == null) return;
            foreach (var attr in attrs)
            {
                if (attr == null || attr.name != "Nickname") continue;
                string nickname = attr.GetStringArg(0);
                if (!string.IsNullOrEmpty(nickname) && !m_ExportNameList.Contains(nickname))
                    m_ExportNameList.Add(nickname);
            }
        }

        /// <summary>
        /// 判断 MetaMemberFunction 是否标注了 @AOT() 属性。
        /// </summary>
        private static bool HasAotAttribute(MetaMemberFunction mmf)
        {
            var attrs = mmf?.attributeList;
            if (attrs == null) return false;
            foreach (var attr in attrs)
            {
                if (attr != null && attr.name == "AOT")
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 从导出的 SLMethodPackage 直接构建 IRMethod，用于 ref module 导入。
        /// 只构建签名（参数、返回值），不包含函数体 IR 指令。
        /// </summary>
        public IRMethod(IRManager irma, SLMethodPackage mp, IRMetaClass ownerIRMc)
        {
            m_IRManager = irma;
            m_BindMetaFunction = null; // ref module 方法不绑定 MetaFunction
            this.id = mp?.id ?? "";
            this.onlyFunctionName = mp?.name ?? "";
            this.virtualFunctionName = ComputeVirtualFunctionName(mp);
            m_IROwnerMetaClass = ownerIRMc;
            m_InterfaceMethod = mp?.interfaceMethod ?? false;
            // SLMethodPackage flags: 1=static, 2=final, 4=abstract, 8=overrideFunction,
            // 16=overrideInterface(==interfaceMethod), 32=canRewrite, 64=constructInit, 128=extendParams,
            // 256=aot(@AOT() 标记)
            var flags = mp?.flags ?? 0;
            m_IsStatic = (flags & 1) != 0;
            m_IsFinal = (flags & 2) != 0;
            m_IsAbstract = (flags & 4) != 0;
            m_IsOverrideFunction = (flags & 8) != 0;
            m_IsExtendParams = (flags & 128) != 0;
            m_IsAot = (flags & 256) != 0;
            m_DeclaringClassId = mp?.declaringClassId ?? 0;
            m_IsTemplateFunction = mp?.isTemplateFunction ?? false;
            if (m_IsTemplateFunction && mp?.templateParameterNames != null)
            {
                foreach (var tn in mp.templateParameterNames)
                {
                    m_TemplateParameterNames.Add(tn ?? string.Empty);
                }
            }
            m_FunEndLabelData = new IRData();
            m_FunEndLabelData.opCode = EIROpCode.Label;

            // 构建参数列表
            if (mp?.argumentList != null)
            {
                foreach (var vp in mp.argumentList)
                {
                    if (vp == null) continue;
                    var irmt = IRMetaType.CreateFromPackage(vp.typeDef, ownerIRMc);
                    var imv = new IRMetaVariable(vp, irmt, IRMetaVariableFrom.Argument);
                    m_MethodArgumentList.Add(imv);
                }
            }
            // 构建返回值列表
            if (mp?.returnList != null)
            {
                foreach (var vp in mp.returnList)
                {
                    if (vp == null) continue;
                    var irmt = IRMetaType.CreateFromPackage(vp.typeDef, ownerIRMc);
                    var imv = new IRMetaVariable(vp, irmt, IRMetaVariableFrom.Return);
                    m_MethodReturnList.Add(imv);
                }
            }
            // 构建局部变量列表
            if (mp?.localList != null)
            {
                foreach (var vp in mp.localList)
                {
                    if (vp == null) continue;
                    var irmt = IRMetaType.CreateFromPackage(vp.typeDef, ownerIRMc);
                    var imv = new IRMetaVariable(vp, irmt, IRMetaVariableFrom.LocalStatement);
                    m_MethodLocalVariableList.Add(imv);
                }
            }
        }

        /// <summary>
        /// 从 SLMethodPackage 数据计算 virtualFunctionName，
        /// 格式与 MetaMemberFunction.UpdateVritualFunctionName 一致：
        /// {name}_{returnType}_{paramCount}_{paramType1}_{paramType2}...
        /// </summary>
        private static string ComputeVirtualFunctionName(SLMethodPackage mp)
        {
            if (mp == null) return "";
            var sb = new StringBuilder();
            sb.Append(mp.name);
            sb.Append("_");
            // 返回类型
            string retType = "Core.void";
            if (mp.returnList != null && mp.returnList.Count > 0 && mp.returnList[0]?.typeDef != null)
                retType = mp.returnList[0].typeDef.className;
            if (string.IsNullOrEmpty(retType)) retType = "Core.void";
            sb.Append(retType);
            sb.Append("_");
            // 参数数量：非静态方法第一个参数是 this，不计入
            bool isStatic = (mp.flags & 1) != 0;
            int argCount = mp.argumentList?.Count ?? 0;
            int paramCount = isStatic ? argCount : Math.Max(0, argCount - 1);
            sb.Append(paramCount);
            if (paramCount > 0)
            {
                sb.Append("_");
                int startIdx = isStatic ? 0 : 1;
                int added = 0;
                for (int i = startIdx; i < argCount; i++)
                {
                    var arg = mp.argumentList[i];
                    if (arg?.typeDef == null) continue;
                    if (added > 0) sb.Append("_");
                    sb.Append(arg.typeDef.className);
                    added++;
                }
            }
            return sb.ToString();
        }
        public void ParseArgumentsOnly()
        {
            var mf = m_BindMetaFunction;
            // ref module 方法（从 package 直接构建）无需通过 MetaFunction 解析参数
            if (mf == null) return;

            if (mf.thisMetaVariable != null)
            {
                IRMetaVariable imp = new IRMetaVariable(mf.thisMetaVariable, 0);
                m_MethodArgumentList.Add(imp);
            }
            if (mf.returnMetaVariable!=null)
            {
                IRMetaVariable imp = new IRMetaVariable(mf.returnMetaVariable, 0);
                m_MethodReturnList.Add(imp);
            }
            var list2 = mf.metaMemberParamCollection.metaDefineParamList;
            for( int i = 0; i < list2.Count; i++ )
            {
                MetaDefineParam mdp = list2[i];
                if (mdp == null) continue;
                var tmv = mdp.metaVariable;
                IRMetaVariable imp = new IRMetaVariable(tmv, m_MethodArgumentList.Count);
                imp.SetHasExpress(mdp.isHasExpress);
                m_MethodArgumentList.Add(imp);
            }
        }
        public void Parse()
        {
            var mf = m_BindMetaFunction;

            // ref module 方法（从 package 直接构建，无 MetaFunction 绑定）不需要解析
            if (mf == null)
                return;

            var id2 = this.id;
            var vfn = mf.virtualFunctionName;

            ParseArgumentsOnly();

            // ref module 函数的 IR body 已在编译后的模块中，不需要重新解析
            if (mf.refFromType == RefFromType.RefModule)
                return;

            var list = mf.GetCalcMetaVariableList();
            for( int i = 0; i < list.Count; i++ )
            {
                var irsd = new IRMetaVariable(list[i], m_MethodLocalVariableList.Count);
                m_MethodLocalVariableList.Add(irsd);
            }

            var mmf = mf;
            MetaBlockStatements mbs = mmf.metaBlockStatements;
            if (mbs == null)
            {
                Debug.Write("----------------  Info 空函数!! --------------------");
                return;
            }

            // ── defer / errdefer wrapping ──
            bool hasDefer = mf.deferStatementsList != null && mf.deferStatementsList.Count > 0;
            bool hasErrDefer = mf.errDeferStatementsList != null && mf.errDeferStatementsList.Count > 0;

            // If defer exists, redirect funEndLabelData so all returns jump to defer cleanup first
            IRData originalFunEndLabel = m_FunEndLabelData;
            IRNop deferCleanupNop = null;
            if (hasDefer)
            {
                deferCleanupNop = new IRNop(this);
                m_FunEndLabelData = deferCleanupNop.data;
            }

            // Generate IR for function body
            IRBlockStatements irbs = new IRBlockStatements(this);
            irbs.ParseAllIRStatements(mbs);

            // ── 闭包共享捕获上下文 prologue ──
            // 宿主函数体内定义过闭包时, 在函数体最前部插入:
            //   AllocClosureContext N -> StoreLocal __closure_ctx__
            //   + 参数型捕获(参数/this)初始化: LoadArgument argIdx -> LoadLocal ctx -> StoreArrayIndex slot
            // 局部变量捕获的槽位保持 null, 其声明/赋值语句经 IRVariable 拦截后自然写入数组槽
            List<IRBase> closurePrologue = GenerateClosureContextPrologue();
            if (closurePrologue != null && closurePrologue.Count > 0)
            {
                irbs.irStatements.InsertRange(0, closurePrologue);
            }

            // ── result 关键字 prologue ──
            // 返回类型为 Result/Result<T> 的函数, 函数体最前部插入:
            //   NewObject/NewTemplateObject -> StoreLocal result
            // (VM 创建对象时自动执行字段默认值初始化, 无需调用 _init_)
            List<IRBase> resultPrologue = GenerateResultPrologue();
            if (resultPrologue != null && resultPrologue.Count > 0)
            {
                irbs.irStatements.InsertRange(0, resultPrologue);
            }

            // Build final IRBase list (optionally wrapped in try/catch for errdefer)
            List<IRBase> finalStatements = new List<IRBase>();

            if (hasErrDefer)
            {
                IRNop errdeferNop = new IRNop(this);

                // BeginTry (catch = errdefer handler, no finally)
                IRData beginTryData = new IRData();
                beginTryData.opCode = EIROpCode.BeginTry;
                TryScopeData tsd = new TryScopeData();
                tsd.catchTarget = errdeferNop.data;
                tsd.finallyTarget = null;
                beginTryData.SetOpValue(tsd);
                finalStatements.Add(new IRRawData(this, beginTryData));

                // Function body
                finalStatements.AddRange(irbs.irStatements);

                // LeaveTry (normal exit -> defer cleanup or real end)
                IRData leaveTryData = new IRData();
                leaveTryData.opCode = EIROpCode.LeaveTry;
                leaveTryData.SetOpValue(hasDefer ? deferCleanupNop.data : originalFunEndLabel);
                finalStatements.Add(new IRRawData(this, leaveTryData));

                // Errdefer handler (catch target)
                finalStatements.Add(errdeferNop);
                for (int i = mf.errDeferStatementsList.Count - 1; i >= 0; i--)
                {
                    var ed = mf.errDeferStatementsList[i];
                    if (ed.errDeferBlockStatements == null) continue;
                    IRBlockStatements irED = new IRBlockStatements(this);
                    irED.ParseIRStatements(ed.errDeferBlockStatements);
                    finalStatements.AddRange(irED.irStatements);
                }

                // If defer also exists, run defer blocks before re-throwing
                if (hasDefer)
                {
                    for (int i = mf.deferStatementsList.Count - 1; i >= 0; i--)
                    {
                        var d = mf.deferStatementsList[i];
                        if (d.deferBlockStatements == null) continue;
                        IRBlockStatements irD = new IRBlockStatements(this);
                        irD.ParseIRStatements(d.deferBlockStatements);
                        finalStatements.AddRange(irD.irStatements);
                    }
                }

                // Re-throw (exception propagates after errdefer cleanup)
                IRData throwData = new IRData();
                throwData.opCode = EIROpCode.Throw;
                finalStatements.Add(new IRRawData(this, throwData));
            }
            else
            {
                finalStatements.AddRange(irbs.irStatements);
            }

            // Add all final statements to m_IRDataList
            for (int i = 0; i < finalStatements.Count; i++)
            {
                if (finalStatements[i] == null) continue;
                for (int j = 0; j < finalStatements[i].IRDataList.Count; j++)
                {
                    var addIR = finalStatements[i].IRDataList[j];
                    addIR.id = m_IRDataList.Count;
                    AddLabelDict(addIR);
                    m_IRDataList.Add(addIR);
                }
            }

            // Defer cleanup section (runs on normal return / function end)
            if (hasDefer)
            {
                // Defer cleanup label
                for (int j = 0; j < deferCleanupNop.IRDataList.Count; j++)
                {
                    var addIR = deferCleanupNop.IRDataList[j];
                    addIR.id = m_IRDataList.Count;
                    AddLabelDict(addIR);
                    m_IRDataList.Add(addIR);
                }

                // Defer blocks in reverse (LIFO) order
                for (int i = mf.deferStatementsList.Count - 1; i >= 0; i--)
                {
                    var d = mf.deferStatementsList[i];
                    if (d.deferBlockStatements == null) continue;
                    IRBlockStatements irD = new IRBlockStatements(this);
                    irD.ParseIRStatements(d.deferBlockStatements);
                    for (int si = 0; si < irD.irStatements.Count; si++)
                    {
                        for (int j = 0; j < irD.irStatements[si].IRDataList.Count; j++)
                        {
                            var addIR = irD.irStatements[si].IRDataList[j];
                            addIR.id = m_IRDataList.Count;
                            AddLabelDict(addIR);
                            m_IRDataList.Add(addIR);
                        }
                    }
                }

                // Jump to real function end
                IRBranch brToEnd = new IRBranch(this, EIROpCode.BrLabel, originalFunEndLabel);
                for (int j = 0; j < brToEnd.IRDataList.Count; j++)
                {
                    var addIR = brToEnd.IRDataList[j];
                    addIR.id = m_IRDataList.Count;
                    AddLabelDict(addIR);
                    m_IRDataList.Add(addIR);
                }
            }

            // ── result 关键字 epilogue ──
            // 返回类型为 Result/Result<T> 且并非所有路径都显式 ret 时,
            // 掉落到函数末尾的控制流自动返回 result 变量:
            //   LoadLocal result -> StoreReturn
            List<IRBase> resultEpilogue = GenerateResultEpilogue();
            if (resultEpilogue != null)
            {
                for (int j = 0; j < resultEpilogue.Count; j++)
                {
                    for (int k = 0; k < resultEpilogue[j].IRDataList.Count; k++)
                    {
                        var addIR = resultEpilogue[j].IRDataList[k];
                        addIR.id = m_IRDataList.Count;
                        AddLabelDict(addIR);
                        m_IRDataList.Add(addIR);
                    }
                }
            }

            // Real function end label
            originalFunEndLabel.id = m_IRDataList.Count;
            m_IRDataList.Add(originalFunEndLabel);

            int nextLabelId = 1; // Label IDs start from 1 (0 reserved for "no label")

            for (int i = 0; i < m_LabelList.Count; i++)
            {
                var defLabel = m_LabelList[i];
                switch (defLabel.opCode)
                {
                    case EIROpCode.BrLabel:
                        {
                            var findex = IRDataList.FindIndex(a => a == defLabel.opValue);
                            defLabel.index = findex;
                            var targetIRData = defLabel.opValue as IRData;
                            if (targetIRData != null && targetIRData.opCode != EIROpCode.Label)
                            {
                                targetIRData.opCode = EIROpCode.Label;
                                targetIRData.index = nextLabelId;
                                targetIRData.Payload = BitConverter.GetBytes(nextLabelId);
                                targetIRData.UpdateByteLength();
                                nextLabelId++;
                            }
                            if (findex < 0)
                            {
                                // goto 引用的标签未定义: 占位目标从未被 label 语句发射进指令序列
                                // (此时 targetIRData.opValue 仍为 label 名字符串, FinalizePack 在其后才执行)
                                string labelName = targetIRData != null ? targetIRData.opValue as string : "?";
                                Log.AddIRLog(LID.GotoLabelNotDefined, null,
                                    "[" + defLabel.debugInfo.path + ":" + defLabel.debugInfo.beginLine + "]", labelName);
                            }
                        }
                        break;
                    case EIROpCode.Br:
                        {
                            var findex = IRDataList.FindIndex(a => a == defLabel.opValue);
                            defLabel.index = findex;
                            // Assign label ID to the target IRData for C VM marker-based jumps.
                            // The target's index becomes the label ID, which FinalizePack
                            // serializes as the branch instruction's payload. C# VM uses
                            // defLabel.index (instruction index); C VM uses payload (label ID).
                            var targetIRData = defLabel.opValue as IRData;
                            if (targetIRData != null && targetIRData.opCode != EIROpCode.Label)
                            {
                                targetIRData.opCode = EIROpCode.Label;
                                targetIRData.index = nextLabelId;
                                targetIRData.Payload = BitConverter.GetBytes(nextLabelId);
                                targetIRData.UpdateByteLength();
                                nextLabelId++;
                            }
                        }
                        break;
                    case EIROpCode.BrFalse:
                        {
                            var findex = IRDataList.FindIndex(a => a == defLabel.opValue);
                            defLabel.index = findex;
                            var targetIRData = defLabel.opValue as IRData;
                            if (targetIRData != null && targetIRData.opCode != EIROpCode.Label)
                            {
                                targetIRData.opCode = EIROpCode.Label;
                                targetIRData.index = nextLabelId;
                                targetIRData.Payload = BitConverter.GetBytes(nextLabelId);
                                targetIRData.UpdateByteLength();
                                nextLabelId++;
                            }
                        }
                        break;
                    case EIROpCode.BrTrue:
                        {
                            var findex = IRDataList.FindIndex(a => a == defLabel.opValue);
                            defLabel.index = findex;
                            var targetIRData = defLabel.opValue as IRData;
                            if (targetIRData != null && targetIRData.opCode != EIROpCode.Label)
                            {
                                targetIRData.opCode = EIROpCode.Label;
                                targetIRData.index = nextLabelId;
                                targetIRData.Payload = BitConverter.GetBytes(nextLabelId);
                                targetIRData.UpdateByteLength();
                                nextLabelId++;
                            }
                        }
                        break;
                    case EIROpCode.LeaveTry:
                        {
                            var findex = IRDataList.FindIndex(a => a == defLabel.opValue);
                            defLabel.index = findex;
                            var targetIRData = defLabel.opValue as IRData;
                            if (targetIRData != null && targetIRData.opCode != EIROpCode.Label)
                            {
                                targetIRData.opCode = EIROpCode.Label;
                                targetIRData.index = nextLabelId;
                                targetIRData.Payload = BitConverter.GetBytes(nextLabelId);
                                targetIRData.UpdateByteLength();
                                nextLabelId++;
                            }
                        }
                        break;
                }
            }

            // ---- Switch jump tables: serialize entry targets (IRData.id) into payload ----
            // Must run after all instruction ids are assigned and after label backfill
            // (targets may be label-converted IRData), but before FinalizePack/EmbedIndexInPayload.
            BuildPendingSwitchPayloads();

            // ---- Finalize packaging: serialize branch opValue (IRData ref) into Payload ----
            for (int i = 0; i < m_IRDataList.Count; i++)
            {
                try { m_IRDataList[i].FinalizePack(); } catch { }
                try { m_IRDataList[i].EmbedIndexInPayload(); } catch { }
            }

            // NOTE: Branch index holds the target's instruction-list index.
            // The C# VM uses m_ExecuteIndex = iri.index for jumps.
            // The C VM computes byte offsets at load time (vm_build_method_code)
            // and patches branch instructions for O(1) direct jumps.
        }
        /// <summary>
        /// 注册一个待构建的 switch 跳转表。
        /// IRSwitchStatements 发射 Switch 指令时登记（此时 case 体目标 IRData 引用尚无 id），
        /// Parse() 末尾 BuildPendingSwitchPayloads() 统一序列化。
        /// </summary>
        public void RegisterPendingSwitchTable(IRSwitchStatements.PendingSwitchTable table)
        {
            if (table != null && table.switchIRData != null)
            {
                m_PendingSwitchTableList.Add(table);
            }
        }

        /// <summary>
        /// 把所有 pending switch 表序列化进对应 Switch 指令的 Payload。
        /// 目标索引直接取 IRData.id（主发射循环中赋值，即 m_IRDataList 中的最终索引）。
        /// 直接赋值 Payload 以避开 SetOpValue 的重打包逻辑。
        /// </summary>
        private void BuildPendingSwitchPayloads()
        {
            for (int i = 0; i < m_PendingSwitchTableList.Count; i++)
            {
                var table = m_PendingSwitchTableList[i];
                try
                {
                    table.switchIRData.opValue = null;
                    table.switchIRData.Payload = IRSwitchStatements.BuildSwitchPayload(table);
                    table.switchIRData.UpdateByteLength();
                }
                catch { }
            }
            m_PendingSwitchTableList.Clear();
        }

        public void AddLabelDict( IRData irdata )
        {
            if( irdata.opCode == EIROpCode.Label )
            {
                var findlabel = m_LabelList.Find(a => a.opValue == irdata.opValue);
                if (findlabel == null )
                {
                    m_LabelList.Add(irdata);
                }
            }
            else if( irdata.opCode == EIROpCode.Br
                || irdata.opCode == EIROpCode.BrLabel
                || irdata.opCode == EIROpCode.BrFalse
                || irdata.opCode == EIROpCode.BrTrue
                || irdata.opCode == EIROpCode.LeaveTry )
            {
                m_LabelList.Add(irdata);
            }
        }

        /// <summary>
        /// 取得(或创建)goto/label 目标 IRData。
        /// label 语句处发射该实例本身(OpCode.Label), goto 语句发射 BrLabel 且
        /// opValue 指向该实例, 供 Parse() 回填阶段按引用找到目标指令索引。
        /// </summary>
        public IRData GetOrAddLabelTargetData(string labelName, Token token)
        {
            if (string.IsNullOrEmpty(labelName))
                return null;
            IRData target;
            if (!m_GotoLabelTargetDict.TryGetValue(labelName, out target))
            {
                target = new IRData();
                target.opCode = EIROpCode.Label;
                target.SetOpValue(labelName);
                target.SetDebugInfoByToken(token, "Label:" + labelName);
                m_GotoLabelTargetDict.Add(labelName, target);
            }
            return target;
        }
        public IRMetaVariable GetIRLocalVariableById( int id )
        {
            return m_MethodLocalVariableList.Find(a => a.id == id);
        }

        /// <summary>
        /// result 关键字: 生成函数入口的 result 变量初始化 prologue:
        ///   NewObject(classId)/NewTemplateObject(模板类型) -> StoreLocal result
        /// VM 侧 NewObject/NewTemplateObject 创建对象时会自动执行字段默认值初始化,
        /// 因此无需调用 _init_。仅当 bindMetaFunction 注入了 result 变量时生成。
        /// </summary>
        private List<IRBase> GenerateResultPrologue()
        {
            var mmf = m_BindMetaFunction as MetaMemberFunction;
            if (mmf == null || !mmf.hasResultVariable)
            {
                return null;
            }
            var rmv = mmf.resultVariable;
            var irmv = GetIRLocalVariableById(rmv.GetHashCode());
            if (irmv == null)
            {
                return null;
            }
            var mt = rmv.GetFinalMetaType();
            if (mt == null)
            {
                return null;
            }

            var prologue = new List<IRBase>();
            var owirmc = IRManager.GetIRMetaClassByMetaOwner(mmf.ownerMetaBase);
            if (mt.GetTemplateMetaClass() == CoreMetaClassManager.resultTMetaClass)
            {
                // 泛型 Result<T>: NewTemplateObject, payload 携带完整模板类型
                var irmt = IRMetaType.CreateIRMetaTypeByGenTemplateMetaTypeList(mt, owirmc);
                IRNew irNew = new IRNew(this, irmt);
                prologue.Add(irNew);
            }
            else
            {
                // 非泛型 Result: NewObject, payload 为 classId
                var irmc = IRManager.GetIRMetaClassByMetaType(mt);
                if (irmc == null)
                {
                    Log.AddIRLog(LID.MetaCoreAssertShowMessage, mmf.token, "result prologue: not found Result IRMetaClass!");
                    return null;
                }
                IRNew irNew = new IRNew(this, irmc);
                prologue.Add(irNew);
            }

            // StoreLocal result (经工厂方法: result 被闭包捕获时路由写入共享上下文数组槽)
            var storeIrmc = IRManager.GetIRMetaClassByMetaType(mt);
            var storeIrmt = storeIrmc != null ? new IRMetaType(storeIrmc) : null;
            IRStoreVariable storeResult = IRStoreVariable.CreateIRStoreVariable(storeIrmt, storeIrmc, this, rmv);
            prologue.Add(storeResult);
            return prologue;
        }

        /// <summary>
        /// result 关键字: 生成函数末尾的 result 兜底返回 epilogue:
        ///   LoadLocal result -> StoreReturn
        /// 仅当注入了 result 变量且并非所有代码路径都显式 ret 时生成
        /// (所有路径都 ret 时发射会覆盖显式 ret 其他 Result 对象的返回值)。
        /// </summary>
        private List<IRBase> GenerateResultEpilogue()
        {
            var mmf = m_BindMetaFunction as MetaMemberFunction;
            if (mmf == null || !mmf.hasResultVariable)
            {
                return null;
            }
            if (mmf.isBlockAlwaysReturn)
            {
                return null;
            }
            var rmv = mmf.resultVariable;
            var irmv = GetIRLocalVariableById(rmv.GetHashCode());
            if (irmv == null)
            {
                return null;
            }
            var mt = rmv.GetFinalMetaType();
            var irmc = IRManager.GetIRMetaClassByMetaType(mt);
            if (irmc == null)
            {
                return null;
            }
            var loadIrmt = new IRMetaType(irmc);

            var list = new List<IRBase>();
            // LoadLocal result (经工厂方法: result 被闭包捕获时路由读取共享上下文数组槽)
            IRLoadVariable loadResult = IRLoadVariable.CreateLoadVariable(loadIrmt, irmc, this, rmv);
            list.Add(loadResult);

            var storeRetData = new IRData();
            storeRetData.opCode = EIROpCode.StoreReturn;
            storeRetData.index = 0;
            storeRetData.SetDebugInfoByToken(mmf.token, "StoreReturn result");
            list.Add(new IRBase(storeRetData));
            return list;
        }

        /// <summary>
        /// 生成宿主函数的闭包共享捕获上下文 prologue:
        ///   1. AllocClosureContext N + StoreLocal __closure_ctx__  (分配共享数组存入隐藏局部变量)
        ///   2. 参数型捕获(参数/this)初始化: [LoadArgument argIdx][LoadLocal ctx][StoreArrayIndex slot flag=0]
        ///      参数有实参槽现值, 必须在函数入口拷入数组; 局部变量捕获槽保持 null,
        ///      由其声明/赋值语句(经 IRVariable 拦截路由)写入。
        /// 仅当 bindMetaFunction 是含 __closure_ctx__ 的宿主函数时生成 (闭包函数自身不生成)。
        /// </summary>
        private List<IRBase> GenerateClosureContextPrologue()
        {
            var mmf = m_BindMetaFunction as MetaMemberFunction;
            if (mmf == null || !mmf.hasClosureContext)
            {
                return null;
            }

            var ctxMv = mmf.closureContextVariable;
            var ctxIrmv = GetIRLocalVariableById(ctxMv.GetHashCode());
            if (ctxIrmv == null)
            {
                return null;
            }

            var prologue = new List<IRBase>();

            // 1. AllocClosureContext N (N = 注册表捕获总数, 0 捕获也分配空数组) + StoreLocal
            var allocData = new IRData();
            allocData.opCode = EIROpCode.AllocClosureContext;
            allocData.index = mmf.closureCaptureList.Count;
            allocData.SetDebugInfoByToken(mmf.token, "AllocClosureContext " + mmf.closureCaptureList.Count);
            prologue.Add(new IRBase(allocData));

            var storeCtxData = new IRData();
            storeCtxData.opCode = EIROpCode.StoreLocal;
            storeCtxData.index = ctxIrmv.index;
            storeCtxData.SetDebugInfoByToken(mmf.token, "StoreLocal __closure_ctx__");
            prologue.Add(new IRBase(storeCtxData));

            // 2. 参数型捕获初始化 (栈序 [..., value, array] -> flag = StoreTopMinus1_ValueTopMinus2)
            for (int i = 0; i < mmf.closureCaptureList.Count; i++)
            {
                var cap = mmf.closureCaptureList[i];
                var hostMv = cap?.hostMetaVariable;
                if (hostMv == null || hostMv.variableFrom != MetaVariable.EVariableFrom.Argument)
                {
                    continue;
                }
                var argIrmv = GetIRArgumentById(hostMv.GetHashCode());
                if (argIrmv == null)
                {
                    continue;
                }

                var loadArgData = new IRData();
                loadArgData.opCode = EIROpCode.LoadArgument;
                loadArgData.index = argIrmv.index;
                loadArgData.SetDebugInfoByToken(mmf.token, "LoadArgument capture:" + hostMv.name);
                prologue.Add(new IRBase(loadArgData));

                var loadCtxData = new IRData();
                loadCtxData.opCode = EIROpCode.LoadLocal;
                loadCtxData.index = ctxIrmv.index;
                loadCtxData.SetDebugInfoByToken(mmf.token, "LoadLocal __closure_ctx__");
                prologue.Add(new IRBase(loadCtxData));

                var storeSlotData = new IRData();
                storeSlotData.opCode = EIROpCode.StoreArrayIndex;
                storeSlotData.index = cap.slotIndex;
                storeSlotData.SetOpValue((byte)EStoreArrayIndexFlag.StoreTopMinus1_ValueTopMinus2);
                storeSlotData.SetDebugInfoByToken(mmf.token, "StoreArrayIndex capture:" + hostMv.name);
                prologue.Add(new IRBase(storeSlotData));
            }

            return prologue;
        }
        public IRMetaVariable GetIRArgumentById( int id )
        {
            return m_MethodArgumentList.Find(a => a.id == id);
        }
        public IRMetaVariable GetReturnVariableById( int id )
        {
            return m_MethodReturnList.Find(a => a.id == id);
        }

        public void PushBreakTarget(IRData target)
        {
            if (target != null)
            {
                m_BreakTargetStack.Push(target);
            }
        }

        public void PopBreakTarget()
        {
            if (m_BreakTargetStack.Count > 0)
            {
                m_BreakTargetStack.Pop();
            }
        }

        public IRData GetCurrentBreakTarget()
        {
            if (m_BreakTargetStack.Count == 0) return null;
            return m_BreakTargetStack.Peek();
        }

        public void PushContinueTarget(IRData target)
        {
            if (target != null)
            {
                m_ContinueTargetStack.Push(target);
            }
        }

        public void PopContinueTarget()
        {
            if (m_ContinueTargetStack.Count > 0)
            {
                m_ContinueTargetStack.Pop();
            }
        }

        public IRData GetCurrentContinueTarget()
        {
            if (m_ContinueTargetStack.Count == 0) return null;
            return m_ContinueTargetStack.Peek();
        }
        public string ToIRString()
        {
            StringBuilder sb = new StringBuilder();

            for( int i = 0; i < IRDataList.Count; i++ )
            {
                sb.Append(i.ToString() + " ");
                var d = IRDataList[i];
                sb.Append(d.ToString());
                sb.Append(FormatStaticFieldDebugSuffix(d));
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        /// <summary>
        /// IRData.ToString() only prints the field type for static loads/stores; add the declaring class field name
        /// (same index as <see cref="IRMetaClass.staticIRMetaVariableList"/>) so e.g. <c>global.arrvar1</c> reads clearly as Project static.
        /// </summary>
        string FormatStaticFieldDebugSuffix(IRData d)
        {
            if (d == null || m_IROwnerMetaClass == null)
                return string.Empty;
            if (d.opCode != EIROpCode.LoadStaticField && d.opCode != EIROpCode.StoreStaticField)
                return string.Empty;
            var list = m_IROwnerMetaClass.staticIRMetaVariableList;
            if (list == null || list.Count == 0)
                return string.Empty;
            for (int j = 0; j < list.Count; j++)
            {
                var v = list[j];
                if (v != null && v.index == d.index)
                    return " field:[" + v.name + "]";
            }
            return string.Empty;
        }

        public override string ToString()
        {
            return this.id;
        }
    }
}
