//****************************************************************************
//  File:      IRSwitchStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/19 12:00:00
//  Description: 
//****************************************************************************


using SimpleLanguage.Core;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRSwitchStatements : IRStatements
    {
        public IRSwitchStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public class IRCaseStatements
        {
            public List<IRCaseStatements> irCaseStatementsList => m_IRCaseStatementsList;

            // branches emitted in the condition part that should jump to the next case test (or default)
            public List<IRBranch> caseFalseBranchList = new List<IRBranch>();

            public List<IRBase> conditionStatList = new List<IRBase>();
            public List<IRBase> thenStatList = new List<IRBase>();

            public IRBranch caseEndBrach = null;
            public IRNop startNop = null;
            public IRNop thenNop = null;

            public bool isContinueNext = false;

            private List<IRCaseStatements> m_IRCaseStatementsList = new List<IRCaseStatements>();
            public void ParseIRStatements(IRMethod _irMethod, MetaSwitchStatements.MetaCaseStatements mires)
            {
                isContinueNext = mires.isContinueNext;
               

                //startNop.data.SetDebugInfoByToken(mires.finalExpress.GetToken());
                if( mires.matchType == MetaSwitchStatements.SwitchMatchType.ClassType)
                {
                    startNop = new IRNop(_irMethod);
                    thenNop = new IRNop(_irMethod);
                    conditionStatList.Add(startNop);

                    // ClassType case uses `is Type` semantics (optionally `is Type newVar`).
                    // Evaluate the bool condition by calling the existing as/is pipeline (Meta already resolved types).
                    // We implement matching with a BrFalse to next case (patched later), not with EIROpCode.Switch.
                    // Load match source
                    var ownerMetaClass = mires.matchMetaVariable.GetFinalTemplateMetaClass();
                    var owirmc = IRManager.GetIRMetaClassByMetaVariable(mires.matchMetaVariable)
                        ?? (ownerMetaClass != null ? IRManager.instance.GetIRMetaClassById(ownerMetaClass.GetHashCode()) : null);
                    var srcMt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mires.matchMetaVariable.GetFinalMetaType(), owirmc);
                    var srcMc = IRManager.GetIRMetaClassByMetaVariable(mires.matchMetaVariable);
                    var loadSrc = IRLoadVariable.CreateLoadVariable(srcMt, srcMc, _irMethod, mires.matchMetaVariable);
                    conditionStatList.Add(loadSrc);

                    // Load const type for target class
                    var targetIRMC = IRManager.instance.GetIRMetaClassById(mires.matchTypeClass.GetHashCode());
                    IRData loadType = new IRData { opCode = EIROpCode.LoadConstType, opValue = targetIRMC };
                    loadType.SetDebugInfoByToken(mires?.token, "SwitchCaseLoadType");
                    conditionStatList.Add(new IRBase(loadType));

                    // Compare: use CastClass opcode as an `is` test by convention (VM-side should return bool / null).
                    IRData castData = new IRData { opCode = EIROpCode.CastClass };
                    castData.SetDebugInfoByToken(mires?.token, "SwitchCaseIs");
                    conditionStatList.Add(new IRBase(castData));

                    var brFalse = new IRBranch(_irMethod, EIROpCode.BrFalse, null);
                    conditionStatList.Add(brFalse);
                    caseFalseBranchList.Add(brFalse);

                    // If binding variable exists, perform cast and store.
                    if (mires.defineMetaVariable != null)
                    {
                        conditionStatList.Add(IRLoadVariable.CreateLoadVariable(srcMt, srcMc, _irMethod, mires.matchMetaVariable));
                        conditionStatList.Add(new IRBase(loadType));
                        conditionStatList.Add(new IRBase(castData));

                        var bindMc = IRManager.GetIRMetaClassByMetaVariable(mires.defineMetaVariable);
                        if (bindMc == null)
                        {
                            var bindTpl = mires.defineMetaVariable.GetFinalTemplateMetaClass();
                            if (bindTpl != null)
                            {
                                bindMc = IRManager.instance.GetIRMetaClassById(bindTpl.GetHashCode());
                            }
                        }
                        if (bindMc == null)
                        {
                            bindMc = IRManager.GetIRMetaClassByMetaOwner(mires.defineMetaVariable.ownerMetaBase);
                        }
                        var bindMt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mires.defineMetaVariable.GetFinalMetaType(), owirmc);
                        var storeBind = IRStoreVariable.CreateIRStoreVariable(bindMt, bindMc, _irMethod, mires.defineMetaVariable);
                        conditionStatList.Add(storeBind);
                    }

                    m_IRCaseStatementsList.Add(this);
                }
                else if( mires.matchType == MetaSwitchStatements.SwitchMatchType.ConstValue )
                {
                    for( int i = 0; i < mires.constExpressList.Count; i++ )
                    {
                        IRCaseStatements iRCaseStatements = new IRCaseStatements();
                        m_IRCaseStatementsList.Add(iRCaseStatements);

                        iRCaseStatements.isContinueNext = mires.isContinueNext;

                        iRCaseStatements.startNop = new IRNop(_irMethod);
                        iRCaseStatements.thenNop = new IRNop(_irMethod);
                        iRCaseStatements.conditionStatList.Add(iRCaseStatements.startNop);

                        //mires.constExpressList[i].SetDebugInfoByToken(mires.finalExpress.GetToken());
                        var express = IRExpressManager.CreateExpress(_irMethod, mires.constExpressList[i] );
                        iRCaseStatements.conditionStatList.Add(express);

                        var caseFalseBreach = new IRBranch(_irMethod, EIROpCode.Switch, null);                     
                        //ifFalseBreach.SetDebugInfoByToken(mires.m_IfOrElseIfKeySyntax.token);
                        iRCaseStatements.conditionStatList.Add(caseFalseBreach);

                        // record for patching to next test / default
                        iRCaseStatements.caseFalseBranchList.Add(caseFalseBreach);

                        iRCaseStatements.conditionStatList.Add(iRCaseStatements.thenNop);

                    iRCaseStatements.caseEndBrach = new IRBranch(_irMethod, EIROpCode.Br, null);

                    IRBlockStatements irbs2 = new IRBlockStatements(_irMethod);
                    _irMethod.PushBreakTarget(iRCaseStatements.caseEndBrach.data);
                    try
                    {
                        irbs2.ParseAllIRStatements(mires.thenMetaStatements);
                    }
                    finally
                    {
                        _irMethod.PopBreakTarget();
                    }
                        iRCaseStatements.thenStatList.AddRange(irbs2.irStatements);
                        iRCaseStatements.thenStatList.Add(iRCaseStatements.caseEndBrach);
                    }
                    return;
                }
                else if (mires.matchType == MetaSwitchStatements.SwitchMatchType.EnumValue)
                {
                    startNop = new IRNop(_irMethod);
                    thenNop = new IRNop(_irMethod);
                    conditionStatList.Add(startNop);

                    var ownerMetaClass = mires.matchMetaVariable.GetFinalTemplateMetaClass();
                    var owirmc = IRManager.GetIRMetaClassByMetaVariable(mires.matchMetaVariable)
                        ?? (ownerMetaClass != null ? IRManager.instance.GetIRMetaClassById(ownerMetaClass.GetHashCode()) : null);

                    var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mires.matchMetaVariable.GetFinalMetaType(), owirmc);
                    var irmc = IRManager.GetIRMetaClassByMetaVariable(mires.matchMetaVariable);                   
                    IRLoadVariable loadLocal = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mires.matchMetaVariable );
                    conditionStatList.Add(loadLocal);

                    var caseFalseBreach = new IRBranch(_irMethod, EIROpCode.Switch, null );
                    //ifFalseBreach.SetDebugInfoByToken(mires.m_IfOrElseIfKeySyntax.token);
                    conditionStatList.Add(caseFalseBreach);

                    caseFalseBranchList.Add(caseFalseBreach);

                    m_IRCaseStatementsList.Add(this);
                }
                conditionStatList.Add(thenNop);

                caseEndBrach = new IRBranch(_irMethod, EIROpCode.Br, null);

                IRBlockStatements irbs = new IRBlockStatements(_irMethod);
                _irMethod.PushBreakTarget(caseEndBrach.data);
                try
                {
                    irbs.ParseAllIRStatements(mires.thenMetaStatements);
                }
                finally
                {
                    _irMethod.PopBreakTarget();
                }
                thenStatList.AddRange(irbs.irStatements);
                thenStatList.Add(caseEndBrach);

                //if (m_IfOrElseIfKeySyntax != null)
                //{
                //    ifEndBrach.data.SetDebugInfoByToken(m_IfOrElseIfKeySyntax?.executeBlockSyntax?.endBlock);
                //}
            }

            public string ToIRString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("#if ");
                //sb.AppendLine(m_FinalExpress.ToFormatString() + "#");

                //for (int i = 0; i < conditionStatList.Count; i++)
                //{
                //    sb.AppendLine(conditionStatList[i].ToIRString());
                //}

                //if(m_ThenMetaStatements != null )
                //{
                //    sb.AppendLine(m_ThenMetaStatements.ToIRString());
                //}

                return sb.ToString();
            }
        }
        public List<IRBase> ParseIRStatements(MetaSwitchStatements ms)
        {
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.Nop;
            insNode.SetDebugInfoByToken(ms?.token, "SwitchStart");

            IRBase irbase = new IRBase(insNode);
            m_IRStatements.Add(irbase);

            var ownerMetaClass = ms.matchSourceMv.GetFinalTemplateMetaClass();
            var owirmc = IRManager.GetIRMetaClassByMetaVariable(ms.matchSourceMv)
                ?? (ownerMetaClass != null ? IRManager.instance.GetIRMetaClassById(ownerMetaClass.GetHashCode()) : null);

            var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(ms.matchSourceMv.GetFinalMetaType(), owirmc);
            var irmc = IRManager.GetIRMetaClassByMetaVariable(ms.matchSourceMv);
            IRLoadVariable loadLocal = IRLoadVariable.CreateLoadVariable(irmt, irmc, irMethod, ms.matchSourceMv );
            m_IRStatements.Add(loadLocal);


            IRNop endIRNop = new IRNop(irMethod);
            List<IRCaseStatements> mirList = new List<IRCaseStatements>();

            for (int i = 0; i < ms.metaCaseStatements.Count; i++)
            {
                var meis = ms.metaCaseStatements[i];

                IRCaseStatements mire = new IRCaseStatements();
                mire.ParseIRStatements(irMethod, meis);

                // Flatten possible multi-const cases into separate tests (irCaseStatementsList)
                for (int j = 0; j < mire.irCaseStatementsList.Count; j++)
                {
                    mirList.Add(mire.irCaseStatementsList[j]);
                }
            }

            // emit all case tests and blocks
            for (int i = 0; i < mirList.Count; i++)
            {
                var cmire = mirList[i];
                m_IRStatements.AddRange(cmire.conditionStatList);
                m_IRStatements.AddRange(cmire.thenStatList);

                // by default, a matched case ends the switch
                if (cmire.caseEndBrach != null)
                {
                    cmire.caseEndBrach.data.opValue = endIRNop.data;
                }
            }

            IRNop defaultNop = null;
            if ( ms.defaultMetaStatements != null )
            {
                defaultNop = new IRNop(irMethod);
                m_IRStatements.Add(defaultNop);
                IRBlockStatements irbs = new IRBlockStatements(irMethod);
                irMethod.PushBreakTarget(endIRNop.data);
                try
                {
                    irbs.ParseIRStatements(ms.defaultMetaStatements);
                }
                finally
                {
                    irMethod.PopBreakTarget();
                }
                m_IRStatements.AddRange(irbs.irStatements);
            }
            else
            {
                defaultNop = endIRNop;
            }

            m_IRStatements.Add(endIRNop);

            // patch failed-case branches to jump to next case test or default
            for (int i = 0; i < mirList.Count; i++)
            {
                var mire = mirList[i];
                var nextTarget = (i < mirList.Count - 1) ? mirList[i + 1].startNop?.data : defaultNop.data;
                if (nextTarget == null) nextTarget = defaultNop.data;

                for (int j = 0; j < mire.caseFalseBranchList.Count; j++)
                {
                    mire.caseFalseBranchList[j].data.opValue = nextTarget;
                }

                // `next` means: continue matching the next case
                if (mire.isContinueNext && mire.caseEndBrach != null)
                {
                    mire.caseEndBrach.data.opValue = nextTarget;
                }
            }

            return m_IRStatements;
        }
    }
}
