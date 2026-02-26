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
            public List<IRBase> conditionStatList = new List<IRBase>();
            public List<IRBase> thenStatList = new List<IRBase>();

            public IRBranch caseEndBrach = null;
            public IRBranch caseFalseBreach = null;
            public IRNop startNop = null;
            public void ParseIRStatements(IRMethod _irMethod, MetaSwitchStatements.MetaCaseStatements mires)
            {
                startNop = new IRNop(_irMethod);
                conditionStatList.Add(startNop);

                //startNop.data.SetDebugInfoByToken(mires.finalExpress.GetToken());
                if( mires.matchType == MetaSwitchStatements.SwitchMatchType.ClassType)
                {
                    IRMetaClass irmc = _irMethod.irManager.GetIRMetaClassByName("S.Core.Type");
                    IRStoreVariable storeLocal = IRStoreVariable.CreateIRStoreVariable(new IRMetaType(irmc, null), irmc, _irMethod, mires.defineMetaVariable);
                    //storeLocal.data.SetDebugInfoByToken(mires.defineMetaVariable.pingToken);
                    conditionStatList.Add(storeLocal);
                    IRMetaType irmt = new IRMetaType(irmc, null);
                    IRLoadVariable loadLocal = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mires.defineMetaVariable);
                    //loadLocal.data.SetDebugInfoByToken(mires.defineMetaVariable.pingToken);
                    conditionStatList.Add(loadLocal);
                }
                else if( mires.matchType == MetaSwitchStatements.SwitchMatchType.ConstValue )
                {
                    for( int i = 0; i < mires.constExpressList.Count; i++ )
                    {
                        //mires.constExpressList[i].SetDebugInfoByToken(mires.finalExpress.GetToken());
                        var express = IRExpressManager.CreateExpress(_irMethod, mires.constExpressList[i] );
                        conditionStatList.Add(express);

                        var switchBreach = new IRBranch(_irMethod, EIROpCode.Switch, null);
                        //ifFalseBreach.SetDebugInfoByToken(mires.m_IfOrElseIfKeySyntax.token);
                        conditionStatList.Add(switchBreach);

                    }
                }
                else if (mires.matchType == MetaSwitchStatements.SwitchMatchType.EnumValue)
                {
                    var ownerMetaClass = mires.matchMetaVariable.GetFinalTemplateMetaClass();
                    var owirmc = IRManager.instance.GetIRMetaClassById(ownerMetaClass.GetHashCode());

                    var irmt = IRMetaType.CreateIRMetaTypeByDefineTemplateMetaTypeList(mires.matchMetaVariable.GetFinalMetaType(), owirmc);
                    var irmc = IRManager.instance.GetIRMetaClassById(mires.matchMetaVariable.GetOwnerClassTemplateClass().GetHashCode());                   
                    IRLoadVariable loadLocal = IRLoadVariable.CreateLoadVariable(irmt, irmc, _irMethod, mires.matchMetaVariable );

                    var switchBreach = new IRBranch(_irMethod, EIROpCode.Switch, null);
                    //ifFalseBreach.SetDebugInfoByToken(mires.m_IfOrElseIfKeySyntax.token);
                    conditionStatList.Add(switchBreach);
                }

                IRBlockStatements irbs = new IRBlockStatements(_irMethod);
                irbs.ParseAllIRStatements(mires.thenMetaStatements);
                thenStatList.AddRange(irbs.irStatements);

                var ifEndBrach = new IRBranch(_irMethod, EIROpCode.Br, null);
                thenStatList.Add(ifEndBrach);

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

            IRBase irbase = new IRBase(insNode);
            m_IRStatements.Add(irbase);

            IRNop endIRNop = new IRNop(irMethod);

            List<IRCaseStatements> mirList = new List<IRCaseStatements>();

            for (int i = 0; i < ms.metaCaseStatements.Count; i++)
            {
                var meis = ms.metaCaseStatements[i];

                IRCaseStatements mire = new IRCaseStatements();
                mirList.Add(mire);

                mire.ParseIRStatements(irMethod, meis);
                m_IRStatements.AddRange(mire.conditionStatList);
                m_IRStatements.AddRange(mire.thenStatList);
                mire.caseEndBrach.data.opValue = endIRNop.data;
            }

            if ( ms.defaultMetaStatements != null )
            {
                IRBlockStatements irbs = new IRBlockStatements(irMethod);
                irbs.ParseIRStatements(ms.defaultMetaStatements);
                m_IRStatements.AddRange(irbs.irStatements);
            }

            m_IRStatements.Add(endIRNop);

            List<IRData> irdataList = new List<IRData>();
            for (int i = 0; i < m_IRStatements.Count; i++)
            {
                for (int j = 0; j < m_IRStatements[i].IRDataList.Count; j++)
                {
                    var addIR = m_IRStatements[i].IRDataList[j];
                    irdataList.Add(addIR);
                }
            }

            for (int i = 0; i < mirList.Count; i++)
            {
                var mire = mirList[i];
                //if (mire.ifFalseBreach != null)
                //{
                //    if (i < mirList.Count - 1)
                //    {
                //        mire.ifFalseBreach.data.opValue = mirList[i + 1].startNop.data;
                //    }
                //    else if (i == mirList.Count - 1)
                //    {
                //        //mire.ifFalseBreach.data.opValue = ifEndIRNop.data;
                //    }
                //}
            }

            return m_IRStatements;
        }
    }
}
