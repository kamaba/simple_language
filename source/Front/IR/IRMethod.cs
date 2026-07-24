//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core;

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
        private IRData m_FunEndLabelData = null;
        private IRManager m_IRManager = null;

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
        }
        public void Parse()
        {
            var mf = m_BindMetaFunction;
            var id2 = this.id;
            var vfn = mf.virtualFunctionName;

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
                m_MethodArgumentList.Add(imp);
            }

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
