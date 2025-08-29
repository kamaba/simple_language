//****************************************************************************
//  File:      IRSwitchStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/19 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using System.Collections.Generic;

namespace SimpleLanguage.IR
{
    public class IRSwitchStatements : IRStatements
    {
        public IRSwitchStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        public List<IRBase> ParseIRStatements(MetaSwitchStatements ms)
        {
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.Nop;
            //m_IRDataList.Add(insNode);
            /*
            for (int i = 0; i < m_MetaElseIfStatements.Count; i++)
            {
                var meis = m_MetaElseIfStatements[i];

                meis.ParseIRStatements(irMethod);
                m_IRDataList.AddRange(meis.conditionStatList);
                m_IRDataList.AddRange(meis.thenStatList);
            }

            IRData nopIR = new IRData();
            nopIR.opCode = EIROpCode.Nop;
            m_IRDataList.Add(nopIR);

            for (int i = 0; i < m_MetaElseIfStatements.Count; i++)
            {
                var meis = m_MetaElseIfStatements[i];
                meis.brData.opValue = nopIR;

                if (meis.ifData != null)
                {
                    if (i < m_MetaElseIfStatements.Count - 1)
                    {
                        meis.ifData.opValue = m_MetaElseIfStatements[i + 1].startData;
                    }
                    else if (i == m_MetaElseIfStatements.Count - 1)
                    {
                        meis.ifData.opValue = nopIR;
                    }
                }
            }
            */
            return m_IRStatements;
        }
    }
}
