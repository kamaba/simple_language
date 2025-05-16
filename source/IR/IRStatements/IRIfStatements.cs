//****************************************************************************
//  File:      IRIfStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/13 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Xml.Linq;

namespace SimpleLanguage.IR
{
    public class IRIfStatements : IRStatements
    {
        public IRIfStatements( IRMethod method )
        {
            this.irMethod = method;
        }
        public class MetaIRElseIfStatements
        {
            public List<IRBase> conditionStatList = new List<IRBase>();
            public List<IRBase> thenStatList = new List<IRBase>();

            public IRBranch ifEndBrach = null;
            public IRBranch ifFalseBreach = null;
            public IRNop startNop = null;

            private IRExpress m_IrExpress = null;
            public void ParseIRStatements( IRMethod _irMethod, MetaIfStatements.MetaElseIfStatements mires )
            {
                startNop = new IRNop( _irMethod );
                conditionStatList.Add(startNop);

                if (mires.ifElseState == MetaIfStatements.IfElseState.If || mires.ifElseState == MetaIfStatements.IfElseState.ElseIf)
                {
                    startNop.data.SetDebugInfoByToken(mires.finalExpress.GetToken());

                    m_IrExpress = new IRExpress(_irMethod, mires.finalExpress);
                    conditionStatList.Add(m_IrExpress);

                    if (mires.metaAssignManager?.isNeedSetMetaVariable == true)
                    {
                        IRStoreVariable storeLocal = new IRStoreVariable(_irMethod, mires.boolConditionVariable.GetHashCode());
                        storeLocal.data.SetDebugInfoByToken(mires.boolConditionVariable.pingToken);
                        conditionStatList.Add(storeLocal);

                        IRLoadVariable loadLocal = new IRLoadVariable(_irMethod, mires.boolConditionVariable.GetHashCode());
                        loadLocal.data.SetDebugInfoByToken(mires.boolConditionVariable.pingToken);
                        conditionStatList.Add(loadLocal);
                    }

                    ifFalseBreach = new IRBranch(_irMethod, EIROpCode.BrFalse, null);
                    //ifFalseBreach.SetDebugInfoByToken(mires.m_IfOrElseIfKeySyntax.token);
                    conditionStatList.Add(ifFalseBreach);
                }

                IRBlockStatements irbs = new IRBlockStatements(_irMethod);
                irbs.ParseAllIRStatements(mires.thenMetaStatements);
                thenStatList.AddRange(irbs.irStatements);

                //{}代码执行结束后的位置
                ifEndBrach = new IRBranch(_irMethod, EIROpCode.Br, null);
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
        public void ParseIRStatements( MetaIfStatements ifstatements )
        {
            for (int i = 0; i < ifstatements.metaElseIfStatements.Count; i++)
            {
                var meis = ifstatements.metaElseIfStatements[i];

                MetaIRElseIfStatements mire = new MetaIRElseIfStatements();

                mire.ParseIRStatements(irMethod, meis);
                m_IRStatements.AddRange(mire.conditionStatList);
                m_IRStatements.AddRange(mire.thenStatList);
            }

            IRNop ifEndIRNop = new IRNop(irMethod);
            m_IRStatements.Add(ifEndIRNop);


            //if (m_FileMetaKeyIfSyntax != null)
            //{
            //    ifEndIRNop.data.SetDebugInfoByToken(m_FileMetaKeyIfSyntax.ifExpressSyntax.executeBlockSyntax?.endBlock);
            //}

            for (int i = 0; i < ifstatements.metaElseIfStatements.Count; i++)
            {
                var meis = ifstatements.metaElseIfStatements[i];

                MetaIRElseIfStatements mire = new MetaIRElseIfStatements();
                mire.ParseIRStatements(irMethod, meis);

                mire.ifEndBrach.data.opValue = ifEndIRNop.data;

                if (mire.ifFalseBreach != null)
                {
                    if (i < ifstatements.metaElseIfStatements.Count - 1)
                    {
                        //mire.ifFalseBreach.data.opValue = ifstatements.metaElseIfStatements[i + 1].startNop.data;
                    }
                    else if (i == ifstatements.metaElseIfStatements.Count - 1)
                    {
                        mire.ifFalseBreach.data.opValue = ifEndIRNop.data;
                    }
                }
            }
        }
    }
}
