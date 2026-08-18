//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;
using SimpleLanguage.Export.SLIR.Types;
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
        /// <summary>声明该方法的类的 classId（来自 SLMethodPackage.declaringClassId）。
        /// 对于继承到子类的方法，指向声明类（如 Object）。0 表示未设置（按当前类处理）。</summary>
        public int declaringClassId => m_DeclaringClassId;
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
        private int m_DeclaringClassId = 0;
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
            // 16=overrideInterface(==interfaceMethod), 32=canRewrite, 64=constructInit, 128=extendParams
            var flags = mp?.flags ?? 0;
            m_IsStatic = (flags & 1) != 0;
            m_IsFinal = (flags & 2) != 0;
            m_IsAbstract = (flags & 4) != 0;
            m_IsOverrideFunction = (flags & 8) != 0;
            m_IsExtendParams = (flags & 128) != 0;
            m_DeclaringClassId = mp?.declaringClassId ?? 0;
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
        public IRMetaVariable GetIRLocalVariableById( int id )
        {
            return m_MethodLocalVariableList.Find(a => a.id == id);
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
