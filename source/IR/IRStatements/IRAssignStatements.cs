//****************************************************************************
//  File:      IRAssignStatements.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/11 12:00:00
//  Description:  handle assign statements syntax to instruction r!
//****************************************************************************

using SimpleLanguage.Core.SelfMeta;
using SimpleLanguage.Core.Statements;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.IR
{
    public class IRAssignStatements : IRStatements
    {
        public IRAssignStatements(IRMethod method)
        {
            this.irMethod = method;
        }
        protected IRExpress m_IRExpress = null;
        protected IRStoreVariable m_StoreVariable = null;
        public void ParseIRStatements( MetaAssignStatements ms )
        {
            if (ms.finalMetaExpress != null)
            {
                m_IRExpress = new IRExpress(irMethod, ms.finalMetaExpress);
                m_IRStatements.Add(m_IRExpress);
            }

            //ms.leftMetaExpress.metaCallLink.ParseToIRDataList(irMethod, true );
            //m_IRStatements.AddRange(ms.leftMetaExpress.metaCallLink.irList);
        }
    }
}
