//****************************************************************************
//  File:      IRBlockStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/12 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.Core.Statements;
using SimpleLanguage.IR;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaBlockStatements
    {
        public override void ParseIRStatements()
        {
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.Nop;
            m_IRDataList.Add(insNode);
        }
        public void ParseAllIRStatements()
        {
            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.Nop;
            m_IRDataList.Add(insNode);

            MetaStatements mbs = m_NextMetaStatements;
            while (mbs != null)
            {
                mbs.ParseIRStatements();
                m_IRDataList.AddRange(mbs.irDataList);
                mbs = mbs.nextMetaStatements;
            }
        }
    }
}
