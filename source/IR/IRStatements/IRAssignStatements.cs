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
        protected IRExpress m_IRExpress = null;
        protected IRStoreVariable m_StoreVariable = null;
        public override void ParseIRStatements()
        {
            if (m_FinalMetaExpress != null)
            {
                m_IRExpress = new IRExpress(irMethod, m_FinalMetaExpress);
                m_IRStatements.Add(m_IRExpress);
            }

            m_MetaCallLink.ParseToIRDataList(irMethod, true );
            m_IRStatements.AddRange(m_MetaCallLink.irList);
        }
    }
}
