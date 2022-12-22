//****************************************************************************
//  File:      IRAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/11 12:00:00
//  Description:  handle assign statements syntax to instruction r!
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.Core.Statements
{
    public partial class MetaAssignStatements
    {
        IRExpress m_IRExpress = null;
        public override void ParseIRStatements()
        {
            if (m_FinalMetaExpress != null)
            {
                m_IRExpress = new IRExpress( irMethod, m_FinalMetaExpress);
                m_IRDataList.AddRange(m_IRExpress.IRDataList);
            }

            IRData insNode = new IRData();
            insNode.opCode = EIROpCode.StoreLocal;
            insNode.index = irMethod.GetLocalVariableIndex(m_MetaVariable);
            m_IRDataList.Add(insNode);
        }
    }
}
